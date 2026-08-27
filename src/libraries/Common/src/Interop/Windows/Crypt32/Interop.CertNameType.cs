// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertNameType : int
        {
            CERT_NAME_EMAIL_TYPE = 1,
            CERT_NAME_RDN_TYPE = 2,
            CERT_NAME_ATTR_TYPE = 3,
            CERT_NAME_SIMPLE_DISPLAY_TYPE = 4,
            CERT_NAME_FRIENDLY_DISPLAY_TYPE = 5,
            CERT_NAME_DNS_TYPE = 6,
            CERT_NAME_URL_TYPE = 7,
            CERT_NAME_UPN_TYPE = 8,
        }
    }
}
