using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  bool tas=a[0]=="tas"; var pr=tas?new HashSet<int>():a[0].Split(',').Select(int.Parse).ToHashSet(); int M2=int.Parse(a[1]);
  int pokeF=a.Length>2?int.Parse(a[2]):-1; int pokeV=a.Length>3?Convert.ToInt32(a[3],16):0;
  int E=-1; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e",(ad,v,f)=>{ if(E<0) E=(int)H.core.Frame; return null;},0x038D63,null));
  int i=0; for(;E<0 && i<16000;i++){ if(i==pokeF) H.wram.PokeByte(0x12F4,(byte)pokeV); H.Set(tas?(i<L.Length?L[i]:"............"):(i<7198?L[i]:(pr.Contains(i)?".........A..":"............"))); H.Adv(); }
  Console.WriteLine("ENTER "+E);
  var vp=H.core.ServiceProvider.GetService<IVideoProvider>();
  for(int k=0;k<M2;k++){ H.Set("............"); H.core.FrameAdvance(H.ctl,k==M2-1,false); }
  var b=vp.GetVideoBuffer(); ulong h=1469598103934665603UL; foreach(var c in b) h=(h^(ulong)(uint)c)*1099511628211UL; Console.WriteLine("FINAL "+h.ToString("X16"));
 }}
