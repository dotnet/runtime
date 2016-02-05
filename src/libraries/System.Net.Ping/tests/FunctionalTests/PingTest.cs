// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class PingTest
    {
        private class FinalizingPing : Ping
        {
            public static volatile bool WasFinalized;

            public static void CreateAndRelease()
            {
                new FinalizingPing();
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposing)
                {
                    WasFinalized = true;
                }

                base.Dispose(disposing);
            }
        }


        [Fact]
        public async Task SendPingAsync_InvalidArgs()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();
            Ping p = new Ping();

            // Null address
            Assert.Throws<ArgumentNullException>("address", () => { p.SendPingAsync((IPAddress)null); });
            Assert.Throws<ArgumentNullException>("hostNameOrAddress", () => { p.SendPingAsync((string)null); });

            // Invalid address
            Assert.Throws<ArgumentException>("address", () => { p.SendPingAsync(IPAddress.Any); });
            Assert.Throws<ArgumentException>("address", () => { p.SendPingAsync(IPAddress.IPv6Any); });

            // Negative timeout
            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(localIpAddress, -1); });
            Assert.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(TestSettings.LocalHost, -1); });

            // Null byte[]
            Assert.Throws<ArgumentNullException>("buffer", () => { p.SendPingAsync(localIpAddress, 0, null); });
            Assert.Throws<ArgumentNullException>("buffer", () => { p.SendPingAsync(TestSettings.LocalHost, 0, null); });

            // Too large byte[]
            Assert.Throws<ArgumentException>("buffer", () => { p.SendPingAsync(localIpAddress, 1, new byte[65501]); });
            Assert.Throws<ArgumentException>("buffer", () => { p.SendPingAsync(TestSettings.LocalHost, 1, new byte[65501]); });
        }

        [Fact]
        public async Task SendPingAsyncWithIPAddress()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [Fact]
        public async Task SendPingAsyncWithIPAddress_AddressAsString()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress.ToString()),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeout()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBuffer()
        {
            byte[] buffer = TestSettings.PayloadAsBytes;
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBuffer_Unix()
        {
            byte[] buffer = TestSettings.PayloadAsBytes;
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    // Non-root pings cannot send arbitrary data in the buffer, and do not receive it back in the PingReply.
                    if (Capability.CanUseRawSockets())
                    {
                        Assert.Equal(buffer, pingReply.Buffer);
                    }
                    else
                    {
                        Assert.Equal(Array.Empty<byte>(), pingReply.Buffer);
                    }
                });
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            var options = new PingOptions();
            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer, options),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                    Assert.Equal(buffer, pingReply.Buffer);
                    Assert.InRange(pingReply.RoundtripTime, 0, long.MaxValue);
                });
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBufferAndPingOptions_Unix()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    // Non-root pings cannot send arbitrary data in the buffer, and do not receive it back in the PingReply.
                    if (Capability.CanUseRawSockets())
                    {
                        Assert.Equal(buffer, pingReply.Buffer);
                    }
                    else
                    {
                        Assert.Equal(Array.Empty<byte>(), pingReply.Buffer);
                    }
                });
        }

        [Fact]
        public async Task SendPingAsyncWithHost()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [Fact]
        public async Task SendPingAsyncWithHostAndTimeout()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBuffer()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBuffer_Unix()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    // Non-root pings cannot send arbitrary data in the buffer, and do not receive it back in the PingReply.
                    if (Capability.CanUseRawSockets())
                    {
                        Assert.Equal(buffer, pingReply.Buffer);
                    }
                    else
                    {
                        Assert.Equal(Array.Empty<byte>(), pingReply.Buffer);
                    }
                });
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBufferAndPingOptions_Unix()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    // Non-root pings cannot send arbitrary data in the buffer, and do not receive it back in the PingReply.
                    if (Capability.CanUseRawSockets())
                    {
                        Assert.Equal(buffer, pingReply.Buffer);
                    }
                    else
                    {
                        Assert.Equal(Array.Empty<byte>(), pingReply.Buffer);
                    }
                });
        }

        [Fact]
        public static async Task SendPings_ReuseInstance_Hostname()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            using (Ping p = new Ping())
            {
                for (int i = 0; i < 3; i++)
                {
                    PingReply pingReply = await p.SendPingAsync(TestSettings.LocalHost);
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                }
            }
        }

        [Fact]
        public static async Task Ping_DisposeAfterSend_Success()
        {
            Ping p = new Ping();
            await p.SendPingAsync(TestSettings.LocalHost);
            p.Dispose();
        }

        [Fact]
        public static void Ping_DisposeMultipletimes_Success()
        {
            Ping p = new Ping();
            p.Dispose();
            p.Dispose();
        }

        [Fact]
        public static void Ping_SendAfterDispose_ThrowsSynchronously()
        {
            Ping p = new Ping();
            p.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { p.SendPingAsync(TestSettings.LocalHost); });
        }

        private static readonly int s_pingcount = 4;

        private static Task SendBatchPingAsync(Func<Ping, Task<PingReply>> sendPing, Action<PingReply> pingResultValidator)
        {
            // create several concurrent pings
            Task[] pingTasks = new Task[s_pingcount];
            for (int i = 0; i < s_pingcount; i++)
            {
                pingTasks[i] = SendPingAsync(sendPing, pingResultValidator);
            }
            return Task.WhenAll(pingTasks);
        }

        private static async Task SendPingAsync(Func<Ping, Task<PingReply>> sendPing, Action<PingReply> pingResultValidator)
        {
            var pingResult = await sendPing(new Ping());
            pingResultValidator(pingResult);
        }

        [Fact]
        public void CanBeFinalized()
        {
            FinalizingPing.CreateAndRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(FinalizingPing.WasFinalized);
        }
    }
}
