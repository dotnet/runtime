// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    /// <summary>DNS response codes as defined in RFC 1035 (and updates).</summary>
    [CLSCompliant(false)]
    public enum DnsResponseCode : ushort
    {
        /// <summary>No error condition.</summary>
        NoError = 0,
        /// <summary>The name server was unable to interpret the query.</summary>
        FormatError = 1,
        /// <summary>The name server was unable to process this query due to a problem with the name server.</summary>
        ServerFailure = 2,
        /// <summary>The domain name referenced in the query does not exist (NXDOMAIN).</summary>
        NxDomain = 3,
        /// <summary>The name server does not support the requested kind of query.</summary>
        NotImplemented = 4,
        /// <summary>The name server refuses to perform the specified operation for policy reasons.</summary>
        Refused = 5,
    }
}
