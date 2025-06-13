// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;

using BCRYPT_PQDSA_PADDING_INFO = Interop.BCrypt.BCRYPT_PQDSA_PADDING_INFO;

namespace System.Security.Cryptography
{
    public sealed partial class MLDsaCng : MLDsa
    {
        private const string NCRYPT_MLDSA_PARAMETER_SET_44 = PqcBlobHelpers.BCRYPT_MLDSA_PARAMETER_SET_44;
        private const string NCRYPT_MLDSA_PARAMETER_SET_65 = PqcBlobHelpers.BCRYPT_MLDSA_PARAMETER_SET_65;
        private const string NCRYPT_MLDSA_PARAMETER_SET_87 = PqcBlobHelpers.BCRYPT_MLDSA_PARAMETER_SET_87;

        /// <summary>
        ///     Creates a new MLDsaCng object that will use the specified key. Unlike the public
        ///     constructor, this does not copy the key and ownership is transferred. The
        ///     <paramref name="transferOwnership"/> parameter must be true.
        /// </summary>
        /// <param name="key">Key to use for MLDsa operations</param>
        /// <param name="transferOwnership">
        /// Must be true. Signals that ownership of <paramref name="key"/> will be transferred to the new instance.
        /// </param>
        internal MLDsaCng(CngKey key, bool transferOwnership)
            : base(AlgorithmFromHandleNoDuplicate(key))
        {
            Debug.Assert(key is not null);
            Debug.Assert(key.AlgorithmGroup == CngAlgorithmGroup.MLDsa);
            Debug.Assert(transferOwnership);

            _key = key;
        }

        private static partial MLDsaAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.AlgorithmGroup != CngAlgorithmGroup.MLDsa)
            {
                throw new ArgumentException(SR.Cryptography_ArgMLDsaRequiresMLDsaKey, nameof(key));
            }

            MLDsaAlgorithm algorithm = AlgorithmFromHandleImpl(key);
            duplicateKey = CngAlgorithmCore.Duplicate(key);
            return algorithm;
        }

        private static MLDsaAlgorithm AlgorithmFromHandleNoDuplicate(CngKey key)
        {
            if (key.AlgorithmGroup != CngAlgorithmGroup.MLDsa)
            {
                throw new CryptographicException(SR.Cryptography_ArgMLDsaRequiresMLDsaKey);
            }

            Debug.Assert(key is not null);

            return AlgorithmFromHandleImpl(key);
        }

        private static MLDsaAlgorithm AlgorithmFromHandleImpl(CngKey key)
        {
            string? parameterSet =
                key.Handle.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);

            return parameterSet switch
            {
                NCRYPT_MLDSA_PARAMETER_SET_44 => MLDsaAlgorithm.MLDsa44,
                NCRYPT_MLDSA_PARAMETER_SET_65 => MLDsaAlgorithm.MLDsa65,
                NCRYPT_MLDSA_PARAMETER_SET_87 => MLDsaAlgorithm.MLDsa87,
                _ => throw DebugFailAndGetException(parameterSet),
            };

            static Exception DebugFailAndGetException(string? parameterSet)
            {
                Debug.Fail($"Unexpected parameter set {parameterSet}");
                throw new CryptographicException();
            }
        }

        public partial CngKey Key
        {
            get
            {
                ThrowIfDisposed();

                return _key;
            }
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(CngKeyBlobFormat.PQDsaPublicBlob, Algorithm.PublicKeySizeInBytes, destination);

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(_key);

            if (encryptedOnlyExport)
            {
                ExportKeyWithEncryptedOnlyExport(
                    static (ref readonly mldsaPrivateKeyAsn, algorithm, destination) =>
                    {
                        ReadOnlyMemory<byte>? seed = mldsaPrivateKeyAsn.Seed ?? mldsaPrivateKeyAsn.Both?.Seed;

                        if (seed is ReadOnlyMemory<byte> seedValue)
                        {
                            if (seedValue.Length != algorithm.PrivateSeedSizeInBytes)
                            {
                                throw new CryptographicException(SR.Argument_PrivateSeedWrongSizeForAlgorithm);
                            }

                            seedValue.Span.CopyTo(destination);
                            return;
                        }

                        throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                    },
                    Algorithm,
                    destination);
            }
            else
            {
                ExportKey(CngKeyBlobFormat.PQDsaPrivateSeedBlob, Algorithm.PrivateSeedSizeInBytes, destination);
            }
        }

        protected override void ExportMLDsaSecretKeyCore(Span<byte> destination)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(_key);

            if (encryptedOnlyExport)
            {
                ExportKeyWithEncryptedOnlyExport(static (ref readonly mldsaPrivateKeyAsn, algorithm, destination) =>
                {
                    ReadOnlyMemory<byte>? expandedKey = mldsaPrivateKeyAsn.ExpandedKey ?? mldsaPrivateKeyAsn.Both?.ExpandedKey;

                    if (expandedKey is ReadOnlyMemory<byte> expandedKeyValue)
                    {
                        if (expandedKeyValue.Length != algorithm.SecretKeySizeInBytes)
                        {
                            throw new CryptographicException(SR.Argument_SecretKeyWrongSizeForAlgorithm);
                        }

                        expandedKeyValue.Span.CopyTo(destination);
                        return;
                    }

                    // If PKCS#8 only has seed, then we can calculate the secret key
                    ReadOnlyMemory<byte>? seed = mldsaPrivateKeyAsn.Seed;

                    if (seed is ReadOnlyMemory<byte> seedValue)
                    {
                        if (seedValue.Length != algorithm.PrivateSeedSizeInBytes)
                        {
                            throw new CryptographicException(SR.Argument_PrivateSeedWrongSizeForAlgorithm);
                        }

                        using (MLDsa cloned = MLDsaImplementation.ImportSeed(algorithm, seedValue.Span))
                        {
                            cloned.ExportMLDsaSecretKey(destination);
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
                ExportKey(CngKeyBlobFormat.PQDsaPrivateBlob, Algorithm.SecretKeySizeInBytes, destination);
            }
        }

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
                    pkcs8.Array.CopyTo(destination);
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

        protected override unsafe void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                fixed (void* pContext = context)
                {
                    BCRYPT_PQDSA_PADDING_INFO paddingInfo = default;
                    paddingInfo.pbCtx = (IntPtr)pContext;
                    paddingInfo.cbCtx = context.Length;

                    duplicatedHandle.SignHash(
                        data,
                        destination,
                        Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PQDSA_FLAG,
                        &paddingInfo);
                }
            }
        }

        protected override unsafe bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
        {
            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                fixed (void* pContext = context)
                {
                    BCRYPT_PQDSA_PADDING_INFO paddingInfo = default;
                    paddingInfo.pbCtx = (IntPtr)pContext;
                    paddingInfo.cbCtx = context.Length;

                    return duplicatedHandle.VerifyHash(
                        data,
                        signature,
                        Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PQDSA_FLAG,
                        &paddingInfo);
                }
            }
        }

        internal static MLDsaCng ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
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
            ReadOnlySpan<byte> pkcs8Source = source.Slice(0, len);
            CngKey key;

            try
            {
                key = CngKey.Import(pkcs8Source, CngKeyBlobFormat.Pkcs8PrivateBlob);
            }
            catch (CryptographicException)
            {
                // TODO: Once Windows moves to new PKCS#8 format, we can remove this conversion.
                byte[] newPkcs8Source = MLDsaPkcs8.ConvertToOldChoicelessFormat(pkcs8Source);

                using (PinAndClear.Track(newPkcs8Source))
                {
                    key = CngKey.Import(newPkcs8Source, CngKeyBlobFormat.Pkcs8PrivateBlob);
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            key.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
            return new MLDsaCng(key, transferOwnership: true);
        }

        protected override void Dispose(bool disposing)
        {
            _key.Dispose();
            _key = null!;
        }

        private void ExportKey(
            CngKeyBlobFormat blobFormat,
            int expectedKeySize,
            Span<byte> destination)
        {
            byte[] blob = _key.Export(blobFormat);

            using (PinAndClear.Track(blob))
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(blob, out ReadOnlySpan<char> parameterSet, out string blobType);

                string expectedParameterSet = PqcBlobHelpers.GetMLDsaParameterSet(Algorithm);

                if (blobType != blobFormat.Format ||
                    keyBytes.Length != expectedKeySize ||
                    !parameterSet.SequenceEqual(expectedParameterSet))
                {
                    Debug.Fail(
                        $"{nameof(blobType)}: {blobType}, " +
                        $"{nameof(parameterSet)}: {parameterSet}, " +
                        $"{nameof(keyBytes)}.Length: {keyBytes.Length} / {expectedKeySize}");

                    throw new CryptographicException();
                }

                keyBytes.CopyTo(destination);
            }
        }

        private delegate void KeySelectorFunc(
            ref readonly MLDsaPrivateKeyAsn mldsaPrivateKeyAsn,
            MLDsaAlgorithm algorithm,
            Span<byte> destination);

        private void ExportKeyWithEncryptedOnlyExport(KeySelectorFunc keySelector, MLDsaAlgorithm algorithm, Span<byte> destination)
        {
            ArraySegment<byte> pkcs8 = GetRentedPkcs8ForEncryptedOnlyExport();
            byte[]? newPkcs8 = null;

            try
            {
                ReadOnlyMemory<byte> privateKey = KeyFormatHelper.ReadPkcs8(KnownOids, pkcs8.AsMemory(), out _);
                MLDsaPrivateKeyAsn mldsaPrivateKeyAsn;

                try
                {
                    mldsaPrivateKeyAsn = MLDsaPrivateKeyAsn.Decode(privateKey, AsnEncodingRules.BER);
                }
                catch (CryptographicException)
                {
                    // TODO: Once Windows moves to new PKCS#8 format, we can remove this conversion.
                    newPkcs8 = MLDsaPkcs8.ConvertFromOldChoicelessFormat(pkcs8);
                    ReadOnlyMemory<byte> newPrivateKey = KeyFormatHelper.ReadPkcs8(KnownOids, newPkcs8, out _);
                    mldsaPrivateKeyAsn = MLDsaPrivateKeyAsn.Decode(newPrivateKey, AsnEncodingRules.BER);
                }
                catch (AsnContentException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                keySelector(ref mldsaPrivateKeyAsn, algorithm, destination);
            }
            finally
            {
                if (newPkcs8 is not null)
                {
                    Array.Clear(newPkcs8);
                }

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
