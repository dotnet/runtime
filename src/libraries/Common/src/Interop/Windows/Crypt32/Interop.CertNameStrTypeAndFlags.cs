// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum CertNameStrTypeAndFlags : int
        {
            CERT_SIMPLE_NAME_STR = 1,
            CERT_OID_NAME_STR = 2,
            CERT_X500_NAME_STR = 3,

            CERT_NAME_STR_SEMICOLON_FLAG = 0x40000000,
            CERT_NAME_STR_NO_PLUS_FLAG = 0x20000000,
            CERT_NAME_STR_NO_QUOTING_FLAG = 0x10000000,
            CERT_NAME_STR_CRLF_FLAG = 0x08000000,
            CERT_NAME_STR_COMMA_FLAG = 0x04000000,
            CERT_NAME_STR_REVERSE_FLAG = 0x02000000,

            CERT_NAME_STR_DISABLE_IE4_UTF8_FLAG = 0x00010000,
            CERT_NAME_STR_ENABLE_T61_UNICODE_FLAG = 0x00020000,
            CERT_NAME_STR_ENABLE_UTF8_UNICODE_FLAG = 0x00040000,
            CERT_NAME_STR_FORCE_UTF8_DIR_STR_FLAG = 0x00080000,
        }
    }
}
