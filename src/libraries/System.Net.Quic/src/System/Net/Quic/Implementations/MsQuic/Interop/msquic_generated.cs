//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//

using System.Runtime.InteropServices;

namespace Microsoft.Quic
{
    internal partial struct QUIC_HANDLE
    {
    }

    internal enum QUIC_EXECUTION_PROFILE
    {
        QUIC_EXECUTION_PROFILE_LOW_LATENCY,
        QUIC_EXECUTION_PROFILE_TYPE_MAX_THROUGHPUT,
        QUIC_EXECUTION_PROFILE_TYPE_SCAVENGER,
        QUIC_EXECUTION_PROFILE_TYPE_REAL_TIME,
    }

    internal enum QUIC_LOAD_BALANCING_MODE
    {
        QUIC_LOAD_BALANCING_DISABLED,
        QUIC_LOAD_BALANCING_SERVER_ID_IP,
    }

    internal enum QUIC_CREDENTIAL_TYPE
    {
        QUIC_CREDENTIAL_TYPE_NONE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_HASH_STORE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_CONTEXT,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_FILE_PROTECTED,
        QUIC_CREDENTIAL_TYPE_CERTIFICATE_PKCS12,
    }

    [System.Flags]
    internal enum QUIC_CREDENTIAL_FLAGS
    {
        QUIC_CREDENTIAL_FLAG_NONE = 0x00000000,
        QUIC_CREDENTIAL_FLAG_CLIENT = 0x00000001,
        QUIC_CREDENTIAL_FLAG_LOAD_ASYNCHRONOUS = 0x00000002,
        QUIC_CREDENTIAL_FLAG_NO_CERTIFICATE_VALIDATION = 0x00000004,
        QUIC_CREDENTIAL_FLAG_ENABLE_OCSP = 0x00000008,
        QUIC_CREDENTIAL_FLAG_INDICATE_CERTIFICATE_RECEIVED = 0x00000010,
        QUIC_CREDENTIAL_FLAG_DEFER_CERTIFICATE_VALIDATION = 0x00000020,
        QUIC_CREDENTIAL_FLAG_REQUIRE_CLIENT_AUTHENTICATION = 0x00000040,
        QUIC_CREDENTIAL_FLAG_USE_TLS_BUILTIN_CERTIFICATE_VALIDATION = 0x00000080,
        QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_END_CERT = 0x00000100,
        QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN = 0x00000200,
        QUIC_CREDENTIAL_FLAG_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000400,
        QUIC_CREDENTIAL_FLAG_IGNORE_NO_REVOCATION_CHECK = 0x00000800,
        QUIC_CREDENTIAL_FLAG_IGNORE_REVOCATION_OFFLINE = 0x00001000,
        QUIC_CREDENTIAL_FLAG_SET_ALLOWED_CIPHER_SUITES = 0x00002000,
        QUIC_CREDENTIAL_FLAG_USE_PORTABLE_CERTIFICATES = 0x00004000,
        QUIC_CREDENTIAL_FLAG_USE_SUPPLIED_CREDENTIALS = 0x00008000,
    }

    [System.Flags]
    internal enum QUIC_ALLOWED_CIPHER_SUITE_FLAGS
    {
        QUIC_ALLOWED_CIPHER_SUITE_NONE = 0x0,
        QUIC_ALLOWED_CIPHER_SUITE_AES_128_GCM_SHA256 = 0x1,
        QUIC_ALLOWED_CIPHER_SUITE_AES_256_GCM_SHA384 = 0x2,
        QUIC_ALLOWED_CIPHER_SUITE_CHACHA20_POLY1305_SHA256 = 0x4,
    }

    [System.Flags]
    internal enum QUIC_CERTIFICATE_HASH_STORE_FLAGS
    {
        QUIC_CERTIFICATE_HASH_STORE_FLAG_NONE = 0x0000,
        QUIC_CERTIFICATE_HASH_STORE_FLAG_MACHINE_STORE = 0x0001,
    }

    [System.Flags]
    internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS
    {
        QUIC_CONNECTION_SHUTDOWN_FLAG_NONE = 0x0000,
        QUIC_CONNECTION_SHUTDOWN_FLAG_SILENT = 0x0001,
    }

    internal enum QUIC_SERVER_RESUMPTION_LEVEL
    {
        QUIC_SERVER_NO_RESUME,
        QUIC_SERVER_RESUME_ONLY,
        QUIC_SERVER_RESUME_AND_ZERORTT,
    }

    [System.Flags]
    internal enum QUIC_SEND_RESUMPTION_FLAGS
    {
        QUIC_SEND_RESUMPTION_FLAG_NONE = 0x0000,
        QUIC_SEND_RESUMPTION_FLAG_FINAL = 0x0001,
    }

    internal enum QUIC_STREAM_SCHEDULING_SCHEME
    {
        QUIC_STREAM_SCHEDULING_SCHEME_FIFO = 0x0000,
        QUIC_STREAM_SCHEDULING_SCHEME_ROUND_ROBIN = 0x0001,
        QUIC_STREAM_SCHEDULING_SCHEME_COUNT,
    }

    [System.Flags]
    internal enum QUIC_STREAM_OPEN_FLAGS
    {
        QUIC_STREAM_OPEN_FLAG_NONE = 0x0000,
        QUIC_STREAM_OPEN_FLAG_UNIDIRECTIONAL = 0x0001,
        QUIC_STREAM_OPEN_FLAG_0_RTT = 0x0002,
    }

    [System.Flags]
    internal enum QUIC_STREAM_START_FLAGS
    {
        QUIC_STREAM_START_FLAG_NONE = 0x0000,
        QUIC_STREAM_START_FLAG_IMMEDIATE = 0x0001,
        QUIC_STREAM_START_FLAG_FAIL_BLOCKED = 0x0002,
        QUIC_STREAM_START_FLAG_SHUTDOWN_ON_FAIL = 0x0004,
        QUIC_STREAM_START_FLAG_INDICATE_PEER_ACCEPT = 0x0008,
    }

    [System.Flags]
    internal enum QUIC_STREAM_SHUTDOWN_FLAGS
    {
        QUIC_STREAM_SHUTDOWN_FLAG_NONE = 0x0000,
        QUIC_STREAM_SHUTDOWN_FLAG_GRACEFUL = 0x0001,
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_SEND = 0x0002,
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT_RECEIVE = 0x0004,
        QUIC_STREAM_SHUTDOWN_FLAG_ABORT = 0x0006,
        QUIC_STREAM_SHUTDOWN_FLAG_IMMEDIATE = 0x0008,
        QUIC_STREAM_SHUTDOWN_FLAG_INLINE = 0x0010,
    }

    [System.Flags]
    internal enum QUIC_RECEIVE_FLAGS
    {
        QUIC_RECEIVE_FLAG_NONE = 0x0000,
        QUIC_RECEIVE_FLAG_0_RTT = 0x0001,
        QUIC_RECEIVE_FLAG_FIN = 0x0002,
    }

    [System.Flags]
    internal enum QUIC_SEND_FLAGS
    {
        QUIC_SEND_FLAG_NONE = 0x0000,
        QUIC_SEND_FLAG_ALLOW_0_RTT = 0x0001,
        QUIC_SEND_FLAG_START = 0x0002,
        QUIC_SEND_FLAG_FIN = 0x0004,
        QUIC_SEND_FLAG_DGRAM_PRIORITY = 0x0008,
        QUIC_SEND_FLAG_DELAY_SEND = 0x0010,
    }

    internal enum QUIC_DATAGRAM_SEND_STATE
    {
        QUIC_DATAGRAM_SEND_UNKNOWN,
        QUIC_DATAGRAM_SEND_SENT,
        QUIC_DATAGRAM_SEND_LOST_SUSPECT,
        QUIC_DATAGRAM_SEND_LOST_DISCARDED,
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED,
        QUIC_DATAGRAM_SEND_ACKNOWLEDGED_SPURIOUS,
        QUIC_DATAGRAM_SEND_CANCELED,
    }

    internal unsafe partial struct QUIC_REGISTRATION_CONFIG
    {
        [NativeTypeName("const char *")]
        public sbyte* AppName;

        public QUIC_EXECUTION_PROFILE ExecutionProfile;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_HASH
    {
        [NativeTypeName("uint8_t [20]")]
        public fixed byte ShaHash[20];
    }

    internal unsafe partial struct QUIC_CERTIFICATE_HASH_STORE
    {
        public QUIC_CERTIFICATE_HASH_STORE_FLAGS Flags;

        [NativeTypeName("uint8_t [20]")]
        public fixed byte ShaHash[20];

        [NativeTypeName("char [128]")]
        public fixed sbyte StoreName[128];
    }

    internal unsafe partial struct QUIC_CERTIFICATE_FILE
    {
        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyFile;

        [NativeTypeName("const char *")]
        public sbyte* CertificateFile;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_FILE_PROTECTED
    {
        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyFile;

        [NativeTypeName("const char *")]
        public sbyte* CertificateFile;

        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyPassword;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_PKCS12
    {
        [NativeTypeName("const uint8_t *")]
        public byte* Asn1Blob;

        [NativeTypeName("uint32_t")]
        public uint Asn1BlobLength;

        [NativeTypeName("const char *")]
        public sbyte* PrivateKeyPassword;
    }

    internal unsafe partial struct QUIC_CREDENTIAL_CONFIG
    {
        public QUIC_CREDENTIAL_TYPE Type;

        public QUIC_CREDENTIAL_FLAGS Flags;

        [NativeTypeName("QUIC_CREDENTIAL_CONFIG::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("const char *")]
        public sbyte* Principal;

        public void* Reserved;

        [NativeTypeName("QUIC_CREDENTIAL_LOAD_COMPLETE_HANDLER")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, int, void> AsyncHandler;

        public QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

        public ref QUIC_CERTIFICATE_HASH* CertificateHash
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateHash;
            }
        }

        public ref QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateHashStore;
            }
        }

        public ref void* CertificateContext
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateContext;
            }
        }

        public ref QUIC_CERTIFICATE_FILE* CertificateFile
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateFile;
            }
        }

        public ref QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateFileProtected;
            }
        }

        public ref QUIC_CERTIFICATE_PKCS12* CertificatePkcs12
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificatePkcs12;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            public QUIC_CERTIFICATE_HASH* CertificateHash;

            [FieldOffset(0)]
            public QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore;

            [FieldOffset(0)]
            [NativeTypeName("QUIC_CERTIFICATE *")]
            public void* CertificateContext;

            [FieldOffset(0)]
            public QUIC_CERTIFICATE_FILE* CertificateFile;

            [FieldOffset(0)]
            public QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected;

            [FieldOffset(0)]
            public QUIC_CERTIFICATE_PKCS12* CertificatePkcs12;
        }
    }

    internal unsafe partial struct QUIC_TICKET_KEY_CONFIG
    {
        [NativeTypeName("uint8_t [16]")]
        public fixed byte Id[16];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte Material[64];

        [NativeTypeName("uint8_t")]
        public byte MaterialLength;
    }

    internal unsafe partial struct QUIC_BUFFER
    {
        [NativeTypeName("uint32_t")]
        public uint Length;

        [NativeTypeName("uint8_t *")]
        public byte* Buffer;
    }

    internal unsafe partial struct QUIC_NEW_CONNECTION_INFO
    {
        [NativeTypeName("uint32_t")]
        public uint QuicVersion;

        [NativeTypeName("const QUIC_ADDR *")]
        public QuicAddr* LocalAddress;

        [NativeTypeName("const QUIC_ADDR *")]
        public QuicAddr* RemoteAddress;

        [NativeTypeName("uint32_t")]
        public uint CryptoBufferLength;

        [NativeTypeName("uint16_t")]
        public ushort ClientAlpnListLength;

        [NativeTypeName("uint16_t")]
        public ushort ServerNameLength;

        [NativeTypeName("uint8_t")]
        public byte NegotiatedAlpnLength;

        [NativeTypeName("const uint8_t *")]
        public byte* CryptoBuffer;

        [NativeTypeName("const uint8_t *")]
        public byte* ClientAlpnList;

        [NativeTypeName("const uint8_t *")]
        public byte* NegotiatedAlpn;

        [NativeTypeName("const char *")]
        public sbyte* ServerName;
    }

    internal enum QUIC_TLS_PROTOCOL_VERSION
    {
        QUIC_TLS_PROTOCOL_UNKNOWN = 0,
        QUIC_TLS_PROTOCOL_1_3 = 0x3000,
    }

    internal enum QUIC_CIPHER_ALGORITHM
    {
        QUIC_CIPHER_ALGORITHM_NONE = 0,
        QUIC_CIPHER_ALGORITHM_AES_128 = 0x660E,
        QUIC_CIPHER_ALGORITHM_AES_256 = 0x6610,
        QUIC_CIPHER_ALGORITHM_CHACHA20 = 0x6612,
    }

    internal enum QUIC_HASH_ALGORITHM
    {
        QUIC_HASH_ALGORITHM_NONE = 0,
        QUIC_HASH_ALGORITHM_SHA_256 = 0x800C,
        QUIC_HASH_ALGORITHM_SHA_384 = 0x800D,
    }

    internal enum QUIC_KEY_EXCHANGE_ALGORITHM
    {
        QUIC_KEY_EXCHANGE_ALGORITHM_NONE = 0,
    }

    internal enum QUIC_CIPHER_SUITE
    {
        QUIC_CIPHER_SUITE_TLS_AES_128_GCM_SHA256 = 0x1301,
        QUIC_CIPHER_SUITE_TLS_AES_256_GCM_SHA384 = 0x1302,
        QUIC_CIPHER_SUITE_TLS_CHACHA20_POLY1305_SHA256 = 0x1303,
    }

    internal enum QUIC_CONGESTION_CONTROL_ALGORITHM
    {
        QUIC_CONGESTION_CONTROL_ALGORITHM_CUBIC,
        QUIC_CONGESTION_CONTROL_ALGORITHM_MAX,
    }

    internal partial struct QUIC_HANDSHAKE_INFO
    {
        public QUIC_TLS_PROTOCOL_VERSION TlsProtocolVersion;

        public QUIC_CIPHER_ALGORITHM CipherAlgorithm;

        [NativeTypeName("int32_t")]
        public int CipherStrength;

        public QUIC_HASH_ALGORITHM Hash;

        [NativeTypeName("int32_t")]
        public int HashStrength;

        public QUIC_KEY_EXCHANGE_ALGORITHM KeyExchangeAlgorithm;

        [NativeTypeName("int32_t")]
        public int KeyExchangeStrength;

        public QUIC_CIPHER_SUITE CipherSuite;
    }

    internal partial struct QUIC_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        public ulong CorrelationId;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint VersionNegotiation
        {
            get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint StatelessRetry
        {
            get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ResumptionAttempted
        {
            get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ResumptionSucceeded
        {
            get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint Rtt;

        [NativeTypeName("uint32_t")]
        public uint MinRtt;

        [NativeTypeName("uint32_t")]
        public uint MaxRtt;

        [NativeTypeName("struct (anonymous struct)")]
        public _Timing_e__Struct Timing;

        [NativeTypeName("struct (anonymous struct)")]
        public _Handshake_e__Struct Handshake;

        [NativeTypeName("struct (anonymous struct)")]
        public _Send_e__Struct Send;

        [NativeTypeName("struct (anonymous struct)")]
        public _Recv_e__Struct Recv;

        [NativeTypeName("struct (anonymous struct)")]
        public _Misc_e__Struct Misc;

        internal partial struct _Timing_e__Struct
        {
            [NativeTypeName("uint64_t")]
            public ulong Start;

            [NativeTypeName("uint64_t")]
            public ulong InitialFlightEnd;

            [NativeTypeName("uint64_t")]
            public ulong HandshakeFlightEnd;
        }

        internal partial struct _Handshake_e__Struct
        {
            [NativeTypeName("uint32_t")]
            public uint ClientFlight1Bytes;

            [NativeTypeName("uint32_t")]
            public uint ServerFlight1Bytes;

            [NativeTypeName("uint32_t")]
            public uint ClientFlight2Bytes;
        }

        internal partial struct _Send_e__Struct
        {
            [NativeTypeName("uint16_t")]
            public ushort PathMtu;

            [NativeTypeName("uint64_t")]
            public ulong TotalPackets;

            [NativeTypeName("uint64_t")]
            public ulong RetransmittablePackets;

            [NativeTypeName("uint64_t")]
            public ulong SuspectedLostPackets;

            [NativeTypeName("uint64_t")]
            public ulong SpuriousLostPackets;

            [NativeTypeName("uint64_t")]
            public ulong TotalBytes;

            [NativeTypeName("uint64_t")]
            public ulong TotalStreamBytes;

            [NativeTypeName("uint32_t")]
            public uint CongestionCount;

            [NativeTypeName("uint32_t")]
            public uint PersistentCongestionCount;
        }

        internal partial struct _Recv_e__Struct
        {
            [NativeTypeName("uint64_t")]
            public ulong TotalPackets;

            [NativeTypeName("uint64_t")]
            public ulong ReorderedPackets;

            [NativeTypeName("uint64_t")]
            public ulong DroppedPackets;

            [NativeTypeName("uint64_t")]
            public ulong DuplicatePackets;

            [NativeTypeName("uint64_t")]
            public ulong TotalBytes;

            [NativeTypeName("uint64_t")]
            public ulong TotalStreamBytes;

            [NativeTypeName("uint64_t")]
            public ulong DecryptionFailures;

            [NativeTypeName("uint64_t")]
            public ulong ValidAckFrames;
        }

        internal partial struct _Misc_e__Struct
        {
            [NativeTypeName("uint32_t")]
            public uint KeyUpdateCount;
        }
    }

    internal partial struct QUIC_STATISTICS_V2
    {
        [NativeTypeName("uint64_t")]
        public ulong CorrelationId;

        public uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        public uint VersionNegotiation
        {
            get
            {
                return _bitfield & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~0x1u) | (value & 0x1u);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint StatelessRetry
        {
            get
            {
                return (_bitfield >> 1) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ResumptionAttempted
        {
            get
            {
                return (_bitfield >> 2) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        public uint ResumptionSucceeded
        {
            get
            {
                return (_bitfield >> 3) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3);
            }
        }

        [NativeTypeName("uint32_t")]
        public uint Rtt;

        [NativeTypeName("uint32_t")]
        public uint MinRtt;

        [NativeTypeName("uint32_t")]
        public uint MaxRtt;

        [NativeTypeName("uint64_t")]
        public ulong TimingStart;

        [NativeTypeName("uint64_t")]
        public ulong TimingInitialFlightEnd;

        [NativeTypeName("uint64_t")]
        public ulong TimingHandshakeFlightEnd;

        [NativeTypeName("uint32_t")]
        public uint HandshakeClientFlight1Bytes;

        [NativeTypeName("uint32_t")]
        public uint HandshakeServerFlight1Bytes;

        [NativeTypeName("uint32_t")]
        public uint HandshakeClientFlight2Bytes;

        [NativeTypeName("uint16_t")]
        public ushort SendPathMtu;

        [NativeTypeName("uint64_t")]
        public ulong SendTotalPackets;

        [NativeTypeName("uint64_t")]
        public ulong SendRetransmittablePackets;

        [NativeTypeName("uint64_t")]
        public ulong SendSuspectedLostPackets;

        [NativeTypeName("uint64_t")]
        public ulong SendSpuriousLostPackets;

        [NativeTypeName("uint64_t")]
        public ulong SendTotalBytes;

        [NativeTypeName("uint64_t")]
        public ulong SendTotalStreamBytes;

        [NativeTypeName("uint32_t")]
        public uint SendCongestionCount;

        [NativeTypeName("uint32_t")]
        public uint SendPersistentCongestionCount;

        [NativeTypeName("uint64_t")]
        public ulong RecvTotalPackets;

        [NativeTypeName("uint64_t")]
        public ulong RecvReorderedPackets;

        [NativeTypeName("uint64_t")]
        public ulong RecvDroppedPackets;

        [NativeTypeName("uint64_t")]
        public ulong RecvDuplicatePackets;

        [NativeTypeName("uint64_t")]
        public ulong RecvTotalBytes;

        [NativeTypeName("uint64_t")]
        public ulong RecvTotalStreamBytes;

        [NativeTypeName("uint64_t")]
        public ulong RecvDecryptionFailures;

        [NativeTypeName("uint64_t")]
        public ulong RecvValidAckFrames;

        [NativeTypeName("uint32_t")]
        public uint KeyUpdateCount;

        [NativeTypeName("uint32_t")]
        public uint SendCongestionWindow;
    }

    internal partial struct QUIC_LISTENER_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        public ulong TotalAcceptedConnections;

        [NativeTypeName("uint64_t")]
        public ulong TotalRejectedConnections;

        [NativeTypeName("uint64_t")]
        public ulong BindingRecvDroppedPackets;
    }

    internal enum QUIC_PERFORMANCE_COUNTERS
    {
        QUIC_PERF_COUNTER_CONN_CREATED,
        QUIC_PERF_COUNTER_CONN_HANDSHAKE_FAIL,
        QUIC_PERF_COUNTER_CONN_APP_REJECT,
        QUIC_PERF_COUNTER_CONN_RESUMED,
        QUIC_PERF_COUNTER_CONN_ACTIVE,
        QUIC_PERF_COUNTER_CONN_CONNECTED,
        QUIC_PERF_COUNTER_CONN_PROTOCOL_ERRORS,
        QUIC_PERF_COUNTER_CONN_NO_ALPN,
        QUIC_PERF_COUNTER_STRM_ACTIVE,
        QUIC_PERF_COUNTER_PKTS_SUSPECTED_LOST,
        QUIC_PERF_COUNTER_PKTS_DROPPED,
        QUIC_PERF_COUNTER_PKTS_DECRYPTION_FAIL,
        QUIC_PERF_COUNTER_UDP_RECV,
        QUIC_PERF_COUNTER_UDP_SEND,
        QUIC_PERF_COUNTER_UDP_RECV_BYTES,
        QUIC_PERF_COUNTER_UDP_SEND_BYTES,
        QUIC_PERF_COUNTER_UDP_RECV_EVENTS,
        QUIC_PERF_COUNTER_UDP_SEND_CALLS,
        QUIC_PERF_COUNTER_APP_SEND_BYTES,
        QUIC_PERF_COUNTER_APP_RECV_BYTES,
        QUIC_PERF_COUNTER_CONN_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_CONN_OPER_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_CONN_OPER_QUEUED,
        QUIC_PERF_COUNTER_CONN_OPER_COMPLETED,
        QUIC_PERF_COUNTER_WORK_OPER_QUEUE_DEPTH,
        QUIC_PERF_COUNTER_WORK_OPER_QUEUED,
        QUIC_PERF_COUNTER_WORK_OPER_COMPLETED,
        QUIC_PERF_COUNTER_PATH_VALIDATED,
        QUIC_PERF_COUNTER_PATH_FAILURE,
        QUIC_PERF_COUNTER_SEND_STATELESS_RESET,
        QUIC_PERF_COUNTER_SEND_STATELESS_RETRY,
        QUIC_PERF_COUNTER_MAX,
    }

    internal unsafe partial struct QUIC_VERSION_SETTINGS
    {
        [NativeTypeName("uint32_t *")]
        public uint* AcceptableVersions;

        [NativeTypeName("uint32_t *")]
        public uint* OfferedVersions;

        [NativeTypeName("uint32_t *")]
        public uint* FullyDeployedVersions;

        [NativeTypeName("uint32_t")]
        public uint AcceptableVersionsLength;

        [NativeTypeName("uint32_t")]
        public uint OfferedVersionsLength;

        [NativeTypeName("uint32_t")]
        public uint FullyDeployedVersionsLength;
    }

    internal partial struct QUIC_GLOBAL_SETTINGS
    {
        [NativeTypeName("QUIC_GLOBAL_SETTINGS::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("uint16_t")]
        public ushort RetryMemoryLimit;

        [NativeTypeName("uint16_t")]
        public ushort LoadBalancingMode;

        public ref ulong IsSetFlags
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IsSetFlags, 1));
            }
        }

        public ref _Anonymous_e__Union._IsSet_e__Struct IsSet
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IsSet, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _IsSet_e__Struct IsSet;

            internal partial struct _IsSet_e__Struct
            {
                public ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                public ulong RetryMemoryLimit
                {
                    get
                    {
                        return _bitfield & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x1UL) | (value & 0x1UL);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong LoadBalancingMode
                {
                    get
                    {
                        return (_bitfield >> 1) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 1)) | ((value & 0x1UL) << 1);
                    }
                }

                [NativeTypeName("uint64_t : 62")]
                public ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 2) & 0x3FFFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x3FFFFFFFUL << 2)) | ((value & 0x3FFFFFFFUL) << 2);
                    }
                }
            }
        }
    }

    internal partial struct QUIC_SETTINGS
    {
        [NativeTypeName("QUIC_SETTINGS::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        [NativeTypeName("uint64_t")]
        public ulong MaxBytesPerKey;

        [NativeTypeName("uint64_t")]
        public ulong HandshakeIdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        public ulong IdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        public ulong MtuDiscoverySearchCompleteTimeoutUs;

        [NativeTypeName("uint32_t")]
        public uint TlsClientMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        public uint TlsServerMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvWindowDefault;

        [NativeTypeName("uint32_t")]
        public uint StreamRecvBufferDefault;

        [NativeTypeName("uint32_t")]
        public uint ConnFlowControlWindow;

        [NativeTypeName("uint32_t")]
        public uint MaxWorkerQueueDelayUs;

        [NativeTypeName("uint32_t")]
        public uint MaxStatelessOperations;

        [NativeTypeName("uint32_t")]
        public uint InitialWindowPackets;

        [NativeTypeName("uint32_t")]
        public uint SendIdleTimeoutMs;

        [NativeTypeName("uint32_t")]
        public uint InitialRttMs;

        [NativeTypeName("uint32_t")]
        public uint MaxAckDelayMs;

        [NativeTypeName("uint32_t")]
        public uint DisconnectTimeoutMs;

        [NativeTypeName("uint32_t")]
        public uint KeepAliveIntervalMs;

        [NativeTypeName("uint16_t")]
        public ushort CongestionControlAlgorithm;

        [NativeTypeName("uint16_t")]
        public ushort PeerBidiStreamCount;

        [NativeTypeName("uint16_t")]
        public ushort PeerUnidiStreamCount;

        [NativeTypeName("uint16_t")]
        public ushort MaxBindingStatelessOperations;

        [NativeTypeName("uint16_t")]
        public ushort StatelessOperationExpirationMs;

        [NativeTypeName("uint16_t")]
        public ushort MinimumMtu;

        [NativeTypeName("uint16_t")]
        public ushort MaximumMtu;

        public byte _bitfield;

        [NativeTypeName("uint8_t : 1")]
        public byte SendBufferingEnabled
        {
            get
            {
                return (byte)(_bitfield & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        public byte PacingEnabled
        {
            get
            {
                return (byte)((_bitfield >> 1) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        public byte MigrationEnabled
        {
            get
            {
                return (byte)((_bitfield >> 2) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        public byte DatagramReceiveEnabled
        {
            get
            {
                return (byte)((_bitfield >> 3) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3));
            }
        }

        [NativeTypeName("uint8_t : 2")]
        public byte ServerResumptionLevel
        {
            get
            {
                return (byte)((_bitfield >> 4) & 0x3u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x3u << 4)) | ((value & 0x3u) << 4));
            }
        }

        [NativeTypeName("uint8_t : 2")]
        public byte RESERVED
        {
            get
            {
                return (byte)((_bitfield >> 6) & 0x3u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x3u << 6)) | ((value & 0x3u) << 6));
            }
        }

        [NativeTypeName("uint8_t")]
        public byte MaxOperationsPerDrain;

        [NativeTypeName("uint8_t")]
        public byte MtuDiscoveryMissingProbeCount;

        public ref ulong IsSetFlags
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IsSetFlags, 1));
            }
        }

        public ref _Anonymous_e__Union._IsSet_e__Struct IsSet
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IsSet, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            public ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _IsSet_e__Struct IsSet;

            internal partial struct _IsSet_e__Struct
            {
                public ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxBytesPerKey
                {
                    get
                    {
                        return _bitfield & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~0x1UL) | (value & 0x1UL);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong HandshakeIdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 1) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 1)) | ((value & 0x1UL) << 1);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong IdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 2) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 2)) | ((value & 0x1UL) << 2);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MtuDiscoverySearchCompleteTimeoutUs
                {
                    get
                    {
                        return (_bitfield >> 3) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 3)) | ((value & 0x1UL) << 3);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong TlsClientMaxSendBuffer
                {
                    get
                    {
                        return (_bitfield >> 4) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 4)) | ((value & 0x1UL) << 4);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong TlsServerMaxSendBuffer
                {
                    get
                    {
                        return (_bitfield >> 5) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 5)) | ((value & 0x1UL) << 5);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvWindowDefault
                {
                    get
                    {
                        return (_bitfield >> 6) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 6)) | ((value & 0x1UL) << 6);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StreamRecvBufferDefault
                {
                    get
                    {
                        return (_bitfield >> 7) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 7)) | ((value & 0x1UL) << 7);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong ConnFlowControlWindow
                {
                    get
                    {
                        return (_bitfield >> 8) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 8)) | ((value & 0x1UL) << 8);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxWorkerQueueDelayUs
                {
                    get
                    {
                        return (_bitfield >> 9) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 9)) | ((value & 0x1UL) << 9);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxStatelessOperations
                {
                    get
                    {
                        return (_bitfield >> 10) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 10)) | ((value & 0x1UL) << 10);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong InitialWindowPackets
                {
                    get
                    {
                        return (_bitfield >> 11) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 11)) | ((value & 0x1UL) << 11);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong SendIdleTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 12) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 12)) | ((value & 0x1UL) << 12);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong InitialRttMs
                {
                    get
                    {
                        return (_bitfield >> 13) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 13)) | ((value & 0x1UL) << 13);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxAckDelayMs
                {
                    get
                    {
                        return (_bitfield >> 14) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 14)) | ((value & 0x1UL) << 14);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong DisconnectTimeoutMs
                {
                    get
                    {
                        return (_bitfield >> 15) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 15)) | ((value & 0x1UL) << 15);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong KeepAliveIntervalMs
                {
                    get
                    {
                        return (_bitfield >> 16) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 16)) | ((value & 0x1UL) << 16);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong CongestionControlAlgorithm
                {
                    get
                    {
                        return (_bitfield >> 17) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 17)) | ((value & 0x1UL) << 17);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PeerBidiStreamCount
                {
                    get
                    {
                        return (_bitfield >> 18) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 18)) | ((value & 0x1UL) << 18);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PeerUnidiStreamCount
                {
                    get
                    {
                        return (_bitfield >> 19) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 19)) | ((value & 0x1UL) << 19);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxBindingStatelessOperations
                {
                    get
                    {
                        return (_bitfield >> 20) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 20)) | ((value & 0x1UL) << 20);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong StatelessOperationExpirationMs
                {
                    get
                    {
                        return (_bitfield >> 21) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 21)) | ((value & 0x1UL) << 21);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MinimumMtu
                {
                    get
                    {
                        return (_bitfield >> 22) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 22)) | ((value & 0x1UL) << 22);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaximumMtu
                {
                    get
                    {
                        return (_bitfield >> 23) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 23)) | ((value & 0x1UL) << 23);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong SendBufferingEnabled
                {
                    get
                    {
                        return (_bitfield >> 24) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 24)) | ((value & 0x1UL) << 24);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong PacingEnabled
                {
                    get
                    {
                        return (_bitfield >> 25) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 25)) | ((value & 0x1UL) << 25);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MigrationEnabled
                {
                    get
                    {
                        return (_bitfield >> 26) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 26)) | ((value & 0x1UL) << 26);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong DatagramReceiveEnabled
                {
                    get
                    {
                        return (_bitfield >> 27) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 27)) | ((value & 0x1UL) << 27);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong ServerResumptionLevel
                {
                    get
                    {
                        return (_bitfield >> 28) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 28)) | ((value & 0x1UL) << 28);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MaxOperationsPerDrain
                {
                    get
                    {
                        return (_bitfield >> 29) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 29)) | ((value & 0x1UL) << 29);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                public ulong MtuDiscoveryMissingProbeCount
                {
                    get
                    {
                        return (_bitfield >> 30) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 30)) | ((value & 0x1UL) << 30);
                    }
                }

                [NativeTypeName("uint64_t : 33")]
                public ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 31) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 31)) | ((value & 0x1UL) << 31);
                    }
                }
            }
        }
    }

    internal unsafe partial struct QUIC_TLS_SECRETS
    {
        [NativeTypeName("uint8_t")]
        public byte SecretLength;

        [NativeTypeName("struct (anonymous struct)")]
        public _IsSet_e__Struct IsSet;

        [NativeTypeName("uint8_t [32]")]
        public fixed byte ClientRandom[32];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte ClientEarlyTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte ClientHandshakeTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte ServerHandshakeTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte ClientTrafficSecret0[64];

        [NativeTypeName("uint8_t [64]")]
        public fixed byte ServerTrafficSecret0[64];

        internal partial struct _IsSet_e__Struct
        {
            public byte _bitfield;

            [NativeTypeName("uint8_t : 1")]
            public byte ClientRandom
            {
                get
                {
                    return (byte)(_bitfield & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                }
            }

            [NativeTypeName("uint8_t : 1")]
            public byte ClientEarlyTrafficSecret
            {
                get
                {
                    return (byte)((_bitfield >> 1) & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1));
                }
            }

            [NativeTypeName("uint8_t : 1")]
            public byte ClientHandshakeTrafficSecret
            {
                get
                {
                    return (byte)((_bitfield >> 2) & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2));
                }
            }

            [NativeTypeName("uint8_t : 1")]
            public byte ServerHandshakeTrafficSecret
            {
                get
                {
                    return (byte)((_bitfield >> 3) & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x1u << 3)) | ((value & 0x1u) << 3));
                }
            }

            [NativeTypeName("uint8_t : 1")]
            public byte ClientTrafficSecret0
            {
                get
                {
                    return (byte)((_bitfield >> 4) & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4));
                }
            }

            [NativeTypeName("uint8_t : 1")]
            public byte ServerTrafficSecret0
            {
                get
                {
                    return (byte)((_bitfield >> 5) & 0x1u);
                }

                set
                {
                    _bitfield = (byte)((_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5));
                }
            }
        }
    }

    internal unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_W
    {
        [NativeTypeName("unsigned long")]
        public uint Attribute;

        public void* Buffer;
    }

    internal enum QUIC_LISTENER_EVENT_TYPE
    {
        QUIC_LISTENER_EVENT_NEW_CONNECTION = 0,
        QUIC_LISTENER_EVENT_STOP_COMPLETE = 1,
    }

    internal partial struct QUIC_LISTENER_EVENT
    {
        public QUIC_LISTENER_EVENT_TYPE Type;

        [NativeTypeName("QUIC_LISTENER_EVENT::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        public ref _Anonymous_e__Union._NEW_CONNECTION_e__Struct NEW_CONNECTION
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.NEW_CONNECTION, 1));
            }
        }

        public ref _Anonymous_e__Union._STOP_COMPLETE_e__Struct STOP_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.STOP_COMPLETE, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _NEW_CONNECTION_e__Struct NEW_CONNECTION;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _STOP_COMPLETE_e__Struct STOP_COMPLETE;

            internal unsafe partial struct _NEW_CONNECTION_e__Struct
            {
                [NativeTypeName("const QUIC_NEW_CONNECTION_INFO *")]
                public QUIC_NEW_CONNECTION_INFO* Info;

                [NativeTypeName("HQUIC")]
                public QUIC_HANDLE* Connection;
            }

            internal partial struct _STOP_COMPLETE_e__Struct
            {
                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte AppCloseInProgress
                {
                    get
                    {
                        return (byte)(_bitfield & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                    }
                }

                [NativeTypeName("BOOLEAN : 7")]
                public byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x7Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x7Fu << 1)) | ((value & 0x7Fu) << 1));
                    }
                }
            }
        }
    }

    internal enum QUIC_CONNECTION_EVENT_TYPE
    {
        QUIC_CONNECTION_EVENT_CONNECTED = 0,
        QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_TRANSPORT = 1,
        QUIC_CONNECTION_EVENT_SHUTDOWN_INITIATED_BY_PEER = 2,
        QUIC_CONNECTION_EVENT_SHUTDOWN_COMPLETE = 3,
        QUIC_CONNECTION_EVENT_LOCAL_ADDRESS_CHANGED = 4,
        QUIC_CONNECTION_EVENT_PEER_ADDRESS_CHANGED = 5,
        QUIC_CONNECTION_EVENT_PEER_STREAM_STARTED = 6,
        QUIC_CONNECTION_EVENT_STREAMS_AVAILABLE = 7,
        QUIC_CONNECTION_EVENT_PEER_NEEDS_STREAMS = 8,
        QUIC_CONNECTION_EVENT_IDEAL_PROCESSOR_CHANGED = 9,
        QUIC_CONNECTION_EVENT_DATAGRAM_STATE_CHANGED = 10,
        QUIC_CONNECTION_EVENT_DATAGRAM_RECEIVED = 11,
        QUIC_CONNECTION_EVENT_DATAGRAM_SEND_STATE_CHANGED = 12,
        QUIC_CONNECTION_EVENT_RESUMED = 13,
        QUIC_CONNECTION_EVENT_RESUMPTION_TICKET_RECEIVED = 14,
        QUIC_CONNECTION_EVENT_PEER_CERTIFICATE_RECEIVED = 15,
    }

    internal partial struct QUIC_CONNECTION_EVENT
    {
        public QUIC_CONNECTION_EVENT_TYPE Type;

        [NativeTypeName("QUIC_CONNECTION_EVENT::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        public ref _Anonymous_e__Union._CONNECTED_e__Struct CONNECTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.CONNECTED, 1));
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_INITIATED_BY_TRANSPORT, 1));
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_INITIATED_BY_PEER, 1));
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_COMPLETE, 1));
            }
        }

        public ref _Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.LOCAL_ADDRESS_CHANGED, 1));
            }
        }

        public ref _Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_ADDRESS_CHANGED, 1));
            }
        }

        public ref _Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_STREAM_STARTED, 1));
            }
        }

        public ref _Anonymous_e__Union._STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.STREAMS_AVAILABLE, 1));
            }
        }

        public ref _Anonymous_e__Union._IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IDEAL_PROCESSOR_CHANGED, 1));
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_STATE_CHANGED, 1));
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_RECEIVED, 1));
            }
        }

        public ref _Anonymous_e__Union._DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_SEND_STATE_CHANGED, 1));
            }
        }

        public ref _Anonymous_e__Union._RESUMED_e__Struct RESUMED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RESUMED, 1));
            }
        }

        public ref _Anonymous_e__Union._RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RESUMPTION_TICKET_RECEIVED, 1));
            }
        }

        public ref _Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_CERTIFICATE_RECEIVED, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _CONNECTED_e__Struct CONNECTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _RESUMED_e__Struct RESUMED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED;

            internal unsafe partial struct _CONNECTED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte SessionResumed;

                [NativeTypeName("uint8_t")]
                public byte NegotiatedAlpnLength;

                [NativeTypeName("const uint8_t *")]
                public byte* NegotiatedAlpn;
            }

            internal partial struct _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct
            {
                [NativeTypeName("HRESULT")]
                public int Status;
            }

            internal partial struct _SHUTDOWN_INITIATED_BY_PEER_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            internal partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte HandshakeCompleted
                {
                    get
                    {
                        return (byte)(_bitfield & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                    }
                }

                [NativeTypeName("BOOLEAN : 1")]
                public byte PeerAcknowledgedShutdown
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1u << 1)) | ((value & 0x1u) << 1));
                    }
                }

                [NativeTypeName("BOOLEAN : 1")]
                public byte AppCloseInProgress
                {
                    get
                    {
                        return (byte)((_bitfield >> 2) & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1u << 2)) | ((value & 0x1u) << 2));
                    }
                }
            }

            internal unsafe partial struct _LOCAL_ADDRESS_CHANGED_e__Struct
            {
                [NativeTypeName("const QUIC_ADDR *")]
                public QuicAddr* Address;
            }

            internal unsafe partial struct _PEER_ADDRESS_CHANGED_e__Struct
            {
                [NativeTypeName("const QUIC_ADDR *")]
                public QuicAddr* Address;
            }

            internal unsafe partial struct _PEER_STREAM_STARTED_e__Struct
            {
                [NativeTypeName("HQUIC")]
                public QUIC_HANDLE* Stream;

                public QUIC_STREAM_OPEN_FLAGS Flags;
            }

            internal partial struct _STREAMS_AVAILABLE_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort BidirectionalCount;

                [NativeTypeName("uint16_t")]
                public ushort UnidirectionalCount;
            }

            internal partial struct _IDEAL_PROCESSOR_CHANGED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort IdealProcessor;
            }

            internal partial struct _DATAGRAM_STATE_CHANGED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte SendEnabled;

                [NativeTypeName("uint16_t")]
                public ushort MaxSendLength;
            }

            internal unsafe partial struct _DATAGRAM_RECEIVED_e__Struct
            {
                [NativeTypeName("const QUIC_BUFFER *")]
                public QUIC_BUFFER* Buffer;

                public QUIC_RECEIVE_FLAGS Flags;
            }

            internal unsafe partial struct _DATAGRAM_SEND_STATE_CHANGED_e__Struct
            {
                public void* ClientContext;

                public QUIC_DATAGRAM_SEND_STATE State;
            }

            internal unsafe partial struct _RESUMED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                public ushort ResumptionStateLength;

                [NativeTypeName("const uint8_t *")]
                public byte* ResumptionState;
            }

            internal unsafe partial struct _RESUMPTION_TICKET_RECEIVED_e__Struct
            {
                [NativeTypeName("uint32_t")]
                public uint ResumptionTicketLength;

                [NativeTypeName("const uint8_t *")]
                public byte* ResumptionTicket;
            }

            internal unsafe partial struct _PEER_CERTIFICATE_RECEIVED_e__Struct
            {
                [NativeTypeName("QUIC_CERTIFICATE *")]
                public void* Certificate;

                [NativeTypeName("uint32_t")]
                public uint DeferredErrorFlags;

                [NativeTypeName("HRESULT")]
                public int DeferredStatus;

                [NativeTypeName("QUIC_CERTIFICATE_CHAIN *")]
                public void* Chain;
            }
        }
    }

    internal enum QUIC_STREAM_EVENT_TYPE
    {
        QUIC_STREAM_EVENT_START_COMPLETE = 0,
        QUIC_STREAM_EVENT_RECEIVE = 1,
        QUIC_STREAM_EVENT_SEND_COMPLETE = 2,
        QUIC_STREAM_EVENT_PEER_SEND_SHUTDOWN = 3,
        QUIC_STREAM_EVENT_PEER_SEND_ABORTED = 4,
        QUIC_STREAM_EVENT_PEER_RECEIVE_ABORTED = 5,
        QUIC_STREAM_EVENT_SEND_SHUTDOWN_COMPLETE = 6,
        QUIC_STREAM_EVENT_SHUTDOWN_COMPLETE = 7,
        QUIC_STREAM_EVENT_IDEAL_SEND_BUFFER_SIZE = 8,
        QUIC_STREAM_EVENT_PEER_ACCEPTED = 9,
    }

    internal partial struct QUIC_STREAM_EVENT
    {
        public QUIC_STREAM_EVENT_TYPE Type;

        [NativeTypeName("QUIC_STREAM_EVENT::(anonymous union)")]
        public _Anonymous_e__Union Anonymous;

        public ref _Anonymous_e__Union._START_COMPLETE_e__Struct START_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.START_COMPLETE, 1));
            }
        }

        public ref _Anonymous_e__Union._RECEIVE_e__Struct RECEIVE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RECEIVE, 1));
            }
        }

        public ref _Anonymous_e__Union._SEND_COMPLETE_e__Struct SEND_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SEND_COMPLETE, 1));
            }
        }

        public ref _Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_SEND_ABORTED, 1));
            }
        }

        public ref _Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_RECEIVE_ABORTED, 1));
            }
        }

        public ref _Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SEND_SHUTDOWN_COMPLETE, 1));
            }
        }

        public ref _Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_COMPLETE, 1));
            }
        }

        public ref _Anonymous_e__Union._IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IDEAL_SEND_BUFFER_SIZE, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _START_COMPLETE_e__Struct START_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _RECEIVE_e__Struct RECEIVE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SEND_COMPLETE_e__Struct SEND_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            public _IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE;

            internal partial struct _START_COMPLETE_e__Struct
            {
                [NativeTypeName("HRESULT")]
                public int Status;

                [NativeTypeName("QUIC_UINT62")]
                public ulong ID;

                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte PeerAccepted
                {
                    get
                    {
                        return (byte)(_bitfield & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                    }
                }

                [NativeTypeName("BOOLEAN : 7")]
                public byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x7Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x7Fu << 1)) | ((value & 0x7Fu) << 1));
                    }
                }
            }

            internal unsafe partial struct _RECEIVE_e__Struct
            {
                [NativeTypeName("uint64_t")]
                public ulong AbsoluteOffset;

                [NativeTypeName("uint64_t")]
                public ulong TotalBufferLength;

                [NativeTypeName("const QUIC_BUFFER *")]
                public QUIC_BUFFER* Buffers;

                [NativeTypeName("uint32_t")]
                public uint BufferCount;

                public QUIC_RECEIVE_FLAGS Flags;
            }

            internal unsafe partial struct _SEND_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte Canceled;

                public void* ClientContext;
            }

            internal partial struct _PEER_SEND_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            internal partial struct _PEER_RECEIVE_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                public ulong ErrorCode;
            }

            internal partial struct _SEND_SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte Graceful;
            }

            internal partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                public byte ConnectionShutdown;

                public byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                public byte AppCloseInProgress
                {
                    get
                    {
                        return (byte)(_bitfield & 0x1u);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~0x1u) | (value & 0x1u));
                    }
                }

                [NativeTypeName("BOOLEAN : 7")]
                public byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 1) & 0x7Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x7Fu << 1)) | ((value & 0x7Fu) << 1));
                    }
                }
            }

            internal partial struct _IDEAL_SEND_BUFFER_SIZE_e__Struct
            {
                [NativeTypeName("uint64_t")]
                public ulong ByteCount;
            }
        }
    }

    internal unsafe partial struct QUIC_API_TABLE
    {
        [NativeTypeName("QUIC_SET_CONTEXT_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, void> SetContext;

        [NativeTypeName("QUIC_GET_CONTEXT_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*> GetContext;

        [NativeTypeName("QUIC_SET_CALLBACK_HANDLER_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, void*, void> SetCallbackHandler;

        [NativeTypeName("QUIC_SET_PARAM_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, uint, uint, void*, int> SetParam;

        [NativeTypeName("QUIC_GET_PARAM_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, uint, uint*, void*, int> GetParam;

        [NativeTypeName("QUIC_REGISTRATION_OPEN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_REGISTRATION_CONFIG*, QUIC_HANDLE**, int> RegistrationOpen;

        [NativeTypeName("QUIC_REGISTRATION_CLOSE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> RegistrationClose;

        [NativeTypeName("QUIC_REGISTRATION_SHUTDOWN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CONNECTION_SHUTDOWN_FLAGS, ulong, void> RegistrationShutdown;

        [NativeTypeName("QUIC_CONFIGURATION_OPEN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SETTINGS*, uint, void*, QUIC_HANDLE**, int> ConfigurationOpen;

        [NativeTypeName("QUIC_CONFIGURATION_CLOSE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ConfigurationClose;

        [NativeTypeName("QUIC_CONFIGURATION_LOAD_CREDENTIAL_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CREDENTIAL_CONFIG*, int> ConfigurationLoadCredential;

        [NativeTypeName("QUIC_LISTENER_OPEN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int>, void*, QUIC_HANDLE**, int> ListenerOpen;

        [NativeTypeName("QUIC_LISTENER_CLOSE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ListenerClose;

        [NativeTypeName("QUIC_LISTENER_START_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QuicAddr*, int> ListenerStart;

        [NativeTypeName("QUIC_LISTENER_STOP_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ListenerStop;

        [NativeTypeName("QUIC_CONNECTION_OPEN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>, void*, QUIC_HANDLE**, int> ConnectionOpen;

        [NativeTypeName("QUIC_CONNECTION_CLOSE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ConnectionClose;

        [NativeTypeName("QUIC_CONNECTION_SHUTDOWN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CONNECTION_SHUTDOWN_FLAGS, ulong, void> ConnectionShutdown;

        [NativeTypeName("QUIC_CONNECTION_START_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_HANDLE*, ushort, sbyte*, ushort, int> ConnectionStart;

        [NativeTypeName("QUIC_CONNECTION_SET_CONFIGURATION_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_HANDLE*, int> ConnectionSetConfiguration;

        [NativeTypeName("QUIC_CONNECTION_SEND_RESUMPTION_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_SEND_RESUMPTION_FLAGS, ushort, byte*, int> ConnectionSendResumptionTicket;

        [NativeTypeName("QUIC_STREAM_OPEN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_OPEN_FLAGS, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>, void*, QUIC_HANDLE**, int> StreamOpen;

        [NativeTypeName("QUIC_STREAM_CLOSE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> StreamClose;

        [NativeTypeName("QUIC_STREAM_START_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_START_FLAGS, int> StreamStart;

        [NativeTypeName("QUIC_STREAM_SHUTDOWN_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_SHUTDOWN_FLAGS, ulong, int> StreamShutdown;

        [NativeTypeName("QUIC_STREAM_SEND_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SEND_FLAGS, void*, int> StreamSend;

        [NativeTypeName("QUIC_STREAM_RECEIVE_COMPLETE_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, ulong, void> StreamReceiveComplete;

        [NativeTypeName("QUIC_STREAM_RECEIVE_SET_ENABLED_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, byte, int> StreamReceiveSetEnabled;

        [NativeTypeName("QUIC_DATAGRAM_SEND_FN")]
        public delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SEND_FLAGS, void*, int> DatagramSend;
    }

    internal static unsafe partial class MsQuic
    {
        [DllImport("msquic", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int MsQuicOpenVersion([NativeTypeName("uint32_t")] uint Version, [NativeTypeName("const void **")] void** QuicApi);

        [DllImport("msquic", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void MsQuicClose([NativeTypeName("const void *")] void* QuicApi);

        [NativeTypeName("#define QUIC_MAX_ALPN_LENGTH 255")]
        public const int QUIC_MAX_ALPN_LENGTH = 255;

        [NativeTypeName("#define QUIC_MAX_SNI_LENGTH 65535")]
        public const int QUIC_MAX_SNI_LENGTH = 65535;

        [NativeTypeName("#define QUIC_MAX_RESUMPTION_APP_DATA_LENGTH 1000")]
        public const int QUIC_MAX_RESUMPTION_APP_DATA_LENGTH = 1000;

        [NativeTypeName("#define QUIC_MAX_TICKET_KEY_COUNT 16")]
        public const int QUIC_MAX_TICKET_KEY_COUNT = 16;

        [NativeTypeName("#define QUIC_TLS_SECRETS_MAX_SECRET_LEN 64")]
        public const int QUIC_TLS_SECRETS_MAX_SECRET_LEN = 64;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_GLOBAL 0x01000000")]
        public const int QUIC_PARAM_PREFIX_GLOBAL = 0x01000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_REGISTRATION 0x02000000")]
        public const int QUIC_PARAM_PREFIX_REGISTRATION = 0x02000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_CONFIGURATION 0x03000000")]
        public const int QUIC_PARAM_PREFIX_CONFIGURATION = 0x03000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_LISTENER 0x04000000")]
        public const int QUIC_PARAM_PREFIX_LISTENER = 0x04000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_CONNECTION 0x05000000")]
        public const int QUIC_PARAM_PREFIX_CONNECTION = 0x05000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS 0x06000000")]
        public const int QUIC_PARAM_PREFIX_TLS = 0x06000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS_SCHANNEL 0x07000000")]
        public const int QUIC_PARAM_PREFIX_TLS_SCHANNEL = 0x07000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_STREAM 0x08000000")]
        public const int QUIC_PARAM_PREFIX_STREAM = 0x08000000;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT 0x01000000")]
        public const int QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT = 0x01000000;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS 0x01000001")]
        public const int QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS = 0x01000001;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE 0x01000002")]
        public const int QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE = 0x01000002;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_PERF_COUNTERS 0x01000003")]
        public const int QUIC_PARAM_GLOBAL_PERF_COUNTERS = 0x01000003;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_VERSION 0x01000004")]
        public const int QUIC_PARAM_GLOBAL_LIBRARY_VERSION = 0x01000004;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_SETTINGS 0x01000005")]
        public const int QUIC_PARAM_GLOBAL_SETTINGS = 0x01000005;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS 0x01000006")]
        public const int QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS = 0x01000006;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_VERSION_SETTINGS 0x01000007")]
        public const int QUIC_PARAM_GLOBAL_VERSION_SETTINGS = 0x01000007;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH 0x01000008")]
        public const int QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH = 0x01000008;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_DATAPATH_PROCESSORS 0x01000009")]
        public const int QUIC_PARAM_GLOBAL_DATAPATH_PROCESSORS = 0x01000009;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_SETTINGS 0x03000000")]
        public const int QUIC_PARAM_CONFIGURATION_SETTINGS = 0x03000000;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_TICKET_KEYS 0x03000001")]
        public const int QUIC_PARAM_CONFIGURATION_TICKET_KEYS = 0x03000001;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS 0x03000002")]
        public const int QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS = 0x03000002;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_LOCAL_ADDRESS 0x04000000")]
        public const int QUIC_PARAM_LISTENER_LOCAL_ADDRESS = 0x04000000;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_STATS 0x04000001")]
        public const int QUIC_PARAM_LISTENER_STATS = 0x04000001;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_CIBIR_ID 0x04000002")]
        public const int QUIC_PARAM_LISTENER_CIBIR_ID = 0x04000002;

        [NativeTypeName("#define QUIC_PARAM_CONN_QUIC_VERSION 0x05000000")]
        public const int QUIC_PARAM_CONN_QUIC_VERSION = 0x05000000;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_ADDRESS 0x05000001")]
        public const int QUIC_PARAM_CONN_LOCAL_ADDRESS = 0x05000001;

        [NativeTypeName("#define QUIC_PARAM_CONN_REMOTE_ADDRESS 0x05000002")]
        public const int QUIC_PARAM_CONN_REMOTE_ADDRESS = 0x05000002;

        [NativeTypeName("#define QUIC_PARAM_CONN_IDEAL_PROCESSOR 0x05000003")]
        public const int QUIC_PARAM_CONN_IDEAL_PROCESSOR = 0x05000003;

        [NativeTypeName("#define QUIC_PARAM_CONN_SETTINGS 0x05000004")]
        public const int QUIC_PARAM_CONN_SETTINGS = 0x05000004;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS 0x05000005")]
        public const int QUIC_PARAM_CONN_STATISTICS = 0x05000005;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_PLAT 0x05000006")]
        public const int QUIC_PARAM_CONN_STATISTICS_PLAT = 0x05000006;

        [NativeTypeName("#define QUIC_PARAM_CONN_SHARE_UDP_BINDING 0x05000007")]
        public const int QUIC_PARAM_CONN_SHARE_UDP_BINDING = 0x05000007;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT 0x05000008")]
        public const int QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT = 0x05000008;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT 0x05000009")]
        public const int QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT = 0x05000009;

        [NativeTypeName("#define QUIC_PARAM_CONN_MAX_STREAM_IDS 0x0500000A")]
        public const int QUIC_PARAM_CONN_MAX_STREAM_IDS = 0x0500000A;

        [NativeTypeName("#define QUIC_PARAM_CONN_CLOSE_REASON_PHRASE 0x0500000B")]
        public const int QUIC_PARAM_CONN_CLOSE_REASON_PHRASE = 0x0500000B;

        [NativeTypeName("#define QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME 0x0500000C")]
        public const int QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME = 0x0500000C;

        [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED 0x0500000D")]
        public const int QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED = 0x0500000D;

        [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED 0x0500000E")]
        public const int QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED = 0x0500000E;

        [NativeTypeName("#define QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION 0x0500000F")]
        public const int QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION = 0x0500000F;

        [NativeTypeName("#define QUIC_PARAM_CONN_RESUMPTION_TICKET 0x05000010")]
        public const int QUIC_PARAM_CONN_RESUMPTION_TICKET = 0x05000010;

        [NativeTypeName("#define QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID 0x05000011")]
        public const int QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID = 0x05000011;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_INTERFACE 0x05000012")]
        public const int QUIC_PARAM_CONN_LOCAL_INTERFACE = 0x05000012;

        [NativeTypeName("#define QUIC_PARAM_CONN_TLS_SECRETS 0x05000013")]
        public const int QUIC_PARAM_CONN_TLS_SECRETS = 0x05000013;

        [NativeTypeName("#define QUIC_PARAM_CONN_VERSION_SETTINGS 0x05000014")]
        public const int QUIC_PARAM_CONN_VERSION_SETTINGS = 0x05000014;

        [NativeTypeName("#define QUIC_PARAM_CONN_CIBIR_ID 0x05000015")]
        public const int QUIC_PARAM_CONN_CIBIR_ID = 0x05000015;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2 0x05000016")]
        public const int QUIC_PARAM_CONN_STATISTICS_V2 = 0x05000016;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2_PLAT 0x05000017")]
        public const int QUIC_PARAM_CONN_STATISTICS_V2_PLAT = 0x05000017;

        [NativeTypeName("#define QUIC_PARAM_TLS_HANDSHAKE_INFO 0x06000000")]
        public const int QUIC_PARAM_TLS_HANDSHAKE_INFO = 0x06000000;

        [NativeTypeName("#define QUIC_PARAM_TLS_NEGOTIATED_ALPN 0x06000001")]
        public const int QUIC_PARAM_TLS_NEGOTIATED_ALPN = 0x06000001;

        [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W 0x07000000")]
        public const int QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W = 0x07000000;

        [NativeTypeName("#define QUIC_PARAM_STREAM_ID 0x08000000")]
        public const int QUIC_PARAM_STREAM_ID = 0x08000000;

        [NativeTypeName("#define QUIC_PARAM_STREAM_0RTT_LENGTH 0x08000001")]
        public const int QUIC_PARAM_STREAM_0RTT_LENGTH = 0x08000001;

        [NativeTypeName("#define QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE 0x08000002")]
        public const int QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE = 0x08000002;

        [NativeTypeName("#define QUIC_PARAM_STREAM_PRIORITY 0x08000003")]
        public const int QUIC_PARAM_STREAM_PRIORITY = 0x08000003;

        [NativeTypeName("#define QUIC_API_VERSION_2 2")]
        public const int QUIC_API_VERSION_2 = 2;
    }
}
