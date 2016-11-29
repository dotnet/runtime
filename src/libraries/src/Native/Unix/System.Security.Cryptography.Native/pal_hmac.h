// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "opensslshim.h"

// The shim API here is slightly less than 1:1 with underlying API so that:
//   * P/Invokes are less chatty
//   * The lifetime semantics are more obvious.
//   * Managed code remains resilient to changes in size of HMAC_CTX across platforms

// Forward declarations - shim API must not depend on knowing layout of these types.
typedef struct hmac_ctx_st HMAC_CTX;
typedef struct env_md_st EVP_MD;

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
extern "C" HMAC_CTX* CryptoNative_HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md);

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
extern "C" void CryptoNative_HmacDestroy(HMAC_CTX* ctx);

/**
 * Resets an HMAC_CTX instance for a new computation, preserving the key and EVP_MD.
 *
 * Implemented by passing all null/0 values but ctx to HMAC_Init_ex.
*/
extern "C" int32_t CryptoNative_HmacReset(HMAC_CTX* ctx);

/**
 * Appends data to the computation.
 *
 * Direct shim to HMAC_Update.
 *
 * Returns 1 for success or 0 for failure. (Always succeeds on platforms where HMAC_Update returns void.)
 */
extern "C" int32_t CryptoNative_HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len);

/**
 * Finalizes the computation and obtains the result.
 *
 * Direct shim to HMAC_Final.
 *
 * Returns 1 for success or 0 for failure. (Always succeeds on platforms where HMAC_Update returns void.)
 */
extern "C" int32_t CryptoNative_HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len);
