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
        // Uses the system-configured DNS servers. A benign race may construct more
        // than one instance, but only one is published; DnsResolver holds no
        // unmanaged state, so the extra instance is simply collected.
        private static DnsResolver DefaultResolver => field ??= new();

        /// <summary>
        /// Resolves the IPv4 (A) and IPv6 (AAAA) addresses for the specified host name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<AddressRecord> ResolveAddresses(string name)
            => DefaultResolver.ResolveAddresses(name);

        /// <summary>
        /// Resolves the addresses of the specified family for the specified host name using the system-configured DNS servers.
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
        public static DnsResult<AddressRecord> ResolveAddresses(string name, AddressFamily addressFamily)
            => DefaultResolver.ResolveAddresses(name, addressFamily);

        /// <summary>
        /// Asynchronously resolves the IPv4 (A) and IPv6 (AAAA) addresses for the specified host name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The host name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the address records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveAddressesAsync(name, cancellationToken);

        /// <summary>
        /// Asynchronously resolves the addresses of the specified family for the specified host name using the system-configured DNS servers.
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
        public static Task<DnsResult<AddressRecord>> ResolveAddressesAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveAddressesAsync(name, addressFamily, cancellationToken);

        /// <summary>
        /// Resolves the service (SRV) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The name to resolve, typically in the form <c>_service._protocol.host</c>.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the SRV records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<SrvRecord> ResolveSrv(string name)
            => DefaultResolver.ResolveSrv(name);

        /// <summary>
        /// Asynchronously resolves the service (SRV) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The name to resolve, typically in the form <c>_service._protocol.host</c>.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the SRV records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<SrvRecord>> ResolveSrvAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveSrvAsync(name, cancellationToken);

        /// <summary>
        /// Resolves the mail exchange (MX) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the MX records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<MxRecord> ResolveMx(string name)
            => DefaultResolver.ResolveMx(name);

        /// <summary>
        /// Asynchronously resolves the mail exchange (MX) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the MX records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<MxRecord>> ResolveMxAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveMxAsync(name, cancellationToken);

        /// <summary>
        /// Resolves the text (TXT) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the TXT records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<TxtRecord> ResolveTxt(string name)
            => DefaultResolver.ResolveTxt(name);

        /// <summary>
        /// Asynchronously resolves the text (TXT) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the TXT records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<TxtRecord>> ResolveTxtAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveTxtAsync(name, cancellationToken);

        /// <summary>
        /// Resolves the canonical name (CNAME) record for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the CNAME records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<CNameRecord> ResolveCName(string name)
            => DefaultResolver.ResolveCName(name);

        /// <summary>
        /// Asynchronously resolves the canonical name (CNAME) record for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the CNAME records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<CNameRecord>> ResolveCNameAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveCNameAsync(name, cancellationToken);

        /// <summary>
        /// Resolves the pointer (PTR) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The name to resolve, typically a reverse-lookup name such as <c>4.3.2.1.in-addr.arpa</c>.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<PtrRecord> ResolvePtr(string name)
            => DefaultResolver.ResolvePtr(name);

        /// <summary>
        /// Resolves the pointer (PTR) records for the specified IP address using the system-configured DNS servers.
        /// </summary>
        /// <param name="address">The IP address to perform a reverse lookup for.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is <see langword="null"/>.</exception>
        public static DnsResult<PtrRecord> ResolvePtr(IPAddress address)
            => DefaultResolver.ResolvePtr(address);

        /// <summary>
        /// Asynchronously resolves the pointer (PTR) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The name to resolve, typically a reverse-lookup name such as <c>4.3.2.1.in-addr.arpa</c>.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<PtrRecord>> ResolvePtrAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolvePtrAsync(name, cancellationToken);

        /// <summary>
        /// Asynchronously resolves the pointer (PTR) records for the specified IP address using the system-configured DNS servers.
        /// </summary>
        /// <param name="address">The IP address to perform a reverse lookup for.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the PTR records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is <see langword="null"/>.</exception>
        public static Task<DnsResult<PtrRecord>> ResolvePtrAsync(IPAddress address, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolvePtrAsync(address, cancellationToken);

        /// <summary>
        /// Resolves the authoritative name server (NS) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <returns>A <see cref="DnsResult{T}"/> containing the NS records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static DnsResult<NsRecord> ResolveNs(string name)
            => DefaultResolver.ResolveNs(name);

        /// <summary>
        /// Asynchronously resolves the authoritative name server (NS) records for the specified name using the system-configured DNS servers.
        /// </summary>
        /// <param name="name">The domain name to resolve.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="DnsResult{T}"/> containing the NS records.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
        public static Task<DnsResult<NsRecord>> ResolveNsAsync(string name, CancellationToken cancellationToken = default)
            => DefaultResolver.ResolveNsAsync(name, cancellationToken);
    }
}
