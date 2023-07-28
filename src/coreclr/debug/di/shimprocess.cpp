// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: ShimProcess.cpp
//

//
// The V3 ICD debugging APIs have a lower abstraction level than V2.
// This provides V2 ICD debugging functionality on top of the V3 debugger object.
//*****************************************************************************

#include "stdafx.h"

#include "safewrap.h"
#include "check.h"

#include <limits.h>
#include "shimpriv.h"

//---------------------------------------------------------------------------------------
//
// Ctor for a ShimProcess
//
// Notes:
//    See InitializeDataTarget in header for details of how to instantiate a ShimProcess and hook it up.
//    Initial ref count is 0. This is the convention used int the RS, and it plays well with semantics
//    like immediately assigning to a smart pointer (which will bump the count up to 1).

ShimProcess::ShimProcess() :
    m_ref(0),
    m_fFirstManagedEvent(false),
    m_fInCreateProcess(false),
    m_fInLoadModule(false),
    m_fIsInteropDebugging(false),
    m_fIsDisposed(false),
    m_loaderBPReceived(false)
{
    m_ShimLock.Init("ShimLock", RSLock::cLockReentrant, RSLock::LL_SHIM_LOCK);
    m_ShimProcessDisposeLock.Init(
        "ShimProcessDisposeLock",
        RSLock::cLockReentrant | RSLock::cLockNonDbgApi,
        RSLock::LL_SHIM_PROCESS_DISPOSE_LOCK);
    m_eventQueue.Init(&m_ShimLock);
    m_pShimCallback.Assign(new ShimProxyCallback(this)); // Throws

    m_fNeedFakeAttachEvents = false;
    m_ContinueStatusChangedData.Clear();

    m_pShimStackWalkHashTable = new ShimStackWalkHashTable();

    m_pDupeEventsHashTable = new DuplicateCreationEventsHashTable();

    m_machineInfo.Clear();

    m_markAttachPendingEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL);
    if (m_markAttachPendingEvent == NULL)
    {
        ThrowLastError();
    }

    m_terminatingEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL);
    if (m_terminatingEvent == NULL)
    {
        ThrowLastError();
    }
}

//---------------------------------------------------------------------------------------
//
// ShimProcess dtor. Invoked when reference count goes to 0.
//
// Assumptions:
//    Dtors should not do any interesting work. If this object has been initialized,
//    then call Dispose() first.
//
//
ShimProcess::~ShimProcess()
{
    // Expected that this was either already disposed first, or not initialized.
    _ASSERTE(m_pWin32EventThread == NULL);

    _ASSERTE(m_ShimProcessDisposeLock.IsInit());
    m_ShimProcessDisposeLock.Destroy();

    if (m_markAttachPendingEvent != NULL)
    {
        CloseHandle(m_markAttachPendingEvent);
        m_markAttachPendingEvent = NULL;
    }

    if (m_terminatingEvent != NULL)
    {
        CloseHandle(m_terminatingEvent);
        m_terminatingEvent = NULL;
    }

    // Dtor will release m_pLiveDataTarget
}

//---------------------------------------------------------------------------------------
//
// Part of initialization to hook up to process.
//
// Arguments:
//      pProcess - debuggee object to connect to. Maybe null if part of shutdown.
//
// Notes:
//     This will take a strong reference to the process object.
//     This is part of the initialization phase.
//     This should only be called once.
//
//
void ShimProcess::SetProcess(ICorDebugProcess * pProcess)
{
    PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(NULL);

    // Data-target should already be setup before we try to connect to a process.
    _ASSERTE(m_pLiveDataTarget != NULL);

    // Reference is kept by m_pProcess;
    m_pIProcess.Assign(pProcess);

    // Get the private shim hooks. This just exists to access private functionality that has not
    // yet been promoted to the ICorDebug interfaces.
    m_pProcess = static_cast<CordbProcess *>(pProcess);

    if (pProcess != NULL)
    {
        // Verify that DataTarget + new process have the same pid?
        _ASSERTE(m_pProcess->GetProcessDescriptor()->m_Pid == m_pLiveDataTarget->GetPid());
    }
}

//---------------------------------------------------------------------------------------
//
// Create a Data-Target around the live process.
//
// Arguments:
//      processId - OS process ID to connect to. Must be a local, same platform, process.
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    This is part of the initialization dance.
//
// Notes:
//    Only call this once, during the initialization dance.
//
HRESULT ShimProcess::InitializeDataTarget(const ProcessDescriptor * pProcessDescriptor)
{
    _ASSERTE(m_pLiveDataTarget == NULL);


    HRESULT hr = BuildPlatformSpecificDataTarget(GetMachineInfo(), pProcessDescriptor, &m_pLiveDataTarget);
    if (FAILED(hr))
    {
        _ASSERTE(m_pLiveDataTarget == NULL);
        return hr;
    }
    m_pLiveDataTarget->HookContinueStatusChanged(ShimProcess::ContinueStatusChanged, this);

    // Ref on pDataTarget is now 1.
    _ASSERTE(m_pLiveDataTarget != NULL);

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Determines if current thread is the Win32 Event Thread
//
// Return Value:
//    True iff current thread is win32 event thread, else false.
//
// Notes:
//    The win32 event thread is created by code:ShimProcess::CreateAndStartWin32ET
//
bool ShimProcess::IsWin32EventThread()
{
    return (m_pWin32EventThread != NULL) && m_pWin32EventThread->IsWin32EventThread();
}

//---------------------------------------------------------------------------------------
//
// Add a reference
//
void ShimProcess::AddRef()
{
    InterlockedIncrement(&m_ref);
}

//---------------------------------------------------------------------------------------
//
// Release a reference.
//
// Notes:
//     When ref goes to 0, object is deleted.
//
void ShimProcess::Release()
{
    LONG ref = InterlockedDecrement(&m_ref);
    if (ref == 0)
    {
        delete this;
    }
}

//---------------------------------------------------------------------------------------
//
// Dispose (Neuter) the object.
//
//
// Assumptions:
//    This is called to gracefully shutdown the ShimProcess object.
//    This must be called before destruction if the object was initialized.
//
// Notes:
//    This will release all external resources, including getting the win32 event thread to exit.
//    This can safely be called multiple times.
//
void ShimProcess::Dispose()
{
    // Serialize Dispose with any other locked access to the shim.  This helps
    // protect against the debugger detaching while we're in the middle of
    // doing stuff on the ShimProcess
    RSLockHolder lockHolder(&m_ShimProcessDisposeLock);

    m_fIsDisposed = true;

    // Can't shut down the W32ET if we're on it.
    _ASSERTE(!IsWin32EventThread());

    m_eventQueue.DeleteAll();

    if (m_pWin32EventThread != NULL)
    {
        // This will block waiting for the thread to exit gracefully.
        m_pWin32EventThread->Stop();

        delete m_pWin32EventThread;
        m_pWin32EventThread = NULL;
    }

    if (m_pLiveDataTarget != NULL)
    {
        m_pLiveDataTarget->Dispose();
        m_pLiveDataTarget.Clear();
    }

    m_pIProcess.Clear();
    m_pProcess = NULL;

    _ASSERTE(m_ShimLock.IsInit());
    m_ShimLock.Destroy();

    if (m_pShimStackWalkHashTable != NULL)
    {
        // The hash table should be empty by now.  ClearAllShimStackWalk() should have been called.
        _ASSERTE(m_pShimStackWalkHashTable->GetCount() == 0);

        delete m_pShimStackWalkHashTable;
        m_pShimStackWalkHashTable = NULL;
    }

    if (m_pDupeEventsHashTable != NULL)
    {
        if (m_pDupeEventsHashTable->GetCount() > 0)
        {
            // loop through all the entries in the hash table, remove them, and delete them
            for (DuplicateCreationEventsHashTable::Iterator pCurElem = m_pDupeEventsHashTable->Begin(),
                pEndElem = m_pDupeEventsHashTable->End();
                pCurElem != pEndElem;
            pCurElem++)
            {
                DuplicateCreationEventEntry * pEntry = *pCurElem;
                delete pEntry;
            }
            m_pDupeEventsHashTable->RemoveAll();
        }

        delete m_pDupeEventsHashTable;
        m_pDupeEventsHashTable = NULL;
    }
}



//---------------------------------------------------------------------------------------
// Track (and close) file handles from debug events.
//
// Arguments:
//    pEvent - debug event
//
// Notes:
//    Some debug events introduce file handles that the debugger needs to track and
//    close on other debug events. For example, the LoadDll,CreateProcess debug
//    events both give back a file handle that the debugger must close. This is generally
//    done on the corresponding UnloadDll/ExitProcess debug events.
//
//    Since we won't use the file handles, we'll just close them as soon as we get them.
//    That way, we don't need to remember any state.
void ShimProcess::TrackFileHandleForDebugEvent(const DEBUG_EVENT * pEvent)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    HANDLE        hFile = NULL;

    switch(pEvent->dwDebugEventCode)
    {
        //
        // Events that add a file handle
        //
        case CREATE_PROCESS_DEBUG_EVENT:
            hFile = pEvent->u.CreateProcessInfo.hFile;
            CloseHandle(hFile);
            break;

        case LOAD_DLL_DEBUG_EVENT:
            hFile = pEvent->u.LoadDll.hFile;
            CloseHandle(hFile);
            break;

    }
}

//---------------------------------------------------------------------------------------
// ThreadProc helper to drain event queue.
//
// Arguments:
//    parameter - thread proc parameter, an ICorDebugProcess*
//
// Returns
//     0.
//
// Notes:
//    This is useful when the shim queued a fake managed event (such as Control+C)
//    and needs to get the debuggee to synchronize in order to start dispatching events.
//    @dbgtodo sync: this will likely change as we iron out the Synchronization feature crew.
//
//    We do this in a new thread proc to avoid thread restrictions:
//    Can't call this on win32 event thread because that can't send the IPC event to
//    make the async-break request.
//    Can't call this on the RCET because that can't send an async-break (see SendIPCEvent for details)
//    So we just spin up a new thread to do the work.
//---------------------------------------------------------------------------------------
DWORD WINAPI CallStopGoThreadProc(LPVOID parameter)
{
    ICorDebugProcess* pProc = reinterpret_cast<ICorDebugProcess *>(parameter);

    // We expect these operations to succeed; but if they do fail, there's nothing we can really do about it.
    // If it fails on process exit/neuter/detach, then it would be ignorable.
    HRESULT hr;


    // Calling Stop + Continue will synchronize the process and force any queued events to be called.
    // Stop is synchronous and will block until debuggee is synchronized.
    hr = pProc->Stop(INFINITE);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    // Continue will resume the debuggee. If there are queued events (which we expect in this case)
    // then continue will drain the event queue instead of actually resuming the process.
    hr = pProc->Continue(FALSE);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    // This thread just needs to trigger an event dispatch. Now that it's done that, it can exit.
    return 0;
}


//---------------------------------------------------------------------------------------
// Does default event handling for native debug events.
//
// Arguments:
//  pEvent - IN event ot handle
//  pdwContinueStatus - IN /OUT - continuation status for event.
//
// Assumptions:
//    Called when target is stopped. Caller still needs to Continue the debug event.
//    This is called on the win32 event thread.
//
// Notes:
//    Some native events require extra work before continuing. Eg, skip loader
//    breakpoint, close certain handles, etc.
//    This is only called in the manage-only case. In the interop-case, the
//    debugger will get and handle these native debug events.
void ShimProcess::DefaultEventHandler(
    const DEBUG_EVENT * pEvent,
    DWORD * pdwContinueStatus)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;


    //
    // Loader breakpoint
    //

    BOOL fFirstChance;
    const EXCEPTION_RECORD * pRecord = NULL;

    if (IsExceptionEvent(pEvent, &fFirstChance, &pRecord))
    {
        DWORD dwThreadId = GetThreadId(pEvent);

        switch(pRecord->ExceptionCode)
        {
        case STATUS_BREAKPOINT:
            {
                if (!m_loaderBPReceived)
                {
                    m_loaderBPReceived = true;

                    // Clear the loader breakpoint
                    *pdwContinueStatus = DBG_CONTINUE;

                    // After loader-breakpoint, notify that managed attach can begin.
                    // This is done to trigger a synchronization. The shim
                    // can then send the fake attach events once the target
                    // is synced.
                    // @dbgtodo sync: not needed once shim can
                    // work on sync APIs.
                    m_pProcess->QueueManagedAttachIfNeeded(); // throws
                }
            }
            break;

        /*
        // If we handle the Ctlr-C event here and send the notification to the debugger, then we may break pre-V4
        // behaviour because the debugger may handle the event and intercept the handlers registered in the debuggee
        // process.  So don't handle the event here and let the debuggee process handle it instead.  See Dev10 issue
        // 846455 for more info.
        //
        // However, when the re-arch is completed, we will need to work with VS to define what the right behaviour
        // should be.  We don't want to rely on in-process code to handle the Ctrl-C event.
        case DBG_CONTROL_C:
        {
        // Queue a fake managed Ctrl+C event.
        m_pShimCallback->ControlCTrap(GetProcess());

        // Request an Async Break
        // This is on Win32 Event Thread, so we can't call Stop / Continue.
        // Instead, spawn a new threead, and have that call Stop/Continue, which
        // will get the RCET to drain the event queue and dispatch the ControlCTrap we just queued.
        {
        DWORD dwDummyId;
        CreateThread(NULL,
        0,
        CallStopGoThreadProc,
        (LPVOID) GetProcess(),
        0,
        &dwDummyId);
        }

        // We don't worry about suspending the Control-C thread right now. The event is
        // coming asynchronously, and so it's ok if the debuggee slips forward while
        // we try to do a managed async break.


        // Clear the control-C event.
        *pdwContinueStatus = DBG_CONTINUE;
        }
        break;

*/
        }


    }


    // Native debugging APIs have an undocumented expectation that you clear for OutputDebugString.
    if (pEvent->dwDebugEventCode == OUTPUT_DEBUG_STRING_EVENT)
    {
        *pdwContinueStatus = DBG_CONTINUE;
    }

    //
    // File handles.
    //
    TrackFileHandleForDebugEvent(pEvent);
}

//---------------------------------------------------------------------------------------
// Determine if we need to change the continue status
//
// Returns:
//    True if the continue status was changed. Else false.
//
// Assumptions:
//    This is single-threaded, which is enforced by it only be called on the win32et.
//    The shim guarnatees only 1 outstanding debug-event at a time.
//
// Notes:
//    See code:ShimProcess::ContinueStatusChangedWorker for big picture.
//    Continue status is changed from a data-target callback which invokes
//    code:ShimProcess::ContinueStatusChangedWorker.
//    Call code:ShimProcess::ContinueStatusChangedData::Clear to clear the 'IsSet' bit.
//
bool ShimProcess::ContinueStatusChangedData::IsSet()
{

    return m_dwThreadId != 0;
}

//---------------------------------------------------------------------------------------
// Clears the bit marking
//
// Assumptions:
//    This is single-threaded, which is enforced by it only be called on the win32et.
//    The shim guarantees only 1 outstanding debug-event at a time.
//
// Notes:
//    See code:ShimProcess::ContinueStatusChangedWorker for big picture.
//    This makes code:ShimProcess::ContinueStatusChangedData::IsSet return false.
//    This can safely be called multiple times in a row.
//
void ShimProcess::ContinueStatusChangedData::Clear()
{
    m_dwThreadId = 0;
}

//---------------------------------------------------------------------------------------
// Callback invoked from data-target when continue status is changed.
//
// Arguments:
//    pUserData - data we supplied to the callback. a 'this' pointer.
//    dwThreadId - the tid whose continue status is changing
//    dwContinueStatus - the new continue status.
//
// Notes:
//

// Static
HRESULT ShimProcess::ContinueStatusChanged(void * pUserData, DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus)
{
    ShimProcess * pThis = reinterpret_cast<ShimProcess *>(pUserData);
    return pThis->ContinueStatusChangedWorker(dwThreadId, dwContinueStatus);
}

//---------------------------------------------------------------------------------------
// Real worker callback invoked from data-target when continue status is changed.
//
// Arguments:
//    dwThreadId - the tid whose continue status is changing
//    dwContinueStatus - the new continue status.
//
// Notes:
//    ICorDebugProcess4::Filter returns an initial continue status (heavily biased to 'gn').
//    Some ICorDebug operations may need to change the continue status that filter returned.
//    For example, on windows, hijacking a thread at an unhandled exception would need to
//    change the status to 'gh' (since continuing 2nd chance exception 'gn' will tear down the
//    process and the hijack would never execute).
//
//    Such operations will invoke into the data-target (code:ICorDebugMutableDataTarget::ContinueStatusChanged)
//    to notify the debugger that the continue status was changed.
//
//    The shim only executes such operations on the win32-event thread in a small window between
//    WaitForDebugEvent and Continue. Therefore, we know:
//    * the callback must come on the Win32EventThread (which means our handling the callback is
//       single-threaded.
//    * We only have 1 outstanding debug event to worry about at a time. This simplifies our tracking.
//
//    The shim tracks the outstanding change request in m_ContinueStatusChangedData.

HRESULT ShimProcess::ContinueStatusChangedWorker(DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus)
{
    // Should only be set once. This is only called on the win32 event thread, which protects against races.
    _ASSERTE(IsWin32EventThread());
    _ASSERTE(!m_ContinueStatusChangedData.IsSet());

    m_ContinueStatusChangedData.m_dwThreadId = dwThreadId;
    m_ContinueStatusChangedData.m_status     = dwContinueStatus;

    // Setting dwThreadId to non-zero should now mark this as set.
    _ASSERTE(m_ContinueStatusChangedData.IsSet());
    return S_OK;
}


//---------------------------------------------------------------------------------------
//
// Add a duplicate creation event entry for the specified key.
//
// Arguments:
//    pKey - the key of the entry to be added; this is expected to be an
//           ICDProcess/ICDAppDomain/ICDThread/ICDAssembly/ICDModule
//
// Assumptions:
//    pKey is really an interface pointer of one of the types mentioned above
//
// Notes:
//    We have to keep track of which creation events we have sent already because some runtime data structures
//    are discoverable through enumeration before they send their creation events.  As a result, we may have
//    faked up a creation event for a data structure during attach, and then later on get another creation
//    event for the same data structure.  VS is not resilient in the face of multiple creation events for
//    the same data structure.
//
//    Needless to say this is a problem in attach scenarios only.  However, keep in mind that for CoreCLR,
//    launch really is early attach.  For early attach, we get three creation events up front: a create
//    process, a create appdomain, and a create thread.
//

void ShimProcess::AddDuplicateCreationEvent(void * pKey)
{
    NewHolder<DuplicateCreationEventEntry> pEntry(new DuplicateCreationEventEntry(pKey));
    m_pDupeEventsHashTable->Add(pEntry);
    pEntry.SuppressRelease();
}


//---------------------------------------------------------------------------------------
//
// Check whether the specified key exists in the hash table.  If so, remove it.
//
// Arguments:
//    pKey - the key of the entry to check; this is expected to be an
//           ICDProcess/ICDAppDomain/ICDThread/ICDAssembly/ICDModule
//
// Return Value:
//    Returns true if the entry exists.  The entry will have been removed because we can't have more than two
//    duplicates for any given event.
//
// Assumptions:
//    pKey is really an interface pointer of one of the types mentioned above
//
// Notes:
//    See code:ShimProcess::AddDuplicateCreationEvent.
//

bool ShimProcess::RemoveDuplicateCreationEventIfPresent(void * pKey)
{
    // We only worry about duplicate events in attach scenarios.
    if (GetAttached())
    {
        // Only do the check if the hash table actually contains entries.
        if (m_pDupeEventsHashTable->GetCount() > 0)
        {
            // Check if this is a dupe.
            DuplicateCreationEventEntry * pResult = m_pDupeEventsHashTable->Lookup(pKey);
            if (pResult != NULL)
            {
                // This is a dupe.  We can't have a dupe twice, so remove it.
                // This will help as a bit of optimization, since we will no longer check the hash table if
                // its count reaches 0.
                m_pDupeEventsHashTable->Remove(pKey);
                delete pResult;
                return true;
            }
        }
    }
    return false;
}


//---------------------------------------------------------------------------------------
// Gets the exception record format of the host
//
// Returns:
//    The CorDebugRecordFormat for the host architecture.
//
// Notes:
//   This corresponds to the definition EXCEPTION_RECORD on the host-architecture.
//   It can be passed into ICorDebugProcess4::Filter.
CorDebugRecordFormat GetHostExceptionRecordFormat()
{
#if defined(HOST_64BIT)
    return FORMAT_WINDOWS_EXCEPTIONRECORD64;
#else
    return FORMAT_WINDOWS_EXCEPTIONRECORD32;
#endif
}

//---------------------------------------------------------------------------------------
// Main event handler for native debug events. Must also ensure Continue is called.
//
// Arguments:
//   pEvent - debug event to handle
//
// Assumptions:
//   Caller did a Flush() if needed.
//
// Notes:
//   The main Handle native debug events.
//   This must call back into ICD to let ICD filter the debug event (in case it's a managed notification).
//
//   If we're interop-debugging (V2), then the debugger is expecting the debug events. In that case,
//   we go through the V2 interop-debugging logic to queue / dispatch the events.
//   If we're managed-only debugging, then the shim provides a default handler for the native debug.
//   This includes some basic work (skipping the loader breakpoint, close certain handles, etc).
//---------------------------------------------------------------------------------------
HRESULT ShimProcess::HandleWin32DebugEvent(const DEBUG_EVENT * pEvent)
{
    _ASSERTE(IsWin32EventThread());

    //
    // If this is an exception event, then we need to feed it into the CLR.
    //
    BOOL dwFirstChance = FALSE;
    const EXCEPTION_RECORD * pRecord = NULL;
    const DWORD dwThreadId = GetThreadId(pEvent);

    bool fContinueNow = true;

    // If true, we're continuing (unhandled) a 2nd-chance exception
    bool fExceptionGoingUnhandled = false;

    //
    const DWORD kDONTCARE = 0;
    DWORD dwContinueStatus = kDONTCARE;

    if (IsExceptionEvent(pEvent, &dwFirstChance, &pRecord))
    {
        // As a diagnostic aid we can configure the debugger to assert when the debuggee does DebugBreak()
#ifdef DEBUG
        static ConfigDWORD config;
        DWORD fAssert = config.val(CLRConfig::INTERNAL_DbgAssertOnDebuggeeDebugBreak);
        if (fAssert)
        {
            // If we got a 2nd-chance breakpoint, then it's extremely likely that it's from an
            // _ASSERTE in the target and we really want to know about it now before we kill the
            // target. The debuggee will exit once we continue (unless we are mixed-mode debugging), so alert now.
            // This assert could be our only warning of various catastrophic failures in the left-side.
            if (!dwFirstChance && (pRecord->ExceptionCode == STATUS_BREAKPOINT) && !m_fIsInteropDebugging)
            {
                DWORD pid = (m_pLiveDataTarget == NULL) ? 0 : m_pLiveDataTarget->GetPid();

                CONSISTENCY_CHECK_MSGF(false,
                    ("Unhandled breakpoint exception in debuggee (pid=%d (0x%x)) on thread %d(0x%x)\n"
                    "This may mean there was an assert in the debuggee on that thread.\n"
                    "\n"
                    "You should attach to that process (non-invasively) and get a callstack of that thread.\n"
                    "(This assert only occurs when CLRConfig::INTERNAL_DebuggerAssertOnDebuggeeDebugBreak is set)\n",
                    pid, pid, dwThreadId,dwThreadId));
            }
        }
#endif

        // We pass the Shim's proxy callback object, which will just take the callbacks and queue them
        // to an event-queue in the shim. When we get the sync-complete event, the shim
        // will then drain the event queue and dispatch the events to the user's callback object.
        const DWORD dwFlags = dwFirstChance ? 1 : 0;

        m_ContinueStatusChangedData.Clear();

        // If ICorDebug doesn't care about this exception, it will leave dwContinueStatus unchanged.
        RSExtSmartPtr<ICorDebugProcess4> pProcess4;
        GetProcess()->QueryInterface(IID_ICorDebugProcess4, (void**) &pProcess4);

        HRESULT hrFilter =  pProcess4->Filter(
            (const BYTE*) pRecord,
            sizeof(EXCEPTION_RECORD),
            GetHostExceptionRecordFormat(),
            dwFlags,
            dwThreadId,
            m_pShimCallback,
            &dwContinueStatus);
        if (FAILED(hrFilter))
        {
            // Filter failed (eg. DAC couldn't be loaded), return the
            // error so it can become an unrecoverable error.
            return hrFilter;
        }

        // For unhandled exceptions, hijacking if needed.
        if (!dwFirstChance)
        {
            // May invoke data-target callback (which may call code:ShimProcess::ContinueStatusChanged) to change continue status.
            if (!m_pProcess->HijackThreadForUnhandledExceptionIfNeeded(dwThreadId))
            {
                // We decided not to hijack, so this exception is going to go unhandled
                fExceptionGoingUnhandled = true;
            }

            if (m_ContinueStatusChangedData.IsSet())
            {
                _ASSERTE(m_ContinueStatusChangedData.m_dwThreadId == dwThreadId);

                // Claiming this now means we won't do any other processing on the exception event.
                // This means the interop-debugging logic will never see 2nd-chance managed exceptions.
                dwContinueStatus = m_ContinueStatusChangedData.m_status;
            }
        }
    }

    // Do standard event handling, including Handling loader-breakpoint,
    // and callback into CordbProcess for Attach if needed.
    HRESULT hrIgnore = S_OK;
    EX_TRY
    {
        // For NonClr notifications, allow extra processing.
        // This includes both non-exception events, and exception events that aren't
        // specific CLR debugging services notifications.
        if (dwContinueStatus == kDONTCARE)
        {
            if (m_fIsInteropDebugging)
            {
                // Interop-debugging logic will handle the continue.
                fContinueNow = false;
#if defined(FEATURE_INTEROP_DEBUGGING)
                // @dbgtodo interop: All the interop-debugging logic is still in CordbProcess.
                // Call back into that. This will handle Continuing the debug event.
                m_pProcess->HandleDebugEventForInteropDebugging(pEvent);
#else
                _ASSERTE(!"Interop debugging not supported");
#endif
            }
            else
            {
                dwContinueStatus = DBG_EXCEPTION_NOT_HANDLED;

                // For managed-only debugging, there's no user handler for native debug events,
                // and so we still need to do some basic work on certain debug events.
                DefaultEventHandler(pEvent, &dwContinueStatus);

                // This is the managed-only case. No reason to keep the target win32 frozen, so continue it immediately.
                _ASSERTE(fContinueNow);
            }
        }
    }
    EX_CATCH_HRESULT(hrIgnore);
    // Dont' expect errors here (but could probably return it up to become an
    // unrecoverable error if necessary). We still want to call Continue thought.
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hrIgnore);

    //
    // Continue the debuggee if needed.
    //
    if (fContinueNow)
    {
        BOOL fContinueOk = GetNativePipeline()->ContinueDebugEvent(
            GetProcessId(pEvent),
            dwThreadId,
            dwContinueStatus);
        (void)fContinueOk; //prevent "unused variable" error from GCC
        SIMPLIFYING_ASSUMPTION(fContinueOk);

        if (fExceptionGoingUnhandled)
        {
            _ASSERTE(dwContinueStatus == DBG_EXCEPTION_NOT_HANDLED);
            // We just passed a 2nd-chance exception back to the OS which may have now invoked
            // Windows error-reporting logic which suspended all threads in the target.  Since we're
            // still debugging and may want to break, inspect state and even detach (eg. to attach
            // a different sort of debugger that can handle the exception) we need to let our threads run.
            // Note that when WER auto-invokes a debugger it doesn't suspend threads, so it doesn't really
            // make sense for them to be suspended now when a debugger is already attached.
            // A better solution may be to suspend this faulting thread before continuing the event, do an
            // async-break and give the debugger a notification of an unhandled exception.  But this will require
            // an ICorDebug API change, and also makes it harder to reliably get the WER dialog box once we're
            // ready for it.
            // Unfortunately we have to wait for WerFault.exe to start and actually suspend the threads, and
            // there doesn't appear to be any better way than to just sleep for a little here.  In practice 200ms
            // seems like more than enough, but this is so uncommon of a scenario that a half-second delay
            // (just to be safe) isn't a problem.
            // Provide an undocumented knob to turn this behavior off in the very rare case it's not what we want
            // (eg. we're trying to debug something that races with crashing / terminating the process on multiple
            // threads)
            static ConfigDWORD config;
            DWORD fSkipResume = config.val(CLRConfig::UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException);
            if (!fSkipResume)
            {
                ::Sleep(500);
            }
        }
    }

    return S_OK;
}

// Trivial accessor to get the event queue.
ManagedEventQueue * ShimProcess::GetManagedEventQueue()
{
    return &m_eventQueue;
}

// Combines GetManagedEventQueue() and Dequeue() into a single function
// that holds m_ShimProcessDisposeLock for the duration
ManagedEvent * ShimProcess::DequeueManagedEvent()
{
    // Serialize this function with Dispoe()
    RSLockHolder lockHolder(&m_ShimProcessDisposeLock);
    if (m_fIsDisposed)
        return NULL;

    return m_eventQueue.Dequeue();
}

// Trivial accessor to get Shim's proxy callback object.
ShimProxyCallback * ShimProcess::GetShimCallback()
{
    return m_pShimCallback;
}

// Trivial accessor to get the ICDProcess for the debuggee.
// A ShimProcess object can then provide V2 functionality by building it on V3 functionality
// exposed by the ICDProcess object.
ICorDebugProcess * ShimProcess::GetProcess()
{
    return m_pIProcess;
}

// Trivial accessor to get the data-target for the debuggee.
// The data-target lets us access the debuggee, especially reading debuggee memory.
ICorDebugMutableDataTarget * ShimProcess::GetDataTarget()
{
    return m_pLiveDataTarget;
};


// Trivial accessor to get the raw native event pipeline.
// In V3, ICorDebug no longer owns the event thread and it does not own the event pipeline either.
INativeEventPipeline * ShimProcess::GetNativePipeline()
{
    return m_pWin32EventThread->GetNativePipeline();
}

// Trivial accessor to expose the W32ET thread to the CordbProcess so that it can emulate V2 behavior.
// In V3, ICorDebug no longer owns the event thread and it does not own the event pipeline either.
// The Win32 Event Thread is the only thread that can use the native pipeline
// see code:ShimProcess::GetNativePipeline.
CordbWin32EventThread * ShimProcess::GetWin32EventThread()
{
    return m_pWin32EventThread;
}


// Trivial accessor to mark whether we're interop-debugging.
// Retrieved via code:ShimProcess::IsInteropDebugging
void ShimProcess::SetIsInteropDebugging(bool fIsInteropDebugging)
{
    m_fIsInteropDebugging = fIsInteropDebugging;
}

// Trivial accessor to check if we're interop-debugging.
// This affects how we handle native debug events.
// The significant usage of this is in code:ShimProcess::HandleWin32DebugEvent
bool ShimProcess::IsInteropDebugging()
{
    return m_fIsInteropDebugging;
}


//---------------------------------------------------------------------------------------
// Begin queueing the fake attach events.
//
// Notes:
//    See code:ShimProcess::QueueFakeAttachEvents for more about "fake attach events".
//
//    This marks that we need to send fake attach events, and queus a CreateProcess.
//    Caller calls code:ShimProcess::QueueFakeAttachEventsIfNeeded to finish queuing
//    the rest of the fake attach events.
void ShimProcess::BeginQueueFakeAttachEvents()
{
    m_fNeedFakeAttachEvents = true;

    // Put a fake CreateProcess event in the queue.
    // This will not be drained until we get a Sync-Complete from the Left-side.
    GetShimCallback()->QueueCreateProcess(GetProcess());
    AddDuplicateCreationEvent(GetProcess());
}

//---------------------------------------------------------------------------------------
// potentially Queue fake attach events like we did in V2.
//
// Arguments:
//   fRealCreateProcessEvent - true if the shim is about to dispatch a real create process event (as opposed
//                             to one faked up by the shim itself)
//
// Notes:
//    See code:ShimProcess::QueueFakeAttachEvents for details.
void ShimProcess::QueueFakeAttachEventsIfNeeded(bool fRealCreateProcessEvent)
{
    // This was set high in code:ShimProcess::BeginQueueFakeAttachEvents
    if (!m_fNeedFakeAttachEvents)
    {
        return;
    }
    m_fNeedFakeAttachEvents = false;

    // If the first event we get after attaching is a create process event, then this is an early attach
    // scenario and we don't need to queue any fake attach events.
    if (!fRealCreateProcessEvent)
    {
        HRESULT hr = S_OK;
        EX_TRY
        {
            QueueFakeAttachEvents();
        }
        EX_CATCH_HRESULT(hr);
    }
}

//---------------------------------------------------------------------------------------
// Send fake Thread-create events for attach, using an arbitrary order.
//
// Returns:
//    S_OK on success, else error.
//
// Notes:
//    This sends fake thread-create events, ala V2 attach.
//    See code:ShimProcess::QueueFakeAttachEvents for details
//
//    The order of thread creates is random and at the mercy of ICorDebugProcess::EnumerateThreads.
//    Whidbey would send thread creates in the order of the OS's native thread
//    list. Since Arrowhead no longer sends fake attach events, the shim simulates
//    the fake attach events. But ICorDebug doesn't provide a way to get the
//    same order that V2 used. So without using platform-specific thread-enumeration,
//    we can't get the V2 ordering.
//
HRESULT ShimProcess::QueueFakeThreadAttachEventsNoOrder()
{
    ICorDebugProcess * pProcess = GetProcess();

    RSExtSmartPtr<ICorDebugThreadEnum> pThreadEnum;
    RSExtSmartPtr<ICorDebugThread> pThread;

    // V2 would only send create threads after a thread had run managed code.
    // V3 has a discovery model where Enumeration can find threads before they've run managed code.
    // So the emulation here may send some additional create-thread events that v2 didn't send.
    HRESULT hr = pProcess->EnumerateThreads(&pThreadEnum);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    if (FAILED(hr))
    {
        return hr;
    }

    ULONG cDummy;

    while(SUCCEEDED(pThreadEnum->Next(1, &pThread, &cDummy)) && (pThread != NULL))
    {
        RSExtSmartPtr<ICorDebugAppDomain> pAppDomain;
        hr = pThread->GetAppDomain(&pAppDomain);

        // Getting the appdomain shouldn't fail. If it does, we can't dispatch
        // this callback, but we can still dispatch the other thread creates.
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
        if (pAppDomain != NULL)
        {
            GetShimCallback()->CreateThread(pAppDomain, pThread);
            AddDuplicateCreationEvent(pThread);
        }
        pThread.Clear();
    }

    return S_OK;
}

//---------------------------------------------------------------------------------------
// Queues the fake Assembly and Module load events
//
// Arguments:
//   pAssembly - non-null, the assembly to queue.
//
// Notes:
//   Helper for code:ShimProcess::QueueFakeAttachEvents
//   Queues create events for the assembly and for all modules within the
//   assembly. Most assemblies only have 1 module.
void ShimProcess::QueueFakeAssemblyAndModuleEvent(ICorDebugAssembly * pAssembly)
{
    RSExtSmartPtr<ICorDebugAppDomain> pAppDomain;

    HRESULT hr = pAssembly->GetAppDomain(&pAppDomain);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    //
    // Send the fake Load Assembly event.
    //
    GetShimCallback()->LoadAssembly(pAppDomain, pAssembly);
    AddDuplicateCreationEvent(pAssembly);

    //
    // Send Modules - must be in load order
    //
    RSExtSmartPtr<ICorDebugModuleEnum> pModuleEnum;
    hr = pAssembly->EnumerateModules(&pModuleEnum);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    ULONG countModules;
    hr = pModuleEnum->GetCount(&countModules);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    // ISSUE WORKAROUND 835869
    // The CordbEnumFilter used as the implementation of CordbAssembly::EnumerateModules has
    // a ref counting bug in it. It adds one ref to each item when it is constructed and never
    // removes that ref. Expected behavior would be that it adds a ref at construction, another on
    // every call to next, and releases the construction ref when the enumerator is destroyed. The
    // user is expected to release the reference they receive from Next. Thus enumerating exactly
    // one time and calling Release() does the correct thing regardless of whether this bug is present
    // or not. Note that with the bug the enumerator holds 0 references at the end of this loop,
    // however the assembly also holds references so the modules will not be prematurely released.
    for(ULONG i = 0; i < countModules; i++)
    {
        ICorDebugModule* pModule = NULL;
        ULONG countFetched = 0;
        pModuleEnum->Next(1, &pModule, &countFetched);
        _ASSERTE(pModule != NULL);
        if(pModule != NULL)
        {
            pModule->Release();
        }
    }

    RSExtSmartPtr<ICorDebugModule> * pModules = new RSExtSmartPtr<ICorDebugModule> [countModules];
    m_pProcess->GetModulesInLoadOrder(pAssembly, pModules, countModules);
    for(ULONG iModule = 0; iModule < countModules; iModule++)
    {
        ICorDebugModule * pModule = pModules[iModule];

        GetShimCallback()->FakeLoadModule(pAppDomain, pModule);
        AddDuplicateCreationEvent(pModule);

        // V2 may send UpdatePdbStreams for certain modules (like dynamic or in-memory modules).
        // We don't yet have this support for out-of-proc.
        // When the LoadModule event that we just queued is actually dispatched, it will
        // send an IPC event in-process that will collect the information and queue the event
        // at that time.
        // @dbgtodo : I don't think the above is true anymore - clean it up?

        RSExtSmartPtr<IStream> pSymbolStream;

        // ICorDebug has no public way to request raw symbols.  This is by-design because we
        // don't want people taking a dependency on a specific format (to give us the ability
        // to innovate for the RefEmit case).  So we must use a private hook here to get the
        // symbol data.
        CordbModule * pCordbModule = static_cast<CordbModule *>(pModule);
        IDacDbiInterface::SymbolFormat symFormat = IDacDbiInterface::kSymbolFormatNone;
        EX_TRY
        {
            symFormat = pCordbModule->GetInMemorySymbolStream(&pSymbolStream);
        }
        EX_CATCH_HRESULT(hr);
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);   // Shouldn't be any errors trying to read symbols

        // Only pass the raw symbols onto the debugger if they're in PDB format (all that was supported
        // in V2).  Note that we could have avoided creating a stream for the non-PDB case, but we'd have
        // to refactor GetInMemorySymbolStream and the perf impact should be negligable.
        if (symFormat == IDacDbiInterface::kSymbolFormatPDB)
        {
            _ASSERTE(pSymbolStream != NULL);    // symFormat should have been kSymbolFormatNone if null stream
            GetShimCallback()->UpdateModuleSymbols(pAppDomain, pModule, pSymbolStream);
        }

    }
    delete [] pModules;
}

//---------------------------------------------------------------------------------------
// Get an array of appdomains, sorted by increasing AppDomain ID
//
// Arguments:
//    pProcess - process containing the appdomains
//    ppAppDomains - array that this function will allocate to hold appdomains
//    pCount - size of ppAppDomains array
//
// Assumptions:
//    Caller must delete [] ppAppDomains
//
// Notes
//   This is used as part of code:ShimProcess::QueueFakeAttachEvents.
//   The fake attach events want appdomains in creation order. ICorDebug doesn't provide
//   this ordering in the enumerators.
//
//   This returns the appdomains sorted in order of increasing AppDomain ID, since that's the best
//   approximation of creation order that we have.
//   @dbgtodo - determine if ICD will provide
//   ordered enumerators
//
HRESULT GetSortedAppDomains(ICorDebugProcess * pProcess, RSExtSmartPtr<ICorDebugAppDomain> **ppAppDomains, ULONG * pCount)
{
    _ASSERTE(ppAppDomains != NULL);

    HRESULT hr = S_OK;
    RSExtSmartPtr<ICorDebugAppDomainEnum> pAppEnum;

    //
    // Find the size of the array to hold all the appdomains
    //
    hr = pProcess->EnumerateAppDomains(&pAppEnum);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    ULONG countAppDomains = 0;

    hr =  pAppEnum->GetCount(&countAppDomains);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

    //
    // Allocate the array
    //
    RSExtSmartPtr<ICorDebugAppDomain> * pAppDomains = new RSExtSmartPtr<ICorDebugAppDomain>[countAppDomains];
    *ppAppDomains = pAppDomains;
    *pCount = countAppDomains;

    //
    // Load all the appdomains into the array
    //
    ULONG countDummy;
    hr = pAppEnum->Next(countAppDomains, (ICorDebugAppDomain**) pAppDomains, &countDummy);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    SIMPLIFYING_ASSUMPTION(countDummy == countAppDomains);

    //
    // Now sort them based on appdomain ID.
    // We generally expect a very low number of appdomains (usually 1). So a n^2 sort shouldn't be a perf
    // problem here.
    //
    for(ULONG i = 0; i < countAppDomains; i++)
    {
        ULONG32 id1;
        hr = pAppDomains[i]->GetID(&id1);
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

        for(ULONG j = i + 1; j < countAppDomains; j++)
        {
            ULONG32 id2;
            hr = pAppDomains[j]->GetID(&id2);
            SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);

            if (id1 > id2)
            {
                // swap values
                ICorDebugAppDomain * pTemp = pAppDomains[i];
                pAppDomains[i].Assign(pAppDomains[j]);
                pAppDomains[j].Assign(pTemp);

                // update id1 key since it's in the outer-loop.
                id1 = id2;
            }
        }
    }



    return S_OK;

}

//---------------------------------------------------------------------------------------
// To emulate the V2 attach-handshake, give the shim a chance to inject fake attach events.
//
// Notes:
//   Do this before the queue is empty so that HasQueuedCallbacks() doesn't toggle from false to true.
//   This is called once the process is synchronized, which emulates V2 semantics on attach.
//   This may be called on the Win32Event Thread from inside of Filter, or on another thread.
void ShimProcess::QueueFakeAttachEvents()
{
    // Serialize this function with Dispose()
    RSLockHolder lockHolder(&m_ShimProcessDisposeLock);
    if (m_fIsDisposed)
        return;

    // The fake CreateProcess is already queued. Start queuing the rest of the events.
    // The target is stopped (synchronized) this whole time.
    // This will use the inspection API to look at the process and queue up the fake
    // events that V2 would have sent in a similar situation. All of the callbacks to GetShimCallback()
    // just queue up the events. The event queue is then drained as the V2 debugger calls continue.

    HRESULT hr = S_OK;
    ICorDebugProcess * pProcess = GetProcess();

    //
    // First, Queue all the Fake AppDomains
    //
    RSExtSmartPtr<ICorDebugAppDomain> * pAppDomains = NULL;
    ULONG countAppDomains = 0;

    hr =  GetSortedAppDomains(pProcess, &pAppDomains, &countAppDomains);
    if (FAILED(hr))
        return;

    for(ULONG i = 0; i < countAppDomains; i++)
    {
        // V2 expects that the debugger then attaches to each AppDomain during the Create-appdomain callback.
        // This was done to allow for potential per-appdomain debugging. However, only-process
        // wide debugging support was allowed in V2. The caller had to attach to all Appdomains.

        GetShimCallback()->CreateAppDomain(pProcess, pAppDomains[i]);
        AddDuplicateCreationEvent(pAppDomains[i]);
    }

    // V2 had a break in the callback queue at this point.

    //
    // Second, queue all Assembly and Modules events.
    //

    for(ULONG iAppDomain = 0; iAppDomain < countAppDomains; iAppDomain++)
    {
        ICorDebugAppDomain * pAppDomain = pAppDomains[iAppDomain];
        //
        // Send Assemblies. Must be in load order.
        //

        RSExtSmartPtr<ICorDebugAssemblyEnum> pAssemblyEnum;
        hr = pAppDomain->EnumerateAssemblies(&pAssemblyEnum);
        if (FAILED(hr))
            break;

        ULONG countAssemblies;
        hr = pAssemblyEnum->GetCount(&countAssemblies);
        if (FAILED(hr))
            break;

        RSExtSmartPtr<ICorDebugAssembly> * pAssemblies = new RSExtSmartPtr<ICorDebugAssembly> [countAssemblies];
        m_pProcess->GetAssembliesInLoadOrder(pAppDomain, pAssemblies, countAssemblies);
        for(ULONG iAssembly = 0; iAssembly < countAssemblies; iAssembly++)
        {
            QueueFakeAssemblyAndModuleEvent(pAssemblies[iAssembly]);
        }
        delete [] pAssemblies;

    }

    delete [] pAppDomains;


    // V2 would have a break in the callback queue at this point.

    // V2 would send all relevant ClassLoad events now.
    //
    // That includes class loads for all modules that:
    //   - are dynamic
    //   - subscribed to class load events via ICorDebugModule::EnableClassLoadCallbacks.
    // We don't provide Class-loads in our emulation because:
    // 1. "ClassLoad" doesn't actually mean anything here.
    // 2. We have no way of enumerating "loaded" classes in the CLR. We could use the metadata to enumerate
    //    all classes, but that's offers no value.
    // 3. ClassLoad is useful for dynamic modules to notify a debugger that the module changed and
    //    to update symbols; but the LoadModule/UpdateModule syms already do that.


    //
    // Third, Queue all Threads
    //
    // Use ICorDebug to enumerate threads. The order of managed threads may
    // not match the order the threads were created in.
    QueueFakeThreadAttachEventsNoOrder();

    // Forth, Queue all Connections.
    // Enumerate connections is not exposed through ICorDebug, so we need to go use a private hook on CordbProcess.
    m_pProcess->QueueFakeConnectionEvents();

    // For V2 jit-attach, the callback queue would also include the jit-attach event (Exception, UserBreak, MDA, etc).
    // This was explicitly in the same callback queue so that a debugger would drain it as part of draining the attach
    // events.

    // In V3, on normal attach, the VM just sends a Sync-complete event.
    // On jit-attach, the VM sends the jit-attach event and then the sync-complete.
    // The shim just queues the fake attach events at the first event it gets from the left-side.
    // In jit-attach, the shim will queue the fake events right before it queues the jit-attach event,
    // thus keeping them in the same callback queue as V2 did.

}

// Accessor for m_attached.
bool ShimProcess::GetAttached()
{
    return m_attached;
}
// We need to know whether we are in the CreateProcess callback to be able to
// return the v2.0 hresults from code:CordbProcess::SetDesiredNGENCompilerFlags
// when we are using the shim.
//
// Expose m_fInCreateProcess
bool ShimProcess::GetInCreateProcess()
{
    return m_fInCreateProcess;
}

void ShimProcess::SetInCreateProcess(bool value)
{
    m_fInCreateProcess = value;
}

// We need to know whether we are in the FakeLoadModule callback to be able to
// return the v2.0 hresults from code:CordbModule::SetJITCompilerFlags when
// we are using the shim.
//
// Expose m_fInLoadModule
bool ShimProcess::GetInLoadModule()
{
    return m_fInLoadModule;

}

void ShimProcess::SetInLoadModule(bool value)
{
    m_fInLoadModule = value;
}

// When we get a continue, we need to clear the flags indicating we're still in a callback
void ShimProcess::NotifyOnContinue ()
{
    m_fInCreateProcess = false;
    m_fInLoadModule = false;
}

// The RS calls this function when the stack is about to be changed in any way, e.g. continue, SetIP, etc.
void ShimProcess::NotifyOnStackInvalidate()
{
    ClearAllShimStackWalk();
}

//---------------------------------------------------------------------------------------
//
// Filter HResults for ICorDebugProcess2::SetDesiredNGENCompilerFlags to emualte V2 error semantics.
// Arguments:
//    hr - V3 hresult
//
// Returns:
//    hresult V2 would have returned in same situation.
HRESULT ShimProcess::FilterSetNgenHresult(HRESULT hr)
{
    if ((hr == CORDBG_E_MUST_BE_IN_CREATE_PROCESS) && !m_fInCreateProcess)
    {
        return hr;
    }
    if (m_attached)
    {
        return CORDBG_E_CANNOT_BE_ON_ATTACH;
    }
    return hr;
}

//---------------------------------------------------------------------------------------
// Filter HRs for ICorDebugModule::EnableJITDebugging, ICorDebugModule2::SetJITCompilerFlags
// to emulate V2 error semantics
//
// Arguments:
//    hr - V3 hresult
//
// Returns:
//    hresult V2 would have returned in same situation.
HRESULT ShimProcess::FilterSetJitFlagsHresult(HRESULT hr)
{
    if ((hr == CORDBG_E_MUST_BE_IN_LOAD_MODULE) && !m_fInLoadModule)
    {
        return hr;
    }
    if (m_attached && (hr == CORDBG_E_MUST_BE_IN_LOAD_MODULE))
    {
        return CORDBG_E_CANNOT_BE_ON_ATTACH;
    }
    return hr;
}

// ----------------------------------------------------------------------------
// ShimProcess::LookupOrCreateShimStackWalk
//
// Description:
//    Find the ShimStackWalk associated with the specified ICDThread.  Create one if it's not found.
//
// Arguments:
//    * pThread - the specified thread
//
// Return Value:
//    Return the ShimStackWalk associated with the specified thread.
//
// Notes:
//    The ShimStackWalks handed back by this function is only valid until the next time the stack is changed
//    in any way.  In other words, the ShimStackWalks are valid until the next time
//    code:CordbThread::CleanupStack or code:CordbThread::MarkStackFramesDirty is called.
//
//    ShimStackWalk and ICDThread have a 1:1 relationship.  Only one ShimStackWalk will be created for any
//    given ICDThread.  So if two threads in the debugger are walking the same thread in the debuggee, they
//    operate on the same ShimStackWalk.  This is ok because ShimStackWalks walk the stack at creation time,
//    cache all the frames, and become read-only after creation.
//
//    Refer to code:ShimProcess::ClearAllShimStackWalk to see how ShimStackWalks are cleared.
//

ShimStackWalk * ShimProcess::LookupOrCreateShimStackWalk(ICorDebugThread * pThread)
{
    ShimStackWalk * pSW = NULL;

    {
        // do the lookup under the Shim lock
        RSLockHolder lockHolder(&m_ShimLock);
        pSW = m_pShimStackWalkHashTable->Lookup(pThread);
    }

    if (pSW == NULL)
    {
        // create one if it's not found and add it to the hash table
        NewHolder<ShimStackWalk> pNewSW(new ShimStackWalk(this, pThread));

        {
            // Do the lookup again under the Shim lock, and only add the new ShimStackWalk if no other thread
            // has beaten us to it.
            RSLockHolder lockHolder(&m_ShimLock);
            pSW = m_pShimStackWalkHashTable->Lookup(pThread);
            if (pSW == NULL)
            {
                m_pShimStackWalkHashTable->Add(pNewSW);
                pSW = pNewSW;

                // don't release the memory if all goes well
                pNewSW.SuppressRelease();
            }
            else
            {
                // The NewHolder will automatically delete the ShimStackWalk when it goes out of scope.
            }
        }
    }

    return pSW;
}

// ----------------------------------------------------------------------------
// ShimProcess::ClearAllShimStackWalk
//
// Description:
//    Remove and delete all the entries in the hash table of ShimStackWalks.
//
// Notes:
//    Refer to code:ShimProcess::LookupOrCreateShimStackWalk to see how ShimStackWalks are created.
//

void ShimProcess::ClearAllShimStackWalk()
{
    RSLockHolder lockHolder(&m_ShimLock);

    // loop through all the entries in the hash table, remove them, and delete them
    for (ShimStackWalkHashTable::Iterator pCurElem = m_pShimStackWalkHashTable->Begin(),
                                          pEndElem = m_pShimStackWalkHashTable->End();
         pCurElem != pEndElem;
         pCurElem++)
    {
        ShimStackWalk * pSW = *pCurElem;
        m_pShimStackWalkHashTable->Remove(pSW->GetThread());
        delete pSW;
    }
}

//---------------------------------------------------------------------------------------
// Called before shim dispatches an event.
//
// Arguments:
//   fRealCreateProcessEvent - true if the shim is about to dispatch a real create process event (as opposed
//                             to one faked up by the shim itself)
// Notes:
//    This may be called from within Filter, which means we may be on the win32-event-thread.
//    This is called on all callbacks from the VM.
//    This gives us a chance to queue fake-attach events. So call it before the Jit-attach
//    event has been queued.
void ShimProcess::PreDispatchEvent(bool fRealCreateProcessEvent /*= false*/)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // For emulating the V2 case, we need to do additional initialization before dispatching the callback to the user.
    if (!m_fFirstManagedEvent)
    {
        // Remember that we're processing the first managed event so that we only call HandleFirstRCEvent() once
        m_fFirstManagedEvent = true;

        // This can fail with the incompatible version HR. The process has already been terminated if this
        // is the case. This will dispatch an Error callback
        // If this fails, the process is in an undefined state.
        // @dbgtodo ipc-block: this will go away once we get rid
        // of the IPC block.
        m_pProcess->FinishInitializeIPCChannel(); // throws on error
    }

    {
        // In jit-attach cases, the first event the shim gets is the event that triggered the jit-attach.
        // Queue up the fake events now, and then once we return, our caller will queue the jit-attach event.
        // In the jit-attach case, this is before a sync-complete has been sent (since the sync doesn't get sent
        // until after the jit-attach event is sent).
        QueueFakeAttachEventsIfNeeded(fRealCreateProcessEvent);
    }

    // Always request an sync (emulates V2 behavior). If LS is not sync-ready, it will ignore the request.
    m_pProcess->RequestSyncAtEvent();


}

//---------------------------------------------------------------------------------------
//
// Locates DAC by finding mscordac{wks|core} next to DBI
//
// Return Value:
//    Returns the module handle for DAC
//    Throws on errors.
//

HMODULE ShimProcess::GetDacModule(PathString& dacModulePath)
{
    HMODULE hDacDll;
    PathString wszAccessDllPath(dacModulePath);

    if (wszAccessDllPath.IsEmpty())
    {
        //
        // Load the access DLL from the same directory as the current CLR Debugging Services DLL.
        //
        if (GetClrModuleDirectory(wszAccessDllPath) != S_OK)
        {
            ThrowLastError();
        }

        // Dac Dll is named:
        //   mscordaccore.dll  <-- coreclr
        //   mscordacwks.dll   <-- desktop
        PCWSTR eeFlavor = MAKEDLLNAME_W(W("mscordaccore"));

        wszAccessDllPath.Append(eeFlavor);
    }
    hDacDll = WszLoadLibrary(wszAccessDllPath);
    if (hDacDll == NULL)
    {
        DWORD dwLastError = GetLastError();
        if (dwLastError == ERROR_MOD_NOT_FOUND)
        {
            // Give a more specific error in the case where we can't find the DAC dll.
            ThrowHR(CORDBG_E_DEBUG_COMPONENT_MISSING);
        }
        else
        {
            ThrowWin32(dwLastError);
        }
    }
    return hDacDll;
}

MachineInfo ShimProcess::GetMachineInfo()
{
    return m_machineInfo;
}

void ShimProcess::SetMarkAttachPendingEvent()
{
    SetEvent(m_markAttachPendingEvent);
}

void ShimProcess::SetTerminatingEvent()
{
    SetEvent(m_terminatingEvent);
}

RSLock * ShimProcess::GetShimLock()
{
    return &m_ShimLock;
}


bool ShimProcess::IsThreadSuspendedOrHijacked(ICorDebugThread * pThread)
{
    return m_pProcess->IsThreadSuspendedOrHijacked(pThread);
}
