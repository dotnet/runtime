// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// Provides information about network interfaces that support Internet Protocol (IP) version 4.0.
    public abstract class IPv6InterfaceProperties
    {
        /// Gets the interface index for the Internet Protocol (IP) address.
        public abstract int Index { get; }

        /// Gets the maximum transmission unit (MTU) for this network interface.
        public abstract int Mtu { get; }

        /// Returns IPv6 scope identifiers.
        public virtual long GetScopeId(ScopeLevel scopeLevel)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
