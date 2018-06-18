// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_digest.h"
#include "pal_types.h"
#include "pal_compiler.h"

typedef struct hmac_ctx_st HmacCtx;

/*
Free a HmacCtx created by AppleCryptoNative_HmacCreate
*/
DLLEXPORT void AppleCryptoNative_HmacFree(HmacCtx* pHmac);

/*
Create an HmacCtx for the specified algorithm, receiving the hash output size in pcbHmac.

If *pcbHmac is negative the algorithm is unknown or not supported. If a non-NULL value is returned
it should be freed via AppleCryptoNative_HmacFree regardless of a negative pbHmac value.

Returns NULL on error, an unkeyed HmacCtx otherwise.
*/
DLLEXPORT HmacCtx* AppleCryptoNative_HmacCreate(PAL_HashAlgorithm algorithm, int32_t* pcbHmac);

/*
Initialize an HMAC to the correct key and start state.

Returns 1 on success, 0 on error.
*/
DLLEXPORT int32_t AppleCryptoNative_HmacInit(HmacCtx* ctx, uint8_t* pbKey, int32_t cbKey);

/*
Add data into the HMAC

Returns 1 on success, 0 on error.
*/
DLLEXPORT int32_t AppleCryptoNative_HmacUpdate(HmacCtx* ctx, uint8_t* pbData, int32_t cbData);

/*
Complete the HMAC and copy the result into pbOutput.

Returns 1 on success, 0 on error.
*/
DLLEXPORT int32_t AppleCryptoNative_HmacFinal(HmacCtx* ctx, uint8_t* pbOutput);
