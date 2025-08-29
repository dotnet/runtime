// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the SHL-DSA family.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class SlhDsaAlgorithm : IEquatable<SlhDsaAlgorithm>
    {
        /// <summary>
        ///   Gets the underlying string representation of the algorithm name.
        /// </summary>
        /// <value>
        ///   The underlying string representation of the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///  Gets the size of the private key in bytes for this algorithm.
        /// </summary>
        /// <value>
        ///  The size of the private key in bytes for this algorithm.
        /// </value>
        public int PrivateKeySizeInBytes { get; }

        /// <summary>
        ///  Gets the size of the public key in bytes for this algorithm.
        /// </summary>
        /// <value>
        ///  The size of the public key in bytes for this algorithm.
        /// </value>
        public int PublicKeySizeInBytes { get; }

        /// <summary>
        ///  Gets the size of the signature in bytes for this algorithm.
        /// </summary>
        /// <value>
        ///  The size of the signature in bytes for this algorithm.
        /// </value>
        public int SignatureSizeInBytes { get; }

        /// <summary>
        ///  Gets the Object Identifier (OID) for this algorithm.
        /// </summary>
        /// <value>
        ///  The Object Identifier (OID) for this algorithm.
        /// </value>
        internal string Oid { get; }

        /// <summary>
        ///  Initializes a new instance of the <see cref="SlhDsaAlgorithm" /> structure with a custom name.
        /// </summary>
        /// <param name="name">
        ///   The name of the algorithm.
        /// </param>
        /// <param name="n">
        ///   The "security parameter" as described in FIPS 205.
        /// </param>
        /// <param name="signatureSizeInBytes">
        ///   The size of the signature in bytes for this algorithm.
        /// </param>
        /// <param name="oid">
        ///   The Object Identifier (OID) for this algorithm.
        /// </param>
        private SlhDsaAlgorithm(string name, int n, int signatureSizeInBytes, string oid)
        {
            Name = name;

            // The private key and public key sizes are shown to be 4n and 2n respectively in
            // section 9.1 "Key Generation", particularly figure 15 and 16.
            PrivateKeySizeInBytes = 4 * n;
            PublicKeySizeInBytes = 2 * n;
            SignatureSizeInBytes = signatureSizeInBytes;
            Oid = oid;
        }

        // SLH-DSA parameter sets, and the sizes associated with them,
        // are defined in FIPS 205, section 11 "Parameter Sets",
        // particularly Table 2 "SLH-DSA parameter sets".

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_128s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-128s", 16, 7856, Oids.SlhDsaSha2_128s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake128s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-128s", 16, 7856, Oids.SlhDsaShake128s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_128f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-128f", 16, 17088, Oids.SlhDsaSha2_128f);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake128f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-128f", 16, 17088, Oids.SlhDsaShake128f);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_192s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-192s", 24, 16224, Oids.SlhDsaSha2_192s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake192s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-192s", 24, 16224, Oids.SlhDsaShake192s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_192f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-192f", 24, 35664, Oids.SlhDsaSha2_192f);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake192f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-192f", 24, 35664, Oids.SlhDsaShake192f);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_256s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-256s", 32, 29792, Oids.SlhDsaSha2_256s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256s algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake256s { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-256s", 32, 29792, Oids.SlhDsaShake256s);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaSha2_256f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHA2-256f", 32, 49856, Oids.SlhDsaSha2_256f);

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256f algorithm.
        /// </value>
        public static SlhDsaAlgorithm SlhDsaShake256f { get; } = new SlhDsaAlgorithm("SLH-DSA-SHAKE-256f", 32, 49856, Oids.SlhDsaShake256f);

        internal static SlhDsaAlgorithm? GetAlgorithmFromOid(string? oid)
        {
            return oid switch
            {
                Oids.SlhDsaSha2_128s => SlhDsaSha2_128s,
                Oids.SlhDsaShake128s => SlhDsaShake128s,
                Oids.SlhDsaSha2_128f => SlhDsaSha2_128f,
                Oids.SlhDsaShake128f => SlhDsaShake128f,
                Oids.SlhDsaSha2_192s => SlhDsaSha2_192s,
                Oids.SlhDsaShake192s => SlhDsaShake192s,
                Oids.SlhDsaSha2_192f => SlhDsaSha2_192f,
                Oids.SlhDsaShake192f => SlhDsaShake192f,
                Oids.SlhDsaSha2_256s => SlhDsaSha2_256s,
                Oids.SlhDsaShake256s => SlhDsaShake256s,
                Oids.SlhDsaSha2_256f => SlhDsaSha2_256f,
                Oids.SlhDsaShake256f => SlhDsaShake256f,

                _ => null,
            };
        }

        /// <summary>
        ///   Compares two <see cref="SlhDsaAlgorithm" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="SlhDsaAlgorithm"/> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        // This is a closed type, so all we need to compare are the names.
        public bool Equals([NotNullWhen(true)] SlhDsaAlgorithm? other) => other is not null && other.Name == Name;

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is SlhDsaAlgorithm alg && alg.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        ///   Determines whether two <see cref="SlhDsaAlgorithm" /> objects specify the same algorithm name.
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
        public static bool operator ==(SlhDsaAlgorithm? left, SlhDsaAlgorithm? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="SlhDsaAlgorithm" /> objects do not specify the same algorithm name.
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
        public static bool operator !=(SlhDsaAlgorithm? left, SlhDsaAlgorithm? right)
        {
            return !(left == right);
        }

        [DoesNotReturn]
        private static SlhDsaAlgorithm ThrowAlgorithmUnknown(string algorithmId)
        {
            throw new CryptographicException(
                SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, algorithmId));
        }
    }
}
