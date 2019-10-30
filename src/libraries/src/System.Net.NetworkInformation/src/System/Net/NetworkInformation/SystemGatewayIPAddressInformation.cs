// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// Provides information about a network interface address.
    internal class SystemGatewayIPAddressInformation : GatewayIPAddressInformation
    {
        private readonly IPAddress _address;

        private SystemGatewayIPAddressInformation(IPAddress address)
        {
            _address = address;
        }

        /// Gets the Internet Protocol (IP) address.
        public override IPAddress Address
        {
            get
            {
                return _address;
            }
        }

        internal static GatewayIPAddressInformationCollection ToGatewayIpAddressInformationCollection(IPAddressCollection addresses)
        {
            GatewayIPAddressInformationCollection gatewayList = new GatewayIPAddressInformationCollection();
            foreach (IPAddress address in addresses)
            {
                gatewayList.InternalAdd(new SystemGatewayIPAddressInformation(address));
            }

            return gatewayList;
        }
    }
}
