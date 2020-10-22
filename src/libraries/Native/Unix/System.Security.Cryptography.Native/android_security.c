// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_err.h"
#include "pal_types.h"
#include "pal_utilities.h"
#include "pal_safecrt.h"

#include "android_security.h"

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <jni.h>
#include <android/log.h>

static JavaVM *gJvm;

PALEXPORT JNIEXPORT jint JNICALL
JNI_OnLoad (JavaVM *vm, void *reserved)
{
    (void)reserved;
    __android_log_write(ANDROID_LOG_INFO, "DOTNET", "JNI_OnLoad in android_security.c");
    fflush(stdout);
    gJvm = vm;
    return JNI_VERSION_1_6;
}

static JNIEnv* GetJniEnv()
{
    assert(!gJvm && "JNI_OnLoad wasn't called in android_security.c. Did you call System.loadLibrary() for this lib?");

    JNIEnv *env;
    (*gJvm)->GetEnv(gJvm, (void**)&env, JNI_VERSION_1_6);
    if (env)
        return env;
    (*gJvm)->AttachCurrentThread(gJvm, &env, NULL);
    return env;
}

int32_t CryptoNative_GetRandomBytes_SecureRandom(uint8_t* buff, int32_t len)
{
    fflush(stdout);
    JNIEnv* jniEnv = GetJniEnv();

    jclass randClass = (*jniEnv)->FindClass (jniEnv, "java/security/SecureRandom");
    jmethodID randCtor = (*jniEnv)->GetMethodID(jniEnv, randClass, "<init>", "()V");
    jmethodID randNextBytes = (*jniEnv)->GetMethodID(jniEnv, randClass, "nextBytes", "([B)V");

    // Should we cache SecureRandom instance?
    jobject randObj = (*jniEnv)->NewObject(jniEnv, randClass, randCtor);
    jbyteArray buffArray = (*jniEnv)->NewByteArray(jniEnv, len);
    (*jniEnv)->SetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);
    (*jniEnv)->CallVoidMethod(jniEnv, randObj, randNextBytes, buffArray);
    (*jniEnv)->GetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);
    printf("\n[CryptoNative_GetRandomBytes]\n\n");
    return 1;
}
