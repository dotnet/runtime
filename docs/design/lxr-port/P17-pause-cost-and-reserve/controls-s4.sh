#!/bin/bash
# Build the native validation program with each S4 control and run the S4 selector. Each control must make checks fail.
# usage: controls-s4.sh <Release|Debug> [selector]
# native-one.sh compiles the program from the worktree and runs the selectors; EXTRA holds the compiler flags.
CFG=${1:-Release}
SEL=${2:---s4-sweep}
cd /root/p17s4
for c in NO_NURSERY_RELEASE NO_REFRESH EARLY_REUSABLE COUNTER NO_SWEEP_WAIT OLD_TOKEN; do
  echo "######## control LXR_S4_${c}_CONTROL ($CFG)"
  EXTRA="-DLXR_S4_${c}_CONTROL" OUTNAME="ctl-$c" TAILN=400 ./native-one.sh $CFG $SEL 2>&1 | grep -E '^(FAIL|PASS|==)|error|Segmentation|Abort|assert' | sort | uniq -c | sort -rn | head -12
  echo "exit status of the run is in the PASS/FAIL line above (none printed means abnormal end)"
done
