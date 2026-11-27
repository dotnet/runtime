// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef struct
{
    uintptr_t size;
    uint32_t ram_size;
} AreaInfo;

PALEXPORT int32_t SystemNative_GetNextAreaInfo(int32_t team, intptr_t* cookie, AreaInfo* areaInfo);
