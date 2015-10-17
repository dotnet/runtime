// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_types.h"

#include <openssl/evp.h>

/*
Shims the EVP_PKEY_new method.

Returns the new EVP_PKEY instance.
*/
extern "C" EVP_PKEY* EvpPkeyCreate();

/*
Cleans up and deletes a EVP_PKEY instance.

Implemented by calling EVP_PKEY_free.

No-op if pkey is null.
The given EVP_PKEY pointer is invalid after this call.
Always succeeds.
*/
extern "C" void EvpPkeyDestroy(EVP_PKEY* pkey);

/*
Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when
duplicating a private key context as part of duplicating the Pal object.

Returns the number (as of this call) of references to the EVP_PKEY. Anything less than
2 is an error, because the key is already in the process of being freed.
*/
extern "C" int32_t UpRefEvpPkey(EVP_PKEY* pkey);
