// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT int32_t CryptoNative_EnsureOpenSslInitialized(void);
PALEXPORT int32_t CryptoNative_GetRandomBytes(uint8_t* buf, int32_t num);

jobject AndroidCryptoNative_CreateKeyPair(JNIEnv* env, jobject publicKey, jobject privateKey);
