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

static PInvokeOverrideFn* s_overrideImpl = nullptr;

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

void PInvokeOverride::SetPInvokeOverride(PInvokeOverrideFn* overrideImpl)
{
    s_overrideImpl = overrideImpl;
}

const void* PInvokeOverride::GetMethodImpl(const char* libraryName, const char* entrypointName)
{
    if (s_overrideImpl != nullptr)
    {
        const void* result = s_overrideImpl(libraryName, entrypointName);
        if (result != nullptr)
        {
            LOG((LF_INTEROP, LL_INFO1000, "PInvoke overriden for: lib: %s, entry: %s \n", libraryName, entrypointName));
            return result;
        }
    }

    return DefaultResolveDllImport(libraryName, entrypointName);
}
