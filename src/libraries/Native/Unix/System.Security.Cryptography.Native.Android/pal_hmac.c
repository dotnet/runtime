// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp.h"
#include "pal_hmac.h"

jobject CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, intptr_t type)
{
    if (key != NULL && keyLen < 0) {
        return NULL;
    }

    // Mac mac = Mac.getInstance(algName);
    // SecretKeySpec key = new SecretKeySpec(key, algName);
    // mac.init(key);

    JNIEnv* env = GetJNIEnv();

    jstring macName = NULL;
    if (type == CryptoNative_EvpSha1())
        macName = make_java_string(env, "HmacSHA1");
    else if (type == CryptoNative_EvpSha256())
        macName = make_java_string(env, "HmacSHA256");
    else if (type == CryptoNative_EvpSha384())
        macName = make_java_string(env, "HmacSHA384");
    else if (type == CryptoNative_EvpSha512())
        macName = make_java_string(env, "HmacSHA512");
    else if (type == CryptoNative_EvpMd5())
        macName = make_java_string(env, "HmacMD5");
    else
        return FAIL;

    jbyteArray keyBytes;

    if (key && keyLen > 0)
    {
        keyBytes = make_java_byte_array(env, keyLen);
        (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLen, (jbyte*)key);
    }
    else
    {
        // Java does not support zero-length byte arrays in the SecretKeySpec type,
        // so instead create an empty 1-byte length byte array that's initalized to 0.
        // the HMAC algorithm pads keys with zeros until the key is block-length,
        // so this effectively creates the same key as if it were a zero byte-length key.
        keyBytes = make_java_byte_array(env, 1);
    }

    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, macName);
    if (CheckJNIExceptions(env) || sksObj == NULL)
    {
        if(sksObj == NULL)
        {
            LOG_WARN ("Unable to create an instance of SecretKeySpec");
        }

        (*env)->DeleteLocalRef(env, keyBytes);
        (*env)->DeleteLocalRef(env, sksObj);
        (*env)->DeleteLocalRef(env, macName);
        return FAIL;
    }

    jobject macObj = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_MacClass, g_MacGetInstance, macName));
    (*env)->CallVoidMethod(env, macObj, g_MacInit, sksObj);
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
    (*env)->CallVoidMethod(env, ctx, g_MacReset);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacUpdate(jobject ctx, uint8_t* data, int32_t len)
{
    if (!ctx)
        return FAIL;

    abort_if_invalid_pointer_argument (data);
    JNIEnv* env = GetJNIEnv();
    jbyteArray dataBytes = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, dataBytes, 0, len, (jbyte*)data);
    (*env)->CallVoidMethod(env, ctx, g_MacUpdate, dataBytes);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

ARGS_NON_NULL_ALL static int32_t DoFinal(JNIEnv* env, jobject mac, uint8_t* data, int32_t* len)
{
    // mac.doFinal();
    jbyteArray dataBytes = (jbyteArray)(*env)->CallObjectMethod(env, mac, g_MacDoFinal);
    jsize dataBytesLen = (*env)->GetArrayLength(env, dataBytes);
    *len = (int32_t)dataBytesLen;
    (*env)->GetByteArrayRegion(env, dataBytes, 0, dataBytesLen, (jbyte*) data);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacFinal(jobject ctx, uint8_t* data, int32_t* len)
{
    abort_if_invalid_pointer_argument (ctx);

    JNIEnv* env = GetJNIEnv();
    return DoFinal(env, ctx, data, len);
}

int32_t CryptoNative_HmacCurrent(jobject ctx, uint8_t* data, int32_t* len)
{
    abort_if_invalid_pointer_argument (ctx);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    // ctx.clone();
    jobject clone = (*env)->CallObjectMethod(env, ctx, g_MacClone);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = DoFinal(env, clone, data, len);

cleanup:
    (*env)->DeleteLocalRef(env, clone);
    return ret;
}

void CryptoNative_HmacDestroy(jobject ctx)
{
    ReleaseGRef(GetJNIEnv(), ctx);
}
