// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    internal partial class NativeRuntimeEventSource
    {
        public static class Keywords // this name and visibility is important for EventSource
        {
            public const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
            public const EventKeywords ThreadTransferKeyword = (EventKeywords)0x80000000;
        }

        [Event(50, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadStart(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount,
            ushort ClrInstanceID)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }

            WriteThreadEvent(50, ActiveWorkerThreadCount);
        }

        [Event(51, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = EventOpcode.Stop, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public void ThreadPoolWorkerThreadStop(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount,
            ushort ClrInstanceID)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }

            WriteThreadEvent(51, ActiveWorkerThreadCount);
        }

        [Event(54, Level = EventLevel.Informational, Message = Messages.WorkerThreadAdjustmentSample, Task = Tasks.ThreadPoolWorkerThreadAdjustment, Opcode = Opcodes.Sample, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkerThreadAdjustmentSample(
            double Throughput,
            ushort ClrInstanceID)
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
            ushort ClrInstanceID)
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

        [Event(57, Level = EventLevel.Informational, Message = Messages.WorkerThread, Task = Tasks.ThreadPoolWorkerThread, Opcode = Opcodes.Wait, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolWorkerThreadWait(
            uint ActiveWorkerThreadCount,
            uint RetiredWorkerThreadCount,
            ushort ClrInstanceID)
        {
            if (!IsEnabled(EventLevel.Informational, Keywords.ThreadingKeyword))
            {
                return;
            }

            WriteThreadEvent(57, ActiveWorkerThreadCount);
        }

        [Event(63, Level = EventLevel.Verbose, Message = Messages.IOEnqueue, Task = Tasks.ThreadPool, Opcode = Opcodes.IOEnqueue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIOEnqueue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            bool MultiDequeues,
            ushort ClrInstanceID)
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

        [Event(64, Level = EventLevel.Verbose, Message = Messages.IO, Task = Tasks.ThreadPool, Opcode = Opcodes.IODequeue, Version = 0, Keywords = Keywords.ThreadingKeyword | Keywords.ThreadTransferKeyword)]
        private unsafe void ThreadPoolIODequeue(
            IntPtr NativeOverlapped,
            IntPtr Overlapped,
            ushort ClrInstanceID)
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

        [Event(60, Level = EventLevel.Verbose, Message = Messages.WorkingThreadCount, Task = Tasks.ThreadPoolWorkingThreadCount, Opcode = EventOpcode.Start, Version = 0, Keywords = Keywords.ThreadingKeyword)]
        public unsafe void ThreadPoolWorkingThreadCount(uint Count, ushort ClrInstanceID)
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
