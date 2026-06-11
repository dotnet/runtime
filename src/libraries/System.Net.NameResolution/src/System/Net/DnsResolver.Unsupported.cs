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
        private Task<DnsResult<AddressRecord>> ResolveAddressesCoreAsync(string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<SrvRecord>> ResolveSrvCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<MxRecord>> ResolveMxCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<TxtRecord>> ResolveTxtCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<CNameRecord>> ResolveCNameCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<PtrRecord>> ResolvePtrCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private Task<DnsResult<NsRecord>> ResolveNsCoreAsync(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<AddressRecord> ResolveAddressesCore(string name, AddressFamily addressFamily, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<SrvRecord> ResolveSrvCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<MxRecord> ResolveMxCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<TxtRecord> ResolveTxtCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<CNameRecord> ResolveCNameCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<PtrRecord> ResolvePtrCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();

        private DnsResult<NsRecord> ResolveNsCore(string name, CancellationToken cancellationToken)
            => throw new PlatformNotSupportedException();
    }
}
