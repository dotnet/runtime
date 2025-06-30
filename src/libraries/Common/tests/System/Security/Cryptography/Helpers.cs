// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    // This file has its own implementation of Helpers.cs methods to minimize the risk of
    // both the product and test code have the same bugs. This is why the csproj does not
    // just link the common Helpers.cs files.
    internal sealed class Helpers
    {
        internal static int HashLength(HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                return 160 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return 256 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return 384 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return 512 / 8;
            }
#if NET
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_256)
            {
                return 256 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_384)
            {
                return 384 / 8;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA3_512)
            {
                return 512 / 8;
            }
#endif
            else if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return 128 / 8;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName));
            }
        }        
    }
}
