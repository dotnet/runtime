// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the ML-DSA family.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed class MLDsaAlgorithm
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
        private MLDsaAlgorithm(string name)
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
    }
}
