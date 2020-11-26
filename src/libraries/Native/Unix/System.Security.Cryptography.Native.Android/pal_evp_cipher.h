// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_evp.h"

PALEXPORT jobject CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc);
PALEXPORT jobject CryptoNative_EvpCipherCreatePartial(intptr_t type);
PALEXPORT int32_t CryptoNative_EvpCipherSetKeyAndIV(jobject ctx, uint8_t* key, uint8_t* iv, int32_t enc);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmNonceLength(jobject ctx, int32_t ivLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmNonceLength(jobject ctx, int32_t ivLength);
PALEXPORT void CryptoNative_EvpCipherDestroy(jobject ctx);
PALEXPORT int32_t CryptoNative_EvpCipherReset(jobject ctx);
PALEXPORT int32_t CryptoNative_EvpCipherCtxSetPadding(jobject ctx, int32_t padding);
PALEXPORT int32_t CryptoNative_EvpCipherUpdate(jobject ctx, uint8_t* out, int32_t* outl, uint8_t* in, int32_t inl);
PALEXPORT int32_t CryptoNative_EvpCipherFinalEx(jobject ctx, uint8_t* outm, int32_t* outl);
PALEXPORT int32_t CryptoNative_EvpCipherGetGcmTag(jobject ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmTag(jobject ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherGetCcmTag(jobject ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmTag(jobject ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT intptr_t CryptoNative_EvpAes128Ecb(void);
PALEXPORT intptr_t CryptoNative_EvpAes128Cbc(void);
PALEXPORT intptr_t CryptoNative_EvpAes128Cfb8(void);
PALEXPORT intptr_t CryptoNative_EvpAes128Cfb128(void);
PALEXPORT intptr_t CryptoNative_EvpAes128Gcm(void);
PALEXPORT intptr_t CryptoNative_EvpAes128Ccm(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Ecb(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Cbc(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Cfb8(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Cfb128(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Gcm(void);
PALEXPORT intptr_t CryptoNative_EvpAes192Ccm(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Ecb(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Cbc(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Cfb8(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Cfb128(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Gcm(void);
PALEXPORT intptr_t CryptoNative_EvpAes256Ccm(void);
PALEXPORT intptr_t CryptoNative_EvpDes3Ecb(void);
PALEXPORT intptr_t CryptoNative_EvpDes3Cbc(void);
PALEXPORT intptr_t CryptoNative_EvpDes3Cfb8(void);
PALEXPORT intptr_t CryptoNative_EvpDes3Cfb64(void);
PALEXPORT intptr_t CryptoNative_EvpDesEcb(void);
PALEXPORT intptr_t CryptoNative_EvpDesCfb8(void);
PALEXPORT intptr_t CryptoNative_EvpDesCbc(void);
PALEXPORT intptr_t CryptoNative_EvpRC2Ecb(void);
PALEXPORT intptr_t CryptoNative_EvpRC2Cbc(void);
