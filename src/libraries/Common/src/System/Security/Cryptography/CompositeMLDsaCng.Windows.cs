// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public sealed partial class CompositeMLDsaCng : CompositeMLDsa
    {
        public partial CngKey GetKey() =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));

        /// <inheritdoc/>
        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));

        /// <inheritdoc/>
        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));

        /// <inheritdoc/>
        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));

        /// <inheritdoc/>
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));

        /// <inheritdoc/>
        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));
    }
}
