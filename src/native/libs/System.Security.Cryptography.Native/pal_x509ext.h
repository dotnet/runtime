// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Creates an X509_EXTENSION with the given args.

Implemented by calling X509_EXTENSION_create_by_OBJ

Returns new X509_EXTENSION on success, nullptr on failure.
*/
PALEXPORT X509_EXTENSION*
CryptoNative_X509ExtensionCreateByObj(ASN1_OBJECT* obj, int32_t isCritical, ASN1_OCTET_STRING* data);

/*
Cleans up and deletes an X509_EXTENSION instance.

Implemented by calling X509_EXTENSION_free.

No-op if a is null.
The given X509_EXTENSION pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_X509ExtensionDestroy(X509_EXTENSION* a);

/*
Shims the X509V3_EXT_print method.

Returns 1 on success, otherwise 0 if there was an error.
*/
PALEXPORT int32_t CryptoNative_X509V3ExtPrint(BIO* out, X509_EXTENSION* ext);

/*
Decodes the X509 BASIC_CONSTRAINTS information and fills the out variables:
1. bool certificateAuthority
2. bool hasPathLengthConstraint
3. int32_t pathLengthConstraint

Returns 1 if the BASIC_CONSTRAINTS information was successfully decoded,
otherwise 0.
*/
PALEXPORT int32_t CryptoNative_DecodeX509BasicConstraints2Extension(const uint8_t* encoded,
                                                                     int32_t encodedLength,
                                                                     int32_t* certificateAuthority,
                                                                     int32_t* hasPathLengthConstraint,
                                                                     int32_t* pathLengthConstraint);

/*
Shims the d2i_EXTENDED_KEY_USAGE method and makes it easier to invoke from managed code.
*/
PALEXPORT EXTENDED_KEY_USAGE* CryptoNative_DecodeExtendedKeyUsage(const uint8_t* buf, int32_t len);

/*
Cleans up and deletes an EXTENDED_KEY_USAGE instance.

Implemented by calling EXTENDED_KEY_USAGE_free.

No-op if a is null.
The given EXTENDED_KEY_USAGE pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_ExtendedKeyUsageDestroy(EXTENDED_KEY_USAGE* a);
