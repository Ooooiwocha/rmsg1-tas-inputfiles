using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static List<int> reads=new List<int>(); static bool log=false;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.Real(ca)); return null;},0x00A0A5,null));
  int p=int.Parse(a[0]); int t=Convert.ToInt32(a[1],16);
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  while(H.core.Frame<p){ H.Set("............"); H.Adv(); }
  H.Set(".........A.."); H.Adv();
  for(int k=0;k<1500;k++){ var s=H.Save(); int pf=(int)H.core.Frame; reads.Clear(); log=true; H.Set(".........A.."); H.Adv(); H.Set("............"); H.Adv(); log=false;
    if(reads.Count>0 && reads[0]==t){ Console.WriteLine("PRESS "+p+","+pf); return; }
    H.Load(s); H.Set("............"); H.Adv(); }
  Console.WriteLine("FAIL");
 }}
