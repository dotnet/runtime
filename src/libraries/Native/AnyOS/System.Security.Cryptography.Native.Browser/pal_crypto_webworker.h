// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>

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
