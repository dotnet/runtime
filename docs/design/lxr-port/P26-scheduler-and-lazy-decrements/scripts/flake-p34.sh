#!/bin/bash
# P26: flake rate of the P3.4 completion-counter validation. Builds the old test source (from a file) and the new test source
# against the scheduler in /root/p25/main and runs each N times.
# usage: flake-p34.sh <old-test.cpp> <new-test.cpp> <runs>
OLD=$1; NEW=$2; N=$3
R=/root/p25/main
GC=$R/src/coreclr/gc
OUT=/root/p25/out/flake
mkdir -p $OUT
for v in old new; do
  src=$OLD; [ $v = new ] && src=$NEW
  clang++ -std=c++17 -pthread -O2 -Wall -Wextra -Werror -DLXR_ENABLE_LAZY_COMPLETION_COUNTER -I$GC \
    $GC/lxr_completion_counter.cpp $GC/lxr_work_packet.cpp $GC/lxr_work_scheduler.cpp $src -o $OUT/p34-$v 2> $OUT/p34-$v.build || { cat $OUT/p34-$v.build | head; exit 2; }
  fails=0
  : > $OUT/p34-$v.fails
  for i in $(seq 1 $N); do
    $OUT/p34-$v > $OUT/p34-$v.run 2>&1 || { fails=$((fails + 1)); grep FAIL $OUT/p34-$v.run | head -2 >> $OUT/p34-$v.fails; }
  done
  echo "$v test: $fails failing runs of $N"
  sort $OUT/p34-$v.fails | uniq -c
done
