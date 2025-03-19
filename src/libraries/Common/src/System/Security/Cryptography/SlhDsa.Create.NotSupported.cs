// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Security.Cryptography
{
    internal abstract partial class SlhDsa
    {
        // Platform specific helpers for creation

        private static bool SupportsAnyHelper() => false;

        private static SlhDsa GenerateKeyHelper(SlhDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        private static SlhDsa ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsa ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsa ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsa ImportSeed(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();
    }
}
