// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the runtime specific interop to native event sinks.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
#if NATIVEAOT
        // We don't have these keywords defined from the genRuntimeEventSources.py, so we need to manually define them here.
        public static partial class Keywords
        {
            public const EventKeywords ContentionKeyword = (EventKeywords)0x4000;
            public const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
            public const EventKeywords ThreadTransferKeyword = (EventKeywords)0x80000000;
            public const EventKeywords WaitHandleKeyword = (EventKeywords)0x40000000000;
        }
#endif

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogContentionLockCreated")]
        private static partial void LogContentionLockCreated(nint LockID, nint AssociatedObjectID, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogContentionStart")]
        private static partial void LogContentionStart(
            ContentionFlagsMap ContentionFlags,
            ushort ClrInstanceID,
            nint LockID,
            nint AssociatedObjectID,
            ulong LockOwnerThreadID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogContentionStop")]
        private static partial void LogContentionStop(
            ContentionFlagsMap ContentionFlags,
            ushort ClrInstanceID,
            double DurationNs);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart")]
        private static partial void LogThreadPoolWorkerThreadStart(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop")]
        private static partial void LogThreadPoolWorkerThreadStop(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait")]
        private static partial void LogThreadPoolWorkerThreadWait(uint ActiveWorkerThreadCount, uint RetiredWorkerThreadCount, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolMinMaxThreads")]
        private static partial void LogThreadPoolMinMaxThreads(ushort MinWorkerThreads, ushort MaxWorkerThreads, ushort MinIOCompletionThreads, ushort MaxIOCompletionThreads, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample")]
        private static partial void LogThreadPoolWorkerThreadAdjustmentSample(double Throughput, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment")]
        private static partial void LogThreadPoolWorkerThreadAdjustmentAdjustment(double AverageThroughput, uint NewWorkerThreadCount, ThreadAdjustmentReasonMap Reason, ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats")]
        private static partial void LogThreadPoolWorkerThreadAdjustmentStats(
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
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolIOEnqueue")]
        private static partial void LogThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            [MarshalAs(UnmanagedType.Bool)] bool MultiDequeues,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolIODequeue")]
        private static partial void LogThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount")]
        private static partial void LogThreadPoolWorkingThreadCount(
            uint Count,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogThreadPoolIOPack")]
        private static partial void LogThreadPoolIOPack(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogWaitHandleWaitStart")]
        private static partial void LogWaitHandleWaitStart(
            WaitHandleWaitSourceMap WaitSource,
            IntPtr AssociatedObjectID,
            ushort ClrInstanceID);

        [NonEvent]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeRuntimeEventSource_LogWaitHandleWaitStop")]
        private static partial void LogWaitHandleWaitStop(ushort ClrInstanceID);
    }
}
