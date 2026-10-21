// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Lxr.Harness.Core;
using Lxr.Harness.Runner;

int Fail(string message)
{
    Console.Error.WriteLine("lxr-harness runner: " + message);
    return 2;
}

RunnerOptions options;
try
{
    options = RunnerOptions.Parse(args);
}
catch (ArgumentException ex)
{
    return Fail(ex.Message);
}

string repoRoot = options.RepoRoot ?? FindRepoRoot(AppContext.BaseDirectory)
    ?? Directory.GetCurrentDirectory();
string buildRoot = options.BuildRoot ?? Path.Combine(repoRoot, "artifacts", "lxr-harness", "build", "bin");
string outputRoot = options.OutputRoot ?? Path.Combine(repoRoot, "artifacts", "lxr-harness", "runs");

List<HostDescriptor> discovered = HostDescriptor.Discover(repoRoot);

if (options.Command is "hosts")
{
    foreach (HostDescriptor host in discovered)
    {
        Console.WriteLine($"{host.Id,-10} aspnet={((host.Capabilities & HostCapabilities.AspNetCoreSharedFramework) != 0 ? "yes" : "no ")} {host.Executable}");
        Console.WriteLine($"           {host.Description}");
    }

    if (discovered.Count == 0)
    {
        Console.WriteLine($"No hosts found under '{repoRoot}'.");
    }

    return 0;
}

if (options.Command is "conformance")
{
    return RunConformance(options.ConformanceInput);
}

// Publication reads run directories and writes files; it launches nothing, so it must not require a
// host to have been discovered. Placing it before the host check is what lets a published checkpoint be
// regenerated from committed run output on a machine that cannot run the matrix at all.
if (options.Command is "publish")
{
    if (options.PublishInputs.Count == 0)
    {
        return Fail("publish requires at least one --publish-input <run directory>.");
    }

    if (options.OutputRoot is null)
    {
        return Fail("publish requires --output <directory>, the committed location the checkpoint is written to.");
    }

    string checkpointId = options.CheckpointId ?? options.RunId;
    Publisher.PublishOutcome outcome = Publisher.Publish(
        options.PublishInputs,
        options.OutputRoot,
        checkpointId,
        options.CheckpointDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        options.StepId,
        options.CheckpointNotes ?? string.Empty);

    ConformanceReport publishReport = ResultConformance.Check(outcome.Document);
    Console.WriteLine(publishReport.Format());
    Console.WriteLine($"{outcome.Document.Results.Count} records, {outcome.InvocationRows} invocation rows -> {options.OutputRoot}");
    foreach (string warning in outcome.Warnings)
    {
        Console.Error.WriteLine("  warning: " + warning);
    }

    // A warning here means a named run directory contributed nothing. Naming it was a claim, so the
    // exit code carries the failure rather than leaving it to whoever reads the console.
    return publishReport.Ok && outcome.Warnings.Count == 0 ? 0 : 1;
}

// Re-aggregation, like publication, launches nothing: it recomputes a run's summary from the reports
// that run already wrote. It sits beside publish for the same reason - the machine that re-derives a
// summary need not be one that can run the matrix.
if (options.Command is "reaggregate")
{
    if (options.PublishInputs.Count == 0)
    {
        return Fail("reaggregate requires at least one --publish-input <run directory>.");
    }

    int totalRecomputed = 0;
    int totalCarried = 0;
    foreach (string reaggregateInput in options.PublishInputs)
    {
        Reaggregator.Outcome reaggregated = Reaggregator.Reaggregate(
            reaggregateInput,
            options.BaselineArm ?? CollectorArms.WorkstationId,
            options.ServerHeapCount,
            options.NoiseThresholdPercent);

        totalRecomputed += reaggregated.Recomputed;
        totalCarried += reaggregated.Carried;

        // Print the two counts and their sum against the total rather than a verdict: a cell carried
        // forward is not a cell that was re-derived, and the difference is the whole point.
        Console.WriteLine(
            $"{reaggregateInput}: {reaggregated.Recomputed} recomputed + {reaggregated.Carried} carried = " +
            $"{reaggregated.Recomputed + reaggregated.Carried} of {reaggregated.Total} records");
        foreach (string carried in reaggregated.CarriedIds)
        {
            Console.WriteLine($"    carried unchanged: {carried}");
        }
    }

    Console.WriteLine($"total: {totalRecomputed} recomputed, {totalCarried} carried");
    return 0;
}

if (discovered.Count == 0)
{
    return Fail($"no usable host found under '{repoRoot}'. Expected .dotnet/dotnet.exe, artifacts/bin/testhost/*/dotnet.exe or artifacts/bin/coreclr/*/corerun.exe.");
}

var hosts = new List<HostDescriptor>();
if (options.Hosts.Count == 0)
{
    hosts.Add(discovered[0]);
}
else
{
    foreach (string id in options.Hosts)
    {
        HostDescriptor? match = discovered.Find(h => string.Equals(h.Id, id, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return Fail($"host '{id}' was not discovered. Available: {string.Join(", ", discovered.ConvertAll(h => h.Id))}.");
        }

        hosts.Add(match);
    }
}

var arms = new List<CollectorArm>();
foreach (string id in options.Arms.Count > 0 ? options.Arms : [CollectorArms.WorkstationId, CollectorArms.ServerId])
{
    try
    {
        arms.Add(CollectorArms.Resolve(id, options.ServerHeapCount));
    }
    catch (ArgumentException ex)
    {
        return Fail(ex.Message);
    }
}

var scenarios = new List<string>();
foreach (string id in options.Scenarios.Count > 0 ? options.Scenarios : ScenarioCatalog.Ids)
{
    if (ScenarioCatalog.Find(id) is null)
    {
        return Fail($"unknown scenario '{id}'. Known: {string.Join(", ", ScenarioCatalog.Ids)}.");
    }

    scenarios.Add(id);
}

string runDirectory = Path.Combine(outputRoot, options.RunId);
Directory.CreateDirectory(runDirectory);

// The measured minimum heaps are loaded from the committed calibration file rather than compiled in,
// so the numbers a run uses and the numbers published as evidence are the same bytes, and a
// recalibration does not need a rebuild. When the file is absent every scenario falls back to a value
// flagged provisional, which is visible in the output rather than silently assumed.
string calibrationPath = options.CalibrationPath
    ?? Path.Combine(repoRoot, "docs", "design", "lxr-port", "P0.5-baselines", "calibration.json");
List<HeapBaseline> loadedBaselines = CalibrationFile.ReadBaselines(calibrationPath);
if (loadedBaselines.Count > 0)
{
    HeapBaselines.Load(loadedBaselines);
    Console.WriteLine($"Loaded {loadedBaselines.Count} calibrated heap baseline(s) from {calibrationPath}.");
}
else if (options.Command is not "calibrate")
{
    Console.WriteLine($"No calibration at '{calibrationPath}'; every heap baseline is the declared provisional fallback.");
}

var runner = new MatrixRunner(options, repoRoot, buildRoot, runDirectory);

// Per-scenario arrival rates derived from a throughput pass, at a fraction of capacity fixed before
// the run. The minimum across arms is used, not each arm's own capacity: offering the faster collector
// more work and then comparing the resulting latencies would compare two different experiments.
if (options.RateSourcePath is string rateSource)
{
    if (!File.Exists(rateSource))
    {
        return Fail($"--rate-from '{rateSource}' does not exist.");
    }

    var capacity = new Dictionary<string, double>(StringComparer.Ordinal);
    foreach (RunResult measured in Publisher.ReadResults(rateSource))
    {
        if (!measured.Valid || measured.OperationsPerSecond <= 0)
        {
            continue;
        }

        capacity[measured.Scenario] = capacity.TryGetValue(measured.Scenario, out double existing)
            ? Math.Min(existing, measured.OperationsPerSecond)
            : measured.OperationsPerSecond;
    }

    int cappedScenarios = 0;
    foreach (KeyValuePair<string, double> entry in capacity)
    {
        // An explicit --scenario-rate always wins: a value stated on the command line is a decision,
        // and silently overwriting it with a derived one would make the run's offered load depend on
        // whichever argument the runner happened to process last.
        if (!options.ScenarioRates.ContainsKey(entry.Key))
        {
            double derived = Math.Max(1.0, Math.Round(entry.Value * options.RateFractionOfCapacity));
            if (options.RateCapPerSecond is double cap && derived > cap)
            {
                derived = cap;
                cappedScenarios++;
            }

            options.ScenarioRates[entry.Key] = derived;
        }
    }

    Console.WriteLine($"Derived {capacity.Count} arrival rate(s) at {options.RateFractionOfCapacity:P0} of the " +
        $"lowest measured per-arm capacity in '{rateSource}'.");
    if (options.RateCapPerSecond is double rateCap)
    {
        Console.WriteLine($"Rate cap {rateCap:N0} op/s applied to {cappedScenarios} of {capacity.Count} scenario(s).");
    }

    // F18. A scenario with no valid throughput cell derives no rate, and before this check it fell
    // through to the global --rate default: `aspnet-request-load` was measured at a plausible-looking
    // 1000 op/s that nothing had chosen, under a heading saying rates were derived from capacity. An
    // offered load nobody selected is not a baseline, so the run refuses rather than inventing one.
    var withoutRate = new List<string>();
    foreach (string scenario in scenarios)
    {
        if (!options.ScenarioRates.ContainsKey(scenario))
        {
            withoutRate.Add(scenario);
        }
    }

    if (withoutRate.Count > 0)
    {
        return Fail($"no arrival rate for {withoutRate.Count} of {scenarios.Count} scenario(s): " +
            $"{string.Join(", ", withoutRate)}. Each had no valid throughput cell in '{rateSource}'. " +
            "Give each an explicit --scenario-rate or drop it from the run; the global --rate default " +
            "would otherwise stand in for a rate nothing measured.");
    }
}

if (options.Command is "calibrate")
{
    var calibrator = new HeapCalibrator(options, runner, hosts[0]);
    CalibrationTrace trace = calibrator.Run(scenarios, arms);
    string tracePath = Path.Combine(runDirectory, "calibration.json");
    CalibrationFile.Write(tracePath, trace);

    // Written twice, deliberately. The run directory copy is evidence tied to the run that produced
    // it; the --calibration copy is the file every later phase actually reads. Writing only the first
    // is how this went wrong once already: the calibrate phase reported '10/10 scenarios calibrated'
    // and exited 0 while the path the matrix reads stayed absent, so the measurement would have run
    // on the provisional fallback under headings claiming calibrated minima. That is F2's failure
    // shape - a label with nothing pinned behind it - reintroduced in the mechanism written to fix F2.
    if (!string.Equals(Path.GetFullPath(tracePath), Path.GetFullPath(calibrationPath), StringComparison.OrdinalIgnoreCase))
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(calibrationPath))!);
        CalibrationFile.Write(calibrationPath, trace);
    }

    int provisional = 0;
    foreach (HeapBaseline baseline in trace.Baselines)
    {
        if (baseline.Provisional)
        {
            provisional++;
        }
    }

    Console.WriteLine();
    Console.WriteLine($"{trace.Baselines.Count - provisional}/{trace.Baselines.Count} scenarios calibrated, " +
        $"{trace.Probes.Count} probes. Trace: {tracePath}");
    Console.WriteLine($"Calibration read by later phases: {calibrationPath}");

    // A scenario that did not converge is not a failure of the calibrator, but it is a gap in the
    // measurement, and the exit code says so rather than leaving it to be noticed in a JSON file.
    return provisional == 0 ? 0 : 1;
}

try
{
    if (options.Command is "controls")
    {
        var suite = new ControlSuite(options, runner, hosts[0]);
        List<ControlEvidence> evidence = suite.RunAll();

        string controlsPath = Path.Combine(runDirectory, "controls.json");
        ControlSuite.Write(controlsPath, evidence);
        string text = ControlSuite.Format(evidence);
        File.WriteAllText(Path.Combine(runDirectory, "controls.txt"), text);
        Console.WriteLine();
        Console.Write(text);

        int fired = 0;
        foreach (ControlEvidence control in evidence)
        {
            if (control.Fired)
            {
                fired++;
            }
        }

        Console.WriteLine($"{fired}/{evidence.Count} controls fired. Evidence: {controlsPath}");
        return fired == evidence.Count ? 0 : 1;
    }

    ResultDocument document = runner.Run(hosts, scenarios, arms);
    string resultsPath = Path.Combine(runDirectory, "results.json");
    ResultWriter.Write(resultsPath, document);

    ConformanceReport report = ResultConformance.Check(document);
    Console.WriteLine();
    Console.WriteLine(report.Format());
    Console.WriteLine($"Results: {resultsPath}");

    int valid = 0;
    int invalid = 0;
    int skippedCount = 0;
    foreach (RunResult result in document.Results)
    {
        if (result.Status == RunStatus.Skipped)
        {
            skippedCount++;
        }
        else if (result.Valid)
        {
            valid++;
        }
        else
        {
            invalid++;
        }
    }

    Console.WriteLine($"{valid} valid, {invalid} invalid, {skippedCount} declared skips.");
    return report.Ok && invalid == 0 ? 0 : 1;
}
finally
{
    // Leaving a mutated runtimeconfig.json behind would make the next manual run silently inherit the
    // last cell's collector.
    RuntimeConfigWriter.RestoreAll();
}

static string? FindRepoRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
            File.Exists(Path.Combine(directory.FullName, ".git")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    return null;
}

static int RunConformance(string? input)
{
    // The conformance check needs its own negative test, or it is as control-less as anything else.
    ResultDocument good = SampleDocument(valid: true);
    ConformanceReport goodReport = ResultConformance.Check(good);

    ResultDocument bad = SampleDocument(valid: false);
    ConformanceReport badReport = ResultConformance.Check(bad);

    Console.WriteLine("conformance self-test");
    Console.WriteLine("  well-formed document: " + (goodReport.Ok ? "accepted" : "REJECTED - " + string.Join("; ", goodReport.Errors)));
    Console.WriteLine("  malformed document:   " + (badReport.Ok ? "ACCEPTED, which is a defect" : "rejected - " + string.Join("; ", badReport.Errors)));

    bool selfTestOk = goodReport.Ok && !badReport.Ok;
    if (input is null)
    {
        Console.WriteLine(selfTestOk ? "self-test passed" : "SELF-TEST FAILED");
        return selfTestOk ? 0 : 1;
    }

    if (!File.Exists(input))
    {
        Console.Error.WriteLine($"conformance: '{input}' does not exist.");
        return 2;
    }

    ConformanceReport fileReport = ResultConformance.CheckFile(input);
    Console.WriteLine();
    Console.WriteLine($"{input}:");
    Console.WriteLine(fileReport.Format());
    return selfTestOk && fileReport.Ok ? 0 : 1;
}

static ResultDocument SampleDocument(bool valid)
{
    var document = new ResultDocument
    {
        Id = "conformance-self-test",
        Date = "2022-11-01",
        StepId = "P0.4",
        Notes = "Synthetic record used only to prove the conformance check rejects what it should.",
    };

    var result = new RunResult
    {
        Scenario = valid ? "low-allocation-compute" : "not-a-scenario",
        Collector = valid ? CollectorArms.WorkstationId : "not-an-arm",
        Host = "sdk",
        OperationsPerSecond = valid ? 1000 : -1,
        Status = RunStatus.Ok,
        Valid = true,

        // The defect that must be caught: claiming validity without a confirmed collector is exactly
        // what "a run whose collector cannot be confirmed is invalid" forbids.
        CollectorConfirmed = valid,
        LatencyMethod = valid ? OpenLoopDriver.LatencyMethod : null,
        LatencyP99Ms = 1.0,
        ArrivalRatePerSecond = 1000,
        Invocations = valid ? 3 : 0,
        RuntimeBuildId = valid ? "0000000000000000000000000000000000000000" : null,
    };

    document.Results.Add(result);
    return document;
}
