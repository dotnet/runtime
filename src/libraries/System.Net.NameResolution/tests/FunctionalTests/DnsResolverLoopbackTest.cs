// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.NameResolution.Tests
{
#if WINDOWS
    public sealed class WindowsLoopbackServer : IAsyncDisposable
    {
        private LoopbackDnsServer? _server;

        // Started lazily on first access (from within a test invocation) rather than in
        // the constructor. LoopbackDnsServer.Start() throws SkipTestException when port 53
        // is unavailable; that is only honored by the ConditionalFact/ConditionalTheory
        // runner, which wraps the test class constructor where Server is first accessed.
        // Starting in this fixture constructor would instead surface as a hard failure.
        internal LoopbackDnsServer Server => _server ??= LoopbackDnsServer.Start(53);

        public async ValueTask DisposeAsync()
        {
            if (_server is not null)
            {
                await _server.DisposeAsync();
            }
        }
    }

    // Deterministic DnsResolver tests driven by an in-process loopback DNS server.
    //
    // On Windows, DnsQueryEx only ever contacts custom DNS servers on the standard
    // port 53 (the sockaddr port field must be 0), so the loopback server binds port 53.
    // When that port is unavailable (e.g. a local DNS service is already running) the
    // server's Start() throws SkipTestException; the tests therefore use
    // ConditionalFact/ConditionalTheory so that skip is honored rather than surfacing as
    // a failure. Because the single machine-wide port 53 is shared, these tests run
    // sequentially (see the collection).
    //
    // Each behavioral test is parameterized over the synchronous and asynchronous APIs
    // so both code paths are exercised against the same loopback responses.
    //
    // These tests cover the record-parsing and response-handling behavior that the
    // OuterLoop tests in DnsResolverTest.cs cannot exercise deterministically.
    [Collection(nameof(DisableParallelization))]
    public class DnsResolverLoopbackTest : IClassFixture<WindowsLoopbackServer>
    {
        public DnsResolverLoopbackTest(WindowsLoopbackServer fixture)
        {
            _server = fixture.Server;
            _server.ClearResponses();
        }
#else
    // for all other platforms, bind an ephemeral port and use that for the loopback server. In
    // these cases, we can run in parallel
    public class DnsResolverLoopbackTest : IAsyncDisposable
    {
        public DnsResolverLoopbackTest()
        {
            _server = LoopbackDnsServer.Start();
        }

        public async ValueTask DisposeAsync()
        {
            await _server.DisposeAsync();
        }
#endif
        private static DnsResolver CreateResolver(LoopbackDnsServer server)
            => new DnsResolver(new DnsResolverOptions { Servers = { server.EndPoint } });

        // Generates a unique multi-label name so neither the OS resolver cache nor a
        // previous test run can satisfy the query without reaching the loopback server.
        private static string UniqueName(string label) => $"{label}-{Guid.NewGuid():N}.test";

        LoopbackDnsServer _server;
        DnsResolver? _resolver;

        internal DnsResolver Resolver => _resolver ??= CreateResolver(_server);

        // ---- Sync/async dispatch helpers ----
        // The synchronous overloads execute inline on the calling thread; the results
        // are wrapped in a completed Task so each test can await a single helper.

        private static async Task<DnsResult<AddressRecord>> ResolveAddresses(bool async, DnsResolver resolver, string name, AddressFamily addressFamily = AddressFamily.Unspecified)
            => async ? await resolver.ResolveAddressesAsync(name, addressFamily) : resolver.ResolveAddresses(name, addressFamily);

        private static async Task<DnsResult<SrvRecord>> ResolveSrv(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveSrvAsync(name) : resolver.ResolveSrv(name);

        private static async Task<DnsResult<MxRecord>> ResolveMx(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveMxAsync(name) : resolver.ResolveMx(name);

        private static async Task<DnsResult<TxtRecord>> ResolveTxt(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveTxtAsync(name) : resolver.ResolveTxt(name);

        private static async Task<DnsResult<CNameRecord>> ResolveCName(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveCNameAsync(name) : resolver.ResolveCName(name);

        private static async Task<DnsResult<PtrRecord>> ResolvePtr(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolvePtrAsync(name) : resolver.ResolvePtr(name);

        private static async Task<DnsResult<NsRecord>> ResolveNs(bool async, DnsResolver resolver, string name)
            => async ? await resolver.ResolveNsAsync(name) : resolver.ResolveNs(name);

        // ---- Address resolution ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_Unspecified_ReturnsBothV4AndV6(bool async)
        {
            string name = UniqueName("host");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, a => a.Address.ToString() == "10.0.0.1");
            Assert.Contains(result.Records, a => a.Address.ToString() == "fd00::1");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_IPv4Only_ReturnsOnlyV4(bool async)
        {
            string name = UniqueName("v4");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 2 }, ttl: 300));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            // A succeeds, AAAA returns NXDOMAIN — overall is success because we got addresses.
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.2", record.Address.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_IPv6Only_ReturnsOnlyV6(bool async)
        {
            string name = UniqueName("v6");
            _server.AddResponse(name, DnsRecordType.A, b => b.ResponseCode(DnsResponseCode.NxDomain));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("fd00::1", record.Address.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_AddressFamilyV4_QueriesOnlyA(bool async)
        {
            string name = UniqueName("famv4");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 192, 0, 2, 7 }, ttl: 200));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name, AddressFamily.InterNetwork);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("192.0.2.7", record.Address.ToString());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_HasTtl(bool async)
        {
            string name = UniqueName("ttl");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            AddressRecord record = Assert.Single(result.Records);
            // The TTL we sent (120s) should be preserved (custom-server queries bypass the OS cache).
            Assert.True(record.Ttl > TimeSpan.Zero && record.Ttl <= TimeSpan.FromSeconds(120),
                $"Unexpected TTL: {record.Ttl}");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_Nxdomain_ReturnsNxDomain(bool async)
        {
            string name = UniqueName("missing");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 120);
            _server.AddResponse(name, DnsRecordType.A, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NxDomain, result.ResponseCode);
            Assert.Empty(result.Records);
            // DnsQueryEx does not surface the authority-section SOA for negative responses
            // (it returns no records at all), so the negative-cache TTL is unavailable on
            // Windows and reported as zero. See DnsResult.NegativeCacheTtl remarks.
            Assert.Equal(TimeSpan.Zero, result.NegativeCacheTtl);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_NoData_ReturnsNoErrorWithEmptyRecords(bool async)
        {
            string name = UniqueName("nodata");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 30);
            _server.AddResponse(name, DnsRecordType.A, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));

            // The name exists but has no A/AAAA records → NODATA for both queries.
            DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Empty(result.Records);
            // DnsQueryEx does not surface the authority-section SOA for NODATA responses
            // (it returns no records at all), so the negative-cache TTL is unavailable on
            // Windows and reported as zero. See DnsResult.NegativeCacheTtl remarks.
            Assert.Equal(TimeSpan.Zero, result.NegativeCacheTtl);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_NoData_And_Nxdomain_AreDistinguishable(bool async)
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

            DnsResult<AddressRecord> nodata = await ResolveAddresses(async, Resolver, nodataName);
            Assert.Equal(DnsResponseCode.NoError, nodata.ResponseCode);
            Assert.Empty(nodata.Records);

            DnsResult<AddressRecord> nxdomain = await ResolveAddresses(async, Resolver, missingName);
            Assert.Equal(DnsResponseCode.NxDomain, nxdomain.ResponseCode);
            Assert.Empty(nxdomain.Records);

            Assert.NotEqual(nodata.ResponseCode, nxdomain.ResponseCode);
        }

        // ---- SRV ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveSrv_ReturnsRecords(bool async)
        {
            string name = $"_http._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120));

            DnsResult<SrvRecord> result = await ResolveSrv(async, Resolver, name);

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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveSrv_IncludesAdditionalAddresses(bool async)
        {
            string name = $"_http._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120)
                .Additional("node1.test", DnsRecordType.A, new byte[] { 10, 0, 0, 10 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.A, new byte[] { 10, 0, 0, 11 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.AAAA, IPAddress.Parse("fd00::11").GetAddressBytes(), ttl: 120));

            DnsResult<SrvRecord> result = await ResolveSrv(async, Resolver, name);

            SrvRecord s1 = Assert.Single(result.Records, s => s.Target == "node1.test");
            Assert.NotNull(s1.Addresses);
            AddressRecord s1Addr = Assert.Single(s1.Addresses);
            Assert.Equal("10.0.0.10", s1Addr.Address.ToString());

            SrvRecord s2 = Assert.Single(result.Records, s => s.Target == "node2.test");
            Assert.NotNull(s2.Addresses);
            Assert.Equal(2, s2.Addresses.Count);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveSrv_NoAdditionalAddresses(bool async)
        {
            string name = $"_noadd._tcp.{UniqueName("svc")}";
            _server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 9090, "noaddr.test"), ttl: 60));

            DnsResult<SrvRecord> result = await ResolveSrv(async, Resolver, name);

            SrvRecord record = Assert.Single(result.Records);
            Assert.Equal("noaddr.test", record.Target);
            Assert.Empty(record.Addresses);
        }

        // ---- MX / TXT / CNAME / PTR / NS ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveMx_ReturnsRecords(bool async)
        {
            string name = UniqueName("mx");
            _server.AddResponse(name, DnsRecordType.MX, b => b
                .Answer(DnsResponseBuilder.BuildMxRdata(10, "mail1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildMxRdata(20, "mail2.test"), ttl: 120));

            DnsResult<MxRecord> result = await ResolveMx(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);

            MxRecord m1 = Assert.Single(result.Records, m => m.Exchange == "mail1.test");
            Assert.Equal((ushort)10, m1.Preference);
            Assert.Single(result.Records, m => m.Exchange == "mail2.test" && m.Preference == 20);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveTxt_ReturnsValues(bool async)
        {
            string name = UniqueName("txt");
            _server.AddResponse(name, DnsRecordType.TXT, b => b
                .Answer(DnsResponseBuilder.BuildTxtRdata("v=spf1 -all"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildTxtRdata("part1", "part2"), ttl: 120));

            DnsResult<TxtRecord> result = await ResolveTxt(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, t => t.Values.Count == 1 && t.Values[0] == "v=spf1 -all");
            Assert.Contains(result.Records, t => t.Values.Count == 2 && t.Values[0] == "part1" && t.Values[1] == "part2");
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveCName_ReturnsCanonicalName(bool async)
        {
            string name = UniqueName("alias");
            _server.AddResponse(name, DnsRecordType.CNAME, b => b
                .Answer(DnsResponseBuilder.EncodeName("canonical.test"), ttl: 120));

            DnsResult<CNameRecord> result = await ResolveCName(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            CNameRecord record = Assert.Single(result.Records);
            Assert.Equal("canonical.test", record.CanonicalName);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolvePtr_ReturnsName(bool async)
        {
            string name = $"1.0.0.10.in-addr.{UniqueName("arpa")}";
            _server.AddResponse(name, DnsRecordType.PTR, b => b
                .Answer(DnsResponseBuilder.EncodeName("host.test"), ttl: 120));

            DnsResult<PtrRecord> result = await ResolvePtr(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            PtrRecord record = Assert.Single(result.Records);
            Assert.Equal("host.test", record.Name);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveNs_ReturnsRecords(bool async)
        {
            string name = UniqueName("ns");
            _server.AddResponse(name, DnsRecordType.NS, b => b
                .Answer(DnsResponseBuilder.EncodeName("ns1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.EncodeName("ns2.test"), ttl: 120));

            DnsResult<NsRecord> result = await ResolveNs(async, Resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, n => n.Name == "ns1.test");
            Assert.Contains(result.Records, n => n.Name == "ns2.test");
        }

        // ---- Custom server endpoint handling ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CustomServer_DefaultPortZero_IsAccepted(bool async)
        {
            // Port 0 means "use the default DNS port"; DnsQueryEx always queries port 53.
            using DnsResolver resolver = new DnsResolver(new DnsResolverOptions
            {
                Servers = { new IPEndPoint(IPAddress.Loopback, 0) }
            });

            string name = UniqueName("port0");
            _server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 5 }, ttl: 120));
            _server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await ResolveAddresses(async, resolver, name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.5", record.Address.ToString());
        }

        // ---- Cancellation while a query is in flight ----

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
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

        // ---- Telemetry ----

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ResolveAddresses_RecordsDurationMetric_CoversQueryTime(bool async)
        {
            TimeSpan delay = TimeSpan.FromMilliseconds(250);
            string name = UniqueName("metrics");
            _server.AddRawResponse(name, DnsRecordType.A, queryId =>
            {
                Thread.Sleep(delay);
                return DnsResponseBuilder.For(queryId, DnsResponseBuilder.EncodeName(name), DnsRecordType.A)
                    .Answer(new byte[] { 10, 0, 0, 9 }, ttl: 120)
                    .Build();
            });

            List<Measurement<double>> measurements = new();
            using (MeterListener listener = new())
            {
                listener.InstrumentPublished = (instrument, l) =>
                {
                    if (instrument.Meter.Name == "System.Net.NameResolution" && instrument.Name == "dns.lookup.duration")
                    {
                        l.EnableMeasurementEvents(instrument);
                    }
                };
                listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
                {
                    lock (measurements)
                    {
                        measurements.Add(new Measurement<double>(measurement, tags));
                    }
                });
                listener.Start();

                // A single A query so exactly one lookup is measured.
                DnsResult<AddressRecord> result = await ResolveAddresses(async, Resolver, name, AddressFamily.InterNetwork);
                Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            }

            Measurement<double>[] matching = measurements
                .Where(m => m.Tags.ToArray().Any(t => t.Key == "dns.question.name" && (string?)t.Value == name))
                .ToArray();

            Measurement<double> recorded = Assert.Single(matching);

            // The measured duration must span the actual query, and so must be at least
            // the server's artificial response delay - the lookup cannot legitimately
            // complete before the server replies. Regression: on the synchronous path
            // telemetry used to start only after the PAL had already begun executing the
            // query, so the recorded duration was shorter than the server delay.
            Assert.True(recorded.Value >= delay.TotalSeconds, $"Expected a lookup duration of at least {delay.TotalSeconds:0.###}s but got {recorded.Value:0.###}s.");
        }
    }
}
