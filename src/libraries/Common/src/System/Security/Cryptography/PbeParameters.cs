// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents parameters to be used for Password-Based Encryption (PBE).
    /// </summary>
    public sealed class PbeParameters
    {
        /// <summary>
        ///   Gets the algorithm to use when encrypting data.
        /// </summary>
        /// <value>
        ///   The algorithm to use when encrypting data.
        /// </value>
        public PbeEncryptionAlgorithm EncryptionAlgorithm { get; }

        /// <summary>
        ///   Gets the name of the hash algorithm to use with the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </summary>
        /// <value>
        ///   The name of the hash algorithm to use with the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </value>
        public HashAlgorithmName HashAlgorithm { get; }

        /// <summary>
        ///   Gets the iteration count to provide to the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </summary>
        /// <value>
        ///   The iteration count to provide to the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </value>
        public int IterationCount { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="PbeParameters" /> class.
        /// </summary>
        /// <param name="encryptionAlgorithm">The algorithm to use when encrypting data.</param>
        /// <param name="hashAlgorithm">
        ///   The name of a hash algorithm to use with the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </param>
        /// <param name="iterationCount">
        ///   The iteration count to provide to the Key Derivation Function (KDF) to turn a password
        ///   into an encryption key.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="iterationCount" /> is less than 1.
        /// </exception>
        public PbeParameters(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            HashAlgorithmName hashAlgorithm,
            int iterationCount)
        {
#if NET
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterationCount);
#else
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(iterationCount),
                    iterationCount,
                    SR.Format(
                        SR.ArgumentOutOfRange_Generic_MustBeNonNegativeNonZero,
                        nameof(iterationCount),
                        iterationCount));
            }
#endif

            EncryptionAlgorithm = encryptionAlgorithm;
            HashAlgorithm = hashAlgorithm;
            IterationCount = iterationCount;
        }
    }
}
