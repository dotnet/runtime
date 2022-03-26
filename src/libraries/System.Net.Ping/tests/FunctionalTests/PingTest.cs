// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.NetworkInformation.Tests
{
    public class PingTest
    {
        public readonly ITestOutputHelper _output;

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

        public PingTest(ITestOutputHelper output)
        {
            _output = output;
        }

        private void PingResultValidator(PingReply pingReply, IPAddress localIpAddress) => PingResultValidator(pingReply, new IPAddress[] { localIpAddress }, _output);

        private void PingResultValidator(PingReply pingReply, IPAddress[] localIpAddresses) => PingResultValidator(pingReply, localIpAddresses, null);

        private static void PingResultValidator(PingReply pingReply, IPAddress[] localIpAddresses, ITestOutputHelper? output)
        {
            Assert.Equal(IPStatus.Success, pingReply.Status);
            if (localIpAddresses.Any(addr => pingReply.Address.Equals(addr)))
            {
                // response did come from expected address. Test will pass.
                return;
            }
            // We did not find response address in given list.
            // Test is going to fail. Collect some more info.
            if (output != null)
            {
                output.WriteLine($"Reply address {pingReply.Address} is not expected local address.");
                foreach (IPAddress address in localIpAddresses)
                {
                    output.WriteLine($"Local address {address}");
                }
            }

            Assert.Contains(pingReply.Address, localIpAddresses); ///, "Reply address {pingReply.Address} is not expected local address.");
        }

        private static byte[] GetPingPayload(AddressFamily addressFamily)
            // On Unix, Non-root processes cannot send arbitrary data in the ping packet payload
            => Capability.CanUseRawSockets(addressFamily) || PlatformDetection.IsOSXLike
                ? TestSettings.PayloadAsBytes
                : Array.Empty<byte>();

        public static bool DoesNotUsePingUtility => OperatingSystem.IsWindows() ||
                                OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsWatchOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() ||
                                Capability.CanUseRawSockets(TestSettings.GetLocalIPAddress().AddressFamily);
        public static bool UsesPingUtility => !DoesNotUsePingUtility;

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsync_InvalidArgs()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync();
            Ping p = new Ping();

            // Null address
            AssertExtensions.Throws<ArgumentNullException>("address", () => { p.SendPingAsync((IPAddress)null); });
            AssertExtensions.Throws<ArgumentNullException>("hostNameOrAddress", () => { p.SendPingAsync((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { p.SendAsync((IPAddress)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("hostNameOrAddress", () => { p.SendAsync((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { p.Send((IPAddress)null); });
            AssertExtensions.Throws<ArgumentNullException>("hostNameOrAddress", () => { p.Send((string)null); });

            // Invalid address
            AssertExtensions.Throws<ArgumentException>("address", () => { p.SendPingAsync(IPAddress.Any); });
            AssertExtensions.Throws<ArgumentException>("address", () => { p.SendPingAsync(IPAddress.IPv6Any); });
            AssertExtensions.Throws<ArgumentException>("address", () => { p.SendAsync(IPAddress.Any, null); });
            AssertExtensions.Throws<ArgumentException>("address", () => { p.SendAsync(IPAddress.IPv6Any, null); });
            AssertExtensions.Throws<ArgumentException>("address", () => { p.Send(IPAddress.Any); });
            AssertExtensions.Throws<ArgumentException>("address", () => { p.Send(IPAddress.IPv6Any); });

            // Negative timeout
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(localIpAddress, -1); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(TestSettings.LocalHost, -1); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(localIpAddress, TimeSpan.FromMilliseconds(-1), default, default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(TestSettings.LocalHost, TimeSpan.FromMilliseconds(-1), default, default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(localIpAddress, TimeSpan.FromMilliseconds((long)int.MaxValue + 1), default, default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendPingAsync(TestSettings.LocalHost, TimeSpan.FromMilliseconds((long)int.MaxValue + 1), default, default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendAsync(localIpAddress, -1, null); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.SendAsync(TestSettings.LocalHost, -1, null); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(localIpAddress, -1); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(TestSettings.LocalHost, -1); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(localIpAddress, TimeSpan.FromMilliseconds(-1), default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(TestSettings.LocalHost, TimeSpan.FromMilliseconds(-1), default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(localIpAddress, TimeSpan.FromMilliseconds((long)int.MaxValue + 1), default, default); });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("timeout", () => { p.Send(TestSettings.LocalHost, TimeSpan.FromMilliseconds((long)int.MaxValue + 1), default, default); });

            // Null byte[]
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.SendPingAsync(localIpAddress, 0, null); });
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.SendPingAsync(TestSettings.LocalHost, 0, null); });
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.SendAsync(localIpAddress, 0, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.SendAsync(TestSettings.LocalHost, 0, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.Send(localIpAddress, 0, null); });
            AssertExtensions.Throws<ArgumentNullException>("buffer", () => { p.Send(TestSettings.LocalHost, 0, null); });

            // Too large byte[]
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.SendPingAsync(localIpAddress, 1, new byte[65501]); });
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.SendPingAsync(TestSettings.LocalHost, 1, new byte[65501]); });
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.SendAsync(localIpAddress, 1, new byte[65501], null); });
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.SendAsync(TestSettings.LocalHost, 1, new byte[65501], null); });
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.Send(localIpAddress, 1, new byte[65501]); });
            AssertExtensions.Throws<ArgumentException>("buffer", () => { p.Send(TestSettings.LocalHost, 1, new byte[65501]); });
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void SendPingWithIPAddress(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            SendBatchPing(
                (ping) => ping.Send(localIpAddress, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public async Task SendPingAsyncWithIPAddress(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void SendPingWithIPAddress_AddressAsString(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            SendBatchPing(
                (ping) => ping.Send(localIpAddress.ToString()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithIPAddress_AddressAsString()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress.ToString()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [Fact]
        public void SendPingWithIPAddressAndTimeout()
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress();

            SendBatchPing(
                (ping) => ping.Send(localIpAddress, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithIPAddressAndTimeout()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                });
        }

        [Fact]
        public void SendPingWithIPAddressAndTimeoutAndBuffer()
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress();
            byte[] buffer = GetPingPayload(localIpAddress.AddressFamily);

            SendBatchPing(
                (ping) => ping.Send(localIpAddress, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBuffer()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync();
            byte[] buffer = GetPingPayload(localIpAddress.AddressFamily);

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void SendPingWithIPAddressAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress();

            var options = new PingOptions();
            byte[] buffer = TestSettings.PayloadAsBytes;
            SendBatchPing(
                (ping) => ping.Send(localIpAddress, TestSettings.PingTimeout, buffer, options),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                    Assert.InRange(pingReply.RoundtripTime, 0, long.MaxValue);
                });
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync();

            var options = new PingOptions();
            byte[] buffer = TestSettings.PayloadAsBytes;
            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer, options),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                    Assert.InRange(pingReply.RoundtripTime, 0, long.MaxValue);
                });
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public void SendPingWithIPAddressAndTimeoutAndBufferAndPingOptions_Unix(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            byte[] buffer = GetPingPayload(localIpAddress.AddressFamily);

            SendBatchPing(
                (ping) => ping.Send(localIpAddress, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public async Task SendPingAsyncWithIPAddressAndTimeoutAndBufferAndPingOptions_Unix(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = await TestSettings.GetLocalIPAddressAsync(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            byte[] buffer = GetPingPayload(localIpAddress.AddressFamily);

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(localIpAddress, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddress);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [Fact]
        public void SendPingWithHost()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();

            SendBatchPing(
                (ping) => ping.Send(TestSettings.LocalHost),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithHost()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                });
        }

        [Fact]
        public void SendPingWithHostAndTimeout()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();

            SendBatchPing(
                (ping) => ping.Send(TestSettings.LocalHost, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithHostAndTimeout()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                });
        }

        [Fact]
        public void SendPingWithHostAndTimeoutAndBuffer()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();
            byte[] buffer = GetPingPayload(localIpAddresses[0].AddressFamily);

            SendBatchPing(
                (ping) => ping.Send(TestSettings.LocalHost, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithHostAndTimeoutAndBuffer()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();
            byte[] buffer = GetPingPayload(localIpAddresses[0].AddressFamily);

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [Fact]
        public void SendPingWithHostAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();
            byte[] buffer = GetPingPayload(localIpAddresses[0].AddressFamily);

            SendBatchPing(
                (ping) => ping.Send(TestSettings.LocalHost, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPingAsyncWithHostAndTimeoutAndBufferAndPingOptions()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();
            byte[] buffer = GetPingPayload(localIpAddresses[0].AddressFamily);

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer, new PingOptions()),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);

                    Assert.Equal(buffer, pingReply.Buffer);
                });
        }

        [ConditionalFact(nameof(DoesNotUsePingUtility))]
        public async Task SendPingWithIPAddressAndBigSize()
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress();

            using (Ping p = new Ping())
            {
                // Assert.DoesNotThrow
                PingReply pingReply = await p.SendPingAsync(localIpAddress, TestSettings.PingTimeout, new byte[10001]);

                // Depending on platform the call may either succeed, report timeout or report too big packet. It
                // should not throw wrapped SocketException though which is what this test guards.
                //
                // On Windows 10 the maximum ping size seems essentially limited to 65500 bytes and thus any buffer
                // size on the loopback ping succeeds. On macOS anything bigger than 8184 will report packet too
                // big error.
                if (OperatingSystem.IsMacOS())
                {
                    Assert.Equal(IPStatus.PacketTooBig, pingReply.Status);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendPings_ReuseInstance_Hostname()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();

            using (Ping p = new Ping())
            {
                for (int i = 0; i < 3; i++)
                {
                    PingReply pingReply = await p.SendPingAsync(TestSettings.LocalHost);
                    PingResultValidator(pingReply, localIpAddresses);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Sends_ReuseInstance_Hostname()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();

            using (Ping p = new Ping())
            {
                for (int i = 0; i < 3; i++)
                {
                    PingReply pingReply = p.Send(TestSettings.LocalHost);
                    PingResultValidator(pingReply, localIpAddresses);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task SendAsyncs_ReuseInstance_Hostname()
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();

            using (Ping p = new Ping())
            {
                TaskCompletionSource tcs = null;
                PingCompletedEventArgs ea = null;
                p.PingCompleted += (s, e) =>
                {
                    ea = e;
                    tcs.TrySetResult();
                };
                Action reset = () =>
                {
                    ea = null;
                    tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                };

                // Several normal iterations
                for (int i = 0; i < 3; i++)
                {
                    reset();
                    p.SendAsync(TestSettings.LocalHost, null);
                    await tcs.Task;

                    Assert.NotNull(ea);
                    PingResultValidator(ea.Reply, localIpAddresses);
                }

                // Several canceled iterations
                for (int i = 0; i < 3; i++)
                {
                    reset();
                    p.SendAsync(TestSettings.LocalHost, null);
                    p.SendAsyncCancel(); // will block until operation can be started again
                    await tcs.Task;

                    bool cancelled = ea.Cancelled;
                    Exception error = ea.Error;
                    PingReply reply = ea.Reply;
                    Assert.True(cancelled ^ (error != null) ^ (reply != null),
                        "Cancelled: " + cancelled +
                        (error == null ? "" : (Environment.NewLine + "Error Message: " + error.Message + Environment.NewLine + "Error Inner Exception: " + error.InnerException)) +
                        (reply == null ? "" : (Environment.NewLine + "Reply Address: " + reply.Address + Environment.NewLine + "Reply Status: " + reply.Status)));
                }
            }
        }

        [Fact]
        public static void Ping_DisposeAfterSend_Success()
        {
            Ping p = new Ping();
            p.Send(TestSettings.LocalHost);
            p.Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static async Task PingAsync_DisposeAfterSend_Success()
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
            Assert.Throws<ObjectDisposedException>(() => { p.Send(TestSettings.LocalHost); });
        }

        [Fact]
        public static void PingAsync_SendAfterDispose_ThrowsSynchronously()
        {
            Ping p = new Ping();
            p.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { p.SendPingAsync(TestSettings.LocalHost); });
        }

        private static readonly int s_pingcount = 4;

        private static void SendBatchPing(Func<Ping, PingReply> sendPing, Action<PingReply> pingResultValidator)
        {
            for (int i = 0; i < s_pingcount; i++)
            {
                SendPing(sendPing, pingResultValidator);
            }
        }

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

        private static void SendPing(Func<Ping, PingReply> sendPing, Action<PingReply> pingResultValidator)
        {
            var pingResult = sendPing(new Ping());
            pingResultValidator(pingResult);
        }

        private static async Task SendPingAsync(Func<Ping, Task<PingReply>> sendPing, Action<PingReply> pingResultValidator)
        {
            var pingResult = await sendPing(new Ping());
            pingResultValidator(pingResult);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void CanBeFinalized()
        {
            FinalizingPing.CreateAndRelease();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(FinalizingPing.WasFinalized);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SendPingAsyncWithHostAndTtlAndFragmentPingOptions(bool fragment)
        {
            IPAddress[] localIpAddresses = await TestSettings.GetLocalIPAddressesAsync();
            byte[] buffer = GetPingPayload(localIpAddresses[0].AddressFamily);

            PingOptions options = new PingOptions();
            options.Ttl = 32;
            options.DontFragment = fragment;

            await SendBatchPingAsync(
                (ping) => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer, options),
                (pingReply) =>
                {
                    PingResultValidator(pingReply, localIpAddresses);
                });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop] // Depends on external host and assumption that network respects and does not change TTL
        public async Task SendPingToExternalHostWithLowTtlTest()
        {
            string host = System.Net.Test.Common.Configuration.Ping.PingHost;
            PingReply pingReply;
            PingOptions options = new PingOptions();
            bool reachable = false;

            byte[] payload = UsesPingUtility ? Array.Empty<byte>() : TestSettings.PayloadAsBytesShort;

            Ping ping = new Ping();
            for (int i = 0; i < s_pingcount; i++)
            {
                pingReply = await ping.SendPingAsync(host, TestSettings.PingTimeout, payload);
                if (pingReply.Status == IPStatus.Success)
                {
                    reachable = true;
                    break;
                }
            }
            if (!reachable)
            {
                throw new SkipTestException($"Host {host} is not reachable. Skipping test.");
            }

            options.Ttl = 1;
            // This should always fail unless host is one IP hop away.
            pingReply = await ping.SendPingAsync(host, TestSettings.PingTimeout, payload, options);
            Assert.True(pingReply.Status == IPStatus.TimeExceeded || pingReply.Status == IPStatus.TtlExpired);
            Assert.NotEqual(IPAddress.Any, pingReply.Address);
        }

        [Fact]
        [OuterLoop]
        public void Ping_TimedOut_Sync_Success()
        {
            var sender = new Ping();
            PingReply reply = sender.Send(TestSettings.UnreachableAddress);
            Assert.Equal(IPStatus.TimedOut, reply.Status);
        }

        [Fact]
        [OuterLoop]
        public async Task Ping_TimedOut_EAP_Success()
        {
            var sender = new Ping();
            sender.PingCompleted += (s, e) =>
            {
                var tcs = (TaskCompletionSource<PingReply>)e.UserState;

                if (e.Cancelled)
                {
                    tcs.TrySetCanceled();
                }
                else if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else
                {
                    tcs.TrySetResult(e.Reply);
                }
            };

            var tcs = new TaskCompletionSource<PingReply>();
            sender.SendAsync(TestSettings.UnreachableAddress, tcs);

            PingReply reply = await tcs.Task;
            Assert.Equal(IPStatus.TimedOut, reply.Status);
        }

        [Fact]
        [OuterLoop]
        public async Task Ping_TimedOut_TAP_Success()
        {
            var sender = new Ping();
            PingReply reply = await sender.SendPingAsync(TestSettings.UnreachableAddress);
            Assert.Equal(IPStatus.TimedOut, reply.Status);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        [OuterLoop] // Depends on sudo
        public void SendPingWithIPAddressAndTimeoutAndBufferAndPingOptions_ElevatedUnix(AddressFamily addressFamily)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            _output.WriteLine($"pinging '{localIpAddress}'");

            RemoteExecutor.Invoke(address =>
            {
                byte[] buffer = TestSettings.PayloadAsBytes;
                SendBatchPing(
                    (ping) => ping.Send(address, TestSettings.PingTimeout, buffer, new PingOptions()),
                    (pingReply) =>
                    {
                        PingResultValidator(pingReply, new IPAddress[] { IPAddress.Parse(address) }, null);
                        Assert.Equal(buffer, pingReply.Buffer);
                    });
            }, localIpAddress.ToString(), new RemoteInvokeOptions { RunAsSudo = true }).Dispose();
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(AddressFamily.InterNetwork, "ja_JP.UTF8", null, null)]
        [InlineData(AddressFamily.InterNetwork, "en_US.UTF8", "ja_JP.UTF8", null)]
        [InlineData(AddressFamily.InterNetwork, "en_US.UTF8", null, "ja_JP.UTF8")]
        [InlineData(AddressFamily.InterNetworkV6, "ja_JP.UTF8", null, null)]
        [InlineData(AddressFamily.InterNetworkV6, "en_US.UTF8", "ja_JP.UTF8", null)]
        [InlineData(AddressFamily.InterNetworkV6, "en_US.UTF8", null, "ja_JP.UTF8")]
        public void SendPing_LocaleEnvVarsMustBeIgnored(AddressFamily addressFamily, string envVar_LANG, string envVar_LC_MESSAGES, string envVar_LC_ALL)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);
            if (localIpAddress == null)
            {
                // No local address for given address family.
                return;
            }

            var remoteInvokeStartInfo = new ProcessStartInfo();

            remoteInvokeStartInfo.EnvironmentVariables["LANG"] = envVar_LANG;
            remoteInvokeStartInfo.EnvironmentVariables["LC_MESSAGES"] = envVar_LC_MESSAGES;
            remoteInvokeStartInfo.EnvironmentVariables["LC_ALL"] = envVar_LC_ALL;

            RemoteExecutor.Invoke(address =>
            {
                SendBatchPing(
                    (ping) => ping.Send(address, TestSettings.PingTimeout),
                    (pingReply) =>
                    {
                        PingResultValidator(pingReply, new IPAddress[] { IPAddress.Parse(address) }, null);
                    });
            }, localIpAddress.ToString(), new RemoteInvokeOptions { StartInfo = remoteInvokeStartInfo }).Dispose();
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(AddressFamily.InterNetwork, "ja_JP.UTF8", null, null)]
        [InlineData(AddressFamily.InterNetwork, "en_US.UTF8", "ja_JP.UTF8", null)]
        [InlineData(AddressFamily.InterNetwork, "en_US.UTF8", null, "ja_JP.UTF8")]
        [InlineData(AddressFamily.InterNetworkV6, "ja_JP.UTF8", null, null)]
        [InlineData(AddressFamily.InterNetworkV6, "en_US.UTF8", "ja_JP.UTF8", null)]
        [InlineData(AddressFamily.InterNetworkV6, "en_US.UTF8", null, "ja_JP.UTF8")]
        public void SendPingAsync_LocaleEnvVarsMustBeIgnored(AddressFamily addressFamily, string envVar_LANG, string envVar_LC_MESSAGES, string envVar_LC_ALL)
        {
            IPAddress localIpAddress = TestSettings.GetLocalIPAddress(addressFamily);

            var remoteInvokeStartInfo = new ProcessStartInfo {
                EnvironmentVariables =
                {
                    ["LANG"] = envVar_LANG,
                    ["LC_MESSAGES"] = envVar_LC_MESSAGES,
                    ["LC_ALL"] = envVar_LC_ALL
                }
            };

            RemoteExecutor.Invoke(async address =>
            {
                await SendBatchPingAsync(
                    (ping) => ping.SendPingAsync(address),
                    (pingReply) =>
                    {
                        PingResultValidator(pingReply, new IPAddress[] { IPAddress.Parse(address) }, null);
                    });
            }, localIpAddress.ToString(), new RemoteInvokeOptions { StartInfo = remoteInvokeStartInfo }).Dispose();
        }

        [ConditionalFact(nameof(UsesPingUtility))]
        public void SendPing_CustomPayload_InsufficientPrivileges_Throws()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();

            byte[] buffer = TestSettings.PayloadAsBytes;
            Ping ping = new Ping();
            Assert.Throws<PlatformNotSupportedException>(() => ping.Send(TestSettings.LocalHost, TestSettings.PingTimeout, buffer));
        }

        [ConditionalFact(nameof(UsesPingUtility))]
        public async Task SendPingAsync_CustomPayload_InsufficientPrivileges_Throws()
        {
            IPAddress[] localIpAddresses = TestSettings.GetLocalIPAddresses();

            byte[] buffer = TestSettings.PayloadAsBytes;
            Ping ping = new Ping();
            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => ping.SendPingAsync(TestSettings.LocalHost, TestSettings.PingTimeout, buffer));
        }
    }
}
