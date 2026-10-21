#!/bin/bash
# P0.3 probe 2, stage 1 - locate the hsqldb stalls in time on both oracles.
#
# MUST NOT run concurrently with any other benchmark: these are latency measurements.
#
# Mirrors P0.1's release-shipped configuration exactly (same JDKs, same shipped .so,
# same heap, same -n 5) and changes only the meter, so the summary line is directly
# comparable with P0.1-benchmarks.md 6.2.
set -uo pipefail
ITERS=${1:-5}
INVOCS=${2:-3}
HEAP=${3:-2000}
STALLMS=${4:-5.0}

DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
OUT=/root/lxr/results/probe2-timeline
mkdir -p "$OUT"
echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

echo "===== probe 2 stage 1 | hsqldb | release | -n $ITERS | heap ${HEAP}M | stall>${STALLMS}ms ====="
for rev in pldi head; do
  JDK=/root/lxr/$rev/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin/java
  pre=""
  [ "$rev" = head ] && pre="MMTK_PLAN=LXR"
  for inv in $(seq 1 "$INVOCS"); do
    log="$OUT/$rev-hsqldb-$HEAP-$inv.log"
    cd /root/lxr/dacapo
    rm -f hs_err_pid*.log core.*
    t0=$(date +%s)
    env $pre timeout -k 10 900 "$JDK" -XX:+UseThirdPartyHeap -server -XX:MetaspaceSize=100M \
        -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=$STALLMS \
        -Dbench.tag="$rev/hsqldb/$HEAP/$inv" -Ddacapo.main=Harness \
        -cp "$DACAPO:/root/lxr/harness-tl/cls-$rev" BenchTL -c TLCallback -n "$ITERS" hsqldb \
        > "$log" 2>&1
    el=$(( $(date +%s) - t0 ))
    st=$(grep -c "PASSED in" "$log")
    echo "-- $rev inv=$inv (${el}s, PASSED=$st)"
    grep -E "^HICCUPTL|^STALLSUM" "$log" | sed 's/^/     /'
  done
done

echo
echo "===== stall counts and totals ====="
grep -h "^STALLSUM" "$OUT"/*.log | sed 's/^/  /'
echo
echo "===== where the stalls fall, relative to iteration boundaries ====="
for f in "$OUT"/*.log; do
  echo "== $(basename "$f")"
  awk '/^MARK/ {printf "   MARK  %10s  %s\n", $3, $4" "$5" "$6}
       /^STALL[^S]/ {printf "   STALL %10s  %s ms\n", $3, $4}' "$f" \
    | sort -k2 -g -s | head -70
done
echo "probe2-timeline done -> $OUT"
