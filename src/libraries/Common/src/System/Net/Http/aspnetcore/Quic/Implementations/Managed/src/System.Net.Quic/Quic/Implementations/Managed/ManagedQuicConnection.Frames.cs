#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed
{
    internal partial class ManagedQuicConnection
    {
        /// <summary>
        ///      Contains data about what all was sent in an outbound packet for packet loss recovery purposes.
        /// </summary>
        internal class SentPacket
        {
            /// <summary>
            ///     Timestamp when the packet was sent.
            /// </summary>
            internal DateTime Sent { get; set; }

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
            internal SortedList<ulong, RangeSet> SentStreamData { get; } = new SortedList<ulong, RangeSet>();

            /// <summary>
            ///     True if HANDSHAKE_DONE frame is sent in the packet.
            /// </summary>
            internal bool HandshakeDoneSent { get; set; }

            /// <summary>
            ///     Resets the object to it's default state so that it can be reused.
            /// </summary>
            internal void Reset()
            {
                AckedRanges.Clear();
                CryptoRanges.Clear();
                SentStreamData.Clear();
                HandshakeDoneSent = false;
            }
        }

        private static bool IsAckEliciting(FrameType frameType)
        {
            return frameType switch
            {
                FrameType.Padding => false,
                FrameType.Ack => false,
                FrameType.ConnectionCloseQuic => false,
                FrameType.ConnectionCloseApplication => false,
                _ => true
            };
        }

        private bool IsFrameAllowed(FrameType frameType, PacketType packetType)
        {
            return packetType switch
            {
                // 1-RTT packets may contain any frame, but HANDSHAKE_DONE can only be sent by server
                PacketType.OneRtt => frameType != FrameType.HandshakeDone || !_isServer,

                PacketType.Initial => frameType switch
                {
                    FrameType.Padding => true,
                    FrameType.Ping => true,
                    FrameType.Ack => true,
                    FrameType.AckWithEcn => true,
                    FrameType.Crypto => true,
                    FrameType.ConnectionCloseQuic => true,
                    _ => false
                },

                PacketType.ZeroRtt => frameType switch
                {
                    FrameType.Ack => false,
                    FrameType.AckWithEcn => false,
                    FrameType.Crypto => false,
                    FrameType.NewToken => false,
                    FrameType.ConnectionCloseQuic => false,
                    FrameType.ConnectionCloseApplication => false,
                    FrameType.HandshakeDone => false,
                    _ => true
                },

                PacketType.Handshake => frameType switch
                {
                    FrameType.Padding => true,
                    FrameType.Ping => true,
                    FrameType.Ack => true,
                    FrameType.AckWithEcn => true,
                    FrameType.Crypto => true,
                    FrameType.ConnectionCloseQuic => true,
                    _ => false
                },

                // these two types do not carry frames, and should never be passed to this function
                // PacketType.Retry,
                // PacketType.VersionNegotiation,
                _ => throw new ArgumentOutOfRangeException(nameof(packetType), packetType, null)
            };
        }

        private ProcessPacketResult ProcessFrames(QuicReader reader, PacketType packetType, RecvContext context)
        {
            bool handshakeWanted = false;

            while (reader.BytesLeft > 0)
            {
                var frameType = reader.PeekFrameType();

                if (!IsFrameAllowed(frameType, packetType))
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation, "Frame type not allowed", frameType);
                }

                if (IsAckEliciting(frameType))
                {
                    GetEpoch(GetEncryptionLevel(packetType)).AckElicited = true;
                }

                ProcessPacketResult result = ProcessPacketResult.Ok;
                switch (frameType)
                {
                    case FrameType.Padding:
                        // discard the padding
                        reader.ReadFrameType();
                        break;
                    case FrameType.Crypto:
                        handshakeWanted = true;
                        result = ProcessCryptoFrame(reader, packetType, context);
                        break;
                    case FrameType.Ping:
                        // ack will be elicited, nothing more to do
                        reader.ReadFrameType();
                        break;
                    case FrameType.Ack:
                        result = ProcessAckFrame(reader, packetType, context);
                        break;
                    case FrameType.AckWithEcn:
                    case FrameType.ResetStream:
                    case FrameType.StopSending:
                    case FrameType.NewToken:
                    case FrameType.Stream:
                    case FrameType.StreamMask:
                    case FrameType.MaxData:
                    case FrameType.MaxStreamData:
                    case FrameType.MaxStreamsBidirectional:
                    case FrameType.MaxStreamsUnidirectional:
                    case FrameType.DataBlocked:
                    case FrameType.StreamDataBlocked:
                    case FrameType.StreamsBlockedBidirectional:
                    case FrameType.StreamsBlockedUnidirectional:
                    case FrameType.NewConnectionId:
                    case FrameType.RetireConnectionId:
                    case FrameType.PathChallenge:
                    case FrameType.PathResponse:
                        throw new NotImplementedException();
                    case FrameType.ConnectionCloseQuic:
                    case FrameType.ConnectionCloseApplication:
                        result = ProcessConnectionClose(reader);
                        break;
                    case FrameType.HandshakeDone:
                        Debug.Assert(!_isServer);
                        _handshakeDoneReceived = true;
                        reader.ReadFrameType();
                        break;
                    default:
                        return CloseConnection(TransportErrorCode.FrameEncodingError, Internal.QuicError.UnknownFrameType, frameType);
                }

                switch (result)
                {
                    case ProcessPacketResult.Ok:
                        continue;
                    case ProcessPacketResult.ConnectionClose when outboundError == null:
                        outboundError = new QuicError(TransportErrorCode.FrameEncodingError,
                            "Unable to deserialize", frameType);
                        break;
                }

                return result;
            }

            // do handshake to set encryption secrets (to be able to process coalesced packets
            if (handshakeWanted)
            {
                DoHandshake();
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessConnectionClose(QuicReader reader)
        {
            if (!ConnectionCloseFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            inboundError = new QuicError((TransportErrorCode)frame.ErrorCode, frame.ReasonPhrase,
                frame.FrameType, frame.IsQuicError);
            return ProcessPacketResult.ConnectionClose; //TODO-RZ: Draining/closing state management
        }

        private ProcessPacketResult ProcessAckFrame(QuicReader reader, PacketType packetType, RecvContext context)
        {
            if (!AckFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            EpochData epoch = GetEpoch(GetEncryptionLevel(packetType));

            if (frame.LargestAcknowledged >= epoch.NextPacketNumber || // acking future packet
                frame.LargestAcknowledged < frame.FirstAckRange)       // acking negative PN
                return CloseConnection(TransportErrorCode.ProtocolViolation, Internal.QuicError.InvalidAckRange, FrameType.Ack);

            // TODO-RZ: check ackDelay
            Span<PacketNumberRange> ranges =
                stackalloc PacketNumberRange[(int)frame.AckRangeCount + 1];

            ranges[^1] = new PacketNumberRange(
                frame.LargestAcknowledged - frame.FirstAckRange, frame.LargestAcknowledged);

            int read = 0;
            // read the ranges in reverse order, so the `ranges` are in ascending order
            for (int i = (int)frame.AckRangeCount - 1; i > 0; i--)
            {
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong gap);
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong acked);

                if (ranges[i].Start < gap + acked - 2)
                {
                    return CloseConnection(TransportErrorCode.FrameEncodingError,
                        Internal.QuicError.InvalidAckRange, frame.HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack);
                }

                ranges[i - 1] = new PacketNumberRange(ranges[i].Start - gap - acked - 2, ranges[i].Start - gap - 2);
            }

            _recovery.OnRangesAcked(GetEpoch(packetType), ranges, TimeSpan.FromTicks((long)frame.AckDelay),
                context.Now);
            ProcessAckedPackets(epoch, ranges);

            return ProcessPacketResult.Ok;
        }

        private void ProcessAckedPackets(EpochData epoch, Span<PacketNumberRange> ranges)
        {
            // TODO-RZ: make this more efficient
            int rangeIndex = 0;

            var pnsInFlight = epoch.PacketsInFlight.Keys.ToArray();
            for (int i = 0; i < pnsInFlight.Length; i++)
            {
                ulong pn = pnsInFlight[i];
                while (ranges[rangeIndex].End < pn)
                {
                    rangeIndex++;
                    if (rangeIndex == ranges.Length)
                    {
                        // all ranges processed
                        break;
                    }
                }

                Debug.Assert(pn <= ranges[rangeIndex].End);
                if (pn < ranges[rangeIndex].Start)
                {
                    continue;
                }

                OnSentPacketAcked(epoch, epoch.PacketsInFlight[pn]);
                epoch.PacketsInFlight.Remove(pn);
            }
        }

        private void OnSentPacketAcked(EpochData epoch, SentPacket packet)
        {
            // mark all sent data as acked
            for (int i = 0; i < packet.CryptoRanges.Count; i++)
            {
                epoch.CryptoOutboundStream.OnAck(
                    packet.CryptoRanges[i].Start, packet.CryptoRanges[i].Length);
            }

            foreach (var (streamId, range) in packet.SentStreamData)
            {
                var buffer = GetStream(streamId).OutboundBuffer!;
                for (int i = 0; i < range.Count; i++)
                {
                    buffer.OnAck(range[i].Start, range[i].Length);
                }
            }

            // Since we know the acks arrived, we don't want to send acks for the packets acked by this frame.
            epoch.UnackedPacketNumbers.Remove(packet.AckedRanges);
        }

        private ProcessPacketResult ProcessCryptoFrame(QuicReader reader, PacketType packetType, RecvContext context)
        {
            if (!CryptoFrame.Read(reader, out var crypto)) return ProcessPacketResult.ConnectionClose;

            EncryptionLevel level = GetEncryptionLevel(packetType);
            var stream = GetEpoch(level).CryptoInboundBuffer;

            // don't buffer if not needed
            if (stream.BytesRead == crypto.Offset)
            {
                stream.Skip((ulong)crypto.CryptoData.Length);
                _tls.OnDataReceived(level, crypto.CryptoData);

                // process also buffered data received earlier
                if (stream.BytesAvailable > 0)
                {
                    // define a copy of level variable with smaller scope to prevent allocations in common case
                    EncryptionLevel level2 = level;
                    stream.Deliver(segment => { _tls.OnDataReceived(level2, segment); });
                }
            }
            else
            {
                stream.Receive(crypto.Offset, crypto.CryptoData);
            }

            return ProcessPacketResult.Ok;
        }

        private void WriteFrames(QuicWriter writer, PacketType packetType, EncryptionLevel level, SendContext context)
        {
            var epoch = GetEpoch(level);

            // TODO-RZ other frames

            if (outboundError != null)
            {
                WriteConnectionCloseFrame(writer, outboundError!);
                return;
            }

            if (writer.BytesAvailable > 0 && _isServer && !_handshakeDoneSent && packetType == PacketType.OneRtt && _tls.IsHandshakeComplete)
            {
                writer.WriteFrameType(FrameType.HandshakeDone);
                // no data
                context.SentPacket.HandshakeDoneSent = true;
                _handshakeDoneSent = true;
            }

            if (writer.BytesAvailable > 0 && _pingWanted)
            {
                writer.WriteFrameType(FrameType.Ping);
                // no data
                // TODO-RZ resend ping after loss?
                _pingWanted = false;
            }

            WriteAckFrame(writer, epoch, context);
            WriteCryptoFrames(writer, epoch, context);
        }

        private static void WriteConnectionCloseFrame(QuicWriter writer, QuicError error)
        {
            ConnectionCloseFrame.Write(writer,
                new ConnectionCloseFrame((ulong)error.ErrorCode,
                    error.IsQuicError,
                    error.FrameType,
                    error.ReasonPhrase));
        }

        private static void WriteCryptoFrames(QuicWriter writer, EpochData epoch, SendContext context)
        {
            // assume 2 * 2 bytes for offset and length and 1 B for type
            const int minSize = 5;
            while (writer.BytesAvailable > minSize)
            {
                if (!epoch.CryptoOutboundStream.HasPendingData)
                    return;

                (ulong offset, ulong count) = epoch.CryptoOutboundStream.GetNextSendableRange();

                count = Math.Min(count, (ulong) writer.BytesAvailable - minSize);
                epoch.CryptoOutboundStream.CheckOut(CryptoFrame.ReservePayloadBuffer(writer, offset, count));

                context.SentPacket.CryptoRanges.Add(offset, offset + count - 1);
            }
        }

        private static unsafe void WriteAckFrame(QuicWriter writer, EpochData epoch, SendContext context)
        {
            if (!epoch.AckElicited)
            {
                return; // no need for ack now
            }

            var ranges = epoch.UnackedPacketNumbers;

            Debug.Assert(ranges.Count > 0); // implied by AckElicited
            Debug.Assert(ranges.Count % 2 == 1); // sanity check

            // TODO-RZ check max ack delay to avoid sending acks every packet
            ulong ackDelay = (ulong) (context.Now - epoch.LargestReceivedPacketTimestamp).Ticks;

            ulong largest = ranges.GetMax();
            var firstRange = ranges[^1];

            int written = 0;
            int lengthEstimate = ranges.Count * 2 * 4;

            Span<byte> ackRangesRaw = lengthEstimate <= 512
                ? stackalloc byte[lengthEstimate]
                : new byte[lengthEstimate];

            ulong gapStart = largest - firstRange.Length;
            for (int i = ranges.Count - 2; i >= 0; i--)
            {
                // the numbers are always encoded as one lesser, meaning sending 0 means 1
                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written),
                    gapStart - ranges[i].End);
                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written),
                    ranges[i].End - ranges[i].Start);
                gapStart = ranges[i].Start - 1;
            }

            // record that the ranges have been sent
            context.SentPacket.AckedRanges.Add(ranges);

            // TODO-RZ implement ECN counts
            AckFrame.Write(writer,
                new AckFrame(largest, ackDelay, (ulong)(ranges.Count - 1) / 2, firstRange.Length - 1, ReadOnlySpan<byte>.Empty,
                    false, 0, 0, 0));

            epoch.AckElicited = false;
        }
    }
}
