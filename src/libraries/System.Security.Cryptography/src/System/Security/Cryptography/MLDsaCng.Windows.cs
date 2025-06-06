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
                // TODO resx
                throw new ArgumentException();
            }

            MLDsaAlgorithm algorithm = AlgorithmFromHandleImpl(key);
            duplicateKey = CngAlgorithmCore.Duplicate(key);
            return algorithm;
        }

        private static MLDsaAlgorithm AlgorithmFromHandleNoDuplicate(CngKey key)
        {
            if (key.AlgorithmGroup != CngAlgorithmGroup.MLDsa)
            {
                // TODO resx
                throw new CryptographicException();
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
                // TODO resx
                _ => throw new CryptographicException(),
            };
        }

        public partial CngKey GetCngKey()
        {
            ThrowIfDisposed();

            // TODO Should this duplicate the key? Other algos don't seem to in their
            // Key property, but making this a method might imply to users that this
            // a new copy that we made for them
            return _key;
        }

        protected override void ExportMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(CngKeyBlobFormat.PQDsaPublicBlob, Algorithm.PublicKeySizeInBytes, destination);

        protected override void ExportMLDsaPrivateSeedCore(Span<byte> destination)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(_key);

            if (encryptedOnlyExport)
            {
                ExportKeyWithEncryptedOnlyExport(
                    static (ref readonly MLDsaPrivateKeyAsn asn) => asn.Seed ?? asn.Both?.Seed,
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
                // TODO avoid closure
                ExportKeyWithEncryptedOnlyExport((ref readonly MLDsaPrivateKeyAsn asn) =>
                {
                    ReadOnlyMemory<byte>? expandedKey = asn.ExpandedKey ?? asn.Both?.ExpandedKey;

                    if (expandedKey  is not null)
                    {
                        return expandedKey.GetValueOrDefault();
                    }

                    // If PKCS#8 only has seed, then we can calculate the secret key
                    ReadOnlyMemory<byte>? seed = asn.Seed ?? asn.Both?.Seed;

                    if (seed is not null)
                    {
                        using (MLDsa cloned = MLDsa.ImportMLDsaPrivateSeed(Algorithm, seed.GetValueOrDefault().Span))
                        {
                            // TODO stackalloc/rent
                            byte[] secretKey = new byte[Algorithm.SecretKeySizeInBytes];
                            cloned.ExportMLDsaSecretKey(secretKey);
                            return secretKey;
                        }
                    }

                    // Caller will throw, so just return null
                    return null;
                },
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
                const string TemporaryExportPassword = "DotnetExportPhrase";
                byte[] exported = _key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);

                ArraySegment<byte> pkcs8 = KeyFormatHelper.DecryptPkcs8(
                    TemporaryExportPassword,
                    exported,
                    out _);

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
                ReadOnlySpan<byte> newPkcs8Source = MLDsaPkcs8.ConvertToOldChoicelessFormat(pkcs8Source);
                key = CngKey.Import(newPkcs8Source, CngKeyBlobFormat.Pkcs8PrivateBlob);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            key.ExportPolicy |= CngExportPolicies.AllowPlaintextExport;
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
            ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeMLDsaBlob(blob, out ReadOnlySpan<char> parameterSet, out string blobType);
            string expectedParameterSet = PqcBlobHelpers.GetParameterSet(Algorithm);

            if (blobType != blobFormat.Format ||
                keyBytes.Length != expectedKeySize ||
                !parameterSet.SequenceEqual(expectedParameterSet))
            {
                // TODO resx
                throw new CryptographicException();
            }

            keyBytes.CopyTo(destination);
        }

        private delegate ReadOnlyMemory<byte>? KeySelectorFunc(ref readonly MLDsaPrivateKeyAsn asn);

        private void ExportKeyWithEncryptedOnlyExport(KeySelectorFunc keySelector, Span<byte> destination)
        {
            const string TemporaryExportPassword = "DotnetExportPhrase";
            byte[] exported = _key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);

            ArraySegment<byte> pkcs8 = KeyFormatHelper.DecryptPkcs8(
                TemporaryExportPassword,
                exported,
                out _);

            try
            {
                ReadOnlyMemory<byte> privateKey = KeyFormatHelper.ReadPkcs8(KnownOids, pkcs8.AsMemory(), out _);
                MLDsaPrivateKeyAsn asn;

                try
                {
                    asn = MLDsaPrivateKeyAsn.Decode(privateKey, AsnEncodingRules.BER);
                }
                catch
                {
                    // TODO: Once Windows moves to new PKCS#8 format, we can remove this conversion.
                    byte[] newPkcs8 = MLDsaPkcs8.ConvertFromOldChoicelessFormat(privateKey.Span);
                    ReadOnlyMemory<byte> newPrivateKey = KeyFormatHelper.ReadPkcs8(KnownOids, newPkcs8, out _);
                    asn = MLDsaPrivateKeyAsn.Decode(newPrivateKey, AsnEncodingRules.BER);
                }

                ReadOnlyMemory<byte> key = keySelector(ref asn) ?? throw new CryptographicException();

                if (destination.Length != key.Length)
                {
                    Debug.Fail("Caller should have provided destination with correct size.");
                    // TODO resx
                    throw new CryptographicException();
                }

                key.Span.CopyTo(destination);
            }
            finally
            {
                CryptoPool.Return(pkcs8);
            }
        }
    }
}
