// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <libs/Common/pal_compiler.h>
#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

PALEXPORT void SystemNative_GetNonCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength);
PALEXPORT int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength);

#ifdef __cplusplus
}
#endif // __cplusplus
