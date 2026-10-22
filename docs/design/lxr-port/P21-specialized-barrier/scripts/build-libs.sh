#!/bin/bash
# Build the Linux libraries (needs the clr build first) in the D1 tree.
set -u
CFG=${1:-Release}
LOG=/root/d1/build-libs-$CFG
cd /root/d1/main
./build.sh -s libs -c $CFG /p:RepositoryCommit=d1 > $LOG.log 2>&1
echo "exit=$?" > $LOG.status
tail -5 $LOG.log
