// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CEEMAIN.H
// 

// 
//

// CEEMAIN.H defines the entrypoints into the Virtual Execution Engine and
// gets the load/run process going.
// ===========================================================================

#ifndef CEEMain_H
#define CEEMain_H

#include <windef.h> // for HFILE, HANDLE, HMODULE

class EEDbgInterfaceImpl;

// Ensure the EE is started up.
HRESULT EnsureEEStarted(COINITIEE flags);

// Wrapper around EnsureEEStarted which also sets startup mode.
HRESULT InitializeEE(COINITIEE flags);

// Enum to control what happens at the end of EE shutdown. There are two options:
// 1. Call ::ExitProcess to cause the process to terminate gracefully. This is how
//    shutdown normally ends. "Shutdown" methods that take this action as an argument
//    do not return when SCA_ExitProcessWhenShutdownComplete is passed.
//
// 2. Return after performing all shutdown processing. This is a special case used
//    by a shutdown initiated via the Shim, and is used to ensure that all runtimes
//    loaded SxS are shutdown gracefully. "Shutdown" methods that take this action
//    as an argument return when SCA_ReturnWhenShutdownComplete is passed.
enum ShutdownCompleteAction
{
    SCA_ExitProcessWhenShutdownComplete,
    SCA_ReturnWhenShutdownComplete
};

// Force shutdown of the EE
void ForceEEShutdown(ShutdownCompleteAction sca = SCA_ExitProcessWhenShutdownComplete);
void InnerCoEEShutDownCOM();

// We have an internal class that can be used to expose EE functionality to other CLR
// DLLs, via the deliberately obscure IEE DLL exports from the shim and the EE
// NOTE:  This class must not ever contain any instance variables.  The reason for
//        this is that the IEE function (corhost.cpp) relies on the fact that you
//        may initialize the object more than once without ill effects.  If you
//        change this class so that this condition is violated, you must rewrite
//        how the g_pCEE and related variables are initialized.
class CExecutionEngine : public IExecutionEngine, public IEEMemoryManager
{
    friend struct _DacGlobals;

    //***************************************************************************
    // public API:
    //***************************************************************************
public:

    // Notification of a DLL_THREAD_DETACH or a Thread Terminate.
    static void ThreadDetaching(void **pTlsData);

    // Delete on TLS block
    static void DeleteTLS(void **pTlsData);

    static void **CheckThreadState(DWORD slot, BOOL force = TRUE);
    static void **CheckThreadStateNoCreate(DWORD slot
#ifdef _DEBUG
                                           , BOOL fForDestruction = FALSE
#endif // _DEBUG
                                           );

    // Setup FLS simulation block, including ClrDebugState and StressLog.
    static void SetupTLSForThread(Thread *pThread);

    static LPVOID* GetTlsData();
    static BOOL SetTlsData (void** ppTlsInfo);

    //***************************************************************************
    // private implementation:
    //***************************************************************************
private:
    static PTLS_CALLBACK_FUNCTION Callbacks[MAX_PREDEFINED_TLS_SLOT];

    //***************************************************************************
    // IUnknown methods
    //***************************************************************************

    HRESULT STDMETHODCALLTYPE QueryInterface(
            REFIID id,
            void **pInterface);

    ULONG STDMETHODCALLTYPE AddRef();

    ULONG STDMETHODCALLTYPE Release();

    //***************************************************************************
    // IExecutionEngine methods for TLS
    //***************************************************************************

    // Associate a callback for cleanup with a TLS slot
    VOID  STDMETHODCALLTYPE TLS_AssociateCallback(
            DWORD slot,
            PTLS_CALLBACK_FUNCTION callback);

    // Get the TLS block for fast Get/Set operations
    LPVOID* STDMETHODCALLTYPE TLS_GetDataBlock();

    // Get the value at a slot
    LPVOID STDMETHODCALLTYPE TLS_GetValue(DWORD slot);

    // Get the value at a slot, return FALSE if TLS info block doesn't exist
    BOOL STDMETHODCALLTYPE TLS_CheckValue(DWORD slot, LPVOID * pValue);

    // Set the value at a slot
    VOID STDMETHODCALLTYPE TLS_SetValue(DWORD slot, LPVOID pData);

    // Free TLS memory block and make callback
    VOID STDMETHODCALLTYPE TLS_ThreadDetaching();
    
    //***************************************************************************
    // IExecutionEngine methods for locking
    //***************************************************************************

    CRITSEC_COOKIE STDMETHODCALLTYPE CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags);

    void STDMETHODCALLTYPE DestroyLock(CRITSEC_COOKIE lock);

    void STDMETHODCALLTYPE AcquireLock(CRITSEC_COOKIE lock);

    void STDMETHODCALLTYPE ReleaseLock(CRITSEC_COOKIE lock);

    EVENT_COOKIE STDMETHODCALLTYPE CreateAutoEvent(BOOL bInitialState);
    EVENT_COOKIE STDMETHODCALLTYPE CreateManualEvent(BOOL bInitialState);
    void STDMETHODCALLTYPE CloseEvent(EVENT_COOKIE event);
    BOOL STDMETHODCALLTYPE ClrSetEvent(EVENT_COOKIE event);
    BOOL STDMETHODCALLTYPE ClrResetEvent(EVENT_COOKIE event);
    DWORD STDMETHODCALLTYPE WaitForEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable);
    DWORD STDMETHODCALLTYPE WaitForSingleObject(HANDLE handle, DWORD dwMilliseconds);

    SEMAPHORE_COOKIE STDMETHODCALLTYPE ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax);
    void STDMETHODCALLTYPE ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore);
    DWORD STDMETHODCALLTYPE ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable);
    BOOL STDMETHODCALLTYPE ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG *lpPreviousCount);

    MUTEX_COOKIE STDMETHODCALLTYPE ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                  BOOL bInitialOwner,
                                                  LPCTSTR lpName);
    void STDMETHODCALLTYPE ClrCloseMutex(MUTEX_COOKIE mutex);
    BOOL STDMETHODCALLTYPE ClrReleaseMutex(MUTEX_COOKIE mutex);
    DWORD STDMETHODCALLTYPE ClrWaitForMutex(MUTEX_COOKIE mutex,
                                            DWORD dwMilliseconds,
                                            BOOL bAlertable);

    DWORD STDMETHODCALLTYPE ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable);

    BOOL STDMETHODCALLTYPE ClrAllocationDisallowed();

    void STDMETHODCALLTYPE GetLastThrownObjectExceptionFromThread(void **ppvException);

    //***************************************************************************
    // IEEMemoryManager methods for locking
    //***************************************************************************
    LPVOID STDMETHODCALLTYPE ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect);
    BOOL STDMETHODCALLTYPE ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType);
    SIZE_T STDMETHODCALLTYPE ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength);
    BOOL STDMETHODCALLTYPE ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect);
    HANDLE STDMETHODCALLTYPE ClrGetProcessHeap();
    HANDLE STDMETHODCALLTYPE ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize);
    BOOL STDMETHODCALLTYPE ClrHeapDestroy(HANDLE hHeap);
    LPVOID STDMETHODCALLTYPE ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, SIZE_T dwBytes);
    BOOL STDMETHODCALLTYPE ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem);
    BOOL STDMETHODCALLTYPE ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem);
    HANDLE STDMETHODCALLTYPE ClrGetProcessExecutableHeap();
    
};

void SetLatchedExitCode (INT32 code);
INT32 GetLatchedExitCode (void);

// Tells whether the garbage collector is fully initialized
// Stronger than IsGCHeapInitialized
BOOL IsGarbageCollectorFullyInitialized();


#endif
