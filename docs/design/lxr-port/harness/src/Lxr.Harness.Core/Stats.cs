// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Lxr.Harness.Core;

public readonly struct RatioEstimate
{
    public RatioEstimate(double ratio, double low, double high, int iterations)
    {
        Ratio = ratio;
        Low = low;
        High = high;
        Iterations = iterations;
    }

    public double Ratio { get; }

    public double Low { get; }

    public double High { get; }

    public int Iterations { get; }

    /// <summary>Half-width of the interval as a fraction of the point estimate.</summary>
    public double HalfWidthFraction => Ratio > 0 ? (High - Low) / 2.0 / Ratio : double.NaN;

    /// <summary>Whether the interval excludes 1.0, i.e. the difference is resolvable at all.</summary>
    public bool ExcludesUnity => Low > 1.0 || High < 1.0;
}

/// <summary>
/// Statistics for latency and throughput comparison.
///
/// Latency distributions are not normal, so a t-interval on them is wrong; ratios are reported with
/// a percentile bootstrap confidence interval instead. Cross-scenario roll-ups use the geometric
/// mean because the paper's own summary figures (arXiv:2210.17175, and P0.2-paper-targets.md section
/// 7) are geometric means - an arithmetic mean would silently disagree with the target it is being
/// compared against.
/// </summary>
public static class Stats
{
    public const int DefaultBootstrapIterations = 10_000;

    /// <summary>Nearest-rank percentile over an ascending-sorted sample.</summary>
    public static double Percentile(double[] sortedAscending, double percentile)
    {
        ArgumentNullException.ThrowIfNull(sortedAscending);
        return Percentile((ReadOnlySpan<double>)sortedAscending, percentile);
    }

    /// <summary>
    /// Nearest-rank percentile over an ascending-sorted sample. The span overload exists because the
    /// latency arrays live off the GC heap; it carries the algorithm and the array overload forwards
    /// to it, so there is exactly one definition of a published percentile.
    /// </summary>
    public static double Percentile(ReadOnlySpan<double> sortedAscending, double percentile)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percentile);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentile, 100);

        if (sortedAscending.Length == 0)
        {
            return double.NaN;
        }

        int rank = (int)Math.Ceiling(percentile / 100.0 * sortedAscending.Length);
        if (rank < 1)
        {
            rank = 1;
        }

        if (rank > sortedAscending.Length)
        {
            rank = sortedAscending.Length;
        }

        return sortedAscending[rank - 1];
    }

    public static double Mean(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return Mean((ReadOnlySpan<double>)values);
    }

    public static double Mean(ReadOnlySpan<double> values)
    {
        if (values.Length == 0)
        {
            return double.NaN;
        }

        double sum = 0;
        foreach (double value in values)
        {
            sum += value;
        }

        return sum / values.Length;
    }

    public static double GeometricMean(double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
        {
            return double.NaN;
        }

        double logSum = 0;
        foreach (double value in values)
        {
            if (value <= 0)
            {
                return double.NaN;
            }

            logSum += Math.Log(value);
        }

        return Math.Exp(logSum / values.Length);
    }

    /// <summary>
    /// Percentile bootstrap confidence interval on the ratio of two arms.
    ///
    /// Resampling is over <em>invocations</em>, not over individual operations: separate process
    /// invocations are the independent experimental unit, while operations inside one invocation
    /// share that process's JIT state, heap layout and scheduling luck, so resampling them would
    /// understate the interval badly.
    /// </summary>
    public static RatioEstimate BootstrapRatio(
        double[] baselineInvocations,
        double[] candidateInvocations,
        int iterations = DefaultBootstrapIterations,
        int seed = 20040,
        double confidenceLevel = 0.95)
    {
        ArgumentNullException.ThrowIfNull(baselineInvocations);
        ArgumentNullException.ThrowIfNull(candidateInvocations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baselineInvocations.Length);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateInvocations.Length);

        double point = Mean(candidateInvocations) / Mean(baselineInvocations);

        double[] ratios = new double[iterations];
        var random = new Random(seed);
        for (int i = 0; i < iterations; i++)
        {
            double baselineSum = 0;
            for (int j = 0; j < baselineInvocations.Length; j++)
            {
                baselineSum += baselineInvocations[random.Next(baselineInvocations.Length)];
            }

            double candidateSum = 0;
            for (int j = 0; j < candidateInvocations.Length; j++)
            {
                candidateSum += candidateInvocations[random.Next(candidateInvocations.Length)];
            }

            double baselineMean = baselineSum / baselineInvocations.Length;
            ratios[i] = baselineMean == 0 ? double.NaN : (candidateSum / candidateInvocations.Length) / baselineMean;
        }

        Array.Sort(ratios);
        double tail = (1.0 - confidenceLevel) / 2.0 * 100.0;
        return new RatioEstimate(point, Percentile(ratios, tail), Percentile(ratios, 100.0 - tail), iterations);
    }
}
