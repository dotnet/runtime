// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_eckey.h"
#include "pal_misc.h"

#include <assert.h>

EC_KEY* AndroidCryptoNative_NewEcKey(jobject curveParameters, jobject keyPair)
{
    abort_if_invalid_pointer_argument (curveParameters);
    abort_if_invalid_pointer_argument (keyPair);

    EC_KEY* keyInfo = xcalloc(1, sizeof(EC_KEY));
    atomic_init(&keyInfo->refCount, 1);
    keyInfo->curveParameters = curveParameters;
    keyInfo->keyPair = keyPair;
    return keyInfo;
}

EC_KEY* AndroidCryptoNative_NewEcKeyFromKeys(JNIEnv *env, jobject /*ECPublicKey*/ publicKey, jobject /*ECPrivateKey*/ privateKey)
{
    abort_if_invalid_pointer_argument (publicKey);

    if (!(*env)->IsInstanceOf(env, publicKey, g_ECPublicKeyClass))
        return NULL;

    jobject curveParameters = (*env)->CallObjectMethod(env, publicKey, g_ECPublicKeyGetParams);
    if (CheckJNIExceptions(env) || curveParameters == NULL)
        return NULL;

    return AndroidCryptoNative_NewEcKey(ToGRef(env, curveParameters), AndroidCryptoNative_CreateKeyPair(env, publicKey, privateKey));
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
                if (privateKey && (*env)->IsInstanceOf(env, privateKey, g_DestroyableClass))
                {
                    (*env)->CallVoidMethod(env, privateKey, g_destroy);
                    ReleaseLRef(env, privateKey);
                    (void)TryClearJNIExceptions(env); // The destroy call might throw an exception. Clear the exception state.
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
    abort_if_invalid_pointer_argument (oid);

    JNIEnv* env = GetJNIEnv();
    EC_KEY* ret = NULL;
    INIT_LOCALS(loc, oidStr, ec, paramSpec, keyPairGenerator, keyPair, keyFactory, publicKey, keySpec, curveParameters);

    // Older versions of Android don't support mapping an OID to a curve name,
    // so do some of the common mappings here.
    if (strcmp(oid, "1.3.132.0.33") == 0)
    {
        loc[oidStr] = make_java_string(env, "secp224r1");
    }
    else if (strcmp(oid, "1.3.132.0.34") == 0 || strcmp(oid, "nistP384") == 0)
    {
        loc[oidStr] = make_java_string(env, "secp384r1");
    }
    else if (strcmp(oid, "1.3.132.0.35") == 0 || strcmp(oid, "nistP521") == 0)
    {
        loc[oidStr] = make_java_string(env, "secp521r1");
    }
    else if (strcmp(oid, "1.2.840.10045.3.1.7") == 0 || strcmp(oid, "nistP256") == 0)
    {
        loc[oidStr] = make_java_string(env, "secp256r1");
    }
    else
    {
        loc[oidStr] = make_java_string(env, oid);
    }
    loc[ec] = make_java_string(env, "EC");

    // First, generate the key pair based on the curve defined by the oid.
    loc[paramSpec] = (*env)->NewObject(env, g_ECGenParameterSpecClass, g_ECGenParameterSpecCtor, loc[oidStr]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[keyPairGenerator] =
        (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, loc[ec]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    (*env)->CallVoidMethod(env, loc[keyPairGenerator], g_keyPairGenInitializeWithParamsMethod, loc[paramSpec]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[keyPair] = (*env)->CallObjectMethod(env, loc[keyPairGenerator], g_keyPairGenGenKeyPairMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    // Now that we have the key pair, we can get the curve parameters from the public key.
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[ec]);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[publicKey] = (*env)->CallObjectMethod(env, loc[keyPair], g_keyPairGetPublicMethod);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[keySpec] =
        (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGetKeySpecMethod, loc[publicKey], g_ECPublicKeySpecClass);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    loc[curveParameters] = (*env)->CallObjectMethod(env, loc[keySpec], g_ECPublicKeySpecGetParams);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    ret = AndroidCryptoNative_NewEcKey(AddGRef(env, loc[curveParameters]), AddGRef(env, loc[keyPair]));

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_EcKeyGetSize(const EC_KEY* key, int32_t* keySize)
{
    if (!keySize)
        return FAIL;

    *keySize = 0;

    if (!key)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;
    int32_t size = 0;
    INIT_LOCALS(loc, curve, field);

    loc[curve] = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurve);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    loc[field] = (*env)->CallObjectMethod(env, loc[curve], g_EllipticCurveGetField);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    size = (*env)->CallIntMethod(env, loc[field], g_ECFieldGetFieldSize);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    *keySize = size;
    ret = SUCCESS;

cleanup:
    RELEASE_LOCALS(loc, env);
    return ret;
}

int32_t AndroidCryptoNative_EcKeyGetCurveName(const EC_KEY* key, uint16_t** curveName)
{
    abort_if_invalid_pointer_argument (curveName);

    *curveName = NULL;

    if (!g_ECParameterSpecGetCurveName)
    {
        // We can't get the curve name. Treat all curves as unnamed.
        return SUCCESS;
    }

    abort_if_invalid_pointer_argument (key);
    JNIEnv* env = GetJNIEnv();

    int32_t ret = FAIL;
    uint16_t* buffer = NULL;
    INIT_LOCALS(loc, curveNameStr);

    loc[curveNameStr] = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetCurveName);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    if (!loc[curveNameStr])
    {
        // No curve name available; treat as an unnamed curve.
        ret = SUCCESS;
        goto cleanup;
    }

    jsize nameLength = (*env)->GetStringLength(env, loc[curveNameStr]);

    // add one for the null terminator.
    buffer = xmalloc(sizeof(int16_t) * (size_t)(nameLength + 1));
    buffer[nameLength] = 0;

    (*env)->GetStringRegion(env, loc[curveNameStr], 0, nameLength, (jchar*)buffer);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    *curveName = buffer;
    buffer = NULL;
    ret = SUCCESS;

cleanup:
    free(buffer);
    RELEASE_LOCALS(loc, env);
    return ret;
}
