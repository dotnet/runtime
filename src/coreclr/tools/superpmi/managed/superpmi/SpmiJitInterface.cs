// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Internal.JitInterface;

namespace SuperPmi.Replay;

// The native side of the replay host (src/coreclr/tools/superpmi/jitinterface).
internal static unsafe partial class SpmiJitInterface
{
    private const string Library = "superpmi-jitinterface";

    // Matches ExceptionKind in jitinterface.cpp.
    public enum ExceptionKind
    {
        EEException,
        SuperPmiException,
        HostFailure,
    }

    [LibraryImport(Library)]
    public static partial IntPtr AllocException(ExceptionKind kind);

    [LibraryImport(Library)]
    public static partial void FreeException(IntPtr pException);

    [LibraryImport(Library)]
    public static partial CorJitResult JitCompileMethod(IntPtr* ppException, uint* pExceptionCode, IntPtr pJit, IntPtr thisHandle, IntPtr callbacks,
        CORINFO_METHOD_INFO* methodInfo, uint flags, byte** entryAddress, uint* nativeSizeOfCode);

    [LibraryImport(Library)]
    public static partial void JitGetVersionIdentifier(IntPtr pJit, Guid* versionIdentifier);

    [LibraryImport(Library)]
    public static partial void JitSetOs(IntPtr pJit, CORINFO_OS os);

    [LibraryImport(Library)]
    public static partial IntPtr GetJitHost(IntPtr configProvider);

    [LibraryImport(Library)]
    public static partial void Instrumentor_GetInsCount(ulong* result);

    [LibraryImport(Library)]
    public static partial ulong GetThreadCycleTime();

    // The level is 0 for errors, 1 for warnings, and 2 for everything else.
    [LibraryImport(Library)]
    public static partial IntPtr GetCoreDisToolsPrinters(delegate* unmanaged<int, byte*, void> callback);

    [LibraryImport(Library)]
    private static partial int GetJitMetricCount();

    [LibraryImport(Library)]
    private static partial byte* GetJitMetric(int index, JitMetrics.MetricType* type);

    public static JitMetrics.Metric[] GetJitMetrics()
    {
        var metrics = new JitMetrics.Metric[GetJitMetricCount()];
        for (int i = 0; i < metrics.Length; i++)
        {
            JitMetrics.MetricType type;
            ReadOnlySpan<byte> name = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetJitMetric(i, &type));
            metrics[i] = new JitMetrics.Metric(Encoding.UTF8.GetString(name), name.ToArray(), type);
        }
        return metrics;
    }
}

// Measures the CPU time of the current thread, less the overhead of measuring it.
internal sealed class CycleTimer
{
    private readonly ulong _overhead = QueryOverhead();
    private ulong _start;
    private ulong _stop;

    public void Start() => _start = SpmiJitInterface.GetThreadCycleTime();

    public void Stop() => _stop = SpmiJitInterface.GetThreadCycleTime();

    public ulong GetCycles() => _stop - _start - _overhead;

    private static ulong QueryOverhead()
    {
        const int N = 1000;
        ulong total = 0;
        for (int i = 0; i < N; i++)
        {
            ulong start = SpmiJitInterface.GetThreadCycleTime();
            total += SpmiJitInterface.GetThreadCycleTime() - start;
        }
        return total / N;
    }
}
