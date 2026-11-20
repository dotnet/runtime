// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class HpkeHybridMLKemAdapter : HpkeManagedKemAdapter
    {
        private const int HybridSeedSizeInBytes = 32;
        private const int MaxExpandedSeedSizeInBytes = 192;
        private const int MaxTraditionalPublicKeySizeInBytes = 97;
        private const int MaxTraditionalSharedSecretSizeInBytes = 48;
        private const int MLKemSharedSecretSize = 32;

        private readonly ECCurve _curve;
        private readonly HpkeKem _dhKem;
        private readonly MLKemAlgorithm _mlkemAlgorithm;
        private readonly int _traditionalSeedSizeInBytes;
        private ECDiffieHellman? _ecdh;
        private MLKem? _mlkem;
        private FixedMemoryKeyBox? _seed;

        internal HpkeHybridMLKemAdapter(HpkeSuite suite) : base(suite)
        {
            if (suite.KemAlgorithm == HpkeKem.MLKEM768_P256)
            {
                _mlkemAlgorithm = MLKemAlgorithm.MLKem768;
                _curve = ECCurve.NamedCurves.nistP256;
                _dhKem = HpkeKem.DHKEM_P256_HKDF_SHA256;
                // draft-irtf-cfrg-concrete-hybrid-kems-04, Section 3.1.1 defines P-256 Nseed as 128 bytes.
                // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-concrete-hybrid-kems-04#section-3.1.1
                _traditionalSeedSizeInBytes = 128;
            }
            else if (suite.KemAlgorithm == HpkeKem.MLKEM1024_P384)
            {
                _mlkemAlgorithm = MLKemAlgorithm.MLKem1024;
                _curve = ECCurve.NamedCurves.nistP384;
                _dhKem = HpkeKem.DHKEM_P384_HKDF_SHA384;
                // draft-irtf-cfrg-concrete-hybrid-kems-04, Section 3.1.1 defines P-384 Nseed as 48 bytes.
                // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-concrete-hybrid-kems-04#section-3.1.1
                _traditionalSeedSizeInBytes = 48;
            }
            else
            {
                Debug.Fail($"Unmapped hybrid HPKE KEM: {suite.KemAlgorithm}.");
                throw new CryptographicException();
            }

            Debug.Assert(_mlkemAlgorithm.SharedSecretSizeInBytes == MLKemSharedSecretSize);
        }

        internal override void Generate()
        {
            Debug.Assert(_seed is null);
            Span<byte> seed = stackalloc byte[HybridSeedSizeInBytes];

            try
            {
                RandomNumberGenerator.Fill(seed);
                InitializeFromSeed(seed);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(seed);
            }
        }

        internal override void DeriveKeyPair(ReadOnlySpan<byte> ikm)
        {
            Debug.Assert(_seed is null);
            Span<byte> seed = stackalloc byte[HybridSeedSizeInBytes];

            try
            {
                // draft-ietf-hpke-pq-05, Section 4 derives the hybrid KEM's 32-byte seed.
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-pq-05#section-4
                LabeledDeriveWithShake256(ikm, "DeriveKeyPair"u8, ReadOnlySpan<byte>.Empty, seed);

                // draft-irtf-cfrg-hybrid-kems-12, Sections 5.1.1 and 5.2 expand the seed into the component keys.
                // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-hybrid-kems-12#section-5.1.1
                InitializeFromSeed(seed);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(seed);
            }
        }

        internal override void Encapsulate(Span<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            Debug.Assert(_mlkem is not null);
            Debug.Assert(_ecdh is not null);
            MLKem mlkem = _mlkem;
            ECDiffieHellman ecdh = _ecdh;
            int mlkemCiphertextSize = _mlkemAlgorithm.CiphertextSizeInBytes;
            Span<byte> mlkemCiphertext = encapsulatedSecret.Slice(0, mlkemCiphertextSize);
            Span<byte> traditionalCiphertext = encapsulatedSecret.Slice(mlkemCiphertextSize);
            Span<byte> mlkemSharedSecret = stackalloc byte[MLKemSharedSecretSize];
            Span<byte> traditionalEncapsulationKey =
                stackalloc byte[MaxTraditionalPublicKeySizeInBytes]
                .Slice(0, TraditionalPublicKeySize);
            Span<byte> traditionalSharedSecret =
                stackalloc byte[MaxTraditionalSharedSecretSizeInBytes]
                .Slice(0, TraditionalSharedSecretSize);

            try
            {
                mlkem.Encapsulate(mlkemCiphertext, mlkemSharedSecret);

                using (ECDiffieHellman ephemeral = ECDiffieHellman.Create(_curve))
                using (ECDiffieHellmanPublicKey recipientPublicKey = ecdh.PublicKey)
                {
                    ExportPublicKey(ephemeral, traditionalCiphertext);
                    DeriveSecret(ephemeral, recipientPublicKey, traditionalSharedSecret);
                }

                ExportPublicKey(ecdh, traditionalEncapsulationKey);
                CompositeMLKemCombiner.Combine(
                    mlkemSharedSecret,
                    traditionalSharedSecret,
                    traditionalCiphertext,
                    traditionalEncapsulationKey,
                    Label,
                    sharedSecret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mlkemSharedSecret);
                CryptographicOperations.ZeroMemory(traditionalSharedSecret);
            }
        }

        internal override void Decapsulate(ReadOnlySpan<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            if (_seed is null)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            Debug.Assert(_mlkem is not null);
            Debug.Assert(_ecdh is not null);
            MLKem mlkem = _mlkem;
            ECDiffieHellman ecdh = _ecdh;
            int mlkemCiphertextSize = _mlkemAlgorithm.CiphertextSizeInBytes;
            ReadOnlySpan<byte> mlkemCiphertext = encapsulatedSecret.Slice(0, mlkemCiphertextSize);
            ReadOnlySpan<byte> traditionalCiphertext = encapsulatedSecret.Slice(mlkemCiphertextSize);
            Span<byte> mlkemSharedSecret = stackalloc byte[MLKemSharedSecretSize];
            Span<byte> traditionalSharedSecretBuffer = stackalloc byte[MaxTraditionalSharedSecretSizeInBytes];
            Span<byte> traditionalSharedSecret =
                traditionalSharedSecretBuffer.Slice(0, TraditionalSharedSecretSize);
            Span<byte> traditionalEncapsulationKey =
                stackalloc byte[MaxTraditionalPublicKeySizeInBytes]
                .Slice(0, TraditionalPublicKeySize);

            try
            {
                mlkem.Decapsulate(mlkemCiphertext, mlkemSharedSecret);

                using (ECDiffieHellman ephemeral = ImportPublicKey(traditionalCiphertext))
                using (ECDiffieHellmanPublicKey ephemeralPublicKey = ephemeral.PublicKey)
                {
                    DeriveSecret(ecdh, ephemeralPublicKey, traditionalSharedSecret);
                }

                ExportPublicKey(ecdh, traditionalEncapsulationKey);
                CompositeMLKemCombiner.Combine(
                    mlkemSharedSecret,
                    traditionalSharedSecret,
                    traditionalCiphertext,
                    traditionalEncapsulationKey,
                    Label,
                    sharedSecret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mlkemSharedSecret);
                CryptographicOperations.ZeroMemory(traditionalSharedSecretBuffer);
            }
        }

        internal override void ImportDecapsulationKey(ReadOnlySpan<byte> decapsulationKey)
        {
            Debug.Assert(decapsulationKey.Length == HybridSeedSizeInBytes);
            InitializeFromSeed(decapsulationKey);
        }

        internal override void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey)
        {
            Debug.Assert(_mlkem is null);
            int mlkemEncapsulationKeySize = _mlkemAlgorithm.EncapsulationKeySizeInBytes;
            ReadOnlySpan<byte> mlkemKey = encapsulationKey.Slice(0, mlkemEncapsulationKeySize);
            ReadOnlySpan<byte> traditionalKey = encapsulationKey.Slice(mlkemEncapsulationKeySize);
            MLKem mlkem = MLKem.ImportEncapsulationKey(_mlkemAlgorithm, mlkemKey);

            try
            {
                ECDiffieHellman ecdh = ImportPublicKey(traditionalKey);
                _mlkem = mlkem;
                _ecdh = ecdh;
            }
            catch
            {
                mlkem.Dispose();
                throw;
            }
        }

        internal override void ExportDecapsulationKey(Span<byte> destination)
        {
            FixedMemoryKeyBox seed = _seed ?? throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);

            if (!seed.TryCopyTo(destination, out int bytesWritten) || bytesWritten != destination.Length)
            {
                Debug.Fail("Buffer copy was incorrect size.");
                throw new CryptographicException();
            }
        }

        internal override void ExportEncapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_mlkem is not null);
            Debug.Assert(_ecdh is not null);

            int mlkemEncapsulationKeySize = _mlkemAlgorithm.EncapsulationKeySizeInBytes;
            _mlkem.ExportEncapsulationKey(destination.Slice(0, mlkemEncapsulationKeySize));
            ExportPublicKey(_ecdh, destination.Slice(mlkemEncapsulationKeySize));
        }

        public override void Dispose()
        {
            _mlkem?.Dispose();
            _ecdh?.Dispose();
            _seed?.Dispose();
        }

        private void InitializeFromSeed(ReadOnlySpan<byte> seed)
        {
            Debug.Assert(_mlkem is null);
            Debug.Assert(_ecdh is null);
            Debug.Assert(_seed is null);
            Debug.Assert(seed.Length == HybridSeedSizeInBytes);

            (MLKem mlkem, ECDiffieHellman ecdh) = ExpandSeed(seed);
            FixedMemoryKeyBox? seedBox = null;

            try
            {
                seedBox = new FixedMemoryKeyBox(seed);
                _mlkem = mlkem;
                _ecdh = ecdh;
                _seed = seedBox;
            }
            catch
            {
                mlkem.Dispose();
                ecdh.Dispose();
                seedBox?.Dispose();
                throw;
            }
        }

        private (MLKem MLKem, ECDiffieHellman ECDiffieHellman) ExpandSeed(ReadOnlySpan<byte> seed)
        {
            // expandDecapsKeyG from draft-irtf-cfrg-hybrid-kems-12, Section 5.1.1.
            // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-hybrid-kems-12#section-5.1.1
            int expandedSeedSize = checked(_mlkemAlgorithm.PrivateSeedSizeInBytes + _traditionalSeedSizeInBytes);
            Debug.Assert(expandedSeedSize <= MaxExpandedSeedSizeInBytes);
            Span<byte> expandedSeedBuffer = stackalloc byte[MaxExpandedSeedSizeInBytes];
            Span<byte> expandedSeed = expandedSeedBuffer.Slice(0, expandedSeedSize);

            try
            {
                Shake256.HashData(seed, expandedSeed);

                ReadOnlySpan<byte> mlkemSeed = expandedSeed.Slice(0, _mlkemAlgorithm.PrivateSeedSizeInBytes);
                ReadOnlySpan<byte> traditionalSeed = expandedSeed.Slice(_mlkemAlgorithm.PrivateSeedSizeInBytes);
                ReadOnlySpan<byte> scalar = SelectScalar(traditionalSeed);
                MLKem mlkem = MLKem.ImportPrivateSeed(_mlkemAlgorithm, mlkemSeed);

                try
                {
                    using (PinAndClear.CopyAndTrack(scalar, out byte[] d))
                    {
                        ECDiffieHellman ecdh = ECDiffieHellman.Create(new ECParameters
                        {
                            Curve = _curve,
                            D = d,
                        });

                        return (mlkem, ecdh);
                    }
                }
                catch
                {
                    mlkem.Dispose();
                    throw;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expandedSeedBuffer);
            }
        }

        // RandomScalar from draft-irtf-cfrg-concrete-hybrid-kems-04, Section 3.1.1.
        // The seed is split into scalar-sized candidates, interpreted as big-endian integers. The first candidate
        // in the range (0, order) is selected. P-256 provides four candidates; P-384 needs only one because its
        // rejection probability is already negligible.
        // https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-concrete-hybrid-kems-04#section-3.1.1
        private ReadOnlySpan<byte> SelectScalar(ReadOnlySpan<byte> seed)
        {
            ReadOnlySpan<byte> order = HpkeECDiffieHellmanKemAdapter.GetOrder(_dhKem);
            int scalarSize = order.Length;

            for (int offset = 0; offset <= seed.Length - scalarSize; offset += scalarSize)
            {
                ReadOnlySpan<byte> candidate = seed.Slice(offset, scalarSize);

                if (HpkeECDiffieHellmanKemAdapter.IsValidScalar(candidate, order))
                {
                    return candidate;
                }
            }

            throw new CryptographicException(SR.Cryptography_HpkeKeyDerivationFailed);
        }

        private ECDiffieHellman ImportPublicKey(ReadOnlySpan<byte> source)
        {
            AsymmetricAlgorithmHelpers.DecodeFromUncompressedAnsiX963Key(
                source,
                hasPrivateKey: false,
                out ECParameters parameters);
            parameters.Curve = _curve;

            try
            {
                return ECDiffieHellman.Create(parameters);
            }
            catch (PlatformNotSupportedException exception)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey, exception);
            }
        }

        private void ExportPublicKey(ECDiffieHellman key, Span<byte> destination)
        {
            ECParameters parameters = key.ExportParameters(includePrivateParameters: false);
            byte[]? x = parameters.Q.X;
            byte[]? y = parameters.Q.Y;
            int fieldSize = TraditionalSharedSecretSize;

            if (x is null ||
                y is null ||
                x.Length != fieldSize ||
                y.Length != fieldSize ||
                !parameters.Curve.IsNamed ||
                parameters.Curve.Oid.Value != _curve.Oid.Value)
            {
                Debug.Fail("ECDH exported unexpected public key parameters.");
                throw new CryptographicException();
            }

            AsymmetricAlgorithmHelpers.EncodeToUncompressedAnsiX963Key(
                x,
                y,
                ReadOnlySpan<byte>.Empty,
                destination);
        }

        private static void DeriveSecret(
            ECDiffieHellman ownKey,
            ECDiffieHellmanPublicKey otherParty,
            Span<byte> destination)
        {
            byte[] secret = ownKey.DeriveRawSecretAgreement(otherParty);

            using (PinAndClear.Track(secret))
            {
                if (secret.Length != destination.Length)
                {
                    Debug.Fail("ECDH produced an unexpected shared secret length.");
                    throw new CryptographicException();
                }

                secret.CopyTo(destination);
            }
        }

        // SEC1 uncompressed points contain a 0x04 format byte followed by X and Y, each one field element wide.
        // draft-irtf-cfrg-concrete-hybrid-kems-04, Section 3.1.1 defines this encoding for P-256 and P-384.
        private int TraditionalPublicKeySize => 1 + 2 * TraditionalSharedSecretSize;

        private int TraditionalSharedSecretSize => Suite.KemAlgorithm switch
        {
            HpkeKem.MLKEM768_P256 => 32,
            HpkeKem.MLKEM1024_P384 => 48,
            _ => throw new UnreachableException(),
        };

        private ReadOnlySpan<byte> Label => Suite.KemAlgorithm switch
        {
            HpkeKem.MLKEM768_P256 => "MLKEM768-P256"u8,
            HpkeKem.MLKEM1024_P384 => "MLKEM1024-P384"u8,
            _ => throw new UnreachableException(),
        };
    }
}
