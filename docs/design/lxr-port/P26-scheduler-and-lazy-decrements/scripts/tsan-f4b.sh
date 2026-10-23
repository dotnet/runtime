#!/bin/bash
# P26: ThreadSanitizer run of the F4b selector (stealing, stress, closed-stage packets). The trace-loop test needs the engine,
# whose large address reservation does not fit the TSan shadow map; its check fails under TSan for that reason only.
cd /root/p25
EXTRA='-fsanitize=thread -g' OUTNAME=tsan ./scripts/one.sh Release --f4b-scheduler > /dev/null 2>&1
cd out/one-Release-tsan
setarch x86_64 -R ./lxr-rc-validation --f4b-scheduler > run3.log 2>&1
echo "tsan_reports=$(grep -c 'WARNING: ThreadSanitizer' run3.log)"
grep -E 'FAIL|f4b_|PASS' run3.log
