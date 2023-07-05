// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp.h"

// just some unique IDs
intptr_t CryptoNative_EvpMd5(void)    { return 101; }
intptr_t CryptoNative_EvpSha1(void)   { return 102; }
intptr_t CryptoNative_EvpSha256(void) { return 103; }
intptr_t CryptoNative_EvpSha384(void) { return 104; }
intptr_t CryptoNative_EvpSha512(void) { return 105; }

int32_t CryptoNative_EvpMdSize(intptr_t md)
{
    if (md == CryptoNative_EvpSha1()) return 20;
    if (md == CryptoNative_EvpSha256()) return 32;
    if (md == CryptoNative_EvpSha384()) return 48;
    if (md == CryptoNative_EvpSha512()) return 64;
    if (md == CryptoNative_EvpMd5()) return 16;
    assert(0 && "unexpected type");
    return -1;
}

int32_t CryptoNative_GetMaxMdSize(void)
{
    return EVP_MAX_MD_SIZE;
}

static jobject GetMessageDigestInstance(JNIEnv* env, intptr_t type)
{
    jobject mdName = NULL;
    if (type == CryptoNative_EvpSha1())
        mdName = make_java_string(env, "SHA-1");
    else if (type == CryptoNative_EvpSha256())
        mdName = make_java_string(env, "SHA-256");
    else if (type == CryptoNative_EvpSha384())
        mdName = make_java_string(env, "SHA-384");
    else if (type == CryptoNative_EvpSha512())
        mdName = make_java_string(env, "SHA-512");
    else if (type == CryptoNative_EvpMd5())
        mdName = make_java_string(env, "MD5");
    else
        return NULL;

    jobject mdObj = (*env)->CallStaticObjectMethod(env, g_mdClass, g_mdGetInstance, mdName);
    (*env)->DeleteLocalRef(env, mdName);

    return CheckJNIExceptions(env) ? FAIL : mdObj;
}

int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (!type || !md || !mdSize || sourceSize < 0 || (sourceSize > 0 && !source))
        return FAIL;

    JNIEnv* env = GetJNIEnv();

    jobject mdObj = GetMessageDigestInstance(env, type);
    if (!mdObj)
        return FAIL;

    jbyteArray bytes = make_java_byte_array(env, sourceSize);
    (*env)->SetByteArrayRegion(env, bytes, 0, sourceSize, (jbyte*) source);
    jbyteArray hashedBytes = (jbyteArray)(*env)->CallObjectMethod(env, mdObj, g_mdDigestWithInputBytes, bytes);
    abort_unless(hashedBytes != NULL, "MessageDigest.digest(...) was not expected to return null");

    jsize hashedBytesLen = (*env)->GetArrayLength(env, hashedBytes);
    (*env)->GetByteArrayRegion(env, hashedBytes, 0, hashedBytesLen, (jbyte*) md);
    *mdSize = (uint32_t)hashedBytesLen;

    (*env)->DeleteLocalRef(env, bytes);
    (*env)->DeleteLocalRef(env, hashedBytes);
    (*env)->DeleteLocalRef(env, mdObj);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

jobject CryptoNative_EvpMdCtxCreate(intptr_t type)
{
    JNIEnv* env = GetJNIEnv();
    return (void*)ToGRef(env, GetMessageDigestInstance(env, type));
}

int32_t CryptoNative_EvpDigestReset(jobject ctx, intptr_t type)
{
    abort_if_invalid_pointer_argument (ctx);

    JNIEnv* env = GetJNIEnv();
    (*env)->CallVoidMethod(env, ctx, g_mdReset);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpDigestUpdate(jobject ctx, void* d, int32_t cnt)
{
    abort_if_invalid_pointer_argument (ctx);
    if(cnt > 0)
        abort_if_invalid_pointer_argument (d);
    JNIEnv* env = GetJNIEnv();

    jbyteArray bytes = make_java_byte_array(env, cnt);
    (*env)->SetByteArrayRegion(env, bytes, 0, cnt, (jbyte*) d);
    (*env)->CallVoidMethod(env, ctx, g_mdUpdate, bytes);
    (*env)->DeleteLocalRef(env, bytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

static int32_t DigestFinal(JNIEnv* env, jobject ctx, uint8_t* md, uint32_t* s)
{
    abort_if_invalid_pointer_argument (md);

    // ctx.digest();
    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, ctx, g_mdDigest);
    abort_unless(bytes != NULL, "digest() was not expected to return null");
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    *s = (uint32_t)bytesLen;
    (*env)->GetByteArrayRegion(env, bytes, 0, bytesLen, (jbyte*) md);
    (*env)->DeleteLocalRef(env, bytes);
    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpDigestFinalEx(jobject ctx, uint8_t* md, uint32_t* s)
{
    abort_if_invalid_pointer_argument (ctx);

    JNIEnv* env = GetJNIEnv();
    return DigestFinal(env, ctx, md, s);
}

int32_t CryptoNative_EvpDigestCurrent(jobject ctx, uint8_t* md, uint32_t* s)
{
    abort_if_invalid_pointer_argument (ctx);

    JNIEnv* env = GetJNIEnv();
    int32_t ret = FAIL;

    // ctx.clone();
    jobject clone = (*env)->CallObjectMethod(env, ctx, g_mdClone);
    ON_EXCEPTION_PRINT_AND_GOTO(cleanup);
    ret = DigestFinal(env, clone, md, s);

cleanup:
    (*env)->DeleteLocalRef(env, clone);
    return ret;
}

void CryptoNative_EvpMdCtxDestroy(jobject ctx)
{
    ReleaseGRef(GetJNIEnv(), ctx);
}
