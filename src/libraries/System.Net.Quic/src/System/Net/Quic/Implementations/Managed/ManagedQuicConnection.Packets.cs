using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Crypto;
using System.Net.Quic.Implementations.Managed.Internal.Headers;

namespace System.Net.Quic.Implementations.Managed
{
    internal sealed partial class ManagedQuicConnection
    {
        /// <summary>
        ///     Current value of the key phase bit for key update detection.
        /// </summary>
        private bool _currentKeyPhase;

        private CryptoSeal? _nextSendSeal;

        private CryptoSeal? _nextRecvSeal;

        private bool _doKeyUpdate;

        internal void ReceiveData(QuicReader reader, EndPoint sender, QuicSocketContext.RecvContext ctx)
        {
            if (_isDraining)
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
                        if (NetEventSource.IsEnabled) NetEventSource.PacketDropped(this, reader.Buffer.Length);
                        break;
                    case ProcessPacketResult.Ok:
                        // An endpoint restarts its idle timer when a packet from its peer is
                        // received and processed successfully.
                        _ackElicitingWasSentSinceLastReceive = false;
                        RestartIdleTimer(ctx.Timestamp);
                        break;
                }

                if (status == ProcessPacketResult.Error)
                {
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

            if (HeaderHelpers.IsLongHeader(first))
            {
                // first, just parse the header without validation, we will validate after lifting header protection
                if (!LongPacketHeader.Read(reader, out var header))
                {
                    return ProcessPacketResult.DropPacket;
                }

                if (HeaderHelpers.HasPacketTypeEncryption(header.PacketType))
                {
                    var pnSpace = GetPacketNumberSpace(GetEncryptionLevel(header.PacketType));

                    if (!UnprotectLongHeaderPacket(reader, ref header, out var headerData, pnSpace))
                    {
                        return ProcessPacketResult.DropPacket;
                    }

                    switch (header.PacketType)
                    {
                        case PacketType.Initial:
                            if (IsServer)
                            {
                                // check UDP datagram size, by now the reader's buffer end is aligned with the UDP datagram end.
                                // TODO-RZ: in rare cases when initial is not the first of the coalesced packets this can falsely close the connection.
                                // as the QUIC does only recommend, not mandate order of the coalesced packets
                                if (reader.Buffer.Length < QuicConstants.MinimumClientInitialDatagramSize)
                                {
                                    return CloseConnection(TransportErrorCode.ProtocolViolation,
                                        QuicError.InitialPacketTooShort);
                                }
                            }

                            // Servers may not send Token in Initial packets
                            if (!IsServer && !headerData.Token.IsEmpty)
                            {
                                return CloseConnection(
                                    TransportErrorCode.ProtocolViolation,
                                    QuicError.UnexpectedToken);
                            }

                            // after client receives the first packet (which is either initial or retry), it must
                            // use the connection id supplied by the server, but should ignore any further changes to CID,
                            // see [TRANSPORT] Section 7.2
                            if (!IsServer &&
                                GetPacketNumberSpace(EncryptionLevel.Initial).LargestReceivedPacketNumber < 0)
                            {
                                // protection keys are not affected by this change
                                DestinationConnectionId = new ConnectionId(
                                    header.SourceConnectionId.ToArray(),
                                    DestinationConnectionId!.SequenceNumber,
                                    DestinationConnectionId.StatelessResetToken);
                            }

                            // continue processing
                            goto case PacketType.Handshake;
                        case PacketType.Handshake:
                        case PacketType.ZeroRtt:
                            // Note: reserved bits can be validated only if decryption succeeds
                            if (headerData.Length > reader.BytesLeft)
                            {
                                return ProcessPacketResult.DropPacket;
                            }

                            // total length of the packet is known and checked during header parsing.
                            // Adjust the buffer to the range belonging to the current packet.
                            reader.Reset(reader.Buffer.Slice(0, reader.BytesRead + (int)headerData.Length),
                                reader.BytesRead);
                            ProcessPacketResult result = ReceiveCommon(reader, header, headerData, pnSpace, context);

                            if (result == ProcessPacketResult.Ok && IsServer && header.PacketType == PacketType.Handshake)
                            {
                                // RFC: A server stops sending and processing Initial packets when it receives its first
                                // Handshake packet
                                DropPacketNumberSpace(PacketSpace.Initial, context.SentPacketPool);
                            }

                            return result;

                        // other types handled elsewhere
                        default:
                            throw new InvalidOperationException("Unreachable");
                    }
                }

                // clients SHOULD ignore fixed bit when receiving version negotiation
                if (!header.FixedBit && IsServer && header.PacketType == PacketType.VersionNegotiation ||
                    // TODO-RZ: following checks should be moved into SocketContext
                    SourceConnectionId != null &&
                    !header.DestinationConnectionId.SequenceEqual(SourceConnectionId!.Data) ||
                    header.Version != QuicVersion.Draft27)
                {
                    return ProcessPacketResult.DropPacket;
                }

                switch (header.PacketType)
                {
                    case PacketType.Retry:
                        return ReceiveRetry(reader, header, context);
                    case PacketType.VersionNegotiation:
                        return ReceiveVersionNegotiation(reader, header, context);
                    // other types handled elsewhere
                    default:
                        throw new InvalidOperationException("Unreachable");
                }
            }
            else // short header
            {
                var pnSpace = GetPacketNumberSpace(EncryptionLevel.Application);
                if (pnSpace.RecvCryptoSeal == null)
                {
                    // Decryption keys are not available yet
                    return ProcessPacketResult.DropPacket;
                }

                // read first without validation
                if (!ShortPacketHeader.Read(reader, _localConnectionIdCollection, out var header))
                {
                    return ProcessPacketResult.DropPacket;
                }

                // remove header protection
                int pnOffset = reader.BytesRead;
                pnSpace.RecvCryptoSeal.UnprotectHeader(reader.Buffer.Span, pnOffset);
                // refresh the first byte
                header = new ShortPacketHeader(reader.Buffer.Span[0], header.DestinationConnectionId);

                // Note: reserved bits can be validated only if decryption succeeds
                if (!header.FixedBit)
                {
                    return ProcessPacketResult.DropPacket;
                }

                CryptoSeal recvSeal = pnSpace.RecvCryptoSeal;

                // the peer MUST not initiate key update before handshake is done,
                // so the seals should already exist, but we should still check against an attack.
                if (header.KeyPhaseBit != _currentKeyPhase && HandshakeConfirmed)
                {
                    // An endpoint SHOULD retain old keys so that packets sent by its peer
                    // prior to receiving the key update can be processed.  Discarding old
                    // keys too early can cause delayed packets to be discarded.  Discarding
                    // packets will be interpreted as packet loss by the peer and could
                    // adversely affect performance.

                    // keys will be updated next time a packet is sent
                    _doKeyUpdate = true;
                    if (_nextRecvSeal == null)
                    {
                        // create updated keys

                        _nextSendSeal = CryptoSeal.UpdateSeal(pnSpace.SendCryptoSeal!);
                        _nextRecvSeal = CryptoSeal.UpdateSeal(pnSpace.RecvCryptoSeal!);
                    }

                    recvSeal = _nextRecvSeal;
                }

                PacketType packetType = PacketType.OneRtt;
                int payloadLength = reader.BytesLeft;

                if (!recvSeal.UnprotectPacket(reader.Buffer.Span, pnOffset, payloadLength,
                    pnSpace.LargestReceivedPacketNumber))
                {
                    // decryption failed, drop the packet.
                    reader.Advance(payloadLength);
                    return ProcessPacketResult.DropPacket;
                }

                // we check value of reserved bits now, because if the packet was decrypted successfully, then we can be
                // sure that it was sent by the expected peer
                if (header.ReservedBits != 0)
                {
                    return CloseConnection(TransportErrorCode.ProtocolViolation,
                        QuicError.InvalidReservedBits);
                }

                return ReceiveProtectedFrames(reader, pnSpace, pnOffset, header.PacketNumberLength, packetType, recvSeal,
                    context);
            }
        }

        private bool UnprotectLongHeaderPacket(QuicReader reader, ref LongPacketHeader header, out SharedPacketData headerData, PacketNumberSpace pnSpace)
        {
            // initialize protection keys if necessary (first initial packet)
            if (IsServer && header.PacketType == PacketType.Initial &&
                pnSpace.RecvCryptoSeal == null && pnSpace.LargestReceivedPacketNumber < 0)
            {
                // clients destination connection Id is ours source connection Id
                SourceConnectionId = new ConnectionId(header.DestinationConnectionId.ToArray(), 0,
                    StatelessResetToken.Random());
                DestinationConnectionId = new ConnectionId(header.SourceConnectionId.ToArray(), 0,
                    StatelessResetToken.Random());

                _localConnectionIdCollection.Add(SourceConnectionId);
                DeriveInitialProtectionKeys(SourceConnectionId.Data);
            }

            if (pnSpace.RecvCryptoSeal == null || // Decryption keys are not available yet
                // skip additional data so the reader position is on packet number offset
                !SharedPacketData.Read(reader, header.FirstByte, out headerData))
            {
                headerData = default;
                return false;
            }

            // remove header protection
            int pnOffset = reader.BytesRead;
            pnSpace.RecvCryptoSeal!.UnprotectHeader(reader.Buffer.Span, pnOffset);

            byte unprotectedFirst = reader.Buffer.Span[0];

            // update headers with the new unprotected first byte data
            header = new LongPacketHeader(unprotectedFirst,
                header.Version, header.DestinationConnectionId, header.SourceConnectionId);
            headerData = new SharedPacketData(unprotectedFirst, headerData.Token, headerData.Length);
            return true;
        }

        private ProcessPacketResult ReceiveRetry(QuicReader reader, in LongPacketHeader header,
            QuicSocketContext.RecvContext context)
        {
            // TODO-RZ: Retry not supported
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveVersionNegotiation(QuicReader reader, in LongPacketHeader header,
            QuicSocketContext.RecvContext context)
        {
            // TODO-RZ: Version negotiation not supported
            throw new NotImplementedException();
        }

        private ProcessPacketResult ReceiveCommon(QuicReader reader, in LongPacketHeader header,
            in SharedPacketData headerData, PacketNumberSpace pnSpace, QuicSocketContext.RecvContext context)
        {
            int pnOffset = reader.BytesRead;
            int payloadLength = (int)headerData.Length;
            PacketType packetType = header.PacketType;
            var seal = pnSpace.RecvCryptoSeal!;

            if (!seal.UnprotectPacket(reader.Buffer.Span, pnOffset, payloadLength,
                pnSpace.LargestReceivedPacketNumber))
            {
                // decryption failed, drop the packet.
                reader.Advance(payloadLength);
                return ProcessPacketResult.DropPacket;
            }

            // we check value of reserved bits now, because if the packet was decrypted successfully, then we can be
            // sure that it was sent by the expected peer
            if (header.ReservedBits != 0)
            {
                return CloseConnection(TransportErrorCode.ProtocolViolation,
                    QuicError.InvalidReservedBits);
            }

            return ReceiveProtectedFrames(reader, pnSpace, pnOffset, header.PacketNumberLength, packetType, seal,
                context);
        }

        private ProcessPacketResult ReceiveProtectedFrames(QuicReader reader, PacketNumberSpace pnSpace, int pnOffset,
            int pnLength,
            PacketType packetType, CryptoSeal seal, QuicSocketContext.RecvContext context)
        {
            reader.TryReadPacketNumber(pnLength, pnSpace.LargestReceivedPacketNumber, out long packetNumber);

            if (pnSpace.ReceivedPacketNumbers.Contains(packetNumber))
            {
                // already processed or outside congestion window
                // Note: there may be false positives if the packet number is 64 lesser than largest received, but that
                // should not occur often due to flow control / congestion window. Besides the data can be retransmitted
                // in following packet.
                return ProcessPacketResult.Ok;
            }

            if (pnSpace.LargestReceivedPacketNumber < packetNumber)
            {
                pnSpace.LargestReceivedPacketNumber = packetNumber;
                pnSpace.LargestReceivedPacketTimestamp = context.Timestamp;
            }

            pnSpace.UnackedPacketNumbers.Add(packetNumber);
            pnSpace.ReceivedPacketNumbers.Add(packetNumber);

            // HACK: we do not want to try processing the AEAD integrity tag as if it were frames, so we just temporarily
            // replace the buffer with shortened version while processing the frames.
            var originalSegment = reader.Buffer;
            int originalBytesRead = reader.BytesRead;
            int tagLength = seal.TagLength;
            int length = reader.BytesLeft - tagLength;
            reader.Reset(originalSegment.Slice(originalBytesRead, length));

            var result = ProcessFrames(reader, packetType, context);

            reader.Reset(originalSegment);
            return result;
        }

        internal void SendData(QuicWriter writer, out EndPoint? receiver, QuicSocketContext.SendContext ctx)
        {
            receiver = _remoteEndpoint;

            if (_isDraining)
            {
                // While otherwise identical to the closing state, an endpoint in the draining state MUST NOT
                // send any packets
                return;
            }

            var level = GetWriteLevel(ctx.Timestamp);
            var origMemory = writer.Buffer;
            int written = 0;

            while (level != EncryptionLevel.None)
            {
                if (GetPacketNumberSpace(level).SendCryptoSeal == null)
                {
                    // Secrets have not been derived yet, can't send anything
                    break;
                }

                if (SendOne(writer, level, ctx))
                {
                    // prepare for sending next packet.
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

                var nextLevel = GetWriteLevel(ctx.Timestamp);

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

            int maxPacketLength = (int)(Tls.IsHandshakeComplete
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

            (int truncatedPn, int pnLength) = pnSpace.GetNextPacketNumber(recoverySpace.LargestTransportedPacketNumber);
            WritePacketHeader(writer, packetType, pnLength);

            // for non 1-RTT packets, we reserve 2 bytes which we will overwrite once total payload length is known
            var payloadLengthSpan = writer.Buffer.Span.Slice(writer.BytesWritten - 2, 2);

            int pnOffset = writer.BytesWritten;
            writer.WriteTruncatedPacketNumber(pnLength, truncatedPn);

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

            // after this point it is certain that the packet will be sent, commit pending key update
            if (_doKeyUpdate)
            {
                pnSpace.SendCryptoSeal = _nextSendSeal;
                pnSpace.RecvCryptoSeal = _nextRecvSeal;
                _nextRecvSeal = null;
                _nextSendSeal = null;
                _currentKeyPhase = !_currentKeyPhase;
                _doKeyUpdate = false;
            }

            if (!IsServer && packetType == PacketType.Initial)
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

            seal.ProtectPacket(writer.Buffer.Span, pnOffset, payloadLength, truncatedPn);
            seal.ProtectHeader(writer.Buffer.Span, pnOffset);

            // remember what we sent in this packet
            context.SentPacket.PacketNumber = pnSpace.NextPacketNumber;
            context.SentPacket.BytesSent = writer.BytesWritten;
            context.SentPacket.TimeSent = context.Timestamp;

            if (isProbePacket)
            {
                recoverySpace.RemainingLossProbes--;
            }

            Recovery.OnPacketSent(GetPacketSpace(packetType), context.SentPacket, Tls.IsHandshakeComplete);
            pnSpace.NextPacketNumber++;
            NetEventSource.PacketSent(this, context.SentPacket.BytesSent);

            if (context.SentPacket.AckEliciting && !_ackElicitingWasSentSinceLastReceive)
            {
                RestartIdleTimer(context.Timestamp);
                _ackElicitingWasSentSinceLastReceive = true;
            }

            if (!IsServer && packetType == PacketType.Handshake)
            {
                // RFC: A client stops sending and processing Initial packets when it sends its first Handshake packet
                DropPacketNumberSpace(PacketSpace.Initial, context.SentPacketPool);
            }

            return true;
        }

        private void WritePacketHeader(QuicWriter writer, PacketType packetType, int pnLength)
        {
            if (packetType == PacketType.OneRtt)
            {
                // 1-RTT packets are the only ones using short header
                // TODO-RZ: implement spin
                const bool spin = false;
                // TODO-RZ: implement key update fully
                bool keyPhase = _doKeyUpdate
                    ? !_currentKeyPhase
                    : _currentKeyPhase;
                ShortPacketHeader.Write(writer,
                    new ShortPacketHeader(spin, keyPhase, 0 /*reserved bits*/, pnLength, DestinationConnectionId!));
            }
            else
            {
                LongPacketHeader.Write(writer, new LongPacketHeader(
                    packetType,
                    pnLength,
                    0 /*reserved bits*/,
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
