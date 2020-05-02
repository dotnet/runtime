using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Headers;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed partial class ManagedQuicConnection
    {
        internal void ReceiveData(QuicReader reader, IPEndPoint sender, QuicSocketContext.RecvContext ctx)
        {
            if (_closingPeriodEnd != null)
            {
                // discard any incoming data
                return;
            }

            var buffer = reader.Buffer;

            while (reader.BytesLeft > 0)
            {
                var status = ReceiveOne(reader, ctx);

                switch (status)
                {
                    case ProcessPacketResult.DropPacket:
                        Console.WriteLine("Packet dropped");
                        break;
                    case ProcessPacketResult.Ok:
                        // An endpoint restarts its idle timer when a packet from its peer is
                        // received and processed successfully.
                        _ackElicitingSentSinceLastReceive = false;
                        RestartIdleTimer(ctx.Timestamp);
                        break;
                }

                // ReceiveOne will adjust the buffer length once it is known, thus the length here skips the
                // just processed coalesced packet
                buffer = buffer.Slice(reader.Buffer.Length);
                reader.Reset(buffer);
            }
        }

        private ProcessPacketResult ReceiveOne(QuicReader reader, QuicSocketContext.RecvContext context)
        {
            byte first = reader.Peek();

            ProcessPacketResult result;
            if (HeaderHelpers.IsLongHeader(first))
            {
                if (!LongPacketHeader.Read(reader, out var header) ||
                    // clients SHOULD ignore fixed bit when receiving version negotiation
                    !header.FixedBit && _isServer && header.PacketType == PacketType.VersionNegotiation ||
                    // packet is not meant for us after all
                    SourceConnectionId != null &&
                    !header.DestinationConnectionId.SequenceEqual(SourceConnectionId!.Data))
                {
                    return ProcessPacketResult.DropPacket;
                }

                result = ReceiveLongHeaderPackets(reader, header, context);
            }

            else
            {
                if (!ShortPacketHeader.Read(reader, _localConnectionIdCollection, out var header) ||
                    !header.FixedBit)
                {
                    return ProcessPacketResult.DropPacket;
                }

                result = Receive1Rtt(reader, header, context);
            }

            return result;
        }

        private ProcessPacketResult ReceiveLongHeaderPackets(QuicReader reader, in LongPacketHeader header,
            QuicSocketContext.RecvContext context)
        {
            var type = header.PacketType;

            // TODO-RZ: Check that connection IDs match and have correct length (not too long)

            switch (type)
            {
                case PacketType.Initial:
                // TODO-RZ: server must not send Token (Protocol violation)
                case PacketType.Handshake:
                case PacketType.ZeroRtt:
                    if (!SharedPacketData.Read(reader, header.FirstByte, out var headerData) ||
                        headerData.Length > reader.BytesLeft)
                    {
                        return ProcessPacketResult.DropPacket;
                    }

                    // total length of the packet is known and checked during header parsing.
                    // Adjust the buffer to the range belonging to the current packet.
                    reader.Reset(reader.Buffer.Slice(0, reader.BytesRead + (int)headerData.Length), reader.BytesRead);

                    return ReceiveCommon(reader, header, headerData, context);
                case PacketType.Retry:
                    return ReceiveRetry(reader, header, context);
                case PacketType.VersionNegotiation:
                    return ReceiveVersionNegotiation(reader, header, context);
                case PacketType.OneRtt:
                    // this type is handled elsewhere
                    throw new InvalidOperationException("Unreachable");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ProcessPacketResult ReceiveRetry(QuicReader reader, in LongPacketHeader header, QuicSocketContext.RecvContext context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveVersionNegotiation(QuicReader reader, in LongPacketHeader header,
            QuicSocketContext.RecvContext context)
        {
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveCommon(QuicReader reader, in LongPacketHeader header,
            in SharedPacketData headerData, QuicSocketContext.RecvContext context)
        {
            //TODO-RZ: Version negotiation
            //TODO-RZ: Check connection id length (beware that greater length is allowed in initial packets)

            int pnOffset = reader.BytesRead;
            var pnSpace = GetPacketNumberSpace(GetEncryptionLevel(header.PacketType));
            int payloadLength = (int)headerData.Length;
            PacketType packetType = header.PacketType;

            if (_isServer && packetType == PacketType.Initial)
            {
                if (pnSpace.RecvCryptoSeal == null)
                {
                    // initialize protection keys
                    // clients destination connection Id is ours source connection Id
                    SourceConnectionId = new ConnectionId(header.DestinationConnectionId.ToArray());
                    DestinationConnectionId = new ConnectionId(header.SourceConnectionId.ToArray());

                    _localConnectionIdCollection.Add(SourceConnectionId);
                    DeriveInitialProtectionKeys(SourceConnectionId.Data);
                }

                // check UDP datagram size, by now the reader's buffer end is aligned with the UDP datagram end.
                // TODO-RZ: in rare cases when initial is not the first of the coalesced packets this can falsely close the connection.
                // as the QUIC does only recommend, not mandate order of the coalesced packets
                if (reader.Buffer.Length < QuicConstants.MinimumClientInitialDatagramSize)
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation,
                        QuicError.InitialPacketTooShort);
                }
            }

            return ReceiveProtectedFrames(reader, pnSpace, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult Receive1Rtt(QuicReader reader, in ShortPacketHeader header, QuicSocketContext.RecvContext context)
        {
            int pnOffset = reader.BytesRead;
            PacketType packetType = PacketType.OneRtt;
            var pnSpace = GetPacketNumberSpace(EncryptionLevel.Application);
            int payloadLength = reader.BytesLeft;

            return ReceiveProtectedFrames(reader, pnSpace, pnOffset, payloadLength, packetType, context);
        }

        private ProcessPacketResult ReceiveProtectedFrames(QuicReader reader, PacketNumberSpace pnSpace, int pnOffset,
            int payloadLength,
            PacketType packetType, QuicSocketContext.RecvContext context)
        {
            if (pnSpace.RecvCryptoSeal == null)
            {
                // Decryption keys are not available yet, drop the packet for now
                // TODO-RZ: consider buffering the packet
                return ProcessPacketResult.DropPacket;
            }

            var seal = pnSpace.RecvCryptoSeal!;

            if (!seal.DecryptPacket(reader.Buffer.Span, pnOffset, payloadLength,
                pnSpace.LargestReceivedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return ProcessPacketResult.DropPacket;
            }

            // TODO-RZ: read in a better way
            int pnLength = HeaderHelpers.GetPacketNumberLength(reader.Buffer.Span[0]);
            reader.TryReadTruncatedPacketNumber(pnLength, out int truncatedPn);

            long packetNumber = QuicPrimitives.DecodePacketNumber(pnSpace.LargestReceivedPacketNumber,
                truncatedPn, pnLength);

            if (pnSpace.ReceivedPacketNumbers.Contains(packetNumber))
            {
                // already processed or outside congestion window
                // TODO-RZ: there may be false positives if the packet number is 64 lesser than largest received
                return ProcessPacketResult.Ok;
            }

            if (pnSpace.LargestReceivedPacketNumber < packetNumber)
            {
                pnSpace.LargestReceivedPacketNumber = packetNumber;
                pnSpace.LargestReceivedPacketTimestamp = context.Timestamp;
            }

            pnSpace.UnackedPacketNumbers.Add(packetNumber);
            pnSpace.ReceivedPacketNumbers.Add(packetNumber);

            return ProcessFramesWithoutTag(reader, packetType, context);
        }

        private ProcessPacketResult ProcessFramesWithoutTag(QuicReader reader, PacketType packetType,
            QuicSocketContext.RecvContext context)
        {
            // HACK: we do not want to try processing the AEAD integrity tag as if it were frames.
            var originalSegment = reader.Buffer;
            int originalBytesRead = reader.BytesRead;
            int tagLength = GetPacketNumberSpace(GetEncryptionLevel(packetType)).RecvCryptoSeal!.TagLength;
            int length = reader.BytesLeft - tagLength;
            reader.Reset(originalSegment.Slice(originalBytesRead, length));
            var retval = ProcessFrames(reader, packetType, context);
            reader.Reset(originalSegment);
            return retval;
        }

        internal void SendData(QuicWriter writer, out IPEndPoint? receiver, QuicSocketContext.SendContext ctx)
        {
            receiver = _remoteEndpoint;

            if(ctx.Timestamp > _closingPeriodEnd)
            {
                SignalConnectionClose();
                return;
            }

            if (ctx.Timestamp > _idleTimeout)
            {
                // TODO-RZ: Force close the connection with error
                SignalConnectionClose();
            }

            if (_isDraining)
            {
                // While otherwise identical to the closing state, an endpoint in the draining state MUST NOT
                // send any packets
                return;
            }

            if (ctx.Timestamp >= Recovery.LossRecoveryTimer)
            {
                Recovery.OnLossDetectionTimeout(_tls.IsHandshakeComplete, ctx.Timestamp);
            }

            var level = GetWriteLevel();
            var origMemory = writer.Buffer;
            int written = 0;

            while (true)
            {
                if (GetPacketNumberSpace(level).SendCryptoSeal == null)
                {
                    // Secrets have not been derived yet, can't send anything
                    break;
                }

                if (SendOne(writer, level, ctx))
                {
                    ctx.StartNextPacket();
                }
                else
                {
                    // no more data to send.
                    ctx.SentPacket.Reset();
                    break;
                }

                written += writer.BytesWritten;

                // 0-RTT packets do not have Length, so they may not be coalesced
                if (level == EncryptionLevel.Application)
                    break;

                var nextLevel = GetWriteLevel();

                // only coalesce packets in ascending encryption level
                if (nextLevel <= level)
                    break;

                level = nextLevel;
                writer.Reset(writer.Buffer.Slice(writer.BytesWritten));
            }

            writer.Reset(origMemory, written);
        }

        private bool SendOne(QuicWriter writer, EncryptionLevel level, QuicSocketContext.SendContext context)
        {
            (PacketType packetType, PacketSpace packetSpace) = level switch
            {
                EncryptionLevel.Initial => (PacketType.Initial, PacketSpace.Initial),
                EncryptionLevel.EarlyData => (PacketType.ZeroRtt, PacketSpace.Application),
                EncryptionLevel.Handshake => (PacketType.Handshake, PacketSpace.Handshake),
                EncryptionLevel.Application => (PacketType.OneRtt, PacketSpace.Application),
                _ => throw new InvalidOperationException()
            };

            var pnSpace = GetPacketNumberSpace(level);
            var recoverySpace = Recovery.GetPacketNumberSpace(packetSpace);
            var seal = pnSpace.SendCryptoSeal!;

            // process lost packets
            var lostPackets = recoverySpace.LostPackets;
            while (lostPackets.TryDequeue(out var lostPacket))
            {
                OnPacketLost(lostPacket, pnSpace);
                context.ReturnPacket(lostPacket);
            }

            (int truncatedPn, int pnLength) = pnSpace.GetNextPacketNumber(recoverySpace.LargestAckedPacketNumber);
            WritePacketHeader(writer, packetType, pnLength);

            // for non 1-RTT packets, we reserve 2 bytes which we will overwrite once total payload length is known
            var payloadLengthSpan = writer.Buffer.Span.Slice(writer.BytesWritten - 2, 2);

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

            int maxPacketLength = (int)(_tls.IsHandshakeComplete
                // Limit maximum size so that it can be always encoded into the reserved 2 bytes of `payloadLengthSpan`
                ? Math.Min((1 << 14) - 1, _peerTransportParameters.MaxPacketSize)
                // use minimum size for packets during handshake
                : QuicConstants.MinimumClientInitialDatagramSize);

            bool isProbePacket = recoverySpace.RemainingLossProbes > 0;

            // make sure we send something if a probe is wanted
            _pingWanted |= isProbePacket;

            // TODO-RZ: Although ping should always work, the actual algorithm for probe packet is following
            // if (!isServer && GetPacketNumberSpace(EncryptionLevel.Application).RecvCryptoSeal == null)
            // {
            // TODO-RZ: Client needs to send an anti-deadlock packet:
            // }
            // else
            // {
            // TODO-RZ: PTO. Send new data if available, else retransmit old data.
            // If neither is available, send single PING frame.
            // }

            // limit outbound packet by available congestion window
            // probe packets are not limited by congestion window
            if (!isProbePacket)
            {
                maxPacketLength = Math.Min(maxPacketLength, Recovery.GetAvailableCongestionWindowBytes());
            }

            if (maxPacketLength <= seal.TagLength)
            {
                // unable to send any useful data anyway.
                return false;
            }

            int written = writer.BytesWritten;
            var origBuffer = writer.Buffer;

            writer.Reset(origBuffer.Slice(0, Math.Min(origBuffer.Length, maxPacketLength - seal.TagLength)), written);
            WriteFrames(writer, packetType, level, context);
            writer.Reset(origBuffer, writer.BytesWritten);

            if (writer.BytesWritten == written)
            {
                // no data to send
                // TODO-RZ: we might be able to detect this sooner
                writer.Reset(writer.Buffer);
                Debug.Assert(!_pingWanted);
                return false;
            }

            if (!_isServer && packetType == PacketType.Initial)
            {
                // TODO-RZ: It would be more efficient to add padding only to the last packet sent when coalescing packets.

                // Pad client initial packets to the minimum size
                int paddingLength = QuicConstants.MinimumClientInitialDatagramSize - seal.TagLength -
                                    writer.BytesWritten;
                if (paddingLength > 0)
                    // zero bytes are equivalent to PADDING frames
                    writer.GetWritableSpan(paddingLength).Clear();

                context.SentPacket.InFlight = true; // padding implies InFlight
            }

            // pad the packet payload so that it can always be sampled for header protection
            if (writer.BytesWritten - pnOffset < seal.PayloadSampleLength + 4)
            {
                writer.GetWritableSpan(seal.PayloadSampleLength + 4 - writer.BytesWritten + pnOffset).Clear();
                context.SentPacket.InFlight = true; // padding implies InFlight
            }

            // reserve space for AEAD integrity tag
            writer.GetWritableSpan(seal.TagLength);
            int payloadLength = writer.BytesWritten - pnOffset;

            // fill in the payload length retrospectively
            if (packetType != PacketType.OneRtt)
            {
                QuicPrimitives.WriteVarInt(payloadLengthSpan, payloadLength, 2);
            }

            seal.EncryptPacket(writer.Buffer.Span, pnOffset, payloadLength, truncatedPn);

            // remember what we sent in this packet
            context.SentPacket.PacketNumber = pnSpace.NextPacketNumber;
            context.SentPacket.BytesSent = writer.BytesWritten;
            context.SentPacket.TimeSent = context.Timestamp;

            if (isProbePacket)
            {
                recoverySpace.RemainingLossProbes--;
            }

            Recovery.OnPacketSent(GetPacketSpace(packetType), context.SentPacket, _tls.IsHandshakeComplete);
            pnSpace.NextPacketNumber++;
            NetEventSource.PacketSent(this, context.SentPacket.BytesSent);

            if (context.SentPacket.AckEliciting && !_ackElicitingSentSinceLastReceive)
            {
                RestartIdleTimer(context.Timestamp);
                _ackElicitingSentSinceLastReceive = true;
            }

            return true;
        }

        private void WritePacketHeader(QuicWriter writer, PacketType packetType, int pnLength)
        {
            if (packetType == PacketType.OneRtt)
            {
                // 1-RTT packets are the only ones using short header
                // TODO-RZ: implement spin
                // TODO-RZ: implement key update
                ShortPacketHeader.Write(writer,
                    new ShortPacketHeader(false, false, pnLength, DestinationConnectionId!));
            }
            else
            {
                LongPacketHeader.Write(writer, new LongPacketHeader(
                    packetType,
                    pnLength,
                    version,
                    DestinationConnectionId!.Data,
                    SourceConnectionId!.Data));

                // HACK: reserve 2 bytes for payload length and overwrite it later
                SharedPacketData.Write(writer, new SharedPacketData(
                    writer.Buffer.Span[0],
                    ReadOnlySpan<byte>.Empty,
                    1000 /*arbitrary number with 2-byte encoding*/));
            }
        }
    }
}
