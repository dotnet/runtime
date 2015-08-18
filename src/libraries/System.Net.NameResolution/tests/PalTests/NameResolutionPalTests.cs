// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

using Xunit;

namespace System.Net.NameResolution.PalTests
{
    public class NameResolutionPalTests
    {
        [Fact]
        public void HostName_NotNull()
        {
            Assert.NotNull(NameResolutionPal.GetHostName());
        }

        [Fact]
        public void GetHostByName_LocalHost()
        {
            var hostEntry = NameResolutionPal.GetHostByName("localhost");
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
        }

        [Fact]
        public void GetHostByName_HostName()
        {
            var hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            var hostEntry = NameResolutionPal.GetHostByName(hostName);
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
            var hostEntry1 = NameResolutionPal.GetHostByName("localhost");
            Assert.NotNull(hostEntry1);
            var hostEntry2 = NameResolutionPal.GetHostByAddr(hostEntry1.AddressList[0]);
            Assert.NotNull(hostEntry2);

            var list1 = hostEntry1.AddressList;
            var list2 = hostEntry2.AddressList;

            for (int i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
            }
        }

        [ActiveIssue(2894)]
        [Fact]
        public void GetHostByName_HostName_GetHostByAddr()
        {
            var hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            var hostEntry1 = NameResolutionPal.GetHostByName(hostName);
            Assert.NotNull(hostEntry1);
            var hostEntry2 = NameResolutionPal.GetHostByAddr(hostEntry1.AddressList[0]);
            Assert.NotNull(hostEntry2);

            var list1 = hostEntry1.AddressList;
            var list2 = hostEntry2.AddressList;

            for (int i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
            }
        }

        [Fact]
        public void TryGetAddrInfo_LocalHost()
        {
            IPHostEntry hostEntry;
            int nativeErrorCode;
            var error = NameResolutionPal.TryGetAddrInfo("localhost", out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
            Assert.NotNull(hostEntry.HostName);
            Assert.NotNull(hostEntry.AddressList);
            Assert.NotNull(hostEntry.Aliases);
        }

        [Fact]
        public void TryGetAddrInfo_HostName()
        {
            var hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry;
            int nativeErrorCode;
            var error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
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
            var name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 127, 0, 0, 1 }), out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv6()
        {
            SocketError error;
            int nativeErrorCode;
            var name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }), out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        public void TryGetAddrInfo_LocalHost_TryGetNameInfo()
        {
            IPHostEntry hostEntry;
            int nativeErrorCode;
            var error = NameResolutionPal.TryGetAddrInfo("localhost", out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);

            var name = NameResolutionPal.TryGetNameInfo(hostEntry.AddressList[0], out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [ActiveIssue(2894)]
        [Fact]
        public void TryGetAddrInfo_HostName_TryGetNameInfo()
        {
            var hostName = NameResolutionPal.GetHostName();
            Assert.NotNull(hostName);

            IPHostEntry hostEntry;
            int nativeErrorCode;
            var error = NameResolutionPal.TryGetAddrInfo(hostName, out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);

            var name = NameResolutionPal.TryGetNameInfo(hostEntry.AddressList[0], out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);
        }

        [Fact]
        public void TryGetNameInfo_LocalHost_IPv4_TryGetAddrInfo()
        {
            SocketError error;
            int nativeErrorCode;
            var name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 127, 0, 0, 1 }), out error, out nativeErrorCode);
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
            var name = NameResolutionPal.TryGetNameInfo(new IPAddress(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }), out error, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(name);

            IPHostEntry hostEntry;
            error = NameResolutionPal.TryGetAddrInfo(name, out hostEntry, out nativeErrorCode);
            Assert.Equal(SocketError.Success, error);
            Assert.NotNull(hostEntry);
        }
    }
}
