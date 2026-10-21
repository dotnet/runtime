#!/bin/bash
d=/root/lxr/results/flogs-fastdebug-shipped
echo "--- log dir ---"
ls "$d" 2>/dev/null | head -20
echo "--- files containing assert/panic/guarantee ---"
grep -lEi 'assert|panicked|guarantee\(' "$d"/*.log 2>/dev/null | head -20
echo "--- crash signature per failing benchmark ---"
for b in lusearch eclipse xalan; do
  for f in "$d"/*"$b"*.log; do
    [ -f "$f" ] || continue
    echo "== $f"
    grep -m3 -E 'SIGSEGV|SIGBUS|Internal Error|assert|panicked|fatal error' "$f"
  done
done
