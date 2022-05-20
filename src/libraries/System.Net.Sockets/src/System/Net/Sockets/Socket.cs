// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Internals;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets
{
    // The Sockets.Socket class implements the Berkeley sockets interface.
    public partial class Socket : IDisposable
    {
        internal const int DefaultCloseTimeout = -1; // NOTE: changing this default is a breaking change.

        private static readonly IPAddress s_IPAddressAnyMapToIPv6 = IPAddress.Any.MapToIPv6();

        private SafeSocketHandle _handle;

        // _rightEndPoint is null if the socket has not been bound.  Otherwise, it is an EndPoint of the
        // correct type (IPEndPoint, etc). The Bind operation sets _rightEndPoint. Other operations must only set
        // it when the value is still null.
        // This enables tracking the file created by UnixDomainSocketEndPoint when the Socket is bound,
        // and to delete that file when the Socket gets disposed.
        internal EndPoint? _rightEndPoint;
        internal EndPoint? _remoteEndPoint;

        // Cached LocalEndPoint value. Cleared on disconnect and error. Cached wildcard addresses are
        // also cleared on connect and accept.
        private EndPoint? _localEndPoint;

        // These flags monitor if the socket was ever connected at any time and if it still is.
        private bool _isConnected;
        private bool _isDisconnected;

        // When the socket is created it will be in blocking mode. We'll only be able to Accept or Connect,
        // so we need to handle one of these cases at a time.
        private bool _willBlock = true; // Desired state of the socket from the user.
        private bool _willBlockInternal = true; // Actual win32 state of the socket.
        private bool _isListening;

        // Our internal state doesn't automatically get updated after a non-blocking connect
        // completes.  Keep track of whether we're doing a non-blocking connect, and make sure
        // to poll for the real state until we're done connecting.
        private bool _nonBlockingConnectInProgress;

        // Keep track of the kind of endpoint used to do a connect, so we can set
        // it to _rightEndPoint when we're connected.
        private EndPoint? _pendingConnectRightEndPoint;

        // These are constants initialized by constructor.
        private AddressFamily _addressFamily;
        private SocketType _socketType;
        private ProtocolType _protocolType;

        // Bool marked true if the native socket option IP_PKTINFO or IPV6_PKTINFO has been set.
        private bool _receivingPacketInformation;

        private int _closeTimeout = Socket.DefaultCloseTimeout;
        private int _disposed; // 0 == false, anything else == true

        public Socket(SocketType socketType, ProtocolType protocolType)
            : this(OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, socketType, protocolType)
        {
            if (OSSupportsIPv6)
            {
                DualMode = true;
            }
        }

        // Initializes a new instance of the Sockets.Socket class.
        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, addressFamily);

            SocketError errorCode = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
            if (errorCode != SocketError.Success)
            {
                Debug.Assert(_handle.IsInvalid);

                // Failed to create the socket, throw.
                throw new SocketException((int)errorCode);
            }

            Debug.Assert(!_handle.IsInvalid);

            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;

        }

        /// <summary>Initializes a new instance of the <see cref="Socket"/> class for the specified socket handle.</summary>
        /// <param name="handle">The socket handle for the socket that the <see cref="Socket"/> object will encapsulate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="handle"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="handle"/> is invalid.</exception>
        /// <exception cref="SocketException"><paramref name="handle"/> is not a socket or information about the socket could not be accessed.</exception>
        /// <remarks>
        /// This method populates the <see cref="Socket"/> instance with data gathered from the supplied <see cref="SafeSocketHandle"/>.
        /// Different operating systems provide varying levels of support for querying a socket handle or file descriptor for its
        /// properties and configuration, which means some of the public APIs on the resulting <see cref="Socket"/> instance may
        /// differ based on operating system, such as <see cref="Socket.ProtocolType"/> and <see cref="Socket.Blocking"/>.
        /// </remarks>
        public Socket(SafeSocketHandle handle) :
            this(ValidateHandle(handle), loadPropertiesFromHandle: true)
        {
        }

        private unsafe Socket(SafeSocketHandle handle, bool loadPropertiesFromHandle)
        {
            _handle = handle;
            _addressFamily = AddressFamily.Unknown;
            _socketType = SocketType.Unknown;
            _protocolType = ProtocolType.Unknown;

            if (!loadPropertiesFromHandle)
            {
                return;
            }

            try
            {
                // Get properties like address family and blocking mode from the OS.
                LoadSocketTypeFromHandle(handle, out _addressFamily, out _socketType, out _protocolType, out _willBlockInternal, out _isListening, out bool isSocket);

                if (isSocket)
                {
                    // We should change stackalloc if this ever grows too big.
                    Debug.Assert(SocketPal.MaximumAddressSize <= 512);
                    // Try to get the address of the socket.
                    Span<byte> buffer = stackalloc byte[SocketPal.MaximumAddressSize];
                    int bufferLength = buffer.Length;
                    fixed (byte* bufferPtr = buffer)
                    {
                        if (SocketPal.GetSockName(handle, bufferPtr, &bufferLength) != SocketError.Success)
                        {
                            return;
                        }
                    }

                    Debug.Assert(bufferLength <= buffer.Length);

                    // Try to get the local end point.  That will in turn enable the remote
                    // end point to be retrieved on-demand when the property is accessed.
                    Internals.SocketAddress? socketAddress = null;
                    switch (_addressFamily)
                    {
                        case AddressFamily.InterNetwork:
                            _rightEndPoint = new IPEndPoint(
                                new IPAddress((long)SocketAddressPal.GetIPv4Address(buffer.Slice(0, bufferLength)) & 0x0FFFFFFFF),
                                SocketAddressPal.GetPort(buffer));
                            break;

                        case AddressFamily.InterNetworkV6:
                            Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                            SocketAddressPal.GetIPv6Address(buffer.Slice(0, bufferLength), address, out uint scope);
                            _rightEndPoint = new IPEndPoint(
                                new IPAddress(address, scope),
                                SocketAddressPal.GetPort(buffer));
                            break;

                        case AddressFamily.Unix:
                            socketAddress = new Internals.SocketAddress(_addressFamily, buffer.Slice(0, bufferLength));
                            _rightEndPoint = new UnixDomainSocketEndPoint(IPEndPointExtensions.GetNetSocketAddress(socketAddress));
                            break;
                    }

                    // Try to determine if we're connected, based on querying for a peer, just as we would in RemoteEndPoint,
                    // but ignoring any failures; this is best-effort (RemoteEndPoint also does a catch-all around the Create call).
                    if (_rightEndPoint != null)
                    {
                        try
                        {
                            // Local and remote end points may be different sizes for protocols like Unix Domain Sockets.
                            bufferLength = buffer.Length;
                            switch (SocketPal.GetPeerName(handle, buffer, ref bufferLength))
                            {
                                case SocketError.Success:
                                    switch (_addressFamily)
                                    {
                                        case AddressFamily.InterNetwork:
                                            _remoteEndPoint = new IPEndPoint(
                                                new IPAddress((long)SocketAddressPal.GetIPv4Address(buffer.Slice(0, bufferLength)) & 0x0FFFFFFFF),
                                                SocketAddressPal.GetPort(buffer));
                                            break;

                                        case AddressFamily.InterNetworkV6:
                                            Span<byte> address = stackalloc byte[IPAddressParserStatics.IPv6AddressBytes];
                                            SocketAddressPal.GetIPv6Address(buffer.Slice(0, bufferLength), address, out uint scope);
                                            _remoteEndPoint = new IPEndPoint(
                                                new IPAddress(address, scope),
                                                SocketAddressPal.GetPort(buffer));
                                            break;

                                        case AddressFamily.Unix:
                                            socketAddress = new Internals.SocketAddress(_addressFamily, buffer.Slice(0, bufferLength));
                                            _remoteEndPoint = new UnixDomainSocketEndPoint(IPEndPointExtensions.GetNetSocketAddress(socketAddress));
                                            break;
                                    }

                                    _isConnected = true;
                                    break;

                                case SocketError.InvalidArgument:
                                    // On some OSes (e.g. macOS), EINVAL means the socket has been shut down.
                                    // This can happen if, for example, socketpair was used and the parent
                                    // process closed its copy of the child's socket.  Since we don't know
                                    // whether we're actually connected or not, err on the side of saying
                                    // we're connected.
                                    _isConnected = true;
                                    break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                _handle = null!;
                GC.SuppressFinalize(this);
                throw;
            }
        }

        private static SafeSocketHandle ValidateHandle(SafeSocketHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);

            if (handle.IsInvalid)
            {
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));
            }

            return handle;
        }

        //
        // Properties
        //

        // The CLR allows configuration of these properties, separately from whether the OS supports IPv4/6.  We
        // do not provide these config options, so SupportsIPvX === OSSupportsIPvX.
        [Obsolete("SupportsIPv4 has been deprecated. Use OSSupportsIPv4 instead.")]
        public static bool SupportsIPv4 => OSSupportsIPv4;
        [Obsolete("SupportsIPv6 has been deprecated. Use OSSupportsIPv6 instead.")]
        public static bool SupportsIPv6 => OSSupportsIPv6;

        public static bool OSSupportsIPv4 => SocketProtocolSupportPal.OSSupportsIPv4;
        public static bool OSSupportsIPv6 => SocketProtocolSupportPal.OSSupportsIPv6;
        public static bool OSSupportsUnixDomainSockets => SocketProtocolSupportPal.OSSupportsUnixDomainSockets;

        // Gets the amount of data pending in the network's input buffer that can be
        // read from the socket.
        public int Available
        {
            get
            {
                ThrowIfDisposed();

                int argp;

                // This may throw ObjectDisposedException.
                SocketError errorCode = SocketPal.GetAvailable(_handle, out argp);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetAvailable returns errorCode:{errorCode}");

                // Throw an appropriate SocketException if the native call fails.
                if (errorCode != SocketError.Success)
                {
                    UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                }

                return argp;
            }
        }

        // Gets the local end point.
        public EndPoint? LocalEndPoint
        {
            get
            {
                ThrowIfDisposed();

                CheckNonBlockingConnectCompleted();

                if (_rightEndPoint == null)
                {
                    return null;
                }

                if (_localEndPoint == null)
                {
                    Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(_rightEndPoint);

                    unsafe
                    {
                        fixed (byte* buffer = socketAddress.Buffer)
                        fixed (int* bufferSize = &socketAddress.InternalSize)
                        {
                            // This may throw ObjectDisposedException.
                            SocketError errorCode = SocketPal.GetSockName(_handle, buffer, bufferSize);
                            if (errorCode != SocketError.Success)
                            {
                                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                            }
                        }
                    }
                    _localEndPoint = _rightEndPoint.Create(socketAddress);
                }

                return _localEndPoint;
            }
        }

        // Gets the remote end point.
        public EndPoint? RemoteEndPoint
        {
            get
            {
                ThrowIfDisposed();

                if (_remoteEndPoint == null)
                {
                    CheckNonBlockingConnectCompleted();

                    if (_rightEndPoint == null || !_isConnected)
                    {
                        return null;
                    }

                    Internals.SocketAddress socketAddress =
                        _addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6 ?
                            IPEndPointExtensions.Serialize(_rightEndPoint) :
                            new Internals.SocketAddress(_addressFamily, SocketPal.MaximumAddressSize); // may be different size than _rightEndPoint.

                    // This may throw ObjectDisposedException.
                    SocketError errorCode = SocketPal.GetPeerName(
                        _handle,
                        socketAddress.Buffer,
                        ref socketAddress.InternalSize);

                    if (errorCode != SocketError.Success)
                    {
                        UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                    }

                    try
                    {
                        _remoteEndPoint = _rightEndPoint.Create(socketAddress);
                    }
                    catch
                    {
                    }
                }

                return _remoteEndPoint;
            }
        }

        public IntPtr Handle => SafeHandle.DangerousGetHandle();

        public SafeSocketHandle SafeHandle
        {
            get
            {
                _handle.SetExposed();
                return _handle;
            }
        }

        internal SafeSocketHandle InternalSafeHandle => _handle; // returns _handle without calling SetExposed.

        // Gets and sets the blocking mode of a socket.
        public bool Blocking
        {
            get
            {
                // Return the user's desired blocking behaviour (not the actual win32 state).
                return _willBlock;
            }
            set
            {
                ThrowIfDisposed();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"value:{value} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}");

                bool current;

                SocketError errorCode = InternalSetBlocking(value, out current);

                if (errorCode != SocketError.Success)
                {
                    UpdateStatusAfterSocketErrorAndThrowException(errorCode);
                }

                // The native call succeeded, update the user's desired state.
                _willBlock = current;
            }
        }

        // On .NET Framework, this property functions as a socket-level switch between IOCP-based and Win32 event based async IO.
        // On that platform, setting UseOnlyOverlappedIO = true prevents assigning a completion port to the socket,
        // allowing calls to DuplicateAndClose() even after performing asynchronous IO.
        // .NET (Core) Windows sockets are entirely IOCP-based, and the concept of "overlapped IO"
        // does not exist on other platforms, therefore UseOnlyOverlappedIO is a dummy, compat-only property.
        [Obsolete("UseOnlyOverlappedIO has been deprecated and is not supported.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool UseOnlyOverlappedIO
        {
            get { return false; }
            set { }
        }

        // Gets the connection state of the Socket. This property will return the latest
        // known state of the Socket. When it returns false, the Socket was either never connected
        // or it is not connected anymore. When it returns true, though, there's no guarantee that the Socket
        // is still connected, but only that it was connected at the time of the last IO operation.
        public bool Connected
        {
            get
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_isConnected:{_isConnected}");

                CheckNonBlockingConnectCompleted();

                return _isConnected;
            }
        }

        // Gets the socket's address family.
        public AddressFamily AddressFamily
        {
            get
            {
                return _addressFamily;
            }
        }

        // Gets the socket's socketType.
        public SocketType SocketType
        {
            get
            {
                return _socketType;
            }
        }

        // Gets the socket's protocol socketType.
        public ProtocolType ProtocolType
        {
            get
            {
                return _protocolType;
            }
        }

        public bool IsBound
        {
            get
            {
                return (_rightEndPoint != null);
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse)! != 0 ? true : false;
            }
            set
            {
                if (IsBound)
                {
                    throw new InvalidOperationException(SR.net_sockets_mustnotbebound);
                }
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, value ? 1 : 0);
            }
        }

        public int ReceiveBufferSize
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer)!;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
            }
        }

        public int SendBufferSize
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer)!;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout)!;
            }
            set
            {
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (value == -1)
                {
                    value = 0;
                }

                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
            }
        }

        public int SendTimeout
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout)!;
            }

            set
            {
                if (value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (value == -1)
                {
                    value = 0;
                }

                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
            }
        }

        [DisallowNull]
        public LingerOption? LingerState
        {
            get
            {
                return (LingerOption?)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger);
            }
            set
            {
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, value!);
            }
        }

        public bool NoDelay
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay)! != 0 ? true : false;
            }
            set
            {
                SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
            }
        }

        public short Ttl
        {
            get
            {
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    return (short)(int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive)!;
                }
                else if (_addressFamily == AddressFamily.InterNetworkV6)
                {
                    return (short)(int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IpTimeToLive)!;
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }

            set
            {
                // Valid values are from 0 to 255 since TTL is really just a byte value on the wire.
                if (value < 0 || value > 255)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, value);
                }

                else if (_addressFamily == AddressFamily.InterNetworkV6)
                {
                    SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IpTimeToLive, value);
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }
        }

        public bool DontFragment
        {
            get
            {
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    return (int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment)! != 0 ? true : false;
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }

            set
            {
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, value ? 1 : 0);
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }
        }

        public bool MulticastLoopback
        {
            get
            {
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    return (int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback)! != 0 ? true : false;
                }
                else if (_addressFamily == AddressFamily.InterNetworkV6)
                {
                    return (int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback)! != 0 ? true : false;
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }

            set
            {
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, value ? 1 : 0);
                }

                else if (_addressFamily == AddressFamily.InterNetworkV6)
                {
                    SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, value ? 1 : 0);
                }
                else
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
            }
        }

        public bool EnableBroadcast
        {
            get
            {
                return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast)! != 0 ? true : false;
            }
            set
            {
                SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, value ? 1 : 0);
            }
        }

        // NOTE: on *nix, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. This can cause behavioral differences in code that checks
        //       the value of DualMode (e.g. the checks in CanTryAddressFamily).
        public bool DualMode
        {
            get
            {
                if (AddressFamily != AddressFamily.InterNetworkV6)
                {
                    return false;
                }
                return ((int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only)! == 0);
            }
            set
            {
                if (AddressFamily != AddressFamily.InterNetworkV6)
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }
                SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, value ? 0 : 1);
            }
        }

        private bool IsDualMode
        {
            get
            {
                return AddressFamily == AddressFamily.InterNetworkV6 && DualMode;
            }
        }

        internal bool CanTryAddressFamily(AddressFamily family)
        {
            return (family == _addressFamily) || (family == AddressFamily.InterNetwork && IsDualMode);
        }

        //
        // Public Methods
        //

        // Associates a socket with an end point.
        public void Bind(EndPoint localEP)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, localEP);
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(localEP);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"localEP:{localEP}");

            Internals.SocketAddress socketAddress = Serialize(ref localEP);
            DoBind(localEP, socketAddress);
        }

        private void DoBind(EndPoint endPointSnapshot, Internals.SocketAddress socketAddress)
        {
            // Mitigation for Blue Screen of Death (Win7, maybe others).
            IPEndPoint? ipEndPoint = endPointSnapshot as IPEndPoint;
            if (!OSSupportsIPv4 && ipEndPoint != null && ipEndPoint.Address.IsIPv4MappedToIPv6)
            {
                UpdateStatusAfterSocketErrorAndThrowException(SocketError.InvalidArgument);
            }

            // This may throw ObjectDisposedException.
            SocketError errorCode = SocketPal.Bind(
                _handle,
                _protocolType,
                socketAddress.Buffer,
                socketAddress.Size);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            // Save a copy of the EndPoint so we can use it for Create().
            // For UnixDomainSocketEndPoint, track the file to delete on Dispose.
            _rightEndPoint = endPointSnapshot is UnixDomainSocketEndPoint unixEndPoint ?
                                unixEndPoint.CreateBoundEndPoint() :
                                endPointSnapshot;
        }

        // Establishes a connection to a remote system.
        public void Connect(EndPoint remoteEP)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remoteEP);

            if (_isDisconnected)
            {
                throw new InvalidOperationException(SR.net_sockets_disconnectedConnect);
            }

            if (_isListening)
            {
                throw new InvalidOperationException(SR.net_sockets_mustnotlisten);
            }

            if (_isConnected)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            ValidateBlockingMode();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"DST:{remoteEP}");

            DnsEndPoint? dnsEP = remoteEP as DnsEndPoint;
            if (dnsEP != null)
            {
                ValidateForMultiConnect(isMultiEndpoint: true); // needs to come before CanTryAddressFamily call

                if (dnsEP.AddressFamily != AddressFamily.Unspecified && !CanTryAddressFamily(dnsEP.AddressFamily))
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }

                Connect(dnsEP.Host, dnsEP.Port);
                return;
            }

            ValidateForMultiConnect(isMultiEndpoint: false);

            Internals.SocketAddress socketAddress = Serialize(ref remoteEP);
            _pendingConnectRightEndPoint = remoteEP;
            _nonBlockingConnectInProgress = !Blocking;

            DoConnect(remoteEP, socketAddress);
        }

        public void Connect(IPAddress address, int port)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(address);

            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            if (_isConnected)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            ValidateForMultiConnect(isMultiEndpoint: false); // needs to come before CanTryAddressFamily call

            if (!CanTryAddressFamily(address.AddressFamily))
            {
                throw new NotSupportedException(SR.net_invalidversion);
            }

            IPEndPoint remoteEP = new IPEndPoint(address, port);
            Connect(remoteEP);
        }

        public void Connect(string host, int port)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(host);

            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
            {
                throw new NotSupportedException(SR.net_invalidversion);
            }

            // No need to call ValidateForMultiConnect(), as the validation
            // will be handled by the delegated Connect overloads.

            IPAddress? parsedAddress;
            if (IPAddress.TryParse(host, out parsedAddress))
            {
                Connect(parsedAddress, port);
            }
            else
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                Connect(addresses, port);
            }
        }

        public void Connect(IPAddress[] addresses, int port)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(addresses);

            if (addresses.Length == 0)
            {
                throw new ArgumentException(SR.net_sockets_invalid_ipaddress_length, nameof(addresses));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
            {
                throw new NotSupportedException(SR.net_invalidversion);
            }

            if (_isConnected)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            ValidateForMultiConnect(isMultiEndpoint: true); // needs to come before CanTryAddressFamily call

            ExceptionDispatchInfo? lastex = null;
            foreach (IPAddress address in addresses)
            {
                if (CanTryAddressFamily(address.AddressFamily))
                {
                    try
                    {
                        Connect(new IPEndPoint(address, port));
                        lastex = null;
                        break;
                    }
                    catch (Exception ex) when (!ExceptionCheck.IsFatal(ex))
                    {
                        lastex = ExceptionDispatchInfo.Capture(ex);
                    }
                }
            }

            lastex?.Throw();

            // If we're not connected, then we didn't get a valid ipaddress in the list.
            if (!Connected)
            {
                throw new ArgumentException(SR.net_invalidAddressList, nameof(addresses));
            }
        }

        public void Close()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"timeout = {_closeTimeout}");
            Dispose();
        }

        public void Close(int timeout)
        {
            if (timeout < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            _closeTimeout = timeout;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"timeout = {_closeTimeout}");

            Dispose();
        }

        /// <summary>
        /// Places a <see cref="Socket"/> in a listening state.
        /// </summary>
        /// <remarks>
        /// The maximum length of the pending connections queue will be determined automatically.
        /// </remarks>
        public void Listen() => Listen(int.MaxValue);

        /// <summary>
        /// Places a <see cref="Socket"/> in a listening state.
        /// </summary>
        /// <param name="backlog">The maximum length of the pending connections queue.</param>
        public void Listen(int backlog)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, backlog);
            ThrowIfDisposed();

            // This may throw ObjectDisposedException.
            SocketError errorCode = SocketPal.Listen(_handle, backlog);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            _isListening = true;
        }

        // Creates a new Sockets.Socket instance to handle an incoming connection.
        public Socket Accept()
        {
            ThrowIfDisposed();

            if (_rightEndPoint == null)
            {
                throw new InvalidOperationException(SR.net_sockets_mustbind);
            }

            if (!_isListening)
            {
                throw new InvalidOperationException(SR.net_sockets_mustlisten);
            }

            if (_isDisconnected)
            {
                throw new InvalidOperationException(SR.net_sockets_disconnectedAccept);
            }

            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint}");

            Internals.SocketAddress socketAddress =
                _addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6 ?
                    IPEndPointExtensions.Serialize(_rightEndPoint) :
                    new Internals.SocketAddress(_addressFamily, SocketPal.MaximumAddressSize); // may be different size.

            if (SocketsTelemetry.Log.IsEnabled()) SocketsTelemetry.Log.AcceptStart(socketAddress);

            // This may throw ObjectDisposedException.
            SafeSocketHandle acceptedSocketHandle;
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.Accept(
                    _handle,
                    socketAddress.Buffer,
                    ref socketAddress.InternalSize,
                    out acceptedSocketHandle);
            }
            catch (Exception ex)
            {
                SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);
                throw;
            }

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                Debug.Assert(acceptedSocketHandle.IsInvalid);
                UpdateAcceptSocketErrorForDisposed(ref errorCode);

                SocketsTelemetry.Log.AfterAccept(errorCode);

                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            SocketsTelemetry.Log.AfterAccept(SocketError.Success);

            Debug.Assert(!acceptedSocketHandle.IsInvalid);

            Socket socket = CreateAcceptSocket(acceptedSocketHandle, _rightEndPoint.Create(socketAddress));
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Accepted(socket, socket.RemoteEndPoint!, socket.LocalEndPoint);
            return socket;
        }

        // Sends a data buffer to a connected socket.
        public int Send(byte[] buffer, int size, SocketFlags socketFlags)
        {
            return Send(buffer, 0, size, socketFlags);
        }

        public int Send(byte[] buffer, SocketFlags socketFlags)
        {
            return Send(buffer, 0, buffer != null ? buffer.Length : 0, socketFlags);
        }

        public int Send(byte[] buffer)
        {
            return Send(buffer, 0, buffer != null ? buffer.Length : 0, SocketFlags.None);
        }

        public int Send(IList<ArraySegment<byte>> buffers)
        {
            return Send(buffers, SocketFlags.None);
        }

        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            SocketError errorCode;
            int bytesTransferred = Send(buffers, socketFlags, out errorCode);
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
            return bytesTransferred;
        }

        public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(buffers);

            if (buffers.Count == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_sockets_zerolist, nameof(buffers)), nameof(buffers));
            }

            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}");

            int bytesTransferred;
            errorCode = SocketPal.Send(_handle, buffers, socketFlags, out bytesTransferred);

            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                // Update the internal state of this socket according to the error before throwing.
                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                // Don't log transfered byte count in case of a failure.
                return 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesSent(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
            }

            return bytesTransferred;
        }

        // Sends data to a connected socket, starting at the indicated location in the buffer.
        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            SocketError errorCode;
            int bytesTransferred = Send(buffer, offset, size, socketFlags, out errorCode);
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
            return bytesTransferred;
        }

        public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();

            ValidateBufferArguments(buffer, offset, size);

            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}");

            int bytesTransferred;
            errorCode = SocketPal.Send(_handle, buffer, offset, size, socketFlags, out bytesTransferred);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                // Update the internal state of this socket according to the error before throwing.
                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                return 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesSent(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, $"Send returns:{bytesTransferred}");
                NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred);
            }

            return bytesTransferred;
        }

        public int Send(ReadOnlySpan<byte> buffer) => Send(buffer, SocketFlags.None);

        public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags)
        {
            int bytesTransferred = Send(buffer, socketFlags, out SocketError errorCode);
            return errorCode == SocketError.Success ?
                bytesTransferred :
                throw new SocketException((int)errorCode);
        }

        public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();
            ValidateBlockingMode();

            int bytesTransferred;
            errorCode = SocketPal.Send(_handle, buffer, socketFlags, out bytesTransferred);

            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                bytesTransferred = 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesSent(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
            }

            return bytesTransferred;
        }

        public void SendFile(string? fileName)
        {
            SendFile(fileName, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread);
        }

        /// <summary>
        /// Sends the file <paramref name="fileName"/> and buffers of data to a connected <see cref="Socket"/> object
        /// using the specified <see cref="TransmitFileOptions"/> value.
        /// </summary>
        /// <param name="fileName">
        /// A <see cref="string"/> that contains the path and name of the file to be sent. This parameter can be <see langword="null"/>.
        /// </param>
        /// <param name="preBuffer">
        /// A <see cref="byte"/> array that contains data to be sent before the file is sent. This parameter can be <see langword="null"/>.
        /// </param>
        /// <param name="postBuffer">
        /// A <see cref="byte"/> array that contains data to be sent after the file is sent. This parameter can be <see langword="null"/>.
        /// </param>
        /// <param name="flags">
        /// One or more of <see cref="TransmitFileOptions"/> values.
        /// </param>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> object has been closed.</exception>
        /// <exception cref="NotSupportedException">The <see cref="Socket"/> object is not connected to a remote host.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="Socket"/> object is not in blocking mode and cannot accept this synchronous call.</exception>
        /// <exception cref="FileNotFoundException">The file <paramref name="fileName"/> was not found.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        public void SendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags)
        {
            SendFile(fileName, preBuffer.AsSpan(), postBuffer.AsSpan(), flags);
        }

        /// <summary>
        /// Sends the file <paramref name="fileName"/> and buffers of data to a connected <see cref="Socket"/> object
        /// using the specified <see cref="TransmitFileOptions"/> value.
        /// </summary>
        /// <param name="fileName">
        /// A <see cref="string"/> that contains the path and name of the file to be sent. This parameter can be <see langword="null"/>.
        /// </param>
        /// <param name="preBuffer">
        /// A <see cref="ReadOnlySpan{T}"/> that contains data to be sent before the file is sent. This buffer can be empty.
        /// </param>
        /// <param name="postBuffer">
        /// A <see cref="ReadOnlySpan{T}"/> that contains data to be sent after the file is sent. This buffer can be empty.
        /// </param>
        /// <param name="flags">
        /// One or more of <see cref="TransmitFileOptions"/> values.
        /// </param>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> object has been closed.</exception>
        /// <exception cref="NotSupportedException">The <see cref="Socket"/> object is not connected to a remote host.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="Socket"/> object is not in blocking mode and cannot accept this synchronous call.</exception>
        /// <exception cref="FileNotFoundException">The file <paramref name="fileName"/> was not found.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        public void SendFile(string? fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
        {
            ThrowIfDisposed();

            if (!Connected)
            {
                throw new NotSupportedException(SR.net_notconnected);
            }

            ValidateBlockingMode();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"::SendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}");

            SendFileInternal(fileName, preBuffer, postBuffer, flags);
        }

        // Sends data to a specific end point, starting at the indicated location in the buffer.
        public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
        {
            ThrowIfDisposed();

            ValidateBufferArguments(buffer, offset, size);
            ArgumentNullException.ThrowIfNull(remoteEP);

            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint} size:{size} remoteEP:{remoteEP}");

            Internals.SocketAddress socketAddress = Serialize(ref remoteEP);

            int bytesTransferred;
            SocketError errorCode = SocketPal.SendTo(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer, socketAddress.Size, out bytesTransferred);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesSent(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
            }

            // Save a copy of the EndPoint so we can use it for Create().
            _rightEndPoint ??= remoteEP;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.DumpBuffer(this, buffer, offset, size);
            return bytesTransferred;
        }

        // Sends data to a specific end point, starting at the indicated location in the data.
        public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP)
        {
            return SendTo(buffer, 0, size, socketFlags, remoteEP);
        }

        public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP)
        {
            return SendTo(buffer, 0, buffer != null ? buffer.Length : 0, socketFlags, remoteEP);
        }

        public int SendTo(byte[] buffer, EndPoint remoteEP)
        {
            return SendTo(buffer, 0, buffer != null ? buffer.Length : 0, SocketFlags.None, remoteEP);
        }

        /// <summary>
        /// Sends data to the specified endpoint.
        /// </summary>
        /// <param name="buffer">A span of bytes that contains the data to be sent.</param>
        /// <param name="remoteEP">The <see cref="EndPoint"/> that represents the destination for the data.</param>
        /// <returns>The number of bytes sent.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is <see langword="null" />.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        public int SendTo(ReadOnlySpan<byte> buffer, EndPoint remoteEP)
        {
            return SendTo(buffer, SocketFlags.None, remoteEP);
        }

        /// <summary>
        /// Sends data to a specific endpoint using the specified <see cref="SocketFlags"/>.
        /// </summary>
        /// <param name="buffer">A span of bytes that contains the data to be sent.</param>
        /// <param name="socketFlags">A bitwise combination of the <see cref="SocketFlags"/> values.</param>
        /// <param name="remoteEP">The <see cref="EndPoint"/> that represents the destination for the data.</param>
        /// <returns>The number of bytes sent.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is <see langword="null" />.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remoteEP);

            ValidateBlockingMode();

            Internals.SocketAddress socketAddress = Serialize(ref remoteEP);

            int bytesTransferred;
            SocketError errorCode = SocketPal.SendTo(_handle, buffer, socketFlags, socketAddress.Buffer, socketAddress.Size, out bytesTransferred);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateSendSocketErrorForDisposed(ref errorCode);

                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesSent(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramSent();
            }

            if (_rightEndPoint == null)
            {
                // Save a copy of the EndPoint so we can use it for Create().
                _rightEndPoint = remoteEP;
            }

            return bytesTransferred;
        }

        // Receives data from a connected socket.
        public int Receive(byte[] buffer, int size, SocketFlags socketFlags)
        {
            return Receive(buffer, 0, size, socketFlags);
        }

        public int Receive(byte[] buffer, SocketFlags socketFlags)
        {
            return Receive(buffer, 0, buffer != null ? buffer.Length : 0, socketFlags);
        }

        public int Receive(byte[] buffer)
        {
            return Receive(buffer, 0, buffer != null ? buffer.Length : 0, SocketFlags.None);
        }

        // Receives data from a connected socket into a specific location of the receive buffer.
        public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
        {
            SocketError errorCode;
            int bytesTransferred = Receive(buffer, offset, size, socketFlags, out errorCode);
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
            return bytesTransferred;
        }

        public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}");

            int bytesTransferred;
            errorCode = SocketPal.Receive(_handle, buffer, offset, size, socketFlags, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);

            if (errorCode != SocketError.Success)
            {
                // Update the internal state of this socket according to the error before throwing.
                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                return 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred);

            return bytesTransferred;
        }

        public int Receive(Span<byte> buffer) => Receive(buffer, SocketFlags.None);

        public int Receive(Span<byte> buffer, SocketFlags socketFlags)
        {
            int bytesTransferred = Receive(buffer, socketFlags, out SocketError errorCode);
            return errorCode == SocketError.Success ?
                bytesTransferred :
                throw new SocketException((int)errorCode);
        }

        public int Receive(Span<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();
            ValidateBlockingMode();

            int bytesTransferred;
            errorCode = SocketPal.Receive(_handle, buffer, socketFlags, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);

            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                bytesTransferred = 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            return bytesTransferred;
        }

        public int Receive(IList<ArraySegment<byte>> buffers)
        {
            return Receive(buffers, SocketFlags.None);
        }

        public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
        {
            SocketError errorCode;
            int bytesTransferred = Receive(buffers, socketFlags, out errorCode);
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
            return bytesTransferred;
        }

        public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(buffers);

            if (buffers.Count == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_sockets_zerolist, nameof(buffers)), nameof(buffers));
            }


            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}");

            int bytesTransferred;
            errorCode = SocketPal.Receive(_handle, buffers, socketFlags, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);

            if (errorCode != SocketError.Success)
            {
                // Update the internal state of this socket according to the error before throwing.
                UpdateStatusAfterSocketError(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, new SocketException((int)errorCode));
                return 0;
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            return bytesTransferred;
        }

        // Receives a datagram into a specific location in the data buffer and stores
        // the end point.
        public int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ValidateReceiveFromEndpointAndState(remoteEP, nameof(remoteEP));

            SocketPal.CheckDualModeReceiveSupport(this);
            ValidateBlockingMode();

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvMsg; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = remoteEP;
            Internals.SocketAddress socketAddress = Serialize(ref endPointSnapshot);

            // Save a copy of the original EndPoint.
            Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(endPointSnapshot);

            SetReceivingPacketInformation();

            Internals.SocketAddress receiveAddress;
            int bytesTransferred;
            SocketError errorCode = SocketPal.ReceiveMessageFrom(this, _handle, buffer, offset, size, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success && errorCode != SocketError.MessageSize)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (errorCode == SocketError.Success && SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            if (!socketAddressOriginal.Equals(receiveAddress))
            {
                try
                {
                    remoteEP = endPointSnapshot.Create(receiveAddress);
                }
                catch
                {
                }
                // Save a copy of the EndPoint so we can use it for Create().
                _rightEndPoint ??= endPointSnapshot;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, errorCode);
            return bytesTransferred;
        }

        /// <summary>
        /// Receives the specified number of bytes of data into the specified location of the data buffer,
        /// using the specified <paramref name="socketFlags"/>, and stores the endpoint and packet information.
        /// </summary>
        /// <param name="buffer">
        /// An <see cref="Span{T}"/> of type <see cref="byte"/> that is the storage location for received data.
        /// </param>
        /// <param name="socketFlags">
        /// A bitwise combination of the <see cref="SocketFlags"/> values.
        /// </param>
        /// <param name="remoteEP">
        /// An <see cref="EndPoint"/>, passed by reference, that represents the remote server.
        /// </param>
        /// <param name="ipPacketInformation">
        /// An <see cref="IPPacketInformation"/> holding address and interface information.
        /// </param>
        /// <returns>
        /// The number of bytes received.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> object has been closed.</exception>
        /// <exception cref="ArgumentNullException">The <see cref="EndPoint"/> remoteEP is null.</exception>
        /// <exception cref="ArgumentException">The <see cref="AddressFamily"/> of the <see cref="EndPoint"/> used in
        /// <see cref="Socket.ReceiveMessageFrom(Span{byte}, ref SocketFlags, ref EndPoint, out IPPacketInformation)"/>
        /// needs to match the <see cref="AddressFamily"/> of the <see cref="EndPoint"/> used in SendTo.</exception>
        /// <exception cref="InvalidOperationException">
        /// <para>The <see cref="Socket"/> object is not in blocking mode and cannot accept this synchronous call.</para>
        /// <para>You must call the Bind method before performing this operation.</para></exception>
        public int ReceiveMessageFrom(Span<byte> buffer, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(remoteEP);

            if (!CanTryAddressFamily(remoteEP.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, remoteEP.AddressFamily, _addressFamily), nameof(remoteEP));
            }
            if (_rightEndPoint == null)
            {
                throw new InvalidOperationException(SR.net_sockets_mustbind);
            }

            SocketPal.CheckDualModeReceiveSupport(this);
            ValidateBlockingMode();

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvMsg; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = remoteEP;
            Internals.SocketAddress socketAddress = Serialize(ref endPointSnapshot);

            // Save a copy of the original EndPoint.
            Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(endPointSnapshot);

            SetReceivingPacketInformation();

            Internals.SocketAddress receiveAddress;
            int bytesTransferred;
            SocketError errorCode = SocketPal.ReceiveMessageFrom(this, _handle, buffer, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success && errorCode != SocketError.MessageSize)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (errorCode == SocketError.Success && SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            if (!socketAddressOriginal.Equals(receiveAddress))
            {
                try
                {
                    remoteEP = endPointSnapshot.Create(receiveAddress);
                }
                catch
                {
                }
                // Save a copy of the EndPoint so we can use it for Create().
                _rightEndPoint ??= endPointSnapshot;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, errorCode);
            return bytesTransferred;
        }

        // Receives a datagram into a specific location in the data buffer and stores
        // the end point.
        public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ValidateReceiveFromEndpointAndState(remoteEP, nameof(remoteEP));

            SocketPal.CheckDualModeReceiveSupport(this);

            ValidateBlockingMode();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SRC{LocalEndPoint} size:{size} remoteEP:{remoteEP}");

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvFrom; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = remoteEP;
            Internals.SocketAddress socketAddress = Serialize(ref endPointSnapshot);
            Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(endPointSnapshot);

            int bytesTransferred;
            SocketError errorCode = SocketPal.ReceiveFrom(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer, ref socketAddress.InternalSize, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
            // If the native call fails we'll throw a SocketException.
            SocketException? socketException = null;
            if (errorCode != SocketError.Success)
            {
                socketException = new SocketException((int)errorCode);
                UpdateStatusAfterSocketError(socketException);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, socketException);

                if (socketException.SocketErrorCode != SocketError.MessageSize)
                {
                    throw socketException;
                }
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            if (!socketAddressOriginal.Equals(socketAddress))
            {
                try
                {
                    remoteEP = endPointSnapshot.Create(socketAddress);
                }
                catch
                {
                }
                // Save a copy of the EndPoint so we can use it for Create().
                _rightEndPoint ??= endPointSnapshot;
            }

            if (socketException != null)
            {
                throw socketException;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.DumpBuffer(this, buffer, offset, size);
            return bytesTransferred;
        }

        // Receives a datagram and stores the source end point.
        public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
        {
            return ReceiveFrom(buffer, 0, size, socketFlags, ref remoteEP);
        }

        public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
        {
            return ReceiveFrom(buffer, 0, buffer != null ? buffer.Length : 0, socketFlags, ref remoteEP);
        }

        public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP)
        {
            return ReceiveFrom(buffer, 0, buffer != null ? buffer.Length : 0, SocketFlags.None, ref remoteEP);
        }

        /// <summary>
        /// Receives a datagram into the data buffer and stores the endpoint.
        /// </summary>
        /// <param name="buffer">A span of bytes that is the storage location for received data.</param>
        /// <param name="remoteEP">An <see cref="EndPoint"/>, passed by reference, that represents the remote server.</param>
        /// <returns>The number of bytes received.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is <see langword="null" />.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        public int ReceiveFrom(Span<byte> buffer, ref EndPoint remoteEP)
        {
            return ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);
        }

        /// <summary>
        /// Receives a datagram into the data buffer, using the specified <see cref="SocketFlags"/>, and stores the endpoint.
        /// </summary>
        /// <param name="buffer">A span of bytes that is the storage location for received data.</param>
        /// <param name="socketFlags">A bitwise combination of the <see cref="SocketFlags"/> values.</param>
        /// <param name="remoteEP">An <see cref="EndPoint"/>, passed by reference, that represents the remote server.</param>
        /// <returns>The number of bytes received.</returns>
        /// <exception cref="ArgumentNullException"><c>remoteEP</c> is <see langword="null" />.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        public int ReceiveFrom(Span<byte> buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
        {
            ThrowIfDisposed();
            ValidateReceiveFromEndpointAndState(remoteEP, nameof(remoteEP));

            SocketPal.CheckDualModeReceiveSupport(this);

            ValidateBlockingMode();

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvFrom; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = remoteEP;
            Internals.SocketAddress socketAddress = Serialize(ref endPointSnapshot);
            Internals.SocketAddress socketAddressOriginal = IPEndPointExtensions.Serialize(endPointSnapshot);

            int bytesTransferred;
            SocketError errorCode = SocketPal.ReceiveFrom(_handle, buffer, socketFlags, socketAddress.Buffer, ref socketAddress.InternalSize, out bytesTransferred);

            UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
            // If the native call fails we'll throw a SocketException.
            SocketException? socketException = null;
            if (errorCode != SocketError.Success)
            {
                socketException = new SocketException((int)errorCode);
                UpdateStatusAfterSocketError(socketException);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, socketException);

                if (socketException.SocketErrorCode != SocketError.MessageSize)
                {
                    throw socketException;
                }
            }
            else if (SocketsTelemetry.Log.IsEnabled())
            {
                SocketsTelemetry.Log.BytesReceived(bytesTransferred);
                if (SocketType == SocketType.Dgram) SocketsTelemetry.Log.DatagramReceived();
            }

            if (!socketAddressOriginal.Equals(socketAddress))
            {
                try
                {
                    remoteEP = endPointSnapshot.Create(socketAddress);
                }
                catch
                {
                }
                if (_rightEndPoint == null)
                {
                    // Save a copy of the EndPoint so we can use it for Create().
                    _rightEndPoint = endPointSnapshot;
                }
            }

            if (socketException != null)
            {
                throw socketException;
            }

            return bytesTransferred;
        }

        public int IOControl(int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
        {
            ThrowIfDisposed();

            int realOptionLength;

            // IOControl is used for Windows-specific IOCTL operations.  If we need to add support for IOCTLs specific
            // to other platforms, we will likely need to add a new API, as the control codes may overlap with those
            // from Windows.  Generally it would be preferable to add new methods/properties to abstract these across
            // platforms, however.
            SocketError errorCode = SocketPal.WindowsIoctl(_handle, ioControlCode, optionInValue, optionOutValue, out realOptionLength);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"WindowsIoctl returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            return realOptionLength;
        }

        public int IOControl(IOControlCode ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
        {
            return IOControl(unchecked((int)ioControlCode), optionInValue, optionOutValue);
        }

        // Sets the specified option to the specified value.
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
        {
            ThrowIfDisposed();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}");

            SetSocketOption(optionLevel, optionName, optionValue, false);
        }

        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            ThrowIfDisposed();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}");

            // This can throw ObjectDisposedException.
            SocketError errorCode = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetSockOpt returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        // Sets the specified option to the specified value.
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
        {
            SetSocketOption(optionLevel, optionName, (optionValue ? 1 : 0));
        }

        // Sets the specified option to the specified value.
        public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(optionValue);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}");

            if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
            {
                LingerOption? lingerOption = optionValue as LingerOption;
                if (lingerOption == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_invalid_optionValue, "LingerOption"), nameof(optionValue));
                }
                if (lingerOption.LingerTime < 0 || lingerOption.LingerTime > (int)ushort.MaxValue)
                {
                    throw new ArgumentException(SR.Format(SR.ArgumentOutOfRange_Bounds_Lower_Upper_Named, 0, (int)ushort.MaxValue, "optionValue.LingerTime"), nameof(optionValue));
                }
                SetLingerOption(lingerOption);
            }
            else if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                MulticastOption? multicastOption = optionValue as MulticastOption;
                if (multicastOption == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_invalid_optionValue, "MulticastOption"), nameof(optionValue));
                }
                SetMulticastOption(optionName, multicastOption);
            }
            else if (optionLevel == SocketOptionLevel.IPv6 && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                // IPv6 Changes: Handle IPv6 Multicast Add / Drop
                IPv6MulticastOption? multicastOption = optionValue as IPv6MulticastOption;
                if (multicastOption == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_invalid_optionValue, "IPv6MulticastOption"), nameof(optionValue));
                }
                SetIPv6MulticastOption(optionName, multicastOption);
            }
            else
            {
                throw new ArgumentException(SR.net_sockets_invalid_optionValue_all, nameof(optionValue));
            }
        }

        /// <summary>Sets a socket option value using platform-specific level and name identifiers.</summary>
        /// <param name="optionLevel">The platform-defined option level.</param>
        /// <param name="optionName">The platform-defined option name.</param>
        /// <param name="optionValue">The value to which the option should be set.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <remarks>
        /// In general, the SetSocketOption method should be used whenever setting a <see cref="Socket"/> option.
        /// The <see cref="SetRawSocketOption"/> should be used only when <see cref="SocketOptionLevel"/> and <see cref="SocketOptionName"/>
        /// do not expose the required option.
        /// </remarks>
        public void SetRawSocketOption(int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
        {
            ThrowIfDisposed();

            SocketError errorCode = SocketPal.SetRawSockOpt(_handle, optionLevel, optionName, optionValue);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetSockOpt optionLevel:{optionLevel} optionName:{optionName} returns errorCode:{errorCode}");

            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        // Gets the value of a socket option.
        public object? GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
        {
            ThrowIfDisposed();
            if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
            {
                return GetLingerOpt();
            }
            else if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                return GetMulticastOpt(optionName);
            }
            else if (optionLevel == SocketOptionLevel.IPv6 && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
            {
                // Handle IPv6 case
                return GetIPv6MulticastOpt(optionName);
            }

            int optionValue;

            // This can throw ObjectDisposedException.
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                out optionValue);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetSockOpt returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return optionValue;
        }

        public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
        {
            ThrowIfDisposed();

            int optionLength = optionValue != null ? optionValue.Length : 0;

            // This can throw ObjectDisposedException.
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                optionValue!,
                ref optionLength);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetSockOpt returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
        {
            ThrowIfDisposed();

            byte[] optionValue = new byte[optionLength];
            int realOptionLength = optionLength;

            // This can throw ObjectDisposedException.
            SocketError errorCode = SocketPal.GetSockOpt(
                _handle,
                optionLevel,
                optionName,
                optionValue,
                ref realOptionLength);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetSockOpt returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            if (optionLength != realOptionLength)
            {
                byte[] newOptionValue = new byte[realOptionLength];
                Buffer.BlockCopy(optionValue, 0, newOptionValue, 0, realOptionLength);
                optionValue = newOptionValue;
            }

            return optionValue;
        }

        /// <summary>Gets a socket option value using platform-specific level and name identifiers.</summary>
        /// <param name="optionLevel">The platform-defined option level.</param>
        /// <param name="optionName">The platform-defined option name.</param>
        /// <param name="optionValue">The span into which the retrieved option value should be stored.</param>
        /// <returns>The number of bytes written into <paramref name="optionValue"/> for a successfully retrieved value.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Socket"/> has been closed.</exception>
        /// <exception cref="SocketException">An error occurred when attempting to access the socket.</exception>
        /// <remarks>
        /// In general, the GetSocketOption method should be used whenever getting a <see cref="Socket"/> option.
        /// The <see cref="GetRawSocketOption"/> should be used only when <see cref="SocketOptionLevel"/> and <see cref="SocketOptionName"/>
        /// do not expose the required option.
        /// </remarks>
        public int GetRawSocketOption(int optionLevel, int optionName, Span<byte> optionValue)
        {
            ThrowIfDisposed();

            int realOptionLength = optionValue.Length;
            SocketError errorCode = SocketPal.GetRawSockOpt(_handle, optionLevel, optionName, optionValue, ref realOptionLength);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetRawSockOpt optionLevel:{optionLevel} optionName:{optionName} returned errorCode:{errorCode}");

            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return realOptionLength;
        }

        [SupportedOSPlatform("windows")]
        public void SetIPProtectionLevel(IPProtectionLevel level)
        {
            if (level == IPProtectionLevel.Unspecified)
            {
                throw new ArgumentException(SR.net_sockets_invalid_optionValue_all, nameof(level));
            }

            if (_addressFamily == AddressFamily.InterNetworkV6)
            {
                SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IPv6, (int)level);
            }
            else if (_addressFamily == AddressFamily.InterNetwork)
            {
                SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IP, (int)level);
            }
            else
            {
                throw new NotSupportedException(SR.net_invalidversion);
            }
        }

        // Determines the status of the socket.
        public bool Poll(int microSeconds, SelectMode mode)
        {
            ThrowIfDisposed();

            bool status;
            SocketError errorCode = SocketPal.Poll(_handle, microSeconds, mode, out status);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Poll returns socketCount:{(int)errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            return status;
        }

        public bool Poll(TimeSpan timeout, SelectMode mode) =>
            Poll(ToTimeoutMicroseconds(timeout), mode);

        // Determines the status of a socket.
        public static void Select(IList? checkRead, IList? checkWrite, IList? checkError, int microSeconds)
        {
            if ((checkRead == null || checkRead.Count == 0) &&
                (checkWrite == null || checkWrite.Count == 0) &&
                (checkError == null || checkError.Count == 0))
            {
                throw new ArgumentNullException(null, SR.net_sockets_empty_select);
            }
            const int MaxSelect = 65536;
            if (checkRead != null && checkRead.Count > MaxSelect)
            {
                throw new ArgumentOutOfRangeException(nameof(checkRead), SR.Format(SR.net_sockets_toolarge_select, nameof(checkRead), MaxSelect.ToString()));
            }
            if (checkWrite != null && checkWrite.Count > MaxSelect)
            {
                throw new ArgumentOutOfRangeException(nameof(checkWrite), SR.Format(SR.net_sockets_toolarge_select, nameof(checkWrite), MaxSelect.ToString()));
            }
            if (checkError != null && checkError.Count > MaxSelect)
            {
                throw new ArgumentOutOfRangeException(nameof(checkError), SR.Format(SR.net_sockets_toolarge_select, nameof(checkError), MaxSelect.ToString()));
            }

            SocketError errorCode = SocketPal.Select(checkRead, checkWrite, checkError, microSeconds);

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                throw new SocketException((int)errorCode);
            }
        }

        public static void Select(IList? checkRead, IList? checkWrite, IList? checkError, TimeSpan timeout) => Select(checkRead, checkWrite, checkError, ToTimeoutMicroseconds(timeout));

        private static int ToTimeoutMicroseconds(TimeSpan timeout)
        {
            long totalMicroseconds = (long)timeout.TotalMicroseconds;
            if (totalMicroseconds < -1 || totalMicroseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
            return (int)totalMicroseconds;
        }

        public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ConnectAsync(remoteEP), callback, state);

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback? requestCallback, object? state) =>
            TaskToApm.Begin(ConnectAsync(host, port), requestCallback, state);

        public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback? requestCallback, object? state) =>
            TaskToApm.Begin(ConnectAsync(address, port), requestCallback, state);

        public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback? requestCallback, object? state) =>
            TaskToApm.Begin(ConnectAsync(addresses, port), requestCallback, state);

        public void EndConnect(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            TaskToApm.End(asyncResult);
        }

        public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(DisconnectAsync(reuseSocket).AsTask(), callback, state);

        public void Disconnect(bool reuseSocket)
        {
            ThrowIfDisposed();

            SocketError errorCode;

            // This can throw ObjectDisposedException (handle, and retrieving the delegate).
            errorCode = SocketPal.Disconnect(this, _handle, reuseSocket);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"UnsafeNclNativeMethods.OSSOCK.DisConnectEx returns:{errorCode}");

            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            SetToDisconnected();
            _remoteEndPoint = null;
            _localEndPoint = null;
        }

        public void EndDisconnect(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            TaskToApm.End(asyncResult);
        }


        public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);

            return TaskToApm.Begin(SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags, default).AsTask(), callback, state);
        }

        public IAsyncResult? BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);

            Task<int> t = SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags, default).AsTask();
            if (t.IsFaulted || t.IsCanceled)
            {
                errorCode = GetSocketErrorFromFaultedTask(t);
                return null;
            }

            errorCode = SocketError.Success;
            return TaskToApm.Begin(t, callback, state);
        }

        public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();

            return TaskToApm.Begin(SendAsync(buffers, socketFlags), callback, state);
        }

        public IAsyncResult? BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();

            Task<int> t = SendAsync(buffers, socketFlags);
            if (t.IsFaulted || t.IsCanceled)
            {
                errorCode = GetSocketErrorFromFaultedTask(t);
                return null;
            }

            errorCode = SocketError.Success;
            return TaskToApm.Begin(t, callback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();

            return TaskToApm.End<int>(asyncResult);
        }

        public int EndSend(IAsyncResult asyncResult, out SocketError errorCode) =>
            EndSendReceive(asyncResult, out errorCode);

        public IAsyncResult BeginSendFile(string? fileName, AsyncCallback? callback, object? state)
        {
            return BeginSendFile(fileName, null, null, TransmitFileOptions.UseDefaultWorkerThread, callback, state);
        }

        public IAsyncResult BeginSendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();

            if (!Connected)
            {
                throw new NotSupportedException(SR.net_notconnected);
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"::DoBeginSendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}");

            return TaskToApm.Begin(SendFileAsync(fileName, preBuffer, postBuffer, flags).AsTask(), callback, state);
        }

        public void EndSendFile(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(asyncResult);

            TaskToApm.End(asyncResult);
        }

        public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ArgumentNullException.ThrowIfNull(remoteEP);

            Task<int> t = SendToAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
            return TaskToApm.Begin(t, callback, state);
        }

        public int EndSendTo(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            return TaskToApm.End<int>(asyncResult);
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            return TaskToApm.Begin(ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default).AsTask(), callback, state);
        }

        public IAsyncResult? BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            Task<int> t = ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default).AsTask();

            if (t.IsFaulted || t.IsCanceled)
            {
                errorCode = GetSocketErrorFromFaultedTask(t);
                return null;
            }

            errorCode = SocketError.Success;
            return TaskToApm.Begin(t, callback, state);
        }

        public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            return TaskToApm.Begin(ReceiveAsync(buffers, socketFlags), callback, state);
        }

        public IAsyncResult? BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            Task<int> t = ReceiveAsync(buffers, socketFlags);

            if (t.IsFaulted || t.IsCanceled)
            {
                errorCode = GetSocketErrorFromFaultedTask(t);
                return null;
            }

            errorCode = SocketError.Success;
            return TaskToApm.Begin(t, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            return TaskToApm.End<int>(asyncResult);
        }

        public int EndReceive(IAsyncResult asyncResult, out SocketError errorCode) =>
            EndSendReceive(asyncResult, out errorCode);

        private int EndSendReceive(IAsyncResult asyncResult, out SocketError errorCode)
        {
            ThrowIfDisposed();

            if (TaskToApm.GetTask(asyncResult) is not Task<int> ti)
            {
                throw new ArgumentException(null, nameof(asyncResult));
            }

            if (!ti.IsCompleted)
            {
                // TODO https://github.com/dotnet/runtime/issues/17148: Wait without throwing
                ((IAsyncResult)ti).AsyncWaitHandle.WaitOne();
            }

            if (ti.IsCompletedSuccessfully)
            {
                errorCode = SocketError.Success;
                return ti.Result;
            }

            errorCode = GetSocketErrorFromFaultedTask(ti);
            return 0;
        }

        public IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"size:{size}");

            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ValidateReceiveFromEndpointAndState(remoteEP, nameof(remoteEP));

            Task<SocketReceiveMessageFromResult> t = ReceiveMessageFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
            // In case of synchronous completion, ReceiveMessageFromAsync() returns a completed task.
            // When this happens, we need to update 'remoteEP' in order to conform to the historical behavior of BeginReceiveMessageFrom().
            if (t.IsCompletedSuccessfully)
            {
                EndPoint resultEp = t.Result.RemoteEndPoint;
                if (!remoteEP.Equals(resultEp)) remoteEP = resultEp;
            }
            IAsyncResult asyncResult = TaskToApm.Begin(t, callback, state);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"size:{size} returning AsyncResult:{asyncResult}");
            return asyncResult;
        }

        public int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(endPoint);
            if (!CanTryAddressFamily(endPoint.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), nameof(endPoint));
            }

            SocketReceiveMessageFromResult result = TaskToApm.End<SocketReceiveMessageFromResult>(asyncResult);
            if (!endPoint.Equals(result.RemoteEndPoint))
            {
                endPoint = result.RemoteEndPoint;
            }
            socketFlags = result.SocketFlags;
            ipPacketInformation = result.PacketInformation;
            return result.ReceivedBytes;
        }

        public IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, size);
            ValidateReceiveFromEndpointAndState(remoteEP, nameof(remoteEP));

            Task<SocketReceiveFromResult> t = ReceiveFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
            // In case of synchronous completion, ReceiveFromAsync() returns a completed task.
            // When this happens, we need to update 'remoteEP' in order to conform to the historical behavior of BeginReceiveFrom().
            if (t.IsCompletedSuccessfully)
            {
                EndPoint resultEp = t.Result.RemoteEndPoint;
                if (!remoteEP.Equals(resultEp)) remoteEP = resultEp;
            }

            return TaskToApm.Begin(t, callback, state);
        }

        public int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(endPoint);
            if (!CanTryAddressFamily(endPoint.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), nameof(endPoint));
            }

            SocketReceiveFromResult result = TaskToApm.End<SocketReceiveFromResult>(asyncResult);
            if (!endPoint.Equals(result.RemoteEndPoint))
            {
                endPoint = result.RemoteEndPoint;
            }
            return result.ReceivedBytes;
        }

        public IAsyncResult BeginAccept(AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(AcceptAsync(), callback, state);

        public Socket EndAccept(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            return TaskToApm.End<Socket>(asyncResult);
        }

        // This method provides support for legacy BeginAccept methods that take a "receiveSize" argument and
        // allow data to be received as part of the accept operation.
        // There's no direct equivalent of this in the Task APIs, so we mimic it here.
        private async Task<(Socket s, byte[] buffer, int bytesReceived)> AcceptAndReceiveHelperAsync(Socket? acceptSocket, int receiveSize)
        {
            if (receiveSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveSize));
            }

            Socket s = await AcceptAsync(acceptSocket).ConfigureAwait(false);

            byte[] buffer;
            int bytesReceived;
            if (receiveSize == 0)
            {
                buffer = Array.Empty<byte>();
                bytesReceived = 0;
            }
            else
            {
                buffer = new byte[receiveSize];
                try
                {
                    bytesReceived = await s.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);
                }
                catch
                {
                    s.Dispose();
                    throw;
                }
            }

            return (s, buffer, bytesReceived);
        }

        public IAsyncResult BeginAccept(int receiveSize, AsyncCallback? callback, object? state) =>
            BeginAccept(acceptSocket: null, receiveSize, callback, state);

        public IAsyncResult BeginAccept(Socket? acceptSocket, int receiveSize, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(AcceptAndReceiveHelperAsync(acceptSocket, receiveSize), callback, state);

        public Socket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
        {
            Socket socket = EndAccept(out byte[] innerBuffer, out int bytesTransferred, asyncResult);
            buffer = new byte[bytesTransferred];
            Buffer.BlockCopy(innerBuffer, 0, buffer, 0, bytesTransferred);
            return socket;
        }

        public Socket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
        {
            ThrowIfDisposed();
            Socket s;
            (s, buffer, bytesTransferred) = TaskToApm.End<(Socket, byte[], int)>(asyncResult);
            return s;
        }

        // Disables sends and receives on a socket.
        public void Shutdown(SocketShutdown how)
        {
            ThrowIfDisposed();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"how:{how}");

            // This can throw ObjectDisposedException.
            SocketError errorCode = SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Shutdown returns errorCode:{errorCode}");

            // Skip good cases: success, socket already closed.
            if (errorCode != SocketError.Success && errorCode != SocketError.NotSocket)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }

            SetToDisconnected();
            InternalSetBlocking(_willBlockInternal);
        }

        //
        // Async methods
        //

        public bool AcceptAsync(SocketAsyncEventArgs e) => AcceptAsync(e, CancellationToken.None);

        private bool AcceptAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.HasMultipleBuffers)
            {
                throw new ArgumentException(SR.net_multibuffernotsupported, nameof(e));
            }
            if (_rightEndPoint == null)
            {
                throw new InvalidOperationException(SR.net_sockets_mustbind);
            }
            if (!_isListening)
            {
                throw new InvalidOperationException(SR.net_sockets_mustlisten);
            }

            // Handle AcceptSocket property.
            SafeSocketHandle? acceptHandle;
            e.AcceptSocket = GetOrCreateAcceptSocket(e.AcceptSocket, true, "AcceptSocket", out acceptHandle);

            if (SocketsTelemetry.Log.IsEnabled()) SocketsTelemetry.Log.AcceptStart(_rightEndPoint!);

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.Accept);
            e.StartOperationAccept();
            SocketError socketError;
            try
            {
                socketError = e.DoOperationAccept(this, _handle, acceptHandle, cancellationToken);
            }
            catch (Exception ex)
            {
                SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);

                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool ConnectAsync(SocketAsyncEventArgs e) =>
            ConnectAsync(e, userSocket: true, saeaCancelable: true);

        internal bool ConnectAsync(SocketAsyncEventArgs e, bool userSocket, bool saeaCancelable)
        {
            bool pending;

            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.HasMultipleBuffers)
            {
                throw new ArgumentException(SR.net_multibuffernotsupported, "BufferList");
            }
            ArgumentNullException.ThrowIfNull(e.RemoteEndPoint, "remoteEP");
            if (_isListening)
            {
                throw new InvalidOperationException(SR.net_sockets_mustnotlisten);
            }

            if (_isConnected)
            {
                throw new SocketException((int)SocketError.IsConnected);
            }

            // Prepare SocketAddress.
            EndPoint? endPointSnapshot = e.RemoteEndPoint;
            DnsEndPoint? dnsEP = endPointSnapshot as DnsEndPoint;

            if (dnsEP != null)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.ConnectedAsyncDns(this);

                ValidateForMultiConnect(isMultiEndpoint: true); // needs to come before CanTryAddressFamily call

                if (dnsEP.AddressFamily != AddressFamily.Unspecified && !CanTryAddressFamily(dnsEP.AddressFamily))
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }

                e.StartOperationCommon(this, SocketAsyncOperation.Connect);
                e.StartOperationConnect(saeaCancelable, userSocket);
                try
                {
                    pending = e.DnsConnectAsync(dnsEP, default, default);
                }
                catch
                {
                    e.Complete(); // Clear in-use flag on event args object.
                    throw;
                }
            }
            else
            {
                ValidateForMultiConnect(isMultiEndpoint: false); // needs to come before CanTryAddressFamily call

                // Throw if remote address family doesn't match socket.
                if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
                {
                    throw new NotSupportedException(SR.net_invalidversion);
                }

                e._socketAddress = Serialize(ref endPointSnapshot);
                _pendingConnectRightEndPoint = endPointSnapshot;
                _nonBlockingConnectInProgress = false;

                WildcardBindForConnectIfNecessary(endPointSnapshot.AddressFamily);

                SocketsTelemetry.Log.ConnectStart(e._socketAddress!);

                // Prepare for the native call.
                try
                {
                    e.StartOperationCommon(this, SocketAsyncOperation.Connect);
                    e.StartOperationConnect(saeaMultiConnectCancelable: false, userSocket);
                }
                catch (Exception ex)
                {
                    SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
                    throw;
                }

                // Make the native call.
                try
                {
                    // ConnectEx supports connection-oriented sockets but not UDS. The socket must be bound before calling ConnectEx.
                    bool canUseConnectEx = _socketType == SocketType.Stream && endPointSnapshot.AddressFamily != AddressFamily.Unix;
                    SocketError socketError = canUseConnectEx ?
                        e.DoOperationConnectEx(this, _handle) :
                        e.DoOperationConnect(this, _handle); // For connectionless protocols, Connect is not an I/O call.
                    pending = socketError == SocketError.IOPending;
                }
                catch (Exception ex)
                {
                    SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);

                    _localEndPoint = null;

                    // Clear in-use flag on event args object.
                    e.Complete();
                    throw;
                }
            }

            return pending;
        }

        public static bool ConnectAsync(SocketType socketType, ProtocolType protocolType, SocketAsyncEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.HasMultipleBuffers)
            {
                throw new ArgumentException(SR.net_multibuffernotsupported, nameof(e));
            }
            if (e.RemoteEndPoint == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "e.RemoteEndPoint"), nameof(e));
            }

            EndPoint endPointSnapshot = e.RemoteEndPoint;
            DnsEndPoint? dnsEP = endPointSnapshot as DnsEndPoint;

            bool pending;
            if (dnsEP != null)
            {
                Socket? attemptSocket = dnsEP.AddressFamily != AddressFamily.Unspecified ? new Socket(dnsEP.AddressFamily, socketType, protocolType) : null;
                e.StartOperationCommon(attemptSocket, SocketAsyncOperation.Connect);
                e.StartOperationConnect(saeaMultiConnectCancelable: true, userSocket: false);
                try
                {
                    pending = e.DnsConnectAsync(dnsEP, socketType, protocolType);
                }
                catch
                {
                    e.Complete(); // Clear in-use flag on event args object.
                    throw;
                }
            }
            else
            {
                Socket attemptSocket = new Socket(endPointSnapshot.AddressFamily, socketType, protocolType);
                pending = attemptSocket.ConnectAsync(e, userSocket: false, saeaCancelable: true);
            }

            return pending;
        }

        /// <summary>Binds an unbound socket to "any" if necessary to support a connect.</summary>
        partial void WildcardBindForConnectIfNecessary(AddressFamily addressFamily);

        public static void CancelConnectAsync(SocketAsyncEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            e.CancelConnectAsync();
        }

        public bool DisconnectAsync(SocketAsyncEventArgs e) => DisconnectAsync(e, default);

        private bool DisconnectAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            // Throw if socket disposed
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.Disconnect);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationDisconnect(this, _handle, cancellationToken);
            }
            catch
            {
                // clear in-use on event arg object
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool ReceiveAsync(SocketAsyncEventArgs e) => ReceiveAsync(e, default(CancellationToken));

        private bool ReceiveAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.Receive);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationReceive(_handle, cancellationToken);
            }
            catch
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool ReceiveFromAsync(SocketAsyncEventArgs e) => ReceiveFromAsync(e, default);

        private bool ReceiveFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.RemoteEndPoint == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "e.RemoteEndPoint"), nameof(e));
            }
            if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, e.RemoteEndPoint.AddressFamily, _addressFamily), nameof(e));
            }

            SocketPal.CheckDualModeReceiveSupport(this);

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvFrom; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = e.RemoteEndPoint;
            e._socketAddress = Serialize(ref endPointSnapshot);

            // DualMode sockets may have updated the endPointSnapshot, and it has to have the same AddressFamily as
            // e.m_SocketAddres for Create to work later.
            e.RemoteEndPoint = endPointSnapshot;

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.ReceiveFrom);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationReceiveFrom(_handle, cancellationToken);
            }
            catch
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            bool pending = (socketError == SocketError.IOPending);
            return pending;
        }

        public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e) => ReceiveMessageFromAsync(e, default);

        private bool ReceiveMessageFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.RemoteEndPoint == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "e.RemoteEndPoint"), nameof(e));
            }
            if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, e.RemoteEndPoint.AddressFamily, _addressFamily), nameof(e));
            }

            SocketPal.CheckDualModeReceiveSupport(this);

            // We don't do a CAS demand here because the contents of remoteEP aren't used by
            // WSARecvMsg; all that matters is that we generate a unique-to-this-call SocketAddress
            // with the right address family.
            EndPoint endPointSnapshot = e.RemoteEndPoint;
            e._socketAddress = Serialize(ref endPointSnapshot);

            // DualMode may have updated the endPointSnapshot, and it has to have the same AddressFamily as
            // e.m_SocketAddres for Create to work later.
            e.RemoteEndPoint = endPointSnapshot;

            SetReceivingPacketInformation();

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.ReceiveMessageFrom);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationReceiveMessageFrom(this, _handle, cancellationToken);
            }
            catch
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool SendAsync(SocketAsyncEventArgs e) => SendAsync(e, default(CancellationToken));

        private bool SendAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.Send);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationSend(_handle, cancellationToken);
            }
            catch
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool SendPacketsAsync(SocketAsyncEventArgs e) => SendPacketsAsync(e, default(CancellationToken));

        private bool SendPacketsAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.SendPacketsElements == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "e.SendPacketsElements"), nameof(e));
            }
            if (!Connected)
            {
                throw new NotSupportedException(SR.net_notconnected);
            }

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.SendPackets);
            SocketError socketError;
            try
            {
                socketError = e.DoOperationSendPackets(this, _handle, cancellationToken);
            }
            catch (Exception)
            {
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            return socketError == SocketError.IOPending;
        }

        public bool SendToAsync(SocketAsyncEventArgs e) => SendToAsync(e, default);

        private bool SendToAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(e);
            if (e.RemoteEndPoint == null)
            {
                throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "e.RemoteEndPoint"), nameof(e));
            }

            // Prepare SocketAddress
            EndPoint endPointSnapshot = e.RemoteEndPoint;
            e._socketAddress = Serialize(ref endPointSnapshot);

            // Prepare for and make the native call.
            e.StartOperationCommon(this, SocketAsyncOperation.SendTo);

            EndPoint? oldEndPoint = _rightEndPoint;
            _rightEndPoint ??= endPointSnapshot;

            SocketError socketError;
            try
            {
                socketError = e.DoOperationSendTo(_handle, cancellationToken);
            }
            catch
            {
                _rightEndPoint = oldEndPoint;
                _localEndPoint = null;
                // Clear in-use flag on event args object.
                e.Complete();
                throw;
            }

            if (!CheckErrorAndUpdateStatus(socketError))
            {
                _rightEndPoint = oldEndPoint;
                _localEndPoint = null;
            }

            return socketError == SocketError.IOPending;
        }

        //
        // Internal and private properties
        //

        internal bool Disposed => _disposed != 0;

        //
        // Internal and private methods
        //

        internal static void GetIPProtocolInformation(AddressFamily addressFamily, Internals.SocketAddress socketAddress, out bool isIPv4, out bool isIPv6)
        {
            bool isIPv4MappedToIPv6 = socketAddress.Family == AddressFamily.InterNetworkV6 && socketAddress.GetIPAddress().IsIPv4MappedToIPv6;
            isIPv4 = addressFamily == AddressFamily.InterNetwork || isIPv4MappedToIPv6; // DualMode
            isIPv6 = addressFamily == AddressFamily.InterNetworkV6;
        }

        internal static int GetAddressSize(EndPoint endPoint)
        {
            AddressFamily fam = endPoint.AddressFamily;
            return
                fam == AddressFamily.InterNetwork ? SocketAddressPal.IPv4AddressSize :
                fam == AddressFamily.InterNetworkV6 ? SocketAddressPal.IPv6AddressSize :
                endPoint.Serialize().Size;
        }

        private Internals.SocketAddress Serialize(ref EndPoint remoteEP)
        {
            if (remoteEP is IPEndPoint ip)
            {
                IPAddress addr = ip.Address;
                if (addr.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
                {
                    addr = addr.MapToIPv6(); // For DualMode, use an IPv6 address.
                    remoteEP = new IPEndPoint(addr, ip.Port);
                }
            }
            else if (remoteEP is DnsEndPoint)
            {
                throw new ArgumentException(SR.Format(SR.net_sockets_invalid_dnsendpoint, nameof(remoteEP)), nameof(remoteEP));
            }

            return IPEndPointExtensions.Serialize(remoteEP);
        }

        private void DoConnect(EndPoint endPointSnapshot, Internals.SocketAddress socketAddress)
        {
            SocketsTelemetry.Log.ConnectStart(socketAddress);
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.Connect(_handle, socketAddress.Buffer, socketAddress.Size);
            }
            catch (Exception ex)
            {
                SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
                throw;
            }

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateConnectSocketErrorForDisposed(ref errorCode);
                // Update the internal state of this socket according to the error before throwing.
                SocketException socketException = SocketExceptionFactory.CreateSocketException((int)errorCode, endPointSnapshot);
                UpdateStatusAfterSocketError(socketException);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, socketException);

                SocketsTelemetry.Log.AfterConnect(errorCode);

                throw socketException;
            }

            SocketsTelemetry.Log.AfterConnect(SocketError.Success);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"connection to:{endPointSnapshot}");

            // Update state and performance counters.
            _pendingConnectRightEndPoint = endPointSnapshot;
            _nonBlockingConnectInProgress = false;
            SetToConnected();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Connected(this, LocalEndPoint, RemoteEndPoint);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (NetEventSource.Log.IsEnabled())
            {
                try
                {
                    NetEventSource.Info(this, $"disposing:{disposing} Disposed:{Disposed}");
                }
                catch (Exception exception) when (!ExceptionCheck.IsFatal(exception)) { }
            }

            // Make sure we're the first call to Dispose
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            SetToDisconnected();

            SafeSocketHandle? handle = _handle;
            // Avoid side effects when we don't own the handle.
            if (handle?.OwnsHandle == true)
            {
                if (!disposing)
                {
                    // When we are running on the finalizer thread, we don't call CloseAsIs
                    // because it may lead to blocking the finalizer thread when trying
                    // to abort on-going operations. We directly dispose the SafeHandle.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling _handle.Dispose()");
                    handle.Dispose();
                }
                else
                {
                    // Close the handle in one of several ways depending on the timeout.
                    // Ignore ObjectDisposedException just in case the handle somehow gets disposed elsewhere.
                    try
                    {
                        int timeout = _closeTimeout;
                        if (timeout == 0)
                        {
                            // Abortive.
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling _handle.CloseAsIs()");
                            handle.CloseAsIs(abortive: true);
                        }
                        else
                        {
                            SocketError errorCode;

                            // Go to blocking mode.
                            if (!_willBlock || !_willBlockInternal)
                            {
                                bool willBlock;
                                errorCode = SocketPal.SetBlocking(handle, false, out willBlock);
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONBIO):{errorCode}");
                            }

                            if (timeout < 0)
                            {
                                // Close with existing user-specified linger option.
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Calling _handle.CloseAsIs()");
                                handle.CloseAsIs(abortive: false);
                            }
                            else
                            {
                                // Since our timeout is in ms and linger is in seconds, implement our own sortof linger here.
                                errorCode = SocketPal.Shutdown(handle, _isConnected, _isDisconnected, SocketShutdown.Send);
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} shutdown():{errorCode}");

                                // This should give us a timeout in milliseconds.
                                errorCode = SocketPal.SetSockOpt(
                                    handle,
                                    SocketOptionLevel.Socket,
                                    SocketOptionName.ReceiveTimeout,
                                    timeout);
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} setsockopt():{errorCode}");

                                if (errorCode != SocketError.Success)
                                {
                                    handle.CloseAsIs(abortive: true);
                                }
                                else
                                {
                                    int unused;
                                    errorCode = SocketPal.Receive(handle, Array.Empty<byte>(), 0, 0, SocketFlags.None, out unused);
                                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} recv():{errorCode}");

                                    if (errorCode != (SocketError)0)
                                    {
                                        // We got a timeout - abort.
                                        handle.CloseAsIs(abortive: true);
                                    }
                                    else
                                    {
                                        // We got a FIN or data.  Use ioctlsocket to find out which.
                                        int dataAvailable = 0;
                                        errorCode = SocketPal.GetAvailable(handle, out dataAvailable);
                                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONREAD):{errorCode}");

                                        if (errorCode != SocketError.Success || dataAvailable != 0)
                                        {
                                            // If we have data or don't know, safest thing is to reset.
                                            handle.CloseAsIs(abortive: true);
                                        }
                                        else
                                        {
                                            // We got a FIN.  It'd be nice to block for the remainder of the timeout for the handshake to finish.
                                            // Since there's no real way to do that, close the socket with the user's preferences.  This lets
                                            // the user decide how best to handle this case via the linger options.
                                            handle.CloseAsIs(abortive: false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                // Delete file of bound UnixDomainSocketEndPoint.
                if (_rightEndPoint is UnixDomainSocketEndPoint unixEndPoint &&
                    unixEndPoint.BoundFileName is not null)
                {
                    try
                    {
                        File.Delete(unixEndPoint.BoundFileName);
                    }
                    catch
                    { }
                }
            }

            // Clean up any cached data
            DisposeCachedTaskSocketAsyncEventArgs();
        }

        public void Dispose()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"timeout = {_closeTimeout}");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Socket()
        {
            Dispose(false);
        }

        // This version does not throw.
        internal void InternalShutdown(SocketShutdown how)
        {

            if (Disposed || _handle.IsInvalid)
            {
                return;
            }

            try
            {
                SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
            }
            catch (ObjectDisposedException) { }
        }

        // Set the socket option to begin receiving packet information if it has not been
        // set for this socket previously.
        internal void SetReceivingPacketInformation()
        {
            if (!_receivingPacketInformation)
            {
                // DualMode: When bound to IPv6Any you must enable both socket options.
                // When bound to an IPv4 mapped IPv6 address you must enable the IPv4 socket option.
                IPEndPoint? ipEndPoint = _rightEndPoint as IPEndPoint;
                IPAddress? boundAddress = (ipEndPoint != null ? ipEndPoint.Address : null);
                Debug.Assert(boundAddress != null, "Not Bound");
                if (_addressFamily == AddressFamily.InterNetwork)
                {
                    SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                }

                if ((boundAddress != null && IsDualMode && (boundAddress.IsIPv4MappedToIPv6 || boundAddress.Equals(IPAddress.IPv6Any))))
                {
                    SocketPal.SetReceivingDualModeIPv4PacketInformation(this);
                }

                if (_addressFamily == AddressFamily.InterNetworkV6
                    && (boundAddress == null || !boundAddress.IsIPv4MappedToIPv6))
                {
                    SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                }

                _receivingPacketInformation = true;
            }
        }

        internal unsafe void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue, bool silent)
        {
            if (silent && (Disposed || _handle.IsInvalid))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "skipping the call");
                return;
            }
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetSockOpt returns errorCode:{errorCode}");
            }
            catch
            {
                if (silent && _handle.IsInvalid)
                {
                    return;
                }
                throw;
            }

            // Keep the internal state in sync if the user manually resets this.
            if (optionName == SocketOptionName.PacketInformation && optionValue == 0 &&
                errorCode == SocketError.Success)
            {
                _receivingPacketInformation = false;
            }

            if (silent)
            {
                return;
            }

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        private void SetMulticastOption(SocketOptionName optionName, MulticastOption MR)
        {
            SocketError errorCode = SocketPal.SetMulticastOption(_handle, optionName, MR);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetMulticastOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        // IPv6 setsockopt for JOIN / LEAVE multicast group.
        private void SetIPv6MulticastOption(SocketOptionName optionName, IPv6MulticastOption MR)
        {
            SocketError errorCode = SocketPal.SetIPv6MulticastOption(_handle, optionName, MR);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetIPv6MulticastOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketErrorAndThrowException(errorCode);
            }
        }

        private void SetLingerOption(LingerOption lref)
        {
            SocketError errorCode = SocketPal.SetLingerOption(_handle, lref);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetLingerOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }
        }

        private LingerOption? GetLingerOpt()
        {
            LingerOption? lingerOption;
            SocketError errorCode = SocketPal.GetLingerOption(_handle, out lingerOption);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetLingerOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return lingerOption;
        }

        private MulticastOption? GetMulticastOpt(SocketOptionName optionName)
        {
            MulticastOption? multicastOption;
            SocketError errorCode = SocketPal.GetMulticastOption(_handle, optionName, out multicastOption);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetMulticastOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return multicastOption;
        }

        // IPv6 getsockopt for JOIN / LEAVE multicast group.
        private IPv6MulticastOption? GetIPv6MulticastOpt(SocketOptionName optionName)
        {
            IPv6MulticastOption? multicastOption;
            SocketError errorCode = SocketPal.GetIPv6MulticastOption(_handle, optionName, out multicastOption);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"GetIPv6MulticastOption returns errorCode:{errorCode}");

            // Throw an appropriate SocketException if the native call fails.
            if (errorCode != SocketError.Success)
            {
                UpdateStatusAfterSocketOptionErrorAndThrowException(errorCode);
            }

            return multicastOption;
        }

        // This method will ignore failures, but returns the win32
        // error code, and will update internal state on success.
        private SocketError InternalSetBlocking(bool desired, out bool current)
        {
            if (Disposed)
            {
                current = _willBlock;
                return SocketError.Success;
            }

            // Can we avoid this call if willBlockInternal is already correct?
            bool willBlock = false;
            SocketError errorCode;
            try
            {
                errorCode = SocketPal.SetBlocking(_handle, desired, out willBlock);
            }
            catch (ObjectDisposedException)
            {
                errorCode = SocketError.NotSocket;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SetBlocking returns errorCode:{errorCode}");

            // We will update only internal state but only on successful win32 call
            // so if the native call fails, the state will remain the same.
            if (errorCode == SocketError.Success)
            {
                _willBlockInternal = willBlock;
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"errorCode:{errorCode} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}");

            current = _willBlockInternal;
            return errorCode;
        }

        // This method ignores all failures.
        internal void InternalSetBlocking(bool desired)
        {
            InternalSetBlocking(desired, out _);
        }

        // CreateAcceptSocket - pulls unmanaged results and assembles them into a new Socket object.
        internal Socket CreateAcceptSocket(SafeSocketHandle fd, EndPoint remoteEP)
        {
            // Internal state of the socket is inherited from listener.
            Debug.Assert(fd != null && !fd.IsInvalid);
            Socket socket = new Socket(fd, loadPropertiesFromHandle: false);
            return UpdateAcceptSocket(socket, remoteEP);
        }

        internal Socket UpdateAcceptSocket(Socket socket, EndPoint remoteEP)
        {
            // Internal state of the socket is inherited from listener.
            socket._addressFamily = _addressFamily;
            socket._socketType = _socketType;
            socket._protocolType = _protocolType;
            socket._remoteEndPoint = remoteEP;

            // If the _rightEndpoint tracks a UnixDomainSocketEndPoint to delete
            // then create a new EndPoint.
            if (_rightEndPoint is UnixDomainSocketEndPoint unixEndPoint &&
                     unixEndPoint.BoundFileName is not null)
            {
                socket._rightEndPoint = unixEndPoint.CreateUnboundEndPoint();
            }
            else
            {
                socket._rightEndPoint = _rightEndPoint;
            }

            // If the listener socket was bound to a wildcard address, then the `accept` system call
            // will assign a specific address to the accept socket's local endpoint instead of a
            // wildcard address. In that case we should not copy listener's wildcard local endpoint.

            socket._localEndPoint = !IsWildcardEndPoint(_localEndPoint) ? _localEndPoint : null;

            // The socket is connected.
            socket.SetToConnected();

            // if the socket is returned by an End(), the socket might have
            // inherited the WSAEventSelect() call from the accepting socket.
            // we need to cancel this otherwise the socket will be in non-blocking
            // mode and we cannot force blocking mode using the ioctlsocket() in
            // Socket.set_Blocking(), since it fails returning 10022 as documented in MSDN.
            // (note that the m_AsyncEvent event will not be created in this case.

            socket._willBlock = _willBlock;

            // We need to make sure the Socket is in the right blocking state
            // even if we don't have to call UnsetAsyncEventSelect
            socket.InternalSetBlocking(_willBlock);

            return socket;
        }

        internal void SetToConnected()
        {
            if (_isConnected)
            {
                // Socket was already connected.
                return;
            }

            Debug.Assert(_nonBlockingConnectInProgress == false);

            // Update the status: this socket was indeed connected at
            // some point in time update the perf counter as well.
            _isConnected = true;
            _isDisconnected = false;
            _rightEndPoint ??= _pendingConnectRightEndPoint;
            _pendingConnectRightEndPoint = null;
            UpdateLocalEndPointOnConnect();
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "now connected");
        }

        private void UpdateLocalEndPointOnConnect()
        {
            // If the client socket was bound to a wildcard address, then the `connect` system call
            // will assign a specific address to the client socket's local endpoint instead of a
            // wildcard address. In that case we should clear the cached wildcard local endpoint.

            if (IsWildcardEndPoint(_localEndPoint))
            {
                _localEndPoint = null;
            }
        }

        private static bool IsWildcardEndPoint(EndPoint? endPoint)
        {
            if (endPoint == null)
            {
                return false;
            }

            if (endPoint is IPEndPoint ipEndpoint)
            {
                IPAddress address = ipEndpoint.Address;
                return IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address) || s_IPAddressAnyMapToIPv6.Equals(address);
            }

            return false;
        }

        internal void SetToDisconnected()
        {
            if (!_isConnected)
            {
                // Socket was already disconnected.
                return;
            }

            // Update the status: this socket was indeed disconnected at
            // some point in time, clear any async select bits.
            _isConnected = false;
            _isDisconnected = true;

            if (!Disposed)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "!Disposed");
            }
        }

        private void UpdateStatusAfterSocketOptionErrorAndThrowException(SocketError error, [CallerMemberName] string? callerName = null)
        {
            // Don't disconnect socket for unknown options.
            bool disconnectOnFailure = error != SocketError.ProtocolOption &&
                                       error != SocketError.OperationNotSupported;
            UpdateStatusAfterSocketErrorAndThrowException(error, disconnectOnFailure, callerName);
        }

        private void UpdateStatusAfterSocketErrorAndThrowException(SocketError error, bool disconnectOnFailure = true, [CallerMemberName] string? callerName = null)
        {
            // Update the internal state of this socket according to the error before throwing.
            var socketException = new SocketException((int)error);
            UpdateStatusAfterSocketError(socketException, disconnectOnFailure);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, socketException, memberName: callerName);
            throw socketException;
        }

        // UpdateStatusAfterSocketError(socketException) - updates the status of a connected socket
        // on which a failure occurred. it'll go to winsock and check if the connection
        // is still open and if it needs to update our internal state.
        internal void UpdateStatusAfterSocketError(SocketException socketException, bool disconnectOnFailure = true)
        {
            UpdateStatusAfterSocketError(socketException.SocketErrorCode, disconnectOnFailure);
        }

        internal void UpdateStatusAfterSocketError(SocketError errorCode, bool disconnectOnFailure = true)
        {
            // If we already know the socket is disconnected
            // we don't need to do anything else.
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"errorCode:{errorCode}, disconnectOnFailure:{disconnectOnFailure}");

            if (disconnectOnFailure && _isConnected && (_handle.IsInvalid || (errorCode != SocketError.WouldBlock &&
                    errorCode != SocketError.IOPending && errorCode != SocketError.NoBufferSpaceAvailable &&
                    errorCode != SocketError.TimedOut)))
            {
                // The socket is no longer a valid socket.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Invalidating socket.");
                SetToDisconnected();
            }
        }

        private bool CheckErrorAndUpdateStatus(SocketError errorCode)
        {
            if (errorCode == SocketError.Success || errorCode == SocketError.IOPending)
            {
                return true;
            }

            UpdateStatusAfterSocketError(errorCode);
            return false;
        }

        // Called in Receive(Message)From variants to validate 'remoteEndPoint',
        // and check whether the socket is bound.
        private void ValidateReceiveFromEndpointAndState(EndPoint remoteEndPoint, string remoteEndPointArgumentName)
        {
            ArgumentNullException.ThrowIfNull(remoteEndPoint, remoteEndPointArgumentName);
            if (!CanTryAddressFamily(remoteEndPoint.AddressFamily))
            {
                throw new ArgumentException(SR.Format(SR.net_InvalidEndPointAddressFamily, remoteEndPoint.AddressFamily, _addressFamily), remoteEndPointArgumentName);
            }
            if (_rightEndPoint == null)
            {
                throw new InvalidOperationException(SR.net_sockets_mustbind);
            }
        }

        // ValidateBlockingMode - called before synchronous calls to validate
        // the fact that we are in blocking mode (not in non-blocking mode) so the
        // call will actually be synchronous.
        private void ValidateBlockingMode()
        {
            if (_willBlock && !_willBlockInternal)
            {
                throw new InvalidOperationException(SR.net_invasync);
            }
        }

        // Validates that the Socket can be used to try another Connect call, in case
        // a previous call failed and the platform does not support that.  In some cases,
        // the call may also be able to "fix" the Socket to continue working, even if the
        // platform wouldn't otherwise support it.  Windows always supports this.
        partial void ValidateForMultiConnect(bool isMultiEndpoint);

        // Helper for SendFile implementations
        private static SafeFileHandle? OpenFileHandle(string? name) => string.IsNullOrEmpty(name) ? null : File.OpenHandle(name, FileMode.Open, FileAccess.Read);

        private void UpdateReceiveSocketErrorForDisposed(ref SocketError socketError, int bytesTransferred)
        {
            // We use bytesTransferred for checking Disposed.
            // When there is a SocketError, bytesTransferred is zero.
            // An interrupted UDP receive on Linux returns SocketError.Success and bytesTransferred zero.
            if (bytesTransferred == 0 && Disposed)
            {
                socketError = IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted;
            }
        }

        private void UpdateSendSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted;
            }
        }

        private void UpdateConnectSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = SocketError.NotSocket;
            }
        }

        private void UpdateAcceptSocketErrorForDisposed(ref SocketError socketError)
        {
            if (Disposed)
            {
                socketError = SocketError.Interrupted;
            }
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(Disposed, this);
        }

        private bool IsConnectionOriented => _socketType == SocketType.Stream;

        internal static void SocketListDangerousReleaseRefs(IList? socketList, ref int refsAdded)
        {
            if (socketList == null)
            {
                return;
            }

            for (int i = 0; (i < socketList.Count) && (refsAdded > 0); i++)
            {
                Socket socket = (Socket)socketList[i]!;
                socket.InternalSafeHandle.DangerousRelease();
                refsAdded--;
            }
        }

        private static SocketError GetSocketErrorFromFaultedTask(Task t)
        {
            Debug.Assert(t.IsCanceled || t.IsFaulted);

            if (t.IsCanceled)
            {
                return SocketError.OperationAborted;
            }

            Debug.Assert(t.Exception != null);
            return t.Exception.InnerException switch
            {
                SocketException se => se.SocketErrorCode,
                ObjectDisposedException => SocketError.OperationAborted,
                OperationCanceledException => SocketError.OperationAborted,
                _ => SocketError.SocketError
            };
        }

        private void CheckNonBlockingConnectCompleted()
        {
            if (_nonBlockingConnectInProgress && SocketPal.HasNonBlockingConnectCompleted(_handle, out bool success))
            {
                _nonBlockingConnectInProgress = false;

                if (success)
                {
                    SetToConnected();
                }
            }
        }
    }
}
