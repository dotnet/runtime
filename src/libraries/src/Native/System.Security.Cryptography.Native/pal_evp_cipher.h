// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"
#include <openssl/evp.h>

/*
Function:
GetEvpCipherCtxSize

Returns the size of EVP_CIPHER_CTX.
*/
extern "C" int32_t GetEvpCipherCtxSize();

/*
Function:
EvpCipherCtxInit

Direct shim to EVP_CIPHER_CTX_init.
*/
extern "C" void EvpCipherCtxInit(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpCipherInitEx

Direct shim to EVP_CipherInit_ex.
*/
extern "C" int32_t EvpCipherInitEx(
    EVP_CIPHER_CTX* ctx,
    const EVP_CIPHER* type,
    ENGINE* impl,
    unsigned char* key,
    unsigned char* iv,
    int32_t enc);

/*
Function:
EvpCipherCtxSetPadding

Direct shim to EVP_CIPHER_CTX_set_padding.
*/
extern "C" int32_t EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding);

/*
Function:
EvpCipherUpdate

Direct shim to EVP_CipherUpdate.
*/
extern "C" int32_t EvpCipherUpdate(
    EVP_CIPHER_CTX* ctx, 
    unsigned char* out,
    int32_t* outl,
    unsigned char* in, 
    int32_t inl);

/*
Function:
EvpCipherFinalEx

Direct shim to EVP_CipherFinal_ex.
*/
extern "C" int32_t EvpCipherFinalEx(
    EVP_CIPHER_CTX* ctx,
    unsigned char* outm,
    int32_t* outl);

/*
Function:
EvpCipherCtxCleanup

Direct shim to EVP_CIPHER_CTX_cleanup.
*/
extern "C" int32_t EvpCipherCtxCleanup(EVP_CIPHER_CTX* ctx);

/*
Function:
EvpAes128Ecb

Direct shim to EVP_aes_128_ecb.
*/
extern "C" const EVP_CIPHER* EvpAes128Ecb();

/*
Function:
EvpAes128Cbc

Direct shim to EVP_aes_128_cbc.
*/
extern "C" const EVP_CIPHER* EvpAes128Cbc();

/*
Function:
EvpAes192Ecb

Direct shim to EVP_aes_192_ecb.
*/
extern "C" const EVP_CIPHER* EvpAes192Ecb();

/*
Function:
EvpAes192Cbc

Direct shim to EVP_aes_192_cbc.
*/
extern "C" const EVP_CIPHER* EvpAes192Cbc();

/*
Function:
EvpAes256Ecb

Direct shim to EVP_aes_256_ecb.
*/
extern "C" const EVP_CIPHER* EvpAes256Ecb();

/*
Function:
EvpAes256Cbc

Direct shim to EVP_aes_256_cbc.
*/
extern "C" const EVP_CIPHER* EvpAes256Cbc();
