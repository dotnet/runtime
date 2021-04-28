// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

// Unless another interpretation is "obvious", pal_seckey functions return 1 on success.
// functions which represent a boolean return 0 on "successful false"
// otherwise functions will return one of the following return values:
static const int32_t kErrorBadInput = -1;
static const int32_t kErrorSeeError = -2;
static const int32_t kErrorUnknownAlgorithm = -3;
static const int32_t kErrorUnknownState = -4;
static const int32_t kPlatformNotSupported = -5;

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
/*
Export a key object.

Public keys are exported using the "OpenSSL" format option, which means, essentially,
"whatever format the openssl CLI would use for this algorithm by default".

Private keys are exported using the "Wrapped PKCS#8" format. These formats are available via
`openssl pkcs8 -topk8 ...`. While the PKCS#8 container is the same for all key types, the
payload is algorithm-dependent (though identified by the PKCS#8 wrapper).

An export passphrase is required for private keys, and ignored for public keys.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyExport(
    SecKeyRef pKey, int32_t exportPrivate, CFStringRef cfExportPassphrase, CFDataRef* ppDataOut, int32_t* pOSStatus);

/*
Import a key from a key blob.

Imports are always done using the "OpenSSL" format option, which means the format used for an
unencrypted private key via the openssl CLI verb of the algorithm being imported.

For public keys the "OpenSSL" format is NOT the format used by the openssl CLI for that algorithm,
but is in fact the X.509 SubjectPublicKeyInfo structure.

Returns 1 on success, 0 on failure (*pOSStatus should be set) and negative numbers for various
state machine errors.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyImportEphemeral(
    uint8_t* pbKeyBlob, int32_t cbKeyBlob, int32_t isPrivateKey, SecKeyRef* ppKeyOut, int32_t* pOSStatus);
#endif

/*
For RSA and DSA this function returns the number of bytes in "the key", which corresponds to
the length of n/Modulus for RSA and for P in DSA.

For ECC the value should not be used.

0 is returned for invalid inputs.
*/
PALEXPORT uint64_t AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SecKeyRef publicKey);
