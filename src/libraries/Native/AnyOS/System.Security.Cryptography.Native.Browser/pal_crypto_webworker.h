// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>

enum sha_hash
{
    sha_hash_1,
    sha_hash_256,
    sha_hash_384,
    sha_hash_512,
};

PALEXPORT int32_t SystemCryptoNativeBrowser_SHAHash(
    enum sha_hash ver,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len);
