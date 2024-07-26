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
        private const int KeepAliveFrameSentId = NextAvailableEventId;
        private const int KeepAliveAckReceivedId = KeepAliveFrameSentId + 1;
        private const int WebSocketTraceId = KeepAliveAckReceivedId + 1;

        private const string Ping = "PING";
        private const string Pong = "PONG";

        [NonEvent]
        public static void KeepAlivePingSent(object? obj, long payload)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveFrameSent(IdOf(obj), Ping, payload);
        }

        [NonEvent]
        public static void UnsolicitedPongSent(object? obj)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveFrameSent(IdOf(obj), Pong, 0);
        }

        [NonEvent]
        public static void PongResponseReceived(object? obj, long payload)
        {
            Debug.Assert(Log.IsEnabled());
            Log.KeepAliveAckReceived(IdOf(obj), payload);
        }

        [NonEvent]
        public static void Trace(object? obj, string? message = null, [CallerMemberName] string? memberName = null)
        {
            Debug.Assert(Log.IsEnabled());
            Log.WebSocketTrace(IdOf(obj), memberName ?? MissingMember, message ?? string.Empty);
        }

        [Event(KeepAliveFrameSentId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void KeepAliveFrameSent(string objName, string opcode, long payload) =>
            WriteEvent(KeepAliveFrameSentId, objName, opcode, payload);

        [Event(KeepAliveAckReceivedId, Keywords = Keywords.Debug, Level = EventLevel.Informational)]
        private void KeepAliveAckReceived(string objName, long payload) =>
            WriteEvent(KeepAliveAckReceivedId, objName, payload);

        [Event(WebSocketTraceId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void WebSocketTrace(string objName, string memberName, string message) =>
            WriteEvent(WebSocketTraceId, objName, memberName, message);

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

    }
}
