// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp.h"

#include <assert.h>

#define SUCCESS 1

extern "C" EVP_MD_CTX* EvpMdCtxCreate(const EVP_MD* type)
{
    EVP_MD_CTX* ctx = EVP_MD_CTX_create();
    if (ctx == nullptr)
    {
        assert(false && "Allocation failed.");
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

extern "C" void EvpMdCtxDestroy(EVP_MD_CTX* ctx)
{
    if (ctx != nullptr)
    {
        EVP_MD_CTX_destroy(ctx);
    }
}

extern "C" int32_t EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type)
{
    return EVP_DigestInit_ex(ctx, type, nullptr);
}

extern "C" int32_t EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt)
{
    return EVP_DigestUpdate(ctx, d, cnt);
}

extern "C" int32_t EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    unsigned int size;
    int32_t ret = EVP_DigestFinal_ex(ctx, md, &size);
    if (ret == SUCCESS)
    {
        *s = size;
    }

    return ret;
}

extern "C" int32_t EvpMdSize(const EVP_MD* md)
{
    return EVP_MD_size(md);
}

extern "C" const EVP_MD* EvpMd5()
{
    return EVP_md5();
}

extern "C" const EVP_MD* EvpSha1()
{
    return EVP_sha1();
}

extern "C" const EVP_MD* EvpSha256()
{
    return EVP_sha256();
}

extern "C" const EVP_MD* EvpSha384()
{
    return EVP_sha384();
}

extern "C" const EVP_MD* EvpSha512()
{
    return EVP_sha512();
}

extern "C" int32_t GetMaxMdSize()
{
    return EVP_MAX_MD_SIZE;
}
