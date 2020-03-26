namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Type of the QUIC packet.
    /// </summary>
    internal enum PacketType
    {
        /// <summary>Initial packet type for initiating new connections and key exchange.</summary>
        Initial = 0x00,

        /// <summary>Packet for initiating new connections with early (0-RTT) data as part of first flight.</summary>
        ZeroRtt = 0x01,

        /// <summary>Packet type used to carry acknowledgement and cryptographic messages.</summary>
        Handshake = 0x02,

        /// <summary>Packet used by server that wishes to retry the connection attempt.</summary>
        Retry = 0x03,

        /// <summary>Packet used after connection establishment to carry application data.</summary>
        OneRtt,

        /// <summary>Packet type used during protocol version negotiation.</summary>
        VersionNegotiation,
    }
}