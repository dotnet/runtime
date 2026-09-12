// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.Pkcs;

namespace Internal.Cryptography
{
    internal abstract class KemRecipientInfoPal : RecipientInfoPal
    {
        public abstract AlgorithmIdentifier KeyDerivationAlgorithm { get; }
        public abstract AlgorithmIdentifier KeyEncapsulationAlgorithm { get; }
        public abstract ReadOnlyMemory<byte> KeyEncapsulationCiphertext { get; }
        public abstract int KeyEncryptionKeyLengthInBytes { get; }
        public abstract ReadOnlyMemory<byte>? UserKeyingMaterial { get; }
    }
}
