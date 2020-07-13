// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Net.Sockets
{
    [EventSource(Name = "System.Net.Sockets")]
    internal sealed class SocketsTelemetry : EventSource
    {
        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

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
            ConnectStopInternal();
        }

        [Event(3, Level = EventLevel.Error)]
        public void ConnectFailed(SocketError error, Exception? exception)
        {
            if (IsEnabled(EventLevel.Error, EventKeywords.All))
            {
                string message = exception?.Message ?? string.Empty;
                WriteEvent(eventId: 3, (int)error, message);
                ConnectStopInternal();
            }
        }

        [Event(4, Level = EventLevel.Warning)]
        public void ConnectCanceled()
        {
            if (IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                WriteEvent(eventId: 4);
                ConnectStopInternal();
            }
        }

        [NonEvent]
        private void ConnectStopInternal()
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 2);
            }
        }
    }
}
