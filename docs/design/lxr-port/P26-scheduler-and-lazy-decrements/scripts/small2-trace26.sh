#!/bin/bash
# P26: why a trace limit of 1 on 2 processors gives more pauses: traced runs of list suffix nonmoving, pinned to 2 processors.
F=/root/p25/frames/p26-final-Release-linux
for c in 1 2; do
  taskset -c 0,1 /root/p25/scripts/trace1.sh $F listsuffix lxr-nonmoving /root/p25/results/s26-small2-trace-conc$c.txt DOTNET_LXRConcGCThreads=$c > /dev/null 2>&1
  echo "== conc=$c"
  python3 /root/p25/scripts/pausemed.py /root/p25/results/s26-small2-trace-conc$c.txt
  python3 - /root/p25/results/s26-small2-trace-conc$c.txt <<'EOF'
import json, sys, collections
ev = [json.loads(l[len("trace_event="):]) for l in open(sys.argv[1], errors="replace") if l.startswith("trace_event=")]
c = collections.Counter((e["kind"], e.get("cause")) for e in ev)
print("kind,cause counts:", dict(c))
print("summary:", [l.strip()[:230] for l in open(sys.argv[1], errors="replace") if l.startswith('{"runtime"')])
EOF
done
