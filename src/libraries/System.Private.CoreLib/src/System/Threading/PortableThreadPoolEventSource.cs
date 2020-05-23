// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Threading
{
    [EventSource(Name = "Microsoft-Windows-DotNETRuntime", Guid = "e13c0d23-ccbc-4e12-931b-d9cc2eee27e4")]
    internal sealed class PortableThreadPoolEventSource : EventSource
    {
        // This value does not seem to be used, leaving it as zero for now. It may be useful for a scenario that may involve
        // multiple instances of the runtime within the same process, but then it seems unlikely that both instances' thread
        // pools would be in moderate use.
        private const short ClrInstanceId = 0;

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

        public static class Keywords // this name and visibility is important for EventSource
        {
            public const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
            public const EventKeywords ThreadTransferKeyword = (EventKeywords)0x80000000;
        }

        private PortableThreadPoolEventSource()
            : base(
                  new Guid(0xe13c0d23, 0xccbc, 0x4e12, 0x93, 0x1b, 0xd9, 0xcc, 0x2e, 0xee, 0x27, 0xe4),
                  "Microsoft-Windows-DotNETRuntime")
        {
        }

        [NonEvent]
        private unsafe void WriteThreadEvent(int eventId, int numExistingThreads)
        {
            int retiredWorkerThreadCount = 0;
            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (IntPtr)(&numExistingThreads);
            data[0].Size = sizeof(int);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&retiredWorkerThreadCount);
            data[1].Size = sizeof(int);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&clrInstanceId);
            data[2].Size = sizeof(short);
            data[2].Reserved = 0;
            WriteEventCore(eventId, 3, data);
        }

        [Event(50, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadStart(int numExistingThreads)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                WriteThreadEvent(50, numExistingThreads);
            }
        }

        [Event(51, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Stop, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public void ThreadPoolWorkerThreadStop(short numExistingThreads)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                WriteThreadEvent(51, numExistingThreads);
            }
        }

        [Event(57, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = Opcodes.Wait, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolWorkerThreadWait(short numExistingThreads)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                WriteThreadEvent(57, numExistingThreads);
            }
        }

        [Event(54, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentSample, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Sample, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentSample(double throughput)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }

            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[2];
            data[0].DataPointer = (IntPtr)(&throughput);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&clrInstanceId);
            data[1].Size = sizeof(short);
            data[1].Reserved = 0;
            WriteEventCore(54, 2, data);
        }

        [Event(55, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentAdjustment, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Adjustment, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentAdjustment(double averageThroughput, int newWorkerThreadCount, int stateOrTransition)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }

            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[4];
            data[0].DataPointer = (IntPtr)(&averageThroughput);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&newWorkerThreadCount);
            data[1].Size = sizeof(int);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&stateOrTransition);
            data[2].Size = sizeof(int);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&clrInstanceId);
            data[3].Size = sizeof(short);
            data[3].Reserved = 0;
            WriteEventCore(55, 4, data);
        }

        [Event(56, Level = EventLevel.Verbose, Message = Messages.WorkerThreadAdjustmentStats, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Stats, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentStats(double duration, double throughput, double threadWave, double throughputWave, double throughputErrorEstimate,
            double averageThroughputNoise, double ratio, double confidence, double currentControlSetting, ushort newThreadWaveMagnitude)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
                return;
            }

            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[11];
            data[0].DataPointer = (IntPtr)(&duration);
            data[0].Size = sizeof(double);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&throughput);
            data[1].Size = sizeof(double);
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&threadWave);
            data[2].Size = sizeof(double);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&throughputWave);
            data[3].Size = sizeof(double);
            data[3].Reserved = 0;
            data[4].DataPointer = (IntPtr)(&throughputErrorEstimate);
            data[4].Size = sizeof(double);
            data[4].Reserved = 0;
            data[5].DataPointer = (IntPtr)(&averageThroughputNoise);
            data[5].Size = sizeof(double);
            data[5].Reserved = 0;
            data[6].DataPointer = (IntPtr)(&ratio);
            data[6].Size = sizeof(double);
            data[6].Reserved = 0;
            data[7].DataPointer = (IntPtr)(&confidence);
            data[7].Size = sizeof(double);
            data[7].Reserved = 0;
            data[8].DataPointer = (IntPtr)(&currentControlSetting);
            data[8].Size = sizeof(double);
            data[8].Reserved = 0;
            data[9].DataPointer = (IntPtr)(&newThreadWaveMagnitude);
            data[9].Size = sizeof(ushort);
            data[9].Reserved = 0;
            data[10].DataPointer = (IntPtr)(&clrInstanceId);
            data[10].Size = sizeof(short);
            data[10].Reserved = 0;
            WriteEventCore(56, 11, data);
        }

        [Event(63, Level = EventLevel.Verbose, Message = Messages.IOEnqueue, Task = Tasks.ThreadPool, Opcode = Opcodes.IOEnqueue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void ThreadPoolIOEnqueue(IntPtr nativeOverlapped, IntPtr overlapped, bool multiDequeues)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
                return;
            }

            int multiDequeuesInt = Convert.ToInt32(multiDequeues);
            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[4];
            data[0].DataPointer = (IntPtr)(&nativeOverlapped);
            data[0].Size = IntPtr.Size;
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&overlapped);
            data[1].Size = IntPtr.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&multiDequeuesInt);
            data[2].Size = sizeof(int);
            data[2].Reserved = 0;
            data[3].DataPointer = (IntPtr)(&clrInstanceId);
            data[3].Size = sizeof(short);
            data[3].Reserved = 0;
            WriteEventCore(63, 4, data);
        }

        // TODO: This event is fired for minor compat with CoreCLR in this case. Consider removing this method and use
        // FrameworkEventSource's thread transfer send/receive events instead at callers.
        [NonEvent]
        public void ThreadPoolIOEnqueue(RegisteredWaitHandle registeredWaitHandle) =>
            ThreadPoolIOEnqueue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero, registeredWaitHandle.Repeating);

        [Event(64, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IODequeue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void ThreadPoolIODequeue(IntPtr nativeOverlapped, IntPtr overlapped)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword))
            {
                return;
            }

            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[3];
            data[0].DataPointer = (IntPtr)(&nativeOverlapped);
            data[0].Size = IntPtr.Size;
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&overlapped);
            data[1].Size = IntPtr.Size;
            data[1].Reserved = 0;
            data[2].DataPointer = (IntPtr)(&clrInstanceId);
            data[2].Size = sizeof(short);
            data[2].Reserved = 0;
            WriteEventCore(64, 3, data);
        }

        // TODO: This event is fired for minor compat with CoreCLR in this case. Consider removing this method and use
        // FrameworkEventSource's thread transfer send/receive events instead at callers.
        [NonEvent]
        public void ThreadPoolIODequeue(RegisteredWaitHandle registeredWaitHandle) =>
            ThreadPoolIODequeue((IntPtr)registeredWaitHandle.GetHashCode(), IntPtr.Zero);

        [Event(60, Level = EventLevel.Verbose, Message = Messages.WorkingThreadCount, Task = Tasks.ThreadPoolWorkingThreadCount, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkingThreadCount(short count)
        {
            if (!IsEnabled(EventLevel.Verbose, Keywords.ThreadingKeyword))
            {
                return;
            }

            int countInt = count;
            short clrInstanceId = ClrInstanceId;

            EventData* data = stackalloc EventData[2];
            data[0].DataPointer = (IntPtr)(&countInt);
            data[0].Size = sizeof(int);
            data[0].Reserved = 0;
            data[1].DataPointer = (IntPtr)(&clrInstanceId);
            data[1].Size = sizeof(short);
            data[1].Reserved = 0;
            WriteEventCore(60, 2, data);
        }

#pragma warning disable IDE1006 // Naming Styles
        public static readonly PortableThreadPoolEventSource Log = new PortableThreadPoolEventSource();
#pragma warning restore IDE1006 // Naming Styles
    }
}
