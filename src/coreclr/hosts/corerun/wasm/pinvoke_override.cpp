// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <string.h>
#include <sal.h>
#include <stdmacros.h>
#include "pinvokeoverride.h"

const void* callhelpers_pinvoke_override(const char* library_name, const char* entry_point_name);

void add_pinvoke_override()
{
    PInvokeOverride::SetPInvokeOverride(callhelpers_pinvoke_override, PInvokeOverride::Source::RuntimeConfiguration);
}

// fake implementations to satisfy the linker
// to avoid linking corerun against libSystem.Runtime.InteropServices.JavaScript.Native
extern "C" {
    void * SystemInteropJS_BindJSImportST (void *) { _ASSERTE(!"Should not be reached"); return nullptr; }
    void SystemInteropJS_CancelPromise (void *) { _ASSERTE(!"Should not be reached"); }
    void SystemInteropJS_InvokeJSFunction (void *, void *) { _ASSERTE(!"Should not be reached"); }
    void SystemInteropJS_InvokeJSImportST (int32_t, void *) { _ASSERTE(!"Should not be reached"); }
    void SystemInteropJS_ReleaseCSOwnedObject (void *) { _ASSERTE(!"Should not be reached"); }
    void SystemInteropJS_ResolveOrRejectPromise (void *) { _ASSERTE(!"Should not be reached"); }
}
