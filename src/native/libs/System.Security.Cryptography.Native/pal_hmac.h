// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

// The shim API here is slightly less than 1:1 with underlying API so that:
//   * P/Invokes are less chatty
//   * The lifetime semantics are more obvious.
//   * Managed code remains resilient to changes in size of HMAC_CTX across platforms

// Forward declarations - shim API must not depend on knowing layout of these types.
typedef struct hmac_ctx_st HMAC_CTX;

/**
 * Creates and initializes an HMAC_CTX with the given key and EVP_MD.
 *
 * Implemented by:
 *    1) allocating a new HMAC_CTX
 *    2) calling HMAC_CTX_Init on the new HMAC_CTX
 *    3) calling HMAC_Init_ex with the new HMAC_CTX and the given args.
 *
 * Returns new HMAC_CTX on success, nullptr on failure.
 */
PALEXPORT HMAC_CTX* CryptoNative_HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md);

/**
 * Cleans up and deletes an HMAC_CTX instance created by HmacCreate.
 *
 * Implemented by:
 *   1) Calling HMAC_CTX_Cleanup
 *   2) Deleting the HMAC_CTX instance.
 *
 * No-op if ctx is null.
 * The given HMAC_CTX pointer is invalid after this call.
 * Always succeeds.
 */
PALEXPORT void CryptoNative_HmacDestroy(HMAC_CTX* ctx);

/**
 * Resets an HMAC_CTX instance for a new computation, preserving the key and EVP_MD.
 *
 * Implemented by passing all null/0 values but ctx to HMAC_Init_ex.
*/
PALEXPORT int32_t CryptoNative_HmacReset(HMAC_CTX* ctx);

/**
 * Appends data to the computation.
 *
 * Direct shim to HMAC_Update.
 *
 * Returns 1 for success or 0 for failure. (Always succeeds on platforms where HMAC_Update returns void.)
 */
PALEXPORT int32_t CryptoNative_HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len);

/**
 * Finalizes the computation and obtains the result.
 *
 * Direct shim to HMAC_Final.
 *
 * Returns 1 for success or 0 for failure. (Always succeeds on platforms where HMAC_Update returns void.)
 */
PALEXPORT int32_t CryptoNative_HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len);

/**
 * Retrieves the HMAC for the data already accumulated in ctx without finalizing the state.
 *
 * Returns 1 for success or 0 for failure.
 */
PALEXPORT int32_t CryptoNative_HmacCurrent(const HMAC_CTX* ctx, uint8_t* md, int32_t* len);

/**
 * Computes the HMAC of data using a key in a single operation.
 * Returns -1 on invalid input, 0 on failure, and 1 on success.
 */
PALEXPORT int32_t CryptoNative_HmacOneShot(const EVP_MD* type,
                                           const uint8_t* key,
                                           int32_t keySize,
                                           const uint8_t* source,
                                           int32_t sourceSize,
                                           uint8_t* md,
                                           int32_t* mdSize);
