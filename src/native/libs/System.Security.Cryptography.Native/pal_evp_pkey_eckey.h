// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Creates a new EVP_PKEY from a raw EC_KEY pointer.
The EC_KEY is duplicated (up-ref'd) so the caller retains ownership of the input.
Also computes and returns the EC key size via outKeySize (may be NULL).

Returns a new EVP_PKEY* on success, or NULL on failure (including if key size is 0).
*/
PALEXPORT EVP_PKEY* CryptoNative_CreateEvpPkeyFromEcKey(EC_KEY* ecKey, int32_t* outKeySize);
