// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1822 // Members do not access instance data and can be marked as static — but we keep them as instance to match the partial signatures on platforms that do implement DNS.

namespace System.Net
{
    public sealed partial class DnsResolver
    {
        private Task<DnsResult<AddressRecord>> ResolveAddressesCore(bool async, string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<SrvRecord>> ResolveSrvCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<MxRecord>> ResolveMxCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<TxtRecord>> ResolveTxtCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<CNameRecord>> ResolveCNameCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<PtrRecord>> ResolvePtrCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<NsRecord>> ResolveNsCore(bool async, string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();
    }
}
