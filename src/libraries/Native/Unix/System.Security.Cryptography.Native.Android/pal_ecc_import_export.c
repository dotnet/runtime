// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecc_import_export.h"
#include "pal_utilities.h"
#include "pal_jni.h"
#include "pal_eckey.h"
#include "pal_bignum.h"

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

    JNIEnv* env = GetJNIEnv();

    // Get the public key
    jobject publicKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPublicMethod);

    jobject Q = (*env)->CallObjectMethod(env, publicKey, g_ECPublicKeyGetW);

    (*env)->DeleteLocalRef(env, publicKey);

    jobject xBn = (*env)->CallObjectMethod(env, Q, g_ECPointGetAffineX);
    jobject yBn = (*env)->CallObjectMethod(env, Q, g_ECPointGetAffineY);
    
    (*env)->DeleteLocalRef(env, Q);

    // Success; assign variables
    *qx = ToGRef(env, xBn); *cbQx = CryptoNative_GetBigNumBytes(xBn); xBn = NULL;
    *qy = ToGRef(env, yBn); *cbQy = CryptoNative_GetBigNumBytes(yBn); yBn = NULL;
    if (*cbQx == FAIL || *cbQy == FAIL) goto error;

    if (includePrivate)
    {
        jobject privateKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPrivateMethod);

        jobject dBn = (*env)->CallObjectMethod(env, privateKey, g_ECPrivateKeyGetS);

        (*env)->DeleteLocalRef(env, privateKey);

        *d = ToGRef(env, dBn);
        *cbD = CryptoNative_GetBigNumBytes(*d);
        if (*cbD == FAIL) goto error;
    }
    else
    {
        if (d)
            *d = NULL;

        if (cbD)
            *cbD = 0;
    }

    return SUCCESS;

error:
    *cbQx = *cbQy = 0;
    *qx = *qy = 0;
    if (d) *d = NULL;
    if (cbD) *cbD = 0;
    if (xBn) (*env)->DeleteLocalRef(env, xBn);
    if (yBn) (*env)->DeleteLocalRef(env, yBn);
    return FAIL;
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

    JNIEnv* env = GetJNIEnv();

    jobject group = NULL;
    jobject G = NULL;
    jobject field = NULL;
    jobject xBn = NULL;
    jobject yBn = NULL;
    jobject pBn = NULL;
    jobject aBn = NULL;
    jobject bBn = NULL;
    jobject orderBn = NULL;
    jobject cofactorBn = NULL;
    jbyteArray seedArray = NULL;
    jobject seedBn = NULL;

    // Exit if CryptoNative_GetECKeyParameters failed
    if (rc != SUCCESS)
        goto error;

    group = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurve);

    aBn = (*env)->CallObjectMethod(env, group, g_EllipticCurveGetA);
    aBn = (*env)->CallObjectMethod(env, group, g_EllipticCurveGetB);
    field = (*env)->CallObjectMethod(env, group, g_EllipticCurveGetField);

    if ((*env)->IsInstanceOf(env, field, g_ECFieldF2mClass))
    {
        *curveType = Characteristic2;
        // Get the reduction polynomial p
        pBn = (*env)->CallObjectMethod(env, field, g_ECFieldF2mGetReductionPolynomial);
    }
    else
    {
        assert((*env)->IsInstanceOf(env, field, g_ECFieldFpClass));
        *curveType = PrimeShortWeierstrass;
        // Get the prime p
        pBn = (*env)->CallObjectMethod(env, field, g_ECFieldFpGetP);
    }

    // Extract gx and gy
    G = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetGenerator);
    xBn = (*env)->CallObjectMethod(env, G, g_ECPointGetAffineX);
    yBn = (*env)->CallObjectMethod(env, G, g_ECPointGetAffineY);

    // Extract order (n)
    orderBn = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetOrder);

    // Extract cofactor (h)
    int32_t cofactorInt = (*env)->CallIntMethod(env, key->curveParameters, g_ECParameterSpecGetCofactor);

    cofactorBn = (*env)->CallStaticObjectMethod(env, g_bigNumClass, g_valueOfMethod, (int64_t)cofactorInt);

    // Extract seed (optional)
    seedArray = (*env)->CallObjectMethod(env, group, g_EllipticCurveGetSeed);
    if (seedArray)
    {
        seedBn = (*env)->NewObject(env, g_bigNumClass, g_bigNumCtor, seedArray);

        *seed = ToGRef(env, seedBn);
        *cbSeed = CryptoNative_GetBigNumBytes(seedBn);

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
    *gx = ToGRef(env, xBn); *cbGx = CryptoNative_GetBigNumBytes(xBn);
    *gy = ToGRef(env, yBn); *cbGy = CryptoNative_GetBigNumBytes(yBn);
    *p = ToGRef(env, pBn); *cbP = CryptoNative_GetBigNumBytes(pBn);
    *a = ToGRef(env, aBn); *cbA = CryptoNative_GetBigNumBytes(aBn);
    *b = ToGRef(env, bBn); *cbB = CryptoNative_GetBigNumBytes(bBn);
    *order = ToGRef(env, orderBn); *cbOrder = CryptoNative_GetBigNumBytes(orderBn);
    *cofactor = ToGRef(env, cofactorBn); *cbCofactor = CryptoNative_GetBigNumBytes(cofactorBn);

    rc = SUCCESS;
    
    if (xBn) (*env)->DeleteLocalRef(env, xBn);
    if (yBn) (*env)->DeleteLocalRef(env, yBn);
    if (pBn) (*env)->DeleteLocalRef(env, pBn);
    if (aBn) (*env)->DeleteLocalRef(env, aBn);
    if (bBn) (*env)->DeleteLocalRef(env, bBn);
    if (orderBn) (*env)->DeleteLocalRef(env, orderBn);
    if (cofactorBn) (*env)->DeleteLocalRef(env, cofactorBn);
    if (seedBn) (*env)->DeleteLocalRef(env, seedBn);

    goto exit;

error:
    // Clear out variables from CryptoNative_GetECKeyParameters
    *cbQx = *cbQy = 0;
    ReleaseGRef(env, *qx);
    ReleaseGRef(env, *qy);
    *qx = *qy = NULL;
    if (d)
    {
        ReleaseGRef(env, *d);
        *d = NULL;
    }
    if (cbD) *cbD = 0;

    // Clear our out variables
    *curveType = Unspecified;
    *cbP = *cbA = *cbB = *cbGx = *cbGy = *cbOrder = *cbCofactor = *cbSeed = 0;
    ReleaseGRef(env, *p);
    ReleaseGRef(env, *a);
    ReleaseGRef(env, *b);
    ReleaseGRef(env, *gx);
    ReleaseGRef(env, *gy);
    ReleaseGRef(env, *order);
    ReleaseGRef(env, *cofactor);
    ReleaseGRef(env, *seed);
    *p = *a = *b = *gx = *gy = *order = *cofactor = *seed = NULL;

    if (xBn) (*env)->DeleteLocalRef(env, xBn);
    if (yBn) (*env)->DeleteLocalRef(env, yBn);
    if (pBn) (*env)->DeleteLocalRef(env, pBn);
    if (aBn) (*env)->DeleteLocalRef(env, aBn);
    if (bBn) (*env)->DeleteLocalRef(env, bBn);
    if (orderBn) (*env)->DeleteLocalRef(env, orderBn);
    if (cofactorBn) (*env)->DeleteLocalRef(env, cofactorBn);
    if (seedBn) (*env)->DeleteLocalRef(env, seedBn);

exit:
    return rc;
}

static jobject CryptoNative_CreateKeyPairFromCurveParameters(jobject curveParameters, uint8_t* qx, int32_t qxLength, uint8_t* qy, int32_t qyLength, uint8_t* d, int32_t dLength)
{
    JNIEnv* env = GetJNIEnv();

    jobject dBn = NULL;
    jobject qxBn = NULL;
    jobject qyBn = NULL;
    jobject pubG = NULL;
    jobject pubKeyPoint = NULL;
    jobject pubKeySpec = NULL;
    jobject privKeySpec = NULL;
    jobject algorithmName = NULL;
    jobject keyFactory = NULL;
    jobject publicKey = NULL;
    jobject privateKey = NULL;
    jobject keyPair = NULL;

    // Create the public and private key specs.
    if (qx && qy)
    {
        qxBn = CryptoNative_BigNumFromBinary(qx, qxLength);
        qyBn = CryptoNative_BigNumFromBinary(qy, qyLength);
        if (!qxBn || !qyBn)
            goto error;

        pubKeyPoint = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, qxBn, qyBn);
        pubKeySpec = (*env)->NewObject(env, g_ECPublicKeySpecClass, g_ECPublicKeySpecCtor, pubKeyPoint, curveParameters);

        // Set private key (optional)
        if (d && dLength)
        {
            dBn = CryptoNative_BigNumFromBinary(d, dLength);
            if (!dBn)
                goto error;

            privKeySpec = (*env)->NewObject(env, g_ECPrivateKeySpecClass, g_ECPrivateKeySpecCtor, dBn, curveParameters);
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

        privKeySpec = (*env)->NewObject(env, g_ECPrivateKeySpecClass, g_ECPrivateKeySpecCtor, dBn, curveParameters);

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
    keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, algorithmName);
    publicKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPublicMethod, pubKeySpec);
    privateKey = (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGenPrivateMethod, privKeySpec);
    keyPair = (*env)->NewObject(env, g_keyPairClass, g_keyPairCtor, publicKey, privateKey);

    goto cleanup;

error:
    if (privateKey)
    {
        // Destroy the private key data.
        (*env)->CallVoidMethod(env, privateKey, g_destroy);
        CheckJNIExceptions(env); // The destroy call might throw an exception. Clear the exception state.
    }

cleanup:
    CryptoNative_BigNumDestroy(qxBn);
    CryptoNative_BigNumDestroy(qyBn);
    CryptoNative_BigNumDestroy(dBn);
    if (pubG) (*env)->DeleteLocalRef(env, pubG);
    if (pubKeyPoint) (*env)->DeleteLocalRef(env, pubKeyPoint);
    if (pubKeySpec) (*env)->DeleteLocalRef(env, pubKeySpec);
    if (privKeySpec) (*env)->DeleteLocalRef(env, privKeySpec);
    if (publicKey) (*env)->DeleteLocalRef(env, publicKey);
    if (privateKey) (*env)->DeleteLocalRef(env, privateKey);
    if (keyFactory) (*env)->DeleteLocalRef(env, keyFactory);
    if (algorithmName) (*env)->DeleteLocalRef(env, algorithmName);
    return ToGRef(env, keyPair);
}

int32_t CryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key, const char* oid, uint8_t* qx, int32_t qxLength, uint8_t* qy, int32_t qyLength, uint8_t* d, int32_t dLength)
{
    if (!key || !oid)
    {
        assert(false);
        return 0;
    }

    *key = NULL;

    JNIEnv* env = GetJNIEnv();

    // The easiest way to create explicit keys with a named curve is to generate
    // new keys for the curve, pull out the explicit paramters, and then create the explicit keys.
    *key = CryptoNative_EcKeyCreateByOid(oid);
    if (*key == NULL)
    {
        return FAIL;
    }
    
    // Release the reference to the generated key pair. We're going to make our own with the explicit keys.
    ReleaseGRef(env, (*key)->keyPair);
    (*key)->keyPair = CryptoNative_CreateKeyPairFromCurveParameters((*key)->curveParameters, qx, qxLength, qy, qyLength, d, dLength);
    
    if ((*key)->keyPair == NULL)
    {
        // We were unable to make the keys, so clean up and return FAIL.
        ReleaseGRef(env, (*key)->curveParameters);
        CryptoNative_EcKeyDestroy(*key);
        *key = NULL;
        return FAIL;
    }
    
    return SUCCESS;
}

// Converts a java.math.BigInteger to a positive int32_t value.
// Returns -1 if bigInteger < 0 or > INT32_MAX
static int32_t CryptoNative_ConvertBigIntegerToPositiveInt32(JNIEnv* env, jobject bigInteger)
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
    jbyteArray seedArray = NULL;

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
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }
    else if (curveType == Characteristic2)
    {
        int32_t m = -1;
        for (int localBitIndex = 7; localBitIndex >= 0; localBitIndex--)
        {
            int test = 1 << localBitIndex;

            if ((p[0] & test) == test)
            {
                // TODO: test for overflow.
                m = 8 * (pLength - 1) + localBitIndex;
            }
        }

        if (m == -1)
        {
            goto error;
        }

        field = (*env)->NewObject(env, g_ECFieldF2mClass, g_ECFieldF2mCtorWithCoefficientBigInteger, m, pBn);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }

    aBn = CryptoNative_BigNumFromBinary(a, aLength);
    bBn = CryptoNative_BigNumFromBinary(b, bLength);
        
    if (seed && seedLength > 0)
    {
        seedArray = (*env)->NewByteArray(env, seedLength);
        (*env)->SetByteArrayRegion(env, seedArray, 0, seedLength, (jbyte*)seed);
        group = (*env)->NewObject(env, g_EllipticCurveClass, g_EllipticCurveCtorWithSeed, field, aBn, bBn, seedArray);
    }
    else
    {
        group = (*env)->NewObject(g_EllipticCurveClass, g_EllipticCurveCtor, field, aBn, bBn);
    }

    // Set generator, order and cofactor
    gxBn = CryptoNative_BigNumFromBinary(gx, gxLength);
    gyBn = CryptoNative_BigNumFromBinary(gy, gyLength);
    G = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, gxBn, gyBn);

    orderBn = CryptoNative_BigNumFromBinary(order, orderLength);

    // Java ECC doesn't support BigInteger-based cofactor. It uses positive 32-bit integers.
    // So, convert the cofactor to a positive 32-bit integer with overflow protection.
    cofactorBn = CryptoNative_BigNumFromBinary(cofactor, cofactorLength);
    int cofactorInt = CryptoNative_ConvertBigIntegerToPositiveInt32(env, cofactorBn);

    if (cofactorInt == -1)
    {
        LOG_ERROR("Only positive cofactors less than %d are supported.", INT32_MAX);
        goto error;
    }

    paramSpec = (*env)->NewObject(env, g_ECParameterSpecClass, g_ECParameterSpecCtor, group, G, orderBn, cofactorInt);

    keyPair = CryptoNative_CreateKeyPairFromCurveParameters(paramSpec, qx, qxLength, qy, qyLength, d, dLength);

    if (!keyPair) goto error;

    keyInfo = CryptoNative_NewEcKey(ToGRef(env, paramSpec), keyPair);

error:
    CryptoNative_BigNumDestroy(pBn);
    CryptoNative_BigNumDestroy(aBn);
    CryptoNative_BigNumDestroy(bBn);
    CryptoNative_BigNumDestroy(gxBn);
    CryptoNative_BigNumDestroy(gyBn);
    CryptoNative_BigNumDestroy(orderBn);
    CryptoNative_BigNumDestroy(cofactorBn);
    if (G) (*env)->DeleteLocalRef(env, G);
    if (group) (*env)->DeleteLocalRef(env, group);
    if (paramSpec) (*env)->DeleteLocalRef(env, paramSpec);
    if (seedArray) (*env)->DeleteLocalRef(env, seedArray);
    if (field) (*env)->DeleteLocalRef(env, field);

    return keyInfo;
}
