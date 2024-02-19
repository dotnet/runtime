// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************

//
// File: rsthread.cpp
//
//*****************************************************************************
#include "stdafx.h"
#include "primitives.h"
#include <float.h>
#include <tls.h>

// Stack-based holder for RSPTRs that we allocated to give to the LS.
// If LS successfully takes ownership of them, then call SuppressRelease().
// Else, dtor will free them up.
// This is using a table protected by the ProcessLock().
template <class T>
class RsPtrHolder
{
    T * m_pObject;
    RsPointer<T> m_ptr;
public:
    RsPtrHolder(T* pObject)
    {
        _ASSERTE(pObject != NULL);
        m_ptr.AllocHandle(pObject->GetProcess(), pObject);
        m_pObject = pObject;
    }

    // If owner didn't call SuppressRelease() to take ownership, then have dtor free it.
    ~RsPtrHolder()
    {
        if (!m_ptr.IsNull())
        {
            // @dbgtodo  synchronization - push this up. Note that since this is in a dtor;
            // need to order it well against RSLockHolder.
            RSLockHolder lockHolder(m_pObject->GetProcess()->GetProcessLock());
            T* pObjTest = m_ptr.UnWrapAndRemove(m_pObject->GetProcess());
            (void)pObjTest; //prevent "unused variable" error from GCC
            _ASSERTE(pObjTest == m_pObject);
        }
    }

    RsPointer<T> Ptr()
    {
        return m_ptr;
    }
    void SuppressRelease()
    {
        m_ptr = RsPointer<T>::NullPtr();
    }

};

/* ------------------------------------------------------------------------- *
 * Managed Thread classes
 * ------------------------------------------------------------------------- */


//---------------------------------------------------------------------------------------
//
// Instantiate a CordbThread object, which represents a managed thread.
//
// Arguments:
//    process - non-null process object that this thread lives in.
//    id - OS thread id of this thread.
//    handle - OS Handle to the native thread in the debuggee.
//
//---------------------------------------------------------------------------------------

CordbThread::CordbThread(CordbProcess * pProcess, VMPTR_Thread vmThread) :
    CordbBase(pProcess,
              VmPtrToCookie(vmThread),
              enumCordbThread),
    m_pContext(NULL),
    m_fContextFresh(false),
    m_pAppDomain(NULL),
    m_debugState(THREAD_RUN),
    m_fFramesFresh(false),
    m_fFloatStateValid(false),
    m_floatStackTop(0),
    m_fException(false),
    m_EnCRemapFunctionIP(NULL),
    m_userState(kInvalidUserState),
    m_hCachedThread(INVALID_HANDLE_VALUE),
    m_hCachedOutOfProcThread(INVALID_HANDLE_VALUE)
{
    m_fHasUnhandledException = FALSE;
    m_pExceptionRecord = NULL;

    // Thread id may be a "fake" OS id for a CLRHosted thread.
    m_vmThreadToken     = vmThread;

    // This id must be unique for the thread. V2 uses the current OS thread id.
    // If we ever support fibers, then we need to use something more unique than that.
    m_dwUniqueID = pProcess->GetDAC()->GetUniqueThreadID(vmThread); // may throw

    LOG((LF_CORDB, LL_INFO1000, "CT::CT new thread 0x%p vmptr=0x%p id=0x%x\n",
        this, m_vmThreadToken, m_dwUniqueID));

    // Unique ID should never be 0.
    _ASSERTE(m_dwUniqueID != 0);

    m_vmLeftSideContext = VMPTR_CONTEXT::NullPtr();
    m_vmExcepObjHandle = VMPTR_OBJECTHANDLE::NullPtr();

#if defined(_DEBUG)
    for (unsigned int i = 0;
         i < (sizeof(m_floatValues) / sizeof(m_floatValues[0]));
         i++)
    {
        m_floatValues[i] = 0;
    }
#endif

    // Set AppDomain
    VMPTR_AppDomain vmAppDomain = pProcess->GetDAC()->GetCurrentAppDomain(vmThread);
    m_pAppDomain = pProcess->LookupOrCreateAppDomain(vmAppDomain);
    _ASSERTE(m_pAppDomain != NULL);
}


CordbThread::~CordbThread()
{
    // We've already been neutered, thus we don't need to call CleanupStack().
    // That will have neutered + cleared frames + chains.
    _ASSERTE(IsNeutered());

    // Cleared in neuter
    _ASSERTE(m_pContext == NULL);
    _ASSERTE(m_hCachedThread == INVALID_HANDLE_VALUE);
    _ASSERTE(m_pExceptionRecord == NULL);
}

// Neutered by the CordbProcess
void CordbThread::Neuter()
{
    if (IsNeutered())
    {
        return;
    }

    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    delete m_pExceptionRecord;
    m_pExceptionRecord = NULL;

    // Neuter frames & Chains.
    CleanupStack();


    if (m_hCachedThread != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hCachedThread);
        m_hCachedThread = INVALID_HANDLE_VALUE;
    }

    if( m_pContext != NULL )
    {
        delete [] m_pContext;
        m_pContext = NULL;
    }

    ClearStackFrameCache();

    CordbBase::Neuter();
}

HRESULT CordbThread::QueryInterface(REFIID id, void ** ppInterface)
{
    if (id == IID_ICorDebugThread)
    {
        *ppInterface = static_cast<ICorDebugThread *>(this);
    }
    else if (id == IID_ICorDebugThread2)
    {
        *ppInterface = static_cast<ICorDebugThread2 *>(this);
    }
    else if (id == IID_ICorDebugThread3)
    {
        *ppInterface = static_cast<ICorDebugThread3*>(this);
    }
    else if (id == IID_ICorDebugThread4)
    {
        *ppInterface = static_cast<ICorDebugThread4*>(this);
    }
    else if (id == IID_ICorDebugThread5)
    {
        *ppInterface = static_cast<ICorDebugThread5*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown *>(static_cast<ICorDebugThread *>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}


#ifdef _DEBUG
// Callback helper for code:CordbThread::DbgAssertThreadDeleted
//
// Arguments:
//    vmThread - thread from enumeration of threads in the target.
//    pUserData - the CordbThread for the thread that's deleted
//
// static
void CordbThread::DbgAssertThreadDeletedCallback(VMPTR_Thread vmThread, void * pUserData)
{
    CordbThread * pThis = reinterpret_cast<CordbThread *>(pUserData);
    INTERNAL_DAC_CALLBACK(pThis->GetProcess());

    VMPTR_Thread vmThreadDelete = pThis->m_vmThreadToken;

    CONSISTENCY_CHECK_MSGF((vmThread != vmThreadDelete),
        ("A Thread Exit event was sent, but it still shows up in the enumeration.\n vmThreadDelete=%p\n",
        VmPtrToCookie(vmThreadDelete)));
}

// Debug-only helper to Assert that this thread is no longer discoverable in DacDbi enumerations
// This is designed to enforce the code:IDacDbiInterface#Enumeration rules for enumerations.
void CordbThread::DbgAssertThreadDeleted()
{
    // Enumerate through all threads and ensure the deleted threads don't show up.
    GetProcess()->GetDAC()->EnumerateThreads(
        DbgAssertThreadDeletedCallback,
        this);
}
#endif // _DEBUG


//---------------------------------------------------------------------------------------
// Mark that this thread has an unhandled native exception on it.
//
// Arguments
//    pRecord - exception record of 2nd-chance exception that we're hijacking at. This will
//            get deep copied into the CordbThread object in case it's needed for hijacking later.
//
// Notes:
//    This bit is cleared in code:CordbThread::HijackForUnhandledException
void CordbThread::SetUnhandledNativeException(const EXCEPTION_RECORD * pExceptionRecord)
{
    m_fHasUnhandledException = true;

    if (m_pExceptionRecord == NULL)
    {
        m_pExceptionRecord = new EXCEPTION_RECORD(); // throws
    }
    memcpy(m_pExceptionRecord, pExceptionRecord, sizeof(EXCEPTION_RECORD));
}

//-----------------------------------------------------------------------------
// Returns true if the thread has an unhandled exception
// This is during the window after code:CordbThread::SetUnhandledNativeException is called,
// but before code:CordbThread::HijackForUnhandledException
bool CordbThread::HasUnhandledNativeException()
{
    return m_fHasUnhandledException;
}


//---------------------------------------------------------------------------------------
// Determine if the thread's latest exception is a managed exception
//
// Notes:
//    The CLR's UnhandledExceptionFilter has to make this same determination.
//
BOOL CordbThread::IsThreadExceptionManaged()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // A Thread's latest exception is managed if the VM Thread object has a managed object
    // for the thread's Current Exception property. The CLR's Exception system is very diligent
    // about tracking and clearing the thread's managed exception property. The runtime will clear
    // the object if the exception is caught by unmanaged code (it can do this in the 2nd-pass).

    // It's the presence of a throwable that makes the difference between a managed
    // exception event and an unmanaged exception event.

    VMPTR_OBJECTHANDLE vmObject = GetProcess()->GetDAC()->GetCurrentException(m_vmThreadToken);

    bool fHasThrowable = !vmObject.IsNull();

    return fHasThrowable;

}

// ----------------------------------------------------------------------------
// CordbThread::CreateCordbRegisterSet
//
// Description:
//    This is a private hook for the shim to create a CordbRegisterSet for a ShimChain.
//
// Arguments:
//    * pContext - the CONTEXT to be converted; this must be the leaf CONTEXT of a chain
//    * fLeaf    - whether the chain is the leaf chain or not
//    * reason   - the chain reason; this is needed for legacy reasons (see below)
//    * ppRegSet - out parameter; return the newly created ICDRegisterSet
//
// Notes:
//    * Note that the fQuickUnwind argument of the ctor of CordbRegisterSet is only true
//        for an enter-managed chain.  We need to keep the same behaviour here.  That's why we need the
//        chain reason.
//

void CordbThread::CreateCordbRegisterSet(DT_CONTEXT *            pContext,
                                         BOOL                    fLeaf,
                                         CorDebugChainReason     reason,
                                         ICorDebugRegisterSet ** ppRegSet)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM(GetProcess());

    IfFailThrow(EnsureThreadIsAlive());

    // The CordbRegisterSet is responsible for freeing this memory.
    NewHolder<DebuggerREGDISPLAY> pDRD(new DebuggerREGDISPLAY());

    // convert the CONTEXT to a DebuggerREGDISPLAY
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    pDAC->ConvertContextToDebuggerRegDisplay(pContext, pDRD, fLeaf);

    // create the CordbRegisterSet
    RSInitHolder<CordbRegisterSet> pRS(new CordbRegisterSet(pDRD,
                                                            this,
                                                            (fLeaf == TRUE),
                                                            (reason == CHAIN_ENTER_MANAGED),
                                                            true));
    pDRD.SuppressRelease();

    pRS.TransferOwnershipExternal(ppRegSet);
}

// ----------------------------------------------------------------------------
// CordbThread::ConvertFrameForILMethodWithoutMetadata
//
// Description:
//    This is a private hook for the shim to convert an ICDFrame into an ICDInternalFrame for a dynamic
//    method.  There are two cases where we need this:
//    1)  In Arrowhead, dynamic methods are exposed as first-class stack frames, not internal frames.  Thus,
//        the shim needs a way to convert an ICDNativeFrame for a dynamic method in Arrowhead to an
//        ICDInternalFrame of type STUBFRAME_LIGHTWEIGHT_FUNCTION in V2.  Furthermore, IL stubs,
//        which are also considered as a type of dynamic methods, are not exposed in V2 at all.
//
//    2)  In V2, PrestubMethodFrames (PMFs) can be exposed as one of two things: a chain of type
//        CHAIN_CLASS_INIT in most cases, or an internal frame of type STUBFRAME_LIGHTWEIGHT_FUNCTION if
//        the method being jitted is a dynamic method.  There is no way to make this distinction at the
//        public ICD level.
//
// Arguments:
//    * pNativeFrame    - the native frame to be converted
//    * ppInternalFrame - out parameter; the converted internal frame; could be NULL (see Notes below)
//
// Returns:
//    Return TRUE if conversion has occurred.  Note that even if the return value is TRUE, ppInternalFrame
//    could be NULL.  See Notes below.
//
// Notes:
//    * There are two main types of dynamic methods: ones which are generated by the runtime itself for
//        internal purposes (i.e. IL stubs), and ones which are generated by the user.  ppInternalFrame
//        is NULL for IL stubs.  We need this functionality because IL stubs are not exposed at all in V2.
//

BOOL CordbThread::ConvertFrameForILMethodWithoutMetadata(ICorDebugFrame *           pFrame,
                                                         ICorDebugInternalFrame2 ** ppInternalFrame2)
{
    PUBLIC_REENTRANT_API_ENTRY_FOR_SHIM(GetProcess());

    _ASSERTE(ppInternalFrame2 != NULL);
    *ppInternalFrame2 = NULL;

    HRESULT hr = E_FAIL;

    CordbFrame * pRealFrame = CordbFrame::GetCordbFrameFromInterface(pFrame);

    CordbInternalFrame * pInternalFrame = pRealFrame->GetAsInternalFrame();
    if (pInternalFrame != NULL)
    {
        // The input is an internal frame.

        // Check its frame type.
        CorDebugInternalFrameType type;
        hr = pInternalFrame->GetFrameType(&type);
        IfFailThrow(hr);

        if (type != STUBFRAME_JIT_COMPILATION)
        {
            // No conversion is necessary.
            return FALSE;
        }
        else
        {
            // We are indeed dealing with a PrestubMethodFrame.
            return pInternalFrame->ConvertInternalFrameForILMethodWithoutMetadata(ppInternalFrame2);
        }
    }
    else
    {
        // The input is a native frame.
        CordbNativeFrame * pNativeFrame = pRealFrame->GetAsNativeFrame();
        _ASSERTE(pNativeFrame != NULL);

        return pNativeFrame->ConvertNativeFrameForILMethodWithoutMetadata(ppInternalFrame2);
    }
}

//-----------------------------------------------------------------------------
// Hijack a thread at an unhandled exception. This lets it execute the
// CLR's Unhandled Exception Filter (which will send the managed 2nd-chance exception event)
//
// Notes:
//    OS will not execute Unhandled Exception Filter (UEF) when debugger is attached.
//    The CLR's UEF does useful work, like dispatching 2nd-chance managed exception event
//    and allowing Func-eval and Continuable Exceptions for unhandled exceptions.
//    So hijack the thread, and the hijack will then execute the CLR's UEF just
//    like the OS would.
void CordbThread::HijackForUnhandledException()
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    _ASSERTE(m_pExceptionRecord != NULL);

    _ASSERTE(m_fHasUnhandledException);
    m_fHasUnhandledException = false;


    ULONG32 dwThreadId = GetVolatileOSThreadID();

    // Note that the data-target is not atomic, and we have no rollback mechanism.
    // We have to do several writes. If the data-target fails the writes half-way through the
    // target will be inconsistent.

    // We don't bother remembering the original context. LS hijack will have the
    // context on its stack and will pass it to RS just like it does for filter-context.
    GetProcess()->GetDAC()->Hijack(
            m_vmThreadToken,
            dwThreadId,
            m_pExceptionRecord,
            NULL, // LS will have the context.
            0, // size of context
            EHijackReason::kUnhandledException,
            NULL,
            NULL);

    // Notify debugger to clear the exception.
    // This will invoke the data-target.
    GetProcess()->ContinueStatusChanged(dwThreadId, DBG_CONTINUE);
}


HRESULT CordbThread::GetProcess(ICorDebugProcess ** ppProcess)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppProcess, ICorDebugProcess **);
    FAIL_IF_NEUTERED(this);

    *ppProcess = GetProcess();
    GetProcess()->ExternalAddRef();

    return S_OK;
}

// Public implementation of ICorDebugThread::GetID
// Back in V1.0, GetID originally meant the OS thread ID that this managed thread was running on.
// In theory, that can change (fibers, logical thread scheduling, etc). However, in practice, in V1.0, it would
// not. Thus debuggers took a depedency on GetID being constant.
// In V2, this returns an opaque handle that is unique to this thread and stable for this thread's lifetime.
//
// Compare to code:CordbThread::GetVolatileOSThreadID, which returns the actual OS thread Id (which may change).
HRESULT CordbThread::GetID(DWORD * pdwThreadId)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(pdwThreadId, DWORD *);
    FAIL_IF_NEUTERED(this);

    *pdwThreadId = GetUniqueId();

    return S_OK;
}

// Returns a unique ID that's stable for the life of this thread.
// In a non-hosted scenarios, this can be the OS thread id.
DWORD CordbThread::GetUniqueId()
{
    return m_dwUniqueID;
}

// Implementation of public API, ICorDebugThread::GetHandle
// @dbgtodo  ICDThread - deprecate in V3, offload to Shim
HRESULT CordbThread::GetHandle(HANDLE * phThreadHandle)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(phThreadHandle, HANDLE *);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (GetProcess()->GetShim() == NULL)
    {
        _ASSERTE(!"CordbThread::GetHandle() should be not be called on the new architecture");
        *phThreadHandle = NULL;
        return E_NOTIMPL;
    }

#if !defined(FEATURE_DBGIPC_TRANSPORT_DI)
    HRESULT hr = S_OK;
    EX_TRY
    {
        HANDLE hThread;
        InternalGetHandle(&hThread);    // throws on error
        *phThreadHandle = hThread;
    }
    EX_CATCH_HRESULT(hr);
#else  // FEATURE_DBGIPC_TRANSPORT_DI
    // In the old SL implementation of Mac debugging, we return a thread handle faked up by the PAL on the Mac.
    // The returned handle is meaningless.  Here we explicitly return E_NOTIMPL.  We plan to deprecate this
    // function in Dev10 anyway.
    //
    // @dbgtodo  Mac - Check with VS to see if they need the thread handle, e.g. for waiting on thread
    // termination.
    HRESULT hr = E_NOTIMPL;
#endif // !FEATURE_DBGIPC_TRANSPORT_DI

    return hr;
}

// Note that we can return invalid handle
void CordbThread::InternalGetHandle(HANDLE * phThread)
{
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    RefreshHandle(phThread);
}

//---------------------------------------------------------------------------------------
//
// This is a simple helper to check if a frame lives on the stack of the current thread.
//
// Arguments:
//    pFrame - the stack frame to check
//
// Return Value:
//    whether the frame lives on the stack of the current thread
//
// Assumption:
//    This function assumes that the stack frames are valid, i.e. the stack frames have not been
//    made dirty since the last stackwalk.
//

bool CordbThread::OwnsFrame(CordbFrame * pFrame)
{
    // preliminary checking
    if ( (pFrame != NULL)           &&
         (!pFrame->IsNeutered())    &&
         (pFrame->m_pThread == this)
       )
    {
        //
        // Note that this is one of the two remaining places where we need to use the cached stack frames.
        // Theoretically, since this is not an exact check anyway, we could just use the thread's stack
        // range instead of looping through all the individual frames.  However, since we need to maintain
        // the stack frame cache for code:CordbThread::GetActiveFunctions, we might as well use the cache here.
        //

        // make sure this thread actually have frames to check
        if (m_stackFrames.Count() != 0)
        {
            // get the stack range of this thread
            FramePointer fpLeaf = (*(m_stackFrames.Get(0)))->GetFramePointer();
            FramePointer fpRoot = (*(m_stackFrames.Get(m_stackFrames.Count() - 1)))->GetFramePointer();

            FramePointer fpCurrent = pFrame->GetFramePointer();

            // compare the stack range against the frame pointer of the specified frame
            if (IsEqualOrCloserToLeaf(fpLeaf, fpCurrent) && IsEqualOrCloserToRoot(fpRoot, fpCurrent))
            {
                return true;
            }
        }
    }

    return false;
}

//---------------------------------------------------------------------------------------
//
// This routine is a internal helper function for ICorDebugThread2::GetTaskId.
//
// Arguments:
//    pHandle - return thread handle here after fetching from the left side.
//
// Return Value:
//    hr - It can fail with CORDBG_E_THREAD_NOT_SCHEDULED.
//
// Notes:
//    This method will most likely be deprecated in V3.0.  We can't always return the thread handle.
//    For example, what does it mean to return a thread handle in remote debugging scenarios?
//
void CordbThread::RefreshHandle(HANDLE * phThread)
{
    // here is where we will put code in to fetch the thread handle from the left side.
    // This should only happen when CLRTask is hosted.
    // Make sure that we are setting the right HR when thread is being switched out.
    THROW_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    if (phThread == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }
    *phThread = INVALID_HANDLE_VALUE;

    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    HANDLE hThread = pDAC->GetThreadHandle(m_vmThreadToken);

    _ASSERTE(hThread != INVALID_HANDLE_VALUE);
    PREFAST_ASSUME(hThread != NULL);

    // need to dup handle here
    if (hThread == m_hCachedOutOfProcThread)
    {
        *phThread = m_hCachedThread;
    }
    else
    {
        BOOL fSuccess = TRUE;
        if (m_hCachedThread != INVALID_HANDLE_VALUE)
        {
            // clear the previous cache
            CloseHandle(m_hCachedThread);
            m_hCachedOutOfProcThread = INVALID_HANDLE_VALUE;
            m_hCachedThread = INVALID_HANDLE_VALUE;
        }

        // now duplicate the out-of-proc handle
        fSuccess = DuplicateHandle(GetProcess()->UnsafeGetProcessHandle(),
                                   hThread,
                                   GetCurrentProcess(),
                                   &m_hCachedThread,
                                   NULL,
                                   FALSE,
                                   DUPLICATE_SAME_ACCESS);
        *phThread = m_hCachedThread;

        if (fSuccess)
        {
            m_hCachedOutOfProcThread = hThread;
        }
        else
        {
            ThrowLastError();
        }
    }
}   // CordbThread::RefreshHandle


//---------------------------------------------------------------------------------------
//
// This routine sets the debug state of a thread.
//
// Arguments:
//    state - The debug state to set to.
//
// Return Value:
//    Normal HRESULT semantics.
//
HRESULT CordbThread::SetDebugState(CorDebugThreadState state)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    LOG((LF_CORDB, LL_INFO1000, "CT::SDS: thread=0x%08x 0x%x, state=%d\n", this, m_id, state));

    // @dbgtodo- , sync - decide on how to suspend a thread. V2 leverages synchronization
    // (see below). For V3, do we just hard suspend the thread?
    if (GetProcess()->GetShim() == NULL)
    {
        return E_NOTIMPL;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            // This lets the debugger suspend / resume threads. This is only called when when the
            // target is already synchronized. That means all the threads are already suspended. So
            // setting the suspend bit here just means that the debugger's continue logic won't resume
            // this thread when we do a Continue.
            if ((state != THREAD_SUSPEND) && (state != THREAD_RUN))
            {
                ThrowHR(E_INVALIDARG);
            }

            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            pDAC->SetDebugState(m_vmThreadToken, state);

            m_debugState = state;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbThread::GetDebugState(CorDebugThreadState * pState)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(pState, CorDebugThreadState *);

    *pState = m_debugState;

    return S_OK;
}


// Public implementation of ICorDebugThread::GetUserState
// Arguments:
//    pState - out parameter; return the user state
//
// Return Value:
//    Return S_OK if the operation is successful.
//    Return E_INVALIDARG  if the out parameter is NULL.
//    Return other failure HRs returned by the call to the DDI.
HRESULT CordbThread::GetUserState(CorDebugUserState * pState)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pState, CorDebugUserState *);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (pState == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }
        *pState = GetUserState();
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Retrieve the user state of the current thread.
//
// Notes:
//    This caches results between continues. The cache is cleared when the target continues or is flushed.
//    See code:CordbThread::CleanupStack, code:CordbThread::MarkStackFramesDirty
//
CorDebugUserState CordbThread::GetUserState()
{
    if (m_userState == kInvalidUserState)
    {
        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        m_userState = pDAC->GetUserState(m_vmThreadToken);
    }

    return m_userState;
}


//---------------------------------------------------------------------------------------
//
// This routine finds and returns the current exception off of a thread.
//
// Arguments:
//    ppExceptionObject - OUT: Space for storing the exception found on the thread as a value.
//
// Return Value:
//    Normal HRESULT semantics.
//
HRESULT CordbThread::GetCurrentException(ICorDebugValue ** ppExceptionObject)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppExceptionObject, ICorDebugValue **);
    *ppExceptionObject = NULL;

    EX_TRY
    {
        if (!HasException())
        {
            //
            // Go to the LS and retrieve any exception object.
            //
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            VMPTR_OBJECTHANDLE vmObjHandle = pDAC->GetCurrentException(m_vmThreadToken);

            if (vmObjHandle.IsNull())
            {
                hr = S_FALSE;
            }
            else
            {
#if defined(_DEBUG)
                // Since we know an exception is in progress on this thread, our assumption about the
                // thread's current AppDomain should be correct
                VMPTR_AppDomain vmAppDomain = pDAC->GetCurrentAppDomain(m_vmThreadToken);
                _ASSERTE(GetAppDomain()->GetADToken() == vmAppDomain);
#endif // _DEBUG

                m_vmExcepObjHandle = vmObjHandle;
            }
        }

        if (hr == S_OK)
        {
            // We've believe this assert may fire in the wild.
            // We've seen m_vmExcepObjHandle null in retail builds after stack overflow.
            _ASSERTE(!m_vmExcepObjHandle.IsNull());

            ICorDebugReferenceValue * pRefValue = NULL;
            hr = CordbReferenceValue::BuildFromGCHandle(GetAppDomain(), m_vmExcepObjHandle, &pRefValue);
            *ppExceptionObject = pRefValue;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbThread::ClearCurrentException()
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // This API is not implemented. For Continuable Exceptions, see InterceptCurrentException.
    // @todo - should it return E_NOTIMPL?
    return S_OK;
}

HRESULT CordbThread::CreateStepper(ICorDebugStepper ** ppStepper)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppStepper, ICorDebugStepper **);

    CordbStepper * pStepper = new (nothrow) CordbStepper(this, NULL);

    if (pStepper == NULL)
    {
        return E_OUTOFMEMORY;
    }

    pStepper->ExternalAddRef();
    *ppStepper = pStepper;

    return S_OK;
}

//Returns true if current user state of a thread is USER_WAIT_SLEEP_JOIN
bool CordbThread::IsThreadWaitingOrSleeping()
{
    CorDebugUserState userState = m_userState;
    if (userState == kInvalidUserState)
    {
        //If m_userState is not ready, we'll read from DAC only part of it which
        //is important for us now, bacuase we don't want possible side effects
        //of reading USER_UNSAFE_POINT flag.
        //We don't cache the value, because it's potentially incomplete.
        IDacDbiInterface *pDAC = GetProcess()->GetDAC();
        userState = pDAC->GetPartialUserState(m_vmThreadToken);
    }

    return (userState & USER_WAIT_SLEEP_JOIN) != 0;
}

//----------------------------------------------------------------------------
// check if the thread is dead
//
// Returns: true if the thread is dead.
//
bool CordbThread::IsThreadDead()
{
    return GetProcess()->GetDAC()->IsThreadMarkedDead(m_vmThreadToken);
}

// Helper to return CORDBG_E_BAD_THREAD_STATE if IsThreadDead
//
// Notes:
//   IsThreadDead queries the VM Thread's actual state, regardless of what ExitThread
//   callbacks have or have not been sent / queued / dispatched.
HRESULT CordbThread::EnsureThreadIsAlive()
{
    if (IsThreadDead())
    {
        return CORDBG_E_BAD_THREAD_STATE;
    }
    else
    {
        return S_OK;
    }
}

// ----------------------------------------------------------------------------
// CordbThread::EnumerateChains
//
// Description:
//    Create and return an ICDChainEnum for enumerating chains on the stack.  Since chains have been
//    deprecated in Arrowhead, this function returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppChains - out parameter; return the ICDChainEnum
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppChains is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbThread is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbThread::EnumerateChains(ICorDebugChainEnum ** ppChains)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppChains, ICorDebugChainEnum **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        *ppChains = NULL;

        if (GetProcess()->GetShim() != NULL)
        {
            hr = EnsureThreadIsAlive();

            if (SUCCEEDED(hr))
            {
                // use the shim to create an ICDChainEnum
                PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
                ShimStackWalk * pSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);
                pSW->EnumerateChains(ppChains);
            }
        }
        else
        {
            // This is the Arrowhead case, where ICDChain has been deprecated.
            hr = E_NOTIMPL;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// ----------------------------------------------------------------------------
// CordbThread::GetActiveChain
//
// Description:
//    Retrieve the leaf chain on this thread.  Since chains have been  deprecated in Arrowhead,
//    this function returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppChain - out parameter; return the leaf chain
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppChain is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbThread is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbThread::GetActiveChain(ICorDebugChain ** ppChain)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        *ppChain = NULL;
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            if (GetProcess()->GetShim() != NULL)
            {
                // use the shim to retrieve the leaf chain
                PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
                ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);
                pSSW->GetActiveChain(ppChain);
            }
            else
            {
                // This is the Arrowhead case, where ICDChain has been deprecated.
                hr = E_NOTIMPL;
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbThread::GetActiveFrame
//
// Description:
//    Retrieve the leaf frame on this thread.  Unfortunately, this is one of the cases where we need to
//    do different things depending on whether there is a shim.  See the Notes below.
//
// Arguments:
//    * ppFrame - out parameter; return the leaf frame
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbThread is neutered.
//    Also return whatever CreateStackWalk() and GetFrame() return if they fail.
//
// Notes:
//    In V2, we return NULL if the leaf frame is not in the leaf chain, i.e. if the leaf chain is
//    empty.  Note that managed chains are never empty.  Also, in V2 it is possible that this API
//    will return an internal frame as the active frame on a thread.
//
//    The Arrowhead implementation two breaking changes:
//    1) It never returns an internal frame.
//    2) We return a frame if the leaf frame is managed.  Otherwise, we return NULL.
//

HRESULT CordbThread::GetActiveFrame(ICorDebugFrame ** ppFrame)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppFrame, ICorDebugFrame **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        *ppFrame = NULL;
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            if (GetProcess()->GetShim() != NULL)
            {
                PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
                ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);
                pSSW->GetActiveFrame(ppFrame);
            }
            else
            {
                // This is the Arrowhead case.  We could call RefreshStack() here, but since we only need the
                // leaf frame, there is no point in walking the entire stack.
                RSExtSmartPtr<ICorDebugStackWalk> pSW;
                hr = CreateStackWalk(&pSW);
                IfFailThrow(hr);

                hr = pSW->GetFrame(ppFrame);
                IfFailThrow(hr);
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbThread::GetActiveRegister
//
// Description:
//    In V2, retrieve the ICDRegisterSet for the leaf chain.  In Arrowhead, retrieve the ICDRegisterSet
//    for the leaf CONTEXT.
//
// Arguments:
//    * ppRegisters - out parameter; return the ICDRegister
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbThread is neutered.
//    Also return whatever CreateStackWalk() and GetContext() return if they fail.
//

HRESULT CordbThread::GetRegisterSet(ICorDebugRegisterSet ** ppRegisters)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(ppRegisters, ICorDebugRegisterSet **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        *ppRegisters = NULL;
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            if (GetProcess()->GetShim() != NULL)
            {
                // use the shim to retrieve the active ICDRegisterSet
                PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
                ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);
                pSSW->GetActiveRegisterSet(ppRegisters);
            }
            else
            {
                // This is the Arrowhead case.  We could call RefreshStack() here, but since we only need the
                // leaf frame, there is no point in walking the entire stack.
                RSExtSmartPtr<ICorDebugStackWalk> pSW;
                hr = CreateStackWalk(&pSW);
                IfFailThrow(hr);

                // retrieve the leaf CONTEXT
                DT_CONTEXT ctx;
                hr = pSW->GetContext(CONTEXT_FULL, sizeof(ctx), NULL, reinterpret_cast<BYTE *>(&ctx));
                IfFailThrow(hr);

                // the CordbRegisterSet is responsible for freeing this memory
                NewHolder<DebuggerREGDISPLAY> pDRD(new DebuggerREGDISPLAY());

                // convert the CONTEXT to a DebuggerREGDISPLAY
                IDacDbiInterface * pDAC = GetProcess()->GetDAC();
                pDAC->ConvertContextToDebuggerRegDisplay(&ctx, pDRD, true);

                // create the CordbRegisterSet
                RSInitHolder<CordbRegisterSet> pRS(new CordbRegisterSet(pDRD,
                                                                        this,
                                                                        true,   // active
                                                                        false,  // !fQuickUnwind
                                                                        true)); // own DRD memory
                pDRD.SuppressRelease();

                pRS.TransferOwnershipExternal(ppRegisters);
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbThread::CreateEval(ICorDebugEval ** ppEval)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppEval, ICorDebugEval **);

    CordbEval * pEval = new (nothrow) CordbEval(this);
    if (pEval == NULL)
    {
        return E_OUTOFMEMORY;
    }

    pEval->ExternalAddRef();
    *ppEval = static_cast<ICorDebugEval *>(pEval);

    return S_OK;
}

// DAC check

// Double check our results w/ DAC.
// This gives DAC some great coverage.
// Given an IP and the md token (that the RS obtained), use DAC to lookup the md token. Then
// we can compare DAC & the RS and make sure DACs working.
void CheckAgainstDAC(CordbFunction * pFunc, void * pIP, mdMethodDef mdExpected)
{
    // This is a hook to add DAC checks against a {function, ip}
}


//---------------------------------------------------------------------------------------
//
// Internal function to build up a stack trace.
//
//
// Return Value:
//    S_OK on success.
//
// Assumptions:
//    Process is stopped.
//
// Notes:
//    Send a IPC events to the LS to build up the stack.
//
//---------------------------------------------------------------------------------------
void CordbThread::RefreshStack()
{
    THROW_IF_NEUTERED(this);

    // We must have the Stop-Go lock to change our thread's stack-state.
    // Also, our caller should have guaranteed that we're synced. And b/c we hold the stop-go lock,
    // that shouldn't have changed.
    // INTERNAL_SYNC_API_ENTRY() checks that we have the lock and that we are synced.
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    // bail out early if the stack hasn't changed
    if (m_fFramesFresh)
    {
        return;
    }

    HRESULT hr = S_OK;

    //
    // Clean up old snapshot.
    //

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());

    // clear the stack frame cache
    ClearStackFrameCache();

    //
    // If we don't have a debugger thread token, then this thread has never
    // executed managed code and we have no frame information for it.
    //
    if (m_vmThreadToken.IsNull())
    {
        ThrowHR(E_FAIL);
    }

    // walk the stack using the V3 API and populate the stack frame cache
    RSInitHolder<CordbStackWalk> pSW(new CordbStackWalk(this));
    pSW->Init();
    do
    {
        RSExtSmartPtr<ICorDebugFrame> pIFrame;
        hr = pSW->GetFrame(&pIFrame);
        IfFailThrow(hr);

        if (pIFrame != NULL)
        {
            // add the stack frame to the cache
            CordbFrame ** ppCFrame = m_stackFrames.AppendThrowing();
            *ppCFrame = CordbFrame::GetCordbFrameFromInterface(pIFrame);

            // Now that we have saved the pointer, increment the ref count.
            // This has to match the InternalRelease() in code:CordbThread::ClearStackFrameCache.
            (*ppCFrame)->InternalAddRef();
        }

        // advance to the next frame
        hr = pSW->Next();
        IfFailThrow(hr);
    }
    while (hr != CORDBG_S_AT_END_OF_STACK);

    m_fFramesFresh = true;
}


//---------------------------------------------------------------------------------------
//
// This function is used to invalidate and clean up the cached stack trace.
//

void CordbThread::CleanupStack()
{
    _ASSERTE(GetProcess()->GetProcessLock()->HasLock());

    // Neuter outstanding CordbChainEnums, CordbFrameEnums, some CordbTypeEnums, and some CordbValueEnums.
    m_RefreshStackNeuterList.NeuterAndClear(GetProcess());

    m_fContextFresh = false;            // invalidate the cached active CONTEXT
    m_vmLeftSideContext = VMPTR_CONTEXT::NullPtr(); // set the LS pointer to the active CONTEXT to NULL
    m_fFramesFresh = false;             // invalidate the cached stack trace (frames & chains)
    m_userState = kInvalidUserState;                // clear the cached user state

    // tell the shim to flush its caches as well
    if (GetProcess()->GetShim() != NULL)
    {
        GetProcess()->GetShim()->NotifyOnStackInvalidate();
    }
}

// Notifying the thread that the process is being continued.
// This will cause our caches to get invalidated without actually cleaning the caches.
void CordbThread::MarkStackFramesDirty()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    // invalidate the cached floating point state
    m_fFloatStateValid = false;

    // This flag is only true between the window when we get an exception callback and
    // when we call continue.  Since this function is only called when we continue, we
    // need to reset this flag here.  Note that in the case of an outstanding funceval,
    // we'll set this flag again when the funceval is completed.
    m_fException = false;

    // Clear the stashed EnC remap IP address if any
    // This is important to ensure we don't try to write into LS memory which is no longer
    // being used to hold the remap IP.
    m_EnCRemapFunctionIP = NULL;

    m_fContextFresh = false;        // invalidate the cached active CONTEXT
    m_vmLeftSideContext = VMPTR_CONTEXT::NullPtr(); // set the LS pointer to the active CONTEXT to NULL
    m_fFramesFresh = false;         // invalidate the cached stack trace (frames & chains)
    m_userState = kInvalidUserState;                // clear the cached user state

    m_RefreshStackNeuterList.NeuterAndClear(GetProcess());

    // tell the shim to flush its caches as well
    if (GetProcess()->GetShim() != NULL)
    {
        GetProcess()->GetShim()->NotifyOnStackInvalidate();
    }
}

// Set that there's an outstanding exception on this thread.
// This can be called when the process object receives an exception notification.
// This is cleared in code:CordbThread::MarkStackFramesDirty.
void CordbThread::SetExInfo(VMPTR_OBJECTHANDLE vmExcepObjHandle)
{
    m_fException = true;
    m_vmExcepObjHandle = vmExcepObjHandle;

    // CordbThread::GetCurrentException assumes that we always have a m_vmExcepObjHandle when at an exception.
    // Push that assert up here.
    _ASSERTE(!m_vmExcepObjHandle.IsNull());
}


// ----------------------------------------------------------------------------
// CordbThread::FindFrame
//
// Description:
// Given a FramePointer, find the matching CordbFrame.
//
// Arguments:
//    * ppFrame - out parameter; the CordbFrame to be returned
//    * fp      - the input FramePointer
//
// Return Value:
//    Return S_OK on success.
//    Return E_FAIL on failure.
//
// Assumptions:
//    * This function is only called from the shim.
//
// Notes:
//    * Currently this function is only used by the shim to map the FramePointer it gets via the
//        DB_IPCE_EXCEPTION_CALLBACK2 callback.  When we figure out what to do with the
//        DB_IPCE_EXCEPTION_CALLBACK2, we should remove this function.
//

HRESULT CordbThread::FindFrame(ICorDebugFrame ** ppFrame, FramePointer fp)
{
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    _ASSERTE(ppFrame != NULL);
    *ppFrame = NULL;

    _ASSERTE(GetProcess()->GetShim() != NULL);

    PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
    ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);

    for (UINT32 i = 0; i < pSSW->GetFrameCount(); i++)
    {
        ICorDebugFrame * pIFrame = pSSW->GetFrame(i);
        CordbFrame * pCFrame = CordbFrame::GetCordbFrameFromInterface(pIFrame);

#if defined(HOST_64BIT)
        // On 64-bit we can simply compare the FramePointer.
        if (pCFrame->GetFramePointer() == fp)
#else  // !HOST_64BIT
        // On other platforms, we need to do a more elaborate check.
        if (pCFrame->IsContainedInFrame(fp))
#endif // HOST_64BIT
        {
            *ppFrame = pIFrame;
            (*ppFrame)->AddRef();
            return S_OK;
        }
    }

    // Cannot find the frame.
    return E_FAIL;
}



#if defined(CROSS_COMPILE) && (defined(TARGET_ARM64) || defined(TARGET_ARM))
extern "C" double FPFillR8(void* pFillSlot)
{
    _ASSERTE(!"nyi for platform");
    return 0;
}
#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM)
extern "C" double FPFillR8(void* pFillSlot);
#endif


#if defined(TARGET_X86)

// CordbThread::Get32bitFPRegisters
// Converts the values in the floating point register area of the context to real number values. See
// code:CordbThread::LoadFloatState for more details.
// Arguments:
//     input:  pContext
//     output: none (initializes m_floatValues)

void CordbThread::Get32bitFPRegisters(CONTEXT * pContext)
{
    // On X86, we get the values by saving our current FPU state, loading
    // the other thread's FPU state into our own, saving out each
    // value off the FPU stack, and then restoring our FPU state.
    //
    FLOATING_SAVE_AREA floatarea = pContext->FloatSave; // copy FloatSave

    //
    // Take the TOP out of the FPU status word. Note, our version of the
    // stack runs from 0->7, not 7->0...
    //
    unsigned int floatStackTop = 7 - ((floatarea.StatusWord & 0x3800) >> 11);

    FLOATING_SAVE_AREA currentFPUState;

#ifdef _MSC_VER
    __asm fnsave currentFPUState // save the current FPU state.
#else
    __asm__ __volatile__
    (
        "  fnsave %0\n" \
        : "=m"(currentFPUState)
    );
#endif

    floatarea.StatusWord &= 0xFF00; // remove any error codes.
    floatarea.ControlWord |= 0x3F; // mask all exceptions.

    // the x86 FPU stores real numbers as 10 byte values in IEEE format. Here we use
    // the hardware to convert these to doubles.

    // @dbgtodo Microsoft crossplat: the conversion from a series of bytes to a floating
    // point value will need to be done with an explicit conversion routine to unpack
    // the IEEE format and compute the real number value represented.

#ifdef _MSC_VER
    __asm
    {
        fninit
        frstor floatarea          ;; reload the threads FPU state.
    }
#else
    __asm__
    (
        "  fninit\n" \
        "  frstor %0\n" \
        : /* no outputs */
        : "m"(floatarea)
    );
#endif

    unsigned int i;

    for (i = 0; i <= floatStackTop; i++)
    {
        double td = 0.0;
        __asm fstp td // copy out the double
        m_floatValues[i] = td;
    }

#ifdef _MSC_VER
    __asm
    {
        fninit
        frstor currentFPUState    ;; restore our saved FPU state.
    }
#else
    __asm__
    (
        "  fninit\n" \
        "  frstor %0\n" \
        : /* no outputs */
        : "m"(currentFPUState)
    );
#endif

    m_fFloatStateValid = true;
    m_floatStackTop = floatStackTop;
} // CordbThread::Get32bitFPRegisters

#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM)

// CordbThread::Get64bitFPRegisters
// Converts the values in the floating point register area of the context to real number values. See
// code:CordbThread::LoadFloatState for more details.
// Arguments:
//     input:  pFPRegisterBase - starting address of the floating point register storage of the CONTEXT
//             registerSize    - the size of a floating point register
//             start           - the index into m_floatValues where we start initializing. For amd64, we start
//                               at the beginning, but for ia64, the first two registers have fixed values,
//                               so we start at two.
//             nRegisters      - the number of registers to be initialized
//     output: none (initializes m_floatValues)

void CordbThread::Get64bitFPRegisters(FPRegister64 * rgContextFPRegisters, int start, int nRegisters)
{
    // make sure no one has changed the type definition for 64-bit FP registers
    _ASSERTE(sizeof(FPRegister64) == 16);
    // We convert and copy all the fp registers.
    for (int reg = start; reg < nRegisters; reg++)
    {
        // @dbgtodo Microsoft crossplat: the conversion from a FLOAT128 or M128A struct to a floating
        // point value will need to be done with an explicit conversion routine instead
        // of the call to FPFillR8
        m_floatValues[reg] = FPFillR8(&rgContextFPRegisters[reg - start]);
    }
} // CordbThread::Get64bitFPRegisters

#endif // TARGET_X86

// CordbThread::LoadFloatState
// Initializes the float state members of this instance of CordbThread. This function gets the context and
// converts the floating point values from their context representation to a real number value. Floating
// point numbers are represented in IEEE format on all current platforms. We store them in the context as a
// pair of 64-bit integers (IA64 and AMD64) or a series of bytes (x86). Rather than unpack them explicitly
// and do the appropriate mathematical operations to produce the corresponding floating point value, we let
// the hardware do it instead. We load a floating point register with the representation from the context
// and then store it in m_floatValues. Using the hardware is obviously a huge perf win. If/when we make
// cross-plat work, we should at least code necessary conversion routines in assembly. Even with cross-plat,
// we can probably still use the hardware in most cases, as long as the size is appropriate.
//
// Arguments: none
// Return Value: none (initializes data members)
// Note: Throws

void CordbThread::LoadFloatState()
{
    THROW_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    DT_CONTEXT  tempContext;
    GetProcess()->GetDAC()->GetContext(m_vmThreadToken, &tempContext);

#if defined(TARGET_X86)
    Get32bitFPRegisters((CONTEXT*) &tempContext);
#elif defined(TARGET_AMD64)
    // we have no fixed-value registers, so we begin with the first one and initialize all 16
    Get64bitFPRegisters((FPRegister64*) &(tempContext.Xmm0), 0, 16);
#elif defined(TARGET_ARM64)
    Get64bitFPRegisters((FPRegister64*) &(tempContext.V), 0, 32);
#elif defined (TARGET_ARM)
    Get64bitFPRegisters((FPRegister64*) &(tempContext.D), 0, 32);
#else
    _ASSERTE(!"nyi for platform");
#endif // !TARGET_X86

    m_fFloatStateValid = true;
} // CordbThread::LoadFloatState


const bool SetIP_fCanSetIPOnly = TRUE;
const bool SetIP_fSetIP = FALSE;

const bool SetIP_fIL = TRUE;
const bool SetIP_fNative = FALSE;

//---------------------------------------------------------------------------------------
//
// Issues a SetIP command to the left-side and returns the result
//
// Arguments:
//    fCanSetIPOnly - TRUE if only to do the setip command and not refresh stacks as well.
//    debuggerModule - LS token to the debugger module.
//    mdMethod - Metadata token for the method.
//    nativeCodeJITInfoToken - LS token to the DebuggerJitInfo for the method.
//    offset - Offset within the method to set the IP to.
//    fIsIl - Is this an IL offset?
//
// Return Value:
//    S_OK on success.
//
HRESULT CordbThread::SetIP(bool fCanSetIPOnly,
                           CordbNativeCode * pNativeCode,
                           SIZE_T offset,
                           bool fIsIL)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);


    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VMPTR_DomainAssembly vmDomainAssembly = pNativeCode->GetModule()->m_vmDomainAssembly;
    _ASSERTE(!vmDomainAssembly.IsNull());

    // If this thread is stopped due to an exception, never allow SetIP
    if (HasException())
    {
       return (CORDBG_E_SET_IP_NOT_ALLOWED_ON_EXCEPTION);
    }

    DebuggerIPCEvent event;
    GetProcess()->InitIPCEvent(&event, DB_IPCE_SET_IP, true, GetAppDomain()->GetADToken());
    event.SetIP.fCanSetIPOnly = fCanSetIPOnly;
    event.SetIP.vmThreadToken = m_vmThreadToken;
    event.SetIP.vmDomainAssembly = vmDomainAssembly;
    event.SetIP.mdMethod = pNativeCode->GetMetadataToken();
    event.SetIP.vmMethodDesc = pNativeCode->GetVMNativeCodeMethodDescToken();
    event.SetIP.startAddress = pNativeCode->GetAddress();
    event.SetIP.offset = offset;
    event.SetIP.fIsIL = fIsIL;


    LOG((LF_CORDB, LL_INFO10000, "[%x] CT::SIP: Info:thread:0x%x"
        "mod:0x%x  MethodDef:0x%x offset:0x%x  il?:0x%x\n",
         GetCurrentThreadId(),
         VmPtrToCookie(m_vmThreadToken),
         VmPtrToCookie(vmDomainAssembly),
         pNativeCode->GetMetadataToken(),
         offset,
         fIsIL));

    LOG((LF_CORDB, LL_INFO10000, "[%x] CT::SIP: sizeof(DebuggerIPCEvent):0x%x **********\n",
        sizeof(DebuggerIPCEvent)));

    HRESULT hr = GetProcess()->m_cordb->SendIPCEvent(GetProcess(), &event, sizeof(DebuggerIPCEvent));

    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(event.type == DB_IPCE_SET_IP);

    if (!fCanSetIPOnly && SUCCEEDED(event.hr))
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        CleanupStack();
    }

    return ErrWrapper(event.hr);
}

// Get the context from a thread in managed code.
// This thread should be stopped gracefully by the LS in managed code.
HRESULT CordbThread::GetManagedContext(DT_CONTEXT ** ppContext)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    if (ppContext == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }

    *ppContext = NULL;
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // Each CordbThread object allocates the m_pContext's DT_CONTEXT structure only once, the first time GetContext is
    // invoked.
    if(m_pContext == NULL)
    {
        // Throw if the allocation fails.
        m_pContext = reinterpret_cast<DT_CONTEXT *>(new BYTE[sizeof(DT_CONTEXT)]);
    }

    HRESULT hr = S_OK;

    if (m_fContextFresh == false)
    {
        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        m_vmLeftSideContext = pDAC->GetManagedStoppedContext(m_vmThreadToken);

        if (m_vmLeftSideContext.IsNull())
        {
            // We don't have a context in managed code.
            ThrowHR(CORDBG_E_CONTEXT_UNVAILABLE);
        }
        else
        {
            LOG((LF_CORDB, LL_INFO1000, "CT::GC: getting context from left side pointer.\n"));

            // The thread we're examining IS handling an exception, So grab the CONTEXT of the exception, NOT the
            // currently executing thread's CONTEXT (which would be the context of the exception handler.)
            hr = GetProcess()->SafeReadThreadContext(m_vmLeftSideContext.ToLsPtr(), m_pContext);
            IfFailThrow(hr);
        }

        // m_fContextFresh should be marked false when CleanupStack, MarkAllFramesAsDirty, etc get called.
        m_fContextFresh = true;
    }

    _ASSERTE(SUCCEEDED(hr));
    (*ppContext) = m_pContext;

    return hr;
}

HRESULT CordbThread::SetManagedContext(DT_CONTEXT * pContext)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    if(pContext == NULL)
    {
        ThrowHR(E_INVALIDARG);
    }

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    m_vmLeftSideContext = pDAC->GetManagedStoppedContext(m_vmThreadToken);

    if (m_vmLeftSideContext.IsNull())
    {
        ThrowHR(CORDBG_E_CONTEXT_UNVAILABLE);
    }
    else
    {
        // The thread we're examining IS handling an exception, So set the CONTEXT of the exception, NOT the currently
        // executing thread's CONTEXT (which would be the context of the exception handler.)
        //
        // Note: we read the remote context and merge the new one in, then write it back. This ensures that we don't
        // write too much information into the remote process.
        DT_CONTEXT tempContext = { 0 };
        hr = GetProcess()->SafeReadThreadContext(m_vmLeftSideContext.ToLsPtr(), &tempContext);
        IfFailThrow(hr);

        CORDbgCopyThreadContext(&tempContext, pContext);

        hr = GetProcess()->SafeWriteThreadContext(m_vmLeftSideContext.ToLsPtr(), &tempContext);
        IfFailThrow(hr);

        // @todo - who's updating the regdisplay to guarantee that's in sync w/ our new context?
    }

    _ASSERTE(SUCCEEDED(hr));
    if (m_fContextFresh && (m_pContext != NULL))
    {
        *m_pContext = *pContext;
    }

    return hr;
}


HRESULT CordbThread::GetAppDomain(ICorDebugAppDomain ** ppAppDomain)
{
    // We don't use the cached m_pAppDomain pointer here because it might be incorrect
    // if the thread has transitioned to another domain but we haven't received any events
    // from it yet.  So we need to ask the left-side for the current domain.
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this);
    {
        ValidateOrThrow(ppAppDomain);
        *ppAppDomain = NULL;
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            CordbAppDomain * pAppDomain = NULL;
            hr = GetCurrentAppDomain(&pAppDomain);
            IfFailThrow(hr);
            _ASSERTE( pAppDomain != NULL );

            *ppAppDomain = static_cast<ICorDebugAppDomain *> (pAppDomain);
            pAppDomain->ExternalAddRef();
        }
    }
    PUBLIC_API_END(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Issues a get appdomain command and returns it.
//
// Arguments:
//    ppAppDomain - OUT: Space for storing the app domain of this thread.
//
// Return Value:
//    S_OK on success.
//
HRESULT CordbThread::GetCurrentAppDomain(CordbAppDomain ** ppAppDomain)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_API_ENTRY(GetProcess());

    *ppAppDomain = NULL;

    HRESULT hr = S_OK;
    EX_TRY
    {
        // @dbgtodo  ICDThread - push this up
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            VMPTR_AppDomain vmAppDomain = pDAC->GetCurrentAppDomain(m_vmThreadToken);

            CordbAppDomain * pAppDomain = GetProcess()->LookupOrCreateAppDomain(vmAppDomain);
            _ASSERTE(pAppDomain != NULL);     // we should be aware of all AppDomains

            *ppAppDomain = pAppDomain;
        }
    }
    EX_CATCH_HRESULT(hr);

    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Issues a get_object command and returns the thread object as a value.
//
// Arguments:
//    ppThreadObject - OUT: Space for storing the thread object of this thread as a value
//
// Return Value:
//    S_OK on success.
//
HRESULT CordbThread::GetObject(ICorDebugValue ** ppThreadObject)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppThreadObject, ICorDebugObjectValue **);

    // Default to NULL
    *ppThreadObject = NULL;

    HRESULT hr = S_OK;
    EX_TRY
    {
        // @dbgtodo  ICDThread - push this up
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            VMPTR_OBJECTHANDLE vmObjHandle = pDAC->GetThreadObject(m_vmThreadToken);
            if (vmObjHandle.IsNull())
            {
                ThrowHR(E_FAIL);
            }

            // We create the object relative to the current AppDomain of the thread
            // Thread objects aren't really agile (eg. their m_Context field is domain-bound and
            // fixed up manually during transitions).  This means that a thread object can only
            // be used in the domain the thread was in when the object was created.
            VMPTR_AppDomain vmAppDomain = pDAC->GetCurrentAppDomain(m_vmThreadToken);

            CordbAppDomain * pThreadCurrentDomain = NULL;
            pThreadCurrentDomain = GetProcess()->m_appDomains.GetBaseOrThrow(VmPtrToCookie(vmAppDomain));
            _ASSERTE(pThreadCurrentDomain != NULL);     // we should be aware of all AppDomains

            if (pThreadCurrentDomain == NULL)
            {
                // fall back to some domain to avoid crashes in retail -
                // safe enough for getting the name of the thread etc.
                pThreadCurrentDomain = GetProcess()->GetDefaultAppDomain();
            }

            lockHolder.Release();

            ICorDebugReferenceValue * pRefValue = NULL;
            hr = CordbReferenceValue::BuildFromGCHandle(pThreadCurrentDomain, vmObjHandle, &pRefValue);
            *ppThreadObject = pRefValue;
        }
    }
    EX_CATCH_HRESULT(hr);

    // Don't return a null pointer with S_OK.
    _ASSERTE((hr != S_OK) || (*ppThreadObject != NULL));
    return hr;
}

/*
 *
 * GetActiveFunctions
 *
 *  This routine is the interface function for ICorDebugThread2::GetActiveFunctions.
 *
 * Parameters:
 *  cFunctions - the count of the number of COR_ACTIVE_FUNCTION in pFunctions.  Zero
 *               indicates no pFunctions buffer.
 *  pcFunctions - pointer to storage for the count of elements filled in to pFunctions, or
 *                count that would be needed to fill pFunctions, if cFunctions is 0.
 *  pFunctions - buffer to store results.  May be NULL.
 *
 * Return Value:
 *  HRESULT from the helper routine.
 *
 */

HRESULT CordbThread::GetActiveFunctions(
    ULONG32 cFunctions,
    ULONG32 * pcFunctions,
    COR_ACTIVE_FUNCTION pFunctions[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ULONG32 index;
    ULONG32 iRealIndex;
    ULONG32 last;

    if (((cFunctions != 0) && (pFunctions == NULL)) || (pcFunctions == NULL))
    {
        return E_INVALIDARG;
    }

    //
    // Default to 0
    //
    *pcFunctions = 0;

    // @dbgtodo  synchronization - The ATT macro may slip the thread to a sychronized state.  The
    // synchronization feature crew needs to figure out what to do here.  Then we can use the
    // PUBLIC_API_BEGIN macro in this function.
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        if (IsThreadDead())
        {
            //
            // Return zero active functions on this thread.
            //
            hr = S_OK;
        }
        else
        {
            ULONG32 cAllFrames   = 0;       // the total number of frames (stack frames and internal frames)
            ULONG32 cStackFrames = 0;       // the number of stack frames
            ShimStackWalk * pSSW = NULL;

            if (GetProcess()->GetShim() != NULL)
            {
                PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
                pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(this);

                // initialize the frame counts
                cAllFrames = pSSW->GetFrameCount();
                for (ULONG32 i = 0; i < cAllFrames; i++)
                {
                    // filter out internal frames
                    if (CordbFrame::GetCordbFrameFromInterface(pSSW->GetFrame(i))->GetAsNativeFrame() != NULL)
                    {
                        cStackFrames += 1;
                    }
                }

                _ASSERTE(cStackFrames <= cAllFrames);
            }
            else
            {
                RefreshStack();

                cAllFrames   = m_stackFrames.Count();
                cStackFrames = cAllFrames;

                // In Arrowhead, the stackwalking API doesn't return internal frames,
                // so the frame counts should be equal.
                _ASSERTE(cStackFrames == cAllFrames);
            }

            *pcFunctions = cStackFrames;

            //
            // If all we want is the count, then return that.
            //
            if ((pFunctions == NULL) || (cFunctions == 0))
            {
                hr = S_OK;
            }
            else
            {
                //
                // Now go down list of frames, storing information
                //
                last = (cFunctions < cStackFrames) ? cFunctions : cStackFrames;
                iRealIndex = 0;
                index =0;

                while((index < last) && (iRealIndex < cAllFrames))
                {
                    CordbFrame * pThisFrame = NULL;
                    if (GetProcess()->GetShim())
                    {
                        _ASSERTE(pSSW != NULL);
                        pThisFrame = CordbFrame::GetCordbFrameFromInterface(pSSW->GetFrame(iRealIndex));
                    }
                    else
                    {
                        pThisFrame = *(m_stackFrames.Get(iRealIndex));
                        _ASSERTE(pThisFrame->GetAsNativeFrame() != NULL);
                    }

                    iRealIndex++;

                    CordbNativeFrame * pNativeFrame = pThisFrame->GetAsNativeFrame();
                    if (pNativeFrame == NULL)
                    {
                        // filter out internal frames
                        _ASSERTE(pThisFrame->GetAsInternalFrame() != NULL);
                        continue;
                    }

                    //
                    // Fill in the easy stuff.
                    //
                    CordbFunction * pFunction;

                    pFunction = (static_cast<CordbFrame *>(pNativeFrame))->GetFunction();
                    ASSERT(pFunction != NULL);

                    hr = pFunction->QueryInterface(IID_ICorDebugFunction2,
                        reinterpret_cast<void **>(&(pFunctions[index].pFunction)));
                    ASSERT(!FAILED(hr));

                    CordbModule * pModule = pFunction->GetModule();
                    pFunctions[index].pModule = pModule;
                    pModule->ExternalAddRef();

                    CordbAppDomain * pAppDomain = pNativeFrame->GetCurrentAppDomain();
                    pFunctions[index].pAppDomain = pAppDomain;
                    pAppDomain->ExternalAddRef();

                    pFunctions[index].flags = 0;

                    //
                    // Now go to the IL frame (if one exists) to the get the offset.
                    //
                    CordbJITILFrame * pJITILFrame;

                    pJITILFrame = pNativeFrame->m_JITILFrame;

                    if (pJITILFrame != NULL)
                    {
                        hr = pJITILFrame->GetIP(&(pFunctions[index].ilOffset), NULL);
                        ASSERT(!FAILED(hr));
                    }
                    else
                    {
                        pFunctions[index].ilOffset = (DWORD) NO_MAPPING;
                    }

                    // Update to the next count.
                    index++;
                }

                // @todo - The spec says that pcFunctions == # of elements in pFunctions,
                // but the behavior here is that it's always the total.
                // If we want to fix that, we should uncomment the assignment here:
                //*pcFunctions = index;
            }
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


//---------------------------------------------------------------------------------------
//
// This is the entry point for continuable exceptions.
// It implements ICorDebugThread2::InterceptCurrentException.
//
// Arguments:
//    pFrame - the stack frame to intercept at
//
// Return Value:
//    HRESULT indicating success or failure
//
// Notes:
//    Since we cannot intercept an exception at an internal frame,
//    pFrame should not be an ICorDebugInternalFrame.
//

HRESULT CordbThread::InterceptCurrentException(ICorDebugFrame * pFrame)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        DebuggerIPCEvent event;

        if (pFrame == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        //
        // Verify we were passed a real stack frame, and not an internal
        // CLR mocked up one.
        //
        {
            RSExtSmartPtr<ICorDebugInternalFrame> pInternalFrame;
            hr = pFrame->QueryInterface(IID_ICorDebugInternalFrame, (void **)&pInternalFrame);

            if (!FAILED(hr))
            {
                ThrowHR(E_INVALIDARG);
            }
        }


        //
        // If the thread is detached, then there should be no frames on its stack.
        //
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            //
            // Refresh the stack frames for this thread and verify pFrame is on it.
            //

            RefreshStack();

            //
            // Now check if the frame actually lives on the stack of the current thread.
            //

            // "Cast" the ICDFrame pointer to a CordbFrame pointer.
            CordbFrame * pRealFrame = CordbFrame::GetCordbFrameFromInterface(pFrame);
            if (!OwnsFrame(pRealFrame))
            {
                ThrowHR(E_INVALIDARG);
            }

            //
            // pFrame is on the stack - good.  Now tell the LS to intercept at that frame.
            //

            GetProcess()->InitIPCEvent(&event, DB_IPCE_INTERCEPT_EXCEPTION, true, VMPTR_AppDomain::NullPtr());

            event.InterceptException.vmThreadToken = m_vmThreadToken;
            event.InterceptException.frameToken  = pRealFrame->GetFramePointer();

            hr = GetProcess()->m_cordb->SendIPCEvent(GetProcess(), &event, sizeof(DebuggerIPCEvent));

            //
            // Stop now if we can't even send the event.
            //
            if (!SUCCEEDED(hr))
            {
                ThrowHR(hr);
            }

            _ASSERTE(event.type == DB_IPCE_INTERCEPT_EXCEPTION_RESULT);

            hr = event.hr;
            // Since we are going to exit anyway, we don't need to throw here.
        }
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Return S_OK if there is a current exception and it is unhandled, otherwise
// return S_FALSE
//
HRESULT CordbThread::HasUnhandledException()
{
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_FALSE;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        if(pDAC->HasUnhandledException(m_vmThreadToken))
        {
            hr = S_OK;
        }
    }
    PUBLIC_REENTRANT_API_END(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Create a stackwalker on the current thread.  Initially, the stackwalker is stopped at the
// managed filter CONTEXT if there is one.  Otherwise it is stopped at the leaf CONTEXT.
//
// Arguments:
//    ppStackWalk - out parameter; return the new stackwalker
//
// Return Value:
//    Return S_OK on success.
//    Return E_FAIL on error.
//
// Notes:
//    The filter CONTEXT will be removed in V3.0.
//

HRESULT CordbThread::CreateStackWalk(ICorDebugStackWalk ** ppStackWalk)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppStackWalk, ICorDebugStackWalk **);

    HRESULT hr = S_OK;

    EX_TRY
    {
        RSLockHolder lockHolder(GetProcess()->GetProcessLock());
        hr = EnsureThreadIsAlive();

        if (SUCCEEDED(hr))
        {
            RSInitHolder<CordbStackWalk> pSW(new CordbStackWalk(this));
            pSW->Init();
            pSW.TransferOwnershipExternal(ppStackWalk);
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


//---------------------------------------------------------------------------------------
//
// This is a callback function used to enumerate the internal frames on a thread.
// Each time this callback is invoked, we'll create a new CordbInternalFrame and store it
// in an array.  See code:DacDbiInterfaceImpl::EnumerateInternalFrames for more information.
//
// Arguments:
//    pFrameData - contains information about the current internal frame in the enumeration
//    pUserData  - This is a GetActiveInternalFramesData.
//                 It contains an array of internl frames to be filled.
//

// static
void CordbThread::GetActiveInternalFramesCallback(const DebuggerIPCE_STRData * pFrameData,
                                                  void *                 pUserData)
{
    // Retrieve the CordbThread.
    GetActiveInternalFramesData * pCallbackData = reinterpret_cast<GetActiveInternalFramesData *>(pUserData);
    CordbThread * pThis = pCallbackData->pThis;
    INTERNAL_DAC_CALLBACK(pThis->GetProcess());

    // Make sure we are getting invoked for internal frames.
    _ASSERTE(pFrameData->eType == DebuggerIPCE_STRData::cStubFrame);

    // Look up the CordbAppDomain.
    CordbAppDomain * pAppDomain = NULL;
    VMPTR_AppDomain vmCurrentAppDomain = pFrameData->vmCurrentAppDomainToken;
    if (!vmCurrentAppDomain.IsNull())
    {
        pAppDomain = pThis->GetProcess()->LookupOrCreateAppDomain(vmCurrentAppDomain);
    }

    // Create a CordbInternalFrame.
    CordbInternalFrame * pInternalFrame = new CordbInternalFrame(pThis,
                                                                 pFrameData->fp,
                                                                 pAppDomain,
                                                                 pFrameData);

    // Store the internal frame in the array and update the index to prepare for the next one.
    pCallbackData->pInternalFrames.Assign(pCallbackData->uIndex, pInternalFrame);
    pCallbackData->uIndex++;
}

//---------------------------------------------------------------------------------------
//
// This function returns an array of ICDInternalFrame2.  Each element represents an internal frame
// on the thread.  If ppInternalFrames is NULL or cInternalFrames is 0, then we just return
// the number of internal frames on the thread.
//
// Arguments:
//    cInternalFrames  - the number of elements in ppInternalFrames
//    pcInternalFrames - out parameter; return the number of internal frames on the thread
//    ppInternalFrames - a buffer to store the array of internal frames
//
// Return Value:
//    S_OK on success.
//    E_INVALIDARG if
//      - ppInternalFrames is NULL but cInternalFrames is not 0
//      - pcInternalFrames is NULL
//      - cInternalFrames is smaller than the number of internal frames actually on the thread
//

HRESULT CordbThread::GetActiveInternalFrames(ULONG32 cInternalFrames,
                                             ULONG32 * pcInternalFrames,
                                             ICorDebugInternalFrame2 * ppInternalFrames[])
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this);
    {
        if ( ((cInternalFrames != 0) && (ppInternalFrames == NULL)) ||
             (pcInternalFrames == NULL) )
        {
            ThrowHR(E_INVALIDARG);
        }

        *pcInternalFrames = 0;

        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        ULONG32 cActiveInternalFrames = pDAC->GetCountOfInternalFrames(m_vmThreadToken);

        // Set the count.
        *pcInternalFrames = cActiveInternalFrames;

        // Don't need to do anything else if the user is only asking for the count.
        if ((cInternalFrames != 0) && (ppInternalFrames != NULL))
        {
            if (cInternalFrames < cActiveInternalFrames)
            {
                ThrowWin32(ERROR_INSUFFICIENT_BUFFER);
            }
            else
            {
                // initialize the callback data
                GetActiveInternalFramesData data;
                data.pThis = this;
                data.uIndex = 0;
                data.pInternalFrames.AllocOrThrow(cActiveInternalFrames);
                // We want to ensure it's automatically cleaned up in all cases
                // e.g. if we're debugging a MiniDumpNormal and we fail to
                // retrieve memory from the target.  The exception will be
                // caught above this frame.
                data.pInternalFrames.EnableAutoClear();

                pDAC->EnumerateInternalFrames(m_vmThreadToken,
                                              &CordbThread::GetActiveInternalFramesCallback,
                                              &data);
                _ASSERTE(cActiveInternalFrames == data.pInternalFrames.Length());

                // Copy the internal frames we have accumulated in GetActiveInternalFramesData to the out
                // argument.
                for (unsigned int i = 0; i < data.pInternalFrames.Length(); i++)
                {
                    RSInitHolder<CordbInternalFrame> pInternalFrame(data.pInternalFrames[i]);
                    pInternalFrame.TransferOwnershipExternal(&(ppInternalFrames[i]));
                }
            }
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}


// ICorDebugThread4

// -------------------------------------------------------------------------------
// Gets the current custom notification on this thread or NULL if no such object exists
// Arguments:
//    output: ppNotificationObject - current CustomNotification object.
//            if we aren't currently inside a CustomNotification callback, this will
//            always return NULL.
// return value:
// S_OK on success
// S_FALSE if no object exists
// CORDBG_E_BAD_REFERENCE_VALUE if the reference is bad
HRESULT CordbThread::GetCurrentCustomDebuggerNotification(ICorDebugValue ** ppNotificationObject)
{
    HRESULT hr = S_OK;
    PUBLIC_API_NO_LOCK_BEGIN(this);
    {
        ATT_REQUIRE_STOPPED_MAY_FAIL_OR_THROW(GetProcess(), ThrowHR);

        if (ppNotificationObject == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        *ppNotificationObject = NULL;

        //
        // Go to the LS and retrieve any notification object.
        //
        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        VMPTR_OBJECTHANDLE vmObjHandle = pDAC->GetCurrentCustomDebuggerNotification(m_vmThreadToken);

#if defined(_DEBUG)
        // Since we know a notification has occurred on this thread, our assumption about the
        // thread's current AppDomain should be correct
        VMPTR_AppDomain vmAppDomain = pDAC->GetCurrentAppDomain(m_vmThreadToken);

        _ASSERTE(GetAppDomain()->GetADToken() == vmAppDomain);
#endif // _DEBUG

        if (!vmObjHandle.IsNull())
        {
            ICorDebugReferenceValue * pRefValue = NULL;
            IfFailThrow(CordbReferenceValue::BuildFromGCHandle(GetAppDomain(), vmObjHandle, &pRefValue));
            *ppNotificationObject = pRefValue;
        }
   }
    PUBLIC_API_END(hr);
    return hr;
}

// ICorDebugThread5

/*
 * GetBytesAllocated
 *
 * Returns S_OK if it was possible to obtain the allocation information for the thread
 * and sets the corresponding SOH and UOH allocations.
 */
HRESULT CordbThread::GetBytesAllocated(ULONG64 *pSohAllocatedBytes,
                                       ULONG64 *pUohAllocatedBytes)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        DacThreadAllocInfo threadAllocInfo = { 0 };

        if (pSohAllocatedBytes == NULL || pUohAllocatedBytes == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        pDAC->GetThreadAllocInfo(m_vmThreadToken, &threadAllocInfo);

        *pSohAllocatedBytes = threadAllocInfo.m_allocBytesSOH;
        *pUohAllocatedBytes = threadAllocInfo.m_allocBytesUOH;
    }
    EX_CATCH_HRESULT(hr);

    return hr;
} // CordbThread::GetBytesAllocated

/*
 *
 * SetRemapIP
 *
 *  This routine communicate the EnC remap IP to the LS by writing it to process memory using
 *  the pointer that was set in the thread. If the address is null, then we haven't seen
 *  a RemapOpportunity call for this frame/function combo yet, so invalid to Remap the function.
 *
 * Parameters:
 *  offset - the IL offset to set the IP to
 *
 * Return Value:
 *  S_OK or CORDBG_E_NO_REMAP_BREAKPIONT.
 *
 */
HRESULT CordbThread::SetRemapIP(SIZE_T offset)
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    // This is only set when we're prepared to do a remap
    if (! m_EnCRemapFunctionIP)
    {
        return CORDBG_E_NO_REMAP_BREAKPIONT;
    }

    // Write the value of the remap offset into the left side
    HRESULT hr = GetProcess()->SafeWriteStruct(PTR_TO_CORDB_ADDRESS(m_EnCRemapFunctionIP), &offset);

    // Prevent SetRemapIP from being called twice for the same RemapOpportunity
    // If we don't get any calls to RemapFunction, this member will be cleared in
    // code:CordbThread::MarkStackFramesDirty when Continue is called
    m_EnCRemapFunctionIP = NULL;

    return hr;
}


//---------------------------------------------------------------------------------------
//
// This routine is the interface function for ICorDebugThread2::GetConnectionID.
//
// Arguments:
//    pdwConnectionId - return connection id set on the thread. Can return INVALID_CONNECTION_ID
//
// Return Value:
//    HRESULT indicating success or failure
//
HRESULT CordbThread::GetConnectionID(CONNID * pConnectionID)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // now retrieve the connection id
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (pConnectionID == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        *pConnectionID = pDAC->GetConnectionID(m_vmThreadToken);

        if (*pConnectionID == INVALID_CONNECTION_ID)
        {
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}   // CordbThread::GetConnectionID

//---------------------------------------------------------------------------------------
//
//  This routine is the interface function for ICorDebugThread2::GetTaskID.
//
// Arguments:
//    pTaskId - return task id set on the thread. Can return INVALID_TASK_ID
//
// Return Value:
//    HRESULT indicating success or failure
//
HRESULT CordbThread::GetTaskID(TASKID * pTaskID)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // now retrieve the task id
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (pTaskID == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        *pTaskID = this->GetTaskID();

        if (*pTaskID == INVALID_TASK_ID)
        {
            hr = S_FALSE;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}   // CordbThread::GetTaskID

//---------------------------------------------------------------------------------------
// Get the task ID for this thread
//
// return:
//     task id set on the thread. Can return INVALID_TASK_ID
//
TASKID CordbThread::GetTaskID()
{
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    return pDAC->GetTaskID(m_vmThreadToken);
}



//---------------------------------------------------------------------------------------
//
//  This routine is the interface function for ICorDebugThread2::GetVolatileOSThreadID.
//
// Arguments:
//    pdwTid - return os thread id
//
// Return Value:
//    HRESULT indicating success or failure
//
// Notes:
//    Compare with code:CordbThread::GetID
HRESULT CordbThread::GetVolatileOSThreadID(DWORD * pdwTID)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // now retrieve the OS thread ID
    HRESULT hr = S_OK;
    EX_TRY
    {
        if (pdwTID == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        *pdwTID = pDAC->TryGetVolatileOSThreadID(m_vmThreadToken);

        if (*pdwTID == 0)
        {
            hr = S_FALSE; // Switched out
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}   //  CordbThread::GetOSThreadID

//---------------------------------------------------------------------------------------
// Get the thread's volatile OS ID. (this is fiber aware)
//
// Returns:
//      Thread's current OS id. For fibers / "logical threads", This may change as a thread executes.
//      Throws if the managed thread currently is not mapped to an OS thread (ie, not scheduled)
//
DWORD CordbThread::GetVolatileOSThreadID()
{
    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    DWORD dwThreadID = pDAC->TryGetVolatileOSThreadID(m_vmThreadToken);

    if (dwThreadID == 0)
    {
        ThrowHR(CORDBG_E_THREAD_NOT_SCHEDULED);
    }
    return dwThreadID;
}

// ----------------------------------------------------------------------------
// CordbThread::ClearStackFrameCache
//
// Description:
//    Clear the cache of stack frames maintained by the CordbThread.
//
// Notes:
//    We are doing an InternalRelease() here to match the InternalAddRef() in code:CordbThread::RefreshStack.
//

void CordbThread::ClearStackFrameCache()
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    for (int i = 0; i < m_stackFrames.Count(); i++)
    {
        (*m_stackFrames.Get(i))->Neuter();
        (*m_stackFrames.Get(i))->InternalRelease();
    }
    m_stackFrames.Clear();
}

// ----------------------------------------------------------------------------
// EnumerateBlockingObjectsCallback
//
// Description:
//    A small helper used by CordbThread::GetBlockingObjects. This callback adds the enumerated items
//    to a list
//
// Arguments:
//    blockingObject - the object to add to the list
//    pUserData - the list to add it to

VOID EnumerateBlockingObjectsCallback(DacBlockingObject blockingObject, CALLBACK_DATA pUserData)
{
    CQuickArrayList<DacBlockingObject>* pDacBlockingObjs = (CQuickArrayList<DacBlockingObject>*)pUserData;
    pDacBlockingObjs->Push(blockingObject);
}

// ----------------------------------------------------------------------------
// CordbThread::GetBlockingObjects
//
// Description:
//    Returns a list of objects that a thread is blocking on by using Monitor.Enter and
//    Monitor.Wait
//
// Arguments:
//    ppBlockingObjectEnum - on return this is an enumerator for the list of blocking objects
//
// Return:
//    S_OK on success or an appropriate failing HRESULT

HRESULT CordbThread::GetBlockingObjects(ICorDebugBlockingObjectEnum **ppBlockingObjectEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(ppBlockingObjectEnum, ICorDebugBlockingObjectEnum **);

    HRESULT hr = S_OK;
    CorDebugBlockingObject* blockingObjs = NULL;
    EX_TRY
    {
        CQuickArrayList<DacBlockingObject> dacBlockingObjects;
        IDacDbiInterface* pDac = GetProcess()->GetDAC();
        pDac->EnumerateBlockingObjects(m_vmThreadToken,
            (IDacDbiInterface::FP_BLOCKINGOBJECT_ENUMERATION_CALLBACK) EnumerateBlockingObjectsCallback,
            (CALLBACK_DATA) &dacBlockingObjects);
        blockingObjs = new CorDebugBlockingObject[dacBlockingObjects.Size()];
        for(SIZE_T i = 0 ; i < dacBlockingObjects.Size(); i++)
        {
            // ICorDebug API needs to flip the direction of the list from the way DAC stores it
            SIZE_T dacObjIndex = dacBlockingObjects.Size()-i-1;
            switch(dacBlockingObjects[dacObjIndex].blockingReason)
            {
                case DacBlockReason_MonitorCriticalSection:
                    blockingObjs[i].blockingReason = BLOCKING_MONITOR_CRITICAL_SECTION;
                    break;
                case DacBlockReason_MonitorEvent:
                    blockingObjs[i].blockingReason = BLOCKING_MONITOR_EVENT;
                    break;
                default:
                    _ASSERTE(!"Should not get here");
                    ThrowHR(E_FAIL);
                    break;
            }
            blockingObjs[i].dwTimeout = dacBlockingObjects[dacObjIndex].dwTimeout;
            CordbAppDomain* pAppDomain;
            {
                RSLockHolder holder(GetProcess()->GetProcessLock());
                pAppDomain = GetProcess()->LookupOrCreateAppDomain(dacBlockingObjects[dacObjIndex].vmAppDomain);
            }
            blockingObjs[i].pBlockingObject = CordbValue::CreateHeapValue(pAppDomain,
                dacBlockingObjects[dacObjIndex].vmBlockingObject);
        }

        CordbBlockingObjectEnumerator* objEnum = new CordbBlockingObjectEnumerator(GetProcess(),
                                                                                  blockingObjs,
                                                                                  (DWORD)dacBlockingObjects.Size());
        GetProcess()->GetContinueNeuterList()->Add(GetProcess(), objEnum);
        hr = objEnum->QueryInterface(__uuidof(ICorDebugBlockingObjectEnum), (void**)ppBlockingObjectEnum);
        _ASSERTE(SUCCEEDED(hr));
    }
    EX_CATCH_HRESULT(hr);
    delete [] blockingObjs;
    return hr;
}

#ifdef FEATURE_INTEROP_DEBUGGING
/* ------------------------------------------------------------------------- *
 * Unmanaged Thread classes
 * ------------------------------------------------------------------------- */

CordbUnmanagedThread::CordbUnmanagedThread(CordbProcess *pProcess, DWORD dwThreadId, HANDLE hThread, void *lpThreadLocalBase)
  : CordbBase(pProcess, dwThreadId, enumCordbUnmanagedThread),
    m_stackBase(0),
    m_stackLimit(0),
    m_handle(hThread),
    m_threadLocalBase(lpThreadLocalBase),
    m_pTLSArray(NULL),
    m_pTLSExtendedArray(NULL),
    m_state(CUTS_None),
    m_originalHandler(NULL),
#ifdef TARGET_X86
    m_pSavedLeafSeh(NULL),
#endif
    m_continueCountCached(0)
{
    m_pLeftSideContext.Set(NULL);

    IBEvent()->m_state = CUES_None;
    IBEvent()->m_next = NULL;
    IBEvent()->m_owner = this;

    IBEvent2()->m_state = CUES_None;
    IBEvent2()->m_next = NULL;
    IBEvent2()->m_owner = this;

    OOBEvent()->m_state = CUES_None;
    OOBEvent()->m_next = NULL;
    OOBEvent()->m_owner = this;

    m_pPatchSkipAddress = NULL;

    this->GetStackRange(NULL, NULL);
}

CordbUnmanagedThread::~CordbUnmanagedThread()
{
    // CordbUnmanagedThread objects will:
    // - never send IPC events.
    // - never be exposed to the public. (we assert external-ref is always == 0)
    // - always manipulated on W32ET (where we can't do IPC stuff)

    UnsafeNeuterDeadObject();

    _ASSERTE(this->IsNeutered());

    // by the time the thread is deleted, it shouldn't have any outstanding debug events.

    // Actually, the thread could get deleted while we have an outstanding IB debug event. We could get the IB event, hijack that thread,
    // and then since the process is continued, something could go off and kill the hijacked thread.
    // If the event is still in the process's queued list, and it still refers back to a thread, then we'll AV when we try to access the event
    // (or continue it).
    CONSISTENCY_CHECK_MSGF(!HasIBEvent(), ("Deleting thread w/ outstanding IB event:this=%p,event-code=%d\n", this, IBEvent()->m_currentDebugEvent.dwDebugEventCode));

    CONSISTENCY_CHECK_MSGF(!HasOOBEvent(), ("Deleting thread w/ outstanding OOB event:this=%p,event-code=%d\n", this, OOBEvent()->m_currentDebugEvent.dwDebugEventCode));
}

HRESULT CordbUnmanagedThread::LoadTLSArrayPtr(void)
{
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    // Just simple math on NT with a small tls index.
    // The TLS slots for 0-63 are embedded in the TIB.
    m_pTLSArray = (BYTE*) m_threadLocalBase + offsetof(TEB, TlsSlots);

    // Extended slot is lazily initialized, so check every time.
    if (m_pTLSExtendedArray == NULL)
    {
        // On NT 5 you can have TLS index's greater than 63, so we
        // have to grab the ptr to the TLS expansion array first,
        // then use that as the base to index off of.  This will
        // never move once we find it for a given thread, so we
        // cache it here so we don't always have to perform two
        // ReadProcessMemory's.
        void *ppTLSArray = (BYTE*) m_threadLocalBase + offsetof(TEB, TlsExpansionSlots);

        hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(ppTLSArray), &m_pTLSExtendedArray);
    }


    return hr;
}

/*
VOID CordbUnmanagedThread::VerifyFSChain()
{
#if defined(TARGET_X86)
    DT_CONTEXT temp;
    temp.ContextFlags = DT_CONTEXT_FULL;
    DbiGetThreadContext(m_handle, &temp);
    LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: 0x%x fs=0x%x TIB=0x%x\n",
             m_id, temp.SegFs, m_threadLocalBase));
    REMOTE_PTR pExceptionRegRecordPtr;
    HRESULT hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(m_threadLocalBase), &pExceptionRegRecordPtr);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x failed to read fs:0 value: computed addr=0x%p err=%x\n",
             m_id, m_threadLocalBase, hr));
        _ASSERTE(FALSE);
        return;
    }
    while(pExceptionRegRecordPtr != EXCEPTION_CHAIN_END && pExceptionRegRecordPtr != NULL)
    {
        REMOTE_PTR prev;
        REMOTE_PTR handler;
        hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pExceptionRegRecordPtr), &prev);
        if(FAILED(hr))
        {
            LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x failed to read prev value: computed addr=0x%p err=%x\n",
                m_id, pExceptionRegRecordPtr, hr));
            return;
        }
        hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS( (VOID*)((DWORD)pExceptionRegRecordPtr+4) ), &handler);
        if(FAILED(hr))
        {
            LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x failed to read handler value: computed addr=0x%p err=%x\n",
                m_id, (DWORD)pExceptionRegRecordPtr+4, hr));
            return;
        }
        LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: OK 0x%x record=0x%x prev=0x%x handler=0x%x\n",
            m_id, pExceptionRegRecordPtr, prev, handler));
        if(handler == NULL)
        {
            LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x NULL handler found\n", m_id));
            _ASSERTE(FALSE);
            return;
        }
        if(prev == NULL)
        {
            LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x NULL prev found\n", m_id));
            _ASSERTE(FALSE);
            return;
        }
        if(prev == pExceptionRegRecordPtr)
        {
            LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: ERROR 0x%x cyclic prev found\n", m_id));
            _ASSERTE(FALSE);
            return;
        }
        pExceptionRegRecordPtr = prev;
    }

    LOG((LF_CORDB, LL_INFO1000, "CUT::VFSC: OK 0x%x\n", m_id));
#endif
    return;
}*/

#ifdef TARGET_X86
HRESULT CordbUnmanagedThread::SaveCurrentLeafSeh()
{
    _ASSERTE(m_pSavedLeafSeh == NULL);
    REMOTE_PTR pExceptionRegRecordPtr;
    HRESULT hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(m_threadLocalBase), &pExceptionRegRecordPtr);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::SCLS: failed to read fs:0 value: computed addr=0x%p err=%x\n", m_threadLocalBase, hr));
        return hr;
    }
    m_pSavedLeafSeh = pExceptionRegRecordPtr;
    return S_OK;
}

HRESULT CordbUnmanagedThread::RestoreLeafSeh()
{
    _ASSERTE(m_pSavedLeafSeh != NULL);
    HRESULT hr = GetProcess()->SafeWriteStruct(PTR_TO_CORDB_ADDRESS(m_threadLocalBase), &m_pSavedLeafSeh);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::RLS: failed to write fs:0 value: computed addr=0x%p err=%x\n", m_threadLocalBase, hr));
        return hr;
    }
    m_pSavedLeafSeh = NULL;
    return S_OK;
}
#endif

// Read the contents from the LS's Predefined TLS block.
// This is an auxiliary TLS storage array-of-void*, indexed off the TLS.
// pRead is optional. This makes sense when '0' is a valid default value.
// 1) On success (block exists in LS, we can read it),
//    return value of data in the slot, *pRead = true
// 2) On failure to read block (block doesn't exist yet, any other failure)
//    return value == 0 (assumed default, *pRead = false
REMOTE_PTR CordbUnmanagedThread::GetPreDefTlsSlot(SIZE_T offset)
{
    REMOTE_PTR tlsDataAddress;
    HRESULT hr = GetClrModuleTlsDataAddress(&tlsDataAddress);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETV: GetClrModuleTlsDataAddress FAILED %x for 0x%x\n", hr, m_id));
        return NULL;
    }

    REMOTE_PTR data = 0;

    // Read the thread's TLS value.
    REMOTE_PTR slotAddr = (BYTE*)tlsDataAddress + offset;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(slotAddr), &data);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETV: failed to get TLS value: tlsData=0x%p offset=%d, err=%x\n",
            tlsDataAddress, offset, hr));
        return NULL;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GEETV: EE Thread TLS value is 0x%x for 0x%x\n", data, m_id));
    return data;
}

// Read the contents from a LS threads's TLS slot.
HRESULT CordbUnmanagedThread::GetTlsSlot(DWORD slot, REMOTE_PTR * pValue)
{
    // Compute the address of the necessary TLS value.
    HRESULT hr = LoadTLSArrayPtr();
    if (FAILED(hr))
    {
        return hr;
    }

    void * pBase = NULL;
    SIZE_T slotAdjusted = slot;

    if (slot < TLS_MINIMUM_AVAILABLE)
    {
        pBase = m_pTLSArray;
    }
    else if (slot < TLS_MINIMUM_AVAILABLE + TLS_EXPANSION_SLOTS)
    {
        pBase = m_pTLSExtendedArray;
        slotAdjusted -= TLS_MINIMUM_AVAILABLE;

        // Expansion slot is lazily allocated. If we're trying to read from it, but hasn't been allocated,
        // then the TLS slot is still the default value, which is 0 (NULL).
        if (pBase == NULL)
        {
            *pValue = NULL;
            return S_OK;
        }
    }
    else
    {
        // Slot is out of range. Shouldn't happen unless debuggee is corrupted.
        _ASSERTE(!"Invalid TLS slot");
       return E_UNEXPECTED;
    }

    void *pEEThreadTLS = (BYTE*)pBase + (slotAdjusted * sizeof(void*));

    // Read the thread's TLS value.
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pEEThreadTLS), pValue);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GTS: failed to read TLS value: computed addr=0x%p slot=%d, err=%x\n",
           pEEThreadTLS, slot, hr));
        return hr;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GTS: EE Thread TLS value is 0x%p for thread 0x%x, slot 0x%x\n", *pValue, m_id, slot));
    return S_OK;
}

// This does a WriteProcessMemory to write to the debuggee's TLS slot
//
// Notes:
//   This is very brittle because the OS can lazily allocates storage for TLS slots.
//   In order to guarantee the storage is available, it must have been written to by the debuggee.
//   For managed threads, that's easy because the Thread* is already written to the slot.
//   But for pure native threads where GetThread() == NULL, the storage may not yet be allocated.
//
//   The saving grace is that the debuggee's hijack filters will force the TLS to be allocated before it
//   sends a flare.
//
//   Therefore, this function can only be called:
//   1) on a managed thread
//   2) on a native thread after that thread has been hijacked and sent a flare.
//
//   This is brittle reasoning, but so is the rest of interop-debugging.
//
HRESULT CordbUnmanagedThread::SetTlsSlot(DWORD slot, REMOTE_PTR value)
{
    FAIL_IF_NEUTERED(this);

    // Compute the address of the necessary TLS value.
    HRESULT hr = LoadTLSArrayPtr();
    if (FAILED(hr))
    {
        return hr;
    }

    void * pBase = NULL;
    SIZE_T slotAdjusted = slot;
    if (slot < TLS_MINIMUM_AVAILABLE)
    {
        pBase = m_pTLSArray;
    }
    else if (slot < TLS_MINIMUM_AVAILABLE + TLS_EXPANSION_SLOTS)
    {
        pBase = m_pTLSExtendedArray;
        slotAdjusted -= TLS_MINIMUM_AVAILABLE;

        // Expansion slot is lazily allocated. If we're trying to read from it, but hasn't been allocated,
        // then the TLS slot is still the default value, which is 0.
        if (pBase == NULL)
        {
            // See reasoning in header for why this should succeed.
            _ASSERTE(!"Can't set to expansion slots because they haven't been allocated");
            return E_FAIL;
        }
    }
    else
    {
        // Slot is out of range. Shouldn't happen unless debuggee is corrupted.
        _ASSERTE(!"Invalid TLS slot");
        return E_INVALIDARG;
    }

    void *pEEThreadTLS = (BYTE*)pBase + (slotAdjusted * sizeof(void*));

    // Write the thread's TLS value.
    hr = GetProcess()->SafeWriteStruct(PTR_TO_CORDB_ADDRESS(pEEThreadTLS), &value);

    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::SEETV: failed to set TLS value: computed addr=0x%p slot=%d, err=%x\n", pEEThreadTLS, slot, hr));
        return hr;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::SEETV: EE Thread TLS value is now 0x%p for 0x%x\n", value, m_id));
    return S_OK;
}

// gets the value of gCurrentThreadInfo.m_pThread
DWORD_PTR CordbUnmanagedThread::GetEEThreadValue()
{
    DWORD_PTR ret = NULL;

    REMOTE_PTR tlsDataAddress;
    HRESULT hr = GetClrModuleTlsDataAddress(&tlsDataAddress);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETV: GetClrModuleTlsDataAddress FAILED %x for 0x%x\n", hr, m_id));
        return NULL;
    }

    // Read the thread's TLS value.
    REMOTE_PTR EEThreadAddr = (BYTE*)tlsDataAddress + GetProcess()->m_runtimeOffsets.m_TLSEEThreadOffset + OFFSETOF__TLS__tls_CurrentThread;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(EEThreadAddr), &ret);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETV: failed to get TLS value: computed addr=0x%p index=%d, err=%x\n",
             EEThreadAddr, GetProcess()->m_runtimeOffsets.m_TLSIndex, hr));
        return NULL;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GEETV: EE Thread TLS value is 0x%p for 0x%x\n", ret, m_id));
    return ret;
}

// returns the remote address of gCurrentThreadInfo
HRESULT CordbUnmanagedThread::GetClrModuleTlsDataAddress(REMOTE_PTR* pAddress)
{
    *pAddress = NULL;

    REMOTE_PTR tlsArrayAddr;
    HRESULT hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)m_threadLocalBase + WINNT_OFFSETOF__TEB__ThreadLocalStoragePointer), &tlsArrayAddr);
    if (FAILED(hr))
    {
        return hr;
    }

    // This is the special break-in thread case: TEB.ThreadLocalStoragePointer == NULL
    if (tlsArrayAddr == NULL)
    {
        return E_FAIL;
    }

    REMOTE_PTR clrModuleTlsDataAddr;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)tlsArrayAddr + GetProcess()->m_runtimeOffsets.m_TLSIndex * sizeof(void*)), &clrModuleTlsDataAddr);
    if (FAILED(hr))
    {
        return hr;
    }

    if (clrModuleTlsDataAddr == NULL)
    {
        _ASSERTE(!"No clr module data present at _tls_index for this thread");
        return E_FAIL;
    }

    *pAddress = (BYTE*) clrModuleTlsDataAddr;
    return S_OK;
}

/*
 * GetEEDebuggerWord
 *
 * This routine returns the value read from the thread
 *
 * Parameters:
 *   pValue - Location to store value.
 *
 * Returns:
 *   E_INVALIDARG, E_FAIL, S_OK
 */
HRESULT CordbUnmanagedThread::GetEEDebuggerWord(REMOTE_PTR *pValue)
{
    LOG((LF_CORDB, LL_INFO1000, "CUT::GEEDW: Entered\n"));
    if (pValue == NULL)
    {
        return E_INVALIDARG;
    }
    return GetTlsSlot(GetProcess()->m_runtimeOffsets.m_debuggerWordTLSIndex, pValue);
}

// SetEEDebuggerWord
//
// This routine writes the value to the thread
//
// Parameters:
//   pValue - Value to write.
//
// Returns:
//   HRESULT failure code or S_OK
//
// Notes:
//    This function is very dangerous. See code:CordbUnmanagedThread::SetEETlsValue for why.
HRESULT CordbUnmanagedThread::SetEEDebuggerWord(REMOTE_PTR value)
{
    LOG((LF_CORDB, LL_INFO1000, "CUT::SEEDW: Entered - value is 0x%p\n", value));
    return SetTlsSlot(GetProcess()->m_runtimeOffsets.m_debuggerWordTLSIndex, value);
}

/*
 * GetEEThreadPtr
 *
 * This routine returns the value read from the thread
 *
 * Parameters:
 *   ppEEThread - Location to store value.
 *
 * Returns:
 *   E_INVALIDARG, E_FAIL, S_OK
 */
HRESULT CordbUnmanagedThread::GetEEThreadPtr(REMOTE_PTR *ppEEThread)
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    if (ppEEThread == NULL)
    {
        return E_INVALIDARG;
    }

    *ppEEThread = (REMOTE_PTR)GetEEThreadValue();

    return S_OK;
}


void CordbUnmanagedThread::GetEEState(bool *threadStepping, bool *specialManagedException)
{
    REMOTE_PTR pEEThread;

    HRESULT hr = GetEEThreadPtr(&pEEThread);

    _ASSERTE(SUCCEEDED(hr));
    _ASSERTE(pEEThread != NULL);

    *threadStepping = false;
    *specialManagedException = false;

    // Compute the address of the thread's state
    DebuggerIPCRuntimeOffsets *pRO = &(GetProcess()->m_runtimeOffsets);
    void *pEEThreadStateNC = (BYTE*) pEEThread + pRO->m_EEThreadStateNCOffset;

    // Grab the thread state out of the EE Thread.
    DWORD EEThreadStateNC;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pEEThreadStateNC), &EEThreadStateNC);
    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETS: failed to read thread state NC: 0x%p + 0x%x = 0x%p, err=%d\n",
             pEEThread, pRO->m_EEThreadStateNCOffset, pEEThreadStateNC, GetLastError()));
        return;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GEETS: EE Thread state NC is 0x%08x\n", EEThreadStateNC));

    // Looks like we've got the state of the thread.
    *threadStepping = ((EEThreadStateNC & pRO->m_EEThreadSteppingStateMask) != 0);
    *specialManagedException = ((EEThreadStateNC & pRO->m_EEIsManagedExceptionStateMask) != 0);

    return;
}

// Is the thread in a "can't stop" region?
// "Can't-Stop" regions include anything that's "inside" the runtime; ie, the runtime has some
// synchronization mechanism that will halt this thread, and so we don't need to suspend it.
// The interop debugger should leave anything in a can't-stop region alone and just let the runtime
// handle it.
bool CordbUnmanagedThread::IsCantStop()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;

    // Definition of a can't stop region:
    // - Any "Special" thread that doesn't have an EE Thread (includes the real Helper Thread,
    //   Concurrent GC thread, ThreadPool thread, etc).
    // - Any thread in Cooperative code.
    // - Any thread w/ a can't-stop count > 0.
    // - Any thread holding a "Debugger" Crst. (This is actually a subset of the
    //   can't-stop count b/c Enter/Leave adjust that count).
    // - Any generic, first chance or RaiseException hijacked thread

    // If the runtime isn't init yet, not a can't-stop.
    // We don't even have the DCB yet.
    if (!GetProcess()->m_initialized)
    {
        return false;
    }
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    if (IsRaiseExceptionHijacked())
    {
        return true;
    }

    REMOTE_PTR pEEThread;
    HRESULT hr = this->GetEEThreadPtr(&pEEThread);
    if (FAILED(hr))
    {
        _ASSERTE(!"Failed to EEThreadPtr in IsCantStop");
        return true;
    }

    DebuggerIPCRuntimeOffsets *pRO = &(GetProcess()->m_runtimeOffsets);

    // @todo-  remove this and use the CantStop index below.
    // Is this a "special" thread?
    // Any thread that can take CLR locks w/o having an EE Thread object should
    // be marked as special. These threads are in "can't-stop" regions b/c if we suspend
    // them, they may be holding a lock that blocks the helper thread.
    // The helper thread is marked as "special".
    {
        REMOTE_PTR special = GetPreDefTlsSlot(pRO->m_TLSIsSpecialOffset);

        // If it's a special thread
        if ((special != 0) && (pEEThread == NULL))
        {
            return true;
        }
    }

    // Check for CantStop regions off the FLS.
    // This is the biggest way to describe can't-stop regions when we're in preemptive mode
    // (or when we don't have a thread object).
    // If a LS thread takes a debugger lock, it will increment the Can't-Stop count.
    {
        REMOTE_PTR count = GetPreDefTlsSlot(pRO->m_TLSCantStopOffset);

        // Just a sanity check here. There's nothing special about 1000, but if the
        // stop-count gets this big, 99% chance it's:
        // - we're accessing the wrong memory (an issue)
        // - someone on the LS is leaking stop-counts. (an issue).
        _ASSERTE(count < (REMOTE_PTR)1000);

        if (count > 0)
        {
            LOG((LF_CORDB, LL_INFO1000000, "Thread 0x%x is can't-stop b/c count=%d\n", m_id, count));
            return true;
        }
    }

    EX_TRY
    {
        GetProcess()->UpdateRightSideDCB();
    }
    EX_CATCH
    {
        _ASSERTE(!"IsCantStop: Failed updating debugger control block");
    }
    EX_END_CATCH(SwallowAllExceptions);

    // Helper's canary thread is always can't-stop.
    if (this->m_id == GetProcess()->GetDCB()->m_CanaryThreadId)
    {
        return true;
    }

    // Check helper thread / or anyone pretending to be the helper thread.
    if ((this->m_id == GetProcess()->GetDCB()->m_helperThreadId) ||
        (this->m_id == GetProcess()->GetDCB()->m_temporaryHelperThreadId) ||
        (this->m_id == GetProcess()->m_helperThreadId))
    {
       return true;
    }

    if (IsGenericHijacked() || IsFirstChanceHijacked())
        return true;

    // If this isn't a EE thread (and not the helper thread, and not hijacked), then it's ok to stop.
    if (pEEThread == NULL)
        return false;

    // This checks for an explicit "can't" stop region.
    // Eventually, these explicit regions should become a complete subset of the other checks.
    REMOTE_PTR count = GetPreDefTlsSlot(GetProcess()->m_runtimeOffsets.m_TLSCantStopOffset);
    if (count > 0)
        return true;

    // If we're in cooperative mode (either managed code or parts inside the runtime), then don't stop.
    // Note we could remove this since the check is made in side of the DAC request below,
    // but it's faster to look here.
    if (GetEEPGCDisabled())
        return true;

    return false;
}

bool CordbUnmanagedThread::GetEEPGCDisabled()
{
    // Note: any failure to read memory is okay for this method. We simply say that the thread has PGC disabled, which
    // is always the worst case scenario.

    REMOTE_PTR pEEThread;

    HRESULT hr = GetEEThreadPtr(&pEEThread);

    _ASSERTE(SUCCEEDED(hr));

    // Compute the address of the thread's PGC disabled word
    DebuggerIPCRuntimeOffsets *pRO = &(GetProcess()->m_runtimeOffsets);
    void *pEEThreadPGCDisabled = (BYTE*) pEEThread + pRO->m_EEThreadPGCDisabledOffset;

    // Grab the PGC disabled word out of the EE Thread.
    DWORD EEThreadPGCDisabled;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pEEThreadPGCDisabled), &EEThreadPGCDisabled);

    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETS: failed to read thread PGC Disabled: 0x%p + 0x%x = 0x%p, err=%d\n",
             pEEThread, pRO->m_EEThreadPGCDisabledOffset, pEEThreadPGCDisabled, GetLastError()));

        return true;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GEETS: EE Thread PGC Disabled is 0x%08x\n", EEThreadPGCDisabled));

    // Looks like we've got it.
    if (EEThreadPGCDisabled == pRO->m_EEThreadPGCDisabledValue)
        return true;
    else
        return false;
}

bool CordbUnmanagedThread::GetEEFrame()
{
    REMOTE_PTR pEEThread;

    HRESULT hr = GetEEThreadPtr(&pEEThread);

    _ASSERTE(SUCCEEDED(hr));
    _ASSERTE(pEEThread != NULL);

    // Compute the address of the thread's frame ptr
    DebuggerIPCRuntimeOffsets *pRO = &(GetProcess()->m_runtimeOffsets);
    void *pEEThreadFrame = (BYTE*) pEEThread + pRO->m_EEThreadFrameOffset;

    // Grab the thread's frame out of the EE Thread.
    DWORD EEThreadFrame;
    hr = GetProcess()->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pEEThreadFrame), &EEThreadFrame);

    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::GEETF: failed to read thread frame: 0x%p + 0x%x = 0x%p, err=%d\n",
             pEEThread, pRO->m_EEThreadFrameOffset, pEEThreadFrame, GetLastError()));

        return false;
    }

    LOG((LF_CORDB, LL_INFO1000000, "CUT::GEETF: EE Thread's frame is 0x%08x\n", EEThreadFrame));

    // Looks like we've got the frame of the thread.
    if (EEThreadFrame != pRO->m_EEMaxFrameValue)
        return true;
    else
        return false;
}

// Gets the thread context as if the thread were unhijacked, regardless
// of whether it really is
HRESULT CordbUnmanagedThread::GetThreadContext(DT_CONTEXT* pContext)
{
    // While hijacked there are 3 potential contexts we could be resuming back to
    // 1) A context provided in SetThreadContext that we defered applying
    // 2) The LS copy of the context on the stack being modified in the handler
    // 3) The original context present when the hijack was started
    //
    // Both #1 and #3 are stored in the GetHijackCtx() space so of course you can't
    // have them both. You have #1 if IsContextSet() is true, otherwise it holds #3.
    //
    // GenericHijack, FirstChanceHijackForSync, and RaiseExceptionHijack use #1 if available
    // and fallback to #3 if not. In other words they use GetHijackCtx() regardless of which thing it holds
    // M2UHandoff uses #1 if available and then falls back to #2.
    //
    // The reasoning here is that the first three hijacks are intended to be transparent. Since
    // the debugger shouldn't know they are occurring then it shouldn't see changes potentially
    // made on the LS. The M2UHandoff is not transparent, it has to update the context in order
    // to get clear of a bp.
    //
    // If not hijacked call the normal Win32 function.

    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_INFO10000, "CUT::GTC: thread=0x%p, flags=0x%x.\n", this, pContext->ContextFlags));

    if(IsContextSet() || IsGenericHijacked() || (IsFirstChanceHijacked() && IsBlockingForSync())
        || IsRaiseExceptionHijacked())
    {
        _ASSERTE(IsFirstChanceHijacked() || IsGenericHijacked() || IsRaiseExceptionHijacked());
        LOG((LF_CORDB, LL_INFO10000, "CUT::GTC: hijackCtx case IsContextSet=%d IsGenericHijacked=%d"
            "HijackedForSync=%d RaiseExceptionHijacked=%d.\n",
            IsContextSet(), IsGenericHijacked(), IsBlockingForSync(), IsRaiseExceptionHijacked()));
        LOG((LF_CORDB, LL_INFO10000, "CUT::GTC: hijackCtx is:\n"));
        LogContext(GetHijackCtx());
        CORDbgCopyThreadContext(pContext, GetHijackCtx());
    }
    // use the LS for M2UHandoff
    else if (IsFirstChanceHijacked() && !IsBlockingForSync())
    {
        LOG((LF_CORDB, LL_INFO10000, "CUT::GTC: getting LS context for first chance hijack, addr=0x%08x.\n",
            m_pLeftSideContext.UnsafeGet()));

        // Read the context into a temp context then copy to the out param.
        DT_CONTEXT tempContext = { 0 };

        hr = GetProcess()->SafeReadThreadContext(m_pLeftSideContext, &tempContext);

        if (SUCCEEDED(hr))
            CORDbgCopyThreadContext(pContext, &tempContext);
    }
    // no hijack in place so just call straight through
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "CUT::GTC: getting context from win32.\n"));

        BOOL succ = DbiGetThreadContext(m_handle, pContext);

        if (!succ)
            hr = HRESULT_FROM_GetLastError();
    }

    if(IsSSFlagHidden())
    {
        UnsetSSFlag(pContext);
    }
    LogContext(pContext);

    return hr;
}

// Sets the thread context as if the thread were unhijacked, regardless
// of whether it really is. See GetThreadContext above for more details
// on this abstraction
HRESULT CordbUnmanagedThread::SetThreadContext(DT_CONTEXT* pContext)
{
    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_INFO10000,
        "CUT::STC: thread=0x%p, flags=0x%x.\n", this, pContext->ContextFlags));

    LogContext(pContext);

    // If the thread is first chance hijacked, then write the context into the remote process. If the thread is generic
    // hijacked, then update the copy of the context that we already have. Otherwise call the normal Win32 function.

    if (IsGenericHijacked() || IsFirstChanceHijacked() || IsRaiseExceptionHijacked())
    {
        if(IsGenericHijacked())
        {
            LOG((LF_CORDB, LL_INFO10000, "CUT::STC: setting context from generic/2nd chance hijack.\n"));
        }
        else if(IsFirstChanceHijacked())
        {
            LOG((LF_CORDB, LL_INFO10000, "CUT::STC: setting context from 1st chance hijack.\n"));
        }
        else
        {
            LOG((LF_CORDB, LL_INFO10000, "CUT::STC: setting context from RaiseException hijack.\n"));
        }
        SetState(CUTS_HasContextSet);
        CORDbgCopyThreadContext(GetHijackCtx(), pContext);
    }
    else
    {
        LOG((LF_CORDB, LL_INFO10000, "CUT::STC: setting context from win32.\n"));

        // If the user is also setting the SS flag then we no longer have to hide it
        if(IsSSFlagEnabled(pContext))
        {
            ClearState(CUTS_IsSSFlagHidden);
        }
        // if the user is turning off the SS flag but we still want it on then leave it on
        // but hidden
        if(!IsSSFlagEnabled(pContext) && IsSSFlagNeeded())
        {
            SetState(CUTS_IsSSFlagHidden);
            SetSSFlag(pContext);
        }

        BOOL succ = DbiSetThreadContext(m_handle, pContext);

        if (!succ)
        {
            hr = HRESULT_FROM_GetLastError();
        }
    }

    return hr;
}

// Turns on the stepping flag internally and tracks whether or not the flag
// should also be seen by the user
VOID CordbUnmanagedThread::BeginStepping()
{
    _ASSERTE(!IsGenericHijacked() && !IsFirstChanceHijacked());
    _ASSERTE(!IsSSFlagNeeded());
    _ASSERTE(!IsSSFlagHidden());

    DT_CONTEXT tempContext;
    tempContext.ContextFlags = DT_CONTEXT_FULL;
    BOOL succ = DbiGetThreadContext(m_handle, &tempContext);
    _ASSERTE(succ);

    if(!IsSSFlagEnabled(&tempContext))
    {
        SetSSFlag(&tempContext);
        SetState(CUTS_IsSSFlagHidden);
    }
    SetState(CUTS_IsSSFlagNeeded);

    succ = DbiSetThreadContext(m_handle, &tempContext);
    _ASSERTE(succ);
}

// Turns off the stepping flag internally. If the user was also not using it then
// the flag is turned off on the context
VOID CordbUnmanagedThread::EndStepping()
{
    _ASSERTE(!IsGenericHijacked() && !IsFirstChanceHijacked());
    _ASSERTE(IsSSFlagNeeded());

    DT_CONTEXT tempContext;
    tempContext.ContextFlags = DT_CONTEXT_FULL;
    BOOL succ = DbiGetThreadContext(m_handle, &tempContext);
    _ASSERTE(succ);

    if(IsSSFlagHidden())
    {
        UnsetSSFlag(&tempContext);
        ClearState(CUTS_IsSSFlagHidden);
    }
    ClearState(CUTS_IsSSFlagNeeded);

    succ = DbiSetThreadContext(m_handle, &tempContext);
    _ASSERTE(succ);
}


// Writes some details of the given context into the debugger log
VOID CordbUnmanagedThread::LogContext(DT_CONTEXT* pContext)
{
#if defined(TARGET_X86)
    LOG((LF_CORDB, LL_INFO10000,
        "CUT::LC: Eip=0x%08x, Esp=0x%08x, Eflags=0x%08x\n", pContext->Eip, pContext->Esp,
        pContext->EFlags));
#elif defined(TARGET_AMD64)
    LOG((LF_CORDB, LL_INFO10000,
        "CUT::LC: Rip=" FMT_ADDR ", Rsp=" FMT_ADDR ", Eflags=0x%08x\n",
        DBG_ADDR(pContext->Rip),
        DBG_ADDR(pContext->Rsp),
        pContext->EFlags));    // EFlags is still 32bits on AMD64
#elif defined(TARGET_ARM64)
    LOG((LF_CORDB, LL_INFO10000,
        "CUT::LC: Pc=" FMT_ADDR ", Sp=" FMT_ADDR ", Lr=" FMT_ADDR ", Cpsr=" FMT_ADDR "\n",
        DBG_ADDR(pContext->Pc),
        DBG_ADDR(pContext->Sp),
        DBG_ADDR(pContext->Lr),
        DBG_ADDR(pContext->Cpsr)));
#else   // TARGET_X86
    PORTABILITY_ASSERT("LogContext needs a PC and stack pointer.");
#endif  // TARGET_X86
}

// Hijacks this thread using the FirstChanceSuspend hijack
HRESULT CordbUnmanagedThread::SetupFirstChanceHijackForSync()
{
    HRESULT hr = S_OK;

    CONSISTENCY_CHECK(!IsBlockingForSync()); // Shouldn't double hijack
    CONSISTENCY_CHECK(!IsCantStop()); // must be in stoppable-region.
    _ASSERTE(HasIBEvent());

    // We used to hijack for real here but now we have a vectored exception handler that will always be
    // triggered. So we don't have hijack in the sense that we overwrite the thread's IP. However we still
    // set the flag so that when we receive the HijackStartedSignal from the LS we know that this thread
    // should block in there rather than continuing.
    //hr = SetupFirstChanceHijack(EHijackReason::kFirstChanceSuspend, &(IBEvent()->m_currentDebugEvent.u.Exception.ExceptionRecord));

    _ASSERTE(!IsFirstChanceHijacked());
    _ASSERTE(!IsGenericHijacked());
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    // We'd better not be hijacking in a can't stop region!
    // This also means we can't hijack in coopeative (since that's a can't-stop)
    _ASSERTE(!IsCantStop());

    // we should not be stepping into hijacks
    _ASSERTE(!IsSSFlagHidden());
    _ASSERTE(!IsSSFlagNeeded());
    _ASSERTE(!IsContextSet());

    // snapshot the current context so we can start spoofing it
    LOG((LF_CORDB, LL_INFO10000, "CUT::SFCHFS: hijackCtx started as:\n"));
    LogContext(GetHijackCtx());

    // Save the thread's full context.
    DT_CONTEXT context;
    context.ContextFlags = DT_CONTEXT_FULL;
    BOOL succ = DbiGetThreadContext(m_handle, &context);
    _ASSERTE(succ);
    // for debugging when GetThreadContext fails
    if(!succ)
    {
        DWORD error = GetLastError();
        LOG((LF_CORDB, LL_ERROR, "CUT::SFCHFS: DbiGetThreadContext error=0x%x\n", error));
    }

    GetHijackCtx()->ContextFlags = DT_CONTEXT_FULL;
    CORDbgCopyThreadContext(GetHijackCtx(), &context);
    LOG((LF_CORDB, LL_INFO10000, "CUT::SFCHFS: thread=0x%x Hijacking for sync. Original context is:\n", this));
    LogContext(GetHijackCtx());

    // We're hijacking now...
    SetState(CUTS_FirstChanceHijacked);
    GetProcess()->m_state |= CordbProcess::PS_HIJACKS_IN_PLACE;

    // We'll decrement this once the hijack returns
    GetProcess()->m_cFirstChanceHijackedThreads++;
    this->SetState(CUTS_BlockingForSync);

    // we don't want to single step into the vectored exception handler
    // we will restore the SS flag after returning from the hijack
    if(IsSSFlagEnabled(&context))
    {
        LOG((LF_CORDB, LL_INFO10000, "CUT::SFCHFS: thread=0x%x Clearing SS flag\n", this));
        UnsetSSFlag(&context);
        succ = DbiSetThreadContext(m_handle, &context);
        _ASSERTE(succ);
    }



    // There's a bizarre race where the thread was suspended right as the thread was about to dispatch a
    // debug event. We still get the debug event, and then may try to hijack. Resume the thread so that
    // it can run to the hijack.
    if (this->IsSuspended())
    {
        LOG((LF_CORDB, LL_ERROR, "CUT::SFCHFS: thread was suspended... resuming\n"));
        DWORD success = ResumeThread(this->m_handle);

        if (success == 0xFFFFFFFF)
        {
            // Since we suspended it, we should be able to resume it in this window.
            CONSISTENCY_CHECK_MSGF(false, ("Failed to resume thread: tid=0x%x!", this->m_id));
        }
        else
        {
            this->ClearState(CUTS_Suspended);
        }
    }

    return hr;

}

HRESULT CordbUnmanagedThread::SetupFirstChanceHijack(EHijackReason::EHijackReason reason, const EXCEPTION_RECORD * pExceptionRecord)
{
    _ASSERTE(!IsFirstChanceHijacked());
    _ASSERTE(!IsGenericHijacked());
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    // We'd better not be hijacking in a can't stop region!
    // This also means we can't hijack in coopeative (since that's a can't-stop)
    _ASSERTE(!IsCantStop());

    // we should not be stepping into hijacks
    _ASSERTE(!IsSSFlagHidden());
    _ASSERTE(!IsSSFlagNeeded());

    // There's a bizarre race where the thread was suspended right as the thread was about to dispatch a
    // debug event. We still get the debug event, and then may try to hijack. Resume the thread so that
    // it can run to the hijack.
    if (this->IsSuspended())
    {
        DWORD succ = ResumeThread(this->m_handle);

        if (succ == 0xFFFFFFFF)
        {
            // Since we suspended it, we should be able to resume it in this window.
            CONSISTENCY_CHECK_MSGF(false, ("Failed to resume thread: tid=0x%x!", this->m_id));
        }
        else
        {
            this->ClearState(CUTS_Suspended);
        }
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
// We save off the SEH handler on X86 to make sure we restore it properly after the hijack is complete
// The hijacks don't return normally and the SEH chain might have handlers added that don't get removed by default
#ifdef TARGET_X86
        hr = SaveCurrentLeafSeh();
        if(FAILED(hr))
            ThrowHR(hr);
#endif
        CORDB_ADDRESS LSContextAddr;
        GetProcess()->GetDAC()->Hijack(VMPTR_Thread::NullPtr(),
                                       GetOSTid(),
                                       pExceptionRecord,
                                       (T_CONTEXT*) GetHijackCtx(),
                                       sizeof(T_CONTEXT),
                                       reason,
                                       NULL,
                                       &LSContextAddr);
        LOG((LF_CORDB, LL_INFO10000, "CUT::SFCH: pLeftSideContext=0x%p\n", LSContextAddr));
        m_pLeftSideContext.Set(CORDB_ADDRESS_TO_PTR(LSContextAddr));
    }
    EX_CATCH_HRESULT(hr);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO10000, "CUT::SFCH: Error setting up hijack context hr=0x%x\n", hr));
        return hr;
    }


    // We're hijacked now...
    SetState(CUTS_FirstChanceHijacked);
    GetProcess()->m_state |= CordbProcess::PS_HIJACKS_IN_PLACE;

    // We'll decrement this once the hijack returns
    GetProcess()->m_cFirstChanceHijackedThreads++;

    return S_OK;
}

HRESULT CordbUnmanagedThread::SetupGenericHijack(DWORD eventCode, const EXCEPTION_RECORD * pRecord)
{
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    _ASSERTE(eventCode == EXCEPTION_DEBUG_EVENT);

    _ASSERTE(!IsFirstChanceHijacked());
    _ASSERTE(!IsGenericHijacked());
    _ASSERTE(!IsContextSet());

    // Save the thread's full context.
    GetHijackCtx()->ContextFlags = DT_CONTEXT_FULL;

    BOOL succ = DbiGetThreadContext(m_handle, GetHijackCtx());

    if (!succ)
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::SGH: couldn't get thread context: %d\n", GetLastError()));
        return HRESULT_FROM_WIN32(GetLastError());
    }

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)

    // On X86 Debugger::GenericHijackFunc() ensures the stack is walkable
    // by simply using the EBP chain, therefore we can execute the hijack
    // by setting the thread's context EIP to point to this function.
    // On X64, however, we first attempt to set up a "proper" hijack, with
    // a function that allows the OS to unwind the stack (ExceptionHijack).
    // If this fails we'll use the same method as on X86, even though the
    // stack will become un-walkable

    ULONG32 dwThreadId = GetOSTid();
    CordbThread * pThread = GetProcess()->TryLookupOrCreateThreadByVolatileOSId(dwThreadId);

    // For threads in the thread store we set up the full size
    // hijack, otherwise we fallback to hijacking by SetIP.
    if (pThread != NULL)
    {
        HRESULT hr = S_OK;
        EX_TRY
        {
            // Note that the data-target is not atomic, and we have no rollback mechanism.
            // We have to do several writes. If the data-target fails the writes half-way through the
            // target will be inconsistent.
            GetProcess()->GetDAC()->Hijack(
                    pThread->m_vmThreadToken,
                    dwThreadId,
                    pRecord,
                    (T_CONTEXT*) GetHijackCtx(),
                    sizeof(T_CONTEXT),
                    EHijackReason::kGenericHijack,
                    NULL,
                    NULL);
        }
        EX_CATCH_HRESULT(hr);
        if (SUCCEEDED(hr))
        {
            // Remember that we've hijacked the thread.
            SetState(CUTS_GenericHijacked);

            return S_OK;
        }

        STRESS_LOG1(LF_CORDB, LL_INFO1000, "CUT::SGH: Error setting up hijack context hr=0x%x\n", hr);
        // fallthrough (above hijack might have failed due to stack overflow, for example)

    }
    // else (non-threadstore threads) fallthrough

#endif // TARGET_AMD64 || defined(TARGET_ARM64)

    // Remember that we've hijacked the thread.
    SetState(CUTS_GenericHijacked);

    LOG((LF_CORDB, LL_INFO1000000, "CUT::SGH: Current IP is 0x%08x\n", CORDbgGetIP(GetHijackCtx())));

    DebuggerIPCRuntimeOffsets *pRO = &(GetProcess()->m_runtimeOffsets);

    // Wack the IP over to our generic hijack function.
    LPVOID holdIP = CORDbgGetIP(GetHijackCtx());
    CORDbgSetIP(GetHijackCtx(), pRO->m_genericHijackFuncAddr);

    LOG((LF_CORDB, LL_INFO1000000, "CUT::SGH: New IP is 0x%08x\n", CORDbgGetIP(GetHijackCtx())));

    // We should never single step into the hijack
    BOOL isSSFlagOn = IsSSFlagEnabled(GetHijackCtx());
    if(isSSFlagOn)
    {
        UnsetSSFlag(GetHijackCtx());
    }

    succ = DbiSetThreadContext(m_handle, GetHijackCtx());

    if (!succ)
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::SGH: couldn't set thread context: %d\n", GetLastError()));

        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Put the original IP back into the local context copy for later.
    CORDbgSetIP(GetHijackCtx(), holdIP);
    // Set the original SS flag into the local context copy for later
    if(isSSFlagOn)
    {
        SetSSFlag(GetHijackCtx());
    }
    return S_OK;
}

HRESULT CordbUnmanagedThread::FixupFromGenericHijack()
{
    LOG((LF_CORDB, LL_INFO1000, "CUT::FFGH: fixing up from generic hijack. Eip=0x%p, Esp=0x%p\n",
         CORDbgGetIP(GetHijackCtx()), CORDbgGetSP(GetHijackCtx())));

    // We're no longer hijacked
    _ASSERTE(IsGenericHijacked());
    ClearState(CUTS_GenericHijacked);

    // Clear the exception so we do a DBG_CONTINUE with the original context. Note: we only do generic hijacks on
    // in-band events.
    IBEvent()->SetState(CUES_ExceptionCleared);

    // Using the context we saved when the event came in originally or the new context if set by user,
    // reset the thread as if it were never hijacked.
    BOOL succ = DbiSetThreadContext(m_handle, GetHijackCtx());
    // if the user set the context it has been applied now
    ClearState(CUTS_HasContextSet);

    if (!succ)
    {
        LOG((LF_CORDB, LL_INFO1000, "CUT::FFGH: couldn't set thread context: %d\n", GetLastError()));

        return HRESULT_FROM_WIN32(GetLastError());
    }

    return S_OK;
}

DT_CONTEXT * CordbUnmanagedThread::GetHijackCtx()
{
    return &m_context;
}


// Enable Single-Step (and bump the eip back one)
// This can only be called after a bp. (because we assume that we executed a bp when we adjust the eip).
HRESULT CordbUnmanagedThread::EnableSSAfterBP()
{
    DT_CONTEXT c;
    c.ContextFlags = DT_CONTEXT_FULL;

    BOOL succ = DbiGetThreadContext(m_handle, &c);

    if (!succ)
        return HRESULT_FROM_WIN32(GetLastError());

    SetSSFlag(&c);

    // Backup IP to point to the instruction we need to execute. Continuing from a breakpoint exception
    // continues execution at the instruction after the breakpoint, but we need to continue where the
    // breakpoint was.
    CORDbgAdjustPCForBreakInstruction(&c);

    succ = DbiSetThreadContext(m_handle, &c);

    if (!succ)
    {
        return HRESULT_FROM_WIN32(GetLastError());
    }

    return S_OK;
}

//
// FixupAfterOOBException automatically gets the debuggee past an OOB exception event. These are only BP or SS
// events. For SS, we just clear it, assuming that the only reason the thread was stepped in such place was to get it
// off of a BP. For a BP, we clear and backup the IP by one, and turn the trace flag on under the assumption that the
// only thing a debugger is allowed to do with an OOB BP exception is to get us off of it.
//
HRESULT CordbUnmanagedThread::FixupAfterOOBException(CordbUnmanagedEvent *ue)
{
    // We really should only be doing things to single steps and breakpoint exceptions.
    if (ue->m_currentDebugEvent.dwDebugEventCode == EXCEPTION_DEBUG_EVENT)
    {
        DWORD ec = ue->m_currentDebugEvent.u.Exception.ExceptionRecord.ExceptionCode;

        if ((ec == STATUS_BREAKPOINT) || (ec == STATUS_SINGLE_STEP))
        {
            // Automatically clear the exception.
            ue->SetState(CUES_ExceptionCleared);

            // Don't bother about toggling the single-step flag. OOB BPs should only be called
            // for raw int3 instructions, so no need to rewind and reexecute.
        }
    }

    return S_OK;
}


//-----------------------------------------------------------------------------
// Setup to skip an native breakpoint
//-----------------------------------------------------------------------------
void CordbUnmanagedThread::SetupForSkipBreakpoint(NativePatch * pNativePatch)
{
    _ASSERTE(pNativePatch != NULL);
    _ASSERTE(!IsSkippingNativePatch());
    _ASSERTE(m_pPatchSkipAddress == NULL);
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    SetState(CUTS_SkippingNativePatch);

#ifdef _DEBUG
    // For debugging, provide a way that Cordbg devs can see if we're silently skipping BPs.
    static DWORD fTrapOnSkip = -1;
    if (fTrapOnSkip == -1)
        fTrapOnSkip = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgTrapOnSkip);

    if (fTrapOnSkip)
    {
        CONSISTENCY_CHECK_MSGF(false, ("The CLR is skipping a native BP at %p on thread 0x%x (%d)."
            "\nYou're getting this notification in debug builds b/c you have com+ var 'DbgTrapOnSkip' enabled.",
            pNativePatch->pAddress, this->m_id, this->m_id));

        // We skipped this BP b/c IsCantStop was true. For debugging convenience, call IsCantStop here
        // (in case we break at the assert above and want to trace why we're in a CS region)
        bool fCantStop = this->IsCantStop();
        LOG((LF_CORDB, LL_INFO1000, "In Can'tStopRegion = %d\n", fCantStop));

        // Refresh the reg key
        fTrapOnSkip = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgTrapOnSkip);
    }
#endif
#if defined(TARGET_X86)
    STRESS_LOG2(LF_CORDB, LL_INFO100, "CUT::SetupSkip. addr=%p. Opcode=%x\n", pNativePatch->pAddress, (DWORD) pNativePatch->opcode);
#endif

    // Replace the BP w/ the opcode.
    RemoveRemotePatch(GetProcess(), pNativePatch->pAddress, pNativePatch->opcode);

    // Enable the SS flag & Adjust IP.
    HRESULT hr = this->EnableSSAfterBP();
    SIMPLIFYING_ASSUMPTION(SUCCEEDED(hr));


    // Now we return,
    // Process continues, LS will single step past BP, and fire a SS exception.
    // When we get the SS, we res


    // We need to remember this so we can make sure we fixup at the proper address.
    // The address of a ss exception is the instruction we finish on, not where
    // we originally placed the BP. Since instructions can be variable length,
    // we can't work backwards.
    m_pPatchSkipAddress = pNativePatch->pAddress;
}

//-----------------------------------------------------------------------------
// Second half of skipping a native bp.
// Note we pass the address in b/c our caller has (from the debug_evet), and
// we don't want to waste storage to remember it ourselves.
//-----------------------------------------------------------------------------
void CordbUnmanagedThread::FixupForSkipBreakpoint()
{
    _ASSERTE(m_pPatchSkipAddress != NULL);
    _ASSERTE(IsSkippingNativePatch());
    _ASSERTE(GetProcess()->ThreadHoldsProcessLock());

    ClearState(CUTS_SkippingNativePatch);

    // Only reapply the int3 if it hasn't been removed yet.
    if (GetProcess()->GetNativePatch(m_pPatchSkipAddress) != NULL)
    {
        ApplyRemotePatch(GetProcess(), m_pPatchSkipAddress);
        STRESS_LOG1(LF_CORDB, LL_INFO100, "CUT::FixupSetupSkip. addr=%p\n", m_pPatchSkipAddress);
    }
    else
    {
        STRESS_LOG1(LF_CORDB, LL_INFO100, "CUT::FixupSetupSkip. Patch removed. Not-reading. addr=%p\n", m_pPatchSkipAddress);
    }

    m_pPatchSkipAddress = NULL;
}

inline TADDR GetSP(DT_CONTEXT* context)
{
#if defined(TARGET_X86)
    return (TADDR)context->Esp;
#elif defined(TARGET_AMD64)
    return (TADDR)context->Rsp;
#elif defined(TARGET_ARM) || defined(TARGET_ARM64)
    return (TADDR)context->Sp;
#else
    _ASSERTE(!"nyi for platform");
#endif
}

BOOL CordbUnmanagedThread::GetStackRange(CORDB_ADDRESS *pBase, CORDB_ADDRESS *pLimit)
{
#if !defined(FEATURE_DBGIPC_TRANSPORT)

    if (m_stackBase == 0 && m_stackLimit == 0)
    {
        HANDLE hProc;
        DT_CONTEXT tempContext;
        MEMORY_BASIC_INFORMATION mbi;

        tempContext.ContextFlags = DT_CONTEXT_FULL;
        if (SUCCEEDED(GetProcess()->GetHandle(&hProc)) &&
            SUCCEEDED(GetThreadContext(&tempContext)) &&
            ::VirtualQueryEx(hProc, (LPCVOID)GetSP(&tempContext), &mbi, sizeof(mbi)) != 0)
        {
            // the lowest stack address is the AllocationBase
            TADDR limit = PTR_TO_TADDR(mbi.AllocationBase);

            // Now, on to find the stack base:
            // Closest to the AllocationBase we might have a MEM_RESERVED block
            // for all the as yet unallocated pages...
            TADDR regionBase = limit;
            if (::VirtualQueryEx(hProc, (LPCVOID) regionBase, &mbi, sizeof(mbi)) == 0
                || mbi.Type != MEM_PRIVATE)
                goto Exit;

            if (mbi.State == MEM_RESERVE)
                regionBase += mbi.RegionSize;

            // Next we might have a few guard pages
            if (::VirtualQueryEx(hProc, (LPCVOID) regionBase, &mbi, sizeof(mbi)) == 0
                || mbi.Type != MEM_PRIVATE)
                goto Exit;

            if (mbi.State == MEM_COMMIT && (mbi.Protect & PAGE_GUARD) != 0)
                regionBase += mbi.RegionSize;

            // And finally the "regular" stack region
            if (::VirtualQueryEx(hProc, (LPCVOID) regionBase, &mbi, sizeof(mbi)) == 0
                || mbi.Type != MEM_PRIVATE)
                goto Exit;

            if (mbi.State == MEM_COMMIT && (mbi.Protect & PAGE_READWRITE) != 0)
                regionBase += mbi.RegionSize;

            if (limit == regionBase)
                goto Exit;

            m_stackLimit = limit;
            m_stackBase = regionBase;
        }
    }

Exit:
    if (pBase != NULL)
        *pBase = m_stackBase;
    if (pLimit != NULL)
        *pLimit = m_stackLimit;

    return (m_stackBase != 0 || m_stackLimit != 0);

#else

    if (pBase != NULL)
        *pBase = 0;
    if (pLimit != NULL)
        *pLimit = 0;

    return FALSE;

#endif // FEATURE_DBGIPC_TRANSPORT
}

//-----------------------------------------------------------------------------
// Returns the thread context to the state it was in when it last entered RaiseException
// This allows the thread to retrigger an exception caused by RaiseException
//-----------------------------------------------------------------------------
void CordbUnmanagedThread::HijackToRaiseException()
{
    LOG((LF_CORDB, LL_INFO1000, "CP::HTRE: hijacking to RaiseException\n"));
    _ASSERTE(HasRaiseExceptionEntryCtx());
    _ASSERTE(!IsRaiseExceptionHijacked());
    _ASSERTE(!IsGenericHijacked());
    _ASSERTE(!IsFirstChanceHijacked());
    _ASSERTE(!IsContextSet());

    BOOL succ = DbiGetThreadContext(m_handle, GetHijackCtx());
    _ASSERTE(succ);
    succ = DbiSetThreadContext(m_handle, &m_raiseExceptionEntryContext);
    _ASSERTE(succ);
    SetState(CUTS_IsRaiseExceptionHijacked);
}

//----------------------------------------------------------------------------
// Returns the context to its unhijacked state.
//----------------------------------------------------------------------------
void CordbUnmanagedThread::RestoreFromRaiseExceptionHijack()
{
    LOG((LF_CORDB, LL_INFO1000, "CP::RFREH: ending RaiseException hijack\n"));
    _ASSERTE(IsRaiseExceptionHijacked());

    DT_CONTEXT restoreContext;
    restoreContext.ContextFlags = DT_CONTEXT_FULL;
    HRESULT hr = GetThreadContext(&restoreContext);
    _ASSERTE(SUCCEEDED(hr));

    ClearState(CUTS_IsRaiseExceptionHijacked);
    hr = SetThreadContext(&restoreContext);
    _ASSERTE(SUCCEEDED(hr));
}

//-----------------------------------------------------------------------------
// Attempts to store the state of a thread currently entering RaiseException
// This grabs both a full context and enough state to determine what exception
// RaiseException should be raising. If any of the state can not be retrieved
// then this entrance to RaiseException is silently ignored
//-----------------------------------------------------------------------------
void CordbUnmanagedThread::SaveRaiseExceptionEntryContext()
{
    _ASSERTE(FALSE); // should be unused now
    LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: saving raise exception context.\n"));
    _ASSERTE(!HasRaiseExceptionEntryCtx());
    _ASSERTE(!IsRaiseExceptionHijacked());
    HRESULT hr = S_OK;
    DT_CONTEXT context;
    context.ContextFlags = DT_CONTEXT_FULL;
    DbiGetThreadContext(m_handle, &context);
    // if the flag is set, unset it
    // we don't want to be single stepping through RaiseException the second time
    // sending out OOB SS events. Ultimately we will rethrow the exception which would
    // cleared the SS flag anyways.
    UnsetSSFlag(&context);
    memcpy(&m_raiseExceptionEntryContext, &context,  sizeof(DT_CONTEXT));

    // calculate the exception that we would expect to come from this invocation of RaiseException
    REMOTE_PTR pExceptionInformation = NULL;
#if defined(TARGET_AMD64)
    m_raiseExceptionExceptionCode = (DWORD)m_raiseExceptionEntryContext.Rcx;
    m_raiseExceptionExceptionFlags = (DWORD)m_raiseExceptionEntryContext.Rdx;
    m_raiseExceptionNumberParameters = (DWORD)m_raiseExceptionEntryContext.R8;
    pExceptionInformation = (REMOTE_PTR)m_raiseExceptionEntryContext.R9;
#elif defined(TARGET_ARM64)
    m_raiseExceptionExceptionCode = (DWORD)m_raiseExceptionEntryContext.X0;
    m_raiseExceptionExceptionFlags = (DWORD)m_raiseExceptionEntryContext.X1;
    m_raiseExceptionNumberParameters = (DWORD)m_raiseExceptionEntryContext.X2;
    pExceptionInformation = (REMOTE_PTR)m_raiseExceptionEntryContext.X3;
#elif defined(TARGET_X86)
    hr = m_pProcess->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)m_raiseExceptionEntryContext.Esp+4), &m_raiseExceptionExceptionCode);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: failed to read exception code.\n"));
        return;
    }
    hr = m_pProcess->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)m_raiseExceptionEntryContext.Esp+8), &m_raiseExceptionExceptionFlags);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: failed to read exception flags.\n"));
        return;
    }
    hr = m_pProcess->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)m_raiseExceptionEntryContext.Esp+12), &m_raiseExceptionNumberParameters);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: failed to read number of parameters.\n"));
        return;
    }
    hr = m_pProcess->SafeReadStruct(PTR_TO_CORDB_ADDRESS((BYTE*)m_raiseExceptionEntryContext.Esp+16), &pExceptionInformation);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: failed to read exception information pointer.\n"));
        return;
    }
#else
    _ASSERTE(!"Implement this for your platform");
    return;
#endif
    LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: RaiseException parameters are 0x%x 0x%x 0x%x 0x%p.\n",
        m_raiseExceptionExceptionCode, m_raiseExceptionExceptionFlags,
        m_raiseExceptionNumberParameters, pExceptionInformation));
    TargetBuffer exceptionInfoTargetBuffer(pExceptionInformation, sizeof(REMOTE_PTR)*m_raiseExceptionNumberParameters);
    EX_TRY
    {
        m_pProcess->SafeReadBuffer(exceptionInfoTargetBuffer, (BYTE*)m_raiseExceptionExceptionInformation);
    }
    EX_CATCH_HRESULT(hr);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::SREEC: failed to read exception information.\n"));
        return;
    }

    // If everything was successful then set this flag, otherwise none of the above data is considered valid
    SetState(CUTS_HasRaiseExceptionEntryCtx);
    return;
}

//-----------------------------------------------------------------------------
// Clears all the state saved in SaveRaiseExceptionContext and returns the thread
// to the state as if RaiseException has yet to be called. This is typically called
// after an exception retriggers or after determining that the exception never will
// retrigger.
//-----------------------------------------------------------------------------
void CordbUnmanagedThread::ClearRaiseExceptionEntryContext()
{
    _ASSERTE(FALSE); // should be unused now
    LOG((LF_CORDB, LL_INFO1000, "CP::CREEC: clearing raise exception context.\n"));
    _ASSERTE(HasRaiseExceptionEntryCtx());
    ClearState(CUTS_HasRaiseExceptionEntryCtx);
}

//-----------------------------------------------------------------------------
// Uses a heuristic to determine if the given exception record is likely to be the exception
// raised by the last invocation of RaiseException on this thread. The current heuristic compares
// ExceptionCode, ExceptionFlags, and all ExceptionInformation.
//-----------------------------------------------------------------------------
BOOL CordbUnmanagedThread::IsExceptionFromLastRaiseException(const EXCEPTION_RECORD* pExceptionRecord)
{
    _ASSERTE(FALSE); // should be unused now
    if(!HasRaiseExceptionEntryCtx())
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: not a match - no previous raise context\n"));
        return FALSE;
    }

    if (pExceptionRecord->ExceptionCode != m_raiseExceptionExceptionCode)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: not a match - exception codes differ 0x%x 0x%x\n",
            pExceptionRecord->ExceptionCode, m_raiseExceptionExceptionCode));
        return FALSE;
    }

    if (pExceptionRecord->ExceptionFlags != m_raiseExceptionExceptionFlags)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: not a match - exception flags differ 0x%x 0x%x\n",
            pExceptionRecord->ExceptionFlags, m_raiseExceptionExceptionFlags));
        return FALSE;
    }

    if (pExceptionRecord->NumberParameters != m_raiseExceptionNumberParameters)
    {
        LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: not a match - number parameters differ 0x%x 0x%x\n",
            pExceptionRecord->NumberParameters, m_raiseExceptionNumberParameters));
        return FALSE;
    }

    for(DWORD i = 0; i < pExceptionRecord->NumberParameters; i++)
    {
        if(m_raiseExceptionExceptionInformation[i] != pExceptionRecord->ExceptionInformation[i])
        {
            LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: not a match - param %d differs 0x%x 0x%x\n",
                i, pExceptionRecord->ExceptionInformation[i], m_raiseExceptionExceptionInformation[i]));
            return FALSE;
        }
    }

    LOG((LF_CORDB, LL_INFO1000, "CP::IEFLRE: match\n"));
    return TRUE;
}


//-----------------------------------------------------------------------------
// Inject an int3 at the given remote address
//-----------------------------------------------------------------------------

// This flavor is assuming our caller already knows the opcode.
HRESULT ApplyRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    const BYTE patch = CORDbg_BREAK_INSTRUCTION;
#elif defined(TARGET_ARM64)
    const PRD_TYPE patch = CORDbg_BREAK_INSTRUCTION;
#else
    const BYTE patch = 0;
    PORTABILITY_ASSERT("NYI: ApplyRemotePatch for this platform");
#endif
    HRESULT hr = pProcess->SafeWriteStruct(PTR_TO_CORDB_ADDRESS(pRemoteAddress), &patch);
    SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    return S_OK;
}


// Get the opcode that we're replacing.
HRESULT ApplyRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress, PRD_TYPE * pOpcode)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Read out opcode. 1 byte on x86
    BYTE opcode;
#elif defined(TARGET_ARM64)
    // Read out opcode. 4 bytes on arm64
    PRD_TYPE opcode;
#else
    BYTE opcode;
    PORTABILITY_ASSERT("NYI: ApplyRemotePatch for this platform");
#endif

    HRESULT hr = pProcess->SafeReadStruct(PTR_TO_CORDB_ADDRESS(pRemoteAddress), &opcode);
    if (FAILED(hr))
    {
        return hr;
    }

    *pOpcode = (PRD_TYPE) opcode;
    ApplyRemotePatch(pProcess, pRemoteAddress);
    return S_OK;
}

//-----------------------------------------------------------------------------
// Remove the int3 from the remote address
//-----------------------------------------------------------------------------
HRESULT RemoveRemotePatch(CordbProcess * pProcess, const void * pRemoteAddress, PRD_TYPE opcode)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Replace the BP w/ the opcode.
    BYTE opcode2 = (BYTE) opcode;
#elif defined(TARGET_ARM64)
    // 4 bytes on arm64
    PRD_TYPE opcode2 = opcode;
#else
    PRD_TYPE opcode2 = opcode;
    PORTABILITY_ASSERT("NYI: RemoveRemotePatch for this platform");
#endif

    pProcess->SafeWriteStruct(PTR_TO_CORDB_ADDRESS(pRemoteAddress), &opcode2);

    // This may fail because the module has been unloaded.  In which case, the patch is also
    // gone so it makes sense to return success.
    return S_OK;
}
#endif // FEATURE_INTEROP_DEBUGGING

//---------------------------------------------------------------------------------------
//
// Simple helper to return the SP value stored in a DebuggerREGDISPLAY.
//
// Arguments:
//    pDRD  - the DebuggerREGDISPLAY in question
//
// Return Value:
//    the SP value
//

inline CORDB_ADDRESS GetSPFromDebuggerREGDISPLAY(DebuggerREGDISPLAY* pDRD)
{
    return pDRD->SP;
}


HRESULT CordbContext::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugContext)
        *pInterface = static_cast<ICorDebugContext*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugContext*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}


/* ------------------------------------------------------------------------- *
 * Frame class
 * ------------------------------------------------------------------------- */


// This is just used as a proxy object to pass a FramePointer around.
CordbFrame::CordbFrame(CordbProcess * pProcess, FramePointer fp)
 : CordbBase(pProcess, 0, enumCordbFrame),
 m_fp(fp)
{
    UnsafeNeuterDeadObject(); // mark as neutered.
}


CordbFrame::CordbFrame(CordbThread *    pThread,
                       FramePointer     fp,
                       SIZE_T           ip,
                       CordbAppDomain * pCurrentAppDomain)
  : CordbBase(pThread->GetProcess(), 0, enumCordbFrame),
    m_ip(ip),
    m_pThread(pThread),
    m_currentAppDomain(pCurrentAppDomain),
    m_fp(fp)
{
#ifdef _DEBUG
    // For debugging purposes, track what Continue session these frames were created in.
    m_DbgContinueCounter = GetProcess()->m_continueCounter;
#endif

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_pThread->GetRefreshStackNeuterList()->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}


CordbFrame::~CordbFrame()
{
    _ASSERTE(IsNeutered());
}

// Neutered by DerivedClasses
void CordbFrame::Neuter()
{
    CordbBase::Neuter();
}


HRESULT CordbFrame::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugFrame)
        *pInterface = static_cast<ICorDebugFrame*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugFrame*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

// ----------------------------------------------------------------------------
// CordbFrame::GetChain
//
// Description:
//    Return the owning chain.  Since chains have been  deprecated in Arrowhead,
//    this function returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppChain - out parameter; return the owning chain
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppChain is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//    Return E_FAIL if failed to find the chain
//

HRESULT CordbFrame::GetChain(ICorDebugChain **ppChain)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(ppChain);
        *ppChain = NULL;

        if (GetProcess()->GetShim() != NULL)
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0(GetProcess(), GET_PUBLIC_LOCK_HOLDER());
            ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(m_pThread);
            pSSW->GetChainForFrame(static_cast<ICorDebugFrame *>(this), ppChain);

            if (*ppChain == NULL)
                hr = E_FAIL;
        }
        else
        {
            // This is the Arrowhead case, where ICDChain has been deprecated.
            hr = E_NOTIMPL;
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}


// Return the stack range taken up by this frame.
// Note that this is not implemented in the base CordbFrame class.
// Instead, this is implemented by the derived classes.
// The start of the stack range is the leafmost boundary, and the end is the rootmost boundary.
//
// Notes: see code:#GetStackRange
HRESULT CordbFrame::GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(pStart);
        ValidateOrThrow(pEnd);

        hr = E_NOTIMPL;
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

// Return the ICorDebugFunction associated with this frame.
// There is one ICorDebugFunction for each EnC version of a method.
HRESULT CordbFrame::GetFunction(ICorDebugFunction **ppFunction)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(ppFunction);

        CordbFunction * pFunc = this->GetFunction();

        if (pFunc == NULL)
        {
            ThrowHR(CORDBG_E_CODE_NOT_AVAILABLE);
        }

        // @dbgtodo  LCG methods, IL stubs, dynamic language debugging
        // Don't return an ICDFunction if we are dealing with a dynamic method.
        // The dynamic debugging feature crew needs to decide exactly what to hand out for dynamic methods.
        if (pFunc->GetMetadataToken() == mdMethodDefNil)
        {
            ThrowHR(CORDBG_E_CODE_NOT_AVAILABLE);
        }

        *ppFunction = static_cast<ICorDebugFunction *>(pFunc);
        pFunc->ExternalAddRef();
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

// Return the token of the ICorDebugFunction associated with this frame.
// There is one ICorDebugFunction for each EnC version of a method.
HRESULT CordbFrame::GetFunctionToken(mdMethodDef *pToken)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(pToken);

        CordbFunction * pFunc = GetFunction();
        if (pFunc == NULL)
        {
            hr = CORDBG_E_CODE_NOT_AVAILABLE;
        }
        else
        {
            *pToken = pFunc->GetMetadataToken();
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbFrame::GetCaller
//
// Description:
// Return the caller of this frame.  The caller is closer to the root.
//    This function has been deprecated in Arrowhead, and so it returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppFrame - out parameter; return the caller frame
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbFrame::GetCaller(ICorDebugFrame **ppFrame)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(ppFrame);

        *ppFrame = NULL;

        if (GetProcess()->GetShim() != NULL)
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0(GetProcess(), GET_PUBLIC_LOCK_HOLDER());
            ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(m_pThread);
            pSSW->GetCallerForFrame(this, ppFrame);
        }
        else
        {
            *ppFrame = NULL;
            hr = E_NOTIMPL;
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbFrame::GetCallee
//
// Description:
// Return the callee of this frame.  The callee is closer to the leaf.
//    This function has been deprecated in Arrowhead, and so it returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppFrame - out parameter; return the callee frame
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbFrame::GetCallee(ICorDebugFrame **ppFrame)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        ValidateOrThrow(ppFrame);

        *ppFrame = NULL;

        if (GetProcess()->GetShim() != NULL)
        {
            PUBLIC_CALLBACK_IN_THIS_SCOPE0(GetProcess(), GET_PUBLIC_LOCK_HOLDER());
            ShimStackWalk * pSSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(m_pThread);
            pSSW->GetCalleeForFrame(static_cast<ICorDebugFrame *>(this), ppFrame);
        }
        else
        {
            *ppFrame = NULL;
            hr = E_NOTIMPL;
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

// Create a stepper on the frame.
HRESULT CordbFrame::CreateStepper(ICorDebugStepper **ppStepper)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    VALIDATE_POINTER_TO_OBJECT(ppStepper, ICorDebugStepper **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        RSInitHolder<CordbStepper> pStepper(new CordbStepper(m_pThread, this));
        pStepper.TransferOwnershipExternal(ppStepper);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Given a frame pointer, determine if it is in the stack range owned by the frame.
//
// Arguments:
//    fp    - frame pointer to check
//
// Return Value:
//    whether the specified frame pointer is in the stack range or not
//

bool CordbFrame::IsContainedInFrame(FramePointer fp)
{
    CORDB_ADDRESS stackStart;
    CORDB_ADDRESS stackEnd;

    // get the stack range
    HRESULT hr;
    hr = GetStackRange(&stackStart, &stackEnd);
    _ASSERTE(SUCCEEDED(hr));

    CORDB_ADDRESS sp  = PTR_TO_CORDB_ADDRESS(fp.GetSPValue());

    if ((stackStart <= sp) && (sp <= stackEnd))
    {
        return true;
    }
    else
    {
        return false;
    }
}

//---------------------------------------------------------------------------------------
//
// Given an ICorDebugFrame interface pointer, return a pointer to the base class CordbFrame.
//
// Arguments:
//    pFrame    - the ICorDebugFrame interface pointer
//
// Return Value:
//    the CordbFrame pointer corresponding to the specified interface pointer
//
// Note:
//    This is currently only used for continuable exceptions.
//

// static
CordbFrame* CordbFrame::GetCordbFrameFromInterface(ICorDebugFrame *pFrame)
{
    CordbFrame* pTargetFrame = NULL;

    if (pFrame != NULL)
    {
        // test for CordbNativeFrame
        RSExtSmartPtr<ICorDebugNativeFrame> pNativeFrame;
        pFrame->QueryInterface(IID_ICorDebugNativeFrame, (void**)&pNativeFrame);
        if (pNativeFrame != NULL)
        {
            pTargetFrame = static_cast<CordbFrame*>(static_cast<CordbNativeFrame*>(pNativeFrame.GetValue()));
        }
        else
        {
            // test for CordbJITILFrame
            RSExtSmartPtr<ICorDebugILFrame> pILFrame;
            pFrame->QueryInterface(IID_ICorDebugILFrame, (void**)&pILFrame);
            if (pILFrame != NULL)
            {
                pTargetFrame = (static_cast<CordbJITILFrame*>(pILFrame.GetValue()))->m_nativeFrame;
            }
            else
            {
                // test for CordbInternalFrame
                RSExtSmartPtr<ICorDebugInternalFrame> pInternalFrame;
                pFrame->QueryInterface(IID_ICorDebugInternalFrame, (void**)&pInternalFrame);
                if (pInternalFrame != NULL)
                {
                    pTargetFrame = static_cast<CordbFrame*>(static_cast<CordbInternalFrame*>(pInternalFrame.GetValue()));
                }
                else
                {
                    // when all else fails, this is just a CordbFrame
                    pTargetFrame = static_cast<CordbFrame*>(pFrame);
                }
            }
        }
    }
    return pTargetFrame;
}


/* ------------------------------------------------------------------------- *

 * Value Enumerator class
 *
 * Used by CordbJITILFrame for EnumLocalVars & EnumArgs.
 * NOTE NOTE NOTE WE ASSUME that the 'frame' argument is actually the
 * CordbJITILFrame's native frame member variable.
 * ------------------------------------------------------------------------- */

CordbValueEnum::CordbValueEnum(CordbNativeFrame *frame, ValueEnumMode mode) :
    CordbBase(frame->GetProcess(), 0)
{
    _ASSERTE( frame != NULL );
    _ASSERTE( mode == LOCAL_VARS_ORIGINAL_IL || mode == LOCAL_VARS_REJIT_IL || mode == ARGS);

    m_frame = frame;
    m_mode = mode;
    m_iCurrent = 0;
    m_iMax = 0;
}

/*
 * CordbValueEnum::Init
 *
 * Initialize a CordbValueEnum object. Must be called after allocating the object and before using it. If Init
 * fails, then destroy the object and release the memory.
 *
 * Parameters:
 *     none.
 *
 * Returns:
 *    HRESULT for success or failure.
 *
 */
HRESULT CordbValueEnum::Init()
{
    HRESULT hr = S_OK;
    CordbNativeFrame *nil = m_frame;
    CordbJITILFrame *jil = nil->m_JITILFrame;

    switch (m_mode)
    {
        case ARGS:
        {
            // Get the function signature
            CordbFunction *func = m_frame->GetFunction();
            ULONG methodArgCount;

            IfFailRet(func->GetSig(NULL, &methodArgCount, NULL));

            // Grab the argument count for the size of the enumeration.
            m_iMax = methodArgCount;
            if (jil->m_fVarArgFnx && !jil->m_sigParserCached.IsNull())
            {
                m_iMax = jil->m_allArgsCount;
            }
            break;
        }
        case LOCAL_VARS_ORIGINAL_IL:
        {
            // Get the locals signature.
            ULONG localsCount;
            IfFailRet(jil->GetOriginalILCode()->GetLocalVarSig(NULL, &localsCount));

            // Grab the number of locals for the size of the enumeration.
            m_iMax = localsCount;
            break;
        }
        case LOCAL_VARS_REJIT_IL:
        {
            // Get the locals signature.
            ULONG localsCount;
            CordbReJitILCode* pCode = jil->GetReJitILCode();
            if (pCode == NULL)
            {
                m_iMax = 0;
            }
            else
            {
                IfFailRet(pCode->GetLocalVarSig(NULL, &localsCount));

                // Grab the number of locals for the size of the enumeration.
                m_iMax = localsCount;
            }
            break;
        }
    }
    // Everything worked okay, so add this object to the neuter list for objects that are tied to the stack trace.
    EX_TRY
    {
        m_frame->m_pThread->GetRefreshStackNeuterList()->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);

    return hr;
}

CordbValueEnum::~CordbValueEnum()
{
    _ASSERTE(this->IsNeutered());
    _ASSERTE(m_frame == NULL);
}

void CordbValueEnum::Neuter()
{
    m_frame = NULL;
    CordbBase::Neuter();
}



HRESULT CordbValueEnum::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugEnum)
        *pInterface = static_cast<ICorDebugEnum*>(this);
    else if (id == IID_ICorDebugValueEnum)
        *pInterface = static_cast<ICorDebugValueEnum*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugValueEnum*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbValueEnum::Skip(ULONG celt)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = E_FAIL;
    if ( (m_iCurrent+celt) < m_iMax ||
         celt == 0)
    {
        m_iCurrent += celt;
        hr = S_OK;
    }

    return hr;
}

HRESULT CordbValueEnum::Reset()
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    m_iCurrent = 0;
    return S_OK;
}

HRESULT CordbValueEnum::Clone(ICorDebugEnum **ppEnum)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        *ppEnum = NULL;
        RSInitHolder<CordbValueEnum> pCVE(new CordbValueEnum(m_frame, m_mode));

        // Initialize the new enum
        hr = pCVE->Init();
        IfFailThrow(hr);

        pCVE.TransferOwnershipExternal(ppEnum);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbValueEnum::GetCount(ULONG *pcelt)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    if( pcelt == NULL)
    {
        return E_INVALIDARG;
    }

    (*pcelt) = m_iMax;
    return S_OK;
}

//
// In the event of failure, the current pointer will be left at
// one element past the troublesome element.  Thus, if one were
// to repeatedly ask for one element to iterate through the
// array, you would iterate exactly m_iMax times, regardless
// of individual failures.
HRESULT CordbValueEnum::Next(ULONG celt, ICorDebugValue *values[], ULONG *pceltFetched)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT_ARRAY(values, ICorDebugValue *,
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

    int iMax = min( m_iMax, m_iCurrent+celt);
    int i;
    for (i = m_iCurrent; i< iMax;i++)
    {
        switch ( m_mode )
        {
        case ARGS:
            {
                hr = m_frame->m_JITILFrame->GetArgument( i, &(values[i-m_iCurrent]) );
                break;
            }
        case LOCAL_VARS_ORIGINAL_IL:
            {
                hr = m_frame->m_JITILFrame->GetLocalVariableEx(ILCODE_ORIGINAL_IL, i, &(values[i-m_iCurrent]) );
                break;
            }
        case LOCAL_VARS_REJIT_IL:
            {
                hr = m_frame->m_JITILFrame->GetLocalVariableEx(ILCODE_REJIT_IL, i, &(values[i - m_iCurrent]));
                break;
            }
        }
        if ( FAILED( hr ) )
        {
            break;
        }
    }

    int count = (i - m_iCurrent);

    if ( FAILED( hr ) )
    {
        //
        // we failed: +1 pushes us past troublesome element
        //
        m_iCurrent += 1 + count;
    }
    else
    {
        m_iCurrent += count;
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    if (FAILED(hr))
    {
        return hr;
    }


    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (((ULONG)count) < celt)
    {
        return S_FALSE;
    }

    return hr;
}


//-----------------------------------------------------------------------------
// CordbInternalFrame
//-----------------------------------------------------------------------------
CordbInternalFrame::CordbInternalFrame(CordbThread *          pThread,
                                       FramePointer           fp,
                                       CordbAppDomain *       pCurrentAppDomain,
                                       const DebuggerIPCE_STRData * pData)
  : CordbFrame(pThread, fp, 0, pCurrentAppDomain)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    m_eFrameType = pData->stubFrame.frameType;
    m_funcMetadataToken = pData->stubFrame.funcMetadataToken;
    m_vmMethodDesc = pData->stubFrame.vmMethodDesc;

    // Some internal frames may not have a Function associated w/ them.
    if (!IsNilToken(m_funcMetadataToken))
    {
        // Find the module of the function.  Note that this module isn't necessarily in the same domain as our frame.
        // FuncEval frames can point to methods they are going to invoke in another domain.
        CordbModule * pModule = NULL;
        pModule = GetProcess()->LookupOrCreateModule(pData->stubFrame.vmDomainAssembly);
        _ASSERTE(pModule != NULL);

        //
        if( pModule != NULL )
        {
            _ASSERTE( (pModule->GetAppDomain() == pCurrentAppDomain) || (m_eFrameType == STUBFRAME_FUNC_EVAL) );



            mdMethodDef token = pData->stubFrame.funcMetadataToken;

            // @dbgtodo  synchronization - push this up.
            RSLockHolder lockHolder(GetProcess()->GetProcessLock());

            // CordbInternalFrame could handle a null function.
            // But if we fail to lookup, things are not in a good state anyways.
            CordbFunction * pFunction = pModule->LookupOrCreateFunctionLatestVersion(token);
            m_function.Assign(pFunction);

        }
    }
}

CordbInternalFrame::CordbInternalFrame(CordbThread *             pThread,
                                       FramePointer              fp,
                                       CordbAppDomain *          pCurrentAppDomain,
                                       CorDebugInternalFrameType frameType,
                                       mdMethodDef               funcMetadataToken,
                                       CordbFunction *           pFunction,
                                       VMPTR_MethodDesc          vmMethodDesc)
  : CordbFrame(pThread, fp, 0, pCurrentAppDomain)
{
    m_eFrameType = frameType;
    m_funcMetadataToken = funcMetadataToken;
    m_function.Assign(pFunction);
    m_vmMethodDesc = vmMethodDesc;
}

HRESULT CordbInternalFrame::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugFrame)
    {
        *pInterface = static_cast<ICorDebugFrame*>(static_cast<ICorDebugInternalFrame*>(this));
    }
    else if (id == IID_ICorDebugInternalFrame)
    {
        *pInterface = static_cast<ICorDebugInternalFrame*>(this);
    }
    else if (id == IID_ICorDebugInternalFrame2)
    {
        *pInterface = static_cast<ICorDebugInternalFrame2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugInternalFrame*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

void CordbInternalFrame::Neuter()
{
    m_function.Clear();
    CordbFrame::Neuter();
}


// ----------------------------------------------------------------------------
// CordbInternalFrame::GetStackRange
//
// Description:
// Return the stack range owned by this frame.
// The start of the stack range is the leafmost boundary, and the end is the rootmost boundary.
//
// Arguments:
//    * pStart - out parameter; return the leaf end of the frame
//    * pEnd   - out parameter; return the root end of the frame
//
// Return Value:
//    Return S_OK on success.
//
// Notes:
// #GetStackRange
// This is a virtual function and so there are multiple implementations for different types of frames.
// It's very important to note that GetStackRange() can work when even after the frame is neutered.
// Debuggers may rely on this to map old frames up to new frames across Continue() calls.
//

HRESULT CordbInternalFrame::GetStackRange(CORDB_ADDRESS *pStart,
                                        CORDB_ADDRESS *pEnd)
{
    PUBLIC_REENTRANT_API_ENTRY(this);

    // Callers explicit require GetStackRange() to be callable when neutered so that they
    // can line up ICorDebugFrame objects across continues. We only return stack ranges
    // here and don't access any special data.
    OK_IF_NEUTERED(this);

    if (GetProcess()->GetShim() != NULL)
    {
        CORDB_ADDRESS pFramePointer = PTR_TO_CORDB_ADDRESS(GetFramePointer().GetSPValue());
        if (pStart)
        {
            *pStart = pFramePointer;
        }
        if (pEnd)
        {
            *pEnd = pFramePointer;
        }
        return S_OK;
    }
    else
    {
        if (pStart != NULL)
        {
            *pStart = NULL;
        }
        if (pEnd != NULL)
        {
            *pEnd = NULL;
        }
        return E_NOTIMPL;
    }
}


// This may return NULL if there's no Method associated w/ this Frame.
// For FuncEval frames, the function returned might also be in a different AppDomain
// than the frame itself.
CordbFunction * CordbInternalFrame::GetFunction()
{
    return m_function;
}

// Accessor for the shim private hook code:CordbThread::ConvertFrameForILMethodWithoutMetadata.
// Refer to that function for comments on the return value, the argument, etc.
BOOL CordbInternalFrame::ConvertInternalFrameForILMethodWithoutMetadata(
    ICorDebugInternalFrame2 ** ppInternalFrame2)
{
    _ASSERTE(ppInternalFrame2 != NULL);
    *ppInternalFrame2 = NULL;

    // The only internal frame conversion we need to perform is from STUBFRAME_JIT_COMPILATION to
    // STUBFRAME_LIGTHWEIGHT_FUNCTION.
    if (m_eFrameType != STUBFRAME_JIT_COMPILATION)
    {
        return FALSE;
    }

    // Check whether the internal frame has an associated MethodDesc.
    // Currently, the only STUBFRAME_JIT_COMPILATION frame with a NULL MethodDesc is ComPrestubMethodFrame,
    // which is not exposed in Whidbey.  So convert it according to rule #2 below.
    if (m_vmMethodDesc.IsNull())
    {
        return TRUE;
    }

    // Retrieve the type of the method associated with the STUBFRAME_JIT_COMPILATION.
    IDacDbiInterface::DynamicMethodType type = GetProcess()->GetDAC()->IsILStubOrLCGMethod(m_vmMethodDesc);

    // Here are the conversion rules:
    // 1)  For a normal managed method, we don't convert, and we return FALSE.
    // 2)  For an IL stub, we convert to NULL, and we return TRUE.
    // 3)  For a dynamic method, we convert to a STUBFRAME_LIGHTWEIGHT_FUNCTION, and we return TRUE.
    if (type == IDacDbiInterface::kNone)
    {
        return FALSE;
    }
    else if (type == IDacDbiInterface::kILStub)
    {
        return TRUE;
    }
    else if (type == IDacDbiInterface::kLCGMethod)
    {
        // Here we are basically cloning another CordbInternalFrame.
        RSInitHolder<CordbInternalFrame> pInternalFrame(new CordbInternalFrame(m_pThread,
                                                                               m_fp,
                                                                               m_currentAppDomain,
                                                                               STUBFRAME_LIGHTWEIGHT_FUNCTION,
                                                                               m_funcMetadataToken,
                                                                               m_function.GetValue(),
                                                                               m_vmMethodDesc));
        pInternalFrame.TransferOwnershipExternal(ppInternalFrame2);
        return TRUE;
    }

    UNREACHABLE();
}


//---------------------------------------------------------------------------------------
//
// Returns the address of an internal frame.  The address is a stack pointer, even on IA64.
//
// Arguments:
//    pAddress - out parameter; return the frame marker address
//
// Return Value:
//    S_OK on success.
//    E_INVALIDARG if pAddress is NULL.
//

HRESULT CordbInternalFrame::GetAddress(CORDB_ADDRESS * pAddress)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (pAddress == NULL)
    {
        return E_INVALIDARG;
    }

    *pAddress = PTR_TO_CORDB_ADDRESS(GetFramePointer().GetSPValue());
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Refer to the comment for code:CordbInternalFrame::IsCloserToLeaf
//

BOOL CordbInternalFrame::IsCloserToLeafWorker(ICorDebugFrame * pFrameToCompare)
{
    // Get the address of the "this" internal frame.
    CORDB_ADDRESS thisFrameAddr = PTR_TO_CORDB_ADDRESS(this->GetFramePointer().GetSPValue());

    // Note that a QI on ICorDebugJITILFrame for ICorDebugNativeFrame will work.
    RSExtSmartPtr<ICorDebugNativeFrame> pNativeFrame;
    pFrameToCompare->QueryInterface(IID_ICorDebugNativeFrame, (void **)&pNativeFrame);
    if (pNativeFrame != NULL)
    {
        // The frame to compare is a CordbNativeFrame.
        CordbNativeFrame * pCNativeFrame = static_cast<CordbNativeFrame *>(pNativeFrame.GetValue());

        // Compare the address of the "this" internal frame to the SP of the stack frame.
        // We can't compare frame pointers because the frame pointer means different things on
        // different platforms.
        CORDB_ADDRESS stackFrameSP = GetSPFromDebuggerREGDISPLAY(&(pCNativeFrame->m_rd));
        return (thisFrameAddr < stackFrameSP);
    }

    RSExtSmartPtr<ICorDebugRuntimeUnwindableFrame> pRUFrame;
    pFrameToCompare->QueryInterface(IID_ICorDebugRuntimeUnwindableFrame, (void **)&pRUFrame);
    if (pRUFrame != NULL)
    {
        // The frame to compare is a CordbRuntimeUnwindableFrame.
        CordbRuntimeUnwindableFrame * pCRUFrame =
            static_cast<CordbRuntimeUnwindableFrame *>(pRUFrame.GetValue());

        DT_CONTEXT * pResumeContext = const_cast<DT_CONTEXT *>(pCRUFrame->GetContext());
        CORDB_ADDRESS stackFrameSP = PTR_TO_CORDB_ADDRESS(CORDbgGetSP(pResumeContext));
        return (thisFrameAddr < stackFrameSP);
    }

    RSExtSmartPtr<ICorDebugInternalFrame> pInternalFrame;
    pFrameToCompare->QueryInterface(IID_ICorDebugInternalFrame, (void **)&pInternalFrame);
    if (pInternalFrame != NULL)
    {
        // The frame to compare is a CordbInternalFrame.
        CordbInternalFrame * pCInternalFrame =
            static_cast<CordbInternalFrame *>(pInternalFrame.GetValue());

        CORDB_ADDRESS frameAddr = PTR_TO_CORDB_ADDRESS(pCInternalFrame->GetFramePointer().GetSPValue());
        return (thisFrameAddr < frameAddr);
    }

    // What does this mean?  This is unexpected.
    _ASSERTE(!"CIF::ICTLW - Unexpected frame type.\n");
    ThrowHR(E_FAIL);
}

//---------------------------------------------------------------------------------------
//
// Checks whether the "this" internal frame is closer to the leaf than the specified ICDFrame.
// If the specified ICDFrame represents a stack frame, then we compare the address of the "this"
// internal frame against the SP of the stack frame.
//
// Arguments:
//    pFrameToCompare - the ICDFrame to compare against
//    pIsCloser       - out parameter; returns TRUE if the "this" internal frame is closer to the leaf
//
// Return Value:
//    S_OK on success.
//    E_INVALIDARG if pFrameToCompare or pIsCloser is NULL.
//    E_FAIL if pFrameToCompare is bogus.
//
// Notes:
//    This function doesn't deal with the backing store at all.
//

HRESULT CordbInternalFrame::IsCloserToLeaf(ICorDebugFrame * pFrameToCompare,
                                           BOOL *           pIsCloser)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this);
    {
        ValidateOrThrow(pFrameToCompare);
        ValidateOrThrow(pIsCloser);

        *pIsCloser = IsCloserToLeafWorker(pFrameToCompare);
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}


CordbRuntimeUnwindableFrame::CordbRuntimeUnwindableFrame(CordbThread *    pThread,
                                                         FramePointer     fp,
                                                         CordbAppDomain * pCurrentAppDomain,
                                                         DT_CONTEXT *     pContext)
  : CordbFrame(pThread, fp, 0, pCurrentAppDomain),
    m_context(*pContext)
{
}

void CordbRuntimeUnwindableFrame::Neuter()
{
    CordbFrame::Neuter();
}

HRESULT CordbRuntimeUnwindableFrame::QueryInterface(REFIID id, void ** ppInterface)
{
    if (id == IID_ICorDebugFrame)
    {
        *ppInterface = static_cast<ICorDebugFrame *>(static_cast<ICorDebugRuntimeUnwindableFrame *>(this));
    }
    else if (id == IID_ICorDebugRuntimeUnwindableFrame)
    {
        *ppInterface = static_cast<ICorDebugRuntimeUnwindableFrame *>(this);
    }
    else if (id == IID_IUnknown)
    {
        *ppInterface = static_cast<IUnknown *>(static_cast<ICorDebugRuntimeUnwindableFrame *>(this));
    }
    else
    {
        *ppInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Returns the CONTEXT corresponding to this CordbRuntimeUnwindableFrame.
//
// Return Value:
//    Return a pointer to the CONTEXT.
//

const DT_CONTEXT * CordbRuntimeUnwindableFrame::GetContext() const
{
    return &m_context;
}


// default constructor to make the compiler happy
CordbMiscFrame::CordbMiscFrame()
{
#ifdef FEATURE_EH_FUNCLETS
    this->parentIP       = 0;
    this->fpParentOrSelf = LEAF_MOST_FRAME;
    this->fIsFilterFunclet = false;
#endif // FEATURE_EH_FUNCLETS
}

// the real constructor which stores the funclet-related information in the CordbMiscFrame
CordbMiscFrame::CordbMiscFrame(DebuggerIPCE_JITFuncData * pJITFuncData)
{
#ifdef FEATURE_EH_FUNCLETS
    this->parentIP       = pJITFuncData->parentNativeOffset;
    this->fpParentOrSelf = pJITFuncData->fpParentOrSelf;
    this->fIsFilterFunclet = (pJITFuncData->fIsFilterFrame == TRUE);
#endif // FEATURE_EH_FUNCLETS
}

/* ------------------------------------------------------------------------- *
 * Native Frame class
 * ------------------------------------------------------------------------- */


CordbNativeFrame::CordbNativeFrame(CordbThread *        pThread,
                                   FramePointer         fp,
                                   CordbNativeCode *    pNativeCode,
                                   SIZE_T               ip,
                                   DebuggerREGDISPLAY * pDRD,
                                   TADDR                taAmbientESP,
                                   bool                 fQuicklyUnwound,
                                   CordbAppDomain *     pCurrentAppDomain,
                                   CordbMiscFrame *     pMisc /*= NULL*/,
                                   DT_CONTEXT *         pContext /*= NULL*/)
  : CordbFrame(pThread, fp, ip, pCurrentAppDomain),
    m_rd(*pDRD),
    m_quicklyUnwound(fQuicklyUnwound),
    m_JITILFrame(NULL),
    m_nativeCode(pNativeCode), // implicit InternalAddRef
    m_taAmbientESP(taAmbientESP)
{
    m_misc = *pMisc;

    // Only new CordbNativeFrames created by the new stackwalk contain a CONTEXT.
    _ASSERTE(pContext != NULL);
    m_context = *pContext;
}

/*
    A list of which resources owned by this object are accounted for.

    RESOLVED:
        CordbJITILFrame*   m_JITILFrame; // Neutered
*/

CordbNativeFrame::~CordbNativeFrame()
{
    _ASSERTE(IsNeutered());
}

// Neutered by CordbThread::CleanupStack
void CordbNativeFrame::Neuter()
{
    // Neuter may be called multiple times so be sure to set ptrs to NULL so that we don't
    // double release them.
    if (IsNeutered())
    {
        return;
    }

    m_nativeCode.Clear();

    if (m_JITILFrame != NULL)
    {
        m_JITILFrame->Neuter();
        m_JITILFrame.Clear();
    }

    CordbFrame::Neuter();
}

// CordbNativeFrame::QueryInterface
//
// Description
//  interface query for this COM object
//
//  NOTE: the COM object associated with this CordbNativeFrame may consist of
//  two C++ objects (the CordbNativeFrame and the CordbJITILFrame).
//
// Parameters
//      id              the GUID associated with the requested interface
//      pInterface      [out] the interface pointer
//
// Returns
//  HRESULT
//      S_OK            If this CordbJITILFrame supports the interface
//      E_NOINTERFACE   If this object does not support the interface
//
// Exceptions
//  None
//
//
HRESULT CordbNativeFrame::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugFrame)
    {
        *pInterface = static_cast<ICorDebugFrame*>(static_cast<ICorDebugNativeFrame*>(this));
    }
    else if (id == IID_ICorDebugNativeFrame)
    {
        *pInterface = static_cast<ICorDebugNativeFrame*>(this);
    }
    else if (id == IID_ICorDebugNativeFrame2)
    {
        *pInterface = static_cast<ICorDebugNativeFrame2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugNativeFrame*>(this));
    }
    else
    {
        // might be searching for an IL Frame. delegate that search to the
        // JITILFrame
        if (m_JITILFrame != NULL)
        {
            return m_JITILFrame->QueryInterfaceInternal(id, pInterface);
        }
        else
        {
            *pInterface = NULL;
            return E_NOINTERFACE;
        }
    }

    ExternalAddRef();
    return S_OK;
}

// Return the CordbNativeCode object associated with this native frame.
// This is just a wrapper around the real helper.
HRESULT CordbNativeFrame::GetCode(ICorDebugCode **ppCode)
{
    PUBLIC_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);
    FAIL_IF_NEUTERED(this);

    CordbNativeCode * pCode = GetNativeCode();
    *ppCode = static_cast<ICorDebugCode*> (pCode);
    pCode->ExternalAddRef();

    return S_OK;;
}

//---------------------------------------------------------------------------------------
//
// Returns the CONTEXT corresponding to this CordbNativeFrame.
//
// Return Value:
//    Return a pointer to the CONTEXT.
//

const DT_CONTEXT * CordbNativeFrame::GetContext() const
{
    return &m_context;
}

//---------------------------------------------------------------------------------------
//
// This is an internal helper to get the CordbNativeCode object associated with this native frame.
//
// Return Value:
//    the associated CordbNativeCode object
//

CordbNativeCode * CordbNativeFrame::GetNativeCode()
{
    return this->m_nativeCode;
}

//---------------------------------------------------------------------------------------
//
// This is an internal helper to get the CordbFunction object associated with this native frame.
//
// Return Value:
//    the associated CordbFunction object
//

CordbFunction *CordbNativeFrame::GetFunction()
{
    return this->m_nativeCode->GetFunction();
}



// Return the native offset.
HRESULT CordbNativeFrame::GetIP(ULONG32 *pnOffset)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnOffset, ULONG32 *);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *pnOffset = (ULONG32)m_ip;

    return S_OK;
}

ULONG32 CordbNativeFrame::GetIPOffset()
{
    return (ULONG32)m_ip;
}

TADDR CordbNativeFrame::GetReturnRegisterValue()
{
#if defined(TARGET_X86)
    return (TADDR)m_context.Eax;
#elif defined(TARGET_AMD64)
    return (TADDR)m_context.Rax;
#elif defined(TARGET_ARM)
    return (TADDR)m_context.R0;
#elif defined(TARGET_ARM64)
    return (TADDR)m_context.X0;
#else
    _ASSERTE(!"nyi for platform");
    return 0;
#endif
}

// Determine if we can set IP at this point.  The specified offset is the native offset.
HRESULT CordbNativeFrame::CanSetIP(ULONG32 nOffset)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (!IsLeafFrame())
        {
            ThrowHR(CORDBG_E_SET_IP_NOT_ALLOWED_ON_NONLEAF_FRAME);
        }

        hr = m_pThread->SetIP(SetIP_fCanSetIPOnly,
                              m_nativeCode,
                              nOffset,
                              SetIP_fNative );
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// Try to set the IP to the specified offset.  The specified offset is the native offset.
HRESULT CordbNativeFrame::SetIP(ULONG32 nOffset)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (!IsLeafFrame())
        {
            ThrowHR(CORDBG_E_SET_IP_NOT_ALLOWED_ON_NONLEAF_FRAME);
        }

        hr = m_pThread->SetIP(SetIP_fSetIP,
                              m_nativeCode,
                              nOffset,
                              SetIP_fNative );
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


// Given a (register,offset) description of a stack location, compute
// the real memory address for it.
// This will also handle ambient SP values (which are encoded with regNum == REGNUM_AMBIENT_SP).
CORDB_ADDRESS CordbNativeFrame::GetLSStackAddress(
    ICorDebugInfo::RegNum regNum,
    signed offset)
{
    UINT_PTR *pRegAddr;

    CORDB_ADDRESS pRemoteValue;

    if (regNum != DBG_TARGET_REGNUM_AMBIENT_SP)
    {
        // Even if we're inside a funclet, variables (in both x64 and ARM) are still
        // relative to the frame pointer or stack pointer, which are accurate in the
        // funclet, after the funclet prolog; the frame pointer is re-established in the
        // funclet prolog using the PSP. Thus, we just look up the frame pointer in the
        // current native frame.

        pRegAddr = this->GetAddressOfRegister(
                ConvertRegNumToCorDebugRegister(regNum));

        // This should never be null as long as regNum is a member of the RegNum enum.
        // If it is, an AV dereferencing a null-pointer in retail builds, or an assert in debug
        // builds is exactly the behavior we want.
        PREFIX_ASSUME(pRegAddr != NULL);

        pRemoteValue = PTR_TO_CORDB_ADDRESS(*pRegAddr + offset);
    }
    else
    {
        // Use the ambient ESP. At this point we're decoding an ambient-sp var, so
        // we should definitely have an ambient-sp. If this is null, then the jit
        // likely gave us an inconsistent data.
        TADDR taAmbient = this->GetAmbientESP();
        _ASSERTE(taAmbient != NULL);

        pRemoteValue = PTR_TO_CORDB_ADDRESS(taAmbient + offset);
    }

    return pRemoteValue;
}


// ----------------------------------------------------------------------------
// CordbNativeFrame::GetStackRange
//
// Description:
// Return the stack range owned by this native frame.
// The start of the stack range is the leafmost boundary, and the end is the rootmost boundary.
//
// Arguments:
//    * pStart - out parameter; return the leaf end of the frame
//    * pEnd   - out parameter; return the root end of the frame
//
// Return Value:
//    Return S_OK on success.
//
// Notes: see code:#GetStackRange

HRESULT CordbNativeFrame::GetStackRange(CORDB_ADDRESS *pStart,
                                        CORDB_ADDRESS *pEnd)
{
    PUBLIC_REENTRANT_API_ENTRY(this);

    // Callers explicit require GetStackRange() to be callable when neutered so that they
    // can line up ICorDebugFrame objects across continues. We only return stack ranges
    // here and don't access any special data.
    OK_IF_NEUTERED(this);

    if (GetProcess()->GetShim() != NULL)
    {
        if (pStart)
        {
            // From register set.
            *pStart = GetSPFromDebuggerREGDISPLAY(&m_rd);
        }

        if (pEnd)
        {
            // The rootmost boundary is the frame pointer.
            // <NOTE>
            // This is not true on AMD64, on which we use the stack pointer as the frame pointer.
            // </NOTE>
            *pEnd = PTR_TO_CORDB_ADDRESS(GetFramePointer().GetSPValue());
        }
        return S_OK;
    }
    else
    {
        if (pStart != NULL)
        {
            *pStart = NULL;
        }
        if (pEnd != NULL)
        {
            *pEnd = NULL;
        }
        return E_NOTIMPL;
    }
}

// Return the register set of the native frame.
HRESULT CordbNativeFrame::GetRegisterSet(ICorDebugRegisterSet **ppRegisters)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppRegisters, ICorDebugRegisterSet **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        // allocate a new CordbRegisterSet object
        RSInitHolder<CordbRegisterSet> pRegisterSet(new CordbRegisterSet(&m_rd,
                                                                         m_pThread,
                                                                         IsLeafFrame(),
                                                                         m_quicklyUnwound));

        pRegisterSet.TransferOwnershipExternal(ppRegisters);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Checks whether the frame is a child frame or not.
//
// Arguments:
//    pIsChild - out parameter; returns whether the frame is a child frame
//
// Return Value:
//    S_OK on success.
//    E_INVALIDARG if the out parmater is NULL.
//

HRESULT CordbNativeFrame::IsChild(BOOL * pIsChild)
{
    HRESULT hr = S_OK;
    PUBLIC_REENTRANT_API_BEGIN(this)
    {
        if (pIsChild == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }
        else
        {
            *pIsChild = ((this->IsFunclet() && !this->IsFilterFunclet()) ? TRUE : FALSE);
        }
    }
    PUBLIC_REENTRANT_API_END(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Given an ICDNativeFrame2, check whether it is the parent frame of the current frame.
//
// Arguments:
//    pPotentialParentFrame - the ICDNativeFrame2 to check
//    pIsParent             - out parameter; returns whether the specified frame is indeed the parent frame
//
// Return Value:
//    S_OK on success.
//    CORDBG_E_NOT_CHILD_FRAME if the current frame is not a child frame.
//    E_INVALIDARG if either of the incoming argument is NULL.
//    E_FAIL on other failures.
//

HRESULT CordbNativeFrame::IsMatchingParentFrame(ICorDebugNativeFrame2 * pPotentialParentFrame,
                                                BOOL *                  pIsParent)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pPotentialParentFrame, ICorDebugNativeFrame2 *);

    HRESULT hr = S_OK;
    EX_TRY
    {
        if ((pPotentialParentFrame == NULL) || (pIsParent == NULL))
        {
            ThrowHR(E_INVALIDARG);
        }

        *pIsParent = FALSE;

        if (!this->IsFunclet())
        {
            ThrowHR(CORDBG_E_NOT_CHILD_FRAME);
        }

#ifdef FEATURE_EH_FUNCLETS
        CordbNativeFrame * pFrameToCheck = static_cast<CordbNativeFrame *>(pPotentialParentFrame);
        if (pFrameToCheck->IsFunclet())
        {
            *pIsParent = FALSE;
        }
        else
        {
            FramePointer fpParent  = this->m_misc.fpParentOrSelf;
            FramePointer fpToCheck = pFrameToCheck->m_misc.fpParentOrSelf;

            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            *pIsParent = pDAC->IsMatchingParentFrame(fpToCheck, fpParent);
        }
#endif // FEATURE_EH_FUNCLETS
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Return the stack parameter size of the current frame.  Since this information is only used on x86,
// we return S_FALSE and a size of 0 on WIN64 platforms.
//
// Arguments:
//    pSize - out parameter; return the size of the stack parameter
//
// Return Value:
//    S_OK on success.
//    S_FALSE on WIN64 platforms.
//    E_INVALIDARG if pSize is NULL.
//
// Notes:
//    Always return S_FALSE on WIN64.
//

HRESULT CordbNativeFrame::GetStackParameterSize(ULONG32 * pSize)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    EX_TRY
    {
        if (pSize == NULL)
        {
            ThrowHR(E_INVALIDARG);
        }

#if defined(TARGET_X86)
        IDacDbiInterface * pDAC = GetProcess()->GetDAC();
        *pSize = pDAC->GetStackParameterSize(PTR_TO_CORDB_ADDRESS(CORDbgGetIP(&m_context)));
#else  // !TARGET_X86
        hr = S_FALSE;
        *pSize = 0;
#endif // TARGET_X86
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//
// GetAddressOfRegister returns the address of the given register in the
// frame's current register display (eg, a local address). This is usually used to build a
// ICorDebugValue from.
//
UINT_PTR * CordbNativeFrame::GetAddressOfRegister(CorDebugRegister regNum) const
{
    UINT_PTR* ret = NULL;

    switch (regNum)
    {
    case REGISTER_STACK_POINTER:
        ret = (UINT_PTR*)GetSPAddress(&m_rd);
        break;

#if !defined(TARGET_AMD64) && !defined(TARGET_ARM) // @ARMTODO
    case REGISTER_FRAME_POINTER:
        ret = (UINT_PTR*)GetFPAddress(&m_rd);
        break;
#endif

#if defined(TARGET_X86)
    case REGISTER_X86_EAX:
        ret = (UINT_PTR*)&m_rd.Eax;
        break;

    case REGISTER_X86_ECX:
        ret = (UINT_PTR*)&m_rd.Ecx;
        break;

    case REGISTER_X86_EDX:
        ret = (UINT_PTR*)&m_rd.Edx;
        break;

    case REGISTER_X86_EBX:
        ret = (UINT_PTR*)&m_rd.Ebx;
        break;

    case REGISTER_X86_ESI:
        ret = (UINT_PTR*)&m_rd.Esi;
        break;

    case REGISTER_X86_EDI:
        ret = (UINT_PTR*)&m_rd.Edi;
        break;

#elif defined(TARGET_AMD64)
    case REGISTER_AMD64_RBP:
        ret = (UINT_PTR*)&m_rd.Rbp;
        break;

    case REGISTER_AMD64_RAX:
        ret = (UINT_PTR*)&m_rd.Rax;
        break;

    case REGISTER_AMD64_RCX:
        ret = (UINT_PTR*)&m_rd.Rcx;
        break;

    case REGISTER_AMD64_RDX:
        ret = (UINT_PTR*)&m_rd.Rdx;
        break;

    case REGISTER_AMD64_RBX:
        ret = (UINT_PTR*)&m_rd.Rbx;
        break;

    case REGISTER_AMD64_RSI:
        ret = (UINT_PTR*)&m_rd.Rsi;
        break;

    case REGISTER_AMD64_RDI:
        ret = (UINT_PTR*)&m_rd.Rdi;
        break;

    case REGISTER_AMD64_R8:
        ret = (UINT_PTR*)&m_rd.R8;
        break;

    case REGISTER_AMD64_R9:
        ret = (UINT_PTR*)&m_rd.R9;
        break;

    case REGISTER_AMD64_R10:
        ret = (UINT_PTR*)&m_rd.R10;
        break;

    case REGISTER_AMD64_R11:
        ret = (UINT_PTR*)&m_rd.R11;
        break;

    case REGISTER_AMD64_R12:
        ret = (UINT_PTR*)&m_rd.R12;
        break;

    case REGISTER_AMD64_R13:
        ret = (UINT_PTR*)&m_rd.R13;
        break;

    case REGISTER_AMD64_R14:
        ret = (UINT_PTR*)&m_rd.R14;
        break;

    case REGISTER_AMD64_R15:
        ret = (UINT_PTR*)&m_rd.R15;
        break;
#elif defined(TARGET_ARM)
    case REGISTER_ARM_R0:
        ret = (UINT_PTR*)&m_rd.R0;
        break;

    case REGISTER_ARM_R1:
        ret = (UINT_PTR*)&m_rd.R1;
        break;

    case REGISTER_ARM_R2:
        ret = (UINT_PTR*)&m_rd.R2;
        break;

    case REGISTER_ARM_R3:
        ret = (UINT_PTR*)&m_rd.R3;
        break;

    case REGISTER_ARM_R4:
        ret = (UINT_PTR*)&m_rd.R4;
        break;

    case REGISTER_ARM_R5:
        ret = (UINT_PTR*)&m_rd.R5;
        break;

    case REGISTER_ARM_R6:
        ret = (UINT_PTR*)&m_rd.R6;
        break;

    case REGISTER_ARM_R7:
        ret = (UINT_PTR*)&m_rd.R7;
        break;

    case REGISTER_ARM_R8:
        ret = (UINT_PTR*)&m_rd.R8;
        break;

    case REGISTER_ARM_R9:
        ret = (UINT_PTR*)&m_rd.R9;
        break;

    case REGISTER_ARM_R10:
        ret = (UINT_PTR*)&m_rd.R10;
        break;

    case REGISTER_ARM_R11:
        ret = (UINT_PTR*)&m_rd.R11;
        break;

    case REGISTER_ARM_R12:
        ret = (UINT_PTR*)&m_rd.R12;
        break;

    case REGISTER_ARM_LR:
        ret = (UINT_PTR*)&m_rd.LR;
        break;

    case REGISTER_ARM_PC:
        ret = (UINT_PTR*)&m_rd.PC;
        break;
#elif defined(TARGET_ARM64)
    case REGISTER_ARM64_X0:
    case REGISTER_ARM64_X1:
    case REGISTER_ARM64_X2:
    case REGISTER_ARM64_X3:
    case REGISTER_ARM64_X4:
    case REGISTER_ARM64_X5:
    case REGISTER_ARM64_X6:
    case REGISTER_ARM64_X7:
    case REGISTER_ARM64_X8:
    case REGISTER_ARM64_X9:
    case REGISTER_ARM64_X10:
    case REGISTER_ARM64_X11:
    case REGISTER_ARM64_X12:
    case REGISTER_ARM64_X13:
    case REGISTER_ARM64_X14:
    case REGISTER_ARM64_X15:
    case REGISTER_ARM64_X16:
    case REGISTER_ARM64_X17:
    case REGISTER_ARM64_X18:
    case REGISTER_ARM64_X19:
    case REGISTER_ARM64_X20:
    case REGISTER_ARM64_X21:
    case REGISTER_ARM64_X22:
    case REGISTER_ARM64_X23:
    case REGISTER_ARM64_X24:
    case REGISTER_ARM64_X25:
    case REGISTER_ARM64_X26:
    case REGISTER_ARM64_X27:
    case REGISTER_ARM64_X28:
        ret = (UINT_PTR*)&m_rd.X[regNum - REGISTER_ARM64_X0];
        break;

    case REGISTER_ARM64_LR:
        ret = (UINT_PTR*)&m_rd.LR;
        break;

    case REGISTER_ARM64_PC:
        ret = (UINT_PTR*)&m_rd.PC;
        break;
#elif defined(TARGET_RISCV64)
    case REGISTER_RISCV64_PC:
        ret = (UINT_PTR*)&m_rd.PC;
        break;

    case REGISTER_RISCV64_RA:
        ret = (UINT_PTR*)&m_rd.RA;
        break;

    case REGISTER_RISCV64_GP:
        ret = (UINT_PTR*)&m_rd.GP;
        break;

    case REGISTER_RISCV64_TP:
        ret = (UINT_PTR*)&m_rd.TP;
        break;

    case REGISTER_RISCV64_T0:
        ret = (UINT_PTR*)&m_rd.T0;
        break;

    case REGISTER_RISCV64_T1:
        ret = (UINT_PTR*)&m_rd.T1;
        break;

    case REGISTER_RISCV64_T2:
        ret = (UINT_PTR*)&m_rd.T2;
        break;

    case REGISTER_RISCV64_S1:
        ret = (UINT_PTR*)&m_rd.S1;
        break;

    case REGISTER_RISCV64_A0:
        ret = (UINT_PTR*)&m_rd.A0;
        break;

    case REGISTER_RISCV64_A1:
        ret = (UINT_PTR*)&m_rd.A1;
        break;

    case REGISTER_RISCV64_A2:
        ret = (UINT_PTR*)&m_rd.A2;
        break;

    case REGISTER_RISCV64_A3:
        ret = (UINT_PTR*)&m_rd.A3;
        break;

    case REGISTER_RISCV64_A4:
        ret = (UINT_PTR*)&m_rd.A4;
        break;

    case REGISTER_RISCV64_A5:
        ret = (UINT_PTR*)&m_rd.A5;
        break;

    case REGISTER_RISCV64_A6:
        ret = (UINT_PTR*)&m_rd.A6;
        break;

    case REGISTER_RISCV64_A7:
        ret = (UINT_PTR*)&m_rd.A7;
        break;

    case REGISTER_RISCV64_S2:
        ret = (UINT_PTR*)&m_rd.S2;
        break;

    case REGISTER_RISCV64_S3:
        ret = (UINT_PTR*)&m_rd.S3;
        break;

    case REGISTER_RISCV64_S4:
        ret = (UINT_PTR*)&m_rd.S4;
        break;

    case REGISTER_RISCV64_S5:
        ret = (UINT_PTR*)&m_rd.S5;
        break;

    case REGISTER_RISCV64_S6:
        ret = (UINT_PTR*)&m_rd.S6;
        break;

    case REGISTER_RISCV64_S7:
        ret = (UINT_PTR*)&m_rd.S7;
        break;

    case REGISTER_RISCV64_S8:
        ret = (UINT_PTR*)&m_rd.S8;
        break;

    case REGISTER_RISCV64_S9:
        ret = (UINT_PTR*)&m_rd.S9;
        break;

    case REGISTER_RISCV64_S10:
        ret = (UINT_PTR*)&m_rd.S10;
        break;

    case REGISTER_RISCV64_S11:
        ret = (UINT_PTR*)&m_rd.S11;
        break;

    case REGISTER_RISCV64_T3:
        ret = (UINT_PTR*)&m_rd.T3;
        break;

    case REGISTER_RISCV64_T4:
        ret = (UINT_PTR*)&m_rd.T4;
        break;

    case REGISTER_RISCV64_T5:
        ret = (UINT_PTR*)&m_rd.T5;
        break;

    case REGISTER_RISCV64_T6:
        ret = (UINT_PTR*)&m_rd.T6;
        break;
#endif

    default:
        _ASSERT(!"Invalid register number!");
    }

    return ret;
}

//
// GetLeftSideAddressOfRegister returns the Left Side address of the given register in the frames current register
// display.
//
CORDB_ADDRESS CordbNativeFrame::GetLeftSideAddressOfRegister(CorDebugRegister regNum) const
{
#if !defined(USE_REMOTE_REGISTER_ADDRESS)
    // Use marker values as the register address.  This is to implement the funceval breaking change.
    //
    if (IsLeafFrame())
    {
        return kLeafFrameRegAddr;
    }
    else
    {
        return kNonLeafFrameRegAddr;
    }

#else  // USE_REMOTE_REGISTER_ADDRESS
    void* ret = 0;

    switch (regNum)
    {

#if !defined(TARGET_AMD64)
    case REGISTER_FRAME_POINTER:
        ret = m_rd.pFP;
        break;
#endif

#if defined(TARGET_X86)
    case REGISTER_X86_EAX:
        ret = m_rd.pEax;
        break;

    case REGISTER_X86_ECX:
        ret = m_rd.pEcx;
        break;

    case REGISTER_X86_EDX:
        ret = m_rd.pEdx;
        break;

    case REGISTER_X86_EBX:
        ret = m_rd.pEbx;
        break;

    case REGISTER_X86_ESI:
        ret = m_rd.pEsi;
        break;

    case REGISTER_X86_EDI:
        ret = m_rd.pEdi;
        break;

#elif defined(TARGET_AMD64)
    case REGISTER_AMD64_RBP:
        ret = m_rd.pRbp;
        break;

    case REGISTER_AMD64_RAX:
        ret = m_rd.pRax;
        break;

    case REGISTER_AMD64_RCX:
        ret = m_rd.pRcx;
        break;

    case REGISTER_AMD64_RDX:
        ret = m_rd.pRdx;
        break;

    case REGISTER_AMD64_RBX:
        ret = m_rd.pRbx;
        break;

    case REGISTER_AMD64_RSI:
        ret = m_rd.pRsi;
        break;

    case REGISTER_AMD64_RDI:
        ret = m_rd.pRdi;
        break;

    case REGISTER_AMD64_R8:
        ret = m_rd.pR8;
        break;

    case REGISTER_AMD64_R9:
        ret = m_rd.pR9;
        break;

    case REGISTER_AMD64_R10:
        ret = m_rd.pR10;
        break;

    case REGISTER_AMD64_R11:
        ret = m_rd.pR11;
        break;

    case REGISTER_AMD64_R12:
        ret = m_rd.pR12;
        break;

    case REGISTER_AMD64_R13:
        ret = m_rd.pR13;
        break;

    case REGISTER_AMD64_R14:
        ret = m_rd.pR14;
        break;

    case REGISTER_AMD64_R15:
        ret = m_rd.pR15;
        break;
#endif
    default:
        _ASSERT(!"Invalid register number!");
    }

    return PTR_TO_CORDB_ADDRESS(ret);
#endif // !USE_REMOTE_REGISTER_ADDRESS
}


//---------------------------------------------------------------------------------------
//
// Given the native variable information of a variable, return its value.
//
// Arguments:
//     pNativeVarInfo - the variable information of the variable to be retrieved
//
// Returns:
//     Return the specified value.
//     Throw on error.
//
// Assumption:
//    This function assumes that the value is either in a register or on the stack
//    (i.e. VLT_REG or VLT_STK).
//
// Notes:
//    Eventually we should make this more general-purpose.
//

SIZE_T CordbNativeFrame::GetRegisterOrStackValue(const ICorDebugInfo::NativeVarInfo * pNativeVarInfo)
{
    SIZE_T uResult;

    if (pNativeVarInfo->loc.vlType == ICorDebugInfo::VLT_REG)
    {
        CorDebugRegister reg = ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlReg.vlrReg);
        uResult = *(reinterpret_cast<SIZE_T *>(GetAddressOfRegister(reg)));
    }
    else if (pNativeVarInfo->loc.vlType == ICorDebugInfo::VLT_STK)
    {
        CORDB_ADDRESS remoteAddr = GetLSStackAddress(pNativeVarInfo->loc.vlStk.vlsBaseReg,
                                                     pNativeVarInfo->loc.vlStk.vlsOffset);

        HRESULT hr = GetProcess()->SafeReadStruct(remoteAddr, &uResult);
        IfFailThrow(hr);
    }
    else
    {
        ThrowHR(E_FAIL);
    }

    return uResult;
}


//---------------------------------------------------------------------------------------
//
// Looks in a register and retrieves the value as a specific type, returning it
// as an ICorDebugValue.
//
// Arguments:
//     reg - The register to use.
//     cbSigBlob - The number of bytes in the signature given.
//     pvSigBlob - A signature stream that describes the type of the value in the register.
//     ppValue - OUT: Space to store the resulting ICorDebugValue
//
// Returns:
//     S_OK on success, else an error code.
//
HRESULT CordbNativeFrame::GetLocalRegisterValue(CorDebugRegister reg,
                                                ULONG cbSigBlob,
                                                PCCOR_SIGNATURE pvSigBlob,
                                                ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pvSigBlob, BYTE, cbSigBlob, true, false);

    CordbType * pType;

    SigParser sigParser(pvSigBlob, cbSigBlob);

    Instantiation emptyInst;

    HRESULT hr = CordbType::SigToType(m_JITILFrame->GetModule(), &sigParser, &emptyInst, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalRegisterValue(reg, pType, ppValue);
}

//---------------------------------------------------------------------------------------
//
// Looks in two registers and retrieves the value as a specific type, returning it
// as an ICorDebugValue.
//
// Arguments:
//     highWordReg - The register to use for the high word.
//     lowWordReg - The register to use for the low word.
//     cbSigBlob - The number of bytes in the signature given.
//     pvSigBlob - A signature stream that describes the type of the value in the register.
//     ppValue - OUT: Space to store the resulting ICorDebugValue
//
// Returns:
//     S_OK on success, else an error code.
//
HRESULT CordbNativeFrame::GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                                      CorDebugRegister lowWordReg,
                                                      ULONG cbSigBlob,
                                                      PCCOR_SIGNATURE pvSigBlob,
                                                      ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (cbSigBlob == 0)
    {
        return E_INVALIDARG;
    }

    CordbType * pType;

    SigParser sigParser(pvSigBlob, cbSigBlob);

    Instantiation emptyInst;

    HRESULT hr = CordbType::SigToType(m_JITILFrame->GetModule(), &sigParser, &emptyInst, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalDoubleRegisterValue(highWordReg, lowWordReg, pType, ppValue);
}


//---------------------------------------------------------------------------------------
//
// Uses an address and retrieves the value as a specific type, returning it
// as an ICorDebugValue.
//
// Arguments:
//     address - A local memory address.
//     cbSigBlob - The number of bytes in the signature given.
//     pvSigBlob - A signature stream that describes the type of the value in the register.
//     ppValue - OUT: Space to store the resulting ICorDebugValue
//
// Returns:
//     S_OK on success, else an error code.
//
HRESULT CordbNativeFrame::GetLocalMemoryValue(CORDB_ADDRESS address,
                                              ULONG cbSigBlob,
                                              PCCOR_SIGNATURE pvSigBlob,
                                              ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pvSigBlob, BYTE, cbSigBlob, true, false);

    CordbType * pType;

    SigParser sigParser(pvSigBlob, cbSigBlob);

    Instantiation emptyInst;

    HRESULT hr = CordbType::SigToType(m_JITILFrame->GetModule(), &sigParser, &emptyInst, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalMemoryValue(address, pType, ppValue);
}


//---------------------------------------------------------------------------------------
//
// Uses a register and an address, retrieving the value as a specific type, returning it
// as an ICorDebugValue.
//
// Arguments:
//     highWordReg - Register to use as the high word.
//     lowWordAddress - A local memory address containing the low word.
//     cbSigBlob - The number of bytes in the signature given.
//     pvSigBlob - A signature stream that describes the type of the value in the register.
//     ppValue - OUT: Space to store the resulting ICorDebugValue
//
// Returns:
//     S_OK on success, else an error code.
//
HRESULT CordbNativeFrame::GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                                      CORDB_ADDRESS lowWordAddress,
                                                      ULONG cbSigBlob,
                                                      PCCOR_SIGNATURE pvSigBlob,
                                                      ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (cbSigBlob == 0)
    {
        return E_INVALIDARG;
    }

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pvSigBlob, BYTE, cbSigBlob, true, true);

    CordbType * pType;

    SigParser sigParser(pvSigBlob, cbSigBlob);

    Instantiation emptyInst;

    HRESULT hr = CordbType::SigToType(m_JITILFrame->GetModule(), &sigParser, &emptyInst, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalRegisterMemoryValue(highWordReg, lowWordAddress, pType, ppValue);
}


//---------------------------------------------------------------------------------------
//
// Uses a register and an address, retrieving the value as a specific type, returning it
// as an ICorDebugValue.
//
// Arguments:
//     highWordReg - A local memory address to use as the high word.
//     lowWordAddress - Register containing the low word.
//     cbSigBlob - The number of bytes in the signature given.
//     pvSigBlob - A signature stream that describes the type of the value in the register.
//     ppValue - OUT: Space to store the resulting ICorDebugValue
//
// Returns:
//     S_OK on success, else an error code.
//
HRESULT CordbNativeFrame::GetLocalMemoryRegisterValue(CORDB_ADDRESS highWordAddress,
                                           CorDebugRegister lowWordRegister,
                                           ULONG cbSigBlob,
                                           PCCOR_SIGNATURE pvSigBlob,
                                           ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (cbSigBlob == 0)
    {
        return E_INVALIDARG;
    }

    VALIDATE_POINTER_TO_OBJECT_ARRAY(pvSigBlob, BYTE, cbSigBlob, true, true);

    CordbType * pType;

    SigParser sigParser(pvSigBlob, cbSigBlob);

    Instantiation emptyInst;

    HRESULT hr = CordbType::SigToType(m_JITILFrame->GetModule(), &sigParser, &emptyInst, &pType);

    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalMemoryRegisterValue(highWordAddress, lowWordRegister, pType, ppValue);
}



HRESULT CordbNativeFrame::GetLocalRegisterValue(CorDebugRegister reg,
                                                CordbType * pType,
                                                ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

#if defined(TARGET_X86) || defined(TARGET_64BIT)
#if defined(TARGET_X86)
    if ((reg >= REGISTER_X86_FPSTACK_0) && (reg <= REGISTER_X86_FPSTACK_7))
#elif defined(TARGET_AMD64)
    if ((reg >= REGISTER_AMD64_XMM0) && (reg <= REGISTER_AMD64_XMM15))
#elif defined(TARGET_ARM64)
    if ((reg >= REGISTER_ARM64_V0) && (reg <= REGISTER_ARM64_V31))
#endif
    {
        return GetLocalFloatingPointValue(reg, pType, ppValue);
    }
#endif

    // The address of the given register is the address of the value
    // in this process. We have no remote address here.
    void *pLocalValue = (void*)GetAddressOfRegister(reg);
    HRESULT hr = S_OK;

    EX_TRY
    {
        // Provide the register info as we create the value. CreateValueByType will transfer ownership of this to
        // the new instance of CordbValue.
        EnregisteredValueHomeHolder pRemoteReg(new RegValueHome(this, reg));
        EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

        ICorDebugValue *pValue;
        CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                      pType,
                                      false,
                                      EMPTY_BUFFER,
                                      MemoryRange(pLocalValue, REG_SIZE),
                                      pRegHolder,
                                      &pValue);  // throws

        *ppValue = pValue;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbNativeFrame::GetLocalDoubleRegisterValue(
                                            CorDebugRegister highWordReg,
                                            CorDebugRegister lowWordReg,
                                            CordbType * pType,
                                            ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Provide the register info as we create the value. CreateValueByType will transfer ownership of this to
        // the new instance of CordbValue.
        EnregisteredValueHomeHolder pRemoteReg(new RegRegValueHome(this, highWordReg, lowWordReg));
        EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

        CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                      pType,
                                      false,
                                      EMPTY_BUFFER,
                                      MemoryRange(NULL, 0),
                                      pRegHolder,
                                      ppValue);  // throws
    }
    EX_CATCH_HRESULT(hr);

#ifdef _DEBUG
    {
        // sanity check object size
        if (SUCCEEDED(hr))
        {
            ULONG32 objectSize;
            hr = (*ppValue)->GetSize(&objectSize);
            _ASSERTE(SUCCEEDED(hr));
            //
            // nickbe
            // 10/31/2002 11:09:42
            //
            // This assert assumes that the JIT will only partially enregister
            // objects that have a size equal to twice the size of a register.
            //
            _ASSERTE(objectSize == 2 * sizeof(void*));
        }
    }
#endif
    return hr;
}

HRESULT
CordbNativeFrame::GetLocalMemoryValue(CORDB_ADDRESS address,
                                      CordbType *   pType,
                                      ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    _ASSERTE(m_nativeCode->GetFunction() != NULL);
    HRESULT hr = S_OK;

    ICorDebugValue *pValue = NULL;
    EX_TRY
    {
        CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                      pType,
                                      false,
                                      TargetBuffer(address, CordbValue::GetSizeForType(pType, kUnboxed)),
                                      MemoryRange(NULL, 0),
                                      NULL,
                                      &pValue);  // throws
    }
    EX_CATCH_HRESULT(hr);

    if (SUCCEEDED(hr))
        *ppValue = pValue;

    return hr;
}

HRESULT
CordbNativeFrame::GetLocalByRefMemoryValue(CORDB_ADDRESS address,
                                           CordbType * pType,
                                           ICorDebugValue **ppValue)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    LPVOID actualAddress = NULL;
    HRESULT hr = GetProcess()->SafeReadStruct(address, &actualAddress);
    if (FAILED(hr))
    {
        return hr;
    }

    return GetLocalMemoryValue(PTR_TO_CORDB_ADDRESS(actualAddress), pType, ppValue);
}

HRESULT
CordbNativeFrame::GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                              CORDB_ADDRESS lowWordAddress,
                                              CordbType * pType,
                                              ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Provide the register info as we create the value. CreateValueByType will transfer ownership of this to
        // the new instance of CordbValue.
        EnregisteredValueHomeHolder pRemoteReg(new RegMemValueHome(this,
                                                                   highWordReg,
                                                                   lowWordAddress));
        EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

        CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                      pType,
                                      false,
                                      EMPTY_BUFFER,
                                      MemoryRange(NULL, 0),
                                      pRegHolder,
                                      ppValue);  // throws
    }
    EX_CATCH_HRESULT(hr);

#ifdef _DEBUG
    {
        if (SUCCEEDED(hr))
        {
            ULONG32 objectSize;
            hr = (*ppValue)->GetSize(&objectSize);
            _ASSERTE(SUCCEEDED(hr));
            // See the comment in CordbNativeFrame::GetLocalDoubleRegisterValue
            // for more information on this assertion
            _ASSERTE(objectSize == 2 * sizeof(void*));
        }
    }
#endif
    return hr;
}

HRESULT
CordbNativeFrame::GetLocalMemoryRegisterValue(CORDB_ADDRESS highWordAddress,
                                              CorDebugRegister lowWordRegister,
                                              CordbType * pType,
                                              ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Provide the register info as we create the value. CreateValueByType will transfer ownership of this to
        // the new instance of CordbValue.
        EnregisteredValueHomeHolder pRemoteReg(new MemRegValueHome(this,
                                                                   lowWordRegister,
                                                                   highWordAddress));
        EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

        CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                      pType,
                                      false,
                                      EMPTY_BUFFER,
                                      MemoryRange(NULL, 0),
                                      pRegHolder,
                                      ppValue);  // throws
    }
    EX_CATCH_HRESULT(hr);

#ifdef _DEBUG
    {
        if (SUCCEEDED(hr))
        {
            ULONG32 objectSize;
            hr = (*ppValue)->GetSize(&objectSize);
            _ASSERTE(SUCCEEDED(hr));
            // See the comment in CordbNativeFrame::GetLocalDoubleRegisterValue
            // for more information on this assertion
            _ASSERTE(objectSize == 2 * sizeof(void*));
        }
    }
#endif
    return hr;
}

HRESULT CordbNativeFrame::GetLocalFloatingPointValue(DWORD index,
                                                     CordbType * pType,
                                                     ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    HRESULT hr = S_OK;

    CorElementType et = pType->m_elementType;

    if ((et != ELEMENT_TYPE_R4) &&
        (et != ELEMENT_TYPE_R8))
        return E_INVALIDARG;

#if defined(TARGET_AMD64)
    if (!((index >= REGISTER_AMD64_XMM0) &&
          (index <= REGISTER_AMD64_XMM15)))
        return E_INVALIDARG;
    index -= REGISTER_AMD64_XMM0;
#elif defined(TARGET_ARM64)
    if (!((index >= REGISTER_ARM64_V0) &&
        (index <= REGISTER_ARM64_V31)))
        return E_INVALIDARG;
    index -= REGISTER_ARM64_V0;
#elif defined(TARGET_ARM)
    if (!((index >= REGISTER_ARM_D0) &&
        (index <= REGISTER_ARM_D31)))
        return E_INVALIDARG;
    index -= REGISTER_ARM_D0;
#else
    if (!((index >= REGISTER_X86_FPSTACK_0) &&
          (index <= REGISTER_X86_FPSTACK_7)))
        return E_INVALIDARG;
    index -= REGISTER_X86_FPSTACK_0;
#endif

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    // Make sure the thread's floating point stack state is loaded
    // over from the left side.
    //
    CordbThread *pThread = m_pThread;

    EX_TRY
    {
        if (!pThread->m_fFloatStateValid)
        {
            pThread->LoadFloatState();
        }
    }
    EX_CATCH_HRESULT(hr);
    if (SUCCEEDED(hr))
    {
#if !defined(TARGET_64BIT)
        // This is needed on x86 because we are dealing with a stack.
        index = pThread->m_floatStackTop - index;
#endif

        if (index >= (sizeof(pThread->m_floatValues) /
                      sizeof(pThread->m_floatValues[0])))
            return E_INVALIDARG;

#ifdef TARGET_X86
        // A workaround (sort of) to get around the difference in format between
        // a float value and a double value.  We can't simply cast a double pointer to
        // a float pointer.  Instead, we have to cast the double itself to a float.
        if (pType->m_elementType == ELEMENT_TYPE_R4)
            *(float *)&(pThread->m_floatValues[index]) = (float)pThread->m_floatValues[index];
#endif

        ICorDebugValue* pValue;

        EX_TRY
        {
            // Provide the register info as we create the value. CreateValueByType will transfer ownership of this to
            // the new instance of CordbValue.
            EnregisteredValueHomeHolder pRemoteReg(new FloatRegValueHome(this, index));
            EnregisteredValueHomeHolder * pRegHolder = pRemoteReg.GetAddr();

            CordbValue::CreateValueByType(GetCurrentAppDomain(),
                                          pType,
                                          false,
                                          EMPTY_BUFFER,
                                          MemoryRange(&(pThread->m_floatValues[index]), sizeof(double)),
                                          pRegHolder,
                                          &pValue);  // throws

            *ppValue = pValue;
        }
        EX_CATCH_HRESULT(hr);

    }

    return hr;
}

//---------------------------------------------------------------------------------------
//
// Quick accessor to tell if we're the leaf frame.
//
// Return Value:
//    whether we are the leaf frame or not
//

bool CordbNativeFrame::IsLeafFrame() const
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // Should only be called by non-neutered stuff.
    // Also, since we're not neutered, we know we have a Thread object, and we know it's state is current.
    _ASSERTE(!this->IsNeutered());

    // If the thread's state is sleeping, then there's no frame below us, but we're actually
    // not the leaf frame.
    // @todo- consider having Sleep / Wait / Join be an ICDInternalFrame.
    _ASSERTE(m_pThread != NULL); // not neutered, so should have a thread
    if (m_pThread->IsThreadWaitingOrSleeping())
    {
        return false;
    }

    if (!m_optfIsLeafFrame.HasValue())
    {
        if (GetProcess()->GetShim() != NULL)
        {
            // In V2, the definition of "leaf frame" is the leaf frame in the leaf chain in the stackwalk.
            PRIVATE_SHIM_CALLBACK_IN_THIS_SCOPE0(GetProcess());
            ShimStackWalk * pSW = GetProcess()->GetShim()->LookupOrCreateShimStackWalk(m_pThread);

            // check if there is any chain
            if (pSW->GetChainCount() > 0)
            {
                // check if the leaf chain has any frame
                if (pSW->GetChain(0)->GetLastFrameIndex() > 0)
                {
                    CordbFrame * pCFrame = GetCordbFrameFromInterface(pSW->GetFrame(0));
                    CordbNativeFrame * pNFrame = pCFrame->GetAsNativeFrame();
                    if (pNFrame != NULL)
                    {
                        // check if the leaf frame in the leaf chain is "this"
                        if (CompareControlRegisters(GetContext(), pNFrame->GetContext()))
                        {
                            m_optfIsLeafFrame = TRUE;
                        }
                    }
                }
            }

            if (!m_optfIsLeafFrame.HasValue())
            {
                m_optfIsLeafFrame = FALSE;
            }
        }
        else
        {
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            m_optfIsLeafFrame = (pDAC->IsLeafFrame(m_pThread->m_vmThreadToken, &m_context) == TRUE);
        }
    }
    return m_optfIsLeafFrame.GetValue();
}

//---------------------------------------------------------------------------------------
//
// Get the offset used to determine if a variable is live in a particular method frame.
//
// Return Value:
//    the offset used for inspection purposes
//
// Notes:
//    On WIN64, variables used in funclets are always homed on the stack.  Morever, the variable lifetime
//    information only covers the parent method.  The idea is that the variables which are live in a funclet
//    will be the variables which are live in the parent method at the offset at which the exception occurs.
//    Thus, to determine if a variable is live in a funclet frame, we need to use the offset of the parent
//    method frame at which the exception occurs.
//

SIZE_T CordbNativeFrame::GetInspectionIP()
{
#ifdef FEATURE_EH_FUNCLETS
    // On 64-bit, if this is a funclet, then return the offset of the parent method frame at which
    // the exception occurs.  Otherwise just return the normal offset.
    return (IsFunclet() ? GetParentIP() : m_ip);
#else
    // Always return the normal offset on all other platforms.
    return m_ip;
#endif // FEATURE_EH_FUNCLETS
}

//---------------------------------------------------------------------------------------
//
// Return whether this is a funclet method frame.
//
// Return Value:
//    whether this is a funclet method frame.
//

bool CordbNativeFrame::IsFunclet()
{
#ifdef FEATURE_EH_FUNCLETS
    return (m_misc.parentIP != NULL);
#else
    return false;
#endif // FEATURE_EH_FUNCLETS
}

//---------------------------------------------------------------------------------------
//
// Return whether this is a filter funclet method frame.
//
// Return Value:
//    whether this is a filter funclet method frame.
//

bool CordbNativeFrame::IsFilterFunclet()
{
#ifdef FEATURE_EH_FUNCLETS
    return (IsFunclet() && m_misc.fIsFilterFunclet);
#else
    return false;
#endif // FEATURE_EH_FUNCLETS
}


#ifdef FEATURE_EH_FUNCLETS
//---------------------------------------------------------------------------------------
//
// Return the offset of the parent method frame at which the exception occurs.
//
// Return Value:
//    the offset of the parent method frame at which the exception occurs
//

SIZE_T CordbNativeFrame::GetParentIP()
{
    return m_misc.parentIP;
}
#endif // FEATURE_EH_FUNCLETS

// Accessor for the shim private hook code:CordbThread::ConvertFrameForILMethodWithoutMetadata.
// Refer to that function for comments on the return value, the argument, etc.
BOOL CordbNativeFrame::ConvertNativeFrameForILMethodWithoutMetadata(
    ICorDebugInternalFrame2 ** ppInternalFrame2)
{
    _ASSERTE(ppInternalFrame2 != NULL);
    *ppInternalFrame2 = NULL;

    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
    IDacDbiInterface::DynamicMethodType type =
        pDAC->IsILStubOrLCGMethod(GetNativeCode()->GetVMNativeCodeMethodDescToken());

    // Here are the conversion rules:
    // 1)  For a normal managed method, we don't convert, and we return FALSE.
    // 2)  For an IL stub, we convert to NULL, and we return TRUE.
    // 3)  For a dynamic method, we convert to a STUBFRAME_LIGHTWEIGHT_FUNCTION, and we return TRUE.
    if (type == IDacDbiInterface::kNone)
    {
        return FALSE;
    }
    else if (type == IDacDbiInterface::kILStub)
    {
        return TRUE;
    }
    else if (type == IDacDbiInterface::kLCGMethod)
    {
        RSInitHolder<CordbInternalFrame> pInternalFrame(
            new CordbInternalFrame(m_pThread,
                                   m_fp,
                                   m_currentAppDomain,
                                   STUBFRAME_LIGHTWEIGHT_FUNCTION,
                                   GetNativeCode()->GetMetadataToken(),
                                   GetNativeCode()->GetFunction(),
                                   GetNativeCode()->GetVMNativeCodeMethodDescToken()));

        pInternalFrame.TransferOwnershipExternal(ppInternalFrame2);
        return TRUE;
    }

    UNREACHABLE();
}

/* ------------------------------------------------------------------------- *
 * JIT-IL Frame class
 * ------------------------------------------------------------------------- */

CordbJITILFrame::CordbJITILFrame(CordbNativeFrame *    pNativeFrame,
                                 CordbILCode *         pCode,
                                 UINT_PTR              ip,
                                 CorDebugMappingResult mapping,
                                 GENERICS_TYPE_TOKEN   exactGenericArgsToken,
                                 DWORD                 dwExactGenericArgsTokenIndex,
                                 bool                  fVarArgFnx,
                                 CordbReJitILCode *    pRejitCode,
                                 bool                  fAdjustedIP)
  : CordbBase(pNativeFrame->GetProcess(), 0, enumCordbJITILFrame),
    m_nativeFrame(pNativeFrame),
    m_ilCode(pCode),
    m_ip(ip),
    m_mapping(mapping),
    m_fVarArgFnx(fVarArgFnx),
    m_allArgsCount(0),
    m_rgbSigParserBuf(NULL),
    m_FirstArgAddr(NULL),
    m_rgNVI(NULL),
    m_genericArgs(),
    m_genericArgsLoaded(false),
    m_frameParamsToken(exactGenericArgsToken),
    m_dwFrameParamsTokenIndex(dwExactGenericArgsTokenIndex),
    m_pReJitCode(pRejitCode),
    m_adjustedIP(fAdjustedIP)
{
    // We'll initialize the SigParser in CordbJITILFrame::Init().
    m_sigParserCached = SigParser(NULL, 0);
    _ASSERTE(m_sigParserCached.IsNull());

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_nativeFrame->m_pThread->GetRefreshStackNeuterList()->Add(GetProcess(), this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

//---------------------------------------------------------------------------------------
//
// Initialize a CordbJITILFrame object.  Must be called after allocating the object and before using it.
// If Init fails, then destroy the object and release the memory.
//
// Return Value:
//    HRESULT for the operation
//
// Notes:
//    This is a nop if the function is not a vararg function.
//

HRESULT CordbJITILFrame::Init()
{
    // ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    HRESULT hr = S_OK;


    EX_TRY
    {
        _ASSERTE(m_ilCode != NULL);

        if (m_fVarArgFnx)
        {
            // First, we need to find the VASigCookie.  Use the native var info to do so.
            const ICorDebugInfo::NativeVarInfo * pNativeVarInfo = NULL;
            CordbNativeFrame * pNativeFrame = this->m_nativeFrame;

            pNativeFrame->m_nativeCode->LoadNativeInfo();
            hr = pNativeFrame->m_nativeCode->ILVariableToNative((DWORD)ICorDebugInfo::VARARGS_HND_ILNUM,
                                                             pNativeFrame->GetInspectionIP(),
                                                             &pNativeVarInfo);
            IfFailThrow(hr);

            // Check for the case where the VASigCookie isn't pushed on the stack yet.
            // This should only be a problem with optimized code.
            if (pNativeVarInfo->loc.vlType != ICorDebugInfo::VLT_STK)
            {
                ThrowHR(E_FAIL);
            }

            // Retrieve the target address.
            CORDB_ADDRESS pRemoteValue = pNativeFrame->GetLSStackAddress(
                                            pNativeVarInfo->loc.vlStk.vlsBaseReg,
                                            pNativeVarInfo->loc.vlStk.vlsOffset);

            CORDB_ADDRESS argBase;
            // Now is the time to ask DacDbi to retrieve the information based on the VASigCookie.
            IDacDbiInterface * pDAC = GetProcess()->GetDAC();
            TargetBuffer sigTargetBuf = pDAC->GetVarArgSig(pRemoteValue, &argBase);

            // make sure we are not leaking any memory
            _ASSERTE(m_rgbSigParserBuf == NULL);

            m_rgbSigParserBuf = new BYTE[sigTargetBuf.cbSize];
            GetProcess()->SafeReadBuffer(sigTargetBuf, m_rgbSigParserBuf);
            m_sigParserCached = SigParser(m_rgbSigParserBuf, sigTargetBuf.cbSize);

            // Note that we should never mutate the SigParser.
            // Instead, make a copy and work with the copy instead.
            if (!m_sigParserCached.IsNull())
            {
                SigParser sigParser = m_sigParserCached;

                // get the actual count of arguments, including the var args
                IfFailThrow(sigParser.SkipMethodHeaderSignature(&m_allArgsCount));

                BOOL methodIsStatic;

                m_ilCode->GetSig(NULL, NULL, &methodIsStatic); // throws

                if (!methodIsStatic)
                {
                    m_allArgsCount++;       // skip the "this" object
                }

                // initialize the variable lifetime information
                m_rgNVI = new ICorDebugInfo::NativeVarInfo[m_allArgsCount]; // throws

                _ASSERTE(ICorDebugInfo::VLT_COUNT <= ICorDebugInfo::VLT_INVALID);

                for (ULONG i = 0; i < m_allArgsCount; i++)
                {
                    m_rgNVI[i].loc.vlType = ICorDebugInfo::VLT_INVALID;
                }
            }

            // GetVarArgSig gets the address of the beginning of the arguments pushed for this frame.
            // We'll need the address of the first argument, which will depend on its size and the
            // calling convention, so we'll commpute that now that we have the SigParser.
            CordbType * pArgType;
            IfFailThrow(GetArgumentType(0, &pArgType));
            ULONG32 argSize = 0;
            IfFailThrow(pArgType->GetUnboxedObjectSize(&argSize));
#if defined(TARGET_X86) // (STACK_GROWS_DOWN_ON_ARGS_WALK)
            m_FirstArgAddr = argBase - argSize;
#else  // !TARGET_X86 (STACK_GROWS_UP_ON_ARGS_WALK)
            AlignAddressForType(pArgType, argBase);
            m_FirstArgAddr = argBase;
#endif // !TARGET_X86 (STACK_GROWS_UP_ON_ARGS_WALK)
        }

        // The stackwalking code can't always successfully retrieve the generics type token.
        // For example, on 64-bit, the JIT only encodes the generics type token location if
        // a method has catch clause for a generic exception (e.g. "catch(MyException<string> e)").
        if ((m_dwFrameParamsTokenIndex != (DWORD)ICorDebugInfo::MAX_ILNUM) && (m_frameParamsToken == NULL))
        {
            // All variables are unavailable in the prolog and the epilog.
            // This includes the generics type token.  Failing to get the token just means that
            // we won't have full generics information.  This should not be a disastrous failure.
            //
            // Currently, on X64, the JIT is reporting that the variables are live even in the epilog.
            // That's why we need this check here.  I need to follow up on this.
            if ((m_mapping != MAPPING_PROLOG) && (m_mapping != MAPPING_EPILOG))
            {
                // Find the generics type token using the variable lifetime information.
                const ICorDebugInfo::NativeVarInfo * pNativeVarInfo = NULL;
                CordbNativeFrame * pNativeFrame = this->m_nativeFrame;

                pNativeFrame->m_nativeCode->LoadNativeInfo();
                HRESULT hrTmp = pNativeFrame->m_nativeCode->ILVariableToNative(m_dwFrameParamsTokenIndex,
                                                                               pNativeFrame->GetInspectionIP(),
                                                                               &pNativeVarInfo);

                // It's not a disaster if we can't find the generics token, so don't throw an exception here.
                // In fact, it's fairly common in retail code.  Even if we can't find the generics token,
                // we may still be able to look up the generics type information later by using the MethodDesc,
                // the "this" object, etc.  If not, we'll at least get the representative type information
                // (e.g. Foo<T> instead of Foo<string>).
                if (SUCCEEDED(hrTmp))
                {
                    _ASSERTE(pNativeVarInfo != NULL);

                    // The generics type token should be stored either in a register or on the stack.
                    SIZE_T uRawToken = pNativeFrame->GetRegisterOrStackValue(pNativeVarInfo);

                    // Ask DAC to resolve the token for us.  We really don't want to deal with all the logic here.
                    IDacDbiInterface * pDAC = GetProcess()->GetDAC();
                    // On a minidump, we'll throw if we're missing the memory.
                    ALLOW_DATATARGET_MISSING_MEMORY(
                        m_frameParamsToken = pDAC->ResolveExactGenericArgsToken(m_dwFrameParamsTokenIndex, uRawToken);
                    );
                }
            }
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

/*
    A list of which resources owned by this object are accounted for.

    UNKNOWN:
        CordbNativeFrame* m_nativeFrame;
        CordbILCode *                m_ilCode;
        CorDebugMappingResult m_mapping;
        CORDB_ADDRESS     m_FirstArgAddr;
        ICorDebugInfo::NativeVarInfo * m_rgNVI; // Deleted in neuter
        CordbClass **m_genericArgs;
*/

CordbJITILFrame::~CordbJITILFrame()
{
    _ASSERTE(IsNeutered());
}

// Neutered by CordbNativeFrame
void CordbJITILFrame::Neuter()
{
    // Since neutering here calls Release directly, we don't want to double-release
    // if neuter is called multiple times.
    if (IsNeutered())
    {
        return;
    }

    // Frames include pointers across to other types that specify the
    // representation instantiation - reduce the reference counts on these....
    for (unsigned int i = 0; i < m_genericArgs.m_cInst; i++)
    {
        m_genericArgs.m_ppInst[i]->Release();
    }

    if (m_rgNVI != NULL)
    {
        delete [] m_rgNVI;
        m_rgNVI = NULL;
    }

    if (m_rgbSigParserBuf != NULL)
    {
        delete [] m_rgbSigParserBuf;
        m_rgbSigParserBuf = NULL;
    }

    m_pReJitCode.Clear();

    // If this class ever inherits from the CordbFrame we'll need a call
    // to CordbFrame::Neuter() here instead of to CordbBase::Neuter();
    CordbBase::Neuter();
}

//---------------------------------------------------------------------------------------
//
// Load the generic type and method arguments and store them into the frame if possible.
//
// Return Value:
//    HRESULT for the operation
//

void CordbJITILFrame::LoadGenericArgs()
{
    THROW_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    // The case where there are no type parameters, or the case where we've
    // already feched the realInst, is easy.
    if (m_genericArgsLoaded)
    {
        return;
    }

    _ASSERTE(m_nativeFrame->m_nativeCode != NULL);

    if (!m_nativeFrame->m_nativeCode->IsInstantiatedGeneric())
    {
        m_genericArgs = Instantiation(0, NULL,0);
        m_genericArgsLoaded = true;
        return;
    }

    // Find the exact generic arguments for a frame that is executing
    // a generic method.  The left-side will fetch these from arguments
    // given on the stack and/or from the IP.


    IDacDbiInterface * pDAC = GetProcess()->GetDAC();

    UINT32 cGenericClassTypeParams = 0;
    DacDbiArrayList<DebuggerIPCE_ExpandedTypeData> rgGenericTypeParams;

    pDAC->GetMethodDescParams(GetCurrentAppDomain()->GetADToken(),
                              m_nativeFrame->GetNativeCode()->GetVMNativeCodeMethodDescToken(),
                              m_frameParamsToken,
                              &cGenericClassTypeParams,
                              &rgGenericTypeParams);

    UINT32 cTotalGenericTypeParams = rgGenericTypeParams.Count();

    // @dbgtodo  reliability - This holder doesn't actually work in this case because it just deletes
    // each element on error.  The RS classes are all expected to be neutered before the destructor is called.
    NewArrayHolder<CordbType *> ppGenericArgs(new CordbType *[cTotalGenericTypeParams]);

    for (UINT32 i = 0; i < cTotalGenericTypeParams;i++)
    {
        // creates a CordbType object for the generic argument
        HRESULT hr = CordbType::TypeDataToType(GetCurrentAppDomain(),
                                               &(rgGenericTypeParams[i]),
                                               &ppGenericArgs[i]);
        IfFailThrow(hr);

        // We add a ref as the instantiation will be stored away in the
        // ref-counted data structure associated with the JITILFrame
        ppGenericArgs[i]->AddRef();
    }

    // initialize the generics information
    m_genericArgs = Instantiation(cTotalGenericTypeParams, ppGenericArgs, cGenericClassTypeParams);
    m_genericArgsLoaded = true;

    ppGenericArgs.SuppressRelease();
}


//
// CordbJITILFrame::QueryInterface
//
// Description
//  Interface query for this COM object
//
//  NOTE: the COM object associated with this CordbJITILFrame may consist of two
//  C++ objects (a CordbJITILFrame and its associated CordbNativeFrame)
//
// Parameters
//      id              the GUID associated with the requested interface
//      pInterface      [out] the interface pointer
//
// Returns
//  HRESULT
//      S_OK            If this CordbJITILFrame supports the interface
//      E_NOINTERFACE   If this object does not support the interface
//
// Exceptions
//  None
//
HRESULT CordbJITILFrame::QueryInterface(REFIID id, void **pInterface)
{
    if (NULL != m_nativeFrame)
    {
        // If the native frame does not support the requested interface, then
        // the native fram is responsible for delegating the query back to this
        // object through QueryInterfaceInternal(...)
        return m_nativeFrame->QueryInterface(id, pInterface);
    }

    // no native frame. Check for interfaces common to CordbNativeFrame and
    // CordbJITILFrame
    if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugILFrame*>(this));
    }
    else if (id == IID_ICorDebugFrame)
    {
        *pInterface = static_cast<ICorDebugFrame*>(this);
    }
    else
    {
        // didn't find an interface yet. Since there's no native frame
        // associated with this IL frame, go ahead and check for the IL frame
        return this->QueryInterfaceInternal(id, pInterface);
    }

    ExternalAddRef();
    return S_OK;
}

//
// CordbJITILFrame::QueryInterfaceInternal
//
// Description
//  Interface query for interfaces implemented ONLY by CordbJITILFrame (as
//  opposed to interfaces implemented by both CordbNativeFrame and
//  CordbJITILFrame)
//
// Parameters
//      id              the GUID associated with the requested interface
//      pInterface      [out] the interface pointer
//  NOTE:   id must not be IUnknown or ICorDebugFrame
//  NOTE:   if this object is in "forward compatibility mode", passing in
//          IID_ICorDebugILFrame2 for the id will result in a failure (returns
//          E_NOINTERFACE)
//
// Returns
//  HRESULT
//      S_OK            If this CordbJITILFrame supports the interface
//      E_NOINTERFACE   If this object does not support the interface
//
// Exceptions
//  None
//
HRESULT
CordbJITILFrame::QueryInterfaceInternal(REFIID id, void** pInterface)
{
    _ASSERTE(IID_ICorDebugFrame != id);
    _ASSERTE(IID_IUnknown != id);

    // don't query for IUnknown or ICorDebugFrame! Someone else should have
    // already taken care of that.
    if (id == IID_ICorDebugILFrame)
    {
        *pInterface = static_cast<ICorDebugILFrame*>(this);
    }
    else if (id == IID_ICorDebugILFrame2)
    {
        *pInterface = static_cast<ICorDebugILFrame2*>(this);
    }
    else if (id == IID_ICorDebugILFrame3)
    {
        *pInterface = static_cast<ICorDebugILFrame3*>(this);
    }
    else if (id == IID_ICorDebugILFrame4)
    {
        *pInterface = static_cast<ICorDebugILFrame4*>(this);
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//---------------------------------------------------------------------------------------
//
// Get an enumerator for the generic type and method arguments on this frame.
//
// Arguments:
//    ppTypeParameterEnum   - out parameter; return the enumerator
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbJITILFrame::EnumerateTypeParameters(ICorDebugTypeEnum **ppTypeParameterEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppTypeParameterEnum, ICorDebugTypeEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    (*ppTypeParameterEnum) = NULL;

    HRESULT hr = S_OK;
    EX_TRY
    {

        // load the generic arguments, which may be cached
        LoadGenericArgs();

        // create the enumerator
        RSInitHolder<CordbTypeEnum> pEnum(
            CordbTypeEnum::Build(GetCurrentAppDomain(), m_nativeFrame->m_pThread->GetRefreshStackNeuterList(), m_genericArgs.m_cInst, m_genericArgs.m_ppInst));
        if ( pEnum == NULL )
        {
            ThrowOutOfMemory();
        }

        pEnum.TransferOwnershipExternal(ppTypeParameterEnum);
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}


// ----------------------------------------------------------------------------
// CordbJITILFrame::GetChain
//
// Description:
//    Return the owning chain.  Since chains have been  deprecated in Arrowhead,
//    this function returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppChain - out parameter; return the owning chain
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppChain is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbJITILFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbJITILFrame::GetChain(ICorDebugChain **ppChain)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppChain, ICorDebugChain **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = m_nativeFrame->GetChain(ppChain);
        // Since we are returning anyway, let's not throw even if the call fails.
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Return the IL code blob associated with this IL frame.
// Each IL frame corresponds to exactly one IL code blob.
HRESULT CordbJITILFrame::GetCode(ICorDebugCode **ppCode)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppCode, ICorDebugCode **);

    *ppCode = static_cast<ICorDebugCode*> (m_ilCode);
    m_ilCode->ExternalAddRef();

    return S_OK;;
}

// Return the function associated with this IL frame.
// Each IL frame corresponds to exactly one function.
HRESULT CordbJITILFrame::GetFunction(ICorDebugFunction **ppFunction)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
        ValidateOrThrow(ppFunction);

        CordbFunction * pFunc = m_nativeFrame->GetFunction();
        *ppFunction = static_cast<ICorDebugFunction *>(pFunc);
        pFunc->ExternalAddRef();
    }
    PUBLIC_API_END(hr);
    return hr;
}

// Return the token of the function associated with this IL frame.
// Each IL frame corresponds to exactly one function.
HRESULT CordbJITILFrame::GetFunctionToken(mdMethodDef *pToken)
{
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pToken, mdMethodDef *);

    *pToken = m_nativeFrame->m_nativeCode->GetMetadataToken();

    return S_OK;
}

// ----------------------------------------------------------------------------
// CordJITILFrame::GetStackRange
//
// Description:
// Get the stack range owned by the associated native frame.
// IL frames and native frames are 1:1 for normal jitted managed methods.
// Dynamic methods are an exception.
//
// Arguments:
//    * pStart - out parameter; return the leaf end of the frame
//    * pEnd   - out parameter; return the root end of the frame
//
// Return Value:
//    Return S_OK on success.
//
// Notes: see code:#GetStackRange

HRESULT CordbJITILFrame::GetStackRange(CORDB_ADDRESS *pStart, CORDB_ADDRESS *pEnd)
{
    PUBLIC_REENTRANT_API_ENTRY(this);

    // The access of m_nativeFrame is not safe here. It's a weak reference.
    OK_IF_NEUTERED(this);

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = m_nativeFrame->GetStackRange(pStart, pEnd);
        // Since we are returning anyway, let's not throw even if the call fails.
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbJITILFrame::GetCaller
//
// Description:
// Delegate to the associated native frame to return the caller, which is closer to the root.
//    This function has been deprecated in Arrowhead, and so it returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppFrame - out parameter; return the caller frame
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbJITILFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbJITILFrame::GetCaller(ICorDebugFrame **ppFrame)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFrame, ICorDebugFrame **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = m_nativeFrame->GetCaller(ppFrame);
        // Since we are returning anyway, let's not throw even if the call fails.
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// ----------------------------------------------------------------------------
// CordbJITILFrame::GetCallee
//
// Description:
// Delegate to the associated native frame to return the callee, which is closer to the leaf.
//    This function has been deprecated in Arrowhead, and so it returns E_NOTIMPL unless there is a shim.
//
// Arguments:
//    * ppFrame - out parameter; return the callee frame
//
// Return Value:
//    Return S_OK on success.
//    Return E_INVALIDARG if ppFrame is NULL.
//    Return CORDBG_E_OBJECT_NEUTERED if the CordbJITILFrame is neutered.
//    Return E_NOTIMPL if there is no shim.
//

HRESULT CordbJITILFrame::GetCallee(ICorDebugFrame **ppFrame)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppFrame, ICorDebugFrame **);

    HRESULT hr = S_OK;
    EX_TRY
    {
        hr = m_nativeFrame->GetCallee(ppFrame);
        // Since we are returning anyway, let's not throw even if the call fails.
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

// Create a stepper on the frame.
HRESULT CordbJITILFrame::CreateStepper(ICorDebugStepper **ppStepper)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // by default, a stepper operates on the IL level, using IL offsets
    return m_nativeFrame->CreateStepper(ppStepper);
}

// Return the IL offset and the mapping result.
HRESULT CordbJITILFrame::GetIP(ULONG32 *pnOffset,
                               CorDebugMappingResult *pMappingResult)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pnOffset, ULONG32 *);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pMappingResult, CorDebugMappingResult *);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *pnOffset = (ULONG32)m_ip;
    if (pMappingResult)
        *pMappingResult = m_mapping;

    return S_OK;
}

// Determine if we can set IP at this point.  The specified offset is the IL offset.
HRESULT CordbJITILFrame::CanSetIP(ULONG32 nOffset)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Check to see that this is a leaf frame
        if (!m_nativeFrame->IsLeafFrame())
        {
            ThrowHR(CORDBG_E_SET_IP_NOT_ALLOWED_ON_NONLEAF_FRAME);
        }

        // delegate to the associated native frame
        CordbNativeCode * pNativeCode = m_nativeFrame->m_nativeCode;
        hr = m_nativeFrame->m_pThread->SetIP(SetIP_fCanSetIPOnly,    // specify that this is for checking only
                                             pNativeCode,
                                             nOffset,
                                             SetIP_fIL );
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

// Try to set the IP to the specified offset.  The specified offset is the IL offset.
HRESULT CordbJITILFrame::SetIP(ULONG32 nOffset)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Check to see that this is a leaf frame
        if (!m_nativeFrame->IsLeafFrame())
        {
            ThrowHR(CORDBG_E_SET_IP_NOT_ALLOWED_ON_NONLEAF_FRAME);
        }

        // delegate to the native frame
        CordbNativeCode * pNativeCode = m_nativeFrame->m_nativeCode;
        hr = m_nativeFrame->m_pThread->SetIP(SetIP_fSetIP,    // specify that this is a real SetIP operation
                                             pNativeCode,
                                             nOffset,
                                             SetIP_fIL );
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

//---------------------------------------------------------------------------------------
//
// This routine creates backing native info for a local variable, returning an ICorDebugInfo
// object for the local variable when successful.
//
// Arguments:
//    dwIndex - Index of the local variable to create native info for.
//    ppNativeInfo - OUT: Space for storing the resulting pointer to native variable info.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbJITILFrame::FabricateNativeInfo(DWORD dwIndex,
                                             const ICorDebugInfo::NativeVarInfo ** ppNativeInfo)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        THROW_IF_NEUTERED(this);
        INTERNAL_SYNC_API_ENTRY(this->GetProcess());
        _ASSERTE(m_fVarArgFnx);

        // This array should have been populated in CordbJITILFrame::Init().
        _ASSERTE(m_rgNVI != NULL);

        // check if we have already fabricated all the information
        if (m_rgNVI[dwIndex].loc.vlType != ICorDebugInfo::VLT_INVALID)
        {
            (*ppNativeInfo) = &m_rgNVI[dwIndex];
        }
        else
        {
            // We'll initialize everything at once
            ULONG cbArchitectureMin;

            // m_FirstArgAddr will already be aligned on platforms that require alignment
            CORDB_ADDRESS rpCur = m_FirstArgAddr;

#if defined(TARGET_X86) || defined(TARGET_ARM)
            cbArchitectureMin = 4;
#elif defined(TARGET_64BIT)
            cbArchitectureMin = 8;
#else
            cbArchitectureMin = 8; //REVISIT_TODO not sure if this is correct
            PORTABILITY_ASSERT("What is the architecture-dependent minimum word size?");
#endif // TARGET_X86

            // make a copy of the cached SigParser
            SigParser sigParser = m_sigParserCached;

            IfFailThrow(sigParser.SkipMethodHeaderSignature(NULL));

            ULONG32 cbType;

            CordbType * pArgType;

            // make sure all the generic type and method arguments are loaded
            LoadGenericArgs();

            // get a CordbType object for the generic argument
            IfFailThrow(CordbType::SigToType(GetModule(), &sigParser, &(this->m_genericArgs), &pArgType));

            IfFailThrow(pArgType->GetUnboxedObjectSize(&cbType));

#if defined(TARGET_X86) // STACK_GROWS_DOWN_ON_ARGS_WALK
            // The rpCur pointer starts off in the right spot for the
            // first argument, but thereafter we have to decrement it
            // before getting the variable's location from it.  So increment
            // it here to be consistent later.
            rpCur += max(cbType, cbArchitectureMin);
#endif

            // Grab the IL code's function's method signature so we can see if it's static.
            BOOL fMethodIsStatic;

            m_ilCode->GetSig(NULL, NULL, &fMethodIsStatic);  // throws

            ULONG i;

            if (fMethodIsStatic)
            {
                i = 0;
            }
            else
            {
                i = 1;
            }

            for ( ; i < m_allArgsCount; i++)
            {
                m_rgNVI[i].startOffset = 0;
                m_rgNVI[i].endOffset = 0xFFffFFff;
                m_rgNVI[i].varNumber = i;
                m_rgNVI[i].loc.vlType = ICorDebugInfo::VLT_FIXED_VA;

                LoadGenericArgs();

                IfFailThrow(CordbType::SigToType(GetModule(), &sigParser, &(this->m_genericArgs), &pArgType));

                IfFailThrow(pArgType->GetUnboxedObjectSize(&cbType));

#if defined(TARGET_X86) // STACK_GROWS_DOWN_ON_ARGS_WALK
                rpCur -= max(cbType, cbArchitectureMin);
                m_rgNVI[i].loc.vlFixedVarArg.vlfvOffset =
                    (unsigned)(m_FirstArgAddr - rpCur);

                // Since the JIT adds in the size of this field, we do too to
                // be consistent.
                m_rgNVI[i].loc.vlFixedVarArg.vlfvOffset += sizeof(((CORINFO_VarArgInfo*)0)->argBytes);
#else // STACK_GROWS_UP_ON_ARGS_WALK
                m_rgNVI[i].loc.vlFixedVarArg.vlfvOffset =
                    (unsigned)(rpCur - m_FirstArgAddr);
                rpCur += max(cbType, cbArchitectureMin);
                AlignAddressForType(pArgType, rpCur);
#endif

                IfFailThrow(sigParser.SkipExactlyOne());
            } // for ( ; i M m_allArgsCount; i++)

            (*ppNativeInfo) = &m_rgNVI[dwIndex];
        } // else (m_rgNVI[dwIndex].loc.vlType == ICorDebugInfo::VLT_INVALID)
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbJITILFrame::ILVariableToNative(DWORD dwVarNumber,
                                            const ICorDebugInfo::NativeVarInfo **ppNativeInfo)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    _ASSERTE(m_nativeFrame->m_nativeCode->IsNativeCodeValid());
    // We keep the fixed argument native var infos in the
    // CordbFunction, which only is an issue for var args info:
    if (!m_fVarArgFnx || //not  a var args function
        (dwVarNumber < m_nativeFrame->m_nativeCode->GetFixedArgCount()) || // var args,fixed arg
           // note that this include the implicit 'this' for nonstatic fnxs
        (dwVarNumber >= m_allArgsCount) ||// var args, local variable
        (m_sigParserCached.IsNull())) //we don't have any VA info
    {
        // If we're in a var args fnx, but we're actually looking
        // for a local variable, then we want to use the variable
        // index as the function sees it - fixed (but not var)
        // args are added to local var number to get native info
        // We are really trying to find a variable by it's number,
        // but "special" variables have a negative number which we
        // don't use. We "number" them conceptually between the
        // arguments and locals:
        //
        //                  arguments        special         locals
        //                  -----------------------------------------
        // Actual numbers:  1 2 3             . . .          4 5 6 7
        // Logical numbers: 0 1 2             3 4            5 6 7 8
        //
        // We have two different counts for the number of arguments: the fixedArgCount
        // gives the actual number of arguments and the allArgsCount is the number of
        // of fixed arguments plus the number of var args.
        //
        // Thus, to get the correct actual number for locals we have to compute it as
        // logicalNumber - allArgsCount + fixedArgCount

        if (m_fVarArgFnx && (dwVarNumber >= m_allArgsCount) && !m_sigParserCached.IsNull())
        {
            dwVarNumber -= m_allArgsCount;
            dwVarNumber += m_nativeFrame->m_nativeCode->GetFixedArgCount();
        }

        return m_nativeFrame->m_nativeCode->ILVariableToNative(dwVarNumber,
                                             m_nativeFrame->GetInspectionIP(),
                                             ppNativeInfo);
    }

    return FabricateNativeInfo(dwVarNumber,ppNativeInfo);
}

//---------------------------------------------------------------------------------------
//
// This routine get the type of a particular argument.
//
// Arguments:
//    dwIndex - Index of the argument.
//    ppResultType - OUT: Space for storing the type of the argument.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbJITILFrame::GetArgumentType(DWORD dwIndex,
                                         CordbType ** ppResultType)
{
    HRESULT hr = S_OK;
    THROW_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess());

    LoadGenericArgs();

    if (m_fVarArgFnx && !m_sigParserCached.IsNull())
    {
        SigParser sigParser = m_sigParserCached;

        IfFailThrow(sigParser.SkipMethodHeaderSignature(NULL));

        // Grab the IL code's function's method signature so we can see if it's static.
        BOOL fMethodIsStatic;

        m_ilCode->GetSig(NULL, NULL, &fMethodIsStatic); // throws
        if (!fMethodIsStatic)
        {
            if (dwIndex == 0)
            {
                // Return the signature for the 'this' pointer for the
                // class this method is in.

                    IfFailThrow(m_ilCode->GetClass()->GetThisType(&(this->m_genericArgs), ppResultType));
                    return hr;
            }
            else
            {
                dwIndex--;
            }
        }
        for (ULONG i = 0; i < dwIndex; i++)
        {
            IfFailThrow(sigParser.SkipExactlyOne());
        }

        IfFailThrow(sigParser.SkipFunkyAndCustomModifiers());

        IfFailThrow(sigParser.SkipAnyVASentinel());

        IfFailThrow(CordbType::SigToType(GetModule(), &sigParser, &(this->m_genericArgs), ppResultType));
    }
    else // (!m_fVarArgFnx || m_sigParserCached.IsNull())
    {
        m_nativeFrame->m_nativeCode->GetArgumentType(dwIndex, &(this->m_genericArgs), ppResultType);
    }

    return hr;
}

//
// GetNativeVariable uses the JIT variable information to delegate to
// the native frame when the value is really created.
//
HRESULT CordbJITILFrame::GetNativeVariable(CordbType *type,
                                           const ICorDebugInfo::NativeVarInfo *pNativeVarInfo,
                                           ICorDebugValue **ppValue)
{
    INTERNAL_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    HRESULT hr = S_OK;

#ifdef FEATURE_EH_FUNCLETS
    if (m_nativeFrame->IsFunclet())
    {
        if ( (pNativeVarInfo->loc.vlType != ICorDebugInfo::VLT_STK) &&
             (pNativeVarInfo->loc.vlType != ICorDebugInfo::VLT_STK2) &&
             (pNativeVarInfo->loc.vlType != ICorDebugInfo::VLT_STK_BYREF) )
        {
            _ASSERTE(!"CordbJITILFrame::GetNativeVariable()"
                      " - Variables used in funclets should always be homed on the stack.\n");
            return E_FAIL;
        }
    }
#endif // FEATURE_EH_FUNCLETS

    switch (pNativeVarInfo->loc.vlType)
    {
    case ICorDebugInfo::VLT_REG:
        hr = m_nativeFrame->GetLocalRegisterValue(
                                 ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlReg.vlrReg),
                                 type, ppValue);
        break;

    case ICorDebugInfo::VLT_REG_BYREF:
        {
            CORDB_ADDRESS pRemoteByRefAddr = PTR_TO_CORDB_ADDRESS(
                *( m_nativeFrame->GetAddressOfRegister(ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlReg.vlrReg))) );

            hr = m_nativeFrame->GetLocalMemoryValue(pRemoteByRefAddr,
                                                    type,
                                                    ppValue);
        }
        break;

#if defined(TARGET_64BIT) || defined(TARGET_ARM)
    case ICorDebugInfo::VLT_REG_FP:
#if defined(TARGET_ARM) // @ARMTODO
        hr = E_NOTIMPL;
#elif defined(TARGET_AMD64)
        hr = m_nativeFrame->GetLocalFloatingPointValue(pNativeVarInfo->loc.vlReg.vlrReg + REGISTER_AMD64_XMM0,
                                                       type, ppValue);
#elif defined(TARGET_ARM64)
        hr = m_nativeFrame->GetLocalFloatingPointValue(pNativeVarInfo->loc.vlReg.vlrReg + REGISTER_ARM64_V0,
                                                       type, ppValue);
#elif defined(TARGET_LOONGARCH64)
        hr = m_nativeFrame->GetLocalFloatingPointValue(pNativeVarInfo->loc.vlReg.vlrReg + REGISTER_LOONGARCH64_F0,
                                                       type, ppValue);
#elif defined(TARGET_RISCV64)
        hr = m_nativeFrame->GetLocalFloatingPointValue(pNativeVarInfo->loc.vlReg.vlrReg + REGISTER_RISCV64_F0,
                                                       type, ppValue);
#else
#error Platform not implemented
#endif  // TARGET_ARM @ARMTODO
        break;
#endif // TARGET_64BIT || TARGET_ARM

    case ICorDebugInfo::VLT_STK_BYREF:
        {
            CORDB_ADDRESS pRemoteByRefAddr = m_nativeFrame->GetLSStackAddress(
                pNativeVarInfo->loc.vlStk.vlsBaseReg, pNativeVarInfo->loc.vlStk.vlsOffset) ;

            hr = m_nativeFrame->GetLocalByRefMemoryValue(pRemoteByRefAddr,
                                                         type,
                                                         ppValue);
        }
        break;

    case ICorDebugInfo::VLT_STK:
        {
            CORDB_ADDRESS pRemoteValue = m_nativeFrame->GetLSStackAddress(
                pNativeVarInfo->loc.vlStk.vlsBaseReg, pNativeVarInfo->loc.vlStk.vlsOffset) ;

            hr = m_nativeFrame->GetLocalMemoryValue(pRemoteValue,
                                                    type,
                                                    ppValue);
        }
        break;

    case ICorDebugInfo::VLT_REG_REG:
        hr = m_nativeFrame->GetLocalDoubleRegisterValue(
                            ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlRegReg.vlrrReg2),
                            ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlRegReg.vlrrReg1),
                            type, ppValue);
        break;

    case ICorDebugInfo::VLT_REG_STK:
        {
            CORDB_ADDRESS pRemoteValue = m_nativeFrame->GetLSStackAddress(
                pNativeVarInfo->loc.vlRegStk.vlrsStk.vlrssBaseReg, pNativeVarInfo->loc.vlRegStk.vlrsStk.vlrssOffset);

            hr = m_nativeFrame->GetLocalMemoryRegisterValue(
                          pRemoteValue,
                          ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlRegStk.vlrsReg),
                          type, ppValue);
        }
        break;

    case ICorDebugInfo::VLT_STK_REG:
        {
            CORDB_ADDRESS pRemoteValue = m_nativeFrame->GetLSStackAddress(
                pNativeVarInfo->loc.vlStkReg.vlsrStk.vlsrsBaseReg,  pNativeVarInfo->loc.vlStkReg.vlsrStk.vlsrsOffset);

            hr = m_nativeFrame->GetLocalRegisterMemoryValue(
                          ConvertRegNumToCorDebugRegister(pNativeVarInfo->loc.vlStkReg.vlsrReg),
                          pRemoteValue, type, ppValue);
        }
        break;

    case ICorDebugInfo::VLT_STK2:
        {
            CORDB_ADDRESS pRemoteValue = m_nativeFrame->GetLSStackAddress(
                pNativeVarInfo->loc.vlStk2.vls2BaseReg, pNativeVarInfo->loc.vlStk2.vls2Offset);

            hr = m_nativeFrame->GetLocalMemoryValue(pRemoteValue,
                                                    type,
                                                    ppValue);
        }
        break;

    case ICorDebugInfo::VLT_FPSTK:
#if defined(TARGET_ARM) // @ARMTODO
        hr = E_NOTIMPL;
#else
        /*
        @TODO [Microsoft] We have to make this work!!!!!!!!!!!!!
        hr = m_nativeFrame->GetLocalFloatingPointValue(
                         pNativeVarInfo->loc.vlFPstk.vlfReg + REGISTER_X86_FPSTACK_0,
                         type, ppValue);
                         */
        hr = CORDBG_E_IL_VAR_NOT_AVAILABLE;
#endif
        break;

    case ICorDebugInfo::VLT_FIXED_VA:
        if (m_sigParserCached.IsNull()) //no var args info
            return CORDBG_E_IL_VAR_NOT_AVAILABLE;

        CORDB_ADDRESS pRemoteValue;


#if defined(TARGET_X86) // STACK_GROWS_DOWN_ON_ARGS_WALK
        pRemoteValue = m_FirstArgAddr - pNativeVarInfo->loc.vlFixedVarArg.vlfvOffset;
        // Remember to subtract out this amount
        pRemoteValue += sizeof(((CORINFO_VarArgInfo*)0)->argBytes);
#else // STACK_GROWS_UP_ON_ARGS_WALK
        pRemoteValue = m_FirstArgAddr + pNativeVarInfo->loc.vlFixedVarArg.vlfvOffset;
#endif

        hr = m_nativeFrame->GetLocalMemoryValue(pRemoteValue,
                                                type,
                                                ppValue);

        break;


    default:
        _ASSERTE(!"Invalid locVarType");
        hr = E_FAIL;
        break;
    }

    return hr;
}

HRESULT CordbJITILFrame::EnumerateLocalVariables(ICorDebugValueEnum **ppValueEnum)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValueEnum, ICorDebugValueEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return EnumerateLocalVariablesEx(ILCODE_ORIGINAL_IL, ppValueEnum);
}

HRESULT CordbJITILFrame::GetLocalVariable(DWORD dwIndex,
                                          ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    return GetLocalVariableEx(ILCODE_ORIGINAL_IL, dwIndex, ppValue);
}


HRESULT CordbJITILFrame::EnumerateArguments(ICorDebugValueEnum **ppValueEnum)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValueEnum, ICorDebugValueEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;

    EX_TRY
    {
        RSInitHolder<CordbValueEnum> cdVE(new CordbValueEnum(m_nativeFrame, CordbValueEnum::ARGS));

        // Initialize the new enum
        hr = cdVE->Init();
        IfFailThrow(hr);

        cdVE.TransferOwnershipExternal(ppValueEnum);

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

//---------------------------------------------------------------------------------------
//
// This routine gets the value of a particular argument
//
// Arguments:
//    dwIndex - Index of the argument.
//    ppValue - OUT: Space for storing the value of the argument
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbJITILFrame::GetArgument(DWORD dwIndex, ICorDebugValue ** ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    const ICorDebugInfo::NativeVarInfo * pNativeInfo;

    //
    // First, make sure that we've got the jitted variable location data
    // loaded from the left side.
    //
    HRESULT hr = S_OK;
    EX_TRY
    {
        m_nativeFrame->m_nativeCode->LoadNativeInfo(); //throws

        hr = ILVariableToNative(dwIndex, &pNativeInfo);
        IfFailThrow(hr);

        // Get the type of this argument from the function
        CordbType * pType;

        hr = GetArgumentType(dwIndex, &pType);
        IfFailThrow(hr);

        hr = GetNativeVariable(pType, pNativeInfo, ppValue);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT CordbJITILFrame::GetStackDepth(ULONG32 *pDepth)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pDepth, ULONG32 *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    /* !!! */

    return E_NOTIMPL;
}

HRESULT CordbJITILFrame::GetStackValue(DWORD dwIndex, ICorDebugValue **ppValue)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    /* !!! */

    return E_NOTIMPL;
}

//---------------------------------------------------------------------------------------
//
// Remaps the active frame to the latest EnC version of the function, preserving the
// execution state of the method such as the values of locals.
// Can only be called when the leaf frame is at a remap opportunity.
//
// Arguments:
//    nOffset - the IL offset in the new version of the function to remap to
//

HRESULT CordbJITILFrame::RemapFunction(ULONG32 nOffset)
{
    HRESULT hr = S_OK;
    PUBLIC_API_BEGIN(this)
    {
#if !defined(FEATURE_REMAP_FUNCTION)
        ThrowHR(E_NOTIMPL);

#else  // FEATURE_REMAP_FUNCTION
        // Can only be called on leaf frame.
        if (!m_nativeFrame->IsLeafFrame())
        {
            ThrowHR(E_INVALIDARG);
        }

        // mark frames as not fresh, because this frame has been updated.
        m_nativeFrame->m_pThread->CleanupStack();

        // Since we may have overwritten anything (objects, code, etc), we should mark
        // everything as needing to be re-cached.
        m_nativeFrame->m_pThread->GetProcess()->m_continueCounter++;

        // Tell the left-side to do the remap
        hr = m_nativeFrame->m_pThread->SetRemapIP(nOffset);

#endif // FEATURE_REMAP_FUNCTION
    }
    PUBLIC_API_END(hr);

    return hr;
}
HRESULT CordbJITILFrame::GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue** ppReturnValue)
{
    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());
    EX_TRY
    {
        hr = GetReturnValueForILOffsetImpl(ILoffset, ppReturnValue);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}


HRESULT CordbJITILFrame::BuildInstantiationForCallsite(CordbModule * pModule, NewArrayHolder<CordbType*> &types, Instantiation &inst, Instantiation *currentInstantiation, mdToken targetClass, SigParser genericSig)
{
    // This function builds an Instantiation object (and backing "types" array) for a given
    // class and method signature.
    HRESULT hr = S_OK;
    RSExtSmartPtr<IMetaDataImport2> pImport2;
    IfFailRet(pModule->GetMetaDataImporter()->QueryInterface(IID_IMetaDataImport2, (void**)&pImport2));

    // If the targetClass is a TypeSpec that means its first element is GENERICINST.
    // We only need to build types for the Instantiation if targetClass is a TypeSpec.
    uint32_t classGenerics = 0;
    SigParser typeSig;
    if (TypeFromToken(targetClass) == mdtTypeSpec)
    {
        // Our goal with this is to full "classGenerics" with the number of
        // generics, and move "typeSig" to the start of the first generic type.
        PCCOR_SIGNATURE sig = 0;
        ULONG sigCount = 0;

        IfFailRet(pImport2->GetTypeSpecFromToken(targetClass, &sig, &sigCount));

        typeSig = SigParser(sig, sigCount);
        CorElementType elemType;
        IfFailRet(typeSig.GetElemType(&elemType));

        if (elemType != ELEMENT_TYPE_GENERICINST)
            return META_E_BAD_SIGNATURE;

        IfFailRet(typeSig.GetElemType(&elemType));
        if (elemType != ELEMENT_TYPE_VALUETYPE && elemType != ELEMENT_TYPE_CLASS)
            return META_E_BAD_SIGNATURE;

        IfFailRet(typeSig.GetToken(NULL));
        IfFailRet(typeSig.GetData(&classGenerics));
    }

    // Similarly for method generics.  Simply fill "methodGenerics" with the number
    // of generics, and move "genericSig" to the start of the first generic param.
    uint32_t methodGenerics = 0;
    if (!genericSig.IsNull())
    {
        uint32_t callingConv = 0;
        IfFailRet(genericSig.GetCallingConvInfo(&callingConv));
        if (callingConv == IMAGE_CEE_CS_CALLCONV_GENERICINST)
            IfFailRet(genericSig.GetData(&methodGenerics));
    }


    // Now build "types" and "inst".
    CordbType *pType = 0;
    types = new CordbType*[methodGenerics+classGenerics];
    ULONG i = 0;
    for (;i < classGenerics; ++i)
    {
        CorElementType et;
        IfFailRet(typeSig.PeekElemType(&et));
        if ((et == ELEMENT_TYPE_VAR || et == ELEMENT_TYPE_MVAR) && currentInstantiation->m_cInst == 0)
            return E_FAIL;

        CordbType::SigToType(pModule, &typeSig, currentInstantiation, &pType);
        types[i] = pType;
        typeSig.SkipExactlyOne();
    }

    for (; i < methodGenerics+classGenerics; ++i)
    {
        CorElementType et;
        IfFailRet(genericSig.PeekElemType(&et));
        if ((et == ELEMENT_TYPE_VAR || et == ELEMENT_TYPE_MVAR) && currentInstantiation->m_cInst == 0)
            return E_FAIL;

        CordbType::SigToType(pModule, &genericSig, currentInstantiation, &pType);
        types[i] = pType;
        genericSig.SkipExactlyOne();
    }

    inst = Instantiation(methodGenerics+classGenerics, types, classGenerics);
    return S_OK;
}

HRESULT CordbJITILFrame::GetReturnValueForILOffsetImpl(ULONG32 ILoffset, ICorDebugValue** ppReturnValue)
{
    if (ppReturnValue == NULL)
        return E_INVALIDARG;

    if (!m_genericArgsLoaded)
        LoadGenericArgs();

    // First verify that we're stopped at the correct native offset
    // by calling ICorDebugCode3::GetReturnValueLiveOffset and
    // compare the returned native offset to our current location.
    HRESULT hr = S_OK;
    CordbNativeCode *pCode = m_nativeFrame->m_nativeCode;
    pCode->LoadNativeInfo();

    ULONG32 count = 0;
    IfFailRet(pCode->GetReturnValueLiveOffsetImpl(&m_genericArgs, ILoffset, 0, &count, NULL));

    NewArrayHolder<ULONG32> offsets(new ULONG32[count]);
    IfFailRet(pCode->GetReturnValueLiveOffsetImpl(&m_genericArgs, ILoffset, count, &count, offsets));

    bool found = false;
    ULONG32 currentOffset = m_nativeFrame->GetIPOffset();
    for (ULONG32 i = 0; i < count; ++i)
    {
        if (currentOffset == offsets[i])
        {
            found = true;
            break;
        }
    }

    if (!found)
        return E_UNEXPECTED;

    // Get the signatures and mdToken for the callee.
    SigParser methodSig, genericSig;
    mdToken mdFunction = 0, targetClass = 0;
    IfFailRet(pCode->GetCallSignature(ILoffset, &targetClass, &mdFunction, methodSig, genericSig));
    IfFailRet(CordbNativeCode::SkipToReturn(methodSig));




    // Create the Instantiation, type and then return value
    NewArrayHolder<CordbType*> types;
    Instantiation inst;
    CordbType *pType = 0;
    IfFailRet(BuildInstantiationForCallsite(GetModule(), types, inst, &m_genericArgs, targetClass, genericSig));
    IfFailRet(CordbType::SigToType(GetModule(), &methodSig, &inst, &pType));
    return GetReturnValueForType(pType, ppReturnValue);
}


HRESULT CordbJITILFrame::GetReturnValueForType(CordbType *pType, ICorDebugValue **ppReturnValue)
{


#if defined(TARGET_X86)
    const CorDebugRegister floatRegister = REGISTER_X86_FPSTACK_0;
#elif defined(TARGET_AMD64)
    const CorDebugRegister floatRegister = REGISTER_AMD64_XMM0;
#elif  defined(TARGET_ARM64)
    const CorDebugRegister floatRegister = REGISTER_ARM64_V0;
#elif  defined(TARGET_ARM)
    const CorDebugRegister floatRegister = REGISTER_ARM_D0;
#elif  defined(TARGET_LOONGARCH64)
    const CorDebugRegister floatRegister = REGISTER_LOONGARCH64_F0;
#elif  defined(TARGET_RISCV64)
    const CorDebugRegister floatRegister = REGISTER_RISCV64_F0;
#endif

#if defined(TARGET_X86)
    const CorDebugRegister ptrRegister = REGISTER_X86_EAX;
    const CorDebugRegister ptrHighWordRegister = REGISTER_X86_EDX;
#elif defined(TARGET_AMD64)
    const CorDebugRegister ptrRegister = REGISTER_AMD64_RAX;
#elif  defined(TARGET_ARM64)
    const CorDebugRegister ptrRegister = REGISTER_ARM64_X0;
#elif  defined(TARGET_ARM)
    const CorDebugRegister ptrRegister = REGISTER_ARM_R0;
    const CorDebugRegister ptrHighWordRegister = REGISTER_ARM_R1;
#elif  defined(TARGET_LOONGARCH64)
    const CorDebugRegister ptrRegister = REGISTER_LOONGARCH64_A0;
#elif  defined(TARGET_RISCV64)
    const CorDebugRegister ptrRegister = REGISTER_RISCV64_A0;
#endif

    CorElementType corReturnType = pType->GetElementType();
    switch (corReturnType)
    {
    default:
        return m_nativeFrame->GetLocalRegisterValue(ptrRegister, pType, ppReturnValue);

    case ELEMENT_TYPE_R4:
    case ELEMENT_TYPE_R8:
        return m_nativeFrame->GetLocalFloatingPointValue(floatRegister, pType, ppReturnValue);

#if defined(TARGET_X86) || defined(TARGET_ARM)
    case ELEMENT_TYPE_I8:
    case ELEMENT_TYPE_U8:
        return m_nativeFrame->GetLocalDoubleRegisterValue(ptrHighWordRegister, ptrRegister, pType, ppReturnValue);
#endif
    }
}

HRESULT CordbJITILFrame::EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum **ppValueEnum)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppValueEnum, ICorDebugValueEnum **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    HRESULT hr = S_OK;
    if (flags != ILCODE_ORIGINAL_IL && flags != ILCODE_REJIT_IL)
        return E_INVALIDARG;

    EX_TRY
    {
        RSInitHolder<CordbValueEnum> cdVE(new CordbValueEnum(m_nativeFrame,
            flags == ILCODE_ORIGINAL_IL ? CordbValueEnum::LOCAL_VARS_ORIGINAL_IL : CordbValueEnum::LOCAL_VARS_REJIT_IL));

        // Initialize the new enum
        hr = cdVE->Init();
        IfFailThrow(hr);

        cdVE.TransferOwnershipExternal(ppValueEnum);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
HRESULT CordbJITILFrame::GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex, ICorDebugValue **ppValue)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (flags != ILCODE_ORIGINAL_IL && flags != ILCODE_REJIT_IL)
        return E_INVALIDARG;
    if (flags == ILCODE_REJIT_IL && m_pReJitCode == NULL)
        return E_INVALIDARG;

    const ICorDebugInfo::NativeVarInfo *pNativeInfo;

    //
    // First, make sure that we've got the jitted variable location data
    // loaded from the left side.
    //

    HRESULT hr = S_OK;
    EX_TRY
    {
        m_nativeFrame->m_nativeCode->LoadNativeInfo(); //throws

        ULONG cArgs;
        if (m_fVarArgFnx && (!m_sigParserCached.IsNull()))
        {
            cArgs = m_allArgsCount;
        }
        else
        {
            cArgs = m_nativeFrame->m_nativeCode->GetFixedArgCount();
        }

        hr = ILVariableToNative(dwIndex + cArgs, &pNativeInfo);
        IfFailThrow(hr);

        LoadGenericArgs();

        // Get the type of this argument from the function
        CordbType *type;
        CordbILCode* pActiveCode = m_pReJitCode != NULL ? m_pReJitCode : m_ilCode;
        hr = pActiveCode->GetLocalVariableType(dwIndex, &(this->m_genericArgs), &type);
        IfFailThrow(hr);

        // if the caller wants the original IL local, it should implicitly map to the same index
        // variable in the profiler instrumented code. We can't determine whether the instrumented code
        // really adhered to this, but we can check two things:
        // a) the requested index was valid in the original signature
        //       (GetLocalVariableType will return E_INVALIDARG if not)
        // b) the type of local in the original signature matches the type of local in the instrumented signature
        //       (the code below will return CORDBG_E_IL_VAR_NOT_AVAILABLE)
        if (flags == ILCODE_ORIGINAL_IL && m_pReJitCode != NULL)
        {
            CordbType* pOriginalType;
            hr = m_ilCode->GetLocalVariableType(dwIndex, &(this->m_genericArgs), &pOriginalType);
            IfFailThrow(hr);
            if (pOriginalType != type)
            {
                IfFailThrow(CORDBG_E_IL_VAR_NOT_AVAILABLE); // bad profiler, it shouldn't have changed types
            }
        }


        hr = GetNativeVariable(type, pNativeInfo, ppValue);
        IfFailThrow(hr);
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}

HRESULT CordbJITILFrame::GetCodeEx(ILCodeKind flags, ICorDebugCode **ppCode)
{
    HRESULT hr = S_OK;
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    if (flags != ILCODE_ORIGINAL_IL && flags != ILCODE_REJIT_IL)
        return E_INVALIDARG;

    if (flags == ILCODE_ORIGINAL_IL)
    {
        return GetCode(ppCode);
    }
    else
    {
        *ppCode = m_pReJitCode;
        if (m_pReJitCode != NULL)
        {
            m_pReJitCode->ExternalAddRef();
        }
    }
    return S_OK;
}

CordbILCode* CordbJITILFrame::GetOriginalILCode()
{
    return m_ilCode;
}

CordbReJitILCode* CordbJITILFrame::GetReJitILCode()
{
    return m_pReJitCode;
}

/* ------------------------------------------------------------------------- *
 * Eval class
 * ------------------------------------------------------------------------- */

CordbEval::CordbEval(CordbThread *pThread)
    : CordbBase(pThread->GetProcess(), 0, enumCordbEval),
      m_thread(pThread), // implicit InternalAddRef
      m_function(NULL),
      m_complete(false),
      m_successful(false),
      m_aborted(false),
      m_resultAddr(NULL),
      m_evalDuringException(false)
{
    m_vmObjectHandle = VMPTR_OBJECTHANDLE::NullPtr();
    m_debuggerEvalKey = LSPTR_DEBUGGEREVAL::NullPtr();

    m_resultType.elementType = ELEMENT_TYPE_VOID;
    m_resultAppDomainToken = VMPTR_AppDomain::NullPtr();

    CordbAppDomain * pDomain = m_thread->GetAppDomain();
    (void)pDomain; //prevent "unused variable" error from GCC
#ifdef _DEBUG
    // Remember what AD we started in so that we can check that we finish there too.
    m_DbgAppDomainStarted = pDomain;
#endif

    // Place ourselves on the processes neuter-list.
    HRESULT hr = S_OK;
    EX_TRY
    {
        GetProcess()->AddToLeftSideResourceCleanupList(this);
    }
    EX_CATCH_HRESULT(hr);
    SetUnrecoverableIfFailed(GetProcess(), hr);
}

CordbEval::~CordbEval()
{
    _ASSERTE(IsNeutered());
}

// Free the left-side resources for the eval.
void CordbEval::NeuterLeftSideResources()
{
    SendCleanup();

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    Neuter();
}

// Neuter the CordbEval
//
// Assumptions:
//    By the time we neuter the eval, it's associated left-side resources
//    are already cleaned up (either explicitly from calling code:CordbEval::SendCleanup
//    or implicitly from the left-side exiting).
//
// Notes:
//    We place ourselves on a neuter list. This gets called when the neuterlist sweeps.
void CordbEval::Neuter()
{
    // By now, we should have freed our target-resources (code:CordbEval::NeuterLeftSideResources
    // or code:CordbEval::SendCleanup), unless the target is dead (terminated or about to exit).
    BOOL fTargetIsDead = !GetProcess()->IsSafeToSendEvents() || GetProcess()->m_exiting;
    (void)fTargetIsDead; //prevent "unused variable" error from GCC
    _ASSERTE(fTargetIsDead || (m_debuggerEvalKey == NULL));

    m_thread.Clear();

    CordbBase::Neuter();
}

HRESULT CordbEval::SendCleanup()
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    HRESULT hr = S_OK;

    // Send a message to the left side to release the eval object over
    // there if one exists.
    if ((m_debuggerEvalKey != NULL) &&
        GetProcess()->IsSafeToSendEvents())
    {
        // Call Abort() before doing new CallFunction()
        if (!m_complete)
            return CORDBG_E_FUNC_EVAL_NOT_COMPLETE;

        // Release the left side handle to the object
        DebuggerIPCEvent event;

        GetProcess()->InitIPCEvent(
                                &event,
                                DB_IPCE_FUNC_EVAL_CLEANUP,
                                true,
                                m_thread->GetAppDomain()->GetADToken());

        event.FuncEvalCleanup.debuggerEvalKey = m_debuggerEvalKey;

        hr = GetProcess()->SendIPCEvent(&event, sizeof(DebuggerIPCEvent));
        IfFailRet(hr);

#if _DEBUG
        if (SUCCEEDED(hr))
            _ASSERTE(event.type == DB_IPCE_FUNC_EVAL_CLEANUP_RESULT);
#endif

        // Null out the key so we don't try to do this again.
        m_debuggerEvalKey = LSPTR_DEBUGGEREVAL::NullPtr();

        hr = event.hr;
    }

    // Release the cached HandleValue for the result. This may cleanup resources,
    // like our object handle to the func-eval result.
    m_pHandleValue.Clear();


    return hr;
}

HRESULT CordbEval::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugEval)
    {
        *pInterface = static_cast<ICorDebugEval*>(this);
    }
    else if (id == IID_ICorDebugEval2)
    {
        *pInterface = static_cast<ICorDebugEval2*>(this);
    }
    else if (id == IID_IUnknown)
    {
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugEval*>(this));
    }
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

//
// Gather data about an argument to either CallFunction or NewObject
// and place it into a DebuggerIPCE_FuncEvalArgData struct for passing
// to the Left Side.
//
HRESULT CordbEval::GatherArgInfo(ICorDebugValue *pValue,
                                 DebuggerIPCE_FuncEvalArgData *argData)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //

    HRESULT hr;
    CORDB_ADDRESS addr;
    CorElementType ty;
    bool needRelease = false;

    pValue->GetType(&ty);

    // Note: if the value passed in is in fact a byref, then we need to dereference it to get to the real thing. Passing
    // a byref as a byref to a func eval is never right.
    if ((ty == ELEMENT_TYPE_BYREF) || (ty == ELEMENT_TYPE_TYPEDBYREF))
    {
        ICorDebugReferenceValue *prv = NULL;

        // The value had better implement ICorDebugReference value.
        IfFailRet(pValue->QueryInterface(IID_ICorDebugReferenceValue, (void**)&prv));

        // This really should always work for a byref, unless we're out of memory.
        hr = prv->Dereference(&pValue);
        prv->Release();

        IfFailRet(hr);

        // Make sure to get the type we were referencing for use below.
        pValue->GetType(&ty);
        needRelease = true;
    }

    // We should never have a byref by this point.
    _ASSERTE((ty != ELEMENT_TYPE_BYREF) && (ty != ELEMENT_TYPE_TYPEDBYREF));

    pValue->GetAddress(&addr);

    argData->argAddr = CORDB_ADDRESS_TO_PTR(addr);
    argData->argElementType = ty;

    argData->argIsHandleValue = false;
    argData->argIsLiteral = false;
    argData->fullArgType = NULL;
    argData->fullArgTypeNodeCount = 0;

    // We have to have knowledge of our value implementation here,
    // which it would nice if we didn't have to know.
    CordbValue *cv = NULL;

    switch(ty)
    {

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
    case ELEMENT_TYPE_STRING:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
        {
            ICorDebugHandleValue *pHandle = NULL;
            pValue->QueryInterface(IID_ICorDebugHandleValue, (void **) &pHandle);
            if (pHandle == NULL)
            {
                // A reference value
                cv = static_cast<CordbValue*> (static_cast<CordbReferenceValue*> (pValue));
                argData->argIsHandleValue = !(((CordbReferenceValue *)pValue)->m_valueHome.ObjHandleIsNull());

                // Is this a literal value? If, we'll copy the data to the
                // buffer area so the left side can get it.
                CordbReferenceValue *rv;
                rv = static_cast<CordbReferenceValue*>(pValue);
                argData->argIsLiteral = rv->CopyLiteralData(argData->argLiteralData);
                if (rv->GetValueHome())
                {
                    rv->GetValueHome()->CopyToIPCEType(&(argData->argHome));
                }
            }
            else
            {
                argData->argIsHandleValue = true;
                argData->argIsLiteral = false;
                pHandle->Release();
                argData->argHome.kind = RAK_NONE;
            }
        }
        break;

    case ELEMENT_TYPE_VALUETYPE:  // OK: this E_T_VALUETYPE comes ICorDebugValue::GetType

        // A value class object
        cv = static_cast<CordbValue*> (static_cast<CordbVCObjectValue*>(static_cast<ICorDebugObjectValue*> (pValue)));

        // The EE does not guarantee to have exact type information
        // available for all struct types, so we indicate the type by using a
        // DebuggerIPCE_TypeArgData serialization of a type.
        //
        // At the moment the LHS only cares about this data
        // when boxing the "this" pointer.
        {
            CordbVCObjectValue * pVCObjVal =
                static_cast<CordbVCObjectValue *>(static_cast<ICorDebugObjectValue*> (pValue));

            unsigned int fullArgTypeNodeCount = 0;
            cv->m_type->CountTypeDataNodes(&fullArgTypeNodeCount);

            _ASSERTE(fullArgTypeNodeCount > 0);
            unsigned int bufferSize = sizeof(DebuggerIPCE_TypeArgData) * fullArgTypeNodeCount;
            DebuggerIPCE_TypeArgData *bufferFrom = (DebuggerIPCE_TypeArgData *) _alloca(bufferSize);

            DebuggerIPCE_TypeArgData *curr = bufferFrom;
            CordbType::GatherTypeData(cv->m_type, &curr);

            void *buffer = NULL;
            IfFailRet(m_thread->GetProcess()->GetAndWriteRemoteBuffer(m_thread->GetAppDomain(), bufferSize, bufferFrom, &buffer));

            argData->fullArgType = buffer;
            argData->fullArgTypeNodeCount = fullArgTypeNodeCount;
            // Is it enregistered?
            if ((addr == NULL) && (pVCObjVal->GetValueHome() != NULL))
            {
                pVCObjVal->GetValueHome()->CopyToIPCEType(&(argData->argHome));
            }

        }
        break;

    default:

        // A generic value
        cv = static_cast<CordbValue*> (static_cast<CordbGenericValue*> (pValue));

        // Is this a literal value? If, we'll copy the data to the
        // buffer area so the left side can get it.
        CordbGenericValue *gv = (CordbGenericValue*)pValue;
        argData->argIsLiteral = gv->CopyLiteralData(argData->argLiteralData);
        // Is it enregistered?
        if ((addr == NULL) && (gv->GetValueHome() != NULL))
        {
            gv->GetValueHome()->CopyToIPCEType(&(argData->argHome));
        }
        break;
    }


    // Release pValue if we got it via a dereference from above.
    if (needRelease)
        pValue->Release();

    return S_OK;
}


HRESULT CordbEval::SendFuncEval(unsigned int genericArgsCount,
                                ICorDebugType *genericArgs[],
                                void *argData1, unsigned int argData1Size,
                                void *argData2, unsigned int argData2Size,
                                DebuggerIPCEvent * event)
{
    FAIL_IF_NEUTERED(this);
    INTERNAL_SYNC_API_ENTRY(GetProcess()); //
    unsigned int genericArgsNodeCount = 0;

    DebuggerIPCE_TypeArgData *tyargData = NULL;
    CordbType::CountTypeDataNodesForInstantiation(genericArgsCount,genericArgs,&genericArgsNodeCount);

    unsigned int tyargDataSize = sizeof(DebuggerIPCE_TypeArgData) * genericArgsNodeCount;

    if (genericArgsNodeCount > 0)
    {
        tyargData = new (nothrow) DebuggerIPCE_TypeArgData[genericArgsNodeCount];
        if (tyargData == NULL)
        {
            return E_OUTOFMEMORY;
        }

        DebuggerIPCE_TypeArgData *curr_tyargData = tyargData;
        CordbType::GatherTypeDataForInstantiation(genericArgsCount, genericArgs, &curr_tyargData);

    }
    event->FuncEval.genericArgsNodeCount = genericArgsNodeCount;


    // Are we doing an eval during an exception? If so, we need to remember
    // that over here and also tell the Left Side.
    event->FuncEval.evalDuringException = m_thread->HasException();
    m_evalDuringException = !!event->FuncEval.evalDuringException;
    m_vmThreadOldExceptionHandle = m_thread->GetThreadExceptionRawObjectHandle();

    // Corresponding Release() on DB_IPCE_FUNC_EVAL_COMPLETE.
    // If a func eval is aborted, the LHS may not complete the abort
    // immediately and hence we cant do a SendCleanup(). Hence, we maintain
    // an extra ref-count to determine when this can be done.
    AddRef();

    HRESULT hr = m_thread->GetProcess()->SendIPCEvent(event, sizeof(DebuggerIPCEvent));

    // If the send failed, return that failure.
    if (FAILED(hr))
        goto LExit;

    _ASSERTE(event->type == DB_IPCE_FUNC_EVAL_SETUP_RESULT);

    hr = event->hr;

    // Memory has been allocated to hold info about each argument on
    // the left side now, so copy the argument data over to the left
    // side. No need to send another event, since the left side won't
    // take any more action on this evaluation until the process is
    // continued anyway.
    //
    // The type arguments come first, followed by up to two blobs of data
    // for other arguments.
    if (SUCCEEDED(hr))
    {
        EX_TRY
        {
            CORDB_ADDRESS argdata = event->FuncEvalSetupComplete.argDataArea;

            if ((tyargData != NULL) && (tyargDataSize != 0))
            {

                TargetBuffer tb(argdata, tyargDataSize);
                m_thread->GetProcess()->SafeWriteBuffer(tb, (const BYTE*) tyargData); // throws

                argdata += tyargDataSize;
            }

            if ((argData1 != NULL) && (argData1Size != 0))
            {
                TargetBuffer tb(argdata, argData1Size);
                m_thread->GetProcess()->SafeWriteBuffer(tb, (const BYTE*) argData1); // throws

                argdata += argData1Size;
            }

            if ((argData2 != NULL) && (argData2Size != 0))
            {
                TargetBuffer tb(argdata, argData2Size);
                m_thread->GetProcess()->SafeWriteBuffer(tb, (const BYTE*) argData2); // throws

                argdata += argData2Size;
            }
        }
        EX_CATCH_HRESULT(hr);
    }

LExit:
    if (tyargData)
    {
        delete [] tyargData;
    }

    // Save the key to the eval on the left side for future reference.
    if (SUCCEEDED(hr))
    {
        m_debuggerEvalKey = event->FuncEvalSetupComplete.debuggerEvalKey;
        m_thread->GetProcess()->IncrementOutstandingEvalCount();
    }
    else
    {
        // We dont expect to receive a DB_IPCE_FUNC_EVAL_COMPLETE, so just release here
        Release();
    }

    return hr;
}


// Get the AppDomain that an object lives in.
// This does not adjust any reference counts.
// Returns NULL if we can't determine the appdomain, or if the value is known to be agile.
CordbAppDomain * GetAppDomainFromValue(ICorDebugValue * pValue)
{
    // Unfortunately, there's no direct way to cast from an ICDValue to a CordbValue.
    // So we need to QI for the culprit interfaces and check specifically.

    {
        RSExtSmartPtr<ICorDebugHandleValue> handleP;
        pValue->QueryInterface(IID_ICorDebugHandleValue, (void**)&handleP);
        if (handleP != NULL)
        {
            CordbHandleValue * chp = static_cast<CordbHandleValue *> (handleP.GetValue());
            return chp->GetAppDomain();
        }
    }

    {
        RSExtSmartPtr<ICorDebugReferenceValue> refP;
        pValue->QueryInterface(IID_ICorDebugReferenceValue, (void**)&refP);
        if (refP != NULL)
        {
            CordbReferenceValue * crp = static_cast<CordbReferenceValue *> (refP.GetValue());
            return crp->GetAppDomain();
        }
    }

    {
        RSExtSmartPtr<ICorDebugObjectValue> objP;
        pValue->QueryInterface(IID_ICorDebugObjectValue, (void**)&objP);
        if (objP != NULL)
        {
            CordbVCObjectValue * crp = static_cast<CordbVCObjectValue*> (objP.GetValue());
            return crp->GetAppDomain();
        }
    }

    // Assume nothing else has AD affinity.
    return NULL;
}

HRESULT CordbEval::CallFunction(ICorDebugFunction *pFunction,
                                ULONG32 nArgs,
                                ICorDebugValue *pArgs[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    if (GetProcess()->GetShim() == NULL)
    {
        return E_NOTIMPL;
    }
    return  CallParameterizedFunction(pFunction,0,NULL,nArgs,pArgs);
}

//-----------------------------------------------------------------------------
// See if we can convert general Func-eval failure HRs (which are usually based on EE-invariants that
// may be meaningless to the user) into a more specific user-friendly hr.
// Doing the conversions here in the RS (instead of in the LS) makes it more clear that these
// HRs definitely map to concepts described by the ICorDebugAPI instead of EE-invariants.
// It also lets us clearly prioritize the HRs in case of ambiguity.
//-----------------------------------------------------------------------------
HRESULT CordbEval::FilterHR(HRESULT hr)
{
    // Currently, we only make CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT more specific.
    // If it's not that HR, then shortcut our work.
    if (hr != CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT)
    {
        return hr;
    }

    // In the case of conflicting HRs (if the func-eval fails for multiple reasons),
    // we'll try to give priority to the more general HR.
    // This communicates the quickest action for the user to be able to get to a
    // func-eval friendly spot. It also means less churn in the hrs we return
    // because specific hrs are more likely to change than general ones.

    // If we got CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT, check the common reasons.
    // We'll use the Right-Side's intimate knowledge of the Left-Side to guess _why_
    // it's a GC-unsafe spot, and then we'll communicate that back w/ a more meaningful HR.
    // If GC safe-spots change, then these errors should be updated.


    //
    // Most likely is if we're in native code. Check that first.
    //
    // In V2, we do this check by checking if the leaf chain is native.  Since we have no chain in Arrowhead,
    // we can't do this check.  Instead, we check whether the active frame is NULL or not.  If it's NULL,
    // then we are stopped in native code.
    //
    HRESULT hrTemp = S_OK;
    if (GetProcess()->GetShim() != NULL)
    {
        // the V2 case
        RSExtSmartPtr<ICorDebugChain> pChain;
        hrTemp = m_thread->GetActiveChain(&pChain);
        if (FAILED(hrTemp))
        {
            // just return the original HR if this call fails
            return hr;
        }

        // pChain should never be NULL here, since we should have at least one thread start chain even if
        // there is no managed code on the stack, but let's just be extra careful here.
        if (pChain == NULL)
        {
            return hr;
        }

        BOOL fManagedChain;
        hrTemp = pChain->IsManaged(&fManagedChain);
        if (FAILED(hrTemp))
        {
            // just return the original HR if this call fails
            return hr;
        }

        if (fManagedChain == FALSE)
        {
            return CORDBG_E_ILLEGAL_IN_NATIVE_CODE;
        }
    }

    RSExtSmartPtr<ICorDebugFrame> pIFrame;
    hrTemp = m_thread->GetActiveFrame(&pIFrame);
    if (FAILED(hrTemp))
    {
        // just return the original HR if this call fails
        return hr;
    }

    CordbFrame * pFrame = NULL;
    pFrame = CordbFrame::GetCordbFrameFromInterface(pIFrame);

    if (GetProcess()->GetShim() == NULL)
    {
        // the Arrowhead case
        if (pFrame == NULL)
    {
        return CORDBG_E_ILLEGAL_IN_NATIVE_CODE;
    }
    }

    // Next, check if we're in optimized code.
    // Optimized code doesn't directly mean that func-evals are illegal; but it greatly
    // increases the odds of being at a GC-unsafe point.
    // We give this failure higher precedence than the "Is in prolog" failure.

    if (pFrame != NULL)
    {
       CordbNativeFrame * pNativeFrame = pFrame->GetAsNativeFrame();
       if (pNativeFrame != NULL)
       {
            CordbNativeCode * pCode = pNativeFrame->GetNativeCode();
            if (pCode != NULL)
            {
                DWORD flags;
                hrTemp = pCode->GetModule()->GetJITCompilerFlags(&flags);

                if (SUCCEEDED(hrTemp))
                {
                    if ((flags & CORDEBUG_JIT_DISABLE_OPTIMIZATION) != CORDEBUG_JIT_DISABLE_OPTIMIZATION)
                    {
                        return CORDBG_E_ILLEGAL_IN_OPTIMIZED_CODE;
                    }

                } // GetCompilerFlags
            } // Code

            CordbJITILFrame * pILFrame = pNativeFrame->m_JITILFrame;
            if (pILFrame != NULL)
            {
                if (pILFrame->m_mapping == MAPPING_PROLOG)
                {
                    return CORDBG_E_ILLEGAL_IN_PROLOG;
                }
            }
       } // Native Frame
    }

    // No filtering.
    return hr;

}

//---------------------------------------------------------------------------------------
//
// This routine calls a function with the given set of type arguments and actual arguments.
// This is the jumping off point for func-eval.
//
// Arguments:
//    pFunction - The function to call.
//    nTypeArgs - The number of type-arguments for the method in rgpTypeArgs
//    rgpTypeArgs - An array of pointers to types.
//    nArgs - The number of arguments for the method in rgpArgs
//    rgpArgs - An array of pointers to values for the arguments to the method.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbEval::CallParameterizedFunction(ICorDebugFunction *pFunction,
                                             ULONG32 nTypeArgs,
                                             ICorDebugType * rgpTypeArgs[],
                                             ULONG32 nArgs,
                                             ICorDebugValue * rgpArgs[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    VALIDATE_POINTER_TO_OBJECT(pFunction, ICorDebugFunction *);

    if (nArgs > 0)
    {
        VALIDATE_POINTER_TO_OBJECT_ARRAY(rgpArgs, ICorDebugValue *, nArgs, true, true);
    }

    HRESULT hr = E_FAIL;

    {
        ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

        // The LS will assume that all of the ICorDebugValues and ICorDebugTypes are in
        // the same appdomain as the function.  Verify this.
        CordbAppDomain * pMethodAppDomain = (static_cast<CordbFunction *> (pFunction))->GetAppDomain();

        if (!DoAppDomainsMatch(pMethodAppDomain, nTypeArgs, rgpTypeArgs, nArgs, rgpArgs))
        {
            return ErrWrapper(CORDBG_E_APPDOMAIN_MISMATCH);
        }

        // Callers are free to reuse an ICorDebugEval object for multiple
        // evals. Since we create a Left Side eval representation each
        // time, we need to be sure to clean it up now that we know we're
        // done with it.
        hr = SendCleanup();

        if (FAILED(hr))
        {
            return hr;
        }

        RSLockHolder lockHolder(GetProcess()->GetProcessLock());

        // Must be locked to get a cookie
        RsPtrHolder<CordbEval> hFuncEval(this);

        if (hFuncEval.Ptr().IsNull())
        {
            return E_OUTOFMEMORY;
        }
        lockHolder.Release(); // release to send an IPC event.

        // Remember the function that we're evaluating.
        m_function = static_cast<CordbFunction *>(pFunction);
        m_evalType = DB_IPCE_FET_NORMAL;


        // Arrange the arguments into a form that the left side can deal
        // with. We do this before starting the func eval setup to ensure
        // that we can complete this step before mutating the left
        // side.
        DebuggerIPCE_FuncEvalArgData * pArgData = NULL;

        if (nArgs > 0)
        {
            // We need to make the same type of array that the left side
            // holds.
            pArgData = new (nothrow) DebuggerIPCE_FuncEvalArgData[nArgs];

            if (pArgData == NULL)
            {
                return E_OUTOFMEMORY;
            }

            // For each argument, convert its home into something the left
            // side can understand.
            for (unsigned int i = 0; i < nArgs; i++)
            {
                hr = GatherArgInfo(rgpArgs[i], &(pArgData[i]));

                if (FAILED(hr))
                {
                    delete [] pArgData;
                    return hr;
                }
            }
        }

        // Send over to the left side and get it to setup this eval.
        DebuggerIPCEvent event;
        m_thread->GetProcess()->InitIPCEvent(&event, DB_IPCE_FUNC_EVAL, true, m_thread->GetAppDomain()->GetADToken());

        event.FuncEval.vmThreadToken = m_thread->m_vmThreadToken;
        event.FuncEval.funcEvalType = m_evalType;
        event.FuncEval.funcMetadataToken = m_function->GetMetadataToken();
        event.FuncEval.vmDomainAssembly = m_function->GetModule()->GetRuntimeDomainAssembly();
        event.FuncEval.funcEvalKey = hFuncEval.Ptr();
        event.FuncEval.argCount = nArgs;
        event.FuncEval.genericArgsCount = nTypeArgs;



        hr = SendFuncEval(nTypeArgs,
                          rgpTypeArgs,
                          reinterpret_cast<void *>(pArgData),
                          sizeof(DebuggerIPCE_FuncEvalArgData) * nArgs,
                          NULL,
                          0,
                          &event);

        // Cleanup

        if (pArgData)
        {
            delete [] pArgData;
        }

        if (SUCCEEDED(hr))
        {
            hFuncEval.SuppressRelease(); // Now LS owns.
        }
    }

    // Convert from LS EE-centric failure code to something more friendly to end-users.
    // Success HRs will not be converted.
    hr = FilterHR(hr);

    // Return any failure the Left Side may have told us about.
    return hr;
}

BOOL CordbEval::DoAppDomainsMatch( CordbAppDomain * pAppDomain,
                                            ULONG32 nTypes,
                                             ICorDebugType *pTypes[],
                                            ULONG32 nValues,
                                            ICorDebugValue *pValues[] )
{
    _ASSERTE( !(pTypes == NULL && nTypes != 0) );
    _ASSERTE( !(pValues == NULL && nValues != 0) );

    // Make sure each value is in the appdomain.
    for(unsigned int i = 0; i < nValues; i++)
    {
        // Assuming that only Ref Values have AD affinity
        CordbAppDomain * pValueAppDomain = GetAppDomainFromValue( pValues[i] );

        if ((pValueAppDomain != NULL) && (pValueAppDomain != pAppDomain))
        {
            LOG((LF_CORDB,LL_INFO1000, "CordbEval::DADM - AD mismatch. appDomain=0x%08x, param #%d=0x%08x, must fail.\n",
                pAppDomain, i, pValueAppDomain));
            return FALSE;
        }
    }

    for(unsigned int i = 0; i < nTypes; i++ )
    {
        CordbType* t = static_cast<CordbType*>( pTypes[i] );
        CordbAppDomain * pTypeAppDomain = t->GetAppDomain();

        if( pTypeAppDomain != NULL && pTypeAppDomain != pAppDomain )
        {
            LOG((LF_CORDB,LL_INFO1000, "CordbEval::DADM - AD mismatch. appDomain=0x%08x, type param #%d=0x%08x, must fail.\n",
                pAppDomain, i, pTypeAppDomain));
            return FALSE;
        }
    }

    return TRUE;
}

HRESULT CordbEval::NewObject(ICorDebugFunction *pConstructor,
                             ULONG32 nArgs,
                             ICorDebugValue *pArgs[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return NewParameterizedObject(pConstructor,0,NULL,nArgs,pArgs);
}

//---------------------------------------------------------------------------------------
//
// This routine calls a constructor with the given set of type arguments and actual arguments.
// This is the jumping off point for func-evaling "new".
//
// Arguments:
//    pConstructor - The function to call.
//    nTypeArgs - The number of type-arguments for the method in rgpTypeArgs
//    rgpTypeArgs - An array of pointers to types.
//    nArgs - The number of arguments for the method in rgpArgs
//    rgpArgs - An array of pointers to values for the arguments to the method.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbEval::NewParameterizedObject(ICorDebugFunction * pConstructor,
                                          ULONG32 nTypeArgs,
                                          ICorDebugType * rgpTypeArgs[],
                                          ULONG32 nArgs,
                                          ICorDebugValue * rgpArgs[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pConstructor, ICorDebugFunction *);
    VALIDATE_POINTER_TO_OBJECT_ARRAY(rgpArgs, ICorDebugValue *, nArgs, true, true);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // The LS will assume that all of the ICorDebugValues and ICorDebugTypes are in
    // the same appdomain as the constructor.  Verify this.
    CordbAppDomain * pConstructorAppDomain = (static_cast<CordbFunction *> (pConstructor))->GetAppDomain();

    if (!DoAppDomainsMatch(pConstructorAppDomain, nTypeArgs, rgpTypeArgs, nArgs, rgpArgs))
    {
        return ErrWrapper(CORDBG_E_APPDOMAIN_MISMATCH);
    }

    // Callers are free to reuse an ICorDebugEval object for multiple
    // evals. Since we create a Left Side eval representation each
    // time, we need to be sure to clean it up now that we know we're
    // done with it.
    HRESULT hr = SendCleanup();

    if (FAILED(hr))
    {
        return hr;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    RsPtrHolder<CordbEval> hFuncEval(this);

    if (hFuncEval.Ptr().IsNull())
    {
        return E_OUTOFMEMORY;
    }
    lockHolder.Release();

    // Remember the function that we're evaluating.
    m_function = static_cast<CordbFunction *>(pConstructor);
    m_evalType = DB_IPCE_FET_NEW_OBJECT;

    // Arrange the arguments into a form that the left side can deal
    // with. We do this before starting the func eval setup to ensure
    // that we can complete this step before mutating up the left
    // side.
    DebuggerIPCE_FuncEvalArgData * pArgData = NULL;

    if (nArgs > 0)
    {
        // We need to make the same type of array that the left side
        // holds.
        pArgData = new (nothrow) DebuggerIPCE_FuncEvalArgData[nArgs];

        if (pArgData == NULL)
        {
            return E_OUTOFMEMORY;
        }

        // For each argument, convert its home into something the left
        // side can understand.
        for (unsigned int i = 0; i < nArgs; i++)
        {
            hr = GatherArgInfo(rgpArgs[i], &(pArgData[i]));

            if (FAILED(hr))
            {
                delete [] pArgData;
                return hr;
            }
        }
    }

    // Send over to the left side and get it to setup this eval.
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event, DB_IPCE_FUNC_EVAL, true, m_thread->GetAppDomain()->GetADToken());

    event.FuncEval.vmThreadToken = m_thread->m_vmThreadToken;
    event.FuncEval.funcEvalType = m_evalType;
    event.FuncEval.funcMetadataToken = m_function->GetMetadataToken();
    event.FuncEval.vmDomainAssembly = m_function->GetModule()->GetRuntimeDomainAssembly();
    event.FuncEval.funcEvalKey = hFuncEval.Ptr();
    event.FuncEval.argCount = nArgs;
    event.FuncEval.genericArgsCount = nTypeArgs;

    hr = SendFuncEval(nTypeArgs,
                      rgpTypeArgs,
                      reinterpret_cast<void *>(pArgData),
                      sizeof(DebuggerIPCE_FuncEvalArgData) * nArgs,
                      NULL,
                      0,
                      &event);

    // Cleanup

    if (pArgData)
    {
        delete [] pArgData;
    }

    if (SUCCEEDED(hr))
    {
        hFuncEval.SuppressRelease(); // Now LS owns.
    }


    // Return any failure the Left Side may have told us about.
    return hr;
}

HRESULT CordbEval::NewObjectNoConstructor(ICorDebugClass *pClass)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return NewParameterizedObjectNoConstructor(pClass,0,NULL);
}

//---------------------------------------------------------------------------------------
//
// This routine creates an object of a certain type, but does not call the constructor
// for the type on the object.
//
// Arguments:
//    pClass - the type of the object to create.
//    nTypeArgs - The number of type-arguments for the method in rgpTypeArgs
//    rgpTypeArgs - An array of pointers to types.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbEval::NewParameterizedObjectNoConstructor(ICorDebugClass * pClass,
                                                       ULONG32 nTypeArgs,
                                                       ICorDebugType * rgpTypeArgs[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pClass, ICorDebugClass *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // The LS will assume that all of the ICorDebugTypes are in
    // the same appdomain as the class.  Verify this.
    CordbAppDomain * pClassAppDomain = (static_cast<CordbClass *> (pClass))->GetAppDomain();

    if (!DoAppDomainsMatch(pClassAppDomain, nTypeArgs, rgpTypeArgs, 0, NULL))
    {
        return ErrWrapper(CORDBG_E_APPDOMAIN_MISMATCH);
    }

    // Callers are free to reuse an ICorDebugEval object for multiple
    // evals. Since we create a Left Side eval representation each
    // time, we need to be sure to clean it up now that we know we're
    // done with it.
    HRESULT hr = SendCleanup();

    if (FAILED(hr))
    {
        return hr;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    RsPtrHolder<CordbEval> hFuncEval(this);
    lockHolder.Release(); // release to send an IPC event.

    if (hFuncEval.Ptr().IsNull())
    {
        return E_OUTOFMEMORY;
    }

    // Remember the function that we're evaluating.
    m_class = (CordbClass*)pClass;
    m_evalType = DB_IPCE_FET_NEW_OBJECT_NC;

    // Send over to the left side and get it to setup this eval.
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event, DB_IPCE_FUNC_EVAL, true, m_thread->GetAppDomain()->GetADToken());

    event.FuncEval.vmThreadToken = m_thread->m_vmThreadToken;
    event.FuncEval.funcEvalType = m_evalType;
    event.FuncEval.funcMetadataToken = mdMethodDefNil;
    event.FuncEval.funcClassMetadataToken = (mdTypeDef)m_class->m_id;
    event.FuncEval.vmDomainAssembly = m_class->GetModule()->GetRuntimeDomainAssembly();
    event.FuncEval.funcEvalKey = hFuncEval.Ptr();
    event.FuncEval.argCount = 0;
    event.FuncEval.genericArgsCount = nTypeArgs;

    hr = SendFuncEval(nTypeArgs, rgpTypeArgs, NULL, 0, NULL, 0, &event);

    if (SUCCEEDED(hr))
    {
        hFuncEval.SuppressRelease(); // Now LS owns.
    }

    // Return any failure the Left Side may have told us about.
    return hr;
}

/*
 *
 * NewString
 *
 *  This routine is the interface function for ICorDebugEval::NewString
 *
 * Parameters:
 *  string - the string to create - must be null-terminated
 *
 * Return Value:
 *  HRESULT from the helper routines on RS and LS.
 *
 */
HRESULT CordbEval::NewString(LPCWSTR string)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    return NewStringWithLength(string, (UINT)u16_strlen(string));
}

//---------------------------------------------------------------------------------------
//
// This routine is the interface function for ICorDebugEval::NewStringWithLength.
//
// Arguments:
//    wszString - the string to create
//    iLength - the number of characters that you want to create. Can include embedded nulls.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbEval::NewStringWithLength(LPCWSTR wszString, UINT iLength)
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(wszString, LPCWSTR); // Gotta have a string...
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    // Callers are free to reuse an ICorDebugEval object for multiple
    // evals. Since we create a Left Side eval representation each
    // time, we need to be sure to clean it up now that we know we're
    // done with it.
    HRESULT hr = SendCleanup();

    if (FAILED(hr))
    {
        return hr;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    RsPtrHolder<CordbEval> hFuncEval(this);
    lockHolder.Release(); // release to send an IPC event.

    if (hFuncEval.Ptr().IsNull())
    {
        return E_OUTOFMEMORY;
    }


    // Length of the string? Don't account for null as COMString::NewString is length-based
    SIZE_T cbString = iLength * sizeof(WCHAR);

    // Remember that we're doing a func eval for a new string.
    m_function = NULL;
    m_evalType = DB_IPCE_FET_NEW_STRING;

    // Send over to the left side and get it to setup this eval.
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event, DB_IPCE_FUNC_EVAL, true, m_thread->GetAppDomain()->GetADToken());

    event.FuncEval.vmThreadToken = m_thread->m_vmThreadToken;
    event.FuncEval.funcEvalType = m_evalType;
    event.FuncEval.funcEvalKey = hFuncEval.Ptr();
    event.FuncEval.stringSize = cbString;

    // Note: no function or module here...
    event.FuncEval.funcMetadataToken = mdMethodDefNil;
    event.FuncEval.funcClassMetadataToken = mdTypeDefNil;
    event.FuncEval.vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
    event.FuncEval.argCount = 0;
    event.FuncEval.genericArgsCount = 0;
    event.FuncEval.genericArgsNodeCount = 0;

    hr = SendFuncEval(0, NULL, (void *)wszString, (unsigned int)cbString, NULL, 0, &event);

    if (SUCCEEDED(hr))
    {
        hFuncEval.SuppressRelease(); // Now LS owns.
    }

    // Return any failure the Left Side may have told us about.
    return hr;
}

HRESULT CordbEval::NewArray(CorElementType elementType,
                            ICorDebugClass *pElementClass,
                            ULONG32 rank,
                            ULONG32 dims[],
                            ULONG32 lowBounds[])
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pElementClass, ICorDebugClass *);

    // If you want a class, you gotta pass a class.
    if ((elementType == ELEMENT_TYPE_CLASS) && (pElementClass == NULL))
        return E_INVALIDARG;

    // If you want an array of objects, then why pass a class?
    if ((elementType == ELEMENT_TYPE_OBJECT) && (pElementClass != NULL))
        return E_INVALIDARG;

    // Arg check...
    if (elementType == ELEMENT_TYPE_VOID)
        return E_INVALIDARG;

    CordbType *typ;
    HRESULT hr = S_OK;
    hr = CordbType::MkUnparameterizedType(m_thread->GetAppDomain(), elementType, (CordbClass *) pElementClass, &typ);

    if (FAILED(hr))
        return hr;

    return NewParameterizedArray(typ, rank,dims,lowBounds);

}


//---------------------------------------------------------------------------------------
//
// This routine sets up a func-eval to create a new array of the given type.
//
// Arguments:
//     pElementType - The type of each element of the array.
//     rank - Rank of the array.
//     rgDimensions - Array of dimensions for the array.
//     rmLowBounds - Array of lower bounds on the array.
//
// Return Value:
//    HRESULT for the operation
//
HRESULT CordbEval::NewParameterizedArray(ICorDebugType * pElementType,
                                         ULONG32 rank,
                                         ULONG32 rgDimensions[],
                                         ULONG32 rgLowBounds[])
{
    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT_OR_NULL(pElementType, ICorDebugType *);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());


    // Callers are free to reuse an ICorDebugEval object for multiple evals. Since we create a Left Side eval
    // representation each time, we need to be sure to clean it up now that we know we're done with it.
    HRESULT hr = SendCleanup();

    if (FAILED(hr))
    {
        return hr;
    }

    // Arg check...
    if ((rank == 0) || (rgDimensions == NULL))
    {
        return E_INVALIDARG;
    }

    RSLockHolder lockHolder(GetProcess()->GetProcessLock());
    RsPtrHolder<CordbEval> hFuncEval(this);
    lockHolder.Release(); // release to send an IPC event.

    if (hFuncEval.Ptr().IsNull())
    {
        return E_OUTOFMEMORY;
    }


    // Remember that we're doing a func eval for a new string.
    m_function = NULL;
    m_evalType = DB_IPCE_FET_NEW_ARRAY;

    // Send over to the left side and get it to setup this eval.
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event, DB_IPCE_FUNC_EVAL, true, m_thread->GetAppDomain()->GetADToken());

    event.FuncEval.vmThreadToken = m_thread->m_vmThreadToken;
    event.FuncEval.funcEvalType = m_evalType;
    event.FuncEval.funcEvalKey = hFuncEval.Ptr();

    event.FuncEval.arrayRank = rank;

    // Note: no function or module here...
    event.FuncEval.funcMetadataToken = mdMethodDefNil;
    event.FuncEval.funcClassMetadataToken = mdTypeDefNil;
    event.FuncEval.vmDomainAssembly = VMPTR_DomainAssembly::NullPtr();
    event.FuncEval.argCount = 0;
    event.FuncEval.genericArgsCount = 1;

    // Prefast overflow sanity check.
    S_UINT32 allocSize = S_UINT32(rank) * S_UINT32(sizeof(SIZE_T));

    if (allocSize.IsOverflow())
    {
        return E_INVALIDARG;
    }

    // Just in case sizeof(SIZE_T) != sizeof(ULONG32)
    SIZE_T * rgDimensionsSizeT = reinterpret_cast<SIZE_T *>(_alloca(allocSize.Value()));

    for (unsigned int i = 0; i < rank; i++)
    {
        rgDimensionsSizeT[i] = rgDimensions[i];
    }

    ICorDebugType * rgpGenericArgs[1];

    rgpGenericArgs[0] = pElementType;

    // @dbgtodo  funceval : lower bounds were ignored in V1 - fix this.
    hr = SendFuncEval(1,
                      rgpGenericArgs,
                      reinterpret_cast<void *>(rgDimensionsSizeT),
                      rank * sizeof(SIZE_T),
                      NULL, // (void*)lowBounds,
                      0, // ((lowBounds == NULL) ? 0 : rank * sizeof(SIZE_T)),
                      &event);

    if (SUCCEEDED(hr))
    {
        hFuncEval.SuppressRelease(); // Now LS owns.
    }

    // Return any failure the Left Side may have told us about.
    return hr;
}

HRESULT CordbEval::IsActive(BOOL *pbActive)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(pbActive, BOOL *);

    *pbActive = (m_complete == true);
    return S_OK;
}

/*
 * This routine submits an abort request to the LS.
 *
 * Parameters:
 *     None.
 *
 * Returns:
 *     The HRESULT as returned by the LS.
 *
 */

HRESULT
CordbEval::Abort(
    void
    )
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);

    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());


    //
    // No need to abort if its already completed.
    //
    if (m_complete)
    {
        return S_OK;
    }


    //
    // Can't abort if its never even been started.
    //
    if (m_debuggerEvalKey == NULL)
    {
        return E_INVALIDARG;
    }

    CORDBRequireProcessStateOK(m_thread->GetProcess());

    //
    // Send over to the left side to get the eval aborted.
    //
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event,
                                         DB_IPCE_FUNC_EVAL_ABORT,
                                         true,
                                         m_thread->GetAppDomain()->GetADToken()
                                        );

    event.FuncEvalAbort.debuggerEvalKey = m_debuggerEvalKey;

    HRESULT hr = m_thread->GetProcess()->SendIPCEvent(&event,
                                                      sizeof(DebuggerIPCEvent)
                                                     );


    //
    // If the send failed, return that failure.
    //
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(event.type == DB_IPCE_FUNC_EVAL_ABORT_RESULT);

    //
    // Since we may have
    // overwritten anything (objects, code, etc), we should mark
    // everything as needing to be re-cached.
    //
    m_thread->GetProcess()->m_continueCounter++;

    hr = event.hr;

    return hr;
}

HRESULT CordbEval::GetResult(ICorDebugValue **ppResult)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppResult, ICorDebugValue **);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    *ppResult = NULL;

    // Is the evaluation complete?
    if (!m_complete)
    {
        return CORDBG_E_FUNC_EVAL_NOT_COMPLETE;
    }

    if (m_aborted)
    {
        return CORDBG_S_FUNC_EVAL_ABORTED;
    }

    // Does the evaluation have a result?
    if (m_resultType.elementType == ELEMENT_TYPE_VOID)
    {
        return CORDBG_S_FUNC_EVAL_HAS_NO_RESULT;
    }

    HRESULT hr = S_OK;
    EX_TRY
    {
        // Make a ICorDebugValue out of the result.
        CordbAppDomain * pAppDomain;

        if (!m_resultAppDomainToken.IsNull())
        {
            // @dbgtodo  funceval - push this up
            RSLockHolder lockHolder(GetProcess()->GetProcessLock());

            pAppDomain = m_thread->GetProcess()->LookupOrCreateAppDomain(m_resultAppDomainToken);
        }
        else
        {
            pAppDomain = m_thread->GetAppDomain();
        }
        PREFIX_ASSUME(pAppDomain != NULL);

        CordbType * pType = NULL;
        hr = CordbType::TypeDataToType(pAppDomain, &m_resultType, &pType);
        IfFailThrow(hr);

        bool resultInHandle =
            ((m_resultType.elementType == ELEMENT_TYPE_CLASS) ||
            (m_resultType.elementType == ELEMENT_TYPE_SZARRAY) ||
            (m_resultType.elementType == ELEMENT_TYPE_OBJECT) ||
            (m_resultType.elementType == ELEMENT_TYPE_ARRAY) ||
            (m_resultType.elementType == ELEMENT_TYPE_STRING));

        if (resultInHandle)
        {
            // if object handle is null here, something has gone wrong!!!
            _ASSERTE(!m_vmObjectHandle.IsNull());

            if (m_pHandleValue == NULL)
            {
                // Create CordbHandleValue for result
                RSInitHolder<CordbHandleValue> pHandleValue(new CordbHandleValue(pAppDomain, pType, HANDLE_STRONG));

                // Initialize the handle value object. The HandleValue will now
                // own the m_objectHandle.
                hr = pHandleValue->Init(m_vmObjectHandle);

                if (!SUCCEEDED(hr))
                {
                    // Neuter the new object we've been working on. This will
                    // call Dispose(), and that will go back to the left side
                    // and free the handle that we got above.
                    pHandleValue->NeuterLeftSideResources();

                    //

                    // Do not delete chv here.  The neuter list still has a reference to it, and it will be cleaned up automatically.
                    ThrowHR(hr);
                }
                m_pHandleValue.Assign(pHandleValue);
                pHandleValue.ClearAndMarkDontNeuter();
            }

            // This AddRef is for caller to release
            //
            *ppResult = m_pHandleValue;
            m_pHandleValue->ExternalAddRef();
        }
        else if (CorIsPrimitiveType(m_resultType.elementType) && (m_resultType.elementType != ELEMENT_TYPE_STRING))
        {
            // create a CordbGenericValue flagged as a literal
            hr = CordbEval::CreatePrimitiveLiteral(pType, ppResult);
        }
        else
        {
            TargetBuffer remoteValue(m_resultAddr, CordbValue::GetSizeForType(pType, kBoxed));
            // Now that we have the module, go ahead and create the result.

            CordbValue::CreateValueByType(pAppDomain,
                                          pType,
                                          true,
                                          remoteValue,
                                          MemoryRange(NULL, 0),
                                          NULL,
                                          ppResult);  // throws
        }

    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbEval::GetThread(ICorDebugThread **ppThread)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    VALIDATE_POINTER_TO_OBJECT(ppThread, ICorDebugThread **);

    *ppThread = static_cast<ICorDebugThread*> (m_thread);
    m_thread->ExternalAddRef();

    return S_OK;
}

// Create a RS literal for primitive type funceval result. In case the result is used as an argument for
// another funceval, we need to make sure that we're not relying on the LS value, which will be freed and
// thus unavailable.
// Arguments:
//     input:  pType   - CordbType instance representing the type of the primitive value
//     output: ppValue - ICorDebugValue representing the result as a literal CordbGenericValue
// Return Value:
//     hr: may fail for OOM, ReadProcessMemory failures
HRESULT CordbEval::CreatePrimitiveLiteral(CordbType *   pType,
                                          ICorDebugValue ** ppValue)
{
    CordbGenericValue * gv = NULL;
    HRESULT hr = S_OK;
    EX_TRY
    {
        // Create a generic value.
        gv = new CordbGenericValue(pType);

        // initialize the local value
        int size = CordbValue::GetSizeForType(pType, kBoxed);
        if (size > 8)
        {
            ThrowHR(HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER));
        }
        TargetBuffer remoteValue(m_resultAddr, size);
        BYTE localBuffer[8] = {0};

        GetProcess()->SafeReadBuffer (remoteValue, localBuffer);
        gv->SetValue(localBuffer);

        // Do not delete gv here even if the initialization fails.
        // The neuter list still has a reference to it, and it will be cleaned up automatically.
        gv->ExternalAddRef();
        *ppValue = (ICorDebugValue*)(ICorDebugGenericValue*)gv;
    }
    EX_CATCH_HRESULT(hr);
    return hr;
}

HRESULT CordbEval::CreateValue(CorElementType elementType,
                               ICorDebugClass *pElementClass,
                               ICorDebugValue **ppValue)
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    CordbType *typ;

    // @todo: only primitive values right now.
    if (((elementType < ELEMENT_TYPE_BOOLEAN) ||
         (elementType > ELEMENT_TYPE_R8)) &&
        !(elementType == ELEMENT_TYPE_CLASS))
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    // MkUnparameterizedType now works if you give it ELEMENT_TYPE_CLASS and
    // a null pElementClass - it returns the type for ELEMENT_TYPE_OBJECT.

    hr = CordbType::MkUnparameterizedType(m_thread->GetAppDomain(), elementType, (CordbClass *) pElementClass, &typ);

    if (FAILED(hr))
        return hr;

    return CreateValueForType(typ, ppValue);
}

// create an ICDValue to represent a value for a funceval
// Arguments:
//     input:  pIType  - the type for the new value
//     output: ppValue - the new ICDValue. If there is a failure of some sort, this will be NULL
// ReturnValue: S_OK on success (ppValue should contain a non-NULL address)
//              E_OUTOFMEMORY, if we can't allocate space for the new ICDValue
// Notes: We can also get read process memory errors or E_INVALIDARG if errors occur during initialization,
// but in that case, we don't return the hresult. Instead, we just never update ppValue, so it will still be
// NULL on exit.
HRESULT CordbEval::CreateValueForType(ICorDebugType *   pIType,
                                      ICorDebugValue ** ppValue)
{
    HRESULT hr = S_OK;

    PUBLIC_REENTRANT_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);
    ATT_REQUIRE_STOPPED_MAY_FAIL(GetProcess());

    VALIDATE_POINTER_TO_OBJECT(ppValue, ICorDebugValue **);
    VALIDATE_POINTER_TO_OBJECT(pIType, ICorDebugType*);

    *ppValue = NULL;
    CordbType *pType = static_cast<CordbType *> (pIType);

    CorElementType elementType = pType->m_elementType;
    // We don't support IntPtr and UIntPtr types as arguments here, but we do support these types as results
    // (see code:CordbEval::CreatePrimitiveLiteral) and we have changed the LS to support them as well
    if (((elementType < ELEMENT_TYPE_BOOLEAN) ||
         (elementType > ELEMENT_TYPE_R8)) &&
        !((elementType == ELEMENT_TYPE_CLASS) || (elementType == ELEMENT_TYPE_OBJECT)))
        return E_INVALIDARG;

    // Note: ELEMENT_TYPE_OBJECT is what we'll get for the null reference case, so allow that.
    if ((elementType == ELEMENT_TYPE_CLASS) || (elementType == ELEMENT_TYPE_OBJECT))
    {
        EX_TRY
        {
            // create a reference value
            CordbReferenceValue *rv = new CordbReferenceValue(pType);

            if (SUCCEEDED(rv->InitRef(MemoryRange(NULL,0))))
            {
                // Do not delete rv here even if the initialization fails.
                // The neuter list still has a reference to it, and it will be cleaned up automatically.
                rv->ExternalAddRef();
                *ppValue = (ICorDebugValue*)(ICorDebugReferenceValue*)rv;
            }
        }
        EX_CATCH_HRESULT(hr);
    }
    else
    {
        CordbGenericValue * gv = NULL;
        EX_TRY
        {
            // Create a generic value.
            gv = new CordbGenericValue(pType);

            gv->Init(MemoryRange(NULL,0));
            // Do not delete gv here even if the initialization fails.
            // The neuter list still has a reference to it, and it will be cleaned up automatically.
            gv->ExternalAddRef();
            *ppValue = (ICorDebugValue*)(ICorDebugGenericValue*)gv;
        }
        EX_CATCH_HRESULT(hr);
    }

    return hr;
} // CordbEval::CreateValueForType


/* ------------------------------------------------------------------------- *
 * CordbEval2
 *
 *   Extensions to the CordbEval class for Whidbey
 *
 * ------------------------------------------------------------------------- */


/*
 * This routine submits a rude abort request to the LS.
 *
 * Parameters:
 *     None.
 *
 * Returns:
 *     The HRESULT as returned by the LS.
 *
 */

HRESULT
CordbEval::RudeAbort(
    void
    )
{
    PUBLIC_API_ENTRY(this);
    FAIL_IF_NEUTERED(this);


    ATT_ALLOW_LIVE_DO_STOPGO(GetProcess());

    //
    // No need to abort if its already completed.
    //
    if (m_complete)
    {
        return S_OK;
    }

    //
    // Can't abort if its never even been started.
    //
    if (m_debuggerEvalKey == NULL)
    {
        return E_INVALIDARG;
    }

    CORDBRequireProcessStateOK(m_thread->GetProcess());

    //
    // Send over to the left side to get the eval aborted.
    //
    DebuggerIPCEvent event;

    m_thread->GetProcess()->InitIPCEvent(&event,
                                         DB_IPCE_FUNC_EVAL_RUDE_ABORT,
                                         true,
                                         m_thread->GetAppDomain()->GetADToken()
                                        );

    event.FuncEvalRudeAbort.debuggerEvalKey = m_debuggerEvalKey;

    HRESULT hr = m_thread->GetProcess()->SendIPCEvent(&event,
                                                      sizeof(DebuggerIPCEvent)
                                                     );

    //
    // If the send failed, return that failure.
    //
    if (FAILED(hr))
    {
        return hr;
    }

    _ASSERTE(event.type == DB_IPCE_FUNC_EVAL_RUDE_ABORT_RESULT);

    //
    // Since we may have
    // overwritten anything (objects, code, etc), we should mark
    // everything as needing to be re-cached.
    //
    m_thread->GetProcess()->m_continueCounter++;

    hr = event.hr;

    return hr;
}




/* ------------------------------------------------------------------------- *
 * CodeParameter Enumerator class
 * ------------------------------------------------------------------------- */

CordbCodeEnum::CordbCodeEnum(unsigned int cCodes, RSSmartPtr<CordbCode> * ppCodes) :
    CordbBase(NULL, 0)
{
    // Because the array is of smart-ptrs, the elements are already reffed
    // We now take ownership of the array itself too.
    m_ppCodes = ppCodes;

    m_iCurrent = 0;
    m_iMax = cCodes;
}


CordbCodeEnum::~CordbCodeEnum()
{
    // This will invoke the SmartPtr dtors on each element and call release.
    delete [] m_ppCodes;
}

HRESULT CordbCodeEnum::QueryInterface(REFIID id, void **pInterface)
{
    if (id == IID_ICorDebugEnum)
        *pInterface = static_cast<ICorDebugEnum*>(this);
    else if (id == IID_ICorDebugCodeEnum)
        *pInterface = static_cast<ICorDebugCodeEnum*>(this);
    else if (id == IID_IUnknown)
        *pInterface = static_cast<IUnknown*>(static_cast<ICorDebugCodeEnum*>(this));
    else
    {
        *pInterface = NULL;
        return E_NOINTERFACE;
    }

    ExternalAddRef();
    return S_OK;
}

HRESULT CordbCodeEnum::Skip(ULONG celt)
{
    HRESULT hr = E_FAIL;
    if ( (m_iCurrent+celt) < m_iMax ||
         celt == 0)
    {
        m_iCurrent += celt;
        hr = S_OK;
    }

    return hr;
}

HRESULT CordbCodeEnum::Reset()
{
    m_iCurrent = 0;
    return S_OK;
}

HRESULT CordbCodeEnum::Clone(ICorDebugEnum **ppEnum)
{
    VALIDATE_POINTER_TO_OBJECT(ppEnum, ICorDebugEnum **);
    (*ppEnum) = NULL;

    HRESULT hr = S_OK;

    // Create a new copy of the array because the CordbCodeEnum will
    // take ownership of it.
    RSSmartPtr<CordbCode> * ppCodes = new (nothrow) RSSmartPtr<CordbCode> [m_iMax];
    if (ppCodes == NULL)
    {
        return E_OUTOFMEMORY;
    }
    for(UINT i = 0; i < m_iMax; i++)
    {
        ppCodes[i].Assign(m_ppCodes[i]);
    }


    CordbCodeEnum *pCVE = new (nothrow) CordbCodeEnum( m_iMax, ppCodes);
    if ( pCVE == NULL )
    {
        delete [] ppCodes;
        hr = E_OUTOFMEMORY;
        goto LExit;
    }

    pCVE->ExternalAddRef();
    (*ppEnum) = (ICorDebugEnum*)pCVE;

LExit:
    return hr;
}

HRESULT CordbCodeEnum::GetCount(ULONG *pcelt)
{
    VALIDATE_POINTER_TO_OBJECT(pcelt, ULONG *);

    if( pcelt == NULL)
        return E_INVALIDARG;

    (*pcelt) = m_iMax;
    return S_OK;
}

//
// In the event of failure, the current pointer will be left at
// one element past the troublesome element.  Thus, if one were
// to repeatedly ask for one element to iterate through the
// array, you would iterate exactly m_iMax times, regardless
// of individual failures.
HRESULT CordbCodeEnum::Next(ULONG celt, ICorDebugCode *values[], ULONG *pceltFetched)
{
    VALIDATE_POINTER_TO_OBJECT_ARRAY(values, ICorDebugClass *,
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

    int iMax = min( m_iMax, m_iCurrent+celt);
    int i;

    for (i = m_iCurrent; i < iMax; i++)
    {
        values[i-m_iCurrent] = m_ppCodes[i];
        values[i-m_iCurrent]->AddRef();
    }

    int count = (i - m_iCurrent);

    if ( FAILED( hr ) )
    {   //we failed: +1 pushes us past troublesome element
        m_iCurrent += 1 + count;
    }
    else
    {
        m_iCurrent += count;
    }

    if (pceltFetched != NULL)
    {
        *pceltFetched = count;
    }

    //
    // If we reached the end of the enumeration, but not the end
    // of the number of requested items, we return S_FALSE.
    //
    if (((ULONG)count) < celt)
    {
        return S_FALSE;
    }

    return hr;
}
