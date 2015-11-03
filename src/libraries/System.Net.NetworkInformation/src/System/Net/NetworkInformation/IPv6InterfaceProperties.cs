// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about network interfaces that support Internet Protocol (IP) version 6.0.
    /// </summary>
    public abstract class IPv6InterfaceProperties
    {
        /// <summary>
        /// Gets the interface index for the Internet Protocol (IP) address.
        /// </summary>
        public abstract int Index { get; }

        /// <summary>
        /// Gets the maximum transmission unit (MTU) for this network interface.
        /// </summary>
        public abstract int Mtu { get; }

        /// <summary>
        /// Returns IPv6 scope identifiers.
        /// </summary>
        /// <param name="scopeLevel">The scope level.</param>
        /// <returns>The IPv6 scope identifier.</returns>
        public virtual long GetScopeId(ScopeLevel scopeLevel)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }
    }
}
