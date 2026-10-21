#!/bin/bash
# P0.3 probe 2, stage 1 - timeline stall meter (no MMTk rebuild).
#
# P0.1 6.2 recorded that hsqldb @2000MB shows p99 = 54-56 ms on the lxr-head oracle
# against 0.34-0.40 ms on the PLDI oracle, like-for-like.  The percentile summary
# cannot say *when* those stalls happen.  This meter keeps every sample with its
# timestamp and cross-references DaCapo's own iteration callbacks, so the stalls can
# be located in time: spread evenly, at iteration boundaries, or in one phase.
#
# Uses the SHIPPED release .so of both oracles - nothing is rebuilt. [obs-oracle] for
# the collector; the harness change is measurement-only.
set -uo pipefail
mkdir -p /root/lxr/harness-tl
cd /root/lxr/harness-tl

cat > HiccupTL.java <<'EOF'
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * Timeline variant of the P0.1 Hiccup meter. Same 1 ms cadence and same excess-over-
 * intended-wake definition, so summary statistics stay comparable with P0.1; but every
 * sample keeps its timestamp, and stalls over a threshold are printed individually
 * alongside phase markers emitted by the DaCapo callback.
 */
public final class HiccupTL extends Thread {
    private static final long INTERVAL_NS = 1_000_000L;
    private static final double STALL_MS =
        Double.parseDouble(System.getProperty("stall.ms", "5.0"));
    private static final int CAP = 8_000_000;

    private static final long ORIGIN = System.nanoTime();
    private static final List<String> MARKS = new ArrayList<String>();

    private final long[] at = new long[CAP];
    private final long[] excess = new long[CAP];
    private int n = 0;
    private volatile boolean running = true;

    public HiccupTL() { setDaemon(true); setName("hiccup-tl"); }

    public static synchronized void mark(String label) {
        MARKS.add(String.format("%.3f\t%s", ms(System.nanoTime() - ORIGIN), label));
    }

    @Override public void run() {
        long next = System.nanoTime() + INTERVAL_NS;
        while (running) {
            try { Thread.sleep(1); } catch (InterruptedException e) { return; }
            long now = System.nanoTime();
            long ex = now - next;
            if (ex < 0) ex = 0;
            if (n < CAP) { at[n] = now - ORIGIN; excess[n] = ex; n++; }
            next = now + INTERVAL_NS;
        }
    }

    private static double ms(long ns) { return ns / 1_000_000.0; }

    public void report(String tag) {
        running = false;
        int m = n;
        if (m == 0) { System.out.println("HICCUPTL " + tag + " no-samples"); return; }

        long[] s = Arrays.copyOf(excess, m);
        Arrays.sort(s);
        long total = 0;
        for (int i = 0; i < m; i++) total += s[i];
        System.out.printf(
            "HICCUPTL %s samples=%d mean=%.3f p50=%.3f p90=%.3f p99=%.3f p999=%.3f max=%.3f%n",
            tag, m, ms(total / m), ms(pct(s, 50.0)), ms(pct(s, 90.0)),
            ms(pct(s, 99.0)), ms(pct(s, 99.9)), ms(s[m - 1]));

        long thresholdNs = (long) (STALL_MS * 1_000_000.0);
        int stalls = 0; long stallSum = 0;
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < m; i++) {
            if (excess[i] >= thresholdNs) {
                stalls++; stallSum += excess[i];
                sb.append(String.format("STALL %s\t%.3f\t%.3f%n", tag, ms(at[i]), ms(excess[i])));
            }
        }
        System.out.printf("STALLSUM %s count=%d over=%.1fms total=%.3fms wall=%.3fms%n",
            tag, stalls, STALL_MS, ms(stallSum), ms(at[m - 1]));
        System.out.print(sb);
        synchronized (HiccupTL.class) {
            for (String s2 : MARKS) System.out.printf("MARK %s\t%s%n", tag, s2);
        }
    }

    private static long pct(long[] s, double p) {
        int i = (int) Math.ceil(p / 100.0 * s.length) - 1;
        if (i < 0) i = 0;
        if (i >= s.length) i = s.length - 1;
        return s[i];
    }
}
EOF

cat > TLCallback.java <<'EOF'
/** Emits a timestamped marker at every DaCapo iteration boundary. */
public class TLCallback extends dacapo.Callback {
    @Override public void start(String b)            { HiccupTL.mark("start " + b);          super.start(b); }
    @Override public void stop()                     { super.stop();  HiccupTL.mark("stop"); }
    @Override public void complete(String b, boolean v) { HiccupTL.mark("complete " + b + " valid=" + v); super.complete(b, v); }
    @Override public void startWarmup(String b)      { HiccupTL.mark("startWarmup " + b);    super.startWarmup(b); }
    @Override public void stopWarmup()               { super.stopWarmup(); HiccupTL.mark("stopWarmup"); }
    @Override public void completeWarmup(String b, boolean v) { HiccupTL.mark("completeWarmup " + b + " valid=" + v); super.completeWarmup(b, v); }
}
EOF

cat > BenchTL.java <<'EOF'
import java.lang.reflect.Method;

/** Starts the timeline meter, then delegates to the DaCapo harness main class. */
public final class BenchTL {
    public static void main(String[] args) throws Exception {
        final String tag = System.getProperty("bench.tag", "run");
        final HiccupTL h = new HiccupTL();
        h.start();
        Runtime.getRuntime().addShutdownHook(new Thread(new Runnable() {
            public void run() { h.report(tag); }
        }));
        String mainClass = System.getProperty("dacapo.main", "Harness");
        Method m = Class.forName(mainClass).getMethod("main", String[].class);
        m.invoke(null, (Object) args);
    }
}
EOF

echo "########## compile with each oracle's release JDK ##########"
DACAPO=/root/lxr/dacapo/dacapo-2006-10-MR2.jar
PJ=/root/lxr/pldi/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin
HJ=/root/lxr/head/mmtk-openjdk/repos/openjdk/build/linux-x86_64-normal-server-release/jdk/bin
mkdir -p cls-pldi cls-head
"$PJ/javac" -cp "$DACAPO" -d cls-pldi HiccupTL.java TLCallback.java BenchTL.java && echo "PLDI_JAVAC=OK" || echo "PLDI_JAVAC=FAILED"
"$HJ/javac" -cp "$DACAPO" -d cls-head HiccupTL.java TLCallback.java BenchTL.java && echo "HEAD_JAVAC=OK" || echo "HEAD_JAVAC=FAILED"
echo "mk-harness-timeline done"
