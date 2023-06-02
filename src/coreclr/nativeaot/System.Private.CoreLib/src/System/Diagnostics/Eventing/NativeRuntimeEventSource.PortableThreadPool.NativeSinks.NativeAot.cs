// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the runtime specific interop to native event sinks.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        [NonEvent]
        internal static void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolMinMaxThreads(ushort MinWorkerThreads, ushort MaxWorkerThreads, ushort MinIOCompletionThreads, ushort MaxIOCompletionThreads, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolMinMaxThreads(MinWorkerThreads, MaxWorkerThreads, MinIOCompletionThreads, MaxIOCompletionThreads, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
        }

        [NonEvent]
        // Reason parameter is an enum in NativeRuntimeEventSource but passed here as the underlying type
        internal static void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, uint Reason, ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadAdjustmentStats(
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
            ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadPoolWorkerThreadWait, ThroughputWave,
            ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            [MarshalAs(UnmanagedType.Bool)] bool MultiDequeues,
            ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkingThreadCount(
            uint Count,
            ushort ClrInstanceID
        )
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolWorkingThreadCount(Count, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIOPack(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
        {
            RuntimeImports.RhEventPipeInternal_LogThreadPoolIOPack(NativeOverlapped, Overlapped, ClrInstanceID);
        }
    }
}
