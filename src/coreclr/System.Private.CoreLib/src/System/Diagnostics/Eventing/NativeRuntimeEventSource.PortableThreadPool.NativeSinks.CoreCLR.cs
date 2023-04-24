// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the runtime specific interop to native event sinks.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolMinMaxThreads(ushort MinWorkerThreads, ushort MaxWorkerThreads, ushort MinIOCompletionThreads, ushort MaxIOCompletionThreads, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, NativeRuntimeEventSource.ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkerThreadAdjustmentStats(
            double Duration,
            double Throughput,
            double ThreadPoolWorkerThreadWait,
            double ThroughputWave,
            double ThroughputErrorEstimate,
            double AverageThroughputErrorEstimate,
            double ThroughputRatio,
            double Confidence,
            double NewControlSetting,
            ushort NewThreadWaveMagnitude,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            [MarshalAs(UnmanagedType.Bool)] bool MultiDequeues,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolWorkingThreadCount(
            uint Count,
            ushort ClrInstanceID
        );

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall)]
        internal static partial void LogThreadPoolIOPack(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID);
    }
}
