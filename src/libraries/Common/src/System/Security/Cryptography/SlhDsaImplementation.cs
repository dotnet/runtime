// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed partial class SlhDsaImplementation : SlhDsa
    {
        internal static partial bool SupportsAny();

        internal static partial SlhDsa GenerateKeyCore(SlhDsaAlgorithm algorithm);
        internal static partial SlhDsa ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial SlhDsa ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial SlhDsa ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
    }
}
