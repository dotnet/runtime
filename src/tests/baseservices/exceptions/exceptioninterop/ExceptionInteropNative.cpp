// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <exception>
#include <platformdefines.h>
#ifdef _WIN32
#pragma warning(push)
#pragma warning(disable:4265 4577)
#include <thread>
#pragma warning(pop)
#endif // _WIN32

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

#ifdef _WIN32
extern "C" DLL_EXPORT void STDMETHODCALLTYPE InvokeCallbackAndCatchNative(PFNACTION1 callback)
{
    try
    {
        callback();
    }
    catch (std::exception& ex)
    {
        printf("Caught exception %s in native code\n", ex.what());
    }
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE InvokeCallbackOnNewThread(PFNACTION1 callback)
{
    std::thread t1(InvokeCallbackAndCatchNative, callback);
    t1.join();
}
#endif // _WIN32
