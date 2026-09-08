// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

#pragma warning disable CA1416 // //TODO:HPKE Call is reachable on "unsupported platform" - deal with this messy daignostic later.

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

    internal sealed class HpkeImplementation : Hpke
    {
        private readonly HpkeManagedKemAdapter _kemAdapter;

        private HpkeImplementation(HpkeSuite suite, HpkeManagedKemAdapter kemAdapter) : base(suite)
        {
            _kemAdapter = kemAdapter;
        }

        internal static bool IsSupportedImpl(HpkeSuite suite) =>
            suite.KemMetadata.IsSupported &&
            suite.KdfMetadata.IsSupported &&
            suite.AeadMetadata.IsSupported;

        internal static HpkeImplementation DeriveKeyImpl(HpkeSuite suite, ReadOnlySpan<byte> ikm)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.DeriveKeyPair(ikm);
                return new HpkeImplementation(suite, adapter);
            }
            catch
            {
                adapter.Dispose();
                throw;
            }
        }

        internal static HpkeImplementation GenerateKeyImpl(HpkeSuite suite)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.Generate();
                return new HpkeImplementation(suite, adapter);
            }
            catch
            {
                adapter.Dispose();
                throw;
            }
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
            _kemAdapter.ExportDecapsulationKey(destination);

        protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
            _kemAdapter.ExportEncapsulationKey(destination);

        protected override void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> encapsulatedSecret,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kemAdapter.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
