// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed partial class SLHDsaImplementation : SLHDsa
    {
        internal static partial bool SupportsAny() => false;

        // The instance override methods are unreachable, as the constructor will always throw.
        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override void ExportSLHDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override void ExportSLHDsaSecretKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override void ExportSLHDsaPrivateSeedCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        internal static partial SLHDsa GenerateKey(SLHDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static partial SLHDsa ImportPublicKey(SLHDsa.ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SLHDsa ImportPkcs8PrivateKeyValue(SLHDsa.ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SLHDsa ImportSecretKey(ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SLHDsa ImportSeed(ParameterSetInfo info, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();
    }
}
