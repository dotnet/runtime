// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal
    {
        private sealed class AppleCertificateExporter : IExportPal
        {
            private X509Certificate2Collection _certs;
            private ICertificatePal _singleCertPal;

            public AppleCertificateExporter(ICertificatePal cert)
            {
                _singleCertPal = cert;
            }

            public AppleCertificateExporter(X509Certificate2Collection certs)
            {
                _certs = certs;
            }

            public void Dispose()
            {
                // Don't dispose any of the resources, they're still owned by the caller.
                _singleCertPal = null;
                _certs = null;
            }

            public byte[] Export(X509ContentType contentType, SafePasswordHandle password)
            {
                Debug.Assert(password != null);
                switch (contentType)
                {
                    case X509ContentType.Cert:
                        return ExportX509Der();
                    case X509ContentType.Pkcs12:
                        return ExportPkcs12(password);
                    case X509ContentType.Pkcs7:
                        return ExportPkcs7();
                    case X509ContentType.SerializedCert:
                    case X509ContentType.SerializedStore:
                        throw new PlatformNotSupportedException(SR.Cryptography_Unix_X509_SerializedExport);
                    default:
                        throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
                }
            }

            private byte[] ExportX509Der()
            {
                if (_singleCertPal != null)
                {
                    return _singleCertPal.RawData;
                }

                // Windows/Desktop compatibility: Exporting a collection (or store) as
                // X509ContentType.Cert returns the equivalent of FirstOrDefault(),
                // so anything past _certs[0] is ignored, and an empty collection is
                // null (not an Exception)
                if (_certs.Count == 0)
                {
                    return null;
                }

                return _certs[0].RawData;
            }

            private byte[] ExportPkcs12(SafePasswordHandle password)
            {
                IntPtr[] certHandles;

                if (_singleCertPal != null)
                {
                    certHandles = new[] { _singleCertPal.Handle };
                }
                else
                {
                    certHandles = new IntPtr[_certs.Count];

                    for (int i = 0; i < _certs.Count; i++)
                    {
                        certHandles[i] = _certs[i].Handle;
                    }
                }

                byte[] exported = Interop.AppleCrypto.X509ExportPfx(certHandles, password);
                GC.KeepAlive(_certs); // ensure certs' safe handles aren't finalized while raw handles are in use
                return exported;
            }

            private byte[] ExportPkcs7()
            {
                IntPtr[] certHandles;

                if (_singleCertPal != null)
                {
                    certHandles = new[] { ((AppleCertificatePal)_singleCertPal).CertificateHandle.DangerousGetHandle() };
                }
                else
                {
                    certHandles = new IntPtr[_certs.Count];

                    for (int i = 0; i < _certs.Count; i++)
                    {
                        AppleCertificatePal pal = (AppleCertificatePal)_certs[i].Pal;
                        certHandles[i] = pal.CertificateHandle.DangerousGetHandle();
                    }
                }

                return Interop.AppleCrypto.X509ExportPkcs7(certHandles);
            }
        }
    }
}