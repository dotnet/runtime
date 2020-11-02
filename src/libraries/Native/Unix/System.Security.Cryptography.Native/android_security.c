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

#define LOG_DEBUG(fmt, ...) ((void)__android_log_print(ANDROID_LOG_DEBUG, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define LOG_INFO(fmt, ...) ((void)__android_log_print(ANDROID_LOG_INFO, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define LOG_ERROR(fmt, ...) ((void)__android_log_print(ANDROID_LOG_ERROR, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))

// java/security/SecureRandom
static jclass g_randClass = NULL;
static jmethodID g_randCtor = NULL;
static jmethodID g_randNextBytesMethod = NULL;

// java/security/MessageDigest
static jclass g_mdClass = NULL;
static jmethodID g_mdGetInstanceMethod = NULL;
static jmethodID g_mdDigestMethod = NULL;
static jmethodID g_mdDigestCurrentMethodId = NULL;
static jmethodID g_mdResetMethod = NULL;
static jmethodID g_mdUpdateMethod = NULL;

static jobject ToGRef(JNIEnv *env, jobject lref)
{
    if (lref == 0)
        return 0;
    jobject gref = (*env)->NewGlobalRef(env, lref);
    (*env)->DeleteLocalRef(env, lref);
    return gref;
}

static jclass GetClassGref(JNIEnv *env, const char* name)
{
    LOG_DEBUG("Finding %s class", name);
    jclass klass = ToGRef(env, (*env)->FindClass (env, name));
    if (!klass) {
        LOG_ERROR("class %s was not found", name);
        assert(klass);
    }
    return klass;
}

static jmethodID GetMethod(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig)
{
    LOG_DEBUG("Finding %s method", name);
    jmethodID mid = isStatic ? (*env)->GetStaticMethodID(env, klass, name, sig) : (*env)->GetMethodID(env, klass, name, sig);
    if (!mid) {
        LOG_ERROR("method %s %s was not found", name, sig);
        assert(mid);
    }
    return mid;
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

PALEXPORT JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
    (void)reserved;
    LOG_INFO("JNI_OnLoad in android_security.c");
    gJvm = vm;

    JNIEnv* env = GetJniEnv();

    // cache some classes and methods while we're in the thread-safe JNI_OnLoad
    g_randClass =               GetClassGref(env, "java/security/SecureRandom");
    g_randCtor =                GetMethod(env, false, g_randClass, "<init>", "()V");
    g_randNextBytesMethod =     GetMethod(env, false, g_randClass, "nextBytes", "([B)V");

    g_mdClass =                 GetClassGref(env, "java/security/MessageDigest");
    g_mdGetInstanceMethod =     GetMethod(env, true,  g_mdClass, "getInstance", "(Ljava/lang/String;)Ljava/security/MessageDigest;");
    g_mdResetMethod =           GetMethod(env, false, g_mdClass, "reset", "()V");
    g_mdDigestMethod =          GetMethod(env, false, g_mdClass, "digest", "([B)[B");
    g_mdDigestCurrentMethodId = GetMethod(env, false, g_mdClass, "digest", "()[B");
    g_mdUpdateMethod =          GetMethod(env, false, g_mdClass, "update", "([B)V");

    return JNI_VERSION_1_6;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    JNIEnv* env = GetJniEnv();
    jobject randObj = (*env)->NewObject(env, g_randClass, g_randCtor);
    assert(randObj && "Unable to create an instance of java/security/SecureRandom");

    jbyteArray buffArray = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, buffArray, 0, len, (jbyte*)buff);
    (*env)->CallVoidMethod(env, randObj, g_randNextBytesMethod, buffArray);
    (*env)->GetByteArrayRegion(env, buffArray, 0, len, (jbyte*)buff);

    (*env)->DeleteLocalRef(env, buffArray);
    (*env)->DeleteLocalRef(env, randObj);
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

static jobject GetMessageDigestInstance(JNIEnv* env, intptr_t type)
{
    jstring mdName = NULL;
    if (type == CryptoNative_EvpSha1())
        mdName = (jstring)(*env)->NewStringUTF(env, "SHA-1");
    else if (type == CryptoNative_EvpSha256())
        mdName = (jstring)(*env)->NewStringUTF(env, "SHA-256");
    else if (type == CryptoNative_EvpSha384())
        mdName = (jstring)(*env)->NewStringUTF(env, "SHA-384");
    else if (type == CryptoNative_EvpSha512())
        mdName = (jstring)(*env)->NewStringUTF(env, "SHA-512");
    else if (type == CryptoNative_EvpMd5())
        mdName = (jstring)(*env)->NewStringUTF(env, "MD5");
    else
        return NULL;

    jobject mdObj = (*env)->CallStaticObjectMethod(env, g_mdClass, g_mdGetInstanceMethod, mdName);
    (*env)->DeleteLocalRef(env, mdName);
    return mdObj;
}

int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (!type || !md || !mdSize || sourceSize < 0)
        return 0;

    JNIEnv* env = GetJniEnv();

    // MessageDigest md = MessageDigest.getInstance("...");
    // hashed = md.digest(src);

    jobject mdObj = GetMessageDigestInstance(env, type);
    if (!mdObj)
        return 0;

    jbyteArray bytes = (*env)->NewByteArray(env, sourceSize);
    (*env)->SetByteArrayRegion(env, bytes, 0, sourceSize, (jbyte*) source);
    jbyteArray hashedBytes = (jbyteArray)(*env)->CallObjectMethod(env, mdObj, g_mdDigestMethod, bytes);
    assert(hashedBytes && "MessageDigest.digest(...) was not expected to return null");

    jsize hashedBytesLen = (*env)->GetArrayLength(env, hashedBytes);
    (*env)->GetByteArrayRegion(env, hashedBytes, 0, hashedBytesLen, (jbyte*) md);
    *mdSize = (uint32_t)hashedBytesLen;

    (*env)->DeleteLocalRef(env, bytes);
    (*env)->DeleteLocalRef(env, hashedBytes);
    (*env)->DeleteLocalRef(env, mdObj);
    return SUCCESS;
}

void* CryptoNative_EvpMdCtxCreate(intptr_t type)
{
    JNIEnv* env = GetJniEnv();
    jobject md = ToGRef(env, GetMessageDigestInstance(env, type));
    // md can be null (caller will handle it as an error)
    // global ref is released in CryptoNative_EvpMdCtxDestroy
    return (void*)md;
}

int32_t CryptoNative_EvpDigestReset(void* ctx, intptr_t type)
{
    if (!ctx)
        return 0;

    (void)type; // not used

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    (*env)->CallVoidMethod(env, mdObj, g_mdResetMethod);
    return SUCCESS;
}

int32_t CryptoNative_EvpDigestUpdate(void* ctx, void* d, int32_t cnt)
{
    if (!ctx)
        return 0;

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;

    jbyteArray bytes = (*env)->NewByteArray(env, cnt);
    (*env)->SetByteArrayRegion(env, bytes, 0, cnt, (jbyte*) d);
    (*env)->CallVoidMethod(env, mdObj, g_mdUpdateMethod, bytes);
    (*env)->DeleteLocalRef(env, bytes);

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

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;

    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, mdObj, g_mdDigestCurrentMethodId);
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    *s = (uint32_t)bytesLen;
    (*env)->GetByteArrayRegion(env, bytes, 0, bytesLen, (jbyte*) md);
    (*env)->DeleteLocalRef(env, bytes);

    return SUCCESS;
}

void CryptoNative_EvpMdCtxDestroy(void* ctx)
{
    if (ctx)
    {
        JNIEnv* env = GetJniEnv();
        (*env)->DeleteGlobalRef(env, (jobject)ctx);
    }
}

