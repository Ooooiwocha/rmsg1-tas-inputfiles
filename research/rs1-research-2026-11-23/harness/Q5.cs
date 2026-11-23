using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static int CMD; static List<(int f,int b,int a)> reads=new List<(int,int,int)>(); static bool log=false;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]); int F=int.Parse(a[1]); int addr=Convert.ToInt32(a[2],16); CMD=Convert.ToInt32(a[3],16);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(((int)H.core.Frame,H.bus.PeekByte(ca),H.Real(ca))); return null;},0x00A0A5,null));
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  while(H.core.Frame<F){ H.Set(H.core.Frame==p?".........A..":"............"); H.Adv(); }
  var s=H.Save(); int orig=H.wram.PeekByte(addr);
  foreach(int v in a.Skip(4).Select(x=>Convert.ToInt32(x,16)).Prepend(-1)){ H.Load(s); if(v>=0) H.wram.PokeByte(addr,(byte)v); reads.Clear(); log=true; for(int k=0;k<200;k++){ H.Set("............"); H.Adv(); } log=false;
    int i=reads.FindIndex(r=>r.b==CMD && r.f==F); string res=(i>=0&&i+1<reads.Count)?("d="+(reads[i+1].f-reads[i].f)+" next="+reads[i+1].a.ToString("X6")):"no11";
    Console.WriteLine("RES "+a[2]+" "+(v<0?"orig("+orig.ToString("X2")+")":v.ToString("X2"))+" "+res); }
 }}
