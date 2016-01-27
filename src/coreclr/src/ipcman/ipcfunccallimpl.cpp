// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
//*****************************************************************************
// File: IPCFuncCallImpl.cpp
//
// Implement support for a cross process function call. 
//
//*****************************************************************************

#include "stdafx.h"
#include "ipcfunccall.h"
#include "ipcshared.h"

#if defined(FEATURE_PERFMON) && defined(FEATURE_IPCMAN)

// #define ENABLE_TIMING

#ifdef ENABLE_TIMING
#include "timer.h"
CTimer g_time;
#endif // ENABLE_TIMING

//-----------------------------------------------------------------------------
// <TODO>@todo: This is very generic. However, If we want to support multiple 
// functions calls, we will have to decorate the event object names.</TODO>
//-----------------------------------------------------------------------------

#define NamePrexix L"Global\\CLR_"

// Name of sync objects
#define StartEnumEventName	NamePrexix L"PerfMon_StartEnumEvent"
#define DoneEnumEventName	NamePrexix L"PerfMon_DoneEnumEvent"
#define WrapMutexName		NamePrexix L"PerfMon_WrapMutex"

// Time the Source Caller is willing to wait for Handler to finish
// Note, a nefarious handler can at worst case make caller
// wait twice the delay below.
const DWORD START_ENUM_TIMEOUT = 500; // time out in milliseconds

//-----------------------------------------------------------------------------
// Wrap an unsafe call in a mutex to assure safety
// Biggest error issues are:
// 1. Timeout (probably handler doesn't exist)
// 2. Handler can be destroyed at any time.
//-----------------------------------------------------------------------------
IPCFuncCallSource::EError IPCFuncCallSource::DoThreadSafeCall()
{
    WRAPPER_NO_CONTRACT;

    DWORD dwDesiredAccess;
    DWORD dwErr;
    EError err = Ok;

#if defined(ENABLE_TIMING)
    g_time.Reset();
    g_time.Start();
#endif

    dwDesiredAccess = EVENT_MODIFY_STATE;

    HANDLE hStartEnum = NULL;
    HANDLE hDoneEnum = NULL;
    HANDLE hWrapCall = NULL;
    DWORD dwWaitRet;

    // Check if we have a handler (handler creates the events) and
    // abort if not.  Do this check asap to optimize the most common
    // case of no handler.
    hStartEnum = WszOpenEvent(dwDesiredAccess,	
                                FALSE,
                                StartEnumEventName);
    if (hStartEnum == NULL) 
    {
        dwErr = GetLastError();
        err = Fail_NoHandler;
        goto errExit;
    }

    hDoneEnum = WszOpenEvent(dwDesiredAccess,
                                FALSE,
                                DoneEnumEventName);
    if (hDoneEnum == NULL) 
    {
        dwErr = GetLastError();
        err = Fail_NoHandler;
        goto errExit;
    }

    // Need to create the mutex
    hWrapCall = WszCreateMutex(NULL, FALSE, WrapMutexName);
    if (hWrapCall == NULL)
    {
        dwErr = GetLastError();
        err = Fail_CreateMutex;
        goto errExit;
    }


// Wait for our turn	
    dwWaitRet = WaitForSingleObject(hWrapCall, START_ENUM_TIMEOUT);
    dwErr = GetLastError();
    switch(dwWaitRet) {
    case WAIT_OBJECT_0:
        // Good case. All other cases are errors and goto errExit.
        break;

    case WAIT_TIMEOUT:
        err = Fail_Timeout_Lock;
        goto errExit;
        break;
    default:
        err = Failed;
        goto errExit;
        break;
    }

    // Our turn: Make the function call
    {
        BOOL fSetOK = 0;

    // Reset the 'Done event' to make sure that Handler sets it after they start.
        fSetOK = ResetEvent(hDoneEnum);
        _ASSERTE(fSetOK);
        dwErr = GetLastError();

    // Signal Handler to execute callback   
        fSetOK = SetEvent(hStartEnum);
        _ASSERTE(fSetOK);
        dwErr = GetLastError();

    // Now wait for handler to finish.
        
        dwWaitRet = WaitForSingleObject(hDoneEnum, START_ENUM_TIMEOUT);
        dwErr = GetLastError();
        switch (dwWaitRet)
        {   
        case WAIT_OBJECT_0:
            break;
        case WAIT_TIMEOUT:
            err = Fail_Timeout_Call;
            break;      
        default:
            err = Failed;
            break;
        }
        

        BOOL fMutexOk;
        fMutexOk = ReleaseMutex(hWrapCall);
        _ASSERTE(fMutexOk);
        dwErr = GetLastError();

    } // End function call



errExit:
// Close all handles
    if (hStartEnum != NULL) 
    {
        CloseHandle(hStartEnum);
        hStartEnum = NULL;
        
    }
    if (hDoneEnum != NULL) 
    {
        CloseHandle(hDoneEnum);
        hDoneEnum = NULL;
    }
    if (hWrapCall != NULL) 
    {
        CloseHandle(hWrapCall);
        hWrapCall = NULL;
    }

#if defined(ENABLE_TIMING)
    g_time.End();
    DWORD dwTime = g_time.GetEllapsedMS();
#endif


    return err;

}


// Reset vars so we can be sure that Init was called
IPCFuncCallHandler::IPCFuncCallHandler()
{   
    m_hStartEnum    = NULL; // event to notify start call
    m_hDoneEnum     = NULL; // event to notify end call
    m_hAuxThread    = NULL; // thread to listen for m_hStartEnum
    m_pfnCallback   = NULL; // Callback handler
    m_pfnCleanupCallback = NULL; // Cleanup callback handler
    m_fShutdownAuxThread = FALSE;
    m_hShutdownThread = NULL;
    m_hCallbackModule = NULL; // module in which the aux thread's start function lives
}

IPCFuncCallHandler::~IPCFuncCallHandler()
{
    // If Terminate was not called then do so now. This should have been 
    // called from CloseCtrs perf counters API. But in Windows XP this order is
    // not guaranteed.
    TerminateFCHandler();
}

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning (disable: 6320) //We handle ALL exceptions so that the host process doesnt die
#endif

//-----------------------------------------------------------------------------
// Thread callback
//-----------------------------------------------------------------------------
DWORD WINAPI HandlerAuxThreadProc(
    LPVOID lpParameter   // thread data
)
{
    IPCFuncCallHandler * pHandler = (IPCFuncCallHandler *) lpParameter;

    struct Param
    {
        IPCFuncCallHandler * pHandler;
    } param;
    param.pHandler = pHandler;

    PAL_TRY(Param *, pParam, &param)
    {
        HANDLER_CALLBACK pfnCallback = pParam->pHandler->m_pfnCallback;

        DWORD dwErr = 0;
        DWORD dwWaitRet; 
    
        HANDLE lpHandles[] = {pParam->pHandler->m_hShutdownThread, pParam->pHandler->m_hStartEnum};
        DWORD dwHandleCount = 2;
    
        do {
            dwWaitRet = WaitForMultipleObjects(dwHandleCount, lpHandles, FALSE /*Wait Any*/, INFINITE);
            dwErr = GetLastError();
    
            // If we are in terminate mode then exit this helper thread.
            if (pParam->pHandler->m_fShutdownAuxThread)
                break;
            
            // Keep the 0th index for the terminate thread so that we never miss it
            // in case of multiple events. note that the ShutdownAuxThread flag above it purely 
            // to protect us against some bug in waitForMultipleObjects.
            if ((dwWaitRet-WAIT_OBJECT_0) == 0)
                break;

            // execute callback if wait succeeded
            if ((dwWaitRet-WAIT_OBJECT_0) == 1)
            {           
                (*pfnCallback)();
                            
                // reset manual event
                BOOL fResetOK;
                fResetOK = ResetEvent(pParam->pHandler->m_hStartEnum);
                _ASSERTE(fResetOK);                
                dwErr = GetLastError();

                BOOL fSetOK;
                fSetOK = SetEvent(pParam->pHandler->m_hDoneEnum);
                _ASSERTE(fSetOK);
                dwErr = GetLastError();
            }
        } while (dwWaitRet != WAIT_FAILED);       
    }
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        WCHAR wszMsg[128];
        swprintf_s(wszMsg, COUNTOF(wszMsg), L"HandlerAuxThreadProc caught exception %x", GetExceptionCode());
        ClrReportEvent(L".NET Runtime",
                         EVENTLOG_ERROR_TYPE,
                         0,
                         0,
                         NULL,
                         wszMsg);
    }
    PAL_ENDTRY


    pHandler->SafeCleanup();

    HMODULE hCallbackModule = pHandler->m_hCallbackModule;
    pHandler->m_hCallbackModule = NULL;

    pHandler->m_fShutdownAuxThread = FALSE;

    // Close the thread's handle and clear the shut down flag.  Note the order here is very tricky
    // to avoid a race.  We must set the shutdown flag first to ensure that once m_hAuxThread is set
    // to NULL no further modification happens to pHandler.
    HANDLE hThread = InterlockedExchangeT(&pHandler->m_hAuxThread, NULL);

    // If hThread was null then WaitForCompletion will close it.
    if (hThread != NULL)
        CloseHandle(hThread);

    FreeLibraryAndExitThread (hCallbackModule, 0);
    // Above call doesn't return

    return 0;
}
 
#ifdef _MSC_VER
#pragma warning (pop) //6320
#endif


//-----------------------------------------------------------------------------
// Receieves the call. This should be in a different process than the source
//-----------------------------------------------------------------------------
HRESULT IPCFuncCallHandler::InitFCHandler(HANDLER_CALLBACK pfnCallback, HANDLER_CALLBACK pfnCleanupCallback)
{
    // If the thread is still in the process of shutting down then 
    // we have to fail.
    if (!IsShutdownComplete())
    {
        _ASSERTE(!"shutdown should have completed before calling this function");
        return E_FAIL;
    }

    m_pfnCallback = pfnCallback;
    m_pfnCleanupCallback = pfnCleanupCallback;

    HRESULT hr = NOERROR;
    DWORD dwThreadId;
    DWORD dwErr = 0;
    DWORD dwDesiredAccess;
    DWORD dwRet = 0;
    HANDLE hToken = NULL;

    SetLastError(0);

    // Grab the SA
    DWORD dwPid = 0;
    SECURITY_ATTRIBUTES *pSA = NULL;

    dwDesiredAccess = EVENT_MODIFY_STATE | SYNCHRONIZE;

    dwPid = GetCurrentProcessId();
    hr = IPCShared::CreateWinNTDescriptor(dwPid, FALSE, &pSA, Event, eDescriptor_Public);

    if (FAILED(hr))
        goto errExit;;

    // try to open event first (another process may already have created one)
    m_hStartEnum = WszOpenEvent(dwDesiredAccess,
                              FALSE,
                              L"Global\\" StartEnumEventName);
    if (m_hStartEnum == NULL)
    {
        // Create the StartEnum Event
        m_hStartEnum = WszCreateEvent(pSA,
                                        TRUE, // manual event for multiple instances of corperfmonext.dll
                                        FALSE,
                                        StartEnumEventName);
    }
    
    if (m_hStartEnum == NULL)
    {
        dwErr = GetLastError();
        hr = HRESULT_FROM_WIN32(dwErr); 
        goto errExit;
    }

    // try to open event first (another process may already have created one)
    m_hDoneEnum = WszOpenEvent(dwDesiredAccess,
                             FALSE,
                             L"Global\\" DoneEnumEventName);

    if (m_hDoneEnum == NULL)
    {
        // Create the EndEnumEvent
        m_hDoneEnum = WszCreateEvent(pSA,
                                        TRUE, // manual event for multiple instances of corperfmonext.dll
                                        FALSE,
                                        DoneEnumEventName);
    }
    if (m_hDoneEnum == NULL)
    {
        dwErr = GetLastError();
        hr = HRESULT_FROM_WIN32(dwErr); 
        goto errExit;
    }

    // Create the ShutdownThread Event
    m_hShutdownThread = WszCreateEvent(pSA,
                                       TRUE, /* Manual Reset */
                                       FALSE, /* Initial state not signalled */
                                       NULL);
    
    dwErr = GetLastError();
    if (m_hShutdownThread == NULL)
    {
        hr = HRESULT_FROM_WIN32(dwErr); 
        goto errExit;
    }

    BOOL bSuccess = FALSE;

    // Get current thread token with duplicate and impersonation access
    // Will use this token for polling thread impersonation if current
    // thread is impersonating
    bSuccess = OpenThreadToken(
        GetCurrentThread(), 
        TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE, 
        TRUE,
        &hToken
        );

    dwErr = GetLastError();
    // token won't exist if running local becase we are not impersonating
    if (FALSE == bSuccess && ERROR_NO_TOKEN != dwErr)
    {
        hr = HRESULT_FROM_WIN32(dwErr);
        goto errExit;
    }

    // at this point, we should either have a valid token or we failed
    // to get the token because one does not exist on this thread
    _ASSERTE(NULL != hToken || ERROR_NO_TOKEN == dwErr);

    // The thread that we are about to create should always 
    // find the code in memory. So we take a ref on the DLL. 
    // and do a free library at the end of the thread's start function
    m_hCallbackModule = WszLoadLibrary (L"CorPerfmonExt.dll");

    dwErr = GetLastError();
    if (m_hCallbackModule == NULL)
    {
        hr = HRESULT_FROM_WIN32(dwErr); 
        goto errExit;
    }

    // Create thread suspended so we can set impersonation token
    m_hAuxThread = CreateThread(
        NULL,
        0,
        HandlerAuxThreadProc,
        this,
        CREATE_SUSPENDED,
        &dwThreadId);

    dwErr = GetLastError();
    if (m_hAuxThread.Load() == NULL)
    {
        hr = HRESULT_FROM_WIN32(dwErr);	

        // In case of an error free this library here otherwise
        // the thread's exit would take care of it.
        if (m_hCallbackModule)
            FreeLibrary (m_hCallbackModule);
        goto errExit;
    }

    // If we got a token for the current thread,
    // set token on new thread
    if (NULL != hToken)
    {
        bSuccess = SetThreadToken((PHANDLE)m_hAuxThread.GetPointer(), hToken);

        dwErr = GetLastError();
        if (FALSE == bSuccess)
        {
            hr = HRESULT_FROM_WIN32(dwErr);
            goto errExit;
        }
    }

    // Resume the newly created thread
    dwRet = ResumeThread(m_hAuxThread);

    dwErr = GetLastError();
    if (dwRet == (DWORD)(-1))
    {
        hr = HRESULT_FROM_WIN32(dwErr);
        goto errExit;
    }

    _ASSERTE(1 == dwRet);

errExit:
    if (NULL != hToken)
    {
        CloseHandle(hToken);
    }

    if (!SUCCEEDED(hr)) 
    {
        TerminateFCHandler();
    }

    if (pSA != NULL)
    {
        IPCShared::DestroySecurityAttributes( pSA );
    }
    return hr;
 
}

//-----------------------------------------------------------------------------
// Close all our handles
//-----------------------------------------------------------------------------
void IPCFuncCallHandler::SafeCleanup()
{
    // Call the cleanup callback

    if (m_pfnCleanupCallback != NULL)
    {
        struct Param
        {
            IPCFuncCallHandler * pHandler;
        } param;
        param.pHandler = this;

        PAL_TRY(Param *, pParam, &param)
        {
            HANDLER_CALLBACK pfnCleanupCallback = pParam->pHandler->m_pfnCleanupCallback;
        
            (*pfnCleanupCallback)();
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            WCHAR wszMsg[128];
            swprintf_s(wszMsg, COUNTOF(wszMsg), L"HandlerAuxThreadProc caught exception %x", GetExceptionCode());
            ClrReportEvent(L".NET Runtime",
                             EVENTLOG_ERROR_TYPE,
                             0,
                             0,
                             NULL,
                             wszMsg);
        }
        PAL_ENDTRY
    }


    // Release all the handles

    if (m_hStartEnum != NULL)
    {
        CloseHandle(m_hStartEnum);
        m_hStartEnum = NULL;
    }

    if (m_hDoneEnum != NULL)
    {
        CloseHandle(m_hDoneEnum);
        m_hDoneEnum = NULL;
    }

    if (m_hShutdownThread != NULL)
    {
        CloseHandle(m_hShutdownThread);
        m_hShutdownThread = NULL;
    }

    m_pfnCallback   = NULL;
    m_pfnCleanupCallback = NULL;
}

void IPCFuncCallHandler::TerminateFCHandler()
{
    // If the thread is in the process of shutting down then 
    // there is nothing to do
    if (m_fShutdownAuxThread)
    {
        return;
    }

    // If this IPCFuncCallHandler has not been initialized yet 
    // then there is nothing to do
    if ((m_hStartEnum == NULL) &&
        (m_hDoneEnum == NULL) &&
        (m_hAuxThread.Load() == NULL) &&
        (m_pfnCallback == NULL))
    {
        return;
    }

    if(m_hAuxThread.Load() != NULL)
    {
        // Always resume the thread to make sure it is not suspended
        if (ResumeThread(m_hAuxThread) == (DWORD)(-1))
        {
            _ASSERTE (!"TerminateFCHandler: ResumeThread(m_hAuxThread) failed");
        }

        // First make sure that we make the aux thread gracefully exit
        m_fShutdownAuxThread = TRUE;

        // Hope that this set event makes the thread quit.
        if (!SetEvent (m_hShutdownThread))
        {
            _ASSERTE (!"TerminateFCHandler: SetEvent(m_hShutdownThread) failed");
        }
    }
    else
    {
        // We failed during InitFCHandler before creating the auxilliary thread
        SafeCleanup();
        
    }

    // The aux thread is responsible for cleanup. When it is finished cleaning
    // up it will set m_fShutdownAuxThread to FALSE.
}

BOOL IPCFuncCallHandler::IsShutdownComplete()
{
    return m_hAuxThread.Load() == NULL;
}

void IPCFuncCallHandler::WaitForShutdown()
{
    // Check to see if the thread handle is null.  If it is then the thread has shut down,
    // otherwise we will wait for the thread to exit.
    HANDLE hThread = InterlockedExchangeT(&m_hAuxThread, NULL);

    if (hThread != NULL)
    {
        // Otherwise wait for the thread to complete and we close its handle.
        DWORD result = WaitForSingleObject(hThread, INFINITE);
        _ASSERTE(result == WAIT_OBJECT_0);

        CloseHandle(hThread);
    }
}
#else  // !FEATURE_PERFMON || !FEATURE_IPCMAN


// Telesto stubs

//-----------------------------------------------------------------------------
// Wrap an unsafe call in a mutex to assure safety
// Biggest error issues are:
// 1. Timeout (probably handler doesn't exist)
// 2. Handler can be destroyed at any time.
//-----------------------------------------------------------------------------
IPCFuncCallSource::EError IPCFuncCallSource::DoThreadSafeCall()
{
    return Ok;
}

#endif // FEATURE_PERFMON && FEATURE_IPCMAN
