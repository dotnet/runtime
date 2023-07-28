// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_misc.h"
#include "pal_jni.h"

int32_t CryptoNative_EnsureOpenSslInitialized(void)
{
    return 0;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    // JNI requires `buff` to be not NULL when passed to `{Get,Set}ByteArrayRegion`
    abort_unless(buff != NULL, "The 'buff' parameter must be a valid pointer");

    JNIEnv* env = GetJNIEnv();
    jobject randObj = (*env)->NewObject(env, g_randClass, g_randCtor);
    abort_unless(randObj != NULL,"Unable to create an instance of java/security/SecureRandom");

    jbyteArray buffArray = make_java_byte_array(env, len);
    (*env)->SetByteArrayRegion(env, buffArray, 0, len, (jbyte*)buff);
    (*env)->CallVoidMethod(env, randObj, g_randNextBytesMethod, buffArray);
    (*env)->GetByteArrayRegion(env, buffArray, 0, len, (jbyte*)buff);

    (*env)->DeleteLocalRef(env, buffArray);
    (*env)->DeleteLocalRef(env, randObj);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

jobject AndroidCryptoNative_CreateKeyPair(JNIEnv* env, jobject publicKey, jobject privateKey)
{
    jobject keyPair = (*env)->NewObject(env, g_keyPairClass, g_keyPairCtor, publicKey, privateKey);
    return CheckJNIExceptions(env) ? FAIL : ToGRef(env, keyPair);
}
