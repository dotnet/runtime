// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net
{
    /// <summary>
    /// Carries the result of a DNS resolution operation, including the response
    /// code, the parsed records, and (for negative responses) the negative-cache TTL.
    /// </summary>
    public readonly struct DnsResult<T>
    {
        private readonly IReadOnlyList<T>? _records;

        /// <summary>The DNS response code returned by the server.</summary>
        [CLSCompliant(false)]
        public DnsResponseCode ResponseCode { get; }

        /// <summary>
        /// The records returned by the server. Empty on error or NODATA responses.
        /// </summary>
        public IReadOnlyList<T> Records => _records ?? Array.Empty<T>();

        /// <summary>
        /// For negative responses (NXDOMAIN/NODATA), the TTL for which the negative
        /// answer may be cached (derived from the SOA minimum TTL in the authority
        /// section, per RFC 2308 §5). <see cref="TimeSpan.Zero"/> if not applicable
        /// or unavailable.
        /// </summary>
        public TimeSpan NegativeCacheTtl { get; }

        internal DnsResult(DnsResponseCode responseCode, IReadOnlyList<T>? records, TimeSpan negativeCacheTtl)
        {
            ResponseCode = responseCode;
            _records = records;
            NegativeCacheTtl = negativeCacheTtl;
        }
    }
}
