#!/bin/bash
# P0.1 - build the benchmark harness: mutator-observed pause meter + DaCapo wrapper.
set -uo pipefail
mkdir -p /root/lxr/harness
cd /root/lxr/harness

echo "########## DaCapo 2006 manifest ##########"
unzip -p /root/lxr/dacapo/dacapo-2006-10-MR2.jar META-INF/MANIFEST.MF | tr -d '\r'

cat > Hiccup.java <<'EOF'
import java.util.Arrays;

/**
 * jHiccup-style mutator-observed stall meter. A daemon thread wakes on a fixed
 * cadence; any excess over the intended wake interval is time during which this
 * mutator thread was not permitted to run. For a stop-the-world collector that
 * excess is dominated by GC pauses.
 */
public final class Hiccup extends Thread {
    private static final long INTERVAL_NS = 1_000_000L;
    private final long[] samples = new long[8_000_000];
    private int n = 0;
    private volatile boolean running = true;

    public Hiccup() { setDaemon(true); setName("hiccup-meter"); }

    @Override public void run() {
        long next = System.nanoTime() + INTERVAL_NS;
        while (running) {
            try { Thread.sleep(1); } catch (InterruptedException e) { return; }
            long now = System.nanoTime();
            long excess = now - next;
            if (excess < 0) excess = 0;
            if (n < samples.length) samples[n++] = excess;
            next = now + INTERVAL_NS;
        }
    }

    private static double ms(long ns) { return ns / 1_000_000.0; }

    public void report(String tag) {
        running = false;
        int m = n;
        if (m == 0) { System.out.println("HICCUP " + tag + " no-samples"); return; }
        long[] s = Arrays.copyOf(samples, m);
        Arrays.sort(s);
        System.out.printf(
            "HICCUP %s samples=%d mean=%.3f p50=%.3f p90=%.3f p99=%.3f p999=%.3f max=%.3f%n",
            tag, m, ms(mean(s)), ms(pct(s, 50.0)), ms(pct(s, 90.0)),
            ms(pct(s, 99.0)), ms(pct(s, 99.9)), ms(s[m - 1]));
    }

    private static long mean(long[] s) { long t = 0; for (long v : s) t += v; return t / s.length; }

    private static long pct(long[] s, double p) {
        int i = (int) Math.ceil(p / 100.0 * s.length) - 1;
        if (i < 0) i = 0;
        if (i >= s.length) i = s.length - 1;
        return s[i];
    }
}
EOF

cat > Bench.java <<'EOF'
import java.lang.reflect.Method;

/** Starts the pause meter, then delegates to the DaCapo harness main class. */
public final class Bench {
    public static void main(String[] args) throws Exception {
        final String tag = System.getProperty("bench.tag", "run");
        final Hiccup h = new Hiccup();
        h.start();
        Runtime.getRuntime().addShutdownHook(new Thread(() -> h.report(tag)));
        String mainClass = System.getProperty("dacapo.main", "Harness");
        Method m = Class.forName(mainClass).getMethod("main", String[].class);
        m.invoke(null, (Object) args);
    }
}
EOF

echo
echo "########## compile with each JDK ##########"
PJ=/root/lxr/pldi/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin
HJ=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin
mkdir -p cls-pldi cls-head
"$PJ/javac" -d cls-pldi Hiccup.java Bench.java && echo "PLDI_JAVAC=OK" || echo "PLDI_JAVAC=FAILED"
"$HJ/javac" -d cls-head Hiccup.java Bench.java && echo "HEAD_JAVAC=OK" || echo "HEAD_JAVAC=FAILED"

echo
echo "########## smoke: head release, antlr, 1 iter, meter on ##########"
cd /root/lxr/dacapo
MMTK_PLAN=LXR "$HJ/java" -XX:+UseThirdPartyHeap -server -XX:MetaspaceSize=100M \
  -Xms10M -Xmx10M -Dbench.tag=smoke -Ddacapo.main=Harness \
  -cp /root/lxr/dacapo/dacapo-2006-10-MR2.jar:/root/lxr/harness/cls-head \
  Bench -n 1 antlr 2>&1 | grep -E "PASSED|FAILED|HICCUP|Exception|error" | head -10
echo "SMOKE_EXIT=${PIPESTATUS[0]}"
