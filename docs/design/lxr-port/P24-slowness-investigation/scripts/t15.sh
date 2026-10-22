#!/bin/bash
R=/root/p24/results
/root/p24/scripts/trace3.sh listsuffix lxr-nursery $R/trace3-listsuffix-nursery-batch.txt GCBENCH_LATENCY=batch
/root/p24/scripts/trace3.sh listsuffix lxr-nonmoving $R/trace3-listsuffix-nonmoving-batch.txt GCBENCH_LATENCY=batch
/root/p24/scripts/trace3.sh requests lxr-nursery $R/trace3-requests-nursery-batch.txt GCBENCH_LATENCY=batch
