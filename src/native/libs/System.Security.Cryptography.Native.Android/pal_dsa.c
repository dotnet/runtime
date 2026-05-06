// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_dsa.h"
#include "pal_utilities.h"
#include "pal_signature.h"
#include "pal_bignum.h"
#include "pal_misc.h"

int32_t AndroidCryptoNative_DsaGenerateKey(jobject* dsa, int32_t bits)
{
    abort_if_invalid_pointer_argument (dsa);

    // KeyPairGenerator kpg = KeyPairGenerator.getInstance("DSA");
    // kpg.initialize(bits);
    // KeyPair kp = kpg.genKeyPair();

    JNIEnv* env = GetJNIEnv();
    jobject dsaStr = make_java_string(env, "DSA");
    jobject kpgObj =  (*env)->CallStaticObjectMethod(env, g_keyPairGenClass, g_keyPairGenGetInstanceMethod, dsaStr);
    (*env)->DeleteLocalRef(env, dsaStr);
    if (CheckJNIExceptions(env))
    {
        return FAIL;
    }

    (*env)->CallVoidMethod(env, kpgObj, g_keyPairGenInitializeMethod, bits);
    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, kpgObj);
        return FAIL;
    }
    jobject keyPair = (*env)->CallObjectMethod(env, kpgObj, g_keyPairGenGenKeyPairMethod);
    (*env)->DeleteLocalRef(env, kpgObj);
    if (CheckJNIExceptions(env))
    {
        return FAIL;
    }
    *dsa = ToGRef(env, keyPair);
    return SUCCESS;
}

ARGS_NON_NULL_ALL static jobject GetQParameter(JNIEnv* env, jobject dsa)
{
    jobject ret = NULL;

    INIT_LOCALS(loc, algName, keyFactory, publicKey, publicKeySpec);
    loc[algName] = make_java_string(env, "DSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algName]);
    loc[publicKey] = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPublicMethod);
    loc[publicKeySpec] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGetKeySpecMethod, loc[publicKey], g_DSAPublicKeySpecClass);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);

    jobject q = (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetQ);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = q;

cleanup:
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return ret;
}

int32_t AndroidCryptoNative_DsaSizeSignature(jobject dsa)
{
    abort_if_invalid_pointer_argument (dsa);

    // The maximum size of a signature for the provided key is 2* bitlength of Q + extra bytes for the DER
    // encoding. The DER encoding is as follows (with R and S being the components of the signature and all lengths
    // being one byte width):
    // - SEQUENCE <length of sequence> INTEGER <length of R> <R> INTEGER <length of S> <S>
    // As a result, we see that there are 6 additional bytes in the DER encoding than the lengths of R and S combined.
    // As the DSA algorithm is defined, the maximum length of R and S each is the bitlength of Q, so as a
    // result we get the maximum size as 2 * bitlength of Q + 6.
    const int derEncodingBytes = 6;
    JNIEnv* env = GetJNIEnv();
    jobject q = GetQParameter(env, dsa);
    if (!q)
    {
        return -1;
    }
    // Add one for a possible leading zero byte to force the sign to positive.
    int byteLength = AndroidCryptoNative_GetBigNumBytesIncludingPaddingByteForSign(q);
    ReleaseLRef(env, q);

    return 2 * byteLength + derEncodingBytes;
}

int32_t AndroidCryptoNative_DsaSizeP(jobject dsa)
{
    abort_if_invalid_pointer_argument (dsa);

    JNIEnv* env = GetJNIEnv();
    INIT_LOCALS(loc, algName, keyFactory, publicKey, publicKeySpec, p);
    loc[algName] = make_java_string(env, "DSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algName]);
    loc[publicKey] = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPublicMethod);
    loc[publicKeySpec] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGetKeySpecMethod, loc[publicKey], g_DSAPublicKeySpecClass);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    loc[p] = (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetP);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    int32_t bytes = AndroidCryptoNative_GetBigNumBytes(loc[p]);
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return bytes;

error:
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return -1;
}

int32_t AndroidCryptoNative_DsaSignatureFieldSize(jobject dsa)
{
    abort_if_invalid_pointer_argument (dsa);

    JNIEnv* env = GetJNIEnv();
    jobject q = GetQParameter(env, dsa);
    if (!q)
    {
        return -1;
    }
    int byteLength = AndroidCryptoNative_GetBigNumBytes(q);
    ReleaseLRef(env, q);
    return byteLength;
}

ARGS_NON_NULL_ALL static jobject GetDsaSignatureObject(JNIEnv* env)
{
    jstring algorithmName = make_java_string(env, "NONEwithDSA");
    jobject signatureObject =
        (*env)->CallStaticObjectMethod(env, g_SignatureClass, g_SignatureGetInstance, algorithmName);
    (*env)->DeleteLocalRef(env, algorithmName);
    if (CheckJNIExceptions(env))
        return NULL;
    return signatureObject;
}

int32_t AndroidCryptoNative_DsaSign(
    jobject dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* refsignature,
    int32_t* outSignatureLength)
{
    abort_if_invalid_pointer_argument (hash);
    abort_if_invalid_pointer_argument (refsignature);
    abort_if_invalid_pointer_argument (dsa);
    if (!outSignatureLength)
    {
        return FAIL;
    }

    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = GetDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject privateKey = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPrivateMethod);
    if (!privateKey)
    {
        ReleaseLRef(env, signatureObject);
        return FAIL;
    }

    int32_t returnValue = AndroidCryptoNative_SignWithSignatureObject(env, signatureObject, privateKey, hash, hashLength, refsignature, outSignatureLength);
    ReleaseLRef(env, privateKey);
    ReleaseLRef(env, signatureObject);
    return returnValue;
}

int32_t AndroidCryptoNative_DsaVerify(
    jobject dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t signatureLength)
{
    abort_if_invalid_pointer_argument (hash);
    abort_if_invalid_pointer_argument (signature);
    abort_if_invalid_pointer_argument (dsa);
    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = GetDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject publicKey = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPublicMethod);
    int32_t returnValue = AndroidCryptoNative_VerifyWithSignatureObject(env, signatureObject, publicKey, hash, hashLength, signature, signatureLength);
    ReleaseLRef(env, publicKey);
    ReleaseLRef(env, signatureObject);
    return returnValue;
}

int32_t AndroidCryptoNative_GetDsaParameters(
    jobject dsa,
    jobject* p, int32_t* pLength,
    jobject* q, int32_t* qLength,
    jobject* g, int32_t* gLength,
    jobject* y, int32_t* yLength,
    jobject* x, int32_t* xLength)
{
    abort_if_invalid_pointer_argument (dsa);
    abort_if_invalid_pointer_argument (p);
    abort_if_invalid_pointer_argument (q);
    abort_if_invalid_pointer_argument (g);
    abort_if_invalid_pointer_argument (y);
    abort_if_invalid_pointer_argument (x);
    abort_if_invalid_pointer_argument (pLength);
    abort_if_invalid_pointer_argument (qLength);
    abort_if_invalid_pointer_argument (gLength);
    abort_if_invalid_pointer_argument (yLength);
    abort_if_invalid_pointer_argument (xLength);

    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(loc, algName, keyFactory, publicKey, publicKeySpec, privateKey, privateKeySpec);

    loc[algName] = make_java_string(env, "DSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[algName]);
    loc[publicKey] = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPublicMethod);
    loc[publicKeySpec] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGetKeySpecMethod, loc[publicKey], g_DSAPublicKeySpecClass);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    *p = ToGRef(env, (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetP));
    *q = ToGRef(env, (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetQ));
    *g = ToGRef(env, (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetG));
    *y = ToGRef(env, (*env)->CallObjectMethod(env, loc[publicKeySpec], g_DSAPublicKeySpecGetY));
    *pLength = AndroidCryptoNative_GetBigNumBytes(*p);
    *qLength = AndroidCryptoNative_GetBigNumBytes(*q);
    *gLength = AndroidCryptoNative_GetBigNumBytes(*g);
    *yLength = AndroidCryptoNative_GetBigNumBytes(*y);

    *x = NULL;
    *xLength = 0;
    loc[privateKey] = (*env)->CallObjectMethod(env, dsa, g_keyPairGetPrivateMethod);
    if (loc[privateKey])
    {
        loc[privateKeySpec] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGetKeySpecMethod, loc[privateKey], g_DSAPrivateKeySpecClass);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
        *x = ToGRef(env, (*env)->CallObjectMethod(env, loc[privateKeySpec], g_DSAPrivateKeySpecGetX));
        *xLength = AndroidCryptoNative_GetBigNumBytes(*x);
    }

    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;

error:
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);
    return FAIL;
}

int32_t AndroidCryptoNative_DsaKeyCreateByExplicitParameters(
    jobject* outDsa,
    uint8_t* p,
    int32_t pLength,
    uint8_t* q,
    int32_t qLength,
    uint8_t* g,
    int32_t gLength,
    uint8_t* y,
    int32_t yLength,
    uint8_t* x,
    int32_t xLength)
{
    abort_if_invalid_pointer_argument (outDsa);

    JNIEnv* env = GetJNIEnv();

    INIT_LOCALS(bn, P, Q, G, Y, X);
    INIT_LOCALS(loc, publicKeySpec, privateKeySpec, publicKey, privateKey, dsa, keyFactory);

    bn[P] = AndroidCryptoNative_BigNumFromBinary(p, pLength);
    bn[Q] = AndroidCryptoNative_BigNumFromBinary(q, qLength);
    bn[G] = AndroidCryptoNative_BigNumFromBinary(g, gLength);
    bn[Y] = AndroidCryptoNative_BigNumFromBinary(y, yLength);
    loc[publicKeySpec] = (*env)->NewObject(env, g_DSAPublicKeySpecClass, g_DSAPublicKeySpecCtor, bn[Y], bn[P], bn[Q], bn[G]);

    if (x)
    {
        bn[X] = AndroidCryptoNative_BigNumFromBinary(x, xLength);
        loc[privateKeySpec] = (*env)->NewObject(env, g_DSAPrivateKeySpecClass, g_DSAPrivateKeySpecCtor, bn[X], bn[P], bn[Q], bn[G]);
    }

    loc[dsa] = make_java_string(env, "DSA");
    loc[keyFactory] = (*env)->CallStaticObjectMethod(env, g_KeyFactoryClass, g_KeyFactoryGetInstanceMethod, loc[dsa]);
    loc[publicKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPublicMethod, loc[publicKeySpec]);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    if (loc[privateKeySpec])
    {
        loc[privateKey] = (*env)->CallObjectMethod(env, loc[keyFactory], g_KeyFactoryGenPrivateMethod, loc[privateKeySpec]);
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }

    *outDsa = AndroidCryptoNative_CreateKeyPair(env, loc[publicKey], loc[privateKey]);
    if (CheckJNIExceptions(env))
    {
        ON_EXCEPTION_PRINT_AND_GOTO(error);
    }

    int32_t returnValue = SUCCESS;
    goto cleanup;

error:
    returnValue = FAIL;
cleanup:
    RELEASE_LOCALS_ENV(bn, ReleaseLRef);
    RELEASE_LOCALS_ENV(loc, ReleaseLRef);

    return returnValue;
}
