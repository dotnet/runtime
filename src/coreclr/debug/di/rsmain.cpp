// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//*****************************************************************************
// File: RsMain.cpp
//
// Random RS utility stuff, plus root ICorDebug implementation
//
//*****************************************************************************

#include "stdafx.h"
#include "primitives.h"
#include "safewrap.h"

#include "check.h"

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include "corpriv.h"
#include "../../dlls/mscorrc/resource.h"
#include <limits.h>


// The top level Cordb object is built around the Shim
#include "shimpriv.h"

//-----------------------------------------------------------------------------
// For debugging ease, cache some global values.
// Include these in retail & free because that's where we need them the most!!
// Optimized builds may not let us view locals & parameters. So Having these
// cached as global values should let us inspect almost all of
// the interesting parts of the RS even in a Retail build!
//-----------------------------------------------------------------------------

RSDebuggingInfo g_RSDebuggingInfo_OutOfProc = {0 }; // set to NULL
RSDebuggingInfo * g_pRSDebuggingInfo = &g_RSDebuggingInfo_OutOfProc;

// The following instances are used for invoking overloaded new/delete
forDbiWorker forDbi;

#ifdef _DEBUG
// For logs, we can print the string name for the debug codes.
const char * GetDebugCodeName(DWORD dwCode)
{
    if (dwCode < 1 || dwCode > 9)
    {
        return "!Invalid Debug Event Code!";
    }

    static const char * const szNames[] = {
        "(1) EXCEPTION_DEBUG_EVENT",
        "(2) CREATE_THREAD_DEBUG_EVENT",
        "(3) CREATE_PROCESS_DEBUG_EVENT",
        "(4) EXIT_THREAD_DEBUG_EVENT",
        "(5) EXIT_PROCESS_DEBUG_EVENT",
        "(6) LOAD_DLL_DEBUG_EVENT",
        "(7) UNLOAD_DLL_DEBUG_EVENT",
        "(8) OUTPUT_DEBUG_STRING_EVENT",
        "(9) RIP_EVENT",// <-- only on Win9X
    };

    return szNames[dwCode - 1];
}

#endif


//-----------------------------------------------------------------------------
// Per-thread state for Debug builds...
//-----------------------------------------------------------------------------
#ifdef RSCONTRACTS
#ifndef __GNUC__
__declspec(thread) DbgRSThread* DbgRSThread::t_pCurrent;
#else // !__GNUC__
__thread DbgRSThread* DbgRSThread::t_pCurrent;
#endif // !__GNUC__

LONG DbgRSThread::s_Total = 0;

DbgRSThread::DbgRSThread()
{
    m_cInsideRS         = 0;
    m_fIsInCallback     = false;
    m_fIsUnrecoverableErrorCallback = false;

    m_cTotalDbgApiLocks = 0;
    for(int i = 0; i < RSLock::LL_MAX; i++)
    {
        m_cLocks[i] = 0;
    }

    // Initialize Identity info
    m_Cookie = COOKIE_VALUE;
    m_tid = GetCurrentThreadId();
}

// NotifyTakeLock & NotifyReleaseLock are called by RSLock to update the per-thread locking context.
// This will assert if the operation is unsafe (ie, violates lock order).
void DbgRSThread::NotifyTakeLock(RSLock * pLock)
{
    if (pLock->HasLock())
    {
        return;
    }

    int iLevel = pLock->GetLevel();

    // Is it safe to take this lock?
    // Must take "bigger" locks first. We shouldn't hold any locks at our current level either.
    // If this lock is re-entrant and we're double-taking it, we would have returned already.
    // And the locking model on the RS forbids taking multiple locks at the same level.
    for(int i = iLevel; i >= 0; i --)
    {
        bool fHasLowerLock = m_cLocks[i] > 0;
        CONSISTENCY_CHECK_MSGF(!fHasLowerLock, (
            "RSLock violation. Trying to take lock '%s (%d)', but already have smaller lock at level %d'\n",
            pLock->Name(), iLevel,
            i));
    }

    // Update the counts
    _ASSERTE(m_cLocks[iLevel] == 0);
    m_cLocks[iLevel]++;

    if (pLock->IsDbgApiLock())
        m_cTotalDbgApiLocks++;
}

void DbgRSThread::NotifyReleaseLock(RSLock * pLock)
{
    if (pLock->HasLock())
    {
        return;
    }

    int iLevel = pLock->GetLevel();
    m_cLocks[iLevel]--;
    _ASSERTE(m_cLocks[iLevel] == 0);

    if (pLock->IsDbgApiLock())
        m_cTotalDbgApiLocks--;

    _ASSERTE(m_cTotalDbgApiLocks >= 0);
}

void DbgRSThread::TakeVirtualLock(RSLock::ERSLockLevel level)
{
    m_cLocks[level]++;
}

void DbgRSThread::ReleaseVirtualLock(RSLock::ERSLockLevel level)
{
    m_cLocks[level]--;
    _ASSERTE(m_cLocks[level] >= 0);
}


// Get a DbgRSThread for the current OS thread id; lazily create if needed.
DbgRSThread * DbgRSThread::GetThread()
{
    DbgRSThread * p = t_pCurrent;
    if (p == NULL)
    {
        // We lazily create for threads that haven't gone through DllMain
        // Since this is per-thread, we don't need to lock.
        p = DbgRSThread::Create();
    }

    _ASSERTE(p->m_Cookie == COOKIE_VALUE);

    return p;
}



#endif // RSCONTRACTS






#ifdef _DEBUG
LONG CordbCommonBase::s_TotalObjectCount = 0;
LONG CordbCommonBase::s_CordbObjectUID = 0;


LONG CordbCommonBase::m_saDwInstance[enumMaxDerived];
LONG CordbCommonBase::m_saDwAlive[enumMaxDerived];
PVOID CordbCommonBase::m_sdThis[enumMaxDerived][enumMaxThis];

#endif

#ifdef _DEBUG_IMPL
// Mem tracking
LONG Cordb::s_DbgMemTotalOutstandingCordb            = 0;
LONG Cordb::s_DbgMemTotalOutstandingInternalRefs     = 0;
#endif

#ifdef TRACK_OUTSTANDING_OBJECTS
void *Cordb::s_DbgMemOutstandingObjects[MAX_TRACKED_OUTSTANDING_OBJECTS] = { NULL };
LONG Cordb::s_DbgMemOutstandingObjectMax = 0;
#endif

// Default implementation for neutering left-side resources.
void CordbBase::NeuterLeftSideResources()
{
    LIMITED_METHOD_CONTRACT;

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    Neuter();
}

// Default implementation for neutering.
// All derived objects should eventually chain to this.
void CordbBase::Neuter()
{
    // Neutering occurs under the process lock. Neuter can be called twice
    // and so locking protects against races in double-delete.
    // @dbgtodo - , some CordbBase objects (Cordb, CordbProcessEnum),
    // don't have process affinity these should eventually be hoisted to the shim,
    // and then we can enforce.
    CordbProcess * pProcess = GetProcess();
    if (pProcess != NULL)
    {
        _ASSERTE(pProcess->ThreadHoldsProcessLock());
    }
    CordbCommonBase::Neuter();
}

//-----------------------------------------------------------------------------
// NeuterLists
//-----------------------------------------------------------------------------

NeuterList::NeuterList()
{
    m_pHead = NULL;
}

NeuterList::~NeuterList()
{
    // Our owner should have neutered us before deleting us.
    // Thus we should be empty.
    CONSISTENCY_CHECK_MSGF(m_pHead == NULL, ("NeuterList not empty on shutdown. this=0x%p", this));
}

// Wrapper around code:NeuterList::UnsafeAdd
void NeuterList::Add(CordbProcess * pProcess, CordbBase * pObject)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    UnsafeAdd(pProcess, pObject);
}

//
// Add an object to be neutered.
//
// Arguments:
//     pProcess - process that holds lock that will protect the neuter list
//     pObject - object to add
//
// Returns:
//     Throws on error.
//
// Notes:
//     This will add it to the list and maintain an internal reference to it.
//     This will take the process lock.
//
void NeuterList::UnsafeAdd(CordbProcess * pProcess, CordbBase * pObject)
{
    _ASSERTE(pObject != NULL);

    // Lock if needed.
    RSLock * pLock = (pProcess != NULL) ? pProcess->GetProcessLock() : NULL;
    RSLockHolder lockHolder(pLock, FALSE);
    if (pLock != NULL) lockHolder.Acquire();


    Node * pNode = new Node(); // throws on error.
    pNode->m_pObject.Assign(pObject);
    pNode->m_pNext = m_pHead;

    m_pHead = pNode;
}

// Neuter everything on the list and clear it
//
// Arguments:
//     pProcess - process tree that this neuterlist belongs in
//     ticket - neuter ticket proving caller ensured we're safe to neuter.
//
// Assumptions:
//     Caller ensures we're safe to neuter (required to obtain NeuterTicket)
//
// Notes:
//     This will release all internal references and empty the list.
void NeuterList::NeuterAndClear(CordbProcess * pProcess)
{
    RSLock * pLock = (pProcess != NULL) ? pProcess->GetProcessLock() : NULL;
    (void)pLock; //prevent "unused variable" error from GCC
    _ASSERTE((pLock == NULL) || pLock->HasLock());

    while (m_pHead != NULL)
    {
        Node * pTemp = m_pHead;
        m_pHead = m_pHead->m_pNext;

        pTemp->m_pObject->Neuter();
        delete pTemp; // will implicitly release
    }
}

// Only neuter objects that are marked.
// Removes neutered objects from the list.
void NeuterList::SweepAllNeuterAtWillObjects(CordbProcess * pProcess)
{
    _ASSERTE(pProcess != NULL);
    RSLock * pLock = pProcess->GetProcessLock();
    RSLockHolder lockHolder(pLock);

    Node ** ppLast = &m_pHead;
    Node * pCur = m_pHead;

    while (pCur != NULL)
    {
        CordbBase * pObject = pCur->m_pObject;
        if (pObject->IsNeuterAtWill() || pObject->IsNeutered())
        {
            // Delete
            pObject->Neuter();

            Node * pNext = pCur->m_pNext;
            delete pCur; // dtor will implicitly release the internal ref to pObject
            pCur =  *ppLast = pNext;
        }
        else
        {
            // Move to next.
            ppLast = &pCur->m_pNext;
            pCur = pCur->m_pNext;
        }
    }
}

//-----------------------------------------------------------------------------
// Neuters all objects in the list and empties the list.
//
// Notes:
//    See also code:LeftSideResourceCleanupList::SweepNeuterLeftSideResources,
//    which only neuters objects that have been marked as NeuterAtWill (external
//    ref count has gone to 0).
void LeftSideResourceCleanupList::NeuterLeftSideResourcesAndClear(CordbProcess * pProcess)
{
    // Traversal protected under Process-lock.
    // SG-lock must already be held to do neutering.
    // Stop-Go lock is bigger than Process-lock.
    // Neutering requires the Stop-Go lock (until we get rid of IPC events)
    // But we want to be able to add to the Neuter list under the Process-lock.
    // So we just need to protected m_pHead under process-lock.

    // "Privatize" the list under the lock.
    _ASSERTE(pProcess != NULL);
    RSLock * pLock = pProcess->GetProcessLock();

    Node * pCur = NULL;
    {
        RSLockHolder lockHolder(pLock); // only acquire lock if we have one
        pCur = m_pHead;
        m_pHead = NULL;
    }

    // @dbgtodo - eventually everything can be under the process lock.
    _ASSERTE(!pLock->HasLock()); // Can't hold Process lock while calling NeuterLeftSideResources

    // Now we're operating on local data, so traversing doesn't need to be under the lock.
    while (pCur != NULL)
    {
        Node * pTemp = pCur;
        pCur = pCur->m_pNext;

        pTemp->m_pObject->NeuterLeftSideResources();
        delete pTemp; // will implicitly release
    }

}

//-----------------------------------------------------------------------------
// Only neuter objects that are marked. Removes neutered objects from the list.
//
// Arguments:
//    pProcess - non-null process owning the objects in the list
//
// Notes:
//    this cleans up left-side resources held by objects in the list.
//    It may send IPC events to do this.
void LeftSideResourceCleanupList::SweepNeuterLeftSideResources(CordbProcess * pProcess)
{
    _ASSERTE(pProcess != NULL);

    // Must be safe to send IPC events.
    _ASSERTE(pProcess->GetStopGoLock()->HasLock()); // holds this for neutering
    _ASSERTE(pProcess->GetSynchronized());

    RSLock * pLock = pProcess->GetProcessLock();

    // Lock while we "privatize" the head.
    RSLockHolder lockHolder(pLock);
    Node * pHead = m_pHead;
    m_pHead = NULL;
    lockHolder.Release();

    Node ** ppLast = &pHead;
    Node * pCur = pHead;

    // Can't hold the process-lock while calling Neuter.
    while (pCur != NULL)
    {
        CordbBase * pObject = pCur->m_pObject;
        if (pObject->IsNeuterAtWill() || pObject->IsNeutered())
        {
            // HeavyNueter can not be done under the process-lock because
            // it may take the Stop-Go lock and send events.
            pObject->NeuterLeftSideResources();

            // Delete
            Node * pNext = pCur->m_pNext;
            delete pCur; // dtor will implicitly release the internal ref to pObject
            pCur =  *ppLast = pNext;
        }
        else
        {
            // Move to next.
            ppLast = &pCur->m_pNext;
            pCur = pCur->m_pNext;
        }
    }

    // Now link back in. m_pHead may have changed while we were unlocked.
    // The list does not need to be ordered.

    lockHolder.Acquire();
    *ppLast = m_pHead;
    m_pHead = pHead;
}



/* ------------------------------------------------------------------------- *
 * CordbBase class
 * ------------------------------------------------------------------------- */
extern void* GetClrModuleBase();

// Do any initialization necessary for both CorPublish and CorDebug
// This includes enabling logging and adding the SEDebug priv.
void CordbCommonBase::InitializeCommon()
{
    static bool IsInitialized = false;
    if( IsInitialized )
    {
        return;
    }

#ifdef STRESS_LOG
    {
        bool fStressLog = false;

#ifdef _DEBUG
        // default for stress log is on debug build
        fStressLog = true;
#endif // DEBUG

        // StressLog will turn on stress logging for the entire runtime.
        // RSStressLog is only used here and only effects just the RS.
        fStressLog =
            (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StressLog, fStressLog) != 0) ||
            (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_RSStressLog) != 0);

        if (fStressLog == true)
        {
            unsigned facilities = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_LogFacility, LF_ALL);
            unsigned level = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_LogLevel, LL_INFO1000);
            unsigned bytesPerThread = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StressLogSize, STRESSLOG_CHUNK_SIZE * 2);
            unsigned totalBytes = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_TotalStressLogSize, STRESSLOG_CHUNK_SIZE * 1024);
            StressLog::Initialize(facilities, level, bytesPerThread, totalBytes, GetClrModuleBase());
        }
    }

#endif // STRESS_LOG

#ifdef LOGGING
    InitializeLogging();
#endif

    // Add debug privilege. This will let us call OpenProcess() on anything, regardless of ACL.
    AddDebugPrivilege();

    IsInitialized = true;
}

// Adjust the permissions of this process to ensure that we have
// the debugging priviledge. If we can't make the adjustment, it
// only means that we won't be able to attach to a service under
// NT, so we won't treat that as a critical failure.
// This also will let us call OpenProcess() on anything, regardless of DACL. This allows an
// Admin debugger to attach to a debuggee in the guest account.
// Ideally, the debugger would set this (and we wouldn't mess with privileges at all). However, we've been
// setting this since V1.0 and removing it may be a breaking change.
void CordbCommonBase::AddDebugPrivilege()
{
#ifndef TARGET_UNIX
    HANDLE hToken;
    TOKEN_PRIVILEGES Privileges;
    BOOL fSucc;

    LUID SeDebugLuid = {0, 0};

    fSucc = LookupPrivilegeValueW(NULL, SE_DEBUG_NAME, &SeDebugLuid);
    DWORD err = GetLastError();

    if (!fSucc)
    {
        STRESS_LOG1(LF_CORDB, LL_INFO1000, "Unable to adjust permissions of this process to include SE_DEBUG. Lookup failed %d\n", err);
        return;
    }


    // Retrieve a handle of the access token
    fSucc = OpenProcessToken(GetCurrentProcess(),
                             TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
                             &hToken);

    if (fSucc)
    {
        Privileges.PrivilegeCount = 1;
        Privileges.Privileges[0].Luid = SeDebugLuid;
        Privileges.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;

        AdjustTokenPrivileges(hToken,
                              FALSE,
                              &Privileges,
                              sizeof(TOKEN_PRIVILEGES),
                              (PTOKEN_PRIVILEGES) NULL,
                              (PDWORD) NULL);
        err = GetLastError();
        // The return value of AdjustTokenPrivileges cannot be tested.
        if (err != ERROR_SUCCESS)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO1000,
                "Unable to adjust permissions of this process to include SE_DEBUG. Adjust failed %d\n", err);
        }
        else
        {
            LOG((LF_CORDB, LL_INFO1000, "Adjusted process permissions to include SE_DEBUG.\n"));
        }
        CloseHandle(hToken);
    }
#endif
}


namespace
{

    //
    // DefaultManagedCallback2
    //
    // In the event that the debugger is of an older version than the Right Side & Left Side, the Right Side may issue
    // new callbacks that the debugger is not expecting. In this case, we need to provide a default behavior for those
    // new callbacks, if for nothing else than to force the debugger to Continue().
    //
    class DefaultManagedCallback2 : public ICorDebugManagedCallback2
    {
    public:
        DefaultManagedCallback2(ICorDebug* pDebug);
        virtual ~DefaultManagedCallback2() { }
        virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** pInterface);
        virtual ULONG STDMETHODCALLTYPE AddRef();
        virtual ULONG STDMETHODCALLTYPE Release();
        COM_METHOD FunctionRemapOpportunity(ICorDebugAppDomain* pAppDomain,
                                                 ICorDebugThread* pThread,
                                                 ICorDebugFunction* pOldFunction,
                                                 ICorDebugFunction* pNewFunction,
                                                 ULONG32 oldILOffset);
        COM_METHOD FunctionRemapComplete(ICorDebugAppDomain *pAppDomain,
                                    ICorDebugThread *pThread,
                                    ICorDebugFunction *pFunction);

        COM_METHOD CreateConnection(ICorDebugProcess *pProcess,
                                    CONNID dwConnectionId,
                                    _In_z_ WCHAR* pConnectionName);
        COM_METHOD ChangeConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId);
        COM_METHOD DestroyConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId);

        COM_METHOD Exception(ICorDebugAppDomain *pAddDomain,
                             ICorDebugThread *pThread,
                             ICorDebugFrame *pFrame,
                             ULONG32 nOffset,
                             CorDebugExceptionCallbackType eventType,
                             DWORD dwFlags );

        COM_METHOD ExceptionUnwind(ICorDebugAppDomain *pAddDomain,
                                   ICorDebugThread *pThread,
                                   CorDebugExceptionUnwindCallbackType eventType,
                                   DWORD dwFlags );
        COM_METHOD MDANotification(
                            ICorDebugController * pController,
                            ICorDebugThread *pThread,
                            ICorDebugMDA * pMDA
        ) { return E_NOTIMPL; }

    private:
        // not implemented
        DefaultManagedCallback2(const DefaultManagedCallback2&);
        DefaultManagedCallback2& operator=(const DefaultManagedCallback2&);

        ICorDebug* m_pDebug;
        LONG m_refCount;
    };




    DefaultManagedCallback2::DefaultManagedCallback2(ICorDebug* pDebug) : m_pDebug(pDebug), m_refCount(0)
    {
    }

    HRESULT
    DefaultManagedCallback2::QueryInterface(REFIID iid, void** pInterface)
    {
        if (IID_ICorDebugManagedCallback2 == iid)
        {
            *pInterface = static_cast<ICorDebugManagedCallback2*>(this);
        }
        else if (IID_IUnknown == iid)
        {
            *pInterface = static_cast<IUnknown*>(this);
        }
        else
        {
            *pInterface = NULL;
            return E_NOINTERFACE;
        }

        this->AddRef();
        return S_OK;
    }

    ULONG
    DefaultManagedCallback2::AddRef()
    {
        return InterlockedIncrement(&m_refCount);
    }

    ULONG
    DefaultManagedCallback2::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_refCount);
        if (0 == ulRef)
        {
            delete this;
        }

        return ulRef;
    }

    HRESULT
    DefaultManagedCallback2::FunctionRemapOpportunity(ICorDebugAppDomain* pAppDomain,
                                                      ICorDebugThread* pThread,
                                                      ICorDebugFunction* pOldFunction,
                                                      ICorDebugFunction* pNewFunction,
                                                      ULONG32 oldILOffset)
    {

        //
        // In theory, this function should never be reached. To get here, we'd have to have a debugger which doesn't
        // support edit and continue somehow turn on edit & continue features.
        //
        _ASSERTE(!"Edit & Continue callback reached when debugger doesn't support Edit And Continue");


        // If you ignore this assertion, or you're in a retail build, there are two options as far as how to proceed
        // from this point
        //  o We can do nothing, and let the debugee process hang, or
        //  o We can silently ignore the FunctionRemapOpportunity, and tell the debugee to Continue running.
        //
        // For now, we'll silently ignore the function remapping.
        pAppDomain->Continue(false);
        pAppDomain->Release();

        return S_OK;
    }


    HRESULT
    DefaultManagedCallback2::FunctionRemapComplete(ICorDebugAppDomain *pAppDomain,
                          ICorDebugThread *pThread,
                          ICorDebugFunction *pFunction)
    {
        //
        // In theory, this function should never be reached. To get here, we'd have to have a debugger which doesn't
        // support edit and continue somehow turn on edit & continue features.
        //
        _ASSERTE(!"Edit & Continue callback reached when debugger doesn't support Edit And Continue");
        return E_NOTIMPL;
    }

    //
    // <TODO>
    // These methods are current left unimplemented.
    //
    // Create/Change/Destroy Connection *should* force the Process/AppDomain/Thread to Continue(). Currently the
    // arguments to these functions don't provide the relevant Process/AppDomain/Thread, so there is no way to figure
    // out which Threads should be forced to Continue().
    //
    // </TODO>
    //
    HRESULT
    DefaultManagedCallback2::CreateConnection(ICorDebugProcess *pProcess,
                                              CONNID dwConnectionId,
                                              _In_z_ WCHAR* pConnectionName)
    {
        _ASSERTE(!"DefaultManagedCallback2::CreateConnection not implemented");
        return E_NOTIMPL;
    }

    HRESULT
    DefaultManagedCallback2::ChangeConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId)
    {
        _ASSERTE(!"DefaultManagedCallback2::ChangeConnection not implemented");
        return E_NOTIMPL;
    }

    HRESULT
    DefaultManagedCallback2::DestroyConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId)
    {
        _ASSERTE(!"DefaultManagedCallback2::DestroyConnection not implemented");
        return E_NOTIMPL;
    }

    HRESULT
    DefaultManagedCallback2::Exception(ICorDebugAppDomain *pAppDomain,
                                       ICorDebugThread *pThread,
                                       ICorDebugFrame *pFrame,
                                       ULONG32 nOffset,
                                       CorDebugExceptionCallbackType eventType,
                                       DWORD dwFlags )
    {
        //
        // Just ignore and continue the process.
        //
        pAppDomain->Continue(false);
        return S_OK;
    }

    HRESULT
    DefaultManagedCallback2::ExceptionUnwind(ICorDebugAppDomain *pAppDomain,
                                             ICorDebugThread *pThread,
                                             CorDebugExceptionUnwindCallbackType eventType,
                                             DWORD dwFlags )
    {
        //
        // Just ignore and continue the process.
        //
        pAppDomain->Continue(false);
        return S_OK;
    }

    //
    // DefaultManagedCallback3
    //
    // In the event that the debugger is of an older version than the Right Side & Left Side, the Right Side may issue
    // new callbacks that the debugger is not expecting. In this case, we need to provide a default behavior for those
    // new callbacks, if for nothing else than to force the debugger to Continue().
    //
    class DefaultManagedCallback3 : public ICorDebugManagedCallback3
    {
    public:
        DefaultManagedCallback3(ICorDebug* pDebug);
        virtual ~DefaultManagedCallback3() { }
        virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** pInterface);
        virtual ULONG STDMETHODCALLTYPE AddRef();
        virtual ULONG STDMETHODCALLTYPE Release();
        COM_METHOD CustomNotification(ICorDebugThread * pThread, ICorDebugAppDomain * pAppDomain);
    private:
        // not implemented
        DefaultManagedCallback3(const DefaultManagedCallback3&);
        DefaultManagedCallback3& operator=(const DefaultManagedCallback3&);

        ICorDebug* m_pDebug;
        LONG m_refCount;
    };

    DefaultManagedCallback3::DefaultManagedCallback3(ICorDebug* pDebug) : m_pDebug(pDebug), m_refCount(0)
    {
    }

    HRESULT
    DefaultManagedCallback3::QueryInterface(REFIID iid, void** pInterface)
    {
        if (IID_ICorDebugManagedCallback3 == iid)
        {
            *pInterface = static_cast<ICorDebugManagedCallback3*>(this);
        }
        else if (IID_IUnknown == iid)
        {
            *pInterface = static_cast<IUnknown*>(this);
        }
        else
        {
            *pInterface = NULL;
            return E_NOINTERFACE;
        }

        this->AddRef();
        return S_OK;
    }

    ULONG
    DefaultManagedCallback3::AddRef()
    {
        return InterlockedIncrement(&m_refCount);
    }

    ULONG
    DefaultManagedCallback3::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_refCount);
        if (0 == ulRef)
        {
            delete this;
        }

        return ulRef;
    }

    HRESULT
    DefaultManagedCallback3::CustomNotification(ICorDebugThread * pThread, ICorDebugAppDomain * pAppDomain)
    {
        //
        // Just ignore and continue the process.
        //
        pAppDomain->Continue(false);
        return S_OK;
    }

    //
    // DefaultManagedCallback4
    //
    // In the event that the debugger is of an older version than the Right Side & Left Side, the Right Side may issue
    // new callbacks that the debugger is not expecting. In this case, we need to provide a default behavior for those
    // new callbacks, if for nothing else than to force the debugger to Continue().
    //
    class DefaultManagedCallback4 : public ICorDebugManagedCallback4
    {
    public:
        DefaultManagedCallback4(ICorDebug* pDebug);
        virtual ~DefaultManagedCallback4() { }
        virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** pInterface);
        virtual ULONG STDMETHODCALLTYPE AddRef();
        virtual ULONG STDMETHODCALLTYPE Release();
        COM_METHOD BeforeGarbageCollection(ICorDebugProcess* pProcess);
        COM_METHOD AfterGarbageCollection(ICorDebugProcess* pProcess);
        COM_METHOD DataBreakpoint(ICorDebugProcess* pProcess, ICorDebugThread* pThread, BYTE* pContext, ULONG32 contextSize);
    private:
        // not implemented
        DefaultManagedCallback4(const DefaultManagedCallback4&);
        DefaultManagedCallback4& operator=(const DefaultManagedCallback4&);

        ICorDebug* m_pDebug;
        LONG m_refCount;
    };

    DefaultManagedCallback4::DefaultManagedCallback4(ICorDebug* pDebug) : m_pDebug(pDebug), m_refCount(0)
    {
    }

    HRESULT
        DefaultManagedCallback4::QueryInterface(REFIID iid, void** pInterface)
    {
        if (IID_ICorDebugManagedCallback4 == iid)
        {
            *pInterface = static_cast<ICorDebugManagedCallback4*>(this);
        }
        else if (IID_IUnknown == iid)
        {
            *pInterface = static_cast<IUnknown*>(this);
        }
        else
        {
            *pInterface = NULL;
            return E_NOINTERFACE;
        }

        this->AddRef();
        return S_OK;
    }

    ULONG
        DefaultManagedCallback4::AddRef()
    {
        return InterlockedIncrement(&m_refCount);
    }

    ULONG
        DefaultManagedCallback4::Release()
    {
        ULONG ulRef = InterlockedDecrement(&m_refCount);
        if (0 == ulRef)
        {
            delete this;
        }

        return ulRef;
    }

    HRESULT
        DefaultManagedCallback4::BeforeGarbageCollection(ICorDebugProcess* pProcess)
    {
        //
        // Just ignore and continue the process.
        //
        pProcess->Continue(false);
        return S_OK;
    }

    HRESULT
        DefaultManagedCallback4::AfterGarbageCollection(ICorDebugProcess* pProcess)
    {
        //
        // Just ignore and continue the process.
        //
        pProcess->Continue(false);
        return S_OK;
    }

    HRESULT
        DefaultManagedCallback4::DataBreakpoint(ICorDebugProcess* pProcess, ICorDebugThread* pThread, BYTE* pContext, ULONG32 contextSize)
    {
        //
        // Just ignore and continue the process.
        //
        pProcess->Continue(false);
        return S_OK;
    }
}

/* ------------------------------------------------------------------------- *
 * Cordb class
 * ------------------------------------------------------------------------- */
Cordb::Cordb(CorDebugInterfaceVersion iDebuggerVersion)
  : Cordb(iDebuggerVersion, ProcessDescriptor::CreateUninitialized(), NULL)
{
}

Cordb::Cordb(CorDebugInterfaceVersion iDebuggerVersion, const ProcessDescriptor& pd, LPCWSTR dacModulePath)
  : CordbBase(NULL, 0, enumCordb),
    m_processes(11),
    m_initialized(false),
    m_debuggerSpecifiedVersion(iDebuggerVersion),
    m_pd(pd),
    m_dacModulePath(dacModulePath),
    m_targetCLR(0)
{
    g_pRSDebuggingInfo->m_Cordb = this;

#ifdef _DEBUG_IMPL
    // Memory leak detection
    InterlockedIncrement(&s_DbgMemTotalOutstandingCordb);
#endif
}

Cordb::~Cordb()
{
    LOG((LF_CORDB, LL_INFO10, "C::~C Terminating Cordb object.\n"));
    if (m_pd.m_ApplicationGroupId != NULL)
    {
        delete [] m_pd.m_ApplicationGroupId;
    }
    g_pRSDebuggingInfo->m_Cordb = NULL;
}

void Cordb::Neuter()
{
    if (this->IsNeutered())
    {
        return;
    }


    RSLockHolder lockHolder(&m_processListMutex);
    m_pProcessEnumList.NeuterAndClear(NULL);


    HRESULT hr = S_OK;
    EX_TRY // @dbgtodo push this up.
    {
        // Iterating needs to be done under the processList lock (small), while neutering
        // needs to be able to take the process lock (big).
        RSPtrArray<CordbProcess> list;
        m_processes.TransferToArray(&list); // throws

        // can't hold list lock while calling CordbProcess::Neuter (which
        // will take the Process-lock).
        lockHolder.Release();

        list.NeuterAndClear();
        // List dtor calls release on each element
    }
    EX_CATCH_HRESULT(hr);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    CordbCommonBase::Neuter();

    // Implicit release from smart ptr.
}

#ifdef _DEBUG_IMPL
void CheckMemLeaks()
{
    // Memory leak detection.
    LONG l = InterlockedDecrement(&Cordb::s_DbgMemTotalOutstandingCordb);
    if (l == 0)
    {
        // If we just released our final Cordb root object,  then we expect no internal references at all.
        // Note that there may still be external references (and thus not all objects may have been
        // deleted yet).
        bool fLeakedInternal = (Cordb::s_DbgMemTotalOutstandingInternalRefs > 0);

        // Some Cordb objects (such as CordbValues) may not be rooted, and thus we can't neuter
        // them and thus an external ref may keep them alive. Since these objects may have internal refs,
        // This means that external refs can keep internal refs.
        // Thus this assert must be tempered if unrooted objects are leaked. (But that means we can always
        // assert the tempered version; regardless of bugs in Cordbg).
        CONSISTENCY_CHECK_MSGF(!fLeakedInternal,
            ("'%d' Outstanding internal references at final Cordb::Terminate\n",
            Cordb::s_DbgMemTotalOutstandingInternalRefs));

        DWORD dLeakCheck = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgLeakCheck);
        if (dLeakCheck > 0)
        {
            // We have 1 ref for this Cordb root object. All other refs should have been deleted.
            CONSISTENCY_CHECK_MSGF(Cordb::s_TotalObjectCount == 1, ("'%d' total cordbBase objects are leaked.\n",
                Cordb::s_TotalObjectCount-1));
        }
    }
}
#endif

// This shuts down ICorDebug.
// All CordbProcess objects owned by this Cordb object must have either:
// - returned for a Detach() call
// - returned from dispatching the ExitProcess() callback.
// In both cases, CordbProcess::NeuterChildren has been called, although the Process object itself
// may not yet be neutered.  This condition will ensure that the CordbProcess objects don't need
// any resources that we're about to release.
HRESULT Cordb::Terminate()
{
    LOG((LF_CORDB, LL_INFO10000, "[%x] Terminating Cordb\n", GetCurrentThreadId()));

    if (!m_initialized)
        return E_FAIL;

    FAIL_IF_NEUTERED(this);

    // We can't terminate the debugging services from within a callback.
    // Caller is supposed to be out of all callbacks when they call this.
    // This also avoids a deadlock because we'll shutdown the RCET, which would block if we're
    // in the RCET.
    if (m_rcEventThread->IsRCEventThread())
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10, "C::T: failed on RCET\n");
        _ASSERTE(!"Gross API Misuse: Debugger shouldn't call ICorDebug::Terminate from within a managed callback.");
        return CORDBG_E_CANT_CALL_ON_THIS_THREAD;
    }

    // @todo - do we need to throw some switch to prevent new processes from being added now?

    // VS must stop all debugging before terminating. Fail if we have any non-neutered processes
    // (b/c those processes should have been either shutdown or detached).
    // We are in an undefined state if this check fails.
    // Process are removed from this list before Process::Detach() returns and before the ExitProcess callback is dispatched.
    // Thus any processes in this list should be live or have an unrecoverable error.
    {
        RSLockHolder ch(&m_processListMutex);

        HASHFIND hfDT;
        CordbProcess * pProcess;

        for (pProcess=  (CordbProcess*) m_processes.FindFirst(&hfDT);
             pProcess != NULL;
             pProcess = (CordbProcess*) m_processes.FindNext(&hfDT))
        {
            _ASSERTE(pProcess->IsSafeToSendEvents() || pProcess->m_unrecoverableError);
            if (pProcess->IsSafeToSendEvents() && !pProcess->m_unrecoverableError)
            {
                CONSISTENCY_CHECK_MSGF(false, ("Gross API misuses. Callling terminate with live process:0x%p\n", pProcess));
                STRESS_LOG1(LF_CORDB, LL_INFO10, "Cordb::Terminate b/c of non-neutered process '%p'\n", pProcess);
                // This is very bad.
                // GROSS API MISUSES - Debugger is calling ICorDebug::Terminate while there
                // are still outstanding (non-neutered) ICorDebugProcess.
                // ICorDebug is now in an undefined state.
                // We will also leak memory b/c we're leaving the EventThreads up (which will in turn
                // keep a reference to this Cordb object).
                return ErrWrapper(CORDBG_E_ILLEGAL_SHUTDOWN_ORDER);
            }
        }
    }

    // @todo- ideally, we'd wait for all threads to get outside of ICorDebug before we proceed.
    // That's tough to implement in practice; but we at least wait for both ET to exit. As these
    // guys dispatch callbacks, that means at least we'll wait until VS is outside of any callback.
    //
    // Stop the event handling threads.
    //
    if (m_rcEventThread != NULL)
    {
        // Stop may do significant work b/c if it drains the worker queue.
        m_rcEventThread->Stop();
        delete m_rcEventThread;
        m_rcEventThread = NULL;
    }


#ifdef _DEBUG
    // @todo - this disables thread-safety asserts on the process-list-hash. We clearly
    // can't hold the lock while neutering it. (lock violation since who knows what neuter may do)
    // @todo- we may have races beteen Cordb::Terminate and Cordb::CreateProcess as both
    // modify the process list. This is mitigated since Terminate is supposed to be the last method called.
    m_processes.DebugSetRSLock(NULL);
#endif

    //
    // We expect the debugger to neuter all processes before calling Terminate(), so do not neuter them here.
    //

#ifdef _DEBUG
    {
        HASHFIND find;
        _ASSERTE(m_processes.FindFirst(&find) == NULL); // should be emptied by neuter
    }
#endif //_DEBUG

    // Officially mark us as neutered.
    this->Neuter();

    m_processListMutex.Destroy();

    //
    // Release the callbacks
    //
    m_managedCallback.Clear();
    m_managedCallback2.Clear();
    m_managedCallback3.Clear();
    m_managedCallback4.Clear();
    m_unmanagedCallback.Clear();

    // The Shell may still have outstanding references, so we don't want to shutdown logging yet.
    // But everything should be neutered anyways.

    m_initialized = FALSE;


    // After this, all outstanding Cordb objects should be neutered.
    LOG((LF_CORDB, LL_EVERYTHING, "Cordb finished terminating.\n"));

#if defined(_DEBUG)
    //
    // Assert that there are no outstanding object references within the debugging
    // API itself.
    //
    CheckMemLeaks();
#endif

    return S_OK;
}

HRESULT Cordb::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebug)
        *pInterface = static_cast<ICorDebug*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebug*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}



//
// Initialize -- setup the ICorDebug object by creating any objects
// that the object needs to operate and starting the two needed IPC
// threads.
//
HRESULT Cordb::Initialize(void)
{
    HRESULT hr = S_OK;

    FAIL_IF_NEUTERED(this);

    if (!m_initialized)
    {
        CordbCommonBase::InitializeCommon();

        // Since logging wasn't active when we called CordbBase, do it now.
        LOG((LF_CORDB, LL_EVERYTHING, "Memory: CordbBase object allocated: this=%p, count=%d, RootObject\n", this, s_TotalObjectCount));
        LOG((LF_CORDB, LL_INFO10, "Initializing ICorDebug...\n"));

        // Ensure someone hasn't messed up the IPC buffer size
        _ASSERTE(sizeof(DebuggerIPCEvent) <= CorDBIPC_BUFFER_SIZE);

        //
        // Init things that the Cordb will need to operate
        //
        m_processListMutex.Init("Process-List Lock", RSLock::cLockReentrant, RSLock::LL_PROCESS_LIST_LOCK);

#ifdef _DEBUG
        m_processes.DebugSetRSLock(&m_processListMutex);
#endif

        //
        // Create the runtime controller event listening thread
        //
        m_rcEventThread = new (nothrow) CordbRCEventThread(this);

        if (m_rcEventThread == NULL)
        {
            hr = E_OUTOFMEMORY;
        }
        else
        {
            // This stuff only creates events & starts the thread
            hr = m_rcEventThread->Init();

            if (SUCCEEDED(hr))
                hr = m_rcEventThread->Start();

            if (FAILED(hr))
            {
                delete m_rcEventThread;
                m_rcEventThread = NULL;
            }
        }

        if (FAILED(hr))
            goto exit;

       m_initialized = TRUE;
    }

exit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Throw if no more process can be debugged with this Cordb object.
//
// Notes:
//     This is highly dependent on the wait sets in the Win32 & RCET threads.
//     @dbgtodo-  this will end up in the shim.

void Cordb::EnsureAllowAnotherProcess()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    RSLockHolder ch(&m_processListMutex);

    // Cordb, Win32, and RCET all have process sets, but Cordb's is the
    // best count of total debuggees. The RCET set is volatile (processes
    // are added / removed when they become synchronized), and Win32's set
    // doesn't include all processes.
    int cCurProcess = GetProcessList()->GetCount();

    // In order to accept another debuggee, we must have a free slot in all
    // wait sets. Currently, we don't expose the size of those sets, but
    // we know they're MAXIMUM_WAIT_OBJECTS. Note that we lose one slot
    // to the control event.
    if (cCurProcess >= MAXIMUM_WAIT_OBJECTS - 1)
    {
        ThrowHR(CORDBG_E_TOO_MANY_PROCESSES);
    }
}

//---------------------------------------------------------------------------------------
//
// Add process to the list.
//
// Notes:
//     AddProcess -- add a process object to this ICorDebug's hash of processes.
//     This also tells this ICorDebug's runtime controller thread that the
//     process set has changed so it can update its list of wait events.
//
void Cordb::AddProcess(CordbProcess* process)
{
    // At this point, we should have already checked that we
    // can have another debuggee.
    STRESS_LOG1(LF_CORDB, LL_INFO10, "Cordb::AddProcess %08x...\n", process);

    if ((m_managedCallback == NULL) || (m_managedCallback2 == NULL) || (m_managedCallback3 == NULL) || (m_managedCallback4 == NULL))
    {
        ThrowHR(E_FAIL);
    }



    RSLockHolder lockHolder(&m_processListMutex);

    // Once we add another process, all outstanding process-enumerators become invalid.
    m_pProcessEnumList.NeuterAndClear(NULL);

    GetProcessList()->AddBaseOrThrow(process);
    m_rcEventThread->ProcessStateChanged();
}

//
// RemoveProcess -- remove a process object from this ICorDebug's hash of
// processes. This also tells this ICorDebug's runtime controller thread
// that the process set has changed so it can update its list of wait events.
//
void Cordb::RemoveProcess(CordbProcess* process)
{
    STRESS_LOG1(LF_CORDB, LL_INFO10, "Cordb::RemoveProcess %08x...\n", process);

    LockProcessList();
    GetProcessList()->RemoveBase((ULONG_PTR)process->m_id);

    m_rcEventThread->ProcessStateChanged();

    UnlockProcessList();
}

//
// LockProcessList -- Lock the process list.
//
void Cordb::LockProcessList(void)
{
    m_processListMutex.Lock();
}

//
// UnlockProcessList -- Unlock the process list.
//
void Cordb::UnlockProcessList(void)
{
    m_processListMutex.Unlock();
}

#ifdef _DEBUG
// Return true iff this thread owns the ProcessList lock
bool Cordb::ThreadHasProcessListLock()
{
    return m_processListMutex.HasLock();
}
#endif


// Get the hash that has the process.
CordbSafeHashTable<CordbProcess> *Cordb::GetProcessList()
{
    // If we're accessing the hash, we'd better be locked.
    _ASSERTE(ThreadHasProcessListLock());

    return &m_processes;
}


HRESULT Cordb::SendIPCEvent(CordbProcess * pProcess,
                            DebuggerIPCEvent * pEvent,
                            SIZE_T eventSize)
{
    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_EVERYTHING, "SendIPCEvent in Cordb called\n"));
    EX_TRY
    {
        hr = m_rcEventThread->SendIPCEvent(pProcess, pEvent, eventSize);
    }
    EX_CATCH_HRESULT(hr)
    return hr;
}


void Cordb::ProcessStateChanged(void)
{
    m_rcEventThread->ProcessStateChanged();
}


HRESULT Cordb::WaitForIPCEventFromProcess(CordbProcess* process,
                                          CordbAppDomain *pAppDomain,
                                          DebuggerIPCEvent* event)
{
    return m_rcEventThread->WaitForIPCEventFromProcess(process,
                                                       pAppDomain,
                                                       event);
}

HRESULT Cordb::SetTargetCLR(HMODULE hmodTargetCLR)
{
    if (m_initialized)
        return E_FAIL;

    m_targetCLR = hmodTargetCLR;
    return S_OK;
}

//-----------------------------------------------------------
// ICorDebug
//-----------------------------------------------------------

// Set the handler for callbacks on managed events
// This can not be NULL.
// If we're debugging V2.0 apps, pCallback must implement ICDManagedCallback2
// @todo- what if somebody calls this after we've already initialized? (eg, changes
// the callback underneath us)
HRESULT Cordb::SetManagedHandler(ICorDebugManagedCallback *pCallback)
{
    if (!m_initialized)
        return E_FAIL;

    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pCallback, ICorDebugManagedCallback*);

    m_managedCallback.Clear();
    m_managedCallback2.Clear();
    m_managedCallback3.Clear();
    m_managedCallback4.Clear();

    // For SxS, V2.0 debuggers must implement ManagedCallback2 to handle v2.0 debug events.
    // For Single-CLR, A v1.0 debugger may actually geta V2.0 debuggee.
    pCallback->QueryInterface(IID_ICorDebugManagedCallback2, (void **)&m_managedCallback2);
    if (m_managedCallback2 == NULL)
    {
        if (GetDebuggerVersion() >= CorDebugVersion_2_0)
        {
            // This will leave our internal callbacks null, which future operations (Create/Attach) will
            // use to know that we're not sufficiently initialized.
            return E_NOINTERFACE;
        }
        else
        {
            // This should only be used in a single-CLR shimming scenario.
            m_managedCallback2.Assign(new (nothrow) DefaultManagedCallback2(this));

            if (m_managedCallback2 == NULL)
            {
                return E_OUTOFMEMORY;
            }
        }
    }

    pCallback->QueryInterface(IID_ICorDebugManagedCallback3, (void **)&m_managedCallback3);
    if (m_managedCallback3 == NULL)
    {
        m_managedCallback3.Assign(new (nothrow) DefaultManagedCallback3(this));
    }

    if (m_managedCallback3 == NULL)
    {
        return E_OUTOFMEMORY;
    }

    pCallback->QueryInterface(IID_ICorDebugManagedCallback4, (void **)&m_managedCallback4);
    if (m_managedCallback4 == NULL)
    {
        m_managedCallback4.Assign(new (nothrow) DefaultManagedCallback4(this));
    }

    if (m_managedCallback4 == NULL)
    {
        return E_OUTOFMEMORY;
    }

    m_managedCallback.Assign(pCallback);
    return S_OK;
}

HRESULT Cordb::SetUnmanagedHandler(ICorDebugUnmanagedCallback *pCallback)
{
    if (!m_initialized)
        return E_FAIL;

    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pCallback, ICorDebugUnmanagedCallback*);

    m_unmanagedCallback.Assign(pCallback);

    return S_OK;
}

// CreateProcess() isn't supported on Windows CoreCLR.
// It is currently supported on Mac CoreCLR, but that may change.
bool Cordb::IsCreateProcessSupported()
{
#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    return false;
#else
    return true;
#endif
}

// Given everything we know about our configuration, can we support interop-debugging
bool Cordb::IsInteropDebuggingSupported()
{
    // We explicitly refrain from checking the unmanaged callback. See comment in
    // ICorDebug::SetUnmanagedHandler for details.
#ifdef FEATURE_INTEROP_DEBUGGING
    return true;
#else
    return false;
#endif
}


//---------------------------------------------------------------------------------------
//
// Implementation of ICorDebug::CreateProcess.
// Creates a process.
//
// Arguments:
//    The following arguments are passed thru unmodified to the OS CreateProcess API and
//       are defined by that API.
//           lpApplicationName
//           lpCommandLine
//           lpProcessAttributes
//           lpThreadAttributes
//           bInheritHandles
//           dwCreationFlags
//           lpCurrentDirectory
//           lpStartupInfo
//           lpProcessInformation
//           debuggingFlags
//
//    ppProcess - Space to fill in for the resulting process, returned as a valid pointer
//      on any success HRESULT.
//
// Return Value:
//    Normal HRESULT semantics.
//
//---------------------------------------------------------------------------------------
HRESULT Cordb::CreateProcess(LPCWSTR lpApplicationName,
                             _In_z_ LPWSTR lpCommandLine,
                             LPSECURITY_ATTRIBUTES lpProcessAttributes,
                             LPSECURITY_ATTRIBUTES lpThreadAttributes,
                             BOOL bInheritHandles,
                             DWORD dwCreationFlags,
                             PVOID lpEnvironment,
                             LPCWSTR lpCurrentDirectory,
                             LPSTARTUPINFOW lpStartupInfo,
                             LPPROCESS_INFORMATION lpProcessInformation,
                             CorDebugCreateProcessFlags debuggingFlags,
                             ICorDebugProcess **ppProcess)
{
    return CreateProcessCommon(NULL,
                               lpApplicationName,
                               lpCommandLine,
                               lpProcessAttributes,
                               lpThreadAttributes,
                               bInheritHandles,
                               dwCreationFlags,
                               lpEnvironment,
                               lpCurrentDirectory,
                               lpStartupInfo,
                               lpProcessInformation,
                               debuggingFlags,
                               ppProcess);
}

HRESULT Cordb::CreateProcessCommon(ICorDebugRemoteTarget * pRemoteTarget,
                                   LPCWSTR lpApplicationName,
                                   _In_z_ LPWSTR lpCommandLine,
                                   LPSECURITY_ATTRIBUTES lpProcessAttributes,
                                   LPSECURITY_ATTRIBUTES lpThreadAttributes,
                                   BOOL bInheritHandles,
                                   DWORD dwCreationFlags,
                                   PVOID lpEnvironment,
                                   LPCWSTR lpCurrentDirectory,
                                   LPSTARTUPINFOW lpStartupInfo,
                                   LPPROCESS_INFORMATION lpProcessInformation,
                                   CorDebugCreateProcessFlags debuggingFlags,
                                   ICorDebugProcess ** ppProcess)
{
    // If you hit this assert, it means that you are attempting to create a process without specifying the version
    // number.
    _ASSERTE(CorDebugInvalidVersion != m_debuggerSpecifiedVersion);

    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess**);

    HRESULT hr = S_OK;

    EX_TRY
    {
        if (!m_initialized)
        {
            ThrowHR(E_FAIL);
        }

        // Check that we support the debugger version
        CheckCompatibility();

    #ifdef FEATURE_INTEROP_DEBUGGING
        // DEBUG_PROCESS (=0x1) means debug this process & all future children.
        // DEBUG_ONLY_THIS_PROCESS =(0x2) means just debug the immediate process.
        // If we want to support DEBUG_PROCESS, then we need to have the RS sniff for new CREATE_PROCESS
        // events and spawn new CordbProcess for them.
        switch(dwCreationFlags & (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS))
        {
            // 1) managed-only debugging
            case 0:
                break;

            // 2) failure - returns E_NOTIMPL. (as this would involve debugging all of our children processes).
            case DEBUG_PROCESS:
                ThrowHR(E_NOTIMPL);

            // 3) Interop-debugging.
            // Note that MSDN (at least as of Jan 2003) is wrong about this flag. MSDN claims
            // DEBUG_ONLY_THIS_PROCESS w/o DEBUG_PROCESS should be ignored.
            // But it really should do launch as a debuggee (but not auto-attach to child processes).
            case DEBUG_ONLY_THIS_PROCESS:
                // Emprically, this is the common case for native / interop-debugging.
                break;

            // 4) Interop.
            // The spec for ICorDebug::CreateProcess says this is the one to use for interop-debugging.
            case DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS:
                // Win2k does not honor these flags properly. So we just use
                // It treats (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS) as if it were DEBUG_PROCESS.
                // We'll just always touch up the flags, even though WinXP and above is fine here.
                // Per win2k issue, strip off DEBUG_PROCESS, so that we're just left w/ DEBUG_ONLY_THIS_PROCESS.
                dwCreationFlags &= ~(DEBUG_PROCESS);
                break;

            default:
                __assume(0);
        }

    #endif // FEATURE_INTEROP_DEBUGGING

        // Must have a managed-callback by now.
        if ((m_managedCallback == NULL) || (m_managedCallback2 == NULL) || (m_managedCallback3 == NULL) || (m_managedCallback4 == NULL))
        {
            ThrowHR(E_FAIL);
        }

        if (!IsCreateProcessSupported())
        {
            ThrowHR(E_NOTIMPL);
        }

        if (!IsInteropDebuggingSupported() &&
            ((dwCreationFlags & (DEBUG_PROCESS | DEBUG_ONLY_THIS_PROCESS)) != 0))
        {
            ThrowHR(CORDBG_E_INTEROP_NOT_SUPPORTED);
        }

        // Check that we can even accept another debuggee before trying anything.
        EnsureAllowAnotherProcess();

    } EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
    {
        return hr;
    }

    hr = ShimProcess::CreateProcess(this,
                                    pRemoteTarget,
                                    lpApplicationName,
                                    lpCommandLine,
                                    lpProcessAttributes,
                                    lpThreadAttributes,
                                    bInheritHandles,
                                    dwCreationFlags,
                                    lpEnvironment,
                                    lpCurrentDirectory,
                                    lpStartupInfo,
                                    lpProcessInformation,
                                    debuggingFlags
                                   );

    LOG((LF_CORDB, LL_EVERYTHING, "Handle in Cordb::CreateProcess is: %.I64x\n", lpProcessInformation->hProcess));

    if (SUCCEEDED(hr))
    {
        LockProcessList();

        CordbProcess * pProcess = GetProcessList()->GetBase(lpProcessInformation->dwProcessId);

        UnlockProcessList();

        PREFIX_ASSUME(pProcess != NULL);

        pProcess->ExternalAddRef();
        *ppProcess = (ICorDebugProcess *)pProcess;
    }

    return hr;
}


HRESULT Cordb::CreateProcessEx(ICorDebugRemoteTarget * pRemoteTarget,
                               LPCWSTR lpApplicationName,
                               _In_z_ LPWSTR lpCommandLine,
                               LPSECURITY_ATTRIBUTES lpProcessAttributes,
                               LPSECURITY_ATTRIBUTES lpThreadAttributes,
                               BOOL bInheritHandles,
                               DWORD dwCreationFlags,
                               PVOID lpEnvironment,
                               LPCWSTR lpCurrentDirectory,
                               LPSTARTUPINFOW lpStartupInfo,
                               LPPROCESS_INFORMATION lpProcessInformation,
                               CorDebugCreateProcessFlags debuggingFlags,
                               ICorDebugProcess ** ppProcess)
{
    if (pRemoteTarget == NULL)
    {
        return E_INVALIDARG;
    }

    return CreateProcessCommon(pRemoteTarget,
                               lpApplicationName,
                               lpCommandLine,
                               lpProcessAttributes,
                               lpThreadAttributes,
                               bInheritHandles,
                               dwCreationFlags,
                               lpEnvironment,
                               lpCurrentDirectory,
                               lpStartupInfo,
                               lpProcessInformation,
                               debuggingFlags,
                               ppProcess);
}


//---------------------------------------------------------------------------------------
//
// Attachs to an existing process.
//
// Arguments:
//    dwProcessID - The PID to attach to
//    fWin32Attach - Flag to tell whether to attach as the Win32 debugger or not.
//    ppProcess - Space to fill in for the resulting process, returned as a valid pointer
//      on any success HRESULT.
//
// Return Value:
//    Normal HRESULT semantics.
//
//---------------------------------------------------------------------------------------
HRESULT Cordb::DebugActiveProcess(DWORD dwProcessId,
                                  BOOL fWin32Attach,
                                  ICorDebugProcess **ppProcess)
{
    return DebugActiveProcessCommon(NULL, dwProcessId, fWin32Attach, ppProcess);
}

HRESULT Cordb::DebugActiveProcessCommon(ICorDebugRemoteTarget * pRemoteTarget,
                                        DWORD dwProcessId,
                                        BOOL fWin32Attach,
                                        ICorDebugProcess ** ppProcess)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess **);

    HRESULT hr = S_OK;

    EX_TRY
    {
        if (!m_initialized)
        {
            ThrowHR(E_FAIL);
        }

        // Must have a managed-callback by now.
        if ((m_managedCallback == NULL) || (m_managedCallback2 == NULL) || (m_managedCallback3 == NULL) || (m_managedCallback4 == NULL))
        {
            ThrowHR(E_FAIL);
        }

        // Verify that given process ID, matches the process ID for which the object was created
        if (m_pd.IsInitialized() && m_pd.m_Pid != dwProcessId)
        {
            ThrowHR(E_INVALIDARG);
        }

        // See the comment in Cordb::CreateProcess
        _ASSERTE(CorDebugInvalidVersion != m_debuggerSpecifiedVersion);

        // Check that we support the debugger version
        CheckCompatibility();

        // Check that we can even accept another debuggee before trying anything.
        EnsureAllowAnotherProcess();

        // Check if we're allowed to do interop.
        bool fAllowInterop = IsInteropDebuggingSupported();

        if (!fAllowInterop && fWin32Attach)
        {
            ThrowHR(CORDBG_E_INTEROP_NOT_SUPPORTED);
        }

    } EX_CATCH_HRESULT(hr)
    if (FAILED(hr))
    {
        return hr;
    }

    hr = ShimProcess::DebugActiveProcess(
        this,
        pRemoteTarget,
        &m_pd,
        fWin32Attach == TRUE);

    // If that worked, then there will be a process object...
    if (SUCCEEDED(hr))
    {
        LockProcessList();
        CordbProcess * pProcess = GetProcessList()->GetBase(dwProcessId);

        if (pProcess != NULL)
        {
            // Add a reference now so process won't go away
            pProcess->ExternalAddRef();
        }
        UnlockProcessList();

        if (pProcess == NULL)
        {
            // This can happen if we add the process into process hash in
            // SendDebugActiveProcessEvent and then process exit
            // before we attemp to retrieve it again from GetBase.
            //
            *ppProcess = NULL;
            return S_FALSE;
        }

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
        // This is where we queue the managed attach event in Whidbey.  In the new architecture, the Windows
        // pipeline gets a loader breakpoint when native attach is completed, and that's where we queue the
        // managed attach event.  See how we handle the loader breakpoint in code:ShimProcess::DefaultEventHandler.
        // However, the Mac debugging transport gets no such breakpoint, and so we need to do this here.
        //
        // @dbgtodo  Mac - Ideally we should hide this in our pipeline implementation, or at least move
        // this to the shim.
        _ASSERTE(!fWin32Attach);
        {
            pProcess->Lock();
            hr = pProcess->QueueManagedAttach();
            pProcess->Unlock();
        }
#endif // FEATURE_DBGIPC_TRANSPORT_DI

        *ppProcess = (ICorDebugProcess*) pProcess;
    }

    return hr;
}

// Make sure we want to support the debugger that's using us
void Cordb::CheckCompatibility()
{
    // Get the debugger version specified by the startup APIs and convert it to a CLR major version number
    CorDebugInterfaceVersion debuggerVersion = GetDebuggerVersion();
    DWORD clrMajor;
    if (debuggerVersion <= CorDebugVersion_1_0 || debuggerVersion == CorDebugVersion_1_1)
        clrMajor = 1;
    else if (debuggerVersion <= CorDebugVersion_2_0)
        clrMajor = 2;
    else if (debuggerVersion <= CorDebugVersion_4_0)
        clrMajor = 4;
    else
        clrMajor = 5;   // some unrecognized future version

    if(!CordbProcess::IsCompatibleWith(clrMajor))
    {
        // Carefully choose our error-code to get an appropriate error-message from VS 2008
        // If GetDebuggerVersion is >= 4, we could consider using the more-appropriate (but not
        // added until V4) HRESULT CORDBG_E_UNSUPPORTED_FORWARD_COMPAT that is used by
        // OpenVirtualProcess, but it's probably simpler to keep ICorDebug APIs returning
        // consistent error codes.
        ThrowHR(CORDBG_E_INCOMPATIBLE_PROTOCOL);
    }
}

HRESULT Cordb::DebugActiveProcessEx(ICorDebugRemoteTarget * pRemoteTarget,
                                    DWORD dwProcessId,
                                    BOOL fWin32Attach,
                                    ICorDebugProcess ** ppProcess)
{
    if (pRemoteTarget == NULL)
    {
        return E_INVALIDARG;
    }

    return DebugActiveProcessCommon(pRemoteTarget, dwProcessId, fWin32Attach, ppProcess);
}


HRESULT Cordb::GetProcess(DWORD dwProcessId, ICorDebugProcess **ppProcess)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess**);

    if (!m_initialized)
    {
        return E_FAIL;
    }

    LockProcessList();
    CordbProcess *p = GetProcessList()->GetBase(dwProcessId);
    UnlockProcessList();

    if (p == NULL)
        return E_INVALIDARG;

    p->ExternalAddRef();
    *ppProcess = static_cast<ICorDebugProcess*> (p);

    return S_OK;
}

HRESULT Cordb::EnumerateProcesses(ICorDebugProcessEnum **ppProcesses)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcesses, ICorDebugProcessEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (!m_initialized)
        {
            ThrowHR(E_FAIL);
        }

        // Locking here just means that the enumerator gets initialized against a consistent
        // process-list. If we add/remove processes w/ an outstanding enumerator, things
        // could still get out of sync.
        RSLockHolder lockHolder(&this->m_processListMutex);

        RSInitHolder<CordbHashTableEnum> pEnum;
        CordbHashTableEnum::BuildOrThrow(
            this,
            &m_pProcessEnumList,
            GetProcessList(),
            IID_ICorDebugProcessEnum,
            pEnum.GetAddr());


        pEnum.TransferOwnershipExternal(ppProcesses);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


//
// Note: the following defs and structs are copied from various NT headers. I wasn't able to include those headers (like
// ntexapi.h) due to loads of redef problems and other conflicts with headers that we already pull in.
//
typedef LONG NTSTATUS;

#ifndef TARGET_UNIX
typedef BOOL (*NTQUERYSYSTEMINFORMATION)(SYSTEM_INFORMATION_CLASS SystemInformationClass,
                                         PVOID SystemInformation,
                                         ULONG SystemInformationLength,
                                         PULONG ReturnLength);
#endif

// Implementation of ICorDebug::CanLaunchOrAttach
// @dbgtodo-  this all goes away in V3.
// @dbgtodo-  this should go away in Dev11.
HRESULT Cordb::CanLaunchOrAttach(DWORD dwProcessId, BOOL fWin32DebuggingEnabled)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    EX_TRY
    {
        EnsureCanLaunchOrAttach(fWin32DebuggingEnabled);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Throw an expcetion if we can't launch/attach.
//
// Arguments:
//    fWin32DebuggingEnabled - true if interop-debugging, else false
//
// Return Value:
//    None. If this returns, then it's safe to launch/attach.
//    Else this throws an exception on failure.
//
// Assumptions:
//
// Notes:
//    It should always be safe to launch/attach except in exceptional cases.
//    @dbgtodo-  this all goes away in V3.
//    @dbgtodo-  this should go away in Dev11.
//
void Cordb::EnsureCanLaunchOrAttach(BOOL fWin32DebuggingEnabled)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;
    if (!m_initialized)
    {
        ThrowHR(E_FAIL);
    }

    EnsureAllowAnotherProcess();

    if (!IsInteropDebuggingSupported() && fWin32DebuggingEnabled)
    {
        ThrowHR(CORDBG_E_INTEROP_NOT_SUPPORTED);
    }

    // Made it this far, we succeeded.
}

HRESULT Cordb::CreateObjectV1(REFIID id, void **object)
{
    return CreateObject(CorDebugVersion_1_0, ProcessDescriptor::UNINITIALIZED_PID, NULL, NULL, id, object);
}

#if defined(FEATURE_DBGIPC_TRANSPORT_DI)
// CoreCLR activates debugger objects via direct COM rather than the shim (just like V1). For now we share the
// same debug engine version as V2, though this may change in the future.
HRESULT Cordb::CreateObjectTelesto(REFIID id, void ** pObject)
{
    return CreateObject(CorDebugVersion_2_0, ProcessDescriptor::UNINITIALIZED_PID, NULL, NULL, id, pObject);
}
#endif // FEATURE_DBGIPC_TRANSPORT_DI

// Static
// Used to create an instance for a ClassFactory (thus an external ref).
HRESULT Cordb::CreateObject(CorDebugInterfaceVersion iDebuggerVersion, DWORD pid, LPCWSTR lpApplicationGroupId, LPCWSTR dacModulePath, REFIID id, void **object)
{
    if (id != IID_IUnknown && id != IID_ICorDebug)
        return (E_NOINTERFACE);

    LPSTR applicationGroupId = NULL;
    if (lpApplicationGroupId != NULL)
    {
        // Get length of target string
        int cbMultiByte = WideCharToMultiByte(CP_ACP, 0, lpApplicationGroupId, -1, NULL, 0, NULL, NULL);
        if (cbMultiByte == 0)
        {
            return E_FAIL;
        }

        applicationGroupId = new (nothrow) CHAR[cbMultiByte];
        if (applicationGroupId == NULL)
        {
            return (E_OUTOFMEMORY);
        }

        /* Convert to ASCII */
        cbMultiByte = WideCharToMultiByte(CP_ACP, 0, lpApplicationGroupId, -1, applicationGroupId, cbMultiByte, NULL, NULL);
        if (cbMultiByte == 0)
        {
            delete [] applicationGroupId;
            return E_FAIL;
        }
    }

    ProcessDescriptor pd = ProcessDescriptor::Create(pid, applicationGroupId);

    Cordb *db = new (nothrow) Cordb(iDebuggerVersion, pd, dacModulePath);

    if (db == NULL)
    {
        if (applicationGroupId != NULL)
            delete [] applicationGroupId;

        return (E_OUTOFMEMORY);
    }

    *object = static_cast<ICorDebug*> (db);
    db->ExternalAddRef();

    return (S_OK);
}


// This is the version of the ICorDebug APIs that the debugger believes it's consuming.
// If this is a different version than that of the debuggee, we have the option of shimming
// behavior.
CorDebugInterfaceVersion
Cordb::GetDebuggerVersion() const
{
    return m_debuggerSpecifiedVersion;
}

//***********************************************************************
//              ICorDebugTMEnum (Thread and Module enumerator)
//***********************************************************************
CordbEnumFilter::CordbEnumFilter(CordbBase * pOwnerObj, NeuterList * pOwnerList)
    : CordbBase (pOwnerObj->GetProcess(), 0),
    m_pOwnerObj(pOwnerObj),
    m_pOwnerNeuterList(pOwnerList),
    m_pFirst (NULL),
    m_pCurrent (NULL),
    m_iCount (0)
{
    _ASSERTE(m_pOwnerNeuterList != NULL);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_pOwnerNeuterList->Add(pOwnerObj->GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);

}

CordbEnumFilter::CordbEnumFilter(CordbEnumFilter *src)
    : CordbBase (src->GetProcess(), 0),
    m_pOwnerObj(src->m_pOwnerObj),
    m_pOwnerNeuterList(src->m_pOwnerNeuterList),
    m_pFirst (NULL),
    m_pCurrent (NULL)
{
    _ASSERTE(m_pOwnerNeuterList != NULL);

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_pOwnerNeuterList->Add(src->GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);



    int iCountSanityCheck = 0;
    EnumElement *pElementCur = NULL;
    EnumElement *pElementNew = NULL;
    EnumElement *pElementNewPrev = NULL;

    m_iCount = src->m_iCount;

    pElementCur = src->m_pFirst;

    while (pElementCur != NULL)
    {
        pElementNew = new (nothrow) EnumElement;
        if (pElementNew == NULL)
        {
            // Out of memory. Clean up and bail out.
            goto Error;
        }

        if (pElementNewPrev == NULL)
        {
            m_pFirst = pElementNew;
        }
        else
        {
            pElementNewPrev->SetNext(pElementNew);
        }

        pElementNewPrev = pElementNew;

        // Copy the element, including the AddRef part
        pElementNew->SetData(pElementCur->GetData());
        IUnknown *iu = (IUnknown *)pElementCur->GetData();
        iu->AddRef();

        if (pElementCur == src->m_pCurrent)
            m_pCurrent = pElementNew;

        pElementCur = pElementCur->GetNext();
        iCountSanityCheck++;
    }

    _ASSERTE(iCountSanityCheck == m_iCount);

    return;
Error:
    // release all the allocated memory before returning
    pElementCur = m_pFirst;

    while (pElementCur != NULL)
    {
        pElementNewPrev = pElementCur;
        pElementCur = pElementCur->GetNext();

        ((ICorDebugModule *)pElementNewPrev->GetData())->Release();
        delete pElementNewPrev;
    }
}

CordbEnumFilter::~CordbEnumFilter()
{
    _ASSERTE(this->IsNeutered());

    _ASSERTE(m_pFirst == NULL);
}

void CordbEnumFilter::Neuter()
{
    EnumElement *pElement = m_pFirst;
    EnumElement *pPrevious = NULL;

    while (pElement != NULL)
    {
        pPrevious = pElement;
        pElement = pElement->GetNext();
        delete pPrevious;
    }

    // Null out the head in case we get neutered again.
    m_pFirst = NULL;
    m_pCurrent = NULL;

    CordbBase::Neuter();
}



HRESULT CordbEnumFilter::QueryInterface(REFIID id, void **ppInterface)
{
    // if we QI with the IID of the base type, we can't just return a pointer ICorDebugEnum directly, because
    // the cast is ambiguous. This happens because CordbEnumFilter implements both ICorDebugModuleEnum and
    // ICorDebugThreadEnum, both of which derive in turn from ICorDebugEnum. This produces a diamond inheritance
    // graph. Thus we need a double cast. It doesn't really matter whether we pick ICorDebugThreadEnum or
    // ICorDebugModuleEnum, because it will be backed by the same object regardless.
    if (id == IID_ICorDebugEnum)
        *ppInterface = static_cast<ICorDebugEnum *>(static_cast<ICorDebugThreadEnum *>(this));
    else if (id == IID_ICorDebugModuleEnum)
        *ppInterface = (ICorDebugModuleEnum*)this;
    else if (id == IID_ICorDebugThreadEnum)
        *ppInterface = (ICorDebugThreadEnum*)this;
    else if (id == IID_IUnknown)
        *ppInterface = this;
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbEnumFilter::Skip(ULONG celt)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        while ((celt-- > 0) && (m_pCurrent != NULL))
        {
            m_pCurrent = m_pCurrent->GetNext();
        }
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::Reset()
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        m_pCurrent = m_pFirst;
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::Clone(ICorDebugEnum **ppEnum)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppEnum);

        CordbEnumFilter * pClone = new CordbEnumFilter(this);

        // Ambiguous conversion from CordbEnumFilter to ICorDebugEnum, so
        // we explicitly convert it through ICorDebugThreadEnum.
        pClone->ExternalAddRef();
        (*ppEnum) = static_cast<ICorDebugThreadEnum *> (pClone);
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::GetCount(ULONG *pcelt)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(pcelt);
        *pcelt = (ULONG)m_iCount;
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::Next(ULONG celt,
                ICorDebugModule *objects[],
                ULONG *pceltFetched)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        hr = NextWorker(celt, objects, pceltFetched);
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::NextWorker(ULONG celt, ICorDebugModule *objects[], ULONG *pceltFetched)
{
    // <TODO>
    //
    // nickbe 11/20/2002 10:43:39
    // This function allows you to enumerate threads that "belong" to a
    // particular AppDomain. While this operation makes some sense, it makes
    // very little sense to
    //  (a) enumerate the list of threads in the enter process
    //  (b) build up a hand-rolled singly linked list (grrr)
    // </TODO>
    VALIDATE_POINTER_TO_OBJECT_ARRAY(objects, ICorDebugModule *,
        celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
            *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT hr = S_OK;

    ULONG count = 0;

    while ((m_pCurrent != NULL) && (count < celt))
    {
        objects[count] = (ICorDebugModule *)m_pCurrent->GetData();
        m_pCurrent = m_pCurrent->GetNext();
        count++;
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (count < celt)
    {
        return S_FALSE;
    }

    return hr;
}


HRESULT CordbEnumFilter::Next(ULONG celt,
                ICorDebugThread *objects[],
                ULONG *pceltFetched)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        hr = NextWorker(celt, objects, pceltFetched);
    }
    PUBLIC_API_END(hr);
    return hr;
}

HRESULT CordbEnumFilter::NextWorker(ULONG celt, ICorDebugThread *objects[], ULONG *pceltFetched)
{
    // @TODO remove this class
    VALIDATE_POINTER_TO_OBJECT_ARRAY(objects, ICorDebugThread *, celt, true, true);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pceltFetched, ULONG *);

    if ((pceltFetched == NULL) && (celt != 1))
    {
        return E_INVALIDARG;
    }

    if (celt == 0)
    {
        if (pceltFetched != NULL)
        {
            *pceltFetched = 0;
        }
        return S_OK;
    }

    HRESULT hr = S_OK;

    ULONG count = 0;

    while ((m_pCurrent != NULL) && (count < celt))
    {
        objects[count] = (ICorDebugThread *)m_pCurrent->GetData();
        m_pCurrent = m_pCurrent->GetNext();
        count++;
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (count < celt)
    {
        return S_FALSE;
    }

    return hr;
}



HRESULT CordbEnumFilter::Init (ICorDebugModuleEnum * pModEnum, CordbAssembly *pAssembly)
{
    INTERNAL_API_ENTRY(GetProcess());

    ICorDebugModule *pCorModule = NULL;
    CordbModule *pModule = NULL;
    ULONG ulDummy = 0;

    HRESULT hr = pModEnum->Next(1, &pCorModule, &ulDummy);

    //
    // Next returns E_FAIL if there is no next item, along with
    // the count being 0.  Convert that to just being S_OK.
    //
    if ((hr == E_FAIL) && (ulDummy == 0))
    {
        hr = S_OK;
    }

    if (FAILED (hr))
        return hr;

    EnumElement *pPrevious = NULL;
    EnumElement *pElement = NULL;

    while (ulDummy != 0)
    {
        pModule = (CordbModule *)(ICorDebugModule *)pCorModule;
        // Is this module part of the assembly for which we're enumerating?
        if (pModule->m_pAssembly == pAssembly)
        {
            pElement = new (nothrow) EnumElement;
            if (pElement == NULL)
            {
                // Out of memory. Clean up and bail out.
                hr = E_OUTOFMEMORY;
                goto Error;
            }

            pElement->SetData ((void *)pCorModule);
            m_iCount++;

            if (m_pFirst == NULL)
            {
                m_pFirst = pElement;
            }
            else
            {
                PREFIX_ASSUME(pPrevious != NULL);
                pPrevious->SetNext (pElement);
            }
            pPrevious = pElement;
        }
        else
            ((ICorDebugModule *)pModule)->Release();

        hr = pModEnum->Next(1, &pCorModule, &ulDummy);

        //
        // Next returns E_FAIL if there is no next item, along with
        // the count being 0.  Convert that to just being S_OK.
        //
        if ((hr == E_FAIL) && (ulDummy == 0))
        {
            hr = S_OK;
        }

        if (FAILED (hr))
            goto Error;
    }

    m_pCurrent = m_pFirst;

    return S_OK;

Error:
    // release all the allocated memory before returning
    pElement = m_pFirst;

    while (pElement != NULL)
    {
        pPrevious = pElement;
        pElement = pElement->GetNext();

        ((ICorDebugModule *)pPrevious->GetData())->Release();
        delete pPrevious;
    }

    return hr;
}

HRESULT CordbEnumFilter::Init (ICorDebugThreadEnum *pThreadEnum, CordbAppDomain *pAppDomain)
{
    INTERNAL_API_ENTRY(GetProcess());

    ICorDebugThread *pCorThread = NULL;
    CordbThread *pThread = NULL;
    ULONG ulDummy = 0;

    HRESULT hr = pThreadEnum->Next(1, &pCorThread, &ulDummy);

    //
    // Next returns E_FAIL if there is no next item, but we want to consider this
    // ok in this context.
    //
    if ((hr == E_FAIL) && (ulDummy == 0))
    {
        hr = S_OK;
    }

    if (FAILED(hr))
    {
        return hr;
    }

    EnumElement *pPrevious = NULL;
    EnumElement *pElement = NULL;

    while (ulDummy > 0)
    {
        pThread = (CordbThread *)(ICorDebugThread *) pCorThread;

        // Is this module part of the appdomain for which we're enumerating?
        // Note that this is rather inefficient (we call into the left side for every AppDomain),
        // but the whole idea of enumerating the threads of an AppDomain is pretty bad,
        // and we don't expect this to be used much if at all.
        CordbAppDomain* pThreadDomain;
        hr = pThread->GetCurrentAppDomain( &pThreadDomain );
        if( FAILED(hr) )
        {
            goto Error;
        }

        if (pThreadDomain == pAppDomain)
        {
            pElement = new (nothrow) EnumElement;
            if (pElement == NULL)
            {
                // Out of memory. Clean up and bail out.
                hr = E_OUTOFMEMORY;
                goto Error;
            }

            pElement->SetData ((void *)pCorThread);
            m_iCount++;

            if (m_pFirst == NULL)
            {
                m_pFirst = pElement;
            }
            else
            {
                PREFIX_ASSUME(pPrevious != NULL);
                pPrevious->SetNext (pElement);
            }

            pPrevious = pElement;
        }
        else
        {
            ((ICorDebugThread *)pThread)->Release();
        }

        //  get the next thread in the thread list
        hr = pThreadEnum->Next(1, &pCorThread, &ulDummy);

        //
        // Next returns E_FAIL if there is no next item, along with
        // the count being 0.  Convert that to just being S_OK.
        //
        if ((hr == E_FAIL) && (ulDummy == 0))
        {
            hr = S_OK;
        }

        if (FAILED (hr))
            goto Error;
    }

    m_pCurrent = m_pFirst;

    return S_OK;

Error:
    // release all the allocated memory before returning
    pElement = m_pFirst;

    while (pElement != NULL)
    {
        pPrevious = pElement;
        pElement = pElement->GetNext();

        ((ICorDebugThread *)pPrevious->GetData())->Release();
        delete pPrevious;
    }

    return hr;
}

