// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    public sealed partial class MLKemCng : MLKem
    {
        private const string NCRYPT_MLKEM_PARAMETER_SET_512 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_512;
        private const string NCRYPT_MLKEM_PARAMETER_SET_768 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_768;
        private const string NCRYPT_MLKEM_PARAMETER_SET_1024 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_1024;

        internal MLKemCng(CngKey key, bool transferOwnership) : base(AlgorithmFromHandleNoDuplicate(key))
        {
            Debug.Assert(key is not null);
            Debug.Assert(key.AlgorithmGroup == CngAlgorithmGroup.MLKem);
            Debug.Assert(transferOwnership);

            _key = key;
        }

        [SupportedOSPlatform("windows")]
        private static partial MLKemAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey)
        {
            ArgumentNullException.ThrowIfNull(key);
            ThrowIfNotSupported();

            if (key.AlgorithmGroup != CngAlgorithmGroup.MLKem)
            {
                throw new ArgumentException(SR.Cryptography_ArgMLKemRequiresMLKemKey, nameof(key));
            }

            MLKemAlgorithm algorithm = AlgorithmFromHandleImpl(key);

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            duplicateKey = CngHelpers.Duplicate(key.HandleNoDuplicate, key.IsEphemeral);
#else
            duplicateKey = key.Duplicate();
#endif

            return algorithm;
        }

        private static MLKemAlgorithm AlgorithmFromHandleNoDuplicate(CngKey key)
        {
            if (key.AlgorithmGroup != CngAlgorithmGroup.MLKem)
            {
                throw new CryptographicException(SR.Cryptography_ArgMLKemRequiresMLKemKey);
            }

            Debug.Assert(key is not null);

            return AlgorithmFromHandleImpl(key);
        }

        private static MLKemAlgorithm AlgorithmFromHandleImpl(CngKey key)
        {
            string? parameterSet =
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                key.HandleNoDuplicate.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#else
                key.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#endif

            return parameterSet switch
            {
                NCRYPT_MLKEM_PARAMETER_SET_512 => MLKemAlgorithm.MLKem512,
                NCRYPT_MLKEM_PARAMETER_SET_768 => MLKemAlgorithm.MLKem768,
                NCRYPT_MLKEM_PARAMETER_SET_1024 => MLKemAlgorithm.MLKem1024,
                _ => throw DebugFailAndGetException(parameterSet),
            };

            static Exception DebugFailAndGetException(string? parameterSet)
            {
                Debug.Fail($"Unexpected parameter set '{parameterSet}'.");
                return new CryptographicException();
            }
        }

        public partial CngKey GetKey()
        {
            ThrowIfDisposed();

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            return CngHelpers.Duplicate(_key.HandleNoDuplicate, _key.IsEphemeral);
#else
#pragma warning disable CA1416 // only supported on: 'windows'
            return _key.Duplicate();
#pragma warning restore CA1416 // only supported on: 'windows'
#endif
        }

        /// <inheritdoc/>
        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(ciphertext.Length == Algorithm.CiphertextSizeInBytes);
            Debug.Assert(sharedSecret.Length == Algorithm.SharedSecretSizeInBytes);

            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                uint written = Interop.NCrypt.NCryptDecapsulate(duplicatedHandle, ciphertext, sharedSecret, 0);
                Debug.Assert(written == (uint)sharedSecret.Length);
            }
        }

        /// <inheritdoc/>
        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Assert(IsSupported);
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

                Debug.Assert(sharedSecretWritten == (uint)sharedSecret.Length);
                Debug.Assert(ciphertextWritten == (uint)ciphertext.Length);
            }
        }

        /// <inheritdoc/>
        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(destination.Length == Algorithm.PrivateSeedSizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                // Windows ncrypt does not yet give us an encrypted PKCS#8 export. For now, we have to throw an exception
                // indicating the seed is not extractable.
                throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
            }

            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, destination);
        }

        /// <inheritdoc/>
        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(destination.Length == Algorithm.DecapsulationKeySizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                // Windows ncrypt does not yet give us an encrypted PKCS#8 export. For now, we have to throw an exception
                // indicating the key is not extractable.
                throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
            }

            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, destination);
        }

        /// <inheritdoc/>
        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(destination.Length == Algorithm.EncapsulationKeySizeInBytes);

            ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, destination);
        }

        /// <inheritdoc/>
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // Windows ncrypt does not yet have functional PKCS#8 exports. For now, try exporting it as a seed or
            // decapsulation key.
            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
            }

            // Since Windows does not have a PKCS#8 export yet, try exporting the seed, and if that fails, the
            // decapsulation key. If that fails, then we cannot export the key. When native PKCS#8 export is available
            // this will use that instead.
            try
            {
                return MLKemPkcs8.TryExportPkcs8PrivateKey(
                    this,
                    hasSeed: true,
                    hasDecapsulationKey: true,
                    destination,
                    out bytesWritten);
            }
            catch (CryptographicException)
            {
                try
                {
                    return MLKemPkcs8.TryExportPkcs8PrivateKey(
                        this,
                        hasSeed: false,
                        hasDecapsulationKey: true,
                        destination,
                        out bytesWritten);
                }
                catch (CryptographicException)
                {
                    throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private void ExportKey(KeyBlobMagicNumber kind, Span<byte> destination)
        {
            Debug.Assert(kind is KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC or
                KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC or
                KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC);

            if (kind != KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC && _key.ExportPolicy == CngExportPolicies.None)
            {
                throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
            }

            int bufferSize;
            string blobKind = PqcBlobHelpers.MLKemBlobMagicToBlobType(kind);

            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                    duplicatedHandle,
                    IntPtr.Zero,
                    blobKind,
                    IntPtr.Zero,
                    null,
                    0,
                    out bufferSize,
                    0);

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }

                byte[] buffer = CryptoPool.Rent(bufferSize);
                PinAndClear pin = PinAndClear.Track(buffer);

                try
                {
                    errorCode = Interop.NCrypt.NCryptExportKey(
                        duplicatedHandle,
                        IntPtr.Zero,
                        blobKind,
                        IntPtr.Zero,
                        buffer,
                        bufferSize,
                        out int written,
                        0);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    ReadCngMLKemBlob(kind, buffer.AsSpan(0, written), destination);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buffer);
                    pin.Dispose();
                    CryptoPool.Return(buffer, clearSize: 0); // Manually cleared above.
                }
            }
        }
    }
}
