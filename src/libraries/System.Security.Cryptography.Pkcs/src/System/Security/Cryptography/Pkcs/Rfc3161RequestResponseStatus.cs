// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Pkcs
{
    internal enum Rfc3161RequestResponseStatus
    {
        Unknown = 0,
        Accepted = 1,
        DoesNotParse = 2,
        RequestFailed = 3,
        HashMismatch = 4,
        VersionTooNew = 5,
        NonceMismatch = 6,
        RequestedCertificatesMissing = 7,
        UnexpectedCertificates = 8,
    }
}
