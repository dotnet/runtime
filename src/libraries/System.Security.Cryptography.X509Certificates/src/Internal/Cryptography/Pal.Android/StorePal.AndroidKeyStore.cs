// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
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
                    throw new CryptographicException();

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

                // TODO: [AndroidCrypto] Handle certs with private key
                if (certPal.HasPrivateKey)
                    throw new NotImplementedException($"{nameof(Add)} [Private Key]");

                bool success = Interop.AndroidCrypto.X509StoreAddCertificate(_keyStoreHandle, certPal.SafeHandle, certPal.Thumbprint.ToHexStringUpper());
                if (!success)
                    throw new CryptographicException(SR.Cryptography_X509_StoreAddFailure);
            }

            public void Remove(ICertificatePal cert)
            {
                AndroidCertificatePal certPal = (AndroidCertificatePal)cert;
                if (_readOnly)
                {
                    bool containsCert = Interop.AndroidCrypto.X509StoreContainsCertificate(_keyStoreHandle, certPal.Thumbprint.ToHexStringUpper());
                    if (containsCert)
                        throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);

                    // Removing a non-existent certificate is not an error
                    return;
                }

                bool success = Interop.AndroidCrypto.X509StoreRemoveCertificate(_keyStoreHandle, certPal.Thumbprint.ToHexStringUpper());
                if (!success)
                    throw new CryptographicException(SR.Cryptography_X509_StoreRemoveFailure);
            }

            public void CloneTo(X509Certificate2Collection collection)
            {
                EnumCertificatesContext context = default;
                context.Results = new HashSet<X509Certificate2>();
                unsafe
                {
                    Interop.AndroidCrypto.X509StoreEnumerateCertificates(
                        _keyStoreHandle,
                        &EnumCertificatesCallback,
                        Unsafe.AsPointer(ref context));
                }

                foreach (X509Certificate2 cert in context.Results)
                {
                    collection.Add(cert);
                }
            }

            private struct EnumCertificatesContext
            {
                public HashSet<X509Certificate2> Results;
            }

            [UnmanagedCallersOnly]
            private static unsafe void EnumCertificatesCallback(void* certPtr, void* context)
            {
                ref EnumCertificatesContext callbackContext = ref Unsafe.As<byte, EnumCertificatesContext>(ref *(byte*)context);
                var handle = new SafeX509Handle((IntPtr)certPtr);
                var cert = new X509Certificate2(new AndroidCertificatePal(handle));
                if (!callbackContext.Results.Add(cert))
                    cert.Dispose();
            }
        }
    }
}
