#!/bin/bash
# P26: which frame holds which code (the LXRWaitForLazy setting exists only after F8).
for f in base f2 f4b final; do
  p=/root/p25/frames/p26-$f-Release-linux/libclrlxrgc.so
  echo "$f waitforlazy=$(strings $p | grep -c LXRWaitForLazy) sha256=$(sha256sum $p | cut -c1-16)"
done
