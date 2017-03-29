// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.NameResolution.PalTests
{
    public class NameResolutionPalTests
    {
        static NameResolutionPalTests()
        {
            NameResolutionPal.EnsureSocketsAreInitialized();
        }

        [Fact]
        public void HostName_NotNull()
        {
            Assert.NotNull(NameResolutionPal.GetHostName());
        }

        [Fact]
        public void GetHostByName_LocalHost()
        {
            IPHostEntry hostEntry = NameResolutionPal.GetHostByName("localhost");
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
        }

        public static object[][] InvalidHostNames = new object[][] {
            new object[] { ":" },
            new object[] { "..." }
        };

        [Theory, MemberData(nameof(InvalidHostNames))]
        public void GetHostByName_InvalidHostName_Throws(string hostName)
        {
            Assert.ThrowsAny<SocketException>(() => NameResolutionPal.GetHostByName(hostName));
        }

        [Fact]
        public void GetHostByName_HostName()
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry = NameResolutionPal.GetHostByName(hostName);
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
        }

        [Fact]
        public void GetHostByAddr_LocalHost()
        {
            Assert.NotNull(NameResolutionPal.GetHostByAddr(new IPAddress(0x0100007f)));
        }

        [Fact]
        public void GetHostByName_LocalHost_GetHostByAddr()
        {
            IPHostEntry hostEntry1 = NameResolutionPal.GetHostByName("localhost");
            Assert.NotNull(hostEntry1);
            IPHostEntry hostEntry2 = NameResolutionPal.GetHostByAddr(hostEntry1.AddressList[0]);
            Assert.NotNull(hostEntry2);

            IPAddress[] list1 = hostEntry1.AddressList;
            IPAddress[] list2 = hostEntry2.AddressList;

            for (int i = 0; i < list1.Length; i++)
            {
                Assert.NotEqual(-1, Array.IndexOf(list2, list1[i]));
            }
        }

        [Fact]
        public void GetHostByName_HostName_GetHostByAddr()
        {
            IPHostEntry hostEntry1 = NameResolutionPal.GetHostByName(System.Net.Test.Common.Configuration.Http.Http2Host);
            Assert.NotNull(hostEntry1);

            IPAddress[] list1 = hostEntry1.AddressList;
            Assert.InRange(list1.Length, 1, Int32.MaxValue);

            foreach (IPAddress addr1 in list1)
            {
                IPHostEntry hostEntry2 = NameResolutionPal.GetHostByAddr(addr1);
                Assert.NotNull(hostEntry2);

                IPAddress[] list2 = hostEntry2.AddressList;
                Assert.InRange(list2.Length, 1, list1.Length);

                foreach (IPAddress addr2 in list2)
                {
                    Assert.NotEqual(-1, Array.IndexOf(list1, addr2));
                }
            }
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
        public void TryGetAddrInfo_HostName()
        {
            string hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry;
            int nativeErrorCode;
            SocketError error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
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
                Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
                return;
            }

            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);

            string name = NameResolutionPal.TryGetNameInfo(hostEntry.AddressList[0], out error, out nativeErrorCode);
            if (error == SocketError.HostNotFound)
            {
                // On Unix, getaddrinfo returns private ipv4 address for hostname. If the OS doesn't have the
                // reverse dns lookup entry for this address, getnameinfo returns host not found.
                Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
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
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);

            IPHostEntry hostEntry;
            error = NameResolutionPal.TryGetAddrInfo(name, out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
        }
    }
}
