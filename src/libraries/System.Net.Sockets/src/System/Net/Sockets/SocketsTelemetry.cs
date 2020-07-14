// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;

namespace System.Net.Sockets
{
    [EventSource(Name = "System.Net.Sockets")]
    internal sealed class SocketsTelemetry : EventSource
    {
        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

        [Event(1, Level = EventLevel.Informational)]
        public void ConnectStart(string? address)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 1, address ?? "");
            }
        }

        [Event(2, Level = EventLevel.Informational)]
        public void ConnectStop()
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 2);
            }
        }

        [Event(3, Level = EventLevel.Error)]
        public void ConnectFailed(SocketError error, string? exceptionMessage)
        {
            if (IsEnabled(EventLevel.Error, EventKeywords.All))
            {
                WriteEvent(eventId: 3, (int)error, exceptionMessage ?? string.Empty);
            }
        }

        [Event(4, Level = EventLevel.Warning)]
        public void ConnectCanceled()
        {
            if (IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                WriteEvent(eventId: 4);
            }
        }

        [NonEvent]
        public void ConnectStart(Internals.SocketAddress address)
        {
            ConnectStart(address.ToString());
        }

        [NonEvent]
        public void ConnectStart(EndPoint address)
        {
            ConnectStart(address.ToString());
        }

        [NonEvent]
        public void ConnectCanceledAndStop()
        {
            ConnectCanceled();
            ConnectStop();
        }

        [NonEvent]
        public void ConnectFailedAndStop(SocketError error, string? exceptionMessage)
        {
            ConnectFailed(error, exceptionMessage);
            ConnectStop();
        }
    }
}
