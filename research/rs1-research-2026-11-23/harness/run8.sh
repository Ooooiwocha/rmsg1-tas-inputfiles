#!/bin/bash
cd /home/claude/work/BizHawk/BizHawk-2.11.1-linux-x64; export LD_LIBRARY_PATH=$PWD/dll:$PWD MONO_PATH=$PWD/dll
end=$(( $(date +%s) + 6900 ))
while [ $(date +%s) -lt $end ]; do
  left=$(( (end - $(date +%s)) / 60 - 3 )); [ $left -lt 5 ] && break
  mono RB8.exe /home/claude/work/harness/rb8.txt 7278 AX 100000000 /home/claude/work/ck8 $left >/dev/null 2>>/home/claude/work/harness/rb8.err
  grep -q '^FINISH' /home/claude/work/harness/rb8.txt && break
  cat /home/claude/work/cur8.txt >> /home/claude/work/bad8.txt; echo >> /home/claude/work/bad8.txt; echo "restart $(date +%H:%M) bad=$(cat /home/claude/work/cur8.txt)" >> /home/claude/work/harness/rb8.err
done
grep -E "CHECKPOINT|FINISH" /home/claude/work/harness/rb8.txt | tail -1
