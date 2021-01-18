// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_bignum.h"

jobject CryptoNative_BigNumFromBinary(uint8_t* bytes, int32_t len)
{
    // return new BigInteger(bytes)
    JNIEnv* env = GetJNIEnv();
    jbyteArray buffArray = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, buffArray, 0, len, (jbyte*)bytes);
    jobject bigNum = (*env)->NewObject(env, g_bigNumClass, g_bigNumCtor, buffArray);
    (*env)->DeleteLocalRef(env, buffArray);
    return CheckJNIExceptions(env) ? FAIL : ToGRef(env, bigNum);
}

int32_t CryptoNative_BigNumToBinary(jobject bignum, uint8_t* output)
{
    // bigNum.toByteArray()
    JNIEnv* env = GetJNIEnv();
    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, bignum, g_toByteArrayMethod);
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    (*env)->GetByteArrayRegion(env, bytes, 0, bytesLen, (jbyte*)output);
    (*env)->DeleteLocalRef(env, bytes);
    return CheckJNIExceptions(env) ? FAIL : (int32_t)bytesLen;
}

int32_t CryptoNative_GetBigNumBytes(jobject bignum)
{
    // bigNum.toByteArray().length();
    JNIEnv* env = GetJNIEnv();
    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, bignum, g_toByteArrayMethod);
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    (*env)->DeleteLocalRef(env, bytes);
    return CheckJNIExceptions(env) ? FAIL : (int32_t)bytesLen;
}

void CryptoNative_BigNumDestroy(jobject bignum)
{
    ReleaseGRef(GetJNIEnv(), bignum);
}
