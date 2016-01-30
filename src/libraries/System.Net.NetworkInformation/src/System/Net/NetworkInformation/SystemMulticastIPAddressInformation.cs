// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    // Specifies the Multicast addresses for an interface.
    internal class SystemMulticastIPAddressInformation : MulticastIPAddressInformation
    {
        private readonly SystemIPAddressInformation _innerInfo;

        public SystemMulticastIPAddressInformation(SystemIPAddressInformation addressInfo)
        {
            _innerInfo = addressInfo;
        }

        public override IPAddress Address { get { return _innerInfo.Address; } }

        // The address is a cluster address and shouldn't be used by most applications.
        public override bool IsTransient
        {
            get
            {
                return (_innerInfo.IsTransient);
            }
        }

        // This address can be used for DNS.
        public override bool IsDnsEligible
        {
            get
            {
                return (_innerInfo.IsDnsEligible);
            }
        }

        public override PrefixOrigin PrefixOrigin
        {
            get
            {
                return PrefixOrigin.Other;
            }
        }

        public override SuffixOrigin SuffixOrigin
        {
            get
            {
                return SuffixOrigin.Other;
            }
        }

        public override DuplicateAddressDetectionState DuplicateAddressDetectionState
        {
            get
            {
                return DuplicateAddressDetectionState.Invalid;
            }
        }

        // Specifies the valid lifetime of the address in seconds.
        public override long AddressValidLifetime
        {
            get
            {
                return 0;
            }
        }

        // Specifies the preferred lifetime of the address in seconds.
        public override long AddressPreferredLifetime
        {
            get
            {
                return 0;
            }
        }

        // Specifies the preferred lifetime of the address in seconds.
        public override long DhcpLeaseLifetime
        {
            get
            {
                return 0;
            }
        }

        internal static MulticastIPAddressInformationCollection ToMulticastIpAddressInformationCollection(IPAddressInformationCollection addresses)
        {
            MulticastIPAddressInformationCollection multicastList = new MulticastIPAddressInformationCollection();
            foreach (IPAddressInformation addressInfo in addresses)
            {
                multicastList.InternalAdd(new SystemMulticastIPAddressInformation((SystemIPAddressInformation)addressInfo));
            }
            return multicastList;
        }
    }
}
