// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
PALEXPORT void CryptoNative_BigNumDestroy(BIGNUM* a);

/*
Shims the BN_bin2bn method.
*/
PALEXPORT BIGNUM* CryptoNative_BigNumFromBinary(const uint8_t* s, int32_t len);

/*
Shims the BN_bn2bin method.
*/
PALEXPORT int32_t CryptoNative_BigNumToBinary(const BIGNUM* a, uint8_t* to);

/*
Returns the number of bytes needed to export a BIGNUM.
*/
PALEXPORT int32_t CryptoNative_GetBigNumBytes(const BIGNUM* a);
