// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "opensslshim.h"

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
Direct shim to OBJ_txt2obj.
*/
extern "C" const ASN1_OBJECT* CryptoNative_ObjTxt2Obj(const char* s);

/*
Direct shim to OBJ_obj2txt.
*/
extern "C" int32_t CryptoNative_ObjObj2Txt(char* buf, int32_t buf_len, const ASN1_OBJECT* a);

/*
Retrieves the ASN1_OBJECT for the specified friendly name.

Can return nullptr if there isn't a corresponding shared object.
*/
extern "C" const ASN1_OBJECT* CryptoNative_GetObjectDefinitionByName(const char* friendlyName);

/*
Direct shim to OBJ_sn2nid.
*/
extern "C" int32_t CryptoNative_ObjSn2Nid(const char* sn);

/*
Direct shim to OBJ_nid2obj.
*/
extern "C" ASN1_OBJECT* CryptoNative_ObjNid2Obj(int32_t nid);

/*
Direct shim to ASN1_OBJECT_free.
*/
extern "C" void CryptoNative_Asn1ObjectFree(ASN1_OBJECT* a);

/*
Shims the d2i_ASN1_BIT_STRING method and makes it easier to invoke from managed code.
*/
extern "C" ASN1_BIT_STRING* CryptoNative_DecodeAsn1BitString(const uint8_t* buf, int32_t len);

/*
Direct shim to ASN1_BIT_STRING_free.
*/
extern "C" void CryptoNative_Asn1BitStringFree(ASN1_STRING* a);

/*
Shims the d2i_ASN1_OCTET_STRING method and makes it easier to invoke from managed code.
*/
extern "C" ASN1_OCTET_STRING* CryptoNative_DecodeAsn1OctetString(const uint8_t* buf, int32_t len);

/*
Direct shim to ASN1_OCTET_STRING_new.
*/
extern "C" ASN1_OCTET_STRING* CryptoNative_Asn1OctetStringNew();

/*
Direct shim to ASN1_OCTET_STRING_set.
*/
extern "C" int32_t CryptoNative_Asn1OctetStringSet(ASN1_OCTET_STRING* s, const uint8_t* data, int32_t len);

/*
Direct shim to ASN1_OCTET_STRING_free.
*/
extern "C" void CryptoNative_Asn1OctetStringFree(ASN1_STRING* a);

/*
Direct shim to ASN1_STRING_free.
*/
extern "C" void CryptoNative_Asn1StringFree(ASN1_STRING* a);

/*
Returns the number of bytes it will take to convert
the ASN1_INTEGER to a DER format.
*/
extern "C" int32_t CryptoNative_GetAsn1IntegerDerSize(ASN1_INTEGER* i);

/*
Shims the i2d_ASN1_INTEGER method.

Returns the number of bytes written to buf.
*/
extern "C" int32_t CryptoNative_EncodeAsn1Integer(ASN1_INTEGER* i, uint8_t* buf);
