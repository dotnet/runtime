// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_hmac.h"

#include <assert.h>
#include <memory>
#include <openssl/hmac.h>

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" HMAC_CTX* HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md)
{
    return CryptoNative_HmacCreate(key, keyLen, md);
}

extern "C" HMAC_CTX* CryptoNative_HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md)
{
    assert(key != nullptr || keyLen == 0);
    assert(keyLen >= 0);
    assert(md != nullptr);

    std::unique_ptr<HMAC_CTX> ctx(new (std::nothrow) HMAC_CTX);
    if (ctx == nullptr)
    {
        assert(false && "Allocation failed.");
        return nullptr;
    }

    // NOTE: We can't pass nullptr as empty key since HMAC_Init_ex will interpret
    // that as request to reuse the "existing" key.
    uint8_t _;
    if (keyLen == 0)
        key = &_;

    HMAC_CTX_init(ctx.get());
    int ret = HMAC_Init_ex(ctx.get(), key, keyLen, md, nullptr);

    if (!ret)
    {
        return nullptr;
    }

    return ctx.release();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void HmacDestroy(HMAC_CTX* ctx)
{
    return CryptoNative_HmacDestroy(ctx);
}

extern "C" void CryptoNative_HmacDestroy(HMAC_CTX* ctx)
{
    if (ctx != nullptr)
    {
        HMAC_CTX_cleanup(ctx);
        delete ctx;
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t HmacReset(HMAC_CTX* ctx)
{
    return CryptoNative_HmacReset(ctx);
}

extern "C" int32_t CryptoNative_HmacReset(HMAC_CTX* ctx)
{
    assert(ctx != nullptr);

    return HMAC_Init_ex(ctx, nullptr, 0, nullptr, nullptr);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    return CryptoNative_HmacUpdate(ctx, data, len);
}

extern "C" int32_t CryptoNative_HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    assert(ctx != nullptr);
    assert(data != nullptr || len == 0);
    assert(len >= 0);

    if (len < 0)
    {
        return 0;
    }

    return HMAC_Update(ctx, data, UnsignedCast(len));
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    return CryptoNative_HmacFinal(ctx, md, len);
}

extern "C" int32_t CryptoNative_HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    assert(ctx != nullptr);
    assert(len != nullptr);
    assert(md != nullptr || *len == 0);
    assert(*len >= 0);

    if (len == nullptr || *len < 0)
    {
        return 0;
    }

    unsigned int unsignedLen = UnsignedCast(*len);
    int ret = HMAC_Final(ctx, md, &unsignedLen);
    *len = SignedCast(unsignedLen);
    return ret;
}
