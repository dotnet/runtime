using System.Collections.Generic;

namespace System.Net.Quic.Tests.Harness
{
    internal readonly struct PacketFlight
    {
        internal readonly IReadOnlyList<PacketBase> Packets;
        internal readonly int UdpDatagramSize;

        public PacketFlight(List<PacketBase> packets, int udpDatagramSize)
        {
            Packets = packets;
            UdpDatagramSize = udpDatagramSize;
        }
    }
}
