// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // The System.Net.Sockets.UdpClient class provides access to UDP services at a higher abstraction
    // level than the System.Net.Sockets.Socket class. System.Net.Sockets.UdpClient is used to
    // connect to a remote host and to receive connections from a remote client.
    public partial class UdpClient : IDisposable
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
            // Validate input parameters.
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // Validate the address family.
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException(SR.net_protocol_invalid_family, nameof(family));
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
            // Validate input parameters.
            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }
            
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
                Debug.Assert(_clientSocket != null);
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

        public void AllowNatTraversal(bool allowed)
        {
            _clientSocket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
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

            Socket chkClientSocket = _clientSocket;
            if (chkClientSocket != null)
            {
                // If the NetworkStream wasn't retrieved, the Socket might
                // still be there and needs to be closed to release the effect
                // of the Bind() call and free the bound IPEndPoint.
                chkClientSocket.InternalShutdown(SocketShutdown.Both);
                chkClientSocket.Dispose();
                _clientSocket = null;
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
                if (NetEventSource.IsEnabled) NetEventSource.Info(this);
                FreeResources();
                GC.SuppressFinalize(this);
            }
        }

        private bool _isBroadcast;
        private void CheckForBroadcast(IPAddress ipAddress)
        {
            // Here we check to see if the user is trying to use a Broadcast IP address
            // we only detect IPAddress.Broadcast (which is not the only Broadcast address)
            // and in that case we set SocketOptionName.Broadcast on the socket to allow its use.
            // if the user really wants complete control over Broadcast addresses he needs to
            // inherit from UdpClient and gain control over the Socket and do whatever is appropriate.
            if (_clientSocket != null && !_isBroadcast && IsBroadcast(ipAddress))
            {
                // We need to set the Broadcast socket option.
                // Note that once we set the option on the Socket we never reset it.
                _isBroadcast = true;
                _clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
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

        public IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint endPoint, AsyncCallback requestCallback, object state)
        {
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (datagram == null)
            {
                throw new ArgumentNullException(nameof(datagram));
            }

            if (bytes > datagram.Length || bytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

            if (_active && endPoint != null)
            {
                // Do not allow sending packets to arbitrary host when connected.
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (endPoint == null)
            {
                return _clientSocket.BeginSend(datagram, 0, bytes, SocketFlags.None, requestCallback, state);
            }

            CheckForBroadcast(endPoint.Address);

            return _clientSocket.BeginSendTo(datagram, 0, bytes, SocketFlags.None, endPoint, requestCallback, state);
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
                    throw new ArgumentException(SR.net_invalidAddressList, nameof(hostname));
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
                return _clientSocket.EndSend(asyncResult);
            }
            else
            {
                return _clientSocket.EndSendTo(asyncResult);
            }
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

            return _clientSocket.BeginReceiveFrom(_buffer, 0, MaxUDPSize, SocketFlags.None, ref tempRemoteEP, requestCallback, state);
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
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            if (multicastAddr == null)
            {
                throw new ArgumentNullException(nameof(multicastAddr));
            }

            // IPv6 Changes: we need to create the correct MulticastOption and
            //               must also check for address family compatibility.
            if (multicastAddr.AddressFamily != _family)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), nameof(multicastAddr));
            }

            if (_family == AddressFamily.InterNetwork)
            {
                MulticastOption mcOpt = new MulticastOption(multicastAddr);

                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    mcOpt);
            }
            else
            {
                IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr);

                _clientSocket.SetSocketOption(
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

            _clientSocket.SetSocketOption(
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
                throw new ArgumentNullException(nameof(multicastAddr));
            }

            if (ifindex < 0)
            {
                throw new ArgumentException(SR.net_value_cannot_be_negative, nameof(ifindex));
            }

            // Ensure that this is an IPv6 client, otherwise throw WinSock 
            // Operation not supported socked exception.
            if (_family != AddressFamily.InterNetworkV6)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr, ifindex);

            _clientSocket.SetSocketOption(
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
                throw new ArgumentNullException(nameof(multicastAddr));
            }
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
            // Validate input parameters.
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (multicastAddr == null)
            {
                throw new ArgumentNullException(nameof(multicastAddr));
            }

            // IPv6 Changes: we need to create the correct MulticastOption and
            //               must also check for address family compatibility.
            if (multicastAddr.AddressFamily != _family)
            {
                throw new ArgumentException(SR.Format(SR.net_protocol_invalid_multicast_family, "UDP"), nameof(multicastAddr));
            }

            if (_family == AddressFamily.InterNetwork)
            {
                MulticastOption mcOpt = new MulticastOption(multicastAddr);

                _clientSocket.SetSocketOption(
                    SocketOptionLevel.IP,
                    SocketOptionName.DropMembership,
                    mcOpt);
            }
            else
            {
                IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr);

                _clientSocket.SetSocketOption(
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
                throw new ArgumentNullException(nameof(multicastAddr));
            }

            if (ifindex < 0)
            {
                throw new ArgumentException(SR.net_value_cannot_be_negative, nameof(ifindex));
            }

            // Ensure that this is an IPv6 client, otherwise throw WinSock 
            // Operation not supported socked exception.
            if (_family != AddressFamily.InterNetworkV6)
            {
                throw new SocketException((int)SocketError.OperationNotSupported);
            }

            IPv6MulticastOption mcOpt = new IPv6MulticastOption(multicastAddr, ifindex);

            _clientSocket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.DropMembership,
                mcOpt);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes)
        {
            return Task<int>.Factory.FromAsync(
                (targetDatagram, targetBytes, callback, state) => ((UdpClient)state).BeginSend(targetDatagram, targetBytes, callback, state),
                asyncResult => ((UdpClient)asyncResult.AsyncState).EndSend(asyncResult),
                datagram,
                bytes,
                state: this);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint endPoint)
        {
            return Task<int>.Factory.FromAsync(
                (targetDatagram, targetBytes, targetEndpoint, callback, state) => ((UdpClient)state).BeginSend(targetDatagram, targetBytes, targetEndpoint, callback, state),
                asyncResult => ((UdpClient)asyncResult.AsyncState).EndSend(asyncResult),
                datagram,
                bytes,
                endPoint,
                state: this);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port)
        {
            Tuple<byte[], string> packedArguments = Tuple.Create(datagram, hostname);

            return Task<int>.Factory.FromAsync(
                (targetPackedArguments, targetBytes, targetPort, callback, state) =>
                {
                    byte[] targetDatagram = targetPackedArguments.Item1;
                    string targetHostname = targetPackedArguments.Item2;
                    var client = (UdpClient)state;

                    return client.BeginSend(targetDatagram, targetBytes, targetHostname, targetPort, callback, state);
                },
                asyncResult => ((UdpClient)asyncResult.AsyncState).EndSend(asyncResult),
                packedArguments,
                bytes,
                port,
                state: this);
        }

        public Task<UdpReceiveResult> ReceiveAsync()
        {
            return Task<UdpReceiveResult>.Factory.FromAsync(
                (callback, state) => ((UdpClient)state).BeginReceive(callback, state),
                asyncResult =>
                {
                    var client = (UdpClient)asyncResult.AsyncState;
                    IPEndPoint remoteEP = null;
                    byte[] buffer = client.EndReceive(asyncResult, ref remoteEP);
                    return new UdpReceiveResult(buffer, remoteEP);
                },
                state: this);
        }

        private void CreateClientSocket()
        {
            // Common initialization code.
            //
            // IPv6 Changes: Use the AddressFamily of this class rather than hardcode.
            _clientSocket = new Socket(_family, SocketType.Dgram, ProtocolType.Udp);
        }
    }
}
