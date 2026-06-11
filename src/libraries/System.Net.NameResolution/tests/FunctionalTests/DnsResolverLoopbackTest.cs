// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class WindowsLoopbackServer : IAsyncDisposable
    {
        private LoopbackDnsServer _server;

        public WindowsLoopbackServer()
        {
            _server = LoopbackDnsServer.Start();
        }

        internal LoopbackDnsServer Server => _server;

        public async ValueTask DisposeAsync()
        {
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
    }

    // Deterministic DnsResolver tests driven by an in-process loopback DNS server.
    //
    // On Windows, DnsQueryEx only ever contacts custom DNS servers on the standard
    // port 53 (the sockaddr port field must be 0), so the loopback server binds port 53.
    // When that port is unavailable (e.g. a local DNS service is already running) the
    // tests are skipped via SkipTestException rather than failing. Because the single
    // machine-wide port 53 is shared, these tests run sequentially (see the collection).
    //
    // These tests cover the record-parsing and response-handling behavior that the
    // OuterLoop tests in DnsResolverTest.cs cannot exercise deterministically.
    [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
    [Collection(nameof(DisableParallelization))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public class DnsResolverLoopbackTest : IClassFixture<WindowsLoopbackServer>
    {
        private static DnsResolver CreateResolver(LoopbackDnsServer server)
            => new DnsResolver(new DnsResolverOptions { Servers = { server.EndPoint } });

        // Generates a unique multi-label name so neither the OS resolver cache nor a
        // previous test run can satisfy the query without reaching the loopback server.
        private static string UniqueName(string label) => $"{label}-{Guid.NewGuid():N}.test";

        LoopbackDnsServer _server;
        DnsResolver? _resolver;

        public DnsResolverLoopbackTest(WindowsLoopbackServer fixture)
        {
            _server = fixture.Server;
            _server.ClearResponses();
        }

        internal DnsResolver Resolver => _resolver ??= CreateResolver(_server);

        // ---- Address resolution ----

        [Fact]
        public async Task ResolveAddresses_Unspecified_ReturnsBothV4AndV6()
        {
            string name = UniqueName("host");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, a => a.Address.ToString() == "10.0.0.1");
            Assert.Contains(result.Records, a => a.Address.ToString() == "fd00::1");
        }

        [Fact]
        public async Task ResolveAddresses_IPv4Only_ReturnsOnlyV4()
        {
            string name = UniqueName("v4");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 2 }, ttl: 300));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            // A succeeds, AAAA returns NXDOMAIN — overall is success because we got addresses.
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.2", record.Address.ToString());
        }

        [Fact]
        public async Task ResolveAddresses_IPv6Only_ReturnsOnlyV6()
        {
            string name = UniqueName("v6");
            _server.AddResponse(name, DnsRecordType.A, b => b.ResponseCode(DnsResponseCode.NxDomain));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("fd00::1", record.Address.ToString());
        }

        [Fact]
        public async Task ResolveAddresses_AddressFamilyV4_QueriesOnlyA()
        {
            string name = UniqueName("famv4");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 192, 0, 2, 7 }, ttl: 200));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name, AddressFamily.InterNetwork);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("192.0.2.7", record.Address.ToString());
        }

        [Fact]
        public async Task ResolveAddresses_HasTtl()
        {
            string name = UniqueName("ttl");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            AddressRecord record = Assert.Single(result.Records);
            // The TTL we sent (120s) should be preserved (custom-server queries bypass the OS cache).
            Assert.True(record.Ttl > TimeSpan.Zero && record.Ttl <= TimeSpan.FromSeconds(120),
                $"Unexpected TTL: {record.Ttl}");
        }

        [Fact]
        public async Task ResolveAddresses_Nxdomain_ReturnsNxDomain()
        {
            string name = UniqueName("missing");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 120);
            _server.AddResponse(name, DnsRecordType.A, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));

            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NxDomain, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task ResolveAddresses_NoData_ReturnsNoErrorWithEmptyRecords()
        {
            string name = UniqueName("nodata");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 30);
            _server.AddResponse(name, DnsRecordType.A, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));

            // The name exists but has no A/AAAA records → NODATA for both queries.
            DnsResult<AddressRecord> result = await Resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task ResolveAddresses_NoData_And_Nxdomain_AreDistinguishable()
        {
            string nodataName = UniqueName("nodata");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 30);
            _server.AddResponse(nodataName, DnsRecordType.A, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));
            _server.AddResponse(nodataName, DnsRecordType.AAAA, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));

            string missingName = UniqueName("missing");
            byte[] nxSoaRdata = DnsResponseBuilder.BuildSoaRdata("test", 120);
            _server.AddResponse(missingName, DnsRecordType.A, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, nxSoaRdata, ttl: 120));
            _server.AddResponse(missingName, DnsRecordType.AAAA, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, nxSoaRdata, ttl: 120));

            DnsResult<AddressRecord> nodata = await Resolver.ResolveAddressesAsync(nodataName);
            Assert.Equal(DnsResponseCode.NoError, nodata.ResponseCode);
            Assert.Empty(nodata.Records);

            DnsResult<AddressRecord> nxdomain = await Resolver.ResolveAddressesAsync(missingName);
            Assert.Equal(DnsResponseCode.NxDomain, nxdomain.ResponseCode);
            Assert.Empty(nxdomain.Records);

            Assert.NotEqual(nodata.ResponseCode, nxdomain.ResponseCode);
        }

        // ---- SRV ----

        [Fact]
        public async Task ResolveSrv_ReturnsRecords()
        {
            string name = $"_http._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120));

            DnsResult<SrvRecord> result = await Resolver.ResolveSrvAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);

            SrvRecord s1 = Assert.Single(result.Records, s => s.Target == "node1.test");
            Assert.Equal((ushort)8080, s1.Port);
            Assert.Equal((ushort)10, s1.Priority);
            Assert.Equal((ushort)100, s1.Weight);

            SrvRecord s2 = Assert.Single(result.Records, s => s.Target == "node2.test");
            Assert.Equal((ushort)8081, s2.Port);
            Assert.Equal((ushort)20, s2.Priority);
        }

        [Fact]
        public async Task ResolveSrv_IncludesAdditionalAddresses()
        {
            string name = $"_http._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120)
                .Additional("node1.test", DnsRecordType.A, new byte[] { 10, 0, 0, 10 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.A, new byte[] { 10, 0, 0, 11 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.AAAA, IPAddress.Parse("fd00::11").GetAddressBytes(), ttl: 120));

            DnsResult<SrvRecord> result = await Resolver.ResolveSrvAsync(name);

            SrvRecord s1 = Assert.Single(result.Records, s => s.Target == "node1.test");
            Assert.NotNull(s1.Addresses);
            AddressRecord s1Addr = Assert.Single(s1.Addresses);
            Assert.Equal("10.0.0.10", s1Addr.Address.ToString());

            SrvRecord s2 = Assert.Single(result.Records, s => s.Target == "node2.test");
            Assert.NotNull(s2.Addresses);
            Assert.Equal(2, s2.Addresses.Count);
        }

        [Fact]
        public async Task ResolveSrv_NoAdditionalAddresses()
        {
            string name = $"_noadd._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 9090, "noaddr.test"), ttl: 60));

            DnsResult<SrvRecord> result = await Resolver.ResolveSrvAsync(name);

            SrvRecord record = Assert.Single(result.Records);
            Assert.Equal("noaddr.test", record.Target);
            Assert.Empty(record.Addresses);
        }

        // ---- MX / TXT / CNAME / PTR / NS ----

        [Fact]
        public async Task ResolveMx_ReturnsRecords()
        {
            string name = UniqueName("mx");
            _server.AddResponse(name, DnsRecordType.MX, b => b
                .Answer(DnsResponseBuilder.BuildMxRdata(10, "mail1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildMxRdata(20, "mail2.test"), ttl: 120));

            DnsResult<MxRecord> result = await Resolver.ResolveMxAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);

            MxRecord m1 = Assert.Single(result.Records, m => m.Exchange == "mail1.test");
            Assert.Equal((ushort)10, m1.Preference);
            Assert.Single(result.Records, m => m.Exchange == "mail2.test" && m.Preference == 20);
        }

        [Fact]
        public async Task ResolveTxt_ReturnsValues()
        {
            string name = UniqueName("txt");
            _server.AddResponse(name, DnsRecordType.TXT, b => b
                .Answer(DnsResponseBuilder.BuildTxtRdata("v=spf1 -all"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildTxtRdata("part1", "part2"), ttl: 120));

            DnsResult<TxtRecord> result = await Resolver.ResolveTxtAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, t => t.Values.Count == 1 && t.Values[0] == "v=spf1 -all");
            Assert.Contains(result.Records, t => t.Values.Count == 2 && t.Values[0] == "part1" && t.Values[1] == "part2");
        }

        [Fact]
        public async Task ResolveCName_ReturnsCanonicalName()
        {
            string name = UniqueName("alias");
            _server.AddResponse(name, DnsRecordType.CNAME, b => b
                .Answer(DnsResponseBuilder.EncodeName("canonical.test"), ttl: 120));

            DnsResult<CNameRecord> result = await Resolver.ResolveCNameAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            CNameRecord record = Assert.Single(result.Records);
            Assert.Equal("canonical.test", record.CanonicalName);
        }

        [Fact]
        public async Task ResolvePtr_ReturnsName()
        {
            string name = $"1.0.0.10.in-addr.{UniqueName("arpa")}";
            _server.AddResponse(name, DnsRecordType.PTR, b => b
                .Answer(DnsResponseBuilder.EncodeName("host.test"), ttl: 120));

            DnsResult<PtrRecord> result = await Resolver.ResolvePtrAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            PtrRecord record = Assert.Single(result.Records);
            Assert.Equal("host.test", record.Name);
        }

        [Fact]
        public async Task ResolveNs_ReturnsRecords()
        {
            string name = UniqueName("ns");
            _server.AddResponse(name, DnsRecordType.NS, b => b
                .Answer(DnsResponseBuilder.EncodeName("ns1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.EncodeName("ns2.test"), ttl: 120));

            DnsResult<NsRecord> result = await Resolver.ResolveNsAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, n => n.Name == "ns1.test");
            Assert.Contains(result.Records, n => n.Name == "ns2.test");
        }

        // ---- Custom server endpoint handling ----

        [Fact]
        public async Task CustomServer_DefaultPortZero_IsAccepted()
        {
            // Port 0 means "use the default DNS port"; DnsQueryEx always queries port 53.
            using DnsResolver resolver = new DnsResolver(new DnsResolverOptions
            {
                Servers = { new IPEndPoint(IPAddress.Loopback, 0) }
            });

            string name = UniqueName("port0");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 5 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.5", record.Address.ToString());
        }

        // ---- Cancellation while a query is in flight ----

        [Fact]
        public async Task ResolveAddresses_CancellationInFlight_Throws()
        {
            using SemaphoreSlim queryReceived = new(0, 1);
            using ManualResetEventSlim serverCanContinue = new(false);
            using CancellationTokenSource cts = new();

            string name = UniqueName("cancel");
            _server.AddRawResponse(name, DnsRecordType.A, queryId =>
            {
                queryReceived.Release();
                // Hold the response until the test cancels and signals us to continue.
                serverCanContinue.Wait(TimeSpan.FromSeconds(30));
                return DnsResponseBuilder.For(queryId, DnsResponseBuilder.EncodeName(name), DnsRecordType.A)
                    .Answer(new byte[] { 10, 0, 0, 1 }, ttl: 60)
                    .Build();
            });

            // Query a single family so exactly one (blocked) UDP query is issued.
            Task resolveTask = Resolver.ResolveAddressesAsync(name, AddressFamily.InterNetwork, cts.Token);

            Assert.True(await queryReceived.WaitAsync(TimeSpan.FromSeconds(10)));
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolveTask);

            serverCanContinue.Set();
        }
    }
}
