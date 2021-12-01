// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class PkcsFormatReader
    {
        internal static bool IsPkcs7(ReadOnlySpan<byte> rawData)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.DecodePkcs7(rawData))
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

                if (Interop.Crypto.BioWrite(bio, rawData) != rawData.Length)
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

        internal static bool TryReadPkcs7Der(ReadOnlySpan<byte> rawData, out ICertificatePal? certPal)
        {
            List<ICertificatePal>? ignored;

            return TryReadPkcs7Der(rawData, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Der(SafeBioHandle bio, out ICertificatePal? certPal)
        {
            List<ICertificatePal>? ignored;

            return TryReadPkcs7Der(bio, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Der(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            ICertificatePal? ignored;

            return TryReadPkcs7Der(rawData, false, out ignored, out certPals);
        }

        internal static bool TryReadPkcs7Der(SafeBioHandle bio, [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            ICertificatePal? ignored;

            return TryReadPkcs7Der(bio, false, out ignored, out certPals);
        }

        private static bool TryReadPkcs7Der(
            ReadOnlySpan<byte> rawData,
            bool single,
            out ICertificatePal? certPal,
            [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            using (SafePkcs7Handle pkcs7 = Interop.Crypto.DecodePkcs7(rawData))
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
            out ICertificatePal? certPal,
            [NotNullWhen(true)] out List<ICertificatePal>? certPals)
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

        internal static bool TryReadPkcs7Pem(ReadOnlySpan<byte> rawData, out ICertificatePal? certPal)
        {
            List<ICertificatePal>? ignored;

            return TryReadPkcs7Pem(rawData, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Pem(SafeBioHandle bio, out ICertificatePal? certPal)
        {
            List<ICertificatePal>? ignored;

            return TryReadPkcs7Pem(bio, true, out certPal, out ignored);
        }

        internal static bool TryReadPkcs7Pem(ReadOnlySpan<byte> rawData, [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            ICertificatePal? ignored;

            return TryReadPkcs7Pem(rawData, false, out ignored, out certPals);
        }

        internal static bool TryReadPkcs7Pem(SafeBioHandle bio, [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            ICertificatePal? ignored;

            return TryReadPkcs7Pem(bio, false, out ignored, out certPals);
        }

        private static bool TryReadPkcs7Pem(
            ReadOnlySpan<byte> rawData,
            bool single,
            out ICertificatePal? certPal,
            [NotNullWhen(true)] out List<ICertificatePal>? certPals)
        {
            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                if (Interop.Crypto.BioWrite(bio, rawData) != rawData.Length)
                {
                    Interop.Crypto.ErrClearError();
                }

                return TryReadPkcs7Pem(bio, single, out certPal, out certPals);
            }
        }

        private static bool TryReadPkcs7Pem(
            SafeBioHandle bio,
            bool single,
            out ICertificatePal? certPal,
            [NotNullWhen(true)] out List<ICertificatePal>? certPals)
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
            out ICertificatePal? certPal,
            [NotNullWhen(true)] out List<ICertificatePal> certPals)
        {
            List<ICertificatePal>? readPals = single ? null : new List<ICertificatePal>();

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

                Debug.Assert(readPals != null); // null if single == true

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

        internal static bool TryReadPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool ephemeralSpecified,
            [NotNullWhen(true)] out ICertificatePal? certPal,
            out Exception? openSslException)
        {
            List<ICertificatePal>? ignored;

            return TryReadPkcs12(
                rawData,
                password,
                single: true,
                ephemeralSpecified,
                out certPal!,
                out ignored,
                out openSslException);
        }

        internal static bool TryReadPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool ephemeralSpecified,
            [NotNullWhen(true)] out List<ICertificatePal>? certPals,
            out Exception? openSslException)
        {
            ICertificatePal? ignored;

            return TryReadPkcs12(
                rawData,
                password,
                single: false,
                ephemeralSpecified,
                out ignored,
                out certPals!,
                out openSslException);
        }

        private static bool TryReadPkcs12(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            bool single,
            bool ephemeralSpecified,
            out ICertificatePal? readPal,
            out List<ICertificatePal>? readCerts,
            out Exception? openSslException)
        {
            // DER-PKCS12
            OpenSslPkcs12Reader? pfx;

            if (!OpenSslPkcs12Reader.TryRead(rawData, out pfx, out openSslException))
            {
                readPal = null;
                readCerts = null;
                return false;
            }

            using (pfx)
            {
                return TryReadPkcs12(pfx, password, single, ephemeralSpecified, out readPal, out readCerts);
            }
        }

        private static bool TryReadPkcs12(
            OpenSslPkcs12Reader pfx,
            SafePasswordHandle password,
            bool single,
            bool ephemeralSpecified,
            out ICertificatePal? readPal,
            out List<ICertificatePal>? readCerts)
        {
            pfx.Decrypt(password, ephemeralSpecified);

            if (single)
            {
                UnixPkcs12Reader.CertAndKey certAndKey = pfx.GetSingleCert();
                OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)certAndKey.Cert!;

                if (certAndKey.Key != null)
                {
                    pal.SetPrivateKey(OpenSslPkcs12Reader.GetPrivateKey(certAndKey.Key));
                }

                readPal = pal;
                readCerts = null;
                return true;
            }

            readPal = null;
            List<ICertificatePal> certs = new List<ICertificatePal>(pfx.GetCertCount());

            foreach (UnixPkcs12Reader.CertAndKey certAndKey in pfx.EnumerateAll())
            {
                OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)certAndKey.Cert!;

                if (certAndKey.Key != null)
                {
                    pal.SetPrivateKey(OpenSslPkcs12Reader.GetPrivateKey(certAndKey.Key));
                }

                certs.Add(pal);
            }

            readCerts = certs;
            return true;
        }
    }
}
