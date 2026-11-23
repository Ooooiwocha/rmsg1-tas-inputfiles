using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var vp=H.core.ServiceProvider.GetService<IVideoProvider>(); int N=int.Parse(a[0]), step=int.Parse(a[1]); string od=a[2]; Directory.CreateDirectory(od);
  for(int i=0;i<7279+N;i++){ H.Set(i<L.Length?L[i]:"............"); bool cap=i>=7279 && (i-7279)%step==0; H.core.FrameAdvance(H.ctl,cap,false);
    if(cap){ var b=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){int c=b[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c;} File.WriteAllBytes(od+"/"+(i-7279).ToString("D6")+"_"+w+"x"+h+".rgb",raw);} }
 }}
