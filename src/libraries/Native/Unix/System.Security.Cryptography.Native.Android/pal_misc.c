// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_misc.h"
#include "pal_jni.h"

int32_t CryptoNative_EnsureOpenSslInitialized()
{
    return 0;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    assert(g_randClass);
    assert(g_randCtor);
    JNIEnv* env = GetJNIEnv();
    jobject randObj = (*env)->NewObject(env, g_randClass, g_randCtor);
    assert(randObj && "Unable to create an instance of java/security/SecureRandom");

    jbyteArray buffArray = (*env)->NewByteArray(env, len);
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
