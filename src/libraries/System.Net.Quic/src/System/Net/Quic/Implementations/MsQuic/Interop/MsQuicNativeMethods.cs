// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    /// <summary>
    /// Contains all native delegates and structs that are used with MsQuic.
    /// </summary>
    internal static unsafe partial class MsQuicNativeMethods
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

        internal delegate uint SetContextDelegate(
            SafeHandle handle,
            IntPtr context);

        internal delegate IntPtr GetContextDelegate(
            SafeHandle handle);

        internal delegate void SetCallbackHandlerDelegate(
            SafeHandle handle,
            Delegate del,
            IntPtr context);

        internal delegate uint SetParamDelegate(
            SafeHandle handle,
            uint param,
            uint bufferLength,
            byte* buffer);

        internal delegate uint GetParamDelegate(
            SafeHandle handle,
            uint param,
            ref uint bufferLength,
            byte* buffer);

        internal delegate uint RegistrationOpenDelegate(
            ref RegistrationConfig config,
            out SafeMsQuicRegistrationHandle registrationContext);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RegistrationCloseDelegate(
            IntPtr registrationContext);

        [NativeMarshalling(typeof(Native))]
        internal struct RegistrationConfig
        {
            internal string AppName;
            internal QUIC_EXECUTION_PROFILE ExecutionProfile;

            [CustomTypeMarshaller(typeof(RegistrationConfig), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            [StructLayout(LayoutKind.Sequential)]
            public struct Native
            {
                private IntPtr AppName;
                private QUIC_EXECUTION_PROFILE ExecutionProfile;

                public Native(RegistrationConfig managed)
                {
                    AppName = Marshal.StringToCoTaskMemUTF8(managed.AppName);
                    ExecutionProfile = managed.ExecutionProfile;
                }

                public RegistrationConfig ToManaged()
                {
                    return new RegistrationConfig()
                    {
                        AppName = Marshal.PtrToStringUTF8(AppName)!,
                        ExecutionProfile = ExecutionProfile
                    };
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(AppName);
                }
            }
        }

        internal delegate uint ConfigurationOpenDelegate(
            SafeMsQuicRegistrationHandle registrationContext,
            QuicBuffer* alpnBuffers,
            uint alpnBufferCount,
            ref QuicSettings settings,
            uint settingsSize,
            IntPtr context,
            out SafeMsQuicConfigurationHandle configuration);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ConfigurationCloseDelegate(
            IntPtr configuration);

        internal delegate uint ConfigurationLoadCredentialDelegate(
            SafeMsQuicConfigurationHandle configuration,
            ref CredentialConfig credConfig);

        internal struct AnyDelegateMarshaller
        {
            private readonly Delegate _managed;

            public AnyDelegateMarshaller(Delegate managed)
            {
                _managed = managed;
                Value = Marshal.GetFunctionPointerForDelegate(_managed);
            }

            public IntPtr Value { get; }

            public void FreeNative()
            {
                GC.KeepAlive(_managed);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct QuicSettings
        {
            internal QuicSettingsIsSetFlags IsSetFlags;
            internal ulong MaxBytesPerKey;
            internal ulong HandshakeIdleTimeoutMs;
            internal ulong IdleTimeoutMs;
            internal ulong MtuDiscoverySearchCompleteTimeoutUs;
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
            internal ushort CongestionControlAlgorithm; // QUIC_CONGESTION_CONTROL_ALGORITHM
            internal ushort PeerBidiStreamCount;
            internal ushort PeerUnidiStreamCount;
            internal ushort MaxBindingStatelessOperations;
            internal ushort StatelessOperationExpirationMs;
            internal ushort MinimumMtu;
            internal ushort MaximumMtu;
            internal QuicSettingsEnabledFlagsFlags EnabledFlags;
            internal byte MaxOperationsPerDrain;
            internal byte MtuDiscoveryMissingProbeCount;
        }

        [Flags]
        internal enum QuicSettingsIsSetFlags : ulong
        {
            MaxBytesPerKey = 1UL << 0,
            HandshakeIdleTimeoutMs = 1UL << 1,
            IdleTimeoutMs = 1UL << 2,
            MtuDiscoverySearchCompleteTimeoutUs = 1UL << 3,
            TlsClientMaxSendBuffer = 1UL << 4,
            TlsServerMaxSendBuffer = 1UL << 5,
            StreamRecvWindowDefault = 1UL << 6,
            StreamRecvBufferDefault = 1UL << 7,
            ConnFlowControlWindow = 1UL << 8,
            MaxWorkerQueueDelayUs = 1UL << 9,
            MaxStatelessOperations = 1UL << 10,
            InitialWindowPackets = 1UL << 11,
            SendIdleTimeoutMs = 1UL << 12,
            InitialRttMs = 1UL << 13,
            MaxAckDelayMs = 1UL << 14,
            DisconnectTimeoutMs = 1UL << 15,
            KeepAliveIntervalMs = 1UL << 16,
            CongestionControlAlgorithm = 1UL << 17,
            PeerBidiStreamCount = 1UL << 18,
            PeerUnidiStreamCount = 1UL << 19,
            MaxBindingStatelessOperations = 1UL << 20,
            StatelessOperationExpirationMs = 1UL << 21,
            MinimumMtu = 1UL << 22,
            MaximumMtu = 1UL << 23,
            SendBufferingEnabled = 1UL << 24,
            PacingEnabled = 1UL << 25,
            MigrationEnabled = 1UL << 26,
            DatagramReceiveEnabled = 1UL << 27,
            ServerResumptionLevel = 1UL << 28,
            MaxOperationsPerDrain = 1UL << 29,
            MtuDiscoveryMissingProbeCount = 1UL << 31,
        }

        [Flags]
        internal enum QuicSettingsEnabledFlagsFlags : byte
        {
            SendBufferingEnabled = 1 << 0,
            PacingEnabled = 1 << 1,
            MigrationEnabled = 1 << 2,
            DatagramReceiveEnabled = 1 << 3,
            // Contains values of QUIC_SERVER_RESUMPTION_LEVEL
            ServerResumptionLevel = 1 << 4 | 1 << 5,
            VersionNegotiationExtEnabled = 1 << 6,
        }

        [NativeMarshalling(typeof(Native))]
        internal struct CredentialConfig
        {
            internal QUIC_CREDENTIAL_TYPE Type;
            internal QUIC_CREDENTIAL_FLAGS Flags;
            // CredentialConfigCertificateUnion*
            internal IntPtr Certificate;

            internal string Principal;
            internal IntPtr Reserved; // Currently unused
            // TODO: define delegate for AsyncHandler and make proper use of it.
            internal IntPtr AsyncHandler;
            internal QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

            [CustomTypeMarshaller(typeof(CredentialConfig), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            [StructLayout(LayoutKind.Sequential)]
            public struct Native
            {
                internal QUIC_CREDENTIAL_TYPE Type;
                internal QUIC_CREDENTIAL_FLAGS Flags;
                // CredentialConfigCertificateUnion*
                internal IntPtr Certificate;
                internal IntPtr Principal;
                internal IntPtr Reserved;
                internal IntPtr AsyncHandler;
                internal QUIC_ALLOWED_CIPHER_SUITE_FLAGS AllowedCipherSuites;

                public Native(CredentialConfig managed)
                {
                    Type = managed.Type;
                    Flags = managed.Flags;
                    Certificate = managed.Certificate;
                    Principal = Marshal.StringToCoTaskMemUTF8(managed.Principal);
                    Reserved = managed.Reserved;
                    AsyncHandler = managed.AsyncHandler;
                    AllowedCipherSuites = managed.AllowedCipherSuites;
                }

                public CredentialConfig ToManaged()
                {
                    return new CredentialConfig
                    {
                        Type = Type,
                        Flags = Flags,
                        Certificate = Certificate,
                        Principal = Marshal.PtrToStringUTF8(Principal)!,
                        Reserved = Reserved,
                        AsyncHandler = AsyncHandler,
                        AllowedCipherSuites = AllowedCipherSuites
                    };
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(Principal);
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct CredentialConfigCertificateUnion
        {
            [FieldOffset(0)]
            internal CredentialConfigCertificateHash CertificateHash;

            [FieldOffset(0)]
            internal CredentialConfigCertificateHashStore CertificateHashStore;

            [FieldOffset(0)]
            internal IntPtr CertificateContext;

            [FieldOffset(0)]
            internal CredentialConfigCertificateFile CertificateFile;

            [FieldOffset(0)]
            internal CredentialConfigCertificateFileProtected CertificateFileProtected;

            [FieldOffset(0)]
            internal CredentialConfigCertificatePkcs12 CertificatePkcs12;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CredentialConfigCertificateHash
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            internal byte[] ShaHash;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CredentialConfigCertificateHashStore
        {
            internal QUIC_CERTIFICATE_HASH_STORE_FLAGS Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            internal byte[] ShaHash;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal char[] StoreName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CredentialConfigCertificateFile
        {
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string PrivateKeyFile;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string CertificateFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CredentialConfigCertificateFileProtected
        {
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string PrivateKeyFile;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string CertificateFile;

            [MarshalAs(UnmanagedType.LPUTF8Str)]
            internal string PrivateKeyPassword;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CredentialConfigCertificatePkcs12
        {
            internal IntPtr Asn1Blob;

            internal uint Asn1BlobLength;

            internal IntPtr PrivateKeyPassword;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ListenerEvent
        {
            internal QUIC_LISTENER_EVENT Type;
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
            internal NewConnectionInfo* Info;
            internal IntPtr Connection;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NewConnectionInfo
        {
            internal uint QuicVersion;
            // QUIC_ADDR
            internal IntPtr LocalAddress;
            // QUIC_ADDR
            internal IntPtr RemoteAddress;
            internal uint CryptoBufferLength;
            internal ushort ClientAlpnListLength;
            internal ushort ServerNameLength;
            internal byte NegotiatedAlpnLength;
            // byte[]
            internal IntPtr CryptoBuffer;
            // byte[]
            internal IntPtr ClientAlpnList;
            // byte[]
            internal IntPtr NegotiatedAlpn;
            // string
            internal IntPtr ServerName;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ListenerCallbackDelegate(
            IntPtr listener,
            IntPtr context,
            ListenerEvent* evt);

        internal delegate uint ListenerOpenDelegate(
           SafeMsQuicRegistrationHandle registration,
           ListenerCallbackDelegate handler,
           IntPtr context,
           out SafeMsQuicListenerHandle listener);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ListenerCloseDelegate(
            IntPtr listener);

        internal delegate uint ListenerStartDelegate(
            SafeMsQuicListenerHandle listener,
            QuicBuffer* alpnBuffers,
            uint alpnBufferCount,
            byte* localAddress);

        internal delegate void ListenerStopDelegate(
            SafeMsQuicListenerHandle listener);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataConnected
        {
            internal byte SessionResumed;
            internal byte NegotiatedAlpnLength;
            // byte[]
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
            // The flags have fixed sized exactly 3 bits
            internal ConnectionEventDataShutdownCompleteFlags Flags;
        }

        [Flags]
        internal enum ConnectionEventDataShutdownCompleteFlags : byte
        {
            HandshakeCompleted = 1 << 0,
            PeerAcknowledgedShutdown = 1 << 1,
            AppCloseInProgress = 1 << 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataLocalAddressChanged
        {
            // QUIC_ADDR
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataPeerAddressChanged
        {
            // QUIC_ADDR
            internal IntPtr Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataPeerStreamStarted
        {
            internal IntPtr Stream;
            internal QUIC_STREAM_OPEN_FLAGS Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventDataStreamsAvailable
        {
            internal ushort BiDirectionalCount;
            internal ushort UniDirectionalCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEventPeerCertificateReceived
        {
            internal IntPtr PlatformCertificateHandle;
            internal uint DeferredErrorFlags;
            internal uint DeferredStatus;
            internal IntPtr PlatformCertificateChainHandle;
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
            internal ConnectionEventDataLocalAddressChanged LocalAddressChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataPeerAddressChanged PeerAddressChanged;

            [FieldOffset(0)]
            internal ConnectionEventDataPeerStreamStarted PeerStreamStarted;

            [FieldOffset(0)]
            internal ConnectionEventDataStreamsAvailable StreamsAvailable;

            [FieldOffset(0)]
            internal ConnectionEventPeerCertificateReceived PeerCertificateReceived;

            // TODO: missing IDEAL_PROCESSOR_CHANGED, ...,  (6 total)
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ConnectionEvent
        {
            internal QUIC_CONNECTION_EVENT_TYPE Type;
            internal ConnectionEventDataUnion Data;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint ConnectionCallbackDelegate(
            IntPtr connection,
            IntPtr context,
            ConnectionEvent* connectionEvent);

        // TODO: order is Open, Close, Shutdown, Start, SetConfiguration, SendResumptionTicket
        internal delegate uint ConnectionOpenDelegate(
            SafeMsQuicRegistrationHandle registration,
            ConnectionCallbackDelegate handler,
            IntPtr context,
            out SafeMsQuicConnectionHandle connection);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void ConnectionCloseDelegate(
            IntPtr connection);

        internal delegate uint ConnectionSetConfigurationDelegate(
            SafeMsQuicConnectionHandle connection,
            SafeMsQuicConfigurationHandle configuration);

        internal delegate uint ConnectionStartDelegate(
            SafeMsQuicConnectionHandle connection,
            SafeMsQuicConfigurationHandle configuration,
            QUIC_ADDRESS_FAMILY family,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string serverName,
            ushort serverPort);

        internal delegate void ConnectionShutdownDelegate(
            SafeMsQuicConnectionHandle connection,
            QUIC_CONNECTION_SHUTDOWN_FLAGS flags,
            long errorCode);

        // TODO: missing SendResumptionTicket

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataStartComplete
        {
            internal uint Status;
            internal ulong Id;
            internal byte PeerAccepted;
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataReceive
        {
            internal ulong AbsoluteOffset;
            internal ulong TotalBufferLength;
            internal QuicBuffer* Buffers;
            internal uint BufferCount;
            internal QUIC_RECEIVE_FLAGS Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendComplete
        {
            internal byte Canceled;
            internal IntPtr ClientContext;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerSendAborted
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataPeerReceiveAborted
        {
            internal long ErrorCode;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataSendShutdownComplete
        {
            internal byte Graceful;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEventDataShutdownComplete
        {
            internal byte ConnectionShutdown;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct StreamEventDataUnion
        {
            [FieldOffset(0)]
            internal StreamEventDataStartComplete StartComplete;

            [FieldOffset(0)]
            internal StreamEventDataReceive Receive;

            [FieldOffset(0)]
            internal StreamEventDataSendComplete SendComplete;

            [FieldOffset(0)]
            internal StreamEventDataPeerSendAborted PeerSendAborted;

            [FieldOffset(0)]
            internal StreamEventDataPeerReceiveAborted PeerReceiveAborted;

            [FieldOffset(0)]
            internal StreamEventDataSendShutdownComplete SendShutdownComplete;

            [FieldOffset(0)]
            internal StreamEventDataShutdownComplete ShutdownComplete;

            // TODO: missing IDEAL_SEND_BUFFER_SIZE
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StreamEvent
        {
            internal QUIC_STREAM_EVENT_TYPE Type;
            internal StreamEventDataUnion Data;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate uint StreamCallbackDelegate(
            IntPtr stream,
            IntPtr context,
            StreamEvent* streamEvent);

        internal delegate uint StreamOpenDelegate(
            SafeMsQuicConnectionHandle connection,
            QUIC_STREAM_OPEN_FLAGS flags,
            StreamCallbackDelegate handler,
            IntPtr context,
            out SafeMsQuicStreamHandle stream);

        internal delegate uint StreamStartDelegate(
            SafeMsQuicStreamHandle stream,
            QUIC_STREAM_START_FLAGS flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void StreamCloseDelegate(
            IntPtr stream);

        internal delegate uint StreamShutdownDelegate(
            SafeMsQuicStreamHandle stream,
            QUIC_STREAM_SHUTDOWN_FLAGS flags,
            long errorCode);

        internal delegate uint StreamSendDelegate(
            SafeMsQuicStreamHandle stream,
            QuicBuffer* buffers,
            uint bufferCount,
            QUIC_SEND_FLAGS flags,
            IntPtr clientSendContext);

        internal delegate void StreamReceiveCompleteDelegate(
            SafeMsQuicStreamHandle stream,
            ulong bufferLength);

        internal delegate uint StreamReceiveSetEnabledDelegate(
            SafeMsQuicStreamHandle stream,
            bool enabled);

        [StructLayout(LayoutKind.Sequential)]
        internal struct QuicBuffer
        {
            internal uint Length;
            internal byte* Buffer;
        }

        // TODO: DatagramSend missing

        internal struct DelegateHelper
        {
            private IntPtr _functionPointer;

            public DelegateHelper(IntPtr functionPointer)
            {
                _functionPointer = functionPointer;
            }
            internal uint SetContext(SafeHandle handle, IntPtr context)
            {
                IntPtr __handle_gen_native;
                uint __retVal;
                //
                // Setup
                //
                bool handle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    handle.DangerousAddRef(ref handle__addRefd);
                    __handle_gen_native = handle.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint>)_functionPointer)(__handle_gen_native, context);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (handle__addRefd)
                        handle.DangerousRelease();
                }

                return __retVal;
            }
            internal IntPtr GetContext(SafeHandle handle)
            {
                IntPtr __handle_gen_native;
                IntPtr __retVal;
                //
                // Setup
                //
                bool handle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    handle.DangerousAddRef(ref handle__addRefd);
                    __handle_gen_native = handle.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr>)_functionPointer)(__handle_gen_native);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (handle__addRefd)
                        handle.DangerousRelease();
                }

                return __retVal;
            }
            internal void SetCallbackHandler(SafeHandle handle, Delegate del, IntPtr context)
            {
                //
                // Setup
                //
                bool handle__addRefd = false;
                AnyDelegateMarshaller __del_gen_native__marshaller = default;
                try
                {
                    //
                    // Marshal
                    //
                    handle.DangerousAddRef(ref handle__addRefd);
                    IntPtr __handle_gen_native = handle.DangerousGetHandle();
                    __del_gen_native__marshaller = new AnyDelegateMarshaller(del);
                    IntPtr __del_gen_native = __del_gen_native__marshaller.Value;
                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)_functionPointer)(__handle_gen_native, __del_gen_native, context);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (handle__addRefd)
                        handle.DangerousRelease();
                    __del_gen_native__marshaller.FreeNative();
                }
            }
            internal uint SetParam(SafeHandle handle, uint param, uint bufferLength, byte* buffer)
            {
                uint __retVal;
                //
                // Setup
                //
                bool handle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    handle.DangerousAddRef(ref handle__addRefd);
                    IntPtr __handle_gen_native = handle.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, uint, uint, byte*, uint>)_functionPointer)(__handle_gen_native, param, bufferLength, buffer);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (handle__addRefd)
                        handle.DangerousRelease();
                }

                return __retVal;
            }
            internal uint GetParam(SafeHandle handle, uint param, ref uint bufferLength, byte* buffer)
            {
                IntPtr __handle_gen_native = default;
                uint __retVal;
                //
                // Setup
                //
                bool handle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    handle.DangerousAddRef(ref handle__addRefd);
                    __handle_gen_native = handle.DangerousGetHandle();
                    fixed (uint* __bufferLength_gen_native = &bufferLength)
                    {
                        __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, uint, uint*, byte*, uint>)_functionPointer)(__handle_gen_native, param, __bufferLength_gen_native, buffer);
                    }
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (handle__addRefd)
                        handle.DangerousRelease();
                }

                return __retVal;
            }
            internal uint RegistrationOpen(ref RegistrationConfig config, out SafeMsQuicRegistrationHandle registrationContext)
            {
                RegistrationConfig.Native __config_gen_native = default;
                registrationContext = default!;
                IntPtr __registrationContext_gen_native = default;
                uint __retVal;
                bool __invokeSucceeded = default;
                //
                // Setup
                //
                SafeMsQuicRegistrationHandle registrationContext__newHandle = new SafeMsQuicRegistrationHandle();
                try
                {
                    //
                    // Marshal
                    //
                    __config_gen_native = new RegistrationConfig.Native(config);
                    __retVal = ((delegate* unmanaged[Cdecl]<RegistrationConfig.Native*, IntPtr*, uint>)_functionPointer)(&__config_gen_native, &__registrationContext_gen_native);
                    __invokeSucceeded = true;
                    //
                    // Unmarshal
                    //
                    config = __config_gen_native.ToManaged();
                }
                finally
                {
                    if (__invokeSucceeded)
                    {
                        //
                        // GuaranteedUnmarshal
                        //
                        Marshal.InitHandle(registrationContext__newHandle, __registrationContext_gen_native);
                        registrationContext = registrationContext__newHandle;
                    }

                    //
                    // Cleanup
                    //
                    __config_gen_native.FreeNative();
                }

                return __retVal;
            }
            internal uint ConfigurationOpen(SafeMsQuicRegistrationHandle registrationContext, QuicBuffer* alpnBuffers, uint alpnBufferCount, ref QuicSettings settings, uint settingsSize, IntPtr context, out SafeMsQuicConfigurationHandle configuration)
            {
                IntPtr __registrationContext_gen_native = default;
                configuration = default!;
                IntPtr __configuration_gen_native = default;
                uint __retVal;
                bool __invokeSucceeded = default;
                //
                // Setup
                //
                bool registrationContext__addRefd = false;
                SafeMsQuicConfigurationHandle configuration__newHandle = new SafeMsQuicConfigurationHandle();
                try
                {
                    //
                    // Marshal
                    //
                    registrationContext.DangerousAddRef(ref registrationContext__addRefd);
                    __registrationContext_gen_native = registrationContext.DangerousGetHandle();
                    fixed (QuicSettings* __settings_gen_native = &settings)
                    {
                        __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QuicBuffer*, uint, QuicSettings*, uint, IntPtr, IntPtr*, uint>)_functionPointer)(__registrationContext_gen_native, alpnBuffers, alpnBufferCount, __settings_gen_native, settingsSize, context, &__configuration_gen_native);
                    }

                    __invokeSucceeded = true;
                }
                finally
                {
                    if (__invokeSucceeded)
                    {
                        //
                        // GuaranteedUnmarshal
                        //
                        Marshal.InitHandle(configuration__newHandle, __configuration_gen_native);
                        configuration = configuration__newHandle;
                    }

                    //
                    // Cleanup
                    //
                    if (registrationContext__addRefd)
                        registrationContext.DangerousRelease();
                }

                return __retVal;
            }
            internal uint ConfigurationLoadCredential(SafeMsQuicConfigurationHandle configuration, ref CredentialConfig credConfig)
            {
                CredentialConfig.Native __credConfig_gen_native = default;
                uint __retVal;
                //
                // Setup
                //
                bool configuration__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    configuration.DangerousAddRef(ref configuration__addRefd);
                    IntPtr __configuration_gen_native = configuration.DangerousGetHandle();
                    __credConfig_gen_native = new CredentialConfig.Native(credConfig);
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, CredentialConfig.Native*, uint>)_functionPointer)(__configuration_gen_native, &__credConfig_gen_native);
                    //
                    // Unmarshal
                    //
                    credConfig = __credConfig_gen_native.ToManaged();
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (configuration__addRefd)
                        configuration.DangerousRelease();
                    __credConfig_gen_native.FreeNative();
                }

                return __retVal;
            }
            internal uint ListenerOpen(SafeMsQuicRegistrationHandle registration, ListenerCallbackDelegate handler, IntPtr context, out SafeMsQuicListenerHandle listener)
            {
                IntPtr __handler_gen_native = default;
                listener = default!;
                IntPtr __listener_gen_native = default;
                uint __retVal;
                bool __invokeSucceeded = default;
                //
                // Setup
                //
                bool registration__addRefd = false;
                SafeMsQuicListenerHandle listener__newHandle = new SafeMsQuicListenerHandle();
                try
                {
                    //
                    // Marshal
                    //
                    registration.DangerousAddRef(ref registration__addRefd);
                    IntPtr __registration_gen_native = registration.DangerousGetHandle();
                    __handler_gen_native = handler != null ? Marshal.GetFunctionPointerForDelegate(handler) : default;
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr*, uint>)_functionPointer)(__registration_gen_native, __handler_gen_native, context, &__listener_gen_native);
                    __invokeSucceeded = true;
                    //
                    // KeepAlive
                    //
                    GC.KeepAlive(handler);
                }
                finally
                {
                    if (__invokeSucceeded)
                    {
                        //
                        // GuaranteedUnmarshal
                        //
                        Marshal.InitHandle(listener__newHandle, __listener_gen_native);
                        listener = listener__newHandle;
                    }

                    //
                    // Cleanup
                    //
                    if (registration__addRefd)
                        registration.DangerousRelease();
                }

                return __retVal;
            }
            internal uint ListenerStart(SafeMsQuicListenerHandle listener, QuicBuffer* alpnBuffers, uint alpnBufferCount, byte* localAddress)
            {
                IntPtr __listener_gen_native;
                uint __retVal;
                //
                // Setup
                //
                bool listener__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    listener.DangerousAddRef(ref listener__addRefd);
                    __listener_gen_native = listener.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QuicBuffer*, uint, byte*, uint>)_functionPointer)(__listener_gen_native, alpnBuffers, alpnBufferCount, localAddress);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (listener__addRefd)
                        listener.DangerousRelease();
                }

                return __retVal;
            }
            internal void ListenerStop(SafeMsQuicListenerHandle listener)
            {
                //
                // Setup
                //
                bool listener__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    listener.DangerousAddRef(ref listener__addRefd);
                    IntPtr __listener_gen_native = listener.DangerousGetHandle();
                    ((delegate* unmanaged[Cdecl]<IntPtr, void>)_functionPointer)(__listener_gen_native);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (listener__addRefd)
                        listener.DangerousRelease();
                }
            }
            internal uint ConnectionOpen(SafeMsQuicRegistrationHandle registration, ConnectionCallbackDelegate handler, IntPtr context, out SafeMsQuicConnectionHandle connection)
            {
                IntPtr __handler_gen_native = default;
                connection = default!;
                IntPtr __connection_gen_native = default;
                uint __retVal;
                bool __invokeSucceeded = default;
                //
                // Setup
                //
                bool registration__addRefd = false;
                SafeMsQuicConnectionHandle connection__newHandle = new SafeMsQuicConnectionHandle();
                try
                {
                    //
                    // Marshal
                    //
                    registration.DangerousAddRef(ref registration__addRefd);
                    IntPtr __registration_gen_native = registration.DangerousGetHandle();
                    __handler_gen_native = handler != null ? Marshal.GetFunctionPointerForDelegate(handler) : default;
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr*, uint>)_functionPointer)(__registration_gen_native, __handler_gen_native, context, &__connection_gen_native);
                    __invokeSucceeded = true;
                    //
                    // KeepAlive
                    //
                    GC.KeepAlive(handler);
                }
                finally
                {
                    if (__invokeSucceeded)
                    {
                        //
                        // GuaranteedUnmarshal
                        //
                        Marshal.InitHandle(connection__newHandle, __connection_gen_native);
                        connection = connection__newHandle;
                    }

                    //
                    // Cleanup
                    //
                    if (registration__addRefd)
                        registration.DangerousRelease();
                }

                return __retVal;
            }
            internal uint ConnectionSetConfiguration(SafeMsQuicConnectionHandle connection, SafeMsQuicConfigurationHandle configuration)
            {
                uint __retVal;
                //
                // Setup
                //
                bool connection__addRefd = false;
                bool configuration__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    connection.DangerousAddRef(ref connection__addRefd);
                    IntPtr __connection_gen_native = connection.DangerousGetHandle();
                    configuration.DangerousAddRef(ref configuration__addRefd);
                    IntPtr __configuration_gen_native = configuration.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint>)_functionPointer)(__connection_gen_native, __configuration_gen_native);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (connection__addRefd)
                        connection.DangerousRelease();
                    if (configuration__addRefd)
                        configuration.DangerousRelease();
                }

                return __retVal;
            }
            internal uint ConnectionStart(SafeMsQuicConnectionHandle connection, SafeMsQuicConfigurationHandle configuration, QUIC_ADDRESS_FAMILY family, string serverName, ushort serverPort)
            {
                IntPtr __connection_gen_native = default;
                IntPtr __configuration_gen_native = default;
                byte* __serverName_gen_native = default;
                uint __retVal;
                //
                // Setup
                //
                bool connection__addRefd = false;
                bool configuration__addRefd = false;
                bool __serverName_gen_native__allocated = false;
                try
                {
                    //
                    // Marshal
                    //
                    connection.DangerousAddRef(ref connection__addRefd);
                    __connection_gen_native = connection.DangerousGetHandle();
                    configuration.DangerousAddRef(ref configuration__addRefd);
                    __configuration_gen_native = configuration.DangerousGetHandle();
                    if (serverName != null)
                    {
                        int __serverName_gen_native__bytelen = (serverName.Length + 1) * 3 + 1;
                        if (__serverName_gen_native__bytelen > 260)
                        {
                            __serverName_gen_native = (byte*)Marshal.StringToCoTaskMemUTF8(serverName);
                            __serverName_gen_native__allocated = true;
                        }
                        else
                        {
                            byte* __serverName_gen_native__stackptr = stackalloc byte[__serverName_gen_native__bytelen];
                            {
                                __serverName_gen_native__bytelen = Text.Encoding.UTF8.GetBytes(serverName, new Span<byte>(__serverName_gen_native__stackptr, __serverName_gen_native__bytelen));
                                __serverName_gen_native__stackptr[__serverName_gen_native__bytelen] = 0;
                            }

                            __serverName_gen_native = (byte*)__serverName_gen_native__stackptr;
                        }
                    }

                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, QUIC_ADDRESS_FAMILY, byte*, ushort, uint>)_functionPointer)(__connection_gen_native, __configuration_gen_native, family, __serverName_gen_native, serverPort);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (connection__addRefd)
                        connection.DangerousRelease();
                    if (configuration__addRefd)
                        configuration.DangerousRelease();
                    if (__serverName_gen_native__allocated)
                    {
                        Marshal.FreeCoTaskMem((IntPtr)__serverName_gen_native);
                    }
                }

                return __retVal;
            }
            internal void ConnectionShutdown(SafeMsQuicConnectionHandle connection, QUIC_CONNECTION_SHUTDOWN_FLAGS flags, long errorCode)
            {
                //
                // Setup
                //
                bool connection__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    connection.DangerousAddRef(ref connection__addRefd);
                    IntPtr __connection_gen_native = connection.DangerousGetHandle();
                    ((delegate* unmanaged[Cdecl]<IntPtr, QUIC_CONNECTION_SHUTDOWN_FLAGS, long, void>)_functionPointer)(__connection_gen_native, flags, errorCode);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (connection__addRefd)
                        connection.DangerousRelease();
                }
            }
            internal uint StreamOpen(SafeMsQuicConnectionHandle connection, QUIC_STREAM_OPEN_FLAGS flags, StreamCallbackDelegate handler, IntPtr context, out SafeMsQuicStreamHandle stream)
            {
                IntPtr __handler_gen_native = default;
                stream = default!;
                IntPtr __stream_gen_native = default;
                uint __retVal;
                bool __invokeSucceeded = default;
                //
                // Setup
                //
                bool connection__addRefd = false;
                SafeMsQuicStreamHandle stream__newHandle = new SafeMsQuicStreamHandle();
                try
                {
                    //
                    // Marshal
                    //
                    connection.DangerousAddRef(ref connection__addRefd);
                    IntPtr __connection_gen_native = connection.DangerousGetHandle();
                    __handler_gen_native = handler != null ? Marshal.GetFunctionPointerForDelegate(handler) : default;
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QUIC_STREAM_OPEN_FLAGS, IntPtr, IntPtr, IntPtr*, uint>)_functionPointer)(__connection_gen_native, flags, __handler_gen_native, context, &__stream_gen_native);
                    __invokeSucceeded = true;
                    //
                    // KeepAlive
                    //
                    GC.KeepAlive(handler);
                }
                finally
                {
                    if (__invokeSucceeded)
                    {
                        //
                        // GuaranteedUnmarshal
                        //
                        Marshal.InitHandle(stream__newHandle, __stream_gen_native);
                        stream = stream__newHandle;
                    }

                    //
                    // Cleanup
                    //
                    if (connection__addRefd)
                        connection.DangerousRelease();
                }

                return __retVal;
            }
            internal uint StreamStart(SafeMsQuicStreamHandle stream, QUIC_STREAM_START_FLAGS flags)
            {
                uint __retVal;
                //
                // Setup
                //
                bool stream__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    stream.DangerousAddRef(ref stream__addRefd);
                    IntPtr __stream_gen_native = stream.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QUIC_STREAM_START_FLAGS, uint>)_functionPointer)(__stream_gen_native, flags);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (stream__addRefd)
                        stream.DangerousRelease();
                }

                return __retVal;
            }
            internal uint StreamShutdown(SafeMsQuicStreamHandle stream, QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
            {
                uint __retVal;
                //
                // Setup
                //
                bool stream__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    stream.DangerousAddRef(ref stream__addRefd);
                    IntPtr __stream_gen_native = stream.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QUIC_STREAM_SHUTDOWN_FLAGS, long, uint>)_functionPointer)(__stream_gen_native, flags, errorCode);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (stream__addRefd)
                        stream.DangerousRelease();
                }

                return __retVal;
            }
            internal uint StreamSend(SafeMsQuicStreamHandle stream, QuicBuffer* buffers, uint bufferCount, QUIC_SEND_FLAGS flags, IntPtr clientSendContext)
            {
                uint __retVal;
                //
                // Setup
                //
                bool stream__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    stream.DangerousAddRef(ref stream__addRefd);
                    IntPtr __stream_gen_native = stream.DangerousGetHandle();
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, QuicBuffer*, uint, QUIC_SEND_FLAGS, IntPtr, uint>)_functionPointer)(__stream_gen_native, buffers, bufferCount, flags, clientSendContext);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (stream__addRefd)
                        stream.DangerousRelease();
                }

                return __retVal;
            }
            internal void StreamReceiveComplete(SafeMsQuicStreamHandle stream, ulong bufferLength)
            {
                //
                // Setup
                //
                bool stream__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    stream.DangerousAddRef(ref stream__addRefd);
                    IntPtr __stream_gen_native = stream.DangerousGetHandle();
                    ((delegate* unmanaged[Cdecl]<IntPtr, ulong, void>)_functionPointer)(__stream_gen_native, bufferLength);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (stream__addRefd)
                        stream.DangerousRelease();
                }
            }
            internal uint StreamReceiveSetEnabled(SafeMsQuicStreamHandle stream, bool enabled)
            {
                uint __retVal;
                //
                // Setup
                //
                bool stream__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    stream.DangerousAddRef(ref stream__addRefd);
                    IntPtr __stream_gen_native = stream.DangerousGetHandle();
                    byte __enabled_gen_native = (byte)(enabled ? 1 : 0);
                    __retVal = ((delegate* unmanaged[Cdecl]<IntPtr, byte, uint>)_functionPointer)(__stream_gen_native, __enabled_gen_native);
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (stream__addRefd)
                        stream.DangerousRelease();
                }

                return __retVal;
            }
        }
    }
}
