// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"
#include "pal_evp.h"


#define TAG_MAX_LENGTH 16

#define CIPHER_ENCRYPT_MODE 1
#define CIPHER_DECRYPT_MODE 2

typedef struct CipherCtx
{
    jobject cipher;
    intptr_t type;
    int32_t ivLength;
    int32_t encMode;
    uint8_t* key;
    uint8_t* iv;
    uint8_t* tag[TAG_MAX_LENGTH];
} CipherCtx;

PALEXPORT CipherCtx* CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc);
PALEXPORT CipherCtx* CryptoNative_EvpCipherCreatePartial(intptr_t type);
PALEXPORT int32_t CryptoNative_EvpCipherSetKeyAndIV(CipherCtx* ctx, uint8_t* key, uint8_t* iv, int32_t enc);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmNonceLength(CipherCtx* ctx, int32_t ivLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmNonceLength(CipherCtx* ctx, int32_t ivLength);
PALEXPORT void CryptoNative_EvpCipherDestroy(CipherCtx* ctx);
PALEXPORT int32_t CryptoNative_EvpCipherReset(CipherCtx* ctx);
PALEXPORT int32_t CryptoNative_EvpCipherCtxSetPadding(CipherCtx* ctx, int32_t padding);
PALEXPORT int32_t CryptoNative_EvpCipherUpdate(CipherCtx* ctx, uint8_t* out, int32_t* outl, uint8_t* in, int32_t inl);
PALEXPORT int32_t CryptoNative_EvpCipherFinalEx(CipherCtx* ctx, uint8_t* outm, int32_t* outl);
PALEXPORT int32_t CryptoNative_EvpCipherGetGcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherGetCcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmTag(CipherCtx* ctx, uint8_t* tag, int32_t tagLength);
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
