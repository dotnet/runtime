// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;

namespace System.Net.Sockets.Tests
{
    public class OSSupportTest
    {
        [Fact]
        public void SupportsIPv4_MatchesOSSupportsIPv4()
        {
#pragma warning disable 0618 // Supports* are obsoleted
            Assert.Equal(Socket.SupportsIPv4, Socket.OSSupportsIPv4);
#pragma warning restore
        }

        [Fact]
        public void SupportsIPv6_MatchesOSSupportsIPv6()
        {
#pragma warning disable 0618 // Supports* are obsoleted
            Assert.Equal(Socket.SupportsIPv6, Socket.OSSupportsIPv6);
#pragma warning restore
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void UseOnlyOverlappedIO_AlwaysFalse()
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal(AddressFamily.InterNetwork, s.AddressFamily);
                Assert.Equal(SocketType.Stream, s.SocketType);
                Assert.Equal(ProtocolType.Tcp, s.ProtocolType);

                Assert.False(s.UseOnlyOverlappedIO);
                s.UseOnlyOverlappedIO = true;
                Assert.False(s.UseOnlyOverlappedIO);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Windows IOCTL
        [Fact]
        public void IOControl_FIONREAD_Success()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] outValue = new byte[sizeof(int)];

                const int FIONREAD = 0x4004667F;
                Assert.Equal(4, client.IOControl(FIONREAD, null, outValue));
                Assert.Equal(client.Available, BitConverter.ToInt32(outValue, 0));

                Assert.Equal(4, client.IOControl(IOControlCode.DataToRead, null, outValue));
                Assert.Equal(client.Available, BitConverter.ToInt32(outValue, 0));
            }
        }
    }
}
