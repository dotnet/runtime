// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.NetworkInformation
{
    internal sealed class SimpleGatewayIPAddressInformation : GatewayIPAddressInformation
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
