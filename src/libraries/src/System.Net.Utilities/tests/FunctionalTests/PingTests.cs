// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.NetworkInformation;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Utilities.Tests
{
    public class PingTests
    {
        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddress()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeout()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBuffer()
        {
            byte[] buffer = TestSettings.PayloadAsBytes;
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHost()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeout()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBuffer()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    Assert.Equal(IPStatus.Success, pingReply.Status);
                    Assert.True(pingReply.Address.Equals(localIpAddress));

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ActiveIssue(2487, PlatformID.AnyUnix)]
        [Fact]
        public async Task SendPingAsyncWithHostAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddress();

            byte[] buffer = TestSettings.PayloadAsBytes;
            SendBatchPingAsync(
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
        public async Task SendAsync_ThrowsPlatformNotSupported_Unix()
        {
            // TODO: Remove this test once Ping implemented on Unix

            IPAddress localIpAddress = IPAddress.Loopback;
            Ping p = new Ping();

            PingException e = await Assert.ThrowsAsync<PingException>(() => p.SendPingAsync(localIpAddress));
            Assert.IsType<PlatformNotSupportedException>(e.InnerException);
        }

        private const int PingCount = 4;

        private static void SendBatchPingAsync(Func<Ping, Task<PingReply>> sendPing, Action<PingReply> pingResultValidator)
        {
            // create several concurrent pings
            Task[] pingTasks = new Task[PingCount];
            for (int i = 0; i < PingCount; i++)
            {
                pingTasks[i] = SendPingAsync(sendPing, pingResultValidator);
            }
            Task.WaitAll(pingTasks);
        }

        private static async Task SendPingAsync(Func<Ping, Task<PingReply>> sendPing, Action<PingReply> pingResultValidator)
        {
            var pingResult = await sendPing(new Ping());
            pingResultValidator(pingResult);
        }
    }
}
