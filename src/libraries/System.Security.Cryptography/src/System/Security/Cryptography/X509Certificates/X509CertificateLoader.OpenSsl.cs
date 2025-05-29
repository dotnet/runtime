// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
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

        private static partial Pkcs12Key? CreateKey(string algorithm, ReadOnlySpan<byte> pkcs8)
        {
            switch (algorithm)
            {
                case Oids.Rsa or Oids.RsaPss:
                    return new AsymmetricAlgorithmPkcs12PrivateKey(pkcs8, static () => new RSAOpenSsl());
                case Oids.EcPublicKey or Oids.EcDiffieHellman:
                    return new AsymmetricAlgorithmPkcs12PrivateKey(pkcs8, static () => new ECDiffieHellmanOpenSsl());
                case Oids.Dsa:
                    return new AsymmetricAlgorithmPkcs12PrivateKey(pkcs8, static () => new DSAOpenSsl());
                case Oids.MlKem512 or Oids.MlKem768 or Oids.MlKem1024:
                    return new MLKemPkcs12PrivateKey(pkcs8);
                case Oids.MLDsa44 or Oids.MLDsa65 or Oids.MLDsa87:
                    return new MLDsaPkcs12PrivateKey(pkcs8);
                case string when Helpers.IsSlhDsaOid(algorithm):
                    return new SlhDsaPkcs12PrivateKey(pkcs8);
                default:
                    return null;
            }
        }

        internal static SafeEvpPKeyHandle GetPrivateKey(Pkcs12Key key)
        {
            if (key.Key is RSAOpenSsl rsa)
            {
                return rsa.DuplicateKeyHandle();
            }

            if (key.Key is DSAOpenSsl dsa)
            {
                return dsa.DuplicateKeyHandle();
            }

            if (key.Key is MLKem kem)
            {
                // We should always get back an MLKemImplementation from PKCS8 loading.
                MLKemImplementation? impl = kem as MLKemImplementation;
                Debug.Assert(impl is not null, "MLKem implementation is not handled for duplicating a handle.");
                return impl.DuplicateHandle();
            }

            if (key.Key is MLDsa mldsa)
            {
                MLDsaImplementation? impl = mldsa as MLDsaImplementation;
                Debug.Assert(impl is not null, "MLDsa implementation is not handled for duplicating a handle.");
                return impl.DuplicateHandle();
            }

            if (key.Key is SlhDsa slhDsa)
            {
                SlhDsaImplementation? impl = slhDsa as SlhDsaImplementation;
                Debug.Assert(impl is not null, "SlhDsa implementation is not handled for duplicating a handle.");
                return impl.DuplicateHandle();
            }

            return ((ECDiffieHellmanOpenSsl)key.Key).DuplicateKeyHandle();
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
