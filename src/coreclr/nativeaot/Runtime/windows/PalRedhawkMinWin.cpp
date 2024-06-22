// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the Redhawk Platform Abstraction Layer (PAL) library when MinWin is the platform. In this
// case most or all of the import requirements which Redhawk has can be satisfied via a forwarding export to
// some native MinWin library. Therefore most of the work is done in the .def file and there is very little
// code here.
//
// Note that in general we don't want to assume that Windows and Redhawk global definitions can co-exist.
// Since this code must include Windows headers to do its job we can't therefore safely include general
// Redhawk header files.
//
#include "common.h"
#include <windows.h>
#include <stdio.h>
#include <errno.h>

#include "holder.h"

#define _T(s) L##s
#include "RhConfig.h"

#define PalRaiseFailFastException RaiseFailFastException

#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcconfig.h"

#include "thread.h"
#include "threadstore.h"

#ifdef FEATURE_SPECIAL_USER_MODE_APC
#include <versionhelpers.h>
#endif

#define REDHAWK_PALEXPORT extern "C"
#define REDHAWK_PALAPI __stdcall

// Index for the fiber local storage of the attached thread pointer
static uint32_t g_flsIndex = FLS_OUT_OF_INDEXES;

// This is called when each *fiber* is destroyed. When the home fiber of a thread is destroyed,
// it means that the thread itself is destroyed.
// Since we receive that notification outside of the Loader Lock, it allows us to safely acquire
// the ThreadStore lock in the RuntimeThreadShutdown.
void __stdcall FiberDetachCallback(void* lpFlsData)
{
    ASSERT(g_flsIndex != FLS_OUT_OF_INDEXES);
    ASSERT(lpFlsData == FlsGetValue(g_flsIndex));

    if (lpFlsData != NULL)
    {
        // The current fiber is the home fiber of a thread, so the thread is shutting down
        RuntimeThreadShutdown(lpFlsData);
    }
}

static HMODULE LoadKernel32dll()
{
    return LoadLibraryExW(L"kernel32", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
}

void InitializeCurrentProcessCpuCount()
{
    DWORD count;

    // If the configuration value has been set, it takes precedence. Otherwise, take into account
    // process affinity and CPU quota limit.

    const unsigned int MAX_PROCESSOR_COUNT = 0xffff;
    uint64_t configValue;

    if (g_pRhConfig->ReadConfigValue("PROCESSOR_COUNT", &configValue, true /* decimal */) &&
        0 < configValue && configValue <= MAX_PROCESSOR_COUNT)
    {
        count = (DWORD)configValue;
    }
    else
    {
        if (GCToOSInterface::CanEnableGCCPUGroups())
        {
            count = GCToOSInterface::GetTotalProcessorCount();
        }
        else
        {
            DWORD_PTR pmask, smask;

            if (!GetProcessAffinityMask(GetCurrentProcess(), &pmask, &smask))
            {
                count = 1;
            }
            else
            {
                count = 0;

                while (pmask)
                {
                    pmask &= (pmask - 1);
                    count++;
                }

                // GetProcessAffinityMask can return pmask=0 and smask=0 on systems with more
                // than 64 processors, which would leave us with a count of 0.  Since the GC
                // expects there to be at least one processor to run on (and thus at least one
                // heap), we'll return 64 here if count is 0, since there are likely a ton of
                // processors available in that case.
                if (count == 0)
                    count = 64;
            }
        }

        JOBOBJECT_CPU_RATE_CONTROL_INFORMATION cpuRateControl;

        if (QueryInformationJobObject(NULL, JobObjectCpuRateControlInformation, &cpuRateControl,
            sizeof(cpuRateControl), NULL))
        {
            const DWORD HardCapEnabled = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_HARD_CAP;
            const DWORD MinMaxRateEnabled = JOB_OBJECT_CPU_RATE_CONTROL_ENABLE | JOB_OBJECT_CPU_RATE_CONTROL_MIN_MAX_RATE;
            DWORD maxRate = 0;

            if ((cpuRateControl.ControlFlags & HardCapEnabled) == HardCapEnabled)
            {
                maxRate = cpuRateControl.CpuRate;
            }
            else if ((cpuRateControl.ControlFlags & MinMaxRateEnabled) == MinMaxRateEnabled)
            {
                maxRate = cpuRateControl.MaxRate;
            }

            // The rate is the percentage times 100
            const DWORD MAXIMUM_CPU_RATE = 10000;

            if (0 < maxRate && maxRate < MAXIMUM_CPU_RATE)
            {
                DWORD cpuLimit = (maxRate * GCToOSInterface::GetTotalProcessorCount() + MAXIMUM_CPU_RATE - 1) / MAXIMUM_CPU_RATE;
                if (cpuLimit < count)
                    count = cpuLimit;
            }
        }
    }

    _ASSERTE(count > 0);
    g_RhNumberOfProcessors = count;
}

// The Redhawk PAL must be initialized before any of its exports can be called. Returns true for a successful
// initialization and false on failure.
REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalInit()
{
    // We use fiber detach callbacks to run our thread shutdown code because the fiber detach
    // callback is made without the OS loader lock
    g_flsIndex = FlsAlloc(FiberDetachCallback);
    if (g_flsIndex == FLS_OUT_OF_INDEXES)
    {
        return false;
    }

    GCConfig::Initialize();

    if (!GCToOSInterface::Initialize())
    {
        return false;
    }

    InitializeCurrentProcessCpuCount();

    return true;
}

// Register the thread with OS to be notified when thread is about to be destroyed
// It fails fast if a different thread was already registered with the current fiber.
// Parameters:
//  thread        - thread to attach
REDHAWK_PALEXPORT void REDHAWK_PALAPI PalAttachThread(void* thread)
{
    void* threadFromCurrentFiber = FlsGetValue(g_flsIndex);

    if (threadFromCurrentFiber != NULL)
    {
        ASSERT_UNCONDITIONALLY("Multiple threads encountered from a single fiber");
        RhFailFast();
    }

    // Associate the current fiber with the current thread.  This makes the current fiber the thread's "home"
    // fiber.  This fiber is the only fiber allowed to execute managed code on this thread.  When this fiber
    // is destroyed, we consider the thread to be destroyed.
    FlsSetValue(g_flsIndex, thread);
}

// Detach thread from OS notifications.
// It fails fast if some other thread value was attached to the current fiber.
// Parameters:
//  thread        - thread to detach
// Return:
//  true if the thread was detached, false if there was no attached thread
REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalDetachThread(void* thread)
{
    ASSERT(g_flsIndex != FLS_OUT_OF_INDEXES);
    void* threadFromCurrentFiber = FlsGetValue(g_flsIndex);

    if (threadFromCurrentFiber == NULL)
    {
        // we've seen this thread, but not this fiber.  It must be a "foreign" fiber that was
        // borrowing this thread.
        return false;
    }

    if (threadFromCurrentFiber != thread)
    {
        ASSERT_UNCONDITIONALLY("Detaching a thread from the wrong fiber");
        RhFailFast();
    }

    FlsSetValue(g_flsIndex, NULL);
    return true;
}

extern "C" uint64_t PalQueryPerformanceCounter()
{
    return GCToOSInterface::QueryPerformanceCounter();
}

extern "C" uint64_t PalQueryPerformanceFrequency()
{
    return GCToOSInterface::QueryPerformanceFrequency();
}

extern "C" uint64_t PalGetCurrentOSThreadId()
{
    return GetCurrentThreadId();
}

#if !defined(USE_PORTABLE_HELPERS) && !defined(FEATURE_RX_THUNKS)
REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalAllocateThunksFromTemplate(_In_ HANDLE hTemplateModule, uint32_t templateRva, size_t templateSize, _Outptr_result_bytebuffer_(templateSize) void** newThunksOut)
{
#ifdef XBOX_ONE
    return E_NOTIMPL;
#else
    BOOL success = FALSE;
    HANDLE hMap = NULL, hFile = INVALID_HANDLE_VALUE;

    const WCHAR * wszModuleFileName = NULL;
    if (PalGetModuleFileName(&wszModuleFileName, hTemplateModule) == 0 || wszModuleFileName == NULL)
        return FALSE;

    hFile = CreateFileW(wszModuleFileName, GENERIC_READ | GENERIC_EXECUTE, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        goto cleanup;

    hMap = CreateFileMapping(hFile, NULL, SEC_IMAGE | PAGE_READONLY, 0, 0, NULL);
    if (hMap == NULL)
        goto cleanup;

    *newThunksOut = MapViewOfFile(hMap, 0, 0, templateRva, templateSize);
    success = ((*newThunksOut) != NULL);

cleanup:
    CloseHandle(hMap);
    CloseHandle(hFile);

    return success;
#endif
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalFreeThunksFromTemplate(_In_ void *pBaseAddress, size_t templateSize)
{
#ifdef XBOX_ONE
    return TRUE;
#else
    return UnmapViewOfFile(pBaseAddress);
#endif
}
#endif // !USE_PORTABLE_HELPERS && !FEATURE_RX_THUNKS

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalMarkThunksAsValidCallTargets(
    void *virtualAddress,
    int thunkSize,
    int thunksPerBlock,
    int thunkBlockSize,
    int thunkBlocksPerMapping)
{
    // We are using RWX pages so there is no need for this API for now.
    // Once we have a scenario for non-RWX pages we should be able to put the implementation here
    return TRUE;
}

REDHAWK_PALEXPORT uint32_t REDHAWK_PALAPI PalCompatibleWaitAny(UInt32_BOOL alertable, uint32_t timeout, uint32_t handleCount, HANDLE* pHandles, UInt32_BOOL allowReentrantWait)
{
    if (!allowReentrantWait)
    {
        return WaitForMultipleObjectsEx(handleCount, pHandles, FALSE, timeout, alertable);
    }
    else
    {
        DWORD index;
        SetLastError(ERROR_SUCCESS); // recommended by MSDN.
        HRESULT hr = CoWaitForMultipleHandles(alertable ? COWAIT_ALERTABLE : 0, timeout, handleCount, pHandles, &index);

        switch (hr)
        {
        case S_OK:
            return index;

        case RPC_S_CALLPENDING:
            return WAIT_TIMEOUT;

        default:
            SetLastError(HRESULT_CODE(hr));
            return WAIT_FAILED;
        }
    }
}

REDHAWK_PALEXPORT HANDLE PalCreateLowMemoryResourceNotification()
{
    return CreateMemoryResourceNotification(LowMemoryResourceNotification);
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalSleep(uint32_t milliseconds)
{
    return Sleep(milliseconds);
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalSwitchToThread()
{
    return SwitchToThread();
}

REDHAWK_PALEXPORT HANDLE REDHAWK_PALAPI PalCreateEventW(_In_opt_ LPSECURITY_ATTRIBUTES pEventAttributes, UInt32_BOOL manualReset, UInt32_BOOL initialState, _In_opt_z_ LPCWSTR pName)
{
    return CreateEventW(pEventAttributes, manualReset, initialState, pName);
}

#ifdef TARGET_X86

#define EXCEPTION_HIJACK  0xe0434f4e    // 0xe0000000 | 'COM'+1

PEXCEPTION_REGISTRATION_RECORD GetCurrentSEHRecord()
{
    return (PEXCEPTION_REGISTRATION_RECORD)__readfsdword(0);
}

VOID SetCurrentSEHRecord(EXCEPTION_REGISTRATION_RECORD *pSEH)
{
    __writefsdword(0, (DWORD)pSEH);
}

VOID PopSEHRecords(LPVOID pTargetSP)
{
    PEXCEPTION_REGISTRATION_RECORD currentContext = GetCurrentSEHRecord();
    // The last record in the chain is EXCEPTION_CHAIN_END which is defined as maxiumum
    // pointer value so it cannot satisfy the loop condition.
    while (currentContext < pTargetSP)
    {
        currentContext = currentContext->Next;
    }
    SetCurrentSEHRecord(currentContext);
}

// This will check who caused the exception.  If it was caused by the redirect function,
// the reason is to resume the thread back at the point it was redirected in the first
// place.  If the exception was not caused by the function, then it was caused by the call
// out to the I[GC|Debugger]ThreadControl client and we need to determine if it's an
// exception that we can just eat and let the runtime resume the thread, or if it's an
// uncatchable exception that we need to pass on to the runtime.
int RtlRestoreContextFallbackExceptionFilter(PEXCEPTION_POINTERS pExcepPtrs, CONTEXT *pCtx, Thread *pThread)
{
    if (pExcepPtrs->ExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Get the thread handle
    _ASSERTE(pExcepPtrs->ExceptionRecord->ExceptionCode == EXCEPTION_HIJACK);

    // Copy everything in the saved context record into the EH context.
    // Historically the EH context has enough space for every enabled context feature.
    // That may not hold for the future features beyond AVX, but this codepath is
    // supposed to be used only on OSes that do not have RtlRestoreContext.
    CONTEXT* pTarget = pExcepPtrs->ContextRecord;
    if (!CopyContext(pTarget, pCtx->ContextFlags, pCtx))
    {
        PalPrintFatalError("Could not set context record.\n");
        RhFailFast();
    }

    DWORD espValue = pCtx->Esp;

    // NOTE: Ugly, ugly workaround.
    // We need to resume the thread into the managed code where it was redirected,
    // and the corresponding ESP is below the current one.  But C++ expects that
    // on an EXCEPTION_CONTINUE_EXECUTION that the ESP will be above where it has
    // installed the SEH handler.  To solve this, we need to remove all handlers
    // that reside above the resumed ESP, but we must leave the OS-installed
    // handler at the top, so we grab the top SEH handler, call
    // PopSEHRecords which will remove all SEH handlers above the target ESP and
    // then link the OS handler back in with SetCurrentSEHRecord.

    // Get the special OS handler and save it until PopSEHRecords is done
    EXCEPTION_REGISTRATION_RECORD *pCurSEH = GetCurrentSEHRecord();

    // Unlink all records above the target resume ESP
    PopSEHRecords((LPVOID)(size_t)espValue);

    // Link the special OS handler back in to the top
    pCurSEH->Next = GetCurrentSEHRecord();

    // Register the special OS handler as the top handler with the OS
    SetCurrentSEHRecord(pCurSEH);

    // Resume execution at point where thread was originally redirected
    return EXCEPTION_CONTINUE_EXECUTION;
}

EXTERN_C VOID __cdecl RtlRestoreContextFallback(PCONTEXT ContextRecord, struct _EXCEPTION_RECORD* ExceptionRecord)
{
    Thread *pThread = ThreadStore::GetCurrentThread();

    // A counter to avoid a nasty case where an
    // up-stack filter throws another exception
    // causing our filter to be run again for
    // some unrelated exception.
    int filter_count = 0;

    __try
    {
        // Save the instruction pointer where we redirected last.  This does not race with the check
        // against this variable because the GC will not attempt to redirect the thread until the
        // instruction pointer of this thread is back in managed code.
        pThread->SetPendingRedirect(ContextRecord->Eip);
        RaiseException(EXCEPTION_HIJACK, 0, 0, NULL);
    }
    __except (++filter_count == 1
            ? RtlRestoreContextFallbackExceptionFilter(GetExceptionInformation(), ContextRecord, pThread)
            : EXCEPTION_CONTINUE_SEARCH)
    {
        _ASSERTE(!"Reached body of __except in RtlRestoreContextFallback");
    }
}

#endif // TARGET_X86

typedef BOOL(WINAPI* PINITIALIZECONTEXT2)(PVOID Buffer, DWORD ContextFlags, PCONTEXT* Context, PDWORD ContextLength, ULONG64 XStateCompactionMask);
PINITIALIZECONTEXT2 pfnInitializeContext2 = NULL;

#ifdef TARGET_X86
EXTERN_C VOID __cdecl RtlRestoreContextFallback(PCONTEXT ContextRecord, struct _EXCEPTION_RECORD* ExceptionRecord);
typedef VOID(__cdecl* PRTLRESTORECONTEXT)(PCONTEXT ContextRecord, struct _EXCEPTION_RECORD* ExceptionRecord);
PRTLRESTORECONTEXT pfnRtlRestoreContext = NULL;

#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_FLOATING_POINT |       \
                          CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS)
#else
#define CONTEXT_COMPLETE (CONTEXT_FULL | CONTEXT_DEBUG_REGISTERS)
#endif

REDHAWK_PALEXPORT CONTEXT* PalAllocateCompleteOSContext(_Out_ uint8_t** contextBuffer)
{
    CONTEXT* pOSContext = NULL;

#if (defined(TARGET_X86) || defined(TARGET_AMD64))
    DWORD context = CONTEXT_COMPLETE;

    if (pfnInitializeContext2 == NULL)
    {
        HMODULE hm = GetModuleHandleW(_T("kernel32.dll"));
        if (hm != NULL)
        {
            pfnInitializeContext2 = (PINITIALIZECONTEXT2)GetProcAddress(hm, "InitializeContext2");
        }
    }

#ifdef TARGET_X86
    if (pfnRtlRestoreContext == NULL)
    {
        HMODULE hm = GetModuleHandleW(_T("ntdll.dll"));
        pfnRtlRestoreContext = (PRTLRESTORECONTEXT)GetProcAddress(hm, "RtlRestoreContext");
        if (pfnRtlRestoreContext == NULL)
        {
            // Fallback to the internal implementation if OS doesn't provide one.
            pfnRtlRestoreContext = RtlRestoreContextFallback;
        }
    }
#endif //TARGET_X86

    // Determine if the processor supports AVX or AVX512 so we could
    // retrieve extended registers
    DWORD64 FeatureMask = GetEnabledXStateFeatures();
    if ((FeatureMask & (XSTATE_MASK_AVX | XSTATE_MASK_AVX512)) != 0)
    {
        context = context | CONTEXT_XSTATE;
    }

    // Retrieve contextSize by passing NULL for Buffer
    DWORD contextSize = 0;
    ULONG64 xStateCompactionMask = XSTATE_MASK_LEGACY | XSTATE_MASK_AVX | XSTATE_MASK_MPX | XSTATE_MASK_AVX512;
    // The initialize call should fail but return contextSize
    BOOL success = pfnInitializeContext2 ?
        pfnInitializeContext2(NULL, context, NULL, &contextSize, xStateCompactionMask) :
        InitializeContext(NULL, context, NULL, &contextSize);

    // Spec mentions that we may get a different error (it was observed on Windows7).
    // In such case the contextSize is undefined.
    if (success || GetLastError() != ERROR_INSUFFICIENT_BUFFER)
    {
        return NULL;
    }

    // So now allocate a buffer of that size and call InitializeContext again
    uint8_t* buffer = new (nothrow)uint8_t[contextSize];
    if (buffer != NULL)
    {
        success = pfnInitializeContext2 ?
            pfnInitializeContext2(buffer, context, &pOSContext, &contextSize, xStateCompactionMask):
            InitializeContext(buffer, context, &pOSContext, &contextSize);

        if (!success)
        {
            delete[] buffer;
            buffer = NULL;
        }
    }

    if (!success)
    {
        pOSContext = NULL;
    }

    *contextBuffer = buffer;

#else
    pOSContext = new (nothrow) CONTEXT;
    pOSContext->ContextFlags = CONTEXT_COMPLETE;
    *contextBuffer = NULL;
#endif

    return pOSContext;
}

REDHAWK_PALEXPORT _Success_(return) bool REDHAWK_PALAPI PalGetCompleteThreadContext(HANDLE hThread, _Out_ CONTEXT * pCtx)
{
    _ASSERTE((pCtx->ContextFlags & CONTEXT_COMPLETE) == CONTEXT_COMPLETE);

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Make sure that AVX feature mask is set, if supported. This should not normally fail.
    // The system silently ignores any feature specified in the FeatureMask which is not enabled on the processor.
    if (!SetXStateFeaturesMask(pCtx, XSTATE_MASK_AVX | XSTATE_MASK_AVX512))
    {
        _ASSERTE(!"Could not apply XSTATE_MASK_AVX | XSTATE_MASK_AVX512");
        return FALSE;
    }
#endif //defined(TARGET_X86) || defined(TARGET_AMD64)

    return GetThreadContext(hThread, pCtx);
}

REDHAWK_PALEXPORT _Success_(return) bool REDHAWK_PALAPI PalSetThreadContext(HANDLE hThread, _Out_ CONTEXT * pCtx)
{
    return SetThreadContext(hThread, pCtx);
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalRestoreContext(CONTEXT * pCtx)
{
    __asan_handle_no_return();
#ifdef TARGET_X86
    _ASSERTE(pfnRtlRestoreContext != NULL);
    pfnRtlRestoreContext(pCtx, NULL);
#else
    RtlRestoreContext(pCtx, NULL);
#endif //TARGET_X86
}

REDHAWK_PALIMPORT void REDHAWK_PALAPI PopulateControlSegmentRegisters(CONTEXT* pContext)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    CONTEXT ctx;

    RtlCaptureContext(&ctx);

    pContext->SegCs = ctx.SegCs;
    pContext->SegSs = ctx.SegSs;
#endif //defined(TARGET_X86) || defined(TARGET_AMD64)
}

static PalHijackCallback g_pHijackCallback;

#ifdef FEATURE_SPECIAL_USER_MODE_APC

// These declarations are for a new special user-mode APC feature introduced in Windows. These are not yet available in Windows
// SDK headers, so some names below are prefixed with "CLONE_" to avoid conflicts in the future. Once the prefixed declarations
// become available in the Windows SDK headers, the prefixed declarations below can be removed in favor of the SDK ones.

enum CLONE_QUEUE_USER_APC_FLAGS
{
    CLONE_QUEUE_USER_APC_FLAGS_NONE = 0x0,
    CLONE_QUEUE_USER_APC_FLAGS_SPECIAL_USER_APC = 0x1,
    CLONE_QUEUE_USER_APC_CALLBACK_DATA_CONTEXT = 0x10000
};

struct CLONE_APC_CALLBACK_DATA
{
    ULONG_PTR Parameter;
    PCONTEXT ContextRecord;
    ULONG_PTR Reserved0;
    ULONG_PTR Reserved1;
};
typedef CLONE_APC_CALLBACK_DATA* CLONE_PAPC_CALLBACK_DATA;

typedef BOOL (WINAPI* QueueUserAPC2Proc)(PAPCFUNC ApcRoutine, HANDLE Thread, ULONG_PTR Data, CLONE_QUEUE_USER_APC_FLAGS Flags);

#define QUEUE_USER_APC2_UNINITIALIZED (QueueUserAPC2Proc)-1
static QueueUserAPC2Proc g_pfnQueueUserAPC2Proc = QUEUE_USER_APC2_UNINITIALIZED;

static const CLONE_QUEUE_USER_APC_FLAGS SpecialUserModeApcWithContextFlags = (CLONE_QUEUE_USER_APC_FLAGS)
                                    (CLONE_QUEUE_USER_APC_FLAGS_SPECIAL_USER_APC |
                                     CLONE_QUEUE_USER_APC_CALLBACK_DATA_CONTEXT);

static void NTAPI ActivationHandler(ULONG_PTR parameter)
{
    CLONE_APC_CALLBACK_DATA* data = (CLONE_APC_CALLBACK_DATA*)parameter;
    g_pHijackCallback(data->ContextRecord, NULL);

    Thread* pThread = (Thread*)data->Parameter;
    pThread->SetActivationPending(false);
}
#endif

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalRegisterHijackCallback(_In_ PalHijackCallback callback)
{
    ASSERT(g_pHijackCallback == NULL);
    g_pHijackCallback = callback;

    return true;
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalHijack(HANDLE hThread, _In_opt_ void* pThreadToHijack)
{
    _ASSERTE(hThread != INVALID_HANDLE_VALUE);

#ifdef FEATURE_SPECIAL_USER_MODE_APC

    // initialize g_pfnQueueUserAPC2Proc on demand.
    // Note that only one thread at a time may perform suspension (guaranteed by the thread store lock)
    // so simple conditional assignment is ok.
    if (g_pfnQueueUserAPC2Proc == QUEUE_USER_APC2_UNINITIALIZED)
    {
        HMODULE hKernel32 = LoadKernel32dll();
#ifdef HOST_AMD64
        typedef BOOL (WINAPI *IsWow64Process2Proc)(HANDLE hProcess, USHORT *pProcessMachine, USHORT *pNativeMachine);

        IsWow64Process2Proc pfnIsWow64Process2Proc = (IsWow64Process2Proc)GetProcAddress(hKernel32, "IsWow64Process2");
        USHORT processMachine, hostMachine;
        if (pfnIsWow64Process2Proc != nullptr &&
            (*pfnIsWow64Process2Proc)(GetCurrentProcess(), &processMachine, &hostMachine) &&
            (hostMachine == IMAGE_FILE_MACHINE_ARM64) &&
            !IsWindowsVersionOrGreater(10, 0, 26100))
        {
            // Special user-mode APCs are broken on WOW64 processes (x64 running on Arm64 machine) with Windows older than 11.0.26100 (24H2)
            g_pfnQueueUserAPC2Proc = NULL;
        }
        else
#endif // HOST_AMD64
        {
            g_pfnQueueUserAPC2Proc = (QueueUserAPC2Proc)GetProcAddress(hKernel32, "QueueUserAPC2");
        }
    }

    if (g_pfnQueueUserAPC2Proc)
    {
        Thread* pThread = (Thread*)pThreadToHijack;

        // An APC can be interrupted by another one, do not queue more if one is pending.
        if (pThread->IsActivationPending())
        {
            return;
        }

        pThread->SetActivationPending(true);
        BOOL success = g_pfnQueueUserAPC2Proc(
            &ActivationHandler,
            hThread,
            (ULONG_PTR)pThreadToHijack,
            SpecialUserModeApcWithContextFlags);

        if (success)
        {
            return;
        }

        // queuing an APC failed
        pThread->SetActivationPending(false);

        DWORD lastError = GetLastError();
        if (lastError != ERROR_INVALID_PARAMETER && lastError != ERROR_NOT_SUPPORTED)
        {
            // An unexpected failure has happened. It is a concern.
            ASSERT_UNCONDITIONALLY("Failed to queue an APC for unusual reason.");

            // maybe it will work next time.
            return;
        }

        // the flags that we passed are not supported.
        // we will not try again
        g_pfnQueueUserAPC2Proc = NULL;
    }
#endif

    if (SuspendThread(hThread) == (DWORD)-1)
    {
        return;
    }

    CONTEXT win32ctx;
    win32ctx.ContextFlags = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_EXCEPTION_REQUEST;

    if (GetThreadContext(hThread, &win32ctx))
    {
        bool isSafeToRedirect = true;

#ifdef TARGET_X86
        // Workaround around WOW64 problems. Only do this workaround if a) this is x86, and b) the OS does
        // not support trap frame reporting.
        if ((win32ctx.ContextFlags & CONTEXT_EXCEPTION_REPORTING) == 0)
        {
            // This code fixes a race between GetThreadContext and NtContinue.  If we redirect managed code
            // at the same place twice in a row, we run the risk of reading a bogus CONTEXT when we redirect
            // the second time.  This leads to access violations on x86 machines.  To fix the problem, we
            // never redirect at the same instruction pointer that we redirected at on the previous GC.
            if (((Thread*)pThreadToHijack)->CheckPendingRedirect(win32ctx.Eip))
            {
                isSafeToRedirect = false;
            }
        }
#else
        // In some cases Windows will not set the CONTEXT_EXCEPTION_REPORTING flag if the thread is executing
        // in kernel mode (i.e. in the middle of a syscall or exception handling). Therefore, we should treat
        // the absence of the CONTEXT_EXCEPTION_REPORTING flag as an indication that it is not safe to
        // manipulate with the current state of the thread context.
        isSafeToRedirect = (win32ctx.ContextFlags & CONTEXT_EXCEPTION_REPORTING) != 0;
#endif

        // The CONTEXT_SERVICE_ACTIVE and CONTEXT_EXCEPTION_ACTIVE output flags indicate we suspended the thread
        // at a point where the kernel cannot guarantee a completely accurate context. We'll fail the request in
        // this case (which should force our caller to resume the thread and try again -- since this is a fairly
        // narrow window we're highly likely to succeed next time).
        if ((win32ctx.ContextFlags & CONTEXT_EXCEPTION_REPORTING) != 0 &&
            ((win32ctx.ContextFlags & (CONTEXT_SERVICE_ACTIVE | CONTEXT_EXCEPTION_ACTIVE)) != 0))
        {
            isSafeToRedirect = false;
        }

        if (isSafeToRedirect)
        {
            g_pHijackCallback(&win32ctx, pThreadToHijack);
        }
    }

    ResumeThread(hThread);
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartBackgroundWork(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext, BOOL highPriority)
{
    HANDLE hThread = CreateThread(
        NULL,
        0,
        (LPTHREAD_START_ROUTINE)callback,
        pCallbackContext,
        highPriority ? CREATE_SUSPENDED : 0,
        NULL);

    if (hThread == NULL)
        return false;

    if (highPriority)
    {
        SetThreadPriority(hThread, THREAD_PRIORITY_HIGHEST);
        ResumeThread(hThread);
    }

    CloseHandle(hThread);
    return true;
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartBackgroundGCThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
    return PalStartBackgroundWork(callback, pCallbackContext, FALSE);
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartFinalizerThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
    return PalStartBackgroundWork(callback, pCallbackContext, TRUE);
}

REDHAWK_PALEXPORT bool REDHAWK_PALAPI PalStartEventPipeHelperThread(_In_ BackgroundCallback callback, _In_opt_ void* pCallbackContext)
{
    return PalStartBackgroundWork(callback, pCallbackContext, FALSE);
}

REDHAWK_PALEXPORT HANDLE REDHAWK_PALAPI PalGetModuleHandleFromPointer(_In_ void* pointer)
{
    // The runtime is not designed to be unloadable today. Use GET_MODULE_HANDLE_EX_FLAG_PIN to prevent
    // the module from ever unloading.

    HMODULE module;
    if (!GetModuleHandleExW(
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN,
        (LPCWSTR)pointer,
        &module))
    {
        return NULL;
    }

    return (HANDLE)module;
}

REDHAWK_PALEXPORT void* REDHAWK_PALAPI PalAddVectoredExceptionHandler(uint32_t firstHandler, _In_ PVECTORED_EXCEPTION_HANDLER vectoredHandler)
{
    return AddVectoredExceptionHandler(firstHandler, vectoredHandler);
}

REDHAWK_PALEXPORT void PalPrintFatalError(const char* message)
{
    // Write the message using lowest-level OS API available. This is used to print the stack overflow
    // message, so there is not much that can be done here.
    DWORD dwBytesWritten;
    WriteFile(GetStdHandle(STD_ERROR_HANDLE), message, (DWORD)strlen(message), &dwBytesWritten, NULL);
}

REDHAWK_PALEXPORT char* PalCopyTCharAsChar(const TCHAR* toCopy)
{
    int len = ::WideCharToMultiByte(CP_UTF8, 0, toCopy, -1, nullptr, 0, nullptr, nullptr);
    if (len == 0)
        return nullptr;

    char* converted = new (nothrow) char[len];
    int written = ::WideCharToMultiByte(CP_UTF8, 0, toCopy, -1, converted, len, nullptr, nullptr);
    assert(len == written);
    return converted;
}

REDHAWK_PALEXPORT HANDLE PalLoadLibrary(const char* moduleName)
{
    assert(moduleName);
    size_t len = strlen(moduleName);
    wchar_t* moduleNameWide = new (nothrow)wchar_t[len + 1];
    if (moduleNameWide == nullptr)
    {
        return 0;
    }
    if (MultiByteToWideChar(CP_UTF8, 0, moduleName, -1, moduleNameWide, (int)(len + 1)) == 0)
    {
        return 0;
    }
    moduleNameWide[len] = '\0';
    
    HANDLE result = LoadLibraryExW(moduleNameWide, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    delete[] moduleNameWide;
    return result;
}

REDHAWK_PALEXPORT void* PalGetProcAddress(HANDLE module, const char* functionName)
{
    assert(module);
    assert(functionName);
    return GetProcAddress((HMODULE)module, functionName);
}

REDHAWK_PALEXPORT _Ret_maybenull_ _Post_writable_byte_size_(size) void* REDHAWK_PALAPI PalVirtualAlloc(uintptr_t size, uint32_t protect)
{
    return VirtualAlloc(NULL, size, MEM_RESERVE | MEM_COMMIT, protect);
}

REDHAWK_PALEXPORT void REDHAWK_PALAPI PalVirtualFree(_In_ void* pAddress, uintptr_t size)
{
    VirtualFree(pAddress, 0, MEM_RELEASE);
}

REDHAWK_PALEXPORT UInt32_BOOL REDHAWK_PALAPI PalVirtualProtect(_In_ void* pAddress, uintptr_t size, uint32_t protect)
{
    DWORD oldProtect;
    return VirtualProtect(pAddress, size, protect, &oldProtect);
}

REDHAWK_PALEXPORT void PalFlushInstructionCache(_In_ void* pAddress, size_t size)
{
    FlushInstructionCache(GetCurrentProcess(), pAddress, size);
}

