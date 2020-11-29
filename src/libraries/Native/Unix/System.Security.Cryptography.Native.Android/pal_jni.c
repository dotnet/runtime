// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

JavaVM* gJvm;

// java/security/SecureRandom
jclass    g_randClass;
jmethodID g_randCtor;
jmethodID g_randNextBytesMethod;

// java/security/MessageDigest
jclass    g_mdClass;
jmethodID g_mdGetInstanceMethod;
jmethodID g_mdDigestMethod;
jmethodID g_mdDigestCurrentMethodId;
jmethodID g_mdResetMethod;
jmethodID g_mdUpdateMethod;

// javax/crypto/Mac
jclass    g_macClass;
jmethodID g_macGetInstanceMethod;
jmethodID g_macDoFinalMethod;
jmethodID g_macUpdateMethod;
jmethodID g_macInitMethod;
jmethodID g_macResetMethod;

// javax/crypto/spec/SecretKeySpec
jclass    g_sksClass;
jmethodID g_sksCtor;

// javax/crypto/Cipher
jclass    g_cipherClass;
jmethodID g_cipherGetInstanceMethod;
jmethodID g_cipherDoFinalMethod;
jmethodID g_cipherUpdateMethod;
jmethodID g_cipherUpdateAADMethod;
jmethodID g_cipherInitMethod;
jmethodID g_getBlockSizeMethod;

// javax/crypto/spec/IvParameterSpec
jclass    g_ivPsClass;
jmethodID g_ivPsCtor;

// java/math/BigInteger
jclass    g_bigNumClass;
jmethodID g_bigNumCtor;
jmethodID g_toByteArrayMethod;

// javax/net/ssl/SSLParameters
jclass    g_sslParamsClass;
jmethodID g_sslParamsGetProtocolsMethod;

// javax/net/ssl/SSLContext
jclass    g_sslCtxClass;
jmethodID g_sslCtxGetDefaultMethod;
jmethodID g_sslCtxGetDefaultSslParamsMethod;

// javax/crypto/spec/GCMParameterSpec
jclass    g_GCMParameterSpecClass;
jmethodID g_GCMParameterSpecCtor;

jobject ToGRef(JNIEnv *env, jobject lref)
{
    if (!lref)
        return NULL;
    jobject gref = (*env)->NewGlobalRef(env, lref);
    (*env)->DeleteLocalRef(env, lref);
    return gref;
}

void ReleaseGRef(JNIEnv *env, jobject gref)
{
    if (gref)
        (*env)->DeleteGlobalRef(env, gref);
}

jclass GetClassGRef(JNIEnv *env, const char* name)
{
    LOG_DEBUG("Finding %s class", name);
    jclass klass = ToGRef(env, (*env)->FindClass (env, name));
    if (!klass) {
        LOG_ERROR("class %s was not found", name);
        assert(klass);
    }
    return klass;
}

bool CheckJNIExceptions(JNIEnv* env)
{
    if ((*env)->ExceptionCheck(env))
    {
        (*env)->ExceptionDescribe(env); 
        (*env)->ExceptionClear(env);
        return true;
    }
    return false;
}

void SaveTo(uint8_t* src, uint8_t** dst, size_t len)
{
    assert(!(*dst));
    *dst = (uint8_t*)malloc(len * sizeof(uint8_t));
    memcpy(*dst, src, len);
}

jmethodID GetMethod(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig)
{
    LOG_DEBUG("Finding %s method", name);
    jmethodID mid = isStatic ? (*env)->GetStaticMethodID(env, klass, name, sig) : (*env)->GetMethodID(env, klass, name, sig);
    if (!mid) {
        LOG_ERROR("method %s %s was not found", name, sig);
        assert(mid);
    }
    return mid;
}

JNIEnv* GetJNIEnv()
{
    JNIEnv *env;
    (*gJvm)->GetEnv(gJvm, (void**)&env, JNI_VERSION_1_6);
    if (env)
        return env;
    jint ret = (*gJvm)->AttachCurrentThreadAsDaemon(gJvm, &env, NULL);
    assert(ret == JNI_OK && "Unable to attach thread to JVM");
    (void)ret;
    return env;
}

PALEXPORT JNIEXPORT jint JNICALL
JNI_OnLoad(JavaVM *vm, void *reserved)
{
    (void)reserved;
    LOG_INFO("JNI_OnLoad in pal_jni.c");
    gJvm = vm;

    JNIEnv* env = GetJNIEnv();

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
    g_cipherDoFinalMethod =     GetMethod(env, false, g_cipherClass, "doFinal", "()[B");
    g_cipherUpdateMethod =      GetMethod(env, false, g_cipherClass, "update", "([B)[B");
    g_cipherUpdateAADMethod =   GetMethod(env, false, g_cipherClass, "updateAAD", "([B)V");
    g_cipherInitMethod =        GetMethod(env, false, g_cipherClass, "init", "(ILjava/security/Key;Ljava/security/spec/AlgorithmParameterSpec;)V");

    g_ivPsClass =               GetClassGRef(env, "javax/crypto/spec/IvParameterSpec");
    g_ivPsCtor =                GetMethod(env, false, g_ivPsClass, "<init>", "([B)V");

    g_GCMParameterSpecClass =   GetClassGRef(env, "javax/crypto/spec/GCMParameterSpec");
    g_GCMParameterSpecCtor =    GetMethod(env, false, g_GCMParameterSpecClass, "<init>", "(I[B)V");

    g_bigNumClass =             GetClassGRef(env, "java/math/BigInteger");
    g_bigNumCtor =              GetMethod(env, false, g_bigNumClass, "<init>", "([B)V");
    g_toByteArrayMethod =       GetMethod(env, false, g_bigNumClass, "toByteArray", "()[B");

    g_sslParamsClass =              GetClassGRef(env, "javax/net/ssl/SSLParameters");
    g_sslParamsGetProtocolsMethod = GetMethod(env, false,  g_sslParamsClass, "getProtocols", "()[Ljava/lang/String;");

    g_sslCtxClass =                     GetClassGRef(env, "javax/net/ssl/SSLContext");
    g_sslCtxGetDefaultMethod =          GetMethod(env, true,  g_sslCtxClass, "getDefault", "()Ljavax/net/ssl/SSLContext;");
    g_sslCtxGetDefaultSslParamsMethod = GetMethod(env, false, g_sslCtxClass, "getDefaultSSLParameters", "()Ljavax/net/ssl/SSLParameters;");

    return JNI_VERSION_1_6;
}
