// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
