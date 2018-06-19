// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_asn1_print.h"

c_static_assert(PAL_B_ASN1_NUMERICSTRING == B_ASN1_NUMERICSTRING);
c_static_assert(PAL_B_ASN1_PRINTABLESTRING == B_ASN1_PRINTABLESTRING);
c_static_assert(PAL_B_ASN1_T61STRING == B_ASN1_T61STRING);
c_static_assert(PAL_B_ASN1_VIDEOTEXSTRING == B_ASN1_VIDEOTEXSTRING);
c_static_assert(PAL_B_ASN1_IA5STRING == B_ASN1_IA5STRING);
c_static_assert(PAL_B_ASN1_GRAPHICSTRING == B_ASN1_GRAPHICSTRING);
c_static_assert(PAL_B_ASN1_VISIBLESTRING == B_ASN1_VISIBLESTRING);
c_static_assert(PAL_B_ASN1_GENERALSTRING == B_ASN1_GENERALSTRING);
c_static_assert(PAL_B_ASN1_UNIVERSALSTRING == B_ASN1_UNIVERSALSTRING);
c_static_assert(PAL_B_ASN1_OCTET_STRING == B_ASN1_OCTET_STRING);
c_static_assert(PAL_B_ASN1_BIT_STRING == B_ASN1_BIT_STRING);
c_static_assert(PAL_B_ASN1_BMPSTRING == B_ASN1_BMPSTRING);
c_static_assert(PAL_B_ASN1_UNKNOWN == B_ASN1_UNKNOWN);
c_static_assert(PAL_B_ASN1_UTF8STRING == B_ASN1_UTF8STRING);
c_static_assert(PAL_B_ASN1_UTCTIME == B_ASN1_UTCTIME);
c_static_assert(PAL_B_ASN1_GENERALIZEDTIME == B_ASN1_GENERALIZEDTIME);
c_static_assert(PAL_B_ASN1_SEQUENCE == B_ASN1_SEQUENCE);

c_static_assert(PAL_ASN1_STRFLGS_UTF8_CONVERT == ASN1_STRFLGS_UTF8_CONVERT);

ASN1_STRING* CryptoNative_DecodeAsn1TypeBytes(const uint8_t* buf, int32_t len, Asn1StringTypeFlags type)
{
    if (!buf || !len)
    {
        return NULL;
    }

    return d2i_ASN1_type_bytes(NULL, &buf, len, type);
}

int32_t CryptoNative_Asn1StringPrintEx(BIO* out, ASN1_STRING* str, Asn1StringPrintFlags flags)
{
    return ASN1_STRING_print_ex(out, str, flags);
}
