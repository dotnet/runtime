using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal class CollectionBackedStoreProvider : IStorePal
    {
        private readonly ReadOnlyCollection<X509Certificate2> _certs;

        internal CollectionBackedStoreProvider(X509Certificate2 cert)
        {
            _certs = new List<X509Certificate2>(1)
            {
                cert
            }.AsReadOnly();
        }

        internal CollectionBackedStoreProvider(X509Certificate2Collection certs)
        {
            var list = new List<X509Certificate2>(certs.Count);

            foreach (X509Certificate2 cert in certs)
            {
                list.Add(cert);
            }

            _certs = list.AsReadOnly();
        }

        public void Dispose()
        {
        }

        public IEnumerable<X509Certificate2> Find(X509FindType findType, object findValue, bool validOnly)
        {
            return Array.Empty<X509Certificate2>();
        }

        public byte[] Export(X509ContentType contentType, string password)
        {
            switch (contentType)
            {
                case X509ContentType.Cert:
                    return ExportX509Der();
                case X509ContentType.Pfx:
                    return ExportPfx(password);
                default:
                    throw new NotImplementedException();
            }
        }

        private byte[] ExportX509Der()
        {
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

        private byte[] ExportPfx(string password)
        {
            using (SafeX509StackHandle publicCerts = Interop.NativeCrypto.NewX509Stack())
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
                        using (SafeX509Handle certHandle = Interop.libcrypto.X509_dup(cert.Handle))
                        {
                            if (!Interop.NativeCrypto.PushX509StackField(publicCerts, certHandle))
                            {
                                throw Interop.libcrypto.CreateOpenSslCryptographicException();
                            }

                            // The handle ownership has been transferred into the STACK_OF(X509).
                            certHandle.SetHandleAsInvalid();
                        }
                    }
                }

                SafeX509Handle privateCertHandle;
                SafeEvpPkeyHandle privateCertKeyHandle;

                if (privateCert != null)
                {
                    OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)privateCert.Pal;
                    privateCertHandle = pal.SafeHandle;
                    privateCertKeyHandle = pal.PrivateKeyHandle ?? SafeEvpPkeyHandle.InvalidHandle;
                }
                else
                {
                    privateCertHandle = SafeX509Handle.InvalidHandle;
                    privateCertKeyHandle = SafeEvpPkeyHandle.InvalidHandle;
                }

                using (SafePkcs12Handle pkcs12 = Interop.libcrypto.PKCS12_create(
                    password,
                    null,
                    privateCertKeyHandle,
                    privateCertHandle,
                    publicCerts,
                    Interop.libcrypto.NID_undef,
                    Interop.libcrypto.NID_undef,
                    Interop.libcrypto.PKCS12_DEFAULT_ITER,
                    Interop.libcrypto.PKCS12_DEFAULT_ITER,
                    0))
                {
                    if (pkcs12.IsInvalid)
                    {
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();
                    }

                    unsafe
                    {
                        return Interop.libcrypto.OpenSslI2D(
                            (handle, b) => Interop.libcrypto.i2d_PKCS12(handle, b),
                            pkcs12);
                    }
                }
            }
        }

        public IEnumerable<X509Certificate2> Certificates
        {
            get { return _certs; }
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