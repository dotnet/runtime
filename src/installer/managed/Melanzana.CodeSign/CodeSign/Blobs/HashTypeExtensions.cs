// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Melanzana.CodeSign.Blobs
{
    public static class HashTypeExtensions
    {
        public static IncrementalHash GetIncrementalHash(this HashType hashType)
        {
            return hashType switch
            {
                HashType.SHA1 => IncrementalHash.CreateHash(HashAlgorithmName.SHA1),
                HashType.SHA256 => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
                HashType.SHA256Truncated => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
                HashType.SHA384 => IncrementalHash.CreateHash(HashAlgorithmName.SHA384),
                HashType.SHA512 => IncrementalHash.CreateHash(HashAlgorithmName.SHA512),
                _ => throw new NotSupportedException()
            };
        }

        public static byte GetSize(this HashType hashType)
        {
            return hashType switch
            {
                HashType.SHA1 => 20,
                HashType.SHA256 => 32,
                HashType.SHA256Truncated => 20,
                HashType.SHA384 => 48,
                HashType.SHA512 => 64,
                _ => throw new NotSupportedException()
            };
        }
    }
}
