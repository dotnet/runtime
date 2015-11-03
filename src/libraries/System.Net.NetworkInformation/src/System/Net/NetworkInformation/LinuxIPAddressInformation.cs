// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal class LinuxIPAddressInformation : IPAddressInformation
    {
        private readonly IPAddress _address;

        public LinuxIPAddressInformation(IPAddress address)
        {
            _address = address;
        }

        public override IPAddress Address { get { return _address; } }

        public override bool IsDnsEligible { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }

        public override bool IsTransient { get { throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform); } }
    }
}
