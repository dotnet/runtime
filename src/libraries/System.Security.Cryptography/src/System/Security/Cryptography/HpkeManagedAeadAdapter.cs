// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal abstract class HpkeManagedAeadAdapter : IDisposable
    {
        internal static HpkeManagedAeadAdapter Create(HpkeSuite suite, ReadOnlySpan<byte> key)
        {
            Debug.Assert(suite.AeadMetadata.Nt == 16);
            Debug.Assert(key.Length == suite.AeadMetadata.Nk);

            switch (suite.AeadAlgorithm)
            {
                case HpkeAead.AES_128_GCM:
                case HpkeAead.AES_256_GCM:
                    return new HpkeManagedAesAeadAdapter(suite, key);
                case HpkeAead.ChaCha20Poly1305:
                    return new HpkeManagedChaCha20Poly1305AeadAdapter(key);
                default:
                    Debug.Fail($"Unmapped AEAD adapter algorithm {suite.AeadAlgorithm}.");
                    throw new CryptographicException();
            }
        }

        internal abstract void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            Span<byte> ciphertext,
            Span<byte> tag);

        internal abstract void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext);

        public abstract void Dispose();
    }
}
