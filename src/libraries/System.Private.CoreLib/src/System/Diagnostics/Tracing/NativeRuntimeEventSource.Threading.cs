// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    // This is part of the NativeRuntimeEventsource, which is the managed version of the Microsoft-Windows-DotNETRuntime provider.
    // Contains the implementation of threading events. This implementation is used by runtime not supporting NativeRuntimeEventSource.Threading.NativeSinks.cs.
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

        private static partial class Messages
        {
            public const string ContentionLockCreated = "LockID={0};\nAssociatedObjectID={1};\nClrInstanceID={2}";
            public const string ContentionStart = "ContentionFlags={0};\nClrInstanceID={1};\nLockID={2};\nAssociatedObjectID={3}\nLockOwnerThreadID={4}";
            public const string ContentionStop = "ContentionFlags={0};\nClrInstanceID={1};\nDurationNs={2}";
            public const string WorkerThread = "ActiveWorkerThreadCount={0};\nRetiredWorkerThreadCount={1};\nClrInstanceID={2}";
            public const string MinMaxThreads = "MinWorkerThreads={0};\nMaxWorkerThreads={1};\nMinIOCompletionThreads={2};\nMaxIOCompletionThreads={3};\nClrInstanceID={4}";
            public const string WorkerThreadAdjustmentSample = "Throughput={0};\nClrInstanceID={1}";
            public const string WorkerThreadAdjustmentAdjustment = "AverageThroughput={0};\nNewWorkerThreadCount={1};\nReason={2};\nClrInstanceID={3}";
            public const string WorkerThreadAdjustmentStats = "Duration={0};\nThroughput={1};\nThreadWave={2};\nThroughputWave={3};\nThroughputErrorEstimate={4};\nAverageThroughputErrorEstimate={5};\nThroughputRatio={6};\nConfidence={7};\nNewControlSetting={8};\nNewThreadWaveMagnitude={9};\nClrInstanceID={10}";
            public const string IOEnqueue = "NativeOverlapped={0};\nOverlapped={1};\nMultiDequeues={2};\nClrInstanceID={3}";
            public const string IO = "NativeOverlapped={0};\nOverlapped={1};\nClrInstanceID={2}";
            public const string WorkingThreadCount = "Count={0};\nClrInstanceID={1}";
            public const string WaitHandleWaitStart = "WaitSource={0};\nAssociatedObjectID={1};\nClrInstanceID={2}";
            public const string WaitHandleWaitStop = "ClrInstanceID={0}";
        }

        // The task definitions for the ETW manifest
        public static partial class Tasks // this name and visibility is important for EventSource
        {
            public const EventTask Contention = (EventTask)8;
            public const EventTask ThreadPoolWorkerThread = (EventTask)16;
            public const EventTask ThreadPoolWorkerThreadAdjustment = (EventTask)18;
            public const EventTask ThreadPool = (EventTask)23;
            public const EventTask ThreadPoolWorkingThreadCount = (EventTask)22;
            public const EventTask ThreadPoolMinMaxThreads = (EventTask)38;
            public const EventTask WaitHandleWait = (EventTask)39;
        }

        public static partial class Opcodes // this name and visibility is important for EventSource
        {
            public const EventOpcode LockCreated = (EventOpcode)11;
            public const EventOpcode IOEnqueue = (EventOpcode)13;
            public const EventOpcode IODequeue = (EventOpcode)14;
            public const EventOpcode IOPack = (EventOpcode)15;
            public const EventOpcode Wait = (EventOpcode)90;
            public const EventOpcode Sample = (EventOpcode)100;
            public const EventOpcode Adjustment = (EventOpcode)101;
            public const EventOpcode Stats = (EventOpcode)102;
        }

        public enum ContentionFlagsMap : byte
        {
            Managed,
            Native,
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
            ThreadTimedOut,
            CooperativeBlocking,
        }

        public enum WaitHandleWaitSourceMap : byte
        {
            Unknown,
            MonitorWait,
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(90, Level = EventLevel.Informational, Message = Messages.ContentionLockCreated, Task = Tasks.Contention, Opcode = EventOpcode.Info, Version = 0, Keywords = Keywords.ContentionKeyword)]
        private unsafe void ContentionLockCreated(nint LockID, nint AssociatedObjectID, ushort ClrInstanceID = DefaultClrInstanceId)
        {
            Debug.Assert(IsEnabled(EventLevel.Informational, Keywords.ContentionKeyword));

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (nint)(&LockID);
            data[0].Size = nint.Size;
            data[0].Reserved = 0;
            data[1].DataPointer = (nint)(&AssociatedObjectID);
            data[1].Size = nint.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (nint)(&ClrInstanceID);
            data[2].Size = sizeof(ushort);
            data[2].Reserved = 0;
            WriteEventCore(90, 3, data);
        }

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ContentionLockCreated(Lock lockObj) => ContentionLockCreated(lockObj.LockIdForEvents, lockObj.ObjectIdForEvents);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(81, Level = EventLevel.Informational, Message = Messages.ContentionStart, Task = Tasks.Contention, Opcode = EventOpcode.Start, Version = 2, Keywords = Keywords.ContentionKeyword)]
        private unsafe void ContentionStart(
            ContentionFlagsMap ContentionFlags,
            ushort ClrInstanceID,
            nint LockID,
            nint AssociatedObjectID,
            ulong LockOwnerThreadID)
        {
            Debug.Assert(IsEnabled(EventLevel.Informational, Keywords.ContentionKeyword));

            EventData* data = stackalloc EventData[5];
            data[0].DataPointer = (nint)(&ContentionFlags);
            data[0].Size = sizeof(ContentionFlagsMap);
            data[0].Reserved = 0;
            data[1].DataPointer = (nint)(&ClrInstanceID);
            data[1].Size = sizeof(ushort);
            data[1].Reserved = 0;
            data[2].DataPointer = (nint)(&LockID);
            data[2].Size = nint.Size;
            data[2].Reserved = 0;
            data[3].DataPointer = (nint)(&AssociatedObjectID);
            data[3].Size = nint.Size;
            data[3].Reserved = 0;
            data[4].DataPointer = (nint)(&LockOwnerThreadID);
            data[4].Size = sizeof(ulong);
            data[4].Reserved = 0;
            WriteEventCore(81, 5, data);
        }

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ContentionStart(Lock lockObj) =>
            ContentionStart(
                ContentionFlagsMap.Managed,
                DefaultClrInstanceId,
                lockObj.LockIdForEvents,
                lockObj.ObjectIdForEvents,
                lockObj.OwningThreadId);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(91, Level = EventLevel.Informational, Message = Messages.ContentionStop, Task = Tasks.Contention, Opcode = EventOpcode.Stop, Version = 1, Keywords = Keywords.ContentionKeyword)]
        private unsafe void ContentionStop(ContentionFlagsMap ContentionFlags, ushort ClrInstanceID, double DurationNs)
        {
            Debug.Assert(IsEnabled(EventLevel.Informational, Keywords.ContentionKeyword));

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (nint)(&ContentionFlags);
            data[0].Size = sizeof(ContentionFlagsMap);
            data[0].Reserved = 0;
            data[1].DataPointer = (nint)(&ClrInstanceID);
            data[1].Size = sizeof(ushort);
            data[1].Reserved = 0;
            data[2].DataPointer = (nint)(&DurationNs);
            data[2].Size = sizeof(double);
            data[2].Reserved = 0;
            WriteEventCore(91, 3, data);
        }

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ContentionStop(double durationNs) =>
            ContentionStop(ContentionFlagsMap.Managed, DefaultClrInstanceId, durationNs);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
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

#pragma warning disable IDE0060
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
#pragma warning restore IDE0060

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(63, Level = EventLevel.Verbose, Message = Messages.IOEnqueue, Task = Tasks.ThreadPool, Opcode = Opcodes.IOEnqueue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped, // 0 if the Windows thread pool is used, the relevant info could be obtained from the NativeOverlapped* if necessary
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

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ThreadPoolIOEnqueue(NativeOverlapped* nativeOverlapped)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
#if TARGET_WINDOWS
                IntPtr overlapped = ThreadPool.UseWindowsThreadPool ? 0 : (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#else
                IntPtr overlapped = (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#endif
                ThreadPoolIOEnqueue(
                    (IntPtr)nativeOverlapped,
                    overlapped,
                    false);
            }
        }

        // TODO: This event is fired for minor compat with CoreCLR in this case. Consider removing this method and use
        // FrameworkEventSource's thread transfer send/receive events instead at callers.
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolIOEnqueue(RegisteredWaitHandle registeredWaitHandle)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
#pragma warning disable CA1416 // 'RegisteredWaitHandle.Repeating' is unsupported on: 'browser'
                ThreadPoolIOEnqueue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero, registeredWaitHandle.Repeating);
#pragma warning restore CA1416
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(64, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IODequeue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped, // 0 if the Windows thread pool is used, the relevant info could be obtained from the NativeOverlapped* if necessary
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

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ThreadPoolIODequeue(NativeOverlapped* nativeOverlapped)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
#if TARGET_WINDOWS
                IntPtr overlapped = ThreadPool.UseWindowsThreadPool ? 0 : (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#else
                IntPtr overlapped = (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#endif
                ThreadPoolIODequeue(
                    (IntPtr)nativeOverlapped,
                    overlapped);
            }
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
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

        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void ThreadPoolIOPack(NativeOverlapped* nativeOverlapped)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
#if TARGET_WINDOWS
                IntPtr overlapped = ThreadPool.UseWindowsThreadPool ? 0 : (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#else
                IntPtr overlapped = (IntPtr)Overlapped.GetOverlappedFromNative(nativeOverlapped).GetHashCode();
#endif
                ThreadPoolIOPack(
                    (IntPtr)nativeOverlapped,
                    overlapped);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(65, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IOPack, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        private unsafe void ThreadPoolIOPack(
            IntPtr NativeOverlapped,
            IntPtr Overlapped, // 0 if the Windows thread pool is used, the relevant info could be obtained from the NativeOverlapped* if necessary
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = NativeOverlapped;
            data[0].Size        = sizeof(IntPtr);
            data[0].Reserved    = 0;
            data[1].DataPointer = Overlapped;
            data[1].Size        = sizeof(IntPtr);
            data[1].Reserved    = 0;
            data[2].DataPointer = (IntPtr)(&ClrInstanceID);
            data[2].Size        = sizeof(ushort);
            data[2].Reserved    = 0;
            WriteEventCore(65, 3, data);
        }


        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(59, Level = EventLevel.Informational, Message = Messages.MinMaxThreads, Task = Tasks.ThreadPoolMinMaxThreads, Opcode = EventOpcode.Info, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolMinMaxThreads(
            ushort MinWorkerThreads,
            ushort MaxWorkerThreads,
            ushort MinIOCompletionThreads,
            ushort MaxIOCompletionThreads,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }
            EventData* data = stackalloc EventData[5];
            data[0].DataPointer = (IntPtr)(&MinWorkerThreads);
            data[0].Size = sizeof(ushort);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&MaxWorkerThreads);
            data[1].Size = sizeof(ushort);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&MinIOCompletionThreads);
            data[2].Size = sizeof(ushort);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&MaxIOCompletionThreads);
            data[3].Size = sizeof(ushort);
            data[3].Reserved = 0;
            data[4].DataPointer = (IntPtr)(&ClrInstanceID);
            data[4].Size = sizeof(ushort);
            data[4].Reserved = 0;
            WriteEventCore(59, 5, data);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(301, Level = EventLevel.Verbose, Message = Messages.WaitHandleWaitStart, Task = Tasks.WaitHandleWait, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.WaitHandleKeyword)]
        private unsafe void WaitHandleWaitStart(
            WaitHandleWaitSourceMap WaitSource,
            nint AssociatedObjectID,
            ushort ClrInstanceID = DefaultClrInstanceId)
        {
            Debug.Assert(IsEnabled(EventLevel.Verbose, Keywords.WaitHandleKeyword));

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (nint)(&WaitSource);
            data[0].Size = sizeof(WaitHandleWaitSourceMap);
            data[0].Reserved = 0;
            data[1].DataPointer = (nint)(&AssociatedObjectID);
            data[1].Size = nint.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (nint)(&ClrInstanceID);
            data[2].Size = sizeof(ushort);
            data[2].Reserved = 0;
            WriteEventCore(301, 3, data);
        }

#pragma warning disable CS8500
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public unsafe void WaitHandleWaitStart(
            WaitHandleWaitSourceMap waitSource = WaitHandleWaitSourceMap.Unknown,
            object? associatedObject = null) =>
            WaitHandleWaitStart(waitSource, *(nint*)&associatedObject);
#pragma warning restore CS8500

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = "Parameters to this method are primitive and are trimmer safe")]
        [Event(302, Level = EventLevel.Verbose, Message = Messages.WaitHandleWaitStop, Task = Tasks.WaitHandleWait, Opcode = EventOpcode.Stop, Version = 0, Keywords = Keywords.WaitHandleKeyword)]
        public unsafe void WaitHandleWaitStop(ushort ClrInstanceID = DefaultClrInstanceId)
        {
            Debug.Assert(IsEnabled(EventLevel.Verbose, Keywords.WaitHandleKeyword));

            EventData* data = stackalloc EventData[1];
            data[0].DataPointer = (nint)(&ClrInstanceID);
            data[0].Size = sizeof(ushort);
            data[0].Reserved = 0;
            WriteEventCore(302, 1, data);
        }
    }
}
