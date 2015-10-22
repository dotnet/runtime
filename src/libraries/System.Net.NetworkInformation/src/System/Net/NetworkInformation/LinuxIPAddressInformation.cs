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

        /// Gets the Internet Protocol (IP) address.
        public override IPAddress Address { get { return _address; } }

        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is legal to appear in a Domain Name System (DNS) server database.
        public override bool IsDnsEligible { get { throw new PlatformNotSupportedException(); } }

        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is transient.
        public override bool IsTransient { get { throw new PlatformNotSupportedException(); } }
    }
}
