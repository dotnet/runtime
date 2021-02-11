// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_jni.h"

typedef enum
{
    PAL_SSL_NONE  = 0,
    PAL_SSL_SSL2  = 12,
    PAL_SSL_SSL3  = 48,
    PAL_SSL_TLS   = 192,
    PAL_SSL_TLS11 = 768,
    PAL_SSL_TLS12 = 3072,
    PAL_SSL_TLS13 = 12288,
} SslProtocols;

PALEXPORT int32_t CryptoNative_OpenSslGetProtocolSupport(SslProtocols protocol);
