using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static List<int> reads=new List<int>(); static bool log=false;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.Real(ca)); return null;},0x00A0A5,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  var res=new List<int>();
  foreach(var t in a.Select(x=>Convert.ToInt32(x,16))){ var cur=H.Save(); bool ok=false;
    for(int d=0;d<2500&&!ok;d++){ H.Load(cur); for(int q=0;q<d;q++){ H.Set("............"); H.Adv(); } int pf=(int)H.core.Frame;
      reads.Clear(); log=true; H.Set(".........A.."); H.Adv(); for(int q=0;q<2;q++){ H.Set("............"); H.Adv(); } log=false;
      if(reads.Count>0 && reads[0]==t){ ok=true; res.Add(pf); H.Load(cur); for(int q=0;q<d;q++){ H.Set("............"); H.Adv(); } H.Set(".........A.."); H.Adv(); } }
    if(!ok){ Console.WriteLine("FAIL "+t.ToString("X6")); return; } }
  Console.WriteLine("PRESS "+string.Join(",",res));
 }}
