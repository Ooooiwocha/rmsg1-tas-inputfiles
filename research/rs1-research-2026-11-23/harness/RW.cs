using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int p=int.Parse(a[0]); int last=0; string lastS="";
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(H.core.Frame<p) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); lastS=H.core.Frame+" X="+x.ToString("X2")+" raw="+ca.ToString("X6")+" real="+H.Real(ca).ToString("X6")+" b="+H.bus.PeekByte(ca).ToString("X2"); return null;},0x00A0A5,null));
  for(int i=0;i<p+900;i++){ H.Set(i<7198?L[i]:(i==p?".........A..":"............")); H.Adv(); }
  Console.WriteLine("last read: "+lastS);
  Console.WriteLine("timer now: "+string.Join(" ",Enumerable.Range(0x12F4,3).Select(q=>H.wram.PeekByte(q).ToString("X2"))));
 }}
