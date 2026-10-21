#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Share of the checked side-metadata functions in perf recordings (output of perf-report.sh).

usage: perf-shares.py <label=prefix> ...   where <prefix>.self.txt and <prefix>.incl.txt exist
Prints one markdown row per label: self share of Load, Store and GetLocation (with GetLocationUnchecked), the sum of all
checked point accesses, the inclusive share of Load, the share of the range functions, and the share of the inline
accessor functions that were not inlined.
"""

import re
import sys

POINT = ("Load", "Store", "GetLocation", "GetLocationUnchecked", "IsDataAddressOwned", "IsSpecEnabled", "Update",
         "CompareExchange", "FetchOr", "FetchAnd", "FetchAddWrapping", "IncrementSaturating", "DecrementNonZeroNonSaturated")
RANGE = ("VisitWords", "IsRangeZero", "ResetRangeQuiescent", "ResetRangeQuiescentCore")


def rows(path):
    for line in open(path, encoding="utf-8", errors="replace"):
        parts = line.split()
        if not parts or not parts[0].endswith("%"):
            continue
        yield parts


def self_shares(path):
    shares = {}
    for parts in rows(path):
        match = re.search(r"(SideMetadataManager|SideMetadataAccessor)::(\w+)", " ".join(parts[1:]))
        if match:
            shares[match.group(2)] = shares.get(match.group(2), 0.0) + float(parts[0].rstrip("%"))
    return shares


def inclusive_load(path):
    total = 0.0
    for parts in rows(path):
        text = " ".join(parts)
        if "SideMetadataManager::Load" in text:
            total += float(parts[0].rstrip("%"))
    return total


def main():
    print("| profile | Load self | Store self | GetLocation self | all checked point accesses | Load inclusive | range functions | inline accessor symbols |")
    print("|---|---|---|---|---|---|---|---|")
    for item in sys.argv[1:]:
        label, prefix = item.split("=", 1)
        shares = self_shares(prefix + ".self.txt")
        point = sum(shares.get(name, 0.0) for name in POINT)
        span = sum(shares.get(name, 0.0) for name in RANGE)
        inline = sum(value for name, value in shares.items() if name not in POINT and name not in RANGE)
        location = shares.get("GetLocation", 0.0) + shares.get("GetLocationUnchecked", 0.0)
        print(f"| {label} | {shares.get('Load', 0.0):.1f}% | {shares.get('Store', 0.0):.1f}% | {location:.1f}% | "
              f"{point:.1f}% | {inclusive_load(prefix + '.incl.txt'):.1f}% | {span:.1f}% | {inline:.1f}% |")


if __name__ == "__main__":
    main()
