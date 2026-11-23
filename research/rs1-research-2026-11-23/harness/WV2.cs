using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int P=int.Parse(a[0]),F1=int.Parse(a[1]),st=int.Parse(a[2]); string od=a[3]; int v10a6=Convert.ToInt32(a[4],16), reg=Convert.ToInt32(a[5],16); Directory.CreateDirectory(od); var vp=H.core.ServiceProvider.GetService<IVideoProvider>();
  for(int i=0;i<F1;i++){ if(i==P){ if(v10a6>=0) H.wram.PokeByte(0x10A6,(byte)v10a6); if(reg>=0) H.wram.PokeByte(0x1681,(byte)reg);}
    H.Set(i<P+5?L[i]:"............"); bool cap=i>=P&&(i-P)%st==0; H.core.FrameAdvance(H.ctl,cap,false);
    if(cap){ var b=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){int c=b[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c;} File.WriteAllBytes(od+"/"+i.ToString("D6")+"_"+w+"x"+h+".rgb",raw);} }
 }}
