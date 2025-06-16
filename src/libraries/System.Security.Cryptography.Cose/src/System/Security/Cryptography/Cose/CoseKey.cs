// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseKey
    {
        internal CoseAlgorithm Algorithm { get; }
        internal KeyType KeyType { get; }
        internal HashAlgorithmName? HashAlgorithm { get; }

        internal RSASignaturePadding? RSASignaturePadding { get; private set; }

        // only used for backward compatibility
        internal AsymmetricAlgorithm? AsymmetricAlgorithm { get; private set; }

        private RSA? _rsaKey;
        private ECDsa? _ecdsaKey;

        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        private MLDsa? _mldsaKey;

        private CoseKey(KeyType keyType, CoseAlgorithm algorithm, HashAlgorithmName? hashAlgorithm)
        {
            KeyType = keyType;
            Algorithm = algorithm;
            HashAlgorithm = hashAlgorithm;
        }

        public static CoseKey FromKey(RSA key, RSASignaturePadding signaturePadding, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(signaturePadding);
            ArgumentNullException.ThrowIfNull(hashAlgorithm.Name);

            CoseAlgorithm coseAlgorithm = GetRSAAlgorithm(signaturePadding, hashAlgorithm);
            CoseKey coseKey = new(KeyType.RSA, coseAlgorithm, hashAlgorithm);
            coseKey.AsymmetricAlgorithm = key;
            coseKey.RSASignaturePadding = signaturePadding;
            coseKey._rsaKey = key;
            return coseKey;
        }

        public static CoseKey FromKey(ECDsa key, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(hashAlgorithm.Name);

            CoseAlgorithm coseAlgorithm = GetECDsaAlgorithm(hashAlgorithm);
            CoseKey coseKey = new(KeyType.ECDsa, coseAlgorithm, hashAlgorithm);
            coseKey.AsymmetricAlgorithm = key;
            coseKey._ecdsaKey = key;
            return coseKey;
        }

        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        public static CoseKey FromKey(MLDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);

            CoseAlgorithm coseAlgorithm = GetMLDsaAlgorithm(key.Algorithm);
            CoseKey coseKey = new(KeyType.MLDsa, coseAlgorithm, null);
            coseKey._mldsaKey = key;
            return coseKey;
        }

        internal static CoseKey FromUntrustedAlgorithmAndKey(CoseAlgorithm untrustedAlgorithm, IDisposable key)
        {
            if (key is ECDsa ecdsaKey)
            {
                return untrustedAlgorithm switch
                {
                    CoseAlgorithm.ES256 => FromKey(ecdsaKey, HashAlgorithmName.SHA256),
                    CoseAlgorithm.ES384 => FromKey(ecdsaKey, HashAlgorithmName.SHA384),
                    CoseAlgorithm.ES512 => FromKey(ecdsaKey, HashAlgorithmName.SHA512),
                    _ => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, untrustedAlgorithm))
                };
            }
            else if (key is RSA rsaKey)
            {
                return untrustedAlgorithm switch
                {
                    CoseAlgorithm.RS256 => FromKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA256),
                    CoseAlgorithm.RS384 => FromKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA384),
                    CoseAlgorithm.RS512 => FromKey(rsaKey, RSASignaturePadding.Pkcs1, HashAlgorithmName.SHA512),
                    CoseAlgorithm.PS256 => FromKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA256),
                    CoseAlgorithm.PS384 => FromKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA384),
                    CoseAlgorithm.PS512 => FromKey(rsaKey, RSASignaturePadding.Pss, HashAlgorithmName.SHA512),
                    _ => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, untrustedAlgorithm))
                };
            }
#pragma warning disable SYSLIB5006
            else if (key is MLDsa mldsaKey)
            {
                return untrustedAlgorithm switch
                {
                    CoseAlgorithm.MLDsa44 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa44, mldsaKey),
                    CoseAlgorithm.MLDsa65 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa65, mldsaKey),
                    CoseAlgorithm.MLDsa87 => FromKeyWithExpectedAlgorithm(MLDsaAlgorithm.MLDsa87, mldsaKey),
                    _ => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, untrustedAlgorithm))
                };

                static CoseKey FromKeyWithExpectedAlgorithm(MLDsaAlgorithm expected, MLDsa key)
                    => key.Algorithm.Name == expected.Name ? FromKey(key) : CoseKey.FromKey(key);
            }
#pragma warning restore SYSLIB5006
            else
            {
                throw new ArgumentException(SR.Format(SR.Sign1UnsupportedKey, key.GetType().Name), nameof(key));
            }
        }

        internal static CoseAlgorithm CoseAlgorithmFromInt64(long alg)
        {
            if (alg >= 0 || alg < int.MinValue)
            {
                throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));
            }

            CoseAlgorithm coseAlgorithm = (CoseAlgorithm)alg;
            ThrowIfCoseAlgorithmNotSupported(coseAlgorithm);

            return coseAlgorithm;
        }

        private static void ThrowIfCoseAlgorithmNotSupported(CoseAlgorithm alg)
        {
#pragma warning disable SYSLIB5006
            if (alg != CoseAlgorithm.ES256 &&
                alg != CoseAlgorithm.ES384 &&
                alg != CoseAlgorithm.ES512 &&
                alg != CoseAlgorithm.PS256 &&
                alg != CoseAlgorithm.PS384 &&
                alg != CoseAlgorithm.PS512 &&
                alg != CoseAlgorithm.RS256 &&
                alg != CoseAlgorithm.RS384 &&
                alg != CoseAlgorithm.RS512 &&
                alg != CoseAlgorithm.MLDsa44 &&
                alg != CoseAlgorithm.MLDsa65 &&
                alg != CoseAlgorithm.MLDsa87)
            {
                throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, alg));
            }
#pragma warning restore SYSLIB5006
        }

        internal static CoseAlgorithm CoseAlgorithmFromString(string algString)
        {
            // https://www.iana.org/assignments/cose/cose.xhtml#algorithms
            return algString switch
            {
                nameof(CoseAlgorithm.ES256) => CoseAlgorithm.ES256,
                nameof(CoseAlgorithm.ES384) => CoseAlgorithm.ES384,
                nameof(CoseAlgorithm.ES512) => CoseAlgorithm.ES512,
                nameof(CoseAlgorithm.PS256) => CoseAlgorithm.PS256,
                nameof(CoseAlgorithm.PS384) => CoseAlgorithm.PS384,
                nameof(CoseAlgorithm.PS512) => CoseAlgorithm.PS512,
                nameof(CoseAlgorithm.RS256) => CoseAlgorithm.RS256,
                nameof(CoseAlgorithm.RS384) => CoseAlgorithm.RS384,
                nameof(CoseAlgorithm.RS512) => CoseAlgorithm.RS512,
#pragma warning disable SYSLIB5006
                "ML-DSA-44" => CoseAlgorithm.MLDsa44,
                "ML-DSA-65" => CoseAlgorithm.MLDsa65,
                "ML-DSA-87" => CoseAlgorithm.MLDsa87,
#pragma warning restore SYSLIB5006
                _ => throw new CryptographicException(SR.Format(SR.Sign1UnknownCoseAlgorithm, algString))
            };
        }

        internal int ComputeSignatureSize()
        {
            switch (KeyType)
            {
                case KeyType.ECDsa:
                    return 2 * ((_ecdsaKey!.KeySize + 7) / 8);
                case KeyType.RSA:
                    return (_rsaKey!.KeySize + 7) / 8;
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                    return _mldsaKey!.Algorithm.SignatureSizeInBytes;
#pragma warning restore SYSLIB5006
                default:
                    Debug.Fail($"Unknown key type: {KeyType}");
                    throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, KeyType.ToString()));
            }
        }

        internal int Sign(ReadOnlySpan<byte> toBeSigned, Span<byte> destination)
        {
            switch (KeyType)
            {
                case KeyType.ECDsa:
                    Debug.Assert(_ecdsaKey != null);
                    return SignHashWithECDsa(_ecdsaKey, toBeSigned, destination);
                case KeyType.RSA:
                    Debug.Assert(_rsaKey != null);
                    Debug.Assert(HashAlgorithm != null);
                    Debug.Assert(RSASignaturePadding != null);
                    return SignHashWithRSA(_rsaKey, toBeSigned, HashAlgorithm.Value, RSASignaturePadding, destination);
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                    Debug.Assert(_mldsaKey != null);
                    return _mldsaKey.SignData(toBeSigned, destination);
#pragma warning restore SYSLIB5006
                default:
                    Debug.Fail("Unknown key type");
                    throw new CryptographicException(SR.Format(SR.Sign1UnsupportedKey, KeyType.ToString()));
            }
        }

        private static int SignHashWithECDsa(ECDsa key, ReadOnlySpan<byte> hash, Span<byte> destination)
        {
            if (!key.TrySignHash(hash, destination, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
        }

        private static int SignHashWithRSA(RSA key, ReadOnlySpan<byte> hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, Span<byte> destination)
        {
            if (!key.TrySignHash(hash, destination, hashAlgorithm, padding, out int bytesWritten))
            {
                Debug.Fail("TrySignData failed with a pre-calculated destination");
                throw new CryptographicException();
            }

            return bytesWritten;
        }

        internal bool Verify(ReadOnlySpan<byte> toBeSigned, ReadOnlySpan<byte> signature)
        {
            switch (KeyType)
            {
                case KeyType.ECDsa:
                {
                    Debug.Assert(_ecdsaKey != null);
                    return _ecdsaKey.VerifyHash(toBeSigned, signature);
                }
                case KeyType.RSA:
                {
                    Debug.Assert(_rsaKey != null);
                    Debug.Assert(RSASignaturePadding != null);
                    Debug.Assert(HashAlgorithm != null);
                    return _rsaKey.VerifyHash(toBeSigned, signature, HashAlgorithm.Value, RSASignaturePadding);
                }
#pragma warning disable SYSLIB5006
                case KeyType.MLDsa:
                {
                    Debug.Assert(_mldsaKey != null);
                    return _mldsaKey.VerifyData(toBeSigned, signature);
                }
#pragma warning restore SYSLIB5006
                default:
                {
                    Debug.Fail($"Unknown keyType: {KeyType}");
                    throw new ArgumentException(SR.Sign1UnsupportedKey, KeyType.ToString());
                }
            }
        }

        private static CoseAlgorithm GetRSAAlgorithm(RSASignaturePadding signaturePadding, HashAlgorithmName hashAlgorithm)
        {
            Debug.Assert(signaturePadding != null);

            if (signaturePadding == RSASignaturePadding.Pss)
            {
                return hashAlgorithm.Name switch
                {
                    nameof(HashAlgorithmName.SHA256) => CoseAlgorithm.PS256,
                    nameof(HashAlgorithmName.SHA384) => CoseAlgorithm.PS384,
                    nameof(HashAlgorithmName.SHA512) => CoseAlgorithm.PS512,
                    _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name), nameof(hashAlgorithm))
                };
            }

            Debug.Assert(signaturePadding == RSASignaturePadding.Pkcs1);

            return hashAlgorithm.Name switch
            {
                nameof(HashAlgorithmName.SHA256) => CoseAlgorithm.RS256,
                nameof(HashAlgorithmName.SHA384) => CoseAlgorithm.RS384,
                nameof(HashAlgorithmName.SHA512) => CoseAlgorithm.RS512,
                _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name), nameof(hashAlgorithm))
            };
        }

        private static CoseAlgorithm GetECDsaAlgorithm(HashAlgorithmName hashAlgorithm)
        {
            return hashAlgorithm.Name switch
            {
                nameof(HashAlgorithmName.SHA256) => CoseAlgorithm.ES256,
                nameof(HashAlgorithmName.SHA384) => CoseAlgorithm.ES384,
                nameof(HashAlgorithmName.SHA512) => CoseAlgorithm.ES512,
                _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithm.Name), nameof(hashAlgorithm))
            };
        }

        [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
        private static CoseAlgorithm GetMLDsaAlgorithm(MLDsaAlgorithm algorithm)
        {
            if (algorithm.Name == MLDsaAlgorithm.MLDsa44.Name)
            {
                return CoseAlgorithm.MLDsa44;
            }
            else if (algorithm.Name == MLDsaAlgorithm.MLDsa65.Name)
            {
                return CoseAlgorithm.MLDsa65;
            }
            else if (algorithm.Name == MLDsaAlgorithm.MLDsa87.Name)
            {
                return CoseAlgorithm.MLDsa87;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.Sign1UnknownCoseAlgorithm, algorithm.Name), "key");
            }
        }

        internal ToBeSignedBuilder CreateToBeSignedBuilder()
        {
            switch (KeyType)
            {
                case KeyType.MLDsa:
                    return new PureDataToBeSignedBuilder();
                default:
                    Debug.Assert(HashAlgorithm != null);
                    return new HashToBeSignedBuilder(HashAlgorithm.Value);
            }
        }
    }
}
