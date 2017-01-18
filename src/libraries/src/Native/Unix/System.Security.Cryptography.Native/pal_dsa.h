// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "opensslshim.h"

/*
Shims the DSA_new method.

Returns the new DSA instance.
*/
extern "C" DSA* CryptoNative_DsaCreate();

/*
Shims the DSA_up_ref method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t CryptoNative_DsaUpRef(DSA* dsa);

/*
Cleans up and deletes a DSA instance.

Implemented by calling DSA_free

No-op if dsa is null.
The given DSA pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_DsaDestroy(DSA* dsa);

/*
Shims the DSA_generate_key_ex method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t CryptoNative_DsaGenerateKey(DSA** dsa, int32_t bits);

/*
Shims the DSA_size method.

Returns the size of the ASN.1 encoded signature.
*/
extern "C" int32_t CryptoNative_DsaSizeSignature(DSA* dsa);

/*
Returns the size of the p parameter in bytes.
*/
extern "C" int32_t CryptoNative_DsaSizeP(DSA* dsa);

/*
Returns the size of the q parameter in bytes.
*/
extern "C" int32_t CryptoNative_DsaSizeQ(DSA* dsa);

/*
Shims the DSA_sign method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t
CryptoNative_DsaSign(
    DSA* dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t* outSignatureLength);

/*
Shims the DSA_verify method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t
CryptoNative_DsaVerify(
    DSA* dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t signatureLength);

/*
Gets all the parameters from the DSA instance.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t CryptoNative_GetDsaParameters(
    const DSA* dsa,
    BIGNUM** p, int32_t* pLength,
    BIGNUM** q, int32_t* qLength,
    BIGNUM** g, int32_t* gLength,
    BIGNUM** y, int32_t* yLength,
    BIGNUM** x, int32_t* xLength);

/*
Sets all the parameters on the DSA instance.
*/
extern "C" int32_t CryptoNative_DsaKeyCreateByExplicitParameters(
    DSA** dsa,
    uint8_t* p,
    int32_t pLength,
    uint8_t* q,
    int32_t qLength,
    uint8_t* g,
    int32_t gLength,
    uint8_t* y,
    int32_t yLength,
    uint8_t* x,
    int32_t xLength);
