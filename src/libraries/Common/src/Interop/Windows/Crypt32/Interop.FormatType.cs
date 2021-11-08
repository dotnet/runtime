// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum FormatType : int
        {
            CERT_QUERY_FORMAT_BINARY = 1,
            CERT_QUERY_FORMAT_BASE64_ENCODED = 2,
            CERT_QUERY_FORMAT_ASN_ASCII_HEX_ENCODED = 3,
        }
    }
}
