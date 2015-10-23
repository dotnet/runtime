// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class LingerStateTest
    {
        private void TestLingerState_Success(Socket sock, bool enabled, int lingerTime)
        {
            sock.LingerState = new LingerOption(enabled, lingerTime);

            Assert.Equal<bool>(enabled, sock.LingerState.Enabled);
            Assert.Equal<int>(lingerTime, sock.LingerState.LingerTime);
        }

        private void TestLingerState_ArgumentException(Socket sock, bool enabled, int lingerTime)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                sock.LingerState = new LingerOption(enabled, lingerTime);
            });
        }

        [Fact]
        public void Socket_LingerState_Common_Boundaries_CorrectBehavior()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.False(sock.LingerState.Enabled, "Linger was turned on by default!");
            Assert.Equal<int>(sock.LingerState.LingerTime, 0);

            TestLingerState_ArgumentException(sock, true, -1);

            TestLingerState_Success(sock, true, 0);
            TestLingerState_Success(sock, true, 120);

            TestLingerState_ArgumentException(sock, true, UInt16.MaxValue + 1);
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)]
        public void Socket_LingerState_Upper_Boundaries_CorrectBehavior()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            TestLingerState_Success(sock, true, Int16.MaxValue);
            TestLingerState_Success(sock, true, Int16.MaxValue + 1);
            TestLingerState_Success(sock, true, UInt16.MaxValue);
        }

        [Fact]
        [PlatformSpecific(PlatformID.OSX)]
        public void Socket_LingerState_Upper_Boundaries_CorrectBehavior_OSX()
        {
            // The upper bound for linger time is drastically different on OS X.
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.Throws<SocketException>(() =>
            {
                sock.LingerState = new LingerOption(true, Int16.MaxValue);
            });

            Assert.Throws<SocketException>(() =>
            {
                sock.LingerState = new LingerOption(true, Int16.MaxValue + 1);
            });

            Assert.Throws<SocketException>(() =>
            {
                sock.LingerState = new LingerOption(true, UInt16.MaxValue);
            });
        }
    }
}
