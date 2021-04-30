// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertLoader : ILoaderPal
        {
            private readonly SafeCFArrayHandle _collectionHandle;

            public AppleCertLoader(SafeCFArrayHandle collectionHandle)
            {
                _collectionHandle = collectionHandle;
            }

            public void Dispose()
            {
                _collectionHandle.Dispose();
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                long longCount = Interop.CoreFoundation.CFArrayGetCount(_collectionHandle);

                if (longCount > int.MaxValue)
                    throw new CryptographicException();

                int count = (int)longCount;

                // Apple returns things in the opposite order from Windows, so read backwards.
                for (int i = count - 1; i >= 0; i--)
                {
                    IntPtr handle = Interop.CoreFoundation.CFArrayGetValueAtIndex(_collectionHandle, i);

                    if (handle != IntPtr.Zero)
                    {
                        ICertificatePal? certPal = CertificatePal.FromHandle(handle, throwOnFail: false);

                        if (certPal != null)
                        {
                            X509Certificate2 cert = new X509Certificate2(certPal);
                            collection.Add(cert);
                        }
                    }
                }
            }
        }

        private sealed class ApplePemCertLoader : ILoaderPal
        {
            private readonly List<ICertificatePal> _collection;

            public ApplePemCertLoader(List<ICertificatePal> collection)
            {
                _collection = collection;
            }

            public void Dispose()
            {
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                foreach (ICertificatePal pal in _collection)
                {
                    X509Certificate2 cert = new X509Certificate2(pal);
                    collection.Add(cert);
                }
            }
        }

        private sealed class ApplePkcs12CertLoader : ILoaderPal
        {
            private readonly ApplePkcs12Reader _pkcs12;
            private SafePasswordHandle _password;

            public ApplePkcs12CertLoader(
                ApplePkcs12Reader pkcs12,
                SafePasswordHandle password)
            {
                _pkcs12 = pkcs12;

                bool addedRef = false;
                password.DangerousAddRef(ref addedRef);
                _password = password;
            }

            public void Dispose()
            {
                _pkcs12.Dispose();

                SafePasswordHandle? password = Interlocked.Exchange(ref _password, null!);
                password?.DangerousRelease();
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                foreach (UnixPkcs12Reader.CertAndKey certAndKey in _pkcs12.EnumerateAll())
                {
                    AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;
                    collection.Add(new X509Certificate2(AppleCertificatePal.ImportPkcs12(certAndKey)));
                }
            }
        }
    }
}
