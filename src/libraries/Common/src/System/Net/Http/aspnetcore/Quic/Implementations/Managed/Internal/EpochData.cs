#nullable enable

using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class CryptoStream
    {
        private int offset;
        private List<byte[]> data = new List<byte[]>();
        internal void Add(ReadOnlySpan<byte> data)
        {
            this.data.Add(data.ToArray());
        }

        internal (byte[] data, int streamOffset) GetDataToSend()
        {
            var data = this.data[0];
            this.data.RemoveAt(0);

            var streamOffset = offset;
            offset += data.Length;

            return (data, streamOffset);
        }

        internal bool HasDataToSend => data.Count > 0;
    }

    /// <summary>
    ///     Class for aggregating all connection data for a single epoch.
    /// </summary>
    internal class EpochData
    {
        /// <summary>
        ///     Largest packet number received by the peer.
        /// </summary>
        internal ulong LargestTransportedPacketNumber { get; set; }

        /// <summary>
        ///     Timestamp when packet with <see cref="LargestTransportedPacketNumber"/> was received.
        /// </summary>
        internal ulong LargestTransportedPacketTimestamp { get; set; }

        /// <summary>
        ///     Number for the next packet to be send with.
        /// </summary>
        internal ulong NextPacketNumber { get; set; }

        /// <summary>
        ///     All packet numbers received.
        /// </summary>
        internal RangeSet ReceivedPacketNumbers { get; } = new RangeSet();

        /// <summary>
        ///     Flag that next time packets for sending are requested, an ack frame should be added, because an ack eliciting frame was received meanwhile.
        /// </summary>
        internal bool AckElicited { get; set; }

        /// <summary>
        ///     CryptoSeal for encryption of the outbound data.
        /// </summary>
        internal CryptoSeal? SendCryptoSeal { get; set; }

        /// <summary>
        ///     CryptoSeal for decryption of inbound data.
        /// </summary>
        internal CryptoSeal? RecvCryptoSeal { get; set; }

        /// <summary>
        ///     Stream of outbound messages to be carried in CRYPTO frames.
        /// </summary>
        internal CryptoStream CryptoStream { get; set; } = new CryptoStream();

        internal (uint truncatedPn, int pnLength) GetNextPacketNumber()
        {
            int pnLength = QuicEncoding.GetPacketNumberByteCount(LargestTransportedPacketNumber, NextPacketNumber);
            return ((uint) NextPacketNumber++, pnLength);
        }
    }
}
