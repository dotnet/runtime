// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal static class ConnectHelper
    {
        public static async ValueTask<Stream> ConnectAsync(string host, int port)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                // TODO #23151: cancellation support?
                await (IPAddress.TryParse(host, out IPAddress address) ?
                    socket.ConnectAsync(address, port) :
                    socket.ConnectAsync(host, port)).ConfigureAwait(false);
            }
            catch (SocketException se)
            {
                socket.Dispose();
                throw new HttpRequestException(se.Message, se);
            }

            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
