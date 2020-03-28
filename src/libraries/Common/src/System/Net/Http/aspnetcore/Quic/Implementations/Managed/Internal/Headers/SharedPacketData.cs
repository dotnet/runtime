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
        ///     Number of least significant bytes of the packet number transferred in this packet.
        /// </summary>
        internal int PacketNumberLength => HeaderHelpers.GetPacketNumberLength(firstByte);

        /// <summary>
        ///     Value of the token provided to the peer previously by <see cref="NewTokenFrame" />. Only used when type of the
        ///     packet is Initial.
        /// </summary>
        internal readonly ReadOnlySpan<byte> Token;

        /// <summary>
        ///     The length of the rest of the packet, including packet number.
        /// </summary>
        internal readonly ulong Length;

        internal SharedPacketData(byte firstByte, ReadOnlySpan<byte> token, ulong length)
        {
            this.firstByte = firstByte;
            Token = token;
            Length = length;
        }

        internal static bool Read(QuicReader reader, byte firstHeaderByte, out SharedPacketData data)
        {
            int pnLength = HeaderHelpers.GetPacketNumberLength(firstHeaderByte);
            var type = HeaderHelpers.GetLongPacketType(firstHeaderByte);

            ReadOnlySpan<byte> token = ReadOnlySpan<byte>.Empty;
            if (type == PacketType.Initial && !reader.TryReadLengthPrefixedSpan(out token) ||
                !reader.TryReadVarInt(out ulong length))
            {
                data = default;
                return false;
            }

            data = new SharedPacketData(firstHeaderByte, token, length);
            return true;
        }

        internal static void Write(QuicWriter writer, in SharedPacketData data)
        {
            Debug.Assert(HeaderHelpers.GetLongPacketType(data.firstByte) == PacketType.Initial ||
                         data.Token.IsEmpty, "Trying to include Token in non-initial packet.");

            writer.WriteLengthPrefixedSpan(data.Token);
            writer.WriteVarInt(data.Length);
        }
    }
}
