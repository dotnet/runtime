// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class LingerStateTest
    {
        // This is a stand-in for an issue to be filed when this code is merged into corefx.
        private const int DummyOSXLingerStateIssue = 123456;

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
        [ActiveIssue(DummyOSXLingerStateIssue, PlatformID.OSX)]
        public void Socket_LingerState_Boundaries_CorrectBehavior()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.False(sock.LingerState.Enabled, "Linger was turned on by default!");
            Assert.Equal<int>(sock.LingerState.LingerTime, 0);

            TestLingerState_ArgumentException(sock, true, -1);

            TestLingerState_Success(sock, true, 0);
            TestLingerState_Success(sock, true, Int16.MaxValue);
            TestLingerState_Success(sock, true, Int16.MaxValue + 1);
            TestLingerState_Success(sock, true, UInt16.MaxValue);

            TestLingerState_ArgumentException(sock, true, UInt16.MaxValue + 1);
        }
    }
}
