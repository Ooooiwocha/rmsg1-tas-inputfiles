using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static bool in28=false; static int cur=0; static Dictionary<string,int> seen=new Dictionary<string,int>();
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var pr=a[0].Split(',').Select(x=>(int.Parse(x.Substring(0,x.Length-1)),x[x.Length-1])).ToDictionary(t=>t.Item1,t=>t.Item2);
  int F1=int.Parse(a[1]); bool armed=false;
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e",(ad,v,f)=>{ if(!armed) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); int ra=H.Real(ca); int b=H.bus.PeekByte(ca);
     if(in28){ Console.WriteLine("  reads: "+string.Join(" ",seen.Select(k=>k.Key))); seen.Clear(); }
     in28 = (b==0x28); cur=ra; Console.WriteLine(H.core.Frame+" "+ra.ToString("X6")+"="+b.ToString("X2")); return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Read,"r",(ad,v,f)=>{ if(!in28) return null; uint A=ad; int bank=(int)(A>>16), lo=(int)(A&0xFFFF);
     bool w = bank==0x7E||bank==0x7F||((bank<0x40||(bank>=0x80&&bank<0xC0))&&lo<0x2000);
     if(w){ int wa= bank==0x7F?0x10000+lo: lo; if(wa>=0x1200&&wa<0x1300) return null; if(wa>=0x1D00&&wa<0x2000) return null; string k=wa.ToString("X5")+":"+H.wram.PeekByte(wa).ToString("X2"); seen[k]=1;} return null;},null,null));
  for(int i=0;i<F1;i++){ string inp="............"; if(i<7198) inp=L[i]; else if(pr.ContainsKey(i)){ var ch=inp.ToCharArray(); ch[pr[i]=='A'?9:pr[i]=='X'?8:7]=pr[i]; inp=new string(ch);} 
    if(i==int.Parse(a[2])) armed=true; H.Set(inp); H.Adv(); }
 }}
