#!/bin/bash
# P25: build the P3.4 validation with the scheduler sources of another revision and run it N times.
# usage: baseline-p34.sh <dir-with-lxr_work_scheduler.{h,cpp}> <runs>
B=$1; N=$2
R=/root/p25/main
mkdir -p /root/p25/base
cp $R/src/coreclr/gc/lxr_completion_counter.* $R/src/coreclr/gc/lxr_work_packet.* /root/p25/base/
tr -d '\r' < $B/lxr_work_scheduler.cpp > /root/p25/base/lxr_work_scheduler.cpp
tr -d '\r' < $B/lxr_work_scheduler.h > /root/p25/base/lxr_work_scheduler.h
clang++ -std=c++17 -pthread -O2 -Wall -Wextra -Werror -DLXR_ENABLE_LAZY_COMPLETION_COUNTER \
  /root/p25/base/lxr_completion_counter.cpp /root/p25/base/lxr_work_packet.cpp /root/p25/base/lxr_work_scheduler.cpp \
  $R/docs/design/lxr-port/P3.4/lxr-completion-counter-validation.cpp -o /root/p25/base/p34 || exit 1
fails=0
for i in $(seq 1 $N); do
  /root/p25/base/p34 > /tmp/b34.log 2>&1 || fails=$((fails + 1))
done
echo "P3.4 with the scheduler of $B: $fails failures in $N runs"
