#!/bin/bash
# P0.3 probe 1b - run the POSITIVE CONTROL.
#
# Probe 1 reported 48/48 runs clean with PLDI's zero-RC invariant re-instated at the head
# oracle.  That is only evidence if the assertion site is REACHED under this load.  This
# run installs an otherwise identical build whose assertion fires on the millionth reach.
#
#   logs a large count -> the site is exercised, so probe 1's null result is a real
#                         negative and the invariant genuinely held under that load
#   logs nothing        -> probe 1's null result is VACUOUS and must be reported as such
#
# Restores the shipped head fastdebug .so on exit.
set -uo pipefail
ITERS=${1:-5}
HEAPS="${*:2}"
HEAPS="${HEAPS:-2000}"

JDKDIR=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-fastdebug
SO=$JDKDIR/jdk/lib/server/libmmtk_openjdk.so
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
LOGD=/root/lxr/results/flogs-probe1b
BENCHES="lusearch hsqldb xalan"
TMO=900

restore () {
  echo "-- restoring shipped head fastdebug .so --"
  cp -a /root/lxr/so-backup/head-fastdebug-shipped.so "$SO" \
    && echo "   restored; 'probe1b' occurrences now: $(strings "$SO" | grep -c 'probe1b')"
}
trap restore EXIT

echo core > /proc/sys/kernel/core_pattern 2>/dev/null
ulimit -c 0
mkdir -p "$LOGD"

echo "########## 0. confirm the CONTROL .so is installed ##########"
cp -a /root/lxr/so-backup/head-fastdebug-probe1b.so "$SO" || exit 1
n=$(strings "$SO" | grep -c 'probe1b positive control')
echo "'probe1b positive control' occurrences in installed .so: $n"
[ "$n" -ge 1 ] || { echo "ABORT: control .so not installed"; exit 1; }

for heap in $HEAPS; do
  for bench in $BENCHES; do
    log="$LOGD/probe1b-$bench-$heap.log"
    cd /root/lxr/dacapo
    t0=$(date +%s)
    env MMTK_PLAN=LXR timeout -k 10 $TMO "$JDKDIR/jdk/bin/java" \
        -XX:+UseThirdPartyHeap -server -XX:MetaspaceSize=100M \
        -Xms${heap}M -Xmx${heap}M \
        -Ddacapo.main=Harness -cp "$DACAPO" Harness -n "$ITERS" "$bench" > "$log" 2>&1
    rc=$?; el=$(( $(date +%s) - t0 ))
    # the last power-of-two line is the lower bound on how often the site was reached
    last=$(grep -o 'slow path reached [0-9]* times' "$log" | tail -1 | grep -o '[0-9]*')
    pass=$(grep -c PASSED "$log")
    printf '%-10s heap=%-5s rc=%-3s %3ds  PASSED=%s  slow-path reached >= %s\n' \
      "$bench" "$heap" "$rc" "$el" "$pass" "${last:-0}"
  done
done
echo "probe1b-run done"
