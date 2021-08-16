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
    internal sealed class ApplePkcs12CertLoader : ILoaderPal
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
