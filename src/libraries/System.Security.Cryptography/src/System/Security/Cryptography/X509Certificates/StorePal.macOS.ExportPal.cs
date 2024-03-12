// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertificateExporter : UnixExportProvider
        {
            private static ReadOnlySpan<byte> EmptyPkcs7 =>
            [
                0x30, 0x23, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07,
                0x02, 0xA0, 0x16, 0x30, 0x14, 0x02, 0x01, 0x01, 0x31, 0x00, 0x30, 0x0B,
                0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x07, 0x01, 0x31,
                0x00,
            ];

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
                else if (_certs!.Count > 0)
                {
                    certHandles = new IntPtr[_certs.Count];

                    for (int i = 0; i < _certs.Count; i++)
                    {
                        AppleCertificatePal pal = (AppleCertificatePal)_certs[i].Pal;
                        certHandles[i] = pal.CertificateHandle.DangerousGetHandle();
                    }
                }
                else
                {
                    // macOS does not correctly create an empty PKCS7 and instead produces
                    // an errSecInternalComponent error. Instead we return a pre-constructed
                    // empty PKCS7 structure.
                    return EmptyPkcs7.ToArray();
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
