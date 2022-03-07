// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Generate an ECC keypair of the specified size.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_EccGenerateKey(int32_t keySizeBits,
                                                   SecKeyRef* pPublicKey,
                                                   SecKeyRef* pPrivateKey,
                                                   CFErrorRef* pErrorOut);

/*
Get the keysize, in bits, of an ECC key.

Returns the keysize, in bits, of the ECC key, or 0 on error.
*/
PALEXPORT uint64_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey);
