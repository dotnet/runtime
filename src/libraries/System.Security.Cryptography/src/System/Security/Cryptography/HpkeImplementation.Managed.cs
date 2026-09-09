// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

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

        internal static HpkeImplementation ImportDecapsulationKeyImpl(HpkeSuite suite, ReadOnlySpan<byte> source)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.ImportDecapsulationKey(source);
                return new HpkeImplementation(suite, adapter);
            }
            catch
            {
                adapter.Dispose();
                throw;
            }
        }

        internal static HpkeImplementation ImportEncapsulationKeyImpl(HpkeSuite suite, ReadOnlySpan<byte> source)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.ImportEncapsulationKey(source);
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
                CryptographicOperations.ZeroMemory(exporterSecretBuffer);
            }
        }

        protected override HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info)
        {
            return CreateSenderContext(mode: 0, encapsulatedSecret, info, psk: default, pskId: default);
        }

        protected override HpkeSender CreatePskSenderCore(
            Span<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId)
        {
            return CreateSenderContext(mode: 1, encapsulatedSecret, info, psk, pskId);
        }

        private HpkeSenderImplementation CreateSenderContext(
            byte mode,
            Span<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId)
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
                    mode,
                    sharedSecret,
                    info,
                    psk,
                    pskId,
                    key,
                    baseNonce,
                    exporterSecret);

                HpkeManagedAeadAdapter aead = HpkeManagedAeadAdapter.Create(Suite, key);

                try
                {
                    return new HpkeSenderImplementation(Suite, aead, kdf, baseNonce, exporterSecret);
                }
                catch
                {
                    aead.Dispose();
                    throw;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedSecretBuffer);
                CryptographicOperations.ZeroMemory(keyBuffer);
                CryptographicOperations.ZeroMemory(exporterSecretBuffer);
            }
        }

        protected override HpkeRecipient CreateRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info) =>
            CreateRecipientContext(mode: 0, encapsulatedSecret, info, psk: default, pskId: default);

        protected override HpkeRecipient CreatePskRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId) =>
            CreateRecipientContext(mode: 1, encapsulatedSecret, info, psk, pskId);

        private HpkeRecipientImplementation CreateRecipientContext(
            byte mode,
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId)
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
                    mode,
                    sharedSecret,
                    info,
                    psk,
                    pskId,
                    key,
                    baseNonce,
                    exporterSecret);

                HpkeManagedAeadAdapter aead = HpkeManagedAeadAdapter.Create(Suite, key);

                try
                {
                    return new HpkeRecipientImplementation(Suite, aead, kdf, baseNonce, exporterSecret);
                }
                catch
                {
                    aead.Dispose();
                    throw;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedSecretBuffer);
                CryptographicOperations.ZeroMemory(keyBuffer);
                CryptographicOperations.ZeroMemory(exporterSecretBuffer);
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

    internal sealed class HpkeSenderImplementation : HpkeSender
    {
        private readonly HpkeManagedAeadAdapter _aeadAdapter;
        private readonly HpkeManagedKdfAdapter _kdfAdapter;
        private readonly byte[] _baseNonce;
        private readonly FixedMemoryKeyBox _exporterSecret;
        private ulong _sequenceNumber;
        private ConcurrencyBlock _block;

        internal HpkeSenderImplementation(
            HpkeSuite suite,
            HpkeManagedAeadAdapter aeadAdapter,
            HpkeManagedKdfAdapter kdfAdapter,
            ReadOnlySpan<byte> baseNonce,
            ReadOnlySpan<byte> exporterSecret) : base(suite)
        {
            Debug.Assert(baseNonce.Length == suite.AeadMetadata.Nn);
            Debug.Assert(baseNonce.Length >= sizeof(ulong));
            Debug.Assert(exporterSecret.Length == suite.KdfMetadata.Nh);

            _baseNonce = baseNonce.ToArray();
            _exporterSecret = new FixedMemoryKeyBox(exporterSecret);
            _aeadAdapter = aeadAdapter;
            _kdfAdapter = kdfAdapter;
        }

        protected override void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData)
        {
            // While this API is not documented as thread-safe, we block concurrent calls to prevent silent nonce reuse.
            using (ConcurrencyBlock.Enter(ref _block))
            {
                if (_sequenceNumber == ulong.MaxValue)
                {
                    throw new CryptographicException(SR.Cryptography_HpkeMessageLimitReached);
                }

                const int MaxStackNonceLength = 12;
                Span<byte> nonceBuffer = stackalloc byte[MaxStackNonceLength];
                Span<byte> nonce = nonceBuffer.Slice(0, _baseNonce.Length);
                _baseNonce.AsSpan().CopyTo(nonce);

                // The zero-padded sequence number only affects the final eight nonce bytes.
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.2
                Span<byte> sequenceBytes = nonce.Slice(nonce.Length - sizeof(ulong));
                BinaryPrimitives.WriteUInt64BigEndian(
                    sequenceBytes,
                    BinaryPrimitives.ReadUInt64BigEndian(sequenceBytes) ^ _sequenceNumber);

                _aeadAdapter.Encrypt(
                    plaintext,
                    nonce,
                    associatedData,
                    ciphertext.Slice(0, plaintext.Length),
                    ciphertext.Slice(plaintext.Length));
                _sequenceNumber++;
            }
        }

        protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            _exporterSecret.UseKey(
                _kdfAdapter,
                exporterContext,
                destination,
                static (kdf, context, output, key) => kdf.ExportSecret(key, context, output));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _aeadAdapter.Dispose();
                }
                finally
                {
                    _exporterSecret.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    internal sealed class HpkeRecipientImplementation : HpkeRecipient
    {
        private readonly HpkeManagedAeadAdapter _aeadAdapter;
        private readonly HpkeManagedKdfAdapter _kdfAdapter;
        private readonly byte[] _baseNonce;
        private readonly FixedMemoryKeyBox _exporterSecret;
        private ulong _sequenceNumber;

        internal HpkeRecipientImplementation(
            HpkeSuite suite,
            HpkeManagedAeadAdapter aeadAdapter,
            HpkeManagedKdfAdapter kdfAdapter,
            ReadOnlySpan<byte> baseNonce,
            ReadOnlySpan<byte> exporterSecret) : base(suite)
        {
            Debug.Assert(baseNonce.Length == suite.AeadMetadata.Nn);
            Debug.Assert(baseNonce.Length >= sizeof(ulong));
            Debug.Assert(exporterSecret.Length == suite.KdfMetadata.Nh);

            _baseNonce = baseNonce.ToArray();
            _exporterSecret = new FixedMemoryKeyBox(exporterSecret);
            _aeadAdapter = aeadAdapter;
            _kdfAdapter = kdfAdapter;
        }

        protected override void OpenCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            if (_sequenceNumber == ulong.MaxValue)
            {
                throw new CryptographicException(SR.Cryptography_HpkeMessageLimitReached);
            }

            const int MaxStackNonceLength = 12;
            Span<byte> nonceBuffer = stackalloc byte[MaxStackNonceLength];
            Span<byte> nonce = nonceBuffer.Slice(0, _baseNonce.Length);
            _baseNonce.AsSpan().CopyTo(nonce);

            // The zero-padded sequence number only affects the final eight nonce bytes.
            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.2
            Span<byte> sequenceBytes = nonce.Slice(nonce.Length - sizeof(ulong));
            BinaryPrimitives.WriteUInt64BigEndian(
                sequenceBytes,
                BinaryPrimitives.ReadUInt64BigEndian(sequenceBytes) ^ _sequenceNumber);

            _aeadAdapter.Decrypt(
                ciphertext.Slice(0, plaintext.Length),
                nonce,
                associatedData,
                ciphertext.Slice(plaintext.Length),
                plaintext);
            _sequenceNumber++;
        }

        protected override void ExportCore(ReadOnlySpan<byte> exporterContext, Span<byte> destination)
        {
            _exporterSecret.UseKey(
                _kdfAdapter,
                exporterContext,
                destination,
                static (kdf, context, output, key) => kdf.ExportSecret(key, context, output));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    _aeadAdapter.Dispose();
                }
                finally
                {
                    _exporterSecret.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
