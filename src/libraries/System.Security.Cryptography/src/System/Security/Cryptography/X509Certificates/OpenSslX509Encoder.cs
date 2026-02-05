// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslX509Encoder : IX509Pal
    {
        public ECDsa DecodeECDsaPublicKey(ICertificatePal? certificatePal)
        {
            if (certificatePal is null)
                throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

            return ((OpenSslX509CertificateReader)certificatePal).GetECDsaPublicKey();
        }

        public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
        {
            if (certificatePal is null)
                throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

            return ((OpenSslX509CertificateReader)certificatePal).GetECDiffieHellmanPublicKey();
        }


        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[]? encodedParameters, ICertificatePal? certificatePal)
        {
            switch (oid.Value)
            {
                case Oids.Rsa:
                    return BuildRsaPublicKey(encodedKeyValue);
                case Oids.Dsa:
                    return BuildDsaPublicKey(encodedKeyValue, encodedParameters);
            }

            // NotSupportedException is thrown by .NET Framework and .NET Core on Windows.
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flags)
        {
            return X500NameEncoder.X500DistinguishedNameDecode(encodedDistinguishedName, true, flags);
        }

        public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
        {
            return X500NameEncoder.X500DistinguishedNameEncode(distinguishedName, flag);
        }

        public string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
        {
            return X500NameEncoder.X500DistinguishedNameDecode(
                encodedDistinguishedName,
                true,
                multiLine ? X500DistinguishedNameFlags.UseNewLines : X500DistinguishedNameFlags.None,
                multiLine);
        }

        public X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
        {
            {
                ICertificatePal? certPal;

                if (OpenSslX509CertificateReader.TryReadX509Der(rawData, out certPal) ||
                    OpenSslX509CertificateReader.TryReadX509Pem(rawData, out certPal))
                {
                    certPal.Dispose();

                    return X509ContentType.Cert;
                }
            }

            if (OpenSslPkcsFormatReader.IsPkcs7(rawData))
            {
                return X509ContentType.Pkcs7;
            }

            if (X509CertificateLoader.IsPkcs12(rawData))
            {
                return X509ContentType.Pkcs12;
            }

            // Unsupported format.
            // Windows throws new CryptographicException(CRYPT_E_NO_MATCH)
            throw new CryptographicException();
        }

        public X509ContentType GetCertContentType(string fileName)
        {
            // If we can't open the file, fail right away.
            using (SafeBioHandle fileBio = Interop.Crypto.BioNewFile(fileName, "rb"))
            {
                Interop.Crypto.CheckValidOpenSslHandle(fileBio);

                int bioPosition = Interop.Crypto.BioTell(fileBio);
                Debug.Assert(bioPosition >= 0);

                // X509ContentType.Cert
                {
                    ICertificatePal? certPal;

                    if (OpenSslX509CertificateReader.TryReadX509Der(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    OpenSslX509CertificateReader.RewindBio(fileBio, bioPosition);

                    if (OpenSslX509CertificateReader.TryReadX509Pem(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    OpenSslX509CertificateReader.RewindBio(fileBio, bioPosition);
                }

                // X509ContentType.Pkcs7
                {
                    if (OpenSslPkcsFormatReader.IsPkcs7Der(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    OpenSslX509CertificateReader.RewindBio(fileBio, bioPosition);

                    if (OpenSslPkcsFormatReader.IsPkcs7Pem(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    OpenSslX509CertificateReader.RewindBio(fileBio, bioPosition);
                }
            }

            // X509ContentType.Pkcs12 (aka PFX)
            if (X509CertificateLoader.IsPkcs12(fileName))
            {
                return X509ContentType.Pkcs12;
            }

            // Unsupported format.
            // Windows throws new CryptographicException(CRYPT_E_NO_MATCH)
            throw new CryptographicException();
        }

        private static RSAOpenSsl BuildRsaPublicKey(byte[] encodedData)
        {
            var rsa = new RSAOpenSsl();
            try
            {
                rsa.ImportRSAPublicKey(new ReadOnlySpan<byte>(encodedData), out _);
            }
            catch (Exception)
            {
                rsa.Dispose();
                throw;
            }
            return rsa;
        }

        private static DSAOpenSsl BuildDsaPublicKey(byte[] encodedKeyValue, byte[]? encodedParameters)
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = Oids.Dsa,
                    Parameters = encodedParameters.ToNullableMemory(),
                },
                SubjectPublicKey = encodedKeyValue,
            };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            spki.Encode(writer);

            DSAOpenSsl dsa = new DSAOpenSsl();
            try
            {
                return writer.Encode(dsa, static (dsa, encoded) =>
                {
                    dsa.ImportSubjectPublicKeyInfo(encoded, out _);
                    return dsa;
                });
            }
            catch (Exception)
            {
                dsa.Dispose();
                throw;
            }
        }
    }
}
