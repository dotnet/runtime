// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the ECDSA_sign method.

Returns 1 on success, otherwise 0.
*/
DLLEXPORT int32_t
CryptoNative_EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key);

/*
Shims the ECDSA_verify method.

Returns 1 for a correct signature, 0 for an incorrect signature, -1 on error.
*/
DLLEXPORT int32_t
CryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key);

/*
Shims the ECDSA_size method.

Returns the maximum length of a DER encoded ECDSA signature created with this key.
*/
DLLEXPORT int32_t CryptoNative_EcDsaSize(const EC_KEY* key);
