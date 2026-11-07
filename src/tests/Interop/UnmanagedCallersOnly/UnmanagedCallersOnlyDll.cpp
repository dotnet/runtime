// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#include <thread>

extern "C" DLL_EXPORT int STDMETHODCALLTYPE DoubleImplNative(int n)
{
    return 2 * n;
}

typedef int (STDMETHODCALLTYPE *CALLBACKPROC)(int n);
typedef int (__stdcall *CALLBACKPROC_STDCALL_SINGLEARG)(int n);
typedef int (__stdcall *CALLBACKPROC_STDCALL)(int m, int n, int o);
typedef int (__cdecl *CALLBACKPROC_CDECL)(int m, int n, int o);
typedef bool (STDMETHODCALLTYPE *BOOL_CALLBACKPROC)(void* firstArg, void* context);
typedef char16_t (STDMETHODCALLTYPE *CHAR_CALLBACKPROC)(void* firstArg, void* context);
#ifdef _WIN32
typedef int (__fastcall *CALLBACKPROC_FASTCALL)(int m, int n, int o);
#endif // _WIN32

#ifdef _MSC_VER
__declspec(noinline)
#else
__attribute__((noinline))
#endif
static void DirtyStack(uintptr_t value)
{
    volatile uintptr_t stack[128];

    for (size_t i = 0; i < sizeof(stack) / sizeof(stack[0]); i++)
        stack[i] = value;
}

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

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Stdcall_SingleArg(CALLBACKPROC_STDCALL_SINGLEARG pCallbackProc, int n)
{
    return pCallbackProc(n);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Stdcall(CALLBACKPROC_STDCALL pCallbackProc, int m, int n, int o)
{
    return pCallbackProc(m, n, o);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Cdecl(CALLBACKPROC_CDECL pCallbackProc, int m, int n, int o)
{
    return pCallbackProc(m, n, o);
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProcReturningBool(BOOL_CALLBACKPROC pCallbackProc, uintptr_t firstArg)
{
    DirtyStack(firstArg);
    return pCallbackProc(reinterpret_cast<void*>(firstArg), nullptr) ? 1 : 0;
}

extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProcReturningChar(CHAR_CALLBACKPROC pCallbackProc, uintptr_t firstArg)
{
    DirtyStack(firstArg);
    return pCallbackProc(reinterpret_cast<void*>(firstArg), nullptr) == u'A' ? 1 : 0;
}

#ifdef _WIN32
extern "C" DLL_EXPORT int STDMETHODCALLTYPE CallManagedProc_Fastcall(CALLBACKPROC_FASTCALL pCallbackProc, int m, int n, int o)
{
    return pCallbackProc(m, n, o);
}
#endif // _WIN32

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
