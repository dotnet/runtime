// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Hybrid Public Key Encryption (HPKE) cipher suite.
    /// </summary>
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class HpkeSuite : IEquatable<HpkeSuite>
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="HpkeSuite" /> class with the specified algorithms.
        /// </summary>
        /// <param name="kem">
        ///   One of the enumeration values that specifies the key encapsulation mechanism (KEM) for the cipher suite.
        /// </param>
        /// <param name="kdf">
        ///   One of the enumeration values that specifies the key derivation function (KDF) for the cipher suite.
        /// </param>
        /// <param name="aead">
        ///   One of the enumeration values that specifies the authenticated encryption with associated data (AEAD)
        ///   algorithm for the cipher suite.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="kem" />, <paramref name="kdf" />, or <paramref name="aead" /> is not a defined value
        ///   of its corresponding enumeration.
        /// </exception>
        public HpkeSuite(HpkeKem kem, HpkeKdf kdf, HpkeAead aead)
        {
            if (!IsValidHpkeKem(kem))
                throw new ArgumentOutOfRangeException(nameof(kem));

            if (!IsValidHpkeKdf(kdf))
                throw new ArgumentOutOfRangeException(nameof(kdf));

            if (!IsValidHpkeAead(aead))
                throw new ArgumentOutOfRangeException(nameof(aead));

            AeadAlgorithm = aead;
            KdfAlgorithm = kdf;
            KemAlgorithm = kem;
        }

        /// <summary>
        ///   Gets the authenticated encryption with associated data (AEAD) algorithm for the cipher suite.
        /// </summary>
        /// <value>
        ///   The authenticated encryption with associated data (AEAD) algorithm for the cipher suite.
        /// </value>
        public HpkeAead AeadAlgorithm { get; }

        /// <summary>
        ///   Gets the key derivation function (KDF) for the cipher suite.
        /// </summary>
        /// <value>
        ///   The key derivation function (KDF) for the cipher suite.
        /// </value>
        public HpkeKdf KdfAlgorithm { get; }

        /// <summary>
        ///   Gets the key encapsulation mechanism (KEM) for the cipher suite.
        /// </summary>
        /// <value>
        ///   The key encapsulation mechanism (KEM) for the cipher suite.
        /// </value>
        public HpkeKem KemAlgorithm { get; }

        /// <summary>
        ///   Compares two <see cref="HpkeSuite" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="HpkeSuite" /> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="other" /> is not <see langword="null" /> and specifies
        ///   the same algorithms as the current object; otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals([NotNullWhen(true)] HpkeSuite? other)
        {
            if (other is null)
            {
                return false;
            }

            return AeadAlgorithm == other.AeadAlgorithm &&
                KdfAlgorithm == other.KdfAlgorithm &&
                KemAlgorithm == other.KemAlgorithm;
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is HpkeSuite suite && Equals(suite);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(KemAlgorithm, KdfAlgorithm, AeadAlgorithm);

        /// <summary>
        ///   Determines whether two <see cref="HpkeSuite" /> objects specify the same algorithms.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies a cipher suite.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(HpkeSuite? left, HpkeSuite? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="HpkeSuite" /> objects do not specify the same algorithms.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies a cipher suite.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are not considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(HpkeSuite? left, HpkeSuite? right) => !(left == right);

        internal static bool IsValidHpkeAead(HpkeAead aead)
        {
            return aead is HpkeAead.AES_128_GCM or HpkeAead.AES_256_GCM or HpkeAead.ChaCha20Poly1305;
        }

        internal static bool IsValidHpkeKdf(HpkeKdf kdf)
        {
            return kdf is HpkeKdf.HKDF_SHA256 or
                HpkeKdf.HKDF_SHA384 or
                HpkeKdf.HKDF_SHA512 or
                HpkeKdf.SHAKE128 or
                HpkeKdf.SHAKE256;
        }

        internal static bool IsValidHpkeKem(HpkeKem kem)
        {
            return kem is HpkeKem.DHKEM_P256_HKDF_SHA256 or
                HpkeKem.DHKEM_P384_HKDF_SHA384 or
                HpkeKem.DHKEM_X25519_HKDF_SHA256 or
                HpkeKem.MLKEM_512 or
                HpkeKem.MLKEM_768 or
                HpkeKem.MLKEM_1024 or
                HpkeKem.MLKEM768_P256 or
                HpkeKem.MLKEM1024_P384;
        }
    }
}
