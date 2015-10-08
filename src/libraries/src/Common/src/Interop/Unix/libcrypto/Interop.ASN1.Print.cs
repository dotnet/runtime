// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        private static unsafe extern SafeAsn1StringHandle d2i_ASN1_type_bytes(IntPtr zero, byte** ppin, int len, Asn1StringTypeFlags flags);

        [DllImport(Libraries.LibCrypto)]
        private static extern int ASN1_STRING_print_ex(SafeBioHandle bio, SafeAsn1StringHandle str, Asn1StringPrintFlags flags);

        internal static unsafe string DerStringToManagedString(
            byte[] derString,
            Asn1StringTypeFlags flags=AnyTextStringType)
        {
            SafeAsn1StringHandle asn1String = OpenSslD2I(
                (zero, ppin, len) => d2i_ASN1_type_bytes(zero, ppin, len, flags),
                derString,
                checkHandle: false);

            if (asn1String.IsInvalid)
            {
                return null;
            }

            byte[] utf8Bytes;

            using (asn1String)
            using (SafeBioHandle bio = BIO_new(BIO_s_mem()))
            {
                int len = ASN1_STRING_print_ex(bio, asn1String, Asn1StringPrintFlags.ASN1_STRFLGS_UTF8_CONVERT);

                if (len < 0)
                {
                    throw Crypto.CreateOpenSslCryptographicException();
                }

                int bioSize = GetMemoryBioSize(bio);
                utf8Bytes = new byte[bioSize + 1];

                int read = BIO_read(bio, utf8Bytes, utf8Bytes.Length);

                if (read < 0)
                {
                    throw Crypto.CreateOpenSslCryptographicException();
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

        // This is defined as an "unsigned long" in native code, so
        // really UIntPtr on Linux/OSX.
        [Flags]
        internal enum Asn1StringPrintFlags : ulong
        {
            ASN1_STRFLGS_UTF8_CONVERT = 0x10,
        }

        [Flags]
        internal enum Asn1StringTypeFlags
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


