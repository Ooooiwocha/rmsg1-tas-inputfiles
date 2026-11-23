using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
// RB.exe OUT LASTF buttons(e.g. A or ABX) [maxNodes]
class Nd{public int id; public byte[] st;}
static class M {
  static List<(int f,int b,int a,int m)> reads=new List<(int,int,int,int)>(); static bool log=false; static int endF=-1; static string endK="";
  static int[] KR=new[]{0x129B,0x128C,0x1290,0x12EC,0x12ED,0x12EE,0x12F4,0x12F5,0x12F6,0x12FC,0x12FD,0x12FE,0x1304,0x1305,0x1306,0x1626,0x1627,0x162F,0x1686,0x16BE,0x181C,0x15E4,0x15E5};
  static ulong Key(){ ulong h=1469598103934665603UL; foreach(int i in KR){ h^=H.wram.PeekByte(i); h*=1099511628211UL; } int sp=H.RegV("S")&0xFFFF; h^=(ulong)sp; h*=1099511628211UL; return h; }
  static string Btn(char c){ var ch="............".ToCharArray(); ch[c=='A'?9:c=='X'?8:7]=c; return new string(ch); }
  static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
    var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
    var w=new StreamWriter(a[0]){AutoFlush=true}; int LASTF=int.Parse(a[1]); string BT=a[2]; int maxN=a.Length>3?int.Parse(a[3]):1000000;
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
        if(reads.Count>0){ var lr=reads[reads.Count-1]; if(lr.b==0x12 && (int)H.core.Frame-lr.f>=2) return new[]{1,(int)H.core.Frame}; }
        H.Set("............"); H.Adv(); }
      return new[]{0,(int)H.core.Frame}; };
    log=true; reads.Clear(); endF=-1;
    { var r=RunToStop(-1,7197); var lr=reads.Count>0?reads[reads.Count-1]:(f:0,b:0,a:0,m:0);
      if(r[0]==1){ ulong k=Key(); seen.Add((r[1],k)); int id=nid++; w.WriteLine("NODE "+id+" parent=-1 q=-1 btn=- f="+r[1]+" addr="+lr.a.ToString("X6")+" m="+lr.m+" key="+k.ToString("X16")+" kb="+string.Join("",KR.Select(i=>H.wram.PeekByte(i).ToString("X2")))+"S"+(H.RegV("S")&0xFFFF).ToString("X4")); if(!queue.ContainsKey(r[1])) queue[r[1]]=new List<Nd>(); queue[r[1]].Add(new Nd{id=id,st=H.Save()}); } else w.WriteLine("NOSTOP "+r[0]+" "+r[1]); }
    int processed=0;
    while(queue.Count>0 && processed<maxN){ int f0=queue.Keys.First(); var lst=queue[f0]; var nd=lst[lst.Count-1]; lst.RemoveAt(lst.Count-1); if(lst.Count==0) queue.Remove(f0); processed++;
      H.Load(nd.st); log=false;
      for(int q=f0;q<=LASTF;q++){ var sq=H.Save();
        foreach(char c in BT){ H.Load(sq); log=true; reads.Clear(); endF=-1; H.Set(Btn(c)); H.Adv();
          var r=RunToStop(nd.id,q+1); log=false;
          if(r[0]==2){ w.WriteLine("END parent="+nd.id+" q="+q+" btn="+c+" f="+r[1]+" "+endK); continue; }
          if(r[0]==1){ var lr=reads[reads.Count-1]; ulong k=Key(); if(seen.Add((r[1],k))){ int id=nid++; w.WriteLine("NODE "+id+" parent="+nd.id+" q="+q+" btn="+c+" f="+r[1]+" addr="+lr.a.ToString("X6")+" m="+lr.m+" key="+k.ToString("X16")+" kb="+string.Join("",KR.Select(i=>H.wram.PeekByte(i).ToString("X2")))+"S"+(H.RegV("S")&0xFFFF).ToString("X4")); if(r[1]<=LASTF){ if(!queue.ContainsKey(r[1])) queue[r[1]]=new List<Nd>(); queue[r[1]].Add(new Nd{id=id,st=H.Save()}); } } }
        }
        H.Load(sq); H.Set("............"); H.Adv(); }
      w.WriteLine("DONE "+nd.id+" f0="+f0+" queued="+queue.Values.Sum(x=>x.Count)+" nodes="+nid);
    }
    w.WriteLine("FINISH processed="+processed+" nodes="+nid);
  }}
