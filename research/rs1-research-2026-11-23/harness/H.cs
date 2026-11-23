using System; using System.IO; using System.Linq; using System.Collections.Generic;
using BizHawk.Emulation.Common; using BizHawk.Emulation.Cores; using BizHawk.Emulation.Cores.Nintendo.BSNES;
class Rom : IRomAsset { public byte[] RomData {get;set;} public byte[] FileData {get;set;} public string Extension {get;set;} public string RomPath {get;set;} public GameInfo Game {get;set;} }
class FP : ICoreFileProvider {
  public string GetRetroSaveRAMDirectory(string c)=>"/tmp"; public string GetRetroSystemPath(string c)=>"/tmp"; public string GetUserPath(string s,bool t)=>"/tmp";
  public byte[] GetFirmware(FirmwareID id,string msg=null)=>null;
  public byte[] GetFirmwareOrThrow(FirmwareID id,string msg=null)=>throw new Exception("fw");
  public (byte[] FW, GameInfo Game) GetFirmwareWithGameInfoOrThrow(FirmwareID id,string msg=null)=>throw new Exception("fw"); }
class Ctl : IController { public ControllerDefinition Definition {get;set;} public HashSet<string> P=new HashSet<string>();
  public IReadOnlyCollection<(string Name,int Strength)> GetHapticsSnapshot()=>new List<(string,int)>(); public bool IsPressed(string b)=>P.Contains(b);
  public int AxisValue(string n)=>0; public void SetHapticChannelStrength(string n,int s){} }
static class H {
  static string[] Keys={"P1 Up","P1 Down","P1 Left","P1 Right","P1 Select","P1 Start","P1 Y","P1 B","P1 X","P1 A","P1 L","P1 R"};
  public static BsnesCore core; public static Ctl ctl; public static MemoryDomain wram, bus; public static IStatable st; public static IDebuggable dbg;
  public static void Set(string line){ ctl.P.Clear(); string s=line.Trim(); if(s.StartsWith("|")){ var parts=s.Split('|'); string sys=parts[1]; s=parts[2];
      if(sys.Length>=2){ if(sys[0]!='.') ctl.P.Add("Reset"); if(sys[1]!='.') ctl.P.Add("Power"); } }
    for(int i=0;i<12&&i<s.Length;i++) if(s[i]!='.'&&s[i]!=' ') ctl.P.Add(Keys[i]); }
  public static int RegV(string r)=>(int)dbg.GetCpuFlagsAndRegisters()[r].Value;
  public static byte[] Save(){ var ms=new MemoryStream(); st.SaveStateBinary(new BinaryWriter(ms)); return ms.ToArray(); }
  public static void Load(byte[] s){ st.LoadStateBinary(new BinaryReader(new MemoryStream(s))); }
  public static void Adv(){ core.FrameAdvance(ctl,false,false); }
  public static int Real(int ca)=>((ca&0xFFFF)<0x8000)?((((ca>>16)+1)<<16)|((ca&0xFFFF)|0x8000)):ca;
  public static void Boot(string romp){
    var rom=File.ReadAllBytes(romp);
    var comm=new CoreComm(s=>{},(s,d)=>{},new FP(),CoreComm.CorePreferencesFlags.None,null);
    var ss=new BsnesCore.SnesSyncSettings(); var json=File.ReadAllText("/home/claude/work/bk2_7279/SyncSettings.json");
    var game=new GameInfo{Name="rs1",System="SNES",Hash="x"};
    var p=new CoreLoadParameters<BsnesCore.SnesSettings,BsnesCore.SnesSyncSettings>{Comm=comm,Game=game,Settings=new BsnesCore.SnesSettings(),SyncSettings=ss,DeterministicEmulationRequested=true};
    p.Roms.Add(new Rom{RomData=rom,FileData=rom,Extension=".sfc",RomPath=romp,Game=game});
    core=new BsnesCore(p); ctl=new Ctl{Definition=core.ControllerDefinition};
    var md=core.ServiceProvider.GetService<IMemoryDomains>(); wram=md["WRAM"]; bus=md["System Bus"];
    st=core.ServiceProvider.GetService<IStatable>(); dbg=core.ServiceProvider.GetService<IDebuggable>();
  }
}
