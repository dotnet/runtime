// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        internal static partial bool SupportsAny() => false;

        // The instance override methods are unreachable, as the constructor will always throw.
        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa GenerateKey(MLDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa ImportPublicKey(MLDsa.ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa ImportPkcs8PrivateKeyValue(MLDsa.ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa ImportSecretKey(ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial MLDsa ImportSeed(ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();
    }
}
