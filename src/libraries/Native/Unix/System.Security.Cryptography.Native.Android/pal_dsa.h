// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"
#include "pal_jni.h"

/*
Shims the DSA_up_ref method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaUpRef(jobject dsa);

/*
Cleans up and deletes a DSA instance.

Implemented by calling DSA_free

No-op if dsa is null.
The given DSA pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void AndroidCryptoNative_DsaDestroy(jobject dsa);

/*
Shims the DSA_generate_key_ex method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaGenerateKey(jobject* dsa, int32_t bits);

/*
Returns the size of the ASN.1 encoded signature.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaSizeSignature(jobject dsa);

/*
Returns the size of the p parameter in bytes.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaSizeP(jobject dsa);

/*
Returns the size of one of the biginteger fields in a signature in bytes.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaSignatureFieldSize(jobject dsa);

/*
Shims the DSA_sign method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t
AndroidCryptoNative_DsaSign(
    jobject dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t* outSignatureLength);

/*
Shims the DSA_verify method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t
AndroidCryptoNative_DsaVerify(
    jobject dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t signatureLength);

/*
Gets all the parameters from the DSA instance.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_GetDsaParameters(
    jobject dsa,
    jobject* p, int32_t* pLength,
    jobject* q, int32_t* qLength,
    jobject* g, int32_t* gLength,
    jobject* y, int32_t* yLength,
    jobject* x, int32_t* xLength);

/*
Sets all the parameters on the DSA instance.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaKeyCreateByExplicitParameters(
    jobject* dsa,
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
