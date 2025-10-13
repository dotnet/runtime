// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <string.h>

#define _In_z_
#define _In_
#include "pinvokeoverride.h"

extern "C" const void* SystemResolveDllImport(const char* name);

// pinvoke_override:
// Check if given function belongs to one of statically linked libraries and return a pointer if found.
static const void* pinvoke_override(const char* library_name, const char* entry_point_name)
{
    // This function is only called with the library name specified for a p/invoke, not any variations.
    // It must handle exact matches to the names specified. See Interop.Libraries.cs for each platform.
    if (strcmp(library_name, "libSystem.Native") == 0)
    {
        return SystemResolveDllImport(entry_point_name);
    }

    return nullptr;
}

void add_pinvoke_override()
{
    PInvokeOverride::SetPInvokeOverride(pinvoke_override, PInvokeOverride::Source::RuntimeConfiguration);
}
