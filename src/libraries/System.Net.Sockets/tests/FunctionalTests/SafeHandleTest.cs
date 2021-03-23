// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SafeHandleTest
    {
        [Fact]
        public static void SafeHandle_NotIsInvalid()
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.False(s.SafeHandle.IsInvalid);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.AnyUnix)]
        public void SafeSocketHandle_CanUseInPInvoke()
        {
            const int AF_INET = 2;
            const int SOCK_STREAM = 1;

            using SafeSocketHandle handle = Socket(AF_INET, SOCK_STREAM, 0);
            Assert.NotNull(handle);
        }

        private static SafeSocketHandle Socket(int af, int type, int protocol) =>
            OperatingSystem.IsWindows() ?
                SocketWindows(af, type, protocol) :
                SocketUnix(af, type, protocol);

        [DllImport("ws2_32.dll", EntryPoint = "socket")]
        private static extern SafeSocketHandle SocketWindows(int af, int type, int protocol);

        [DllImport("libc", EntryPoint = "socket")]
        private static extern SafeSocketHandle SocketUnix(int af, int type, int protocol);
    }
}
