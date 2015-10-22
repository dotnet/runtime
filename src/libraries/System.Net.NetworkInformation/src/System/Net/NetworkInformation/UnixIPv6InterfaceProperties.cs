// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    internal abstract class UnixIPv6InterfaceProperties : IPv6InterfaceProperties
    {
        private readonly UnixNetworkInterface _uni;

        public UnixIPv6InterfaceProperties(UnixNetworkInterface uni)
        {
            _uni = uni;
        }

        public sealed override int Index { get { return _uni.Index; } }
    }
}
