// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace System.Net.Sockets.Tests
{
    public abstract class Accept<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public Accept(ITestOutputHelper output) : base(output) { }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Accept_Success(IPAddress listenAt)
        {
            using (Socket listen = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listen.BindToAnonymousPort(listenAt);
                listen.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listen);
                Assert.False(acceptTask.IsCompleted);

                using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    await ConnectAsync(client, new IPEndPoint(listenAt, port));
                    Socket accept = await acceptTask;
                    Assert.NotNull(accept);
                    Assert.True(accept.Connected);
                    Assert.Equal(client.LocalEndPoint, accept.RemoteEndPoint);
                    Assert.Equal(accept.LocalEndPoint, client.RemoteEndPoint);
                }
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public async Task Accept_ConcurrentAcceptsBeforeConnects_Success(int numberAccepts)
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Listen(listener, numberAccepts);

                var clients = new Socket[numberAccepts];
                var servers = new Task<Socket>[numberAccepts];

                try
                {
                    for (int i = 0; i < numberAccepts; i++)
                    {
                        clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        servers[i] = AcceptAsync(listener);
                    }

                    foreach (Socket client in clients)
                    {
                        await ConnectAsync(client, listener.LocalEndPoint);
                    }

                    await Task.WhenAll(servers);
                    Assert.All(servers, s => Assert.Equal(TaskStatus.RanToCompletion, s.Status));
                    Assert.All(servers, s => Assert.NotNull(s.Result));
                    Assert.All(servers, s => Assert.True(s.Result.Connected));
                }
                finally
                {
                    foreach (Socket client in clients)
                    {
                        client?.Dispose();
                    }

                    foreach (Task<Socket> server in servers)
                    {
                        if (server?.Status == TaskStatus.RanToCompletion)
                        {
                            server.Result.Dispose();
                        }
                    }
                }
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public async Task Accept_ConcurrentAcceptsAfterConnects_Success(int numberAccepts)
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                Listen(listener, numberAccepts);

                var clients = new Socket[numberAccepts];
                var clientConnects = new Task[numberAccepts];
                var servers = new Task<Socket>[numberAccepts];

                try
                {
                    for (int i = 0; i < numberAccepts; i++)
                    {
                        clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        clientConnects[i] = ConnectAsync(clients[i], listener.LocalEndPoint);
                    }

                    for (int i = 0; i < numberAccepts; i++)
                    {
                        servers[i] = AcceptAsync(listener);
                    }

                    await Task.WhenAll(clientConnects);
                    Assert.All(clientConnects, c => Assert.Equal(TaskStatus.RanToCompletion, c.Status));

                    await Task.WhenAll(servers);
                    Assert.All(servers, s => Assert.Equal(TaskStatus.RanToCompletion, s.Status));
                    Assert.All(servers, s => Assert.NotNull(s.Result));
                    Assert.All(servers, s => Assert.True(s.Result.Connected));
                }
                finally
                {
                    foreach (Socket client in clients)
                    {
                        client?.Dispose();
                    }

                    foreach (Task<Socket> server in servers)
                    {
                        if (server?.Status == TaskStatus.RanToCompletion)
                        {
                            server.Result.Dispose();
                        }
                    }
                }
            }
        }

        [OuterLoop]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1483", TestPlatforms.AnyUnix)]
        public async Task Accept_WithTargetSocket_Success()
        {
            if (!SupportsAcceptIntoExistingSocket)
                return;

            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener, server);
                client.Connect(IPAddress.Loopback, port);

                Socket accepted = await acceptTask;
                Assert.Same(server, accepted);
                Assert.True(accepted.Connected);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/1483", TestPlatforms.AnyUnix)]
        [OuterLoop]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Accept_WithTargetSocket_ReuseAfterDisconnect_Success(bool reuseSocket)
        {
            if (!SupportsAcceptIntoExistingSocket)
                return;

            // APM mode fails currently.  Issue: #22764
            if (typeof(T) == typeof(SocketHelperApm))
                return;

            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task<Socket> acceptTask = AcceptAsync(listener, server);
                    client.Connect(IPAddress.Loopback, port);

                    Socket accepted = await acceptTask;
                    Assert.Same(server, accepted);
                    Assert.True(accepted.Connected);
                }

                server.Disconnect(reuseSocket);
                Assert.False(server.Connected);

                if (reuseSocket)
                {
                    using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        Task<Socket> acceptTask = AcceptAsync(listener, server);
                        client.Connect(IPAddress.Loopback, port);

                        Socket accepted = await acceptTask;
                        Assert.Same(server, accepted);
                        Assert.True(accepted.Connected);
                    }
                }
                else
                {
                    SocketException se = await Assert.ThrowsAsync<SocketException>(() => AcceptAsync(listener, server));
                    Assert.Equal(SocketError.InvalidArgument, se.SocketErrorCode);
                }
            }
        }

        [OuterLoop]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1483", TestPlatforms.AnyUnix)]
        public void Accept_WithAlreadyBoundTargetSocket_Fails()
        {
            if (!SupportsAcceptIntoExistingSocket)
                return;

            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                server.BindToAnonymousPort(IPAddress.Loopback);

                Assert.Throws<InvalidOperationException>(() => { AcceptAsync(listener, server); });
            }
        }

        [OuterLoop]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/1483", TestPlatforms.AnyUnix)]
        public async Task Accept_WithInUseTargetSocket_Fails()
        {
            if (!SupportsAcceptIntoExistingSocket)
                return;

            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listener.BindToAnonymousPort(IPAddress.Loopback);
                listener.Listen(1);

                Task<Socket> acceptTask = AcceptAsync(listener, server);
                client.Connect(IPAddress.Loopback, port);

                Socket accepted = await acceptTask;
                Assert.Same(server, accepted);
                Assert.True(accepted.Connected);

                Assert.Throws<InvalidOperationException>(() => { AcceptAsync(listener, server); });
            }
        }

        [Fact]
        public async Task AcceptAsync_MultipleAcceptsThenDispose_AcceptsThrowAfterDispose()
        {
            if (UsesSync)
            {
                return;
            }

            for (int i = 0; i < 100; i++)
            {
                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(2);

                    Task accept1 = AcceptAsync(listener);
                    Task accept2 = AcceptAsync(listener);
                    listener.Dispose();
                    await Assert.ThrowsAnyAsync<Exception>(() => accept1);
                    await Assert.ThrowsAnyAsync<Exception>(() => accept2);
                }
            }
        }

        public static readonly TheoryData<IPAddress> AcceptGetsCanceledByDispose_Data = new TheoryData<IPAddress>
        {
            { IPAddress.Loopback },
            { IPAddress.IPv6Loopback },
            { IPAddress.Loopback.MapToIPv6() }
        };

        [Theory]
        [MemberData(nameof(AcceptGetsCanceledByDispose_Data))]
        public async Task AcceptGetsCanceledByDispose(IPAddress loopback)
        {
            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                var listener = new Socket(loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (loopback.IsIPv4MappedToIPv6) listener.DualMode = true;
                listener.Bind(new IPEndPoint(loopback, 0));
                listener.Listen(1);

                Task acceptTask = AcceptAsync(listener);

                // Wait a little so the operation is started.
                await Task.Delay(msDelay);
                msDelay *= 2;
                Task disposeTask = Task.Run(() => listener.Dispose());

                await Task.WhenAny(disposeTask, acceptTask).WaitAsync(TimeSpan.FromSeconds(30));
                await disposeTask;

                SocketError? localSocketError = null;
                bool disposedException = false;
                try
                {
                    await acceptTask;
                }
                catch (SocketException se)
                {
                    localSocketError = se.SocketErrorCode;
                }
                catch (ObjectDisposedException)
                {
                    disposedException = true;
                }

                if (UsesApm)
                {
                    Assert.Null(localSocketError);
                    Assert.True(disposedException);
                }
                else if (UsesSync)
                {
                    Assert.Equal(SocketError.Interrupted, localSocketError);
                }
                else
                {
                    Assert.Equal(SocketError.OperationAborted, localSocketError);
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task AcceptReceive_Windows_Success()
        {
            if (!SupportsAcceptReceive)
            {
                // Currently only supported by APM and EAP
                return;
            }

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = listener.BindToAnonymousPort(IPAddress.Loopback);
            IPEndPoint listenerEndpoint = new IPEndPoint(IPAddress.Loopback, port);
            listener.Listen(100);

            var acceptTask = AcceptAsync(listener, 1);

            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(listenerEndpoint);
            sender.Send(new byte[] { 42 });

            (_, byte[] recvBuffer) = await acceptTask;
            Assert.Equal(new byte[] { 42 }, recvBuffer);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void AcceptReceive_Unix_ThrowsPlatformNotSupportedException()
        {
            if (!SupportsAcceptReceive)
            {
                // Currently only supported by APM and EAP
                return;
            }

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            int port = listener.BindToAnonymousPort(IPAddress.Loopback);
            IPEndPoint listenerEndpoint = new IPEndPoint(IPAddress.Loopback, port);
            listener.Listen(100);

            Assert.ThrowsAsync<PlatformNotSupportedException>(() => AcceptAsync(listener, 1) );
        }
    }

    public sealed class AcceptSync : Accept<SocketHelperArraySync>
    {
        public AcceptSync(ITestOutputHelper output) : base(output) {}
    }

    public sealed class AcceptSyncForceNonBlocking : Accept<SocketHelperSyncForceNonBlocking>
    {
        public AcceptSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    public sealed class AcceptApm : Accept<SocketHelperApm>
    {
        public AcceptApm(ITestOutputHelper output) : base(output) {}

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void EndAccept_AcceptReceiveUnix_ThrowsPlatformNotSupportedException()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Creating a fake IAsyncResult:
            int port = listener.BindToAnonymousPort(IPAddress.Loopback);
            IPEndPoint listenerEndpoint = new IPEndPoint(IPAddress.Loopback, port);
            listener.Listen(100);
            IAsyncResult iar = listener.BeginAccept(callback: null, state: null);

            Assert.Throws<PlatformNotSupportedException>(() => listener.EndAccept(out _, iar));
            Assert.Throws<PlatformNotSupportedException>(() => listener.EndAccept(out _, out _, iar));
        }
    }

    public sealed class AcceptTask : Accept<SocketHelperTask>
    {
        public AcceptTask(ITestOutputHelper output) : base(output) {}
    }

    public sealed class AcceptEap : Accept<SocketHelperEap>
    {
        public AcceptEap(ITestOutputHelper output) : base(output) {}
    }
}
