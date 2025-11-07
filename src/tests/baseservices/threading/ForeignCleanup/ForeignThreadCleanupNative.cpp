// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdio.h"
#include <stdlib.h>

#ifdef _WIN32
#pragma warning(push)
#pragma warning(disable:4265 4577)
#include <thread>
#pragma warning(pop)
#else // _WIN32
#include <pthread.h>
#endif // _WIN32

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

#define AbortIfFail(st) if (st != 0) abort()

#endif // !_WIN32

#ifdef _WIN32
void InvokeCallbackOnNewThreadCommon(PFNACTION1 callback, void (*startRoutine)(PFNACTION1), bool joinBeforeReturn)
{
    std::thread t1(startRoutine, callback);

    if (joinBeforeReturn)
    {
        t1.join();
    }
    else
    {
        t1.detach();
    }
}

#else // _WIN32
void InvokeCallbackOnNewThreadCommon(PFNACTION1 callback, void *(*startRoutine)(void*), bool joinBeforeReturn)
{
    // For Unix, we need to use pthreads to create the thread so that we can set its stack size.
    // We need to set the stack size due to the very small (80kB) default stack size on MUSL
    // based Linux distros.
    pthread_attr_t attr;
    int st = pthread_attr_init(&attr);
    AbortIfFail(st);

    // set stack size to 1.5MB
    st = pthread_attr_setstacksize(&attr, 0x180000);
    AbortIfFail(st);

    pthread_t t;
    st = pthread_create(&t, &attr, startRoutine, (void*)callback);
    AbortIfFail(st);

    if (joinBeforeReturn)
    {
        st = pthread_join(t, NULL);
        AbortIfFail(st);
    }
}
#endif // _WIN32

extern "C" DLL_EXPORT void InvokeCallbackOnNewThread(PFNACTION1 callback, bool joinBeforeReturn)
{
#ifdef _WIN32
    InvokeCallbackOnNewThreadCommon(callback, InvokeCallback, joinBeforeReturn);
#else // _WIN32
    InvokeCallbackOnNewThreadCommon(callback, InvokeCallbackUnix, joinBeforeReturn);
#endif
}
