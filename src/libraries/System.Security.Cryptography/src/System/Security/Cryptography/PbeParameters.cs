// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed class PbeParameters
    {
        public PbeEncryptionAlgorithm EncryptionAlgorithm { get; }
        public HashAlgorithmName HashAlgorithm { get; }
        public int IterationCount { get; }

        public PbeParameters(
            PbeEncryptionAlgorithm encryptionAlgorithm,
            HashAlgorithmName hashAlgorithm,
            int iterationCount)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(iterationCount, 1);

            EncryptionAlgorithm = encryptionAlgorithm;
            HashAlgorithm = hashAlgorithm;
            IterationCount = iterationCount;
        }
    }
}
