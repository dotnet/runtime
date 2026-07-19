// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Threading;

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
            public const EventKeywords SuspendStateMachineAsyncContext = (EventKeywords)0x1000;
            public const EventKeywords CompleteStateMachineAsyncContext = (EventKeywords)0x2000;
            public const EventKeywords UnwindStateMachineAsyncException = (EventKeywords)0x4000;
            public const EventKeywords ResumeStateMachineAsyncCallstack = (EventKeywords)0x8000;
            public const EventKeywords ResumeStateMachineAsyncMethod = (EventKeywords)0x10000;
            public const EventKeywords CompleteStateMachineAsyncMethod = (EventKeywords)0x20000;
            public const EventKeywords TraceIdChanged = (EventKeywords)0x40000;
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
            Keywords.SuspendStateMachineAsyncContext |
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
            // This blob carries every buffered payload, including trace-id records under the decoupled
            // transport, so its keyword must include TraceIdChanged; otherwise a trace-id-only session buffers
            // records that are then dropped here because the carrier's keyword is inactive.
            Keywords = AsyncEventKeywords | Keywords.TraceIdChanged,
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

        // --------------------------------------------------------------------------------------------
        // Synchronous trace-id timeline
        //
        // The synchronous change-points (Activity.Current transitions) are produced in
        // System.Diagnostics.DiagnosticSource and reach CoreLib through UnsafeAccessor:
        //   * AsyncProfilerTraceIdKeyword.Register - installs the CurrentChanged (un)subscribe callback.
        //   * EmitCurrentTraceIdChanged - called from the CurrentChanged handler; routes the change-point to
        //     the per-thread buffer, the same transport the async change-points use.
        // --------------------------------------------------------------------------------------------

        [NonEvent]
        internal static void EmitCurrentTraceIdChanged(ReadOnlySpan<byte> traceId)
        {
            if (!AsyncProfilerTraceIdKeyword.Enabled)
            {
                return;
            }

            AsyncProfiler.EmitSyncTraceIdChanged(traceId);
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

            // A keyword mask of 0 from an enable means "all keywords", so substitute the full async set.
            // On disable EventSource also resets m_matchAnyKeyword to 0 (with m_level 0 == LogAlways) before
            // this callback, so gate the substitution on IsEnabled() to avoid re-enabling instrumentation
            // while the source is being disabled.
            if (keywords == 0 && IsEnabled())
            {
                keywords = AsyncEventKeywords | Keywords.TraceIdChanged;
            }

            AsyncProfiler.Config.Update(m_level, keywords);

            // Publish the trace-id keyword transition so DiagnosticSource can (un)subscribe
            // Activity.CurrentChanged. Use IsEnabled (the aggregate across all sessions), not this one
            // command's mask, so an unrelated enable/disable does not spuriously toggle the subscription.
            AsyncProfilerTraceIdKeyword.Set(IsEnabled(EventLevel.Informational, Keywords.TraceIdChanged));
        }
    }

    // Carries the trace-id keyword's on/off state and the DiagnosticSource-side CurrentChanged subscription
    // toggle. Kept separate from AsyncProfilerEventSource so that registering the bridge on first use of
    // Activity does not construct the event source; the source is built only when a session enables it.
    internal static class AsyncProfilerTraceIdKeyword
    {
        private static int s_enabled;
        private static Action<bool>? s_subscriber;

        // True while any session has the trace-id keyword enabled. Read on the synchronous emit path as a
        // cheap guard against a keyword-off race between a CurrentChanged transition and its emit.
        internal static bool Enabled => Volatile.Read(ref s_enabled) != 0;

        // Installs the CurrentChanged (un)subscribe callback on first use of Activity, then syncs it to the
        // current state so a keyword enabled before Activity was first used still subscribes.
        internal static void Register(Action<bool> callback)
        {
            Interlocked.Exchange(ref s_subscriber, callback);
            callback(Volatile.Read(ref s_enabled) != 0);
        }

        // Drives the subscription from OnEventCommand so change-points flow only while the keyword is enabled.
        internal static void Set(bool enabled)
        {
            Interlocked.Exchange(ref s_enabled, enabled ? 1 : 0);
            Volatile.Read(ref s_subscriber)?.Invoke(enabled);
        }
    }
}
