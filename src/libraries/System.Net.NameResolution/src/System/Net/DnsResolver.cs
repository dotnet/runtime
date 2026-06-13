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
    /// </remarks>
    public sealed partial class DnsResolver : IAsyncDisposable, IDisposable
    {
        private readonly DnsResolverOptions _options;
        private bool _disposed;

        public DnsResolver() : this(new DnsResolverOptions()) { }

        public DnsResolver(DnsResolverOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
        }

        public DnsResult<AddressRecord> ResolveAddresses(string name)
            => ResolveAddresses(name, AddressFamily.Unspecified);

        public DnsResult<AddressRecord> ResolveAddresses(string name, AddressFamily addressFamily)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<AddressRecord>> task = ResolveAddressesCore(async: false, name, addressFamily, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<SrvRecord> ResolveSrv(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<SrvRecord>> task = ResolveSrvCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<MxRecord> ResolveMx(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<MxRecord>> task = ResolveMxCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<TxtRecord> ResolveTxt(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<TxtRecord>> task = ResolveTxtCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<CNameRecord> ResolveCName(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<CNameRecord>> task = ResolveCNameCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<PtrRecord> ResolvePtr(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<PtrRecord>> task = ResolvePtrCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public DnsResult<NsRecord> ResolveNs(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            Task<DnsResult<NsRecord>> task = ResolveNsCore(async: false, name, default);
            Debug.Assert(task.IsCompleted);
            return task.GetAwaiter().GetResult();
        }

        public Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, CancellationToken cancellationToken = default)
            => ResolveAddressesAsync(name, AddressFamily.Unspecified, cancellationToken);

        public Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveAddressesCore(async: true, name, addressFamily, cancellationToken);
        }

        public Task<DnsResult<SrvRecord>> ResolveSrvAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveSrvCore(async: true, name, cancellationToken);
        }

        public Task<DnsResult<MxRecord>> ResolveMxAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveMxCore(async: true, name, cancellationToken);
        }

        public Task<DnsResult<TxtRecord>> ResolveTxtAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveTxtCore(async: true, name, cancellationToken);
        }

        public Task<DnsResult<CNameRecord>> ResolveCNameAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveCNameCore(async: true, name, cancellationToken);
        }

        public Task<DnsResult<PtrRecord>> ResolvePtrAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolvePtrCore(async: true, name, cancellationToken);
        }

        public Task<DnsResult<PtrRecord>> ResolvePtrAsync(IPAddress address, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(address);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolvePtrCore(async: true, BuildArpaName(address), cancellationToken);
        }

        public Task<DnsResult<NsRecord>> ResolveNsAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveNsCore(async: true, name, cancellationToken);
        }

        public void Dispose() => _disposed = true;

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

        private Task<DnsResult<AddressRecord>> ResolveAddressesCore(bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveAddresses(_options.Servers, async, name, addressFamily, cancellationToken), static r => MapAnswers(r, static a => a.Address.ToString()))
                : DnsResolverPal.ResolveAddresses(_options.Servers, async, name, addressFamily, cancellationToken);

        private Task<DnsResult<SrvRecord>> ResolveSrvCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveSrv(_options.Servers, async, name, cancellationToken), static r => MapAnswers(r, static a => a.Target))
                : DnsResolverPal.ResolveSrv(_options.Servers, async, name, cancellationToken);

        private Task<DnsResult<MxRecord>> ResolveMxCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveMx(_options.Servers, async, name, cancellationToken), static r => MapAnswers(r, static a => a.Exchange))
                : DnsResolverPal.ResolveMx(_options.Servers, async, name, cancellationToken);

        private Task<DnsResult<TxtRecord>> ResolveTxtCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveTxt(_options.Servers, async, name, cancellationToken), static r =>
                {
                    List<string> values = new();
                    foreach (TxtRecord record in r.Records)
                    {
                        values.AddRange(record.Values);
                    }
                    return values.ToArray();
                })
                : DnsResolverPal.ResolveTxt(_options.Servers, async, name, cancellationToken);

        private Task<DnsResult<CNameRecord>> ResolveCNameCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveCName(_options.Servers, async, name, cancellationToken), static r => MapAnswers(r, static a => a.CanonicalName))
                : DnsResolverPal.ResolveCName(_options.Servers, async, name, cancellationToken);

        private Task<DnsResult<PtrRecord>> ResolvePtrCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolvePtr(_options.Servers, async, name, cancellationToken), static r => MapAnswers(r, static a => a.Name))
                : DnsResolverPal.ResolvePtr(_options.Servers, async, name, cancellationToken);

        private Task<DnsResult<NsRecord>> ResolveNsCore(bool async, string name, CancellationToken cancellationToken)
            => NameResolutionTelemetry.AnyDiagnosticsEnabled()
                ? ResolveWithTelemetry(name, () => DnsResolverPal.ResolveNs(_options.Servers, async, name, cancellationToken), static r => MapAnswers(r, static a => a.Name))
                : DnsResolverPal.ResolveNs(_options.Servers, async, name, cancellationToken);

        private static async Task<DnsResult<T>> ResolveWithTelemetry<T>(string name, Func<Task<DnsResult<T>>> resolve, Func<DnsResult<T>, string[]> getAnswers)
        {
            NameResolutionActivity activity = NameResolutionTelemetry.Log.BeforeResolution(name);
            try
            {
                DnsResult<T> result = await resolve().ConfigureAwait(false);
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
                return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}.in-addr.arpa");
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                Span<byte> bytes = stackalloc byte[16];
                address.TryWriteBytes(bytes, out _);
                Span<char> chars = stackalloc char[32 * 2 + 9];
                int pos = 0;
                for (int i = 15; i >= 0; i--)
                {
                    byte b = bytes[i];
                    chars[pos++] = ToHex(b & 0xF);
                    chars[pos++] = '.';
                    chars[pos++] = ToHex(b >> 4);
                    chars[pos++] = '.';
                }
                "ip6.arpa".AsSpan().CopyTo(chars.Slice(pos));
                pos += "ip6.arpa".Length;
                return new string(chars.Slice(0, pos));

                static char ToHex(int n) => (char)(n < 10 ? '0' + n : 'a' + (n - 10));
            }
            else
            {
                throw new ArgumentException(SR.net_invalid_ip_addr, nameof(address));
            }
        }
    }
}
