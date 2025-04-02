// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class MLKemImplementation : MLKem
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

        internal static MLKem GenerateKeyImpl(MLKemAlgorithm algorithm)
        {
            Debug.Assert(IsSupported);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKem ImportPrivateSeedImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.PrivateSeedSizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpKemGeneratePkey(kemName, source);
            return new MLKemImplementation(algorithm, key, hasSeed: true, hasDecapsulationKey: true);
        }

        internal static MLKem ImportDecapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(source.Length == algorithm.DecapsulationKeySizeInBytes);
            string kemName = MapAlgorithmToName(algorithm);
            SafeEvpPKeyHandle key = Interop.Crypto.EvpPKeyFromData(kemName, source, privateKey: true);
            return new MLKemImplementation(algorithm, key, hasSeed: false, hasDecapsulationKey: true);
        }

        internal static MLKem ImportEncapsulationKeyImpl(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
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

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            string oid = Algorithm.Oid;
            AlgorithmIdentifierAsn algorithmIdentifier = new()
            {
                Algorithm = oid,
                Parameters = default(ReadOnlyMemory<byte>?),
            };

            MLKemPrivateKeyAsn privateKeyAsn = default;
            byte[]? rented = null;
            int written = 0;

            try
            {
                if (_hasSeed)
                {
                    int seedSize = Algorithm.PrivateSeedSizeInBytes;
                    rented = CryptoPool.Rent(seedSize);
                    Memory<byte> buffer = rented.AsMemory(0, seedSize);
                    ExportPrivateSeedCore(buffer.Span);
                    written = buffer.Length;
                    privateKeyAsn.Seed = buffer;
                }
                else if (_hasDecapsulationKey)
                {
                    int decapsulationKeySize = Algorithm.DecapsulationKeySizeInBytes;
                    rented = CryptoPool.Rent(decapsulationKeySize);
                    Memory<byte> buffer = rented.AsMemory(0, decapsulationKeySize);
                    ExportDecapsulationKeyCore(buffer.Span);
                    written = buffer.Length;
                    privateKeyAsn.ExpandedKey = buffer;
                }
                else
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                }

                AsnWriter algorithmWriter = new(AsnEncodingRules.DER);
                algorithmIdentifier.Encode(algorithmWriter);
                AsnWriter privateKeyWriter = new(AsnEncodingRules.DER);
                privateKeyAsn.Encode(privateKeyWriter);
                AsnWriter pkcs8Writer = KeyFormatHelper.WritePkcs8(algorithmWriter, privateKeyWriter);

                bool result = pkcs8Writer.TryEncode(destination, out bytesWritten);
                privateKeyWriter.Reset();
                pkcs8Writer.Reset();
                return result;
            }
            finally
            {
                if (rented is not null)
                {
                    CryptoPool.Return(rented, written);
                }
            }
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
