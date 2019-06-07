// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#include "stdafx.h"

#include "clrhost.h"
#include "utilcode.h"
#include "ex.h"
#include "hostimpl.h"
#include "clrnt.h"
#include "contract.h"

#if defined __llvm__
#  if defined(__has_feature) && __has_feature(address_sanitizer)
#    define HAS_ADDRESS_SANITIZER
#  endif
#endif

#ifdef _DEBUG_IMPL

//
// I'd very much like for this to go away. Its used to disable all THROWS contracts within whatever DLL this
// function is called from. That's obviously very, very bad, since there's no validation of those macros. But it
// can be difficult to remove this without actually fixing every violation at the same time.
//
// When this flag is finally removed, remove RealCLRThrowsExceptionWorker() too and put CONTRACT_THROWS() in place
// of it.
//
//
static BOOL dbg_fDisableThrowCheck = FALSE;

void DisableThrowCheck()
{
    LIMITED_METHOD_CONTRACT;

    dbg_fDisableThrowCheck = TRUE;
}

#ifdef HAS_ADDRESS_SANITIZER
// use the functionality from address santizier (which does not throw exceptions)
#else

#define CLRThrowsExceptionWorker() RealCLRThrowsExceptionWorker(__FUNCTION__, __FILE__, __LINE__)

static void RealCLRThrowsExceptionWorker(__in_z const char *szFunction,
                                         __in_z const char *szFile,
                                         int lineNum)
{
    WRAPPER_NO_CONTRACT;

    if (dbg_fDisableThrowCheck)
    {
        return;
    }

    CONTRACT_THROWSEX(szFunction, szFile, lineNum);
}

#endif // HAS_ADDRESS_SANITIZER
#endif //_DEBUG_IMPL

#if defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)

// Fls callback to deallocate ClrDebugState when our FLS block goes away.
void FreeClrDebugState(LPVOID pTlsData)
{
#ifdef _DEBUG
    ClrDebugState *pClrDebugState = (ClrDebugState*)pTlsData;

    // Make sure the ClrDebugState was initialized by a compatible version of
    // utilcode.lib. If it was initialized by an older version, we just let it leak.
    if (pClrDebugState && (pClrDebugState->ViolationMask() & CanFreeMe) && !(pClrDebugState->ViolationMask() & BadDebugState))
    {
#undef HeapFree
#undef GetProcessHeap
        
        // Since "!(pClrDebugState->m_violationmask & BadDebugState)", we know we have
        // a valid m_pLockData
        _ASSERTE(pClrDebugState->GetDbgStateLockData() != NULL);
        ::HeapFree (GetProcessHeap(), 0, pClrDebugState->GetDbgStateLockData());

        ::HeapFree (GetProcessHeap(), 0, pClrDebugState);
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
#define GetProcessHeap() Dont_Use_GetProcessHeap()
    }
#endif //_DEBUG
}

// This is a drastic shutoff toggle that forces all new threads to fail their CLRInitDebugState calls.
// We only invoke this if FLS can't allocate its master block, preventing us from tracking the shutoff
// on a per-thread basis.
BYTE* GetGlobalContractShutoffFlag()
{
#ifdef SELF_NO_HOST

    static BYTE gGlobalContractShutoffFlag = 0;
    return &gGlobalContractShutoffFlag;
#else //!SELF_NO_HOST
    HINSTANCE hmod = GetCLRModule();
    if (!hmod)
    {
        return NULL;
    }
    typedef BYTE*(__stdcall * PGETSHUTOFFADDRFUNC)();
    PGETSHUTOFFADDRFUNC pGetContractShutoffFlagFunc = (PGETSHUTOFFADDRFUNC)GetProcAddress(hmod, "GetAddrOfContractShutoffFlag");
    if (!pGetContractShutoffFlagFunc)
    {
        return NULL;
    }
    return pGetContractShutoffFlagFunc();
#endif //!SELF_NO_HOST
}

static BOOL AreContractsShutoff()
{
    BYTE *pShutoff = GetGlobalContractShutoffFlag();
    if (!pShutoff)
    {
        return FALSE;
    }
    else
    {
        return 0 != *pShutoff;
    }
}

static VOID ShutoffContracts()
{
    BYTE *pShutoff = GetGlobalContractShutoffFlag();
    if (pShutoff)
    {
        *pShutoff = 1;
    }
}

//=============================================================================================
// Used to initialize the per-thread ClrDebugState. This is called once per thread (with
// possible exceptions for OOM scenarios.)
//
// No matter what, this function will not return NULL. If it can't do its job because of OOM reasons,
// it will return a pointer to &gBadClrDebugState which effectively disables contracts for
// this thread.
//=============================================================================================
ClrDebugState *CLRInitDebugState()
{
    // workaround!
    //
    // The existing Fls apis didn't provide the support we need and adding support cleanly is
    // messy because of the brittleness of IExecutionEngine.
    //
    // To understand this function, you need to know that the Fls routines have special semantics
    // for the TlsIdx_ClrDebugState slot:
    //
    //  - FlsSetValue will never throw. If it fails due to OOM on creation of the slot storage,
    //    it will silently bail. Thus, we must do a confirming FlsGetValue before we can conclude
    //    that the SetValue succeeded.
    //
    //  - FlsAssociateCallback will not complain about multiple sets of the callback.
    //
    //  - The mscorwks implemention of FlsAssociateCallback will ignore the passed in value
    //    and use the version of FreeClrDebugState compiled into mscorwks. This is needed to
    //    avoid dangling pointer races on shutdown.


    // This is our global "bad" debug state that thread use when they OOM on CLRInitDebugState.
    // We really only need to initialize it once but initializing each time is convenient
    // and has low perf impact.
    static ClrDebugState gBadClrDebugState;
    gBadClrDebugState.ViolationMaskSet( AllViolation );
    gBadClrDebugState.SetOkToThrow();

    ClrDebugState *pNewClrDebugState = NULL;
    ClrDebugState *pClrDebugState    = NULL;
    DbgStateLockData    *pNewLockData      = NULL;

    // We call this first partly to force a CheckThreadState. We've hopefully chased out all the
    // recursive contract calls inside here but if we haven't, it's best to get them out of the way
    // early.
    ClrFlsAssociateCallback(TlsIdx_ClrDebugState, FreeClrDebugState);


    if (AreContractsShutoff())
    {
        pNewClrDebugState = NULL;
    }
    else
    {
        // Yuck. We cannot call the hosted allocator for ClrDebugState (it is impossible to maintain a guarantee
        // that none of code paths, many of them called conditionally, don't themselves trigger a ClrDebugState creation.)
        // We have to call the OS directly for this.
#undef HeapAlloc
#undef GetProcessHeap
        pNewClrDebugState = (ClrDebugState*)::HeapAlloc(GetProcessHeap(), 0, sizeof(ClrDebugState));
        if (pNewClrDebugState != NULL)
        {
            // Only allocate a DbgStateLockData if its owning ClrDebugState was successfully allocated
            pNewLockData  = (DbgStateLockData *)::HeapAlloc(GetProcessHeap(), 0, sizeof(DbgStateLockData));
        }
#define GetProcessHeap() Dont_Use_GetProcessHeap()
#define HeapAlloc(hHeap, dwFlags, dwBytes) Dont_Use_HeapAlloc(hHeap, dwFlags, dwBytes)

        if ((pNewClrDebugState != NULL) && (pNewLockData != NULL))
        {
            // Both allocations succeeded, so initialize the structures, and have
            // pNewClrDebugState point to pNewLockData.  If either of the allocations
            // failed, we'll use gBadClrDebugState for this thread, and free whichever of
            // pNewClrDebugState or pNewLockData actually did get allocated (if either did).
            // (See code in this function below, outside this block.)

            pNewClrDebugState->SetStartingValues();
            pNewClrDebugState->ViolationMaskSet( CanFreeMe );
            _ASSERTE(!(pNewClrDebugState->ViolationMask() & BadDebugState));

            pNewLockData->SetStartingValues();
            pNewClrDebugState->SetDbgStateLockData(pNewLockData);
        }
    }


    // This is getting really diseased. All the one-time host init stuff inside the ClrFlsStuff could actually
    // have caused mscorwks contracts to be executed since the last time we actually checked to see if the ClrDebugState
    // needed creating.
    //
    // So we must make one last check to see if the ClrDebugState still needs creating.
    //
    ClrDebugState *pTmp = (ClrDebugState*)(ClrFlsGetValue(TlsIdx_ClrDebugState));
    if (pTmp != NULL)
    {
        // Recursive call set up ClrDebugState for us
        pClrDebugState = pTmp;
    }
    else if ((pNewClrDebugState != NULL) && (pNewLockData != NULL))
    {
        // Normal case: our new ClrDebugState will be the one we just allocated.
        // Note that we require BOTH the ClrDebugState and the DbgStateLockData
        // structures to have been successfully allocated for contracts to be
        // enabled for this thread.
        _ASSERTE(!(pNewClrDebugState->ViolationMask() & BadDebugState));
        _ASSERTE(pNewClrDebugState->GetDbgStateLockData() == pNewLockData);
        pClrDebugState = pNewClrDebugState;
    }
    else
    {
        // OOM case: HeapAlloc of newClrDebugState failed.
        pClrDebugState = &gBadClrDebugState;
    }

    _ASSERTE(pClrDebugState != NULL);


    ClrFlsSetValue(TlsIdx_ClrDebugState, (LPVOID)pClrDebugState);

    // For the ClrDebugState index, ClrFlsSetValue does *not* throw on OOM.
    // Instead, it silently throws away the value. So we must now do a confirming
    // FlsGet to learn if our Set succeeded.
    if (ClrFlsGetValue(TlsIdx_ClrDebugState) == NULL)
    {
        // Our FlsSet didn't work. That means it couldn't allocate the master FLS block for our thread.
        // Now we're a bad state because not only can't we succeed, we can't record that we didn't succeed.
        // And it's invalid to return a BadClrDebugState here only to return a good debug state later.
        //
        // So we now take the drastic step of forcing all future ClrInitDebugState calls to return the OOM state.
        ShutoffContracts();
        pClrDebugState = &gBadClrDebugState;

        // Try once more time to set the FLS (if it doesn't work, the next call will keep cycling through here
        // until it does succeed.)
        ClrFlsSetValue(TlsIdx_ClrDebugState, &gBadClrDebugState);
    }


#if defined(_DEBUG)
    // The ClrDebugState we allocated above made it into FLS iff
    //      the DbgStateLockData we allocated above made it into
    //      the FLS's ClrDebugState::m_pLockData
    // These debug-only checks enforce this invariant

    if (pClrDebugState != NULL)
    {
        // If we're here, then typically pClrDebugState is what's in FLS.  However,
        // it's possible that pClrDebugState is gBadClrDebugState, and FLS is NULL
        // (if the last ClrFlsSetValue() failed).  Either way, our checks below
        // are valid ones to make.

        if (pClrDebugState == pNewClrDebugState)
        {
            // ClrDebugState we allocated above made it into FLS, so DbgStateLockData
            // must be there, too
            _ASSERTE(pNewLockData != NULL);
            _ASSERTE(pClrDebugState->GetDbgStateLockData() == pNewLockData);
        }
        else
        {
            // ClrDebugState we allocated above did NOT make it into FLS,
            // so the DbgStateLockData we allocated must not be there, either
            _ASSERTE(pClrDebugState->GetDbgStateLockData() == NULL || pClrDebugState->GetDbgStateLockData() != pNewLockData);
        }
    }

    // One more invariant:  Because of ordering & conditions around the HeapAllocs above,
    // we'll never have a DbgStateLockData without a ClrDebugState
    _ASSERTE((pNewLockData == NULL) || (pNewClrDebugState != NULL));

#endif //_DEBUG

#undef HeapFree
#undef GetProcessHeap
    if (pNewClrDebugState != NULL && pClrDebugState != pNewClrDebugState)
    {
        // We allocated a ClrDebugState which didn't make it into FLS, so free it.
        ::HeapFree (GetProcessHeap(), 0, pNewClrDebugState);
        if (pNewLockData != NULL)
        {
            // We also allocated a DbgStateLockData that didn't make it into FLS, so
            // free it, too.  (Remember, we asserted above that we can only have
            // this unused DbgStateLockData if we had an unused ClrDebugState
            // as well (which we just freed).)
            ::HeapFree (GetProcessHeap(), 0, pNewLockData);
        }
    }
#define HeapFree(hHeap, dwFlags, lpMem) Dont_Use_HeapFree(hHeap, dwFlags, lpMem)
#define GetProcessHeap() Dont_Use_GetProcessHeap()

    // Not necessary as TLS slots are born NULL and potentially problematic for OOM cases as we can't
    // take an exception here.
    //ClrFlsSetValue(TlsIdx_OwnedCrstsChain, NULL);

    return pClrDebugState;
} // CLRInitDebugState

#endif //defined(_DEBUG_IMPL) && defined(ENABLE_CONTRACTS_IMPL)

const NoThrow nothrow = { 0 };

#ifdef HAS_ADDRESS_SANITIZER
// use standard heap functions for address santizier
#else

void * __cdecl
operator new(size_t n)
{
#ifdef _DEBUG_IMPL
    CLRThrowsExceptionWorker();
#endif

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    void * result = ClrAllocInProcessHeap(0, S_SIZE_T(n));
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    TRASH_LASTERROR;
    return result;
}

void * __cdecl
operator new[](size_t n)
{
#ifdef _DEBUG_IMPL
    CLRThrowsExceptionWorker();
#endif

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    void * result = ClrAllocInProcessHeap(0, S_SIZE_T(n));
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    TRASH_LASTERROR;
    return result;
};

#endif // HAS_ADDRESS_SANITIZER

void * __cdecl operator new(size_t n, const NoThrow&) NOEXCEPT
{
#ifdef HAS_ADDRESS_SANITIZER
    // use standard heap functions for address santizier (which doesn't provide for NoThrow)
	void * result = operator new(n);
#else
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    void * result = ClrAllocInProcessHeap(0, S_SIZE_T(n));
#endif // HAS_ADDRESS_SANITIZER
	TRASH_LASTERROR;
    return result;
}

void * __cdecl operator new[](size_t n, const NoThrow&) NOEXCEPT
{
#ifdef HAS_ADDRESS_SANITIZER
    // use standard heap functions for address santizier (which doesn't provide for NoThrow)
	void * result = operator new[](n);
#else
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    void * result = ClrAllocInProcessHeap(0, S_SIZE_T(n));
#endif // HAS_ADDRESS_SANITIZER
	TRASH_LASTERROR;
    return result;
}

#ifdef HAS_ADDRESS_SANITIZER
// use standard heap functions for address santizier
#else
void __cdecl
operator delete(void *p) NOEXCEPT
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    if (p != NULL)
        ClrFreeInProcessHeap(0, p);
    TRASH_LASTERROR;
}

void __cdecl
operator delete[](void *p) NOEXCEPT
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY;

    if (p != NULL)
        ClrFreeInProcessHeap(0, p);
    TRASH_LASTERROR;
}

#endif // HAS_ADDRESS_SANITIZER


/* ------------------------------------------------------------------------ *
 * New operator overloading for the executable heap
 * ------------------------------------------------------------------------ */

#ifndef FEATURE_PAL 
 
const CExecutable executable = { 0 };

void * __cdecl operator new(size_t n, const CExecutable&)
{
#if defined(_DEBUG_IMPL)
    CLRThrowsExceptionWorker();
#endif

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    HANDLE hExecutableHeap = ClrGetProcessExecutableHeap();
    if (hExecutableHeap == NULL) {
        ThrowOutOfMemory();
    }

    void * result = ClrHeapAlloc(hExecutableHeap, 0, S_SIZE_T(n));
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    TRASH_LASTERROR;
    return result;
}

void * __cdecl operator new[](size_t n, const CExecutable&)
{
#if defined(_DEBUG_IMPL)
    CLRThrowsExceptionWorker();
#endif

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    HANDLE hExecutableHeap = ClrGetProcessExecutableHeap();
    if (hExecutableHeap == NULL) {
        ThrowOutOfMemory();
    }

    void * result = ClrHeapAlloc(hExecutableHeap, 0, S_SIZE_T(n));
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    TRASH_LASTERROR;
    return result;
}

void * __cdecl operator new(size_t n, const CExecutable&, const NoThrow&)
{    
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    HANDLE hExecutableHeap = ClrGetProcessExecutableHeap();
    if (hExecutableHeap == NULL)
        return NULL;

    void * result = ClrHeapAlloc(hExecutableHeap, 0, S_SIZE_T(n));
    TRASH_LASTERROR;
    return result;
}

void * __cdecl operator new[](size_t n, const CExecutable&, const NoThrow&)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FAULT;

    INCONTRACT(_ASSERTE(!ARE_FAULTS_FORBIDDEN()));

    HANDLE hExecutableHeap = ClrGetProcessExecutableHeap();
    if (hExecutableHeap == NULL)
        return NULL;

    void * result = ClrHeapAlloc(hExecutableHeap, 0, S_SIZE_T(n));
    TRASH_LASTERROR;
    return result;
}

#endif // FEATURE_PAL 

#ifdef _DEBUG

// This is a DEBUG routing to verify that a memory region complies with executable requirements
BOOL DbgIsExecutable(LPVOID lpMem, SIZE_T length)
{
#if defined(CROSSGEN_COMPILE) || defined(FEATURE_PAL)
    // No NX support on PAL or for crossgen compilations.
    return TRUE;
#else // !(CROSSGEN_COMPILE || FEATURE_PAL) 
    BYTE *regionStart = (BYTE*) ALIGN_DOWN((BYTE*)lpMem, GetOsPageSize());
    BYTE *regionEnd = (BYTE*) ALIGN_UP((BYTE*)lpMem+length, GetOsPageSize());
    _ASSERTE(length > 0);
    _ASSERTE(regionStart < regionEnd);

    while(regionStart < regionEnd)
    {
        MEMORY_BASIC_INFORMATION mbi;

        SIZE_T cbBytes = ClrVirtualQuery(regionStart, &mbi, sizeof(mbi));
        _ASSERTE(cbBytes);

        // The pages must have EXECUTE set
        if(!(mbi.Protect & (PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE | PAGE_EXECUTE_WRITECOPY)))
            return FALSE;

        _ASSERTE((BYTE*)mbi.BaseAddress + mbi.RegionSize > regionStart);
        regionStart = (BYTE*)mbi.BaseAddress + mbi.RegionSize;
    }

    return TRUE;
#endif // CROSSGEN_COMPILE || FEATURE_PAL
}

#endif //_DEBUG




// Access various ExecutionEngine support services, like a logical TLS that abstracts
// fiber vs. thread issues.  We obtain it from a DLL export via the shim.

typedef IExecutionEngine * (__stdcall * IEE_FPTR) ();

//
// Access various ExecutionEngine support services, like a logical TLS that abstracts
// fiber vs. thread issues.
// From an IExecutionEngine is possible to get other services via QueryInterfaces such
// as memory management
//
IExecutionEngine *g_pExecutionEngine = NULL;

#ifdef SELF_NO_HOST
BYTE g_ExecutionEngineInstance[sizeof(UtilExecutionEngine)];
#endif


IExecutionEngine *GetExecutionEngine()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SUPPORTS_DAC_HOST_ONLY;
       
    if (g_pExecutionEngine == NULL)
    {
        IExecutionEngine* pExecutionEngine;
#ifdef SELF_NO_HOST
        // Create a local copy on the stack and then copy it over to the static instance.
        // This avoids race conditions caused by multiple initializations of vtable in the constructor
        UtilExecutionEngine local;
        memcpy((void*)&g_ExecutionEngineInstance, (void*)&local, sizeof(UtilExecutionEngine));
        pExecutionEngine = (IExecutionEngine*)(UtilExecutionEngine*)&g_ExecutionEngineInstance;
#else
        // statically linked.
        VALIDATECORECLRCALLBACKS();
        pExecutionEngine = g_CoreClrCallbacks.m_pfnIEE();
#endif  // SELF_NO_HOST

        //We use an explicit memory barrier here so that the reference g_pExecutionEngine is valid when
        //it is used, This ia a requirement on platforms with weak memory model . We cannot use VolatileStore  
        //because they are the same as normal assignment for DAC builds [see code:VOLATILE]

        MemoryBarrier();
        g_pExecutionEngine = pExecutionEngine;
    }

    // It's a bug to ask for the ExecutionEngine interface in scenarios where the
    // ExecutionEngine cannot be loaded.
    _ASSERTE(g_pExecutionEngine);
    return g_pExecutionEngine;
} // GetExecutionEngine

IEEMemoryManager * GetEEMemoryManager()
{
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_CANNOT_TAKE_LOCK;
    SUPPORTS_DAC_HOST_ONLY;

    static IEEMemoryManager *pEEMemoryManager = NULL;
    if (NULL == pEEMemoryManager) {
        IExecutionEngine *pExecutionEngine = GetExecutionEngine();
        _ASSERTE(pExecutionEngine);

        // It is dangerous to pass a global pointer to QueryInterface.  The pointer may be set
        // to NULL in the call.  Imagine that thread 1 calls QI, and get a pointer.  But before thread 1
        // returns the pointer to caller, thread 2 calls QI and the pointer is set to NULL.
        IEEMemoryManager *pEEMM;
        pExecutionEngine->QueryInterface(IID_IEEMemoryManager, (void**)&pEEMM);
        pEEMemoryManager = pEEMM;
    }
    // It's a bug to ask for the MemoryManager interface in scenarios where it cannot be loaded.
    _ASSERTE(pEEMemoryManager);
    return pEEMemoryManager;
}

// should return some error code or exception
void SetExecutionEngine(IExecutionEngine *pExecutionEngine)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;

    _ASSERTE(pExecutionEngine && !g_pExecutionEngine);
    if (!g_pExecutionEngine) {
        g_pExecutionEngine = pExecutionEngine;
        g_pExecutionEngine->AddRef();
    }
}

void ClrFlsAssociateCallback(DWORD slot, PTLS_CALLBACK_FUNCTION callback)
{
    WRAPPER_NO_CONTRACT;

    GetExecutionEngine()->TLS_AssociateCallback(slot, callback);
}

LPVOID *ClrFlsGetBlockGeneric()
{
    WRAPPER_NO_CONTRACT;

    return (LPVOID *) GetExecutionEngine()->TLS_GetDataBlock();
}

CLRFLSGETBLOCK __ClrFlsGetBlock = ClrFlsGetBlockGeneric;

CRITSEC_COOKIE ClrCreateCriticalSection(CrstType crstType, CrstFlags flags)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->CreateLock(NULL, (LPCSTR)crstType, flags);
}

HRESULT ClrDeleteCriticalSection(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;
    GetExecutionEngine()->DestroyLock(cookie);
    return S_OK;
}

void ClrEnterCriticalSection(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->AcquireLock(cookie);
}

void ClrLeaveCriticalSection(CRITSEC_COOKIE cookie)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ReleaseLock(cookie);
}

EVENT_COOKIE ClrCreateAutoEvent(BOOL bInitialState)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->CreateAutoEvent(bInitialState);
}

EVENT_COOKIE ClrCreateManualEvent(BOOL bInitialState)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->CreateManualEvent(bInitialState);
}

void ClrCloseEvent(EVENT_COOKIE event)
{
    WRAPPER_NO_CONTRACT;

    GetExecutionEngine()->CloseEvent(event);
}

BOOL ClrSetEvent(EVENT_COOKIE event)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrSetEvent(event);
}

BOOL ClrResetEvent(EVENT_COOKIE event)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrResetEvent(event);
}

DWORD ClrWaitEvent(EVENT_COOKIE event, DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->WaitForEvent(event, dwMilliseconds, bAlertable);
}

SEMAPHORE_COOKIE ClrCreateSemaphore(DWORD dwInitial, DWORD dwMax)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrCreateSemaphore(dwInitial, dwMax);
}

void ClrCloseSemaphore(SEMAPHORE_COOKIE semaphore)
{
    WRAPPER_NO_CONTRACT;

    GetExecutionEngine()->ClrCloseSemaphore(semaphore);
}

BOOL ClrReleaseSemaphore(SEMAPHORE_COOKIE semaphore, LONG lReleaseCount, LONG *lpPreviousCount)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrReleaseSemaphore(semaphore, lReleaseCount, lpPreviousCount);
}

DWORD ClrWaitSemaphore(SEMAPHORE_COOKIE semaphore, DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrWaitForSemaphore(semaphore, dwMilliseconds, bAlertable);
}

MUTEX_COOKIE ClrCreateMutex(LPSECURITY_ATTRIBUTES lpMutexAttributes,
                            BOOL bInitialOwner,
                            LPCTSTR lpName)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrCreateMutex(lpMutexAttributes, bInitialOwner, lpName);
}

void ClrCloseMutex(MUTEX_COOKIE mutex)
{
    WRAPPER_NO_CONTRACT;

    GetExecutionEngine()->ClrCloseMutex(mutex);
}

BOOL ClrReleaseMutex(MUTEX_COOKIE mutex)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrReleaseMutex(mutex);
}

DWORD ClrWaitForMutex(MUTEX_COOKIE mutex, DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrWaitForMutex(mutex, dwMilliseconds, bAlertable);
}

DWORD ClrSleepEx(DWORD dwMilliseconds, BOOL bAlertable)
{
    WRAPPER_NO_CONTRACT;

    return GetExecutionEngine()->ClrSleepEx(dwMilliseconds, bAlertable);
}

LPVOID ClrVirtualAlloc(LPVOID lpAddress, SIZE_T dwSize, DWORD flAllocationType, DWORD flProtect)
{
    WRAPPER_NO_CONTRACT;

    LPVOID result =  GetEEMemoryManager()->ClrVirtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
    LOG((LF_EEMEM, LL_INFO100000, "ClrVirtualAlloc  (0x%p, 0x%06x, 0x%06x, 0x%02x) = 0x%p\n", lpAddress, dwSize, flAllocationType, flProtect, result));

    return result;
}

BOOL ClrVirtualFree(LPVOID lpAddress, SIZE_T dwSize, DWORD dwFreeType)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_EEMEM, LL_INFO100000, "ClrVirtualFree   (0x%p, 0x%06x, 0x%04x)\n", lpAddress, dwSize, dwFreeType));
    BOOL result = GetEEMemoryManager()->ClrVirtualFree(lpAddress, dwSize, dwFreeType);

    return result;
}

SIZE_T ClrVirtualQuery(LPCVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer, SIZE_T dwLength)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_EEMEM, LL_INFO100000, "ClrVirtualQuery  (0x%p)\n", lpAddress));
    return GetEEMemoryManager()->ClrVirtualQuery(lpAddress, lpBuffer, dwLength);
}

BOOL ClrVirtualProtect(LPVOID lpAddress, SIZE_T dwSize, DWORD flNewProtect, PDWORD lpflOldProtect)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_EEMEM, LL_INFO100000, "ClrVirtualProtect(0x%p, 0x%06x, 0x%02x)\n", lpAddress, dwSize, flNewProtect));
    return GetEEMemoryManager()->ClrVirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect);
}

HANDLE ClrGetProcessHeap()
{
    WRAPPER_NO_CONTRACT;

    return GetEEMemoryManager()->ClrGetProcessHeap();
}

HANDLE ClrHeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
    WRAPPER_NO_CONTRACT;

    return GetEEMemoryManager()->ClrHeapCreate(flOptions, dwInitialSize, dwMaximumSize);
}

BOOL ClrHeapDestroy(HANDLE hHeap)
{
    WRAPPER_NO_CONTRACT;

    return GetEEMemoryManager()->ClrHeapDestroy(hHeap);
}

LPVOID ClrHeapAlloc(HANDLE hHeap, DWORD dwFlags, S_SIZE_T dwBytes)
{
    WRAPPER_NO_CONTRACT;

    if(dwBytes.IsOverflow()) return NULL;

    LPVOID result = GetEEMemoryManager()->ClrHeapAlloc(hHeap, dwFlags, dwBytes.Value());

    return result;
}

BOOL ClrHeapFree(HANDLE hHeap, DWORD dwFlags, LPVOID lpMem)
{
    WRAPPER_NO_CONTRACT;

    BOOL result = GetEEMemoryManager()->ClrHeapFree(hHeap, dwFlags, lpMem);

    return result;
}

BOOL ClrHeapValidate(HANDLE hHeap, DWORD dwFlags, LPCVOID lpMem)
{
    WRAPPER_NO_CONTRACT;

    return GetEEMemoryManager()->ClrHeapValidate(hHeap, dwFlags, lpMem);
}

HANDLE ClrGetProcessExecutableHeap()
{
    WRAPPER_NO_CONTRACT;

    return GetEEMemoryManager()->ClrGetProcessExecutableHeap();
}

void GetLastThrownObjectExceptionFromThread(void **ppvException)
{
    WRAPPER_NO_CONTRACT;

    GetExecutionEngine()->GetLastThrownObjectExceptionFromThread(ppvException);
}
