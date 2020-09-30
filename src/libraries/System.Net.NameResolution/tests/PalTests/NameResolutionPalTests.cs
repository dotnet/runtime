// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.NameResolution.PalTests
{
    public class NameResolutionPalTests
    {
        private ITestOutputHelper _output;

        public NameResolutionPalTests(ITestOutputHelper output)
        {
            NameResolutionPal.EnsureSocketsAreInitialized();
            _output = output;
        }

        private void LogUnixInfo()
        {
            _output.WriteLine("--- /etc/hosts ---");
            _output.WriteLine(File.ReadAllText("/etc/hosts"));
            _output.WriteLine("--- /etc/resolv.conf ---");
            _output.WriteLine(File.ReadAllText("/etc/resolv.conf"));
            _output.WriteLine("------");
        }

        [Fact]
        public void HostName_NotNull()
        {
            Assert.NotNull(NameResolutionPal.GetHostName());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetAddrInfo_LocalHost(bool justAddresses)
        {
            SocketError error = NameResolutionPal.TryGetAddrInfo("localhost", justAddresses, out string hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            if (!justAddresses)
            {
                Assert.NotNull(hostName);
            }
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop("Uses external server")]
        public void TryGetAddrInfo_HostName(bool justAddresses)
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, out hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);
            if (error == SocketError.HostNotFound && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
            {
                // On Unix, we are not guaranteed to be able to resove the local host. The ability to do so depends on the
                // machine configurations, which varies by distro and is often inconsistent.
                return;
            }

            Assert.Equal(SocketError.Success, error);
            if (!justAddresses)
            {
                Assert.NotNull(hostName);
            }
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv4()
        {
            SocketError error;
            int nativeErrorCode;
            string name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 127, 0, 0, 1 }), out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv6()
        {
            SocketError error;
            int nativeErrorCode;
            string name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }), out error, out nativeErrorCode);
            if (SocketError.Success != error && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                LogUnixInfo();
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        public void TryGetAddrInfo_LocalHost_TryGetNameInfo()
        {
            SocketError error = NameResolutionPal.TryGetAddrInfo("localhost", justAddresses: false, out string hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostName);
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);

            string name = NameResolutionPal.TryGetNameInfo(addresses[0], out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        [OuterLoop("Uses external server")]
        public void TryGetAddrInfo_HostName_TryGetNameInfo()
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses: false, out hostName, out string[] aliases, out IPAddress[] addresses, out int nativeErrorCode);
            if (error == SocketError.HostNotFound)
            {
                // On Unix, getaddrinfo returns host not found, if all the machine discovery settings on the local network
                // is turned off. Hence dns lookup for it's own hostname fails.
                Assert.Equal(PlatformID.Unix, Environment.OSVersion.Platform);
                return;
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostName);
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);

            string name = NameResolutionPal.TryGetNameInfo(addresses[0], out error, out nativeErrorCode);
            if (error == SocketError.HostNotFound)
            {
                // On Unix, getaddrinfo returns private ipv4 address for hostname. If the OS doesn't have the
                // reverse dns lookup entry for this address, getnameinfo returns host not found.
                Assert.Equal(PlatformID.Unix, Environment.OSVersion.Platform);
                return;
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetAddrInfo_ExternalHost(bool justAddresses)
        {
            string hostName = "microsoft.com";

            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, justAddresses, out hostName, out string[] aliases, out IPAddress[] addresses, out _);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetNameInfo_LocalHost_IPv4_TryGetAddrInfo(bool justAddresses)
        {
            string name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 127, 0, 0, 1 }), out SocketError error, out _);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);

            error = NameResolutionPal.TryGetAddrInfo(name, justAddresses, out string hostName, out string[] aliases, out IPAddress[] addresses, out _);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TryGetNameInfo_LocalHost_IPv6_TryGetAddrInfo(bool justAddresses)
        {
            SocketError error;
            int nativeErrorCode;
            string name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }), out error, out nativeErrorCode);
            if (SocketError.Success != error && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                LogUnixInfo();
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);

            error = NameResolutionPal.TryGetAddrInfo(name, justAddresses, out string hostName, out string[] aliases, out IPAddress[] addresses, out _);
            if (SocketError.Success != error && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                LogUnixInfo();
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(aliases);
            Assert.NotNull(addresses);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Exception_HostNotFound_Success()
        {
            var ex = new  SocketException((int)SocketError.HostNotFound);

            Assert.Equal(-1, ex.Message.IndexOf("Device"));
        }
    }
}
