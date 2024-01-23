// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// pinvokeoverride.cpp
//
// Helpers to implement PInvoke overriding
//
//*****************************************************************************

#include "common.h"
#include "pinvokeoverride.h"

extern "C" const void* GlobalizationResolveDllImport(const char* name);

namespace
{
    PInvokeOverrideFn* s_overrideImpls[(int)PInvokeOverride::Source::Last + 1] = {0};
    bool s_hasOverrides = false;
}

#if defined(_WIN32)
#define GLOBALIZATION_DLL_NAME "System.Globalization.Native"
#else
#define GLOBALIZATION_DLL_NAME "libSystem.Globalization.Native"
#endif

// here we handle PInvokes whose implementation is always statically linked (even in .so/.dll case)
static const void* DefaultResolveDllImport(const char* libraryName, const char* entrypointName)
{
    if (strcmp(libraryName, GLOBALIZATION_DLL_NAME) == 0)
    {
        return GlobalizationResolveDllImport(entrypointName);
    }

    return nullptr;
}

void PInvokeOverride::SetPInvokeOverride(PInvokeOverrideFn* overrideImpl, Source source)
{
    _ASSERTE(s_overrideImpls[(int)source] == NULL);
    s_overrideImpls[(int)source] = overrideImpl;
    s_hasOverrides = true;
}

const void* PInvokeOverride::GetMethodImpl(const char* libraryName, const char* entrypointName)
{
    if (s_hasOverrides)
    {
        for (size_t i = 0; i < ARRAY_SIZE(s_overrideImpls); ++i)
        {
            PInvokeOverrideFn* overrideImpl = s_overrideImpls[i];
            if (overrideImpl == nullptr)
                continue;

            const void* result = overrideImpl(libraryName, entrypointName);
            if (result != nullptr)
            {
                LOG((LF_INTEROP, LL_INFO1000, "PInvoke overridden for: lib: %s, entry: %s \n", libraryName, entrypointName));
                return result;
            }
        }
    }

    return DefaultResolveDllImport(libraryName, entrypointName);
}
