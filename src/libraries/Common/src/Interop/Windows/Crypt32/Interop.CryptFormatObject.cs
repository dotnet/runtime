// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Crypt32
    {
        internal const int CRYPT_FORMAT_STR_NONE       = 0;
        internal const int CRYPT_FORMAT_STR_MULTI_LINE = 0x00000001;
        internal const int CRYPT_FORMAT_STR_NO_HEX     = 0x00000010;

        [DllImport(Libraries.Crypt32, SetLastError = true, BestFitMapping = false)]
        internal static extern unsafe bool CryptFormatObject(
            [In]      int dwCertEncodingType,   // only valid value is X509_ASN_ENCODING
            [In]      int dwFormatType,         // unused - pass 0.
            [In]      int dwFormatStrType,      // select multiline
            [In]      IntPtr pFormatStruct,     // unused - pass IntPtr.Zero
            [In]      byte* lpszStructType,     // OID value
            [In]      byte[] pbEncoded,         // Data to be formatted
            [In]      int cbEncoded,            // Length of data to be formatted
            [Out]     void* pbFormat,           // Receives formatted string.
            [In, Out] ref int pcbFormat);       // Sends/receives length of formatted string in bytes
    }
}
