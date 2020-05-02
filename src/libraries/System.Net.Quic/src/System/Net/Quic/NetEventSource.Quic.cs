// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Quic.Implementations.Managed;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net-Quic")]
    internal sealed partial class NetEventSource : EventSource
    {
        private const int PacketSentId = NextAvailableEventId;
        private const int PacketLostId = PacketSentId + 1;

        [NonEvent]
        public static void PacketLost(ManagedQuicConnection connection, int size)
        {
            if (IsEnabled)
            {
                Log.PacketLostE(size);
            }
        }

        [Event(PacketLostId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void PacketLostE(int size)
        {
            WriteEvent(PacketLostId, size);
        }

        [NonEvent]
        public static void PacketSent(ManagedQuicConnection connection, int size)
        {
            if (IsEnabled)
            {
                Log.PacketSentE(size);
            }
        }

        [Event(PacketSentId, Keywords = Keywords.Debug, Level = EventLevel.Verbose)]
        private void PacketSentE(int size)
        {
            WriteEvent(PacketSentId, size);
        }
    }
}
