#!/bin/bash
# Build the diagnostic variant of the collector (P24 only; the sources are a COPY under /root/p24/main, not the repository).
# The variant adds two environment switches and nothing else: P24_SKIP_BLACK_TRACE=1 and P24_WORKERS=<n>.
# With neither switch set it behaves as the p24 frame. Output: /root/p24/frames/p24diag-Release-linux
export PATH=/usr/local/lib/python3.10/dist-packages/cmake/data/bin:$PATH
R=/root/p24
ninja -C $R/obj-Release clrlxrgc > $R/logs/build-diag.log 2>&1
echo "build exit=$?"; tail -3 $R/logs/build-diag.log
F=$R/frames/p24diag-Release-linux
rm -rf $F
cp -al $R/frames/p24-Release-linux $F
cp --remove-destination $R/obj-Release/gc/libclrlxrgc.so $F/libclrlxrgc.so
cp --remove-destination $R/obj-Release/gc/libclrlxrgc.so.dbg $F/libclrlxrgc.so.dbg
ls -la $F/libclrlxrgc.so
