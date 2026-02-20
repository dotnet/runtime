// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void DnsBeginGetHostAddresses_BadName_Throws()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("BadName", null, null);
            Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(asyncObject));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void DnsBeginGetHostAddresses_BadIpString_ReturnsAddress()
        {
            IAsyncResult asyncObject = Dns.BeginGetHostAddresses("0.0.1.1", null, null);
            IPAddress[] results = Dns.EndGetHostAddresses(asyncObject);

            Assert.Equal(1, results.Length);
            Assert.Equal(IPAddress.Parse("0.0.1.1"), results[0]);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GetHostAddresses_DisableIPv6_ExcludesIPv6Addresses(bool useAsyncOuter)
        {
            RemoteExecutor.Invoke(RunTest, useAsyncOuter.ToString()).Dispose();

            static async Task RunTest(string useAsync)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                IPAddress[] addresses =
                    bool.Parse(useAsync) ? await Dns.GetHostAddressesAsync(TestSettings.LocalHost) :
                    Dns.GetHostAddresses(TestSettings.LocalHost);
                Assert.All(addresses, address => Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily));
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void GetHostAddresses_DisableIPv6_AddressFamilyInterNetworkV6_ReturnsEmpty(bool useAsyncOuter)
        {
            RemoteExecutor.Invoke(RunTest, useAsyncOuter.ToString()).Dispose();
            static async Task RunTest(string useAsync)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                IPAddress[] addresses =
                    bool.Parse(useAsync) ? await Dns.GetHostAddressesAsync(TestSettings.LocalHost, AddressFamily.InterNetworkV6) :
                    Dns.GetHostAddresses(TestSettings.LocalHost, AddressFamily.InterNetworkV6);
                Assert.Empty(addresses);
            }
        }

        // RFC 6761 Section 6.4: "invalid" and "*.invalid" must always return NXDOMAIN (HostNotFound).
        [Theory]
        [InlineData("invalid")]
        [InlineData("invalid.")]
        [InlineData("test.invalid")]
        [InlineData("test.invalid.")]
        [InlineData("foo.bar.invalid")]
        [InlineData("INVALID")]
        [InlineData("Test.INVALID")]
        public async Task DnsGetHostAddresses_InvalidDomain_ThrowsHostNotFound(string hostName)
        {
            SocketException ex = Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(hostName));
            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);

            ex = await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostAddressesAsync(hostName));
            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);
        }

        // RFC 6761 Section 6.3: "*.localhost" subdomains - OS resolver is tried first,
        // falling back to plain "localhost" resolution if OS resolver fails or returns empty.
        [Theory]
        [InlineData("foo.localhost")]
        [InlineData("bar.foo.localhost")]
        [InlineData("test.localhost")]
        [InlineData("FOO.LOCALHOST")]
        [InlineData("Test.LocalHost")]
        public async Task DnsGetHostAddresses_LocalhostSubdomain_ReturnsLoopback(string hostName)
        {
            // The subdomain goes to OS resolver first. If it fails (likely on most systems),
            // it falls back to resolving plain "localhost", which should return loopback addresses.
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            Assert.True(addresses.Length >= 1, "Expected at least one loopback address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            addresses = await Dns.GetHostAddressesAsync(hostName);
            Assert.True(addresses.Length >= 1, "Expected at least one loopback address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: "*.localhost" subdomains should respect AddressFamily parameter.
        // OS resolver is tried first, falling back to plain "localhost" resolution.
        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        public async Task DnsGetHostAddresses_LocalhostSubdomain_RespectsAddressFamily(AddressFamily addressFamily)
        {
            // Skip IPv6 test if OS doesn't support it.
            if (addressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
            {
                return;
            }

            string hostName = "test.localhost";

            // The subdomain goes to OS resolver first. If it fails, it falls back to
            // resolving plain "localhost" with the same address family filter.
            IPAddress[] addresses = Dns.GetHostAddresses(hostName, addressFamily);
            if (addressFamily == AddressFamily.InterNetwork)
            {
                Assert.True(addresses.Length >= 1, "Expected at least one IPv4 address");
            }
            Assert.All(addresses, addr => Assert.Equal(addressFamily, addr.AddressFamily));

            addresses = await Dns.GetHostAddressesAsync(hostName, addressFamily);
            if (addressFamily == AddressFamily.InterNetwork)
            {
                Assert.True(addresses.Length >= 1, "Expected at least one IPv4 address");
            }
            Assert.All(addresses, addr => Assert.Equal(addressFamily, addr.AddressFamily));
        }

        // RFC 6761: Verify that localhost subdomains return loopback addresses.
        // Note: We don't require exact equality with plain "localhost" because:
        // 1. The OS resolver is tried first for subdomains
        // 2. The OS may return different results (e.g., both IPv4+IPv6 vs IPv4 only)
        // 3. Different systems configure localhost differently
        // The key requirement is that localhost subdomains return loopback addresses.
        [Fact]
        public async Task DnsGetHostAddresses_LocalhostAndSubdomain_BothReturnLoopback()
        {
            IPAddress[] localhostAddresses = Dns.GetHostAddresses("localhost");
            IPAddress[] subdomainAddresses = Dns.GetHostAddresses("foo.localhost");

            // Both should return loopback addresses
            Assert.True(localhostAddresses.Length >= 1);
            Assert.True(subdomainAddresses.Length >= 1);
            Assert.All(localhostAddresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
            Assert.All(subdomainAddresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            // Async version
            localhostAddresses = await Dns.GetHostAddressesAsync("localhost");
            subdomainAddresses = await Dns.GetHostAddressesAsync("bar.localhost");

            Assert.True(localhostAddresses.Length >= 1);
            Assert.True(subdomainAddresses.Length >= 1);
            Assert.All(localhostAddresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
            Assert.All(subdomainAddresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: Localhost subdomains with trailing dot should work (e.g., "foo.localhost.")
        [Theory]
        [InlineData("foo.localhost.")]
        [InlineData("bar.test.localhost.")]
        public async Task DnsGetHostAddresses_LocalhostSubdomainWithTrailingDot_ReturnsLoopback(string hostName)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);
            Assert.True(addresses.Length >= 1, "Expected at least one loopback address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            addresses = await Dns.GetHostAddressesAsync(hostName);
            Assert.True(addresses.Length >= 1, "Expected at least one loopback address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: Ensure names that look similar but are not reserved are still resolved via OS.
        [Theory]
        [InlineData("notlocalhost")]
        [InlineData("localhostfoo")]
        [InlineData("invalidname")]
        [InlineData("testinvalid")]
        public async Task DnsGetHostAddresses_SimilarButNotReserved_ThrowsSocketException(string hostName)
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(hostName));
            await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostAddressesAsync(hostName));
        }

        // Malformed hostnames should not be treated as RFC 6761 reserved names.
        // Note: Only ".invalid" variants are tested here. Malformed localhost names
        // (e.g., ".localhost") may succeed on some platforms because the OS resolver
        // handles localhost specially.
        [Theory]
        [InlineData(".invalid")]
        [InlineData("test..invalid")]
        public async Task DnsGetHostAddresses_MalformedReservedName_NotTreatedAsReserved(string hostName)
        {
            Assert.ThrowsAny<Exception>(() => Dns.GetHostAddresses(hostName));
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostAddressesAsync(hostName));
        }

        // "localhost." (with trailing dot) should NOT be treated as a subdomain.
        [Fact]
        public async Task DnsGetHostAddresses_LocalhostWithTrailingDot_ReturnsLoopback()
        {
            IPAddress[] addresses = Dns.GetHostAddresses("localhost.");
            Assert.True(addresses.Length >= 1, "Expected at least one address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            addresses = await Dns.GetHostAddressesAsync("localhost.");
            Assert.True(addresses.Length >= 1, "Expected at least one address");
            Assert.All(addresses, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }
    }

    // Cancellation tests are sequential to reduce the chance of timing issues.
    [Collection(nameof(DisableParallelization))]
    public class GetHostAddressesTest_Cancellation
    {
        [OuterLoop]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/78909")]
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
