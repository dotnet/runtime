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

/*
Returns the ECC key parameters.
*/
PALEXPORT int32_t AndroidCryptoNative_GetECKeyParameters(const EC_KEY* key,
                                                  int32_t includePrivate,
                                                  jobject* qx,
                                                  int32_t* cbQx,
                                                  jobject* qy,
                                                  int32_t* cbQy,
                                                  jobject* d,
                                                  int32_t* cbD);

/*
Returns the ECC key and curve parameters.
*/
PALEXPORT int32_t AndroidCryptoNative_GetECCurveParameters(const EC_KEY* key,
                                                    int32_t includePrivate,
                                                    ECCurveType* curveType,
                                                    jobject* qx,
                                                    int32_t* cbx,
                                                    jobject* qy,
                                                    int32_t* cby,
                                                    jobject* d,
                                                    int32_t* cbd,
                                                    jobject* p,
                                                    int32_t* cbP,
                                                    jobject* a,
                                                    int32_t* cbA,
                                                    jobject* b,
                                                    int32_t* cbB,
                                                    jobject* gx,
                                                    int32_t* cbGx,
                                                    jobject* gy,
                                                    int32_t* cbGy,
                                                    jobject* order,
                                                    int32_t* cbOrder,
                                                    jobject* cofactor,
                                                    int32_t* cbCofactor,
                                                    jobject* seed,
                                                    int32_t* cbSeed);

/*
Creates the new EC_KEY instance using the curve oid (friendly name or value) and public key parameters.
Returns 1 upon success, -1 if oid was not found, otherwise 0.
*/
PALEXPORT int32_t AndroidCryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key,
                                                          const char* oid,
                                                          uint8_t* qx,
                                                          int32_t qxLength,
                                                          uint8_t* qy,
                                                          int32_t qyLength,
                                                          uint8_t* d,
                                                          int32_t dLength);

/*
Returns the new EC_KEY instance using the explicit parameters.
*/
PALEXPORT EC_KEY* AndroidCryptoNative_EcKeyCreateByExplicitParameters(ECCurveType curveType,
                                                               uint8_t* qx,
                                                               int32_t qxLength,
                                                               uint8_t* qy,
                                                               int32_t qyLength,
                                                               uint8_t* d,
                                                               int32_t dLength,
                                                               uint8_t* p,
                                                               int32_t pLength,
                                                               uint8_t* a,
                                                               int32_t aLength,
                                                               uint8_t* b,
                                                               int32_t bLength,
                                                               uint8_t* gx,
                                                               int32_t gxLength,
                                                               uint8_t* gy,
                                                               int32_t gyLength,
                                                               uint8_t* order,
                                                               int32_t nLength,
                                                               uint8_t* cofactor,
                                                               int32_t hLength,
                                                               uint8_t* seed,
                                                               int32_t sLength);
