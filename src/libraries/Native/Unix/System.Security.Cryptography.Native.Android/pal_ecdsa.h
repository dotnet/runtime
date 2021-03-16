// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_eckey.h"
#include "pal_types.h"

/*
Shims the ECDSA_sign method.

Returns 1 on success, otherwise 0.
*/
PALEXPORT int32_t
AndroidCryptoNative_EcDsaSign(const uint8_t* dgst, int32_t dgstlen, uint8_t* sig, int32_t* siglen, EC_KEY* key);

/*
Shims the ECDSA_verify method.

Returns 1 for a correct signature, 0 for an incorrect signature, -1 on error.
*/
PALEXPORT int32_t
AndroidCryptoNative_EcDsaVerify(const uint8_t* dgst, int32_t dgstlen, const uint8_t* sig, int32_t siglen, EC_KEY* key);

/*
Shims the ECDSA_size method.

Returns the maximum length of a DER encoded ECDSA signature created with this key.
*/
PALEXPORT int32_t AndroidCryptoNative_EcDsaSize(const EC_KEY* key);
