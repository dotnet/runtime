// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // Contains the implementation of ThreadPool events. This implemention is used by runtime not supporting NativeRuntimeEventSource.PortableThreadPool.NativeSinks.cs.
    internal sealed partial class NativeRuntimeEventSource : EventSource
    {
        // We don't have these keywords defined from the genRuntimeEventSources.py, so we need to manually define them here.
        public static class Keywords
        {
            public const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
            public const EventKeywords ThreadTransferKeyword = (EventKeywords)0x80000000;
        }

#if !ES_BUILD_STANDALONE
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";
#endif
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

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
        [NonEvent]
        private unsafe void WriteThreadEvent(int eventId, uint numExistingThreads)
        {
            uint retiredWorkerThreadCount = 0;
            ushort clrInstanceId = DefaultClrInstanceId;

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (IntPtr)(&numExistingThreads);
            data[0].Size = sizeof(uint);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&retiredWorkerThreadCount);
            data[1].Size = sizeof(uint);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&clrInstanceId);
            data[2].Size = sizeof(ushort);
            data[2].Reserved = 0;
            WriteEventCore(eventId, 3, data);
        }

        [Event(50, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadStart(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount = 0,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                WriteThreadEvent(50, ActiveWorkerThreadCount);
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
                WriteThreadEvent(51, ActiveWorkerThreadCount);
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
                WriteThreadEvent(57, ActiveWorkerThreadCount);
            }
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
        [Event(54, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentSample, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Sample, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentSample(
            double Throughput,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }
            EventData* data = stackalloc EventData[2];
            data[0].DataPointer = (IntPtr)(&Throughput);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&ClrInstanceID);
            data[1].Size = sizeof(ushort);
            data[1].Reserved = 0;
            WriteEventCore(54, 2, data);
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
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
            EventData* data = stackalloc EventData[4];
            data[0].DataPointer = (IntPtr)(&AverageThroughput);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&NewWorkerThreadCount);
            data[1].Size = sizeof(uint);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&Reason);
            data[2].Size = sizeof(ThreadAdjustmentReasonMap);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&ClrInstanceID);
            data[3].Size = sizeof(ushort);
            data[3].Reserved = 0;
            WriteEventCore(55, 4, data);
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
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
            EventData* data = stackalloc EventData[11];
            data[0].DataPointer = (IntPtr)(&Duration);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&Throughput);
            data[1].Size = sizeof(double);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&ThreadWave);
            data[2].Size = sizeof(double);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&ThroughputWave);
            data[3].Size = sizeof(double);
            data[3].Reserved = 0;
            data[4].DataPointer = (IntPtr)(&ThroughputErrorEstimate);
            data[4].Size = sizeof(double);
            data[4].Reserved = 0;
            data[5].DataPointer = (IntPtr)(&AverageThroughputErrorEstimate);
            data[5].Size = sizeof(double);
            data[5].Reserved = 0;
            data[6].DataPointer = (IntPtr)(&ThroughputRatio);
            data[6].Size = sizeof(double);
            data[6].Reserved = 0;
            data[7].DataPointer = (IntPtr)(&Confidence);
            data[7].Size = sizeof(double);
            data[7].Reserved = 0;
            data[8].DataPointer = (IntPtr)(&NewControlSetting);
            data[8].Size = sizeof(double);
            data[8].Reserved = 0;
            data[9].DataPointer = (IntPtr)(&NewThreadWaveMagnitude);
            data[9].Size = sizeof(ushort);
            data[9].Reserved = 0;
            data[10].DataPointer = (IntPtr)(&ClrInstanceID);
            data[10].Size = sizeof(ushort);
            data[10].Reserved = 0;
            WriteEventCore(56, 11, data);
        }

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
        [Event(63, Level = EventLevel.Verbose, Message = Messages.IOEnqueue, Task = Tasks.ThreadPool, Opcode = Opcodes.IOEnqueue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            bool MultiDequeues,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            int multiDequeuesInt = Convert.ToInt32(MultiDequeues); // bool maps to "win:Boolean", a 4-byte boolean
            EventData* data = stackalloc EventData[4];
            data[0].DataPointer = (IntPtr)(&NativeOverlapped);
            data[0].Size = IntPtr.Size;
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&Overlapped);
            data[1].Size = IntPtr.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&multiDequeuesInt);
            data[2].Size = sizeof(int);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&ClrInstanceID);
            data[3].Size = sizeof(ushort);
            data[3].Reserved = 0;
            WriteEventCore(63, 4, data);
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

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
        [Event(64, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IODequeue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (IntPtr)(&NativeOverlapped);
            data[0].Size = IntPtr.Size;
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&Overlapped);
            data[1].Size = IntPtr.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&ClrInstanceID);
            data[2].Size = sizeof(ushort);
            data[2].Reserved = 0;
            WriteEventCore(64, 3, data);
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

#if !ES_BUILD_STANDALONE
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
#endif
        [Event(60, Level = EventLevel.Verbose, Message = Messages.WorkingThreadCount, Task = Tasks.ThreadPoolWorkingThreadCount, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
                return;
            }
            EventData* data = stackalloc EventData[2];
            data[0].DataPointer = (IntPtr)(&Count);
            data[0].Size = sizeof(uint);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&ClrInstanceID);
            data[1].Size = sizeof(ushort);
            data[1].Reserved = 0;
            WriteEventCore(60, 2, data);
        }
    }
}
