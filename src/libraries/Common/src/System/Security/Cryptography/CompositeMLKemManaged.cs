// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLKemManaged : CompositeMLKem
    {
        private static readonly Dictionary<CompositeMLKemAlgorithm, AlgorithmMetadata> s_algorithmMetadata = CreateAlgorithmMetadata();
        private static readonly ConcurrentDictionary<CompositeMLKemAlgorithm, bool> s_algorithmSupport = new();

        private MLKem _mlkem;
        private TraditionalKem _traditionalKem;
        private readonly bool _hasDecapsulationKey;

        private AlgorithmMetadata AlgorithmDetails => field ??= s_algorithmMetadata[Algorithm];

        private CompositeMLKemManaged(CompositeMLKemAlgorithm algorithm, MLKem mlkem, TraditionalKem traditionalKem, bool hasDecapsulationKey)
            : base(algorithm)
        {
            _mlkem = mlkem;
            _traditionalKem = traditionalKem;
            _hasDecapsulationKey = hasDecapsulationKey;
        }

        internal static bool IsAlgorithmSupportedImpl(CompositeMLKemAlgorithm algorithm)
        {
            return s_algorithmSupport.GetOrAdd(
                algorithm,
                static alg =>
                {
                    AlgorithmMetadata metadata = s_algorithmMetadata[alg];

                    return MLKemImplementation.IsAlgorithmSupported(metadata.MLKemAlgorithm) && metadata.TraditionalKemAlgorithm switch
                    {
                        RsaKemAlgorithm rsaAlgorithm => RsaKem.IsAlgorithmSupported(rsaAlgorithm),
                        ECDiffieHellmanKemAlgorithm ecdhAlgorithm => ECDiffieHellmanKem.IsAlgorithmSupported(ecdhAlgorithm),
                        XDiffieHellmanKemAlgorithm xdhAlgorithm => XDiffieHellmanKem.IsAlgorithmSupported(xdhAlgorithm),
                    };
                });
        }

        internal static CompositeMLKem GenerateKeyImpl(CompositeMLKemAlgorithm algorithm)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-kem-19, 3.1
            // 1. Generate component keys
            //
            //    mlkemSeed = Random(64)
            //    (mlkemPK, mlkemSK) = ML-KEM.KeyGen_internal(
            //                                    mlkemSeed[:32],
            //                                    mlkemSeed[32:] )
            //    (tradPK, tradSK) = Trad.KeyGen()
            //
            // 2. Check for component key gen failure
            //
            //    if NOT (mlkemPK, mlkemSK) or NOT (tradPK, tradSK):
            //      output "Key generation error"

            MLKem mlkem = MLKem.GenerateKey(metadata.MLKemAlgorithm);

            TraditionalKem? tradKey;

            try
            {
                tradKey = metadata.TraditionalKemAlgorithm switch
                {
                    RsaKemAlgorithm rsaAlgorithm => RsaKem.GenerateKey(rsaAlgorithm),
                    ECDiffieHellmanKemAlgorithm ecdhAlgorithm => ECDiffieHellmanKem.GenerateKey(ecdhAlgorithm),
                    XDiffieHellmanKemAlgorithm xdhAlgorithm => XDiffieHellmanKem.GenerateKey(xdhAlgorithm),
                };
            }
            catch
            {
                mlkem.Dispose();
                throw;
            }

            // 3. Output the composite public and private keys
            //
            //    pk = SerializePublicKey(mlkemPK, tradPK)
            //    sk = SerializePrivateKey(mlkemSeed, tradSK)
            //    return (pk, sk)

            return new CompositeMLKemManaged(algorithm, mlkem, tradKey, hasDecapsulationKey: true);
        }

        internal static CompositeMLKem ImportEncapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-kem-19, 4.1
            // 1. Parse each constituent encoded public key.
            //    The length of the mlkemPK is known based on the size of
            //    the ML-KEM component key length specified by the Object ID.
            //
            //    switch ML-KEM do
            //       case ML-KEM-768:
            //         mlkemPK = bytes[:1184]
            //         tradPK  = bytes[1184:]
            //       case ML-KEM-1024:
            //         mlkemPK = bytes[:1568]
            //         tradPK  = bytes[1568:]
            //
            //    Note that while ML-KEM has fixed-length keys, RSA
            //    may not, depending on encoding, so rigorous length-checking
            //    of the overall composite key is not always possible.
            //
            // 2. Output the component public keys
            //
            // output (mlkemPK, tradPK)

            ReadOnlySpan<byte> mlkemKey = source.Slice(0, metadata.MLKemAlgorithm.EncapsulationKeySizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(metadata.MLKemAlgorithm.EncapsulationKeySizeInBytes);

            MLKem mlkem = MLKem.ImportEncapsulationKey(metadata.MLKemAlgorithm, mlkemKey);

            TraditionalKem? traditionalKem;

            try
            {
                traditionalKem = metadata.TraditionalKemAlgorithm switch
                {
                    RsaKemAlgorithm rsaAlgorithm => RsaKem.ImportPublicKey(rsaAlgorithm, tradKey),
                    ECDiffieHellmanKemAlgorithm ecdhAlgorithm => ECDiffieHellmanKem.ImportPublicKey(ecdhAlgorithm, tradKey),
                    XDiffieHellmanKemAlgorithm xdhAlgorithm => XDiffieHellmanKem.ImportPublicKey(xdhAlgorithm, tradKey),
                };
            }
            catch
            {
                mlkem.Dispose();
                throw;
            }

            return new CompositeMLKemManaged(algorithm, mlkem, traditionalKem, hasDecapsulationKey: false);
        }

        internal static CompositeMLKem ImportDecapsulationKeyImpl(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsAlgorithmSupportedImpl(algorithm));

            AlgorithmMetadata metadata = s_algorithmMetadata[algorithm];

            // draft-ietf-lamps-pq-composite-kem-19, 4.2
            // 1. Parse the ML-KEM seed, which is always a 64 byte seed
            //    for all parameter sets.
            //
            //    mlkemSeed = bytes[:64]
            //    tradSK    = bytes[64:]
            //
            // 2. Output the component private keys
            //
            //    output (mlkemSeed, tradKey)

            ReadOnlySpan<byte> mlkemKey = source.Slice(0, metadata.MLKemAlgorithm.PrivateSeedSizeInBytes);
            ReadOnlySpan<byte> tradKey = source.Slice(metadata.MLKemAlgorithm.PrivateSeedSizeInBytes);

            MLKem mlkem = MLKem.ImportPrivateSeed(metadata.MLKemAlgorithm, mlkemKey);

            TraditionalKem? traditionalKem;

            try
            {
                traditionalKem = metadata.TraditionalKemAlgorithm switch
                {
                    RsaKemAlgorithm rsaAlgorithm => RsaKem.ImportPrivateKey(rsaAlgorithm, tradKey),
                    ECDiffieHellmanKemAlgorithm ecdhAlgorithm => ECDiffieHellmanKem.ImportPrivateKey(ecdhAlgorithm, tradKey),
                    XDiffieHellmanKemAlgorithm xdhAlgorithm => XDiffieHellmanKem.ImportPrivateKey(xdhAlgorithm, tradKey),
                };
            }
            catch
            {
                mlkem.Dispose();
                throw;
            }

            return new CompositeMLKemManaged(algorithm, mlkem, traditionalKem, hasDecapsulationKey: true);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            // draft-ietf-lamps-pq-composite-kem-19, 3.2
            // 1. Separate the public keys.
            //
            //    (mlkemPK, tradPK) = DeserializePublicKey(pk)

            /* no-op */

            // 2. Perform the respective component Encap operations according to
            //    their algorithm specifications.
            //
            //    (mlkemCT, mlkemSS) = ML-KEM.Encaps(mlkemPK)
            //    (tradCT, tradSS) = TradKEM.Encaps(tradPK)
            //
            // 3. If either ML-KEM.Encaps() or TradKEM.Encaps() return an error,
            //    then this process must return an error.
            //
            //    if NOT (mlkemCT, mlkemSS) or NOT (tradCT, tradSS):
            //      output "Encapsulation error"
            //
            // 4. Encode the ciphertext
            //
            //     ct = SerializeCiphertext(mlkemCT, tradCT)

            int mlkemCiphertextSize = AlgorithmDetails.MLKemAlgorithm.CiphertextSizeInBytes;

            Span<byte> mlkemCT = ciphertext.Slice(0, mlkemCiphertextSize);
            Span<byte> tradCT = ciphertext.Slice(mlkemCiphertextSize);

            const int MaxMLKemSharedSecretSize = 32;
            const int MaxTraditionalSharedSecretSize = 66;

            int mlkemSharedSecretSize = AlgorithmDetails.MLKemAlgorithm.SharedSecretSizeInBytes;
            int traditionalSharedSecretSize = AlgorithmDetails.TraditionalKemAlgorithm.SharedSecretSizeInBytes;
            Debug.Assert(mlkemSharedSecretSize <= MaxMLKemSharedSecretSize, $"Increase {nameof(MaxMLKemSharedSecretSize)} to utilize the stackalloc below.");
            Debug.Assert(traditionalSharedSecretSize <= MaxTraditionalSharedSecretSize, $"Increase {nameof(MaxTraditionalSharedSecretSize)} to utilize the stackalloc below.");

            Span<byte> mlkemSS = mlkemSharedSecretSize <= MaxMLKemSharedSecretSize
                ? (stackalloc byte[MaxMLKemSharedSecretSize]).Slice(0, mlkemSharedSecretSize)
                : new byte[mlkemSharedSecretSize]; // We should never get here, but handle it anyway.

            Span<byte> tradSS = traditionalSharedSecretSize <= MaxTraditionalSharedSecretSize
                ? (stackalloc byte[MaxTraditionalSharedSecretSize]).Slice(0, traditionalSharedSecretSize)
                : new byte[traditionalSharedSecretSize]; // We should never get here, but handle it anyway.

            try
            {
                _mlkem.Encapsulate(mlkemCT, mlkemSS);
                _traditionalKem.Encapsulate(tradCT, tradSS);

                //  5. Combine the KEM secrets and additional context to yield the
                //     composite shared secret key.
                //
                //       ss = KemCombiner(mlkemSS, tradSS, tradCT, tradPK, Label)

                CombineSecrets(mlkemSS, tradSS, tradCT, sharedSecret);

                // 6. Output composite shared secret key and ciphertext.
                //
                //    return (ss, ct)
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mlkemSS);
                CryptographicOperations.ZeroMemory(tradSS);
            }
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            // draft-ietf-lamps-pq-composite-kem-19, 3.3
            // 1. Separate the private keys and ciphertexts
            //
            //    (mlkemSeed, tradSK) = DeserializePrivateKey(sk)
            //    (_, mlkemSK) = ML-KEM.KeyGen(mlkemSeed[:32], mlkemSeed[32:])
            //    (mlkemCT, tradCT) = DeserializeCiphertext(ct)

            int mlkemCiphertextSize = AlgorithmDetails.MLKemAlgorithm.CiphertextSizeInBytes;
            ReadOnlySpan<byte> mlkemCT = ciphertext.Slice(0, mlkemCiphertextSize);
            ReadOnlySpan<byte> tradCT = ciphertext.Slice(mlkemCiphertextSize);

            // 2. Perform the respective component Decap operations according
            //    to their algorithm specifications.
            //
            //    mlkemSS = ML-KEM.Decaps(mlkemSK, mlkemCT)
            //    tradSS  = TradKEM.Decaps(tradSK, tradCT)
            //
            // 3. If either ML-KEM.Decaps() or TradKEM.Decaps() return an error,
            //    then this process must return an error.
            //
            //    if NOT mlkemSS or NOT tradSS:
            //      output "Decapsulation error"

            const int MaxMLKemSharedSecretSize = 32;
            const int MaxTraditionalSharedSecretSize = 66;

            int mlkemSharedSecretSize = AlgorithmDetails.MLKemAlgorithm.SharedSecretSizeInBytes;
            int traditionalSharedSecretSize = AlgorithmDetails.TraditionalKemAlgorithm.SharedSecretSizeInBytes;
            Debug.Assert(mlkemSharedSecretSize <= MaxMLKemSharedSecretSize, $"Increase {nameof(MaxMLKemSharedSecretSize)} to utilize the stackalloc below.");
            Debug.Assert(traditionalSharedSecretSize <= MaxTraditionalSharedSecretSize, $"Increase {nameof(MaxTraditionalSharedSecretSize)} to utilize the stackalloc below.");

            Span<byte> mlkemSS = mlkemSharedSecretSize <= MaxMLKemSharedSecretSize
                ? (stackalloc byte[MaxMLKemSharedSecretSize]).Slice(0, mlkemSharedSecretSize)
                : new byte[mlkemSharedSecretSize]; // We should never get here, but handle it anyway.

            Span<byte> tradSS = traditionalSharedSecretSize <= MaxTraditionalSharedSecretSize
                ? (stackalloc byte[MaxTraditionalSharedSecretSize]).Slice(0, traditionalSharedSecretSize)
                : new byte[traditionalSharedSecretSize]; // We should never get here, but handle it anyway.

            try
            {
                _mlkem.Decapsulate(mlkemCT, mlkemSS);
                _traditionalKem.Decapsulate(tradCT, tradSS);

                // 4. Combine the KEM secrets and additional context to yield the
                //    composite shared secret key.
                //
                //    ss = KemCombiner(mlkemSS, tradSS, tradCT, tradPK, Label)

                CombineSecrets(mlkemSS, tradSS, tradCT, sharedSecret);

                // 5. Output composite shared secret key.
                //
                //    return ss
            }
            finally
            {
                CryptographicOperations.ZeroMemory(mlkemSS);
                CryptographicOperations.ZeroMemory(tradSS);
            }
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return TryExportPkcs8FromExportedDecapsulationKey(destination, out bytesWritten);
        }

        protected override int ExportEncapsulationKeyCore(Span<byte> destination)
        {
            // draft-ietf-lamps-pq-composite-kem-19, 4.1
            // 1. Combine and output the encoded public key
            //
            //    output mlkemPK || tradPK

            int mlkemEncapsulationKeySize = AlgorithmDetails.MLKemAlgorithm.EncapsulationKeySizeInBytes;
            int bytesWritten = 0;

            _mlkem.ExportEncapsulationKey(destination.Slice(0, mlkemEncapsulationKeySize));
            bytesWritten += mlkemEncapsulationKeySize;

            bytesWritten += _traditionalKem.ExportPublicKey(destination.Slice(mlkemEncapsulationKeySize));

            return bytesWritten;
        }

        protected override int ExportDecapsulationKeyCore(Span<byte> destination)
        {
            if (!_hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            // draft-ietf-lamps-pq-composite-kem-19, 4.2
            // 1. Combine and output the encoded private key.
            //
            //    output mlkemSeed || tradSK

            try
            {
                int mlkemDecapsulationKeySize = AlgorithmDetails.MLKemAlgorithm.PrivateSeedSizeInBytes;
                int bytesWritten = 0;

                _mlkem.ExportPrivateSeed(destination.Slice(0, mlkemDecapsulationKeySize));
                bytesWritten += mlkemDecapsulationKeySize;

                bytesWritten += _traditionalKem.ExportPrivateKey(destination.Slice(mlkemDecapsulationKeySize));

                return bytesWritten;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(destination);
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mlkem?.Dispose();
                _mlkem = null!;

                _traditionalKem?.Dispose();
                _traditionalKem = null!;
            }

            base.Dispose(disposing);
        }

        private void CombineSecrets(
            ReadOnlySpan<byte> mlkemSharedSecret,
            ReadOnlySpan<byte> traditionalSharedSecret,
            ReadOnlySpan<byte> traditionalCiphertext,
            Span<byte> destination)
        {
            // draft-ietf-lamps-pq-composite-kem-19, 3.4
            // ss = SHA3-256(mlkemSS || tradSS || tradCT || tradPK || Label)
            //
            // return ss

            int maxTraditionalPublicKeySize =
                Algorithm.MaxEncapsulationKeySizeInBytes - AlgorithmDetails.MLKemAlgorithm.EncapsulationKeySizeInBytes;

            using (CryptoPoolLease lease = CryptoPoolLease.Rent(maxTraditionalPublicKeySize, skipClear: true))
            using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA3_256))
            {
                int traditionalPublicKeySize = _traditionalKem.ExportPublicKey(lease.Span);

                hash.AppendData(mlkemSharedSecret);
                hash.AppendData(traditionalSharedSecret);
                hash.AppendData(traditionalCiphertext);
                hash.AppendData(lease.Span.Slice(0, traditionalPublicKeySize));
                hash.AppendData(AlgorithmDetails.Label);

                if (!hash.TryGetHashAndReset(destination, out int bytesWritten) || bytesWritten != destination.Length)
                {
                    Debug.Fail("SHA3-256 produced an unexpected output length.");
                    throw new CryptographicException();
                }
            }
        }

#if DESIGNTIMEINTERFACES
        private interface ITraditionalKemFactory<TTraditionalKem, TAlgorithmKemAlgorithm>
            where TTraditionalKem : TraditionalKem, ITraditionalKemFactory<TTraditionalKem, TAlgorithmKemAlgorithm>
        {
            internal static abstract bool IsAlgorithmSupported(TAlgorithmKemAlgorithm algorithm);
            internal static abstract TTraditionalKem GenerateKey(TAlgorithmKemAlgorithm algorithm);
            internal static abstract TTraditionalKem ImportPrivateKey(TAlgorithmKemAlgorithm algorithm, ReadOnlySpan<byte> source);
            internal static abstract TTraditionalKem ImportPublicKey(TAlgorithmKemAlgorithm algorithm, ReadOnlySpan<byte> source);
        }
#endif

        private closed class TraditionalKem : IDisposable
        {
            private bool _disposed;

            internal abstract void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret);
            internal abstract void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);
            internal abstract int ExportPublicKey(Span<byte> destination);
            internal abstract int ExportPrivateKey(Span<byte> destination);

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }
            }

            protected virtual void Dispose(bool disposing)
            {
            }
        }

        private static Dictionary<CompositeMLKemAlgorithm, AlgorithmMetadata> CreateAlgorithmMetadata()
        {
            const int Count = 12;

            Dictionary<CompositeMLKemAlgorithm, AlgorithmMetadata> algorithmMetadata = new(Count)
            {
                {
                    CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048,
                    new(MLKemAlgorithm.MLKem768, new RsaKemAlgorithm(2048), [.."MLKEM768-RSAOAEP2048"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithRsaOaep3072,
                    new(MLKemAlgorithm.MLKem768, new RsaKemAlgorithm(3072), [.."MLKEM768-RSAOAEP3072"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithRsaOaep4096,
                    new(MLKemAlgorithm.MLKem768, new RsaKemAlgorithm(4096), [.."MLKEM768-RSAOAEP4096"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithX25519,
                    new(MLKemAlgorithm.MLKem768, new XDiffieHellmanKemAlgorithm(IsX25519: true), [0x5C, 0x2E, 0x2F, 0x2F, 0x5E, 0x5C])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256,
                    new(MLKemAlgorithm.MLKem768, new ECDiffieHellmanKemAlgorithm(Oids.secp256r1, 256, 256), [.."MLKEM768-P256"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP384,
                    new(MLKemAlgorithm.MLKem768, new ECDiffieHellmanKemAlgorithm(Oids.secp384r1, 384, 384), [.."MLKEM768-P384"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanBrainpoolP256r1,
                    new(MLKemAlgorithm.MLKem768, new ECDiffieHellmanKemAlgorithm(Oids.brainpoolP256r1, 256, 256), [.."MLKEM768-BP256"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem1024WithRsaOaep3072,
                    new(MLKemAlgorithm.MLKem1024, new RsaKemAlgorithm(3072), [.."MLKEM1024-RSAOAEP3072"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384,
                    new(MLKemAlgorithm.MLKem1024, new ECDiffieHellmanKemAlgorithm(Oids.secp384r1, 384, 384), [.."MLKEM1024-P384"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanBrainpoolP384r1,
                    new(MLKemAlgorithm.MLKem1024, new ECDiffieHellmanKemAlgorithm(Oids.brainpoolP384r1, 384, 384), [.."MLKEM1024-BP384"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem1024WithX448,
                    new(MLKemAlgorithm.MLKem1024, new XDiffieHellmanKemAlgorithm(IsX25519: false), [.."MLKEM1024-X448"u8])
                },
                {
                    CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP521,
                    new(MLKemAlgorithm.MLKem1024, new ECDiffieHellmanKemAlgorithm(Oids.secp521r1, 521, 521), [.."MLKEM1024-P521"u8])
                },
            };

            Debug.Assert(algorithmMetadata.Count == Count);
            return algorithmMetadata;
        }

        private sealed class AlgorithmMetadata(
            MLKemAlgorithm mlkemAlgorithm,
            TraditionalKemAlgorithm traditionalAlgorithm,
            byte[] label)
        {
            internal MLKemAlgorithm MLKemAlgorithm { get; } = mlkemAlgorithm;
            internal TraditionalKemAlgorithm TraditionalKemAlgorithm { get; } = traditionalAlgorithm;
            internal byte[] Label { get; } = label;
        }

        private closed record TraditionalKemAlgorithm
        {
            internal abstract int SharedSecretSizeInBytes { get; }
        }

        private sealed record RsaKemAlgorithm(int KeySizeInBits)
            : TraditionalKemAlgorithm
        {
            internal const int SecretSizeInBytes = 32;

            internal override int SharedSecretSizeInBytes => SecretSizeInBytes;
        }

        private sealed record ECDiffieHellmanKemAlgorithm(string CurveOidValue, int FieldSizeInBits, int OrderSizeInBits)
            : TraditionalKemAlgorithm
        {
            internal ECCurve Curve { get; } = ECCurve.CreateFromValue(CurveOidValue);
            internal int FieldSizeInBytes { get; } = (FieldSizeInBits + 7) / 8;
            internal int OrderSizeInBytes { get; } = (OrderSizeInBits + 7) / 8;

            internal override int SharedSecretSizeInBytes => FieldSizeInBytes;
        }

        private sealed record XDiffieHellmanKemAlgorithm(bool IsX25519)
            : TraditionalKemAlgorithm
        {
            private const int X448SecretSizeInBytes = 56;

            internal override int SharedSecretSizeInBytes =>
                IsX25519 ? X25519DiffieHellman.SecretAgreementSizeInBytes : X448SecretSizeInBytes;
        }
    }
}
