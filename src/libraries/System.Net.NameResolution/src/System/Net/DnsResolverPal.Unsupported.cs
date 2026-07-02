// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal static partial class DnsResolverPal
    {
        public static Task<DnsResult<AddressRecord>> ResolveAddresses(IList<IPEndPoint> servers, bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<SrvRecord>> ResolveSrv(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<MxRecord>> ResolveMx(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<TxtRecord>> ResolveTxt(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<CNameRecord>> ResolveCName(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<PtrRecord>> ResolvePtr(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        public static Task<DnsResult<NsRecord>> ResolveNs(IList<IPEndPoint> servers, bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();
    }
}
