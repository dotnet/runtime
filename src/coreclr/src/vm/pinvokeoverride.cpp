// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// pinvokeoverride.cpp
//
// Helpers to implement PInvoke overriding
//
//*****************************************************************************

#include "pinvokeoverride.h"

PInvokeOverrideFn* PInvokeOverride::s_overrideImpl = nullptr;

void PInvokeOverride::SetPInvokeOverride(PInvokeOverrideFn* overrideImpl)
{
    s_overrideImpl = overrideImpl;
}

const void* PInvokeOverride::TryGetMethodImpl(const char* libraryName, const char* entrypointName)
{
    return s_overrideImpl ?
        s_overrideImpl(libraryName, entrypointName) :
        nullptr;
}
