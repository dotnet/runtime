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

/// <summary>What one control demonstration produced.</summary>
public sealed class ControlEvidence
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Expectation { get; init; }

    public required bool Fired { get; init; }

    public required string Observed { get; init; }

    public List<string> Detail { get; init; } = [];
}

/// <summary>
/// Demonstrates each control by making it fail, not by watching it pass.
///
/// <para>A control that has not been shown to fire is indistinguishable from an absent one. Every
/// method here therefore injects a specific defect and asserts the harness <em>rejects</em> the run;
/// several also run the clean counterpart, because a check that rejects everything is equally
/// worthless.</para>
/// </summary>
public sealed class ControlSuite
{
    private readonly RunnerOptions _options;
    private readonly MatrixRunner _runner;
    private readonly HostDescriptor _host;

    public ControlSuite(RunnerOptions options, MatrixRunner runner, HostDescriptor host)
    {
        _options = options;
        _runner = runner;
        _host = host;
    }

    public List<ControlEvidence> RunAll()
    {
        return
        [
            CollectorIdentity(),
            CoordinatedOmission(),
            ConfigurationPinning(),
            Timeout(),
            CrashCapture(),
            SuccessMarker(),
            MeasurementResolution(),
        ];
    }

    private MatrixCell Cell(string scenario, CollectorArm arm, string tag, int timeoutSeconds = 120) => new()
    {
        Scenario = scenario,
        Arm = arm,
        Host = _host,
        Primary = ScenarioCatalog.Get(scenario).Primary,
        Invocations = 1,
        TimeoutSeconds = timeoutSeconds,
        Tag = tag,
    };

    /// <summary>
    /// Control 1. Ask for Server GC through the host-property channel while forcing Workstation
    /// through the environment. The environment channel is consulted first (gcenv.ee.cpp lines 1324
    /// and 1351), so the process really does run the other collector - this is a genuine mismatch, not
    /// a simulated one - and the harness must mark the run invalid and refuse to use it in a ratio.
    /// </summary>
    private ControlEvidence CollectorIdentity()
    {
        MatrixCell cell = Cell("low-allocation-compute", CollectorArms.Server(_options.ServerHeapCount), "c1");
        InvocationOutcome forced = _runner.LaunchCell(
            cell,
            0,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["DOTNET_gcServer"] = "0" });

        InvocationOutcome clean = _runner.LaunchCell(cell, 1);

        string? reason = ReadString(forced.Report, "invalidReason");
        bool forcedInvalid = forced.Report is JsonElement report &&
            report.TryGetProperty("valid", out JsonElement valid) && valid.ValueKind is JsonValueKind.False &&
            reason == InvalidReason.CollectorIdentityMismatch;

        // The ratio must not merely be flagged - it must not exist.
        CellAggregate withBad = Aggregator.Aggregate(cell, [forced]);
        CellAggregate withGood = Aggregator.Aggregate(cell, [clean]);
        (RatioEstimate? estimate, string? refusal) = Aggregator.Ratio(withGood, withBad);

        var detail = new List<string>
        {
            $"forced run: requested System.GC.Server=true, DOTNET_gcServer=0; observed ServerGC={ReadObserved(forced.Report, GcIdentity.ServerGcKey)}",
            $"forced run: valid={ReadBool(forced.Report, "valid")}, invalidReason={reason ?? "<null>"}, exit={forced.ExitCode}",
            $"clean run: observed ServerGC={ReadObserved(clean.Report, GcIdentity.ServerGcKey)}, valid={ReadBool(clean.Report, "valid")}",
            $"ratio containing the forced run: {(estimate is null ? "refused - " + refusal : "PUBLISHED, which is a defect")}",
        };

        return new ControlEvidence
        {
            Id = "1",
            Name = "collector identity",
            Expectation = "a run whose collector does not match the request is marked invalid and cannot contribute to a ratio",
            Fired = forcedInvalid && estimate is null && clean.Valid,
            Observed = forcedInvalid
                ? $"invalid, reason={reason}; ratio refused; clean counterpart still valid"
                : $"NOT DETECTED (valid={ReadBool(forced.Report, "valid")}, reason={reason ?? "<null>"})",
            Detail = detail,
        };
    }

    /// <summary>
    /// Control 2, the most valuable one. A stall of known duration and period is injected into the
    /// operation. The open-loop pipeline must report high percentiles that move by the predicted
    /// magnitude, and the service-time view of <em>the same operations</em> - which is what a
    /// closed-loop harness would report - must barely register it. The pair is the proof; the
    /// open-loop half alone proves nothing.
    /// </summary>
    private ControlEvidence CoordinatedOmission()
    {
        const double StallMs = 200;
        const double PeriodSeconds = 1;
        double rate = 1000;

        MatrixCell cell = Cell("allocation-churn", CollectorArms.Workstation, "c2");
        var latencyArgs = new List<string> { "--mode", "latency", "--rate", rate.ToString(CultureInfo.InvariantCulture), "--arrival", "uniform" };

        InvocationOutcome baseline = _runner.LaunchCell(cell, 0, extraArguments: latencyArgs);
        var stallArgs = new List<string>(latencyArgs)
        {
            "--inject-stall-ms", StallMs.ToString(CultureInfo.InvariantCulture),
            "--inject-stall-every-seconds", PeriodSeconds.ToString(CultureInfo.InvariantCulture),
        };
        InvocationOutcome stalled = _runner.LaunchCell(cell, 1, extraArguments: stallArgs);

        double baseP99 = ReadMetric(baseline.Report, "latencyP99Ms");
        double stallP99 = ReadMetric(stalled.Report, "latencyP99Ms");
        double baseService = ReadMetric(baseline.Report, "serviceTimeP99Ms");
        double stallService = ReadMetric(stalled.Report, "serviceTimeP99Ms");

        // With a stall of S every P seconds at rate R, a fraction f = S/(P*1000) of operations arrive
        // during a stall and are delayed on a ramp from S down to 0. The p99 of the whole distribution
        // therefore sits at (1 - 0.01/f) * S.
        double affectedFraction = StallMs / (PeriodSeconds * 1000.0);
        double predictedP99 = (1.0 - (0.01 / affectedFraction)) * StallMs;

        bool openLoopMoved = stallP99 > predictedP99 * 0.8 && stallP99 < predictedP99 * 1.25;
        bool closedLoopBlind = stallService < baseService * 4 + 1.0;

        return new ControlEvidence
        {
            Id = "2",
            Name = "coordinated omission",
            Expectation = $"open-loop p99 moves to about {predictedP99:F1} ms while the service-time view of the same run barely changes",
            Fired = openLoopMoved && closedLoopBlind,
            Observed = $"open-loop p99 {baseP99:F3} -> {stallP99:F3} ms (predicted {predictedP99:F1}); service-time p99 {baseService:F3} -> {stallService:F3} ms",
            Detail =
            [
                // Read the duration back from the run rather than restating a constant. The control
                // never passed --duration-seconds, so a local literal here described an experiment that
                // did not happen - the published parameters of a control have to be observations too.
                $"injection: {StallMs} ms every {PeriodSeconds} s, arrival rate {rate}/s uniform, {ReadTopLevel(stalled.Report, "steadyStateSeconds"):F1} s steady state",
                $"open-loop p50 {ReadMetric(baseline.Report, "latencyP50Ms"):F3} -> {ReadMetric(stalled.Report, "latencyP50Ms"):F3} ms",
                $"open-loop p99.9 {ReadMetric(baseline.Report, "latencyP999Ms"):F3} -> {ReadMetric(stalled.Report, "latencyP999Ms"):F3} ms",
                $"open-loop max {ReadMetric(baseline.Report, "latencyMaxMs"):F3} -> {ReadMetric(stalled.Report, "latencyMaxMs"):F3} ms",
                $"queue delay p99 {ReadMetric(baseline.Report, "queueDelayP99Ms"):F3} -> {ReadMetric(stalled.Report, "queueDelayP99Ms"):F3} ms",
                $"achieved rate {ReadMetric(baseline.Report, "achievedRatePerSecond"):F1} -> {ReadMetric(stalled.Report, "achievedRatePerSecond"):F1} per second (not overloaded, so this is not a capacity artefact)",
                $"service time is what a closed-loop harness reports; it moved by a factor of {(baseService > 0 ? stallService / baseService : double.NaN):F2}",
            ],
        };
    }

    /// <summary>
    /// Control 3. DATAS specifically, in both directions, plus a knob that fails to take effect.
    /// </summary>
    private ControlEvidence ConfigurationPinning()
    {
        MatrixCell serverPinned = Cell("low-allocation-compute", CollectorArms.Server(_options.ServerHeapCount), "c3");
        InvocationOutcome pinned = _runner.LaunchCell(serverPinned, 0);

        MatrixCell serverDatas = Cell("low-allocation-compute", CollectorArms.ServerDatas, "c3");
        InvocationOutcome datas = _runner.LaunchCell(serverDatas, 0);

        // A heap count the runtime will not honour: the environment channel parses GC integers as hex
        // (gcenv.ee.cpp:1338), so DOTNET_GCHeapCount=16 asks for 0x16 = 22 heaps while the arm's
        // property asks for the decimal value. The observed heap count must disagree with the request.
        InvocationOutcome mismatched = _runner.LaunchCell(
            serverPinned,
            1,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["DOTNET_GCHeapCount"] = "10" });

        string pinnedDatas = ReadObserved(pinned.Report, GcIdentity.DynamicAdaptationModeKey);
        string datasOn = ReadObserved(datas.Report, GcIdentity.DynamicAdaptationModeKey);
        string mismatchedHeaps = ReadObserved(mismatched.Report, GcIdentity.HeapCountKey);
        string? mismatchReason = ReadString(mismatched.Report, "invalidReason");

        bool datasOffWhenPinned = pinnedDatas == "0";
        bool datasOnByDefault = datasOn == "1";
        bool mismatchDetected = ReadBool(mismatched.Report, "valid") == "false" &&
            mismatchReason is InvalidReason.ConfigPinNotHonoured or InvalidReason.CollectorIdentityMismatch;

        return new ControlEvidence
        {
            Id = "3",
            Name = "configuration pinning (DATAS specifically)",
            Expectation = "pinning the heap count forces DATAS off and is observed; DATAS defaults on and is observed; a knob that does not take effect is detected",
            Fired = datasOffWhenPinned && datasOnByDefault && mismatchDetected,
            Observed = $"srv (HeapCount={_options.ServerHeapCount}) observed GCDynamicAdaptationMode={pinnedDatas}; " +
                $"srv-datas observed {datasOn}; forced-heap-count run observed HeapCount={mismatchedHeaps}, invalidReason={mismatchReason ?? "<null>"}",
            Detail =
            [
                $"srv arm requests System.GC.HeapCount={_options.ServerHeapCount}; init.cpp:792-796 forces dynamic_adaptation_mode to 0 when GetHeapCount() != 0, and interface.cpp:752 writes the effective value back",
                $"srv arm observed HeapCount={ReadObserved(pinned.Report, GcIdentity.HeapCountKey)}, ServerGC={ReadObserved(pinned.Report, GcIdentity.ServerGcKey)}",
                $"srv-datas arm observed HeapCount={ReadObserved(datas.Report, GcIdentity.HeapCountKey)} (initial only; DATAS varies it during the run)",
                $"forced run set DOTNET_GCHeapCount=10, which the environment channel parses as hexadecimal 0x10 = 16 (gcenv.ee.cpp:1338)",
                $"forced run: valid={ReadBool(mismatched.Report, "valid")}, invalidReason={mismatchReason ?? "<null>"}",
                "Workstation note: GCDynamicAdaptationMode is only written back under DYNAMIC_HEAP_COUNT (gcpriv.h:158-162), which needs MULTIPLE_HEAPS, so its value under wks is the raw request and is not asserted.",
            ],
        };
    }

    /// <summary>Control 4. A scenario that hangs must be recorded as a failed run, not a missing one.</summary>
    private ControlEvidence Timeout()
    {
        MatrixCell cell = Cell("low-allocation-compute", CollectorArms.Workstation, "c4", timeoutSeconds: 10);
        InvocationOutcome outcome = _runner.LaunchCell(cell, 0, extraArguments: ["--hang"]);

        CellAggregate aggregate = Aggregator.Aggregate(cell, [outcome]);
        RunResult result = Aggregator.ToResult(aggregate, _options.RunId, _options.NoiseThresholdPercent);

        return new ControlEvidence
        {
            Id = "4",
            Name = "timeout",
            Expectation = "the process tree is killed, the run is recorded with status timeout and valid=false, and it appears in the results rather than being absent",
            Fired = outcome.Status == RunStatus.Timeout && result.Status == RunStatus.Timeout && !result.Valid,
            Observed = $"status={outcome.Status}, wall={WorkerLauncher.FormatSeconds(outcome.WallSeconds)}s against a {cell.TimeoutSeconds}s limit, " +
                $"result status={result.Status}, valid={result.Valid}, invalidReason={result.InvalidReason}",
            Detail =
            [
                $"marker seen: {outcome.MarkerSeen} (a hung worker never prints one)",
                $"report file written: {(outcome.ReportPath is null ? "no" : outcome.ReportPath)}",
                "the run is present in results.json with status=timeout, so a hang cannot be mistaken for a cell that was never scheduled",
            ],
        };
    }

    /// <summary>
    /// Control 5. A crash must produce a bounded dump inside the run directory - never in the shared
    /// temp directory that already holds about 12 GB of stale dumps - and must invalidate the run.
    /// The budget is then exhausted deliberately to show the bound itself works.
    /// </summary>
    private ControlEvidence CrashCapture()
    {
        MatrixCell cell = Cell("low-allocation-compute", CollectorArms.Workstation, "c5", timeoutSeconds: 60);
        InvocationOutcome crashed = _runner.LaunchCell(cell, 0, extraArguments: ["--crash"]);

        long dumpBytes = crashed.DumpPath is not null && File.Exists(crashed.DumpPath)
            ? new FileInfo(crashed.DumpPath).Length
            : 0;

        var exhausted = new WorkerLauncher(Path.Combine(_runner.RunDirectory, "budget-exhausted"), 0);
        InvocationOutcome suppressed = exhausted.Launch(
            _host,
            _runner.WorkerAssemblyFor(cell.Scenario),
            CollectorArms.Workstation,
            ["--scenario", cell.Scenario, "--crash", "--warmup-seconds", "0.1", "--duration-seconds", "0.2"],
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            "budget-exhausted",
            0,
            60);

        CellAggregate aggregate = Aggregator.Aggregate(cell, [crashed]);
        RunResult result = Aggregator.ToResult(aggregate, _options.RunId, _options.NoiseThresholdPercent);

        bool inRunDirectory = crashed.DumpPath is not null &&
            Path.GetFullPath(crashed.DumpPath).StartsWith(Path.GetFullPath(_runner.RunDirectory), StringComparison.OrdinalIgnoreCase);
        bool bounded = dumpBytes > 0 && dumpBytes < _options.DumpBudgetMb * 1024L * 1024L;

        return new ControlEvidence
        {
            Id = "5",
            Name = "crash capture, bounded",
            Expectation = "a dump is written inside the run directory, is under budget, invalidates the run; and with the budget spent, no dump is written at all",
            Fired = crashed.Status == RunStatus.Crashed && inRunDirectory && bounded && !result.Valid &&
                suppressed.DumpPolicy == "suppressed-budget" && suppressed.DumpPath is null,
            Observed = $"dump={crashed.DumpPath ?? "<none>"} ({dumpBytes / 1024.0 / 1024.0:F2} MB), status={crashed.Status}, " +
                $"result valid={result.Valid}; budget-exhausted launch dumpPolicy={suppressed.DumpPolicy}, dump={suppressed.DumpPath ?? "<none>"}",
            Detail =
            [
                $"dump type 1 (normal) requested via DOTNET_DbgMiniDumpType, clrconfigvalues.h:577; the runtime default of 2 (withheap) is what produced the multi-gigabyte files already on this machine",
                $"dump directory: {Path.Combine(_runner.RunDirectory, "dumps")} - not the shared temp directory",
                $"budget: {_options.DumpBudgetMb} MB, used {(crashed.DumpPath is null ? 0 : dumpBytes / 1024 / 1024)} MB",
                $"exit code {crashed.ExitCode} (0x{(uint)crashed.ExitCode:X8})",
            ],
        };
    }

    /// <summary>
    /// Control 6. Exiting zero is not evidence of work. Two flavours: no work at all, and a run that
    /// does a tenth of the work it declared.
    /// </summary>
    private ControlEvidence SuccessMarker()
    {
        MatrixCell cell = Cell("allocation-churn", CollectorArms.Workstation, "c6", timeoutSeconds: 60);
        InvocationOutcome fake = _runner.LaunchCell(cell, 0, extraArguments: ["--fake-success"]);
        InvocationOutcome partial = _runner.LaunchCell(cell, 1, extraArguments: ["--partial-work", "0.1"]);
        InvocationOutcome honest = _runner.LaunchCell(cell, 2);

        CellAggregate fakeAggregate = Aggregator.Aggregate(cell, [fake]);
        RunResult fakeResult = Aggregator.ToResult(fakeAggregate, _options.RunId, _options.NoiseThresholdPercent);

        double honestOps = ReadMetric(honest.Report, "operationsPerSecond");
        double partialOps = ReadMetric(partial.Report, "operationsPerSecond");

        return new ControlEvidence
        {
            Id = "6",
            Name = "success marker",
            Expectation = "a worker that exits 0 without printing its completion marker is not counted as a success",
            Fired = fake.ExitCode == 0 && !fake.MarkerSeen && !fake.Valid && !fakeResult.Valid &&
                fakeResult.InvalidReason == InvalidReason.MarkerMissing,
            Observed = $"--fake-success exited {fake.ExitCode} with no marker; harness marked it valid={fake.Valid}, " +
                $"result invalidReason={fakeResult.InvalidReason}",
            Detail =
            [
                $"marker prefix looked for on stdout: '{WorkerEntryPoint.MarkerPrefix}'",
                $"honest run marker: {honest.Marker ?? "<none>"}",
                $"partial run (10% of the steady state) marker: {partial.Marker ?? "<none>"}",
                $"partial run throughput {partialOps:F0}/s versus honest {honestOps:F0}/s - similar rates, so throughput alone would not reveal the truncation",
                $"recorded steady state: honest {ReadTopLevel(honest.Report, "steadyStateSeconds"):F2} s versus partial {ReadTopLevel(partial.Report, "steadyStateSeconds"):F2} s, against {ReadTopLevel(partial.Report, "steadyStateSecondsRequested"):F2} s requested - the truncation is visible in the run's own record",
                "exit code 0 is never sufficient on its own: validity requires the marker, the checksum and the identity assertions",
            ],
        };
    }

    /// <summary>
    /// Control 7. The paper's barrier cost is 1.6% against a noise floor of about 3%. This asserts that
    /// the bootstrap estimator resolves an injected slowdown it should never miss and refuses to claim
    /// one where none was injected, then <em>measures</em> - and publishes - what this host can actually
    /// resolve at the paper's barrier scale rather than assuming the paper's floor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Run in both directions, because an interval narrow enough to exclude 1.0 on an injected effect
    /// would do so just as eagerly on no effect at all if it were simply too narrow. So an arm is
    /// launched that is identical to the baseline in every respect, and the same estimator must
    /// <em>include</em> 1.0 for it. One direction alone would not distinguish a working interval from an
    /// over-confident one.
    /// </para>
    /// <para>
    /// The separation of the asserted magnitude from the measured one is deliberate and was forced by
    /// evidence. Asserting on a barrier-scale effect made the verdict track background load rather than
    /// harness correctness; the same binary reached the 1.6% target at n=8, at n=15, and not at all on
    /// three consecutive runs. The assertion therefore uses an effect this host cannot miss, and the
    /// barrier-scale question is answered with a published number - including when that number is
    /// unwelcome, which is the case it exists for.
    /// </para>
    /// </remarks>
    private ControlEvidence MeasurementResolution()
    {
        // Two injected magnitudes, doing two different jobs.
        //
        // CoarseEveryNth carries the assertion. It is a true-positive test of the estimator: an effect
        // this large is far outside the roughly plus or minus 3% single-invocation spread measured on
        // this host, so if the estimator cannot see it, the estimator is broken - a conclusion that does
        // not depend on how busy the machine happens to be.
        //
        // FineEveryNth is the paper's question, and it is measured rather than asserted. At barrier
        // scale the effect sits inside the host's own noise: three paired 8 s probes of exactly this
        // injection returned ratios of 1.0221, 0.9649 and 0.9922 - a 2% signal under a plus or minus 3%
        // spread, with the sign flipping. That is the paper's 1.6%-inside-plus-or-minus-3% problem
        // (P0.2 section 7) reproduced on this VM, and it is a finding, not a defect. Asserting on it
        // made this control's verdict track the machine's weather: identical code reached the target at
        // n=8, at n=15, and not at all on three consecutive runs, and on the third the fine effect
        // itself came out at 0.94% against a null replicate of 0.88% - indistinguishable. A control that
        // flakes teaches the next reader to rerun until it passes, which is how a control stops meaning
        // anything.
        const int CoarseEveryNth = 5;
        const int FineEveryNth = 50;
        double coarseNominal = 1.0 - (1.0 / (CoarseEveryNth + 1.0));
        double fineNominal = 1.0 - (1.0 / (FineEveryNth + 1.0));

        MatrixCell cell = Cell("low-allocation-compute", CollectorArms.Workstation, "c7");
        int invocations = Math.Max(9, _options.Invocations);

        // A short invocation is dominated by process start and JIT warmup, which adds variance without
        // adding signal, so this control insists on a longer steady state than the smoke default even
        // when the operator asked for a quick run. Resolving a 2% effect is the whole point of it.
        double steadyState = Math.Max(8.0, _options.SteadyStateSeconds);
        string[] baseArgs = ["--duration-seconds", steadyState.ToString(CultureInfo.InvariantCulture)];
        string[] coarseArgs =
        [
            .. baseArgs,
            "--inject-extra-work-every", CoarseEveryNth.ToString(CultureInfo.InvariantCulture),
        ];
        string[] fineArgs =
        [
            .. baseArgs,
            "--inject-extra-work-every", FineEveryNth.ToString(CultureInfo.InvariantCulture),
        ];

        var baseline = new List<double>();
        var coarse = new List<double>();
        var slowed = new List<double>();
        var replicate = new List<double>();
        int dropped = 0;
        for (int i = 0; i < invocations; i++)
        {
            // Interleaved, not blocked: this host is shared, and blocking the arms would let drift
            // masquerade as the injected effect - which is precisely the error this control exists to
            // rule out. The null replicate is interleaved on the same footing for the same reason, and
            // it is what caught the drift on the run that prompted this design: baseline ran 0.88%
            // faster than an identical uninjected arm, which alone accounted for most of the fine
            // arm's apparent effect.
            Collect(_runner.LaunchCell(cell, i * 4, extraArguments: baseArgs), baseline);
            Collect(_runner.LaunchCell(cell, (i * 4) + 1, extraArguments: coarseArgs), coarse);
            Collect(_runner.LaunchCell(cell, (i * 4) + 2, extraArguments: fineArgs), slowed);
            Collect(_runner.LaunchCell(cell, (i * 4) + 3, extraArguments: baseArgs), replicate);
        }

        void Collect(InvocationOutcome outcome, List<double> into)
        {
            if (outcome.Valid)
            {
                into.Add(ReadMetric(outcome.Report, "operationsPerSecond"));
            }
            else
            {
                dropped++;
            }
        }

        if (baseline.Count < 2 || coarse.Count < 2 || slowed.Count < 2 || replicate.Count < 2)
        {
            return new ControlEvidence
            {
                Id = "7",
                Name = "measurement resolution",
                Expectation = "an injected slowdown is resolved, and no difference is claimed where none was injected",
                Fired = false,
                Observed = $"insufficient valid invocations (baseline {baseline.Count}, coarse {coarse.Count}, " +
                    $"fine {slowed.Count}, replicate {replicate.Count})",
            };
        }

        RatioEstimate coarseEffect = Stats.BootstrapRatio([.. baseline], [.. coarse]);
        RatioEstimate effect = Stats.BootstrapRatio([.. baseline], [.. slowed]);
        RatioEstimate nullRatio = Stats.BootstrapRatio([.. baseline], [.. replicate]);

        // Reporting the half-width at one invocation count answers "can this host do it?" with a bare
        // yes or no, and invites tuning the count until the answer is yes - which would be choosing the
        // sample size from the result. Instead the estimator is re-run over prefixes of the same
        // interleaved sequence, so the entire ladder is published and the required count is read off
        // it. A prefix is an earlier contiguous window of a single interleaved run, not a subset picked
        // for its value.
        const double BarrierCostFraction = 0.016;
        int usable = Math.Min(baseline.Count, slowed.Count);
        var ladder = new List<string>();
        int requiredInvocations = 0;
        for (int n = 3; n <= usable; n++)
        {
            RatioEstimate step = Stats.BootstrapRatio([.. baseline.GetRange(0, n)], [.. slowed.GetRange(0, n)]);
            bool meets = step.HalfWidthFraction <= BarrierCostFraction;
            if (meets && requiredInvocations == 0)
            {
                requiredInvocations = n;
            }

            ladder.Add($"n={n}: ratio {step.Ratio:F4} [{step.Low:F4}, {step.High:F4}], half-width {step.HalfWidthFraction * 100:F2}%{(meets ? " <= 1.6%" : string.Empty)}");
        }

        bool resolvesCoarse = coarseEffect.ExcludesUnity && coarseEffect.Ratio < 1.0;
        bool refusesNull = !nullRatio.ExcludesUnity;
        bool resolvesFine = effect.ExcludesUnity && effect.Ratio < 1.0;
        bool reachesTarget = requiredInvocations > 0;
        bool ladderBuilt = ladder.Count > 0;

        // The blind-band assertion. P0.4 designed it, declined to ship it, and said why: an assertion
        // nobody has watched fail is indistinguishable from an absent one. P0.5 reopens these controls,
        // so it can be watched failing, and it is asserted here.
        //
        // What it asserts is the estimator's own precision on the fine arm, not whether the fine effect
        // is resolved. That distinction is the whole reason this can be asserted at all: the effect sits
        // inside this host's noise and its resolution flaps run to run, but the half-width is a property
        // of the sample this run actually collected. It degrades when the machine degrades, and it does
        // not depend on whether a 2% signal happened to poke above the floor today.
        //
        // The bound is generous against an observed ~1% - the same reasoning that put the coarse
        // injection at 16.7% rather than 2%. It leaves a real blind band between roughly 2% and the
        // bound where a resolution regression still fails nothing. That band is published in
        // P0.5-baselines.md rather than closed, because closing it needs a quieter machine.
        double fineHalfWidth = effect.HalfWidthFraction;
        bool fineWithinBound = !double.IsNaN(fineHalfWidth) && fineHalfWidth <= _options.FineHalfWidthBound;

        return new ControlEvidence
        {
            Id = "7",
            Name = "measurement resolution",
            Expectation = $"a ~{(1 - coarseNominal) * 100:F1}% injected slowdown gives an interval excluding 1.0 on the low side, " +
                "and an uninjected replicate of the same arm gives an interval including 1.0. " +
                $"The half-width of the ~{(1 - fineNominal) * 100:F1}% barrier-scale arm stays within " +
                $"{_options.FineHalfWidthBound * 100:F0}%, so a catastrophic loss of resolution fails rather than being published. " +
                "Whether that arm's effect is itself resolved is measured and reported, not asserted",
            Fired = resolvesCoarse && refusesNull && ladderBuilt && fineWithinBound,
            Observed = $"coarse ~{(1 - coarseNominal) * 100:F1}% injection {coarseEffect.Ratio:F4} " +
                $"[{coarseEffect.Low:F4}, {coarseEffect.High:F4}] excludes 1.0: {coarseEffect.ExcludesUnity}; " +
                $"null replicate {nullRatio.Ratio:F4} [{nullRatio.Low:F4}, {nullRatio.High:F4}] includes 1.0: {refusesNull}; " +
                $"fine ~{(1 - fineNominal) * 100:F1}% injection {effect.Ratio:F4} [{effect.Low:F4}, {effect.High:F4}] " +
                $"half-width {fineHalfWidth * 100:F2}% <= {_options.FineHalfWidthBound * 100:F0}%: {fineWithinBound}; " +
                $"that arm's effect excludes 1.0: {resolvesFine} (measured, not asserted)",
            Detail =
            [
                $"ASSERTED - coarse injection: every {CoarseEveryNth}th operation is performed twice, a nominal " +
                    $"1/{CoarseEveryNth + 1} = {(1 - coarseNominal) * 100:F2}% of the scenario's own work. This is a true-positive test of the " +
                    "estimator, set far enough above the host's single-invocation spread that its verdict does not depend on background load.",
                $"ASSERTED - null replicate: an arm identical to the baseline in every respect. Without it, an interval too narrow to be " +
                    "trusted would look like a success on the injected arm; this is the direction that catches over-confidence.",
                $"MEASURED - fine injection: every {FineEveryNth}th operation is performed twice, a nominal " +
                    $"1/{FineEveryNth + 1} = {(1 - fineNominal) * 100:F2}%, chosen to sit at the paper's 1.6% barrier-cost scale. " +
                    "Whether the effect is resolved is reported, never asserted - see the P0.5 input below for why.",
                $"ASSERTED - blind-band bound: the fine arm's half-width must stay within {_options.FineHalfWidthBound * 100:F0}%. " +
                    $"Observed {fineHalfWidth * 100:F2}%. This asserts the estimator's precision, not the effect's significance, which is why " +
                    "it can be asserted at all where 'the fine effect is resolved' could not. A resolution regression between roughly 2% and " +
                    $"{_options.FineHalfWidthBound * 100:F0}% still fails nothing: that blind band is a published limitation of this control, not a closed gap.",
                $"{baseline.Count} baseline, {coarse.Count} coarse, {slowed.Count} fine and {replicate.Count} null-replicate invocations, " +
                    $"interleaved A/B/C/D, {steadyState:F0} s steady state each, {dropped} invocation(s) dropped as invalid",
                $"{Aggregator.CiMethodDescription}",
                "MEASURED RESOLUTION OF THIS HOST - half-width on the fine pair as the invocation count grows:",
                .. ladder.ConvertAll(line => "    " + line),
                reachesTarget
                    ? $"P0.5 INPUT: on this host, {requiredInvocations} interleaved invocations of {steadyState:F0} s bring the half-width " +
                        $"to {BarrierCostFraction * 100:F1}%. Fewer than that cannot settle a barrier-cost question here."
                    : $"P0.5 INPUT: this host did not reach a {BarrierCostFraction * 100:F1}% half-width at any count up to {usable}. A barrier-cost " +
                        "question cannot be settled here without more invocations, a longer steady state, or a quieter machine.",
                resolvesFine
                    ? $"P0.5 INPUT: the ~{(1 - fineNominal) * 100:F1}% injected effect WAS resolved on this run. That is not guaranteed run to run - see below."
                    : $"P0.5 INPUT: the ~{(1 - fineNominal) * 100:F1}% injected effect was NOT resolved on this run, despite the coarse effect being resolved. " +
                        "The estimator is working; the effect is simply at or below this host's floor today.",
                "P0.5 INPUT: three paired 8 s probes of the fine injection, run directly against the worker, returned ratios of 1.0221, 0.9649 " +
                    "and 0.9922 - a 2% signal under a plus or minus 3% single-invocation spread, with the sign flipping. The paper's own " +
                    "1.6%-inside-plus-or-minus-3% problem (P0.2 section 7) is therefore reproduced on this VM. Any barrier-cost claim in P0.5 must " +
                    "clear this floor with invocation count, not with a single comparison.",
                "baseline samples (ops/s): " + string.Join(", ", baseline.ConvertAll(v => v.ToString("F0", CultureInfo.InvariantCulture))),
                "coarse samples (ops/s): " + string.Join(", ", coarse.ConvertAll(v => v.ToString("F0", CultureInfo.InvariantCulture))),
                "fine samples (ops/s): " + string.Join(", ", slowed.ConvertAll(v => v.ToString("F0", CultureInfo.InvariantCulture))),
                "null replicate samples (ops/s): " + string.Join(", ", replicate.ConvertAll(v => v.ToString("F0", CultureInfo.InvariantCulture))),
            ],
        };
    }

    private static double ReadMetric(JsonElement? report, string name)
    {
        if (report is not JsonElement element || !element.TryGetProperty("metrics", out JsonElement metrics))
        {
            return double.NaN;
        }

        return metrics.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.Number
            ? value.GetDouble()
            : double.NaN;
    }

    private static double ReadTopLevel(JsonElement? report, string name)
    {
        if (report is not JsonElement element)
        {
            return double.NaN;
        }

        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.Number
            ? value.GetDouble()
            : double.NaN;
    }

    private static string ReadObserved(JsonElement? report, string key)
    {
        if (report is not JsonElement element || !element.TryGetProperty("observedGcConfig", out JsonElement config))
        {
            return "<no report>";
        }

        return config.TryGetProperty(key, out JsonElement value) ? value.ToString() : "<absent>";
    }

    private static string? ReadString(JsonElement? report, string name) =>
        report is JsonElement element && element.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ReadBool(JsonElement? report, string name) =>
        report is JsonElement element && element.TryGetProperty(name, out JsonElement value)
            ? value.ValueKind switch
            {
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => "<not a boolean>",
            }
            : "<no report>";

    public static void Write(string path, IReadOnlyList<ControlEvidence> evidence)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", RunResult.SchemaVersion);
        writer.WriteString("stepId", "P0.4");
        writer.WriteStartArray("controls");
        foreach (ControlEvidence control in evidence)
        {
            writer.WriteStartObject();
            writer.WriteString("id", control.Id);
            writer.WriteString("name", control.Name);
            writer.WriteString("expectation", control.Expectation);
            writer.WriteBoolean("fired", control.Fired);
            writer.WriteString("observed", control.Observed);
            writer.WriteStartArray("detail");
            foreach (string line in control.Detail)
            {
                writer.WriteStringValue(line);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
    }

    public static string Format(IReadOnlyList<ControlEvidence> evidence)
    {
        var text = new StringBuilder();
        foreach (ControlEvidence control in evidence)
        {
            text.Append("control ").Append(control.Id).Append(" - ").Append(control.Name).Append(": ")
                .AppendLine(control.Fired ? "FIRED" : "DID NOT FIRE");
            text.Append("  expectation: ").AppendLine(control.Expectation);
            text.Append("  observed:    ").AppendLine(control.Observed);
            foreach (string line in control.Detail)
            {
                text.Append("    - ").AppendLine(line);
            }

            text.AppendLine();
        }

        return text.ToString();
    }
}
