using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
// probe: P.exe OUT step p0 p1 R
static class M {
  static List<(int f,int b,int a)> reads=new List<(int,int,int)>(); static bool log=false;
  static int pokeAddr=-1, pokeVal=0; static int holdUntilRead=0; // used during intervention
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    var w=new StreamWriter(a[0]); int step=int.Parse(a[1]), p0=int.Parse(a[2]), p1=int.Parse(a[3]), R=int.Parse(a[4]);
    int[] targets={0x1636,0x1686}; 
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x);
        reads.Add(((int)H.core.Frame,H.bus.PeekByte(ca),H.Real(ca))); return null;},0x00A0A5,null));
    for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
    while(H.core.Frame<p0){ H.Set("............"); H.Adv(); }
    for(int p=p0;p<=p1;p+=step){
      var idle=H.Save(); var frameStates=new Dictionary<int,byte[]>(); reads.Clear(); log=true;
      for(int k=0;k<R;k++){ int fr=(int)H.core.Frame; frameStates[fr]=H.Save(); H.Set(fr==p?".........A..":"............"); H.Adv(); }
      log=false; var rd=reads.ToList(); Console.Error.WriteLine("p="+p+" reads="+rd.Count+" n11="+rd.Count(r=>r.b==0x11)+" last="+(rd.Count>0?rd[rd.Count-1].f:0));
      // events: reads of 0x11 or 0x13 with a later read in a later frame
      for(int i=0;i<rd.Count;i++){ var e=rd[i]; if(e.b!=0x11 && e.b!=0x13) continue; if(e.f<=p) continue;
        int j=i+1; if(j>=rd.Count) continue; int d=rd[j].f-e.f; int nxt=rd[j].a;
        // baseline values at frame start of e.f
        H.Load(frameStates[e.f]); int v36=H.wram.PeekByte(0x1636), v86=H.wram.PeekByte(0x1686), v90=H.wram.PeekByte(0x1290);
        var res=new List<string>();
        foreach(int pre in new[]{0,4,10}){ if(!frameStates.ContainsKey(e.f-pre)) continue;
          foreach(int val in new[]{0,2,4,1,3}){ if(val==v36) continue;
            H.Load(frameStates[e.f-pre]); reads.Clear(); log=true; int d2=-1; int n2=-1;
            for(int k=0;k<pre+d+40;k++){ int fr=(int)H.core.Frame; H.wram.PokeByte(0x1636,(byte)val); H.Set("............"); H.Adv();
              // find read of e at e.a then the next read after it
              int ix=reads.FindIndex(r=>r.a==e.a && r.b==e.b); if(ix>=0 && ix+1<reads.Count){ d2=reads[ix+1].f-reads[ix].f; n2=reads[ix+1].a; break; } }
            log=false; if(d2!=d || n2!=nxt) res.Add("pre"+pre+":v"+val+"->"+d2+"/"+n2.ToString("X6")); } }
        w.WriteLine("p="+p+" f="+e.f+" cmd="+e.b.ToString("X2")+" at="+e.a.ToString("X6")+" d="+d+" next="+nxt.ToString("X6")+" 1636="+v36.ToString("X2")+" 1686="+v86.ToString("X2")+" 1290="+v90.ToString("X2")+" DIFF "+string.Join(",",res));
        w.Flush(); }
      H.Load(idle); for(int k=0;k<step;k++){ H.Set("............"); H.Adv(); }
    }
    w.Close(); } }
