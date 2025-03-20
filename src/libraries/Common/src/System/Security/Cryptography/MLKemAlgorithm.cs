// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the ML-KEM family.
    /// </summary>
    /// <seealso cref="MLKem" />
    [DebuggerDisplay("{Name,nq}")]
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public sealed class MLKemAlgorithm : IEquatable<MLKemAlgorithm>
    {
        private MLKemAlgorithm(
            string name,
            int encapsulationKeySizeInBytes,
            int decapsulationKeySizeInBytes,
            int ciphertextSizeInBytes)
        {
            Name = name;
            EncapsulationKeySizeInBytes = encapsulationKeySizeInBytes;
            DecapsulationKeySizeInBytes = decapsulationKeySizeInBytes;
            CiphertextSizeInBytes = ciphertextSizeInBytes;
        }

        // Values are from NIST FIPS-203 table 3.

        /// <summary>
        ///   Gets an ML-KEM algorithm identifier for the ML-KEM-512 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-KEM algorithm identifier for the ML-KEM-512 algorithm.
        /// </value>
        public static MLKemAlgorithm MLKem512 { get; } = new("ML-KEM-512", 800, 1632, 768);

        /// <summary>
        ///   Gets an ML-KEM algorithm identifier for the ML-KEM-768 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-KEM algorithm identifier for the ML-KEM-768 algorithm.
        /// </value>
        public static MLKemAlgorithm MLKem768 { get; } = new("ML-KEM-768", 1184, 2400, 1088);

        /// <summary>
        ///   Gets an ML-KEM algorithm identifier for the ML-KEM-1024 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-KEM algorithm identifier for the ML-KEM-1024 algorithm.
        /// </value>
        public static MLKemAlgorithm MLKem1024 { get; } = new("ML-KEM-1024", 1568, 3168, 1568);

        /// <summary>
        ///   Gets the name of the algorithm.
        /// </summary>
        /// <value>
        ///   An a string representing the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///   Gets size of the encapsulation key for the algorithm, in bytes.
        /// </summary>
        /// <value>
        /// The size of the encapsulation key for the algorithm, in bytes.
        /// </value>
        public int EncapsulationKeySizeInBytes { get; }

        /// <summary>
        ///   Gets size of the decapsulation key for the algorithm, in bytes.
        /// </summary>
        /// <value>
        /// The size of the decapsulation key for the algorithm, in bytes.
        /// </value>
        public int DecapsulationKeySizeInBytes { get; }

        /// <summary>
        ///   Gets size of the ciphertext for the algorithm, in bytes.
        /// </summary>
        /// <value>
        /// The size of the ciphertext for the algorithm, in bytes.
        /// </value>
        public int CiphertextSizeInBytes { get; }

        /// <summary>
        ///   Gets size of the shared secret for the algorithm, in bytes.
        /// </summary>
        /// <value>
        /// The size of the shared secret for the algorithm, in bytes.
        /// </value>
        // Right now every shared secret for ML-KEM is 32 bytes. If or when a different shared secret
        // size is needed, then it can be provided as input to the private constructor.
        public int SharedSecretSizeInBytes { get; } = 32;

        /// <summary>
        ///   Gets size of the private seed for the algorithm, in bytes.
        /// </summary>
        /// <value>
        /// The size of the private seed for the algorithm, in bytes.
        /// </value>
        // Right now every seed for ML-KEM is 64 bytes. If or when a different seed
        // size is needed, then it can be provided as input to the private constructor.
        public int PrivateSeedSizeInBytes { get; } = 64;

        /// <summary>
        ///   Compares two <see cref="MLKemAlgorithm" /> objects.
        /// </summary>
        /// <param name="other">
        ///   An object to be compared to the current <see cref="MLKemAlgorithm"/> object.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the objects are considered equal; otherwise, <see langword="false" />.
        /// </returns>
        // This is a closed type, so all we need to compare are the names.
        public bool Equals(MLKemAlgorithm? other) => other is not null && other.Name == Name;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is MLKemAlgorithm alg && alg.Name == Name;

        /// <inheritdoc />
        public override int GetHashCode() => Name.GetHashCode();

        /// <summary>
        ///   Determines whether two <see cref="MLKemAlgorithm" /> objects specify the same algorithm name.
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
        public static bool operator ==(MLKemAlgorithm? left, MLKemAlgorithm? right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="MLKemAlgorithm" /> objects do not specify the same algorithm name.
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
        public static bool operator !=(MLKemAlgorithm? left, MLKemAlgorithm? right)
        {
            return !(left == right);
        }
    }
}
