// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1416 // //TODO:HPKE Call is reachable on "unsupported platform" - deal with this messy daignostic later.

namespace System.Security.Cryptography
{
    internal sealed class HpkeManagedChaCha20Poly1305AeadAdapter : HpkeManagedAeadAdapter
    {
        private readonly ChaCha20Poly1305 _chacha;

        internal HpkeManagedChaCha20Poly1305AeadAdapter(ReadOnlySpan<byte> key)
        {
            _chacha = new ChaCha20Poly1305(key);
        }

        internal override void Encrypt(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            Span<byte> ciphertext,
            Span<byte> tag)
        {
            _chacha.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
        }

        internal override void Decrypt(
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext)
        {
            _chacha.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        }

        public override void Dispose() => _chacha.Dispose();
    }
}
