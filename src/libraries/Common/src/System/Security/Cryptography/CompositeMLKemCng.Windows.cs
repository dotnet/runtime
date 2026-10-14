// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class CompositeMLKemCng : CompositeMLKem
    {
        private const string NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256;
        private const string NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519;
        private const string NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384;

        private static partial CompositeMLKemAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (!CompositeMLKemImplementation.IsSupported)
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
            }

            if (key.AlgorithmGroup != CngAlgorithmGroup.CompositeMLKem)
            {
                throw new ArgumentException(SR.Cryptography_ArgCompositeMLKemRequiresCompositeMLKemKey, nameof(key));
            }

            CompositeMLKemAlgorithm algorithm = AlgorithmFromHandleImpl(key);
            duplicateKey = key.Duplicate();
            return algorithm;
        }

        private static CompositeMLKemAlgorithm AlgorithmFromHandleImpl(CngKey key)
        {
            string? parameterSet =
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                key.HandleNoDuplicate.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#else
                key.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#endif

            return parameterSet switch
            {
                NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256 => CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256,
                NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519 => CompositeMLKemAlgorithm.MLKem768WithX25519,
                NCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384 => CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384,
                _ => throw Fail(parameterSet),
            };

            static CryptographicException Fail(string? parameterSet)
            {
                Debug.Fail($"Unexpected Composite ML-KEM parameter set '{parameterSet}'.");
                return new CryptographicException();
            }
        }

        public partial CngKey GetKey()
        {
            ThrowIfDisposed();

            return _key.Duplicate();
        }

        /// <inheritdoc/>
        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsAlgorithmSupported(Algorithm));
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                uint written = Interop.NCrypt.NCryptDecapsulate(duplicatedHandle, ciphertext, sharedSecret, 0);

                if (written != (uint)sharedSecret.Length)
                {
                    Debug.Fail($"Unexpected number of bytes written by NCryptDecapsulate: {written}.");
                    throw new CryptographicException();
                }
            }
        }

        /// <inheritdoc/>
        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsAlgorithmSupported(Algorithm));
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                Interop.NCrypt.NCryptEncapsulate(
                    duplicatedHandle,
                    sharedSecret,
                    ciphertext,
                    out uint sharedSecretWritten,
                    out uint ciphertextWritten,
                    0);

                if (sharedSecretWritten != (uint)sharedSecret.Length ||
                    ciphertextWritten != (uint)ciphertext.Length)
                {
                    Debug.Fail($"Unexpected number of bytes written by NCryptEncapsulate: {sharedSecretWritten} shared secret bytes, {ciphertextWritten} ciphertext bytes.");
                    throw new CryptographicException();
                }
            }
        }

        /// <inheritdoc/>
        protected override int ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(IsAlgorithmSupported(Algorithm));
            Debug.Assert(destination.Length == Algorithm.MaxEncapsulationKeySizeInBytes);

            return ExportKey(CngKeyBlobFormat.CompositeMLKemPublicBlob, destination);
        }

        /// <inheritdoc/>
        protected override int ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(IsAlgorithmSupported(Algorithm));
            Debug.Assert(destination.Length == Algorithm.MaxDecapsulationKeySizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                ArraySegment<byte> pkcs8 = GetRentedPkcs8ForEncryptedOnlyExport();

                try
                {
                    ReadOnlySpan<byte> key = KeyFormatHelper.ReadPkcs8([Algorithm.Oid], pkcs8.AsSpan(), out _);

                    if (!key.TryCopyTo(destination))
                    {
                        Debug.Fail($"Decapsulation key size too large for buffer: {key.Length} / {destination.Length}");
                        throw new CryptographicException();
                    }

                    return key.Length;
                }
                finally
                {
                    CryptoPool.Return(pkcs8);
                }
            }

            return ExportKey(CngKeyBlobFormat.CompositeMLKemPrivateBlob, destination);
        }

        /// <inheritdoc/>
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(IsAlgorithmSupported(Algorithm));

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                ArraySegment<byte> pkcs8 = GetRentedPkcs8ForEncryptedOnlyExport();

                try
                {
                    if (destination.Length < pkcs8.Count)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    bytesWritten = pkcs8.Count;
                    pkcs8.AsSpan().CopyTo(destination);
                    return true;
                }
                finally
                {
                    CryptoPool.Return(pkcs8);
                }
            }

            // Windows NCrypt does not yet support PKCS#8 export for Composite ML-KEM, so build it from the private key.
            return TryExportPkcs8FromExportedDecapsulationKey(destination, out bytesWritten);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private int ExportKey(CngKeyBlobFormat blobFormat, Span<byte> destination)
        {
            byte[] blob = _key.Export(blobFormat);

            using (PinAndClear.Track(blob))
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeCompositeMLKemBlob(
                    blob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                if (!PqcBlobHelpers.TryGetCompositeMLKemParameterSet(Algorithm, out string? expectedParameterSet))
                {
                    Debug.Fail($"Unknown algorithm {Algorithm.Name}.");
                    throw new CryptographicException();
                }

                if (blobType != blobFormat.Format ||
                    keyBytes.Length > destination.Length ||
                    !parameterSet.SequenceEqual(expectedParameterSet))
                {
                    Debug.Fail(
                        $"{nameof(blobType)}: {blobType}, " +
                        $"{nameof(parameterSet)}: {parameterSet.ToString()}, " +
                        $"{nameof(keyBytes)}.Length: {keyBytes.Length} / {destination.Length}");

                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
                return keyBytes.Length;
            }
        }

        private ArraySegment<byte> GetRentedPkcs8ForEncryptedOnlyExport()
        {
            const string TemporaryExportPassword = "DotnetExportPhrase";
            byte[] exported = _key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);

            using (PinAndClear.Track(exported))
            {
                return KeyFormatHelper.DecryptPkcs8(
                    TemporaryExportPassword,
                    exported,
                    out _);
            }
        }
    }
}
