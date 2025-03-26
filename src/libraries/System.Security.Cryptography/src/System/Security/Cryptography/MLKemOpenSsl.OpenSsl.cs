// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class MLKemOpenSsl
    {
        private SafeEvpPKeyHandle _key;

        [MemberNotNull(nameof(_key))]
        private partial void Initialize(SafeEvpPKeyHandle upRefHandle) => _key = upRefHandle;

        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        private static partial MLKemAlgorithm AlgorithmFromHandle(SafeEvpPKeyHandle pkeyHandle, out SafeEvpPKeyHandle upRefHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);
            upRefHandle = pkeyHandle.DuplicateHandle();

            try
            {
                string name = Interop.Crypto.EvpKemGetName(upRefHandle);

                if (name == MLKemAlgorithm.MLKem512.Name)
                {
                    return MLKemAlgorithm.MLKem512;
                }
                if (name == MLKemAlgorithm.MLKem768.Name)
                {
                    return MLKemAlgorithm.MLKem768;
                }
                if (name == MLKemAlgorithm.MLKem1024.Name)
                {
                    return MLKemAlgorithm.MLKem1024;
                }

                throw new CryptographicException(SR.Format(SR.Cryptography_KemInvalidAlgorithmHandle, name));
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
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportDecapsulationKey(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportEncapsulationKey(_key, destination);
        }
    }
}
