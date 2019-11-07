// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Cleans up and deletes an BIGNUM instance.

Implemented by:
1) Calling BN_clear_free

No-op if a is null.
The given BIGNUM pointer is invalid after this call.
Always succeeds.
*/
DLLEXPORT void CryptoNative_BigNumDestroy(BIGNUM* a);

/*
Shims the BN_bin2bn method.
*/
DLLEXPORT BIGNUM* CryptoNative_BigNumFromBinary(const uint8_t* s, int32_t len);

/*
Shims the BN_bn2bin method.
*/
DLLEXPORT int32_t CryptoNative_BigNumToBinary(const BIGNUM* a, uint8_t* to);

/*
Returns the number of bytes needed to export a BIGNUM.
*/
DLLEXPORT int32_t CryptoNative_GetBigNumBytes(const BIGNUM* a);
