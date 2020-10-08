using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal.Frames
{
    /// <summary>
    ///     Used to transmit opaque cryptographic handshake messages.
    /// </summary>
    internal readonly ref struct CryptoFrame
    {
        /// <summary>
        ///     Byte offset of the stream carrying the cryptographic data.
        /// </summary>
        internal readonly long Offset;

        /// <summary>
        ///     Cryptographic message data;
        /// </summary>
        internal readonly ReadOnlySpan<byte> CryptoData;

        internal CryptoFrame(long offset, ReadOnlySpan<byte> cryptoData)
        {
            Offset = offset;
            CryptoData = cryptoData;
        }

        internal int GetSerializedLength()
        {
            return 1 +
                   QuicPrimitives.GetVarIntLength(Offset) +
                   QuicPrimitives.GetVarIntLength(CryptoData.Length) +
                   CryptoData.Length;
        }

        internal static bool Read(QuicReader reader, out CryptoFrame frame)
        {
            var type = reader.ReadFrameType();
            Debug.Assert(type == FrameType.Crypto);

            if (!reader.TryReadVarInt(out long offset) ||
                !reader.TryReadLengthPrefixedSpan(out var data))
            {
                frame = default;
                return false;
            }

            frame = new CryptoFrame(offset, data);
            return true;
        }

        internal static Span<byte> ReservePayloadBuffer(QuicWriter writer, long offset, long length)
        {
            writer.WriteFrameType(FrameType.Crypto);

            writer.WriteVarInt(offset);
            writer.WriteVarInt(length);
            return writer.GetWritableSpan((int) length);
        }

        internal static void Write(QuicWriter writer, in CryptoFrame frame)
        {
            Debug.Assert(writer.BytesAvailable >= frame.GetSerializedLength());

            frame.CryptoData.CopyTo(ReservePayloadBuffer(writer, frame.Offset, frame.CryptoData.Length));
        }
    }
}
