// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the runtime specific interop to native event sinks.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        // We don't have these keywords defined from the genRuntimeEventSources.py, so we need to manually define them here.
        public static partial class Keywords
        {
            public const EventKeywords ContentionKeyword = (EventKeywords)0x4000;
            public const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
            public const EventKeywords ThreadTransferKeyword = (EventKeywords)0x80000000;
            public const EventKeywords WaitHandleKeyword = (EventKeywords)0x40000000000;
        }

        [NonEvent]
        internal static void LogContentionLockCreated(nint LockID, nint AssociatedObjectID, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogContentionLockCreated(LockID, AssociatedObjectID, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogContentionStart(ContentionFlagsMap ContentionFlags, ushort ClrInstanceID, nint LockID, nint AssociatedObjectID, ulong LockOwnerThreadID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogContentionStart((byte)ContentionFlags, ClrInstanceID, LockID, AssociatedObjectID, LockOwnerThreadID);
        }

        [NonEvent]
        internal static void LogContentionStop(ContentionFlagsMap ContentionFlags, ushort ClrInstanceID, double DurationNs)
        {
            RuntimeImports.NativeRuntimeEventSource_LogContentionStop((byte)ContentionFlags, ClrInstanceID, DurationNs);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolMinMaxThreads(ushort MinWorkerThreads, ushort MaxWorkerThreads, ushort MinIOCompletionThreads, ushort MaxIOCompletionThreads, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolMinMaxThreads(MinWorkerThreads, MaxWorkerThreads, MinIOCompletionThreads, MaxIOCompletionThreads, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, (uint)Reason, ClrInstanceID);
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
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadPoolWorkerThreadWait, ThroughputWave,
            ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            [MarshalAs(UnmanagedType.Bool)] bool MultiDequeues,
            ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolWorkingThreadCount(
            uint Count,
            ushort ClrInstanceID
        )
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount(Count, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogThreadPoolIOPack(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogThreadPoolIOPack(NativeOverlapped, Overlapped, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogWaitHandleWaitStart(
            WaitHandleWaitSourceMap WaitSource,
            IntPtr AssociatedObjectID,
            ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogWaitHandleWaitStart((byte)WaitSource, AssociatedObjectID, ClrInstanceID);
        }

        [NonEvent]
        internal static void LogWaitHandleWaitStop(ushort ClrInstanceID)
        {
            RuntimeImports.NativeRuntimeEventSource_LogWaitHandleWaitStop(ClrInstanceID);
        }
    }
}
