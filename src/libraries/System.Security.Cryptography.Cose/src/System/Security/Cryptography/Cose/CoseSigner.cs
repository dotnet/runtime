// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Provides signing information to be used with sign operations in <see cref="CoseSign1Message"/> and <see cref="CoseMultiSignMessage"/>.
    /// </summary>
    public sealed class CoseSigner
    {
        internal readonly CoseAlgorithm? _algHeaderValueToSlip;
        internal CoseHeaderMap? _protectedHeaders;
        internal CoseHeaderMap? _unprotectedHeaders;

        /// <summary>
        /// Gets the private key to use during signing.
        /// </summary>
        /// <value>The private key to use during signing.</value>
        public AsymmetricAlgorithm? Key { get; }

        /// <summary>
        /// Gets the private key to use during signing.
        /// </summary>
        /// <value>The private key to use during signing.</value>
        internal CoseKey CoseKey { get; }

        /// <summary>
        /// Gets the hash algorithm to use to create the hash value for signing.
        /// </summary>
        /// <value>The hash algorithm to use to create the hash value for signing.</value>
        public HashAlgorithmName HashAlgorithm => CoseKey.HashAlgorithm ?? default;

        /// <summary>
        /// Gets the padding mode to use when signing.
        /// </summary>
        /// <value>The padding mode to use when signing.</value>
        public RSASignaturePadding? RSASignaturePadding => CoseKey.RSASignaturePadding;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseSigner"/> class.
        /// </summary>
        /// <param name="key">The private key to use for signing.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value for signing.</param>
        /// <param name="protectedHeaders">The collection of protected header parameters to append to the message when signing.</param>
        /// <param name="unprotectedHeaders">The collection of unprotected header parameters to append to the message when signing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="key"/> is <see cref="RSA"/>, use <see cref="CoseSigner(RSA, RSASignaturePadding, HashAlgorithmName, CoseHeaderMap?, CoseHeaderMap?)"/> to specify a signature padding.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="key"/> is of an unsupported type.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="protectedHeaders"/> contains a value with the <see cref="CoseHeaderLabel.Algorithm"/> label, but the value was incorrect based on the <paramref name="key"/> and <paramref name="hashAlgorithm"/>.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="unprotectedHeaders"/> specifies a value with the <see cref="CoseHeaderLabel.Algorithm"/> label.
        ///   </para>
        /// </exception>
        /// <remarks>
        /// For sign operations in <see cref="CoseSign1Message"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the content (and only) layer.
        /// For sign operations in <see cref="CoseMultiSignMessage"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the buckets of the signature layer.
        /// </remarks>
        public CoseSigner(AsymmetricAlgorithm key, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key is RSA)
                throw new ArgumentException(SR.CoseSignerRSAKeyNeedsPadding, nameof(key));

            if (key is not ECDsa)
                throw new ArgumentException(SR.Format(SR.Sign1UnsupportedKey, key.GetType().Name), nameof(key));

#pragma warning disable CS0618 // Type or member is obsolete
            Key = key;
#pragma warning restore CS0618 // Type or member is obsolete
            CoseKey = CoseKey.FromKey((ECDsa)key, hashAlgorithm);

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseSigner"/> class.
        /// </summary>
        /// <param name="key">The private key to use for signing.</param>
        /// <param name="signaturePadding">The padding mode to use when signing.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value for signing.</param>
        /// <param name="protectedHeaders">The collection of protected header parameters to append to the message when signing.</param>
        /// <param name="unprotectedHeaders">The collection of unprotected header parameters to append to the message when signing.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="protectedHeaders"/> contains a value with the <see cref="CoseHeaderLabel.Algorithm"/> label, but the value was incorrect based on the <paramref name="key"/>, <paramref name="signaturePadding"/> and <paramref name="hashAlgorithm"/>.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="unprotectedHeaders"/> specifies a value with the <see cref="CoseHeaderLabel.Algorithm"/> label.
        ///   </para>
        /// </exception>
        /// <remarks>
        /// For sign operations in <see cref="CoseSign1Message"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the header parameters of the content layer.
        /// For sign operations in <see cref="CoseMultiSignMessage"/>, <paramref name="protectedHeaders"/> and <paramref name="unprotectedHeaders"/> are used as the header parameters of the signature layer.
        /// </remarks>
        public CoseSigner(RSA key, RSASignaturePadding signaturePadding, HashAlgorithmName hashAlgorithm, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(signaturePadding);

#pragma warning disable CS0618 // Type or member is obsolete
            Key = key;
#pragma warning restore CS0618 // Type or member is obsolete
            CoseKey = CoseKey.FromKey(key, signaturePadding, hashAlgorithm);

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseSigner"/> class.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="protectedHeaders"></param>
        /// <param name="unprotectedHeaders"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public CoseSigner(CoseKey key, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

#pragma warning disable CS0618 // Type or member is obsolete
            Key = null;
#pragma warning restore CS0618 // Type or member is obsolete
            CoseKey = key;

            _protectedHeaders = protectedHeaders;
            _unprotectedHeaders = unprotectedHeaders;
            _algHeaderValueToSlip = ValidateOrSlipAlgorithmHeader();
        }

        /// <summary>
        /// Gets the protected header parameters to append to the message when signing.
        /// </summary>
        /// <value>A collection of protected header parameters to append to the message when signing.</value>
        public CoseHeaderMap ProtectedHeaders => _protectedHeaders ??= new CoseHeaderMap();

        /// <summary>
        /// Gets the unprotected header parameters to append to the message when signing.
        /// </summary>
        /// <value>A collection of unprotected header parameters to append to the message when signing.</value>
        public CoseHeaderMap UnprotectedHeaders => _unprotectedHeaders ??= new CoseHeaderMap();

        // If we Validate: The caller specified a COSE Algorithm, we will make sure it matches the specified key and hash algorithm.
        // If we Slip: The caller did not specify a COSE Algorithm, we will write the header for them rather than throw.
        internal CoseAlgorithm? ValidateOrSlipAlgorithmHeader()
        {
            CoseAlgorithm algHeaderValue = CoseKey.Algorithm;

            if (_protectedHeaders != null && _protectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out CoseHeaderValue value))
            {
                ValidateAlgorithmHeader(value.EncodedValue, algHeaderValue);
                return null;
            }

            if (_unprotectedHeaders != null && _unprotectedHeaders.ContainsKey(CoseHeaderLabel.Algorithm))
            {
                throw new ArgumentException(SR.Sign1SignAlgMustBeProtected, "unprotectedHeaders");
            }

            return algHeaderValue;
        }

        private void ValidateAlgorithmHeader(ReadOnlyMemory<byte> encodedAlg, CoseAlgorithm expectedAlg)
        {
            CoseAlgorithm? alg = CoseHelpers.DecodeCoseAlgorithmHeader(encodedAlg);
            Debug.Assert(alg.HasValue, "Algorithm (alg) is a known header and should have been validated in Set[Encoded]Value()");

            if (expectedAlg != alg.Value)
            {
                string exMsg;
                switch (CoseKey.KeyType)
                {
                    case KeyType.RSA:
                        exMsg = SR.Format(SR.Sign1SignCoseAlgorithmDoesNotMatchSpecifiedKeyHashAlgorithmAndPadding, alg.Value, CoseKey.KeyType, HashAlgorithm.Name, RSASignaturePadding);
                        break;
#pragma warning disable SYSLIB5006
                    case KeyType.MLDsa:
                        exMsg = SR.Format(SR.Sign1UnknownCoseAlgorithm, alg.Value);
                        break;
#pragma warning restore SYSLIB5006
                    default:
                        exMsg = SR.Format(SR.Sign1SignCoseAlgorithmDoesNotMatchSpecifiedKeyAndHashAlgorithm, alg.Value, CoseKey.KeyType, HashAlgorithm.Name);
                        break;
                }

                throw new ArgumentException(exMsg, "protectedHeaders");
            }
        }
    }
}
