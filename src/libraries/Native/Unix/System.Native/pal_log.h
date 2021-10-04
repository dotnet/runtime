// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT void SystemNative_Log(uint8_t* buffer, int32_t length);
PALEXPORT void SystemNative_LogError(uint8_t* buffer, int32_t length);
