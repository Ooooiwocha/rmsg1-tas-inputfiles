using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var reads=new List<string>(); bool log=false;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.core.Frame+" "+H.bus.PeekByte(ca).ToString("X2")+" "+H.Real(ca).ToString("X6")); return null;},0x00A0A5,null));
  var t=DateTime.Now;
  for(int i=0;i<7290;i++){ if(i==7270) log=true; H.Set(i<L.Length?L[i]:"............"); H.Adv(); }
  Console.WriteLine((DateTime.Now-t).TotalSeconds+"s"); foreach(var r in reads.Where(r=>r.Contains(" 40 "))) Console.WriteLine(r);
}}
