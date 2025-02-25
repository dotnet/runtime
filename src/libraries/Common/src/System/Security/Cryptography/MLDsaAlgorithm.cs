// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the ML-DSA family.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal struct MLDsaAlgorithm : IEquatable<MLDsaAlgorithm>
    {
        /// <summary>
        ///   Gets the underlying string representation of the algorithm name.
        /// </summary>
        /// <value>
        ///   The underlying string representation of the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MLDsaAlgorithm" /> structure with a custom name.
        /// </summary>
        /// <param name="name">
        ///   The name of the algorithm.
        /// </param>
        public MLDsaAlgorithm(string name)
        {
            Name = name;
        }

        // TODO: Our algorithm names generally match CNG.  If they don't in this case, consider changing the values.

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-44 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-44 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa44 { get; } = new MLDsaAlgorithm("ML-DSA-44");

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-65 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-65 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa65 { get; } = new MLDsaAlgorithm("ML-DSA-65");

        /// <summary>
        ///   Gets an ML-DSA algorithm identifier for the ML-DSA-87 algorithm.
        /// </summary>
        /// <value>
        ///   An ML-DSA algorithm identifier for the ML-DSA-87 algorithm.
        /// </value>
        public static MLDsaAlgorithm MLDsa87 { get; } = new MLDsaAlgorithm("ML-DSA-87");

        /// <summary>
        ///   Indicates whether two <see cref="MLDsaAlgorithm"/> values are equal.
        /// </summary>
        /// <param name="other">
        ///   The object to compare with the current instance.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the <see cref="Name" /> property of <paramref name="other" /> is equal
        ///   to that of the current instance;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        public bool Equals(MLDsaAlgorithm other)
        {
            return Name == other.Name;
        }

        /// <summary>
        ///   Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">
        ///   The object to compare with the current instance.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="obj" /> is a <see cref="MLDsaAlgorithm" /> value and
        ///   its <see cref="Name" /> property is equal to that of the current instance;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        public override bool Equals(object? obj)
        {
            return obj is MLDsaAlgorithm other && Equals(other);
        }

        /// <summary>
        ///   Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        /// <summary>
        ///   Determines whether two <see cref="MLDsaAlgorithm"/> specified values are equal.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        ///   <see langword="true" /> if both values have the same the <see cref="Name" /> value;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        public static bool operator ==(MLDsaAlgorithm left, MLDsaAlgorithm right)
        {
            return left.Equals(right);
        }

        /// <summary>
        ///   Determines whether two <see cref="MLDsaAlgorithm"/> specified values are not equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        ///   <see langword="true" /> if both values do not have the same the <see cref="Name" /> value;
        ///   otherwise, <see langword="false" />.
        public static bool operator !=(MLDsaAlgorithm left, MLDsaAlgorithm right)
        {
            return !left.Equals(right);
        }
    }
}
