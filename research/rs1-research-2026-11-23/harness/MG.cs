using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines(a[0]).Where(l=>l.StartsWith("|")).ToArray(); int F0=int.Parse(a[1]), F1=int.Parse(a[2]);
  bool on=false; long fs=0; long idle=-1; int polls=0; long lastRead=-1; var rd=new List<string>(); long pollFirst=-1,pollLast=-1;
  foreach(uint pc in new uint[]{0x48065,0x48095,0x480A9,0x480DC,0x480FD,0x481C0,0x481C8,0x481DE,0x48245,0x48316,0x4832D,0x4834D,0x4839A,0x48439,0x484E7,0x48509,0x4852B,0x48559,0x48561,0x48586,0x485A6}){ uint q=pc; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"p"+q,(x,v,f)=>{ if(on){ polls++; long c=H.dbg.TotalExecutedCycles-fs; if(pollFirst<0) pollFirst=c; pollLast=c; } return null;},q,null)); }
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"idle",(x,v,f)=>{ if(on&&idle<0) idle=H.dbg.TotalExecutedCycles-fs; return null;},0x00893B,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(x,v,f)=>{ if(!on) return null; int xx=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+xx)<<16)|(H.wram.PeekByte(0x129d+xx)<<8)|H.wram.PeekByte(0x129c+xx); lastRead=H.dbg.TotalExecutedCycles-fs; rd.Add(H.bus.PeekByte(ca).ToString("X2")+"@"+H.Real(ca).ToString("X6")); return null;},0x00A0A5,null));
  for(int i=0;i<F1;i++){ on=i>=F0; fs=H.dbg.TotalExecutedCycles; idle=-1; polls=0; lastRead=-1; rd.Clear(); pollFirst=-1; pollLast=-1;
    H.Set(i<L.Length?L[i]:"............"); H.Adv(); long len=H.dbg.TotalExecutedCycles-fs;
    if(on) Console.WriteLine("F "+i+" len="+len+" idle="+idle+" lastRead="+lastRead+" polls="+polls+" pollspan="+pollFirst+"-"+pollLast+" reads="+string.Join(",",rd.Take(12))); }
 }}