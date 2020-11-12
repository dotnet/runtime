// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_compiler.h"

#define FAIL 0
#define SUCCESS 1
#define EVP_MAX_MD_SIZE 64

PALEXPORT int32_t CryptoNative_EnsureOpenSslInitialized(void);

PALEXPORT int32_t CryptoNative_GetRandomBytes(uint8_t* buf, int32_t num);
PALEXPORT int32_t CryptoNative_EvpMdSize(intptr_t md);
PALEXPORT intptr_t CryptoNative_EvpMd5(void);
PALEXPORT intptr_t CryptoNative_EvpSha1(void);
PALEXPORT intptr_t CryptoNative_EvpSha256(void);
PALEXPORT intptr_t CryptoNative_EvpSha384(void);
PALEXPORT intptr_t CryptoNative_EvpSha512(void);
PALEXPORT int32_t CryptoNative_GetMaxMdSize(void);
PALEXPORT int32_t CryptoNative_EvpDigestOneShot(intptr_t type, void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize);

PALEXPORT void* CryptoNative_EvpMdCtxCreate(intptr_t type);
PALEXPORT int32_t CryptoNative_EvpDigestReset(void* ctx, intptr_t type);
PALEXPORT int32_t CryptoNative_EvpDigestUpdate(void* ctx, void* d, int32_t cnt);
PALEXPORT int32_t CryptoNative_EvpDigestFinalEx(void* ctx, uint8_t* md, uint32_t* s);
PALEXPORT int32_t CryptoNative_EvpDigestCurrent(void* ctx, uint8_t* md, uint32_t* s);
PALEXPORT void CryptoNative_EvpMdCtxDestroy(void* ctx);

PALEXPORT void* CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, intptr_t md);
PALEXPORT int32_t CryptoNative_HmacReset(void* ctx);
PALEXPORT int32_t CryptoNative_HmacUpdate(void* ctx, uint8_t* data, int32_t len);
PALEXPORT int32_t CryptoNative_HmacFinal(void* ctx, uint8_t* md, int32_t* len);
PALEXPORT int32_t CryptoNative_HmacCurrent(void* ctx, uint8_t* md, int32_t* len);
PALEXPORT void CryptoNative_HmacDestroy(void* ctx);

PALEXPORT void* CryptoNative_EvpCipherCreate2(intptr_t type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, uint8_t* iv, int32_t enc);
PALEXPORT void* CryptoNative_EvpCipherCreatePartial(intptr_t type);
PALEXPORT int32_t CryptoNative_EvpCipherSetKeyAndIV(void* ctx, uint8_t* key, uint8_t* iv, int32_t enc);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmNonceLength(void* ctx, int32_t ivLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmNonceLength(void* ctx, int32_t ivLength);
PALEXPORT void CryptoNative_EvpCipherDestroy(void* ctx);
PALEXPORT int32_t CryptoNative_EvpCipherReset(void* ctx);
PALEXPORT int32_t CryptoNative_EvpCipherCtxSetPadding(void* x, int32_t padding);
PALEXPORT int32_t CryptoNative_EvpCipherUpdate(void* ctx, uint8_t* out, int32_t* outl, uint8_t* in, int32_t inl);
PALEXPORT int32_t CryptoNative_EvpCipherFinalEx(void* ctx, uint8_t* outm, int32_t* outl);
PALEXPORT int32_t CryptoNative_EvpCipherGetGcmTag(void* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetGcmTag(void* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherGetCcmTag(void* ctx, uint8_t* tag, int32_t tagLength);
PALEXPORT int32_t CryptoNative_EvpCipherSetCcmTag(void* ctx, uint8_t* tag, int32_t tagLength);
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
