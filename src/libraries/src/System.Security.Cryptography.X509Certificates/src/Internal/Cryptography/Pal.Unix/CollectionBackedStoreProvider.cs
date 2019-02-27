// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class CollectionBackedStoreProvider : IStorePal
    {
        private readonly List<X509Certificate2> _certs;
        private SafeX509StackHandle _nativeCollection;

        internal CollectionBackedStoreProvider(List<X509Certificate2> certs)
        {
            _certs = certs;
        }

        public void Dispose()
        {
            // Dispose is explicitly doing nothing here.
            // The LM\Root and LM\CA stores are reused after being Disposed, because there's no
            // point in re-allocating the array to instantiate them every time.
            // But the interface requires Dispose.
        }

        public void CloneTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);
            Debug.Assert(_certs != null);

            foreach (X509Certificate2 cert in _certs)
            {
                var certPal = (OpenSslX509CertificateReader)cert.Pal;
                collection.Add(new X509Certificate2(certPal.DuplicateHandles()));
            }
        }

        public void Add(ICertificatePal cert)
        {
            throw new InvalidOperationException();
        }

        public void Remove(ICertificatePal cert)
        {
            throw new InvalidOperationException();
        }

        SafeHandle IStorePal.SafeHandle
        {
            get { return null; }
        }

        internal SafeX509StackHandle GetNativeCollection()
        {
            if (_nativeCollection == null)
            {
                lock (_certs)
                {
                    if (_nativeCollection == null)
                    {
                        SafeX509StackHandle nativeCollection = Interop.Crypto.NewX509Stack();

                        foreach (X509Certificate2 cert in _certs)
                        {
                            var certPal = (OpenSslX509CertificateReader)cert.Pal;

                            using (SafeX509Handle tmp = Interop.Crypto.X509UpRef(certPal.SafeHandle))
                            {
                                if (!Interop.Crypto.PushX509StackField(nativeCollection, tmp))
                                {
                                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                                }

                                // Ownership was transferred to the cert stack.
                                tmp.SetHandleAsInvalid();
                            }
                        }

                        _nativeCollection = nativeCollection;
                    }
                }
            }

            return _nativeCollection;
        }
    }
}
