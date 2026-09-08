// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
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
            const int MaxStackSecretLength = 64;

            using (CryptoPoolLease sharedSecret = CryptoPoolLease.RentConditionally(
                Suite.KemMetadata.Nsecret, stackalloc byte[MaxStackSecretLength]))
            using (CryptoPoolLease key = CryptoPoolLease.RentConditionally(
                Suite.AeadMetadata.Nk, stackalloc byte[MaxStackSecretLength]))
            using (CryptoPoolLease baseNonce = CryptoPoolLease.RentConditionally(
                Suite.AeadMetadata.Nn, stackalloc byte[MaxStackSecretLength]))
            using (CryptoPoolLease exporterSecret = CryptoPoolLease.RentConditionally(
                Suite.KdfMetadata.Nh, stackalloc byte[MaxStackSecretLength]))
            {
                _kemAdapter.Encapsulate(encapsulatedSecret, sharedSecret.Span);

                HpkeManagedKdfAdapter kdf = HpkeManagedKdfAdapter.Create(Suite);
                kdf.DeriveSecrets(
                    mode: 0,
                    sharedSecret.Span,
                    info,
                    psk: default,
                    pskId: default,
                    key.Span,
                    baseNonce.Span,
                    exporterSecret.Span);

                using (HpkeManagedAeadAdapter aead = HpkeManagedAeadAdapter.Create(Suite, key.Span))
                {
                    // Single-shot sealing uses sequence number zero, so the nonce is base_nonce.
                    // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.2
                    aead.Encrypt(
                        plaintext,
                        baseNonce.Span,
                        associatedData,
                        ciphertext.Slice(0, plaintext.Length),
                        ciphertext.Slice(plaintext.Length));
                }
            }
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
