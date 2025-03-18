// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class MLKemImplementation : MLKem
    {
        private SafeEvpPKeyHandle _key;

        // OpenSSL is expected to give "all or none" support.
        internal static new bool IsSupported => Interop.Crypto.EvpKemAlgs.MlKem512 is not null;

        private MLKemImplementation(MLKemAlgorithm algorithm, SafeEvpPKeyHandle key) : base(algorithm)
        {
            _key = key;
        }

        internal static MLKem Generate(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);

            SafeEvpKemHandle? handle = null; // Shared Handle. Do not dispose.

            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                handle = Interop.Crypto.EvpKemAlgs.MlKem512;
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                handle = Interop.Crypto.EvpKemAlgs.MlKem768;
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                handle = Interop.Crypto.EvpKemAlgs.MlKem1024;
            }

            if (handle is null)
            {
                Debug.Fail("Unhandled ML-KEM algorithm or ML-KEM is not available.");
                throw new CryptographicException();
            }

            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(handle);
            return new MLKemImplementation(algorithm, key);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key?.Dispose();
            }
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            throw new NotImplementedException();
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            throw new NotImplementedException();
        }

        protected override void ExportMLKemPrivateSeedCore(Span<byte> destination)
        {
            ThrowIfDisposed();
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }
    }
}
