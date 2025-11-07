// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>
#include <emscripten.h>

#ifndef EXTERN_C
#define EXTERN_C extern
#endif//EXTERN_C

// implemented in JavaScript
EXTERN_C void* SystemInteropJS_BindJSImportST(void* signature);
EXTERN_C void SystemInteropJS_InvokeJSImportST(int32_t functionHandle, void *args);
EXTERN_C void SystemInteropJS_ReleaseCSOwnedObject (int32_t jsHandle);
EXTERN_C void SystemInteropJS_ResolveOrRejectPromise (void *args);
EXTERN_C void SystemInteropJS_CancelPromise (int32_t taskHolderGCHandle);
EXTERN_C void SystemInteropJS_InvokeJSFunction (int32_t functionJSSHandle, void *args);

static const Entry s_browserNative[] =
{
    DllImportEntry(SystemInteropJS_BindJSImportST)
    DllImportEntry(SystemInteropJS_InvokeJSImportST)
    DllImportEntry(SystemInteropJS_ReleaseCSOwnedObject)
    DllImportEntry(SystemInteropJS_ResolveOrRejectPromise)
    DllImportEntry(SystemInteropJS_CancelPromise)
    DllImportEntry(SystemInteropJS_InvokeJSFunction)
};

EXTERN_C const void* SystemJSInteropResolveDllImport(const char* name);

EXTERN_C const void* SystemJSInteropResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_browserNative, ARRAY_SIZE(s_browserNative), name);
}
