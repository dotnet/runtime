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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(true, IPProtectionLevel.Unrestricted)]
        [InlineData(false, IPProtectionLevel.EdgeRestricted)]
        public void AllowNatTraversal_Windows(bool allow, IPProtectionLevel resultLevel)
        {
            using (var c = new UdpClient())
            {
                c.AllowNatTraversal(allow);
                Assert.Equal((int)resultLevel, (int)c.Client.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.IPProtectionLevel));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(true)]
        [InlineData(false)]
        public void AllowNatTraversal_AnyUnix(bool allow)
        {
            using (var c = new UdpClient())
            {
                Assert.Throws<PlatformNotSupportedException>(() => c.AllowNatTraversal(allow));
            }
        }
    }
}
