// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Security.Cryptography
{
    internal abstract partial class SlhDsa
    {
        // Platform specific helpers for creation

        private static bool SupportsAnyHelper() => SlhDsaCng.SupportsAny();

        private static SlhDsaCng GenerateKeyHelper(SlhDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        private static SlhDsaCng ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsaCng ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsaCng ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        private static SlhDsaCng ImportSeed(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();
    }
}
