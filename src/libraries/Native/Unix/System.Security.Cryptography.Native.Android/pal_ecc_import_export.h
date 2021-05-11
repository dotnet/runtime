// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_eckey.h"
#include "pal_jni.h"
#include "pal_types.h"

typedef enum
{
    Unspecified = 0,
    PrimeShortWeierstrass = 1,
    PrimeTwistedEdwards = 2,
    PrimeMontgomery = 3,
    Characteristic2 = 4
} ECCurveType;

typedef struct
{
    jobject qx_bn;
    jobject qy_bn;
    jobject d_bn;
    int32_t qx_cb;
    int32_t qy_cb;
    int32_t d_cb;
} AndroidECKeyParameters;

typedef struct
{
    jobject qx_bn;
    jobject qy_bn;
    jobject p_bn;
    jobject a_bn;
    jobject b_bn;
    jobject gx_bn;
    jobject gy_bn;
    jobject order_bn;
    jobject cofactor_bn;
    jobject seed_bn;
    jobject d_bn;
    int32_t qx_cb;
    int32_t qy_cb;
    int32_t p_cb;
    int32_t a_cb;
    int32_t b_cb;
    int32_t gx_cb;
    int32_t gy_cb;
    int32_t order_cb;
    int32_t cofactor_cb;
    int32_t seed_cb;
    int32_t d_cb;
} AndroidECCurveParameters;

typedef struct
{
    uint8_t* qx;
    uint8_t* qy;
    uint8_t* d;
    int32_t qx_length;
    int32_t qy_length;
    int32_t d_length;
} AndroidECKeyArrayParameters;

typedef struct
{
    uint8_t* qx;
    uint8_t* qy;
    uint8_t* d;
    uint8_t* p;
    uint8_t* a;
    uint8_t* b;
    uint8_t* gx;
    uint8_t* gy;
    uint8_t* order;
    uint8_t* cofactor;
    uint8_t* seed;
    int32_t qx_length;
    int32_t qy_length;
    int32_t d_length;
    int32_t p_length;
    int32_t a_length;
    int32_t b_length;
    int32_t gx_length;
    int32_t gy_length;
    int32_t order_length;
    int32_t cofactor_length;
    int32_t seed_length;
} AndroidECKeyExplicitParameters;
/*
Returns the ECC key parameters.
*/
PALEXPORT int32_t AndroidCryptoNative_GetECKeyParameters(const EC_KEY* key,
                                                  int32_t includePrivate,
                                                  AndroidECKeyParameters *parameters);

/*
Returns the ECC key and curve parameters.
*/
PALEXPORT int32_t AndroidCryptoNative_GetECCurveParameters(const EC_KEY* key,
                                                    int32_t includePrivate,
                                                    ECCurveType* curveType,
                                                    AndroidECCurveParameters* parameters);

/*
Creates the new EC_KEY instance using the curve oid (friendly name or value) and public key parameters.
Returns 1 upon success, -1 if oid was not found, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key,
                                                          const char* oid,
                                                          AndroidECKeyArrayParameters* parameters);

/*
Returns the new EC_KEY instance using the explicit parameters.
*/
PALEXPORT EC_KEY* AndroidCryptoNative_EcKeyCreateByExplicitParameters(ECCurveType curveType, AndroidECKeyExplicitParameters* parameters);
