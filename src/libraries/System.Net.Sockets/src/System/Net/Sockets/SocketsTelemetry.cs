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
        public static bool IsEnabled(EventLevel level)
        {
            return Log.IsEnabled(level, EventKeywords.All);
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

        [Event(1, Level = EventLevel.Informational)]
        public void ConnectStart(string? address)
        {
            WriteEvent(eventId: 1, address ?? "");
        }

        [Event(2, Level = EventLevel.Informational)]
        public void ConnectStop()
        {
            WriteEvent(eventId: 2);
        }

        [Event(3, Level = EventLevel.Error)]
        public void ConnectFailed()
        {
            WriteEvent(eventId: 3);
            ConnectStop();
        }

        [Event(4, Level = EventLevel.Warning)]
        public void ConnectCancelled()
        {
            WriteEvent(eventId: 4);
        }
    }
}
