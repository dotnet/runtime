// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_hmac.h"

struct hmac_ctx_st
{
    CCHmacAlgorithm appleAlgId;
    CCHmacContext hmac;
};

extern "C" void AppleCryptoNative_HmacFree(HmacCtx* pHmac)
{
    if (pHmac != nullptr)
    {
        free(pHmac);
    }
}

static CCHmacAlgorithm PalAlgorithmToAppleAlgorithm(PAL_HashAlgorithm algorithm)
{
    switch (algorithm)
    {
        case PAL_MD5:
            return kCCHmacAlgMD5;
        case PAL_SHA1:
            return kCCHmacAlgSHA1;
        case PAL_SHA256:
            return kCCHmacAlgSHA256;
        case PAL_SHA384:
            return kCCHmacAlgSHA384;
        case PAL_SHA512:
            return kCCHmacAlgSHA512;
        default:
            // 0 is a defined value (SHA1) so "unknown" has to be something else
            return UINT_MAX;
    }
}

static int32_t GetHmacOutputSize(PAL_HashAlgorithm algorithm)
{
    switch (algorithm)
    {
        case PAL_MD5:
            return CC_MD5_DIGEST_LENGTH;
        case PAL_SHA1:
            return CC_SHA1_DIGEST_LENGTH;
        case PAL_SHA256:
            return CC_SHA256_DIGEST_LENGTH;
        case PAL_SHA384:
            return CC_SHA384_DIGEST_LENGTH;
        case PAL_SHA512:
            return CC_SHA512_DIGEST_LENGTH;
        default:
            return -1;
    }
}

extern "C" HmacCtx* AppleCryptoNative_HmacCreate(PAL_HashAlgorithm algorithm, int32_t* pcbHmac)
{
    if (pcbHmac == nullptr)
        return nullptr;

    CCHmacAlgorithm appleAlgId = PalAlgorithmToAppleAlgorithm(algorithm);

    if (appleAlgId == UINT_MAX)
    {
        *pcbHmac = -1;
        return nullptr;
    }

    HmacCtx* hmacCtx = reinterpret_cast<HmacCtx*>(malloc(sizeof(HmacCtx)));
    if (hmacCtx == nullptr)
        return hmacCtx;

    hmacCtx->appleAlgId = appleAlgId;
    *pcbHmac = GetHmacOutputSize(algorithm);
    return hmacCtx;
}

extern "C" int32_t AppleCryptoNative_HmacInit(HmacCtx* ctx, uint8_t* pbKey, int32_t cbKey)
{
    if (ctx == nullptr || cbKey < 0)
        return 0;
    if (cbKey != 0 && pbKey == nullptr)
        return 0;

    // No return value
    CCHmacInit(&ctx->hmac, ctx->appleAlgId, pbKey, static_cast<size_t>(cbKey));
    return 1;
}

extern "C" int32_t AppleCryptoNative_HmacUpdate(HmacCtx* ctx, uint8_t* pbData, int32_t cbData)
{
    if (cbData == 0)
        return 1;
    if (ctx == nullptr || pbData == nullptr)
        return 0;

    // No return value
    CCHmacUpdate(&ctx->hmac, pbData, static_cast<size_t>(cbData));
    return 1;
}

extern "C" int32_t AppleCryptoNative_HmacFinal(HmacCtx* ctx, uint8_t* pbOutput)
{
    if (ctx == nullptr || pbOutput == nullptr)
        return 0;

    // No return value
    CCHmacFinal(&ctx->hmac, pbOutput);
    return 1;
}
