// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;

namespace System.Net.Quic.Implementations.Managed.Internal.Tracing
{
    internal interface IQuicTrace
    {
        void OnTransportParametersSet(TransportParameters parameters);

        void OnKeyUpdated(ReadOnlySpan<byte> secret, EncryptionLevel level, bool isServer,
            KeyUpdateTrigger trigger, int? generation);

        void OnDatagramReceived(int length);
        void OnDatagramSent(int length);
        void OnDatagramDropped(int length);
        void OnStreamStateUpdated(int length);

        void OnPacketReceiveStart(ReadOnlySpan<byte> scid, ReadOnlySpan<byte> dcid, PacketType packetType,
            long packetNumber, long payloadLength, long packetSize);

        void OnPacketReceiveEnd();
        void OnPacketSendStart();

        void OnPacketSendEnd(ReadOnlySpan<byte> scid, ReadOnlySpan<byte> dcid,
            PacketType packetType, long packetNumber, long payloadLength, long packetSize);

        void OnPacketDropped(PacketType? type, int packetSize);
        void OnPaddingFrame(int length);
        void OnPingFrame();
        void OnAckFrame(in AckFrame frame, long ackDelayMicroseconds);
        void OnResetStreamFrame(in ResetStreamFrame frame);
        void OnStopSendingFrame(in StopSendingFrame frame);
        void OnCryptoFrame(in CryptoFrame frame);
        void OnNewTokenFrame(in NewTokenFrame frame);
        void OnStreamFrame(in StreamFrame frame);
        void OnMaxDataFrame(in MaxDataFrame frame);
        void OnMaxStreamDataFrame(in MaxStreamDataFrame frame);
        void OnMaxStreamsFrame(in MaxStreamsFrame frame);
        void OnDataBlockedFrame(in DataBlockedFrame frame);
        void OnStreamDataBlockedFrame(in StreamDataBlockedFrame frame);
        void OnStreamsBlockedFrame(in StreamsBlockedFrame frame);
        void OnNewConnectionIdFrame(in NewConnectionIdFrame frame);
        void OnRetireConnectionIdFrame(in RetireConnectionIdFrame frame);
        void OnPathChallengeFrame(in PathChallengeFrame frame);
        void OnConnectionCloseFrame(in ConnectionCloseFrame frame);
        void OnHandshakeDoneFrame();
        void OnUnknownFrame(long frameType, int length);
        void OnPacketLost(PacketType packetType, long packetNumber, PacketLossTrigger trigger);
        void OnRecoveryMetricsUpdated(RecoveryController recovery);
        void OnCongestionStateUpdated(CongestionState state);
        void OnLossTimerUpdated();
        void OnRecoveryParametersSet(RecoveryController recovery);
        void Dispose();
    }
}
