// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/bn.h>

/*
Cleans up and deletes an BIGNUM instance.

Implemented by:
1) Calling BN_clear_free

No-op if a is null.
The given BIGNUM pointer is invalid after this call.
Always succeeds.
*/
extern "C" void BigNumDestroy(BIGNUM* a);

/*
Shims the BN_bin2bn method.
*/
extern "C" BIGNUM* BigNumFromBinary(const uint8_t* s, int32_t len);

/*
Shims the BN_bn2bin method.
*/
extern "C" int32_t BigNumToBinary(const BIGNUM* a, uint8_t* to);

/*
Returns the number of bytes needed to export a BIGNUM.
*/
extern "C" int32_t GetBigNumBytes(const BIGNUM* a);
