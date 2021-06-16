// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the EVP_PKEY_new method.

Returns the new EVP_PKEY instance.
*/
PALEXPORT EVP_PKEY* CryptoNative_EvpPkeyCreate(void);

/*
Create a new EVP_PKEY that has the same interior key as currentKey,
optionally verifying that the key has the correct algorithm.
*/
PALEXPORT EVP_PKEY* CryptoNative_EvpPKeyDuplicate(EVP_PKEY* currentKey, int32_t algId);

/*
Cleans up and deletes a EVP_PKEY instance.

Implemented by calling EVP_PKEY_free.

No-op if pkey is null.
The given EVP_PKEY pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey);

/*
Returns the maximum size, in bytes, of an operation with the provided key.
*/
PALEXPORT int32_t CryptoNative_EvpPKeySize(EVP_PKEY* pkey);

/*
Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when
duplicating a private key context as part of duplicating the Pal object.

Returns the number (as of this call) of references to the EVP_PKEY. Anything less than
2 is an error, because the key is already in the process of being freed.
*/
PALEXPORT int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey);

/*
Decodes an X.509 SubjectPublicKeyInfo into an EVP_PKEY*, verifying the interpreted algorithm type.

Requres a non-null buf, and len > 0.
*/
PALEXPORT EVP_PKEY* CryptoNative_DecodeSubjectPublicKeyInfo(const uint8_t* buf, int32_t len, int32_t algId);

/*
Decodes an Pkcs8PrivateKeyInfo into an EVP_PKEY*, verifying the interpreted algorithm type.

Requres a non-null buf, and len > 0.
*/
PALEXPORT EVP_PKEY* CryptoNative_DecodePkcs8PrivateKey(const uint8_t* buf, int32_t len, int32_t algId);

/*
Reports the number of bytes rqeuired to encode an EVP_PKEY* as a Pkcs8PrivateKeyInfo, or a negative value on error.
*/
PALEXPORT int32_t CryptoNative_GetPkcs8PrivateKeySize(EVP_PKEY* pkey);

/*
Encodes the EVP_PKEY* as a Pkcs8PrivateKeyInfo, writing the encoded value to buf.

buf must be big enough, or an out of bounds write may occur.

Returns the number of bytes written.
*/
PALEXPORT int32_t CryptoNative_EncodePkcs8PrivateKey(EVP_PKEY* pkey, uint8_t* buf);

/*
Reports the number of bytes rqeuired to encode an EVP_PKEY* as an X.509 SubjectPublicKeyInfo, or a negative value on error.
*/
PALEXPORT int32_t CryptoNative_GetSubjectPublicKeyInfoSize(EVP_PKEY* pkey);

/*
Encodes the EVP_PKEY* as an X.509 SubjectPublicKeyInfo, writing the encoded value to buf.

buf must be big enough, or an out of bounds write may occur.

Returns the number of bytes written.
*/
PALEXPORT int32_t CryptoNative_EncodeSubjectPublicKeyInfo(EVP_PKEY* pkey, uint8_t* buf);
