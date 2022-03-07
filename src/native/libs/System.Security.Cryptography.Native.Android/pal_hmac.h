// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

PALEXPORT jobject CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, intptr_t md);
PALEXPORT int32_t CryptoNative_HmacReset(jobject ctx);
PALEXPORT int32_t CryptoNative_HmacUpdate(jobject ctx, uint8_t* data, int32_t len);
PALEXPORT int32_t CryptoNative_HmacFinal(jobject ctx, uint8_t* md, int32_t* len);
PALEXPORT int32_t CryptoNative_HmacCurrent(jobject ctx, uint8_t* md, int32_t* len);
PALEXPORT void CryptoNative_HmacDestroy(jobject ctx);
PALEXPORT int32_t CryptoNative_HmacOneShot(intptr_t type,
                                           uint8_t* key,
                                           int32_t keyLen,
                                           uint8_t* source,
                                           int32_t sourceLen,
                                           uint8_t* md,
                                           int32_t* mdSize);
