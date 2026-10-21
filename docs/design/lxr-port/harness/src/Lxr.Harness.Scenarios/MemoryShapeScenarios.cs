// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Lxr.Harness.Core;

namespace Lxr.Harness.Scenarios;

/// <summary>
/// Scenario 3 - mature space, high survival, defragmentation pressure.
///
/// <para>Exercises the part of LXR that is not reference counting: Immix mature-space evacuation, the
/// 5% mature-wastage threshold that triggers defragmentation, and the single evacuation set (P0.2).
/// Long-lived objects are also the population the paper's Table 7 calls "Stuck" - objects whose
/// reference count saturates and which therefore can only be reclaimed by the backup trace.</para>
/// </summary>
public sealed class LongLivedCacheScenario : IScenario
{
    private sealed class Entry
    {
        public byte[]? Value;
        public Entry? Newer;
        public Entry? Older;
        public long Key;
    }

    private Entry?[] _table = [];
    private int _entrySizeBytes;
    private int _lookupsPerOperation;
    private long _operations;
    private long _hits;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "long-lived-cache",
        Rationale =
            "Mature space, high survival and defragmentation pressure. This is where Immix mature " +
            "evacuation, the 5% mature-wastage defragmentation threshold and the single evacuation set " +
            "are exercised (P0.2), and where the 'Stuck' population of Table 7 lives: objects whose " +
            "reference count saturates, which reference counting alone can never reclaim.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 500,
        ProvisionalBaselineHeapBytes = 512L * 1024 * 1024,
        Axes = ["residentEntries", "entrySizeBytes", "lookupsPerOperation"],
    };

    public void Setup(ScenarioContext context)
    {
        int resident = context.Parameters.GetInt32("residentEntries", 1 << 16);
        _entrySizeBytes = context.Parameters.GetInt32("entrySizeBytes", 512);
        _lookupsPerOperation = context.Parameters.GetInt32("lookupsPerOperation", 8);
        _table = new Entry?[resident];

        // Fill the cache so the run starts in the steady state it means to measure, rather than
        // spending its warmup filling an empty cache.
        for (int i = 0; i < resident; i++)
        {
            _table[i] = new Entry { Key = i, Value = new byte[_entrySizeBytes] };
        }

        for (int i = 0; i < resident; i++)
        {
            _table[i]!.Newer = _table[(i + 1) % resident];
            _table[i]!.Older = _table[(i + resident - 1) % resident];
        }

        _operations = 0;
        _hits = 0;
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        Entry?[] table = _table;
        int slot = (int)(index % table.Length);

        // Replacing a resident entry retires an old mature object and creates a new one, which is
        // what produces mature-space fragmentation over time.
        var replacement = new Entry { Key = index, Value = new byte[_entrySizeBytes] };
        Entry? evicted = table[slot];
        replacement.Older = evicted?.Older;
        replacement.Newer = evicted?.Newer;
        table[slot] = replacement;

        long accumulator = index;
        for (int i = 0; i < _lookupsPerOperation; i++)
        {
            Entry? found = table[(int)((index * 2654435761L + i) % table.Length)];
            if (found?.Value is not null)
            {
                Interlocked.Increment(ref _hits);
                accumulator += found.Key + found.Value.Length;
            }
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        int resident = 0;
        foreach (Entry? entry in _table)
        {
            if (entry?.Value is not null)
            {
                resident++;
            }
        }

        var violations = new List<string>();
        if (operations > 0 && resident != _table.Length)
        {
            violations.Add($"cache lost residency: {resident} of {_table.Length} slots populated, so survival was not what the scenario claims");
        }

        return new ScenarioVerification
        {
            Success = operations >= 500 && violations.Count == 0,
            Marker = $"long-lived-cache:hits={Interlocked.Read(ref _hits)}",
            Detail = $"operations={operations}, residentEntries={resident}",
            Violations = violations,
        };
    }

    public void Teardown() => _table = [];
}

/// <summary>
/// Scenario 4 - the sharpest discriminator between a reference-counting collector and a tracing one.
///
/// <para>A pure reference count can never reclaim a cycle, because every object in the cycle is kept
/// alive by another object in the cycle. This is precisely what forces LXR to run its snapshot-at-
/// the-beginning backup trace. The paper's own numbers make the stakes concrete: backup tracing
/// reclaims 5.1% of objects on average across the suite, but on <c>batik</c> it reclaims 54.5% - the
/// majority (P0.2). A port that implemented only the young reference-counting path would reproduce
/// sixteen of seventeen benchmarks and fail this one catastrophically.</para>
///
/// <para>Because of that, this scenario is correctness-first: it asserts that the live set does not
/// grow. Unreclaimed cycles are a leak, and a leak must fail the scenario rather than quietly show up
/// as a slower number.</para>
/// </summary>
public sealed class CyclicGarbageScenario : IScenario
{
    private sealed class CycleNode
    {
        public CycleNode? Next;
        public byte[]? Ballast;
        public long Tag;
    }

    private int _cycleSize;
    private int _cyclesPerOperation;
    private int _ballastBytes;
    private long _operations;
    private long _baselineLiveBytes;
    private double _growthToleranceFactor;
    private object?[] _anchors = [];

    public ScenarioDescriptor Describe() => new()
    {
        Id = "cyclic-garbage",
        Rationale =
            "The sharpest reference-counting discriminator. Pure reference counting cannot reclaim a " +
            "cycle, so this is the workload that forces LXR's snapshot-at-the-beginning backup trace. " +
            "Backup tracing reclaims 5.1% of objects on average across the paper's suite but 54.5% on " +
            "batik (P0.2), so a port implementing only the young reference-counting path would pass " +
            "almost every other scenario and fail this one. Asserts live-set stability, so unreclaimed " +
            "cycles fail the run instead of appearing as a merely slower number.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 200,
        ProvisionalBaselineHeapBytes = 256L * 1024 * 1024,
        Axes = ["cycleSize", "cyclesPerOperation", "ballastBytes"],
    };

    public void Setup(ScenarioContext context)
    {
        _cycleSize = Math.Max(2, context.Parameters.GetInt32("cycleSize", 16));
        _cyclesPerOperation = context.Parameters.GetInt32("cyclesPerOperation", 8);
        _ballastBytes = context.Parameters.GetInt32("ballastBytes", 128);
        _growthToleranceFactor = context.Parameters.GetDouble("growthToleranceFactor", 3.0);
        _anchors = new object?[64];
        _operations = 0;

        // The baseline is taken the same way the final measurement will be - after a full blocking
        // collection - so the two numbers are comparable. This happens during setup, outside the
        // measured region, so it is not an induced collection during measurement.
        _baselineLiveBytes = MeasureLiveBytes();
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        long accumulator = index;

        for (int c = 0; c < _cyclesPerOperation; c++)
        {
            CycleNode head = new() { Tag = index * 31 + c, Ballast = new byte[_ballastBytes] };
            CycleNode current = head;
            for (int i = 1; i < _cycleSize; i++)
            {
                var node = new CycleNode { Tag = head.Tag + i, Ballast = new byte[_ballastBytes] };
                current.Next = node;
                current = node;
                accumulator += node.Tag;
            }

            // Closing the ring is the whole point: from here every node in the cycle is referenced by
            // another node in the cycle, so no reference count in the cycle ever reaches zero.
            current.Next = head;

            // The cycle is dropped immediately and becomes unreachable garbage that only a trace can
            // reclaim. One is anchored occasionally so the shape is definitely materialised.
            if ((index + c) % 512 == 0)
            {
                _anchors[(int)((index + c) / 512 % _anchors.Length)] = head;
            }
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        long liveBytes = MeasureLiveBytes();
        var violations = new List<string>();

        long allowed = (long)(Math.Max(_baselineLiveBytes, 1) * _growthToleranceFactor) + (16L * 1024 * 1024);
        if (liveBytes > allowed)
        {
            violations.Add(
                $"live set grew from {_baselineLiveBytes} to {liveBytes} bytes, above the allowed {allowed}: " +
                "cyclic garbage was not reclaimed, which is the failure mode a reference-counting collector " +
                "without a working backup trace would show");
        }

        return new ScenarioVerification
        {
            Success = operations >= 200 && violations.Count == 0,
            Marker = $"cyclic-garbage:cycles={operations * _cyclesPerOperation}",
            Detail = $"operations={operations}, baselineLiveBytes={_baselineLiveBytes}, finalLiveBytes={liveBytes}",
            Violations = violations,
        };
    }

    /// <summary>
    /// Only ever called outside the measured region. Measuring a live set requires a full blocking
    /// collection, and P0.3 traced a cross-oracle p99 discrepancy to exactly this kind of induced
    /// collection, so it must never happen while measuring.
    /// </summary>
    private static long MeasureLiveBytes()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    public void Teardown() => _anchors = [];
}

/// <summary>
/// Scenario 5 - write-barrier cost under mutation-heavy pointer updates, and locality after evacuation.
///
/// <para>Complements the low-allocation scenario rather than duplicating it: that one measures the
/// barrier with very few stores per unit of work, this one with a great many. Run as a pair they
/// separate per-store barrier cost from per-object cost. The traversal also exposes locality, which
/// is what an evacuating collector changes when it moves objects.</para>
/// </summary>
public sealed class PointerChasingScenario : IScenario
{
    private RefNode[] _nodes = [];
    private int _hops;
    private int _mutationsPerOperation;
    private int _allocationBytesPerOperation;
    private byte[]? _allocationSink;
    private long _operations;
    private long _hopsPerformed;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "pointer-chasing",
        Rationale =
            "Write-barrier cost under mutation-heavy pointer updates, plus post-evacuation locality. " +
            "Deliberately paired with low-allocation-compute: that scenario has few stores per unit of " +
            "work and this one has many, so the pair separates per-store barrier cost from per-object " +
            "cost. The dependent-load traversal also makes locality visible, which is exactly what an " +
            "evacuating collector alters when it moves objects.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 500,
        ProvisionalBaselineHeapBytes = 256L * 1024 * 1024,
        Axes = [
            "nodeCount",
            "hops",
            "mutationsPerOperation",
            "allocationBytesPerOperation",
        ],
    };

    public void Setup(ScenarioContext context)
    {
        int nodeCount = context.Parameters.GetInt32("nodeCount", 1 << 18);
        _hops = context.Parameters.GetInt32("hops", 64);
        _mutationsPerOperation = context.Parameters.GetInt32("mutationsPerOperation", 16);
        _allocationBytesPerOperation = Math.Max(
            0,
            context.Parameters.GetInt32("allocationBytesPerOperation", 0));

        _nodes = new RefNode[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            _nodes[i] = new RefNode { Payload = i };
        }

        // A random permutation cycle: every node is visited exactly once before any repeats, so the
        // traversal is a genuine dependent-load chain rather than a prefetchable stride.
        var random = new Random(context.Seed);
        int[] permutation = new int[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            permutation[i] = i;
        }

        for (int i = nodeCount - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (permutation[i], permutation[j]) = (permutation[j], permutation[i]);
        }

        for (int i = 0; i < nodeCount; i++)
        {
            _nodes[permutation[i]].Next = _nodes[permutation[(i + 1) % nodeCount]];
            _nodes[i].Alt = _nodes[random.Next(nodeCount)];
        }

        _operations = 0;
        _hopsPerformed = 0;
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        RefNode[] nodes = _nodes;
        RefNode current = nodes[(int)(index % nodes.Length)];
        long accumulator = index;

        for (int i = 0; i < _hops; i++)
        {
            current = current.Next!;
            accumulator += current.Payload;
        }

        // In-place pointer mutation against the traversal, so barrier work and traversal work happen
        // over the same objects rather than in separate phases.
        for (int i = 0; i < _mutationsPerOperation; i++)
        {
            RefNode node = nodes[(int)((accumulator + i) % nodes.Length)];
            node.Alt = current;
            current = node.Next!;
        }

        if (_allocationBytesPerOperation != 0)
        {
            byte[] pressure = new byte[_allocationBytesPerOperation];
            pressure[0] = unchecked((byte)index);
            _allocationSink = pressure;
            accumulator += pressure[0];
        }

        Interlocked.Add(ref _hopsPerformed, _hops);
        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        var violations = new List<string>();

        // The permutation cycle must still be a single cycle covering every node; a broken link would
        // silently shorten the traversal and inflate the throughput number.
        int visited = 0;
        RefNode start = _nodes[0];
        RefNode? cursor = start;
        while (cursor is not null && visited <= _nodes.Length)
        {
            visited++;
            cursor = cursor.Next;
            if (ReferenceEquals(cursor, start))
            {
                break;
            }
        }

        if (visited != _nodes.Length)
        {
            violations.Add($"traversal cycle covers {visited} of {_nodes.Length} nodes; the chain was broken so hop counts are not comparable");
        }

        return new ScenarioVerification
        {
            Success = operations >= 500 && violations.Count == 0,
            Marker = $"pointer-chasing:hops={Interlocked.Read(ref _hopsPerformed)}",
            Detail =
                $"operations={operations}, cycleLength={visited}, " +
                $"allocationBytesPerOperation={_allocationBytesPerOperation}",
            Violations = violations,
        };
    }

    public void Teardown()
    {
        _nodes = [];
        _allocationSink = null;
    }
}

/// <summary>
/// Scenario 10 - large object heap and pinned object heap pressure.
///
/// <para>Distinct from the long-lived cache because size class, not lifetime, drives placement: an
/// allocation of 85 000 bytes or more goes to the large object heap, which is not compacted by
/// default. The pinned object heap is pinned by construction, which is why this scenario overlaps
/// deliberately with the pinning scenario and ledger row R05.</para>
/// </summary>
public sealed class LargeObjectPressureScenario : IScenario
{
    private const int LohThresholdBytes = 85_000;

    private byte[]?[] _retained = [];
    private int _smallSizeBytes;
    private int _largeSizeBytes;
    private int _allocationsPerOperation;
    private double _largeFraction;
    private long _operations;
    private long _largeAllocations;
    private long _pinnedAllocations;
    private bool _usePinnedHeap;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "large-object-pressure",
        Rationale =
            "Large object heap and pinned object heap behaviour. Allocations of 85 000 bytes or more " +
            "are placed by size class rather than by lifetime and land on a heap that is not compacted " +
            "by default, which is a different reclamation and fragmentation regime from anything the " +
            "other scenarios reach. Sizes straddle the threshold deliberately so both sides are " +
            "exercised in one run, and the pinned object heap variant overlaps with ledger row R05.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 200,
        ProvisionalBaselineHeapBytes = 1024L * 1024 * 1024,
        Axes = ["largeSizeBytes", "largeFraction", "usePinnedHeap"],
    };

    public void Setup(ScenarioContext context)
    {
        // Straddle the threshold: just under, and comfortably over.
        _smallSizeBytes = context.Parameters.GetInt32("smallSizeBytes", LohThresholdBytes - 1_000);
        _largeSizeBytes = context.Parameters.GetInt32("largeSizeBytes", LohThresholdBytes + 15_000);
        _allocationsPerOperation = context.Parameters.GetInt32("allocationsPerOperation", 4);
        _largeFraction = Math.Clamp(context.Parameters.GetDouble("largeFraction", 0.5), 0.0, 1.0);
        _usePinnedHeap = context.Parameters.GetBoolean("usePinnedHeap", false);
        _retained = new byte[]?[context.Parameters.GetInt32("retainedSlots", 256)];
        _operations = 0;
        _largeAllocations = 0;
        _pinnedAllocations = 0;
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        long accumulator = index;
        int largeEvery = _largeFraction <= 0 ? int.MaxValue : Math.Max(1, (int)(1.0 / _largeFraction));

        for (int i = 0; i < _allocationsPerOperation; i++)
        {
            bool large = i % largeEvery == 0;
            int size = large ? _largeSizeBytes : _smallSizeBytes;

            byte[] buffer;
            if (large && _usePinnedHeap)
            {
                // The pinned object heap produces immovable objects by construction, which is the
                // capability LXR declares unsupported (ledger row R05) and which .NET cannot opt out of.
                buffer = GC.AllocateArray<byte>(size, pinned: true);
                Interlocked.Increment(ref _pinnedAllocations);
            }
            else
            {
                buffer = GC.AllocateUninitializedArray<byte>(size);
            }

            buffer[0] = (byte)index;
            buffer[^1] = (byte)i;
            accumulator += buffer.Length;

            if (large)
            {
                Interlocked.Increment(ref _largeAllocations);
                _retained[(int)((index + i) % _retained.Length)] = buffer;
            }
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        long large = Interlocked.Read(ref _largeAllocations);
        var violations = new List<string>();

        if (operations > 0 && _largeFraction > 0 && large == 0)
        {
            violations.Add("no large-object allocations were made despite a non-zero largeFraction");
        }

        if (_largeSizeBytes < LohThresholdBytes)
        {
            violations.Add($"largeSizeBytes {_largeSizeBytes} is below the {LohThresholdBytes}-byte large object threshold, so the scenario would not exercise the large object heap");
        }

        return new ScenarioVerification
        {
            Success = operations >= 200 && violations.Count == 0,
            Marker = $"large-object-pressure:large={large}",
            Detail = $"operations={operations}, largeAllocations={large}, pinnedAllocations={Interlocked.Read(ref _pinnedAllocations)}, " +
                $"smallSizeBytes={_smallSizeBytes.ToString(CultureInfo.InvariantCulture)}, largeSizeBytes={_largeSizeBytes.ToString(CultureInfo.InvariantCulture)}",
            Violations = violations,
        };
    }

    public void Teardown() => _retained = [];
}
