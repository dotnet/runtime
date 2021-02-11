// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecc_import_export.h"
#include "pal_utilities.h"
#include "pal_jni.h"
#include "pal_eckey.h"
#include "pal_bignum.h"

static ECCurveType EcKeyGetCurveType(
    const EC_KEY* key)
{
    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (!group) return Unspecified;

    const EC_METHOD* method = EC_GROUP_method_of(group);
    if (!method) return Unspecified;

    return MethodToCurveType(method);
}

int32_t CryptoNative_GetECKeyParameters(
    const EC_KEY* key,
    int32_t includePrivate,
    jobject* qx, int32_t* cbQx,
    jobject* qy, int32_t* cbQy,
    jobject* d, int32_t* cbD)
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

#if HAVE_OPENSSL_EC2M
    if (API_EXISTS(EC_POINT_get_affine_coordinates_GF2m) && (curveType == Characteristic2))
    {
        if (!EC_POINT_get_affine_coordinates_GF2m(group, Q, xBn, yBn, NULL))
            goto error;
    }
    else
#endif
    {
        if (!EC_POINT_get_affine_coordinates_GFp(group, Q, xBn, yBn, NULL))
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
    jobject* qx, int32_t* cbQx,
    jobject* qy, int32_t* cbQy,
    jobject* d, int32_t* cbD,
    jobject* p, int32_t* cbP,
    jobject* a, int32_t* cbA,
    jobject* b, int32_t* cbB,
    jobject* gx, int32_t* cbGx,
    jobject* gy, int32_t* cbGy,
    jobject* order, int32_t* cbOrder,
    jobject* cofactor, int32_t* cbCofactor,
    jobject* seed, int32_t* cbSeed)
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

    // Get the public key parameters first in case any of its 'out' parameters are not initialized
    int32_t rc = CryptoNative_GetECKeyParameters(key, includePrivate, qx, cbQx, qy, cbQy, d, cbD);

    const EC_GROUP* group = NULL;
    const EC_POINT* G = NULL;
    const EC_METHOD* curveMethod = NULL;
    jobject xBn = NULL;
    jobject yBn = NULL;
    jobject pBn = NULL;
    jobject aBn = NULL;
    jobject bBn = NULL;
    jobject orderBn = NULL;
    jobject cofactorBn = NULL;
    jobject seedBn = NULL;

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

    // oid can be friendly name or value
    int nid = OBJ_txt2nid(oid);
    if (!nid)
        return -1;

    *key = EC_KEY_new_by_curve_name(nid);
    if (!(*key))
        return -1;

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

        if (!EC_KEY_set_public_key_affine_coordinates(*key, qxBn, qyBn))
            goto error;

        // Set private key (optional)
        if (d && dLength > 0)
        {
            dBn = BN_bin2bn(d, dLength, NULL);
            if (!dBn)
                goto error;

            if (!EC_KEY_set_private_key(*key, dBn))
                goto error;
        }

        // Validate key
        if (!EC_KEY_check_key(*key))
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

        if (!EC_KEY_set_private_key(*key, dBn))
            goto error;

        const EC_GROUP* group = EC_KEY_get0_group(*key);

        if (!group)
            goto error;

        pubG = EC_POINT_new(group);

        if (!pubG)
            goto error;

        if (!EC_POINT_mul(group, pubG, dBn, NULL, NULL, NULL))
            goto error;

        if (!EC_KEY_set_public_key(*key, pubG))
            goto error;

        if (!EC_KEY_check_key(*key))
            goto error;
    }

    // Success
    return 1;

error:
    if (qxBn) BN_free(qxBn);
    if (qyBn) BN_free(qyBn);
    if (dBn) BN_clear_free(dBn);
    if (pubG) EC_POINT_free(pubG);
    if (*key)
    {
        EC_KEY_free(*key);
        *key = NULL;
    }
    return 0;
}

// Converts a java.math.BigInteger to a positive int32_t value.
// Returns -1 if bigInteger < 0 or > INT32_MAX
int32_t CryptoNative_ConvertBigIntegerToPositiveInt32(JNIEnv* env, jobject bigInteger)
{
    jobject zero = (*env)->CallStaticObjectMethod(env, g_bigNumClass, g_valueOfMethod, (int64_t)0);
    int isPositive = (*env)->CallIntMethod(env, bigInteger, g_compareToMethod, zero);
    (*env)->DeleteLocalRef(env, zero);

    // bigInteger is negative.
    if (isPositive < 0)
    {
        return -1;
    }

    jobject int32MaxBigInt = (*env)->CallStaticObjectMethod(env, g_bigNumClass, g_valueOfMethod, (int64_t)INT32_MAX);
    int willOverflow = (*env)->CallIntMethod(env, bigInteger, g_compareToMethod, int32MaxBigInt);
    (*env)->DeleteLocalRef(env, int32MaxBigInt);
    
    // If bigInteger > int32MaxBigInt, then a conversion to int32_t would be lossy.
    if (willOverflow > 0)
    {
        return -1;
    }

    return (*env)->CallIntMethod(env, bigInteger, g_intValueMethod);
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
    
    JNIEnv* env = GetJNIEnv();

    EC_KEY* keyInfo = NULL;
    jobject keyPair = NULL;
    jobject G = NULL;
    jobject pubG = NULL;

    jobject qxBn = NULL;
    jobject qyBn = NULL;
    jobject dBn = NULL;
    jobject pBn = NULL; // p = either the char2 polynomial or the prime
    jobject aBn = NULL;
    jobject bBn = NULL;
    jobject gxBn = NULL;
    jobject gyBn = NULL;
    jobject orderBn = NULL;
    jobject cofactorBn = NULL;
    
    jobject field = NULL;
    jobject group = NULL;
    jobject paramSpec = NULL;
    jobject pubKeyPoint = NULL;
    jobject pubKeySpec = NULL;
    jobject privKeySpec = NULL;
    jbyteArray seedArray = NULL;
    jobject keyFactory = NULL;
    jobject publicKey = NULL;
    jobject privateKey = NULL;
    jstring algorithmName = NULL;

    // Java only supports Weierstrass and characteristic-2 type curves.
    if (curveType != PrimeShortWeierstrass && curveType != Characteristic2)
    {
        LOG_ERROR("Unuspported curve type specified: %d", curveType);
        return NULL;
    }

    pBn = CryptoNative_BigNumFromBinary(p, pLength);
    
    // At this point we should use 'goto error' since we allocated objects or memory

    if (curveType == PrimeShortWeierstrass)
    {
        field = (*env)->NewObject(env, g_ECFieldFpClass, g_ECFieldFpCtor, pBn);
    }
    else if (curveType == Characteristic2)
    {
        int m = -1;
        for (int localBitIndex = 7; localBitIndex >= 0; localBitIndex--)
        {
            int test = 1 << localBitIndex;

            if ((polynomial[0] & test) == test)
            {
                // TODO: test for overflow.
                m = 8 * lastIndex + localBitIndex;
            }
        }

        if (m == -1)
        {
            goto error;
        }

        field = (*env)->NewObject(env, g_ECFieldF2mClass, g_ECFieldF2mCtorWithCoefficientBigInteger, m, pBn);
    }

    aBn = CryptoNative_BigNumFromBinary(a, aLength);
    bBn = CryptoNative_BigNumFromBinary(b, bLength);
        
    if (seed && seedLength > 0)
    {
        seedArray = (*env)->NewByteArray(env, seedLength);
        (*env)->SetByteArrayRegion(env, seedArray, 0, seedLength, seed);
        group = (*env)->NewObject(env, g_EllipticCurveClass, g_EllipticCurveCtorWithSeed, field, aBn, bBn, seedArray);
    }
    else
    {
        group = (*env)->NewObject(g_EllipticCurveClass, g_EllipticCurveCtor, field, aBn, bBn);
    }

    // Set generator, order and cofactor
    gxBn = CryptoNative_BigNumFromBinary(gx, gxLength, NULL);
    gyBn = CryptoNative_BigNumFromBinary(gy, gyLength, NULL);
    G = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, gxBn, gyBn);

    orderBn = CryptoNative_BigNumFromBinary(order, orderLength, NULL);

    // Java ECC doesn't support BigInteger-based cofactor. It uses positive 32-bit integers.
    // So, convert the cofactor to a positive 32-bit integer with overflow protection.
    cofactorBn = CryptoNative_BigNumFromBinary(cofactor, cofactorLength, NULL);
    int cofactorInt = CryptoNative_ConvertBigIntegerToPositiveInt32(env, cofactorBn);

    if (cofactorInt == -1)
    {
        LOG_ERROR("Only cofactors less than %d are supported.", INT32_MAX);
        goto error;
    }

    paramSpec = (*env)->NewObject(env, g_ECParameterSpecClass, g_ECParameterSpecCtor, group, G, orderBn, cofactorInt);

    // Create the public and private key specs.
    if (qx && qy)
    {
        qxBn = CryptoNative_BigNumFromBinary(qx, qxLength);
        qyBn = CryptoNative_BigNumFromBinary(qy, qyLength);
        if (!qxBn || !qyBn)
            goto error;

        pubKeyPoint = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, qxBn, qyBn);
        pubKeySpec = (*env)->NewObject(env, g_ECPublicKeySpec, g_ECPublicKeySpecCtor, pubKeyPoint, paramSpec);

        // Set private key (optional)
        if (d && dLength)
        {
            dBn = CryptoNative_BigNumFromBinary(d, dLength);
            if (!dBn)
                goto error;

            privKeySpec = (*env)->NewObject(env, g_ECPrivateKeySpec, g_ECPrivateKeySpecCtor, dBn, paramSpec);
        }
    }
    // If we don't have the public key but we have the private key, we can
    // re-derive the public key from d.
    else if (qx == NULL && qy == NULL && qxLength == 0 && qyLength == 0 &&
             d && dLength > 0)
    {
        dBn = CryptoNative_BigNumFromBinary(d, dLength);
        if (!dBn)
            goto error;

        privKeySpec = (*env)->NewObject(env, g_ECPrivateKeySpec, g_ECPrivateKeySpecCtor, dBn, paramSpec);

        // Java doesn't have a public implementation of operations on points on an elliptic curve
        // so we can't yet derive a new public key from the private key and generator.
        LOG_ERROR("Deriving a new public EC key from a provided private EC key and curve is unsupported");
        goto error;

        // pubG = EC_POINT_new(group);

        // if (!pubG)
        //     goto error;

        // if (!EC_POINT_mul(group, pubG, dBn, NULL, NULL, NULL))
        //     goto error;

        // if (!EC_KEY_set_public_key(key, pubG))
        //     goto error;

        // if (!EC_KEY_check_key(key))
        //     goto error;
    }

    assert(pubKeySpec != NULL && privKeySpec != NULL);

    // Create the private and public keys and put them into a key pair.
    algorithmName = JSTRING("EC");
    keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algName);
    publicKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, pubKeySpec);
    privateKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPrivateMethod, privKeySpec);
    keyPair = (*env)->NewObject(env, g_keyPairClass, g_keyPairCtor, publicKey, privateKey);

    keyInfo = CryptoNative_NewEcKey(ToGRef(paramSpec), ToGRef(keyPair));

    goto cleanup;

error:
    if (privateKey)
    {
        // TODO: Destroy private key using the Destroyable interface.
    }

cleanup:
    CryptoNative_BigNumDestroy(qxBn);
    CryptoNative_BigNumDestroy(qyBn);
    CryptoNative_BigNumDestroy(dBn);
    CryptoNative_BigNumDestroy(pBn);
    CryptoNative_BigNumDestroy(aBn);
    CryptoNative_BigNumDestroy(bBn);
    CryptoNative_BigNumDestroy(gxBn);
    CryptoNative_BigNumDestroy(gyBn);
    CryptoNative_BigNumDestroy(orderBn);
    CryptoNative_BigNumDestroy(cofactorBn);
    if (G) (*env)->DeleteLocalRef(env, G);
    if (pubG) (*env)->DeleteLocalRef(env, pubG);
    if (group) (*env)->DeleteLocalRef(env, group);
    if (paramSpec) (*env)->DeleteLocalRef(env, paramSpec);
    if (pubKeyPoint) (*env)->DeleteLocalRef(env, pubKeyPoint);
    if (pubKeySpec) (*env)->DeleteLocalRef(env, pubKeySpec);
    if (privKeySpec) (*env)->DeleteLocalRef(env, privKeySpec);
    if (seedArray) (*env)->DeleteLocalRef(env, seedArray);
    if (field) (*env)->DeleteLocalRef(env, field);
    if (publicKey) (*env)->DeleteLocalRef(env, publicKey);
    if (privateKey) (*env)->DeleteLocalRef(env, privateKey);
    if (keyFactory) (*env)->DeleteLocalRef(env, keyFactory);
    if (keyPair) (*env)->DeleteLocalRef(env, keyPair);

    return keyInfo;
}