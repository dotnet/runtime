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
    if (curveType == Unspecified || !Q || !group) {
        rc = 5;
        goto error;
    }

    // Extract qx and qy
    xBn = BN_new();
    yBn = BN_new();
    if (!xBn || !yBn) {
        rc = 6;
        goto error;
    }

    if (!EcPointGetAffineCoordinates(group, curveType, Q, xBn, yBn)) {
        rc = 7;
        goto error;
    }

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
    {
        // Clear error queue - this is expected for explicit curve keys.
        ERR_clear_error();
        return 0;
    }

    *nidName = OBJ_txt2nid(curveName);
    return 1;
#else
    return 0;
#endif
}

int32_t CryptoNative_EvpPKeyEcHasExplicitEncoding(const EVP_PKEY* pkey)
{
    if (!pkey || EVP_PKEY_get_base_id(pkey) != EVP_PKEY_EC)
        return 0;

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_get_utf8_string_param))
    {
        return 0;
    }
#endif

#ifdef NEED_OPENSSL_3_0
    char encoding[32] = {0};
    if (!EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_EC_ENCODING, encoding, sizeof(encoding), NULL))
        return 0;

    return (strcmp(encoding, "explicit") == 0) ? 1 : 0;
#else
    return 0;
#endif
}

int32_t CryptoNative_EvpPKeyGetEcFieldDegree(const EVP_PKEY* pkey)
{
    if (!pkey || EVP_PKEY_get_base_id(pkey) != EVP_PKEY_EC)
        return 0;

#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_get_bn_param))
    {
        return 0;
    }
#endif

#ifdef NEED_OPENSSL_3_0
    char fieldType[32] = {0};
    if (!EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_EC_FIELD_TYPE, fieldType, sizeof(fieldType), NULL))
        return 0;

    BIGNUM* p = NULL;
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_P, &p) || !p)
        return 0;

    // For GF(2^m): p is the irreducible polynomial, degree = BN_num_bits(p) - 1.
    // For GF(p): degree = BN_num_bits(p).
    int degree = BN_num_bits(p);
    if (strcmp(fieldType, "characteristic-two-field") == 0)
        degree--;

    BN_free(p);
    return degree;
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
    if (!API_EXISTS(EVP_PKEY_get_octet_string_param))
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
    uint8_t* pubKeyBuf = NULL;

#ifdef NEED_OPENSSL_3_0
    // Ensure we have an EC key
    if (EVP_PKEY_get_base_id(pkey) != EVP_PKEY_EC)
        goto error;

    ERR_clear_error();

    if (includePrivate)
    {
        if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_PRIV_KEY, &dBn)) {
            rc = 12;
            goto error;
        }

        *d = dBn;
        *cbD = BN_num_bytes(dBn);
    }
    else
    {
        *d = NULL;
        *cbD = 0;
    }

    // Get the public key as an encoded point (may be compressed or uncompressed).
    // We use OSSL_PKEY_PARAM_PUB_KEY instead of OSSL_PKEY_PARAM_EC_PUB_X/Y
    // because the individual X/Y components may not be materialized yet.
    size_t pubKeyLen = 0;
    if (!EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_PUB_KEY, NULL, 0, &pubKeyLen))
    {
        rc = 5;
        goto error;
    }

    pubKeyBuf = (uint8_t*)calloc(pubKeyLen, 1);
    if (pubKeyBuf == NULL)
    {
        rc = 7;
        goto error;
    }

    if (!EVP_PKEY_get_octet_string_param(pkey, OSSL_PKEY_PARAM_PUB_KEY, pubKeyBuf, pubKeyLen, &pubKeyLen))
    {
        rc = 8;
        goto error;
    }

    {
        // Decode the encoded point (compressed or uncompressed) to extract X and Y.
        // Build an EC_GROUP from the key's parameters to perform the decoding.
        EC_GROUP* group = NULL;

        // Try named curve first.
        char curveName[80] = {0};
        if (EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_GROUP_NAME, curveName, sizeof(curveName), NULL))
        {
            int nid = OBJ_txt2nid(curveName);
            if (nid != NID_undef && API_EXISTS(EC_GROUP_new_by_curve_name))
            {
                group = EC_GROUP_new_by_curve_name(nid);
            }
        }

        if (group == NULL)
        {
            // Explicit curve — build EC_GROUP from the key's field params.
            BIGNUM* ecP = NULL;
            BIGNUM* ecA = NULL;
            BIGNUM* ecB = NULL;

            EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_P, &ecP);
            EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_A, &ecA);
            EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_B, &ecB);

            if (ecP == NULL || ecA == NULL || ecB == NULL)
            {
                BN_free(ecP);
                BN_free(ecA);
                BN_free(ecB);
                rc = 6;
                goto error;
            }

            char fieldType[64] = {0};
            int isChar2 = 0;

            if (EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_EC_FIELD_TYPE, fieldType, sizeof(fieldType), NULL))
            {
                isChar2 = (strcmp(fieldType, "characteristic-two-field") == 0);
            }

#if HAVE_OPENSSL_EC2M
            if (isChar2 && API_EXISTS(EC_GROUP_new_curve_GF2m))
            {
                group = EC_GROUP_new_curve_GF2m(ecP, ecA, ecB, NULL);
            }
            else
#endif
            {
                group = EC_GROUP_new_curve_GFp(ecP, ecA, ecB, NULL);
            }

            BN_free(ecP);
            BN_free(ecA);
            BN_free(ecB);

            if (group == NULL)
            {
                rc = 6;
                goto error;
            }
        }

        EC_POINT* point = EC_POINT_new(group);
        if (point == NULL ||
            !API_EXISTS(EC_POINT_oct2point) ||
            !EC_POINT_oct2point(group, point, pubKeyBuf, pubKeyLen, NULL))
        {
            EC_POINT_free(point);
            EC_GROUP_free(group);
            rc = 9;
            goto error;
        }

        xBn = BN_new();
        yBn = BN_new();

        if (xBn == NULL || yBn == NULL)
        {
            EC_POINT_free(point);
            EC_GROUP_free(group);
            rc = 10;
            goto error;
        }

        const EC_METHOD* groupMethod = EC_GROUP_method_of(group);
        ECCurveType groupCurveType = MethodToCurveType(groupMethod);

        if (!EcPointGetAffineCoordinates(group, groupCurveType, point, xBn, yBn))
        {
            EC_POINT_free(point);
            EC_GROUP_free(group);
            rc = 11;
            goto error;
        }

        EC_POINT_free(point);
        EC_GROUP_free(group);
    }

    *qx = xBn;
    *cbQx = BN_num_bytes(xBn);
    *qy = yBn;
    *cbQy = BN_num_bytes(yBn);

    free(pubKeyBuf);
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
    free(pubKeyBuf);
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
    int curveTypeNID;
    int fieldTypeNID;

    // Exit if CryptoNative_EvpPKeyGetEcKeyParameters failed
    if (rc != 1)
        goto error;

    if (!xBn || !yBn)
        goto error;

    if (!CryptoNative_EvpPKeyGetEcGroupNid(pkey, &curveTypeNID) || !curveTypeNID)
    {
        // For explicit curves, the group name may not be available.
        // Get the field type directly instead.
        char fieldTypeStr[32] = {0};
        if (!EVP_PKEY_get_utf8_string_param(pkey, OSSL_PKEY_PARAM_EC_FIELD_TYPE, fieldTypeStr, sizeof(fieldTypeStr), NULL))
            goto error;

        fieldTypeNID = OBJ_txt2nid(fieldTypeStr);
        if (fieldTypeNID == NID_undef)
            goto error;
    }
    else
    {
        // Named curve: create group to get field type.
        group = EC_GROUP_new_by_curve_name(curveTypeNID);
        if (!group)
            goto error;

        fieldTypeNID = EC_GROUP_get_field_type(group);
    }

    // Extract p, a, b
    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_P, &pBn))
        goto error;

    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_A, &aBn))
        goto error;

    if (!EVP_PKEY_get_bn_param(pkey, OSSL_PKEY_PARAM_EC_B, &bBn))
        goto error;

    *curveType = NIDToCurveType(fieldTypeNID);
    if (*curveType == Unspecified)
        goto error;

    // For explicit curves where group was not created from a curve name,
    // build it from the field parameters to decode the generator point.
    if (!group)
    {
        if (fieldTypeNID == NID_X9_62_characteristic_two_field)
            group = EC_GROUP_new_curve_GF2m(pBn, aBn, bBn, NULL);
        else
            group = EC_GROUP_new_curve_GFp(pBn, aBn, bBn, NULL);
    }

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

int32_t CryptoNative_EvpPKeyGenerateByEcKeyOid(
    EVP_PKEY** pkey,
    const char* oid)
{
    if (!pkey || !oid)
    {
        assert(false);
        return 0;
    }

    *pkey = NULL;

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (!API_EXISTS(EVP_PKEY_keygen))
    {
        return 0;
    }

    int nid = OBJ_txt2nid(oid);
    if (!nid)
    {
        return -1;
    }

    const char* groupName = OBJ_nid2sn(nid);
    if (!groupName)
    {
        return -1;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_from_name(NULL, "EC", NULL);
    if (ctx == NULL)
    {
        goto error;
    }

    if (EVP_PKEY_keygen_init(ctx) <= 0)
    {
        goto error;
    }

    if (EVP_PKEY_CTX_set_group_name(ctx, groupName) <= 0)
    {
        goto error;
    }

    if (EVP_PKEY_keygen(ctx, pkey) <= 0)
    {
        goto error;
    }

    EVP_PKEY_CTX_free(ctx);
    return 1;

error:
    if (ctx != NULL)
    {
        EVP_PKEY_CTX_free(ctx);
    }

    if (*pkey != NULL)
    {
        EVP_PKEY_free(*pkey);
        *pkey = NULL;
    }

    return 0;
#else
    (void)oid;
    return 0;
#endif
}

int32_t CryptoNative_EvpPKeyCreateByEcKeyParameters(
    EVP_PKEY** pkey,
    const char* oid,
    const uint8_t* qx, int32_t qxLength,
    const uint8_t* qy, int32_t qyLength,
    const uint8_t* d, int32_t dLength)
{
    if (!pkey || !oid)
    {
        assert(false);
        return 0;
    }

    *pkey = NULL;

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (!API_EXISTS(EVP_PKEY_fromdata) || !API_EXISTS(OSSL_PARAM_BLD_new))
    {
        return 0;
    }

    // Verify the OID is recognized before doing any work.
    int nid = OBJ_txt2nid(oid);
    if (!nid)
    {
        return -1;
    }

    // OBJ_nid2sn returns the short name OpenSSL expects for the group name param.
    const char* groupName = OBJ_nid2sn(nid);
    if (!groupName)
    {
        return -1;
    }

    EVP_PKEY_CTX* ctx = NULL;
    uint8_t* pubKeyBuf = NULL;
    OSSL_PARAM_BLD* bld = NULL;
    OSSL_PARAM* params = NULL;
    BIGNUM* dBn = NULL;

    bld = OSSL_PARAM_BLD_new();
    if (bld == NULL)
    {
        goto error;
    }

    if (!OSSL_PARAM_BLD_push_utf8_string(bld, OSSL_PKEY_PARAM_GROUP_NAME, groupName, 0))
    {
        goto error;
    }

    if (qx && qy)
    {
        // Build the uncompressed public key: 0x04 || qx || qy
        int32_t coordLen = qxLength > qyLength ? qxLength : qyLength;
        int32_t pubKeyLen = 1 + 2 * coordLen;
        pubKeyBuf = (uint8_t*)calloc(1, (size_t)pubKeyLen);

        if (pubKeyBuf == NULL)
        {
            goto error;
        }

        pubKeyBuf[0] = 0x04;
        memcpy(pubKeyBuf + 1 + (coordLen - qxLength), qx, (size_t)qxLength);
        memcpy(pubKeyBuf + 1 + coordLen + (coordLen - qyLength), qy, (size_t)qyLength);

        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_PUB_KEY, pubKeyBuf, (size_t)pubKeyLen))
        {
            goto error;
        }
    }
    else if (d && dLength > 0)
    {
        // No public key provided, derive Q = d * G using EC_GROUP/EC_POINT (not deprecated).
        EC_GROUP* group = EC_GROUP_new_by_curve_name(nid);
        if (group == NULL)
        {
            goto error;
        }

        EC_POINT* pubPoint = EC_POINT_new(group);
        BIGNUM* dBnTmp = BN_bin2bn(d, dLength, NULL);

        if (pubPoint == NULL || dBnTmp == NULL ||
            !EC_POINT_mul(group, pubPoint, dBnTmp, NULL, NULL, NULL))
        {
            BN_clear_free(dBnTmp);
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        BN_clear_free(dBnTmp);

        size_t pubKeyLen = EC_POINT_point2oct(group, pubPoint, POINT_CONVERSION_UNCOMPRESSED, NULL, 0, NULL);
        if (pubKeyLen == 0)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        pubKeyBuf = (uint8_t*)calloc(1, pubKeyLen);
        if (pubKeyBuf == NULL)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        if (EC_POINT_point2oct(group, pubPoint, POINT_CONVERSION_UNCOMPRESSED, pubKeyBuf, pubKeyLen, NULL) != pubKeyLen)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        EC_POINT_free(pubPoint);
        EC_GROUP_free(group);

        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_PUB_KEY, pubKeyBuf, pubKeyLen))
        {
            goto error;
        }
    }

    if (d && dLength > 0)
    {
        dBn = BN_bin2bn(d, dLength, NULL);
        if (dBn == NULL)
        {
            goto error;
        }

        if (!OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_PRIV_KEY, dBn))
        {
            goto error;
        }
    }

    params = OSSL_PARAM_BLD_to_param(bld);
    if (params == NULL)
    {
        goto error;
    }

    ctx = EVP_PKEY_CTX_new_from_name(NULL, "EC", NULL);
    if (ctx == NULL)
    {
        goto error;
    }

    if (EVP_PKEY_fromdata_init(ctx) != 1)
    {
        goto error;
    }

    {
        int selection = (d && dLength > 0) ? EVP_PKEY_KEYPAIR : EVP_PKEY_PUBLIC_KEY;
        if (EVP_PKEY_fromdata(ctx, pkey, selection, params) != 1)
        {
            goto error;
        }
    }

    OSSL_PARAM_free(params);
    OSSL_PARAM_BLD_free(bld);
    EVP_PKEY_CTX_free(ctx);
    BN_clear_free(dBn);
    free(pubKeyBuf);
    return 1;

error:
    OSSL_PARAM_free(params);
    OSSL_PARAM_BLD_free(bld);
    EVP_PKEY_CTX_free(ctx);
    BN_clear_free(dBn);
    free(pubKeyBuf);
    if (*pkey)
    {
        EVP_PKEY_free(*pkey);
        *pkey = NULL;
    }
    return 0;
#else
    (void)oid;
    (void)qx; (void)qxLength;
    (void)qy; (void)qyLength;
    (void)d; (void)dLength;
    return 0;
#endif
}

EVP_PKEY* CryptoNative_EvpPKeyCreateByEcExplicitParameters(
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
    const uint8_t* seed, int32_t seedLength)
{
    if (!p || !a || !b || !gx || !gy || !order || !cofactor)
    {
        assert(false);
        return NULL;
    }

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (!API_EXISTS(EVP_PKEY_fromdata) || !API_EXISTS(OSSL_PARAM_BLD_new) ||
        !API_EXISTS(EVP_PKEY_generate) || !API_EXISTS(EVP_PKEY_CTX_new_from_pkey))
    {
        return NULL;
    }

    EVP_PKEY* pkey = NULL;
    EVP_PKEY_CTX* ctx = NULL;
    OSSL_PARAM_BLD* bld = NULL;
    OSSL_PARAM* params = NULL;
    uint8_t* generatorBuf = NULL;
    uint8_t* pubKeyBuf = NULL;
    BIGNUM* pBn = NULL;
    BIGNUM* aBn = NULL;
    BIGNUM* bBn = NULL;
    BIGNUM* orderBn = NULL;
    BIGNUM* cofactorBn = NULL;
    BIGNUM* dBn = NULL;

    bld = OSSL_PARAM_BLD_new();
    if (bld == NULL)
    {
        goto error;
    }

    const char* fieldType = (curveType == Characteristic2)
        ? "characteristic-two-field"
        : "prime-field";

    if (!OSSL_PARAM_BLD_push_utf8_string(bld, OSSL_PKEY_PARAM_EC_FIELD_TYPE, fieldType, 0))
    {
        goto error;
    }

    if (!OSSL_PARAM_BLD_push_utf8_string(bld, OSSL_PKEY_PARAM_EC_ENCODING, "explicit", 0))
    {
        goto error;
    }

    pBn = BN_bin2bn(p, pLength, NULL);
    aBn = BN_bin2bn(a, aLength, NULL);
    bBn = BN_bin2bn(b, bLength, NULL);

    if (!pBn || !aBn || !bBn)
    {
        goto error;
    }

    if (!OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_EC_P, pBn) ||
        !OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_EC_A, aBn) ||
        !OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_EC_B, bBn))
    {
        goto error;
    }

    // Generator as uncompressed point: 0x04 || gx || gy
    {
        int32_t coordLen = gxLength > gyLength ? gxLength : gyLength;
        int32_t genLen = 1 + 2 * coordLen;
        generatorBuf = (uint8_t*)calloc(1, (size_t)genLen);
        if (generatorBuf == NULL)
        {
            goto error;
        }

        generatorBuf[0] = 0x04;
        memcpy(generatorBuf + 1 + (coordLen - gxLength), gx, (size_t)gxLength);
        memcpy(generatorBuf + 1 + coordLen + (coordLen - gyLength), gy, (size_t)gyLength);

        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_EC_GENERATOR, generatorBuf, (size_t)genLen))
        {
            goto error;
        }
    }

    orderBn = BN_bin2bn(order, orderLength, NULL);
    cofactorBn = BN_bin2bn(cofactor, cofactorLength, NULL);

    if (!orderBn || !cofactorBn)
    {
        goto error;
    }

    if (!OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_EC_ORDER, orderBn) ||
        !OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_EC_COFACTOR, cofactorBn))
    {
        goto error;
    }

    if (seed && seedLength > 0)
    {
        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_EC_SEED, seed, (size_t)seedLength))
        {
            goto error;
        }
    }

    if (d && dLength > 0)
    {
        dBn = BN_bin2bn(d, dLength, NULL);
        if (dBn == NULL)
        {
            goto error;
        }

        if (!OSSL_PARAM_BLD_push_BN(bld, OSSL_PKEY_PARAM_PRIV_KEY, dBn))
        {
            goto error;
        }
    }

    if (qx && qy)
    {
        int32_t coordLen = qxLength > qyLength ? qxLength : qyLength;
        int32_t pubKeyLen = 1 + 2 * coordLen;
        pubKeyBuf = (uint8_t*)calloc(1, (size_t)pubKeyLen);
        if (pubKeyBuf == NULL)
        {
            goto error;
        }

        pubKeyBuf[0] = 0x04;
        memcpy(pubKeyBuf + 1 + (coordLen - qxLength), qx, (size_t)qxLength);
        memcpy(pubKeyBuf + 1 + coordLen + (coordLen - qyLength), qy, (size_t)qyLength);

        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_PUB_KEY, pubKeyBuf, (size_t)pubKeyLen))
        {
            goto error;
        }
    }
    else if (d && dLength > 0)
    {
        // No public key provided, derive Q = d * G from the explicit curve parameters.
        const EC_METHOD* curveMethod = CurveTypeToMethod(curveType);
        if (!curveMethod)
        {
            goto error;
        }

        EC_GROUP* group = EC_GROUP_new(curveMethod);
        if (group == NULL)
        {
            goto error;
        }

#if HAVE_OPENSSL_EC2M
        if (API_EXISTS(EC_GROUP_set_curve_GF2m) && (curveType == Characteristic2))
        {
            if (!EC_GROUP_set_curve_GF2m(group, pBn, aBn, bBn, NULL))
            {
                EC_GROUP_free(group);
                goto error;
            }
        }
        else
#endif
        {
            if (!EC_GROUP_set_curve_GFp(group, pBn, aBn, bBn, NULL))
            {
                EC_GROUP_free(group);
                goto error;
            }
        }

        // Set the generator
        EC_POINT* G = EC_POINT_new(group);
        BIGNUM* gxBn = BN_bin2bn(gx, gxLength, NULL);
        BIGNUM* gyBn = BN_bin2bn(gy, gyLength, NULL);

        if (G == NULL || gxBn == NULL || gyBn == NULL)
        {
            BN_free(gxBn);
            BN_free(gyBn);
            EC_POINT_free(G);
            EC_GROUP_free(group);
            goto error;
        }

#if HAVE_OPENSSL_EC2M
        if (API_EXISTS(EC_POINT_set_affine_coordinates_GF2m) && (curveType == Characteristic2))
        {
            if (!EC_POINT_set_affine_coordinates_GF2m(group, G, gxBn, gyBn, NULL))
            {
                BN_free(gxBn);
                BN_free(gyBn);
                EC_POINT_free(G);
                EC_GROUP_free(group);
                goto error;
            }
        }
        else
#endif
        {
            if (!EC_POINT_set_affine_coordinates_GFp(group, G, gxBn, gyBn, NULL))
            {
                BN_free(gxBn);
                BN_free(gyBn);
                EC_POINT_free(G);
                EC_GROUP_free(group);
                goto error;
            }
        }

        BN_free(gxBn);
        BN_free(gyBn);

        EC_GROUP_set_generator(group, G, orderBn, cofactorBn);
        EC_POINT_free(G);

        // Derive Q = d * G
        EC_POINT* pubPoint = EC_POINT_new(group);
        if (pubPoint == NULL ||
            !EC_POINT_mul(group, pubPoint, dBn, NULL, NULL, NULL))
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        size_t pubKeyLen = EC_POINT_point2oct(group, pubPoint, POINT_CONVERSION_UNCOMPRESSED, NULL, 0, NULL);
        if (pubKeyLen == 0)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        pubKeyBuf = (uint8_t*)calloc(1, pubKeyLen);
        if (pubKeyBuf == NULL)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        if (EC_POINT_point2oct(group, pubPoint, POINT_CONVERSION_UNCOMPRESSED, pubKeyBuf, pubKeyLen, NULL) != pubKeyLen)
        {
            EC_POINT_free(pubPoint);
            EC_GROUP_free(group);
            goto error;
        }

        EC_POINT_free(pubPoint);
        EC_GROUP_free(group);

        if (!OSSL_PARAM_BLD_push_octet_string(bld, OSSL_PKEY_PARAM_PUB_KEY, pubKeyBuf, pubKeyLen))
        {
            goto error;
        }
    }

    params = OSSL_PARAM_BLD_to_param(bld);
    if (params == NULL)
    {
        goto error;
    }

    ctx = EVP_PKEY_CTX_new_from_name(NULL, "EC", NULL);
    if (ctx == NULL)
    {
        goto error;
    }

    if ((!qx || !qy) && (!d || dLength <= 0))
    {
        // No key material — generate a new key from the domain parameters.
        EVP_PKEY* templateKey = NULL;

        if (EVP_PKEY_fromdata_init(ctx) != 1)
        {
            goto error;
        }

        if (EVP_PKEY_fromdata(ctx, &templateKey, EVP_PKEY_KEY_PARAMETERS, params) != 1)
        {
            goto error;
        }

        EVP_PKEY_CTX_free(ctx);
        ctx = EVP_PKEY_CTX_new_from_pkey(NULL, templateKey, NULL);
        EVP_PKEY_free(templateKey);

        if (ctx == NULL)
        {
            goto error;
        }

        if (EVP_PKEY_keygen_init(ctx) != 1)
        {
            goto error;
        }

        if (EVP_PKEY_generate(ctx, &pkey) != 1)
        {
            goto error;
        }
    }
    else
    {
        if (EVP_PKEY_fromdata_init(ctx) != 1)
        {
            goto error;
        }

        int selection = (d && dLength > 0) ? EVP_PKEY_KEYPAIR : EVP_PKEY_PUBLIC_KEY;
        if (EVP_PKEY_fromdata(ctx, &pkey, selection, params) != 1)
        {
            goto error;
        }
    }

    OSSL_PARAM_free(params);
    OSSL_PARAM_BLD_free(bld);
    EVP_PKEY_CTX_free(ctx);
    free(generatorBuf);
    free(pubKeyBuf);
    BN_free(pBn);
    BN_free(aBn);
    BN_free(bBn);
    BN_free(orderBn);
    BN_free(cofactorBn);
    BN_clear_free(dBn);
    return pkey;

error:
    OSSL_PARAM_free(params);
    OSSL_PARAM_BLD_free(bld);
    EVP_PKEY_CTX_free(ctx);
    free(generatorBuf);
    free(pubKeyBuf);
    BN_free(pBn);
    BN_free(aBn);
    BN_free(bBn);
    BN_free(orderBn);
    BN_free(cofactorBn);
    BN_clear_free(dBn);
    if (pkey) EVP_PKEY_free(pkey);
    return NULL;
#else
    (void)curveType;
    (void)qx; (void)qxLength; (void)qy; (void)qyLength;
    (void)d; (void)dLength;
    (void)p; (void)pLength; (void)a; (void)aLength; (void)b; (void)bLength;
    (void)gx; (void)gxLength; (void)gy; (void)gyLength;
    (void)order; (void)orderLength; (void)cofactor; (void)cofactorLength;
    (void)seed; (void)seedLength;
    return NULL;
#endif
}
