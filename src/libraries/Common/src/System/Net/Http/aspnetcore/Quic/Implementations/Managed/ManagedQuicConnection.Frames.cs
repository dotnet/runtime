#nullable enable

using System.Diagnostics;
using System.Linq;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Buffers;
using System.Net.Quic.Implementations.Managed.Internal.Frames;

namespace System.Net.Quic.Implementations.Managed
{
    /// <summary>
    ///     Helper class for managing timestamps.
    /// </summary>
    internal static class Timestamp
    {
        public static long Now => Stopwatch.GetTimestamp();

        public static long FromMilliseconds(long milliseconds) => TimeSpan.TicksPerMillisecond * milliseconds;
        public static long FromMicroseconds(long microseconds) => TimeSpan.TicksPerMillisecond * 1000 * microseconds;

        public static long GetMilliseconds(long timeDiff) => timeDiff / TimeSpan.TicksPerMillisecond;
        public static long GetMicroseconds(long timeDiff) => timeDiff / (TimeSpan.TicksPerMillisecond * 1000);
    }

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

        private ProcessPacketResult DiscardFrameType(QuicReader reader)
        {
            reader.ReadFrameType();
            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult DiscardPadding(QuicReader reader)
        {
            while (reader.BytesLeft > 0 && reader.Peek() == 0)
            {
                reader.ReadUInt8();
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessFrames(QuicReader reader, PacketType packetType, RecvContext context)
        {
            while (reader.BytesLeft > 0)
            {
                var frameType = reader.PeekFrameType();

                if (!IsFrameAllowed(frameType, packetType))
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation, "Frame type not allowed", frameType);
                }

                if (IsAckEliciting(frameType))
                {
                    GetPacketNumberSpace(GetEncryptionLevel(packetType)).AckElicited = true;
                }

                ProcessPacketResult result = frameType switch
                {
                    FrameType.Padding => DiscardPadding(reader),
                    FrameType.Ping => DiscardFrameType(reader),
                    FrameType.Ack => ProcessAckFrame(reader, packetType, context),
                    FrameType.AckWithEcn => ProcessAckFrame(reader, packetType, context),
                    FrameType.StopSending => throw new NotImplementedException(),
                    FrameType.Crypto => ProcessCryptoFrame(reader, packetType, context),
                    FrameType.NewToken => throw new NotImplementedException(),
                    FrameType.MaxData => ProcessMaxDataFrame(reader),
                    FrameType.MaxStreamData => ProcessMaxStreamDataFrame(reader),
                    FrameType.MaxStreamsBidirectional => ProcessMaxStreamsFrame(reader),
                    FrameType.MaxStreamsUnidirectional => ProcessMaxStreamsFrame(reader),
                    FrameType.DataBlocked => throw new NotImplementedException(),
                    FrameType.StreamDataBlocked => throw new NotImplementedException(),
                    FrameType.StreamsBlockedBidirectional => throw new NotImplementedException(),
                    FrameType.StreamsBlockedUnidirectional => throw new NotImplementedException(),
                    FrameType.NewConnectionId => throw new NotImplementedException(),
                    FrameType.RetireConnectionId => throw new NotImplementedException(),
                    FrameType.PathChallenge => throw new NotImplementedException(),
                    FrameType.PathResponse => throw new NotImplementedException(),
                    FrameType.ConnectionCloseQuic => ProcessConnectionClose(reader),
                    FrameType.ConnectionCloseApplication => ProcessConnectionClose(reader),
                    FrameType.HandshakeDone => ProcessHandshakeDoneFrame(reader),
                    _ when (frameType & FrameType.StreamMask) == frameType => ProcessStreamFrame(reader),
                    _ => CloseConnection(TransportErrorCode.FrameEncodingError, QuicError.UnknownFrameType, frameType)
                };

                switch (result)
                {
                    case ProcessPacketResult.Ok:
                        continue;
                    case ProcessPacketResult.ConnectionClose when outboundError == null:
                        outboundError = new QuicError(TransportErrorCode.FrameEncodingError,
                            QuicError.UnableToDeserialize, frameType);
                        break;
                }

                return result;
            }

            // do handshake to set encryption secrets (to be able to process coalesced packets)
            if (context.HandshakeWanted)
            {
                DoHandshake();
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessHandshakeDoneFrame(QuicReader reader)
        {
            Debug.Assert(!_isServer); // frame not allowed handled elsewhere
            reader.ReadFrameType();
            _handshakeDoneReceived = true;
            if (!_isServer)
            {
                _connectTcs.Complete(0);
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessMaxStreamDataFrame(QuicReader reader)
        {
            if (!MaxStreamDataFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            if (!StreamHelpers.IsReadable(_isServer, frame.StreamId))
                // TODO-RZ: check stream state
                return CloseConnection(TransportErrorCode.StreamStateError,
                    QuicError.NotInRecvState, FrameType.MaxStreamData);

            throw new NotImplementedException();
        }

        private ProcessPacketResult ProcessMaxStreamsFrame(QuicReader reader)
        {
            if (!MaxStreamsFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            if (frame.Bidirectional)
                _peerLimits.UpdateMaxStreamsBidi(frame.MaximumStreams);
            else
                _peerLimits.UpdateMaxStreamsUni(frame.MaximumStreams);

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessMaxDataFrame(QuicReader reader)
        {
            if (!MaxDataFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            _peerLimits.UpdateMaxData(frame.MaximumData);
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

            PacketNumberSpace pnSpace = GetPacketNumberSpace(GetEncryptionLevel(packetType));

            if (frame.LargestAcknowledged >= pnSpace.NextPacketNumber || // acking future packet
                frame.LargestAcknowledged < frame.FirstAckRange)       // acking negative PN
                return CloseConnection(TransportErrorCode.ProtocolViolation, QuicError.InvalidAckRange, FrameType.Ack);

            // TODO-RZ: check ackDelay
            Span<PacketNumberRange> ranges =
                stackalloc PacketNumberRange[(int)frame.AckRangeCount + 1];

            // TODO-RZ: it is really unnecessary to have yet another class for ranges
            ranges[^1] = new PacketNumberRange(
                frame.LargestAcknowledged - frame.FirstAckRange, frame.LargestAcknowledged);

            int read = 0;

            long prevSmallestAcked = ranges[^1].Start;

            // read the ranges in reverse order, so the `ranges` are in ascending order
            for (int i = (int)frame.AckRangeCount; i > 0; i--)
            {
                read += QuicPrimitives.TryReadVarInt(frame.AckRangesRaw.Slice(read), out long gap);
                read += QuicPrimitives.TryReadVarInt(frame.AckRangesRaw.Slice(read), out long acked);

                // the numbers are always encoded as one lesser, meaning sending 0 in gap means actually 1,
                // implying that     nextLargestAcked = prevSmallestAck - gap - 2

                long nextLargestAcked = prevSmallestAcked - gap - 2;
                long nextSmallestAcked = nextLargestAcked - acked;

                if (nextLargestAcked < 0)
                {
                    return CloseConnection(TransportErrorCode.FrameEncodingError,
                        QuicError.InvalidAckRange, frame.HasEcnCounts ? FrameType.AckWithEcn : FrameType.Ack);
                }

                ranges[i - 1] = new PacketNumberRange(nextSmallestAcked, nextLargestAcked);
                prevSmallestAcked = nextSmallestAcked;
            }

            var space = GetPacketSpace(packetType);
            long ackDelay = Timestamp.FromMicroseconds(frame.AckDelay * (1 << (int) _peerTransportParameters.AckDelayExponent));
            Recovery.OnAckReceived(space, ranges, ackDelay, frame, context.Timestamp, _tls.IsHandshakeComplete);

            var ackedPackets = Recovery.GetAckedPackets(space);
            foreach (SentPacket packet in ackedPackets)
            {
                OnPacketAcked(packet, pnSpace);
            }
            ackedPackets.Clear();

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessCryptoFrame(QuicReader reader, PacketType packetType, RecvContext context)
        {
            if (!CryptoFrame.Read(reader, out var crypto)) return ProcessPacketResult.ConnectionClose;

            EncryptionLevel level = GetEncryptionLevel(packetType);
            var stream = GetPacketNumberSpace(level).CryptoInboundBuffer;

            // don't buffer if not needed
            if (stream.BytesRead == crypto.Offset)
            {
                stream.Skip(crypto.CryptoData.Length);
                _tls.OnDataReceived(level, crypto.CryptoData);

                // process also buffered data received earlier
                if (stream.BytesAvailable > 0)
                {
                    // define a copy of level variable with smaller scope to prevent allocations in common case
                    EncryptionLevel level2 = level;
                    stream.Deliver(segment => { _tls.OnDataReceived(level2, segment.Span); });
                }

                context.HandshakeWanted = true;
            }
            else
            {
                stream.Receive(crypto.Offset, crypto.CryptoData);
            }

            return ProcessPacketResult.Ok;
        }

        private ProcessPacketResult ProcessStreamFrame(QuicReader reader)
        {
            if (!StreamFrame.Read(reader, out var frame))
                return ProcessPacketResult.ConnectionClose;

            bool bidirectional = StreamHelpers.IsBidirectional(frame.StreamId);
            long index = StreamHelpers.GetStreamIndex(frame.StreamId);
            long limit = bidirectional
                ? _localLimits.MaxStreamsBidi
                : _localLimits.MaxStreamsUni;

            if (index > limit)
            {
                // Flow control violated
                return CloseConnection(TransportErrorCode.StreamLimitError, QuicError.StreamsLimitExceeded,
                    FrameType.Stream);
            }

            var stream = _streams.GetOrCreateStream(frame.StreamId, _localTransportParameters, _peerTransportParameters, _isServer, _socketContext);
            if (stream.InboundBuffer == null)
            {
                // Flow trying to write into receive only stream
                return CloseConnection(TransportErrorCode.StreamStateError, QuicError.StreamNotWritable,
                    FrameType.Stream);
            }

            var buffer = stream.InboundBuffer!;

            if (frame.Fin)
            {
                long finalSize = frame.Offset + frame.StreamData.Length;

                if (buffer.FinalSize != null && buffer.FinalSize != finalSize ||
                    buffer.EstimatedSize > finalSize)
                {
                    return CloseConnection(TransportErrorCode.FinalSizeError, QuicError.InconsistentFinalSize);
                }
            }

            if (buffer.FinalSize != null && frame.Offset + frame.StreamData.Length > buffer.FinalSize)
            {
                return CloseConnection(TransportErrorCode.FinalSizeError, QuicError.WritingPastFinalSize);
            }

            buffer.Receive(frame.Offset, frame.StreamData, frame.Fin);

            return ProcessPacketResult.Ok;
        }

        private void WriteFrames(QuicWriter writer, PacketType packetType, EncryptionLevel level, SendContext context)
        {
            var pnSpace = GetPacketNumberSpace(level);

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

            WriteAckFrame(writer, pnSpace, context);
            WriteCryptoFrames(writer, pnSpace, context);

            if (packetType == PacketType.OneRtt)
            {
                WriteStreamFrames(writer, context);
            }
        }

        private static void WriteConnectionCloseFrame(QuicWriter writer, QuicError error)
        {
            ConnectionCloseFrame.Write(writer,
                new ConnectionCloseFrame((long)error.ErrorCode,
                    error.IsQuicError,
                    error.FrameType,
                    error.ReasonPhrase));
        }

        private static void WriteCryptoFrames(QuicWriter writer, PacketNumberSpace pnSpace, SendContext context)
        {
            // assume 2 * 2 bytes for offset and length and 1 B for type
            const int minSize = 5;
            while (writer.BytesAvailable > minSize)
            {
                if (!pnSpace.CryptoOutboundStream.IsFlushable)
                    return;

                (long offset, long count) = pnSpace.CryptoOutboundStream.GetNextSendableRange();

                count = Math.Min(count, (long) writer.BytesAvailable - minSize);
                pnSpace.CryptoOutboundStream.CheckOut(CryptoFrame.ReservePayloadBuffer(writer, offset, count));

                context.SentPacket.SentStreamData.Add(
                    SentPacket.StreamChunkInfo.ForCryptoStream(offset, offset + count - 1));
            }
        }

        private void WriteAckFrame(QuicWriter writer, PacketNumberSpace pnSpace, SendContext context)
        {
            var ranges = pnSpace.UnackedPacketNumbers;

            if (ranges.Count == 0)
            {
                return;
            }

            // if (!pnSpace.AckElicited)
            // {
                // return; // no need for ack now
            // }
            // Debug.Assert(ranges.Count > 0); // implied by AckElicited

            // TODO-RZ check max ack delay to avoid sending acks every packet?
            long ackDelay = Timestamp.GetMicroseconds(context.Timestamp - pnSpace.LargestReceivedPacketTimestamp) >>
                            (int) _localTransportParameters.AckDelayExponent;

            long largest = ranges.GetMax();
            var firstRange = ranges[^1];

            int written = 0;
            int lengthEstimate = ranges.Count * 2 * 4;

            Span<byte> ackRangesRaw = lengthEstimate <= 512
                ? stackalloc byte[lengthEstimate]
                : new byte[lengthEstimate];

            long prevSmallestAcked = firstRange.Start;

            for (int i = ranges.Count - 2; i >= 0; i--)
            {
                var range = ranges[i];

                long nextLargestAcked = range.End;

                // the numbers are always encoded as one lesser, meaning sending 0 in gap means actually 1,
                // implying that     nextLargestAcked = prevSmallestAck - gap - 2

                long gap = prevSmallestAcked - nextLargestAcked - 2;
                long ack = range.Length - 1;

                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written), gap);
                written += QuicPrimitives.WriteVarInt(ackRangesRaw.Slice(written), ack);
                prevSmallestAcked = ranges[i].Start;
            }

            // record that the ranges have been sent
            context.SentPacket.AckedRanges.Add(ranges);

            // TODO-RZ implement ECN counts
            AckFrame.Write(writer,
                new AckFrame(largest, ackDelay, ranges.Count - 1,
                    firstRange.Length - 1, ackRangesRaw.Slice(0, written),
                    false, 0, 0, 0));

            pnSpace.AckElicited = false;
        }

        private void WriteStreamFrames(QuicWriter writer, SendContext context)
        {
            ManagedQuicStream? stream;
            while (writer.BytesAvailable > StreamFrame.MinSize && (stream = _streams.GetFirstFlushableStream()) != null)
            {
                var buffer = stream!.OutboundBuffer!;

                if (!buffer.IsFlushable && !buffer.SizeKnown)
                {
                    // race condition, should not happen terribly often
                    continue;
                }

                (long offset, long count) = buffer.GetNextSendableRange();
                int overhead = StreamFrame.GetOverheadLength(stream!.StreamId, offset, Math.Min(count, writer.BytesAvailable));

                count = Math.Min(count,  writer.BytesAvailable - overhead);

                // if size is known, WrittenBytes is no longer mutable
                bool fin = buffer.SizeKnown && buffer.WrittenBytes == offset + count;

                if (count > 0 || fin)
                {
                    var data = StreamFrame.ReservePayloadBuffer(writer, stream!.StreamId, offset, (int)count, fin);

                    if (count > 0)
                    {
                        buffer.CheckOut(data);
                    }

                    context.SentPacket.SentStreamData.Add(
                        new SentPacket.StreamChunkInfo(stream!.StreamId, offset, count, fin));
                }

                if (buffer.IsFlushable)
                {
                    _streams.MarkFlushable(stream!);
                }

                if (count <= 0)
                {
                    // no more data could fit into the packet.
                    break;
                }
            }
        }

        private void OnPacketAcked(SentPacket packet, PacketNumberSpace pnSpace)
        {
            // mark all sent data as acked
            foreach (var data in packet.SentStreamData)
            {
                if (data.IsCryptoStream)
                {
                    pnSpace.CryptoOutboundStream.OnAck(data.Offset, data.Count);
                }
                else
                {
                    // empty frames are sent only to send the FIN bit
                    Debug.Assert(data.Count > 0 || data.Fin);

                    var stream = _streams[data.StreamId];
                    var buffer = stream.OutboundBuffer!;
                    buffer.OnAck(data.Offset, data.Count, data.Fin);
                    if (buffer.Finished)
                    {
                        stream.NotifyShutdownWriteCompleted();
                    }
                }
            }

            // Since we know the acks arrived, we don't want to send acks sent by this packet anymore.
            pnSpace.UnackedPacketNumbers.Remove(packet.AckedRanges);
        }
    }
}
