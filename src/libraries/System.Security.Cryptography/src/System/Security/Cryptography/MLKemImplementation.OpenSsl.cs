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

        internal static MLKem GenerateKeyImpl(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName);
            return new MLKemImplementation(algorithm, key);
        }

        internal static MLKem ImportPrivateSeedImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName, source);
            return new MLKemImplementation(algorithm, key);
        }

        internal static MLKem ImportDecapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.DecapsulationKeySizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(kemName, source, privateKey: true);
            return new MLKemImplementation(algorithm, key);
        }

        internal static MLKem ImportEncapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.EncapsulationKeySizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(kemName, source, privateKey: false);
            return new MLKemImplementation(algorithm, key);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key.Dispose();
            }
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemDecapsulate(_key, ciphertext, sharedSecret);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemEncapsulate(_key, ciphertext, sharedSecret);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportDecapsulationKey(_key, destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportEncapsulationKey(_key, destination);
        }

        private static string MapAlgorithmToName(MLKemAlgorithm algorithm)
        {
            string? name = null;

            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                name = Interop.Crypto.EvpKemAlgs.MlKem512;
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                name = Interop.Crypto.EvpKemAlgs.MlKem768;
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                name = Interop.Crypto.EvpKemAlgs.MlKem1024;
            }

            if (name is null)
            {
                Debug.Fail("Unhandled ML-KEM algorithm or ML-KEM is not available.");
                throw new CryptographicException();
            }

            return name;
        }
    }
}
