// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;

namespace System.Net.Sockets.Tests
{
    public class TcpClientTest
    {
        private readonly ITestOutputHelper _log;

        public TcpClientTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Theory]
        [InlineData(AddressFamily.Banyan)]
        [InlineData(AddressFamily.DataLink)]
        [InlineData(AddressFamily.NetBios)]
        [InlineData(AddressFamily.Unix)]
        public void Ctor_InvalidFamily_Throws(AddressFamily family)
        {
            AssertExtensions.Throws<ArgumentException>("family", () => new TcpClient(family));
        }

        [Fact]
        public void Ctor_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("localEP", () => new TcpClient(null));
            AssertExtensions.Throws<ArgumentNullException>("hostname", () => new TcpClient(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new TcpClient("localhost", -1));
        }

        [Fact]
        public void Connect_InvalidArguments_Throws()
        {
            using (var client = new TcpClient())
            {
                AssertExtensions.Throws<ArgumentNullException>("hostname", () => client.Connect((string)null, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => client.Connect("localhost", -1));

                AssertExtensions.Throws<ArgumentNullException>("address", () => client.Connect((IPAddress)null, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => client.Connect(IPAddress.Loopback, -1));

                AssertExtensions.Throws<ArgumentNullException>("remoteEP", () => client.Connect(null));
            }
        }

        [Fact]
        public void GetStream_NotConnected_Throws()
        {
            using (var client = new TcpClient())
            {
                Assert.Throws<InvalidOperationException>(() => client.GetStream());
            }
        }

        [Fact]
        public void Active_Roundtrips()
        {
            using (var client = new DerivedTcpClient())
            {
                Assert.False(client.Active);

                client.Active = true;
                Assert.True(client.Active);
                Assert.Throws<SocketException>(() => client.Connect("anywhere", 0));

                client.Active = false;
                Assert.False(client.Active);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisposeClose_OperationsThrow(bool close)
        {
            var tcpClient = new TcpClient();

            for (int i = 0; i < 2; i++) // verify double dispose doesn't throw
            {
                if (close) tcpClient.Close();
                else tcpClient.Dispose();
            }

            Assert.Throws<ObjectDisposedException>(() => tcpClient.Connect(null));
            Assert.Throws<ObjectDisposedException>(() => tcpClient.Connect(IPAddress.Loopback, 0));
            Assert.Throws<ObjectDisposedException>(() => tcpClient.Connect("localhost", 0));
            Assert.Throws<ObjectDisposedException>(() => tcpClient.GetStream());
        }

        [OuterLoop]
        [Fact]
        public void Ctor_StringInt_ConnectsSuccessfully()
        {
            string host = System.Net.Test.Common.Configuration.Sockets.SocketServer.IdnHost;
            int port = System.Net.Test.Common.Configuration.Sockets.SocketServer.Port;

            using (TcpClient client = new TcpClient(host, port))
            {
                Assert.True(client.Connected);
                Assert.NotNull(client.Client);
                Assert.Same(client.Client, client.Client);
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        public async Task ConnectAsync_DnsEndPoint_Success(int mode)
        {
            using (var client = new DerivedTcpClient())
            {
                Assert.False(client.Connected);
                Assert.False(client.Active);

                string host = System.Net.Test.Common.Configuration.Sockets.SocketServer.IdnHost;
                int port = System.Net.Test.Common.Configuration.Sockets.SocketServer.Port;

                IPAddress[] addresses;
                switch (mode)
                {
                    case 0:
                        await client.ConnectAsync(host, port);
                        break;
                    case 1:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(addresses[0], port);
                        break;
                    case 2:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(addresses, port);
                        break;

                    case 3:
                        await Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, host, port, null);
                        break;
                    case 4:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, addresses[0], port, null);
                        break;
                    case 5:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await Task.Factory.FromAsync(client.BeginConnect, client.EndConnect, addresses, port, null);
                        break;

                    case 6:
                        await client.ConnectAsync(host, port, CancellationToken.None);
                        break;
                    case 7:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(addresses[0], port, CancellationToken.None);
                        break;
                    case 8:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(new IPEndPoint(addresses[0], port));
                        break;
                    case 9:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(new IPEndPoint(addresses[0], port), CancellationToken.None);
                        break;
                    case 10:
                        addresses = await Dns.GetHostAddressesAsync(host);
                        await client.ConnectAsync(addresses, port, CancellationToken.None);
                        break;
                }

                Assert.True(client.Active);
                Assert.True(client.Connected);
                Assert.NotNull(client.Client);
                Assert.Same(client.Client, client.Client);

                using (NetworkStream s = client.GetStream())
                {
                    byte[] getRequest = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
                    await s.WriteAsync(getRequest, 0, getRequest.Length);
                    Assert.NotEqual(-1, s.ReadByte()); // just verify we successfully get any data back
                }
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Connect_DnsEndPoint_Success(int mode)
        {
            using (TcpClient client = new TcpClient())
            {
                Assert.False(client.Connected);

                string host = System.Net.Test.Common.Configuration.Sockets.SocketServer.IdnHost;
                int port = System.Net.Test.Common.Configuration.Sockets.SocketServer.Port;

                if (mode == 0)
                {
                    client.Connect(host, port);
                }
                else if (mode == 1)
                {
                    client.Client = null;
                    client.Connect(host, port);
                }
                else
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(host);
                    if (mode == 2)
                    {
                        client.Connect(addresses[0], port);
                    }
                    else
                    {
                        client.Connect(addresses, port);
                    }
                }

                Assert.True(client.Connected);
                Assert.NotNull(client.Client);
                Assert.Same(client.Client, client.Client);

                using (NetworkStream s = client.GetStream())
                {
                    byte[] getRequest = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
                    s.Write(getRequest, 0, getRequest.Length);
                    Assert.NotEqual(-1, s.ReadByte()); // just verify we successfully get any data back
                }
            }
        }

        [OuterLoop]
        [Fact]
        public void ConnectedAvailable_InitialValues_Default()
        {
            using (TcpClient client = new TcpClient())
            {
                Assert.False(client.Connected);
                Assert.Equal(0, client.Available);
            }
        }

        [OuterLoop]
        [Fact]
        public void ConnectedAvailable_NullClient()
        {
            using (TcpClient client = new TcpClient())
            {
                client.Client = null;

                Assert.False(client.Connected);
                Assert.Equal(0, client.Available);
            }
        }

        [OuterLoop]
        [Fact]
        public void ExclusiveAddressUse_NullClient()
        {
            using (TcpClient client = new TcpClient())
            {
                client.Client = null;

                Assert.False(client.ExclusiveAddressUse);
            }
        }

        [Fact]
        public void Roundtrip_ExclusiveAddressUse_GetEqualsSet_True()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ExclusiveAddressUse = true;
                Assert.True(client.ExclusiveAddressUse);
            }
        }

        [Fact]
        public void Roundtrip_ExclusiveAddressUse_GetEqualsSet_False()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ExclusiveAddressUse = false;
                Assert.False(client.ExclusiveAddressUse);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_LingerOption_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.LingerState = new LingerOption(true, 42);
                Assert.True(client.LingerState.Enabled);
                Assert.Equal(42, client.LingerState.LingerTime);

                client.LingerState = new LingerOption(true, 0);
                Assert.True(client.LingerState.Enabled);
                Assert.Equal(0, client.LingerState.LingerTime);

                client.LingerState = new LingerOption(false, 0);
                Assert.False(client.LingerState.Enabled);
                Assert.Equal(0, client.LingerState.LingerTime);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_NoDelay_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.NoDelay = true;
                Assert.True(client.NoDelay);
                client.NoDelay = false;
                Assert.False(client.NoDelay);
            }
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void Ttl_Set_GetEqualsSet(AddressFamily af)
        {
            using (TcpClient client = new TcpClient(af))
            {
                short newTtl = client.Client.Ttl;
                // Change default ttl.
                newTtl += (short)((newTtl < 255) ? 1 : -1);
                client.Client.Ttl = newTtl;
                Assert.Equal(newTtl, client.Client.Ttl);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_ReceiveBufferSize_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ReceiveBufferSize = 4096;
                Assert.InRange(client.ReceiveBufferSize, 4096, int.MaxValue);
                client.ReceiveBufferSize = 8192;
                Assert.InRange(client.ReceiveBufferSize, 8192, int.MaxValue);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_SendBufferSize_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.SendBufferSize = 4096;
                Assert.InRange(client.SendBufferSize, 4096, int.MaxValue);
                client.SendBufferSize = 8192;
                Assert.InRange(client.SendBufferSize, 8192, int.MaxValue);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_ReceiveTimeout_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ReceiveTimeout = 1;
                Assert.Equal(1, client.ReceiveTimeout);
                client.ReceiveTimeout = 0;
                Assert.Equal(0, client.ReceiveTimeout);
            }
        }

        [OuterLoop]
        [Fact]
        public void Roundtrip_SendTimeout_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.SendTimeout = 1;
                Assert.Equal(1, client.SendTimeout);
                client.SendTimeout = 0;
                Assert.Equal(0, client.SendTimeout);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task Properties_PersistAfterConnect()
        {
            using (TcpClient client = new TcpClient())
            {
                // Set a few properties
                client.LingerState = new LingerOption(true, 1);
                client.ReceiveTimeout = 42;
                client.SendTimeout = 84;

                await client.ConnectAsync(System.Net.Test.Common.Configuration.Sockets.SocketServer.IdnHost, System.Net.Test.Common.Configuration.Sockets.SocketServer.Port);

                // Verify their values remain as were set before connecting
                Assert.True(client.LingerState.Enabled);
                Assert.Equal(1, client.LingerState.LingerTime);
                Assert.Equal(42, client.ReceiveTimeout);
                Assert.Equal(84, client.SendTimeout);

                // Note: not all properties can be tested for this on all OSes, as some
                // properties are modified by the OS, e.g. Linux will double whatever
                // buffer size you set and return that double value.  OSes may also enforce
                // minimums and maximums, silently capping to those amounts.
            }
        }

        [OuterLoop]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_CancelsConnectAsync(bool connectByName)
        {
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Set up a server socket to which to connect
                server.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                server.Listen(1);
                var endpoint = (IPEndPoint)server.LocalEndPoint;

                // Connect asynchronously...
                var client = new TcpClient();
                Task connectTask = connectByName ?
                    client.ConnectAsync("localhost", endpoint.Port) :
                    client.ConnectAsync(endpoint.Address, endpoint.Port);

                // ...and hopefully before it's completed connecting, dispose.
                var sw = Stopwatch.StartNew();
                client.Dispose();

                // There is a race condition here.  If the connection succeeds before the
                // disposal, then the task will complete successfully.  Otherwise, it should
                // fail with an exception.
                try
                {
                    await connectTask;
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted) { }
                sw.Stop();

                Assert.Null(client.Client); // should be nulled out after Dispose
            }
        }

        [Fact]
        public void Connect_Dual_Success()
        {
            if (!Socket.OSSupportsIPv6)
            {
                return;
            }

            using (var server = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                // Set up a server socket to which to connect
                server.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                server.Listen(1);
                var endpoint = (IPEndPoint)server.LocalEndPoint;

                using (TcpClient client = new TcpClient())
                {
                    // Some platforms may not support IPv6 dual mode and they should fall-back to IPv4
                    // without throwing exception. However in such case attempt to connect to IPv6 would still fail.
                    if (client.Client.AddressFamily == AddressFamily.InterNetworkV6 && client.Client.DualMode)
                    {
                        client.Connect(endpoint);
                    }
                }
            }
        }

        [Theory]
        [InlineData(false, "::ffff:127.0.0.1")]
        [InlineData(false, "127.0.0.1")]
        [InlineData(false, "localhost")]
        [InlineData(true, "::1")]
        public void CtorConnect_Success(bool useIPv6, string connectString)
        {
            if (!Socket.OSSupportsIPv6)
            {
                return;
            }

            IPAddress serverAddress = useIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;

            using (var server = new Socket(serverAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                // Set up a server socket to which to connect
                server.Bind(new IPEndPoint(serverAddress, 0));
                server.Listen(1);
                var endpoint = (IPEndPoint)server.LocalEndPoint;

                using (TcpClient client = new TcpClient(connectString, endpoint.Port))
                {
                    Assert.True(client.Connected);
                }
            }
        }

        private sealed class DerivedTcpClient : TcpClient
        {
            public new bool Active
            {
                get { return base.Active; }
                set { base.Active = value; }
            }
        }
    }
}
