using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines(a[0]+"/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int lastIn=-1; for(int i=0;i<L.Length;i++) if(L[i].Replace("|","").Replace(".","").Replace("0","").Replace(",","").Trim()!="") lastIn=i;
  var reads=new List<string>(); int endF=-1; string endK="";
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(H.core.Frame<7000||endF>=0) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(H.core.Frame+":"+H.Real(ca).ToString("X6")+"="+H.bus.PeekByte(ca).ToString("X2")+"m"+(x/8+1)); return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e1",(ad,v,f)=>{ if(endF<0&&H.core.Frame>7000){endF=(int)H.core.Frame;endK="800F";} return null;},0x03800F,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e2",(ad,v,f)=>{ if(endF<0&&H.core.Frame>7000){endF=(int)H.core.Frame;endK="800C";} return null;},0x03800C,null));
  for(int i=0;i<lastIn+4000 && endF<0;i++){ H.Set(i<L.Length?L[i]:"............"); H.Adv(); }
  var pressLines=new List<string>(); for(int i=7000;i<=lastIn;i++){ string s=L[i]; if(s.Replace("|","").Replace(".","").Trim()!="") pressLines.Add(i+":"+s.Split('|')[2].Trim()); }
  Console.WriteLine("LASTIN "+lastIn+" END "+endK+" "+endF);
  Console.WriteLine("PRESSES "+string.Join(" ",pressLines));
  Console.WriteLine("TAIL "+string.Join(" ",reads));
 }}
