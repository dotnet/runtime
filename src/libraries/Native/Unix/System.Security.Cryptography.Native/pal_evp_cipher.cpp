// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_cipher.h"

#include <assert.h>
#include <memory>

#define SUCCESS 1
#define KEEP_CURRENT_DIRECTION -1

extern "C" EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc)
{
    std::unique_ptr<EVP_CIPHER_CTX> ctx(new (std::nothrow) EVP_CIPHER_CTX);
    if (ctx == nullptr)
    {
        // Allocation failed
        return nullptr;
    }

    EVP_CIPHER_CTX_init(ctx.get());
    int ret = EVP_CipherInit_ex(ctx.get(), type, nullptr, key, iv, enc);

    if (!ret)
    {
        return nullptr;
    }

    return ctx.release();
}

extern "C" void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx)
{
    if (ctx != nullptr)
    {
        EVP_CIPHER_CTX_cleanup(ctx);
        delete ctx;
    }
}

extern "C" int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx)
{
    // EVP_CipherInit_ex with all nulls preserves the algorithm, resets the IV,
    // and maintains the key.
    //
    // The only thing that you can't do is change the encryption direction,
    // that requires passing the key and IV in again.
    //
    // But since we have a different object returned for CreateEncryptor
    // and CreateDecryptor we don't need to worry about that.

    return EVP_CipherInit_ex(ctx, nullptr, nullptr, nullptr, nullptr, KEEP_CURRENT_DIRECTION);
}

extern "C" int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    return EVP_CIPHER_CTX_set_padding(x, padding);
}

extern "C" int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl)
{
    int outLength;
    int32_t ret = EVP_CipherUpdate(ctx, out, &outLength, in, inl);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

extern "C" int32_t CryptoNative_EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl)
{
    int outLength;
    int32_t ret = EVP_CipherFinal_ex(ctx, outm, &outLength);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Ecb()
{
    return EVP_aes_128_ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Cbc()
{
    return EVP_aes_128_cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Ecb()
{
    return EVP_aes_192_ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Cbc()
{
    return EVP_aes_192_cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Ecb()
{
    return EVP_aes_256_ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Cbc()
{
    return EVP_aes_256_cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Ecb()
{
    return EVP_des_ede3();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Cbc()
{
    return EVP_des_ede3_cbc();
}
