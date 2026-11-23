using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static bool done=false; static int hits=0;
 static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc"); int tgt=Convert.ToInt32(a[0],16); string outd=a[1]; Directory.CreateDirectory(outd);
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(done||H.core.Frame<7190) return null; int x=H.RegV("X")&0xFFFF;
    Console.WriteLine("HIT f="+H.core.Frame+" X="+x.ToString("X")+" 129B="+H.wram.PeekByte(0x129B).ToString("X2")+" mem="+string.Join(" ",Enumerable.Range(0,8).Select(i=>H.wram.PeekByte(0x129c+x+i).ToString("X2"))));
    // overwrite current memo pointer
    H.wram.PokeByte(0x129c+x,(byte)(tgt&0xff)); H.wram.PokeByte(0x129d+x,(byte)((tgt>>8)&0xff)); H.wram.PokeByte(0x129e+x,(byte)(tgt>>16));
    done=true; return null;},0x00A0A5,null));
  var reads=new List<string>();
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r2",(ad,v,f)=>{ int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); if(H.core.Frame>=7190&&reads.Count<4000) reads.Add(H.core.Frame+":"+x.ToString("X")+":"+H.Real(ca).ToString("X6")+"="+H.bus.PeekByte(ca).ToString("X2")); return null;},0x00A0A5,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"end",(ad,v,f)=>{ Console.WriteLine("END8D63 f="+H.core.Frame); return null;},0x038D63,null));
  H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"end2",(ad,v,f)=>{ Console.WriteLine("END800F f="+H.core.Frame); return null;},0x03800F,null));
  for(int i=0;i<int.Parse(a[4]);i++){ H.Set(L[i]); H.Adv(); }
  // after first read memo12 drifts; so log what is read
  for(int k=0;k<int.Parse(a[2]);k++){ H.Set("............"); H.core.FrameAdvance(H.ctl,true,false);
    if(k%int.Parse(a[3])==0){ var vp=H.core.ServiceProvider.GetService<IVideoProvider>(); var b=vp.GetVideoBuffer(); int w=vp.BufferWidth,h=vp.BufferHeight; var raw=new byte[w*h*3]; for(int q=0;q<w*h;q++){int c=b[q]; raw[q*3]=(byte)(c>>16); raw[q*3+1]=(byte)(c>>8); raw[q*3+2]=(byte)c;} File.WriteAllBytes(outd+"/"+H.core.Frame.ToString("D6")+"_"+w+"x"+h+".rgb",raw);} }
  File.WriteAllLines(outd+"/reads.txt",reads);
 }}
