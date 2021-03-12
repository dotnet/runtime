// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal enum QUIC_EXECUTION_PROFILE : uint
    {
        QUIC_EXECUTION_PROFILE_LOW_LATENCY,         // Default
        QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT,
        QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER,
        QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME
    }

    internal enum QUIC_CREDENTIAL_TYPE : uint
    {
        NONE,
        HASH,
        HASH_STORE,
        CONTEXT,
        FILE,
        STUB_NULL = 0xF0000000 // pass as server cert to stubtls implementation
    }

    [Flags]
    internal enum QUIC_CREDENTIAL_FLAGS : uint
    {
        NONE = 0,
        CLIENT = 1, // lack of client flag indicates server.
        LOAD_ASYNCHRONOUS = 2,
        NO_CERTIFICATE_VALIDATION = 4,
        ENABLE_OCSP = 8
    }

    [Flags]
    internal enum QUIC_CONNECTION_SHUTDOWN_FLAG : uint
    {
        NONE = 0x0,
        SILENT = 0x1
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
        ABORT = ABORT_SEND | ABORT_RECV,
        IMMEDIATE = 0x8
    }

    [Flags]
    internal enum QUIC_RECEIVE_FLAG : uint
    {
        NONE = 0,
        ZERO_RTT = 0x1,
        FIN = 0x02
    }

    [Flags]
    internal enum QUIC_SEND_FLAG : uint
    {
        NONE = 0,
        ALLOW_0_RTT = 0x00000001,
        START = 0x00000002,
        FIN = 0x00000004,
        DGRAM_PRIORITY = 0x00000008
    }

    internal enum QUIC_PARAM_LEVEL : uint
    {
        GLOBAL,
        REGISTRATION,
        CONFIGURATION,
        LISTENER,
        CONNECTION,
        TLS,
        STREAM
    }

    internal enum QUIC_PARAM_GLOBAL : uint
    {
        RETRY_MEMORY_PERCENT,
        SUPPORTED_VERSIONS,
        LOAD_BALANCING_MODE,
        PERF_COUNTERS,
        SETTINGS
    }

    internal enum QUIC_PARAM_REGISTRATION : uint
    {
        CID_PREFIX = 0
    }

    internal enum QUIC_PARAM_SESSION : uint
    {
        TLS_TICKET_KEY = 0,
        PEER_BIDI_STREAM_COUNT = 1,
        PEER_UNIDI_STREAM_COUNT = 2,
        IDLE_TIMEOUT = 3,
        DISCONNECT_TIMEOUT = 4,
        MAX_BYTES_PER_KEY = 5,
        MIGRATION_ENABLED = 6,
        DATAGRAM_RECEIVE_ENABLED = 7,
        SERVER_RESUMPTION_LEVEL = 8
    }

    internal enum QUIC_PARAM_LISTENER : uint
    {
        LOCAL_ADDRESS = 0,
        STATS = 1
    }

    internal enum QUIC_PARAM_CONN : uint
    {
        QUIC_VERSION,
        LOCAL_ADDRESS,
        REMOTE_ADDRESS,
        IDEAL_PROCESSOR,
        SETTINGS,
        STATISTICS,
        STATISTICS_PLAT,
        SHARE_UDP_BINDING,
        LOCAL_BIDI_STREAM_COUNT,
        LOCAL_UNIDI_STREAM_COUNT,
        MAX_STREAM_IDS,
        CLOSE_REASON_PHRASE,
        STREAM_SCHEDULING_SCHEME,
        DATAGRAM_RECEIVE_ENABLED,
        DATAGRAM_SEND_ENABLED,
        DISABLE_1RTT_ENCRYPTION,
        RESUMPTION_TICKET
    }

    internal enum QUIC_PARAM_STREAM : uint
    {
        ID = 0,
        ZERORTT_LENGTH = 1,
        IDEAL_SEND_BUFFER = 2
    }

    internal enum QUIC_LISTENER_EVENT : uint
    {
        NEW_CONNECTION = 0
    }

    internal enum QUIC_CONNECTION_EVENT : uint
    {
        CONNECTED = 0,
        SHUTDOWN_INITIATED_BY_TRANSPORT = 1,
        SHUTDOWN_INITIATED_BY_PEER = 2,
        SHUTDOWN_COMPLETE = 3,
        LOCAL_ADDRESS_CHANGED = 4,
        PEER_ADDRESS_CHANGED = 5,
        PEER_STREAM_STARTED = 6,
        STREAMS_AVAILABLE = 7,
        PEER_NEEDS_STREAMS = 8,
        IDEAL_PROCESSOR_CHANGED = 9,
        DATAGRAM_STATE_CHANGED = 10,
        DATAGRAM_RECEIVED = 11,
        DATAGRAM_SEND_STATE_CHANGED = 12,
        RESUMED = 13,
        RESUMPTION_TICKET_RECEIVED = 14
    }

    internal enum QUIC_STREAM_EVENT : uint
    {
        START_COMPLETE = 0,
        RECEIVE = 1,
        SEND_COMPLETE = 2,
        PEER_SEND_SHUTDOWN = 3,
        PEER_SEND_ABORTED = 4,
        PEER_RECEIVE_ABORTED = 5,
        SEND_SHUTDOWN_COMPLETE = 6,
        SHUTDOWN_COMPLETE = 7,
        IDEAL_SEND_BUFFER_SIZE = 8,
    }

    internal enum QUIC_SERVER_RESUMPTION_LEVEL : byte
    {
        NO_RESUME,
        RESUME_ONLY,
        RESUME_AND_ZERORTT
    }
}
