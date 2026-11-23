using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int F0=int.Parse(a[0]),F1=int.Parse(a[1]),st=int.Parse(a[2]); string od=a[3]; bool poke=a[4]=="1"; Directory.CreateDirectory(od); var vp=H.core.ServiceProvider.GetService<IVideoProvider>();
  for(int i=0;i<F1;i++){ if(poke&&i>=F0){ H.wram.PokeByte(0x10A6,(byte)(H.wram.PeekByte(0x10A6)|0x30)); }
    if(i>=F0&&(i-F0)%st==0) Console.WriteLine(i+" 1681="+H.wram.PeekByte(0x1681).ToString("X2"));
    H.Set(L[i]); bool cap=i>=F0&&(i-F0)%st==0; H.core.FrameAdvance(H.ctl,cap,false);
    if(cap){ var b=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){int c=b[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c;} File.WriteAllBytes(od+"/"+i.ToString("D6")+"_"+w+"x"+h+".rgb",raw);} }
 }}
