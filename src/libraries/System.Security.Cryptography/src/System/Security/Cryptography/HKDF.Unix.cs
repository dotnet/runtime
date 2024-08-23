// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static void Extract(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, ReadOnlySpan<byte> salt, Span<byte> prk)
        {
            throw new NotImplementedException();
        }

        private static void Expand(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> prk, Span<byte> output, ReadOnlySpan<byte> info)
        {
            throw new NotImplementedException();
        }

        private static void DeriveKeyCore(HashAlgorithmName hashAlgorithmName, int hashLength, ReadOnlySpan<byte> ikm, Span<byte> output, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> info)
        {
            throw new NotImplementedException();
        }
    }
}
