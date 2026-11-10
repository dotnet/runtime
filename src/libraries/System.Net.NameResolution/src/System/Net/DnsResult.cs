// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net
{
    /// <summary>
    /// Represents the result of a DNS resolution operation, including the response
    /// code, the parsed records, and (for negative responses) the negative-cache TTL.
    /// </summary>
    /// <typeparam name="T">The type of the resolved records.</typeparam>
    public readonly struct DnsResult<T>
    {
        private readonly IReadOnlyList<T>? _records;

        /// <summary>Gets the DNS response code returned by the server.</summary>
        [CLSCompliant(false)]
        public DnsResponseCode ResponseCode { get; }

        /// <summary>
        /// Gets the records returned by the server. The list is empty on error or NODATA responses.
        /// </summary>
        public IReadOnlyList<T> Records => _records ?? Array.Empty<T>();

        /// <summary>
        /// Gets the duration for which a negative response (NXDOMAIN or NODATA) may be cached.
        /// </summary>
        /// <remarks>
        /// The value is derived from the SOA minimum TTL in the authority section, per RFC 2308 §5.
        /// Availability is best-effort and platform-dependent; the value is <see cref="TimeSpan.Zero"/>
        /// when not applicable or unavailable.
        /// </remarks>
        public TimeSpan NegativeCacheTtl { get; }

        internal DnsResult(DnsResponseCode responseCode, IReadOnlyList<T>? records, TimeSpan negativeCacheTtl)
        {
            ResponseCode = responseCode;
            _records = records;
            NegativeCacheTtl = negativeCacheTtl;
        }
    }
}
