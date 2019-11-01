// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the EVP_PKEY_get1_EC_KEY method.

Returns the EC_KEY instance for the EVP_PKEY.
*/
DLLEXPORT EC_KEY* CryptoNative_EvpPkeyGetEcKey(EVP_PKEY* pkey);

/*
Shims the EVP_PKEY_set1_EC_KEY method to set the EC_KEY
instance on the EVP_KEY.

Returns 1 upon success, otherwise 0.
*/
DLLEXPORT int32_t CryptoNative_EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key);
