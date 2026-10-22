#!/bin/bash
# P25: machine-code hash of the stock allocation helpers of a libcoreclr.so (P19 helperhash.py). The P25 changes are all in
# libclrlxrgc.so, so libcoreclr.so is not rebuilt. This script shows the hash of the libcoreclr.so that the frames use.
# usage: hash-helpers.sh <libcoreclr.so> <libcoreclr.so.dbg>
rm -rf /tmp/hash-p25
cd /root/p25/main/docs/design/lxr-port/P19-integration/scripts
python3 helperhash.py "$1" "$2" --dump /tmp/hash-p25 2>&1 | tail -6
echo "baseline TOTAL 46cee8bd87fd6252f5265267148d0a34"
