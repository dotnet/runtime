// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdio.h"
#include <stdlib.h>
#include <platformdefines.h>

typedef void (*PFNACTION1)();
extern "C" DLL_EXPORT void InvokeCallback(PFNACTION1 callback)
{
    callback();
}

#ifndef _WIN32
void* InvokeCallbackUnix(void* callback)
{
    InvokeCallback((PFNACTION1)callback);
    return NULL;
}

#endif // !_WIN32
