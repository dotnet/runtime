#!/bin/bash
PERF=/usr/lib/linux-tools/5.15.0-198-generic/perf
D=/root/p24/results/perf/${1:-ls-nursery-k}.data
echo "== callers of the kernel spin-unlock symbol (user-space part of the chain)"
$PERF report -i $D --stdio --no-children -S _raw_spin_unlock_irqrestore -g caller,0.5,callee --max-stack 12 2>/dev/null | grep -v '^#' | grep -v '^$' | head -60 | cut -c1-140
echo
echo "== callers of finish_task_switch"
$PERF report -i $D --stdio --no-children -S finish_task_switch.isra.0 -g caller,0.5,callee --max-stack 12 2>/dev/null | grep -v '^#' | grep -v '^$' | head -40 | cut -c1-140
