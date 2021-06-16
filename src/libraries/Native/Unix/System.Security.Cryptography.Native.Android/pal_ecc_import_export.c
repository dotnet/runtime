// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecc_import_export.h"
#include "pal_bignum.h"
#include "pal_eckey.h"
#include "pal_jni.h"
#include "pal_utilities.h"
#include "pal_misc.h"

int32_t AndroidCryptoNative_GetECKeyParameters(const EC_KEY* key,
                                        int32_t includePrivate,
                                        jobject* qx,
                                        int32_t* cbQx,
                                        jobject* qy,
                                        int32_t* cbQy,
                                        jobject* d,
                                        int32_t* cbD)
{
    abort_if_invalid_pointer_argument (qx);
    abort_if_invalid_pointer_argument (cbQx);
    abort_if_invalid_pointer_argument (qy);
    abort_if_invalid_pointer_argument (cbQy);
    abort_if_invalid_pointer_argument (d);
    abort_if_invalid_pointer_argument (cbD);

    JNIEnv* env = GetJNIEnv();

    // Get the public key
    jobject publicKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPublicMethod);

    jobject Q = (*env)->CallObjectMethod(env, publicKey, g_ECPublicKeyGetW);

    (*env)->DeleteLocalRef(env, publicKey);

    jobject xBn = (*env)->CallObjectMethod(env, Q, g_ECPointGetAffineX);
    jobject yBn = (*env)->CallObjectMethod(env, Q, g_ECPointGetAffineY);

    (*env)->DeleteLocalRef(env, Q);

    // Success; assign variables
    *qx = ToGRef(env, xBn);
    *cbQx = AndroidCryptoNative_GetBigNumBytes(*qx);
    xBn = NULL;
    *qy = ToGRef(env, yBn);
    *cbQy = AndroidCryptoNative_GetBigNumBytes(*qy);
    yBn = NULL;
    if (*cbQx == FAIL || *cbQy == FAIL)
        goto error;

    if (includePrivate)
    {
        abort_if_invalid_pointer_argument (d);

        jobject privateKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPrivateMethod);

        if (!privateKey)
        {
            *d = NULL;
            *cbD = 0;
            goto error;
        }

        jobject dBn = (*env)->CallObjectMethod(env, privateKey, g_ECPrivateKeyGetS);

        (*env)->DeleteLocalRef(env, privateKey);

        *d = ToGRef(env, dBn);
        *cbD = AndroidCryptoNative_GetBigNumBytes(*d);
        if (*cbD == FAIL)
            goto error;
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
    if (d)
        *d = NULL;
    if (cbD)
        *cbD = 0;
    return FAIL;
}

int32_t AndroidCryptoNative_GetECCurveParameters(const EC_KEY* key,
                                          int32_t includePrivate,
                                          ECCurveType* curveType,
                                          jobject* qx,
                                          int32_t* cbQx,
                                          jobject* qy,
                                          int32_t* cbQy,
                                          jobject* d,
                                          int32_t* cbD,
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
                                          int32_t* cbSeed)
{
    abort_if_invalid_pointer_argument (p);
    abort_if_invalid_pointer_argument (cbP);
    abort_if_invalid_pointer_argument (a);
    abort_if_invalid_pointer_argument (cbA);
    abort_if_invalid_pointer_argument (b);
    abort_if_invalid_pointer_argument (cbB);
    abort_if_invalid_pointer_argument (gx);
    abort_if_invalid_pointer_argument (cbGx);
    abort_if_invalid_pointer_argument (gy);
    abort_if_invalid_pointer_argument (cbGy);
    abort_if_invalid_pointer_argument (order);
    abort_if_invalid_pointer_argument (cbOrder);
    abort_if_invalid_pointer_argument (cofactor);
    abort_if_invalid_pointer_argument (cbCofactor);
    abort_if_invalid_pointer_argument (seed);
    abort_if_invalid_pointer_argument (cbSeed);

    // Get the public key parameters first in case any of its 'out' parameters are not initialized
    int32_t rc = AndroidCryptoNative_GetECKeyParameters(key, includePrivate, qx, cbQx, qy, cbQy, d, cbD);

    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, group, G, field, seedArray);

    INIT_LOCALS(bn, X, Y, P, A, B, ORDER, COFACTOR, SEED);

    // Exit if AndroidCryptoNative_GetECKeyParameters failed
    if (rc != SUCCESS)
        goto error;

    loc[group] = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurve);

    bn[A] = (*env)->CallObjectMethod(env, loc[group], g_EllipticCurveGetA);
    bn[B] = (*env)->CallObjectMethod(env, loc[group], g_EllipticCurveGetB);
    loc[field] = (*env)->CallObjectMethod(env, loc[group], g_EllipticCurveGetField);

    if ((*env)->IsInstanceOf(env, loc[field], g_ECFieldF2mClass))
    {
        *curveType = Characteristic2;
        // Get the reduction polynomial p
        bn[P] = (*env)->CallObjectMethod(env, loc[field], g_ECFieldF2mGetReductionPolynomial);
    }
    else
    {
        abort_unless((*env)->IsInstanceOf(env, loc[field], g_ECFieldFpClass), "Must be an instance of java.security.spec.ECFieldFp");
        *curveType = PrimeShortWeierstrass;
        // Get the prime p
        bn[P] = (*env)->CallObjectMethod(env, loc[field], g_ECFieldFpGetP);
    }

    // Extract gx and gy
    loc[G] = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetGenerator);
    bn[X] = (*env)->CallObjectMethod(env, loc[G], g_ECPointGetAffineX);
    bn[Y] = (*env)->CallObjectMethod(env, loc[G], g_ECPointGetAffineY);

    // Extract order (n)
    bn[ORDER] = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetOrder);

    // Extract cofactor (h)
    int32_t cofactorInt = (*env)->CallIntMethod(env, key->curveParameters, g_ECParameterSpecGetCofactor);

    bn[COFACTOR] = (*env)->CallStaticObjectMethod(env, g_bigNumClass, g_valueOfMethod, (int64_t)cofactorInt);

    // Extract seed (optional)
    loc[seedArray] = (*env)->CallObjectMethod(env, loc[group], g_EllipticCurveGetSeed);
    if (loc[seedArray])
    {
        bn[SEED] = (*env)->NewObject(env, g_bigNumClass, g_bigNumCtorWithSign, 1, loc[seedArray]);

        *seed = ToGRef(env, bn[SEED]);
        *cbSeed = AndroidCryptoNative_GetBigNumBytes(*seed);
    }
    else
    {
        *seed = NULL;
        *cbSeed = 0;
    }

    // Success; assign variables
    *gx = ToGRef(env, bn[X]);
    *cbGx = AndroidCryptoNative_GetBigNumBytes(*gx);
    *gy = ToGRef(env, bn[Y]);
    *cbGy = AndroidCryptoNative_GetBigNumBytes(*gy);
    *p = ToGRef(env, bn[P]);
    *cbP = AndroidCryptoNative_GetBigNumBytes(*p);
    *a = ToGRef(env, bn[A]);
    *cbA = AndroidCryptoNative_GetBigNumBytes(*a);
    *b = ToGRef(env, bn[B]);
    *cbB = AndroidCryptoNative_GetBigNumBytes(*b);
    *order = ToGRef(env, bn[ORDER]);
    *cbOrder = AndroidCryptoNative_GetBigNumBytes(*order);
    *cofactor = ToGRef(env, bn[COFACTOR]);
    *cbCofactor = AndroidCryptoNative_GetBigNumBytes(*cofactor);

    rc = SUCCESS;

    goto exit;

error:
    // Clear out variables from AndroidCryptoNative_GetECKeyParameters
    *cbQx = *cbQy = 0;
    ReleaseGRef(env, *qx);
    ReleaseGRef(env, *qy);
    *qx = *qy = NULL;
    if (d)
    {
        ReleaseGRef(env, *d);
        *d = NULL;
    }
    if (cbD)
        *cbD = 0;

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

    // Clear local BigInteger instances. On success, these are converted to global
    // references for the out variables, so the local release is only on error.
    RELEASE_LOCALS_ENV(bn, ReleaseLRef);

exit:
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return rc;
}

static jobject AndroidCryptoNative_CreateKeyPairFromCurveParameters(
    jobject curveParameters, uint8_t* qx, int32_t qxLength, uint8_t* qy, int32_t qyLength, uint8_t* d, int32_t dLength)
{
    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(bn, D, QX, QY);
    INIT_LOCALS(loc, pubG, pubKeyPoint, pubKeySpec, privKeySpec, algorithmName, keyFactory, publicKey, privateKey);

    jobject keyPair = NULL;

    // Create the public and private key specs.
    if (qx && qy)
    {
        bn[QX] = AndroidCryptoNative_BigNumFromBinary(qx, qxLength);
        bn[QY] = AndroidCryptoNative_BigNumFromBinary(qy, qyLength);
        if (!bn[QX] || !bn[QY])
            goto error;

        loc[pubKeyPoint] = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, bn[QX], bn[QY]);
        loc[pubKeySpec] =
            (*env)->NewObject(env, g_ECPublicKeySpecClass, g_ECPublicKeySpecCtor, loc[pubKeyPoint], curveParameters);

        // Set private key (optional)
        if (d && dLength)
        {
            bn[D] = AndroidCryptoNative_BigNumFromBinary(d, dLength);
            if (!bn[D])
                goto error;

            loc[privKeySpec] = (*env)->NewObject(env, g_ECPrivateKeySpecClass, g_ECPrivateKeySpecCtor, bn[D], curveParameters);
        }
    }
    // If we don't have the public key but we have the private key, we can
    // re-derive the public key from d.
    else if (qx == NULL && qy == NULL && qxLength == 0 && qyLength == 0 && d && dLength > 0)
    {
        bn[D] = AndroidCryptoNative_BigNumFromBinary(d, dLength);
        if (!bn[D])
            goto error;

        loc[privKeySpec] = (*env)->NewObject(env, g_ECPrivateKeySpecClass, g_ECPrivateKeySpecCtor, bn[D], curveParameters);

        // Java doesn't have a public implementation of operations on points on an elliptic curve
        // so we can't yet derive a new public key from the private key and generator.
        LOG_ERROR("Deriving a new public EC key from a provided private EC key and curve is unsupported");
        goto error;
    }
    else
    {
        goto error;
    }

    // Create the private and public keys and put them into a key pair.
    loc[algorithmName] = make_java_string(env, "EC");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algorithmName]);
    loc[publicKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPublicMethod, loc[pubKeySpec]);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    if (loc[privKeySpec])
    {
        loc[privateKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[privKeySpec]);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }
    keyPair = AndroidCryptoNative_CreateKeyPair(env, loc[publicKey], loc[privateKey]);

    goto cleanup;

error:
    if (loc[privateKey] && (*env)->IsInstanceOf(env, loc[privateKey], g_DestroyableClass))
    {
        // Destroy the private key data.
        (*env)->CallVoidMethod(env, loc[privateKey], g_destroy);
        (void)TryClearJNIExceptions(env); // The destroy call might throw an exception. Clear the exception state.
    }

cleanup:
    RELEASE_LOCALS_ENV(bn, ReleaseLRef);
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return keyPair;
}

#define CURVE_NOT_SUPPORTED -1

int32_t AndroidCryptoNative_EcKeyCreateByKeyParameters(EC_KEY** key,
                                                const char* oid,
                                                uint8_t* qx,
                                                int32_t qxLength,
                                                uint8_t* qy,
                                                int32_t qyLength,
                                                uint8_t* d,
                                                int32_t dLength)
{
    abort_if_invalid_pointer_argument (key);

    *key = NULL;

    JNIEnv* env = GetJNIEnv();

    // The easiest way to create explicit keys with a named curve is to generate
    // new keys for the curve, pull out the explicit paramters, and then create the explicit keys.
    *key = AndroidCryptoNative_EcKeyCreateByOid(oid);
    if (*key == NULL)
    {
        return CURVE_NOT_SUPPORTED;
    }

    // Release the reference to the generated key pair. We're going to make our own with the explicit keys.
    ReleaseGRef(env, (*key)->keyPair);
    (*key)->keyPair =
        AndroidCryptoNative_CreateKeyPairFromCurveParameters((*key)->curveParameters, qx, qxLength, qy, qyLength, d, dLength);

    if ((*key)->keyPair == NULL)
    {
        // We were unable to make the keys, so clean up and return FAIL.
        AndroidCryptoNative_EcKeyDestroy(*key);
        *key = NULL;
        return FAIL;
    }

    return SUCCESS;
}

// Converts a java.math.BigInteger to a positive int32_t value.
// Returns -1 if bigInteger < 0 or > INT32_MAX
ARGS_NON_NULL_ALL static int32_t ConvertBigIntegerToPositiveInt32(JNIEnv* env, jobject bigInteger)
{
    // bigInteger is negative.
    if ((*env)->CallIntMethod(env, bigInteger, g_sigNumMethod) < 0)
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

EC_KEY* AndroidCryptoNative_EcKeyCreateByExplicitParameters(ECCurveType curveType,
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
                                                     int32_t orderLength,
                                                     uint8_t* cofactor,
                                                     int32_t cofactorLength,
                                                     uint8_t* seed,
                                                     int32_t seedLength)
{
    abort_if_invalid_pointer_argument (p);
    abort_if_invalid_pointer_argument (a);
    abort_if_invalid_pointer_argument (b);
    abort_if_invalid_pointer_argument (gx);
    abort_if_invalid_pointer_argument (gy);
    abort_if_invalid_pointer_argument (order);
    abort_if_invalid_pointer_argument (cofactor);

    // qx, qy, d and seed are optional

    JNIEnv* env = GetJNIEnv();

    EC_KEY* keyInfo = NULL;
    jobject keyPair = NULL;

    INIT_LOCALS(loc, G, field, group, paramSpec, seedArray);
    INIT_LOCALS(bn, P, A, B, GX, GY, ORDER, COFACTOR);

    // Java only supports Weierstrass and characteristic-2 type curves.
    if (curveType != PrimeShortWeierstrass && curveType != Characteristic2)
    {
        LOG_ERROR("Unuspported curve type specified: %d", curveType);
        return NULL;
    }

    bn[P] = AndroidCryptoNative_BigNumFromBinary(p, pLength);

    // At this point we should use 'goto error' since we allocated objects or memory

    if (curveType == PrimeShortWeierstrass)
    {
        loc[field] = (*env)->NewObject(env, g_ECFieldFpClass, g_ECFieldFpCtor, bn[P]);
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
                if ((INT32_MAX - localBitIndex) / 8 < (pLength - 1))
                {
                    // We'll overflow if we try to calculate m.
                    goto error;
                }
                m = 8 * (pLength - 1) + localBitIndex;
            }
        }

        if (m == -1)
        {
            goto error;
        }

        loc[field] = (*env)->NewObject(env, g_ECFieldF2mClass, g_ECFieldF2mCtorWithCoefficientBigInteger, m, bn[P]);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }

    bn[A] = AndroidCryptoNative_BigNumFromBinary(a, aLength);
    bn[B] = AndroidCryptoNative_BigNumFromBinary(b, bLength);

    if (seed && seedLength > 0)
    {
        loc[seedArray] = make_java_byte_array(env, seedLength);
        (*env)->SetByteArrayRegion(env, loc[seedArray], 0, seedLength, (jbyte*)seed);
        loc[group] = (*env)->NewObject(env, g_EllipticCurveClass, g_EllipticCurveCtorWithSeed, loc[field], bn[A], bn[B], loc[seedArray]);
    }
    else
    {
        loc[group] = (*env)->NewObject(env, g_EllipticCurveClass, g_EllipticCurveCtor, loc[field], bn[A], bn[B]);
    }

    // Set generator, order and cofactor
    bn[GX] = AndroidCryptoNative_BigNumFromBinary(gx, gxLength);
    bn[GY] = AndroidCryptoNative_BigNumFromBinary(gy, gyLength);
    loc[G] = (*env)->NewObject(env, g_ECPointClass, g_ECPointCtor, bn[GX], bn[GY]);

    bn[ORDER] = AndroidCryptoNative_BigNumFromBinary(order, orderLength);

    // Java ECC doesn't support BigInteger-based cofactor. It uses positive 32-bit integers.
    // So, convert the cofactor to a positive 32-bit integer with overflow protection.
    bn[COFACTOR] = AndroidCryptoNative_BigNumFromBinary(cofactor, cofactorLength);
    int cofactorInt = ConvertBigIntegerToPositiveInt32(env, bn[COFACTOR]);

    if (cofactorInt == -1)
    {
        LOG_ERROR("Only positive cofactors less than %d are supported.", INT32_MAX);
        goto error;
    }

    loc[paramSpec] = (*env)->NewObject(env, g_ECParameterSpecClass, g_ECParameterSpecCtor, loc[group], loc[G], bn[ORDER], cofactorInt);

    if ((qx && qy) || d)
    {
        // If we have explicit key parameters, use those.
        keyPair = AndroidCryptoNative_CreateKeyPairFromCurveParameters(loc[paramSpec], qx, qxLength, qy, qyLength, d, dLength);
    }
    else
    {
        // Otherwise generate a new key pair.
        jstring ec = make_java_string(env, "EC");
        jobject keyPairGenerator =
            (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, ec);
        (*env)->CallVoidMethod(env, keyPairGenerator, g_keyPairGenInitializeWithParamsMethod, loc[paramSpec]);
        if (CheckJNIExceptions(env))
        {
            ReleaseLRef(env, keyPairGenerator);
            ReleaseLRef(env, ec);
            goto error;
        }

        keyPair = AddGRef(env, (*env)->CallObjectMethod(env, keyPairGenerator, g_keyPairGenGenKeyPairMethod));
    }

    if (!keyPair)
    {
        CheckJNIExceptions(env);
        goto error;
    }

    // Use AddGRef here since we always delete the local ref below.
    keyInfo = AndroidCryptoNative_NewEcKey(AddGRef(env, loc[paramSpec]), keyPair);

error:
    RELEASE_LOCALS_ENV(bn, ReleaseLRef);
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return keyInfo;
}
