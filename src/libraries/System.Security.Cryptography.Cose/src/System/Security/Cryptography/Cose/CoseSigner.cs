// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseSigner
    {
        internal readonly KeyType _keyType;
        internal readonly int? _algHeaderValueToSlip;
        internal CoseHeaderMap? _protectedHeaders;
        internal CoseHeaderMap? _unprotectedHeaders;
        public AsymmetricAlgorithm Key { get; }
        public HashAlgorithmName HashAlgorithm { get; }
        public RSASignaturePadding? RSASignaturePadding { get; }

        public CoseSigner(AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (key is RSA)
                throw new CryptographicException(SR.CoseSignerRSAKeyNeedsPadding);

            Key = key;
            HashAlgorithm = hashAlgorithm;

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _keyType = CoseHelpers.GetKeyType(key);
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        public CoseSigner(RSA key, RSASignaturePadding signaturePadding, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (signaturePadding is null)
                throw new ArgumentNullException(nameof(signaturePadding));

            Key = key;
            HashAlgorithm = hashAlgorithm;
            RSASignaturePadding = signaturePadding;

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _keyType = CoseHelpers.GetKeyType(key);
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        public CoseHeaderMap ProtectedHeaders => _protectedHeaders ??= new CoseHeaderMap();
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders ??= new CoseHeaderMap();

        // If we Validate: The caller specified a COSE Algorithm, we will make sure it matches the specified key and hash algorithm.
        // If we Slip: The caller did not specify a COSE Algorithm, we will write the header for them rather than throw.
        internal int? ValidateOrSlipAlgorithmHeader()
        {
            int algHeaderValue = GetCoseAlgorithmHeader();

            if (_protectedHeaders != null && _protectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out CoseHeaderValue value))
            {
                ValidateAlgorithmHeader(value.EncodedValue, algHeaderValue);
                return null;
            }

            if (_unprotectedHeaders != null && _unprotectedHeaders.ContainsKey(CoseHeaderLabel.Algorithm))
            {
                throw new CryptographicException(SR.Sign1SignAlgMustBeProtected);
            }

            return algHeaderValue;
        }

        private void ValidateAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg, int expectedAlg)
        {
            int? alg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            Debug.Assert(alg.HasValue, "Algorithm (alg) is a known header and should have been validated in Set[Encoded]Value()");

            if (expectedAlg != alg.Value)
            {
                string exMsg;
                if (_keyType == KeyType.RSA)
                {
                    exMsg = SR.Format(SR.Sign1SignCoseAlgorithmDoesNotMatchSpecifiedKeyHashAlgorithmAndPadding, alg.Value, _keyType, HashAlgorithm.Name, RSASignaturePadding);
                }
                else
                {
                    exMsg = SR.Format(SR.Sign1SignCoseAlgorithmDoesNotMatchSpecifiedKeyAndHashAlgorithm, alg.Value, _keyType, HashAlgorithm.Name);
                }

                throw new CryptographicException(exMsg);
            }
        }

        private int GetCoseAlgorithmHeader()
        {
            string? hashAlgorithmName = HashAlgorithm.Name;
            if (_keyType == KeyType.ECDsa)
            {
                return hashAlgorithmName switch
                {
                    nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.ES256,
                    nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.ES384,
                    nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.ES512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
                };
            }

            Debug.Assert(_keyType == KeyType.RSA);
            Debug.Assert(RSASignaturePadding != null);

            if (RSASignaturePadding == RSASignaturePadding.Pss)
            {
                return hashAlgorithmName switch
                {
                    nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.PS256,
                    nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.PS384,
                    nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.PS512,
                    _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
                };
            }

            Debug.Assert(RSASignaturePadding == RSASignaturePadding.Pkcs1);

            return hashAlgorithmName switch
            {
                nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.RS256,
                nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.RS384,
                nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.RS512,
                _ => throw new CryptographicException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
            };
        }
    }
}
