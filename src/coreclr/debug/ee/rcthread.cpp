// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: RCThread.cpp
//

//
// Runtime Controller Thread
//
//*****************************************************************************

#include "stdafx.h"
#include "threadsuspend.h"
#ifndef TARGET_UNIX

#include "securitywrapper.h"
#endif
#include <aclapi.h>

#ifndef SM_REMOTESESSION
#define SM_REMOTESESSION 0x1000
#endif

#include <limits.h>

#ifdef _DEBUG
// Declare statics
EEThreadId DebuggerRCThread::s_DbgHelperThreadId;
#endif

//
// Constructor
//
DebuggerRCThread::DebuggerRCThread(Debugger * pDebugger)
    : m_debugger(pDebugger),
    m_pDCB(NULL),
    m_thread(NULL),
    m_run(true),
    m_threadControlEvent(NULL),
    m_helperThreadCanGoEvent(NULL),
    m_fDetachRightSide(false)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        GC_NOTRIGGER;
        CONSTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    _ASSERTE(pDebugger != NULL);

    for( int i = 0; i < IPC_TARGET_COUNT;i++)
    {
        m_rgfInitRuntimeOffsets[i] = true;
    }

    // Initialize this here because we Destroy it in the DTOR.
    // Note that this function can't fail.
}


//
// Destructor. Cleans up all of the open handles the RC thread uses.
// This expects that the RC thread has been stopped and has terminated
// before being called.
//
DebuggerRCThread::~DebuggerRCThread()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "DebuggerRCThread::~DebuggerRCThread\n"));

    // We explicitly leak the debugger object on shutdown. See Debugger::StopDebugger for details.
    _ASSERTE(!"RCThread dtor should not be called.");
}



//---------------------------------------------------------------------------------------
//
// Close the IPC events associated with a debugger connection
//
// Notes:
//    The only IPC connection supported is OOP.
//
//---------------------------------------------------------------------------------------
void DebuggerRCThread::CloseIPCHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if( m_pDCB != NULL)
    {
        m_pDCB->m_rightSideProcessHandle.Close();
    }
}

//-----------------------------------------------------------------------------
// Helper to get the proper decorated name
// Caller ensures that pBufSize is large enough. We'll assert just to check,
// but no runtime failure.
// pBuf - the output buffer to write the decorated name in
// cBufSizeInChars - the size of the buffer in characters, including the null.
// pPrefx - The undecorated name of the event.
//-----------------------------------------------------------------------------
void GetPidDecoratedName(_Out_writes_(cBufSizeInChars) WCHAR * pBuf,
                         int cBufSizeInChars,
                         const WCHAR * pPrefix)
{
    LIMITED_METHOD_CONTRACT;

    DWORD pid = GetCurrentProcessId();

    GetPidDecoratedName(pBuf, cBufSizeInChars, pPrefix, pid);
}




//-----------------------------------------------------------------------------
// Simple wrapper to create win32 events.
// This helps make DebuggerRCThread::Init pretty, beccause we
// create lots of events there.
// These will either:
// 1) Create/Open and return an event
// 2) or throw an exception.
// @todo - should these be CLREvents? ClrCreateManualEvent / ClrCreateAutoEvent
//-----------------------------------------------------------------------------
HANDLE CreateWin32EventOrThrow(
    LPSECURITY_ATTRIBUTES lpEventAttributes,
    EEventResetType eType,
    BOOL bInitialState
)
{
    CONTRACT(HANDLE)
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(lpEventAttributes, NULL_OK));
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    HANDLE h = NULL;
    h = WszCreateEvent(lpEventAttributes, (BOOL) eType, bInitialState, NULL);

    if (h == NULL)
        ThrowLastError();

    RETURN h;
}

//-----------------------------------------------------------------------------
// Open an event. Another helper for DebuggerRCThread::Init
//-----------------------------------------------------------------------------
HANDLE OpenWin32EventOrThrow(
    DWORD dwDesiredAccess,
    BOOL bInheritHandle,
    LPCWSTR lpName
)
{
    CONTRACT(HANDLE)
    {
        THROWS;
        GC_NOTRIGGER;
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    HANDLE h = WszOpenEvent(
        dwDesiredAccess,
        bInheritHandle,
        lpName
    );
    if (h == NULL)
        ThrowLastError();

    RETURN h;
}

//---------------------------------------------------------------------------------------
//
// Init
//
// Initialize the IPC block.
//
// Arguments:
//     hRsea - Handle to Right-Side Event Available event.
//     hRser - Handle to Right-Side Event Read event.
//     hLsea - Handle to Left-Side Event Available event.
//     hLser - Handle to Left-Side Event Read event.
//     hLsuwe - Handle to Left-Side unmanaged wait event.
//
// Notes:
//     The Init method works since there are no virtual functions - don't add any virtual functions without
//         changing this!
//     We assume ownership of the handles as soon as we're called; regardless of our success.
//     On failure, we throw.
//     Initialization of the debugger control block occurs partly on the left side and partly on
//     the right side. This initialization occurs in parallel, so it's unsafe to make assumptions about
//     the order in which the fields will be initialized.
//
//
//---------------------------------------------------------------------------------------
HRESULT DebuggerIPCControlBlock::Init(
    HANDLE hRsea,
    HANDLE hRser,
    HANDLE hLsea,
    HANDLE hLser,
    HANDLE hLsuwe
)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // NOTE this works since there are no virtual functions - don't add any without changing this!
    // Although we assume the IPC block is zero-initialized by the OS upon creation, we still need to clear
    // the memory here to protect ourselves from DOS attack.  One scenario is when a malicious debugger
    // pre-creates a bogus IPC block.  This means that our synchronization scheme won't work in DOS
    // attack scenarios, but we will be messed up anyway.
    // WARNING!!!  m_DCBSize is used as a semaphore and is set to non-zero to signal that initialization of the
    // WARNING!!!  DCB is complete.  if you remove the below memset be sure to initialize m_DCBSize to zero in the ctor!
    memset( this, 0, sizeof( DebuggerIPCControlBlock) );

    // Setup version checking info.
    m_verMajor = RuntimeFileBuildVersion;
    m_verMinor = RuntimeFileRevisionVersion;

#ifdef _DEBUG
    m_checkedBuild = true;
#else
    m_checkedBuild = false;
#endif
    m_bHostingInFiber = false;

    // Are we in fiber mode? In Whidbey, we do not support launch a fiber mode process
    // nor do we support attach to a fiber mode process.
    //
    if (g_CORDebuggerControlFlags & DBCF_FIBERMODE)
    {
        m_bHostingInFiber = true;
    }

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    // Copy RSEA and RSER into the control block.
    if (!m_rightSideEventAvailable.SetLocal(hRsea))
    {
        ThrowLastError();
    }

    if (!m_rightSideEventRead.SetLocal(hRser))
    {
        ThrowLastError();
    }

    if (!m_leftSideUnmanagedWaitEvent.SetLocal(hLsuwe))
    {
        ThrowLastError();
    }
#endif // !FEATURE_DBGIPC_TRANSPORT_VM


    // Mark the debugger special thread list as not dirty, empty and null.
    m_specialThreadListDirty = false;
    m_specialThreadListLength = 0;
    m_specialThreadList = NULL;

    m_shutdownBegun = false;

    return S_OK;
}

void DebuggerRCThread::WatchForStragglers(void)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_threadControlEvent != NULL);
    LOG((LF_CORDB,LL_INFO100000, "DRCT::WFS:setting event to watch "
        "for stragglers\n"));

    SetEvent(m_threadControlEvent);
}

//---------------------------------------------------------------------------------------
//
// Init sets up all the objects that the RC thread will need to run.
//
//
// Return Value:
//    S_OK on success. May also throw.
//
// Assumptions:
//    Called during startup, even if we're not debugging.
//
//
//---------------------------------------------------------------------------------------
HRESULT DebuggerRCThread::Init(void)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(!ThisIsHelperThreadWorker()); // initialized by main thread
    }
    CONTRACTL_END;


    LOG((LF_CORDB, LL_EVERYTHING, "DebuggerRCThreadInit called\n"));

    DWORD dwStatus;
    if (m_debugger == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }

    // Init should only be called once.
    if (g_pRCThread != NULL)
    {
        ThrowHR(E_FAIL);
    }

    g_pRCThread = this;

    m_favorData.Init(); // throws


    // Create the thread control event.
    m_threadControlEvent = CreateWin32EventOrThrow(NULL, kAutoResetEvent, FALSE);

    // Create the helper thread can go event.
    m_helperThreadCanGoEvent = CreateWin32EventOrThrow(NULL, kManualResetEvent, TRUE);

    m_pDCB = new(nothrow) DebuggerIPCControlBlock;

    // Don't fail out because the shared memory failed to create
#if _DEBUG
    if (m_pDCB == NULL)
    {
        LOG((LF_CORDB, LL_INFO10000,
            "DRCT::I: Failed to get Debug IPC block.\n"));
    }
#endif // _DEBUG

    HRESULT hr;

#if defined(FEATURE_DBGIPC_TRANSPORT_VM)

    if (m_pDCB)
    {
        hr = m_pDCB->Init(NULL, NULL, NULL, NULL, NULL);
        _ASSERTE(SUCCEEDED(hr)); // throws on error.
    }
#else //FEATURE_DBGIPC_TRANSPORT_VM

    // Create the events that the thread will need to receive events
    // from the out of process piece on the right side.
    // We will not fail out if CreateEvent fails for RSEA or RSER. Because
    // the worst case is that debugger cannot attach to debuggee.
    //
    HandleHolder rightSideEventAvailable(WszCreateEvent(NULL, (BOOL) kAutoResetEvent, FALSE, NULL));

    // Security fix:
    // We need to check the last error to see if the event was precreated or not
    // If so, we need to release the handle right now.
    //
    dwStatus = GetLastError();
    if (dwStatus == ERROR_ALREADY_EXISTS)
    {
        // clean up the handle now
        rightSideEventAvailable.Clear();
    }

    HandleHolder rightSideEventRead(WszCreateEvent(NULL, (BOOL) kAutoResetEvent, FALSE, NULL));

    // Security fix:
    // We need to check the last error to see if the event was precreated or not
    // If so, we need to release the handle right now.
    //
    dwStatus = GetLastError();
    if (dwStatus == ERROR_ALREADY_EXISTS)
    {
        // clean up the handle now
        rightSideEventRead.Clear();
    }


    HandleHolder leftSideUnmanagedWaitEvent(CreateWin32EventOrThrow(NULL, kManualResetEvent, FALSE));

    // Copy RSEA and RSER into the control block only if shared memory is created without error.
    if (m_pDCB)
    {
        // Since Init() gets ownership of handles as soon as it's called, we can
        // release our ownership now.
        rightSideEventAvailable.SuppressRelease();
        rightSideEventRead.SuppressRelease();
        leftSideUnmanagedWaitEvent.SuppressRelease();

        // NOTE: initialization of the debugger control block occurs partly on the left side and partly on
        // the right side. This initialization occurs in parallel, so it's unsafe to make assumptions about
        // the order in which the fields will be initialized.
        hr = m_pDCB->Init(rightSideEventAvailable,
                                       rightSideEventRead,
                                       NULL,
                                       NULL,
                                       leftSideUnmanagedWaitEvent);

        _ASSERTE(SUCCEEDED(hr)); // throws on error.
    }
#endif //FEATURE_DBGIPC_TRANSPORT_VM

    if(m_pDCB)
    {
        // We have to ensure that most of the runtime offsets for the out-of-proc DCB are initialized right away. This is
        // needed to support certian races during an interop attach. Since we can't know whether an interop attach will ever
        // happen or not, we are forced to do this now. Note: this is really too early, as some data structures haven't been
        // initialized yet!
        hr = EnsureRuntimeOffsetsInit(IPC_TARGET_OUTOFPROC);
        _ASSERTE(SUCCEEDED(hr)); // throw on error

        // Note: we have to mark that we need the runtime offsets re-initialized for the out-of-proc DCB. This is because
        // things like the patch table aren't initialized yet. Calling NeedRuntimeOffsetsReInit() ensures that this happens
        // before we really need the patch table.
        NeedRuntimeOffsetsReInit(IPC_TARGET_OUTOFPROC);

        m_pDCB->m_helperThreadStartAddr = (void *) DebuggerRCThread::ThreadProcStatic;
        m_pDCB->m_helperRemoteStartAddr = (void *) DebuggerRCThread::ThreadProcRemote;
        m_pDCB->m_leftSideProtocolCurrent = CorDB_LeftSideProtocolCurrent;
        m_pDCB->m_leftSideProtocolMinSupported = CorDB_LeftSideProtocolMinSupported;

        LOG((LF_CORDB, LL_INFO10,
             "DRCT::I: version info: %d.%d.%d current protocol=%d, min protocol=%d\n",
             m_pDCB->m_verMajor,
             m_pDCB->m_verMinor,
             m_pDCB->m_checkedBuild,
             m_pDCB->m_leftSideProtocolCurrent,
             m_pDCB->m_leftSideProtocolMinSupported));

        // Left-side always creates helper-thread.
        // @dbgtodo  inspection - by end of V3, LS will never create helper-thread :)
        m_pDCB->m_rightSideShouldCreateHelperThread = false;

        // m_DCBSize is used as a semaphore to indicate that the DCB is fully initialized.
        // let's ensure that it's updated after all the other fields.
        MemoryBarrier();
        m_pDCB->m_DCBSize = sizeof(DebuggerIPCControlBlock);
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Setup the Runtime Offsets struct.
//
// Arguments:
//    pDebuggerIPCControlBlock - Pointer to the debugger's portion of the IPC
//       block, which this routine will write into the offsets of various parts of
//       the runtime.
//
// Return Value:
//    S_OK on success.
//
//---------------------------------------------------------------------------------------
HRESULT DebuggerRCThread::SetupRuntimeOffsets(DebuggerIPCControlBlock * pDebuggerIPCControlBlock)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    // Allocate the struct if needed. We just fill in any existing one.
    DebuggerIPCRuntimeOffsets * pDebuggerRuntimeOffsets = pDebuggerIPCControlBlock->m_pRuntimeOffsets;

    if (pDebuggerRuntimeOffsets == NULL)
    {
        // Perhaps we should preallocate this. This is the only allocation
        // that would force SendIPCEvent to throw an exception. It'd be very
        // nice to have
        CONTRACT_VIOLATION(ThrowsViolation);
        pDebuggerRuntimeOffsets = new DebuggerIPCRuntimeOffsets();
        _ASSERTE(pDebuggerRuntimeOffsets != NULL); // throws on oom
    }

    // Fill out the struct.
#ifdef FEATURE_INTEROP_DEBUGGING
    pDebuggerRuntimeOffsets->m_genericHijackFuncAddr = Debugger::GenericHijackFunc;
    // Set flares - these only exist for interop debugging.
    pDebuggerRuntimeOffsets->m_signalHijackStartedBPAddr = (void*) SignalHijackStartedFlare;
    pDebuggerRuntimeOffsets->m_excepForRuntimeHandoffStartBPAddr = (void*) ExceptionForRuntimeHandoffStartFlare;
    pDebuggerRuntimeOffsets->m_excepForRuntimeHandoffCompleteBPAddr = (void*) ExceptionForRuntimeHandoffCompleteFlare;
    pDebuggerRuntimeOffsets->m_signalHijackCompleteBPAddr = (void*) SignalHijackCompleteFlare;
    pDebuggerRuntimeOffsets->m_excepNotForRuntimeBPAddr = (void*) ExceptionNotForRuntimeFlare;
    pDebuggerRuntimeOffsets->m_notifyRSOfSyncCompleteBPAddr = (void*) NotifyRightSideOfSyncCompleteFlare;
    pDebuggerRuntimeOffsets->m_debuggerWordTLSIndex = g_debuggerWordTLSIndex;
#endif // FEATURE_INTEROP_DEBUGGING

    pDebuggerRuntimeOffsets->m_pPatches = DebuggerController::GetPatchTable();
    pDebuggerRuntimeOffsets->m_pPatchTableValid = (BOOL*)DebuggerController::GetPatchTableValidAddr();
    pDebuggerRuntimeOffsets->m_offRgData = DebuggerPatchTable::GetOffsetOfEntries();
    pDebuggerRuntimeOffsets->m_offCData = DebuggerPatchTable::GetOffsetOfCount();
    pDebuggerRuntimeOffsets->m_cbPatch = sizeof(DebuggerControllerPatch);
    pDebuggerRuntimeOffsets->m_offAddr = offsetof(DebuggerControllerPatch, address);
    pDebuggerRuntimeOffsets->m_offOpcode = offsetof(DebuggerControllerPatch, opcode);
    pDebuggerRuntimeOffsets->m_cbOpcode = sizeof(PRD_TYPE);
    pDebuggerRuntimeOffsets->m_offTraceType = offsetof(DebuggerControllerPatch, trace.type);
    pDebuggerRuntimeOffsets->m_traceTypeUnmanaged = TRACE_UNMANAGED;

    // @dbgtodo  inspection - this should all go away or be obtained from DacDbi Primitives.
    g_pEEInterface->GetRuntimeOffsets(&pDebuggerRuntimeOffsets->m_TLSIndex,
                                      &pDebuggerRuntimeOffsets->m_TLSEEThreadOffset,
                                      &pDebuggerRuntimeOffsets->m_TLSIsSpecialOffset,
                                      &pDebuggerRuntimeOffsets->m_TLSCantStopOffset,
                                      &pDebuggerRuntimeOffsets->m_EEThreadStateOffset,
                                      &pDebuggerRuntimeOffsets->m_EEThreadStateNCOffset,
                                      &pDebuggerRuntimeOffsets->m_EEThreadPGCDisabledOffset,
                                      &pDebuggerRuntimeOffsets->m_EEThreadPGCDisabledValue,
                                      &pDebuggerRuntimeOffsets->m_EEThreadFrameOffset,
                                      &pDebuggerRuntimeOffsets->m_EEThreadMaxNeededSize,
                                      &pDebuggerRuntimeOffsets->m_EEThreadSteppingStateMask,
                                      &pDebuggerRuntimeOffsets->m_EEMaxFrameValue,
                                      &pDebuggerRuntimeOffsets->m_EEThreadDebuggerFilterContextOffset,
                                      &pDebuggerRuntimeOffsets->m_EEFrameNextOffset,
                                      &pDebuggerRuntimeOffsets->m_EEIsManagedExceptionStateMask);

    // Remember the struct in the control block.
    pDebuggerIPCControlBlock->m_pRuntimeOffsets = pDebuggerRuntimeOffsets;

    return S_OK;
}

struct DebugFilterParam
{
    DebuggerIPCEvent *event;
};

// Filter called when we throw an exception while Handling events.
static LONG _debugFilter(LPEXCEPTION_POINTERS ep, PVOID pv)
{
    LOG((LF_CORDB, LL_INFO10,
         "Unhandled exception in Debugger::HandleIPCEvent\n"));

    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

#if defined(_DEBUG)
    DebuggerIPCEvent *event = ((DebugFilterParam *)pv)->event;

    DWORD pid = GetCurrentProcessId();
    DWORD tid = GetCurrentThreadId();

    DebuggerIPCEventType type = (DebuggerIPCEventType) (event->type & DB_IPCE_TYPE_MASK);
#endif // _DEBUG

    // We should never AV here. In a debug build, throw up an assert w/ lots of useful (private) info.
#ifdef _DEBUG
    {
        // We can't really use SStrings on the helper thread; though if we're at this point, we've already died.
        // So go ahead and risk it and use them anyways.
        SString sStack;
        StackScratchBuffer buffer;
        GetStackTraceAtContext(sStack, ep->ContextRecord);
        const CHAR *string = NULL;

        EX_TRY
        {
            string = sStack.GetANSI(buffer);
        }
        EX_CATCH
        {
            string = "*Could not retrieve stack*";
        }
        EX_END_CATCH(RethrowTerminalExceptions);

        CONSISTENCY_CHECK_MSGF(false,
            ("Unhandled exception on the helper thread.\nEvent=%s(0x%p)\nCode=0x%0x, Ip=0x%p, .cxr=%p, .exr=%p.\n pid=0x%x (%d), tid=0x%x (%d).\n-----\nStack of exception:\n%s\n----\n",
            IPCENames::GetName(type), type,
            ep->ExceptionRecord->ExceptionCode, GetIP(ep->ContextRecord), ep->ContextRecord, ep->ExceptionRecord,
            pid, pid, tid, tid,
            string));
    }
#endif

    // For debugging, we can change the behavior by manually setting eax.
    // EXCEPTION_EXECUTE_HANDLER=1, EXCEPTION_CONTINUE_SEARCH=0, EXCEPTION_CONTINUE_EXECUTION=-1
    return EXCEPTION_CONTINUE_SEARCH;
}

//---------------------------------------------------------------------------------------
//
// Primary function of the Runtime Controller thread. First, we let
// the Debugger Interface know that we're up and running. Then, we run
// the main loop.
//
//---------------------------------------------------------------------------------------
void DebuggerRCThread::ThreadProc(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;        // Debugger::SuspendComplete can trigger GC

        // Although we're the helper thread, we haven't set it yet.
        DISABLED(PRECONDITION(ThisIsHelperThreadWorker()));

        INSTANCE_CHECK;
    }
    CONTRACTL_END;

    STRESS_LOG_RESERVE_MEM (0);
    // This message actually serves a purpose (which is why it is always run)
    // The Stress log is run during hijacking, when other threads can be suspended
    // at arbitrary locations (including when holding a lock that NT uses to serialize
    // all memory allocations).  By sending a message now, we insure that the stress
    // log will not allocate memory at these critical times an avoid deadlock.
    {
        SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
        STRESS_LOG0(LF_CORDB|LF_ALWAYS, LL_ALWAYS, "Debugger Thread spinning up\n");

        // Call this to force creation of the TLS slots on helper-thread.
        IsDbgHelperSpecialThread();
    }

#ifdef _DEBUG
    // Track the helper thread.
    s_DbgHelperThreadId.SetToCurrentThread();
#endif
    CantAllocHolder caHolder;


#ifdef _DEBUG
    // Cause wait in the helper thread startup. This lets us test against certain races.
    // 1 = 6 sec. (shorter than Poll)
    // 2 = 12 sec (longer than Poll).
    // 3 = infinite - never comes up.
    static int fDelayHelper = -1;

    if (fDelayHelper == -1)
    {
        fDelayHelper = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgDelayHelper);
    }

    if (fDelayHelper)
    {
        DWORD dwSleep = 6000;

        switch(fDelayHelper)
        {
            case 1: dwSleep =  6000; break;
            case 2: dwSleep = 12000; break;
            case 3: dwSleep = INFINITE; break;
        }

        ClrSleepEx(dwSleep, FALSE);
    }
#endif

    LOG((LF_CORDB, LL_INFO1000, "DRCT::TP: helper thread spinning up...\n"));

    // In case the shared memory is not initialized properly, it will be noop
    if (m_pDCB == NULL)
    {
        return;
    }

    // Lock the debugger before spinning up.
    Debugger::DebuggerLockHolder debugLockHolder(m_debugger);

    if (m_pDCB->m_helperThreadId != 0)
    {
        // someone else has created a helper thread, we're outta here
        // the most likely scenario here is that there was some kind of
        // race between remotethread creation and localthread creation

        LOG((LF_CORDB, LL_EVERYTHING, "Second debug helper thread creation detected, thread will safely suicide\n"));
        // dbgLockHolder goes out of scope - implicit Release
        return;
    }

    // this thread took the lock and there is no existing m_helperThreadID therefore
    // this *IS* the helper thread and nobody else can be the helper thread

    // the handle was created by the Start method
    _ASSERTE(m_thread != NULL);

#ifdef _DEBUG
    // Make sure that we have the proper permissions.
    {
        DWORD dwWaitResult = WaitForSingleObject(m_thread, 0);
        _ASSERTE(dwWaitResult == WAIT_TIMEOUT);
    }
#endif

    // Mark that we're the true helper thread. Now that we've marked
    // this, no other threads will ever become the temporary helper
    // thread.
    m_pDCB->m_helperThreadId = GetCurrentThreadId();

    LOG((LF_CORDB, LL_INFO1000, "DRCT::TP: helper thread id is 0x%x helperThreadId\n",
        m_pDCB->m_helperThreadId));

    // If there is a temporary helper thread, then we need to wait for
    // it to finish being the helper thread before we can become the
    // helper thread.
    if (m_pDCB->m_temporaryHelperThreadId != 0)
    {
        LOG((LF_CORDB, LL_INFO1000,
             "DRCT::TP: temporary helper thread 0x%x is in the way, "
             "waiting...\n",
             m_pDCB->m_temporaryHelperThreadId));

        debugLockHolder.Release();

        // Wait for the temporary helper thread to finish up.
        DWORD dwWaitResult = WaitForSingleObject(m_helperThreadCanGoEvent, INFINITE);
        (void)dwWaitResult; //prevent "unused variable" error from GCC

        LOG((LF_CORDB, LL_INFO1000, "DRCT::TP: done waiting for temp help to finish up.\n"));

        _ASSERTE(dwWaitResult == WAIT_OBJECT_0);
        _ASSERTE(m_pDCB->m_temporaryHelperThreadId==0);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO1000, "DRCT::TP: no temp help in the way...\n"));

        debugLockHolder.Release();
    }

    // Run the main loop as the true helper thread.
    MainLoop();
}

void DebuggerRCThread::RightSideDetach(void)
{
    _ASSERTE( m_fDetachRightSide == false );
    m_fDetachRightSide = true;
#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    CloseIPCHandles();
#endif // !FEATURE_DBGIPC_TRANSPORT_VM
}

//
// These defines control how many times we spin while waiting for threads to sync and how often. Note its higher in
// debug builds to allow extra time for threads to sync.
//
#define CorDB_SYNC_WAIT_TIMEOUT  20   // 20ms

#ifdef _DEBUG
#define CorDB_MAX_SYNC_SPIN_COUNT (10000 / CorDB_SYNC_WAIT_TIMEOUT)  // (10 seconds)
#else
#define CorDB_MAX_SYNC_SPIN_COUNT (3000 / CorDB_SYNC_WAIT_TIMEOUT)   // (3 seconds)
#endif

//
// NDPWhidbey issue 10749 - Due to a compiler change for vc7.1,
// Don't inline this function!
// PAL_TRY allocates space on the stack and so can not be used within a loop,
// else we'll slowly leak stack space w/ each interation and get an overflow.
// So make this its own function to enforce that we free the stack space between
// iterations.
//
bool HandleIPCEventWrapper(Debugger* pDebugger, DebuggerIPCEvent *e)
{
    struct Param : DebugFilterParam
    {
        Debugger* pDebugger;
        bool wasContinue;
    } param;
    param.event = e;
    param.pDebugger = pDebugger;
    param.wasContinue = false;
    PAL_TRY(Param *, pParam, &param)
    {
        pParam->wasContinue = pParam->pDebugger->HandleIPCEvent(pParam->event);
    }
    PAL_EXCEPT_FILTER(_debugFilter)
    {
        LOG((LF_CORDB, LL_INFO10, "Unhandled exception caught in Debugger::HandleIPCEvent\n"));
    }
    PAL_ENDTRY

    return param.wasContinue;
}

bool DebuggerRCThread::HandleRSEA()
{
    CONTRACTL
    {
        NOTHROW;
        if (g_pEEInterface->GetThread() != NULL) { GC_TRIGGERS; } else { GC_NOTRIGGER; }
        PRECONDITION(ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000, "RSEA from out of process (right side)\n"));
    DebuggerIPCEvent * e;
#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    // Make room for any Right Side event on the stack.
 BYTE buffer[CorDBIPC_BUFFER_SIZE];
    e = (DebuggerIPCEvent *) buffer;

    // If the RSEA is signaled, then handle the event from the Right Side.
    memcpy(e, GetIPCEventReceiveBuffer(), CorDBIPC_BUFFER_SIZE);
#else
    // Be sure to fetch the event into the official receive buffer since some event handlers assume it's there
    // regardless of the the event buffer pointer passed to them.
    e = GetIPCEventReceiveBuffer();
    g_pDbgTransport->GetNextEvent(e, CorDBIPC_BUFFER_SIZE);
#endif // !FEATURE_DBGIPC_TRANSPOPRT

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    // If no reply is required, then let the Right Side go since we've got a copy of the event now.
    _ASSERTE(!e->asyncSend || !e->replyRequired);

    if (!e->replyRequired && !e->asyncSend)
    {
        LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: no reply required, letting Right Side go.\n"));

        BOOL succ = SetEvent(m_pDCB->m_rightSideEventRead);

        if (!succ)
            CORDBDebuggerSetUnrecoverableWin32Error(m_debugger, 0, true);
    }
#ifdef LOGGING
    else if (e->asyncSend)
        LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: async send.\n"));
    else
        LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: reply required, holding Right Side...\n"));
#endif
#endif // !FEATURE_DBGIPC_TRANSPORT_VM

    // Pass the event to the debugger for handling. Returns true if the event was a Continue event and we can
    // stop looking for stragglers.  We wrap this whole thing in an exception handler to help us debug faults.
    bool wasContinue = false;

    wasContinue = HandleIPCEventWrapper(m_debugger, e);

    return wasContinue;
}

//---------------------------------------------------------------------------------------
//
// Main loop of the Runtime Controller thread. It waits for IPC events
// and dishes them out to the Debugger object for processing.
//
// Some of this logic is copied in Debugger::VrpcToVls
//
//---------------------------------------------------------------------------------------
void DebuggerRCThread::MainLoop()
{
    // This function can only be called on native Debugger helper thread.
    //

    CONTRACTL
    {
        NOTHROW;

        PRECONDITION(m_thread != NULL);
        PRECONDITION(ThisIsHelperThreadWorker());
        PRECONDITION(IsDbgHelperSpecialThread());   // Can only be called on native debugger helper thread
        PRECONDITION((!ThreadStore::HoldingThreadStore()) || g_fProcessDetach);
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: running main loop\n"));

    // Anybody doing helper duty is in a can't-stop range, period.
    // Our helper thread is already in a can't-stop range, so this is particularly useful for
    // threads doing helper duty.
    CantStopHolder cantStopHolder;

    HANDLE rghWaitSet[DRCT_COUNT_FINAL];

#ifdef _DEBUG
    DWORD dwSyncSpinCount = 0;
#endif

    // We start out just listening on RSEA and the thread control event...
    unsigned int cWaitCount = DRCT_COUNT_INITIAL;
    DWORD dwWaitTimeout = INFINITE;
    rghWaitSet[DRCT_CONTROL_EVENT] = m_threadControlEvent;
    rghWaitSet[DRCT_FAVORAVAIL] = GetFavorAvailableEvent();
#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    rghWaitSet[DRCT_RSEA] = m_pDCB->m_rightSideEventAvailable;
#else
    rghWaitSet[DRCT_RSEA] = g_pDbgTransport->GetIPCEventReadyEvent();
#endif // !FEATURE_DBGIPC_TRANSPORT_VM

    CONTRACT_VIOLATION(ThrowsViolation);// HndCreateHandle throws, and this loop is not backstopped by any EH

    // Lock holder. Don't take it yet. We take lock on this when we succeeded suspended runtime.
    // We will release the lock later when continue happens and runtime resumes
    Debugger::DebuggerLockHolder debugLockHolderSuspended(m_debugger, false);

    while (m_run)
    {
        LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: waiting for event.\n"));

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
        // If there is a debugger attached, wait on its handle, too...
        if ((cWaitCount == DRCT_COUNT_INITIAL) &&
            m_pDCB->m_rightSideProcessHandle.ImportToLocalProcess() != NULL)
        {
            _ASSERTE((cWaitCount + 1) == DRCT_COUNT_FINAL);
            rghWaitSet[DRCT_DEBUGGER_EVENT] = m_pDCB->m_rightSideProcessHandle;
            cWaitCount = DRCT_COUNT_FINAL;
        }
#endif // !FEATURE_DBGIPC_TRANSPORT_VM


        if (m_fDetachRightSide)
        {
            m_fDetachRightSide = false;

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
            _ASSERTE(cWaitCount == DRCT_COUNT_FINAL);
            _ASSERTE((cWaitCount - 1) == DRCT_COUNT_INITIAL);

            rghWaitSet[DRCT_DEBUGGER_EVENT] = NULL;
            cWaitCount = DRCT_COUNT_INITIAL;
#endif // !FEATURE_DBGIPC_TRANSPORT_VM
        }

        // Wait for an event from the Right Side.
        DWORD dwWaitResult = WaitForMultipleObjectsEx(cWaitCount, rghWaitSet, FALSE, dwWaitTimeout, FALSE);

        if (!m_run)
        {
            continue;
        }


        if (dwWaitResult == WAIT_OBJECT_0 + DRCT_DEBUGGER_EVENT)
        {
            // If the handle of the right side process is signaled, then we've lost our controlling debugger. We
            // terminate this process immediatley in such a case.
            LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: terminating this process. Right Side has exited.\n"));
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
            EEPOLICY_HANDLE_FATAL_ERROR(0);
            _ASSERTE(!"Should never reach this point.");
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_FAVORAVAIL)
        {
            // execute the callback set by DoFavor()
            FAVORCALLBACK fpCallback = GetFavorFnPtr();
            // We never expect the callback to be null unless some other component
            // wrongly signals our event (see DD 463807).
            // In case we messed up, we will not set the FavorReadEvent and will hang favor requesting thread.
            if (fpCallback)
            {
                (*fpCallback)(GetFavorData());
                SetEvent(GetFavorReadEvent());
            }
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_RSEA)
        {
            bool fWasContinue = HandleRSEA();

            if (fWasContinue)
            {

                // If they called continue, then we must have released the TSL.
                _ASSERTE(!ThreadStore::HoldingThreadStore() || g_fProcessDetach);

                // Let's release the lock here since runtime is resumed.
                debugLockHolderSuspended.Release();

                // This debugger thread shoud not be holding debugger locks anymore
                _ASSERTE(!g_pDebugger->ThreadHoldsLock());
#ifdef _DEBUG
                // Always reset the syncSpinCount to 0 on a continue so that we have the maximum number of possible
                // spins the next time we need to sync.
                dwSyncSpinCount = 0;
#endif

                if (dwWaitTimeout != INFINITE)
                {
                    LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: don't check for stragglers due to continue.\n"));

                    dwWaitTimeout = INFINITE;
                }

            }
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_CONTROL_EVENT)
        {
            LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: straggler event set.\n"));

            ThreadStoreLockHolder tsl;
            Debugger::DebuggerLockHolder debugLockHolder(m_debugger);
            // Make sure that we're still synchronizing...
            if (m_debugger->IsSynchronizing())
            {
                LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: dropping the timeout.\n"));

                dwWaitTimeout = CorDB_SYNC_WAIT_TIMEOUT;

                //
                // Skip waiting the first time and just give it a go.  Note: Implicit
                // release of the debugger and thread store lock, because we are leaving its scope.
                //
                goto LWaitTimedOut;
            }
#ifdef LOGGING
            else
                LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: told to wait, but not syncing anymore.\n"));
#endif
            // dbgLockHolder goes out of scope - implicit Release
            // tsl goes out of scope - implicit Release
         }
        else if (dwWaitResult == WAIT_TIMEOUT)
        {

LWaitTimedOut:

            LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: wait timed out.\n"));

            ThreadStore::LockThreadStore();
            // Debugger::DebuggerLockHolder debugLockHolder(m_debugger);
            // Explicitly get the lock here since we try to check to see if
            // have suspended.  We will release the lock if we are not suspended yet.
            //
            debugLockHolderSuspended.Acquire();

            // We should still be synchronizing, otherwise we would not have timed out.
            _ASSERTE(m_debugger->IsSynchronizing());

            LOG((LF_CORDB, LL_INFO1000, "DRCT::ML:: sweeping the thread list.\n"));

#ifdef _DEBUG
            // If we fail to suspend the CLR, don't bother waiting for a BVT to timeout,
            // fire up an assert up now.
            // Threads::m_DebugWillSyncCount+1 is the number of outstanding threads.
            // We're trying to suspend any thread w/ TS_DebugWillSync set.
            if (dwSyncSpinCount++ > CorDB_MAX_SYNC_SPIN_COUNT)
            {
                _ASSERTE_MSG(false, "Timeout trying to suspend CLR for debugging. Possibly a deadlock.\n"\
                                    "You can ignore this assert to continue waiting\n");
                dwSyncSpinCount = 0;
            }
#endif

            // Don't call Sweep if we're doing helper thread duty.
            // If we're doing helper thread duty, then we already Suspended the CLR, and we already hold the TSL.
            bool fSuspended;
            {
                // SweepThreadsForDebug() may call new!!! ARGG!!!
                SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
                fSuspended = g_pEEInterface->SweepThreadsForDebug(false);
            }

            if (fSuspended)
            {
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::ML:: wait set empty after sweep.\n");

                // There are no more threads to wait for, so go ahead and send the sync complete event.
                m_debugger->SuspendComplete();
                dwWaitTimeout = INFINITE;

                // Note: we hold the thread store lock now and debugger lock...

                // We also hold debugger lock the whole time that Runtime is stopped. We will release the debugger lock
                // when we receive the Continue event that resumes the runtime.

                _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach);
            }
            else
            {
                // If we're doing helper thread duty, then we expect to have been suspended already.
                // And so the sweep should always succeed.
                STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::ML:: threads still syncing after sweep.\n");
                debugLockHolderSuspended.Release();
                ThreadStore::UnlockThreadStore();
            }
            // debugLockHolderSuspended does not go out of scope. It has to be either released explicitly on the line above or
            // we intend to hold the lock till we hit continue event.

        }
    }

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::ML:: Exiting.\n");
}

//---------------------------------------------------------------------------------------
//
// Main loop of the temporary Helper thread. It waits for IPC events
// and dishes them out to the Debugger object for processing.
//
// Notes:
//     When we enter here, we are holding debugger lock and thread store lock.
//     The debugger lock was SuppressRelease in DoHelperThreadDuty. The continue event
//     that we are waiting for will trigger the corresponding release.
//
//     IMPORTANT!!! READ ME!!!!
//     This MainLoop is similiar to MainLoop function above but simplified to deal with only
//     some scenario. So if you change here, you should look at MainLoop to see if same change is
//     required.
//---------------------------------------------------------------------------------------
void DebuggerRCThread::TemporaryHelperThreadMainLoop()
{
    CONTRACTL
    {
        NOTHROW;


        // If we come in here, this managed thread is trying to do helper thread duty.
        // It should be holding the debugger lock!!!
        //
        PRECONDITION(m_debugger->ThreadHoldsLock());
        PRECONDITION((ThreadStore::HoldingThreadStore()) || g_fProcessDetach);
        PRECONDITION(ThisIsTempHelperThread());
    }
    CONTRACTL_END;

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::THTML:: Doing helper thread duty, running main loop.\n");
    // Anybody doing helper duty is in a can't-stop range, period.
    // Our helper thread is already in a can't-stop range, so this is particularly useful for
    // threads doing helper duty.
    CantStopHolder cantStopHolder;

    HANDLE rghWaitSet[DRCT_COUNT_FINAL];

#ifdef _DEBUG
    DWORD dwSyncSpinCount = 0;
#endif

    // We start out just listening on RSEA and the thread control event...
    unsigned int cWaitCount = DRCT_COUNT_INITIAL;
    DWORD dwWaitTimeout = INFINITE;
    rghWaitSet[DRCT_CONTROL_EVENT] = m_threadControlEvent;
    rghWaitSet[DRCT_FAVORAVAIL] = GetFavorAvailableEvent();
#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    rghWaitSet[DRCT_RSEA] = m_pDCB->m_rightSideEventAvailable;
#else //FEATURE_DBGIPC_TRANSPORT_VM
    rghWaitSet[DRCT_RSEA] = g_pDbgTransport->GetIPCEventReadyEvent();
#endif // !FEATURE_DBGIPC_TRANSPORT_VM

    CONTRACT_VIOLATION(ThrowsViolation);// HndCreateHandle throws, and this loop is not backstopped by any EH

    while (m_run)
    {
        LOG((LF_CORDB, LL_INFO1000, "DRCT::ML: waiting for event.\n"));

        // Wait for an event from the Right Side.
        DWORD dwWaitResult = WaitForMultipleObjectsEx(cWaitCount, rghWaitSet, FALSE, dwWaitTimeout, FALSE);

        if (!m_run)
        {
            continue;
        }


        if (dwWaitResult == WAIT_OBJECT_0 + DRCT_DEBUGGER_EVENT)
        {
            // If the handle of the right side process is signaled, then we've lost our controlling debugger. We
            // terminate this process immediatley in such a case.
            LOG((LF_CORDB, LL_INFO1000, "DRCT::THTML: terminating this process. Right Side has exited.\n"));

            TerminateProcess(GetCurrentProcess(), 0);
            _ASSERTE(!"Should never reach this point.");
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_FAVORAVAIL)
        {
            // execute the callback set by DoFavor()
            (*GetFavorFnPtr())(GetFavorData());

            SetEvent(GetFavorReadEvent());
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_RSEA)
        {
            // @todo:
            // We are only interested in dealing with Continue event here...
            // Once we remove the HelperThread duty, this will just go away.
            //
            bool fWasContinue = HandleRSEA();

            if (fWasContinue)
            {
                // If they called continue, then we must have released the TSL.
                _ASSERTE(!ThreadStore::HoldingThreadStore() || g_fProcessDetach);

#ifdef _DEBUG
                // Always reset the syncSpinCount to 0 on a continue so that we have the maximum number of possible
                // spins the next time we need to sync.
                dwSyncSpinCount = 0;
#endif

                // HelperThread duty is finished. We have got a Continue message
                goto LExit;
            }
        }
        else if (dwWaitResult == WAIT_OBJECT_0 + DRCT_CONTROL_EVENT)
        {
            LOG((LF_CORDB, LL_INFO1000, "DRCT::THTML:: straggler event set.\n"));

            // Make sure that we're still synchronizing...
            _ASSERTE(m_debugger->IsSynchronizing());
            LOG((LF_CORDB, LL_INFO1000, "DRCT::THTML:: dropping the timeout.\n"));

            dwWaitTimeout = CorDB_SYNC_WAIT_TIMEOUT;

            //
            // Skip waiting the first time and just give it a go.  Note: Implicit
            // release of the lock, because we are leaving its scope.
            //
            goto LWaitTimedOut;
         }
        else if (dwWaitResult == WAIT_TIMEOUT)
        {

LWaitTimedOut:

            LOG((LF_CORDB, LL_INFO1000, "DRCT::THTML:: wait timed out.\n"));

            // We should still be synchronizing, otherwise we would not have timed out.
            _ASSERTE(m_debugger->IsSynchronizing());

            LOG((LF_CORDB, LL_INFO1000, "DRCT::THTML:: sweeping the thread list.\n"));

#ifdef _DEBUG
            // If we fail to suspend the CLR, don't bother waiting for a BVT to timeout,
            // fire up an assert up now.
            // Threads::m_DebugWillSyncCount+1 is the number of outstanding threads.
            // We're trying to suspend any thread w/ TS_DebugWillSync set.
            if (dwSyncSpinCount++ > CorDB_MAX_SYNC_SPIN_COUNT)
            {
                _ASSERTE(false || !"Timeout trying to suspend CLR for debugging. Possibly a deadlock. "
                "You can ignore this assert to continue waiting\n");
                dwSyncSpinCount = 0;
            }
#endif

            STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::THTML:: wait set empty after sweep.\n");

            // We are holding Debugger lock (Look at the SuppressRelease on the DoHelperThreadDuty)
            // The debugger lock will be released on the Continue event which we will then
            // exit the loop.

            // There are no more threads to wait for, so go ahead and send the sync complete event.
            m_debugger->SuspendComplete();
            dwWaitTimeout = INFINITE;

            // Note: we hold the thread store lock now and debugger lock...
            _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach);

        }
    }

LExit:

    STRESS_LOG0(LF_CORDB, LL_INFO1000, "DRCT::THTML:: Exiting.\n");
}



//
// This is the thread's real thread proc. It simply calls to the
// thread proc on the RCThread object.
//
/*static*/ DWORD WINAPI DebuggerRCThread::ThreadProcRemote(LPVOID)
{
    // We just wrap create a local thread and we're outta here
    WRAPPER_NO_CONTRACT;

    ClrFlsSetThreadType(ThreadType_DbgHelper);

    LOG((LF_CORDB, LL_EVERYTHING, "ThreadProcRemote called\n"));
#ifdef _DEBUG
    dbgOnly_IdentifySpecialEEThread();
#endif

    // this method can be called both by a local createthread or a remote create thread
    // so we must use the g_RCThread global to find the (unique!) this pointer
    // we cannot count on the parameter.

    DebuggerRCThread* t = (DebuggerRCThread*)g_pRCThread;

    // This remote thread is created by the debugger process
    // and so its ACLs will reflect permissions for the user running
    // the debugger. If this process is running in the context of a
    // different user then this (the now running) process will not be
    // able to do operations on that (remote) thread.
    //
    // To avoid this problem, if we are the remote thread, then
    // we simply launch a new, local, thread right here and let
    // the remote thread die.  This new thread is created the same
    // way as always, and since it is created by this process
    // this process will be able to synchronize with it and so forth

    t->Start();  // this thread is remote, we must start a new thread

    return 0;
}

//
// This is the thread's real thread proc. It simply calls to the
// thread proc on the RCThread object.
//
/*static*/ DWORD WINAPI DebuggerRCThread::ThreadProcStatic(LPVOID)
{
    // We just wrap the instance method DebuggerRCThread::ThreadProc
    WRAPPER_NO_CONTRACT;

    ClrFlsSetThreadType(ThreadType_DbgHelper);

    LOG((LF_CORDB, LL_EVERYTHING, "ThreadProcStatic called\n"));

#ifdef _DEBUG
    dbgOnly_IdentifySpecialEEThread();
#endif

    DebuggerRCThread* t = (DebuggerRCThread*)g_pRCThread;

    t->ThreadProc(); // this thread is local, go and become the helper

    return 0;
}

RCThreadLazyInit * DebuggerRCThread::GetLazyData()
{
    return g_pDebugger->GetRCThreadLazyData();
}


//
// Start actually creates and starts the RC thread. It waits for the thread
// to come up and perform initial synchronization with the Debugger
// Interface before returning.
//
HRESULT DebuggerRCThread::Start(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_EVERYTHING, "DebuggerRCThread::Start called...\n"));

    DWORD helperThreadId;

    if (m_thread != NULL)
    {
       LOG((LF_CORDB, LL_EVERYTHING, "DebuggerRCThread::Start declined to start another helper thread...\n"));
       return S_OK;
    }

    Debugger::DebuggerLockHolder debugLockHolder(m_debugger);

    if (m_thread == NULL)
    {
        // Create suspended so that we can sniff the tid before the thread actually runs.
        // This may not be before the native thread-create event, but should be before everything else.
        // Note: strange as it may seem, the Right Side depends on us
        // using CreateThread to create the helper thread here. If you
        // ever change this to some other thread creation routine, you
        // need to update the logic in process.cpp where we discover the
        // helper thread on CREATE_THREAD_DEBUG_EVENTs...
        m_thread = CreateThread(NULL, 0, DebuggerRCThread::ThreadProcStatic,
                                NULL, CREATE_SUSPENDED, &helperThreadId );

        if (m_thread == NULL)
        {
            LOG((LF_CORDB, LL_EVERYTHING, "DebuggerRCThread failed, err=%d\n", GetLastError()));
            hr = HRESULT_FROM_GetLastError();

        }
        else
        {
            LOG((LF_CORDB, LL_EVERYTHING, "DebuggerRCThread start was successful, id=%d\n", helperThreadId));
        }

        // This gets published immediately.
        DebuggerIPCControlBlock* dcb = GetDCB();
        PREFIX_ASSUME(dcb != NULL);
        dcb->m_realHelperThreadId = helperThreadId;

#ifdef _DEBUG
        // Record the OS Thread ID for debugging purposes.
        m_DbgHelperThreadOSTid = helperThreadId ;
#endif

        if (m_thread != NULL)
        {
            ResumeThread(m_thread);
        }

    }

    // unlock debugger lock is implied.

    return hr;
}


//---------------------------------------------------------------------------------------
//
// Stop causes the RC thread to stop receiving events and exit.
// It does not wait for it to exit before returning (hence "AsyncStop" instead of "Stop").
//
// Return Value:
//   Always S_OK at the moment.
//
//---------------------------------------------------------------------------------------
HRESULT DebuggerRCThread::AsyncStop(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

#ifdef TARGET_X86
        PRECONDITION(!ThisIsHelperThreadWorker());
#else
        PRECONDITION(!ThisIsHelperThreadWorker());
#endif
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    m_run = FALSE;

    // We need to get the helper thread out of its wait loop. So ping the thread-control event.
    // (Don't ping RSEA since that event should be used only for IPC communication).
    // Don't bother waiting for it to exit.
    SetEvent(this->m_threadControlEvent);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// This method checks that the runtime offset has been loaded, and if not, loads it.
//
//---------------------------------------------------------------------------------------
HRESULT inline DebuggerRCThread::EnsureRuntimeOffsetsInit(IpcTarget ipcTarget)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (m_rgfInitRuntimeOffsets[ipcTarget] == true)
    {
        hr = SetupRuntimeOffsets(m_pDCB);
        _ASSERTE(SUCCEEDED(hr)); // throws on failure

        // RuntimeOffsets structure is setup.
        m_rgfInitRuntimeOffsets[ipcTarget] = false;
    }

    return hr;
}

//
// Call this function to tell the rc thread that we need the runtime offsets re-initialized at the next avaliable time.
//
void DebuggerRCThread::NeedRuntimeOffsetsReInit(IpcTarget i)
{
    LIMITED_METHOD_CONTRACT;

    m_rgfInitRuntimeOffsets[i] = true;
}

//---------------------------------------------------------------------------------------
//
// Send an debug event to the Debugger. This may be either a notification
// or a reply to a debugger query.
//
// Arguments:
//    iTarget - which connection. This must be IPC_TARGET_OUTOFPROC.
//
// Return Value:
//    S_OK on success
//
// Notes:
//    SendIPCEvent is used by the Debugger object to send IPC events to
//    the Debugger Interface. It waits for acknowledgement from the DI
//    before returning.
//
//    This assumes that the event send buffer has been properly
//    filled in. All it does it wake up the DI and let it know that its
//    safe to copy the event out of this process.
//
//    This function may block indefinitely if the controlling debugger
//    suddenly went away.
//
//    @dbgtodo  inspection - this is all a nop around SendRawEvent!
//
//---------------------------------------------------------------------------------------
HRESULT DebuggerRCThread::SendIPCEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER; // duh, we're in preemptive..

        if (m_debugger->m_isBlockedOnGarbageCollectionEvent)
        {
            //
            // If m_debugger->m_isBlockedOnGarbageCollectionEvent is true, then it must be reporting
            // either the BeforeGarbageCollection event or the AfterGarbageCollection event
            // The thread is in preemptive mode during BeforeGarbageCollection
            // The thread is in cooperative mode during AfterGarbageCollection
            // In either case, the thread mode doesn't really matter because GC has already taken control
            // of execution.
            //
            // Despite the fact that we are actually in preemptive mode during BeforeGarbageCollection,
            // because IsGCThread() is true, the EEContract::DoCheck() will happily accept the fact we are
            // testing for MODE_COOPERATIVE.
            //
            MODE_COOPERATIVE;
        }
        else
        {
            if (ThisIsHelperThreadWorker())
            {
                // When we're stopped, the helper could actually be contracted as either mode-cooperative
                // or mode-preemptive!
                // If we're the helper thread, we're only sending events while we're stopped.
                // Our callers will be mode-cooperative, so call this mode_cooperative to avoid a bunch
                // of unncessary contract violations.
                MODE_COOPERATIVE;
            }
            else
            {
                // Managed threads sending debug events should always be in preemptive mode.
                MODE_PREEMPTIVE;
            }
        }
        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;


    // one right side
    _ASSERTE(m_debugger->ThreadHoldsLock());

    HRESULT hr = S_OK;

    // All the initialization is already done in code:DebuggerRCThread.Init,
    // so we can just go ahead and send the event.

    DebuggerIPCEvent* pManagedEvent = GetIPCEventSendBuffer();

    STRESS_LOG2(LF_CORDB, LL_INFO1000, "D::SendIPCEvent %s to outofproc appD 0x%p,\n",
            IPCENames::GetName(pManagedEvent->type),
            VmPtrToCookie(pManagedEvent->vmAppDomain));

    // increase the debug counter
    DbgLog((DebuggerIPCEventType)(pManagedEvent->type & DB_IPCE_TYPE_MASK));

    g_pDebugger->SendRawEvent(pManagedEvent);

    return hr;
}

//
// Return true if the helper thread is up & running
//
bool DebuggerRCThread::IsRCThreadReady()
{
    LIMITED_METHOD_CONTRACT;

    if (GetDCB() == NULL)
    {
        return false;
    }

    int idHelper = GetDCB()->m_helperThreadId;

    // The simplest check. If the threadid isn't set, we're not ready.
    if (idHelper == 0)
    {
        LOG((LF_CORDB, LL_EVERYTHING, "DRCT::IsReady - Helper not ready since DCB says id = 0.\n"));
        return false;
    }

    // a more subtle check. It's possible the thread was up, but then
    // an bad call to ExitProcess suddenly terminated the helper thread,
    // leaving the threadid still non-0. So check the actual thread object
    // and make sure it's still around.
    int ret = WaitForSingleObject(m_thread, 0);
    LOG((LF_CORDB, LL_EVERYTHING, "DRCT::IsReady - wait(0x%p)=%d, GetLastError() = %d\n", m_thread, ret, GetLastError()));

    if (ret != WAIT_TIMEOUT)
    {
        return false;
    }

    return true;
}


HRESULT DebuggerRCThread::ReDaclEvents(PSECURITY_DESCRIPTOR pSecurityDescriptor)
{
    LIMITED_METHOD_CONTRACT;

#ifndef TARGET_UNIX
    if (m_pDCB != NULL)
    {
        if (m_pDCB->m_rightSideEventAvailable)
        {
            if (SetKernelObjectSecurity(m_pDCB->m_rightSideEventAvailable,
                DACL_SECURITY_INFORMATION,
                pSecurityDescriptor) == 0)
            {
                // failed!
                return HRESULT_FROM_GetLastError();
            }
        }
        if (m_pDCB->m_rightSideEventRead)
        {
            if (SetKernelObjectSecurity(m_pDCB->m_rightSideEventRead,
                DACL_SECURITY_INFORMATION,
                pSecurityDescriptor) == 0)
            {
                // failed!
                return HRESULT_FROM_GetLastError();
            }
        }
    }
#endif // TARGET_UNIX

    return S_OK;
}


//
// A normal thread may hit a stack overflow and so we want to do
// any stack-intensive work on the Helper thread so that we don't
// use up the grace memory.
// Note that DoFavor will block until the fp is executed
//
void DebuggerRCThread::DoFavor(FAVORCALLBACK fp, void * pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;

        PRECONDITION(!ThisIsHelperThreadWorker());

#ifdef PREFAST
        // Prefast issue
        // error C2664: 'CHECK CheckPointer(TypeHandle,IsNullOK)' : cannot convert parameter 1 from
        //  'DebuggerRCThread::FAVORCALLBACK' to 'TypeHandle'
#else
        PRECONDITION(CheckPointer(fp));
        PRECONDITION(CheckPointer(pData, NULL_OK));
#endif
    }
    CONTRACTL_END;

    // We are being called on managed thread only.
    //

    // We'll have problems if another thread comes in and
    // deletes the RCThread object on us while we're in this call.
    if (IsRCThreadReady())
    {
        // If the helper thread calls this, we deadlock.
        // (Since we wait on an event that only the helper thread sets)
        _ASSERTE(GetRCThreadId() != GetCurrentThreadId());

        // Only lock if we're waiting on the helper thread.
        // This should be the only place the FavorLock is used.
        // Note this is never called on the helper thread.
        CrstHolder  ch(GetFavorLock());

        SetFavorFnPtr(fp, pData);

        // Our main message loop operating on the Helper thread will
        // pickup that event, call the fp, and set the Read event
        SetEvent(GetFavorAvailableEvent());

        LOG((LF_CORDB, LL_INFO10000, "DRCT::DF - Waiting on FavorReadEvent for favor 0x%08x\n", fp));

        // Wait for either the FavorEventRead to be set (which means that the favor
        // was executed by the helper thread) or the helper thread's handle (which means
        // that the helper thread exited without doing the favor, so we should do it)
        //
        // Note we are assuming that there's only 2 ways the helper thread can exit:
        // 1) Someone calls ::ExitProcess, killing all threads. That will kill us too, so we're "ok".
        // 2) Someone calls Stop(), causing the helper to exit gracefully. That's ok too. The helper
        // didn't execute the Favor (else the FREvent would have been set first) and so we can.
        //
        // Beware of problems:
        // 1) If the helper can block, we may deadlock.
        // 2) If the helper can exit magically (or if we change the Wait to include a timeout) ,
        // the helper thread may have not executed the favor, partially executed the favor,
        // or totally executed the favor but not yet signaled the FavorReadEvent. We don't
        // know what it did, so we don't know what we can do; so we're in an unstable state.

        const HANDLE waitset [] = { GetFavorReadEvent(), m_thread };

        // the favor worker thread will require a transition to cooperative mode in order to complete its work and we will
        // wait for the favor to complete before terminating the process.  if there is a GC in progress the favor thread
        // will be blocked and if the thread requesting the favor is in cooperative mode we'll deadlock, so we switch to
        // preemptive mode before waiting for the favor to complete (see Dev11 72349).
        GCX_PREEMP();

        DWORD ret = WaitForMultipleObjectsEx(
            ARRAY_SIZE(waitset),
            waitset,
            FALSE,
            INFINITE,
            FALSE
        );

        DWORD wn = (ret - WAIT_OBJECT_0);
        if (wn == 0) // m_FavorEventRead
        {
            // Favor was executed, nothing to do here.
            LOG((LF_CORDB, LL_INFO10000, "DRCT::DF - favor 0x%08x finished, ret = %d\n", fp, ret));
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "DRCT::DF - lost helper thread during wait, "
                "doing favor 0x%08x on current thread\n", fp));

            // Since we have no timeout, we shouldn't be able to get an error on the wait,
            // but just in case ...
            _ASSERTE(ret != WAIT_FAILED);
            _ASSERTE((wn == 1) && !"DoFavor - unexpected return from WFMO");

            // Thread exited without doing favor, so execute it on our thread.
            // If we're here because of a stack overflow, this may push us over the edge,
            // but there's nothing else we can really do
            (*fp)(pData);

            ResetEvent(GetFavorAvailableEvent());
        }

        // m_fpFavor & m_pFavorData are meaningless now. We could set them
        // to NULL, but we may as well leave them as is to leave a trail.

    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "DRCT::DF - helper thread not ready, "
            "doing favor 0x%08x on current thread\n", fp));
        // If helper isn't ready yet, go ahead and execute the favor
        // on the callee's space
        (*fp)(pData);
    }

    // Drop a log message so that we know if we survived a stack overflow or not
    LOG((LF_CORDB, LL_INFO10000, "DRCT::DF - Favor 0x%08x completed successfully\n", fp));
}


//
// SendIPCReply simply indicates to the Right Side that a reply to a
// two-way event is ready to be read and that the last event sent from
// the Right Side has been fully processed.
//
// NOTE: this assumes that the event receive buffer has been properly
// filled in. All it does it wake up the DI and let it know that its
// safe to copy the event out of this process.
//
HRESULT DebuggerRCThread::SendIPCReply()
{
    HRESULT hr = S_OK;

#ifdef LOGGING
    DebuggerIPCEvent* event = GetIPCEventReceiveBuffer();

    LOG((LF_CORDB, LL_INFO10000, "D::SIPCR: replying with %s.\n",
         IPCENames::GetName(event->type)));
#endif

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    BOOL succ = SetEvent(m_pDCB->m_rightSideEventRead);
    if (!succ)
    {
        hr = CORDBDebuggerSetUnrecoverableWin32Error(m_debugger, 0, false);
    }
#else // !FEATURE_DBGIPC_TRANSPORT_VM
    hr = g_pDbgTransport->SendEvent(GetIPCEventReceiveBuffer());
    if (FAILED(hr))
    {
        m_debugger->UnrecoverableError(hr,
            0,
            __FILE__,
            __LINE__,
            false);
    }
#endif // !FEATURE_DBGIPC_TRANSPORT_VM

    return hr;
}

//
// EarlyHelperThreadDeath handles the case where the helper
// thread has been ripped out from underneath of us by
// ExitProcess or TerminateProcess. These calls are bad, whacking
// all threads except the caller in the process. This can happen, for
// instance, when an app calls ExitProcess. All threads are wacked,
// the main thread calls all DLL main's, and the EE starts shutting
// down in its DLL main with the helper thread terminated.
//
void DebuggerRCThread::EarlyHelperThreadDeath(void)
{
    LOG((LF_CORDB, LL_INFO10000, "DRCT::EHTD\n"));

    // If we ever spun up a thread...
    if (m_thread != NULL && m_pDCB)
    {
        Debugger::DebuggerLockHolder debugLockHolder(m_debugger);

        m_pDCB->m_helperThreadId = 0;

        LOG((LF_CORDB, LL_INFO10000, "DRCT::EHTD helperThreadId\n"));
        // dbgLockHolder goes out of scope - implicit Release
    }
}

