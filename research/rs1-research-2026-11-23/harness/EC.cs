using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int last=-100;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"c",(ad,v,f)=>{ int fr=(int)H.core.Frame; if(fr-last>30) Console.WriteLine("ECCA f="+fr+" 1681="+H.wram.PeekByte(0x1681).ToString("X2")+" 10A6="+H.wram.PeekByte(0x10A6).ToString("X2")); last=fr; return null;},0x00ECCA,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
 }}
