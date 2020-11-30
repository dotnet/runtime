// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <jni.h>
#include <android/log.h>
#include <assert.h>
#include <stdlib.h>
#include "pal_safecrt.h"

#define FAIL 0
#define SUCCESS 1

extern JavaVM* gJvm;

// java/security/SecureRandom
extern jclass    g_randClass;
extern jmethodID g_randCtor;
extern jmethodID g_randNextBytesMethod;

// java/security/MessageDigest
extern jclass    g_mdClass;
extern jmethodID g_mdGetInstanceMethod;
extern jmethodID g_mdDigestMethod;
extern jmethodID g_mdDigestCurrentMethodId;
extern jmethodID g_mdResetMethod;
extern jmethodID g_mdUpdateMethod;

// javax/crypto/Mac
extern jclass    g_macClass;
extern jmethodID g_macGetInstanceMethod;
extern jmethodID g_macDoFinalMethod;
extern jmethodID g_macUpdateMethod;
extern jmethodID g_macInitMethod;
extern jmethodID g_macResetMethod;

// javax/crypto/spec/SecretKeySpec
extern jclass    g_sksClass;
extern jmethodID g_sksCtor;

// javax/crypto/Cipher
extern jclass    g_cipherClass;
extern jmethodID g_cipherGetInstanceMethod;
extern jmethodID g_cipherDoFinalMethod;
extern jmethodID g_cipherUpdateMethod;
extern jmethodID g_cipherUpdateAADMethod;
extern jmethodID g_cipherInitMethod;
extern jmethodID g_getBlockSizeMethod;

// javax/crypto/spec/IvParameterSpec
extern jclass    g_ivPsClass;
extern jmethodID g_ivPsCtor;

// java/math/BigInteger
extern jclass    g_bigNumClass;
extern jmethodID g_bigNumCtor;
extern jmethodID g_toByteArrayMethod;

// javax/net/ssl/SSLParameters
extern jclass    g_sslParamsClass;
extern jmethodID g_sslParamsGetProtocolsMethod;

// javax/net/ssl/SSLContext
extern jclass    g_sslCtxClass;
extern jmethodID g_sslCtxGetDefaultMethod;
extern jmethodID g_sslCtxGetDefaultSslParamsMethod;

// javax/crypto/spec/GCMParameterSpec
extern jclass    g_GCMParameterSpecClass;
extern jmethodID g_GCMParameterSpecCtor;

// JNI helpers
#define LOG_DEBUG(fmt, ...) ((void)__android_log_print(ANDROID_LOG_DEBUG, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define LOG_INFO(fmt, ...) ((void)__android_log_print(ANDROID_LOG_INFO, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define LOG_ERROR(fmt, ...) ((void)__android_log_print(ANDROID_LOG_ERROR, "DOTNET", "%s: " fmt, __FUNCTION__, ## __VA_ARGS__))
#define JSTRING(str) ((jstring)(*env)->NewStringUTF(env, str))

void SaveTo(uint8_t* src, uint8_t** dst, size_t len);
jobject ToGRef(JNIEnv *env, jobject lref);
void ReleaseGRef(JNIEnv *env, jobject gref);
jclass GetClassGRef(JNIEnv *env, const char* name);
bool CheckJNIExceptions(JNIEnv* env);
jmethodID GetMethod(JNIEnv *env, bool isStatic, jclass klass, const char* name, const char* sig);
JNIEnv* GetJNIEnv(void);
