using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
// RB.exe OUT LASTF buttons(e.g. A or ABX) [maxNodes]
static class ZZ{}
class Nd{public int id; public byte[] st;}
static class M {
  static List<(int f,int b,int a,int m)> reads=new List<(int,int,int,int)>(); static bool log=false; static int endF=-1; static string endK="";
  static int[] KR=new[]{0x129B,0x128C,0x1290,0x12EC,0x12ED,0x12EE,0x12F4,0x12F5,0x12F6,0x12FC,0x12FD,0x12FE,0x1304,0x1305,0x1306,0x1626,0x1627,0x162F,0x1686,0x16BE,0x181C,0x15E4,0x15E5};
  static ulong Key(){ ulong h=1469598103934665603UL; foreach(int i in KR){ h^=H.wram.PeekByte(i); h*=1099511628211UL; } int sp=H.RegV("S")&0xFFFF; h^=(ulong)sp; h*=1099511628211UL; return h; }
  static byte[] Z(byte[] b){ var ms=new MemoryStream(); using(var g=new System.IO.Compression.GZipStream(ms,System.IO.Compression.CompressionLevel.Fastest)) g.Write(b,0,b.Length); return ms.ToArray(); }
  static byte[] U(byte[] b){ var o=new MemoryStream(); using(var g=new System.IO.Compression.GZipStream(new MemoryStream(b),System.IO.Compression.CompressionMode.Decompress)) g.CopyTo(o); return o.ToArray(); }
  static HashSet<string> BAD=new HashSet<string>();
  static string Btn(char c){ var ch="............".ToCharArray(); ch[c=='A'?9:c=='X'?8:7]=c; return new string(ch); }
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    if(File.Exists(a[4]+"/meta.txt")){ var ml=File.ReadAllLines(a[4]+"/meta.txt").FirstOrDefault(x=>x.StartsWith("LEN ")); if(ml!=null){ using(var fs=new FileStream(a[0],FileMode.Open)) fs.SetLength(long.Parse(ml.Substring(4))); } } else if(File.Exists(a[0])) File.Delete(a[0]);
    var w=new StreamWriter(a[0],true){AutoFlush=true}; if(File.Exists("/home/claude/work/bad8.txt")) foreach(var x in File.ReadAllLines("/home/claude/work/bad8.txt")) BAD.Add(x.Trim()); string CK=a[4]; double BUDGET=double.Parse(a[5]); var t0=DateTime.Now; Directory.CreateDirectory(CK); int LASTF=int.Parse(a[1]); string BT=a[2]; int maxN=int.Parse(a[3]);
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"r",(ad,v,f)=>{ if(!log) return null; int x=H.RegV("X")&0xFFFF; int ca=(H.wram.PeekByte(0x129e+x)<<16)|(H.wram.PeekByte(0x129d+x)<<8)|H.wram.PeekByte(0x129c+x); reads.Add(((int)H.core.Frame,H.bus.PeekByte(ca),H.Real(ca),x/8+1)); return null;},0x00A0A5,null));
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e1",(ad,v,f)=>{ if(log&&endF<0){endF=(int)H.core.Frame;endK="800F";} return null;},0x03800F,null));
    H.dbg.MemoryCallbacks.Add(new MemoryCallback("System Bus",MemoryCallbackType.Execute,"e2",(ad,v,f)=>{ if(log&&endF<0){endF=(int)H.core.Frame;endK="800C";} return null;},0x03800C,null));
    // prefix
    for(int i=0;i<7197;i++){ H.Set(L[i]); H.Adv(); }
    // first run after the fixed first press: run to stop
    var queue=new SortedDictionary<int,List<Nd>>(); var seen=new HashSet<(int,ulong)>(); int nid=0;
    Func<int,int,int[]> RunToStop=(parent,startFrame)=>{ // after press already applied; returns stop
      int last=reads.Count>0?reads[reads.Count-1].f:startFrame; 
      while((int)H.core.Frame<=LASTF){ if(endF>=0) return new[]{2,endF};
        if(reads.Count>0){ var lr=reads[reads.Count-1]; if((int)H.core.Frame-lr.f>=2) return new[]{1,(int)H.core.Frame}; } else if((int)H.core.Frame-startFrame>=6) return new[]{3,(int)H.core.Frame};
        H.Set("............"); H.Adv(); }
      return new[]{0,(int)H.core.Frame}; };
    bool resumed=File.Exists(CK+"/meta.txt");
    if(resumed){ foreach(var ln in File.ReadAllLines(CK+"/meta.txt")){ var p=ln.Split(' '); if(p[0]=="NID") nid=int.Parse(p[1]); else if(p[0]=="Q"){ int fr=int.Parse(p[1]), id=int.Parse(p[2]); if(!queue.ContainsKey(fr)) queue[fr]=new List<Nd>(); queue[fr].Add(new Nd{id=id,st=File.ReadAllBytes(CK+"/n"+id+".bin")}); } }
      foreach(var ln in File.ReadAllLines(CK+"/seen.txt")){ var p=ln.Split(' '); seen.Add((int.Parse(p[0]),ulong.Parse(p[1]))); }
      w.WriteLine("RESUME nid="+nid+" queued="+queue.Values.Sum(x=>x.Count)); }
    else {
    log=true; reads.Clear(); endF=-1;
    { var r=RunToStop(-1,7197); var lr=reads.Count>0?reads[reads.Count-1]:(f:0,b:0,a:0,m:0);
      if(r[0]==1){ ulong k=Key(); seen.Add((r[1],k)); int id=nid++; w.WriteLine("NODE "+id+" parent=-1 q=-1 btn=- f="+r[1]+" addr="+lr.a.ToString("X6")+" m="+lr.m+" b="+lr.b.ToString("X2")+" key="+k.ToString("X16")); if(!queue.ContainsKey(r[1])) queue[r[1]]=new List<Nd>(); queue[r[1]].Add(new Nd{id=id,st=Z(H.Save())}); } else w.WriteLine("NOSTOP "+r[0]+" "+r[1]); }
    }
    Action<int> Ckpt=(processedN)=>{ foreach(var f in Directory.GetFiles(CK)) File.Delete(f);
      var meta=new List<string>{"NID "+nid}; foreach(var kv in queue) foreach(var n in kv.Value){ File.WriteAllBytes(CK+"/n"+n.id+".bin",n.st); meta.Add("Q "+kv.Key+" "+n.id); }
      File.WriteAllLines(CK+"/seen.txt",seen.Select(x=>x.Item1+" "+x.Item2)); File.WriteAllLines(CK+"/meta.txt",meta); w.WriteLine("CHECKPOINT processed="+processedN+" nodes="+nid+" queued="+meta.Count); w.Flush(); meta.Insert(0,"LEN "+w.BaseStream.Length); File.WriteAllLines(CK+"/meta.txt",meta); };
    var lastCk=DateTime.Now;
    int processed=0;
    while(queue.Count>0 && processed<maxN && (DateTime.Now-t0).TotalMinutes<BUDGET){ int f0=queue.Keys.First(); var lst=queue[f0]; var nd=lst[lst.Count-1]; lst.RemoveAt(lst.Count-1); if(lst.Count==0) queue.Remove(f0); processed++;
      H.Load(U(nd.st)); log=false;
      for(int q=f0;q<=LASTF;q++){ var sq=H.Save();
        foreach(char c in BT){ string bk=nd.id+" "+q+" "+c; if(BAD.Contains(bk)){ w.WriteLine("CRASHBRANCH "+bk); continue; } File.WriteAllText(CK+".cur",bk); H.Load(sq); log=true; reads.Clear(); endF=-1; H.Set(Btn(c)); H.Adv();
          var r=RunToStop(nd.id,q+1); log=false;
          if(r[0]==2){ w.WriteLine("END parent="+nd.id+" q="+q+" btn="+c+" f="+r[1]+" "+endK); continue; }
          if(r[0]==1){ var lr=reads[reads.Count-1]; ulong k=((ulong)lr.a*64+(ulong)lr.m)*256+(ulong)H.wram.PeekByte(0x129B); foreach(int ad in new[]{0x181C,0x1627,0x162F,0x128C}){ k=k*1099511628211UL ^ (ulong)H.wram.PeekByte(ad); } if(seen.Add((r[1],k))){ int id=nid++; w.WriteLine("NODE "+id+" parent="+nd.id+" q="+q+" btn="+c+" f="+r[1]+" addr="+lr.a.ToString("X6")+" m="+lr.m+" b="+lr.b.ToString("X2")+" key="+k.ToString("X16")); if(r[1]<=LASTF){ if(!queue.ContainsKey(r[1])) queue[r[1]]=new List<Nd>(); queue[r[1]].Add(new Nd{id=id,st=Z(H.Save())}); } } }
        }
        H.Load(sq); H.Set("............"); H.Adv(); }
      if((DateTime.Now-lastCk).TotalMinutes>5){ Ckpt(processed); lastCk=DateTime.Now; }
      w.WriteLine("DONE "+nd.id+" f0="+f0+" queued="+queue.Values.Sum(x=>x.Count)+" nodes="+nid);
    }
    if(queue.Count>0){ Ckpt(processed); }
    else { if(File.Exists(CK+"/meta.txt")) File.Delete(CK+"/meta.txt"); w.WriteLine("FINISH processed="+processed+" nodes="+nid); }
  }}
