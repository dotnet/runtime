// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed partial class CompositeMLDsaImplementation : CompositeMLDsa
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

        private readonly bool _hasPrivateKey;
        private SafeBCryptKeyHandle _key;

        private CompositeMLDsaImplementation(
            CompositeMLDsaAlgorithm algorithm,
            SafeBCryptKeyHandle key,
            bool hasPrivateKey)
            : base(algorithm)
        {
            _key = key;
            _hasPrivateKey = hasPrivateKey;
        }

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool SupportsAny() => s_algHandle is not null;

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static partial bool IsAlgorithmSupportedImpl(CompositeMLDsaAlgorithm algorithm) =>
            SupportsAny() && PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out _);

        protected override int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return Interop.BCrypt.BCryptSignHashPqcPure(_key, data, context, destination);
        }

        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            Interop.BCrypt.BCryptVerifySignaturePqcPure(_key, data, context, signature);

        internal static partial CompositeMLDsa GenerateKeyImpl(CompositeMLDsaAlgorithm algorithm)
        {
            Debug.Assert(SupportsAny());

            if (!PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out string? parameterSet))
            {
                Debug.Fail("Base class should have validated algorithm support.");
                throw new CryptographicException();
            }

            SafeBCryptKeyHandle keyHandle = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, keyLength: 0);

            try
            {
                Interop.BCrypt.BCryptSetSZProperty(keyHandle, Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME, parameterSet);
                Interop.BCrypt.BCryptFinalizeKeyPair(keyHandle);
            }
            catch
            {
                keyHandle?.Dispose();
                throw;
            }

            return new CompositeMLDsaImplementation(algorithm, keyHandle, hasPrivateKey: true);
        }

        internal static partial CompositeMLDsa ImportCompositeMLDsaPublicKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            if (!PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out string? parameterSet))
            {
                Debug.Fail("Base class should have validated algorithm support.");
                throw new CryptographicException();
            }

            const string PublicBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeCompositeMLDsaBlob(
                    parameterSet,
                    source,
                    PublicBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PublicBlobType, blob));

            return new CompositeMLDsaImplementation(algorithm, key, hasPrivateKey: false);
        }

        internal static partial CompositeMLDsa ImportCompositeMLDsaPrivateKeyImpl(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            Debug.Assert(SupportsAny());

            if (!PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out string? parameterSet))
            {
                Debug.Fail("Base class should have validated algorithm support.");
                throw new CryptographicException();
            }

            const string PrivateBlobType = Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB;

            SafeBCryptKeyHandle key =
                PqcBlobHelpers.EncodeCompositeMLDsaBlob(
                    parameterSet,
                    source,
                    PrivateBlobType,
                    static blob => Interop.BCrypt.BCryptImportKeyPair(s_algHandle, PrivateBlobType, blob));

            return new CompositeMLDsaImplementation(algorithm, key, hasPrivateKey: true);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return TryExportPkcs8FromExportedPrivateKey(destination, out bytesWritten);
        }

        protected override int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination) =>
            ExportKey(Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB, destination);

        protected override int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination)
        {
            if (!_hasPrivateKey)
            {
                throw new CryptographicException(SR.Cryptography_NoPrivateKeyAvailable);
            }

            return ExportKey(Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB, destination);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private int ExportKey(string keyBlobType, Span<byte> destination)
        {
            ArraySegment<byte> keyBlob = Interop.BCrypt.BCryptExportKey(_key, keyBlobType);

            try
            {
                ReadOnlySpan<byte> keyBytes = PqcBlobHelpers.DecodeCompositeMLDsaBlob(
                    keyBlob,
                    out ReadOnlySpan<char> parameterSet,
                    out string blobType);

                if (!PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(Algorithm, out string? expectedParameterSet))
                {
                    Debug.Fail("Unsupported algorithm.");
                    throw new CryptographicException();
                }

                if (blobType != keyBlobType ||
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
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            if (!Helpers.IsOSPlatformWindows)
            {
                return null;
            }

            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.CompositeMLDsa,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }
            else
            {
                return hAlgorithm;
            }
        }
    }
}
