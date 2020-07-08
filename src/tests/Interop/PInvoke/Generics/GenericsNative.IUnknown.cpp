// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdint.h>
#include <xplatform.h>
#include <platformdefines.h>

extern "C" DLL_EXPORT IUnknown* STDMETHODCALLTYPE GetIComInterface()
{
    throw "P/Invoke for IComInterface<T> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetIComInterfaceOut(IUnknown** pValue)
{
    throw "P/Invoke for IComInterface<T> should be unsupported.";
}

extern "C" DLL_EXPORT const IUnknown** STDMETHODCALLTYPE GetIComInterfacePtr()
{
    throw "P/Invoke for IComInterface<T> should be unsupported.";
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetIComInterfaces(IUnknown** pValues, int count)
{
    throw "P/Invoke for IComInterface<T> should be unsupported.";
}

