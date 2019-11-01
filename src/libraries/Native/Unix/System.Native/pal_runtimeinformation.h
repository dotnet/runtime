// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

DLLEXPORT const char* SystemNative_GetUnixName(void);

DLLEXPORT char* SystemNative_GetUnixRelease(void);

DLLEXPORT int32_t SystemNative_GetUnixVersion(char* version, int* capacity);

DLLEXPORT int32_t SystemNative_GetOSArchitecture(void);

DLLEXPORT int32_t SystemNative_GetProcessArchitecture(void);

enum 
{
    ARCH_X86,
    ARCH_X64,
    ARCH_ARM,
    ARCH_ARM64
};
