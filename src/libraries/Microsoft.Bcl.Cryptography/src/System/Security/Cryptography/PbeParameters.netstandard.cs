// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
#if !NET
    internal sealed class PbeParameters
    {
        public PbeEncryptionAlgorithm EncryptionAlgorithm { get; }
        public HashAlgorithmName HashAlgorithm { get; }
        public int IterationCount { get; }

        public PbeParameters(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            HashAlgorithmName hashAlgorithm,
            int iterationCount)
        {
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

            EncryptionAlgorithm = encryptionAlgorithm;
            HashAlgorithm = hashAlgorithm;
            IterationCount = iterationCount;
        }
    }
#endif
}
