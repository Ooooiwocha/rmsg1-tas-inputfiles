using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  int F0=int.Parse(a[0]), N=int.Parse(a[1]), lo=int.Parse(a[2]), hi=int.Parse(a[3]);
  for(int i=0;i<F0;i++){ H.Set(L[i]); H.Adv(); }
  for(int k=0;k<N;k++){ var s=H.Save(); Console.WriteLine("S "+(F0+k)+" "+string.Join(" ",Enumerable.Range(lo,hi-lo).Select(i=>s[i].ToString("X2")))); H.Set(L[F0+k]); H.Adv(); }
  // also sub-frame: step via smaller? not available
 }}
