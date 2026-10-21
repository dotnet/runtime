#!/bin/bash
# P0.1 addendum - build the SHIPPED pairs (pristine binding Cargo.toml/lock, git-pinned mmtk-core).
#   PLDI shipped = binding abbdd1d + mmtk-core df8d30a3
#   HEAD shipped = binding 0682434 + mmtk-core 304ce69d
set -uo pipefail
export PATH=/root/.cargo/bin:$PATH
export LIBCLANG_PATH=/usr/lib/llvm-14/lib
unset MMTK_PLAN
mkdir -p /root/lxr/logs /root/lxr/so-backup

save_so () { # $1=tag $2=jdkdir $3=conf
  local s=$2/build/$3/jdk/lib/server/libmmtk_openjdk.so
  [ -f "$s" ] && cp -a "$s" /root/lxr/so-backup/$1.so && echo "  saved override .so -> /root/lxr/so-backup/$1.so"
}

echo "########## 0. back up the override (task-named-oracle) .so files ##########"
save_so pldi-release-override  /root/lxr/pldi/mmtk-openjdk/repos/openjdk linux-x86_64-normal-server-release
save_so pldi-fastdebug-override /root/lxr/pldi/mmtk-openjdk/repos/openjdk linux-x86_64-normal-server-fastdebug
save_so head-release-override  /root/lxr/head/mmtk-openjdk/repos/openjdk linux-x86_64-normal-server-release
save_so head-fastdebug-override /root/lxr/head/mmtk-openjdk/repos/openjdk linux-x86_64-normal-server-fastdebug

echo
echo "########## 1. restore pristine Cargo.toml + Cargo.lock (= shipped config) ##########"
for b in /root/lxr/pldi/mmtk-openjdk /root/lxr/head/mmtk-openjdk; do
  git -C $b checkout -- mmtk/Cargo.toml mmtk/Cargo.lock
  echo "-- $b --"
  grep -n '^mmtk = ' $b/mmtk/Cargo.toml
  git -C $b status --short | head -5
done

echo
echo "########## 2. build PLDI shipped (release, fastdebug) ##########"
cd /root/lxr/pldi/mmtk-openjdk/repos/openjdk
for conf in release fastdebug; do
  echo "-- pldi $conf --"
  make CONF=linux-x86_64-normal-server-$conf \
       THIRD_PARTY_HEAP=/root/lxr/pldi/mmtk-openjdk/openjdk \
       GC_FEATURES=lxr,immix > /root/lxr/logs/pldi-shipped-$conf.log 2>&1
  echo "   MAKE_EXIT=$?"
  grep -E '^cargo build' /root/lxr/logs/pldi-shipped-$conf.log | tail -1
  grep -E 'error(\[|:)' /root/lxr/logs/pldi-shipped-$conf.log | head -5
  ls -l --time-style=+%H:%M build/linux-x86_64-normal-server-$conf/jdk/lib/server/libmmtk_openjdk.so 2>/dev/null
  cp -a build/linux-x86_64-normal-server-$conf/jdk/lib/server/libmmtk_openjdk.so \
        /root/lxr/so-backup/pldi-$conf-shipped.so 2>/dev/null
done

echo
echo "########## 3. build HEAD shipped (release, fastdebug) ##########"
cd /root/lxr/head/mmtk-openjdk/repos/openjdk
for conf in release fastdebug; do
  echo "-- head $conf --"
  make CONF=linux-x86_64-normal-server-$conf \
       THIRD_PARTY_HEAP=/root/lxr/head/mmtk-openjdk/openjdk \
       > /root/lxr/logs/head-shipped-$conf.log 2>&1
  echo "   MAKE_EXIT=$?"
  grep -E '^cargo build' /root/lxr/logs/head-shipped-$conf.log | tail -1
  grep -E 'error(\[|:)' /root/lxr/logs/head-shipped-$conf.log | head -5
  ls -l --time-style=+%H:%M build/linux-x86_64-normal-server-$conf/jdk/lib/server/libmmtk_openjdk.so 2>/dev/null
  cp -a build/linux-x86_64-normal-server-$conf/jdk/lib/server/libmmtk_openjdk.so \
        /root/lxr/so-backup/head-$conf-shipped.so 2>/dev/null
done

echo
echo "########## 4. confirm the git rev cargo actually used ##########"
grep -rn 'rev=' /root/lxr/pldi/mmtk-openjdk/mmtk/Cargo.lock | head -2
grep -rn 'rev=' /root/lxr/head/mmtk-openjdk/mmtk/Cargo.lock | head -2
ls -l --time-style=+%H:%M /root/lxr/so-backup/
echo done
