// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT const char* SystemNative_GetUnixName(void);

PALEXPORT char* SystemNative_GetUnixRelease(void);

PALEXPORT int32_t SystemNative_GetUnixVersion(char* version, int* capacity);

PALEXPORT int32_t SystemNative_GetOSArchitecture(void);

// Keep in sync with System.Runtime.InteropServices.Architecture enum
enum 
{
    ARCH_X86,
    ARCH_X64,
    ARCH_ARM,
    ARCH_ARM64,
    ARCH_WASM,
    ARCH_S390X,
    ARCH_LOONGARCH64
};
