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

typedef void (*PFNACTION1)();
extern "C" DLL_EXPORT void InvokeCallbackCatchCallbackAndRethrow(PFNACTION1 callback1, PFNACTION1 callback2)
{
    try
    {
        callback1();
    }
    catch (std::exception& ex)
    {
        callback2();
        printf("Caught exception %s in native code, rethrowing\n", ex.what());
        throw;
    }
}

