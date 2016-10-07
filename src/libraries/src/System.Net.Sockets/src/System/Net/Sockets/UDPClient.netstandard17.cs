// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public partial class UdpClient
    {
        public UdpClient(string hostname, int port)
        {
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }
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
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (hostname == null)
            {
                throw new ArgumentNullException(nameof(hostname));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            // We must now look for addresses that use a compatible address family to the client socket. However, in the 
            // case of the <hostname,port> constructor we will have deferred creating the socket and will do that here 
            // instead.

            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

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
                                    ipv6Socket.Close();
                                }
                            }
                            else if (ipv6Socket != null)
                            {
                                ipv6Socket.Connect(address, port);
                                _clientSocket = ipv6Socket;
                                if (ipv4Socket != null)
                                {
                                    ipv4Socket.Close();
                                }
                            }


                            _family = address.AddressFamily;
                            _active = true;
                            break;
                        }
                        else if (address.AddressFamily == _family)
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
                    if (ipv6Socket != null)
                    {
                        ipv6Socket.Close();
                    }

                    if (ipv4Socket != null)
                    {
                        ipv4Socket.Close();
                    }

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
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (addr == null)
            {
                throw new ArgumentNullException(nameof(addr));
            }
            if (!TcpValidationHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            IPEndPoint endPoint = new IPEndPoint(addr, port);

            Connect(endPoint);
        }

        public void Connect(IPEndPoint endPoint)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            CheckForBroadcast(endPoint.Address);
            Client.Connect(endPoint);
            _active = true;
        }

        public byte[] Receive(ref IPEndPoint remoteEP)
        {
            //
            // parameter validation
            //
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }

            // this is a fix due to the nature of the ReceiveFrom() call and the 
            // ref parameter convention, we need to cast an IPEndPoint to it's base
            // class EndPoint and cast it back down to IPEndPoint. ugly but it works.
            //
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
        public int Send(byte[] dgram, int bytes, IPEndPoint endPoint)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException(nameof(dgram));
            }
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


        // Sends a UDP datagram to the specified port on the specified remote host.
        public int Send(byte[] dgram, int bytes, string hostname, int port)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException(nameof(dgram));
            }
            if (_active && ((hostname != null) || (port != 0)))
            {
                // Do not allow sending packets to arbitrary host when connected
                throw new InvalidOperationException(SR.net_udpconnected);
            }

            if (hostname == null || port == 0)
            {
                return Client.Send(dgram, 0, bytes, SocketFlags.None);
            }

            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

            int i = 0;
            for (; i < addresses.Length && addresses[i].AddressFamily != _family; i++)
            {
                ; // just count the addresses
            }

            if (addresses.Length == 0 || i == addresses.Length)
            {
                throw new ArgumentException(SR.net_invalidAddressList, nameof(hostname));
            }

            CheckForBroadcast(addresses[i]);
            IPEndPoint ipEndPoint = new IPEndPoint(addresses[i], port);
            return Client.SendTo(dgram, 0, bytes, SocketFlags.None, ipEndPoint);
        }


        // Sends a UDP datagram to a remote host.
        public int Send(byte[] dgram, int bytes)
        {
            if (_cleanedUp)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
            if (dgram == null)
            {
                throw new ArgumentNullException(nameof(dgram));
            }
            if (!_active)
            {
                // only allowed on connected socket
                throw new InvalidOperationException(SR.net_notconnected);
            }

            return Client.Send(dgram, 0, bytes, SocketFlags.None);
        }
    }
}
