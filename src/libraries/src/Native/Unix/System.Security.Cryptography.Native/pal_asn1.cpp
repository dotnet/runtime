// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_asn1.h"

static_assert(PAL_NID_undef == NID_undef, "");
static_assert(PAL_NID_X9_62_prime256v1 == NID_X9_62_prime256v1, "");
static_assert(PAL_NID_secp224r1 == NID_secp224r1, "");
static_assert(PAL_NID_secp384r1 == NID_secp384r1, "");
static_assert(PAL_NID_secp521r1 == NID_secp521r1, "");

extern "C" const ASN1_OBJECT* CryptoNative_ObjTxt2Obj(const char* s)
{
    return OBJ_txt2obj(s, true);
}

extern "C" int32_t CryptoNative_ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a)
{
    return OBJ_obj2txt(buf, buf_len, a, true);
}

extern "C" const ASN1_OBJECT* CryptoNative_GetObjectDefinitionByName(const char* friendlyName)
{
    int nid = OBJ_ln2nid(friendlyName);

    if (nid == NID_undef)
    {
        nid = OBJ_sn2nid(friendlyName);
    }

    if (nid == NID_undef)
    {
        return nullptr;
    }

    return OBJ_nid2obj(nid);
}

extern "C" int32_t CryptoNative_ObjSn2Nid(const char* sn)
{
    return OBJ_sn2nid(sn);
}

extern "C" ASN1_OBJECT* CryptoNative_ObjNid2Obj(int32_t nid)
{
    return OBJ_nid2obj(nid);
}

extern "C" void CryptoNative_Asn1ObjectFree(ASN1_OBJECT* a)
{
    ASN1_OBJECT_free(a);
}

extern "C" ASN1_BIT_STRING* CryptoNative_DecodeAsn1BitString(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_ASN1_BIT_STRING(nullptr, &buf, len);
}

extern "C" void CryptoNative_Asn1BitStringFree(ASN1_STRING* a)
{
    ASN1_BIT_STRING_free(a);
}

extern "C" ASN1_OCTET_STRING* CryptoNative_DecodeAsn1OctetString(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_ASN1_OCTET_STRING(nullptr, &buf, len);
}

extern "C" ASN1_OCTET_STRING* CryptoNative_Asn1OctetStringNew()
{
    return ASN1_OCTET_STRING_new();
}

extern "C" int32_t CryptoNative_Asn1OctetStringSet(ASN1_OCTET_STRING* s, const uint8_t* data, int32_t len)
{
    return ASN1_OCTET_STRING_set(s, data, len);
}

extern "C" void CryptoNative_Asn1OctetStringFree(ASN1_STRING* a)
{
    ASN1_OCTET_STRING_free(a);
}

extern "C" void CryptoNative_Asn1StringFree(ASN1_STRING* a)
{
    ASN1_STRING_free(a);
}

extern "C" int32_t CryptoNative_GetAsn1IntegerDerSize(ASN1_INTEGER* i)
{
    return i2d_ASN1_INTEGER(i, nullptr);
}

extern "C" int32_t CryptoNative_EncodeAsn1Integer(ASN1_INTEGER* i, uint8_t* buf)
{
    return i2d_ASN1_INTEGER(i, &buf);
}
