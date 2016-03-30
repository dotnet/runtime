// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdio.h"

#pragma warning(push)
#pragma warning(disable:4265 4577)
 #include <thread>
#pragma warning(pop)


#if defined _WIN32
  #define DLL_EXPORT __declspec(dllexport)
#else
  #if __GNUC__ >= 4
    #define DLL_EXPORT __attribute__ ((visibility ("default")))
  #else
    #define DLL_EXPORT
  #endif
#endif //_WIN32

typedef void (*PFNACTION1)();

extern "C" DLL_EXPORT void InvokeCallback(PFNACTION1 callback)
{
    callback();
}

extern "C" DLL_EXPORT void InvokeCallbackOnNewThread(PFNACTION1 callback)
{
    std::thread t1(InvokeCallback, callback);
    t1.join();
}