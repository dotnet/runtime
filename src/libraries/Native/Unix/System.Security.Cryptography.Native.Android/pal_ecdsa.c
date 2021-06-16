// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ecdsa.h"
#include "pal_bignum.h"
#include "pal_signature.h"
#include "pal_utilities.h"

ARGS_NON_NULL_ALL static jobject GetEcDsaSignatureObject(JNIEnv* env)
{
    jstring algorithmName = make_java_string(env, "NONEwithECDSA");
    jobject signatureObject =
        (*env)->CallStaticObjectMethod(env, g_SignatureClass, g_SignatureGetInstance, algorithmName);
    (*env)->DeleteLocalRef(env, algorithmName);
    if (CheckJNIExceptions(env))
        return NULL;
    return signatureObject;
}

int32_t AndroidCryptoNative_EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key)
{
    abort_if_invalid_pointer_argument (dgst);
    abort_if_invalid_pointer_argument (sig);
    abort_if_invalid_pointer_argument (key);
    abort_if_invalid_pointer_argument (siglen);

    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = GetEcDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject privateKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPrivateMethod);
    if (!privateKey)
    {
        ReleaseLRef(env, signatureObject);
        return FAIL;
    }

    int32_t returnValue = AndroidCryptoNative_SignWithSignatureObject(env, signatureObject, privateKey, dgst, dgstlen, sig, siglen);
    ReleaseLRef(env, privateKey);
    ReleaseLRef(env, signatureObject);
    return returnValue;
}

int32_t AndroidCryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key)
{
    abort_if_invalid_pointer_argument (dgst);
    abort_if_invalid_pointer_argument (sig);
    abort_if_invalid_pointer_argument (key);

    JNIEnv* env = GetJNIEnv();

    jobject signatureObject = GetEcDsaSignatureObject(env);
    if (!signatureObject)
    {
        return FAIL;
    }

    jobject publicKey = (*env)->CallObjectMethod(env, key->keyPair, g_keyPairGetPublicMethod);
    int32_t returnValue = AndroidCryptoNative_VerifyWithSignatureObject(env, signatureObject, publicKey, dgst, dgstlen, sig, siglen);
    ReleaseLRef(env, publicKey);
    ReleaseLRef(env, signatureObject);
    return returnValue;
}

int32_t AndroidCryptoNative_EcDsaSize(const EC_KEY* key)
{
    abort_if_invalid_pointer_argument (key);

    // The maximum size of a signature for the provided key is 2* bitlength of the order + extra bytes for the DER
    // encoding. The DER encoding is as follows (with R and S being the components of the signature and all lengths
    // being one byte width):
    // - SEQUENCE <length of sequence> INTEGER <length of R> <R> INTEGER <length of S> <S>
    // As a result, we see that there are 6 additional bytes in the DER encoding than the lengths of R and S combined.
    // As the ECDSA algorithm is defined, the maximum length of R and S each is the bitlength of the order, so as a
    // result we get the maximum size as 2 * bitlength of the order + 6.
    // With some additional padding bytes for the bigintegers to keep them positive, we get a current max of 7.
    const int derEncodingBytes = 7;
    JNIEnv* env = GetJNIEnv();
    jobject order = (*env)->CallObjectMethod(env, key->curveParameters, g_ECParameterSpecGetOrder);
    int byteLength = AndroidCryptoNative_GetBigNumBytesIncludingPaddingByteForSign(order);
    ReleaseLRef(env, order);
    return 2 * byteLength + derEncodingBytes;
}
