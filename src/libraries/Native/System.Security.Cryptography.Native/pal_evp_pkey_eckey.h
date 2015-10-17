// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/ec.h>
#include <openssl/evp.h>

/*
Shims the EVP_PKEY_get1_EC_KEY method.

Returns the EC_KEY instance for the EVP_PKEY.
*/
extern "C" EC_KEY* EvpPkeyGetEcKey(EVP_PKEY* pkey);

/*
Shims the EVP_PKEY_set1_EC_KEY method to set the EC_KEY
instance on the EVP_KEY.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key);
