// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    /// <summary>
    /// Contains all native delegates and structs that are used with MsQuic.
    /// </summary>
    internal static unsafe class MsQuicNativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeApi
        {
            internal IntPtr SetContext;
            internal IntPtr GetContext;
            internal IntPtr SetCallbackHandler;

            internal IntPtr SetParam;
            internal IntPtr GetParam;

            internal IntPtr RegistrationOpen;
            internal IntPtr RegistrationClose;
            internal IntPtr RegistrationShutdown;

            internal IntPtr ConfigurationOpen;
            internal IntPtr ConfigurationClose;
            internal IntPtr ConfigurationLoadCredential;

            internal IntPtr ListenerOpen;
            internal IntPtr ListenerClose;
            internal IntPtr ListenerStart;
            internal IntPtr ListenerStop;

            internal IntPtr ConnectionOpen;
            internal IntPtr ConnectionClose;
            internal IntPtr ConnectionShutdown;
            internal IntPtr ConnectionStart;
            internal IntPtr ConnectionSetConfiguration;
            internal IntPtr ConnectionSendResumptionTicket;

            internal IntPtr StreamOpen;
            internal IntPtr StreamClose;
            internal IntPtr StreamStart;
            internal IntPtr StreamShutdown;
            internal IntPtr StreamSend;
            internal IntPtr StreamReceiveComplete;
            internal IntPtr StreamReceiveSetEnabled;

            internal IntPtr DatagramSend;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void SetCallbackHandlerDelegate(
            SafeHandle handle,
            Delegate del,
            IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint SetParamDelegate(
            SafeHandle handle,
            uint level,
            uint param,
            uint bufferLength,
            byte* buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint GetParamDelegate(
            SafeHandle handle,
            uint level,
            uint param,
            ref uint bufferLength,
            byte* buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint RegistrationOpenDelegate(
            ref RegistrationConfig config,
            out SafeMsQuicRegistrationHandle registrationContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RegistrationCloseDelegate(IntPtr registrationContext);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RegistrationConfig
        {
            internal byte* AppName;
            internal uint ExecutionProfile;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConfigurationOpenDelegate(
            SafeMsQuicRegistrationHandle registrationContext,
            ref QuicBuffer alpnBuffers,
            uint alpnBufferCount,
            ref Settings settings,
            uint settingsSize,
            IntPtr context,
            out SafeMsQuicConfigurationHandle configuration);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ConfigurationCloseDelegate(
            IntPtr configuration);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConfigurationLoadCredentialDelegate(
            SafeMsQuicConfigurationHandle configuration,
            CredentialConfig *credConfig);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListenerEvent
        {
            internal uint Type;
            internal ListenerEventDataUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ListenerEventDataUnion
        {
            [FieldOffset(0)]
            internal ListenerEventDataNewConnection NewConnection;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListenerEventDataNewConnection
        {
            internal IntPtr Info;
            internal IntPtr Connection;
            internal IntPtr SecurityConfig;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NewConnectionInfo
        {
            internal uint QuicVersion;
            internal IntPtr LocalAddress;
            internal IntPtr RemoteAddress;
            internal uint CryptoBufferLength;
            internal ushort AlpnListLength;
            internal ushort ServerNameLength;
            internal byte NegotiatedAlpnLength;
            internal IntPtr CryptoBuffer;
            internal IntPtr ClientAlpnList;
            internal IntPtr NegotiatedAlpn;
            internal IntPtr ServerName;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ListenerCallbackDelegate(
            IntPtr listener,
            IntPtr context,
            ref ListenerEvent evt);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ListenerOpenDelegate(
           SafeMsQuicRegistrationHandle registration,
           ListenerCallbackDelegate handler,
           IntPtr context,
           out SafeMsQuicListenerHandle listener);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ListenerCloseDelegate(
            IntPtr listener);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ListenerStartDelegate(
            SafeMsQuicListenerHandle listener,
            ref QuicBuffer alpnBuffers,
            uint alpnBufferCount,
            ref SOCKADDR_INET localAddress);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ListenerStopDelegate(
            SafeMsQuicListenerHandle listener);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataConnected
        {
            internal bool SessionResumed;
            internal byte NegotiatedAlpnLength;
            internal IntPtr NegotiatedAlpn;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownInitiatedByTransport
        {
            internal uint Status;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownInitiatedByPeer
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataShutdownComplete
        {
            internal bool TimedOut;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataLocalAddrChanged
        {
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataPeerAddrChanged
        {
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataStreamStarted
        {
            internal IntPtr Stream;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataStreamsAvailable
        {
            internal ushort BiDirectionalCount;
            internal ushort UniDirectionalCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct ConnectionEventDataUnion
        {
            [FieldOffset(0)]
            internal ConnectionEventDataConnected Connected;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownInitiatedByTransport ShutdownInitiatedByTransport;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownInitiatedByPeer ShutdownInitiatedByPeer;

            [FieldOffset(0)]
            internal ConnectionEventDataShutdownComplete ShutdownComplete;

            [FieldOffset(0)]
            internal ConnectionEventDataLocalAddrChanged LocalAddrChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataPeerAddrChanged PeerAddrChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataStreamStarted StreamStarted;

            [FieldOffset(0)]
            internal ConnectionEventDataStreamsAvailable StreamsAvailable;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEvent
        {
            internal uint Type;
            internal ConnectionEventDataUnion Data;

            internal bool EarlyDataAccepted => Data.Connected.SessionResumed;
            //internal ulong NumBytes => Data.IdealSendBuffer.NumBytes;
            internal uint ShutdownBeginStatus => Data.ShutdownInitiatedByTransport.Status;
            internal long ShutdownBeginPeerStatus => Data.ShutdownInitiatedByPeer.ErrorCode;
            internal bool ShutdownTimedOut => Data.ShutdownComplete.TimedOut;
            internal ushort BiDirectionalCount => Data.StreamsAvailable.BiDirectionalCount;
            internal ushort UniDirectionalCount => Data.StreamsAvailable.UniDirectionalCount;
            internal QUIC_STREAM_OPEN_FLAG StreamFlags => (QUIC_STREAM_OPEN_FLAG)Data.StreamStarted.Flags;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConnectionCallbackDelegate(
            IntPtr connection,
            IntPtr context,
            ref ConnectionEvent connectionEvent);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConnectionOpenDelegate(
            SafeMsQuicRegistrationHandle registration,
            ConnectionCallbackDelegate handler,
            IntPtr context,
            out SafeMsQuicConnectionHandle connection);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ConnectionCloseDelegate(
            IntPtr connection);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConnectionSetConfigurationDelegate(
            SafeMsQuicConnectionHandle connection,
            SafeMsQuicConfigurationHandle configuration);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConnectionStartDelegate(
            SafeMsQuicConnectionHandle connection,
            SafeMsQuicConfigurationHandle configuration,
            ushort family,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string serverName,
            ushort serverPort);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ConnectionShutdownDelegate(
            SafeMsQuicConnectionHandle connection,
            uint flags,
            long errorCode);

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataRecv
        {
            internal ulong AbsoluteOffset;
            internal ulong TotalBufferLength;
            internal QuicBuffer* Buffers;
            internal uint BufferCount;
            internal uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendComplete
        {
            internal bool Canceled;
            internal IntPtr ClientContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerSendAbort
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerRecvAbort
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendShutdownComplete
        {
            internal byte Graceful;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct StreamEventDataUnion
        {
            [FieldOffset(0)]
            internal StreamEventDataRecv Recv;

            [FieldOffset(0)]
            internal StreamEventDataSendComplete SendComplete;

            [FieldOffset(0)]
            internal StreamEventDataPeerSendAbort PeerSendAbort;

            [FieldOffset(0)]
            internal StreamEventDataPeerRecvAbort PeerRecvAbort;

            [FieldOffset(0)]
            internal StreamEventDataSendShutdownComplete SendShutdownComplete;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEvent
        {
            internal uint Type;
            internal StreamEventDataUnion Data;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN
        {
            internal ushort sin_family;
            internal ushort sin_port;
            internal byte sin_addr0;
            internal byte sin_addr1;
            internal byte sin_addr2;
            internal byte sin_addr3;

            internal byte[] Address
            {
                get
                {
                    return new byte[] { sin_addr0, sin_addr1, sin_addr2, sin_addr3 };
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SOCKADDR_IN6
        {
            internal ushort _family;
            internal ushort _port;
            internal uint _flowinfo;
            internal byte _addr0;
            internal byte _addr1;
            internal byte _addr2;
            internal byte _addr3;
            internal byte _addr4;
            internal byte _addr5;
            internal byte _addr6;
            internal byte _addr7;
            internal byte _addr8;
            internal byte _addr9;
            internal byte _addr10;
            internal byte _addr11;
            internal byte _addr12;
            internal byte _addr13;
            internal byte _addr14;
            internal byte _addr15;
            internal uint _scope_id;

            internal byte[] Address
            {
                get
                {
                    return new byte[] {
                    _addr0, _addr1, _addr2, _addr3,
                    _addr4, _addr5, _addr6, _addr7,
                    _addr8, _addr9, _addr10, _addr11,
                    _addr12, _addr13, _addr14, _addr15 };
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
        internal struct SOCKADDR_INET
        {
            [FieldOffset(0)]
            internal SOCKADDR_IN Ipv4;
            [FieldOffset(0)]
            internal SOCKADDR_IN6 Ipv6;
            [FieldOffset(0)]
            internal ushort si_family;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamCallbackDelegate(
            IntPtr stream,
            IntPtr context,
            ref StreamEvent streamEvent);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamOpenDelegate(
            SafeMsQuicConnectionHandle connection,
            uint flags,
            StreamCallbackDelegate handler,
            IntPtr context,
            out SafeMsQuicStreamHandle stream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamStartDelegate(
            SafeMsQuicStreamHandle stream,
            uint flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void StreamCloseDelegate(
            IntPtr stream);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamShutdownDelegate(
            SafeMsQuicStreamHandle stream,
            uint flags,
            long errorCode);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamSendDelegate(
            SafeMsQuicStreamHandle stream,
            QuicBuffer *buffers,
            uint bufferCount,
            uint flags,
            IntPtr clientSendContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamReceiveCompleteDelegate(
            SafeMsQuicStreamHandle stream,
            ulong bufferLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamReceiveSetEnabledDelegate(
            SafeMsQuicStreamHandle stream,
            [MarshalAs(UnmanagedType.U1)]
            bool enabled);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct QuicBuffer
        {
            internal uint Length;
            internal byte* Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CredentialConfig
        {
            internal uint Type;
            internal uint Flags;
            internal IntPtr Certificate; // PCERT_CONTEXT (SCHANNEL)
            internal IntPtr Principal;
            internal IntPtr TicketKey; // optional.
            internal IntPtr AsyncHandler; // optional, must set LOAD_ASYNCHRONOUS flag.
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Settings
        {
            internal ulong IsSetFlags;
            internal ulong MaxBytesPerKey;
            internal ulong HandshakeIdleTimeoutMs;
            internal ulong IdleTimeoutMs;
            internal uint TlsClientMaxSendBuffer;
            internal uint TlsServerMaxSendBuffer;
            internal uint StreamRecvWindowDefault;
            internal uint StreamRecvBufferDefault;
            internal uint ConnFlowControlWindow;
            internal uint MaxWorkerQueueDelayUs;
            internal uint MaxStatelessOperations;
            internal uint InitialWindowPackets;
            internal uint SendIdleTimeoutMs;
            internal uint InitialRttMs;
            internal uint MaxAckDelayMs;
            internal uint DisconnectTimeoutMs;
            internal uint KeepAliveIntervalMs;
            internal ushort PeerBidiStreamCount;
            internal ushort PeerUnidiStreamCount;
            internal ushort RetryMemoryLimit;              // Global only
            internal ushort LoadBalancingMode;             // Global only
            internal byte MaxOperationsPerDrain;
            internal byte BitField;
        }

        internal enum SettingsBitField : byte
        {
            SendBufferingEnabled = 1,
            PacingEnabled = 1 << 1,
            MigrationEnabled = 1 << 2,
            DatagramReceiveEnabled = 1 << 3,

            // single enum field, 2 bits.
            NoResume = QUIC_SERVER_RESUMPTION_LEVEL.NO_RESUME << 4,
            ResumeOnly =  QUIC_SERVER_RESUMPTION_LEVEL.RESUME_ONLY << 4,
            ResumeAndZeroRtt = QUIC_SERVER_RESUMPTION_LEVEL.RESUME_AND_ZERORTT << 4
        }

        [Flags]
        internal enum SettingsFlags : ulong
        {
            MaxBytesPerKey = 1,
            HandshakeIdleTimeoutMs = 1 << 1,
            IdleTimeoutMs = 1 << 2,
            TlsClientMaxSendBuffer = 1 << 3,
            TlsServerMaxSendBuffer = 1 << 4,
            StreamRecvWindowDefault = 1 << 5,
            StreamRecvBufferDefault = 1 << 6,
            ConnFlowControlWindow = 1 << 7,
            MaxWorkerQueueDelayUs = 1 << 8,
            MaxStatelessOperations = 1 << 9,
            InitialWindowPackets = 1 << 10,
            SendIdleTimeoutMs = 1 << 11,
            InitialRttMs = 1 << 12,
            MaxAckDelayMs = 1 << 13,
            DisconnectTimeoutMs = 1 << 14,
            KeepAliveIntervalMs = 1 << 15,
            PeerBidiStreamCount = 1 << 16,
            PeerUnidiStreamCount = 1 << 17,
            RetryMemoryLimit = 1 << 18,
            LoadBalancingMode = 1 << 19,
            MaxOperationsPerDrain = 1 << 20,
            SendBufferingEnabled = 1 << 21,
            PacingEnabled = 1 << 22,
            MigrationEnabled = 1 << 23,
            DatagramReceiveEnabled = 1 << 24,
            ServerResumptionLevel = 1 << 25
        }
    }
}
