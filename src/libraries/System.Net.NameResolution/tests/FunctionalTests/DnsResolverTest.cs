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
            await Assert.ThrowsAsync<ArgumentNullException>(() => Dns.ResolvePtrAsync((IPAddress)null!));
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

        [Fact]
        public async Task DnsResolver_PreCanceledToken_ReturnsCanceled()
        {
            using DnsResolver r = new DnsResolver();
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => r.ResolveAddressesAsync(TestHost, cts.Token));
        }

        // ---- Windows network tests (require outbound DNS) ----

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveAddresses_KnownName_ReturnsRecords()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await r.ResolveAddressesAsync(TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (AddressRecord rec in result.Records)
            {
                Assert.NotNull(rec.Address);
                Assert.True(rec.Address.AddressFamily == AddressFamily.InterNetwork || rec.Address.AddressFamily == AddressFamily.InterNetworkV6);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveAddresses_IPv4Only_ReturnsOnlyIPv4()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await r.ResolveAddressesAsync(TestHost, AddressFamily.InterNetwork);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            foreach (AddressRecord rec in result.Records)
            {
                Assert.Equal(AddressFamily.InterNetwork, rec.Address.AddressFamily);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveAddresses_NonExistent_ReturnsNxDomain()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<AddressRecord> result = await r.ResolveAddressesAsync(NonExistentHost);
            Assert.Equal(DnsResponseCode.NxDomain, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveMx_KnownName_ReturnsRecords()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<MxRecord> result = await r.ResolveMxAsync(TestMxHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (MxRecord rec in result.Records)
            {
                Assert.False(string.IsNullOrEmpty(rec.Exchange));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveTxt_KnownName_ReturnsRecords()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<TxtRecord> result = await r.ResolveTxtAsync(TestTxtHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (TxtRecord rec in result.Records)
            {
                Assert.NotEmpty(rec.Values);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveCName_KnownName_ReturnsRecord()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<CNameRecord> result = await r.ResolveCNameAsync(TestCNameHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            // CNAME may or may not exist for the target; at minimum the call should succeed.
            if (result.Records.Count > 0)
            {
                Assert.False(string.IsNullOrEmpty(result.Records[0].CanonicalName));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolveNs_KnownName_ReturnsRecords()
        {
            using DnsResolver r = new DnsResolver();
            DnsResult<NsRecord> result = await r.ResolveNsAsync(TestNsHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            foreach (NsRecord rec in result.Records)
            {
                Assert.False(string.IsNullOrEmpty(rec.Name));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolvePtr_ByIPAddress_ReturnsRecord()
        {
            DnsResult<PtrRecord> result = await Dns.ResolvePtrAsync(IPAddress.Parse("8.8.8.8"));
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
            Assert.False(string.IsNullOrEmpty(result.Records[0].Name));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task Static_Dns_ResolveAddressesAsync_Works()
        {
            DnsResult<AddressRecord> result = await Dns.ResolveAddressesAsync(TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task DnsResolver_CustomServer_Port53_Works()
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
            DnsResult<AddressRecord> result = await r.ResolveAddressesAsync(TestHost);
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.NotEmpty(result.Records);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public async Task DnsResolver_CustomServer_NonStandardPort_ThrowsPlatformNotSupported()
        {
            // DnsQueryEx only supports custom DNS servers on the standard port 53.
            DnsResolverOptions opts = new DnsResolverOptions
            {
                Servers = { new IPEndPoint(IPAddress.Loopback, 5353) }
            };
            using DnsResolver r = new DnsResolver(opts);
            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => r.ResolveAddressesAsync(TestHost));
            Assert.Throws<PlatformNotSupportedException>(() => r.ResolveAddresses(TestHost));
        }

        // ---- Reverse-arpa name building (covers both IPv4 and IPv6 paths used by ResolvePtr(IPAddress)) ----

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [OuterLoop]
        public async Task ResolvePtr_IPv6Address_DoesNotThrow()
        {
            // Google public DNS IPv6 — call shouldn't throw, even if no PTR record exists.
            DnsResult<PtrRecord> result = await Dns.ResolvePtrAsync(IPAddress.Parse("2001:4860:4860::8888"));
            Assert.True(result.ResponseCode == DnsResponseCode.NoError || result.ResponseCode == DnsResponseCode.NxDomain);
        }
    }
}
