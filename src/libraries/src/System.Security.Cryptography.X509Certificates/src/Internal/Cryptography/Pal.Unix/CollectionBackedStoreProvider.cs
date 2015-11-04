using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal class CollectionBackedStoreProvider : IStorePal
    {
        private readonly X509Certificate2[] _certs;
        private static readonly SafeEvpPKeyHandle InvalidPKeyHandle = new SafeEvpPKeyHandle(IntPtr.Zero, false);

        internal CollectionBackedStoreProvider(X509Certificate2 cert)
        {
            _certs = new X509Certificate2[] { cert };
        }

        internal CollectionBackedStoreProvider(X509Certificate2Collection certs)
        {
            _certs = new X509Certificate2[certs.Count];
            certs.CopyTo(_certs, 0);
        }

        public void Dispose()
        {
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
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
            // Windows/Desktop compatibility: Exporting a collection (or store) as
            // X509ContentType.Cert returns the equivalent of FirstOrDefault(),
            // so anything past _certs[0] is ignored, and an empty collection is
            // null (not an Exception)

            if (_certs.Length == 0)
            {
                return null;
            }

            return _certs[0].RawData;
        }

        private byte[] ExportPfx(string password)
        {
            using (SafeX509StackHandle publicCerts = Interop.Crypto.NewX509Stack())
            {
                X509Certificate2 privateCert = null;

                // Walk the collection backwards, because we're pushing onto a stack.
                // This will cause the read order later to be the same as it was now.
                for (int i = _certs.Length - 1; i >= 0; --i)
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
                        using (SafeX509Handle certHandle = Interop.Crypto.X509Duplicate(cert.Handle))
                        {
                            if (!Interop.Crypto.PushX509StackField(publicCerts, certHandle))
                            {
                                throw Interop.Crypto.CreateOpenSslCryptographicException();
                            }

                            // The handle ownership has been transferred into the STACK_OF(X509).
                            certHandle.SetHandleAsInvalid();
                        }
                    }
                }

                SafeX509Handle privateCertHandle;
                SafeEvpPKeyHandle privateCertKeyHandle;

                if (privateCert != null)
                {
                    OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)privateCert.Pal;
                    privateCertHandle = pal.SafeHandle;
                    privateCertKeyHandle = pal.PrivateKeyHandle ?? InvalidPKeyHandle;
                }
                else
                {
                    privateCertHandle = SafeX509Handle.InvalidHandle;
                    privateCertKeyHandle = InvalidPKeyHandle;
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

        private byte[] ExportPkcs7()
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.Pkcs7CreateSigned())
            {
                Interop.Crypto.CheckValidOpenSslHandle(pkcs7);

                foreach (X509Certificate2 cert in _certs)
                {
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

        public void CopyTo(X509Certificate2Collection collection)
        {
            Debug.Assert(collection != null);

            collection.AddRange(_certs);
        }

        public void Add(ICertificatePal cert)
        {
            throw new InvalidOperationException();
        }

        public void Remove(ICertificatePal cert)
        {
            throw new InvalidOperationException();
        }
    }
}