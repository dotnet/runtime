// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/CSCommon.h#L384 for reference.
/// </summary>
internal enum HashType : byte
{
    SHA256 = 2
}

internal static class HashTypeExtensions
{
    internal static HashAlgorithm CreateHashAlgorithm(this HashType hashType)
    {
        return hashType switch
        {
            HashType.SHA256 => SHA256.Create(),
            _ => throw new NotSupportedException($"HashType {hashType} is not supported.")
        };
    }

    internal static byte GetHashSize(this HashType hashType)
    {
        return hashType switch
        {
            HashType.SHA256 => 32, // SHA-256 produces a 32-byte hash
            _ => throw new NotSupportedException($"HashType {hashType} is not supported.")
        };
    }
}
