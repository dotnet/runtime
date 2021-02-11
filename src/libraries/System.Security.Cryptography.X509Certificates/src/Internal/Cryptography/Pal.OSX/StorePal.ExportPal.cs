// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertificateExporter : UnixExportProvider
        {
            public AppleCertificateExporter(ICertificatePalCore cert)
                : base(cert)
            {
            }

            public AppleCertificateExporter(X509Certificate2Collection certs)
                : base(certs)
            {
            }

            protected override byte[] ExportPkcs7()
            {
                IntPtr[] certHandles;

                if (_singleCertPal != null)
                {
                    certHandles = new[] { ((AppleCertificatePal)_singleCertPal).CertificateHandle.DangerousGetHandle() };
                }
                else
                {
                    certHandles = new IntPtr[_certs!.Count];

                    for (int i = 0; i < _certs.Count; i++)
                    {
                        AppleCertificatePal pal = (AppleCertificatePal)_certs[i].Pal;
                        certHandles[i] = pal.CertificateHandle.DangerousGetHandle();
                    }
                }

                return Interop.AppleCrypto.X509ExportPkcs7(certHandles);
            }

            protected override byte[] ExportPkcs8(ICertificatePalCore certificatePal, ReadOnlySpan<char> password)
            {
                AppleCertificatePal pal = (AppleCertificatePal)certificatePal;
                return pal.ExportPkcs8(password);
            }
        }
    }
}
