#nullable enable

using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed
{
    internal partial class ManagedQuicConnection
    {
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

        private static bool IsFrameAllowed(FrameType frameType, PacketType packetType)
        {
            return packetType switch
            {
                // 1-RTT packets may contain any frame
                PacketType.OneRtt => true,

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

        private ProcessPacketResult ProcessFrames(QuicReader reader, PacketType packetType)
        {
            bool handshakeWanted = false;

            while (reader.BytesLeft > 0)
            {
                var frameType = reader.PeekFrameType();

                if (!IsFrameAllowed(frameType, packetType))
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation, frameType, "Frame type not allowed");
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
                        result = ProcessCryptoFrame(reader, packetType);
                        break;
                    case FrameType.Ping:
                    case FrameType.Ack:
                        result = ProcessAckFrame(reader, packetType);
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
                        throw new NotImplementedException();
                    default:
                        // unknown frame type
                        return CloseConnection(TransportErrorCode.FrameEncodingError, null, "Unknown frame type");
                }

                switch (result)
                {
                    case ProcessPacketResult.Ok:
                        continue;
                    case ProcessPacketResult.ConnectionClose when outboundError == null:
                        outboundError = new QuicError(TransportErrorCode.FrameEncodingError, frameType,
                            "Unable to deserialize");
                        break;
                }

                return result;
            }

            // do handshake to set encryption secrets (to be able to process coalesced packets
            if (handshakeWanted)
            {
                _tls.DoHandshake();
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessConnectionClose(QuicReader reader)
        {
            if (!ConnectionCloseFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            inboundError = new QuicError((TransportErrorCode)frame.ErrorCode, frame.FrameType, frame.ReasonPhrase,
                frame.IsQuicError);
            return ProcessPacketResult.ConnectionClose; //TODO-RZ:
        }

        private ProcessPacketResult ProcessAckFrame(QuicReader reader, PacketType packetType)
        {
            if (!AckFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            // TODO-RZ: check validity of the frame
            Span<PacketNumberRange> ranges =
                stackalloc PacketNumberRange[(int)frame.AckRangeCount + 1];

            ranges[0] = new PacketNumberRange(
                frame.LargestAcknowledged - frame.FirstAckRange, frame.LargestAcknowledged);

            int read = 0;
            for (int i = 0; i < (int)frame.AckRangeCount; i++)
            {
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong gap);
                read += QuicPrimitives.ReadVarInt(frame.AckRangesRaw.Slice(read), out ulong acked);

                if (ranges[i].Start < gap + acked)
                {
                    return CloseConnection(TransportErrorCode.FrameEncodingError,
                        frame.HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack,
                        "Negative PN acked");
                }

                ranges[i + 1] = new PacketNumberRange(ranges[i].Start - gap - acked - 2, ranges[i].Start - gap - 2);
            }

            // TODO-RZ: maintain current date-time throughout processing the frame
            _recovery.OnRangeAcked(GetEpoch(packetType), ranges, TimeSpan.FromTicks((long)frame.AckDelay),
                DateTime.Now);

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessCryptoFrame(QuicReader reader, PacketType packetType)
        {
            if (!CryptoFrame.Read(reader, out var crypto)) return ProcessPacketResult.ConnectionClose;
            // TODO-RZ: Utilize the offset
            _tls.OnDataReceived(GetEncryptionLevel(packetType), crypto.CryptoData);

            return ProcessPacketResult.Ok;
        }

        private void WriteFrames(QuicWriter writer, PacketType packetType, EncryptionLevel level)
        {
            var epoch = GetEpoch(level);

            // TODO-RZ other frames
            if (outboundError != null)
            {
                WriteConnectionCloseFrame(writer, outboundError!);
                return;
            }

            WriteAckFrame(writer, epoch);
            WriteCryptoFrames(writer, epoch);
        }

        private static void WriteConnectionCloseFrame(QuicWriter writer, QuicError error)
        {
            ConnectionCloseFrame.Write(writer,
                new ConnectionCloseFrame((ulong)error.ErrorCode,
                    error.IsQuicError,
                    error.FrameType ?? FrameType.Padding, // use 0x00 (same as padding) when frame type unknown
                    error.ReasonPhrase));
        }

        private static void WriteCryptoFrames(QuicWriter writer, EpochData epoch)
        {
            while (epoch.CryptoStream.NextSizeToSend > 0 && epoch.CryptoStream.NextSizeToSend < writer.BytesAvailable)
            {
                var (data, offset) = epoch.CryptoStream.PeekDataToSend();
                CryptoFrame frame = new CryptoFrame((ulong)offset, data);
                if (writer.BytesAvailable < frame.GetSerializedLength())
                {
                    // cannot fit in this frame
                    break;
                }

                CryptoFrame.Write(writer, frame);
                epoch.CryptoStream.GetDataToSend();
            }
        }

        private static unsafe void WriteAckFrame(QuicWriter writer, EpochData epoch)
        {
            if (!epoch.AckElicited)
            {
                return; // no need for ack now
            }

            var ranges = epoch.UnackedPacketNumbers;

            Debug.Assert(ranges.Count > 0); // implied by AckElicited
            Debug.Assert(ranges.Count % 2 == 1); // sanity check

            // TODO-RZ generate AckDelay
            // TODO-RZ check max ack delay to avoid sending acks every packet
            ulong ackDelay = 0ul;

            ulong largest = ranges.GetMax();
            var firstRange = ranges[^1];
            ulong firstRangeLen = firstRange.Start - firstRange.End;

            // TODO-RZ fallback to heap alloc
            int written = 0;
            Span<byte> ackRangesRaw = stackalloc byte[128];
            ulong gapStart = largest - firstRangeLen;
            for (int i = ranges.Count - 2; i >= 0; i--)
            {
                // the numbers are always encoded as one lesser, meaning sending 0 means 1
                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written),
                    gapStart - ranges[i].End);
                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written),
                    ranges[i].End - ranges[i].Start);
                gapStart = ranges[i].Start - 1;
            }

            // TODO-RZ implement ECN counts
            AckFrame.Write(writer,
                new AckFrame(largest, ackDelay, (ulong)(ranges.Count - 1) / 2, firstRangeLen, ReadOnlySpan<byte>.Empty,
                    false, 0, 0, 0));

            epoch.AckElicited = false;
        }
    }
}
