using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Type specific data for the initial, 0-RTT and Handshake packet. Excluding the payload frames.
    /// </summary>
    internal readonly ref struct SharedPacketData
    {
        /// <summary>
        ///     Copy of the first byte from the long packet header.
        /// </summary>
        private readonly byte firstByte;

        /// <summary>
        ///     Reserved bits. Reception of any value other than 00 implies PROTOCOL_VIOLATION connection error.
        /// </summary>
        internal byte ReservedBits => HeaderHelpers.GetShortHeaderReservedBits(firstByte);

        /// <summary>
        ///     Number of least significant bytes of the packet number transferred in this packet. The transfered value is
        ///     accessible in <see cref="TruncatedPacketNumber" />.
        /// </summary>
        internal int PacketNumberLength => HeaderHelpers.GetPacketNumberLength(firstByte);

        /// <summary>
        ///     Value of the token provided to the peer previously by <see cref="NewTokenFrame" />. Only used when type of the
        ///     packet is Initial.
        /// </summary>
        internal readonly ReadOnlySpan<byte> Token;

        /// <summary>
        ///     The length of the rest of the packet, including packet number.
        ///     TODO-RZ: there does not seem to be any point of sending length? maybe they forgot to remove it
        /// </summary>
        internal readonly ulong Length;

        /// <summary>
        ///     Lower part of the packet number. Number of significant bytes is encoded in <see cref="PacketNumberLength" />.
        /// </summary>
        internal readonly uint TruncatedPacketNumber;

        internal SharedPacketData(byte firstByte, ReadOnlySpan<byte> token, ulong length, uint truncatedPacketNumber)
        {
            this.firstByte = firstByte;
            Token = token;
            Length = length;
            TruncatedPacketNumber = truncatedPacketNumber;
        }

        internal static bool Read(QuicReader reader, byte firstHeaderByte, out SharedPacketData data)
        {
            int pnLength = HeaderHelpers.GetPacketNumberLength(firstHeaderByte);
            var type = HeaderHelpers.GetPacketType(firstHeaderByte);

            ReadOnlySpan<byte> token = ReadOnlySpan<byte>.Empty;
            if (type == PacketType.Initial && !reader.TryReadLengthPrefixedSpan(out token) ||
                !reader.TryReadVarInt(out ulong length) ||
                !reader.TryReadTruncatedPacketNumber(pnLength, out uint truncatedPn))
            {
                data = default;
                return false;
            }

            data = new SharedPacketData(firstHeaderByte, token, length, truncatedPn);
            return true;
        }

        internal static void Write(QuicWriter writer, in SharedPacketData data)
        {
            Debug.Assert(HeaderHelpers.GetPacketType(data.firstByte) == PacketType.Initial ||
                         data.Token.IsEmpty, "Trying to include Token in non-initial packet.");

            writer.WriteLengthPrefixedSpan(data.Token);
            writer.WriteVarInt(data.Length);
            writer.WriteTruncatedPacketNumber(data.PacketNumberLength, data.TruncatedPacketNumber);
        }
    }
}
