using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M {
 static string W(int a)=>H.wram.PeekByte(a).ToString("X2");
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]), F1=int.Parse(a[1]);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  bool on=false;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"h11",(ad,v,f)=>{ if(!on) return null; int y=H.RegV("Y")&0xFFFF;
     Console.WriteLine("F"+H.core.Frame+" 11 entry Y="+y.ToString("X4")+" 8C="+W(0x8C)+" 166C="+W(0x166C)+" w0[1600 1606 160F]="+W(0x1600)+" "+W(0x1606)+" "+W(0x160F)+" w1="+W(0x1620)+" "+W(0x1626)+" "+W(0x162F)+" 12E8="+W(0x12E8)); return null;},0x00BEBD,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"lp",(ad,v,f)=>{ if(!on) return null; int y=H.RegV("Y")&0xFFFF;
     Console.WriteLine("F"+H.core.Frame+"   C744 loop Y="+y.ToString("X4")+" 160F+Y="+W(0x160F+y)+" 1606+Y="+W(0x1606+y)); return null;},0x00C755,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  on=true; while(H.core.Frame<F1){ int fr=(int)H.core.Frame; H.Set(fr==p?".........A..":"............"); H.Adv(); }
 }}
