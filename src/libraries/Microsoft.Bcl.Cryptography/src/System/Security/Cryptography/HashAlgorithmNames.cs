// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    // Strings need to match CNG identifiers.
    internal static class HashAlgorithmNames
    {
        internal const string SHA1 = nameof(SHA1);
        internal const string SHA256 = nameof(SHA256);
        internal const string SHA384 = nameof(SHA384);
        internal const string SHA512 = nameof(SHA512);
        internal const string SHA3_256 = "SHA3-256";
        internal const string SHA3_384 = "SHA3-384";
        internal const string SHA3_512 = "SHA3-512";
    }
}
