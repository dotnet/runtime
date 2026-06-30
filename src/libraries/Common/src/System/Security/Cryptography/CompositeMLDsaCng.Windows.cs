// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;
using static Interop.BCrypt;

namespace System.Security.Cryptography
{
    public sealed partial class CompositeMLDsaCng : CompositeMLDsa
    {
        internal const string NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_44_ECDSA_P256_SHA256 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_44_ECDSA_P256_SHA256;
        internal const string NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P256_SHA512 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P256_SHA512;
        internal const string NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P384_SHA512 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P384_SHA512;
        internal const string NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_87_ECDSA_P384_SHA512 = PqcBlobHelpers.BCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_87_ECDSA_P384_SHA512;

        [SupportedOSPlatform("windows")]
        private static partial CompositeMLDsaAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey)
        {
            ArgumentNullException.ThrowIfNull(key);
            ThrowIfNotSupported();

            if (key.AlgorithmGroup != CngAlgorithmGroup.CompositeMLDsa)
            {
                throw new ArgumentException(SR.Cryptography_ArgCompositeMLDsaRequiresCompositeMLDsaKey, nameof(key));
            }

            CompositeMLDsaAlgorithm algorithm = AlgorithmFromHandleImpl(key);

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            duplicateKey = CngHelpers.Duplicate(key.HandleNoDuplicate, key.IsEphemeral);
#else
            duplicateKey = key.Duplicate();
#endif

            return algorithm;
        }

        private static CompositeMLDsaAlgorithm AlgorithmFromHandleImpl(CngKey key)
        {
            string? parameterSet =
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                key.HandleNoDuplicate.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#else
                key.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#endif

            return parameterSet switch
            {
                NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_44_ECDSA_P256_SHA256 => CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,
                NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P256_SHA512 => CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,
                NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_65_ECDSA_P384_SHA512 => CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,
                NCRYPT_COMPOSITE_MLDSA_PARAMETER_SET_87_ECDSA_P384_SHA512 => CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,
                _ => throw DebugFailAndGetException(parameterSet),
            };

            static Exception DebugFailAndGetException(string? parameterSet)
            {
                Debug.Fail($"Unexpected parameter set {parameterSet}");
                throw new CryptographicException();
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
        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            int bytesWritten = 0;

            using (SafeNCryptKeyHandle duplicatedHandle = _key.Handle)
            {
                fixed (void* pContext = context)
                {
                    BCRYPT_PQDSA_PADDING_INFO paddingInfo = default;
                    paddingInfo.pbCtx = (IntPtr)pContext;
                    paddingInfo.cbCtx = context.Length;

                    bool result = duplicatedHandle.TrySignHash(
                        data,
                        destination,
                        Interop.NCrypt.AsymmetricPaddingMode.NCRYPT_PAD_PQDSA_FLAG,
                        &paddingInfo,
                        out bytesWritten);

                    if (!result)
                    {
                        Debug.Fail("Buffer too small but caller should have already validated the buffer size.");
                        throw new CryptographicException();
                    }
                }
            }

            return bytesWritten;
        }

        /// <inheritdoc/>
        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature)
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

        /// <inheritdoc/>
        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(CngKeyBlobFormat.PQDsaPublicBlob, Algorithm.MaxPublicKeySizeInBytes, destination);

        /// <inheritdoc/>
        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination) =>
            CngPkcs8.AllowsOnlyEncryptedExport(_key)
            ? throw new CryptographicException(SR.Cryptography_KeyNotExtractable)
            : ExportKey(CngKeyBlobFormat.PQDsaPrivateBlob, Algorithm.MaxPrivateKeySizeInBytes, destination);

        /// <inheritdoc/>
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            bool encryptedOnlyExport = CngPkcs8.AllowsOnlyEncryptedExport(_key);

            // Windows NCrypt does not yet support PKCS#8 export for Composite ML-DSA.
            if (encryptedOnlyExport)
            {
                throw new CryptographicException(SR.Cryptography_KeyNotExtractable);
            }

            return TryExportPkcs8FromExportedPrivateKey(destination, out bytesWritten);
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

        private int ExportKey(CngKeyBlobFormat blobFormat, int maxKeySize, Span<byte> destination)
        {
            byte[] blob = _key.Export(blobFormat);

            using (PinAndClear.Track(blob))
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeCompositeMLDsaBlob(blob, out ReadOnlySpan<char> parameterSet, out string blobType);

                if (!PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(Algorithm, out string? expectedParameterSet))
                {
                    Debug.Fail($"Unknown algorithm {Algorithm.Name}.");
                    throw new CryptographicException();
                }

                if (blobType != blobFormat.Format ||
                    keyBytes.Length > maxKeySize ||
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
    }
}
