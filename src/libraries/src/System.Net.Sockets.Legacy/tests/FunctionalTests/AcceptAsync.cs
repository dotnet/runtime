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

        [Fact]
        public void Success()
        {
            AutoResetEvent completed = new AutoResetEvent(false);

            if (Socket.OSSupportsIPv4)
            {
                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    int port = sock.BindToAnonymousPort(IPAddress.Loopback);
                    sock.Listen(1);

                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += OnAcceptCompleted;
                    args.UserToken = completed;

                    // Not expecting the operation to finish synchronously as no client should be trying to connect.
                    Assert.True(sock.AcceptAsync(args));
                    _log.WriteLine("IPv4 Server: Waiting for clients.");

                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(new IPEndPoint(IPAddress.Loopback, port));

                    _log.WriteLine("IPv4 Client: Connecting.");
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "IPv4: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv4 Accept Socket was not connected");

                    client.Dispose();
                }
            }

            if (Socket.OSSupportsIPv6)
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    int port = sock.BindToAnonymousPort(IPAddress.IPv6Loopback);
                    sock.Listen(1);

                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.Completed += OnAcceptCompleted;
                    args.UserToken = completed;

                    Assert.True(sock.AcceptAsync(args));
                    _log.WriteLine("IPv6 Server: Waiting for clients.");

                    Socket client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    client.Connect(new IPEndPoint(IPAddress.IPv6Loopback, port));

                    _log.WriteLine("IPv6 Client: Connecting.");
                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "IPv6: Timed out while waiting for connection");

                    Assert.Equal<SocketError>(SocketError.Success, args.SocketError);
                    Assert.NotNull(args.AcceptSocket);
                    Assert.True(args.AcceptSocket.Connected, "IPv6 Accept Socket was not connected");
                    //Assert.NotNull(args.AcceptSocket.m_RightEndPoint, "m_RightEndPoint was not set");
                    //Assert.Equal(client.LocalEndPoint, args.AcceptSocket.m_RemoteEndPoint, "m_RemoteEndPoint is wrong!");

                    client.Dispose();
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
