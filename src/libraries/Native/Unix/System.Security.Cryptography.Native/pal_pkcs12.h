// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_crypto_types.h"
#include "opensslshim.h"

/*
Shims the d2i_PKCS12 method and makes it easier to invoke from managed code.
*/
extern "C" PKCS12* CryptoNative_DecodePkcs12(const uint8_t* buf, int32_t len);

/*
Shims the d2i_PKCS12_bio method.

Returns the new PKCS12 instance.
*/
extern "C" PKCS12* CryptoNative_DecodePkcs12FromBio(BIO* bio);

/*
Cleans up and deletes a PKCS12 instance.

Implemented by calling PKCS12_free.

No-op if p12 is null.
The given PKCS12 pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_Pkcs12Destroy(PKCS12* p12);

/*
Shims the PKCS12_create method.

Returns the new PKCS12 instance.
*/
extern "C" PKCS12* CryptoNative_Pkcs12Create(char* pass, EVP_PKEY* pkey, X509* cert, X509Stack* ca);

/*
Returns the number of bytes it will take to convert
the PKCS12 to a DER format.
*/
extern "C" int32_t CryptoNative_GetPkcs12DerSize(PKCS12* p12);

/*
Shims the i2d_PKCS12 method.

Returns the number of bytes written to buf.
*/
extern "C" int32_t CryptoNative_EncodePkcs12(PKCS12* p12, uint8_t* buf);

/*
Shims the PKCS12_parse method.

Returns 1 on success, otherwise 0.
*/
extern "C" int32_t
CryptoNative_Pkcs12Parse(PKCS12* p12, const char* pass, EVP_PKEY** pkey, X509** cert, X509Stack** ca);
