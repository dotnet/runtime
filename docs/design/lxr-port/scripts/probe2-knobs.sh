#!/bin/bash
# P0.3 probe 2, stage 3 - runtime-knob sweep on the SHIPPED lxr-head oracle.
#
# Stage 2 falsified LAZY_MU_REUSE_BLOCK_SWEEPING as the cause of head's hsqldb pauses.
# Reading both args.rs files then turned up a verified default inversion:
#
#   PLDI  df8d30a src/args.rs:169  MAX_SURVIVAL_MB: Lazy<Option<usize>>  -> None unless set
#   head  304ce69 src/args.rs:56   max_survival_mb: env_arg(...).unwrap_or(128)
#
# hsqldb has the largest live set in DaCapo 2006 (~117 MB by MarkCompact minheap),
# which sits right at head's 128 MB default.  These knobs are read from the environment
# at both oracles, so this needs no rebuild and perturbs nothing else.
set -uo pipefail
ITERS=${1:-5}
INVOCS=${2:-2}
HEAP=${3:-2000}

DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
JDK=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin/java
OUT=/root/lxr/results/probe2-knobs
mkdir -p "$OUT"
echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0

run_cfg () { # $1=label  $2...=env assignments
  local label=$1; shift
  for inv in $(seq 1 "$INVOCS"); do
    local log="$OUT/head-$label-$inv.log"
    cd /root/lxr/dacapo
    rm -f hs_err_pid*.log core.*
    env MMTK_PLAN=LXR "$@" timeout -k 10 900 "$JDK" -XX:+UseThirdPartyHeap -server \
        -XX:MetaspaceSize=100M -Xms${HEAP}M -Xmx${HEAP}M -Dstall.ms=5.0 \
        -Dbench.tag="head-$label/hsqldb/$HEAP/$inv" -Ddacapo.main=Harness \
        -cp "$DACAPO:/root/lxr/harness-tl/cls-head" BenchTL -c TLCallback -n "$ITERS" hsqldb \
        > "$log" 2>&1
    printf '  %-28s inv=%s  ' "$label" "$inv"
    grep -h "^STALLSUM" "$log" | sed -E 's/^STALLSUM [^ ]+ //'
    grep -h "^HICCUPTL" "$log" | sed -E 's/^HICCUPTL [^ ]+ /     /'
  done
}

echo "===== head oracle, hsqldb @${HEAP}M, -n $ITERS, runtime-knob sweep ====="
run_cfg baseline
run_cfg max_survival_1M       MAX_SURVIVAL_MB=1048576
run_cfg rc_stop_100           RC_STOP_PERCENT=100
run_cfg trace_thresh_high     TRACE_THRESHOLD=1000000
run_cfg no_mature_evac_knob   MAX_YOUNG_EVAC_SIZE=0

echo
echo "===== summary ====="
for f in "$OUT"/*.log; do
  printf '%-44s ' "$(basename "$f" .log)"
  grep -h "^STALLSUM" "$f" | sed -E 's/^STALLSUM [^ ]+ //'
done
echo "probe2-knobs done -> $OUT"
