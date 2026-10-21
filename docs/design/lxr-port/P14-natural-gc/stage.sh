#!/bin/bash
# Stage the benchmark into WSL: copy sources and the compiled .NET assembly, compile Java.
# usage: stage.sh <repo-root-as-WSL-path>
set -eu
SRC="$1"
mkdir -p /root/gcbench/java /root/gcbench/net /root/gcbench/results
cp "$SRC/docs/design/lxr-port/P14-natural-gc/GcBench.java" /root/gcbench/java/
sed 's/\r$//' "$SRC/docs/design/lxr-port/P14-natural-gc/run-matrix.py" > /root/gcbench/run-matrix.py
cp "$SRC"/artifacts/P14-natural-gc/gcbench/gcbench.* /root/gcbench/net/
(cd /root/gcbench/java && javac -d . GcBench.java)
echo staged
