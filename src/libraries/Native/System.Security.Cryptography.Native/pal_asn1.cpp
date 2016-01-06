// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_asn1.h"

#include <openssl/objects.h>

static_assert(PAL_NID_undef == NID_undef, "");
static_assert(PAL_NID_X9_62_prime256v1 == NID_X9_62_prime256v1, "");
static_assert(PAL_NID_secp224r1 == NID_secp224r1, "");
static_assert(PAL_NID_secp384r1 == NID_secp384r1, "");
static_assert(PAL_NID_secp521r1 == NID_secp521r1, "");

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const ASN1_OBJECT* ObjTxt2Obj(const char* s)
{
    return CryptoNative_ObjTxt2Obj(s);
}

extern "C" const ASN1_OBJECT* CryptoNative_ObjTxt2Obj(const char* s)
{
    return OBJ_txt2obj(s, true);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a)
{
    return CryptoNative_ObjObj2Txt(buf, buf_len, a);
}

extern "C" int32_t CryptoNative_ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a)
{
    return OBJ_obj2txt(buf, buf_len, a, true);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const ASN1_OBJECT* GetObjectDefinitionByName(const char* friendlyName)
{
    return CryptoNative_GetObjectDefinitionByName(friendlyName);
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t ObjSn2Nid(const char* sn)
{
    return CryptoNative_ObjSn2Nid(sn);
}

extern "C" int32_t CryptoNative_ObjSn2Nid(const char* sn)
{
    return OBJ_sn2nid(sn);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_OBJECT* ObjNid2Obj(int32_t nid)
{
    return CryptoNative_ObjNid2Obj(nid);
}

extern "C" ASN1_OBJECT* CryptoNative_ObjNid2Obj(int32_t nid)
{
    return OBJ_nid2obj(nid);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void Asn1ObjectFree(ASN1_OBJECT* a)
{
    return CryptoNative_Asn1ObjectFree(a);
}

extern "C" void CryptoNative_Asn1ObjectFree(ASN1_OBJECT* a)
{
    ASN1_OBJECT_free(a);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_BIT_STRING* DecodeAsn1BitString(const uint8_t* buf, int32_t len)
{
    return CryptoNative_DecodeAsn1BitString(buf, len);
}

extern "C" ASN1_BIT_STRING* CryptoNative_DecodeAsn1BitString(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_ASN1_BIT_STRING(nullptr, &buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void Asn1BitStringFree(ASN1_STRING* a)
{
    return CryptoNative_Asn1BitStringFree(a);
}

extern "C" void CryptoNative_Asn1BitStringFree(ASN1_STRING* a)
{
    ASN1_BIT_STRING_free(a);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_OCTET_STRING* DecodeAsn1OctetString(const uint8_t* buf, int32_t len)
{
    return CryptoNative_DecodeAsn1OctetString(buf, len);
}

extern "C" ASN1_OCTET_STRING* CryptoNative_DecodeAsn1OctetString(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_ASN1_OCTET_STRING(nullptr, &buf, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" ASN1_OCTET_STRING* Asn1OctetStringNew()
{
    return CryptoNative_Asn1OctetStringNew();
}

extern "C" ASN1_OCTET_STRING* CryptoNative_Asn1OctetStringNew()
{
    return ASN1_OCTET_STRING_new();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t Asn1OctetStringSet(ASN1_OCTET_STRING* s, const uint8_t* data, int32_t len)
{
    return CryptoNative_Asn1OctetStringSet(s, data, len);
}

extern "C" int32_t CryptoNative_Asn1OctetStringSet(ASN1_OCTET_STRING* s, const uint8_t* data, int32_t len)
{
    return ASN1_OCTET_STRING_set(s, data, len);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void Asn1OctetStringFree(ASN1_STRING* a)
{
    return CryptoNative_Asn1OctetStringFree(a);
}

extern "C" void CryptoNative_Asn1OctetStringFree(ASN1_STRING* a)
{
    ASN1_OCTET_STRING_free(a);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void Asn1StringFree(ASN1_STRING* a)
{
    return CryptoNative_Asn1StringFree(a);
}

extern "C" void CryptoNative_Asn1StringFree(ASN1_STRING* a)
{
    ASN1_STRING_free(a);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetAsn1IntegerDerSize(ASN1_INTEGER* i)
{
    return CryptoNative_GetAsn1IntegerDerSize(i);
}

extern "C" int32_t CryptoNative_GetAsn1IntegerDerSize(ASN1_INTEGER* i)
{
    return i2d_ASN1_INTEGER(i, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EncodeAsn1Integer(ASN1_INTEGER* i, uint8_t* buf)
{
    return CryptoNative_EncodeAsn1Integer(i, buf);
}

extern "C" int32_t CryptoNative_EncodeAsn1Integer(ASN1_INTEGER* i, uint8_t* buf)
{
    return i2d_ASN1_INTEGER(i, &buf);
}
