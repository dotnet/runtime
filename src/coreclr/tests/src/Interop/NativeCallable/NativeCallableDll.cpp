// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <platformdefines.h>

#include <thread>

typedef int (STDMETHODCALLTYPE *CALLBACKPROC)(int n);

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc(CALLBACKPROC pCallbackProc, int n)
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
