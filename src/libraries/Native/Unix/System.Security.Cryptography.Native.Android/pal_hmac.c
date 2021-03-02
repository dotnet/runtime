// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp.h"
#include "pal_hmac.h"

jobject CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, intptr_t type)
{
    assert(key || (keyLen == 0));
    assert(keyLen >= 0);

    // Mac mac = Mac.getInstance(algName);
    // SecretKeySpec key = new SecretKeySpec(key, algName);
    // mac.init(key);

    JNIEnv* env = GetJNIEnv();

    jstring macName = NULL;
    if (type == CryptoNative_EvpSha1())
        macName = JSTRING("HmacSHA1");
    else if (type == CryptoNative_EvpSha256())
        macName = JSTRING("HmacSHA256");
    else if (type == CryptoNative_EvpSha384())
        macName = JSTRING("HmacSHA384");
    else if (type == CryptoNative_EvpSha512())
        macName = JSTRING("HmacSHA512");
    else if (type == CryptoNative_EvpMd5())
        macName = JSTRING("HmacMD5");
    else
        return FAIL;

    jbyteArray keyBytes = (*env)->NewByteArray(env, keyLen);
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLen, (jbyte*)key);
    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, macName);
    if (CheckJNIExceptions(env))
    {
        (*env)->DeleteLocalRef(env, keyBytes);
        (*env)->DeleteLocalRef(env, sksObj);
        (*env)->DeleteLocalRef(env, macName);
        return FAIL;
    }
    assert(sksObj && "Unable to create an instance of SecretKeySpec");
    jobject macObj = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_macClass, g_macGetInstanceMethod, macName));
    (*env)->CallVoidMethod(env, macObj, g_macInitMethod, sksObj);
    (*env)->DeleteLocalRef(env, keyBytes);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, macName);

    return CheckJNIExceptions(env) ? FAIL : macObj;
}

int32_t CryptoNative_HmacReset(jobject ctx)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    (*env)->CallVoidMethod(env, ctx, g_macResetMethod);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacUpdate(jobject ctx, uint8_t* data, int32_t len)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    jbyteArray dataBytes = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, dataBytes, 0, len, (jbyte*)data);
    (*env)->CallVoidMethod(env, ctx, g_macUpdateMethod, dataBytes);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacFinal(jobject ctx, uint8_t* data, int32_t* len)
{
    return CryptoNative_HmacCurrent(ctx, data, len);
}

int32_t CryptoNative_HmacCurrent(jobject ctx, uint8_t* data, int32_t* len)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJNIEnv();
    jbyteArray dataBytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx, g_macDoFinalMethod);
    jsize dataBytesLen = (*env)->GetArrayLength(env, dataBytes);
    *len = (int32_t)dataBytesLen;
    (*env)->GetByteArrayRegion(env, dataBytes, 0, dataBytesLen, (jbyte*) data);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

void CryptoNative_HmacDestroy(jobject ctx)
{
    ReleaseGRef(GetJNIEnv(), ctx);
}
