// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    // Tests for the new DnsResolver / Dns.Resolve* APIs.
    // Network tests are individually marked with [OuterLoop].
    public class DnsResolverTest
    {
        private const string TestHost = "microsoft.com";
        private const string TestSrv = "_sip._tls.microsoft.com";   // SRV record for SIP discovery
        private const string TestMxHost = "microsoft.com";
        private const string TestTxtHost = "microsoft.com";
        private const string TestCNameHost = "www.microsoft.com";
        private const string TestNsHost = "microsoft.com";
        private const string NonExistentHost = "this-name-definitely-does-not-exist.dotnet-test.invalid";

        // ---- Cross-platform argument-validation tests ----

        [Fact]
        public void DnsResolver_Construct_NullOptions_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DnsResolver(null!));
        }

        [Fact]
        public void DnsResolver_Construct_DefaultOptions_DoesNotThrow()
        {
            using DnsResolver r = new DnsResolver();
            Assert.NotNull(r);
        }

        [Fact]
        public async Task DnsResolver_NullName_Throws()
        {
            using DnsResolver r = new DnsResolver();
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveAddressesAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveSrvAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveMxAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveTxtAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveCNameAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolvePtrAsync((string)null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolvePtrAsync((IPAddress)null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => r.ResolveNsAsync(null!));
        }

        [Fact]
        public void DnsResolver_NullName_Throws_Sync()
        {
            using DnsResolver r = new DnsResolver();
            Assert.Throws<ArgumentNullException>(() => r.ResolveAddresses(null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolveSrv(null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolveMx(null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolveTxt(null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolveCName(null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolvePtr((string)null!));
            Assert.Throws<ArgumentNullException>(() => r.ResolveNs(null!));
        }

        [Fact]
        public async Task DnsResolver_EmptyName_Throws()
        {
            using DnsResolver r = new DnsResolver();
            await Assert.ThrowsAsync<ArgumentException>(() => r.ResolveAddressesAsync(string.Empty));
            Assert.Throws<ArgumentException>(() => r.ResolveAddresses(string.Empty));
        }

        [Fact]
        public async Task DnsResolver_Disposed_Throws()
        {
            DnsResolver r = new DnsResolver();
            r.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => r.ResolveAddressesAsync(TestHost));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => r.ResolveSrvAsync(TestSrv));
            await Assert.ThrowsAsync<ObjectDisposedException>(() => r.ResolveMxAsync(TestMxHost));
            Assert.Throws<ObjectDisposedException>(() => r.ResolveAddresses(TestHost));
            Assert.Throws<ObjectDisposedException>(() => r.ResolveSrv(TestSrv));
            Assert.Throws<ObjectDisposedException>(() => r.ResolveMx(TestMxHost));
        }

        [Fact]
        public async Task DnsResolver_DisposeAsync_ThrowsOnUse()
        {
            DnsResolver r = new DnsResolver();
            await r.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => r.ResolveAddressesAsync(TestHost));
        }

        // ---- Sync/async dispatch helpers ----
        // The synchronous overloads execute inline on the calling thread; the results
        // are wrapped in a completed Task so each test can await a single helper.

        private static async Task<DnsResult<AddressRecord>> ResolveAddresses(bool async, DnsResolver resolver, string name, AddressFamily addressFamily = AddressFamily.Unspecified)
            => async ? await resolver.ResolveAddressesAsync(name, addressFamily) : resolver.ResolveAddresses(name, addressFamily);

        private static async Task<DnsResult<MxRecord>> ResolveMx(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveMxAsync(name) : resolver.ResolveMx(name);

        private static async Task<DnsResult<TxtRecord>> ResolveTxt(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveTxtAsync(name) : resolver.ResolveTxt(name);

        private static async Task<DnsResult<CNameRecord>> ResolveCName(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveCNameAsync(name) : resolver.ResolveCName(name);

        private static async Task<DnsResult<NsRecord>> ResolveNs(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveNsAsync(name) : resolver.ResolveNs(name);

        private static async Task<DnsResult<PtrRecord>> ResolvePtr(bool async, DnsResolver resolver, IPAddress address)
            => async ? await resolver.ResolvePtrAsync(address) : resolver.ResolvePtr(address);

        private static async Task<DnsResult<AddressRecord>> Static_ResolveAddresses(bool async, string name)
            => async ? await Dns.ResolveAddressesAsync(name) : Dns.ResolveAddresses(name);

        public static TheoryData<bool, string> SynchronouslyCompletingQueryNames()
        {
            string hostName = Dns.GetHostName();
            return new TheoryData<bool, string>
            {
                { false, "localhost" },
                { true, "localhost" },
                { false, "loopback" },
                { true, "loopback" },
                { false, "..DnsServers" },
                { true, "..DnsServers" },
                { false, "..localmachine" },
                { true, "..localmachine" },
                { false, "127.0.0.1" },
                { true, "127.0.0.1" },
                { false, "::1" },
                { true, "::1" },
                { false, hostName },
                { true, hostName },
            };
        }

        // ---- Windows network tests (require outbound DNS) ----

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public async Task DnsResolver_PreCanceledToken_ReturnsCanceled()
        {
            using DnsResolver r = new DnsResolver();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => r.ResolveAddressesAsync(TestHost, cts.Token));
        }

        // Regression test for the Windows 10 DnsQueryEx bug where an asynchronous query
        // that the OS can satisfy synchronously (for example localhost, loopback, IP
        // literals, the local host name, and a few Windows special names) returns
        // ERROR_SUCCESS inline and never invokes the registered completion callback.
        // If the implementation waited for that callback it would hang forever; the PAL
        // must instead detect the synchronous completion (any status other than
        // DNS_REQUEST_PENDING) and surface the result directly.
        // See https://dblohm7.ca/blog/2022/05/06/dnsqueryex-needs-love/.
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [MemberData(nameof(SynchronouslyCompletingQueryNames))]
        public async Task ResolveAddresses_SynchronouslyCompletingQuery_DoesNotHang(bool async, string name)
        {
            using DnsResolver r = new DnsResolver();

            // These names can be answered without the normal asynchronous callback path,
            // which is what triggers the synchronous-completion path inside DnsQueryEx.
            // A short timeout turns the "callback never fires" hang into a test failure
            // rather than letting the run stall.
            Task<DnsResult<AddressRecord>> task = ResolveAddresses(async, r, name);
            DnsResult<AddressRecord> result = await task.WaitAsync(TimeSpan.FromSeconds(30));

            // ..DnsServers and ..localmachine may return a non-NoError code on machines without
            // DNS servers configured; treat that as acceptable. But when the query succeeds the
            // records list must be non-empty — that is the key correctness check for the
            // async-path sync-fallback: a NoError response must come with actual records.
            if (result.ResponseCode == DnsResponseCode.NoError)
            {
                Assert.NotEmpty(result.Records);

                if (name is "localhost" or "loopback" or "127.0.0.1" or "::1")
                {
                    foreach (AddressRecord rec in result.Records)
                    {
                        Assert.True(IPAddress.IsLoopback(rec.Address), $"Expected a loopback address but got {rec.Address}.");
                    }
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveAddresses_KnownName_ReturnsRecords(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await ResolveAddresses(async, r, TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (AddressRecord rec in result.Records)
            {
                Assert.NotNull(rec.Address);
                Assert.True(rec.Address.AddressFamily == AddressFamily.InterNetwork || rec.Address.AddressFamily == AddressFamily.InterNetworkV6);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveAddresses_IPv4Only_ReturnsOnlyIPv4(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await ResolveAddresses(async, r, TestHost, AddressFamily.InterNetwork);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            foreach (AddressRecord rec in result.Records)
            {
                Assert.Equal(AddressFamily.InterNetwork, rec.Address.AddressFamily);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveAddresses_NonExistent_ReturnsNxDomain(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await ResolveAddresses(async, r, NonExistentHost);
            Assert.Equal(DnsResponseCode.NxDomain, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveMx_KnownName_ReturnsRecords(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<MxRecord> result = await ResolveMx(async, r, TestMxHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (MxRecord rec in result.Records)
            {
                Assert.False(string.IsNullOrEmpty(rec.Exchange));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveTxt_KnownName_ReturnsRecords(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<TxtRecord> result = await ResolveTxt(async, r, TestTxtHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (TxtRecord rec in result.Records)
            {
                Assert.NotEmpty(rec.Values);
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveCName_KnownName_ReturnsRecord(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<CNameRecord> result = await ResolveCName(async, r, TestCNameHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            // CNAME may or may not exist for the target; at minimum the call should succeed.
            if (result.Records.Count > 0)
            {
                Assert.False(string.IsNullOrEmpty(result.Records[0].CanonicalName));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolveNs_KnownName_ReturnsRecords(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<NsRecord> result = await ResolveNs(async, r, TestNsHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (NsRecord rec in result.Records)
            {
                Assert.False(string.IsNullOrEmpty(rec.Name));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolvePtr_ByIPAddress_ReturnsRecord(bool async)
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<PtrRecord> result = await ResolvePtr(async, r, IPAddress.Parse("8.8.8.8"));
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            Assert.False(string.IsNullOrEmpty(result.Records[0].Name));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task Static_Dns_ResolveAddresses_Works(bool async)
        {
            DnsResult<AddressRecord> result = await Static_ResolveAddresses(async, TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task DnsResolver_CustomServer_Port53_Works(bool async)
        {
            IPAddress? dnsAddress = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(ni => ni.GetIPProperties().DnsAddresses)
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            if (dnsAddress is null)
            {
                // No IPv4 DNS server is configured on this machine; nothing to validate.
                return;
            }

            DnsResolverOptions opts = new DnsResolverOptions
            {
                Servers = { new IPEndPoint(dnsAddress, 53) }
            };
            using DnsResolver r = new DnsResolver(opts);
            DnsResult<AddressRecord> result = await ResolveAddresses(async, r, TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void DnsResolver_CustomServer_NonStandardPort_ThrowsPlatformNotSupported()
        {
            // DnsQueryEx only supports custom DNS servers on the standard port 53, so the
            // resolver rejects any other port when it is constructed.
            DnsResolverOptions opts = new DnsResolverOptions
            {
                Servers = { new IPEndPoint(IPAddress.Loopback, 5353) }
            };
            Assert.Throws<PlatformNotSupportedException>(() => new DnsResolver(opts));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void DnsResolver_CustomServers_MixedAddressFamilies_ThrowsArgumentException()
        {
            // DnsQueryEx encodes a single address family for the whole server list, so the
            // resolver rejects mixed IPv4/IPv6 server lists when it is constructed.
            DnsResolverOptions opts = new DnsResolverOptions
            {
                Servers =
                {
                    new IPEndPoint(IPAddress.Loopback, 53),
                    new IPEndPoint(IPAddress.IPv6Loopback, 53),
                }
            };
            Assert.Throws<ArgumentException>(() => new DnsResolver(opts));
        }

        // ---- Reverse-arpa name building (covers both IPv4 and IPv6 paths used by ResolvePtr(IPAddress)) ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        [OuterLoop]
        public async Task ResolvePtr_IPv6Address_DoesNotThrow(bool async)
        {
            using DnsResolver r = new DnsResolver();
            // Google public DNS IPv6 — call shouldn't throw, even if no PTR record exists.
            DnsResult<PtrRecord> result = await ResolvePtr(async, r, IPAddress.Parse("2001:4860:4860::8888"));
            Assert.True(result.ResponseCode == DnsResponseCode.NoError || result.ResponseCode == DnsResponseCode.NxDomain);
        }
    }
}
