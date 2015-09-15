// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public String X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag)
        {
            CertNameStrTypeAndFlags dwStrType = CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag);
            unsafe
            {
                fixed (byte* pbEncoded = encodedDistinguishedName)
                {
                    CRYPTOAPI_BLOB nameBlob;
                    nameBlob.cbData = encodedDistinguishedName.Length;
                    nameBlob.pbData = pbEncoded;

                    int cchDecoded = Interop.crypt32.CertNameToStr(CertEncodingType.All, ref nameBlob, dwStrType, null, 0);
                    if (cchDecoded == 0)
                        throw ErrorCode.CERT_E_INVALID_NAME.ToCryptographicException();

                    StringBuilder sb = new StringBuilder(cchDecoded);
                    if (Interop.crypt32.CertNameToStr(CertEncodingType.All, ref nameBlob, dwStrType, sb, cchDecoded) == 0)
                        throw ErrorCode.CERT_E_INVALID_NAME.ToCryptographicException();

                    return sb.ToString();
                }
            }
        }

        public byte[] X500DistinguishedNameEncode(String distinguishedName, X500DistinguishedNameFlags flag)
        {
            Debug.Assert(distinguishedName != null);

            CertNameStrTypeAndFlags dwStrType = CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag);

            int cbEncoded = 0;
            if (!Interop.crypt32.CertStrToName(CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, null, ref cbEncoded, IntPtr.Zero))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encodedName = new byte[cbEncoded];
            if (!Interop.crypt32.CertStrToName(CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, encodedName, ref cbEncoded, IntPtr.Zero))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encodedName;
        }

        public String X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
        {
            if (encodedDistinguishedName == null || encodedDistinguishedName.Length == 0)
                return String.Empty;

            FormatObjectStringType stringType = multiLine ? FormatObjectStringType.CRYPT_FORMAT_STR_MULTI_LINE : FormatObjectStringType.None;

            int cbFormat = 0;
            if (!Interop.crypt32.CryptFormatObject(
                CertEncodingType.X509_ASN_ENCODING,
                FormatObjectType.None,
                stringType,
                IntPtr.Zero,
                FormatObjectStructType.X509_NAME,
                encodedDistinguishedName,
                encodedDistinguishedName.Length,
                null,
                ref cbFormat))
            {
                return encodedDistinguishedName.ToHexStringUpper();
            }

            StringBuilder sb = new StringBuilder((cbFormat + 1) / 2);
            if (!Interop.crypt32.CryptFormatObject(
                CertEncodingType.X509_ASN_ENCODING,
                FormatObjectType.None,
                stringType,
                IntPtr.Zero,
                FormatObjectStructType.X509_NAME,
                encodedDistinguishedName,
                encodedDistinguishedName.Length,
                sb,
                ref cbFormat))
            {
                return encodedDistinguishedName.ToHexStringUpper();
            }

            return sb.ToString();
        }

        private static CertNameStrTypeAndFlags MapNameToStrFlag(X500DistinguishedNameFlags flag)
        {
            // All values or'ed together. Change this if you add values to the enumeration.
            uint allFlags = 0x71F1;
            uint dwFlags = (uint)flag;
            Debug.Assert((dwFlags & ~allFlags) == 0);

            CertNameStrTypeAndFlags dwStrType = 0;
            if (dwFlags != 0)
            {
                if ((flag & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_REVERSE_FLAG;

                if ((flag & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_SEMICOLON_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseCommas) == X500DistinguishedNameFlags.UseCommas)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_COMMA_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_CRLF_FLAG;

                if ((flag & X500DistinguishedNameFlags.DoNotUsePlusSign) == X500DistinguishedNameFlags.DoNotUsePlusSign)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_NO_PLUS_FLAG;
                if ((flag & X500DistinguishedNameFlags.DoNotUseQuotes) == X500DistinguishedNameFlags.DoNotUseQuotes)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_NO_QUOTING_FLAG;

                if ((flag & X500DistinguishedNameFlags.ForceUTF8Encoding) == X500DistinguishedNameFlags.ForceUTF8Encoding)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG;

                if ((flag & X500DistinguishedNameFlags.UseUTF8Encoding) == X500DistinguishedNameFlags.UseUTF8Encoding)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseT61Encoding) == X500DistinguishedNameFlags.UseT61Encoding)
                    dwStrType |= CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG;
            }
            return dwStrType;
        }
    }
}

