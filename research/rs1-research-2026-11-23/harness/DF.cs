using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  for(int i=0;i<7197;i++){ H.Set(L[i]); H.Adv(); }
  while(H.core.Frame<int.Parse(a[0])){ H.Set("............"); H.Adv(); }
  var s=H.Save(); var res=new byte[3][];
  string[] bt={".........A..",".......B....","........X..."};
  for(int k=0;k<3;k++){ H.Load(s); H.Set(bt[k]); H.Adv(); for(int j=0;j<int.Parse(a[1]);j++){ H.Set("............"); H.Adv(); } res[k]=Enumerable.Range(0,0x20000).Select(i=>H.wram.PeekByte(i)).ToArray(); }
  for(int k=1;k<3;k++){ var d=Enumerable.Range(0,0x20000).Where(i=>res[0][i]!=res[k][i]).ToArray(); Console.WriteLine("A vs "+bt[k]+": "+d.Length+" "+string.Join(" ",d.Take(40).Select(i=>i.ToString("X5")+":"+res[0][i].ToString("X2")+"/"+res[k][i].ToString("X2")))); }
 }}
