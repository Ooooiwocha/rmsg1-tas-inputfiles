using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]),F=int.Parse(a[1]),N=int.Parse(a[2]);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  while(H.core.Frame<F){ H.Set(H.core.Frame==p?".........A..":"............"); H.Adv(); }
  var s=H.Save(); bool on=false; var cnt=new Dictionary<uint,int>();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"all",(ad,v,f)=>{ if(!on) return null; if((ad>>16)>3) return null; cnt.TryGetValue(ad,out int c); cnt[ad]=c+1; return null;},null,null));
  foreach(int v in new[]{0,0xFF}){ H.Load(s); H.wram.PokeByte(0x1290,(byte)v); cnt.Clear(); on=true; for(int k=0;k<N;k++){ H.Set("............"); H.Adv(); } on=false;
    File.WriteAllLines("/home/claude/work/harness/x_"+v+".txt",cnt.OrderBy(k=>k.Key).Select(k=>k.Key.ToString("X6")+" "+k.Value)); }
 }}
