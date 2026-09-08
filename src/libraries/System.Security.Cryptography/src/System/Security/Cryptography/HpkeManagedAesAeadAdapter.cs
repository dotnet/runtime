// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1416 // //TODO:HPKE Call is reachable on "unsupported platform" - deal with this messy daignostic later.

namespace System.Security.Cryptography
{
    internal sealed class HpkeManagedAesAeadAdapter : HpkeManagedAeadAdapter
    {
        private readonly AesGcm _aes;

        internal HpkeManagedAesAeadAdapter(HpkeSuite suite, ReadOnlySpan<byte> key)
        {
            _aes = new AesGcm(key, suite.AeadMetadata.Nt);
        }

        internal override void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            Span<byte> ciphertext,
            Span<byte> tag)
        {
            _aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        internal override void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext)
        {
            _aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }


        public override void Dispose() => _aes.Dispose();
    }
}
