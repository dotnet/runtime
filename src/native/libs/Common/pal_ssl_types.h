// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>

// Matches managed System.Security.Authentication.SslProtocols
enum
{
    PAL_SslProtocol_None = 0,
    PAL_SslProtocol_Ssl2 = 12,
    PAL_SslProtocol_Ssl3 = 48,
    PAL_SslProtocol_Tls10 = 192,
    PAL_SslProtocol_Tls11 = 768,
    PAL_SslProtocol_Tls12 = 3072,
    PAL_SslProtocol_Tls13 = 12288,
};
typedef int32_t PAL_SslProtocol;
