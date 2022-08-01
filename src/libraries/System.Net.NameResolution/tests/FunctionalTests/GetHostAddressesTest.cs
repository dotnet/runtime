// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostAddressesTest
    {
        [Fact]
        public Task Dns_GetHostAddressesAsync_HostString_Ok() => TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalHost));

        [Fact]
        public Task Dns_GetHostAddressesAsync_IPString_Ok() => TestGetHostAddressesAsync(() => Dns.GetHostAddressesAsync(TestSettings.LocalIPString));

        private static async Task TestGetHostAddressesAsync(Func<Task<IPAddress[]>> getHostAddressesFunc)
        {
            Task<IPAddress[]> hostEntryTask1 = getHostAddressesFunc();
            Task<IPAddress[]> hostEntryTask2 = getHostAddressesFunc();

            await TestSettings.WhenAllOrAnyFailedWithTimeout(hostEntryTask1, hostEntryTask2);

            IPAddress[] list1 = hostEntryTask1.Result;
            IPAddress[] list2 = hostEntryTask2.Result;

            Assert.NotNull(list1);
            Assert.NotNull(list2);

            Assert.Equal(list1.Length, list2.Length);

            var set = new HashSet<IPAddress>();
            for (int i = 0; i < list1.Length; i++)
            {
                Assert.Equal(list1[i], list2[i]);
                Assert.True(set.Add(list1[i]), "Multiple entries for address " + list1[i]);
            }
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_LongHostNameEndsInDot_Ok()
        {
            int maxHostName = 255;
            string longHostName = new string('a', maxHostName - 1);
            string longHostNameWithDot = longHostName + ".";

            SocketException ex = await Assert.ThrowsAnyAsync<SocketException>(
                () => Dns.GetHostAddressesAsync(longHostNameWithDot));

            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_LongHostNameDoesNotEndInDot_Fail()
        {
            int maxHostName = 255;
            string longHostName = new string('a', maxHostName);
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Dns.GetHostAddressesAsync(longHostName));
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_NullHost_Fail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostAddressesAsync(null));
        }

        [Fact]
        public void DnsBeginGetHostAddresses_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(asyncObject));
        }

        [Fact]
        public void DnsBeginGetHostAddresses_BadIpString_ReturnsAddress()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("0.0.1.1", null, null);
            IPAddress[] results = Dns.EndGetHostAddresses(asyncObject);

            Assert.Equal(1, results.Length);
            Assert.Equal(IPAddress.Parse("0.0.1.1"), results[0]);
        }

        [Fact]
        public void DnsBeginGetHostAddresses_MachineName_MatchesGetHostAddresses()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses(TestSettings.LocalHost, null, null);
            IPAddress[] results = Dns.EndGetHostAddresses(asyncObject);
            IPAddress[] addresses = Dns.GetHostAddresses(TestSettings.LocalHost);
            Assert.Equal(addresses, results);
        }

        [Fact]
        public void DnsGetHostAddresses_IPv4String_ReturnsSameIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(IPAddress.Loopback.ToString());
            Assert.Equal(1, addresses.Length);
            Assert.Equal(IPAddress.Loopback, addresses[0]);
        }

        [Fact]
        public void DnsGetHostAddresses_IPv6String_ReturnsSameIP()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(IPAddress.IPv6Loopback.ToString());
            Assert.Equal(1, addresses.Length);
            Assert.Equal(IPAddress.IPv6Loopback, addresses[0]);
        }

        [Theory]
        [MemberData(nameof(IPAndIncorrectFamily_Data))]
        public async Task DnsGetHostAddresses_IPStringAndIncorrectFamily_ReturnsNoIPs(bool useAsync, IPAddress address, AddressFamily family)
        {
            IPAddress[] addresses =
                useAsync ? await Dns.GetHostAddressesAsync(address.ToString(), family) :
                Dns.GetHostAddresses(address.ToString(), family);

            Assert.Empty(addresses);
        }

        public static TheoryData<bool, IPAddress, AddressFamily> IPAndIncorrectFamily_Data => new TheoryData<bool, IPAddress, AddressFamily>
        {
            // useAsync, IP, family
            { false, IPAddress.Loopback, AddressFamily.InterNetworkV6 },
            { false, IPAddress.IPv6Loopback, AddressFamily.InterNetwork },
            { true, IPAddress.Loopback, AddressFamily.InterNetworkV6 },
            { true, IPAddress.IPv6Loopback, AddressFamily.InterNetwork }
        };

        [Fact]
        public void DnsGetHostAddresses_LocalHost_ReturnsSameAsGetHostEntry()
        {
            IPAddress[] addresses = Dns.GetHostAddresses(TestSettings.LocalHost);
            IPHostEntry ipEntry = Dns.GetHostEntry(TestSettings.LocalHost);

            Assert.Equal(ipEntry.AddressList, addresses);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(AddressFamilySpecificTestData))]
        public async Task DnsGetHostAddresses_LocalHost_AddressFamilySpecific(bool useAsync, string host, AddressFamily addressFamily)
        {
            IPAddress[] addresses =
                useAsync ? await Dns.GetHostAddressesAsync(host, addressFamily) :
                Dns.GetHostAddresses(host, addressFamily);

            Assert.All(addresses, address => Assert.Equal(addressFamily, address.AddressFamily));
        }

        public static TheoryData<bool, string, AddressFamily> AddressFamilySpecificTestData =>
            new TheoryData<bool, string, AddressFamily>()
            {
                // async, hostname, af
                { false, TestSettings.IPv4Host, AddressFamily.InterNetwork },
                { false, TestSettings.IPv6Host, AddressFamily.InterNetworkV6 },
                { true, TestSettings.IPv4Host, AddressFamily.InterNetwork },
                { true, TestSettings.IPv6Host, AddressFamily.InterNetworkV6 }
            };

        [Fact]
        public async Task DnsGetHostAddresses_PreCancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Dns.GetHostAddressesAsync(TestSettings.LocalHost, cts.Token));
            Assert.Equal(cts.Token, oce.CancellationToken);
        }
    }

    // Cancellation tests are sequential to reduce the chance of timing issues.
    [Collection(nameof(DisableParallelization))]
    public class GetHostAddressesTest_Cancellation
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33378", TestPlatforms.AnyUnix)] // Cancellation of an outstanding getaddrinfo is not supported on *nix.
        [SkipOnCoreClr("JitStress interferes with cancellation timing", RuntimeTestModes.JitStress | RuntimeTestModes.JitStressRegs)]
        public async Task DnsGetHostAddresses_PostCancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();

            Task task = Dns.GetHostAddressesAsync(TestSettings.UncachedHost, cts.Token);

            // This test might flake if the cancellation token takes too long to trigger:
            // It's a race between the DNS server getting back to us and the cancellation processing.
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        // This is a regression test for https://github.com/dotnet/runtime/issues/63552
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33378", TestPlatforms.AnyUnix)] // Cancellation of an outstanding getaddrinfo is not supported on *nix.
        public async Task DnsGetHostAddresses_ResolveParallelCancelOnFailure_AllCallsReturn()
        {
            string invalidAddress = TestSettings.UncachedHost;
            await ResolveManyAsync(invalidAddress);
            await ResolveManyAsync(invalidAddress, TestSettings.LocalHost)
                .WaitAsync(TestSettings.PassingTestTimeout);

            static async Task ResolveManyAsync(params string[] addresses)
            {
                using CancellationTokenSource cts = new();
                Task[] resolveTasks = addresses.Select(a => ResolveOneAsync(a, cts)).ToArray();
                await Task.WhenAll(resolveTasks);
            }

            static async Task ResolveOneAsync(string address, CancellationTokenSource cancellationTokenSource)
            {
                try
                {
                    await Dns.GetHostAddressesAsync(address, cancellationTokenSource.Token);
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }
    }
}
