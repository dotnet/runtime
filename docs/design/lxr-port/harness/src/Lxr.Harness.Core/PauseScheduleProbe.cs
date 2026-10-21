// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace Lxr.Harness.Core;

internal sealed class PauseScheduleProbe : IDisposable
{
    private static readonly string[] s_kindNames =
        ["Full", "RefCount", "InitialMark", "FinalMark"];

    private readonly nint _library;
    private readonly nint _handle;
    private readonly RunCallback _run;
    private readonly GetMetricsCallback _getMetrics;
    private readonly DestroyCallback _destroy;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _cancel = new(initialState: false);
    private readonly List<ulong>[] _samples = [[], [], [], []];
    private readonly int _countPerKind;
    private readonly double _warmupSeconds;
    private readonly double _steadySeconds;
    private readonly int _policy;
    private readonly string _policyName;
    private ExceptionDispatchInfo? _failure;
    private long _startTimestamp;
    private bool _started;
    private bool _disposed;

    private PauseScheduleProbe(
        string libraryPath,
        int workerCount,
        int policy,
        string policyName,
        int countPerKind,
        int injectedMicroseconds,
        double warmupSeconds,
        double steadySeconds)
    {
        _policy = policy;
        _policyName = policyName;
        _countPerKind = countPerKind;
        _warmupSeconds = warmupSeconds;
        _steadySeconds = steadySeconds;
        _library = NativeLibrary.Load(libraryPath);
        try
        {
            CreateCallback create = GetExport<CreateCallback>("P32_Benchmark_Create");
            _run = GetExport<RunCallback>("P32_Benchmark_Run");
            _getMetrics = GetExport<GetMetricsCallback>("P32_Benchmark_GetMetrics");
            _destroy = GetExport<DestroyCallback>("P32_Benchmark_Destroy");
            _handle = create(
                (nuint)Math.Max(1, workerCount),
                (uint)policy,
                (uint)injectedMicroseconds);
            if (_handle == 0)
            {
                throw new InvalidOperationException(
                    "The native P3.2 pause benchmark rejected its configuration.");
            }
        }
        catch
        {
            NativeLibrary.Free(_library);
            throw;
        }

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "P3.2 synthetic pause schedule",
        };
    }

    public static PauseScheduleProbe? TryCreateFromEnvironment(
        IReadOnlyDictionary<string, string> parameters,
        int workerCount,
        double warmupSeconds,
        double steadySeconds)
    {
        string? libraryPath = Environment.GetEnvironmentVariable(
            "P32_PAUSE_SCHEDULE_LIBRARY");
        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            return null;
        }

        string policyName = Get(parameters, "pauseSchedulePolicy", "scheduled");
        int policy = policyName.ToLowerInvariant() switch
        {
            "scheduled" => 1,
            "null" => 2,
            "injected" => 3,
            "same-code" => 4,
            _ => throw new ArgumentException(
                $"Unknown pauseSchedulePolicy '{policyName}'."),
        };
        int countPerKind = GetInt32(
            parameters,
            "pauseScheduleCountPerKind",
            defaultValue: 8,
            minimum: 1);
        int injectedMicroseconds = GetInt32(
            parameters,
            "pauseScheduleInjectedMicroseconds",
            defaultValue: 5000,
            minimum: 0);
        return new PauseScheduleProbe(
            libraryPath,
            workerCount,
            policy,
            policyName,
            countPerKind,
            injectedMicroseconds,
            warmupSeconds,
            steadySeconds);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started)
        {
            throw new InvalidOperationException(
                "The pause schedule probe was already started.");
        }

        _started = true;
        _startTimestamp = Stopwatch.GetTimestamp();
        _thread.Start();
    }

    public PauseScheduleSnapshot WaitForCompletion()
    {
        if (!_started)
        {
            throw new InvalidOperationException(
                "The pause schedule probe was not started.");
        }

        _thread.Join();
        _failure?.Throw();
        if (_getMetrics(_handle, out NativeMetrics metrics) != 0)
        {
            throw new InvalidOperationException(
                "The native P3.2 pause benchmark did not return metrics.");
        }

        ulong[][] samples =
        [
            [.. _samples[0]],
            [.. _samples[1]],
            [.. _samples[2]],
            [.. _samples[3]],
        ];
        return new PauseScheduleSnapshot(
            _policyName,
            _policy,
            _countPerKind,
            samples,
            metrics);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancel.Set();
        if (_started && _thread.IsAlive)
        {
            _thread.Join();
        }
        _destroy(_handle);
        NativeLibrary.Free(_library);
        _cancel.Dispose();
    }

    private void Run()
    {
        try
        {
            int total = checked(_countPerKind * s_kindNames.Length);
            for (int sequence = 0; sequence < total; sequence++)
            {
                double targetSeconds =
                    _warmupSeconds +
                    (_steadySeconds * (sequence + 1) / (total + 1));
                WaitUntil(targetSeconds);
                if (_cancel.IsSet)
                {
                    return;
                }

                int kind = sequence % s_kindNames.Length;
                ulong duration = _run(
                    _handle,
                    (uint)kind,
                    (ulong)sequence,
                    out int status);
                if (status != 0)
                {
                    throw new InvalidOperationException(
                        $"The native P3.2 pause benchmark failed with status {status}.");
                }
                _samples[kind].Add(duration);
            }
        }
        catch (Exception ex)
        {
            _failure = ExceptionDispatchInfo.Capture(ex);
        }
    }

    private void WaitUntil(double targetSeconds)
    {
        while (true)
        {
            double remainingSeconds =
                targetSeconds -
                Stopwatch.GetElapsedTime(_startTimestamp).TotalSeconds;
            if (remainingSeconds <= 0)
            {
                return;
            }

            int waitMilliseconds = Math.Max(
                1,
                Math.Min(25, (int)Math.Ceiling(remainingSeconds * 1000)));
            if (_cancel.Wait(waitMilliseconds))
            {
                return;
            }
        }
    }

    private T GetExport<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

    private static string Get(
        IReadOnlyDictionary<string, string> parameters,
        string name,
        string defaultValue) =>
        parameters.TryGetValue(name, out string? value) ? value : defaultValue;

    private static int GetInt32(
        IReadOnlyDictionary<string, string> parameters,
        string name,
        int defaultValue,
        int minimum)
    {
        if (!parameters.TryGetValue(name, out string? text))
        {
            return defaultValue;
        }
        if (!int.TryParse(
            text,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int value) ||
            value < minimum)
        {
            throw new ArgumentException(
                $"{name} must be an integer greater than or equal to {minimum}.");
        }
        return value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeMetrics
    {
        public ulong Schedules;
        public ulong Suspends;
        public ulong Resumes;
        public ulong KindCountFull;
        public ulong KindCountRefCount;
        public ulong KindCountInitialMark;
        public ulong KindCountFinalMark;
        public ulong AaNanosecondsA;
        public ulong AaNanosecondsB;
        public ulong AaCountA;
        public ulong AaCountB;

        public readonly ulong GetKindCount(int index) => index switch
        {
            0 => KindCountFull,
            1 => KindCountRefCount,
            2 => KindCountInitialMark,
            3 => KindCountFinalMark,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint CreateCallback(
        nuint workerCount,
        uint policy,
        uint injectedMicroseconds);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong RunCallback(
        nint handle,
        uint pauseKind,
        ulong sequence,
        out int status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetMetricsCallback(nint handle, out NativeMetrics metrics);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DestroyCallback(nint handle);
}

internal sealed class PauseScheduleSnapshot
{
    private static readonly string[] s_kindNames =
        ["Full", "RefCount", "InitialMark", "FinalMark"];

    private readonly PauseScheduleProbe.NativeMetrics _metrics;

    public PauseScheduleSnapshot(
        string policyName,
        int policy,
        int countPerKind,
        ulong[][] samples,
        PauseScheduleProbe.NativeMetrics metrics)
    {
        PolicyName = policyName;
        Policy = policy;
        CountPerKind = countPerKind;
        Samples = samples;
        _metrics = metrics;
        Failures = Validate();
    }

    public string PolicyName { get; }

    public int Policy { get; }

    public int CountPerKind { get; }

    public ulong[][] Samples { get; }

    public IReadOnlyList<string> Failures { get; }

    public void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("pauseSchedule");
        writer.WriteString("source", "synthetic-p3.2-scheduler");
        writer.WriteString("policy", PolicyName);
        writer.WriteNumber("policyId", Policy);
        writer.WriteNumber("requestedCountPerKind", CountPerKind);
        writer.WriteNumber("completedSchedules", _metrics.Schedules);
        writer.WriteNumber("syntheticSuspends", _metrics.Suspends);
        writer.WriteNumber("syntheticResumes", _metrics.Resumes);
        writer.WriteNumber("aaNanosecondsA", _metrics.AaNanosecondsA);
        writer.WriteNumber("aaNanosecondsB", _metrics.AaNanosecondsB);
        writer.WriteNumber("aaCountA", _metrics.AaCountA);
        writer.WriteNumber("aaCountB", _metrics.AaCountB);
        writer.WriteStartArray("kinds");
        for (int index = 0; index < s_kindNames.Length; index++)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", s_kindNames[index]);
            writer.WriteNumber("completedCount", _metrics.GetKindCount(index));
            writer.WriteStartArray("durationNanoseconds");
            foreach (ulong sample in Samples[index])
            {
                writer.WriteNumberValue(sample);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private IReadOnlyList<string> Validate()
    {
        List<string> failures = [];
        ulong expectedMultiplier = Policy is 3 or 4 ? 2UL : Policy == 2 ? 0UL : 1UL;
        ulong expectedPerKind = checked((ulong)CountPerKind * expectedMultiplier);
        ulong expectedTotal = checked(expectedPerKind * (ulong)s_kindNames.Length);
        if (_metrics.Schedules != expectedTotal)
        {
            failures.Add(
                $"synthetic schedules differ: expected {expectedTotal}, observed {_metrics.Schedules}");
        }
        if (_metrics.Suspends != expectedTotal || _metrics.Resumes != expectedTotal)
        {
            failures.Add(
                $"synthetic adapter counts differ: schedules={_metrics.Schedules}, " +
                $"suspends={_metrics.Suspends}, resumes={_metrics.Resumes}");
        }
        for (int index = 0; index < s_kindNames.Length; index++)
        {
            ulong observedKindCount = _metrics.GetKindCount(index);
            if (observedKindCount != expectedPerKind)
            {
                failures.Add(
                    $"{s_kindNames[index]} count differs: expected {expectedPerKind}, " +
                    $"observed {observedKindCount}");
            }
            if (Samples[index].Length != CountPerKind)
            {
                failures.Add(
                    $"{s_kindNames[index]} samples differ: expected {CountPerKind}, " +
                    $"observed {Samples[index].Length}");
            }
        }
        if (Policy == 2 &&
            (_metrics.Schedules != 0 || _metrics.Suspends != 0 || _metrics.Resumes != 0))
        {
            failures.Add("null policy reported synthetic suspensions");
        }
        if (Policy == 4 && (_metrics.AaCountA == 0 || _metrics.AaCountB == 0))
        {
            failures.Add("same-code policy produced no paired A/A measurements");
        }
        return failures;
    }
}
