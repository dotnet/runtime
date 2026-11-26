// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.Pkcs;

namespace Internal.Cryptography
{
    internal abstract class KemRecipientInfoPal : RecipientInfoPal
    {
        internal abstract AlgorithmIdentifier KeyDerivationAlgorithm { get; }
        internal abstract AlgorithmIdentifier KeyEncapsulationAlgorithm { get; }
        internal abstract ReadOnlyMemory<byte> KeyEncapsulationCiphertext { get; }
        internal abstract int KeyEncryptionKeyLengthInBytes { get; }
        internal abstract ReadOnlyMemory<byte>? UserKeyingMaterial { get; }
    }
}
