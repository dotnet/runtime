// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"
#include <stdint.h>

typedef bool (*RemoteCertificateValidationCallback)(intptr_t, jstring);

PALEXPORT void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback);

PALEXPORT int32_t AndroidCryptoNative_GetPlatformValidationError(jstring platformValidationError, const uint16_t** out, int32_t* outLen) ARGS_NON_NULL(2, 3);
PALEXPORT void AndroidCryptoNative_ReleasePlatformValidationError(jstring platformValidationError, const uint16_t* chars);

jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle, const char* targetHost);

JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv *env, jobject thisHandle, jlong sslStreamProxyHandle, jstring platformValidationError);
