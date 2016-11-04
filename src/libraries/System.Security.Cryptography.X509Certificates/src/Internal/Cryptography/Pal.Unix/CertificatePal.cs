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
    internal sealed partial class CertificatePal
    {
        public static ICertificatePal FromHandle(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            return new OpenSslX509CertificateReader(Interop.Crypto.X509UpRef(handle));
        }

        public static ICertificatePal FromOtherCert(X509Certificate cert)
        {
            Debug.Assert(cert.Pal != null);

            // Ensure private key is copied
            OpenSslX509CertificateReader certPal = (OpenSslX509CertificateReader)cert.Pal;
            return certPal.DuplicateHandles();
        }

        public static ICertificatePal FromBlob(byte[] rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(password != null);

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
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        public static ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            // If we can't open the file, fail right away.
            using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(fileBio);

                return FromBio(fileBio, password);
            }
        }

        private static ICertificatePal FromBio(SafeBioHandle bio, SafePasswordHandle password)
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
            Exception openSslException = Interop.Crypto.CreateOpenSslCryptographicException();

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
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        internal static bool TryReadX509Der(byte[] rawData, out ICertificatePal certPal)
        {
            SafeX509Handle certHandle = Interop.Crypto.DecodeX509(rawData, rawData.Length);

            if (certHandle.IsInvalid)
            {
                certHandle.Dispose();
                certPal = null;
                return false;
            }

            certPal = new OpenSslX509CertificateReader(certHandle);
            return true;
        }

        internal static bool TryReadX509Pem(SafeBioHandle bio, out ICertificatePal certPal)
        {
            SafeX509Handle cert = Interop.Crypto.PemReadX509FromBio(bio);

            if (cert.IsInvalid)
            {
                cert.Dispose();
                certPal = null;
                return false;
            }

            certPal = new OpenSslX509CertificateReader(cert);
            return true;
        }

        internal static bool TryReadX509Pem(byte[] rawData, out ICertificatePal certPal)
        {
            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                Interop.Crypto.BioWrite(bio, rawData, rawData.Length);
                return TryReadX509Pem(bio, out certPal);
            }
        }

        internal static bool TryReadX509Der(SafeBioHandle bio, out ICertificatePal fromBio)
        {
            SafeX509Handle cert = Interop.Crypto.ReadX509AsDerFromBio(bio);

            if (cert.IsInvalid)
            {
                cert.Dispose();
                fromBio = null;
                return false;
            }

            fromBio = new OpenSslX509CertificateReader(cert);
            return true;
        }
    }
}
