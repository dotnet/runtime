// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "opensslshim.h"

typedef enum : int32_t {
    Unspecified = 0,
    PrimeShortWeierstrass = 1,
    PrimeTwistedEdwards = 2,
    PrimeMontgomery = 3,
    Characteristic2 = 4
} ECCurveType;


/*
Returns the ECCurveType given the key.
*/
extern "C" ECCurveType CryptoNative_EcKeyGetCurveType(
    const EC_KEY* key);

/*
Returns the ECC key parameters.
*/
extern "C" int32_t CryptoNative_GetECKeyParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    BIGNUM** qx, int32_t* cbQx,
    BIGNUM** qy, int32_t* cbQy,
    BIGNUM** d, int32_t* cbD);

/*
Returns the ECC key and curve parameters.
*/
extern "C" int32_t CryptoNative_GetECCurveParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    ECCurveType* curveType,
    BIGNUM** qx, int32_t* cbx,
    BIGNUM** qy, int32_t* cby,
    BIGNUM** d, int32_t* cbd,
    BIGNUM** p, int32_t* cbP,
    BIGNUM** a, int32_t* cbA,
    BIGNUM** b, int32_t* cbB,
    BIGNUM** gx, int32_t* cbGx,
    BIGNUM** gy, int32_t* cbGy,
    BIGNUM** order, int32_t* cbOrder,
    BIGNUM** cofactor, int32_t* cbCofactor,
    BIGNUM** seed, int32_t* cbSeed);

/*
Creates the new EC_KEY instance using the curve oid (friendly name or value) and public key parameters.
Returns 1 upon success, -1 if oid was not found, otherwise 0.
*/
extern "C" int32_t CryptoNative_EcKeyCreateByKeyParameters(
    EC_KEY** key,
    const char* oid,
    uint8_t* qx, int32_t qxLength, 
    uint8_t* qy, int32_t qyLength, 
    uint8_t* d, int32_t dLength);

/*
Returns the new EC_KEY instance using the explicit parameters.
*/
extern "C" EC_KEY* CryptoNative_EcKeyCreateByExplicitParameters(
    ECCurveType curveType,
    uint8_t* qx, int32_t qxLength,
    uint8_t* qy, int32_t qyLength,
    uint8_t* d, int32_t dLength,
    uint8_t* p, int32_t pLength,
    uint8_t* a, int32_t aLength,
    uint8_t* b, int32_t bLength,
    uint8_t* gx, int32_t gxLength,
    uint8_t* gy, int32_t gyLength,
    uint8_t* order, int32_t nLength,
    uint8_t* cofactor, int32_t hLength,
    uint8_t* seed, int32_t sLength);
