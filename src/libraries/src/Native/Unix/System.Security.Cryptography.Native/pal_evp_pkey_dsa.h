// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the EVP_PKEY_get1_DSA method.

Returns the DSA instance for the EVP_PKEY.
*/
DLLEXPORT DSA* CryptoNative_EvpPkeyGetDsa(EVP_PKEY* pkey);

/*
Shims the EVP_PKEY_set1_DSA method to set the DSA
instance on the EVP_KEY.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t CryptoNative_EvpPkeySetDsa(EVP_PKEY* pkey, DSA* dsa);
