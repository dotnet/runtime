// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal static partial class DnsResolverPal
    {
        // No platform-specific server restrictions to enforce; resolution itself is
        // unsupported and throws PlatformNotSupportedException at query time.
        public static void ValidateServers(IPEndPoint[] servers) => _ = servers;

        public static Task<DnsResult<AddressRecord>> ResolveAddresses(IPEndPoint[] servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<SrvRecord>> ResolveSrv(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<MxRecord>> ResolveMx(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<TxtRecord>> ResolveTxt(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<CNameRecord>> ResolveCName(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<NsRecord>> ResolveNs(IPEndPoint[] servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();
    }
}
