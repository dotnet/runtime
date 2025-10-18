// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_jni.h"

typedef bool (*RemoteCertificateValidationCallback)(intptr_t);

PALEXPORT void AndroidCryptoNative_RegisterRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback);

jobjectArray GetTrustManagers(JNIEnv* env, intptr_t sslStreamProxyHandle);

void StoreRemoteVerificationCallback (RemoteCertificateValidationCallback callback);
JNIEXPORT jboolean JNICALL Java_net_dot_android_crypto_DotnetProxyTrustManager_verifyRemoteCertificate(
    JNIEnv *env, jobject thisHandle, jlong sslStreamProxyHandle);
