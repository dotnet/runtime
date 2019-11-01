// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    internal class SimpleGatewayIPAddressInformation : GatewayIPAddressInformation
    {
        private readonly IPAddress _address;

        public SimpleGatewayIPAddressInformation(IPAddress address)
        {
            _address = address;
        }

        public override IPAddress Address
        {
            get
            {
                return _address;
            }
        }
    }
}
