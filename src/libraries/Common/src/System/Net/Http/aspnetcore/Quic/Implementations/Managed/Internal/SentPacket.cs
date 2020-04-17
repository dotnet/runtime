using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///      Contains data about what all was sent in an outbound packet for packet loss recovery purposes.
    /// </summary>
    internal class SentPacket
    {
        /// <summary>
        ///     Timestamp when the packet was sent.
        /// </summary>
        internal long TimeSent { get; set; }

        /// <summary>
        ///     Ranges of values which have been acked in the Ack frame in this packet, empty if nothing was acked.
        /// </summary>
        internal RangeSet AckedRanges { get; } = new RangeSet();

        /// <summary>
        ///     Ranges sent in the Crypto frames.
        /// </summary>
        internal RangeSet CryptoRanges { get; } = new RangeSet();

        /// <summary>
        ///     Data ranges set in Stream frames.
        /// </summary>
        internal SortedList<long, RangeSet> SentStreamData { get; } = new SortedList<long, RangeSet>();

        /// <summary>
        ///     True if HANDSHAKE_DONE frame is sent in the packet.
        /// </summary>
        internal bool HandshakeDoneSent { get; set; }

        /// <summary>
        ///     Number of bytes sent, includes QUIC header and payload, but not UDP and IP overhead.
        /// </summary>
        internal int BytesSent { get; set; }

        /// <summary>
        ///     True if the packet counts toward bytes in flight for congestion control purposes.
        /// </summary>
        internal bool InFlight { get; set; }

        /// <summary>
        ///     True if the packet contained an ack-eliciting frame. If true, it is expected that an
        ///     acknowledgement will be received, though the peer could delay sending the ACK frame by
        ///     up to the MaxAckDelay transport parameter.
        /// </summary>
        internal bool AckEliciting { get; set; }

        /// <summary>
        ///     Resets the object to it's default state so that it can be reused.
        /// </summary>
        internal void Reset()
        {
            AckedRanges.Clear();
            CryptoRanges.Clear();
            SentStreamData.Clear();
            HandshakeDoneSent = false;
            BytesSent = 0;
        }
    }
}
