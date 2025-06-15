// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class MLDsaOpenSsl : MLDsa
    {
        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        private static partial MLDsaAlgorithm AlgorithmFromHandle(
            SafeEvpPKeyHandle pkeyHandle,
            out SafeEvpPKeyHandle upRefHandle,
            out bool hasSeed,
            out bool hasSecretKey)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));
            }

            upRefHandle = pkeyHandle.DuplicateHandle();

            try
            {
                Interop.Crypto.PalMLDsaAlgorithmId mldsaId = Interop.Crypto.MLDsaGetPalId(
                    upRefHandle,
                    out hasSeed,
                    out hasSecretKey);

                switch (mldsaId)
                {
                    case Interop.Crypto.PalMLDsaAlgorithmId.MLDsa44:
                        return MLDsaAlgorithm.MLDsa44;
                    case Interop.Crypto.PalMLDsaAlgorithmId.MLDsa65:
                        return MLDsaAlgorithm.MLDsa65;
                    case Interop.Crypto.PalMLDsaAlgorithmId.MLDsa87:
                        return MLDsaAlgorithm.MLDsa87;
                    default:
                        throw new CryptographicException(SR.Cryptography_MLDsaInvalidAlgorithmHandle);
                }
            }
            catch
            {
                upRefHandle.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            Interop.Crypto.MLDsaSignPure(_key, data, context, destination);

        /// <inheritdoc />
        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.Crypto.MLDsaVerifyPure(_key, data, context, signature);

        /// <inheritdoc />
        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            Interop.Crypto.MLDsaExportPublicKey(_key, destination);

        /// <inheritdoc />
        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            if (!_hasSecretKey)
            {
                throw new CryptographicException(SR.Cryptography_MLDsaNoSecretKey);
            }

            Interop.Crypto.MLDsaExportSecretKey(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            if (!_hasSeed)
            {
                throw new CryptographicException(SR.Cryptography_PqcNoSeed);
            }

            Interop.Crypto.MLDsaExportSeed(_key, destination);
        }

        /// <inheritdoc />
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLDsaPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasSecretKey,
                destination,
                out bytesWritten);
        }
    }
}
