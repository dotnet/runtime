// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_ecc_import_export.h"
#include "pal_utilities.h"

ECCurveType MethodToCurveType(EC_METHOD* method)
{
    if (method == EC_GFp_mont_method())
        return ECCurveType::PrimeMontgomery;

    int fieldType = EC_METHOD_get_field_type(method);

    if (fieldType == NID_X9_62_characteristic_two_field)
        return ECCurveType::Characteristic2;

    if (fieldType == NID_X9_62_prime_field)
        return ECCurveType::PrimeShortWeierstrass;

    return ECCurveType::Unspecified;
}

const EC_METHOD* CurveTypeToMethod(ECCurveType curveType)
{
    if (curveType == ECCurveType::PrimeShortWeierstrass)
        return EC_GFp_simple_method();

    if (curveType == ECCurveType::PrimeMontgomery)
        return EC_GFp_mont_method();

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GF2m_simple_method) && (curveType == ECCurveType::Characteristic2))
        return EC_GF2m_simple_method();
#endif

    return nullptr; //Edwards and others
}

extern "C" ECCurveType CryptoNative_EcKeyGetCurveType(
    const EC_KEY* key)
{
    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (!group) return ECCurveType::Unspecified;

    const EC_METHOD* method = EC_GROUP_method_of(group);
    if (!method) return ECCurveType::Unspecified;

    return MethodToCurveType(const_cast<EC_METHOD*>(method));
}

extern "C" int32_t CryptoNative_GetECKeyParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    BIGNUM** qx, int32_t* cbQx,
    BIGNUM** qy, int32_t* cbQy,
    BIGNUM** d, int32_t* cbD)
{
    // Verify the out parameters. Note out parameters used to minimize pinvoke calls.
    if (!key ||
        !qx || !cbQx ||
        !qy || !cbQy ||
        (includePrivate && (!d  || !cbD)))
    {
        assert(false);

        // Since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (qx) *qx = nullptr; if (cbQx) *cbQx = 0;
        if (qy) *qy = nullptr; if (cbQy) *cbQy = 0;
        if (d)  *d  = nullptr; if (cbD) *cbD = 0;

        return 0;
    }

    // Get the public key and curve
    int rc = 0;
    BIGNUM *xBn = nullptr;
    BIGNUM *yBn = nullptr;

    ECCurveType curveType = CryptoNative_EcKeyGetCurveType(key);
    const EC_POINT* Q = EC_KEY_get0_public_key(key);
    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (curveType == ECCurveType::Unspecified || !Q || !group) 
        goto error;

    // Extract qx and qy
    xBn = BN_new();
    yBn = BN_new();
    if (!xBn || !yBn)
        goto error;

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_get_affine_coordinates_GF2m) && (curveType == ECCurveType::Characteristic2))
    {
        if (!EC_POINT_get_affine_coordinates_GF2m(group, Q, xBn, yBn, nullptr)) 
            goto error;
    }
    else
#endif
    {
        if (!EC_POINT_get_affine_coordinates_GFp(group, Q, xBn, yBn, nullptr)) 
            goto error;
    }

    // Success; assign variables
    *qx = xBn; *cbQx = BN_num_bytes(xBn);
    *qy = yBn; *cbQy = BN_num_bytes(yBn);

    if (includePrivate)
    {
        const BIGNUM* const_bignum_privateKey = EC_KEY_get0_private_key(key);
        if (const_bignum_privateKey != nullptr)
        {
            *d = const_cast<BIGNUM*>(const_bignum_privateKey);
            *cbD = BN_num_bytes(*d);
        }
        else
        {
            rc = -1;
            goto error;
        }
    }
    else
    {
        if (d)
            *d = nullptr;

        if (cbD)
            *cbD = 0;
    }

    // success
    return 1;

error:
    *cbQx = *cbQy = 0;
    *qx = *qy = 0;
    if (d) *d = nullptr;
    if (cbD) *cbD = 0;
    if (xBn) BN_free(xBn);
    if (yBn) BN_free(yBn);
    return rc;
}

extern "C" int32_t CryptoNative_GetECCurveParameters(
    const EC_KEY* key,
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
    BIGNUM** seed, int32_t* cbSeed)
{
    // Get the public key parameters first in case any of its 'out' parameters are not initialized
    int32_t rc = CryptoNative_GetECKeyParameters(key, includePrivate, qx, cbQx, qy, cbQy, d, cbD);

    // Verify the out parameters. Note out parameters used to minimize pinvoke calls.
    if (!p || !cbP ||
        !a || !cbA ||
        !b || !cbB ||
        !gx || !cbGx ||
        !gy || !cbGy ||
        !order || !cbOrder ||
        !cofactor || !cbCofactor ||
        !seed || !cbSeed)
    {
        assert(false);

        // Since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (p) *p = nullptr; if (cbP) *cbP = 0;
        if (a) *a = nullptr; if (cbA) *cbA = 0;
        if (b) *b = nullptr; if (cbB) *cbB = 0;
        if (gx) *gx = nullptr; if (cbGx) *cbGx = 0;
        if (gy) *gy = nullptr; if (cbGy) *cbGy = 0;
        if (order) *order = nullptr; if (cbOrder) *cbOrder = 0;
        if (cofactor) *cofactor = nullptr; if (cbCofactor) *cbCofactor = 0;
        if (seed) *seed = nullptr; if (cbSeed) *cbSeed = 0;

        return 0;
    }

    EC_GROUP* group = nullptr;
    EC_POINT* G = nullptr;
    EC_METHOD* curveMethod = nullptr;
    BIGNUM* xBn = nullptr;
    BIGNUM* yBn = nullptr;
    BIGNUM* pBn = nullptr;
    BIGNUM* aBn = nullptr;
    BIGNUM* bBn = nullptr;
    BIGNUM* orderBn = nullptr;
    BIGNUM* cofactorBn = nullptr;
    BIGNUM* seedBn = nullptr;

    // Exit if CryptoNative_GetECKeyParameters failed
    if (rc != 1) 
        goto error;

    xBn = BN_new();
    yBn = BN_new();
    pBn = BN_new();
    aBn = BN_new();
    bBn = BN_new();
    orderBn = BN_new();
    cofactorBn = BN_new();

    if (!xBn || !yBn || !pBn || !aBn || !bBn || !orderBn || !cofactorBn)
        goto error;

    group = const_cast<EC_GROUP*>(EC_KEY_get0_group(key)); // curve
    if (!group) 
        goto error;

    curveMethod = const_cast<EC_METHOD*>(EC_GROUP_method_of(group));
    if (!curveMethod) 
        goto error;

    *curveType = MethodToCurveType(curveMethod);
    if (*curveType == ECCurveType::Unspecified) 
        goto error;

    // Extract p, a, b
#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GROUP_get_curve_GF2m) && (*curveType == ECCurveType::Characteristic2))
    {
        // pBn represents the binary polynomial
        if (!EC_GROUP_get_curve_GF2m(group, pBn, aBn, bBn, nullptr)) 
            goto error;
    }
    else
#endif
    {
        // pBn represents the prime
        if (!EC_GROUP_get_curve_GFp(group, pBn, aBn, bBn, nullptr))
            goto error;
    }

    // Extract gx and gy
    G = const_cast<EC_POINT*>(EC_GROUP_get0_generator(group));
#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_get_affine_coordinates_GF2m) && (*curveType == ECCurveType::Characteristic2))
    {
        if (!EC_POINT_get_affine_coordinates_GF2m(group, G, xBn, yBn, NULL)) 
            goto error;
    }
    else
#endif
    {
        if (!EC_POINT_get_affine_coordinates_GFp(group, G, xBn, yBn, NULL)) 
            goto error;
    }

    // Extract order (n)
    if (!EC_GROUP_get_order(group, orderBn, nullptr))
        goto error;

    // Extract cofactor (h)
    if (!EC_GROUP_get_cofactor(group, cofactorBn, nullptr))
        goto error;

    // Extract seed (optional)
    if (EC_GROUP_get0_seed(group))
    {
        seedBn = BN_bin2bn(EC_GROUP_get0_seed(group), 
            static_cast<int>(EC_GROUP_get_seed_len(group)), NULL);
        
        *seed = seedBn;
        *cbSeed = BN_num_bytes(seedBn);

        /*
            To implement SEC 1 standard and align to Windows, we also want to extract the nid
            to the algorithm (e.g. NID_sha256) that was used to generate seed but this
            metadata does not appear to exist in openssl (see openssl's ec_curve.c) so we may
            eventually want to add that metadata, but that could be done on the managed side.
        */
    }
    else
    {
        *seed = nullptr;
        *cbSeed = 0;
    }

    // Success; assign variables
    *gx = xBn; *cbGx = BN_num_bytes(xBn);
    *gy = yBn; *cbGy = BN_num_bytes(yBn);
    *p = pBn; *cbP = BN_num_bytes(pBn);
    *a = aBn; *cbA = BN_num_bytes(aBn);
    *b = bBn; *cbB = BN_num_bytes(bBn);
    *order = orderBn; *cbOrder = BN_num_bytes(orderBn);
    *cofactor = cofactorBn; *cbCofactor = BN_num_bytes(cofactorBn);

    rc = 1;
    goto exit;

error:
    // Clear out variables from CryptoNative_GetECKeyParameters
    *cbQx = *cbQy = 0;
    *qx = *qy = nullptr;
    if (d) *d = nullptr;
    if (cbD) *cbD = 0;

    // Clear our out variables
    *curveType = ECCurveType::Unspecified;
    *cbP = *cbA = *cbB = *cbGx = *cbGy = *cbOrder = *cbCofactor = *cbSeed = 0;
    *p = *a = *b = *gx = *gy = *order = *cofactor = *seed = nullptr;

    if (xBn) BN_free(xBn);
    if (yBn) BN_free(yBn);
    if (pBn) BN_free(pBn);
    if (aBn) BN_free(aBn);
    if (bBn) BN_free(bBn);
    if (orderBn) BN_free(orderBn);
    if (cofactorBn) BN_free(cofactorBn);
    if (seedBn) BN_free(seedBn);

exit:
    return rc;
}

extern "C" int32_t CryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key, const char* oid, uint8_t* qx, int32_t qxLength, uint8_t* qy, int32_t qyLength, uint8_t* d, int32_t dLength)
{
    if (!key || !oid)
    {
        assert(false);
        return 0;
    }

    *key = nullptr;

    // oid can be friendly name or value
    int nid = OBJ_txt2nid(oid);
    if (!nid)
        return -1;

    *key = EC_KEY_new_by_curve_name(nid);
    if (!(*key))
        return -1;

    BIGNUM* dBn = nullptr;
    BIGNUM* qxBn = nullptr;
    BIGNUM* qyBn = nullptr;

    // If key values specified, use them, otherwise a key will be generated later
    if (qx && qy)
    {
        qxBn = BN_bin2bn(qx, qxLength, nullptr);
        qyBn = BN_bin2bn(qy, qyLength, nullptr);
        if (!qxBn || !qyBn)
            goto error;

        if (!EC_KEY_set_public_key_affine_coordinates(*key, qxBn, qyBn))
            goto error;

        // Set private key (optional)
        if (d && dLength > 0)
        {
            dBn = BN_bin2bn(d, dLength, nullptr);
            if (!dBn)
                goto error;

            if (!EC_KEY_set_private_key(*key, dBn))
                goto error;
        }

        // Validate key
        if (!EC_KEY_check_key(*key))
            goto error;
    }

    // Success
    return 1;

error:
    if (qxBn) BN_free(qxBn);
    if (qyBn) BN_free(qyBn);
    if (dBn) BN_free(dBn);
    if (*key)
    {
        EC_KEY_free(*key);
        *key = nullptr;
    }
    return 0;
}

extern "C" EC_KEY* CryptoNative_EcKeyCreateByExplicitParameters(
    ECCurveType curveType,
    uint8_t* qx, int32_t qxLength,
    uint8_t* qy, int32_t qyLength,
    uint8_t* d,  int32_t dLength,
    uint8_t* p,  int32_t pLength,
    uint8_t* a,  int32_t aLength,
    uint8_t* b,  int32_t bLength,
    uint8_t* gx, int32_t gxLength,
    uint8_t* gy, int32_t gyLength,
    uint8_t* order,  int32_t orderLength,
    uint8_t* cofactor,  int32_t cofactorLength,
    uint8_t* seed,  int32_t seedLength)
{
    if (!p || !a || !b || !gx || !gy || !order || !cofactor)
    {
        // qx, qy, d and seed are optional
        assert(false);
        return 0;
    }

    EC_KEY* key = nullptr;
    EC_POINT* G = nullptr;

    BIGNUM* qxBn = nullptr;
    BIGNUM* qyBn = nullptr;
    BIGNUM* dBn = nullptr;
    BIGNUM* pBn = nullptr; // p = either the char2 polynomial or the prime
    BIGNUM* aBn = nullptr;
    BIGNUM* bBn = nullptr;
    BIGNUM* gxBn = nullptr;
    BIGNUM* gyBn = nullptr;
    BIGNUM* orderBn = nullptr;
    BIGNUM* cofactorBn = nullptr;

    // Create the group. Explicitly specify the curve type because using EC_GROUP_new_curve_GFp
    // will default to montgomery curve
    const EC_METHOD* curveMethod = CurveTypeToMethod(curveType);
    if (!curveMethod) return nullptr;

    EC_GROUP* group = EC_GROUP_new(curveMethod);
    if (!group) return nullptr;

    pBn = BN_bin2bn(p, pLength, nullptr);
    // At this point we should use 'goto error' since we allocated memory
    aBn = BN_bin2bn(a, aLength, nullptr);
    bBn = BN_bin2bn(b, bLength, nullptr);

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GROUP_set_curve_GF2m) && (curveType == ECCurveType::Characteristic2))
    {
        if (!EC_GROUP_set_curve_GF2m(group, pBn, aBn, bBn, nullptr)) 
            goto error;
    }
    else
#endif
    {
        if (!EC_GROUP_set_curve_GFp(group, pBn, aBn, bBn, nullptr))    
            goto error;
    }

    // Set generator, order and cofactor
    G = EC_POINT_new(group);
    gxBn = BN_bin2bn(gx, gxLength, nullptr);
    gyBn = BN_bin2bn(gy, gyLength, nullptr);

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_set_affine_coordinates_GF2m) && (curveType == ECCurveType::Characteristic2))
    {
        EC_POINT_set_affine_coordinates_GF2m(group, G, gxBn, gyBn, nullptr);
    }
    else
#endif
    {
        EC_POINT_set_affine_coordinates_GFp(group, G, gxBn, gyBn, nullptr);
    }

    orderBn = BN_bin2bn(order, orderLength, nullptr);
    cofactorBn = BN_bin2bn(cofactor, cofactorLength, nullptr);
    EC_GROUP_set_generator(group, G, orderBn, cofactorBn);

    // Set seed (optional)
    if (seed && seedLength > 0)
    {
        if (!EC_GROUP_set_seed(group, seed, static_cast<size_t>(seedLength)))
            goto error;
    }

    // Validate group
    if (!EC_GROUP_check(group, nullptr))
        goto error;

    // Create key
    key = EC_KEY_new();
    if (!key)
        goto error;

    if (!EC_KEY_set_group(key, group))
        goto error;

    // Set the public and private key values
    if (qx && qy)
    {
        qxBn = BN_bin2bn(qx, qxLength, nullptr);
        qyBn = BN_bin2bn(qy, qyLength, nullptr);
        if (!qxBn || !qyBn)
            goto error;

        if (!EC_KEY_set_public_key_affine_coordinates(key, qxBn, qyBn))
            goto error;

        // Set private key (optional)
        if (d && dLength)
        {
            dBn = BN_bin2bn(d, dLength, nullptr);
            if (!dBn)
                goto error;

            if (!EC_KEY_set_private_key(key, dBn))
                goto error;
        }

        // Validate key
        if (!EC_KEY_check_key(key))
            goto error;
    }

    // Success
    return key;

error:
    if (qxBn) BN_free(qxBn);
    if (qyBn) BN_free(qyBn);
    if (dBn) BN_free(dBn);
    if (pBn) BN_free(pBn);
    if (aBn) BN_free(aBn);
    if (bBn) BN_free(bBn);
    if (gxBn) BN_free(gxBn);
    if (gyBn) BN_free(gyBn);
    if (orderBn) BN_free(orderBn);
    if (cofactorBn) BN_free(cofactorBn);
    if (G) EC_POINT_free(G);
    if (group) EC_GROUP_free(group);
    if (key) EC_KEY_free(key);
    return nullptr;
}
