#!/bin/bash
# Build the native validation program with each S5b control and run the S5b selector. Each control must make checks fail.
# usage: controls-s5b.sh <Release|Debug> [selector]
# native-one.sh compiles the program from the worktree and runs the selectors; EXTRA holds the compiler flags.
CFG=${1:-Release}
SEL=${2:---s5b-overshoot}
cd /root/p17s5b
for c in NO_BORROW NO_GIVEBACK ALWAYS_RETURN NO_ALLOC_SHRINK NO_PLANE_DECOMMIT NO_DATA_DECOMMIT KEEP_RANGE_OWNED \
    IGNORE_HARD_LIMIT IGNORE_PLANE_LIMIT RESERVE_TRIGGER NO_PLACEMENT NO_STALE_TOLERANCE; do
  echo "######## control LXR_S5B_${c}_CONTROL ($CFG)"
  EXTRA="-DLXR_S5B_${c}_CONTROL" OUTNAME="ctl-$c" TAILN=400 ./native-one.sh $CFG $SEL 2>&1 | grep -E '^(FAIL|PASS|==)|error|Segmentation|Abort|assert' | sort | uniq -c | sort -rn | head -12
  echo "exit status of the run is in the PASS/FAIL line above (none printed means abnormal end)"
done
