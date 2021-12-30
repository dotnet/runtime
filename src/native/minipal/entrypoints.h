// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_ENTRYPOINTS_H
#define HAVE_MINIPAL_ENTRYPOINTS_H

#include <stdint.h>
#include <string.h>
#include <minipal/utils.h>

typedef struct
{
    const char* name;
    const void* method;
} Entry;

// expands to:      {"impl", (void*)impl},
#define DllImportEntry(impl) \
    {#impl, (void*)impl},

static const void* minipal_resolve_dllimport(const Entry* resolutionTable, size_t tableLength, const char* name)
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

#endif // HAVE_MINIPAL_ENTRYPOINTS_H
