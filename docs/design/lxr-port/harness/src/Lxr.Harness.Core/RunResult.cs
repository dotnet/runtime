// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Lxr.Harness.Core;

public static class RunStatus
{
    public const string Ok = "ok";
    public const string Failed = "failed";
    public const string Timeout = "timeout";
    public const string Crashed = "crashed";
    public const string Skipped = "skipped";
}

public static class InvalidReason
{
    public const string CollectorIdentityMismatch = "collector-identity-mismatch";
    public const string ConfigPinNotHonoured = "config-pin-not-honoured";
    public const string MarkerMissing = "marker-missing";
    public const string InsufficientOps = "insufficient-ops";
    public const string ChecksumMismatch = "checksum-mismatch";
    public const string UnexpectedInducedCollections = "unexpected-induced-collections";
    public const string SemanticViolation = "semantic-violation";
    public const string Timeout = "timeout";
    public const string Crashed = "crashed";
    public const string WorkerError = "worker-error";
    public const string ReferenceEnumerationProbeFailed =
        "reference-enumeration-probe-failed";
    public const string PauseScheduleProbeFailed = "pause-schedule-probe-failed";

    /// <summary>
    /// The open-loop dispatcher could not keep the arrival schedule, so the reported latency is its
    /// own backlog rather than the collector's behaviour.
    /// </summary>
    public const string ScheduleNotDelivered = "schedule-not-delivered";
}

/// <summary>
/// One measured cell: a scenario, on a collector arm, at a heap setting, on a host.
///
/// <para>This is the harness's own record. It is a proposed <c>schemaVersion: 2</c> superset of the
/// roadmap board's v1 record, which is pause-centric and has no field for application-observed
/// latency at all - the very signal P0.2 established as the acceptance criterion. Every v1 field is
/// retained with its meaning unchanged so a v2 record is readable as v1 plus additions.</para>
/// </summary>
public sealed class RunResult
{
    public const int SchemaVersion = 2;

    // ---- v1 fields, meanings unchanged ----
    public required string Scenario { get; init; }

    public required string Collector { get; init; }

    public double OperationsPerSecond { get; set; }

    /// <remarks>
    /// Null rather than zero when the run observed no collection. v1's rule is to use null for an
    /// unavailable metric, and a zero here would read as a collector that paused for no time at all.
    /// </remarks>
    public double? PauseAverageMs { get; set; }

    public double? PauseP99Ms { get; set; }

    public double? PauseMaxMs { get; set; }

    public double WorkingSetMb { get; set; }

    /// <remarks>Null when no collection completed, so <c>GCMemoryInfo</c> was never populated.</remarks>
    public double? CommittedMb { get; set; }

    public string Notes { get; set; } = string.Empty;

    // ---- application-observed latency: the acceptance signal ----
    public double? LatencyP50Ms { get; set; }

    public double? LatencyP99Ms { get; set; }

    public double? LatencyP999Ms { get; set; }

    /// <summary>The paper's supported G1-relative latency target is stated at 99.99% (P0.2), so this
    /// percentile is not optional.</summary>
    public double? LatencyP9999Ms { get; set; }

    public double? LatencyMaxMs { get; set; }

    /// <summary>
    /// How the latency was measured. <c>open-loop-intended-start</c> means
    /// <c>end - intendedStart</c> against a schedule fixed before the run. A record without this
    /// field must be treated as not coordinated-omission free.
    /// </summary>
    public string? LatencyMethod { get; set; }

    /// <summary>Service time (<c>end - serviceStart</c>) from the same run, retained purely so the
    /// coordinated-omission component is visible rather than merely absent.</summary>
    public double? ServiceTimeP99Ms { get; set; }

    public double? ArrivalRatePerSecond { get; set; }

    public double? AchievedRatePerSecond { get; set; }

    /// <summary>An overloaded open-loop run measures the capacity limit, not latency, and must not be
    /// silently compared.</summary>
    public bool Overloaded { get; set; }

    /// <summary>
    /// Share of arrival slots dispatched late. Null outside open-loop runs. Published because a
    /// latency percentile is only a collector measurement if the arrival process was actually
    /// delivered, and a reader cannot tell from the percentile alone.
    /// </summary>
    public double? LateFraction { get; set; }

    /// <summary>
    /// The p99 gap between when an operation was scheduled to start and when the dispatcher released
    /// it. This is the harness's own scheduling error; it bounds how much of the reported latency can
    /// be attributed to the collector at all.
    /// </summary>
    public double? DispatchLagP99Ms { get; set; }

    /// <summary>
    /// The part of <see cref="DispatchLagP99Ms"/> that the collector does not account for: the p99
    /// dispatch lag less the longest observed pause, floored at zero.
    /// </summary>
    /// <remarks>
    /// The dispatcher runs inside the process it measures, so a suspension delays it along with the
    /// workload and the schedule slips by the length of the pause. That slip is a property of the
    /// collector; what is left after subtracting it is the harness failing to keep up. The two
    /// populations do not overlap - see <c>Aggregator.MaximumUnexplainedDispatchLagMs</c>. Both terms
    /// are published per invocation in the raw CSV, so this figure can be recomputed without the
    /// harness.
    /// </remarks>
    public double? UnexplainedDispatchLagMs { get; set; }

    // ---- heap axis: the paper's throughput results invert with heap generosity ----
    public double? HeapFactor { get; set; }

    public long? HeapLimitMb { get; set; }

    // ---- configuration: observed versus requested ----
    public Dictionary<string, string> RequestedConfig { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> ObservedConfig { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> ConfigEvidence { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Knobs we can only claim to have requested. An honest short list beats a clean sheet.</summary>
    public List<string> UnverifiedKnobs { get; init; } = [];

    // ---- validity ----
    public bool CollectorConfirmed { get; set; }

    public bool Valid { get; set; }

    public string? InvalidReason { get; set; }

    public string Status { get; set; } = RunStatus.Ok;

    public string? SkipReason { get; set; }

    // ---- provenance ----
    public string? RuntimeBuildId { get; set; }

    public string? RuntimeDescription { get; set; }

    public string? CoreClrSha256 { get; set; }

    public string? Host { get; set; }

    // ---- comparison ----
    public string? RatioStatistic { get; set; }

    public double? RatioVsBaseline { get; set; }

    public double? RatioCiLow { get; set; }

    public double? RatioCiHigh { get; set; }

    public string? CiMethod { get; set; }

    // ---- reproducibility ----
    public int Invocations { get; set; }

    public long Seed { get; set; }

    public double WarmupSeconds { get; set; }

    public double SteadyStateSeconds { get; set; }

    public string? RawSamplesPath { get; set; }

    /// <summary>Asserted to be zero unless the scenario declares otherwise; see <see cref="GcTelemetry"/>.</summary>
    public int InducedCollections { get; set; }

    public int Gen0Collections { get; set; }

    public int Gen1Collections { get; set; }

    public int Gen2Collections { get; set; }

    public string? PauseSource { get; set; }

    public MachineInfo? Machine { get; set; }

    public double? BackgroundLoadPercent { get; set; }

    /// <summary>Set when background activity on this shared host makes the run suspect data.</summary>
    public bool Noisy { get; set; }
}

/// <summary>
/// A checkpoint: one set of results gathered under one configuration, at one point in the project.
/// </summary>
/// <remarks>
/// The <c>checkpoints</c> wrapper is load-bearing rather than decorative. The roadmap canvas loads
/// every <c>*.json</c> in its results directory, merges each file's <c>checkpoints</c> array and
/// orders the result by date. A document that carried its rows at the top level would parse as JSON,
/// contribute nothing, and report no error - so the shape is asserted by
/// <see cref="ResultConformance"/> rather than left to reviewers to notice.
/// </remarks>
public sealed class ResultDocument
{
    public required string Id { get; init; }

    public required string Date { get; init; }

    public required string StepId { get; init; }

    public string Notes { get; set; } = string.Empty;

    public List<RunResult> Results { get; init; } = [];
}

public static class ResultWriter
{
    public static void Write(string path, ResultDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", RunResult.SchemaVersion);
        writer.WriteStartArray("checkpoints");
        writer.WriteStartObject();
        writer.WriteString("id", document.Id);
        writer.WriteString("date", document.Date);
        writer.WriteString("stepId", document.StepId);
        writer.WriteString("notes", document.Notes);
        writer.WriteStartArray("results");

        foreach (RunResult result in document.Results)
        {
            WriteResult(writer, result);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }

    private static void WriteResult(Utf8JsonWriter writer, RunResult r)
    {
        writer.WriteStartObject();

        writer.WriteString("scenario", r.Scenario);
        writer.WriteString("collector", r.Collector);
        writer.WriteNumber("operationsPerSecond", Round(r.OperationsPerSecond));
        WriteOptionalNumber(writer, "pauseAverageMs", r.PauseAverageMs);
        WriteOptionalNumber(writer, "pauseP99Ms", r.PauseP99Ms);
        WriteOptionalNumber(writer, "pauseMaxMs", r.PauseMaxMs);
        writer.WriteNumber("workingSetMb", Round(r.WorkingSetMb));
        WriteOptionalNumber(writer, "committedMb", r.CommittedMb);
        writer.WriteString("notes", r.Notes);

        WriteOptionalNumber(writer, "latencyP50Ms", r.LatencyP50Ms);
        WriteOptionalNumber(writer, "latencyP99Ms", r.LatencyP99Ms);
        WriteOptionalNumber(writer, "latencyP999Ms", r.LatencyP999Ms);
        WriteOptionalNumber(writer, "latencyP9999Ms", r.LatencyP9999Ms);
        WriteOptionalNumber(writer, "latencyMaxMs", r.LatencyMaxMs);
        WriteOptionalString(writer, "latencyMethod", r.LatencyMethod);
        WriteOptionalNumber(writer, "serviceTimeP99Ms", r.ServiceTimeP99Ms);
        WriteOptionalNumber(writer, "arrivalRatePerSecond", r.ArrivalRatePerSecond);
        WriteOptionalNumber(writer, "achievedRatePerSecond", r.AchievedRatePerSecond);
        writer.WriteBoolean("overloaded", r.Overloaded);
        WriteOptionalNumber(writer, "lateFraction", r.LateFraction);
        WriteOptionalNumber(writer, "dispatchLagP99Ms", r.DispatchLagP99Ms);
        WriteOptionalNumber(writer, "unexplainedDispatchLagMs", r.UnexplainedDispatchLagMs);

        WriteOptionalNumber(writer, "heapFactor", r.HeapFactor);
        if (r.HeapLimitMb is long heapLimit)
        {
            writer.WriteNumber("heapLimitMb", heapLimit);
        }
        else
        {
            writer.WriteNull("heapLimitMb");
        }

        WriteStringMap(writer, "requestedConfig", r.RequestedConfig);
        WriteStringMap(writer, "observedConfig", r.ObservedConfig);
        WriteStringMap(writer, "configEvidence", r.ConfigEvidence);

        writer.WriteStartArray("unverifiedKnobs");
        foreach (string knob in r.UnverifiedKnobs)
        {
            writer.WriteStringValue(knob);
        }

        writer.WriteEndArray();

        writer.WriteBoolean("collectorConfirmed", r.CollectorConfirmed);
        writer.WriteBoolean("valid", r.Valid);
        WriteOptionalString(writer, "invalidReason", r.InvalidReason);
        writer.WriteString("status", r.Status);
        WriteOptionalString(writer, "skipReason", r.SkipReason);

        WriteOptionalString(writer, "runtimeBuildId", r.RuntimeBuildId);
        WriteOptionalString(writer, "runtimeDescription", r.RuntimeDescription);
        WriteOptionalString(writer, "coreclrSha256", r.CoreClrSha256);
        WriteOptionalString(writer, "host", r.Host);

        WriteOptionalString(writer, "ratioStatistic", r.RatioStatistic);
        WriteOptionalNumber(writer, "ratioVsBaseline", r.RatioVsBaseline);
        WriteOptionalNumber(writer, "ratioCiLow", r.RatioCiLow);
        WriteOptionalNumber(writer, "ratioCiHigh", r.RatioCiHigh);
        WriteOptionalString(writer, "ciMethod", r.CiMethod);

        writer.WriteNumber("invocations", r.Invocations);
        writer.WriteNumber("seed", r.Seed);
        writer.WriteNumber("warmupSeconds", Round(r.WarmupSeconds));
        writer.WriteNumber("steadyStateSeconds", Round(r.SteadyStateSeconds));
        WriteOptionalString(writer, "rawSamplesPath", r.RawSamplesPath);

        writer.WriteNumber("inducedCollections", r.InducedCollections);
        writer.WriteNumber("gen0Collections", r.Gen0Collections);
        writer.WriteNumber("gen1Collections", r.Gen1Collections);
        writer.WriteNumber("gen2Collections", r.Gen2Collections);
        WriteOptionalString(writer, "pauseSource", r.PauseSource);

        WriteOptionalNumber(writer, "backgroundLoadPercent", r.BackgroundLoadPercent);
        writer.WriteBoolean("noisy", r.Noisy);

        if (r.Machine is MachineInfo machine)
        {
            writer.WriteStartObject("machine");
            WriteOptionalString(writer, "processorName", machine.ProcessorName);
            writer.WriteNumber("logicalCores", machine.LogicalCores);
            if (machine.PhysicalCores is int physical)
            {
                writer.WriteNumber("physicalCores", physical);
            }
            else
            {
                writer.WriteNull("physicalCores");
            }

            writer.WriteNumber("totalMemoryBytes", machine.TotalMemoryBytes);
            WriteOptionalString(writer, "powerPlan", machine.PowerPlan);
            WriteOptionalString(writer, "systemModel", machine.SystemModel);
            if (machine.Virtualized is bool virtualized)
            {
                writer.WriteBoolean("virtualized", virtualized);
            }
            else
            {
                // Absent, not false. 'false' would claim bare metal on a host nobody characterised.
                writer.WriteNull("virtualized");
            }
            writer.WriteString("osDescription", machine.OsDescription);
            writer.WriteNumber("processCount", machine.ProcessCount);
            writer.WriteNumber("timerResolutionNs", Round(machine.TimerResolutionNs));
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteNull("machine");
        }

        writer.WriteEndObject();
    }

    private static void WriteStringMap(Utf8JsonWriter writer, string name, IReadOnlyDictionary<string, string> map)
    {
        writer.WriteStartObject(name);
        foreach (KeyValuePair<string, string> entry in map)
        {
            writer.WriteString(entry.Key, entry.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteOptionalString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteOptionalNumber(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is double number && !double.IsNaN(number) && !double.IsInfinity(number))
        {
            writer.WriteNumber(name, Round(number));
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    private static double Round(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) ? 0.0 : Math.Round(value, 6, MidpointRounding.AwayFromZero);

    public static string ToInvariant(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
