using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Header used for 1-RTT packets.
    /// </summary>
    internal readonly ref struct ShortPacketHeader
    {
        /// <summary>
        ///     First byte of the header, contains compacted data from <see cref="FixedBit" />, <see cref="SpinBit" />,
        ///     <see cref="ReservedBits" /> and <see cref="PacketNumberLength" />.
        /// </summary>
        internal readonly byte FirstByte;

        /// <summary>
        ///     Bit with fixed value 1. Reception of value 0 implies that the packet must be quietly discarded.
        /// </summary>
        internal bool FixedBit => HeaderHelpers.GetFixedBit(FirstByte);

        /// <summary>
        ///     Optional spin bit information for measuring latency.
        /// </summary>
        internal bool SpinBit => HeaderHelpers.GetSpinBit(FirstByte);

        /// <summary>
        ///     Reserved bits. Reception of any value other than 00 implies PROTOCOL_VIOLATION connection error.
        /// </summary>
        internal byte ReservedBits => HeaderHelpers.GetShortHeaderReservedBits(FirstByte);

        /// <summary>
        ///     Used to identify the keys used for protecting this packet when keys are updated mid-connection.
        /// </summary>
        internal bool KeyPhaseBit => HeaderHelpers.GetKeyPhase(FirstByte);

        /// <summary>
        ///     Number of least significant bytes of the packet number transferred in this packet.
        /// </summary>
        internal int PacketNumberLength => HeaderHelpers.GetPacketNumberLength(FirstByte);

        /// <summary>
        ///     Connection ID chosen by the intended recipient of the packet.
        /// </summary>
        internal readonly ConnectionId DestinationConnectionId;

        internal ShortPacketHeader(byte firstByte, ConnectionId destinationConnectionId)
        {
            FirstByte = firstByte;
            DestinationConnectionId = destinationConnectionId;
        }

        internal ShortPacketHeader(bool spin, bool keyPhase, int packetNumberLength,
            ConnectionId destinationConnectionId)
            : this(HeaderHelpers.ComposeShortHeaderByte(spin, keyPhase, packetNumberLength),
                destinationConnectionId)
        {
        }

        internal static bool Read(QuicReader reader, ConnectionIdCollection connectionIds, out ShortPacketHeader header)
        {
            byte firstByte = reader.ReadUInt8();
            Debug.Assert(!HeaderHelpers.IsLongHeader(firstByte), "Trying to parse long header as short");

            var dcid = connectionIds.Find(reader.PeekSpan(reader.BytesLeft));
            if (dcid == null)
            {
                header = default;
                return false;
            }

            reader.Advance(dcid.Data.Length);
            header = new ShortPacketHeader(firstByte, dcid);
            return true;
        }

        internal static void Write(QuicWriter writer, in ShortPacketHeader header)
        {
            writer.WriteUInt8(header.FirstByte);
            writer.WriteSpan(header.DestinationConnectionId.Data);
        }
    }
}
