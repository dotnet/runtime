#!/usr/bin/env python3
"""Markdown tables of the P25 benchmark runs.
usage: mk-tables.py <results-dir>
Reads f1-matrix.jsonl, f5-matrix.jsonl, f4-matrix.jsonl and the pause files. P23 numbers are from the task text."""
import json
import re
import statistics
import sys
from pathlib import Path

root = Path(sys.argv[1])
P23 = {  # wall seconds, medians of P23
    ("binarytrees", "wks"): 0.32, ("requests", "wks"): 1.71, ("listsuffix", "wks"): 3.41,
    ("binarytrees", "lxr-nonmoving"): 0.75, ("binarytrees", "lxr-nursery"): 0.86,
    ("requests", "lxr-nonmoving"): 10.57, ("requests", "lxr-nursery"): 4.20,
    ("listsuffix", "lxr-nonmoving"): 21.62, ("listsuffix", "lxr-nursery"): 30.42,
}
TAGS = ["f1", "f5", "f4"]
WL = ["binarytrees", "requests", "listsuffix"]
ARMS = ["wks", "server", "lxr-nonmoving", "lxr-nursery"]


def load(tag):
    rows = [json.loads(l) for l in (root / f"{tag}-matrix.jsonl").read_text().splitlines() if l.strip()]
    out = {}
    for w in WL:
        for a in ARMS:
            ok = [r for r in rows if r["workload"] == w and r["arm"] == a and r["prog"]]
            if not ok:
                continue
            out[(w, a)] = {
                "wall": statistics.median(r["wall_s"] for r in ok),
                "cpu": statistics.median(r["prog"]["cpu_ms"] for r in ok) / 1000,
                "gc": statistics.median(r["prog"]["gc_ms"] for r in ok) / 1000,
                "pauses": statistics.median(r["prog"].get("lxr_pauses", r["prog"].get("gc_count", 0)) for r in ok),
                "full": statistics.median(r["prog"].get("lxr_full", 0) for r in ok),
                "rss": statistics.median(r["prog"]["peak_rss_kb"] for r in ok) / 1024,
                "n": len(ok),
            }
    return out


def pauses(tag):
    out = {}
    cell = None
    for line in (root / f"{tag}-pauses.txt").read_text().splitlines():
        m = re.match(r"== (\S+) (\S+)", line)
        if m:
            cell = (m.group(1), m.group(2))
            continue
        m = re.match(r"\s+(\w+)\s+n=\s*(\d+) median=\s*([\d.]+) ms", line)
        if m and cell:
            out.setdefault(cell, {})[m.group(1)] = (int(m.group(2)), float(m.group(3)))
    return out


data = {t: load(t) for t in TAGS}
pz = {t: pauses(t) for t in TAGS}
print("### Wall time (s, median of 3), with the ratio to WKS of the same session")
print()
print("| program | arm | P23 | P24 baseline (c16710a) | after F1 | after F5 | after F4 (final) | final x WKS | final CPU s | final CPU x WKS |")
print("|---|---|---|---|---|---|---|---|---|---|")
P24 = {("binarytrees", "lxr-nonmoving"): 0.76, ("binarytrees", "lxr-nursery"): 0.82, ("requests", "lxr-nonmoving"): 10.72,
       ("requests", "lxr-nursery"): 4.32, ("listsuffix", "lxr-nonmoving"): 20.39, ("listsuffix", "lxr-nursery"): 28.80,
       ("binarytrees", "wks"): 0.34, ("requests", "wks"): 1.72, ("listsuffix", "wks"): 3.57}
for w in WL:
    for a in ARMS:
        if (w, a) not in data["f4"]:
            continue
        f = data["f4"][(w, a)]
        wks = data["f4"][(w, "wks")]
        cells = [str(P23.get((w, a), "")), str(P24.get((w, a), ""))]
        for t in TAGS:
            cells.append(f"{data[t][(w, a)]['wall']:.2f}")
        print(f"| {w} | {a} | " + " | ".join(cells) + f" | {f['wall'] / wks['wall']:.1f} | {f['cpu']:.2f} | {f['cpu'] / wks['cpu']:.1f} |")
print()
print("### Final build (F4): all columns")
print()
print("| program | arm | wall s | cpu s | gc s | pauses | full | RSS MiB | RefCount pause median ms (traced run) |")
print("|---|---|---|---|---|---|---|---|---|")
for w in WL:
    for a in ARMS:
        if (w, a) not in data["f4"]:
            continue
        f = data["f4"][(w, a)]
        rc = pz["f4"].get((w, a), {}).get("RefCount")
        print(f"| {w} | {a} | {f['wall']:.2f} | {f['cpu']:.2f} | {f['gc']:.2f} | {f['pauses']:.0f} | {f['full']:.0f} | {f['rss']:.0f} | {rc[1] if rc else ''} |")
print()
print("### Number of Full pauses and median RefCount pause (ms), per fix")
print()
print("| program | arm | Full F1 | Full F5 | Full F4 | RefCount ms F1 | RefCount ms F5 | RefCount ms F4 |")
print("|---|---|---|---|---|---|---|---|")
for w in WL:
    for a in ["lxr-nonmoving", "lxr-nursery"]:
        fulls = [f"{data[t][(w, a)]['full']:.0f}" for t in TAGS]
        rcs = [str(pz[t].get((w, a), {}).get("RefCount", ("", ""))[1]) for t in TAGS]
        print(f"| {w} | {a} | " + " | ".join(fulls) + " | " + " | ".join(rcs) + " |")
