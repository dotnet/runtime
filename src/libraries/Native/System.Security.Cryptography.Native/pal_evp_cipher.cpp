// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_cipher.h"

#include <assert.h>
#include <memory>

#define SUCCESS 1
#define KEEP_CURRENT_DIRECTION -1

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EVP_CIPHER_CTX*
EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc)
{
    return CryptoNative_EvpCipherCreate(type, key, iv, enc);
}

extern "C" EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc)
{
    std::unique_ptr<EVP_CIPHER_CTX> ctx(new (std::nothrow) EVP_CIPHER_CTX);
    if (ctx == nullptr)
    {
        assert(false && "Allocation failed.");
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void EvpCipherDestroy(EVP_CIPHER_CTX* ctx)
{
    return CryptoNative_EvpCipherDestroy(ctx);
}

extern "C" void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx)
{
    if (ctx != nullptr)
    {
        EVP_CIPHER_CTX_cleanup(ctx);
        delete ctx;
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpCipherReset(EVP_CIPHER_CTX* ctx)
{
    return CryptoNative_EvpCipherReset(ctx);
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    return CryptoNative_EvpCipherCtxSetPadding(x, padding);
}

extern "C" int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    return EVP_CIPHER_CTX_set_padding(x, padding);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t
EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl)
{
    return CryptoNative_EvpCipherUpdate(ctx, out, outl, in, inl);
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl)
{
    return CryptoNative_EvpCipherFinalEx(ctx, outm, outl);
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes128Ecb()
{
    return CryptoNative_EvpAes128Ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Ecb()
{
    return EVP_aes_128_ecb();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes128Cbc()
{
    return CryptoNative_EvpAes128Cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes128Cbc()
{
    return EVP_aes_128_cbc();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes192Ecb()
{
    return CryptoNative_EvpAes192Ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Ecb()
{
    return EVP_aes_192_ecb();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes192Cbc()
{
    return CryptoNative_EvpAes192Cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes192Cbc()
{
    return EVP_aes_192_cbc();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes256Ecb()
{
    return CryptoNative_EvpAes256Ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Ecb()
{
    return EVP_aes_256_ecb();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpAes256Cbc()
{
    return CryptoNative_EvpAes256Cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpAes256Cbc()
{
    return EVP_aes_256_cbc();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpDes3Ecb()
{
    return CryptoNative_EvpDes3Ecb();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Ecb()
{
    return EVP_des_ede3();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_CIPHER* EvpDes3Cbc()
{
    return CryptoNative_EvpDes3Cbc();
}

extern "C" const EVP_CIPHER* CryptoNative_EvpDes3Cbc()
{
    return EVP_des_ede3_cbc();
}
