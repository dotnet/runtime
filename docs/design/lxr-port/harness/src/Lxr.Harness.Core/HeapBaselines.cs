// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Lxr.Harness.Core;

/// <summary>
/// One scenario's minimum viable heap, per collector arm, as measured by the <c>calibrate</c> verb.
/// </summary>
/// <remarks>
/// <para>The heap axis is not decoration. P0.2 established from arXiv:2210.17175 that LXR's throughput
/// relative to G1 <em>inverts</em> with heap generosity - 0.97 at 1.3x, 0.96 at 2x, 1.01 at 6x - so a
/// throughput number published without the heap factor that produced it is unusable, and a heap factor
/// is meaningless without a measured minimum heap to be a factor <em>of</em>.</para>
///
/// <para>Both arms' minima are recorded, and <see cref="SharedMinimumMb"/> is the larger of the two.
/// Running each arm at its own minimum would compare two collectors at two different heap sizes, which
/// is not a comparison of collectors at all; running both at the larger minimum is the only choice that
/// keeps the heap axis a controlled variable. The smaller value is kept rather than discarded because
/// the difference between the two <em>is</em> a result - it is how much more heap one collector needs
/// to run the same workload at all.</para>
/// </remarks>
public sealed class HeapBaseline
{
    public required string Scenario { get; init; }

    /// <summary>Smallest heap hard limit, in MiB, at which the Workstation arm completed the viability rule.</summary>
    public required long WorkstationMinimumMb { get; init; }

    /// <summary>Smallest heap hard limit, in MiB, at which the Server arm completed the viability rule.</summary>
    public required long ServerMinimumMb { get; init; }

    /// <summary>
    /// True while these are declared guesses rather than measured minima. A provisional baseline may be
    /// used to shape a run, but a result derived from one must say so - the flag exists so that
    /// "provisional" survives into the published record instead of being lost in a round number.
    /// </summary>
    public required bool Provisional { get; init; }

    /// <summary>Where the numbers came from: which calibration run, or which declaration.</summary>
    public required string Source { get; init; }

    /// <summary>Why calibration stopped where it did, when that needs explaining.</summary>
    public string? Note { get; init; }

    /// <summary>The heap both arms run at, so the heap size is a controlled variable rather than an arm-dependent one.</summary>
    public long SharedMinimumMb => Math.Max(WorkstationMinimumMb, ServerMinimumMb);

    public long MinimumForArm(string armId) =>
        armId.StartsWith("srv", StringComparison.Ordinal) ? ServerMinimumMb : WorkstationMinimumMb;
}

/// <summary>
/// The single machine-readable statement of every scenario's calibrated minimum heap.
/// </summary>
/// <remarks>
/// <para>There is deliberately one copy of these numbers rather than one per scenario class. Two copies
/// cross-checked against each other only prove that a literal was typed twice; the check that has
/// content is between the number published in a result and the calibration trace that produced it,
/// which is committed alongside and which the verification gate re-derives. Duplicating the table
/// would have added a check that cannot fail for the right reason.</para>
/// </remarks>
public static class HeapBaselines
{
    /// <summary>
    /// Used when a scenario has no entry at all, so that a missing calibration is a loud default rather
    /// than a silent zero. It is marked provisional, which is what keeps it out of a published record
    /// that claims to be measured.
    /// </summary>
    public static readonly HeapBaseline Fallback = new()
    {
        Scenario = "(unlisted)",
        WorkstationMinimumMb = 64,
        ServerMinimumMb = 64,
        Provisional = true,
        Source = "declared fallback; no calibration exists for this scenario",
    };

    private static readonly Dictionary<string, HeapBaseline> s_measured = new(StringComparer.Ordinal);

    /// <summary>
    /// Replaces the table with measured values. Called at start-up from the committed calibration file
    /// so the runner never has to be recompiled to carry a measurement, and so the numbers used by a
    /// run and the numbers committed as evidence are the same bytes.
    /// </summary>
    public static void Load(IEnumerable<HeapBaseline> baselines)
    {
        ArgumentNullException.ThrowIfNull(baselines);
        foreach (HeapBaseline baseline in baselines)
        {
            s_measured[baseline.Scenario] = baseline;
        }
    }

    public static void Clear() => s_measured.Clear();

    public static IReadOnlyCollection<HeapBaseline> Loaded => s_measured.Values;

    public static HeapBaseline For(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        return s_measured.TryGetValue(scenario, out HeapBaseline? baseline)
            ? baseline
            : new HeapBaseline
            {
                Scenario = scenario,
                WorkstationMinimumMb = Fallback.WorkstationMinimumMb,
                ServerMinimumMb = Fallback.ServerMinimumMb,
                Provisional = true,
                Source = Fallback.Source,
            };
    }

    /// <summary>
    /// The heap hard limit, in MiB, for a scenario at a heap factor.
    /// </summary>
    /// <remarks>
    /// Rounded up, never down: rounding down could land below the measured minimum and turn a 1.3x cell
    /// into an out-of-memory failure that looks like a collector difference.
    /// </remarks>
    public static long LimitMb(string scenario, double factor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(factor);
        long minimum = For(scenario).SharedMinimumMb;
        return (long)Math.Ceiling(minimum * factor);
    }

    public static string Describe(HeapBaseline baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{baseline.Scenario}: wks {baseline.WorkstationMinimumMb} MiB, srv {baseline.ServerMinimumMb} MiB, " +
            $"shared {baseline.SharedMinimumMb} MiB{(baseline.Provisional ? " (PROVISIONAL)" : string.Empty)} - {baseline.Source}");
    }
}

/// <summary>One heap size that calibration actually tried, and what happened.</summary>
/// <remarks>
/// Every probe is recorded, including the ones that failed and the ones that were only tried because a
/// bracket had to be widened. A bisection reported as its answer alone is an assertion; reported with
/// its probes it is a measurement someone else can check, and the disagreements between neighbouring
/// probes are where a non-monotone scenario shows itself.
/// </remarks>
public sealed class CalibrationProbe
{
    public required string Scenario { get; init; }

    public required string Arm { get; init; }

    public required long LimitMb { get; init; }

    public required bool Viable { get; init; }

    /// <summary>How many consecutive invocations were required, and how many succeeded.</summary>
    public required int AttemptsRequired { get; init; }

    public required int AttemptsSucceeded { get; init; }

    public required string Detail { get; init; }

    public required double WallSeconds { get; init; }
}

/// <summary>The complete record of one calibration run: its rule, its probes and its conclusions.</summary>
public sealed class CalibrationTrace
{
    public required string RunId { get; init; }

    public required string Host { get; init; }

    public required string ViabilityRule { get; init; }

    public required double WarmupSeconds { get; init; }

    public required double SteadyStateSeconds { get; init; }

    public string? CoreClrSha256 { get; set; }

    public List<CalibrationProbe> Probes { get; init; } = [];

    public List<HeapBaseline> Baselines { get; init; } = [];
}

/// <summary>Reads and writes the committed calibration file.</summary>
public static class CalibrationFile
{
    public const int SchemaVersion = 1;

    public static void Write(string path, CalibrationTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", SchemaVersion);
        writer.WriteString("runId", trace.RunId);
        writer.WriteString("host", trace.Host);
        writer.WriteString("viabilityRule", trace.ViabilityRule);
        writer.WriteNumber("warmupSeconds", trace.WarmupSeconds);
        writer.WriteNumber("steadyStateSeconds", trace.SteadyStateSeconds);
        if (trace.CoreClrSha256 is string sha)
        {
            writer.WriteString("coreclrSha256", sha);
        }
        else
        {
            writer.WriteNull("coreclrSha256");
        }

        writer.WriteStartArray("baselines");
        foreach (HeapBaseline baseline in trace.Baselines)
        {
            writer.WriteStartObject();
            writer.WriteString("scenario", baseline.Scenario);
            writer.WriteNumber("workstationMinimumMb", baseline.WorkstationMinimumMb);
            writer.WriteNumber("serverMinimumMb", baseline.ServerMinimumMb);
            writer.WriteNumber("sharedMinimumMb", baseline.SharedMinimumMb);
            writer.WriteBoolean("provisional", baseline.Provisional);
            writer.WriteString("source", baseline.Source);
            if (baseline.Note is string note)
            {
                writer.WriteString("note", note);
            }
            else
            {
                writer.WriteNull("note");
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("probes");
        foreach (CalibrationProbe probe in trace.Probes)
        {
            writer.WriteStartObject();
            writer.WriteString("scenario", probe.Scenario);
            writer.WriteString("arm", probe.Arm);
            writer.WriteNumber("limitMb", probe.LimitMb);
            writer.WriteBoolean("viable", probe.Viable);
            writer.WriteNumber("attemptsRequired", probe.AttemptsRequired);
            writer.WriteNumber("attemptsSucceeded", probe.AttemptsSucceeded);
            writer.WriteNumber("wallSeconds", Math.Round(probe.WallSeconds, 3, MidpointRounding.AwayFromZero));
            writer.WriteString("detail", probe.Detail);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }

    /// <summary>
    /// Reads the baselines from a calibration file. Returns an empty list rather than throwing when the
    /// file is absent, so a first run with no calibration behaves as an explicit, visible fallback.
    /// </summary>
    public static List<HeapBaseline> ReadBaselines(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using FileStream stream = File.OpenRead(path);
        using JsonDocument document = JsonDocument.Parse(stream);
        var baselines = new List<HeapBaseline>();
        if (!document.RootElement.TryGetProperty("baselines", out JsonElement array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return baselines;
        }

        foreach (JsonElement element in array.EnumerateArray())
        {
            baselines.Add(new HeapBaseline
            {
                Scenario = element.GetProperty("scenario").GetString()!,
                WorkstationMinimumMb = element.GetProperty("workstationMinimumMb").GetInt64(),
                ServerMinimumMb = element.GetProperty("serverMinimumMb").GetInt64(),
                Provisional = element.GetProperty("provisional").GetBoolean(),
                Source = element.TryGetProperty("source", out JsonElement source) && source.ValueKind is JsonValueKind.String
                    ? source.GetString()!
                    : "unstated",
                Note = element.TryGetProperty("note", out JsonElement note) && note.ValueKind is JsonValueKind.String
                    ? note.GetString()
                    : null,
            });
        }

        return baselines;
    }
}
