// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_browser.h"
#include "pal_crypto_webworker.h"

// Forward declarations
extern int32_t dotnet_browser_simple_digest_hash(
    enum simple_digest ver,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len);

extern int32_t dotnet_browser_can_use_simple_digest_hash(void);

int32_t SystemCryptoNativeBrowser_SimpleDigestHash(
    enum simple_digest ver,
    uint8_t* input_buffer,
    int32_t input_len,
    uint8_t* output_buffer,
    int32_t output_len)
{
    return dotnet_browser_simple_digest_hash(ver, input_buffer, input_len, output_buffer, output_len);
}

int32_t SystemCryptoNativeBrowser_CanUseSimpleDigestHash(void)
{
    return dotnet_browser_can_use_simple_digest_hash();
}
