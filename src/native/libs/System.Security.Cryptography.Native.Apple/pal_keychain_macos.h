// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Get a CFRetain()ed SecKeychainRef value for the keychain to which the keychain item belongs.

The behavior of this function is undefined if `item` is not a CFTypeRef.
For types that are not understood by this function to be keychain items an invalid parameter error is returned.
Errors of the item having no keychain are suppressed, returning success (0) with *pKeychainOut set to NULL.

For all other situations, see SecKeychainItemCopyKeychain documentation.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainItemCopyKeychain(SecKeychainItemRef item, SecKeychainRef* pKeychainOut);

/*
Create a keychain at the specified location with a given (UTF-8 encoded) lock passphrase.

Returns the result of SecKeychainCreate.

Output:
pKeychainOut: The SecKeychainRef created by this function
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainCreate(const char* pathName,
                                                      uint32_t passphraseLength,
                                                      const uint8_t* passphraseUtf8,
                                                      SecKeychainRef* pKeychainOut);

/*
Delete a keychain, including the file on disk.

Returns the result of SecKeychainDelete
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainDelete(SecKeychainRef keychain);

/*
Open the default keychain.
This is usually login.keychain, but can be adjusted by the user.

Returns the result of SecKeychainCopyDefault.

Output:
pKeyChainOut: Receives the SecKeychainRef for the default keychain.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainCopyDefault(SecKeychainRef* pKeychainOut);

/*
Open the named keychain (full path to the file).

Returns the result of SecKeychainOpen.

Output:
pKeychainOut: Receives the SecKeychainRef for the named keychain.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainOpen(const char* pszKeychainPath, SecKeychainRef* pKeychainOut);

/*
Unlock an opened keychain with a given (UTF-8 encoded) lock passphrase.

Returns the result of SecKeychainUnlock.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainUnlock(SecKeychainRef keychain,
                                                      uint32_t passphraseLength,
                                                      const uint8_t* passphraseUtf8);

/*
Set a keychain to never (automatically) lock.

Returns the result of SecKeychainSetSettings to a never-auto-lock policy.
*/
PALEXPORT int32_t AppleCryptoNative_SetKeychainNeverLock(SecKeychainRef keychain);

/*
Enumerate the certificate objects within the given keychain.

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t
AppleCryptoNative_SecKeychainEnumerateCerts(SecKeychainRef keychain, CFArrayRef* pCertsOut, int32_t* pOSStatus);

/*
Enumerate the certificate objects within the given keychain.

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Note that any identity will also necessarily be returned as a certificate with no private key by
SecKeychainEnumerateCerts.  De-duplication of values is the responsibility of the caller.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeychainEnumerateIdentities(SecKeychainRef keychain,
                                                                   CFArrayRef* pIdentitiesOut,
                                                                   int32_t* pOSStatus);

/*
Add a certificate from the specified keychain.

Returns
0 on failure -> see OSStatus
1 on success
any other value is invalid

Output:
pOSStatus: Receives the last OSStatus value..
*/
PALEXPORT int32_t
AppleCryptoNative_X509StoreAddCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, int32_t* pOSStatus);

/*
Remove a certificate from the specified keychain.

Returns
0 on failure -> see OSStatus
1 on success (including no item to delete),
2 on blocking user trust modification,
3 on blocking system trust modification,
4 on deleting an existing certificate while in read only mode,
any other value is invalid

Output:
pOSStatus: Receives the last OSStatus value..
*/
PALEXPORT int32_t
AppleCryptoNative_X509StoreRemoveCertificate(CFTypeRef certOrIdentity, SecKeychainRef keychain, uint8_t isReadOnlyMode, int32_t* pOSStatus);
