// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

#define EVP_MAX_MD_SIZE 64

PALEXPORT int32_t CryptoNative_EvpMdSize(intptr_t md);
PALEXPORT int32_t CryptoNative_GetMaxMdSize(void);
PALEXPORT intptr_t CryptoNative_EvpMd5(void);
PALEXPORT intptr_t CryptoNative_EvpSha1(void);
PALEXPORT intptr_t CryptoNative_EvpSha256(void);
PALEXPORT intptr_t CryptoNative_EvpSha384(void);
PALEXPORT intptr_t CryptoNative_EvpSha512(void);
PALEXPORT int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize);
PALEXPORT jobject CryptoNative_EvpMdCtxCreate(intptr_t type);
PALEXPORT int32_t CryptoNative_EvpDigestReset(jobject ctx, intptr_t type);
PALEXPORT int32_t CryptoNative_EvpDigestUpdate(jobject ctx, void* d, int32_t cnt);
PALEXPORT int32_t CryptoNative_EvpDigestFinalEx(jobject ctx, uint8_t* md, uint32_t* s);
PALEXPORT int32_t CryptoNative_EvpDigestCurrent(jobject ctx, uint8_t* md, uint32_t* s);
PALEXPORT void CryptoNative_EvpMdCtxDestroy(jobject ctx);
