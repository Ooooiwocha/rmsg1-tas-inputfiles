using System; using System.IO; using System.Linq; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var fs=a[0].Split(',').Select(int.Parse).ToArray(); int k=0;
  for(int i=0;i<=fs.Max();i++){ H.Set(L[i]); H.Adv(); if(fs.Contains(i)){ Console.WriteLine(i+" 111C-111F="+string.Join(" ",Enumerable.Range(0x111C,4).Select(x=>H.wram.PeekByte(x).ToString("X2")))+" 15E0-15E5="+string.Join(" ",Enumerable.Range(0x109D,2).Select(x=>H.wram.PeekByte(x).ToString("X2")))); } }
 }}
