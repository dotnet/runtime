// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Xunit.Abstractions;

using System.Threading.Tasks;

namespace System.Net.Sockets.Tests
{
    public class Shutdown
    {
        private readonly ITestOutputHelper _log;

        public Shutdown(ITestOutputHelper output)
        {
            _log = output;
        }

        private static void OnOperationCompleted(object sender, SocketAsyncEventArgs args)
        {
            Assert.Equal(SocketError.Success, args.SocketError);

            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                {
                    Socket client = args.AcceptSocket;
                    args.SetBuffer(new byte[1], 0, 1);
                    args.UserToken = client;

                    Assert.True(client.ReceiveAsync(args));
                    break;
                }

                case SocketAsyncOperation.Receive:
                {
                    var client = (Socket)args.UserToken;
                    if (args.BytesTransferred == 0)
                    {
                        client.Dispose();
                        break;
                    }

                    Assert.True(client.SendAsync(args));
                    break;
                }

                case SocketAsyncOperation.Send:
                {
                    var client = (Socket)args.UserToken;

                    Assert.True(args.BytesTransferred == args.Buffer.Length);
                    Assert.True(client.ReceiveAsync(args));
                    break;
                }
            }
        }

        [Fact]
        public void Shutdown_TCP_CLOSED_Success()
        {
            using (Socket server = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.Bind(IPAddress.IPv6Any);
                server.Listen(1);

                var args = new SocketAsyncEventArgs();
                args.Completed += OnOperationCompleted;
                Assert.True(server.AcceptAsync(args));

                client.Connect(IPAddress.IPv6Loopback, port);

                var buffer = new byte[] { 42 };
                for (int i = 0; i < 32; i++)
                {
                    int sent = client.Send(buffer);
                    Assert.Equal(1, sent);
                }

                client.Shutdown(SocketShutdown.Send);

                int received = 0;
                do
                {
                    received = client.Receive(buffer);
                } while (received != 0);

                // Wait for the underlying connection to transition from TIME_WAIT to
                // CLOSED.
                Task.Delay(5000).Wait();

                client.Shutdown(SocketShutdown.Both);
            }
        }
    }
}
