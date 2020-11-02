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
#include <assert.h>

static JavaVM *gJvm;

// java/security/SecureRandom
static jclass g_randClass = NULL;
static jmethodID g_randCtor = NULL;
static jmethodID g_randNextBytes = NULL;

// java/security/MessageDigest
static jclass g_mdClass = NULL;
static jmethodID g_mdGetInstanceMethodId = NULL;
static jmethodID g_mdDigestMethodId = NULL;
static jmethodID g_mdResetMethodId = NULL;
static jmethodID g_mdUpdateMethodId = NULL;


PALEXPORT JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
    (void)reserved;
    __android_log_write(ANDROID_LOG_INFO, "DOTNET", "JNI_OnLoad in android_security.c");
    gJvm = vm;
    return JNI_VERSION_1_6;
}

static jobject ToGRef(JNIEnv *env, jobject lref)
{
    if (lref == 0)
        return 0;
    jobject gref = (*env)->NewGlobalRef(env, lref);
    (*env)->DeleteLocalRef(env, lref);
    return gref;
}

static JNIEnv* GetJniEnv()
{
    JNIEnv *env;
    (*gJvm)->GetEnv(gJvm, (void**)&env, JNI_VERSION_1_6);
    if (env)
        return env;
    jint ret = (*gJvm)->AttachCurrentThreadAsDaemon(gJvm, &env, NULL);
    assert(ret == JNI_OK && "Unable to attach thread to JVM");
    return env;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    JNIEnv* jniEnv = GetJniEnv();

    if (!g_randClass) {
        g_randClass = ToGRef(jniEnv, (*jniEnv)->FindClass (jniEnv, "java/security/SecureRandom"));
        g_randCtor = (*jniEnv)->GetMethodID(jniEnv, g_randClass, "<init>", "()V");
        g_randNextBytes = (*jniEnv)->GetMethodID(jniEnv, g_randClass, "nextBytes", "([B)V");
    }

    assert(g_randClass && "java/security/SecureRandom was not found");
    jobject randObj = (*jniEnv)->NewObject(jniEnv, g_randClass, g_randCtor);
    assert(randObj && "Unable to create an instance of java/security/SecureRandom");

    jbyteArray buffArray = (*jniEnv)->NewByteArray(jniEnv, len);
    (*jniEnv)->SetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);
    (*jniEnv)->CallVoidMethod(jniEnv, randObj, g_randNextBytes, buffArray);
    (*jniEnv)->GetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);

    (*jniEnv)->DeleteLocalRef(jniEnv, buffArray);
    (*jniEnv)->DeleteLocalRef(jniEnv, randObj);
    return SUCCESS;
}

// return some unique ids - e.g. max sizes
intptr_t CryptoNative_EvpMd5()       { return 16; }
intptr_t CryptoNative_EvpSha1()      { return 20; }
intptr_t CryptoNative_EvpSha256()    { return 32; }
intptr_t CryptoNative_EvpSha384()    { return 48; }
intptr_t CryptoNative_EvpSha512()    { return 64; }
int32_t  CryptoNative_GetMaxMdSize() { return 64; }

int32_t CryptoNative_EvpMdSize(intptr_t md)
{
    // we can call "MessageDigest.getInstance("...").getDigestLength()" to make sure
    // but md id is already the actual size we need.
    return (int32_t)md;
}

static jclass GetMessageDigestClass(JNIEnv* jniEnv)
{
    if (!g_mdClass) {
        g_mdClass = ToGRef(jniEnv, (*jniEnv)->FindClass (jniEnv, "java/security/MessageDigest"));
    }
    assert(g_mdClass && "java/security/MessageDigest was not found");
    return g_mdClass;
}

static jobject GetMessageDigestInstance(JNIEnv* jniEnv, intptr_t type)
{
    jclass mdClass = GetMessageDigestClass(jniEnv);

    if (!g_mdGetInstanceMethodId) {
        g_mdGetInstanceMethodId = (*jniEnv)->GetStaticMethodID(
            jniEnv, mdClass, "getInstance", "(Ljava/lang/String;)Ljava/security/MessageDigest;");
    }
    assert(g_mdGetInstanceMethodId && "MessageDigest.getInstance(...) was not found");

    jstring mdName = NULL;
    if (type == CryptoNative_EvpMd5())
        mdName = (jstring)(*jniEnv)->NewStringUTF(jniEnv, "MD5");
    else if (type == CryptoNative_EvpSha1())
        mdName = (jstring)(*jniEnv)->NewStringUTF(jniEnv, "SHA-1");
    else if (type == CryptoNative_EvpSha256())
        mdName = (jstring)(*jniEnv)->NewStringUTF(jniEnv, "SHA-256");
    else if (type == CryptoNative_EvpSha384())
        mdName = (jstring)(*jniEnv)->NewStringUTF(jniEnv, "SHA-384");
    else if (type == CryptoNative_EvpSha512())
        mdName = (jstring)(*jniEnv)->NewStringUTF(jniEnv, "SHA-512");
    else
        return NULL;

    jobject mdObj = (*jniEnv)->CallStaticObjectMethod(jniEnv, mdClass, g_mdGetInstanceMethodId, mdName);
    (*jniEnv)->DeleteLocalRef(jniEnv, mdName);
    return mdObj;
}

int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (!type || !md || !mdSize || sourceSize < 0)
        return 0;

    JNIEnv* jniEnv = GetJniEnv();
    jclass mdClass = GetMessageDigestClass(jniEnv);

    // MessageDigest md = MessageDigest.getInstance("...");
    // hashed = md.digest(src);

    if (!g_mdDigestMethodId)
        g_mdDigestMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "digest", "([B)[B");
    assert(g_mdDigestMethodId && "MessageDigest.digest(...) was not found");

    jobject mdObj = GetMessageDigestInstance(jniEnv, type);
    if (!mdObj)
        return 0;

    jbyteArray bytes = (*jniEnv)->NewByteArray(jniEnv, sourceSize);
    (*jniEnv)->SetByteArrayRegion(jniEnv, bytes, 0, sourceSize, (jbyte*) source);
    jbyteArray hashedBytes = (jbyteArray)(*jniEnv)->CallObjectMethod(jniEnv, mdObj, g_mdDigestMethodId, bytes);
    assert(hashedBytes && "MessageDigest.digest(...) was not expected to return null");

    jsize hashedBytesLen = (*jniEnv)->GetArrayLength(jniEnv, hashedBytes);
    (*jniEnv)->GetByteArrayRegion(jniEnv, hashedBytes, 0, hashedBytesLen, (jbyte*) md);
    *mdSize = (uint32_t)hashedBytesLen;

    (*jniEnv)->DeleteLocalRef(jniEnv, bytes);
    (*jniEnv)->DeleteLocalRef(jniEnv, hashedBytes);
    (*jniEnv)->DeleteLocalRef(jniEnv, mdObj);
    return SUCCESS;
}

void* CryptoNative_EvpMdCtxCreate(intptr_t type)
{
    JNIEnv* jniEnv = GetJniEnv();
    jobject md = ToGRef(jniEnv, GetMessageDigestInstance(jniEnv, type));
    // md can be null (caller will handle it as an error)
    // global ref is released in CryptoNative_EvpMdCtxDestroy
    return (void*)md;
}

int32_t CryptoNative_EvpDigestReset(void* ctx, intptr_t type)
{
    if (!ctx)
        return 0;

    (void)type; // not used

    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = GetMessageDigestClass(jniEnv);

    if (!g_mdResetMethodId)
        g_mdResetMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "reset", "()V");
    assert(g_mdResetMethodId && "MessageDigest.reset() was not found");

    (*jniEnv)->CallVoidMethod(jniEnv, mdObj, g_mdResetMethodId);
    return SUCCESS;
}

int32_t CryptoNative_EvpDigestUpdate(void* ctx, void* d, int32_t cnt)
{
    if (!ctx)
        return 0;

    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = GetMessageDigestClass(jniEnv);

    if (!g_mdUpdateMethodId)
        g_mdUpdateMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "update", "([B)V");
    assert(g_mdUpdateMethodId && "MessageDigest.update(...) was not found");

    jbyteArray bytes = (*jniEnv)->NewByteArray(jniEnv, cnt);
    (*jniEnv)->SetByteArrayRegion(jniEnv, bytes, 0, cnt, (jbyte*) d);
    (*jniEnv)->CallVoidMethod(jniEnv, mdObj, g_mdUpdateMethodId, bytes);
    (*jniEnv)->DeleteLocalRef(jniEnv, bytes);

    return SUCCESS;
}

int32_t CryptoNative_EvpDigestFinalEx(void* ctx, uint8_t* md, uint32_t* s)
{
    return CryptoNative_EvpDigestCurrent(ctx, md, s);
}

int32_t CryptoNative_EvpDigestCurrent(void* ctx, uint8_t* md, uint32_t* s)
{
    if (!ctx)
        return 0;

    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = GetMessageDigestClass(jniEnv);

    if (!g_mdDigestMethodId)
        g_mdDigestMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "digest", "()[B");
    assert(g_mdDigestMethodId && "MessageDigest.digest() was not found");

    jbyteArray bytes = (jbyteArray)(*jniEnv)->CallObjectMethod(jniEnv, mdObj, g_mdDigestMethodId);
    jsize bytesLen = (*jniEnv)->GetArrayLength(jniEnv, bytes);
    *s = (uint32_t)bytesLen;
    (*jniEnv)->GetByteArrayRegion(jniEnv, bytes, 0, bytesLen, (jbyte*) md);
    (*jniEnv)->DeleteLocalRef(jniEnv, bytes);

    return SUCCESS;
}

void CryptoNative_EvpMdCtxDestroy(void* ctx)
{
    if (ctx)
    {
        JNIEnv* jniEnv = GetJniEnv();
        (*jniEnv)->DeleteGlobalRef(jniEnv, (jobject)ctx);
    }
}

