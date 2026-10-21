#!/usr/bin/env python3
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Print one line per run from a results .jsonl file."""

import json
import sys

for line in open(sys.argv[1], encoding="utf-8"):
    r = json.loads(line)
    if r.get("status") == "failed":
        print(f"{r['workload']:11} {r['collector']:18} x{r.get('factor')} FAILED exit={r['exit']} {r['stderr_tail'][-120:]!r}")
        continue
    extra = {k[4:]: r[k] for k in r if k.startswith("lxr_")}
    print(f"{r['workload']:11} {r['collector']:18} x{r.get('factor')} {r['elapsed_ms']:9.0f}ms cpu {r['cpu_ms']:9.0f} "
          f"gcs {r['gc_count']:4} gc_ms {r['gc_ms']:6} hic99 {r['hiccup_p99_us']:9.0f} max {r['hiccup_max_us']:10.0f} "
          f"req99 {r['req_p99_us']:8.0f} rss {r['peak_rss_kb'] // 1024:5}MB {extra if extra else ''}")
