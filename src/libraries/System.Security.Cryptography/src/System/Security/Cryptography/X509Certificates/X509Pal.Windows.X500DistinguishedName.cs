// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag)
        {
            int dwStrType = (int)(Interop.Crypt32.CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag));
            unsafe
            {
                fixed (byte* pbEncoded = encodedDistinguishedName)
                {
                    Interop.Crypt32.DATA_BLOB nameBlob;
                    nameBlob.cbData = (uint)encodedDistinguishedName.Length;
                    nameBlob.pbData = new IntPtr(pbEncoded);

                    int cchDecoded = Interop.Crypt32.CertNameToStr((int)Interop.Crypt32.CertEncodingType.All, &nameBlob, dwStrType, null, 0);
                    if (cchDecoded == 0)
                        throw ErrorCode.CERT_E_INVALID_NAME.ToCryptographicException();

                    Span<char> buffer = cchDecoded <= 256 ? stackalloc char[cchDecoded] : new char[cchDecoded];
                    fixed (char* ptr = buffer)
                    {
                        if (Interop.Crypt32.CertNameToStr((int)Interop.Crypt32.CertEncodingType.All, &nameBlob, dwStrType, ptr, cchDecoded) == 0)
                            throw ErrorCode.CERT_E_INVALID_NAME.ToCryptographicException();
                    }

                    return new string(buffer.Slice(0, cchDecoded - 1));
                }
            }
        }

        public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
        {
            Debug.Assert(distinguishedName != null);

            Interop.Crypt32.CertNameStrTypeAndFlags dwStrType = Interop.Crypt32.CertNameStrTypeAndFlags.CERT_X500_NAME_STR | MapNameToStrFlag(flag);

            int cbEncoded = 0;
            if (!Interop.Crypt32.CertStrToName(Interop.Crypt32.CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, null, ref cbEncoded, IntPtr.Zero))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encodedName = new byte[cbEncoded];
            if (!Interop.Crypt32.CertStrToName(Interop.Crypt32.CertEncodingType.All, distinguishedName, dwStrType, IntPtr.Zero, encodedName, ref cbEncoded, IntPtr.Zero))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encodedName;
        }

        public unsafe string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
        {
            if (encodedDistinguishedName == null || encodedDistinguishedName.Length == 0)
                return string.Empty;

            int stringType = multiLine ? Interop.Crypt32.CRYPT_FORMAT_STR_MULTI_LINE : Interop.Crypt32.CRYPT_FORMAT_STR_NONE;

            int cbFormat = 0;
            if (!Interop.Crypt32.CryptFormatObject(
                (int)Interop.Crypt32.CertEncodingType.X509_ASN_ENCODING,
                (int)FormatObjectType.None,
                stringType,
                IntPtr.Zero,
                (byte*)(int)FormatObjectStructType.X509_NAME,
                encodedDistinguishedName,
                encodedDistinguishedName.Length,
                null,
                ref cbFormat))
            {
                return encodedDistinguishedName.ToHexStringUpper();
            }

            int spanLength = (cbFormat + 1) / 2;
            Span<char> buffer = spanLength <= 256 ? stackalloc char[spanLength] : new char[spanLength];
            fixed (char* ptr = buffer)
            {
                if (!Interop.Crypt32.CryptFormatObject(
                    (int)Interop.Crypt32.CertEncodingType.X509_ASN_ENCODING,
                    (int)FormatObjectType.None,
                    stringType,
                    IntPtr.Zero,
                    (byte*)(int)FormatObjectStructType.X509_NAME,
                    encodedDistinguishedName,
                    encodedDistinguishedName.Length,
                    (byte*)ptr,
                    ref cbFormat))
                {
                    return encodedDistinguishedName.ToHexStringUpper();
                }
            }

            return new string(buffer.Slice(0, (cbFormat / 2) - 1));
        }

        private static Interop.Crypt32.CertNameStrTypeAndFlags MapNameToStrFlag(X500DistinguishedNameFlags flag)
        {
            // All values or'ed together. Change this if you add values to the enumeration.
            uint allFlags = 0x71F1;
            uint dwFlags = (uint)flag;
            Debug.Assert((dwFlags & ~allFlags) == 0);

            Interop.Crypt32.CertNameStrTypeAndFlags dwStrType = 0;
            if (dwFlags != 0)
            {
                if ((flag & X500DistinguishedNameFlags.Reversed) == X500DistinguishedNameFlags.Reversed)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_REVERSE_FLAG;

                if ((flag & X500DistinguishedNameFlags.UseSemicolons) == X500DistinguishedNameFlags.UseSemicolons)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_SEMICOLON_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseCommas) == X500DistinguishedNameFlags.UseCommas)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_COMMA_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseNewLines) == X500DistinguishedNameFlags.UseNewLines)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_CRLF_FLAG;

                if ((flag & X500DistinguishedNameFlags.DoNotUsePlusSign) == X500DistinguishedNameFlags.DoNotUsePlusSign)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_NO_PLUS_FLAG;
                if ((flag & X500DistinguishedNameFlags.DoNotUseQuotes) == X500DistinguishedNameFlags.DoNotUseQuotes)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_NO_QUOTING_FLAG;

                if ((flag & X500DistinguishedNameFlags.ForceUTF8Encoding) == X500DistinguishedNameFlags.ForceUTF8Encoding)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG;

                if ((flag & X500DistinguishedNameFlags.UseUTF8Encoding) == X500DistinguishedNameFlags.UseUTF8Encoding)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG;
                else if ((flag & X500DistinguishedNameFlags.UseT61Encoding) == X500DistinguishedNameFlags.UseT61Encoding)
                    dwStrType |= Interop.Crypt32.CertNameStrTypeAndFlags.CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG;
            }
            return dwStrType;
        }
    }
}
