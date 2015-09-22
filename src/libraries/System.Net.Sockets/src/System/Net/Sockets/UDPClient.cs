// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.UdpClient class provides access to UDP services at a higher abstraction
    // level than the System.Net.Sockets.Socket class. System.Net.Sockets.UdpClient is used to
    // connect to a remote host and to receive connections from a remote client.
    public class UdpClient : IDisposable
    {
        private const int MaxUDPSize = 0x10000;

        private Socket _clientSocket;
        private bool _active;
        private byte[] _buffer = new byte[MaxUDPSize];
        private AddressFamily _family = AddressFamily.InterNetwork;

        // Initializes a new instance of the System.Net.Sockets.UdpClientclass.
        public UdpClient() : this(AddressFamily.InterNetwork)
        {
        }

        // Initializes a new instance of the System.Net.Sockets.UdpClientclass.
        public UdpClient(AddressFamily family)
        {
            // Validate the address family.
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_family, "UDP"), "family");
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
            // Validate input parameters.
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            // Validate the address family.
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.net_protocol_invalid_family, "family");
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

            Client.Bind(localEP);
        }

        // Creates a new instance of the UdpClient class that communicates on the
        // specified end point.
        public UdpClient(IPEndPoint localEP)
        {
            // Validate input parameters.
            if (localEP == null)
            {
                throw new ArgumentNullException("localEP");
            }

            // IPv6 Changes: Set the AddressFamily of this object before
            //               creating the client socket.
            _family = localEP.AddressFamily;

            CreateClientSocket();

            Client.Bind(localEP);
        }

        // Creates a new instance of the System.Net.Sockets.UdpClient class and connects to the
        // specified remote host on the specified port.
        public UdpClient(string hostname, int port)
        {
            // Validate input parameters.
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            // NOTE: Need to create different kinds of sockets based on the addresses
            //       returned from DNS. As a result, we defer the creation of the 
            //       socket until the Connect method.

            Connect(hostname, port);
        }

        // Used by the class to provide the underlying network socket.
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

        public void AllowNatTraversal(bool allowed)
        {
            if (allowed)
            {
                _clientSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            }
            else
            {
                _clientSocket.SetIPProtectionLevel(IPProtectionLevel.EdgeRestricted);
            }
        }

        private bool _cleanedUp = false;
        private void FreeResources()
        {
            // The only resource we need to free is the network stream, since this
            // is based on the client socket, closing the stream will cause us
            // to flush the data to the network, close the stream and (in the
            // NetoworkStream code) close the socket as well.
            if (_cleanedUp)
            {
                return;
            }

            Socket chkClientSocket = Client;
            if (chkClientSocket != null)
            {
                // If the NetworkStream wasn't retrieved, the Socket might
                // still be there and needs to be closed to release the effect
                // of the Bind() call and free the bound IPEndPoint.
                chkClientSocket.InternalShutdown(SocketShutdown.Both);
                chkClientSocket.Dispose();
                Client = null;
            }
            _cleanedUp = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GlobalLog.Print("UdpClient::Dispose()");
                FreeResources();
                GC.SuppressFinalize(this);
            }
        }

        // Establishes a connection to the specified port on the specified host.
        public void Connect(string hostname, int port)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (hostname == null)
            {
                throw new ArgumentNullException("hostname");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            // IPv6 Changes: instead of just using the first address in the list,
            //               we must now look for addresses that use a compatible
            //               address family to the client socket.
            //               However, in the case of the <hostname,port> constructor
            //               we will have deferred creating the socket and will
            //               do that here instead.
            //               In addition, the redundant CheckForBroadcast call was
            //               removed here since it is called from Connect().
            IPAddress[] addresses = Dns.GetHostAddressesAsync(hostname).GetAwaiter().GetResult();

            Exception lastex = null;
            Socket ipv6Socket = null;
            Socket ipv4Socket = null;
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
                                if (ipv6Socket != null)
                                {
                                    ipv6Socket.Dispose();
                                }
                            }
                            else if (ipv6Socket != null)
                            {
                                ipv6Socket.Connect(address, port);
                                _clientSocket = ipv6Socket;
                                if (ipv4Socket != null)
                                {
                                    ipv4Socket.Dispose();
                                }
                            }

                            _family = address.AddressFamily;
                            _active = true;
                            break;
                        }
                        else if (address.AddressFamily == _family)
                        {
                            // Only use addresses with a matching family.
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
                // Cleanup temp sockets on failure. The main socket gets closed when the UDPClient
                // gets closed.

                // Did we connect?
                if (!_active)
                {
                    if (ipv6Socket != null)
                    {
                        ipv6Socket.Dispose();
                    }

                    if (ipv4Socket != null)
                    {
                        ipv4Socket.Dispose();
                    }

                    // The connect failed - rethrow the last error we had.
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

        // Establishes a connection with the host at the specified address on the
        // specified port.
        public void Connect(IPAddress addr, int port)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (addr == null)
            {
                throw new ArgumentNullException("addr");
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException("port");
            }

            // IPv6 Changes: Removed redundant call to CheckForBroadcast() since
            //               it is made in the real Connect() method.
            IPEndPoint endPoint = new IPEndPoint(addr, port);

            Connect(endPoint);
        }

        // Establishes a connection to a remote end point.
        public void Connect(IPEndPoint endPoint)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }

            // IPv6 Changes: Actually, no changes but we might want to check for 
            //               compatible protocols here rather than push it down
            //               to WinSock.
            CheckForBroadcast(endPoint.Address);
            Client.Connect(endPoint);
            _active = true;
        }


        private bool _isBroadcast;
        private void CheckForBroadcast(IPAddress ipAddress)
        {
            // Here we check to see if the user is trying to use a Broadcast IP address
            // we only detect IPAddress.Broadcast (which is not the only Broadcast address)
            // and in that case we set SocketOptionName.Broadcast on the socket to allow its use.
            // if the user really wants complete control over Broadcast addresses he needs to
            // inherit from UdpClient and gain control over the Socket and do whatever is appropriate.
            if (Client != null && !_isBroadcast && IsBroadcast(ipAddress))
            {
                // We need to set the Broadcast socket option.
                // Note that once we set the option on the Socket we never reset it.
                _isBroadcast = true;
                Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            }
        }

        private bool IsBroadcast(IPAddress address)
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

        // Sends a UDP datagram to the host at the remote end point.
        public int Send(byte[] dgram, int bytes, IPEndPoint endPoint)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException("dgram");
            }
            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (endPoint == null)
            {
                return Client.Send(dgram, 0, bytes, SocketFlags.None);
            }

            CheckForBroadcast(endPoint.Address);

            return Client.SendTo(dgram, 0, bytes, SocketFlags.None, endPoint);
        }


        // Sends a UDP datagram to the specified port on the specified remote host.
        public int Send(byte[] dgram, int bytes, string hostname, int port)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException("dgram");
            }
            if (_active && ((hostname != null) || (port != 0)))
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (hostname == null || port == 0)
            {
                return Client.Send(dgram, 0, bytes, SocketFlags.None);
            }

            IPAddress[] addresses = Dns.GetHostAddressesAsync(hostname).GetAwaiter().GetResult();

            int i = 0;
            for (; i < addresses.Length && addresses[i].AddressFamily != _family; i++) ;

            if (addresses.Length == 0 || i == addresses.Length)
            {
                throw new ArgumentException(SR.net_invalidAddressList, "hostname");
            }

            CheckForBroadcast(addresses[i]);
            IPEndPoint ipEndPoint = new IPEndPoint(addresses[i], port);
            return Client.SendTo(dgram, 0, bytes, SocketFlags.None, ipEndPoint);
        }

        // Sends a UDP datagram to a remote host.
        public int Send(byte[] dgram, int bytes)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException("dgram");
            }
            if (!_active)
            {
                // Only allowed on connected socket.
                throw new InvalidOperationException(SR.net_notconnected);
            }

            return Client.Send(dgram, 0, bytes, SocketFlags.None);
        }

        public IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint endPoint, AsyncCallback requestCallback, object state)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (datagram == null)
            {
                throw new ArgumentNullException("datagram");
            }

            if (bytes > datagram.Length || bytes < 0)
            {
                throw new ArgumentOutOfRangeException("bytes");
            }

            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (endPoint == null)
            {
                return Client.BeginSend(datagram, 0, bytes, SocketFlags.None, requestCallback, state);
            }

            CheckForBroadcast(endPoint.Address);

            return Client.BeginSendTo(datagram, 0, bytes, SocketFlags.None, endPoint, requestCallback, state);
        }

        public IAsyncResult BeginSend(byte[] datagram, int bytes, string hostname, int port, AsyncCallback requestCallback, object state)
        {
            if (_active && ((hostname != null) || (port != 0)))
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            IPEndPoint ipEndPoint = null;
            if (hostname != null && port != 0)
            {
                IPAddress[] addresses = Dns.GetHostAddressesAsync(hostname).GetAwaiter().GetResult();

                int i = 0;
                for (; i < addresses.Length && addresses[i].AddressFamily != _family; i++)
                {
                }

                if (addresses.Length == 0 || i == addresses.Length)
                {
                    throw new ArgumentException(SR.net_invalidAddressList, "hostname");
                }

                CheckForBroadcast(addresses[i]);
                ipEndPoint = new IPEndPoint(addresses[i], port);
            }

            return BeginSend(datagram, bytes, ipEndPoint, requestCallback, state);
        }

        public IAsyncResult BeginSend(byte[] datagram, int bytes, AsyncCallback requestCallback, object state)
        {
            return BeginSend(datagram, bytes, null, requestCallback, state);
        }

        public int EndSend(IAsyncResult asyncResult)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (_active)
            {
                return Client.EndSend(asyncResult);
            }
            else
            {
                return Client.EndSendTo(asyncResult);
            }
        }

        // Returns a datagram sent by a server.
        public byte[] Receive(ref IPEndPoint remoteEP)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            // Due to the nature of the ReceiveFrom() call and the ref parameter convention,
            // we need to cast an IPEndPoint to its base class EndPoint and cast it back down
            // to IPEndPoint.
            EndPoint tempRemoteEP;
            if (_family == AddressFamily.InterNetwork)
            {
                tempRemoteEP = IPEndPointStatics.Any;
            }
            else
            {
                tempRemoteEP = IPEndPointStatics.IPv6Any;
            }

            int received = Client.ReceiveFrom(_buffer, MaxUDPSize, 0, ref tempRemoteEP);
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

        public IAsyncResult BeginReceive(AsyncCallback requestCallback, object state)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            // Due to the nature of the ReceiveFrom() call and the ref parameter convention,
            // we need to cast an IPEndPoint to its base class EndPoint and cast it back down
            // to IPEndPoint.
            EndPoint tempRemoteEP;
            if (_family == AddressFamily.InterNetwork)
            {
                tempRemoteEP = IPEndPointStatics.Any;
            }
            else
            {
                tempRemoteEP = IPEndPointStatics.IPv6Any;
            }

            return Client.BeginReceiveFrom(_buffer, 0, MaxUDPSize, SocketFlags.None, ref tempRemoteEP, requestCallback, state);
        }

        public byte[] EndReceive(IAsyncResult asyncResult, ref IPEndPoint remoteEP)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            EndPoint tempRemoteEP;
            if (_family == AddressFamily.InterNetwork)
            {
                tempRemoteEP = IPEndPointStatics.Any;
            }
            else
            {
                tempRemoteEP = IPEndPointStatics.IPv6Any;
            }

            int received = Client.EndReceiveFrom(asyncResult, ref tempRemoteEP);
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
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (multicastAddr == null)
            {
                throw new ArgumentNullException("multicastAddr");
            }

            // IPv6 Changes: we need to create the correct MulticastOption and
            //               must also check for address family compatibility.
            if (multicastAddr.AddressFamily != _family)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), "multicastAddr");
            }

            if (_family == AddressFamily.InterNetwork)
            {
                MulticastOption mcOpt = new MulticastOption(multicastAddr);

                Client.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    mcOpt);
            }
            else
            {
                IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr);

                Client.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.AddMembership,
                    mcOpt);
            }
        }

        public void JoinMulticastGroup(IPAddress multicastAddr, IPAddress localAddress)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (_family != AddressFamily.InterNetwork)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            MulticastOption mcOpt = new MulticastOption(multicastAddr, localAddress);

            Client.SetSocketOption(
               SocketOptionLevel.IP,
               SocketOptionName.AddMembership,
               mcOpt);
        }

        // Joins an IPv6 multicast address group.
        public void JoinMulticastGroup(int ifindex, IPAddress multicastAddr)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (multicastAddr == null)
            {
                throw new ArgumentNullException("multicastAddr");
            }

            if (ifindex < 0)
            {
                throw new ArgumentException(SR.net_value_cannot_be_negative, "ifindex");
            }

            // Ensure that this is an IPv6 client, otherwise throw WinSock 
            // Operation not supported socked exception.
            if (_family != AddressFamily.InterNetworkV6)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr, ifindex);

            Client.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.AddMembership,
                mcOpt);
        }

        // Joins a multicast address group with the specified time to live (TTL).
        public void JoinMulticastGroup(IPAddress multicastAddr, int timeToLive)
        {
            // parameter validation;
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (multicastAddr == null)
            {
                throw new ArgumentNullException("multicastAddr");
            }
            if (!RangeValidationHelpers.ValidateRange(timeToLive, 0, 255))
            {
                throw new ArgumentOutOfRangeException("timeToLive");
            }

            // Join the Multicast Group.
            JoinMulticastGroup(multicastAddr);

            // Set Time To Live (TTL).
            Client.SetSocketOption(
                (_family == AddressFamily.InterNetwork) ? SocketOptionLevel.IP : SocketOptionLevel.IPv6,
                SocketOptionName.MulticastTimeToLive,
                timeToLive);
        }

        // Leaves a multicast address group.
        public void DropMulticastGroup(IPAddress multicastAddr)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (multicastAddr == null)
            {
                throw new ArgumentNullException("multicastAddr");
            }

            // IPv6 Changes: we need to create the correct MulticastOption and
            //               must also check for address family compatibility.
            if (multicastAddr.AddressFamily != _family)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), "multicastAddr");
            }

            if (_family == AddressFamily.InterNetwork)
            {
                MulticastOption mcOpt = new MulticastOption(multicastAddr);

                Client.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.DropMembership,
                    mcOpt);
            }
            else
            {
                IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr);

                Client.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.DropMembership,
                    mcOpt);
            }
        }

        // Leaves an IPv6 multicast address group.
        public void DropMulticastGroup(IPAddress multicastAddr, int ifindex)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (multicastAddr == null)
            {
                throw new ArgumentNullException("multicastAddr");
            }

            if (ifindex < 0)
            {
                throw new ArgumentException(SR.net_value_cannot_be_negative, "ifindex");
            }

            // Ensure that this is an IPv6 client, otherwise throw WinSock 
            // Operation not supported socked exception.
            if (_family != AddressFamily.InterNetworkV6)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr, ifindex);

            Client.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.DropMembership,
                mcOpt);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return Task<int>.Factory.FromAsync(BeginSend, EndSend, datagram, bytes, null);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            return Task<int>.Factory.FromAsync(BeginSend, EndSend, datagram, bytes, endPoint, null);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port)
        {
            return Task<int>.Factory.FromAsync((callback, state) => BeginSend(datagram, bytes, hostname, port, callback, state), EndSend, null);
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            return Task<UdpReceiveResult>.Factory.FromAsync((callback, state) => BeginReceive(callback, state), (ar) =>
            {
                IPEndPoint remoteEP = null;
                Byte[] buffer = EndReceive(ar, ref remoteEP);
                return new UdpReceiveResult(buffer, remoteEP);
            }, null);
        }

        private void CreateClientSocket()
        {
            // Common initialization code.
            //
            // IPv6 Changes: Use the AddressFamily of this class rather than hardcode.
            Client = new Socket(_family, SocketType.Dgram, ProtocolType.Udp);
        }
    }
}
