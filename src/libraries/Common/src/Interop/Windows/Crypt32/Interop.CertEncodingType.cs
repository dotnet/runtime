// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertEncodingType : int
        {
            PKCS_7_ASN_ENCODING = 0x10000,
            X509_ASN_ENCODING   = 0x00001,

            All = PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
        }
    }
}
