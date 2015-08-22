// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
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

        public static ICertificatePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            ICertificatePal cert;

            if (TryReadX509Der(rawData, out cert) ||
                TryReadX509Pem(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs7Der(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs7Pem(rawData, out cert) ||
                PkcsFormatReader.TryReadPkcs12(rawData, password, out cert))
            {
                if (cert == null)
                {
                    // Empty collection, most likely.
                    throw new CryptographicException();
                }

                return cert;
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

                return FromBio(fileBio, password);
            }
        }

        private static ICertificatePal FromBio(SafeBioHandle bio, string password)
        {
            int bioPosition = Interop.Crypto.BioTell(bio);

            Debug.Assert(bioPosition >= 0);

            ICertificatePal certPal;
            if (TryReadX509Pem(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (TryReadX509Der(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Pem(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs7Der(bio, out certPal))
            {
                return certPal;
            }

            // Rewind, try again.
            RewindBio(bio, bioPosition);

            if (PkcsFormatReader.TryReadPkcs12(bio, password, out certPal))
            {
                return certPal;
            }

            // Since we aren't going to finish reading, leaving the buffer where it was when we got
            // it seems better than leaving it in some arbitrary other position.
            // 
            // But, before seeking back to start, save the Exception representing the last reported
            // OpenSSL error in case the last BioSeek would change it.
            Exception openSslException = Interop.libcrypto.CreateOpenSslCryptographicException();

            // Use BioSeek directly for the last seek attempt, because any failure here should instead
            // report the already created (but not yet thrown) exception.
            Interop.Crypto.BioSeek(bio, bioPosition);

            throw openSslException;
        }

        internal static void RewindBio(SafeBioHandle bio, int bioPosition)
        {
            int ret = Interop.Crypto.BioSeek(bio, bioPosition);

            if (ret < 0)
            {
                throw Interop.libcrypto.CreateOpenSslCryptographicException();
            }
        }

        internal static unsafe bool TryReadX509Der(byte[] rawData, out ICertificatePal certPal)
        {
            SafeX509Handle certHandle = Interop.libcrypto.OpenSslD2I(
                (ptr, b, i) => Interop.libcrypto.d2i_X509(ptr, b, i),
                rawData,
                checkHandle: false);

            if (certHandle.IsInvalid)
            {
                certPal = null;
                return false;
            }

            certPal = new OpenSslX509CertificateReader(certHandle);
            return true;
        }

        internal static bool TryReadX509Pem(SafeBioHandle bio, out ICertificatePal certPal)
        {
            SafeX509Handle cert = Interop.libcrypto.PEM_read_bio_X509_AUX(bio, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (cert.IsInvalid)
            {
                certPal = null;
                return false;
            }

            certPal = new OpenSslX509CertificateReader(cert);
            return true;
        }

        internal static bool TryReadX509Pem(byte[] rawData, out ICertificatePal certPal)
        {
            SafeX509Handle certHandle;
            using (SafeBioHandle bio = Interop.libcrypto.BIO_new(Interop.libcrypto.BIO_s_mem()))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(bio);

                Interop.libcrypto.BIO_write(bio, rawData, rawData.Length);
                certHandle = Interop.libcrypto.PEM_read_bio_X509_AUX(bio, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            if (certHandle.IsInvalid)
            {
                certPal = null;
                return false;
            }

            certPal = new OpenSslX509CertificateReader(certHandle);
            return true;
        }

        internal static bool TryReadX509Der(SafeBioHandle bio, out ICertificatePal fromBio)
        {
            SafeX509Handle cert = Interop.Crypto.ReadX509AsDerFromBio(bio);

            if (cert.IsInvalid)
            {
                fromBio = null;
                return false;
            }

            fromBio = new OpenSslX509CertificateReader(cert);
            return true;
        }
    }
}
