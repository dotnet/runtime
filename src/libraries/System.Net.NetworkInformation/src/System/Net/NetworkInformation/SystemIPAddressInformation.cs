// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    // This is the main addressinformation class that contains the ipaddress
    // and other properties.
    internal sealed class SystemIPAddressInformation : IPAddressInformation
    {
        private readonly IPAddress _address;
        internal readonly bool Transient;
        internal readonly bool DnsEligible;

        internal SystemIPAddressInformation(IPAddress address, Interop.IpHlpApi.AdapterAddressFlags flags)
        {
            _address = address;
            Transient = (flags & Interop.IpHlpApi.AdapterAddressFlags.Transient) > 0;
            DnsEligible = (flags & Interop.IpHlpApi.AdapterAddressFlags.DnsEligible) > 0;
        }

        public override IPAddress Address { get { return _address; } }

        /// The address is a cluster address and shouldn't be used by most applications.
        public override bool IsTransient
        {
            get
            {
                return Transient;
            }
        }

        /// This address can be used for DNS.
        public override bool IsDnsEligible
        {
            get
            {
                return DnsEligible;
            }
        }
    }
}
