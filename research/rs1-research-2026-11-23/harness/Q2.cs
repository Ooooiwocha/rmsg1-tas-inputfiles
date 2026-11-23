using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
class Sink : ITraceSink { public List<string> L=new List<string>(); public bool on=false; public void Put(TraceInfo i){ if(on) L.Add(i.Disassembly+" | "+i.RegisterInfo); } }
static class M { static List<string> reads=new List<string>(); static bool log=false;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]), F0=int.Parse(a[1]), F1=int.Parse(a[2]);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.core.Frame+" "+H.bus.PeekByte(ca).ToString("X2")+" "+H.Real(ca).ToString("X6")+" m"+(x/8+1)); return null;},0x00A0A5,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  var sk=new Sink(); var cnt=new Dictionary<uint,int>();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"all",(ad,v,f)=>{ if(!sk.on) return null; cnt.TryGetValue(ad,out int c); cnt[ad]=c+1; return null;},null,null));
  log=true;
  while(H.core.Frame<F1){ int fr=(int)H.core.Frame; sk.on=(fr>=F0); H.Set(fr==p?".........A..":"............"); H.Adv();
    if(fr>=F0){ File.WriteAllLines("/home/claude/work/harness/pc_"+p+"_"+fr+".txt",cnt.OrderBy(k=>k.Key).Select(k=>k.Key.ToString("X6")+" "+k.Value)); cnt.Clear(); } }
  foreach(var r in reads.Where(r=>int.Parse(r.Split(' ')[0])>=F0-40)) Console.WriteLine(r);
 }}
