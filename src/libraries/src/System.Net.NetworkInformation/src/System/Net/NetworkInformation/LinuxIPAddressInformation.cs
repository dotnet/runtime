// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
