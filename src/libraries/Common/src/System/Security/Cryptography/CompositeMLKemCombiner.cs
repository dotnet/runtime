// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static class CompositeMLKemCombiner
    {
        // C2PRICombiner from draft-irtf-cfrg-hybrid-kems-12, Section 5.1.3.
        // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-hybrid-kems-12#section-5.1.3
        internal static void Combine(
            ReadOnlySpan<byte> mlkemSharedSecret,
            ReadOnlySpan<byte> traditionalSharedSecret,
            ReadOnlySpan<byte> traditionalCiphertext,
            ReadOnlySpan<byte> traditionalEncapsulationKey,
            ReadOnlySpan<byte> label,
            Span<byte> destination)
        {
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA3_256))
            {
                hash.AppendData(mlkemSharedSecret);
                hash.AppendData(traditionalSharedSecret);
                hash.AppendData(traditionalCiphertext);
                hash.AppendData(traditionalEncapsulationKey);
                hash.AppendData(label);

                if (!hash.TryGetHashAndReset(destination, out int bytesWritten) || bytesWritten != destination.Length)
                {
                    Debug.Fail("SHA3-256 produced an unexpected output length.");
                    throw new CryptographicException();
                }
            }
        }
    }
}
