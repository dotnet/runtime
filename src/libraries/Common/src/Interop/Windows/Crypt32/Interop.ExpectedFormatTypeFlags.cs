// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [Flags]
        internal enum ExpectedFormatTypeFlags : int
        {
            CERT_QUERY_FORMAT_FLAG_BINARY = 1 << FormatType.CERT_QUERY_FORMAT_BINARY,
            CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_BASE64_ENCODED,
            CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED = 1 << FormatType.CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED,

            CERT_QUERY_FORMAT_FLAG_ALL = CERT_QUERY_FORMAT_FLAG_BINARY | CERT_QUERY_FORMAT_FLAG_BASE64_ENCODED | CERT_QUERY_FORMAT_FLAG_ASN_ASCII_HEX_ENCODED,
        }
    }
}
