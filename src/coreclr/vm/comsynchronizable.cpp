// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/*============================================================
**
** Header: COMSynchronizable.cpp
**
** Purpose: Native methods on System.SynchronizableObject
**          and its subclasses.
**
**
===========================================================*/

#include "common.h"

#include <object.h>
#include "threads.h"
#include "excep.h"
#include "vars.hpp"
#include "field.h"
#include "comsynchronizable.h"
#include "dbginterface.h"
#include "comdelegate.h"
#include "eeconfig.h"
#include "callhelpers.h"
#include "appdomain.hpp"
#include "appdomain.inl"

#ifndef TARGET_UNIX
#include "utilcode.h"
#endif


// For the following helpers, we make no attempt to synchronize.  The app developer
// is responsible for managing their own race conditions.
//
// Note: if the internal Thread is NULL, this implies that the exposed object has
//       finalized and then been resurrected.
static inline BOOL ThreadNotStarted(Thread *t)
{
    WRAPPER_NO_CONTRACT;
    return (t && t->IsUnstarted());
}

static inline BOOL ThreadIsDead(Thread *t)
{
    WRAPPER_NO_CONTRACT;
    return (t == 0 || t->IsDead());
}


// Map our exposed notion of thread priorities into the enumeration that NT uses.
static INT32 MapToNTPriority(INT32 ours)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    switch (ours)
    {
    case ThreadNative::PRIORITY_LOWEST:
        return THREAD_PRIORITY_LOWEST;

    case ThreadNative::PRIORITY_BELOW_NORMAL:
        return THREAD_PRIORITY_BELOW_NORMAL;

    case ThreadNative::PRIORITY_NORMAL:
        return THREAD_PRIORITY_NORMAL;

    case ThreadNative::PRIORITY_ABOVE_NORMAL:
        return THREAD_PRIORITY_ABOVE_NORMAL;

    case ThreadNative::PRIORITY_HIGHEST:
        return THREAD_PRIORITY_HIGHEST;

    default:
        COMPlusThrow(kArgumentOutOfRangeException, W("Argument_InvalidFlag"));
    }
}

// Map to our exposed notion of thread priorities from the enumeration that NT uses.
INT32 MapFromNTPriority(INT32 NTPriority)
{
    LIMITED_METHOD_CONTRACT;

    INT32   ours = 0;

    if (NTPriority <= THREAD_PRIORITY_LOWEST)
    {
        // managed code does not support IDLE.  Map it to PRIORITY_LOWEST.
        ours = ThreadNative::PRIORITY_LOWEST;
    }
    else if (NTPriority >= THREAD_PRIORITY_HIGHEST)
    {
        ours = ThreadNative::PRIORITY_HIGHEST;
    }
    else if (NTPriority == THREAD_PRIORITY_BELOW_NORMAL)
    {
        ours = ThreadNative::PRIORITY_BELOW_NORMAL;
    }
    else if (NTPriority == THREAD_PRIORITY_NORMAL)
    {
        ours = ThreadNative::PRIORITY_NORMAL;
    }
    else if (NTPriority == THREAD_PRIORITY_ABOVE_NORMAL)
    {
        ours = ThreadNative::PRIORITY_ABOVE_NORMAL;
    }
    else
    {
        _ASSERTE (!"not supported priority");
        ours = ThreadNative::PRIORITY_NORMAL;
    }
    return ours;
}

static void KickOffThread_Worker(LPVOID ptr)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
        PRECONDITION(ptr == NULL);
    }
    CONTRACTL_END;

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__THREAD__START_CALLBACK);
    DECLARE_ARGHOLDER_ARRAY(args, 1);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(GetThread()->GetExposedObjectRaw());

    CALL_MANAGED_METHOD_NORET(args);
}

// Helper to avoid two EX_TRY/EX_CATCH blocks in one function
static void PulseAllHelper(Thread* pThread)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        DISABLED(NOTHROW);
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    EX_TRY
    {
        // GetExposedObject() will either throw, or we have a valid object.  Note
        // that we re-acquire it each time, since it may move during calls.
        pThread->GetExposedObject()->EnterObjMonitor();
        pThread->GetExposedObject()->PulseAll();
        pThread->GetExposedObject()->LeaveObjMonitor();
    }
    EX_CATCH
    {
        // just keep going...
    }
    EX_END_CATCH(SwallowAllExceptions)
}

// When an exposed thread is started by Win32, this is where it starts.
static ULONG WINAPI KickOffThread(void* pass)
{

    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_ANY;
    }
    CONTRACTL_END;

    Thread* pThread = (Thread*)pass;
    _ASSERTE(pThread != NULL);

    if (pThread->HasStarted())
    {
        // Do not swallow the unhandled exception here
        //

        // Fire ETW event to correlate with the thread that created current thread
        if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadRunning))
            FireEtwThreadRunning(pThread, GetClrInstanceId());

        // We have a sticky problem here.
        //
        // Under some circumstances, the context of 'this' doesn't match the context
        // of the thread.  Today this can only happen if the thread is marked for an
        // STA.  If so, the delegate that is stored in the object may not be directly
        // suitable for invocation.  Instead, we need to call through a proxy so that
        // the correct context transitions occur.
        //
        // All the changes occur inside HasStarted(), which will switch this thread
        // over to a brand new STA as necessary.  We have to notice this happening, so
        // we can adjust the delegate we are going to invoke on.

        _ASSERTE(GetThread() == pThread);        // Now that it's started

        ManagedThreadBase::KickOff(KickOffThread_Worker, NULL);

        PulseAllHelper(pThread);

        GCX_PREEMP_NO_DTOR();

        pThread->ClearThreadCPUGroupAffinity();

        DestroyThread(pThread);
    }

    return 0;
}

extern "C" void QCALLTYPE ThreadNative_Start(QCall::ThreadHandle thread, int threadStackSize, int priority, BOOL isThreadPool, PCWSTR pThreadName)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    Thread* pNewThread = thread;
    _ASSERTE(pNewThread != NULL);

    // Is the thread already started?  You can't restart a thread.
    if (!ThreadNotStarted(pNewThread))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_AlreadyStarted"));
    }

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // Attempt to eagerly set the apartment state during thread startup.
    Thread::ApartmentState as = pNewThread->GetApartmentOfUnstartedThread();
    if (as == Thread::AS_Unknown)
    {
        pNewThread->SetApartmentOfUnstartedThread(Thread::AS_InMTA);
    }
#endif

    pNewThread->IncExternalCount();

    // Fire an ETW event to mark the current thread as the launcher of the new thread
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, ThreadCreating))
        FireEtwThreadCreating(pNewThread, GetClrInstanceId());

    // As soon as we create the new thread, it is eligible for suspension, etc.
    // So it gets transitioned to cooperative mode before this call returns to
    // us.  It is our duty to start it running immediately, so that GC isn't blocked.

    BOOL success = pNewThread->CreateNewThread(
                                    threadStackSize /* 0 stackSize override*/,
                                    KickOffThread, pNewThread, pThreadName);

    if (!success)
    {
        pNewThread->DecExternalCount(FALSE);
        COMPlusThrowOM();
    }

    // After we have established the thread handle, we can check m_Priority.
    // This ordering is required to eliminate the race condition on setting the
    // priority of a thread just as it starts up.
    INT32 NTPriority = MapToNTPriority(priority);

    pNewThread->SetThreadPriority(NTPriority);
    pNewThread->ChooseThreadCPUGroupAffinity();

    pNewThread->SetThreadState(Thread::TS_LegalToJoin);
    if (isThreadPool)
        pNewThread->SetIsThreadPoolThread();

    DWORD ret = pNewThread->StartThread();

    // When running under a user mode native debugger there is a race
    // between the moment we've created the thread (in CreateNewThread) and
    // the moment we resume it (in StartThread); the debugger may receive
    // the "ct" (create thread) notification, and it will attempt to
    // suspend/resume all threads in the process.  Now imagine the debugger
    // resumes this thread first, and only later does it try to resume the
    // newly created thread.  In these conditions our call to ResumeThread
    // may come before the debugger's call to ResumeThread actually causing
    // ret to equal 2.
    // We cannot use IsDebuggerPresent() in the condition below because the
    // debugger may have been detached between the time it got the notification
    // and the moment we execute the test below.
    _ASSERTE(ret == 1 || ret == 2);

    // Synchronize with HasStarted.
    YIELD_WHILE (!pNewThread->HasThreadState(Thread::TS_FailStarted) &&
            pNewThread->HasThreadState(Thread::TS_Unstarted));

    if (pNewThread->HasThreadState(Thread::TS_FailStarted))
    {
        GCX_COOP();

        PulseAllHelper(pNewThread);
        pNewThread->HandleThreadStartupFailure();
    }

    END_QCALL;
}

extern "C" void QCALLTYPE ThreadNative_SetPriority(QCall::ObjectHandleOnStack thread, INT32 iPriority)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    THREADBASEREF threadRef = NULL;
    GCPROTECT_BEGIN(threadRef)
    threadRef = (THREADBASEREF)thread.Get();

    if (threadRef == NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    // Note that you can manipulate the priority of a thread that hasn't started yet,
    // or one that is running. But you get an exception if you manipulate the priority
    // of a thread that has died.
    Thread* th = threadRef->GetInternal();
    if (ThreadIsDead(th))
        COMPlusThrow(kThreadStateException, W("ThreadState_Dead_Priority"));

    // translate the priority (validating as well)
    INT32 priority = MapToNTPriority(iPriority);

    INT32 oldPriority = threadRef->GetPriority();

    // Eliminate the race condition by setting priority field before we check for if
    // the thread is running. See ThreadNative::Start() for the other half.
    threadRef->SetPriority(iPriority);

    if (!th->SetThreadPriority(priority))
    {
        threadRef->SetPriority(oldPriority);
        COMPlusThrow(kThreadStateException, W("ThreadState_SetPriorityFailed"));
    }

    GCPROTECT_END();
    END_QCALL;
}

extern "C" void QCALLTYPE ThreadNative_GetCurrentThread(QCall::ObjectHandleOnStack thread)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    thread.Set(GetThread()->GetExposedObject());

    END_QCALL;
}

extern "C" UINT64 QCALLTYPE ThreadNative_GetCurrentOSThreadId()
{
    QCALL_CONTRACT;

    // The Windows API GetCurrentThreadId returns a 32-bit integer thread ID.
    // On some non-Windows platforms (e.g. OSX), the thread ID is a 64-bit value.
    // We special case the API for non-Windows to get the 64-bit value and zero-extend
    // the Windows value to return a single data type on all platforms.

    UINT64 threadId = 0;

    BEGIN_QCALL;
#ifndef TARGET_UNIX
    threadId = (UINT64) GetCurrentThreadId();
#else
    threadId = (UINT64) PAL_GetCurrentOSThreadId();
#endif
    END_QCALL;

    return threadId;
}

extern "C" void QCALLTYPE ThreadNative_Initialize(QCall::ObjectHandleOnStack t)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    THREADBASEREF threadRef = NULL;
    GCPROTECT_BEGIN(threadRef)
    threadRef = (THREADBASEREF)t.Get();

    _ASSERTE(threadRef != NULL);
    _ASSERTE(threadRef->GetInternal() == NULL);

    // if we don't have an internal Thread object associated with this exposed object,
    // now is our first opportunity to create one.
    Thread* unstarted = SetupUnstartedThread();
    PREFIX_ASSUME(unstarted != NULL);

    threadRef->SetInternal(unstarted);
    threadRef->SetManagedThreadId(unstarted->GetThreadId());
    unstarted->SetExposedObject(threadRef);

    // Initialize the thread priority to normal.
    threadRef->SetPriority(ThreadNative::PRIORITY_NORMAL);

    GCPROTECT_END();
    END_QCALL;
}

// Deliver the state of the thread as a consistent set of bits.
// Duplicate logic in DacDbiInterfaceImpl::GetPartialUserState()
extern "C" INT32 QCALLTYPE ThreadNative_GetThreadState(QCall::ThreadHandle thread)
{
    CONTRACTL
    {
        QCALL_CHECK_NO_GC_TRANSITION;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    INT32 res = 0;

    // grab a snapshot
    Thread::ThreadState state = thread->GetSnapshotState();

    if (state & Thread::TS_Background)
        res |= ThreadNative::ThreadBackground;

    if (state & Thread::TS_Unstarted)
        res |= ThreadNative::ThreadUnstarted;

    // Don't report a StopRequested if the thread has actually stopped.
    if (state & Thread::TS_Dead)
    {
        res |= ThreadNative::ThreadStopped;
    }
    else
    {
        if (state & Thread::TS_AbortRequested)
            res |= ThreadNative::ThreadAbortRequested;
    }

    if (state & Thread::TS_Interruptible)
        res |= ThreadNative::ThreadWaitSleepJoin;

    return res;
}

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

// Return whether the thread hosts an STA, is a member of the MTA or is not
// currently initialized for COM.
extern "C" INT32 QCALLTYPE ThreadNative_GetApartmentState(QCall::ObjectHandleOnStack t)
{
    QCALL_CONTRACT;

    INT32 retVal = 0;

    BEGIN_QCALL;

    Thread* thread = NULL;
    {
        GCX_COOP();
        THREADBASEREF threadRef = (THREADBASEREF)t.Get();
        if (threadRef == NULL)
            COMPlusThrow(kNullReferenceException, W("NullReference_This"));

        thread = threadRef->GetInternal();

        if (ThreadIsDead(thread))
            COMPlusThrow(kThreadStateException, W("ThreadState_Dead_State"));
    }

    retVal = thread->GetApartment();

#ifdef FEATURE_COMINTEROP
    if (retVal == Thread::AS_Unknown)
    {
        // If the CLR hasn't started COM yet, start it up and attempt the call again.
        // We do this in order to minimize the number of situations under which we return
        // ApartmentState.Unknown to our callers.
        if (!g_fComStarted)
        {
            EnsureComStarted();
            retVal = thread->GetApartment();
        }
    }
#endif // FEATURE_COMINTEROP

    END_QCALL;
    return retVal;
}

// Indicate whether the thread will host an STA (this may fail if the thread has
// already been made part of the MTA, use GetApartmentState or the return state
// from this routine to check for this).
extern "C" INT32 QCALLTYPE ThreadNative_SetApartmentState(QCall::ObjectHandleOnStack t, INT32 iState)
{
    QCALL_CONTRACT;

    INT32 retVal = 0;

    BEGIN_QCALL;

    Thread* thread = NULL;
    {
        GCX_COOP();
        THREADBASEREF threadRef = (THREADBASEREF)t.Get();
        if (threadRef == NULL)
            COMPlusThrow(kNullReferenceException, W("NullReference_This"));

        thread = threadRef->GetInternal();
    }

    // We can only change the apartment if the thread is unstarted or
    // running, and if it's running we have to be in the thread's
    // context.
    if (ThreadNotStarted(thread))
    {
        // Compat: Disallow resetting the initial apartment state
        if (thread->GetApartmentOfUnstartedThread() == Thread::AS_Unknown)
            thread->SetApartmentOfUnstartedThread((Thread::ApartmentState)iState);

        retVal = thread->GetApartmentOfUnstartedThread();
    }
    else
    {
        if (GetThread() != thread)
        {
            COMPlusThrow(kThreadStateException);
        }

        retVal = thread->SetApartment((Thread::ApartmentState)iState);
    }

    END_QCALL;
    return retVal;
}
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

void ReleaseThreadExternalCount(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    pThread->DecExternalCount(FALSE);
}

typedef Holder<Thread *, DoNothing, ReleaseThreadExternalCount> ThreadExternalCountHolder;

// Wait for the thread to die
static BOOL DoJoin(THREADBASEREF dyingThread, INT32 timeout)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(dyingThread != NULL);
        PRECONDITION((timeout >= 0) || (timeout == INFINITE_TIMEOUT));
    }
    CONTRACTL_END;

    Thread* DyingInternal = dyingThread->GetInternal();

    // Validate the handle.  It's valid to Join a thread that's not running -- so
    // long as it was once started.
    if (DyingInternal == NULL ||
        !(DyingInternal->m_State & Thread::TS_LegalToJoin))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_NotStarted"));
    }

    // Don't grab the handle until we know it has started, to eliminate the race
    // condition.
    if (ThreadIsDead(DyingInternal) || !DyingInternal->HasValidThreadHandle())
        return TRUE;

    // There is a race here. The Thread is going to close its thread handle.
    // If we grab the handle and then the Thread closes it, we will wait forever
    // in DoAppropriateWait.
    int RefCount = DyingInternal->IncExternalCount();
    if (RefCount == 1)
    {
        // !!! We resurrect the Thread Object.
        // !!! We will keep the Thread ref count to be 1 so that we will not try
        // !!! to destroy the Thread Object again.
        // !!! Do not call DecExternalCount here!
        _ASSERTE (!DyingInternal->HasValidThreadHandle());
        return TRUE;
    }

    ThreadExternalCountHolder dyingInternalHolder(DyingInternal);

    if (!DyingInternal->HasValidThreadHandle())
    {
        return TRUE;
    }

    GCX_PREEMP();
    DWORD dwTimeOut32 = (timeout == INFINITE_TIMEOUT
                   ? INFINITE
                   : (DWORD) timeout);

    DWORD rv = DyingInternal->JoinEx(dwTimeOut32, (WaitMode)(WaitMode_Alertable/*alertable*/|WaitMode_InDeadlock));
    switch(rv)
    {
        case WAIT_OBJECT_0:
            return TRUE;

        case WAIT_TIMEOUT:
            break;

        case WAIT_FAILED:
            if(!DyingInternal->HasValidThreadHandle())
                return TRUE;
            break;

        default:
            _ASSERTE(!"This return code is not understood \n");
            break;
    }

    return FALSE;
}

extern "C" BOOL QCALLTYPE ThreadNative_Join(QCall::ObjectHandleOnStack thread, INT32 Timeout)
{
    QCALL_CONTRACT;

    BOOL retVal = FALSE;

    BEGIN_QCALL;

    GCX_COOP();
    retVal = DoJoin((THREADBASEREF)thread.Get(), Timeout);

    END_QCALL;

    return retVal;
}

// If the exposed object is created after-the-fact, for an existing thread, we call
// InitExisting on it.  This is the other "construction", as opposed to SetDelegate.
void ThreadBaseObject::InitExisting()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    Thread *pThread = GetInternal();
    _ASSERTE (pThread);
    switch (pThread->GetThreadPriority())
    {
    case THREAD_PRIORITY_LOWEST:
    case THREAD_PRIORITY_IDLE:
        m_Priority = ThreadNative::PRIORITY_LOWEST;
        break;

    case THREAD_PRIORITY_BELOW_NORMAL:
        m_Priority = ThreadNative::PRIORITY_BELOW_NORMAL;
        break;

    case THREAD_PRIORITY_NORMAL:
        m_Priority = ThreadNative::PRIORITY_NORMAL;
        break;

    case THREAD_PRIORITY_ABOVE_NORMAL:
        m_Priority = ThreadNative::PRIORITY_ABOVE_NORMAL;
        break;

    case THREAD_PRIORITY_HIGHEST:
    case THREAD_PRIORITY_TIME_CRITICAL:
        m_Priority = ThreadNative::PRIORITY_HIGHEST;
        break;

    case THREAD_PRIORITY_ERROR_RETURN:
        _ASSERTE(FALSE);
        m_Priority = ThreadNative::PRIORITY_NORMAL;
        break;

    default:
        m_Priority = ThreadNative::PRIORITY_NORMAL;
        break;
    }
}

FCIMPL1(void, ThreadNative::Finalize, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    THREADBASEREF   refThis = (THREADBASEREF)pThisUNSAFE;
    Thread*         thread  = refThis->GetInternal();

    // Prevent multiple calls to Finalize
    // Objects can be resurrected after being finalized.  However, there is no
    // race condition here.  We always check whether an exposed thread object is
    // still attached to the internal Thread object, before proceeding.
    if (thread)
    {
        refThis->ResetStartHelper();

        if (GetThreadNULLOk() != thread)
        {
            refThis->ClearInternal();
        }

        thread->SetThreadState(Thread::TS_Finalized);
        Thread::SetCleanupNeededForFinalizedThread();
    }
}
FCIMPLEND

// Get whether or not this is a background thread.
extern "C" BOOL QCALLTYPE ThreadNative_GetIsBackground(QCall::ThreadHandle thread)
{
    CONTRACTL
    {
        QCALL_CHECK_NO_GC_TRANSITION;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    return thread->IsBackground();
}

// Set whether or not this is a background thread.
extern "C" void QCALLTYPE ThreadNative_SetIsBackground(QCall::ThreadHandle thread, BOOL value)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    thread->SetBackground(value);

    END_QCALL;
}

extern "C" void QCALLTYPE ThreadNative_InformThreadNameChange(QCall::ThreadHandle thread, LPCWSTR name, INT32 len)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    Thread* pThread = thread;

    // The name will show up in ETW traces and debuggers which is very helpful if more and more threads
    // get a meaningful name. Will also show up in Linux in gdb and such.
    if (len > 0 && name != NULL && pThread->GetThreadHandle() != INVALID_HANDLE_VALUE)
    {
        SetThreadName(pThread->GetThreadHandle(), name);
    }

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackThreads());
        if (name == NULL)
        {
            (&g_profControlBlock)->ThreadNameChanged((ThreadID)pThread, 0, NULL);
        }
        else
        {
            (&g_profControlBlock)->ThreadNameChanged((ThreadID)pThread, len, (WCHAR*)name);
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

#ifdef DEBUGGING_SUPPORTED
    if (CORDebuggerAttached())
    {
        _ASSERTE(NULL != g_pDebugInterface);
        g_pDebugInterface->NameChangeEvent(NULL, pThread);
    }
#endif // DEBUGGING_SUPPORTED

    END_QCALL;
}

FCIMPL0(INT32, ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
{
    FCALL_CONTRACT;

    return (INT32)YieldProcessorNormalization::GetOptimalMaxNormalizedYieldsPerSpinIteration();
}
FCIMPLEND

extern "C" void QCALLTYPE ThreadNative_SpinWait(INT32 iterations)
{
    FCALL_CONTRACT;

    if (iterations <= 0)
    {
        return;
    }

    YieldProcessorNormalized(iterations);
}

// This service can be called on unstarted and dead threads.  For unstarted ones, the
// next wait will be interrupted.  For dead ones, this service quietly does nothing.
extern "C" void QCALLTYPE ThreadNative_Interrupt(QCall::ThreadHandle thread)
{
    CONTRACTL
    {
        QCALL_CHECK;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    BEGIN_QCALL;

    thread->UserInterrupt(Thread::TI_Interrupt);

    END_QCALL;
}

extern "C" void QCALLTYPE ThreadNative_Sleep(INT32 iTime)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GetThread()->UserSleep(iTime);

    END_QCALL;
}

#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE ThreadNative_DisableComObjectEagerCleanup(QCall::ThreadHandle thread)
{
    CONTRACTL
    {
        QCALL_CHECK_NO_GC_TRANSITION;
        PRECONDITION(thread != NULL);
    }
    CONTRACTL_END;

    thread->SetDisableComObjectEagerCleanup();
}
#endif //FEATURE_COMINTEROP

extern "C" void QCALLTYPE ThreadNative_PollGC()
{
    // This is an intentional no-op.  The call is made to ensure that the thread goes through a GC transition
    // and is thus marked as a GC safe point, and that the p/invoke rare path will kick in
}

extern "C" BOOL QCALLTYPE ThreadNative_YieldThread()
{
    QCALL_CONTRACT;

    BOOL ret = FALSE;

    BEGIN_QCALL;

    ret = __SwitchToThread(0, CALLER_LIMITS_SPINNING);

    END_QCALL;

    return ret;
}

extern "C" void QCALLTYPE ThreadNative_Abort(QCall::ThreadHandle thread)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    thread->UserAbort(EEPolicy::TA_Safe, INFINITE);

    END_QCALL;
}

// Unmark the current thread for a safe abort.
extern "C" void QCALLTYPE ThreadNative_ResetAbort()
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    Thread *pThread = GetThread();
    if (pThread->IsAbortRequested())
    {
        pThread->UnmarkThreadForAbort(EEPolicy::TA_Safe);
    }
}
