// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public static partial class Dns
    {
        // Shared DnsResolver instance used by the static Resolve* methods.
        // Uses the system-configured DNS servers.
        private static DnsResolver? s_defaultResolver;

        private static DnsResolver DefaultResolver =>
            s_defaultResolver ??= new DnsResolver();

        public static DnsResult<AddressRecord> ResolveAddresses(string name)
            => DefaultResolver.ResolveAddresses(name);

        public static DnsResult<AddressRecord> ResolveAddresses(string name, AddressFamily addressFamily)
            => DefaultResolver.ResolveAddresses(name, addressFamily);

        public static Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveAddressesAsync(name, cancellationToken);

        public static Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveAddressesAsync(name, addressFamily, cancellationToken);

        public static DnsResult<SrvRecord> ResolveSrv(string name)
            => DefaultResolver.ResolveSrv(name);

        public static Task<DnsResult<SrvRecord>> ResolveSrvAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveSrvAsync(name, cancellationToken);

        public static DnsResult<MxRecord> ResolveMx(string name)
            => DefaultResolver.ResolveMx(name);

        public static Task<DnsResult<MxRecord>> ResolveMxAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveMxAsync(name, cancellationToken);

        public static DnsResult<TxtRecord> ResolveTxt(string name)
            => DefaultResolver.ResolveTxt(name);

        public static Task<DnsResult<TxtRecord>> ResolveTxtAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveTxtAsync(name, cancellationToken);

        public static DnsResult<CNameRecord> ResolveCName(string name)
            => DefaultResolver.ResolveCName(name);

        public static Task<DnsResult<CNameRecord>> ResolveCNameAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveCNameAsync(name, cancellationToken);

        public static DnsResult<PtrRecord> ResolvePtr(string name)
            => DefaultResolver.ResolvePtr(name);

        public static DnsResult<PtrRecord> ResolvePtr(IPAddress address)
        {
            ArgumentNullException.ThrowIfNull(address);
            return DefaultResolver.ResolvePtr(DnsResolver.BuildArpaName(address));
        }

        public static Task<DnsResult<PtrRecord>> ResolvePtrAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolvePtrAsync(name, cancellationToken);

        public static Task<DnsResult<PtrRecord>> ResolvePtrAsync(IPAddress address, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolvePtrAsync(address, cancellationToken);

        public static DnsResult<NsRecord> ResolveNs(string name)
            => DefaultResolver.ResolveNs(name);

        public static Task<DnsResult<NsRecord>> ResolveNsAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveNsAsync(name, cancellationToken);
    }
}
