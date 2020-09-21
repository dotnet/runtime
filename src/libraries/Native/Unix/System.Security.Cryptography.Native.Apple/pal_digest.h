// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonHMAC.h>

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
Free the resources held by a DigestCtx
*/
PALEXPORT void AppleCryptoNative_DigestFree(DigestCtx* pDigest);

/*
Create a digest handle for the specified algorithm.

Returns NULL when the algorithm is unknown, or pcbDigest is NULL; otherwise returns a pointer
to a digest context suitable for calling DigestUpdate and DigestFinal on and sets pcbDigest to
the size of the digest output.
*/
PALEXPORT DigestCtx* AppleCryptoNative_DigestCreate(PAL_HashAlgorithm algorithm, int32_t* pcbDigest);

/*
Apply cbBuf bytes of data from pBuf to the ongoing digest represented in ctx.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t AppleCryptoNative_DigestUpdate(DigestCtx* ctx, uint8_t* pBuf, int32_t cbBuf);

/*
Complete the digest in ctx, copying the results to pOutput, and reset ctx for a new digest.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t AppleCryptoNative_DigestFinal(DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput);

/*
Get the digest of the data already loaded into ctx, without resetting ctx.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t AppleCryptoNative_DigestCurrent(const DigestCtx* ctx, uint8_t* pOutput, int32_t cbOutput);

/*
Combines DigestCreate, DigestUpdate, and DigestFinal in to a single operation.

Returns 1 on success, 0 on failure, any other value on invalid inputs/state.
*/
PALEXPORT int32_t AppleCryptoNative_DigestOneShot(PAL_HashAlgorithm algorithm, uint8_t* pBuf, int32_t cbBuf, uint8_t* pOutput, int32_t cbOutput, int32_t* pcbDigest);
