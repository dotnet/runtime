// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;

namespace Lxr.Harness.Core;

public sealed class ThroughputOptions
{
    public required double WarmupSeconds { get; init; }

    public required double SteadyStateSeconds { get; init; }

    public int WorkerCount { get; init; } = 1;

    /// <summary>
    /// Operations timed as a single block. Fixed-work quanta amortise per-invocation overhead into a
    /// constant, which is what buys back the calibration machinery BenchmarkDotNet would have
    /// supplied. See "Why not BenchmarkDotNet" in P0.4-harness.md.
    /// </summary>
    public int OperationsPerQuantum { get; init; } = 4096;

    /// <summary>
    /// Every Nth operation is performed twice, the second result discarded, producing a slowdown of
    /// close to 1/(N+1) in units of the scenario's own work. Control 7 uses this to measure the
    /// smallest effect this machine can actually resolve. Zero disables it.
    /// </summary>
    /// <remarks>
    /// The injection is deliberately shaped so that both arms execute the same instruction sequence
    /// and differ only in whether the branch is taken: a counter compared against a threshold, never
    /// a modulo. An earlier revision used <c>counter % n == 0</c>, which put a 64-bit integer
    /// division on the slowed arm's hot path and nowhere on the baseline's, and inflated a nominal
    /// 1.96% injection to a measured 4.6%. The instrumentation must not be part of what is measured.
    /// </remarks>
    public int InjectExtraWorkEveryNth { get; init; }
}

public sealed class ThroughputRun
{
    public required double OperationsPerSecond { get; init; }

    public required long SteadyOperations { get; init; }

    public required double SteadySeconds { get; init; }

    public required long Checksum { get; init; }

    public required int Quanta { get; init; }

    /// <summary>Per-quantum operations-per-second over the steady-state phase, retained raw.</summary>
    public required double[] QuantumRates { get; init; }
}

/// <summary>
/// Closed-loop, maximum-rate driver used for throughput-primary scenarios. It deliberately does not
/// report latency: a closed-loop measurement of latency is exactly the coordinated-omission mistake
/// this harness exists to avoid. Latency always comes from <see cref="OpenLoopDriver"/>.
/// </summary>
public static class ThroughputDriver
{
    public static ThroughputRun Run(IScenario scenario, ThroughputOptions options)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.WorkerCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.OperationsPerQuantum);

        long warmupTicks = (long)(options.WarmupSeconds * Stopwatch.Frequency);
        long steadyTicks = (long)(options.SteadyStateSeconds * Stopwatch.Frequency);

        int workerCount = options.WorkerCount;
        long[] checksums = new long[workerCount];
        long[] steadyOperations = new long[workerCount];
        var quantumRates = new System.Collections.Generic.List<double>(capacity: 1024);
        object rateLock = new();

        using var barrier = new Barrier(workerCount);
        long start = Stopwatch.GetTimestamp();
        long steadyStart = start + warmupTicks;
        long deadline = steadyStart + steadyTicks;

        Thread[] threads = new Thread[workerCount];
        for (int w = 0; w < workerCount; w++)
        {
            int workerIndex = w;
            threads[w] = new Thread(() =>
            {
                long localChecksum = 0;
                long localSteady = 0;
                long counter = 0;
                long injectStride = options.InjectExtraWorkEveryNth > 0 ? options.InjectExtraWorkEveryNth : long.MaxValue;
                long nextInject = injectStride;
                barrier.SignalAndWait();

                while (true)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (now >= deadline)
                    {
                        break;
                    }

                    bool steady = now >= steadyStart;
                    long quantumStart = now;
                    for (int i = 0; i < options.OperationsPerQuantum; i++)
                    {
                        long value = scenario.RunOperation(workerIndex);
                        localChecksum = unchecked((localChecksum * 31) + value);

                        counter++;
                        if (counter >= nextInject)
                        {
                            nextInject += injectStride;
                            scenario.RunOperation(workerIndex);
                        }
                    }

                    long quantumEnd = Stopwatch.GetTimestamp();
                    if (steady)
                    {
                        localSteady += options.OperationsPerQuantum;
                        double seconds = (quantumEnd - quantumStart) / (double)Stopwatch.Frequency;
                        if (seconds > 0)
                        {
                            lock (rateLock)
                            {
                                quantumRates.Add(options.OperationsPerQuantum / seconds);
                            }
                        }
                    }
                }

                checksums[workerIndex] = localChecksum;
                steadyOperations[workerIndex] = localSteady;
            })
            {
                IsBackground = true,
                Name = $"lxr-harness-throughput-{workerIndex}",
            };
        }

        foreach (Thread thread in threads)
        {
            thread.Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        long end = Stopwatch.GetTimestamp();
        double steadySeconds = (end - Math.Min(steadyStart, end)) / (double)Stopwatch.Frequency;

        long totalSteady = 0;
        long checksum = 0;
        for (int i = 0; i < workerCount; i++)
        {
            totalSteady += steadyOperations[i];
            checksum = unchecked((checksum * 31) + checksums[i]);
        }

        return new ThroughputRun
        {
            OperationsPerSecond = steadySeconds > 0 ? totalSteady / steadySeconds : 0,
            SteadyOperations = totalSteady,
            SteadySeconds = steadySeconds,
            Checksum = checksum,
            Quanta = quantumRates.Count,
            QuantumRates = quantumRates.ToArray(),
        };
    }
}
