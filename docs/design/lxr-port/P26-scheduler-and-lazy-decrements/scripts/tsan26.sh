#!/bin/bash
# P26: ThreadSanitizer run of the scheduler suites (they do not need the big address reservation of the engine).
# usage: tsan26.sh        (uses the tree in /root/p25/main; ASLR is switched off for the run because of the TSan shadow map)
R=/root/p25/main
GC=$R/src/coreclr/gc
OUT=/root/p25/out/tsan26
mkdir -p $OUT
FLAGS="-std=c++17 -pthread -O1 -g -fsanitize=thread -DLXR_ENABLE_LAZY_COMPLETION_COUNTER -I$GC"
for t in P3.1/lxr-work-packet-validation P3.3/lxr-deferred-bucket-validation P3.4/lxr-completion-counter-validation; do
  name=$(basename $t)
  clang++ $FLAGS $GC/lxr_completion_counter.cpp $GC/lxr_work_packet.cpp $GC/lxr_work_scheduler.cpp \
    $R/docs/design/lxr-port/$t.cpp -o $OUT/$name 2> $OUT/$name.build || { head -5 $OUT/$name.build; continue; }
  setarch x86_64 -R $OUT/$name > $OUT/$name.run 2>&1
  echo "$name exit=$? tsan_reports=$(grep -c 'WARNING: ThreadSanitizer' $OUT/$name.run) :: $(grep -E '^(PASS|FAIL):' $OUT/$name.run | tail -1)"
done
