// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Quic;

internal partial struct QUIC_SETTINGS : System.IEquatable<QUIC_SETTINGS>
{
    // Because QUIC_SETTINGS may contain gaps due to layout/alignment of individual
    // fields, we implement IEquatable<QUIC_SETTINGS> manually. If a new field is added,
    // then there is a unit test which should fail.

    public readonly bool Equals(QUIC_SETTINGS other)
    {
        return Anonymous1.IsSetFlags == other.Anonymous1.IsSetFlags
            && MaxBytesPerKey == other.MaxBytesPerKey
            && HandshakeIdleTimeoutMs == other.HandshakeIdleTimeoutMs
            && IdleTimeoutMs == other.IdleTimeoutMs
            && MtuDiscoverySearchCompleteTimeoutUs == other.MtuDiscoverySearchCompleteTimeoutUs
            && TlsClientMaxSendBuffer == other.TlsClientMaxSendBuffer
            && TlsServerMaxSendBuffer == other.TlsServerMaxSendBuffer
            && StreamRecvWindowDefault == other.StreamRecvWindowDefault
            && StreamRecvBufferDefault == other.StreamRecvBufferDefault
            && ConnFlowControlWindow == other.ConnFlowControlWindow
            && MaxWorkerQueueDelayUs == other.MaxWorkerQueueDelayUs
            && MaxStatelessOperations == other.MaxStatelessOperations
            && InitialWindowPackets == other.InitialWindowPackets
            && SendIdleTimeoutMs == other.SendIdleTimeoutMs
            && InitialRttMs == other.InitialRttMs
            && MaxAckDelayMs == other.MaxAckDelayMs
            && DisconnectTimeoutMs == other.DisconnectTimeoutMs
            && KeepAliveIntervalMs == other.KeepAliveIntervalMs
            && CongestionControlAlgorithm == other.CongestionControlAlgorithm
            && PeerBidiStreamCount == other.PeerBidiStreamCount
            && PeerUnidiStreamCount == other.PeerUnidiStreamCount
            && MaxBindingStatelessOperations == other.MaxBindingStatelessOperations
            && StatelessOperationExpirationMs == other.StatelessOperationExpirationMs
            && MinimumMtu == other.MinimumMtu
            && MaximumMtu == other.MaximumMtu
            && _bitfield == other._bitfield
            && MaxOperationsPerDrain == other.MaxOperationsPerDrain
            && MtuDiscoveryMissingProbeCount == other.MtuDiscoveryMissingProbeCount
            && DestCidUpdateIdleTimeoutMs == other.DestCidUpdateIdleTimeoutMs
            && Anonymous2.Flags == other.Anonymous2.Flags
            && StreamRecvWindowBidiLocalDefault == other.StreamRecvWindowBidiLocalDefault
            && StreamRecvWindowBidiRemoteDefault == other.StreamRecvWindowBidiRemoteDefault
            && StreamRecvWindowUnidiDefault == other.StreamRecvWindowUnidiDefault;
    }

    public override readonly int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(Anonymous1.IsSetFlags);
        hash.Add(MaxBytesPerKey);
        hash.Add(HandshakeIdleTimeoutMs);
        hash.Add(IdleTimeoutMs);
        hash.Add(MtuDiscoverySearchCompleteTimeoutUs);
        hash.Add(TlsClientMaxSendBuffer);
        hash.Add(TlsServerMaxSendBuffer);
        hash.Add(StreamRecvWindowDefault);
        hash.Add(StreamRecvBufferDefault);
        hash.Add(ConnFlowControlWindow);
        hash.Add(MaxWorkerQueueDelayUs);
        hash.Add(MaxStatelessOperations);
        hash.Add(InitialWindowPackets);
        hash.Add(SendIdleTimeoutMs);
        hash.Add(InitialRttMs);
        hash.Add(MaxAckDelayMs);
        hash.Add(DisconnectTimeoutMs);
        hash.Add(KeepAliveIntervalMs);
        hash.Add(CongestionControlAlgorithm);
        hash.Add(PeerBidiStreamCount);
        hash.Add(PeerUnidiStreamCount);
        hash.Add(MaxBindingStatelessOperations);
        hash.Add(StatelessOperationExpirationMs);
        hash.Add(MinimumMtu);
        hash.Add(MaximumMtu);
        hash.Add(_bitfield);
        hash.Add(MaxOperationsPerDrain);
        hash.Add(MtuDiscoveryMissingProbeCount);
        hash.Add(DestCidUpdateIdleTimeoutMs);
        hash.Add(Anonymous2.Flags);
        hash.Add(StreamRecvWindowBidiLocalDefault);
        hash.Add(StreamRecvWindowBidiRemoteDefault);
        hash.Add(StreamRecvWindowUnidiDefault);
        return hash.ToHashCode();
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is QUIC_SETTINGS other && Equals(other);
    }
}
