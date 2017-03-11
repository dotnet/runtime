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
    internal sealed class OpenSslX509Encoder : ManagedX509ExtensionProcessor, IX509Pal
    {
        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal)
        {
            if (oid.Value == Oids.Ecc && certificatePal != null)
            {
                return ((OpenSslX509CertificateReader)certificatePal).GetECDsaPublicKey();
            }

            switch (oid.Value)
            {
                case Oids.RsaRsa:
                    return BuildRsaPublicKey(encodedKeyValue);
                case Oids.DsaDsa:
                    return BuildDsaPublicKey(encodedKeyValue, encodedParameters);
            }

            // NotSupportedException is what desktop and CoreFx-Windows throw in this situation.
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

        public X509ContentType GetCertContentType(byte[] rawData)
        {
            {
                ICertificatePal certPal;

                if (CertificatePal.TryReadX509Der(rawData, out certPal) ||
                    CertificatePal.TryReadX509Pem(rawData, out certPal))
                {
                    certPal.Dispose();

                    return X509ContentType.Cert;
                }
            }

            if (PkcsFormatReader.IsPkcs7(rawData))
            {
                return X509ContentType.Pkcs7;
            }

            {
                OpenSslPkcs12Reader pfx;

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
                    ICertificatePal certPal;

                    if (CertificatePal.TryReadX509Der(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);

                    if (CertificatePal.TryReadX509Pem(fileBio, out certPal))
                    {
                        certPal.Dispose();

                        return X509ContentType.Cert;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
                }

                // X509ContentType.Pkcs7
                {
                    if (PkcsFormatReader.IsPkcs7Der(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);

                    if (PkcsFormatReader.IsPkcs7Pem(fileBio))
                    {
                        return X509ContentType.Pkcs7;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
                }

                // X509ContentType.Pkcs12 (aka PFX)
                {
                    OpenSslPkcs12Reader pkcs12Reader;

                    if (OpenSslPkcs12Reader.TryRead(fileBio, out pkcs12Reader))
                    {
                        pkcs12Reader.Dispose();

                        return X509ContentType.Pkcs12;
                    }

                    CertificatePal.RewindBio(fileBio, bioPosition);
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
            OidCollection oids = new OidCollection();

            using (SafeEkuExtensionHandle eku = Interop.Crypto.DecodeExtendedKeyUsage(encoded, encoded.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(eku);

                int count = Interop.Crypto.GetX509EkuFieldCount(eku);

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

        private static RSA BuildRsaPublicKey(byte[] encodedData)
        {
            using (SafeRsaHandle rsaHandle = Interop.Crypto.DecodeRsaPublicKey(encodedData, encodedData.Length))
            {
                Interop.Crypto.CheckValidOpenSslHandle(rsaHandle);

                RSAParameters rsaParameters = Interop.Crypto.ExportRsaParameters(rsaHandle, false);
                RSA rsa = new RSAOpenSsl();
                rsa.ImportParameters(rsaParameters);
                return rsa;
            }
        }

        private static DSA BuildDsaPublicKey(byte[] encodedKey, byte[] encodedParameters)
        {
            // Dss-Parms ::= SEQUENCE { 
            //   p INTEGER, 
            //   q INTEGER, 
            //   g INTEGER 
            // } 

            // The encodedKey value is a DER INTEGER representing the Y value

            DerSequenceReader parametersReader = new DerSequenceReader(encodedParameters);
            DerSequenceReader keyReader = DerSequenceReader.CreateForPayload(encodedKey);

            DSAParameters parameters = new DSAParameters();

            // While this could use the object initializer, the read modifies the data stream, so
            // leaving these in flat call for clarity.
            parameters.P = parametersReader.ReadIntegerBytes();
            parameters.Q = parametersReader.ReadIntegerBytes();
            parameters.G = parametersReader.ReadIntegerBytes();
            parameters.Y = keyReader.ReadIntegerBytes();

            // Make the structure look like it would from Windows / .NET Framework
            TrimPaddingByte(ref parameters.P);
            TrimPaddingByte(ref parameters.Q);

            PadOrTrim(ref parameters.G, parameters.P.Length);
            PadOrTrim(ref parameters.Y, parameters.P.Length);

            DSA dsa = new DSAOpenSsl();
            dsa.ImportParameters(parameters);
            return dsa;
        }

        private static void TrimPaddingByte(ref byte[] data)
        {
            if (data.Length > 0 && data[0] == 0)
            {
                byte[] tmp = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, tmp, 0, tmp.Length);
                data = tmp;
            }
        }

        private static void PadOrTrim(ref byte[] data, int dataLen)
        {
            if (data.Length == dataLen)
                return;

            if (data.Length < dataLen)
            {
                // Add leading 0s
                byte[] tmp = new byte[dataLen];
                Buffer.BlockCopy(data, 0, tmp, dataLen - data.Length, dataLen);
                data = tmp;
                return;
            }

            if (data.Length == dataLen + 1 && data[0] == 0)
            {
                byte[] tmp = new byte[dataLen];
                Buffer.BlockCopy(data, 1, tmp, 0, dataLen);
                data = tmp;
                return;
            }

            throw new CryptographicException();
        }
    }
}
