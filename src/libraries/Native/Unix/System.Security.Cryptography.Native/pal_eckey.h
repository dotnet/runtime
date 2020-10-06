// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Cleans up and deletes an EC_KEY instance.

Implemented by calling EC_KEY_free.

No-op if r is null.
The given EC_KEY pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_EcKeyDestroy(EC_KEY* r);

/*
Shims the EC_KEY_new_by_curve_name method.

Returns the new EC_KEY instance.
*/
PALEXPORT EC_KEY* CryptoNative_EcKeyCreateByOid(const char* oid);

/*
Shims the EC_KEY_generate_key method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EcKeyGenerateKey(EC_KEY* eckey);

/*
Shims the EC_KEY_up_ref method.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EcKeyUpRef(EC_KEY* r);

/*
Gets the key size in bits for the specified EC_KEY.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EcKeyGetSize(const EC_KEY* key, int32_t* keySize);

/*
Gets the NID of the curve name as an oid value for the specified EC_KEY.

Returns 1 upon success, otherwise 0.
*/
PALEXPORT int32_t CryptoNative_EcKeyGetCurveName2(const EC_KEY* key, int32_t* nidName);
