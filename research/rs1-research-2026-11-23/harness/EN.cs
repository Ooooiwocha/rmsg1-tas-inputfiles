using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static int tgt=-1; static bool armed=false; static long lastRead=0; static int endF=-1; static string endK=""; static int lastAddr=0, lastB=0, nreads=0;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var addrs=File.ReadAllLines(a[0]).Where(s=>s.Trim()!="").Select(s=>Convert.ToInt32(s.Trim(),16)).ToArray();
  int part=int.Parse(a[2]), nparts=int.Parse(a[3]); int CAP=int.Parse(a[4]);
  var sw=new StreamWriter(a[1]){AutoFlush=true};
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(H.core.Frame<7190) return null; int x=H.RegV("X")&0xFFFF;
     if(armed && x==0x58){ H.wram.PokeByte(0x12F4,(byte)(tgt&0xff)); H.wram.PokeByte(0x12F5,(byte)((tgt>>8)&0xff)); H.wram.PokeByte(0x12F6,(byte)(tgt>>16)); armed=false; }
     int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); lastAddr=H.Real(ca); lastB=H.bus.PeekByte(ca); lastRead=H.core.Frame; nreads++; return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e8D63",(ad,v,f)=>{ if(endF<0&&H.core.Frame>=7190){endF=(int)H.core.Frame; endK="8D63";} return null;},0x038D63,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e800C",(ad,v,f)=>{ if(endF<0&&H.core.Frame>=7190){endF=(int)H.core.Frame; endK="800C";} return null;},0x03800C,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e800F",(ad,v,f)=>{ if(endF<0&&H.core.Frame>=7190){endF=(int)H.core.Frame; endK="800F";} return null;},0x03800F,null));
  for(int i=0;i<7190;i++){ H.Set(L[i]); H.Adv(); }
  var S0=H.Save();
  for(int j=part;j<addrs.Length;j+=nparts){ tgt=addrs[j]; H.Load(S0); armed=true; endF=-1; endK=""; lastRead=7190; nreads=0;
    int fr=7190;
    for(;fr<7190+CAP;fr++){ H.Set(fr<7197?L[fr]:"............"); H.Adv(); if(endF>=0) break; if(fr>7200 && fr-lastRead>300) break; }
    sw.WriteLine(tgt.ToString("X6")+" "+(endF>=0?"END "+endK+" "+endF:"NO")+" last="+lastAddr.ToString("X6")+" b="+lastB.ToString("X2")+" lastf="+lastRead+" reads="+nreads+" armedLeft="+armed);
  }
 }}
