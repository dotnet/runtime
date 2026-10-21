using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>
/// Recomputes a run directory's <c>results.json</c> from the per-invocation reports it already holds,
/// without re-measuring anything.
/// </summary>
/// <remarks>
/// Aggregation is a pure function of the reports, so an aggregation defect found after a measurement
/// campaign does not have to invalidate the campaign. P0.5 found one (F21: latency percentiles were a
/// single invocation's values for every cell whose declared primary was throughput), and the choice was
/// between re-running 2.5 hours of measurement and re-deriving the summary from the samples that had
/// already been retained. Re-deriving is not merely cheaper: re-measuring would have produced different
/// numbers for unrelated reasons, so the fix and the drift would arrive together and neither could be
/// attributed.
/// <para>
/// The conservatism is deliberate and is the reason this is safe. A cell is recomputed only when the
/// reports on disk exactly account for the invocation count the existing record published; anything else
/// is carried forward byte-for-byte and counted. So a cell whose invocations crashed - which leaves no
/// report - keeps its recorded failure rather than being silently reborn as a valid cell with no data.
/// </para>
/// </remarks>
public static class Reaggregator
{
    public sealed class Outcome
    {
        public int Recomputed { get; set; }

        public int Carried { get; set; }

        public int Total { get; set; }

        public List<string> CarriedIds { get; } = [];
    }

    public static Outcome Reaggregate(string runDirectory, string baselineArm, int serverHeapCount, double noiseThresholdPercent)
    {
        string resultsPath = Path.Combine(runDirectory, "results.json");
        string reportsDirectory = Path.Combine(runDirectory, "reports");
        // The run directory is named for the run, which is where the run id has to come from: it is
        // recorded in the notes of the document being replaced, not in a field of it.
        string runId = Path.GetFileName(runDirectory.TrimEnd(Path.DirectorySeparatorChar));
        List<RunResult> existing = Publisher.ReadResults(resultsPath);

        Dictionary<string, List<JsonElement>> reportsByCell = ReadReports(reportsDirectory);

        // The checkpoint header is not derivable from the reports either, so it is read from the
        // document being replaced rather than regenerated with today's date under the old run's name.
        (string id, string date, string stepId, string notes) = ReadHeader(resultsPath);
        var document = new ResultDocument { Id = id, Date = date, StepId = stepId, Notes = notes };
        var aggregates = new Dictionary<string, CellAggregate>(StringComparer.Ordinal);
        var cells = new List<MatrixCell>();
        var outcome = new Outcome { Total = existing.Count };
        var carriedIds = new HashSet<string>(StringComparer.Ordinal);
        var recomputedPairs = new List<(RunResult Recomputed, RunResult Original, string BaselineId)>();
        var restoreRatioFrom = new List<(RunResult Recomputed, RunResult Original)>();

        foreach (RunResult result in existing)
        {
            string cellId = CellId(result);
            reportsByCell.TryGetValue(cellId, out List<JsonElement>? reports);

            // The published invocation count is the number of *valid* invocations, and a report is
            // written only by a worker that completed. Requiring equality is what makes a carried cell
            // distinguishable from a recomputed one rather than a judgement call.
            if (reports is null || !result.Valid || reports.Count != result.Invocations)
            {
                document.Results.Add(result);
                outcome.Carried++;
                outcome.CarriedIds.Add(cellId);
                carriedIds.Add(cellId);
                continue;
            }

            MatrixCell cell = RebuildCell(result, reports.Count, serverHeapCount);

            // Background load is measured by the launcher, not written into the report, so it cannot be
            // re-derived. Replaying the published mean onto each reconstructed invocation preserves it
            // exactly and leaves the noisy/quiet decision to the one place that makes it, rather than
            // copying a verdict across.
            List<InvocationOutcome> outcomes = [.. reports.Select(report => new InvocationOutcome
            {
                Status = RunStatus.Ok,
                MarkerSeen = true,
                Report = report,
                BackgroundLoadPercent = result.BackgroundLoadPercent,
            })];

            CellAggregate aggregate = Aggregator.Aggregate(cell, outcomes);
            RunResult recomputed = Aggregator.ToResult(
                aggregate,
                runId,
                noiseThresholdPercent,
                result.Machine?.PowerPlan,
                result.Machine?.PhysicalCores,
                result.Machine?.SystemModel,
                result.Machine?.TotalMemoryBytes);

            // Fields that describe how the run was produced rather than what it measured cannot be
            // recovered from a report, so they are carried across rather than dropped or invented.
            recomputed.RawSamplesPath = result.RawSamplesPath;
            recomputed.RuntimeBuildId ??= result.RuntimeBuildId;
            recomputed.RuntimeDescription ??= result.RuntimeDescription;

            document.Results.Add(recomputed);
            aggregates[cell.Id] = aggregate;
            cells.Add(cell);
            recomputedPairs.Add((recomputed, result, cellId.Replace($".{result.Collector}.", $".{baselineArm}.", StringComparison.Ordinal)));
            outcome.Recomputed++;
        }

        foreach ((RunResult recomputed, RunResult original, string baselineId) in recomputedPairs)
        {
            if (carriedIds.Contains(baselineId))
            {
                restoreRatioFrom.Add((recomputed, original));
            }
        }

        Aggregator.ApplyRatios(document, cells, aggregates, baselineArm);

        // A cell whose baseline was carried has no baseline aggregate, so ApplyRatios cannot reach it -
        // and the refusal it recorded ("baseline has 5 invalid invocations") is not derivable from the
        // reports, because the invalid invocations are precisely the ones that wrote none. Restoring the
        // original ratio fields keeps the refusal rather than letting a re-derivation quietly delete it,
        // which would turn "no ratio, and here is why" into "no ratio".
        foreach ((RunResult recomputed, RunResult original) in restoreRatioFrom)
        {
            recomputed.RatioStatistic = original.RatioStatistic;
            recomputed.RatioVsBaseline = original.RatioVsBaseline;
            recomputed.RatioCiLow = original.RatioCiLow;
            recomputed.RatioCiHigh = original.RatioCiHigh;
            recomputed.CiMethod = original.CiMethod;
            recomputed.Notes = original.Notes;
        }

        ResultWriter.Write(resultsPath, document);
        return outcome;
    }

    private static (string Id, string Date, string StepId, string Notes) ReadHeader(string resultsPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(resultsPath));
        JsonElement checkpoint = document.RootElement.GetProperty("checkpoints")[0];
        return (
            checkpoint.GetProperty("id").GetString() ?? string.Empty,
            checkpoint.GetProperty("date").GetString() ?? string.Empty,
            checkpoint.GetProperty("stepId").GetString() ?? string.Empty,
            checkpoint.TryGetProperty("notes", out JsonElement notes) ? notes.GetString() ?? string.Empty : string.Empty);
    }

    private static Dictionary<string, List<JsonElement>> ReadReports(string reportsDirectory)
    {
        var byCell = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
        if (!Directory.Exists(reportsDirectory))
        {
            return byCell;
        }

        // Report names are "<cellId>.<invocation>.json"; the invocation index is the last segment
        // before the extension. Ordering by that index keeps the reports in the order they were
        // produced, which matters because ToResult reads scalar fields from the last valid report.
        foreach (string path in Directory.EnumerateFiles(reportsDirectory, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            int split = name.LastIndexOf('.');
            if (split <= 0 || !int.TryParse(name[(split + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                continue;
            }

            string cellId = name[..split];
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!byCell.TryGetValue(cellId, out List<JsonElement>? list))
            {
                list = [];
                byCell[cellId] = list;
            }

            list.Add(document.RootElement.Clone());
        }

        return byCell;
    }

    private static string CellId(RunResult result) =>
        $"{result.Scenario}.{result.Collector}.{result.Host}." +
        (result.HeapFactor is double factor
            ? "h" + factor.ToString("0.0#", CultureInfo.InvariantCulture)
            : "hdefault");

    private static MatrixCell RebuildCell(RunResult result, int invocations, int serverHeapCount) => new()
    {
        Scenario = result.Scenario,
        Arm = CollectorArms.Resolve(result.Collector, serverHeapCount),
        // Only the identifier is consumed downstream: ToResult reads Host.Id and nothing else, because
        // everything a host determines about a run is already baked into the report it produced.
        Host = new HostDescriptor
        {
            Id = result.Host ?? "unknown",
            Executable = string.Empty,
            UsesCoreRunProperties = false,
            Capabilities = HostCapabilities.None,
            Description = "reconstructed for re-aggregation",
        },
        Primary = ScenarioCatalog.Get(result.Scenario).Primary,
        HeapFactor = result.HeapFactor,
        HeapLimitMb = result.HeapLimitMb,
        Invocations = invocations,
        TimeoutSeconds = 0,
    };
}
