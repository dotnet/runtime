// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography.Asn1;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslX509Encoder : ManagedX509ExtensionProcessor, IX509Pal
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


        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal? certificatePal)
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

            {
                OpenSslPkcs12Reader? pfx;

                if (OpenSslPkcs12Reader.TryRead(rawData, out pfx))
                {
                    pfx.Dispose();
                    return X509ContentType.Pkcs12;
                }
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
            {
                OpenSslPkcs12Reader? pkcs12Reader;

                if (OpenSslPkcs12Reader.TryRead(File.ReadAllBytes(fileName), out pkcs12Reader))
                {
                    pkcs12Reader.Dispose();

                    return X509ContentType.Pkcs12;
                }
            }

            // Unsupported format.
            // Windows throws new CryptographicException(CRYPT_E_NO_MATCH)
            throw new CryptographicException();
        }

        public override void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
        {
            using (SafeAsn1BitStringHandle bitString = Interop.Crypto.DecodeAsn1BitString(encoded, encoded.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(bitString);

                byte[] decoded = Interop.Crypto.GetAsn1StringBytes(bitString.DangerousGetHandle());

                // Only 9 bits are defined.
                if (decoded.Length > 2)
                {
                    throw new CryptographicException();
                }

                // DER encodings of BIT_STRING values number the bits as
                // 01234567 89 (big endian), plus a number saying how many bits of the last byte were padding.
                //
                // So digitalSignature (0) doesn't mean 2^0 (0x01), it means the most significant bit
                // is set in this byte stream.
                //
                // BIT_STRING values are compact.  So a value of cRLSign (6) | keyEncipherment (2), which
                // is 0b0010001 => 0b0010 0010 (1 bit padding) => 0x22 encoded is therefore
                // 0x02 (length remaining) 0x01 (1 bit padding) 0x22.
                //
                // OpenSSL's d2i_ASN1_BIT_STRING is going to take that, and return 0x22.  0x22 lines up
                // exactly with X509KeyUsageFlags.CrlSign (0x20) | X509KeyUsageFlags.KeyEncipherment (0x02)
                //
                // Once the decipherOnly (8) bit is added to the mix, the values become:
                // 0b001000101 => 0b0010 0010 1000 0000 (7 bits padding)
                // { 0x03 0x07 0x22 0x80 }
                // And OpenSSL returns new byte[] { 0x22 0x80 }
                //
                // The value of X509KeyUsageFlags.DecipherOnly is 0x8000.  0x8000 in a little endian
                // representation is { 0x00 0x80 }.  This means that the DER storage mechanism has effectively
                // ended up being little-endian for BIT_STRING values.  Untwist the bytes, and now the bits all
                // line up with the existing X509KeyUsageFlags.

                int value = 0;

                if (decoded.Length > 0)
                {
                    value = decoded[0];
                }

                if (decoded.Length > 1)
                {
                    value |= decoded[1] << 8;
                }

                keyUsages = (X509KeyUsageFlags)value;
            }
        }

        public override void DecodeX509BasicConstraints2Extension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            if (!Interop.Crypto.DecodeX509BasicConstraints2Extension(
                encoded,
                encoded.Length,
                out certificateAuthority,
                out hasPathLengthConstraint,
                out pathLengthConstraint))
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }
        }

        public override void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
        {
            OidCollection oids;

            using (SafeEkuExtensionHandle eku = Interop.Crypto.DecodeExtendedKeyUsage(encoded, encoded.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(eku);

                int count = Interop.Crypto.GetX509EkuFieldCount(eku);
                oids = new OidCollection(count);

                for (int i = 0; i < count; i++)
                {
                    IntPtr oidPtr = Interop.Crypto.GetX509EkuField(eku, i);

                    if (oidPtr == IntPtr.Zero)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    string oidValue = Interop.Crypto.GetOidValue(oidPtr);

                    oids.Add(new Oid(oidValue));
                }
            }

            usages = oids;
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

        private static DSAOpenSsl BuildDsaPublicKey(byte[] encodedKeyValue, byte[] encodedParameters)
        {
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn { Algorithm = Oids.Dsa, Parameters = encodedParameters },
                SubjectPublicKey = encodedKeyValue,
            };

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            spki.Encode(writer);

            DSAOpenSsl dsa = new DSAOpenSsl();
            try
            {
                dsa.ImportSubjectPublicKeyInfo(writer.Encode(), out _);
                return dsa;
            }
            catch (Exception)
            {
                dsa.Dispose();
                throw;
            }
        }
    }
}
