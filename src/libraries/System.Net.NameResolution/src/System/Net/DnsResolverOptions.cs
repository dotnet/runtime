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
        /// <summary>
        /// DNS servers to query. When empty, the system-configured DNS servers are used.
        /// </summary>
        public IList<IPEndPoint> Servers { get; set; } = new List<IPEndPoint>();
    }
}
