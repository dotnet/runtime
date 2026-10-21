#!/bin/bash
# P0.1 - build the remaining three JDK configurations sequentially.
set -uo pipefail
# Sibling scripts live next to this one; resolve them relative to it.
SF="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATUS=/root/lxr/logs/build-status.txt
mkdir -p /root/lxr/logs
: > "$STATUS"

for combo in "head release" "pldi fastdebug" "head fastdebug"; do
  set -- $combo
  rev=$1; lvl=$2
  echo "########## BUILDING $rev $lvl ##########"
  if bash "$SF/build-jdk.sh" "$rev" "$lvl" > "/root/lxr/logs/build-$rev-$lvl.out" 2>&1; then
    echo "$rev/$lvl=OK" >> "$STATUS"
    tail -6 "/root/lxr/logs/build-$rev-$lvl.out"
  else
    echo "$rev/$lvl=FAILED" >> "$STATUS"
    tail -30 "/root/lxr/logs/build-$rev-$lvl.out"
  fi
done

echo "########## ALL DONE ##########"
cat "$STATUS"
