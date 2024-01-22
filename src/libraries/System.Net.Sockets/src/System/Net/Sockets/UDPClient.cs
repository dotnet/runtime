// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.UdpClient class provides access to UDP services at a higher abstraction
    // level than the System.Net.Sockets.Socket class. System.Net.Sockets.UdpClient is used to
    // connect to a remote host and to receive connections from a remote client.
    public partial class UdpClient : IDisposable
    {
        private const int MaxUDPSize = 0x10000;

        private Socket _clientSocket = null!; // initialized by helper called from ctor
        private bool _active;
        private readonly byte[] _buffer = new byte[MaxUDPSize];
        private AddressFamily _family = AddressFamily.InterNetwork;

        // Initializes a new instance of the System.Net.Sockets.UdpClientclass.
        public UdpClient() : this(AddressFamily.InterNetwork)
        {
        }

        // Initializes a new instance of the System.Net.Sockets.UdpClientclass.
        public UdpClient(AddressFamily family)
        {
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "UDP"), nameof(family));
            }

            _family = family;

            CreateClientSocket();
        }

        // Creates a new instance of the UdpClient class that communicates on the
        // specified port number.
        //
        // NOTE: We should obsolete this. This also breaks IPv6-only scenarios.
        // But fixing it has many complications that we have decided not
        // to fix it and instead obsolete it later.
        public UdpClient(int port) : this(port, AddressFamily.InterNetwork)
        {
        }

        // Creates a new instance of the UdpClient class that communicates on the
        // specified port number.
        public UdpClient(int port, AddressFamily family)
        {
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "UDP"), nameof(family));
            }

            IPEndPoint localEP;
            _family = family;

            if (_family == AddressFamily.InterNetwork)
            {
                localEP = new IPEndPoint(IPAddress.Any, port);
            }
            else
            {
                localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            }

            CreateClientSocket();

            _clientSocket.Bind(localEP);
        }

        // Creates a new instance of the UdpClient class that communicates on the
        // specified end point.
        public UdpClient(IPEndPoint localEP)
        {
            ArgumentNullException.ThrowIfNull(localEP);

            // IPv6 Changes: Set the AddressFamily of this object before
            //               creating the client socket.
            _family = localEP.AddressFamily;

            CreateClientSocket();

            _clientSocket.Bind(localEP);
        }

        // Used by the class to indicate that a connection to a remote host has been made.
        protected bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
            }
        }

        public int Available
        {
            get
            {
                return _clientSocket.Available;
            }
        }

        public Socket Client
        {
            get
            {
                return _clientSocket;
            }
            set
            {
                _clientSocket = value;
            }
        }

        public short Ttl
        {
            get
            {
                return _clientSocket.Ttl;
            }
            set
            {
                _clientSocket.Ttl = value;
            }
        }

        public bool DontFragment
        {
            get
            {
                return _clientSocket.DontFragment;
            }
            set
            {
                _clientSocket.DontFragment = value;
            }
        }

        public bool MulticastLoopback
        {
            get
            {
                return _clientSocket.MulticastLoopback;
            }
            set
            {
                _clientSocket.MulticastLoopback = value;
            }
        }

        public bool EnableBroadcast
        {
            get
            {
                return _clientSocket.EnableBroadcast;
            }
            set
            {
                _clientSocket.EnableBroadcast = value;
            }
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return _clientSocket.ExclusiveAddressUse;
            }
            set
            {
                _clientSocket.ExclusiveAddressUse = value;
            }
        }

        [SupportedOSPlatform("windows")]
        public void AllowNatTraversal(bool allowed)
        {
            _clientSocket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
        }

        private bool _disposed;

        private bool IsAddressFamilyCompatible(AddressFamily family)
        {
            // Check if the provided address family is compatible with the socket address family
            if (family == _family)
            {
                return true;
            }

            if (family == AddressFamily.InterNetwork)
            {
                return _family == AddressFamily.InterNetworkV6 && _clientSocket.DualMode;
            }

            return false;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);

                // The only resource we need to free is the network stream, since this
                // is based on the client socket, closing the stream will cause us
                // to flush the data to the network, close the stream and (in the
                // NetoworkStream code) close the socket as well.
                if (_disposed)
                {
                    return;
                }

                Socket chkClientSocket = _clientSocket;
                if (chkClientSocket != null)
                {
                    // If the NetworkStream wasn't retrieved, the Socket might
                    // still be there and needs to be closed to release the effect
                    // of the Bind() call and free the bound IPEndPoint.
                    chkClientSocket.InternalShutdown(SocketShutdown.Both);
                    chkClientSocket.Dispose();
                    _clientSocket = null!;
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        private bool _isBroadcast;
        private void CheckForBroadcast(IPAddress ipAddress)
        {
            // Here we check to see if the user is trying to use a Broadcast IP address
            // we only detect IPAddress.Broadcast (which is not the only Broadcast address)
            // and in that case we set SocketOptionName.Broadcast on the socket to allow its use.
            // if the user really wants complete control over Broadcast addresses they need to
            // inherit from UdpClient and gain control over the Socket and do whatever is appropriate.
            if (_clientSocket != null && !_isBroadcast && IsBroadcast(ipAddress))
            {
                // We need to set the Broadcast socket option.
                // Note that once we set the option on the Socket we never reset it.
                _isBroadcast = true;
                _clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            }
        }

        private static bool IsBroadcast(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // No such thing as a broadcast address for IPv6.
                return false;
            }
            else
            {
                return address.Equals(IPAddress.Broadcast);
            }
        }

        public IAsyncResult BeginSend(byte[] datagram, int bytes, AsyncCallback? requestCallback, object? state) =>
            BeginSend(datagram, bytes, null, requestCallback, state);

        public IAsyncResult BeginSend(byte[] datagram, int bytes, string? hostname, int port, AsyncCallback? requestCallback, object? state) =>
            BeginSend(datagram, bytes, GetEndpoint(hostname, port), requestCallback, state);

        public IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint? endPoint, AsyncCallback? requestCallback, object? state)
        {
            ValidateDatagram(datagram, bytes, endPoint);

            if (endPoint is null)
            {
                return _clientSocket.BeginSend(datagram, 0, bytes, SocketFlags.None, requestCallback, state);
            }
            else
            {
                CheckForBroadcast(endPoint.Address);
                return _clientSocket.BeginSendTo(datagram, 0, bytes, SocketFlags.None, endPoint, requestCallback, state);
            }
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            ThrowIfDisposed();

            return _active ?
                _clientSocket.EndSend(asyncResult) :
                _clientSocket.EndSendTo(asyncResult);
        }

        private void ValidateDatagram(byte[] datagram, int bytes, IPEndPoint? endPoint)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(datagram);

            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(bytes, datagram.Length);

            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }
        }

        private IPEndPoint? GetEndpoint(string? hostname, int port)
        {
            if (_active && ((hostname != null) || (port != 0)))
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            IPEndPoint? ipEndPoint = null;
            if (hostname != null && port != 0)
            {
                IPAddress[] addresses = Dns.GetHostAddresses(hostname);

                int i = 0;
                for (; i < addresses.Length && !IsAddressFamilyCompatible(addresses[i].AddressFamily); i++)
                {
                }

                if (addresses.Length == 0 || i == addresses.Length)
                {
                    throw new ArgumentException(SR.net_invalidAddressList, nameof(hostname));
                }

                CheckForBroadcast(addresses[i]);
                ipEndPoint = new IPEndPoint(addresses[i], port);
            }

            return ipEndPoint;
        }

        public IAsyncResult BeginReceive(AsyncCallback? requestCallback, object? state)
        {
            ThrowIfDisposed();

            // Due to the nature of the ReceiveFrom() call and the ref parameter convention,
            // we need to cast an IPEndPoint to its base class EndPoint and cast it back down
            // to IPEndPoint.
            EndPoint tempRemoteEP = _family == AddressFamily.InterNetwork ?
                IPEndPointStatics.Any :
                IPEndPointStatics.IPv6Any;

            return _clientSocket.BeginReceiveFrom(_buffer, 0, MaxUDPSize, SocketFlags.None, ref tempRemoteEP, requestCallback, state);
        }

        public byte[] EndReceive(IAsyncResult asyncResult, ref IPEndPoint? remoteEP)
        {
            ThrowIfDisposed();

            EndPoint tempRemoteEP = _family == AddressFamily.InterNetwork ?
                IPEndPointStatics.Any :
                IPEndPointStatics.IPv6Any;

            int received = _clientSocket.EndReceiveFrom(asyncResult, ref tempRemoteEP);
            remoteEP = (IPEndPoint)tempRemoteEP;

            // Because we don't return the actual length, we need to ensure the returned buffer
            // has the appropriate length.
            if (received < MaxUDPSize)
            {
                byte[] newBuffer = new byte[received];
                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, received);
                return newBuffer;
            }

            return _buffer;
        }

        // Joins a multicast address group.
        public void JoinMulticastGroup(IPAddress multicastAddr)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(multicastAddr);
            if (multicastAddr.AddressFamily != _family)
            {
                // For IPv6, we need to create the correct MulticastOption and must also check for address family compatibility.
                // Note: we cannot reliably use IPv4 multicast over IPv6 in DualMode, as such we keep the compatibility explicit between IP stack versions
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), nameof(multicastAddr));
            }

            if (_family == AddressFamily.InterNetwork)
            {
                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    new MulticastOption(multicastAddr));
            }
            else
            {
                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.AddMembership,
                    new IPv6MulticastOption(multicastAddr));
            }
        }

        public void JoinMulticastGroup(IPAddress multicastAddr, IPAddress localAddress)
        {
            ThrowIfDisposed();

            if (_family != AddressFamily.InterNetwork)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            _clientSocket.SetSocketOption(
               SocketOptionLevel.IP,
               SocketOptionName.AddMembership,
               new MulticastOption(multicastAddr, localAddress));
        }

        // Joins an IPv6 multicast address group.
        public void JoinMulticastGroup(int ifindex, IPAddress multicastAddr)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(multicastAddr);
            ArgumentOutOfRangeException.ThrowIfNegative(ifindex);
            if (_family != AddressFamily.InterNetworkV6)
            {
                // Ensure that this is an IPv6 client, otherwise throw WinSock
                // Operation not supported socked exception.
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            _clientSocket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.AddMembership,
                new IPv6MulticastOption(multicastAddr, ifindex));
        }

        // Joins a multicast address group with the specified time to live (TTL).
        public void JoinMulticastGroup(IPAddress multicastAddr, int timeToLive)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(multicastAddr);
            if (!RangeValidationHelpers.ValidateRange(timeToLive, 0, 255))
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLive));
            }

            // Join the Multicast Group.
            JoinMulticastGroup(multicastAddr);

            // Set Time To Live (TTL).
            _clientSocket.SetSocketOption(
                (_family == AddressFamily.InterNetwork) ? SocketOptionLevel.IP : SocketOptionLevel.IPv6,
                SocketOptionName.MulticastTimeToLive,
                timeToLive);
        }

        // Leaves a multicast address group.
        public void DropMulticastGroup(IPAddress multicastAddr)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(multicastAddr);
            if (multicastAddr.AddressFamily != _family)
            {
                // For IPv6, we need to create the correct MulticastOption and must also check for address family compatibility.
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), nameof(multicastAddr));
            }

            if (_family == AddressFamily.InterNetwork)
            {
                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.DropMembership,
                    new MulticastOption(multicastAddr));
            }
            else
            {
                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.DropMembership,
                    new IPv6MulticastOption(multicastAddr));
            }
        }

        // Leaves an IPv6 multicast address group.
        public void DropMulticastGroup(IPAddress multicastAddr, int ifindex)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(multicastAddr);
            ArgumentOutOfRangeException.ThrowIfNegative(ifindex);
            if (_family != AddressFamily.InterNetworkV6)
            {
                // Ensure that this is an IPv6 client.
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            _clientSocket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.DropMembership,
                new IPv6MulticastOption(multicastAddr, ifindex));
        }

        public Task<int> SendAsync(byte[] datagram, int bytes) =>
            SendAsync(datagram, bytes, null);

        /// <summary>
        /// Sends a UDP datagram asynchronously to a remote host.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlyMemory{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is None.
        /// </param>
        /// <returns>A <see cref="ValueTask{T}"/> that represents the asynchronous send operation. The value of its Result property contains the number of bytes sent.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default) =>
            SendAsync(datagram, null, cancellationToken);

        public Task<int> SendAsync(byte[] datagram, int bytes, string? hostname, int port) =>
            SendAsync(datagram, bytes, GetEndpoint(hostname, port));

        /// <summary>
        /// Sends a UDP datagram asynchronously to a remote host.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlyMemory{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <param name="hostname">
        /// The name of the remote host to which you intend to send the datagram.
        /// </param>
        /// <param name="port">
        /// The remote port number with which you intend to communicate.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is None.
        /// </param>
        /// <returns>A <see cref="ValueTask{T}"/> that represents the asynchronous send operation. The value of its Result property contains the number of bytes sent.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="UdpClient"/> has already established a default remote host.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, string? hostname, int port, CancellationToken cancellationToken = default) =>
            SendAsync(datagram, GetEndpoint(hostname, port), cancellationToken);

        public Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint? endPoint)
        {
            ValidateDatagram(datagram, bytes, endPoint);

            if (endPoint is null)
            {
                return _clientSocket.SendAsync(new ArraySegment<byte>(datagram, 0, bytes), SocketFlags.None);
            }
            else
            {
                CheckForBroadcast(endPoint.Address);
                return _clientSocket.SendToAsync(new ArraySegment<byte>(datagram, 0, bytes), SocketFlags.None, endPoint);
            }
        }

        /// <summary>
        /// Sends a UDP datagram asynchronously to a remote host.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlyMemory{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <param name="endPoint">
        /// An <see cref="IPEndPoint"/> that represents the host and port to which to send the datagram.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests. The default value is None.
        /// </param>
        /// <returns>A <see cref="ValueTask{T}"/> that represents the asynchronous send operation. The value of its Result property contains the number of bytes sent.</returns>
        /// <exception cref="InvalidOperationException"><see cref="UdpClient"/> has already established a default remote host and <paramref name="endPoint"/> is not <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, IPEndPoint? endPoint, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (endPoint is null)
            {
                return _clientSocket.SendAsync(datagram, SocketFlags.None, cancellationToken);
            }
            if (_active)
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }
            CheckForBroadcast(endPoint.Address);
            return _clientSocket.SendToAsync(datagram, SocketFlags.None, endPoint, cancellationToken);
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            ThrowIfDisposed();

            return WaitAndWrap(_clientSocket.ReceiveFromAsync(
                new ArraySegment<byte>(_buffer, 0, MaxUDPSize),
                SocketFlags.None,
                _family == AddressFamily.InterNetwork ? IPEndPointStatics.Any : IPEndPointStatics.IPv6Any));

            async Task<UdpReceiveResult> WaitAndWrap(Task<SocketReceiveFromResult> task)
            {
                SocketReceiveFromResult result = await task.ConfigureAwait(false);

                byte[] buffer = result.ReceivedBytes < MaxUDPSize ?
                    _buffer.AsSpan(0, result.ReceivedBytes).ToArray() :
                    _buffer;

                return new UdpReceiveResult(buffer, (IPEndPoint)result.RemoteEndPoint);
            }
        }

        /// <summary>
        /// Returns a UDP datagram asynchronously that was sent by a remote host.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// </param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">The underlying <see cref="Socket"/> has been closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return WaitAndWrap(_clientSocket.ReceiveFromAsync(
                _buffer,
                SocketFlags.None,
                _family == AddressFamily.InterNetwork ? IPEndPointStatics.Any : IPEndPointStatics.IPv6Any, cancellationToken));

            async ValueTask<UdpReceiveResult> WaitAndWrap(ValueTask<SocketReceiveFromResult> task)
            {
                SocketReceiveFromResult result = await task.ConfigureAwait(false);

                byte[] buffer = result.ReceivedBytes < MaxUDPSize ?
                    _buffer.AsSpan(0, result.ReceivedBytes).ToArray() :
                    _buffer;

                return new UdpReceiveResult(buffer, (IPEndPoint)result.RemoteEndPoint);
            }
        }

        private void CreateClientSocket()
        {
            // Common initialization code.
            //
            // IPv6 Changes: Use the AddressFamily of this class rather than hardcode.
            _clientSocket = new Socket(_family, SocketType.Dgram, ProtocolType.Udp);
        }

        public UdpClient(string hostname, int port)
        {
            ArgumentNullException.ThrowIfNull(hostname);
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // NOTE: Need to create different kinds of sockets based on the addresses
            //       returned from DNS. As a result, we defer the creation of the
            //       socket until the Connect method.

            Connect(hostname, port);
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Connect(string hostname, int port)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(hostname);
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // We must now look for addresses that use a compatible address family to the client socket. However, in the
            // case of the <hostname,port> constructor we will have deferred creating the socket and will do that here
            // instead.

            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

            Exception? lastex = null;
            Socket? ipv6Socket = null;
            Socket? ipv4Socket = null;

            try
            {
                if (_clientSocket == null)
                {
                    if (Socket.OSSupportsIPv4)
                    {
                        ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    }
                    if (Socket.OSSupportsIPv6)
                    {
                        ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                    }
                }


                foreach (IPAddress address in addresses)
                {
                    try
                    {
                        if (_clientSocket == null)
                        {
                            // We came via the <hostname,port> constructor. Set the
                            // address family appropriately, create the socket and
                            // try to connect.
                            if (address.AddressFamily == AddressFamily.InterNetwork && ipv4Socket != null)
                            {
                                ipv4Socket.Connect(address, port);
                                _clientSocket = ipv4Socket;
                                ipv6Socket?.Close();
                            }
                            else if (ipv6Socket != null)
                            {
                                ipv6Socket.Connect(address, port);
                                _clientSocket = ipv6Socket;
                                ipv4Socket?.Close();
                            }


                            _family = address.AddressFamily;
                            _active = true;
                            break;
                        }
                        else if (IsAddressFamilyCompatible(address.AddressFamily))
                        {
                            // Only use addresses with a matching family
                            Connect(new IPEndPoint(address, port));
                            _active = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ExceptionCheck.IsFatal(ex))
                        {
                            throw;
                        }
                        lastex = ex;
                    }
                }
            }

            catch (Exception ex)
            {
                if (ExceptionCheck.IsFatal(ex))
                {
                    throw;
                }
                lastex = ex;
            }
            finally
            {
                //cleanup temp sockets if failed
                //main socket gets closed when tcpclient gets closed

                //did we connect?
                if (!_active)
                {
                    ipv6Socket?.Close();
                    ipv4Socket?.Close();

                    // The connect failed - rethrow the last error we had
                    if (lastex != null)
                    {
                        throw lastex;
                    }
                    else
                    {
                        throw new SocketException((int)SocketError.NotConnected);
                    }
                }
            }
        }

        public void Connect(IPAddress addr, int port)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(addr);
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            IPEndPoint endPoint = new IPEndPoint(addr, port);

            Connect(endPoint);
        }

        public void Connect(IPEndPoint endPoint)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(endPoint);

            CheckForBroadcast(endPoint.Address);
            Client.Connect(endPoint);
            _active = true;
        }

        public byte[] Receive([NotNull] ref IPEndPoint? remoteEP)
        {
            ThrowIfDisposed();

            // this is a fix due to the nature of the ReceiveFrom() call and the
            // ref parameter convention, we need to cast an IPEndPoint to it's base
            // class EndPoint and cast it back down to IPEndPoint. ugly but it works.
            EndPoint tempRemoteEP = _family == AddressFamily.InterNetwork ?
                IPEndPointStatics.Any :
                IPEndPointStatics.IPv6Any;

            int received = Client.ReceiveFrom(_buffer, MaxUDPSize, 0, ref tempRemoteEP);
            remoteEP = (IPEndPoint)tempRemoteEP;

            // because we don't return the actual length, we need to ensure the returned buffer
            // has the appropriate length.

            if (received < MaxUDPSize)
            {
                byte[] newBuffer = new byte[received];
                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, received);
                return newBuffer;
            }
            return _buffer;
        }


        // Sends a UDP datagram to the host at the remote end point.
        public int Send(byte[] dgram, int bytes, IPEndPoint? endPoint)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(dgram);
            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (endPoint == null)
            {
                return Client.Send(dgram, 0, bytes, SocketFlags.None);
            }

            CheckForBroadcast(endPoint.Address);

            return Client.SendTo(dgram, 0, bytes, SocketFlags.None, endPoint);
        }

        /// <summary>
        /// Sends a UDP datagram to the host at the specified remote endpoint.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlySpan{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <param name="endPoint">
        /// An <see cref="IPEndPoint"/> that represents the host and port to which to send the datagram.
        /// </param>
        /// <returns>The number of bytes sent.</returns>
        /// <exception cref="InvalidOperationException"><see cref="UdpClient"/> has already established a default remote host and <paramref name="endPoint"/> is not <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException"><see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public int Send(ReadOnlySpan<byte> datagram, IPEndPoint? endPoint)
        {
            ThrowIfDisposed();

            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (endPoint == null)
            {
                return Client.Send(datagram, SocketFlags.None);
            }

            CheckForBroadcast(endPoint.Address);

            return Client.SendTo(datagram, SocketFlags.None, endPoint);
        }

        // Sends a UDP datagram to the specified port on the specified remote host.
        public int Send(byte[] dgram, int bytes, string? hostname, int port) => Send(dgram, bytes, GetEndpoint(hostname, port));

        /// <summary>
        /// Sends a UDP datagram to a specified port on a specified remote host.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlySpan{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <param name="hostname">
        /// The name of the remote host to which you intend to send the datagram.
        /// </param>
        /// <param name="port">
        /// The remote port number with which you intend to communicate.
        /// </param>
        /// <returns>The number of bytes sent.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="UdpClient"/> has already established a default remote host.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public int Send(ReadOnlySpan<byte> datagram, string? hostname, int port) => Send(datagram, GetEndpoint(hostname, port));

        // Sends a UDP datagram to a remote host.
        public int Send(byte[] dgram, int bytes)
        {
            ThrowIfDisposed();

            ArgumentNullException.ThrowIfNull(dgram);
            if (!_active)
            {
                // only allowed on connected socket
                throw new InvalidOperationException(SR.net_notconnected);
            }

            return Client.Send(dgram, 0, bytes, SocketFlags.None);
        }

        /// <summary>
        /// Sends a UDP datagram to a remote host.
        /// </summary>
        /// <param name="datagram">
        /// An <see cref="ReadOnlySpan{T}"/> of Type <see cref="byte"/> that specifies the UDP datagram that you intend to send.
        /// </param>
        /// <returns>The number of bytes sent.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="UdpClient"/> has not established a default remote host.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="UdpClient"/> is closed.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        public int Send(ReadOnlySpan<byte> datagram)
        {
            ThrowIfDisposed();

            if (!_active)
            {
                // only allowed on connected socket
                throw new InvalidOperationException(SR.net_notconnected);
            }

            return Client.Send(datagram, SocketFlags.None);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
