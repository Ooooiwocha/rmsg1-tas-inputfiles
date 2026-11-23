using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M {
  static List<(int f,int b,int a,int m)> reads=new List<(int,int,int,int)>(); static bool log=false; static byte[] snap=null; static bool armed=false; static int prevOp=-1, prevF=-1;
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    var ps=File.ReadAllText(a[0]).Split(' ').Select(int.Parse).OrderBy(x=>x).ToList(); string outd=a[1];
    int curP=0;
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log||snap!=null) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x);
        int fr=(int)H.core.Frame; int b=H.bus.PeekByte(ca); int m=x/8+1;
        if(prevOp>=0 && fr>prevF && fr>=curP){ snap=new byte[0x20000]; for(int q=0;q<0x20000;q++) snap[q]=H.wram.PeekByte(q); return null; }
        prevOp=(b<=3 && m==12 && fr>=curP)?b:-1; prevF=fr; return null;},0x00A0A5,null));
    for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
    foreach(int p in ps){ while(H.core.Frame<p){ H.Set("............"); H.Adv(); }
      var idle=H.Save(); curP=p; snap=null; prevOp=-1; log=true;
      for(int k=0;k<900 && snap==null;k++){ int fr=(int)H.core.Frame; H.Set(fr==p?".........A..":"............"); H.Adv(); }
      log=false; if(snap!=null) File.WriteAllBytes(outd+"/"+p+".ram",snap); H.Load(idle); }
  } }
