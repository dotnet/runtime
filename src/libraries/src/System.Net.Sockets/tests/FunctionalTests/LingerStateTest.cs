// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
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
            AssertExtensions.Throws<ArgumentException>("optionValue.LingerTime", () =>
            {
                sock.LingerState = new LingerOption(enabled, lingerTime);
            });
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(~TestPlatforms.OSX)]  // The upper bound for linger time is drastically different on OS X.
        public void Socket_LingerState_Upper_Boundaries_CorrectBehavior()
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            TestLingerState_Success(sock, true, Int16.MaxValue);
            TestLingerState_Success(sock, true, Int16.MaxValue + 1);
            TestLingerState_Success(sock, true, UInt16.MaxValue);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]  // The upper bound for linger time is drastically different on OS X.
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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        public void SetLingerAfterServerClosed(bool linger, int timeout)
        {
            using (var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                {
                    client.Connect(IPAddress.Loopback, port);

                    server.Dispose();
                    Thread.Sleep(10); // give the server socket time to close

                    client.LingerState = new LingerOption(linger, timeout);
                }
            }
        }
    }
}
