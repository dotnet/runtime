// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp.h"

#include <assert.h>

#define SUCCESS 1

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EVP_MD_CTX* EvpMdCtxCreate(const EVP_MD* type)
{
    return CryptoNative_EvpMdCtxCreate(type);
}

extern "C" EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type)
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void EvpMdCtxDestroy(EVP_MD_CTX* ctx)
{
    return CryptoNative_EvpMdCtxDestroy(ctx);
}

extern "C" void CryptoNative_EvpMdCtxDestroy(EVP_MD_CTX* ctx)
{
    if (ctx != nullptr)
    {
        EVP_MD_CTX_destroy(ctx);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type)
{
    return CryptoNative_EvpDigestReset(ctx, type);
}

extern "C" int32_t CryptoNative_EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type)
{
    return EVP_DigestInit_ex(ctx, type, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt)
{
    return CryptoNative_EvpDigestUpdate(ctx, d, cnt);
}

extern "C" int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, size_t cnt)
{
    return EVP_DigestUpdate(ctx, d, cnt);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    return CryptoNative_EvpDigestFinalEx(ctx, md, s);
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

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpMdSize(const EVP_MD* md)
{
    return CryptoNative_EvpMdSize(md);
}

extern "C" int32_t CryptoNative_EvpMdSize(const EVP_MD* md)
{
    return EVP_MD_size(md);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_MD* EvpMd5()
{
    return CryptoNative_EvpMd5();
}

extern "C" const EVP_MD* CryptoNative_EvpMd5()
{
    return EVP_md5();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_MD* EvpSha1()
{
    return CryptoNative_EvpSha1();
}

extern "C" const EVP_MD* CryptoNative_EvpSha1()
{
    return EVP_sha1();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_MD* EvpSha256()
{
    return CryptoNative_EvpSha256();
}

extern "C" const EVP_MD* CryptoNative_EvpSha256()
{
    return EVP_sha256();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_MD* EvpSha384()
{
    return CryptoNative_EvpSha384();
}

extern "C" const EVP_MD* CryptoNative_EvpSha384()
{
    return EVP_sha384();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" const EVP_MD* EvpSha512()
{
    return CryptoNative_EvpSha512();
}

extern "C" const EVP_MD* CryptoNative_EvpSha512()
{
    return EVP_sha512();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t GetMaxMdSize()
{
    return CryptoNative_GetMaxMdSize();
}

extern "C" int32_t CryptoNative_GetMaxMdSize()
{
    return EVP_MAX_MD_SIZE;
}
