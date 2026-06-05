// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

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
PALEXPORT int32_t CryptoNative_GetECKeyParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    const BIGNUM** qx, int32_t* cbQx,
    const BIGNUM** qy, int32_t* cbQy,
    const BIGNUM** d, int32_t* cbD);

/*
Returns the ECC key and curve parameters.
*/
PALEXPORT int32_t CryptoNative_GetECCurveParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    ECCurveType* curveType,
    const BIGNUM** qx, int32_t* cbx,
    const BIGNUM** qy, int32_t* cby,
    const BIGNUM** d, int32_t* cbd,
    const BIGNUM** p, int32_t* cbP,
    const BIGNUM** a, int32_t* cbA,
    const BIGNUM** b, int32_t* cbB,
    const BIGNUM** gx, int32_t* cbGx,
    const BIGNUM** gy, int32_t* cbGy,
    const BIGNUM** order, int32_t* cbOrder,
    const BIGNUM** cofactor, int32_t* cbCofactor,
    const BIGNUM** seed, int32_t* cbSeed);

/*
Creates the new EC_KEY instance using the curve oid (friendly name or value) and public key parameters.
Returns 1 upon success, -1 if oid was not found, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EcKeyCreateByKeyParameters(
    EC_KEY** key,
    const char* oid,
    const uint8_t* qx, int32_t qxLength,
    const uint8_t* qy, int32_t qyLength,
    const uint8_t* d, int32_t dLength);

/*
Gets the NID of the curve name as an oid value for the specified EVP_PKEY.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyGetEcGroupNid(EVP_PKEY *pkey, int32_t* nidName);

/*
Returns the EC key parameters (public coordinates and optional private key) from the given EVP_PKEY.
Returns 1 upon success, -1 if includePrivate is set but the key has no private component, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyGetEcKeyParameters(
    const EVP_PKEY* pkey,
    int32_t includePrivate,
    BIGNUM** qx, int32_t* cbQx,
    BIGNUM** qy, int32_t* cbQy,
    BIGNUM** d, int32_t* cbD);

/*
Returns the new EC_KEY instance using the explicit parameters.
*/
PALEXPORT EC_KEY* CryptoNative_EcKeyCreateByExplicitParameters(
    ECCurveType curveType,
    const uint8_t* qx, int32_t qxLength,
    const uint8_t* qy, int32_t qyLength,
    const uint8_t* d, int32_t dLength,
    const uint8_t* p, int32_t pLength,
    const uint8_t* a, int32_t aLength,
    const uint8_t* b, int32_t bLength,
    const uint8_t* gx, int32_t gxLength,
    const uint8_t* gy, int32_t gyLength,
    const uint8_t* order, int32_t nLength,
    const uint8_t* cofactor, int32_t hLength,
    const uint8_t* seed, int32_t sLength);

/*
Generates a new EC key pair for a named curve using EVP_PKEY APIs.
On OpenSSL 3.0+ uses EVP_PKEY_CTX, otherwise falls back to EC_KEY APIs.
If keySize is not NULL, it receives the key size in bits.
Returns 1 upon success, 2 if oid was not found, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyGenerateByEcCurveOid(
    EVP_PKEY** pkey,
    const char* oid,
    int32_t* keySize);

/*
Returns 1 if the EVP_PKEY EC key uses explicit encoding, 0 if it uses named curve encoding
or the encoding could not be read (named curve is the default), or -1 if the API is unavailable
(e.g. pre-3.0 OpenSSL) and the caller should use an alternative method.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyEcHasExplicitEncoding(EVP_PKEY* pkey);

/*
Returns the EC field degree (bits in the underlying field) for the specified EVP_PKEY.
This is the value used as KeySize for EC keys in .NET, matching Windows CNG's
BCRYPT_PUBLIC_KEY_LENGTH semantics for EC keys (field modulus bit-size, e.g. 256
for P-256). Note that for some uncommon curves this differs from the subgroup
order's bit length (e.g. wap-wsg-idm-ecid-wtls7 has field=160, order=161).
Works for both provider-backed and legacy EC_KEY-backed EVP_PKEYs.
Returns 0 on failure.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyGetEcKeySize(EVP_PKEY* pkey);

/*
Creates a new EVP_PKEY for a named EC curve using the provided key parameters.
On success, *pkey receives the new key and *keySize receives the key size in bits.
Returns 1 upon success, 2 if oid was not found, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyCreateByEcParameters(
    EVP_PKEY** pkey,
    const char* oid,
    const uint8_t* qx, int32_t qxLength,
    const uint8_t* qy, int32_t qyLength,
    const uint8_t* d, int32_t dLength,
    int32_t* keySize);

/*
Creates a new EVP_PKEY for an EC key with explicit curve parameters.
On success, *pkey receives the new key and *keySize receives the key size in bits.
Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyCreateByEcExplicitParameters(
    ECCurveType curveType,
    const uint8_t* qx, int32_t qxLength,
    const uint8_t* qy, int32_t qyLength,
    const uint8_t* d, int32_t dLength,
    const uint8_t* p, int32_t pLength,
    const uint8_t* a, int32_t aLength,
    const uint8_t* b, int32_t bLength,
    const uint8_t* gx, int32_t gxLength,
    const uint8_t* gy, int32_t gyLength,
    const uint8_t* order, int32_t orderLength,
    const uint8_t* cofactor, int32_t cofactorLength,
    const uint8_t* seed, int32_t seedLength,
    EVP_PKEY** pkey,
    int32_t* keySize);

/*
Returns the ECC curve parameters of the given EVP_PKEY.
Returns 1 upon success, -1 if includePrivate is set but the key has no private component, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EvpPKeyGetEcCurveParameters(
    EVP_PKEY* pkey,
    int32_t includePrivate,
    ECCurveType* curveType,
    BIGNUM** qx, int32_t* cbQx,
    BIGNUM** qy, int32_t* cbQy,
    BIGNUM** d, int32_t* cbD,
    BIGNUM** p, int32_t* cbP,
    BIGNUM** a, int32_t* cbA,
    BIGNUM** b, int32_t* cbB,
    BIGNUM** gx, int32_t* cbGx,
    BIGNUM** gy, int32_t* cbGy,
    BIGNUM** order, int32_t* cbOrder,
    BIGNUM** cofactor, int32_t* cbCofactor,
    BIGNUM** seed, int32_t* cbSeed);
