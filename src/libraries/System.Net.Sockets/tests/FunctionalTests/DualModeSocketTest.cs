// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConstructorAndProperty : DualModeBase
    {
        [Fact]
        public void DualModeConstructor_InterNetworkV6Default()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
                Assert.True(socket.DualMode);
            }
        }

        [Fact]
        public void DualModeUdpConstructor_DualModeConfgiured()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
                Assert.True(socket.DualMode);
            }
        }

        [Fact]
        public void NormalConstructor_DualModeConfgiureable()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.False(socket.DualMode);

                socket.DualMode = true;
                Assert.True(socket.DualMode);

                socket.DualMode = false;
                Assert.False(socket.DualMode);
            }
        }

        [Fact]
        public void IPv4Constructor_DualMode_GetterReturnsFalse()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.False(socket.DualMode);
            }
        }

        [Fact]
        public void IPv4Constructor_DualMode_SetterThrows()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    socket.DualMode = true;
                });
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectToIPAddress : DualModeBase
    {
        [Fact] // Base case
        public void Socket_ConnectV4IPAddressToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;

                Assert.Throws<NotSupportedException>(() =>
                {
                    socket.Connect(IPAddress.Loopback, UnusedPort);
                });
            }
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false);
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV4IPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectV6IPAddressToV6Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectV4IPAddressToV6Host_Fails()
        {
            DualModeConnect_IPAddressToHost_Fails_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ConnectV6IPAddressToV4Host_Fails()
        {
            DualModeConnect_IPAddressToHost_Fails_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void ConnectV4IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV6IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        private void DualModeConnect_IPAddressToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            Assert.ThrowsAny<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                if (!OperatingSystem.IsWindows())
                {
                    // On Unix, socket assignment is random (not incremental) and there is a small chance the
                    // listening socket was created in another test currently running. Try the test one more time.
                    DualModeConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                }
            });
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectToIPEndPoint : DualModeBase
    {
        [Fact] // Base case
        public void Socket_ConnectV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;

                Assert.ThrowsAny<SocketException>(() =>
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, UnusedPort));
                });
            }
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false);
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV4IPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectV6IPEndPointToV6Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectV4IPEndPointToV6Host_Fails()
        {
            DualModeConnect_IPEndPointToHost_Fails_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ConnectV6IPEndPointToV4Host_Fails()
        {
            DualModeConnect_IPEndPointToHost_Fails_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void ConnectV4IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectV6IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                socket.Connect(new IPEndPoint(connectTo, port));
                Assert.True(socket.Connected);
            }
        }

        private void DualModeConnect_IPEndPointToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            Assert.ThrowsAny<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(connectTo, listenOn, false);
                if (!OperatingSystem.IsWindows())
                {
                    // On Unix, socket assignment is random (not incremental) and there is a small chance the
                    // listening socket was created in another test currently running. Try the test one more time.
                    DualModeConnect_IPEndPointToHost_Helper(connectTo, listenOn, false);
                }
            });
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectToIPAddressArray : DualModeBase
    {
        [Fact] // Base Case
        // "None of the discovered or specified addresses match the socket address family."
        public void Socket_ConnectV4IPAddressListToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;

                using (SocketServer server = new SocketServer(_log, IPAddress.Loopback, false, out int port))
                {
                    server.Start();
                    AssertExtensions.Throws<ArgumentException>("addresses", () =>
                    {
                        socket.Connect(new IPAddress[] { IPAddress.Loopback }, port);
                    });
                }
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Throws_Data))]
        public void DualModeConnect_IPAddressListToHost_Throws(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            SocketServer server = null;
            int port = 0;

            // PortBlocker creates a temporary socket of the opposite AddressFamily in the background, so parallel tests won't attempt
            // to create their listener sockets on the same port.
            // This should prevent 'server' from accepting DualMode connections of unrelated tests.
            using PortBlocker blocker = new PortBlocker(() =>
            {
                server = new SocketServer(_log, listenOn, dualModeServer, out port);
                return server.Socket;
            });

            using (server)
            {
                server.Start();
                Assert.ThrowsAny<SocketException>(() =>
                {
                    socket.Connect(connectTo, port);
                });
                Assert.False(socket.Connected);
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Success_Data))]
        public void DualModeConnect_IPAddressListToHost_Success(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectToHostString : DualModeBase
    {
        [ConditionalTheory(nameof(LocalhostIsBothIPv4AndIPv6))]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        public void DualModeConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                socket.Connect("localhost", port);
                Assert.True(socket.Connected);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectToDnsEndPoint : DualModeBase
    {
        [ConditionalTheory(nameof(LocalhostIsBothIPv4AndIPv6))]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        public void DualModeConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                socket.Connect(new DnsEndPoint("localhost", port, AddressFamily.Unspecified));
                Assert.True(socket.Connected);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeBeginConnectToIPAddress : DualModeBase
    {
        [Fact] // Base case
        public void Socket_BeginConnectV4IPAddressToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;

                Assert.Throws<NotSupportedException>(() =>
                {
                    socket.BeginConnect(IPAddress.Loopback, UnusedPort, null, null);
                });
            }
        }

        [Fact]
        public Task BeginConnectV4IPAddressToV4Host_Success() => DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51392", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public Task BeginConnectV6IPAddressToV6Host_Success() => DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);

        [Fact]
        public Task BeginConnectV4IPAddressToV6Host_Fails() => DualModeBeginConnect_IPAddressToHost_Fails_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);

        [Fact]
        public Task BeginConnectV6IPAddressToV4Host_Fails() => DualModeBeginConnect_IPAddressToHost_Fails_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);

        [Fact]
        public Task BeginConnectV4IPAddressToDualHost_Success() => DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);

        [Fact]
        public Task BeginConnectV6IPAddressToDualHost_Success() => DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);

        private async Task DualModeBeginConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, connectTo, port, null);
                Assert.True(socket.Connected);
            }
        }

        private async Task DualModeBeginConnect_IPAddressToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            SocketException e = await Assert.ThrowsAnyAsync<SocketException>(async () =>
            {
                await DualModeBeginConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                if (!OperatingSystem.IsWindows())
                {
                    // On Unix, socket assignment is random (not incremental) and there is a small chance the
                    // listening socket was created in another test currently running. Try the test one more time.
                    await DualModeBeginConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                }
            });
            Assert.NotEmpty(e.Message);
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeBeginConnectToIPEndPoint : DualModeBase
    {
        [Fact]
        public void Socket_BeginConnectV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;
                Assert.Throws<NotSupportedException>(() => socket.BeginConnect(new IPEndPoint(IPAddress.Loopback, UnusedPort), null, null));
            }
        }

        [Fact]
        public Task BeginConnectV4IPEndPointToV4Host_Success() => DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);

        [Fact]
        public Task BeginConnectV6IPEndPointToV6Host_Success() => DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);

        [Fact]
        public Task BeginConnectV4IPEndPointToDualHost_Success() => DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);

        [Fact]
        public Task BeginConnectV6IPEndPointToDualHost_Success() => DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);

        private async Task DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, new IPEndPoint(connectTo, port), null);
                Assert.True(socket.Connected);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeBeginConnect : DualModeBase
    {
        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Connecting sockets to DNS endpoints via the instance Connect and ConnectAsync methods not supported on Unix
        public async Task DualModeBeginConnect_IPAddressListToHost_Helper(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, connectTo, port, null);
                Assert.True(socket.Connected);
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Connecting sockets to DNS endpoints via the instance Connect and ConnectAsync methods not supported on Unix
        public async Task DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, "localhost", port, null);
                Assert.True(socket.Connected);
            }
        }

        [Theory]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        [PlatformSpecific(TestPlatforms.Windows)]  // Connecting sockets to DNS endpoints via the instance Connect and ConnectAsync methods not supported on Unix
        public async Task DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, new DnsEndPoint("localhost", port), null);
                Assert.True(socket.Connected);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectAsync : DualModeBase
    {
        [Fact] // Base case
        public void Socket_ConnectAsyncV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                Assert.Throws<NotSupportedException>(() =>
                {
                    socket.ConnectAsync(args);
                });
            }
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV6Host_Fails()
        {
            DualModeConnectAsync_IPEndPointToHost_Fails_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV4Host_Fails()
        {
            DualModeConnectAsync_IPEndPointToHost_Fails_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.UserToken = waitHandle;

                bool pending = socket.ConnectAsync(args);
                if (!pending)
                    waitHandle.Set();

                Assert.True(waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        private void DualModeConnectAsync_IPEndPointToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            Assert.ThrowsAny<SocketException>(() =>
            {
                DualModeConnectAsync_IPEndPointToHost_Helper(connectTo, listenOn, false);
                if (!OperatingSystem.IsWindows())
                {
                    // On Unix, socket assignment is random (not incremental) and there is a small chance the
                    // listening socket was created in another test currently running. Try the test one more time.
                    DualModeConnectAsync_IPEndPointToHost_Helper(connectTo, listenOn, false);
                }
            });
        }

        [ConditionalTheory(nameof(LocalhostIsBothIPv4AndIPv6))]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        public void DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                bool pending = socket.ConnectAsync(args);
                if (!pending)
                    waitHandle.Set();

                Assert.True(waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        [ConditionalTheory(nameof(LocalhostIsBothIPv4AndIPv6))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22225")]
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        public void DualModeConnectAsync_Static_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
                server.Start();
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                bool pending = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args);
                if (!pending)
                    waitHandle.Set();

                Assert.True(waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(args.ConnectSocket.Connected);
                args.ConnectSocket.Dispose();
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeBind : DualModeBase
    {
        [Fact]
        public void Socket_BindV4IPEndPoint_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, UnusedBindablePort));
                }
            });
        }

        [Fact] // Base Case; BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindMappedV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback.MapToIPv6());
            }
        }

        [Fact] // BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.Loopback);
            }
        }

        [Fact]
        public void BindV6IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
            }
        }

        [Fact]
        public void Socket_BindDnsEndPoint_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
            {
                using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new DnsEndPoint("localhost", UnusedBindablePort));
                }
            });
        }

        [Fact]
        public void Socket_EnableDualModeAfterV4Bind_Throws()
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.DualMode = false;
                serverSocket.BindToAnonymousPort(IPAddress.IPv6Any);
                Assert.Throws<SocketException>(() =>
                {
                    serverSocket.DualMode = true;
                });
            }
        }
    }

    public abstract class DualModeAcceptBase<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        public DualModeAcceptBase(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task AcceptV4BoundToSpecificV4_Success() => Accept_Helper(IPAddress.Loopback, IPAddress.Loopback);

        [Fact]
        public Task AcceptV4BoundToAnyV4_Success() => Accept_Helper(IPAddress.Any, IPAddress.Loopback);

        [Fact]
        public Task AcceptV6BoundToSpecificV6_Success() => Accept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);

        [Fact]
        public Task AcceptV6BoundToAnyV6_Success() => Accept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);

        [Fact]
        public Task AcceptV4BoundToAnyV6_Success() => Accept_Helper(IPAddress.IPv6Any, IPAddress.Loopback);

        [Fact]
        public Task AcceptV6BoundToSpecificV4_CantConnect() => Accept_Helper_Failing(IPAddress.Loopback, IPAddress.IPv6Loopback);

        [Fact]
        public Task AcceptV4BoundToSpecificV6_CantConnect() => Accept_Helper_Failing(IPAddress.IPv6Loopback, IPAddress.Loopback);

        [Fact]
        public Task AcceptV6BoundToAnyV4_CantConnect() => Accept_Helper_Failing(IPAddress.Any, IPAddress.IPv6Loopback);

        private async Task Accept_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            int port = serverSocket.BindToAnonymousPort(listenOn);
            serverSocket.Listen(1);

            using Socket client = new Socket(connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Task connectTask = client.ConnectAsync(connectTo, port);
            Socket clientSocket = await AcceptAsync(serverSocket);
            await connectTask;
            Assert.True(clientSocket.Connected);
            AssertDualModeEnabled(clientSocket, listenOn);
            Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
            if (connectTo == IPAddress.Loopback)
            {
                Assert.Contains(((IPEndPoint)clientSocket.LocalEndPoint).Address, DualModeBase.ValidIPv6Loopbacks);
            }
            else
            {
                Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
            }
        }

        private async Task Accept_Helper_Failing(IPAddress listenOn, IPAddress connectTo)
        {
            using Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            int port = serverSocket.BindToAnonymousPort(listenOn);
            serverSocket.Listen(1);
            _ = AcceptAsync(serverSocket);

            using Socket client = new Socket(connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await Assert.ThrowsAsync<SocketException>(() => client.ConnectAsync(connectTo, port));
        }

        protected static void AssertDualModeEnabled(Socket socket, IPAddress listenOn)
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.True(socket.DualMode);
            }
            else if (OperatingSystem.IsFreeBSD())
            {
                // This is not valid check on FreeBSD.
                // Accepted socket is never DualMode and cannot be changed.
            }
            else
            {
                Assert.True((listenOn != IPAddress.IPv6Any && !listenOn.IsIPv4MappedToIPv6) || socket.DualMode);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAcceptSync : DualModeAcceptBase<SocketHelperArraySync>
    {
        public DualModeAcceptSync(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAcceptApm : DualModeAcceptBase<SocketHelperApm>
    {
        public DualModeAcceptApm(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAcceptEap : DualModeAcceptBase<SocketHelperEap>
    {
        public DualModeAcceptEap(ITestOutputHelper output) : base(output) { }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAcceptTask : DualModeAcceptBase<SocketHelperTask>
    {
        public DualModeAcceptTask(ITestOutputHelper output) : base(output) { }
    }

    public abstract class DualModeConnectionlessSendToBase<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected DualModeConnectionlessSendToBase(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Socket_SendToV4IPEndPointToV4Host_Throws()
        {
            using Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;
            await Assert.ThrowsAsync<SocketException>(
                () => SendToAsync(socket, new byte[1], new IPEndPoint(IPAddress.Loopback, DualModeBase.UnusedPort)));
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public async Task Socket_SendToDnsEndPoint_Throws()
        {
            using Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            await AssertExtensions.ThrowsAsync<ArgumentException>("remoteEP",
                () => SendToAsync(socket, new byte[1], new DnsEndPoint("localhost", DualModeBase.UnusedPort)));
        }

        [Fact]
        public Task SendToV4IPEndPointToV4Host_Success() => DualModeSendTo_IPEndPointToHost_Success_Helper(IPAddress.Loopback, IPAddress.Loopback, false);

        [Fact]
        public Task SendToV6IPEndPointToV6Host_Success() => DualModeSendTo_IPEndPointToHost_Success_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);

        [Fact]
        public Task SendToV4IPEndPointToDualHost_Success() => DualModeSendTo_IPEndPointToHost_Success_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);

        [Fact]
        public Task SendToV6IPEndPointToDualHost_Success() => DualModeSendTo_IPEndPointToHost_Success_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);

        [Fact]
        public Task SendToV4IPEndPointToV6Host_NotReceived() => DualModeSendTo_IPEndPointToHost_Failing_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false);

        [Fact]
        public Task SendToV6IPEndPointToV4Host_NotReceived() => DualModeSendTo_IPEndPointToHost_Failing_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false);

        private async Task DualModeSendTo_IPEndPointToHost_Success_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using Socket server = dualModeServer ?
                new Socket(SocketType.Dgram, ProtocolType.Udp) :
                new Socket(listenOn.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            int port = server.BindToAnonymousPort(listenOn);

            Task<int> receiveTask = server.ReceiveAsync(new byte[1]);
            int sent = await SendToAsync(client, new byte[1], new IPEndPoint(connectTo, port)).WaitAsync(TestSettings.PassingTestTimeout);
            Assert.Equal(1, sent);

            int received = await receiveTask.WaitAsync(TestSettings.PassingTestTimeout);
            Assert.Equal(1, received);
        }

        private async Task DualModeSendTo_IPEndPointToHost_Failing_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using Socket server = dualModeServer ?
                    new Socket(SocketType.Dgram, ProtocolType.Udp) :
                    new Socket(listenOn.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            int port = server.BindToAnonymousPort(listenOn);

            _ = SendToAsync(client, new byte[1], new IPEndPoint(connectTo, port)).WaitAsync(TestSettings.PassingTestTimeout);
            await Assert.ThrowsAsync<TimeoutException>(() => server.ReceiveAsync(new byte[1]).WaitAsync(TestSettings.FailingTestTimeout));
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessSendToSync : DualModeConnectionlessSendToBase<SocketHelperArraySync>
    {
        public DualModeConnectionlessSendToSync(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessSendToApm : DualModeConnectionlessSendToBase<SocketHelperApm>
    {
        public DualModeConnectionlessSendToApm(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessSendToEap : DualModeConnectionlessSendToBase<SocketHelperEap>
    {
        public DualModeConnectionlessSendToEap(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessSendToTask : DualModeConnectionlessSendToBase<SocketHelperTask>
    {
        public DualModeConnectionlessSendToTask(ITestOutputHelper output) : base(output)
        {
        }
    }

    public abstract class DualModeConnectionlessReceiveFromBase<T> : SocketTestHelperBase<T> where T : SocketHelperBase, new()
    {
        protected DualModeConnectionlessReceiveFromBase(ITestOutputHelper output) : base(output)
        {
        }

        [Fact] // Base case
        public async Task Socket_ReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
            using Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, DualModeBase.UnusedPort);
            await Assert.ThrowsAsync<ArgumentException>(() => ReceiveFromAsync(socket, new byte[1], receivedFrom));
        }

        [Fact] // Base case
        public async Task Socket_ReceiveFromDnsEndPoint_Throws()
        {
            // "The parameter remoteEP must not be of type DnsEndPoint."
            using Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            
            int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
            EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
            await AssertExtensions.ThrowsAsync<ArgumentException>("remoteEP", () => ReceiveFromAsync(socket, new byte[1], receivedFrom));
        }

        [Fact]
        public Task ReceiveFromV4BoundToSpecificV4_Success() => ReceiveFrom_Success_Helper(IPAddress.Loopback, IPAddress.Loopback);

        [Fact]
        public Task ReceiveFromV4BoundToAnyV4_Success() => ReceiveFrom_Success_Helper(IPAddress.Any, IPAddress.Loopback);

        [Fact]
        public Task ReceiveFromV6BoundToSpecificV6_Success() => ReceiveFrom_Success_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);

        [Fact]
        public Task ReceiveFromV6BoundToAnyV6_Success() => ReceiveFrom_Success_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);

        [Fact]
        public Task ReceiveFromV4BoundToAnyV6_Success() => ReceiveFrom_Success_Helper(IPAddress.IPv6Any, IPAddress.Loopback);

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also ReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public Task ReceiveFromV6BoundToSpecificV4_NotReceived() => ReceiveFrom_Failure_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also expected behavior is different on OSX and Linux (ArgumentException instead of SocketException)
        [PlatformSpecific(TestPlatforms.Windows)]
        public Task ReceiveFromV4BoundToSpecificV6_NotReceived() => ReceiveFrom_Failure_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also ReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public Task ReceiveFromV6BoundToAnyV4_NotReceived() => ReceiveFrom_Failure_Helper(IPAddress.Any, IPAddress.IPv6Loopback);

        protected async Task ReceiveFrom_Success_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            int port = serverSocket.BindToAnonymousPort(listenOn);

            Socket client = new Socket(connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            Task<int> sendTask = client.SendToAsync(new byte[1], new IPEndPoint(connectTo, port))
                .WaitAsync(TestSettings.PassingTestTimeout);

            var result = await ReceiveFromAsync(serverSocket, new byte[1], new IPEndPoint(connectTo, port))
                .WaitAsync(TestSettings.PassingTestTimeout);

            Assert.Equal(1, result.ReceivedBytes);
            IPEndPoint remoteEndPoint = Assert.IsType<IPEndPoint>(result.RemoteEndPoint);
            Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
            Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
        }

        protected async Task ReceiveFrom_Failure_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            int port = serverSocket.BindToAnonymousPort(listenOn);

            Socket client = new Socket(connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _ = client.SendToAsync(new byte[1], new IPEndPoint(connectTo, port)).WaitAsync(TestSettings.PassingTestTimeout);
            await Assert.ThrowsAsync<TimeoutException>(() => ReceiveFromAsync(serverSocket, new byte[1], new IPEndPoint(connectTo, port))
                .WaitAsync(TestSettings.FailingTestTimeout));
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessReceiveFromSync : DualModeConnectionlessReceiveFromBase<SocketHelperArraySync>
    {
        public DualModeConnectionlessReceiveFromSync(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessReceiveFromApm : DualModeConnectionlessReceiveFromBase<SocketHelperApm>
    {
        public DualModeConnectionlessReceiveFromApm(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessReceiveFromEap : DualModeConnectionlessReceiveFromBase<SocketHelperEap>
    {
        public DualModeConnectionlessReceiveFromEap(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessReceiveFromTask : DualModeConnectionlessReceiveFromBase<SocketHelperTask>
    {
        public DualModeConnectionlessReceiveFromTask(ITestOutputHelper output) : base(output)
        {
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    [Collection(nameof(DisableParallelization))]
    public class DualModeConnectionlessReceiveMessageFrom : DualModeBase
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS)]  // ReceiveMessageFrom not supported on Apple platforms
        public void ReceiveMessageFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];
                SocketFlags flags = SocketFlags.None;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveMessageFrom(buf, 0, buf.Length, ref flags, ref ep, out IPPacketInformation packetInfo));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS)]  // ReceiveMessageFromAsync not supported on Apple platforms
        public void ReceiveMessageFromAsync_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                byte[] buf = new byte[1];
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(buf, 0, buf.Length);
                args.RemoteEndPoint = ep;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveMessageFromAsync(args));
            }
        }

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                SocketFlags socketFlags = SocketFlags.None;
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out IPPacketInformation ipPacketInformation);
                });
            }
        }

        [Fact] // Base case
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                SocketFlags socketFlags = SocketFlags.None;

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out IPPacketInformation ipPacketInformation);
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV4BoundToAnyV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV6BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use; ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use; ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use; ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromV4BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFrom not supported on Apple platforms")]
        public void ReceiveMessageFromAsync_SocketAsyncEventArgs_Success(bool ipv4)
        {
            const int DataLength = 10;
            AddressFamily family = ipv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6;
            IPAddress loopback = ipv4 ? IPAddress.Loopback : IPAddress.Loopback.MapToIPv6();
            IPAddress clientAddress = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            var completed = new ManualResetEventSlim(false);
            using (var sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp))
            using (var receiver = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                receiver.DualMode = true;
                receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
                receiver.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, true);
                int receiverPort = receiver.BindToAnonymousPort(IPAddress.IPv6Any);

                if (!ipv4)
                {
                    sender.DualMode = true;
                }

                int senderPort = sender.BindToAnonymousPort(loopback);
                var expectedEP = new IPEndPoint(IPAddress.Loopback.MapToIPv6(), senderPort);

                var args = new SocketAsyncEventArgs() { RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Any, 0) };
                args.Completed += (s, e) => { Console.WriteLine("Got 1 packet {0} {1}", e.RemoteEndPoint, e.ReceiveMessageFromPacketInfo.Address); completed.Set(); };
                args.SetBuffer(new byte[DataLength], 0, DataLength);

                var ep = new IPEndPoint(loopback, receiverPort);
                for (int iters = 0; iters < 5; iters++)
                {
                    sender.SendTo(new byte[DataLength], ep);

                    if (!receiver.ReceiveMessageFromAsync(args))
                    {
                        completed.Set();
                    }
                    Assert.True(completed.Wait(TestSettings.PassingTestTimeout), "Timeout while waiting for connection");
                    completed.Reset();

                    Assert.Equal(DataLength, args.BytesTransferred);
                    Assert.Equal(expectedEP, args.RemoteEndPoint);
                    Assert.True(args.ReceiveMessageFromPacketInfo.Address.Equals(IPAddress.Loopback) || args.ReceiveMessageFromPacketInfo.Address.Equals(IPAddress.Loopback.MapToIPv6()));
                }
            }
        }

        private void ReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                int received = 0;

                serverSocket.ReceiveTimeout = TestSettings.FailingTestTimeout;

                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<SocketException>(() =>
                    {
                        // This is a false start.
                        // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receivemessagefrom
                        // "...the returned IPPacketInformation object will only be valid for packets which arrive at the
                        // local computer after the socket option has been set. If a socket is sent packets between when
                        // it is bound to a local endpoint (explicitly by the Bind method or implicitly by one of the Connect,
                        // ConnectAsync, SendTo, or SendToAsync methods) and its first call to the ReceiveMessageFrom method,
                        // calls to ReceiveMessageFrom method will return invalid IPPacketInformation objects for these packets."
                        received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                    });
                }
                else
                {
                    // *nix may throw either a SocketException or ArgumentException in this case, depending on how the IP stack
                    // behaves w.r.t. dual-mode sockets bound to IPv6-specific addresses.
                    Assert.ThrowsAny<Exception>(() =>
                    {
                        received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                    });
                }

                serverSocket.ReceiveTimeout = expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout;

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);

                receivedFrom = new IPEndPoint(connectTo, port);
                socketFlags = SocketFlags.None;
                received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(typeof(IPEndPoint), receivedFrom.GetType());

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);

                Assert.Equal(connectTo, ipPacketInformation.Address);
            }
        }

        [Fact]
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                SocketFlags socketFlags = SocketFlags.None;

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);

                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                SocketFlags socketFlags = SocketFlags.None;
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV4BoundToAnyV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV6BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Expected behavior is different on Apple platforms and Linux
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Android)] // Read the comment above
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "BeginReceiveMessageFrom not supported on Apple platforms")]
        public void BeginReceiveMessageFromV4BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void BeginReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IAsyncResult async = serverSocket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);

                // Behavior difference from Desktop: receivedFrom will _not_ change during the synchronous phase.

                // IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                // Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                // Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                bool success = async.AsyncWaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveMessageFrom(async, ref socketFlags, ref receivedFrom, out IPPacketInformation ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(typeof(IPEndPoint), receivedFrom.GetType());

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.Equal(connectTo, ipPacketInformation.Address);
            }
        }

        [Fact]
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                args.SetBuffer(new byte[1], 0, 1);

                AssertExtensions.Throws<ArgumentException>("e", () =>
                {
                    socket.ReceiveMessageFromAsync(args);
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.ReceiveMessageFromAsync(args);
                });
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Expected behavior is different on Apple platforms and Linux
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        // NOTE: on Linux, the OS IP stack changes a dual-mode socket back to a
        //       normal IPv6 socket once the socket is bound to an IPv6-specific
        //       address. As a result, the argument validation checks in
        //       ReceiveFrom that check that the supplied endpoint is compatible
        //       with the socket's address family fail. We've decided that this is
        //       an acceptable difference due to the extra state that would otherwise
        //       be necessary to emulate the Winsock behavior.
        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.Android)]  // Read the comment above
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived_Linux()
        {
            AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS, "ReceiveMessageFromAsync not supported on Apple platforms")]
        public void ReceiveMessageFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveMessageFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.Completed += AsyncCompleted;
                args.UserToken = waitHandle;

                bool async = serverSocket.ReceiveMessageFromAsync(args);
                Assert.True(async);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port);
                if (!waitHandle.WaitOne(serverSocket.ReceiveTimeout))
                {
                    throw new TimeoutException();
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(typeof(IPEndPoint), args.RemoteEndPoint.GetType());

                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, args.SocketFlags);
                Assert.Equal(connectTo, args.ReceiveMessageFromPacketInfo.Address);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.MacCatalyst | TestPlatforms.iOS | TestPlatforms.tvOS)]  // BeginReceiveMessageFrom not supported on Apple platforms
        public void BeginReceiveMessageFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.BeginReceiveMessageFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null));
            }
        }
    }

    public class DualModeBase
    {
        // Ports 8 and 8887 are unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        internal const int UnusedPort = 8;
        protected const int UnusedBindablePort = 8887;

        protected readonly ITestOutputHelper _log;

        internal static IPAddress[] ValidIPv6Loopbacks = new IPAddress[] {
            new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 127, 0, 0, 1 }, 0),  // ::127.0.0.1
            IPAddress.Loopback.MapToIPv6(),                                                     // ::ffff:127.0.0.1
            IPAddress.IPv6Loopback                                                              // ::1
        };

        protected DualModeBase()
        {
            _log = TestLogging.GetInstance();
            Assert.True(Capability.IPv4Support() && Capability.IPv6Support());
        }

        public static bool LocalhostIsBothIPv4AndIPv6 { get; } = GetLocalhostIsBothIPv4AndIPv6();

        private static bool GetLocalhostIsBothIPv4AndIPv6()
        {
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses("localhost");
                return
                    addresses.Any(ip => ip.AddressFamily == AddressFamily.InterNetwork) &&
                    addresses.Any(ip => ip.AddressFamily == AddressFamily.InterNetworkV6);
            }
            catch { }
            return false;
        }

        protected static void AssertDualModeEnabled(Socket socket, IPAddress listenOn)
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.True(socket.DualMode);
            }
            else if (OperatingSystem.IsFreeBSD())
            {
                // This is not valid check on FreeBSD.
                // Accepted socket is never DualMode and cannot be changed.
            }
            else
            {
                Assert.True((listenOn != IPAddress.IPv6Any && !listenOn.IsIPv4MappedToIPv6) || socket.DualMode);
            }
        }

        public static readonly object[][] DualMode_Connect_IPAddress_DualMode_Data = {
            new object[] { IPAddress.Loopback, false },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.IPv6Any, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Any, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Throws_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback.MapToIPv6() }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Any, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Loopback, true },
        };

        public static readonly object[][] DualMode_IPAddresses_ListenOn_DualMode_Success_Data = {
            new object[] { new IPAddress[] { IPAddress.Loopback.MapToIPv6() }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Loopback, false },
            new object[] { new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, IPAddress.IPv6Any, true },
            new object[] { new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback }, IPAddress.IPv6Any, true }
        };

        protected class SocketServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _acceptedSocket;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public Socket Socket { get; }

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
                else
                {
                    Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }

                port = Socket.BindToAnonymousPort(address);
                Socket.Listen(1);   
            }

            public void Start()
            {
                IPAddress remoteAddress = Socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress, 0);
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remote;
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Accepted);
                e.UserToken = _waitHandle;

                Socket.AcceptAsync(e);
            }

            private void Accepted(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Accepted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                _acceptedSocket = e.AcceptSocket;

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    Socket.Dispose();
                    if (_acceptedSocket != null)
                        _acceptedSocket.Dispose();
                }
                catch (Exception) { }
            }
        }

        protected class SocketUdpServer : IDisposable
        {
            private readonly ITestOutputHelper _output;
            private Socket _server;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketUdpServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Dgram, ProtocolType.Udp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                }

                port = _server.BindToAnonymousPort(address);

                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.SetBuffer(new byte[1], 0, 1);
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                e.UserToken = _waitHandle;

                _server.ReceiveAsync(e);
            }

            private void Received(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Received: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                handle.Set();
            }

            public void Dispose()
            {
                try
                {
                    _server.Dispose();
                }
                catch (Exception) { }
            }
        }

        protected class SocketUdpClient
        {
            private readonly ITestOutputHelper _output;

            private int _port;
            private IPAddress _connectTo;
            private Socket _serverSocket;

            public SocketUdpClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port, bool sendNow = true)
            {
                _output = output;

                _connectTo = connectTo;
                _port = port;
                _serverSocket = serverSocket;

                if (sendNow)
                {
                    Task.Run(() => ClientSend());
                }
            }

            public void ClientSend(int timeout = 3)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.SendTimeout = timeout * 1000;

                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                    e.SetBuffer(new byte[1], 0, 1);

                    socket.SendToAsync(e);
                }
                catch (SocketException e)
                {
                    _output.WriteLine("Send to {0} {1} failed: {2}", _connectTo, _port, e.ToString());
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
        }

        protected void AsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            EventWaitHandle handle = (EventWaitHandle)e.UserToken;

            _log.WriteLine(
                "AsyncCompleted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                handle.GetHashCode() + " error: " + e.SocketError);

            handle.Set();
        }

        protected void ReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 1000;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port, sendNow: false);

                client.ClientSend();

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.ReceiveFrom(new byte[1], ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(typeof(IPEndPoint), receivedFrom.GetType());

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }
    }
}
