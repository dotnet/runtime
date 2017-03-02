// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_seckey.h"

#include <Security/Security.h>

/*
Generate an ECC keypair of the specified size.

Returns 1 on success, 0 on failure. On failure, *pOSStatus should carry the OS failure code.
*/
extern "C" int32_t AppleCryptoNative_EccGenerateKey(int32_t keySizeBits,
                                                    SecKeychainRef tempKeychain,
                                                    SecKeyRef* pPublicKey,
                                                    SecKeyRef* pPrivateKey,
                                                    int32_t* pOSStatus);

/*
Get the keysize, in bits, of an ECC key.

Returns the keysize, in bits, of the ECC key, or 0 on error.
*/
extern "C" uint64_t AppleCryptoNative_EccGetKeySizeInBits(SecKeyRef publicKey);
