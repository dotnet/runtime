// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>All invocations of one cell, reduced to a result record.</summary>
public sealed class CellAggregate
{
    public required MatrixCell Cell { get; init; }

    public required List<InvocationOutcome> Outcomes { get; init; }

    public List<InvocationOutcome> ValidOutcomes { get; } = [];

    public double[] PrimarySamples { get; set; } = [];
}

/// <summary>
/// Turns invocations into result records and comparisons.
///
/// <para>The rule that shapes this type: <strong>a run whose collector cannot be confirmed is
/// invalid, and an invalid run cannot contribute to a ratio.</strong> Not "is flagged in the output" -
/// cannot contribute. A comparison containing an unconfirmed run is not published at all, because a
/// ratio silently computed over a run that used the wrong collector is worse than no ratio.</para>
/// </summary>
public static class Aggregator
{
    public const string CiMethodDescription = "percentile bootstrap over invocations, B=10000, seed=20040, 95%";

    /// <summary>
    /// The fewest valid invocations per arm from which a ratio may be published.
    /// </summary>
    /// <remarks>
    /// The bootstrap resamples invocations with replacement. At n=1 every one of the B resamples draws
    /// the same single value, so the resample distribution is a point mass: the interval collapses to
    /// zero width and reports that it excludes 1.0 for any ratio at all. That is not a weak result, it
    /// is a fabricated one - it claims significance for the difference between two single runs. At n=2
    /// the interval is real but still far too wide to be useful; the floor is set at 3 so that a
    /// published interval always has some spread behind it, and control 7's resolution ladder is what
    /// determines the invocation count an actual measurement needs.
    /// </remarks>
    public const int MinimumInvocationsForBootstrap = 3;

    /// <summary>
    /// The share of arrival slots a latency run may dispatch late before its percentiles stop being a
    /// statement about the collector. Chosen generously - the observed dispatcher-bound cell was at
    /// 0.68, and a healthy run is near zero - so that ordinary jitter cannot trip it but a run whose
    /// offered load was never delivered cannot be published.
    /// </summary>
    public const double MaximumLateFraction = 0.05;

    /// <summary>
    /// The p99 dispatcher scheduling error that a collector pause does not account for, in
    /// milliseconds, beyond which a latency run is not a measurement of the collector.
    /// </summary>
    /// <remarks>
    /// The bound is on <em>unexplained</em> lag, and the distinction is the whole point. An in-process
    /// dispatcher is suspended by the collector it is measuring, so when the collector pauses, the
    /// schedule slips by exactly that pause - and that slip is the measurement, not an artefact. Across
    /// P0.5's 54 open-loop rate probes the worst dispatch lag equalled the worst pause to within
    /// 0.01 ms in run after run: 135.34 against 135.33 ms, 32.22 against 32.22 ms, 19.57 against
    /// 19.58 ms, on two independent clocks. Rejecting those runs, or lowering their arrival rate until
    /// the lag went away, would have discarded precisely the configurations where the collector pauses
    /// most - and allocation-churn's rate would have fallen to 1,993 op/s, 1.6% of its capacity, which
    /// is choosing the offered load from the result it produces.
    /// <para>
    /// What remains after subtracting the pause is dispatcher overload, and it does not overlap: the
    /// six probes that asked for more than the dispatcher could deliver ran 1,205 to 6,015 ms
    /// unexplained, while the largest unexplained value among the other 48 was 0.14 ms. One
    /// millisecond sits seven times above the latter and four orders of magnitude below the former.
    /// </para>
    /// </remarks>
    public const double MaximumUnexplainedDispatchLagMs = 1.0;

    public static CellAggregate Aggregate(MatrixCell cell, List<InvocationOutcome> outcomes)
    {
        var aggregate = new CellAggregate { Cell = cell, Outcomes = outcomes };
        var samples = new List<double>();

        foreach (InvocationOutcome outcome in outcomes)
        {
            if (!outcome.Valid || outcome.Report is not JsonElement report)
            {
                continue;
            }

            aggregate.ValidOutcomes.Add(outcome);
            if (TryReadPrimary(report, cell.Primary, out double value))
            {
                samples.Add(value);
            }
        }

        aggregate.PrimarySamples = [.. samples];
        return aggregate;
    }

    private static bool TryReadPrimary(JsonElement report, PrimaryMetric primary, out double value)
    {
        value = 0;
        if (!report.TryGetProperty("metrics", out JsonElement metrics))
        {
            return false;
        }

        string field = primary is PrimaryMetric.Latency ? "latencyP99Ms" : "operationsPerSecond";
        if (!metrics.TryGetProperty(field, out JsonElement element) || element.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        value = element.GetDouble();
        return true;
    }

    /// <summary>
    /// Applies the collector-versus-baseline ratio to every non-baseline cell. Shared by the matrix
    /// and by <c>reaggregate</c> so a re-derived document carries ratios computed by the same rule as
    /// a freshly measured one; a second copy of this rule is a second place for it to drift.
    /// </summary>
    public static void ApplyRatios(
        ResultDocument document,
        IReadOnlyList<MatrixCell> cells,
        IReadOnlyDictionary<string, CellAggregate> aggregates,
        string baselineArm)
    {
        var byResult = new Dictionary<string, RunResult>(StringComparer.Ordinal);
        foreach (RunResult result in document.Results)
        {
            byResult[$"{result.Scenario}.{result.Collector}.{result.Host}.{(result.HeapFactor is double f ? "h" + f.ToString("0.0#", CultureInfo.InvariantCulture) : "hdefault")}"] = result;
        }

        foreach (MatrixCell cell in cells)
        {
            if (string.Equals(cell.Arm.Id, baselineArm, StringComparison.Ordinal))
            {
                continue;
            }

            string baselineId = cell.Id.Replace($".{cell.Arm.Id}.", $".{baselineArm}.", StringComparison.Ordinal);
            if (!aggregates.TryGetValue(baselineId, out CellAggregate? baseline) ||
                !byResult.TryGetValue(cell.Id, out RunResult? result))
            {
                continue;
            }

            (RatioEstimate? estimate, string? refusal) = Ratio(baseline, aggregates[cell.Id]);

            // Name the statistic the bootstrap actually resamples, not just the field it came from.
            // "latencyP99Ms" alone would let a reader assume the ratio is of the two published p99
            // values as-is; it is the ratio of their means across invocations, which is the same thing
            // only because the published field is now that mean too.
            result.RatioStatistic = cell.Primary is PrimaryMetric.Latency
                ? "mean over invocations of latencyP99Ms"
                : "mean over invocations of operationsPerSecond";

            if (estimate is RatioEstimate ratio)
            {
                result.RatioVsBaseline = ratio.Ratio;
                result.RatioCiLow = ratio.Low;
                result.RatioCiHigh = ratio.High;
                result.CiMethod = Aggregator.CiMethodDescription;
            }
            else
            {
                // Refusing to publish a ratio is the enforcement of "a run whose collector cannot be
                // confirmed is invalid". Flagging would not be enough; the number must not exist.
                result.CiMethod = $"ratio-refused: {refusal}";
                result.Notes += $" | ratio refused vs {baselineArm}: {refusal}";
            }
        }
    }

    public static RunResult ToResult(CellAggregate aggregate, string runId, double noiseThresholdPercent) =>
        ToResult(aggregate, runId, noiseThresholdPercent, null, null, null, null);

    public static RunResult ToResult(
        CellAggregate aggregate,
        string runId,
        double noiseThresholdPercent,
        string? machinePowerPlan,
        int? machinePhysicalCores,
        string? machineModel,
        long? machineTotalMemoryBytes)
    {
        MatrixCell cell = aggregate.Cell;
        var result = new RunResult
        {
            Scenario = cell.Scenario,
            Collector = cell.Arm.Id,
            Host = cell.Host.Id,
            HeapFactor = cell.HeapFactor,
            HeapLimitMb = cell.HeapLimitMb,
            Invocations = aggregate.ValidOutcomes.Count,
            Status = RunStatus.Ok,
        };

        foreach (KeyValuePair<string, string> property in cell.Arm.RuntimeProperties)
        {
            result.RequestedConfig[property.Key] = property.Value;
        }

        // Status is the worst thing that happened, not the best. A cell with any timeout or crash says
        // so, and the counts are kept in the notes so a partial failure is never rounded away.
        int timeouts = 0;
        int crashes = 0;
        int failures = 0;
        int markerless = 0;
        foreach (InvocationOutcome outcome in aggregate.Outcomes)
        {
            switch (outcome.Status)
            {
                case RunStatus.Timeout:
                    timeouts++;
                    break;
                case RunStatus.Crashed:
                    crashes++;
                    break;
                case RunStatus.Failed:
                    failures++;
                    break;
                default:
                    break;
            }

            if (!outcome.MarkerSeen)
            {
                markerless++;
            }
        }

        if (aggregate.ValidOutcomes.Count == 0)
        {
            result.Valid = false;
            result.CollectorConfirmed = false;
            result.Status = crashes > 0 ? RunStatus.Crashed
                : timeouts > 0 ? RunStatus.Timeout
                : RunStatus.Failed;
            result.InvalidReason = FirstInvalidReason(aggregate.Outcomes)
                ?? (crashes > 0 ? Core.InvalidReason.Crashed
                    : timeouts > 0 ? Core.InvalidReason.Timeout
                    : Core.InvalidReason.WorkerError);
            result.Notes = Describe(aggregate.Outcomes.Count, timeouts, crashes, failures, markerless);
            return result;
        }

        if (timeouts > 0 || crashes > 0 || failures > 0)
        {
            result.Status = RunStatus.Failed;
        }

        JsonElement report = aggregate.ValidOutcomes[^1].Report!.Value;
        result.CollectorConfirmed = true;
        result.Valid = true;

        PopulateFromReport(result, report, aggregate);
        result.Machine = ReadMachine(report, machinePowerPlan, machinePhysicalCores, machineModel, machineTotalMemoryBytes);

        // The arm's properties are what the arm asks for; they are not the whole request. The heap hard
        // limit is per cell, so populating requestedConfig from the arm alone published a run pinned to
        // 128 MiB whose requestedConfig said only Server and Concurrent - the one knob this step varies
        // was the one knob missing. Merging the worker's own copy fixes that and is stronger besides:
        // it records what the worker was actually told rather than what the runner meant to tell it.
        MergeRequestedConfig(result, report);

        double[] samples = aggregate.PrimarySamples;
        if (samples.Length > 0)
        {
            // Every published statistic is the mean across the cell's valid invocations of that
            // invocation's own figure, and which family gets aggregated is decided by what the
            // invocations reported rather than by the scenario catalogue's declared primary metric.
            //
            // Keying it on `cell.Primary` - which is what this did - is F16 again, and it reached
            // further here. Nine of the ten scenarios declare Throughput and run open-loop, so their
            // latency percentiles skipped the mean entirely and were published as PopulateFromReport
            // left them: one invocation's values, out of five measured. In P0.5's first published
            // checkpoint `low-allocation-compute.wks` at h1.3 published a 0.3498 ms latency p99 whose
            // five invocations mean 0.7668 ms. Nothing was wrong with either number; the record simply
            // did not say which one it was, and `invocations: 5` beside it said the wrong thing.
            //
            // The mean specifically, because Stats.BootstrapRatio resamples invocations and takes
            // their mean: a reader dividing two published p99 values must land on the published ratio.
            result.LatencyP50Ms = MeanAcross(aggregate, "latencyP50Ms") ?? result.LatencyP50Ms;
            result.LatencyP99Ms = MeanAcross(aggregate, "latencyP99Ms") ?? result.LatencyP99Ms;
            result.LatencyP999Ms = MeanAcross(aggregate, "latencyP999Ms") ?? result.LatencyP999Ms;
            result.LatencyP9999Ms = MeanAcross(aggregate, "latencyP9999Ms") ?? result.LatencyP9999Ms;
            result.LatencyMaxMs = MeanAcross(aggregate, "latencyMaxMs") ?? result.LatencyMaxMs;
            result.ServiceTimeP99Ms = MeanAcross(aggregate, "serviceTimeP99Ms") ?? result.ServiceTimeP99Ms;

            // Read by name rather than from PrimarySamples, which holds whichever metric the cell
            // declared primary: for a latency-primary cell those samples are latencies, and meaning
            // them into OperationsPerSecond would publish a latency as a throughput.
            result.OperationsPerSecond = MeanAcross(aggregate, "operationsPerSecond") ?? result.OperationsPerSecond;

            // The pause statistics and collection counts are still whatever PopulateFromReport copied
            // out of a single invocation. They are not meaned here because doing so would change what
            // `pauseP99Ms` denotes without the schema saying so - a p99 of one invocation's pauses and
            // the mean of five invocations' p99s are different estimators. The per-invocation values
            // are all in the published CSV; see finding F20.
        }

        // A run that delivered an arrival schedule is judged on whether it delivered it, whatever the
        // scenario's declared primary metric is. Keying this on `cell.Primary` - which is a property of
        // the scenario catalogue entry, not of the run - is what P0.5's first latency matrix did, and
        // in that matrix nine of the ten scenarios ran open-loop while declaring Throughput as their
        // primary. Nineteen cells were published valid with a dispatch lag over the bound, including
        // `lifecycle-semantics.srv` at h6.0, whose published 90.25 ms latency p99 sat against a 90.79 ms
        // dispatcher lag: the number was the harness's own backlog, quotable as a collector baseline.
        //
        // MeanAcross returns null when no invocation reported the field, so a closed-loop run - which
        // has no schedule to keep and emits no dispatch lag - is untouched by this block.
        result.LateFraction = MeanAcross(aggregate, "lateFraction");

        // A latency run is a statement about a delivered arrival process. When the dispatcher cannot
        // keep the schedule, the reported percentiles are its own backlog: in P0.5's first latency
        // matrix a cell reported a 22.15 ms p99 of which 22.15 ms was queue delay, against a 0.0015 ms
        // service time and zero collections in the measured region. `Overloaded` does not catch it -
        // late work still completes, so achieved rate matches requested. The run is marked invalid
        // rather than flagged, because a number that measures the harness must not be available to be
        // quoted as a collector baseline.
        //
        // The test is on lag the collector does not explain. The dispatcher runs in the process it is
        // measuring, so a suspension stops it too, and the schedule slips by the length of the pause;
        // that slip is a real consequence of the collector and belongs in the result. Subtracting the
        // longest observed pause from the p99 lag is deliberately generous to the collector, which is
        // the right direction for a bound whose purpose is catching apparatus failure.
        //
        // The test is on the lag's magnitude, not on how many slots were late. A Poisson schedule at a
        // high rate puts many arrivals closer together than one dispatch costs, so a healthy run is
        // late on a third of its slots by a few microseconds each.
        result.DispatchLagP99Ms = MeanAcross(aggregate, "dispatchLagP99Ms");
        double? explainedBy = MeanAcrossSection(aggregate, "gc", "pauseMaxMs");
        if (result.DispatchLagP99Ms is double lag)
        {
            result.UnexplainedDispatchLagMs = Math.Max(0, lag - (explainedBy ?? 0));
            if (result.UnexplainedDispatchLagMs > MaximumUnexplainedDispatchLagMs)
            {
                result.Valid = false;
                result.InvalidReason = Core.InvalidReason.ScheduleNotDelivered;
                result.Status = RunStatus.Failed;
            }
        }

        double background = 0;
        int backgroundCount = 0;
        foreach (InvocationOutcome outcome in aggregate.ValidOutcomes)
        {
            if (outcome.BackgroundLoadPercent is double load)
            {
                background += load;
                backgroundCount++;
            }
        }

        if (backgroundCount > 0)
        {
            result.BackgroundLoadPercent = background / backgroundCount;
            result.Noisy = result.BackgroundLoadPercent > noiseThresholdPercent;
        }

        result.Notes = Describe(aggregate.Outcomes.Count, timeouts, crashes, failures, markerless) +
            $" runId={runId}";
        return result;
    }

    private static string? FirstInvalidReason(List<InvocationOutcome> outcomes)
    {
        foreach (InvocationOutcome outcome in outcomes)
        {
            if (outcome.Status is RunStatus.Timeout)
            {
                return Core.InvalidReason.Timeout;
            }

            if (outcome.Status is RunStatus.Crashed)
            {
                return Core.InvalidReason.Crashed;
            }

            if (outcome.Report is JsonElement report &&
                report.TryGetProperty("invalidReason", out JsonElement reason) &&
                reason.ValueKind is JsonValueKind.String)
            {
                return reason.GetString();
            }

            if (!outcome.MarkerSeen)
            {
                return Core.InvalidReason.MarkerMissing;
            }
        }

        return null;
    }

    private static void PopulateFromReport(RunResult result, JsonElement report, CellAggregate aggregate)
    {
        if (report.TryGetProperty("metrics", out JsonElement metrics))
        {
            result.OperationsPerSecond = ReadDouble(metrics, "operationsPerSecond") ?? 0;
            result.LatencyP50Ms = ReadDouble(metrics, "latencyP50Ms");
            result.LatencyP99Ms = ReadDouble(metrics, "latencyP99Ms");
            result.LatencyP999Ms = ReadDouble(metrics, "latencyP999Ms");
            result.LatencyP9999Ms = ReadDouble(metrics, "latencyP9999Ms");
            result.LatencyMaxMs = ReadDouble(metrics, "latencyMaxMs");
            result.ServiceTimeP99Ms = ReadDouble(metrics, "serviceTimeP99Ms");
            result.ArrivalRatePerSecond = ReadDouble(metrics, "arrivalRatePerSecond");
            result.AchievedRatePerSecond = ReadDouble(metrics, "achievedRatePerSecond");
            result.LatencyMethod = ReadString(metrics, "latencyMethod");
            result.Overloaded = metrics.TryGetProperty("overloaded", out JsonElement overloaded) &&
                overloaded.ValueKind is JsonValueKind.True;
            result.LateFraction = ReadDouble(metrics, "lateFraction");
        }

        if (report.TryGetProperty("gc", out JsonElement gc))
        {
            result.PauseAverageMs = ReadDouble(gc, "pauseAverageMs");
            result.PauseP99Ms = ReadDouble(gc, "pauseP99Ms");
            result.PauseMaxMs = ReadDouble(gc, "pauseMaxMs");
            result.InducedCollections = (int)(ReadDouble(gc, "inducedCollections") ?? 0);
            result.Gen0Collections = (int)(ReadDouble(gc, "gen0Collections") ?? 0);
            result.Gen1Collections = (int)(ReadDouble(gc, "gen1Collections") ?? 0);
            result.Gen2Collections = (int)(ReadDouble(gc, "gen2Collections") ?? 0);
            result.PauseSource = ReadString(gc, "pauseSource");
        }

        if (report.TryGetProperty("process", out JsonElement process))
        {
            result.WorkingSetMb = ReadDouble(process, "workingSetMb") ?? 0;
            result.CommittedMb = ReadDouble(process, "committedMb");
        }

        if (report.TryGetProperty("runtime", out JsonElement runtime))
        {
            result.RuntimeDescription = ReadString(runtime, "frameworkDescription");
            result.CoreClrSha256 = ReadString(runtime, "coreClrSha256");
            result.RuntimeBuildId = ReadString(runtime, "commitSha") is string sha && sha.Length > 0
                ? sha
                : ReadString(runtime, "coreClrSha256");
        }

        if (report.TryGetProperty("observedGcConfig", out JsonElement observed) &&
            observed.ValueKind is JsonValueKind.Object)
        {
            foreach (JsonProperty property in observed.EnumerateObject())
            {
                result.ObservedConfig[property.Name] = property.Value.ToString();
            }
        }

        if (report.TryGetProperty("knobs", out JsonElement knobs) && knobs.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement knob in knobs.EnumerateArray())
            {
                string? name = ReadString(knob, "name");
                string? evidence = ReadString(knob, "evidence");
                if (name is not null && evidence is not null)
                {
                    result.ConfigEvidence[name] = evidence;
                }
            }
        }

        if (report.TryGetProperty("unverifiedKnobs", out JsonElement unverified) &&
            unverified.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement knob in unverified.EnumerateArray())
            {
                if (knob.GetString() is string text)
                {
                    result.UnverifiedKnobs.Add(text);
                }
            }
        }

        result.Seed = (long)(ReadDouble(report, "seed") ?? 0);
        result.WarmupSeconds = ReadDouble(report, "warmupSeconds") ?? 0;
        result.SteadyStateSeconds = ReadDouble(report, "steadyStateSeconds") ?? 0;
        result.RawSamplesPath = ReadString(report, "samplesPath");
    }

    /// <summary>
    /// Computes the ratio of a candidate arm to a baseline arm, or refuses.
    ///
    /// <para>Refusal is the point. If either side contains a run whose collector was not confirmed,
    /// or has too few valid invocations to bootstrap, no ratio is produced and the reason is recorded.
    /// The brief's requirement that "a run whose collector cannot be confirmed is invalid" is only
    /// meaningful if invalidity actually prevents a number from being published.</para>
    /// </summary>
    public static (RatioEstimate? Estimate, string? Refusal) Ratio(CellAggregate baseline, CellAggregate candidate)
    {
        if (baseline.Outcomes.Count != baseline.ValidOutcomes.Count)
        {
            return (null, $"baseline cell {baseline.Cell.Id} has {baseline.Outcomes.Count - baseline.ValidOutcomes.Count} invalid invocation(s)");
        }

        if (candidate.Outcomes.Count != candidate.ValidOutcomes.Count)
        {
            return (null, $"candidate cell {candidate.Cell.Id} has {candidate.Outcomes.Count - candidate.ValidOutcomes.Count} invalid invocation(s)");
        }

        if (baseline.PrimarySamples.Length < MinimumInvocationsForBootstrap ||
            candidate.PrimarySamples.Length < MinimumInvocationsForBootstrap)
        {
            return (null,
                $"fewer than {MinimumInvocationsForBootstrap} valid invocations per arm " +
                $"(baseline={baseline.PrimarySamples.Length}, candidate={candidate.PrimarySamples.Length}); " +
                "a bootstrap over one sample resamples the same value every time and reports a " +
                "zero-width interval, which would assert significance it has not measured");
        }

        return (Stats.BootstrapRatio(baseline.PrimarySamples, candidate.PrimarySamples), null);
    }

    /// <summary>
    /// The mean across valid invocations of a per-invocation metric, or null when no invocation
    /// reported it. Null rather than zero: a metric nobody measured is absent, not zero.
    /// </summary>
    private static double? MeanAcross(CellAggregate aggregate, string metric) =>
        MeanAcrossSection(aggregate, "metrics", metric);

    /// <summary>
    /// Means a field from a named top-level section of the worker's report across the cell's valid
    /// invocations, returning <see langword="null"/> when no invocation reported it.
    /// </summary>
    private static double? MeanAcrossSection(CellAggregate aggregate, string section, string metric)
    {
        double total = 0;
        int count = 0;

        foreach (InvocationOutcome outcome in aggregate.ValidOutcomes)
        {
            if (outcome.Report is JsonElement report &&
                report.TryGetProperty(section, out JsonElement fields) &&
                ReadDouble(fields, metric) is double value)
            {
                total += value;
                count++;
            }
        }

        return count > 0 ? total / count : null;
    }

    /// <summary>
    /// Copies the worker's own record of the configuration it was given into the published result.
    /// </summary>
    public static void MergeRequestedConfig(RunResult result, JsonElement report)
    {
        if (report.ValueKind is JsonValueKind.Object &&
            report.TryGetProperty("requestedConfig", out JsonElement requested) &&
            requested.ValueKind is JsonValueKind.Object)
        {
            foreach (JsonProperty property in requested.EnumerateObject())
            {
                if (property.Value.ValueKind is JsonValueKind.String)
                {
                    result.RequestedConfig[property.Name] = property.Value.GetString()!;
                }
            }
        }
    }

    /// <summary>Test seam for <see cref="ReadMachine"/>.</summary>
    public static MachineInfo? MachineFromReport(
        JsonElement report,
        string? powerPlan,
        int? physicalCores,
        string? model,
        long? totalMemoryBytes) =>
        ReadMachine(report, powerPlan, physicalCores, model, totalMemoryBytes);

    /// <summary>
    /// The machine block, merging what the worker could observe with the facts it could not.
    /// </summary>
    /// <remarks>
    /// P0.4 captured a <see cref="MachineInfo"/> in the worker and never copied it into the result, so
    /// every published record carried <c>machine: null</c> - the schema field existed and was empty on
    /// the only files it was ever asked to describe. Power plan, physical core count and system model
    /// are not readable in-process without a WMI dependency the harness deliberately does not take, so
    /// they arrive from the operator's machine survey and are recorded as given. A null there is honest;
    /// a plausible default would not be.
    /// </remarks>
    private static MachineInfo? ReadMachine(
        JsonElement report,
        string? powerPlan,
        int? physicalCores,
        string? model,
        long? totalMemoryBytes)
    {
        if (!report.TryGetProperty("machine", out JsonElement machine) || machine.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        return new MachineInfo
        {
            ProcessorName = ReadString(machine, "processorName"),
            LogicalCores = (int)(ReadDouble(machine, "logicalCores") ?? 0),
            PhysicalCores = physicalCores,
            // The runner's reading when it has one, because the worker's is the heap limit under a pin.
            TotalMemoryBytes = totalMemoryBytes ?? (long)(ReadDouble(machine, "totalMemoryBytes") ?? 0),
            PowerPlan = powerPlan,
            SystemModel = model,
            Virtualized = model is null
                ? null
                : model.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                  model.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                  model.Contains("KVM", StringComparison.OrdinalIgnoreCase),
            OsDescription = ReadString(machine, "osDescription") ?? string.Empty,
            ProcessCount = (int)(ReadDouble(machine, "processCount") ?? 0),
            TimerResolutionNs = ReadDouble(machine, "timerResolutionNs") ?? 0,
        };
    }

    private static double? ReadDouble(JsonElement element, string name) =>        element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Describe(int total, int timeouts, int crashes, int failures, int markerless)
    {
        var parts = new List<string> { $"invocations={total.ToString(CultureInfo.InvariantCulture)}" };
        if (timeouts > 0)
        {
            parts.Add($"timeouts={timeouts}");
        }

        if (crashes > 0)
        {
            parts.Add($"crashes={crashes}");
        }

        if (failures > 0)
        {
            parts.Add($"failed={failures}");
        }

        if (markerless > 0)
        {
            parts.Add($"markerMissing={markerless}");
        }

        return string.Join(", ", parts);
    }
}
