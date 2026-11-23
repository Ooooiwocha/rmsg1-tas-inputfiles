using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  bool on=false; var wl=new List<string>(); uint ad=Convert.ToUInt32(a[0],16); int F0=int.Parse(a[1]),F1=int.Parse(a[2]);
  foreach(uint q in new[]{ad,ad|0x7E0000}){ uint qq=q; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Write,"w"+qq,(x,v,f)=>{ if(on) wl.Add(H.core.Frame+" W v="+v.ToString("X2")+" pc="+(H.RegV("PC")&0xFFFFFF).ToString("X6")+" D="+(H.RegV("D")&0xFFFF).ToString("X4")); return null;},qq,null)); }
  for(int i=0;i<F1;i++){ on=i>=F0; H.Set(i<L.Length?L[i]:"............"); H.Adv(); }
  foreach(var x in wl.Take(60)) Console.WriteLine("WL "+x);
 }}
