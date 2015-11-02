// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about a network interface address.
    /// </summary>
    public abstract class IPAddressInformation
    {
        /// <summary>
        /// Gets the Internet Protocol (IP) address.
        /// </summary>
        public abstract IPAddress Address { get; }

        /// <summary>
        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is legal to appear in a Domain Name System (DNS) server database.
        /// </summary>
        public abstract bool IsDnsEligible { get; }

        /// <summary>
        /// Gets a bool value that indicates whether the Internet Protocol (IP) address is transient.
        /// </summary>
        public abstract bool IsTransient { get; }
    }
}
