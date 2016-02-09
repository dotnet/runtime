// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

using System.Threading.Tasks;
using System.Net.Test.Common;
using System.Text;

namespace System.Net.Sockets.Tests
{
    public class TcpClientTest
    {
        private readonly ITestOutputHelper _log;

        public TcpClientTest(ITestOutputHelper output)
        {
            _log = output;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task Connect_DnsEndPoint_Success(int mode)
        {
            using (TcpClient client = new TcpClient())
            {
                Assert.False(client.Connected);

                string host = HttpTestServers.Host;
                const int port = 80;

                if (mode == 0)
                {
                    await client.ConnectAsync(host, port);
                }
                else
                {
                    IPAddress[] addresses = await Dns.GetHostAddressesAsync(host);
                    await (mode == 1 ?
                        client.ConnectAsync(addresses[0], port) :
                        client.ConnectAsync(addresses, port));
                }

                Assert.True(client.Connected);

                using (NetworkStream s = client.GetStream())
                {
                    byte[] getRequest = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\n\r\n");
                    await s.WriteAsync(getRequest, 0, getRequest.Length);
                    Assert.NotEqual(-1, s.ReadByte()); // just verify we successfully get any data back
                }
            }
        }

        [Fact]
        public void ConnectedAvailable_InitialValues_Default()
        {
            using (TcpClient client = new TcpClient())
            {
                Assert.False(client.Connected);
                Assert.Equal(0, client.Available);
            }
        }

        [Fact]
        public void Roundtrip_ExclusiveAddressUse_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ExclusiveAddressUse = true;
                Assert.True(client.ExclusiveAddressUse);
                client.ExclusiveAddressUse = false;
                Assert.False(client.ExclusiveAddressUse);
            }
        }

        [Fact]
        public void Roundtrip_LingerOption_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.LingerState = new LingerOption(true, 42);
                Assert.True(client.LingerState.Enabled);
                Assert.Equal(42, client.LingerState.LingerTime);
                client.LingerState = new LingerOption(false, 0);
                Assert.False(client.LingerState.Enabled);
                Assert.Equal(0, client.LingerState.LingerTime);
            }
        }

        [Fact]
        public void Roundtrip_NoDelay_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.NoDelay = true;
                Assert.True(client.NoDelay);
                client.NoDelay = false;
                Assert.False(client.NoDelay);
            }
        }

        [Fact]
        public void Roundtrip_ReceiveBufferSize_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ReceiveBufferSize = 4096;
                Assert.Equal(4096, client.ReceiveBufferSize);
                client.ReceiveBufferSize = 8192;
                Assert.Equal(8192, client.ReceiveBufferSize);
            }
        }

        [Fact]
        public void Roundtrip_SendBufferSize_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.SendBufferSize = 4096;
                Assert.Equal(4096, client.SendBufferSize);
                client.SendBufferSize = 8192;
                Assert.Equal(8192, client.SendBufferSize);
            }
        }

        [Fact]
        public void Roundtrip_ReceiveTimeout_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.ReceiveTimeout = 1;
                Assert.Equal(1, client.ReceiveTimeout);
                client.ReceiveTimeout = 0;
                Assert.Equal(0, client.ReceiveTimeout);
            }
        }

        [Fact]
        public void Roundtrip_SendTimeout_GetEqualsSet()
        {
            using (TcpClient client = new TcpClient())
            {
                client.SendTimeout = 1;
                Assert.Equal(1, client.SendTimeout);
                client.SendTimeout = 0;
                Assert.Equal(0, client.SendTimeout);
            }
        }

        [Fact]
        public async Task Properties_PersistAfterConnect()
        {
            using (TcpClient client = new TcpClient())
            {
                // Set a few properties
                client.LingerState = new LingerOption(true, 1);
                client.ReceiveTimeout = 42;
                client.SendTimeout = 84;

                await client.ConnectAsync(HttpTestServers.Host, 80);

                // Verify their values remain as were set before connecting
                Assert.True(client.LingerState.Enabled);
                Assert.Equal(1, client.LingerState.LingerTime);
                Assert.Equal(42, client.ReceiveTimeout);
                Assert.Equal(84, client.SendTimeout);

                // Note: not all properties can be tested for this on all OSes, as some
                // properties are modified by the OS, e.g. Linux will double whatever
                // buffer size you set and return that double value.  OSes may also enforce
                // minimums and maximums, silently capping to those amounts.
            }
        }
    }
}
