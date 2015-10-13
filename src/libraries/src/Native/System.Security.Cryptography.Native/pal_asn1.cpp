// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_asn1.h"

#include <openssl/objects.h>

static_assert(PAL_NID_undef == NID_undef, "");
static_assert(PAL_NID_X9_62_prime256v1 == NID_X9_62_prime256v1, "");
static_assert(PAL_NID_secp224r1 == NID_secp224r1, "");
static_assert(PAL_NID_secp384r1 == NID_secp384r1, "");
static_assert(PAL_NID_secp521r1 == NID_secp521r1, "");

extern "C" int64_t Asn1IntegerGet(ASN1_INTEGER* a)
{
    return ASN1_INTEGER_get(a);
}

extern "C" const ASN1_OBJECT* ObjTxt2Obj(const char* s)
{
    return OBJ_txt2obj(s, true);
}

extern "C" int32_t ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a)
{
    return OBJ_obj2txt(buf, buf_len, a, true);
}

extern "C" const ASN1_OBJECT* GetObjectDefinitionByName(const char* friendlyName)
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

extern "C" int32_t ObjSn2Nid(const char* sn)
{
    return OBJ_sn2nid(sn);
}

extern "C" ASN1_OBJECT* ObjNid2Obj(int32_t nid)
{
    return OBJ_nid2obj(nid);
}

extern "C" void Asn1ObjectFree(ASN1_OBJECT* a)
{
    ASN1_OBJECT_free(a);
}

extern "C" ASN1_BIT_STRING* D2IAsn1BitString(ASN1_BIT_STRING** zero, const unsigned char** ppin, int32_t len)
{
    return d2i_ASN1_BIT_STRING(zero, ppin, len);
}

extern "C" void Asn1BitStringFree(ASN1_STRING* a)
{
    ASN1_BIT_STRING_free(a);
}

extern "C" ASN1_OCTET_STRING* D2IAsn1OctetString(ASN1_OCTET_STRING** zero, const unsigned char** ppin, int32_t len)
{
    return d2i_ASN1_OCTET_STRING(zero, ppin, len);
}

extern "C" ASN1_OCTET_STRING* Asn1OctetStringNew()
{
    return ASN1_OCTET_STRING_new();
}

extern "C" int32_t Asn1OctetStringSet(ASN1_OCTET_STRING* s, const unsigned char* data, int32_t len)
{
    return ASN1_OCTET_STRING_set(s, data, len);
}

extern "C" void Asn1OctetStringFree(ASN1_STRING* a)
{
    ASN1_OCTET_STRING_free(a);
}

extern "C" void Asn1StringFree(ASN1_STRING* a)
{
    ASN1_STRING_free(a);
}

