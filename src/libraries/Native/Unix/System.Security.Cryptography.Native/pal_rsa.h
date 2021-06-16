// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the RSA_new method.

Returns the new RSA instance.
*/
PALEXPORT RSA* CryptoNative_RsaCreate(void);

/*
Shims the RSA_up_ref method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_RsaUpRef(RSA* rsa);

/*
Cleans up and deletes a RSA instance.

Implemented by calling RSA_free

No-op if rsa is null.
The given RSA pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_RsaDestroy(RSA* rsa);

/*
Shims the d2i_RSAPublicKey method and makes it easier to invoke from managed code.
*/
PALEXPORT RSA* CryptoNative_DecodeRsaPublicKey(const uint8_t* buf, int32_t len);

/*
Shims the RSA_size method.

Returns the RSA modulus size in bytes.
*/
PALEXPORT int32_t CryptoNative_RsaSize(RSA* rsa);

/*
Gets all the parameters from the RSA instance.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_GetRsaParameters(const RSA* rsa,
                                                const BIGNUM** n,
                                                const BIGNUM** e,
                                                const BIGNUM** d,
                                                const BIGNUM** p,
                                                const BIGNUM** dmp1,
                                                const BIGNUM** q,
                                                const BIGNUM** dmq1,
                                                const BIGNUM** iqmp);

/*
Sets all the parameters on the RSA instance.
*/
PALEXPORT int32_t CryptoNative_SetRsaParameters(RSA* rsa,
                                              uint8_t* n,
                                              int32_t nLength,
                                              uint8_t* e,
                                              int32_t eLength,
                                              uint8_t* d,
                                              int32_t dLength,
                                              uint8_t* p,
                                              int32_t pLength,
                                              uint8_t* dmp1,
                                              int32_t dmp1Length,
                                              uint8_t* q,
                                              int32_t qLength,
                                              uint8_t* dmq1,
                                              int32_t dmq1Length,
                                              uint8_t* iqmp,
                                              int32_t iqmpLength);
