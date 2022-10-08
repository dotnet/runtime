// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Tracing
{
    [EventSource(Guid = "8E9F5090-2D75-4d03-8A81-E5AFBF85DAF1", Name = "System.Diagnostics.Eventing.FrameworkEventSource")]
    [EventSourceAutoGenerate]
    internal sealed partial class FrameworkEventSource : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";
        public static readonly FrameworkEventSource Log = new FrameworkEventSource();

        // Keyword definitions.  These represent logical groups of events that can be turned on and off independently
        // Often each task has a keyword, but where tasks are determined by subsystem, keywords are determined by
        // usefulness to end users to filter.  Generally users don't mind extra events if they are not high volume
        // so grouping low volume events together in a single keywords is OK (users can post-filter by task if desired)
        public static class Keywords
        {
            public const EventKeywords ThreadPool = (EventKeywords)0x0002;
            public const EventKeywords ThreadTransfer = (EventKeywords)0x0010;
        }

        /// <summary>ETW tasks that have start/stop events.</summary>
        public static class Tasks // this name is important for EventSource
        {
            /// <summary>Send / Receive - begin transfer/end transfer</summary>
            public const EventTask ThreadTransfer = (EventTask)3;
        }

        // Parameterized constructor to block initialization and ensure the EventSourceGenerator is creating the default constructor
        // as you can't make a constructor partial.
        private FrameworkEventSource(int _) { }

        // optimized for common signatures (used by the ThreadTransferSend/Receive events)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string? arg3, bool arg4, int arg5, int arg6)
        {
            if (IsEnabled())
            {
                arg3 ??= "";
                fixed (char* string3Bytes = arg3)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[6];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[2].Reserved = 0;
                    descrs[3].DataPointer = (IntPtr)(&arg4);
                    descrs[3].Size = 4;
                    descrs[3].Reserved = 0;
                    descrs[4].DataPointer = (IntPtr)(&arg5);
                    descrs[4].Size = 4;
                    descrs[4].Reserved = 0;
                    descrs[5].DataPointer = (IntPtr)(&arg6);
                    descrs[5].Size = 4;
                    descrs[5].Reserved = 0;
                    WriteEventCore(eventId, 6, descrs);
                }
            }
        }

        // optimized for common signatures (used by the ThreadTransferSend/Receive events)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, long arg1, int arg2, string? arg3)
        {
            if (IsEnabled())
            {
                arg3 ??= "";
                fixed (char* string3Bytes = arg3)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[3];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[2].Reserved = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        [Event(30, Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool | Keywords.ThreadTransfer)]
        public void ThreadPoolEnqueueWork(long workID)
        {
            WriteEvent(30, workID);
        }

        // The object's current location in memory was being used before. Since objects can be moved, it may be difficult to
        // associate Enqueue/Dequeue events with the object's at-the-time location in memory, the ETW listeners would have to
        // know specifics about the events and track GC movements to associate events. The hash code is a stable value and
        // easier to use for association, though there may be collisions.
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolEnqueueWorkObject(object workID) => ThreadPoolEnqueueWork(workID.GetHashCode());

        [Event(31, Level = EventLevel.Verbose, Keywords = Keywords.ThreadPool | Keywords.ThreadTransfer)]
        public void ThreadPoolDequeueWork(long workID)
        {
            WriteEvent(31, workID);
        }

        // The object's current location in memory was being used before. Since objects can be moved, it may be difficult to
        // associate Enqueue/Dequeue events with the object's at-the-time location in memory, the ETW listeners would have to
        // know specifics about the events and track GC movements to associate events. The hash code is a stable value and
        // easier to use for association, though there may be collisions.
        [NonEvent]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ThreadPoolDequeueWorkObject(object workID) => ThreadPoolDequeueWork(workID.GetHashCode());

        // id -   represents a correlation ID that allows correlation of two activities, one stamped by
        //        ThreadTransferSend, the other by ThreadTransferReceive
        // kind - identifies the transfer: values below 64 are reserved for the runtime. Currently used values:
        //        1 - managed Timers ("roaming" ID)
        //        2 - managed async IO operations (FileStream, PipeStream, a.o.)
        //        3 - WinRT dispatch operations
        // info - any additional information user code might consider interesting
        // intInfo1/2 - any additional integer information user code might consider interesting
        [Event(150, Level = EventLevel.Informational, Keywords = Keywords.ThreadTransfer, Task = Tasks.ThreadTransfer, Opcode = EventOpcode.Send)]
        public void ThreadTransferSend(long id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2)
        {
            WriteEvent(150, id, kind, info, multiDequeues, intInfo1, intInfo2);
        }

        // id - is a managed object's hash code
        [NonEvent]
        public void ThreadTransferSendObj(object id, int kind, string info, bool multiDequeues, int intInfo1, int intInfo2) =>
            ThreadTransferSend(id.GetHashCode(), kind, info, multiDequeues, intInfo1, intInfo2);

        // id -   represents a correlation ID that allows correlation of two activities, one stamped by
        //        ThreadTransferSend, the other by ThreadTransferReceive
        //    -   The object's current location in memory was being used before. Since objects can be moved, it may be difficult to
        //        associate Enqueue/Dequeue events with the object's at-the-time location in memory, the ETW listeners would have to
        //        know specifics about the events and track GC movements to associate events. The hash code is a stable value and
        //        easier to use for association, though there may be collisions.
        // kind - identifies the transfer: values below 64 are reserved for the runtime. Currently used values:
        //        1 - managed Timers ("roaming" ID)
        //        2 - managed async IO operations (FileStream, PipeStream, a.o.)
        //        3 - WinRT dispatch operations
        // info - any additional information user code might consider interesting
        [Event(151, Level = EventLevel.Informational, Keywords = Keywords.ThreadTransfer, Task = Tasks.ThreadTransfer, Opcode = EventOpcode.Receive)]
        public void ThreadTransferReceive(long id, int kind, string? info)
        {
            WriteEvent(151, id, kind, info);
        }

        // id - is a managed object. it gets translated to the object's address.
        //    - The object's current location in memory was being used before. Since objects can be moved, it may be difficult to
        //      associate Enqueue/Dequeue events with the object's at-the-time location in memory, the ETW listeners would have to
        //      know specifics about the events and track GC movements to associate events. The hash code is a stable value and
        //      easier to use for association, though there may be collisions.
        [NonEvent]
        public void ThreadTransferReceiveObj(object id, int kind, string? info) =>
            ThreadTransferReceive(id.GetHashCode(), kind, info);
    }
}
