// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Rsa.Tests;

namespace System.Security.Cryptography.Tests
{
    internal static partial class RSAKeyPool
    {
        // Known keys can use the same pool as the generated keys,
        // but have to avoid legal key sizes.
        //
        // Thankfully, the smallest size any provider has is 384,
        // so 0-383 are automatically available.
        internal static RSALease RentBigExponentKey()
        {
            return ShapeLease(
                s_pool.Rent(0, ks => new IdempotentRSA(TestData.RsaBigExponentParams)));
        }
    }
}
