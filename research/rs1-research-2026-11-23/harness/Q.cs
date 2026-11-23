using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static List<string> reads=new List<string>(); static bool log=false;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]), F=int.Parse(a[1]);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.core.Frame+" "+H.bus.PeekByte(ca).ToString("X2")+" "+H.Real(ca).ToString("X6")+" 1636="+H.wram.PeekByte(0x1636).ToString("X2")); return null;},0x00A0A5,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  while(H.core.Frame<F){ H.Set(H.core.Frame==p?".........A..":"............"); H.Adv(); }
  var s=H.Save();
  foreach(int v in new[]{-1,0,2,4}){ H.Load(s); reads.Clear(); log=true; for(int k=0;k<30;k++){ if(v>=0) H.wram.PokeByte(0x1636,(byte)v); H.Set("............"); H.Adv(); } log=false;
    Console.WriteLine("== v="+v+" reads="+reads.Count); foreach(var r in reads.Take(14)) Console.WriteLine(r); }
 }}
