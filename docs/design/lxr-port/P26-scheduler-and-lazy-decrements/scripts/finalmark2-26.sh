#!/bin/bash
# P26: where the time of the FinalMark of list suffix nursery goes: the whole collection (total_us) against the stop-the-world part.
# usage: finalmark2-26.sh <prefix> <step> ...
P=$1; shift
for s in "$@"; do
  echo "#### $s"
  python3 - /root/p25/results/$P-$s-trace-listsuffix-lxr-nursery.txt <<'EOF'
import json, sys
for line in open(sys.argv[1], errors="replace"):
    if line.startswith("trace_event="):
        e = json.loads(line[len("trace_event="):])
        if e["kind"] == 3 or e["kind"] == 2:
            keys = ["total_us", "lock_wait_us", "policy_drain_us", "pre_drain_us", "stw_pause_us", "post_drain_us"]
            print("kind", e["kind"], {k: e.get(k) for k in keys})
EOF
done
