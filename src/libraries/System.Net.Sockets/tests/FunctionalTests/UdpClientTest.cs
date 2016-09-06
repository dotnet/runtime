// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class UdpClientTest
    {
        // Port 8 is unassigned as per https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.txt
        private const int UnusedPort = 8;

        private ManualResetEvent _waitHandle = new ManualResetEvent(false);

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_NegativeBytes_Throws()
        {
            UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
            byte[] sendBytes = new byte[1];
            IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);

            Assert.Throws<ArgumentOutOfRangeException>("bytes", () =>
            {
                udpClient.BeginSend(sendBytes, -1, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_BytesMoreThanArrayLength_Throws()
        {
            UdpClient udpClient = new UdpClient(AddressFamily.InterNetwork);
            byte[] sendBytes = new byte[1];
            IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);

            Assert.Throws<ArgumentOutOfRangeException>("bytes", () =>
            {
                udpClient.BeginSend(sendBytes, sendBytes.Length + 1, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void BeginSend_AsyncOperationCompletes_Success()
        {
            UdpClient udpClient = new UdpClient();
            byte[] sendBytes = new byte[1];
            IPEndPoint remoteServer = new IPEndPoint(IPAddress.Loopback, UnusedPort);
            _waitHandle.Reset();
            udpClient.BeginSend(sendBytes, sendBytes.Length, remoteServer, new AsyncCallback(AsyncCompleted), udpClient);

            Assert.True(_waitHandle.WaitOne(TestSettings.PassingTestTimeout), "Timed out while waiting for connection");
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Client_Idempotent()
        {
            using (var c = new UdpClient())
            {
                Socket client = c.Client;
                Assert.NotNull(client);
                Assert.Same(client, c.Client);
            }
        }
        
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [ActiveIssue(9189, PlatformID.AnyUnix)]
        public async Task ConnectAsync_StringHost_Success()
        {
            using (var c = new UdpClient())
            {
                await c.Client.ConnectAsync("114.114.114.114", 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ConnectAsync_IPAddressHost_Success()
        {
            using (var c = new UdpClient())
            {
                await c.Client.ConnectAsync(IPAddress.Parse("114.114.114.114"), 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [ActiveIssue(9189, PlatformID.AnyUnix)]
        public void Connect_StringHost_Success()
        {
            using (var c = new UdpClient())
            {
                c.Client.Connect("114.114.114.114", 53);
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void Connect_IPAddressHost_Success()
        {
            using (var c = new UdpClient())
            {
                c.Client.Connect(IPAddress.Parse("114.114.114.114"), 53);
            }
        }

        private void AsyncCompleted(IAsyncResult ar)
        {
            UdpClient udpService = (UdpClient)ar.AsyncState;
            udpService.EndSend(ar);
            _waitHandle.Set();
        }
    }
}
