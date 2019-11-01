// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    /// <summary>Provides a container class for Internet host address information.</summary>
    public class IPHostEntry
    {
        /// <summary>Gets or sets the DNS name of the host.</summary>
        public string HostName { get; set; }

        /// <summary>Gets or sets a list of aliases that are associated with a host.</summary>
        public string[] Aliases { get; set; }

        /// <summary>Gets or sets a list of IP addresses that are associated with a host.</summary>
        public IPAddress[] AddressList { get; set; }
    }
}
