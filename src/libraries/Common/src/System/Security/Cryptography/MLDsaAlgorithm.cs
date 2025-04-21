// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the ML-DSA family.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public sealed class MLDsaAlgorithm
    {
        /// <summary>
        ///   Gets the underlying string representation of the algorithm name.
        /// </summary>
        /// <value>
        ///   The underlying string representation of the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///   Gets the size, in bytes, of the ML-DSA secret key for the current ML-DSA algorithm.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the ML-DSA secret key for the current ML-DSA algorithm.
        /// </value>
        public int SecretKeySizeInBytes { get; }

        /// <summary>
        ///   Gets the size, in bytes, of the ML-DSA private seed for the current ML-DSA algorithm.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the ML-DSA private seed for the current ML-DSA algorithm.
        /// </value>
        public int PrivateSeedSizeInBytes => 32;

        /// <summary>
        ///   Gets the size of the ML-DSA public key for the current ML-DSA algorithm.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the ML-DSA public key for the current ML-DSA algorithm.
        /// </value>
        public int PublicKeySizeInBytes { get; }

        /// <summary>
        ///   Gets the size, in bytes, of the signature for the current ML-DSA algorithm.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the signature for the current ML-DSA algorithm.
        /// </value>
        public int SignatureSizeInBytes { get; }

        internal string Oid { get; }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MLDsaAlgorithm" /> structure with a custom name.
        /// </summary>
        /// <param name="name">
        ///   The name of the algorithm.
        /// </param>
        /// <param name="secretKeySizeInBytes">
        ///   The size of the secret key in bytes.
        /// </param>
        /// <param name="publicKeySizeInBytes">
        ///   The size of the public key in bytes.
        /// </param>
        /// <param name="signatureSizeInBytes">
        ///   The size of the signature in bytes.
        /// </param>
        /// <param name="oid">
        ///   The OID of the algorithm.
        /// </param>
        private MLDsaAlgorithm(string name, int secretKeySizeInBytes, int publicKeySizeInBytes, int signatureSizeInBytes, string oid)
        {
            Name = name;
            SecretKeySizeInBytes = secretKeySizeInBytes;
            PublicKeySizeInBytes = publicKeySizeInBytes;
            SignatureSizeInBytes = signatureSizeInBytes;
            Oid = oid;
        }

        // TODO: Our algorithm names generally match CNG.  If they don't in this case, consider changing the values.
        // TODO: These values match OpenSSL names, if changing this for CNG, we should make sure to do the right thing for OpenSSL.

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-44 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa44 { get; } = new MLDsaAlgorithm("ML-DSA-44", 2560, 1312, 2420, Oids.MLDsa44);

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-65 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa65 { get; } = new MLDsaAlgorithm("ML-DSA-65", 4032, 1952, 3309, Oids.MLDsa65);

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-87 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa87 { get; } = new MLDsaAlgorithm("ML-DSA-87", 4896, 2592, 4627, Oids.MLDsa87);

        internal static MLDsaAlgorithm? GetMLDsaAlgorithmFromOid(string? oid)
        {
            return oid switch
            {
                Oids.MLDsa44 => MLDsa44,
                Oids.MLDsa65 => MLDsa65,
                Oids.MLDsa87 => MLDsa87,
                _ => null,
            };
        }
    }
}
