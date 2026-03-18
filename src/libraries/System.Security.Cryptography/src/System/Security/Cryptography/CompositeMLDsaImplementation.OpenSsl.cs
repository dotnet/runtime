// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaImplementation : CompositeMLDsa
    {
        private CompositeMLDsaImplementation(CompositeMLDsaAlgorithm algorithm)
            : base(algorithm)
        {
            throw new PlatformNotSupportedException();
        }

        internal static partial bool SupportsAny() => CompositeMLDsaManaged.SupportsAny();

        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm) =>
            CompositeMLDsaManaged.IsAlgorithmSupportedImpl(algorithm);

        internal static partial CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm) =>
            CompositeMLDsaManaged.GenerateKeyImpl(algorithm);

        internal static partial CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaManaged.ImportCompositeMLDsaPublicKeyImpl(algorithm, source);

        internal static partial CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source) =>
            CompositeMLDsaManaged.ImportCompositeMLDsaPrivateKeyImpl(algorithm, source);

        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();

        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();
    }
}
