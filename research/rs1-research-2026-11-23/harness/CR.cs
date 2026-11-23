using System; using System.IO; using System.Linq; using System.Collections.Generic; using BizHawk.Emulation.Common;
static class M { static void Main(string[] a){ H.Boot("/home/claude/work/rs1.sfc");
  var L=File.ReadAllLines("/home/claude/work/bk2_7279/Input Log.txt").Where(l=>l.StartsWith("|")).ToArray();
  var pr=new Dictionary<int,char>(); foreach(var x in a[0].Split(',')) pr[int.Parse(x.Substring(0,x.Length-1))]=x[x.Length-1];
  int N=int.Parse(a[1]);
  for(int i=0;i<N;i++){ string s="............"; if(i<7197) s=L[i]; else if(pr.ContainsKey(i)){ var ch=s.ToCharArray(); char c=pr[i]; ch[c=='A'?9:c=='X'?8:7]=c; s=new string(ch);} 
    H.Set(s); H.Adv(); if(i%50==0||i>=pr.Keys.Max()) Console.Error.WriteLine("F "+i+" PC="+H.RegV("PC").ToString("X6")); }
  Console.WriteLine("ALIVE "+N);
 }}
