// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal const int CRYPT_FORMAT_STR_NONE       = 0;
        internal const int CRYPT_FORMAT_STR_MULTI_LINE = 0x00000001;
        internal const int CRYPT_FORMAT_STR_NO_HEX     = 0x00000010;

        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CryptFormatObject(
            int dwCertEncodingType,   // only valid value is X509_ASN_ENCODING
            int dwFormatType,         // unused - pass 0.
            int dwFormatStrType,      // select multiline
            IntPtr pFormatStruct,     // unused - pass IntPtr.Zero
            byte* lpszStructType,     // OID value
            byte[] pbEncoded,         // Data to be formatted
            int cbEncoded,            // Length of data to be formatted
            void* pbFormat,           // Receives formatted string.
            ref int pcbFormat);       // Sends/receives length of formatted string in bytes
    }
}
