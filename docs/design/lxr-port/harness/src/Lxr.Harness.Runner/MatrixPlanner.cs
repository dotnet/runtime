// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>One cell of the matrix: a scenario, on an arm, at a heap setting, on a host.</summary>
public sealed class MatrixCell
{
    public required string Scenario { get; init; }

    public required CollectorArm Arm { get; init; }

    public required HostDescriptor Host { get; init; }

    public required PrimaryMetric Primary { get; init; }

    public double? HeapFactor { get; init; }

    public long? HeapLimitMb { get; init; }

    public required int Invocations { get; init; }

    public required int TimeoutSeconds { get; init; }

    /// <summary>
    /// Distinguishes cells that share a scenario, arm, host and heap setting but must not share output
    /// files. Without it the control demonstrations collide: they all exercise the same scenario on the
    /// same arm, so their reports, samples and dump files land on the same paths and a leftover artefact
    /// from one control is attributed to the next.
    /// </summary>
    public string? Tag { get; init; }

    public string Id =>
        $"{(Tag is null ? string.Empty : Tag + ".")}{Scenario}.{Arm.Id}.{Host.Id}.{(HeapFactor is double f ? "h" + f.ToString("0.0#", CultureInfo.InvariantCulture) : "hdefault")}";
}

/// <summary>
/// Builds the run order.
///
/// <para>Invocations of different arms are interleaved rather than run in blocks. This machine is a
/// shared Azure VM, not a quiesced benchmarking host, so thermal state and unrelated background work
/// drift over the length of a matrix; running all of arm A and then all of arm B would let that drift
/// masquerade as a difference between collectors. Alternating means drift hits both arms about
/// equally, and the interleave order is seeded so a suspicious result can be replayed exactly.</para>
/// </summary>
public static class MatrixPlanner
{
    /// <summary>Heap factors mirroring the paper's own axis. P0.2 established that LXR's throughput
    /// relative to G1 <em>inverts</em> with heap generosity (0.97 at 1.3x, 0.96 at 2x, 1.01 at 6x), so
    /// a single heap size would produce a result that is true only by accident.</summary>
    public static readonly double[] PaperHeapFactors = [1.3, 2.0, 6.0];

    public static List<MatrixCell> Expand(
        IReadOnlyList<string> scenarios,
        IReadOnlyList<CollectorArm> arms,
        IReadOnlyList<HostDescriptor> hosts,
        IReadOnlyList<double>? heapFactors,
        int invocations,
        Func<string, int> timeoutForScenario,
        Func<string, PrimaryMetric> primaryForScenario,
        Func<string, double, long>? heapLimitForScenario = null)
    {
        var cells = new List<MatrixCell>();
        foreach (HostDescriptor host in hosts)
        {
            foreach (string scenario in scenarios)
            {
                foreach (CollectorArm arm in arms)
                {
                    if (heapFactors is null || heapFactors.Count == 0)
                    {
                        cells.Add(new MatrixCell
                        {
                            Scenario = scenario,
                            Arm = arm,
                            Host = host,
                            Primary = primaryForScenario(scenario),
                            Invocations = invocations,
                            TimeoutSeconds = timeoutForScenario(scenario),
                        });
                        continue;
                    }

                    foreach (double factor in heapFactors)
                    {
                        cells.Add(new MatrixCell
                        {
                            Scenario = scenario,
                            Arm = arm,
                            Host = host,
                            Primary = primaryForScenario(scenario),
                            HeapFactor = factor,
                            HeapLimitMb = heapLimitForScenario?.Invoke(scenario, factor),
                            Invocations = invocations,
                            TimeoutSeconds = timeoutForScenario(scenario),
                        });
                    }
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// Produces the interleaved invocation order: for each (scenario, host, heap) group, invocation
    /// <c>i</c> of every arm runs before invocation <c>i+1</c> of any arm, and the arm order within
    /// each round is shuffled with a seeded RNG so a systematic first-position advantage cannot
    /// accumulate.
    /// </summary>
    public static List<(MatrixCell Cell, int Invocation)> InterleavedOrder(IReadOnlyList<MatrixCell> cells, int seed)
    {
        var groups = new Dictionary<string, List<MatrixCell>>(StringComparer.Ordinal);
        var groupOrder = new List<string>();
        foreach (MatrixCell cell in cells)
        {
            string key = $"{cell.Host.Id}|{cell.Scenario}|{cell.HeapFactor}";
            if (!groups.TryGetValue(key, out List<MatrixCell>? group))
            {
                group = [];
                groups[key] = group;
                groupOrder.Add(key);
            }

            group.Add(cell);
        }

        var random = new Random(seed);
        var order = new List<(MatrixCell, int)>();
        foreach (string key in groupOrder)
        {
            List<MatrixCell> group = groups[key];
            int rounds = 0;
            foreach (MatrixCell cell in group)
            {
                rounds = Math.Max(rounds, cell.Invocations);
            }

            for (int round = 0; round < rounds; round++)
            {
                var shuffled = new List<MatrixCell>(group);
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                foreach (MatrixCell cell in shuffled)
                {
                    if (round < cell.Invocations)
                    {
                        order.Add((cell, round));
                    }
                }
            }
        }

        return order;
    }
}
