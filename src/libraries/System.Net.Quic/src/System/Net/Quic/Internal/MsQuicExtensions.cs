// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;

namespace Microsoft.Quic;

internal unsafe partial struct QUIC_NEW_CONNECTION_INFO
{
    public override string ToString()
        => $"{{ {nameof(QuicVersion)} = {QuicVersion}, {nameof(LocalAddress)} = {MsQuicHelpers.QuicAddrToIPEndPoint(LocalAddress)}, {nameof(RemoteAddress)} = {MsQuicHelpers.QuicAddrToIPEndPoint(RemoteAddress)} }}";
}

internal unsafe partial struct QUIC_LISTENER_EVENT
{
    public override string ToString()
        => Type switch
        {
            QUIC_LISTENER_EVENT_TYPE.NEW_CONNECTION
                => $"{{ {nameof(NEW_CONNECTION.Info)} = {{ {nameof(QUIC_NEW_CONNECTION_INFO.QuicVersion)} = {NEW_CONNECTION.Info->QuicVersion}, {nameof(QUIC_NEW_CONNECTION_INFO.LocalAddress)} = {MsQuicHelpers.QuicAddrToIPEndPoint(NEW_CONNECTION.Info->LocalAddress)}, {nameof(QUIC_NEW_CONNECTION_INFO.RemoteAddress)} = {MsQuicHelpers.QuicAddrToIPEndPoint(NEW_CONNECTION.Info->RemoteAddress)} }}, {nameof(NEW_CONNECTION.Connection)} = 0x{(IntPtr)NEW_CONNECTION.Connection:X11} }}",
            _ => string.Empty
        };
}

internal unsafe partial struct QUIC_CONNECTION_EVENT
{
    public override string ToString()
        => Type switch
        {
            QUIC_CONNECTION_EVENT_TYPE.CONNECTED
                => $"{{ {nameof(CONNECTED.SessionResumed)} = {CONNECTED.SessionResumed} }}",
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT
                => $"{{ {nameof(SHUTDOWN_INITIATED_BY_TRANSPORT.Status)} = {SHUTDOWN_INITIATED_BY_TRANSPORT.Status}, {nameof(SHUTDOWN_INITIATED_BY_TRANSPORT.ErrorCode)} = {SHUTDOWN_INITIATED_BY_TRANSPORT.ErrorCode} }}",
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER
                => $"{{ {nameof(SHUTDOWN_INITIATED_BY_PEER.ErrorCode)} = {SHUTDOWN_INITIATED_BY_PEER.ErrorCode} }}",
            QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE
                => $"{{ {nameof(SHUTDOWN_COMPLETE.HandshakeCompleted)} = {SHUTDOWN_COMPLETE.HandshakeCompleted}, {nameof(SHUTDOWN_COMPLETE.PeerAcknowledgedShutdown)} = {SHUTDOWN_COMPLETE.PeerAcknowledgedShutdown}, {nameof(SHUTDOWN_COMPLETE.AppCloseInProgress)} = {SHUTDOWN_COMPLETE.AppCloseInProgress} }}",
            QUIC_CONNECTION_EVENT_TYPE.LOCAL_ADDRESS_CHANGED
                => $"{{ {nameof(LOCAL_ADDRESS_CHANGED.Address)} = {MsQuicHelpers.QuicAddrToIPEndPoint(LOCAL_ADDRESS_CHANGED.Address)} }}",
            QUIC_CONNECTION_EVENT_TYPE.PEER_ADDRESS_CHANGED
                => $"{{ {nameof(PEER_ADDRESS_CHANGED.Address)} = {MsQuicHelpers.QuicAddrToIPEndPoint(PEER_ADDRESS_CHANGED.Address)} }}",
            QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED
                => $"{{ {nameof(PEER_STREAM_STARTED.Stream)} = 0x{(IntPtr)PEER_STREAM_STARTED.Stream:X11} {nameof(PEER_STREAM_STARTED.Flags)} = {PEER_STREAM_STARTED.Flags} }}",
            QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE
                => $"{{ {nameof(STREAMS_AVAILABLE.BidirectionalCount)} = {STREAMS_AVAILABLE.BidirectionalCount}, {nameof(STREAMS_AVAILABLE.UnidirectionalCount)} = {STREAMS_AVAILABLE.UnidirectionalCount} }}",
            QUIC_CONNECTION_EVENT_TYPE.PEER_NEEDS_STREAMS
                => $"{{ {nameof(PEER_NEEDS_STREAMS.Bidirectional)} = {PEER_NEEDS_STREAMS.Bidirectional} }}",
            QUIC_CONNECTION_EVENT_TYPE.IDEAL_PROCESSOR_CHANGED
                => $"{{ {nameof(IDEAL_PROCESSOR_CHANGED.IdealProcessor)} = {IDEAL_PROCESSOR_CHANGED.IdealProcessor}, {nameof(IDEAL_PROCESSOR_CHANGED.PartitionIndex)} = {IDEAL_PROCESSOR_CHANGED.PartitionIndex} }}",
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_STATE_CHANGED
                => $"{{ {nameof(DATAGRAM_STATE_CHANGED.SendEnabled)} = {DATAGRAM_STATE_CHANGED.SendEnabled}, {nameof(DATAGRAM_STATE_CHANGED.MaxSendLength)} = {DATAGRAM_STATE_CHANGED.MaxSendLength} }}",
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_RECEIVED
                => $"{{ {nameof(DATAGRAM_RECEIVED.Flags)} = {DATAGRAM_RECEIVED.Flags} }}",
            QUIC_CONNECTION_EVENT_TYPE.DATAGRAM_SEND_STATE_CHANGED
                => $"{{ {nameof(DATAGRAM_SEND_STATE_CHANGED.ClientContext)} = 0x{(IntPtr)DATAGRAM_SEND_STATE_CHANGED.ClientContext:X11}, {nameof(DATAGRAM_SEND_STATE_CHANGED.State)} = {DATAGRAM_SEND_STATE_CHANGED.State} }}",
            QUIC_CONNECTION_EVENT_TYPE.RESUMED
                => $"{{ {nameof(RESUMED.ResumptionStateLength)} = {RESUMED.ResumptionStateLength} }}",
            QUIC_CONNECTION_EVENT_TYPE.RESUMPTION_TICKET_RECEIVED
                => $"{{ {nameof(RESUMPTION_TICKET_RECEIVED.ResumptionTicketLength)} = {RESUMPTION_TICKET_RECEIVED.ResumptionTicketLength} }}",
            QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED
                => $"{{ {nameof(PEER_CERTIFICATE_RECEIVED.DeferredStatus)} = {PEER_CERTIFICATE_RECEIVED.DeferredStatus}, {nameof(PEER_CERTIFICATE_RECEIVED.DeferredErrorFlags)} = {PEER_CERTIFICATE_RECEIVED.DeferredErrorFlags}, {nameof(PEER_CERTIFICATE_RECEIVED.Certificate)} = 0x{(IntPtr)PEER_CERTIFICATE_RECEIVED.Certificate:X11} }}",
            QUIC_CONNECTION_EVENT_TYPE.RELIABLE_RESET_NEGOTIATED
                => $"{{ {nameof(RELIABLE_RESET_NEGOTIATED.IsNegotiated)} = {RELIABLE_RESET_NEGOTIATED.IsNegotiated} }}",
            QUIC_CONNECTION_EVENT_TYPE.ONE_WAY_DELAY_NEGOTIATED
                => $"{{ {nameof(ONE_WAY_DELAY_NEGOTIATED.SendNegotiated)} = {ONE_WAY_DELAY_NEGOTIATED.SendNegotiated}, {nameof(ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated)} = {ONE_WAY_DELAY_NEGOTIATED.ReceiveNegotiated} }}",
            _ => string.Empty
        };
}

internal unsafe partial struct QUIC_STREAM_EVENT
{
    public override string ToString()
        => Type switch
        {
            QUIC_STREAM_EVENT_TYPE.START_COMPLETE
                => $"{{ {nameof(START_COMPLETE.Status)} = {START_COMPLETE.Status}, {nameof(START_COMPLETE.ID)} = {START_COMPLETE.ID}, {nameof(START_COMPLETE.PeerAccepted)} = {START_COMPLETE.PeerAccepted} }}",
            QUIC_STREAM_EVENT_TYPE.RECEIVE
                => $"{{ {nameof(RECEIVE.AbsoluteOffset)} = {RECEIVE.AbsoluteOffset}, {nameof(RECEIVE.TotalBufferLength)} = {RECEIVE.TotalBufferLength}, {nameof(RECEIVE.Flags)} = {RECEIVE.Flags} }}",
            QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE
                => $"{{ {nameof(SEND_COMPLETE.Canceled)} = {SEND_COMPLETE.Canceled} }}",
            QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED
                => $"{{ {nameof(PEER_SEND_ABORTED.ErrorCode)} = {PEER_SEND_ABORTED.ErrorCode} }}",
            QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED
                => $"{{ {nameof(PEER_RECEIVE_ABORTED.ErrorCode)} = {PEER_RECEIVE_ABORTED.ErrorCode} }}",
            QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE
                => $"{{ {nameof(SEND_SHUTDOWN_COMPLETE.Graceful)} = {SEND_SHUTDOWN_COMPLETE.Graceful} }}",
            QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE
                => $"{{ {nameof(SHUTDOWN_COMPLETE.ConnectionShutdown)} = {SHUTDOWN_COMPLETE.ConnectionShutdown}, {nameof(SHUTDOWN_COMPLETE.ConnectionShutdownByApp)} = {SHUTDOWN_COMPLETE.ConnectionShutdownByApp}, {nameof(SHUTDOWN_COMPLETE.ConnectionClosedRemotely)} = {SHUTDOWN_COMPLETE.ConnectionClosedRemotely}, {nameof(SHUTDOWN_COMPLETE.ConnectionErrorCode)} = {SHUTDOWN_COMPLETE.ConnectionErrorCode}, {nameof(SHUTDOWN_COMPLETE.ConnectionCloseStatus)} = {SHUTDOWN_COMPLETE.ConnectionCloseStatus} }}",
            QUIC_STREAM_EVENT_TYPE.IDEAL_SEND_BUFFER_SIZE
                => $"{{ {nameof(IDEAL_SEND_BUFFER_SIZE.ByteCount)} = {IDEAL_SEND_BUFFER_SIZE.ByteCount} }}",
            _ => string.Empty
        };
}
