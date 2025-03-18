// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed partial class SLHDsaImplementation : SLHDsa
    {
        private SLHDsaImplementation(SLHDsaAlgorithm algorithm)
            : base(algorithm)
        {
            ThrowIfNotSupported();
        }

        internal static partial bool SupportsAny();

        internal static partial SLHDsa GenerateKey(SLHDsaAlgorithm algorithm);
        internal static partial SLHDsa ImportPublicKey(ParameterSetInfo info, ReadOnlySpan<byte> source);
        internal static partial SLHDsa ImportPkcs8PrivateKeyValue(ParameterSetInfo info, ReadOnlySpan<byte> source);
        internal static partial SLHDsa ImportSecretKey(ParameterSetInfo info, ReadOnlySpan<byte> source);
        internal static partial SLHDsa ImportSeed(ParameterSetInfo info, ReadOnlySpan<byte> source);
    }
}
