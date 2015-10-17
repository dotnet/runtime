// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_hmac.h"

#include <assert.h>
#include <memory>
#include <openssl/hmac.h>

// Return values of certain HMAC functions vary per platform/OpenSSL
// version. On some platforms, void is returned and success must be
// assumed as there is no provision for failure. On others, int is
// returned with 1 signifying success and 0 signifying failure.
//
// We call these functions indirectly via overloaded HmacCall helper
// that will synthesize a successful result of 1 on platforms where
// the functions return void and otherwise just forward along the
// actual return value.

template <typename F, typename... Args>
inline static auto HmacCall(F func, Args... args) -> ReplaceVoidResultOf<F(Args...), int>
{
    func(args...);
    return 1;
}

template <typename F, typename... Args>
inline static auto HmacCall(F func, Args... args) -> NonVoidResultOf<F(Args...)>
{
    return func(args...);
}

extern "C" HMAC_CTX* HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md)
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
    int ret = HmacCall(HMAC_Init_ex, ctx.get(), key, keyLen, md, nullptr);

    if (!ret)
    {
        return nullptr;
    }

    return ctx.release();
}

extern "C" void HmacDestroy(HMAC_CTX* ctx)
{
    if (ctx != nullptr)
    {
        HMAC_CTX_cleanup(ctx);
        delete ctx;
    }
}

extern "C" int32_t HmacReset(HMAC_CTX* ctx)
{
    assert(ctx != nullptr);

    return HmacCall(HMAC_Init_ex, ctx, nullptr, 0, nullptr, nullptr);
}

extern "C" int32_t HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    assert(ctx != nullptr);
    assert(data != nullptr || len == 0);
    assert(len >= 0);

    if (len < 0)
    {
        return 0;
    }

    return HmacCall(HMAC_Update, ctx, data, UnsignedCast(len));
}

extern "C" int32_t HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len)
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
    int ret = HmacCall(HMAC_Final, ctx, md, &unsignedLen);
    *len = SignedCast(unsignedLen);
    return ret;
}
