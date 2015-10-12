// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides information about a network interface address.
    public abstract class GatewayIPAddressInformation
    {
        /// Gets the Internet Protocol (IP) address.
        public abstract IPAddress Address { get; }
    }
}
