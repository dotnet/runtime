// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public static partial class HKDF
    {
        private static void ExtractCore(
            HashAlgorithmName hashAlgorithmName,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> prk)
        {
            HKDFManagedImplementation.Extract(hashAlgorithmName, ikm, salt, prk);
        }

        private static void Expand(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> prk,
            Span<byte> output,
            ReadOnlySpan<byte> info)
        {
            HKDFManagedImplementation.Expand(hashAlgorithmName, hashLength, prk, output, info);
        }

        private static void DeriveKeyCore(
            HashAlgorithmName hashAlgorithmName,
            int hashLength,
            ReadOnlySpan<byte> ikm,
            Span<byte> output,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info)
        {
            HKDFManagedImplementation.DeriveKey(hashAlgorithmName, hashLength, ikm, output, salt, info);
        }
    }
}
