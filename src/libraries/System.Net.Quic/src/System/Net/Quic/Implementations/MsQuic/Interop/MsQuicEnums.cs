// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal enum QUIC_EXECUTION_PROFILE : uint
    {
        QUIC_EXECUTION_PROFILE_LOW_LATENCY, // Default
        QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT,
        QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER,
        QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME,
    }

    internal enum QUIC_CREDENTIAL_TYPE : uint
    {
        NONE,
        HASH,
        HASH_STORE,
        CONTEXT,
        FILE,
        FILE_PROTECTED,
        PKCS12,
    }

    [Flags]
    internal enum QUIC_CREDENTIAL_FLAGS : uint
    {
        NONE = 0x00000000,
        CLIENT = 0x00000001, // Lack of client flag indicates server.
        LOAD_ASYNCHRONOUS = 0x00000002,
        NO_CERTIFICATE_VALIDATION = 0x00000004,
        ENABLE_OCSP = 0x00000008, // Schannel only currently.
        INDICATE_CERTIFICATE_RECEIVED = 0x00000010,
        DEFER_CERTIFICATE_VALIDATION = 0x00000020, // Schannel only currently.
        REQUIRE_CLIENT_AUTHENTICATION = 0x00000040, // Schannel only currently.
        USE_TLS_BUILTIN_CERTIFICATE_VALIDATION = 0x00000080,
        USE_PORTABLE_CERTIFICATES = 0x00004000,
    }

    internal enum QUIC_CERTIFICATE_HASH_STORE_FLAGS
    {
        QUIC_CERTIFICATE_HASH_STORE_FLAG_NONE = 0x0000,
        QUIC_CERTIFICATE_HASH_STORE_FLAG_MACHINE_STORE = 0x0001,
    }

    [Flags]
    internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS : uint
    {
        NONE = 0x0000,
        SILENT = 0x0001, // Don't send the close frame over the network.
    }

    internal enum QUIC_SERVER_RESUMPTION_LEVEL : uint
    {
        NO_RESUME,
        RESUME_ONLY,
        RESUME_AND_ZERORTT,
    }

    [Flags]
    internal enum QUIC_STREAM_OPEN_FLAGS : uint
    {
        NONE = 0x0000,
        UNIDIRECTIONAL = 0x0001, // Indicates the stream is unidirectional.
        ZERO_RTT = 0x0002, // The stream was opened via a 0-RTT packet.
    }

    [Flags]
    internal enum QUIC_STREAM_START_FLAGS : uint
    {
        NONE = 0x0000,
        FAIL_BLOCKED = 0x0001, // Only opens the stream if flow control allows.
        IMMEDIATE = 0x0002, // Immediately informs peer that stream is open.
        ASYNC = 0x0004, // Don't block the API call to wait for completion.
        SHUTDOWN_ON_FAIL = 0x0008, // Shutdown the stream immediately after start failure.
    }

    [Flags]
    internal enum QUIC_STREAM_SHUTDOWN_FLAGS : uint
    {
        NONE = 0x0000,
        GRACEFUL = 0x0001, // Cleanly closes the send path.
        ABORT_SEND = 0x0002, // Abruptly closes the send path.
        ABORT_RECEIVE = 0x0004, // Abruptly closes the receive path.
        ABORT = 0x0006, // Abruptly closes both send and receive paths.
        IMMEDIATE = 0x0008, // Immediately sends completion events to app.
    }

    [Flags]
    internal enum QUIC_RECEIVE_FLAGS : uint
    {
        NONE = 0x0000,
        ZERO_RTT = 0x0001, // Data was encrypted with 0-RTT key.
        FIN = 0x0002, // FIN was included with this data.
    }

    [Flags]
    internal enum QUIC_SEND_FLAGS : uint
    {
        NONE = 0x0000,
        ALLOW_0_RTT = 0x0001, // Allows the use of encrypting with 0-RTT key.
        START = 0x0002, // Asynchronously starts the stream with the sent data.
        FIN = 0x0004, // Indicates the request is the one last sent on the stream.
        DGRAM_PRIORITY = 0x0008, // Indicates the datagram is higher priority than others.
        DELAY_SEND = 0x0010, // Indicates the send should be delayed because more will be queued soon.
    }

    internal enum QUIC_PARAM_LEVEL : uint
    {
        GLOBAL,
        REGISTRATION,
        CONFIGURATION,
        LISTENER,
        CONNECTION,
        TLS,
        STREAM,
    }

    internal enum QUIC_PARAM_GLOBAL : uint
    {
        RETRY_MEMORY_PERCENT = 0, // uint16_t
        SUPPORTED_VERSIONS = 1, // uint32_t[] - network byte order
        LOAD_BALANCING_MODE = 2, // uint16_t - QUIC_LOAD_BALANCING_MODE
        PERF_COUNTERS = 3, // uint64_t[] - Array size is QUIC_PERF_COUNTER_MAX
        SETTINGS = 4, // QUIC_SETTINGS
    }

    internal enum QUIC_PARAM_REGISTRATION : uint
    {
        CID_PREFIX = 0, // uint8_t[]
    }

    internal enum QUIC_PARAM_LISTENER : uint
    {
        LOCAL_ADDRESS = 0, // QUIC_ADDR
        STATS = 1, // QUIC_LISTENER_STATISTICS
    }

    internal enum QUIC_PARAM_CONN : uint
    {
        QUIC_VERSION = 0, // uint32_t
        LOCAL_ADDRESS = 1, // QUIC_ADDR
        REMOTE_ADDRESS = 2, // QUIC_ADDR
        IDEAL_PROCESSOR = 3, // uint16_t
        SETTINGS = 4, // QUIC_SETTINGS
        STATISTICS = 5, // QUIC_STATISTICS
        STATISTICS_PLAT = 6, // QUIC_STATISTICS
        SHARE_UDP_BINDING = 7, // uint8_t (BOOLEAN)
        LOCAL_BIDI_STREAM_COUNT = 8, // uint16_t
        LOCAL_UNIDI_STREAM_COUNT = 9, // uint16_t
        MAX_STREAM_IDS = 10, // uint64_t[4]
        CLOSE_REASON_PHRASE = 11, // char[]
        STREAM_SCHEDULING_SCHEME = 12, // QUIC_STREAM_SCHEDULING_SCHEME
        DATAGRAM_RECEIVE_ENABLED = 13, // uint8_t (BOOLEAN)
        DATAGRAM_SEND_ENABLED = 14, // uint8_t (BOOLEAN)
        DISABLE_1RTT_ENCRYPTION = 15, // uint8_t (BOOLEAN)
        RESUMPTION_TICKET = 16, // uint8_t[]
        PEER_CERTIFICATE_VALID = 17, // uint8_t (BOOLEAN)
    }

    internal enum QUIC_PARAM_STREAM : uint
    {
        ID = 0, // QUIC_UINT62
        ZERRTT_LENGTH = 1, // uint64_t
        IDEAL_SEND_BUFFER_SIZE = 2, // uint64_t - bytes
    }

    internal enum QUIC_LISTENER_EVENT : uint
    {
        NEW_CONNECTION = 0,
    }

    internal enum QUIC_CONNECTION_EVENT_TYPE : uint
    {
        CONNECTED = 0,
        SHUTDOWN_INITIATED_BY_TRANSPORT = 1, // The transport started the shutdown process.
        SHUTDOWN_INITIATED_BY_PEER = 2, // The peer application started the shutdown process.
        SHUTDOWN_COMPLETE = 3, // Ready for the handle to be closed.
        LOCAL_ADDRESS_CHANGED = 4,
        PEER_ADDRESS_CHANGED = 5,
        PEER_STREAM_STARTED = 6,
        STREAMS_AVAILABLE = 7,
        PEER_NEEDS_STREAMS = 8,
        IDEAL_PROCESSOR_CHANGED = 9,
        DATAGRAM_STATE_CHANGED = 10,
        DATAGRAM_RECEIVED = 11,
        DATAGRAM_SEND_STATE_CHANGED = 12,
        RESUMED = 13, // Server-only; provides resumption data, if any.
        RESUMPTION_TICKET_RECEIVED = 14, // Client-only; provides ticket to persist, if any.
        PEER_CERTIFICATE_RECEIVED = 15, // Only with QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED set.
    }

    internal enum QUIC_STREAM_EVENT_TYPE : uint
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

    internal enum QUIC_ADDRESS_FAMILY : ushort
    {
        UNSPEC = 0,
        INET = 2,
        INET6 = 23,
    }
}
