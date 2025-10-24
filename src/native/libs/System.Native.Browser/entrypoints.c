// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>
#include <emscripten.h>

#ifndef EXTERN_C
#define EXTERN_C extern
#endif//EXTERN_C

// implemented in JavaScript
EXTERN_C int32_t SystemJS_RandomBytes(uint8_t* buffer, int32_t bufferLength);
EXTERN_C uint16_t* SystemJS_GetLocaleInfo (const uint16_t* locale, int32_t localeLength, const uint16_t* culture, int32_t cultureLength, const uint16_t* result, int32_t resultMaxLength, int *resultLength);

static const Entry s_browserNative[] =
{
    DllImportEntry(SystemJS_RandomBytes)
    DllImportEntry(SystemJS_GetLocaleInfo)
};

EXTERN_C const void* SystemJSResolveDllImport(const char* name);

EXTERN_C const void* SystemJSResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_browserNative, ARRAY_SIZE(s_browserNative), name);
}
