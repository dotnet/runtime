// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT char* SystemNative_GetUnixRelease(void);

PALEXPORT int32_t SystemNative_GetUnixVersion(char* version, int* capacity);

PALEXPORT int32_t SystemNative_GetOSArchitecture(void);
