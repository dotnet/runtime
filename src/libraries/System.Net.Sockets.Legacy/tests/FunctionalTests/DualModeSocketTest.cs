// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class DualMode
    {
        // TODO: These constants are fill-ins for issues that need to be opened
        //       once this code is merged into corefx/master.
        private const int DummyDualModeV6Issue = 123456;
        private const int DummyErrorMismatchIssue = 123457;
        private const int DummyOSXPacketInfoIssue = 123458;
        private const int DummyOSXDualModePacketInfoIssue = 123459;
        private const int DummyLoopbackV6Issue = 123460;

        private const int TestPortBase = TestPortBases.DualMode;
        private readonly ITestOutputHelper _log;

        public DualMode(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        #region Constructor and Property

        [Fact]
        public void DualModeConstructor_DualModeConfgiured()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
            Assert.True(socket.DualMode);
        }

        [Fact]
        public void NormalConstructor_DualModeConfgiureable()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Assert.False(socket.DualMode);

            socket.DualMode = true;
            Assert.True(socket.DualMode);

            socket.DualMode = false;
            Assert.False(socket.DualMode);
        }

        [Fact]
        public void IPv4Constructor_DualModeThrows()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.Throws<NotSupportedException>(() =>
            {
                Assert.False(socket.DualMode);
            });

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.DualMode = true;
            });
        }

        #endregion Constructor and Property

        #region Connect

        #region Connect Sync

        #region Connect to IPAddress

        [Fact] // Base case
        public void Socket_ConnectV4IPAddressToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.Connect(IPAddress.Loopback, TestPortBase);
            });
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false, TestPortBase + 1);
        }

        [Fact] // Base Case
        public void ConnectV4MappedIPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true, TestPortBase + 2);
        }

        [Fact]
        public void ConnectV4IPAddressToV4Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 3);
        }

        [Fact]
        public void ConnectV6IPAddressToV6Host_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 4);
        }

        [Fact]
        public void ConnectV4IPAddressToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 5);
            });
        }

        [Fact]
        public void ConnectV6IPAddressToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 6);
            });
        }

        [Fact]
        public void ConnectV4IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 7);
        }

        [Fact]
        public void ConnectV6IPAddressToDualHost_Success()
        {
            DualModeConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 8);
        }

        private void DualModeConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPAddress

        #region Connect to IPEndPoint

        [Fact] // Base case
        public void Socket_ConnectV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<SocketException>(() =>
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, TestPortBase + 10));
            });
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, false, TestPortBase + 11);
        }

        [Fact] // Base case
        public void ConnectV4MappedIPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.IPv6Any, true, TestPortBase + 12);
        }

        [Fact]
        public void ConnectV4IPEndPointToV4Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 13);
        }

        [Fact]
        public void ConnectV6IPEndPointToV6Host_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 14);
        }

        [Fact]
        public void ConnectV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 15);
            });
        }

        [Fact]
        public void ConnectV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 16);
            });
        }

        [Fact]
        public void ConnectV4IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 17);
        }

        [Fact]
        public void ConnectV6IPEndPointToDualHost_Success()
        {
            DualModeConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 18);
        }

        private void DualModeConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                socket.Connect(new IPEndPoint(connectTo, port));
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPEndPoint

        #region Connect to IPAddress[]

        [Fact] // Base Case
        // "None of the discovered or specified addresses match the socket address family."
        public void Socket_ConnectV4IPAddressListToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;
            using (SocketServer server = new SocketServer(_log, IPAddress.Loopback, false, TestPortBase + 20))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    socket.Connect(new IPAddress[] { IPAddress.Loopback }, TestPortBase + 20);
                });
            }
        }

        [Fact] // Base Case
        public void ConnectMappedV4IPAddressListToV4Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback.MapToIPv6() },
                IPAddress.Loopback, false, TestPortBase + 21);
        }

        [Fact]
        public void ConnectMappedV4IPAddressListToAnyV6Host_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressListToHost_Helper(
                    new IPAddress[] { IPAddress.Loopback.MapToIPv6() },
                    IPAddress.IPv6Loopback, false, TestPortBase + 22);
            });
        }

        [Fact]
        public void ConnectV4IPAddressListToV4Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback },
                IPAddress.Loopback, false, TestPortBase + 23);
        }

        [Fact]
        public void ConnectV4IPAddressListToSpecificV6Host_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressListToHost_Helper(
                    new IPAddress[] { IPAddress.Loopback },
                    IPAddress.IPv6Loopback, false, TestPortBase + 24);
            });
        }

        [Fact]
        public void ConnectV4IPAddressListToAnyV6Host_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressListToHost_Helper(
                    new IPAddress[] { IPAddress.Loopback },
                    IPAddress.IPv6Any, false, TestPortBase + 25);
            });
        }

        [Fact]
        public void ConnectV4IPAddressListToSpecificV6DualHost_Throws()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_IPAddressListToHost_Helper(
                    new IPAddress[] { IPAddress.Loopback },
                    IPAddress.IPv6Loopback, true, TestPortBase + 26);
            });
        }

        [Fact]
        public void ConnectV4IPAddressListToAnyV6DualHost_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback },
                IPAddress.IPv6Any, true, TestPortBase + 27);
        }

        [Fact]
        public void ConnectV4V6IPAddressListToV4Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.Loopback, false, TestPortBase + 28);
        }

        [Fact]
        public void ConnectV6V4IPAddressListToV4Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.Loopback, false, TestPortBase + 29);
        }

        [Fact]
        public void ConnectV4V6IPAddressListToV6Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.IPv6Loopback, false, TestPortBase + 30);
        }

        [Fact]
        public void ConnectV6V4IPAddressListToV6Host_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.IPv6Loopback, false, TestPortBase + 31);
        }

        [Fact]
        public void ConnectV4V6IPAddressListToDualHost_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.IPv6Any, true, TestPortBase + 32);
        }

        [Fact]
        public void ConnectV6V4IPAddressListToDualHost_Success()
        {
            DualModeConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.IPv6Any, true, TestPortBase + 33);
        }

        private void DualModeConnect_IPAddressListToHost_Helper(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                socket.Connect(connectTo, port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to IPAdddress[]

        #region Connect to host string

        [Fact]
        public void ConnectLoopbackToV4Host_Success()
        {
            DualModeConnect_LoopbackDnsToHost_Helper(IPAddress.Loopback, false, TestPortBase + 40);
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void ConnectLoopbackToV6Host_Success()
        {
            DualModeConnect_LoopbackDnsToHost_Helper(IPAddress.IPv6Loopback, false, TestPortBase + 41);
        }

        [Fact]
        public void ConnectLoopbackToDualHost_Success()
        {
            DualModeConnect_LoopbackDnsToHost_Helper(IPAddress.IPv6Any, true, TestPortBase + 42);
        }

        private void DualModeConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                socket.Connect("localhost", port);
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to host string

        #region Connect to DnsEndPoint

        [Fact]
        public void DualModeSocket_DnsEndPointToV4Host_Success()
        {
            DualModeConnect_DnsEndPointToHost_Helper(IPAddress.Loopback, false, AddressFamily.Unspecified, TestPortBase + 50);
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void DualModeSocket_DnsEndPointToV6Host_Success()
        {
            DualModeConnect_DnsEndPointToHost_Helper(IPAddress.IPv6Loopback, false, AddressFamily.Unspecified, TestPortBase + 51);
        }

        [Fact]
        public void DualModeSocket_DnsEndPointToDualHost_Success()
        {
            DualModeConnect_DnsEndPointToHost_Helper(IPAddress.IPv6Any, true, AddressFamily.Unspecified, TestPortBase + 52);
        }

        private void DualModeConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer, AddressFamily addressFamily, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                socket.Connect(new DnsEndPoint("localhost", port, addressFamily));
                Assert.True(socket.Connected);
            }
        }

        #endregion Connect to DnsEndPoint

        #endregion Connect Sync

        #region BeginConnect

        #region BeginConnect to IPAddress

        [Fact] // Base case
        public void Socket_BeginConnectV4IPAddressToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.BeginConnect(IPAddress.Loopback, TestPortBase + 60, null, null);
            });
        }

        [Fact]
        public void BeginConnectV4IPAddressToV4Host_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 61);
        }

        [Fact]
        public void BeginConnectV6IPAddressToV6Host_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 62);
        }

        [Fact]
        public void BeginConnectV4IPAddressToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 63);
            });
        }

        [Fact]
        public void BeginConnectV6IPAddressToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 64);
            });
        }

        [Fact]
        public void BeginConnectV4IPAddressToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 65);
        }

        [Fact]
        public void BeginConnectV6IPAddressToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 66);
        }

        private void DualModeBeginConnect_IPAddressToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                IAsyncResult async = socket.BeginConnect(connectTo, port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPAddress

        #region BeginConnect to IPEndPoint

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BeginConnectV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;
            Assert.Throws<SocketException>(() =>
            {
                socket.BeginConnect(new IPEndPoint(IPAddress.Loopback, TestPortBase + 70), null, null);
            });
        }

        [Fact]
        public void BeginConnectV4IPEndPointToV4Host_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 71);
        }

        [Fact]
        public void BeginConnectV6IPEndPointToV6Host_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 72);
        }

        [Fact]
        public void BeginConnectV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 73);
            });
        }

        [Fact]
        public void BeginConnectV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 74);
            });
        }

        [Fact]
        public void BeginConnectV4IPEndPointToDualHost_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 75);
        }

        [Fact]
        public void BeginConnectV6IPEndPointToDualHost_Success()
        {
            DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 76);
        }

        private void DualModeBeginConnect_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                IAsyncResult async = socket.BeginConnect(new IPEndPoint(connectTo, port), null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPEndPoint

        #region BeginConnect to IPAddress[]

        [Fact]
        public void BeginConnectV4V6IPAddressListToV4Host_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.Loopback, false, TestPortBase + 80);
        }

        [Fact]
        public void BeginConnectV6V4IPAddressListToV4Host_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.Loopback, false, TestPortBase + 81);
        }

        [Fact]
        public void BeginConnectV4V6IPAddressListToV6Host_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.IPv6Loopback, false, TestPortBase + 82);
        }

        [Fact]
        public void BeginConnectV6V4IPAddressListToV6Host_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.IPv6Loopback, false, TestPortBase + 83);
        }

        [Fact]
        public void BeginConnectV4V6IPAddressListToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.Loopback, IPAddress.IPv6Loopback },
                IPAddress.IPv6Any, true, TestPortBase + 84);
        }

        [Fact]
        public void BeginConnectV6V4IPAddressListToDualHost_Success()
        {
            DualModeBeginConnect_IPAddressListToHost_Helper(
                new IPAddress[] { IPAddress.IPv6Loopback, IPAddress.Loopback },
                IPAddress.IPv6Any, true, TestPortBase + 85);
        }

        private void DualModeBeginConnect_IPAddressListToHost_Helper(IPAddress[] connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                IAsyncResult async = socket.BeginConnect(connectTo, port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to IPAdddress[]

        #region BeginConnect to host string

        [Fact]
        public void BeginConnectLoopbackToV4Host_Success()
        {
            DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress.Loopback, false, TestPortBase + 90);
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void BeginConnectLoopbackToV6Host_Success()
        {
            DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress.IPv6Loopback, false, TestPortBase + 91);
        }

        [Fact]
        public void BeginConnectLoopbackToDualHost_Success()
        {
            DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress.IPv6Any, true, TestPortBase + 92);
        }

        private void DualModeBeginConnect_LoopbackDnsToHost_Helper(IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                IAsyncResult async = socket.BeginConnect("localhost", port, null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to host string

        #region BeginConnect to DnsEndPoint

        [Fact]
        public void DualModeSocket_BeginConnectDnsEndPointToV4Host_Success()
        {
            DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress.Loopback, false, TestPortBase + 100);
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void DualModeSocket_BeginConnectDnsEndPointToV6Host_Success()
        {
            DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress.IPv6Loopback, false, TestPortBase + 101);
        }

        [Fact]
        public void DualModeSocket_BeginConnectDnsEndPointToDualHost_Success()
        {
            DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress.IPv6Any, true, TestPortBase + 102);
        }

        private void DualModeBeginConnect_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                IAsyncResult async = socket.BeginConnect(new DnsEndPoint("localhost", port), null, null);
                socket.EndConnect(async);
                Assert.True(socket.Connected);
            }
        }

        #endregion BeginConnect to DnsEndPoint

        #endregion BeginConnect

        #region ConnectAsync

        #region ConnectAsync to IPEndPoint

        [Fact] // Base case
        public void Socket_ConnectAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = false;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestPortBase + 110);
            Assert.Throws<NotSupportedException>(() =>
            {
                socket.ConnectAsync(args);
            });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 111);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 112);
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToV6Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 113);
            });
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToV4Host_Fails()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 114);
            });
        }

        [Fact]
        public void ConnectAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 115);
        }

        [Fact]
        public void ConnectAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 116);
        }

        private void DualModeConnectAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                Assert.True(waitHandle.WaitOne(5000), "Timed out while waiting for connection");
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        #endregion ConnectAsync to IPAddress

        #region ConnectAsync to DnsEndPoint

        [Fact]
        public void DualModeSocket_ConnectAsyncDnsEndPointToV4Host_Success()
        {
            DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress.Loopback, false, TestPortBase + 120);
        }

        [Fact]
        [ActiveIssue(DummyLoopbackV6Issue, PlatformID.AnyUnix)]
        public void DualModeSocket_ConnectAsyncDnsEndPointToV6Host_Success()
        {
            DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress.IPv6Loopback, false, TestPortBase + 121);
        }

        [Fact]
        public void DualModeSocket_ConnectAsyncDnsEndPointToDualHost_Success()
        {
            DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress.IPv6Any, true, TestPortBase + 122);
        }

        private void DualModeConnectAsync_DnsEndPointToHost_Helper(IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            using (SocketServer server = new SocketServer(_log, listenOn, dualModeServer, port))
            {
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(AsyncCompleted);
                args.RemoteEndPoint = new DnsEndPoint("localhost", port);
                args.UserToken = waitHandle;

                socket.ConnectAsync(args);

                waitHandle.WaitOne();
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }
                Assert.True(socket.Connected);
            }
        }

        #endregion ConnectAsync to DnsEndPoint

        #endregion ConnectAsync

        #endregion Connect

        #region Accept

        #region Bind

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BindV4IPEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.DualMode = false;
                Assert.Throws<SocketException>(() =>
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, TestPortBase + 130));
                });
            }
        }

        [Fact] // Base Case; BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindMappedV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback.MapToIPv6(), TestPortBase + 131));
            }
        }

        [Fact] // BSoD on Win7, Win8 with IPv4 uninstalled
        public void BindV4IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, TestPortBase + 132));
            }
        }

        [Fact]
        public void BindV6IPEndPoint_Success()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 133));
            }
        }

        [Fact]
        public void Socket_BindDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    socket.Bind(new DnsEndPoint("localhost", TestPortBase + 134));
                });
            }
        }

        [Fact]
        // "An invalid argument was supplied"
        public void Socket_EnableDualModeAfterBind_Throws()
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.DualMode = false;
                serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, TestPortBase + 135));
                Assert.Throws<SocketException>(() =>
                {
                    serverSocket.DualMode = true;
                });
            }
        }

        #endregion Bind

        #region Accept Sync

        [Fact]
        public void AcceptV4BoundToSpecificV4_Success()
        {
            Accept_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 140);
        }

        [Fact]
        public void AcceptV4BoundToAnyV4_Success()
        {
            Accept_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 141);
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void AcceptV6BoundToSpecificV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 142);
        }

        [Fact]
        public void AcceptV6BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 143);
        }

        [Fact]
        public void AcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 144);
            });
        }

        [Fact]
        public void AcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 145);
            });
        }

        [Fact]
        public void AcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                Accept_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 146);
            });
        }

        [Fact]
        public void AcceptV4BoundToAnyV6_Success()
        {
            Accept_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 147);
        }

        private void Accept_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(new IPEndPoint(listenOn, port));
                serverSocket.Listen(1);
                SocketClient client = new SocketClient(serverSocket, connectTo, port);
                Socket clientSocket = serverSocket.Accept();
                Assert.True(clientSocket.Connected);
                Assert.True(clientSocket.DualMode);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
            }
        }

        #endregion Accept Sync

        #region Accept Begin/End

        [Fact]
        public void BeginAcceptV4BoundToSpecificV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 150);
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV4_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 151);
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void BeginAcceptV6BoundToSpecificV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 152);
        }

        [Fact]
        public void BeginAcceptV6BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 153);
        }

        [Fact]
        public void BeginAcceptV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 154);
            });
        }

        [Fact]
        public void BeginAcceptV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 155);
            });
        }

        [Fact]
        public void BeginAcceptV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_BeginAccept_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 156);
            });
        }

        [Fact]
        public void BeginAcceptV4BoundToAnyV6_Success()
        {
            DualModeConnect_BeginAccept_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 157);
        }

        private void DualModeConnect_BeginAccept_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(new IPEndPoint(listenOn, port));
                serverSocket.Listen(1);
                IAsyncResult async = serverSocket.BeginAccept(null, null);
                SocketClient client = new SocketClient(serverSocket, connectTo, port);
                Socket clientSocket = serverSocket.EndAccept(async);
                Assert.True(clientSocket.Connected);
                Assert.True(clientSocket.DualMode);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
            }
        }

        #endregion Accept Begin/End

        #region Accept Async/Event

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 160);
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV4_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 161);
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void AcceptAsyncV6BoundToSpecificV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 162);
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 163);
        }

        [Fact]
        public void AcceptAsyncV6BoundToSpecificV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 164);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToSpecificV6_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 165);
            });
        }

        [Fact]
        public void AcceptAsyncV6BoundToAnyV4_CantConnect()
        {
            Assert.Throws<SocketException>(() =>
            {
                DualModeConnect_AcceptAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 166);
            });
        }

        [Fact]
        public void AcceptAsyncV4BoundToAnyV6_Success()
        {
            DualModeConnect_AcceptAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 167);
        }

        private void DualModeConnect_AcceptAsync_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                serverSocket.Bind(new IPEndPoint(listenOn, port));
                serverSocket.Listen(1);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += AsyncCompleted;
                ManualResetEvent waitHandle = new ManualResetEvent(false);
                args.UserToken = waitHandle;

                _log.WriteLine(args.GetHashCode() + " SocketAsyncEventArgs with manual event " + waitHandle.GetHashCode());

                serverSocket.AcceptAsync(args);
                SocketClient client = new SocketClient(serverSocket, connectTo, port);
                Assert.True(waitHandle.WaitOne(5000), "Timed out while waiting for connection");

                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                Socket clientSocket = args.AcceptSocket;
                Assert.NotNull(clientSocket);
                Assert.True(clientSocket.Connected);
                Assert.True(clientSocket.DualMode);
                Assert.Equal(AddressFamily.InterNetworkV6, clientSocket.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), ((IPEndPoint)clientSocket.LocalEndPoint).Address);
            }
        }

        #endregion Accept Async/Event

        #endregion Accept

        #region Connectionless

        [Fact]
        public void DualModeUdpConstructor_DualModeConfgiured()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
            Assert.True(socket.DualMode);
        }

        #region SendTo

        #region SendTo Sync IPEndPoint

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_SendToV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;
            Assert.Throws<SocketException>(() =>
            {
                socket.SendTo(new byte[1], new IPEndPoint(IPAddress.Loopback, TestPortBase + 170));
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.SendTo(new byte[1], new DnsEndPoint("localhost", TestPortBase + 171));
            });
        }

        [Fact]
        public void SendToV4IPEndPointToV4Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 172);
        }

        [Fact]
        public void SendToV6IPEndPointToV6Host_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 173);
        }

        [Fact]
        public void SendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 174);
            });
        }

        [Fact]
        public void SendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 175);
            });
        }

        [Fact]
        public void SendToV4IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 176);
        }

        [Fact]
        public void SendToV6IPEndPointToDualHost_Success()
        {
            DualModeSendTo_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 177);
        }

        private void DualModeSendTo_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(listenOn, dualModeServer, port))
            {
                int sent = client.SendTo(new byte[1], new IPEndPoint(connectTo, port));
                Assert.Equal(1, sent);

                bool success = server.WaitHandle.WaitOne(500); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Sync

        #region SendTo Begin/End

        [Fact] // Base case
        // "The system detected an invalid pointer address in attempting to use a pointer argument in a call"
        public void Socket_BeginSendToV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            Assert.Throws<SocketException>(() =>
            {
                socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, TestPortBase + 180),
                null, null);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginSendToDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new DnsEndPoint("localhost", TestPortBase + 181), null, null);
            });
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV4Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 182);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV6Host_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 183);
        }

        [Fact]
        public void BeginSendToV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 184);
            });
        }

        [Fact]
        public void BeginSendToV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 185);
            });
        }

        [Fact]
        public void BeginSendToV4IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 186);
        }

        [Fact]
        public void BeginSendToV6IPEndPointToDualHost_Success()
        {
            DualModeBeginSendTo_EndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 187);
        }

        private void DualModeBeginSendTo_EndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(listenOn, dualModeServer, port))
            {
                IAsyncResult async = client.BeginSendTo(new byte[1], 0, 1, SocketFlags.None, new IPEndPoint(connectTo, port), null, null);

                int sent = client.EndSendTo(async);
                Assert.Equal(1, sent);

                bool success = server.WaitHandle.WaitOne(100); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Begin/End

        #region SendTo Async/Event

        [Fact] // Base case
        [ActiveIssue(DummyErrorMismatchIssue, PlatformID.AnyUnix)]
        public void Socket_SendToAsyncV4IPEndPointToV4Host_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestPortBase + 190);
            args.SetBuffer(new byte[1], 0, 1);

            bool async = socket.SendToAsync(args);
            Assert.False(async);
            Assert.Equal(SocketError.Fault, args.SocketError);
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_SendToAsyncDnsEndPoint_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 191);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.SendToAsync(args);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV4Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.Loopback, false, TestPortBase + 192);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV6Host_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 193);
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToV6Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, false, TestPortBase + 194);
            });
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToV4Host_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, false, TestPortBase + 195);
            });
        }

        [Fact]
        public void SendToAsyncV4IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.Loopback, IPAddress.IPv6Any, true, TestPortBase + 196);
        }

        [Fact]
        public void SendToAsyncV6IPEndPointToDualHost_Success()
        {
            DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Any, true, TestPortBase + 197);
        }

        private void DualModeSendToAsync_IPEndPointToHost_Helper(IPAddress connectTo, IPAddress listenOn, bool dualModeServer, int port)
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            Socket client = new Socket(SocketType.Dgram, ProtocolType.Udp);
            using (SocketUdpServer server = new SocketUdpServer(listenOn, dualModeServer, port))
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = client.SendToAsync(args);
                if (async)
                {
                    Assert.True(waitHandle.WaitOne(5000), "Timeout while waiting for connection");
                }

                Assert.Equal(1, args.BytesTransferred);
                if (args.SocketError != SocketError.Success)
                {
                    throw new SocketException((int)args.SocketError);
                }

                bool success = server.WaitHandle.WaitOne(100); // Make sure the bytes were received
                if (!success)
                {
                    throw new TimeoutException();
                }
            }
        }

        #endregion SendTo Async/Event

        #endregion SendTo

        #region ReceiveFrom

        #region ReceiveFrom Sync

        [Fact] // Base case
        public void Socket_ReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, TestPortBase + 200);
            Assert.Throws<ArgumentException>(() =>
            {
                int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
            });
        }

        [Fact] // Base case
        public void Socket_ReceiveFromDnsEndPoint_Throws()
        {
            // "The parameter remoteEP must not be of type DnsEndPoint."
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint receivedFrom = new DnsEndPoint("localhost", TestPortBase + 201, AddressFamily.InterNetworkV6);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 201));
                Assert.Throws<ArgumentException>(() =>
                {
                    int received = socket.ReceiveFrom(new byte[1], ref receivedFrom);
                });
            }
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveFromV4BoundToSpecificV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 202);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveFromV4BoundToAnyV4_Success()
        {
            ReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 203);
        }

        [Fact]
        public void ReceiveFromV6BoundToSpecificV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 204);
        }

        [Fact]
        public void ReceiveFromV6BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 205);
        }

        [Fact]
        public void ReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 206);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void ReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 207);
            });
        }

        [Fact]
        public void ReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 208);
            });
        }

        [Fact]
        public void ReceiveFromV4BoundToAnyV6_Success()
        {
            ReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 209);
        }

        private void ReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.ReceiveFrom(new byte[1], ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Sync

        #region ReceiveFrom Begin/End

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, TestPortBase + 210);
            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint receivedFrom = new DnsEndPoint("localhost", TestPortBase + 211, AddressFamily.InterNetworkV6);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 211));

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);
                });
            }
        }

        [Fact]
        public void BeginReceiveFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 212);
        }

        [Fact]
        public void BeginReceiveFromV4BoundToAnyV4_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 213);
        }

        [Fact]
        public void BeginReceiveFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 214);
        }

        [Fact]
        public void BeginReceiveFromV6BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 215);
        }

        [Fact]
        public void BeginReceiveFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 216);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void BeginReceiveFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 217);
            });
        }

        [Fact]
        public void BeginReceiveFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 218);
            });
        }

        [Fact]
        public void BeginReceiveFromV4BoundToAnyV6_Success()
        {
            BeginReceiveFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 219);
        }

        private void BeginReceiveFrom_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                IAsyncResult async = serverSocket.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref receivedFrom, null, null);

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);
                bool success = async.AsyncWaitHandle.WaitOne(500);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveFrom(async, ref receivedFrom);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);
            }
        }

        #endregion ReceiveFrom Begin/End

        #region ReceiveFrom Async/Event

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestPortBase + 220);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.ReceiveFromAsync(args);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 221));

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 221, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.ReceiveFromAsync(args);
                });
            }
        }

        [Fact]
        public void ReceiveFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 222);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 223);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 224);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 225);
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 226);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void ReceiveFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 227);
            });
        }

        [Fact]
        public void ReceiveFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 228);
            });
        }

        [Fact]
        public void ReceiveFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 229);
        }

        private void ReceiveFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(listenOn, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.UserToken = waitHandle;
                args.Completed += AsyncCompleted;

                bool async = serverSocket.ReceiveFromAsync(args);
                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);
                if (async && !waitHandle.WaitOne(200))
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

        #endregion ReceiveFrom

        #region ReceiveMessageFrom

        #region ReceiveMessageFrom Sync

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, TestPortBase + 230);
            SocketFlags socketFlags = SocketFlags.None;
            IPPacketInformation ipPacketInformation;
            Assert.Throws<ArgumentException>(() =>
            {
                int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                EndPoint receivedFrom = new DnsEndPoint("localhost", TestPortBase + 231, AddressFamily.InterNetworkV6);
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 231));
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;

                Assert.Throws<ArgumentException>(() =>
                {
                    int received = socket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                });
            }
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, TestPortBase + 232);
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback, TestPortBase + 233);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 234);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV4BoundToAnyV4_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 235);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 236);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromV6BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 237);
        }

        [Fact]
        public void ReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 238);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 239);
            });
        }

        [Fact]
        public void ReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<SocketException>(() =>
            {
                ReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 240);
            });
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromV4BoundToAnyV6_Success()
        {
            ReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 241);
        }

        private void ReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                int received = 0;
                Assert.Throws<SocketException>(() =>
                {
                    // This is a false start.
                    // http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.receivemessagefrom.aspx
                    // "...the returned IPPacketInformation object will only be valid for packets which arrive at the
                    // local computer after the socket option has been set. If a socket is sent packets between when
                    // it is bound to a local endpoint (explicitly by the Bind method or implicitly by one of the Connect,
                    // ConnectAsync, SendTo, or SendToAsync methods) and its first call to the ReceiveMessageFrom method,
                    // calls to ReceiveMessageFrom method will return invalid IPPacketInformation objects for these packets."
                    received = serverSocket.ReceiveMessageFrom(new byte[1], 0, 1, ref socketFlags, ref receivedFrom, out ipPacketInformation);
                });

                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);

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

                // TODO: Move to NetworkInformation tests.
                // Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, ipPacketInformation.Interface);
            }
        }

        #endregion ReceiveMessageFrom Sync

        #region ReceiveMessageFrom Begin/End

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_BeginReceiveMessageFromV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            EndPoint receivedFrom = new IPEndPoint(IPAddress.Loopback, TestPortBase + 250);
            SocketFlags socketFlags = SocketFlags.None;

            Assert.Throws<ArgumentException>(() =>
            {
                socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_BeginReceiveMessageFromDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 251));

                EndPoint receivedFrom = new DnsEndPoint("localhost", TestPortBase + 251, AddressFamily.InterNetworkV6);
                SocketFlags socketFlags = SocketFlags.None;
                Assert.Throws<ArgumentException>(() =>
                {
                    socket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);
                });
            }
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToSpecificMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, TestPortBase + 252);
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToAnyMappedV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback, TestPortBase + 253);
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToSpecificV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 254);
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToAnyV4_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 255);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToSpecificV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 256);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void BeginReceiveMessageFromV6BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 257);
        }

        [Fact]
        public void BeginReceiveMessageFromV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 258);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 259);
            });
        }

        [Fact]
        public void BeginReceiveMessageFromV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                BeginReceiveMessageFrom_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 260);
            });
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void BeginReceiveMessageFromV4BoundToAnyV6_Success()
        {
            BeginReceiveMessageFrom_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 261);
        }

        private void BeginReceiveMessageFrom_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                EndPoint receivedFrom = new IPEndPoint(connectTo, port);
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                IAsyncResult async = serverSocket.BeginReceiveMessageFrom(new byte[1], 0, 1, socketFlags, ref receivedFrom, null, null);

                IPEndPoint remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);
                bool success = async.AsyncWaitHandle.WaitOne(500);
                if (!success)
                {
                    throw new TimeoutException();
                }

                receivedFrom = new IPEndPoint(connectTo, port);
                int received = serverSocket.EndReceiveMessageFrom(async, ref socketFlags, ref receivedFrom, out ipPacketInformation);

                Assert.Equal(1, received);
                Assert.Equal<Type>(receivedFrom.GetType(), typeof(IPEndPoint));

                remoteEndPoint = receivedFrom as IPEndPoint;
                Assert.Equal(AddressFamily.InterNetworkV6, remoteEndPoint.AddressFamily);
                Assert.Equal(connectTo.MapToIPv6(), remoteEndPoint.Address);

                Assert.Equal(SocketFlags.None, socketFlags);
                Assert.NotNull(ipPacketInformation);
                Assert.Equal(connectTo, ipPacketInformation.Address);

                // TODO: Move to NetworkInformation tests.
                //Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, ipPacketInformation.Interface);
            }
        }

        #endregion ReceiveMessageFrom Begin/End

        #region ReceiveMessageFrom Async/Event

        [Fact] // Base case
        // "The supplied EndPoint of AddressFamily InterNetwork is not valid for this Socket, use InterNetworkV6 instead."
        public void Socket_ReceiveMessageFromAsyncV4IPEndPointFromV4Client_Throws()
        {
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.DualMode = false;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, TestPortBase + 270);
            args.SetBuffer(new byte[1], 0, 1);

            Assert.Throws<ArgumentException>(() =>
            {
                socket.ReceiveMessageFromAsync(args);
            });
        }

        [Fact] // Base case
        // "The parameter remoteEP must not be of type DnsEndPoint."
        public void Socket_ReceiveMessageFromAsyncDnsEndPoint_Throws()
        {
            using (Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, TestPortBase + 271));

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new DnsEndPoint("localhost", TestPortBase + 271, AddressFamily.InterNetworkV6);
                args.SetBuffer(new byte[1], 0, 1);

                Assert.Throws<ArgumentException>(() =>
                {
                    socket.ReceiveMessageFromAsync(args);
                });
            }
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, TestPortBase + 272);
        }

        [Fact] // Base case
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToAnyMappedV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any.MapToIPv6(), IPAddress.Loopback, TestPortBase + 273);
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.Loopback, TestPortBase + 274);
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToAnyV4_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.Loopback, TestPortBase + 275);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback, TestPortBase + 276);
        }

        [Fact]
        [ActiveIssue(DummyOSXPacketInfoIssue, PlatformID.OSX)]
        public void ReceiveMessageFromAsyncV6BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.IPv6Loopback, TestPortBase + 277);
        }

        [Fact]
        public void ReceiveMessageFromAsyncV6BoundToSpecificV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Loopback, IPAddress.IPv6Loopback, TestPortBase + 278);
            });
        }

        [Fact]
        [ActiveIssue(DummyDualModeV6Issue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToSpecificV6_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.IPv6Loopback, IPAddress.Loopback, TestPortBase + 279);
            });
        }

        [Fact]
        public void ReceiveMessageFromAsyncV6BoundToAnyV4_NotReceived()
        {
            Assert.Throws<TimeoutException>(() =>
            {
                ReceiveMessageFromAsync_Helper(IPAddress.Any, IPAddress.IPv6Loopback, TestPortBase + 280);
            });
        }

        [Fact]
        [ActiveIssue(DummyOSXDualModePacketInfoIssue, PlatformID.AnyUnix)]
        public void ReceiveMessageFromAsyncV4BoundToAnyV6_Success()
        {
            ReceiveMessageFromAsync_Helper(IPAddress.IPv6Any, IPAddress.Loopback, TestPortBase + 281);
        }

        private void ReceiveMessageFromAsync_Helper(IPAddress listenOn, IPAddress connectTo, int port)
        {
            using (Socket serverSocket = new Socket(SocketType.Dgram, ProtocolType.Udp))
            {
                serverSocket.ReceiveTimeout = 500;
                serverSocket.Bind(new IPEndPoint(listenOn, port));

                ManualResetEvent waitHandle = new ManualResetEvent(false);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = new IPEndPoint(connectTo, port);
                args.SetBuffer(new byte[1], 0, 1);
                args.Completed += AsyncCompleted;
                args.UserToken = waitHandle;

                bool async = serverSocket.ReceiveMessageFromAsync(args);
                Assert.True(async);

                SocketUdpClient client = new SocketUdpClient(serverSocket, connectTo, port);
                if (!waitHandle.WaitOne(500))
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

                // TODO: Move to NetworkInformation tests.
                // Assert.Equal(NetworkInterface.IPv6LoopbackInterfaceIndex, args.ReceiveMessageFromPacketInfo.Interface);
            }
        }

        #endregion ReceiveMessageFrom Async/Event

        #endregion ReceiveMessageFrom

        #endregion Connectionless

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

        #region Helpers

        private class SocketServer : IDisposable
        {
            private readonly ITestOutputHelper _log;
            private TcpListener _server;

            public SocketServer(ITestOutputHelper log, IPAddress address, bool dualMode, int port)
            {
                _log = log;
                _server = new TcpListener(address, port);
                if (dualMode)
                {
                    _server.Server.DualMode = dualMode;
                }

                _server.Start();
                _server.BeginAcceptSocket(Accepted, null);
            }

            private void Accepted(IAsyncResult ar)
            {
                try
                {
                    Socket socket = _server.EndAcceptSocket(ar);
                    _log.WriteLine("Accpeted Socket: " + socket.RemoteEndPoint);
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
            }

            public void Dispose()
            {
                try
                {
                    _server.Stop();
                }
                catch (Exception) { }
            }
        }

        private class SocketClient
        {
            private IPAddress _connectTo;
            private Socket _serverSocket;
            private int _port;

            public SocketClient(Socket serverSocket, IPAddress connectTo, int port)
            {
                _connectTo = connectTo;
                _serverSocket = serverSocket;
                _port = port;

                ThreadPool.QueueUserWorkItem(ConnectClient);
            }

            private void ConnectClient(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(_connectTo, _port);
                }
                catch (SocketException)
                {
                    Task.Delay(100).Wait(); // Give the other end a chance to call Accept().
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
        }

        private class SocketUdpServer : IDisposable
        {
            private Socket _server;
            private ManualResetEvent _waitHandle = new ManualResetEvent(false);

            public EventWaitHandle WaitHandle
            {
                get { return _waitHandle; }
            }

            public SocketUdpServer(IPAddress address, bool dualMode, int port)
            {
                _server = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                if (dualMode)
                {
                    _server.DualMode = dualMode;
                }

                _server.Bind(new IPEndPoint(address, port));

                IPAddress remoteAddress = address.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                EndPoint remote = new IPEndPoint(remoteAddress , 0);
                _server.BeginReceiveFrom(new byte[1], 0, 1, SocketFlags.None, ref remote, Received, null);
            }

            private void Received(IAsyncResult ar)
            {
                try
                {
                    IPAddress remoteAddress = _server.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any;
                    EndPoint remote = new IPEndPoint(remoteAddress, 0);
                    int byteCount = _server.EndReceiveFrom(ar, ref remote);
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }

                WaitHandle.Set();
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

        private class SocketUdpClient
        {
            private int _port;
            private IPAddress _connectTo;
            private Socket _serverSocket;

            public SocketUdpClient(Socket serverSocket, IPAddress connectTo, int port)
            {
                _connectTo = connectTo;
                _port = port;
                _serverSocket = serverSocket;
                ThreadPool.QueueUserWorkItem(ClientSend);
            }

            private void ClientSend(object state)
            {
                try
                {
                    Socket socket = new Socket(_connectTo.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.SendTo(new byte[1], new IPEndPoint(_connectTo, _port));
                }
                catch (SocketException)
                {
                    _serverSocket.Dispose(); // Cancels the test
                }
            }
        }

        private void AsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            ManualResetEvent waitHandle = (ManualResetEvent)e.UserToken;
            _log.WriteLine(
                "AsyncCompleted: " + e.GetHashCode() + " SocketAsyncEventArgs with manual event " +
                waitHandle.GetHashCode() + " error: " + e.SocketError);

            waitHandle.Set();
        }

        #endregion Helpers
    }
}
