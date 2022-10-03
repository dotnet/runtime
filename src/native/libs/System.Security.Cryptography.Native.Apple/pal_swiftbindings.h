// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

PALEXPORT int32_t AppleCryptoNative_ChaCha20Poly1305Encrypt(
    uint8_t* keyPtr,
    int32_t keyLength,
    uint8_t* noncePtr,
    int32_t nonceLength,
    uint8_t* plaintextPtr,
    int32_t plaintextLength,
    uint8_t* ciphertextBuffer,
    int32_t ciphertextBufferLength,
    uint8_t* tagBuffer,
    int32_t tagBufferLength,
    uint8_t* aadPtr,
    int32_t aadLength);

PALEXPORT int32_t AppleCryptoNative_ChaCha20Poly1305Decrypt(
    uint8_t* keyPtr,
    int32_t keyLength,
    uint8_t* noncePtr,
    int32_t nonceLength,
    uint8_t* ciphertextPtr,
    int32_t ciphertextLength,
    uint8_t* tagPtr,
    int32_t tagLength,
    uint8_t* plaintextBuffer,
    int32_t plaintextBufferLength,
    uint8_t* aadPtr,
    int32_t aadLength);
