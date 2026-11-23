using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int p=int.Parse(a[0]),F=int.Parse(a[1]),N=int.Parse(a[2]); string outd=a[3];
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var vp=H.core.ServiceProvider.GetService<IVideoProvider>();
  for(int i=0;i<7198;i++){ H.Set(L[i]); H.Adv(); }
  int pre=int.Parse(a[4]);
  while(H.core.Frame<F-pre){ H.Set(H.core.Frame==p?".........A..":"............"); H.Adv(); }
  var s=H.Save(); bool mine=false; var wv=new List<int>();
  foreach(uint b0 in new uint[]{0x002100,0x802100}){ uint q=b0; H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Write,"b"+q,(x,v,f)=>{ if(mine) return null; wv.Add((int)v); if((v&0x80)==0 && (v&0x0F)<0x0F){ mine=true; H.bus.PokeByte(0x2100,0x0F); mine=false; } return null;},q,null)); }
  foreach(int v in new[]{0,0xFF}){ H.Load(s); Directory.CreateDirectory(outd+"/v"+v);
    for(int k=0;k<N+pre;k++){ if(H.core.Frame==F) H.wram.PokeByte(0x1290,(byte)v); H.bus.PokeByte(0x2100,0x0F); H.Set("............"); H.core.FrameAdvance(H.ctl,true,false);
      var buf=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){ int c=buf[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c; }
      File.WriteAllBytes(outd+"/v"+v+"/"+k.ToString("D4")+"_"+w+"x"+h+".rgb",raw); } Console.WriteLine("W2100 "+string.Join(",",wv.Distinct().Select(x=>x.ToString("X2")))); wv.Clear(); }
 }}
