// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "stdio.h"

#pragma warning(push)
#pragma warning(disable:4265 4577)
 #include <thread>
#pragma warning(pop)

// Work around typedef redefinition: platformdefines.h defines error_t
// as unsigned while it's defined as int in errno.h.
#define error_t error_t_ignore
#include <platformdefines.h>
#undef error_t

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