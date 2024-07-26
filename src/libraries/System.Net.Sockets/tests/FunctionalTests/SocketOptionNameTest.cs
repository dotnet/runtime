// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class SocketOptionNameTest
    {
        private static bool SocketsReuseUnicastPortSupport => Capability.SocketsReuseUnicastPortSupport().HasValue;

        [ConditionalFact(nameof(SocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketGetOption()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (Capability.SocketsReuseUnicastPortSupport().Value)
                {
                    Assert.Equal(0, (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort));
                }
                else
                {
                    Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort));
                }
            }
        }

        [ConditionalFact(nameof(SocketsReuseUnicastPortSupport))]
        public void ReuseUnicastPort_CreateSocketSetOption()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (Capability.SocketsReuseUnicastPortSupport().Value)
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 0);
                    int optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
                    Assert.Equal(0, optionValue);
                }
                else
                {
                    Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1));
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/104547", typeof(PlatformDetection), nameof(PlatformDetection.IsQemuLinux))]
        public void MulticastOption_CreateSocketSetGetOption_GroupAndInterfaceIndex_SetSucceeds_GetThrows()
        {
            int interfaceIndex = 0;
            IPAddress groupIp = IPAddress.Parse("239.1.2.3");

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(groupIp, interfaceIndex));

                Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Skip on Nano: https://github.com/dotnet/runtime/issues/26286
        [ActiveIssue("https://github.com/dotnet/runtime/issues/104547", typeof(PlatformDetection), nameof(PlatformDetection.IsQemuLinux))]
        public async Task MulticastInterface_Set_AnyInterface_Succeeds()
        {
            // On all platforms, index 0 means "any interface"
            await MulticastInterface_Set_Helper(0);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Skip on Nano: https://github.com/dotnet/runtime/issues/26286
        [PlatformSpecific(TestPlatforms.Windows)] // see comment below
        public async Task MulticastInterface_Set_Loopback_Succeeds()
        {
            // On Windows, we can apparently assume interface 1 is "loopback." On other platforms, this is not a
            // valid assumption. We could maybe use NetworkInterface.LoopbackInterfaceIndex to get the index, but
            // this would introduce a dependency on System.Net.NetworkInformation, which depends on System.Net.Sockets,
            // which is what we're testing here....  So for now, we'll just assume "loopback == 1" and run this on
            // Windows only.
            await MulticastInterface_Set_Helper(1);
        }

        private async Task MulticastInterface_Set_Helper(int interfaceIndex)
        {
            IPAddress multicastAddress = IPAddress.Parse("239.1.2.3");
            string message = "hello";
            int port;

            using (Socket receiveSocket = CreateBoundUdpSocket(out port),
                          sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                receiveSocket.ReceiveTimeout = 1000;
                receiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastAddress, interfaceIndex));

                sendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(interfaceIndex));

                var receiveBuffer = new byte[1024];
                var receiveTask = receiveSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), SocketFlags.None);

                sendSocket.SendTo(Encoding.UTF8.GetBytes(message), new IPEndPoint(multicastAddress, port));

                int bytesReceived = await receiveTask.WaitAsync(TimeSpan.FromSeconds(30));
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesReceived);

                Assert.Equal(receivedMessage, message);
            }
        }

        [Fact]
        public void MulticastInterface_Set_InvalidIndex_Throws()
        {
            int interfaceIndex = 31415;
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Throws<SocketException>(() =>
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(interfaceIndex)));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Skip on Nano: https://github.com/dotnet/runtime/issues/26286
        [SkipOnPlatform(TestPlatforms.OSX | TestPlatforms.FreeBSD, "Expected behavior is different on OSX or FreeBSD")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/104547", typeof(PlatformDetection), nameof(PlatformDetection.IsQemuLinux))]
        public async Task MulticastInterface_Set_IPv6_AnyInterface_Succeeds()
        {
            // On all platforms, index 0 means "any interface"
            await MulticastInterface_Set_IPv6_Helper(0);
        }

        [Fact]
        public void MulticastTTL_Set_IPv4_Succeeds()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                // This should not throw. We currently do not have good mechanism how to verify that the TTL/Hops is actually set.

                int ttl = (int)socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive);
                ttl += 1;
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
                Assert.Equal(ttl, (int)socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Skip on Nano: https://github.com/dotnet/runtime/issues/26286
        public void MulticastTTL_Set_IPv6_Succeeds()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                // This should not throw. We currently do not have good mechanism how to verify that the TTL/Hops is actually set.

                int ttl = (int)socket.GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive);
                ttl += 1;
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, ttl);
                Assert.Equal(ttl, (int)socket.GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive));
            }
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void Ttl_Set_Succeeds(AddressFamily af)
        {
            using (Socket socket = new Socket(af, SocketType.Dgram, ProtocolType.Udp))
            {
                short newTtl = socket.Ttl;
                // Change default ttl.
                newTtl += (short)((newTtl < 255) ? 1 : -1);
                socket.Ttl = newTtl;
                Assert.Equal(newTtl, socket.Ttl);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Skip on Nano: https://github.com/dotnet/runtime/issues/26286
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task MulticastInterface_Set_IPv6_Loopback_Succeeds()
        {
            // On Windows, we can apparently assume interface 1 is "loopback." On other platforms, this is not a
            // valid assumption. We could maybe use NetworkInterface.LoopbackInterfaceIndex to get the index, but
            // this would introduce a dependency on System.Net.NetworkInformation, which depends on System.Net.Sockets,
            // which is what we're testing here....  So for now, we'll just assume "loopback == 1" and run this on
            // Windows only.
            await MulticastInterface_Set_IPv6_Helper(1);
        }

        private async Task MulticastInterface_Set_IPv6_Helper(int interfaceIndex)
        {
            IPAddress multicastAddress = IPAddress.Parse("ff11::1:1");
            string message = "hello";
            int port;

            using (Socket receiveSocket = CreateBoundUdpIPv6Socket(out port),
                          sendSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                receiveSocket.ReceiveTimeout = 1000;
                receiveSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(multicastAddress, interfaceIndex));

                sendSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, interfaceIndex);

                var receiveBuffer = new byte[1024];
                var receiveTask = receiveSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), SocketFlags.None);

                sendSocket.SendTo(Encoding.UTF8.GetBytes(message), new IPEndPoint(multicastAddress, port));

                int bytesReceived = await receiveTask.WaitAsync(TimeSpan.FromSeconds(30));
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, bytesReceived);

                Assert.Equal(receivedMessage, message);
            }
        }

        [Fact]
        public void MulticastInterface_Set_IPv6_InvalidIndex_Throws()
        {
            int interfaceIndex = 31415;
            using (Socket s = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Throws<SocketException>(() =>
                                               s.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, interfaceIndex));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.FreeBSD, "on FreeBSD Connect may or may not fail immediately based on timing.")]
        public void FailedConnect_GetSocketOption_SocketOptionNameError(bool simpleGet)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = false })
            {
                // Fail a Connect
                using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    server.Bind(new IPEndPoint(IPAddress.Loopback, 0)); // bind but don't listen
                    Assert.ThrowsAny<Exception>(() => client.Connect(server.LocalEndPoint));
                }

                // Verify via Poll that there's an error
                Assert.True(client.Poll(10_000_000, SelectMode.SelectError));

                // Get the last error and validate it's what's expected
                int errorCode;
                if (simpleGet)
                {
                    errorCode = (int)client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                }
                else
                {
                    byte[] optionValue = new byte[sizeof(int)];
                    client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error, optionValue);
                    errorCode = BitConverter.ToInt32(optionValue, 0);
                }
                Assert.Equal((int)SocketError.ConnectionRefused, errorCode);

                // Then get it again
                if (OperatingSystem.IsWindows())
                {
                    // The Windows implementation doesn't clear the error code after retrieved.
                    // https://github.com/dotnet/runtime/issues/17260
                    Assert.Equal(errorCode, (int)client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error));
                }
                else
                {
                    // The Unix implementation matches the getsockopt and MSDN docs and clears the error code as part of retrieval.
                    Assert.Equal((int)SocketError.Success, (int)client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error));
                }
            }
        }

        // Create an Udp Socket and binds it to an available port
        private static Socket CreateBoundUdpSocket(out int localPort)
        {
            Socket receiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // sending a message will bind the socket to an available port
            string sendMessage = "dummy message";
            int port = 54320;
            IPAddress multicastAddress = IPAddress.Parse("239.1.1.1");
            receiveSocket.SendTo(Encoding.UTF8.GetBytes(sendMessage), new IPEndPoint(multicastAddress, port));

            localPort = (receiveSocket.LocalEndPoint as IPEndPoint).Port;
            return receiveSocket;
        }

        // Create an Udp Socket and binds it to an available port
        private static Socket CreateBoundUdpIPv6Socket(out int localPort)
        {
            Socket receiveSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

            // sending a message will bind the socket to an available port
            string sendMessage = "dummy message";
            int port = 54320;
            IPAddress multicastAddress = IPAddress.Parse("ff11::1:1");
            receiveSocket.SendTo(Encoding.UTF8.GetBytes(sendMessage), new IPEndPoint(multicastAddress, port));

            localPort = (receiveSocket.LocalEndPoint as IPEndPoint).Port;
            return receiveSocket;
        }

        [Theory]
        [InlineData(null, null, null, true)]
        [InlineData(null, null, false, true)]
        [InlineData(null, false, false, true)]
        [InlineData(null, true, false, true)]
        [InlineData(null, true, true, false)]
        [InlineData(true, null, null, true)]
        [InlineData(true, null, false, true)]
        [InlineData(true, null, true, true)]
        [InlineData(true, false, null, true)]
        [InlineData(true, false, false, true)]
        [InlineData(true, false, true, true)]
        public void ReuseAddress(bool? exclusiveAddressUse, bool? firstSocketReuseAddress, bool? secondSocketReuseAddress, bool expectFailure)
        {
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                if (exclusiveAddressUse.HasValue)
                {
                    a.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, exclusiveAddressUse.Value);
                }
                if (firstSocketReuseAddress.HasValue)
                {
                    a.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, firstSocketReuseAddress.Value);
                }

                a.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                using (Socket b = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    if (secondSocketReuseAddress.HasValue)
                    {
                        b.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, secondSocketReuseAddress.Value);
                    }

                    if (expectFailure)
                    {
                        Assert.ThrowsAny<SocketException>(() => b.Bind(a.LocalEndPoint));
                    }
                    else
                    {
                        b.Bind(a.LocalEndPoint);
                    }
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]  // ExclusiveAddressUse option is a Windows-specific option (when set to "true," tells Windows not to allow reuse of same local address)
        [InlineData(false, null, null, true)]
        [InlineData(false, null, false, true)]
        [InlineData(false, false, null, true)]
        [InlineData(false, false, false, true)]
        [InlineData(false, true, null, true)]
        [InlineData(false, true, false, true)]
        [InlineData(false, true, true, false)]
        public void ReuseAddress_Windows(bool? exclusiveAddressUse, bool? firstSocketReuseAddress, bool? secondSocketReuseAddress, bool expectFailure)
        {
            ReuseAddress(exclusiveAddressUse, firstSocketReuseAddress, secondSocketReuseAddress, expectFailure);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Windows defaults are different
        public void ExclusiveAddress_Default_Unix()
        {
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.Equal(1, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse));
                Assert.True(a.ExclusiveAddressUse);
                Assert.Equal(0, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress));
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // Unix does not have separate options for ExclusiveAddressUse and ReuseAddress.
        public void SettingExclusiveAddress_SetsReuseAddress(int value)
        {
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                a.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, value);

                Assert.Equal(value, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse));
                Assert.Equal(value == 1 ? 0 : 1, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress));
            }

            // SettingReuseAddress_SetsExclusiveAddress
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                a.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value);

                Assert.Equal(value, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress));
                Assert.Equal(value == 1 ? 0 : 1, (int)a.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse));
            }
        }

        [Fact]
        public void ExclusiveAddressUseTcp()
        {
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // ExclusiveAddressUse defaults to true on Unix, on Windows it defaults to false.
                a.ExclusiveAddressUse = true;

                a.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                a.Listen();
                int port = (a.LocalEndPoint as IPEndPoint).Port;

                using (Socket b = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    SocketException ex = Assert.ThrowsAny<SocketException>(() => b.Bind(new IPEndPoint(IPAddress.Loopback, port)));
                    Assert.Equal(SocketError.AddressAlreadyInUse, ex.SocketErrorCode);
                }
            }
        }

        [ConditionalFact]
        public async Task TcpFastOpen_Roundrip_Succeeds()
        {
            if (PlatformDetection.IsWindows && !PlatformDetection.IsWindows10OrLater)
            {
                // Old Windows versions do not support fast open and SetSocketOption fails with error.
                throw new SkipTestException("TCP fast open is not supported");
            }

            using (Socket l = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                l.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                l.Listen();

                int oldValue = (int)l.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen);
                int newValue = oldValue == 0 ? 1 : 0;
                l.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen, newValue);
                oldValue = (int)l.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen);
                Assert.Equal(newValue, oldValue);

                using (Socket c = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    oldValue = (int)c.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen);
                    newValue = oldValue == 0 ? 1 : 0;
                    c.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen, newValue);
                    oldValue = (int)c.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.FastOpen);
                    Assert.Equal(newValue, oldValue);

                    await c.ConnectAsync(l.LocalEndPoint);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.OSX)]
        public unsafe void ReuseAddressUdp()
        {
            // Verify that .NET Core Sockets can bind to the UDP address from applications
            // that allow binding the same address.
            int SOL_SOCKET = -1;
            int option = -1;
            if (OperatingSystem.IsLinux())
            {
                // Linux: use SO_REUSEADDR to allow binding the same address.
                SOL_SOCKET = 1;
                const int SO_REUSEADDR = 2;
                option = SO_REUSEADDR;
            }
            else if (OperatingSystem.IsMacOS())
            {
                // BSD: use SO_REUSEPORT to allow binding the same address.
                SOL_SOCKET = 0xffff;
                const int SO_REUSEPORT = 0x200;
                option = SO_REUSEPORT;
            }
            using (Socket s1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                int value = 1;
                s1.SetRawSocketOption(SOL_SOCKET, option, new Span<byte>(&value, sizeof(int)));
                s1.Bind(new IPEndPoint(IPAddress.Any, 0));
                using (Socket s2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    s2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    s2.Bind(s1.LocalEndPoint);
                }
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]  // SetIPProtectionLevel not supported on Unix
        [InlineData(IPProtectionLevel.EdgeRestricted, AddressFamily.InterNetwork, SocketOptionLevel.IP)]
        [InlineData(IPProtectionLevel.Restricted, AddressFamily.InterNetwork, SocketOptionLevel.IP)]
        [InlineData(IPProtectionLevel.Unrestricted, AddressFamily.InterNetwork, SocketOptionLevel.IP)]
        [InlineData(IPProtectionLevel.EdgeRestricted, AddressFamily.InterNetworkV6, SocketOptionLevel.IPv6)]
        [InlineData(IPProtectionLevel.Restricted, AddressFamily.InterNetworkV6, SocketOptionLevel.IPv6)]
        [InlineData(IPProtectionLevel.Unrestricted, AddressFamily.InterNetworkV6, SocketOptionLevel.IPv6)]
        public void SetIPProtectionLevel_Windows(IPProtectionLevel level, AddressFamily family, SocketOptionLevel optionLevel)
        {
            using (var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetIPProtectionLevel(level);

                int result = (int)socket.GetSocketOption(optionLevel, SocketOptionName.IPProtectionLevel);
                Assert.Equal(result, (int)level);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // SetIPProtectionLevel not supported on Unix
        [InlineData(IPProtectionLevel.EdgeRestricted, AddressFamily.InterNetwork)]
        [InlineData(IPProtectionLevel.Restricted, AddressFamily.InterNetwork)]
        [InlineData(IPProtectionLevel.Unrestricted, AddressFamily.InterNetwork)]
        [InlineData(IPProtectionLevel.EdgeRestricted, AddressFamily.InterNetworkV6)]
        [InlineData(IPProtectionLevel.Restricted, AddressFamily.InterNetworkV6)]
        [InlineData(IPProtectionLevel.Unrestricted, AddressFamily.InterNetworkV6)]
        public void SetIPProtectionLevel_Unix(IPProtectionLevel level, AddressFamily family)
        {
            using (var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => socket.SetIPProtectionLevel(level));
            }
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void SetIPProtectionLevel_ArgumentException(AddressFamily family)
        {
            using (var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp))
            {
                AssertExtensions.Throws<ArgumentException>("level", () => socket.SetIPProtectionLevel(IPProtectionLevel.Unspecified));
            }
        }

        [ConditionalTheory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50568", TestPlatforms.Android)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void GetSetRawSocketOption_Roundtrips(AddressFamily family)
        {
            int SOL_SOCKET;
            int SO_RCVBUF;

            if (OperatingSystem.IsWindows() ||
                OperatingSystem.IsFreeBSD() ||
                OperatingSystem.IsMacOS())
            {
                SOL_SOCKET = 0xffff;
                SO_RCVBUF = 0x1002;
            }
            else if (OperatingSystem.IsLinux())
            {
                SOL_SOCKET = 1;
                SO_RCVBUF = 8;
            }
            else
            {
                throw new SkipTestException("Unknown platform");
            }

            using (var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp))
            {
                const int SetSize = 8192;
                int ExpectedGetSize =
                    OperatingSystem.IsLinux() ? SetSize * 2 : // Linux kernel documented to double the size
                    SetSize;

                socket.SetRawSocketOption(SOL_SOCKET, SO_RCVBUF, BitConverter.GetBytes(SetSize));

                var buffer = new byte[sizeof(int)];
                Assert.Equal(4, socket.GetRawSocketOption(SOL_SOCKET, SO_RCVBUF, buffer));
                Assert.Equal(ExpectedGetSize, BitConverter.ToInt32(buffer));

                Assert.Equal(ExpectedGetSize, socket.ReceiveBufferSize);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52124", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public void Get_AcceptConnection_Succeeds()
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal(0, s.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.AcceptConnection));

                s.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                s.Listen();

                Assert.NotEqual(0, s.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.AcceptConnection));
            }
        }

        [Fact]
        public void GetUnsupportedSocketOption_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                SocketException se = Assert.Throws<SocketException>(() => socket1.GetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1)));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void GetUnsupportedSocketOptionBytesArg_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                var optionValue = new byte[4];
                SocketException se = Assert.Throws<SocketException>(() => socket1.GetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1), optionValue));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void GetUnsupportedSocketOptionLengthArg_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                SocketException se = Assert.Throws<SocketException>(() => socket1.GetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1), optionLength: 4));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void SetUnsupportedSocketOptionIntArg_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                SocketException se = Assert.Throws<SocketException>(() => socket1.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1), optionValue: 1));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void SetUnsupportedSocketOptionBytesArg_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                var optionValue = new byte[4];
                SocketException se = Assert.Throws<SocketException>(() => socket1.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1), optionValue));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void SetUnsupportedSocketOptionBoolArg_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                bool optionValue = true;
                SocketException se = Assert.Throws<SocketException>(() => socket1.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)(-1), optionValue));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void GetUnsupportedRawSocketOption_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                var optionValue = new byte[4];
                SocketException se = Assert.Throws<SocketException>(() => socket1.GetRawSocketOption(SOL_SOCKET, -1, optionValue));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        [Fact]
        public void SetUnsupportedRawSocketOption_DoesNotDisconnectSocket()
        {
            (Socket socket1, Socket socket2) = SocketTestExtensions.CreateConnectedSocketPair();
            using (socket1)
            using (socket2)
            {
                var optionValue = new byte[4];
                SocketException se = Assert.Throws<SocketException>(() => socket1.SetRawSocketOption(SOL_SOCKET, -1, optionValue));
                Assert.True(se.SocketErrorCode == SocketError.ProtocolOption ||
                            se.SocketErrorCode == SocketError.OperationNotSupported, $"SocketError: {se.SocketErrorCode}");

                Assert.True(socket1.Connected, "Connected");
            }
        }

        private static int SOL_SOCKET = OperatingSystem.IsLinux() ? 1 : (int)SocketOptionLevel.Socket;
    }

    [Collection(nameof(DisableParallelization))]
    // Set of tests to not run  together with any other tests.
    public class NoParallelTests
    {
        [Fact]
        public void BindDuringTcpWait_Succeeds()
        {
            int port = 0;
            using (Socket a = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                a.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                port = (a.LocalEndPoint as IPEndPoint).Port;
                a.Listen();

                // Connect a client
                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.Connect(new IPEndPoint(IPAddress.Loopback, port));
                    // accept socket and close it with zero linger time.
                    a.Accept().Close(0);
                }
            }

            // Bind a socket to the same address we just used.
            // To avoid conflict with other tests, this is part of the DisableParallelization test collection.
            using (Socket b = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                b.Bind(new IPEndPoint(IPAddress.Loopback, port));
            }
        }
    }
}
