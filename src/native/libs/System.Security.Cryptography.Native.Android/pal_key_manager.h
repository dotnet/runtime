// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_trust_manager.h"
#include <stdint.h>

typedef intptr_t (*LocalCertificateSelectionCallback)(
    intptr_t sslStreamProxyHandle,
    int32_t acceptableIssuerCount,
    uint16_t** acceptableIssuers);

PALEXPORT void AndroidCryptoNative_RegisterSslStreamCallbacks(
    RemoteCertificateValidationCallback remoteCertificateValidationCallback,
    LocalCertificateSelectionCallback localCertificateSelectionCallback);

PALEXPORT jobjectArray AndroidCryptoNative_SSLStreamCreateKeyManagersForSelection(
    intptr_t sslStreamProxyHandle);

JNIEXPORT jobjectArray JNICALL Java_net_dot_android_crypto_DotnetX509KeyManager_selectClientCertificate(
    JNIEnv* env,
    jclass thisClass,
    jlong sslStreamProxyHandle,
    jobjectArray acceptableIssuers);
