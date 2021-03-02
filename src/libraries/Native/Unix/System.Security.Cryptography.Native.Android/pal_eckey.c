// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_eckey.h"

#include <assert.h>

EC_KEY* AndroidCryptoNative_NewEcKey(jobject curveParameters, jobject keyPair)
{
    assert(curveParameters);
    assert(keyPair);

    EC_KEY* keyInfo = malloc(sizeof(EC_KEY));
    memset(keyInfo, 0, sizeof(EC_KEY));
    atomic_init(&keyInfo->refCount, 1);
    keyInfo->curveParameters = curveParameters;
    keyInfo->keyPair = keyPair;
    return keyInfo;
}

#pragma clang diagnostic push
// There's no way to specify explicit memory ordering for increment/decrement with C atomics.
#pragma clang diagnostic ignored "-Watomic-implicit-seq-cst"
void AndroidCryptoNative_EcKeyDestroy(EC_KEY* r)
{
    if (r)
    {
        int count = --r->refCount;
        if (count == 0)
        {
            JNIEnv* env = GetJNIEnv();
            if (r->keyPair != NULL)
            {
                // Destroy the private key data.
                jobject privateKey = (*env)->CallObjectMethod(env, r->keyPair, g_keyPairGetPrivateMethod);
                if (privateKey)
                {
                    (*env)->CallVoidMethod(env, privateKey, g_destroy);
                    ReleaseLRef(env, privateKey);
                    CheckJNIExceptions(env); // The destroy call might throw an exception. Clear the exception state.
                }
            }

            ReleaseGRef(env, r->keyPair);
            ReleaseGRef(env, r->curveParameters);
            free(r);
        }
    }
}

int32_t AndroidCryptoNative_EcKeyUpRef(EC_KEY* r)
{
    if (!r)
        return FAIL;
    r->refCount++;
    return SUCCESS;
}
#pragma clang diagnostic pop

EC_KEY* AndroidCryptoNative_EcKeyCreateByOid(const char* oid)
{
    JNIEnv* env = GetJNIEnv();

    // Older versions of Android don't support mapping an OID to a curve name,
    // so do some of the common mappings here.
    jstring oidStr;
    if (strcmp(oid, "1.3.132.0.33") == 0)
    {
        oidStr = JSTRING("secp224r1");
    }
    else if (strcmp(oid, "1.3.132.0.34") == 0 || strcmp(oid, "nistP384") == 0)
    {
        oidStr = JSTRING("secp384r1");
    }
    else if (strcmp(oid, "1.3.132.0.35") == 0 || strcmp(oid, "nistP521") == 0)
    {
        oidStr = JSTRING("secp521r1");
    }
    else if (strcmp(oid, "1.2.840.10045.3.1.7") == 0 || strcmp(oid, "nistP256") == 0)
    {
        oidStr = JSTRING("secp256r1");
    }
    else 
    {
        oidStr = JSTRING(oid);
    }
    jstring ec = JSTRING("EC");

    // First, generate the key pair based on the curve defined by the oid.
    jobject paramSpec = (*env)->NewObject(env, g_ECGenParameterSpecClass, g_ECGenParameterSpecCtor, oidStr);
    ReleaseLRef(env, oidStr);

    jobject keyPairGenerator =
        (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, ec);
    (*env)->CallVoidMethod(env, keyPairGenerator, g_keyPairGenInitializeWithParamsMethod, paramSpec);

    ReleaseLRef(env, paramSpec);
    if (CheckJNIExceptions(env))
    {
        LOG_DEBUG("Failed to create curve");
        ReleaseLRef(env, ec);
        ReleaseLRef(env, keyPairGenerator);
        return NULL;
    }

    jobject keyPair = (*env)->CallObjectMethod(env, keyPairGenerator, g_keyPairGenGenKeyPairMethod);

    ReleaseLRef(env, keyPairGenerator);

    // Now that we have the key pair, we can get the curve parameters from the public key.
    jobject keyFactory = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, ec);
    jobject publicKey = (*env)->CallObjectMethod(env, keyPair, g_keyPairGetPublicMethod);
    jobject keySpec =
        (*env)->CallObjectMethod(env, keyFactory, g_KeyFactoryGetKeySpecMethod, publicKey, g_ECPublicKeySpecClass);

    ReleaseLRef(env, ec);
    ReleaseLRef(env, publicKey);
    ReleaseLRef(env, keyFactory);

    if (CheckJNIExceptions(env))
    {
        ReleaseLRef(env, keySpec);
        ReleaseLRef(env, keyPair);
        return NULL;
    }

    jobject curveParameters = (*env)->CallObjectMethod(env, keySpec, g_ECPublicKeySpecGetParams);
    return AndroidCryptoNative_NewEcKey(ToGRef(env, curveParameters), ToGRef(env, keyPair));
}

int32_t AndroidCryptoNative_EcKeyGetSize(const EC_KEY* key, int32_t* keySize)
{
    if (!keySize)
        return FAIL;

    *keySize = 0;

    if (!key)
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    jobject curve = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurve);
    jobject field = (*env)->CallObjectMethod(env, curve, g_EllipticCurveGetField);
    *keySize = (*env)->CallIntMethod(env, field, g_ECFieldGetFieldSize);

    ReleaseLRef(env, field);
    ReleaseLRef(env, curve);
    return SUCCESS;
}

int32_t AndroidCryptoNative_EcKeyGetCurveName(const EC_KEY* key, uint16_t** curveName)
{
    if (!g_ECParameterSpecGetCurveName)
    {
        // We can't get the curve name. Treat all curves as unnamed.
        *curveName = NULL;
        return SUCCESS;
    }

    JNIEnv* env = GetJNIEnv();

    jstring curveNameStr = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurveName);

    if (!curveNameStr)
    {
        *curveName = NULL;
        return SUCCESS;
    }

    if (CheckJNIExceptions(env))
    {
        *curveName = NULL;
        return FAIL;
    }

    jsize nameLength = (*env)->GetStringLength(env, curveNameStr);

    // add one for the null terminator.
    uint16_t* buffer = malloc(sizeof(int16_t) * (size_t)(nameLength + 1));
    buffer[nameLength] = 0;

    (*env)->GetStringRegion(env, curveNameStr, 0, nameLength, (jchar*)buffer);
    (*env)->DeleteLocalRef(env, curveNameStr);

    *curveName = buffer;

    return SUCCESS;
}
