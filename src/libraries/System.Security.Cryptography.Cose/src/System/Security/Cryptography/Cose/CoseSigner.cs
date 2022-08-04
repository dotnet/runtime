// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Provides signing information to be used with sign operations in <see cref="CoseSign1Message"/> and <see cref="CoseMultiSignMessage"/>.
    /// </summary>
    /// <seealso cref="CoseSign1Message.SignEmbedded(byte[], CoseSigner, byte[])"/>
    /// <seealso cref="CoseMultiSignMessage.SignEmbedded(byte[], CoseSigner, CoseHeaderMap?, CoseHeaderMap?, byte[])"/>
    /// <seealso cref="CoseMultiSignMessage.AddSignatureForEmbedded(CoseSigner, byte[])"/>
    public sealed class CoseSigner
    {
        internal readonly KeyType _keyType;
        internal readonly int? _algHeaderValueToSlip;
        internal CoseHeaderMap? _protectedHeaders;
        internal CoseHeaderMap? _unprotectedHeaders;

        /// <summary>
        /// Gets the cryptographic key of this instance.
        /// </summary>
        public AsymmetricAlgorithm Key { get; }

        /// <summary>
        /// Gets the hash algorithm of this instance.
        /// </summary>
        public HashAlgorithmName HashAlgorithm { get; }

        /// <summary>
        /// Gets the signature padding of this instance.
        /// </summary>
        public RSASignaturePadding? RSASignaturePadding { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseSigner"/> class.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the content.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> is <see cref="RSA"/>, use <see cref="CoseSigner(RSA, RSASignaturePadding, HashAlgorithmName, CoseHeaderMap?, CoseHeaderMap?)"/> to specify a signature padding.
        /// -or-
        /// <paramref name="key"/> is of an unsupported type.
        /// -or-
        /// <see cref="CoseHeaderLabel.Algorithm"/> was incorrect based on the <paramref name="key"/> and <paramref name="hashAlgorithm"/>.
        /// -or-
        /// <see cref="CoseHeaderLabel.Algorithm"/> was not contained in <paramref name="protectedHeaders"/>.</exception>
        /// <remarks>
        /// For sign operations in <see cref="CoseSign1Message"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the content (and only) layer.
        /// For sign operations in <see cref="CoseMultiSignMessage"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the signature layer.
        /// </remarks>
        public CoseSigner(AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (key is RSA)
                throw new ArgumentException(SR.CoseSignerRSAKeyNeedsPadding);

            Key = key;
            HashAlgorithm = hashAlgorithm;

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _keyType = CoseHelpers.GetKeyType(key);
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseSigner"/> class.
        /// </summary>
        /// <param name="key">The private key used to sign the content.</param>
        /// <param name="signaturePadding">The padding mode.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the content.</param>
        /// <param name="protectedHeaders">The protected header parameters to append to the message.</param>
        /// <param name="unprotectedHeaders">The unprotected header parameters to append to the message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><see cref="CoseHeaderLabel.Algorithm"/> was incorrect based on the <paramref name="key"/>, <paramref name="signaturePadding"/> and <paramref name="hashAlgorithm"/>.
        /// -or-
        /// <see cref="CoseHeaderLabel.Algorithm"/> was not contained in <paramref name="protectedHeaders"/>.</exception>
        /// <remarks>
        /// For sign operations in <see cref="CoseSign1Message"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the content (and only) layer.
        /// For sign operations in <see cref="CoseMultiSignMessage"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the signature layer.
        /// </remarks>
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

        /// <summary>
        /// Gets the protected header parameters of this instance.
        /// </summary>
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders ??= new CoseHeaderMap();

        /// <summary>
        /// Gets the unprotected header parameters of this instance.
        /// </summary>
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
                throw new ArgumentException(SR.Sign1SignAlgMustBeProtected);
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

                throw new ArgumentException(exMsg);
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
                    _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
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
                    _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
                };
            }

            Debug.Assert(RSASignaturePadding == RSASignaturePadding.Pkcs1);

            return hashAlgorithmName switch
            {
                nameof(HashAlgorithmName.SHA256) => KnownCoseAlgorithms.RS256,
                nameof(HashAlgorithmName.SHA384) => KnownCoseAlgorithms.RS384,
                nameof(HashAlgorithmName.SHA512) => KnownCoseAlgorithms.RS512,
                _ => throw new ArgumentException(SR.Format(SR.Sign1SignUnsupportedHashAlgorithm, hashAlgorithmName))
            };
        }
    }
}
