// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdint.h>
#include <string.h>

#ifndef lengthof
#define lengthof(rg) (sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

// expands to:      {"impl", (void*)impl},
#define DllImportEntry(impl) \
    {#impl, (void*)impl},

static const void* ResolveDllImport(const Entry* resolutionTable, size_t tableLength, const char* name)
{
    for (size_t i = 0; i < tableLength; i++)
    {
        if (strcmp(name, resolutionTable[i].name) == 0)
        {
            return resolutionTable[i].method;
        }
    }

    return NULL;
}
