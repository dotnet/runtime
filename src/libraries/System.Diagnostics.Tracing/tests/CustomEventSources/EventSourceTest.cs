// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Diagnostics;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif

// We wish to test both Microsoft.Diagnostics.Tracing (Nuget)
// and System.Diagnostics.Tracing (Framework), we use this Ifdef make each kind 

namespace SdtEventSources
{
    /// <summary>
    /// A sample Event source. The Guid and Name attributes are "idempotent", i.e. they 
    /// don't change the default computed by EventSource; they're specified here just to 
    /// increase the code coverage.
    /// </summary>
    [EventSource(Guid = "69e2aa3e-083b-5014-cad4-3e511a0b94cf", Name = "EventSourceTest")]
    public sealed class EventSourceTest : EventSource
    {
        public EventSourceTest(bool useSelfDescribingEvents = false)
            : base(true)
        { }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            Debug.WriteLine(String.Format("EventSourceTest: Got Command {0}", command.Command));
            Debug.WriteLine("  Args: " + string.Join(", ", command.Arguments.Select((pair) => string.Format("{0} -> {1}", pair.Key, pair.Value))));
        }

        [Event(1, Keywords = Keywords.HasNoArgs, Level = EventLevel.Informational)]
        public void Event0() { WriteEvent(1); }

        [Event(2, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventI(int arg1) { WriteEvent(2, arg1); }

        [Event(3, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventII(int arg1, int arg2) { WriteEvent(3, arg1, arg2); }

        [Event(4, Keywords = Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void EventIII(int arg1, int arg2, int arg3 = 12) { WriteEvent(4, arg1, arg2, arg3); }

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
        
        [Event(17, Keywords = Keywords.Transfer | Keywords.HasStringArgs, Opcode = EventOpcode.Send, Task = Tasks.WorkItem)]
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
                    WriteEventWithRelatedActivityIdCore(17, &RelatedActivityId, 1, descrs);
                }
            }
        }

        [Event(18, Keywords = Keywords.HasStringArgs | Keywords.HasIntArgs, Level = EventLevel.Informational)]
        public void SlowerHelper(int arg1, string arg2)
        { if (IsEnabled()) WriteEvent(18, arg1, arg2); }


        [Event(19, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventEnum(MyColor x) { WriteEvent(19, (int)x); }

        [Event(20, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventEnum1(MyColor x) { WriteEvent(20, x); }

        [Event(21, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventFlags(MyFlags x) { WriteEvent(21, (int)x); }

        [Event(22, Keywords = Keywords.HasEnumArgs, Level = EventLevel.Informational)]
        public void EventFlags1(MyFlags x) { WriteEvent(22, x); }

        [Event(23, Keywords = Keywords.Transfer | Keywords.HasStringArgs, Opcode = EventOpcode.Send, Task = Tasks.WorkItemBad)]
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
                    WriteEventWithRelatedActivityIdCore(23, &RelatedActivityId, 1, descrs);
                }
            }
        }

        // v4.5 does not support DateTime (until v4.5.1)
        [Event(24, Keywords = Keywords.HasDateTimeArgs,
         Message = "DateTime passed in: <{0}>",
         Opcode = Opcodes.Opcode1,
         Task = Tasks.WorkDateTime,
         Level = EventLevel.Informational)]
        public void EventDateTime(DateTime dt) { WriteEvent(24, dt); }

        [Event(25, Keywords = Keywords.HasNoArgs, Level = EventLevel.Informational)]
        public void EventWithManyTypeArgs(string msg, long l, uint ui, UInt64 ui64,
                                          byte b, sbyte sb, short sh, ushort ush,
                                          float f, double d, Guid guid)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.HasNoArgs))
                // 4.5 EventSource does not support "Char" type
                WriteEvent(25, msg, l, ui, ui64, b, sb, sh, ush, f, d, guid);
        }

        [Event(26)]
        public void EventWith7Strings(string s0, string s1, string s2, string s3, string s4, string s5, string s6)
        { WriteEvent(26, s0, s1, s2, s3, s4, s5, s6); }

        [Event(27)]
        public void EventWith9Strings(string s0, string s1, string s2, string s3, string s4, string s5, string s6, string s7, string s8)
        { WriteEvent(27, s0, s1, s2, s3, s4, s5, s6, s7, s8); }

        [Event(28, Keywords = Keywords.Transfer | Keywords.HasNoArgs)]
        public void LogTransferNoOpcode(Guid RelatedActivityId)
        {
            unsafe
            {
                WriteEventWithRelatedActivityIdCore(28, &RelatedActivityId, 0, null);
            }
        }

        [Event(29, Keywords = Keywords.Transfer | Keywords.HasNoArgs, Level = EventLevel.Informational, Opcode = EventOpcode.Send, Task = Tasks.WorkManyArgs)]
        public void EventWithXferManyTypeArgs(Guid RelatedActivityId, long l, uint ui, UInt64 ui64, char ch,
                                          byte b, sbyte sb, short sh, ushort ush,
                                          float f, double d, Guid guid)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.HasNoArgs))
            {
                unsafe
                {
                    EventSource.EventData* descrs = stackalloc EventSource.EventData[11];
                    descrs[0].DataPointer = (IntPtr)(&l);
                    descrs[0].Size = 8;
                    descrs[1].DataPointer = (IntPtr)(&ui);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&ui64);
                    descrs[2].Size = 8;
                    descrs[3].DataPointer = (IntPtr)(&ch);
                    descrs[3].Size = 2;
                    descrs[4].DataPointer = (IntPtr)(&b);
                    descrs[4].Size = 1;
                    descrs[5].DataPointer = (IntPtr)(&sb);
                    descrs[5].Size = 1;
                    descrs[6].DataPointer = (IntPtr)(&sh);
                    descrs[6].Size = 2;
                    descrs[7].DataPointer = (IntPtr)(&ush);
                    descrs[7].Size = 2;
                    descrs[8].DataPointer = (IntPtr)(&f);
                    descrs[8].Size = 4;
                    descrs[9].DataPointer = (IntPtr)(&d);
                    descrs[9].Size = 8;
                    descrs[10].DataPointer = (IntPtr)(&guid);
                    descrs[10].Size = 16;
                    WriteEventWithRelatedActivityIdCore(29, &RelatedActivityId, 11, descrs);
                }
            }
        }

        [Event(30)]
        // 4.5 EventSource does not support IntPtr args
        public void EventWithWeirdArgs(IntPtr iptr, bool b, MyLongEnum le /*, decimal dec*/)
        { WriteEvent(30, iptr, b, le /*, dec*/); }

        [Event(31, Keywords = Keywords.Transfer | Keywords.HasNoArgs, Level = EventLevel.Informational, Opcode = EventOpcode.Send, Task = Tasks.WorkWeirdArgs)]
        public void EventWithXferWeirdArgs(Guid RelatedActivityId, IntPtr iptr, bool b, MyLongEnum le /*, decimal dec */)
        {
            unsafe
            {
                EventSource.EventData* descrs = stackalloc EventSource.EventData[4];
                descrs[0].DataPointer = (IntPtr)(&iptr);
                descrs[0].Size = IntPtr.Size;
                int boolval = b ? 1 : 0;
                descrs[1].DataPointer = (IntPtr)(&boolval);
                descrs[1].Size = 4;
                descrs[2].DataPointer = (IntPtr)(&le);
                descrs[2].Size = 8;
                // descrs[3].DataPointer = (IntPtr)(&dec);
                // descrs[3].Size = 16;
                WriteEventWithRelatedActivityIdCore(31, &RelatedActivityId, 3 /*4*/, descrs);
            }
        }

        [NonEvent]
        public void NonEvent()
        { EventWithString(DateTime.Now.ToString()); }

        // The above produces different results on 4.5 vs. 4.5.1. Skip the test for those EventSources
        [Event(32, Level = EventLevel.Informational, Message = "msg={0}, n={1}!")]
        public void EventWithEscapingMessage(string msg, int n)
        { WriteEvent(32, msg, n); }

        // The above produces different results on 4.5 vs. 4.5.1. Skip the test for those EventSources
        [Event(33, Level = EventLevel.Informational, Message = "{{msg}}={0}! percentage={1}%")]
        public void EventWithMoreEscapingMessage(string msg, int percentage)
        { WriteEvent(33, msg, percentage); }

        [Event(36, Level = EventLevel.Informational, Message = "Int arg after byte ptr: {2}")]
        public unsafe void EventWithIncorrectNumberOfParameters(string message, string path = "", int line = 0)
        {
            string text = string.Concat("{", path, ":", line, "}", message);
            WriteEvent(36, text);
        }


        [Event(39, Level = EventLevel.Informational, Message = "int int string event")]
        public void EventWithIntIntString(int i1, int i2, string str)
        { WriteEvent(39, i1, i2, str); }

        [Event(40, Level = EventLevel.Informational, Message = "int long string")]
        public void EventWithIntLongString(int i1, long l1, string str)
        { WriteEvent(40, i1, l1, str); }

        [Event(41)]
        public void EventWithString(string str)
        {
            this.WriteEvent(41, str);
        }

        [Event(42)]
        public void EventWithIntAndString(int i, string str)
        {
            this.WriteEvent(42, i, str);
        }

        [Event(43)]
        public void EventWithLongAndString(long l, string str)
        {
            this.WriteEvent(43, l, str);
        }

        [Event(44)]
        public void EventWithStringAndInt(string str, int i)
        {
            this.WriteEvent(44, str, i);
        }

        [Event(45)]
        public void EventWithStringAndIntAndInt(string str, int i, int j)
        {
            this.WriteEvent(45, str, i, j);
        }

        [Event(46)]
        public void EventWithStringAndLong(string str, long l)
        {
            this.WriteEvent(46, str, l);
        }

        [Event(47)]
        public void EventWithStringAndString(string str, string str2)
        {
            this.WriteEvent(47, str, str2);
        }

        [Event(48)]
        public void EventWithStringAndStringAndString(string str, string str2, string str3)
        {
            this.WriteEvent(48, str, str2, str3);
        }

        [Event(49)]
        public void EventVarArgsWithString(int i, int j, int k, string str)
        {
            this.WriteEvent(49, i, j, k, str);
        }

        /// <summary>
        /// This event, combined with the one after it, test whether an Event named "Foo" and one named
        /// "FooStart" can coexist.
        /// </summary>
        [Event(50)]
        public void EventNamedEvent()
        {
            this.WriteEvent(50);
        }

        /// <summary>
        /// This event, combined with the one before it, test whether an Event named "Foo" and one named
        /// "FooStart" can coexist.
        /// </summary>
        [Event(51)]
        public void EventNamedEventStart()
        {
            this.WriteEvent(51);
        }

        [Event(52)]
        public void EventWithByteArray(byte[] arr)
        {
            this.WriteEvent(52, arr);
        }

        #region Keywords / Tasks /Opcodes / Channels
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
            public const EventTask WorkManyArgs = (EventTask)3;
            public const EventTask WorkWeirdArgs = (EventTask)4;
            public const EventTask WorkDateTime = (EventTask)5;
        }

        public class Opcodes
        {
            public const EventOpcode Opcode1 = (EventOpcode)11;
            public const EventOpcode Opcode2 = (EventOpcode)12;
        }
        #endregion
    }

    public enum MyColor
    {
        Red,
        Blue,
        Green,
    }

    public enum MyLongEnum : long
    {
        LongVal1 = (long)0x13 << 32,
        LongVal2 = (long)0x20 << 32,
    }

    [Flags]
    public enum MyFlags
    {
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
    }

    public sealed class EventSourceNoAttribute : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void Event0() { WriteEvent(1); }

        [Event(2, Level = EventLevel.Informational)]
        public void EventI(int arg1) { WriteEvent(2, arg1); }

        [Event(3, Level = EventLevel.Informational)]
        public void EventII(int arg1, int arg2) { WriteEvent(3, arg1, arg2); }

        [Event(4, Level = EventLevel.Informational)]
        public void EventIII(int arg1, int arg2, int arg3 = 12) { WriteEvent(4, arg1, arg2, arg3); }

        [Event(5, Level = EventLevel.Informational)]
        public void EventL(long arg1) { WriteEvent(5, arg1); }

        [Event(6, Level = EventLevel.Informational)]
        public void EventLL(long arg1, long arg2) { WriteEvent(6, arg1, arg2); }

        [Event(7, Level = EventLevel.Informational)]
        public void EventLLL(long arg1, long arg2, long arg3) { WriteEvent(7, arg1, arg2, arg3); }

        [Event(8, Level = EventLevel.Informational)]
        public void EventS(string arg1) { WriteEvent(8, arg1); }

        [Event(9, Level = EventLevel.Informational)]
        public void EventSS(string arg1, string arg2) { WriteEvent(9, arg1, arg2); }

        [Event(10, Level = EventLevel.Informational)]
        public void EventSSS(string arg1, string arg2, string arg3) { WriteEvent(10, arg1, arg2, arg3); }

        [Event(11, Level = EventLevel.Informational)]
        public void EventSI(string arg1, int arg2) { WriteEvent(11, arg1, arg2); }

        [Event(12, Level = EventLevel.Informational)]
        public void EventSL(string arg1, long arg2) { WriteEvent(12, arg1, arg2); }

        [Event(13, Level = EventLevel.Informational)]
        public void EventSII(string arg1, int arg2, int arg3) { WriteEvent(13, arg1, arg2, arg3); }

        [Event(14, Level = EventLevel.Informational)]
        public void Message(string arg1) { WriteEvent(14, arg1); }

        [Event(15, Level = EventLevel.Informational)]
        public void StartTrackingActivity() { WriteEvent(15); }

        // Make sure this is before any #if so it gets a deterministic ID
        public void EventNoAttributes(string s) { WriteEvent(16, s); }
    }
}
