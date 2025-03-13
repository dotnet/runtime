// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecc_import_export.h"
#include "pal_utilities.h"

static ECCurveType MethodToCurveType(const EC_METHOD* method)
{
    if (method == EC_GFp_mont_method())
        return PrimeMontgomery;

    int fieldType = EC_METHOD_get_field_type(method);

    if (fieldType == NID_X9_62_characteristic_two_field)
        return Characteristic2;

    if (fieldType == NID_X9_62_prime_field)
        return PrimeShortWeierstrass;

    return Unspecified;
}

static const EC_METHOD* CurveTypeToMethod(ECCurveType curveType)
{
    if (curveType == PrimeShortWeierstrass)
        return EC_GFp_simple_method();

    if (curveType == PrimeMontgomery)
        return EC_GFp_mont_method();

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GF2m_simple_method) && (curveType == Characteristic2))
        return EC_GF2m_simple_method();
#endif

    return NULL; //Edwards and others
}

static ECCurveType EcKeyGetCurveType(
    const EC_KEY* key)
{
    // Simple accessors, no error queue impact.
    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (!group) return Unspecified;

    const EC_METHOD* method = EC_GROUP_method_of(group);
    if (!method) return Unspecified;

    return MethodToCurveType(method);
}

static int EcPointGetAffineCoordinates(const EC_GROUP *group, ECCurveType curveType, const EC_POINT *p, BIGNUM *x, BIGNUM *y)
{
#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_get_affine_coordinates_GF2m) && (curveType == Characteristic2))
    {
        if (!EC_POINT_get_affine_coordinates_GF2m(group, p, x, y, NULL))
            return 0;
    }
    else
#endif
    {
        if (!EC_POINT_get_affine_coordinates_GFp(group, p, x, y, NULL))
            return 0;
    }

#if !HAVE_OPENSSL_EC2M
    (void)curveType;
#endif

    return 1;
}

int32_t CryptoNative_GetECKeyParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    const BIGNUM** qx, int32_t* cbQx,
    const BIGNUM** qy, int32_t* cbQy,
    const BIGNUM** d, int32_t* cbD)
{
    assert(qx != NULL);
    assert(cbQx != NULL);
    assert(qy != NULL);
    assert(cbQy != NULL);
    assert(d != NULL);
    assert(cbD != NULL);

    // Get the public key and curve
    int rc = 0;
    BIGNUM *xBn = NULL;
    BIGNUM *yBn = NULL;

    ERR_clear_error();

    ECCurveType curveType = EcKeyGetCurveType(key);
    const EC_POINT* Q = EC_KEY_get0_public_key(key);
    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (curveType == Unspecified || !Q || !group)
        goto error;

    // Extract qx and qy
    xBn = BN_new();
    yBn = BN_new();
    if (!xBn || !yBn)
        goto error;

    if (!EcPointGetAffineCoordinates(group, curveType, Q, xBn, yBn))
        goto error;

    // Success; assign variables
    *qx = xBn; *cbQx = BN_num_bytes(xBn);
    *qy = yBn; *cbQy = BN_num_bytes(yBn);

    if (includePrivate)
    {
        const BIGNUM* const_bignum_privateKey = EC_KEY_get0_private_key(key);
        if (const_bignum_privateKey != NULL)
        {
            *d = const_bignum_privateKey;
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
            *d = NULL;

        if (cbD)
            *cbD = 0;
    }

    // success
    return 1;

error:
    *cbQx = *cbQy = 0;
    *qx = *qy = 0;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;
    if (xBn) BN_free(xBn);
    if (yBn) BN_free(yBn);
    return rc;
}

int32_t CryptoNative_GetECCurveParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    ECCurveType* curveType,
    const BIGNUM** qx, int32_t* cbQx,
    const BIGNUM** qy, int32_t* cbQy,
    const BIGNUM** d, int32_t* cbD,
    const BIGNUM** p, int32_t* cbP,
    const BIGNUM** a, int32_t* cbA,
    const BIGNUM** b, int32_t* cbB,
    const BIGNUM** gx, int32_t* cbGx,
    const BIGNUM** gy, int32_t* cbGy,
    const BIGNUM** order, int32_t* cbOrder,
    const BIGNUM** cofactor, int32_t* cbCofactor,
    const BIGNUM** seed, int32_t* cbSeed)
{
    assert(p != NULL);
    assert(cbP != NULL);
    assert(a != NULL);
    assert(cbA != NULL);
    assert(b != NULL);
    assert(cbB != NULL);
    assert(gx != NULL);
    assert(cbGx != NULL);
    assert(gy != NULL);
    assert(cbGy != NULL);
    assert(order != NULL);
    assert(cbOrder != NULL);
    assert(cofactor != NULL);
    assert(cbCofactor != NULL);
    assert(seed != NULL);
    assert(cbSeed != NULL);

    ERR_clear_error();

    // Get the public key parameters first in case any of its 'out' parameters are not initialized
    int32_t rc = CryptoNative_GetECKeyParameters(key, includePrivate, qx, cbQx, qy, cbQy, d, cbD);

    const EC_GROUP* group = NULL;
    const EC_POINT* G = NULL;
    const EC_METHOD* curveMethod = NULL;
    BIGNUM* xBn = NULL;
    BIGNUM* yBn = NULL;
    BIGNUM* pBn = NULL;
    BIGNUM* aBn = NULL;
    BIGNUM* bBn = NULL;
    BIGNUM* orderBn = NULL;
    BIGNUM* cofactorBn = NULL;
    BIGNUM* seedBn = NULL;

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

    group = EC_KEY_get0_group(key); // curve
    if (!group)
        goto error;

    curveMethod = EC_GROUP_method_of(group);
    if (!curveMethod)
        goto error;

    *curveType = MethodToCurveType(curveMethod);
    if (*curveType == Unspecified)
        goto error;

    // Extract p, a, b
#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GROUP_get_curve_GF2m) && (*curveType == Characteristic2))
    {
        // pBn represents the binary polynomial
        if (!EC_GROUP_get_curve_GF2m(group, pBn, aBn, bBn, NULL))
            goto error;
    }
    else
#endif
    {
        // pBn represents the prime
        if (!EC_GROUP_get_curve_GFp(group, pBn, aBn, bBn, NULL))
            goto error;
    }

    // Extract gx and gy
    G = EC_GROUP_get0_generator(group);
#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_get_affine_coordinates_GF2m) && (*curveType == Characteristic2))
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
    if (!EC_GROUP_get_order(group, orderBn, NULL))
        goto error;

    // Extract cofactor (h)
    if (!EC_GROUP_get_cofactor(group, cofactorBn, NULL))
        goto error;

    // Extract seed (optional)
    if (EC_GROUP_get0_seed(group))
    {
        seedBn = BN_bin2bn(EC_GROUP_get0_seed(group),
            (int)EC_GROUP_get_seed_len(group), NULL);

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
        *seed = NULL;
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
    *qx = *qy = NULL;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;

    // Clear our out variables
    *curveType = Unspecified;
    *cbP = *cbA = *cbB = *cbGx = *cbGy = *cbOrder = *cbCofactor = *cbSeed = 0;
    *p = *a = *b = *gx = *gy = *order = *cofactor = *seed = NULL;

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

int32_t CryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key, const char* oid, uint8_t* qx, int32_t qxLength, uint8_t* qy, int32_t qyLength, uint8_t* d, int32_t dLength)
{
    if (!key || !oid)
    {
        assert(false);
        return 0;
    }

    *key = NULL;

    ERR_clear_error();

    // oid can be friendly name or value
    int nid = OBJ_txt2nid(oid);
    if (!nid)
        return -1;

    EC_KEY* tmpKey = EC_KEY_new_by_curve_name(nid);
    if (tmpKey == NULL)
        return -1;

    int ret = 0;
    BIGNUM* dBn = NULL;
    BIGNUM* qxBn = NULL;
    BIGNUM* qyBn = NULL;
    EC_POINT* pubG = NULL;

    // If key values specified, use them, otherwise a key will be generated later
    if (qx && qy)
    {
        qxBn = BN_bin2bn(qx, qxLength, NULL);
        qyBn = BN_bin2bn(qy, qyLength, NULL);
        if (!qxBn || !qyBn)
            goto error;

        if (!EC_KEY_set_public_key_affine_coordinates(tmpKey, qxBn, qyBn))
            goto error;

        // Set private key (optional)
        if (d && dLength > 0)
        {
            dBn = BN_bin2bn(d, dLength, NULL);
            if (!dBn)
                goto error;

            if (!EC_KEY_set_private_key(tmpKey, dBn))
                goto error;
        }

        // Validate key
        if (!EC_KEY_check_key(tmpKey))
            goto error;
    }

    // If we don't have the public key but we have the private key, we can
    // re-derive the public key from d.
    else if (qx == NULL && qy == NULL && qxLength == 0 && qyLength == 0 &&
             d && dLength > 0)
    {
        dBn = BN_bin2bn(d, dLength, NULL);

        if (!dBn)
            goto error;

        if (!EC_KEY_set_private_key(tmpKey, dBn))
            goto error;

        const EC_GROUP* group = EC_KEY_get0_group(tmpKey);

        if (!group)
            goto error;

        pubG = EC_POINT_new(group);

        if (!pubG)
            goto error;

        if (!EC_POINT_mul(group, pubG, dBn, NULL, NULL, NULL))
            goto error;

        if (!EC_KEY_set_public_key(tmpKey, pubG))
            goto error;

        if (!EC_KEY_check_key(tmpKey))
            goto error;
    }

    // Success
    *key = tmpKey;
    tmpKey = NULL;
    ret = 1;

error:
    if (qxBn) BN_free(qxBn);
    if (qyBn) BN_free(qyBn);
    if (dBn) BN_clear_free(dBn);
    if (pubG) EC_POINT_free(pubG);
    if (tmpKey) EC_KEY_free(tmpKey);

    return ret;
}

int32_t CryptoNative_EvpPKeyGetEcGroupNid(const EVP_PKEY *pkey, int32_t* nidName)
{
    if (!nidName)
        return 0;

    *nidName = NID_undef;

    if (!pkey || EVP_PKEY_get_base_id(pkey) != EVP_PKEY_EC)
        return 0;

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_get_utf8_string_param))
    {
        return 0;
    }
#endif

#ifdef NEED_OPENSSL_3_0
    // Retrieve the textual name of the EC group (e.g., "prime256v1")
    // In all known cases this should be exactly 10 characters + 1 null byte but leaving some room in case it changes in the future
    // versions of OpenSSL. This length also matches with what OpenSSL uses in their demo code:
    // https://github.com/openssl/openssl/blob/ac80e1e15dcd13c61392a706170c427250c7bb69/demos/pkey/EVP_PKEY_EC_keygen.c#L88
    char curveName[80] = {0};

    if (!EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_GROUP_NAME, curveName, sizeof(curveName), NULL))
        return 0;

    *nidName = OBJ_txt2nid(curveName);
    return 1;
#else
    return 0;
#endif
}

int32_t CryptoNative_EvpPKeyGetEcKeyParameters(
    const EVP_PKEY* pkey,
    int32_t includePrivate,
    BIGNUM** qx, int32_t* cbQx,
    BIGNUM** qy, int32_t* cbQy,
    BIGNUM** d, int32_t* cbD)
{
    assert(qx != NULL);
    assert(cbQx != NULL);
    assert(qy != NULL);
    assert(cbQy != NULL);
    assert(d != NULL);
    assert(cbD != NULL);

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_get_bn_param))
    {
        *cbQx = *cbQy = 0;
        *qx = *qy = 0;
        if (d) *d = NULL;
        if (cbD) *cbD = 0;
        return 0;
    }
#endif

    int rc = 0;
    BIGNUM *xBn = NULL;
    BIGNUM *yBn = NULL;
    BIGNUM *dBn = NULL;

#ifdef NEED_OPENSSL_3_0
    // Ensure we have an EC key
    if (EVP_PKEY_get_base_id(pkey) != EVP_PKEY_EC)
        goto error;

    ERR_clear_error();

    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_PUB_X, &xBn))
        goto error;
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_PUB_Y, &yBn))
        goto error;

    *qx = xBn;
    *cbQx = BN_num_bytes(xBn);
    *qy = yBn;
    *cbQy = BN_num_bytes(yBn);

    if (includePrivate)
    {
        if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_PRIV_KEY, &dBn))
            goto error;

        *d = dBn;
        *cbD = BN_num_bytes(dBn);
    }
    else
    {
        *d = NULL;
        *cbD = 0;
    }

    // success
    return 1;

error:
#else
    (void)pkey;
    (void)includePrivate;
#endif
    *cbQx = *cbQy = 0;
    *qx = *qy = 0;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;
    if (xBn) BN_free(xBn);
    if (yBn) BN_free(yBn);
    return rc;
}

EC_KEY* CryptoNative_EcKeyCreateByExplicitParameters(
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

    ERR_clear_error();

    EC_KEY* key = NULL;
    EC_KEY* ret = NULL;
    EC_POINT* G = NULL;
    EC_POINT* pubG = NULL;

    BIGNUM* qxBn = NULL;
    BIGNUM* qyBn = NULL;
    BIGNUM* dBn = NULL;
    BIGNUM* pBn = NULL; // p = either the char2 polynomial or the prime
    BIGNUM* aBn = NULL;
    BIGNUM* bBn = NULL;
    BIGNUM* gxBn = NULL;
    BIGNUM* gyBn = NULL;
    BIGNUM* orderBn = NULL;
    BIGNUM* cofactorBn = NULL;

    // Create the group. Explicitly specify the curve type because using EC_GROUP_new_curve_GFp
    // will default to montgomery curve
    const EC_METHOD* curveMethod = CurveTypeToMethod(curveType);
    if (!curveMethod) return NULL;

    EC_GROUP* group = EC_GROUP_new(curveMethod);
    if (!group) return NULL;

    pBn = BN_bin2bn(p, pLength, NULL);
    // At this point we should use 'goto error' since we allocated memory
    aBn = BN_bin2bn(a, aLength, NULL);
    bBn = BN_bin2bn(b, bLength, NULL);

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_GROUP_set_curve_GF2m) && (curveType == Characteristic2))
    {
        if (!EC_GROUP_set_curve_GF2m(group, pBn, aBn, bBn, NULL))
            goto error;
    }
    else
#endif
    {
        if (!EC_GROUP_set_curve_GFp(group, pBn, aBn, bBn, NULL))
            goto error;
    }

    // Set generator, order and cofactor
    G = EC_POINT_new(group);
    gxBn = BN_bin2bn(gx, gxLength, NULL);
    gyBn = BN_bin2bn(gy, gyLength, NULL);

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_set_affine_coordinates_GF2m) && (curveType == Characteristic2))
    {
        EC_POINT_set_affine_coordinates_GF2m(group, G, gxBn, gyBn, NULL);
    }
    else
#endif
    {
        EC_POINT_set_affine_coordinates_GFp(group, G, gxBn, gyBn, NULL);
    }

    orderBn = BN_bin2bn(order, orderLength, NULL);
    cofactorBn = BN_bin2bn(cofactor, cofactorLength, NULL);
    EC_GROUP_set_generator(group, G, orderBn, cofactorBn);

    // Set seed (optional)
    if (seed && seedLength > 0)
    {
        if (!EC_GROUP_set_seed(group, seed, (size_t)seedLength))
            goto error;
    }

    // Validate group
    if (!EC_GROUP_check(group, NULL))
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
        qxBn = BN_bin2bn(qx, qxLength, NULL);
        qyBn = BN_bin2bn(qy, qyLength, NULL);
        if (!qxBn || !qyBn)
            goto error;

        if (!EC_KEY_set_public_key_affine_coordinates(key, qxBn, qyBn))
            goto error;

        // Set private key (optional)
        if (d && dLength)
        {
            dBn = BN_bin2bn(d, dLength, NULL);
            if (!dBn)
                goto error;

            if (!EC_KEY_set_private_key(key, dBn))
                goto error;
        }

        // Validate key
        if (!EC_KEY_check_key(key))
            goto error;
    }
    // If we don't have the public key but we have the private key, we can
    // re-derive the public key from d.
    else if (qx == NULL && qy == NULL && qxLength == 0 && qyLength == 0 &&
             d && dLength > 0)
    {
        dBn = BN_bin2bn(d, dLength, NULL);

        if (!dBn)
            goto error;

        if (!EC_KEY_set_private_key(key, dBn))
            goto error;

        pubG = EC_POINT_new(group);

        if (!pubG)
            goto error;

        if (!EC_POINT_mul(group, pubG, dBn, NULL, NULL, NULL))
            goto error;

        if (!EC_KEY_set_public_key(key, pubG))
            goto error;

        if (!EC_KEY_check_key(key))
            goto error;
    }

    // Success
    ret = key;
    key = NULL;

error:
    if (qxBn) BN_free(qxBn);
    if (qyBn) BN_free(qyBn);
    if (dBn) BN_clear_free(dBn);
    if (pBn) BN_free(pBn);
    if (aBn) BN_free(aBn);
    if (bBn) BN_free(bBn);
    if (gxBn) BN_free(gxBn);
    if (gyBn) BN_free(gyBn);
    if (orderBn) BN_free(orderBn);
    if (cofactorBn) BN_free(cofactorBn);
    if (G) EC_POINT_free(G);
    if (pubG) EC_POINT_free(pubG);
    if (group) EC_GROUP_free(group);
    if (key) EC_KEY_free(key);

    return ret;
}

static ECCurveType NIDToCurveType(int fieldType)
{
    if (fieldType == NID_X9_62_characteristic_two_field)
        return Characteristic2;

    if (fieldType == NID_X9_62_prime_field)
        return PrimeShortWeierstrass;

    return Unspecified;
}

int32_t CryptoNative_EvpPKeyGetEcCurveParameters(
    const EVP_PKEY* pkey,
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
    assert(p != NULL);
    assert(cbP != NULL);
    assert(a != NULL);
    assert(cbA != NULL);
    assert(b != NULL);
    assert(cbB != NULL);
    assert(gx != NULL);
    assert(cbGx != NULL);
    assert(gy != NULL);
    assert(cbGy != NULL);
    assert(order != NULL);
    assert(cbOrder != NULL);
    assert(cofactor != NULL);
    assert(cbCofactor != NULL);
    assert(seed != NULL);
    assert(cbSeed != NULL);

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EC_GROUP_new_by_curve_name) ||
        !API_EXISTS(EC_GROUP_get_field_type) ||
        !API_EXISTS(EVP_PKEY_get_octet_string_param) ||
        !API_EXISTS(EC_POINT_oct2point))
    {
        return 0;
    }
#endif

#ifdef NEED_OPENSSL_3_0
    ERR_clear_error();

    // Get the public key parameters first in case any of its 'out' parameters are not initialized
    int32_t rc = CryptoNative_EvpPKeyGetEcKeyParameters(pkey, includePrivate, qx, cbQx, qy, cbQy, d, cbD);

    EC_POINT* G = NULL;
    BIGNUM* xBn = BN_new();
    BIGNUM* yBn = BN_new();
    BIGNUM* pBn = NULL;
    BIGNUM* aBn = NULL;
    BIGNUM* bBn = NULL;
    BIGNUM* orderBn = NULL;
    BIGNUM* cofactorBn = NULL;
    size_t sufficientSeedBufferSize = 0;
    unsigned char* seedBuffer = NULL;
    EC_GROUP* group = NULL;
    size_t generatorBufferSize = 0;
    unsigned char* generatorBuffer = NULL;

    // Exit if CryptoNative_EvpPKeyGetEcKeyParameters failed
    if (rc != 1)
        goto error;

    if (!xBn || !yBn)
        goto error;

    int curveTypeNID;
    if (!CryptoNative_EvpPKeyGetEcGroupNid(pkey, &curveTypeNID) || !curveTypeNID)
        goto error;

    // Extract p, a, b
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_P, &pBn))
        goto error;

    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_A, &aBn))
        goto error;

    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_B, &bBn))
        goto error;

    // curveTypeNID will be always NID_X9_62_characteristic_two_field or NID_X9_62_prime_field
    group = EC_GROUP_new_by_curve_name(curveTypeNID);

    // In some cases EVP_PKEY_get_field_type can return NID_undef
    // and some providers seem to be ignoring OSSL_PKEY_PARAM_EC_FIELD_TYPE.
    // This is specifically true for tpm2 provider.
    // We can reliably get the field type from the EC_GROUP.
    int fieldTypeNID = EC_GROUP_get_field_type(group);

    *curveType = NIDToCurveType(fieldTypeNID);
    if (*curveType == Unspecified)
        goto error;

    if (!group)
        goto error;

    if (!EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_EC_GENERATOR, NULL, 0, &generatorBufferSize))
        goto error;

    generatorBuffer = (unsigned char*)OPENSSL_malloc(generatorBufferSize);
    if (!EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_EC_GENERATOR, generatorBuffer, generatorBufferSize, &generatorBufferSize))
        goto error;

    G = EC_POINT_new(group);

    if (!G)
        goto error;

    if (!EC_POINT_oct2point(group, G, generatorBuffer, generatorBufferSize, NULL))
        goto error;

    if (!EcPointGetAffineCoordinates(group, *curveType, G, xBn, yBn))
        goto error;

    // Extract order (n)
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_ORDER, &orderBn))
        goto error;

    // Extract cofactor (h)
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_COFACTOR, &cofactorBn))
        goto error;

    // Extract seed (optional)
    if (!seed || !EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_EC_SEED, NULL, 0, &sufficientSeedBufferSize))
    {
        *seed = NULL;
        *cbSeed = 0;
    }
    else
    {
        seedBuffer = (unsigned char*)OPENSSL_malloc(sufficientSeedBufferSize);
        size_t actualSeedSize;
        if (EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_EC_SEED, seedBuffer, sufficientSeedBufferSize, &actualSeedSize))
        {
            *cbSeed = SizeTToInt32(actualSeedSize);
            *seed = BN_bin2bn(seedBuffer, *cbSeed, NULL);

            if (!*seed)
            {
                *cbSeed = 0;
            }
        }
        else
        {
            *seed = NULL;
            *cbSeed = 0;
        }
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
    // Clear out variables from CryptoNative_EvpPKeyGetEcKeyParameters
    if (*qx) BN_free((BIGNUM*)*qx);
    if (*qy) BN_free(*qy);
    if (d && *d) BN_clear_free(*d);

    *cbQx = *cbQy = 0;
    *qx = *qy = NULL;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;

    // Clear our out variables
    *curveType = Unspecified;
    *cbP = *cbA = *cbB = *cbGx = *cbGy = *cbOrder = *cbCofactor = *cbSeed = 0;
    *p = *a = *b = *gx = *gy = *order = *cofactor = *seed = NULL;

    if (xBn) BN_free(xBn);
    if (yBn) BN_free(yBn);
    if (pBn) BN_free(pBn);
    if (aBn) BN_free(aBn);
    if (bBn) BN_free(bBn);
    if (orderBn) BN_free(orderBn);
    if (cofactorBn) BN_free(cofactorBn);

exit:
    // Clear out temporary variables
    if (group) EC_GROUP_free(group);
    if (generatorBuffer) OPENSSL_free(generatorBuffer);
    if (G) EC_POINT_free(G);
    if (seedBuffer) OPENSSL_free(seedBuffer);

    return rc;
#else
    (void)pkey;
    (void)includePrivate;
    *cbQx = *cbQy = 0;
    *qx = *qy = NULL;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;
    *curveType = Unspecified;
    *cbP = *cbA = *cbB = *cbGx = *cbGy = *cbOrder = *cbCofactor = *cbSeed = 0;
    *p = *a = *b = *gx = *gy = *order = *cofactor = *seed = NULL;
    return 0;
#endif
}
