// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, "handle");

            return new OpenSslX509CertificateReader(Interop.libcrypto.X509_dup(handle));
        }

        public static unsafe ICertificatePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            // If we can see a hyphen, assume it's PEM.  Otherwise try DER-X509, then fall back to DER-PKCS12.
            SafeX509Handle cert;

            // PEM
            if (rawData[0] == '-')
            {
                using (SafeBioHandle bio = Interop.libcrypto.BIO_new(Interop.libcrypto.BIO_s_mem()))
                {
                    Interop.libcrypto.CheckValidOpenSslHandle(bio);

                    Interop.libcrypto.BIO_write(bio, rawData, rawData.Length);
                    cert = Interop.libcrypto.PEM_read_bio_X509_AUX(bio, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                }

                Interop.libcrypto.CheckValidOpenSslHandle(cert);
            }

            // DER-X509
            cert = Interop.libcrypto.OpenSslD2I((ptr, b, i) => Interop.libcrypto.d2i_X509(ptr, b, i), rawData, checkHandle: false);

            if (!cert.IsInvalid)
            {
                return new OpenSslX509CertificateReader(cert);
            }

            // DER-PKCS12
            OpenSslPkcs12Reader pfx;

            if (OpenSslPkcs12Reader.TryRead(rawData, out pfx))
            {
                using (pfx)
                {
                    pfx.Decrypt(password);

                    ICertificatePal first = null;

                    foreach (OpenSslX509CertificateReader certPal in pfx.ReadCertificates())
                    {
                        // When requesting an X509Certificate2 from a PFX only the first entry is
                        // returned.  Other entries should be disposed.
                        if (first == null)
                        {
                            first = certPal;
                        }
                        else
                        {
                            certPal.Dispose();
                        }
                    }

                    return first;
                }
            }

            // Unsupported
            throw Interop.libcrypto.CreateOpenSslCryptographicException();
        }

        public static ICertificatePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            // If we can't open the file, fail right away.
            using (SafeBioHandle fileBio = Interop.libcrypto.BIO_new_file(fileName, "rb"))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(fileBio);

                return OpenSslX509CertificateReader.FromBio(fileBio, password);
            }
        }
    }
}
