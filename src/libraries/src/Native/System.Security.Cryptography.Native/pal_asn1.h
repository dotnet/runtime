// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/asn1.h>

/*
NID values that are used in managed code.
*/
enum SupportedAlgorithmNids
{
    PAL_NID_undef = 0,
    PAL_NID_X9_62_prime256v1 = 415,
    PAL_NID_secp224r1 = 713,
    PAL_NID_secp384r1 = 715,
    PAL_NID_secp521r1 = 716,
};

/*
Direct shim to ASN1_INTEGER_get.
*/
extern "C" int64_t Asn1IntegerGet(ASN1_INTEGER* a);

/*
Direct shim to OBJ_txt2obj.
*/
extern "C" const ASN1_OBJECT* ObjTxt2Obj(const char* s);

/*
Direct shim to OBJ_obj2txt.
*/
extern "C" int32_t ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a);

/*
Retrieves the ASN1_OBJECT for the specified friendly name.

Can return nullptr if there isn't a corresponding shared object.
*/
extern "C" const ASN1_OBJECT* GetObjectDefinitionByName(const char* friendlyName);

/*
Direct shim to OBJ_sn2nid.
*/
extern "C" int32_t ObjSn2Nid(const char* sn);

/*
Direct shim to OBJ_nid2obj.
*/
extern "C" ASN1_OBJECT* ObjNid2Obj(int32_t nid);

/*
Direct shim to ASN1_OBJECT_free.
*/
extern "C" void Asn1ObjectFree(ASN1_OBJECT* a);

/*
Direct shim to d2i_ASN1_BIT_STRING.
*/
extern "C" ASN1_BIT_STRING* D2IAsn1BitString(ASN1_BIT_STRING** zero, const unsigned char** ppin, int32_t len);

/*
Direct shim to ASN1_BIT_STRING_free.
*/
extern "C" void Asn1BitStringFree(ASN1_STRING* a);

/*
Direct shim to d2i_ASN1_OCTET_STRING.
*/
extern "C" ASN1_OCTET_STRING* D2IAsn1OctetString(ASN1_OCTET_STRING** zero, const unsigned char** ppin, int32_t len);

/*
Direct shim to ASN1_OCTET_STRING_new.
*/
extern "C" ASN1_OCTET_STRING* Asn1OctetStringNew();

/*
Direct shim to ASN1_OCTET_STRING_set.
*/
extern "C" int32_t Asn1OctetStringSet(ASN1_OCTET_STRING* s, const unsigned char* data, int32_t len);

/*
Direct shim to ASN1_OCTET_STRING_free.
*/
extern "C" void Asn1OctetStringFree(ASN1_STRING* a);

/*
Direct shim to ASN1_STRING_free.
*/
extern "C" void Asn1StringFree(ASN1_STRING* a);

