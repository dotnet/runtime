// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_bignum.h"

int32_t AndroidCryptoNative_BigNumToBinary(jobject bignum, uint8_t* output)
{
    // JNI requires object passed to `Call*Method` to be not NULL
    abort_if_invalid_pointer_argument (bignum);

    // JNI requires `output` to be not NULL when passed to `{Get,Set}ByteArrayRegion`
    abort_if_invalid_pointer_argument (output);

    // bigNum.toByteArray()
    JNIEnv* env = GetJNIEnv();
    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, bignum, g_toByteArrayMethod);
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);

    // We strip the leading zero byte from the byte array.
    jsize startingIndex = 0;
    jbyte leadingByte;
    (*env)->GetByteArrayRegion(env, bytes, 0, 1, &leadingByte);
    if (leadingByte == 0)
    {
        startingIndex++;
        bytesLen--;
    }

    (*env)->GetByteArrayRegion(env, bytes, startingIndex, bytesLen, (jbyte*)output);
    (*env)->DeleteLocalRef(env, bytes);
    return CheckJNIExceptions(env) ? FAIL : (int32_t)bytesLen;
}

int32_t AndroidCryptoNative_GetBigNumBytes(jobject bignum)
{
    // JNI requires object passed to `Call*Method` to be not NULL
    abort_if_invalid_pointer_argument (bignum);

    // bigNum.bitlength();
    // round up to the nearest byte
    JNIEnv* env = GetJNIEnv();
    int bytesLen = ((*env)->CallIntMethod(env, bignum, g_bitLengthMethod) + 7) / 8;
    return CheckJNIExceptions(env) ? FAIL : (int32_t)bytesLen;
}

jobject AndroidCryptoNative_BigNumFromBinary(uint8_t* bytes, int32_t len)
{
    // JNI requires `bytes` to be not NULL when passed to `{Get,Set}ByteArrayRegion`
    abort_if_invalid_pointer_argument (bytes);

    // return new BigInteger(bytes)
    JNIEnv* env = GetJNIEnv();
    jbyteArray buffArray = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, buffArray, 0, len, (jbyte*)bytes);
    jobject bigNum = (*env)->NewObject(env, g_bigNumClass, g_bigNumCtorWithSign, 1, buffArray);
    (*env)->DeleteLocalRef(env, buffArray);
    return CheckJNIExceptions(env) ? FAIL : bigNum;
}

int32_t AndroidCryptoNative_GetBigNumBytesIncludingPaddingByteForSign(jobject bignum)
{
    // JNI requires object passed to `Call*Method` to be not NULL
    abort_if_invalid_pointer_argument (bignum);

    // Use the array here to get the leading zero byte if it exists.
    // bigNum.toByteArray().length();
    JNIEnv* env = GetJNIEnv();
    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, bignum, g_toByteArrayMethod);
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    (*env)->DeleteLocalRef(env, bytes);
    return CheckJNIExceptions(env) ? FAIL : (int32_t)bytesLen;
}
