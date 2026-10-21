#!/bin/bash
# Build the native validation program with each S6 control and run the S6 selector. Each control must make checks fail.
# usage: controls-s6.sh <Release|Debug> [selector]
# native-one.sh compiles the program from the worktree (REPO) and runs the selectors; EXTRA holds the compiler flags.
CFG=${1:-Release}
SEL=${2:---s6-registry}
cd ${P17_ROOT:-/root/p17s6}
for c in UNSAFE_APPEND TOUCHED_DROP NO_SUPPRESS MARKED_READY NO_DEDUPE NO_RELEASE NO_RETAIN RC_DECIDE NO_DECIDE NO_TRACE NO_RELOCATE NO_RELOCATE_HELD; do
  echo "######## control LXR_S6_${c}_CONTROL ($CFG)"
  EXTRA="-DLXR_S6_${c}_CONTROL" OUTNAME="ctl6-$c" TAILN=400 ./scripts/native-one.sh $CFG $SEL 2>&1 | grep -E '^(FAIL|PASS|==)|error|Segmentation|Abort|assert' | sort | uniq -c | sort -rn | head -14
  echo "exit status of the run is in the PASS/FAIL line above (none printed means abnormal end)"
done