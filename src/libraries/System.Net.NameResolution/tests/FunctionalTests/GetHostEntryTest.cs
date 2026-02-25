// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class GetHostEntryTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task Dns_GetHostEntryAsync_IPAddress_Ok()
        {
            IPAddress localIPAddress = await TestSettings.GetLocalIPAddress();
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(localIPAddress));
        }


        public static bool GetHostEntryWorks =
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/27622")]
            PlatformDetection.IsNotArmNorArm64Process &&
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/1488", TestPlatforms.OSX)]
            !PlatformDetection.IsOSX &&
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/51377", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
            !PlatformDetection.IsiOS && !PlatformDetection.IstvOS && !PlatformDetection.IsMacCatalyst;

        [ConditionalTheory(nameof(GetHostEntryWorks))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public async Task Dns_GetHostEntry_HostString_Ok(string hostName)
        {
            try
            {
                await TestGetHostEntryAsync(() => Task.FromResult(Dns.GetHostEntry(hostName)));
            }
            catch (Exception ex) when (hostName == "")
            {
                // Additional data for debugging sporadic CI failures https://github.com/dotnet/runtime/issues/1488
                string actualHostName = Dns.GetHostName();
                string etcHosts = "";
                Exception getHostEntryException = null;
                Exception etcHostsException = null;

                try
                {
                    Dns.GetHostEntry(actualHostName);
                }
                catch (Exception e2)
                {
                    getHostEntryException = e2;
                }

                try
                {
                    if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                    {
                        etcHosts = File.ReadAllText("/etc/hosts");
                    }
                }
                catch (Exception e2)
                {
                    etcHostsException = e2;
                }

                throw new Exception(
                    $"Failed for empty hostname.{Environment.NewLine}" +
                    $"Dns.GetHostName() == {actualHostName}{Environment.NewLine}" +
                    $"{nameof(getHostEntryException)}=={getHostEntryException}{Environment.NewLine}" +
                    $"{nameof(etcHostsException)}=={etcHostsException}{Environment.NewLine}" +
                    $"/etc/host =={Environment.NewLine}{etcHosts}",
                    ex);
            }
        }

        [ConditionalTheory(nameof(GetHostEntryWorks))]
        [InlineData("")]
        [InlineData(TestSettings.LocalHost)]
        public async Task Dns_GetHostEntryAsync_HostString_Ok(string hostName)
        {
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(hostName));
        }

        [Fact]
        public async Task Dns_GetHostEntryAsync_IPString_Ok() =>
            await TestGetHostEntryAsync(() => Dns.GetHostEntryAsync(TestSettings.LocalIPString));

        private static async Task TestGetHostEntryAsync(Func<Task<IPHostEntry>> getHostEntryFunc)
        {
            Task<IPHostEntry> hostEntryTask1 = getHostEntryFunc();
            Task<IPHostEntry> hostEntryTask2 = getHostEntryFunc();

            await TestSettings.WhenAllOrAnyFailedWithTimeout(hostEntryTask1, hostEntryTask2);

            IPAddress[] list1 = hostEntryTask1.Result.AddressList;
            IPAddress[] list2 = hostEntryTask2.Result.AddressList;

            Assert.NotNull(list1);
            Assert.NotNull(list2);
            Assert.Equal<IPAddress>(list1, list2);
        }

        public static bool GetHostEntry_DisableIPv6_Condition = GetHostEntryWorks && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(GetHostEntry_DisableIPv6_Condition))]
        [InlineData("", false)]
        [InlineData("", true)]
        [InlineData(TestSettings.LocalHost, false)]
        [InlineData(TestSettings.LocalHost, true)]
        public void GetHostEntry_DisableIPv6_ExcludesIPv6Addresses(string hostnameOuter, bool useAsyncOuter)
        {
            string expectedHostName = Dns.GetHostEntry(hostnameOuter).HostName;
            RemoteExecutor.Invoke(RunTest, hostnameOuter, expectedHostName, useAsyncOuter.ToString()).Dispose();

            static async Task RunTest(string hostnameInner, string expectedHostName, string useAsync)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);

                IPHostEntry entry = bool.Parse(useAsync) ?
                    await Dns.GetHostEntryAsync(hostnameInner) :
                    Dns.GetHostEntry(hostnameInner);

                Assert.Equal(entry.HostName, expectedHostName);
                Assert.All(entry.AddressList, address => Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily));
            }
        }

        [ConditionalTheory(nameof(GetHostEntry_DisableIPv6_Condition))]
        [InlineData(false)]
        [InlineData(true)]
        public void GetHostEntry_DisableIPv6_AddressFamilyInterNetworkV6_ReturnsEmpty(bool useAsyncOuter)
        {
            RemoteExecutor.Invoke(RunTest, useAsyncOuter.ToString()).Dispose();
            static async Task RunTest(string useAsync)
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                IPHostEntry entry = bool.Parse(useAsync) ?
                    await Dns.GetHostEntryAsync(TestSettings.LocalHost, AddressFamily.InterNetworkV6) :
                    Dns.GetHostEntry(TestSettings.LocalHost, AddressFamily.InterNetworkV6);
                Assert.Empty(entry.AddressList);
            }
        }

        [Fact]
        public async Task Dns_GetHostEntry_NullStringHost_Fail()
        {
            Assert.Throws<ArgumentNullException>(() => Dns.GetHostEntry((string)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((string)null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task Dns_GetHostEntry_NullStringHost_Fail_Obsolete()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, (string)null, null));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Wasi, "WASI has no getnameinfo")]
        public async Task Dns_GetHostEntryAsync_NullIPAddressHost_Fail()
        {
            Assert.Throws<ArgumentNullException>(() => Dns.GetHostEntry((IPAddress)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.GetHostEntryAsync((IPAddress)null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, (IPAddress)null, null));
        }

        public static IEnumerable<object[]> GetInvalidAddresses()
        {
            yield return new object[] { IPAddress.Any };
            yield return new object[] { IPAddress.IPv6Any };
            yield return new object[] { IPAddress.IPv6None };
        }

        [Theory]
        [MemberData(nameof(GetInvalidAddresses))]
        [SkipOnPlatform(TestPlatforms.Wasi, "WASI has no getnameinfo")]
        public async Task Dns_GetHostEntry_AnyIPAddress_Fail(IPAddress address)
        {
            Assert.Throws<ArgumentException>(() => Dns.GetHostEntry(address));
            Assert.Throws<ArgumentException>(() => Dns.GetHostEntry(address.ToString()));

            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(address));
            await Assert.ThrowsAsync<ArgumentException>(() => Dns.GetHostEntryAsync(address.ToString()));

            await Assert.ThrowsAsync<ArgumentException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address, null));
            await Assert.ThrowsAsync<ArgumentException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address.ToString(), null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public async Task DnsGetHostEntry_MachineName_AllVariationsMatch()
        {
            IPHostEntry syncResult = Dns.GetHostEntry(TestSettings.LocalHost);
            IPHostEntry apmResult = Dns.EndGetHostEntry(Dns.BeginGetHostEntry(TestSettings.LocalHost, null, null));
            IPHostEntry asyncResult = await Dns.GetHostEntryAsync(TestSettings.LocalHost);

            Assert.Equal(syncResult.HostName, apmResult.HostName);
            Assert.Equal(syncResult.HostName, asyncResult.HostName);

            Assert.Equal(syncResult.AddressList, apmResult.AddressList);
            Assert.Equal(syncResult.AddressList, asyncResult.AddressList);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Wasi, "WASI has no getnameinfo")]
        public async Task DnsGetHostEntry_Loopback_AllVariationsMatch()
        {
            IPHostEntry syncResult = Dns.GetHostEntry(IPAddress.Loopback);
            IPHostEntry apmResult = Dns.EndGetHostEntry(Dns.BeginGetHostEntry(IPAddress.Loopback, null, null));
            IPHostEntry asyncResult = await Dns.GetHostEntryAsync(IPAddress.Loopback);

            Assert.Equal(syncResult.HostName, apmResult.HostName);
            Assert.Equal(syncResult.HostName, asyncResult.HostName);

            Assert.Equal(syncResult.AddressList, apmResult.AddressList);
            Assert.Equal(syncResult.AddressList, asyncResult.AddressList);
        }

        [Theory]
        [InlineData("BadName")] // unknown name
        [InlineData("0.0.1.1")] // unknown address
        [InlineData("Test-\u65B0-Unicode")] // unknown unicode name
        [InlineData("xn--test--unicode-0b01a")] // unknown punicode name
        [InlineData("Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Are")] // very long name but not too long
        [ActiveIssue("https://github.com/dotnet/runtime/issues/107339", TestPlatforms.Wasi)]
        public async Task DnsGetHostEntry_BadName_ThrowsSocketException(string hostNameOrAddress)
        {
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostEntryAsync(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<SocketException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, hostNameOrAddress, null));
        }

        [Theory]
        [InlineData("Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Aret")]
        public async Task DnsGetHostEntry_BadName_ThrowsArgumentOutOfRangeException(string hostNameOrAddress)
        {
            Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Dns.GetHostEntry(hostNameOrAddress));
            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => Dns.GetHostEntryAsync(hostNameOrAddress));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [InlineData("Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualllllllly.I.Will.Get.To.The.Eeeee"
                + "eeeeend.Almost.There.Are.We.Really.Long.Name.Over.One.Hundred.And.Twenty.Six.Chars.Eeeeeeeventualll"
                + "llllly.I.Will.Get.To.The.Eeeeeeeeeend.Almost.There.Aret")]
        public async Task DnsGetHostEntry_BadName_ThrowsArgumentOutOfRangeException_Obsolete(string hostNameOrAddress)
        {
            await Assert.ThrowsAnyAsync<ArgumentOutOfRangeException>(() => Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, hostNameOrAddress, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/107339", TestPlatforms.Wasi)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124079", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
        public async Task DnsGetHostEntry_LocalHost_ReturnsFqdnAndLoopbackIPs(int mode)
        {
            IPHostEntry entry = mode switch
            {
                0 => Dns.GetHostEntry("localhost"),
                1 => await Dns.GetHostEntryAsync("localhost"),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, "localhost", null)
            };

            Assert.NotNull(entry.HostName);
            Assert.True(entry.HostName.Length > 0, "Empty host name");
            Assert.True(entry.AddressList.Length >= 1, "No local IPs");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), "Not a loopback address: " + addr));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task DnsGetHostEntry_LoopbackIP_MatchesGetHostEntryLoopbackString(int mode)
        {
            if (OperatingSystem.IsWasi() && mode == 2)
                throw new SkipTestException("mode 2 is not supported on WASI");

            IPAddress address = IPAddress.Loopback;

            IPHostEntry ipEntry = mode switch
            {
                0 => Dns.GetHostEntry(address),
                1 => await Dns.GetHostEntryAsync(address),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address, null)
            };
            IPHostEntry stringEntry = mode switch
            {
                0 => Dns.GetHostEntry(address.ToString()),
                1 => await Dns.GetHostEntryAsync(address.ToString()),
                _ => await Task.Factory.FromAsync(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, address.ToString(), null)
            };

            Assert.Equal(ipEntry.HostName, stringEntry.HostName);
            Assert.Equal(ipEntry.AddressList, stringEntry.AddressList);
        }

        [OuterLoop]
        [Theory]
        [MemberData(nameof(AddressFamilySpecificTestData))]
        public async Task DnsGetHostEntry_LocalHost_AddressFamilySpecific(bool useAsync, string host, AddressFamily addressFamily)
        {
            IPHostEntry entry =
                useAsync ? await Dns.GetHostEntryAsync(host, addressFamily) :
                Dns.GetHostEntry(host, addressFamily);

            Assert.All(entry.AddressList, address => Assert.Equal(addressFamily, address.AddressFamily));
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

        // RFC 6761 Section 6.4: "invalid" and "*.invalid" must always return NXDOMAIN (HostNotFound).
        [Theory]
        [InlineData("invalid")]
        [InlineData("invalid.")]
        [InlineData("test.invalid")]
        [InlineData("test.invalid.")]
        [InlineData("foo.bar.invalid")]
        [InlineData("INVALID")]
        [InlineData("Test.INVALID")]
        public async Task DnsGetHostEntry_InvalidDomain_ThrowsHostNotFound(string hostName)
        {
            SocketException ex = Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(hostName));
            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);

            ex = await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostEntryAsync(hostName));
            Assert.Equal(SocketError.HostNotFound, ex.SocketErrorCode);
        }

        // RFC 6761 Section 6.3: "*.localhost" subdomains - OS resolver is tried first,
        // falling back to plain "localhost" resolution if OS resolver fails or returns empty.
        // This preserves /etc/hosts customizations.
        [Theory]
        [InlineData("foo.localhost")]
        [InlineData("bar.foo.localhost")]
        [InlineData("test.localhost")]
        [InlineData("FOO.LOCALHOST")]
        [InlineData("Test.LocalHost")]
        public async Task DnsGetHostEntry_LocalhostSubdomain_ReturnsLoopback(string hostName)
        {
            // The subdomain goes to OS resolver first. If it fails (likely on most systems),
            // it falls back to resolving plain "localhost", which should return loopback addresses.
            IPHostEntry entry = Dns.GetHostEntry(hostName);
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one loopback address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            entry = await Dns.GetHostEntryAsync(hostName);
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one loopback address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: Ensure names that look similar but are not reserved are still resolved via OS.
        [Theory]
        [InlineData("notlocalhost")]
        [InlineData("localhostfoo")]
        [InlineData("invalidname")]
        [InlineData("testinvalid")]
        public async Task DnsGetHostEntry_SimilarButNotReserved_ThrowsSocketException(string hostName)
        {
            // These should go to the OS resolver and fail with HostNotFound (not special-cased).
            Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(hostName));
            await Assert.ThrowsAnyAsync<SocketException>(() => Dns.GetHostEntryAsync(hostName));
        }

        // Malformed hostnames should not be treated as RFC 6761 reserved names.
        // They should fall through to the OS resolver which will reject them.
        // Note: Only ".invalid" variants are tested here. Malformed localhost names
        // (e.g., ".localhost") may succeed on some platforms because the OS resolver
        // handles localhost specially.
        [Theory]
        [InlineData(".invalid")]
        [InlineData("test..invalid")]
        public async Task DnsGetHostEntry_MalformedReservedName_NotTreatedAsReserved(string hostName)
        {
            // Malformed hostnames should go to OS resolver, not be special-cased.
            // OS resolver will typically reject them with ArgumentException or SocketException.
            Assert.ThrowsAny<Exception>(() => Dns.GetHostEntry(hostName));
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostEntryAsync(hostName));
        }

        // "localhost." (with trailing dot) should NOT be treated as a subdomain.
        // It's equivalent to plain "localhost" and should resolve via OS resolver.
        [Fact]
        public async Task DnsGetHostEntry_LocalhostWithTrailingDot_ReturnsLoopback()
        {
            IPHostEntry entry = Dns.GetHostEntry("localhost.");
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            entry = await Dns.GetHostEntryAsync("localhost.");
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: "*.localhost" subdomains should respect AddressFamily parameter.
        // OS resolver is tried first, falling back to plain "localhost" resolution.
        [Theory]
        [InlineData(AddressFamily.InterNetwork)]
        [InlineData(AddressFamily.InterNetworkV6)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124751", TestPlatforms.Android)]
        public async Task DnsGetHostEntry_LocalhostSubdomain_RespectsAddressFamily(AddressFamily addressFamily)
        {
            // Skip IPv6 test if OS doesn't support it.
            if (addressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
            {
                return;
            }

            string hostName = "test.localhost";

            // The subdomain goes to OS resolver first. If it fails, it falls back to
            // resolving plain "localhost" with the same address family filter.
            IPHostEntry entry = Dns.GetHostEntry(hostName, addressFamily);
            if (addressFamily == AddressFamily.InterNetwork)
            {
                Assert.True(entry.AddressList.Length >= 1, "Expected at least one IPv4 address");
            }
            Assert.All(entry.AddressList, addr => Assert.Equal(addressFamily, addr.AddressFamily));

            entry = await Dns.GetHostEntryAsync(hostName, addressFamily);
            if (addressFamily == AddressFamily.InterNetwork)
            {
                Assert.True(entry.AddressList.Length >= 1, "Expected at least one IPv4 address");
            }
            Assert.All(entry.AddressList, addr => Assert.Equal(addressFamily, addr.AddressFamily));
        }

        // RFC 6761: Verify that localhost subdomains return loopback addresses.
        // Note: We don't require exact equality with plain "localhost" because:
        // 1. The OS resolver is tried first for subdomains
        // 2. The OS may return different results (e.g., both IPv4+IPv6 vs IPv4 only)
        // 3. Different systems configure localhost differently
        // The key requirement is that localhost subdomains return loopback addresses.
        [Fact]
        public async Task DnsGetHostEntry_LocalhostAndSubdomain_BothReturnLoopback()
        {
            IPHostEntry localhostEntry = Dns.GetHostEntry("localhost");
            IPHostEntry subdomainEntry = Dns.GetHostEntry("foo.localhost");

            // Both should return loopback addresses
            Assert.True(localhostEntry.AddressList.Length >= 1);
            Assert.True(subdomainEntry.AddressList.Length >= 1);
            Assert.All(localhostEntry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
            Assert.All(subdomainEntry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            // Async version
            localhostEntry = await Dns.GetHostEntryAsync("localhost");
            subdomainEntry = await Dns.GetHostEntryAsync("bar.localhost");

            Assert.True(localhostEntry.AddressList.Length >= 1);
            Assert.True(subdomainEntry.AddressList.Length >= 1);
            Assert.All(localhostEntry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
            Assert.All(subdomainEntry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        // RFC 6761: Localhost subdomains with trailing dot should work (e.g., "foo.localhost.")
        // Trailing dot is valid DNS notation indicating the root.
        [Theory]
        [InlineData("foo.localhost.")]
        [InlineData("bar.test.localhost.")]
        public async Task DnsGetHostEntry_LocalhostSubdomainWithTrailingDot_ReturnsLoopback(string hostName)
        {
            IPHostEntry entry = Dns.GetHostEntry(hostName);
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one loopback address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));

            entry = await Dns.GetHostEntryAsync(hostName);
            Assert.True(entry.AddressList.Length >= 1, "Expected at least one loopback address");
            Assert.All(entry.AddressList, addr => Assert.True(IPAddress.IsLoopback(addr), $"Expected loopback address but got: {addr}"));
        }

        [Fact]
        public async Task DnsGetHostEntry_PreCancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Dns.GetHostEntryAsync(TestSettings.LocalHost, cts.Token));
            Assert.Equal(cts.Token, oce.CancellationToken);
        }
    }

    // Cancellation tests are sequential to reduce the chance of timing issues.
    [Collection(nameof(DisableParallelization))]
    public class GetHostEntryTest_Cancellation
    {
        [Fact]
        [OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33378", TestPlatforms.AnyUnix)] // Cancellation of an outstanding getaddrinfo is not supported on *nix.
        [SkipOnCoreClr("JitStress interferes with cancellation timing", RuntimeTestModes.JitStress | RuntimeTestModes.JitStressRegs)]
        public async Task DnsGetHostEntry_PostCancelledToken_Throws()
        {
            using var cts = new CancellationTokenSource();

            Task task = Dns.GetHostEntryAsync(TestSettings.UncachedHost, cts.Token);

            // This test might flake if the cancellation token takes too long to trigger:
            // It's a race between the DNS server getting back to us and the cancellation processing.
            cts.Cancel();

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            Assert.Equal(cts.Token, oce.CancellationToken);
        }
    }
}
