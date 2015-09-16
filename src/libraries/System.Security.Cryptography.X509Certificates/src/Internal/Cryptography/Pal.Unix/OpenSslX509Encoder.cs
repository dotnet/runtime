// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal class OpenSslX509Encoder : IX509Pal
    {
        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal certificatePal)
        {
            switch (oid.Value)
            {
                case Oids.RsaRsa:
                    return BuildRsaPublicKey(encodedKeyValue);
            }

            // NotSupportedException is what desktop and CoreFx-Windows throw in this situation.
            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public unsafe string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag)
        {
            using (SafeX509NameHandle x509Name = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_X509_NAME, encodedDistinguishedName))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(x509Name);

                using (SafeBioHandle bioHandle = Interop.libcrypto.BIO_new(Interop.libcrypto.BIO_s_mem()))
                {
                    Interop.libcrypto.CheckValidOpenSslHandle(bioHandle);
                    
                    OpenSslX09NameFormatFlags nativeFlags = ConvertFormatFlags(flag);

                    int written = Interop.libcrypto.X509_NAME_print_ex(
                        bioHandle,
                        x509Name,
                        0,
                        new UIntPtr((uint)nativeFlags));

                    // X509_NAME_print_ex returns how many bytes were written into the buffer.
                    // BIO_gets wants to ensure that the response is NULL-terminated.
                    // So add one to leave space for the NULL.
                    StringBuilder builder = new StringBuilder(written + 1);
                    int read = Interop.libcrypto.BIO_gets(bioHandle, builder, builder.Capacity);

                    if (read < 0)
                    {
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();
                    }

                    return builder.ToString();
                }
            }
        }

        public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
        {
            throw new NotImplementedException();
        }

        public string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
        {
            throw new NotImplementedException();
        }

        public X509ContentType GetCertContentType(byte[] rawData)
        {
            throw new NotImplementedException();
        }

        public X509ContentType GetCertContentType(string fileName)
        {
            throw new NotImplementedException();
        }

        public byte[] EncodeX509KeyUsageExtension(X509KeyUsageFlags keyUsages)
        {
            throw new NotImplementedException();
        }

        public unsafe void DecodeX509KeyUsageExtension(byte[] encoded, out X509KeyUsageFlags keyUsages)
        {
            using (SafeAsn1BitStringHandle bitString = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_ASN1_BIT_STRING, encoded))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(bitString);

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

        public byte[] EncodeX509BasicConstraints2Extension(
            bool certificateAuthority,
            bool hasPathLengthConstraint,
            int pathLengthConstraint)
        {
            throw new NotImplementedException();
        }

        public void DecodeX509BasicConstraintsExtension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            throw new NotImplementedException();
        }

        public unsafe void DecodeX509BasicConstraints2Extension(
            byte[] encoded,
            out bool certificateAuthority,
            out bool hasPathLengthConstraint,
            out int pathLengthConstraint)
        {
            using (SafeBasicConstraintsHandle constraints = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_BASIC_CONSTRAINTS, encoded))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(constraints);

                Interop.libcrypto.BASIC_CONSTRAINTS* data = (Interop.libcrypto.BASIC_CONSTRAINTS*)constraints.DangerousGetHandle();
                certificateAuthority = data->CA != 0;

                if (data->pathlen != IntPtr.Zero)
                {
                    hasPathLengthConstraint = true;
                    pathLengthConstraint = Interop.libcrypto.ASN1_INTEGER_get(data->pathlen).ToInt32();
                }
                else
                {
                    hasPathLengthConstraint = false;
                    pathLengthConstraint = 0;
                }
            }
        }

        public byte[] EncodeX509EnhancedKeyUsageExtension(OidCollection usages)
        {
            throw new NotImplementedException();
        }

        public unsafe void DecodeX509EnhancedKeyUsageExtension(byte[] encoded, out OidCollection usages)
        {
            OidCollection oids = new OidCollection();

            using (SafeEkuExtensionHandle eku = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_EXTENDED_KEY_USAGE, encoded))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(eku);

                int count = Interop.Crypto.GetX509EkuFieldCount(eku);

                for (int i = 0; i < count; i++)
                {
                    IntPtr oidPtr = Interop.Crypto.GetX509EkuField(eku, i);

                    if (oidPtr == IntPtr.Zero)
                    {
                        throw Interop.libcrypto.CreateOpenSslCryptographicException();
                    }

                    string oidValue = Interop.libcrypto.OBJ_obj2txt_helper(oidPtr);

                    oids.Add(new Oid(oidValue));
                }
            }

            usages = oids;
        }

        public byte[] EncodeX509SubjectKeyIdentifierExtension(byte[] subjectKeyIdentifier)
        {
            throw new NotImplementedException();
        }

        public unsafe void DecodeX509SubjectKeyIdentifierExtension(byte[] encoded, out byte[] subjectKeyIdentifier)
        {
            using (SafeAsn1OctetStringHandle octetString = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_ASN1_OCTET_STRING, encoded))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(octetString);

                subjectKeyIdentifier = Interop.Crypto.GetAsn1StringBytes(octetString.DangerousGetHandle());
            }
        }

        public byte[] ComputeCapiSha1OfPublicKey(PublicKey key)
        {
            throw new NotImplementedException();
        }

        private static OpenSslX09NameFormatFlags ConvertFormatFlags(X500DistinguishedNameFlags inFlags)
        {
            OpenSslX09NameFormatFlags outFlags = 0;

            if (inFlags.HasFlag(X500DistinguishedNameFlags.Reversed))
            {
                outFlags |= OpenSslX09NameFormatFlags.XN_FLAG_DN_REV;
            }

            if (inFlags.HasFlag(X500DistinguishedNameFlags.UseSemicolons))
            {
                outFlags |= OpenSslX09NameFormatFlags.XN_FLAG_SEP_SPLUS_SPC;
            }
            else if (inFlags.HasFlag(X500DistinguishedNameFlags.UseNewLines))
            {
                outFlags |= OpenSslX09NameFormatFlags.XN_FLAG_SEP_MULTILINE;
            }
            else
            {
                outFlags |= OpenSslX09NameFormatFlags.XN_FLAG_SEP_CPLUS_SPC;
            }

            if (inFlags.HasFlag(X500DistinguishedNameFlags.DoNotUseQuotes))
            {
                // TODO: Handle this.
            }

            if (inFlags.HasFlag(X500DistinguishedNameFlags.ForceUTF8Encoding))
            {
                // TODO: Handle this.
            }

            if (inFlags.HasFlag(X500DistinguishedNameFlags.UseUTF8Encoding))
            {
                // TODO: Handle this.
            }
            else if (inFlags.HasFlag(X500DistinguishedNameFlags.UseT61Encoding))
            {
                // TODO: Handle this.
            }

            return outFlags;
        }

        private static unsafe RSA BuildRsaPublicKey(byte[] encodedData)
        {
            using (SafeRsaHandle rsaHandle = Interop.libcrypto.OpenSslD2I(Interop.libcrypto.d2i_RSAPublicKey, encodedData))
            {
                Interop.libcrypto.CheckValidOpenSslHandle(rsaHandle);

                RSAParameters rsaParameters = Interop.libcrypto.ExportRsaParameters(rsaHandle, false);
                RSA rsa = new RSAOpenSsl();
                rsa.ImportParameters(rsaParameters);
                return rsa;
            }
        }

        [Flags]
        private enum OpenSslX09NameFormatFlags : uint
        {
            XN_FLAG_SEP_MASK = (0xf << 16),

            // O=Apache HTTP Server, OU=Test Certificate, CN=localhost
            // Note that this is the only not-reversed value, since XN_FLAG_COMPAT | XN_FLAG_DN_REV produces nothing.
            XN_FLAG_COMPAT = 0,

            // CN=localhost,OU=Test Certificate,O=Apache HTTP Server
            XN_FLAG_SEP_COMMA_PLUS = (1 << 16),

            // CN=localhost, OU=Test Certificate, O=Apache HTTP Server
            XN_FLAG_SEP_CPLUS_SPC = (2 << 16),

            // CN=localhost; OU=Test Certificate; O=Apache HTTP Server
            XN_FLAG_SEP_SPLUS_SPC = (3 << 16),

            // CN=localhost
            // OU=Test Certificate
            // O=Apache HTTP Server
            XN_FLAG_SEP_MULTILINE = (4 << 16),

            XN_FLAG_DN_REV = (1 << 20),

            XN_FLAG_FN_MASK = (0x3 << 21),

            // CN=localhost, OU=Test Certificate, O=Apache HTTP Server
            XN_FLAG_FN_SN = 0,

            // commonName=localhost, organizationalUnitName=Test Certificate, organizationName=Apache HTTP Server
            XN_FLAG_FN_LN = (1 << 21),

            // 2.5.4.3=localhost, 2.5.4.11=Test Certificate, 2.5.4.10=Apache HTTP Server
            XN_FLAG_FN_OID = (2 << 21),

            // localhost, Test Certificate, Apache HTTP Server
            XN_FLAG_FN_NONE = (3 << 21),

            // CN = localhost, OU = Test Certificate, O = Apache HTTP Server
            XN_FLAG_SPC_EQ = (1 << 23),

            XN_FLAG_DUMP_UNKNOWN_FIELDS = (1 << 24),

            XN_FLAG_FN_ALIGN = (1 << 25),
        }
    }
}
