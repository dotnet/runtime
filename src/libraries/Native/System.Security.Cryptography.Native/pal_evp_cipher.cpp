// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_cipher.h"

#define SUCCESS 1

extern "C" int32_t GetEvpCipherCtxSize()
{
    return sizeof(EVP_CIPHER_CTX);
}

extern "C" void EvpCipherCtxInit(EVP_CIPHER_CTX* ctx)
{
    EVP_CIPHER_CTX_init(ctx);
}

extern "C" int32_t EvpCipherInitEx(
    EVP_CIPHER_CTX* ctx,
    const EVP_CIPHER* type,
    ENGINE* impl,
    unsigned char* key,
    unsigned char* iv,
    int32_t enc)
{
    return EVP_CipherInit_ex(ctx, type, impl, key, iv, enc);
}

extern "C" int32_t EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    return EVP_CIPHER_CTX_set_padding(x, padding);
}

extern "C" int32_t EvpCipherUpdate(
    EVP_CIPHER_CTX* ctx,
    unsigned char* out,
    int32_t* outl,
    unsigned char* in,
    int32_t inl)
{
    int outLength;
    int32_t ret = EVP_CipherUpdate(ctx, out, &outLength, in, inl);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

extern "C" int32_t EvpCipherFinalEx(
    EVP_CIPHER_CTX* ctx,
    unsigned char* outm,
    int32_t* outl)
{
    int outLength;
    int32_t ret = EVP_CipherFinal_ex(ctx, outm, &outLength);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

extern "C" int32_t EvpCipherCtxCleanup(EVP_CIPHER_CTX* ctx)
{
    return EVP_CIPHER_CTX_cleanup(ctx);
}

extern "C" const EVP_CIPHER* EvpAes128Ecb()
{
    return EVP_aes_128_ecb();
}

extern "C" const EVP_CIPHER* EvpAes128Cbc()
{
    return EVP_aes_128_cbc();
}

extern "C" const EVP_CIPHER* EvpAes192Ecb()
{
    return EVP_aes_192_ecb();
}

extern "C" const EVP_CIPHER* EvpAes192Cbc()
{
    return EVP_aes_192_cbc();
}

extern "C" const EVP_CIPHER* EvpAes256Ecb()
{
    return EVP_aes_256_ecb();
}

extern "C" const EVP_CIPHER* EvpAes256Cbc()
{
    return EVP_aes_256_cbc();
}
