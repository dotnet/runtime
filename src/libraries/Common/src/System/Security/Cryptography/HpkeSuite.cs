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
        internal HpkeAeadMetadata AeadMetadata { get; }
        internal HpkeKdfMetadata KdfMetadata { get; }
        internal HpkeKemMetadata KemMetadata { get; }

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
            KemMetadata = HpkeKemMetadata.Create(kem) ?? throw new ArgumentOutOfRangeException(nameof(kem));
            KdfMetadata = HpkeKdfMetadata.Create(kdf) ?? throw new ArgumentOutOfRangeException(nameof(kdf));
            AeadMetadata = HpkeAeadMetadata.Create(aead) ?? throw new ArgumentOutOfRangeException(nameof(aead));
        }

        /// <summary>
        ///   Gets the authenticated encryption with associated data (AEAD) algorithm for the cipher suite.
        /// </summary>
        /// <value>
        ///   The authenticated encryption with associated data (AEAD) algorithm for the cipher suite.
        /// </value>
        public HpkeAead AeadAlgorithm => AeadMetadata.Aead;

        /// <summary>
        ///   Gets the key derivation function (KDF) for the cipher suite.
        /// </summary>
        /// <value>
        ///   The key derivation function (KDF) for the cipher suite.
        /// </value>
        public HpkeKdf KdfAlgorithm => KdfMetadata.Kdf;

        /// <summary>
        ///   Gets the key encapsulation mechanism (KEM) for the cipher suite.
        /// </summary>
        /// <value>
        ///   The key encapsulation mechanism (KEM) for the cipher suite.
        /// </value>
        public HpkeKem KemAlgorithm => KemMetadata.Kem;

        /// <summary>
        ///   Gets the size of the authentication tag for the cipher suite, in bytes.
        /// </summary>
        /// <value>
        ///   The size of the authentication tag for the cipher suite, in bytes.
        /// </value>
        public int AeadTagSizeInBytes => AeadMetadata.Nt;

        /// <summary>
        ///   Gets the size of the decapsulation key for the cipher suite, in bytes.
        /// </summary>
        /// <value>
        ///   The size of the decapsulation key for the cipher suite, in bytes.
        /// </value>
        /// <remarks>
        ///   For ML-KEM and hybrid ML-KEM cipher suites, this is the size of the private seed.
        /// </remarks>
        public int DecapsulationKeySizeInBytes => KemMetadata.Nsk;

        /// <summary>
        ///   Gets the size of an encapsulated secret for the cipher suite, in bytes.
        /// </summary>
        /// <value>
        ///   The size of an encapsulated secret for the cipher suite, in bytes.
        /// </value>
        public int EncapsulatedSecretSizeInBytes => KemMetadata.Nenc;

        /// <summary>
        ///   Gets the size of the encapsulation key for the cipher suite, in bytes.
        /// </summary>
        /// <value>
        ///   The size of the encapsulation key for the cipher suite, in bytes.
        /// </value>
        public int EncapsulationKeySizeInBytes => KemMetadata.Npk;

        /// <summary>
        ///   Gets the name of the cipher suite.
        /// </summary>
        /// <value>
        ///   A string containing the KEM, KDF, and AEAD names, separated by spaces.
        /// </value>
        public string Name => field ??= $"{KemMetadata.Name} {KdfMetadata.Name} {AeadMetadata.Name}";

        /// <summary>
        ///   Gets the length of the ciphertext produced by encrypting a plaintext of the specified length.
        /// </summary>
        /// <param name="plaintextLength">
        ///   The length of the plaintext, in bytes.
        /// </param>
        /// <returns>
        ///   The length of the ciphertext, in bytes.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="plaintextLength" /> is negative or the resulting ciphertext length cannot be
        ///   represented as a signed 32-bit integer.
        /// </exception>
        /// <remarks>
        ///   The returned length includes the authentication tag, but does not include the encapsulated secret.
        /// </remarks>
        public int GetCiphertextLength(int plaintextLength)
        {
            int tagSize = AeadTagSizeInBytes;

            if (plaintextLength < 0 || plaintextLength > int.MaxValue - tagSize)
            {
                throw new ArgumentOutOfRangeException(nameof(plaintextLength));
            }

            return plaintextLength + tagSize;
        }

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

        /// <inheritdoc />
        public override string ToString() => Name;

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
    }
}
