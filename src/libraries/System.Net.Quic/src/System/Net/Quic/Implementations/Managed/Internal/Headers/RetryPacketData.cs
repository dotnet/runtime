using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal.Headers
{
    /// <summary>
    ///     Packet data carried by the retry packets. These are sent from the server wishing to perform a retry.
    /// </summary>
    internal readonly ref struct RetryPacketData
    {
        /// <summary>
        ///     Opaque token server can use to validate the clients address.
        /// </summary>
        internal readonly ReadOnlySpan<byte> RetryToken;

        /// <summary>
        ///     Integrity tag computed for the <see cref="RetryToken" />.
        /// </summary>
        internal readonly ReadOnlySpan<byte> RetryIntegrityTag;

        internal RetryPacketData(ReadOnlySpan<byte> retryToken, ReadOnlySpan<byte> retryIntegrityTag)
        {
            RetryToken = retryToken;
            RetryIntegrityTag = retryIntegrityTag;
        }

        internal static bool Read(QuicReader reader, out RetryPacketData data)
        {
            if (!reader.TryReadSpan(reader.BytesLeft - CryptoSealAesGcm.IntegrityTagLength, out var token) ||
                !reader.TryReadSpan(CryptoSealAesGcm.IntegrityTagLength, out var tag))
            {
                data = default;
                return false;
            }

            data = new RetryPacketData(token, tag);
            return true;
        }

        internal static void Write(QuicWriter writer, in RetryPacketData data)
        {
            writer.WriteSpan(data.RetryToken);
            writer.WriteSpan(data.RetryIntegrityTag);
        }
    }
}
