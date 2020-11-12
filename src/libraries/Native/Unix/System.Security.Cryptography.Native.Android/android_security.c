// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// javax/crypto/Mac
static jclass g_macClass = NULL;
static jmethodID g_macGetInstanceMethod = NULL;
static jmethodID g_macDoFinalMethod = NULL;
static jmethodID g_macUpdateMethod = NULL;
static jmethodID g_macInitMethod = NULL;
static jmethodID g_macResetMethod = NULL;

// javax/crypto/spec/SecretKeySpec
static jclass g_sksClass = NULL;
static jmethodID g_sksCtor = NULL;

// javax/crypto/Cipher
static jclass g_cipherClass = NULL;
static jmethodID g_cipherGetInstanceMethod = NULL;
static jmethodID g_cipherDoFinalMethod = NULL;
static jmethodID g_cipherUpdateMethod = NULL;
static jmethodID g_cipherInitMethod = NULL;
static jmethodID g_getBlockSizeMethod = NULL;

// javax/crypto/spec/IvParameterSpec
static jclass g_ivPsClass = NULL;
static jmethodID g_ivPsCtor = NULL;

static jobject ToGRef(JNIEnv *env, jobject lref)
{
    if (!lref)
        return NULL;
    jobject gref = (*env)->NewGlobalRef(env, lref);
    (*env)->DeleteLocalRef(env, lref);
    return gref;
}

static void ReleaseGRef(JNIEnv *env, jobject gref)
{
    if (gref)
        (*env)->DeleteGlobalRef(env, gref);
}

static jclass GetClassGRef(JNIEnv *env, const char* name)
{
    LOG_DEBUG("Finding %s class", name);
    jclass klass = ToGRef(env, (*env)->FindClass (env, name));
    if (!klass) {
        LOG_ERROR("class %s was not found", name);
        assert(klass);
    }
    return klass;
}

static bool CheckJNIExceptions(JNIEnv* env)
{
    if ((*env)->ExceptionCheck(env))
    {
        (*env)->ExceptionDescribe(env); 
        (*env)->ExceptionClear(env);
        return true;
    }
    return false;
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
    g_randClass =               GetClassGRef(env, "java/security/SecureRandom");
    g_randCtor =                GetMethod(env, false, g_randClass, "<init>", "()V");
    g_randNextBytesMethod =     GetMethod(env, false, g_randClass, "nextBytes", "([B)V");

    g_mdClass =                 GetClassGRef(env, "java/security/MessageDigest");
    g_mdGetInstanceMethod =     GetMethod(env, true,  g_mdClass, "getInstance", "(Ljava/lang/String;)Ljava/security/MessageDigest;");
    g_mdResetMethod =           GetMethod(env, false, g_mdClass, "reset", "()V");
    g_mdDigestMethod =          GetMethod(env, false, g_mdClass, "digest", "([B)[B");
    g_mdDigestCurrentMethodId = GetMethod(env, false, g_mdClass, "digest", "()[B");
    g_mdUpdateMethod =          GetMethod(env, false, g_mdClass, "update", "([B)V");

    g_macClass =                GetClassGRef(env, "javax/crypto/Mac");
    g_macGetInstanceMethod =    GetMethod(env, true,  g_macClass, "getInstance", "(Ljava/lang/String;)Ljavax/crypto/Mac;");
    g_macDoFinalMethod =        GetMethod(env, false, g_macClass, "doFinal", "()[B");
    g_macUpdateMethod =         GetMethod(env, false, g_macClass, "update", "([B)V");
    g_macInitMethod =           GetMethod(env, false, g_macClass, "init", "(Ljava/security/Key;)V");
    g_macResetMethod =          GetMethod(env, false, g_macClass, "reset", "()V");

    g_sksClass =                GetClassGRef(env, "javax/crypto/spec/SecretKeySpec");
    g_sksCtor =                 GetMethod(env, false, g_sksClass, "<init>", "([BLjava/lang/String;)V");

    g_cipherClass =             GetClassGRef(env, "javax/crypto/Cipher");
    g_cipherGetInstanceMethod = GetMethod(env, true,  g_cipherClass, "getInstance", "(Ljava/lang/String;)Ljavax/crypto/Cipher;");
    g_getBlockSizeMethod =      GetMethod(env, false, g_cipherClass, "getBlockSize", "()I");
    g_cipherDoFinalMethod =     GetMethod(env, false, g_cipherClass, "doFinal", "([BI)I");
    g_cipherUpdateMethod =      GetMethod(env, false, g_cipherClass, "update", "([B)[B");
    g_cipherInitMethod =        GetMethod(env, false, g_cipherClass, "init", "(ILjava/security/Key;Ljava/security/spec/AlgorithmParameterSpec;)V");

    g_ivPsClass =               GetClassGRef(env, "javax/crypto/spec/IvParameterSpec");
    g_ivPsCtor =                GetMethod(env, false, g_ivPsClass, "<init>", "([B)V");

    return JNI_VERSION_1_6;
}

int32_t CryptoNative_EnsureOpenSslInitialized(void)
{
    return 0;
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

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

// just some unique numbers
intptr_t CryptoNative_EvpMd5()          { return 101; }
intptr_t CryptoNative_EvpSha1()         { return 102; }
intptr_t CryptoNative_EvpSha256()       { return 103; }
intptr_t CryptoNative_EvpSha384()       { return 104; }
intptr_t CryptoNative_EvpSha512()       { return 105; }
intptr_t CryptoNative_EvpAes128Ecb()    { return 1001; }
intptr_t CryptoNative_EvpAes128Cbc()    { return 1002; }
intptr_t CryptoNative_EvpAes128Cfb8()   { return 1003; }
intptr_t CryptoNative_EvpAes128Cfb128() { return 1004; }
intptr_t CryptoNative_EvpAes128Gcm()    { return 1005; }
intptr_t CryptoNative_EvpAes128Ccm()    { return 1006; }
intptr_t CryptoNative_EvpAes192Ecb()    { return 1007; }
intptr_t CryptoNative_EvpAes192Cbc()    { return 1008; }
intptr_t CryptoNative_EvpAes192Cfb8()   { return 1009; }
intptr_t CryptoNative_EvpAes192Cfb128() { return 1010; }
intptr_t CryptoNative_EvpAes192Gcm()    { return 1011; }
intptr_t CryptoNative_EvpAes192Ccm()    { return 1012; }
intptr_t CryptoNative_EvpAes256Ecb()    { return 1013; }
intptr_t CryptoNative_EvpAes256Cbc()    { return 1014; }
intptr_t CryptoNative_EvpAes256Cfb8()   { return 1015; }
intptr_t CryptoNative_EvpAes256Cfb128() { return 1016; }
intptr_t CryptoNative_EvpAes256Gcm()    { return 1017; }
intptr_t CryptoNative_EvpAes256Ccm()    { return 1018; }
intptr_t CryptoNative_EvpDes3Ecb()      { return 1019; }
intptr_t CryptoNative_EvpDes3Cbc()      { return 1020; }
intptr_t CryptoNative_EvpDes3Cfb8()     { return 1021; }
intptr_t CryptoNative_EvpDes3Cfb64()    { return 1022; }
intptr_t CryptoNative_EvpDesEcb()       { return 1023; }
intptr_t CryptoNative_EvpDesCfb8()      { return 1024; }
intptr_t CryptoNative_EvpDesCbc()       { return 1025; }
intptr_t CryptoNative_EvpRC2Ecb()       { return 1026; }
intptr_t CryptoNative_EvpRC2Cbc()       { return 1027; }

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

int32_t CryptoNative_GetMaxMdSize()
{
    return EVP_MAX_MD_SIZE;
}

static jobject GetMessageDigestInstance(JNIEnv* env, intptr_t type)
{
    jobject mdName = NULL;
    if (type == CryptoNative_EvpSha1())
        mdName = (*env)->NewStringUTF(env, "SHA-1");
    else if (type == CryptoNative_EvpSha256())
        mdName = (*env)->NewStringUTF(env, "SHA-256");
    else if (type == CryptoNative_EvpSha384())
        mdName = (*env)->NewStringUTF(env, "SHA-384");
    else if (type == CryptoNative_EvpSha512())
        mdName = (*env)->NewStringUTF(env, "SHA-512");
    else if (type == CryptoNative_EvpMd5())
        mdName = (*env)->NewStringUTF(env, "MD5");
    else
        return NULL;

    jobject mdObj = (*env)->CallStaticObjectMethod(env, g_mdClass, g_mdGetInstanceMethod, mdName);
    (*env)->DeleteLocalRef(env, mdName);

    return CheckJNIExceptions(env) ? FAIL : mdObj;
}

int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (!type || !md || !mdSize || sourceSize < 0)
        return FAIL;

    JNIEnv* env = GetJniEnv();

    jobject mdObj = GetMessageDigestInstance(env, type);
    if (!mdObj)
        return FAIL;

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

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

void* CryptoNative_EvpMdCtxCreate(intptr_t type)
{
    JNIEnv* env = GetJniEnv();
    return (void*)ToGRef(env, GetMessageDigestInstance(env, type));
}

int32_t CryptoNative_EvpDigestReset(void* ctx, intptr_t type)
{
    if (!ctx)
        return FAIL;

    (void)type; // not used

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;
    (*env)->CallVoidMethod(env, mdObj, g_mdResetMethod);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpDigestUpdate(void* ctx, void* d, int32_t cnt)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;

    jbyteArray bytes = (*env)->NewByteArray(env, cnt);
    (*env)->SetByteArrayRegion(env, bytes, 0, cnt, (jbyte*) d);
    (*env)->CallVoidMethod(env, mdObj, g_mdUpdateMethod, bytes);
    (*env)->DeleteLocalRef(env, bytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpDigestFinalEx(void* ctx, uint8_t* md, uint32_t* s)
{
    return CryptoNative_EvpDigestCurrent(ctx, md, s);
}

int32_t CryptoNative_EvpDigestCurrent(void* ctx, uint8_t* md, uint32_t* s)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject mdObj = (jobject)ctx;

    jbyteArray bytes = (jbyteArray)(*env)->CallObjectMethod(env, mdObj, g_mdDigestCurrentMethodId);
    assert(bytes && "digest() was not expected to return null");
    jsize bytesLen = (*env)->GetArrayLength(env, bytes);
    *s = (uint32_t)bytesLen;
    (*env)->GetByteArrayRegion(env, bytes, 0, bytesLen, (jbyte*) md);
    (*env)->DeleteLocalRef(env, bytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

void CryptoNative_EvpMdCtxDestroy(void* ctx)
{
    ReleaseGRef(GetJniEnv(), (jobject)ctx);
}

void* CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, intptr_t type)
{
    assert(key || (keyLen == 0));
    assert(keyLen >= 0);

    // Mac mac = Mac.getInstance(algName);
    // SecretKeySpec key = new SecretKeySpec(key, algName);
    // mac.init(key);

    JNIEnv* env = GetJniEnv();

    jstring macName = NULL;
    if (type == CryptoNative_EvpSha1())
        macName = (jstring)(*env)->NewStringUTF(env, "HmacSHA1");
    else if (type == CryptoNative_EvpSha256())
        macName = (jstring)(*env)->NewStringUTF(env, "HmacSHA256");
    else if (type == CryptoNative_EvpSha384())
        macName = (jstring)(*env)->NewStringUTF(env, "HmacSHA384");
    else if (type == CryptoNative_EvpSha512())
        macName = (jstring)(*env)->NewStringUTF(env, "HmacSHA512");
    else if (type == CryptoNative_EvpMd5())
        macName = (jstring)(*env)->NewStringUTF(env, "HmacMD5");
    else
        return FAIL;

    jbyteArray keyBytes = (*env)->NewByteArray(env, keyLen);
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLen, (jbyte*)key);
    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, macName);
    assert(sksObj && "Unable to create an instance of SecretKeySpec");
    jobject macObj = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_macClass, g_macGetInstanceMethod, macName));
    (*env)->CallVoidMethod(env, macObj, g_macInitMethod, sksObj);
    (*env)->DeleteLocalRef(env, keyBytes);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, macName);

    return CheckJNIExceptions(env) ? FAIL : macObj;
}

int32_t CryptoNative_HmacReset(void* ctx)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject macObj = (jobject)ctx;
    (*env)->CallVoidMethod(env, macObj, g_macResetMethod);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacUpdate(void* ctx, uint8_t* data, int32_t len)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject macObj = (jobject)ctx;
    jbyteArray dataBytes = (*env)->NewByteArray(env, len);
    (*env)->SetByteArrayRegion(env, dataBytes, 0, len, (jbyte*)data);
    (*env)->CallVoidMethod(env, macObj, g_macUpdateMethod, dataBytes);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_HmacFinal(void* ctx, uint8_t* data, int32_t* len)
{
    return CryptoNative_HmacCurrent(ctx, data, len);
}

int32_t CryptoNative_HmacCurrent(void* ctx, uint8_t* data, int32_t* len)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject macObj = (jobject)ctx;
    jbyteArray dataBytes = (jbyteArray)(*env)->CallObjectMethod(env, macObj, g_macDoFinalMethod);
    jsize dataBytesLen = (*env)->GetArrayLength(env, dataBytes);
    *len = (int32_t)dataBytesLen;
    (*env)->GetByteArrayRegion(env, dataBytes, 0, dataBytesLen, (jbyte*) data);
    (*env)->DeleteLocalRef(env, dataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

void CryptoNative_HmacDestroy(void* ctx)
{
    ReleaseGRef(GetJniEnv(), (jobject)ctx);
}

// TODO: AES/DES

void* CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc)
{
    if (effectiveKeyLength != 0)
    {
        LOG_ERROR("Non-zero effectiveKeyLength is not supported");
        return FAIL;
    }

    // input:  0 for Decrypt, 1 for Encrypt
    // Cipher: 2 for Decrypt, 1 for Encrypt
    assert(enc == 0 || enc == 1);
    int encMode = enc == 0 ? 2 : 1;

    JNIEnv* env = GetJniEnv();
    // Cipher cipher = Cipher.getInstance("AES");
    // int ivSize = cipher.getBlockSize();
    // SecretKeySpec keySpec = new SecretKeySpec(key.getEncoded(), "AES");
    // IvParameterSpec ivSpec = new IvParameterSpec(IV);
    // cipher.init(encMode, keySpec, ivSpec);

    jobject algName = NULL;

    if ((type == CryptoNative_EvpAes128Cbc()) ||
        (type == CryptoNative_EvpAes192Cbc()) ||
        (type == CryptoNative_EvpAes256Cbc()))
    {
        algName = (*env)->NewStringUTF(env, "AES/CBC/NoPadding");
    }
    else if (
        (type == CryptoNative_EvpAes128Ecb()) ||
        (type == CryptoNative_EvpAes192Ecb()) ||
        (type == CryptoNative_EvpAes256Ecb()))
    {
        algName = (*env)->NewStringUTF(env, "AES/ECB/NoPadding");
    }
    else if (
        (type == CryptoNative_EvpAes128Cfb8()) ||
        (type == CryptoNative_EvpAes192Cfb8()) ||
        (type == CryptoNative_EvpAes256Cfb8()))
    {
        algName = (*env)->NewStringUTF(env, "AES/CFB/NoPadding");
    }
    else
    {
        LOG_ERROR("unexpected type: %d", (int)type);
        return FAIL;
    }

    jobject cipherObj = ToGRef(env, (*env)->CallStaticObjectMethod(env, g_cipherClass, g_cipherGetInstanceMethod, algName));

    int blockSize = (*env)->CallIntMethod(env, cipherObj, g_getBlockSizeMethod);
    jbyteArray keyBytes = (*env)->NewByteArray(env, keyLength / 8); // bits to bytes, e.g. 256 -> 32
    (*env)->SetByteArrayRegion(env, keyBytes, 0, keyLength / 8, (jbyte*)key);
    jbyteArray ivBytes = (*env)->NewByteArray(env, blockSize);
    (*env)->SetByteArrayRegion(env, ivBytes, 0, blockSize, (jbyte*)iv);

    jobject sksObj = (*env)->NewObject(env, g_sksClass, g_sksCtor, keyBytes, algName);
    jobject ivPsObj = (*env)->NewObject(env, g_ivPsClass, g_ivPsCtor, ivBytes);
    (*env)->CallVoidMethod(env, cipherObj, g_cipherInitMethod, encMode, sksObj, ivPsObj);

    (*env)->DeleteLocalRef(env, algName);
    (*env)->DeleteLocalRef(env, sksObj);
    (*env)->DeleteLocalRef(env, ivPsObj);
    (*env)->DeleteLocalRef(env, keyBytes);
    (*env)->DeleteLocalRef(env, ivBytes);

    return CheckJNIExceptions(env) ? FAIL : cipherObj;
}


int32_t CryptoNative_EvpCipherUpdate(void* ctx, uint8_t* outm, int32_t* outl, uint8_t* in, int32_t inl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject cipherObj = (jobject)ctx;
    jbyteArray inDataBytes = (*env)->NewByteArray(env, inl);
    (*env)->SetByteArrayRegion(env, inDataBytes, 0, inl, (jbyte*)in);
    jbyteArray outDataBytes = (jbyteArray)(*env)->CallObjectMethod(env, cipherObj, g_cipherUpdateMethod, inDataBytes);
    if (outDataBytes) {
        jsize outDataBytesLen = (*env)->GetArrayLength(env, outDataBytes);
        *outl = (int32_t)outDataBytesLen;
        (*env)->GetByteArrayRegion(env, outDataBytes, 0, outDataBytesLen, (jbyte*) outm);
        (*env)->DeleteLocalRef(env, outDataBytes);
    } else {
        *outl = 0;
    }

    (*env)->DeleteLocalRef(env, inDataBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherFinalEx(void* ctx, uint8_t* outm, int32_t* outl)
{
    if (!ctx)
        return FAIL;

    JNIEnv* env = GetJniEnv();
    jobject cipherObj = (jobject)ctx;

    int blockSize = (*env)->CallIntMethod(env, cipherObj, g_getBlockSizeMethod);
    jbyteArray outBytes = (*env)->NewByteArray(env, blockSize);
    int written = (*env)->CallIntMethod(env, cipherObj, g_cipherDoFinalMethod, outBytes, 0 /*offset*/);
    if (written > 0)
        (*env)->GetByteArrayRegion(env, outBytes, 0, blockSize, (jbyte*) outm);
    *outl = written;
    (*env)->DeleteLocalRef(env, outBytes);

    return CheckJNIExceptions(env) ? FAIL : SUCCESS;
}

int32_t CryptoNative_EvpCipherCtxSetPadding(void* x, int32_t padding)
{
    if (padding == 0)
    {
        return SUCCESS;
    }
    else
    {
        LOG_ERROR("Non-zero padding (%d) is not supported", (int)padding);
        return FAIL;
    }
}

int32_t CryptoNative_EvpCipherReset(void* ctx)
{
    // there is no "reset()" API for an existing Cipher object
    return SUCCESS;
}

void CryptoNative_EvpCipherDestroy(void* ctx)
{
    ReleaseGRef(GetJniEnv(), (jobject)ctx);
}
