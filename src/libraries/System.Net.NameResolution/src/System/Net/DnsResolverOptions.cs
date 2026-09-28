// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net
{
    /// <summary>
    /// Options controlling DNS resolution performed by <see cref="DnsResolver"/>.
    /// </summary>
    public sealed class DnsResolverOptions
    {
        private IList<IPEndPoint> _servers = new List<IPEndPoint>();

        /// <summary>
        /// Gets or sets the DNS servers to query. When empty, the system-configured DNS servers are used.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value being set is <see langword="null"/>.</exception>
        public IList<IPEndPoint> Servers
        {
            get => _servers;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _servers = value;
            }
        }
    }
}
