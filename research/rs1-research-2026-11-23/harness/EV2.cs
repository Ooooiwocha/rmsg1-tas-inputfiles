using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var pr=a[0].Split(',').Select(x=>(int.Parse(x.Substring(0,x.Length-1)),x[x.Length-1])).ToDictionary(t=>t.Item1,t=>t.Item2);
  int F0=int.Parse(a[1]),F1=int.Parse(a[2]),st=int.Parse(a[3]); string od=a[4]; Directory.CreateDirectory(od); var vp=H.core.ServiceProvider.GetService<IVideoProvider>();

  for(int i=0;i<F1;i++){ string inp="............"; if(i<7198) inp=L[i]; else if(pr.ContainsKey(i)){ var ch=inp.ToCharArray(); ch[pr[i]=='A'?9:pr[i]=='X'?8:7]=pr[i]; inp=new string(ch);} 
    if(i>=7190) H.bus.PokeByte(0x2100,0x0F);
    H.Set(inp); bool cap=i>=F0&&(i-F0)%st==0; H.core.FrameAdvance(H.ctl,cap,false);
    if(cap){ var b=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){int c=b[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c;} File.WriteAllBytes(od+"/"+i.ToString("D6")+"_"+w+"x"+h+".rgb",raw);} }
 }}
