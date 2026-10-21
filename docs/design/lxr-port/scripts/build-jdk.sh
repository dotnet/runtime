#!/bin/bash
# P0.1 - build MMTk-OpenJDK for one revision at one debug level.
#   usage: build-jdk.sh <pldi|head> <release|fastdebug>
#
# PLDI: LXR is a COMPILE-TIME cargo feature -> MMTK_PLAN=lxr must be set for make.
# head: LXR is selected at RUNTIME via the MMTK_PLAN env var -> do NOT set it at build.
set -euo pipefail
cd /root
export PATH=/root/.cargo/bin:$PATH
export CARGO_NET_GIT_FETCH_WITH_CLI=true
export LIBCLANG_PATH=/usr/lib/llvm-14/lib

REV=$1
LVL=$2

BINDING=/root/lxr/$REV/mmtk-openjdk
JDK=$BINDING/repos/openjdk
LOG=/root/lxr/logs
mkdir -p "$LOG"

cd "$JDK"

echo "=== [$REV/$LVL] configure ==="
CONF_ARGS="--disable-warnings-as-errors --with-debug-level=$LVL --with-boot-jdk=/usr/lib/jvm/java-11-openjdk-amd64"
if [ "$REV" = "pldi" ]; then
  export MMTK_PLAN=lxr        # compile-time feature selection
fi
sh configure $CONF_ARGS > "$LOG/configure-$REV-$LVL.log" 2>&1 || {
  echo "CONFIGURE FAILED - tail:"; tail -30 "$LOG/configure-$REV-$LVL.log"; exit 1; }
echo "configure ok"

echo "=== [$REV/$LVL] make (THIRD_PARTY_HEAP=$BINDING/openjdk) ==="
make CONF=linux-x86_64-normal-server-$LVL \
     THIRD_PARTY_HEAP=$BINDING/openjdk \
     > "$LOG/make-$REV-$LVL.log" 2>&1 || {
  echo "MAKE FAILED - error lines:"; grep -iE '^\s*(error|.*Error [0-9])' "$LOG/make-$REV-$LVL.log" | head -25
  echo "--- tail ---"; tail -30 "$LOG/make-$REV-$LVL.log"; exit 1; }

JAVA_BIN=$JDK/build/linux-x86_64-normal-server-$LVL/jdk/bin/java
echo "=== [$REV/$LVL] BUILD OK ==="
ls -la "$JAVA_BIN"
ls -la "$JDK/build/linux-x86_64-normal-server-$LVL/jdk/lib/server/libmmtk_openjdk.so"
"$JAVA_BIN" -version 2>&1 | head -3
