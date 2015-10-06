// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides information about a network interface address.
    public abstract class IPAddressInformation
    {
        /// Gets the Internet Protocol (IP) address.
        public abstract IPAddress Address { get; }

        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is legal to appear in a Domain Name System (DNS) server database.
        public abstract bool IsDnsEligible { get; }

        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is transient.
        public abstract bool IsTransient { get; }
    }
}
