// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0060 // Remove unused parameter

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaImplementation : CompositeMLDsa
    {
        public CompositeMLDsaImplementation(CompositeMLDsaAlgorithm algorithm)
            : base(algorithm)
        {
            throw new PlatformNotSupportedException();
        }

        internal static bool SupportsAny() => false;

        internal static bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm) => false;

        internal static CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm) =>
            throw new PlatformNotSupportedException();

        internal static CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        internal static CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            throw new PlatformNotSupportedException();

        protected override bool TrySignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportCompositeMLDsaPublicKeyCore(ReadOnlySpan<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportCompositeMLDsaPrivateKeyCore(ReadOnlySpan<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();
    }
}
