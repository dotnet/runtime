// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed;

namespace System.Net.Quic.Tests.Harness
{
    internal readonly struct PacketFlight
    {
        internal static readonly Comparer<PacketFlight> TimeSentComparer = Comparer<PacketFlight>.Create((l,r) => l.TimeSent.CompareTo(r.TimeSent));

        internal readonly IReadOnlyList<PacketBase> Packets;
        internal readonly int UdpDatagramSize;
        internal readonly ManagedQuicConnection Sender;
        internal readonly long TimeSent;

        public PacketFlight(List<PacketBase> packets, int udpDatagramSize, ManagedQuicConnection sender, long timeSent)
        {
            Packets = packets;
            UdpDatagramSize = udpDatagramSize;
            Sender = sender;
            TimeSent = timeSent;
        }
    }
}
