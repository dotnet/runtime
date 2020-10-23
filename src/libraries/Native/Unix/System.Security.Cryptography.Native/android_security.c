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
    (*gJvm)->AttachCurrentThread(gJvm, &env, NULL);
    return env;
}

int32_t CryptoNative_GetRandomBytes(uint8_t* buff, int32_t len)
{
    JNIEnv* jniEnv = GetJniEnv();

    jclass randClass = (*jniEnv)->FindClass (jniEnv, "java/security/SecureRandom");
    jmethodID randCtor = (*jniEnv)->GetMethodID(jniEnv, randClass, "<init>", "()V");
    jmethodID randNextBytes = (*jniEnv)->GetMethodID(jniEnv, randClass, "nextBytes", "([B)V");

    // TODO: should we cache SecureRandom instance?
    jobject randObj = (*jniEnv)->NewObject(jniEnv, randClass, randCtor);
    jbyteArray buffArray = (*jniEnv)->NewByteArray(jniEnv, len);
    (*jniEnv)->SetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);
    (*jniEnv)->CallVoidMethod(jniEnv, randObj, randNextBytes, buffArray);
    (*jniEnv)->GetByteArrayRegion(jniEnv, buffArray, 0, len, (jbyte*)buff);

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
    // MessageDigest.getInstance("...").getDigestLength();
    // but md id is actually a size
    return (int32_t)md;
}

static jobject GetMessageDigestInstance(JNIEnv* jniEnv, intptr_t type)
{
    // return MessageDigest.getInstance("...");

    jclass mdClass = (*jniEnv)->FindClass (jniEnv, "java/security/MessageDigest");
    jmethodID mdGetInstanceMethodId = (*jniEnv)->GetStaticMethodID(
        jniEnv, mdClass, "getInstance", "(Ljava/lang/String;)Ljava/security/MessageDigest;");

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

    return (*jniEnv)->CallStaticObjectMethod(jniEnv, mdClass, mdGetInstanceMethodId, mdName);
}

int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (!type || !md || !mdSize || sourceSize < 0)
        return 0;

    JNIEnv* jniEnv = GetJniEnv();

    // MessageDigest md = MessageDigest.getInstance("...");
    // hashed = md.digest(src);

    jclass mdClass = (*jniEnv)->FindClass(jniEnv, "java/security/MessageDigest");
    jmethodID mdDigestMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "digest", "([B)[B");

    // TODO: for one-shot digest()s it makes sense to cache instance (convert to a gref)
    jobject mdObj = GetMessageDigestInstance(jniEnv, type);
    if (!mdObj)
        return 0;

    jbyteArray bytes = (*jniEnv)->NewByteArray(jniEnv, sourceSize);
    (*jniEnv)->SetByteArrayRegion(jniEnv, bytes, 0, sourceSize, (jbyte*) source);
    jbyteArray hashedBytes = (jbyteArray)(*jniEnv)->CallObjectMethod(jniEnv, mdObj, mdDigestMethodId, bytes);

    jsize hashedBytesLen = (*jniEnv)->GetArrayLength(jniEnv, hashedBytes);
    (*jniEnv)->GetByteArrayRegion(jniEnv, hashedBytes, 0, hashedBytesLen, (jbyte*) md);
    *mdSize = (uint32_t)hashedBytesLen;

    // TODO: should we delete all locals by hands here?

    return SUCCESS;
}

void* CryptoNative_EvpMdCtxCreate(intptr_t type)
{
    JNIEnv* jniEnv = GetJniEnv();
    jobject md = ToGRef(jniEnv, GetMessageDigestInstance(jniEnv, type));
    return (void*)md;
}

int32_t CryptoNative_EvpDigestReset(void* ctx, intptr_t type)
{
    (void)type;

    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = (*jniEnv)->FindClass(jniEnv, "java/security/MessageDigest");
    jmethodID mdResetMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "reset", "()V");
    (*jniEnv)->CallVoidMethod(jniEnv, mdObj, mdResetMethodId);
    return SUCCESS;
}

int32_t CryptoNative_EvpDigestUpdate(void* ctx, void* d, int32_t cnt)
{
    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = (*jniEnv)->FindClass(jniEnv, "java/security/MessageDigest");
    jmethodID mdUpdateMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "update", "([B)V");

    jbyteArray bytes = (*jniEnv)->NewByteArray(jniEnv, cnt);
    (*jniEnv)->SetByteArrayRegion(jniEnv, bytes, 0, cnt, (jbyte*) d);
    (*jniEnv)->CallVoidMethod(jniEnv, mdObj, mdUpdateMethodId, bytes);

    return SUCCESS;
}

int32_t CryptoNative_EvpDigestFinalEx(void* ctx, uint8_t* md, uint32_t* s)
{
    int32_t ret = CryptoNative_EvpDigestCurrent(ctx, md, s);
    return ret;
}

int32_t CryptoNative_EvpDigestCurrent(void* ctx, uint8_t* md, uint32_t* s)
{
    JNIEnv* jniEnv = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    jclass mdClass = (*jniEnv)->FindClass(jniEnv, "java/security/MessageDigest");
    jmethodID mdDigestMethodId = (*jniEnv)->GetMethodID(jniEnv, mdClass, "digest", "()[B");
    jbyteArray bytes = (jbyteArray)(*jniEnv)->CallObjectMethod(jniEnv, mdObj, mdDigestMethodId);
    jsize bytesLen = (*jniEnv)->GetArrayLength(jniEnv, bytes);
    *s = (uint32_t)bytesLen;
    (*jniEnv)->GetByteArrayRegion(jniEnv, bytes, 0, bytesLen, (jbyte*) md);
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

