// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class MLKemOpenSsl
    {
        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        private static partial MLKemAlgorithm AlgorithmFromHandle(
            SafeEvpPKeyHandle pkeyHandle,
            out SafeEvpPKeyHandle upRefHandle,
            out bool hasSeed,
            out bool hasDecapsulationKey)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));
            }

            upRefHandle = pkeyHandle.DuplicateHandle();

            try
            {
                Interop.Crypto.PalKemAlgorithmId kemId = Interop.Crypto.EvpKemGetKemIdentifier(
                    upRefHandle,
                    out hasSeed,
                    out hasDecapsulationKey);

                switch (kemId)
                {
                    case Interop.Crypto.PalKemAlgorithmId.MLKem512:
                        return MLKemAlgorithm.MLKem512;
                    case Interop.Crypto.PalKemAlgorithmId.MLKem768:
                        return MLKemAlgorithm.MLKem768;
                    case Interop.Crypto.PalKemAlgorithmId.MLKem1024:
                        return MLKemAlgorithm.MLKem1024;
                    default:
                        throw new CryptographicException(SR.Cryptography_KemInvalidAlgorithmHandle);
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
        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            // This cannot use _hasDecapsulationKey here because this field only indicates if it is exportable, not if
            // it is usable. This instance could represent an ML-KEM instance with a decapsulation key in hardware,
            // for example.
            Interop.Crypto.EvpKemDecapsulate(_key, ciphertext, sharedSecret);
        }

        /// <inheritdoc />
        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemEncapsulate(_key, ciphertext, sharedSecret);
        }

        /// <inheritdoc />
        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            ThrowIfNoSeed(_hasSeed);
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);
            Interop.Crypto.EvpKemExportDecapsulationKey(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportEncapsulationKey(_key, destination);
        }

        /// <inheritdoc />
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLKemPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasDecapsulationKey,
                destination,
                out bytesWritten);
        }
    }
}
