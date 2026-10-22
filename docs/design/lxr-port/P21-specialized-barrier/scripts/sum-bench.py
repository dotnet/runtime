#!/usr/bin/env python3
# P21 (D1): summarize the lean benchmark (wall.jsonl): per workload and mode, mean elapsed ms and checksum per build.
import json, sys, collections
rows = [json.loads(l) for l in open(sys.argv[1])]
groups = collections.defaultdict(list)
for r in rows:
    groups[(r["workload"], r["mode"], r["build"])].append(r)
print(f"{'workload':12} {'mode':10} {'build':4} {'elapsed_ms (2 reps)':26} {'mean':>8} {'cpu_ms':>8} {'gc_ms':>7} {'pauses':>6} {'p99_us':>8} {'rss_mb':>7} checksum")
for key in sorted(groups):
    g = groups[key]
    el = [x["elapsed_ms"] for x in g]
    cs = {x["checksum"] for x in g}
    print(f"{key[0]:12} {key[1]:10} {key[2]:4} {', '.join('%.0f' % e for e in el):26} {sum(el)/len(el):8.0f} "
          f"{sum(x['cpu_ms'] for x in g)/len(g):8.0f} {sum(x['gc_ms'] for x in g)/len(g):7.0f} "
          f"{sum(x.get('lxr_pauses', 0) for x in g)/len(g):6.0f} {sum(x['hiccup_p99_us'] for x in g)/len(g):8.0f} "
          f"{sum(x['peak_rss_kb'] for x in g)/len(g)/1024:7.0f} {','.join(str(c) for c in cs)}")
# checksum equality across builds
by = collections.defaultdict(set)
for r in rows:
    by[(r["workload"], r["mode"])].add(r["checksum"])
print("checksums equal across builds and modes:", all(len(v) == 1 for v in by.values()))
