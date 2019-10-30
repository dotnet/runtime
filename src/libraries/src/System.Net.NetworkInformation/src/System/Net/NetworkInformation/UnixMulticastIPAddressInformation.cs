// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class UnixMulticastIPAddressInformation : MulticastIPAddressInformation
    {
        private readonly IPAddress _address;

        public UnixMulticastIPAddressInformation(IPAddress address)
        {
            _address = address;
        }

        public override IPAddress Address { get { return _address; } }

        public override bool IsDnsEligible { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsTransient { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long AddressPreferredLifetime { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long AddressValidLifetime { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override long DhcpLeaseLifetime { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override DuplicateAddressDetectionState DuplicateAddressDetectionState { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override PrefixOrigin PrefixOrigin { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override SuffixOrigin SuffixOrigin { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }
    }
}
