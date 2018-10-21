// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
