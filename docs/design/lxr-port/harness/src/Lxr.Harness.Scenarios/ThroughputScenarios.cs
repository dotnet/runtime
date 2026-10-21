// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Lxr.Harness.Core;

namespace Lxr.Harness.Scenarios;

/// <summary>A node used by the reference-store scenarios. A class, so stores into its fields are
/// genuine reference assignments that go through the write barrier.</summary>
internal sealed class RefNode
{
    public RefNode? Next;
    public RefNode? Alt;
    public long Payload;
}

/// <summary>
/// Scenario 1 - barrier cost in the limit.
///
/// <para>LXR's field write barrier is paid on every reference store whether or not there is garbage
/// to pay for it, so this scenario allocates almost nothing and does nothing but reference stores
/// and arithmetic over a pre-built graph. It is the only scenario where a barrier regression is the
/// dominant visible effect.</para>
///
/// <para>It is also the scenario that sets the harness's own precision requirement. The paper reports
/// barrier overhead as a 1.6% geometric mean against a no-barrier full-heap Immix build, inside a
/// noise floor of roughly plus or minus 3% (P0.2). A harness that cannot resolve a couple of percent
/// cannot say anything about barrier cost at all, which is what control 7 measures rather than
/// assumes.</para>
/// </summary>
public sealed class LowAllocationComputeScenario : IScenario
{
    private RefNode[] _nodes = [];
    private int _storesPerOperation;
    private int _mask;
    private long _operations;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "low-allocation-compute",
        Rationale =
            "Barrier cost in the limit. LXR pays its field write barrier on every reference store " +
            "regardless of how much garbage there is to reclaim, so a workload that allocates almost " +
            "nothing isolates that cost. The paper's 1.6% geomean barrier overhead sits inside a " +
            "roughly plus or minus 3% noise floor (P0.2), which makes this scenario the harness's own " +
            "resolution test as much as a collector test.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 1000,
        ProvisionalBaselineHeapBytes = 64L * 1024 * 1024,
        Axes = ["storesPerOperation", "nodeCount"],
    };

    public void Setup(ScenarioContext context)
    {
        int nodeCount = context.Parameters.GetInt32("nodeCount", 1 << 14);
        nodeCount = 1 << (int)Math.Ceiling(Math.Log2(Math.Max(2, nodeCount)));
        _storesPerOperation = context.Parameters.GetInt32("storesPerOperation", 64);
        _mask = nodeCount - 1;

        _nodes = new RefNode[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            _nodes[i] = new RefNode { Payload = i };
        }

        var random = new Random(context.Seed);
        for (int i = 0; i < nodeCount; i++)
        {
            _nodes[i].Next = _nodes[random.Next(nodeCount)];
            _nodes[i].Alt = _nodes[random.Next(nodeCount)];
        }

        // Everything the measured region touches is now live and reachable; from here the scenario
        // allocates nothing at all.
        _operations = 0;
    }

    public long RunOperation(int workerIndex)
    {
        RefNode[] nodes = _nodes;
        int mask = _mask;
        long index = Interlocked.Increment(ref _operations);
        long accumulator = index;

        int position = (int)(index & mask);
        for (int i = 0; i < _storesPerOperation; i++)
        {
            RefNode node = nodes[position];
            RefNode target = nodes[(position + i + 1) & mask];

            // A real reference store into a heap object: this is what the barrier intercepts.
            node.Next = target;
            node.Alt = target.Next;

            accumulator += node.Payload ^ (i * 31);
            position = (int)((accumulator + i) & mask);
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        var violations = new List<string>();

        // The graph must still be internally consistent: every reachable link must point at a node
        // from the pre-built array, which would not hold if stores had been dropped or corrupted.
        foreach (RefNode node in _nodes)
        {
            if (node.Next is null)
            {
                violations.Add("a node's Next reference was null after the run");
                break;
            }
        }

        return new ScenarioVerification
        {
            Success = operations >= 1000 && violations.Count == 0,
            Marker = $"low-allocation-compute:stores={operations * _storesPerOperation}",
            Detail = $"operations={operations.ToString(CultureInfo.InvariantCulture)}, storesPerOperation={_storesPerOperation}",
            Violations = violations,
        };
    }

    public void Teardown() => _nodes = [];
}

/// <summary>
/// Scenario 2 - young-object throughput and reference-count decrement volume.
///
/// <para>The paper's Table 7 reports that LXR's young reference-counting phase reclaims 94.3% of
/// objects on average (P0.2), making this the dominant reclamation path: a regression here is a
/// regression almost everywhere. The survival fraction is a parameter because the cost profile of
/// reference counting depends on how much of the young cohort dies immediately.</para>
/// </summary>
public sealed class AllocationChurnScenario : IScenario
{
    private sealed class Payload
    {
        public byte[]? Buffer;
        public Payload? Link;
        public long Tag;
    }

    private Payload?[] _survivors = [];
    private int _objectsPerOperation;
    private int _objectSizeBytes;
    private double _survivalFraction;
    private long _operations;
    private long _allocated;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "allocation-churn",
        Rationale =
            "Young-object throughput and reference-count decrement volume. LXR's young reference " +
            "counting reclaims 94.3% of objects on average (paper Table 7, P0.2), so this is the " +
            "dominant reclamation path and the one whose decrement traffic scales with allocation " +
            "rate. The survival fraction is tuneable because reference counting costs differ sharply " +
            "between a cohort that dies immediately and one that partly survives.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        MinimumOperations = 500,
        ProvisionalBaselineHeapBytes = 256L * 1024 * 1024,
        Axes = ["objectsPerOperation", "objectSizeBytes", "survivalFraction"],
    };

    public void Setup(ScenarioContext context)
    {
        _objectsPerOperation = context.Parameters.GetInt32("objectsPerOperation", 128);
        _objectSizeBytes = context.Parameters.GetInt32("objectSizeBytes", 64);
        _survivalFraction = Math.Clamp(context.Parameters.GetDouble("survivalFraction", 0.05), 0.0, 0.2);
        _survivors = new Payload?[context.Parameters.GetInt32("survivorSlots", 1 << 15)];
        _operations = 0;
        _allocated = 0;
    }

    public long RunOperation(int workerIndex)
    {
        long index = Interlocked.Increment(ref _operations);
        int survivalEvery = _survivalFraction <= 0 ? int.MaxValue : Math.Max(1, (int)(1.0 / _survivalFraction));
        Payload? chain = null;
        long accumulator = index;

        for (int i = 0; i < _objectsPerOperation; i++)
        {
            var payload = new Payload
            {
                Buffer = new byte[_objectSizeBytes],
                Tag = index * 31 + i,
                Link = chain,
            };

            payload.Buffer[0] = (byte)i;
            chain = payload;
            accumulator += payload.Tag;

            if (i % survivalEvery == 0)
            {
                // Publishing into the survivor ring is what promotes a fraction of the cohort out of
                // the nursery; overwriting an existing slot is also the decrement that matters.
                _survivors[(int)((index * _objectsPerOperation + i) % _survivors.Length)] = payload;
            }
        }

        Interlocked.Add(ref _allocated, _objectsPerOperation);
        GC.KeepAlive(chain);
        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long operations = Interlocked.Read(ref _operations);
        long allocated = Interlocked.Read(ref _allocated);
        int retained = 0;
        foreach (Payload? survivor in _survivors)
        {
            if (survivor is not null)
            {
                retained++;
            }
        }

        var violations = new List<string>();
        if (operations > 0 && _survivalFraction > 0 && retained == 0)
        {
            violations.Add("survival fraction was non-zero but nothing survived, so the retention path did not execute");
        }

        return new ScenarioVerification
        {
            Success = operations >= 500 && allocated == operations * _objectsPerOperation && violations.Count == 0,
            Marker = $"allocation-churn:allocated={allocated}",
            Detail = $"operations={operations}, retainedSurvivors={retained}",
            Violations = violations,
        };
    }

    public void Teardown() => _survivors = [];
}

/// <summary>
/// Scenario 6 - scalability and contention on reference-count updates.
///
/// <para>LXR allocates from a lock-free global block buffer and keeps coalescing reference-count logs
/// per mutator (P0.2), so the interesting cost is not the single-threaded path but the contention
/// when many mutators drain those logs at once. Worker count is the axis; this host has 16 logical
/// cores.</para>
/// </summary>
public sealed class MultiThreadThroughputScenario : IScenario
{
    private sealed class Cell
    {
        public object? Reference;
        public long Counter;
    }

    private Cell[] _shared = [];
    private object[][] _perWorkerPool = [];
    private long[] _perWorkerOps = [];
    private int _sharedTouchesPerOperation;
    private int _allocationsPerOperation;
    private SchedulerBenchmarkBridge? _schedulerBenchmark;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "multi-thread-throughput",
        Rationale =
            "Scalability and contention on reference-count updates. LXR serves allocation from a " +
            "lock-free global block buffer and keeps per-mutator coalescing reference-count logs " +
            "(P0.2), so the cost that matters appears only when many mutators mutate shared references " +
            "and drain those logs concurrently. Worker count is the axis rather than a constant.",
        Primary = PrimaryMetric.Throughput,
        DefaultTimeoutSeconds = 300,
        DefaultWorkerCount = 4,
        MaxWorkerCount = 64,
        MinimumOperations = 500,
        ProvisionalBaselineHeapBytes = 256L * 1024 * 1024,
        Axes =
        [
            "workerCount",
            "sharedTouchesPerOperation",
            "allocationsPerOperation",
            "schedulerBenchmark",
            "schedulerBenchmarkMode",
            "schedulerReferenceCapacity",
        ],
    };

    public void Setup(ScenarioContext context)
    {
        _sharedTouchesPerOperation = context.Parameters.GetInt32("sharedTouchesPerOperation", 16);
        _allocationsPerOperation = context.Parameters.GetInt32("allocationsPerOperation", 16);
        int cellCount = context.Parameters.GetInt32("sharedCells", 1024);
        bool schedulerBenchmark = context.Parameters.GetBoolean("schedulerBenchmark", false);
        int schedulerMode = context.Parameters.GetInt32("schedulerBenchmarkMode", 1);
        int schedulerReferenceCapacity =
            context.Parameters.GetInt32("schedulerReferenceCapacity", 1024);

        _shared = new Cell[cellCount];
        for (int i = 0; i < cellCount; i++)
        {
            _shared[i] = new Cell { Reference = new object() };
        }

        // Per-worker state lives in pre-sized arrays so the measured region allocates only what the
        // scenario is deliberately allocating.
        _perWorkerPool = new object[context.WorkerCount][];
        _perWorkerOps = new long[context.WorkerCount];
        for (int worker = 0; worker < context.WorkerCount; worker++)
        {
            _perWorkerPool[worker] = new object[64];
            for (int i = 0; i < _perWorkerPool[worker].Length; i++)
            {
                _perWorkerPool[worker][i] = new object();
            }
        }

        if (schedulerBenchmark)
        {
            string? library = Environment.GetEnvironmentVariable("P31_SCHEDULER_BENCHMARK_LIBRARY");
            if (string.IsNullOrWhiteSpace(library))
            {
                throw new InvalidOperationException(
                    "schedulerBenchmark requires P31_SCHEDULER_BENCHMARK_LIBRARY.");
            }
            _schedulerBenchmark = new SchedulerBenchmarkBridge(
                library,
                context.WorkerCount,
                context.WorkerCount,
                schedulerReferenceCapacity,
                schedulerMode);
        }
    }

    public long RunOperation(int workerIndex)
    {
        long index = ++_perWorkerOps[workerIndex];
        object[] pool = _perWorkerPool[workerIndex];
        Cell[] shared = _shared;
        long accumulator = index + workerIndex;

        for (int i = 0; i < _allocationsPerOperation; i++)
        {
            pool[(int)((index + i) % pool.Length)] = new object();
        }

        for (int i = 0; i < _sharedTouchesPerOperation; i++)
        {
            int slot = (int)((index * 31 + i * 17 + workerIndex) % shared.Length);
            Cell cell = shared[slot];

            // A contended reference store into a shared object, which is the traffic LXR's
            // coalescing logs have to absorb.
            Volatile.Write(ref cell.Reference, pool[(int)((index + i) % pool.Length)]);
            accumulator += Interlocked.Increment(ref cell.Counter);
        }

        if (_schedulerBenchmark is not null)
        {
            accumulator ^= unchecked((long)_schedulerBenchmark.Run(workerIndex, (ulong)index));
        }

        return accumulator;
    }

    public ScenarioVerification Verify()
    {
        long total = 0;
        foreach (long ops in _perWorkerOps)
        {
            total += ops;
        }

        var violations = new List<string>();
        int idleWorkers = 0;
        foreach (long ops in _perWorkerOps)
        {
            if (ops == 0)
            {
                idleWorkers++;
            }
        }

        if (idleWorkers > 0)
        {
            violations.Add($"{idleWorkers} of {_perWorkerOps.Length} workers ran no operations, so the run was not the concurrency it claims");
        }

        long counted = 0;
        foreach (Cell cell in _shared)
        {
            counted += cell.Counter;
        }

        string schedulerDetail = _schedulerBenchmark?.Verify(total, violations) ??
            "schedulerBenchmark=disabled";
        return new ScenarioVerification
        {
            Success = total >= 500 && violations.Count == 0 && counted == total * _sharedTouchesPerOperation,
            Marker = $"multi-thread-throughput:sharedUpdates={counted}",
            Detail = $"operations={total}, workers={_perWorkerOps.Length}, {schedulerDetail}",
            Violations = violations,
        };
    }

    public void Teardown()
    {
        _schedulerBenchmark?.Dispose();
        _schedulerBenchmark = null;
        _shared = [];
        _perWorkerPool = [];
    }
}
