import java.io.BufferedReader;
import java.io.FileReader;
import java.lang.management.GarbageCollectorMXBean;
import java.lang.management.ManagementFactory;
import java.util.Locale;

// Natural-collection benchmark. No explicit collection is requested while measuring.
// Keep the logic identical to GcBench.cs: same workloads, same random numbers, same checksums.
public class GcBench {
    static final int BUCKETS = 64 * 8;
    static final int LIST_LENGTH = 64;
    static long rs;

    static long next() {
        rs ^= rs >>> 12;
        rs ^= rs << 25;
        rs ^= rs >>> 27;
        return rs * 0x2545F4914F6CDD1DL;
    }

    static int rnd(int n) {
        return (int) ((next() >>> 33) % n);
    }

    static void record(long[] histogram, long ns) {
        if (ns < 1) {
            ns = 1;
        }
        int lg = 63 - Long.numberOfLeadingZeros(ns);
        int sub = lg >= 3 ? (int) ((ns >>> (lg - 3)) & 7) : 0;
        histogram[lg * 8 + sub]++;
    }

    static double percentileMicros(long[] histogram, double fraction) {
        long total = 0;
        for (long count : histogram) {
            total += count;
        }
        if (total == 0) {
            return 0;
        }
        long target = (long) Math.ceil(total * fraction);
        long seen = 0;
        for (int bucket = 0; bucket < histogram.length; bucket++) {
            seen += histogram[bucket];
            if (seen >= target) {
                int lg = bucket / 8;
                int sub = bucket % 8;
                double upper = lg >= 3 ? (double) (9 + sub) * (double) (1L << (lg - 3)) : (double) (1L << lg);
                return upper / 1000.0;
            }
        }
        return 0;
    }

    static final class Tree {
        Tree left;
        Tree right;

        Tree(Tree l, Tree r) {
            left = l;
            right = r;
        }
    }

    static Tree bottomUp(int depth) {
        return depth > 0 ? new Tree(bottomUp(depth - 1), bottomUp(depth - 1)) : new Tree(null, null);
    }

    static int check(Tree t) {
        return t.left == null ? 1 : 1 + check(t.left) + check(t.right);
    }

    static long binaryTrees(int maxDepth, boolean calibrate) {
        int minDepth = 4;
        if (calibrate) {
            keep = bottomUp(maxDepth);
            return 0;
        }
        long total = check(bottomUp(maxDepth + 1));
        Tree longLived = bottomUp(maxDepth);
        for (int depth = minDepth; depth <= maxDepth; depth += 2) {
            int iterations = 1 << (maxDepth - depth + minDepth);
            long sum = 0;
            for (int i = 0; i < iterations; i++) {
                sum += check(bottomUp(depth));
            }
            total += sum + depth;
        }
        total += check(longLived);
        return total;
    }

    static final class Item {
        int v;
        Item next;
        byte[] data;
    }

    static final class Entry {
        int key;
        Item items;
    }

    static Entry buildEntry(int key) {
        Entry e = new Entry();
        e.key = key;
        Item head = null;
        for (int i = 0; i < 4; i++) {
            Item it = new Item();
            it.v = key + i;
            it.data = new byte[48];
            it.data[0] = (byte) (key + i);
            it.next = head;
            head = it;
        }
        e.items = head;
        return e;
    }

    static Object keep;

    static long requests(int entries, long count, long[] latency, boolean calibrate) {
        Entry[] table = new Entry[entries];
        for (int i = 0; i < entries; i++) {
            table[i] = buildEntry(i);
        }
        if (calibrate) {
            keep = table;
            return 0;
        }
        long cs = 0;
        for (long r = 0; r < count; r++) {
            long started = latency != null ? System.nanoTime() : 0;
            Item head = null;
            for (int i = 0; i < 24; i++) {
                Item it = new Item();
                it.v = i;
                it.data = new byte[128];
                it.data[0] = (byte) i;
                it.next = head;
                head = it;
            }
            for (Item it = head; it != null; it = it.next) {
                cs += it.v + (it.data[0] & 0xFF);
            }
            for (int k = 0; k < 8; k++) {
                Entry e = table[rnd(entries)];
                for (Item it = e.items; it != null; it = it.next) {
                    cs += it.v + (it.data[0] & 0xFF);
                }
            }
            if ((next() & 7) == 0) {
                int slot = rnd(entries);
                table[slot] = buildEntry(slot);
            }
            if (latency != null) {
                record(latency, System.nanoTime() - started);
            }
        }
        return cs;
    }

    static final class Node {
        int v;
        Node next;
    }

    static long listSuffix(int slots, long ops, boolean calibrate) {
        Node[] heads = new Node[slots];
        for (int j = 0; j < slots; j++) {
            Node head = null;
            for (int i = 0; i < LIST_LENGTH; i++) {
                Node n = new Node();
                n.v = j + i;
                n.next = head;
                head = n;
            }
            heads[j] = head;
        }
        if (calibrate) {
            keep = heads;
            return 0;
        }
        long cs = 0;
        for (long op = 0; op < ops; op++) {
            int j = rnd(slots);
            int p = rnd(LIST_LENGTH);
            Node cur = heads[j];
            for (int i = 0; i < p; i++) {
                cur = cur.next;
            }
            Node prev = cur;
            for (int i = 0; i < LIST_LENGTH - 1 - p; i++) {
                Node n = new Node();
                n.v = (int) op ^ i;
                prev.next = n;
                prev = n;
            }
            prev.next = null;
            cs += cur.v;
        }
        return cs;
    }

    static long run(String workload, int size, long ops, long[] latency, boolean calibrate) {
        switch (workload) {
            case "binarytrees":
                return binaryTrees(size, calibrate);
            case "requests":
                return requests(size, ops, latency, calibrate);
            case "listsuffix":
                return listSuffix(size, ops, calibrate);
            default:
                throw new IllegalArgumentException(workload);
        }
    }

    static volatile boolean stop;
    static final long[] hiccup = new long[BUCKETS];
    static long hiccupMax;

    static long statusKb(String key) {
        try (BufferedReader r = new BufferedReader(new FileReader("/proc/self/status"))) {
            String line;
            while ((line = r.readLine()) != null) {
                if (line.startsWith(key)) {
                    return Long.parseLong(line.replaceAll("[^0-9]", ""));
                }
            }
        } catch (Exception e) {
            return -1;
        }
        return -1;
    }

    public static void main(String[] a) throws Exception {
        String workload = a[0];
        int size = Integer.parseInt(a[1]);
        long ops = Long.parseLong(a[2]);
        String mode = a.length > 3 ? a[3] : "run";
        if (mode.equals("calibrate")) {
            run(workload, size, ops, null, true);
            System.gc();
            System.gc();
            Runtime rt = Runtime.getRuntime();
            System.out.println("{\"runtime\":\"java\",\"workload\":\"" + workload + "\",\"live_bytes\":" + (rt.totalMemory() - rt.freeMemory()) + "}");
            return;
        }
        int warmSize = workload.equals("binarytrees") ? Math.max(8, size - 4) : Math.max(1024, size / 8);
        long warmOps = Math.max(1, ops / 8);
        rs = 0x9E3779B97F4A7C15L;
        run(workload, warmSize, warmOps, null, false);
        rs = 0x9E3779B97F4A7C15L;
        long[] latency = new long[BUCKETS];
        Thread meter = new Thread(() -> {
            while (!stop) {
                long t = System.nanoTime();
                try {
                    Thread.sleep(1);
                } catch (InterruptedException e) {
                    return;
                }
                long d = System.nanoTime() - t;
                record(hiccup, d);
                if (d > hiccupMax) {
                    hiccupMax = d;
                }
            }
        });
        meter.setDaemon(true);
        com.sun.management.OperatingSystemMXBean os =
            (com.sun.management.OperatingSystemMXBean) ManagementFactory.getOperatingSystemMXBean();
        long gcCountBefore = 0;
        long gcMsBefore = 0;
        for (GarbageCollectorMXBean b : ManagementFactory.getGarbageCollectorMXBeans()) {
            gcCountBefore += Math.max(0, b.getCollectionCount());
            gcMsBefore += Math.max(0, b.getCollectionTime());
        }
        meter.start();
        System.out.println("BENCH_MARK start");
        System.err.println("BENCH_MARK start");
        long cpu0 = os.getProcessCpuTime();
        long t0 = System.nanoTime();
        long checksum = run(workload, size, ops, latency, false);
        long elapsed = System.nanoTime() - t0;
        long cpu = os.getProcessCpuTime() - cpu0;
        System.out.println("BENCH_MARK end");
        System.err.println("BENCH_MARK end");
        stop = true;
        meter.join();
        long gcCount = 0;
        long gcMs = 0;
        for (GarbageCollectorMXBean b : ManagementFactory.getGarbageCollectorMXBeans()) {
            gcCount += Math.max(0, b.getCollectionCount());
            gcMs += Math.max(0, b.getCollectionTime());
        }
        System.out.println(String.format(Locale.ROOT,
            "{\"runtime\":\"java\",\"workload\":\"%s\",\"size\":%d,\"ops\":%d,\"checksum\":%d,\"elapsed_ms\":%.3f,\"cpu_ms\":%.3f,"
            + "\"gc_count\":%d,\"gc_ms\":%d,\"hiccup_p50_us\":%.1f,\"hiccup_p99_us\":%.1f,\"hiccup_p999_us\":%.1f,\"hiccup_max_us\":%.1f,"
            + "\"req_p50_us\":%.1f,\"req_p99_us\":%.1f,\"req_p999_us\":%.1f,"
            + "\"peak_rss_kb\":%d}",
            workload, size, ops, checksum, elapsed / 1e6, cpu / 1e6,
            gcCount - gcCountBefore, gcMs - gcMsBefore,
            percentileMicros(hiccup, 0.5), percentileMicros(hiccup, 0.99), percentileMicros(hiccup, 0.999), hiccupMax / 1000.0,
            percentileMicros(latency, 0.5), percentileMicros(latency, 0.99), percentileMicros(latency, 0.999),
            statusKb("VmHWM")));
    }
}
