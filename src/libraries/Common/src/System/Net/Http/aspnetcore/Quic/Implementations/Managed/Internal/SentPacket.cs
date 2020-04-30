using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Xml;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///      Contains data about what all was sent in an outbound packet for packet loss recovery purposes.
    /// </summary>
    internal class SentPacket : IPoolableObject
    {
        internal static readonly Comparer<SentPacket> PacketNumberComparer = Comparer<SentPacket>.Create((l, r) => l.PacketNumber.CompareTo(r.PacketNumber));

        /// <summary>
        ///     Structure containing data about sent data in Stream frames.
        /// </summary>
        internal readonly struct StreamFrameHeader
        {
            internal StreamFrameHeader(long streamId, long offset, int count, bool fin)
            {
                StreamId = streamId;
                Offset = offset;
                Count = count;
                Fin = fin;
            }

            internal static StreamFrameHeader ForCryptoStream(long offset, int length)
            {
                return new StreamFrameHeader(-1, offset, length, false);
            }

            /// <summary>
            ///     Id of the stream to which data were sent. -1 if data were sent in Crypto stream.
            /// </summary>
            internal readonly long StreamId;

            /// <summary>
            ///     True if the data were sent on a crypto stream.
            /// </summary>
            internal bool IsCryptoStream => StreamId == -1;

            /// <summary>
            ///     Offset on which the data were sent.
            /// </summary>
            internal readonly long Offset;

            /// <summary>
            ///     Number of bytes sent.
            /// </summary>
            internal readonly int Count;

            /// <summary>
            ///     True if this the Fin bit was set in the stream frame.
            /// </summary>
            internal readonly bool Fin;
        }

        /// <summary>
        ///     Packet number this packet was sent with.
        /// </summary>
        internal long PacketNumber { get; set; }

        /// <summary>
        ///     Timestamp when the packet was sent.
        /// </summary>
        internal long TimeSent { get; set; }

        /// <summary>
        ///     Ranges of values which have been acked in the Ack frame in this packet, empty if nothing was acked.
        /// </summary>
        internal RangeSet AckedRanges { get; } = new RangeSet();

        /// <summary>
        ///     Headers of the stream frames sent in this packet.
        /// </summary>
        internal List<StreamFrameHeader> StreamFrames { get; } = new List<StreamFrameHeader>();

        /// <summary>
        ///     List of <see cref="MaxStreamDataFrames"/> sent in the packet.
        /// </summary>
        internal List<MaxStreamDataFrame> MaxStreamDataFrames { get; } = new List<MaxStreamDataFrame>();

        /// <summary>
        ///     Contains data from <see cref="MaxDataFrame"/>, if it was sent in the packet.
        /// </summary>
        internal MaxDataFrame? MaxDataFrame { get; set; }

        /// <summary>
        ///     True if HANDSHAKE_DONE frame is sent in the packet.
        /// </summary>
        internal bool HandshakeDoneSent { get; set; }

        /// <summary>
        ///     Number of bytes sent, includes QUIC header and payload, but not UDP and IP overhead.
        /// </summary>
        internal int BytesSent { get; set; }

        /// <summary>
        ///     True if the packet counts toward bytes in flight for congestion control purposes. That happens when
        ///     The packet contains either PADDING frame or an ack-eliciting frame.
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
        public void Reset()
        {
            PacketNumber = 0;
            TimeSent = 0;

            AckedRanges.Clear();
            StreamFrames.Clear();
            MaxStreamDataFrames.Clear();
            MaxDataFrame = null;

            HandshakeDoneSent = false;
            InFlight = false;
            AckEliciting = false;
            BytesSent = 0;
        }
    }
}
