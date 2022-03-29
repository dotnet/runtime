// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include "pal_types.h"
#include "pal_compiler.h"

typedef struct hmac_ctx_st HmacCtx;

/*
Free a HmacCtx created by AppleCryptoNative_HmacCreate
*/
PALEXPORT void AppleCryptoNative_HmacFree(HmacCtx* pHmac);

/*
Create an HmacCtx for the specified algorithm, receiving the hash output size in pcbHmac.

If *pcbHmac is negative the algorithm is unknown or not supported. If a non-NULL value is returned
it should be freed via AppleCryptoNative_HmacFree regardless of a negative pbHmac value.

Returns NULL on error, an unkeyed HmacCtx otherwise.
*/
PALEXPORT HmacCtx* AppleCryptoNative_HmacCreate(PAL_HashAlgorithm algorithm, int32_t* pcbHmac);

/*
Initialize an HMAC to the correct key and start state.

Returns 1 on success, 0 on error.
*/
PALEXPORT int32_t AppleCryptoNative_HmacInit(HmacCtx* ctx, uint8_t* pbKey, int32_t cbKey);

/*
Add data into the HMAC

Returns 1 on success, 0 on error.
*/
PALEXPORT int32_t AppleCryptoNative_HmacUpdate(HmacCtx* ctx, uint8_t* pbData, int32_t cbData);

/*
Complete the HMAC and copy the result into pbOutput.

Returns 1 on success, 0 on error.
*/
PALEXPORT int32_t AppleCryptoNative_HmacFinal(HmacCtx* ctx, uint8_t* pbOutput);

/*
Computes the HMAC of the accumulated data in ctx without resetting the state.

Returns 1 on success, 0 on error.
*/
PALEXPORT int32_t AppleCryptoNative_HmacCurrent(const HmacCtx* ctx, uint8_t* pbOutput);

/*
Computes the HMAC of data with a key in to the pOutput buffer in one step.

Return 1 on success, 0 on error, and negative values for invalid input.
*/
PALEXPORT int32_t AppleCryptoNative_HmacOneShot(PAL_HashAlgorithm algorithm,
                                                const uint8_t* pKey,
                                                int32_t cbKey,
                                                const uint8_t* pBuf,
                                                int32_t cbBuf,
                                                uint8_t* pOutput,
                                                int32_t cbOutput,
                                                int32_t* pcbDigest);
