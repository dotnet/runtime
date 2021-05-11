// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"
#include "pal_jni.h"

/*
Generates a key pair of `bits` bits wide.

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
Returns the size of one of the biginteger fields in a P1363 signature in bytes.
*/
PALEXPORT int32_t AndroidCryptoNative_DsaSignatureFieldSize(jobject dsa);

/*
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
Returns 1 upon success, 0 upon verification mismatch, -1 on internal failure.
*/
PALEXPORT int32_t
AndroidCryptoNative_DsaVerify(
    jobject dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t signatureLength);

typedef struct
{
    jobject p_bn;
    jobject q_bn;
    jobject g_bn;
    jobject y_bn;
    jobject x_bn;
    int32_t p_cb;
    int32_t q_cb;
    int32_t g_cb;
    int32_t y_cb;
    int32_t x_cb;
} AndroidDSAParameters;

/*
Gets all the parameters from the DSA instance.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_GetDsaParameters(jobject dsa, AndroidDSAParameters *parameters);

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
