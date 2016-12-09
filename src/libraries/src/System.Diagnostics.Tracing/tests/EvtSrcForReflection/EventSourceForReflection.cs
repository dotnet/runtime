// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvtSrcForReflection
{
    /// <summary>
    /// This class is used for validating two main aspects of EventSource:
    ///   1. Generating manifests from event source types loaded in the reflection context
    ///   2. Generating manifests for event source types that use localization resources
    /// </summary>
    [EventSource(LocalizationResources = "EvtSrcForReflection.EsrResources")]
    public class EventSourceForReflection : EventSource
    {
        public class Keywords
        {
            public const EventKeywords HasNoArgs = (EventKeywords)0x0001;
            public const EventKeywords HasIntArgs = (EventKeywords)0x0002;
            public const EventKeywords HasLongArgs = (EventKeywords)0x0004;
            public const EventKeywords HasStringArgs = (EventKeywords)0x0008;
            public const EventKeywords HasDateTimeArgs = (EventKeywords)0x0010;
            public const EventKeywords HasEnumArgs = (EventKeywords)0x0020;
            public const EventKeywords Transfer = (EventKeywords)0x0040;
        }

        public class Tasks
        {
            public const EventTask WorkItem = (EventTask)1;
            public const EventTask WorkItemBad = (EventTask)2;
        }

        public class Opcodes
        {
            public const EventOpcode Opcode1 = (EventOpcode)11;
            public const EventOpcode Opcode2 = (EventOpcode)12;
        }

        [Event(1, Keywords = Keywords.HasNoArgs, Level = EventLevel.Informational)]
        public void Event0() { WriteEvent(1); }

        [Event(2, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventI(int arg1) { WriteEvent(2, arg1); }

        [Event(3, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventII(int arg1, int arg2) { WriteEvent(3, arg1, arg2); }

        [Event(4, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventIII(int arg1, int arg2, int arg3) { WriteEvent(4, arg1, arg2, arg3); }

        [Event(5, Keywords = Keywords.HasLongArgs, Level = EventLevel.Informational)]
        public void EventL(long arg1) { WriteEvent(5, arg1); }

        [Event(6, Keywords = Keywords.HasLongArgs, Level = EventLevel.Informational)]
        public void EventLL(long arg1, long arg2) { WriteEvent(6, arg1, arg2); }

        [Event(7, Keywords = Keywords.HasLongArgs, Level = EventLevel.Informational)]
        public void EventLLL(long arg1, long arg2, long arg3) { WriteEvent(7, arg1, arg2, arg3); }

        [Event(8, Keywords = Keywords.HasStringArgs, Level = EventLevel.Informational)]
        public void EventS(string arg1) { WriteEvent(8, arg1); }

        [Event(9, Keywords = Keywords.HasStringArgs, Level = EventLevel.Informational)]
        public void EventSS(string arg1, string arg2) { WriteEvent(9, arg1, arg2); }

        [Event(10, Keywords = Keywords.HasStringArgs, Level = EventLevel.Informational)]
        public void EventSSS(string arg1, string arg2, string arg3) { WriteEvent(10, arg1, arg2, arg3); }

        [Event(11, Keywords = Keywords.HasStringArgs | Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventSI(string arg1, int arg2) { WriteEvent(11, arg1, arg2); }

        [Event(12, Keywords = Keywords.HasStringArgs | Keywords.HasLongArgs, Level = EventLevel.Informational)]
        public void EventSL(string arg1, long arg2) { WriteEvent(12, arg1, arg2); }

        [Event(13, Keywords = Keywords.HasStringArgs | Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventSII(string arg1, int arg2, int arg3) { WriteEvent(13, arg1, arg2, arg3); }

        [Event(14, Keywords = Keywords.HasStringArgs, Level = EventLevel.Informational)]
        public void Message(string arg1) { WriteEvent(14, arg1); }

        [Event(15, Keywords = Keywords.HasNoArgs, Level = EventLevel.Informational, Task = Tasks.WorkItem)]
        public void StartTrackingActivity() { WriteEvent(15); }

        [Event(16, Keywords = Keywords.Transfer | Keywords.HasStringArgs, Opcode = EventOpcode.Send, Task = Tasks.WorkItem)]
        public unsafe void LogTaskScheduled(Guid RelatedActivityId, string message)
        {
            unsafe
            {
                if (message == null) message = "";
                fixed (char* string1Bytes = message)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[1];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((message.Length + 1) * 2);
                    WriteEventWithRelatedActivityIdCore(16, &RelatedActivityId, 1, descrs);
                }
            }
        }

        [Event(17, Keywords = Keywords.HasStringArgs | Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void SlowerHelper(int arg1, string arg2)
        { if (IsEnabled()) WriteEvent(17, arg1, arg2); }


        [Event(18, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventEnum(MyColor x) { WriteEvent(18, (int)x); }

        [Event(19, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventEnum1(MyColor x) { WriteEvent(19, x); }

        [Event(20, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventFlags(MyFlags x) { WriteEvent(20, (int)x); }

        [Event(21, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventFlags1(MyFlags x) { WriteEvent(21, x); }

        [Event(22, Keywords = Keywords.Transfer | Keywords.HasStringArgs, Opcode = EventOpcode.Send, Task = Tasks.WorkItemBad)]
        public void LogTaskScheduledBad(Guid RelatedActivityId, string message)
        {
            unsafe
            {
                if (message == null) message = "";
                fixed (char* string1Bytes = message)
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[1];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((message.Length + 1) * 2);
                    WriteEventWithRelatedActivityIdCore(22, &RelatedActivityId, 1, descrs);
                }
            }
        }

        [Event(23, Keywords = Keywords.HasDateTimeArgs,
         Message = "DateTime passed in: <{0}>",
         Opcode = Opcodes.Opcode1,
         Level = EventLevel.Informational)]
        public void EventDateTime(DateTime dt) { WriteEvent(23, dt); }

        public void EventNoAttributes(DateTime dt) { WriteEvent(24, dt); }

        [Event(25, Keywords = Keywords.HasNoArgs, Level = EventLevel.Informational)]
        public void EventWithManyTypeArgs(string msg, long l, uint ui, UInt64 ui64, char ch,
                                          byte b, sbyte sb, short sh, ushort ush,
                                          float f, double d, Guid guid)
        {
            WriteEvent(25, msg, l, ui, ui64, ch, b, sb, sh, ush, f, d, guid);
        }

        [NonEvent]
        public void NonEvent()
        { EventDateTime(DateTime.Now); }

        [Event(26, Level = EventLevel.Informational, Message="msg=\"{0}\", n={1}!")]
        public void EventWithEscapingMessage(string msg, int n)
        { WriteEvent(26, msg, n); }

        [Event(27, Level = EventLevel.Informational, Message = "{{msg}}=\"{0}!\" percentage={1}%")]
        public void EventWithMoreEscapingMessage(string msg, int percentage)
        { WriteEvent(27, msg, percentage); }
    }

    public enum MyColor
    {
        Red,
        Blue,
        Green,
    }

    [Flags]
    public enum MyFlags
    {
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
    }
}
