// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Lxr.Harness.Core;

/// <summary>
/// One operation's timing triple. This is a struct held in a pre-sized array so that the
/// measurement machinery itself allocates nothing inside the measured region - the harness must not
/// perturb the collector it is measuring.
/// </summary>
public struct OperationRecord
{
    /// <summary>When the operation was <em>scheduled</em> to start, fixed before the run began.</summary>
    public long IntendedTimestamp;

    /// <summary>When a worker actually picked the operation up.</summary>
    public long ServiceStartTimestamp;

    /// <summary>
    /// When the dispatcher actually enqueued the operation. The gap to <see cref="IntendedTimestamp"/>
    /// is the harness's own scheduling error, and it is recorded so it can be told apart from queueing
    /// caused by the system under test. Both delay an operation; only the second is a measurement.
    /// </summary>
    public long DispatchTimestamp;

    public long EndTimestamp;

    /// <summary>Value returned by the scenario, folded into the run checksum.</summary>
    public long Value;

    /// <summary>0 = warmup, 1 = steady state. Retained so the boundary can be re-analysed later.</summary>
    public int Phase;
}

public enum ArrivalDistribution
{
    Uniform,
    Poisson,
}

/// <summary>
/// A deliberately injected stall, used by control 2 to prove the latency pipeline is free of
/// coordinated omission. It emulates a stop-the-world pause by blocking every worker at once while
/// leaving the arrival schedule untouched - which is exactly how a real GC pause behaves relative to
/// clients that keep sending requests.
/// </summary>
public sealed class StallInjection
{
    public required double PeriodSeconds { get; init; }

    public required double DurationMilliseconds { get; init; }
}

public sealed class OpenLoopOptions
{
    public required double ArrivalRatePerSecond { get; init; }

    public required double WarmupSeconds { get; init; }

    public required double SteadyStateSeconds { get; init; }

    public ArrivalDistribution Distribution { get; init; } = ArrivalDistribution.Poisson;

    public int Seed { get; init; } = 20040;

    public int WorkerCount { get; init; } = 1;

    public int QueueCapacity { get; init; } = 1 << 16;

    public StallInjection? Stall { get; init; }

    /// <summary>
    /// Fractional shortfall in achieved arrival rate beyond which the run is marked overloaded. An
    /// overloaded open-loop run measures the capacity limit, not latency, and must not be compared.
    /// </summary>
    public double OverloadTolerance { get; init; } = 0.02;
}

public sealed class MeasuredRun : IDisposable
{
    public required NativeBuffer<OperationRecord> Records { get; init; }

    /// <summary>
    /// Bytes the harness holds off the GC heap for this run. Published so that a working-set figure
    /// which includes the apparatus can still be read as a statement about the workload.
    /// </summary>
    public required long ApparatusBytes { get; init; }

    public required int RecordCount { get; init; }

    public required double RequestedRatePerSecond { get; init; }

    public required double AchievedRatePerSecond { get; init; }

    public required long LateCount { get; init; }

    public required int BacklogMax { get; init; }

    public required bool Overloaded { get; init; }

    public required long Checksum { get; init; }

    public required double WallSeconds { get; init; }

    public required int SteadyStateCount { get; init; }

    public void Dispose() => Records.Dispose();
}

/// <summary>
/// Open-loop, coordinated-omission-free load driver.
///
/// The arrival schedule is computed in full <em>before</em> the run starts, so it cannot be deformed
/// by the system under test being slow. Reported latency is <c>End - IntendedTimestamp</c>. A
/// closed-loop driver - which is what BenchmarkDotNet and most request-then-wait loops implement -
/// would instead report <c>End - ServiceStart</c> and structurally hide exactly the stalls a GC
/// study exists to find. Both are retained here so the difference is measurable rather than
/// asserted; see control 2 in P0.4-harness.md.
/// </summary>
public static class OpenLoopDriver
{
    /// <summary>
    /// The value recorded in <c>latencyMethod</c>. It is a constant rather than a literal at each
    /// emission site because a record whose latency method is absent or misspelled has to be treated
    /// as not coordinated-omission free, and a typo would quietly demote real data.
    /// </summary>
    public const string LatencyMethod = "open-loop-intended-start";

    public static MeasuredRun Run(IScenario scenario, OpenLoopOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.ArrivalRatePerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WorkerCount);

        double totalSeconds = options.WarmupSeconds + options.SteadyStateSeconds;
        int totalOperations = checked((int)(options.ArrivalRatePerSecond * totalSeconds));
        if (totalOperations <= 0)
        {
            throw new ArgumentException("Arrival rate and duration produce no operations.", nameof(options));
        }

        var records = new NativeBuffer<OperationRecord>(totalOperations);
        using var offsets = new NativeBuffer<long>(totalOperations);
        BuildSchedule(offsets, totalOperations, options);
        long warmupOffset = (long)(options.WarmupSeconds * Stopwatch.Frequency);

        for (int i = 0; i < totalOperations; i++)
        {
            records[i].Phase = offsets[i] < warmupOffset ? 0 : 1;
        }

        var queue = new IndexQueue(options.QueueCapacity);
        var stall = new StallController(options.Stall);
        long lateCount = 0;

        Thread[] workers = new Thread[options.WorkerCount];
        for (int w = 0; w < workers.Length; w++)
        {
            int workerIndex = w;
            workers[w] = new Thread(() => WorkerLoop(scenario, records, queue, stall, workerIndex))
            {
                IsBackground = true,
                Name = $"lxr-harness-worker-{workerIndex}",
            };
        }

        // Everything above this line allocates; nothing below it does until the run is over.
        long start = Stopwatch.GetTimestamp();
        stall.Start(start);
        foreach (Thread thread in workers)
        {
            thread.Start();
        }

        for (int i = 0; i < totalOperations; i++)
        {
            long target = start + offsets[i];
            records[i].IntendedTimestamp = target;

            long now = Stopwatch.GetTimestamp();
            if (now < target)
            {
                WaitUntil(target);
            }
            else if (i > 0)
            {
                // The schedule slot had already elapsed when we got here: dispatch immediately and
                // count it. We never skip a slot and never re-time one - that is what coordinated
                // omission is.
                lateCount++;
            }

            records[i].DispatchTimestamp = Stopwatch.GetTimestamp();
            queue.Enqueue(i);
        }

        queue.CompleteAdding();
        foreach (Thread thread in workers)
        {
            thread.Join();
        }

        stall.Stop();
        long end = Stopwatch.GetTimestamp();

        double wallSeconds = (end - start) / (double)Stopwatch.Frequency;
        double achievedRate = totalOperations / wallSeconds;
        bool overloaded = achievedRate < options.ArrivalRatePerSecond * (1.0 - options.OverloadTolerance);

        long checksum = 0;
        int steadyCount = 0;
        for (int i = 0; i < totalOperations; i++)
        {
            checksum = unchecked((checksum * 31) + records[i].Value);
            if (records[i].Phase == 1)
            {
                steadyCount++;
            }
        }

        return new MeasuredRun
        {
            Records = records,
            ApparatusBytes = records.ByteCount + offsets.ByteCount,
            RecordCount = totalOperations,
            RequestedRatePerSecond = options.ArrivalRatePerSecond,
            AchievedRatePerSecond = achievedRate,
            LateCount = lateCount,
            BacklogMax = queue.MaxDepth,
            Overloaded = overloaded,
            Checksum = checksum,
            WallSeconds = wallSeconds,
            SteadyStateCount = steadyCount,
        };
    }

    private static void WorkerLoop(IScenario scenario, NativeBuffer<OperationRecord> records, IndexQueue queue, StallController stall, int workerIndex)
    {
        while (queue.TryDequeue(out int index))
        {
            stall.WaitIfStalled();
            records[index].ServiceStartTimestamp = Stopwatch.GetTimestamp();
            records[index].Value = scenario.RunOperation(workerIndex);
            records[index].EndTimestamp = Stopwatch.GetTimestamp();
        }
    }

    private static void BuildSchedule(NativeBuffer<long> offsets, int count, OpenLoopOptions options)
    {
        double ticksPerSecond = Stopwatch.Frequency;
        double meanInterarrival = 1.0 / options.ArrivalRatePerSecond;

        if (options.Distribution == ArrivalDistribution.Uniform)
        {
            for (int i = 0; i < count; i++)
            {
                offsets[i] = (long)(i * meanInterarrival * ticksPerSecond);
            }

            return;
        }

        var random = new Random(options.Seed);
        double cumulative = 0;
        for (int i = 0; i < count; i++)
        {
            offsets[i] = (long)(cumulative * ticksPerSecond);
            // Inverse-transform sampling of the exponential distribution. NextDouble() is in [0,1),
            // so 1 - u is in (0,1] and the logarithm is always defined.
            cumulative += -Math.Log(1.0 - random.NextDouble()) * meanInterarrival;
        }
    }

    /// <summary>
    /// The margin, in milliseconds, left unslept before a scheduled dispatch. It must exceed the
    /// coarsest timer tick the host can round a sleep up to, so that a sleep can overshoot by a full
    /// tick and still land before the deadline with the remainder spun out.
    /// </summary>
    /// <remarks>
    /// Windows' default timer resolution is 15.625 ms, and <see cref="Thread.Sleep(int)"/> rounds up to
    /// it: <c>Sleep(1)</c> routinely returns after 15 ms. P0.5's first latency matrix asked for 1000
    /// op/s - a 1 ms mean gap - and measured a p99 dispatch lag of 11 ms with a 13.2 ms maximum, which
    /// is that tick and not the collector. The margin is not lowered towards the tick: sleeping
    /// <c>remaining - 20 ms</c> can overshoot by 15.625 ms and still leave 4 ms to spin.
    /// <para>
    /// Raising the system timer resolution with <c>timeBeginPeriod</c> was rejected. It would change
    /// thread scheduling and timer-driven behaviour inside the runtime being measured, which for a
    /// collector benchmark means perturbing the subject to instrument it.
    /// </para>
    /// </remarks>
    internal const double CoarseSleepMarginMs = 20.0;

    internal static void WaitUntil(long targetTimestamp)
    {
        while (true)
        {
            long remaining = targetTimestamp - Stopwatch.GetTimestamp();
            if (remaining <= 0)
            {
                return;
            }

            double remainingMs = remaining * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > CoarseSleepMarginMs)
            {
                Thread.Sleep((int)(remainingMs - CoarseSleepMarginMs));
            }
            else
            {
                // Spinning burns the dispatcher thread for up to the margin. That cost is accepted and
                // symmetric across arms; delivering the schedule late is not.
                Thread.SpinWait(40);
            }
        }
    }
}

/// <summary>
/// Bounded queue of operation indices. Pre-allocated; enqueue and dequeue perform no allocation.
/// </summary>
internal sealed class IndexQueue
{
    private readonly int[] _buffer;
    private readonly object _lock = new();
    private int _head;
    private int _tail;
    private int _count;
    private int _maxDepth;
    private bool _completed;

    internal IndexQueue(int capacity) => _buffer = new int[capacity];

    internal int MaxDepth => Volatile.Read(ref _maxDepth);

    internal void Enqueue(int value)
    {
        lock (_lock)
        {
            while (_count == _buffer.Length)
            {
                Monitor.Wait(_lock);
            }

            _buffer[_tail] = value;
            _tail = (_tail + 1) % _buffer.Length;
            _count++;
            if (_count > _maxDepth)
            {
                _maxDepth = _count;
            }

            Monitor.PulseAll(_lock);
        }
    }

    internal bool TryDequeue(out int value)
    {
        lock (_lock)
        {
            while (_count == 0)
            {
                if (_completed)
                {
                    value = 0;
                    return false;
                }

                Monitor.Wait(_lock);
            }

            value = _buffer[_head];
            _head = (_head + 1) % _buffer.Length;
            _count--;
            Monitor.PulseAll(_lock);
            return true;
        }
    }

    internal void CompleteAdding()
    {
        lock (_lock)
        {
            _completed = true;
            Monitor.PulseAll(_lock);
        }
    }
}

/// <summary>
/// Drives <see cref="StallInjection"/>. A separate thread raises a deadline that every worker
/// observes, so all workers stall together, as they would across a stop-the-world pause.
/// </summary>
internal sealed class StallController
{
    private readonly StallInjection? _injection;
    private Thread? _thread;
    private long _stallUntil;
    private volatile bool _running;

    internal StallController(StallInjection? injection) => _injection = injection;

    internal void Start(long startTimestamp)
    {
        if (_injection is null)
        {
            return;
        }

        _running = true;
        long period = (long)(_injection.PeriodSeconds * Stopwatch.Frequency);
        long duration = (long)(_injection.DurationMilliseconds / 1000.0 * Stopwatch.Frequency);

        _thread = new Thread(() =>
        {
            long next = startTimestamp + period;
            while (_running)
            {
                OpenLoopDriver.WaitUntil(next);
                if (!_running)
                {
                    return;
                }

                Volatile.Write(ref _stallUntil, Stopwatch.GetTimestamp() + duration);
                next += period;
            }
        })
        {
            IsBackground = true,
            Name = "lxr-harness-stall",
        };

        _thread.Start();
    }

    internal void WaitIfStalled()
    {
        if (_injection is null)
        {
            return;
        }

        long until = Volatile.Read(ref _stallUntil);
        if (until > Stopwatch.GetTimestamp())
        {
            OpenLoopDriver.WaitUntil(until);
        }
    }

    internal void Stop()
    {
        _running = false;
        _thread?.Join(TimeSpan.FromSeconds(5));
    }
}
