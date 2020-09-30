// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public abstract class Connect<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public Connect(ITestOutputHelper output) : base(output) {}

        [OuterLoop]
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_Success(IPAddress listenAt)
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, listenAt, out port))
            {
                using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task connectTask = ConnectAsync(client, new IPEndPoint(listenAt, port));
                    await connectTask;
                    Assert.True(client.Connected);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_Udp_Success(IPAddress listenAt)
        {
            using Socket listener = new Socket(listenAt.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket client = new Socket(listenAt.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            listener.Bind(new IPEndPoint(listenAt, 0));

            await ConnectAsync(client, new IPEndPoint(listenAt, ((IPEndPoint)listener.LocalEndPoint).Port));
            Assert.True(client.Connected);
        }

        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_Dns_Success(IPAddress listenAt)
        {
            // On some systems (like Ubuntu 16.04 and Ubuntu 18.04) "localhost" doesn't resolve to '::1'.
            if (Array.IndexOf(Dns.GetHostAddresses("localhost"), listenAt) == -1)
            {
                return;
            }

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, listenAt, out port))
            {
                using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    Task connectTask = ConnectAsync(client, new DnsEndPoint("localhost", port));
                    await connectTask;
                    Assert.True(client.Connected);
                }
            }
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(Loopbacks))]
        public async Task Connect_MultipleIPAddresses_Success(IPAddress listenAt)
        {
            if (!SupportsMultiConnect)
                return;

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, listenAt, out port))
            using (Socket client = new Socket(listenAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                Task connectTask = MultiConnectAsync(client, new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, port);
                await connectTask;
                Assert.True(client.Connected);
            }
        }

        [Fact]
        public async Task Connect_OnConnectedSocket_Fails()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port));

                // In the sync case, we throw a derived exception here, so need to use ThrowsAnyAsync
                SocketException se = await Assert.ThrowsAnyAsync<SocketException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                Assert.Equal(SocketError.IsConnected, se.SocketErrorCode);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Unix currently does not support Disconnect
        [OuterLoop]
        [Fact]
        public async Task Connect_AfterDisconnect_Fails()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port));
                client.Disconnect(reuseSocket: false);

                if (ConnectAfterDisconnectResultsInInvalidOperationException)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                }
                else
                {
                    SocketException se = await Assert.ThrowsAsync<SocketException>(() => ConnectAsync(client, new IPEndPoint(IPAddress.Loopback, port)));
                    Assert.Equal(SocketError.IsConnected, se.SocketErrorCode);
                }
            }
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.OSX | TestPlatforms.FreeBSD))] // Not supported on BSD like OSes.
        public async Task ConnectGetsCanceledByDispose()
        {
            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Task connectTask = ConnectAsync(client, new IPEndPoint(IPAddress.Parse("1.1.1.1"), 23));

                // Wait a little so the operation is started.
                await Task.Delay(msDelay);
                msDelay *= 2;
                Task disposeTask = Task.Run(() => client.Dispose());

                var cts = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(30000, cts.Token);
                Assert.NotSame(timeoutTask, await Task.WhenAny(disposeTask, connectTask, timeoutTask));
                cts.Cancel();

                await disposeTask;

                SocketError? localSocketError = null;
                bool disposedException = false;
                try
                {
                    await connectTask;
                }
                catch (SocketException se)
                {
                    // On connection timeout, retry.
                    Assert.NotEqual(SocketError.TimedOut, se.SocketErrorCode);

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
                    Assert.Equal(SocketError.NotSocket, localSocketError);
                }
                else
                {
                    Assert.Equal(SocketError.OperationAborted, localSocketError);
                }
            }, maxAttempts: 10);
        }
    }

    public sealed class ConnectSync : Connect<SocketHelperArraySync>
    {
        public ConnectSync(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectSyncForceNonBlocking : Connect<SocketHelperSyncForceNonBlocking>
    {
        public ConnectSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectApm : Connect<SocketHelperApm>
    {
        public ConnectApm(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectTask : Connect<SocketHelperTask>
    {
        public ConnectTask(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectEap : Connect<SocketHelperEap>
    {
        public ConnectEap(ITestOutputHelper output) : base(output) {}
    }

    public sealed class ConnectCancellableTask : Connect<SocketHelperCancellableTask>
    {
        public ConnectCancellableTask(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ConnectEndPoint_Precanceled_Throws()
        {
            EndPoint ep = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.ConnectAsync(ep, cts.Token));
            }
        }

        [Fact]
        public async Task ConnectAddressAndPort_Precanceled_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.ConnectAsync(IPAddress.Parse("1.2.3.4"), 1, cts.Token));
            }
        }

        [Fact]
        public async Task ConnectMultiAddressAndPort_Precanceled_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.ConnectAsync(new IPAddress[] { IPAddress.Parse("1.2.3.4"), IPAddress.Parse("1.2.3.5") }, 1, cts.Token));
            }
        }

        [Fact]
        public async Task ConnectHostNameAndPort_Precanceled_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await client.ConnectAsync("1.2.3.4", 1, cts.Token));
            }
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        [PlatformSpecific(TestPlatforms.Windows)]   // Linux will not even attempt to connect to the invalid IP address
        public async Task ConnectEndPoint_CancelDuringConnect_Throws()
        {
            EndPoint ep = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1);

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();

                ValueTask t = client.ConnectAsync(ep, cts.Token);

                // Delay cancellation a bit to try to ensure the OS actually attempts to connect
                cts.CancelAfter(100);

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t);
            }
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        [PlatformSpecific(TestPlatforms.Windows)]   // Linux will not even attempt to connect to the invalid IP address
        public async Task ConnectAddressAndPort_CancelDuringConnect_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();

                ValueTask t = client.ConnectAsync(IPAddress.Parse("1.2.3.4"), 1, cts.Token);

                // Delay cancellation a bit to try to ensure the OS actually attempts to connect
                cts.CancelAfter(100);

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t);
            }
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        [PlatformSpecific(TestPlatforms.Windows)]   // Linux will not even attempt to connect to the invalid IP address
        public async Task ConnectMultiAddressAndPort_CancelDuringConnect_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();

                ValueTask t = client.ConnectAsync(new IPAddress[] { IPAddress.Parse("1.2.3.4"), IPAddress.Parse("1.2.3.5") }, 1, cts.Token);

                // Delay cancellation a bit to try to ensure the OS actually attempts to connect
                cts.CancelAfter(100);

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t);
            }
        }

        [Fact]
        [OuterLoop("Uses Task.Delay")]
        [PlatformSpecific(TestPlatforms.Windows)]   // Linux will not even attempt to connect to the invalid IP address
        public async Task ConnectHostNameAndPort_CancelDuringConnect_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var cts = new CancellationTokenSource();

                ValueTask t = client.ConnectAsync("1.2.3.4", 1, cts.Token);

                // Delay cancellation a bit to try to ensure the OS actually attempts to connect
                cts.CancelAfter(100);

                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await t);
            }
        }
    }
}
