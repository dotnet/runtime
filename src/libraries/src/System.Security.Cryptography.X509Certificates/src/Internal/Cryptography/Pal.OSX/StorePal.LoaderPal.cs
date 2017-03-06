// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertLoader : ILoaderPal
        {
            private readonly SafeCFArrayHandle _collectionHandle;
            private readonly SafeTemporaryKeychainHandle _tmpKeychain;

            public AppleCertLoader(SafeCFArrayHandle collectionHandle, SafeTemporaryKeychainHandle tmpKeychain)
            {
                _collectionHandle = collectionHandle;
                _tmpKeychain = tmpKeychain;
            }

            public void Dispose()
            {
                _collectionHandle.Dispose();
                _tmpKeychain?.Dispose();
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
                        ICertificatePal certPal = CertificatePal.FromHandle(handle, throwOnFail: false);

                        if (certPal != null)
                        {
                            X509Certificate2 cert = new X509Certificate2(certPal);
                            collection.Add(cert);
                        }
                    }
                }
            }
        }
    }
}