// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
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

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
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
        public async Task Accept_WithAlreadyBoundTargetSocket_Fails()
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

                await Assert.ThrowsAsync<InvalidOperationException>(() => AcceptAsync(listener, server));
            }
        }

        [OuterLoop]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
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

                await Assert.ThrowsAsync<InvalidOperationException>(() => AcceptAsync(listener, server));
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

        public static readonly TheoryData<IPAddress, bool> AcceptGetsCanceledByDispose_Data = new TheoryData<IPAddress, bool>
        {
            { IPAddress.Loopback, true },
            { IPAddress.IPv6Loopback, true },
            { IPAddress.Loopback.MapToIPv6(), true },
            { IPAddress.Loopback, false },
            { IPAddress.IPv6Loopback, false },
            { IPAddress.Loopback.MapToIPv6(), false }
        };

        [Theory]
        [MemberData(nameof(AcceptGetsCanceledByDispose_Data))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/107981", TestPlatforms.Wasi)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73536", TestPlatforms.iOS | TestPlatforms.tvOS)]
        public async Task AcceptGetsCanceledByDispose(IPAddress loopback, bool owning)
        {
            // Aborting sync operations for non-owning handles is not supported on Unix.
            if (!owning && UsesSync && !PlatformDetection.IsWindows)
            {
                return;
            }

            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                var listener = new Socket(loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                using SafeSocketHandle? owner = ReplaceWithNonOwning(ref listener, owning);

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

                try
                {
                    await acceptTask;
                }
                catch (SocketException se)
                {
                    localSocketError = se.SocketErrorCode;
                }

                if (UsesSync)
                {
                    Assert.Equal(SocketError.Interrupted, localSocketError);
                }
                else
                {
                    Assert.Equal(SocketError.OperationAborted, localSocketError);
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task AcceptReceive_Success()
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
            AssertExtensions.SequenceEqual(new byte[] { 42 }, recvBuffer);
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public sealed class AcceptSync : Accept<SocketHelperArraySync>
    {
        public AcceptSync(ITestOutputHelper output) : base(output) {}
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public sealed class AcceptSyncForceNonBlocking : Accept<SocketHelperSyncForceNonBlocking>
    {
        public AcceptSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
    public sealed class AcceptApm : Accept<SocketHelperApm>
    {
        public AcceptApm(ITestOutputHelper output) : base(output) {}

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task AbortedByDispose_LeaksNoUnobservedExceptions()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BindToAnonymousPort(IPAddress.Loopback);
                socket.Listen(10);

                bool unobservedThrown = false;
                TaskScheduler.UnobservedTaskException += (_, __) => unobservedThrown = true;

                await Task.Run(() =>
                {
                    socket.BeginAccept(asyncResult =>
                    {
                        try
                        {
                            socket.EndAccept(asyncResult);
                        }
                        catch
                        {
                        }
                    }, socket);
                });

                // Give some time for the Accept operation to start
                await Task.Delay(30);

                // Close the socket aborting Accept
                socket.Dispose();

                // Wait for the internal AcceptAsync Task to complete with the exception.
                await Task.Delay(30);

                // Ensure that the internal TaskExceptionHolder is finalized and the exception published to UnobservedTaskException.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.False(unobservedThrown);
            }).DisposeAsync();   
        }
    }

    public sealed class AcceptTask : Accept<SocketHelperTask>
    {
        public AcceptTask(ITestOutputHelper output) : base(output) {}
    }

    public sealed class AcceptCancellableTask : Accept<SocketHelperCancellableTask>
    {
        public AcceptCancellableTask(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task AcceptAsync_Precanceled_Throws()
        {
            using (Socket listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listen.BindToAnonymousPort(IPAddress.Loopback);
                listen.Listen(1);

                var cts = new CancellationTokenSource();
                cts.Cancel();

                var acceptTask = listen.AcceptAsync(cts.Token);
                Assert.True(acceptTask.IsCompleted);

                var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await acceptTask);
                Assert.Equal(cts.Token, oce.CancellationToken);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/107981", TestPlatforms.Wasi)]
        public async Task AcceptAsync_CanceledDuringOperation_Throws()
        {
            using (Socket listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = listen.BindToAnonymousPort(IPAddress.Loopback);
                listen.Listen(1);

                var cts = new CancellationTokenSource();

                var acceptTask = listen.AcceptAsync(cts.Token);
                Assert.False(acceptTask.IsCompleted);

                cts.Cancel();

                var oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await acceptTask);
                Assert.Equal(cts.Token, oce.CancellationToken);
            }
        }
    }

    public sealed class AcceptEap : Accept<SocketHelperEap>
    {
        public AcceptEap(ITestOutputHelper output) : base(output) {}
    }

    public sealed class AcceptDualStackResetTests
    {
        // Regression: on macOS, when a peer connects to a dual-stack AF_INET6 listener and
        // immediately resets (SO_LINGER=0), accept(2) can return successfully with an empty
        // remote sockaddr. Without a guard on the resulting zero-sized SocketAddress, the
        // shared FinishOperationSyncSuccess path throws ArgumentException from EndPoint.Create
        // and permanently breaks the listener (observed in Kestrel's accept loop).
        [ConditionalFact(typeof(Socket), nameof(Socket.OSSupportsIPv6))]
        public async Task AcceptAsync_DualStackListener_PeerImmediatelyResets_ListenerStaysHealthy()
        {
            using Socket listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            listener.DualMode = true;
            listener.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            int port = ((IPEndPoint)listener.LocalEndPoint!).Port;
            listener.Listen(128);

            for (int i = 0; i < 200; i++)
            {
                using Socket ipv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                using Socket ipv4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                Task connect6 = ipv6.ConnectAsync(IPAddress.IPv6Loopback, port);
                Task connect4 = ipv4.ConnectAsync(IPAddress.Loopback, port);
                await Task.WhenAll(connect6, connect4).WaitAsync(TimeSpan.FromSeconds(5));

                ipv4.LingerState = new LingerOption(true, 0);
                ipv4.Close();

                using Socket a1 = await listener.AcceptAsync().WaitAsync(TimeSpan.FromSeconds(5));
                using Socket a2 = await listener.AcceptAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }

            Task<Socket> finalAccept = listener.AcceptAsync();
            using (Socket probe = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await probe.ConnectAsync(IPAddress.Loopback, port);
                using Socket finalAccepted = await finalAccept.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.True(finalAccepted.Connected);
            }
        }
    }
}
