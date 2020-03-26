using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Shared data from packets with long header.
    /// </summary>
    internal readonly ref struct LongPacketHeader
    {
        private const byte FixedBitMask = 0x40;
        private const byte TypeSpecificBitsMask = 0x0f;

        /// <summary>
        ///     First byte of the header, contains compacted data from <see cref="FixedBit"/>, <see cref=""/>, <see cref="ReservedBits"/> and <see cref="PacketNumberLength"/>.
        /// </summary>
        internal readonly byte FirstByte;

        /// <summary>
        ///     Bit with fixed value 1. Reception of value 0 implies that the packet must be quietly discarded.
        /// </summary>
        internal bool FixedBit
        {
            get => (FirstByte & FixedBitMask) != 0;
        }

        /// <summary>
        ///     Bit with fixed value 1. Reception of value 0 implies that the packet must be quietly discarded.
        /// </summary>
        internal PacketType PacketType
        {
            get => HeaderHelpers.GetPacketType(FirstByte);
        }

        /// <summary>
        ///     Four bits of reserved data for individual packet types.
        /// </summary>
        internal byte TypeSpecificBits
        {
            get => (byte)((FirstByte & TypeSpecificBitsMask) >> 3);
        }

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

        private static byte ComposeFirstByte(PacketType type, byte typeSpecificData)
        {
            Debug.Assert((uint) type <= 3, "Wrong type of packet when creating long header.");
            Debug.Assert(typeSpecificData <= 0x0f, "Type specific data overflow.");

            // first two bits are always set (form + fixed bit)
            return (byte)(0xc0 | ((int)type << 4) | typeSpecificData);
        }

        internal LongPacketHeader(PacketType type, byte typeSpecificData, QuicVersion version, ReadOnlySpan<byte> destinationConnectionId,
            ReadOnlySpan<byte> sourceConnectionId)
            : this(ComposeFirstByte(type, typeSpecificData), version, destinationConnectionId, sourceConnectionId)
        {
        }

        internal LongPacketHeader(byte firstByte, QuicVersion version, ReadOnlySpan<byte> destinationConnectionId, ReadOnlySpan<byte> sourceConnectionId)
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

            header = new LongPacketHeader(firstByte, (QuicVersion) version, dcid, scid);
            return true;
        }

        internal static void Write(QuicWriter writer, in LongPacketHeader header)
        {
            writer.WriteUInt8(header.FirstByte);

            writer.WriteUInt32((uint) header.Version);

            writer.WriteUInt8((byte) header.DestinationConnectionId.Length);
            writer.WriteSpan(header.DestinationConnectionId);

            writer.WriteUInt8((byte) header.SourceConnectionId.Length);
            writer.WriteSpan(header.SourceConnectionId);
        }
    }
}
