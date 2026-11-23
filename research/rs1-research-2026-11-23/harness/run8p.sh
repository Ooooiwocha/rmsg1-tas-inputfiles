#!/bin/bash
# run8p.sh TAG  (TAG=a|b)
T=$1; cd /home/claude/work/BizHawk/BizHawk-2.11.1-linux-x64; export LD_LIBRARY_PATH=$PWD/dll:$PWD MONO_PATH=$PWD/dll
O=/home/claude/work/harness/rb8$T.txt; E=/home/claude/work/harness/rb8$T.err; touch $O
end=$(( $(date +%s) + 6900 ))
while [ $(date +%s) -lt $end ]; do
  left=$(( (end - $(date +%s)) / 60 - 3 )); [ $left -lt 5 ] && break
  mono RB8.exe $O 7278 AX 100000000 /home/claude/work/ck8$T $left >/dev/null 2>>$E
  tail -1 $O | grep -q '^FINISH' && break
  if tail -1 $O | grep -q '^CHECKPOINT' && [ $(date +%s) -ge $(( end - 400 )) ]; then break; fi
  cat /home/claude/work/ck8$T.cur >> /home/claude/work/bad8.txt; echo >> /home/claude/work/bad8.txt; echo "restart $(date +%H:%M) bad=$(cat /home/claude/work/ck8$T.cur)" >> $E
done
tail -1 $O
