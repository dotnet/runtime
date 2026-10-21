#!/bin/bash
# P0.1 - sanity check: does the built JDK actually run LXR?
#   usage: sanity.sh <pldi|head> <release|fastdebug> [MMTK_PLAN]
set -uo pipefail

REV=$1
LVL=$2
PLAN=${3:-}

JDKBIN=/root/lxr/$REV/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-$LVL/jdk/bin
mkdir -p /tmp/sanity && cd /tmp/sanity

cat > HelloWorld.java <<'EOF'
import java.util.ArrayList;
public class HelloWorld {
  public static void main(String[] args) {
    ArrayList<byte[]> live = new ArrayList<>();
    for (int i = 0; i < 400000; i++) {
      live.add(new byte[256]);
      if (live.size() > 2000) live.clear();
    }
    System.out.println("HelloWorld OK; live=" + live.size());
  }
}
EOF

echo "=== javac ($REV/$LVL) ==="
"$JDKBIN/javac" HelloWorld.java || { echo "JAVAC FAILED"; exit 1; }

echo "=== java -XX:+UseThirdPartyHeap  MMTK_PLAN='${PLAN}' ==="
if [ -n "$PLAN" ]; then
  MMTK_PLAN="$PLAN" "$JDKBIN/java" -XX:+UseThirdPartyHeap -Xms128M -Xmx128M HelloWorld 2>&1 | tail -25
else
  "$JDKBIN/java" -XX:+UseThirdPartyHeap -Xms128M -Xmx128M HelloWorld 2>&1 | tail -25
fi
echo "RUN_EXIT=${PIPESTATUS[0]}"
