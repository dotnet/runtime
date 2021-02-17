// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecdsa.h"
#include "pal_bignum.h"
#include "pal_utilities.h"

static jobject CryptoNative_GetEsDsaSignatureObject(JNIEnv* env)
{
    jstring algorithmName = JSTRING("SHA1withECDSA");
    jobject signatureObject =
        (*env)->CallStaticObjectMethod(env, g_SignatureClass, g_SignatureGetInstance, algorithmName);
    (*env)->DeleteLocalRef(algorithmName);
    if (CheckJNIExceptions())
        return NULL;
    return signatureObject;
}

int32_t CryptoNative_EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key)
{
    if (!siglen)
    {
        return FAIL;
    }

    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = CryptoNative_GetEsDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject privateKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPrivateMethod);
    (*env)->CallVoidMethod(env, signatureObject, g_SignatureInitSign, privateKey);
    ReleaseLRef(env, privateKey);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    jbyteArray digestArray = (*env)->NewByteArray(env, dgstlen);
    (*env)->SetByteArrayRegion(env, digestArray, 0, dgstlen, (const jbyte*)dgst);
    (*env)->CallVoidMethod(env, signatureObject, g_SignatureUpdate, digestArray);
    ReleaseLRef(env, digestArray);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    jbyteArray sigResult = (*env)->CallObjectMethod(env, signatureObject, g_SignatureSign);
    ON_EXCEPTION_PRINT_AND_GOTO(error);
    jsize sigSize = (*env)->GetArrayLength(sigResult);
    (*env)->GetByteArrayRegion(env, sigResult, 0, sigSize, sig);
    ReleaseLRef(env, sigResult);

    ReleaseLRef(env, signatureObject);

    return SUCCESS;

error:
    ReleaseLRef(env, signatureObject);
    return FAIL;
}

int32_t CryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key)
{
    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = CryptoNative_GetEsDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject publicKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPublicMethod);
    (*env)->CallVoidMethod(env, signatureObject, g_SignatureInitVerify, publicKey);
    ReleaseLRef(env, publicKey);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    jbyteArray digestArray = (*env)->NewByteArray(env, dgstlen);
    (*env)->SetByteArrayRegion(env, digestArray, 0, dgstlen, (const jbyte*)dgst);
    (*env)->CallVoidMethod(env, signatureObject, g_SignatureUpdate, digestArray);
    ReleaseLRef(env, digestArray);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    jbyteArray sigArray = (*env)->NewByteArray(env, dgstlen);
    (*env)->SetByteArrayRegion(env, sigArray, 0, siglen, (const jbyte*)sig);
    (*env)->CallObjectMethod(env, signatureObject, g_SignatureVerify, sigArray);
    ReleaseLRef(env, sigArray);
    ON_EXCEPTION_PRINT_AND_GOTO(error);

    ReleaseLRef(env, signatureObject);

    return SUCCESS;

error:
    ReleaseLRef(env, signatureObject);
    return FAIL;
}

int32_t CryptoNative_EcDsaSize(const EC_KEY* key)
{
    // The maximum size of a signature for the provided key is 2* bitlength of the order + extra bytes for the DER
    // encoding. The DER encoding is as follows (with R and S being the components of the signature and all lengths
    // being one byte width):
    // - SEQUENCE <length of sequence> INTEGER <length of R> <R> INTEGER <length of S> <S>
    // As a result, we see that there are 6 additional bytes in the DER encoding than the lengths of R and S combined.
    // As the ECDSA algorithm is defined, the maximum length of R and S each is the bitlength of the order, so as a
    // result we get the maximum size as 2 * bitlength of the order + 6.
    const int derEncodingBytes = 6;
    JNIEnv* env = GetJNIEnv();
    jobject order = (*env)->CallObjectMethod(key->curveParameters, g_ECParameterSpecGetOrder);
    int byteLength = CryptoNative_GetBigNumBytes(order);
    ReleaseLRef(env, order);
    return 2 * byteLength + derEncodingBytes;
}
