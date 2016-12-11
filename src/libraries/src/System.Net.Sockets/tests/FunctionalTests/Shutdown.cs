// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                        bool pending = client.ReceiveAsync(args);
                        if (!pending)
                        {
                            OnOperationCompleted(null, args);
                        }
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

                        bool pending = client.SendAsync(args);
                        if (!pending)
                        {
                            OnOperationCompleted(null, args);
                        }
                        break;
                    }

                case SocketAsyncOperation.Send:
                    {
                        var client = (Socket)args.UserToken;

                        Assert.True(args.BytesTransferred == args.Buffer.Length);

                        bool pending = client.ReceiveAsync(args);
                        if (!pending)
                        {
                            OnOperationCompleted(null, args);
                        }
                        break;
                    }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Shutdown_TCP_CLOSED_Success()
        {
            // NOTE: this value should technically be at least as long as the amount
            //       of time that a TCP connection will stay in the TIME_WAIT state.
            //       That value, however, is technically defined as 2 * MSL, which is
            //       officially 4 minutes, and may differ between systems. In practice,
            //       5 seconds has proved to be long enough.
            const int TimeWaitTimeout = 5000;

            using (Socket server = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.IPv6Any);
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
                Task.Delay(TimeWaitTimeout).Wait();

                client.Shutdown(SocketShutdown.Both);
            }
        }
    }
}
