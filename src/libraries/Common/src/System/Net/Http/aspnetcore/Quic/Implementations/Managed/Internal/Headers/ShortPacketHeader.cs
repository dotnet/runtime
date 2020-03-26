using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Header used for 1-RTT packets.
    /// </summary>
    internal readonly ref struct ShortPacketHeader
    {
        private const byte FixedBitMask = 0x40;
        private const byte SpinBitMask = 0x20;
        private const byte ReservedBitsMask = 0x18;
        private const byte KeyPhaseBitMask = 0x04;
        private const byte PacketNumberLengthMask = 0x03;

        /// <summary>
        ///     First byte of the header, contains compacted data from <see cref="FixedBit"/>, <see cref="SpinBit"/>, <see cref="ReservedBits"/> and <see cref="PacketNumberLength"/>.
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
        ///     Optional spin bit information for measuring latency.
        /// </summary>
        internal bool SpinBit
        {
            get => (FirstByte & SpinBitMask) != 0;
        }

        /// <summary>
        ///     Reserved bits. Reception of any value other than 00 implies PROTOCOL_VIOLATION connection error.
        /// </summary>
        internal byte ReservedBits
        {
            get => (byte)((FirstByte & ReservedBitsMask) >> 3);
        }

        /// <summary>
        ///     Used to identify the keys used for protecting this packet when keys are updated mid-connection.
        /// </summary>
        internal bool KeyPhaseBit
        {
            get => (FirstByte & KeyPhaseBitMask) != 0;
        }

        /// <summary>
        ///     Number of least significant bytes of the packet number transferred in this packet. The transfered value is accessible in <see cref="TruncatedPacketNumber"/>.
        /// </summary>
        internal int PacketNumberLength
        {
            get => (FirstByte & PacketNumberLengthMask) + 1;
        }

        /// <summary>
        ///     Lower part of the packet number. Number of significant bytes is encoded in <see cref="PacketNumberLength"/>.
        /// </summary>
        internal readonly uint TruncatedPacketNumber;

        /// <summary>
        ///     Connection ID chosen by the intended recipient of the packet.
        /// </summary>
        internal readonly ConnectionId DestinationConnectionId;

        private static byte ComposeFirstByte(bool spin, bool keyPhase, int packetNumberLength)
        {
            Debug.Assert((uint)packetNumberLength <= 4);

            // Fixed bit is always 1, reserved bits always 0
            int firstByte = (packetNumberLength | FixedBitMask);
            if (spin) firstByte |= SpinBitMask;
            if (keyPhase) firstByte |= KeyPhaseBitMask;

            return (byte)firstByte;
        }

        internal ShortPacketHeader(byte firstByte, uint truncatedPacketNumber, ConnectionId destinationConnectionId)
        {
            this.FirstByte = firstByte;
            TruncatedPacketNumber = truncatedPacketNumber;
            DestinationConnectionId = destinationConnectionId;
        }

        internal ShortPacketHeader(bool spin, bool keyPhase, int packetNumberLength, uint truncatedPacketNumber,
            ConnectionId destinationConnectionId)
            : this(ComposeFirstByte(spin, keyPhase, packetNumberLength), truncatedPacketNumber, destinationConnectionId)
        {
        }

        internal static bool Read(QuicReader reader, ConnectionIdCollection connectionIds, out ShortPacketHeader header)
        {
            byte firstByte = reader.ReadUInt8();
            Debug.Assert(!HeaderHelpers.IsLongHeader(firstByte), "Trying to parse long header as short");

            // TODO-RZ: maybe expect only single connection id
            var dcid = connectionIds.FindConnectionId(reader.PeekSpan(reader.BytesLeft));
            if (dcid == null)
            {
                header = default;
                return false;
            }

            reader.Advance(dcid.Data.Length);
            if (!reader.TryReadTruncatedPacketNumber(firstByte & PacketNumberLengthMask, out uint truncatedPacketNumber))
            {
                header = default;
                return false;
            }

            header = new ShortPacketHeader(firstByte, truncatedPacketNumber, dcid);
            return true;
        }

        static void Write(QuicWriter writer, in ShortPacketHeader header)
        {
            writer.WriteUInt64(header.FirstByte);
            writer.WriteSpan(header.DestinationConnectionId.Data);
            writer.WriteTruncatedPacketNumber(header.PacketNumberLength, header.TruncatedPacketNumber);
        }
    }
}
