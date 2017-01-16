// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class Issue14037
    {
        [OuterLoop]
        [Fact]
        public static void AcceptNonBlocking_Success()
        {
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = listenSocket.BindToAnonymousPort(IPAddress.Loopback);

            listenSocket.Blocking = false;

            listenSocket.Listen(5);

            Task t = Task.Run(() => { DoAccept(listenSocket, 5); });

            // Loop, doing connections and pausing between
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(50);
                Socket connectSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connectSocket.Connect(listenSocket.LocalEndPoint);
            }

            t.Wait();
        }

        public static void DoAccept(Socket listenSocket, int connectionsToAccept)
        {
            int connectionCount = 0;
            while (true) 
            {
                var ls = new List<Socket> { listenSocket };
                Socket.Select(ls, null, null, 1000000);
                if (ls.Count > 0)
                {
                    while (true)
                    {
                        try
                        {
                            var newConnection = listenSocket.Accept();
                            connectionCount++;
                        }
                        catch (SocketException)
                        {
                            //No more requests in queue
                            break;
                        }

                        if (connectionCount == connectionsToAccept)
                        {
                            return;
                        }
                    }
                }
            }
        }
    }
}
