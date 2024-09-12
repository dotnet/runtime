// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.WebSockets")]
    internal sealed partial class NetEventSource
    {
        // NOTE
        // - The 'Start' and 'Stop' suffixes on the following event names have special meaning in EventSource. They
        //   enable creating 'activities'.
        //   For more information, take a look at the following blog post:
        //   https://blogs.msdn.microsoft.com/vancem/2015/09/14/exploring-eventsource-activity-correlation-and-causation-features/
        // - A stop event's event id must be next one after its start event.

        private const int KeepAliveSentId = NextAvailableEventId;
        private const int KeepAliveAckedId = KeepAliveSentId + 1;

        private const int WsTraceId = KeepAliveAckedId + 1;

        private const int CloseStartId = WsTraceId + 1;
        private const int CloseStopId = CloseStartId + 1;

        private const int ReceiveStartId = CloseStopId + 1;
        private const int ReceiveStopId = ReceiveStartId + 1;

        private const int SendStartId = ReceiveStopId + 1;
        private const int SendStopId = SendStartId + 1;

        private const int MutexEnterId = SendStopId + 1;
        private const int MutexExitId = MutexEnterId + 1;
        private const int MutexContendedId = MutexExitId + 1;

        //
        // Keep-Alive
        //

        private const string Ping = "Ping";
        private const string Pong = "Pong";

        [Event(KeepAliveSentId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void KeepAliveSent(string objName, string opcode, long payload) =>
            WriteEvent(KeepAliveSentId, objName, opcode, payload);

        [Event(KeepAliveAckedId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void KeepAliveAcked(string objName, long payload) =>
            WriteEvent(KeepAliveAckedId, objName, payload);

        [NonEvent]
        public static void KeepAlivePingSent(object? obj, long payload)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveSent(IdOf(obj), Ping, payload);
        }

        [NonEvent]
        public static void UnsolicitedPongSent(object? obj)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveSent(IdOf(obj), Pong, 0);
        }

        [NonEvent]
        public static void PongResponseReceived(object? obj, long payload)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveAcked(IdOf(obj), payload);
        }

        //
        // Debug Messages
        //

        [Event(WsTraceId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void WsTrace(string objName, string memberName, string message) =>
            WriteEvent(WsTraceId, objName, memberName, message);

        [NonEvent]
        public static void TraceErrorMsg(object? obj, Exception exception, [CallerMemberName] string? memberName = null)
            => Trace(obj, $"{exception.GetType().Name}: {exception.Message}", memberName);

        [NonEvent]
        public static void TraceException(object? obj, Exception exception, [CallerMemberName] string? memberName = null)
            => Trace(obj, exception.ToString(), memberName);

        [NonEvent]
        public static void Trace(object? obj, string? message = null, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.WsTrace(IdOf(obj), memberName ?? MissingMember, message ?? memberName ?? string.Empty);
        }

        //
        // Close
        //

        [Event(CloseStartId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void CloseStart(string objName, string memberName) =>
            WriteEvent(CloseStartId, objName, memberName);

        [Event(CloseStopId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void CloseStop(string objName, string memberName) =>
            WriteEvent(CloseStopId, objName, memberName);

        [NonEvent]
        public static void CloseAsyncPrivateStarted(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.CloseStart(IdOf(obj), memberName ?? MissingMember);
        }

        [NonEvent]
        public static void CloseAsyncPrivateCompleted(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.CloseStop(IdOf(obj), memberName ?? MissingMember);
        }

        //
        // ReceiveAsyncPrivate
        //

        [Event(ReceiveStartId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void ReceiveStart(string objName, string memberName, int bufferLength) =>
            WriteEvent(ReceiveStartId, objName, memberName, bufferLength);

        [Event(ReceiveStopId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void ReceiveStop(string objName, string memberName) =>
            WriteEvent(ReceiveStopId, objName, memberName);

        [NonEvent]
        public static void ReceiveAsyncPrivateStarted(object? obj, int bufferLength, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.ReceiveStart(IdOf(obj), memberName ?? MissingMember, bufferLength);
        }

        [NonEvent]
        public static void ReceiveAsyncPrivateCompleted(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.ReceiveStop(IdOf(obj), memberName ?? MissingMember);
        }

        //
        // SendFrameAsync
        //

        [Event(SendStartId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void SendStart(string objName, string memberName, string opcode, int bufferLength) =>
            WriteEvent(SendStartId, objName, memberName, opcode, bufferLength);

        [Event(SendStopId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void SendStop(string objName, string memberName) =>
            WriteEvent(SendStopId, objName, memberName);

        [NonEvent]
        public static void SendFrameAsyncStarted(object? obj, string opcode, int bufferLength, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.SendStart(IdOf(obj), memberName ?? MissingMember, opcode, bufferLength);
        }

        [NonEvent]
        public static void SendFrameAsyncCompleted(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.SendStop(IdOf(obj), memberName ?? MissingMember);
        }

        //
        // AsyncMutex
        //

        [Event(MutexEnterId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void MutexEnter(string objName, string memberName) =>
            WriteEvent(MutexEnterId, objName, memberName);

        [Event(MutexExitId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void MutexExit(string objName, string memberName) =>
            WriteEvent(MutexExitId, objName, memberName);

        [Event(MutexContendedId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void MutexContended(string objName, string memberName, int queueLength) =>
            WriteEvent(MutexContendedId, objName, memberName, queueLength);

        [NonEvent]
        public static void MutexEntered(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.MutexEnter(IdOf(obj), memberName ?? MissingMember);
        }

        [NonEvent]
        public static void MutexExited(object? obj, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.MutexExit(IdOf(obj), memberName ?? MissingMember);
        }

        [NonEvent]
        public static void MutexContended(object? obj, int gateValue, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.MutexContended(IdOf(obj), memberName ?? MissingMember, -gateValue);
        }

        //
        // WriteEvent overloads
        //

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, long arg3)
        {
            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            {
                const int NumEventDatas = 3;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(arg2Ptr),
                    Size = (arg2.Length + 1) * sizeof(char)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&arg3),
                    Size = sizeof(long)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3, int arg4)
        {
            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            fixed (char* arg3Ptr = arg3)
            {
                const int NumEventDatas = 4;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)(arg1Ptr),
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)(arg2Ptr),
                    Size = (arg2.Length + 1) * sizeof(char)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(arg3Ptr),
                    Size = (arg3.Length + 1) * sizeof(char)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)(&arg4),
                    Size = sizeof(int)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }

    }
}
