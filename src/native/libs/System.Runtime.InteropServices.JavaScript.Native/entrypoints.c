// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>
#include <emscripten.h>

#ifndef EXTERN_C
#define EXTERN_C extern
#endif//EXTERN_C

// implemented in JavaScript
EXTERN_C int32_t SystemInteropJS_InvokeJSImportST(uint8_t* buffer, int32_t bufferLength);

static const Entry s_browserNative[] =
{
    DllImportEntry(SystemInteropJS_InvokeJSImportST)
};

EXTERN_C const void* SystemJSInteropResolveDllImport(const char* name);

EXTERN_C const void* SystemJSInteropResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_browserNative, ARRAY_SIZE(s_browserNative), name);
}
