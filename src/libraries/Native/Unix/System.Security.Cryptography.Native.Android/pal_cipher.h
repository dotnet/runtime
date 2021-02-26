// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

#define TAG_MAX_LENGTH 16

#define CIPHER_ENCRYPT_MODE 1
#define CIPHER_DECRYPT_MODE 2

typedef struct CipherInfo CipherInfo;

typedef struct CipherCtx
{
    jobject cipher;
    CipherInfo* type;
    int32_t ivLength;
    int32_t tagLength;
    int32_t encMode;
    uint8_t* key;
    uint8_t* iv;
} CipherCtx;

PALEXPORT CipherCtx* AndroidCryptoNative_CipherCreate(CipherInfo* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc);
PALEXPORT CipherCtx* AndroidCryptoNative_CipherCreatePartial(CipherInfo* type);
PALEXPORT int32_t AndroidCryptoNative_CipherSetTagLength(CipherCtx* ctx, int32_t tagLength);
PALEXPORT int32_t AndroidCryptoNative_CipherSetKeyAndIV(CipherCtx* ctx, uint8_t* key, uint8_t* iv, int32_t enc);
PALEXPORT int32_t AndroidCryptoNative_CipherSetNonceLength(CipherCtx* ctx, int32_t ivLength);
PALEXPORT void AndroidCryptoNative_CipherDestroy(CipherCtx* ctx);
PALEXPORT int32_t AndroidCryptoNative_CipherReset(CipherCtx* ctx);
PALEXPORT int32_t AndroidCryptoNative_CipherCtxSetPadding(CipherCtx* ctx, int32_t padding);
PALEXPORT int32_t AndroidCryptoNative_CipherUpdateAAD(CipherCtx* ctx, uint8_t* in, int32_t inl);
PALEXPORT int32_t AndroidCryptoNative_CipherUpdate(CipherCtx* ctx, uint8_t* out, int32_t* outl, uint8_t* in, int32_t inl);
PALEXPORT int32_t AndroidCryptoNative_CipherFinalEx(CipherCtx* ctx, uint8_t* outm, int32_t* outl);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Ecb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Cbc(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Cfb8(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Cfb128(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Gcm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes128Ccm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Ecb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Cbc(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Cfb8(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Cfb128(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Gcm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes192Ccm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Ecb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Cbc(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Cfb8(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Cfb128(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Gcm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Aes256Ccm(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Des3Ecb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Des3Cbc(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Des3Cfb8(void);
PALEXPORT CipherInfo* AndroidCryptoNative_Des3Cfb64(void);
PALEXPORT CipherInfo* AndroidCryptoNative_DesEcb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_DesCfb8(void);
PALEXPORT CipherInfo* AndroidCryptoNative_DesCbc(void);
PALEXPORT CipherInfo* AndroidCryptoNative_RC2Ecb(void);
PALEXPORT CipherInfo* AndroidCryptoNative_RC2Cbc(void);
