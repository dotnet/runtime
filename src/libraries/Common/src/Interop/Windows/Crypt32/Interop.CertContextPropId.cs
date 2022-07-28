// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CertContextPropId : int
        {
            CERT_KEY_PROV_INFO_PROP_ID = 2,
            CERT_SHA1_HASH_PROP_ID = 3,
            CERT_KEY_CONTEXT_PROP_ID = 5,
            CERT_FRIENDLY_NAME_PROP_ID = 11,
            CERT_ARCHIVED_PROP_ID = 19,
            CERT_KEY_IDENTIFIER_PROP_ID = 20,
            CERT_PUBKEY_ALG_PARA_PROP_ID = 22,
            CERT_OCSP_RESPONSE_PROP_ID = 70,
            CERT_NCRYPT_KEY_HANDLE_PROP_ID = 78,
            CERT_DELETE_KEYSET_PROP_ID = 101,
            CERT_CLR_DELETE_KEY_PROP_ID = 125,
        }
    }
}
