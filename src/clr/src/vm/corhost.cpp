// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// CorHost.cpp
//
// Implementation for the meta data dispenser code.
//

//*****************************************************************************

#include "common.h"

#include "mscoree.h"
#include "corhost.h"
#include "excep.h"
#include "threads.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "ceemain.h"
#include "rwlock.h"
#include "hosting.h"
#include "eepolicy.h"
#include "clrex.h"
#ifdef FEATURE_IPCMAN
#include "ipcmanagerinterface.h"
#endif // FEATURE_IPCMAN
#include "comcallablewrapper.h"
#include "hostexecutioncontext.h"
#include "invokeutil.h"
#include "appdomain.inl"
#include "vars.hpp"
#include "comdelegate.h"
#include "dllimportcallback.h"
#include "eventtrace.h"

#include "win32threadpool.h"
#include "eventtrace.h"
#include "finalizerthread.h"
#include "threadsuspend.h"

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "stringarraylist.h"

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#endif

#if defined(FEATURE_APPX_BINDER)
#include "clrprivbinderappx.h"
#include "clrprivtypecachewinrt.h"
#endif

GVAL_IMPL_INIT(DWORD, g_fHostConfig, 0);

#ifdef FEATURE_IMPLICIT_TLS
#ifndef __llvm__
EXTERN_C __declspec(thread) ThreadLocalInfo gCurrentThreadInfo;
#else // !__llvm__
EXTERN_C __thread ThreadLocalInfo gCurrentThreadInfo;
#endif // !__llvm__
#ifndef FEATURE_PAL
EXTERN_C UINT32 _tls_index;
#else // FEATURE_PAL
UINT32 _tls_index = 0;
#endif // FEATURE_PAL
SVAL_IMPL_INIT(DWORD, CExecutionEngine, TlsIndex, _tls_index);
#else
SVAL_IMPL_INIT(DWORD, CExecutionEngine, TlsIndex, TLS_OUT_OF_INDEXES);
#endif


#if defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)
SVAL_IMPL_INIT(ECustomDumpFlavor, CCLRErrorReportingManager, g_ECustomDumpFlavor, DUMP_FLAVOR_Default);
#endif

#ifndef DACCESS_COMPILE

extern void STDMETHODCALLTYPE EEShutDown(BOOL fIsDllUnloading);
extern HRESULT STDMETHODCALLTYPE CoInitializeEE(DWORD fFlags);
extern void PrintToStdOutA(const char *pszString);
extern void PrintToStdOutW(const WCHAR *pwzString);
extern BOOL g_fEEHostedStartup;

INT64 g_PauseTime;         // Total time in millisecond the CLR has been paused
Volatile<BOOL> g_IsPaused;  // True if the runtime is paused (FAS)
CLREventStatic g_ClrResumeEvent; // Event that is fired at FAS Resuming 
#ifndef FEATURE_CORECLR
CLREventStatic g_PauseCompletedEvent; // Set when Pause has completed its work on another thread.
#endif

#if defined(FEATURE_CORECLR)
extern BYTE g_rbTestKeyBuffer[];
#endif

#if !defined(FEATURE_CORECLR)
//******************************************************************************
// <TODO>TODO: ICorThreadpool: Move this into a separate file CorThreadpool.cpp
// after the move to VBL </TODO>
//******************************************************************************

HRESULT STDMETHODCALLTYPE  CorThreadpool::CorRegisterWaitForSingleObject(PHANDLE phNewWaitObject,
                                                                         HANDLE hWaitObject,
                                                                         WAITORTIMERCALLBACK Callback,
                                                                         PVOID Context,
                                                                         ULONG timeout,
                                                                         BOOL  executeOnlyOnce,
                                                                         BOOL* pResult)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;

    BEGIN_ENTRYPOINT_NOTHROW;

    ULONG flag = executeOnlyOnce ? WAIT_SINGLE_EXECUTION : 0;
    *pResult = FALSE;
    EX_TRY
    {
        *pResult = ThreadpoolMgr::RegisterWaitForSingleObject(phNewWaitObject,
                                                  hWaitObject,
                                                  Callback,
                                                  Context,
                                                  timeout,
                                                  flag);

        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


HRESULT STDMETHODCALLTYPE  CorThreadpool::CorBindIoCompletionCallback(HANDLE fileHandle,
                                                                      LPOVERLAPPED_COMPLETION_ROUTINE callback)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    BOOL ret = FALSE;
    DWORD errCode = 0;

    EX_TRY
    {
        ret = ThreadpoolMgr::BindIoCompletionCallback(fileHandle,callback,0, errCode);
        hr = (ret ? S_OK : HRESULT_FROM_WIN32(errCode));
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


HRESULT STDMETHODCALLTYPE  CorThreadpool::CorUnregisterWait(HANDLE hWaitObject,
                                                            HANDLE CompletionEvent,
                                                            BOOL* pResult)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;

    BEGIN_ENTRYPOINT_NOTHROW;

    *pResult = FALSE;
    EX_TRY
    {

        *pResult = ThreadpoolMgr::UnregisterWaitEx(hWaitObject,CompletionEvent);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;

    return hr;

}

HRESULT STDMETHODCALLTYPE  CorThreadpool::CorQueueUserWorkItem(LPTHREAD_START_ROUTINE Function,
                                                               PVOID Context,BOOL executeOnlyOnce,
                                                               BOOL* pResult )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pResult = FALSE;
    EX_TRY
    {
        *pResult = ThreadpoolMgr::QueueUserWorkItem(Function,Context,QUEUE_ONLY);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

HRESULT STDMETHODCALLTYPE  CorThreadpool::CorCallOrQueueUserWorkItem(LPTHREAD_START_ROUTINE Function,
                                                                     PVOID Context,
                                                                     BOOL* pResult )
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;
    *pResult = FALSE;
    EX_TRY
    {
        *pResult = ThreadpoolMgr::QueueUserWorkItem(Function,Context,CALL_OR_QUEUE);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT STDMETHODCALLTYPE  CorThreadpool::CorCreateTimer(PHANDLE phNewTimer,
                                                         WAITORTIMERCALLBACK Callback,
                                                         PVOID Parameter,
                                                         DWORD DueTime,
                                                         DWORD Period,
                                                         BOOL* pResult)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pResult = FALSE;
    EX_TRY
    {
        *pResult = ThreadpoolMgr::CreateTimerQueueTimer(phNewTimer,Callback,Parameter,DueTime,Period,0);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT STDMETHODCALLTYPE  CorThreadpool::CorDeleteTimer(HANDLE Timer, HANDLE CompletionEvent, BOOL* pResult)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pResult = FALSE;
    EX_TRY
    {
        *pResult = ThreadpoolMgr::DeleteTimerQueueTimer(Timer,CompletionEvent);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

HRESULT STDMETHODCALLTYPE  CorThreadpool::CorChangeTimer(HANDLE Timer,
                                                         ULONG DueTime,
                                                         ULONG Period,
                                                         BOOL* pResult)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    *pResult = FALSE;
    EX_TRY
    {
        //CONTRACT_VIOLATION(ThrowsViolation);
        *pResult = ThreadpoolMgr::ChangeTimerQueueTimer(Timer,DueTime,Period);
        hr = (*pResult ? S_OK : HRESULT_FROM_GetLastError());
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT STDMETHODCALLTYPE CorThreadpool::CorSetMaxThreads(DWORD MaxWorkerThreads,
                                                          DWORD MaxIOCompletionThreads)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    BOOL result = FALSE;
    EX_TRY
    {
        result = ThreadpoolMgr::SetMaxThreads(MaxWorkerThreads, MaxIOCompletionThreads);
        hr = (result ? S_OK : E_FAIL);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

HRESULT STDMETHODCALLTYPE CorThreadpool::CorGetMaxThreads(DWORD *MaxWorkerThreads,
                                                          DWORD *MaxIOCompletionThreads)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    BOOL result = FALSE;
    EX_TRY
    {
        result = ThreadpoolMgr::GetMaxThreads(MaxWorkerThreads, MaxIOCompletionThreads);
        hr = (result ? S_OK : E_FAIL);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

HRESULT STDMETHODCALLTYPE CorThreadpool::CorGetAvailableThreads(DWORD *AvailableWorkerThreads,
                                                                DWORD *AvailableIOCompletionThreads)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    BEGIN_ENTRYPOINT_NOTHROW;

    BOOL result = FALSE;
    EX_TRY
    {
        result = ThreadpoolMgr::GetAvailableThreads(AvailableWorkerThreads, AvailableIOCompletionThreads);
        hr = (result ? S_OK : E_FAIL);
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // !defined(FEATURE_CORECLR)
//***************************************************************************

ULONG CorRuntimeHostBase::m_Version = 0;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
static CCLRDebugManager s_CLRDebugManager;
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#if defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)
CCLRErrorReportingManager g_CLRErrorReportingManager;
#endif // defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)

#ifdef FEATURE_IPCMAN
static CCLRSecurityAttributeManager s_CLRSecurityAttributeManager;
#endif // FEATURE_IPCMAN

#endif // !DAC

typedef DPTR(CONNID)   PTR_CONNID;

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
// Hash table to keep track <connection, name> for SQL fiber support
class ConnectionNameTable : CHashTableAndData<CNewDataNoThrow>
{
    friend class CCLRDebugManager;
public:

    // Key to match is connection ID.
    // Returns true if the given HASHENTRY has the same key as the requested key.
    BOOL Cmp(SIZE_T requestedKey, const HASHENTRY * pEntry)
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;

        CONNID keyRequested = (CONNID)requestedKey;
        CONNID keySearch = dac_cast<PTR_ConnectionNameHashEntry>(pEntry)->m_dwConnectionId;
        return  keyRequested != keySearch;
    }

    // Hash function
    ULONG Hash(CONNID dwConnectionId)
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;

        return (ULONG)(dwConnectionId);
    }

#ifndef DACCESS_COMPILE
    // constructor
    ConnectionNameTable(
        ULONG      iBuckets) :         // # of chains we are hashing into.
        CHashTableAndData<CNewDataNoThrow>(iBuckets)
    {LIMITED_METHOD_CONTRACT;}

    // destructor
    ~ConnectionNameTable()
    {
        CONTRACTL
        {
            if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
            NOTHROW;
        }
        CONTRACTL_END;
        HASHFIND hashFind;
        ConnectionNameHashEntry *pNameEntry;

        pNameEntry = (ConnectionNameHashEntry *)FindFirstEntry(&hashFind);
        while (pNameEntry != NULL)
        {
            if (pNameEntry->m_pwzName)
            {
                delete pNameEntry->m_pwzName;
                pNameEntry->m_pwzName = NULL;
            }

            if (pNameEntry->m_CLRTaskCount != 0)
            {
                _ASSERTE(pNameEntry->m_ppCLRTaskArray != NULL);
                for (UINT i = 0; i < pNameEntry->m_CLRTaskCount; i++)
                {
                    pNameEntry->m_ppCLRTaskArray[i]->Release();
                }
                delete [] pNameEntry->m_ppCLRTaskArray;
                pNameEntry->m_ppCLRTaskArray = NULL;
                pNameEntry->m_CLRTaskCount = 0;
            }
            pNameEntry = (ConnectionNameHashEntry *)FindNextEntry(&hashFind);
        }
    }

    // Add a new connection into hash table.
    // This function does not throw but return NULL when memory allocation fails.
    ConnectionNameHashEntry *AddConnection(
        CONNID  dwConnectionId,
        __in_z WCHAR   *pwzName)  // We should review this in the future.  This API is
                                                 // public and callable by a host.  This SAL annotation
                                                 // is the best we can do now.
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            NOTHROW;
        }
        CONTRACTL_END;

        ULONG iHash = Hash(dwConnectionId);

        size_t len = wcslen(pwzName) + 1;
        WCHAR *pConnName = new (nothrow) WCHAR[len];
        if (pConnName == NULL)
            return NULL;

        ConnectionNameHashEntry *pRecord = (ConnectionNameHashEntry *)Add(iHash);
        if (pRecord)
        {
            pRecord->m_dwConnectionId = dwConnectionId;
            pRecord->m_pwzName = pConnName;
            wcsncpy_s(pRecord->m_pwzName, len, pwzName, len);
            pRecord->m_CLRTaskCount = 0;
            pRecord->m_ppCLRTaskArray = NULL;
        }
        else
        {
            if (pConnName)
                delete [] pConnName;
        }

        return pRecord;
    }

    // Delete a hash entry given a connection id
    void DeleteConnection(CONNID dwConnectionId)
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            NOTHROW;
        }
        CONTRACTL_END;

        ULONG  iHash;
        iHash = Hash(dwConnectionId);
        ConnectionNameHashEntry * pRecord = 
            reinterpret_cast<ConnectionNameHashEntry *>(Find(iHash, (SIZE_T)dwConnectionId));
        if (pRecord == NULL)
        {
            return;
        }

        _ASSERTE(pRecord->m_CLRTaskCount == 0 && pRecord->m_ppCLRTaskArray == NULL);
        if (pRecord->m_pwzName)
        {
            delete pRecord->m_pwzName;
            pRecord->m_pwzName = NULL;
        }
        Delete(iHash, (HASHENTRY *)pRecord);
    }

    // return NULL if the given connection id cannot be found.
    ConnectionNameHashEntry *FindConnection(CONNID dwConnectionId)
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            NOTHROW;
        }
        CONTRACTL_END;

        ULONG  iHash;
        iHash = Hash(dwConnectionId);
        return reinterpret_cast<ConnectionNameHashEntry *>(Find(iHash, (SIZE_T)dwConnectionId));
    }
#endif // !DAC
};
#endif //FEATURE_INCLUDE_ALL_INTERFACES


// Keep track connection id and name
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
SPTR_IMPL(ConnectionNameTable, CCLRDebugManager, m_pConnectionNameHash);
CrstStatic CCLRDebugManager::m_lockConnectionNameTable;
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#ifndef DACCESS_COMPILE


#if !defined(FEATURE_CORECLR) // simple hosting
//*****************************************************************************
// ICorRuntimeHost
//*****************************************************************************
extern BOOL g_singleVersionHosting;

// *** ICorRuntimeHost methods ***
// Returns an object for configuring the runtime prior to
// it starting. If the runtime has been initialized this
// routine returns an error. See ICorConfiguration.
HRESULT CorHost::GetConfiguration(ICorConfiguration** pConfiguration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    HRESULT hr=E_FAIL;
    BEGIN_ENTRYPOINT_NOTHROW;
    if (CorHost::GetHostVersion() != 1)
    {
        hr=HOST_E_INVALIDOPERATION;
    }
    else
    if (!pConfiguration)
        hr= E_POINTER;
    else
    if (!m_Started)
    {
        *pConfiguration = (ICorConfiguration *) this;
        AddRef();
        hr=S_OK;
    }
    END_ENTRYPOINT_NOTHROW;
    // Cannot obtain configuration after the runtime is started
    return hr;
}

STDMETHODIMP CorHost::Start(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;
    hr = CorRuntimeHostBase::Start();

    END_ENTRYPOINT_NOTHROW;

    if (hr == S_FALSE)
    {
        // This is to keep v1 behavior.
        hr = S_OK;
    }
    return(hr);
}
#endif // !defined(FEATURE_CORECLR)


// *** ICorRuntimeHost methods ***
#ifndef FEATURE_CORECLR
// Returns an object for configuring the runtime prior to
// it starting. If the runtime has been initialized this
// routine returns an error. See ICorConfiguration.
HRESULT CorHost2::GetConfiguration(ICorConfiguration** pConfiguration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (!pConfiguration)
        return E_POINTER;
    HRESULT hr=E_FAIL;
    BEGIN_ENTRYPOINT_NOTHROW;
    if (!m_Started)
    {
        *pConfiguration = (ICorConfiguration *) this;
        AddRef();
        hr=S_OK;
    }
    END_ENTRYPOINT_NOTHROW;
    // Cannot obtain configuration after the runtime is started
    return hr;
}
#endif // FEATURE_CORECLR

extern BOOL g_fWeOwnProcess;

CorHost2::CorHost2()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_CORECLR
    m_fStarted = FALSE;
    m_fFirstToLoadCLR = FALSE;
    m_fAppDomainCreated = FALSE;
#endif // FEATURE_CORECLR
}

static DangerousNonHostedSpinLock lockOnlyOneToInvokeStart;

STDMETHODIMP CorHost2::Start()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }CONTRACTL_END;

    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

#ifdef FEATURE_CORECLR
    // Ensure that only one thread at a time gets in here
    DangerousNonHostedSpinLockHolder lockHolder(&lockOnlyOneToInvokeStart);
        
    // To provide the complete semantic of Start/Stop in context of a given host, we check m_fStarted and let
    // them invoke the Start only if they have not already. Likewise, they can invoke the Stop method
    // only if they have invoked Start prior to that.
    //
    // This prevents a host from invoking Stop twice and hitting the refCount to zero, when another
    // host is using the CLR, as CLR instance sharing across hosts is a scenario for CoreCLR.

    if (g_fEEStarted)
    {
        hr = S_OK;
        // CoreCLR is already running - but was Start already invoked by this host?
        if (m_fStarted)
        {
            // This host had already invoked the Start method - return them an error
            hr = HOST_E_INVALIDOPERATION;
        }
        else
        {
            // Increment the global (and dynamic) refCount...
            FastInterlockIncrement(&m_RefCount);

            // And set our flag that this host has invoked the Start...
            m_fStarted = TRUE;
        }
    }
    else
#endif // FEATURE_CORECLR
    {
        // Using managed C++ libraries, its possible that when the runtime is already running,
        // MC++ will use CorBindToRuntimeEx to make callbacks into specific appdomain of its
        // choice. Now, CorBindToRuntimeEx results in CorHost2::CreateObject being invoked
        // that will set runtime hosted flag "g_fHostConfig |= CLRHOSTED".
        //
        // For the case when managed code started without CLR hosting and MC++ does a 
        // CorBindToRuntimeEx, setting the CLR hosted flag is incorrect.
        //
        // Thus, before we attempt to start the runtime, we save the status of it being
        // already running or not. Next, if we are able to successfully start the runtime
        // and ONLY if it was not started earlier will we set the hosted flag below.
        if (!g_fEEStarted)
        {
            g_fHostConfig |= CLRHOSTED;
        }

        hr = CorRuntimeHostBase::Start();
        if (SUCCEEDED(hr))
        {
#ifdef FEATURE_CORECLR
            // Set our flag that this host invoked the Start method.
            m_fStarted = TRUE;

            // And they also loaded the CoreCLR DLL in the memory (for this version).
            // This is a special flag as the host that has got this flag set will be allowed
            // to repeatedly invoke Stop method (without corresponding Start method invocations).
            // This is to support scenarios like that of Office where they need to bring down
            // the CLR at any cost.
            // 
            // So, if you want to do that, just make sure you are the first host to load the
            // specific version of CLR in memory AND start it.
            m_fFirstToLoadCLR = TRUE;
#endif // FEATURE_CORECLR
            if (FastInterlockIncrement(&m_RefCount) != 1)
            {
            }
            else
            {
                if (g_fWeOwnProcess)
                {
                    // Runtime is started by a managed exe.  Bump the ref-count, so that
                    // matching Start/Stop does not stop runtime.
                    FastInterlockIncrement(&m_RefCount);
                }
            }
        }
    }

    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// Starts the runtime. This is equivalent to CoInitializeEE();
HRESULT CorRuntimeHostBase::Start()
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    {
        m_Started = TRUE;
#ifdef FEATURE_EVENT_TRACE
        g_fEEHostedStartup = TRUE;
#endif // FEATURE_EVENT_TRACE
        hr = InitializeEE(COINITEE_DEFAULT);
    }
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#if !defined(FEATURE_CORECLR) // simple hosting
HRESULT CorHost::Stop()
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    // This must remain this way (that is doing nothing) for backwards compat reasons.
    return S_OK;
}
#endif // !defined(FEATURE_CORECLR)

HRESULT CorHost2::Stop()
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;    // We're bringing the EE down, so no point in probing
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;
    if (!g_fEEStarted)
    {
        return E_UNEXPECTED;
    }
    HRESULT hr=S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

#ifdef FEATURE_CORECLR
    // Is this host eligible to invoke the Stop method?
    if ((!m_fStarted) && (!m_fFirstToLoadCLR))
    {
        // Well - since this host never invoked Start, it is not eligible to invoke Stop.
        // Semantically, for such a host, CLR is not available in the process. The only
        // exception to this condition is the host that first loaded this version of the
        // CLR and invoked Start method. For details, refer to comments in CorHost2::Start implementation.
        hr = HOST_E_CLRNOTAVAILABLE;
    }
    else
#endif // FEATURE_CORECLR
    {
        while (TRUE)
        {
            LONG refCount = m_RefCount;
            if (refCount == 0)
            {
    #ifdef FEATURE_CORECLR
                hr = HOST_E_CLRNOTAVAILABLE;
    #else // !FEATURE_CORECLR
                hr= E_UNEXPECTED;
    #endif // FEATURE_CORECLR
                break;
            }
            else
            if (FastInterlockCompareExchange(&m_RefCount, refCount - 1, refCount) == refCount)
            {
    #ifdef FEATURE_CORECLR
                // Indicate that we have got a Stop for a corresponding Start call from the
                // Host. Semantically, CoreCLR has stopped for them.
                m_fStarted = FALSE;
    #endif // FEATURE_CORECLR

                if (refCount > 1)
                {
                    hr=S_FALSE;
                    break;
                }
                else
                {
                    break;
                }
            }
        }
    }
#ifndef  FEATURE_CORECLR    
    if (hr==S_OK)
    {
        EPolicyAction action = GetEEPolicy()->GetDefaultAction(OPR_ProcessExit, NULL);
        if (action > eExitProcess)
        {
            g_fFastExitProcess = 1;
        }
        EEShutDown(FALSE);
    }
#endif // FEATURE_CORECLR     
    END_ENTRYPOINT_NOTHROW;

#ifndef FEATURE_CORECLR
    if (hr == S_OK)
    {
        if (m_HostControl)
        {
            m_HostControl->Release();
            m_HostControl = NULL;
        }
    }
#endif // FEATURE_CORECLR

    return hr;
}

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)

// Creates a domain in the runtime. The identity array is
// a pointer to an array TYPE containing IIdentity objects defining
// the security identity.
HRESULT CorRuntimeHostBase::CreateDomain(LPCWSTR pwzFriendlyName,
                                         IUnknown* pIdentityArray, // Optional
                                         IUnknown ** pAppDomain)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;

    return CreateDomainEx(pwzFriendlyName,
                          NULL,
                          NULL,
                          pAppDomain);
}


// Returns the default domain.
HRESULT CorRuntimeHostBase::GetDefaultDomain(IUnknown ** pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    } CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    if (!g_fEEStarted)
        return hr;

    if( pAppDomain == NULL)
        return E_POINTER;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        if (SystemDomain::System()) {
            AppDomain* pCom = SystemDomain::System()->DefaultDomain();
            if(pCom)
                hr = pCom->GetComIPForExposedObject(pAppDomain);
        }

    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// Returns the default domain.
HRESULT CorRuntimeHostBase::CurrentDomain(IUnknown ** pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    HRESULT hr = E_UNEXPECTED;
    if (!g_fEEStarted)
        return hr;

   if( pAppDomain == NULL) return E_POINTER;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        AppDomain* pCom = ::GetAppDomain();
        if(pCom)
            hr = pCom->GetComIPForExposedObject(pAppDomain);

    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
};

#endif // FEATURE_COMINTEROP && !FEATURE_CORECLR

HRESULT CorHost2::GetCurrentAppDomainId(DWORD *pdwAppDomainId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // No point going further if the runtime is not running...
    // We use CanRunManagedCode() instead of IsRuntimeActive() because this allows us
    // to specify test using the form that does not trigger a GC.
    if (!(g_fEEStarted && CanRunManagedCode(LoaderLockCheck::None))
#ifdef FEATURE_CORECLR
        || !m_fStarted
#endif    
    )
    {
        return HOST_E_CLRNOTAVAILABLE;
    }   
    
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(pdwAppDomainId == NULL)
    {
        hr = E_POINTER;
    }
    else
    {
        Thread *pThread = GetThread();
        if (!pThread)
        {
            hr = E_UNEXPECTED;
        }
        else
        {
            *pdwAppDomainId = SystemDomain::GetCurrentDomain()->GetId().m_dwId;
        }
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorHost2::ExecuteApplication(LPCWSTR   pwzAppFullName,
                                     DWORD     dwManifestPaths,
                                     LPCWSTR   *ppwzManifestPaths,
                                     DWORD     dwActivationData,
                                     LPCWSTR   *ppwzActivationData,
                                     int       *pReturnValue)
{
#ifndef FEATURE_CORECLR
    // This API should not be called when the EE has already been started.
    HRESULT hr = E_UNEXPECTED;
    if (g_fEEStarted)
        return hr;

    //
    // We will let unhandled exceptions in the activated application
    // propagate all the way up, so that ClickOnce semi-trusted apps
    // can participate in the Dr Watson program, etc...
    //

    CONTRACTL {
        THROWS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (!pwzAppFullName)
        IfFailGo(E_POINTER);

    // Set the information about the application to execute.
    CorCommandLine::m_pwszAppFullName = (LPWSTR) pwzAppFullName;
    CorCommandLine::m_dwManifestPaths = dwManifestPaths;
    CorCommandLine::m_ppwszManifestPaths = (LPWSTR*) ppwzManifestPaths;
    CorCommandLine::m_dwActivationData = dwActivationData;
    CorCommandLine::m_ppwszActivationData = (LPWSTR*) ppwzActivationData;

    // Start up the EE.
    IfFailGo(Start());

    Thread *pThread;
    pThread = GetThread();
    if (pThread == NULL)
        pThread = SetupThreadNoThrow(&hr);
    if (pThread == NULL)
        goto ErrExit;

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    hr = S_OK;

    BEGIN_ENTRYPOINT_THROWS_WITH_THREAD(pThread);
    ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(),ADV_DEFAULTAD)

    SystemDomain::ActivateApplication(pReturnValue);

    END_DOMAIN_TRANSITION;
    END_ENTRYPOINT_THROWS_WITH_THREAD;

ErrExit:
    return hr;
#else // FEATURE_CORECLR
    return E_NOTIMPL;
#endif
}

#ifdef FEATURE_CORECLR
/*
 * This method processes the arguments sent to the host which are then used
 * to invoke the main method.
 * Note -
 * [0] - points to the assemblyName that has been sent by the host.
 * The rest are the arguments sent to the assembly.
 * Also note, this might not always return the exact same identity as the cmdLine
 * used to invoke the method.
 *
 * For example :-
 * ActualCmdLine - Foo arg1 arg2.
 * (Host1)       - Full_path_to_Foo arg1 arg2
*/
void SetCommandLineArgs(LPCWSTR pwzAssemblyPath, int argc, LPCWSTR* argv)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    struct _gc
    {
        PTRARRAYREF cmdLineArgs;
    } gc;

    ZeroMemory(&gc, sizeof(gc));
    GCPROTECT_BEGIN(gc);

    gc.cmdLineArgs = (PTRARRAYREF)AllocateObjectArray(argc + 1 /* arg[0] should be the exe name*/, g_pStringClass);
    OBJECTREF orAssemblyPath = StringObject::NewString(pwzAssemblyPath);
    gc.cmdLineArgs->SetAt(0, orAssemblyPath);

    for (int i = 0; i < argc; ++i)
    {
        OBJECTREF argument = StringObject::NewString(argv[i]);
        gc.cmdLineArgs->SetAt(i + 1, argument);
    }

    MethodDescCallSite setCmdLineArgs(METHOD__ENVIRONMENT__SET_COMMAND_LINE_ARGS);

    ARG_SLOT args[] =
    {
        ObjToArgSlot(gc.cmdLineArgs),
    };
    setCmdLineArgs.Call(args);

    GCPROTECT_END();
}

HRESULT CorHost2::ExecuteAssembly(DWORD dwAppDomainId,
                                      LPCWSTR pwzAssemblyPath,
                                      int argc,
                                      LPCWSTR* argv,
                                      DWORD *pReturnValue)
{
    CONTRACTL
    {
        THROWS; // Throws...as we do not want it to swallow the managed exception
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // This is currently supported in default domain only
    if (dwAppDomainId != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive() || !m_fStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }   
   
    if(!pwzAssemblyPath)
        return E_POINTER;

    if(argc < 0)
    {
        return E_INVALIDARG;
    }

    if(argc > 0 && argv == NULL)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;

    AppDomain *pCurDomain = SystemDomain::GetCurrentDomain();

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
        }
    }

    if(pCurDomain->GetId().m_dwId != DefaultADID)
    {
        return HOST_E_INVALIDOPERATION;
    }

    INSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    Assembly *pAssembly = AssemblySpec::LoadAssembly(pwzAssemblyPath);

#if defined(FEATURE_MULTICOREJIT)
    pCurDomain->GetMulticoreJitManager().AutoStartProfile(pCurDomain);
#endif // defined(FEATURE_MULTICOREJIT)

    {
        GCX_COOP();

        // Here we call the managed method that gets the cmdLineArgs array.
        SetCommandLineArgs(pwzAssemblyPath, argc, argv);

        PTRARRAYREF arguments = NULL;
        GCPROTECT_BEGIN(arguments);

        arguments = (PTRARRAYREF)AllocateObjectArray(argc, g_pStringClass);
        for (int i = 0; i < argc; ++i)
        {
            STRINGREF argument = StringObject::NewString(argv[i]);
            arguments->SetAt(i, argument);
        }

        DWORD retval = pAssembly->ExecuteMainMethod(&arguments, TRUE /* waitForOtherThreads */);
        if (pReturnValue)
        {
            *pReturnValue = retval;
        }

        GCPROTECT_END();

    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_UNHANDLED_MANAGED_EXCEPTION_TRAP;

ErrExit:

    return hr;
}
#endif

HRESULT CorHost2::ExecuteInDefaultAppDomain(LPCWSTR pwzAssemblyPath,
                                            LPCWSTR pwzTypeName,
                                            LPCWSTR pwzMethodName,
                                            LPCWSTR pwzArgument,
                                            DWORD   *pReturnValue)
{
    CONTRACTL
    {
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive()
#ifdef FEATURE_CORECLR
        || !m_fStarted
#endif    
    )
    {
        return HOST_E_CLRNOTAVAILABLE;
    }   
   
    
#ifndef FEATURE_CORECLR
    if(! (pwzAssemblyPath && pwzTypeName && pwzMethodName) )
        return E_POINTER;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            goto ErrExit;
    }
    }

    _ASSERTE (!pThread->PreemptiveGCDisabled());

    EX_TRY
    {
    ENTER_DOMAIN_PTR(SystemDomain::System()->DefaultDomain(),ADV_DEFAULTAD)

    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    Assembly *pAssembly = AssemblySpec::LoadAssembly(pwzAssemblyPath);

    SString szTypeName(pwzTypeName);
    StackScratchBuffer buff1;
    const char* szTypeNameUTF8 = szTypeName.GetUTF8(buff1);
    MethodTable *pMT = ClassLoader::LoadTypeByNameThrowing(pAssembly,
                                                           NULL,
                                                           szTypeNameUTF8).AsMethodTable();

    SString szMethodName(pwzMethodName);
    StackScratchBuffer buff;
    const char* szMethodNameUTF8 = szMethodName.GetUTF8(buff);
    MethodDesc *pMethodMD = MemberLoader::FindMethod(pMT, szMethodNameUTF8, &gsig_SM_Str_RetInt);

    if (!pMethodMD)
    {
        hr = COR_E_MISSINGMETHOD;
    }
    else
    {
        GCX_COOP();

        MethodDescCallSite method(pMethodMD);

        STRINGREF sref = NULL;
        GCPROTECT_BEGIN(sref);

        if (pwzArgument)
            sref = StringObject::NewString(pwzArgument);

        ARG_SLOT MethodArgs[] =
        {
            ObjToArgSlot(sref)
        };
        DWORD retval = method.Call_RetI4(MethodArgs);
        if (pReturnValue)
            {
            *pReturnValue = retval;
            }

        GCPROTECT_END();
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    END_DOMAIN_TRANSITION;
    }
    EX_CATCH_HRESULT(hr);

ErrExit:

    END_ENTRYPOINT_NOTHROW;

    return hr;
#else // FEATURE_CORECLR
    // Ensure that code is not loaded in the Default AppDomain
    return HOST_E_INVALIDOPERATION;
#endif
}

HRESULT ExecuteInAppDomainHelper(FExecuteInAppDomainCallback pCallback,
                                 void * cookie)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_SO_INTOLERANT;

    HRESULT hr = S_OK;

    BEGIN_SO_TOLERANT_CODE(GetThread());
    hr = pCallback(cookie);
    END_SO_TOLERANT_CODE;

    return hr;
}

HRESULT CorHost2::ExecuteInAppDomain(DWORD dwAppDomainId,
                                     FExecuteInAppDomainCallback pCallback,
                                     void * cookie)
{

    // No point going further if the runtime is not running...
    if (!IsRuntimeActive()
#ifdef FEATURE_CORECLR
        || !m_fStarted
#endif // FEATURE_CORECLR    
    )
    {
        return HOST_E_CLRNOTAVAILABLE;
    }       

#ifdef FEATURE_CORECLR
    if(!(m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN))
    {
        // Ensure that code is not loaded in the Default AppDomain
        if (dwAppDomainId == DefaultADID)
           return HOST_E_INVALIDOPERATION;
    }
#endif // FEATURE_CORECLR

    // Moved this here since no point validating the pointer
    // if the basic checks [above] fail
    if( pCallback == NULL)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());
    ENTER_DOMAIN_ID(ADID(dwAppDomainId))
    {
        // We are calling an unmanaged function pointer, either an unmanaged function, or a marshaled out delegate.
        // The thread should be in preemptive mode, and SO_Tolerant.
        GCX_PREEMP();
        hr=ExecuteInAppDomainHelper (pCallback, cookie);
    }
    END_DOMAIN_TRANSITION;
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#define EMPTY_STRING_TO_NULL(s) {if(s && s[0] == 0) {s=NULL;};}

HRESULT CorHost2::_CreateAppDomain(
    LPCWSTR wszFriendlyName,
    DWORD  dwFlags,
    LPCWSTR wszAppDomainManagerAssemblyName, 
    LPCWSTR wszAppDomainManagerTypeName, 
    int nProperties, 
    LPCWSTR* pPropertyNames, 
    LPCWSTR* pPropertyValues,
#if !defined(FEATURE_CORECLR)
    ICLRPrivBinder* pBinder,
#endif
    DWORD* pAppDomainID)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;

#ifdef FEATURE_CORECLR
    //cannot call the function more than once when single appDomain is allowed
    if (m_fAppDomainCreated && (m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN))
    {
        return HOST_E_INVALIDOPERATION;
    }
#endif

    //normalize empty strings
    EMPTY_STRING_TO_NULL(wszFriendlyName);
    EMPTY_STRING_TO_NULL(wszAppDomainManagerAssemblyName);
    EMPTY_STRING_TO_NULL(wszAppDomainManagerTypeName);

    if(pAppDomainID==NULL)
        return E_POINTER;

#ifdef FEATURE_CORECLR
    if (!m_fStarted)
        return HOST_E_INVALIDOPERATION;
#endif // FEATURE_CORECLR

    if(wszFriendlyName == NULL)
        return E_INVALIDARG;

    if((wszAppDomainManagerAssemblyName == NULL) != (wszAppDomainManagerTypeName == NULL))
        return E_INVALIDARG;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());

    AppDomainCreationHolder<AppDomain> pDomain;

#ifdef FEATURE_CORECLR
    // If StartupFlag specifies single appDomain then return the default domain instead of creating new one
    if(m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN)
    {
        pDomain.Assign(SystemDomain::System()->DefaultDomain());
    }
    else
#endif
    {
        AppDomain::CreateUnmanagedObject(pDomain);
    }

    ETW::LoaderLog::DomainLoad(pDomain, (LPWSTR)wszFriendlyName);

#ifdef FEATURE_CORECLR
    if (dwFlags & APPDOMAIN_IGNORE_UNHANDLED_EXCEPTIONS)
    {
        pDomain->SetIgnoreUnhandledExceptions();
    }
#endif // FEATURE_CORECLR

    if (dwFlags & APPDOMAIN_SECURITY_FORBID_CROSSAD_REVERSE_PINVOKE)
        pDomain->SetReversePInvokeCannotEnter();

    if (dwFlags & APPDOMAIN_FORCE_TRIVIAL_WAIT_OPERATIONS)
        pDomain->SetForceTrivialWaitOperations();

#if !defined(FEATURE_CORECLR)
    if (pBinder != NULL)
        pDomain->SetLoadContextHostBinder(pBinder);
#endif
        
#ifdef PROFILING_SUPPORTED
    EX_TRY
#endif    
    {
        pDomain->SetAppDomainManagerInfo(wszAppDomainManagerAssemblyName,wszAppDomainManagerTypeName,eInitializeNewDomainFlags_None);

        GCX_COOP();
    
        struct 
        {
            STRINGREF friendlyName;
            PTRARRAYREF propertyNames;
            PTRARRAYREF propertyValues;
            STRINGREF sandboxName;
            OBJECTREF setupInfo;
            OBJECTREF adSetup;
        } _gc;

        ZeroMemory(&_gc,sizeof(_gc));

        GCPROTECT_BEGIN(_gc)
        _gc.friendlyName=StringObject::NewString(wszFriendlyName);
        
        if(nProperties>0)
        {
            _gc.propertyNames = (PTRARRAYREF) AllocateObjectArray(nProperties, g_pStringClass);
            _gc.propertyValues= (PTRARRAYREF) AllocateObjectArray(nProperties, g_pStringClass);
            for (int i=0;i< nProperties;i++)
            {
                STRINGREF obj = StringObject::NewString(pPropertyNames[i]);
                _gc.propertyNames->SetAt(i, obj);
                
                obj = StringObject::NewString(pPropertyValues[i]);
                _gc.propertyValues->SetAt(i, obj);
            }
        }

        if (dwFlags & APPDOMAIN_SECURITY_SANDBOXED)
        {
            _gc.sandboxName = StringObject::NewString(W("Internet"));
        }
        else
        {
            _gc.sandboxName = StringObject::NewString(W("FullTrust"));
        }

        MethodDescCallSite prepareDataForSetup(METHOD__APP_DOMAIN__PREPARE_DATA_FOR_SETUP);

        ARG_SLOT args[8];
        args[0]=ObjToArgSlot(_gc.friendlyName);
        args[1]=ObjToArgSlot(NULL);
        args[2]=ObjToArgSlot(NULL);
        args[3]=ObjToArgSlot(NULL);
#ifdef FEATURE_CORECLR
        //CoreCLR shouldn't have dependencies on parent app domain.
        args[4]=ObjToArgSlot(NULL);
#else
        args[4]=PtrToArgSlot(GetAppDomain()->GetSecurityDescriptor());
#endif //FEATURE_CORECLR
        args[5]=ObjToArgSlot(_gc.sandboxName);
        args[6]=ObjToArgSlot(_gc.propertyNames);
        args[7]=ObjToArgSlot(_gc.propertyValues);

        _gc.setupInfo=prepareDataForSetup.Call_RetOBJECTREF(args);

        //
        // Get the new flag values and set it to the domain
        //
        PTRARRAYREF handleArrayObj = (PTRARRAYREF) ObjectToOBJECTREF(_gc.setupInfo);
        _gc.adSetup = ObjectToOBJECTREF(handleArrayObj->GetAt(1));

#ifndef FEATURE_CORECLR
        // We need to setup domain sorting before any other managed code runs in the domain, since that code 
        // could end up caching data based on the sorting mode of the domain.
        pDomain->InitializeSorting(&_gc.adSetup);
        pDomain->InitializeHashing(&_gc.adSetup);
#endif

        pDomain->DoSetup(&_gc.setupInfo);

        pDomain->CacheStringsForDAC();
        
        GCPROTECT_END();

        *pAppDomainID=pDomain->GetId().m_dwId;

#ifdef FEATURE_CORECLR
        // If StartupFlag specifies single appDomain then set the flag that appdomain has already been created
        if(m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN)
        {
            m_fAppDomainCreated = TRUE;
        }
#endif
    }
#ifdef PROFILING_SUPPORTED
    EX_HOOK
    {
        // Need the first assembly loaded in to get any data on an app domain.
        {
            BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
            GCX_PREEMP();
            g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID)(AppDomain*) pDomain, GET_EXCEPTION()->GetHR());
            END_PIN_PROFILER();
        }
    }
    EX_END_HOOK;

    // Need the first assembly loaded in to get any data on an app domain.
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackAppDomainLoads());
        GCX_PREEMP();
        g_profControlBlock.pProfInterface->AppDomainCreationFinished((AppDomainID)(AppDomain*) pDomain, S_OK);
        END_PIN_PROFILER();
    }        
#endif // PROFILING_SUPPORTED

    // DoneCreating releases ownership of AppDomain.  After this call, there should be no access to pDomain.
    pDomain.DoneCreating();

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;

};

HRESULT CorHost2::_CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,     
    LPCWSTR wszClassName,     
    LPCWSTR wszMethodName,
    INT_PTR* fnPtr)
{

    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr=S_OK;

    EMPTY_STRING_TO_NULL(wszAssemblyName);
    EMPTY_STRING_TO_NULL(wszClassName);
    EMPTY_STRING_TO_NULL(wszMethodName);

    if (fnPtr == NULL)
       return E_POINTER;
    *fnPtr = NULL;

    if(wszAssemblyName == NULL)
        return E_INVALIDARG;
    
    if(wszClassName == NULL)
        return E_INVALIDARG;

    if(wszMethodName == NULL)
        return E_INVALIDARG;
    
#ifdef FEATURE_CORECLR
    if (!m_fStarted)
        return HOST_E_INVALIDOPERATION;

    if(!(m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN))
    {
        // Ensure that code is not loaded in the Default AppDomain
        if (appDomainID == DefaultADID)
            return HOST_E_INVALIDOPERATION;
    }
#endif

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    GCX_COOP_THREAD_EXISTS(GET_THREAD());

    MAKE_UTF8PTR_FROMWIDE(szAssemblyName, wszAssemblyName);
    MAKE_UTF8PTR_FROMWIDE(szClassName, wszClassName);
    MAKE_UTF8PTR_FROMWIDE(szMethodName, wszMethodName);

    ADID id;
    id.m_dwId=appDomainID;

    ENTER_DOMAIN_ID(id)

    GCX_PREEMP();

    AssemblySpec spec;
    spec.Init(szAssemblyName);
    Assembly* pAsm=spec.LoadAssembly(FILE_ACTIVE);

    // we have no signature to check so allowing calling partially trusted code
    // can result in an exploit
    if (!pAsm->GetSecurityDescriptor()->IsFullyTrusted())    
          ThrowHR(COR_E_SECURITY);

    TypeHandle th=pAsm->GetLoader()->LoadTypeByNameThrowing(pAsm,NULL,szClassName);
    MethodDesc* pMD=NULL;
    
    if (!th.IsTypeDesc()) 
    {
        pMD = MemberLoader::FindMethodByName(th.GetMethodTable(), szMethodName, MemberLoader::FM_Unique);
        if (pMD == NULL)
        {
            // try again without the FM_Unique flag (error path)
            pMD = MemberLoader::FindMethodByName(th.GetMethodTable(), szMethodName, MemberLoader::FM_Default);
            if (pMD != NULL)
            {
                // the method exists but is overloaded
                ThrowHR(COR_E_AMBIGUOUSMATCH);
            }
        }
    }

    if (pMD==NULL || !pMD->IsStatic() || pMD->ContainsGenericVariables()) 
        ThrowHR(COR_E_MISSINGMETHOD);

#ifdef FEATURE_CORECLR
    // the target method must be decorated with AllowReversePInvokeCallsAttribute
    if (!COMDelegate::IsMethodAllowedToSinkReversePInvoke(pMD))
        ThrowHR(COR_E_SECURITY);
#endif

    UMEntryThunk *pUMEntryThunk = GetAppDomain()->GetUMEntryThunkCache()->GetUMEntryThunk(pMD);
    *fnPtr = (INT_PTR)pUMEntryThunk->GetCode();

    END_DOMAIN_TRANSITION;

    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#ifdef FEATURE_CORECLR
HRESULT CorHost2::CreateAppDomainWithManager(
    LPCWSTR wszFriendlyName,
    DWORD  dwFlags,
    LPCWSTR wszAppDomainManagerAssemblyName, 
    LPCWSTR wszAppDomainManagerTypeName, 
    int nProperties, 
    LPCWSTR* pPropertyNames, 
    LPCWSTR* pPropertyValues, 
    DWORD* pAppDomainID)
{
    WRAPPER_NO_CONTRACT;

    return _CreateAppDomain(
        wszFriendlyName,
        dwFlags,
        wszAppDomainManagerAssemblyName, 
        wszAppDomainManagerTypeName, 
        nProperties, 
        pPropertyNames, 
        pPropertyValues,
        pAppDomainID);
}

HRESULT CorHost2::CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,     
    LPCWSTR wszClassName,     
    LPCWSTR wszMethodName,
    INT_PTR* fnPtr)
{
    WRAPPER_NO_CONTRACT;

    return _CreateDelegate(appDomainID, wszAssemblyName, wszClassName, wszMethodName, fnPtr);
}

HRESULT CorHost2::Authenticate(ULONGLONG authKey)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    // Host authentication was used by Silverlight. It is no longer relevant for CoreCLR.
    return S_OK;
}

HRESULT CorHost2::RegisterMacEHPort()
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    return S_OK;
}

HRESULT CorHost2::SetStartupFlags(STARTUP_FLAGS flag)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    if (g_fEEStarted)
    {
        return HOST_E_INVALIDOPERATION;
    }

    m_dwStartupFlags = flag;

    return S_OK;
}

#endif //FEATURE_CORECLR

#ifndef FEATURE_CORECLR
void PauseOneAppDomain(AppDomainIterator* pi)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EX_TRY {
        ENTER_DOMAIN_PTR(pi->GetDomain(),ADV_ITERATOR);

        MethodDescCallSite(METHOD__APP_DOMAIN__PAUSE).Call(NULL);

        END_DOMAIN_TRANSITION;
    } EX_CATCH {
    } EX_END_CATCH(SwallowAllExceptions);
}

void ResumeOneAppDomain(AppDomainIterator* pi)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_COOPERATIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    EX_TRY {
        ENTER_DOMAIN_PTR(pi->GetDomain(),ADV_ITERATOR);

        MethodDescCallSite(METHOD__APP_DOMAIN__RESUME).Call(NULL);

        END_DOMAIN_TRANSITION;
    } EX_CATCH {
    } EX_END_CATCH(SwallowAllExceptions);
}

// see comments in SuspendEEFromPause
DWORD WINAPI SuspendAndResumeForPause(LPVOID arg)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);

    g_PauseCompletedEvent.Set();
    g_ClrResumeEvent.Wait(INFINITE, FALSE);

    ThreadSuspend::RestartEE(FALSE, TRUE);
    return 0;
}

#endif // !FEATURE_CORECLR

HRESULT SuspendEEForPause()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#ifdef FEATURE_CORECLR
    // In CoreCLR, we always resume from the same thread that paused.  So we can simply suspend the EE from this thread,
    // knowing we'll restart from the same thread.
    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
#else
    // In the CLR, we can resume from a different thread than the one that paused.  We can't call SuspendEE directly,
    // because we can't call RestartEE from another thread.  So we queue a workitem to the ThreadPool to call SuspendEE
    // and ResumeEE on our behalf.

    EX_TRY
    {
        if (!ThreadpoolMgr::QueueUserWorkItem(SuspendAndResumeForPause, NULL, QUEUE_ONLY))
        {
            hr = HRESULT_FROM_GetLastError();
        }
        else
        {
            // wait for SuspendEE to complete before returning.
            g_PauseCompletedEvent.Wait(INFINITE,FALSE);
        }
    }
    EX_CATCH
    {
        hr = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif

    return hr;
}

HRESULT RestartEEFromPauseAndSetResumeEvent()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // see comments in SuspendEEFromPause
#ifdef FEATURE_CORECLR
    ThreadSuspend::RestartEE(FALSE, TRUE);
#else
    // setting the resume event below will restart the EE as well.  We don't wait for the restart
    // to complete, because we'll sync with it next time we go to cooperative mode.
#endif

    _ASSERTE(g_ClrResumeEvent.IsValid());
    g_ClrResumeEvent.Set();

    return S_OK;
}
    


CorExecutionManager::CorExecutionManager()
    : m_dwFlags(0), m_pauseStartTime(0)
{
    LIMITED_METHOD_CONTRACT;
    g_IsPaused = FALSE;
    g_PauseTime = 0;
}

HRESULT CorExecutionManager::Pause(DWORD dwAppDomainId, DWORD dwFlags)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#ifndef FEATURE_CORECLR
    if (!IsRuntimeActive())
        return HOST_E_CLRNOTAVAILABLE;
#endif

    if(g_IsPaused)
        return E_FAIL;

    EX_TRY
    {
        if(!g_ClrResumeEvent.IsValid())
            g_ClrResumeEvent.CreateManualEvent(FALSE);
        else
            g_ClrResumeEvent.Reset();

#ifndef FEATURE_CORECLR
        if (!g_PauseCompletedEvent.IsValid())
            g_PauseCompletedEvent.CreateManualEvent(FALSE);
        else
            g_PauseCompletedEvent.Reset();
#endif
    }
    EX_CATCH_HRESULT(hr);
    
    if (FAILED(hr))
        return hr;
    
    BEGIN_ENTRYPOINT_NOTHROW;

    m_dwFlags = dwFlags;

#ifndef FEATURE_CORECLR
    if ((m_dwFlags & PAUSE_APP_DOMAINS) != 0)
    {
        Thread* pThread = SetupThreadNoThrow(&hr);
        if (pThread != NULL)
        {
            GCX_COOP_THREAD_EXISTS(pThread);

            AppDomainIterator ai(/*bOnlyActive:*/ TRUE);
            while (ai.Next())
                PauseOneAppDomain(&ai);
        }
    }
#endif

    if (SUCCEEDED(hr))
    {
        g_IsPaused = TRUE;

        hr = SuspendEEForPause();

        // Even though this is named with TickCount, it returns milliseconds
        m_pauseStartTime = (INT64)CLRGetTickCount64(); 
    }

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


HRESULT CorExecutionManager::Resume(DWORD dwAppDomainId)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThread()) {GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
        ENTRY_POINT;  // This is called by a host.
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

#ifndef FEATURE_CORECLR
    if (!IsRuntimeActive())
        return HOST_E_CLRNOTAVAILABLE;
#endif

    if(!g_IsPaused)
        return E_FAIL;

#ifdef FEATURE_CORECLR
    // GCThread is the thread that did the Pause. Resume should also happen on that same thread
    Thread *pThread = GetThread();
    if(pThread != ThreadSuspend::GetSuspensionThread())
    {
        _ASSERTE(!"HOST BUG: The same thread that did Pause should do the Resume");
        return E_FAIL;
    }
#endif

    BEGIN_ENTRYPOINT_NOTHROW;

    // Even though this is named with TickCount, it returns milliseconds
    INT64 currTime = (INT64)CLRGetTickCount64(); 
    _ASSERTE(currTime >= m_pauseStartTime);
    _ASSERTE(m_pauseStartTime != 0);

    g_PauseTime += (currTime - m_pauseStartTime);
    g_IsPaused = FALSE;

    hr = RestartEEFromPauseAndSetResumeEvent();

#ifndef FEATURE_CORECLR
    if (SUCCEEDED(hr))
    {
        if ((m_dwFlags & PAUSE_APP_DOMAINS) != 0)
        {
            Thread* pThread = SetupThreadNoThrow(&hr);
            if (pThread != NULL)
            {
                GCX_COOP_THREAD_EXISTS(pThread);

                AppDomainIterator ai(/*bOnlyActive:*/ TRUE);
                while (ai.Next())
                    ResumeOneAppDomain(&ai);
            }
        }
    }
#endif

    END_ENTRYPOINT_NOTHROW;

    return hr;
}


#endif //!DACCESS_COMPILE

#ifdef FEATURE_CORECLR
#ifndef DACCESS_COMPILE
SVAL_IMPL(STARTUP_FLAGS, CorHost2, m_dwStartupFlags = STARTUP_CONCURRENT_GC);
#else
SVAL_IMPL(STARTUP_FLAGS, CorHost2, m_dwStartupFlags);
#endif

STARTUP_FLAGS CorHost2::GetStartupFlags()
{
    return m_dwStartupFlags;
}
#endif //FEATURE_CORECLR

#ifndef DACCESS_COMPILE

#if !defined(FEATURE_CORECLR)
/*************************************************************************************
 ** ICLRPrivRuntime Methods
 *************************************************************************************/

HRESULT CorHost2::GetInterface(
    REFCLSID rclsid,
    REFIID   riid,
    LPVOID * ppUnk)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        if (rclsid == __uuidof(CLRPrivAppXBinder))
        {
            CLRPrivBinderAppX * pBinder = CLRPrivBinderAppX::GetOrCreateBinder();
            hr = pBinder->QueryInterface(riid, ppUnk);
        }
        else
        {
            hr = E_NOINTERFACE;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CorHost2::CreateAppDomain(
    LPCWSTR pwzFriendlyName,
    ICLRPrivBinder * pBinder,
    LPDWORD pdwAppDomainId)
{
    return _CreateAppDomain(
        pwzFriendlyName,
        0 /* default security */,
        nullptr, /* domain manager */
        nullptr, /* domain manager */
        0, /* property count */
        nullptr, /* property names */
        nullptr, /* property values */
        pBinder,
        pdwAppDomainId);
}

HRESULT CorHost2::CreateDelegate(
    DWORD appDomainID,
    LPCWSTR wszAssemblyName,
    LPCWSTR wszClassName,
    LPCWSTR wszMethodName,
    LPVOID * ppvDelegate)
{
    return _CreateDelegate(appDomainID, wszAssemblyName, wszClassName,
                          wszMethodName, reinterpret_cast<INT_PTR*>(ppvDelegate));
}

// Flag indicating if the EE was started up by an managed exe. Defined in ceemain.cpp.
extern BOOL g_fEEManagedEXEStartup;

HRESULT CorHost2::ExecuteMain(
    ICLRPrivBinder * pBinder,
    int * pRetVal)
{
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_ENTRY_POINT;
    
    HRESULT hr = S_OK;

    // If an exception passes through here, it will cause the
    // "The application has generated an unhandled exception" dialog and offer to debug.
    BEGIN_ENTRYPOINT_THROWS;

    // Indicates that the EE was started up by a managed exe.
    g_fEEManagedEXEStartup = TRUE;

    IfFailGo(CorCommandLine::SetArgvW(WszGetCommandLine()));

    IfFailGo(EnsureEEStarted(COINITEE_MAIN));

    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    //
    // Look for the [STAThread] or [MTAThread] attribute
    // TODO delete this code when we move to the default  AppDomain
    // 
    HMODULE hMod = WszGetModuleHandle(NULL);
    
    PEImageHolder pTempImage(PEImage::LoadImage(hMod));
    PEFileHolder pTempFile(PEFile::Open(pTempImage.Extract()));

    // Check for CustomAttributes - Set up the DefaultDomain and the main thread
    // Note that this has to be done before ExplicitBind() as it
    // affects the bind
    mdToken tkEntryPoint = pTempFile->GetEntryPointToken();
    // <TODO>@TODO: What if the entrypoint is in another file of the assembly?</TODO>
    ReleaseHolder<IMDInternalImport> scope(pTempFile->GetMDImportWithRef());
    // In theory, we should have a valid executable image and scope should never be NULL, but we've been  
    // getting Watson failures for AVs here due to ISVs modifying image headers and some new OS loader 
    // checks (see Dev10# 718530 and Windows 7# 615596)
    if (scope == NULL)
    {
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    Thread::ApartmentState state = Thread::AS_Unknown;

    if((!IsNilToken(tkEntryPoint)) && (TypeFromToken(tkEntryPoint) == mdtMethodDef)) {
        if (scope->IsValidToken(tkEntryPoint))
            state = SystemDomain::GetEntryPointThreadAptState(scope, tkEntryPoint);
        else
            ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    BOOL fSetGlobalSharePolicyUsingAttribute = FALSE;

    if((!IsNilToken(tkEntryPoint)) && (TypeFromToken(tkEntryPoint) == mdtMethodDef))
    {
        // The global share policy needs to be set before initializing default domain 
        // so that it is in place for loading of appdomain manager.
        fSetGlobalSharePolicyUsingAttribute = SystemDomain::SetGlobalSharePolicyUsingAttribute(scope, tkEntryPoint);
    }

    // If the entry point has an explicit thread apartment state, set it
    // before running the AppDomainManager initialization code.
    if (state == Thread::AS_InSTA || state == Thread::AS_InMTA)
        SystemDomain::SetThreadAptState(scope, state);

    // This can potentially run managed code.
    SystemDomain::InitializeDefaultDomain(FALSE, pBinder);

    // If we haven't set an explicit thread apartment state, set it after the
    // AppDomainManager has got a chance to go set it in InitializeNewDomain.
    if (state != Thread::AS_InSTA && state != Thread::AS_InMTA)
        SystemDomain::SetThreadAptState(scope, state);

    if (fSetGlobalSharePolicyUsingAttribute)
        SystemDomain::System()->DefaultDomain()->SetupLoaderOptimization(g_dwGlobalSharePolicy);

    ADID adId(DefaultADID);

    GCX_COOP();

    ENTER_DOMAIN_ID(adId)
    TESTHOOKCALL(EnteredAppDomain(adId.m_dwId));
    {
        GCX_PREEMP();

        AppDomain *pDomain = GetAppDomain();
        _ASSERTE(pDomain);

        PathString wzExeFileName;
         
        if (WszGetModuleFileName(nullptr, wzExeFileName) == 0)
            IfFailThrow(E_UNEXPECTED);

        LPWSTR wzExeSimpleFileName = nullptr;
        size_t cchExeSimpleFileName = 0;
        SplitPathInterior(
        wzExeFileName,
        nullptr, nullptr, // drive
        nullptr, nullptr, // dir
        (LPCWSTR*)&wzExeSimpleFileName, &cchExeSimpleFileName, // filename
        nullptr, nullptr); // ext

        // Remove the extension
        wzExeSimpleFileName[cchExeSimpleFileName] = W('\0');

        ReleaseHolder<IAssemblyName> pAssemblyName;
        IfFailThrow(CreateAssemblyNameObject(
            &pAssemblyName,             // Returned IAssemblyName
            wzExeSimpleFileName,        // Name of assembly
            CANOF_PARSE_DISPLAY_NAME,   // Parse as display name
            nullptr));                  // Reserved

        AssemblySpec specExe;
        specExe.InitializeSpec(pAssemblyName, nullptr, false);
        
        PEAssemblyHolder pPEAssembly = pDomain->BindAssemblySpec(&specExe, TRUE, FALSE);

        pDomain->SetRootAssembly(pDomain->LoadAssembly(NULL, pPEAssembly, FILE_ACTIVE));

        LOG((LF_CLASSLOADER | LF_CORDB,
             LL_INFO10,
             "Created domain for an executable at %p\n",
             (pDomain->GetRootAssembly()? pDomain->GetRootAssembly()->Parent() : NULL)));
        TESTHOOKCALL(RuntimeStarted(RTS_CALLINGENTRYPOINT));

        // Set the friendly name to indicate that this is an immersive domain.
        pDomain->SetFriendlyName(W("Immersive Application Domain"), TRUE);

        // Execute the main method
        // NOTE: we call the entry point with our entry point exception filter active
        // after the AppDomain transition which is a bit different from classic apps.
        // this is so that we have the correct context when notifying the debugger
        // or invoking WER on the main thread and mimics the behavior of classic apps.
		// the assumption is that AppX entry points are always invoked post-AD transition.
        ExecuteMainInner(pDomain->GetRootAssembly());

        // Get the global latched exit code instead of the return value from ExecuteMainMethod
        // because in the case of a "void Main" method the return code is always 0,
        // while the latched exit code is set in either case.
        *pRetVal = GetLatchedExitCode();
    }
    END_DOMAIN_TRANSITION;
    TESTHOOKCALL(LeftAppDomain(adId.m_dwId));

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    
ErrExit:
    END_ENTRYPOINT_THROWS;

    return hr;
}

VOID CorHost2::ExecuteMainInner(Assembly* pRootAssembly)
{
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_ENTRY_POINT;

    struct Param
    {
        Assembly* pRootAssembly;
    } param;

    param.pRootAssembly = pRootAssembly;

    PAL_TRY(Param*, pParam, &param)
    {
		// since this is the thread 0 entry point for AppX apps we use
		// the EntryPointFilter so that an unhandled exception here will
		// trigger the same behavior as in classic apps.
        pParam->pRootAssembly->ExecuteMainMethod(NULL, TRUE /* waitForOtherThreads */);
    }
    PAL_EXCEPT_FILTER(EntryPointFilter)
    {
        LOG((LF_STARTUP, LL_INFO10, "EntryPointFilter returned EXCEPTION_EXECUTE_HANDLER!"));
    }
    PAL_ENDTRY
}

// static
HRESULT CorHost2::SetFlagsAndHostConfig(STARTUP_FLAGS dwStartupFlags, LPCWSTR pwzHostConfigFile, BOOL fFinalize)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = E_INVALIDARG;

    if (pwzHostConfigFile == NULL)
        pwzHostConfigFile = W("");

    DangerousNonHostedSpinLockHolder lockHolder(&m_FlagsLock);

    if (m_dwFlagsFinalized)
    {
        // verify that flags and config file are the same
        if (dwStartupFlags == m_dwStartupFlags &&
            _wcsicmp(pwzHostConfigFile, m_wzHostConfigFile) == 0)
        {
            hr = S_OK;
        }   
    }
    else
    {
        // overwrite the flags and config with the incoming values
        if (wcslen(pwzHostConfigFile) < COUNTOF(m_wzHostConfigFile))
        {
            VERIFY(wcscpy_s(m_wzHostConfigFile, COUNTOF(m_wzHostConfigFile), pwzHostConfigFile) == 0);

            // If they asked for the server gc but only have one processor, deny that option.
            // Keep this in sync with shim logic in ComputeStartupFlagsAndFlavor that also switches to 
            // the workstation GC on uniprocessor boxes.
            if (g_SystemInfo.dwNumberOfProcessors == 1 && (dwStartupFlags & STARTUP_SERVER_GC)) 
                dwStartupFlags = (STARTUP_FLAGS)(dwStartupFlags & ~(STARTUP_SERVER_GC | STARTUP_CONCURRENT_GC));

            m_dwStartupFlags = dwStartupFlags;

            if (fFinalize)
                m_dwFlagsFinalized = TRUE;

            hr = S_OK;
        }
    }

    return hr;
}

// static
STARTUP_FLAGS CorHost2::GetStartupFlags()
{
    WRAPPER_NO_CONTRACT;

    if (!m_dwFlagsFinalized) // make sure we return consistent results
    {
        DangerousNonHostedSpinLockHolder lockHolder(&m_FlagsLock);
        m_dwFlagsFinalized = TRUE;
    }

    return m_dwStartupFlags;
}

// static
LPCWSTR CorHost2::GetHostConfigFile()
{
    WRAPPER_NO_CONTRACT;

    if (!m_dwFlagsFinalized) // make sure we return consistent results
    {
        DangerousNonHostedSpinLockHolder lockHolder(&m_FlagsLock);
        m_dwFlagsFinalized = TRUE;
    }

    return m_wzHostConfigFile;
}

// static
void CorHost2::GetDefaultAppDomainProperties(StringArrayList **pPropertyNames, StringArrayList **pPropertyValues)
{
    LIMITED_METHOD_CONTRACT;
    
    // We should only read these after the runtime has started to ensure that the host isn't modifying them
    // still
    _ASSERTE(g_fEEStarted || HasStarted());

    *pPropertyNames = &s_defaultDomainPropertyNames;
    *pPropertyValues = &s_defaultDomainPropertyValues;
}

#endif // !FEATURE_CORECLR

#ifdef FEATURE_COMINTEROP

// Enumerate currently existing domains.
HRESULT CorRuntimeHostBase::EnumDomains(HDOMAINENUM *hEnum)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_PREEMPTIVE;
        WRAPPER(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if(hEnum == NULL) return E_POINTER;

    // Thread setup happens in BEGIN_EXTERNAL_ENTRYPOINT below.
    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    HRESULT hr = E_OUTOFMEMORY;
    *hEnum = NULL;
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)

    AppDomainIterator *pEnum = new (nothrow) AppDomainIterator(FALSE);
    if(pEnum) {
        *hEnum = (HDOMAINENUM) pEnum;
        hr = S_OK;
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // FEATURE_COMINTEROP

extern "C"
HRESULT  GetCLRRuntimeHost(REFIID riid, IUnknown **ppUnk)
{
    WRAPPER_NO_CONTRACT;

    return CorHost2::CreateObject(riid, (void**)ppUnk);
}

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)

HRESULT NextDomainWorker(AppDomainIterator *pEnum,
                         IUnknown** pAppDomain)
{
    CONTRACTL
    {
        DISABLED(NOTHROW); // nothrow contract's fs:0 handler gets called before the C++ EH fs:0 handler which is pushed in the prolog
        GC_TRIGGERS;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    Thread *pThread = GetThread();
    BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, return COR_E_STACKOVERFLOW);

    EX_TRY
    {
        GCX_COOP_THREAD_EXISTS(pThread);

        if (pEnum->Next())
        {
            AppDomain* pDomain = pEnum->GetDomain();
            // Need to enter the AppDomain to synchronize access to the exposed
            // object properly (can't just take the system domain mutex since we
            // might need to run code that uses higher ranking crsts).
            ENTER_DOMAIN_PTR(pDomain,ADV_ITERATOR)
            {

                hr = pDomain->GetComIPForExposedObject(pAppDomain);
            }
            END_DOMAIN_TRANSITION;
        }
        else
        {
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    END_SO_INTOLERANT_CODE;

    return hr;
}

// Returns S_FALSE when there are no more domains. A domain
// is passed out only when S_OK is returned.
HRESULT CorRuntimeHostBase::NextDomain(HDOMAINENUM hEnum,
                                       IUnknown** pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if(hEnum == NULL || pAppDomain == NULL)
        return E_POINTER;

    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    AppDomainIterator *pEnum = (AppDomainIterator *) hEnum;

    do
    {
        hr = NextDomainWorker(pEnum, pAppDomain);
    // Might need to look at the next appdomain if we were attempting to get at
    // the exposed appdomain object and were chucked out as the result of an
    // appdomain unload.
    } while (hr == COR_E_APPDOMAINUNLOADED);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

// Creates a domain in the runtime. The identity array is
// a pointer to an array TYPE containing IIdentity objects defining
// the security identity.
HRESULT CorRuntimeHostBase::CreateDomainEx(LPCWSTR pwzFriendlyName,
                                           IUnknown* pSetup, // Optional
                                           IUnknown* pEvidence, // Optional
                                           IUnknown ** pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    if(!pwzFriendlyName) return E_POINTER;
    if(pAppDomain == NULL) return E_POINTER;
    if(!g_fEEStarted) return E_FAIL;

    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        struct _gc {
            STRINGREF pName;
            OBJECTREF pSetup;
            OBJECTREF pEvidence;
            APPDOMAINREF pDomain;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        if (FAILED(hr = EnsureComStartedNoThrow()))
            goto lDone;

        GCPROTECT_BEGIN(gc);

        gc.pName = StringObject::NewString(pwzFriendlyName);

        if(pSetup)
            GetObjectRefFromComIP(&gc.pSetup, pSetup);
        if(pEvidence)
            GetObjectRefFromComIP(&gc.pEvidence, pEvidence);

        MethodDescCallSite createDomain(METHOD__APP_DOMAIN__CREATE_DOMAIN);

        ARG_SLOT args[3] = {
            ObjToArgSlot(gc.pName),
            ObjToArgSlot(gc.pEvidence),
            ObjToArgSlot(gc.pSetup),
        };

        gc.pDomain = (APPDOMAINREF) createDomain.Call_RetOBJECTREF(args);

        *pAppDomain = GetComIPFromObjectRef((OBJECTREF*) &gc.pDomain);

        GCPROTECT_END();

lDone: ;
    }
    END_EXTERNAL_ENTRYPOINT;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// Close the enumeration releasing resources
HRESULT CorRuntimeHostBase::CloseEnum(HDOMAINENUM hEnum)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(hEnum) {
        AppDomainIterator* pEnum = (AppDomainIterator*) hEnum;
        delete pEnum;
    }

    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT CorRuntimeHostBase::CreateDomainSetup(IUnknown **pAppDomainSetup)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!pAppDomainSetup)
        return E_POINTER;

    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    // Create the domain.
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        struct _gc {
            OBJECTREF pSetup;
        } gc;
        ZeroMemory(&gc, sizeof(gc));
        MethodTable* pMT = NULL;

        hr = EnsureComStartedNoThrow();
        if (FAILED(hr))
            goto lDone;

        pMT = MscorlibBinder::GetClass(CLASS__APPDOMAIN_SETUP);

        GCPROTECT_BEGIN(gc);
        gc.pSetup = AllocateObject(pMT);
        *pAppDomainSetup = GetComIPFromObjectRef((OBJECTREF*) &gc.pSetup);
        GCPROTECT_END();

lDone: ;
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CorRuntimeHostBase::CreateEvidence(IUnknown **pEvidence)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (!pEvidence)
        return E_POINTER;

#ifdef FEATURE_CAS_POLICY

    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    // Create the domain.
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP_THREAD_EXISTS(GET_THREAD());

        struct _gc {
            OBJECTREF pEvidence;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        MethodTable* pMT = NULL;

        hr = EnsureComStartedNoThrow();
        if (FAILED(hr))
            goto lDone;

        pMT = MscorlibBinder::GetClass(CLASS__EVIDENCE);

        GCPROTECT_BEGIN(gc);
        gc.pEvidence = AllocateObject(pMT);
        MethodDescCallSite ctor(METHOD__EVIDENCE__CTOR, &(gc.pEvidence));

        // Call the Evidence class constructor.
        ARG_SLOT CtorArgs[] =
        { 
            ObjToArgSlot(gc.pEvidence)
        };
        ctor.Call(CtorArgs);

        *pEvidence = GetComIPFromObjectRef((OBJECTREF*) &gc.pEvidence);
        GCPROTECT_END();

lDone: ;
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;
#else // !FEATURE_CAS_POLICY
    // There is no Evidence class support without CAS policy.
    return E_NOTIMPL;
#endif // FEATURE_CAS_POLICY

    return hr;
}

HRESULT CorRuntimeHostBase::UnloadDomain(IUnknown *pUnkDomain)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (!pUnkDomain)
        return E_POINTER;

    // If the runtime has not started, we have nothing to do.
    if (!g_fEEStarted)
    {
        return HOST_E_CLRNOTAVAILABLE;
    }

    CONTRACT_VIOLATION(FaultViolation); // This entire function is full of OOM potential: must fix.

    HRESULT hr = S_OK;
    DWORD dwDomainId = 0;
    BEGIN_ENTRYPOINT_NOTHROW;

    _ASSERTE (g_fComStarted);

    {
        SystemDomain::LockHolder lh;

        ComCallWrapper* pWrap = GetCCWFromIUnknown(pUnkDomain, FALSE);
        if (!pWrap)
        {
            hr = COR_E_APPDOMAINUNLOADED;
        }
        if (SUCCEEDED(hr))
        {
            dwDomainId = pWrap->GetDomainID().m_dwId;
        }
    }
    if (SUCCEEDED(hr))
    {
        hr = UnloadAppDomain(dwDomainId, TRUE);
    }
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // FEATURE_COMINTEROP && !FEATURE_CORECLR

STDMETHODIMP CorHost2::UnloadAppDomain(DWORD dwDomainId, BOOL fWaitUntilDone)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

#ifdef FEATURE_CORECLR
    if (!m_fStarted)
        return HOST_E_INVALIDOPERATION;

    if(m_dwStartupFlags & STARTUP_SINGLE_APPDOMAIN)
    {
        if (!g_fEEStarted)
        {
            return HOST_E_CLRNOTAVAILABLE;
        }

        if(!m_fAppDomainCreated)
        {
            return HOST_E_INVALIDOPERATION;
        }

        HRESULT hr=S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;
    
        if (!m_fFirstToLoadCLR)
        {
            _ASSERTE(!"Not reachable");
            hr = HOST_E_CLRNOTAVAILABLE;
        }
        else
        {
            LONG refCount = m_RefCount;
            if (refCount == 0)
            {
                hr = HOST_E_CLRNOTAVAILABLE;
            }
            else
            if (1 == refCount)
            {
                // Stop coreclr on unload.
                m_fStarted = FALSE;
                EEShutDown(FALSE);
            }
            else
            {
                _ASSERTE(!"Not reachable");
                hr = S_FALSE;
            }
        }
        END_ENTRYPOINT_NOTHROW;

        return hr;
    }
    else
#endif // FEATURE_CORECLR

    return CorRuntimeHostBase::UnloadAppDomain(dwDomainId, fWaitUntilDone);
}

HRESULT CorRuntimeHostBase::UnloadAppDomain(DWORD dwDomainId, BOOL fSync)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        FORBID_FAULT; // Unloading domains cannot fail due to OOM
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // No point going further if the runtime is not running...
    {
        // In IsRuntimeActive, we will call CanRunManagedCode that will
        // check if the current thread has taken the loader lock or not,
        // if MDA is supported. To do the check, MdaLoaderLock::ReportViolation
        // will be invoked that will internally end up invoking
        // MdaFactory<MdaXmlElement>::GetNext that will use the "new" operator
        // that has the "FAULT" contract set, resulting in FAULT_VIOLATION since
        // this method has the FORBID_FAULT contract set above.
        //
        // However, for a thread that holds the loader lock, unloading the appDomain is
        // not a supported scenario. Thus, we should not be ending up in this code
        // path for the FAULT violation. 
        //
        // Hence, the CONTRACT_VIOLATION below for overriding the FORBID_FAULT
        // for this scope only.
        CONTRACT_VIOLATION(FaultViolation);
        if (!IsRuntimeActive()
    #ifdef FEATURE_CORECLR
            || !m_fStarted
    #endif // FEATURE_CORECLR    
        )
        {
            return HOST_E_CLRNOTAVAILABLE;
        }   
    }
    
    BEGIN_ENTRYPOINT_NOTHROW;

    // We do not use BEGIN_EXTERNAL_ENTRYPOINT here because
    // we do not want to setup Thread.  Process may be OOM, and we want Unload
    // to work.
    hr =  AppDomain::UnloadById(ADID(dwDomainId), fSync);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

//*****************************************************************************
// Fiber Methods
//*****************************************************************************
#if !defined(FEATURE_CORECLR) // simple hosting
HRESULT CorHost::CreateLogicalThreadState()
{
    CONTRACTL
    {
        NOTHROW;
        DISABLED(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    if (CorHost::GetHostVersion() != 1)
    {
        hr=HOST_E_INVALIDOPERATION;
    }
    else
    {
        _ASSERTE (GetThread() == 0 || GetThread()->HasRightCacheStackBase());
        /* Thread  *thread = */ SetupThreadNoThrow(&hr);
      
    }
    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT CorHost::DeleteLogicalThreadState()
{
    if (CorHost::GetHostVersion() != 1)
    {
        return HOST_E_INVALIDOPERATION;
    }

    Thread *pThread = GetThread();
    if (!pThread)
        return E_UNEXPECTED;

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;
    // We need to reset the TrapReturningThread count that was
    // set when a thread is requested to be aborted.  Otherwise
    // every stub call is going to go through a slow path.
    if (pThread->IsAbortRequested())
        pThread->UnmarkThreadForAbort(Thread::TAR_ALL);

    // see code:Thread::OnThreadTerminate#ReportDeadOnThreadTerminate
    pThread->SetThreadState(Thread::TS_ReportDead);

    pThread->OnThreadTerminate(FALSE);
    END_ENTRYPOINT_NOTHROW;
    return hr;
}


HRESULT CorHost::SwitchInLogicalThreadState(DWORD *pFiberCookie)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_ENTRY_POINT;

    if (CorHost::GetHostVersion() != 1)
    {
        return HOST_E_INVALIDOPERATION;
    }

    if (!pFiberCookie)
    {
        return E_POINTER;
    }

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    hr = ((Thread*)pFiberCookie)->SwitchIn(::GetCurrentThread());

    END_ENTRYPOINT_NOTHROW;
    return hr;

}

HRESULT CorHost::SwitchOutLogicalThreadState(DWORD **pFiberCookie)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_ENTRY_POINT;

    if (CorHost::GetHostVersion() != 1)
    {
        return HOST_E_INVALIDOPERATION;
    }

     if (!pFiberCookie)
    {
        return E_POINTER;
    }

     Thread *pThread = GetThread();
     if (!pThread)
     {
        return E_UNEXPECTED;
     }

        pThread->InternalSwitchOut();
        *pFiberCookie = (DWORD*)pThread;

    return S_OK;
}
#endif // !defined(FEATURE_CORECLR)

HRESULT CorRuntimeHostBase::LocksHeldByLogicalThread(DWORD *pCount)
{
    if (!pCount)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_NOTHROW;

    Thread* pThread = GetThread();
    if (pThread == NULL)
        *pCount = 0;
    else
        *pCount = pThread->m_dwLockCount;

    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

//*****************************************************************************
// ICorConfiguration
//*****************************************************************************
#if !defined(FEATURE_CORECLR)
IGCThreadControl *CorConfiguration::m_CachedGCThreadControl = 0;
IGCHostControl *CorConfiguration::m_CachedGCHostControl = 0;
IDebuggerThreadControl *CorConfiguration::m_CachedDebuggerThreadControl = 0;
DWORD *CorConfiguration::m_DSTArray = 0;
DWORD CorConfiguration::m_DSTCount = 0;
DWORD CorConfiguration::m_DSTArraySize = 0;

// *** ICorConfiguration methods ***


HRESULT CorConfiguration::SetGCThreadControl(IGCThreadControl *pGCThreadControl)
{
    if (!pGCThreadControl)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (m_CachedGCThreadControl)
        m_CachedGCThreadControl->Release();

    m_CachedGCThreadControl = pGCThreadControl;

    if (m_CachedGCThreadControl)
        m_CachedGCThreadControl->AddRef();

    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

HRESULT CorConfiguration::SetGCHostControl(IGCHostControl *pGCHostControl)
{
    if (!pGCHostControl)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (m_CachedGCHostControl)
        m_CachedGCHostControl->Release();

    m_CachedGCHostControl = pGCHostControl;

    if (m_CachedGCHostControl)
        m_CachedGCHostControl->AddRef();

    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

HRESULT CorConfiguration::SetDebuggerThreadControl(IDebuggerThreadControl *pDebuggerThreadControl)
{
    if (!pDebuggerThreadControl)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

#ifdef DEBUGGING_SUPPORTED
    // Can't change the debugger thread control object once its been set.
    if (m_CachedDebuggerThreadControl != NULL)
        IfFailGo(E_INVALIDARG);

    m_CachedDebuggerThreadControl = pDebuggerThreadControl;

    // If debugging is already initialized then provide this interface pointer to it.
    // It will also addref the new one and release the old one.
    if (g_pDebugInterface)
        g_pDebugInterface->SetIDbgThreadControl(pDebuggerThreadControl);

    if (m_CachedDebuggerThreadControl)
        m_CachedDebuggerThreadControl->AddRef();

    hr = S_OK;
#else // !DEBUGGING_SUPPORTED
    hr = E_NOTIMPL;
#endif // !DEBUGGING_SUPPORTED

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;

}


HRESULT CorConfiguration::AddDebuggerSpecialThread(DWORD dwSpecialThreadId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;    // debugging not hardened for SO
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;


#ifdef DEBUGGING_SUPPORTED
    // If it's already in the list, don't add it again.
    if (IsDebuggerSpecialThread(dwSpecialThreadId))
    {
        hr = S_OK;
        goto ErrExit;
    }
    // Grow the array if necessary.
    if (m_DSTCount >= m_DSTArraySize)
    {
        // There's probably only ever gonna be one or two of these
        // things, so we'll start small.
        DWORD newSize = (m_DSTArraySize == 0) ? 2 : m_DSTArraySize * 2;

        DWORD *newArray = new (nothrow) DWORD[newSize];
        IfNullGo(newArray);

        // If we're growing instead of starting, then copy the old array.
        if (m_DSTArray)
        {
            memcpy(newArray, m_DSTArray, m_DSTArraySize * sizeof(DWORD));
            delete [] m_DSTArray;
        }

        // Update to the new array and size.
        m_DSTArray = newArray;
        m_DSTArraySize = newSize;
    }

    // Save the new thread ID.
    m_DSTArray[m_DSTCount++] = dwSpecialThreadId;

    hr = (RefreshDebuggerSpecialThreadList());
#else // !DEBUGGING_SUPPORTED
    hr = E_NOTIMPL;
#endif // !DEBUGGING_SUPPORTED
ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;

}
// Helper function to update the thread list in the debugger control block
HRESULT CorConfiguration::RefreshDebuggerSpecialThreadList()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef DEBUGGING_SUPPORTED
    HRESULT hr = S_OK;

    if (g_pDebugInterface)
    {
        // Inform the debugger services that this list has changed
        hr = g_pDebugInterface->UpdateSpecialThreadList(
            m_DSTCount, m_DSTArray);

        _ASSERTE(SUCCEEDED(hr));
    }

    return (hr);
#else // !DEBUGGING_SUPPORTED
    return E_NOTIMPL;
#endif // !DEBUGGING_SUPPORTED
}


// Helper func that returns true if the thread is in the debugger special thread list
BOOL CorConfiguration::IsDebuggerSpecialThread(DWORD dwThreadId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    for (DWORD i = 0; i < m_DSTCount; i++)
    {
        if (m_DSTArray[i] == dwThreadId)
            return (TRUE);
    }

    return (FALSE);
}


// Clean up any debugger thread control object we may be holding, called at shutdown.
void CorConfiguration::CleanupDebuggerThreadControl()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_CachedDebuggerThreadControl != NULL)
    {
        // Note: we don't release the IDebuggerThreadControl object if we're cleaning up from
        // our DllMain. The DLL that implements the object may already have been unloaded.
        // Leaking the object is okay... the PDM doesn't care.
        if (!IsAtProcessExit())
            m_CachedDebuggerThreadControl->Release();

        m_CachedDebuggerThreadControl = NULL;
    }
}
#endif // !defined(FEATURE_CORECLR)

//*****************************************************************************
// IUnknown
//*****************************************************************************

ULONG CorRuntimeHostBase::AddRef()
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        SO_TOLERANT;
    }
    CONTRACTL_END;
    return InterlockedIncrement(&m_cRef);
}

#if !defined(FEATURE_CORECLR) // simple hosting
ULONG CorHost::Release()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    ULONG   cRef = InterlockedDecrement(&m_cRef);
    if (!cRef) {
        delete this;
    }

    return (cRef);
}
#endif // !defined(FEATURE_CORECLR)

ULONG CorHost2::Release()
{
    LIMITED_METHOD_CONTRACT;

    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (!cRef) {
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
        // CorHost2 is allocated before host memory interface is set up.
        if (GetHostMemoryManager() == NULL)
#endif // FEATURE_INCLUDE_ALL_INTERFACES
            delete this;
    }

    return (cRef);
}

#if !defined(FEATURE_CORECLR) // simple hosting
HRESULT CorHost::QueryInterface(REFIID riid, void **ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;    // no global state updates that need guarding.
    }
    CONTRACTL_END;

    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
        *ppUnk = (IUnknown *) (ICorRuntimeHost *) this;
    else if (riid == IID_ICorRuntimeHost)
    {
        ULONG version = 1;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        if (m_Version != version && (g_singleVersionHosting || !g_fEEStarted))
        {
            return HOST_E_INVALIDOPERATION;
        }

        *ppUnk = (ICorRuntimeHost *) this;
    }
    else if (riid == IID_ICorThreadpool)
        *ppUnk = (ICorThreadpool *) this;
    else if (riid == IID_IGCHost)
        *ppUnk = (IGCHost *) this;
    else if (riid == IID_IGCHost2)
        *ppUnk = (IGCHost2 *) this;
    else if (riid == IID_IValidator)
        *ppUnk = (IValidator *) this;
    else if (riid == IID_IDebuggerInfo)
        *ppUnk = (IDebuggerInfo *) this;
    else if (riid == IID_ICLRExecutionManager)
        *ppUnk = (ICLRExecutionManager *) this;
    else
        return (E_NOINTERFACE);
    AddRef();
    return (S_OK);
}
#endif // !defined(FEATURE_CORECLR)


HRESULT CorHost2::QueryInterface(REFIID riid, void **ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;    // no global state updates that need guarding.
    }
    CONTRACTL_END;

    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
        *ppUnk = static_cast<IUnknown *>(static_cast<ICLRRuntimeHost *>(this));
#ifdef FEATURE_CORECLR // CoreCLR only supports IID_ICLRRuntimeHost2
    else if (riid == IID_ICLRRuntimeHost2)
    {
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRRuntimeHost2 *>(this);
    }
#else // DesktopCLR only supports IID_ICLRRuntimeHost
    else if (riid == IID_ICLRRuntimeHost)
    {
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRRuntimeHost *>(this);
    }
#endif // FEATURE_CORECLR
    else if (riid == IID_ICLRExecutionManager)
    {
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRExecutionManager *>(this);
    }
#if !defined(FEATURE_CORECLR)
    else if (riid == __uuidof(ICLRPrivRuntime))
    {
        ULONG version = 2;
        if (m_Version == 0)
            FastInterlockCompareExchange((LONG*)&m_Version, version, 0);

        *ppUnk = static_cast<ICLRPrivRuntime *>(this);
    }
#endif
#ifndef FEATURE_PAL
    else if (riid == IID_IPrivateManagedExceptionReporting)
    {
        *ppUnk = static_cast<IPrivateManagedExceptionReporting *>(this);
    }
#endif // !FEATURE_PAL
#ifndef FEATURE_CORECLR
    else if (riid == IID_ICorThreadpool)
        *ppUnk = static_cast<ICorThreadpool *>(this);
    // TODO: wwl Remove this after SQL uses new interface.
    else if (riid == IID_IGCHost &&
             GetHostVersion() == 3)
        *ppUnk = static_cast<IGCHost *>(this);
    else if (riid == IID_ICLRValidator)
        *ppUnk = static_cast<ICLRValidator *>(this);
    else if (riid == IID_IDebuggerInfo)
        *ppUnk = static_cast<IDebuggerInfo *>(this);
#ifdef FEATURE_TESTHOOKS
    else if (riid == IID_ICLRTestHookManager)
    {
        *ppUnk=CLRTestHookManager::Start();
        if(*ppUnk==NULL)
            return E_OUTOFMEMORY;
    }
#endif // FEATURE_TESTHOOKS
#endif // FEATURE_CORECLR
    else
        return (E_NOINTERFACE);
    AddRef();
    return (S_OK);
}

#ifndef FEATURE_CORECLR // CorHost isn't exposed externally
//*****************************************************************************
// Called by the class factory template to create a new instance of this object.
//*****************************************************************************
HRESULT CorHost::CreateObject(REFIID riid, void **ppUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    CorHost *pCorHost = new (nothrow) CorHost();
    if (!pCorHost)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        hr = pCorHost->QueryInterface(riid, ppUnk);

    if (FAILED(hr))
        delete pCorHost;
    }
    return (hr);
}
#endif // FEATURE_CORECLR

#ifndef FEATURE_PAL
HRESULT CorHost2::GetBucketParametersForCurrentException(BucketParameters *pParams)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // To avoid confusion, clear the buckets.
    memset(pParams, 0, sizeof(BucketParameters));

    // Defer to Watson helper.
    hr = ::GetBucketParametersForCurrentException(pParams);

    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // !FEATURE_PAL

HRESULT CorHost2::CreateObject(REFIID riid, void **ppUnk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW; );
    CorHost2 *pCorHost = new (nothrow) CorHost2();
    if (!pCorHost)
    {
        hr = E_OUTOFMEMORY;
    }
    else
    {
        hr = pCorHost->QueryInterface(riid, ppUnk);
    if (FAILED(hr))
        delete pCorHost;
    }
    END_SO_INTOLERANT_CODE;
    return (hr);
}


//-----------------------------------------------------------------------------
// MapFile - Maps a file into the runtime in a non-standard way
//-----------------------------------------------------------------------------

static PEImage *MapFileHelper(HANDLE hFile)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    GCX_PREEMP();

    HandleHolder hFileMap(WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
    if (hFileMap == NULL)
        ThrowLastError();

    CLRMapViewHolder base(CLRMapViewOfFile(hFileMap, FILE_MAP_READ, 0, 0, 0));
    if (base == NULL)
        ThrowLastError();

    DWORD dwSize = SafeGetFileSize(hFile, NULL);
    if (dwSize == 0xffffffff && GetLastError() != NOERROR)
    {
        ThrowLastError();
    }
    PEImageHolder pImage(PEImage::LoadFlat(base, dwSize));
    return pImage.Extract();
}

HRESULT CorRuntimeHostBase::MapFile(HANDLE hFile, HMODULE* phHandle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr;
    BEGIN_ENTRYPOINT_NOTHROW;

    BEGIN_EXTERNAL_ENTRYPOINT(&hr)
    {
        *phHandle = (HMODULE) (MapFileHelper(hFile)->GetLoadedLayout()->GetBase());
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;


    return hr;
}

///////////////////////////////////////////////////////////////////////////////
// IDebuggerInfo::IsDebuggerAttached
#if !defined(FEATURE_CORECLR)
HRESULT CorDebuggerInfo::IsDebuggerAttached(BOOL *pbAttached)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    if (pbAttached == NULL)
        hr = E_INVALIDARG;
    else
#ifdef DEBUGGING_SUPPORTED
    *pbAttached = (CORDebuggerAttached() != 0);
#else
    *pbAttached = FALSE;
#endif

    END_ENTRYPOINT_NOTHROW;

    return hr;
}
#endif // !defined(FEATURE_CORECLR)

LONG CorHost2::m_RefCount = 0;

IHostControl *CorHost2::m_HostControl = NULL;

LPCWSTR CorHost2::s_wszAppDomainManagerAsm = NULL;
LPCWSTR CorHost2::s_wszAppDomainManagerType = NULL;
EInitializeNewDomainFlags CorHost2::s_dwDomainManagerInitFlags = eInitializeNewDomainFlags_None;

#ifndef FEATURE_CORECLR // not supported

StringArrayList CorHost2::s_defaultDomainPropertyNames;
StringArrayList CorHost2::s_defaultDomainPropertyValues;

IHostMemoryManager *CorHost2::m_HostMemoryManager = NULL;
IHostMalloc *CorHost2::m_HostMalloc = NULL;
IHostTaskManager *CorHost2::m_HostTaskManager = NULL;
IHostThreadpoolManager *CorHost2::m_HostThreadpoolManager = NULL;
IHostIoCompletionManager *CorHost2::m_HostIoCompletionManager = NULL;
IHostSyncManager *CorHost2::m_HostSyncManager = NULL;
IHostAssemblyManager *CorHost2::m_HostAssemblyManager = NULL;
IHostGCManager *CorHost2::m_HostGCManager = NULL;
IHostSecurityManager *CorHost2::m_HostSecurityManager = NULL;
IHostPolicyManager *CorHost2::m_HostPolicyManager = NULL;
int CorHost2::m_HostOverlappedExtensionSize = -1;

STARTUP_FLAGS CorHost2::m_dwStartupFlags = STARTUP_CONCURRENT_GC;
WCHAR CorHost2::m_wzHostConfigFile[_MAX_PATH] = { 0 };

BOOL CorHost2::m_dwFlagsFinalized = FALSE;
DangerousNonHostedSpinLock CorHost2::m_FlagsLock;

class CCLRMemoryNotificationCallback: public ICLRMemoryNotificationCallback
{
public:
    virtual HRESULT STDMETHODCALLTYPE OnMemoryNotification(EMemoryAvailable eMemoryAvailable) {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        // We have not started runtime yet.
        if (!g_fEEStarted)
            return S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        switch (eMemoryAvailable)
        {
        case eMemoryAvailableLow:
            STRESS_LOG0(LF_GC, LL_INFO100, "Host delivers memory notification: Low\n");
            break;
        case eMemoryAvailableNeutral:
            STRESS_LOG0(LF_GC, LL_INFO100, "Host delivers memory notification: Neutral\n");
            break;
        case eMemoryAvailableHigh:
            STRESS_LOG0(LF_GC, LL_INFO100, "Host delivers memory notification: High\n");
            break;
        }
        static DWORD lastTime = (DWORD)-1;
        if (eMemoryAvailable == eMemoryAvailableLow)
        {
            FastInterlockIncrement ((LONG *)&g_bLowMemoryFromHost);
            DWORD curTime = GetTickCount();
            if (curTime < lastTime || curTime - lastTime >= 0x2000)
            {
                lastTime = curTime;
                FinalizerThread::EnableFinalization();
            }
        }
        else
        {
            FastInterlockExchange ((LONG *)&g_bLowMemoryFromHost, FALSE);
        }
        END_ENTRYPOINT_NOTHROW;

        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRMemoryNotificationCallback && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppvObject = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
};

static CCLRMemoryNotificationCallback s_MemoryNotification;

class CLRTaskManager : public ICLRTaskManager
{
public:
    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject) {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRTaskManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppvObject = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE CreateTask(ICLRTask **pTask)
    {
        CONTRACTL
        {
            NOTHROW;
            DISABLED(GC_NOTRIGGER);
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

#ifdef _DEBUG
        _ASSERTE (!CLRTaskHosted() || GetCurrentHostTask());
#endif
        _ASSERTE (GetThread() == NULL);
        Thread *pThread = NULL;
        pThread = SetupThreadNoThrow(&hr);
        *pTask = pThread;

        END_ENTRYPOINT_NOTHROW;

        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentTask(ICLRTask **pTask)
    {
        // This function may be called due SQL SwitchIn/Out.  Contract may
        // force memory allocation which is not allowed during Switch.
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_SO_TOLERANT;
        STATIC_CONTRACT_ENTRY_POINT;

        *pTask = GetThread();
        return S_OK;
    }

    virtual HRESULT STDMETHODCALLTYPE SetUILocale(LCID lcid)
    {
        Thread *pThread = GetThread();
        if (pThread == NULL)
            return HOST_E_INVALIDOPERATION;

        CONTRACTL
        {
            GC_TRIGGERS;
            NOTHROW;
            MODE_PREEMPTIVE;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        //BEGIN_ENTRYPOINT_NOTHROW;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            pThread->SetCultureId(lcid,TRUE);
        }
        END_EXTERNAL_ENTRYPOINT;
        //END_ENTRYPOINT_NOTHROW;

        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE SetLocale(LCID lcid)
    {
        Thread *pThread = GetThread();
        if (pThread == NULL)
            return HOST_E_INVALIDOPERATION;

        CONTRACTL
        {
            GC_TRIGGERS;
            NOTHROW;
            MODE_PREEMPTIVE;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        //BEGIN_ENTRYPOINT_NOTHROW;

        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            pThread->SetCultureId(lcid,FALSE);
        }
        END_EXTERNAL_ENTRYPOINT;
        //END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentTaskType(ETaskType *pTaskType)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        BEGIN_ENTRYPOINT_NOTHROW;
        *pTaskType = ::GetCurrentTaskType();
        END_ENTRYPOINT_NOTHROW;

        return S_OK;
    }
};

static CLRTaskManager s_CLRTaskManager;

class CLRSyncManager : public ICLRSyncManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetMonitorOwner(SIZE_T Cookie,
                                                      IHostTask **ppOwnerHostTask)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            GC_NOTRIGGER;
            ENTRY_POINT;;
        }
        CONTRACTL_END;

        BEGIN_ENTRYPOINT_NOTHROW;

        // Cookie is the SyncBlock
        // <TODO>TODO: Lifetime of Cookie?</TODO>
        AwareLock* pAwareLock = (AwareLock*)Cookie;
        IHostTask *pTask = NULL;
        Thread *pThread = pAwareLock->GetOwningThread();
        if (pThread)
        {
            ThreadStoreLockHolder tsLock;
            pThread = pAwareLock->GetOwningThread();
            if (pThread)
            {
                // See if the lock is orphaned, and the Thread object has been deleted
                Thread *pWalk = NULL;
                while ((pWalk = ThreadStore::GetAllThreadList(pWalk, 0, 0)) != NULL)
                {
                    if (pWalk == pThread)
                    {
                        pTask = pThread->GetHostTaskWithAddRef();
                        break;
                    }
                }
            }
        }

        *ppOwnerHostTask = pTask;

        END_ENTRYPOINT_NOTHROW;


        return S_OK;
    }
    virtual HRESULT STDMETHODCALLTYPE CreateRWLockOwnerIterator(SIZE_T Cookie,
                                                                SIZE_T *pIterator) {
        Thread *pThread = GetThread();

        // We may open a window for GC here.
        // A host should not hijack a coop thread to do deadlock detection.
        if (pThread && pThread->PreemptiveGCDisabled())
            return HOST_E_INVALIDOPERATION;

        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = E_FAIL;

#ifdef FEATURE_RWLOCK
        BEGIN_ENTRYPOINT_NOTHROW;
        ThreadStoreLockHolder tsLock;
        // Cookie is a weak handle.  We need to make sure that the object is not moving.
        CRWLock *pRWLock = *(CRWLock **) Cookie;
        *pIterator = NULL;
        if (pRWLock == NULL)
        {
            hr = S_OK;
        }
        else
        {
            hr = pRWLock->CreateOwnerIterator(pIterator);
        }
        END_ENTRYPOINT_NOTHROW;
#endif // FEATURE_RWLOCK

        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE GetRWLockOwnerNext(SIZE_T Iterator,
                                                         IHostTask **ppOwnerHostTask)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

#ifdef FEATURE_RWLOCK
        BEGIN_ENTRYPOINT_NOTHROW;
        CRWLock::GetNextOwner(Iterator,ppOwnerHostTask);
        END_ENTRYPOINT_NOTHROW;
#endif // FEATURE_RWLOCK

        return S_OK;
    }

    virtual HRESULT STDMETHODCALLTYPE DeleteRWLockOwnerIterator(SIZE_T Iterator)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

#ifdef FEATURE_RWLOCK
        BEGIN_ENTRYPOINT_NOTHROW;
        CRWLock::DeleteOwnerIterator(Iterator);
        END_ENTRYPOINT_NOTHROW;
#endif // FEATURE_RWLOCK

        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRSyncManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppvObject = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
};

static CLRSyncManager s_CLRSyncManager;

extern void HostIOCompletionCallback(DWORD ErrorCode,
                                     DWORD numBytesTransferred,
                                     LPOVERLAPPED lpOverlapped);
class CCLRIoCompletionManager :public ICLRIoCompletionManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE OnComplete(DWORD dwErrorCode,
                                                 DWORD NumberOfBytesTransferred,
                                                 void* pvOverlapped)
    {
        WRAPPER_NO_CONTRACT;
        STATIC_CONTRACT_ENTRY_POINT;

        if (pvOverlapped)
        {
            BEGIN_ENTRYPOINT_NOTHROW;
            HostIOCompletionCallback (dwErrorCode, NumberOfBytesTransferred, (LPOVERLAPPED)pvOverlapped);
            END_ENTRYPOINT_NOTHROW;
        }

        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
    BEGIN_INTERFACE HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRIoCompletionManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppvObject = this;
        return S_OK;
    }
};

static CCLRIoCompletionManager s_CLRIoCompletionManager;
#endif // FEATURE_CORECLR

#ifdef _DEBUG
extern void ValidateHostInterface();
#endif

// fusion's global copy of host assembly manager stuff
BOOL g_bFusionHosted = FALSE;
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
ICLRAssemblyReferenceList *g_pHostAsmList = NULL;
IHostAssemblyStore *g_pHostAssemblyStore = NULL;
#endif // FEATURE_INCLUDE_ALL_INTERFACES

/*static*/ BOOL CorHost2::IsLoadFromBlocked() // LoadFrom, LoadFile and Load(byte[]) are blocked in certain hosting scenarios
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    return (g_bFusionHosted && (g_pHostAsmList != NULL));
#else // !FEATURE_INCLUDE_ALL_INTERFACES
    return FALSE; // as g_pHostAsmList is not defined for CoreCLR; hence above expression will be FALSE.
#endif // FEATURE_INCLUDE_ALL_INTERFACES
}

static Volatile<BOOL> fOneOnly = 0;

///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::SetHostControl
///////////////////////////////////////////////////////////////////////////////
HRESULT CorHost2::SetHostControl(IHostControl* pHostControl)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;
    if (m_Version < 2)
        // CLR is hosted with v1 hosting interface.  Some part of v2 hosting API are disabled.
        return HOST_E_INVALIDOPERATION;

    if (pHostControl == 0)
        return E_INVALIDARG;

    // If Runtime has been started, do not allow setting HostMemoryManager
    if (g_fEEStarted)
        return E_ACCESSDENIED;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    
    DWORD dwSwitchCount = 0;

    while (FastInterlockExchange((LONG*)&fOneOnly, 1) == 1)
    {
    #ifndef FEATURE_CORECLR
        if (m_HostTaskManager != NULL)
        {
            m_HostTaskManager->SwitchToTask(0);
        }
        else
        {
            IHostTaskManager *pHostTaskManager = NULL;
            if (pHostControl->GetHostManager(IID_IHostTaskManager, (void**)&pHostTaskManager) == S_OK &&
                pHostTaskManager != NULL)
            {
                pHostTaskManager->SwitchToTask(0);
                pHostTaskManager->Release();
            }
            else
            {
                __SwitchToThread(0, ++dwSwitchCount);
            }
        }
    #else
             __SwitchToThread(0, ++dwSwitchCount);
    #endif //  FEATURE_CORECLR        
    }
    
#ifndef FEATURE_CORECLR

#ifdef _DEBUG
    ValidateHostInterface();
#endif

#ifdef _DEBUG
    DWORD dbg_HostManagerConfig = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_HostManagerConfig);
#endif

    IHostMemoryManager *memoryManager = NULL;
    IHostTaskManager *taskManager = NULL;
    IHostThreadpoolManager *threadpoolManager = NULL;
    IHostIoCompletionManager *ioCompletionManager = NULL;
    IHostSyncManager *syncManager = NULL;
    IHostAssemblyManager *assemblyManager = NULL;
    IHostGCManager *gcManager = NULL;
    IHostSecurityManager *securityManager = NULL;
    IHostPolicyManager *policyManager = NULL;

    if (m_HostMemoryManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRMEMORYHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostMemoryManager,(void**)&memoryManager) == S_OK &&
        memoryManager != NULL) {
        if (m_HostMalloc == NULL)
        {
            hr = memoryManager->CreateMalloc (MALLOC_THREADSAFE, &m_HostMalloc);
            if (hr == S_OK)
            {
                memoryManager->RegisterMemoryNotificationCallback(&s_MemoryNotification);
            }
            else
            {
                memoryManager->Release();
                IfFailGo(E_UNEXPECTED);
            }
        }
        m_HostMemoryManager = memoryManager;
        g_fHostConfig |= CLRMEMORYHOSTED;
    }

    if (m_HostTaskManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRTASKHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostTaskManager,(void**)&taskManager) == S_OK &&
        taskManager != NULL) {
#ifdef _TARGET_ARM_ // @ARMTODO: re-enable once we support hosted p/invokes.
        IfFailGo(E_NOTIMPL);
#endif
        m_HostTaskManager = taskManager;
        m_HostTaskManager->SetCLRTaskManager(&s_CLRTaskManager);
        g_fHostConfig |= CLRTASKHOSTED;
    }

    if (m_HostThreadpoolManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRTHREADPOOLHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostThreadpoolManager,(void**)&threadpoolManager) == S_OK &&
        threadpoolManager != NULL) {
        m_HostThreadpoolManager = threadpoolManager;
        g_fHostConfig |= CLRTHREADPOOLHOSTED;
    }

    if (m_HostIoCompletionManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRIOCOMPLETIONHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostIoCompletionManager,(void**)&ioCompletionManager) == S_OK &&
        ioCompletionManager != NULL) {
        DWORD hostSize;
        hr = ioCompletionManager->GetHostOverlappedSize(&hostSize);
        if (FAILED(hr))
        {
            ioCompletionManager->Release();
            IfFailGo(E_UNEXPECTED);
        }
        m_HostOverlappedExtensionSize = (int)hostSize;
        m_HostIoCompletionManager = ioCompletionManager;
        m_HostIoCompletionManager->SetCLRIoCompletionManager(&s_CLRIoCompletionManager);
        g_fHostConfig |= CLRIOCOMPLETIONHOSTED;
    }

    if (m_HostSyncManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRSYNCHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostSyncManager,(void**)&syncManager) == S_OK &&
        syncManager != NULL) {
        m_HostSyncManager = syncManager;
        m_HostSyncManager->SetCLRSyncManager(&s_CLRSyncManager);
        g_fHostConfig |= CLRSYNCHOSTED;
    }

    if (m_HostAssemblyManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRASSEMBLYHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostAssemblyManager,(void**)&assemblyManager) == S_OK &&
        assemblyManager != NULL) {

        assemblyManager->GetAssemblyStore(&g_pHostAssemblyStore);

        hr = assemblyManager->GetNonHostStoreAssemblies(&g_pHostAsmList);
        if (FAILED(hr))
        {
            assemblyManager->Release();
            IfFailGo(hr);
        }

        if (g_pHostAssemblyStore || g_pHostAsmList)
            g_bFusionHosted = TRUE;
        m_HostAssemblyManager = assemblyManager;
        g_fHostConfig |= CLRASSEMBLYHOSTED;
    }

    if (m_HostGCManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRGCHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostGCManager,
                                     (void**)&gcManager) == S_OK &&
        gcManager != NULL) {
        m_HostGCManager = gcManager;
        g_fHostConfig |= CLRGCHOSTED;
    }

    if (m_HostSecurityManager == NULL &&
#ifdef _DEBUG
        (dbg_HostManagerConfig & CLRSECURITYHOSTED) &&
#endif
        pHostControl->GetHostManager(IID_IHostSecurityManager,
                                     (void**)&securityManager) == S_OK &&
        securityManager != NULL) {
        g_fHostConfig |= CLRSECURITYHOSTED;
        m_HostSecurityManager = securityManager;
#ifdef FEATURE_CAS_POLICY
        HostExecutionContextManager::InitializeRestrictedContext();
#endif // #ifdef FEATURE_CAS_POLICY
    }

    if (m_HostPolicyManager == NULL &&
        pHostControl->GetHostManager(IID_IHostPolicyManager,
                                     (void**)&policyManager) == S_OK &&
        policyManager != NULL) {
        m_HostPolicyManager = policyManager;
    }
#endif //!FEATURE_CORECLR

    if (m_HostControl == NULL)
    {
        m_HostControl = pHostControl;
        m_HostControl->AddRef();
    }

    goto ErrExit;

ErrExit:
    fOneOnly = 0;

    END_ENTRYPOINT_NOTHROW;

    return hr;
}

class CCLRPolicyManager: public ICLRPolicyManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE SetDefaultAction(EClrOperation operation,
                                                       EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR    
        STATIC_CONTRACT_ENTRY_POINT;
        HRESULT hr;
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetDefaultAction(operation, action);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR        
    }

    virtual HRESULT STDMETHODCALLTYPE SetTimeout(EClrOperation operation,
                                                 DWORD dwMilliseconds)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR    
        STATIC_CONTRACT_ENTRY_POINT;
        HRESULT hr;
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetTimeout(operation,dwMilliseconds);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR          
    }

    virtual HRESULT STDMETHODCALLTYPE SetActionOnTimeout(EClrOperation operation,
                                                         EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR    
        STATIC_CONTRACT_ENTRY_POINT;
        HRESULT hr;
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetActionOnTimeout(operation,action);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR          
    }

    virtual HRESULT STDMETHODCALLTYPE SetTimeoutAndAction(EClrOperation operation, DWORD dwMilliseconds,
                                                          EPolicyAction action)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR    
        STATIC_CONTRACT_SO_TOLERANT;
        HRESULT hr;
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetTimeoutAndAction(operation,dwMilliseconds,action);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR          
    }

    virtual HRESULT STDMETHODCALLTYPE SetActionOnFailure(EClrFailure failure,
                                                         EPolicyAction action)
    {
        // This is enabled for CoreCLR since a host can use this to 
        // specify action for handling AV.
        STATIC_CONTRACT_ENTRY_POINT;
        LIMITED_METHOD_CONTRACT;
        HRESULT hr;
#ifdef FEATURE_CORECLR
        // For CoreCLR, this method just supports FAIL_AccessViolation as a valid
        // failure input arg. The validation of the specified action for the failure
        // will be done in EEPolicy::IsValidActionForFailure.
        if (failure != FAIL_AccessViolation)
        {
            return E_INVALIDARG;
        }
#endif // FEATURE_CORECLR        
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetActionOnFailure(failure,action);
        END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE SetUnhandledExceptionPolicy(EClrUnhandledException policy)
    {
        LIMITED_METHOD_CONTRACT;
#ifndef FEATURE_CORECLR    
        STATIC_CONTRACT_ENTRY_POINT;
        HRESULT hr;
        BEGIN_ENTRYPOINT_NOTHROW;
        hr = GetEEPolicy()->SetUnhandledExceptionPolicy(policy);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR          
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        return 1;
    }

    BEGIN_INTERFACE HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                             void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        STATIC_CONTRACT_SO_TOLERANT;
        if (riid != IID_ICLRPolicyManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);

        // Ensure that the out going pointer is not null
        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = this;
        return S_OK;
    }
};

static CCLRPolicyManager s_PolicyManager;

#ifndef FEATURE_CORECLR // not supported
class CCLROnEventManager: public ICLROnEventManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE RegisterActionOnEvent(EClrEvent event,
                                                            IActionOnCLREvent *pAction)
    {
        CONTRACTL
        {
            GC_TRIGGERS;
            NOTHROW;
            ENTRY_POINT;

            // This function is always called from outside the Runtime. So, we assert that we either don't have a
            // managed thread, or if we do, that we're in preemptive GC mode.
            PRECONDITION((GetThread() == NULL) || !GetThread()->PreemptiveGCDisabled());
        }
        CONTRACTL_END;

        if (event >= MaxClrEvent || pAction == NULL || event < (EClrEvent)0)
            return E_INVALIDARG;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        // Note: its only safe to use a straight ReleaseHolder from within the VM directory when we know we're
        // called from outside the Runtime. We assert that above, just to be sure.
        ReleaseHolder<IActionOnCLREvent>  actionHolder(pAction);
        pAction->AddRef();

        CrstHolderWithState ch(m_pLock);

        DWORD dwSwitchCount = 0;
        while (m_ProcessEvent != 0)
        {
            ch.Release();
            __SwitchToThread(0, ++dwSwitchCount);
            ch.Acquire();
        }

        if (m_pAction[event] == NULL)
        {
            m_pAction[event] = new (nothrow)ActionNode;
            if (m_pAction[event] == NULL)
                hr = E_OUTOFMEMORY;
        }

        if (SUCCEEDED(hr))
        {
            ActionNode *walk = m_pAction[event];
            while (TRUE)
            {
                int n = 0;
                for ( ; n < ActionNode::ActionArraySize; n ++)
                {
                    if (walk->pAction[n] == NULL)
                    {
                        walk->pAction[n] = pAction;
                        actionHolder.SuppressRelease();
                        hr = S_OK;
                        break;
                    }
                }
                if (n < ActionNode::ActionArraySize)
                {
                    break;
                }
                if (walk->pNext == NULL)
                {
                    walk->pNext = new (nothrow) ActionNode;
                    if (walk->pNext == NULL)
                    {
                        hr = E_OUTOFMEMORY;
                        break;
                    }
                }
                walk = walk->pNext;
            }
        }

        END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE UnregisterActionOnEvent(EClrEvent event,
                                                              IActionOnCLREvent *pAction)
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            NOTHROW;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        if (event == Event_StackOverflow)
        {
            // We don't want to take a lock when we process StackOverflow event, because we may
            // not have enough stack to do it.
            // So we do not release our cache of the callback in order to avoid race.
            return HOST_E_INVALIDOPERATION;
        }

        HRESULT hr = S_OK;

        ActionNode *walk = NULL;
        ActionNode *prev = NULL;


        BEGIN_ENTRYPOINT_NOTHROW;

        CrstHolderWithState ch(m_pLock);

        DWORD dwSwitchCount = 0;
        while (m_ProcessEvent != 0)
        {
            ch.Release();
            __SwitchToThread(0, ++dwSwitchCount);
            ch.Acquire();
        }

        if (m_pAction[event] == NULL)
            IfFailGo(HOST_E_INVALIDOPERATION);

        walk = m_pAction[event];
        while (walk)
        {
            BOOL fInUse = FALSE;
            for (int n = 0; n < ActionNode::ActionArraySize; n ++)
            {
                if (prev && !fInUse && walk->pAction[n])
                    fInUse = TRUE;
                if (walk->pAction[n] == pAction)
                {
                    walk->pAction[n] = NULL;
                    ch.Release();
                    pAction->Release();
                    hr = S_OK;
                    goto ErrExit;
                }
            }
            if (prev && !fInUse)
            {
                prev->pNext = walk->pNext;
                delete walk;
                walk = prev;
            }
            prev = walk;
            walk = walk->pNext;
        }
        hr = HOST_E_INVALIDOPERATION;
ErrExit:
        END_ENTRYPOINT_NOTHROW;

        return hr;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppUnk)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLROnEventManager && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppUnk = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    // This function is to work around an issue in scan.exe.
    // scan.exe is not smart to handle that if (){} else {}.
    void ProcessSOEvent(void *data)
    {
        STATIC_CONTRACT_SO_TOLERANT;
        WRAPPER_NO_CONTRACT;

        if (m_pLock == NULL)
            return;

        ActionNode *walk = m_pAction[Event_StackOverflow];

            while (walk)
            {
                for (int n = 0; n < ActionNode::ActionArraySize; n ++)
                {
                    if (walk->pAction[n])
                    {
                    walk->pAction[n]->OnEvent(Event_StackOverflow,data);
                    }
                }
                walk = walk->pNext;
            }
    }

    void ProcessEvent(EClrEvent event, void *data)
    {
        WRAPPER_NO_CONTRACT;

        if (m_pLock == NULL)
        {
            return;
        }

        _ASSERTE (event != Event_StackOverflow);

        {
            CrstHolder ch(m_pLock);

            if (event == Event_ClrDisabled)
            {
                if (m_CLRDisabled)
                {
                    return;
                }
                m_CLRDisabled = TRUE;
            }
            m_ProcessEvent ++;

            // Release the lock around the call into the host. Is this correct?
            // It seems that we need to hold the lock except for the actual callback itself.
        }

        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        {
        ActionNode *walk = m_pAction[event];
        while (walk)
        {
            for (int n = 0; n < ActionNode::ActionArraySize; n ++)
            {
                if (walk->pAction[n])
                {
                    walk->pAction[n]->OnEvent(event,data);
                }
            }
            walk = walk->pNext;
        }
        }    
        END_SO_TOLERANT_CODE_CALLING_HOST;

        {
            CrstHolder ch(m_pLock);
            m_ProcessEvent --;
        }
    }

    BOOL IsActionRegisteredForEvent(EClrEvent event)
    {
        WRAPPER_NO_CONTRACT;

        // Check to see if the event manager has been set up.
        if (m_pLock == NULL)
            return FALSE;

        CrstHolder ch(m_pLock);

        ActionNode *walk = m_pAction[event];
        while (walk)
        {
            for (int n = 0; n < ActionNode::ActionArraySize; n ++)
            {
                if (walk->pAction[n] != NULL)
                {
                    // We found an action registered for this event.
                    return TRUE;
                }
            }
            walk = walk->pNext;
        }

        // There weren't any actions registered.
        return FALSE;
    }

    HRESULT Init()
    {
        STATIC_CONTRACT_NOTHROW;
        STATIC_CONTRACT_GC_NOTRIGGER;
        STATIC_CONTRACT_MODE_ANY;
        STATIC_CONTRACT_SO_TOLERANT;

        HRESULT hr = S_OK;
        if (m_pLock == NULL)
        {
            EX_TRY
            {
                BEGIN_SO_INTOLERANT_CODE(GetThread());
                {
                InitHelper();
            }
                END_SO_INTOLERANT_CODE;
            }
            EX_CATCH
            {
                hr = GET_EXCEPTION()->GetHR();
            }
            EX_END_CATCH(SwallowAllExceptions);
        }

        return hr;
    }

#if 0
    // We do not need this one.  We have one instance of this class
    // and it is static.
    CCLROnEventManager()
    {
        LIMITED_METHOD_CONTRACT;
        for (int n = 0; n < MaxClrEvent; n ++)
            m_pAction[n] = NULL;
    }
#endif

private:
    struct ActionNode
    {
        static const int ActionArraySize = 8;

        IActionOnCLREvent *pAction[ActionArraySize];
        ActionNode        *pNext;

        ActionNode ()
        : pNext(NULL)
        {
            LIMITED_METHOD_CONTRACT;

            for (int n = 0; n < ActionArraySize; n ++)
                pAction[n] = 0;
        }
    };
    ActionNode *m_pAction[MaxClrEvent];

    Crst* m_pLock;

    BOOL m_CLRDisabled;

    // We can not call out into host while holding the lock.  At the same time
    // we need to make our data consistent.  Therefore, m_ProcessEvent is a marker
    // to forbid touching the data structure from Register and UnRegister.
    DWORD m_ProcessEvent;

    void InitHelper()
    {
        CONTRACTL
        {
            GC_NOTRIGGER;
            THROWS;
            MODE_ANY;
        }
        CONTRACTL_END;

        m_ProcessEvent = 0;

        Crst* tmp = new Crst(CrstOnEventManager, CrstFlags(CRST_DEFAULT | CRST_DEBUGGER_THREAD));
        if (FastInterlockCompareExchangePointer(&m_pLock, tmp, NULL) != NULL)
            delete tmp;
    }
};

static CCLROnEventManager s_OnEventManager;
#endif // FEATURE_CORECLR


void ProcessEventForHost(EClrEvent event, void *data)
{
#ifndef FEATURE_CORECLR
    WRAPPER_NO_CONTRACT;

    _ASSERTE (event != Event_StackOverflow);

    GCX_PREEMP();

    s_OnEventManager.ProcessEvent(event,data);
#endif // FEATURE_CORECLR
}

// We do not call ProcessEventForHost for stack overflow, since we have limit stack
// and we should avoid calling GCX_PREEMPT
void ProcessSOEventForHost(EXCEPTION_POINTERS *pExceptionInfo, BOOL fInSoTolerant)
{
#ifndef FEATURE_CORECLR
    WRAPPER_NO_CONTRACT;

    StackOverflowInfo soInfo;
    if (fInSoTolerant)
    {
        soInfo.soType = SO_Managed;
    }
    else if (pExceptionInfo == NULL || IsIPInModule(g_pMSCorEE, GetIP(pExceptionInfo->ContextRecord)))
    {
        soInfo.soType = SO_ClrEngine;
    }
    else
    {
        soInfo.soType = SO_Other;
    }

    soInfo.pExceptionInfo = pExceptionInfo;
    s_OnEventManager.ProcessSOEvent(&soInfo);
#endif // FEATURE_CORECLR
}

BOOL IsHostRegisteredForEvent(EClrEvent event)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_CORECLR
    return FALSE;
#else // FEATURE_CORECLR
    return s_OnEventManager.IsActionRegisteredForEvent(event);
#endif // FEATURE_CORECLR
}

inline size_t SizeInKBytes(size_t cbSize)
{
    LIMITED_METHOD_CONTRACT;
    size_t cb = (cbSize % 1024) ? 1 : 0;
    return ((cbSize / 1024) + cb);
}

SIZE_T Host_SegmentSize = 0;
SIZE_T Host_MaxGen0Size = 0;
BOOL  Host_fSegmentSizeSet = FALSE;
BOOL  Host_fMaxGen0SizeSet = FALSE;

void UpdateGCSettingFromHost ()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE (g_pConfig);
    if (Host_fSegmentSizeSet)
    {
        g_pConfig->SetSegmentSize(Host_SegmentSize);
    }
    if (Host_fMaxGen0SizeSet)
    {
        g_pConfig->SetGCgen0size(Host_MaxGen0Size);
    }
}

#if !defined(FEATURE_CORECLR) || defined(FEATURE_WINDOWSPHONE)
class CCLRGCManager: public ICLRGCManager2
{
public:
    virtual HRESULT STDMETHODCALLTYPE Collect(LONG Generation)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;

        if (Generation > (int) GCHeap::GetGCHeap()->GetMaxGeneration())
            hr = E_INVALIDARG;

        if (SUCCEEDED(hr))
        {
            // Set up a Thread object if this is called on a native thread.
            Thread *pThread;
            pThread = GetThread();
            if (pThread == NULL)
                pThread = SetupThreadNoThrow(&hr);
            if (pThread != NULL)
            {
                BEGIN_ENTRYPOINT_NOTHROW_WITH_THREAD(pThread);
                GCX_COOP();

                EX_TRY
                {
                    STRESS_LOG0(LF_GC, LL_INFO100, "Host triggers GC\n");
                    hr = GCHeap::GetGCHeap()->GarbageCollect(Generation);
                }
                EX_CATCH
                {
                    hr = GET_EXCEPTION()->GetHR();
                }
                EX_END_CATCH(SwallowAllExceptions);

                END_ENTRYPOINT_NOTHROW_WITH_THREAD;
            }
        }

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetStats(COR_GC_STATS *pStats)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

    #if defined(ENABLE_PERF_COUNTERS)

        Perf_GC     *pgc = &GetPerfCounters().m_GC;

        if (!pStats)
            IfFailGo(E_INVALIDARG);

        if (pStats->Flags & COR_GC_COUNTS)
        {
            pStats->ExplicitGCCount = pgc->cInducedGCs;

            for (int idx=0; idx<3; idx++)
                pStats->GenCollectionsTaken[idx] = pgc->cGenCollections[idx];
        }

        if (pStats->Flags & COR_GC_MEMORYUSAGE)
        {
            pStats->CommittedKBytes = SizeInKBytes(pgc->cTotalCommittedBytes);
            pStats->ReservedKBytes = SizeInKBytes(pgc->cTotalReservedBytes);
            pStats->Gen0HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[0]);
            pStats->Gen1HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[1]);
            pStats->Gen2HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[2]);
            pStats->LargeObjectHeapSizeKBytes = SizeInKBytes(pgc->cLrgObjSize);
            pStats->KBytesPromotedFromGen0 = SizeInKBytes(pgc->cbPromotedMem[0]);
            pStats->KBytesPromotedFromGen1 = SizeInKBytes(pgc->cbPromotedMem[1]);
        }
        hr = S_OK;
ErrExit:
    #else
        hr = E_NOTIMPL;
    #endif // ENABLE_PERF_COUNTERS

        END_ENTRYPOINT_NOTHROW;
        return hr;
    }
    virtual HRESULT STDMETHODCALLTYPE SetGCStartupLimits(
        DWORD SegmentSize,
        DWORD MaxGen0Size)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;

        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        // Set default overrides if specified by caller.
        if (SegmentSize != (DWORD) ~0 && SegmentSize > 0)
        {
            hr = _SetGCSegmentSize(SegmentSize);
        }

        if (SUCCEEDED(hr) && MaxGen0Size != (DWORD) ~0 && MaxGen0Size > 0)
        {
            hr = _SetGCMaxGen0Size(MaxGen0Size);
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE SetGCStartupLimitsEx(
        SIZE_T SegmentSize,
        SIZE_T MaxGen0Size)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;

        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        // Set default overrides if specified by caller.
        if (SegmentSize != (SIZE_T) ~0 && SegmentSize > 0)
        {
            hr = _SetGCSegmentSize(SegmentSize);
        }

        if (SUCCEEDED(hr) && MaxGen0Size != (SIZE_T) ~0 && MaxGen0Size > 0)
        {
            hr = _SetGCMaxGen0Size(MaxGen0Size);
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, OUT PVOID *ppUnk)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRGCManager && riid != IID_ICLRGCManager2 && riid != IID_IUnknown)
            return (E_NOINTERFACE);
        *ppUnk = this;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
private:
    HRESULT _SetGCSegmentSize(SIZE_T SegmentSize);
    HRESULT _SetGCMaxGen0Size(SIZE_T MaxGen0Size);
};


HRESULT CCLRGCManager::_SetGCSegmentSize(SIZE_T SegmentSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value, it must be a power of two and big enough.
    if (!GCHeap::IsValidSegmentSize(SegmentSize))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_SegmentSize = SegmentSize;
        Host_fSegmentSizeSet = TRUE;
    }

    return (hr);
}

HRESULT CCLRGCManager::_SetGCMaxGen0Size(SIZE_T MaxGen0Size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value is at least large enough.
    if (!GCHeap::IsValidGen0MaxSize(MaxGen0Size))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_MaxGen0Size = MaxGen0Size;
        Host_fMaxGen0SizeSet = TRUE;
    }

    return (hr);
}

static CCLRGCManager s_GCManager;
#endif // !FEATURE_CORECLR || FEATURE_WINDOWSPHONE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
class CCLRAppDomainResourceMonitor : public ICLRAppDomainResourceMonitor
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetCurrentAllocated(DWORD dwAppDomainId, 
                                                          ULONGLONG* pBytesAllocated)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        SystemDomain::LockHolder lh;
        AppDomainFromIDHolder pAppDomain((ADID)dwAppDomainId, TRUE, AppDomainFromIDHolder::SyncType_ADLock);

        if (!pAppDomain.IsUnloaded())
        {
            if (pBytesAllocated)
            {
                *pBytesAllocated = pAppDomain->GetAllocBytes();
            }
        }
        else
        {
            hr = COR_E_APPDOMAINUNLOADED;
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentSurvived(DWORD dwAppDomainId, 
                                                         ULONGLONG* pAppDomainBytesSurvived, 
                                                         ULONGLONG* pTotalBytesSurvived)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT     hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        SystemDomain::LockHolder lh;
        AppDomainFromIDHolder pAppDomain((ADID)dwAppDomainId, TRUE, AppDomainFromIDHolder::SyncType_ADLock);

        if (pAppDomain.IsUnloaded())
        {
            hr = COR_E_APPDOMAINUNLOADED;
        }
        else
        {
            if (pAppDomainBytesSurvived)
            {
                *pAppDomainBytesSurvived = pAppDomain->GetSurvivedBytes();
            }
            if (pTotalBytesSurvived)
            {
                *pTotalBytesSurvived = SystemDomain::GetTotalSurvivedBytes();
            }
        }

        END_ENTRYPOINT_NOTHROW;

        return (hr);
    }

    virtual HRESULT STDMETHODCALLTYPE GetCurrentCpuTime(DWORD dwAppDomainId, 
                                                        ULONGLONG* pMilliseconds)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        {
            SystemDomain::LockHolder lh;
    
            {
                AppDomainFromIDHolder pAppDomain((ADID)dwAppDomainId, TRUE, AppDomainFromIDHolder::SyncType_ADLock);

                if (!pAppDomain.IsUnloaded())
                {
                    if (pMilliseconds)
                    {
                        *pMilliseconds = pAppDomain->QueryProcessorUsage() / 10000;
                    }
                }
                else
                {
                    hr = COR_E_APPDOMAINUNLOADED;
                }
            }
        }

        END_ENTRYPOINT_NOTHROW;

        return hr;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, OUT PVOID *ppUnk)
    {
        LIMITED_METHOD_CONTRACT;
        *ppUnk = NULL;
        if (riid == IID_IUnknown)
            *ppUnk = (IUnknown*)this;
        else if (riid == IID_ICLRAppDomainResourceMonitor)
            *ppUnk = (ICLRAppDomainResourceMonitor*)this;
        else
            return E_NOINTERFACE;
        return S_OK;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }
};
static CCLRAppDomainResourceMonitor s_Arm;
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING

#ifdef FEATURE_APTCA
class CLRDomainManager : public ICLRDomainManager
{
public:
    virtual HRESULT STDMETHODCALLTYPE SetAppDomainManagerType(__in LPCWSTR wszAppDomainManagerAssembly,
                                                              __in LPCWSTR wszAppDomainManagerType,
                                                              EInitializeNewDomainFlags dwInitializeDomainFlags)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        hr = CorHost2::SetAppDomainManagerType(wszAppDomainManagerAssembly,
                                               wszAppDomainManagerType,
                                               dwInitializeDomainFlags);
        END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual HRESULT STDMETHODCALLTYPE SetPropertiesForDefaultAppDomain(DWORD nProperties,
                                                                       __in_ecount(nProperties) LPCWSTR *pwszPropertyNames,
                                                                       __in_ecount(nProperties) LPCWSTR *pwszPropertyValues)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_ENTRYPOINT_NOTHROW;

        hr = CorHost2::SetPropertiesForDefaultAppDomain(nProperties, pwszPropertyNames, pwszPropertyValues);

        END_ENTRYPOINT_NOTHROW;
        return hr;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef()
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release()
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual HRESULT STDMETHODCALLTYPE QueryInterface(__in REFIID riid, __out LPVOID *ppvObject)
    {
        LIMITED_METHOD_CONTRACT;

        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = NULL;

        if (riid == IID_ICLRDomainManager)
        {
            *ppvObject = this;
        }
        else if (riid == IID_IUnknown)
        {
            *ppvObject = static_cast<IUnknown *>(this);
        }

        if (*ppvObject == NULL)
            return E_NOINTERFACE;

        AddRef();
        return S_OK;
    }
};

static CLRDomainManager s_CLRDomainManager;
#endif // FEATURE_APTCA

BYTE g_CorHostProtectionManagerInstance[sizeof(CorHostProtectionManager)];

void InitHostProtectionManager()
{
    WRAPPER_NO_CONTRACT;
    new (g_CorHostProtectionManagerInstance) CorHostProtectionManager();
}

BOOL g_CLRPolicyRequested = FALSE;

class CCorCLRControl: public ICLRControl
{
public:
    virtual HRESULT STDMETHODCALLTYPE GetCLRManager(REFIID riid, void **ppObject)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SO_TOLERANT;    // no global state updates
        }
        CONTRACTL_END;

        // Sanity check.
        if (ppObject == NULL)
            return E_INVALIDARG;

#ifndef FEATURE_CORECLR
        // ErrorReportingManager is allowed, even if runtime is started, so
        //  make this check first.
        // Host must call release on CLRErrorReportingManager after this call
        if (riid == IID_ICLRErrorReportingManager)
        {
            *ppObject = &g_CLRErrorReportingManager;
            return S_OK;
        }
        else 
#elif defined(FEATURE_WINDOWSPHONE)
        if (riid == IID_ICLRErrorReportingManager2)
        {
            *ppObject = &g_CLRErrorReportingManager;
            return S_OK;
        }
        else
#endif // !FEATURE_CORECLR || defined(FEATURE_WINDOWSPHONE)
        if (g_fEEStarted && !m_fFullAccess)
        {
            // If runtime has been started, do not allow user to obtain CLR managers.
            return HOST_E_INVALIDOPERATION;
        }
#ifndef FEATURE_CORECLR
        else if (riid == IID_ICLRTaskManager) {
            *ppObject = &s_CLRTaskManager;
            return S_OK;
        }
#endif // !FEATURE_CORECLR      

        // CoreCLR supports ICLRPolicyManager since it allows the host
        // to specify the policy for AccessViolation.  
        else if (riid == IID_ICLRPolicyManager) {
            *ppObject = &s_PolicyManager;
            FastInterlockExchange((LONG*)&g_CLRPolicyRequested, TRUE);
            return S_OK;
        }
#ifndef FEATURE_CORECLR        
        else if (riid == IID_ICLRHostProtectionManager) {
            *ppObject = GetHostProtectionManager();
            return S_OK;
        }

        // Host must call release on CLRDebugManager after this call
        else if (riid == IID_ICLRDebugManager)
        {
            *ppObject = &s_CLRDebugManager;
            return S_OK;
        }

        else if (riid == IID_ICLROnEventManager)
        {
            HRESULT hr = s_OnEventManager.Init();
            if (FAILED(hr))
                return hr;
            *ppObject = &s_OnEventManager;
            return S_OK;
        }
#endif // !FEATURE_CORECLR

#if !defined(FEATURE_CORECLR) || defined(FEATURE_WINDOWSPHONE)
        else if ((riid == IID_ICLRGCManager) || (riid == IID_ICLRGCManager2))
        {
            *ppObject = &s_GCManager;
            return S_OK;
        }
#endif // !FEATURE_CORECLR || FEATURE_WINDOWSPHONE

#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
        else if (riid == IID_ICLRAppDomainResourceMonitor)
        {
            EnableARM();
            *ppObject = &s_Arm;
            return S_OK;
        }
#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING
#ifdef FEATURE_APTCA
        else if (riid == IID_ICLRDomainManager)
        {
            *ppObject = &s_CLRDomainManager;
            return S_OK;
        }
#endif // FEATURE_APTCA
        else
            return (E_NOINTERFACE);
    }

    virtual HRESULT STDMETHODCALLTYPE SetAppDomainManagerType(
        LPCWSTR pwzAppDomainManagerAssembly,
        LPCWSTR pwzAppDomainManagerType)
    {
#ifndef FEATURE_CORECLR    
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            ENTRY_POINT;    // no global state updates
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;

        BEGIN_ENTRYPOINT_NOTHROW;

        hr = CorHost2::SetAppDomainManagerType(pwzAppDomainManagerAssembly,
                                               pwzAppDomainManagerType,
                                               eInitializeNewDomainFlags_None);
        END_ENTRYPOINT_NOTHROW;
        return hr;
#else // FEATURE_CORECLR
        
        // CoreCLR does not support this method
        return E_NOTIMPL;
#endif // !FEATURE_CORECLR        
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void)
    {
        LIMITED_METHOD_CONTRACT;
        return 1;
    }

    BEGIN_INTERFACE HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                             void **ppvObject)
    {
        LIMITED_METHOD_CONTRACT;
        if (riid != IID_ICLRControl && riid != IID_IUnknown)
            return (E_NOINTERFACE);

        // Ensure that the out going pointer is not null
        if (ppvObject == NULL)
            return E_POINTER;

        *ppvObject = this;
        return S_OK;
    }

    // This is to avoid having ctor.  We have static objects, and it is
    // difficult to support ctor on certain platform.
    void SetAccess(BOOL fFullAccess)
    {
        LIMITED_METHOD_CONTRACT;
        m_fFullAccess = fFullAccess;
    }
private:
    BOOL m_fFullAccess;
};

// Before CLR starts, we give out s_CorCLRControl which has full access to all managers.
// After CLR starts, we give out s_CorCLRControlLimited which allows limited access to managers.
static CCorCLRControl s_CorCLRControl;

#ifndef FEATURE_CORECLR
static CCorCLRControl s_CorCLRControlLimited;
#endif // FEATURE_CORECLR

///////////////////////////////////////////////////////////////////////////////
// ICLRRuntimeHost::GetCLRControl
HRESULT CorHost2::GetCLRControl(ICLRControl** pCLRControl)
{
    LIMITED_METHOD_CONTRACT;
    
    // Ensure that we have a valid pointer
    if (pCLRControl == NULL)
    {
        return E_POINTER;
    }

    HRESULT hr = S_OK;
    
    STATIC_CONTRACT_ENTRY_POINT;
    BEGIN_ENTRYPOINT_NOTHROW;
    if (!g_fEEStarted && m_Version >= 2)
    {
        s_CorCLRControl.SetAccess(TRUE);
        *pCLRControl = &s_CorCLRControl;
    }
    else
    {
#ifndef FEATURE_CORECLR
        // Even CLR is hosted by v1 hosting interface, we still allow part of CLRControl, like IID_ICLRErrorReportingManager.
        s_CorCLRControlLimited.SetAccess(FALSE);
        *pCLRControl = &s_CorCLRControlLimited;
#else // FEATURE_CORECLR
        // If :
        // 1) request comes for interface other than ICLRControl*, OR
        // 2) runtime has already started, OR
        // 3) version is not 2
        //
        // we will return failure and set the out pointer to NULL
        *pCLRControl = NULL;
        if (g_fEEStarted)
        {
            // Return HOST_E_INVALIDOPERATION as per MSDN if runtime has already started
            hr = HOST_E_INVALIDOPERATION;
        }
        else
        {
            hr = E_NOTIMPL;
        }
#endif // !FEATURE_CORECLR    
    }
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#ifndef FEATURE_CORECLR

// static
HRESULT CorHost2::SetPropertiesForDefaultAppDomain(DWORD nProperties,
                                                    __in_ecount(nProperties) LPCWSTR *pwszPropertyNames,
                                                    __in_ecount(nProperties) LPCWSTR *pwszPropertyValues)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Default domain properties can only be set before the CLR has started
    if (g_fEEStarted || HasStarted())
    {
        return HOST_E_INVALIDOPERATION;
    }

    // If the host is specifying properties, they should be there
    if (nProperties > 0 && (pwszPropertyNames == NULL || pwszPropertyValues == NULL))
    {
        return E_POINTER;
    }

    // v4 - since this property is being added late in the cycle to address a specific scenario, we
    // reject any attempt to set anything but a single well known property name.  This restriction
    // can be removed in the future.
    for (DWORD iProperty = 0; iProperty < nProperties; ++iProperty)
    {
        if (pwszPropertyNames[iProperty] == NULL)
        {
            return E_POINTER;
        }
        if (pwszPropertyValues[iProperty] == NULL)
        {
            return E_POINTER;
        }
        if (wcscmp(PARTIAL_TRUST_VISIBLE_ASSEMBLIES_PROPERTY, pwszPropertyNames[iProperty]) != 0)
        {
            return HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY);
        }
    }

    HRESULT hr = S_OK;

    EX_TRY
    {
        for (DWORD iProperty = 0; iProperty < nProperties; ++iProperty)
        {
            SString propertyName(pwszPropertyNames[iProperty]);
            s_defaultDomainPropertyNames.Append(propertyName);

            SString propertyValue(pwszPropertyValues[iProperty]);
            s_defaultDomainPropertyValues.Append(propertyValue);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// static
HRESULT CorHost2::SetAppDomainManagerType(LPCWSTR wszAppDomainManagerAssembly,
                                          LPCWSTR wszAppDomainManagerType,
                                          EInitializeNewDomainFlags dwInitializeDomainFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The AppDomainManger can only be set by the host before the CLR has started
    if (g_fEEStarted || HasStarted())
    {
        return HOST_E_INVALIDOPERATION;
    }

    // Both the type and assembly must be specified
    if (wszAppDomainManagerAssembly == NULL || wszAppDomainManagerType == NULL)
    {
        return E_INVALIDARG;
    }
    
    // Make sure we understand the incoming flags
    const EInitializeNewDomainFlags knownFlags = eInitializeNewDomainFlags_NoSecurityChanges;
    if ((dwInitializeDomainFlags & (~knownFlags)) != eInitializeNewDomainFlags_None)
    {
        return E_INVALIDARG;
    }

    // Get a copy of the AppDomainManager assembly
    size_t cchAsm = wcslen(wszAppDomainManagerAssembly) + 1;
    NewArrayHolder<WCHAR> wszAppDomainManagerAssemblyCopy(new (nothrow) WCHAR[cchAsm]);
    if (wszAppDomainManagerAssemblyCopy == NULL)
    {
        return E_OUTOFMEMORY;
    }
    wcsncpy_s(wszAppDomainManagerAssemblyCopy, cchAsm, wszAppDomainManagerAssembly, cchAsm - 1);

    // And of the AppDomainManagerType
    size_t cchType = wcslen(wszAppDomainManagerType) + 1;
    NewArrayHolder<WCHAR> wszAppDomainManagerTypeCopy(new (nothrow) WCHAR[cchType]);
    if (wszAppDomainManagerTypeCopy == NULL)
    {
        return E_OUTOFMEMORY;
    }
    wcsncpy_s(wszAppDomainManagerTypeCopy, cchType, wszAppDomainManagerType, cchType - 1);

    LPCWSTR wszOldAsmValue = FastInterlockCompareExchangePointer(&s_wszAppDomainManagerAsm,
                                                                 static_cast<LPCWSTR>(wszAppDomainManagerAssemblyCopy.GetValue()),
                                                                 NULL);
    if (wszOldAsmValue != NULL)
    {
        // We've tried to setup an AppDomainManager twice ... that's not allowed
        return HOST_E_INVALIDOPERATION;
    }

    s_wszAppDomainManagerType = wszAppDomainManagerTypeCopy;
    s_dwDomainManagerInitFlags = dwInitializeDomainFlags;

    wszAppDomainManagerAssemblyCopy.SuppressRelease();
    wszAppDomainManagerTypeCopy.SuppressRelease();
    return S_OK;
}
#endif // !FEATURE_CORECLR

LPCWSTR CorHost2::GetAppDomainManagerAsm()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_CORECLR
    return NULL;
#else // FEATURE_CORECLR
    _ASSERTE (g_fEEStarted);
    return s_wszAppDomainManagerAsm;
#endif // FEATURE_CORECLR
}

LPCWSTR CorHost2::GetAppDomainManagerType()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_CORECLR
    return NULL;
#else // FEATURE_CORECLR
    _ASSERTE (g_fEEStarted);
    return s_wszAppDomainManagerType;
#endif // FEATURE_CORECLR
}

// static
EInitializeNewDomainFlags CorHost2::GetAppDomainManagerInitializeNewDomainFlags()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_CORECLR
    return eInitializeNewDomainFlags_None;
#else // FEAUTRE_CORECLR
    _ASSERTE (g_fEEStarted);
    return s_dwDomainManagerInitFlags;
#endif // FEATURE_CORECLR
}

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
// We do not implement the Release since our host does not control the lifetime on this object
ULONG CCLRDebugManager::Release()
{
    LIMITED_METHOD_CONTRACT;
    return (1);
}

HRESULT CCLRDebugManager::QueryInterface(REFIID riid, void **ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
    {
        *ppUnk = (IUnknown *) this;
    }
    else if (riid == IID_ICLRDebugManager)
    {
        *ppUnk = (ICLRDebugManager *) this;
    }
    else 
    {
        hr = E_NOINTERFACE;
    }

    return hr;

}

/*
*
* Called once to when process start up to initialize the lock for connection name hash table
*
*/
void CCLRDebugManager::ProcessInit()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_lockConnectionNameTable.Init(CrstConnectionNameTable, (CrstFlags) (CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));
}

/*
* Called once to when process shut down to destroy the lock for connection name hash table
*
*/
void CCLRDebugManager::ProcessCleanup()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    m_lockConnectionNameTable.Destroy();
}
#endif // FEATURE_INCLUDE_ALL_INTERFACES

#endif // !DAC


#ifdef DACCESS_COMPILE

#ifdef FEATURE_INCLUDE_ALL_INTERFACES

//---------------------------------------------------------------------------------------
// Begin an iterating over connections for Debugger
//
// Arguments: 
//     pHashfind - out: initializes cookie to pass to to future calls to code:CCLRDebugManager.FindNext
//
// Returns:
//     NULL if iteration is done. Else a ConnectionNameHashEntry representing the connection.
//
ConnectionNameHashEntry * CCLRDebugManager::FindFirst(HASHFIND * pHashfind)
{
    SUPPORTS_DAC;
    if (m_pConnectionNameHash == NULL)
    {
        return NULL;
    }

    ConnectionNameHashEntry * pConnection = dac_cast<PTR_ConnectionNameHashEntry>(m_pConnectionNameHash->FindFirstEntry(pHashfind));
    return pConnection;
    }

//---------------------------------------------------------------------------------------
// Begin an iterating over connections for Debugger
//
// Arguments: 
//     pHashfind - in/out: iterator cookie to pass to future calls to code:CCLRDebugManager.FindNext
//
// Returns:
//     NULL if iteration is done. Else a ConnectionNameHashEntry representing the connection.
//
ConnectionNameHashEntry * CCLRDebugManager::FindNext(HASHFIND * pHashfind)
    {
    SUPPORTS_DAC;
    ConnectionNameHashEntry * pConnection = dac_cast<PTR_ConnectionNameHashEntry>(m_pConnectionNameHash->FindNextEntry(pHashfind));
    return pConnection;
}

#endif // FEATURE_INCLUDE_ALL_INTERFACES

#endif //DACCESS_COMPILE

#ifndef DACCESS_COMPILE

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
HRESULT CCLRDebugManager::IsDebuggerAttached(BOOL *pbAttached)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (pbAttached == NULL)
        return E_INVALIDARG;

    BEGIN_ENTRYPOINT_NOTHROW;

#ifdef DEBUGGING_SUPPORTED
    *pbAttached = (CORDebuggerAttached() != 0);
#else
    *pbAttached = FALSE;
#endif

    END_ENTRYPOINT_NOTHROW;


    return S_OK;
}

// By default, we permit symbols to be read for full-trust assemblies only
ESymbolReadingSetBy CCLRDebugManager::m_symbolReadingSetBy = eSymbolReadingSetByDefault;
ESymbolReadingPolicy CCLRDebugManager::m_symbolReadingPolicy = eSymbolReadingFullTrustOnly;

HRESULT CCLRDebugManager::SetSymbolReadingPolicy(ESymbolReadingPolicy policy)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;

    if( policy > eSymbolReadingFullTrustOnly )
    {
        return E_INVALIDARG;
    }

    SetSymbolReadingPolicy( policy, eSymbolReadingSetByHost );    

    return S_OK;
}

void CCLRDebugManager::SetSymbolReadingPolicy( ESymbolReadingPolicy policy, ESymbolReadingSetBy setBy )
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE( policy <= eSymbolReadingFullTrustOnly );  // don't have _COUNT because it's not in convention for mscoree.idl enums
    _ASSERTE( setBy < eSymbolReadingSetBy_COUNT );

    // if the setter meets or exceeds the precendence of the existing setting then override the setting
    if( setBy >= m_symbolReadingSetBy )
    {
        m_symbolReadingSetBy = setBy;
        m_symbolReadingPolicy = policy;
    }
}


/*
*   Call by host to set the name of a connection and begin a connection.
*
*/
HRESULT CCLRDebugManager::BeginConnection(
        CONNID  dwConnectionId,
        __in_z wchar_t *wzConnectionName) // We should review this in the future.  This API is
                                                         // public and callable by a host.  This SAL annotation
                                                         // is the best we can do now.
{
    CONTRACTL
    {
        GC_TRIGGERS;    // I am having problem in putting either GC_TRIGGERS or GC_NOTRIGGER. It is not happy either way when debugger
                        // call back event needs to enable preemptive GC.
        ENTRY_POINT;
        NOTHROW;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    ConnectionNameHashEntry *pEntry = NULL;

    // check input parameter
    if (dwConnectionId == INVALID_CONNECTION_ID || wzConnectionName == NULL || wzConnectionName[0] == W('\0'))
        IfFailGo(E_INVALIDARG);

    if (wcslen(wzConnectionName) >= MAX_CONNECTION_NAME)
        IfFailGo(E_INVALIDARG);

    {
        CrstHolder ch(&m_lockConnectionNameTable);

        if (m_pConnectionNameHash == NULL)
        {
            m_pConnectionNameHash = new (nothrow) ConnectionNameTable(50);
            IfNullGo(m_pConnectionNameHash);
            IfFailGo(m_pConnectionNameHash->NewInit(50, sizeof(ConnectionNameHashEntry), USHRT_MAX));
        }

        // error: Should not have an existing connection id already
        if (m_pConnectionNameHash->FindConnection(dwConnectionId))
            IfFailGo(E_INVALIDARG);

        // Our implementation of hashtable cannot throw out of memory exception
        pEntry = m_pConnectionNameHash->AddConnection(dwConnectionId, wzConnectionName);
        IfNullGo(pEntry);
    }

    // send notification to debugger
    if (CORDebuggerAttached())
    {
        g_pDebugInterface->CreateConnection(dwConnectionId, wzConnectionName);
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

/*
*   Call by host to end a connection
*/
HRESULT CCLRDebugManager::EndConnection(CONNID   dwConnectionId)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    UINT CLRTaskCount = 0;
    ICLRTask **ppCLRTaskArray = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (dwConnectionId == INVALID_CONNECTION_ID)
        IfFailGo(E_INVALIDARG);

    // No connection exist at all
    if (m_pConnectionNameHash == NULL)
        IfFailGo(E_FAIL);

    {
        CrstHolder ch(&m_lockConnectionNameTable);
        ConnectionNameHashEntry *pEntry = NULL;

        if ((pEntry = m_pConnectionNameHash->FindConnection(dwConnectionId)) == NULL)
            IfFailGo(E_INVALIDARG);

        // Note that the Release on CLRTask chould take a ThreadStoreLock. So we need to finish our
        // business with ConnectionNameHash before hand and release our name hash lock
        //
        CLRTaskCount = pEntry->m_CLRTaskCount;
        ppCLRTaskArray = pEntry->m_ppCLRTaskArray;
        pEntry->m_ppCLRTaskArray = NULL;
        pEntry->m_CLRTaskCount = 0;
        m_pConnectionNameHash->DeleteConnection(dwConnectionId);
    }

    if (CLRTaskCount != 0)
    {
        _ASSERTE(ppCLRTaskArray != NULL);
        for (UINT i = 0; i < CLRTaskCount; i++)
        {
            ((Thread *)ppCLRTaskArray[i])->SetConnectionId(INVALID_CONNECTION_ID);
            ppCLRTaskArray[i]->Release();
        }
        delete [] ppCLRTaskArray;
    }

    // send notification to debugger
    if (CORDebuggerAttached())
        g_pDebugInterface->DestroyConnection(dwConnectionId);

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

/*
*   Call by host to set a set of tasks as a connection.
*
*/
HRESULT CCLRDebugManager::SetConnectionTasks(
    DWORD id,
    DWORD dwCount,
    ICLRTask **ppCLRTask)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    ICLRTask **ppCLRTaskArrayNew = NULL;
    UINT CLRTaskCountPrevious = 0;
    ICLRTask **ppCLRTaskArrayPrevious = NULL;

    BEGIN_ENTRYPOINT_NOTHROW;

    DWORD       index;
    Thread      *pThread;
    ConnectionNameHashEntry *pEntry = NULL;

    if (id == INVALID_CONNECTION_ID || dwCount == 0 || ppCLRTask == NULL)
        IfFailGo(E_INVALIDARG);

    {
        CrstHolder ch(&m_lockConnectionNameTable);

        // check the BeginConnectin has been called.
        if (m_pConnectionNameHash == NULL)
            // No connection exist
            IfFailGo(E_INVALIDARG);

        // Host forget to call BeginConnection before calling SetConnectionTask!
        if ((pEntry = m_pConnectionNameHash->FindConnection(id)) == NULL)
            IfFailGo(E_INVALIDARG);

        for (index = 0; index < dwCount; index++)
        {
            // Check on input parameter
            pThread = (Thread *) ppCLRTask[index];
            if (pThread == NULL)
            {
                // _ASSERTE(!"Host passed in NULL ICLRTask pointer");
                IfFailGo(E_INVALIDARG);
            }

            // Check for Finalizer thread
            if (GCHeap::IsGCHeapInitialized() && (pThread == FinalizerThread::GetFinalizerThread()))
            {
                // _ASSERTE(!"Host should not try to schedule user code on our Finalizer Thread");
                IfFailGo(E_INVALIDARG);

            }
        }

        ppCLRTaskArrayNew = new (nothrow) ICLRTask*[dwCount];
        IfNullGo(ppCLRTaskArrayNew);

        CLRTaskCountPrevious = pEntry->m_CLRTaskCount;
        ppCLRTaskArrayPrevious = pEntry->m_ppCLRTaskArray;
        pEntry->m_ppCLRTaskArray = NULL;
        pEntry->m_CLRTaskCount = 0;

        if (CLRTaskCountPrevious != 0)
        {
            // Clear the old connection set
            _ASSERTE(ppCLRTaskArrayPrevious != NULL);
            for (UINT i = 0; i < CLRTaskCountPrevious; i++)
                ((Thread *)ppCLRTaskArrayPrevious[i])->SetConnectionId(INVALID_CONNECTION_ID);
        }

        // now remember the new set
        pEntry->m_ppCLRTaskArray = ppCLRTaskArrayNew;

        for (index = 0; index < dwCount; index++)
        {
            pThread = (Thread *) ppCLRTask[index];
            pThread->SetConnectionId( id );
            pEntry->m_ppCLRTaskArray[index] = ppCLRTask[index];
        }
        pEntry->m_CLRTaskCount = dwCount;

        // AddRef and Release on Thread object can call ThreadStoreLock. So we will release our
        // lock first of all.
    }

    // Does the addref on the new set
    for (index = 0; index < dwCount; index++)
        ppCLRTaskArrayNew[index]->AddRef();

    // Does the release on the old set
    if (CLRTaskCountPrevious != 0)
    {
        _ASSERTE(ppCLRTaskArrayPrevious != NULL);
        for (UINT i = 0; i < CLRTaskCountPrevious; i++)
            ppCLRTaskArrayPrevious[i]->Release();
        delete ppCLRTaskArrayPrevious;
    }

    // send notification to debugger
    if (CORDebuggerAttached())
    {
        g_pDebugInterface->ChangeConnection(id);
    }

ErrExit:
    END_ENTRYPOINT_NOTHROW;
    return hr;
}

HRESULT CCLRDebugManager::SetDacl(PACL pacl)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;
    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    hr = E_NOTIMPL;

    END_ENTRYPOINT_NOTHROW;
    return hr;
}   // SetDACL


HRESULT CCLRDebugManager::GetDacl(PACL *pacl)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_ENTRY_POINT;
    HRESULT hr;

    BEGIN_ENTRYPOINT_NOTHROW;

    hr = E_NOTIMPL;

    END_ENTRYPOINT_NOTHROW;
    return hr;
}   // SetDACL

#endif // FEATURE_INCLUDE_ALL_INTERFACES

#if defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)

HRESULT CCLRErrorReportingManager::QueryInterface(REFIID riid, void** ppUnk)
{
    if (!ppUnk)
        return E_POINTER;

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    
    if (ppUnk == NULL)
    {
        return E_POINTER;
    }

    *ppUnk = 0;

    // Deliberately do NOT hand out ICorConfiguration.  They must explicitly call
    // GetConfiguration to obtain that interface.
    if (riid == IID_IUnknown)
    {
        *ppUnk = (IUnknown *) this;
    }
    else if (riid == IID_ICLRErrorReportingManager)
    {
        *ppUnk = (ICLRErrorReportingManager *) this;
    }
#ifdef FEATURE_WINDOWSPHONE
    else if (riid == IID_ICLRErrorReportingManager2)
    {
        *ppUnk = (ICLRErrorReportingManager2 *) this;
    }
#endif // FEATURE_WINDOWSPHONE
    else
    {
        hr = E_NOINTERFACE;
    }

    return hr;

} // HRESULT CCLRErrorReportingManager::QueryInterface()

ULONG CCLRErrorReportingManager::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
} // HRESULT CCLRErrorReportingManager::AddRef()

ULONG CCLRErrorReportingManager::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
} // HRESULT CCLRErrorReportingManager::Release()

// Get Watson bucket parameters for "current" exception (on calling thread).
HRESULT CCLRErrorReportingManager::GetBucketParametersForCurrentException(
    BucketParameters *pParams)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;

    // To avoid confusion, clear the buckets.
    memset(pParams, 0, sizeof(BucketParameters));

#ifndef FEATURE_PAL   
    // Defer to Watson helper.
    hr = ::GetBucketParametersForCurrentException(pParams);
 #else
    // Watson doesn't exist on non-windows platforms
    hr = E_NOTIMPL;
#endif // !FEATURE_PAL

    END_ENTRYPOINT_NOTHROW;

    return hr;

} // HRESULT CCLRErrorReportingManager::GetBucketParametersForCurrentException()

//
// The BeginCustomDump function configures the custom dump support
//
// Parameters -
// dwFlavor     - The flavor of the dump
// dwNumItems   - The number of items in the CustomDumpItem array.
//                  Should always be zero today, since no custom items are defined
// items        - Array of CustomDumpItem structs specifying items to be added to the dump.
//                  Should always be NULL today, since no custom items are defined.
// dwReserved   - reserved for future use. Must be zero today
//
HRESULT CCLRErrorReportingManager::BeginCustomDump( ECustomDumpFlavor dwFlavor,
                                        DWORD dwNumItems,
                                        CustomDumpItem items[],
                                        DWORD dwReserved)
{
    STATIC_CONTRACT_ENTRY_POINT;
    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    if (dwNumItems != 0 ||  items != NULL || dwReserved != 0)
    {
        IfFailGo(E_INVALIDARG);
    }
    if (g_ECustomDumpFlavor != DUMP_FLAVOR_Default)
    {
        // BeginCustomDump is called without matching EndCustomDump
        IfFailGo(E_INVALIDARG);
    }
    g_ECustomDumpFlavor = dwFlavor;

ErrExit:
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

//
// EndCustomDump clears the custom dump configuration
//
HRESULT CCLRErrorReportingManager::EndCustomDump()
{
    STATIC_CONTRACT_ENTRY_POINT;
    // NOT IMPLEMENTED YET
    BEGIN_ENTRYPOINT_NOTHROW;
    g_ECustomDumpFlavor = DUMP_FLAVOR_Default;
    END_ENTRYPOINT_NOTHROW;

    return S_OK;
}

#ifdef FEATURE_WINDOWSPHONE
HRESULT CopyStringWorker(_Out_ WCHAR** pTarget, WCHAR const* pSource)
{
    LIMITED_METHOD_CONTRACT;

    if (pTarget == NULL || pSource == NULL)
        return E_INVALIDARG;

    if (*pTarget)
        delete[] (*pTarget);

    // allocate space for the data plus one wchar for NULL
    size_t sourceLen = wcslen(pSource);
    *pTarget = new (nothrow) WCHAR[sourceLen + 1];
    
    if (!(*pTarget))
        return E_OUTOFMEMORY;

    errno_t result = wcsncpy_s(*pTarget, sourceLen + 1, pSource, sourceLen);
    _ASSERTE(result == 0);
    
    if (result != 0)
    {
        delete[] (*pTarget);
        *pTarget = NULL;

        return E_FAIL;
    }

    return S_OK;
}

CCLRErrorReportingManager::BucketParamsCache::BucketParamsCache(DWORD maxNumParams) : m_pParams(NULL), m_cMaxParams(maxNumParams)
{
    LIMITED_METHOD_CONTRACT;
}

CCLRErrorReportingManager::BucketParamsCache::~BucketParamsCache()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pParams)
    {
        for (DWORD i = 0; i < m_cMaxParams; ++i)
            if (m_pParams[i]) delete[] m_pParams[i];
    }
}

WCHAR const* CCLRErrorReportingManager::BucketParamsCache::GetAt(BucketParameterIndex index)
{
    LIMITED_METHOD_CONTRACT;

    if (index >= InvalidBucketParamIndex)
    {
        _ASSERTE(!"bad bucket parameter index");
        return NULL;
    }

    if (!m_pParams)
        return NULL;

    return m_pParams[index];
}

HRESULT CCLRErrorReportingManager::BucketParamsCache::SetAt(BucketParameterIndex index, WCHAR const* val)
{
    LIMITED_METHOD_CONTRACT;

    if (index >= InvalidBucketParamIndex)
    {
        _ASSERTE(!"bad bucket parameter index");
        return E_INVALIDARG;
    }

    if (!val)
        return E_INVALIDARG;

    if (!m_pParams)
    {
        m_pParams = new (nothrow) WCHAR*[m_cMaxParams];
        if (!m_pParams)
            return E_OUTOFMEMORY;

        for (DWORD i = 0; i < m_cMaxParams; ++i)
            m_pParams[i] = NULL;
    }

    return CopyStringWorker(&m_pParams[index], val);
}

HRESULT CCLRErrorReportingManager::CopyToDataCache(_Out_ WCHAR** pTarget, WCHAR const* pSource)
{
    LIMITED_METHOD_CONTRACT;

    return CopyStringWorker(pTarget, pSource);
}

HRESULT CCLRErrorReportingManager::SetApplicationData(ApplicationDataKey key, WCHAR const* pValue)
{
    STATIC_CONTRACT_ENTRY_POINT;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(g_fEEStarted)
        return HOST_E_INVALIDOPERATION;

    if (pValue == NULL || wcslen(pValue) > MAX_LONGPATH)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    switch (key)
    {
    case ApplicationID:
        hr = CopyToDataCache(&m_pApplicationId, pValue);
        break;

    case InstanceID:
        hr = CopyToDataCache(&m_pInstanceId, pValue);
        break;

    default:
        hr = E_INVALIDARG;
    }
    
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

HRESULT CCLRErrorReportingManager::SetBucketParametersForUnhandledException(BucketParameters const* pBucketParams, DWORD* pCountParams)
{
    STATIC_CONTRACT_ENTRY_POINT;

    BEGIN_ENTRYPOINT_NOTHROW;

    if(g_fEEStarted)
        return HOST_E_INVALIDOPERATION;

    if (pBucketParams == NULL || pCountParams == NULL || pBucketParams->fInited != TRUE)
        return E_INVALIDARG;

    *pCountParams = 0;

    if (!m_pBucketParamsCache)
    {
        m_pBucketParamsCache = new (nothrow) BucketParamsCache(InvalidBucketParamIndex);
        if (!m_pBucketParamsCache)
            return E_OUTOFMEMORY;
    }

    HRESULT hr = S_OK;
    bool hasOverride = false;

    for (DWORD i = 0; i < InvalidBucketParamIndex; ++i)
    {
        if (pBucketParams->pszParams[i][0] != W('\0'))
        {
            hasOverride = true;
            hr = m_pBucketParamsCache->SetAt(static_cast<BucketParameterIndex>(i), pBucketParams->pszParams[i]);
            if (SUCCEEDED(hr))
                *pCountParams += 1;
            else
                break;
        }
    }
    
    if (!hasOverride)
        return E_INVALIDARG;
    
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

WCHAR const* CCLRErrorReportingManager::GetApplicationData(ApplicationDataKey key)
{
    LIMITED_METHOD_CONTRACT;

    WCHAR* pValue = NULL;

    switch (key)
    {
    case ApplicationID:
        pValue = m_pApplicationId;
        break;

    case InstanceID:
        pValue = m_pInstanceId;
        break;

    default:
        _ASSERTE(!"invalid key specified");
    }

    return pValue;
}

WCHAR const* CCLRErrorReportingManager::GetBucketParamOverride(BucketParameterIndex bucketParamId)
{
    LIMITED_METHOD_CONTRACT;

    if (!m_pBucketParamsCache)
        return NULL;

    return m_pBucketParamsCache->GetAt(bucketParamId);
}

#endif // FEATURE_WINDOWSPHONE

CCLRErrorReportingManager::CCLRErrorReportingManager()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_WINDOWSPHONE
    m_pApplicationId = NULL;
    m_pInstanceId = NULL;
    m_pBucketParamsCache = NULL;
#endif
}

CCLRErrorReportingManager::~CCLRErrorReportingManager()
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_WINDOWSPHONE
    if (m_pApplicationId)
        delete[] m_pApplicationId;

    if (m_pInstanceId)
        delete[] m_pInstanceId;

    if (m_pBucketParamsCache)
        delete m_pBucketParamsCache;
#endif
}

#endif // defined(FEATURE_INCLUDE_ALL_INTERFACES) || defined(FEATURE_WINDOWSPHONE)

#ifdef FEATURE_IPCMAN

CrstStatic          CCLRSecurityAttributeManager::m_hostSAMutex;
PACL                CCLRSecurityAttributeManager::m_pACL;

SECURITY_ATTRIBUTES CCLRSecurityAttributeManager::m_hostSA;
SECURITY_DESCRIPTOR CCLRSecurityAttributeManager::m_hostSD;

/*
* constructor
*
*/
void CCLRSecurityAttributeManager::ProcessInit()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_hostSAMutex.Init(CrstReDacl, CRST_UNSAFE_ANYMODE);
    m_pACL = NULL;
}

/*
* destructor
*
*/
void CCLRSecurityAttributeManager::ProcessCleanUp()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    m_hostSAMutex.Destroy();
    if (m_pACL)
        CoTaskMemFree(m_pACL);
}

// Set private block and events to the new ACL.
HRESULT CCLRSecurityAttributeManager::SetDACL(PACL pacl)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    DWORD       dwError;
    PACL        pNewACL = NULL;
    HANDLE      hProc = NULL;
    DWORD       pid = 0;

    // @todo: How can we make sure that debugger attach will not attempt to happen during this time???
    //
    CrstHolder ch(&m_hostSAMutex);

    // make sure our host pass our a valid ACL
    if (!IsValidAcl(pacl))
    {
        dwError = GetLastError();
        hr = HRESULT_FROM_WIN32(dwError);
        goto ErrExit;
    }

    // Cannnot set DACL while debugger is attached. Because the events are already all hooked up
    // between LS and RS.
    if (CORDebuggerAttached())
        return CORDBG_E_DEBUGGER_ALREADY_ATTACHED;

    // make a copy of the new ACL
    pNewACL = (PACL) CoTaskMemAlloc(pacl->AclSize);
    if (FAILED( CopyACL(pacl, pNewACL)))
        goto ErrExit;

    _ASSERTE (SECURITY_DESCRIPTOR_MIN_LENGTH == sizeof(SECURITY_DESCRIPTOR));

    if (!InitializeSecurityDescriptor(&m_hostSD, SECURITY_DESCRIPTOR_REVISION))
    {
        hr = HRESULT_FROM_GetLastError();
        goto ErrExit;
    }

    if (!SetSecurityDescriptorDacl(&m_hostSD, TRUE, pNewACL, FALSE))
    {
        hr = HRESULT_FROM_GetLastError();
        goto ErrExit;
    }

    // Now cache the pNewACL to m_pACL and delete m_pACL.
    if (m_pACL)
        CoTaskMemFree(m_pACL);

    m_pACL = pNewACL;
    pNewACL = NULL;

    m_hostSA.nLength = sizeof(SECURITY_ATTRIBUTES);
    m_hostSA.lpSecurityDescriptor = &m_hostSD;
    m_hostSA.bInheritHandle = FALSE;

    // first of all, try to reDacl on the process token
    pid = GetCurrentProcessId();
    hProc = OpenProcess(WRITE_DAC, FALSE, pid);
    if (hProc == NULL)
    {
        hr = HRESULT_FROM_GetLastError();
        goto ErrExit;
    }
    if (SetKernelObjectSecurity(hProc, DACL_SECURITY_INFORMATION, &m_hostSD) == 0)
    {
        // failed!
        hr = HRESULT_FROM_GetLastError();
        goto ErrExit;
    }


    // now reset all of the kernel object token's DACL.
    // This will reDACL the global shared section
    if (FAILED(g_pIPCManagerInterface->ReDaclLegacyPrivateBlock(&m_hostSD)))
        goto ErrExit;

    // This will reDacl on debugger events.
    if (g_pDebugInterface)
    {
        g_pDebugInterface->ReDaclEvents(&m_hostSD);
    }

ErrExit:
    if (pNewACL)
        CoTaskMemFree(pNewACL);
    if (hProc != NULL)
        CloseHandle(hProc);

    return hr;
}

// cLen - specify the size of input buffer ppacl. If cLen is zero or ppacl is null,
// pcLenTotal will return the total size of required pacl buffer.
// pacl - caller allocated space. We will fill acl in this buffer.
// pcLenTotal - the total size of ACL.
//
HRESULT CCLRSecurityAttributeManager::GetDACL(PACL *ppacl)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;
    PACL        pNewACL = NULL;
    PACL        pDefaultACL = NULL;
    SECURITY_ATTRIBUTES *pSA = NULL;

    // output parameter cannot be NULL
    if (ppacl == NULL)
        return E_INVALIDARG;

    *ppacl = NULL;

    CrstHolder ch(&m_hostSAMutex);

    // we want to return the ACL of our default policy
    if (m_pACL == NULL)
    {
        hr = g_pIPCManagerInterface->CreateWinNTDescriptor(GetCurrentProcessId(), &pSA, eDescriptor_Private);
        if (FAILED(hr))
        {
            goto ErrExit;
        }
        EX_TRY
        {
            BOOL bDaclPresent;
            BOOL bDaclDefault;

            LeaveRuntimeHolder holder((size_t)(::GetSecurityDescriptorDacl));
            ::GetSecurityDescriptorDacl(pSA->lpSecurityDescriptor, &bDaclPresent, &pDefaultACL, &bDaclDefault);
        }
        EX_CATCH
        {
            hr = GET_EXCEPTION()->GetHR();
        }
        EX_END_CATCH(SwallowAllExceptions);
        if (FAILED(hr) || pDefaultACL == NULL || pDefaultACL->AclSize == 0)
        {
            goto ErrExit;
        }
    }
    else
    {
        pDefaultACL = m_pACL;
    }

    pNewACL = (PACL) CoTaskMemAlloc(pDefaultACL->AclSize);
    if (pNewACL == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto ErrExit;
    }

    // make a copy of ACL
    hr = CCLRSecurityAttributeManager::CopyACL(pDefaultACL, pNewACL);
    if (SUCCEEDED(hr))
        *ppacl = pNewACL;

ErrExit:
    if (FAILED(hr))
    {
        if (pNewACL)
        {
            CoTaskMemFree(pNewACL);
        }
    }
    if (pSA != NULL)
    {
        g_pIPCManagerInterface->DestroySecurityAttributes(pSA);
    }
    return hr;
}


// This API will duplicate a copy of pAclOrigingal and pass it out on ppAclNew
HRESULT CCLRSecurityAttributeManager::CopyACL(PACL pAclOriginal, PACL pNewACL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT     hr = NO_ERROR;
    DWORD       dwError = GetLastError();
    int         i;
    ACE_HEADER  *pDACLAce;

    _ASSERTE(pNewACL && pAclOriginal);

    // initialize the target ACL buffer
    if (!InitializeAcl(pNewACL, pAclOriginal->AclSize, ACL_REVISION))
    {
        dwError = GetLastError();
        hr = HRESULT_FROM_WIN32(dwError);
        goto ErrExit;
    }

    // loop through each existing ace and copy it over
    for (i = 0; i < pAclOriginal->AceCount; i++)
    {
        if (!GetAce(pAclOriginal, i, (LPVOID *) &pDACLAce))
        {
            dwError = GetLastError();
            hr = HRESULT_FROM_WIN32(dwError);
            goto ErrExit;
        }

        if (!AddAce(pNewACL, ACL_REVISION, i, pDACLAce, pDACLAce->AceSize))
        {
            dwError = GetLastError();
            hr = HRESULT_FROM_WIN32(dwError);
            goto ErrExit;
        }
    }

    // make sure everything went well with the new ACL
    if (!IsValidAcl(pNewACL))
    {
        dwError = GetLastError();
        hr = HRESULT_FROM_WIN32(dwError);
        goto ErrExit;
    }

ErrExit:
    return hr;
}


HRESULT CCLRSecurityAttributeManager::GetHostSecurityAttributes(SECURITY_ATTRIBUTES **ppSA)
{
    WRAPPER_NO_CONTRACT;

    if(!ppSA)
        return E_POINTER;

    HRESULT hr = S_OK;

    *ppSA = NULL;

    // host has specified ACL
    if (m_pACL != NULL)
        *ppSA = &(m_hostSA);

    else
        hr = g_pIPCManagerInterface->CreateWinNTDescriptor(GetCurrentProcessId(), ppSA, eDescriptor_Private);

    return hr;
}

void CCLRSecurityAttributeManager::DestroyHostSecurityAttributes(SECURITY_ATTRIBUTES *pSA)
{
    WRAPPER_NO_CONTRACT;

    // no pSA to cleanup
    if (pSA == NULL)
        return;

    // it is our current host SA.
    if (&(m_hostSA) == pSA)
        return;

    g_pIPCManagerInterface->DestroySecurityAttributes(pSA);
}
#endif // FEATURE_IPCMAN

void GetProcessMemoryLoad(LPMEMORYSTATUSEX pMSEX)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    pMSEX->dwLength = sizeof(MEMORYSTATUSEX);
    BOOL fRet = GlobalMemoryStatusEx(pMSEX);
    _ASSERTE (fRet);

#ifdef FEATURE_INCLUDE_ALL_INTERFACES
    // CoreCLR cannot be memory hosted
    if (CLRMemoryHosted())
    {
        BEGIN_SO_TOLERANT_CODE_CALLING_HOST(GetThread());
        DWORD memoryLoad;
        SIZE_T availableBytes;
        HRESULT hr = CorHost2::GetHostMemoryManager()->GetMemoryLoad(&memoryLoad, &availableBytes);
        if (hr == S_OK) {
            pMSEX->dwMemoryLoad = memoryLoad;
            pMSEX->ullAvailPhys = availableBytes;
        }
        END_SO_TOLERANT_CODE_CALLING_HOST;
    }
#endif // FEATURE_INCLUDE_ALL_INTERFACES

    // If the machine has more RAM than virtual address limit, let us cap it.
    // Our GC can never use more than virtual address limit.
    if (pMSEX->ullAvailPhys > pMSEX->ullTotalVirtual)
    {
        pMSEX->ullAvailPhys = pMSEX->ullAvailVirtual;
    }
}

// This is the instance that exposes interfaces out to all the other DLLs of the CLR
// so they can use our services for TLS, synchronization, memory allocation, etc.
static BYTE g_CEEInstance[sizeof(CExecutionEngine)];
static Volatile<IExecutionEngine*> g_pCEE = NULL;

PTLS_CALLBACK_FUNCTION CExecutionEngine::Callbacks[MAX_PREDEFINED_TLS_SLOT];

extern "C" IExecutionEngine * __stdcall IEE()
{
    LIMITED_METHOD_CONTRACT;

    // Unfortunately,we can't probe here. The probing system requires the
    // use of TLS, and in order to initialize TLS we need to call IEE.

    //BEGIN_ENTRYPOINT_VOIDRET;


    // The following code does NOT contain a race condition.  The following code is BY DESIGN.
    // The issue is that we can have two separate threads inside this if statement, both of which are
    // initializing the g_CEEInstance variable (and subsequently updating g_pCEE).  This works fine,
    // and will not cause an inconsistent state due to the fact that CExecutionEngine has no
    // local variables.  If multiple threads make it inside this if statement, it will copy the same
    // bytes over g_CEEInstance and there will not be a time when there is an inconsistent state.
    if ( !g_pCEE )
    {
        // Create a local copy on the stack and then copy it over to the static instance.
        // This avoids race conditions caused by multiple initializations of vtable in the constructor
       CExecutionEngine local;
       memcpy(&g_CEEInstance, (void*)&local, sizeof(CExecutionEngine));

       g_pCEE = (IExecutionEngine*)(CExecutionEngine*)&g_CEEInstance;
    }
    //END_ENTRYPOINT_VOIDRET;

    return g_pCEE;
}


HRESULT STDMETHODCALLTYPE CExecutionEngine::QueryInterface(REFIID id, void **pInterface)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    if (!pInterface)
        return E_POINTER;

    *pInterface = NULL;

    //CANNOTTHROWCOMPLUSEXCEPTION();
    if (id == IID_IExecutionEngine)
        *pInterface = (IExecutionEngine *)this;
    else if (id == IID_IEEMemoryManager)
        *pInterface = (IEEMemoryManager *)this;
    else if (id == IID_IUnknown)
        *pInterface = (IUnknown *)(IExecutionEngine *)this;
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
} // HRESULT STDMETHODCALLTYPE CExecutionEngine::QueryInterface()


ULONG STDMETHODCALLTYPE CExecutionEngine::AddRef()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

ULONG STDMETHODCALLTYPE CExecutionEngine::Release()
{
    LIMITED_METHOD_CONTRACT;
    return 1;
}

struct ClrTlsInfo
{
    void* data[MAX_PREDEFINED_TLS_SLOT];
    // When hosted, we may not be able to delete memory in DLL_THREAD_DETACH.
    // We will chain this into a side list, and free these on Finalizer thread.
    ClrTlsInfo *next;
};

#define DataToClrTlsInfo(a) (a)?(ClrTlsInfo*)((BYTE*)a - offsetof(ClrTlsInfo, data)):NULL

#if !defined(FEATURE_CORECLR)
#define HAS_FLS_SUPPORT 1
#endif

#ifdef HAS_FLS_SUPPORT

static BOOL fHasFlsSupport = FALSE;

typedef DWORD (*Func_FlsAlloc)(PFLS_CALLBACK_FUNCTION lpCallback);
typedef BOOL (*Func_FlsFree)(DWORD dwFlsIndex);
typedef BOOL (*Func_FlsSetValue)(DWORD dwFlsIndex,PVOID lpFlsData);
typedef PVOID (*Func_FlsGetValue)(DWORD dwFlsIndex);

static DWORD FlsIndex = FLS_OUT_OF_INDEXES;
static Func_FlsAlloc pFlsAlloc;
static Func_FlsSetValue pFlsSetValue;
static Func_FlsFree pFlsFree;
static Func_FlsGetValue pFlsGetValue;
static Volatile<BOOL> fFlsSetupDone = FALSE;

VOID WINAPI FlsCallback(
  PVOID lpFlsData
)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE (pFlsGetValue);
    if (pFlsGetValue(FlsIndex) != lpFlsData)
    {
        // The current running fiber is being destroyed.  We can not destroy the memory yet,
        // because our DllMain function may still need the memory.
        CExecutionEngine::ThreadDetaching((void **)lpFlsData);        
    }
    else
    {
        // The thread is being wound down.
        // In hosting scenarios the host will have already called ICLRTask::ExitTask, which 
        // ends up calling CExecutionEngine::SwitchOut, which will have reset the TLS at TlsIndex.
        // 
        // Unfortunately different OSes have different ordering of destroying FLS data and sending
        // the DLL_THREAD_DETACH notification (pre-Vista FlsCallback is called after DllMain, while 
        // in Vista and up, FlsCallback is called before DllMain).  Additionally, starting with 
        // Vista SP1 and Win2k8, the OS will set the FLS slot to 0 after the call to FlsCallback, 
        // effectively removing our last reference to this data. Since in EEDllMain we need to be 
        // able to access the FLS data, we save lpFlsData in the TLS slot at TlsIndex, if needed.
        if (CExecutionEngine::GetTlsData() == NULL)
        {
            CExecutionEngine::SetTlsData((void **)lpFlsData);
        }
    }
}

#endif // HAS_FLS_SUPPORT


#ifdef FEATURE_IMPLICIT_TLS
void** CExecutionEngine::GetTlsData()
{
    LIMITED_METHOD_CONTRACT;

   return gCurrentThreadInfo.m_EETlsData;
}

BOOL CExecutionEngine::SetTlsData (void** ppTlsInfo)
{
    LIMITED_METHOD_CONTRACT;

    gCurrentThreadInfo.m_EETlsData = ppTlsInfo;
    return TRUE;
}
#else 
void** CExecutionEngine::GetTlsData()
{
    LIMITED_METHOD_CONTRACT;

    if (TlsIndex == TLS_OUT_OF_INDEXES)
        return NULL;

    void **ppTlsData = (void **)UnsafeTlsGetValue(TlsIndex);
    return ppTlsData;
}
BOOL CExecutionEngine::SetTlsData (void** ppTlsInfo)
{
    LIMITED_METHOD_CONTRACT;

    if (TlsIndex == TLS_OUT_OF_INDEXES)
        return FALSE;

    return UnsafeTlsSetValue(TlsIndex, ppTlsInfo);
}

#endif // FEATURE_IMPLICIT_TLS

static VolatilePtr<ClrTlsInfo> g_pDetachedTlsInfo;

BOOL CExecutionEngine::HasDetachedTlsInfo()
{
    LIMITED_METHOD_CONTRACT;

    return g_pDetachedTlsInfo.Load() != NULL;
}

void CExecutionEngine::CleanupDetachedTlsInfo()
{
    WRAPPER_NO_CONTRACT;

    if (g_pDetachedTlsInfo.Load() == NULL)
    {
        return;
    }
    ClrTlsInfo *head = FastInterlockExchangePointer(g_pDetachedTlsInfo.GetPointer(), NULL);

    while (head)
    {
        ClrTlsInfo *node = head;
        head = head->next;
        DeleteTLS(node->data);
    }
}

void CExecutionEngine::DetachTlsInfo(void **pTlsData)
{
    LIMITED_METHOD_CONTRACT;
   
    if (pTlsData == NULL)
    {
        return;
    }    

    if (CExecutionEngine::GetTlsData() == pTlsData)
    {
        CExecutionEngine::SetTlsData(0);
    }

#ifdef HAS_FLS_SUPPORT
    if (fHasFlsSupport && pFlsGetValue(FlsIndex) == pTlsData)
    {
        pFlsSetValue(FlsIndex, NULL);
    }
#endif

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);
    // PREFIX_ASSUME needs TLS.  If we use it here, we may do memory allocation.
#if defined(_PREFAST_) || defined(_PREFIX_) 
    if (pTlsInfo == NULL) __UNREACHABLE();
#else
    _ASSERTE(pTlsInfo != NULL);
#endif // _PREFAST_ || _PREFIX_

    if (pTlsInfo->data[TlsIdx_StressLog])
    {
#ifdef STRESS_LOG
      CantAllocHolder caHolder; 
      StressLog::ThreadDetach ((ThreadStressLog *)pTlsInfo->data[TlsIdx_StressLog]);
      pTlsInfo->data[TlsIdx_StressLog] = NULL;
#else
        _ASSERTE (!"Shouldn't have stress log!");
#endif
    }

    while (TRUE)
    {
        ClrTlsInfo *head = g_pDetachedTlsInfo.Load();
        pTlsInfo->next =  head;
        if (FastInterlockCompareExchangePointer(g_pDetachedTlsInfo.GetPointer(), pTlsInfo, head) == head)
        {
            return;
        }
    }
}

//---------------------------------------------------------------------------------------
//
// Returns the current logical thread's data block (ClrTlsInfo::data).
//
// Arguments:
//    slot - Index of the slot that is about to be requested
//    force - If the data block does not exist yet, create it as a side-effect
//
// Return Value:
//    NULL, if the data block did not exist yet for the current thread and force was FALSE.
//    A pointer to the data block, otherwise.
//
// Notes:
//    If the underlying OS does not support fiber mode, the data block is stored in TLS.
//    If the underlying OS does support fiber mode, it is primarily stored in FLS,
//    and cached in TLS so that we can use our generated optimized TLS accessors.
//
// TLS support for the other DLLs of the CLR operates quite differently in hosted
// and unhosted scenarios.

void **CExecutionEngine::CheckThreadState(DWORD slot, BOOL force)
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    // !!! This function is called during Thread::SwitchIn and SwitchOut
    // !!! It is extremely important that while executing this function, we will not
    // !!! cause fiber switch.  This means we can not allocate memory, lock, etc...

    //<TODO> @TODO: Decide on an exception strategy for all the DLLs of the CLR, and then
    // enable all the exceptions out of this method.</TODO>

    // Treat as a runtime assertion, since the invariant spans many DLLs.
    _ASSERTE(slot < MAX_PREDEFINED_TLS_SLOT);
//    if (slot >= MAX_PREDEFINED_TLS_SLOT)
//        COMPlusThrow(kArgumentOutOfRangeException);

#ifdef HAS_FLS_SUPPORT
    if (!fFlsSetupDone)
    {
        // Contract depends on Fls support.  Don't use contract here.
        HMODULE hmod = GetModuleHandleA(WINDOWS_KERNEL32_DLLNAME_A);
        if (hmod)
        {
            pFlsSetValue = (Func_FlsSetValue) GetProcAddress(hmod, "FlsSetValue");
            pFlsGetValue = (Func_FlsGetValue) GetProcAddress(hmod, "FlsGetValue");
            pFlsAlloc = (Func_FlsAlloc) GetProcAddress(hmod, "FlsAlloc");
            pFlsFree = (Func_FlsFree) GetProcAddress(hmod, "FlsFree");

            if (pFlsSetValue && pFlsGetValue && pFlsAlloc && pFlsFree )
            {
                fHasFlsSupport = TRUE;
            }
            else
            {
                // Since we didn't find them all, we shouldn't have found any
                _ASSERTE( pFlsSetValue == NULL && pFlsGetValue == NULL && pFlsAlloc == NULL && pFlsFree == NULL);
            }
            fFlsSetupDone = TRUE;
        }
    }

    if (fHasFlsSupport && FlsIndex == FLS_OUT_OF_INDEXES)
    {
        // PREFIX_ASSUME needs TLS.  If we use it here, we will loop forever
#if defined(_PREFAST_) || defined(_PREFIX_) 
        if (pFlsAlloc == NULL) __UNREACHABLE();
#else
        _ASSERTE(pFlsAlloc != NULL);
#endif // _PREFAST_ || _PREFIX_

        DWORD tryFlsIndex = pFlsAlloc(FlsCallback);
        if (tryFlsIndex != FLS_OUT_OF_INDEXES)
        {
            if (FastInterlockCompareExchange((LONG*)&FlsIndex, tryFlsIndex, FLS_OUT_OF_INDEXES) != FLS_OUT_OF_INDEXES)
            {
                pFlsFree(tryFlsIndex);
            }
        }
        if (FlsIndex == FLS_OUT_OF_INDEXES)
        {
            COMPlusThrowOM();
        }
    }
#endif // HAS_FLS_SUPPORT

#ifndef FEATURE_IMPLICIT_TLS
    // Ensure we have a TLS Index
    if (TlsIndex == TLS_OUT_OF_INDEXES)
    {
        DWORD tryTlsIndex = UnsafeTlsAlloc();
        if (tryTlsIndex != TLS_OUT_OF_INDEXES)
        {
            if (FastInterlockCompareExchange((LONG*)&TlsIndex, tryTlsIndex, TLS_OUT_OF_INDEXES) != (LONG)TLS_OUT_OF_INDEXES)
            {
                UnsafeTlsFree(tryTlsIndex);
            }
        }
        if (TlsIndex == TLS_OUT_OF_INDEXES)
        {
            COMPlusThrowOM();
        }
    }
#endif // FEATURE_IMPLICIT_TLS

    void** pTlsData = CExecutionEngine::GetTlsData();
    BOOL fInTls = (pTlsData != NULL);

#ifdef HAS_FLS_SUPPORT
    if (fHasFlsSupport)
    {
        if (pTlsData == NULL)
        {
            pTlsData = (void **)pFlsGetValue(FlsIndex);
        }
    }
#endif

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);
    if (pTlsInfo == 0 && force)
    {
#undef HeapAlloc
#undef GetProcessHeap
        // !!! Contract uses our TLS support.  Contract may be used before our host support is set up.
        // !!! To better support contract, we call into OS for memory allocation.
        pTlsInfo = (ClrTlsInfo*) ::HeapAlloc(GetProcessHeap(),0,sizeof(ClrTlsInfo));
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)
        if (pTlsInfo == NULL)
        {
            goto LError;
        }
        memset (pTlsInfo, 0, sizeof(ClrTlsInfo));
#ifdef HAS_FLS_SUPPORT
        if (fHasFlsSupport && !pFlsSetValue(FlsIndex, pTlsInfo))
        {
            goto LError;
        }
#endif
        // We save the last intolerant marker on stack in this slot.  
        // -1 is the larget unsigned number, and therefore our marker is always smaller than it.
        pTlsInfo->data[TlsIdx_SOIntolerantTransitionHandler] = (void*)(-1);
    }

    if (!fInTls && pTlsInfo)
    {
#ifdef HAS_FLS_SUPPORT
        // If we have a thread object or are on a non-fiber thread, we are safe for fiber switching.
        if (!fHasFlsSupport ||
            GetThread() ||
            ((g_fEEStarted || g_fEEInit) && !CLRTaskHosted()) ||
            (((size_t)pTlsInfo->data[TlsIdx_ThreadType]) & (ThreadType_GC | ThreadType_Gate | ThreadType_Timer | ThreadType_DbgHelper)))
        {
#ifdef _DEBUG
            Thread *pThread = GetThread();
            if (pThread)
            {
                pThread->AddFiberInfo(Thread::ThreadTrackInfo_Lifetime);
            }
#endif
            if (!CExecutionEngine::SetTlsData(pTlsInfo->data) && !fHasFlsSupport)
            {
                goto LError;
            }
        }
#else
        if (!CExecutionEngine::SetTlsData(pTlsInfo->data))
        {
            goto LError;
        }
#endif
    }

    return pTlsInfo?pTlsInfo->data:NULL;

LError:
    if (pTlsInfo)
    {
#undef HeapFree
#undef GetProcessHeap
        ::HeapFree(GetProcessHeap(), 0, pTlsInfo);
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
    }
    // If this is for the stack probe, and we failed to allocate memory for it, we won't
    // put in a guard page.
    if (slot == TlsIdx_ClrDebugState || slot == TlsIdx_StackProbe)
        return NULL;

    ThrowOutOfMemory();
}


void **CExecutionEngine::CheckThreadStateNoCreate(DWORD slot
#ifdef _DEBUG
                                                  , BOOL fForDestruction
#endif // _DEBUG
                                                  )
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_SO_TOLERANT;

    // !!! This function is called during Thread::SwitchIn and SwitchOut
    // !!! It is extremely important that while executing this function, we will not
    // !!! cause fiber switch.  This means we can not allocate memory, lock, etc...


    // Treat as a runtime assertion, since the invariant spans many DLLs.
    _ASSERTE(slot < MAX_PREDEFINED_TLS_SLOT);

    void **pTlsData = CExecutionEngine::GetTlsData();

#ifdef HAS_FLS_SUPPORT
    if (fHasFlsSupport)
    {
        if (pTlsData == NULL)
        {
            pTlsData = (void **)pFlsGetValue(FlsIndex);
        }
    }
#endif

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);

    return pTlsInfo?pTlsInfo->data:NULL;
}

// Note: Sampling profilers also use this function to initialize TLS for a unmanaged 
// sampling thread so that initialization can be done in advance to avoid deadlocks. 
// See ProfToEEInterfaceImpl::InitializeCurrentThread for more details.
void CExecutionEngine::SetupTLSForThread(Thread *pThread)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_ANY;

#ifdef _DEBUG
    if (pThread)
        pThread->AddFiberInfo(Thread::ThreadTrackInfo_Lifetime);
#endif
#ifdef STRESS_LOG
    if (StressLog::StressLogOn(~0u, 0))
    {
        StressLog::CreateThreadStressLog();
    }
#endif
    void **pTlsData;
    pTlsData = CheckThreadState(0);

    PREFIX_ASSUME(pTlsData != NULL);

#ifdef ENABLE_CONTRACTS
    // Profilers need the side effect of GetClrDebugState() to perform initialization
    // in advance to avoid deadlocks. Refer to ProfToEEInterfaceImpl::InitializeCurrentThread
    ClrDebugState *pDebugState = ::GetClrDebugState();

    if (pThread)
        pThread->m_pClrDebugState = pDebugState; 
#endif
}

void CExecutionEngine::SwitchIn()
{
    // No real contracts here.  This function is called by Thread::SwitchIn.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_ENTRY_POINT;

    // @TODO - doesn't look like we can probe here....

#ifdef HAS_FLS_SUPPORT
    if (fHasFlsSupport)
    {
        void **pTlsData = (void **)pFlsGetValue(FlsIndex);

        BOOL fResult = CExecutionEngine::SetTlsData(pTlsData);
        if (fResult)
        {
#ifdef STRESS_LOG
            // We are in task transition period.  We can not call into host to create stress log.
            if (ClrTlsGetValue(TlsIdx_StressLog) != NULL)
            {
                STRESS_LOG1(LF_SYNC, LL_INFO100, ThreadStressLog::TaskSwitchMsg(), ::GetCurrentThreadId());
            }
#endif
        }
        // It is OK for UnsafeTlsSetValue to fail here, since we can always go back to Fls to get value.
    }
#endif
}

void CExecutionEngine::SwitchOut()
{
    // No real contracts here.  This function is called by Thread::SwitchOut
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_ENTRY_POINT;

#ifdef HAS_FLS_SUPPORT
    // @TODO - doesn't look like we can probe here.
    if (fHasFlsSupport && pFlsGetValue != NULL  && (void **)pFlsGetValue(FlsIndex) != NULL)
    {
        // Clear out TLS unless we're in the process of ThreadDetach 
        // We establish that we're in ThreadDetach because fHasFlsSupport will
        // be TRUE, but the FLS will not exist.
        CExecutionEngine::SetTlsData(NULL);
    }
#endif // HAS_FLS_SUPPORT
}

static void ThreadDetachingHelper(PTLS_CALLBACK_FUNCTION callback, void* pData)
{
    // Do not use contract.  We are freeing TLS blocks.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

        callback(pData);
    }

// Called here from a thread detach or from destruction of a Thread object.  In
// the detach case, we get our info from TLS.  In the destruct case, it comes from
// the object we are destructing.
void CExecutionEngine::ThreadDetaching(void ** pTlsData)
{
    // Can not cause memory allocation during thread detach, so no real contracts.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    // This function may be called twice:
    // 1. When a physical thread dies, our DLL_THREAD_DETACH calls this function with pTlsData = NULL
    // 2. When a fiber is destroyed, or OS calls FlsCallback after DLL_THREAD_DETACH process.
    // We will null the FLS and TLS entry if it matches the deleted one.

    if (pTlsData)
    {
        DeleteTLS (pTlsData);
    }
}

void CExecutionEngine::DeleteTLS(void ** pTlsData)
{
    // Can not cause memory allocation during thread detach, so no real contracts.
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_ANY;

    if (CExecutionEngine::GetTlsData() == NULL)
    {
        // We have not allocated TlsData yet.
        return;
    }

    PREFIX_ASSUME(pTlsData != NULL);

    ClrTlsInfo *pTlsInfo = DataToClrTlsInfo(pTlsData);
    BOOL fNeed;
    do
    {
        fNeed = FALSE;
        for (int i=0; i<MAX_PREDEFINED_TLS_SLOT; i++)
        {
            if (i == TlsIdx_ClrDebugState ||
                i == TlsIdx_StressLog)
            {
                // StressLog and DebugState may be needed during callback.
                continue;
            }
            // If we have some data and a callback, issue it.
            if (Callbacks[i] != 0 && pTlsInfo->data[i] != 0)
            {
                void* pData = pTlsInfo->data[i];
                pTlsInfo->data[i] = 0;
                ThreadDetachingHelper(Callbacks[i], pData);
                fNeed = TRUE;
            }
        }
    } while (fNeed);

    if (pTlsInfo->data[TlsIdx_StressLog] != 0)
    {
#ifdef STRESS_LOG
        StressLog::ThreadDetach((ThreadStressLog *)pTlsInfo->data[TlsIdx_StressLog]);
#else
        _ASSERTE (!"should not have StressLog");
#endif
    }

    if (Callbacks[TlsIdx_ClrDebugState] != 0 && pTlsInfo->data[TlsIdx_ClrDebugState] != 0)
    {
        void* pData = pTlsInfo->data[TlsIdx_ClrDebugState];
        pTlsInfo->data[TlsIdx_ClrDebugState] = 0;
        ThreadDetachingHelper(Callbacks[TlsIdx_ClrDebugState], pData);
    }

#ifdef _DEBUG
    Thread *pThread = GetThread();
    if (pThread)
    {
        pThread->AddFiberInfo(Thread::ThreadTrackInfo_Lifetime);
    }
#endif

    // NULL TLS and FLS entry so that we don't double free.
    // We may get two callback here on thread death
    // 1. From EEDllMain
    // 2. From OS callback on FLS destruction
    if (CExecutionEngine::GetTlsData() == pTlsData)
    {
        CExecutionEngine::SetTlsData(0);
    }

#ifdef HAS_FLS_SUPPORT
    if (fHasFlsSupport && pFlsGetValue(FlsIndex) == pTlsData)
    {
        pFlsSetValue(FlsIndex, NULL);
    }
#endif

#undef HeapFree
#undef GetProcessHeap
    ::HeapFree (GetProcessHeap(),0,pTlsInfo);
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
#define GetProcessHeap() Dont_Use_GetProcessHeap()

}

#ifdef ENABLE_CONTRACTS_IMPL
// Fls callback to deallocate ClrDebugState when our FLS block goes away.
void FreeClrDebugState(LPVOID pTlsData);
#endif

VOID STDMETHODCALLTYPE CExecutionEngine::TLS_AssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    CheckThreadState(slot);

    // They can toggle between a callback and no callback.  But anything else looks like
    // confusion on their part.
    //
    // (TlsIdx_ClrDebugState associates its callback from utilcode.lib - which can be replicated. But
    // all the callbacks are equally good.)
    _ASSERTE(slot == TlsIdx_ClrDebugState || Callbacks[slot] == 0 || Callbacks[slot] == callback || callback == 0);
    if (slot == TlsIdx_ClrDebugState)
    {
#ifdef ENABLE_CONTRACTS_IMPL
        // ClrDebugState is shared among many dlls.  Some dll, like perfcounter.dll, may be unloaded.
        // We force the callback function to be in mscorwks.dll.
        Callbacks[slot] = FreeClrDebugState;
#else
        _ASSERTE (!"should not get here");
#endif
    }
    else
        Callbacks[slot] = callback;
}

LPVOID* STDMETHODCALLTYPE CExecutionEngine::TLS_GetDataBlock()
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    STATIC_CONTRACT_SO_TOLERANT;

    return CExecutionEngine::GetTlsData();
}

LPVOID STDMETHODCALLTYPE CExecutionEngine::TLS_GetValue(DWORD slot)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EETlsGetValue(slot);
}

BOOL STDMETHODCALLTYPE CExecutionEngine::TLS_CheckValue(DWORD slot, LPVOID * pValue)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EETlsCheckValue(slot, pValue);
}

VOID STDMETHODCALLTYPE CExecutionEngine::TLS_SetValue(DWORD slot, LPVOID pData)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    EETlsSetValue(slot,pData);
}


VOID STDMETHODCALLTYPE CExecutionEngine::TLS_ThreadDetaching()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    CExecutionEngine::ThreadDetaching(NULL);
}


CRITSEC_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateLock(LPCSTR szTag, LPCSTR level, CrstFlags flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    CRITSEC_COOKIE cookie = NULL;
    BEGIN_ENTRYPOINT_VOIDRET;
    cookie = ::EECreateCriticalSection(*(CrstType*)&level, flags);
    END_ENTRYPOINT_VOIDRET;
    return cookie;
}

void STDMETHODCALLTYPE CExecutionEngine::DestroyLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    ::EEDeleteCriticalSection(cookie);
}

void STDMETHODCALLTYPE CExecutionEngine::AcquireLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    BEGIN_SO_INTOLERANT_CODE(GetThread());
    ::EEEnterCriticalSection(cookie);
    END_SO_INTOLERANT_CODE;
}

void STDMETHODCALLTYPE CExecutionEngine::ReleaseLock(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    BEGIN_SO_INTOLERANT_CODE(GetThread());
    ::EELeaveCriticalSection(cookie);
    END_SO_INTOLERANT_CODE;
}

// Locking routines supplied by the EE to the other DLLs of the CLR.  In a _DEBUG
// build of the EE, we poison the Crst as a poor man's attempt to do some argument
// validation.
#define POISON_BITS 3

static inline EVENT_COOKIE CLREventToCookie(CLREvent * pEvent)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) pEvent) & POISON_BITS) == 0);
#ifdef _DEBUG
    pEvent = (CLREvent *) (((uintptr_t) pEvent) | POISON_BITS);
#endif
    return (EVENT_COOKIE) pEvent;
}

static inline CLREvent *CookieToCLREvent(EVENT_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (EVENT_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLREvent *) cookie;
}


EVENT_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateAutoEvent(BOOL bInitialState)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    EVENT_COOKIE event = NULL;
    BEGIN_ENTRYPOINT_THROWS;
    NewHolder<CLREvent> pEvent(new CLREvent());
    pEvent->CreateAutoEvent(bInitialState);
    event = CLREventToCookie(pEvent);
    pEvent.SuppressRelease();
    END_ENTRYPOINT_THROWS;

    return event;
}

EVENT_COOKIE STDMETHODCALLTYPE CExecutionEngine::CreateManualEvent(BOOL bInitialState)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    EVENT_COOKIE event = NULL;
    BEGIN_ENTRYPOINT_THROWS;

    NewHolder<CLREvent> pEvent(new CLREvent());
    pEvent->CreateManualEvent(bInitialState);
    event = CLREventToCookie(pEvent);
    pEvent.SuppressRelease();

    END_ENTRYPOINT_THROWS;

    return event;
}

void STDMETHODCALLTYPE CExecutionEngine::CloseEvent(EVENT_COOKIE event)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        pEvent->CloseEvent();
        delete pEvent;
    }
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrSetEvent(EVENT_COOKIE event)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Set();
    }
    return FALSE;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrResetEvent(EVENT_COOKIE event)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Reset();
    }
    return FALSE;
}

DWORD STDMETHODCALLTYPE CExecutionEngine::WaitForEvent(EVENT_COOKIE event,
                                                       DWORD dwMilliseconds,
                                                       BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    if (event) {
        CLREvent *pEvent = CookieToCLREvent(event);
        return pEvent->Wait(dwMilliseconds,bAlertable);
    }

    if (GetThread() && bAlertable)
        ThrowHR(E_INVALIDARG);
    return WAIT_FAILED;
}

DWORD STDMETHODCALLTYPE CExecutionEngine::WaitForSingleObject(HANDLE handle,
                                                              DWORD dwMilliseconds)
{
    STATIC_CONTRACT_WRAPPER;
    STATIC_CONTRACT_SO_TOLERANT;
    LeaveRuntimeHolder holder((size_t)(::WaitForSingleObject));
    return ::WaitForSingleObject(handle,dwMilliseconds);
}

static inline SEMAPHORE_COOKIE CLRSemaphoreToCookie(CLRSemaphore * pSemaphore)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    _ASSERTE((((uintptr_t) pSemaphore) & POISON_BITS) == 0);
#ifdef _DEBUG
    pSemaphore = (CLRSemaphore *) (((uintptr_t) pSemaphore) | POISON_BITS);
#endif
    return (SEMAPHORE_COOKIE) pSemaphore;
}

static inline CLRSemaphore *CookieToCLRSemaphore(SEMAPHORE_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (SEMAPHORE_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLRSemaphore *) cookie;
}


SEMAPHORE_COOKIE STDMETHODCALLTYPE CExecutionEngine::ClrCreateSemaphore(DWORD dwInitial,
                                                                        DWORD dwMax)
{
    CONTRACTL
    {
        THROWS;
        MODE_ANY;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    NewHolder<CLRSemaphore> pSemaphore(new CLRSemaphore());
    pSemaphore->Create(dwInitial, dwMax);
    SEMAPHORE_COOKIE ret = CLRSemaphoreToCookie(pSemaphore);;
    pSemaphore.SuppressRelease();
    return ret;
}

void STDMETHODCALLTYPE CExecutionEngine::ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    pSemaphore->Close();
    delete pSemaphore;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore,
                                                             LONG lReleaseCount,
                                                             LONG *lpPreviousCount)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    return pSemaphore->Release(lReleaseCount,lpPreviousCount);
}

DWORD STDMETHODCALLTYPE CExecutionEngine::ClrWaitForSemaphore(SEMAPHORE_COOKIE semaphore,
                                                              DWORD dwMilliseconds,
                                                              BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    CLRSemaphore *pSemaphore = CookieToCLRSemaphore(semaphore);
    return pSemaphore->Wait(dwMilliseconds,bAlertable);
}

static inline MUTEX_COOKIE CLRMutexToCookie(CLRMutex * pMutex)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) pMutex) & POISON_BITS) == 0);
#ifdef _DEBUG
    pMutex = (CLRMutex *) (((uintptr_t) pMutex) | POISON_BITS);
#endif
    return (MUTEX_COOKIE) pMutex;
}

static inline CLRMutex *CookieToCLRMutex(MUTEX_COOKIE cookie)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE((((uintptr_t) cookie) & POISON_BITS) == POISON_BITS);
#ifdef _DEBUG
    if (cookie)
    {
    cookie = (MUTEX_COOKIE) (((uintptr_t) cookie) & ~POISON_BITS);
    }
#endif
    return (CLRMutex *) cookie;
}


MUTEX_COOKIE STDMETHODCALLTYPE CExecutionEngine::ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                                                                BOOL bInitialOwner,
                                                                LPCTSTR lpName)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_NOTRIGGER;
        SO_TOLERANT;    // we catch any erros and free the allocated memory
    }
    CONTRACTL_END;


    MUTEX_COOKIE mutex = 0;
    CLRMutex *pMutex = new (nothrow) CLRMutex();
    if (pMutex)
    {
        EX_TRY
        {
            pMutex->Create(lpMutexAttributes, bInitialOwner, lpName);
            mutex = CLRMutexToCookie(pMutex);
        }
        EX_CATCH
        {
            delete pMutex;
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
    return mutex;
}

void STDMETHODCALLTYPE CExecutionEngine::ClrCloseMutex(MUTEX_COOKIE mutex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    pMutex->Close();
    delete pMutex;
}

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    return pMutex->Release();
}

DWORD STDMETHODCALLTYPE CExecutionEngine::ClrWaitForMutex(MUTEX_COOKIE mutex,
                                                          DWORD dwMilliseconds,
                                                          BOOL bAlertable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    CLRMutex *pMutex = CookieToCLRMutex(mutex);
    return pMutex->Wait(dwMilliseconds,bAlertable);
}

#undef ClrSleepEx
DWORD STDMETHODCALLTYPE CExecutionEngine::ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    return EESleepEx(dwMilliseconds,bAlertable);
}
#define ClrSleepEx EESleepEx

#undef ClrAllocationDisallowed
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrAllocationDisallowed()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEAllocationDisallowed();
}
#define ClrAllocationDisallowed EEAllocationDisallowed

#undef ClrVirtualAlloc
LPVOID STDMETHODCALLTYPE CExecutionEngine::ClrVirtualAlloc(LPVOID lpAddress,
                                                           SIZE_T dwSize,
                                                           DWORD flAllocationType,
                                                           DWORD flProtect)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
}
#define ClrVirtualAlloc EEVirtualAlloc

#undef ClrVirtualFree
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrVirtualFree(LPVOID lpAddress,
                                                        SIZE_T dwSize,
                                                        DWORD dwFreeType)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualFree(lpAddress, dwSize, dwFreeType);
}
#define ClrVirtualFree EEVirtualFree

#undef ClrVirtualQuery
SIZE_T STDMETHODCALLTYPE CExecutionEngine::ClrVirtualQuery(LPCVOID lpAddress,
                                                           PMEMORY_BASIC_INFORMATION lpBuffer,
                                                           SIZE_T dwLength)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEVirtualQuery(lpAddress, lpBuffer, dwLength);
}
#define ClrVirtualQuery EEVirtualQuery

#if defined(_DEBUG) && defined(FEATURE_CORECLR) && !defined(FEATURE_PAL)
static VolatilePtr<BYTE> s_pStartOfUEFSection = NULL;
static VolatilePtr<BYTE> s_pEndOfUEFSectionBoundary = NULL;
static Volatile<DWORD> s_dwProtection = 0;
#endif // _DEBUG && FEATURE_CORECLR && !FEATURE_PAL

#undef ClrVirtualProtect

BOOL STDMETHODCALLTYPE CExecutionEngine::ClrVirtualProtect(LPVOID lpAddress,
                                                           SIZE_T dwSize,
                                                           DWORD flNewProtect,
                                                           PDWORD lpflOldProtect)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

   // Get the UEF installation details - we will use these to validate
   // that the calls to ClrVirtualProtect are not going to affect the UEF.
   // 
   // The OS UEF invocation mechanism was updated. When a UEF is setup,the OS captures 
   // the following details about it:
   //  1) Protection of the pages in which the UEF lives
   //  2) The size of the region in which the UEF lives
   //  3) The region's Allocation Base
   //
   //  The OS verifies details surrounding the UEF before invocation.  For security reasons 
   //  the page protection cannot change between SetUnhandledExceptionFilter and invocation.
   //
   // Prior to this change, the UEF lived in a common section of code_Seg, along with
   // JIT_PatchedCode. Thus, their pages have the same protection, they live
   //  in the same region (and thus, its size is the same).
   //
   // In EEStartupHelper, when we setup the UEF and then invoke InitJitHelpers1 and InitJitHelpers2, 
   // they perform some optimizations that result in the memory page protection being changed. When
   // the UEF is to be invoked, the OS does the check on the UEF's cached details against the current
   // memory pages. This check used to fail when on 64bit retail builds when JIT_PatchedCode was
   // aligned after the UEF with a different memory page protection (post the optimizations by InitJitHelpers). 
   // Thus, the UEF was never invoked.
   //
   // To circumvent this, we put the UEF in its own section in the code segment so that any modifications
   // to memory pages will not affect the UEF details that the OS cached. This is done in Excep.cpp
   // using the "#pragma code_seg" directives.
   //
   // Below, we double check that:
   // 
   // 1) the address being protected does not lie in the region of of the UEF. 
   // 2) the section after UEF is not having the same memory protection as UEF section.
   //
   // We assert if either of the two conditions above are true.

#if defined(_DEBUG) && defined(FEATURE_CORECLR) && !defined(FEATURE_PAL)
   // We do this check in debug/checked builds only

    // Do we have the UEF details?
    if (s_pEndOfUEFSectionBoundary.Load() == NULL)
    {
        // Get reference to MSCORWKS image in memory...
        PEDecoder pe(g_pMSCorEE);

        // Find the UEF section from the image
        IMAGE_SECTION_HEADER* pUEFSection = pe.FindSection(CLR_UEF_SECTION_NAME);
        _ASSERTE(pUEFSection != NULL);
        if (pUEFSection)
        {
            // We got our section - get the start of the section
            BYTE* pStartOfUEFSection = static_cast<BYTE*>(pe.GetBase())+pUEFSection->VirtualAddress;
            s_pStartOfUEFSection = pStartOfUEFSection;

            // Now we need the protection attributes for the memory region in which the
            // UEF section is...
            MEMORY_BASIC_INFORMATION uefInfo;
            if (ClrVirtualQuery(pStartOfUEFSection, &uefInfo, sizeof(uefInfo)) != 0)
            {
                // Calculate how many pages does the UEF section take to get to the start of the
                // next section. We dont calculate this as
                //
                // pStartOfUEFSection + uefInfo.RegionSize
                //
                // because the section following UEF will also be included in the region size
                // if it has the same protection as the UEF section.
                DWORD dwUEFSectionPageCount = ((pUEFSection->Misc.VirtualSize + OS_PAGE_SIZE - 1)/OS_PAGE_SIZE);

                BYTE* pAddressOfFollowingSection = pStartOfUEFSection + (OS_PAGE_SIZE * dwUEFSectionPageCount);
                
                // Ensure that the section following us is having different memory protection
                MEMORY_BASIC_INFORMATION nextSectionInfo;
                _ASSERTE(ClrVirtualQuery(pAddressOfFollowingSection, &nextSectionInfo, sizeof(nextSectionInfo)) != 0);
                _ASSERTE(nextSectionInfo.Protect != uefInfo.Protect);
                
                // save the memory protection details
                s_dwProtection = uefInfo.Protect;

                // Get the end of the UEF section
                BYTE* pEndOfUEFSectionBoundary = pAddressOfFollowingSection - 1;
                
                // Set the end of UEF section boundary
                FastInterlockExchangePointer(s_pEndOfUEFSectionBoundary.GetPointer(), pEndOfUEFSectionBoundary);
            }
            else
            {
                _ASSERTE(!"Unable to get UEF Details!");
            }
        }
    }

    if (s_pEndOfUEFSectionBoundary.Load() != NULL)
    {
        // Is the protection being changed?
        if (flNewProtect != s_dwProtection)
        {
            // Is the target address NOT affecting the UEF ? Possible cases:
            // 1) Starts and ends before the UEF start
            // 2) Starts after the UEF start

            void* pEndOfRangeAddr = static_cast<BYTE*>(lpAddress)+dwSize-1;

            _ASSERTE_MSG(((pEndOfRangeAddr < s_pStartOfUEFSection.Load()) || (lpAddress > s_pEndOfUEFSectionBoundary.Load())), 
                "Do not virtual protect the section in which UEF lives!");
        }
    }
#endif // _DEBUG && FEATURE_CORECLR && !FEATURE_PAL

    return EEVirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}
#define ClrVirtualProtect EEVirtualProtect

#undef ClrGetProcessHeap
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrGetProcessHeap()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEGetProcessHeap();
}
#define ClrGetProcessHeap EEGetProcessHeap

#undef ClrGetProcessExecutableHeap
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrGetProcessExecutableHeap()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEGetProcessExecutableHeap();
}
#define ClrGetProcessExecutableHeap EEGetProcessExecutableHeap


#undef ClrHeapCreate
HANDLE STDMETHODCALLTYPE CExecutionEngine::ClrHeapCreate(DWORD flOptions,
                                                         SIZE_T dwInitialSize,
                                                         SIZE_T dwMaximumSize)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapCreate(flOptions, dwInitialSize, dwMaximumSize);
}
#define ClrHeapCreate EEHeapCreate

#undef ClrHeapDestroy
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapDestroy(HANDLE hHeap)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapDestroy(hHeap);
}
#define ClrHeapDestroy EEHeapDestroy

#undef ClrHeapAlloc
LPVOID STDMETHODCALLTYPE CExecutionEngine::ClrHeapAlloc(HANDLE hHeap,
                                                        DWORD dwFlags,
                                                        SIZE_T dwBytes)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    // We need to guarentee a very small stack consumption in allocating.  And we can't allow
    // an SO to happen while calling into the host.  This will force a hard SO which is OK because
    // we shouldn't ever get this close inside the EE in SO-intolerant code, so this should
    // only fail if we call directly in from outside the EE, such as the JIT.
    MINIMAL_STACK_PROBE_CHECK_THREAD(GetThread());

    return EEHeapAlloc(hHeap, dwFlags, dwBytes);
}
#define ClrHeapAlloc EEHeapAlloc

#undef ClrHeapFree
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapFree(HANDLE hHeap,
                                                     DWORD dwFlags,
                                                     LPVOID lpMem)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapFree(hHeap, dwFlags, lpMem);
}
#define ClrHeapFree EEHeapFree

#undef ClrHeapValidate
BOOL STDMETHODCALLTYPE CExecutionEngine::ClrHeapValidate(HANDLE hHeap,
                                                         DWORD dwFlags,
                                                         LPCVOID lpMem)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;
    return EEHeapValidate(hHeap, dwFlags, lpMem);
}
#define ClrHeapValidate EEHeapValidate

//------------------------------------------------------------------------------
// Helper function to get an exception object from outside the exception.  In
//  the CLR, it may be from the Thread object.  Non-CLR users have no thread object,
//  and it will do nothing.

void CExecutionEngine::GetLastThrownObjectExceptionFromThread(void **ppvException)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

    // Cast to our real type.
    Exception **ppException = reinterpret_cast<Exception**>(ppvException);

    // Try to get a better message.
    GetLastThrownObjectExceptionFromThread_Internal(ppException);

} // HRESULT CExecutionEngine::GetLastThrownObjectExceptionFromThread()


#ifdef FEATURE_VERSIONING
LocaleID RuntimeGetFileSystemLocale()
{
    return PEImage::GetFileSystemLocale();
};
#endif

#ifdef FEATURE_CORECLR
HRESULT CorHost2::DllGetActivationFactory(DWORD appDomainID, LPCWSTR wszTypeName, IActivationFactory ** factory)
{
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
    // WinRT activation currently supported in default domain only
    if (appDomainID != DefaultADID)
        return HOST_E_INVALIDOPERATION;

    HRESULT hr = S_OK;

    Thread *pThread = GetThread();
    if (pThread == NULL)
    {
        pThread = SetupThreadNoThrow(&hr);
        if (pThread == NULL)
        {
            return hr;
        }
    }

    if(SystemDomain::GetCurrentDomain()->GetId().m_dwId != DefaultADID)
    {
        return HOST_E_INVALIDOPERATION;
    }

    return DllGetActivationFactoryImpl(NULL, wszTypeName, NULL, factory);
#else
    return E_NOTIMPL;
#endif
}
#endif


#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION

HRESULT STDMETHODCALLTYPE DllGetActivationFactoryImpl(LPCWSTR wszAssemblyName, 
                                                      LPCWSTR wszTypeName, 
                                                      LPCWSTR wszCodeBase,
                                                      IActivationFactory ** factory)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;

    AppDomain* pDomain = SystemDomain::System()->DefaultDomain();
    _ASSERTE(pDomain);
#ifndef FEATURE_CORECLR // coreclr uses winrt binder which does not allow redirects
    {
        BaseDomain::LockHolder lh(pDomain);
        if (!pDomain->HasLoadContextHostBinder())
        {
            // don't allow redirects
            SystemDomain::InitializeDefaultDomain(FALSE);
        }
    }
#endif

    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        GCX_COOP();

        bool bIsPrimitive;
        TypeHandle typeHandle = WinRTTypeNameConverter::GetManagedTypeFromWinRTTypeName(wszTypeName, &bIsPrimitive);
        if (!bIsPrimitive && !typeHandle.IsNull() && !typeHandle.IsTypeDesc() && typeHandle.AsMethodTable()->IsExportedToWinRT())
        {
            struct _gc {
                OBJECTREF type;
            } gc;
            memset(&gc, 0, sizeof(gc));

#if defined(FEATURE_MULTICOREJIT) && defined(FEATURE_APPX_BINDER)
            // For Appx, multicore JIT is only needed when root assembly does not have NI image
            // When it has NI image, we can't generate profile, and do not need to playback profile
            if (AppX::IsAppXProcess() && ! typeHandle.IsZapped())
            {
                GCX_PREEMP();

                pDomain->GetMulticoreJitManager().AutoStartProfileAppx(pDomain);
            }
#endif

            IActivationFactory* activationFactory;
            GCPROTECT_BEGIN(gc);

            gc.type = typeHandle.GetManagedClassObject();

            MethodDescCallSite mdcs(METHOD__WINDOWSRUNTIMEMARSHAL__GET_ACTIVATION_FACTORY_FOR_TYPE);
            ARG_SLOT args[1] = {
                ObjToArgSlot(gc.type)
            };
            activationFactory = (IActivationFactory*)mdcs.Call_RetLPVOID(args);

            *factory = activationFactory;

            GCPROTECT_END();
        }
        else
        {
            hr = COR_E_TYPELOAD;
        }
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // !FEATURE_COMINTEROP_MANAGED_ACTIVATION


#ifdef FEATURE_COMINTEROP_WINRT_DESKTOP_HOST

HRESULT STDMETHODCALLTYPE GetClassActivatorForApplicationImpl(HSTRING appPath, IWinRTClassActivator** ppActivator)
{
    CONTRACTL
    {
        DISABLED(NOTHROW);
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    BEGIN_ENTRYPOINT_NOTHROW;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    {
        if (GetAppDomain()->GetWinRtBinder()->SetLocalWinMDPath(appPath))
        {
            GCX_COOP();

            struct
            {
                STRINGREF appbase;
            } gc;
            ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            UINT32 appPathLength = 0;
            PCWSTR wszAppPath = WindowsGetStringRawBuffer(appPath, &appPathLength);

            gc.appbase = StringObject::NewString(wszAppPath, appPathLength);

            MethodDescCallSite getClassActivator(METHOD__WINDOWSRUNTIMEMARSHAL__GET_CLASS_ACTIVATOR_FOR_APPLICATION);

            ARG_SLOT args[] =
            {
                ObjToArgSlot(gc.appbase)
            };

            IWinRTClassActivator* pActivator = reinterpret_cast<IWinRTClassActivator *>(getClassActivator.Call_RetLPVOID(args));
            *ppActivator = pActivator;

            GCPROTECT_END();
        }
        else
        {
            hr = CO_E_BAD_PATH;
        }
    }
    END_EXTERNAL_ENTRYPOINT;
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

#endif // FEATURE_COMINTEROP_WINRT_DESKTOP_HOST


#endif // !DACCESS_COMPILE
