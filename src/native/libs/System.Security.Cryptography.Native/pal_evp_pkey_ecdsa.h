// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "opensslshim.h"
#include "pal_compiler.h"
#include "pal_types.h"

/*
Complete the ECDSA signature generation for the specified hash using the provided ECDSA key
(wrapped in an EVP_PKEY) and padding/digest options.

Returns the number of bytes written to destination, -1 on error.
*/
PALEXPORT int32_t CryptoNative_EcDsaSignHash(EVP_PKEY* pkey,
                                             void* extraHandle,
                                             const uint8_t* hash,
                                             int32_t hashLen,
                                             uint8_t* destination,
                                             int32_t destinationLen);

/*
Verify an ECDSA signature for the specified hash using the provided ECDSA key (wrapped in an EVP_PKEY)
and padding/digest options.

Returns 1 on a verified signature, 0 on a mismatched signature, -1 on error.
*/
PALEXPORT int32_t CryptoNative_EcDsaVerifyHash(EVP_PKEY* pkey,
                                               void* extraHandle,
                                               const uint8_t* hash,
                                               int32_t hashLen,
                                               const uint8_t* signature,
                                               int32_t signatureLen);
