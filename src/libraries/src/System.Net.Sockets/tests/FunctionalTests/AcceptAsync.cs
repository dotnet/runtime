// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class AcceptAsync
    {
        private readonly ITestOutputHelper _log;

        public AcceptAsync(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        public void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            _log.WriteLine("OnAcceptCompleted event handler");
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }
        public void OnConnectCompleted(object sender, SocketAsyncEventArgs args)
        {
            _log.WriteLine("OnConnectCompleted event handler");
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [Fact]
        [Trait("IPv4", "true")]
        public void AcceptAsync_IpV4_Success()
        {
            Assert.True(Capability.IPv4Support());

            AutoResetEvent completed = new AutoResetEvent(false);
            AutoResetEvent completedClient = new AutoResetEvent(false);

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.Loopback);
                sock.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnAcceptCompleted;
                args.UserToken = completed;

                Assert.True(sock.AcceptAsync(args));
                _log.WriteLine("IPv4 Server: Waiting for clients.");

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    SocketAsyncEventArgs argsClient = new SocketAsyncEventArgs();
                    argsClient.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                    argsClient.Completed += OnConnectCompleted;
                    argsClient.UserToken = completedClient;
                    client.ConnectAsync(argsClient);

                    _log.WriteLine("IPv4 Client: Connecting.");
                    Assert.True(completed.WaitOne(5000), "IPv4: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv4 Accept Socket was not connected");
                    Assert.NotNull(args.AcceptSocket.RemoteEndPoint);
                    Assert.Equal(client.LocalEndPoint, args.AcceptSocket.RemoteEndPoint);
                }
            }
        }

        [Fact]
        [Trait("IPv6", "true")]
        public void AcceptAsync_IPv6_Success()
        {
            Assert.True(Capability.IPv6Support());

            AutoResetEvent completed = new AutoResetEvent(false);
            AutoResetEvent completedClient = new AutoResetEvent(false);

            using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = sock.BindToAnonymousPort(IPAddress.IPv6Loopback);
                sock.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += OnAcceptCompleted;
                args.UserToken = completed;

                Assert.True(sock.AcceptAsync(args));
                _log.WriteLine("IPv6 Server: Waiting for clients.");

                using (Socket client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    SocketAsyncEventArgs argsClient = new SocketAsyncEventArgs();
                    argsClient.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, port);
                    argsClient.Completed += OnConnectCompleted;
                    argsClient.UserToken = completedClient;
                    client.ConnectAsync(argsClient);

                    _log.WriteLine("IPv6 Client: Connecting.");
                    Assert.True(completed.WaitOne(5000), "IPv6: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv6 Accept Socket was not connected");
                    Assert.NotNull(args.AcceptSocket.RemoteEndPoint);
                    Assert.Equal(client.LocalEndPoint, args.AcceptSocket.RemoteEndPoint);
                }
            }
        }

        #region GC Finalizer test
        // This test assumes sequential execution of tests and that it is going to be executed after other tests
        // that used Sockets. 
        [Fact]
        public void TestFinalizers()
        {
            // Making several passes through the FReachable list.
            for (int i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        #endregion 
    }
}
