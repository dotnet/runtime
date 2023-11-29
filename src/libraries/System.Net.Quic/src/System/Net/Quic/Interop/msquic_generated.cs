#pragma warning disable IDE0073
//
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
//
#pragma warning restore IDE0073

#pragma warning disable CS0649

// Polyfill for MemoryMarshal on .NET Standard
#if NETSTANDARD && !NETSTANDARD2_1_OR_GREATER
using MemoryMarshal = Microsoft.Quic.Polyfill.MemoryMarshal;
#else
using MemoryMarshal = System.Runtime.InteropServices.MemoryMarshal;
#endif

using System.Runtime.InteropServices;

namespace Microsoft.Quic
{
    internal partial struct QUIC_HANDLE
    {
    }

    internal enum QUIC_TLS_PROVIDER
    {
        SCHANNEL = 0x0000,
        OPENSSL = 0x0001,
    }

    internal enum QUIC_EXECUTION_PROFILE
    {
        LOW_LATENCY,
        MAX_THROUGHPUT,
        SCAVENGER,
        REAL_TIME,
    }

    internal enum QUIC_LOAD_BALANCING_MODE
    {
        DISABLED,
        SERVER_ID_IP,
        SERVER_ID_FIXED,
        COUNT,
    }

    internal enum QUIC_TLS_ALERT_CODES
    {
        SUCCESS = 0xFFFF,
        UNEXPECTED_MESSAGE = 10,
        BAD_CERTIFICATE = 42,
        UNSUPPORTED_CERTIFICATE = 43,
        CERTIFICATE_REVOKED = 44,
        CERTIFICATE_EXPIRED = 45,
        CERTIFICATE_UNKNOWN = 46,
        ILLEGAL_PARAMETER = 47,
        UNKNOWN_CA = 48,
        ACCESS_DENIED = 49,
        INSUFFICIENT_SECURITY = 71,
        INTERNAL_ERROR = 80,
        USER_CANCELED = 90,
        CERTIFICATE_REQUIRED = 116,
        MAX = 255,
    }

    internal enum QUIC_CREDENTIAL_TYPE
    {
        NONE,
        CERTIFICATE_HASH,
        CERTIFICATE_HASH_STORE,
        CERTIFICATE_CONTEXT,
        CERTIFICATE_FILE,
        CERTIFICATE_FILE_PROTECTED,
        CERTIFICATE_PKCS12,
    }

    [System.Flags]
    internal enum QUIC_CREDENTIAL_FLAGS
    {
        NONE = 0x00000000,
        CLIENT = 0x00000001,
        LOAD_ASYNCHRONOUS = 0x00000002,
        NO_CERTIFICATE_VALIDATION = 0x00000004,
        ENABLE_OCSP = 0x00000008,
        INDICATE_CERTIFICATE_RECEIVED = 0x00000010,
        DEFER_CERTIFICATE_VALIDATION = 0x00000020,
        REQUIRE_CLIENT_AUTHENTICATION = 0x00000040,
        USE_TLS_BUILTIN_CERTIFICATE_VALIDATION = 0x00000080,
        REVOCATION_CHECK_END_CERT = 0x00000100,
        REVOCATION_CHECK_CHAIN = 0x00000200,
        REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000400,
        IGNORE_NO_REVOCATION_CHECK = 0x00000800,
        IGNORE_REVOCATION_OFFLINE = 0x00001000,
        SET_ALLOWED_CIPHER_SUITES = 0x00002000,
        USE_PORTABLE_CERTIFICATES = 0x00004000,
        USE_SUPPLIED_CREDENTIALS = 0x00008000,
        USE_SYSTEM_MAPPER = 0x00010000,
        CACHE_ONLY_URL_RETRIEVAL = 0x00020000,
        REVOCATION_CHECK_CACHE_ONLY = 0x00040000,
        INPROC_PEER_CERTIFICATE = 0x00080000,
        SET_CA_CERTIFICATE_FILE = 0x00100000,
    }

    [System.Flags]
    internal enum QUIC_ALLOWED_CIPHER_SUITE_FLAGS
    {
        NONE = 0x0,
        AES_128_GCM_SHA256 = 0x1,
        AES_256_GCM_SHA384 = 0x2,
        CHACHA20_POLY1305_SHA256 = 0x4,
    }

    [System.Flags]
    internal enum QUIC_CERTIFICATE_HASH_STORE_FLAGS
    {
        NONE = 0x0000,
        MACHINE_STORE = 0x0001,
    }

    [System.Flags]
    internal enum QUIC_CONNECTION_SHUTDOWN_FLAGS
    {
        NONE = 0x0000,
        SILENT = 0x0001,
    }

    internal enum QUIC_SERVER_RESUMPTION_LEVEL
    {
        NO_RESUME,
        RESUME_ONLY,
        RESUME_AND_ZERORTT,
    }

    [System.Flags]
    internal enum QUIC_SEND_RESUMPTION_FLAGS
    {
        NONE = 0x0000,
        FINAL = 0x0001,
    }

    internal enum QUIC_STREAM_SCHEDULING_SCHEME
    {
        FIFO = 0x0000,
        ROUND_ROBIN = 0x0001,
        COUNT,
    }

    [System.Flags]
    internal enum QUIC_STREAM_OPEN_FLAGS
    {
        NONE = 0x0000,
        UNIDIRECTIONAL = 0x0001,
        ZERO_RTT = 0x0002,
        DELAY_ID_FC_UPDATES = 0x0004,
    }

    [System.Flags]
    internal enum QUIC_STREAM_START_FLAGS
    {
        NONE = 0x0000,
        IMMEDIATE = 0x0001,
        FAIL_BLOCKED = 0x0002,
        SHUTDOWN_ON_FAIL = 0x0004,
        INDICATE_PEER_ACCEPT = 0x0008,
    }

    [System.Flags]
    internal enum QUIC_STREAM_SHUTDOWN_FLAGS
    {
        NONE = 0x0000,
        GRACEFUL = 0x0001,
        ABORT_SEND = 0x0002,
        ABORT_RECEIVE = 0x0004,
        ABORT = 0x0006,
        IMMEDIATE = 0x0008,
        INLINE = 0x0010,
    }

    [System.Flags]
    internal enum QUIC_RECEIVE_FLAGS
    {
        NONE = 0x0000,
        ZERO_RTT = 0x0001,
        FIN = 0x0002,
    }

    [System.Flags]
    internal enum QUIC_SEND_FLAGS
    {
        NONE = 0x0000,
        ALLOW_0_RTT = 0x0001,
        START = 0x0002,
        FIN = 0x0004,
        DGRAM_PRIORITY = 0x0008,
        DELAY_SEND = 0x0010,
    }

    internal enum QUIC_DATAGRAM_SEND_STATE
    {
        UNKNOWN,
        SENT,
        LOST_SUSPECT,
        LOST_DISCARDED,
        ACKNOWLEDGED,
        ACKNOWLEDGED_SPURIOUS,
        CANCELED,
    }

    [System.Flags]
    internal enum QUIC_EXECUTION_CONFIG_FLAGS
    {
        NONE = 0x0000,
        QTIP = 0x0001,
        RIO = 0x0002,
    }

    internal unsafe partial struct QUIC_EXECUTION_CONFIG
    {
        internal QUIC_EXECUTION_CONFIG_FLAGS Flags;

        [NativeTypeName("uint32_t")]
        internal uint PollingIdleTimeoutUs;

        [NativeTypeName("uint32_t")]
        internal uint ProcessorCount;

        [NativeTypeName("uint16_t [1]")]
        internal fixed ushort ProcessorList[1];
    }

    internal unsafe partial struct QUIC_REGISTRATION_CONFIG
    {
        [NativeTypeName("const char *")]
        internal sbyte* AppName;

        internal QUIC_EXECUTION_PROFILE ExecutionProfile;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_HASH
    {
        [NativeTypeName("uint8_t [20]")]
        internal fixed byte ShaHash[20];
    }

    internal unsafe partial struct QUIC_CERTIFICATE_HASH_STORE
    {
        internal QUIC_CERTIFICATE_HASH_STORE_FLAGS Flags;

        [NativeTypeName("uint8_t [20]")]
        internal fixed byte ShaHash[20];

        [NativeTypeName("char [128]")]
        internal fixed sbyte StoreName[128];
    }

    internal unsafe partial struct QUIC_CERTIFICATE_FILE
    {
        [NativeTypeName("const char *")]
        internal sbyte* PrivateKeyFile;

        [NativeTypeName("const char *")]
        internal sbyte* CertificateFile;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_FILE_PROTECTED
    {
        [NativeTypeName("const char *")]
        internal sbyte* PrivateKeyFile;

        [NativeTypeName("const char *")]
        internal sbyte* CertificateFile;

        [NativeTypeName("const char *")]
        internal sbyte* PrivateKeyPassword;
    }

    internal unsafe partial struct QUIC_CERTIFICATE_PKCS12
    {
        [NativeTypeName("const uint8_t *")]
        internal byte* Asn1Blob;

        [NativeTypeName("uint32_t")]
        internal uint Asn1BlobLength;

        [NativeTypeName("const char *")]
        internal sbyte* PrivateKeyPassword;
    }

    internal unsafe partial struct QUIC_CREDENTIAL_CONFIG
    {
        internal QUIC_CREDENTIAL_TYPE Type;

        internal QUIC_CREDENTIAL_FLAGS Flags;

        [NativeTypeName("QUIC_CREDENTIAL_CONFIG::(anonymous union)")]
        internal _Anonymous_e__Union Anonymous;

        [NativeTypeName("const char *")]
        internal sbyte* Principal;

        internal void* Reserved;

        [NativeTypeName("QUIC_CREDENTIAL_LOAD_COMPLETE_HANDLER")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, int, void> AsyncHandler;

        internal QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

        [NativeTypeName("const char *")]
        internal sbyte* CaCertificateFile;

        internal ref QUIC_CERTIFICATE_HASH* CertificateHash
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateHash;
            }
        }

        internal ref QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateHashStore;
            }
        }

        internal ref void* CertificateContext
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateContext;
            }
        }

        internal ref QUIC_CERTIFICATE_FILE* CertificateFile
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateFile;
            }
        }

        internal ref QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref this, 1)).Anonymous.CertificateFileProtected;
            }
        }

        internal ref QUIC_CERTIFICATE_PKCS12* CertificatePkcs12
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
            internal QUIC_CERTIFICATE_HASH* CertificateHash;

            [FieldOffset(0)]
            internal QUIC_CERTIFICATE_HASH_STORE* CertificateHashStore;

            [FieldOffset(0)]
            [NativeTypeName("QUIC_CERTIFICATE *")]
            internal void* CertificateContext;

            [FieldOffset(0)]
            internal QUIC_CERTIFICATE_FILE* CertificateFile;

            [FieldOffset(0)]
            internal QUIC_CERTIFICATE_FILE_PROTECTED* CertificateFileProtected;

            [FieldOffset(0)]
            internal QUIC_CERTIFICATE_PKCS12* CertificatePkcs12;
        }
    }

    internal unsafe partial struct QUIC_TICKET_KEY_CONFIG
    {
        [NativeTypeName("uint8_t [16]")]
        internal fixed byte Id[16];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte Material[64];

        [NativeTypeName("uint8_t")]
        internal byte MaterialLength;
    }

    internal unsafe partial struct QUIC_BUFFER
    {
        [NativeTypeName("uint32_t")]
        internal uint Length;

        [NativeTypeName("uint8_t *")]
        internal byte* Buffer;
    }

    internal unsafe partial struct QUIC_NEW_CONNECTION_INFO
    {
        [NativeTypeName("uint32_t")]
        internal uint QuicVersion;

        [NativeTypeName("const QUIC_ADDR *")]
        internal QuicAddr* LocalAddress;

        [NativeTypeName("const QUIC_ADDR *")]
        internal QuicAddr* RemoteAddress;

        [NativeTypeName("uint32_t")]
        internal uint CryptoBufferLength;

        [NativeTypeName("uint16_t")]
        internal ushort ClientAlpnListLength;

        [NativeTypeName("uint16_t")]
        internal ushort ServerNameLength;

        [NativeTypeName("uint8_t")]
        internal byte NegotiatedAlpnLength;

        [NativeTypeName("const uint8_t *")]
        internal byte* CryptoBuffer;

        [NativeTypeName("const uint8_t *")]
        internal byte* ClientAlpnList;

        [NativeTypeName("const uint8_t *")]
        internal byte* NegotiatedAlpn;

        [NativeTypeName("const char *")]
        internal sbyte* ServerName;
    }

    internal enum QUIC_TLS_PROTOCOL_VERSION
    {
        UNKNOWN = 0,
        TLS_1_3 = 0x3000,
    }

    internal enum QUIC_CIPHER_ALGORITHM
    {
        NONE = 0,
        AES_128 = 0x660E,
        AES_256 = 0x6610,
        CHACHA20 = 0x6612,
    }

    internal enum QUIC_HASH_ALGORITHM
    {
        NONE = 0,
        SHA_256 = 0x800C,
        SHA_384 = 0x800D,
    }

    internal enum QUIC_KEY_EXCHANGE_ALGORITHM
    {
        NONE = 0,
    }

    internal enum QUIC_CIPHER_SUITE
    {
        TLS_AES_128_GCM_SHA256 = 0x1301,
        TLS_AES_256_GCM_SHA384 = 0x1302,
        TLS_CHACHA20_POLY1305_SHA256 = 0x1303,
    }

    internal enum QUIC_CONGESTION_CONTROL_ALGORITHM
    {
        CUBIC,
        BBR,
        MAX,
    }

    internal partial struct QUIC_HANDSHAKE_INFO
    {
        internal QUIC_TLS_PROTOCOL_VERSION TlsProtocolVersion;

        internal QUIC_CIPHER_ALGORITHM CipherAlgorithm;

        [NativeTypeName("int32_t")]
        internal int CipherStrength;

        internal QUIC_HASH_ALGORITHM Hash;

        [NativeTypeName("int32_t")]
        internal int HashStrength;

        internal QUIC_KEY_EXCHANGE_ALGORITHM KeyExchangeAlgorithm;

        [NativeTypeName("int32_t")]
        internal int KeyExchangeStrength;

        internal QUIC_CIPHER_SUITE CipherSuite;
    }

    internal partial struct QUIC_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        internal ulong CorrelationId;

        internal uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        internal uint VersionNegotiation
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
        internal uint StatelessRetry
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
        internal uint ResumptionAttempted
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
        internal uint ResumptionSucceeded
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
        internal uint Rtt;

        [NativeTypeName("uint32_t")]
        internal uint MinRtt;

        [NativeTypeName("uint32_t")]
        internal uint MaxRtt;

        [NativeTypeName("struct (anonymous struct)")]
        internal _Timing_e__Struct Timing;

        [NativeTypeName("struct (anonymous struct)")]
        internal _Handshake_e__Struct Handshake;

        [NativeTypeName("struct (anonymous struct)")]
        internal _Send_e__Struct Send;

        [NativeTypeName("struct (anonymous struct)")]
        internal _Recv_e__Struct Recv;

        [NativeTypeName("struct (anonymous struct)")]
        internal _Misc_e__Struct Misc;

        internal partial struct _Timing_e__Struct
        {
            [NativeTypeName("uint64_t")]
            internal ulong Start;

            [NativeTypeName("uint64_t")]
            internal ulong InitialFlightEnd;

            [NativeTypeName("uint64_t")]
            internal ulong HandshakeFlightEnd;
        }

        internal partial struct _Handshake_e__Struct
        {
            [NativeTypeName("uint32_t")]
            internal uint ClientFlight1Bytes;

            [NativeTypeName("uint32_t")]
            internal uint ServerFlight1Bytes;

            [NativeTypeName("uint32_t")]
            internal uint ClientFlight2Bytes;
        }

        internal partial struct _Send_e__Struct
        {
            [NativeTypeName("uint16_t")]
            internal ushort PathMtu;

            [NativeTypeName("uint64_t")]
            internal ulong TotalPackets;

            [NativeTypeName("uint64_t")]
            internal ulong RetransmittablePackets;

            [NativeTypeName("uint64_t")]
            internal ulong SuspectedLostPackets;

            [NativeTypeName("uint64_t")]
            internal ulong SpuriousLostPackets;

            [NativeTypeName("uint64_t")]
            internal ulong TotalBytes;

            [NativeTypeName("uint64_t")]
            internal ulong TotalStreamBytes;

            [NativeTypeName("uint32_t")]
            internal uint CongestionCount;

            [NativeTypeName("uint32_t")]
            internal uint PersistentCongestionCount;
        }

        internal partial struct _Recv_e__Struct
        {
            [NativeTypeName("uint64_t")]
            internal ulong TotalPackets;

            [NativeTypeName("uint64_t")]
            internal ulong ReorderedPackets;

            [NativeTypeName("uint64_t")]
            internal ulong DroppedPackets;

            [NativeTypeName("uint64_t")]
            internal ulong DuplicatePackets;

            [NativeTypeName("uint64_t")]
            internal ulong TotalBytes;

            [NativeTypeName("uint64_t")]
            internal ulong TotalStreamBytes;

            [NativeTypeName("uint64_t")]
            internal ulong DecryptionFailures;

            [NativeTypeName("uint64_t")]
            internal ulong ValidAckFrames;
        }

        internal partial struct _Misc_e__Struct
        {
            [NativeTypeName("uint32_t")]
            internal uint KeyUpdateCount;
        }
    }

    internal partial struct QUIC_STATISTICS_V2
    {
        [NativeTypeName("uint64_t")]
        internal ulong CorrelationId;

        internal uint _bitfield;

        [NativeTypeName("uint32_t : 1")]
        internal uint VersionNegotiation
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
        internal uint StatelessRetry
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
        internal uint ResumptionAttempted
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
        internal uint ResumptionSucceeded
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

        [NativeTypeName("uint32_t : 1")]
        internal uint GreaseBitNegotiated
        {
            get
            {
                return (_bitfield >> 4) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 4)) | ((value & 0x1u) << 4);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        internal uint EcnCapable
        {
            get
            {
                return (_bitfield >> 5) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 5)) | ((value & 0x1u) << 5);
            }
        }

        [NativeTypeName("uint32_t : 1")]
        internal uint EncryptionOffloaded
        {
            get
            {
                return (_bitfield >> 6) & 0x1u;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6);
            }
        }

        [NativeTypeName("uint32_t : 25")]
        internal uint RESERVED
        {
            get
            {
                return (_bitfield >> 7) & 0x1FFFFFFu;
            }

            set
            {
                _bitfield = (_bitfield & ~(0x1FFFFFFu << 7)) | ((value & 0x1FFFFFFu) << 7);
            }
        }

        [NativeTypeName("uint32_t")]
        internal uint Rtt;

        [NativeTypeName("uint32_t")]
        internal uint MinRtt;

        [NativeTypeName("uint32_t")]
        internal uint MaxRtt;

        [NativeTypeName("uint64_t")]
        internal ulong TimingStart;

        [NativeTypeName("uint64_t")]
        internal ulong TimingInitialFlightEnd;

        [NativeTypeName("uint64_t")]
        internal ulong TimingHandshakeFlightEnd;

        [NativeTypeName("uint32_t")]
        internal uint HandshakeClientFlight1Bytes;

        [NativeTypeName("uint32_t")]
        internal uint HandshakeServerFlight1Bytes;

        [NativeTypeName("uint32_t")]
        internal uint HandshakeClientFlight2Bytes;

        [NativeTypeName("uint16_t")]
        internal ushort SendPathMtu;

        [NativeTypeName("uint64_t")]
        internal ulong SendTotalPackets;

        [NativeTypeName("uint64_t")]
        internal ulong SendRetransmittablePackets;

        [NativeTypeName("uint64_t")]
        internal ulong SendSuspectedLostPackets;

        [NativeTypeName("uint64_t")]
        internal ulong SendSpuriousLostPackets;

        [NativeTypeName("uint64_t")]
        internal ulong SendTotalBytes;

        [NativeTypeName("uint64_t")]
        internal ulong SendTotalStreamBytes;

        [NativeTypeName("uint32_t")]
        internal uint SendCongestionCount;

        [NativeTypeName("uint32_t")]
        internal uint SendPersistentCongestionCount;

        [NativeTypeName("uint64_t")]
        internal ulong RecvTotalPackets;

        [NativeTypeName("uint64_t")]
        internal ulong RecvReorderedPackets;

        [NativeTypeName("uint64_t")]
        internal ulong RecvDroppedPackets;

        [NativeTypeName("uint64_t")]
        internal ulong RecvDuplicatePackets;

        [NativeTypeName("uint64_t")]
        internal ulong RecvTotalBytes;

        [NativeTypeName("uint64_t")]
        internal ulong RecvTotalStreamBytes;

        [NativeTypeName("uint64_t")]
        internal ulong RecvDecryptionFailures;

        [NativeTypeName("uint64_t")]
        internal ulong RecvValidAckFrames;

        [NativeTypeName("uint32_t")]
        internal uint KeyUpdateCount;

        [NativeTypeName("uint32_t")]
        internal uint SendCongestionWindow;

        [NativeTypeName("uint32_t")]
        internal uint DestCidUpdateCount;

        [NativeTypeName("uint32_t")]
        internal uint SendEcnCongestionCount;
    }

    internal partial struct QUIC_LISTENER_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        internal ulong TotalAcceptedConnections;

        [NativeTypeName("uint64_t")]
        internal ulong TotalRejectedConnections;

        [NativeTypeName("uint64_t")]
        internal ulong BindingRecvDroppedPackets;
    }

    internal enum QUIC_PERFORMANCE_COUNTERS
    {
        CONN_CREATED,
        CONN_HANDSHAKE_FAIL,
        CONN_APP_REJECT,
        CONN_RESUMED,
        CONN_ACTIVE,
        CONN_CONNECTED,
        CONN_PROTOCOL_ERRORS,
        CONN_NO_ALPN,
        STRM_ACTIVE,
        PKTS_SUSPECTED_LOST,
        PKTS_DROPPED,
        PKTS_DECRYPTION_FAIL,
        UDP_RECV,
        UDP_SEND,
        UDP_RECV_BYTES,
        UDP_SEND_BYTES,
        UDP_RECV_EVENTS,
        UDP_SEND_CALLS,
        APP_SEND_BYTES,
        APP_RECV_BYTES,
        CONN_QUEUE_DEPTH,
        CONN_OPER_QUEUE_DEPTH,
        CONN_OPER_QUEUED,
        CONN_OPER_COMPLETED,
        WORK_OPER_QUEUE_DEPTH,
        WORK_OPER_QUEUED,
        WORK_OPER_COMPLETED,
        PATH_VALIDATED,
        PATH_FAILURE,
        SEND_STATELESS_RESET,
        SEND_STATELESS_RETRY,
        MAX,
    }

    internal unsafe partial struct QUIC_VERSION_SETTINGS
    {
        [NativeTypeName("const uint32_t *")]
        internal uint* AcceptableVersions;

        [NativeTypeName("const uint32_t *")]
        internal uint* OfferedVersions;

        [NativeTypeName("const uint32_t *")]
        internal uint* FullyDeployedVersions;

        [NativeTypeName("uint32_t")]
        internal uint AcceptableVersionsLength;

        [NativeTypeName("uint32_t")]
        internal uint OfferedVersionsLength;

        [NativeTypeName("uint32_t")]
        internal uint FullyDeployedVersionsLength;
    }

    internal partial struct QUIC_GLOBAL_SETTINGS
    {
        [NativeTypeName("QUIC_GLOBAL_SETTINGS::(anonymous union)")]
        internal _Anonymous_e__Union Anonymous;

        [NativeTypeName("uint16_t")]
        internal ushort RetryMemoryLimit;

        [NativeTypeName("uint16_t")]
        internal ushort LoadBalancingMode;

        [NativeTypeName("uint32_t")]
        internal uint FixedServerID;

        internal ref ulong IsSetFlags
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IsSetFlags, 1));
            }
        }

        internal ref _Anonymous_e__Union._IsSet_e__Struct IsSet
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
            internal ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _IsSet_e__Struct IsSet;

            internal partial struct _IsSet_e__Struct
            {
                internal ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                internal ulong RetryMemoryLimit
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
                internal ulong LoadBalancingMode
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
                internal ulong FixedServerID
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

                [NativeTypeName("uint64_t : 61")]
                internal ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 3) & 0x1FFFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1FFFFFFFUL << 3)) | ((value & 0x1FFFFFFFUL) << 3);
                    }
                }
            }
        }
    }

    internal partial struct QUIC_SETTINGS
    {
        [NativeTypeName("QUIC_SETTINGS::(anonymous union)")]
        internal _Anonymous1_e__Union Anonymous1;

        [NativeTypeName("uint64_t")]
        internal ulong MaxBytesPerKey;

        [NativeTypeName("uint64_t")]
        internal ulong HandshakeIdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        internal ulong IdleTimeoutMs;

        [NativeTypeName("uint64_t")]
        internal ulong MtuDiscoverySearchCompleteTimeoutUs;

        [NativeTypeName("uint32_t")]
        internal uint TlsClientMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        internal uint TlsServerMaxSendBuffer;

        [NativeTypeName("uint32_t")]
        internal uint StreamRecvWindowDefault;

        [NativeTypeName("uint32_t")]
        internal uint StreamRecvBufferDefault;

        [NativeTypeName("uint32_t")]
        internal uint ConnFlowControlWindow;

        [NativeTypeName("uint32_t")]
        internal uint MaxWorkerQueueDelayUs;

        [NativeTypeName("uint32_t")]
        internal uint MaxStatelessOperations;

        [NativeTypeName("uint32_t")]
        internal uint InitialWindowPackets;

        [NativeTypeName("uint32_t")]
        internal uint SendIdleTimeoutMs;

        [NativeTypeName("uint32_t")]
        internal uint InitialRttMs;

        [NativeTypeName("uint32_t")]
        internal uint MaxAckDelayMs;

        [NativeTypeName("uint32_t")]
        internal uint DisconnectTimeoutMs;

        [NativeTypeName("uint32_t")]
        internal uint KeepAliveIntervalMs;

        [NativeTypeName("uint16_t")]
        internal ushort CongestionControlAlgorithm;

        [NativeTypeName("uint16_t")]
        internal ushort PeerBidiStreamCount;

        [NativeTypeName("uint16_t")]
        internal ushort PeerUnidiStreamCount;

        [NativeTypeName("uint16_t")]
        internal ushort MaxBindingStatelessOperations;

        [NativeTypeName("uint16_t")]
        internal ushort StatelessOperationExpirationMs;

        [NativeTypeName("uint16_t")]
        internal ushort MinimumMtu;

        [NativeTypeName("uint16_t")]
        internal ushort MaximumMtu;

        internal byte _bitfield;

        [NativeTypeName("uint8_t : 1")]
        internal byte SendBufferingEnabled
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
        internal byte PacingEnabled
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
        internal byte MigrationEnabled
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
        internal byte DatagramReceiveEnabled
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
        internal byte ServerResumptionLevel
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

        [NativeTypeName("uint8_t : 1")]
        internal byte GreaseQuicBitEnabled
        {
            get
            {
                return (byte)((_bitfield >> 6) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 6)) | ((value & 0x1u) << 6));
            }
        }

        [NativeTypeName("uint8_t : 1")]
        internal byte EcnEnabled
        {
            get
            {
                return (byte)((_bitfield >> 7) & 0x1u);
            }

            set
            {
                _bitfield = (byte)((_bitfield & ~(0x1u << 7)) | ((value & 0x1u) << 7));
            }
        }

        [NativeTypeName("uint8_t")]
        internal byte MaxOperationsPerDrain;

        [NativeTypeName("uint8_t")]
        internal byte MtuDiscoveryMissingProbeCount;

        [NativeTypeName("uint32_t")]
        internal uint DestCidUpdateIdleTimeoutMs;

        [NativeTypeName("QUIC_SETTINGS::(anonymous union)")]
        internal _Anonymous2_e__Union Anonymous2;

        [NativeTypeName("uint32_t")]
        internal uint StreamRecvWindowBidiLocalDefault;

        [NativeTypeName("uint32_t")]
        internal uint StreamRecvWindowBidiRemoteDefault;

        [NativeTypeName("uint32_t")]
        internal uint StreamRecvWindowUnidiDefault;

        internal ref ulong IsSetFlags
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous1.IsSetFlags, 1));
            }
        }

        internal ref _Anonymous1_e__Union._IsSet_e__Struct IsSet
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous1.IsSet, 1));
            }
        }

        internal ref ulong Flags
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous2.Flags, 1));
            }
        }

        internal ulong HyStartEnabled
        {
            get
            {
                return Anonymous2.Anonymous.HyStartEnabled;
            }

            set
            {
                Anonymous2.Anonymous.HyStartEnabled = value;
            }
        }

        internal ulong EncryptionOffloadAllowed
        {
            get
            {
                return Anonymous2.Anonymous.EncryptionOffloadAllowed;
            }

            set
            {
                Anonymous2.Anonymous.EncryptionOffloadAllowed = value;
            }
        }

        internal ulong ReliableResetEnabled
        {
            get
            {
                return Anonymous2.Anonymous.ReliableResetEnabled;
            }

            set
            {
                Anonymous2.Anonymous.ReliableResetEnabled = value;
            }
        }

        internal ulong OneWayDelayEnabled
        {
            get
            {
                return Anonymous2.Anonymous.OneWayDelayEnabled;
            }

            set
            {
                Anonymous2.Anonymous.OneWayDelayEnabled = value;
            }
        }

        internal ulong ReservedFlags
        {
            get
            {
                return Anonymous2.Anonymous.ReservedFlags;
            }

            set
            {
                Anonymous2.Anonymous.ReservedFlags = value;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous1_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            internal ulong IsSetFlags;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _IsSet_e__Struct IsSet;

            internal partial struct _IsSet_e__Struct
            {
                internal ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                internal ulong MaxBytesPerKey
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
                internal ulong HandshakeIdleTimeoutMs
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
                internal ulong IdleTimeoutMs
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
                internal ulong MtuDiscoverySearchCompleteTimeoutUs
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
                internal ulong TlsClientMaxSendBuffer
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
                internal ulong TlsServerMaxSendBuffer
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
                internal ulong StreamRecvWindowDefault
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
                internal ulong StreamRecvBufferDefault
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
                internal ulong ConnFlowControlWindow
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
                internal ulong MaxWorkerQueueDelayUs
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
                internal ulong MaxStatelessOperations
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
                internal ulong InitialWindowPackets
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
                internal ulong SendIdleTimeoutMs
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
                internal ulong InitialRttMs
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
                internal ulong MaxAckDelayMs
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
                internal ulong DisconnectTimeoutMs
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
                internal ulong KeepAliveIntervalMs
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
                internal ulong CongestionControlAlgorithm
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
                internal ulong PeerBidiStreamCount
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
                internal ulong PeerUnidiStreamCount
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
                internal ulong MaxBindingStatelessOperations
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
                internal ulong StatelessOperationExpirationMs
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
                internal ulong MinimumMtu
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
                internal ulong MaximumMtu
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
                internal ulong SendBufferingEnabled
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
                internal ulong PacingEnabled
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
                internal ulong MigrationEnabled
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
                internal ulong DatagramReceiveEnabled
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
                internal ulong ServerResumptionLevel
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
                internal ulong MaxOperationsPerDrain
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
                internal ulong MtuDiscoveryMissingProbeCount
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

                [NativeTypeName("uint64_t : 1")]
                internal ulong DestCidUpdateIdleTimeoutMs
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

                [NativeTypeName("uint64_t : 1")]
                internal ulong GreaseQuicBitEnabled
                {
                    get
                    {
                        return (_bitfield >> 32) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 32)) | ((value & 0x1UL) << 32);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong EcnEnabled
                {
                    get
                    {
                        return (_bitfield >> 33) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 33)) | ((value & 0x1UL) << 33);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong HyStartEnabled
                {
                    get
                    {
                        return (_bitfield >> 34) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 34)) | ((value & 0x1UL) << 34);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong StreamRecvWindowBidiLocalDefault
                {
                    get
                    {
                        return (_bitfield >> 35) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 35)) | ((value & 0x1UL) << 35);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong StreamRecvWindowBidiRemoteDefault
                {
                    get
                    {
                        return (_bitfield >> 36) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 36)) | ((value & 0x1UL) << 36);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong StreamRecvWindowUnidiDefault
                {
                    get
                    {
                        return (_bitfield >> 37) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 37)) | ((value & 0x1UL) << 37);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong EncryptionOffloadAllowed
                {
                    get
                    {
                        return (_bitfield >> 38) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 38)) | ((value & 0x1UL) << 38);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong ReliableResetEnabled
                {
                    get
                    {
                        return (_bitfield >> 39) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 39)) | ((value & 0x1UL) << 39);
                    }
                }

                [NativeTypeName("uint64_t : 1")]
                internal ulong OneWayDelayEnabled
                {
                    get
                    {
                        return (_bitfield >> 40) & 0x1UL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x1UL << 40)) | ((value & 0x1UL) << 40);
                    }
                }

                [NativeTypeName("uint64_t : 23")]
                internal ulong RESERVED
                {
                    get
                    {
                        return (_bitfield >> 41) & 0x7FFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0x7FFFFFUL << 41)) | ((value & 0x7FFFFFUL) << 41);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous2_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("uint64_t")]
            internal ulong Flags;

            [FieldOffset(0)]
            [NativeTypeName("QUIC_SETTINGS::(anonymous struct)")]
            internal _Anonymous_e__Struct Anonymous;

            internal partial struct _Anonymous_e__Struct
            {
                internal ulong _bitfield;

                [NativeTypeName("uint64_t : 1")]
                internal ulong HyStartEnabled
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
                internal ulong EncryptionOffloadAllowed
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
                internal ulong ReliableResetEnabled
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
                internal ulong OneWayDelayEnabled
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

                [NativeTypeName("uint64_t : 60")]
                internal ulong ReservedFlags
                {
                    get
                    {
                        return (_bitfield >> 4) & 0xFFFFFFFUL;
                    }

                    set
                    {
                        _bitfield = (_bitfield & ~(0xFFFFFFFUL << 4)) | ((value & 0xFFFFFFFUL) << 4);
                    }
                }
            }
        }
    }

    internal unsafe partial struct QUIC_TLS_SECRETS
    {
        [NativeTypeName("uint8_t")]
        internal byte SecretLength;

        [NativeTypeName("struct (anonymous struct)")]
        internal _IsSet_e__Struct IsSet;

        [NativeTypeName("uint8_t [32]")]
        internal fixed byte ClientRandom[32];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte ClientEarlyTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte ClientHandshakeTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte ServerHandshakeTrafficSecret[64];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte ClientTrafficSecret0[64];

        [NativeTypeName("uint8_t [64]")]
        internal fixed byte ServerTrafficSecret0[64];

        internal partial struct _IsSet_e__Struct
        {
            internal byte _bitfield;

            [NativeTypeName("uint8_t : 1")]
            internal byte ClientRandom
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
            internal byte ClientEarlyTrafficSecret
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
            internal byte ClientHandshakeTrafficSecret
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
            internal byte ServerHandshakeTrafficSecret
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
            internal byte ClientTrafficSecret0
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
            internal byte ServerTrafficSecret0
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

    internal partial struct QUIC_STREAM_STATISTICS
    {
        [NativeTypeName("uint64_t")]
        internal ulong ConnBlockedBySchedulingUs;

        [NativeTypeName("uint64_t")]
        internal ulong ConnBlockedByPacingUs;

        [NativeTypeName("uint64_t")]
        internal ulong ConnBlockedByAmplificationProtUs;

        [NativeTypeName("uint64_t")]
        internal ulong ConnBlockedByCongestionControlUs;

        [NativeTypeName("uint64_t")]
        internal ulong ConnBlockedByFlowControlUs;

        [NativeTypeName("uint64_t")]
        internal ulong StreamBlockedByIdFlowControlUs;

        [NativeTypeName("uint64_t")]
        internal ulong StreamBlockedByFlowControlUs;

        [NativeTypeName("uint64_t")]
        internal ulong StreamBlockedByAppUs;
    }

    internal unsafe partial struct QUIC_SCHANNEL_CREDENTIAL_ATTRIBUTE_W
    {
        [NativeTypeName("unsigned long")]
        internal uint Attribute;

        [NativeTypeName("unsigned long")]
        internal uint BufferLength;

        internal void* Buffer;
    }

    internal unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_W
    {
        [NativeTypeName("unsigned long")]
        internal uint Attribute;

        internal void* Buffer;
    }

    internal unsafe partial struct QUIC_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W
    {
        [NativeTypeName("unsigned long")]
        internal uint Attribute;

        [NativeTypeName("unsigned long")]
        internal uint BufferLength;

        internal void* Buffer;
    }

    internal enum QUIC_LISTENER_EVENT_TYPE
    {
        NEW_CONNECTION = 0,
        STOP_COMPLETE = 1,
    }

    internal partial struct QUIC_LISTENER_EVENT
    {
        internal QUIC_LISTENER_EVENT_TYPE Type;

        [NativeTypeName("QUIC_LISTENER_EVENT::(anonymous union)")]
        internal _Anonymous_e__Union Anonymous;

        internal ref _Anonymous_e__Union._NEW_CONNECTION_e__Struct NEW_CONNECTION
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.NEW_CONNECTION, 1));
            }
        }

        internal ref _Anonymous_e__Union._STOP_COMPLETE_e__Struct STOP_COMPLETE
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
            internal _NEW_CONNECTION_e__Struct NEW_CONNECTION;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _STOP_COMPLETE_e__Struct STOP_COMPLETE;

            internal unsafe partial struct _NEW_CONNECTION_e__Struct
            {
                [NativeTypeName("const QUIC_NEW_CONNECTION_INFO *")]
                internal QUIC_NEW_CONNECTION_INFO* Info;

                [NativeTypeName("HQUIC")]
                internal QUIC_HANDLE* Connection;
            }

            internal partial struct _STOP_COMPLETE_e__Struct
            {
                internal byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                internal byte AppCloseInProgress
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
                internal byte RESERVED
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
        RESUMPTION_TICKET_RECEIVED = 14,
        PEER_CERTIFICATE_RECEIVED = 15,
        RELIABLE_RESET_NEGOTIATED = 16,
        ONE_WAY_DELAY_NEGOTIATED = 17,
    }

    internal partial struct QUIC_CONNECTION_EVENT
    {
        internal QUIC_CONNECTION_EVENT_TYPE Type;

        [NativeTypeName("QUIC_CONNECTION_EVENT::(anonymous union)")]
        internal _Anonymous_e__Union Anonymous;

        internal ref _Anonymous_e__Union._CONNECTED_e__Struct CONNECTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.CONNECTED, 1));
            }
        }

        internal ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_INITIATED_BY_TRANSPORT, 1));
            }
        }

        internal ref _Anonymous_e__Union._SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_INITIATED_BY_PEER, 1));
            }
        }

        internal ref _Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_COMPLETE, 1));
            }
        }

        internal ref _Anonymous_e__Union._LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.LOCAL_ADDRESS_CHANGED, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_ADDRESS_CHANGED, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_STREAM_STARTED, 1));
            }
        }

        internal ref _Anonymous_e__Union._STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.STREAMS_AVAILABLE, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_NEEDS_STREAMS_e__Struct PEER_NEEDS_STREAMS
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_NEEDS_STREAMS, 1));
            }
        }

        internal ref _Anonymous_e__Union._IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.IDEAL_PROCESSOR_CHANGED, 1));
            }
        }

        internal ref _Anonymous_e__Union._DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_STATE_CHANGED, 1));
            }
        }

        internal ref _Anonymous_e__Union._DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_RECEIVED, 1));
            }
        }

        internal ref _Anonymous_e__Union._DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.DATAGRAM_SEND_STATE_CHANGED, 1));
            }
        }

        internal ref _Anonymous_e__Union._RESUMED_e__Struct RESUMED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RESUMED, 1));
            }
        }

        internal ref _Anonymous_e__Union._RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RESUMPTION_TICKET_RECEIVED, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_CERTIFICATE_RECEIVED, 1));
            }
        }

        internal ref _Anonymous_e__Union._RELIABLE_RESET_NEGOTIATED_e__Struct RELIABLE_RESET_NEGOTIATED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RELIABLE_RESET_NEGOTIATED, 1));
            }
        }

        internal ref _Anonymous_e__Union._ONE_WAY_DELAY_NEGOTIATED_e__Struct ONE_WAY_DELAY_NEGOTIATED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.ONE_WAY_DELAY_NEGOTIATED, 1));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _CONNECTED_e__Struct CONNECTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct SHUTDOWN_INITIATED_BY_TRANSPORT;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SHUTDOWN_INITIATED_BY_PEER_e__Struct SHUTDOWN_INITIATED_BY_PEER;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _LOCAL_ADDRESS_CHANGED_e__Struct LOCAL_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_ADDRESS_CHANGED_e__Struct PEER_ADDRESS_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_STREAM_STARTED_e__Struct PEER_STREAM_STARTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _STREAMS_AVAILABLE_e__Struct STREAMS_AVAILABLE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_NEEDS_STREAMS_e__Struct PEER_NEEDS_STREAMS;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _IDEAL_PROCESSOR_CHANGED_e__Struct IDEAL_PROCESSOR_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _DATAGRAM_STATE_CHANGED_e__Struct DATAGRAM_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _DATAGRAM_RECEIVED_e__Struct DATAGRAM_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _DATAGRAM_SEND_STATE_CHANGED_e__Struct DATAGRAM_SEND_STATE_CHANGED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _RESUMED_e__Struct RESUMED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _RESUMPTION_TICKET_RECEIVED_e__Struct RESUMPTION_TICKET_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_CERTIFICATE_RECEIVED_e__Struct PEER_CERTIFICATE_RECEIVED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _RELIABLE_RESET_NEGOTIATED_e__Struct RELIABLE_RESET_NEGOTIATED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _ONE_WAY_DELAY_NEGOTIATED_e__Struct ONE_WAY_DELAY_NEGOTIATED;

            internal unsafe partial struct _CONNECTED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte SessionResumed;

                [NativeTypeName("uint8_t")]
                internal byte NegotiatedAlpnLength;

                [NativeTypeName("const uint8_t *")]
                internal byte* NegotiatedAlpn;
            }

            internal partial struct _SHUTDOWN_INITIATED_BY_TRANSPORT_e__Struct
            {
                [NativeTypeName("HRESULT")]
                internal int Status;

                [NativeTypeName("QUIC_UINT62")]
                internal ulong ErrorCode;
            }

            internal partial struct _SHUTDOWN_INITIATED_BY_PEER_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                internal ulong ErrorCode;
            }

            internal partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                internal byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                internal byte HandshakeCompleted
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
                internal byte PeerAcknowledgedShutdown
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
                internal byte AppCloseInProgress
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
                internal QuicAddr* Address;
            }

            internal unsafe partial struct _PEER_ADDRESS_CHANGED_e__Struct
            {
                [NativeTypeName("const QUIC_ADDR *")]
                internal QuicAddr* Address;
            }

            internal unsafe partial struct _PEER_STREAM_STARTED_e__Struct
            {
                [NativeTypeName("HQUIC")]
                internal QUIC_HANDLE* Stream;

                internal QUIC_STREAM_OPEN_FLAGS Flags;
            }

            internal partial struct _STREAMS_AVAILABLE_e__Struct
            {
                [NativeTypeName("uint16_t")]
                internal ushort BidirectionalCount;

                [NativeTypeName("uint16_t")]
                internal ushort UnidirectionalCount;
            }

            internal partial struct _PEER_NEEDS_STREAMS_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte Bidirectional;
            }

            internal partial struct _IDEAL_PROCESSOR_CHANGED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                internal ushort IdealProcessor;

                [NativeTypeName("uint16_t")]
                internal ushort PartitionIndex;
            }

            internal partial struct _DATAGRAM_STATE_CHANGED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte SendEnabled;

                [NativeTypeName("uint16_t")]
                internal ushort MaxSendLength;
            }

            internal unsafe partial struct _DATAGRAM_RECEIVED_e__Struct
            {
                [NativeTypeName("const QUIC_BUFFER *")]
                internal QUIC_BUFFER* Buffer;

                internal QUIC_RECEIVE_FLAGS Flags;
            }

            internal unsafe partial struct _DATAGRAM_SEND_STATE_CHANGED_e__Struct
            {
                internal void* ClientContext;

                internal QUIC_DATAGRAM_SEND_STATE State;
            }

            internal unsafe partial struct _RESUMED_e__Struct
            {
                [NativeTypeName("uint16_t")]
                internal ushort ResumptionStateLength;

                [NativeTypeName("const uint8_t *")]
                internal byte* ResumptionState;
            }

            internal unsafe partial struct _RESUMPTION_TICKET_RECEIVED_e__Struct
            {
                [NativeTypeName("uint32_t")]
                internal uint ResumptionTicketLength;

                [NativeTypeName("const uint8_t *")]
                internal byte* ResumptionTicket;
            }

            internal unsafe partial struct _PEER_CERTIFICATE_RECEIVED_e__Struct
            {
                [NativeTypeName("QUIC_CERTIFICATE *")]
                internal void* Certificate;

                [NativeTypeName("uint32_t")]
                internal uint DeferredErrorFlags;

                [NativeTypeName("HRESULT")]
                internal int DeferredStatus;

                [NativeTypeName("QUIC_CERTIFICATE_CHAIN *")]
                internal void* Chain;
            }

            internal partial struct _RELIABLE_RESET_NEGOTIATED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte IsNegotiated;
            }

            internal partial struct _ONE_WAY_DELAY_NEGOTIATED_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte SendNegotiated;

                [NativeTypeName("BOOLEAN")]
                internal byte ReceiveNegotiated;
            }
        }
    }

    internal enum QUIC_STREAM_EVENT_TYPE
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
        PEER_ACCEPTED = 9,
    }

    internal partial struct QUIC_STREAM_EVENT
    {
        internal QUIC_STREAM_EVENT_TYPE Type;

        [NativeTypeName("QUIC_STREAM_EVENT::(anonymous union)")]
        internal _Anonymous_e__Union Anonymous;

        internal ref _Anonymous_e__Union._START_COMPLETE_e__Struct START_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.START_COMPLETE, 1));
            }
        }

        internal ref _Anonymous_e__Union._RECEIVE_e__Struct RECEIVE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.RECEIVE, 1));
            }
        }

        internal ref _Anonymous_e__Union._SEND_COMPLETE_e__Struct SEND_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SEND_COMPLETE, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_SEND_ABORTED, 1));
            }
        }

        internal ref _Anonymous_e__Union._PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.PEER_RECEIVE_ABORTED, 1));
            }
        }

        internal ref _Anonymous_e__Union._SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SEND_SHUTDOWN_COMPLETE, 1));
            }
        }

        internal ref _Anonymous_e__Union._SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE
        {
            get
            {
                return ref MemoryMarshal.GetReference(MemoryMarshal.CreateSpan(ref Anonymous.SHUTDOWN_COMPLETE, 1));
            }
        }

        internal ref _Anonymous_e__Union._IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE
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
            internal _START_COMPLETE_e__Struct START_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _RECEIVE_e__Struct RECEIVE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SEND_COMPLETE_e__Struct SEND_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_SEND_ABORTED_e__Struct PEER_SEND_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _PEER_RECEIVE_ABORTED_e__Struct PEER_RECEIVE_ABORTED;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SEND_SHUTDOWN_COMPLETE_e__Struct SEND_SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _SHUTDOWN_COMPLETE_e__Struct SHUTDOWN_COMPLETE;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct)")]
            internal _IDEAL_SEND_BUFFER_SIZE_e__Struct IDEAL_SEND_BUFFER_SIZE;

            internal partial struct _START_COMPLETE_e__Struct
            {
                [NativeTypeName("HRESULT")]
                internal int Status;

                [NativeTypeName("QUIC_UINT62")]
                internal ulong ID;

                internal byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                internal byte PeerAccepted
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
                internal byte RESERVED
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
                internal ulong AbsoluteOffset;

                [NativeTypeName("uint64_t")]
                internal ulong TotalBufferLength;

                [NativeTypeName("const QUIC_BUFFER *")]
                internal QUIC_BUFFER* Buffers;

                [NativeTypeName("uint32_t")]
                internal uint BufferCount;

                internal QUIC_RECEIVE_FLAGS Flags;
            }

            internal unsafe partial struct _SEND_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte Canceled;

                internal void* ClientContext;
            }

            internal partial struct _PEER_SEND_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                internal ulong ErrorCode;
            }

            internal partial struct _PEER_RECEIVE_ABORTED_e__Struct
            {
                [NativeTypeName("QUIC_UINT62")]
                internal ulong ErrorCode;
            }

            internal partial struct _SEND_SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte Graceful;
            }

            internal partial struct _SHUTDOWN_COMPLETE_e__Struct
            {
                [NativeTypeName("BOOLEAN")]
                internal byte ConnectionShutdown;

                internal byte _bitfield;

                [NativeTypeName("BOOLEAN : 1")]
                internal byte AppCloseInProgress
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
                internal byte ConnectionShutdownByApp
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
                internal byte ConnectionClosedRemotely
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

                [NativeTypeName("BOOLEAN : 5")]
                internal byte RESERVED
                {
                    get
                    {
                        return (byte)((_bitfield >> 3) & 0x1Fu);
                    }

                    set
                    {
                        _bitfield = (byte)((_bitfield & ~(0x1Fu << 3)) | ((value & 0x1Fu) << 3));
                    }
                }

                [NativeTypeName("QUIC_UINT62")]
                internal ulong ConnectionErrorCode;

                [NativeTypeName("HRESULT")]
                internal int ConnectionCloseStatus;
            }

            internal partial struct _IDEAL_SEND_BUFFER_SIZE_e__Struct
            {
                [NativeTypeName("uint64_t")]
                internal ulong ByteCount;
            }
        }
    }

    internal unsafe partial struct QUIC_API_TABLE
    {
        [NativeTypeName("QUIC_SET_CONTEXT_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, void> SetContext;

        [NativeTypeName("QUIC_GET_CONTEXT_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*> GetContext;

        [NativeTypeName("QUIC_SET_CALLBACK_HANDLER_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, void*, void> SetCallbackHandler;

        [NativeTypeName("QUIC_SET_PARAM_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, uint, uint, void*, int> SetParam;

        [NativeTypeName("QUIC_GET_PARAM_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, uint, uint*, void*, int> GetParam;

        [NativeTypeName("QUIC_REGISTRATION_OPEN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_REGISTRATION_CONFIG*, QUIC_HANDLE**, int> RegistrationOpen;

        [NativeTypeName("QUIC_REGISTRATION_CLOSE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> RegistrationClose;

        [NativeTypeName("QUIC_REGISTRATION_SHUTDOWN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CONNECTION_SHUTDOWN_FLAGS, ulong, void> RegistrationShutdown;

        [NativeTypeName("QUIC_CONFIGURATION_OPEN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SETTINGS*, uint, void*, QUIC_HANDLE**, int> ConfigurationOpen;

        [NativeTypeName("QUIC_CONFIGURATION_CLOSE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ConfigurationClose;

        [NativeTypeName("QUIC_CONFIGURATION_LOAD_CREDENTIAL_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CREDENTIAL_CONFIG*, int> ConfigurationLoadCredential;

        [NativeTypeName("QUIC_LISTENER_OPEN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_LISTENER_EVENT*, int>, void*, QUIC_HANDLE**, int> ListenerOpen;

        [NativeTypeName("QUIC_LISTENER_CLOSE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ListenerClose;

        [NativeTypeName("QUIC_LISTENER_START_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QuicAddr*, int> ListenerStart;

        [NativeTypeName("QUIC_LISTENER_STOP_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ListenerStop;

        [NativeTypeName("QUIC_CONNECTION_OPEN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_CONNECTION_EVENT*, int>, void*, QUIC_HANDLE**, int> ConnectionOpen;

        [NativeTypeName("QUIC_CONNECTION_CLOSE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> ConnectionClose;

        [NativeTypeName("QUIC_CONNECTION_SHUTDOWN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_CONNECTION_SHUTDOWN_FLAGS, ulong, void> ConnectionShutdown;

        [NativeTypeName("QUIC_CONNECTION_START_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_HANDLE*, ushort, sbyte*, ushort, int> ConnectionStart;

        [NativeTypeName("QUIC_CONNECTION_SET_CONFIGURATION_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_HANDLE*, int> ConnectionSetConfiguration;

        [NativeTypeName("QUIC_CONNECTION_SEND_RESUMPTION_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_SEND_RESUMPTION_FLAGS, ushort, byte*, int> ConnectionSendResumptionTicket;

        [NativeTypeName("QUIC_STREAM_OPEN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_OPEN_FLAGS, delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void*, QUIC_STREAM_EVENT*, int>, void*, QUIC_HANDLE**, int> StreamOpen;

        [NativeTypeName("QUIC_STREAM_CLOSE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, void> StreamClose;

        [NativeTypeName("QUIC_STREAM_START_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_START_FLAGS, int> StreamStart;

        [NativeTypeName("QUIC_STREAM_SHUTDOWN_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_STREAM_SHUTDOWN_FLAGS, ulong, int> StreamShutdown;

        [NativeTypeName("QUIC_STREAM_SEND_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SEND_FLAGS, void*, int> StreamSend;

        [NativeTypeName("QUIC_STREAM_RECEIVE_COMPLETE_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, ulong, void> StreamReceiveComplete;

        [NativeTypeName("QUIC_STREAM_RECEIVE_SET_ENABLED_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, byte, int> StreamReceiveSetEnabled;

        [NativeTypeName("QUIC_DATAGRAM_SEND_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, QUIC_BUFFER*, uint, QUIC_SEND_FLAGS, void*, int> DatagramSend;

        [NativeTypeName("QUIC_CONNECTION_COMP_RESUMPTION_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, byte, int> ConnectionResumptionTicketValidationComplete;

        [NativeTypeName("QUIC_CONNECTION_COMP_CERT_FN")]
        internal delegate* unmanaged[Cdecl]<QUIC_HANDLE*, byte, QUIC_TLS_ALERT_CODES, int> ConnectionCertificateValidationComplete;
    }

    internal static unsafe partial class MsQuic
    {
        [DllImport("msquic", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        internal static extern int MsQuicOpenVersion([NativeTypeName("uint32_t")] uint Version, [NativeTypeName("const void **")] void** QuicApi);

        [DllImport("msquic", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        internal static extern void MsQuicClose([NativeTypeName("const void *")] void* QuicApi);

        [NativeTypeName("#define QUIC_MAX_ALPN_LENGTH 255")]
        internal const uint QUIC_MAX_ALPN_LENGTH = 255;

        [NativeTypeName("#define QUIC_MAX_SNI_LENGTH 65535")]
        internal const uint QUIC_MAX_SNI_LENGTH = 65535;

        [NativeTypeName("#define QUIC_MAX_RESUMPTION_APP_DATA_LENGTH 1000")]
        internal const uint QUIC_MAX_RESUMPTION_APP_DATA_LENGTH = 1000;

        [NativeTypeName("#define QUIC_STATELESS_RESET_KEY_LENGTH 32")]
        internal const uint QUIC_STATELESS_RESET_KEY_LENGTH = 32;

        [NativeTypeName("#define QUIC_EXECUTION_CONFIG_MIN_SIZE (uint32_t)FIELD_OFFSET(QUIC_EXECUTION_CONFIG, ProcessorList)")]
        internal static readonly uint QUIC_EXECUTION_CONFIG_MIN_SIZE = unchecked((uint)((int)(Marshal.OffsetOf<QUIC_EXECUTION_CONFIG>("ProcessorList"))));

        [NativeTypeName("#define QUIC_MAX_TICKET_KEY_COUNT 16")]
        internal const uint QUIC_MAX_TICKET_KEY_COUNT = 16;

        [NativeTypeName("#define QUIC_TLS_SECRETS_MAX_SECRET_LEN 64")]
        internal const uint QUIC_TLS_SECRETS_MAX_SECRET_LEN = 64;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_GLOBAL 0x01000000")]
        internal const uint QUIC_PARAM_PREFIX_GLOBAL = 0x01000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_REGISTRATION 0x02000000")]
        internal const uint QUIC_PARAM_PREFIX_REGISTRATION = 0x02000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_CONFIGURATION 0x03000000")]
        internal const uint QUIC_PARAM_PREFIX_CONFIGURATION = 0x03000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_LISTENER 0x04000000")]
        internal const uint QUIC_PARAM_PREFIX_LISTENER = 0x04000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_CONNECTION 0x05000000")]
        internal const uint QUIC_PARAM_PREFIX_CONNECTION = 0x05000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS 0x06000000")]
        internal const uint QUIC_PARAM_PREFIX_TLS = 0x06000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_TLS_SCHANNEL 0x07000000")]
        internal const uint QUIC_PARAM_PREFIX_TLS_SCHANNEL = 0x07000000;

        [NativeTypeName("#define QUIC_PARAM_PREFIX_STREAM 0x08000000")]
        internal const uint QUIC_PARAM_PREFIX_STREAM = 0x08000000;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT 0x01000000")]
        internal const uint QUIC_PARAM_GLOBAL_RETRY_MEMORY_PERCENT = 0x01000000;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS 0x01000001")]
        internal const uint QUIC_PARAM_GLOBAL_SUPPORTED_VERSIONS = 0x01000001;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE 0x01000002")]
        internal const uint QUIC_PARAM_GLOBAL_LOAD_BALACING_MODE = 0x01000002;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_PERF_COUNTERS 0x01000003")]
        internal const uint QUIC_PARAM_GLOBAL_PERF_COUNTERS = 0x01000003;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_VERSION 0x01000004")]
        internal const uint QUIC_PARAM_GLOBAL_LIBRARY_VERSION = 0x01000004;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_SETTINGS 0x01000005")]
        internal const uint QUIC_PARAM_GLOBAL_SETTINGS = 0x01000005;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS 0x01000006")]
        internal const uint QUIC_PARAM_GLOBAL_GLOBAL_SETTINGS = 0x01000006;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_VERSION_SETTINGS 0x01000007")]
        internal const uint QUIC_PARAM_GLOBAL_VERSION_SETTINGS = 0x01000007;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH 0x01000008")]
        internal const uint QUIC_PARAM_GLOBAL_LIBRARY_GIT_HASH = 0x01000008;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_EXECUTION_CONFIG 0x01000009")]
        internal const uint QUIC_PARAM_GLOBAL_EXECUTION_CONFIG = 0x01000009;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_TLS_PROVIDER 0x0100000A")]
        internal const uint QUIC_PARAM_GLOBAL_TLS_PROVIDER = 0x0100000A;

        [NativeTypeName("#define QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY 0x0100000B")]
        internal const uint QUIC_PARAM_GLOBAL_STATELESS_RESET_KEY = 0x0100000B;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_SETTINGS 0x03000000")]
        internal const uint QUIC_PARAM_CONFIGURATION_SETTINGS = 0x03000000;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_TICKET_KEYS 0x03000001")]
        internal const uint QUIC_PARAM_CONFIGURATION_TICKET_KEYS = 0x03000001;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS 0x03000002")]
        internal const uint QUIC_PARAM_CONFIGURATION_VERSION_SETTINGS = 0x03000002;

        [NativeTypeName("#define QUIC_PARAM_CONFIGURATION_SCHANNEL_CREDENTIAL_ATTRIBUTE_W 0x03000003")]
        internal const uint QUIC_PARAM_CONFIGURATION_SCHANNEL_CREDENTIAL_ATTRIBUTE_W = 0x03000003;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_LOCAL_ADDRESS 0x04000000")]
        internal const uint QUIC_PARAM_LISTENER_LOCAL_ADDRESS = 0x04000000;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_STATS 0x04000001")]
        internal const uint QUIC_PARAM_LISTENER_STATS = 0x04000001;

        [NativeTypeName("#define QUIC_PARAM_LISTENER_CIBIR_ID 0x04000002")]
        internal const uint QUIC_PARAM_LISTENER_CIBIR_ID = 0x04000002;

        [NativeTypeName("#define QUIC_PARAM_CONN_QUIC_VERSION 0x05000000")]
        internal const uint QUIC_PARAM_CONN_QUIC_VERSION = 0x05000000;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_ADDRESS 0x05000001")]
        internal const uint QUIC_PARAM_CONN_LOCAL_ADDRESS = 0x05000001;

        [NativeTypeName("#define QUIC_PARAM_CONN_REMOTE_ADDRESS 0x05000002")]
        internal const uint QUIC_PARAM_CONN_REMOTE_ADDRESS = 0x05000002;

        [NativeTypeName("#define QUIC_PARAM_CONN_IDEAL_PROCESSOR 0x05000003")]
        internal const uint QUIC_PARAM_CONN_IDEAL_PROCESSOR = 0x05000003;

        [NativeTypeName("#define QUIC_PARAM_CONN_SETTINGS 0x05000004")]
        internal const uint QUIC_PARAM_CONN_SETTINGS = 0x05000004;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS 0x05000005")]
        internal const uint QUIC_PARAM_CONN_STATISTICS = 0x05000005;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_PLAT 0x05000006")]
        internal const uint QUIC_PARAM_CONN_STATISTICS_PLAT = 0x05000006;

        [NativeTypeName("#define QUIC_PARAM_CONN_SHARE_UDP_BINDING 0x05000007")]
        internal const uint QUIC_PARAM_CONN_SHARE_UDP_BINDING = 0x05000007;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT 0x05000008")]
        internal const uint QUIC_PARAM_CONN_LOCAL_BIDI_STREAM_COUNT = 0x05000008;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT 0x05000009")]
        internal const uint QUIC_PARAM_CONN_LOCAL_UNIDI_STREAM_COUNT = 0x05000009;

        [NativeTypeName("#define QUIC_PARAM_CONN_MAX_STREAM_IDS 0x0500000A")]
        internal const uint QUIC_PARAM_CONN_MAX_STREAM_IDS = 0x0500000A;

        [NativeTypeName("#define QUIC_PARAM_CONN_CLOSE_REASON_PHRASE 0x0500000B")]
        internal const uint QUIC_PARAM_CONN_CLOSE_REASON_PHRASE = 0x0500000B;

        [NativeTypeName("#define QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME 0x0500000C")]
        internal const uint QUIC_PARAM_CONN_STREAM_SCHEDULING_SCHEME = 0x0500000C;

        [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED 0x0500000D")]
        internal const uint QUIC_PARAM_CONN_DATAGRAM_RECEIVE_ENABLED = 0x0500000D;

        [NativeTypeName("#define QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED 0x0500000E")]
        internal const uint QUIC_PARAM_CONN_DATAGRAM_SEND_ENABLED = 0x0500000E;

        [NativeTypeName("#define QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION 0x0500000F")]
        internal const uint QUIC_PARAM_CONN_DISABLE_1RTT_ENCRYPTION = 0x0500000F;

        [NativeTypeName("#define QUIC_PARAM_CONN_RESUMPTION_TICKET 0x05000010")]
        internal const uint QUIC_PARAM_CONN_RESUMPTION_TICKET = 0x05000010;

        [NativeTypeName("#define QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID 0x05000011")]
        internal const uint QUIC_PARAM_CONN_PEER_CERTIFICATE_VALID = 0x05000011;

        [NativeTypeName("#define QUIC_PARAM_CONN_LOCAL_INTERFACE 0x05000012")]
        internal const uint QUIC_PARAM_CONN_LOCAL_INTERFACE = 0x05000012;

        [NativeTypeName("#define QUIC_PARAM_CONN_TLS_SECRETS 0x05000013")]
        internal const uint QUIC_PARAM_CONN_TLS_SECRETS = 0x05000013;

        [NativeTypeName("#define QUIC_PARAM_CONN_VERSION_SETTINGS 0x05000014")]
        internal const uint QUIC_PARAM_CONN_VERSION_SETTINGS = 0x05000014;

        [NativeTypeName("#define QUIC_PARAM_CONN_CIBIR_ID 0x05000015")]
        internal const uint QUIC_PARAM_CONN_CIBIR_ID = 0x05000015;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2 0x05000016")]
        internal const uint QUIC_PARAM_CONN_STATISTICS_V2 = 0x05000016;

        [NativeTypeName("#define QUIC_PARAM_CONN_STATISTICS_V2_PLAT 0x05000017")]
        internal const uint QUIC_PARAM_CONN_STATISTICS_V2_PLAT = 0x05000017;

        [NativeTypeName("#define QUIC_PARAM_CONN_ORIG_DEST_CID 0x05000018")]
        internal const uint QUIC_PARAM_CONN_ORIG_DEST_CID = 0x05000018;

        [NativeTypeName("#define QUIC_PARAM_TLS_HANDSHAKE_INFO 0x06000000")]
        internal const uint QUIC_PARAM_TLS_HANDSHAKE_INFO = 0x06000000;

        [NativeTypeName("#define QUIC_PARAM_TLS_NEGOTIATED_ALPN 0x06000001")]
        internal const uint QUIC_PARAM_TLS_NEGOTIATED_ALPN = 0x06000001;

        [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W 0x07000000")]
        internal const uint QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_W = 0x07000000;

        [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W 0x07000001")]
        internal const uint QUIC_PARAM_TLS_SCHANNEL_CONTEXT_ATTRIBUTE_EX_W = 0x07000001;

        [NativeTypeName("#define QUIC_PARAM_TLS_SCHANNEL_SECURITY_CONTEXT_TOKEN 0x07000002")]
        internal const uint QUIC_PARAM_TLS_SCHANNEL_SECURITY_CONTEXT_TOKEN = 0x07000002;

        [NativeTypeName("#define QUIC_PARAM_STREAM_ID 0x08000000")]
        internal const uint QUIC_PARAM_STREAM_ID = 0x08000000;

        [NativeTypeName("#define QUIC_PARAM_STREAM_0RTT_LENGTH 0x08000001")]
        internal const uint QUIC_PARAM_STREAM_0RTT_LENGTH = 0x08000001;

        [NativeTypeName("#define QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE 0x08000002")]
        internal const uint QUIC_PARAM_STREAM_IDEAL_SEND_BUFFER_SIZE = 0x08000002;

        [NativeTypeName("#define QUIC_PARAM_STREAM_PRIORITY 0x08000003")]
        internal const uint QUIC_PARAM_STREAM_PRIORITY = 0x08000003;

        [NativeTypeName("#define QUIC_PARAM_STREAM_STATISTICS 0X08000004")]
        internal const uint QUIC_PARAM_STREAM_STATISTICS = 0X08000004;

        [NativeTypeName("#define QUIC_PARAM_STREAM_RELIABLE_OFFSET 0x08000005")]
        internal const uint QUIC_PARAM_STREAM_RELIABLE_OFFSET = 0x08000005;

        [NativeTypeName("#define QUIC_API_VERSION_2 2")]
        internal const uint QUIC_API_VERSION_2 = 2;
    }
}
