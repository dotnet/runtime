// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeAsn1TypeBytes")]
        private static extern SafeAsn1StringHandle DecodeAsn1TypeBytes(byte[] buf, int len, Asn1StringTypeFlags flags);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1StringPrintEx")]
        private static extern int Asn1StringPrintEx(SafeBioHandle bio, SafeAsn1StringHandle str, Asn1StringPrintFlags flags);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1StringPrintEx")]
        private static extern int Asn1StringPrintEx(SafeBioHandle bio, SafeSharedAsn1StringHandle str, Asn1StringPrintFlags flags);

        internal static string DerStringToManagedString(byte[] derString)
        {
            SafeAsn1StringHandle asn1String = DecodeAsn1TypeBytes(derString, derString.Length, AnyTextStringType);

            if (asn1String.IsInvalid)
            {
                return null;
            }

            using (asn1String)
            {
                return Asn1StringToManagedString(
                    asn1String,
                    (bio, str, flags) => Asn1StringPrintEx(bio, str, flags));
            }
        }

        internal static string Asn1StringToManagedString(SafeSharedAsn1StringHandle asn1String)
        {
            CheckValidOpenSslHandle(asn1String);

            return Asn1StringToManagedString(
                asn1String,
                (bio, str, flags) => Asn1StringPrintEx(bio, str, flags));
        }

        private static string Asn1StringToManagedString<THandle>(
            THandle asn1String,
            Func<SafeBioHandle, THandle, Asn1StringPrintFlags, int> asn1StringPrintEx)
        {
            byte[] utf8Bytes;

            using (SafeBioHandle bio = CreateMemoryBio())
            {
                int len = asn1StringPrintEx(bio, asn1String, Asn1StringPrintFlags.ASN1_STRFLGS_UTF8_CONVERT);

                if (len < 0)
                {
                    throw CreateOpenSslCryptographicException();
                }

                if (len == 0)
                {
                    return "";
                }

                int bioSize = GetMemoryBioSize(bio);
                utf8Bytes = new byte[bioSize + 1];

                int read = BioRead(bio, utf8Bytes, utf8Bytes.Length);

                if (read < 0)
                {
                    throw CreateOpenSslCryptographicException();
                }
            }

            int nonNullCount = utf8Bytes.Length;

            if (utf8Bytes[utf8Bytes.Length - 1] == 0)
            {
                for (int i = utf8Bytes.Length - 1; i >= 0; i--)
                {
                    if (utf8Bytes[i] != 0)
                    {
                        break;
                    }

                    nonNullCount = i;
                }
            }

            return Encoding.UTF8.GetString(utf8Bytes, 0, nonNullCount);
        }

        [Flags]
        private enum Asn1StringPrintFlags : ulong
        {
            ASN1_STRFLGS_UTF8_CONVERT = 0x10,
        }

        [Flags]
        private enum Asn1StringTypeFlags
        {
            B_ASN1_NUMERICSTRING = 0x0001,
            B_ASN1_PRINTABLESTRING = 0x0002,
            B_ASN1_T61STRING = 0x0004,
            B_ASN1_VIDEOTEXSTRING = 0x0008,
            B_ASN1_IA5STRING = 0x0010,
            B_ASN1_GRAPHICSTRING = 0x0020,
            B_ASN1_VISIBLESTRING = 0x0040,
            B_ASN1_GENERALSTRING = 0x0080,
            B_ASN1_UNIVERSALSTRING = 0x0100,
            B_ASN1_OCTET_STRING = 0x0200,
            B_ASN1_BIT_STRING = 0x0400,
            B_ASN1_BMPSTRING = 0x0800,
            B_ASN1_UNKNOWN = 0x1000,
            B_ASN1_UTF8STRING = 0x2000,
            B_ASN1_UTCTIME = 0x4000,
            B_ASN1_GENERALIZEDTIME = 0x8000,
            B_ASN1_SEQUENCE = 0x10000,
        }

        private const Asn1StringTypeFlags AnyTextStringType =
            Asn1StringTypeFlags.B_ASN1_NUMERICSTRING |
            Asn1StringTypeFlags.B_ASN1_PRINTABLESTRING |
            Asn1StringTypeFlags.B_ASN1_T61STRING |
            Asn1StringTypeFlags.B_ASN1_VIDEOTEXSTRING |
            Asn1StringTypeFlags.B_ASN1_IA5STRING |
            Asn1StringTypeFlags.B_ASN1_GRAPHICSTRING |
            Asn1StringTypeFlags.B_ASN1_VISIBLESTRING |
            Asn1StringTypeFlags.B_ASN1_GENERALSTRING |
            Asn1StringTypeFlags.B_ASN1_UNIVERSALSTRING |
            Asn1StringTypeFlags.B_ASN1_BMPSTRING |
            Asn1StringTypeFlags.B_ASN1_UTF8STRING |
            Asn1StringTypeFlags.B_ASN1_UTCTIME |
            Asn1StringTypeFlags.B_ASN1_GENERALIZEDTIME;
    }
}
