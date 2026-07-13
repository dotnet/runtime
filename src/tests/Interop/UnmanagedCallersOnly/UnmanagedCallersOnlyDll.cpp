// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#include <thread>

extern "C" DLL_EXPORT int STDMETHODCALLTYPE DoubleImplNative(int n)
{
    return 2 * n;
}

typedef int (STDMETHODCALLTYPE *CALLBACKPROC)(int n);
typedef int (__stdcall *CALLBACKPROC_STDCALL)(int n);
typedef int (__cdecl *CALLBACKPROC_CDECL)(int n);

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProcMultipleTimes(int m, CALLBACKPROC pCallbackProc, int n)
{
    int acc = 0;
    for (int i = 0; i < m; ++i)
        acc += pCallbackProc(n);

    return acc;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc(CALLBACKPROC pCallbackProc, int n)
{
    return CallManagedProcMultipleTimes(1, pCallbackProc, n);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Stdcall(CALLBACKPROC_STDCALL pCallbackProc, int n)
{
    return pCallbackProc(n);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Cdecl(CALLBACKPROC_CDECL pCallbackProc, int n)
{
    return pCallbackProc(n);
}

namespace
{
    struct ProxyCallContext
    {
        CALLBACKPROC CallbackProc;
        int N;
        int Result;
    };

    void ProxyCall(ProxyCallContext* cxt)
    {
        cxt->Result = CallManagedProc(cxt->CallbackProc, cxt->N);
    }
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProcOnNewThread(CALLBACKPROC pCallbackProc, int n)
{
    ProxyCallContext cxt{ pCallbackProc, n, 0 };
    std::thread newThreadToRuntime{ ProxyCall, &cxt };

    // Wait for new thread to complete
    newThreadToRuntime.join();

    return cxt.Result;
}

#ifdef _WIN32
extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProcCatchException(CALLBACKPROC pCallbackProc, int n)
{
    __try
    {
        return CallManagedProc(pCallbackProc, n);
    }
    __except(EXCEPTION_EXECUTE_HANDLER)
    {
        return -1;
    }
}
#endif // _WIN32
