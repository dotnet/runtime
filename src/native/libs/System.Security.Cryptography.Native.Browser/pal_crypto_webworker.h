// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>

// These values are also defined in the System.Security.Cryptography library's
// browser-crypto implementation, and utilized in the dotnet-crypto-worker in the wasm runtime.
enum simple_digest
{
    sd_sha_1,
    sd_sha_256,
    sd_sha_384,
    sd_sha_512,
};

PALEXPORT int32_t SystemCryptoNativeBrowser_SimpleDigestHash(
    enum simple_digest ver,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len);

PALEXPORT int32_t SystemCryptoNativeBrowser_Sign(
    enum simple_digest ver,
    uint8_t* key_buffer,
    int32_t key_len,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len);

PALEXPORT int32_t SystemCryptoNativeBrowser_EncryptDecrypt(
    int32_t encrypting,
    uint8_t* key_buffer,
    int32_t key_len,
    uint8_t* iv_buffer,
    int32_t iv_len,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len);

PALEXPORT int32_t SystemCryptoNativeBrowser_DeriveBits(
    uint8_t* password_buffer,
    int32_t password_len,
    uint8_t* salt_buffer,
    int32_t salt_len,
    int32_t iterations,
    enum simple_digest hashAlgorithm,
    uint8_t* output_buffer,
    int32_t output_len);

PALEXPORT int32_t SystemCryptoNativeBrowser_CanUseSubtleCryptoImpl(void);
