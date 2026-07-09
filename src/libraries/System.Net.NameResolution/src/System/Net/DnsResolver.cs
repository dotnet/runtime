// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    /// <summary>
    /// Resolves DNS records, optionally using a caller-specified set of DNS servers.
    /// </summary>
    /// <remarks>
    /// When constructed without options, or with empty <see cref="DnsResolverOptions.Servers"/>,
    /// the resolver uses the system-configured DNS servers.
    /// <para>
    /// Instances are thread-safe: a single <see cref="DnsResolver"/> may be shared across threads
    /// and used to issue multiple concurrent resolutions.
    /// </para>
    /// </remarks>
    public sealed partial class DnsResolver : IAsyncDisposable, IDisposable
    {
        private readonly IPEndPoint[] _servers;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResolver"/> class that uses the
        /// system-configured DNS servers.
        /// </summary>
        public DnsResolver() : this(new DnsResolverOptions()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsResolver"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options controlling how DNS resolution is performed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public DnsResolver(DnsResolverOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            // Capture a defensive snapshot of the configured servers. IPEndPoint is
            // mutable, so clone each entry to ensure later mutations of the options
            // instance (or the endpoints it holds) don't affect this resolver.
            IList<IPEndPoint> servers = options.Servers;
            _servers = new IPEndPoint[servers.Count];
            for (int i = 0; i < _servers.Length; i++)
            {
                IPEndPoint server = servers[i];
                if (server is null)
                {
                    throw new ArgumentException(SR.net_dns_servers_contains_null, $"{nameof(options)}.{nameof(DnsResolverOptions.Servers)}");
                }
                _servers[i] = new IPEndPoint(server.Address, server.Port);
            }

            // Let the platform reject any server configuration it cannot honor
            // (e.g. custom ports or mixed address families on Windows).
            DnsResolverPal.ValidateServers(_servers);
        }

        /// <summary>
        /// Resolves the IPv4 (A) and IPv6 (AAAA) addresses for the specified host name.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<AddressRecord> ResolveAddresses(string name)
            => ResolveAddresses(name, AddressFamily.Unspecified);

        /// <summary>
        /// Resolves the addresses of the specified family for the specified host name.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <param name="addressFamily">
        /// The address family to query. Use <see cref="AddressFamily.InterNetwork"/> for A records,
        /// <see cref="AddressFamily.InterNetworkV6"/> for AAAA records, or
        /// <see cref="AddressFamily.Unspecified"/> for both.
        /// </param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<AddressRecord> ResolveAddresses(string name, AddressFamily addressFamily)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<AddressRecord>> task = ResolveAddressesCore(async: false, name, addressFamily, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the service (SRV) records for the specified name.
        /// </summary>
        /// <param name="name">The name to resolve, typically in the form <c>_service._protocol.host</c>.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the SRV records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<SrvRecord> ResolveSrv(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<SrvRecord>> task = ResolveSrvCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the mail exchange (MX) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the MX records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<MxRecord> ResolveMx(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<MxRecord>> task = ResolveMxCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the text (TXT) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the TXT records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<TxtRecord> ResolveTxt(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<TxtRecord>> task = ResolveTxtCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the canonical name (CNAME) record for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the CNAME records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<CNameRecord> ResolveCName(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<CNameRecord>> task = ResolveCNameCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the pointer (PTR) records for the specified name, typically used for reverse DNS lookups.
        /// </summary>
        /// <param name="name">The name to resolve, typically a reverse-lookup name such as <c>4.3.2.1.in-addr.arpa</c>.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<PtrRecord> ResolvePtr(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<PtrRecord>> task = ResolvePtrCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the pointer (PTR) records for the specified IP address, performing a reverse DNS lookup.
        /// </summary>
        /// <param name="address">The IP address to perform a reverse lookup for.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<PtrRecord> ResolvePtr(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<PtrRecord>> task = ResolvePtrCore(async: false, BuildArpaName(address), default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Resolves the authoritative name server (NS) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the NS records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public DnsResult<NsRecord> ResolveNs(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<NsRecord>> task = ResolveNsCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously resolves the IPv4 (A) and IPv6 (AAAA) addresses for the specified host name.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, CancellationToken cancellationToken = default)
            => ResolveAddressesAsync(name, AddressFamily.Unspecified, cancellationToken);

        /// <summary>
        /// Asynchronously resolves the addresses of the specified family for the specified host name.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <param name="addressFamily">
        /// The address family to query. Use <see cref="AddressFamily.InterNetwork"/> for A records,
        /// <see cref="AddressFamily.InterNetworkV6"/> for AAAA records, or
        /// <see cref="AddressFamily.Unspecified"/> for both.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveAddressesCore(async: true, name, addressFamily, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the service (SRV) records for the specified name.
        /// </summary>
        /// <param name="name">The name to resolve, typically in the form <c>_service._protocol.host</c>.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the SRV records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<SrvRecord>> ResolveSrvAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveSrvCore(async: true, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the mail exchange (MX) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the MX records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<MxRecord>> ResolveMxAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveMxCore(async: true, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the text (TXT) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the TXT records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<TxtRecord>> ResolveTxtAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveTxtCore(async: true, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the canonical name (CNAME) record for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the CNAME records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<CNameRecord>> ResolveCNameAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveCNameCore(async: true, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the pointer (PTR) records for the specified name, typically used for reverse DNS lookups.
        /// </summary>
        /// <param name="name">The name to resolve, typically a reverse-lookup name such as <c>4.3.2.1.in-addr.arpa</c>.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<PtrRecord>> ResolvePtrAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolvePtrCore(async: true, name, cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the pointer (PTR) records for the specified IP address, performing a reverse DNS lookup.
        /// </summary>
        /// <param name="address">The IP address to perform a reverse lookup for.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<PtrRecord>> ResolvePtrAsync(IPAddress address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolvePtrCore(async: true, BuildArpaName(address), cancellationToken);
        }

        /// <summary>
        /// Asynchronously resolves the authoritative name server (NS) records for the specified name.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the NS records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The resolver has been disposed.</exception>
        public Task<DnsResult<NsRecord>> ResolveNsAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveNsCore(async: true, name, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose() => _disposed = true;

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }

        // ---- Resolve*Core methods ----
        //
        // These instance methods are the platform-agnostic seam between the public
        // API and the platform abstraction layer (DnsResolverPal). They issue the
        // underlying query through the PAL (synchronously or asynchronously per the
        // `async` flag) and wrap it with telemetry. When no diagnostics consumer is
        // enabled, the PAL task is returned directly so the common path stays
        // allocation-free and, on the synchronous path, completes inline. When
        // telemetry is enabled, the PAL call is deferred into ResolveWithTelemetry so
        // that the measurement starts before the query runs - on the synchronous path
        // the PAL would otherwise execute the entire query before telemetry began.

        private async Task<DnsResult<AddressRecord>> ResolveAddressesCore(bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
        {
            if (addressFamily == AddressFamily.Unspecified)
            {
                // if `async == true` then this runs both queries in parallel, otherwise it runs them sequentially (the synchronous path is expected to be rare and the async path is expected to be the common case).
                Task<DnsResult<AddressRecord>> aTask = DoResolve(async, name, AddressFamily.InterNetwork, cancellationToken);
                Task<DnsResult<AddressRecord>> aaaaTask = DoResolve(async, name, AddressFamily.InterNetworkV6, cancellationToken);

                await Task.WhenAll(aTask, aaaaTask).ConfigureAwait(false);
                DnsResult<AddressRecord> aRes = await aTask.ConfigureAwait(false);
                DnsResult<AddressRecord> aaaaRes = await aaaaTask.ConfigureAwait(false);
                return MergeAddressResults(aRes, aaaaRes);
            }

            return await DoResolve(async, name, addressFamily, cancellationToken).ConfigureAwait(false);

            Task<DnsResult<AddressRecord>> DoResolve(bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
                => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                    ? ResolveWithTelemetry(name, (servers: _servers, async, name, addressFamily, cancellationToken),
                        static s => DnsResolverPal.ResolveAddresses(s.servers, s.async, s.name, s.addressFamily, s.cancellationToken),
                        static r => MapAnswers(r, static a => a.Address.ToString()))
                    : DnsResolverPal.ResolveAddresses(_servers, async, name, addressFamily, cancellationToken);
        }

        private Task<DnsResult<SrvRecord>> ResolveSrvCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolveSrv(s.servers, s.async, s.name, s.cancellationToken),
                    static r => MapAnswers(r, static a => a.Target))
                : DnsResolverPal.ResolveSrv(_servers, async, name, cancellationToken);

        private Task<DnsResult<MxRecord>> ResolveMxCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolveMx(s.servers, s.async, s.name, s.cancellationToken),
                    static r => MapAnswers(r, static a => a.Exchange))
                : DnsResolverPal.ResolveMx(_servers, async, name, cancellationToken);

        private Task<DnsResult<TxtRecord>> ResolveTxtCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolveTxt(s.servers, s.async, s.name, s.cancellationToken),
                    static r =>
                    {
                        List<string> values = new();
                        foreach (TxtRecord record in r.Records)
                        {
                            values.AddRange(record.Values);
                        }
                        return values.ToArray();
                    })
                : DnsResolverPal.ResolveTxt(_servers, async, name, cancellationToken);

        private Task<DnsResult<CNameRecord>> ResolveCNameCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolveCName(s.servers, s.async, s.name, s.cancellationToken),
                    static r => MapAnswers(r, static a => a.CanonicalName))
                : DnsResolverPal.ResolveCName(_servers, async, name, cancellationToken);

        private Task<DnsResult<PtrRecord>> ResolvePtrCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolvePtr(s.servers, s.async, s.name, s.cancellationToken),
                    static r => MapAnswers(r, static a => a.Name))
                : DnsResolverPal.ResolvePtr(_servers, async, name, cancellationToken);

        private Task<DnsResult<NsRecord>> ResolveNsCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, (servers: _servers, async, name, cancellationToken),
                    static s => DnsResolverPal.ResolveNs(s.servers, s.async, s.name, s.cancellationToken),
                    static r => MapAnswers(r, static a => a.Name))
                : DnsResolverPal.ResolveNs(_servers, async, name, cancellationToken);

        private static async Task<DnsResult<T>> ResolveWithTelemetry<T, TState>(string name, TState state, Func<TState, Task<DnsResult<T>>> resolve, Func<DnsResult<T>, string[]> getAnswers)
        {
            NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(name);
            try
            {
                DnsResult<T> result = await resolve(state).ConfigureAwait(false);
                NameResolutionTelemetry.Log.AfterResolution(name, in activity, getAnswers(result));
                return result;
            }
            catch (Exception ex)
            {
                NameResolutionTelemetry.Log.AfterResolution(name, in activity, answer: null, exception: ex);
                throw;
            }
        }

        private static string[] MapAnswers<T>(DnsResult<T> result, Func<T, string> selector)
        {
            IReadOnlyList<T> records = result.Records;
            string[] answers = new string[records.Count];
            for (int i = 0; i < records.Count; i++)
            {
                answers[i] = selector(records[i]);
            }
            return answers;
        }

        // Combines the results of the separate A and AAAA queries issued for an
        // AddressFamily.Unspecified request into a single result.
        private static DnsResult<AddressRecord> MergeAddressResults(DnsResult<AddressRecord> a, DnsResult<AddressRecord> b)
        {
            if (a.Records.Count > 0 || b.Records.Count > 0)
            {
                AddressRecord[] merged = [.. a.Records, .. b.Records];
                // A positive result carries no negative-cache TTL.
                return new DnsResult<AddressRecord>(DnsResponseCode.NoError, merged, TimeSpan.Zero);
            }

            DnsResponseCode chosenRc = a.ResponseCode == DnsResponseCode.NxDomain || b.ResponseCode == DnsResponseCode.NxDomain
                ? DnsResponseCode.NxDomain
                : (a.ResponseCode != DnsResponseCode.NoError ? a.ResponseCode : b.ResponseCode);
            TimeSpan negTtl = MinNonZero(a.NegativeCacheTtl, b.NegativeCacheTtl);
            return new DnsResult<AddressRecord>(chosenRc, null, negTtl);
        }

        // Returns the smaller of two non-zero negative-cache TTLs, or zero if neither is positive.
        private static TimeSpan MinNonZero(TimeSpan x, TimeSpan y)
        {
            if (x <= TimeSpan.Zero)
            {
                return y > TimeSpan.Zero ? y : TimeSpan.Zero;
            }

            if (y <= TimeSpan.Zero)
            {
                return x;
            }

            return x < y ? x : y;
        }

        private static void ValidateName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
        }

        /// <summary>
        /// Builds the reverse-lookup .arpa domain name for an IPv4 or IPv6 address.
        /// </summary>
        internal static unsafe string BuildArpaName(IPAddress address)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                Span<byte> bytes = stackalloc byte[4];
                address.TryWriteBytes(bytes, out _);
                return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa";
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Span<byte> bytes = stackalloc byte[16];
                address.TryWriteBytes(bytes, out _);
                Span<char> chars = stackalloc char[16 * 4];
                int pos = 0;
                for (int i = bytes.Length - 1; i >= 0; i--)
                {
                    byte b = bytes[i];
                    chars[pos++] = ToHex(b & 0xF);
                    chars[pos++] = '.';
                    chars[pos++] = ToHex(b >> 4);
                    chars[pos++] = '.';
                }
                return string.Concat(chars, "ip6.arpa");

                static char ToHex(int n) => (char)(n < 10 ? '0' + n : 'a' + (n - 10));
            }
            else
            {
                throw new ArgumentException(SR.net_dns_unsupported_address_family, nameof(address));
            }
        }
    }
}
