// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// PAL stubs that are only needed when targeting WASI (browser-wasm gets
// real implementations from Emscripten). Shared WASM stubs live in
// wasm-stubs.cpp.

#include "pal/dbgmsg.h"
#include "pal/signal.hpp"

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

extern "C" void DebugBreak()
{
    DBG_DebugBreak();
}

extern "C" void PALAPI OutputDebugStringA(LPCSTR lpOutputString)
{
    (void)lpOutputString;
}

extern "C" void PALAPI OutputDebugStringW(LPCWSTR lpOutputString)
{
    (void)lpOutputString;
}

// C++ EH runtime symbols (__cxa_thread_atexit, __cxa_allocate_exception,
// __cxa_throw, __cxa_begin_catch, __cxa_end_catch) are provided by the
// wasm-exceptions-enabled libc++abi and libunwind.

BOOL PAL_ProbeMemory(PVOID pBuffer, DWORD cbBuffer, BOOL fWriteAccess)
{
    (void)pBuffer; (void)cbBuffer; (void)fWriteAccess;
    return TRUE;
}

void SEHCleanupSignals(bool isChildProcess)
{
    (void)isChildProcess;
}

void UnmaskActivationSignal()
{
}

BOOL SEHInitializeSignals(CorUnix::CPalThread *pthrCurrent, DWORD flags)
{
    (void)pthrCurrent; (void)flags;
    return TRUE;
}

extern "C" int shm_open(const char *name, int oflag, int mode)
{
    (void)name; (void)oflag; (void)mode;
    return -1;
}

extern "C" int shm_unlink(const char *name)
{
    (void)name;
    return -1;
}

BOOL PALAPI FlushInstructionCache(HANDLE hProcess, LPCVOID lpBaseAddress, SIZE_T dwSize)
{
    (void)hProcess; (void)lpBaseAddress; (void)dwSize;
    return TRUE;
}

BOOL PALAPI GetThreadContext(HANDLE hThread, LPCONTEXT lpContext)
{
    (void)hThread; (void)lpContext;
    return FALSE;
}

void PALAPI PAL_EnableCrashReportBeforeSignalChaining()
{
}

void PALAPI PAL_FreeExceptionRecords(IN EXCEPTION_RECORD *exceptionRecord, IN CONTEXT *contextRecord)
{
    (void)exceptionRecord; (void)contextRecord;
}

BOOL PALAPI PAL_VirtualUnwind(CONTEXT *context)
{
    (void)context;
    return FALSE;
}

VOID PALAPI RaiseException(DWORD dwExceptionCode, DWORD dwExceptionFlags, DWORD nNumberOfArguments, CONST ULONG_PTR *lpArguments)
{
    (void)dwExceptionCode; (void)dwExceptionFlags; (void)nNumberOfArguments; (void)lpArguments;
    abort();
}

extern "C" void SystemJS_GetLocaleInfo(void)
{
}

extern "C" int pthread_getschedparam(pthread_t, int *policy, struct sched_param *param)
{
    if (policy) *policy = 0;
    if (param) memset(param, 0, sizeof(struct sched_param));
    return 0;
}
