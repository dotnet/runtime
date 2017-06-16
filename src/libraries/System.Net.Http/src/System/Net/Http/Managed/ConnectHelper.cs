// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Http.Managed
{
    internal static class ConnectHelper
    {
        public static async Task<NetworkStream> ConnectAsync(string host, int port)
        {
            TcpClient client;
            try
            {
                // You would think TcpClient.Connect would just do this, but apparently not.
                // It works for IPv4 addresses but seems to barf on IPv6.
                // I need to explicitly invoke the constructor with AddressFamily = IPv6.
                // TODO: Does this mean that connecting by name will only work with IPv4
                // (since that's the default)?  If so, need to rework this logic
                // to resolve the IPAddress ourselves.  Yuck.
                // TODO: No cancellationToken on ConnectAsync?
                IPAddress ipAddress;
                if (IPAddress.TryParse(host, out ipAddress))
                {
                    client = new TcpClient(ipAddress.AddressFamily);
                    await client.ConnectAsync(ipAddress, port);
                }
                else
                {
                    client = new TcpClient();
                    await client.ConnectAsync(host, port);
                }
            }
            catch (SocketException se)
            {
                throw new HttpRequestException("could not connect", se);
            }

            client.NoDelay = true;

            NetworkStream networkStream = client.GetStream();

            // TODO: Timeouts?
            // Default timeout should be something less than infinity (the Socket default)
            // Timeouts probably need to be configurable
            // However, timeouts are also a huge pain when debugging, so consider that too.
#if false
            // Set default read/write timeouts of 5 seconds.
            networkStream.ReadTimeout = 5000;
            networkStream.WriteTimeout = 5000;
#endif

            return networkStream;
        }
    }
}
