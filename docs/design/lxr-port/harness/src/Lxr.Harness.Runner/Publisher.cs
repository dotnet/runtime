// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>
/// Merges run directories into one committed schema-v2 checkpoint, with the raw data needed to
/// re-derive every statistic it publishes.
/// </summary>
/// <remarks>
/// <para>This is a program rather than a hand-assembly step for one reason: a published file that was
/// edited by hand cannot be regenerated, so nobody can check whether it still matches the runs it
/// claims to summarise. Everything here is derived from the run directories, and the verification gate
/// recomputes the published means from the committed CSV and requires them to agree - which is only a
/// meaningful check because no human hand comes between the two.</para>
///
/// <para>P0.4's emitter published <c>?? 0</c> for absent metrics. A scenario that never paused would
/// have been published as a collector with perfect zero pauses, which is a fabricated result rather
/// than a formatting choice. Every optional field here stays null, and <see cref="ResultWriter"/>
/// writes JSON null; a consumer omits null from a series rather than plotting it at the origin.</para>
/// </remarks>
public static class Publisher
{
    /// <summary>
    /// The per-invocation CSV. Its columns are the exact vector the bootstrap resamples, so a reader
    /// can recompute the published mean and re-run the interval without the harness.
    /// </summary>
    public const string InvocationsHeader =
        "runId,scenario,collector,host,heapFactor,heapLimitMb,mode,invocation,valid,status,invalidReason," +
        "operationsPerSecond,latencyP50Ms,latencyP99Ms,latencyP999Ms,latencyP9999Ms,latencyMaxMs,serviceTimeP99Ms," +
        "arrivalRatePerSecond,achievedRatePerSecond,overloaded,lateFraction,dispatchLagP99Ms,pauseAverageMs,pauseP99Ms,pauseMaxMs," +
        "gen0Collections,gen1Collections,gen2Collections,inducedCollections,workingSetMb,committedMb," +
        "warmupSeconds,steadyStateSeconds,seed,backgroundLoadPercent";

    public sealed class PublishOutcome
    {
        public required ResultDocument Document { get; init; }

        public required int InvocationRows { get; init; }

        public List<string> Warnings { get; init; } = [];
    }

    public static PublishOutcome Publish(
        IReadOnlyList<string> runDirectories,
        string outputDirectory,
        string checkpointId,
        string date,
        string stepId,
        string notes)
    {
        ArgumentNullException.ThrowIfNull(runDirectories);

        var document = new ResultDocument
        {
            Id = checkpointId,
            Date = date,
            StepId = stepId,
            Notes = notes,
        };

        var warnings = new List<string>();
        var csv = new StringBuilder();
        csv.AppendLine(InvocationsHeader);
        int rows = 0;

        foreach (string runDirectory in runDirectories)
        {
            string resultsPath = Path.Combine(runDirectory, "results.json");
            if (!File.Exists(resultsPath))
            {
                // Naming a directory is a claim that it holds results. A missing one is reported, never
                // skipped quietly, because a checkpoint short of one whole run reads exactly like a
                // checkpoint that was only ever meant to have the others.
                warnings.Add($"'{resultsPath}' does not exist; that run contributed nothing");
                continue;
            }

            foreach (RunResult result in ReadResults(resultsPath))
            {
                document.Results.Add(result);
            }

            rows += AppendInvocations(csv, runDirectory);
        }

        Directory.CreateDirectory(outputDirectory);
        string rawDirectory = Path.Combine(outputDirectory, "raw");
        Directory.CreateDirectory(rawDirectory);

        ResultWriter.Write(Path.Combine(outputDirectory, checkpointId + ".json"), document);
        File.WriteAllText(Path.Combine(rawDirectory, checkpointId + "-invocations.csv"), csv.ToString());

        return new PublishOutcome { Document = document, InvocationRows = rows, Warnings = warnings };
    }

    /// <summary>
    /// Re-reads a written results.json back into records, rather than keeping the in-memory document.
    /// </summary>
    /// <remarks>
    /// Publishing from the bytes on disk means the published checkpoint is a function of the committed
    /// run output and nothing else. Carrying the objects across would let a value that never reached the
    /// run directory appear in the checkpoint, and the gate that compares the two would then be
    /// comparing the emitter with itself.
    /// </remarks>
    public static List<RunResult> ReadResults(string resultsPath)
    {
        var results = new List<RunResult>();
        using FileStream stream = File.OpenRead(resultsPath);
        using JsonDocument document = JsonDocument.Parse(stream);

        if (!document.RootElement.TryGetProperty("checkpoints", out JsonElement checkpoints) ||
            checkpoints.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (JsonElement checkpoint in checkpoints.EnumerateArray())
        {
            if (!checkpoint.TryGetProperty("results", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (JsonElement element in array.EnumerateArray())
            {
                results.Add(FromJson(element));
            }
        }

        return results;
    }

    private static RunResult FromJson(JsonElement e)
    {
        var result = new RunResult
        {
            Scenario = String(e, "scenario") ?? "(unnamed)",
            Collector = String(e, "collector") ?? "(unnamed)",
            Host = String(e, "host"),
            OperationsPerSecond = Number(e, "operationsPerSecond") ?? 0,
            PauseAverageMs = Number(e, "pauseAverageMs"),
            PauseP99Ms = Number(e, "pauseP99Ms"),
            PauseMaxMs = Number(e, "pauseMaxMs"),
            WorkingSetMb = Number(e, "workingSetMb") ?? 0,
            CommittedMb = Number(e, "committedMb"),
            Notes = String(e, "notes") ?? string.Empty,
            LatencyP50Ms = Number(e, "latencyP50Ms"),
            LatencyP99Ms = Number(e, "latencyP99Ms"),
            LatencyP999Ms = Number(e, "latencyP999Ms"),
            LatencyP9999Ms = Number(e, "latencyP9999Ms"),
            LatencyMaxMs = Number(e, "latencyMaxMs"),
            LatencyMethod = String(e, "latencyMethod"),
            ServiceTimeP99Ms = Number(e, "serviceTimeP99Ms"),
            ArrivalRatePerSecond = Number(e, "arrivalRatePerSecond"),
            AchievedRatePerSecond = Number(e, "achievedRatePerSecond"),
            Overloaded = Bool(e, "overloaded"),
            LateFraction = Number(e, "lateFraction"),
            DispatchLagP99Ms = Number(e, "dispatchLagP99Ms"),
            UnexplainedDispatchLagMs = Number(e, "unexplainedDispatchLagMs"),
            HeapFactor = Number(e, "heapFactor"),
            HeapLimitMb = Number(e, "heapLimitMb") is double limit ? (long)limit : null,
            CollectorConfirmed = Bool(e, "collectorConfirmed"),
            Valid = Bool(e, "valid"),
            InvalidReason = String(e, "invalidReason"),
            Status = String(e, "status") ?? RunStatus.Ok,
            SkipReason = String(e, "skipReason"),
            RuntimeBuildId = String(e, "runtimeBuildId"),
            RuntimeDescription = String(e, "runtimeDescription"),
            CoreClrSha256 = String(e, "coreclrSha256"),
            RatioStatistic = String(e, "ratioStatistic"),
            RatioVsBaseline = Number(e, "ratioVsBaseline"),
            RatioCiLow = Number(e, "ratioCiLow"),
            RatioCiHigh = Number(e, "ratioCiHigh"),
            CiMethod = String(e, "ciMethod"),
            Invocations = (int)(Number(e, "invocations") ?? 0),
            Seed = (long)(Number(e, "seed") ?? 0),
            WarmupSeconds = Number(e, "warmupSeconds") ?? 0,
            SteadyStateSeconds = Number(e, "steadyStateSeconds") ?? 0,
            RawSamplesPath = String(e, "rawSamplesPath"),
            InducedCollections = (int)(Number(e, "inducedCollections") ?? 0),
            Gen0Collections = (int)(Number(e, "gen0Collections") ?? 0),
            Gen1Collections = (int)(Number(e, "gen1Collections") ?? 0),
            Gen2Collections = (int)(Number(e, "gen2Collections") ?? 0),
            PauseSource = String(e, "pauseSource"),
            BackgroundLoadPercent = Number(e, "backgroundLoadPercent"),
            Noisy = Bool(e, "noisy"),
        };

        CopyMap(e, "requestedConfig", result.RequestedConfig);
        CopyMap(e, "observedConfig", result.ObservedConfig);
        CopyMap(e, "configEvidence", result.ConfigEvidence);

        if (e.TryGetProperty("unverifiedKnobs", out JsonElement knobs) && knobs.ValueKind is JsonValueKind.Array)
        {
            foreach (JsonElement knob in knobs.EnumerateArray())
            {
                if (knob.GetString() is string text)
                {
                    result.UnverifiedKnobs.Add(text);
                }
            }
        }

        if (e.TryGetProperty("machine", out JsonElement machine) && machine.ValueKind is JsonValueKind.Object)
        {
            result.Machine = new MachineInfo
            {
                ProcessorName = String(machine, "processorName"),
                LogicalCores = (int)(Number(machine, "logicalCores") ?? 0),
                PhysicalCores = Number(machine, "physicalCores") is double cores ? (int)cores : null,
                TotalMemoryBytes = (long)(Number(machine, "totalMemoryBytes") ?? 0),
                PowerPlan = String(machine, "powerPlan"),
                SystemModel = String(machine, "systemModel"),
                Virtualized = NullableBool(machine, "virtualized"),
                OsDescription = String(machine, "osDescription") ?? string.Empty,
                ProcessCount = (int)(Number(machine, "processCount") ?? 0),
                TimerResolutionNs = Number(machine, "timerResolutionNs") ?? 0,
            };
        }

        return result;
    }

    /// <summary>
    /// Writes one CSV row per worker report found in the run directory - the per-invocation vector
    /// behind every published mean and every bootstrap interval.
    /// </summary>
    private static int AppendInvocations(StringBuilder csv, string runDirectory)
    {
        string reportsDirectory = Path.Combine(runDirectory, "reports");
        if (!Directory.Exists(reportsDirectory))
        {
            return 0;
        }

        string runId = Path.GetFileName(runDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string[] files = Directory.GetFiles(reportsDirectory, "*.json");
        Array.Sort(files, StringComparer.Ordinal);

        int rows = 0;
        foreach (string file in files)
        {
            JsonElement report;
            try
            {
                using FileStream stream = File.OpenRead(file);
                using JsonDocument document = JsonDocument.Parse(stream);
                report = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                continue;
            }

            JsonElement metrics = report.TryGetProperty("metrics", out JsonElement m) ? m : default;
            JsonElement gc = report.TryGetProperty("gc", out JsonElement g) ? g : default;
            JsonElement process = report.TryGetProperty("process", out JsonElement p) ? p : default;

            // The cell id and invocation index are encoded in the report file name, which is the only
            // place the runner records which cell a report belongs to.
            string name = Path.GetFileNameWithoutExtension(file);
            (string cellId, string invocation) = SplitReportName(name);
            (string scenario, string collector, string host, string heapFactor) = SplitCellId(cellId);

            csv.Append(runId).Append(',')
                .Append(scenario).Append(',')
                .Append(collector).Append(',')
                .Append(host).Append(',')
                .Append(heapFactor).Append(',')
                .Append(Cell(report, "heapLimitMb")).Append(',')
                .Append(StringField(report, "mode")).Append(',')
                .Append(invocation).Append(',')
                .Append(Bool(report, "valid") ? "true" : "false").Append(',')
                .Append(Bool(report, "valid") ? RunStatus.Ok : RunStatus.Failed).Append(',')
                .Append(StringField(report, "invalidReason")).Append(',')
                .Append(Cell(metrics, "operationsPerSecond")).Append(',')
                .Append(Cell(metrics, "latencyP50Ms")).Append(',')
                .Append(Cell(metrics, "latencyP99Ms")).Append(',')
                .Append(Cell(metrics, "latencyP999Ms")).Append(',')
                .Append(Cell(metrics, "latencyP9999Ms")).Append(',')
                .Append(Cell(metrics, "latencyMaxMs")).Append(',')
                .Append(Cell(metrics, "serviceTimeP99Ms")).Append(',')
                .Append(Cell(metrics, "arrivalRatePerSecond")).Append(',')
                .Append(Cell(metrics, "achievedRatePerSecond")).Append(',')
                .Append(Bool(metrics, "overloaded") ? "true" : "false").Append(',')
                .Append(Cell(metrics, "lateFraction")).Append(',')
                .Append(Cell(metrics, "dispatchLagP99Ms")).Append(',')
                .Append(Cell(gc, "pauseAverageMs")).Append(',')
                .Append(Cell(gc, "pauseP99Ms")).Append(',')
                .Append(Cell(gc, "pauseMaxMs")).Append(',')
                .Append(Cell(gc, "gen0Collections")).Append(',')
                .Append(Cell(gc, "gen1Collections")).Append(',')
                .Append(Cell(gc, "gen2Collections")).Append(',')
                .Append(Cell(gc, "inducedCollections")).Append(',')
                .Append(Cell(process, "workingSetMb")).Append(',')
                .Append(Cell(process, "committedMb")).Append(',')
                .Append(Cell(report, "warmupSeconds")).Append(',')
                .Append(Cell(report, "steadyStateSeconds")).Append(',')
                .Append(Cell(report, "seed")).Append(',')
                .Append(Cell(report, "backgroundLoadPercent"))
                .Append('\n');
            rows++;
        }

        return rows;
    }

    /// <summary>Splits <c>cell.id.3</c> into its cell id and invocation index.</summary>
    public static (string CellId, string Invocation) SplitReportName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        int lastDot = name.LastIndexOf('.');
        return lastDot < 0 ? (name, string.Empty) : (name[..lastDot], name[(lastDot + 1)..]);
    }

    /// <summary>
    /// Splits a cell id into its parts. The id is built as
    /// <c>[tag.]scenario.arm.host.h&lt;factor&gt;</c>, and scenario ids themselves contain no dots, so
    /// the four trailing components are read from the right.
    /// </summary>
    /// <summary>
    /// Splits a cell id into scenario, collector arm, host and heap factor.
    /// </summary>
    /// <remarks>
    /// The id is <c>[tag.]scenario.arm.host.h&lt;factor&gt;</c> (<see cref="MatrixPlanner"/>), and the
    /// obvious index-from-the-right parse is wrong: the heap factor is formatted <c>"0.0#"</c>, so
    /// <c>h1.3</c> is <em>two</em> dot-separated components and <c>parts[^4]</c> lands on the arm rather
    /// than the scenario. The heap token is instead located directly - the last component that looks like
    /// <c>hdefault</c> or <c>h&lt;digit&gt;</c> - and the three components before it are the scenario, arm
    /// and host. Searching from the right means a scenario whose name began with <c>h</c> followed by a
    /// digit could not be mistaken for it.
    /// </remarks>
    public static (string Scenario, string Collector, string Host, string HeapFactor) SplitCellId(string cellId)
    {
        ArgumentNullException.ThrowIfNull(cellId);
        string[] parts = cellId.Split('.');

        for (int i = parts.Length - 1; i >= 3; i--)
        {
            string part = parts[i];
            if (part.Length > 1 && part[0] == 'h' && (part == "hdefault" || char.IsAsciiDigit(part[1])))
            {
                string heap = part == "hdefault"
                    ? string.Empty
                    : string.Join('.', parts[i..])[1..];
                return (parts[i - 3], parts[i - 2], parts[i - 1], heap);
            }
        }

        return parts.Length >= 4
            ? (parts[^4], parts[^3], parts[^2], string.Empty)
            : (cellId, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// A CSV cell for a numeric field: the value, or empty when it is absent.
    /// </summary>
    /// <remarks>
    /// Empty, never <c>0</c>. This is the same rule the JSON emitter follows and for the same reason: a
    /// zero pause time published for a scenario that never paused is a fabricated measurement, and it is
    /// worse in a CSV than in JSON because a spreadsheet will average it without complaint.
    /// </remarks>
    private static string Cell(JsonElement element, string name)
    {
        if (element.ValueKind is not JsonValueKind.Object ||
            !element.TryGetProperty(name, out JsonElement value) ||
            value.ValueKind is not JsonValueKind.Number)
        {
            return string.Empty;
        }

        double number = value.GetDouble();
        return double.IsNaN(number) || double.IsInfinity(number)
            ? string.Empty
            : number.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string StringField(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object &&
        element.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.String
            ? value.GetString()!.Replace(',', ';')
            : string.Empty;

    private static double? Number(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object &&
        element.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static string? String(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object &&
        element.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool Bool(JsonElement element, string name) =>
        element.ValueKind is JsonValueKind.Object &&
        element.TryGetProperty(name, out JsonElement value) &&
        value.ValueKind is JsonValueKind.True;

    /// <summary>
    /// A tri-state boolean: true, false, or null when the property is absent or JSON null.
    /// </summary>
    /// <remarks>
    /// <see cref="Bool"/> collapses "absent" and "false" into false, which is right for a flag and
    /// wrong for a recorded observation. Round-tripping a machine block through <see cref="Bool"/>
    /// would turn every unknown into a definite negative on republication, so the null survives
    /// republication only because this overload exists.
    /// </remarks>
    private static bool? NullableBool(JsonElement element, string name)
    {
        if (element.ValueKind is not JsonValueKind.Object ||
            !element.TryGetProperty(name, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static void CopyMap(JsonElement element, string name, Dictionary<string, string> into)
    {
        if (element.TryGetProperty(name, out JsonElement map) && map.ValueKind is JsonValueKind.Object)
        {
            foreach (JsonProperty property in map.EnumerateObject())
            {
                into[property.Name] = property.Value.ValueKind is JsonValueKind.String
                    ? property.Value.GetString()!
                    : property.Value.ToString();
            }
        }
    }
}
