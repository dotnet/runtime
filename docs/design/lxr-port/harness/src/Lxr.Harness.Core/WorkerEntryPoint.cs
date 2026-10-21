// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;

namespace Lxr.Harness.Core;

/// <summary>
/// Runs one scenario, in one collector configuration, in one process, and reports what actually
/// happened rather than what was asked for.
///
/// <para>The order of operations matters and is enforced here: measurement ends <em>before</em>
/// verification begins. Several scenarios can only check their invariants by forcing a collection,
/// and an induced collection inside the measured region is precisely the confound P0.3 traced the
/// hsqldb p99 discrepancy to. The P1.5 validation probe is the only exception: it declares an exact
/// count up front and the worker rejects any different observed count.</para>
/// </summary>
public static class WorkerEntryPoint
{
    /// <summary>Printed on stdout only when the run genuinely completed its work.</summary>
    public const string MarkerPrefix = "LXR-HARNESS-COMPLETE ";

    private static readonly Dictionary<string, string> HostPropertyToGcConfigKey = new(StringComparer.Ordinal)
    {
        ["System.GC.Server"] = GcIdentity.ServerGcKey,
        ["System.GC.Concurrent"] = GcIdentity.ConcurrentGcKey,
        ["System.GC.HeapCount"] = GcIdentity.HeapCountKey,
        ["System.GC.HeapHardLimit"] = GcIdentity.HeapHardLimitKey,
        ["System.GC.DynamicAdaptationMode"] = GcIdentity.DynamicAdaptationModeKey,
        ["System.GC.Name"] = GcIdentity.GcNameKey,
        ["System.GC.Path"] = GcIdentity.GcPathKey,
    };

    public static int Run(string[] args, Func<string, IScenario?> scenarioFactory)
    {
        WorkerOptions options;
        try
        {
            options = WorkerOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"lxr-harness-worker: {ex.Message}");
            return 2;
        }

        // Control 4: hang past the timeout. Deliberately before any work, so the runner's timeout is
        // what ends the process rather than anything the worker chooses to do.
        if (options.Hang)
        {
            Console.Error.WriteLine("lxr-harness-worker: --hang requested; sleeping indefinitely");
            Thread.Sleep(Timeout.Infinite);
        }

        // Control 6: exit zero having done nothing at all. The marker is what proves work happened, so
        // this path must be counted as a failure despite the exit code.
        if (options.FakeSuccess)
        {
            Console.Out.WriteLine("lxr-harness-worker: pretending to succeed without doing any work");
            return 0;
        }

        IScenario? scenario = scenarioFactory(options.Scenario);
        if (scenario is null)
        {
            Console.Error.WriteLine($"lxr-harness-worker: this worker cannot construct scenario '{options.Scenario}'.");
            return 3;
        }

        try
        {
            return Execute(options, scenario);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"lxr-harness-worker: unhandled {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 4;
        }
    }

    private static int Execute(WorkerOptions options, IScenario scenario)
    {
        ScenarioDescriptor descriptor = scenario.Describe();

        // Identity first: if the collector is not what was asked for, nothing measured afterwards is
        // worth anything, and the result must say so rather than carrying a plausible-looking number.
        Dictionary<string, string> gcConfig = GcIdentity.ReadConfiguration();
        var identityFailures = new List<string>();
        CollectorArm? arm = null;
        try
        {
            arm = CollectorArms.Resolve(options.Arm, options.ServerHeapCount);
        }
        catch (ArgumentException ex)
        {
            identityFailures.Add(ex.Message);
        }

        if (arm is not null)
        {
            foreach (IdentityAssertion assertion in arm.Identity)
            {
                if (assertion.AppliesWhenKey is string gateKey &&
                    (!gcConfig.TryGetValue(gateKey, out string? gateValue) ||
                     !string.Equals(KnobObservation.Normalize(gateValue), KnobObservation.Normalize(assertion.AppliesWhenValue!), StringComparison.OrdinalIgnoreCase)))
                {
                    // The assertion's precondition does not hold, so its readback would prove nothing.
                    // Recording it as "not applicable" is the honest outcome; claiming it passed would
                    // be an unfired control.
                    continue;
                }

                if (!gcConfig.TryGetValue(assertion.Key, out string? observed))
                {
                    identityFailures.Add($"{assertion.Key}: not reported by the GC at all (expected '{assertion.ExpectedValue}'). {assertion.Justification}");
                }
                else if (!string.Equals(KnobObservation.Normalize(observed), KnobObservation.Normalize(assertion.ExpectedValue), StringComparison.OrdinalIgnoreCase))
                {
                    identityFailures.Add($"{assertion.Key}: expected '{assertion.ExpectedValue}', observed '{observed}'. {assertion.Justification}");
                }
            }
        }

        // A second, independent source for the same fact. If the GC's own report and GCSettings
        // disagree, one of them is wrong and the run must not be trusted either way.
        if (!GcIdentity.AgreesWithGcSettings(gcConfig))
        {
            identityFailures.Add(
                $"GC reports ServerGC='{gcConfig.GetValueOrDefault(GcIdentity.ServerGcKey)}' but GCSettings.IsServerGC is {GCSettings()}; the two disagree");
        }

        bool collectorConfirmed = identityFailures.Count == 0;

        List<KnobObservation> knobs = ObserveKnobs(options, gcConfig);
        var configPinFailures = new List<string>();
        var unverifiedKnobs = new List<string>();
        foreach (KnobObservation knob in knobs)
        {
            if (knob.Contradicted)
            {
                configPinFailures.Add($"{knob.Name}: requested '{knob.Requested}', observed '{knob.Observed}'");
            }

            if (knob.Evidence is ConfigEvidence.RequestedOnly)
            {
                unverifiedKnobs.Add(knob.Name);
            }
        }

        RuntimeBuildIdentity runtime = RuntimeBuildIdentity.Capture();
        MachineInfo machine = MachineInfo.Capture(null, null, null, null);

        // Control 5: crash after identity capture so the dump has a realistic process state.
        if (options.Crash)
        {
            Console.Error.WriteLine("lxr-harness-worker: --crash requested; failing fast");
            Console.Error.Flush();
            Environment.FailFast("lxr-harness deliberate crash for control 5");
        }

        var context = new ScenarioContext
        {
            Seed = options.Seed,
            WorkerCount = Math.Clamp(options.WorkerCount, 1, Math.Max(1, descriptor.MaxWorkerCount)),
            Parameters = new ScenarioParameters(options.Parameters),
            ControlTag = options.ControlTag,
        };

        scenario.Setup(context);

        using ReferenceEnumerationProbe? referenceEnumerationProbe =
            ReferenceEnumerationProbe.TryCreateFromEnvironment();
        if (referenceEnumerationProbe is not null)
        {
            // The scenario graph must be mature and no setup collection may leak into the measured
            // scan counters. Both collections are outside GcTelemetry's measurement window.
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
            referenceEnumerationProbe.Reset();
        }

        using var telemetry = new GcTelemetry();
        telemetry.BeginMeasurement();

        // Control 6, second form: do a fraction of the work and still exit zero. The success marker
        // and the minimum operation count are what must reject this, not the exit code.
        double durationScale = Math.Clamp(options.PartialWorkFraction, 0.0, 1.0);
        double steadySeconds = options.SteadyStateSeconds * durationScale;
        int expectedInducedCollections =
            referenceEnumerationProbe?.FixedFullCollectionCount ?? 0;
        using FixedFullCollectionSchedule? fixedFullCollections =
            expectedInducedCollections > 0
                ? new FixedFullCollectionSchedule(
                    expectedInducedCollections,
                    options.WarmupSeconds,
                    steadySeconds)
                : null;
        using PauseScheduleProbe? pauseScheduleProbe =
            PauseScheduleProbe.TryCreateFromEnvironment(
                options.Parameters,
                context.WorkerCount,
                options.WarmupSeconds,
                steadySeconds);

        long runStart = Stopwatch.GetTimestamp();
        MeasuredRun? openLoop = null;
        ThroughputRun? throughput = null;
        fixedFullCollections?.Start();
        pauseScheduleProbe?.Start();

        if (options.Mode.Equals("latency", StringComparison.OrdinalIgnoreCase))
        {
            openLoop = OpenLoopDriver.Run(scenario, new OpenLoopOptions
            {
                ArrivalRatePerSecond = options.ArrivalRatePerSecond,
                WarmupSeconds = options.WarmupSeconds,
                SteadyStateSeconds = steadySeconds,
                Distribution = options.Distribution,
                Seed = options.Seed,
                WorkerCount = context.WorkerCount,
                Stall = options.InjectStallMs > 0
                    ? new StallInjection { PeriodSeconds = options.InjectStallEverySeconds, DurationMilliseconds = options.InjectStallMs }
                    : null,
            });
        }
        else
        {
            throughput = ThroughputDriver.Run(scenario, new ThroughputOptions
            {
                WarmupSeconds = options.WarmupSeconds,
                SteadyStateSeconds = steadySeconds,
                WorkerCount = context.WorkerCount,
                InjectExtraWorkEveryNth = options.InjectExtraWorkEveryNth,
            });
        }

        fixedFullCollections?.WaitForCompletion();
        PauseScheduleSnapshot? pauseSchedule =
            pauseScheduleProbe?.WaitForCompletion();
        IReadOnlyList<string> pauseScheduleFailures =
            pauseSchedule?.Failures ?? Array.Empty<string>();

        // Measurement is over. Scenario verification may now force collections without affecting it.
        GcSummary gc = telemetry.EndMeasurement();
        ReferenceEnumerationSnapshot? referenceEnumeration =
            referenceEnumerationProbe?.CaptureAndStop();
        IReadOnlyList<string> referenceEnumerationFailures =
            referenceEnumeration?.Validate() ?? Array.Empty<string>();
        GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
        long workingSet = Environment.WorkingSet;

        ScenarioVerification verification = scenario.Verify();
        scenario.Teardown();

        if (options.SamplesPath is string samplesPath)
        {
            if (openLoop is not null)
            {
                SampleStore.WriteOpenLoop(samplesPath, openLoop, runStart);
            }
            else if (throughput is not null)
            {
                SampleStore.WriteQuantumRates(samplesPath, throughput.QuantumRates);
            }
        }

        long operationsCompleted = openLoop?.RecordCount ?? throughput?.SteadyOperations ?? 0;
        bool enoughWork = operationsCompleted >= descriptor.MinimumOperations;
        bool inducedOk = referenceEnumerationProbe is not null
            ? gc.InducedCollections == expectedInducedCollections
            : descriptor.AllowsInducedCollections ||
              gc.InducedCollections == 0;

        bool valid = collectorConfirmed &&
            configPinFailures.Count == 0 &&
            verification.Success &&
            verification.Violations.Count == 0 &&
            referenceEnumerationFailures.Count == 0 &&
            pauseScheduleFailures.Count == 0 &&
            enoughWork &&
            inducedOk;

        string? invalidReason = !collectorConfirmed ? InvalidReason.CollectorIdentityMismatch
            : configPinFailures.Count > 0 ? InvalidReason.ConfigPinNotHonoured
            : verification.Violations.Count > 0 ? InvalidReason.SemanticViolation
            : referenceEnumerationFailures.Count > 0
                ? InvalidReason.ReferenceEnumerationProbeFailed
            : pauseScheduleFailures.Count > 0
                ? InvalidReason.PauseScheduleProbeFailed
            : !enoughWork ? InvalidReason.InsufficientOps
            : !inducedOk ? InvalidReason.UnexpectedInducedCollections
            : !verification.Success ? InvalidReason.MarkerMissing
            : null;

        if (options.OutputPath is string outputPath)
        {
            WriteReport(outputPath, options, descriptor, verification, gc, openLoop, throughput, knobs, gcConfig,
                identityFailures, configPinFailures, unverifiedKnobs, runtime, machine, memoryInfo, workingSet,
                collectorConfirmed, valid, invalidReason, operationsCompleted, steadySeconds,
                expectedInducedCollections, referenceEnumeration, referenceEnumerationFailures,
                pauseSchedule, pauseScheduleFailures);
        }

        // The marker is the deterministic success signal. It is printed only when the scenario itself
        // declares the work was done, so an exit code of zero can never stand in for it.
        if (verification.Success && verification.Violations.Count == 0 && enoughWork)
        {
            Console.Out.WriteLine(MarkerPrefix + verification.Marker);
        }
        else
        {
            Console.Error.WriteLine($"lxr-harness-worker: no success marker; success={verification.Success}, violations={verification.Violations.Count}, operations={operationsCompleted}");
            foreach (string violation in verification.Violations)
            {
                Console.Error.WriteLine($"  violation: {violation}");
            }
        }

        foreach (string failure in identityFailures)
        {
            Console.Error.WriteLine($"  collector-identity: {failure}");
        }

        foreach (string failure in configPinFailures)
        {
            Console.Error.WriteLine($"  config-pin: {failure}");
        }

        return valid ? 0 : 1;
    }

    private sealed class FixedFullCollectionSchedule : IDisposable
    {
        private readonly int _collectionCount;
        private readonly double _warmupSeconds;
        private readonly double _steadySeconds;
        private readonly ManualResetEventSlim _cancel = new(initialState: false);
        private readonly Thread _thread;
        private ExceptionDispatchInfo? _failure;
        private long _startTimestamp;
        private int _collectionsCompleted;
        private bool _started;

        public FixedFullCollectionSchedule(
            int collectionCount,
            double warmupSeconds,
            double steadySeconds)
        {
            _collectionCount = collectionCount;
            _warmupSeconds = warmupSeconds;
            _steadySeconds = steadySeconds;
            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "P1.5 fixed full collections",
            };
        }

        public void Start()
        {
            ObjectDisposedException.ThrowIf(_cancel.IsSet, this);
            if (_started)
            {
                throw new InvalidOperationException(
                    "The fixed full-collection schedule was already started.");
            }

            _started = true;
            _startTimestamp = Stopwatch.GetTimestamp();
            _thread.Start();
        }

        public void WaitForCompletion()
        {
            if (!_started)
            {
                throw new InvalidOperationException(
                    "The fixed full-collection schedule was not started.");
            }

            _thread.Join();
            _failure?.Throw();
            if (_collectionsCompleted != _collectionCount)
            {
                throw new InvalidOperationException(
                    $"The fixed full-collection schedule completed {_collectionsCompleted} of {_collectionCount} collections.");
            }
        }

        public void Dispose()
        {
            _cancel.Set();
            if (_started && _thread.IsAlive)
            {
                _thread.Join();
            }
            _cancel.Dispose();
        }

        private void Run()
        {
            try
            {
                for (int collection = 0;
                     collection < _collectionCount;
                     collection++)
                {
                    double targetSeconds =
                        _warmupSeconds +
                        (_steadySeconds * (collection + 1) /
                         (_collectionCount + 1));
                    while (true)
                    {
                        double remainingSeconds =
                            targetSeconds -
                            Stopwatch.GetElapsedTime(
                                _startTimestamp).TotalSeconds;
                        if (remainingSeconds <= 0)
                        {
                            break;
                        }

                        int waitMilliseconds = Math.Max(
                            1,
                            Math.Min(
                                50,
                                (int)Math.Ceiling(
                                    remainingSeconds * 1000)));
                        if (_cancel.Wait(waitMilliseconds))
                        {
                            return;
                        }
                    }

                    if (_cancel.IsSet)
                    {
                        return;
                    }

                    GC.Collect(
                        2,
                        GCCollectionMode.Forced,
                        blocking: true,
                        compacting: false);
                    _collectionsCompleted++;
                }
            }
            catch (Exception ex)
            {
                _failure = ExceptionDispatchInfo.Capture(ex);
            }
        }
    }

    private static bool GCSettings() => System.Runtime.GCSettings.IsServerGC;

    private static List<KnobObservation> ObserveKnobs(WorkerOptions options, Dictionary<string, string> gcConfig)
    {
        var knobs = new List<KnobObservation>();

        foreach (KeyValuePair<string, string> requested in options.RequestedConfig)
        {
            if (!HostPropertyToGcConfigKey.TryGetValue(requested.Key, out string? gcKey))
            {
                continue;
            }

            gcConfig.TryGetValue(gcKey, out string? observed);

            bool datasOnWorkstation = gcKey == GcIdentity.DynamicAdaptationModeKey && !GcIdentity.DatasReadbackIsMeaningful(gcConfig);

            knobs.Add(new KnobObservation
            {
                Name = requested.Key,
                Requested = requested.Value,
                Observed = observed,
                Evidence = observed is null || datasOnWorkstation ? ConfigEvidence.RequestedOnly : ConfigEvidence.RuntimeReported,
                Note = datasOnWorkstation
                    ? "GCDynamicAdaptationMode is only written back under DYNAMIC_HEAP_COUNT, which gcpriv.h:158-162 defines only for USE_REGIONS with MULTIPLE_HEAPS. Outside Server GC the reported value is simply the requested one and confirms nothing."
                    : null,
            });
        }

        // Deliberately not echoing the whole effective GC configuration into `knobs`: it is already
        // recorded verbatim in `observedGcConfig`, and duplicating thirty-odd unpinned values here
        // would bury the handful of knobs this run actually claims to control.
        knobs.AddRange(TieringProbe.Observe(options.RequestedConfig));
        return knobs;
    }

    private static void WriteReport(
        string path,
        WorkerOptions options,
        ScenarioDescriptor descriptor,
        ScenarioVerification verification,
        GcSummary gc,
        MeasuredRun? openLoop,
        ThroughputRun? throughput,
        List<KnobObservation> knobs,
        Dictionary<string, string> gcConfig,
        List<string> identityFailures,
        List<string> configPinFailures,
        List<string> unverifiedKnobs,
        RuntimeBuildIdentity runtime,
        MachineInfo machine,
        GCMemoryInfo memoryInfo,
        long workingSet,
        bool collectorConfirmed,
        bool valid,
        string? invalidReason,
        long operationsCompleted,
        double steadySeconds,
        int expectedInducedCollections,
        ReferenceEnumerationSnapshot? referenceEnumeration,
        IReadOnlyList<string> referenceEnumerationFailures,
        PauseScheduleSnapshot? pauseSchedule,
        IReadOnlyList<string> pauseScheduleFailures)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using FileStream stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteNumber("workerReportVersion", 1);
        writer.WriteString("scenario", options.Scenario);
        writer.WriteString("arm", options.Arm);
        writer.WriteString("mode", options.Mode);
        writer.WriteString("controlTag", options.ControlTag);
        writer.WriteNumber("seed", options.Seed);
        writer.WriteNumber("workerCount", options.WorkerCount);
        writer.WriteNumber("warmupSeconds", options.WarmupSeconds);

        // The duration the run actually used, not the one it was asked for. Under --partial-work these
        // differ, and this is the field whose only job is to let someone reproduce the number. Writing
        // the requested value here would also have falsified control 6's own published claim that a
        // truncated run is visible in its recorded steady-state duration.
        writer.WriteNumber("steadyStateSeconds", steadySeconds);
        writer.WriteNumber("steadyStateSecondsRequested", options.SteadyStateSeconds);
        writer.WriteNumber("processId", Environment.ProcessId);

        writer.WriteBoolean("collectorConfirmed", collectorConfirmed);
        writer.WriteBoolean("valid", valid);
        WriteNullableString(writer, "invalidReason", invalidReason);

        writer.WriteBoolean("verificationSuccess", verification.Success);
        writer.WriteString("marker", verification.Marker);
        WriteNullableString(writer, "verificationDetail", verification.Detail);
        WriteStringArray(writer, "violations", verification.Violations);
        WriteStringArray(writer, "identityFailures", identityFailures);
        WriteStringArray(writer, "configPinFailures", configPinFailures);
        WriteStringArray(writer, "unverifiedKnobs", unverifiedKnobs);
        WriteStringArray(
            writer,
            "referenceEnumerationFailures",
            referenceEnumerationFailures);
        WriteStringArray(
            writer,
            "pauseScheduleFailures",
            pauseScheduleFailures);

        writer.WriteStartObject("observedGcConfig");
        foreach (KeyValuePair<string, string> entry in gcConfig)
        {
            writer.WriteString(entry.Key, entry.Value);
        }

        writer.WriteEndObject();

        if (referenceEnumeration is null)
        {
            writer.WriteNull("referenceEnumeration");
        }
        else
        {
            writer.WriteStartObject("referenceEnumeration");
            writer.WriteString(
                "hookLibraryPath",
                referenceEnumeration.HookLibraryPath);
            writer.WriteString(
                "hookLibrarySha256",
                referenceEnumeration.HookLibrarySha256);
            writer.WriteNumber(
                "expectedMode",
                referenceEnumeration.ExpectedMode);
            writer.WriteString(
                "expectedModeName",
                referenceEnumeration.ExpectedModeName);
            writer.WriteNumber("mode", referenceEnumeration.Mode);
            writer.WriteNumber("errors", referenceEnumeration.Errors);
            writer.WriteNumber(
                "objectScans",
                referenceEnumeration.ObjectScans);
            writer.WriteNumber("ranges", referenceEnumeration.Ranges);
            writer.WriteNumber("slots", referenceEnumeration.Slots);
            writer.WriteNumber(
                "nonNullSlots",
                referenceEnumeration.NonNullSlots);
            writer.WriteNumber("checksum", referenceEnumeration.Checksum);
            writer.WriteString(
                "window",
                "post-reset-through-telemetry-end");
            writer.WriteEndObject();
        }

        if (pauseSchedule is null)
        {
            writer.WriteNull("pauseSchedule");
        }
        else
        {
            pauseSchedule.Write(writer);
        }

        writer.WriteStartObject("requestedConfig");
        foreach (KeyValuePair<string, string> entry in options.RequestedConfig)
        {
            writer.WriteString(entry.Key, entry.Value);
        }

        writer.WriteEndObject();

        writer.WriteStartArray("knobs");
        foreach (KnobObservation knob in knobs)
        {
            writer.WriteStartObject();
            writer.WriteString("name", knob.Name);
            WriteNullableString(writer, "requested", knob.Requested);
            WriteNullableString(writer, "observed", knob.Observed);
            writer.WriteString("evidence", knob.Evidence.ToString());
            writer.WriteBoolean("contradicted", knob.Contradicted);
            WriteNullableString(writer, "note", knob.Note);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartObject("metrics");
        writer.WriteNumber("operationsCompleted", operationsCompleted);
        if (throughput is not null)
        {
            writer.WriteNumber("operationsPerSecond", throughput.OperationsPerSecond);
            writer.WriteNumber("steadyOperations", throughput.SteadyOperations);
            writer.WriteNumber("steadySeconds", throughput.SteadySeconds);
            writer.WriteNumber("quanta", throughput.Quanta);
            writer.WriteNumber("checksum", throughput.Checksum);
        }

        if (openLoop is not null)
        {
            WriteLatency(writer, openLoop);
        }

        writer.WriteEndObject();

        writer.WriteStartObject("gc");
        writer.WriteNumber("gen0Collections", gc.Gen0Collections);
        writer.WriteNumber("gen1Collections", gc.Gen1Collections);
        writer.WriteNumber("gen2Collections", gc.Gen2Collections);
        writer.WriteNumber("inducedCollections", gc.InducedCollections);
        writer.WriteNumber(
            "expectedInducedCollections",
            expectedInducedCollections);
        writer.WriteBoolean(
            "inducedCollectionsAllowed",
            descriptor.AllowsInducedCollections ||
            expectedInducedCollections > 0);
        writer.WriteNumber("totalPauseMs", gc.TotalPauseMs);
        writer.WriteNumber("observedPauseCount", gc.ObservedPauseCount);
        writer.WriteNumber("pauseShortfall", gc.PauseShortfall);
        writer.WriteString("pauseSource", gc.PauseSource);

        double[] pauses = [.. gc.PauseSamplesMs];
        Array.Sort(pauses);

        // A run that observed no pause has no pause distribution. Emitting 0 would be indistinguishable
        // from a collector that paused for zero milliseconds, which is the flattering reading, and the
        // board would chart it as a real measurement. The v1 result schema says to use null for an
        // unavailable metric, so that is what an absent distribution reports.
        WriteNullableNumber(writer, "pauseAverageMs", gc.MeanPauseMs);
        WriteNullableNumber(writer, "pauseP99Ms", pauses.Length > 0 ? Stats.Percentile(pauses, 99) : null);
        WriteNullableNumber(writer, "pauseMaxMs", pauses.Length > 0 ? pauses[^1] : null);
        writer.WriteEndObject();

        writer.WriteStartObject("process");
        writer.WriteNumber("workingSetMb", workingSet / (1024.0 * 1024.0));

        // GCMemoryInfo describes the last completed collection, so in a run where none happened every
        // field of it is zero. Index is the documented signal for that: it is the sequence number of
        // the collection being described, and stays 0 until one completes. Reporting 0 MB committed
        // for such a run would be a fabricated measurement, not a small one.
        bool memoryInfoIsPopulated = memoryInfo.Index > 0;
        WriteNullableNumber(writer, "committedMb", memoryInfoIsPopulated ? memoryInfo.TotalCommittedBytes / (1024.0 * 1024.0) : null);
        WriteNullableNumber(writer, "heapSizeMb", memoryInfoIsPopulated ? memoryInfo.HeapSizeBytes / (1024.0 * 1024.0) : null);
        WriteNullableNumber(writer, "totalAvailableMemoryMb", memoryInfoIsPopulated ? memoryInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0) : null);

        // Allocated bytes and the live-heap estimate do not depend on a collection having happened, and
        // without them a scenario that never collects would appear to have used no memory at all.
        writer.WriteNumber("totalAllocatedMb", GC.GetTotalAllocatedBytes(precise: false) / (1024.0 * 1024.0));
        writer.WriteNumber("totalMemoryMb", GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0));
        writer.WriteEndObject();

        writer.WriteStartObject("runtime");
        writer.WriteString("frameworkDescription", runtime.FrameworkDescription);
        writer.WriteString("environmentVersion", runtime.EnvironmentVersion);
        writer.WriteString("runtimeIdentifier", runtime.RuntimeIdentifier);
        WriteNullableString(writer, "coreLibInformationalVersion", runtime.CoreLibInformationalVersion);
        WriteNullableString(writer, "commitSha", runtime.CommitSha);
        WriteNullableString(writer, "coreClrPath", runtime.CoreClrPath);
        WriteNullableString(writer, "coreClrFileVersion", runtime.CoreClrFileVersion);
        WriteNullableString(writer, "coreClrSha256", runtime.CoreClrSha256);
        WriteNullableString(writer, "processPath", runtime.ProcessPath);
        writer.WriteBoolean("isCoreRunHost", runtime.IsCoreRunHost);
        writer.WriteEndObject();

        writer.WriteStartObject("machine");
        WriteNullableString(writer, "processorName", machine.ProcessorName);
        writer.WriteNumber("logicalCores", machine.LogicalCores);
        writer.WriteNumber("totalMemoryBytes", machine.TotalMemoryBytes);
        writer.WriteString("osDescription", machine.OsDescription);
        writer.WriteNumber("processCount", machine.ProcessCount);
        writer.WriteNumber("timerResolutionNs", machine.TimerResolutionNs);
        writer.WriteEndObject();

        WriteNullableString(writer, "samplesPath", options.SamplesPath);
        if (options.HeapFactor is double heapFactor)
        {
            writer.WriteNumber("heapFactor", heapFactor);
        }
        else
        {
            writer.WriteNull("heapFactor");
        }

        if (options.HeapLimitMb is long heapLimitMb)
        {
            writer.WriteNumber("heapLimitMb", heapLimitMb);
        }
        else
        {
            writer.WriteNull("heapLimitMb");
        }

        writer.WriteEndObject();
        writer.Flush();
    }

    private static void WriteLatency(Utf8JsonWriter writer, MeasuredRun run)
    {
        double toMs = 1000.0 / Stopwatch.Frequency;

        int steady = 0;
        for (int i = 0; i < run.RecordCount; i++)
        {
            if (run.Records[i].Phase == 1)
            {
                steady++;
            }
        }

        using var endToIntended = new NativeBuffer<double>(steady);
        using var endToService = new NativeBuffer<double>(steady);
        using var queueDelay = new NativeBuffer<double>(steady);
        using var dispatchLag = new NativeBuffer<double>(steady);
        int next = 0;
        for (int i = 0; i < run.RecordCount; i++)
        {
            ref OperationRecord record = ref run.Records[i];
            if (record.Phase != 1)
            {
                continue;
            }

            endToIntended[next] = (record.EndTimestamp - record.IntendedTimestamp) * toMs;
            endToService[next] = (record.EndTimestamp - record.ServiceStartTimestamp) * toMs;
            queueDelay[next] = (record.ServiceStartTimestamp - record.IntendedTimestamp) * toMs;
            dispatchLag[next] = (record.DispatchTimestamp - record.IntendedTimestamp) * toMs;
            next++;
        }

        endToIntended.AsSpan().Sort();
        endToService.AsSpan().Sort();
        queueDelay.AsSpan().Sort();
        dispatchLag.AsSpan().Sort();

        writer.WriteNumber("operationsPerSecond", run.AchievedRatePerSecond);
        writer.WriteNumber("arrivalRatePerSecond", run.RequestedRatePerSecond);
        writer.WriteNumber("achievedRatePerSecond", run.AchievedRatePerSecond);
        writer.WriteBoolean("overloaded", run.Overloaded);
        writer.WriteNumber("lateCount", run.LateCount);
        // The fraction of arrival slots that had already elapsed when the dispatcher reached them. An
        // open-loop run that cannot keep its own schedule is measuring the harness, not the collector:
        // the queue delay it reports is dispatcher backlog. `overloaded` cannot see this, because it
        // compares achieved throughput against requested and late work still completes.
        writer.WriteNumber("lateFraction", run.RecordCount > 0 ? run.LateCount / (double)run.RecordCount : 0.0);
        writer.WriteNumber("backlogMax", run.BacklogMax);
        writer.WriteNumber("checksum", run.Checksum);
        writer.WriteNumber("wallSeconds", run.WallSeconds);
        writer.WriteNumber("steadyStateOperations", steady);
        writer.WriteNumber("apparatusBytes", run.ApparatusBytes);

        // The coordinated-omission-free numbers.
        writer.WriteString("latencyMethod", OpenLoopDriver.LatencyMethod);
        WritePercentiles(writer, "latency", endToIntended.AsSpan());

        // The same run analysed the way a closed-loop harness would report it. Retained so the size of
        // the coordinated-omission error is a measured quantity in every run, not just in control 2.
        writer.WriteString("serviceTimeMethod", "closed-loop-service-start");
        WritePercentiles(writer, "serviceTime", endToService.AsSpan());
        WritePercentiles(writer, "queueDelay", queueDelay.AsSpan());

        // How far behind its own schedule the dispatcher ran. Latency is only a statement about the
        // collector to the extent this stays small: a lag of tens of milliseconds means the reported
        // percentiles are the harness's backlog. Counting merely *late* slots cannot say this, because
        // a Poisson schedule puts many arrivals closer together than the cost of dispatching one, so a
        // third of slots are microseconds late in a perfectly healthy run.
        WritePercentiles(writer, "dispatchLag", dispatchLag.AsSpan());
    }

    private static void WritePercentiles(Utf8JsonWriter writer, string prefix, ReadOnlySpan<double> sorted)
    {
        writer.WriteNumber(prefix + "P50Ms", Safe(Stats.Percentile(sorted, 50)));
        writer.WriteNumber(prefix + "P99Ms", Safe(Stats.Percentile(sorted, 99)));
        writer.WriteNumber(prefix + "P999Ms", Safe(Stats.Percentile(sorted, 99.9)));
        writer.WriteNumber(prefix + "P9999Ms", Safe(Stats.Percentile(sorted, 99.99)));
        writer.WriteNumber(prefix + "MaxMs", sorted.Length > 0 ? Safe(sorted[^1]) : 0);
        writer.WriteNumber(prefix + "MeanMs", Safe(Stats.Mean(sorted)));
    }

    private static double Safe(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0.0 : value;

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
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

    private static void WriteStringArray(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartArray(name);
        foreach (string value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    internal static string ToInvariant(long value) => value.ToString(CultureInfo.InvariantCulture);
}
