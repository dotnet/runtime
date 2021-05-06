// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        private SafeKeychainHandle? _tempKeychain;

        public static ICertificatePal FromBlob(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

            X509ContentType contentType = X509Certificate2.GetCertContentType(rawData);

            if (contentType == X509ContentType.Pkcs7)
            {
                // In single mode for a PKCS#7 signed or signed-and-enveloped file we're supposed to return
                // the certificate which signed the PKCS#7 file.
                //
                // X509Certificate2Collection::Export(X509ContentType.Pkcs7) claims to be a signed PKCS#7,
                // but doesn't emit a signature block. So this is hard to test.
                //
                // TODO(2910): Figure out how to extract the signing certificate, when it's present.
                throw new CryptographicException(SR.Cryptography_X509_PKCS7_NoSigner);
            }

            if (contentType == X509ContentType.Pkcs12)
            {
                if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                {
                    throw new PlatformNotSupportedException(SR.Cryptography_X509_NoEphemeralPfx);
                }

                bool exportable = (keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable;

                bool persist =
                    (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet;

                SafeKeychainHandle keychain = persist
                    ? Interop.AppleCrypto.SecKeychainCopyDefault()
                    : Interop.AppleCrypto.CreateTemporaryKeychain();

                using (keychain)
                {
                    AppleCertificatePal ret = ImportPkcs12(rawData, password, exportable, keychain);
                    if (!persist)
                    {
                        // If we used temporary keychain we need to prevent deletion.
                        // on 10.15+ if keychain is unlinked, certain certificate operations may fail.
                        bool success = false;
                        keychain.DangerousAddRef(ref success);
                        if (success)
                        {
                            ret._tempKeychain = keychain;
                        }
                    }

                    return ret;
                }
            }

            SafeSecIdentityHandle identityHandle;
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                rawData,
                contentType,
                SafePasswordHandle.InvalidHandle,
                SafeTemporaryKeychainHandle.InvalidHandle,
                exportable: true,
                out identityHandle);

            if (identityHandle.IsInvalid)
            {
                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            Debug.Fail("Non-PKCS12 import produced an identity handle");

            identityHandle.Dispose();
            certHandle.Dispose();
            throw new CryptographicException();
        }

        public void DisposeTempKeychain()
        {
            SafeKeychainHandle? tempKeychain = Interlocked.Exchange(ref _tempKeychain, null);
            if (tempKeychain != null)
            {
                tempKeychain.Dispose();
            }
        }

        internal unsafe byte[] ExportPkcs8(ReadOnlySpan<char> password)
        {
            Debug.Assert(_identityHandle != null);

            using (SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPrivateKeyFromIdentity(_identityHandle))
            {
                return ExportPkcs8(key, password);
            }
        }

        internal static unsafe byte[] ExportPkcs8(SafeSecKeyRefHandle key, ReadOnlySpan<char> password)
        {
            using (SafeCFDataHandle data = Interop.AppleCrypto.SecKeyExportData(key, exportPrivate: true, password))
            {
                ReadOnlySpan<byte> systemExport = Interop.CoreFoundation.CFDataDangerousGetSpan(data);

                fixed (byte* ptr = systemExport)
                {
                    using (PointerMemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, systemExport.Length))
                    {
                        // Apple's PKCS8 export exports using PBES2, which Win7, Win8.1, and Apple all fail to
                        // understand in their PKCS12 readers, so re-encrypt using the Win7 PKCS12-PBE parameters.
                        //
                        // Since Apple only reliably exports keys with encrypted PKCS#8 there's not a
                        // "so export it plaintext and only encrypt it once" option.
                        AsnWriter writer = KeyFormatHelper.ReencryptPkcs8(
                            password,
                            manager.Memory,
                            password,
                            UnixExportProvider.s_windowsPbe);

                        return writer.Encode();
                    }
                }
            }
        }

        public DSA? GetDSAPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            Debug.Assert(!_identityHandle.IsInvalid);
            SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.X509GetPublicKey(_certHandle);
            SafeSecKeyRefHandle privateKey = Interop.AppleCrypto.X509GetPrivateKeyFromIdentity(_identityHandle);

            if (publicKey.IsInvalid)
            {
                // SecCertificateCopyKey returns null for DSA, so fall back to manually building it.
                publicKey = Interop.AppleCrypto.ImportEphemeralKey(_certData.SubjectPublicKeyInfo, false);
            }

            return new DSAImplementation.DSASecurityTransforms(publicKey, privateKey);
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            var typedKey = privateKey as DSAImplementation.DSASecurityTransforms;

            if (typedKey != null)
            {
                return CopyWithPrivateKey(typedKey.GetKeys().PrivateKey);
            }

            DSAParameters dsaParameters = privateKey.ExportParameters(true);

            using (PinAndClear.Track(dsaParameters.X!))
            using (typedKey = new DSAImplementation.DSASecurityTransforms())
            {
                typedKey.ImportParameters(dsaParameters);
                return CopyWithPrivateKey(typedKey.GetKeys().PrivateKey);
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            var typedKey = privateKey as ECDsaImplementation.ECDsaSecurityTransforms;

            if (typedKey != null)
            {
                return CopyWithPrivateKey(typedKey.GetKeys().PrivateKey);
            }

            byte[] ecPrivateKey = privateKey.ExportECPrivateKey();

            using (PinAndClear.Track(ecPrivateKey))
            using (SafeSecKeyRefHandle privateSecKey = Interop.AppleCrypto.ImportEphemeralKey(ecPrivateKey, true))
            {
                return CopyWithPrivateKey(privateSecKey);
            }
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            var typedKey = privateKey as ECDiffieHellmanImplementation.ECDiffieHellmanSecurityTransforms;

            if (typedKey != null)
            {
                return CopyWithPrivateKey(typedKey.GetKeys().PrivateKey);
            }

            byte[] ecPrivateKey = privateKey.ExportECPrivateKey();

            using (PinAndClear.Track(ecPrivateKey))
            using (SafeSecKeyRefHandle privateSecKey = Interop.AppleCrypto.ImportEphemeralKey(ecPrivateKey, true))
            {
                return CopyWithPrivateKey(privateSecKey);
            }
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            var typedKey = privateKey as RSAImplementation.RSASecurityTransforms;

            if (typedKey != null)
            {
                return CopyWithPrivateKey(typedKey.GetKeys().PrivateKey);
            }

            byte[] rsaPrivateKey = privateKey.ExportRSAPrivateKey();

            using (PinAndClear.Track(rsaPrivateKey))
            using (SafeSecKeyRefHandle privateSecKey = Interop.AppleCrypto.ImportEphemeralKey(rsaPrivateKey, true))
            {
                return CopyWithPrivateKey(privateSecKey);
            }
        }

        internal AppleCertificatePal? MoveToKeychain(SafeKeychainHandle keychain, SafeSecKeyRefHandle? privateKey)
        {
            SafeSecIdentityHandle? identity = Interop.AppleCrypto.X509MoveToKeychain(
                _certHandle,
                keychain,
                privateKey);

            if (identity != null)
            {
                return new AppleCertificatePal(identity);
            }

            return null;
        }

        private ICertificatePal CopyWithPrivateKey(SafeSecKeyRefHandle? privateKey)
        {
            if (privateKey == null)
            {
                // Both Windows and Linux/OpenSSL are unaware if they bound a public or private key.
                // Here, we do know.  So throw if we can't do what they asked.
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
            }

            SafeKeychainHandle keychain = Interop.AppleCrypto.SecKeychainItemCopyKeychain(privateKey);

            // If we're using a key already in a keychain don't add the certificate to that keychain here,
            // do it in the temporary add/remove in the shim.
            SafeKeychainHandle cloneKeychain = SafeTemporaryKeychainHandle.InvalidHandle;

            if (keychain.IsInvalid)
            {
                keychain = Interop.AppleCrypto.CreateTemporaryKeychain();
                cloneKeychain = keychain;
            }

            // Because SecIdentityRef only has private constructors we need to have the cert and the key
            // in the same keychain.  That almost certainly means we're going to need to add this cert to a
            // keychain, and when a cert that isn't part of a keychain gets added to a keychain then the
            // interior pointer of "what keychain did I come from?" used by SecKeychainItemCopyKeychain gets
            // set. That makes this function have side effects, which is not desired.
            //
            // It also makes reference tracking on temporary keychains broken, since the cert can
            // DangerousRelease a handle it didn't DangerousAddRef on.  And so CopyWithPrivateKey makes
            // a temporary keychain, then deletes it before anyone has a chance to (e.g.) export the
            // new identity as a PKCS#12 blob.
            //
            // Solution: Clone the cert, like we do in Windows.
            SafeSecCertificateHandle tempHandle;

            {
                byte[] export = RawData;
                const bool exportable = false;
                SafeSecIdentityHandle identityHandle;
                tempHandle = Interop.AppleCrypto.X509ImportCertificate(
                    export,
                    X509ContentType.Cert,
                    SafePasswordHandle.InvalidHandle,
                    cloneKeychain,
                    exportable,
                    out identityHandle);

                Debug.Assert(identityHandle.IsInvalid, "identityHandle should be IsInvalid");
                identityHandle.Dispose();

                Debug.Assert(!tempHandle.IsInvalid, "tempHandle should not be IsInvalid");
            }

            using (keychain)
            using (tempHandle)
            {
                SafeSecIdentityHandle identityHandle = Interop.AppleCrypto.X509CopyWithPrivateKey(
                    tempHandle,
                    privateKey,
                    keychain);

                AppleCertificatePal newPal = new AppleCertificatePal(identityHandle);
                return newPal;
            }
        }
    }
}
