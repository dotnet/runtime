// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            return ResolveAddressesCore(async: false, name, addressFamily, default).GetAwaiter().GetResult();
        }

        public DnsResult<SrvRecord> ResolveSrv(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveSrvCore(async: false, name, default).GetAwaiter().GetResult();
        }

        public DnsResult<MxRecord> ResolveMx(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveMxCore(async: false, name, default).GetAwaiter().GetResult();
        }

        public DnsResult<TxtRecord> ResolveTxt(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveTxtCore(async: false, name, default).GetAwaiter().GetResult();
        }

        public DnsResult<CNameRecord> ResolveCName(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveCNameCore(async: false, name, default).GetAwaiter().GetResult();
        }

        public DnsResult<PtrRecord> ResolvePtr(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolvePtrCore(async: false, name, default).GetAwaiter().GetResult();
        }

        public DnsResult<NsRecord> ResolveNs(string name)
        {
            ValidateName(name);
            ObjectDisposedException.ThrowIf(_disposed, this);
            return ResolveNsCore(async: false, name, default).GetAwaiter().GetResult();
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
