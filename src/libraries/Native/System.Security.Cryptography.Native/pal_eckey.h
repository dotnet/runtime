// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/ec.h>

/*
Cleans up and deletes an EC_KEY instance.

Implemented by calling EC_KEY_free.

No-op if r is null.
The given EC_KEY pointer is invalid after this call.
Always succeeds.
*/
extern "C" void CryptoNative_EcKeyDestroy(EC_KEY* r);

/*
Shims the EC_KEY_new_by_curve_name method.

Returns the new EC_KEY instance.
*/
extern "C" EC_KEY* CryptoNative_EcKeyCreateByCurveName(int32_t nid);

/*
Shims the EC_KEY_generate_key method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t CryptoNative_EcKeyGenerateKey(EC_KEY* eckey);

/*
Shims the EC_KEY_up_ref method.

Returns 1 upon success, otherwise 0.
*/
extern "C" int32_t CryptoNative_EcKeyUpRef(EC_KEY* r);

/*
Gets the curve name for the specified EC_KEY.

Returns the NID of the curve name.
*/
extern "C" int32_t CryptoNative_EcKeyGetCurveName(const EC_KEY* key);
