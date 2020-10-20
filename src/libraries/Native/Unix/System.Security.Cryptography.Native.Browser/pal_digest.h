// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#define CC_MD5_DIGEST_LENGTH	16			/* digest length in bytes */
#define CC_SHA1_DIGEST_LENGTH	20			/* digest length in bytes */
#define CC_SHA256_DIGEST_LENGTH		32			/* digest length in bytes */
#define CC_SHA384_DIGEST_LENGTH		48			/* digest length in bytes */
#define CC_SHA512_DIGEST_LENGTH		64			/* digest length in bytes */

enum
{
    PAL_Unknown = 0,
    PAL_MD5,
    PAL_SHA1,
    PAL_SHA256,
    PAL_SHA384,
    PAL_SHA512,
};
typedef uint32_t PAL_HashAlgorithm;

typedef struct digest_ctx_st DigestCtx;

/*
Free the resources held by a int32_t
*/
PALEXPORT void SubtleCryptoNative_DigestFree(int32_t* pDigest);

/*
Unregister the resources held by a DigestCtx* ctx
*/
PALEXPORT void SubtleCryptoNative_DigestUnregister(DigestCtx* ctx);

/*
Create a digest handle for the specified algorithm.

Returns NULL when the algorithm is unknown, or pcbDigest is NULL; otherwise returns a pointer
to a digest context suitable for calling DigestUpdate and DigestFinal on and sets pcbDigest to
the size of the digest output.
*/
PALEXPORT DigestCtx* SubtleCryptoNative_DigestCreate(PAL_HashAlgorithm algorithm, int32_t* pcbDigest);

/*
Apply cbBuf bytes of data from pBuf to the ongoing digest represented in ctx.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t SubtleCryptoNative_DigestUpdate(DigestCtx* ctx, uint8_t* pBuf, int32_t cbBuf);

/*
Complete the digest in ctx, copying the results to pOutput, and reset ctx for a new digest.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t SubtleCryptoNative_DigestFinal(DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput, int32_t gc_handle);

/*
Get the digest of the data already loaded into ctx, without resetting ctx.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t SubtleCryptoNative_DigestCurrent(const DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput);

/*
Combines DigestCreate, DigestUpdate, and DigestFinal in to a single operation.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t SubtleCryptoNative_DigestOneShot(PAL_HashAlgorithm algorithm, uint8_t* pBuf, int32_t cbBuf, uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest, int32_t gc_handle);
