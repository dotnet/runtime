// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
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

        [Fact]
        public void TryGetAddrInfo_LocalHost()
        {
            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo("localhost", out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
        }

        [Fact]
        [OuterLoop("Uses external server")]
        public void TryGetAddrInfo_HostName()
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
            if (error == SocketError.HostNotFound && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
            {
                // On Unix, we are not guaranteed to be able to resove the local host. The ability to do so depends on the
                // machine configurations, which varies by distro and is often inconsistent.
                return;
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
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
            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo("localhost", out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);

            string name = NameResolutionPal.TryGetNameInfo(hostEntry.AddressList[0], out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        [OuterLoop("Uses external server")]
        public void TryGetAddrInfo_HostName_TryGetNameInfo()
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
            if (error == SocketError.HostNotFound)
            {
                // On Unix, getaddrinfo returns host not found, if all the machine discovery settings on the local network
                // is turned off. Hence dns lookup for it's own hostname fails.
                Assert.Equal(PlatformID.Unix, Environment.OSVersion.Platform);
                return;
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);

            string name = NameResolutionPal.TryGetNameInfo(hostEntry.AddressList[0], out error, out nativeErrorCode);
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

        [Fact]
        public void TryGetAddrInfo_ExternalHost()
        {
            string hostName = "microsoft.com";

            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv4_TryGetAddrInfo()
        {
            SocketError error;
            int nativeErrorCode;
            string name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 127, 0, 0, 1 }), out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);

            IPHostEntry hostEntry;
            error = NameResolutionPal.TryGetAddrInfo(name, out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv6_TryGetAddrInfo()
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

            IPHostEntry hostEntry;
            error = NameResolutionPal.TryGetAddrInfo(name, out hostEntry, out nativeErrorCode);
            if (SocketError.Success != error && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                LogUnixInfo();
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
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
