// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a specific algorithm within the SHL-DSA family.
    /// </summary>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed class SLHDsaAlgorithm
    {
        /// <summary>
        ///   Gets the underlying string representation of the algorithm name.
        /// </summary>
        /// <value>
        ///   The underlying string representation of the algorithm name.
        /// </value>
        public string Name { get; }

        /// <summary>
        ///  Initializes a new instance of the <see cref="SLHDsaAlgorithm" /> structure with a custom name.
        /// </summary>
        /// <param name="name">
        ///   The name of the algorithm.
        /// </param>
        private SLHDsaAlgorithm(string name)
        {
            Name = name;
        }

        // TODO: Our algorithm names generally match CNG.  If they don't in this case, consider changing the values.

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_128s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-128s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_128s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-128s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-128f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_128f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-128f");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-128f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_128f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-128f");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_192s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-192s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_192s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-192s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-192f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_192f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-192f");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-192f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_192f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-192f");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_256s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-256s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256s algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256s algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_256s { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-256s");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHA2-256f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaSha2_256f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHA2-256f");

        /// <summary>
        ///   Gets an SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256f algorithm.
        /// </summary>
        /// <value>
        ///   An SLH-DSA algorithm identifier for the SLH-DSA-SHAKE-256f algorithm.
        /// </value>
        public static SLHDsaAlgorithm SLHDsaShake_256f { get; } = new SLHDsaAlgorithm("SLH-DSA-SHAKE-256f");
    }
}
