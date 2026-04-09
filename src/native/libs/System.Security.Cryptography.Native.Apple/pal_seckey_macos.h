// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

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
