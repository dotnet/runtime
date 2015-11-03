// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about a network interface address.
    /// </summary>
    public abstract class GatewayIPAddressInformation
    {
        /// <summary>
        /// Gets the Internet Protocol (IP) address.
        /// </summary>
        public abstract IPAddress Address { get; }
    }
}
