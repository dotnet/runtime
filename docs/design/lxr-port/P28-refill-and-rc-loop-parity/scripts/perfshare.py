#!/usr/bin/env python3
"""P28: share of selected symbols in one perf recording.
usage: perfshare.py <perf-binary> <perf.data>
Prints, for each symbol of interest, the self share and the inclusive (children) share of all samples, and the same two
numbers as a share of the user samples (samples in the program and in the collector, not in the kernel).
The symbol list is the list of P24 (section 7 and 10) plus the names of the functions that P28 changes."""
import re
import subprocess
import sys

perf, data = sys.argv[1], sys.argv[2]

SYMBOLS = [
    "TryCopyNursery", "DecrementWithDeathCore", "ProcessIncrement", "IsNurseryFast",
    "LxrGcHeap::Alloc", "ImmixAllocationManager::AcquireBlock", "ImmixAllocationManager::IsLineDead",
    "ImmixAllocationManager::TryFindNextHole", "ImmixAllocationManager::PrepareFreshBlock",
    "ImmixAllocationManager::PrepareRecycledHole", "ImmixAllocationManager::InitializeFieldLogRange",
    "SideMetadataManager::ResetRangeQuiescentCore", "SideMetadataManager::ResetRangeQuiescent",
    "ImmixBumpAllocator::TryRefill", "ProcessIncrementWork", "ProcessDecrements", "ProcessDecrementWork",
    "IncrementSaturating", "DecrementNonZeroNonSaturated", "ImmixBlockManager::TryAcquire",
]


def report(children):
    args = [perf, "report", "-i", data, "--stdio", "--children" if children else "--no-children",
            "--sort", "dso,symbol", "-g", "none"]
    out = subprocess.run(args, capture_output=True, text=True).stdout
    rows = []
    for line in out.splitlines():
        if line.startswith("#") or not line.strip():
            continue
        m = re.match(r"\s*([\d.]+)%\s+(?:([\d.]+)%\s+)?(\S+)\s+\[(.)\]\s+(.*)$", line)
        if m:
            rows.append((float(m.group(1)), m.group(3), m.group(5)))
    return rows


self_rows = report(False)
child_rows = report(True)
kernel = sum(p for p, dso, sym in self_rows if dso.startswith("[kernel"))
user = 100.0 - kernel
print("samples: kernel %.1f%%, user %.1f%%" % (kernel, user))
print("%-48s %9s %9s %9s %9s" % ("symbol", "self%all", "incl%all", "self%usr", "incl%usr"))
for name in SYMBOLS:
    s = sum(p for p, dso, sym in self_rows if name in sym)
    c = max([p for p, dso, sym in child_rows if name in sym] or [0.0])
    if s == 0 and c == 0:
        continue
    print("%-48s %9.2f %9.2f %9.2f %9.2f" % (name, s, c, s * 100 / user if user else 0, c * 100 / user if user else 0))
print("top self symbols:")
for p, dso, sym in self_rows[:12]:
    print("  %6.2f%% %s %s" % (p, dso, sym[:90]))
