// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // It contains the handwritten implementation of the ThreadPool events.
    // The events here do not call into the typical WriteEvent* APIs unlike most EventSources because that results in the
    // events to be forwarded to EventListeners twice, once directly from the managed WriteEvent API, and another time
    // from the mechanism in NativeRuntimeEventSource.ProcessEvents that forwards native runtime events to EventListeners.
    // To prevent this, these events call directly into QCalls provided by the runtime (refer to NativeRuntimeEventSource.cs) which call
    // FireEtw* methods auto-generated from ClrEtwAll.man. This ensures that corresponding event sinks are being used
    // for the native platform.
    // For implementation of these events not supporting native sinks, refer to NativeRuntimeEventSource.PortableThreadPool.cs.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        // This value does not seem to be used, leaving it as zero for now. It may be useful for a scenario that may involve
        // multiple instances of the runtime within the same process, but then it seems unlikely that both instances' thread
        // pools would be in moderate use.
        private const ushort DefaultClrInstanceId = 0;

        private static class Messages
        {
            public const string WorkerThread = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}";
            public const string WorkerThreadAdjustmentSample = "Throughput={0};\nClrInstanceID={1}";
            public const string WorkerThreadAdjustmentAdjustment = "AverageThroughput={0};\nNewWorkerThreadCount={1};\nReason={2};\nClrInstanceID={3}";
            public const string WorkerThreadAdjustmentStats = "Duration={0};\nThroughput={1};\nThreadWave={2};\nThroughputWave={3};\nThroughputErrorEstimate={4};\nAverageThroughputErrorEstimate={5};\nThroughputRatio={6};\nConfidence={7};\nNewControlSetting={8};\nNewThreadWaveMagnitude={9};\nClrInstanceID={10}";
            public const string IOEnqueue = "NativeOverlapped={0};\nOverlapped={1};\nMultiDequeues={2};\nClrInstanceID={3}";
            public const string IO = "NativeOverlapped={0};\nOverlapped={1};\nClrInstanceID={2}";
            public const string WorkingThreadCount = "Count={0};\nClrInstanceID={1}";
        }

        // The task definitions for the ETW manifest
        public static class Tasks // this name and visibility is important for EventSource
        {
            public const EventTask ThreadPoolWorkerThread = (EventTask)16;
            public const EventTask ThreadPoolWorkerThreadAdjustment = (EventTask)18;
            public const EventTask ThreadPool = (EventTask)23;
            public const EventTask ThreadPoolWorkingThreadCount = (EventTask)22;
        }

        public static class Opcodes // this name and visibility is important for EventSource
        {
            public const EventOpcode IOEnqueue = (EventOpcode)13;
            public const EventOpcode IODequeue = (EventOpcode)14;
            public const EventOpcode Wait = (EventOpcode)90;
            public const EventOpcode Sample = (EventOpcode)100;
            public const EventOpcode Adjustment = (EventOpcode)101;
            public const EventOpcode Stats = (EventOpcode)102;
        }

        public enum ThreadAdjustmentReasonMap : uint
        {
            Warmup,
            Initializing,
            RandomMove,
            ClimbingMove,
            ChangePoint,
            Stabilizing,
            Starvation,
            ThreadTimedOut
        }

        [Event(50, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadStart(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount = 0,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                LogThreadPoolWorkerThreadStart(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
            }
        }

        [Event(51, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Stop, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public void ThreadPoolWorkerThreadStop(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount = 0,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                LogThreadPoolWorkerThreadStop(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
            }
        }

        [Event(57, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = Opcodes.Wait, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolWorkerThreadWait(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount = 0,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                LogThreadPoolWorkerThreadWait(ActiveWorkerThreadCount, RetiredWorkerThreadCount, ClrInstanceID);
            }
        }

        [Event(54, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentSample, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Sample, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentSample(
            double Throughput,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }
            LogThreadPoolWorkerThreadAdjustmentSample(Throughput, ClrInstanceID);
        }

        [Event(55, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentAdjustment, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Adjustment, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentAdjustment(
            double AverageThroughput,
            uint NewWorkerThreadCount,
            ThreadAdjustmentReasonMap Reason,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }
            LogThreadPoolWorkerThreadAdjustmentAdjustment(AverageThroughput, NewWorkerThreadCount, Reason, ClrInstanceID);
        }

        [Event(56, Level = EventLevel.Verbose, Message = Messages.WorkerThreadAdjustmentStats, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Stats, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentStats(
            double Duration,
            double Throughput,
            double ThreadWave,
            double ThroughputWave,
            double ThroughputErrorEstimate,
            double AverageThroughputErrorEstimate,
            double ThroughputRatio,
            double Confidence,
            double NewControlSetting,
            ushort NewThreadWaveMagnitude,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
                return;
            }
            LogThreadPoolWorkerThreadAdjustmentStats(Duration, Throughput, ThreadWave, ThroughputWave, ThroughputErrorEstimate, AverageThroughputErrorEstimate, ThroughputRatio, Confidence, NewControlSetting, NewThreadWaveMagnitude, ClrInstanceID);
        }

        [Event(63, Level = EventLevel.Verbose, Message = Messages.IOEnqueue, Task = Tasks.ThreadPool, Opcode = Opcodes.IOEnqueue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            bool MultiDequeues,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            LogThreadPoolIOEnqueue(NativeOverlapped, Overlapped, MultiDequeues, ClrInstanceID);
        }

        // TODO: This event is fired for minor compat with CoreCLR in this case. Consider removing this method and use
        // FrameworkEventSource's thread transfer send/receive events instead at callers.
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolIOEnqueue(RegisteredWaitHandle registeredWaitHandle)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
                ThreadPoolIOEnqueue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero, registeredWaitHandle.Repeating);
            }
        }

        [Event(64, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IODequeue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            LogThreadPoolIODequeue(NativeOverlapped, Overlapped, ClrInstanceID);
        }

        // TODO: This event is fired for minor compat with CoreCLR in this case. Consider removing this method and use
        // FrameworkEventSource's thread transfer send/receive events instead at callers.
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolIODequeue(RegisteredWaitHandle registeredWaitHandle)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
                ThreadPoolIODequeue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero);
            }
        }

        [Event(60, Level = EventLevel.Verbose, Message = Messages.WorkingThreadCount, Task = Tasks.ThreadPoolWorkingThreadCount, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
                return;
            }
            LogThreadPoolWorkingThreadCount(Count, ClrInstanceID);
        }
    }
}
