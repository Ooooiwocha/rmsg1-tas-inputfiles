using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var vp=H.core.ServiceProvider.GetService<IVideoProvider>();
  for(int i=0;i<7300;i++){ H.Set(i<L.Length?L[i]:"............"); if(i>=7180 && a.Length>0) H.bus.PokeByte(0x2100,0x0F); H.Adv();
    if(i%20==0 && i>=7000){ var b=vp.GetVideoBuffer(); long s=0; foreach(var c in b) s+=(c&0xFF)+((c>>8)&0xFF)+((c>>16)&0xFF); Console.WriteLine("F "+i+" sum="+s+" r2100="+H.bus.PeekByte(0x2100).ToString("X2")); } }
 }}
