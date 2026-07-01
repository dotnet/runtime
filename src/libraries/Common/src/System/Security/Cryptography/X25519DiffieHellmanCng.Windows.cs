// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    public sealed partial class X25519DiffieHellmanCng : X25519DiffieHellman
    {
        private CngKey _key;
        private static readonly string[] s_eccKeyOid = [Oids.EcPublicKey];

        public partial X25519DiffieHellmanCng(CngKey key)
        {
            if (!Helpers.IsOSPlatformWindows)
            {
                throw new PlatformNotSupportedException();
            }

            ArgumentNullException.ThrowIfNull(key);
            ThrowIfNotSupported();

            if (key.AlgorithmGroup != CngAlgorithmGroup.ECDiffieHellman || !IsX25519Key(key))
            {
                throw new ArgumentException(SR.Cryptography_ArgX25519RequiresX25519Key, nameof(key));
            }

            _key = key.Duplicate();
        }

        public partial CngKey GetKey()
        {
            ThrowIfDisposed();
            return _key.Duplicate();
        }

        protected override unsafe partial void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            // We intentionally don't special case otherParty being an instance of X25519DiffieHellmanCng and always
            // export the public key into the current instance's provider.
            Span<byte> publicKeyBytes = stackalloc byte[PublicKeySizeInBytes];
            otherParty.ExportPublicKey(publicKeyBytes);
            DeriveRawSecretAgreementWithPublicKey(publicKeyBytes, destination);
        }

        protected override partial void DeriveRawSecretAgreementCore(ReadOnlySpan<byte> otherPartyPublicKey, Span<byte> destination)
        {
            Debug.Assert(otherPartyPublicKey.Length == PublicKeySizeInBytes);
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            DeriveRawSecretAgreementWithPublicKey(otherPartyPublicKey, destination);
        }

        private void DeriveRawSecretAgreementWithPublicKey(ReadOnlySpan<byte> otherPartyPublicKey, Span<byte> destination)
        {
            scoped Span<byte> reducedPublicKey;

            unsafe
            {
                reducedPublicKey = stackalloc byte[PublicKeySizeInBytes];
            }

            X25519WindowsHelpers.ReducePublicKey(otherPartyPublicKey, reducedPublicKey);

            // CNG does not permit cross-provider key agreements. Import the public key in to the same provider
            // as the current key.
            CngProvider provider = _key.Provider ?? CngProvider.MicrosoftSoftwareKeyStorageProvider;

            using (CryptoPoolLease lease = X25519WindowsHelpers.CreateCngBlob(reducedPublicKey, privateKey: false, out _))
            using (SafeNCryptProviderHandle providerHandle = provider.OpenStorageProvider())
            {
                int flags = 0;

                if (provider == CngProvider.MicrosoftSoftwareKeyStorageProvider)
                {
                    const int NCRYPT_NO_KEY_VALIDATION = (int)Interop.BCrypt.BCryptImportKeyPairFlags.BCRYPT_NO_KEY_VALIDATION;
                    flags |= NCRYPT_NO_KEY_VALIDATION;
                }

                SafeNCryptKeyHandle keyHandle = ECCng.ImportKeyBlob(
                    CngKeyBlobFormat.EccPublicBlob.Format,
                    lease.Span,
                    X25519WindowsHelpers.BCRYPT_ECC_CURVE_25519,
                    providerHandle,
                    flags);

                using (keyHandle)
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                using (SafeNCryptSecretHandle secretAgreement = Interop.NCrypt.DeriveSecretAgreement(
                    _key.HandleNoDuplicate,
                    keyHandle))
#else
                using (SafeNCryptKeyHandle currentKeyHandle = _key.Handle)
                using (SafeNCryptSecretHandle secretAgreement = Interop.NCrypt.DeriveSecretAgreement(
                    currentKeyHandle,
                    keyHandle))
#endif
                {
                    bool success = Interop.NCrypt.TryDeriveKeyMaterialTruncate(
                        secretAgreement,
                        Interop.NCrypt.SecretAgreementFlags.None,
                        destination,
                        out int bytesWritten);

                    if (!success || bytesWritten != SecretAgreementSizeInBytes)
                    {
                        // The destination should have already been pre-sized to a well-behaving X25519 implementation
                        // but a provider could be implemented incorrectly. Zero whatever was written since it is
                        // incorrect.
                        CryptographicOperations.ZeroMemory(destination);
                        throw new CryptographicException();
                    }

                    // If the CngKey was created with NCRYPT_NO_KEY_VALIDATION then low-order public keys can be imported.
                    // Block low-order key agreements that result in an all-zero secret.
                    if (CryptographicOperations.FixedTimeEquals(destination, 0))
                    {
                        throw new CryptographicException();
                    }
                }
            }
        }

        protected override partial void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);
            ExportKeyFromBlob(_key, privateKey: false, destination);
        }

        protected override partial void ExportPrivateKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);

            if (CngPkcs8.AllowsOnlyEncryptedExport(_key))
            {
                ExportPrivateKeyFromEncryptedPkcs8(destination);
            }
            else
            {
                ExportKeyFromBlob(_key, privateKey: true, destination);
            }
        }

        protected override partial bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // This will use ExportPrivateKeyCore which, in turn, will handle encrypted-only exports
            // so we don't handle it here. We cannot use the PKCS#8 that CNG gives us - it does not understand
            // RFC 8410 OIDs so X25519 keys are exported with explicit parameters. Since the PKCS#8 would need to be
            // re-assembled anyway, let it use the existing exporter instead.
            return TryExportPkcs8PrivateKeyImpl(destination, out bytesWritten);
        }

        protected override partial void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key?.Dispose();
                _key = null!;
            }

            base.Dispose(disposing);
        }

        private static void ExportKeyFromBlob(CngKey key, bool privateKey, Span<byte> destination)
        {
            int numBytesNeeded;
            string format = privateKey ?
                CngKeyBlobFormat.EccPrivateBlob.Format :
                CngKeyBlobFormat.EccPublicBlob.Format;

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            SafeNCryptKeyHandle keyHandle = key.HandleNoDuplicate;
#else
            using SafeNCryptKeyHandle keyHandle = key.Handle;
#endif

            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                format,
                IntPtr.Zero,
                null,
                0,
                out numBytesNeeded,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            using (CryptoPoolLease lease = CryptoPoolLease.Rent(numBytesNeeded, skipClear: !privateKey))
            {
                errorCode = Interop.NCrypt.NCryptExportKey(
                    keyHandle,
                    IntPtr.Zero,
                    format,
                    IntPtr.Zero,
                    lease.Span,
                    lease.Span.Length,
                    out numBytesNeeded,
                    0);

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }

                X25519WindowsHelpers.ExportKey(lease.Span.Slice(0, numBytesNeeded), privateKey, destination);
            }
        }

        private static bool IsX25519Key(CngKey key)
        {
#if SYSTEM_SECURITY_CRYPTOGRAPHY
            return key.GetCurveName(out _) == X25519WindowsHelpers.BCRYPT_ECC_CURVE_25519;
#else
            return key.GetPropertyAsString(KeyPropertyName.ECCCurveName) == X25519WindowsHelpers.BCRYPT_ECC_CURVE_25519;
#endif
        }

        private void ExportPrivateKeyFromEncryptedPkcs8(Span<byte> destination)
        {
            const string TemporaryExportPassword = "DotnetExportPhrase";
            byte[] exported = _key.ExportPkcs8KeyBlob(TemporaryExportPassword, 1);
            byte[] privateKey = new byte[PrivateKeySizeInBytes];

            using (PinAndClear.Track(exported))
            using (PinAndClear.Track(privateKey))
            {
                KeyFormatHelper.ReadEncryptedPkcs8(
                    s_eccKeyOid,
                    exported,
                    TemporaryExportPassword,
                    privateKey,
                    static (ReadOnlySpan<byte> key, byte[] privateKey, in ValueAlgorithmIdentifierAsn algId, out object? ret) =>
                    {
                        if (algId.Algorithm != Oids.EcPublicKey)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                        }

                        // Windows currently exports X25519 keys as an explicit curve. However
                        // since the constructor validates that the CngKey curve is curve25519 we can be reasonably sure
                        // that the key is for X25519, so we don't validate the parameters.
                        ValueECPrivateKey.Decode(key, AsnEncodingRules.BER, out ValueECPrivateKey ecKey);

                        if (ecKey.PrivateKey.Length != PrivateKeySizeInBytes)
                        {
                            throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                        }

                        ecKey.PrivateKey.CopyTo(privateKey);
                        ret = (object?)null;
                    },
                    out _,
                    out _);

                privateKey.CopyTo(destination);
            }
        }
    }
}
