// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_asn1_print.h"

static_assert(PAL_B_ASN1_NUMERICSTRING == B_ASN1_NUMERICSTRING, "");
static_assert(PAL_B_ASN1_PRINTABLESTRING == B_ASN1_PRINTABLESTRING, "");
static_assert(PAL_B_ASN1_T61STRING == B_ASN1_T61STRING, "");
static_assert(PAL_B_ASN1_VIDEOTEXSTRING == B_ASN1_VIDEOTEXSTRING, "");
static_assert(PAL_B_ASN1_IA5STRING == B_ASN1_IA5STRING, "");
static_assert(PAL_B_ASN1_GRAPHICSTRING == B_ASN1_GRAPHICSTRING, "");
static_assert(PAL_B_ASN1_VISIBLESTRING == B_ASN1_VISIBLESTRING, "");
static_assert(PAL_B_ASN1_GENERALSTRING == B_ASN1_GENERALSTRING, "");
static_assert(PAL_B_ASN1_UNIVERSALSTRING == B_ASN1_UNIVERSALSTRING, "");
static_assert(PAL_B_ASN1_OCTET_STRING == B_ASN1_OCTET_STRING, "");
static_assert(PAL_B_ASN1_BIT_STRING == B_ASN1_BIT_STRING, "");
static_assert(PAL_B_ASN1_BMPSTRING == B_ASN1_BMPSTRING, "");
static_assert(PAL_B_ASN1_UNKNOWN == B_ASN1_UNKNOWN, "");
static_assert(PAL_B_ASN1_UTF8STRING == B_ASN1_UTF8STRING, "");
static_assert(PAL_B_ASN1_UTCTIME == B_ASN1_UTCTIME, "");
static_assert(PAL_B_ASN1_GENERALIZEDTIME == B_ASN1_GENERALIZEDTIME, "");
static_assert(PAL_B_ASN1_SEQUENCE == B_ASN1_SEQUENCE, "");

static_assert(PAL_ASN1_STRFLGS_UTF8_CONVERT == ASN1_STRFLGS_UTF8_CONVERT, "");

extern "C" ASN1_STRING* CryptoNative_DecodeAsn1TypeBytes(const uint8_t* buf, int32_t len, Asn1StringTypeFlags type)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_ASN1_type_bytes(nullptr, &buf, len, type);
}

extern "C" int32_t CryptoNative_Asn1StringPrintEx(BIO* out, ASN1_STRING* str, Asn1StringPrintFlags flags)
{
    return ASN1_STRING_print_ex(out, str, flags);
}
