// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

extern "C" int32_t SystemNative_GetUnixVersion(char* version, int* capacity);

extern "C" int32_t SystemNative_GetUnixArchitecture();

enum 
{
    ARCH_X86,
    ARCH_X64,
    ARCH_ARM
};
