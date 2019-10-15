// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_digest.h"
#include "pal_seckey.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Generate a new RSA keypair with the specified key size, in bits.

Returns 1 on success, 0 on failure.  On failure, *pOSStatus should contain the OS reported error.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaGenerateKey(int32_t keySizeBits,
                                                   SecKeychainRef tempKeychain,
                                                   SecKeyRef* pPublicKey,
                                                   SecKeyRef* pPrivateKey,
                                                   int32_t* pOSStatus);

/*
Decrypt the contents of pbData using the provided privateKey under OAEP padding.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaDecryptOaep(SecKeyRef privateKey,
                                                   uint8_t* pbData,
                                                   int32_t cbData,
                                                   PAL_HashAlgorithm mfgAlgorithm,
                                                   CFDataRef* pDecryptedOut,
                                                   CFErrorRef* pErrorOut);

/*
Decrypt the contents of pbData using the provided privateKey under PKCS#1 padding.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaDecryptPkcs(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDecryptedOut, CFErrorRef* pErrorOut);

/*
Encrypt pbData for the provided publicKey using OAEP padding.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaEncryptOaep(SecKeyRef publicKey,
                                                   uint8_t* pbData,
                                                   int32_t cbData,
                                                   PAL_HashAlgorithm mgfAlgorithm,
                                                   CFDataRef* pEncryptedOut,
                                                   CFErrorRef* pErrorOut);

/*
Encrypt pbData for the provided publicKey using PKCS#1 padding.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaEncryptPkcs(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pEncryptedOut, CFErrorRef* pErrorOut);

/*
Apply an RSA private key to a signing operation on data which was already padded.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaSignaturePrimitive(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut);

/*
Apply an RSA private key to an encryption operation to emit data which is still padded.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaDecryptionPrimitive(
    SecKeyRef privateKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut);

/*
Apply an RSA public key to an encryption operation on data which was already padded.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaEncryptionPrimitive(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut);

/*
Apply an RSA public key to a signing operation to emit data which is still padded.

Follows pal_seckey return conventions.
*/
DLLEXPORT int32_t AppleCryptoNative_RsaVerificationPrimitive(
    SecKeyRef publicKey, uint8_t* pbData, int32_t cbData, CFDataRef* pDataOut, CFErrorRef* pErrorOut);
