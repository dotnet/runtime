#!/bin/bash
# Research-only: print the collection kinds recorded by each profiled run.
for n in binarytrees listsuffix-rc listsuffix-35-5-fullheavy requests; do
  echo "$n: $(grep '^{' /root/gcbench/results/p15-perf/$n.run.txt | grep -o -E '"elapsed_ms":[0-9.]+|"gc_count":[0-9]+|"gc_ms":[0-9]+|"lxr_initial_mark":[0-9]+|"lxr_final_mark":[0-9]+|"lxr_full":[0-9]+' | tr '\n' ' ')"
done
