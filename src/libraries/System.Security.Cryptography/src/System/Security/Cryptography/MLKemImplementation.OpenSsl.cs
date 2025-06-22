// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class MLKemImplementation : MLKem
    {
        private SafeEvpPKeyHandle _key;

        // OpenSSL is expected to give "all or none" support.
        internal static new bool IsSupported => Interop.Crypto.EvpKemAlgs.MlKem512 is not null;

        private readonly bool _hasSeed;
        private readonly bool _hasDecapsulationKey;

        private MLKemImplementation(
            MLKemAlgorithm algorithm,
            SafeEvpPKeyHandle key,
            bool hasSeed,
            bool hasDecapsulationKey) : base(algorithm)
        {
            _key = key;
            _hasSeed = hasSeed;
            _hasDecapsulationKey = hasDecapsulationKey;
        }

        internal static MLKemImplementation GenerateKeyImpl(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportPrivateSeedImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName, source);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportDecapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.DecapsulationKeySizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(kemName, source, privateKey: true);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: true);
        }

        internal static MLKemImplementation ImportEncapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.EncapsulationKeySizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(kemName, source, privateKey: false);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key.Dispose();
            }
        }

        internal SafeEvpPKeyHandle DuplicateHandle() =>  _key.DuplicateHandle();

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);
            Interop.Crypto.EvpKemDecapsulate(_key, ciphertext, sharedSecret);
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemEncapsulate(_key, ciphertext, sharedSecret);
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            ThrowIfNoSeed(_hasSeed);
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            ThrowIfNoDecapsulationKey(_hasDecapsulationKey);
            Interop.Crypto.EvpKemExportDecapsulationKey(_key, destination);
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportEncapsulationKey(_key, destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            return MLKemPkcs8.TryExportPkcs8PrivateKey(
                this,
                _hasSeed,
                _hasDecapsulationKey,
                destination,
                out bytesWritten);
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
