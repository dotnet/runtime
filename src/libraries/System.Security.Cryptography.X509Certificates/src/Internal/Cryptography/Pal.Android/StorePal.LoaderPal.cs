// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AndroidCertLoader : ILoaderPal
        {
            private readonly SafeX509Handle[] _certs;

            public AndroidCertLoader(SafeX509Handle[] certs)
            {
                _certs = certs;
            }

            public void Dispose()
            {
                foreach (var cert in _certs)
                    cert.Dispose();
            }

            public void MoveTo(X509Certificate2Collection collection)
            {
                for (int i = 0; i < _certs.Length; i++)
                {
                    SafeX509Handle handle = _certs[i];
                    System.Diagnostics.Debug.Assert(!handle.IsInvalid);

                    ICertificatePal certPal = AndroidCertificatePal.FromHandle(handle.DangerousGetHandle());
                    X509Certificate2 cert = new X509Certificate2(certPal);
                    collection.Add(cert);
                }
            }
        }
    }
}
