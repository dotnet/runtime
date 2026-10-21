// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Lxr.Harness.Core;

namespace Lxr.Harness.Runner;

/// <summary>
/// Runs the matrix: expands cells, interleaves invocations across arms, launches workers, and reduces
/// the outcomes into result records and ratios.
/// </summary>
public sealed class MatrixRunner
{
    private readonly RunnerOptions _options;
    private readonly string _repoRoot;
    private readonly string _buildRoot;
    private readonly string _runDirectory;
    private readonly WorkerLauncher _launcher;

    public MatrixRunner(RunnerOptions options, string repoRoot, string buildRoot, string runDirectory)
    {
        _options = options;
        _repoRoot = repoRoot;
        _buildRoot = buildRoot;
        _runDirectory = runDirectory;
        _launcher = new WorkerLauncher(runDirectory, options.DumpBudgetMb);
    }

    public string RunDirectory => _runDirectory;

    public string WorkerAssemblyFor(string scenario)
    {
        ScenarioCatalog.Entry entry = ScenarioCatalog.Get(scenario);
        return (entry.RequiredCapabilities & HostCapabilities.AspNetCoreSharedFramework) != 0
            ? Path.Combine(_buildRoot, "Lxr.Harness.Worker.AspNet", "release", "Lxr.Harness.Worker.AspNet.dll")
            : Path.Combine(_buildRoot, "Lxr.Harness.Worker", "release", "Lxr.Harness.Worker.dll");
    }

    public ResultDocument Run(IReadOnlyList<HostDescriptor> hosts, IReadOnlyList<string> scenarios, IReadOnlyList<CollectorArm> arms)
    {
        List<MatrixCell> cells = MatrixPlanner.Expand(
            scenarios,
            arms,
            hosts,
            _options.HeapFactors.Count > 0 ? _options.HeapFactors : null,
            _options.Invocations,
            scenario => _options.TimeoutFor(ScenarioCatalog.Get(scenario).DefaultTimeoutSeconds),
            scenario => ScenarioCatalog.Get(scenario).Primary,

            // P0.4 planned the heap axis and then never connected it: this argument was omitted, so
            // HeapLimitMb stayed null for every cell, the heap-limit branch in LaunchCell never fired,
            // and running at three heap factors would have produced three identically configured
            // unpinned runs published under three different heap labels. Passing it is what makes the
            // factor a configuration rather than a caption.
            HeapBaselines.LimitMb);

        var document = new ResultDocument
        {
            Id = _options.RunId,
            Date = _options.CheckpointDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            StepId = _options.StepId,
            Notes = _options.CheckpointNotes ?? string.Empty,
        };

        var skipped = new List<MatrixCell>();
        var runnable = new List<MatrixCell>();
        foreach (MatrixCell cell in cells)
        {
            ScenarioCatalog.Entry entry = ScenarioCatalog.Get(cell.Scenario);
            if ((entry.RequiredCapabilities & ~cell.Host.Capabilities) != 0)
            {
                skipped.Add(cell);
            }
            else if (!File.Exists(WorkerAssemblyFor(cell.Scenario)))
            {
                skipped.Add(cell);
            }
            else
            {
                runnable.Add(cell);
            }
        }

        // A capability the host lacks produces a declared skip carrying its reason. It is never simply
        // absent from the output: an absent row and a row that could not run are indistinguishable to
        // a reader, and the second one is a fact about the port.
        foreach (MatrixCell cell in skipped)
        {
            document.Results.Add(SkipResult(cell));
        }

        var outcomes = new Dictionary<string, List<InvocationOutcome>>(StringComparer.Ordinal);
        foreach (MatrixCell cell in runnable)
        {
            outcomes[cell.Id] = [];
        }

        List<(MatrixCell Cell, int Invocation)> order = MatrixPlanner.InterleavedOrder(runnable, _options.Seed);
        int index = 0;
        foreach ((MatrixCell cell, int invocation) in order)
        {
            index++;
            Console.WriteLine($"[{index}/{order.Count}] {cell.Id} #{invocation}");
            InvocationOutcome outcome = LaunchCell(cell, invocation);
            outcomes[cell.Id].Add(outcome);
            Console.WriteLine(
                $"    status={outcome.Status} valid={outcome.Valid} exit={outcome.ExitCode} " +
                $"wall={WorkerLauncher.FormatSeconds(outcome.WallSeconds)}s marker={(outcome.MarkerSeen ? "yes" : "NO")}");
            if (!outcome.MarkerSeen && outcome.StdErr.Length > 0)
            {
                Console.WriteLine("    stderr: " + outcome.StdErr.Trim().Replace("\n", "\n    ", StringComparison.Ordinal));
            }
        }

        var aggregates = new Dictionary<string, CellAggregate>(StringComparer.Ordinal);
        foreach (MatrixCell cell in runnable)
        {
            aggregates[cell.Id] = Aggregator.Aggregate(cell, outcomes[cell.Id]);
        }

        foreach (MatrixCell cell in runnable)
        {
            document.Results.Add(Aggregator.ToResult(
                aggregates[cell.Id],
                _options.RunId,
                _options.NoiseThresholdPercent,
                _options.MachinePowerPlan,
                _options.MachinePhysicalCores,
                _options.MachineModel,
                _options.RunnerTotalMemoryBytes));
        }

        Aggregator.ApplyRatios(document, runnable, aggregates, _options.BaselineArm ?? CollectorArms.WorkstationId);
        return document;
    }

    private RunResult SkipResult(MatrixCell cell)
    {
        ScenarioCatalog.Entry entry = ScenarioCatalog.Get(cell.Scenario);
        bool capabilityMissing = (entry.RequiredCapabilities & ~cell.Host.Capabilities) != 0;
        var result = new RunResult
        {
            Scenario = cell.Scenario,
            Collector = cell.Arm.Id,
            Host = cell.Host.Id,
            HeapFactor = cell.HeapFactor,
            HeapLimitMb = cell.HeapLimitMb,
            Status = RunStatus.Skipped,
            Valid = false,
            CollectorConfirmed = false,
            SkipReason = capabilityMissing
                ? "host-lacks-aspnetcore-shared-framework"
                : "worker-assembly-not-built",
            Notes = capabilityMissing
                ? $"Host '{cell.Host.Id}' has no ASP.NET Core shared framework; run scripts/compose-testhost-aspnet.ps1 to give the locally built runtime one."
                : $"Worker assembly '{WorkerAssemblyFor(cell.Scenario)}' is not present.",
        };

        foreach (KeyValuePair<string, string> property in cell.Arm.RuntimeProperties)
        {
            result.RequestedConfig[property.Key] = property.Value;
        }

        return result;
    }

    /// <summary>
    /// The offered arrival rate for a scenario's open-loop pass.
    /// </summary>
    /// <remarks>
    /// <para>A single global rate is wrong in both directions at once. Offered far below capacity, the
    /// run measures an idle system and every percentile collapses to the service time; offered above
    /// capacity, the queue grows without bound and the number produced is the capacity limit wearing a
    /// latency's units. This host's scenarios differ by four orders of magnitude in sustainable rate, so
    /// no one value avoids both.</para>
    ///
    /// <para>The same rate is used for every arm at a given scenario, deliberately. Deriving each arm's
    /// rate from its own measured capacity would offer the faster collector more work and then compare
    /// the resulting latencies as though the load had been equal.</para>
    /// </remarks>
    public double RateFor(string scenario) =>
        _options.ScenarioRates.TryGetValue(scenario, out double rate) ? rate : _options.ArrivalRatePerSecond;

    public InvocationOutcome LaunchCell(        MatrixCell cell,
        int invocation,
        IReadOnlyDictionary<string, string>? extraEnvironment = null,
        IReadOnlyList<string>? extraArguments = null)
    {
        var arguments = new List<string>
        {
            "--scenario", cell.Scenario,
            "--seed", (_options.Seed + invocation).ToString(CultureInfo.InvariantCulture),
            "--workers", _options.Workers.ToString(CultureInfo.InvariantCulture),
            "--warmup-seconds", _options.WarmupSeconds.ToString(CultureInfo.InvariantCulture),
            "--duration-seconds", _options.SteadyStateSeconds.ToString(CultureInfo.InvariantCulture),
            "--server-heap-count", _options.ServerHeapCount.ToString(CultureInfo.InvariantCulture),
        };

        string mode = cell.Primary is PrimaryMetric.Latency ? "latency" : _options.Mode;
        arguments.Add("--mode");
        arguments.Add(mode);

        if (mode is "latency")
        {
            arguments.Add("--rate");
            arguments.Add(RateFor(cell.Scenario).ToString(CultureInfo.InvariantCulture));
        }

        if (_options.KeepSamples)
        {
            arguments.Add("--samples");
            arguments.Add(Path.Combine(_runDirectory, "samples", $"{cell.Id}.{invocation}.bin.gz"));
        }

        var extraProperties = new Dictionary<string, string>(StringComparer.Ordinal);
        if (cell.HeapLimitMb is long limitMb)
        {
            arguments.Add("--heap-limit-mb");
            arguments.Add(limitMb.ToString(CultureInfo.InvariantCulture));
            extraProperties["System.GC.HeapHardLimit"] = (limitMb * 1024L * 1024L).ToString(CultureInfo.InvariantCulture);
        }

        if (cell.HeapFactor is double factor)
        {
            arguments.Add("--heap-factor");
            arguments.Add(factor.ToString(CultureInfo.InvariantCulture));
        }

        if (extraArguments is not null)
        {
            arguments.AddRange(extraArguments);
        }

        return _launcher.Launch(
            cell.Host,
            WorkerAssemblyFor(cell.Scenario),
            cell.Arm,
            arguments,
            extraProperties,
            extraEnvironment ?? new Dictionary<string, string>(StringComparer.Ordinal),
            cell.Id,
            invocation,
            cell.TimeoutSeconds);
    }
}
