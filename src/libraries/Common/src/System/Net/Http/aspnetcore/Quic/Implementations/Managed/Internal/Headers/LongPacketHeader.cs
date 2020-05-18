using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Shared data from packets with long header.
    /// </summary>
    internal readonly ref struct LongPacketHeader
    {
        /// <summary>
        ///     First byte of the header, contains compacted data from <see cref="FixedBit" />, <see cref="PacketType" />.
        /// </summary>
        internal readonly byte FirstByte;

        /// <summary>
        ///     Bit with fixed value 1. Reception of value 0 implies that the packet must be quietly discarded.
        /// </summary>
        internal bool FixedBit => HeaderHelpers.GetFixedBit(FirstByte);

        /// <summary>
        ///     Type of the received packet.
        /// </summary>
        internal PacketType PacketType => Version == QuicVersion.Negotiation
            ? PacketType.VersionNegotiation
            : HeaderHelpers.GetLongPacketType(FirstByte);

        /// <summary>
        ///     How many lower bytes are encoded in this packet. Contains valid valid value only when <see cref="PacketType" /> is
        ///     <see cref="PacketType.Initial" />, <see cref="PacketType.ZeroRtt" />, <see cref="PacketType.Handshake" />.
        /// </summary>
        internal int PacketNumberLength => HeaderHelpers.GetPacketNumberLength(FirstByte);

        /// <summary>
        ///     Bits reserved for future versions of the QUIC protocol, should be always 0.
        /// </summary>
        internal byte ReservedBits => HeaderHelpers.GetLongHeaderReservedBits(FirstByte);

        /// <summary>
        ///     Version of the QUIC protocol used.
        /// </summary>
        internal readonly QuicVersion Version;

        /// <summary>
        ///     Destination connection ID.
        /// </summary>
        internal readonly ReadOnlySpan<byte> DestinationConnectionId;

        /// <summary>
        ///     Source connection ID.
        /// </summary>
        internal readonly ReadOnlySpan<byte> SourceConnectionId;

        internal LongPacketHeader(PacketType type, int packetNumberLength, QuicVersion version,
            ReadOnlySpan<byte> destinationConnectionId,
            ReadOnlySpan<byte> sourceConnectionId)
            : this(HeaderHelpers.ComposeLongHeaderByte(type, packetNumberLength), version, destinationConnectionId,
                sourceConnectionId)
        {
        }

        internal LongPacketHeader(byte firstByte, QuicVersion version, ReadOnlySpan<byte> destinationConnectionId,
            ReadOnlySpan<byte> sourceConnectionId)
        {
            FirstByte = firstByte;
            Version = version;
            DestinationConnectionId = destinationConnectionId;
            SourceConnectionId = sourceConnectionId;
        }

        internal static bool Read(QuicReader reader, out LongPacketHeader header)
        {
            byte firstByte = reader.ReadUInt8();
            Debug.Assert(HeaderHelpers.IsLongHeader(firstByte), "Trying to parse short packet header as long.");

            if (!reader.TryReadUInt32(out uint version) ||
                !reader.TryReadUInt8(out byte dcidLen) ||
                !reader.TryReadSpan(dcidLen, out var dcid) ||
                !reader.TryReadUInt8(out byte scidLen) ||
                !reader.TryReadSpan(scidLen, out var scid))
            {
                header = default;
                return false;
            }

            header = new LongPacketHeader(firstByte, (QuicVersion)version, dcid, scid);
            return true;
        }

        internal static void Write(QuicWriter writer, in LongPacketHeader header)
        {
            writer.WriteUInt8(header.FirstByte);

            writer.WriteInt32((int)header.Version);

            writer.WriteUInt8((byte)header.DestinationConnectionId.Length);
            writer.WriteSpan(header.DestinationConnectionId);

            writer.WriteUInt8((byte)header.SourceConnectionId.Length);
            writer.WriteSpan(header.SourceConnectionId);
        }
    }
}
