#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Shares of the sweep-related functions in perf recordings (output of perf-report.sh), and the top self functions.

usage: perf-s4.py <label=prefix> ...   where <prefix>.self.txt and <prefix>.incl.txt exist
For each label: the five largest self-time symbols, and the self and inclusive share (percent of all samples) of the
functions that S4 removes, moves or adds.
"""
import re
import sys

FUNCTIONS = (
    "LxrReferenceCounting::Sweep", "LxrReferenceCounting::SweepCycles", "LxrReferenceCounting::GetHeapStatistics",
    "LxrReferenceCounting::ReleaseEpoch", "LxrReferenceCounting::ReleaseNurseryBlocks",
    "LxrReferenceCounting::RefreshBlock", "LxrReferenceCounting::ReleaseBlockIfDead",
    "ImmixBlockManager::TryReleaseIfRcDead", "ImmixBlockManager::TryReleaseNursery",
    "ImmixBlockManager::TrySetReusableLazily", "ImmixBlockManager::SetReusable",
    "ImmixAllocationManager::TryFindNextHole", "LxrReferenceCounting::GetMaturePages",
)


def rows(path):
    for line in open(path, encoding="utf-8", errors="replace"):
        parts = line.split(None, 2)
        if len(parts) >= 2 and parts[0].endswith("%"):
            yield float(parts[0].rstrip("%")), line.strip()


def share(path, name):
    total = 0.0
    for value, text in rows(path):
        if re.search(re.escape(name) + r"\b", text):
            total += value
    return total


def main():
    for item in sys.argv[1:]:
        label, prefix = item.split("=", 1)
        print(f"### {label}")
        print("top self:", "; ".join(re.sub(r"\s+", " ", text)[:90] for _, text in list(rows(prefix + ".self.txt"))[:5]))
        print("| function | self % | inclusive % |")
        print("|---|---|---|")
        for name in FUNCTIONS:
            s, i = share(prefix + ".self.txt", name), share(prefix + ".incl.txt", name)
            if s or i:
                print(f"| `{name}` | {s:.2f} | {i:.2f} |")
        print()


if __name__ == "__main__":
    main()
