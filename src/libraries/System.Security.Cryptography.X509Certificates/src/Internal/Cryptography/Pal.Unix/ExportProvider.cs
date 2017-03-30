// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed class ExportProvider : IExportPal
    {
        private static readonly SafeEvpPKeyHandle InvalidPKeyHandle = new SafeEvpPKeyHandle(IntPtr.Zero, false);

        private ICertificatePal _singleCertPal;
        private X509Certificate2Collection _certs;

        internal ExportProvider(ICertificatePal singleCertPal)
        {
            _singleCertPal = singleCertPal;
        }

        internal ExportProvider(X509Certificate2Collection certs)
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
                case X509ContentType.Pfx:
                    return ExportPfx(password);
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

        private byte[] ExportPfx(SafePasswordHandle password)
        {
            using (SafeX509StackHandle publicCerts = Interop.Crypto.NewX509Stack())
            {
                SafeX509Handle privateCertHandle = SafeX509Handle.InvalidHandle;
                SafeEvpPKeyHandle privateCertKeyHandle = InvalidPKeyHandle;

                if (_singleCertPal != null)
                {
                    var certPal = (OpenSslX509CertificateReader)_singleCertPal;

                    if (_singleCertPal.HasPrivateKey)
                    {
                        privateCertHandle = certPal.SafeHandle;
                        privateCertKeyHandle = certPal.PrivateKeyHandle;
                    }
                    else
                    {
                        PushHandle(certPal.Handle, publicCerts);
                    }
                }
                else
                {
                    X509Certificate2 privateCert = null;

                    // Walk the collection backwards, because we're pushing onto a stack.
                    // This will cause the read order later to be the same as it was now.
                    for (int i = _certs.Count - 1; i >= 0; --i)
                    {
                        X509Certificate2 cert = _certs[i];

                        if (cert.HasPrivateKey)
                        {
                            if (privateCert != null)
                            {
                                // OpenSSL's PKCS12 accelerator (PKCS12_create) only supports one
                                // private key.  The data structure supports more than one, but
                                // being able to use that functionality requires a lot more code for
                                // a low-usage scenario.
                                throw new PlatformNotSupportedException(SR.NotSupported_Export_MultiplePrivateCerts);
                            }

                            privateCert = cert;
                        }
                        else
                        {
                            PushHandle(cert.Handle, publicCerts);
                        }
                    }
                }

                using (SafePkcs12Handle pkcs12 = Interop.Crypto.Pkcs12Create(
                    password,
                    privateCertKeyHandle,
                    privateCertHandle,
                    publicCerts))
                {
                    if (pkcs12.IsInvalid)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    return Interop.Crypto.OpenSslEncode(
                        Interop.Crypto.GetPkcs12DerSize,
                        Interop.Crypto.EncodePkcs12,
                        pkcs12);
                }
            }
        }

        private static void PushHandle(IntPtr certPtr, SafeX509StackHandle publicCerts)
        {
            using (SafeX509Handle certHandle = Interop.Crypto.X509UpRef(certPtr))
            {
                if (!Interop.Crypto.PushX509StackField(publicCerts, certHandle))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // The handle ownership has been transferred into the STACK_OF(X509).
                certHandle.SetHandleAsInvalid();
            }
        }

        private byte[] ExportPkcs7()
        {
            // Pack all of the certificates into a new PKCS7*, export it to a byte[],
            // then free the PKCS7*, since we don't need it any more.
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.Pkcs7CreateSigned())
            {
                Interop.Crypto.CheckValidOpenSslHandle(pkcs7);

                foreach (X509Certificate2 cert in _certs)
                {
                    // Pkcs7AddCertificate => PKCS7_add_certificate increments the
                    // reference count of the certificate, so passing just the Handle
                    // value is sufficient.
                    //
                    // It gets decremented again when the SafePkcs7Handle is released.
                    if (!Interop.Crypto.Pkcs7AddCertificate(pkcs7, cert.Handle))
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }
                }

                return Interop.Crypto.OpenSslEncode(
                    handle => Interop.Crypto.GetPkcs7DerSize(handle),
                    (handle, buf) => Interop.Crypto.EncodePkcs7(handle, buf),
                    pkcs7);
            }
        }
    }
}
