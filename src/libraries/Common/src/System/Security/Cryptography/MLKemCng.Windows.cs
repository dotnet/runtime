// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Cryptography;
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

            duplicateKey = key.Duplicate();

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

            return _key.Duplicate();
        }

        internal CngKey KeyNoDuplicate => _key;

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

        [SupportedOSPlatform("windows")]
        internal static MLKemCng ImportPkcs8PrivateKey(byte[] source, out int bytesRead)
        {
            int len;

            try
            {
                AsnDecoder.ReadEncodedValue(
                    source,
                    AsnEncodingRules.BER,
                    out _,
                    out _,
                    out len);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            bytesRead = len;
            CngKey key;

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            ReadOnlySpan<byte> pkcs8Source = source.AsSpan(0, len);
#else
            using (Helpers.TrimAndTrack(source, bytesRead, out byte[] pkcs8Source))
#endif
            {
                try
                {
                    key = CngKey.Import(pkcs8Source, CngKeyBlobFormat.Pkcs8PrivateBlob);
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }
            }

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
#else
            CngKeyExtensions.SetExportPolicy(key, CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport);
#endif
            return new MLKemCng(key, transferOwnership: true);
        }

        /// <inheritdoc/>
        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(destination.Length == Algorithm.PrivateSeedSizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                ExportKeyWithEncryptedOnlyExport(
                    static (ref readonly mlKemPrivateKeyAsn, algorithm, destination) =>
                    {
                        ReadOnlySpan<byte> seedValue = default;
                        bool hasSeed = false;

                        if (mlKemPrivateKeyAsn.HasSeed)
                        {
                            hasSeed = true;
                            seedValue = mlKemPrivateKeyAsn.Seed;
                        }
                        else if (mlKemPrivateKeyAsn.HasBoth)
                        {
                            hasSeed = true;
                            seedValue = mlKemPrivateKeyAsn.Both.Seed;
                        }

                        if (hasSeed)
                        {
                            if (seedValue.Length != algorithm.PrivateSeedSizeInBytes)
                            {
                                throw new CryptographicException(SR.Argument_PrivateSeedWrongSizeForAlgorithm);
                            }

                            seedValue.CopyTo(destination);
                            return;
                        }

                        throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                    },
                    Algorithm,
                    destination);
            }
            else
            {
                ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, destination);
            }
        }

        /// <inheritdoc/>
        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Assert(IsSupported);
            Debug.Assert(destination.Length == Algorithm.DecapsulationKeySizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                ExportKeyWithEncryptedOnlyExport(static (ref readonly mlKemPrivateKeyAsn, algorithm, destination) =>
                {
                    ReadOnlySpan<byte> decapsulationKeyValue = default;
                    bool hasDecapsulationKey = false;

                    if (mlKemPrivateKeyAsn.HasExpandedKey)
                    {
                        hasDecapsulationKey = true;
                        decapsulationKeyValue = mlKemPrivateKeyAsn.ExpandedKey;
                    }
                    else if (mlKemPrivateKeyAsn.HasBoth)
                    {
                        hasDecapsulationKey = true;
                        decapsulationKeyValue = mlKemPrivateKeyAsn.Both.ExpandedKey;
                    }

                    if (hasDecapsulationKey)
                    {
                        if (decapsulationKeyValue.Length != algorithm.DecapsulationKeySizeInBytes)
                        {
                            throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                        }

                        decapsulationKeyValue.CopyTo(destination);
                        return;
                    }

                    if (mlKemPrivateKeyAsn.HasSeed)
                    {
                        ReadOnlySpan<byte> seedValue = mlKemPrivateKeyAsn.Seed;

                        if (seedValue.Length != algorithm.PrivateSeedSizeInBytes)
                        {
                            throw new CryptographicException(SR.Argument_PrivateSeedWrongSizeForAlgorithm);
                        }

                        using (MLKem cloned = MLKemImplementation.ImportPrivateSeedImpl(algorithm, seedValue))
                        {
                            cloned.ExportDecapsulationKey(destination);
                            return;
                        }
                    }

                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                },
                Algorithm,
                destination);
            }
            else
            {
                ExportKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, destination);
            }
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
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(_key);

            if (encryptedOnlyExport)
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

            return _key.TryExportKeyBlob(
                Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                destination,
                out bytesWritten);
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

        private delegate void KeySelectorFunc(
            ref readonly ValueMLKemPrivateKeyAsn mlKemPrivateKeyAsn,
            MLKemAlgorithm algorithm,
            Span<byte> destination);

        private void ExportKeyWithEncryptedOnlyExport(KeySelectorFunc keySelector, MLKemAlgorithm algorithm, Span<byte> destination)
        {
            ArraySegment<byte> pkcs8 = GetRentedPkcs8ForEncryptedOnlyExport();

            try
            {
                ReadOnlySpan<byte> privateKey = KeyFormatHelper.ReadPkcs8([Algorithm.Oid], pkcs8.AsSpan(), out _);
                scoped ValueMLKemPrivateKeyAsn mlKemPrivateKeyAsn;

                try
                {
                    ValueMLKemPrivateKeyAsn.Decode(privateKey, AsnEncodingRules.BER, out mlKemPrivateKeyAsn);
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                keySelector(ref mlKemPrivateKeyAsn, algorithm, destination);
            }
            finally
            {
                CryptoPool.Return(pkcs8);
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
