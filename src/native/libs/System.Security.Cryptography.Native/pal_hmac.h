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

struct _DN_MAC_CTX {
    HMAC_CTX* legacy;
    EVP_MAC_CTX* mac;
    EVP_MAC_CTX* original;
};

typedef struct _DN_MAC_CTX DN_MAC_CTX;

/**
 * Creates and initializes a context with the given key and EVP_MD. The context is not guaranteed to be a particular
 * OpenSSL construct. It should be treated as an opaque pointer that is passed to the appropriate CryptoNative functions.
 *
 * Returns new context on success, nullptr on failure.
 */
PALEXPORT DN_MAC_CTX* CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, const EVP_MD* md);

/**
 * Cleans up and deletes an context instance created by HmacCreate.
 *
 * No-op if ctx is null.
 * Always succeeds.
 */
PALEXPORT void CryptoNative_HmacDestroy(DN_MAC_CTX* ctx);

/**
 * Resets an instance for a new computation, preserving the key and EVP_MD.
*/
PALEXPORT int32_t CryptoNative_HmacReset(DN_MAC_CTX* ctx);

/**
 * Appends data to the computation.
 *
 * Returns 1 for success or 0 for failure.
 */
PALEXPORT int32_t CryptoNative_HmacUpdate(DN_MAC_CTX* ctx, const uint8_t* data, int32_t len);

/**
 * Finalizes the computation and obtains the result.
 *
 * Returns 1 for success or 0 for failure.
 */
PALEXPORT int32_t CryptoNative_HmacFinal(DN_MAC_CTX* ctx, uint8_t* md, int32_t* len);

/**
 * Retrieves the HMAC for the data already accumulated in ctx without finalizing the state.
 *
 * Returns 1 for success or 0 for failure.
 */
PALEXPORT int32_t CryptoNative_HmacCurrent(const DN_MAC_CTX* ctx, uint8_t* md, int32_t* len);

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

/**
 * Clones the context of the HMAC.
 * Returns NULL on failure.
*/
PALEXPORT DN_MAC_CTX* CryptoNative_HmacCopy(const DN_MAC_CTX* ctx);
