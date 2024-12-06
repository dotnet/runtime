// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data)
        {
            ICertificatePal? pal;

            if (OpenSslX509CertificateReader.TryReadX509Der(data, out pal) ||
                OpenSslX509CertificateReader.TryReadX509Pem(data, out pal))
            {
                return pal;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static partial ICertificatePal LoadCertificatePalFromFile(string path)
        {
            ICertificatePal? pal;

            using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(path, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(fileBio);

                int bioPosition = Interop.Crypto.BioTell(fileBio);
                Debug.Assert(bioPosition >= 0);

                if (!OpenSslX509CertificateReader.TryReadX509Der(fileBio, out pal))
                {
                    OpenSslX509CertificateReader.RewindBio(fileBio, bioPosition);

                    if (!OpenSslX509CertificateReader.TryReadX509Pem(fileBio, out pal))
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
            }

            return pal;
        }

        private static partial Pkcs12Return FromCertAndKey(CertAndKey certAndKey, ImportState importState)
        {
            OpenSslX509CertificateReader pal = (OpenSslX509CertificateReader)certAndKey.Cert!;

            if (certAndKey.Key is not null)
            {
                pal.SetPrivateKey(GetPrivateKey(certAndKey.Key));
                certAndKey.Key.Dispose();
            }

            return new Pkcs12Return(pal);
        }

        private static partial AsymmetricAlgorithm? CreateKey(string algorithm)
        {
            return algorithm switch
            {
                Oids.Rsa or Oids.RsaPss => new RSAOpenSsl(),
                Oids.EcPublicKey or Oids.EcDiffieHellman => new ECDiffieHellmanOpenSsl(),
                Oids.Dsa => new DSAOpenSsl(),
                _ => null,
            };
        }

        internal static SafeEvpPKeyHandle GetPrivateKey(AsymmetricAlgorithm key)
        {
            if (key is RSAOpenSsl rsa)
            {
                return rsa.DuplicateKeyHandle();
            }

            if (key is DSAOpenSsl dsa)
            {
                return dsa.DuplicateKeyHandle();
            }

            return ((ECDiffieHellmanOpenSsl)key).DuplicateKeyHandle();
        }

        private static partial ICertificatePalCore LoadX509Der(ReadOnlyMemory<byte> data)
        {
            if (OpenSslX509CertificateReader.TryReadX509Der(data.Span, out ICertificatePal? ret))
            {
                return ret;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }
    }
}
