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
            Span<byte> sharedSecretBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> keyBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> baseNonceBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> exporterSecretBuffer = stackalloc byte[MaxStackSecretLength];

            try
            {
                Span<byte> sharedSecret = sharedSecretBuffer.Slice(0, Suite.KemMetadata.Nsecret);
                Span<byte> key = keyBuffer.Slice(0, Suite.AeadMetadata.Nk);
                Span<byte> baseNonce = baseNonceBuffer.Slice(0, Suite.AeadMetadata.Nn);
                Span<byte> exporterSecret = exporterSecretBuffer.Slice(0, Suite.KdfMetadata.Nh);
                _kemAdapter.Encapsulate(encapsulatedSecret, sharedSecret);

                HpkeManagedKdfAdapter kdf = HpkeManagedKdfAdapter.Create(Suite);
                kdf.DeriveSecrets(
                    mode: 0,
                    sharedSecret,
                    info,
                    psk: default,
                    pskId: default,
                    key,
                    baseNonce,
                    exporterSecret);

                using (HpkeManagedAeadAdapter aead = HpkeManagedAeadAdapter.Create(Suite, key))
                {
                    // Single-shot sealing uses sequence number zero, so the nonce is base_nonce.
                    // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.2
                    aead.Encrypt(
                        plaintext,
                        baseNonce,
                        associatedData,
                        ciphertext.Slice(0, plaintext.Length),
                        ciphertext.Slice(plaintext.Length));
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedSecretBuffer);
                CryptographicOperations.ZeroMemory(keyBuffer);
                CryptographicOperations.ZeroMemory(baseNonceBuffer);
                CryptographicOperations.ZeroMemory(exporterSecretBuffer);
            }
        }

        protected override void OpenCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info)
        {
            const int MaxStackSecretLength = 64;
            Span<byte> sharedSecretBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> keyBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> baseNonceBuffer = stackalloc byte[MaxStackSecretLength];
            Span<byte> exporterSecretBuffer = stackalloc byte[MaxStackSecretLength];

            try
            {
                Span<byte> sharedSecret = sharedSecretBuffer.Slice(0, Suite.KemMetadata.Nsecret);
                Span<byte> key = keyBuffer.Slice(0, Suite.AeadMetadata.Nk);
                Span<byte> baseNonce = baseNonceBuffer.Slice(0, Suite.AeadMetadata.Nn);
                Span<byte> exporterSecret = exporterSecretBuffer.Slice(0, Suite.KdfMetadata.Nh);
                _kemAdapter.Decapsulate(encapsulatedSecret, sharedSecret);

                HpkeManagedKdfAdapter kdf = HpkeManagedKdfAdapter.Create(Suite);
                kdf.DeriveSecrets(
                    mode: 0,
                    sharedSecret,
                    info,
                    psk: default,
                    pskId: default,
                    key,
                    baseNonce,
                    exporterSecret);

                using (HpkeManagedAeadAdapter aead = HpkeManagedAeadAdapter.Create(Suite, key))
                {
                    // Single-shot opening uses sequence number zero, so the nonce is base_nonce.
                    // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.2
                    aead.Decrypt(
                        ciphertext.Slice(0, plaintext.Length),
                        baseNonce,
                        associatedData,
                        ciphertext.Slice(plaintext.Length),
                        plaintext);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedSecretBuffer);
                CryptographicOperations.ZeroMemory(keyBuffer);
                CryptographicOperations.ZeroMemory(baseNonceBuffer);
                CryptographicOperations.ZeroMemory(exporterSecretBuffer);
            }
        }

        protected override HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info) =>
            throw new NotImplementedException();

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
