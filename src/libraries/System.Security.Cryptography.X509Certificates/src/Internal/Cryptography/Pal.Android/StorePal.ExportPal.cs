// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AndroidExportProvider : UnixExportProvider
        {
            public AndroidExportProvider(ICertificatePalCore cert)
                : base(cert)
            {
            }

            public AndroidExportProvider(X509Certificate2Collection certs)
                : base(certs)
            {
            }

            protected override byte[] ExportPkcs7()
            {
                IntPtr[] certHandles;
                if (_singleCertPal != null)
                {
                    certHandles = new[] { _singleCertPal.Handle };
                }
                else
                {
                    Debug.Assert(_certs != null);
                    certHandles = new IntPtr[_certs!.Count];
                    for (int i = 0; i < _certs.Count; i++)
                    {
                        certHandles[i] = _certs[i].Pal.Handle;
                    }
                }

                Debug.Assert(certHandles.Length > 0);
                return Interop.AndroidCrypto.X509ExportPkcs7(certHandles);
            }

            protected override byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password)
            {
                Debug.Assert(certificatePal.HasPrivateKey);
                SafeKeyHandle? privateKey = ((AndroidCertificatePal)certificatePal).PrivateKeyHandle;

                AsymmetricAlgorithm algorithm;
                switch (privateKey)
                {
                    case SafeEcKeyHandle ec:
                        algorithm = new ECDsaImplementation.ECDsaAndroid(ec);
                        break;
                    case SafeRsaHandle rsa:
                        algorithm = new RSAImplementation.RSAAndroid(rsa);
                        break;
                    case SafeDsaHandle dsa:
                        algorithm = new DSAImplementation.DSAAndroid(dsa);
                        break;
                    default:
                        throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
                }

                using (algorithm)
                {
                    return algorithm.ExportEncryptedPkcs8PrivateKey(password, s_windowsPbe);
                }
            }
        }
    }
}
