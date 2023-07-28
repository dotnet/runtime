// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_asn1.h"
#include <stdlib.h>
#include <stdbool.h>

c_static_assert(PAL_NID_undef == NID_undef);
c_static_assert(PAL_NID_X9_62_prime256v1 == NID_X9_62_prime256v1);
c_static_assert(PAL_NID_secp224r1 == NID_secp224r1);
c_static_assert(PAL_NID_secp384r1 == NID_secp384r1);
c_static_assert(PAL_NID_secp521r1 == NID_secp521r1);

const ASN1_OBJECT* CryptoNative_ObjTxt2Obj(const char* s)
{
    ERR_clear_error();
    return OBJ_txt2obj(s, true);
}

int32_t CryptoNative_ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a)
{
    ERR_clear_error();
    return OBJ_obj2txt(buf, buf_len, a, true);
}

const ASN1_OBJECT* CryptoNative_GetObjectDefinitionByName(const char* friendlyName)
{
    ERR_clear_error();

    // Neither ln2nid nor sn2nid can generate errors, but nid2obj can in certain circumstances.

    int nid = OBJ_ln2nid(friendlyName);

    if (nid == NID_undef)
    {
        nid = OBJ_sn2nid(friendlyName);
    }

    if (nid == NID_undef)
    {
        return NULL;
    }

    return OBJ_nid2obj(nid);
}

int32_t CryptoNative_ObjTxt2Nid(const char* sn)
{
    ERR_clear_error();
    return OBJ_txt2nid(sn);
}

const ASN1_OBJECT* CryptoNative_ObjNid2Obj(int32_t nid)
{
    ERR_clear_error();
    return OBJ_nid2obj(nid);
}

void CryptoNative_Asn1ObjectFree(ASN1_OBJECT* a)
{
    ASN1_OBJECT_free(a);
}

ASN1_BIT_STRING* CryptoNative_DecodeAsn1BitString(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    ERR_clear_error();

    return d2i_ASN1_BIT_STRING(NULL, &buf, len);
}

void CryptoNative_Asn1BitStringFree(ASN1_STRING* a)
{
    ASN1_BIT_STRING_free(a);
}

ASN1_OCTET_STRING* CryptoNative_Asn1OctetStringNew(void)
{
    ERR_clear_error();
    return ASN1_OCTET_STRING_new();
}

int32_t CryptoNative_Asn1OctetStringSet(ASN1_OCTET_STRING* s, const uint8_t* data, int32_t len)
{
    ERR_clear_error();
    return ASN1_OCTET_STRING_set(s, data, len);
}

void CryptoNative_Asn1OctetStringFree(ASN1_STRING* a)
{
    ASN1_OCTET_STRING_free(a);
}

int32_t CryptoNative_GetAsn1IntegerDerSize(ASN1_INTEGER* i)
{
    ERR_clear_error();
    return i2d_ASN1_INTEGER(i, NULL);
}

int32_t CryptoNative_EncodeAsn1Integer(ASN1_INTEGER* i, uint8_t* buf)
{
    ERR_clear_error();
    return i2d_ASN1_INTEGER(i, &buf);
}
