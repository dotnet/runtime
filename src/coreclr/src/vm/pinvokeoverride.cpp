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

PInvokeOverrideFn* PInvokeOverride::s_overrideImpl = nullptr;

void PInvokeOverride::SetPInvokeOverride(PInvokeOverrideFn* overrideImpl)
{
    s_overrideImpl = overrideImpl;
}

const void* PInvokeOverride::TryGetMethodImpl(const char* libraryName, const char* entrypointName)
{
    if (s_overrideImpl != nullptr)
    {
        const void* result = s_overrideImpl(libraryName, entrypointName);
        if (result != nullptr)
        {
            return result;
        }
    }

    return DefaultResolveDllImport(libraryName, entrypointName);
}

// here we handle PInvokes whose implementation is always statically linked (even in .so/.dll case)
const void* PInvokeOverride::DefaultResolveDllImport(const char* libraryName, const char* entrypointName)
{
    if (strcmp(libraryName, "libSystem.Globalization.Native") == 0)
    {
        return GlobalizationResolveDllImport(entrypointName);
    }

    return nullptr;
}
