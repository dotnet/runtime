// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_digest.h"

#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonDigest.h>
#include <assert.h>

struct digest_ctx_st
{
    PAL_HashAlgorithm algorithm;
    // This 32-bit field is required for alignment,
    // but it's also handy for remembering how big the final buffer is.
    int32_t cbDigest;
    union {
        CC_MD5_CTX md5;
        CC_SHA1_CTX sha1;
        CC_SHA256_CTX sha256;
        CC_SHA512_CTX sha384;
        CC_SHA512_CTX sha512;
    } d;
};

void AppleCryptoNative_DigestFree(DigestCtx* pDigest)
{
    if (pDigest != NULL)
    {
        free(pDigest);
    }
}

DigestCtx* AppleCryptoNative_DigestCreate(PAL_HashAlgorithm algorithm, int32_t* pcbDigest)
{
    if (pcbDigest == NULL)
        return NULL;

    DigestCtx* digestCtx = (DigestCtx*)malloc(sizeof(DigestCtx));
    if (digestCtx == NULL)
        return NULL;

    digestCtx->algorithm = algorithm;

    switch (algorithm)
    {
        case PAL_MD5:
            *pcbDigest = CC_MD5_DIGEST_LENGTH;
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            CC_MD5_Init(&digestCtx->d.md5);
#pragma clang diagnostic pop
            break;
        case PAL_SHA1:
            *pcbDigest = CC_SHA1_DIGEST_LENGTH;
            CC_SHA1_Init(&digestCtx->d.sha1);
            break;
        case PAL_SHA256:
            *pcbDigest = CC_SHA256_DIGEST_LENGTH;
            CC_SHA256_Init(&digestCtx->d.sha256);
            break;
        case PAL_SHA384:
            *pcbDigest = CC_SHA384_DIGEST_LENGTH;
            CC_SHA384_Init(&digestCtx->d.sha384);
            break;
        case PAL_SHA512:
            *pcbDigest = CC_SHA512_DIGEST_LENGTH;
            CC_SHA512_Init(&digestCtx->d.sha512);
            break;
        default:
            *pcbDigest = -1;
            free(digestCtx);
            return NULL;
    }

    digestCtx->cbDigest = *pcbDigest;
    return digestCtx;
}

int32_t AppleCryptoNative_DigestUpdate(DigestCtx* ctx, uint8_t* pBuf, int32_t cbBuf)
{
    if (cbBuf == 0)
        return 1;
    if (ctx == NULL || pBuf == NULL)
        return -1;

    CC_LONG bufSize = (CC_LONG)cbBuf;

    switch (ctx->algorithm)
    {
        case PAL_MD5:
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            return CC_MD5_Update(&ctx->d.md5, pBuf, bufSize);
#pragma clang diagnostic pop
        case PAL_SHA1:
            return CC_SHA1_Update(&ctx->d.sha1, pBuf, bufSize);
        case PAL_SHA256:
            return CC_SHA256_Update(&ctx->d.sha256, pBuf, bufSize);
        case PAL_SHA384:
            return CC_SHA384_Update(&ctx->d.sha384, pBuf, bufSize);
        case PAL_SHA512:
            return CC_SHA512_Update(&ctx->d.sha512, pBuf, bufSize);
        default:
            return -1;
    }
}

int32_t AppleCryptoNative_DigestFinal(DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    if (ctx == NULL || pOutput == NULL || cbOutput < ctx->cbDigest)
        return -1;

    int32_t ret = 0;

    switch (ctx->algorithm)
    {
        case PAL_MD5:
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            ret = CC_MD5_Final(pOutput, &ctx->d.md5);
#pragma clang diagnostic pop
            break;
        case PAL_SHA1:
            ret = CC_SHA1_Final(pOutput, &ctx->d.sha1);
            break;
        case PAL_SHA256:
            ret = CC_SHA256_Final(pOutput, &ctx->d.sha256);
            break;
        case PAL_SHA384:
            ret = CC_SHA384_Final(pOutput, &ctx->d.sha384);
            break;
        case PAL_SHA512:
            ret = CC_SHA512_Final(pOutput, &ctx->d.sha512);
            break;
        default:
            ret = -1;
            break;
    }

    if (ret != 1)
    {
        return ret;
    }

    return AppleCryptoNative_DigestReset(ctx);
}

int32_t AppleCryptoNative_DigestCurrent(const DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    if (ctx == NULL || pOutput == NULL || cbOutput < ctx->cbDigest)
        return -1;

    DigestCtx dup = *ctx;
    return AppleCryptoNative_DigestFinal(&dup, pOutput, cbOutput);
}

int32_t AppleCryptoNative_DigestOneShot(PAL_HashAlgorithm algorithm, uint8_t* pBuf, int32_t cbBuf, uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest)
{
    if (pOutput == NULL || cbOutput <= 0 || pcbDigest == NULL)
        return -1;

    switch (algorithm)
    {
        case PAL_SHA1:
            *pcbDigest = CC_SHA1_DIGEST_LENGTH;
            if (cbOutput < CC_SHA1_DIGEST_LENGTH)
            {
                return -1;
            }
            CC_SHA1(pBuf, cbBuf, pOutput);
            return 1;
        case PAL_SHA256:
            *pcbDigest = CC_SHA256_DIGEST_LENGTH;
            if (cbOutput < CC_SHA256_DIGEST_LENGTH)
            {
                return -1;
            }
            CC_SHA256(pBuf, cbBuf, pOutput);
            return 1;
        case PAL_SHA384:
            *pcbDigest = CC_SHA384_DIGEST_LENGTH;
            if (cbOutput < CC_SHA384_DIGEST_LENGTH)
            {
                return -1;
            }
            CC_SHA384(pBuf, cbBuf, pOutput);
            return 1;
        case PAL_SHA512:
            *pcbDigest = CC_SHA512_DIGEST_LENGTH;
            if (cbOutput < CC_SHA512_DIGEST_LENGTH)
            {
                return -1;
            }
            CC_SHA512(pBuf, cbBuf, pOutput);
            return 1;
        case PAL_MD5:
            *pcbDigest = CC_MD5_DIGEST_LENGTH;
            if (cbOutput < CC_MD5_DIGEST_LENGTH)
            {
                return -1;
            }
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            CC_MD5(pBuf, cbBuf, pOutput);
#pragma clang diagnostic pop
            return 1;
        default:
            return -1;
    }
}

int32_t AppleCryptoNative_DigestReset(DigestCtx* ctx)
{
    if (ctx == NULL)
        return -1;

    switch (ctx->algorithm)
    {
        case PAL_MD5:
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
            return CC_MD5_Init(&ctx->d.md5);
#pragma clang diagnostic pop
        case PAL_SHA1:
            return CC_SHA1_Init(&ctx->d.sha1);
        case PAL_SHA256:
            return CC_SHA256_Init(&ctx->d.sha256);
        case PAL_SHA384:
            return CC_SHA384_Init(&ctx->d.sha384);
        case PAL_SHA512:
            return CC_SHA512_Init(&ctx->d.sha512);
        default:
            assert(false);
            return -2;
    }

    return 1;
}
