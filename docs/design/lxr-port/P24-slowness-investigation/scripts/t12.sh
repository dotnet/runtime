#!/bin/bash
# Traced runs for the other cells (S0 phase timers).
R=/root/p24/results
/root/p24/scripts/trace1.sh binarytrees lxr-nonmoving $R/trace-binarytrees-nonmoving-r1.txt > /dev/null
/root/p24/scripts/trace1.sh binarytrees lxr-nursery $R/trace-binarytrees-nursery-r1.txt > /dev/null
/root/p24/scripts/trace1.sh requests lxr-nursery $R/trace-requests-nursery-r1.txt > /dev/null
/root/p24/scripts/trace1.sh requests lxr-defrag $R/trace-requests-defrag-r1.txt > /dev/null
EXTRA="mature_copied=96,mature_sel=92,mature_elig=91,mature_fail=97,mature_remset=98"
FRAME=/root/p24/frames/p24-Release-linux /root/p24/scripts/run1.sh /root/p24/frames/p24-Release-linux /root/gcbench/trace19/gctrace.dll requests lxr-defrag GCTRACE=1 GCTRACE_EXTRA="$EXTRA,blocks_rel=7,free_pages=248,reserve_pages=247,young_bytes=86,cause=43" > $R/trace-requests-defrag-r2.txt 2>&1
for f in binarytrees-nonmoving binarytrees-nursery requests-nursery requests-defrag requests-defrag-r2; do tail -2 $R/trace-$f-r1.txt 2>/dev/null | head -1 | cut -c1-250; done
