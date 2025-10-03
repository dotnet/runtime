// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a composite ML-DSA algorithm identifier, combining ML-DSA with a traditional algorithm.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class CompositeMLDsaAlgorithm : IEquatable<CompositeMLDsaAlgorithm>
    {
        /// <summary>
        ///   Gets the name of the algorithm.
        /// </summary>
        /// <value>
        ///   A string representing the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///   Gets the maximum signature size in bytes for the composite algorithm.
        /// </summary>
        /// <value>
        ///   The maximum signature size in bytes for the composite algorithm.
        /// </value>
        public int MaxSignatureSizeInBytes { get; }

        internal int MinPrivateKeySizeInBytes { get; }
        internal int MaxPrivateKeySizeInBytes { get; }
        internal int MinPublicKeySizeInBytes { get; }
        internal int MaxPublicKeySizeInBytes { get; }
        internal int MinSignatureSizeInBytes { get; }

        internal string Oid { get; }

        private CompositeMLDsaAlgorithm(
            string name,
            int minPrivateKeySizeInBytes,
            int maxPrivateKeySizeInBytes,
            int minPublicKeySizeInBytes,
            int maxPublicKeySizeInBytes,
            int minSignatureSize,
            int maxSignatureSize,
            string oid)
        {
            Debug.Assert(minPrivateKeySizeInBytes <= maxPrivateKeySizeInBytes);
            Debug.Assert(minPublicKeySizeInBytes <= maxPublicKeySizeInBytes);
            Debug.Assert(minSignatureSize <= maxSignatureSize);

            Name = name;
            MinPrivateKeySizeInBytes = minPrivateKeySizeInBytes;
            MaxPrivateKeySizeInBytes = maxPrivateKeySizeInBytes;
            MinPublicKeySizeInBytes = minPublicKeySizeInBytes;
            MaxPublicKeySizeInBytes = maxPublicKeySizeInBytes;
            MinSignatureSizeInBytes = minSignatureSize;
            MaxSignatureSizeInBytes = maxSignatureSize;
            Oid = oid;
        }

        internal bool IsValidPrivateKeySize(int size) => MinPrivateKeySizeInBytes <= size && size <= MaxPrivateKeySizeInBytes;
        internal bool IsValidPublicKeySize(int size) => MinPublicKeySizeInBytes <= size && size <= MaxPublicKeySizeInBytes;
        internal bool IsValidSignatureSize(int size) => MinSignatureSizeInBytes <= size && size <= MaxSignatureSizeInBytes;

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PSS with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PSS with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pss { get; } =
            CreateRsa(
                "MLDSA44-RSA2048-PSS-SHA256",
                MLDsaAlgorithm.MLDsa44,
                2048,
                Oids.MLDsa44WithRSA2048PssPreHashSha256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PKCS1-v1_5 with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and 2048-bit RSASSA-PKCS1-v1_5 with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithRSA2048Pkcs15 { get; } =
            CreateRsa(
                "MLDSA44-RSA2048-PKCS15-SHA256",
                MLDsaAlgorithm.MLDsa44,
                2048,
                Oids.MLDsa44WithRSA2048Pkcs15PreHashSha256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and Ed25519 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and Ed25519 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithEd25519 { get; } =
            CreateEdDsa(
                "MLDSA44-Ed25519-SHA512",
                MLDsaAlgorithm.MLDsa44,
                32 * 8,
                Oids.MLDsa44WithEd25519PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-44 and ECDSA P-256 with SHA256 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 and ECDSA P-256 with SHA256 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa44WithECDsaP256 { get; } =
            CreateECDsa(
                "MLDSA44-ECDSA-P256-SHA256",
                MLDsaAlgorithm.MLDsa44,
                256,
                Oids.MLDsa44WithECDsaP256PreHashSha256);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pss { get; } =
            CreateRsa(
                "MLDSA65-RSA3072-PSS-SHA512",
                MLDsaAlgorithm.MLDsa65,
                3072,
                Oids.MLDsa65WithRSA3072PssPreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 3072-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA3072Pkcs15 { get; } =
            CreateRsa(
                "MLDSA65-RSA3072-PKCS15-SHA512",
                MLDsaAlgorithm.MLDsa65,
                3072,
                Oids.MLDsa65WithRSA3072Pkcs15PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pss { get; } =
            CreateRsa(
                "MLDSA65-RSA4096-PSS-SHA512",
                MLDsaAlgorithm.MLDsa65,
                4096,
                Oids.MLDsa65WithRSA4096PssPreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and 4096-bit RSASSA-PKCS1-v1_5 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithRSA4096Pkcs15 { get; } =
            CreateRsa(
                "MLDSA65-RSA4096-PKCS15-SHA512",
                MLDsaAlgorithm.MLDsa65,
                4096,
                Oids.MLDsa65WithRSA4096Pkcs15PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-256 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-256 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP256 { get; } =
            CreateECDsa(
                "MLDSA65-ECDSA-P256-SHA512",
                MLDsaAlgorithm.MLDsa65,
                256,
                Oids.MLDsa65WithECDsaP256PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-384 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA P-384 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaP384 { get; } =
            CreateECDsa(
                "MLDSA65-ECDSA-P384-SHA512",
                MLDsaAlgorithm.MLDsa65,
                384,
                Oids.MLDsa65WithECDsaP384PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA BrainpoolP256r1 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and ECDSA BrainpoolP256r1 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithECDsaBrainpoolP256r1 { get; } =
            CreateECDsa(
                "MLDSA65-ECDSA-brainpoolP256r1-SHA512",
                MLDsaAlgorithm.MLDsa65,
                256,
                Oids.MLDsa65WithECDsaBrainpoolP256r1PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-65 and Ed25519 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 and Ed25519 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa65WithEd25519 { get; } =
            CreateEdDsa(
                "MLDSA65-Ed25519-SHA512",
                MLDsaAlgorithm.MLDsa65,
                32 * 8,
                Oids.MLDsa65WithEd25519PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-384 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-384 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP384 { get; } =
            CreateECDsa(
                "MLDSA87-ECDSA-P384-SHA512",
                MLDsaAlgorithm.MLDsa87,
                384,
                Oids.MLDsa87WithECDsaP384PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA BrainpoolP384r1 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA BrainpoolP384r1 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaBrainpoolP384r1 { get; } =
            CreateECDsa(
                "MLDSA87-ECDSA-brainpoolP384r1-SHA512",
                MLDsaAlgorithm.MLDsa87,
                384,
                Oids.MLDsa87WithECDsaBrainpoolP384r1PreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and Ed448 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and Ed448 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithEd448 { get; } =
            CreateEdDsa(
                "MLDSA87-Ed448-SHAKE256",
                MLDsaAlgorithm.MLDsa87,
                57 * 8,
                Oids.MLDsa87WithEd448PreHashShake256_512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and 3072-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithRSA3072Pss { get; } =
            CreateRsa(
                "MLDSA87-RSA3072-PSS-SHA512",
                MLDsaAlgorithm.MLDsa87,
                3072,
                Oids.MLDsa87WithRSA3072PssPreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and 4096-bit RSASSA-PSS with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithRSA4096Pss { get; } =
            CreateRsa(
                "MLDSA87-RSA4096-PSS-SHA512",
                MLDsaAlgorithm.MLDsa87,
                4096,
                Oids.MLDsa87WithRSA4096PssPreHashSha512);

        /// <summary>
        ///   Gets a Composite ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-521 with SHA512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 and ECDSA P-521 with SHA512 algorithm.
        /// </value>
        public static CompositeMLDsaAlgorithm MLDsa87WithECDsaP521 { get; } =
            CreateECDsa(
                "MLDSA87-ECDSA-P521-SHA512",
                MLDsaAlgorithm.MLDsa87,
                521,
                Oids.MLDsa87WithECDsaP521PreHashSha512);

        /// <summary>
        ///   Compares two <see cref="CompositeMLDsaAlgorithm" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="CompositeMLDsaAlgorithm"/> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        // This is a closed type, so all we need to compare are the names.
        public bool Equals([NotNullWhen(true)] CompositeMLDsaAlgorithm? other) => other is not null && other.Name == Name;

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CompositeMLDsaAlgorithm alg && alg.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLDsaAlgorithm" /> objects specify the same algorithm name.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies an algorithm name.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLDsaAlgorithm" /> objects do not specify the same algorithm name.
        /// </summary>
        /// <param name="left">
        ///   An object that specifies an algorithm name.
        /// </param>
        /// <param name="right">
        ///   A second object, to be compared to the object that is identified by the <paramref name="left" /> parameter.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are not considered equal; otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator !=(CompositeMLDsaAlgorithm? left, CompositeMLDsaAlgorithm? right)
        {
            return !(left == right);
        }

        internal static CompositeMLDsaAlgorithm? GetAlgorithmFromOid(string? oid)
        {
            return oid switch
            {
                Oids.MLDsa44WithRSA2048PssPreHashSha256 =>              MLDsa44WithRSA2048Pss,
                Oids.MLDsa44WithRSA2048Pkcs15PreHashSha256 =>           MLDsa44WithRSA2048Pkcs15,
                Oids.MLDsa44WithEd25519PreHashSha512 =>                 MLDsa44WithEd25519,
                Oids.MLDsa44WithECDsaP256PreHashSha256 =>               MLDsa44WithECDsaP256,
                Oids.MLDsa65WithRSA3072PssPreHashSha512 =>              MLDsa65WithRSA3072Pss,
                Oids.MLDsa65WithRSA3072Pkcs15PreHashSha512 =>           MLDsa65WithRSA3072Pkcs15,
                Oids.MLDsa65WithRSA4096PssPreHashSha512 =>              MLDsa65WithRSA4096Pss,
                Oids.MLDsa65WithRSA4096Pkcs15PreHashSha512 =>           MLDsa65WithRSA4096Pkcs15,
                Oids.MLDsa65WithECDsaP256PreHashSha512 =>               MLDsa65WithECDsaP256,
                Oids.MLDsa65WithECDsaP384PreHashSha512 =>               MLDsa65WithECDsaP384,
                Oids.MLDsa65WithECDsaBrainpoolP256r1PreHashSha512 =>    MLDsa65WithECDsaBrainpoolP256r1,
                Oids.MLDsa65WithEd25519PreHashSha512 =>                 MLDsa65WithEd25519,
                Oids.MLDsa87WithECDsaP384PreHashSha512 =>               MLDsa87WithECDsaP384,
                Oids.MLDsa87WithECDsaBrainpoolP384r1PreHashSha512 =>    MLDsa87WithECDsaBrainpoolP384r1,
                Oids.MLDsa87WithEd448PreHashShake256_512 =>             MLDsa87WithEd448,
                Oids.MLDsa87WithRSA3072PssPreHashSha512 =>              MLDsa87WithRSA3072Pss,
                Oids.MLDsa87WithRSA4096PssPreHashSha512 =>              MLDsa87WithRSA4096Pss,
                Oids.MLDsa87WithECDsaP521PreHashSha512 =>               MLDsa87WithECDsaP521,

                _ => null,
            };
        }

        private static CompositeMLDsaAlgorithm CreateRsa(
            string name,
            MLDsaAlgorithm mldsaAlgorithm,
            int keySizeInBits,
            string oid)
        {
            Debug.Assert(keySizeInBits % 8 == 0);
            int keySizeInBytes = keySizeInBits / 8;

            const int MaxUniversalTagLength = 1;

            // long form prefix and 4 bytes for length. CLR arrays and spans only support length up to int.MaxValue.
            // Padding with leading zero bytes is allowed, but we still limit the length to 4 bytes since the only
            // plausible scenario would be encoding a 4-byte numeric data type without trimming.
            // Note this bound also covers indefinite length encodings which require only 1 + 2 bytes of overhead.
            const int MaxLengthLength = 1 + 4;

            const int MaxPrefixLength = MaxUniversalTagLength + MaxLengthLength;

            const int PossibleLeadingZeroByte = 1; // ASN.1 INTEGER can have a leading zero byte.
            int maxKeyEncodingLength = keySizeInBytes + PossibleLeadingZeroByte;
            int maxHalfKeyEncodingLength = (keySizeInBytes + 1) / 2 + PossibleLeadingZeroByte;
            int maxExponentEncodingLength = 256 / 8 + PossibleLeadingZeroByte; // FIPS 186-5, 5.4 (e): The exponent e shall be an odd, positive integer such that 2^16 < e < 2^256

            // RFC 8017, A.1.1
            // RSAPublicKey::= SEQUENCE {
            //     modulus INTEGER,  --n
            //     publicExponent INTEGER   --e
            // }

            int maxRsaPublicKeySizeInBytes =
                MaxPrefixLength +
                (
                    MaxPrefixLength + maxKeyEncodingLength +
                    MaxPrefixLength + maxExponentEncodingLength
                );

            // RFC 8017, A.1.2
            // RSAPrivateKey::= SEQUENCE {
            //     version Version,
            //     modulus           INTEGER,  --n
            //     publicExponent INTEGER,  --e
            //     privateExponent INTEGER,  --d
            //     prime1 INTEGER,  --p
            //     prime2 INTEGER,  --q
            //     exponent1 INTEGER,  --d mod(p - 1)
            //     exponent2 INTEGER,  --d mod(q - 1)
            //     coefficient INTEGER,  --(inverse of q) mod p
            //     otherPrimeInfos OtherPrimeInfos OPTIONAL
            // }

            int maxRsaPrivateKeySizeInBytes =
                MaxPrefixLength +
                (
                    MaxPrefixLength + 1 + // Version should always be 0 or 1
                    MaxPrefixLength + maxKeyEncodingLength +
                    MaxPrefixLength + maxExponentEncodingLength +
                    MaxPrefixLength + maxKeyEncodingLength +
                    MaxPrefixLength + maxHalfKeyEncodingLength +
                    MaxPrefixLength + maxHalfKeyEncodingLength +
                    MaxPrefixLength + maxHalfKeyEncodingLength +
                    MaxPrefixLength + maxHalfKeyEncodingLength +
                    MaxPrefixLength + maxHalfKeyEncodingLength
                    // OtherPrimeInfos omitted since multi-prime is not supported
                );

            return new CompositeMLDsaAlgorithm(
                name,
                mldsaAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes, // Private key contains at least n
                mldsaAlgorithm.PrivateSeedSizeInBytes + maxRsaPrivateKeySizeInBytes,
                mldsaAlgorithm.PublicKeySizeInBytes + keySizeInBytes, // Private key contains at least n
                mldsaAlgorithm.PublicKeySizeInBytes + maxRsaPublicKeySizeInBytes,
                mldsaAlgorithm.SignatureSizeInBytes + keySizeInBytes,
                mldsaAlgorithm.SignatureSizeInBytes + keySizeInBytes,
                oid);
        }

        private static CompositeMLDsaAlgorithm CreateECDsa(
            string name,
            MLDsaAlgorithm mldsaAlgorithm,
            int keySizeInBits,
            string oid)
        {
            int keySizeInBytes = (keySizeInBits + 7) / 8;

            // RFC 5915, Section 3
            // ECPrivateKey ::= SEQUENCE {
            //   version        INTEGER { ecPrivkeyVer1(1) } (ecPrivkeyVer1),
            //   privateKey     OCTET STRING,
            //   parameters [0] ECParameters {{ NamedCurve }} OPTIONAL,
            //   publicKey  [1] BIT STRING OPTIONAL
            // }

            int versionSizeInBytes =
                1 + // Tag for INTEGER
                1 + // Length field
                1;  // Value (always 1)

            int privateKeySizeInBytes =
                1 +                                     // Tag for OCTET STRING
                GetDerLengthLength(keySizeInBytes) +    // Length field
                keySizeInBytes;                         // Value

            // parameters and publicKey must be omitted for Composite ML-DSA

            int ecPrivateKeySizeInBytes =
                1 +                                                                 // Tag for SEQUENCE
                GetDerLengthLength(versionSizeInBytes + privateKeySizeInBytes) +    // Length field
                versionSizeInBytes +                                                // Version
                privateKeySizeInBytes;

            return new CompositeMLDsaAlgorithm(
                name,
                mldsaAlgorithm.PrivateSeedSizeInBytes + ecPrivateKeySizeInBytes,
                mldsaAlgorithm.PrivateSeedSizeInBytes + ecPrivateKeySizeInBytes,
                mldsaAlgorithm.PublicKeySizeInBytes + 1 + 2 * keySizeInBytes,
                mldsaAlgorithm.PublicKeySizeInBytes + 1 + 2 * keySizeInBytes,
                mldsaAlgorithm.SignatureSizeInBytes + 2 + 3 * 2, // 2 non-zero INTEGERS and overhead for 3 ASN.1 values
                mldsaAlgorithm.SignatureSizeInBytes + AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(keySizeInBits),
                oid);
        }

        private static CompositeMLDsaAlgorithm CreateEdDsa(
            string name,
            MLDsaAlgorithm mldsaAlgorithm,
            int keySizeInBits,
            string oid)
        {
            Debug.Assert(keySizeInBits % 8 == 0);
            int keySizeInBytes = keySizeInBits / 8;

            return new CompositeMLDsaAlgorithm(
                name,
                mldsaAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes,
                mldsaAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes,
                mldsaAlgorithm.PublicKeySizeInBytes + keySizeInBytes,
                mldsaAlgorithm.PublicKeySizeInBytes + keySizeInBytes,
                mldsaAlgorithm.SignatureSizeInBytes + 2 * keySizeInBytes,
                mldsaAlgorithm.SignatureSizeInBytes + 2 * keySizeInBytes,
                oid);
        }

        private static int GetDerLengthLength(int payloadLength)
        {
            Debug.Assert(payloadLength >= 0);

            if (payloadLength <= 0x7F)
                return 1;

            if (payloadLength <= 0xFF)
                return 2;

            if (payloadLength <= 0xFFFF)
                return 3;

            if (payloadLength <= 0xFFFFFF)
                return 4;

            return 5;
        }
    }
}
