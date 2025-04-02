// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private MLDsaImplementation(MLDsaAlgorithm algorithm)
            : base(algorithm)
        {
            ThrowIfNotSupported();
        }

        internal static partial bool SupportsAny();

        internal static partial MLDsa GenerateKeyImpl(MLDsaAlgorithm algorithm);
        internal static partial MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsa ImportPkcs8PrivateKeyValue(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsa ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    }
}
