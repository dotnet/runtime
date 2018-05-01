// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class PkcsFormatReader
    {
        internal static bool IsPkcs7(byte[] rawData)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.DecodePkcs7(rawData, rawData.Length))
            {
                if (pkcs7.IsInvalid)
                {
                    Interop.Crypto.ErrClearError();
                }
                else
                {
                    return true;
                }

            }

            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                if (Interop.Crypto.BioWrite(bio, rawData, rawData.Length) != rawData.Length)
                {
                    Interop.Crypto.ErrClearError();
                }

                using (SafePkcs7Handle pkcs7 = Interop.Crypto.PemReadBioPkcs7(bio))
                {
                    if (pkcs7.IsInvalid)
                    {
                        Interop.Crypto.ErrClearError();
                        return false;
                    }

                    return true;
                }
            }
        }

        internal static bool IsPkcs7Der(SafeBioHandle fileBio)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.D2IPkcs7Bio(fileBio))
            {
                if (pkcs7.IsInvalid)
                {
                    Interop.Crypto.ErrClearError();
                    return false;
                }

                return true;
            }
        }

        internal static bool IsPkcs7Pem(SafeBioHandle fileBio)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.PemReadBioPkcs7(fileBio))
            {
                if (pkcs7.IsInvalid)
                {
                    Interop.Crypto.ErrClearError();
                    return false;
                }

                return true;
            }
        }

        internal static bool TryReadPkcs7Der(byte[] rawData, out ICertificatePal certPal)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs7Der(rawData, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Der(SafeBioHandle bio, out ICertificatePal certPal)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs7Der(bio, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Der(byte[] rawData, out List<ICertificatePal> certPals)
        {
            ICertificatePal ignored;

            return TryReadPkcs7Der(rawData, false, out ignored, out certPals);
        }

        internal static bool TryReadPkcs7Der(SafeBioHandle bio, out List<ICertificatePal> certPals)
        {
            ICertificatePal ignored;

            return TryReadPkcs7Der(bio, false, out ignored, out certPals);
        }

        private static bool TryReadPkcs7Der(
            byte[] rawData,
            bool single,
            out ICertificatePal certPal,
            out List<ICertificatePal> certPals)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.DecodePkcs7(rawData, rawData.Length))
            {
                if (pkcs7.IsInvalid)
                {
                    certPal = null;
                    certPals = null;
                    Interop.Crypto.ErrClearError();
                    return false;
                }

                return TryReadPkcs7(pkcs7, single, out certPal, out certPals);
            }
        }

        private static bool TryReadPkcs7Der(
            SafeBioHandle bio,
            bool single,
            out ICertificatePal certPal,
            out List<ICertificatePal> certPals)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.D2IPkcs7Bio(bio))
            {
                if (pkcs7.IsInvalid)
                {
                    certPal = null;
                    certPals = null;
                    Interop.Crypto.ErrClearError();
                    return false;
                }

                return TryReadPkcs7(pkcs7, single, out certPal, out certPals);
            }
        }

        internal static bool TryReadPkcs7Pem(byte[] rawData, out ICertificatePal certPal)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs7Pem(rawData, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Pem(SafeBioHandle bio, out ICertificatePal certPal)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs7Pem(bio, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Pem(byte[] rawData, out List<ICertificatePal> certPals)
        {
            ICertificatePal ignored;

            return TryReadPkcs7Pem(rawData, false, out ignored, out certPals);
        }

        internal static bool TryReadPkcs7Pem(SafeBioHandle bio, out List<ICertificatePal> certPals)
        {
            ICertificatePal ignored;

            return TryReadPkcs7Pem(bio, false, out ignored, out certPals);
        }

        private static bool TryReadPkcs7Pem(
            byte[] rawData,
            bool single,
            out ICertificatePal certPal,
            out List<ICertificatePal> certPals)
        {
            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                if (Interop.Crypto.BioWrite(bio, rawData, rawData.Length) != rawData.Length)
                {
                    Interop.Crypto.ErrClearError();
                }

                return TryReadPkcs7Pem(bio, single, out certPal, out certPals);
            }
        }

        private static bool TryReadPkcs7Pem(
            SafeBioHandle bio,
            bool single,
            out ICertificatePal certPal,
            out List<ICertificatePal> certPals)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.PemReadBioPkcs7(bio))
            {
                if (pkcs7.IsInvalid)
                {
                    certPal = null;
                    certPals = null;
                    Interop.Crypto.ErrClearError();
                    return false;
                }

                return TryReadPkcs7(pkcs7, single, out certPal, out certPals);
            }
        }

        private static bool TryReadPkcs7(
            SafePkcs7Handle pkcs7,
            bool single,
            out ICertificatePal certPal,
            out List<ICertificatePal> certPals)
        {
            List<ICertificatePal> readPals = single ? null : new List<ICertificatePal>();

            using (SafeSharedX509StackHandle certs = Interop.Crypto.GetPkcs7Certificates(pkcs7))
            {
                int count = Interop.Crypto.GetX509StackFieldCount(certs);

                if (single)
                {
                    // In single mode for a PKCS#7 signed or signed-and-enveloped file we're supposed to return
                    // the certificate which signed the PKCS#7 file.
                    // 
                    // X509Certificate2Collection::Export(X509ContentType.Pkcs7) claims to be a signed PKCS#7,
                    // but doesn't emit a signature block. So this is hard to test.
                    //
                    // TODO(2910): Figure out how to extract the signing certificate, when it's present.
                    throw new CryptographicException(SR.Cryptography_X509_PKCS7_NoSigner);
                }

                for (int i = 0; i < count; i++)
                {
                    // Use FromHandle to duplicate the handle since it would otherwise be freed when the PKCS7
                    // is Disposed.
                    IntPtr certHandle = Interop.Crypto.GetX509StackField(certs, i);
                    ICertificatePal pal = CertificatePal.FromHandle(certHandle);
                    readPals.Add(pal);
                }
            }

            certPal = null;
            certPals = readPals;
            return true;
        }

        internal static bool TryReadPkcs12(byte[] rawData, SafePasswordHandle password, out ICertificatePal certPal, out Exception openSslException)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs12(rawData, password, true, out certPal, out ignored, out openSslException);
        }

        internal static bool TryReadPkcs12(SafeBioHandle bio, SafePasswordHandle password, out ICertificatePal certPal, out Exception openSslException)
        {
            List<ICertificatePal> ignored;

            return TryReadPkcs12(bio, password, true, out certPal, out ignored, out openSslException);
        }

        internal static bool TryReadPkcs12(byte[] rawData, SafePasswordHandle password, out List<ICertificatePal> certPals, out Exception openSslException)
        {
            ICertificatePal ignored;

            return TryReadPkcs12(rawData, password, false, out ignored, out certPals, out openSslException);
        }

        internal static bool TryReadPkcs12(SafeBioHandle bio, SafePasswordHandle password, out List<ICertificatePal> certPals, out Exception openSslException)
        {
            ICertificatePal ignored;

            return TryReadPkcs12(bio, password, false, out ignored, out certPals, out openSslException);
        }

        private static bool TryReadPkcs12(
            byte[] rawData,
            SafePasswordHandle password,
            bool single,
            out ICertificatePal readPal,
            out List<ICertificatePal> readCerts,
            out Exception openSslException)
        {
            // DER-PKCS12
            OpenSslPkcs12Reader pfx;

            if (!OpenSslPkcs12Reader.TryRead(rawData, out pfx, out openSslException))
            {
                readPal = null;
                readCerts = null;
                return false;
            }

            using (pfx)
            {
                return TryReadPkcs12(pfx, password, single, out readPal, out readCerts);
            }
        }

        private static bool TryReadPkcs12(
            SafeBioHandle bio,
            SafePasswordHandle password,
            bool single,
            out ICertificatePal readPal,
            out List<ICertificatePal> readCerts,
            out Exception openSslException)
        {
            // DER-PKCS12
            OpenSslPkcs12Reader pfx;

            if (!OpenSslPkcs12Reader.TryRead(bio, out pfx, out openSslException))
            {
                readPal = null;
                readCerts = null;
                return false;
            }

            using (pfx)
            {
                return TryReadPkcs12(pfx, password, single, out readPal, out readCerts);
            }
        }

        private static bool TryReadPkcs12(
            OpenSslPkcs12Reader pfx,
            SafePasswordHandle password,
            bool single,
            out ICertificatePal readPal,
            out List<ICertificatePal> readCerts)
        {
            pfx.Decrypt(password);

            ICertificatePal first = null;
            List<ICertificatePal> certs = null;

            if (!single)
            {
                certs = new List<ICertificatePal>();
            }

            foreach (OpenSslX509CertificateReader certPal in pfx.ReadCertificates())
            {
                if (single)
                {
                    // When requesting an X509Certificate2 from a PFX only the first entry is
                    // returned.  Other entries should be disposed.

                    if (first == null)
                    {
                        first = certPal;
                    }
                    else if (certPal.HasPrivateKey && !first.HasPrivateKey)
                    {
                        first.Dispose();
                        first = certPal;
                    }
                    else
                    {
                        certPal.Dispose();
                    }
                }
                else
                {
                    certs.Add(certPal);
                }
            }

            readPal = first;
            readCerts = certs;
            return true;
        }
    }
}
