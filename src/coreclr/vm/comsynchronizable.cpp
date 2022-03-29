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
    return (t && t->IsUnstarted() && !t->HasValidThreadHandle());
}

static inline BOOL ThreadIsRunning(Thread *t)
{
    WRAPPER_NO_CONTRACT;
    return (t &&
            (t->m_State & (Thread::TS_ReportDead|Thread::TS_Dead)) == 0 &&
            (t->HasValidThreadHandle()));
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

    INT32   NTPriority = 0;

    switch (ours)
    {
    case ThreadNative::PRIORITY_LOWEST:
        NTPriority = THREAD_PRIORITY_LOWEST;
        break;

    case ThreadNative::PRIORITY_BELOW_NORMAL:
        NTPriority = THREAD_PRIORITY_BELOW_NORMAL;
        break;

    case ThreadNative::PRIORITY_NORMAL:
        NTPriority = THREAD_PRIORITY_NORMAL;
        break;

    case ThreadNative::PRIORITY_ABOVE_NORMAL:
        NTPriority = THREAD_PRIORITY_ABOVE_NORMAL;
        break;

    case ThreadNative::PRIORITY_HIGHEST:
        NTPriority = THREAD_PRIORITY_HIGHEST;
        break;

    default:
        COMPlusThrow(kArgumentOutOfRangeException, W("Argument_InvalidFlag"));
    }
    return NTPriority;
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


void ThreadNative::KickOffThread_Worker(LPVOID ptr)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    KickOffThread_Args *pKickOffArgs = (KickOffThread_Args *) ptr;
    pKickOffArgs->retVal = 0;

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
ULONG WINAPI ThreadNative::KickOffThread(void* pass)
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

        KickOffThread_Args args;
        args.share = NULL;
        args.pThread = pThread;

        ManagedThreadBase::KickOff(KickOffThread_Worker, &args);

        PulseAllHelper(pThread);

        GCX_PREEMP_NO_DTOR();

        pThread->ClearThreadCPUGroupAffinity();

        DestroyThread(pThread);
    }

    return 0;
}

extern "C" void QCALLTYPE ThreadNative_Start(QCall::ThreadHandle thread, int threadStackSize, int priority, PCWSTR pThreadName)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ThreadNative::Start(thread, threadStackSize, priority, pThreadName);

    END_QCALL;
}

void ThreadNative::Start(Thread* pNewThread, int threadStackSize, int priority, PCWSTR pThreadName)
{
    _ASSERTE(pNewThread != NULL);

    // Is the thread already started?  You can't restart a thread.
    if (!ThreadNotStarted(pNewThread))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_AlreadyStarted"));
    }

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    // Attempt to eagerly set the apartment state during thread startup.
    Thread::ApartmentState as = pNewThread->GetExplicitApartment();
    if (as == Thread::AS_Unknown)
    {
        pNewThread->SetApartment(Thread::AS_InMTA);
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
    pNewThread->SetThreadPriority(MapToNTPriority(priority));
    pNewThread->ChooseThreadCPUGroupAffinity();

    FastInterlockOr((ULONG *) &pNewThread->m_State, Thread::TS_LegalToJoin);

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
}

// Note that you can manipulate the priority of a thread that hasn't started yet,
// or one that is running.  But you get an exception if you manipulate the priority
// of a thread that has died.
FCIMPL1(INT32, ThreadNative::GetPriority, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    // validate the handle
    if (ThreadIsDead(pThisUNSAFE->GetInternal()))
        FCThrowRes(kThreadStateException, W("ThreadState_Dead_Priority"));

    return pThisUNSAFE->m_Priority;
}
FCIMPLEND

FCIMPL2(void, ThreadNative::SetPriority, ThreadBaseObject* pThisUNSAFE, INT32 iPriority)
{
    FCALL_CONTRACT;

    int     priority;
    Thread *thread;

    THREADBASEREF  pThis = (THREADBASEREF) pThisUNSAFE;
    HELPER_METHOD_FRAME_BEGIN_1(pThis);

    if (pThis==NULL)
    {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    // translate the priority (validating as well)
    priority = MapToNTPriority(iPriority);  // can throw; needs a frame

    // validate the thread
    thread = pThis->GetInternal();

    if (ThreadIsDead(thread))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_Dead_Priority"));
    }

    INT32 oldPriority = pThis->m_Priority;

    // Eliminate the race condition by establishing m_Priority before we check for if
    // the thread is running.  See ThreadNative::Start() for the other half.
    pThis->m_Priority = iPriority;

    if (!thread->SetThreadPriority(priority))
    {
        pThis->m_Priority = oldPriority;
        COMPlusThrow(kThreadStateException, W("ThreadState_SetPriorityFailed"));
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// This service can be called on unstarted and dead threads.  For unstarted ones, the
// next wait will be interrupted.  For dead ones, this service quietly does nothing.
FCIMPL1(void, ThreadNative::Interrupt, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowResVoid(kNullReferenceException, W("NullReference_This"));

    Thread  *thread = pThisUNSAFE->GetInternal();

    if (thread == 0)
        FCThrowExVoid(kThreadStateException, IDS_EE_THREAD_CANNOT_GET, NULL, NULL, NULL);

    HELPER_METHOD_FRAME_BEGIN_0();

    thread->UserInterrupt(Thread::TI_Interrupt);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, ThreadNative::IsAlive, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    THREADBASEREF thisRef(pThisUNSAFE);
    BOOL ret = false;

    // Keep managed Thread object alive, since the native object's
    // lifetime is tied to the managed object's finalizer.  And with
    // resurrection, it may be possible to get a dangling pointer here -
    // consider both protecting thisRef and setting the managed object's
    // Thread* to NULL in the GC's ScanForFinalization method.
    HELPER_METHOD_FRAME_BEGIN_RET_1(thisRef);

    Thread  *thread = thisRef->GetInternal();

    if (thread == 0)
        COMPlusThrow(kThreadStateException, IDS_EE_THREAD_CANNOT_GET);

    ret = ThreadIsRunning(thread);

    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

FCIMPL2(FC_BOOL_RET, ThreadNative::Join, ThreadBaseObject* pThisUNSAFE, INT32 Timeout)
{
    FCALL_CONTRACT;

    BOOL            retVal = FALSE;
    THREADBASEREF   pThis   = (THREADBASEREF) pThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

    if (pThis==NULL)
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));

    // validate the timeout
    if ((Timeout < 0) && (Timeout != INFINITE_TIMEOUT))
        COMPlusThrowArgumentOutOfRange(W("millisecondsTimeout"), W("ArgumentOutOfRange_NeedNonNegOrNegative1"));

    retVal = DoJoin(pThis, Timeout);

    HELPER_METHOD_FRAME_END();

    FC_RETURN_BOOL(retVal);
}
FCIMPLEND

#undef Sleep
FCIMPL1(void, ThreadNative::Sleep, INT32 iTime)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();

    GetThread()->UserSleep(iTime);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#define Sleep(dwMilliseconds) Dont_Use_Sleep(dwMilliseconds)

extern "C" void QCALLTYPE ThreadNative_UninterruptibleSleep0()
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ClrSleepEx(0, false);

    END_QCALL;
}

FCIMPL1(INT32, ThreadNative::GetManagedThreadId, ThreadBaseObject* th) {
    FCALL_CONTRACT;

    FC_GC_POLL_NOT_NEEDED();
    if (th == NULL)
        FCThrow(kNullReferenceException);

    return th->GetManagedThreadId();
}
FCIMPLEND

NOINLINE static Object* GetCurrentThreadHelper()
{
    FCALL_CONTRACT;
    FC_INNER_PROLOG(ThreadNative::GetCurrentThread);
    OBJECTREF   refRetVal  = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, refRetVal);
    refRetVal = GetThread()->GetExposedObject();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
    return OBJECTREFToObject(refRetVal);
}

FCIMPL0(Object*, ThreadNative::GetCurrentThread)
{
    FCALL_CONTRACT;
    OBJECTHANDLE ExposedObject = GetThread()->m_ExposedObject;
    _ASSERTE(ExposedObject != 0); //Thread's constructor always initializes its GCHandle
    Object* result = *((Object**) ExposedObject);
    if (result != 0)
        return result;

    FC_INNER_RETURN(Object*, GetCurrentThreadHelper());
}
FCIMPLEND

extern "C" UINT64 QCALLTYPE ThreadNative_GetCurrentOSThreadId()
{
    QCALL_CONTRACT;

    // The Windows API GetCurrentThreadId returns a 32-bit integer thread ID.
    // On some non-Windows platforms (e.g. OSX), the thread ID is a 64-bit value.
    // We special case the API for non-Windows to get the 64-bit value and zero-extend
    // the Windows value to return a single data type on all platforms.

    UINT64 threadId;

    BEGIN_QCALL;
#ifndef TARGET_UNIX
    threadId = (UINT64) GetCurrentThreadId();
#else
    threadId = (UINT64) PAL_GetCurrentOSThreadId();
#endif
    END_QCALL;

    return threadId;
}

FCIMPL1(void, ThreadNative::Initialize, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    THREADBASEREF   pThis       = (THREADBASEREF) pThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_1(pThis);

    _ASSERTE(pThis != NULL);
    _ASSERTE(pThis->m_InternalThread == NULL);

    // if we don't have an internal Thread object associated with this exposed object,
    // now is our first opportunity to create one.
    Thread      *unstarted = SetupUnstartedThread();

    PREFIX_ASSUME(unstarted != NULL);

    if (GetThread()->GetDomain()->IgnoreUnhandledExceptions())
    {
        unstarted->SetThreadStateNC(Thread::TSNC_IgnoreUnhandledExceptions);
    }

    pThis->SetInternal(unstarted);
    pThis->SetManagedThreadId(unstarted->GetThreadId());
    unstarted->SetExposedObject(pThis);

    // Initialize the thread priority to normal.
    pThis->SetPriority(ThreadNative::PRIORITY_NORMAL);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


// Set whether or not this is a background thread.
FCIMPL2(void, ThreadNative::SetBackground, ThreadBaseObject* pThisUNSAFE, CLR_BOOL isBackground)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowResVoid(kNullReferenceException, W("NullReference_This"));

    // validate the thread
    Thread  *thread = pThisUNSAFE->GetInternal();

    if (ThreadIsDead(thread))
        FCThrowResVoid(kThreadStateException, W("ThreadState_Dead_State"));

    HELPER_METHOD_FRAME_BEGIN_0();

    thread->SetBackground(isBackground);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

// Return whether or not this is a background thread.
FCIMPL1(FC_BOOL_RET, ThreadNative::IsBackground, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    // validate the thread
    Thread  *thread = pThisUNSAFE->GetInternal();

    if (ThreadIsDead(thread))
        FCThrowRes(kThreadStateException, W("ThreadState_Dead_State"));

    FC_RETURN_BOOL(thread->IsBackground());
}
FCIMPLEND


// Deliver the state of the thread as a consistent set of bits.
// This copied in VM\EEDbgInterfaceImpl.h's
//     CorDebugUserState GetUserState( Thread *pThread )
// , so propagate changes to both functions
FCIMPL1(INT32, ThreadNative::GetThreadState, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    INT32               res = 0;
    Thread::ThreadState state;

    if (pThisUNSAFE==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    // validate the thread.  Failure here implies that the thread was finalized
    // and then resurrected.
    Thread  *thread = pThisUNSAFE->GetInternal();

    if (!thread)
        FCThrowEx(kThreadStateException, IDS_EE_THREAD_CANNOT_GET, NULL, NULL, NULL);

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    // grab a snapshot
    state = thread->GetSnapshotState();

    if (state & Thread::TS_Background)
        res |= ThreadBackground;

    if (state & Thread::TS_Unstarted)
        res |= ThreadUnstarted;

    // Don't report a StopRequested if the thread has actually stopped.
    if (state & Thread::TS_Dead)
    {
        res |= ThreadStopped;
    }
    else
    {
        if (state & Thread::TS_AbortRequested)
            res |= ThreadAbortRequested;
    }

    if (state & Thread::TS_Interruptible)
        res |= ThreadWaitSleepJoin;

    HELPER_METHOD_POLL();
    HELPER_METHOD_FRAME_END();

    return res;
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT

// Indicate whether the thread will host an STA (this may fail if the thread has
// already been made part of the MTA, use GetApartmentState or the return state
// from this routine to check for this).
FCIMPL2(INT32, ThreadNative::SetApartmentState, ThreadBaseObject* pThisUNSAFE, INT32 iState)
{
    FCALL_CONTRACT;

    if (pThisUNSAFE==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    INT32           retVal  = ApartmentUnknown;
    BOOL    ok = TRUE;
    THREADBASEREF   pThis   = (THREADBASEREF) pThisUNSAFE;

    HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

    // Translate state input. ApartmentUnknown is not an acceptable input state.
    // Throw an exception here rather than pass it through to the internal
    // routine, which asserts.
    Thread::ApartmentState state = Thread::AS_Unknown;
    if (iState == ApartmentSTA)
        state = Thread::AS_InSTA;
    else if (iState == ApartmentMTA)
        state = Thread::AS_InMTA;
    else if (iState == ApartmentUnknown)
        state = Thread::AS_Unknown;
    else
        COMPlusThrow(kArgumentOutOfRangeException, W("ArgumentOutOfRange_Enum"));

    Thread  *thread = pThis->GetInternal();
    if (!thread)
        COMPlusThrow(kThreadStateException, IDS_EE_THREAD_CANNOT_GET);

    {
        pThis->EnterObjMonitor();

        // We can only change the apartment if the thread is unstarted or
        // running, and if it's running we have to be in the thread's
        // context.
        if ((!ThreadNotStarted(thread) && !ThreadIsRunning(thread)) ||
            (!ThreadNotStarted(thread) && (GetThread() != thread)))
            ok = FALSE;
        else
        {
            EX_TRY
            {
                state = thread->SetApartment(state);
            }
            EX_CATCH
            {
                pThis->LeaveObjMonitor();
                EX_RETHROW;
            }
            EX_END_CATCH_UNREACHABLE;
        }

        pThis->LeaveObjMonitor();
    }


    // Now it's safe to throw exceptions again.
    if (!ok)
        COMPlusThrow(kThreadStateException);

    // Translate state back into external form
    if (state == Thread::AS_InSTA)
        retVal = ApartmentSTA;
    else if (state == Thread::AS_InMTA)
        retVal = ApartmentMTA;
    else if (state == Thread::AS_Unknown)
        retVal = ApartmentUnknown;
    else
        _ASSERTE(!"Invalid state returned from SetApartment");

    HELPER_METHOD_FRAME_END();

    return retVal;
}
FCIMPLEND

// Return whether the thread hosts an STA, is a member of the MTA or is not
// currently initialized for COM.
FCIMPL1(INT32, ThreadNative::GetApartmentState, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    INT32 retVal = 0;

    THREADBASEREF refThis = (THREADBASEREF) ObjectToOBJECTREF(pThisUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_RET_1(refThis);

    if (refThis == NULL)
    {
        COMPlusThrow(kNullReferenceException, W("NullReference_This"));
    }

    Thread* thread = refThis->GetInternal();

    if (ThreadIsDead(thread))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_Dead_State"));
    }

    Thread::ApartmentState state = thread->GetApartment();

#ifdef FEATURE_COMINTEROP
    if (state == Thread::AS_Unknown)
    {
        // If the CLR hasn't started COM yet, start it up and attempt the call again.
        // We do this in order to minimize the number of situations under which we return
        // ApartmentState.Unknown to our callers.
        if (!g_fComStarted)
        {
            EnsureComStarted();
            state = thread->GetApartment();
        }
    }
#endif // FEATURE_COMINTEROP

    // Translate state into external form
    retVal = ApartmentUnknown;
    if (state == Thread::AS_InSTA)
    {
        retVal = ApartmentSTA;
    }
    else if (state == Thread::AS_InMTA)
    {
        retVal = ApartmentMTA;
    }
    else if (state == Thread::AS_Unknown)
    {
        retVal = ApartmentUnknown;
    }
    else
    {
        _ASSERTE(!"Invalid state returned from GetApartment");
    }

    HELPER_METHOD_FRAME_END();

    return retVal;
}
FCIMPLEND

#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

void ReleaseThreadExternalCount(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;
    pThread->DecExternalCount(FALSE);
}

typedef Holder<Thread *, DoNothing, ReleaseThreadExternalCount> ThreadExternalCountHolder;

// Wait for the thread to die
BOOL ThreadNative::DoJoin(THREADBASEREF DyingThread, INT32 timeout)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(DyingThread != NULL);
        PRECONDITION((timeout >= 0) || (timeout == INFINITE_TIMEOUT));
    }
    CONTRACTL_END;

    Thread * DyingInternal = DyingThread->GetInternal();

    // Validate the handle.  It's valid to Join a thread that's not running -- so
    // long as it was once started.
    if (DyingInternal == 0 ||
        !(DyingInternal->m_State & Thread::TS_LegalToJoin))
    {
        COMPlusThrow(kThreadStateException, W("ThreadState_NotStarted"));
    }

    // Don't grab the handle until we know it has started, to eliminate the race
    // condition.
    if (ThreadIsDead(DyingInternal) || !DyingInternal->HasValidThreadHandle())
        return TRUE;

    DWORD dwTimeOut32 = (timeout == INFINITE_TIMEOUT
                   ? INFINITE
                   : (DWORD) timeout);

    // There is a race here.  DyingThread is going to close its thread handle.
    // If we grab the handle and then DyingThread closes it, we will wait forever
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

    // This function is intentionally blank.
    // See comment in code:MethodTable::CallFinalizer.

    _ASSERTE (!"Should not be called");

    FCUnique(0x21);
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP
FCIMPL1(void, ThreadNative::DisableComObjectEagerCleanup, ThreadBaseObject* pThisUNSAFE)
{
    FCALL_CONTRACT;

    _ASSERTE(pThisUNSAFE != NULL);
    VALIDATEOBJECT(pThisUNSAFE);
    Thread *pThread = pThisUNSAFE->GetInternal();

    HELPER_METHOD_FRAME_BEGIN_0();

    if (pThread == NULL)
        COMPlusThrow(kThreadStateException, IDS_EE_THREAD_CANNOT_GET);

    pThread->SetDisableComObjectEagerCleanup();

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
#endif //FEATURE_COMINTEROP

extern "C" void QCALLTYPE ThreadNative_InformThreadNameChange(QCall::ThreadHandle thread, LPCWSTR name, INT32 len)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ThreadNative::InformThreadNameChange(thread, name, len);

    END_QCALL;
}

void ThreadNative::InformThreadNameChange(Thread* pThread, LPCWSTR name, INT32 len)
{
    // Set on Windows 10 Creators Update and later machines the unmanaged thread name as well. That will show up in ETW traces and debuggers which is very helpful
    // if more and more threads get a meaningful name
    // Will also show up in Linux in gdb and such.
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
}

extern "C" UINT64 QCALLTYPE ThreadNative_GetProcessDefaultStackSize()
{
    QCALL_CONTRACT;

    SIZE_T reserve = 0;
    SIZE_T commit = 0;

    BEGIN_QCALL;

    if (!Thread::GetProcessDefaultStackSize(&reserve, &commit))
        reserve = 1024 * 1024;

    END_QCALL;

    return (UINT64)reserve;
}



FCIMPL1(FC_BOOL_RET, ThreadNative::IsThreadpoolThread, ThreadBaseObject* thread)
{
    FCALL_CONTRACT;

    if (thread==NULL)
        FCThrowRes(kNullReferenceException, W("NullReference_This"));

    Thread *pThread = thread->GetInternal();

    if (pThread == NULL)
        FCThrowRes(kThreadStateException, W("ThreadState_Dead_State"));

    BOOL ret = pThread->IsThreadPoolThread();

    FC_GC_POLL_RET();

    FC_RETURN_BOOL(ret);
}
FCIMPLEND

FCIMPL1(void, ThreadNative::SetIsThreadpoolThread, ThreadBaseObject* thread)
{
    FCALL_CONTRACT;

    if (thread == NULL)
        FCThrowResVoid(kNullReferenceException, W("NullReference_This"));

    Thread *pThread = thread->GetInternal();

    if (pThread == NULL)
        FCThrowResVoid(kThreadStateException, W("ThreadState_Dead_State"));

    pThread->SetIsThreadPoolThread();
}
FCIMPLEND

FCIMPL0(INT32, ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
{
    FCALL_CONTRACT;

    return (INT32)YieldProcessorNormalization::GetOptimalMaxNormalizedYieldsPerSpinIteration();
}
FCIMPLEND

FCIMPL1(void, ThreadNative::SpinWait, int iterations)
{
    FCALL_CONTRACT;

    if (iterations <= 0)
    {
        return;
    }

    //
    // If we're not going to spin for long, it's ok to remain in cooperative mode.
    // The threshold is determined by the cost of entering preemptive mode; if we're
    // spinning for less than that number of cycles, then switching to preemptive
    // mode won't help a GC start any faster.
    //
    if (iterations <= 100000)
    {
        YieldProcessorNormalized(iterations);
        return;
    }

    //
    // Too many iterations; better switch to preemptive mode to avoid stalling a GC.
    //
    HELPER_METHOD_FRAME_BEGIN_NOPOLL();
    GCX_PREEMP();

    YieldProcessorNormalized(iterations);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

extern "C" BOOL QCALLTYPE ThreadNative_YieldThread()
{
    QCALL_CONTRACT;

    BOOL ret = FALSE;

    BEGIN_QCALL

    ret = __SwitchToThread(0, CALLER_LIMITS_SPINNING);

    END_QCALL

    return ret;
}

FCIMPL0(INT32, ThreadNative::GetCurrentProcessorNumber)
{
    FCALL_CONTRACT;

#ifndef TARGET_UNIX
    PROCESSOR_NUMBER proc_no_cpu_group;
    GetCurrentProcessorNumberEx(&proc_no_cpu_group);
    return (proc_no_cpu_group.Group << 6) | proc_no_cpu_group.Number;
#else
    return ::GetCurrentProcessorNumber();
#endif //!TARGET_UNIX
}
FCIMPLEND;
