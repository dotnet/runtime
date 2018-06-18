// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Flags for the 'type' parameter of CryptoNative_DecodeAsn1TypeBytes.
*/
typedef enum
{
    PAL_B_ASN1_NUMERICSTRING = 0x0001,
    PAL_B_ASN1_PRINTABLESTRING = 0x0002,
    PAL_B_ASN1_T61STRING = 0x0004,
    PAL_B_ASN1_VIDEOTEXSTRING = 0x0008,
    PAL_B_ASN1_IA5STRING = 0x0010,
    PAL_B_ASN1_GRAPHICSTRING = 0x0020,
    PAL_B_ASN1_VISIBLESTRING = 0x0040,
    PAL_B_ASN1_GENERALSTRING = 0x0080,
    PAL_B_ASN1_UNIVERSALSTRING = 0x0100,
    PAL_B_ASN1_OCTET_STRING = 0x0200,
    PAL_B_ASN1_BIT_STRING = 0x0400,
    PAL_B_ASN1_BMPSTRING = 0x0800,
    PAL_B_ASN1_UNKNOWN = 0x1000,
    PAL_B_ASN1_UTF8STRING = 0x2000,
    PAL_B_ASN1_UTCTIME = 0x4000,
    PAL_B_ASN1_GENERALIZEDTIME = 0x8000,
    PAL_B_ASN1_SEQUENCE = 0x10000,
} Asn1StringTypeFlags;

/*
Flags for the 'flags' parameter of CryptoNative_Asn1StringPrintEx.
*/
typedef enum
{
    PAL_ASN1_STRFLGS_UTF8_CONVERT = 0x10,
} Asn1StringPrintFlags;

/*
Shims the d2i_ASN1_type_bytes method and makes it easier to invoke from managed code.
*/
DLLEXPORT ASN1_STRING* CryptoNative_DecodeAsn1TypeBytes(const uint8_t* buf, int32_t len, Asn1StringTypeFlags type);

/*
Direct shim to ASN1_STRING_print_ex.
*/
DLLEXPORT int32_t CryptoNative_Asn1StringPrintEx(BIO* out, ASN1_STRING* str, Asn1StringPrintFlags flags);
