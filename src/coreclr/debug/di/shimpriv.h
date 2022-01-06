// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// shimprivate.h
//

//
// private header for RS shim which bridges from V2 to V3.
//*****************************************************************************

#ifndef SHIMPRIV_H
#define SHIMPRIV_H

#include "helpers.h"

#include "shimdatatarget.h"

#include <shash.h>

// Forward declarations
class CordbWin32EventThread;
class Cordb;

class ShimStackWalk;
class ShimChain;
class ShimChainEnum;
class ShimFrameEnum;

// This struct specifies that it's a hash table of ShimStackWalk * using ICorDebugThread as the key.
struct ShimStackWalkHashTableTraits : public PtrSHashTraits<ShimStackWalk, ICorDebugThread *> {};
typedef SHash<ShimStackWalkHashTableTraits> ShimStackWalkHashTable;


//---------------------------------------------------------------------------------------
//
// Simple struct for storing a void *.  This is to be used with a SHash hash table.
//

struct DuplicateCreationEventEntry
{
public:
    DuplicateCreationEventEntry(void * pKey) : m_pKey(pKey) {};

    // These functions must be defined for DuplicateCreationEventsHashTableTraits.
    void * GetKey() {return m_pKey;};
    static UINT32 Hash(void * pKey) {return (UINT32)(size_t)pKey;};

private:
    void * m_pKey;
};

// This struct specifies that it's a hash table of DuplicateCreationEventEntry * using a void * as the key.
// The void * is expected to be an ICDProcess/ICDAppDomain/ICDThread/ICDAssembly/ICDThread interface pointer.
struct DuplicateCreationEventsHashTableTraits : public PtrSHashTraits<DuplicateCreationEventEntry, void *> {};
typedef SHash<DuplicateCreationEventsHashTableTraits> DuplicateCreationEventsHashTable;

//
// Callback that shim provides, which then queues up the events.
//
class ShimProxyCallback :
    public ICorDebugManagedCallback,
    public ICorDebugManagedCallback2,
    public ICorDebugManagedCallback3,
    public ICorDebugManagedCallback4
{
    ShimProcess * m_pShim; // weak reference
    LONG m_cRef;

public:
    ShimProxyCallback(ShimProcess * pShim);
    virtual ~ShimProxyCallback() {}

    // Implement IUnknown
    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void **ppInterface);

    //
    // Implementation of ICorDebugManagedCallback
    //

    COM_METHOD Breakpoint( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugBreakpoint *pBreakpoint);

    COM_METHOD StepComplete( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugStepper *pStepper,
        CorDebugStepReason reason);

    COM_METHOD Break( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *thread);

    COM_METHOD Exception( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        BOOL unhandled);

    COM_METHOD EvalComplete( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugEval *pEval);

    COM_METHOD EvalException( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugEval *pEval);

    COM_METHOD CreateProcess( ICorDebugProcess *pProcess);
    void QueueCreateProcess( ICorDebugProcess *pProcess);

    COM_METHOD ExitProcess( ICorDebugProcess *pProcess);

    COM_METHOD CreateThread( ICorDebugAppDomain *pAppDomain, ICorDebugThread *thread);


    COM_METHOD ExitThread( ICorDebugAppDomain *pAppDomain, ICorDebugThread *thread);

    COM_METHOD LoadModule( ICorDebugAppDomain *pAppDomain, ICorDebugModule *pModule);

    void FakeLoadModule(ICorDebugAppDomain *pAppDomain, ICorDebugModule *pModule);

    COM_METHOD UnloadModule( ICorDebugAppDomain *pAppDomain, ICorDebugModule *pModule);

    COM_METHOD LoadClass( ICorDebugAppDomain *pAppDomain, ICorDebugClass *c);

    COM_METHOD UnloadClass( ICorDebugAppDomain *pAppDomain, ICorDebugClass *c);

    COM_METHOD DebuggerError( ICorDebugProcess *pProcess, HRESULT errorHR, DWORD errorCode);

    COM_METHOD LogMessage( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        LONG lLevel,
        _In_ LPWSTR pLogSwitchName,
        _In_ LPWSTR pMessage);

    COM_METHOD LogSwitch( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        LONG lLevel,
        ULONG ulReason,
        _In_ LPWSTR pLogSwitchName,
        _In_ LPWSTR pParentName);

    COM_METHOD CreateAppDomain(ICorDebugProcess *pProcess,
        ICorDebugAppDomain *pAppDomain);

    COM_METHOD ExitAppDomain(ICorDebugProcess *pProcess,
        ICorDebugAppDomain *pAppDomain);

    COM_METHOD LoadAssembly(ICorDebugAppDomain *pAppDomain,
        ICorDebugAssembly *pAssembly);

    COM_METHOD UnloadAssembly(ICorDebugAppDomain *pAppDomain,
        ICorDebugAssembly *pAssembly);

    COM_METHOD ControlCTrap(ICorDebugProcess *pProcess);

    COM_METHOD NameChange(ICorDebugAppDomain *pAppDomain, ICorDebugThread *pThread);


    COM_METHOD UpdateModuleSymbols( ICorDebugAppDomain *pAppDomain,
        ICorDebugModule *pModule,
        IStream *pSymbolStream);

    COM_METHOD EditAndContinueRemap( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugFunction *pFunction,
        BOOL fAccurate);

    COM_METHOD BreakpointSetError( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugBreakpoint *pBreakpoint,
        DWORD dwError);

    ///
    /// Implementation of ICorDebugManagedCallback2
    ///
    COM_METHOD FunctionRemapOpportunity( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugFunction *pOldFunction,
        ICorDebugFunction *pNewFunction,
        ULONG32 oldILOffset);

    COM_METHOD CreateConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId, _In_ LPWSTR pConnName);

    COM_METHOD ChangeConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId );


    COM_METHOD DestroyConnection(ICorDebugProcess *pProcess, CONNID dwConnectionId);

    COM_METHOD Exception(ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugFrame *pFrame,
        ULONG32 nOffset,
        CorDebugExceptionCallbackType dwEventType,
        DWORD dwFlags );

    COM_METHOD ExceptionUnwind(ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        CorDebugExceptionUnwindCallbackType dwEventType,
        DWORD dwFlags);

    COM_METHOD FunctionRemapComplete( ICorDebugAppDomain *pAppDomain,
        ICorDebugThread *pThread,
        ICorDebugFunction *pFunction);

    COM_METHOD MDANotification(ICorDebugController * pController, ICorDebugThread *pThread, ICorDebugMDA * pMDA);

    ///
    /// Implementation of ICorDebugManagedCallback3
    ///

    // Implementation of ICorDebugManagedCallback3::CustomNotification
    COM_METHOD CustomNotification(ICorDebugThread * pThread, ICorDebugAppDomain * pAppDomain);

    ///
    /// Implementation of ICorDebugManagedCallback4
    ///

    // Implementation of ICorDebugManagedCallback4::BeforeGarbageCollection
    COM_METHOD BeforeGarbageCollection(ICorDebugProcess* pProcess);

    // Implementation of ICorDebugManagedCallback4::AfterGarbageCollection
    COM_METHOD AfterGarbageCollection(ICorDebugProcess* pProcess);

    // Implementation of ICorDebugManagedCallback4::DataBreakpoint
    COM_METHOD DataBreakpoint(ICorDebugProcess* pProcess, ICorDebugThread* pThread, BYTE* pContext, ULONG32 contextSize);
};


//
// Base class for event queue. These are nested into a singly linked list.
// Shim maintains event queue
//
class ManagedEvent
{
public:
    // Need virtual dtor since this is a base class.
    virtual ~ManagedEvent();

#ifdef _DEBUG
    // For debugging, get a pointer value that can identify the type of this event.
    void * GetDebugCookie();
#endif

    // We'll have a lot of derived classes of ManagedEvent, and so encapsulating the arguments
    // for the Dispatch() function lets us juggle them around easily without hitting every signature.
    class DispatchArgs
    {
    public:
        DispatchArgs(ICorDebugManagedCallback * pCallback1, ICorDebugManagedCallback2 * pCallback2, ICorDebugManagedCallback3 * pCallback3, ICorDebugManagedCallback4 * pCallback4);

        ICorDebugManagedCallback * GetCallback1();
        ICorDebugManagedCallback2 * GetCallback2();
        ICorDebugManagedCallback3 * GetCallback3();
        ICorDebugManagedCallback4 * GetCallback4();


    protected:
        ICorDebugManagedCallback * m_pCallback1;
        ICorDebugManagedCallback2 * m_pCallback2;
        ICorDebugManagedCallback3 * m_pCallback3;
        ICorDebugManagedCallback4 * m_pCallback4;
    };

    // Returns: value of callback from end-user
    virtual HRESULT Dispatch(DispatchArgs args) = 0;


    // Returns 0 if none.
    DWORD GetOSTid();

protected:
    // Ctor for events with thread-affinity
    ManagedEvent(ICorDebugThread * pThread);

    // Ctor for events without thread affinity.
    ManagedEvent();

    friend class ManagedEventQueue;
    ManagedEvent * m_pNext;

    DWORD m_dwThreadId;
};

//
// Queue of managed events.
// Shim can use this to collect managed debug events, queue them, and then drain the event
// queue when a sync-complete occurs.
// Event queue gets initialized with a lock and will lock internally.
class ManagedEventQueue
{
public:
    ManagedEventQueue();


    void Init(RSLock * pLock);

    // Remove event from the top. Caller then takes ownership of Event and will call Delete on it.
    // Caller checks IsEmpty() first.
    ManagedEvent * Dequeue();

    // Queue owns the event and will delete it (unless it's dequeued first).
    void QueueEvent(ManagedEvent * pEvent);

    // Test if event queue is empty
    bool IsEmpty();

    // Empty event queue and delete all objects
    void DeleteAll();

    // Nothrows
    BOOL HasQueuedCallbacks(ICorDebugThread * pThread);

    // Save the current queue and start with a new empty queue
    void SuspendQueue();

    // Restore the saved queue onto the end of the current queue
    void RestoreSuspendedQueue();

protected:
    // The lock to be used for synchronizing all access to the queue
    RSLock * m_pLock;

    // If empty,  First + Last are both NULL.
    // Else first points to the head of the queue; and Last points to the end of the queue.
    ManagedEvent * m_pFirstEvent;
    ManagedEvent * m_pLastEvent;

};


//---------------------------------------------------------------------------------------
//
// Shim's layer on top of a process.
//
// Notes:
//    This contains a V3 ICorDebugProcess, and provides V2 ICDProcess functionality.
//
class ShimProcess
{
    // Delete via Ref count semantics.
    ~ShimProcess();
public:
    // Initialize ref count is 0.
    ShimProcess();

    // Lifetime semantics handled by reference counting.
    void AddRef();
    void Release();

    // Release all resources. Can be called multiple times.
    void Dispose();

    // Initialization phases.
    // 1. allocate new ShimProcess(). This lets us spin up a Win32 EventThread, which can then
    //    be used to
    // 2. Call ShimProcess::CreateProcess/DebugActiveProcess. This will call CreateAndStartWin32ET to
    //     craete the w32et.
    // 3. Create OS-debugging pipeline. This establishes the physical OS process and gets us a pid/handle
    // 4. pShim->InitializeDataTarget - this creates a reader/writer abstraction around the OS process.
    // 5. pShim->SetProcess() - this connects the Shim to the ICDProcess object.
    HRESULT InitializeDataTarget(const ProcessDescriptor * pProcessDescriptor);
    void SetProcess(ICorDebugProcess * pProcess);

    //-----------------------------------------------------------
    // Creation
    //-----------------------------------------------------------

    static HRESULT CreateProcess(
          Cordb * pCordb,
          ICorDebugRemoteTarget * pRemoteTarget,
          LPCWSTR programName,
          _In_z_ LPWSTR  programArgs,
          LPSECURITY_ATTRIBUTES lpProcessAttributes,
          LPSECURITY_ATTRIBUTES lpThreadAttributes,
          BOOL bInheritHandles,
          DWORD dwCreationFlags,
          PVOID lpEnvironment,
          LPCWSTR lpCurrentDirectory,
          LPSTARTUPINFOW lpStartupInfo,
          LPPROCESS_INFORMATION lpProcessInformation,
          CorDebugCreateProcessFlags corDebugFlags
    );

    static HRESULT DebugActiveProcess(
        Cordb * pCordb,
        ICorDebugRemoteTarget * pRemoteTarget,
        const ProcessDescriptor * pProcessDescriptor,
        BOOL win32Attach

    );

    // Locates the DAC module adjacent to DBI
    static HMODULE GetDacModule();

    //
    // Functions used by CordbProcess
    //

    // Determine if the calling thread is the win32 event thread.
    bool IsWin32EventThread();


    // Expose the W32ET thread to the CordbProcess so that it can emulate V2 behavior
    CordbWin32EventThread * GetWin32EventThread();

    // Accessor wrapper to mark whether we're interop-debugging.
    void SetIsInteropDebugging(bool fIsInteropDebugging);

    // Handle a debug event.
    HRESULT HandleWin32DebugEvent(const DEBUG_EVENT * pEvent);

    ManagedEventQueue * GetManagedEventQueue();

    ManagedEvent * DequeueManagedEvent();

    ShimProxyCallback * GetShimCallback();

    // Begin Queing the fake attach events.
    void BeginQueueFakeAttachEvents();

    // Queue fake attach events if needed
    void QueueFakeAttachEventsIfNeeded(bool fRealCreateProcessEvent);

    // Actually do the work to queue the fake attach events.
    void QueueFakeAttachEvents();

    // Helper to queue fake assembly and mdule events
    void QueueFakeAssemblyAndModuleEvent(ICorDebugAssembly * pAssembly);

    // Queue fake thread-create events on attach. Order via native threads.
    HRESULT QueueFakeThreadAttachEventsNativeOrder();

    // Queue fake thread-create events on attach. No ordering.
    HRESULT QueueFakeThreadAttachEventsNoOrder();

    bool IsThreadSuspendedOrHijacked(ICorDebugThread * pThread);

    // Expose m_attached to CordbProcess.
    bool GetAttached();

    // We need to know whether we are in the CreateProcess callback to be able to
    // return the v2.0 hresults from code:CordbProcess::SetDesiredNGENCompilerFlags
    // when we are using the shim.
    //
    // Expose m_fInCreateProcess
    bool GetInCreateProcess();
    void SetInCreateProcess(bool value);

    // We need to know whether we are in the FakeLoadModule callback to be able to
    // return the v2.0 hresults from code:CordbModule::SetJITCompilerFlags when
    // we are using the shim.
    //
    // Expose m_fInLoadModule
    bool GetInLoadModule();
    void SetInLoadModule(bool value);

    // When we get a continue, we need to clear the flags indicating we're still in a callback
    void NotifyOnContinue ();

    // The RS calls this function when the stack is about to be changed in any way, e.g. continue, SetIP,
    // etc.
    void NotifyOnStackInvalidate();

    // Helpers to filter HRs to emulate V2 error codes.
    HRESULT FilterSetNgenHresult(HRESULT hr);
    HRESULT FilterSetJitFlagsHresult(HRESULT hr);

    //.............................................................


    // Lookup or create a ShimStackWalk for the specified thread.  ShimStackWalk and ICorDebugThread has
    // a 1:1 relationship.
    ShimStackWalk * LookupOrCreateShimStackWalk(ICorDebugThread * pThread);

    // Clear all ShimStackWalks and flush all the caches.
    void            ClearAllShimStackWalk();

    // Get the corresponding ICDProcess object.
    ICorDebugProcess * GetProcess();

    // Get the data target to access the debuggee.
    ICorDebugMutableDataTarget * GetDataTarget();

    // Get the native event pipeline
    INativeEventPipeline * GetNativePipeline();

    // Are we interop-debugging?
    bool IsInteropDebugging();


    // Finish all the necessary initialization work and queue up any necessary fake attach events before
    // dispatching an event.
    void PreDispatchEvent(bool fRealCreateProcessEvent = false);

    // Look for a CLR in the process and if found, return it's instance ID
    HRESULT FindLoadedCLR(CORDB_ADDRESS * pClrInstanceId);

    // Retrieve the IP address and the port number of the debugger proxy.
    MachineInfo GetMachineInfo();

    // Add an entry in the duplicate creation event hash table for the specified key.
    void AddDuplicateCreationEvent(void * pKey);

    // Check if a duplicate creation event entry exists for the specified key.  If so, remove it.
    bool RemoveDuplicateCreationEventIfPresent(void * pKey);

    void SetMarkAttachPendingEvent();

    void SetTerminatingEvent();

    RSLock * GetShimLock();

protected:

    // Reference count.
    LONG m_ref;

    //
    // Helper functions
    //
    HRESULT CreateAndStartWin32ET(Cordb * pCordb);

    //
    // Synchronization events to ensure that AttachPending bit is marked before DebugActiveProcess
    // returns or debugger is detaching
    //
    HANDLE  m_markAttachPendingEvent;
    HANDLE  m_terminatingEvent;

    // Finds the base address of [core]clr.dll
    CORDB_ADDRESS GetCLRInstanceBaseAddress();

    //
    // Event Queues
    //

    // Shim maintains event queue to emulate V2 semantics.
    // In V2, IcorDebug internally queued debug events and dispatched them
    // once the debuggee was synchronized. In V3, ICorDebug dispatches events immediately.
    // The event queue is moved into the shim to build V2 semantics of V3 behavior.
    ManagedEventQueue m_eventQueue;

    // Lock to protect Shim data structures. This is currently a small lock that
    // protects leaf-level structures, but it may grow to protect larger things.
    RSLock m_ShimLock;

    // Serializes ShimProcess:Dispose() with other ShimProcess functions. For now, this
    // cannot be the same as m_ShimLock. See LL_SHIM_PROCESS_DISPOSE_LOCK for more
    // information
    RSLock m_ShimProcessDisposeLock;

    // Sticky bit to do lazy-initialization on the first managed event.
    bool                  m_fFirstManagedEvent;

    RSExtSmartPtr<ShimProxyCallback> m_pShimCallback;


    // This is for emulating V2 Attach. Initialized to false, and then set to true if we ened to send fake attach events.
    // Reset to false once the events are sent. See code:ShimProcess::QueueFakeAttachEventsIfNeeded
    bool  m_fNeedFakeAttachEvents;

    // True if the process was created from an attach (DebugActiveProcess); False if it was launched (CreateProcess)
    // This is used to send an Attach IPC event, and also used to provide more specific error codes.
    bool m_attached;

    // True iff we are in the shim's CreateProcess callback. This is used to determine which hresult to
    // return from code:CordbProcess::SetDesiredNGENCompilerFlags so we correctly emulate the behavior of v2.0.
    // This is set at the beginning of the callback and cleared in code:CordbProcess::ContinueInternal.
    bool m_fInCreateProcess;

    // True iff we are in the shim's FakeLoadModule callback. This is used to determine which hresult to
    // return from code:CordbModule::SetJITCompilerFlags so we correctly emulate the behavior of v2.0.
    // This is set at the beginning of the callback and cleared in code:CordbProcess::ContinueInternal.
    bool m_fInLoadModule;
    //
    // Data
    //

    // Pointer to CordbProcess.
    // @dbgtodo shim: We'd like this to eventually go through public interfaces (ICorDebugProcess)
    IProcessShimHooks * m_pProcess; // Reference is kept by m_pIProcess;
    RSExtSmartPtr<ICorDebugProcess> m_pIProcess;

    // Win32EvenThread, which is the thread that uses the native debug API.
    CordbWin32EventThread * m_pWin32EventThread;

    // Actual data-target. Since we're shimming V2 scenarios, and V3 is always
    // live-debugging, this is always a live data-target.
    RSExtSmartPtr<ShimDataTarget> m_pLiveDataTarget;


    // If true, the shim is emulating interop-debugging
    // If false, the shim is emulating managed-only debugging.
    // Both managed and native debugging have the same underlying pipeline (built
    // on native-debug events). So the only difference is how they handle those events.
    bool m_fIsInteropDebugging;

    // true iff Dispose() was called.  Consult this and do your work under m_ShimProcessDisposeLock
    // to serialize yourself against a call to Dispose().  This protects your work
    // from the user doing a Debugger Detach in the middle.
    bool m_fIsDisposed;

    //.............................................................................
    //
    // Members used for handling native events when managed-only debugging.
    //
    //.............................................................................

    // Default handler for native events when managed-only debugging.
    void DefaultEventHandler(const DEBUG_EVENT * pEvent, DWORD * pdwContinueStatus);

    // Given a debug event, track the file handles.
    void TrackFileHandleForDebugEvent(const DEBUG_EVENT * pEvent);

    // Have we gotten the loader breakpoint yet?
    // A Debugger needs to do special work to skip the loader breakpoint,
    // and that's also when it should dispatch the faked managed attach events.
    bool m_loaderBPReceived;

    // Raw callback for ContinueStatusChanged from Data-target.
    static HRESULT ContinueStatusChanged(void * pUserData, DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus);

    // Real worker to update ContinueStatusChangedData
    HRESULT ContinueStatusChangedWorker(DWORD dwThreadId, CORDB_CONTINUE_STATUS dwContinueStatus);

    struct ContinueStatusChangedData
    {
        void Clear();
        bool IsSet();
        // Tid of Thread changed
        DWORD m_dwThreadId;

        // New continue status.
        CORDB_CONTINUE_STATUS m_status;
    } m_ContinueStatusChangedData;

    // the hash table of ShimStackWalks
    ShimStackWalkHashTable * m_pShimStackWalkHashTable;

    // the hash table of duplicate creation events
    DuplicateCreationEventsHashTable * m_pDupeEventsHashTable;

    MachineInfo m_machineInfo;
};


//---------------------------------------------------------------------------------------
//
// This is the container class of ShimChains, ICorDebugFrames, ShimChainEnums, and ShimFrameEnums.
// It has a 1:1 relationship  with ICorDebugThreads.  Upon creation, this class walks the entire stack and
// caches all the stack frames and chains.  The enumerators are created on demand.
//

class ShimStackWalk
{
public:
    ShimStackWalk(ShimProcess * pProcess, ICorDebugThread * pThread);
    ~ShimStackWalk();

    // These functions do not adjust the reference count.
    ICorDebugThread * GetThread();
    ShimChain *       GetChain(UINT32 index);
    ICorDebugFrame *  GetFrame(UINT32 index);

    // Get the number of frames and chains.
    ULONG             GetChainCount();
    ULONG             GetFrameCount();

    RSLock *          GetShimLock();

    // Add ICDChainEnum and ICDFrameEnum.
    void AddChainEnum(ShimChainEnum * pChainEnum);
    void AddFrameEnum(ShimFrameEnum * pFrameEnum);

    // The next two functions are for ShimStackWalkHashTableTraits.
    ICorDebugThread * GetKey();
    static UINT32 Hash(ICorDebugThread * pThread);

    // Check if the specified frame is the leaf frame according to the V2 definition.
    BOOL IsLeafFrame(ICorDebugFrame * pFrame);

    // Check if the two specified frames are the same.  This function checks the SPs, frame address, etc.
    // instead of just checking for pointer equality.
    BOOL IsSameFrame(ICorDebugFrame * pLeft, ICorDebugFrame * pRight);

    // The following functions are entry point into the ShimStackWalk.  They are called by the RS.
    void EnumerateChains(ICorDebugChainEnum ** ppChainEnum);

    void GetActiveChain(ICorDebugChain ** ppChain);
    void GetActiveFrame(ICorDebugFrame ** ppFrame);
    void GetActiveRegisterSet(ICorDebugRegisterSet ** ppRegisterSet);

    void GetChainForFrame(ICorDebugFrame * pFrame, ICorDebugChain ** ppChain);
    void GetCallerForFrame(ICorDebugFrame * pFrame, ICorDebugFrame ** ppCallerFrame);
    void GetCalleeForFrame(ICorDebugFrame * pFrame, ICorDebugFrame ** ppCalleeFrame);

private:
    //---------------------------------------------------------------------------------------
    //
    // This is a helper class used to store the information of a chain during a stackwalk.  A chain is marked
    // by the CONTEXT on the leaf boundary and a FramePointer on the root boundary.  Also, notice that we
    // are keeping two CONTEXTs.  This is because some chain types may cancel a previous unmanaged chain.
    // For example, a CHAIN_FUNC_EVAL chain cancels any CHAIN_ENTER_UNMANAGED chain immediately preceding
    // it.  In this case, the leaf boundary of the CHAIN_FUNC_EVAL chain is marked by the CONTEXT of the
    // previous CHAIN_ENTER_MANAGED, not the previous CHAIN_ENTER_UNMANAGED.
    //

    struct ChainInfo
    {
    public:
        ChainInfo() : m_rootFP(LEAF_MOST_FRAME), m_reason(CHAIN_NONE), m_fNeedEnterManagedChain(FALSE), m_fLeafNativeContextIsValid(FALSE) {}

        void CancelUMChain() { m_reason = CHAIN_NONE; }
        BOOL IsTrackingUMChain() { return (m_reason == CHAIN_ENTER_UNMANAGED); }

        DT_CONTEXT          m_leafNativeContext;
        DT_CONTEXT          m_leafManagedContext;
        FramePointer        m_rootFP;
        CorDebugChainReason m_reason;
        bool                m_fNeedEnterManagedChain;
        bool                m_fLeafNativeContextIsValid;
    };

    //---------------------------------------------------------------------------------------
    //
    // This is a helper class used to store information during a stackwalk.  Conceptually it is a simplified
    // version of FrameInfo used on the LS in V2.
    //

    struct StackWalkInfo
    {
    public:
        StackWalkInfo();
        ~StackWalkInfo();

        // Reset all the per-frame information.
        void ResetForNextFrame();

        // During the stackwalk, we need to find out whether we should process the next stack frame or the
        // next internal frame.  These functions help us determine whether we have exhausted one or both
        // types of frames.  The stackwalk is finished when both types are exhausted.
        bool ExhaustedAllFrames();
        bool ExhaustedAllStackFrames();
        bool ExhaustedAllInternalFrames();

        // Simple helper function to get the current internal frame.
        ICorDebugInternalFrame2 * GetCurrentInternalFrame();

        // Check whether we are processing the first frame.
        BOOL IsLeafFrame();

        // Check whether we are skipping frames because of a child frame.
        BOOL IsSkippingFrame();

        // Indicates whether we are dealing with a converted frame.
        // See code:CordbThread::ConvertFrameForILMethodWithoutMetadata.
        BOOL HasConvertedFrame();

        // Store the child frame we are currently trying to find the parent frame for.
        // If this is NULL, then we are not skipping frames.
        RSExtSmartPtr<ICorDebugNativeFrame2>   m_pChildFrame;

        // Store the converted frame, if any.
        RSExtSmartPtr<ICorDebugInternalFrame2> m_pConvertedInternalFrame2;

        // Store the array of internal frames.  This is an array of RSExtSmartPtrs, and so each element
        // is protected, and we only need to call Clear() to release each element and free all the memory.
        RSExtPtrArray<ICorDebugInternalFrame2> m_ppInternalFrame2;

        UINT32  m_cChain;               // number of chains
        UINT32  m_cFrame;               // number of frames
        UINT32  m_firstFrameInChain;    // the index of the first frame in the current chain
        UINT32  m_cInternalFrames;      // number of internal frames
        UINT32  m_curInternalFrame;     // the index of the current internal frame being processed

        CorDebugInternalFrameType m_internalFrameType;

        bool m_fExhaustedAllStackFrames;

        // Indicate whether we are processing an internal frame or a stack frame.
        bool m_fProcessingInternalFrame;

        // Indicate whether we should skip the current chain because it's a chain derived from a leaf frame
        // of type TYPE_INTERNAL.  This is the behaviour in V2.
        // See code:DebuggerWalkStackProc.
        bool m_fSkipChain;

        // Indicate whether the current frame is the first frame we process.
        bool m_fLeafFrame;

        // Indicate whether we are processing a converted frame.
        bool m_fHasConvertedFrame;
    };

    // A ShimStackWalk is deleted when a process is continued, or when the stack is changed in any way
    // (e.g. SetIP, EnC, etc.).
    void Populate();
    void Clear();

    // Get a FramePointer to mark the root boundary of a chain.
    FramePointer GetFramePointerForChain(DT_CONTEXT * pContext);
    FramePointer GetFramePointerForChain(ICorDebugInternalFrame2 * pInternalFrame2);

    CorDebugInternalFrameType GetInternalFrameType(ICorDebugInternalFrame2 * pFrame2);

    // Append a frame to the array.
    void AppendFrame(ICorDebugFrame * pFrame, StackWalkInfo * pStackWalkInfo);
    void AppendFrame(ICorDebugInternalFrame2 * pInternalFrame2, StackWalkInfo * pStackWalkInfo);

    // Append a chain to the array.
    void AppendChainWorker(StackWalkInfo *     pStackWalkInfo,
                           DT_CONTEXT *        pLeafContext,
                           FramePointer        fpRoot,
                           CorDebugChainReason chainReason,
                           BOOL                fIsManagedChain);
    void AppendChain(ChainInfo * pChainInfo, StackWalkInfo * pStackWalkInfo);

    // Save information on the ChainInfo regarding the current chain.
    void SaveChainContext(ICorDebugStackWalk * pSW, ChainInfo * pChainInfo, DT_CONTEXT * pContext);

    // Check what we are process next, a internal frame or a stack frame.
    BOOL CheckInternalFrame(ICorDebugFrame *     pNextStackFrame,
                            StackWalkInfo *      pStackWalkInfo,
                            ICorDebugThread3 *   pThread3,
                            ICorDebugStackWalk * pSW);

    // Convert an ICDInternalFrame to another ICDInternalFrame due to IL methods without metadata.
    // See code:CordbThread::ConvertFrameForILMethodWithoutMetadata.
    BOOL ConvertInternalFrameToDynamicMethod(StackWalkInfo * pStackWalkInfo);

    // Convert an ICDNativeFrame to an ICDInternalFrame due to IL methods without metadata.
    // See code:CordbThread::ConvertFrameForILMethodWithoutMetadata.
    BOOL ConvertStackFrameToDynamicMethod(ICorDebugFrame * pFrame, StackWalkInfo * pStackWalkInfo);

    // Process an unmanaged chain.
    BOOL ShouldTrackUMChain(StackWalkInfo * pswInfo);
    void TrackUMChain(ChainInfo * pChainInfo, StackWalkInfo * pStackWalkInfo);

    // Check whether the internal frame is a newly exposed type in Arrowhead.  If so, then the shim should
    // not expose it.
    BOOL IsV3FrameType(CorDebugInternalFrameType type);

    // Check whether the specified frame represents a dynamic method.
    BOOL IsILFrameWithoutMetadata(ICorDebugFrame * pFrame);

    CDynArray<ShimChain *>      m_stackChains;  // growable ordered array of chains and frames
    CDynArray<ICorDebugFrame *> m_stackFrames;

    ShimChainEnum * m_pChainEnumList;           // linked list of ShimChainEnum and ShimFrameEnum
    ShimFrameEnum * m_pFrameEnumList;

    // the thread on which we are doing a stackwalk, i.e. the "owning" thread
    RSExtSmartPtr<ShimProcess>     m_pProcess;
    RSExtSmartPtr<ICorDebugThread> m_pThread;
};


//---------------------------------------------------------------------------------------
//
// This class implements the deprecated ICDChain interface.
//

class ShimChain : public ICorDebugChain
{
public:
    ShimChain(ShimStackWalk *     pSW,
              DT_CONTEXT *        pContext,
              FramePointer        fpRoot,
              UINT32              chainIndex,
              UINT32              frameStartIndex,
              UINT32              frameEndIndex,
              CorDebugChainReason chainReason,
              BOOL                fIsManaged,
              RSLock *            pShimLock);
    virtual ~ShimChain();

    void Neuter();
    BOOL IsNeutered();

    //
    // IUnknown
    //

    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void ** ppInterface);

    //
    // ICorDebugChain
    //

    COM_METHOD GetThread(ICorDebugThread ** ppThread);
    COM_METHOD GetStackRange(CORDB_ADDRESS * pStart, CORDB_ADDRESS * pEnd);
    COM_METHOD GetContext(ICorDebugContext ** ppContext);
    COM_METHOD GetCaller(ICorDebugChain ** ppChain);
    COM_METHOD GetCallee(ICorDebugChain ** ppChain);
    COM_METHOD GetPrevious(ICorDebugChain ** ppChain);
    COM_METHOD GetNext(ICorDebugChain ** ppChain);
    COM_METHOD IsManaged(BOOL * pManaged);
    COM_METHOD EnumerateFrames(ICorDebugFrameEnum ** ppFrames);
    COM_METHOD GetActiveFrame(ICorDebugFrame ** ppFrame);
    COM_METHOD GetRegisterSet(ICorDebugRegisterSet ** ppRegisters);
    COM_METHOD GetReason(CorDebugChainReason * pReason);

    //
    // accessors
    //

    // Get the owning ShimStackWalk.
    ShimStackWalk * GetShimStackWalk();

    // Get the first and last index of the frame owned by this chain.  This class itself doesn't store the
    // frames.  Rather, the frames are stored on the ShimStackWalk.  This class just stores the indices.
    // Note that the indices are [firstIndex, lastIndex), i.e. the last index is exclusive.
    UINT32 GetFirstFrameIndex();
    UINT32 GetLastFrameIndex();

private:
    // A chain describes a stack range within the stack.  This includes a CONTEXT at the start (leafmost)
    // end of the chain, and a frame pointer where the chain ends (rootmost).  This stack range is exposed
    // publicly via ICDChain::GetStackRange(), and can be used to stitch managed and native stack frames
    // together into a unified stack.
    DT_CONTEXT          m_context;          // the leaf end of the chain
    FramePointer        m_fpRoot;           // the root end of the chain

    ShimStackWalk *     m_pStackWalk;       // the owning ShimStackWalk
    Volatile<ULONG>     m_refCount;

    // The 0-based index of this chain in the ShimStackWalk's chain array (m_pStackWalk->m_stackChains).
    UINT32              m_chainIndex;

    // The 0-based index of the first frame owned by this chain in the ShimStackWalk's frame array
    // (m_pStackWalk->m_stackFrames).  See code::ShimChain::GetFirstFrameIndex().
    UINT32              m_frameStartIndex;

    // The 0-based index of the last frame owned by this chain in the ShimStackWalk's frame array
    // (m_pStackWalk->m_stackFrames).  This index is exlusive.  See code::ShimChain::GetLastFrameIndex().
    UINT32              m_frameEndIndex;

    CorDebugChainReason m_chainReason;
    BOOL                m_fIsManaged;       // indicates whether this chain contains managed frames
    BOOL                m_fIsNeutered;

    RSLock *            m_pShimLock;        // shim lock from ShimProcess to protect neuteredness checks
};


//---------------------------------------------------------------------------------------
//
// This class implements the deprecated ICDChainEnum interface.
//

class ShimChainEnum : public ICorDebugChainEnum
{
public:
    ShimChainEnum(ShimStackWalk * pSW, RSLock * pShimLock);
    virtual ~ShimChainEnum();

    void Neuter();
    BOOL IsNeutered();

    //
    // IUnknown
    //

    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void ** ppInterface);

    //
    // ICorDebugEnum
    //

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum ** ppEnum);
    COM_METHOD GetCount(ULONG * pcChains);

    //
    // ICorDebugChainEnum
    //

    COM_METHOD Next(ULONG cChains, ICorDebugChain * rgpChains[], ULONG * pcChainsFetched);

    //
    // accessors
    //

    // used to link ShimChainEnums in a list
    ShimChainEnum * GetNext();
    void SetNext(ShimChainEnum * pNext);

private:
    ShimStackWalk * m_pStackWalk;           // the owning ShimStackWalk

    // This points to the next ShimChainEnum in the linked list of ShimChainEnums to be cleaned up.
    // The head of the list is on the ShimStackWalk (m_pStackWalk->m_pChainEnumList).
    ShimChainEnum * m_pNext;

    UINT32          m_currentChainIndex;    // the index of the current ShimChain being enumerated
    Volatile<ULONG> m_refCount;
    BOOL            m_fIsNeutered;

    RSLock *        m_pShimLock;            // shim lock from ShimProcess to protect neuteredness checks
};


//---------------------------------------------------------------------------------------
//
// This class implements the deprecated ICDFrameEnum interface.
//

class ShimFrameEnum : public ICorDebugFrameEnum
{
public:
    ShimFrameEnum(ShimStackWalk * pSW, ShimChain * pChain, UINT32 frameStartIndex, UINT32 frameEndIndex, RSLock * pShimLock);
    virtual ~ShimFrameEnum();

    void Neuter();
    BOOL IsNeutered();

    //
    // IUnknown
    //

    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    COM_METHOD QueryInterface(REFIID riid, void ** ppInterface);

    //
    // ICorDebugEnum
    //

    COM_METHOD Skip(ULONG celt);
    COM_METHOD Reset();
    COM_METHOD Clone(ICorDebugEnum ** ppEnum);
    COM_METHOD GetCount(ULONG * pcFrames);

    //
    // ICorDebugFrameEnum
    //

    COM_METHOD Next(ULONG cFrames, ICorDebugFrame * rgpFrames[], ULONG * pcFramesFetched);

    //
    // accessors
    //

    // used to link ShimChainEnums in a list
    ShimFrameEnum * GetNext();
    void SetNext(ShimFrameEnum * pNext);

private:
    ShimStackWalk * m_pStackWalk;           // the owning ShimStackWalk
    ShimChain *     m_pChain;               // the owning ShimChain
    RSLock *        m_pShimLock;            // shim lock from ShimProcess to protect neuteredness checks

    // This points to the next ShimFrameEnum in the linked list of ShimFrameEnums to be cleaned up.
    // The head of the list is on the ShimStackWalk (m_pStackWalk->m_pFrameEnumList).
    ShimFrameEnum * m_pNext;

    UINT32          m_currentFrameIndex;    // the current ICDFrame being enumerated
    UINT32          m_endFrameIndex;        // the last index (exclusive) of the frame owned by the chain;
                                            // see code:ShimChain::GetLastFrameIndex
    Volatile<ULONG> m_refCount;
    BOOL            m_fIsNeutered;
};


#endif // SHIMPRIV_H

