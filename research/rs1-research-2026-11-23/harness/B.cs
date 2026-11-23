using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
// B.exe pA pB : bisect WRAM bytes that turn A's outcome into B's
static class M {
  static List<(int f,int b,int a,int m)> reads=new List<(int,int,int,int)>(); static bool log=false;
  static string[] L; static Dictionary<int,int> apu=new Dictionary<int,int>(); static List<string> cyc=new List<string>(); static Dictionary<int,long> fstart=new Dictionary<int,long>();
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    foreach(uint pc in new uint[]{0x48065,0x48095,0x480A9,0x480DC,0x480FD,0x481C0,0x481C8,0x481DE,0x48245,0x48316,0x4832D,0x4834D,0x4839A,0x48439,0x484E7,0x48509,0x4852B,0x48559,0x48561,0x48586,0x485A6}){ uint q=pc; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"w"+q,(ad,v,f)=>{ if(log){ apu.TryGetValue((int)H.core.Frame,out int c); apu[(int)H.core.Frame]=c+1; } return null;},q,null)); }
    foreach(uint pa in new uint[]{}){ uint q=pa; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Read,"apu"+q,(ad,v,f)=>{ if(log){ apu.TryGetValue((int)H.core.Frame,out int c); apu[(int)H.core.Frame]=c+1; } return null;},q,null)); }
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x);
        reads.Add(((int)H.core.Frame,H.bus.PeekByte(ca),H.Real(ca),x/8+1)); if(fstart.ContainsKey((int)H.core.Frame)) cyc.Add((H.core.Frame)+":"+(H.dbg.TotalExecutedCycles-fstart[(int)H.core.Frame])); return null;},0x00A0A5,null));
    for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
    var root=H.Save();
    int pA=int.Parse(a[0]), pB=int.Parse(a[1]);
    var (sA,ramA,TA)=ToT(root,pA); var (sB,ramB,TB)=ToT(root,pB);
    if(a.Length>2 && a[2]=="clk"){ string oA2=Out(sA,null,null), oB2=Out(sB,null,null);
      ulong va=BitConverter.ToUInt64(sA,101604), vb=BitConverter.ToUInt64(sB,101604); Console.WriteLine("CLK A="+va+" B="+vb);
      var modes=a.Length>6?new[]{a[6]}:new[]{"Aapu","Bapu"}; ulong st0=a.Length>5?ulong.Parse(a[5]):0;
      foreach(var mode in modes){ var baseS=(byte[])sA.Clone(); if(mode=="Bapu") for(int i=315908;i<381444;i++) baseS[i]=sB[i];
        int N=int.Parse(a[3]); ulong mx=ulong.Parse(a[4]);
        for(int k=0;k<=N;k++){ ulong v=st0+(mx-st0)/(ulong)N*(ulong)k; v&=~7UL; var t=(byte[])baseS.Clone(); var bb=BitConverter.GetBytes(v); for(int j=0;j<8;j++) t[101604+j]=bb[j];
          string res; try{ res=Out(t,null,null);}catch(Exception e){res="ERR";} Console.WriteLine("SW "+mode+" "+v+" "+(res==oB2?"B":res==oA2?"A":"other")); } }
      return; }
    if(a.Length>2 && a[2]=="st"){ string oA2=Out(sA,null,null), oB2=Out(sB,null,null); Console.WriteLine("A "+oA2+"\nB "+oB2+" len="+sA.Length+"/"+sB.Length);
      var regs=new (string,int,int)[]{("pre",0,114440),("VRAM",114440,179976),("CGRAM",179976,180488),("CPU",180488,184392),("WRAM",184392,315464),("SPCREG",315464,315908),("APURAM",315908,381444),("rest",381444,sA.Length)};
      Console.WriteLine("PREDIFF "+string.Join(" ",Enumerable.Range(0,114440).Where(i=>sA[i]!=sB[i]).Select(i=>i+":"+sA[i].ToString("X2")+">"+sB[i].ToString("X2"))));
      foreach(var r in regs){ int n=0; for(int i=r.Item2;i<r.Item3&&i<sA.Length;i++) if(sA[i]!=sB[i]) n++; Console.WriteLine("DIFF "+r.Item1+" "+n); }
      foreach(var set in a.Skip(3)){ var names=set.Split('+'); var t=(byte[])sA.Clone(); foreach(var r in regs) if(names.Contains(r.Item1)) for(int i=r.Item2;i<r.Item3&&i<t.Length;i++) t[i]=sB[i];
        foreach(var nm in names) if(nm.StartsWith("@")) foreach(var bi in nm.Substring(1).Split(',')){ int ix=int.Parse(bi); t[ix]=sB[ix]; }
        string res; try{ res=Out(t,null,null);}catch(Exception e){res="ERR "+e.Message;} Console.WriteLine("SWAP "+set+" -> "+(res==oB2?"=B":res==oA2?"=A":"other "+res)); }
      return; }
    if(a.Length>2 && a[2]=="watch"){ var adrs=a.Skip(3).Select(x=>Convert.ToUInt32(x,16)).ToList(); var wl=new List<string>(); bool won=false; int wf0=0;
      foreach(uint ad0 in adrs){ foreach(uint ad in new[]{ad0,ad0|0x7E0000}){ uint q=ad; foreach(var ty in new[]{MemoryCallbackType.Write,MemoryCallbackType.Read}){ var tt=ty;
        H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",tt,"w"+q+tt,(x,v,f)=>{ if(won) wl.Add((H.core.Frame-wf0)+" "+(tt==MemoryCallbackType.Write?"W":"R")+" "+(q&0xFFFF).ToString("X4")+" v="+v.ToString("X2")+" pc="+(H.RegV("PC")&0xFFFFFF).ToString("X6")); return null;},q,null)); } } }
      foreach(var st in new[]{sA,sB}){ H.Load(st); wf0=(int)H.core.Frame; wl.Clear(); reads.Clear(); won=true; log=true; for(int k=0;k<130;k++){ H.Set("............"); H.Adv(); } won=false; log=false;
        Console.WriteLine("=== run"); foreach(var x in wl.Take(80)) Console.WriteLine("WL "+x); Console.WriteLine("SEQ "+string.Join(" ",reads.Select(x=>(x.f-wf0)+":"+x.b.ToString("X2")+"@"+x.a.ToString("X6")).Skip(15).Take(30))); }
      return; }
    if(a.Length>2 && a[2]=="grp"){ string oA2=Out(sA,null,null), oB2=Out(sB,null,null); Console.WriteLine("A "+oA2+"\nB "+oB2);
      foreach(var g in a.Skip(3)){ var idx=g.Split(',').Select(x=>Convert.ToInt32(x,16)).ToList(); string r=Out(sA,idx,ramB); Console.WriteLine("GRP "+g+" -> "+(r==oB2?"=B":r==oA2?"=A":"other "+r)); } return; }
    if(a.Length>2 && a[2]=="val"){ string oA2=Out(sA,null,null); int ad=Convert.ToInt32(a[3],16); Console.WriteLine("A "+oA2+" orig="+ramA[ad].ToString("X2")+" B="+ramB[ad].ToString("X2"));
      foreach(var vs in a.Skip(4)){ int v=Convert.ToInt32(vs,16); var tmp=(byte[])ramA.Clone(); tmp[ad]=(byte)v; string r=Out(sA,new List<int>{ad},tmp); Console.WriteLine("VAL "+vs+" -> "+r); } return; }
    if(a.Length>2){ foreach(var st in new[]{sA,sB}){ H.Load(st); int f0=(int)H.core.Frame; reads.Clear(); apu.Clear(); log=true; for(int k=0;k<400;k++){ H.Set("............"); H.Adv(); } log=false;
        Console.WriteLine("SEQ "+string.Join(" ",reads.Select(x=>(x.f-f0)+":"+x.b.ToString("X2")+"@"+x.a.ToString("X6"))));
        Console.WriteLine("POLL "+string.Join(" ",apu.OrderBy(k=>k.Key).Select(k=>(k.Key-f0)+":"+k.Value))); } return; }
    string oA=Out(sA,null,null), oB=Out(sB,null,null);
    Console.WriteLine("T "+TA.ToString("X6")+"/"+TB.ToString("X6")+"\nA "+oA+"\nB "+oB);
    foreach(var md in H.core.ServiceProvider.GetService<IMemoryDomains>()) Console.WriteLine("DOM "+md.Name+" "+md.Size);
    foreach(var dn in new[]{"APURAM","VRAM","OAM","CGRAM","SRAM"}){ var dm=H.core.ServiceProvider.GetService<IMemoryDomains>()[dn]; if(dm==null) continue;
      H.Load(sB); var bb=new byte[dm.Size]; for(long q=0;q<dm.Size;q++) bb[q]=dm.PeekByte(q);
      H.Load(sA); int nd=0; for(long q=0;q<dm.Size;q++) if(dm.PeekByte(q)!=bb[q]) nd++;
      string r=OutDom(sA,dm,bb); Console.WriteLine("swap "+dn+" ndiff="+nd+" -> "+(r==oB?"=B":r==oA?"=A":"other "+r)); }
    { var doms=H.core.ServiceProvider.GetService<IMemoryDomains>(); var names=new[]{"WRAM","APURAM","VRAM"}; var bbs=new Dictionary<string,byte[]>();
      H.Load(sB); foreach(var n in names){ var dm=doms[n]; var bb=new byte[dm.Size]; for(long q=0;q<dm.Size;q++) bb[q]=dm.PeekByte(q); bbs[n]=bb; }
      H.Load(sA); foreach(var n in names){ var dm=doms[n]; var bb=bbs[n]; for(long q=0;q<dm.Size;q++) if(dm.PeekByte(q)!=bb[q]) dm.PokeByte(q,bb[q]); }
      string r=Out(null,null,null); Console.WriteLine("swap WRAM+APURAM+VRAM -> "+(r==oB?"=B":r==oA?"=A":"other "+r));
      foreach(var st in new[]{sA,sB}){ H.Load(st); int f0=(int)H.core.Frame; reads.Clear(); cyc.Clear(); log=true; for(int k=0;k<160;k++){ fstart[(int)H.core.Frame]=H.dbg.TotalExecutedCycles; H.Set("............"); H.Adv(); } log=false;
        Console.WriteLine("CYC "+string.Join(" ",cyc.Select(c=>c))); Console.WriteLine("APU "+string.Join(" ",apu.OrderBy(k=>k.Key).Where(k=>k.Key>=f0+140&&k.Key<=f0+152).Select(k=>(k.Key-f0)+":"+k.Value))); apu.Clear(); Console.WriteLine("SEQ "+string.Join(" ",reads.Select(x=>(x.f-f0)+":"+x.b.ToString("X2")+"@"+x.a.ToString("X6")))); } }
    var D=Enumerable.Range(0,0x20000).Where(i=>ramA[i]!=ramB[i]).ToList(); Console.WriteLine("diff bytes "+D.Count);
    string all=Out(sA,D,ramB); Console.WriteLine("A+allB "+(all==oB?"=B":all==oA?"=A":"other: "+all));
    if(all!=oB) return;
    var S=D;
    while(S.Count>1){ var h1=S.Take(S.Count/2).ToList(); var h2=S.Skip(S.Count/2).ToList();
      string r1=Out(sA,h1,ramB); if(r1==oB){ S=h1; continue; } string r2=Out(sA,h2,ramB); if(r2==oB){ S=h2; continue; }
      Console.WriteLine("neither half alone ("+S.Count+"): r1="+(r1==oA?"A":"other")+" r2="+(r2==oA?"A":"other")); break; }
    Console.WriteLine("SET "+string.Join(" ",S.Take(40).Select(i=>i.ToString("X5")+":"+ramA[i].ToString("X2")+"->"+ramB[i].ToString("X2"))));
  }
  static (byte[],byte[],int) ToT(byte[] root,int p){ H.Load(root); var st=new Dictionary<int,byte[]>(); reads.Clear(); log=true; int prevOp=-1,prevF=-1; int Tf=-1,T=0;
    while(H.core.Frame<p){ H.Set("............"); H.Adv(); }
    for(int k=0;k<900&&Tf<0;k++){ int fr=(int)H.core.Frame; st[fr]=H.Save(); int n0=reads.Count; H.Set(fr==p?".........A..":"............"); H.Adv();
      for(int i=n0;i<reads.Count;i++){ var r=reads[i]; if(prevOp>=0 && r.f>prevF){ Tf=r.f; T=r.a; break; } prevOp=(r.b<=3&&r.m==12&&r.f>=p)?r.b:-1; prevF=r.f; } }
    log=false; H.Load(st[Tf]); var ram=new byte[0x20000]; for(int q=0;q<0x20000;q++) ram[q]=H.wram.PeekByte(q); return (st[Tf],ram,T); }
  static string OutDom(byte[] s,MemoryDomain dm,byte[] bb){ H.Load(s); for(long q=0;q<dm.Size;q++) if(dm.PeekByte(q)!=bb[q]) dm.PokeByte(q,bb[q]); return Out(null,null,null); }
  static string Out(byte[] s,List<int> idx,byte[] src){ if(s!=null) H.Load(s); if(idx!=null) foreach(int i in idx) H.wram.PokeByte(i,src[i]); int f0=(int)H.core.Frame; reads.Clear(); log=true;
    for(int k=0;k<400;k++){ H.Set("............"); H.Adv(); } log=false; if(reads.Count==0) return "none";
    return string.Join(",",reads.Select(r=>(r.f-f0)+":"+r.a.ToString("X6"))).GetHashCode().ToString("X8")+" last="+reads[reads.Count-1].a.ToString("X6")+"@"+(reads[reads.Count-1].f-f0)+" n="+reads.Count; }
}
