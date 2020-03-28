using System.Net.Quic.Implementations.Managed.Internal.Crypto;

namespace System.Net.Quic.Implementations.Managed.Internal
{
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
        internal RangeSet ReceivedPacketNumbers { get; set; }

        /// <summary>
        ///     Flag that next time packets for sending are requested, an ack frame should be added, because an ack eliciting frame was received meanwhile.
        /// </summary>
        internal bool AckElicited { get; set; }

        /// <summary>
        ///     CryptoSeal for encryption of the outbound data.
        /// </summary>
        internal CryptoSeal SendCryptoSeal { get; set; }

        /// <summary>
        ///     CryptoSeal for decryption of inbound data.
        /// </summary>
        internal CryptoSeal RecvCryptoSeal { get; set; }
    }
}
