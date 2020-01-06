﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    /// <summary>
    /// Flags to pass when creating a certificate hash store.
    /// </summary>
    [Flags]
    internal enum QUIC_CERT_HASH_STORE_FLAG : uint
    {
        NONE = 0,
        MACHINE_CERT = 0x0001,
    }

    /// <summary>
    /// Flags to pass when creating a security config.
    /// </summary>
    [Flags]
    internal enum QUIC_SEC_CONFIG_FLAG : uint
    {
        NONE = 0,
        CERT_HASH = 0x00000001,
        CERT_HASH_STORE = 0x00000002,
        CERT_CONTEXT = 0x00000004,
        CERT_FILE = 0x00000008,
        ENABL_OCSP = 0x00000010,
        CERT_NULL = 0xF0000000,
    }

    internal enum QUIC_LISTENER_EVENT : byte
    {
        NEW_CONNECTION = 0
    }

    internal enum QUIC_CONNECTION_EVENT : byte
    {
        CONNECTED = 0,
        SHUTDOWN_BEGIN = 1,
        SHUTDOWN_BEGIN_PEER = 2,
        SHUTDOWN_COMPLETE = 3,
        LOCAL_ADDR_CHANGED = 4,
        PEER_ADDR_CHANGED = 5,
        NEW_STREAM = 6,
        STREAMS_AVAILABLE = 7,
        PEER_NEEDS_STREAMS = 8,
        IDEAL_SEND_BUFFER = 9,
    }

    [Flags]
    internal enum QUIC_CONNECTION_SHUTDOWN_FLAG : uint
    {
        NONE = 0x0,
        SILENT = 0x1
    }

    internal enum QUIC_PARAM_LEVEL : uint
    {
        REGISTRATION = 0,
        SESSION = 1,
        LISTENER = 2,
        CONNECTION = 3,
        TLS = 4,
        STREAM = 5,
    }

    internal enum QUIC_PARAM_REGISTRATION : uint
    {
        RETRY_MEMORY_PERCENT = 0,
        CID_PREFIX = 1
    }

    internal enum QUIC_PARAM_SESSION : uint
    {
        TLS_TICKET_KEY = 0,
        PEER_BIDI_STREAM_COUNT = 1,
        PEER_UNIDI_STREAM_COUNT = 2,
        IDLE_TIMEOUT = 3,
        DISCONNECT_TIMEOUT = 4,
        MAX_BYTES_PER_KEY = 5
    }

    internal enum QUIC_PARAM_LISTENER : uint
    {
        LOCAL_ADDRESS = 0
    }

    internal enum QUIC_PARAM_CONN : uint
    {
        QUIC_VERSION = 0,
        LOCAL_ADDRESS = 1,
        REMOTE_ADDRESS = 2,
        IDLE_TIMEOUT = 3,
        PEER_BIDI_STREAM_COUNT = 4,
        PEER_UNIDI_STREAM_COUNT = 5,
        LOCAL_BIDI_STREAM_COUNT = 6,
        LOCAL_UNIDI_STREAM_COUNT = 7,
        CLOSE_REASON_PHRASE = 8,
        STATISTICS = 9,
        STATISTICS_PLAT = 10,
        CERT_VALIDATION_FLAGS = 11,
        KEEP_ALIVE_ENABLED = 12,
        DISCONNECT_TIMEOUT = 13,
        SEC_CONFIG = 14,
        USE_SEND_BUFFER = 15,
        USE_PACING = 16,
        SHARE_UDP_BINDING = 17,
        IDEAL_PROCESSOR = 18,
        MAX_STREAM_IDS = 19
    }

    internal enum QUIC_PARAM_STREAM : uint
    {
        ID = 0,
        RECEIVE_ENABLED = 1,
        ZERORTT_LENGTH = 2,
        IDEAL_SEND_BUFFER = 3
    }

    internal enum QUIC_STREAM_EVENT : byte
    {
        START_COMPLETE = 0,
        RECV = 1,
        SEND_COMPLETE = 2,
        PEER_SEND_CLOSE = 3,
        PEER_SEND_ABORT = 4,
        PEER_RECV_ABORT = 5,
        SEND_SHUTDOWN_COMPLETE = 6,
        SHUTDOWN_COMPLETE = 7,
        IDEAL_SEND_BUFFER_SIZE = 8,
    }

    [Flags]
    internal enum QUIC_STREAM_OPEN_FLAG : uint
    {
        NONE = 0,
        UNIDIRECTIONAL = 0x1,
        ZERO_RTT = 0x2,
    }

    [Flags]
    internal enum QUIC_STREAM_START_FLAG : uint
    {
        NONE = 0,
        FAIL_BLOCKED = 0x1,
        IMMEDIATE = 0x2,
        ASYNC = 0x4,
    }

    [Flags]
    internal enum QUIC_STREAM_SHUTDOWN_FLAG : uint
    {
        NONE = 0,
        GRACEFUL = 0x1,
        ABORT_SEND = 0x2,
        ABORT_RECV = 0x4,
        ABORT = 0x6,
        IMMEDIATE = 0x8
    }

    [Flags]
    internal enum QUIC_SEND_FLAG : uint
    {
        NONE = 0,
        ALLOW_0_RTT = 0x00000001,
        FIN = 0x00000002,
    }

    [Flags]
    internal enum QUIC_RECV_FLAG : byte
    {
        NONE = 0,
        ZERO_RTT = 0x1,
        FIN = 0x02
    }
}
