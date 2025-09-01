// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <../../Interop/common/xplatform.h>
#include <../../Interop/common/ComHelpers.h>
#include "platformdefines.h"

#ifdef _WIN32
#pragma warning(push)
#pragma warning(disable:4265 4577)
#include <thread>
#pragma warning(pop)
#else // _WIN32
#include <pthread.h>
#endif // _WIN32

// #include <platformdefines.h>

// Interface ID for ITest
// {92BAA992-DB5A-4ADD-977B-B22838EE91FD}
static const GUID IID_ITest =
{ 0x92baa992, 0xdb5a, 0x4add, { 0x97, 0x7b, 0xb2, 0x28, 0x38, 0xee, 0x91, 0xfd } };

// Interface definition for ITest
MIDL_INTERFACE("92BAA992-DB5A-4ADD-977B-B22838EE91FD")
ITest : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE Test() = 0;
};

void NativeTestThread(IUnknown* pUnknown)
{
    ITest* pITest = nullptr;
    pUnknown->QueryInterface(IID_ITest, reinterpret_cast<void**>(&pITest));
    // This tests causes the QueryInterface to fail, so we don't release the pITest
}

#ifndef _WIN32
void* NativeTestThreadUnix(void* pUnknown)
{
    NativeTestThread((IUnknown*)pUnknown);
    return NULL;
}

#define AbortIfFail(st) if (st != 0) abort()

#endif // !_WIN32

extern "C" DLL_EXPORT void TestFromNativeThread(IUnknown* pUnknown)
{
#ifdef _WIN32
    std::thread t1(NativeTestThread, pUnknown);
    t1.join();
#else // _WIN32
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
    st = pthread_create(&t, &attr, NativeTestThreadUnix, (void*)pUnknown);
    AbortIfFail(st);

    st = pthread_join(t, NULL);
    AbortIfFail(st);
#endif // _WIN32
}
