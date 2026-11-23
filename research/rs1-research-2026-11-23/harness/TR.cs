using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var pr=a[0]=="tas"?new HashSet<int>():a[0].Split(',').Select(int.Parse).ToHashSet(); int F0=int.Parse(a[1]),F1=int.Parse(a[2]); bool log=false;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); Console.WriteLine("R "+H.core.Frame+" "+H.bus.PeekByte(ca).ToString("X2")+" "+H.Real(ca).ToString("X6")); return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e",(ad,v,f)=>{ if(log) Console.WriteLine("E "+H.core.Frame+" 8D63"); return null;},0x038D63,null));
  int PF=a.Length>3?int.Parse(a[3]):-1; int PV=a.Length>4?Convert.ToInt32(a[4],16):0;
  for(int i=0;i<F1;i++){ log=i>=F0; if(i==PF){ Console.WriteLine("POKE 12F4 "+H.wram.PeekByte(0x12F4).ToString("X2")+"->"+PV.ToString("X2")); H.wram.PokeByte(0x12F4,(byte)PV); } H.Set(a[0]=="tas"?(i<L.Length?L[i]:"............"):(i<7198?L[i]:(pr.Contains(i)?".........A..":"............"))); H.Adv(); }
 }}
