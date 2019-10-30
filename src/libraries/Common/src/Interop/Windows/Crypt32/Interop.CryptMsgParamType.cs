// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal enum CryptMsgParamType : int
        {
            CMSG_TYPE_PARAM = 1,
            CMSG_CONTENT_PARAM = 2,
            CMSG_BARE_CONTENT_PARAM = 3,
            CMSG_INNER_CONTENT_TYPE_PARAM = 4,
            CMSG_SIGNER_COUNT_PARAM = 5,
            CMSG_SIGNER_INFO_PARAM = 6,
            CMSG_SIGNER_CERT_INFO_PARAM = 7,
            CMSG_SIGNER_HASH_ALGORITHM_PARAM = 8,
            CMSG_SIGNER_AUTH_ATTR_PARAM = 9,
            CMSG_SIGNER_UNAUTH_ATTR_PARAM = 10,
            CMSG_CERT_COUNT_PARAM = 11,
            CMSG_CERT_PARAM = 12,
            CMSG_CRL_COUNT_PARAM = 13,
            CMSG_CRL_PARAM = 14,
            CMSG_ENVELOPE_ALGORITHM_PARAM = 15,
            CMSG_RECIPIENT_COUNT_PARAM = 17,
            CMSG_RECIPIENT_INDEX_PARAM = 18,
            CMSG_RECIPIENT_INFO_PARAM = 19,
            CMSG_HASH_ALGORITHM_PARAM = 20,
            CMSG_HASH_DATA_PARAM = 21,
            CMSG_COMPUTED_HASH_PARAM = 22,
            CMSG_ENCRYPT_PARAM = 26,
            CMSG_ENCRYPTED_DIGEST = 27,
            CMSG_ENCODED_SIGNER = 28,
            CMSG_ENCODED_MESSAGE = 29,
            CMSG_VERSION_PARAM = 30,
            CMSG_ATTR_CERT_COUNT_PARAM = 31,
            CMSG_ATTR_CERT_PARAM = 32,
            CMSG_CMS_RECIPIENT_COUNT_PARAM = 33,
            CMSG_CMS_RECIPIENT_INDEX_PARAM = 34,
            CMSG_CMS_RECIPIENT_ENCRYPTED_KEY_INDEX_PARAM = 35,
            CMSG_CMS_RECIPIENT_INFO_PARAM = 36,
            CMSG_UNPROTECTED_ATTR_PARAM = 37,
            CMSG_SIGNER_CERT_ID_PARAM = 38,
            CMSG_CMS_SIGNER_INFO_PARAM = 39,
        }
    }
}
