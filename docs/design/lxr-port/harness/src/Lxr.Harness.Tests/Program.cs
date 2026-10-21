// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Lxr.Harness.Core;
using Lxr.Harness.Runner;
using Lxr.Harness.Scenarios;
using static Lxr.Harness.Tests.TestRunner;

Console.WriteLine("Lxr.Harness.Tests");
Console.WriteLine();

Console.WriteLine("Stats.Percentile");
Test("nearest rank picks a real sample, never an interpolated one", () =>
{
    double[] sorted = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    Equal(1.0, Stats.Percentile(sorted, 0), "the 0th percentile is the smallest sample");
    Equal(5.0, Stats.Percentile(sorted, 50), "nearest rank at 50% of 10 samples is rank 5");
    Equal(10.0, Stats.Percentile(sorted, 100), "the 100th percentile is the largest sample");
    Equal(10.0, Stats.Percentile(sorted, 99.99), "a percentile finer than the sample count saturates at the maximum");
});

Test("a single sample answers every percentile", () =>
{
    double[] one = [42];
    Equal(42.0, Stats.Percentile(one, 0), "0th");
    Equal(42.0, Stats.Percentile(one, 99.99), "99.99th");
});

Test("an empty sample is NaN rather than zero", () =>
{
    // Zero would read as an excellent latency. NaN propagates into the report instead.
    True(double.IsNaN(Stats.Percentile([], 99)), "an empty distribution has no 99th percentile");
});

Test("an out-of-range percentile is rejected", () =>
{
    Throws<ArgumentOutOfRangeException>(() => Stats.Percentile([1], 101), "above 100");
    Throws<ArgumentOutOfRangeException>(() => Stats.Percentile([1], -1), "below 0");
});

Console.WriteLine();
Console.WriteLine("Stats.BootstrapRatio");
Test("identical arms give a ratio of 1.0 with an interval containing it", () =>
{
    double[] a = [100, 101, 99, 100, 102, 98, 100, 101, 99];
    RatioEstimate estimate = Stats.BootstrapRatio(a, a);
    Close(1.0, estimate.Ratio, 1e-9, "the same data compared with itself is exactly 1.0");
    False(estimate.ExcludesUnity, "an interval on identical arms must not claim a difference");
});

Test("a large separation is resolved", () =>
{
    double[] baseline = [100, 101, 99, 100, 102, 98, 100];
    double[] candidate = [200, 201, 199, 200, 202, 198, 200];
    RatioEstimate estimate = Stats.BootstrapRatio(baseline, candidate);
    Close(2.0, estimate.Ratio, 0.05, "candidate is twice baseline");
    True(estimate.ExcludesUnity, "a 2x difference must be resolvable");
});

Test("wide overlapping arms are NOT resolved", () =>
{
    // The direction that matters. An estimator that always excludes 1.0 would make control 7
    // meaningless and every ratio in a report a false positive.
    double[] baseline = [50, 150, 60, 140, 55, 145, 70];
    double[] candidate = [52, 148, 63, 139, 58, 143, 68];
    RatioEstimate estimate = Stats.BootstrapRatio(baseline, candidate);
    False(estimate.ExcludesUnity, "arms this noisy must not be declared different");
});

Test("the interval is reproducible for a given seed", () =>
{
    double[] baseline = [10, 12, 11, 13, 9, 14, 10];
    double[] candidate = [11, 13, 12, 14, 10, 15, 11];
    RatioEstimate first = Stats.BootstrapRatio(baseline, candidate, seed: 7);
    RatioEstimate second = Stats.BootstrapRatio(baseline, candidate, seed: 7);
    Equal(first.Low, second.Low, "the same seed must give the same lower bound");
    Equal(first.High, second.High, "the same seed must give the same upper bound");

    RatioEstimate other = Stats.BootstrapRatio(baseline, candidate, seed: 8);
    True(other.Low != first.Low || other.High != first.High, "a different seed must actually resample differently");
});

Test("half-width is a fraction of the point estimate", () =>
{
    var estimate = new RatioEstimate(2.0, 1.8, 2.2, 100);
    Close(0.1, estimate.HalfWidthFraction, 1e-12, "(2.2-1.8)/2/2.0 = 0.1");
});

Test("an empty arm is rejected rather than producing a NaN ratio", () =>
{
    Throws<ArgumentOutOfRangeException>(() => Stats.BootstrapRatio([], [1, 2, 3]), "empty baseline");
    Throws<ArgumentOutOfRangeException>(() => Stats.BootstrapRatio([1, 2, 3], []), "empty candidate");
});

Console.WriteLine();
Console.WriteLine("Stats.GeometricMean");
Test("geometric mean matches the hand-computed value", () =>
{
    Close(2.0, Stats.GeometricMean([1, 2, 4]), 1e-9, "cube root of 8");
    True(double.IsNaN(Stats.GeometricMean([1, 0, 4])), "a zero has no geometric mean");
    True(double.IsNaN(Stats.GeometricMean([1, -2, 4])), "a negative has no geometric mean");
});

Test("geometric and arithmetic means differ, which is why the choice is recorded", () =>
{
    // P0.2 section 7's barrier figure is a geometric mean. Rolling up with an arithmetic mean would
    // silently disagree with the number being compared against.
    double[] ratios = [0.5, 2.0];
    Close(1.0, Stats.GeometricMean(ratios), 1e-9, "geometric mean of 0.5 and 2.0 is 1.0");
    Close(1.25, Stats.Mean(ratios), 1e-9, "arithmetic mean of the same pair is 1.25");
});

Console.WriteLine();
Console.WriteLine("OpenLoopDriver apparatus (P0.5)");
Test("per-operation bookkeeping is held off the GC heap", () =>
{
    // The apparatus is pre-sized so the measured region allocates nothing. Pre-sizing it as a
    // *managed* array satisfies that only while the heap is unbounded: the buffer is charged to the
    // very heap the experiment pins, so at a 1.3x heap factor the instrumentation competes with the
    // workload for the budget under study. In the P0.5 latency matrix it exhausted the heap outright
    // before the first operation - 140 invocations died in 0.25 s. This budget is what detects that.
    static (int Operations, long Allocated, long Apparatus) Measure(double seconds)
    {
        var options = new OpenLoopOptions
        {
            ArrivalRatePerSecond = 20_000,
            WarmupSeconds = 0.05,
            SteadyStateSeconds = seconds,
            Distribution = ArrivalDistribution.Uniform,
            WorkerCount = 1,
        };

        long before = GC.GetTotalAllocatedBytes(precise: true);
        using MeasuredRun run = OpenLoopDriver.Run(new NonAllocatingScenario(), options);
        return (run.RecordCount, GC.GetTotalAllocatedBytes(precise: true) - before, run.ApparatusBytes);
    }

    _ = Measure(0.05);

    // The bounded index queue and the worker threads are a fixed cost that does not scale, so an
    // absolute budget would measure them instead. The defect was that cost *scaled* with the
    // operation count, so the slope between two run sizes is what has to be flat.
    (int smallOps, long smallBytes, _) = Measure(0.15);
    (int largeOps, long largeBytes, long largeApparatus) = Measure(0.60);

    Equal((long)largeOps * 56, largeApparatus, "apparatus bytes account for both the records and the schedule");

    int extraOperations = largeOps - smallOps;
    long extraManaged = largeBytes - smallBytes;
    long extraApparatus = (long)extraOperations * 56;

    True(extraOperations >= 5_000, $"the two runs must differ enough to expose a slope, got {smallOps} then {largeOps}");
    True(
        extraManaged * 6 < extraApparatus,
        $"{extraOperations} extra operations moved {extraApparatus} B of apparatus off-heap but added {extraManaged} B of managed allocation " +
        $"({smallOps} ops = {smallBytes} B, {largeOps} ops = {largeBytes} B); a managed records array would add about {extraApparatus} B");
});

Console.WriteLine();
Console.WriteLine("SampleStore");
Test("raw samples round-trip through the compressed store", () =>
{
    string path = Path.Combine(Path.GetTempPath(), $"lxr-samples-{Guid.NewGuid():N}.bin.gz");
    try
    {
        using var records = new NativeBuffer<OperationRecord>(3);
        for (int i = 0; i < records.Length; i++)
        {
            records[i] = new OperationRecord
            {
                IntendedTimestamp = 1000 + i,
                ServiceStartTimestamp = 1005 + i,
                DispatchTimestamp = 1002 + i,
                EndTimestamp = 1020 + i,
                Value = 7 * i,
                Phase = i == 0 ? 0 : 1,
            };
        }

        var run = new MeasuredRun
        {
            Records = records,
            ApparatusBytes = records.ByteCount,
            RecordCount = records.Length,
            SteadyStateCount = 2,
            RequestedRatePerSecond = 1000,
            AchievedRatePerSecond = 999.7,
            LateCount = 0,
            BacklogMax = 1,
            Overloaded = false,
            Checksum = 21,
            WallSeconds = 1.5,
        };
        SampleStore.WriteOpenLoop(path, run, runStartTimestamp: 1000);

        (long frequency, OperationRecord[] read) = SampleStore.ReadOpenLoop(path);
        Equal(System.Diagnostics.Stopwatch.Frequency, frequency, "the frequency is stored so ticks need no guessing");
        Equal(3, read.Length, "all three records come back");
        Equal(0L, read[0].IntendedTimestamp, "timestamps are stored relative to the run start");
        Equal(20L, read[0].EndTimestamp, "end timestamp survives the round trip");
        Equal(2L, read[0].DispatchTimestamp, "the dispatcher's own release time survives, so apparatus lag stays separable");
        Equal(14L, read[2].Value, "the operation value survives the round trip");
        Equal(0, read[0].Phase, "the warmup phase marker survives, so the boundary can be re-chosen later");
        Equal(1, read[1].Phase, "the steady-state phase marker survives");
    }
    finally
    {
        File.Delete(path);
    }
});

Test("a file that is not a sample store is rejected", () =>
{
    string path = Path.Combine(Path.GetTempPath(), $"lxr-notsamples-{Guid.NewGuid():N}.bin.gz");
    try
    {
        File.WriteAllText(path, "this is not a sample file");
        Throws<Exception>(() => SampleStore.ReadOpenLoop(path), "a foreign file must not be read as samples");
    }
    finally
    {
        File.Delete(path);
    }
});

Console.WriteLine();
Console.WriteLine("MatrixPlanner.InterleavedOrder");
Test("arms alternate rather than blocking", () =>
{
    List<(MatrixCell Cell, int Invocation)> order = MatrixPlanner.InterleavedOrder(TwoArmCells(3), seed: 1);
    Equal(6, order.Count, "three invocations of two arms");
    for (int round = 0; round < 3; round++)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal)
        {
            order[round * 2].Cell.Arm.Id,
            order[(round * 2) + 1].Cell.Arm.Id,
        };

        Equal(2, seen.Count, $"round {round} must contain both arms, not the same arm twice");
        Equal(round, order[round * 2].Invocation, "invocation index advances one round at a time");
        Equal(round, order[(round * 2) + 1].Invocation, "both arms are at the same invocation index");
    }
});

Test("arm order within a round is shuffled, not fixed", () =>
{
    // All-A-then-all-B would let thermal drift masquerade as an effect, and so would a fixed A,B,A,B
    // in which A always warms the caches for B.
    var firstPositions = new HashSet<string>(StringComparer.Ordinal);
    for (int seed = 0; seed < 20; seed++)
    {
        List<(MatrixCell Cell, int Invocation)> order = MatrixPlanner.InterleavedOrder(TwoArmCells(4), seed);
        for (int round = 0; round < 4; round++)
        {
            firstPositions.Add(order[round * 2].Cell.Arm.Id);
        }
    }

    Equal(2, firstPositions.Count, "both arms must appear first sometimes, or the shuffle does nothing");
});

Test("the order is reproducible for a given seed", () =>
{
    List<(MatrixCell Cell, int Invocation)> a = MatrixPlanner.InterleavedOrder(TwoArmCells(4), seed: 99);
    List<(MatrixCell Cell, int Invocation)> b = MatrixPlanner.InterleavedOrder(TwoArmCells(4), seed: 99);
    for (int i = 0; i < a.Count; i++)
    {
        Equal(a[i].Cell.Id, b[i].Cell.Id, $"position {i} must be identical for the same seed");
        Equal(a[i].Invocation, b[i].Invocation, $"position {i} invocation must be identical for the same seed");
    }
});

Test("a tag makes two otherwise identical cells distinct", () =>
{
    // The defect this guards against was real. Every control reused one cell id, so a crash dump
    // written by control 5 was still on disk when control 7 launched and was attributed to it,
    // marking a clean run as crashed and silently dropping a good measurement out of the interval.
    string untagged = Cell(CollectorArms.Workstation, 1, tag: null).Id;
    string tagged = Cell(CollectorArms.Workstation, 1, tag: "c5").Id;
    True(tagged != untagged, "a tag must change the cell id, or per-control artifacts collide");
});

Console.WriteLine();
Console.WriteLine("ScenarioCatalog");
Test("the catalogue holds the ten scenarios the brief specifies", () =>
{
    Equal(10, ScenarioCatalog.All.Count, "ten scenarios, no more and no fewer");
});

Test("every catalogued scenario has a timeout and an implementation in the right assembly", () =>
{
    foreach (ScenarioCatalog.Entry entry in ScenarioCatalog.All)
    {
        True(entry.DefaultTimeoutSeconds > 0, $"{entry.Id} must declare a timeout");

        bool needsAspNet = (entry.RequiredCapabilities & HostCapabilities.AspNetCoreSharedFramework) != 0;
        bool inSharedRegistry = ScenarioRegistry.TryCreate(entry.Id, out _);

        // The split is deliberate. Lxr.Harness.Scenarios must not carry an ASP.NET framework
        // reference or it could not load on a CoreRun host at all, so the one scenario that needs
        // Kestrel lives in the separate ASP.NET worker and is absent from this registry by design.
        // Asserting the correspondence rather than mere presence is what makes that a decision
        // instead of an oversight.
        Equal(!needsAspNet, inSharedRegistry, $"{entry.Id}: ASP.NET scenarios belong to the ASP.NET worker, all others to the shared registry");
    }
});

Test("every registered implementation is catalogued", () =>
{
    // The reverse direction. An implementation with no catalogue entry would have no timeout, no
    // declared capability requirement and no place in the matrix, yet would still be launchable.
    foreach (string id in ScenarioRegistry.Ids)
    {
        True(ScenarioCatalog.Find(id) is not null, $"{id} is implemented but not catalogued");
    }
});

Test("at least one latency-primary scenario runs without ASP.NET", () =>
{
    // The gate asserts this too. It matters because the flagship scenario cannot reach a CoreRun
    // host, so without this the future LXR arm would have no application-latency evidence at all.
    bool any = false;
    foreach (ScenarioCatalog.Entry entry in ScenarioCatalog.All)
    {
        if (entry.SupportsLatency && (entry.RequiredCapabilities & HostCapabilities.AspNetCoreSharedFramework) == 0)
        {
            any = true;
            break;
        }
    }

    True(any, "every host must be able to produce some open-loop latency evidence");
});

Test("scenarios runnable on a bare host exclude exactly the ASP.NET one", () =>
{
    IReadOnlyList<string> bare = ScenarioCatalog.RunnableOn(HostCapabilities.None);
    Equal(ScenarioCatalog.All.Count - 1, bare.Count, "one scenario needs the ASP.NET shared framework");
    False(new List<string>(bare).Contains("aspnet-request-load"), "the Kestrel scenario cannot run on a host without ASP.NET");
});

Console.WriteLine();
Console.WriteLine("ResultConformance");
Test("a well-formed document is accepted", () =>
{
    ConformanceReport report = ResultConformance.Check(Document(Sample()));
    True(report.Ok, "a document built by the harness itself must conform: " + string.Join("; ", report.Errors));
});

Test("a latency result missing its method is rejected", () =>
{
    // Records without latencyMethod must be treated as not coordinated-omission-free, so a result
    // that carries percentiles has to say how they were obtained.
    RunResult result = Sample();
    result.LatencyMethod = null;
    False(ResultConformance.Check(Document(result)).Ok, "a latency result with no method must be rejected");
});

Test("a document naming an unknown scenario is rejected", () =>
{
    False(ResultConformance.Check(Document(Sample("not-a-scenario"))).Ok, "a scenario outside the catalogue must be rejected");
});

Test("a valid result that never confirmed its collector is rejected", () =>
{
    // "A run whose collector cannot be confirmed is invalid" is the brief's correctness criterion, so
    // the schema must not be able to express a confirmed-nothing valid run.
    RunResult result = Sample();
    result.CollectorConfirmed = false;
    False(ResultConformance.Check(Document(result)).Ok, "valid=true with collectorConfirmed=false is a contradiction");
});

Test("a valid result that did not finish is rejected", () =>
{
    RunResult result = Sample();
    result.Status = RunStatus.Timeout;
    False(ResultConformance.Check(Document(result)).Ok, "a timed-out run cannot also be valid");
});

Test("a negative throughput is rejected", () =>
{
    RunResult result = Sample();
    result.OperationsPerSecond = -1;
    False(ResultConformance.Check(Document(result)).Ok, "a negative rate is not a measurement");
});

Test("an open-loop result with no arrival rate is rejected", () =>
{
    RunResult result = Sample();
    result.ArrivalRatePerSecond = null;
    False(ResultConformance.Check(Document(result)).Ok, "an open-loop latency number is meaningless without the offered load");
});

Test("a declared skip explains itself through skipReason, an undeclared one does not", () =>
{
    // A skip is a declared non-run, not an invalid run, so its explanation lives in skipReason and
    // demanding invalidReason as well would only invite the uninformative invalidReason "skipped".
    // Both directions matter: the accept direction is what the corerun host's aspnet-request-load
    // row needs, and the reject direction is what stops a skip from being a silent absence.
    RunResult declared = Sample();
    declared.Status = RunStatus.Skipped;
    declared.Valid = false;
    declared.SkipReason = "host-lacks-aspnetcore-shared-framework";
    declared.InvalidReason = null;
    True(ResultConformance.Check(Document(declared)).Ok, "a skip with a skipReason must be accepted");

    RunResult undeclared = Sample();
    undeclared.Status = RunStatus.Skipped;
    undeclared.Valid = false;
    undeclared.SkipReason = null;
    undeclared.InvalidReason = null;
    False(ResultConformance.Check(Document(undeclared)).Ok, "a skip with no reason is a silent absence");
});

Test("a valid result with no runtime build identity is rejected", () =>
{
    // Section 5.2 of the brief: a number has to be tie-able to a commit later. A result that cannot
    // name the runtime it ran on is not re-checkable, so it does not conform.
    RunResult result = Sample();
    result.RuntimeBuildId = null;
    False(ResultConformance.Check(Document(result)).Ok, "a valid run must identify the runtime build it measured");
});

Test("conformance is checked against the written file, not the object model", () =>
{
    // Checking the in-memory object would attest to the object, not to the file anyone else reads.
    string path = Path.Combine(Path.GetTempPath(), $"lxr-results-{Guid.NewGuid():N}.json");
    try
    {
        ResultWriter.Write(path, Document(Sample()));
        ConformanceReport report = ResultConformance.CheckFile(path);
        True(report.Ok, "a written document must conform: " + string.Join("; ", report.Errors));

        File.WriteAllText(path, "{ \"schemaVersion\": 2, \"results\": [ { \"scenario\": \"nope\" } ] }");
        False(ResultConformance.CheckFile(path).Ok, "a hand-written malformed file must be rejected");

        File.WriteAllText(path, "{ not json at all");
        False(ResultConformance.CheckFile(path).Ok, "unparseable content must be rejected, not thrown past the caller");
    }
    finally
    {
        File.Delete(path);
    }
});

Test("the emitted document nests its rows under the canvas's checkpoints array", () =>
{
    // The board loads every *.json in its directory, merges each file's "checkpoints" array and
    // orders by date. A document that carried its rows at the top level would be valid JSON,
    // contribute nothing and report no error - the quietest possible failure. This test exists
    // because the emitter did exactly that until the v1 schema was read from the artifact rather
    // than from a description of it.
    string path = Path.Combine(Path.GetTempPath(), $"lxr-shape-{Guid.NewGuid():N}.json");
    try
    {
        ResultWriter.Write(path, Document(Sample()));
        using JsonDocument parsed = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = parsed.RootElement;

        True(root.TryGetProperty("checkpoints", out JsonElement checkpoints), "the document must carry a checkpoints array");
        True(checkpoints.ValueKind is JsonValueKind.Array && checkpoints.GetArrayLength() == 1, "one Write call is one checkpoint");
        True(checkpoints[0].TryGetProperty("results", out _), "the results live inside the checkpoint, not beside it");
        False(root.TryGetProperty("results", out _), "results must not also sit at the top level");

        foreach (string field in new[] { "id", "date", "stepId", "notes" })
        {
            True(checkpoints[0].TryGetProperty(field, out _), $"checkpoint field '{field}' belongs inside the checkpoint");
        }
    }
    finally
    {
        File.Delete(path);
    }

    // And the reject direction: the flattened shape the emitter used to produce must not conform.
    string flattened = """
    { "schemaVersion": 2, "id": "x", "date": "2026-08-11", "stepId": "P0.4", "notes": "", "results": [] }
    """;
    string flatPath = Path.Combine(Path.GetTempPath(), $"lxr-flat-{Guid.NewGuid():N}.json");
    try
    {
        File.WriteAllText(flatPath, flattened);
        False(ResultConformance.CheckFile(flatPath).Ok, "a top-level 'results' document contributes nothing to the board and must be rejected");
    }
    finally
    {
        File.Delete(flatPath);
    }
});

Test("a ratio is refused rather than fabricated when an arm has too few invocations", () =>
{
    // At n=1 every bootstrap resample draws the same single value, so the interval collapses to zero
    // width and reports that it excludes 1.0 for any ratio at all - asserting significance for the
    // difference between two single runs. Refusing is the only honest output, and the refusal has to
    // name the reason so it is visible rather than silent.
    for (int n = 1; n < Aggregator.MinimumInvocationsForBootstrap; n++)
    {
        CellAggregate baseline = AggregateWithSamples(CollectorArms.Workstation, n, 100.0);
        CellAggregate candidate = AggregateWithSamples(CollectorArms.Server(8), n, 103.0);

        (RatioEstimate? estimate, string? refusal) = Aggregator.Ratio(baseline, candidate);
        True(estimate is null, $"a ratio must not be published from {n} invocation(s) per arm");
        True(refusal is not null && refusal.Contains("fewer than", StringComparison.Ordinal),
            $"the refusal at n={n} must say why: {refusal}");
    }

    // And the accept direction, or the check above would pass just as well if Ratio always refused.
    CellAggregate okBaseline = AggregateWithSamples(CollectorArms.Workstation, Aggregator.MinimumInvocationsForBootstrap, 100.0);
    CellAggregate okCandidate = AggregateWithSamples(CollectorArms.Server(8), Aggregator.MinimumInvocationsForBootstrap, 103.0);
    (RatioEstimate? okEstimate, string? okRefusal) = Aggregator.Ratio(okBaseline, okCandidate);
    True(okEstimate is not null, $"a ratio must be published at the floor: {okRefusal}");
    True(okEstimate!.Value.High > okEstimate.Value.Low, "a published interval must have non-zero width");
});

Test("mean GC pause divides by suspensions, not by triple-counted collection totals", () =>
{
    // GC.CollectionCount(0) counts every collection of gen0 or higher, so Gen1 and Gen2 are subsets of
    // Gen0 rather than disjoint from it. Summing the three counts gen1 twice and gen2 three times,
    // deflating the mean by a factor that varies with the generation mix - so it would not even cancel
    // between the two arms being compared.
    var summary = new GcSummary
    {
        Gen0Collections = 20,
        Gen1Collections = 19,
        Gen2Collections = 2,
        InducedCollections = 0,
        TotalPauseMs = 79.156,
        ObservedPauseCount = 20,
        PauseShortfall = 0,
        PauseSamplesMs = [],
        PauseSource = "test",
    };

    double expected = 79.156 / 20;
    True(summary.MeanPauseMs is double mean && Math.Abs(mean - expected) < 1e-9,
        $"expected {expected:F4} ms from 20 suspensions, got {summary.MeanPauseMs}");

    double tripleCounted = 79.156 / (20 + 19 + 2);
    True(summary.MeanPauseMs is double m2 && Math.Abs(m2 - tripleCounted) > 1e-6,
        "the triple-counted denominator must not be what is returned");

    // No collection observed at all is an absent measurement, not a zero-millisecond pause.
    var empty = new GcSummary
    {
        Gen0Collections = 0,
        Gen1Collections = 0,
        Gen2Collections = 0,
        InducedCollections = 0,
        TotalPauseMs = 0,
        ObservedPauseCount = 0,
        PauseShortfall = 0,
        PauseSamplesMs = [],
        PauseSource = "test",
    };
    True(empty.MeanPauseMs is null, "a run with no collections has no mean pause");
});

Test("P3.2 pause schedule snapshots validate per-kind counts", () =>
{
    var scheduledMetrics = new PauseScheduleProbe.NativeMetrics
    {
        Schedules = 8,
        Suspends = 8,
        Resumes = 8,
        KindCountFull = 2,
        KindCountRefCount = 2,
        KindCountInitialMark = 2,
        KindCountFinalMark = 2,
    };
    ulong[][] samples =
    [
        [10, 11],
        [20, 21],
        [30, 31],
        [40, 41],
    ];
    var scheduled = new PauseScheduleSnapshot(
        "scheduled",
        policy: 1,
        countPerKind: 2,
        samples,
        scheduledMetrics);
    Equal(0, scheduled.Failures.Count, "an exact schedule snapshot passes");

    var nullMetrics = new PauseScheduleProbe.NativeMetrics
    {
        Schedules = 1,
        Suspends = 1,
        Resumes = 1,
    };
    var invalidNull = new PauseScheduleSnapshot(
        "null",
        policy: 2,
        countPerKind: 2,
        samples,
        nullMetrics);
    True(
        invalidNull.Failures.Count > 0,
        "a null control that reports a suspension fails");
});

Test("a throughput-primary scenario that ran open-loop is still judged on delivering its schedule", () =>
{
    // F16. The gate that rejects a run whose latency is its own dispatcher backlog used to sit behind
    // `cell.Primary is PrimaryMetric.Latency`. Primary is a property of the scenario catalogue entry,
    // not of the run: in P0.5's first latency matrix nine of the ten scenarios ran open-loop while
    // declaring Throughput, so the gate evaluated one scenario out of ten. Nineteen cells were
    // published valid with a lag over the bound, `lifecycle-semantics.srv` at h6.0 reporting a
    // 90.25 ms latency p99 against a 90.79 ms dispatcher lag.
    double overBound = Aggregator.MaximumUnexplainedDispatchLagMs * 2;
    CellAggregate lagging = AggregateWithReports(PrimaryMetric.Throughput, $$"""
    { "valid": true, "metrics": { "operationsPerSecond": 1000.0, "dispatchLagP99Ms": {{overBound.ToString(System.Globalization.CultureInfo.InvariantCulture)}} } }
    """);

    RunResult result = Aggregator.ToResult(lagging, "test", 100);
    False(result.Valid, $"a {overBound} ms dispatch lag exceeds the {Aggregator.MaximumUnexplainedDispatchLagMs} ms bound whatever the scenario declares");
    Equal(InvalidReason.ScheduleNotDelivered, result.InvalidReason, "the reason must name the schedule, not a generic failure");

    // The accept direction, or the check above would pass equally well if every cell were rejected.
    CellAggregate keeping = AggregateWithReports(PrimaryMetric.Throughput, """
    { "valid": true, "metrics": { "operationsPerSecond": 1000.0, "dispatchLagP99Ms": 0.05 } }
    """);
    True(Aggregator.ToResult(keeping, "test", 100).Valid, "a dispatcher keeping its schedule must stay valid");

    // And a closed-loop run reports no dispatch lag at all: it has no schedule to keep, so the absent
    // field must not be read as a zero that passes, nor as a missing value that fails.
    CellAggregate closedLoop = AggregateWithReports(PrimaryMetric.Throughput, """
    { "valid": true, "metrics": { "operationsPerSecond": 1000.0 } }
    """);
    RunResult closedResult = Aggregator.ToResult(closedLoop, "test", 100);
    True(closedResult.Valid, "a closed-loop run is untouched by the schedule gate");
    True(closedResult.DispatchLagP99Ms is null, "a run that delivered no schedule must publish no dispatch lag");
});

Test("dispatch lag that a collector pause accounts for is measured, not rejected", () =>
{
    // The dispatcher runs in the process it measures, so the collector suspends it too and the
    // schedule slips by the length of the pause. Across P0.5's 54 open-loop rate probes the worst
    // dispatch lag equalled the worst pause to within 0.01 ms - 135.34 against 135.33, 32.22 against
    // 32.22, 19.57 against 19.58 - on two independent clocks. Rejecting those runs would discard
    // exactly the configurations where the collector pauses most, which is choosing the offered load
    // from the result it produces.
    CellAggregate suspended = AggregateWithReports(PrimaryMetric.Throughput, """
    { "valid": true, "metrics": { "operationsPerSecond": 1000.0, "dispatchLagP99Ms": 90.79 },
      "gc": { "pauseMaxMs": 90.25, "gen0Collections": 12 } }
    """);

    RunResult explained = Aggregator.ToResult(suspended, "test", 100);
    True(explained.Valid, "a 90.79 ms lag against a 90.25 ms pause is the collector's, and stays valid");
    Equal(0.54, Math.Round(explained.UnexplainedDispatchLagMs ?? -1, 2), "the unexplained remainder is the lag less the pause");

    // The same magnitude of lag with no collection to explain it is the apparatus failing: this is
    // F15's originating cell, a 22.15 ms p99 of pure queue delay with zero collections in the
    // measured region.
    CellAggregate overloaded = AggregateWithReports(PrimaryMetric.Throughput, """
    { "valid": true, "metrics": { "operationsPerSecond": 1000.0, "dispatchLagP99Ms": 90.79 },
      "gc": { "gen0Collections": 0 } }
    """);

    RunResult unexplained = Aggregator.ToResult(overloaded, "test", 100);
    False(unexplained.Valid, "the same lag with no collection to explain it is dispatcher overload");
    Equal(InvalidReason.ScheduleNotDelivered, unexplained.InvalidReason, "and it is rejected as an undelivered schedule");
    Equal(90.79, Math.Round(unexplained.UnexplainedDispatchLagMs ?? -1, 2), "with the whole lag unexplained");
});

Test("latency percentiles are the mean across invocations for every cell that reports them", () =>
{
    // F21, and F16's shape a fifth time: the aggregation was guarded by `cell.Primary is Latency`,
    // so the nine scenarios that declare Throughput while running open-loop published one
    // invocation's percentiles out of the five measured, beside a record saying `invocations: 5`.
    CellAggregate spread = AggregateWithVaryingReports(PrimaryMetric.Throughput, [
        """{ "valid": true, "metrics": { "operationsPerSecond": 900.0, "latencyP99Ms": 0.30, "latencyP50Ms": 0.10, "serviceTimeP99Ms": 0.02 } }""",
        """{ "valid": true, "metrics": { "operationsPerSecond": 1000.0, "latencyP99Ms": 0.60, "latencyP50Ms": 0.20, "serviceTimeP99Ms": 0.03 } }""",
        """{ "valid": true, "metrics": { "operationsPerSecond": 1100.0, "latencyP99Ms": 1.20, "latencyP50Ms": 0.30, "serviceTimeP99Ms": 0.04 } }""",
    ]);

    RunResult result = Aggregator.ToResult(spread, "test", 100);
    Equal(0.7, Math.Round(result.LatencyP99Ms ?? -1, 6), "p99 is the mean of 0.30, 0.60 and 1.20, not any one of them");
    Equal(0.2, Math.Round(result.LatencyP50Ms ?? -1, 6), "and the whole percentile family is aggregated the same way");
    Equal(0.03, Math.Round(result.ServiceTimeP99Ms ?? -1, 6), "service time included");
    Equal(1000.0, Math.Round(result.OperationsPerSecond, 6), "throughput is the mean of the same three invocations");

    // A latency-primary cell must not have its declared-primary samples meaned into throughput:
    // those samples are latencies.
    CellAggregate latencyPrimary = AggregateWithVaryingReports(PrimaryMetric.Latency, [
        """{ "valid": true, "metrics": { "operationsPerSecond": 500.0, "latencyP99Ms": 2.0 } }""",
        """{ "valid": true, "metrics": { "operationsPerSecond": 700.0, "latencyP99Ms": 4.0 } }""",
        """{ "valid": true, "metrics": { "operationsPerSecond": 900.0, "latencyP99Ms": 6.0 } }""",
    ]);

    RunResult latencyResult = Aggregator.ToResult(latencyPrimary, "test", 100);
    Equal(700.0, Math.Round(latencyResult.OperationsPerSecond, 6), "throughput is read by name, never from the primary samples");
    Equal(4.0, Math.Round(latencyResult.LatencyP99Ms ?? -1, 6), "and the latency family is meaned as before");
});

Test("the dispatcher's wait does not round a short sleep up to the host's timer tick", () =>
{
    // F14's sibling, and the reason aspnet-request-load measured an 11 ms p99 dispatch lag at 1000
    // op/s. Thread.Sleep rounds up to the system timer tick - 15.625 ms by default on Windows - so a
    // wait implemented as Sleep(remaining - 2ms) overshoots a 5 ms deadline by around 12 ms. The
    // measurement is the median of several attempts so that one descheduled attempt cannot fail it.
    var overshoots = new List<double>();
    for (int i = 0; i < 9; i++)
    {
        long target = System.Diagnostics.Stopwatch.GetTimestamp() +
            (long)(0.005 * System.Diagnostics.Stopwatch.Frequency);
        OpenLoopDriver.WaitUntil(target);
        long actual = System.Diagnostics.Stopwatch.GetTimestamp();
        overshoots.Add((actual - target) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
    }

    overshoots.Sort();
    double median = overshoots[overshoots.Count / 2];
    True(median >= 0, $"the wait must not return before its deadline (median overshoot {median:F3} ms)");
    True(median < 2.0,
        $"a 5 ms wait overshot by a median of {median:F3} ms; the pre-fix implementation overshot by " +
        $"roughly a 15.6 ms timer tick, which is what a 1000 op/s arrival schedule measured as latency");
});


Test("a heap factor multiplies the calibrated minimum, so the axis is a configuration not a caption", () =>
{
    // The defect this catches is F2: P0.4 expanded cells at three heap factors while never passing a
    // limit, so three identically configured unpinned runs would have been published under three
    // different heap labels.
    HeapBaselines.Clear();
    HeapBaselines.Load([new HeapBaseline
    {
        Scenario = "low-allocation-compute",
        WorkstationMinimumMb = 100,
        ServerMinimumMb = 120,
        Provisional = false,
        Source = "unit test",
    }]);

    Equal(156L, HeapBaselines.LimitMb("low-allocation-compute", 1.3), "1.3 x the 120 MiB shared minimum, rounded up");
    Equal(240L, HeapBaselines.LimitMb("low-allocation-compute", 2.0), "2.0 x 120");
    Equal(720L, HeapBaselines.LimitMb("low-allocation-compute", 6.0), "6.0 x 120");
    True(HeapBaselines.LimitMb("low-allocation-compute", 1.3) != HeapBaselines.LimitMb("low-allocation-compute", 2.0),
        "two heap factors must not produce the same limit, which is exactly the bug F2 describes");
    HeapBaselines.Clear();
});

Test("the shared minimum is the larger arm, so both arms run at one heap size", () =>
{
    var baseline = new HeapBaseline
    {
        Scenario = "s",
        WorkstationMinimumMb = 64,
        ServerMinimumMb = 192,
        Provisional = false,
        Source = "unit test",
    };

    Equal(192L, baseline.SharedMinimumMb, "comparing collectors at different heap sizes is not a comparison of collectors");
    Equal(64L, baseline.MinimumForArm(CollectorArms.WorkstationId), "the per-arm minima stay available; the difference is itself a result");
    Equal(192L, baseline.MinimumForArm(CollectorArms.ServerId), "server arm reads the server minimum");
});

Test("an uncalibrated scenario is provisional rather than silently zero", () =>
{
    HeapBaselines.Clear();
    HeapBaseline missing = HeapBaselines.For("no-such-scenario");
    True(missing.Provisional, "a scenario with no calibration must never be published as measured");
    True(missing.SharedMinimumMb > 0, "the fallback is a declared value, not a zero that would pin an unusable heap");
});

Test("a heap factor of zero or below is rejected rather than silently pinning nothing", () =>
{
    Throws<ArgumentOutOfRangeException>(() => HeapBaselines.LimitMb("s", 0), "a zero factor would compute a zero limit");
    Throws<ArgumentOutOfRangeException>(() => HeapBaselines.LimitMb("s", -1), "a negative factor is meaningless");
});

Test("a calibration file round-trips through disk", () =>
{
    string path = Path.Combine(Path.GetTempPath(), "lxr-calibration-" + Guid.NewGuid().ToString("N") + ".json");
    try
    {
        var trace = new CalibrationTrace
        {
            RunId = "unit-test",
            Host = "testhost",
            ViabilityRule = "2 consecutive valid invocations",
            WarmupSeconds = 5,
            SteadyStateSeconds = 20,
        };
        trace.Baselines.Add(new HeapBaseline
        {
            Scenario = "cyclic-garbage",
            WorkstationMinimumMb = 88,
            ServerMinimumMb = 152,
            Provisional = false,
            Source = "unit test",
            Note = "bisected",
        });
        trace.Probes.Add(new CalibrationProbe
        {
            Scenario = "cyclic-garbage",
            Arm = CollectorArms.WorkstationId,
            LimitMb = 88,
            Viable = true,
            AttemptsRequired = 2,
            AttemptsSucceeded = 2,
            Detail = "all invocations valid",
            WallSeconds = 41.5,
        });

        CalibrationFile.Write(path, trace);
        List<HeapBaseline> read = CalibrationFile.ReadBaselines(path);
        Equal(1, read.Count, "one baseline was written");
        Equal(88L, read[0].WorkstationMinimumMb, "the workstation minimum survives the round trip");
        Equal(152L, read[0].ServerMinimumMb, "the server minimum survives the round trip");
        False(read[0].Provisional, "a measured baseline must not come back provisional");
    }
    finally
    {
        File.Delete(path);
    }
});

Test("a missing calibration file is an empty list, not an exception", () =>
{
    // The first run on a fresh machine has no calibration. It must fall back visibly rather than
    // failing to start, or the mechanism that measures the baselines cannot itself be run.
    List<HeapBaseline> read = CalibrationFile.ReadBaselines(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N") + ".json"));
    Equal(0, read.Count, "an absent file yields no baselines");
});

Console.WriteLine();
Console.WriteLine("Publisher (P0.5)");
Test("an absent metric is an empty CSV cell, never a zero", () =>
{
    // P0.4's emitter published `?? 0`. In a CSV this is worse than in JSON: a spreadsheet averages an
    // empty cell away and averages a zero in, so a scenario that never paused would publish a
    // collector with perfect zero pauses.
    string runDirectory = Path.Combine(Path.GetTempPath(), "lxr-publish-" + Guid.NewGuid().ToString("N"));
    try
    {
        Directory.CreateDirectory(Path.Combine(runDirectory, "reports"));
        File.WriteAllText(
            Path.Combine(runDirectory, "reports", "low-allocation-compute.wks.testhost.h1.3.0.json"),
            """
            {
              "valid": true,
              "mode": "throughput",
              "seed": 7,
              "warmupSeconds": 5,
              "steadyStateSeconds": 20,
              "heapLimitMb": 156,
              "metrics": { "operationsPerSecond": 1234.5 },
              "gc": { "gen0Collections": 3, "inducedCollections": 0 },
              "process": { "workingSetMb": 210.5 }
            }
            """);
        File.WriteAllText(
            Path.Combine(runDirectory, "results.json"),
            """
            { "schemaVersion": 2, "checkpoints": [ { "id": "x", "date": "2026-01-01", "stepId": "P0.5", "results": [] } ] }
            """);

        string outputDirectory = Path.Combine(runDirectory, "out");
        Publisher.PublishOutcome outcome = Publisher.Publish([runDirectory], outputDirectory, "unit", "2026-01-01", "P0.5", "notes");
        Equal(1, outcome.InvocationRows, "one report produced one row");

        string csv = File.ReadAllText(Path.Combine(outputDirectory, "raw", "unit-invocations.csv"));
        string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Equal(2, lines.Length, "a header and one data row");

        string[] header = lines[0].Split(',');
        string[] row = lines[1].Split(',');
        int pauseIndex = Array.IndexOf(header, "pauseP99Ms");
        True(pauseIndex >= 0, "the CSV declares a pauseP99Ms column");
        Equal(string.Empty, row[pauseIndex].Trim(), "a pause the run never observed must be empty, not 0");

        int opsIndex = Array.IndexOf(header, "operationsPerSecond");
        Equal("1234.5", row[opsIndex].Trim(), "a metric that was measured is published as measured");

        int scenarioIndex = Array.IndexOf(header, "scenario");
        Equal("low-allocation-compute", row[scenarioIndex].Trim(), "the scenario is recovered from the report file name");
    }
    finally
    {
        Directory.Delete(runDirectory, recursive: true);
    }
});

Test("a named run directory that contributed nothing is a warning, not a silent omission", () =>
{
    string missing = Path.Combine(Path.GetTempPath(), "lxr-absent-" + Guid.NewGuid().ToString("N"));
    string outputDirectory = Path.Combine(Path.GetTempPath(), "lxr-out-" + Guid.NewGuid().ToString("N"));
    try
    {
        Publisher.PublishOutcome outcome = Publisher.Publish([missing], outputDirectory, "unit", "2026-01-01", "P0.5", string.Empty);
        Equal(1, outcome.Warnings.Count, "naming a directory is a claim it holds results; failing that claim must be reported");
    }
    finally
    {
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }
});

Test("a cell id splits into scenario, arm, host and heap factor", () =>
{
    // Not by counting components from either end. A control tag prefixes the id, so counting from the
    // left attributes a tagged cell's scenario to its tag; and the heap factor is formatted "0.0#", so
    // "h1.3" occupies two dot-separated components and counting from the right lands on the arm. The
    // heap token is located directly and the three components before it are the scenario, arm and host.
    (string scenario, string collector, string host, string heap) = Publisher.SplitCellId("low-allocation-compute.wks.testhost.h1.3");
    Equal("low-allocation-compute", scenario, "scenario");
    Equal("wks", collector, "arm");
    Equal("testhost", host, "host");
    Equal("1.3", heap, "the heap factor is the whole token, both components of it");

    (string tagged, string taggedArm, string taggedHost, string taggedHeap) = Publisher.SplitCellId("c7.low-allocation-compute.wks.testhost.hdefault");
    Equal("low-allocation-compute", tagged, "a control tag must not be mistaken for the scenario");
    Equal("wks", taggedArm, "a control tag must not shift the arm either");
    Equal("testhost", taggedHost, "nor the host");
    Equal(string.Empty, taggedHeap, "an unpinned cell has no heap factor, and empty is not '0'");

    (string bothScenario, _, _, string bothHeap) = Publisher.SplitCellId("c7.low-allocation-compute.srv.sdk.h6.0");
    Equal("low-allocation-compute", bothScenario, "a tag and a two-component heap factor at once");
    Equal("6.0", bothHeap, "and the factor still survives both");
});

Test("a report file name splits into its cell id and invocation index", () =>
{
    (string cellId, string invocation) = Publisher.SplitReportName("low-allocation-compute.wks.testhost.hdefault.12");
    Equal("low-allocation-compute.wks.testhost.hdefault", cellId, "everything up to the last dot is the cell");
    Equal("12", invocation, "the trailing token is the invocation index");
});

Console.WriteLine();
Console.WriteLine("RunnerOptions (P0.5)");
Test("the new verbs parse, and an unknown one is still rejected", () =>
{
    Equal("calibrate", RunnerOptions.Parse(["calibrate"]).Command, "calibrate is a verb");
    Equal("publish", RunnerOptions.Parse(["publish"]).Command, "publish is a verb");
    Throws<ArgumentException>(() => RunnerOptions.Parse(["calibrat"]), "a near-miss verb must not be silently accepted as a default");
});

Test("per-scenario rates parse and override the global rate", () =>
{
    RunnerOptions options = RunnerOptions.Parse(["matrix", "--rate", "1000", "--scenario-rate", "allocation-churn=250,pointer-chasing=90000"]);
    Equal(250.0, options.ScenarioRates["allocation-churn"], "the per-scenario rate is read");
    Equal(90000.0, options.ScenarioRates["pointer-chasing"], "a second pair on the same argument is read");
    Equal(1000.0, options.ArrivalRatePerSecond, "the global rate remains the fallback for scenarios with no entry");
    Throws<ArgumentException>(() => RunnerOptions.Parse(["matrix", "--scenario-rate", "no-equals-sign"]), "a malformed pair is rejected");
});

Test("the blind-band bound is parameterised so it can be exercised", () =>
{
    Equal(0.10, RunnerOptions.Parse(["controls"]).FineHalfWidthBound, "the default bound is generous against an observed ~1%");
    Equal(0.005, RunnerOptions.Parse(["controls", "--fine-half-width-bound", "0.005"]).FineHalfWidthBound,
        "a bound this tight is how the assertion is watched failing; an assertion nobody has seen fail is indistinguishable from an absent one");
});

Test("calibration knobs parse with defensible defaults", () =>
{
    RunnerOptions defaults = RunnerOptions.Parse(["calibrate"]);
    True(defaults.CalibrationSuccesses >= 2, "one success is not a measurement of a minimum heap");
    True(defaults.CalibrationCeilingMb > defaults.CalibrationFloorMb, "the bracket must be a bracket");
    True(defaults.CalibrationToleranceMb > 0, "a zero tolerance would bisect forever");

    RunnerOptions custom = RunnerOptions.Parse(["calibrate", "--calibration-floor-mb", "32", "--calibration-ceiling-mb", "512", "--calibration-tolerance-mb", "4", "--calibration-successes", "3"]);
    Equal(32L, custom.CalibrationFloorMb, "floor");
    Equal(512L, custom.CalibrationCeilingMb, "ceiling");
    Equal(4L, custom.CalibrationToleranceMb, "tolerance");
    Equal(3, custom.CalibrationSuccesses, "successes");
});

Console.WriteLine();
Console.WriteLine("Aggregator machine and config (P0.5)");
Test("the published requestedConfig carries the per-cell heap limit, not just the arm's knobs", () =>
{
    // Regression: requestedConfig was populated from cell.Arm.RuntimeProperties alone, so a run pinned
    // to 128 MiB published a requestedConfig naming only Server and Concurrent. The one knob this step
    // varies was the one knob missing from the record of what was asked for.
    string report = """
    {
      "requestedConfig": { "System.GC.Server": "false", "System.GC.HeapHardLimit": "134217728" },
      "machine": { "processorName": "test", "logicalCores": 16, "totalMemoryBytes": 134217728,
                   "osDescription": "test-os", "processCount": 100, "timerResolutionNs": 100 }
    }
    """;
    using JsonDocument document = JsonDocument.Parse(report);
    var result = new RunResult { Scenario = "s", Collector = "wks", Host = "testhost" };
    Aggregator.MergeRequestedConfig(result, document.RootElement);

    Equal("134217728", result.RequestedConfig["System.GC.HeapHardLimit"], "the heap limit reaches the published record");
    Equal("false", result.RequestedConfig["System.GC.Server"], "and the arm's knobs are still there");
});

Test("machine memory is the host's, not the heap limit the worker was pinned to", () =>
{
    // Regression: MachineInfo.Capture reads GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, which under
    // a heap hard limit reports the limit. Publishing it unaltered described this 64 GiB host as a
    // 128 MiB one, and described it differently at every heap factor.
    string report = """
    { "machine": { "processorName": "test", "logicalCores": 16, "totalMemoryBytes": 134217728,
                   "osDescription": "test-os", "processCount": 100, "timerResolutionNs": 100 } }
    """;
    using JsonDocument document = JsonDocument.Parse(report);

    MachineInfo? pinned = Aggregator.MachineFromReport(document.RootElement, null, null, null, 68719476736L);
    True(pinned is not null, "the machine block is populated at all; P0.4 published machine: null throughout");
    Equal(68719476736L, pinned!.TotalMemoryBytes, "the runner's unpinned reading wins over the worker's pinned one");

    MachineInfo? unpinned = Aggregator.MachineFromReport(document.RootElement, "High performance", 8, "Virtual Machine", null);
    Equal(134217728L, unpinned!.TotalMemoryBytes, "with no runner reading the worker's value is still reported rather than dropped");
    Equal("High performance", unpinned.PowerPlan, "operator-supplied facts are recorded");
    True(unpinned.Virtualized == true, "'Virtual Machine' is recognised as virtualised");
});

Test("an undetermined virtualization state stays null rather than becoming a claim of bare metal", () =>
{
    // 'false' here would assert the host is physical. Every run made without --machine-model would
    // then publish that claim, and on this project's own hardware the claim is false: the machine
    // is a VM. Absent is not the same as negative, for the same reason absent is not zero.
    using var document = JsonDocument.Parse("""{"machine":{"totalMemoryBytes":1024}}""");
    MachineInfo? unknown = Aggregator.MachineFromReport(document.RootElement, null, null, null, null);
    True(unknown!.Virtualized is null, "with no system model the virtualization state is null, not false");

    MachineInfo? physical = Aggregator.MachineFromReport(document.RootElement, null, null, "Precision 7960", null);
    True(physical!.Virtualized == false, "a model that names no hypervisor is a genuine false");

    // The null has to survive republication too: a tri-state read through a two-state helper is
    // where it would quietly become false again.
    var result = new RunResult { Scenario = "s", Collector = "workstation", Machine = unknown };
    string path = Path.Combine(Path.GetTempPath(), $"lxr-virt-{Guid.NewGuid():N}.json");
    try
    {
        ResultWriter.Write(path, new ResultDocument
        {
            Id = "c",
            Date = "2026-08-11",
            StepId = "P0.5",
            Notes = "virtualization null round-trip",
            Results = { result },
        });
        string json = File.ReadAllText(path);
        True(json.Contains("\"virtualized\": null") || json.Contains("\"virtualized\":null"),
            "an unknown virtualization state is written as JSON null, not omitted and not false");
    }
    finally
    {
        File.Delete(path);
    }
});

return Summarize();

static CellAggregate AggregateWithReports(PrimaryMetric primary, string reportJson)
{
    // The schedule gate reads per-invocation reports rather than PrimarySamples, so the outcomes have
    // to carry real JSON. Three invocations because that is the bootstrap floor, and identical ones
    // because the gate is a bound on the mean, not on the spread.
    var outcomes = new List<InvocationOutcome>();
    for (int i = 0; i < 3; i++)
    {
        outcomes.Add(new InvocationOutcome
        {
            Status = RunStatus.Ok,
            MarkerSeen = true,
            Report = JsonDocument.Parse(reportJson).RootElement,
        });
    }

    var aggregate = new CellAggregate
    {
        Cell = new MatrixCell
        {
            Scenario = "low-allocation-compute",
            Arm = CollectorArms.Workstation,
            Host = HostDescriptor.Sdk("dotnet.exe", aspNetAvailable: false),
            Primary = primary,
            Invocations = outcomes.Count,
            TimeoutSeconds = 60,
        },
        Outcomes = outcomes,
        PrimarySamples = [1000.0, 1000.0, 1000.0],
    };
    aggregate.ValidOutcomes.AddRange(outcomes);
    return aggregate;
}

static CellAggregate AggregateWithVaryingReports(PrimaryMetric primary, string[] reportJson)
{
    // Distinct reports, because the point is which invocation the published figure comes from. If
    // every invocation carried the same values, a copy of one and the mean of all would be equal and
    // the test would pass against both the defect and the fix.
    var outcomes = new List<InvocationOutcome>();
    foreach (string json in reportJson)
    {
        outcomes.Add(new InvocationOutcome
        {
            Status = RunStatus.Ok,
            MarkerSeen = true,
            Report = JsonDocument.Parse(json).RootElement,
        });
    }

    var aggregate = new CellAggregate
    {
        Cell = new MatrixCell
        {
            Scenario = "low-allocation-compute",
            Arm = CollectorArms.Workstation,
            Host = HostDescriptor.Sdk("dotnet.exe", aspNetAvailable: false),
            Primary = primary,
            Invocations = outcomes.Count,
            TimeoutSeconds = 60,
        },
        Outcomes = outcomes,
        // Deliberately not the throughput values: a latency-primary cell's primary samples are
        // latencies, and the aggregator must not mean them into OperationsPerSecond.
        PrimarySamples = [2.0, 4.0, 6.0],
    };
    aggregate.ValidOutcomes.AddRange(outcomes);
    return aggregate;
}

static CellAggregate AggregateWithSamples(CollectorArm arm, int invocations, double magnitude)
{
    // The bootstrap floor is about sample count, so the outcomes only need to exist and be valid;
    // PrimarySamples is what Ratio actually resamples. Real spread is included so that the accept
    // direction produces a genuinely non-degenerate interval rather than passing by accident.
    var outcomes = new List<InvocationOutcome>();
    var samples = new double[invocations];
    for (int i = 0; i < invocations; i++)
    {
        outcomes.Add(new InvocationOutcome { Status = RunStatus.Ok, MarkerSeen = true });
        samples[i] = magnitude + i;
    }

    var aggregate = new CellAggregate
    {
        Cell = Cell(arm, invocations, null),
        Outcomes = outcomes,
        PrimarySamples = samples,
    };
    aggregate.ValidOutcomes.AddRange(outcomes);
    return aggregate;
}

static MatrixCell Cell(CollectorArm arm, int invocations, string? tag) => new()
{
    Scenario = "low-allocation-compute",
    Arm = arm,
    Host = HostDescriptor.Sdk("dotnet.exe", aspNetAvailable: false),
    Primary = PrimaryMetric.Throughput,
    Invocations = invocations,
    TimeoutSeconds = 60,
    Tag = tag,
};

static List<MatrixCell> TwoArmCells(int invocations) =>
[
    Cell(CollectorArms.Workstation, invocations, tag: null),
    Cell(CollectorArms.Server(8), invocations, tag: null),
];

static RunResult Sample(string scenario = "long-lived-cache") => new()
{
    Scenario = scenario,
    Collector = CollectorArms.WorkstationId,
    Host = "sdk",
    Status = RunStatus.Ok,
    Valid = true,
    CollectorConfirmed = true,
    OperationsPerSecond = 12345.6,
    LatencyMethod = OpenLoopDriver.LatencyMethod,
    ArrivalRatePerSecond = 1000,
    AchievedRatePerSecond = 999.7,
    LatencyP50Ms = 0.4,
    LatencyP99Ms = 1.2,
    LatencyP999Ms = 3.4,
    LatencyP9999Ms = 8.9,
    LatencyMaxMs = 12.0,
    Invocations = 3,
    Seed = 20040,
    WarmupSeconds = 5,
    SteadyStateSeconds = 20,
    RuntimeDescription = ".NET 11.0.0",
    RuntimeBuildId = "11.0.0+abcdef1234567890abcdef1234567890abcdef12",
    CoreClrSha256 = new string('a', 64),
    InducedCollections = 0,
};

static ResultDocument Document(RunResult result) => new()
{
    Id = "unit-test",
    Date = "2024-01-01",
    StepId = "P0.4",
    Results = [result],
};

/// <summary>
/// A scenario that allocates nothing, so a managed-allocation budget measured around the driver
/// reflects the driver's own bookkeeping rather than the workload's.
/// </summary>
internal sealed class NonAllocatingScenario : IScenario
{
    private long _counter;

    public ScenarioDescriptor Describe() => new()
    {
        Id = "non-allocating-test-scenario",
        Rationale = "Allocates nothing, so a managed-allocation budget around the driver measures the driver.",
        Primary = PrimaryMetric.Throughput,
    };

    public void Setup(ScenarioContext context)
    {
    }

    public long RunOperation(int workerIndex) => ++_counter;

    public ScenarioVerification Verify() => new() { Success = true, Marker = "counter-advanced" };

    public void Teardown()
    {
    }
}
