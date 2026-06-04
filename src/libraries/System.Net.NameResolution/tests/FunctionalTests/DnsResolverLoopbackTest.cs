// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.NameResolution.Tests
{
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
    [Collection(nameof(DnsLoopbackTestCollection))]
    public class DnsResolverLoopbackTest
    {
        public static bool IsSupported => PlatformDetection.IsWindows;

        private static DnsResolver CreateResolver(LoopbackDnsServer server)
            => new DnsResolver(new DnsResolverOptions { Servers = { server.EndPoint } });

        // Generates a unique multi-label name so neither the OS resolver cache nor a
        // previous test run can satisfy the query without reaching the loopback server.
        private static string UniqueName(string label) => $"{label}-{Guid.NewGuid():N}.test";

        // ---- Address resolution ----

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_Unspecified_ReturnsBothV4AndV6()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("host");
            server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, a => a.Address.ToString() == "10.0.0.1");
            Assert.Contains(result.Records, a => a.Address.ToString() == "fd00::1");
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_IPv4Only_ReturnsOnlyV4()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("v4");
            server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 2 }, ttl: 300));
            server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            // A succeeds, AAAA returns NXDOMAIN — overall is success because we got addresses.
            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.2", record.Address.ToString());
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_IPv6Only_ReturnsOnlyV6()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("v6");
            server.AddResponse(name, DnsRecordType.A, b => b.ResponseCode(DnsResponseCode.NxDomain));
            server.AddResponse(name, DnsRecordType.AAAA, b => b.Answer(IPAddress.Parse("fd00::1").GetAddressBytes(), ttl: 60));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("fd00::1", record.Address.ToString());
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_AddressFamilyV4_QueriesOnlyA()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("famv4");
            server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 192, 0, 2, 7 }, ttl: 200));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name, AddressFamily.InterNetwork);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("192.0.2.7", record.Address.ToString());
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_HasTtl()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("ttl");
            server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 1 }, ttl: 120));
            server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            AddressRecord record = Assert.Single(result.Records);
            // The TTL we sent (120s) should be preserved (custom-server queries bypass the OS cache).
            Assert.True(record.Ttl > TimeSpan.Zero && record.Ttl <= TimeSpan.FromSeconds(120),
                $"Unexpected TTL: {record.Ttl}");
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_Nxdomain_ReturnsNxDomain()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("missing");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 120);
            server.AddResponse(name, DnsRecordType.A, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));
            server.AddResponse(name, DnsRecordType.AAAA, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 120));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NxDomain, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_NoData_ReturnsNoErrorWithEmptyRecords()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("nodata");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 30);
            server.AddResponse(name, DnsRecordType.A, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));
            server.AddResponse(name, DnsRecordType.AAAA, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));

            // The name exists but has no A/AAAA records → NODATA for both queries.
            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Empty(result.Records);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_NoData_And_Nxdomain_AreDistinguishable()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string nodataName = UniqueName("nodata");
            byte[] soaRdata = DnsResponseBuilder.BuildSoaRdata("test", 30);
            server.AddResponse(nodataName, DnsRecordType.A, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));
            server.AddResponse(nodataName, DnsRecordType.AAAA, b => b
                .Authority("test", DnsRecordType.SOA, soaRdata, ttl: 30));

            string missingName = UniqueName("missing");
            byte[] nxSoaRdata = DnsResponseBuilder.BuildSoaRdata("test", 120);
            server.AddResponse(missingName, DnsRecordType.A, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, nxSoaRdata, ttl: 120));
            server.AddResponse(missingName, DnsRecordType.AAAA, b => b
                .ResponseCode(DnsResponseCode.NxDomain)
                .Authority("test", DnsRecordType.SOA, nxSoaRdata, ttl: 120));

            DnsResult<AddressRecord> nodata = await resolver.ResolveAddressesAsync(nodataName);
            Assert.Equal(DnsResponseCode.NoError, nodata.ResponseCode);
            Assert.Empty(nodata.Records);

            DnsResult<AddressRecord> nxdomain = await resolver.ResolveAddressesAsync(missingName);
            Assert.Equal(DnsResponseCode.NxDomain, nxdomain.ResponseCode);
            Assert.Empty(nxdomain.Records);

            Assert.NotEqual(nodata.ResponseCode, nxdomain.ResponseCode);
        }

        // ---- SRV ----

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveSrv_ReturnsRecords()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = $"_http._tcp.{UniqueName("svc")}";
            server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120));

            DnsResult<SrvRecord> result = await resolver.ResolveSrvAsync(name);

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

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveSrv_IncludesAdditionalAddresses()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = $"_http._tcp.{UniqueName("svc")}";
            server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 8080, "node1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildSrvRdata(20, 50, 8081, "node2.test"), ttl: 120)
                .Additional("node1.test", DnsRecordType.A, new byte[] { 10, 0, 0, 10 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.A, new byte[] { 10, 0, 0, 11 }, ttl: 120)
                .Additional("node2.test", DnsRecordType.AAAA, IPAddress.Parse("fd00::11").GetAddressBytes(), ttl: 120));

            DnsResult<SrvRecord> result = await resolver.ResolveSrvAsync(name);

            SrvRecord s1 = Assert.Single(result.Records, s => s.Target == "node1.test");
            Assert.NotNull(s1.Addresses);
            AddressRecord s1Addr = Assert.Single(s1.Addresses);
            Assert.Equal("10.0.0.10", s1Addr.Address.ToString());

            SrvRecord s2 = Assert.Single(result.Records, s => s.Target == "node2.test");
            Assert.NotNull(s2.Addresses);
            Assert.Equal(2, s2.Addresses.Count);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveSrv_NoAdditionalAddresses()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = $"_noadd._tcp.{UniqueName("svc")}";
            server.AddResponse(name, DnsRecordType.SRV, b => b
                .Answer(DnsResponseBuilder.BuildSrvRdata(10, 100, 9090, "noaddr.test"), ttl: 60));

            DnsResult<SrvRecord> result = await resolver.ResolveSrvAsync(name);

            SrvRecord record = Assert.Single(result.Records);
            Assert.Equal("noaddr.test", record.Target);
            Assert.Empty(record.Addresses);
        }

        // ---- MX / TXT / CNAME / PTR / NS ----

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveMx_ReturnsRecords()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("mx");
            server.AddResponse(name, DnsRecordType.MX, b => b
                .Answer(DnsResponseBuilder.BuildMxRdata(10, "mail1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildMxRdata(20, "mail2.test"), ttl: 120));

            DnsResult<MxRecord> result = await resolver.ResolveMxAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);

            MxRecord m1 = Assert.Single(result.Records, m => m.Exchange == "mail1.test");
            Assert.Equal((ushort)10, m1.Preference);
            Assert.Single(result.Records, m => m.Exchange == "mail2.test" && m.Preference == 20);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveTxt_ReturnsValues()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("txt");
            server.AddResponse(name, DnsRecordType.TXT, b => b
                .Answer(DnsResponseBuilder.BuildTxtRdata("v=spf1 -all"), ttl: 120)
                .Answer(DnsResponseBuilder.BuildTxtRdata("part1", "part2"), ttl: 120));

            DnsResult<TxtRecord> result = await resolver.ResolveTxtAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, t => t.Values.Count == 1 && t.Values[0] == "v=spf1 -all");
            Assert.Contains(result.Records, t => t.Values.Count == 2 && t.Values[0] == "part1" && t.Values[1] == "part2");
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveCName_ReturnsCanonicalName()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("alias");
            server.AddResponse(name, DnsRecordType.CNAME, b => b
                .Answer(DnsResponseBuilder.EncodeName("canonical.test"), ttl: 120));

            DnsResult<CNameRecord> result = await resolver.ResolveCNameAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            CNameRecord record = Assert.Single(result.Records);
            Assert.Equal("canonical.test", record.CanonicalName);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolvePtr_ReturnsName()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = $"1.0.0.10.in-addr.{UniqueName("arpa")}";
            server.AddResponse(name, DnsRecordType.PTR, b => b
                .Answer(DnsResponseBuilder.EncodeName("host.test"), ttl: 120));

            DnsResult<PtrRecord> result = await resolver.ResolvePtrAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            PtrRecord record = Assert.Single(result.Records);
            Assert.Equal("host.test", record.Name);
        }

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveNs_ReturnsRecords()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("ns");
            server.AddResponse(name, DnsRecordType.NS, b => b
                .Answer(DnsResponseBuilder.EncodeName("ns1.test"), ttl: 120)
                .Answer(DnsResponseBuilder.EncodeName("ns2.test"), ttl: 120));

            DnsResult<NsRecord> result = await resolver.ResolveNsAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            Assert.Equal(2, result.Records.Count);
            Assert.Contains(result.Records, n => n.Name == "ns1.test");
            Assert.Contains(result.Records, n => n.Name == "ns2.test");
        }

        // ---- Custom server endpoint handling ----

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task CustomServer_DefaultPortZero_IsAccepted()
        {
            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            // Port 0 means "use the default DNS port"; DnsQueryEx always queries port 53.
            using DnsResolver resolver = new DnsResolver(new DnsResolverOptions
            {
                Servers = { new IPEndPoint(IPAddress.Loopback, 0) }
            });

            string name = UniqueName("port0");
            server.AddResponse(name, DnsRecordType.A, b => b.Answer(new byte[] { 10, 0, 0, 5 }, ttl: 120));
            server.AddResponse(name, DnsRecordType.AAAA, b => b.ResponseCode(DnsResponseCode.NxDomain));

            DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(name);

            Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
            AddressRecord record = Assert.Single(result.Records);
            Assert.Equal("10.0.0.5", record.Address.ToString());
        }

        // ---- Cancellation while a query is in flight ----

        [ConditionalFact(nameof(IsSupported))]
        [OuterLoop("Binds the loopback DNS port 53 and issues real DnsQueryEx calls.")]
        public async Task ResolveAddresses_CancellationInFlight_Throws()
        {
            using SemaphoreSlim queryReceived = new(0, 1);
            using ManualResetEventSlim serverCanContinue = new(false);
            using CancellationTokenSource cts = new();

            await using LoopbackDnsServer server = LoopbackDnsServer.Start();
            using DnsResolver resolver = CreateResolver(server);

            string name = UniqueName("cancel");
            server.AddRawResponse(name, DnsRecordType.A, queryId =>
            {
                queryReceived.Release();
                // Hold the response until the test cancels and signals us to continue.
                serverCanContinue.Wait(TimeSpan.FromSeconds(30));
                return DnsResponseBuilder.For(queryId, DnsResponseBuilder.EncodeName(name), DnsRecordType.A)
                    .Answer(new byte[] { 10, 0, 0, 1 }, ttl: 60)
                    .Build();
            });

            // Query a single family so exactly one (blocked) UDP query is issued.
            Task resolveTask = resolver.ResolveAddressesAsync(name, AddressFamily.InterNetwork, cts.Token);

            Assert.True(await queryReceived.WaitAsync(TimeSpan.FromSeconds(10)));
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resolveTask);

            serverCanContinue.Set();
        }
    }

    // The loopback DNS server binds the single machine-wide port 53, so all tests that
    // use it must run sequentially. Placing them in this collection disables parallel
    // execution between the test classes that opt into it.
    [CollectionDefinition(nameof(DnsLoopbackTestCollection), DisableParallelization = true)]
    public sealed class DnsLoopbackTestCollection { }
}
