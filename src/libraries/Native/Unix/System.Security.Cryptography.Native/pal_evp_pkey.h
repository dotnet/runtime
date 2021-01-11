// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_compiler.h"
#include "opensslshim.h"

/*
Shims the EVP_PKEY_new method.

Returns the new EVP_PKEY instance.
*/
PALEXPORT EVP_PKEY* CryptoNative_EvpPkeyCreate(void);

/*
Cleans up and deletes a EVP_PKEY instance.

Implemented by calling EVP_PKEY_free.

No-op if pkey is null.
The given EVP_PKEY pointer is invalid after this call.
Always succeeds.
*/
PALEXPORT void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey);

/*
Used by System.Security.Cryptography.X509Certificates' OpenSslX509CertificateReader when
duplicating a private key context as part of duplicating the Pal object.

Returns the number (as of this call) of references to the EVP_PKEY. Anything less than
2 is an error, because the key is already in the process of being freed.
*/
PALEXPORT int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey);

/*
Returns the size, in bits, of the key represented by the EVP_PKEY*
*/
PALEXPORT int32_t CryptoNative_EvpPKeyKeySize(EVP_PKEY* pkey);

/*
Assigns a new EVP_PKEY object, which uses the same internal key object as pkeyIn,
to pkeyOut.

Returns 1 on success, 0 on OpenSSL error, other on abnormal behavior.
*/
PALEXPORT int32_t CryptoNative_EvpPkeyDuplicate(EVP_PKEY* pkeyIn, EVP_PKEY** pkeyOut);
