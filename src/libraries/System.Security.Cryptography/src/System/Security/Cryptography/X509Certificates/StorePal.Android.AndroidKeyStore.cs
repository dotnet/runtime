// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
        private sealed class AndroidKeyStore : IStorePal
        {
            private readonly bool _readOnly;
            private readonly SafeX509StoreHandle _keyStoreHandle;

            public static AndroidKeyStore OpenDefault(OpenFlags openFlags)
            {
                SafeX509StoreHandle store = Interop.AndroidCrypto.X509StoreOpenDefault();
                if (store.IsInvalid)
                {
                    store.Dispose();
                    throw new CryptographicException();
                }

                return new AndroidKeyStore(store, openFlags);
            }

            private AndroidKeyStore(SafeX509StoreHandle keyStoreHandle, OpenFlags openFlags)
            {
                _keyStoreHandle = keyStoreHandle;
                _readOnly = (openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed)) == 0;
            }

            public SafeHandle SafeHandle => _keyStoreHandle;

            public void Dispose()
            {
                _keyStoreHandle.Dispose();
            }

            public void Add(ICertificatePal cert)
            {
                if (_readOnly)
                    throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);

                AndroidCertificatePal certPal = (AndroidCertificatePal)cert;
                string hashString = GetCertificateHashString(cert);

                bool success;
                if (certPal.HasPrivateKey)
                {
                    Interop.AndroidCrypto.PAL_KeyAlgorithm algorithm = certPal.PrivateKeyHandle switch
                    {
                        // The AndroidKeyStore doesn't support adding DSA private key entries in newer versions (API 23+)
                        // Our minimum supported version (API 21) does support it, but for simplicity, we simply block adding
                        // certificates with DSA private keys on all versions instead of trying to support it on two versions.
                        SafeDsaHandle => throw new PlatformNotSupportedException(SR.Cryptography_X509_StoreDSAPrivateKeyNotSupported),
                        SafeEcKeyHandle => Interop.AndroidCrypto.PAL_KeyAlgorithm.EC,
                        SafeRsaHandle => Interop.AndroidCrypto.PAL_KeyAlgorithm.RSA,
                        _ => throw new NotSupportedException(SR.NotSupported_KeyAlgorithm)
                    };

                    success = Interop.AndroidCrypto.X509StoreAddCertificateWithPrivateKey(_keyStoreHandle, certPal.SafeHandle, certPal.PrivateKeyHandle, algorithm, hashString);
                }
                else
                {
                    success = Interop.AndroidCrypto.X509StoreAddCertificate(_keyStoreHandle, certPal.SafeHandle, hashString);
                }

                if (!success)
                    throw new CryptographicException(SR.Cryptography_X509_StoreAddFailure);
            }

            public void Remove(ICertificatePal cert)
            {
                string hashString = GetCertificateHashString(cert);
                AndroidCertificatePal certPal = (AndroidCertificatePal)cert;
                if (_readOnly)
                {
                    bool containsCert = Interop.AndroidCrypto.X509StoreContainsCertificate(_keyStoreHandle, certPal.SafeHandle, hashString);
                    if (containsCert)
                        throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);

                    // Removing a non-existent certificate is not an error
                    return;
                }

                bool success = Interop.AndroidCrypto.X509StoreRemoveCertificate(_keyStoreHandle, certPal.SafeHandle, hashString);
                if (!success)
                    throw new CryptographicException(SR.Cryptography_X509_StoreRemoveFailure);
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                EnumCertificatesContext context = default;
                context.Results = new HashSet<X509Certificate2>();
                unsafe
                {
                    bool success = Interop.AndroidCrypto.X509StoreEnumerateCertificates(
                        _keyStoreHandle,
                        &EnumCertificatesCallback,
                        Unsafe.AsPointer(ref context));
                    if (!success)
                    {
                        throw new CryptographicException(SR.Cryptography_X509_StoreEnumerateFailure);
                    }
                }

                foreach (X509Certificate2 cert in context.Results)
                {
                    collection.Add(cert);
                }
            }

            private static string GetCertificateHashString(ICertificatePal certPal)
            {
                return X509Certificate.GetCertHashString(HashAlgorithmName.SHA256, certPal);
            }

            private struct EnumCertificatesContext
            {
                public HashSet<X509Certificate2> Results;
            }

            [UnmanagedCallersOnly]
            private static unsafe void EnumCertificatesCallback(void* certPtr, void* privateKeyPtr, Interop.AndroidCrypto.PAL_KeyAlgorithm privateKeyAlgorithm, void* context)
            {
                ref EnumCertificatesContext callbackContext = ref Unsafe.As<byte, EnumCertificatesContext>(ref *(byte*)context);

                AndroidCertificatePal certPal;
                var handle = new SafeX509Handle((IntPtr)certPtr);
                if (privateKeyPtr != null)
                {
                    SafeKeyHandle privateKey = privateKeyAlgorithm switch
                    {
                        Interop.AndroidCrypto.PAL_KeyAlgorithm.DSA => new SafeDsaHandle((IntPtr)privateKeyPtr),
                        Interop.AndroidCrypto.PAL_KeyAlgorithm.EC => new SafeEcKeyHandle((IntPtr)privateKeyPtr),
                        Interop.AndroidCrypto.PAL_KeyAlgorithm.RSA => new SafeRsaHandle((IntPtr)privateKeyPtr),
                        _ => throw new NotSupportedException(SR.NotSupported_KeyAlgorithm)
                    };
                    certPal = new AndroidCertificatePal(handle, privateKey);
                }
                else
                {
                    certPal = new AndroidCertificatePal(handle);
                }

                var cert = new X509Certificate2(certPal);
                if (!callbackContext.Results.Add(cert))
                    cert.Dispose();
            }
        }
    }
}
