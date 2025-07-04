// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed partial class SlhDsaImplementation : SlhDsa
    {
        internal static partial bool SupportsAny() => false;

        // TODO: Define this in terms of Windows BCrypt.dll (ephemeral keys)
        private SlhDsaImplementation(/* CngKey key, */ SlhDsaAlgorithm algorithm) : base(algorithm) =>
            throw new PlatformNotSupportedException();

        internal static partial SlhDsaImplementation GenerateKeyCore(SlhDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override void ExportSlhDsaSecretKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        internal static partial SlhDsaImplementation ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SlhDsaImplementation ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static partial SlhDsaImplementation ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();
    }
}
