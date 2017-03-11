// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class HandleTest
    {
        [Fact]
        public static void ValidHandle_NotNegativeOne()
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.NotEqual((IntPtr)(-1), s.Handle);
            }
        }

        [Fact]
        public static void ValidHandle_NotZero()
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                Assert.NotEqual(IntPtr.Zero, s.Handle);
            }
        }
    }
}
