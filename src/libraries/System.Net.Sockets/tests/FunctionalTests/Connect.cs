// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Linq;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Net.Sockets.Tests
{
    public abstract class Connect<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public Connect(ITestOutputHelper output) : base(output) {}

        [OuterLoop]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
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
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
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
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
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

        [OuterLoop("Connects to external server")]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.FreeBSD, "Not supported on BSD like OSes.")]
        [Theory]
        [InlineData("1.1.1.1", false, true)]
        [InlineData("1.1.1.1", true, true)]
        [InlineData("[::ffff:1.1.1.1]", false, true)]
        [InlineData("[::ffff:1.1.1.1]", true, true)]
        [InlineData("1.1.1.1", false, false)]
        [InlineData("1.1.1.1", true, false)]
        [InlineData("[::ffff:1.1.1.1]", false, false)]
        [InlineData("[::ffff:1.1.1.1]", true, false)]
        public async Task ConnectGetsCanceledByDispose(string addressString, bool useDns, bool owning)
        {
            // Skip test on Linux kernels that may have a regression that was fixed in 6.6.
            // See TcpReceiveSendGetsCanceledByDispose test for additional information.
            if (UsesSync && PlatformDetection.IsLinux && Environment.OSVersion.Version < new Version(6, 6))
            {
                return;
            }

            // Aborting sync operations for non-owning handles is not supported on Unix.
            if (!owning && UsesSync && !PlatformDetection.IsWindows)
            {
                return;
            }

            IPAddress address = IPAddress.Parse(addressString);

            // We try this a couple of times to deal with a timing race: if the Dispose happens
            // before the operation is started, we won't see a SocketException.
            int msDelay = 100;
            await RetryHelper.ExecuteAsync(async () =>
            {
                var client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                using SafeSocketHandle? owner = ReplaceWithNonOwning(ref client, owning);

                if (address.IsIPv4MappedToIPv6) client.DualMode = true;

                Task connectTask = ConnectAsync(client, useDns ?
                    new DnsEndPoint("one.one.one.one", 23) :
                    new IPEndPoint(address, 23));

                // Wait a little so the operation is started.
                await Task.Delay(Math.Min(msDelay, 1000));
                msDelay *= 2;
                Task disposeTask = Task.Run(() => client.Dispose());

                await Task.WhenAny(disposeTask, connectTask).WaitAsync(TimeSpan.FromSeconds(30));
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

                if (UsesSync)
                {
                    Assert.True(disposedException || localSocketError == SocketError.NotSocket, $"{disposedException} {localSocketError}");
                }
                else
                {
                    Assert.Equal(SocketError.OperationAborted, localSocketError);
                }
            }, maxAttempts: 10, retryWhen: e => e is XunitException);
        }

        [OuterLoop("Connection failure takes long on Windows.")]
        [Fact]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public async Task Connect_WithoutListener_ThrowSocketExceptionWithAppropriateInfo()
        {
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BindToAnonymousPort(IPAddress.Loopback);
                return socket;
            });
            Socket a = portBlocker.MainSocket;
            using Socket b = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketException ex = await Assert.ThrowsAsync<SocketException>(() => ConnectAsync(b, a.LocalEndPoint));
            Assert.Contains(Marshal.GetPInvokeErrorMessage(ex.NativeErrorCode), ex.Message);

            if (UsesSync)
            {
                Assert.Contains(a.LocalEndPoint.ToString(), ex.Message);
            }
        }

        [Theory]
        [MemberData(nameof(LoopbacksAndAny))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/107981", TestPlatforms.Wasi)]
        public async Task Connect_DatagramSockets_DontThrowConnectedException_OnSecondAttempt(IPAddress listenAt, IPAddress secondConnection)
        {
            using Socket listener = new Socket(listenAt.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket s = new Socket(listenAt.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            listener.Bind(new IPEndPoint(listenAt, 0));

            await ConnectAsync(s, new IPEndPoint(listenAt, ((IPEndPoint)listener.LocalEndPoint).Port));
            Assert.True(s.Connected);
            // According to the OSX man page, it's enough connecting to an invalid address to dissolve the connection. (0 port connection returns error on OSX)
            await ConnectAsync(s, new IPEndPoint(secondConnection, PlatformDetection.IsApplePlatform ? 1 : 0));
            Assert.True(s.Connected);
        }

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public Task MultiConnect_KeepAliveOptionsPreserved(bool dnsConnect) => MultiConnectTestImpl(dnsConnect,
            c =>
            {
                c.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                c.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 5);
                c.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 4);
                c.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
            },
            c =>
            {
                int keepAlive = (int)c.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive)!;
                int keepAliveTime = (int)c.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime)!;
                int keepAliveInterval = (int)c.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval)!;
                int keepAliveRetryCount = (int)c.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount)!;

                Assert.True(keepAlive is not 0);
                Assert.Equal(5, keepAliveTime);
                Assert.Equal(4, keepAliveInterval);
                Assert.Equal(3, keepAliveRetryCount);
            });

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public Task MultiConnect_LingerState_Preserved(bool dnsConnect) => MultiConnectTestImpl(dnsConnect,
            c =>
            {
                c.LingerState = new LingerOption(true, 42);
            },
            c =>
            {
                Assert.True(c.LingerState.Enabled);
                Assert.Equal(42, c.LingerState.LingerTime);
            });

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public Task MultiConnect_MiscProperties_Preserved(bool dnsConnect) => MultiConnectTestImpl(dnsConnect,
            c =>
            {
                c.ReceiveTimeout = 4321;
                c.NoDelay = true;
            },
            c =>
            {
                Assert.Equal(4321, c.ReceiveTimeout);
                Assert.True(c.NoDelay);
            });

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory]
        [InlineData("single")]
        [InlineData("multi")]
        [InlineData("dns")]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public async Task Connect_ExposeHandle_FirstAttemptSucceeds(string connectMode)
        {
            if (UsesEap && connectMode is "multi")
            {
                throw new SkipTestException("EAP does not support IPAddress[] connect");
            }

            IPAddress address = (await Dns.GetHostAddressesAsync("localhost"))[0];

            int port = -1;
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket s = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                port = s.BindToAnonymousPort(address);
                return s;
            });
            Socket listeningSocket = portBlocker.MainSocket;
            listeningSocket.Listen();
            _ = listeningSocket.AcceptAsync();

            using Socket c = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _ = c.SafeHandle; // Expose the handle.

            await (connectMode switch
            {
                "single" => ConnectAsync(c, listeningSocket.LocalEndPoint),
                "multi" => MultiConnectAsync(c, [address], port),
                _ => ConnectAsync(c, new DnsEndPoint("localhost", port))
            });
            Assert.True(c.Connected);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public async Task MultiConnect_ExposeHandle_TerminatesAtFirstFailure(bool dnsConnect)
        {
            if (UsesEap && !dnsConnect)
            {
                throw new SkipTestException("EAP does not support IPAddress[] connect");
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync("localhost");
            
            // While most Unix environments are configured to resolve 'localhost' only to the ipv4 loopback address,
            // on some CI machines it resolves to both ::1 and 127.0.0.1. This test is valid in those environments only.
            bool testFailingConnect = addresses.Length > 1;
            if (!testFailingConnect)
            {
                throw new SkipTestException("'localhost' should resolve to both IPv6 and IPv4 for this test to be valid.");
            }

            // PortBlocker's "shadow socket" will be the one addresses[0] is pointing to. The test will fail to connect to that socket.
            IPAddress successAddress = addresses[1];
            int port = -1;
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket s = new Socket(successAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                port = s.BindToAnonymousPort(successAddress);
                return s;
            });
            Socket listeningSocket = portBlocker.MainSocket;

            listeningSocket.Listen();
            _ = listeningSocket.AcceptAsync();

            using Socket c = new Socket(SocketType.Stream, ProtocolType.Tcp);

            _ = c.SafeHandle; // Expose the handle.

            SocketException ex = await Assert.ThrowsAsync<SocketException>(
                async() => await (dnsConnect ? ConnectAsync(c, new DnsEndPoint("localhost", port)) : MultiConnectAsync(c, addresses, port)));
            Assert.True(ex.SocketErrorCode is SocketError.ConnectionRefused
                or SocketError.TimedOut); // Some Mac OS 12 machines produce SocketError.TimedOut here.
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public async Task SingleConnect_ExposeHandle_SecondAttemptThrowsPNSEOnUnix()
        {
            int port = -1;
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                port = s.BindToAnonymousPort(IPAddress.Loopback);
                return s;
            });

            using Socket c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _ = c.SafeHandle; // Expose the handle.

            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);

            // No listeners, the first connect should fail.
            await Assert.ThrowsAsync<SocketException>(() => ConnectAsync(c, ep));

            // Start listening so connecting should be possible.
            Socket listeningSocket = portBlocker.MainSocket;
            listeningSocket.Listen();
            _ = listeningSocket.AcceptAsync();

            // The second attempt throws PNSE on Unix.
            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => ConnectAsync(c, ep));
        }

        [ConditionalFact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.Wasi, "Wasi doesn't support PortBlocker")]
        public async Task MultiConnect_DualMode_Preserved()
        {
            if (UsesEap) throw new SkipTestException("EAP does not support IPAddress[] connect");

            int port = -1;
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket l = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                port = l.BindToAnonymousPort(IPAddress.IPv6Loopback);
                return l;
            });

            Socket l = portBlocker.MainSocket;
            l.Listen();
            _ = l.AcceptAsync();

            using Socket c = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = false
            };

            IPAddress[] addresses = [IPAddress.Parse("fc00:1:2:3:4:5:6:7"), IPAddress.IPv6Loopback]; // No listeners on the first address.
            await c.ConnectAsync(addresses, port);
            Assert.False(c.DualMode);
        }

        private async Task MultiConnectTestImpl(bool dnsConnect, Action<Socket> setupSocket, Action<Socket> validateSocket)
        {
            if (UsesEap && !dnsConnect)
            {
                throw new SkipTestException("EAP does not support IPAddress[] connect");
            }

            IPAddress[] addresses = await Dns.GetHostAddressesAsync("localhost");
            Assert.NotEmpty(addresses);

            // While most Unix environments are configured to resolve 'localhost' only to the ipv4 loopback address, on some CI machines it resolves to both ::1 and 127.0.0.1.
            // In such environments this test stresses the socket option tracking feature implemented in the Unix PAL by forcing the first connect attempt to fail.
            bool testFailingConnect = addresses.Length > 1;
            _output.WriteLine($"dnsConnect={dnsConnect}, testFailingConnect={testFailingConnect}, 'loopback' resolved to {string.Join(',', addresses)}.");

            // In case testFailingConnect == true, PortBlocker's "shadow socket" will be the one addresses[0] is pointing to.
            // The test will fail to connect to that socket.
            IPAddress successAddress = testFailingConnect ? addresses[1] : addresses[0];
            int port = -1;
            using PortBlocker portBlocker = new PortBlocker(() =>
            {
                Socket s = new Socket(successAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                port = s.BindToAnonymousPort(successAddress);
                return s;
            });
            Socket listeningSocket = portBlocker.MainSocket;

            listeningSocket.Listen();
            _ = listeningSocket.AcceptAsync();

            using Socket c = new Socket(SocketType.Stream, ProtocolType.Tcp);
            setupSocket(c);

            await (dnsConnect ? ConnectAsync(c, new DnsEndPoint("localhost", port)) : MultiConnectAsync(c, addresses, port));

            validateSocket(c);
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public sealed class ConnectSync : Connect<SocketHelperArraySync>
    {
        public ConnectSync(ITestOutputHelper output) : base(output) {}
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public sealed class ConnectSyncForceNonBlocking : Connect<SocketHelperSyncForceNonBlocking>
    {
        public ConnectSyncForceNonBlocking(ITestOutputHelper output) : base(output) {}
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ConnectAsync_WithData_DataReceived(bool useArrayApi)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            IPEndPoint serverEp = (IPEndPoint)listener.LocalEndPoint!;
            listener.Listen();

            var serverTask = Task.Run(async () =>
            {
                using Socket handler = await listener.AcceptAsync();
                using var cts = new CancellationTokenSource(TestSettings.PassingTestTimeout);
                byte[] recvBuffer = new byte[6];
                int received = await handler.ReceiveAsync(recvBuffer, SocketFlags.None, cts.Token);
                Assert.True(received == 4);

                recvBuffer.AsSpan(0, 4).SequenceEqual(new byte[] { 2, 3, 4, 5 });
            });

            using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            byte[] buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            
            var mre = new ManualResetEventSlim(false);
            var saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = serverEp;

            // Slice the buffer to test the offset management:
            if (useArrayApi)
            {
                saea.SetBuffer(buffer, 2, 4);
            }
            else
            {
                saea.SetBuffer(buffer.AsMemory(2, 4));
            }
            
            saea.Completed += (_, __) => mre.Set();

            if (client.ConnectAsync(saea))
            {
                Assert.True(mre.Wait(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
            }

            Assert.Equal(SocketError.Success, saea.SocketError);

            await serverTask;
        }
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FailedConnect_ConnectedReturnsFalse(bool useTimeSpan)
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect to port 1 where we expect no server to be listening.
            SocketException se = await Assert.ThrowsAnyAsync<SocketException>(() => ConnectAsync(socket, new IPEndPoint(IPAddress.Loopback, 1)));

            if (se.SocketErrorCode != SocketError.ConnectionRefused)
            {
                Assert.Equal(SocketError.WouldBlock, se.SocketErrorCode);

                // Give the non-blocking connect some time to complete.
                if (useTimeSpan)
                {
                    socket.Poll(TimeSpan.FromMilliseconds(5000), SelectMode.SelectWrite);
                }
                else
                {
                    socket.Poll(5_000_000 /* microSeconds */, SelectMode.SelectWrite);
                }
            }

            Assert.False(socket.Connected);
        }
    }

    // The test class is declared non-parallel because of possible IPv4/IPv6 port-collision on Unix:
    // When running these tests in parallel with other tests, there is some chance that the DualMode client
    // will connect to an IPv4 server of a parallel test case.
    [Collection(nameof(DisableParallelization))]
    public abstract class Connect_NonParallel<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected Connect_NonParallel(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
        public async Task Connect_DualMode_MultiAddressFamilyConnect_RetrievedEndPoints_Success()
        {
            if (!SupportsMultiConnect)
                return;

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.True(client.DualMode);

                await MultiConnectAsync(client, new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, port);

                CheckIsIpv6LoopbackEndPoint(client.LocalEndPoint);
                CheckIsIpv6LoopbackEndPoint(client.RemoteEndPoint);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))] // async SocketTestServer requires threads
        public async Task Connect_DualMode_DnsConnect_RetrievedEndPoints_Success()
        {
            var localhostAddresses = Dns.GetHostAddresses("localhost");
            if (Array.IndexOf(localhostAddresses, IPAddress.Loopback) == -1 ||
                Array.IndexOf(localhostAddresses, IPAddress.IPv6Loopback) == -1)
            {
                return;
            }

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, IPAddress.Loopback, out port))
            using (Socket client = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.True(client.DualMode);

                await ConnectAsync(client, new DnsEndPoint("localhost", port));

                CheckIsIpv6LoopbackEndPoint(client.LocalEndPoint);
                CheckIsIpv6LoopbackEndPoint(client.RemoteEndPoint);
            }
        }

        private static void CheckIsIpv6LoopbackEndPoint(EndPoint endPoint)
        {
            IPEndPoint ep = endPoint as IPEndPoint;
            Assert.NotNull(ep);
            Assert.True(ep.Address.Equals(IPAddress.IPv6Loopback) || ep.Address.Equals(IPAddress.Loopback.MapToIPv6()));
        }
    }

    public sealed class ConnectSync_NonParallel : Connect_NonParallel<SocketHelperArraySync>
    {
        public ConnectSync_NonParallel(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public sealed class ConnectSyncForceNonBlocking_NonParallel : Connect_NonParallel<SocketHelperSyncForceNonBlocking>
    {
        public ConnectSyncForceNonBlocking_NonParallel(ITestOutputHelper output) : base(output) { }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
    public sealed class ConnectApm_NonParallel : Connect_NonParallel<SocketHelperApm>
    {
        public ConnectApm_NonParallel(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ConnectTask_NonParallel : Connect_NonParallel<SocketHelperTask>
    {
        public ConnectTask_NonParallel(ITestOutputHelper output) : base(output) { }
    }

    public sealed class ConnectEap_NonParallel : Connect_NonParallel<SocketHelperEap>
    {
        public ConnectEap_NonParallel(ITestOutputHelper output) : base(output) { }
    }
}
