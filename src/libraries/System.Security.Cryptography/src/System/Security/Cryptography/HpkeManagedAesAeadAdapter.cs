// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed class HpkeManagedAesAeadAdapter : HpkeManagedAeadAdapter
    {
        private readonly AesGcm _aes;

        internal HpkeManagedAesAeadAdapter(HpkeSuite suite, ReadOnlySpan<byte> key)
        {
#pragma warning disable CA1416
            _aes = new AesGcm(key, suite.AeadMetadata.Nt);
#pragma warning restore CA1416
        }

        internal override void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            Span<byte> ciphertext,
            Span<byte> tag)
        {
#pragma warning disable CA1416
            _aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
#pragma warning restore CA1416
        }

        internal override void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext)
        {
#pragma warning disable CA1416
            _aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
#pragma warning restore CA1416
        }


#pragma warning disable CA1416
        public override void Dispose() => _aes.Dispose();
#pragma warning restore CA1416
    }
}
