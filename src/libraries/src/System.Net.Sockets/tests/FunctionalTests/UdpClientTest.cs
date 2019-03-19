// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class UdpClientTest
    {
        // Port 8 is unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int UnusedPort = 8;

        private const int DiscardPort = 9;

        private ManualResetEvent _waitHandle = new ManualResetEvent(false);

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void Ctor_ValidAddressFamily_Succeeds(AddressFamily family)
        {
            new UdpClient(family).Dispose();
        }

        [Theory]
        [InlineData(AddressFamily.DataKit)]
        [InlineData(AddressFamily.Unix)]
        [InlineData(AddressFamily.Unspecified)]
        public void Ctor_InvalidAddressFamily_Throws(AddressFamily family)
        {
            AssertExtensions.Throws<ArgumentException>("family", () => new UdpClient(family));
            AssertExtensions.Throws<ArgumentException>("family", () => new UdpClient(0, family));
        }

        [Fact]
        public void Ctor_InvalidHostName_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("hostname", () => new UdpClient(null, 0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(66000)]
        public void Ctor_InvalidPort_Throws(int port)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new UdpClient(port));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new UdpClient(port, AddressFamily.InterNetwork));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new UdpClient(port, AddressFamily.InterNetworkV6));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => new UdpClient("localhost", port));
        }

        [Fact]
        public void Ctor_NullEndpoint_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("localEP", () => new UdpClient(null));
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_CanSend()
        {
            using (var udpClient = new DerivedUdpClient())
            {
                Assert.Equal(1, udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.Loopback, UnusedPort)));
                Assert.False(udpClient.Active);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_Int_CanSend()
        {
            try
            {
                using (var udpClient = new UdpClient(UnusedPort))
                {
                    Assert.Equal(1, udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.Loopback, UnusedPort)));
                }
            }
            catch (SocketException e)
            {
                // Some configurations require elevation to bind to UnusedPort
                Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_IntAddressFamily_IPv4_CanSend()
        {
            try
            {
                using (var udpClient = new UdpClient(UnusedPort, AddressFamily.InterNetwork))
                {
                    Assert.Equal(1, udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.Loopback, UnusedPort)));
                }
            }
            catch (SocketException e)
            {
                // Some configurations require elevation to bind to UnusedPort
                Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_IntAddressFamily_IPv6_CanSend()
        {
            try
            {
                using (var udpClient = new UdpClient(UnusedPort, AddressFamily.InterNetworkV6))
                {
                    Assert.Equal(1, udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.IPv6Loopback, UnusedPort)));
                }
            }
            catch (SocketException e)
            {
                // Some configurations require elevation to bind to UnusedPort
                Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_IPEndPoint_CanSend()
        {
            try
            {
                using (var udpClient = new UdpClient(new IPEndPoint(IPAddress.IPv6Any, UnusedPort)))
                {
                    Assert.Equal(1, udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.IPv6Loopback, UnusedPort)));
                }
            }
            catch (SocketException e)
            {
                // Some configurations require elevation to bind to UnusedPort
                Assert.Equal(SocketError.AccessDenied, e.SocketErrorCode);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Ctor_StringInt_CanSend()
        {
            using (var udpClient = new DerivedUdpClient("localhost", UnusedPort))
            {
                Assert.Equal(1, udpClient.Send(new byte[1], 1));
                Assert.True(udpClient.Active);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DisposeClose_OperationsThrow(bool close)
        {
            var udpClient = new UdpClient();

            for (int i = 0; i < 2; i++) // verify double dispose doesn't throw
            {
                if (close) udpClient.Close();
                else udpClient.Dispose();
            }

            IPEndPoint remoteEP = null;

            Assert.Throws<ObjectDisposedException>(() => udpClient.BeginSend(new byte[1], 1, null, null));
            Assert.Throws<ObjectDisposedException>(() => udpClient.EndSend(null));

            Assert.Throws<ObjectDisposedException>(() => udpClient.BeginReceive(null, null));
            Assert.Throws<ObjectDisposedException>(() => udpClient.EndReceive(null, ref remoteEP));

            Assert.Throws<ObjectDisposedException>(() => udpClient.JoinMulticastGroup(IPAddress.Loopback));
            Assert.Throws<ObjectDisposedException>(() => udpClient.JoinMulticastGroup(IPAddress.Loopback, IPAddress.Loopback));
            Assert.Throws<ObjectDisposedException>(() => udpClient.JoinMulticastGroup(0, IPAddress.Loopback));
            Assert.Throws<ObjectDisposedException>(() => udpClient.JoinMulticastGroup(IPAddress.Loopback, 0));

            Assert.Throws<ObjectDisposedException>(() => udpClient.DropMulticastGroup(IPAddress.Loopback));
            Assert.Throws<ObjectDisposedException>(() => udpClient.DropMulticastGroup(IPAddress.Loopback, 0));

            Assert.Throws<ObjectDisposedException>(() => udpClient.Connect(null));
            Assert.Throws<ObjectDisposedException>(() => udpClient.Connect(IPAddress.Loopback, 0));
            Assert.Throws<ObjectDisposedException>(() => udpClient.Connect("localhost", 0));

            Assert.Throws<ObjectDisposedException>(() => udpClient.Receive(ref remoteEP));

            Assert.Throws<ObjectDisposedException>(() => udpClient.Send(null, 0, remoteEP));
            Assert.Throws<ObjectDisposedException>(() => udpClient.Send(null, 0));
            Assert.Throws<ObjectDisposedException>(() => udpClient.Send(null, 0, "localhost", 0));
        }

        [Fact]
        public void Finalize_NoExceptionsThrown()
        {
            WeakReference<UdpClient> udpClientWeakRef = CreateAndDiscardUdpClient();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            UdpClient ignored;
            Assert.False(udpClientWeakRef.TryGetTarget(out ignored));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<UdpClient> CreateAndDiscardUdpClient() => new WeakReference<UdpClient>(new DerivedUdpClient());

        [Fact]
        public void Active_Roundtrips()
        {
            using (var udpClient = new DerivedUdpClient())
            {
                Assert.False(udpClient.Active);
                udpClient.Active = true;
                Assert.True(udpClient.Active);
                udpClient.Active = false;
                Assert.False(udpClient.Active);
            }
        }

        [Fact]
        public void Client_Idemptotent()
        {
            using (var udpClient = new UdpClient())
            {
                Socket s = udpClient.Client;
                Assert.NotNull(s);
                Assert.Same(s, udpClient.Client);

                udpClient.Client = null;
                Assert.Null(udpClient.Client);

                udpClient.Client = s;
                Assert.Same(s, udpClient.Client);

                udpClient.Client = null;
                s.Dispose();
            }
        }

        [Fact]
        public void Ttl_Roundtrips()
        {
            using (var udpClient = new UdpClient())
            {
                short ttl = udpClient.Ttl;
                Assert.Equal(ttl, udpClient.Ttl);

                udpClient.Ttl = 100;
                Assert.Equal(100, udpClient.Ttl);
            }
        }

        [PlatformSpecific(~TestPlatforms.OSX)] // macOS doesn't have an equivalent of DontFragment
        [Fact]
        public void DontFragment_Roundtrips()
        {
            using (var udpClient = new UdpClient())
            {
                Assert.False(udpClient.DontFragment);
                udpClient.DontFragment = true;
                Assert.True(udpClient.DontFragment);
                udpClient.DontFragment = false;
                Assert.False(udpClient.DontFragment);
            }
        }

        [Fact]
        public void MulticastLoopback_Roundtrips()
        {
            using (var udpClient = new UdpClient())
            {
                Assert.True(udpClient.MulticastLoopback);
                udpClient.MulticastLoopback = false;
                Assert.False(udpClient.MulticastLoopback);
                udpClient.MulticastLoopback = true;
                Assert.True(udpClient.MulticastLoopback);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        public void EnableBroadcast_Roundtrips()
        {
            using (var udpClient = new UdpClient())
            {
                Assert.False(udpClient.EnableBroadcast);
                udpClient.EnableBroadcast = true;
                Assert.True(udpClient.EnableBroadcast);
                udpClient.EnableBroadcast = false;
                Assert.False(udpClient.EnableBroadcast);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // ExclusiveAddressUse is Windows-specific
        [Fact]
        public void ExclusiveAddressUse_Roundtrips()
        {
            using (var udpClient = new UdpClient())
            {
                Assert.False(udpClient.ExclusiveAddressUse);
                udpClient.ExclusiveAddressUse = true;
                Assert.True(udpClient.ExclusiveAddressUse);
                udpClient.ExclusiveAddressUse = false;
                Assert.False(udpClient.ExclusiveAddressUse);
            }
        }

        [Fact]
        public void InvalidArguments_Throw()
        {
            using (var udpClient = new UdpClient("localhost", UnusedPort))
            {
                AssertExtensions.Throws<ArgumentNullException>("datagram", () => udpClient.BeginSend(null, 0, null, null));
                Assert.Throws<InvalidOperationException>(() => udpClient.BeginSend(new byte[1], 1, "localhost", 0, null, null));
                Assert.Throws<InvalidOperationException>(() => udpClient.BeginSend(new byte[1], 1, new IPEndPoint(IPAddress.Loopback, 0), null, null));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_NegativeBytes_Throws()
        {
            using (UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork))
            {
                byte[] sendBytes = new byte[1];
                IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () =>
                {
                    udpClient.BeginSend(sendBytes, -1, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);
                });
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_BytesMoreThanArrayLength_Throws()
        {
            using (UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork))
            {
                byte[] sendBytes = new byte[1];
                IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);

                AssertExtensions.Throws<ArgumentOutOfRangeException>("bytes", () =>
                {
                    udpClient.BeginSend(sendBytes, sendBytes.Length + 1, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);
                });
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_AsyncOperationCompletes_Success()
        {
            using (UdpClient udpClient = new UdpClient())
            {
                byte[] sendBytes = new byte[1];
                IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);
                _waitHandle.Reset();
                udpClient.BeginSend(sendBytes, sendBytes.Length, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);

                Assert.True(_waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
            }
        }

        [Fact]
        public void Send_InvalidArguments_Throws()
        {
            using (var udpClient = new DerivedUdpClient())
            {
                AssertExtensions.Throws<ArgumentNullException>("dgram", () => udpClient.Send(null, 0));
                AssertExtensions.Throws<ArgumentNullException>("dgram", () => udpClient.Send(null, 0, "localhost", 0));
                AssertExtensions.Throws<ArgumentNullException>("dgram", () => udpClient.Send(null, 0, new IPEndPoint(IPAddress.Loopback, 0)));
                Assert.Throws<InvalidOperationException>(() => udpClient.Send(new byte[1], 1));
                udpClient.Active = true;
                Assert.Throws<InvalidOperationException>(() => udpClient.Send(new byte[1], 1, new IPEndPoint(IPAddress.Loopback, 0)));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // localhost on Windows resolves to both IPV4/6, but doesn't on all Unix
        public void Send_InvalidArguments_StringInt_Throws()
        {
            using (var udpClient = new UdpClient("localhost", 0))
            {
                Assert.Throws<InvalidOperationException>(() => udpClient.Send(new byte[1], 1, "localhost", 0));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Client_Idempotent()
        {
            using (var c = new UdpClient())
            {
                Socket client = c.Client;
                Assert.NotNull(client);
                Assert.Same(client, c.Client);
            }
        }

        [Fact]
        public void Connect_InvalidArguments_Throws()
        {
            using (var udpClient = new UdpClient())
            {
                AssertExtensions.Throws<ArgumentNullException>("hostname", () => udpClient.Connect((string)null, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => udpClient.Connect("localhost", -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => udpClient.Connect("localhost", 66000));

                AssertExtensions.Throws<ArgumentNullException>("addr", () => udpClient.Connect((IPAddress)null, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => udpClient.Connect(IPAddress.Loopback, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("port", () => udpClient.Connect(IPAddress.Loopback, 66000));

                AssertExtensions.Throws<ArgumentNullException>("endPoint", () => udpClient.Connect(null));
            }
        }
        
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ConnectAsync_StringHost_Success()
        {
            using (var c = new UdpClient())
            {
                await c.Client.ConnectAsync("114.114.114.114", 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ConnectAsync_IPAddressHost_Success()
        {
            using (var c = new UdpClient())
            {
                await c.Client.ConnectAsync(IPAddress.Parse("114.114.114.114"), 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Connect_StringHost_Success()
        {
            using (var c = new UdpClient())
            {
                c.Client.Connect("114.114.114.114", 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Connect_IPAddressHost_Success()
        {
            using (var c = new UdpClient())
            {
                c.Client.Connect(IPAddress.Parse("114.114.114.114"), 53);
            }
        }

        private void AsyncCompleted(IAsyncResult ar)
        {
            UdpClient udpService = (UdpClient)ar.AsyncState;
            udpService.EndSend(ar);
            _waitHandle.Set();
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]  // Udp.AllowNatTraversal only supported on Windows
        [InlineData(true, IPProtectionLevel.Unrestricted)]
        [InlineData(false, IPProtectionLevel.EdgeRestricted)]
        public void AllowNatTraversal_Windows(bool allow, IPProtectionLevel resultLevel)
        {
            using (var c = new UdpClient())
            {
                c.AllowNatTraversal(allow);
                Assert.Equal((int)resultLevel, (int)c.Client.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.IPProtectionLevel));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Udp.AllowNatTraversal throws PNSE on Unix
        [InlineData(true)]
        [InlineData(false)]
        public void AllowNatTraversal_AnyUnix(bool allow)
        {
            using (var c = new UdpClient())
            {
                Assert.Throws<PlatformNotSupportedException>(() => c.AllowNatTraversal(allow));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Send_Receive_Success(bool ipv4)
        {
            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            using (var receiver = new UdpClient(new IPEndPoint(address, 0)))
            using (var sender = new UdpClient(new IPEndPoint(address, 0)))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.Send(new byte[1], 1, new IPEndPoint(address, ((IPEndPoint)receiver.Client.LocalEndPoint).Port));
                }

                IPEndPoint remoteEP = null;
                byte[] data = receiver.Receive(ref remoteEP);
                Assert.NotNull(remoteEP);
                Assert.InRange(data.Length, 1, int.MaxValue);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // "localhost" resolves to IPv4 & IPV6 on Windows, but may resolve to only one of those on Unix 
        [OuterLoop] // TODO: Issue #11345
        public void Send_Receive_Connected_Success()
        {
            using (var receiver = new UdpClient("localhost", 0))
            using (var sender = new UdpClient("localhost", ((IPEndPoint)receiver.Client.LocalEndPoint).Port))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.Send(new byte[1], 1);
                }

                IPEndPoint remoteEP = null;
                byte[] data = receiver.Receive(ref remoteEP);
                Assert.NotNull(remoteEP);
                Assert.InRange(data.Length, 1, int.MaxValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Send_Available_Success(bool ipv4)
        {
            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            using (var receiver = new UdpClient(new IPEndPoint(address, 0)))
            using (var sender = new UdpClient(new IPEndPoint(address, 0)))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.Send(new byte[1], 1, new IPEndPoint(address, ((IPEndPoint)receiver.Client.LocalEndPoint).Port));
                }

                Assert.True(SpinWait.SpinUntil(() => receiver.Available > 0, 30000), "Expected data to be available for receive within time limit");
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BeginEndSend_BeginEndReceive_Success(bool ipv4)
        {
            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            using (var receiver = new UdpClient(new IPEndPoint(address, 0)))
            using (var sender = new UdpClient(new IPEndPoint(address, 0)))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.EndSend(sender.BeginSend(new byte[1], 1, new IPEndPoint(address, ((IPEndPoint)receiver.Client.LocalEndPoint).Port), null, null));
                }

                IPEndPoint remoteEP = null;
                byte[] data = receiver.EndReceive(receiver.BeginReceive(null, null), ref remoteEP);
                Assert.NotNull(remoteEP);
                Assert.InRange(data.Length, 1, int.MaxValue);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // "localhost" resolves to IPv4 & IPV6 on Windows, but may resolve to only one of those on Unix 
        [OuterLoop] // TODO: Issue #11345
        public void BeginEndSend_BeginEndReceive_Connected_Success()
        {
            using (var receiver = new UdpClient("localhost", 0))
            using (var sender = new UdpClient("localhost", ((IPEndPoint)receiver.Client.LocalEndPoint).Port))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    sender.EndSend(sender.BeginSend(new byte[1], 1, null, null));
                }

                IPEndPoint remoteEP = null;
                byte[] data = receiver.EndReceive(receiver.BeginReceive(null, null), ref remoteEP);
                Assert.NotNull(remoteEP);
                Assert.InRange(data.Length, 1, int.MaxValue);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_ReceiveAsync_Success(bool ipv4)
        {
            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;

            using (var receiver = new UdpClient(new IPEndPoint(address, 0)))
            using (var sender = new UdpClient(new IPEndPoint(address, 0)))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    await sender.SendAsync(new byte[1], 1, new IPEndPoint(address, ((IPEndPoint)receiver.Client.LocalEndPoint).Port));
                }

                UdpReceiveResult result = await receiver.ReceiveAsync();
                Assert.NotNull(result);
                Assert.NotNull(result.RemoteEndPoint);
                Assert.NotNull(result.Buffer);
                Assert.InRange(result.Buffer.Length, 1, int.MaxValue);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // "localhost" resolves to IPv4 & IPV6 on Windows, but may resolve to only one of those on Unix 
        [OuterLoop] // TODO: Issue #11345
        public async Task SendAsync_ReceiveAsync_Connected_Success()
        {
            using (var receiver = new UdpClient("localhost", 0))
            using (var sender = new UdpClient("localhost", ((IPEndPoint)receiver.Client.LocalEndPoint).Port))
            {
                for (int i = 0; i < TestSettings.UDPRedundancy; i++)
                {
                    await sender.SendAsync(new byte[1], 1);
                }

                UdpReceiveResult result = await receiver.ReceiveAsync();
                Assert.NotNull(result);
                Assert.NotNull(result.RemoteEndPoint);
                Assert.NotNull(result.Buffer);
                Assert.InRange(result.Buffer.Length, 1, int.MaxValue);
            }
        }

        [Fact]
        public void JoinDropMulticastGroup_InvalidArguments_Throws()
        {
            using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
            {
                AssertExtensions.Throws<ArgumentNullException>("multicastAddr", () => udpClient.JoinMulticastGroup(null));
                AssertExtensions.Throws<ArgumentNullException>("multicastAddr", () => udpClient.JoinMulticastGroup(0, null));
                AssertExtensions.Throws<ArgumentNullException>("multicastAddr", () => udpClient.JoinMulticastGroup(null, 0));
                AssertExtensions.Throws<ArgumentException>("ifindex", () => udpClient.JoinMulticastGroup(-1, IPAddress.Any));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("timeToLive", () => udpClient.JoinMulticastGroup(IPAddress.Loopback, -1));

                AssertExtensions.Throws<ArgumentNullException>("multicastAddr", () => udpClient.DropMulticastGroup(null));
                AssertExtensions.Throws<ArgumentNullException>("multicastAddr", () => udpClient.DropMulticastGroup(null, 0));
                AssertExtensions.Throws<ArgumentException>("multicastAddr", () => udpClient.DropMulticastGroup(IPAddress.IPv6Loopback));
                AssertExtensions.Throws<ArgumentException>("ifindex", () => udpClient.DropMulticastGroup(IPAddress.Loopback, -1));
            }
        }

        [Fact]
        public void UdpReceiveResult_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => new UdpReceiveResult(null, null));
            AssertExtensions.Throws<ArgumentNullException>("remoteEndPoint", () => new UdpReceiveResult(new byte[1], null));
        }

        [Fact]
        public void UdpReceiveResult_Equality()
        {
            byte[] buffer1 = new byte[1] { 42 }, buffer2 = new byte[1] { 42 };
            IPEndPoint ep1 = new IPEndPoint(IPAddress.Loopback, 123), ep2 = new IPEndPoint(IPAddress.Loopback, 123), ep3 = new IPEndPoint(IPAddress.Any, 456);

            Assert.Equal(new UdpReceiveResult().GetHashCode(), new UdpReceiveResult().GetHashCode());
            Assert.Equal(new UdpReceiveResult(buffer1, ep1).GetHashCode(), new UdpReceiveResult(buffer1, ep2).GetHashCode());

            Assert.True(new UdpReceiveResult(buffer1, ep1).Equals(new UdpReceiveResult(buffer1, ep2)));
            Assert.False(new UdpReceiveResult(buffer1, ep1).Equals(new UdpReceiveResult(buffer2, ep1)));
            Assert.False(new UdpReceiveResult(buffer1, ep1).Equals(new UdpReceiveResult(buffer1, ep3)));

            Assert.True(new UdpReceiveResult(buffer1, ep1).Equals((object)new UdpReceiveResult(buffer1, ep2)));
            Assert.False(new UdpReceiveResult(buffer1, ep1).Equals((object)new UdpReceiveResult(buffer2, ep1)));
            Assert.False(new UdpReceiveResult(buffer1, ep1).Equals((object)new UdpReceiveResult(buffer1, ep3)));
            Assert.False(new UdpReceiveResult(buffer1, ep1).Equals(new object()));

            Assert.True(new UdpReceiveResult(buffer1, ep1) == new UdpReceiveResult(buffer1, ep2));
            Assert.True(new UdpReceiveResult(buffer1, ep1) != new UdpReceiveResult(buffer2, ep1));
            Assert.True(new UdpReceiveResult(buffer1, ep1) != new UdpReceiveResult(buffer1, ep3));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void BeginSend_IPv6Socket_IPv4Dns_Success()
        {
            using (var receiver = new UdpClient("127.0.0.1", DiscardPort))
            using (var sender = new UdpClient(AddressFamily.InterNetworkV6))
            {
                sender.Client.DualMode = true;
                if (sender.Client.AddressFamily == AddressFamily.InterNetworkV6 && sender.Client.DualMode)
                {
                    sender.Send(new byte[1], 1, "127.0.0.1", ((IPEndPoint)receiver.Client.LocalEndPoint).Port);
                }
            }
        }

        private sealed class DerivedUdpClient : UdpClient
        {
            public DerivedUdpClient() { }
            public DerivedUdpClient(string hostname, int port) : base(hostname, port) { }
            ~DerivedUdpClient() { Dispose(false); }

            public new bool Active
            {
                get { return base.Active; }
                set { base.Active = value; }
            }
        }
    }
}
