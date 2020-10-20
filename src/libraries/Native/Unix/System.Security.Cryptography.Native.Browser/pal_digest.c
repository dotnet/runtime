// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdint.h>
#include <stdbool.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <assert.h>
#include <unistd.h>
#include <time.h>
#include <errno.h>
#include <stdio.h>

#include "pal_digest.h"
#include "pal_config.h"
#include <assert.h>

struct digest_ctx_st
{
    PAL_HashAlgorithm algorithm;
    // This 32-bit field is required for alignment,
    // but it's also handy for remembering how big the final buffer is.
    int32_t cbDigest;
    int32_t ctxId;
};

void SubtleCryptoNative_DigestFree(int32_t* pDigest)
{
    //fprintf(stderr, "SubtleCryptoNative_DigestFree %i.\n", *pDigest);
    if (pDigest != NULL)
    {
        free(pDigest);
    }
}

extern int32_t dotnet_browser_digest(uint32_t algo, int32_t digestLength);
DigestCtx* SubtleCryptoNative_DigestCreate(PAL_HashAlgorithm algorithm, int32_t* pcbDigest)
{
    //fprintf(stderr, "SubtleCryptoNative_DigestCreate %u.\n", *(pcbDigest));
    if (pcbDigest == NULL)
        return NULL;

    DigestCtx* digestCtx = (DigestCtx*)malloc(sizeof(DigestCtx));
    if (digestCtx == NULL)
        return NULL;

    digestCtx->algorithm = algorithm;
    digestCtx->ctxId = -1;

    switch (algorithm)
    {
        case PAL_MD5:
            *pcbDigest = CC_MD5_DIGEST_LENGTH;
            break;
        case PAL_SHA1:
            *pcbDigest = CC_SHA1_DIGEST_LENGTH;
            break;
        case PAL_SHA256:
            *pcbDigest = CC_SHA256_DIGEST_LENGTH;
            break;
        case PAL_SHA384:
            *pcbDigest = CC_SHA384_DIGEST_LENGTH;
            break;
        case PAL_SHA512:
            *pcbDigest = CC_SHA512_DIGEST_LENGTH;
            break;
        default:
            *pcbDigest = -1;
            free(digestCtx);
            return NULL;
    }

    int32_t ctxId = dotnet_browser_digest(algorithm, *(pcbDigest));
    //fprintf(stderr, "SubtleCryptoNative_DigestCreate::dotnet_browser_digest %i.\n", ctxId);

    digestCtx->cbDigest = *pcbDigest;
    digestCtx->ctxId = ctxId;
    return digestCtx;
}

extern int32_t dotnet_browser_digest_update(int32_t cxt_id, uint8_t* pBuf, int32_t cbBuf, int32_t digestLength);
extern int32_t dotnet_browser_digest_initialize(void);
int32_t SubtleCryptoNative_DigestUpdate(DigestCtx* ctx, uint8_t* pBuf, int32_t cbBuf)
{
    //fprintf(stderr, "SubtleCryptoNative_DigestUpdate %s.\n", "ASDF");
    if (cbBuf == 0)
        return 1;
    if (ctx == NULL || pBuf == NULL)
        return -1;

    if (ctx->ctxId == -1)
        ctx->ctxId = dotnet_browser_digest_initialize();
    return dotnet_browser_digest_update(ctx->ctxId, pBuf, cbBuf, ctx->cbDigest);
}

extern int32_t dotnet_browser_digest_final(uint32_t algo, int32_t cxt_id, uint8_t* pOutput, int32_t cbOutput, int32_t digestLength, int32_t gc_handle);
extern int32_t dotnet_browser_digest_reset(int32_t cxt_id);
int32_t SubtleCryptoNative_DigestFinal(DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput, int32_t gc_handle)
{
    if (ctx == NULL || pOutput == NULL || cbOutput < ctx->cbDigest)
        return -1;

    int32_t ret = dotnet_browser_digest_final(ctx->algorithm, ctx->ctxId, pOutput, cbOutput, ctx->cbDigest, gc_handle);
    if (ret != 1)
    {
        return ret;
    }
    int32_t resetResult = dotnet_browser_digest_reset(ctx->ctxId);
    if (resetResult)
    {
        //fprintf(stderr, "SubtleCryptoNative_DigestFinal reset %i.\n", ctx->ctxId);
        ctx->ctxId = -1;
    }
    return resetResult;

}

int32_t SubtleCryptoNative_DigestCurrent(const DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput)
{
    if (ctx == NULL || pOutput == NULL || cbOutput < ctx->cbDigest)
        return -1;

    DigestCtx dup = *ctx;
    return SubtleCryptoNative_DigestFinal(&dup, pOutput, cbOutput, -1);
}

extern int32_t dotnet_browser_digest_oneshot(uint32_t algo, uint8_t* pBuf, int32_t cbBuf, uint8_t* pOutput, int32_t cbOutput, int32_t digestLength, int32_t gc_handle);
int32_t SubtleCryptoNative_DigestOneShot(PAL_HashAlgorithm algorithm, uint8_t* pBuf, int32_t cbBuf, uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest, int32_t gc_handle)
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
            dotnet_browser_digest_oneshot(algorithm, pBuf, cbBuf, pOutput, cbOutput, *(pcbDigest), gc_handle);
            return 1;
        case PAL_SHA256:
            *pcbDigest = CC_SHA256_DIGEST_LENGTH;
            if (cbOutput < CC_SHA256_DIGEST_LENGTH)
            {
                return -1;
            }
            dotnet_browser_digest_oneshot(algorithm, pBuf, cbBuf, pOutput, cbOutput, *(pcbDigest), gc_handle);
            return 1;
        case PAL_SHA384:
            *pcbDigest = CC_SHA384_DIGEST_LENGTH;
            if (cbOutput < CC_SHA384_DIGEST_LENGTH)
            {
                return -1;
            }
            dotnet_browser_digest_oneshot(algorithm, pBuf, cbBuf, pOutput, cbOutput, *(pcbDigest), gc_handle);
            return 1;
        case PAL_SHA512:
            *pcbDigest = CC_SHA512_DIGEST_LENGTH;
            if (cbOutput < CC_SHA512_DIGEST_LENGTH)
            {
                return -1;
            }
            dotnet_browser_digest_oneshot(algorithm, pBuf, cbBuf, pOutput, cbOutput, *(pcbDigest), gc_handle);
            return 1;
        case PAL_MD5:
            *pcbDigest = CC_MD5_DIGEST_LENGTH;
            if (cbOutput < CC_MD5_DIGEST_LENGTH)
            {
                return -1;
            }
            dotnet_browser_digest_oneshot(algorithm, pBuf, cbBuf, pOutput, cbOutput, *(pcbDigest), gc_handle);
            return 1;
        default:
            return -1;
   }
}
