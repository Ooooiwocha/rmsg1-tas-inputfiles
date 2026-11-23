using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int pokeAt=int.Parse(a[0]); bool poked=false; int pf=-1;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"c",(ad,v,f)=>{ int D=H.RegV("D")&0xFFFF; int val=H.wram.PeekByte(D+0xF0)|(H.wram.PeekByte(D+0xF1)<<8);
     Console.WriteLine("C1AF f="+H.core.Frame+" D="+D.ToString("X4")+" val="+val.ToString("X4"));
     if(!poked && H.core.Frame>=pokeAt){ H.wram.PokeByte(D+0xF0,0x77); H.wram.PokeByte(D+0xF1,0x02); poked=true; pf=(int)H.core.Frame; Console.WriteLine("POKED"); } return null;},0x01C1AF,null));
  var seen=new HashSet<int>();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!poked) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); int ra=H.Real(ca); if((ra>>8)!=0 && seen.Add(ra>>8)) Console.WriteLine("  read f="+H.core.Frame+" "+ra.ToString("X6")); return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e",(ad,v,f)=>{ Console.WriteLine("END8D63 f="+H.core.Frame); return null;},0x038D63,null));
  int N=int.Parse(a[1]);
  for(int i=0;i<N;i++){ H.Set(poked? "............" : (i<L.Length?L[i]:"............")); H.Adv(); }
 }}
