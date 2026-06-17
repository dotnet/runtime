// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace System.Runtime.CompilerServices
{
    /// <summary>Provides an event source for tracing async execution.</summary>
    [EventSource(Name = "System.Runtime.CompilerServices.AsyncProfilerEventSource")]
    internal sealed partial class AsyncProfilerEventSource : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";

        public static readonly AsyncProfilerEventSource Log = new AsyncProfilerEventSource();

        public static class Keywords // this name is important for EventSource
        {
            public const EventKeywords CreateRuntimeAsyncContext = (EventKeywords)0x1;
            public const EventKeywords ResumeRuntimeAsyncContext = (EventKeywords)0x2;
            public const EventKeywords SuspendRuntimeAsyncContext = (EventKeywords)0x4;
            public const EventKeywords CompleteRuntimeAsyncContext = (EventKeywords)0x8;
            public const EventKeywords UnwindRuntimeAsyncException = (EventKeywords)0x10;
            public const EventKeywords CreateRuntimeAsyncCallstack = (EventKeywords)0x20;
            public const EventKeywords ResumeRuntimeAsyncCallstack = (EventKeywords)0x40;
            public const EventKeywords SuspendRuntimeAsyncCallstack = (EventKeywords)0x80;
            public const EventKeywords ResumeRuntimeAsyncMethod = (EventKeywords)0x100;
            public const EventKeywords CompleteRuntimeAsyncMethod = (EventKeywords)0x200;
            public const EventKeywords CreateStateMachineAsyncContext = (EventKeywords)0x400;
            public const EventKeywords ResumeStateMachineAsyncContext = (EventKeywords)0x800;
            public const EventKeywords CompleteStateMachineAsyncContext = (EventKeywords)0x1000;
            public const EventKeywords UnwindStateMachineAsyncException = (EventKeywords)0x2000;
            public const EventKeywords ResumeStateMachineAsyncCallstack = (EventKeywords)0x4000;
            public const EventKeywords ResumeStateMachineAsyncMethod = (EventKeywords)0x8000;
            public const EventKeywords CompleteStateMachineAsyncMethod = (EventKeywords)0x10000;
        }

        public const EventKeywords AsyncEventKeywords =
            Keywords.CreateRuntimeAsyncContext |
            Keywords.ResumeRuntimeAsyncContext |
            Keywords.SuspendRuntimeAsyncContext |
            Keywords.CompleteRuntimeAsyncContext |
            Keywords.CreateRuntimeAsyncCallstack |
            Keywords.ResumeRuntimeAsyncCallstack |
            Keywords.SuspendRuntimeAsyncCallstack |
            Keywords.UnwindRuntimeAsyncException |
            Keywords.ResumeRuntimeAsyncMethod |
            Keywords.CompleteRuntimeAsyncMethod |
            Keywords.CreateStateMachineAsyncContext |
            Keywords.ResumeStateMachineAsyncContext |
            Keywords.CompleteStateMachineAsyncContext |
            Keywords.UnwindStateMachineAsyncException |
            Keywords.ResumeStateMachineAsyncCallstack |
            Keywords.ResumeStateMachineAsyncMethod |
            Keywords.CompleteStateMachineAsyncMethod;


        public const int FlushCommand = 1;

        //----------------------- Event IDs (must be unique) -----------------------
        public const int ASYNC_EVENTS_ID = 1;

        //-----------------------------------------------------------------------------------
        //
        // Events
        //
        [Event(
            ASYNC_EVENTS_ID,
            Version = 1,
            Opcode = EventOpcode.Info,
            Level = EventLevel.Informational,
            Keywords = AsyncEventKeywords,
            Message = "")]
        public void AsyncEvents(byte[] buffer)
        {
            AsyncEvents(buffer.AsSpan());
        }

        [NonEvent]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern", Justification = EventSourceSuppressMessage)]
        public void AsyncEvents(ReadOnlySpan<byte> buffer)
        {
            unsafe
            {
                fixed (byte* pBuffer = buffer)
                {
                    int length = buffer.Length;
                    EventData* eventPayload = stackalloc EventData[2];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&length));
                    eventPayload[0].Reserved = 0;
                    eventPayload[1].Size = sizeof(byte) * length;
                    eventPayload[1].DataPointer = length != 0 ? ((IntPtr)pBuffer) : ((IntPtr)(&length));
                    eventPayload[1].Reserved = 0;
                    WriteEventCore(ASYNC_EVENTS_ID, 2, eventPayload);
                }
            }
        }

        /// <summary>
        /// Get callbacks when the ETW sends us commands
        /// </summary>
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == (EventCommand)FlushCommand || command.Command == EventCommand.SendManifest)
            {
                AsyncProfiler.Config.CaptureState();
                return;
            }

            EventKeywords keywords = m_matchAnyKeyword;
            if (keywords == 0)
            {
                keywords = AsyncEventKeywords;
            }

            AsyncProfiler.Config.Update(m_level, keywords);
        }
    }
}
