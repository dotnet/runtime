// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp.h"

#include <assert.h>

#define SUCCESS 1

extern "C" EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type)
{
    EVP_MD_CTX* ctx = EVP_MD_CTX_create();
    if (ctx == nullptr)
    {
        // Allocation failed
        return nullptr;
    }

    int ret = EVP_DigestInit_ex(ctx, type, nullptr);
    if (!ret)
    {
        EVP_MD_CTX_destroy(ctx);
        return nullptr;
    }

    return ctx;
}

extern "C" void CryptoNative_EvpMdCtxDestroy(EVP_MD_CTX* ctx)
{
    if (ctx != nullptr)
    {
        EVP_MD_CTX_destroy(ctx);
    }
}

extern "C" int32_t CryptoNative_EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type)
{
    return EVP_DigestInit_ex(ctx, type, nullptr);
}

extern "C" int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt)
{
    return EVP_DigestUpdate(ctx, d, cnt);
}

extern "C" int32_t CryptoNative_EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    unsigned int size;
    int32_t ret = EVP_DigestFinal_ex(ctx, md, &size);
    if (ret == SUCCESS)
    {
        *s = size;
    }

    return ret;
}

extern "C" int32_t CryptoNative_EvpMdSize(const EVP_MD* md)
{
    return EVP_MD_size(md);
}

extern "C" const EVP_MD* CryptoNative_EvpMd5()
{
    return EVP_md5();
}

extern "C" const EVP_MD* CryptoNative_EvpSha1()
{
    return EVP_sha1();
}

extern "C" const EVP_MD* CryptoNative_EvpSha256()
{
    return EVP_sha256();
}

extern "C" const EVP_MD* CryptoNative_EvpSha384()
{
    return EVP_sha384();
}

extern "C" const EVP_MD* CryptoNative_EvpSha512()
{
    return EVP_sha512();
}

extern "C" int32_t CryptoNative_GetMaxMdSize()
{
    return EVP_MAX_MD_SIZE;
}
