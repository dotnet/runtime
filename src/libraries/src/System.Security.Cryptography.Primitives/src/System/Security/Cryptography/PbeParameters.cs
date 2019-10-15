// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (iterationCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(iterationCount),
                    iterationCount,
                    SR.ArgumentOutOfRange_NeedPosNum);
            }

            EncryptionAlgorithm = encryptionAlgorithm;
            HashAlgorithm = hashAlgorithm;
            IterationCount = iterationCount;
        }
    }
}
