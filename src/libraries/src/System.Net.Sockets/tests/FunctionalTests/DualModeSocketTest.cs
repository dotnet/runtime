// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Test.Common;
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
        public void IPv4Constructor_DualModeThrows()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    Assert.False(socket.DualMode);
                });

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
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        private void DualModeConnect_IPAddressToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            Assert.ThrowsAny<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                socket.Connect(new IPEndPoint(connectTo, port));
                Assert.True(socket.Connected);
            }
        }

        private void DualModeConnect_IPEndPointToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            Assert.ThrowsAny<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(connectTo, listenOn, false);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                    AssertExtensions.Throws<ArgumentException>("addresses", () =>
                    {
                        socket.Connect(new IPAddress[] { IPAddress.Loopback }, port);
                    });
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Throws_Data))]
        public void DualModeConnect_IPAddressListToHost_Throws(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            Assert.ThrowsAny<SocketException>(() => DualModeConnect_IPAddressListToHost_Success(connectTo, listenOn, dualModeServer));
        }

        [Theory]
        [MemberData(nameof(DualMode_IPAddresses_ListenOn_DualMode_Success_Data))]
        public void DualModeConnect_IPAddressListToHost_Success(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
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
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, connectTo, port, null);
                Assert.True(socket.Connected);
            }
        }

        private async Task DualModeBeginConnect_IPAddressToHost_Fails_Helper(IPAddress connectTo, IPAddress listenOn)
        {
            SocketException e = await Assert.ThrowsAnyAsync<SocketException>(async () =>
            {
                await DualModeBeginConnect_IPAddressToHost_Helper(connectTo, listenOn, false);
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BeginConnectV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;
                Assert.Throws<SocketException>(() =>
                {
                    socket.BeginConnect(new IPEndPoint(IPAddress.Loopback, UnusedPort), null, null);
                });
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
        private async Task DualModeBeginConnect_IPAddressListToHost_Helper(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer)
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
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
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
        [MemberData(nameof(DualMode_Connect_IPAddress_DualMode_Data))]
        public void DualModeConnectAsync_Static_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer)
        {
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, out int port))
            {
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

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAccept : DualModeBase
    {
        [Fact]
        public void AcceptV4BoundToSpecificV4_Success()
        {
            Accept_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptV4BoundToAnyV4_Success()
        {
            Accept_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptV6BoundToSpecificV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptV6BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // https://github.com/dotnet/corefx/issues/5832
        public void AcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // https://github.com/dotnet/corefx/issues/5832
        public void AcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // https://github.com/dotnet/corefx/issues/5832
        public void AcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptV4BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void Accept_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);
                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);
                Socket clientSocket = serverSocket.Accept();
                Assert.True(clientSocket.Connected);
                AssertDualModeEnabled(clientSocket, listenOn);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeBeginAccept : DualModeBase
    {
        [Fact]
        public void BeginAcceptV4BoundToSpecificV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void BeginAcceptV6BoundToSpecificV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void BeginAcceptV6BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void BeginAcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void BeginAcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void BeginAcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void DualModeConnect_BeginAccept_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);
                IAsyncResult async = serverSocket.BeginAccept(null, null);
                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);

                // Due to the nondeterministic nature of calling dispose on a Socket that is doing
                // an EndAccept operation, we expect two types of exceptions to happen.
                Socket clientSocket;
                try
                {
                    clientSocket = serverSocket.EndAccept(async);
                    Assert.True(clientSocket.Connected);
                    AssertDualModeEnabled(clientSocket, listenOn);
                    Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                    if (connectTo == IPAddress.Loopback)
                    {
                        Assert.Contains(((IPEndPoint)clientSocket.LocalEndPoint).Address, ValidIPv6Loopbacks);
                    }
                    else
                    {
                        Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                    }
                    Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }

                Assert.True(
                    client.WaitHandle.WaitOne(TestSettings.PassingTestTimeout),
                    "Timed out while waiting for connection");

                if (client.Error != SocketError.Success)
                {
                    throw new SocketException((int)client.Error);
                }
            }
        }
    }

    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeAcceptAsync : DualModeBase
    {
        [Fact]
        public void AcceptAsyncV4BoundToSpecificV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void AcceptAsyncV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void AcceptAsyncV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void AcceptAsyncV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void DualModeConnect_AcceptAsync_Helper(IPAddress listenOn, IPAddress connectTo)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);
                serverSocket.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += AsyncCompleted;
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                args.UserToken = waitHandle;
                args.SocketError = SocketError.SocketError;

                _log.WriteLine(args.GetHashCode() + " SocketAsyncEventArgs with manual event " + waitHandle.GetHashCode());
                if (!serverSocket.AcceptAsync(args))
                {
                    throw new SocketException((int)args.SocketError);
                }

                SocketClient client = new SocketClient(_log, serverSocket, connectTo, port);

                var waitHandles = new WaitHandle[2];
                waitHandles[0] = waitHandle;
                waitHandles[1] = client.WaitHandle;

                int completedHandle = WaitHandle.WaitAny(waitHandles, TestSettings.PassingTestTimeout);

                if (completedHandle == WaitHandle.WaitTimeout)
                {
                    throw new TimeoutException("Timed out while waiting for either of client and server connections...");
                }

                if (completedHandle == 1)   // Client finished
                {
                    if (client.Error != SocketError.Success)
                    {
                        // Client SocketException
                        throw new SocketException((int)client.Error);
                    }

                    if (!waitHandle.WaitOne(5000))  // Now wait for the server.
                    {
                        throw new TimeoutException("Timed out while waiting for the server accept...");
                    }
                }

                _log.WriteLine(args.SocketError.ToString());


                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Socket clientSocket = args.AcceptSocket;
                Assert.NotNull(clientSocket);
                Assert.True(clientSocket.Connected);
                AssertDualModeEnabled(clientSocket, listenOn);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                if (connectTo == IPAddress.Loopback)
                {
                    Assert.Contains(((IPEndPoint)clientSocket.LocalEndPoint).Address, ValidIPv6Loopbacks);
                }
                else
                {
                    Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
                }
                clientSocket.Dispose();
            }
        }
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessSendTo : DualModeBase
    {
        #region SendTo Sync IPEndPoint

        [Fact]
        public void Socket_SendToV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;
                Assert.Throws<SocketException>(() =>
                {
                    socket.SendTo(new byte[1], new IPEndPoint(IPAddress.Loopback, UnusedPort));
                });
            }
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.SendTo(new byte[1], new DnsEndPoint("localhost", UnusedPort));
                });
            }
        }

        [Fact]
        public void SendToV4IPEndPointToV4Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void SendToV6IPEndPointToV6Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void SendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void SendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToV4IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void SendToV6IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeSendTo_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            using (Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp))
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out int port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < (expectedToTimeout ? 1 : TestSettings.UDPRedundancy); i++)
                {
                    int sent = client.SendTo(new byte[1], new IPEndPoint(connectTo, port));
                    Assert.Equal(1, sent);
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Sync
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessBeginSendTo : DualModeBase
    {
        #region SendTo Begin/End

        [Fact]
        public void Socket_BeginSendToV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                Assert.Throws<SocketException>(() =>
                {
                    socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, UnusedPort), null, null);
                });
            }
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginSendToDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new DnsEndPoint("localhost", UnusedPort), null, null);
                });
            }
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV4Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV6Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void BeginSendToV4IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeBeginSendTo_EndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            using (Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp))
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out int port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < (expectedToTimeout ? 1 : TestSettings.UDPRedundancy); i++)
                {
                    IAsyncResult async = client.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(connectTo, port), null, null);

                    int sent = client.EndSendTo(async);
                    Assert.Equal(1, sent);
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Begin/End
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessSendToAsync : DualModeBase
    {
        #region SendTo Async/Event

        [Fact]
        public void Socket_SendToAsyncV4IPEndPointToV4Host_Throws()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                args.SetBuffer(new byte[1], 0, 1);
                bool async = socket.SendToAsync(args);
                Assert.False(async);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Equal(SocketError.Fault, args.SocketError);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // NOTE: on Linux, this API returns ENETUNREACH instead of EFAULT: this platform
                    //       checks the family of the provided socket address before checking its size
                    //       (as long as the socket address is large enough to store an address family).
                    Assert.Equal(SocketError.NetworkUnreachable, args.SocketError);
                }
                else
                {
                    // NOTE: on other Unix platforms, this API returns EINVAL instead of EFAULT.
                    Assert.Equal(SocketError.InvalidArgument, args.SocketError);
                }
            }
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", UnusedPort);
                args.SetBuffer(new byte[1], 0, 1);
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.SendToAsync(args);
                });
            }
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void SendToAsyncV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Binds to a specific port on 'connectTo' which on Unix may already be in use
        public void SendToAsyncV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, expectedToTimeout: true);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true);
        }

        private void DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, bool expectedToTimeout = false)
        {
            using (Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp))
            using (SocketUdpServer server = new SocketUdpServer(_log, listenOn, dualModeServer, out int port))
            {
                // Send a few packets, in case they aren't delivered reliably.
                for (int i = 0; i < (expectedToTimeout ? 1 : TestSettings.UDPRedundancy); i++)
                {
                    using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                    {
                        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                        args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                        args.SetBuffer(new byte[1], 0, 1);
                        args.UserToken = waitHandle;
                        args.Completed += AsyncCompleted;

                        bool async = client.SendToAsync(args);
                        if (async)
                        {
                            Assert.True(waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timeout while waiting for connection");
                        }

                        Assert.Equal(1, args.BytesTransferred);
                        if (args.SocketError != SocketError.Success)
                        {
                            throw new SocketException((int)args.SocketError);
                        }
                    }
                }

                bool success = server.WaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Async/Event
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessReceiveFrom : DualModeBase
    {
        #region ReceiveFrom Sync

        [Fact] // Base case
        public void Socket_ReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void Socket_ReceiveFromDnsEndPoint_Throws()
        {
            // "The parameter remoteEP must not be of type DnsEndPoint."
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFromV4BoundToSpecificV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFromV4BoundToAnyV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFromV6BoundToSpecificV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFromV6BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also ReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also expected behavior is different on OSX and Linux (ArgumentException instead of SocketException)
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also ReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFromV4BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        #endregion ReceiveFrom Sync

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // ReceiveFrom not supported on OSX
        public void ReceiveFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, SocketFlags.None, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, buf.Length, SocketFlags.None, ref ep));
                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep));
            }
        }
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessBeginReceiveFrom : DualModeBase
    {
        #region ReceiveFrom Begin/End

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.DualMode = false;

                EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                EndPoint receivedFrom = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFromV4BoundToAnyV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFromV6BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also BeginReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public void BeginReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also expected behavior is different on OSX and Linux (ArgumentException instead of TimeoutException)
        [PlatformSpecific(TestPlatforms.Windows)]
        public void BeginReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        // Binds to a specific port on 'connectTo' which on Unix may already be in use
        // Also BeginReceiveFrom not supported on OSX
        [PlatformSpecific(TestPlatforms.Windows)]
        public void BeginReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFromV4BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void BeginReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                int port = serverSocket.BindToAnonymousPort(listenOn);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                IAsyncResult async = serverSocket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);

                // Behavior difference from Desktop: receivedFrom will _not_ change during the synchronous phase.

                // IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                // Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                // Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port, redundant: !expectedToTimeout);
                bool success = async.AsyncWaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveFrom(async, ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Begin/End
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessReceiveFromAsync : DualModeBase
    {
        #region ReceiveFrom Async/Event

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                args.SetBuffer(new byte[1], 0, 1);

                AssertExtensions.Throws<ArgumentException>("RemoteEndPoint", () =>
                {
                    socket.ReceiveFromAsync(args);
                });
            }
        }

        [Fact] // Base case
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = socket.BindToAnonymousPort(IPAddress.IPv6Loopback);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", port, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
                {
                    socket.ReceiveFromAsync(args);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
        }

        private void ReceiveFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, bool expectedToTimeout = false)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                int port = serverSocket.BindToAnonymousPort(listenOn);

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(listenOn, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = serverSocket.ReceiveFromAsync(args);
                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port, redundant: !expectedToTimeout);
                if (async && !waitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout))
                {
                    throw new TimeoutException();
                }

                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(args.RemoteEndPoint.GetType(), typeof(IPEndPoint));
                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Async/Event

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // ReceiveFromAsync not supported on OSX
        public void ReceiveFromAsync_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                byte[] buf = new byte[1];
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.SetBuffer(buf, 0, buf.Length);
                args.RemoteEndPoint = ep;

                Assert.Throws<PlatformNotSupportedException>(() => sock.ReceiveFromAsync(args));
            }
        }
    }

    [OuterLoop] // https://github.com/dotnet/corefx/issues/17681
    [Trait("IPv4", "true")]
    [Trait("IPv6", "true")]
    public class DualModeConnectionlessReceiveMessageFrom : DualModeBase
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
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
        [PlatformSpecific(TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
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
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
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
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToAnyV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
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
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFrom not supported on OSX
        public void ReceiveMessageFromV4BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback);
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

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.NotNull(ipPacketInformation);

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
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
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
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV4BoundToAnyV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV6BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.Linux | TestPlatforms.OSX))]  // Expected behavior is different on OSX and Linux
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
        [PlatformSpecific(TestPlatforms.Linux)]  // Read the comment above
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived_Linux()
        {
            AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
        public void BeginReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
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

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port, redundant: !expectedToTimeout);
                bool success = async.AsyncWaitHandle.WaitOne(expectedToTimeout ? TestSettings.FailingTestTimeout : TestSettings.PassingTestTimeout);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveMessageFrom(async, ref socketFlags, ref receivedFrom, out IPPacketInformation ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.NotNull(ipPacketInformation);
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

                AssertExtensions.Throws<ArgumentException>("RemoteEndPoint", () =>
                {
                    socket.ReceiveMessageFromAsync(args);
                });
            }
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
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
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback);
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~(TestPlatforms.Linux | TestPlatforms.OSX))]  // Expected behavior is different on OSX and Linux
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
        [PlatformSpecific(TestPlatforms.Linux)]  // Read the comment above
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived_Linux()
        {
            AssertExtensions.Throws<ArgumentException>("remoteEP", () =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
        public void ReceiveMessageFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, expectedToTimeout: true);
            });
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // ReceiveMessageFromAsync not supported on OSX
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

                SocketUdpClient client = new SocketUdpClient(_log, serverSocket, connectTo, port, redundant: !expectedToTimeout);
                if (!waitHandle.WaitOne(serverSocket.ReceiveTimeout))
                {
                    throw new TimeoutException();
                }

                Assert.Equal(1, args.BytesTransferred);
                Assert.Equal<Type>(args.RemoteEndPoint.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = args.RemoteEndPoint as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, args.SocketFlags);
                Assert.NotNull(args.ReceiveMessageFromPacketInfo);
                Assert.Equal(connectTo, args.ReceiveMessageFromPacketInfo.Address);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // BeginReceiveFrom not supported on OSX
        public void BeginReceiveFrom_NotSupported()
        {
            using (Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                sock.Bind(ep);

                byte[] buf = new byte[1];

                Assert.Throws<PlatformNotSupportedException>(() => sock.BeginReceiveFrom(buf, 0, buf.Length, SocketFlags.None, ref ep, null, null));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // BeginReceiveMessageFrom not supported on OSX
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
        protected const int UnusedPort = 8;
        protected const int UnusedBindablePort = 8887;

        protected readonly ITestOutputHelper _log;

        protected static IPAddress[] ValidIPv6Loopbacks = new IPAddress[] {
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(socket.DualMode);
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
            private Socket _server;
            private Socket _acceptedSocket;
            private EventWaitHandle _waitHandle = new AutoResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketServer(ITestOutputHelper output, IPAddress address, bool dualMode, out int port)
            {
                _output = output;

                if (dualMode)
                {
                    _server = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }
                else
                {
                    _server = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }

                port = _server.BindToAnonymousPort(address);
                _server.Listen(1);

                IPAddress remoteAddress = address.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress, 0);
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = remote;
                e.Completed += new EventHandler<SocketAsyncEventArgs>(Accepted);
                e.UserToken = _waitHandle;

                _server.AcceptAsync(e);
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
                    _server.Dispose();
                    if (_acceptedSocket != null)
                        _acceptedSocket.Dispose();
                }
                catch (Exception) { }
            }
        }

        protected class SocketClient
        {
            private IPAddress _connectTo;
            private Socket _serverSocket;
            private int _port;
            private readonly ITestOutputHelper _output;

            private EventWaitHandle _waitHandle = new AutoResetEvent(false);
            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketError Error
            {
                get;
                private set;
            }

            public SocketClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port)
            {
                _output = output;
                _connectTo = connectTo;
                _serverSocket = serverSocket;
                _port = port;
                Error = SocketError.Success;

                Task.Run(() => ConnectClient(null));
            }

            private void ConnectClient(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                    e.Completed += new EventHandler<SocketAsyncEventArgs>(Connected);
                    e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                    e.UserToken = _waitHandle;

                    if (!socket.ConnectAsync(e))
                    {
                        Connected(socket, e);
                    }
                }
                catch (SocketException ex)
                {
                    Error = ex.SocketErrorCode;
                    Thread.Sleep(TestSettings.FailingTestTimeout); // Give the other end a chance to call Accept().
                    _serverSocket.Dispose(); // Cancels the test
                    _waitHandle.Set();
                }
            }
            private void Connected(object sender, SocketAsyncEventArgs e)
            {
                EventWaitHandle handle = (EventWaitHandle)e.UserToken;
                _output.WriteLine(
                    "Connected: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                    handle.GetHashCode() + " error: " + e.SocketError);

                Error = e.SocketError;
                if (Error != SocketError.Success)
                {
                    Thread.Sleep(TestSettings.FailingTestTimeout); // Give the other end a chance to call Accept().
                    _serverSocket.Dispose(); // Cancels the test
                }
                handle.Set();
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

            public SocketUdpClient(ITestOutputHelper output, Socket serverSocket, IPAddress connectTo, int port, bool redundant = true, bool sendNow = true)
            {
                _output = output;

                _connectTo = connectTo;
                _port = port;
                _serverSocket = serverSocket;

                if (sendNow)
                {
                    Task.Run(() => ClientSend(redundant));
                }
            }

            public void ClientSend(bool redundant = true, int timeout = 3)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.SendTimeout = timeout * 1000;

                    for (int i = 0; i < (redundant ? TestSettings.UDPRedundancy : 1); i++)
                    {
                        SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                        e.RemoteEndPoint = new IPEndPoint(_connectTo, _port);
                        e.SetBuffer(new byte[1], 0, 1);

                        socket.SendToAsync(e);
                    }
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
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }
    }
}
