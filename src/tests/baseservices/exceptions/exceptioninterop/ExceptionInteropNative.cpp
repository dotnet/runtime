// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <exception>
#include <platformdefines.h>

extern "C" DLL_EXPORT void STDMETHODCALLTYPE ThrowException()
{
    throw std::exception{};
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE NativeFunction()
{
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE CallCallback(void (*cb)())
{
    cb();
}