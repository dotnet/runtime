// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Composite ML-KEM algorithm identifier, combining ML-KEM with a traditional algorithm.
    /// </summary>
    /// <seealso cref="CompositeMLKem" />
    [DebuggerDisplay("{Name,nq}")]
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class CompositeMLKemAlgorithm : IEquatable<CompositeMLKemAlgorithm>
    {
        /// <summary>
        ///   Gets the name of the algorithm.
        /// </summary>
        /// <value>
        ///   A string representing the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///   Gets the size of the ciphertext for the composite algorithm, in bytes.
        /// </summary>
        /// <value>
        ///   The size of the ciphertext for the composite algorithm, in bytes.
        /// </value>
        public int CiphertextSizeInBytes { get; }

        /// <summary>
        ///   Gets the size of the shared secret for the composite algorithm, in bytes.
        /// </summary>
        /// <value>
        ///   The size of the shared secret for the composite algorithm, in bytes.
        /// </value>
        public int SharedSecretSizeInBytes { get; }

        internal int MinEncapsulationKeySizeInBytes { get; }
        internal int MaxEncapsulationKeySizeInBytes { get; }
        internal int MinDecapsulationKeySizeInBytes { get; }
        internal int MaxDecapsulationKeySizeInBytes { get; }

        internal string Oid { get; }

        private CompositeMLKemAlgorithm(
            string name,
            int minEncapsulationKeySizeInBytes,
            int maxEncapsulationKeySizeInBytes,
            int minDecapsulationKeySizeInBytes,
            int maxDecapsulationKeySizeInBytes,
            int ciphertextSizeInBytes,
            int sharedSecretSizeInBytes,
            string oid)
        {
            Debug.Assert(minEncapsulationKeySizeInBytes <= maxEncapsulationKeySizeInBytes);
            Debug.Assert(minDecapsulationKeySizeInBytes <= maxDecapsulationKeySizeInBytes);

            Name = name;
            MinEncapsulationKeySizeInBytes = minEncapsulationKeySizeInBytes;
            MaxEncapsulationKeySizeInBytes = maxEncapsulationKeySizeInBytes;
            MinDecapsulationKeySizeInBytes = minDecapsulationKeySizeInBytes;
            MaxDecapsulationKeySizeInBytes = maxDecapsulationKeySizeInBytes;
            CiphertextSizeInBytes = ciphertextSizeInBytes;
            SharedSecretSizeInBytes = sharedSecretSizeInBytes;
            Oid = oid;
        }

        internal bool IsValidEncapsulationKeySize(int size) =>
            MinEncapsulationKeySizeInBytes <= size && size <= MaxEncapsulationKeySizeInBytes;

        internal bool IsValidDecapsulationKeySize(int size) =>
            MinDecapsulationKeySizeInBytes <= size && size <= MaxDecapsulationKeySizeInBytes;

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and 2048-bit RSA-OAEP algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and 2048-bit RSA-OAEP algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithRsaOaep2048 { get; } =
            CreateRsaOaep(
                "MLKEM768-RSA2048-SHA3-256",
                MLKemAlgorithm.MLKem768,
                2048,
                Oids.MLKem768WithRsaOaep2048Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and 3072-bit RSA-OAEP algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and 3072-bit RSA-OAEP algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithRsaOaep3072 { get; } =
            CreateRsaOaep(
                "MLKEM768-RSA3072-SHA3-256",
                MLKemAlgorithm.MLKem768,
                3072,
                Oids.MLKem768WithRsaOaep3072Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and 4096-bit RSA-OAEP algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and 4096-bit RSA-OAEP algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithRsaOaep4096 { get; } =
            CreateRsaOaep(
                "MLKEM768-RSA4096-SHA3-256",
                MLKemAlgorithm.MLKem768,
                4096,
                Oids.MLKem768WithRsaOaep4096Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and X25519 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and X25519 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithX25519 { get; } =
            CreateXDiffieHellman(
                "MLKEM768-X25519-SHA3-256",
                MLKemAlgorithm.MLKem768,
                32 * 8,
                Oids.MLKem768WithX25519Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH P-256 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH P-256 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithECDiffieHellmanP256 { get; } =
            CreateECDiffieHellman(
                "MLKEM768-ECDH-P256-SHA3-256",
                MLKemAlgorithm.MLKem768,
                256,
                Oids.MLKem768WithECDiffieHellmanP256Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH P-384 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH P-384 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithECDiffieHellmanP384 { get; } =
            CreateECDiffieHellman(
                "MLKEM768-ECDH-P384-SHA3-256",
                MLKemAlgorithm.MLKem768,
                384,
                Oids.MLKem768WithECDiffieHellmanP384Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH brainpoolP256r1 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-768 and ECDH brainpoolP256r1 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem768WithECDiffieHellmanBrainpoolP256r1 { get; } =
            CreateECDiffieHellman(
                "MLKEM768-ECDH-brainpoolP256r1-SHA3-256",
                MLKemAlgorithm.MLKem768,
                256,
                Oids.MLKem768WithECDiffieHellmanBrainpoolP256r1Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-1024 and 3072-bit RSA-OAEP algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-1024 and 3072-bit RSA-OAEP algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem1024WithRsaOaep3072 { get; } =
            CreateRsaOaep(
                "MLKEM1024-RSA3072-SHA3-256",
                MLKemAlgorithm.MLKem1024,
                3072,
                Oids.MLKem1024WithRsaOaep3072Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH P-384 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH P-384 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem1024WithECDiffieHellmanP384 { get; } =
            CreateECDiffieHellman(
                "MLKEM1024-ECDH-P384-SHA3-256",
                MLKemAlgorithm.MLKem1024,
                384,
                Oids.MLKem1024WithECDiffieHellmanP384Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH brainpoolP384r1 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH brainpoolP384r1 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem1024WithECDiffieHellmanBrainpoolP384r1 { get; } =
            CreateECDiffieHellman(
                "MLKEM1024-ECDH-brainpoolP384r1-SHA3-256",
                MLKemAlgorithm.MLKem1024,
                384,
                Oids.MLKem1024WithECDiffieHellmanBrainpoolP384r1Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-1024 and X448 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-1024 and X448 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem1024WithX448 { get; } =
            CreateXDiffieHellman(
                "MLKEM1024-X448-SHA3-256",
                MLKemAlgorithm.MLKem1024,
                56 * 8,
                Oids.MLKem1024WithX448Sha3_256);

        /// <summary>
        ///   Gets a Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH P-521 algorithm.
        /// </summary>
        /// <value>
        ///   A Composite ML-KEM algorithm identifier for the ML-KEM-1024 and ECDH P-521 algorithm.
        /// </value>
        public static CompositeMLKemAlgorithm MLKem1024WithECDiffieHellmanP521 { get; } =
            CreateECDiffieHellman(
                "MLKEM1024-ECDH-P521-SHA3-256",
                MLKemAlgorithm.MLKem1024,
                521,
                Oids.MLKem1024WithECDiffieHellmanP521Sha3_256);

        /// <summary>
        ///   Compares two <see cref="CompositeMLKemAlgorithm" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="CompositeMLKemAlgorithm"/> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        // This is a closed type, so all we need to compare are the names.
        public bool Equals([NotNullWhen(true)] CompositeMLKemAlgorithm? other) => other is not null && other.Name == Name;

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CompositeMLKemAlgorithm alg && alg.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLKemAlgorithm" /> objects specify the same algorithm name.
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
        public static bool operator ==(CompositeMLKemAlgorithm? left, CompositeMLKemAlgorithm? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="CompositeMLKemAlgorithm" /> objects do not specify the same algorithm name.
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
        public static bool operator !=(CompositeMLKemAlgorithm? left, CompositeMLKemAlgorithm? right)
        {
            return !(left == right);
        }

        internal static CompositeMLKemAlgorithm? GetAlgorithmFromOid(string? oid)
        {
            return oid switch
            {
                Oids.MLKem768WithRsaOaep2048Sha3_256 =>                      MLKem768WithRsaOaep2048,
                Oids.MLKem768WithRsaOaep3072Sha3_256 =>                      MLKem768WithRsaOaep3072,
                Oids.MLKem768WithRsaOaep4096Sha3_256 =>                      MLKem768WithRsaOaep4096,
                Oids.MLKem768WithX25519Sha3_256 =>                           MLKem768WithX25519,
                Oids.MLKem768WithECDiffieHellmanP256Sha3_256 =>              MLKem768WithECDiffieHellmanP256,
                Oids.MLKem768WithECDiffieHellmanP384Sha3_256 =>              MLKem768WithECDiffieHellmanP384,
                Oids.MLKem768WithECDiffieHellmanBrainpoolP256r1Sha3_256 =>   MLKem768WithECDiffieHellmanBrainpoolP256r1,
                Oids.MLKem1024WithRsaOaep3072Sha3_256 =>                     MLKem1024WithRsaOaep3072,
                Oids.MLKem1024WithECDiffieHellmanP384Sha3_256 =>             MLKem1024WithECDiffieHellmanP384,
                Oids.MLKem1024WithECDiffieHellmanBrainpoolP384r1Sha3_256 =>  MLKem1024WithECDiffieHellmanBrainpoolP384r1,
                Oids.MLKem1024WithX448Sha3_256 =>                            MLKem1024WithX448,
                Oids.MLKem1024WithECDiffieHellmanP521Sha3_256 =>             MLKem1024WithECDiffieHellmanP521,

                _ => null,
            };
        }

        private static CompositeMLKemAlgorithm CreateRsaOaep(
            string name,
            MLKemAlgorithm mlkemAlgorithm,
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

            return new CompositeMLKemAlgorithm(
                name,
                mlkemAlgorithm.EncapsulationKeySizeInBytes + keySizeInBytes, // Encapsulation key contains at least n
                mlkemAlgorithm.EncapsulationKeySizeInBytes + maxRsaPublicKeySizeInBytes,
                mlkemAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes, // Decapsulation key contains at least n
                mlkemAlgorithm.PrivateSeedSizeInBytes + maxRsaPrivateKeySizeInBytes,
                mlkemAlgorithm.CiphertextSizeInBytes + keySizeInBytes, // RSA-OAEP ciphertext is the size of the modulus
                mlkemAlgorithm.SharedSecretSizeInBytes,
                oid);
        }

        private static CompositeMLKemAlgorithm CreateECDiffieHellman(
            string name,
            MLKemAlgorithm mlkemAlgorithm,
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

            // version

            int versionSizeInBytes =
                1 + // Tag for INTEGER
                1 + // Length field
                1;  // Value (always 1)

            // privateKey

            int privateKeySizeInBytes =
                1 +                                     // Tag for OCTET STRING
                GetDerLengthLength(keySizeInBytes) +    // Length field
                keySizeInBytes;                         // Value

            // parameters

            int namedCurveSizeInBytes =
                oid switch
                {
                    Oids.MLKem768WithECDiffieHellmanP256Sha3_256 =>
                        // 1.2.840.10045.3.1.7
                        // 06 08 2A 86 48 CE 3D 03 01 07
                        10,
                    Oids.MLKem768WithECDiffieHellmanP384Sha3_256 or
                    Oids.MLKem1024WithECDiffieHellmanP384Sha3_256 =>
                        // 1.3.132.0.34
                        // 06 05 2B 81 04 00 22
                        7,
                    Oids.MLKem1024WithECDiffieHellmanP521Sha3_256 =>
                        // 1.3.132.0.35
                        // 06 05 2B 81 04 00 23
                        7,
                    Oids.MLKem768WithECDiffieHellmanBrainpoolP256r1Sha3_256 =>
                        // 1.3.36.3.3.2.8.1.1.7
                        // 06 09 2B 24 03 03 02 08 01 01 07
                        11,
                    Oids.MLKem1024WithECDiffieHellmanBrainpoolP384r1Sha3_256 =>
                        // 1.3.36.3.3.2.8.1.1.11
                        // 06 09 2B 24 03 03 02 08 01 01 0B
                        11,
                    _ => AssertAndThrow(oid),
                };

            static int AssertAndThrow(string oid)
            {
                Debug.Fail($"Unsupported OID: {oid}.");
                throw new CryptographicException();
            }

            int parametersSizeInBytes =
                1 +                                         // Context-specific tag for [0]
                GetDerLengthLength(namedCurveSizeInBytes) + // Length field
                namedCurveSizeInBytes;                      // Value

            // publicKey must be omitted for Composite ML-KEM

            int ecPrivateKeySizeInBytes =
                1 +                                                                                         // Tag for SEQUENCE
                GetDerLengthLength(versionSizeInBytes + privateKeySizeInBytes + parametersSizeInBytes) +    // Length field
                versionSizeInBytes + privateKeySizeInBytes + parametersSizeInBytes;                         // Value

            // The traditional component of the encapsulation key and the ciphertext are both uncompressed
            // elliptic curve points, i.e. 0x04 followed by the X and Y coordinates.
            int uncompressedPointSizeInBytes = 1 + 2 * keySizeInBytes;

            return new CompositeMLKemAlgorithm(
                name,
                mlkemAlgorithm.EncapsulationKeySizeInBytes + uncompressedPointSizeInBytes,
                mlkemAlgorithm.EncapsulationKeySizeInBytes + uncompressedPointSizeInBytes,
                mlkemAlgorithm.PrivateSeedSizeInBytes + ecPrivateKeySizeInBytes,
                mlkemAlgorithm.PrivateSeedSizeInBytes + ecPrivateKeySizeInBytes,
                mlkemAlgorithm.CiphertextSizeInBytes + uncompressedPointSizeInBytes,
                mlkemAlgorithm.SharedSecretSizeInBytes,
                oid);
        }

        private static CompositeMLKemAlgorithm CreateXDiffieHellman(
            string name,
            MLKemAlgorithm mlkemAlgorithm,
            int keySizeInBits,
            string oid)
        {
            Debug.Assert(keySizeInBits % 8 == 0);
            int keySizeInBytes = keySizeInBits / 8;

            return new CompositeMLKemAlgorithm(
                name,
                mlkemAlgorithm.EncapsulationKeySizeInBytes + keySizeInBytes,
                mlkemAlgorithm.EncapsulationKeySizeInBytes + keySizeInBytes,
                mlkemAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes,
                mlkemAlgorithm.PrivateSeedSizeInBytes + keySizeInBytes,
                mlkemAlgorithm.CiphertextSizeInBytes + keySizeInBytes,
                mlkemAlgorithm.SharedSecretSizeInBytes,
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
