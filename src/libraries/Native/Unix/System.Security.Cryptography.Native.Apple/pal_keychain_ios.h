// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

#include <Security/Security.h>

/*
Enumerate the certificate objects within the given keychain.

Returns the last OSStatus value (noErr on success).

Output:
pCertsOut: When the return value is not noErr, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainEnumerateCerts(CFArrayRef* pCertsOut);

/*
Enumerate the certificate objects within the given keychain.

Returns the last OSStatus value (noErr on success).

Note that any identity will also necessarily be returned as a certificate with no private key by
SecKeychainEnumerateCerts.  De-duplication of values is the responsibility of the caller.

Output:
pCertsOut: When the return value is not noErr, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainEnumerateIdentities(CFArrayRef* pIdentitiesOut);

/*
Add a certificate from the specified keychain.

Returns the last OSStatus value (noErr on success).
*/
PALEXPORT int32_t AppleCryptoNative_X509StoreAddCertificate(CFTypeRef certOrIdentity);

/*
Remove a certificate from the specified keychain.

Returns the last OSStatus value (noErr on success).
*/
PALEXPORT int32_t AppleCryptoNative_X509StoreRemoveCertificate(CFTypeRef certOrIdentity, uint8_t isReadOnlyMode);
