using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
// S.exe OUT p0 p1 step MAXF
static class M {
  static List<(int f,int b,int a,int m)> reads=new List<(int,int,int,int)>(); static bool log=false;
  static int[] KEYS={0x1686,0x162F,0x1626,0x160F,0x1606,0x8C,0x181C,0x1290,0x1636,0x166C};
  static Dictionary<int,int[]> snapAt=new Dictionary<int,int[]>();
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    var w=new StreamWriter(a[0]); int p0=int.Parse(a[1]), p1=int.Parse(a[2]), step=int.Parse(a[3]), MAXF=int.Parse(a[4]);
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x);
        reads.Add(((int)H.core.Frame,H.bus.PeekByte(ca),H.Real(ca),x/8+1)); snapAt[reads.Count-1]=KEYS.Select(k=>(int)H.wram.PeekByte(k)).ToArray(); return null;},0x00A0A5,null));
    for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
    while(H.core.Frame<p0){ H.Set("............"); H.Adv(); }
    for(int p=p0;p<=p1;p+=step){
      var idle=H.Save(); reads.Clear(); snapAt.Clear(); log=true; int lastRead=p;
      for(int k=0;k<MAXF;k++){ int fr=(int)H.core.Frame; H.Set(fr==p?".........A..":"............"); H.Adv(); if(reads.Count>0) lastRead=reads[reads.Count-1].f; if(fr-lastRead>240 && fr>p+5) break; }
      log=false;
      // first jump: op 00-03 read in m12 after press, followed by a read in a later frame
      int ji=-1; for(int i=0;i<reads.Count-1;i++){ if(reads[i].f<p) continue; if(reads[i].b<=3 && reads[i+1].f>reads[i].f){ ji=i; break; } }
      if(ji<0){ w.WriteLine("p="+p+" nojump"); }
      else { var t=reads[ji+1]; var rest=reads.Skip(ji+1).ToList();
        string seq=string.Join(",",rest.Select(r=>(r.f-t.f)+":"+r.a.ToString("X6")));
        int h=seq.GetHashCode(); var lr=rest[rest.Count-1];
        w.WriteLine("p="+p+" op="+reads[ji].b.ToString("X2")+" opat="+reads[ji].a.ToString("X6")+" opm="+reads[ji].m+" T="+t.a.ToString("X6")+" Tf="+t.f+" Tm="+t.m+" keys="+string.Join(".",snapAt[ji+1].Select(v=>v.ToString("X2")))+" last="+lr.a.ToString("X6")+"@"+(lr.f-t.f)+" n="+rest.Count+" h="+h.ToString("X8")+" first="+string.Join(",",rest.Take(12).Select(r=>(r.f-t.f)+":"+r.b.ToString("X2")+"@"+r.a.ToString("X6"))));
      }
      w.Flush(); H.Load(idle); for(int k=0;k<step;k++){ H.Set("............"); H.Adv(); }
    }
    w.Close(); } }
