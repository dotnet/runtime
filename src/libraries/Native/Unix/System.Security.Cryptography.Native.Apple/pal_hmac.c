// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_hmac.h"

struct hmac_ctx_st
{
    CCHmacAlgorithm appleAlgId;
    CCHmacContext hmac;
};

void AppleCryptoNative_HmacFree(HmacCtx* pHmac)
{
    if (pHmac != NULL)
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

HmacCtx* AppleCryptoNative_HmacCreate(PAL_HashAlgorithm algorithm, int32_t* pcbHmac)
{
    if (pcbHmac == NULL)
        return NULL;

    CCHmacAlgorithm appleAlgId = PalAlgorithmToAppleAlgorithm(algorithm);

    if (appleAlgId == UINT_MAX)
    {
        *pcbHmac = -1;
        return NULL;
    }

    HmacCtx* hmacCtx = (HmacCtx*)malloc(sizeof(HmacCtx));
    if (hmacCtx == NULL)
        return hmacCtx;

    hmacCtx->appleAlgId = appleAlgId;
    *pcbHmac = GetHmacOutputSize(algorithm);
    return hmacCtx;
}

int32_t AppleCryptoNative_HmacInit(HmacCtx* ctx, uint8_t* pbKey, int32_t cbKey)
{
    if (ctx == NULL || cbKey < 0)
        return 0;
    if (cbKey != 0 && pbKey == NULL)
        return 0;

    // No return value
    CCHmacInit(&ctx->hmac, ctx->appleAlgId, pbKey, (size_t)cbKey);
    return 1;
}

int32_t AppleCryptoNative_HmacUpdate(HmacCtx* ctx, uint8_t* pbData, int32_t cbData)
{
    if (cbData == 0)
        return 1;
    if (ctx == NULL || pbData == NULL)
        return 0;

    // No return value
    CCHmacUpdate(&ctx->hmac, pbData, (size_t)cbData);
    return 1;
}

int32_t AppleCryptoNative_HmacFinal(HmacCtx* ctx, uint8_t* pbOutput)
{
    if (ctx == NULL || pbOutput == NULL)
        return 0;

    // No return value
    CCHmacFinal(&ctx->hmac, pbOutput);
    return 1;
}

int32_t AppleCryptoNative_HmacCurrent(const HmacCtx* ctx, uint8_t* pbOutput)
{
    if (ctx == NULL || pbOutput == NULL)
        return 0;

    HmacCtx dup = *ctx;
    return AppleCryptoNative_HmacFinal(&dup, pbOutput);
}

int32_t AppleCryptoNative_HmacOneShot(PAL_HashAlgorithm algorithm,
                                      const uint8_t* pKey,
                                      int32_t cbKey,
                                      const uint8_t* pBuf,
                                      int32_t cbBuf,
                                      uint8_t* pOutput,
                                      int32_t cbOutput,
                                      int32_t* pcbDigest)
{
    if (pOutput == NULL || cbOutput <= 0 || pcbDigest == NULL)
        return -1;

    CCHmacAlgorithm ccAlgorithm = PalAlgorithmToAppleAlgorithm(algorithm);
    *pcbDigest = GetHmacOutputSize(algorithm);

    if (ccAlgorithm == UINT_MAX)
        return -1;

    if (cbOutput < *pcbDigest)
        return -1;

    CCHmac(ccAlgorithm, pKey, cbKey, pBuf, cbBuf, pOutput);
    return 1;
}
