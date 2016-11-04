// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.Sockets
{
    partial class TcpClient
    {
        private void ConnectCore(string hostname, int port)
        {
            // IPv6: We need to process each of the addresses returned from
            //       DNS when trying to connect. Use of AddressList[0] is
            //       bad form.
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
                        ipv4Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    if (Socket.OSSupportsIPv6)
                    {
                        ipv6Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
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
                        else if (address.AddressFamily == _family)
                        {
                            // Only use addresses with a matching family
                            Connect(new IPEndPoint(address, port));
                            _active = true;
                            break;
                        }
                    }
                    catch (Exception ex) when (!(ex is OutOfMemoryException))
                    {
                        lastex = ex;
                    }
                }
            }

            catch (Exception ex)
            {
                if (ex is OutOfMemoryException)
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

        private void ConnectCore(IPAddress[] ipAddresses, int port)
        {
            Client.Connect(ipAddresses, port);
            _active = true;
        }
    }
}
