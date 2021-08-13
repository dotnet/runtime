// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: debugger.cpp
//

//
// Debugger runtime controller routines.
//
//*****************************************************************************

#include "stdafx.h"
#include "debugdebugger.h"
#include "../inc/common.h"
#include "eeconfig.h" // This is here even for retail & free builds...
#include "../../dlls/mscorrc/resource.h"

#include "vars.hpp"
#include <limits.h>
#include "ilformatter.h"
#include "typeparse.h"
#include "debuginfostore.h"
#include "generics.h"
#include "../../vm/methoditer.h"
#include "../../vm/encee.h"
#include "../../vm/dwreport.h"
#include "../../vm/eepolicy.h"
#include "../../vm/excep.h"
#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
#include "dbgtransportsession.h"
#endif // FEATURE_DBGIPC_TRANSPORT_VM

#ifdef TEST_DATA_CONSISTENCY
#include "datatest.h"
#endif // TEST_DATA_CONSISTENCY

#include "dbgenginemetrics.h"

#include "../../vm/rejit.h"

#include "threadsuspend.h"


#ifdef DEBUGGING_SUPPORTED

#ifdef _DEBUG
// Reg key. We can set this and then any debugger-lazy-init code will assert.
// This helps track down places where we're caching in debugger stuff in a
// non-debugger scenario.
bool g_DbgShouldntUseDebugger = false;
#endif


/* ------------------------------------------------------------------------ *
 * Global variables
 * ------------------------------------------------------------------------ */

GPTR_IMPL(Debugger,         g_pDebugger);
GPTR_IMPL(EEDebugInterface, g_pEEInterface);
SVAL_IMPL_INIT(BOOL, Debugger, s_fCanChangeNgenFlags, TRUE);

// This is a public export so debuggers can read and determine if the coreclr
// process is waiting for JIT debugging attach.
GVAL_IMPL_INIT(ULONG, CLRJitAttachState, 0);

bool g_EnableSIS = false;

// The following instances are used for invoking overloaded new/delete
InteropSafe interopsafe;

#ifndef DACCESS_COMPILE

DebuggerRCThread        *g_pRCThread = NULL;

#ifndef _PREFAST_
// Do some compile time checking on the events in DbgIpcEventTypes.h
// No one ever calls this. But the compiler should still compile it,
// and that should be sufficient.
void DoCompileTimeCheckOnDbgIpcEventTypes()
{
    _ASSERTE(!"Don't call this function. It just does compile time checking\n");

    // We use the C_ASSERT macro here to get a compile-time assert.

    // Make sure we don't have any duplicate numbers.
    // The switch statements in the main loops won't always catch this
    // since we may not switch on all events.

    // store Type-0 in const local vars, so we can use them for bounds checking
    // Create local vars with the val from Type1 & Type2. If there are any
    // collisions, then the variables' names will collide at compile time.
    #define IPC_EVENT_TYPE0(type, val)  const int e_##type = val;
    #define IPC_EVENT_TYPE1(type, val)  int T_##val; T_##val = 0;
    #define IPC_EVENT_TYPE2(type, val)  int T_##val; T_##val = 0;
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0

    // Ensure that all identifiers are unique and are matched with
    // integer values.
    #define IPC_EVENT_TYPE0(type, val)  int T2_##type; T2_##type = val;
    #define IPC_EVENT_TYPE1(type, val)  int T2_##type; T2_##type = val;
    #define IPC_EVENT_TYPE2(type, val)  int T2_##type; T2_##type = val;
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0

    // Make sure all values are subset of the bits specified by DB_IPCE_TYPE_MASK
    #define IPC_EVENT_TYPE0(type, val)
    #define IPC_EVENT_TYPE1(type, val)  C_ASSERT((val & e_DB_IPCE_TYPE_MASK) == val);
    #define IPC_EVENT_TYPE2(type, val)  C_ASSERT((val & e_DB_IPCE_TYPE_MASK) == val);
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0

    // Make sure that no value is DB_IPCE_INVALID_EVENT
    #define IPC_EVENT_TYPE0(type, val)
    #define IPC_EVENT_TYPE1(type, val)  C_ASSERT(val != e_DB_IPCE_INVALID_EVENT);
    #define IPC_EVENT_TYPE2(type, val)  C_ASSERT(val != e_DB_IPCE_INVALID_EVENT);
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0

    // Make sure first-last values are well structured.
    static_assert_no_msg(e_DB_IPCE_RUNTIME_FIRST < e_DB_IPCE_RUNTIME_LAST);
    static_assert_no_msg(e_DB_IPCE_DEBUGGER_FIRST < e_DB_IPCE_DEBUGGER_LAST);

    // Make sure that event ranges don't overlap.
    // This check is simplified because L->R events come before R<-L
    static_assert_no_msg(e_DB_IPCE_RUNTIME_LAST < e_DB_IPCE_DEBUGGER_FIRST);


    // Make sure values are in the proper ranges
    // Type1 should be in the Runtime range, Type2 in the Debugger range.
    #define IPC_EVENT_TYPE0(type, val)
    #define IPC_EVENT_TYPE1(type, val)  C_ASSERT((e_DB_IPCE_RUNTIME_FIRST <= val) && (val < e_DB_IPCE_RUNTIME_LAST));
    #define IPC_EVENT_TYPE2(type, val)  C_ASSERT((e_DB_IPCE_DEBUGGER_FIRST <= val) && (val < e_DB_IPCE_DEBUGGER_LAST));
    #include "dbgipceventtypes.h"
    #undef IPC_EVENT_TYPE2
    #undef IPC_EVENT_TYPE1
    #undef IPC_EVENT_TYPE0

    // Make sure that events are in increasing order
    // It's ok if the events skip numbers.
    // This is a more specific check than the range check above.

    /* Expands to look like this:
    const bool f = (
    first <=
    10) && (10 <
    11) && (11 <
    12) && (12 <
    last)
    static_assert_no_msg(f);
    */

    const bool f1 = (
        (e_DB_IPCE_RUNTIME_FIRST <=
        #define IPC_EVENT_TYPE0(type, val)
        #define IPC_EVENT_TYPE1(type, val)  val) && (val <
        #define IPC_EVENT_TYPE2(type, val)
        #include "dbgipceventtypes.h"
        #undef IPC_EVENT_TYPE2
        #undef IPC_EVENT_TYPE1
        #undef IPC_EVENT_TYPE0
        e_DB_IPCE_RUNTIME_LAST)
    );
    static_assert_no_msg(f1);

    const bool f2 = (
        (e_DB_IPCE_DEBUGGER_FIRST <=
        #define IPC_EVENT_TYPE0(type, val)
        #define IPC_EVENT_TYPE1(type, val)
        #define IPC_EVENT_TYPE2(type, val) val) && (val <
        #include "dbgipceventtypes.h"
        #undef IPC_EVENT_TYPE2
        #undef IPC_EVENT_TYPE1
        #undef IPC_EVENT_TYPE0
        e_DB_IPCE_DEBUGGER_LAST)
    );
    static_assert_no_msg(f2);

} // end checks
#endif // _PREFAST_

//-----------------------------------------------------------------------------
// Ctor for AtSafePlaceHolder
AtSafePlaceHolder::AtSafePlaceHolder(Thread * pThread)
{
    _ASSERTE(pThread != NULL);
    if (!g_pDebugger->IsThreadAtSafePlace(pThread))
    {
        m_pThreadAtUnsafePlace = pThread;
        g_pDebugger->IncThreadsAtUnsafePlaces();
    }
    else
    {
        m_pThreadAtUnsafePlace = NULL;
    }
}

//-----------------------------------------------------------------------------
// Dtor for AtSafePlaceHolder
AtSafePlaceHolder::~AtSafePlaceHolder()
{
    Clear();
}

//-----------------------------------------------------------------------------
// Returns true if this adjusted the unsafe counter
bool AtSafePlaceHolder::IsAtUnsafePlace()
{
    return m_pThreadAtUnsafePlace != NULL;
}

//-----------------------------------------------------------------------------
// Clear the holder.
// Notes:
//    This can be called multiple times.
//    Calling this makes the dtor a nop.
void AtSafePlaceHolder::Clear()
{
    if (m_pThreadAtUnsafePlace != NULL)
    {
        // The thread is still at an unsafe place.
        // We're clearing the flag to avoid the Dtor() calling DecThreads again.
        m_pThreadAtUnsafePlace = NULL;
        g_pDebugger->DecThreadsAtUnsafePlaces();
    }
}

//-----------------------------------------------------------------------------
// Is the guard page missing on this thread?
// Should only be called for managed threads handling a managed exception.
// If we're handling a stack overflow (ie, missing guard page), then another
// stack overflow will instantly terminate the process. In that case, do stack
// intensive stuff on the helper thread (which has lots of stack space). Only
// problem is that if the faulting thread has a lock, the helper thread may
// get stuck.
// Serves as a hint whether we want to do a favor on the
// faulting thread (preferred) or the helper thread (if low stack).
// See whidbey issue 127436.
//-----------------------------------------------------------------------------
bool IsGuardPageGone()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread * pThread = g_pEEInterface->GetThread();

    // We're not going to be called for a unmanaged exception.
    // Should always have a managed thread, but just in case something really
    // strange happens, it's not worth an AV. (since this is just being used as a hint)
    if (pThread == NULL)
    {
        return false;
    }

    // Don't use pThread->IsGuardPageGone(), it's not accurate here.
    bool fGuardPageGone = (pThread->DetermineIfGuardPagePresent() == FALSE);
    LOG((LF_CORDB, LL_INFO1000000, "D::IsGuardPageGone=%d\n", fGuardPageGone));
    return fGuardPageGone;
}

//-----------------------------------------------------------------------------
// LSPTR_XYZ is a type-safe wrapper around an opaque reference type XYZ in the left-side.
// But TypeHandles are value-types that can't be directly converted into a pointer.
// Thus converting between LSPTR_XYZ and TypeHandles requires some extra glue.
// The following conversions are valid:
//      LSPTR_XYZ <--> XYZ*   (via Set/UnWrap methods)
//      TypeHandle <--> void* (via AsPtr() and FromPtr()).
// so we can't directly convert between LSPTR_TYPEHANDLE and TypeHandle.
// We must do:  TypeHandle <--> void* <--> XYZ <--> LSPTR_XYZ
// So LSPTR_TYPEHANDLE is actually for TypeHandleDummyPtr, and then we unsafe cast
// that to a void* to use w/ AsPtr() and FromPtr() to convert to TypeHandles.
// @todo- it would be nice to have these happen automatically w/ Set & UnWrap.
//-----------------------------------------------------------------------------

// helper class to do conversion above.
class TypeHandleDummyPtr
{
private:
    TypeHandleDummyPtr() { }; // should never actually create this.
    void * data;
};

// Convert: VMPTR_TYPEHANDLE --> TypeHandle
TypeHandle GetTypeHandle(VMPTR_TypeHandle ptr)
{
    return TypeHandle::FromPtr(ptr.GetRawPtr());
}

// Convert: TypeHandle --> LSPTR_TYPEHANDLE
VMPTR_TypeHandle WrapTypeHandle(TypeHandle th)
{
    return VMPTR_TypeHandle::MakePtr(reinterpret_cast<TypeHandle *> (th.AsPtr()));
}

extern void WaitForEndOfShutdown();


// Get the Canary structure which can sniff if the helper thread is safe to run.
HelperCanary * Debugger::GetCanary()
{
    return g_pRCThread->GetCanary();
}

// IMPORTANT!!!!!
// Do not call Lock and Unlock directly. Because you might not unlock
// if exception takes place. Use DebuggerLockHolder instead!!!
// Only AcquireDebuggerLock can call directly.
//
void Debugger::DoNotCallDirectlyPrivateLock(void)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB,LL_INFO10000, "D::Lock acquire attempt by 0x%x\n",
        GetCurrentThreadId()));

    // Debugger lock is larger than both Controller & debugger-data locks.
    // So we should never try to take the D lock if we hold either of the others.


    // Lock becomes no-op in late shutdown.
    if (g_fProcessDetach)
    {
        return;
    }


    //
    // If the debugger has been disabled by the runtime, this means that it should block
    // all threads that are trying to travel thru the debugger.  We do this by blocking
    // threads as they try and take the debugger lock.
    //
    if (m_fDisabled)
    {
        __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
        _ASSERTE (!"Can not reach here");
    }

    m_mutex.Enter();

    //
    // If we were blocked on the lock and the debugging facilities got disabled
    // while we were waiting, release the lock and park this thread.
    //
    if (m_fDisabled)
    {
        m_mutex.Leave();
        __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
        _ASSERTE (!"Can not reach here");
    }

    //
    // Now check if we are in a shutdown case...
    //
    Thread * pThread;
    bool fIsCooperative;

    pThread = g_pEEInterface->GetThread();
    fIsCooperative = (pThread != NULL) && (pThread->PreemptiveGCDisabled());

    if (m_fShutdownMode && !fIsCooperative)
    {
        // The big fear is that some other random thread will take the debugger-lock and then block on something else,
        // and thus prevent the helper/finalizer threads from taking the debugger-lock in shutdown scenarios.
        //
        // If we're in shutdown mode, then some locks (like the Thread-Store-Lock) get special semantics.
        // Only helper / finalizer / shutdown threads can actually take these locks.
        // Other threads that try to take them will just get parked and block forever.
        // This is ok b/c the only threads that need to run at this point are the Finalizer and Helper threads.
        //
        // We need to be in preemptive to block for shutdown, so we don't do this block in Coop mode.
        // Fortunately, it's safe to take this lock in coop mode because we know the thread can't block
        // on anything interesting because we're in a GC-forbid region (see crst flags).
        m_mutex.ReleaseAndBlockForShutdownIfNotSpecialThread();
    }



#ifdef _DEBUG
    _ASSERTE(m_mutexCount >= 0);

    if (m_mutexCount>0)
    {
        if (pThread)
        {
            // mamaged thread
            _ASSERTE(m_mutexOwner == GetThreadIdHelper(pThread));
        }
        else
        {
            // unmanaged thread
            _ASSERTE(m_mutexOwner == GetCurrentThreadId());
        }
    }

    m_mutexCount++;
    if (pThread)
    {
        m_mutexOwner = GetThreadIdHelper(pThread);
    }
    else
    {
        // unmanaged thread
        m_mutexOwner = GetCurrentThreadId();
    }

    if (m_mutexCount == 1)
    {
        LOG((LF_CORDB,LL_INFO10000, "D::Lock acquired by 0x%x\n", m_mutexOwner));
    }
#endif

}

// See comment above.
// Only ReleaseDebuggerLock can call directly.
void Debugger::DoNotCallDirectlyPrivateUnlock(void)
{
    WRAPPER_NO_CONTRACT;

    // Controller lock is "smaller" than debugger lock.


    if (!g_fProcessDetach)
    {
#ifdef _DEBUG
        if (m_mutexCount == 1)
            LOG((LF_CORDB,LL_INFO10000, "D::Unlock released by 0x%x\n",
                m_mutexOwner));

        if(0 == --m_mutexCount)
            m_mutexOwner = 0;

        _ASSERTE( m_mutexCount >= 0);
#endif
        m_mutex.Leave();

        //
        // If the debugger has been disabled by the runtime, this means that it should block
        // all threads that are trying to travel thru the debugger.  We do this by blocking
        // threads also as they leave the debugger lock.
        //
        if (m_fDisabled)
        {
            __SwitchToThread(INFINITE, CALLER_LIMITS_SPINNING);
            _ASSERTE (!"Can not reach here");
        }

    }
}

#ifdef TEST_DATA_CONSISTENCY

// ---------------------------------------------------------------------------------
// Implementations for DataTest member functions
// ---------------------------------------------------------------------------------

// Send an event to the RS to signal that it should test to determine if a crst is held.
// This is for testing purposes only.
// Arguments:
//     input:  pCrst     - the lock to test
//             fOkToTake - true iff the LS does NOT currently hold the lock
//     output: none
// Notes: The RS will throw if the lock is held. The code that tests the lock will catch the
//        exception and assert if throwing was not the correct thing to do (determined via the
//        boolean). See the case for DB_IPCE_TEST_CRST in code:CordbProcess::RawDispatchEvent.
//
void DataTest::SendDbgCrstEvent(Crst * pCrst, bool fOkToTake)
{
    DebuggerIPCEvent * pLockEvent = g_pDebugger->m_pRCThread->GetIPCEventSendBuffer();

    g_pDebugger->InitIPCEvent(pLockEvent, DB_IPCE_TEST_CRST);

    pLockEvent->TestCrstData.vmCrst.SetRawPtr(pCrst);
    pLockEvent->TestCrstData.fOkToTake = fOkToTake;

    g_pDebugger->SendRawEvent(pLockEvent);

} // DataTest::SendDbgCrstEvent

// Send an event to the RS to signal that it should test to determine if a SimpleRWLock is held.
// This is for testing purposes only.
// Arguments:
//     input:  pRWLock   - the lock to test
//             fOkToTake - true iff the LS does NOT currently hold the lock
//     output: none
// Note:  The RS will throw if the lock is held. The code that tests the lock will catch the
//        exception and assert if throwing was not the correct thing to do (determined via the
//        boolean). See the case for DB_IPCE_TEST_RWLOCK in code:CordbProcess::RawDispatchEvent.
//
void DataTest::SendDbgRWLockEvent(SimpleRWLock * pRWLock, bool okToTake)
{
    DebuggerIPCEvent * pLockEvent = g_pDebugger->m_pRCThread->GetIPCEventSendBuffer();

    g_pDebugger->InitIPCEvent(pLockEvent, DB_IPCE_TEST_RWLOCK);

    pLockEvent->TestRWLockData.vmRWLock.SetRawPtr(pRWLock);
    pLockEvent->TestRWLockData.fOkToTake = okToTake;

    g_pDebugger->SendRawEvent(pLockEvent);
} // DataTest::SendDbgRWLockEvent

// Takes a series of locks in various ways and signals the RS to test the locks at interesting
// points to ensure we reliably detect when the LS holds a lock. If in the course of inspection, the
// DAC needs to execute a code path where the LS holds a lock, we assume that the locked data is in
// an inconsistent state. In this situation, we don't want to report information about this data, so
// we throw an exception.
// This is for testing purposes only.
//
// Arguments: none
// Return Value: none
// Notes: See code:CordbProcess::RawDispatchEvent for the RS part of this test and code:Debugger::Startup
//        for the LS invocation of the test.
//        The environment variable TestDataConsistency must be set to 1 to make this test run.
void DataTest::TestDataSafety()
{
    const bool okToTake = true;

    SendDbgCrstEvent(&m_crst1, okToTake);
    {
        CrstHolder ch1(&m_crst1);
        SendDbgCrstEvent(&m_crst1, !okToTake);
        {
            CrstHolder ch2(&m_crst2);
            SendDbgCrstEvent(&m_crst2, !okToTake);
            SendDbgCrstEvent(&m_crst1, !okToTake);
        }
        SendDbgCrstEvent(&m_crst2, okToTake);
        SendDbgCrstEvent(&m_crst1, !okToTake);
    }
    SendDbgCrstEvent(&m_crst1, okToTake);

    {
        SendDbgRWLockEvent(&m_rwLock, okToTake);
        SimpleReadLockHolder readLock(&m_rwLock);
        SendDbgRWLockEvent(&m_rwLock, okToTake);
    }
    SendDbgRWLockEvent(&m_rwLock, okToTake);
    {
        SimpleWriteLockHolder readLock(&m_rwLock);
        SendDbgRWLockEvent(&m_rwLock, !okToTake);
    }

} // DataTest::TestDataSafety

#endif // TEST_DATA_CONSISTENCY

#if _DEBUG
static DebugEventCounter g_debugEventCounter;
static int g_iDbgRuntimeCounter[DBG_RUNTIME_MAX];
static int g_iDbgDebuggerCounter[DBG_DEBUGGER_MAX];

void DoAssertOnType(DebuggerIPCEventType event, int count)
{
    WRAPPER_NO_CONTRACT;

    // check to see if we need fire the assertion or not.
    if ((event & 0x0300) == 0x0100)
    {
        // use the Runtime array
        if (g_iDbgRuntimeCounter[event & 0x00ff] == count)
        {
            char        tmpStr[256];
            _snprintf_s(tmpStr, _countof(tmpStr), _TRUNCATE, "%s == %d, break now!",
                        IPCENames::GetName(event), count);

            // fire the assertion
            DbgAssertDialog(__FILE__, __LINE__, tmpStr);
        }
    }
    // check to see if we need fire the assertion or not.
    else if ((event & 0x0300) == 0x0200)
    {
        // use the Runtime array
        if (g_iDbgDebuggerCounter[event & 0x00ff] == count)
        {
            char        tmpStr[256];
            _snprintf_s(tmpStr, _countof(tmpStr), _TRUNCATE, "%s == %d, break now!",
                        IPCENames::GetName(event), count);

            // fire the assertion
            DbgAssertDialog(__FILE__, __LINE__, tmpStr);
        }
    }

}
void DbgLogHelper(DebuggerIPCEventType event)
{
    WRAPPER_NO_CONTRACT;

    switch (event)
    {
// we don't need to handle event type 0
#define IPC_EVENT_TYPE0(type, val)
#define IPC_EVENT_TYPE1(type, val)  case type: {\
                                        g_debugEventCounter.m_iDebugCount_##type++; \
                                        DoAssertOnType(type, g_debugEventCounter.m_iDebugCount_##type); \
                                        break; \
                                    }
#define IPC_EVENT_TYPE2(type, val)  case type: { \
                                        g_debugEventCounter.m_iDebugCount_##type++; \
                                        DoAssertOnType(type, g_debugEventCounter.m_iDebugCount_##type); \
                                        break; \
                                    }
#include "dbgipceventtypes.h"
#undef IPC_EVENT_TYPE2
#undef IPC_EVENT_TYPE1
#undef IPC_EVENT_TYPE0
            default:
                break;
    }
}
#endif // _DEBUG









/* ------------------------------------------------------------------------ *
 * DLL export routine
 * ------------------------------------------------------------------------ */

Debugger *CreateDebugger(void)
{
    Debugger *pDebugger = NULL;

    EX_TRY
    {
        pDebugger = new (nothrow) Debugger();
    }
    EX_CATCH
    {
        if (pDebugger != NULL)
        {
            delete pDebugger;
            pDebugger = NULL;
        }
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return pDebugger;
}

//
// CorDBGetInterface is exported to the Runtime so that it can call
// the Runtime Controller.
//
extern "C"{
HRESULT __cdecl CorDBGetInterface(DebugInterface** rcInterface)
{
    CONTRACT(HRESULT)
    {
        NOTHROW; // use HRESULTS instead
        GC_NOTRIGGER;
        POSTCONDITION(FAILED(RETVAL) || (rcInterface == NULL) || (*rcInterface != NULL));
    }
    CONTRACT_END;

    HRESULT hr = S_OK;

    if (rcInterface != NULL)
    {
        if (g_pDebugger == NULL)
        {
            LOG((LF_CORDB, LL_INFO10,
                 "CorDBGetInterface: initializing debugger.\n"));

            g_pDebugger = CreateDebugger();
            TRACE_ALLOC(g_pDebugger);

            if (g_pDebugger == NULL)
                hr = E_OUTOFMEMORY;
        }

        *rcInterface = g_pDebugger;
    }

    RETURN hr;
}
}

//-----------------------------------------------------------------------------
// Send a pre-init IPC event and block.
// We assume the IPC event has already been initialized. There's nothing special
// here; it just used the standard formula for sending an IPC event to the RS.
// This should match up w/ the description in SENDIPCEVENT_BEGIN.
//-----------------------------------------------------------------------------
void Debugger::SendSimpleIPCEventAndBlock()
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // BEGIN will acquire the lock (END will release it). While blocking, the
    // debugger may have detached though, so we need to check for that.
    _ASSERTE(ThreadHoldsLock());

    if (CORDebuggerAttached())
    {
        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        this->TrapAllRuntimeThreads();
    }
}

//-----------------------------------------------------------------------------
// Get context from a thread in managed code.
// See header for exact semantics.
//-----------------------------------------------------------------------------
CONTEXT * GetManagedStoppedCtx(Thread * pThread)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pThread != NULL);

    // We may be stopped or live.

    // If we're stopped at an interop-hijack, we'll have a filter context,
    // but we'd better not be redirected for a managed-suspension hijack.
    if (pThread->GetInteropDebuggingHijacked())
    {
        _ASSERTE(!ISREDIRECTEDTHREAD(pThread));
        return NULL;
    }

    // Check if we have a filter ctx. This should only be for managed-code.
    // We're stopped at some exception (likely an int3 or single-step).
    // Can't have both filter ctx + redirected ctx.
    CONTEXT *pCtx = g_pEEInterface->GetThreadFilterContext(pThread);
    if (pCtx != NULL)
    {
        _ASSERTE(!ISREDIRECTEDTHREAD(pThread));
        return pCtx;
    }

    if (ISREDIRECTEDTHREAD(pThread))
    {
        pCtx = GETREDIRECTEDCONTEXT(pThread);
        _ASSERTE(pCtx != NULL);
        return pCtx;
    }

    // Not stopped somewhere in managed code.
    return NULL;
}

//-----------------------------------------------------------------------------
// See header for exact semantics.
// Never NULL. (Caller guarantees this is active.)
//-----------------------------------------------------------------------------
CONTEXT * GetManagedLiveCtx(Thread * pThread)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pThread != NULL);

    // We should never be on the helper thread, we should only be inspecting our own thread.
    // We're in some Controller's Filter after hitting an exception.
    // We're not stopped.
    //_ASSERTE(!g_pDebugger->IsStopped()); <-- @todo - this fires, need to find out why.
    _ASSERTE(GetThreadNULLOk() == pThread);

    CONTEXT *pCtx = g_pEEInterface->GetThreadFilterContext(pThread);

    // Note that we may be in a M2U hijack. So we can't assert !pThread->GetInteropDebuggingHijacked()
    _ASSERTE(!ISREDIRECTEDTHREAD(pThread));
    _ASSERTE(pCtx);

    return pCtx;
}

// Attempt to validate a GC handle.
HRESULT ValidateGCHandle(OBJECTHANDLE oh)
{
    // The only real way to do this is to Enumerate all GC handles in the handle table.
    // That's too expensive. So we'll use a similar workaround that we use in ValidateObject.
    // This will err on the side off returning True for invalid handles.

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        // Use AVInRuntimeImplOkHolder.
        AVInRuntimeImplOkayHolder AVOkay;

        // This may throw if the Object Handle is invalid.
        Object * objPtr = *((Object**) oh);

        // NULL is certinally valid...
        if (objPtr != NULL)
        {
            if (!objPtr->ValidateObjectWithPossibleAV())
            {
                LOG((LF_CORDB, LL_INFO10000, "GAV: object methodtable-class invariant doesn't hold.\n"));
                hr = E_INVALIDARG;
                goto LExit;
            }
        }

    LExit: ;
    }
    EX_CATCH
    {
        LOG((LF_CORDB, LL_INFO10000, "GAV: exception indicated ref is bad.\n"));
        hr = E_INVALIDARG;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}


// Validate an object. Returns E_INVALIDARG or S_OK.
HRESULT ValidateObject(Object *objPtr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        // Use AVInRuntimeImplOkHolder.
        AVInRuntimeImplOkayHolder AVOkay;

        // NULL is certinally valid...
        if (objPtr != NULL)
        {
            if (!objPtr->ValidateObjectWithPossibleAV())
            {
                LOG((LF_CORDB, LL_INFO10000, "GAV: object methodtable-class invariant doesn't hold.\n"));
                hr = E_INVALIDARG;
                goto LExit;
            }
        }

    LExit: ;
    }
    EX_CATCH
    {
        LOG((LF_CORDB, LL_INFO10000, "GAV: exception indicated ref is bad.\n"));
        hr = E_INVALIDARG;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}   // ValidateObject


#ifdef FEATURE_DBGIPC_TRANSPORT_VM
void
ShutdownTransport()
{
    if (g_pDbgTransport != NULL)
    {
        g_pDbgTransport->Shutdown();
        g_pDbgTransport = NULL;
    }
}
#endif // FEATURE_DBGIPC_TRANSPORT_VM


/* ------------------------------------------------------------------------ *
 * Debugger routines
 * ------------------------------------------------------------------------ */

//
// a Debugger object represents the global state of the debugger program.
//

//
// Constructor & Destructor
//

/******************************************************************************
 *
 ******************************************************************************/
Debugger::Debugger()
  :
    m_fLeftSideInitialized(FALSE),
#ifdef _DEBUG
    m_mutexCount(0),
#endif //_DEBUG
    m_pRCThread(NULL),
    m_trappingRuntimeThreads(FALSE),
    m_stopped(FALSE),
    m_unrecoverableError(FALSE),
    m_ignoreThreadDetach(FALSE),
    m_pMethodInfos(NULL),
    m_mutex(CrstDebuggerMutex, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_REENTRANCY | CRST_DEBUGGER_THREAD)),
#ifdef _DEBUG
    m_mutexOwner(0),
    m_tidLockedForEventSending(0),
#endif //_DEBUG
    m_threadsAtUnsafePlaces(0),
    m_jitAttachInProgress(FALSE),
    m_launchingDebugger(FALSE),
    m_LoggingEnabled(TRUE),
    m_pAppDomainCB(NULL),
    m_dClassLoadCallbackCount(0),
    m_pModules(NULL),
    m_RSRequestedSync(FALSE),
    m_sendExceptionsOutsideOfJMC(TRUE),
    m_forceNonInterceptable(FALSE),
    m_pLazyData(NULL),
    m_defines(_defines),
    m_isSuspendedForGarbageCollection(FALSE),
    m_isBlockedOnGarbageCollectionEvent(FALSE),
    m_willBlockOnGarbageCollectionEvent(FALSE),
    m_isGarbageCollectionEventsEnabled(FALSE),
    m_isGarbageCollectionEventsEnabledLatch(FALSE)
{
    CONTRACTL
    {
        WRAPPER(THROWS);
        WRAPPER(GC_TRIGGERS);
        CONSTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    m_fShutdownMode = false;
    m_fDisabled = false;
    m_rgHijackFunction = NULL;

#ifdef _DEBUG
    InitDebugEventCounting();
#endif

    m_processId = GetCurrentProcessId();

    // Initialize these in ctor because we free them in dtor.
    // And we can't set them to some safe uninited value (like NULL).



    //------------------------------------------------------------------------------
    // Metadata data structure version numbers
    //
    // 1 - initial state of the layouts ( .NET Framework 4.5.2 )
    //
    // as data structure layouts change, add a new version number
    // and comment the changes
    m_mdDataStructureVersion = 1;

}

/******************************************************************************
 *
 ******************************************************************************/
Debugger::~Debugger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        DESTRUCTOR_CHECK;
    }
    CONTRACTL_END;

    // We explicitly leak the debugger object on shutdown. See Debugger::StopDebugger for details.
    _ASSERTE(!"Debugger dtor should not be called.");
}

#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)

// Given the start address and the end address of a function, return a MemoryRange for the function.
inline MemoryRange GetMemoryRangeForFunction(void *pfnStart, void *pfnEnd)
{
    PCODE pfnStartAddress = (PCODE)GetEEFuncEntryPoint(pfnStart);
    PCODE pfnEndAddress   = (PCODE)GetEEFuncEntryPoint(pfnEnd);
    return MemoryRange(dac_cast<PTR_VOID>(pfnStartAddress), (pfnEndAddress - pfnStartAddress));
}

// static
MemoryRange Debugger::s_hijackFunction[kMaxHijackFunctions] =
    {GetMemoryRangeForFunction(ExceptionHijack, ExceptionHijackEnd),
     GetMemoryRangeForFunction(RedirectedHandledJITCaseForGCThreadControl_Stub,
                               RedirectedHandledJITCaseForGCThreadControl_StubEnd),
     GetMemoryRangeForFunction(RedirectedHandledJITCaseForDbgThreadControl_Stub,
                               RedirectedHandledJITCaseForDbgThreadControl_StubEnd),
     GetMemoryRangeForFunction(RedirectedHandledJITCaseForUserSuspend_Stub,
                               RedirectedHandledJITCaseForUserSuspend_StubEnd)
#if defined(HAVE_GCCOVER) && defined(TARGET_AMD64)
     ,
     GetMemoryRangeForFunction(RedirectedHandledJITCaseForGCStress_Stub,
                               RedirectedHandledJITCaseForGCStress_StubEnd)
#endif // HAVE_GCCOVER && TARGET_AMD64
#ifdef FEATURE_SPECIAL_USER_MODE_APC
     ,
     GetMemoryRangeForFunction(ApcActivationCallbackStub,
                               ApcActivationCallbackStubEnd)
#endif // FEATURE_SPECIAL_USER_MODE_APC
    };
#endif // FEATURE_HIJACK && !TARGET_UNIX

// Save the necessary information for the debugger to recognize an IP in one of the thread redirection
// functions.
void Debugger::InitializeHijackFunctionAddress()
{
#if defined(FEATURE_HIJACK) && !defined(TARGET_UNIX)
    // Advertise hijack address for the DD Hijack primitive
    m_rgHijackFunction = Debugger::s_hijackFunction;
#endif // FEATURE_HIJACK && !TARGET_UNIX
}

// For debug-only builds, we'll have a debugging feature to count
// the number of ipc events and break on a specific number.
// Initialize the stuff to do that.
void Debugger::InitDebugEventCounting()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
#ifdef _DEBUG
    // initialize the debug event counter structure to zero
    memset(&g_debugEventCounter, 0, sizeof(DebugEventCounter));
    memset(&g_iDbgRuntimeCounter, 0, DBG_RUNTIME_MAX*sizeof(int));
    memset(&g_iDbgDebuggerCounter, 0, DBG_DEBUGGER_MAX*sizeof(int));

    // retrieve the possible counter for break point
    CLRConfigStringHolder wstrValue = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DebuggerBreakPoint);
    // The string value is of the following format
    // <Event Name>=Count;<Event Name>=Count;....;
    // The string must end with ;
    if (wstrValue != NULL)
    {
        LPSTR   strValue;
        int     cbReq;
        cbReq = WszWideCharToMultiByte(CP_UTF8, 0, wstrValue,-1, 0,0, 0,0);

        strValue = new (nothrow) char[cbReq+1];
        // This is a debug only thingy, if it fails, not worth taking
        // down the process.
        if (strValue == NULL)
            return;


        // now translate the unicode to ansi string
        WszWideCharToMultiByte(CP_UTF8, 0, wstrValue, -1, strValue, cbReq+1, 0,0);
        char *szEnd = (char *)strchr(strValue, ';');
        char *szStart = strValue;
        while (szEnd != NULL)
        {
            // Found a key value
            char    *szNameEnd = strchr(szStart, '=');
            int     iCount;
            DebuggerIPCEventType eventType;
            if (szNameEnd != NULL)
            {
                // This is a well form key
                *szNameEnd = '\0';
                *szEnd = '\0';

                // now szStart is the key name null terminated. Translate the counter into integer.
                iCount = atoi(szNameEnd+1);
                if (iCount != 0)
                {
                    eventType = IPCENames::GetEventType(szStart);

                    if (eventType < DB_IPCE_DEBUGGER_FIRST)
                    {
                        // use the runtime one
                        g_iDbgRuntimeCounter[eventType & 0x00ff] = iCount;
                    }
                    else if (eventType < DB_IPCE_DEBUGGER_LAST)
                    {
                        // use the debugger one
                        g_iDbgDebuggerCounter[eventType & 0x00ff] = iCount;
                    }
                    else
                        _ASSERTE(!"Unknown Event Type");
                }
            }
            szStart = szEnd + 1;
            // try to find next key value
            szEnd = (char *)strchr(szStart, ';');
        }

        // free the ansi buffer
        delete [] strValue;
    }
#endif // _DEBUG
}

// Checks if the MethodInfos table has been allocated, and if not does so.
// Throw on failure, so we always return
HRESULT Debugger::CheckInitMethodInfoTable()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_pMethodInfos == NULL)
    {
        DebuggerMethodInfoTable *pMethodInfos = NULL;

        EX_TRY
        {
            pMethodInfos = new (interopsafe) DebuggerMethodInfoTable();
        }
        EX_CATCH
        {
            pMethodInfos = NULL;
        }
        EX_END_CATCH(RethrowTerminalExceptions);


        if (pMethodInfos == NULL)
        {
            return E_OUTOFMEMORY;
        }

        if (InterlockedCompareExchangeT(&m_pMethodInfos, pMethodInfos, NULL) != NULL)
        {
            DeleteInteropSafe(pMethodInfos);
        }
    }

    return S_OK;
}

// Checks if the m_pModules table has been allocated, and if not does so.
HRESULT Debugger::CheckInitModuleTable()
{
    CONTRACT(HRESULT)
    {
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(m_pModules != NULL);
    }
    CONTRACT_END;

    if (m_pModules == NULL)
    {
        DebuggerModuleTable *pModules = new (interopsafe, nothrow) DebuggerModuleTable();

        if (pModules == NULL)
        {
            RETURN (E_OUTOFMEMORY);
        }

        if (InterlockedCompareExchangeT(&m_pModules, pModules, NULL) != NULL)
        {
            DeleteInteropSafe(pModules);
        }
    }

    RETURN (S_OK);
}

// Checks if the m_pModules table has been allocated, and if not does so.
HRESULT Debugger::CheckInitPendingFuncEvalTable()
{
    CONTRACT(HRESULT)
    {
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(GetPendingEvals() != NULL);
    }
    CONTRACT_END;

#ifndef DACCESS_COMPILE

    if (GetPendingEvals() == NULL)
    {
        DebuggerPendingFuncEvalTable *pPendingEvals = new (interopsafe, nothrow) DebuggerPendingFuncEvalTable();

        if (pPendingEvals == NULL)
        {
            RETURN(E_OUTOFMEMORY);
        }

        // Since we're setting, we need an LValue and not just an accessor.
        if (InterlockedCompareExchangeT(&(GetLazyData()->m_pPendingEvals), pPendingEvals, NULL) != NULL)
        {
            DeleteInteropSafe(pPendingEvals);
        }
    }
#endif

    RETURN (S_OK);
}


#ifdef _DEBUG_DMI_TABLE
// Returns the number of (official) entries in the table
ULONG DebuggerMethodInfoTable::CheckDmiTable(void)
{
    LIMITED_METHOD_CONTRACT;

    ULONG cApparent = 0;
    ULONG cOfficial = 0;

    if (NULL != m_pcEntries)
    {
        DebuggerMethodInfoEntry *dcp;
        int i = 0;
        while (i++ <m_iEntries)
        {
            dcp = (DebuggerMethodInfoEntry*)&(((DebuggerMethodInfoEntry *)m_pcEntries)[i]);
            if(dcp->pFD != 0 &&
               dcp->pFD != (MethodDesc*)0xcdcdcdcd &&
               dcp->mi != NULL)
            {
                cApparent++;

                _ASSERTE( dcp->pFD == dcp->mi->m_fd );
                LOG((LF_CORDB, LL_INFO1000, "DMIT::CDT:Entry:0x%p mi:0x%p\nPrevs:\n",
                    dcp, dcp->mi));
                DebuggerMethodInfo *dmi = dcp->mi->m_prevMethodInfo;

                while(dmi != NULL)
                {
                    LOG((LF_CORDB, LL_INFO1000, "\t0x%p\n", dmi));
                    dmi = dmi->m_prevMethodInfo;
                }
                dmi = dcp->mi->m_nextMethodInfo;

                LOG((LF_CORDB, LL_INFO1000, "Nexts:\n", dmi));
                while(dmi != NULL)
                {
                    LOG((LF_CORDB, LL_INFO1000, "\t0x%p\n", dmi));
                    dmi = dmi->m_nextMethodInfo;
                }

                LOG((LF_CORDB, LL_INFO1000, "DMIT::CDT:DONE\n",
                    dcp, dcp->mi));
            }
        }

        if (m_piBuckets == 0)
        {
            LOG((LF_CORDB, LL_INFO1000, "DMIT::CDT: The table is officially empty!\n"));
            return cOfficial;
        }

        LOG((LF_CORDB, LL_INFO1000, "DMIT::CDT:Looking for official entries:\n"));

        ULONG iNext = m_piBuckets[0];
        ULONG iBucket = 1;
        HASHENTRY   *psEntry = NULL;
        while (TRUE)
        {
            while (iNext != UINT32_MAX)
            {
                cOfficial++;

                psEntry = EntryPtr(iNext);
                dcp = ((DebuggerMethodInfoEntry *)psEntry);

                LOG((LF_CORDB, LL_INFO1000, "\tEntry:0x%p mi:0x%p @idx:0x%x @bucket:0x%x\n",
                    dcp, dcp->mi, iNext, iBucket));

                iNext = psEntry->iNext;
            }

            // Advance to the next bucket.
            if (iBucket < m_iBuckets)
                iNext = m_piBuckets[iBucket++];
            else
                break;
        }

        LOG((LF_CORDB, LL_INFO1000, "DMIT::CDT:Finished official entries: ****************"));
    }

    return cOfficial;
}
#endif // _DEBUG_DMI_TABLE


//---------------------------------------------------------------------------------------
//
// Class constructor for DebuggerEval.  This is the supporting data structure for
// func-eval tracking.
//
// Arguments:
//      pContext - The context to return to when done with this eval.
//      pEvalInfo - Contains all the important information, such as parameters, type args, method.
//      fInException - TRUE if the thread for the eval is currently in an exception notification.
//      bpInfoSegmentRX - bpInfoSegmentRX is an InteropSafe allocation allocated by the caller.
//                        (Caller allocated as there is no way to fail the allocation without
//                        throwing, and this function is called in a NOTHROW region)
//
DebuggerEval::DebuggerEval(CONTEXT * pContext, DebuggerIPCE_FuncEvalInfo * pEvalInfo, bool fInException, DebuggerEvalBreakpointInfoSegment* bpInfoSegmentRX)
{
    WRAPPER_NO_CONTRACT;

#if !defined(DBI_COMPILE) && !defined(DACCESS_COMPILE) && defined(HOST_OSX) && defined(HOST_ARM64)
    ExecutableWriterHolder<DebuggerEvalBreakpointInfoSegment> bpInfoSegmentWriterHolder(bpInfoSegmentRX, sizeof(DebuggerEvalBreakpointInfoSegment));
    DebuggerEvalBreakpointInfoSegment *bpInfoSegmentRW = bpInfoSegmentWriterHolder.GetRW();
#else // !DBI_COMPILE && !DACCESS_COMPILE && HOST_OSX && HOST_ARM64
    DebuggerEvalBreakpointInfoSegment *bpInfoSegmentRW = bpInfoSegmentRX;
#endif // !DBI_COMPILE && !DACCESS_COMPILE && HOST_OSX && HOST_ARM64
    new (bpInfoSegmentRW) DebuggerEvalBreakpointInfoSegment(this);
    m_bpInfoSegment = bpInfoSegmentRX;

    // This must be non-zero so that the saved opcode is non-zero, and on IA64 we want it to be 0x16
    // so that we can have a breakpoint instruction in any slot in the bundle.
    bpInfoSegmentRW->m_breakpointInstruction[0] = 0x16;
#if defined(TARGET_ARM)
    USHORT *bp = (USHORT*)&m_bpInfoSegment->m_breakpointInstruction;
    *bp = CORDbg_BREAK_INSTRUCTION;
#endif // TARGET_ARM
    m_thread = pEvalInfo->vmThreadToken.GetRawPtr();
    m_evalType = pEvalInfo->funcEvalType;
    m_methodToken = pEvalInfo->funcMetadataToken;
    m_classToken = pEvalInfo->funcClassMetadataToken;

    // Note: we can't rely on just the DebuggerModule* or AppDomain* because the AppDomain
    // could get unloaded between now and when the funceval actually starts.  So we stash an
    // AppDomain ID which is safe to use after the AD is unloaded.  It's only safe to
    // use the DebuggerModule* after we've verified the ADID is still valid (i.e. by entering that domain).
    m_debuggerModule = g_pDebugger->LookupOrCreateModule(pEvalInfo->vmDomainFile);
    m_funcEvalKey = pEvalInfo->funcEvalKey;
    m_argCount = pEvalInfo->argCount;
    m_targetCodeAddr = NULL;
    m_stringSize = pEvalInfo->stringSize;
    m_arrayRank = pEvalInfo->arrayRank;
    m_genericArgsCount = pEvalInfo->genericArgsCount;
    m_genericArgsNodeCount = pEvalInfo->genericArgsNodeCount;
    m_successful = false;
    m_argData = NULL;
    memset(m_result, 0, sizeof(m_result));
    m_md = NULL;
    m_resultType = TypeHandle();
    m_aborting = FE_ABORT_NONE;
    m_aborted = false;
    m_completed = false;
    m_evalDuringException = fInException;
    m_retValueBoxing = Debugger::NoValueTypeBoxing;
    m_vmObjectHandle = VMPTR_OBJECTHANDLE::NullPtr();

    // Copy the thread's context.
    if (pContext == NULL)
    {
        memset(&m_context, 0, sizeof(m_context));
    }
    else
    {
        memcpy(&m_context, pContext, sizeof(m_context));
    }
}

#ifdef _DEBUG
// Thread proc for interop stress coverage. Have an unmanaged thread
// that just loops throwing native exceptions. This can test corner cases
// such as getting an native exception while the runtime is synced.
DWORD WINAPI DbgInteropStressProc(void * lpParameter)
{
    LIMITED_METHOD_CONTRACT;

    int i = 0;
    int zero = 0;


    // This will ensure that the compiler doesn't flag our 1/0 exception below at compile-time.
    if (lpParameter != NULL)
    {
        zero = 1;
    }

    // Note that this thread is a non-runtime thread. So it can't take any CLR locks
    // or do anything else that may block the helper thread.
    // (Log statements take CLR locks).
    while(true)
    {
        i++;

        if ((i % 10) != 0)
        {
            // Generate an in-band event.
            PAL_CPP_TRY
            {
                // Throw a handled exception. Don't use an AV since that's pretty special.
                *(int*)lpParameter = 1 / zero;
            }
            PAL_CPP_CATCH_ALL
            {
            }
            PAL_CPP_ENDTRY
        }
        else
        {
            // Generate the occasional oob-event.
            WszOutputDebugString(W("Ping from DbgInteropStressProc"));
        }

        // This helps parallelize if we have a lot of threads, and keeps us from
        // chewing too much CPU time.
        ClrSleepEx(2000,FALSE);
        ClrSleepEx(GetRandomInt(1000), FALSE);
    }

    return 0;
}

// ThreadProc that does everything in a can't stop region.
DWORD WINAPI DbgInteropCantStopStressProc(void * lpParameter)
{
    WRAPPER_NO_CONTRACT;

    // This will mark us as a can't stop region.
    ClrFlsSetThreadType (ThreadType_DbgHelper);

    return DbgInteropStressProc(lpParameter);
}

// Generate lots of OOB events.
DWORD WINAPI DbgInteropDummyStressProc(void * lpParameter)
{
    LIMITED_METHOD_CONTRACT;

    ClrSleepEx(1,FALSE);
    return 0;
}

DWORD WINAPI DbgInteropOOBStressProc(void * lpParameter)
{
    WRAPPER_NO_CONTRACT;

    int i = 0;
    while(true)
    {
        i++;
        if (i % 10 == 1)
        {
            // Create a dummy thread. That generates 2 oob events
            // (1 for create, 1 for destroy)
            DWORD id;
            ::CreateThread(NULL, 0, DbgInteropDummyStressProc, NULL, 0, &id);
        }
        else
        {
            // Generate the occasional oob-event.
            WszOutputDebugString(W("OOB ping from "));
        }

        ClrSleepEx(3000, FALSE);
    }

    return 0;
}

// List of the different possible stress procs.
LPTHREAD_START_ROUTINE g_pStressProcs[] =
{
    DbgInteropOOBStressProc,
    DbgInteropCantStopStressProc,
    DbgInteropStressProc
};
#endif


DebuggerHeap * Debugger::GetInteropSafeHeap()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Lazily initialize our heap.
    if (!m_heap.IsInit())
    {
        _ASSERTE(!"InteropSafe Heap should have already been initialized in LazyInit");

        // Just in case we miss it in retail, convert to OOM here:
        ThrowOutOfMemory();
    }

    return &m_heap;
}

DebuggerHeap * Debugger::GetInteropSafeHeap_NoThrow()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Lazily initialize our heap.
    if (!m_heap.IsInit())
    {
        _ASSERTE(!"InteropSafe Heap should have already been initialized in LazyInit");

        // Just in case we miss it in retail, convert to OOM here:
        return NULL;
    }
    return &m_heap;
}

DebuggerHeap * Debugger::GetInteropSafeExecutableHeap()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Lazily initialize our heap.
    if (!m_executableHeap.IsInit())
    {
        _ASSERTE(!"InteropSafe Executable Heap should have already been initialized in LazyInit");

        // Just in case we miss it in retail, convert to OOM here:
        ThrowOutOfMemory();
    }

    return &m_executableHeap;
}

DebuggerHeap * Debugger::GetInteropSafeExecutableHeap_NoThrow()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Lazily initialize our heap.
    if (!m_executableHeap.IsInit())
    {
        _ASSERTE(!"InteropSafe Executable Heap should have already been initialized in LazyInit");

        // Just in case we miss it in retail, convert to OOM here:
        return NULL;
    }
    return &m_executableHeap;
}

//---------------------------------------------------------------------------------------
//
// Notify potential debugger that the runtime has started up
//
//
// Assumptions:
//    Called during startup path
//
// Notes:
//    If no debugger is attached, this does nothing.
//
//---------------------------------------------------------------------------------------
void Debugger::RaiseStartupNotification()
{
    // Right-side will read this field from OOP via DAC-primitive to determine attach or launch case.
    // We do an interlocked increment to gaurantee this is an atomic memory write, and to ensure
    // that it's flushed from any CPU cache into memory.
    InterlockedIncrement(&m_fLeftSideInitialized);

#ifndef FEATURE_DBGIPC_TRANSPORT_VM
    // If we are remote debugging, don't send the event now if a debugger is not attached.  No one will be
    // listening, and we will fail.  However, we still want to initialize the variable above.
    DebuggerIPCEvent startupEvent;
    InitIPCEvent(&startupEvent, DB_IPCE_LEFTSIDE_STARTUP, NULL, VMPTR_AppDomain::NullPtr());

    SendRawEvent(&startupEvent);

    // RS will set flags from OOP while we're stopped at the event if it wants to attach.
#endif // FEATURE_DBGIPC_TRANSPORT_VM
}


//---------------------------------------------------------------------------------------
//
// Sends a raw managed debug event to the debugger.
//
// Arguments:
//      pManagedEvent - managed debug event
//
//
// Notes:
//    This can be called even if a debugger is not attached.
//    The entire process will get frozen by the debugger once we send.  The debugger
//    needs to resume the process. It may detach as well.
//    See code:IsEventDebuggerNotification for decoding this event. These methods must stay in sync.
//    The debugger process reads the events via code:CordbProcess.CopyManagedEventFromTarget.
//
//---------------------------------------------------------------------------------------
void Debugger::SendRawEvent(const DebuggerIPCEvent * pManagedEvent)
{
#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
    HRESULT hr = g_pDbgTransport->SendDebugEvent(const_cast<DebuggerIPCEvent *>(pManagedEvent));

    if (FAILED(hr))
    {
        _ASSERTE(!"Failed to send debugger event");

        STRESS_LOG1(LF_CORDB, LL_INFO1000, "D::SendIPCEvent Error on Send with 0x%x\n", hr);
        UnrecoverableError(hr,
            0,
            FILE_DEBUG,
            LINE_DEBUG,
            false);

        // @dbgtodo  Mac - what can we do here?
    }
#else
    // We get to send an array of ULONG_PTRs as data with the notification.
    // The debugger can then use ReadProcessMemory to read through this array.
    ULONG_PTR rgData [] = {
        CLRDBG_EXCEPTION_DATA_CHECKSUM,
        (ULONG_PTR)GetClrModuleBase(),
        (ULONG_PTR) pManagedEvent
    };

    // If no debugger attached, then don't bother raising a 1st-chance exception because nobody will sniff it.
    // @dbgtodo iDNA: in iDNA case, the recorder may sniff it.
    if (!IsDebuggerPresent())
    {
        return;
    }

    //
    // Physically send the event via an OS Exception. We're using exceptions as a notification
    // mechanism on top of the OS native debugging pipeline.
    // @dbgtodo  cross-plat - this needs to be cross-plat.
    //
    EX_TRY
    {
        const DWORD dwFlags = 0; // continuable (eg, Debugger can continue GH)
        RaiseException(CLRDBG_NOTIFICATION_EXCEPTION_CODE, dwFlags, NumItems(rgData), rgData);

        // If debugger continues "GH" (DBG_CONTINUE), then we land here.
        // This is the expected path for a well-behaved ICorDebug debugger.
    }
    EX_CATCH
    {
        // If no debugger is attached, or if the debugger continues "GN" (DBG_EXCEPTION_NOT_HANDLED), then we land here.
        // A naive (not-ICorDebug aware) native-debugger won't handle the exception and so land us here.
        // We may also get here if a debugger detaches at the Exception notification
        // (and thus implicitly continues GN).
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif // FEATURE_DBGIPC_TRANSPORT_VM
}

//---------------------------------------------------------------------------------------
// Send a createProcess event to give the RS a chance to do SetDesiredNGENFlags
//
// Arguments:
//    pDbgLockHolder - lock holder.
//
// Assumptions:
//    Lock is initially held. This will toggle the lock to send an IPC event.
//    This will start a synchronization.
//
// Notes:
//    In V2, this also gives the RS a chance to intialize the IPC protocol.
//    Spefically, this needs to be sent before the LS can send a sync-complete.
//---------------------------------------------------------------------------------------
void Debugger::SendCreateProcess(DebuggerLockHolder * pDbgLockHolder)
{
    pDbgLockHolder->Release();

    // Encourage helper thread to spin up so that we're in a consistent state.
    PollWaitingForHelper();

    // we don't need to use SENDIPCEVENT_BEGIN/END macros that perform the debug-suspend aware checks,
    // as this code executes on the startup path...
    SENDIPCEVENT_RAW_BEGIN(pDbgLockHolder);

    // Send a CreateProcess event.
    // @dbgtodo  pipeline - eliminate these reasons for needing a CreateProcess event (part of pipeline feature crew)
    // This will let the RS know that the IPC block is up + ready, and then the RS can read it.
    // The RS will then update the DCB with enough information so that we can send the sync-complete.
    // (such as letting us know whether we're interop-debugging or not).
    DebuggerIPCEvent event;
    InitIPCEvent(&event, DB_IPCE_CREATE_PROCESS, NULL, VMPTR_AppDomain::NullPtr());
    SendRawEvent(&event);

    // @dbgtodo  inspection- it doesn't really make sense to sync on a CreateProcess. We only have 1 thread
    // in the CLR and we know exactly what state we're in and we can ensure that we're synchronized.
    // For V3,RS should be able to treat a CreateProcess like a synchronized.
    // Remove this in V3 as we make SetDesiredNgenFlags operate OOP.
    TrapAllRuntimeThreads();

    // Must have a thread object so that we ensure that we will actually block here.
    // This ensures the debuggee is actually stopped at startup, and
    // this gives the debugger a chance to call SetDesiredNGENFlags before we
    // set s_fCanChangeNgenFlags to FALSE.
    _ASSERTE(GetThreadNULLOk() != NULL);
    SENDIPCEVENT_RAW_END;

    pDbgLockHolder->Acquire();
}

#if !defined(TARGET_UNIX)

HANDLE g_hContinueStartupEvent = INVALID_HANDLE_VALUE;

CLR_ENGINE_METRICS g_CLREngineMetrics = {
    sizeof(CLR_ENGINE_METRICS),
    CorDebugVersion_4_0,
    &g_hContinueStartupEvent};

#define StartupNotifyEventNamePrefix W("TelestoStartupEvent_")
const int cchEventNameBufferSize = sizeof(StartupNotifyEventNamePrefix)/sizeof(WCHAR) + 8; // + hex DWORD (8).  NULL terminator is included in sizeof(StartupNotifyEventNamePrefix)
HANDLE OpenStartupNotificationEvent()
{
    DWORD debuggeePID = GetCurrentProcessId();
    WCHAR szEventName[cchEventNameBufferSize];
    swprintf_s(szEventName, cchEventNameBufferSize, StartupNotifyEventNamePrefix W("%08x"), debuggeePID);

    return WszOpenEvent(MAXIMUM_ALLOWED | SYNCHRONIZE | EVENT_MODIFY_STATE, FALSE, szEventName);
}

void NotifyDebuggerOfStartup()
{
    // Create the continue event first so that we guarantee that any
    // enumeration of this process will get back a valid continue event
    // the instant we signal the startup notification event.

    CONSISTENCY_CHECK(INVALID_HANDLE_VALUE == g_hContinueStartupEvent);
    g_hContinueStartupEvent = WszCreateEvent(NULL, TRUE, FALSE, NULL);
    CONSISTENCY_CHECK(INVALID_HANDLE_VALUE != g_hContinueStartupEvent); // we reserve this value for error conditions in EnumerateCLRs

    HANDLE startupEvent = OpenStartupNotificationEvent();
    if (startupEvent != NULL)
    {
        // signal notification event
        SetEvent(startupEvent);
        CloseHandle(startupEvent);
        startupEvent = NULL;

        // wait on continue startup event
        // The debugger may attach to us while we're blocked here.
        WaitForSingleObject(g_hContinueStartupEvent, INFINITE);
    }

    CloseHandle(g_hContinueStartupEvent);
    g_hContinueStartupEvent = NULL;
}

#endif // !TARGET_UNIX

void Debugger::CleanupTransportSocket(void)
{
#if defined(TARGET_UNIX) && defined(FEATURE_DBGIPC_TRANSPORT_VM)
    if (g_pDbgTransport != NULL)
    {
        g_pDbgTransport->AbortConnection();
    }
#endif // TARGET_UNIX && FEATURE_DBGIPC_TRANSPORT_VM
}

//---------------------------------------------------------------------------------------
//
// Initialize Left-Side debugger object
//
// Return Value:
//    S_OK on success. May also throw.
//
// Assumptions:
//    This is called in the startup path.
//
// Notes:
// Startup initializes any necessary debugger objects, including creating
// and starting the Runtime Controller thread. Once the RC thread is started
// and we return successfully, the Debugger object can expect to have its
// event handlers called.
//
//---------------------------------------------------------------------------------------
HRESULT Debugger::Startup(void)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    _ASSERTE(g_pEEInterface != NULL);

#if !defined(TARGET_UNIX)
    // This may block while an attach occurs.
    NotifyDebuggerOfStartup();
#endif // !TARGET_UNIX
    {
        DebuggerLockHolder dbgLockHolder(this);

        // Stubs in Stacktraces are always enabled.
        g_EnableSIS = true;

        // We can get extra Interop-debugging test coverage by having some auxillary unmanaged
        // threads running and throwing debug events. Keep these stress procs separate so that
        // we can focus on certain problem areas.
    #ifdef _DEBUG
        g_DbgShouldntUseDebugger = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgNoDebugger) != 0;


        // Creates random thread procs.
        DWORD dwRegVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgExtraThreads);
        DWORD dwId;
        DWORD i;

        if (dwRegVal > 0)
        {
            for (i = 0; i < dwRegVal; i++)
            {
                int iProc = GetRandomInt(NumItems(g_pStressProcs));
                LPTHREAD_START_ROUTINE pStartRoutine = g_pStressProcs[iProc];
                ::CreateThread(NULL, 0, pStartRoutine, NULL, 0, &dwId);
                LOG((LF_CORDB, LL_INFO1000, "Created random thread (%d) with tid=0x%x\n", i, dwId));
            }
        }

        dwRegVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgExtraThreadsIB);
        if (dwRegVal > 0)
        {
            for (i = 0; i < dwRegVal; i++)
            {
                ::CreateThread(NULL, 0, DbgInteropStressProc, NULL, 0, &dwId);
                LOG((LF_CORDB, LL_INFO1000, "Created extra thread (%d) with tid=0x%x\n", i, dwId));
            }
        }

        dwRegVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgExtraThreadsCantStop);
        if (dwRegVal > 0)
        {
            for (i = 0; i < dwRegVal; i++)
            {
                ::CreateThread(NULL, 0, DbgInteropCantStopStressProc, NULL, 0, &dwId);
                LOG((LF_CORDB, LL_INFO1000, "Created extra thread 'can't-stop' (%d) with tid=0x%x\n", i, dwId));
            }
        }

        dwRegVal = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgExtraThreadsOOB);
        if (dwRegVal > 0)
        {
            for (i = 0; i < dwRegVal; i++)
            {
                ::CreateThread(NULL, 0, DbgInteropOOBStressProc, NULL, 0, &dwId);
                LOG((LF_CORDB, LL_INFO1000, "Created extra thread OOB (%d) with tid=0x%x\n", i, dwId));
            }
        }
    #endif

        // Lazily initialize the interop-safe heap

        // Must be done before the RC thread is initialized.
        // @dbgtodo  - In V2, LS was lazily initialized; but was eagerly pre-initialized if launched by debugger.
        // (This was for perf reasons). But we don't want Launch vs. Attach checks in the LS, so we now always
        // init. As we move more to OOP, this init will become cheaper.
        {
            LazyInit();
            DebuggerController::Initialize();
        }

        InitializeHijackFunctionAddress();

        // Also initialize the AppDomainEnumerationIPCBlock
    #if !defined(FEATURE_IPCMAN) || defined(FEATURE_DBGIPC_TRANSPORT_VM)
        m_pAppDomainCB = new (nothrow) AppDomainEnumerationIPCBlock();
    #else
        m_pAppDomainCB = g_pIPCManagerInterface->GetAppDomainBlock();
    #endif

        if (m_pAppDomainCB == NULL)
        {
            LOG((LF_CORDB, LL_INFO100, "D::S: Failed to get AppDomain IPC block from IPCManager.\n"));
            ThrowHR(E_FAIL);
        }

        hr = InitAppDomainIPC();
        _ASSERTE(SUCCEEDED(hr)); // throws on error.

        // Allows the debugger (and profiler) diagnostics to be disabled so resources like
        // the named pipes and semaphores are not created.
        if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_EnableDiagnostics) == 0)
        {
            return S_OK;
        }

        // Create the runtime controller thread, a.k.a, the debug helper thread.
        // Don't use the interop-safe heap b/c we don't want to lazily create it.
        m_pRCThread = new DebuggerRCThread(this);
        _ASSERTE(m_pRCThread != NULL); // throws on oom
        TRACE_ALLOC(m_pRCThread);

        hr = m_pRCThread->Init();
        _ASSERTE(SUCCEEDED(hr)); // throws on error

    #if defined(FEATURE_DBGIPC_TRANSPORT_VM)
         // Create transport session and initialize it.
        g_pDbgTransport = new DbgTransportSession();
        hr = g_pDbgTransport->Init(m_pRCThread->GetDCB(), m_pAppDomainCB);
        if (FAILED(hr))
        {
            ShutdownTransport();
            ThrowHR(hr);
        }
    #endif // FEATURE_DBGIPC_TRANSPORT_VM

        RaiseStartupNotification();

        // See if we need to spin up the helper thread now, rather than later.
        DebuggerIPCControlBlock* pIPCControlBlock = m_pRCThread->GetDCB();
        (void)pIPCControlBlock; //prevent "unused variable" error from GCC

        _ASSERTE(pIPCControlBlock != NULL);
        _ASSERTE(!pIPCControlBlock->m_rightSideShouldCreateHelperThread);
        {
            // Create the win32 thread for the helper and let it run free.
            hr = m_pRCThread->Start();

            // convert failure to exception as with old contract
            if (FAILED(hr))
            {
                ThrowHR(hr);
            }

            LOG((LF_CORDB, LL_EVERYTHING, "Start was successful\n"));
        }

    #ifdef TEST_DATA_CONSISTENCY
        // if we have set the environment variable TestDataConsistency, run the data consistency test.
        // See code:DataTest::TestDataSafety for more information
        if ((g_pConfig != NULL) && (g_pConfig->TestDataConsistency() == true))
        {
            DataTest dt;
            dt.TestDataSafety();
        }
    #endif
    }

#ifdef TARGET_UNIX
    // Signal the debugger (via dbgshim) and wait until it is ready for us to
    // continue. This needs to be outside the lock and after the transport is
    // initialized.
    if (PAL_NotifyRuntimeStarted())
    {
        // The runtime was successfully launched and attached so mark it now
        // so no notifications are missed especially the initial module load
        // which would cause debuggers problems with reliable setting breakpoints
        // in startup code or Main.
       MarkDebuggerAttachedInternal();
    }
#endif // TARGET_UNIX

    // We don't bother changing this process's permission.
    // A managed debugger will have the SE_DEBUG permission which will allow it to open our process handle,
    // even if we're a guest account.

    return hr;
}

//---------------------------------------------------------------------------------------
// Finishes startup once we have a Thread object.
//
// Arguments:
//    pThread - the current thread. Must be non-null
//
// Notes:
//    Most debugger initialization is done in code:Debugger.Startup,
//    However, debugger can't block on synchronization without a Thread object,
//    so sending IPC events must wait until after we have a thread object.
//---------------------------------------------------------------------------------------
HRESULT Debugger::StartupPhase2(Thread * pThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Must have a thread so that we can block
    _ASSERTE(pThread != NULL);

    DebuggerLockHolder dbgLockHolder(this);

    // @dbgtodo  - This may need to change when we remove SetupSyncEvent...
    // If we're launching, then sync now so that the RS gets an early chance to dispatch the CreateProcess event.
    // This is especially important b/c certain portions of the ICorDebugAPI (like setting ngen flags) are only
    // valid during the CreateProcess callback in the launch case.
    // We need to send the callback early enough so those APIs can set the flags before they're actually used.
    // We also ensure the debugger is actually attached.
    if (SUCCEEDED(hr) && CORDebuggerAttached())
    {
        StartCanaryThread();
        SendCreateProcess(&dbgLockHolder); // toggles lock
    }

    // After returning from debugger startup we assume that the runtime might start using the NGEN flags to make
    // binding decisions. From now on the debugger can not influence NGEN binding policy
    // Use volatile store to guarantee make the value visible to the DAC (the store can be optimized out otherwise)
    VolatileStoreWithoutBarrier(&s_fCanChangeNgenFlags, FALSE);

    // Must release the lock (which would be done at the end of this method anyways) so that
    // the helper thread can do the jit-attach.
    dbgLockHolder.Release();


#ifdef _DEBUG
    // Give chance for stress harnesses to launch a managed debugger when a managed app starts up.
    // This lets us run a set of managed apps under a debugger.
    if (!CORDebuggerAttached())
    {
        #define DBG_ATTACH_ON_STARTUP_ENV_VAR W("COMPlus_DbgAttachOnStartup")
        PathString temp;
        // We explicitly just check the env because we don't want a switch this invasive to be global.
        DWORD fAttach = WszGetEnvironmentVariable(DBG_ATTACH_ON_STARTUP_ENV_VAR, temp) > 0;

        if (fAttach)
        {
            // Remove the env var from our process so that the debugger we spin up won't inherit it.
            // Else, if the debugger is managed, we'll have an infinite recursion.
            BOOL fOk = WszSetEnvironmentVariable(DBG_ATTACH_ON_STARTUP_ENV_VAR, NULL);

            if (fOk)
            {
                // We've already created the helper thread (which can service the attach request)
                // So just do a normal jit-attach now.

                SString szName(W("DebuggerStressStartup"));
                SString szDescription(W("MDA used for debugger-stress scenario. This is fired to trigger a jit-attach")
                    W("to allow us to attach a debugger to any managed app that starts up.")
                    W("This MDA is only fired when the 'DbgAttachOnStartup' COM+ knob/reg-key is set on checked builds."));
                SString szXML(W("<xml>See the description</xml>"));

                SendMDANotification(
                    NULL, // NULL b/c we don't have a thread yet
                    &szName,
                    &szDescription,
                    &szXML,
                    ((CorDebugMDAFlags) 0 ),
                    TRUE // this will force the jit-attach
                );
            }
        }
    }
#endif


    return hr;
}


//---------------------------------------------------------------------------------------
//
// Public entrypoint into the debugger to force the lazy data to be initialized at a
// controlled point in time. This is useful for those callers into the debugger (e.g.,
// ETW rundown) that know they will need the lazy data initialized but cannot afford to
// have it initialized unpredictably or inside a lock.
//
// This may be called more than once, and will know to initialize the lazy data only
// once.
//

void Debugger::InitializeLazyDataIfNecessary()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (!HasLazyData())
    {
        DebuggerLockHolder lockHolder(this);
        LazyInit(); // throws
    }
}


/******************************************************************************
Lazy initialize stuff once we know we are debugging.
This reduces the startup cost in the non-debugging case.

We can do this at a bunch of random strategic places.
 ******************************************************************************/

HRESULT Debugger::LazyInitWrapper()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Do lazy initialization now.
    EX_TRY
    {
        LazyInit(); // throws on errors.
    }
    EX_CATCH
    {
        Exception *_ex = GET_EXCEPTION();
        hr = _ex->GetHR();
        STRESS_LOG1(LF_CORDB, LL_ALWAYS, "LazyInit failed w/ hr:0x%08x\n", hr);
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

void Debugger::LazyInit()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(ThreadHoldsLock()); // ensure we're serialized, requires GC_NOTRIGGER

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    // Have knob that catches places where we lazy init.
    _ASSERTE(!g_DbgShouldntUseDebugger);

    // If we're already init, then bail.
    if (m_pLazyData != NULL)
    {
        return;
    }




    // Lazily create our heap.
    HRESULT hr = m_heap.Init(FALSE);
    IfFailThrow(hr);

    hr = m_executableHeap.Init(TRUE);
    IfFailThrow(hr);

    m_pLazyData = new (interopsafe) DebuggerLazyInit();
    _ASSERTE(m_pLazyData != NULL); // throws on oom.

    m_pLazyData->Init();

}

HelperThreadFavor::HelperThreadFavor() :
    m_fpFavor(NULL),
    m_pFavorData(NULL),
    m_FavorReadEvent(NULL),
    m_FavorLock(CrstDebuggerFavorLock, CRST_DEFAULT),
    m_FavorAvailableEvent(NULL)
{
}

void HelperThreadFavor::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    // Create events for managing favors.
    m_FavorReadEvent      = CreateWin32EventOrThrow(NULL, kAutoResetEvent, FALSE);
    m_FavorAvailableEvent = CreateWin32EventOrThrow(NULL, kAutoResetEvent, FALSE);
}



DebuggerLazyInit::DebuggerLazyInit() :
    m_pPendingEvals(NULL),
    // @TODO: a-meicht
    // Major clean up needed for giving the right flag
    // There are cases where DebuggerDataLock is taken by managed thread and unmanaged trhead is also trying to take it.
    // It could cause deadlock if we toggle GC upon taking lock.
    // Unfortunately UNSAFE_COOPGC is not enough. There is a code path in Jit comipling that we are in GC Preemptive
    // enabled. workaround by orring the unsafe_anymode flag. But we really need to do proper clean up.
    //
    // NOTE: If this ever gets fixed, you should replace CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT
    // with appropriate contracts at each site.
    //
    m_DebuggerDataLock(CrstDebuggerJitInfo, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_REENTRANCY | CRST_DEBUGGER_THREAD)),
    m_CtrlCMutex(NULL),
    m_exAttachEvent(NULL),
    m_exUnmanagedAttachEvent(NULL),
    m_garbageCollectionBlockerEvent(NULL),
    m_DebuggerHandlingCtrlC(NULL)
{
}

void DebuggerLazyInit::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    // Caller ensures this isn't double-called.

    // This event is only used in the unmanaged attach case.  We must mark this event handle as inheritable.
    // Otherwise, the unmanaged debugger won't be able to notify us.
    //
    // Note that PAL currently doesn't support specifying the security attributes when creating an event, so
    // unmanaged attach for unhandled exceptions is broken on PAL.
    SECURITY_ATTRIBUTES* pSA = NULL;
    SECURITY_ATTRIBUTES secAttrib;
    secAttrib.nLength              = sizeof(secAttrib);
    secAttrib.lpSecurityDescriptor = NULL;
    secAttrib.bInheritHandle       = TRUE;

    pSA = &secAttrib;

    // Create some synchronization events...
    // these events stay signaled all the time except when an attach is in progress
    m_exAttachEvent          = CreateWin32EventOrThrow(NULL, kManualResetEvent, TRUE);
    m_exUnmanagedAttachEvent = CreateWin32EventOrThrow(pSA,  kManualResetEvent, TRUE);

    m_CtrlCMutex             = CreateWin32EventOrThrow(NULL, kAutoResetEvent, FALSE);
    m_DebuggerHandlingCtrlC  = FALSE;

    m_garbageCollectionBlockerEvent = CreateEventW(NULL, TRUE, FALSE, NULL);

    // Let the helper thread lazy init stuff too.
    m_RCThread.Init();
}


DebuggerLazyInit::~DebuggerLazyInit()
{
    {
        USHORT cBlobs = m_pMemBlobs.Count();
        void **rgpBlobs = m_pMemBlobs.Table();

        for (int i = 0; i < cBlobs; i++)
        {
            g_pDebugger->ReleaseRemoteBuffer(rgpBlobs[i], false);
        }
    }

    if (m_pPendingEvals)
    {
        DeleteInteropSafe(m_pPendingEvals);
        m_pPendingEvals = NULL;
    }

    if (m_CtrlCMutex != NULL)
    {
        CloseHandle(m_CtrlCMutex);
    }

    if (m_exAttachEvent != NULL)
    {
        CloseHandle(m_exAttachEvent);
    }

    if (m_exUnmanagedAttachEvent != NULL)
    {
        CloseHandle(m_exUnmanagedAttachEvent);
    }

    if (m_garbageCollectionBlockerEvent != NULL)
    {
        CloseHandle(m_garbageCollectionBlockerEvent);
    }
}


//
// RequestFavor gets the debugger helper thread to call a function. It's
// typically called when the current thread can't call the function directly,
// e.g, there isn't enough stack space.
//
// RequestFavor can be called in stack-overflow scenarios and thus explicitly
// avoids any lazy initialization.
// It blocks until the favor callback completes.
//
// Parameters:
//   fp    - a non-null Favour callback function
//   pData - the parameter passed to the favor callback function. This can be any value.
//
// Return values:
//   S_OK if the function succeeds, else a failure HRESULT
//

HRESULT Debugger::RequestFavor(FAVORCALLBACK fp, void * pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        PRECONDITION(fp != NULL);
    }
    CONTRACTL_END;

    if (m_pRCThread == NULL ||
        m_pRCThread->GetRCThreadId() == GetCurrentThreadId())
    {
        // Since favors are only used internally, we know that the helper should alway be up and ready
        // to handle them. Also, since favors can be used in low-stack scenarios, there's not any
        // extra initialization needed for them.
        _ASSERTE(!"Helper not initialized for favors.");
        return E_UNEXPECTED;
    }

    m_pRCThread->DoFavor(fp, pData);
    return S_OK;
}

/******************************************************************************
// Called to set the interface that the Runtime exposes to us.
 ******************************************************************************/
void Debugger::SetEEInterface(EEDebugInterface* i)
{
    LIMITED_METHOD_CONTRACT;

    // @@@

    // Implements DebugInterface API

    g_pEEInterface = i;

}


/******************************************************************************
// Called to shut down the debugger. This stops the RC thread and cleans
// the object up.
 ******************************************************************************/
void Debugger::StopDebugger(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Leak almost everything on process exit. The OS will clean it up anyways and trying to
    // clean it up ourselves is just one more place we may AV / deadlock.

#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
    ShutdownTransport();
#endif // FEATURE_DBGIPC_TRANSPORT_VM

    // Ping the helper thread to exit. This will also prevent the helper from servicing new requests.
    if (m_pRCThread != NULL)
    {
        m_pRCThread->AsyncStop();
    }

    // Also clean up the AppDomain stuff since this is cross-process.
    TerminateAppDomainIPC ();

    //
    // Tell the VM to clear out all references to the debugger before we start cleaning up,
    // so that nothing will reference (accidentally) through the partially cleaned up debugger.
    //
    // NOTE: we cannot clear out g_pDebugger before the delete call because the
    // stuff in delete (particularly deleteinteropsafe) needs to look at it.
    //
    g_pEEInterface->ClearAllDebugInterfaceReferences();
    g_pDebugger = NULL;
}


/* ------------------------------------------------------------------------ *
 * JIT Interface routines
 * ------------------------------------------------------------------------ */


/******************************************************************************
 *
 ******************************************************************************/
DebuggerMethodInfo *Debugger::CreateMethodInfo(Module *module, mdMethodDef md)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(HasDebuggerDataLock());
    }
    CONTRACTL_END;


    // <TODO>@todo perf: creating these on the heap is slow. We should use a
    // pool and create them out of there since we never free them
    // until the AD is unloaded.</TODO>
    //
    DebuggerMethodInfo *mi = new (interopsafe) DebuggerMethodInfo(module, md);
    _ASSERTE(mi != NULL); // throws on oom error

    TRACE_ALLOC(mi);

    LOG((LF_CORDB, LL_INFO100000, "D::CreateMethodInfo module=%p, token=0x%08x, info=%p\n",
        module, md, mi));

    //
    // Lock a mutex when changing the table.
    //
    //@TODO : _ASSERTE(EnC);
    HRESULT hr;
    hr =InsertToMethodInfoList(mi);

    if (FAILED(hr))
    {
        LOG((LF_CORDB, LL_EVERYTHING, "IAHOL Failed!!\n"));
        DeleteInteropSafe(mi);
        return NULL;
    }
    return mi;

}





/******************************************************************************
// void Debugger::JITComplete():   JITComplete is called by
// the jit interface when the JIT completes, successfully or not.
//
// MethodDesc* fd:  MethodDesc of the code that's been JITted
// BYTE* newAddress:  The address of that the method begins at.
//          If newAddress is NULL then the JIT failed. Remember that this
//          gets called before the start address of the MethodDesc gets set,
//          and so methods like GetFunctionAddress & GetFunctionSize won't work.
//
// <TODO>@Todo If we're passed 0 for the newAddress param, the jit has been
//      cancelled & should be undone.</TODO>
 ******************************************************************************/
void Debugger::JITComplete(NativeCodeVersion nativeCodeVersion, TADDR newAddress)
{

    CONTRACTL
    {
        THROWS;
        PRECONDITION(!HasDebuggerDataLock());
        PRECONDITION(newAddress != NULL);
        CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    MethodDesc* fd = nativeCodeVersion.GetMethodDesc();

    LOG((LF_CORDB, LL_INFO100000, "D::JITComplete: md:0x%x (%s::%s), address:0x%x.\n",
        fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName,
        newAddress));

#ifdef TARGET_ARM
    newAddress = newAddress|THUMB_CODE;
#endif

    // @@@
    // Can be called on managed thread only
    // This API Implements DebugInterface

    if (CORDebuggerAttached())
    {
        // Populate the debugger's cache of DJIs. Normally we can do this lazily,
        // the only reason we do it here is b/c the MethodDesc is not yet officially marked as "jitted",
        // and so we can't lazily create it yet. Furthermore, the binding operations may need the DJIs.
        //
        // This also gives the debugger a chance to know if new JMC methods are coming.
        DebuggerMethodInfo * dmi = GetOrCreateMethodInfo(fd->GetModule(), fd->GetMemberDef());
        if (dmi == NULL)
        {
            goto Exit;
        }
        BOOL jiWasCreated = FALSE;
        DebuggerJitInfo * ji = dmi->CreateInitAndAddJitInfo(nativeCodeVersion, newAddress, &jiWasCreated);
        if (!jiWasCreated)
        {
            // we've already been notified about this code, no work remains.
            // The JIT is occasionally asked to generate code for the same
            // method on two threads. When this occurs both threads will
            // return the same code pointer and this callback is invoked
            // multiple times.
            LOG((LF_CORDB, LL_INFO1000000, "D::JITComplete: md:0x%x (%s::%s), address:0x%x. Already created\n",
                fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName,
                newAddress));
            goto Exit;
        }

        LOG((LF_CORDB, LL_INFO1000000, "D::JITComplete: md:0x%x (%s::%s), address:0x%x. Created ji:0x%x\n",
            fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName,
            newAddress, ji));

        // Bind any IL patches to the newly jitted native code.
        HRESULT hr;
        hr = MapAndBindFunctionPatches(ji, fd, (CORDB_ADDRESS_TYPE *)newAddress);
        _ASSERTE(SUCCEEDED(hr));
    }

    LOG((LF_CORDB, LL_EVERYTHING, "JitComplete completed successfully\n"));

Exit:
    ;
}

/******************************************************************************
// Get the number of fixed arguments to a function, i.e., the explicit args and the "this" pointer.
// This does not include other implicit arguments or varargs. This is used to compute a variable ID
// (see comment in CordbJITILFrame::ILVariableToNative for more detail)
// fVarArg is not used when this is called by Debugger::GetAndSendJITInfo, thus it has a default value.
// The return value is not used when this is called by Debugger::getVars.
 ******************************************************************************/
SIZE_T Debugger::GetArgCount(MethodDesc *fd,BOOL *fVarArg /* = NULL */)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Create a MetaSig for the given method's sig. (Easier than
    // picking the sig apart ourselves.)
    PCCOR_SIGNATURE pCallSig;
    DWORD cbCallSigSize;

    fd->GetSig(&pCallSig, &cbCallSigSize);

    if (pCallSig == NULL)
    {
        // Sig should only be null if the image is corrupted. (Even for lightweight-codegen)
        // We expect the jit+verifier to catch this, so that we never land here.
        // But just in case ...
        CONSISTENCY_CHECK_MSGF(false, ("Corrupted image, null sig.(%s::%s)", fd->m_pszDebugClassName, fd->m_pszDebugMethodName));
        return 0;
    }

    MetaSig msig(pCallSig, cbCallSigSize, g_pEEInterface->MethodDescGetModule(fd), NULL, MetaSig::sigMember);

    // Get the arg count.
    UINT32 NumArguments = msig.NumFixedArgs();

    // Account for the 'this' argument.
    if (!(g_pEEInterface->MethodDescIsStatic(fd)))
        NumArguments++;

    // Is this a VarArg's function?
    if (msig.IsVarArg() && fVarArg != NULL)
    {
        *fVarArg = true;
    }

    return NumArguments;
}

#endif // #ifndef DACCESS_COMPILE





/******************************************************************************
    DebuggerJitInfo * Debugger::GetJitInfo():   GetJitInfo
    will return a pointer to a DebuggerJitInfo.  If the DJI
    doesn't exist, or it does exist, but the method has actually
    been pitched (and the caller wants pitched methods filtered out),
    then we'll return NULL.

    Note: This will also create a DMI for if one does not exist for this DJI.

    MethodDesc* fd:  MethodDesc for the method we're interested in.
    CORDB_ADDRESS_TYPE * pbAddr:  Address within the code, to indicate which
            version we want.  If this is NULL, then we want the
            head of the DebuggerJitInfo list, whether it's been
            JITted or not.
 ******************************************************************************/


// Get a DJI from an address.
DebuggerJitInfo *Debugger::GetJitInfoFromAddr(TADDR addr)
{
    WRAPPER_NO_CONTRACT;

    MethodDesc *fd;
    fd = g_pEEInterface->GetNativeCodeMethodDesc(addr);
    _ASSERTE(fd);

    return GetJitInfo(fd, (const BYTE*) addr, NULL);
}

// Get a DJI for a Native MD (MD for a native function).
// In the EnC scenario, the MethodDesc refers to the most recent method.
// This is very dangerous since there may be multiple versions alive at the same time.
// This will give back the wrong DJI if we're lookikng for a stale method desc.
// @todo - can a caller possibly use this correctly?
DebuggerJitInfo *Debugger::GetLatestJitInfoFromMethodDesc(MethodDesc * pMethodDesc)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pMethodDesc != NULL);
    // We'd love to assert that we're jitted; but since this may be in the JitComplete
    // callback path, we can't be sure.

    return GetJitInfoWorker(pMethodDesc, NULL, NULL);
}


DebuggerJitInfo *Debugger::GetJitInfo(MethodDesc *fd, const BYTE *pbAddr, DebuggerMethodInfo **pMethInfo )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(!g_pDebugger->HasDebuggerDataLock());
    }
    CONTRACTL_END;

    // Address should be non-null and in range of MethodDesc. This lets us tell which EnC version.
    _ASSERTE(pbAddr != NULL);

    return GetJitInfoWorker(fd, pbAddr, pMethInfo);

}

// Internal worker to GetJitInfo. Doesn't validate parameters.
DebuggerJitInfo *Debugger::GetJitInfoWorker(MethodDesc *fd, const BYTE *pbAddr, DebuggerMethodInfo **pMethInfo)
{

    DebuggerMethodInfo *dmi = NULL;
    DebuggerJitInfo *dji = NULL;

    // If we have a null MethodDesc - we're not going to get a jit-info. Do this check once at the top
    // rather than littered throughout the rest of this function.
    if (fd == NULL)
    {
        LOG((LF_CORDB, LL_EVERYTHING, "Debugger::GetJitInfo, addr=0x%p - null fd - returning null\n", pbAddr));
        return NULL;
    }
    else
    {
        CONSISTENCY_CHECK_MSGF(!fd->IsWrapperStub(), ("Can't get Jit-info for wrapper MDesc,'%s'", fd->m_pszDebugMethodName));
    }

    // The debugger doesn't track Lightweight-codegen methods b/c they have no metadata.
    if (fd->IsDynamicMethod())
    {
        return NULL;
    }


    // initialize our out param
    if (pMethInfo)
    {
        *pMethInfo = NULL;
    }

    LOG((LF_CORDB, LL_EVERYTHING, "Debugger::GetJitInfo called\n"));
    //    CHECK_DJI_TABLE_DEBUGGER;

    // Find the DJI via the DMI
    //
    // One way to improve the perf, both in terms of memory usage, number of allocations
    // and lookup speeds would be to have the first JitInfo inline in the MethodInfo
    // struct.  After all, we never want to have a MethodInfo in the table without an
    // associated JitInfo, and this should bring us back very close to the old situation
    // in terms of perf.  But correctness comes first, and perf later...
    //        CHECK_DMI_TABLE;
    dmi = GetOrCreateMethodInfo(fd->GetModule(), fd->GetMemberDef());

    if (dmi == NULL)
    {
        // If we can't create the DMI, we won't be able to create the DJI.
        return NULL;
    }

    // TODO: Currently, this method does not handle code versioning properly (at least in some profiler scenarios), it may need
    // to take pbAddr into account and lazily create a DJI for that particular version of the method.

    // This may take the lock and lazily create an entry, so we do it up front.
    dji = dmi->GetLatestJitInfo(fd);


    DebuggerDataLockHolder debuggerDataLockHolder(this);

    // Note the call to GetLatestJitInfo() will lazily create the first DJI if we don't already have one.
    for (; dji != NULL; dji = dji->m_prevJitInfo)
    {
        if (PTR_TO_TADDR(dji->m_nativeCodeVersion.GetMethodDesc()) == PTR_HOST_TO_TADDR(fd))
        {
            break;
        }
    }
    LOG((LF_CORDB, LL_INFO1000, "D::GJI: for md:0x%x (%s::%s), got dmi:0x%x.\n",
         fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName,
         dmi));




    // Log stuff - fd may be null; so we don't want to AV in the log.

    LOG((LF_CORDB, LL_INFO1000, "D::GJI: for md:0x%x (%s::%s), got dmi:0x%x, dji:0x%x, latest dji:0x%x, latest fd:0x%x, prev dji:0x%x\n",
        fd, fd->m_pszDebugClassName, fd->m_pszDebugMethodName,
        dmi, dji, (dmi ? dmi->GetLatestJitInfo_NoCreate() : 0),
        ((dmi && dmi->GetLatestJitInfo_NoCreate()) ? dmi->GetLatestJitInfo_NoCreate()->m_nativeCodeVersion.GetMethodDesc():0),
        (dji?dji->m_prevJitInfo:0)));

    if ((dji != NULL) && (pbAddr != NULL))
    {
        dji = dji->GetJitInfoByAddress(pbAddr);

        // XXX Microsoft - dac doesn't support stub tracing
        // so this just results in not-impl exceptions.
#ifndef DACCESS_COMPILE
        if (dji == NULL) //may have been given address of a thunk
        {
            LOG((LF_CORDB,LL_INFO1000,"Couldn't find a DJI by address 0x%p, "
                "so it might be a stub or thunk\n", pbAddr));
            TraceDestination trace;

            g_pEEInterface->TraceStub((const BYTE *)pbAddr, &trace);

            if ((trace.GetTraceType() == TRACE_MANAGED) && (pbAddr != (const BYTE *)trace.GetAddress()))
            {
                LOG((LF_CORDB,LL_INFO1000,"Address thru thunk"
                    ": 0x%p\n", trace.GetAddress()));
                dji = GetJitInfo(fd, dac_cast<PTR_CBYTE>(trace.GetAddress()));
            }
#ifdef LOGGING
            else
            {
                _ASSERTE(trace.GetTraceType() != TRACE_UNJITTED_METHOD ||
                    (fd == trace.GetMethodDesc()));
                LOG((LF_CORDB,LL_INFO1000,"Address not thunked - "
                    "must be to unJITted method, or normal managed "
                    "method lacking a DJI!\n"));
            }
#endif //LOGGING
        }
#endif // #ifndef DACCESS_COMPILE
    }

    if (pMethInfo)
    {
        *pMethInfo = dmi;
    }

    // DebuggerDataLockHolder out of scope - release implied

    return dji;
}

DebuggerMethodInfo *Debugger::GetOrCreateMethodInfo(Module *pModule, mdMethodDef token)
{
    CONTRACTL
    {
        SUPPORTS_DAC;
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebuggerMethodInfo *info = NULL;

    // When dump debugging, we don't expect to have a lock,
    // nor would it be useful for anything.
    ALLOW_DATATARGET_MISSING_MEMORY(
        // In case we don't have already, take it now.
        DebuggerDataLockHolder debuggerDataLockHolder(this);
    );

    if (m_pMethodInfos != NULL)
    {
        info = m_pMethodInfos->GetMethodInfo(pModule, token);
    }

    // dac checks ngen'ed image content first, so
    // if we didn't find information it doesn't exist.
#ifndef DACCESS_COMPILE
    if (info == NULL)
    {
        info = CreateMethodInfo(pModule, token);

        LOG((LF_CORDB, LL_INFO1000, "D::GOCMI: created DMI for mdToken:0x%x, dmi:0x%x\n",
            token, info));
    }
#endif // #ifndef DACCESS_COMPILE


    if (info == NULL)
    {
        // This should only happen in an oom scenario. It would be nice to throw here.
        STRESS_LOG2(LF_CORDB, LL_EVERYTHING, "OOM - Failed to allocate DJI (0x%p, 0x%x)\n", pModule, token);
    }

    // DebuggerDataLockHolder out of scope - release implied
    return info;
}


#ifndef DACCESS_COMPILE

// Helper to use w/ the debug stores.
BYTE* InteropSafeNoThrowNew(void*, size_t cBytes)
{
    BYTE* p = new (interopsafe, nothrow) BYTE[cBytes];
    return p;
}

/******************************************************************************
 * GetILToNativeMapping returns a map from IL offsets to native
 * offsets for this code. An array of COR_PROF_IL_TO_NATIVE_MAP
 * structs will be returned, and some of the ilOffsets in this array
 * may be the values specified in CorDebugIlToNativeMappingTypes.
 ******************************************************************************/
HRESULT Debugger::GetILToNativeMapping(PCODE pNativeCodeStartAddress, ULONG32 cMap,
                                       ULONG32 *pcMap, COR_DEBUG_IL_TO_NATIVE_MAP map[])
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

#ifdef PROFILING_SUPPORTED
    // At this point, we're pulling in the debugger.
    if (!HasLazyData())
    {
        DebuggerLockHolder lockHolder(this);
        LazyInit(); // throws
    }

    // Get the JIT info by functionId.

    // This function is unsafe to use during EnC because the MethodDesc doesn't tell
    // us which version is being requested.
    // However, this function is only used by the profiler, and you can't profile with EnC,
    // which means that getting the latest jit-info is still correct.
#if defined(PROFILING_SUPPORTED)
    _ASSERTE(CORProfilerPresent());
#endif // PROFILING_SUPPORTED

    MethodDesc *fd = g_pEEInterface->GetNativeCodeMethodDesc(pNativeCodeStartAddress);
    if (fd == NULL || fd->IsWrapperStub())
    {
        return E_FAIL;
    }

    if (fd->IsDynamicMethod())
    {
        if (g_pConfig->GetTrackDynamicMethodDebugInfo())
        {
            DebugInfoRequest diq;
            diq.InitFromStartingAddr(fd, pNativeCodeStartAddress);

            if (cMap == 0)
            {
                if (DebugInfoManager::GetBoundariesAndVars(diq, nullptr, nullptr, pcMap, nullptr, nullptr, nullptr))
                {
                    return S_OK;
                }

                return E_FAIL;
            }

            ICorDebugInfo::OffsetMapping* pMap = nullptr;
            if (DebugInfoManager::GetBoundariesAndVars(diq, InteropSafeNoThrowNew, nullptr, pcMap, &pMap, nullptr, nullptr))
            {
                for (ULONG32 i = 0; i < cMap; ++i)
                {
                    map[i].ilOffset = pMap[i].ilOffset;
                    map[i].nativeStartOffset = pMap[i].nativeOffset;
                    if (i > 0)
                    {
                        map[i - 1].nativeEndOffset = map[i].nativeStartOffset;
                    }
                }

                DeleteInteropSafe(pMap);

                return S_OK;
            }

            return E_FAIL;
        }
        else
        {
            return E_FAIL;
        }
    }

    DebuggerMethodInfo *pDMI = GetOrCreateMethodInfo(fd->GetModule(), fd->GetMemberDef());
    if (pDMI == NULL)
    {
        return E_FAIL;
    }

    DebuggerJitInfo *pDJI = pDMI->FindOrCreateInitAndAddJitInfo(fd, pNativeCodeStartAddress);

    // Dunno what went wrong
    if (pDJI == NULL)
        return (E_FAIL);

    // If they gave us space to copy into...
    if (map != NULL)
    {
        // Only copy as much as either they gave us or we have to copy.
        ULONG32 cpyCount = min(cMap, pDJI->GetSequenceMapCount());

        // Read the map right out of the Left Side.
        if (cpyCount > 0)
            ExportILToNativeMap(cpyCount,
                        map,
                        pDJI->GetSequenceMap(),
                        pDJI->m_sizeOfCode);
    }

    // Return the true count of entries
    if (pcMap)
    {
        *pcMap = pDJI->GetSequenceMapCount();
    }

    return (S_OK);
#else
    return E_NOTIMPL;
#endif
}


//---------------------------------------------------------------------------------------
//
// This is morally the same as GetILToNativeMapping, except the output is in a different
// format, to better facilitate sending the ETW ILToNativeMap events.
//
// Arguments:
//      pMD - MethodDesc whose IL-to-native map will be returned
//      cMapMax - Max number of map entries to return.  Although
//                this function handles the allocation of the returned
//                array, the caller still wants to limit how big this
//                can get, since ETW itself has limits on how big
//                events can get
//      pcMap - [out] Number of entries returned in each output parallel array (next
//                    two parameters).
//      prguiILOffset - [out] Array of IL offsets.  This function allocates, caller must free.
//      prguiNativeOffset - [out] Array of the starting native offsets that correspond
//                                to each (*prguiILOffset)[i].  This function allocates,
//                                caller must free.
//
// Return Value:
//      HRESULT indicating success or failure.
//
// Notes:
//     * This function assumes lazy data has already been initialized (in order to
//         ensure that this doesn't trigger or take the large debugger mutex).  So
//         callers must guarantee they call InitializeLazyDataIfNecessary() first.
//     * Either this function fails, and (*prguiILOffset) & (*prguiNativeOffset) will be
//         untouched OR this function succeeds and (*prguiILOffset) & (*prguiNativeOffset)
//         will both be non-NULL, set to the parallel arrays this function allocated.
//     *  If this function returns success, then the caller must free (*prguiILOffset) and
//         (*prguiNativeOffset)
//     * (*prguiILOffset) and (*prguiNativeOffset) are parallel arrays, such that
//         (*prguiILOffset)[i] corresponds to (*prguiNativeOffset)[i] for each 0 <= i < *pcMap
//     * If EnC is enabled, this function will return the IL-to-native mapping for the latest
//         EnC version of the function.  This may not be what the profiler wants, but EnC
//         + ETW-map events is not a typical combination, and this is consistent with
//         other ETW events like JittingStarted or MethodLoad, which also fire multiple
//         events for the same MethodDesc (each time it's EnC'd), with each event
//         corresponding to the most recent EnC version at the time.
//

HRESULT Debugger::GetILToNativeMappingIntoArrays(
    MethodDesc * pMethodDesc,
    PCODE pNativeCodeStartAddress,
    USHORT cMapMax,
    USHORT * pcMap,
    UINT ** prguiILOffset,
    UINT ** prguiNativeOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pMethodDesc != NULL);
    _ASSERTE(pcMap != NULL);
    _ASSERTE(prguiILOffset != NULL);
    _ASSERTE(prguiNativeOffset != NULL);

    // Any caller of GetILToNativeMappingIntoArrays had better call
    // InitializeLazyDataIfNecessary first!
    _ASSERTE(HasLazyData());

    // Get the JIT info by functionId.

    if (pMethodDesc->IsWrapperStub() || pMethodDesc->IsDynamicMethod())
    {
        return E_FAIL;
    }

    DebuggerMethodInfo *pDMI = GetOrCreateMethodInfo(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef());
    if (pDMI == NULL)
    {
        return E_FAIL;
    }

    DebuggerJitInfo *pDJI = pDMI->FindOrCreateInitAndAddJitInfo(pMethodDesc, pNativeCodeStartAddress);

    // Dunno what went wrong
    if (pDJI == NULL)
        return E_FAIL;

    ULONG32 cMap = min(cMapMax, pDJI->GetSequenceMapCount());
    DebuggerILToNativeMap * rgMapInt = pDJI->GetSequenceMap();

    NewArrayHolder<UINT> rguiILOffsetTemp = new (nothrow) UINT[cMap];
    if (rguiILOffsetTemp == NULL)
        return E_OUTOFMEMORY;

    NewArrayHolder<UINT> rguiNativeOffsetTemp = new (nothrow) UINT[cMap];
    if (rguiNativeOffsetTemp == NULL)
        return E_OUTOFMEMORY;

    for (ULONG32 iMap=0; iMap < cMap; iMap++)
    {
        rguiILOffsetTemp[iMap] = rgMapInt[iMap].ilOffset;
        rguiNativeOffsetTemp[iMap] = rgMapInt[iMap].nativeStartOffset;
    }

    // Since cMap is the min of cMapMax (and something else) and cMapMax is a USHORT,
    // then cMap must fit in a USHORT as well
    _ASSERTE(FitsIn<USHORT>(cMap));
    *pcMap = (USHORT) cMap;
    *prguiILOffset = rguiILOffsetTemp.Extract();
    *prguiNativeOffset = rguiNativeOffsetTemp.Extract();

    return S_OK;
}




#endif // #ifndef DACCESS_COMPILE

/******************************************************************************
 *
 ******************************************************************************/
CodeRegionInfo CodeRegionInfo::GetCodeRegionInfo(DebuggerJitInfo *dji, MethodDesc *md, PTR_CORDB_ADDRESS_TYPE addr)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (dji && dji->m_addrOfCode)
    {
        LOG((LF_CORDB, LL_EVERYTHING, "CRI::GCRI: simple case\n"));
        return dji->m_codeRegionInfo;
    }
    else
    {
        LOG((LF_CORDB, LL_EVERYTHING, "CRI::GCRI: more complex case\n"));
        CodeRegionInfo codeRegionInfo;

        // Use method desc from dji if present
        if (dji && dji->m_nativeCodeVersion.GetMethodDesc())
        {
            _ASSERTE(!md || md == dji->m_nativeCodeVersion.GetMethodDesc());
            md = dji->m_nativeCodeVersion.GetMethodDesc();
        }

        if (!addr)
        {
            _ASSERTE(md);
            addr = dac_cast<PTR_CORDB_ADDRESS_TYPE>(g_pEEInterface->GetFunctionAddress(md));
        }
        else
        {
            _ASSERTE(!md ||
                     (addr == dac_cast<PTR_CORDB_ADDRESS_TYPE>(g_pEEInterface->GetFunctionAddress(md))));
        }

        if (addr)
        {
            PCODE pCode = PINSTRToPCODE(dac_cast<TADDR>(addr));
            codeRegionInfo.InitializeFromStartAddress(pCode);
        }

        return codeRegionInfo;
    }
}


#ifndef DACCESS_COMPILE
/******************************************************************************
//  Helper function for getBoundaries to get around AMD64 compiler and
// contract holders with PAL_TRY in the same function.
 ******************************************************************************/
void Debugger::getBoundariesHelper(MethodDesc * md,
                                   unsigned int *cILOffsets,
                                   DWORD **pILOffsets)
{
    //
    // CANNOT ADD A CONTRACT HERE.  Contract is in getBoundaries
    //

    //
    // Grab the JIT info struct for this method.  Create if needed, as this
    // may be called before JITComplete.
    //
    DebuggerMethodInfo *dmi = NULL;
    dmi = GetOrCreateMethodInfo(md->GetModule(), md->GetMemberDef());

    if (dmi != NULL)
    {
        LOG((LF_CORDB,LL_INFO10000,"De::NGB: Got dmi 0x%x\n",dmi));

#if defined(FEATURE_ISYM_READER)
        // Note: we need to make sure to enable preemptive GC here just in case we block in the symbol reader.
        GCX_PREEMP_EEINTERFACE();

        Module *pModule = md->GetModule();
        (void)pModule; //prevent "unused variable" error from GCC
        _ASSERTE(pModule != NULL);

        SafeComHolder<ISymUnmanagedReader> pReader(pModule->GetISymUnmanagedReader());

        // If we got a reader, use it.
        if (pReader != NULL)
        {
            // Grab the sym reader's method.
            ISymUnmanagedMethod *pISymMethod;

            HRESULT hr = pReader->GetMethod(md->GetMemberDef(),
                                            &pISymMethod);

            ULONG32 n = 0;

            if (SUCCEEDED(hr))
            {
                // Get the count of sequence points.
                hr = pISymMethod->GetSequencePointCount(&n);
                _ASSERTE(SUCCEEDED(hr));


                LOG((LF_CORDB, LL_INFO100000,
                     "D::NGB: Reader seq pt count is %d\n", n));

                ULONG32 *p;

                if (n > 0)
                {
                    ULONG32 dummy;

                    p = new ULONG32[n];
                    _ASSERTE(p != NULL); // throws on oom errror

                    hr = pISymMethod->GetSequencePoints(n, &dummy,
                                                        p, NULL, NULL, NULL,
                                                        NULL, NULL);
                    _ASSERTE(SUCCEEDED(hr));
                    _ASSERTE(dummy == n);

                    *pILOffsets = (DWORD*)p;

                    // Translate the IL offets based on an
                    // instrumented IL map if one exists.
                    if (dmi->HasInstrumentedILMap())
                    {
                        InstrumentedILOffsetMapping mapping =
                            dmi->GetRuntimeModule()->GetInstrumentedILOffsetMapping(dmi->m_token);

                        for (SIZE_T i = 0; i < n; i++)
                        {
                            int origOffset = *p;

                            *p = dmi->TranslateToInstIL(
                                                  &mapping,
                                                  origOffset,
                                                  bOriginalToInstrumented);

                            LOG((LF_CORDB, LL_INFO100000,
                                 "D::NGB: 0x%04x (Real IL:0x%x)\n",
                                 origOffset, *p));

                            p++;
                        }
                    }
#ifdef LOGGING
                    else
                    {
                        for (SIZE_T i = 0; i < n; i++)
                        {
                            LOG((LF_CORDB, LL_INFO100000,
                                 "D::NGB: 0x%04x \n", *p));
                            p++;
                        }
                    }
#endif
                }
                else
                    *pILOffsets = NULL;

                pISymMethod->Release();
            }
            else
            {

                *pILOffsets = NULL;

                LOG((LF_CORDB, LL_INFO10000,
                     "De::NGB: failed to find method 0x%x in sym reader.\n",
                     md->GetMemberDef()));
            }

            *cILOffsets = n;
        }
        else
        {
            LOG((LF_CORDB, LL_INFO100000, "D::NGB: no reader.\n"));
        }

#else // FEATURE_ISYM_READER
        // We don't have ISymUnmanagedReader.  Pretend there are no sequence points.
        *cILOffsets = 0;
#endif // FEATURE_ISYM_READER
    }

    LOG((LF_CORDB, LL_INFO100000, "D::NGB: cILOffsets=%d\n", *cILOffsets));
    return;
}
#endif

/******************************************************************************
// Use an ISymUnmanagedReader to get method sequence points.
 ******************************************************************************/
void Debugger::getBoundaries(MethodDesc * md,
                             unsigned int *cILOffsets,
                             DWORD **pILOffsets,
                             ICorDebugInfo::BoundaryTypes *implicitBoundaries)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    // May be here even when a debugger is not attached.

    // @@@
    // Implements DebugInterface API

    *cILOffsets = 0;
    *pILOffsets = NULL;
    *implicitBoundaries = ICorDebugInfo::DEFAULT_BOUNDARIES;
    // If there has been an unrecoverable Left Side error, then we
    // just pretend that there are no boundaries.
    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    // LCG methods have their own resolution scope that is seperate from a module
    // so they shouldn't have their symbols looked up in the module PDB. Right now
    // LCG methods have no symbols so we can just early out, but if they ever
    // had some symbols attached we would need a different way of getting to them.
    // See Dev10 issue 728519
    if(md->IsLCGMethod())
    {
        return;
    }

    // If JIT optimizations are allowed for the module this function
    // lives in, then don't grab specific boundaries from the symbol
    // store since any boundaries we give the JIT will be pretty much
    // ignored anyway.
    if (!CORDisableJITOptimizations(md->GetModule()->GetDebuggerInfoBits()))
    {
        *implicitBoundaries  = ICorDebugInfo::BoundaryTypes(ICorDebugInfo::STACK_EMPTY_BOUNDARIES |
                                         ICorDebugInfo::CALL_SITE_BOUNDARIES);

        return;
    }

    Module* pModule = md->GetModule();
    DWORD dwBits = pModule->GetDebuggerInfoBits();
    if ((dwBits & DACF_IGNORE_PDBS) != 0)
    {
        //
        // If told to explicitly ignore PDBs for this function, then bail now.
        //
        return;
    }

    if( !pModule->IsSymbolReadingEnabled() )
    {
        // Symbol reading is disabled for this module, so bail out early (for efficiency only)
        return;
    }

    if (pModule == SystemDomain::SystemModule())
    {
        // We don't look up PDBs for CoreLib.  This is not quite right, but avoids
        // a bootstrapping problem.  When an EXE loads, it has the option of setting
        // the COM apartment model to STA if we need to.  It is important that no
        // other Coinitialize happens before this.  Since loading the PDB reader uses
        // com we can not come first.  However managed code IS run before the COM
        // apartment model is set, and thus we have a problem since this code is
        // called for when JITTing managed code.    We avoid the problem by just
        // bailing for CoreLib.
        return;
    }

        // At this point, we're pulling in the debugger.
    if (!HasLazyData())
    {
        DebuggerLockHolder lockHolder(this);
        LazyInit(); // throws
    }

    getBoundariesHelper(md, cILOffsets, pILOffsets);

#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE
}


/******************************************************************************
 *
 ******************************************************************************/
void Debugger::getVars(MethodDesc * md, ULONG32 *cVars, ICorDebugInfo::ILVarInfo **vars,
                       bool *extendOthers)
{
#ifndef DACCESS_COMPILE
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
        PRECONDITION(!ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;



    // At worst return no information
    *cVars = 0;
    *vars = NULL;

    // Just tell the JIT to extend everything.
    // Note that if optimizations are enabled, the native compilers are
    // free to ingore *extendOthers
    *extendOthers = true;

    DWORD bits = md->GetModule()->GetDebuggerInfoBits();

    if (CORDBUnrecoverableError(this))
        goto Exit;

    if (CORDisableJITOptimizations(bits))
//    if (!CORDebuggerAllowJITOpts(bits))
    {
        //
        // @TODO: Do we really need this code since *extendOthers==true?
        //

        // Is this a vararg function?
        BOOL fVarArg = false;
        GetArgCount(md, &fVarArg);

        if (fVarArg)
        {
            COR_ILMETHOD *ilMethod = g_pEEInterface->MethodDescGetILHeader(md);

            if (ilMethod)
            {
                // It is, so we need to tell the JIT to give us the
                // varags handle.
                ICorDebugInfo::ILVarInfo *p = new ICorDebugInfo::ILVarInfo[1];
                _ASSERTE(p != NULL); // throws on oom error

                COR_ILMETHOD_DECODER header(ilMethod);
                unsigned int ilCodeSize = header.GetCodeSize();

                p->startOffset = 0;
                p->endOffset = ilCodeSize;
                p->varNumber = (DWORD) ICorDebugInfo::VARARGS_HND_ILNUM;

                *cVars = 1;
                *vars = p;
            }
        }
    }

    LOG((LF_CORDB, LL_INFO100000, "D::gV: cVars=%d, extendOthers=%d\n",
         *cVars, *extendOthers));

Exit:
    ;
#else
    DacNotImpl();
#endif // #ifndef DACCESS_COMPILE
}


#ifndef DACCESS_COMPILE

// If we have a varargs function, we can't set the IP (we don't know how to pack/unpack the arguments), so if we
// call SetIP with fCanSetIPOnly = true, we need to check for that.
// Arguments:
//     input:  nEntries      - number of entries in varNativeInfo
//             varNativeInfo - array of entries describing the args and locals for the function
//     output: true iff the function has varargs
BOOL Debugger::IsVarArgsFunction(unsigned int nEntries, PTR_NativeVarInfo varNativeInfo)
{
    for (unsigned int i = 0; i < nEntries; ++i)
    {
        if (varNativeInfo[i].loc.vlType == ICorDebugInfo::VLT_FIXED_VA)
        {
            return TRUE;
        }
    }
    return FALSE;
}

// We want to keep the 'worst' HRESULT - if one has failed (..._E_...) & the
// other hasn't, take the failing one.  If they've both/neither failed, then
// it doesn't matter which we take.
// Note that this macro favors retaining the first argument
#define WORST_HR(hr1,hr2) (FAILED(hr1)?hr1:hr2)
/******************************************************************************
 *
 ******************************************************************************/
HRESULT Debugger::SetIP( bool fCanSetIPOnly, Thread *thread,Module *module,
                         mdMethodDef mdMeth, DebuggerJitInfo* dji,
                         SIZE_T offsetILTo, BOOL fIsIL)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(thread));
        PRECONDITION(CheckPointer(module));
        PRECONDITION(mdMeth != mdMethodDefNil);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    static ConfigDWORD breakOnSetIP;
    if (breakOnSetIP.val(CLRConfig::INTERNAL_DbgBreakOnSetIP)) _ASSERTE(!"DbgBreakOnSetIP");
#endif

    HRESULT hr = S_OK;
    HRESULT hrAdvise = S_OK;

    DWORD offsetILFrom;
    CorDebugMappingResult map;
    DWORD whichIgnore;

    ControllerStackInfo csi;

    BOOL exact;
    SIZE_T offsetNatTo;

    PCODE    pbDest = NULL;
    BYTE    *pbBase = NULL;
    CONTEXT *pCtx   = NULL;
    DWORD    dwSize = 0;
    SIZE_T  *rgVal1 = NULL;
    SIZE_T  *rgVal2 = NULL;
    BYTE **pVCs   = NULL;

    LOG((LF_CORDB, LL_INFO1000, "D::SIP: In SetIP ==> fCanSetIPOnly:0x%x <==!\n", fCanSetIPOnly));

    CodeVersionManager *pCodeVersionManager = module->GetCodeVersionManager();
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        ILCodeVersion ilCodeVersion = pCodeVersionManager->GetActiveILCodeVersion(module, mdMeth);
        if (!ilCodeVersion.IsDefaultVersion())
        {
            return CORDBG_E_SET_IP_IMPOSSIBLE;
        }
    }

    pCtx = GetManagedStoppedCtx(thread);

    // If we can't get a context, then we can't possibly be a in a good place
    // to do a setip.
    if (pCtx == NULL)
    {
        return CORDBG_S_BAD_START_SEQUENCE_POINT;
    }

    // Implicit Caveat: We need to be the active frame.
    // We can safely take a stack trace because the thread is synchronized.
    StackTraceTicket ticket(thread);
    csi.GetStackInfo(ticket, thread, LEAF_MOST_FRAME, NULL);

    ULONG offsetNatFrom = csi.m_activeFrame.relOffset;
#if defined(FEATURE_EH_FUNCLETS)
    if (csi.m_activeFrame.IsFuncletFrame())
    {
        offsetNatFrom = (ULONG)((SIZE_T)GetControlPC(&(csi.m_activeFrame.registers)) -
                                (SIZE_T)(dji->m_addrOfCode));
    }
#endif // FEATURE_EH_FUNCLETS

    _ASSERTE(dji != NULL);

    // On WIN64 platforms, it's important to use the total size of the
    // parent method and the funclets below (i.e. m_sizeOfCode).  Don't use
    // the size of the individual funclets or the parent method.
    pbBase = (BYTE*)CORDB_ADDRESS_TO_PTR(dji->m_addrOfCode);
    dwSize = (DWORD)dji->m_sizeOfCode;
#if defined(FEATURE_EH_FUNCLETS)
    // Currently, method offsets are not bigger than 4 bytes even on WIN64.
    // Assert that it is so here.
    _ASSERTE((SIZE_T)dwSize == dji->m_sizeOfCode);
#endif // FEATURE_EH_FUNCLETS


    // Create our structure for analyzing this.
    // <TODO>@PERF: optimize - hold on to this so we don't rebuild it for both
    // CanSetIP & SetIP.</TODO>
    int           cFunclet  = 0;
    const DWORD * rgFunclet = NULL;
#if defined(FEATURE_EH_FUNCLETS)
    cFunclet  = dji->GetFuncletCount();
    rgFunclet = dji->m_rgFunclet;
#endif // FEATURE_EH_FUNCLETS

    EHRangeTree* pEHRT = new (nothrow) EHRangeTree(csi.m_activeFrame.pIJM,
                                                   csi.m_activeFrame.MethodToken,
                                                   dwSize,
                                                   cFunclet,
                                                   rgFunclet);

    // To maintain the current semantics, we will check the following right before SetIPFromSrcToDst() is called
    // (instead of checking them now):
    // 1) pEHRT == NULL
    // 2) FAILED(pEHRT->m_hrInit)


    {
        LOG((LF_CORDB, LL_INFO1000, "D::SIP:Got version info fine\n"));

        // Caveat: we need to start from a sequence point
        offsetILFrom = dji->MapNativeOffsetToIL(offsetNatFrom,
                                                &map, &whichIgnore);
        if ( !(map & MAPPING_EXACT) )
        {
            LOG((LF_CORDB, LL_INFO1000, "D::SIP:Starting native offset is bad!\n"));
            hrAdvise = WORST_HR(hrAdvise, CORDBG_S_BAD_START_SEQUENCE_POINT);
        }
        else
        {   // exact IL mapping

            if (!(dji->GetSrcTypeFromILOffset(offsetILFrom) & ICorDebugInfo::STACK_EMPTY))
            {
                LOG((LF_CORDB, LL_INFO1000, "D::SIP:Starting offset isn't stack empty!\n"));
                hrAdvise = WORST_HR(hrAdvise, CORDBG_S_BAD_START_SEQUENCE_POINT);
            }
        }

        // Caveat: we need to go to a sequence point
        if (fIsIL )
        {
#if defined(FEATURE_EH_FUNCLETS)
            int funcletIndexFrom = dji->GetFuncletIndex((CORDB_ADDRESS)offsetNatFrom, DebuggerJitInfo::GFIM_BYOFFSET);
            offsetNatTo = dji->MapILOffsetToNativeForSetIP(offsetILTo, funcletIndexFrom, pEHRT, &exact);
#else  // FEATURE_EH_FUNCLETS
            DebuggerJitInfo::ILToNativeOffsetIterator it;
            dji->InitILToNativeOffsetIterator(it, offsetILTo);
            offsetNatTo = it.CurrentAssertOnlyOne(&exact);
#endif // FEATURE_EH_FUNCLETS

            if (!exact)
            {
                LOG((LF_CORDB, LL_INFO1000, "D::SIP:Dest (via IL offset) is bad!\n"));
                hrAdvise = WORST_HR(hrAdvise, CORDBG_S_BAD_END_SEQUENCE_POINT);
            }
        }
        else
        {
            offsetNatTo = offsetILTo;
            LOG((LF_CORDB, LL_INFO1000, "D::SIP:Dest of 0x%p (via native "
                "offset) is fine!\n", offsetNatTo));
        }

        CorDebugMappingResult mapping;
        DWORD which;
        offsetILTo = dji->MapNativeOffsetToIL(offsetNatTo, &mapping, &which);

        // We only want to perhaps return CORDBG_S_BAD_END_SEQUENCE_POINT if
        // we're not already returning CORDBG_S_BAD_START_SEQUENCE_POINT.
        if (hr != CORDBG_S_BAD_START_SEQUENCE_POINT)
        {
            if ( !(mapping & MAPPING_EXACT) )
            {
                LOG((LF_CORDB, LL_INFO1000, "D::SIP:Ending native offset is bad!\n"));
                hrAdvise = WORST_HR(hrAdvise, CORDBG_S_BAD_END_SEQUENCE_POINT);
            }
            else
            {
                // <NOTE WIN64>
                // All duplicate sequence points (ones with the same IL offset) should have the same SourceTypes.
                // </NOTE WIN64>
                if (!(dji->GetSrcTypeFromILOffset(offsetILTo) & ICorDebugInfo::STACK_EMPTY))
                {
                    LOG((LF_CORDB, LL_INFO1000, "D::SIP:Ending offset isn't a sequence"
                                                " point, or not stack empty!\n"));
                    hrAdvise = WORST_HR(hrAdvise, CORDBG_S_BAD_END_SEQUENCE_POINT);
                }
            }
        }

        // Once we finally have a native offset, it had better be in range.
        if (offsetNatTo >= dwSize)
        {
            LOG((LF_CORDB, LL_INFO1000, "D::SIP:Code out of range! offsetNatTo = 0x%x, dwSize=0x%x\n", offsetNatTo, dwSize));
            hrAdvise = E_INVALIDARG;
            goto LExit;
        }

        pbDest = CodeRegionInfo::GetCodeRegionInfo(dji).OffsetToAddress(offsetNatTo);
        LOG((LF_CORDB, LL_INFO1000, "D::SIP:Dest is 0x%p\n", pbDest));

        // Don't allow SetIP if the source or target is cold (SetIPFromSrcToDst does not
        // correctly handle this case).
        if (!CodeRegionInfo::GetCodeRegionInfo(dji).IsOffsetHot(offsetNatTo) ||
            !CodeRegionInfo::GetCodeRegionInfo(dji).IsOffsetHot(offsetNatFrom))
        {
            hrAdvise = WORST_HR(hrAdvise, CORDBG_E_SET_IP_IMPOSSIBLE);
            goto LExit;
        }
    }

    if (!fCanSetIPOnly)
    {
        hr = ShuffleVariablesGet(dji,
                                 offsetNatFrom,
                                 pCtx,
                                 &rgVal1,
                                 &rgVal2,
                                 &pVCs);
        LOG((LF_CORDB|LF_ENC,
             LL_INFO10000,
             "D::SIP: rgVal1 0x%X, rgVal2 0x%X\n",
             rgVal1,
             rgVal2));

        if (FAILED(hr))
        {
            // This will only fail fatally, so exit.
            hrAdvise = WORST_HR(hrAdvise, hr);
            goto LExit;
        }
    }
    else // fCanSetIPOnly
    {
        if (IsVarArgsFunction(dji->GetVarNativeInfoCount(), dji->GetVarNativeInfo()))
        {
            hrAdvise = E_INVALIDARG;
            goto LExit;
        }
    }


    if (pEHRT == NULL)
    {
        hr = E_OUTOFMEMORY;
    }
    else if (FAILED(pEHRT->m_hrInit))
    {
        hr = pEHRT->m_hrInit;
    }
    else
    {
        //
        // This is a known, ok, violation.  END_EXCEPTION_GLUE has a call to GetThrowable in it, but
        // we will never hit it because we are passing in NULL below.  This is to satisfy the static
        // contract analyzer.
        //
        CONTRACT_VIOLATION(GCViolation);

        EX_TRY
        {
            hr =g_pEEInterface->SetIPFromSrcToDst(thread,
                                                  pbBase,
                                                  offsetNatFrom,
                                                  (DWORD)offsetNatTo,
                                                  fCanSetIPOnly,
                                                  &(csi.m_activeFrame.registers),
                                                  pCtx,
                                                  (void *)dji,
                                                  pEHRT);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);

    }

    // Get the return code, if any
    if (hr != S_OK)
    {
        hrAdvise = WORST_HR(hrAdvise, hr);
        goto LExit;
    }

    // If we really want to do this, we'll have to put the
    // variables into their new locations.
    if (!fCanSetIPOnly && !FAILED(hrAdvise))
    {
        // TODO: We should zero out any registers which have now become live GC roots,
        // but which aren't tracked variables (i.e. they are JIT temporaries).  Such registers may
        // have garbage left over in them, and we don't want the GC to try and dereference them
        // as object references.  However, we can't easily tell here which of the callee-saved regs
        // are used in this method and therefore safe to clear.
        //

        hr = ShuffleVariablesSet(dji,
                            offsetNatTo,
                            pCtx,
                            &rgVal1,
                            &rgVal2,
                            pVCs);


        if (hr != S_OK)
        {
            hrAdvise = WORST_HR(hrAdvise, hr);
            goto LExit;
        }

        _ASSERTE(pbDest != NULL);

        ::SetIP(pCtx, pbDest);

        LOG((LF_CORDB, LL_INFO1000, "D::SIP:Set IP to be 0x%p\n", GetIP(pCtx)));
    }


LExit:
    if (rgVal1 != NULL)
    {
        DeleteInteropSafe(rgVal1);
    }

    if (rgVal2 != NULL)
    {
        DeleteInteropSafe(rgVal2);
    }

    if (pEHRT != NULL)
    {
        delete pEHRT;
    }

    LOG((LF_CORDB, LL_INFO1000, "D::SIP:Returning 0x%x\n", hr));
    return hrAdvise;
}

#include "nativevaraccessors.h"

/******************************************************************************
 *
 ******************************************************************************/

HRESULT Debugger::ShuffleVariablesGet(DebuggerJitInfo  *dji,
                                      SIZE_T            offsetFrom,
                                      CONTEXT          *pCtx,
                                      SIZE_T          **prgVal1,
                                      SIZE_T          **prgVal2,
                                      BYTE           ***prgpVCs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(dji));
        PRECONDITION(CheckPointer(pCtx));
        PRECONDITION(CheckPointer(prgVal1));
        PRECONDITION(CheckPointer(prgVal2));
        PRECONDITION(dji->m_sizeOfCode >= offsetFrom);
    }
    CONTRACTL_END;

    LONG cVariables = 0;
    DWORD i;

    //
    // Find the largest variable number
    //
    for (i = 0; i < dji->GetVarNativeInfoCount(); i++)
    {
        if ((LONG)(dji->GetVarNativeInfo()[i].varNumber) > cVariables)
        {
            cVariables = (LONG)(dji->GetVarNativeInfo()[i].varNumber);
        }
    }

    HRESULT hr = S_OK;

    //
    // cVariables is a zero-based count of the number of variables.  Increment it.
    //
    cVariables++;

    SIZE_T *rgVal1 = new (interopsafe, nothrow) SIZE_T[cVariables + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM)];

    SIZE_T *rgVal2 = NULL;

    if (rgVal1 == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto LExit;
    }

    rgVal2 = new (interopsafe, nothrow) SIZE_T[cVariables + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM)];

    if (rgVal2 == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto LExit;
    }

    memset(rgVal1, 0, sizeof(SIZE_T) * (cVariables + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM)));
    memset(rgVal2, 0, sizeof(SIZE_T) * (cVariables + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM)));

    LOG((LF_CORDB|LF_ENC,
         LL_INFO10000,
         "D::SVG cVariables %d, hiddens %d, rgVal1 0x%X, rgVal2 0x%X\n",
         cVariables,
         unsigned(-ICorDebugInfo::UNKNOWN_ILNUM),
         rgVal1,
         rgVal2));

    GetVariablesFromOffset(dji->m_nativeCodeVersion.GetMethodDesc(),
                           dji->GetVarNativeInfoCount(),
                           dji->GetVarNativeInfo(),
                           offsetFrom,
                           pCtx,
                           rgVal1,
                           rgVal2,
                           cVariables + unsigned(-ICorDebugInfo::UNKNOWN_ILNUM),
                           prgpVCs);


LExit:
    if (!FAILED(hr))
    {
        (*prgVal1) = rgVal1;
        (*prgVal2) = rgVal2;
    }
    else
    {
        LOG((LF_CORDB, LL_INFO100, "D::SVG: something went wrong hr=0x%x!", hr));

        (*prgVal1) = NULL;
        (*prgVal2) = NULL;

        if (rgVal1 != NULL)
            delete[] rgVal1;

        if (rgVal2 != NULL)
            delete[] rgVal2;
    }

    return hr;
}

/******************************************************************************
 *
 ******************************************************************************/
HRESULT Debugger::ShuffleVariablesSet(DebuggerJitInfo  *dji,
                                   SIZE_T            offsetTo,
                                   CONTEXT          *pCtx,
                                   SIZE_T          **prgVal1,
                                   SIZE_T          **prgVal2,
                                   BYTE            **rgpVCs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(dji));
        PRECONDITION(CheckPointer(pCtx));
        PRECONDITION(CheckPointer(prgVal1));
        PRECONDITION(CheckPointer(prgVal2));
        PRECONDITION(dji->m_sizeOfCode >= offsetTo);
    }
    CONTRACTL_END;

    LOG((LF_CORDB|LF_ENC,
         LL_INFO10000,
         "D::SVS: rgVal1 0x%X, rgVal2 0x%X\n",
         (*prgVal1),
         (*prgVal2)));

    HRESULT hr = SetVariablesAtOffset(dji->m_nativeCodeVersion.GetMethodDesc(),
                                      dji->GetVarNativeInfoCount(),
                                      dji->GetVarNativeInfo(),
                                      offsetTo,
                                      pCtx,
                                      *prgVal1,
                                      *prgVal2,
                                      rgpVCs);

    LOG((LF_CORDB|LF_ENC,
         LL_INFO100000,
         "D::SVS deleting rgVal1 0x%X, rgVal2 0x%X\n",
         (*prgVal1),
         (*prgVal2)));

    DeleteInteropSafe(*prgVal1);
    (*prgVal1) = NULL;
    DeleteInteropSafe(*prgVal2);
    (*prgVal2) = NULL;
    return hr;
}

//
// This class is used by Get and SetVariablesFromOffsets to manage a frameHelper
// list for the arguments and locals corresponding to each varNativeInfo. The first
// four are hidden args, but the remainder will all have a corresponding entry
// in the argument or local signature list.
//
// The structure of the array varNativeInfo contains home information for each variable
// at various points in the function.  Thus, you have to search for the proper native offset
// (IP) in the varNativeInfo, and then find the correct varNumber in that native offset to
// find the correct home information.
//
// Important to note is that the JIT has hidden args that have varNumbers that are negative.
// Thus we cannot use varNumber as a strict index into our holder arrays, and instead shift
// indexes before indexing into our holder arrays.
//
// The hidden args are a fixed-sized array given by the value of 0-UNKNOWN_ILNUM. These are used
// to pass cookies about the arguments (var args, generics, retarg buffer etc.) to the function.
// The real arguments and locals are as one would expect.
//

class GetSetFrameHelper
{
public:
    GetSetFrameHelper();
    ~GetSetFrameHelper();

    HRESULT Init(MethodDesc* pMD);

    bool GetValueClassSizeOfVar(int varNum, ICorDebugInfo::VarLocType varType, SIZE_T* pSize);
    int ShiftIndexForHiddens(int varNum);

private:
    MethodDesc*     m_pMD;
    SIZE_T*         m_rgSize;
    CorElementType* m_rgElemType;
    ULONG           m_numArgs;
    ULONG           m_numTotalVars;

    SIZE_T  GetValueClassSize(MetaSig* pSig);

    static SIZE_T  GetSizeOfElement(CorElementType cet);
};

//
// GetSetFrameHelper::GetSetFrameHelper()
//
// This is the constructor.  It just initailizes all member variables.
//
// parameters: none
//
// return value: none
//
GetSetFrameHelper::GetSetFrameHelper() : m_pMD(NULL), m_rgSize(NULL), m_rgElemType(NULL),
                                         m_numArgs(0), m_numTotalVars(0)
{
    LIMITED_METHOD_CONTRACT;
}

//
// GetSetFrameHelper::Init()
//
// This method extracts the element type and the size of the arguments and locals of the method we are doing
// the SetIP on and stores this information in instance variables.
//
// parameters:   pMD - MethodDesc of the method we are doing the SetIP on
//
// return value: S_OK or E_OUTOFMEMORY
//
HRESULT
GetSetFrameHelper::Init(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    COR_ILMETHOD* pILHeader = NULL;
    m_pMD = pMD;
    MetaSig *pLocSig = NULL;
    MetaSig *pArgSig = NULL;

    m_rgSize = NULL;
    m_rgElemType = NULL;

    // Initialize decoderOldIL before checking the method argument signature.
    EX_TRY
    {
        pILHeader = pMD->GetILHeader();
    }
    EX_CATCH_HRESULT(hr);
    if (FAILED(hr))
        return hr;

    COR_ILMETHOD_DECODER decoderOldIL(pILHeader);
    mdSignature mdLocalSig = (decoderOldIL.GetLocalVarSigTok()) ? (decoderOldIL.GetLocalVarSigTok()):
                                                                  (mdSignatureNil);

    PCCOR_SIGNATURE pCallSig;
    DWORD cbCallSigSize;

    pMD->GetSig(&pCallSig, &cbCallSigSize);

    if (pCallSig != NULL)
    {
        // Yes, we do need to pass in the text because this might be generic function!
        SigTypeContext tmpContext(pMD);

        pArgSig = new (interopsafe, nothrow) MetaSig(pCallSig,
                                                     cbCallSigSize,
                                                     pMD->GetModule(),
                                                     &tmpContext,
                                                     MetaSig::sigMember);

        if (pArgSig == NULL)
        {
            IfFailGo(E_OUTOFMEMORY);
        }

        m_numArgs = pArgSig->NumFixedArgs();

        if (pArgSig->HasThis())
        {
            m_numArgs++;
        }

        // <TODO>
        // What should we do in this case?
        // </TODO>
        /*
        if (argSig.IsVarArg())
            m_numArgs++;
        */
    }

    // allocation of pArgSig succeeded
    ULONG cbSig;
    PCCOR_SIGNATURE pLocalSig;
    pLocalSig = NULL;
    if (mdLocalSig != mdSignatureNil)
    {
        IfFailGo(pMD->GetModule()->GetMDImport()->GetSigFromToken(mdLocalSig, &cbSig, &pLocalSig));
    }
    if (pLocalSig != NULL)
    {
        SigTypeContext tmpContext(pMD);
        pLocSig = new (interopsafe, nothrow) MetaSig(pLocalSig,
                                                     cbSig,
                                                     pMD->GetModule(),
                                                     &tmpContext,
                                                     MetaSig::sigLocalVars);

        if (pLocSig == NULL)
        {
            IfFailGo(E_OUTOFMEMORY);
        }
    }

    // allocation of pLocalSig succeeded
    m_numTotalVars = m_numArgs + (pLocSig != NULL ? pLocSig->NumFixedArgs() : 0);

    if (m_numTotalVars > 0)
    {
        m_rgSize     = new (interopsafe, nothrow) SIZE_T[m_numTotalVars];
        m_rgElemType = new (interopsafe, nothrow) CorElementType[m_numTotalVars];

        if ((m_rgSize == NULL) || (m_rgElemType == NULL))
        {
            IfFailGo(E_OUTOFMEMORY);
        }
        else
        {
            // allocation of m_rgSize and m_rgElemType succeeded
            for (ULONG i = 0; i < m_numTotalVars; i++)
            {
                // Choose the correct signature to walk.
                MetaSig *pCur = NULL;
                if (i < m_numArgs)
                {
                    pCur = pArgSig;
                }
                else
                {
                    pCur = pLocSig;
                }

                // The "this" argument isn't stored in the signature, so we have to
                // check for it manually.
                if (i == 0 && pCur->HasThis())
                {
                    _ASSERTE(pCur == pArgSig);

                    m_rgElemType[i] = ELEMENT_TYPE_CLASS;
                    m_rgSize[i]     = sizeof(SIZE_T);
                }
                else
                {
                    m_rgElemType[i] = pCur->NextArg();

                    if (m_rgElemType[i] == ELEMENT_TYPE_VALUETYPE)
                    {
                        m_rgSize[i] = GetValueClassSize(pCur);
                    }
                    else
                    {
                        m_rgSize[i] = GetSetFrameHelper::GetSizeOfElement(m_rgElemType[i]);
                    }

                    LOG((LF_CORDB, LL_INFO10000, "GSFH::I: var 0x%x is of type %x, size:0x%x\n",
                         i, m_rgElemType[i], m_rgSize[i]));
                }
            }
        } // allocation of m_rgSize and m_rgElemType succeeded
    }   // if there are variables to take care of

ErrExit:
    // clean up
    if (pArgSig != NULL)
    {
        DeleteInteropSafe(pArgSig);
    }

    if (pLocSig != NULL)
    {
        DeleteInteropSafe(pLocSig);
    }

    if (FAILED(hr))
    {
        if (m_rgSize != NULL)
        {
            DeleteInteropSafe(m_rgSize);
        }

        if (m_rgElemType != NULL)
        {
            DeleteInteropSafe((int*)m_rgElemType);
        }
    }

    return hr;
} // GetSetFrameHelper::Init

//
// GetSetFrameHelper::~GetSetFrameHelper()
//
// This is the destructor.  It checks the two arrays we have allocated and frees the memory accordingly.
//
// parameters:   none
//
// return value: none
//
GetSetFrameHelper::~GetSetFrameHelper()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_rgSize)
    {
        DeleteInteropSafe(m_rgSize);
    }

    if (m_rgElemType)
    {
        DeleteInteropSafe((int*)m_rgElemType);
    }
}

//
// GetSetFrameHelper::GetSizeOfElement()
//
// Given a CorElementType, this function returns the size of this type.
// Note that this function doesn't handle ELEMENT_TYPE_VALUETYPE.  Use GetValueClassSize() instead.
//
// parameters:   cet - the CorElementType of the argument/local we are dealing with
//
// return value: the size of the argument/local
//
// static
SIZE_T GetSetFrameHelper::GetSizeOfElement(CorElementType cet)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(cet != ELEMENT_TYPE_VALUETYPE);
    }
    CONTRACTL_END;

    if (!CorIsPrimitiveType(cet))
    {
        return sizeof(SIZE_T);
    }
    else
    {
        switch (cet)
        {
        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
#if defined(HOST_64BIT)
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
#endif // HOST_64BIT
        case ELEMENT_TYPE_R8:
               return 8;

        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
#if !defined(HOST_64BIT)
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
#endif // !HOST_64BIT
        case ELEMENT_TYPE_R4:
            return 4;

        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
        case ELEMENT_TYPE_CHAR:
            return 2;

        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
        case ELEMENT_TYPE_BOOLEAN:
            return 1;

        case ELEMENT_TYPE_VOID:
        case ELEMENT_TYPE_END:
            _ASSERTE(!"debugger.cpp - Check this code path\n");
            return 0;

        case ELEMENT_TYPE_STRING:
            return sizeof(SIZE_T);

        default:
            _ASSERTE(!"debugger.cpp - Check this code path\n");
            return sizeof(SIZE_T);
        }
    }
}

//
// GetSetFrameHelper::GetValueClassSize()
//
// Given a MetaSig pointer to the signature of a value type, this function returns its size.
//
// parameters:   pSig - MetaSig pointer to the signature of a value type
//
// return value: the size of this value type
//
SIZE_T GetSetFrameHelper::GetValueClassSize(MetaSig* pSig)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pSig));
    }
    CONTRACTL_END;

    // We need to determine the number of bytes for this value-type.
    SigPointer sp = pSig->GetArgProps();

    TypeHandle vcType = TypeHandle();
    {
        // Lookup operations run the class loader in non-load mode.
        ENABLE_FORBID_GC_LOADER_USE_IN_THIS_SCOPE();

        // This will return Null if type is not restored
        // @todo : is this what we want?
        SigTypeContext typeContext(m_pMD);
        vcType = sp.GetTypeHandleThrowing(m_pMD->GetModule(),
                                          &typeContext,
                                          // == FailIfNotLoaded
                                          ClassLoader::DontLoadTypes);
    }
    // We need to know the size of the class in bytes. This means:
    // - we need a specific instantiation (since that affects size)
    // - but we don't care if it's shared (since it will be the same size either way)
    _ASSERTE(!vcType.IsNull() && vcType.IsValueType());

    return (vcType.GetMethodTable()->GetAlignedNumInstanceFieldBytes());
}

//
// GetSetFrameHelper::GetValueClassSizeOfVar()
//
// This method retrieves the size of the variable saved in the array m_rgSize.  Also, it returns true
// if the variable is a value type.
//
// parameters:   varNum  - the variable number (arguments come before locals)
//               varType - the type of variable home
//               pSize   - [out] the size
//
// return value: whether this variable is a value type
//
bool GetSetFrameHelper::GetValueClassSizeOfVar(int varNum, ICorDebugInfo::VarLocType varType, SIZE_T* pSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(varType != ICorDebugInfo::VLT_FIXED_VA);
        PRECONDITION(pSize != NULL);
    }
    CONTRACTL_END;

    // preliminary checking
    if (varNum < 0)
    {
        // Make sure this is one of the secret parameters (e.g. VASigCookie, generics context, etc.).
        _ASSERTE(varNum > (int)ICorDebugInfo::MAX_ILNUM);

        *pSize = sizeof(LPVOID);
        return false;
    }

    // This check is only safe after we make sure that varNum is not negative.
    if ((UINT)varNum >= m_numTotalVars)
    {
        _ASSERTE(!"invalid variable index encountered during setip");
        *pSize = 0;
        return false;
    }

    CorElementType cet = m_rgElemType[varNum];
    *pSize = m_rgSize[varNum];

    if ((cet != ELEMENT_TYPE_VALUETYPE) ||
        (varType == ICorDebugInfo::VLT_REG) ||
        (varType == ICorDebugInfo::VLT_REG_REG) ||
        (varType == ICorDebugInfo::VLT_REG_STK) ||
        (varType == ICorDebugInfo::VLT_STK_REG))
    {
        return false;
    }
    else
    {
        return true;
    }
}

int GetSetFrameHelper::ShiftIndexForHiddens(int varNum)
{
    LIMITED_METHOD_CONTRACT;

    //
    // Need to shift them up so are appropriate index for rgVal arrays
    //
    return varNum - ICorDebugInfo::UNKNOWN_ILNUM;
}

// Helper method pair to grab all, then set all, variables at a given
// point in a routine.
// NOTE: GetVariablesFromOffset and SetVariablesAtOffset are
// very similar - modifying one will probably need to be reflected in the other...
// rgVal1 and rgVal2 are preallocated by callers with estimated size.
// We pass in the size of the allocation in rRgValeSize. The safe index will be rgVal1[0..uRgValSize - 1]
//
HRESULT Debugger::GetVariablesFromOffset(MethodDesc  *pMD,
                                         UINT varNativeInfoCount,
                                         ICorDebugInfo::NativeVarInfo *varNativeInfo,
                                         SIZE_T offsetFrom,
                                         CONTEXT *pCtx,
                                         SIZE_T  *rgVal1,
                                         SIZE_T  *rgVal2,
                                         UINT    uRgValSize, // number of elements of the preallocated rgVal1 and rgVal2
                                         BYTE ***rgpVCs)
{
    // @todo - convert this to throwing w/ holders. It will be cleaner.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(rgpVCs));
        PRECONDITION(CheckPointer(pCtx));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(varNativeInfo));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(rgVal1));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(rgVal2));
        // This may or may not be called on the helper thread.
    }
    CONTRACTL_END;

    *rgpVCs = NULL;
    // if there are no locals, well, we are done!

    if (varNativeInfoCount == 0)
    {
        return S_OK;
    }

    memset( rgVal1, 0, sizeof(SIZE_T)*uRgValSize);
    memset( rgVal2, 0, sizeof(SIZE_T)*uRgValSize);

    LOG((LF_CORDB|LF_ENC, LL_INFO10000, "D::GVFO: %s::%s, infoCount:0x%x, from:0x%p\n",
         pMD->m_pszDebugClassName,
         pMD->m_pszDebugMethodName,
         varNativeInfoCount,
         offsetFrom));

    GetSetFrameHelper frameHelper;
    HRESULT hr = frameHelper.Init(pMD);
    if (FAILED(hr))
    {
        return hr;
    }
    // preallocate enough to hold all possible valueclass args & locals
    // sure this is more than we need, but not a big deal and better
    // than having to crawl through the frameHelper and count
    ULONG cValueClasses = 0;
    BYTE **rgpValueClasses = new (interopsafe, nothrow)  BYTE *[varNativeInfoCount];
    if (rgpValueClasses == NULL)
    {
        return E_OUTOFMEMORY;
    }
    memset(rgpValueClasses, 0, sizeof(BYTE *)*varNativeInfoCount);

    hr = S_OK;

    LOG((LF_CORDB|LF_ENC,
         LL_INFO10000,
         "D::GVFO rgVal1 0x%X, rgVal2 0x%X\n",
         rgVal1,
         rgVal2));

    // Now go through the full array and save off each arg and local
    for (UINT i = 0; i< varNativeInfoCount;i++)
    {
        // Ignore variables not live at offsetFrom
        //
        // #VarLife
        //
        // The condition below is a little strange. If a var is alive when this is true:
        //
        // startOffset <= offsetFrom < endOffset
        //
        // Then you'd expect the negated expression below to be:
        //
        // startOffset > offsetFrom || endOffset <= offsetFrom
        //
        // instead of what we're doing ("<" instead of "<="):
        //
        // startOffset > offsetFrom || endOffset < offsetFrom
        //
        // I'm not sure if the condition below is a mistake, or if it's intentionally
        // mirroring a workaround from FindNativeInfoInILVariableArray() (Debug\DI\module.cpp)
        // to deal with optimized code. So I'm leaving it alone for now. See
        // code:FindNativeInfoInILVariableArray for more info on this workaround.
        if ((varNativeInfo[i].startOffset > offsetFrom) ||
            (varNativeInfo[i].endOffset < offsetFrom) ||
            (varNativeInfo[i].loc.vlType == ICorDebugInfo::VLT_INVALID))
        {
            LOG((LF_CORDB|LF_ENC,LL_INFO10000, "D::GVFO [%2d] invalid\n", i));
            continue;
        }

        SIZE_T cbClass;
        bool isVC = frameHelper.GetValueClassSizeOfVar(varNativeInfo[i].varNumber,
                                                       varNativeInfo[i].loc.vlType,
                                                       &cbClass);

        if (!isVC)
        {
            int rgValIndex = frameHelper.ShiftIndexForHiddens(varNativeInfo[i].varNumber);

            _ASSERTE(rgValIndex >= 0 && rgValIndex < (int)uRgValSize);

            BOOL res = GetNativeVarVal(varNativeInfo[i].loc,
                                       pCtx,
                                       rgVal1 + rgValIndex,
                                       rgVal2 + rgValIndex
                                       BIT64_ARG(cbClass));

            LOG((LF_CORDB|LF_ENC,LL_INFO10000,
                 "D::GVFO [%2d] varnum %d, nonVC type %x, addr %8.8x: %8.8x;%8.8x\n",
                 i,
                 varNativeInfo[i].varNumber,
                 varNativeInfo[i].loc.vlType,
                 NativeVarStackAddr(varNativeInfo[i].loc, pCtx),
                 rgVal1[rgValIndex],
                 rgVal2[rgValIndex]));

            if (res == TRUE)
            {
                continue;
            }

            _ASSERTE(res == TRUE);
            hr = E_FAIL;
            break;
        }

        // it's definately a value class
        // Make space for it - note that it uses the VC index, NOT the variable index
        _ASSERTE(cbClass != 0);
        rgpValueClasses[cValueClasses] = new (interopsafe, nothrow) BYTE[cbClass];
        if (rgpValueClasses[cValueClasses] == NULL)
        {
            hr = E_OUTOFMEMORY;
            break;
        }
        memcpy(rgpValueClasses[cValueClasses],
               NativeVarStackAddr(varNativeInfo[i].loc, pCtx),
               cbClass);

        // Move index up.
        cValueClasses++;
#ifdef _DEBUG
        LOG((LF_CORDB|LF_ENC,LL_INFO10000,
             "D::GVFO [%2d] varnum %d, VC len %d, addr %8.8x, sample: %8.8x%8.8x\n",
             i,
             varNativeInfo[i].varNumber,
             cbClass,
             NativeVarStackAddr(varNativeInfo[i].loc, pCtx),
             (rgpValueClasses[cValueClasses-1])[0], (rgpValueClasses[cValueClasses-1])[1]));
#endif
    }

    LOG((LF_CORDB|LF_ENC, LL_INFO10000, "D::GVFO: returning %8.8x\n", hr));
    if (SUCCEEDED(hr))
    {
        (*rgpVCs) = rgpValueClasses;
        return hr;
    }

    // We failed for some reason
    if (rgpValueClasses != NULL)
    {   // free any memory we allocated for VCs here
        while(cValueClasses > 0)
        {
            --cValueClasses;
            DeleteInteropSafe(rgpValueClasses[cValueClasses]);  // OK to delete NULL
        }
        DeleteInteropSafe(rgpValueClasses);
        rgpValueClasses = NULL;
    }
    return hr;
}

// NOTE: GetVariablesFromOffset and SetVariablesAtOffset are
// very similar - modifying one will probably need to be reflected in the other...
HRESULT Debugger::SetVariablesAtOffset(MethodDesc  *pMD,
                                       UINT varNativeInfoCount,
                                       ICorDebugInfo::NativeVarInfo *varNativeInfo,
                                       SIZE_T offsetTo,
                                       CONTEXT *pCtx,
                                       SIZE_T  *rgVal1,
                                       SIZE_T  *rgVal2,
                                       BYTE **rgpVCs)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pCtx));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(rgpVCs));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(varNativeInfo));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(rgVal1));
        PRECONDITION(varNativeInfoCount == 0 || CheckPointer(rgVal2));
        // This may or may not be called on the helper thread.
    }
    CONTRACTL_END;

    LOG((LF_CORDB|LF_ENC, LL_INFO10000, "D::SVAO: %s::%s, infoCount:0x%x, to:0x%p\n",
         pMD->m_pszDebugClassName,
         pMD->m_pszDebugMethodName,
         varNativeInfoCount,
         offsetTo));

    if (varNativeInfoCount == 0)
    {
        return S_OK;
    }

    GetSetFrameHelper frameHelper;
    HRESULT hr = frameHelper.Init(pMD);
    if (FAILED(hr))
    {
        return hr;
    }

    ULONG iVC = 0;
    hr = S_OK;

    // Note that since we obtain all the variables in the first loop, we
    // can now splatter those variables into their new locations
    // willy-nilly, without the fear that variable locations that have
    // been swapped might accidentally overwrite a variable value.
    for (UINT i = 0;i< varNativeInfoCount;i++)
    {
        // Ignore variables not live at offsetTo
        //
        // If this IF condition looks wrong to you, see
        // code:Debugger::GetVariablesFromOffset#VarLife for more info
        if ((varNativeInfo[i].startOffset > offsetTo) ||
            (varNativeInfo[i].endOffset < offsetTo) ||
            (varNativeInfo[i].loc.vlType == ICorDebugInfo::VLT_INVALID))
        {
            LOG((LF_CORDB|LF_ENC,LL_INFO10000, "D::SVAO [%2d] invalid\n", i));
            continue;
        }

        SIZE_T cbClass;
        bool isVC = frameHelper.GetValueClassSizeOfVar(varNativeInfo[i].varNumber,
                                                       varNativeInfo[i].loc.vlType,
                                                       &cbClass);

        if (!isVC)
        {
            int rgValIndex = frameHelper.ShiftIndexForHiddens(varNativeInfo[i].varNumber);

            _ASSERTE(rgValIndex >= 0);

            BOOL res = SetNativeVarVal(varNativeInfo[i].loc,
                                       pCtx,
                                       rgVal1[rgValIndex],
                                       rgVal2[rgValIndex]
                                       BIT64_ARG(cbClass));

            LOG((LF_CORDB|LF_ENC,LL_INFO10000,
                 "D::SVAO [%2d] varnum %d, nonVC type %x, addr %8.8x: %8.8x;%8.8x\n",
                 i,
                 varNativeInfo[i].varNumber,
                 varNativeInfo[i].loc.vlType,
                 NativeVarStackAddr(varNativeInfo[i].loc, pCtx),
                 rgVal1[rgValIndex],
                 rgVal2[rgValIndex]));

            if (res == TRUE)
            {
                continue;
            }
            _ASSERTE(res == TRUE);
            hr = E_FAIL;
            break;
        }

        // It's definately a value class.
        _ASSERTE(cbClass != 0);
        if (rgpVCs[iVC] == NULL)
        {
            // it's new in scope, so just clear it
            memset(NativeVarStackAddr(varNativeInfo[i].loc, pCtx), 0, cbClass);
            LOG((LF_CORDB|LF_ENC,LL_INFO10000, "D::SVAO [%2d] varnum %d, new VC len %d, addr %8.8x\n",
                 i,
                 varNativeInfo[i].varNumber,
                 cbClass,
                 NativeVarStackAddr(varNativeInfo[i].loc, pCtx)));
            continue;
        }
        // it's a pre-existing VC, so copy it
        memmove(NativeVarStackAddr(varNativeInfo[i].loc, pCtx), rgpVCs[iVC], cbClass);
#ifdef _DEBUG
        LOG((LF_CORDB|LF_ENC,LL_INFO10000,
             "D::SVAO [%2d] varnum %d, VC len %d, addr: %8.8x sample: %8.8x%8.8x\n",
             i,
             varNativeInfo[i].varNumber,
             cbClass,
             NativeVarStackAddr(varNativeInfo[i].loc, pCtx),
             rgpVCs[iVC][0],
             rgpVCs[iVC][1]));
#endif
        // Now get rid of the memory
        DeleteInteropSafe(rgpVCs[iVC]);
        rgpVCs[iVC] = NULL;
        iVC++;
    }

    LOG((LF_CORDB|LF_ENC, LL_INFO10000, "D::SVAO: returning %8.8x\n", hr));

    if (rgpVCs != NULL)
    {
        DeleteInteropSafe(rgpVCs);
    }

    return hr;
}

BOOL IsDuplicatePatch(SIZE_T *rgEntries,
                      ULONG cEntries,
                      SIZE_T Entry )
{
    LIMITED_METHOD_CONTRACT;

    for( ULONG i = 0; i < cEntries;i++)
    {
        if (rgEntries[i] == Entry)
            return TRUE;
    }
    return FALSE;
}


/******************************************************************************
// HRESULT Debugger::MapAndBindFunctionBreakpoints():  For each breakpoint
//      that we've set in any version of the existing function,
//      set a correponding breakpoint in the new function if we haven't moved
//      the patch to the new version already.
//
//      This must be done _AFTER_ the MethodDesc has been udpated
//      with the new address (ie, when GetFunctionAddress pFD returns
//      the address of the new EnC code)
//
// Parameters:
// djiNew - this is the DJI created in D::JitComplete.
//   If djiNew == NULL iff we aren't tracking debug-info.
// fd - the method desc that we're binding too.
// addrOfCode - address of the native blob of code we just jitted
//
//  <TODO>@todo Replace array with hashtable for improved efficiency</TODO>
//  <TODO>@todo Need to factor code,so that we can selectively map forward DFK(ilOFfset) BPs</TODO>
 ******************************************************************************/
HRESULT Debugger::MapAndBindFunctionPatches(DebuggerJitInfo *djiNew,
                                            MethodDesc * fd,
                                            CORDB_ADDRESS_TYPE *addrOfCode)
{
    // @@@
    // Internal helper API. Can be called from Debugger or Controller.
    //

    CONTRACTL
    {
        THROWS;
        CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT;
        PRECONDITION(!djiNew || djiNew->m_nativeCodeVersion.GetMethodDesc() == fd);
    }
    CONTRACTL_END;

    HRESULT     hr =                S_OK;
    HASHFIND    hf;
    SIZE_T      *pidTableEntry =    NULL;
    SIZE_T      pidInCaseTableMoves;
    Module      *pModule =          g_pEEInterface->MethodDescGetModule(fd);
    mdMethodDef md =                fd->GetMemberDef();

    LOG((LF_CORDB,LL_INFO10000,"D::MABFP: All BPs will be mapped to "
        "Ver:0x%04x (DJI:0x%08x)\n", djiNew?djiNew->m_methodInfo->GetCurrentEnCVersion():0, djiNew));

    // We need to traverse the patch list while under the controller lock (small lock).
    // But we can only send BreakpointSetErros while under the debugger lock (big lock).
    // So to avoid a lock violation, we queue any errors we find under the small lock,
    // and then send the whole list when under the big lock.
    PATCH_UNORDERED_ARRAY listUnbindablePatches;


    // First lock the patch table so it doesn't move while we're
    //  examining it.
    LOG((LF_CORDB,LL_INFO10000, "D::MABFP: About to lock patch table\n"));
    {
        DebuggerController::ControllerLockHolder ch;

        // Manipulate tables AFTER lock's been acquired.
        DebuggerPatchTable *pPatchTable = DebuggerController::GetPatchTable();
        GetBPMappingDuplicates()->Clear(); //dups are tracked per-version

        for (DebuggerControllerPatch *dcp = pPatchTable->GetFirstPatch(&hf);
             dcp != NULL;
             dcp = pPatchTable->GetNextPatch( &hf ))
        {

            LOG((LF_CORDB, LL_INFO10000, "D::MABFP: got patch 0x%p\n", dcp));

            // Only copy over breakpoints that are in this method
            // Ideally we'd have a per-method index since there can be a lot of patches
            // when the EnCBreakpoint patches are included.
            if (dcp->key.module != pModule || dcp->key.md != md)
            {
                LOG((LF_CORDB, LL_INFO10000, "Patch not in this method\n"));
                continue;
            }

            // If the patch only applies in certain generic instances, don't bind it
            // elsewhere.
            if(dcp->pMethodDescFilter != NULL && dcp->pMethodDescFilter != djiNew->m_nativeCodeVersion.GetMethodDesc())
            {
                LOG((LF_CORDB, LL_INFO10000, "Patch not in this generic instance\n"));
                continue;
            }


            // Do not copy over slave breakpoint patches.  Instead place a new slave
            // based off the master.
            if (dcp->IsILSlavePatch())
            {
                LOG((LF_CORDB, LL_INFO10000, "Not copying over slave breakpoint patch\n"));
                continue;
            }

            // If the patch is already bound, then we don't want to try to rebind it.
            // Eg. It may be bound to a different generic method instantiation.
            if (dcp->IsBound())
            {
                LOG((LF_CORDB, LL_INFO10000, "Skipping already bound patch\n"));
                continue;
            }

            // Only apply breakpoint patches that are for this version.
            // If the patch doesn't have a particular EnCVersion available from its data then
            // we're (probably) not tracking JIT info.
            if (dcp->IsBreakpointPatch() && dcp->HasEnCVersion() && djiNew && dcp->GetEnCVersion() != djiNew->m_encVersion)
            {
                LOG((LF_CORDB, LL_INFO10000, "Not applying breakpoint patch to new version\n"));
                continue;
            }

            // Only apply breakpoint and stepper patches
            //
            // The DJI gets deleted as part of the Unbind/Rebind process in MovedCode.
            // This is to signal that we should not skip here.
            // <NICE> under exactly what scenarios (EnC, code pitching etc.) will this apply?... </NICE>
            // <NICE> can't we be a little clearer about why we don't want to bind the patch in this arcane situation?</NICE>
            if (dcp->HasDJI() && !dcp->IsBreakpointPatch() &&  !dcp->IsStepperPatch())
            {
                LOG((LF_CORDB, LL_INFO10000, "Neither stepper nor BP but we have valid a DJI (i.e. the DJI hasn't been deleted as part of the Unbind/MovedCode/Rebind mess)! - getting next patch!\n"));
                continue;
            }

            // Now check if we're tracking JIT info or not
            if (djiNew == NULL)
            {
                // This means we put a patch in a method w/ no debug info.
                _ASSERTE(dcp->IsBreakpointPatch() ||
                    dcp->IsStepperPatch() ||
                    dcp->controller->GetDCType() == DEBUGGER_CONTROLLER_THREAD_STARTER);

                // W/o Debug-info, We can only patch native offsets, and only at the start of the method (native offset 0).
                // <TODO> Why can't we patch other native offsets??
                // Maybe b/c we don't know if we're patching
                // in the middle of an instruction. Though that's not a
                // strict requirement.</TODO>
                // We can't even do a IL-offset 0 because that's after the prolog and w/o the debug-info,
                // we don't know where the prolog ends.
                // Failing this assert is arguably an API misusage - the debugger should have enabled
                // jit-tracking if they wanted to put bps at offsets other than native:0.
                if (dcp->IsNativePatch() && (dcp->offset == 0))
                {
                    DebuggerController::g_patches->BindPatch(dcp, addrOfCode);
                    DebuggerController::ActivatePatch(dcp);
                }
                else
                {
                    // IF a debugger calls EnableJitDebugging(true, ...) in the module-load callback,
                    // we should never get here.
                    *(listUnbindablePatches.AppendThrowing()) = dcp;
                }

            }
            else
            {
                pidInCaseTableMoves = dcp->pid;

                // If we've already mapped this one to the current version,
                //  don't map it again.
                LOG((LF_CORDB,LL_INFO10000,"D::MABFP: Checking if 0x%x is a dup...",
                    pidInCaseTableMoves));

                if ( IsDuplicatePatch(GetBPMappingDuplicates()->Table(),
                    GetBPMappingDuplicates()->Count(),
                    pidInCaseTableMoves) )
                {
                    LOG((LF_CORDB,LL_INFO10000,"it is!\n"));
                    continue;
                }
                LOG((LF_CORDB,LL_INFO10000,"nope!\n"));

                // Attempt mapping from patch to new version of code, and
                // we don't care if it turns out that there isn't a mapping.
                // <TODO>@todo-postponed: EnC: Make sure that this doesn't cause
                // the patch-table to shift.</TODO>
                hr = MapPatchToDJI( dcp, djiNew );
                if (CORDBG_E_CODE_NOT_AVAILABLE == hr )
                {
                    *(listUnbindablePatches.AppendThrowing()) = dcp;
                    hr = S_OK;
                }

                if (FAILED(hr))
                    break;

                //Remember the patch id to prevent duplication later
                pidTableEntry = GetBPMappingDuplicates()->Append();
                if (NULL == pidTableEntry)
                {
                    hr = E_OUTOFMEMORY;
                    break;
                }

                *pidTableEntry = pidInCaseTableMoves;
                LOG((LF_CORDB,LL_INFO10000,"D::MABFP Adding 0x%x to list of "
                    "already mapped patches\n", pidInCaseTableMoves));
            }
        }

        // unlock controller lock before sending events.
    }
    LOG((LF_CORDB,LL_INFO10000, "D::MABFP: Unlocked patch table\n"));


    // Now send any Breakpoint bind error events.
    if (listUnbindablePatches.Count() > 0)
    {
        LockAndSendBreakpointSetError(&listUnbindablePatches);
    }

    return hr;
}

/******************************************************************************
// HRESULT Debugger::MapPatchToDJI():  Maps the given
//  patch to the corresponding location at the new address.
//  We assume that the new code has been JITTed.
// Returns:  CORDBG_E_CODE_NOT_AVAILABLE - Indicates that a mapping wasn't
//  available, and thus no patch was placed.  The caller may or may
//  not care.
 ******************************************************************************/
HRESULT Debugger::MapPatchToDJI( DebuggerControllerPatch *dcp,DebuggerJitInfo *djiTo)
{
    CONTRACTL
    {
        THROWS;
        CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT;
        PRECONDITION(djiTo != NULL);
        PRECONDITION(djiTo->m_jitComplete == true);
    }
    CONTRACTL_END;

    _ASSERTE(DebuggerController::HasLock());
#ifdef _DEBUG
    static BOOL shouldBreak = -1;
    if (shouldBreak == -1)
        shouldBreak = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgBreakOnMapPatchToDJI);

    if (shouldBreak > 0) {
        _ASSERTE(!"DbgBreakOnMatchPatchToDJI");
    }
#endif

    LOG((LF_CORDB, LL_EVERYTHING, "Calling MapPatchToDJI\n"));

    // We shouldn't have been asked to map an already bound patch
    _ASSERTE( !dcp->IsBound() );
    if ( dcp->IsBound() )
    {
        return S_OK;
    }

    // If the patch has no DJI then we're doing a UnbindFunctionPatches/RebindFunctionPatches.  Either
    // way, we simply want the most recent version.  In the absence of EnC we should have djiCur == djiTo.
    DebuggerJitInfo *djiCur = dcp->HasDJI() ? dcp->GetDJI() : djiTo;
    PREFIX_ASSUME(djiCur != NULL);

    // If the source and destination are the same version, then this method
    // decays into BindFunctionPatch's BindPatch function
    if (djiCur->m_encVersion == djiTo->m_encVersion)
    {
        // If the patch is a "master" then make a new "slave" patch instead of
        // binding the old one.  This is to stop us mucking with the master breakpoint patch
        // which we may need to bind several times for generic code.
        if (dcp->IsILMasterPatch())
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Add, Bind, Activate new patch from master patch\n"));
            if (dcp->controller->AddBindAndActivateILSlavePatch(dcp, djiTo))
            {
                LOG((LF_CORDB, LL_INFO1000, "Add, Bind Activate went fine!\n" ));
                return S_OK;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO1000, "Didn't work for some reason!\n"));

                // Caller can track this HR and send error.
                return CORDBG_E_CODE_NOT_AVAILABLE;
            }
        }
        else
        {
            // <TODO>
            // We could actually have a native managed patch here.  This patch is probably added
            // as a result of tracing a patch.  See if we can eliminate the need for this code path
            // </TODO>
            _ASSERTE( dcp->GetKind() == PATCH_KIND_NATIVE_MANAGED );

            // We have an unbound native patch (eg. for PatchTrace), lets try to bind and activate it
            dcp->SetDJI(djiTo);
            LOG((LF_CORDB, LL_EVERYTHING, "trying to bind patch... could be problem\n"));
            if (DebuggerController::BindPatch(dcp, djiTo->m_nativeCodeVersion.GetMethodDesc(), NULL))
            {
                DebuggerController::ActivatePatch(dcp);
                LOG((LF_CORDB, LL_INFO1000, "Application went fine!\n" ));
                return S_OK;
            }
            else
            {
                LOG((LF_CORDB, LL_INFO1000, "Didn't apply for some reason!\n"));

                // Caller can track this HR and send error.
                return CORDBG_E_CODE_NOT_AVAILABLE;
            }
        }
    }

    // Breakpoint patches never get mapped over
    _ASSERTE(!dcp->IsBreakpointPatch());

    return S_OK;
}


/* ------------------------------------------------------------------------ *
 * EE Interface routines
 * ------------------------------------------------------------------------ */

//
// SendSyncCompleteIPCEvent sends a Sync Complete event to the Right Side.
//
void Debugger::SendSyncCompleteIPCEvent(bool isEESuspendedForGC)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThreadHoldsLock());

        // Anyone sending the synccomplete must hold the TSL.
        PRECONDITION(ThreadStore::HoldingThreadStore() || g_fProcessDetach);

        // The sync complete is now only sent on a helper thread.
        if (!isEESuspendedForGC)
        {
            PRECONDITION(ThisIsHelperThreadWorker());
        }
        MODE_COOPERATIVE;

        // We had better be trapping Runtime threads and not stopped yet.
        if (isEESuspendedForGC)
        {
            PRECONDITION(m_stopped);
        }
        else
        {
            PRECONDITION(m_stopped && m_trappingRuntimeThreads);
        }
    }
    CONTRACTL_END;

    // @@@
    // Internal helper API.
    // This is to send Sync Complete event to RightSide.
    // We should have hold the debugger lock
    //

    STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::SSCIPCE: sync complete.\n");

    // Synchronizing while in in rude shutdown should be extremely rare b/c we don't
    // TART in rude shutdown. Shutdown must have started after we started to sync.
    // We know we're not on the shutdown thread here.
    // And we also know we can't block the shutdown thread (b/c it has the TSL and will
    // get a free pass through the GC toggles that normally block threads for debugging).
    if (g_fProcessDetach)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::SSCIPCE: Skipping for shutdown.\n");
        return;
    }

    // If we're not marked as attached yet, then do that now.
    // This can be safely called multiple times.
    // This can happen in the normal attach case. The Right-side sends an async-break,
    // but we don't want to be considered attach until we've actually gotten our first synchronization.
    // Else threads may slip forward during attach and send debug events while we're tyring to attach.
    MarkDebuggerAttachedInternal();

    DebuggerIPCControlBlock * pDCB;
    pDCB = m_pRCThread->GetDCB();
    (void)pDCB; //prevent "unused variable" error from GCC

    PREFIX_ASSUME(pDCB != NULL); // must have DCB by the time we're sending IPC events.
#ifdef FEATURE_INTEROP_DEBUGGING
    // The synccomplete can't be the first IPC event over. That's b/c the LS needs to know
    // if we're interop-debugging and the RS needs to know special addresses for interop-debugging
    // (like flares). All of this info is in the DCB.
    if (pDCB->m_rightSideIsWin32Debugger)
    {

        // If the Right Side is the win32 debugger of this process, then we need to throw a special breakpoint exception
        // here instead of sending the sync complete event. The Right Side treats this the same as a sync complete
        // event, but its also able to suspend unmanaged threads quickly.
        // This also prevents races between sending the sync-complete and getting a native debug event
        // (since the sync-complete becomes a native debug event, and all native debug events are serialized).
        //
        // Note: we reset the syncThreadIsLockFree event before sending the sync complete flare. This thread will set
        // this event once its released the debugger lock. This will prevent the Right Side from suspending this thread
        // until it has released the debugger lock.
        Debugger::NotifyRightSideOfSyncComplete();
    }
    else
#endif // FEATURE_INTEROP_DEBUGGING
    {
        STRESS_LOG0(LF_CORDB, LL_EVERYTHING, "GetIPCEventSendBuffer called in SendSyncCompleteIPCEvent\n");
        // Send the Sync Complete event to the Right Side
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce, DB_IPCE_SYNC_COMPLETE);

        m_pRCThread->SendIPCEvent();
    }
}

//
// Lookup or create a DebuggerModule for the given pDomainFile.
//
// Arguments:
//    pDomainFile - non-null domain file.
//
// Returns:
//   DebuggerModule instance for the given domain file. May be lazily created.
//
// Notes:
//  @dbgtodo JMC - this should go away when we get rid of DebuggerModule.
//

DebuggerModule * Debugger::LookupOrCreateModule(DomainFile * pDomainFile)
{
    _ASSERTE(pDomainFile != NULL);
    LOG((LF_CORDB, LL_INFO1000, "D::LOCM df=0x%x\n", pDomainFile));
    DebuggerModule * pDModule = LookupOrCreateModule(pDomainFile->GetModule(), pDomainFile->GetAppDomain());
    LOG((LF_CORDB, LL_INFO1000, "D::LOCM m=0x%x ad=0x%x -> dm=0x%x\n", pDomainFile->GetModule(), pDomainFile->GetAppDomain(), pDModule));
    _ASSERTE(pDModule != NULL);
    _ASSERTE(pDModule->GetDomainFile() == pDomainFile);

    return pDModule;
}

// Overloaded Wrapper around for VMPTR_DomainFile-->DomainFile*
//
// Arguments:
//    vmDomainFile - VMPTR cookie for a domain file. This can be NullPtr().
//
// Returns:
//    Debugger Module instance for the given domain file. May be lazily created.
//
// Notes:
//    VMPTR comes from IPC events
DebuggerModule * Debugger::LookupOrCreateModule(VMPTR_DomainFile vmDomainFile)
{
    DomainFile * pDomainFile = vmDomainFile.GetRawPtr();
    if (pDomainFile == NULL)
    {
        return NULL;
    }
    return LookupOrCreateModule(pDomainFile);
}

// Lookup or create a DebuggerModule for the given (Module, AppDomain) pair.
//
// Arguments:
//    pModule - required runtime module. May be domain netural.
//    pAppDomain - required appdomain that the module is in.
//
// Returns:
//    Debugger Module isntance for the given domain file. May be lazily created.
//
DebuggerModule* Debugger::LookupOrCreateModule(Module* pModule, AppDomain *pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "D::LOCM m=0x%x ad=0x%x\n", pModule, pAppDomain));

    // DebuggerModules are relative to a specific AppDomain so we should always be looking up a module /
    // AppDomain pair.
    _ASSERTE( pModule != NULL );
    _ASSERTE( pAppDomain != NULL );

    // This is called from all over. We just need to lock in order to lookup. We don't need
    // the lock when actually using the DebuggerModule (since it won't be unloaded as long as there is a thread
    // in that appdomain). Many of our callers already have this lock, many don't.
    // We can take the lock anyways because it's reentrant.
    DebuggerDataLockHolder ch(g_pDebugger); // need to traverse module list

    // if this is a module belonging to the system assembly, then scan
    // the complete list of DebuggerModules looking for the one
    // with a matching appdomain id
    // it.

    DebuggerModule* dmod = NULL;

    if (m_pModules != NULL)
    {
        dmod = m_pModules->GetModule(pModule);
    }

    // If it doesn't exist, create it.
    if (dmod == NULL)
    {
        LOG((LF_CORDB, LL_INFO1000, "D::LOCM dmod for m=0x%x ad=0x%x not found, creating.\n", pModule, pAppDomain));
        HRESULT hr = S_OK;
        EX_TRY
        {
            DomainFile * pDomainFile = pModule->GetDomainFile();
            SIMPLIFYING_ASSUMPTION(pDomainFile != NULL);
            dmod = AddDebuggerModule(pDomainFile); // throws
        }
        EX_CATCH_HRESULT(hr);
        SIMPLIFYING_ASSUMPTION(dmod != NULL); // may not be true in OOM cases; but LS doesn't handle OOM.
    }

    // The module must be in the AppDomain that was requested
    _ASSERTE( (dmod == NULL) || (dmod->GetAppDomain() == pAppDomain) );

    LOG((LF_CORDB, LL_INFO1000, "D::LOCM m=0x%x ad=0x%x -> dm=0x%x(Mod=0x%x, DomFile=0x%x, AD=0x%x)\n",
        pModule, pAppDomain, dmod, dmod->GetRuntimeModule(), dmod->GetDomainFile(), dmod->GetAppDomain()));
    return dmod;
}

// Create a new DebuggerModule object
//
// Arguments:
//    pDomainFile-  runtime domain file to create debugger module object around
//
// Returns:
//    New instnace of a DebuggerModule. Throws on failure.
//
DebuggerModule* Debugger::AddDebuggerModule(DomainFile * pDomainFile)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "D::ADM df=0x%x\n", pDomainFile));
    DebuggerDataLockHolder chInfo(this);

    Module *     pRuntimeModule = pDomainFile->GetCurrentModule();
    AppDomain *  pAppDomain     = pDomainFile->GetAppDomain();

    HRESULT hr = CheckInitModuleTable();
    IfFailThrow(hr);

    DebuggerModule* pModule = new (interopsafe) DebuggerModule(pRuntimeModule, pDomainFile, pAppDomain);
    _ASSERTE(pModule != NULL); // throws on oom

    TRACE_ALLOC(pModule);

    m_pModules->AddModule(pModule); // throws
    // @dbgtodo  inspection/exceptions - this may leak module in OOM case. LS is not OOM resilient; and we
    // expect to get rid of DebuggerModule anyways.

    LOG((LF_CORDB, LL_INFO1000, "D::ADM df=0x%x -> dm=0x%x\n", pDomainFile, pModule));
    return pModule;
}

//
// TrapAllRuntimeThreads causes every Runtime thread that is executing
// in the EE to trap and send the at safe point event to the RC thread as
// soon as possible. It also sets the EE up so that Runtime threads that
// are outside of the EE will trap when they try to re-enter.
//
// @TODO::
// Neither pDbgLockHolder nor pAppDomain are used.
void Debugger::TrapAllRuntimeThreads()
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        // We acquired the lock b/c we're in a scope between LFES & UFES.
        PRECONDITION(ThreadHoldsLock());

        // This should never be called on a Temporary Helper thread.
        PRECONDITION(IsDbgHelperSpecialThread() ||
                     (g_pEEInterface->GetThread() == NULL) ||
                     !g_pEEInterface->IsPreemptiveGCDisabled());
    }
    CONTRACTL_END;

#if !defined(FEATURE_DBGIPC_TRANSPORT_VM)
    // Only sync if RS requested it.
    if (!m_RSRequestedSync)
    {
        return;
    }
    m_RSRequestedSync = FALSE;
#endif

    // If we're doing shutdown, then don't bother trying to communicate w/ the RS.
    // If we're not the thread doing shutdown, then we may be asynchronously killed by the OS.
    // If we are the thread in shutdown, don't TART b/c that may block and do complicated stuff.
    if (g_fProcessDetach)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::TART: Skipping for shutdown.\n");
        return;
    }


    // Only try to start trapping if we're not already trapping.
    if (m_trappingRuntimeThreads == FALSE)
    {
        bool fSuspended;

        STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::TART: Trapping all Runtime threads.\n");

        // There's no way that we should be stopped and still trying to call this function.
        _ASSERTE(!m_stopped);

        // Mark that we're trapping now.
        m_trappingRuntimeThreads = TRUE;

        // Take the thread store lock.
        assert(ThreadStore::HoldingThreadStore());

        // We start the suspension here, and let the helper thread finish it.
        // If there's no helper thread, then we need to do helper duty.
        {
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
            fSuspended = g_pEEInterface->StartSuspendForDebug(NULL, TRUE);
        }

        // We tell the RC Thread to check for other threads now and then and help them get synchronized. (This
        // is similar to what is done when suspending threads for GC with the HandledJITCase() function.)

        // This does not block.
        // Pinging this will waken the helper thread (or temp H. thread) and tell it to sweep & send
        // the sync complete.
        m_pRCThread->WatchForStragglers();

        // It's possible we may not have a real helper thread.
        // - on startup in dllmain, helper is blocked on DllMain loader lock.
        // - on shutdown, helper has been removed on us.
        // In those cases, we need somebody to send the sync-complete, and handle
        // managed events, and wait for the continue. So we pretend to be the helper thread.
        STRESS_LOG0(LF_CORDB, LL_EVERYTHING, "D::SSCIPCE: Calling IsRCThreadReady()\n");

        // We must check the helper thread status while under the lock.
        _ASSERTE(ThreadHoldsLock());
        // If we failed to suspend, then that means we must have multiple managed threads.
        // That means that our helper is not blocked on starting up, thus we can wait infinite on it.
        // Thus we don't need to do helper duty if the suspend fails.
        bool fShouldDoHelperDuty = !m_pRCThread->IsRCThreadReady() && fSuspended;
        if (fShouldDoHelperDuty && !g_fProcessDetach)
        {
            // In V1.0, we had the assumption that if the helper thread isn't ready yet, then we're in
            // a state that SuspendForDebug will succeed on the first try, and thus we'll
            // never call Sweep when doing helper thread duty.
            _ASSERTE(fSuspended);

            // This call will do a ton of work, it will toggle the lock,
            // and it will block until we receive a continue!
            DoHelperThreadDuty();

            // We will have released the TSL after the call to continue.
        }
        _ASSERTE(ThreadHoldsLock()); // still hold the lock. (though it may have been toggled)
    }
}


//
// ReleaseAllRuntimeThreads releases all Runtime threads that may be
// stopped after trapping and sending the at safe point event.
//
void Debugger::ReleaseAllRuntimeThreads(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        // We acquired the lock b/c we're in a scope between LFES & UFES.
        PRECONDITION(ThreadHoldsLock());

        // Currently, this is only done on a helper thread.
        PRECONDITION(ThisIsHelperThreadWorker());

        // Make sure that we were stopped...
        PRECONDITION(m_trappingRuntimeThreads && m_stopped);
    }
    CONTRACTL_END;

    //<TODO>@todo APPD if we want true isolation, remove this & finish the work</TODO>
    pAppDomain = NULL;

    STRESS_LOG1(LF_CORDB, LL_INFO10000, "D::RART: Releasing all Runtime threads"
        "for AppD 0x%x.\n", pAppDomain);

    // Mark that we're on our way now...
    m_trappingRuntimeThreads = FALSE;
    m_stopped = FALSE;

    // Go ahead and resume the Runtime threads.
    g_pEEInterface->ResumeFromDebug(pAppDomain);
}

// Given a method, get's its EnC version number. 1 if the method is not EnCed.
// Note that MethodDescs are reused between versions so this will give us
// the most recent EnC number.
int Debugger::GetMethodEncNumber(MethodDesc * pMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebuggerJitInfo * dji = GetLatestJitInfoFromMethodDesc(pMethod);
    if (dji == NULL)
    {
        // If there's no DJI, couldn't have been EnCed.
        return 1;
    }
    return (int) dji->m_encVersion;
}


bool Debugger::IsJMCMethod(Module* pModule, mdMethodDef tkMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CORDebuggerAttached());
    }
    CONTRACTL_END;

#ifdef _DEBUG
    Crst crstDbg(CrstIsJMCMethod, CRST_UNSAFE_ANYMODE);
    PRECONDITION(crstDbg.IsSafeToTake());
#endif

    DebuggerMethodInfo *pInfo = GetOrCreateMethodInfo(pModule, tkMethod);

    if (pInfo == NULL)
        return false;

    return pInfo->IsJMCFunction();
}

/******************************************************************************
 * Called by Runtime when on a 1st chance Native Exception.
 * This is likely when we hit a breakpoint / single-step.
 * This is called for all native exceptions (except COM+) on managed threads,
 * regardless of whether the debugger is attached.
 ******************************************************************************/
bool Debugger::FirstChanceNativeException(EXCEPTION_RECORD *exception,
                                          CONTEXT *context,
                                          DWORD code,
                                          Thread *thread)
{

    // @@@
    // Implement DebugInterface
    // Can be called from EE exception code. Or from our M2UHandoffHijackFilter
    // must be on managed thread.

    CONTRACTL
    {
        NOTHROW;

        // No clear GC_triggers semantics here. See DispatchNativeException.
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;

        PRECONDITION(CheckPointer(exception));
        PRECONDITION(CheckPointer(context));
        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;


    // Ignore any notification exceptions sent from code:Debugger.SendRawEvent.
    // This is not a common case, but could happen in some cases described
    // in SendRawEvent. Either way, Left-Side and VM should just ignore these.
    if (IsEventDebuggerNotification(exception, PTR_TO_CORDB_ADDRESS(GetClrModuleBase())))
    {
        return true;
    }

    bool retVal;

    // Don't stop for native debugging anywhere inside our inproc-Filters.
    CantStopHolder hHolder;

    if (!CORDBUnrecoverableError(this))
    {
        retVal = DebuggerController::DispatchNativeException(exception, context,
                                                           code, thread);
    }
    else
    {
        retVal = false;
    }

    return retVal;
}

/******************************************************************************
 *
 ******************************************************************************/
PRD_TYPE Debugger::GetPatchedOpcode(CORDB_ADDRESS_TYPE *ip)
{
    WRAPPER_NO_CONTRACT;

    if (!CORDBUnrecoverableError(this))
    {
        return DebuggerController::GetPatchedOpcode(ip);
    }
    else
    {
        PRD_TYPE mt;
        InitializePRD(&mt);
        return mt;
    }
}

/******************************************************************************
 *
 ******************************************************************************/
BOOL Debugger::CheckGetPatchedOpcode(CORDB_ADDRESS_TYPE *address, /*OUT*/ PRD_TYPE *pOpcode)
{
    WRAPPER_NO_CONTRACT;
    CONSISTENCY_CHECK(CheckPointer(address));
    CONSISTENCY_CHECK(CheckPointer(pOpcode));

    if (CORDebuggerAttached() && !CORDBUnrecoverableError(this))
    {
        return DebuggerController::CheckGetPatchedOpcode(address, pOpcode);
    }
    else
    {
        InitializePRD(pOpcode);
        return FALSE;
    }
}

/******************************************************************************
 *
 ******************************************************************************/
void Debugger::TraceCall(const BYTE *code)
{
    CONTRACTL
    {
        // We're being called right before we call managed code. Can't trigger
        // because there may be unprotected args on the stack.
        MODE_COOPERATIVE;
        GC_NOTRIGGER;

        NOTHROW;
    }
    CONTRACTL_END;


    Thread * pCurThread = g_pEEInterface->GetThread();
    // Ensure we never even think about running managed code on the helper thread.
    _ASSERTE(!ThisIsHelperThreadWorker() || !"You're running managed code on the helper thread");

    // One threat is that our helper thread may be forced to execute a managed DLL main.
    // In that case, it's before the helper thread proc is even executed, so our conventional
    // IsHelperThread() checks are inadequate.
    _ASSERTE((GetCurrentThreadId() != g_pRCThread->m_DbgHelperThreadOSTid) || !"You're running managed code on the helper thread");

    _ASSERTE((g_pEEInterface->GetThreadFilterContext(pCurThread) == NULL) || !"Shouldn't run managed code w/ Filter-Context set");

    if (!CORDBUnrecoverableError(this))
    {
        // There are situations where our callers can't tolerate us throwing.
        EX_TRY
        {
            // Since we have a try catch and the debugger code can deal properly with
            // faults occuring inside DebuggerController::DispatchTraceCall, we can safely
            // establish a FAULT_NOT_FATAL region. This is required since some callers can't
            // tolerate faults.
            FAULT_NOT_FATAL();

            DebuggerController::DispatchTraceCall(pCurThread, code);
        }
        EX_CATCH
        {
            // We're being called for our benefit, not our callers. So if we fail,
            // they don't care.
            // Failure for us means that some steppers may miss their notification
            // for entering managed code.
            LOG((LF_CORDB, LL_INFO10000, "Debugger::TraceCall - inside catch, %p\n", code));
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
}

/******************************************************************************
 * For Just-My-Code (aka Just-User-Code).
 * Invoked from a probe in managed code when we enter a user method and
 * the flag (set by GetJMCFlagAddr) for that method is != 0.
 * pIP - the ip within the method, right after the prolog.
 * sp  - stack pointer (frame pointer on x86) for the managed method we're entering.
 * bsp - backing store pointer for the managed method we're entering
  ******************************************************************************/
void Debugger::OnMethodEnter(void * pIP)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000000, "D::OnMethodEnter(ip=%p)\n", pIP));

    if (!CORDebuggerAttached())
    {
        LOG((LF_CORDB, LL_INFO1000000, "D::OnMethodEnter returning since debugger attached.\n"));
        return;
    }
    FramePointer fp = LEAF_MOST_FRAME;
    DebuggerController::DispatchMethodEnter(pIP, fp);
}
/******************************************************************************
 * GetJMCFlagAddr
 * Provide an address of the flag that the JMC probes use to decide whether
 * or not to call TriggerMethodEnter.
 * Called for each method that we jit.
 * md - method desc for the JMC probe
 * returns an address of a flag that the probe can use.
 ******************************************************************************/
DWORD* Debugger::GetJMCFlagAddr(Module * pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    // This callback will be invoked whenever we jit debuggable code.
    // A debugger may not be attached yet, but we still need someplace
    // to store this dword.
    // Use the EE's module, because it's always around, even if a debugger
    // is attached or not.
    return &(pModule->m_dwDebuggerJMCProbeCount);
}

/******************************************************************************
 * Updates the JMC flag on all the EE modules.
 * We can do this as often as we'd like - though it's a perf hit.
 ******************************************************************************/
void Debugger::UpdateAllModuleJMCFlag(bool fStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000000, "D::UpdateModuleJMCFlag to %d\n", fStatus));

    _ASSERTE(HasDebuggerDataLock());

    // Loop through each module.
    // The module table is lazily allocated. As soon as we set JMC status on any module, that will cause an
    // allocation of the module table. So if the table isn't allocated no module has JMC set,
    // and so there is nothing to update.
    if (m_pModules != NULL)
    {
        HASHFIND f;
        for (DebuggerModule * m = m_pModules->GetFirstModule(&f);
             m != NULL;
             m = m_pModules->GetNextModule(&f))
        {
            // the primary module may get called multiple times, but that's ok.
            UpdateModuleJMCFlag(m->GetRuntimeModule(), fStatus);
        } // end for all modules.
    }
}

/******************************************************************************
 * Updates the JMC flag on the given Primary module
 * We can do this as often as we'd like - though it's a perf hit.
 * If we've only changed methods in a single module, then we can just call this.
 * If we do a more global thing (Such as enable MethodEnter), then that could
 * affect all modules, so we use the UpdateAllModuleJMCFlag helper.
 ******************************************************************************/
void Debugger::UpdateModuleJMCFlag(Module * pRuntimeModule, bool fStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(HasDebuggerDataLock());


    DWORD * pFlag = &(pRuntimeModule->m_dwDebuggerJMCProbeCount);
    _ASSERTE(pFlag != NULL);

    if (pRuntimeModule->HasAnyJMCFunctions())
    {
        // If this is a user-code module, then update the JMC flag
        // the probes look at so that we get MethodEnter callbacks.
        *pFlag = fStatus;

        LOG((LF_CORDB, LL_EVERYTHING, "D::UpdateModuleJMCFlag, module %p is user code\n", pRuntimeModule));
    } else {
        LOG((LF_CORDB, LL_EVERYTHING, "D::UpdateModuleJMCFlag, module %p is not-user code\n", pRuntimeModule));

        // if non-user code, flag should be 0 so that we don't waste
        // cycles in the callbacks.
        _ASSERTE(*pFlag == 0);
    }
}

// This sets the JMC status for the entire module.
// fStatus - default status for whole module
void Debugger::SetModuleDefaultJMCStatus(Module * pRuntimeModule, bool fStatus)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO100000, "DM::SetJMCStatus, status=%d, this=%p\n", fStatus, this));

    // Ensure that all active DMIs have our status.
    // All new DMIs can lookup their status from us.
    // This should also update the module count of active JMC DMI's.
    DebuggerMethodInfoTable * pTable = g_pDebugger->GetMethodInfoTable();

    if (pTable != NULL)
    {
        Debugger::DebuggerDataLockHolder debuggerDataLockHolder(g_pDebugger);
        HASHFIND info;

        for (DebuggerMethodInfo *dmi = pTable->GetFirstMethodInfo(&info);
            dmi != NULL;
            dmi = pTable->GetNextMethodInfo(&info))
        {
            if (dmi->GetRuntimeModule() == pRuntimeModule)
            {
                // This DMI is in this module, so update its status
                dmi->SetJMCStatus(fStatus);
            }
        }
    }

    pRuntimeModule->SetJMCStatus(fStatus);

#ifdef _DEBUG
    // If we're disabling JMC in this module, then we shouldn't
    // have any active JMC functions.
    if (!fStatus)
    {
        _ASSERTE(!pRuntimeModule->HasAnyJMCFunctions());
    }
#endif
}

/******************************************************************************
 * Called by GC to determine if it's safe to do a GC.
 ******************************************************************************/
bool Debugger::ThreadsAtUnsafePlaces(void)
{
    LIMITED_METHOD_CONTRACT;

    // If we're in shutdown mode, then all other threads are parked.
    // Even if they claim to be at unsafe regions, they're still safe to do a GC. They won't touch
    // their stacks.
    if (m_fShutdownMode)
    {
        if (m_threadsAtUnsafePlaces > 0)
        {
            STRESS_LOG1(LF_CORDB, LL_INFO10000, "D::TAUP: Claiming safety in shutdown mode.%d\n", m_threadsAtUnsafePlaces);
        }
        return false;
    }


    return (m_threadsAtUnsafePlaces != 0);
}

void Debugger::SuspendForGarbageCollectionStarted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    this->m_isGarbageCollectionEventsEnabledLatch = this->m_isGarbageCollectionEventsEnabled;
    this->m_willBlockOnGarbageCollectionEvent = this->m_isGarbageCollectionEventsEnabledLatch;
}

void Debugger::SuspendForGarbageCollectionCompleted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    this->m_isSuspendedForGarbageCollection = TRUE;

    if (!CORDebuggerAttached() || !this->m_isGarbageCollectionEventsEnabledLatch)
    {
        return;
    }
    this->m_isBlockedOnGarbageCollectionEvent = TRUE;

    Thread* pThread = GetThread();

    if (CORDBUnrecoverableError(this))
        return;

    {
        Debugger::DebuggerLockHolder dbgLockHolder(this);

        DebuggerIPCEvent* ipce1 = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce1,
            DB_IPCE_BEFORE_GARBAGE_COLLECTION,
            pThread,
            pThread->GetDomain());

        m_pRCThread->SendIPCEvent();
        this->SuspendComplete(true);
    }

    WaitForSingleObject(this->GetGarbageCollectionBlockerEvent(), INFINITE);
    ResetEvent(this->GetGarbageCollectionBlockerEvent());
}

void Debugger::ResumeForGarbageCollectionStarted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    this->m_isSuspendedForGarbageCollection = FALSE;

    if (!CORDebuggerAttached() || !this->m_isGarbageCollectionEventsEnabledLatch)
    {
        return;
    }

    Thread* pThread = GetThread();

    if (CORDBUnrecoverableError(this))
        return;

    {
        Debugger::DebuggerLockHolder dbgLockHolder(this);

        DebuggerIPCEvent* ipce1 = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce1,
            DB_IPCE_AFTER_GARBAGE_COLLECTION,
            pThread,
            pThread->GetDomain());

        m_pRCThread->SendIPCEvent();
        this->SuspendComplete(true);
    }

    WaitForSingleObject(this->GetGarbageCollectionBlockerEvent(), INFINITE);
    ResetEvent(this->GetGarbageCollectionBlockerEvent());
    this->m_isBlockedOnGarbageCollectionEvent = FALSE;
    this->m_willBlockOnGarbageCollectionEvent = FALSE;
}

#ifdef FEATURE_DATABREAKPOINT
void Debugger::SendDataBreakpoint(Thread *thread, CONTEXT *context,
    DebuggerDataBreakpoint *breakpoint)
{
    CONTRACTL
    {
        NOTHROW;
    GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

#ifdef _DEBUG
    static BOOL shouldBreak = -1;
    if (shouldBreak == -1)
        shouldBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgBreakOnSendBreakpoint);

    if (shouldBreak > 0) {
        _ASSERTE(!"DbgBreakOnSendBreakpoint");
    }
#endif

    LOG((LF_CORDB, LL_INFO10000, "D::SDB: breakpoint BP:0x%x\n", breakpoint));

    _ASSERTE((g_pEEInterface->GetThread() &&
        !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) ||
        g_fInControlC);

    _ASSERTE(ThreadHoldsLock());

    // Send a breakpoint event to the Right Side
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    memcpy(&(ipce->DataBreakpointData.context), context, sizeof(CONTEXT));
    InitIPCEvent(ipce,
        DB_IPCE_DATA_BREAKPOINT,
        thread,
        thread->GetDomain());
    //_ASSERTE(breakpoint->m_pAppDomain == ipce->vmAppDomain.GetRawPtr());

    m_pRCThread->SendIPCEvent();
}
#endif

//
// SendBreakpoint is called by Runtime threads to send that they've
// hit a breakpoint to the Right Side.
//
void Debugger::SendBreakpoint(Thread *thread, CONTEXT *context,
                              DebuggerBreakpoint *breakpoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

#ifdef _DEBUG
    static BOOL shouldBreak = -1;
    if (shouldBreak == -1)
        shouldBreak = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_DbgBreakOnSendBreakpoint);

    if (shouldBreak > 0) {
        _ASSERTE(!"DbgBreakOnSendBreakpoint");
    }
#endif

    LOG((LF_CORDB, LL_INFO10000, "D::SB: breakpoint BP:0x%x\n", breakpoint));

    _ASSERTE((g_pEEInterface->GetThread() &&
             !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) ||
             g_fInControlC);

    _ASSERTE(ThreadHoldsLock());

    // Send a breakpoint event to the Right Side
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_BREAKPOINT,
                 thread,
                 thread->GetDomain());
    ipce->BreakpointData.breakpointToken.Set(breakpoint);
    _ASSERTE( breakpoint->m_pAppDomain == ipce->vmAppDomain.GetRawPtr());

    m_pRCThread->SendIPCEvent();
}


//---------------------------------------------------------------------------------------
// Send a user breakpoint event for this thread and sycnhronize the process.
//
// Arguments:
//     pThread - non-null thread to send user breakpoint event for.
//
// Notes:
//     Can't assume that a debugger is attached (since it may detach before we get the lock).
void Debugger::SendUserBreakpointAndSynchronize(Thread * pThread)
{
    AtSafePlaceHolder unsafePlaceHolder(pThread);

    SENDIPCEVENT_BEGIN(this, pThread);

    // Actually send the event
    if (CORDebuggerAttached())
    {
        SendRawUserBreakpoint(pThread);
        TrapAllRuntimeThreads();
    }

    SENDIPCEVENT_END;
}

//---------------------------------------------------------------------------------------
//
// SendRawUserBreakpoint is called by Runtime threads to send that
// they've hit a user breakpoint to the Right Side. This is the event
// send only part, since it can be called from a few different places.
//
// Arguments:
//    pThread - [in] managed thread where user break point takes place.
//        mus be curernt thread.
//
void Debugger::SendRawUserBreakpoint(Thread * pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;

        PRECONDITION(pThread == GetThreadNULLOk());

        PRECONDITION(ThreadHoldsLock());

        // Debugger must have been attached to get us to this point.
        // We hold the Debugger-lock, so debugger could not have detached from
        // underneath us either.
        PRECONDITION(CORDebuggerAttached());
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO10000, "D::SRUB: user breakpoint\n"));



    // Send a breakpoint event to the Right Side
    DebuggerIPCEvent* pEvent = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(pEvent,
                 DB_IPCE_USER_BREAKPOINT,
                 pThread,
                 pThread->GetDomain());

    m_pRCThread->SendIPCEvent();
}

//
// SendInterceptExceptionComplete is called by Runtime threads to send that
// they've completed intercepting an exception to the Right Side. This is the event
// send only part, since it can be called from a few different places.
//
void Debugger::SendInterceptExceptionComplete(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO10000, "D::SIEC: breakpoint\n"));

    _ASSERTE(!g_pEEInterface->IsPreemptiveGCDisabled());
    _ASSERTE(ThreadHoldsLock());

    // Send a breakpoint event to the Right Side
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_INTERCEPT_EXCEPTION_COMPLETE,
                 thread,
                 thread->GetDomain());

    m_pRCThread->SendIPCEvent();
}



//
// SendStep is called by Runtime threads to send that they've
// completed a step to the Right Side.
//
void Debugger::SendStep(Thread *thread, CONTEXT *context,
                        DebuggerStepper *stepper,
                        CorDebugStepReason reason)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO10000, "D::SS: step:token:0x%p reason:0x%x\n",
        stepper, reason));

    _ASSERTE((g_pEEInterface->GetThread() &&
             !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) ||
             g_fInControlC);

    _ASSERTE(ThreadHoldsLock());

    // Send a step event to the Right Side
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_STEP_COMPLETE,
                 thread,
                 thread->GetDomain());
    ipce->StepData.stepperToken.Set(stepper);
    ipce->StepData.reason = reason;
    m_pRCThread->SendIPCEvent();
}

//-------------------------------------------------------------------------------------------------
// Send an EnC remap opportunity and block until it is continued.
//
// dji - current method information
// currentIP - IL offset within that method
// resumeIP - address of a SIZE_T that the RS will write to cross-process if they take the
//  remap opportunity. *resumeIP is untouched if the RS does not remap.
//-------------------------------------------------------------------------------------------------
void Debugger::LockAndSendEnCRemapEvent(DebuggerJitInfo * dji, SIZE_T currentIP, SIZE_T *resumeIP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS; // From SendIPCEvent
        PRECONDITION(dji != NULL);
    }
    CONTRACTL_END;


    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRE:\n"));

    if (CORDBUnrecoverableError(this))
        return;

    MethodDesc * pFD = dji->m_nativeCodeVersion.GetMethodDesc();

    // Note that the debugger lock is reentrant, so we may or may not hold it already.
    Thread *thread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, thread);

    // Send an EnC remap event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_ENC_REMAP,
                 thread,
                 thread->GetDomain());

    ipce->EnCRemap.currentVersionNumber = dji->m_encVersion;
    ipce->EnCRemap.resumeVersionNumber = dji->m_methodInfo->GetCurrentEnCVersion();;
    ipce->EnCRemap.currentILOffset = currentIP;
    ipce->EnCRemap.resumeILOffset = resumeIP;
    ipce->EnCRemap.funcMetadataToken = pFD->GetMemberDef();

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRE: token 0x%x, from version %d to %d\n",
    ipce->EnCRemap.funcMetadataToken, ipce->EnCRemap.currentVersionNumber, ipce->EnCRemap.resumeVersionNumber));

    Module *pRuntimeModule = pFD->GetModule();

    DebuggerModule * pDModule = LookupOrCreateModule(pRuntimeModule, thread->GetDomain());
    ipce->EnCRemap.vmDomainFile.SetRawPtr((pDModule ? pDModule->GetDomainFile() : NULL));

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRE: %s::%s "
        "dmod:0x%x, methodDef:0x%x \n",
        pFD->m_pszDebugClassName, pFD->m_pszDebugMethodName,
        pDModule,
        ipce->EnCRemap.funcMetadataToken));

    // IPC event is now initialized, so we can send it over.
    SendSimpleIPCEventAndBlock();

    // This will block on the continue
    SENDIPCEVENT_END;

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRE: done\n"));

}

// Send the RemapComplete event and block until the debugger Continues
// pFD - specifies the method in which we've remapped into
void Debugger::LockAndSendEnCRemapCompleteEvent(MethodDesc *pFD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRE:\n"));

    if (CORDBUnrecoverableError(this))
        return;

    Thread *thread = g_pEEInterface->GetThread();
    // Note that the debugger lock is reentrant, so we may or may not hold it already.
    SENDIPCEVENT_BEGIN(this, thread);

    EX_TRY
    {
        // Ensure the DJI for the latest version of this method has been pre-created.
        // It's not clear whether this is necessary or not, but it shouldn't hurt since
        // we're going to need to create it anyway since we'll be debugging inside it.
        DebuggerJitInfo *dji = g_pDebugger->GetLatestJitInfoFromMethodDesc(pFD);
        (void)dji; //prevent "unused variable" error from GCC
        _ASSERTE( dji != NULL );
    }
    EX_CATCH
    {
        // GetLatestJitInfo could throw on OOM, but the debugger isn't resiliant to OOM.
        // I'm not aware of any other legitimate reason why it may throw, so we'll ASSERT
        // if it fails.
        _ASSERTE(!"Unexpected exception from Debugger::GetLatestJitInfoFromMethodDesc on EnC remap complete");
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    // Send an EnC remap complete event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_ENC_REMAP_COMPLETE,
                 thread,
                 thread->GetDomain());


    ipce->EnCRemapComplete.funcMetadataToken = pFD->GetMemberDef();

    Module *pRuntimeModule = pFD->GetModule();

    DebuggerModule * pDModule = LookupOrCreateModule(pRuntimeModule, thread->GetDomain());
    ipce->EnCRemapComplete.vmDomainFile.SetRawPtr((pDModule ? pDModule->GetDomainFile() : NULL));


    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRC: %s::%s "
        "dmod:0x%x, methodDef:0x%x \n",
        pFD->m_pszDebugClassName, pFD->m_pszDebugMethodName,
        pDModule,
        ipce->EnCRemap.funcMetadataToken));

    // IPC event is now initialized, so we can send it over.
    SendSimpleIPCEventAndBlock();

    // This will block on the continue
    SENDIPCEVENT_END;

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCRC: done\n"));

}
//
// This function sends a notification to the RS about a specific update that has occurred as part of
// applying an Edit and Continue.  We send notification only for function add/update and field add.
// At this point, the EE is already stopped for handling an EnC ApplyChanges operation, so no need
// to take locks etc.
//
void Debugger::SendEnCUpdateEvent(DebuggerIPCEventType eventType,
                                  Module * pModule,
                                  mdToken memberToken,
                                  mdTypeDef classToken,
                                  SIZE_T enCVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCUFE:\n"));

    _ASSERTE(eventType == DB_IPCE_ENC_UPDATE_FUNCTION ||
                      eventType == DB_IPCE_ENC_ADD_FUNCTION ||
                      eventType== DB_IPCE_ENC_ADD_FIELD);

    if (CORDBUnrecoverableError(this))
        return;

    // Send an EnC UpdateFunction event to the Right Side.
    DebuggerIPCEvent* event = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(event,
                 eventType,
                 NULL,
                 NULL);

    event->EnCUpdate.newVersionNumber = enCVersion;
    event->EnCUpdate.memberMetadataToken = memberToken;
    // we have to pass the class token across to the RS because we cannot look it up over
    // there based on the added field/method because the metadata on the RS will not yet
    // have the changes applied, so the token will not exist in its metadata and we have
    // no way to find it.
    event->EnCUpdate.classMetadataToken = classToken;

    _ASSERTE(pModule);
    // we don't support shared assemblies, so must have an appdomain
    _ASSERTE(pModule->GetDomain()->IsAppDomain());

    DebuggerModule * pDModule = LookupOrCreateModule(pModule, pModule->GetDomain()->AsAppDomain());
    event->EnCUpdate.vmDomainFile.SetRawPtr((pDModule ? pDModule->GetDomainFile() : NULL));

    m_pRCThread->SendIPCEvent();

    LOG((LF_CORDB, LL_INFO10000, "D::LASEnCUE: done\n"));

}


//
// Send a BreakpointSetError event to the Right Side if the given patch is for a breakpoint. Note: we don't care if this
// fails, there is nothing we can do about it anyway, and the breakpoint just wont hit.
//
void Debugger::LockAndSendBreakpointSetError(PATCH_UNORDERED_ARRAY * listUnbindablePatches)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(listUnbindablePatches != NULL);

    if (CORDBUnrecoverableError(this))
        return;


    ULONG count = listUnbindablePatches->Count();
    _ASSERTE(count > 0); // must send at least 1 event.


    Thread *thread = g_pEEInterface->GetThread();
    // Note that the debugger lock is reentrant, so we may or may not hold it already.
    SENDIPCEVENT_BEGIN(this, thread);

    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

    for(ULONG i =  0; i < count; i++)
    {
        DebuggerControllerPatch *patch = listUnbindablePatches->Table()[i];
        _ASSERTE(patch != NULL);

        // Only do this for breakpoint controllers
        DebuggerController *controller = patch->controller;

        if (controller->GetDCType() != DEBUGGER_CONTROLLER_BREAKPOINT)
        {
            continue;
        }

        LOG((LF_CORDB, LL_INFO10000, "D::LASBSE:\n"));

        // Send a breakpoint set error event to the Right Side.
        InitIPCEvent(ipce, DB_IPCE_BREAKPOINT_SET_ERROR, thread, thread->GetDomain());

        ipce->BreakpointSetErrorData.breakpointToken.Set(static_cast<DebuggerBreakpoint*> (controller));

        // IPC event is now initialized, so we can send it over.
        m_pRCThread->SendIPCEvent();
    }

    // Stop all Runtime threads
    TrapAllRuntimeThreads();

    // This will block on the continue
    SENDIPCEVENT_END;

}

//
// Called from the controller to lock the debugger for event
// sending. This is called before controller events are sent, like
// breakpoint, step complete, and thread started.
//
// Note that it's possible that the debugger detached (and destroyed our IPC
// events) while we're waiting for our turn.
// So Callers should check for that case.
void Debugger::LockForEventSending(DebuggerLockHolder *dbgLockHolder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // @todo - Force our parents to bump up the stop-count. That way they can
    // guarantee it's balanced.
    IncCantStopCount();
    _ASSERTE(IsInCantStopRegion());

    // What we need is for caller to get the debugger lock
    if (dbgLockHolder != NULL)
    {
        dbgLockHolder->Acquire();
    }

#ifdef _DEBUG
     // Track our TID. We're not re-entrant.
    //_ASSERTE(m_tidLockedForEventSending == 0);
    m_tidLockedForEventSending = GetCurrentThreadId();
#endif

}

//
// Called from the controller to unlock the debugger from event
// sending. This is called after controller events are sent, like
// breakpoint, step complete, and thread started.
//
void Debugger::UnlockFromEventSending(DebuggerLockHolder *dbgLockHolder)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

#ifdef _DEBUG
    //_ASSERTE(m_tidLockedForEventSending == GetCurrentThreadId());
    m_tidLockedForEventSending = 0;
#endif
    if (dbgLockHolder != NULL)
    {
        dbgLockHolder->Release();
    }
    // @todo - Force our parents to bump up the stop-count. That way they can
    // guarantee it's balanced.
    _ASSERTE(IsInCantStopRegion());
    DecCantStopCount();
}


//
// Called from the controller after all events have been sent for a
// thread to sync the process.
//
void Debugger::SyncAllThreads(DebuggerLockHolder *dbgLockHolder)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::SAT: sync all threads.\n");

    Thread *pThread = g_pEEInterface->GetThread();
    (void)pThread; //prevent "unused variable" error from GCC
    _ASSERTE((pThread &&
             !pThread->m_fPreemptiveGCDisabled) ||
              g_fInControlC);

    _ASSERTE(ThreadHoldsLock());

    // Stop all Runtime threads
    TrapAllRuntimeThreads();
}

//---------------------------------------------------------------------------------------
// Launch a debugger and then trigger a breakpoint (either managed or native)
//
// Arguments:
//    useManagedBPForManagedAttach - TRUE if we should stop with a managed breakpoint
//                                   when managed attached, FALSE if we should always
//                                   stop with a native breakpoint
//    pThread - the managed thread that attempts to launch the registered debugger
//    pExceptionInfo - the unhandled exception info
//    explicitUserRequest - TRUE if this attach is caused by a call to the Debugger.Launch() API.
//
// Returns:
//    S_OK on success. Else failure.
//
// Notes:
//    This function doesn't try to stop the launched native debugger by calling DebugBreak().
//    It sends a breakpoint event only for managed debuggers.
//
HRESULT Debugger::LaunchDebuggerForUser(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo,
                                        BOOL useManagedBPForManagedAttach, BOOL explicitUserRequest)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_CORDB, LL_INFO10000, "D::LDFU: Attaching Debugger.\n"));

    //
    // Initiate a jit attach
    //
    JitAttach(pThread, pExceptionInfo, useManagedBPForManagedAttach, explicitUserRequest);

    if (useManagedBPForManagedAttach)
    {
        if(CORDebuggerAttached() && (g_pEEInterface->GetThread() != NULL))
        {
            //
            // Send a managed-breakpoint.
            //
            SendUserBreakpointAndSynchronize(g_pEEInterface->GetThread());
        }
        else if (!CORDebuggerAttached() && IsDebuggerPresent())
        {
            //
            // If the registered debugger is not a managed debugger, send a native breakpoint
            //
            DebugBreak();
        }
    }
    else if(!useManagedBPForManagedAttach)
    {
        //
        // Send a native breakpoint
        //
        DebugBreak();
    }

    if (!IsDebuggerPresent())
    {
        LOG((LF_CORDB, LL_ERROR, "D::LDFU: Failed to launch the debugger.\n"));
    }

    return S_OK;
}


// The following JDI structures will be passed to a debugger on Vista.  Because we do not know when the debugger
// will be done looking at them, and there is at most one debugger attaching to the process, we always set them
// once and leave them set without the risk of clobbering something we care about.
JIT_DEBUG_INFO   Debugger::s_DebuggerLaunchJitInfo = {0};
EXCEPTION_RECORD Debugger::s_DebuggerLaunchJitInfoExceptionRecord = {0};
CONTEXT          Debugger::s_DebuggerLaunchJitInfoContext = {0};

//----------------------------------------------------------------------------
//
// InitDebuggerLaunchJitInfo - initialize JDI structure on Vista
//
// Arguments:
//    pThread - the managed thread with the unhandled excpetion
//    pExceptionInfo - unhandled exception info
//
// Return Value:
//    None
//
//----------------------------------------------------------------------------
void Debugger::InitDebuggerLaunchJitInfo(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((pExceptionInfo != NULL) &&
             (pExceptionInfo->ContextRecord != NULL) &&
             (pExceptionInfo->ExceptionRecord != NULL));

    if ((pExceptionInfo == NULL) || (pExceptionInfo->ContextRecord == NULL) || (pExceptionInfo->ExceptionRecord == NULL))
    {
        return;
    }

    s_DebuggerLaunchJitInfoExceptionRecord = *pExceptionInfo->ExceptionRecord;
    s_DebuggerLaunchJitInfoContext = *pExceptionInfo->ContextRecord;

    s_DebuggerLaunchJitInfo.dwSize = sizeof(s_DebuggerLaunchJitInfo);
    s_DebuggerLaunchJitInfo.dwThreadID = pThread == NULL ? GetCurrentThreadId() : pThread->GetOSThreadId();
    s_DebuggerLaunchJitInfo.lpExceptionRecord = reinterpret_cast<ULONG64>(&s_DebuggerLaunchJitInfoExceptionRecord);
    s_DebuggerLaunchJitInfo.lpContextRecord = reinterpret_cast<ULONG64>(&s_DebuggerLaunchJitInfoContext);
    s_DebuggerLaunchJitInfo.lpExceptionAddress = s_DebuggerLaunchJitInfoExceptionRecord.ExceptionAddress != NULL ?
        reinterpret_cast<ULONG64>(s_DebuggerLaunchJitInfoExceptionRecord.ExceptionAddress) :
        reinterpret_cast<ULONG64>(reinterpret_cast<PVOID>(GetIP(pExceptionInfo->ContextRecord)));

#if defined(TARGET_X86)
    s_DebuggerLaunchJitInfo.dwProcessorArchitecture = PROCESSOR_ARCHITECTURE_INTEL;
#elif defined(TARGET_AMD64)
    s_DebuggerLaunchJitInfo.dwProcessorArchitecture = PROCESSOR_ARCHITECTURE_AMD64;
#elif defined(TARGET_ARM)
    s_DebuggerLaunchJitInfo.dwProcessorArchitecture = PROCESSOR_ARCHITECTURE_ARM;
#elif defined(TARGET_ARM64)
    s_DebuggerLaunchJitInfo.dwProcessorArchitecture = PROCESSOR_ARCHITECTURE_ARM64;
#else
#error Unknown processor.
#endif
}


//----------------------------------------------------------------------------
//
// GetDebuggerLaunchJitInfo - retrieve the initialized JDI structure on Vista
//
// Arguments:
//    None
//
// Return Value:
//    JIT_DEBUG_INFO * - pointer to JDI structure
//
//----------------------------------------------------------------------------
JIT_DEBUG_INFO * Debugger::GetDebuggerLaunchJitInfo(void)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE((s_DebuggerLaunchJitInfo.lpExceptionAddress != NULL) &&
             (s_DebuggerLaunchJitInfo.lpExceptionRecord != NULL) &&
             (s_DebuggerLaunchJitInfo.lpContextRecord != NULL) &&
             (((EXCEPTION_RECORD *)(s_DebuggerLaunchJitInfo.lpExceptionRecord))->ExceptionAddress != NULL));

    return &s_DebuggerLaunchJitInfo;
}
#endif // !DACCESS_COMPILE


// This function checks the registry for the debug launch setting upon encountering an exception or breakpoint.
DebuggerLaunchSetting Debugger::GetDbgJITDebugLaunchSetting()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#if TARGET_UNIX
    DebuggerLaunchSetting setting = DLS_ATTACH_DEBUGGER;
#else
    BOOL bAuto = FALSE;

    DebuggerLaunchSetting setting = DLS_ASK_USER;

    DWORD cchDbgFormat = MAX_LONGPATH;
    INDEBUG(DWORD cchOldDbgFormat = cchDbgFormat);

#if defined(DACCESS_COMPILE)
    WCHAR * wszDbgFormat = new (nothrow) WCHAR[cchDbgFormat];
#else
    WCHAR * wszDbgFormat = new (interopsafe, nothrow) WCHAR[cchDbgFormat];
#endif // DACCESS_COMPILE

    if (wszDbgFormat == NULL)
    {
        return setting;
    }

    HRESULT hr = GetDebuggerSettingInfoWorker(wszDbgFormat, &cchDbgFormat, &bAuto);
    while (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        _ASSERTE(cchDbgFormat > cchOldDbgFormat);
        INDEBUG(cchOldDbgFormat = cchDbgFormat);

#if defined(DACCESS_COMPILE)
        delete [] wszDbgFormat;
        wszDbgFormat = new (nothrow) WCHAR[cchDbgFormat];
#else
        DeleteInteropSafe(wszDbgFormat);
        wszDbgFormat = new (interopsafe, nothrow) WCHAR[cchDbgFormat];
#endif // DACCESS_COMPILE

        if (wszDbgFormat == NULL)
        {
            return setting;
        }

        hr = GetDebuggerSettingInfoWorker(wszDbgFormat, &cchDbgFormat, &bAuto);
    }

#if defined(DACCESS_COMPILE)
    delete [] wszDbgFormat;
#else
    DeleteInteropSafe(wszDbgFormat);
#endif // DACCESS_COMPILE

    if (SUCCEEDED(hr) && bAuto)
    {
        setting = DLS_ATTACH_DEBUGGER;
    }
#endif // TARGET_UNIX

    return setting;
}

// Returns a bitfield reflecting the managed debugging state at the time of
// the jit attach.
CLR_DEBUGGING_PROCESS_FLAGS Debugger::GetAttachStateFlags()
{
    LIMITED_METHOD_DAC_CONTRACT;
    ULONG flags = CLRJitAttachState;
    return (CLR_DEBUGGING_PROCESS_FLAGS)flags;
}

#ifndef DACCESS_COMPILE
//-----------------------------------------------------------------------------
// Get the full launch string for a jit debugger.
//
// If a jit-debugger is registed, then writes string into pStrArgsBuf and
//   return true.
//
// If no jit-debugger is registered, then return false.
//
// Throws on error (like OOM).
//-----------------------------------------------------------------------------
bool Debugger::GetCompleteDebuggerLaunchString(SString * pStrArgsBuf)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    DWORD pid = GetCurrentProcessId();

    SString ssDebuggerString;
    GetDebuggerSettingInfo(ssDebuggerString, NULL);

    if (ssDebuggerString.IsEmpty())
    {
        // No jit-debugger available. Don't make one up.
        return false;
    }

    // There is no security concern to expect that the debug string we retrieve from HKLM follows a certain
    // format because changing HKLM keys requires admin priviledge.  Padding with zeros is not a security mitigation,
    // but rather a forward looking compability measure.  If future verions of Windows introduces more parameters for
    // JIT debugger launch, it is preferrable to pass zeros than other random values for those unsupported parameters.
    pStrArgsBuf->Printf(ssDebuggerString, pid, GetUnmanagedAttachEvent(), GetDebuggerLaunchJitInfo(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    return true;
#else // !TARGET_UNIX
    return false;
#endif // !TARGET_UNIX
}

// Proxy code for EDA
struct EnsureDebuggerAttachedParams
{
    Debugger *                  m_pThis;
    HRESULT                     m_retval;
    PROCESS_INFORMATION *       m_pProcessInfo;
    EnsureDebuggerAttachedParams() :
        m_pThis(NULL), m_retval(E_FAIL), m_pProcessInfo(NULL) {LIMITED_METHOD_CONTRACT; }
};

// This is called by the helper thread
void EDAHelperStub(EnsureDebuggerAttachedParams * p)
{
    WRAPPER_NO_CONTRACT;

    p->m_retval = p->m_pThis->EDAHelper(p->m_pProcessInfo);
}

// This gets called just like the normal version, but it sends the call over to the helper thread
HRESULT Debugger::EDAHelperProxy(PROCESS_INFORMATION * pProcessInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    _ASSERTE(!ThisIsHelperThreadWorker());
    _ASSERTE(ThreadHoldsLock());

    HRESULT hr = LazyInitWrapper();
    if (FAILED(hr))
    {
        // We already stress logged this case.
        return hr;
    }


    if (!IsGuardPageGone())
    {
        return EDAHelper(pProcessInfo);
    }

    EnsureDebuggerAttachedParams p;
    p.m_pThis = this;
    p.m_pProcessInfo = pProcessInfo;

    LOG((LF_CORDB, LL_INFO1000000, "D::EDAHelperProxy\n"));
    m_pRCThread->DoFavor((FAVORCALLBACK) EDAHelperStub, &p);
    LOG((LF_CORDB, LL_INFO1000000, "D::EDAHelperProxy return\n"));

    return p.m_retval;
}

//   E_ABORT - if the attach was declined
//   S_OK    - Jit-attach successfully started
HRESULT Debugger::EDAHelper(PROCESS_INFORMATION *pProcessInfo)
{
    CONTRACTL
    {
        NOTHROW;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        PRECONDITION(ThisMaybeHelperThread()); // on helper if stackoverflow.
    }
    CONTRACTL_END;

#ifndef TARGET_UNIX
    LOG((LF_CORDB, LL_INFO10000, "D::EDA: thread 0x%x is launching the debugger.\n", GetCurrentThreadId()));

    _ASSERTE(HasLazyData());

    // Another potential hang. This may get run on the helper if we have a stack overflow.
    // Hopefully the odds of 1 thread hitting a stack overflow while another is stuck holding the heap
    // lock is very small.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

    BOOL fCreateSucceeded = FALSE;

    StackSString strDbgCommand;
    const WCHAR * wszDbgCommand = NULL;
    SString strCurrentDir;
    const WCHAR * wszCurrentDir = NULL;

    EX_TRY
    {

        // Get the debugger to launch.  The returned string is via the strDbgCommand out param. Throws on error.
        bool fHasDebugger = GetCompleteDebuggerLaunchString(&strDbgCommand);
        if (fHasDebugger)
        {
            wszDbgCommand = strDbgCommand.GetUnicode();
            _ASSERTE(wszDbgCommand != NULL); // would have thrown on oom.

            LOG((LF_CORDB, LL_INFO10000, "D::EDA: launching with command [%S]\n", wszDbgCommand));

            ClrGetCurrentDirectory(strCurrentDir);
            wszCurrentDir = strCurrentDir.GetUnicode();
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    STARTUPINFOW startupInfo = {0};
    startupInfo.cb = sizeof(STARTUPINFOW);

    DWORD errCreate = 0;

    if (wszDbgCommand != NULL)
    {
        // Create the debugger process
        // When we are launching an debugger, we need to let the child process inherit our handles.
        // This is necessary for the debugger to signal us that the attach is complete.
        fCreateSucceeded = WszCreateProcess(NULL, const_cast<WCHAR*> (wszDbgCommand),
                               NULL, NULL,
                               TRUE,
                               CREATE_NEW_CONSOLE,
                               NULL, wszCurrentDir,
                               &startupInfo,
                               pProcessInfo);
        errCreate = GetLastError();
    }

    if (!fCreateSucceeded)
    {
        LOG((LF_CORDB, LL_INFO10000, "D::EDA: debugger did not launch successfully.\n"));
        return E_ABORT;
    }

    LOG((LF_CORDB, LL_INFO10000, "D::EDA: debugger launched successfully.\n"));
    return S_OK;
#else // !TARGET_UNIX
    return E_ABORT;
#endif // !TARGET_UNIX
}

// ---------------------------------------------------------------------------------------------------------------------
// This function decides who wins the race for any jit attach and marks the appropriate state that a jit
// attach is in progress.
//
// Arguments
//  willSendManagedEvent - indicates whether or not we plan to send a managed debug event after the jit attach
//  explicitUserRequest - TRUE if this attach is caused by a call to the Debugger.Launch() API.
//
// Returns
//    TRUE - if some other thread already has jit attach in progress -> this thread should block until that is complete
//    FALSE - this is the first thread to jit attach -> this thread should launch the debugger
//
//
BOOL Debugger::PreJitAttach(BOOL willSendManagedEvent, BOOL willLaunchDebugger, BOOL explicitUserRequest)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(!ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    LOG( (LF_CORDB, LL_INFO10000, "D::PreJA: Entering\n") );

    // Multiple threads may be calling this, so need to take the lock.
    if(!m_jitAttachInProgress)
    {
        // TODO:  This is a known deadlock!  Debugger::PreJitAttach is called during WatsonLastChance.
        //        If the event (exception/crash) happens while this thread is holding the ThreadStore
        //        lock, we may deadlock if another thread holds the DebuggerMutex and is waiting on
        //        the ThreadStore lock.  The DebuggerMutex has to be broken into two smaller locks
        //        so that you can take that lock here when holding the ThreadStore lock.
        DebuggerLockHolder dbgLockHolder(this);

        if (!m_jitAttachInProgress)
        {
            m_jitAttachInProgress = TRUE;
            m_launchingDebugger = willLaunchDebugger;
            CLRJitAttachState = (willSendManagedEvent ? CLR_DEBUGGING_MANAGED_EVENT_PENDING : 0) | (explicitUserRequest ? CLR_DEBUGGING_MANAGED_EVENT_DEBUGGER_LAUNCH : 0);
            ResetEvent(GetUnmanagedAttachEvent());
            ResetEvent(GetAttachEvent());
            LOG( (LF_CORDB, LL_INFO10000, "D::PreJA: Leaving - first thread\n") );
            return TRUE;
        }
    }

    LOG( (LF_CORDB, LL_INFO10000, "D::PreJA: Leaving - following thread\n") );
    return FALSE;
}

//---------------------------------------------------------------------------------------------------------------------
// This function gets the jit debugger launched and waits for the native attach to complete
// Make sure you called PreJitAttach and it returned TRUE before you call this
//
// Arguments:
//    pThread - the managed thread with the unhandled excpetion
//    pExceptionInfo - the unhandled exception info
//
// Returns:
//   S_OK if the debugger was launched successfully and a failing HRESULT otherwise
//
HRESULT Debugger::LaunchJitDebuggerAndNativeAttach(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    // You need to have called PreJitAttach first to determine which thread gets to launch the debugger
    _ASSERTE(m_jitAttachInProgress);

    LOG( (LF_CORDB, LL_INFO10000, "D::LJDANA: Entering\n") );
    PROCESS_INFORMATION processInfo = {0};
    DebuggerLockHolder dbgLockHolder(this);

    // <TODO>
    // If the JIT debugger failed to launch or if there is no JIT debugger, EDAHelperProxy will
    // switch to preemptive GC mode to display a dialog to the user indicating the JIT debugger
    // was unavailable. There are some rare cases where this could cause a deadlock with the
    // debugger lock; however these are rare enough that fixing this doesn't meet the bar for
    // Whidbey at this point. We might want to revisit this later however.
    // </TODO>
    CONTRACT_VIOLATION(GCViolation);

    {
        LOG((LF_CORDB, LL_INFO1000, "D::EDA: Initialize JDI.\n"));

        EXCEPTION_POINTERS exceptionPointer;
        EXCEPTION_RECORD   exceptionRecord;
        CONTEXT context;

        if (pExceptionInfo == NULL)
        {
            ZeroMemory(&exceptionPointer, sizeof(exceptionPointer));
            ZeroMemory(&exceptionRecord, sizeof(exceptionRecord));
            ZeroMemory(&context, sizeof(context));

            context.ContextFlags = CONTEXT_CONTROL;
            ClrCaptureContext(&context);

            exceptionRecord.ExceptionAddress = reinterpret_cast<PVOID>(GetIP(&context));
            exceptionPointer.ContextRecord   = &context;
            exceptionPointer.ExceptionRecord = &exceptionRecord;

            pExceptionInfo = &exceptionPointer;
        }

        InitDebuggerLaunchJitInfo(pThread, pExceptionInfo);
    }

    // This will make the CreateProcess call to create the debugger process.
    // We then expect that the debugger process will turn around and attach to us.
    HRESULT hr = EDAHelperProxy(&processInfo);
    if(FAILED(hr))
    {
        return hr;
    }

    LOG((LF_CORDB, LL_INFO10000, "D::LJDANA: waiting on m_exUnmanagedAttachEvent and debugger's process handle\n"));
    DWORD  dwHandles = 2;
    HANDLE arrHandles[2];
    arrHandles[0] = GetUnmanagedAttachEvent();
    arrHandles[1] = processInfo.hProcess;

    // Let the helper thread do the attach logic for us and wait for the
    // attach event.  Must release the lock before blocking on a wait.
    dbgLockHolder.Release();

    // Wait for one or the other to be set. Multiple threads could be waiting here.
    // The events are manual events, so when they go high, all threads will be released.
    DWORD res = WaitForMultipleObjectsEx(dwHandles, arrHandles, FALSE, INFINITE, FALSE);

    // We no long need to keep handles to the debugger process.
    CloseHandle(processInfo.hProcess);
    CloseHandle(processInfo.hThread);

    // Indicate to the caller that the attach was aborted
    if (res == WAIT_OBJECT_0 + 1)
    {
        LOG((LF_CORDB, LL_INFO10000, "D::LJDANA: Debugger process is unexpectedly terminated!\n"));
        return E_FAIL;
    }

    // Otherwise, attach was successful (Note, only native attach is done so far)
    _ASSERTE((res == WAIT_OBJECT_0) && "WaitForMultipleObjectsEx failed!");
    LOG( (LF_CORDB, LL_INFO10000, "D::LJDANA: Leaving\n") );
    return S_OK;

}

// Blocks until the debugger completes jit attach
void Debugger::WaitForDebuggerAttach()
{
    LIMITED_METHOD_CONTRACT;

    LOG( (LF_CORDB, LL_INFO10000, "D::WFDA:Entering\n") );

    // if this thread previously called LaunchDebuggerAndNativeAttach then this wait is spurious,
    // the event is still set and it continues immediately. If this is an auxilliary thread however
    // then the wait is necessary
    // If we are not launching the debugger (e.g. unhandled exception on Win7), then we should not
    // wait on the unmanaged attach event.  If the debugger is launched by the OS, then the unmanaged
    // attach event passed to the debugger is created by the OS, not by us, so our event will never
    // be signaled.
    if (m_launchingDebugger)
    {
        WaitForSingleObject(GetUnmanagedAttachEvent(), INFINITE);
    }

    // Wait until the pending managed debugger attach is completed
    if (CORDebuggerPendingAttach() && !CORDebuggerAttached())
    {
        LOG( (LF_CORDB, LL_INFO10000, "D::WFDA: Waiting for managed attach too\n") );
        WaitForSingleObject(GetAttachEvent(), INFINITE);
    }

    // We can't reset the event here because some threads may
    // be just about to wait on it. If we reset it before the
    // other threads hit the wait, they'll block.

    // We have an innate race here that can't easily fix. The best
    // we can do is have a super small window (by moving the reset as
    // far out this making it very unlikely that a thread will
    // hit the window.

    LOG( (LF_CORDB, LL_INFO10000, "D::WFDA: Leaving\n") );
}

// Cleans up after jit attach is complete
void Debugger::PostJitAttach()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
        PRECONDITION(!ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    LOG( (LF_CORDB, LL_INFO10000, "D::PostJA: Entering\n") );
    // Multiple threads may be calling this, so need to take the lock.
    DebuggerLockHolder dbgLockHolder(this);

    // clear the attaching flags which allows other threads to initiate jit attach if needed
    m_jitAttachInProgress = FALSE;
    m_launchingDebugger = FALSE;
    CLRJitAttachState = 0;

    // set the attaching events to unblock other threads waiting on this attach
    // regardless of whether or not it completed
    SetEvent(GetUnmanagedAttachEvent());
    SetEvent(GetAttachEvent());
    LOG( (LF_CORDB, LL_INFO10000, "D::PostJA: Leaving\n") );
}

//---------------------------------------------------------------------------------------
// Launches a debugger and blocks waiting for it to either attach or abort the attach.
//
// Arguments:
//    pThread - the managed thread with the unhandled excpetion
//    pExceptionInfo - the unhandled exception info
//    willSendManagedEvent - TRUE if after getting attached we will send a managed debug event
//    explicitUserRequest - TRUE if this attach is caused by a call to the Debugger.Launch() API.
//
// Returns:
//     None. Callers can requery if a debugger is attached.
//
// Assumptions:
//     This may be called by multiple threads, each firing their own debug events. This function will handle locking.
//     Thus this could block for an arbitrary length of time:
//     - may need to prompt the user to decide if an attach occurs.
//     - may block waiting for a debugger to attach.
//
// Notes:
//     The launch string is retrieved from code:GetDebuggerSettingInfo.
//     This will not do a sync-complete. Instead, the caller can send a debug event (the jit-attach
//     event, such as a User-breakpoint or unhandled exception) and that can send a sync-complete,
//     just as if the debugger was always attached. This ensures that the jit-attach event is in the
//     same callback queue as any faked-up events that the Right-side Shim creates.
//
void Debugger::JitAttach(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo, BOOL willSendManagedEvent, BOOL explicitUserRequest)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(!ThisIsHelperThreadWorker()); // Must be a managed thread
    }
    CONTRACTL_END;

    // Don't do anything if there is a native debugger already attached or the debugging support has been disabled.
    if (IsDebuggerPresent() || m_pRCThread == NULL)
        return;

    GCX_PREEMP_EEINTERFACE_TOGGLE_IFTHREAD();

    EnsureDebuggerAttached(pThread, pExceptionInfo, willSendManagedEvent, explicitUserRequest);
}

//-----------------------------------------------------------------------------
// Ensure that a debugger is attached. Will jit-attach if needed.
//
// Arguments
//    pThread - the managed thread with the unhandled excpetion
//    pExceptionInfo - the unhandled exception info
//    willSendManagedEvent - true if after getting (or staying) attached we will send
//                           a managed debug event
//    explicitUserRequest - true if this attach is caused by a call to the
//                          Debugger.Launch() API.
//
// Returns:
//   None. Either a debugger is attached or it is not.
//
// Notes:
//   There are several intermediate possible outcomes:
//   - Debugger already attached before this was called.
//   - JIT-atttach debugger spawned, and attached successfully.
//   - JIT-attach debugger spawned, but declined to attach.
//   - Failed to spawn jit-attach debugger.
//
//   Ultimately, the only thing that matters at the end is whether a debugger
//   is now attached, which is retreived via CORDebuggerAttached().
//-----------------------------------------------------------------------------
void Debugger::EnsureDebuggerAttached(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo, BOOL willSendManagedEvent, BOOL explicitUserRequest)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        PRECONDITION(!ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    LOG( (LF_CORDB,LL_INFO10000,"D::EDA\n") );

    HRESULT hr = S_OK;

    // We could be in three states:
    // 1) no debugger attached
    // 2) native attached but not managed (yet?)
    // 3) native attached and managed


    // There is a race condition here that can be hit if multiple threads
    // were to trigger jit attach at the right time
    // Thread 1 starts jit attach
    // Thread 2 also starts jit attach and gets to waiting for the attach complete
    // Thread 1 rapidly completes the jit attach then starts it again
    // Thread 2 may still be waiting from the first jit attach at this point
    //
    // Note that this isn't all that bad because if the debugger hasn't actually detached
    // in the middle then the second jit attach will complete almost instantly and thread 2
    // is unblocked. If the debugger did detach in the middle then it seems reasonable for
    // thread 2 to continue to wait until until the debugger is attached once again for the
    // second attach. Basically if one jit attach completes and restarts fast enough it might
    // just go unnoticed by some threads and it will be as if it never happened. Doesn't seem
    // that bad as long as we know another jit attach is again in progress.

    BOOL startedJitAttach = FALSE;

    // First check to see if we need to launch the debugger ourselves
    if(PreJitAttach(willSendManagedEvent, TRUE, explicitUserRequest))
    {
        // if the debugger is already attached then we can't launch one
        // and whatever attach state we are in is just what we get
        if(IsDebuggerPresent())
        {
            // unblock other threads waiting on our attach and clean up
            PostJitAttach();
            return;
        }
        else
        {
            hr = LaunchJitDebuggerAndNativeAttach(pThread, pExceptionInfo);
            if(FAILED(hr))
            {
                // unblock other threads waiting on our attach and clean up
                PostJitAttach();
                return;
            }
        }
        startedJitAttach = TRUE;
    }

    // at this point someone should have launched the native debugger and
    // it is somewhere between not attached and attach complete
    // (it might have even been completely attached before this function even started)
    // step 2 - wait for the attach to complete
    WaitForDebuggerAttach();

    // step 3 - if we initiated then we also cleanup
    if(startedJitAttach)
        PostJitAttach();
    LOG( (LF_CORDB, LL_INFO10000, "D::EDA:Leaving\n") );
}


// Proxy code for AttachDebuggerForBreakpoint
// Structure used in the proxy function callback
struct SendExceptionOnHelperThreadParams
{
    Debugger        *m_pThis;
    HRESULT         m_retval;
    Thread          *m_pThread;
    OBJECTHANDLE    m_exceptionHandle;
    bool            m_continuable;
    FramePointer    m_framePointer;
    SIZE_T          m_nOffset;
    CorDebugExceptionCallbackType m_eventType;
    DWORD           m_dwFlags;


    SendExceptionOnHelperThreadParams() :
        m_pThis(NULL),
        m_retval(S_OK),
        m_pThread(NULL)
        {LIMITED_METHOD_CONTRACT; }
};

//**************************************************************************
// This function sends Exception and ExceptionCallback2 event.
//
// Arguments:
//   pThread : managed thread which exception takes place
//   exceptionHandle : handle to the managed exception object (usually
//       something derived from System.Exception)
//   fContinuable : true iff continuable
//   framePointer : frame pointer associated with callback.
//   nOffset : il offset associated with callback.
//   eventType : type of callback
//   dwFlags : additional flags (see CorDebugExceptionFlags).
//
// Returns:
//    S_OK on sucess. Else some error. May also throw.
//
// Notes:
//    This is a helper for code:Debugger.SendExceptionEventsWorker.
//    See code:Debugger.SendException for more details about parameters.
//    This is always called on a managed thread (never the helper thread)
//    This will synchronize and block.
//**************************************************************************
HRESULT Debugger::SendExceptionHelperAndBlock(
    Thread      *pThread,
    OBJECTHANDLE exceptionHandle,
    bool        fContinuable,
    FramePointer framePointer,
    SIZE_T      nOffset,
    CorDebugExceptionCallbackType eventType,
    DWORD       dwFlags)

{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;

    // This is a normal event to send from LS to RS
    SENDIPCEVENT_BEGIN(this, pThread);

    // This function can be called on helper thread or managed thread.
    // However, we should be holding locks upon entry

    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

    //
    // Send pre-Whidbey EXCEPTION IPC event.
    //
    InitIPCEvent(ipce, DB_IPCE_EXCEPTION, pThread, pThread->GetDomain());

    ipce->Exception.vmExceptionHandle.SetRawPtr(exceptionHandle);
    ipce->Exception.firstChance = (eventType == DEBUG_EXCEPTION_FIRST_CHANCE);
    ipce->Exception.continuable = fContinuable;
    hr = m_pRCThread->SendIPCEvent();

    _ASSERTE(SUCCEEDED(hr) && "D::SE: Send ExceptionCallback event failed.");

    //
    // Send Whidbey EXCEPTION IPC event.
    //
    InitIPCEvent(ipce, DB_IPCE_EXCEPTION_CALLBACK2, pThread, pThread->GetDomain());

    ipce->ExceptionCallback2.framePointer = framePointer;
    ipce->ExceptionCallback2.eventType = eventType;
    ipce->ExceptionCallback2.nOffset = nOffset;
    ipce->ExceptionCallback2.dwFlags = dwFlags;
    ipce->ExceptionCallback2.vmExceptionHandle.SetRawPtr(exceptionHandle);

    LOG((LF_CORDB, LL_INFO10000, "D::SE: sending ExceptionCallback2 event"));
    hr = m_pRCThread->SendIPCEvent();

    if (eventType == DEBUG_EXCEPTION_FIRST_CHANCE)
    {
        pThread->GetExceptionState()->GetFlags()->SetSentDebugFirstChance();
    }
    else
    {
        _ASSERTE(eventType == DEBUG_EXCEPTION_UNHANDLED);
    }

    _ASSERTE(SUCCEEDED(hr) && "D::SE: Send ExceptionCallback2 event failed.");

    if (SUCCEEDED(hr))
    {
        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }

    // Let other Runtime threads handle their events.
    SENDIPCEVENT_END;

    return hr;

}

// Send various first-chance / unhandled exception events.
//
// Assumptions:
//    Caller has already determined that we want to send exception events.
//
// Notes:
//    This is a helper function for code:Debugger.SendException
void Debugger::SendExceptionEventsWorker(
    Thread * pThread,
    bool fFirstChance,
    bool fIsInterceptable,
    bool fContinuable,
    SIZE_T currentIP,
    FramePointer framePointer,
    bool atSafePlace)
{
    HRESULT hr = S_OK;

    ThreadExceptionState* pExState = pThread->GetExceptionState();
    //
    // Figure out parameters to the IPC events.
    //
    const BYTE *ip;

    SIZE_T nOffset = (SIZE_T)ICorDebugInfo::NO_MAPPING;
    DebuggerMethodInfo *pDebugMethodInfo = NULL;

    // If we're passed a zero IP or SP, then go to the ThreadExceptionState on the thread to get the data. Note:
    // we can only do this if there is a context in the pExState. There are cases (most notably the
    // EEPolicy::HandleFatalError case) where we don't have that. So we just leave the IP/SP 0.
    if ((currentIP == 0) && (pExState->GetContextRecord() != NULL))
    {
        ip = (BYTE *)GetIP(pExState->GetContextRecord());
    }
    else
    {
        ip = (BYTE *)currentIP;
    }

    if (g_pEEInterface->IsManagedNativeCode(ip))
    {

        MethodDesc *pMethodDesc = g_pEEInterface->GetNativeCodeMethodDesc(PCODE(ip));
        _ASSERTE(pMethodDesc != NULL);

        if (pMethodDesc != NULL)
        {
            DebuggerJitInfo *pDebugJitInfo = GetJitInfo(pMethodDesc, ip, &pDebugMethodInfo);

            if (pDebugJitInfo != NULL)
            {
                SIZE_T nativeOffset = CodeRegionInfo::GetCodeRegionInfo(pDebugJitInfo, pMethodDesc).AddressToOffset(ip);
                CorDebugMappingResult mapResult;
                DWORD which;

                nOffset = pDebugJitInfo->MapNativeOffsetToIL(nativeOffset, &mapResult, &which);
            }
        }
    }

    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

    if (fFirstChance)
    {
        // We can call into this method when there is no exception in progress to alert
        // the debugger to a stack overflow, however that case should never specify first
        // chance. An exception must be in progress to check the flags on the exception state
        _ASSERTE(pThread->IsExceptionInProgress());

        //
        // Send the first chance exception if we have not already and if it is not suppressed
        //
        if (m_sendExceptionsOutsideOfJMC && !pExState->GetFlags()->SentDebugFirstChance())
        {
            // Blocking here is especially important so that the debugger can mark any code as JMC.
            hr = SendExceptionHelperAndBlock(
                pThread,
                g_pEEInterface->GetThreadException(pThread),
                fContinuable,
                framePointer,
                nOffset,
                DEBUG_EXCEPTION_FIRST_CHANCE,
                fIsInterceptable ? DEBUG_EXCEPTION_CAN_BE_INTERCEPTED : 0);

            {
                // Toggle GC into COOP to block this thread.
                GCX_COOP_EEINTERFACE();

                //
                // If we weren't at a safe place when we enabled PGC, then go ahead and unmark that fact now that we've successfully
                // disabled.
                //
                if (!atSafePlace)
                {
                    g_pDebugger->DecThreadsAtUnsafePlaces();
                }

                ProcessAnyPendingEvals(pThread);

                //
                // If we weren't at a safe place, increment the unsafe count before we enable preemptive mode.
                //
                if (!atSafePlace)
                {
                    g_pDebugger->IncThreadsAtUnsafePlaces();
                }
            } // end of GCX_CCOP_EEINTERFACE();
        } //end if (m_sendExceptionsOutsideOfJMC && !SentDebugFirstChance())

        //
        // If this is a JMC function, then we send a USER's first chance as well.
        //
        if ((pDebugMethodInfo != NULL) &&
            pDebugMethodInfo->IsJMCFunction() &&
            !pExState->GetFlags()->SentDebugUserFirstChance())
        {
            SENDIPCEVENT_BEGIN(this, pThread);

            InitIPCEvent(ipce, DB_IPCE_EXCEPTION_CALLBACK2, pThread, pThread->GetDomain());

            ipce->ExceptionCallback2.framePointer = framePointer;
            ipce->ExceptionCallback2.eventType = DEBUG_EXCEPTION_USER_FIRST_CHANCE;
            ipce->ExceptionCallback2.nOffset = nOffset;
            ipce->ExceptionCallback2.dwFlags = fIsInterceptable ? DEBUG_EXCEPTION_CAN_BE_INTERCEPTED : 0;
            ipce->ExceptionCallback2.vmExceptionHandle.SetRawPtr(g_pEEInterface->GetThreadException(pThread));

            LOG((LF_CORDB, LL_INFO10000, "D::SE: sending ExceptionCallback2 (USER FIRST CHANCE)"));
            hr = m_pRCThread->SendIPCEvent();

            _ASSERTE(SUCCEEDED(hr) && "D::SE: Send ExceptionCallback2 (User) event failed.");

            if (SUCCEEDED(hr))
            {
                // Stop all Runtime threads
                TrapAllRuntimeThreads();
            }

            pExState->GetFlags()->SetSentDebugUserFirstChance();

            // Let other Runtime threads handle their events.
            SENDIPCEVENT_END;

        } // end if (!SentDebugUserFirstChance)

    } // end if (firstChance)
    else
    {
        // unhandled exception case
        // if there is no exception in progress then we are sending a fake exception object
        //   as an indication of a fatal error (stack overflow). In this case it is illegal
        //   to read GetFlags() from the exception state.
        // else if there is an exception in progress we only want to send the notification if
        //   we did not already send a CHF, previous unhandled, or unwind begin notification
        BOOL sendNotification = TRUE;
        if(pThread->IsExceptionInProgress())
        {
            sendNotification = !pExState->GetFlags()->DebugCatchHandlerFound() &&
                               !pExState->GetFlags()->SentDebugUnhandled() &&
                               !pExState->GetFlags()->SentDebugUnwindBegin();
        }

        if(sendNotification)
        {
            hr = SendExceptionHelperAndBlock(
                pThread,
                g_pEEInterface->GetThreadException(pThread),
                fContinuable,
                LEAF_MOST_FRAME,
                (SIZE_T)ICorDebugInfo::NO_MAPPING,
                DEBUG_EXCEPTION_UNHANDLED,
                fIsInterceptable ? DEBUG_EXCEPTION_CAN_BE_INTERCEPTED : 0);

            if(pThread->IsExceptionInProgress())
            {
                pExState->GetFlags()->SetSentDebugUnhandled();
            }
        }

    } // end if (!firstChance)
}

//
// SendException is called by Runtime threads to send that they've hit an Managed exception to the Right Side.
// This may block this thread and suspend the debuggee, and let the debugger inspect us.
//
// The thread's throwable should be set so that the debugger can inspect the current exception.
// It does not report native exceptions in native code (which is consistent because those don't have a
// managed exception object).
//
// This may kick off a jit-attach (in which case fAttaching==true), and so may be called even when no debugger
// is yet involved.
//
// Parameters:
//    pThread - the thread throwing the exception.
//    fFirstChance - true if this is a first chance exception. False if this is an unhandled exception.
//    currentIP - absolute native address of the exception if it is from managed code. If this is 0, we try to find it
//                based off the thread's current exception state.
//    currentSP - stack pointer of the exception. This will get converted into a FramePointer and then used by the debugger
//                to identify which stack frame threw the exception.
//    currentBSP - additional information for IA64 only to identify the stack frame.
//    fContinuable - not used.
//    fAttaching - true iff this exception may initiate a jit-attach. In the common case, if this is true, then
//                 CorDebuggerAttached() is false. However, since a debugger can attach at any time, it's possible
//                 for another debugger to race against the jit-attach and win. Thus this may err on the side of being true.
//    fForceNonInterceptable - This is used to determine if the exception is continuable (ie "Interceptible",
//                  we can handle a DB_IPCE_INTERCEPT_EXCEPTION event for it). If true, then the exception can not be continued.
//                  If false, we get continuation status from the exception properties of the current thread.
//
// Returns:
//    S_OK on success (common case by far).
//    propogates other errors.
//
HRESULT Debugger::SendException(Thread *pThread,
                                bool fFirstChance,
                                SIZE_T currentIP,
                                SIZE_T currentSP,
                                bool fContinuable, // not used by RS.
                                bool fAttaching,
                                bool fForceNonInterceptable,
                                EXCEPTION_POINTERS * pExceptionInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;

        MODE_ANY;

        PRECONDITION(HasLazyData());
        PRECONDITION(CheckPointer(pThread));
        PRECONDITION((pThread->GetFilterContext() == NULL) || !fFirstChance);
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::SendException\n"));

    if (CORDBUnrecoverableError(this))
    {
        return (E_FAIL);
    }

    // Mark if we're at an unsafe place.
    AtSafePlaceHolder unsafePlaceHolder(pThread);

    // Grab the exception name from the current exception object to pass to the JIT attach.
    bool fIsInterceptable;

    if (fForceNonInterceptable)
    {
        fIsInterceptable = false;
        m_forceNonInterceptable = true;
    }
    else
    {
        fIsInterceptable = IsInterceptableException(pThread);
        m_forceNonInterceptable = false;
    }

    ThreadExceptionState* pExState = pThread->GetExceptionState();
    BOOL managedEventNeeded = ((!fFirstChance) ||
            (fFirstChance && (!pExState->GetFlags()->SentDebugFirstChance() || !pExState->GetFlags()->SentDebugUserFirstChance())));

    // There must be a managed exception object to send a managed exception event
    if (g_pEEInterface->IsThreadExceptionNull(pThread) && (pThread->LastThrownObjectHandle() == NULL))
    {
        managedEventNeeded = FALSE;
    }

    if (fAttaching)
    {
        JitAttach(pThread, pExceptionInfo, managedEventNeeded, FALSE);
        // If the jit-attach occurred, CORDebuggerAttached() may now be true and we can
        // just act as if a debugger was always attached.
    }

    if(managedEventNeeded)
    {
        {
            // We have to send enabled, so enable now.
            GCX_PREEMP_EEINTERFACE();

            // Send the exception events. Even in jit-attach case, we should now be fully attached.
            if (CORDebuggerAttached())
            {
                // Initialize frame-pointer associated with exception notification.
                LPVOID stackPointer;
                if ((currentSP == 0) && (pExState->GetContextRecord() != NULL))
                {
                    stackPointer = dac_cast<PTR_VOID>(GetSP(pExState->GetContextRecord()));
                }
                else
                {
                    stackPointer = (LPVOID)currentSP;
                }
                FramePointer framePointer = FramePointer::MakeFramePointer(stackPointer);


                // Do the real work of sending the events
                SendExceptionEventsWorker(
                    pThread,
                    fFirstChance,
                    fIsInterceptable,
                    fContinuable,
                    currentIP,
                    framePointer,
                    !unsafePlaceHolder.IsAtUnsafePlace());
            }
            else
            {
                LOG((LF_CORDB,LL_INFO100, "D:SE: Skipping SendIPCEvent because not supposed to send anything, or RS detached.\n"));
            }
        }

        // If we weren't at a safe place when we switched to PREEMPTIVE, then go ahead and unmark that fact now
        // that we're successfully back in COOPERATIVE mode.
        unsafePlaceHolder.Clear();

        {
            GCX_COOP_EEINTERFACE();
            ProcessAnyPendingEvals(pThread);
        }
    }

    if (CORDebuggerAttached())
    {
        return S_FALSE;
    }
    else
    {
        return S_OK;
    }
}


/*
 * ProcessAnyPendingEvals
 *
 * This function checks for, and then processes, any pending func-evals.
 *
 * Parameters:
 *   pThread - The thread to process.
 *
 * Returns:
 *   None.
 *
 */
void Debugger::ProcessAnyPendingEvals(Thread *pThread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    // If no debugger is attached, then no evals to process.
    // We may get here in oom situations during jit-attach, so we'll check now and be safe.
    if (!CORDebuggerAttached())
    {
        return;
    }

    //
    // Note: if there is a filter context installed, we may need remove it, do the eval, then put it back. I'm not 100%
    // sure which yet... it kinda depends on whether or not we really need the filter context updated due to a
    // collection during the func eval...
    //
    // If we need to do a func eval on this thread, then there will be a pending eval registered for this thread. We'll
    // loop so long as there are pending evals registered. We block in FuncEvalHijackWorker after sending up the
    // FuncEvalComplete event, so if the user asks for another func eval then there will be a new pending eval when we
    // loop and check again.
    //
    DebuggerPendingFuncEval *pfe;

    while (GetPendingEvals() != NULL && (pfe = GetPendingEvals()->GetPendingEval(pThread)) != NULL)
    {
        DebuggerEval *pDE = pfe->pDE;

        _ASSERTE(pDE->m_evalDuringException);
        _ASSERTE(pDE->m_thread == GetThreadNULLOk());

        // Remove the pending eval from the hash. This ensures that if we take a first chance exception during the eval
        // that we can do another nested eval properly.
        GetPendingEvals()->RemovePendingEval(pThread);

        // Go ahead and do the pending func eval. pDE is invalid after this.
        void *ret;
        ret = ::FuncEvalHijackWorker(pDE);


        // The return value should be NULL when FuncEvalHijackWorker is called as part of an exception.
        _ASSERTE(ret == NULL);
    }

#endif

}


/*
 * FirstChanceManagedException is called by Runtime threads when crawling the managed stack frame
 * for a handler for the exception.  It is called for each managed call on the stack.
 *
 * Parameters:
 *   pThread - The thread the exception is occurring on.
 *   currentIP - the IP in the current stack frame.
 *   currentSP - the SP in the current stack frame.
 *
 * Returns:
 *   Always FALSE.
 *
 */
bool Debugger::FirstChanceManagedException(Thread *pThread, SIZE_T currentIP, SIZE_T currentSP)
{

    // @@@
    // Implement DebugInterface
    // Can only be called from EE/exception
    // must be on managed thread.

    CONTRACTL
    {
        THROWS;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        PRECONDITION(CORDebuggerAttached());
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::FCE: First chance exception, TID:0x%x, \n", GetThreadIdHelper(pThread)));

    _ASSERTE(GetThreadNULLOk() != NULL);

#ifdef _DEBUG
    static ConfigDWORD d_fce;
    if (d_fce.val(CLRConfig::INTERNAL_D__FCE))
        _ASSERTE(!"Stop in Debugger::FirstChanceManagedException?");
#endif

    SendException(pThread, TRUE, currentIP, currentSP, FALSE, FALSE, FALSE, NULL);

    return false;
}


/*
 * FirstChanceManagedExceptionCatcherFound is called by Runtime threads when crawling the
 * managed stack frame and a handler for the exception is found.
 *
 * Parameters:
 *   pThread - The thread the exception is occurring on.
 *   pTct - Contains the function information that has the catch clause.
 *   pEHClause - Contains the native offset information of the catch clause.
 *
 * Returns:
 *   None.
 *
 */
void Debugger::FirstChanceManagedExceptionCatcherFound(Thread *pThread,
                                                       MethodDesc *pMD, TADDR pMethodAddr,
                                                       BYTE *currentSP,
                                                       EE_ILEXCEPTION_CLAUSE *pEHClause)
{

    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
        MODE_ANY;
    }
    CONTRACTL_END;

    // @@@
    // Implements DebugInterface
    // Call by EE/exception. Must be on managed thread
    _ASSERTE(GetThreadNULLOk() != NULL);

    // Quick check.
    if (!CORDebuggerAttached())
    {
        return;
    }

    // Compute the offset

    DWORD nOffset = (DWORD)(SIZE_T)ICorDebugInfo::NO_MAPPING;
    DebuggerMethodInfo *pDebugMethodInfo = NULL;
    DebuggerJitInfo *pDebugJitInfo = NULL;
    bool isInJMCFunction = false;

    if (pMD != NULL)
    {
        _ASSERTE(!pMD->IsILStub());

        pDebugJitInfo = GetJitInfo(pMD, (const BYTE *) pMethodAddr, &pDebugMethodInfo);
        if (pDebugMethodInfo != NULL)
        {
            isInJMCFunction = pDebugMethodInfo->IsJMCFunction();
        }
    }

    // Here we check if debugger opted-out of receiving exception related events from outside of JMC methods
    // or this exception ever crossed JMC frame (in this case we have already sent user first chance event)
    if (m_sendExceptionsOutsideOfJMC ||
        isInJMCFunction ||
        pThread->GetExceptionState()->GetFlags()->SentDebugUserFirstChance())
    {
        if (pDebugJitInfo != NULL)
        {
            CorDebugMappingResult mapResult;
            DWORD which;

            // Map the native instruction to the IL instruction.
            // Be sure to skip past the prolog on amd64/arm to get the right IL
            // instruction (on x86 there will not be a prolog as x86 does not use
            // funclets).
            nOffset = pDebugJitInfo->MapNativeOffsetToIL(
                pEHClause->HandlerStartPC,
                &mapResult,
                &which,
                TRUE
                );
        }

        bool fIsInterceptable = IsInterceptableException(pThread);
        m_forceNonInterceptable = false;
        DWORD dwFlags = fIsInterceptable ? DEBUG_EXCEPTION_CAN_BE_INTERCEPTED : 0;

        FramePointer fp = FramePointer::MakeFramePointer(currentSP);
        SendCatchHandlerFound(pThread, fp, nOffset, dwFlags);
    }

    // flag that we catch handler found so that we won't send other mutually exclusive events
    // such as unwind begin or unhandled
    pThread->GetExceptionState()->GetFlags()->SetDebugCatchHandlerFound();
}

// Filter to trigger CHF callback
// Notify of a catch-handler found callback.
LONG Debugger::NotifyOfCHFFilter(EXCEPTION_POINTERS* pExceptionPointers, PVOID pData)
{
    CONTRACTL
    {
        if ((GetThreadNULLOk() == NULL) || g_pEEInterface->IsThreadExceptionNull(GetThread()))
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        else
        {
            THROWS;
            MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
        }
        MODE_ANY;
    }
    CONTRACTL_END;

    SCAN_IGNORE_TRIGGER; // Scan can't handle conditional contracts.

    // @@@
    // Implements DebugInterface
    // Can only be called from EE

    // If no debugger is attached, then don't bother sending the events.
    // This can't kick off a jit-attach.
    if (!CORDebuggerAttached())
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    //
    // If this exception has never bubbled thru to managed code, then there is no
    // useful information for the debugger and, in fact, it may be a completely
    // internally handled runtime exception, so we should do nothing.
    //
    if ((GetThreadNULLOk() == NULL) || g_pEEInterface->IsThreadExceptionNull(GetThread()))
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Caller must pass in the stack address. This should match up w/ a Frame.
    BYTE * pCatcherStackAddr = (BYTE*) pData;

    // If we don't have any catcher frame, then use ebp from the context.
    if (pData == NULL)
    {
        pCatcherStackAddr = (BYTE*) GetFP(pExceptionPointers->ContextRecord);
    }
    else
    {
#ifdef _DEBUG
        _ASSERTE(pData != NULL);
        {
            // We want the CHF stack addr to match w/ the Internal Frame Cordbg sees
            // in the stacktrace.
            // The Internal Frame comes from an EE Frame. This means that the CHF stack
            // addr must match that EE Frame exactly. Let's check that now.

            Frame * pFrame = reinterpret_cast<Frame*>(pData);
            // Calling a virtual method will enforce that we have a valid Frame. ;)
            // If we got passed in a random catch address, then when we cast to a Frame
            // the vtable pointer will be bogus and this call will AV.
            Frame::ETransitionType e;
            e = pFrame->GetTransitionType();
        }
#endif
    }

    // @todo - when Stubs-In-Stacktraces is always enabled, remove this.
    if (!g_EnableSIS)
    {
        return EXCEPTION_CONTINUE_SEARCH;
    }

    // Stubs don't have an IL offset.
    const SIZE_T offset = (SIZE_T)ICorDebugInfo::NO_MAPPING;
    Thread *pThread = GetThread();
    DWORD dwFlags = IsInterceptableException(pThread) ? DEBUG_EXCEPTION_CAN_BE_INTERCEPTED : 0;
    m_forceNonInterceptable = false;

    FramePointer fp = FramePointer::MakeFramePointer(pCatcherStackAddr);

    //
    // If we have not sent a first-chance notification, do so now.
    //
    ThreadExceptionState* pExState = pThread->GetExceptionState();

    if (!pExState->GetFlags()->SentDebugFirstChance())
    {
        SendException(pThread,
                      TRUE, // first-chance
                      (SIZE_T)(GetIP(pExceptionPointers->ContextRecord)), // IP
                      (SIZE_T)pCatcherStackAddr, // SP
                      FALSE, // fContinuable
                      FALSE, // attaching
                      TRUE,  // ForceNonInterceptable since we are transition stub, the first and last place
                             // that will see this exception.
                      pExceptionPointers);
    }

    // Here we check if debugger opted-out of receiving exception related events from outside of JMC methods
    // or this exception ever crossed JMC frame (in this case we have already sent user first chance event)
    if (m_sendExceptionsOutsideOfJMC || pExState->GetFlags()->SentDebugUserFirstChance())
    {
        SendCatchHandlerFound(pThread, fp, offset, dwFlags);
    }

    // flag that we catch handler found so that we won't send other mutually exclusive events
    // such as unwind begin or unhandled
    pExState->GetFlags()->SetDebugCatchHandlerFound();

#ifdef DEBUGGING_SUPPORTED
#ifdef DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
    if ( (pThread != NULL) &&
         (pThread->IsExceptionInProgress()) &&
         (pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo()) )
    {
        //
        // The debugger wants to intercept this exception.  It may return in a failure case,
        // in which case we want to continue thru this path.
        //
        ClrDebuggerDoUnwindAndIntercept(X86_FIRST_ARG(EXCEPTION_CHAIN_END) pExceptionPointers->ExceptionRecord);
    }
#endif // DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
#endif // DEBUGGING_SUPPORTED

    return EXCEPTION_CONTINUE_SEARCH;
}


// Actually send the catch handler found event.
// This can be used to send CHF for both regular managed catchers as well
// as stubs that catch (Func-eval, COM-Interop, AppDomains)
void Debugger::SendCatchHandlerFound(
    Thread * pThread,
    FramePointer fp,
    SIZE_T   nOffset,
    DWORD    dwFlags
)
{

    CONTRACTL
    {
        THROWS;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::FirstChanceManagedExceptionCatcherFound\n"));

    if (pThread == NULL)
    {
        _ASSERTE(!"Bad parameter");
        LOG((LF_CORDB, LL_INFO10000, "D::FirstChanceManagedExceptionCatcherFound - Bad parameter.\n"));
        return;
    }

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    //
    // Mark if we're at an unsafe place.
    //
    AtSafePlaceHolder unsafePlaceHolder(pThread);

    {
        GCX_COOP_EEINTERFACE();

        {
            SENDIPCEVENT_BEGIN(this, pThread);

            if (CORDebuggerAttached() &&
                !pThread->GetExceptionState()->GetFlags()->DebugCatchHandlerFound() &&
                !pThread->GetExceptionState()->GetFlags()->SentDebugUnhandled() &&
                !pThread->GetExceptionState()->GetFlags()->SentDebugUnwindBegin())
            {
                HRESULT hr;

                //
                // Figure out parameters to the IPC events.
                //
                DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

                //
                // Send Whidbey EXCEPTION IPC event.
                //
                InitIPCEvent(ipce, DB_IPCE_EXCEPTION_CALLBACK2, pThread, pThread->GetDomain());

                ipce->ExceptionCallback2.framePointer = fp;
                ipce->ExceptionCallback2.eventType = DEBUG_EXCEPTION_CATCH_HANDLER_FOUND;
                ipce->ExceptionCallback2.nOffset = nOffset;
                ipce->ExceptionCallback2.dwFlags = dwFlags;
                ipce->ExceptionCallback2.vmExceptionHandle.SetRawPtr(g_pEEInterface->GetThreadException(pThread));

                LOG((LF_CORDB, LL_INFO10000, "D::FCMECF: sending ExceptionCallback2"));
                hr = m_pRCThread->SendIPCEvent();

                _ASSERTE(SUCCEEDED(hr) && "D::FCMECF: Send ExceptionCallback2 event failed.");

                //
                // Stop all Runtime threads
                //
                TrapAllRuntimeThreads();

            } // end if (!Attached)
            else
            {
                LOG((LF_CORDB,LL_INFO1000, "D:FCMECF: Skipping SendIPCEvent because RS detached.\n"));
            }

            //
            // Let other Runtime threads handle their events.
            //
            SENDIPCEVENT_END;
        }

        //
        // If we weren't at a safe place when we enabled PGC, then go ahead and unmark that fact now that we've successfully
        // disabled.
        //
        unsafePlaceHolder.Clear();

        ProcessAnyPendingEvals(pThread);
    } // end of GCX_COOP_EEINTERFACE();

    return;
}

/*
 * ManagedExceptionUnwindBegin is called by Runtime threads when crawling the
 * managed stack frame and unwinding them.
 *
 * Parameters:
 *   pThread - The thread the unwind is occurring on.
 *
 * Returns:
 *   None.
 *
 */
void Debugger::ManagedExceptionUnwindBegin(Thread *pThread)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // @@@
    // Implements DebugInterface
    // Can only be called on managed threads
    //

    LOG((LF_CORDB, LL_INFO10000, "D::ManagedExceptionUnwindBegin\n"));

    if (pThread == NULL)
    {
        _ASSERTE(!"Bad parameter");
        LOG((LF_CORDB, LL_INFO10000, "D::ManagedExceptionUnwindBegin - Bad parameter.\n"));
        return;
    }

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    //
    // Mark if we're at an unsafe place.
    //
    AtSafePlaceHolder unsafePlaceHolder(pThread);
    {
        GCX_COOP_EEINTERFACE();

        {
            SENDIPCEVENT_BEGIN(this, pThread);

            if (CORDebuggerAttached() &&
                !pThread->GetExceptionState()->GetFlags()->SentDebugUnwindBegin() &&
                !pThread->GetExceptionState()->GetFlags()->DebugCatchHandlerFound() &&
                !pThread->GetExceptionState()->GetFlags()->SentDebugUnhandled())
            {
                HRESULT hr;

                DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

                //
                // Send Whidbey EXCEPTION IPC event.
                //
                InitIPCEvent(ipce, DB_IPCE_EXCEPTION_UNWIND, pThread, pThread->GetDomain());

                ipce->ExceptionUnwind.eventType = DEBUG_EXCEPTION_UNWIND_BEGIN;
                ipce->ExceptionUnwind.dwFlags = 0;

                        LOG((LF_CORDB, LL_INFO10000, "D::MEUB: sending ExceptionUnwind event"));
                        hr = m_pRCThread->SendIPCEvent();

                _ASSERTE(SUCCEEDED(hr) && "D::MEUB: Send ExceptionUnwind event failed.");

                pThread->GetExceptionState()->GetFlags()->SetSentDebugUnwindBegin();

                //
                // Stop all Runtime threads
                //
                TrapAllRuntimeThreads();

            } // end if (!Attached)

            //
            // Let other Runtime threads handle their events.
            //
            SENDIPCEVENT_END;
        }

    //
    // If we weren't at a safe place when we enabled PGC, then go ahead and unmark that fact now that we've successfully
    // disabled.
    //
        unsafePlaceHolder.Clear();
    }

    return;
}

/*
 * DeleteInterceptContext
 *
 * This function is called by the VM to release any debugger specific information for an
 * exception object.  It is called when the VM releases its internal exception stuff, i.e.
 * ExInfo on X86 and ExceptionTracker on WIN64.
 *
 *
 * Parameters:
 *   pContext - Debugger specific context.
 *
 * Returns:
 *   None.
 *
 * Notes:
 *   pContext is just a pointer to a DebuggerContinuableExceptionBreakpoint.
 *
 */
void Debugger::DeleteInterceptContext(void *pContext)
{
    LIMITED_METHOD_CONTRACT;

    DebuggerContinuableExceptionBreakpoint *pBp = (DebuggerContinuableExceptionBreakpoint *)pContext;

    if (pBp != NULL)
    {
        DeleteInteropSafe(pBp);
    }
}


// Get the frame point for an exception handler
FramePointer GetHandlerFramePointer(BYTE *pStack)
{
    FramePointer handlerFP;

#if !defined(TARGET_ARM) && !defined(TARGET_ARM64)
    // Refer to the comment in DispatchUnwind() to see why we have to add
    // sizeof(LPVOID) to the handler ebp.
    handlerFP = FramePointer::MakeFramePointer(LPVOID(pStack + sizeof(void*)));
#else
    // ARM is similar to IA64 in that it uses the establisher frame as the
    // handler. in this case we don't need to add sizeof(void*) to the FP.
    handlerFP = FramePointer::MakeFramePointer((LPVOID)pStack);
#endif // TARGET_ARM

    return handlerFP;
}

//
// ExceptionFilter is called by the Runtime threads when an exception
// is being processed.
// - fd - MethodDesc of filter function
// - pMethodAddr - any address inside of the method. This lets us resolve exactly which version
//                 of the method is being executed (for EnC)
// - offset - native offset to handler.
// - pStack, pBStore - stack pointers.
//
void Debugger::ExceptionFilter(MethodDesc *fd, TADDR pMethodAddr, SIZE_T offset, BYTE *pStack)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(!IsDbgHelperSpecialThread());
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000, "D::EF: pStack:0x%x MD: %s::%s, offset:0x%x\n",
        pStack, fd->m_pszDebugClassName, fd->m_pszDebugMethodName, offset));

    //
    // !!! Need to think through logic for when to step through filter code -
    // perhaps only during a "step in".
    //

    //
    // !!! Eventually there may be some weird mechanics introduced for
    // returning from the filter that we have to understand.  For now we should
    // be able to proceed normally.
    //

    FramePointer handlerFP;
    handlerFP = GetHandlerFramePointer(pStack);

    DebuggerJitInfo * pDJI = NULL;
    EX_TRY
    {
        pDJI = GetJitInfo(fd, (const BYTE *) pMethodAddr);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!fd->IsDynamicMethod() && (pDJI == NULL))
    {
        // The only way we shouldn't have a DJI is from a dynamic method or from oom (which the LS doesn't handle).
        _ASSERTE(!"Debugger doesn't support OOM scenarios.");
        return;
    }

    DebuggerController::DispatchUnwind(g_pEEInterface->GetThread(),
                                       fd, pDJI, offset, handlerFP, STEP_EXCEPTION_FILTER);
}


//
// ExceptionHandle is called by Runtime threads when an exception is
// being handled.
// - fd - MethodDesc of filter function
// - pMethodAddr - any address inside of the method. This lets us resolve exactly which version
//                 of the method is being executed (for EnC)
// - offset - native offset to handler.
// - pStack, pBStore - stack pointers.
//
void Debugger::ExceptionHandle(MethodDesc *fd, TADDR pMethodAddr, SIZE_T offset, BYTE *pStack)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(!IsDbgHelperSpecialThread());
    }
    CONTRACTL_END;


    FramePointer handlerFP;
    handlerFP = GetHandlerFramePointer(pStack);

    DebuggerJitInfo * pDJI = NULL;
    EX_TRY
    {
        pDJI = GetJitInfo(fd, (const BYTE *) pMethodAddr);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!fd->IsDynamicMethod() && (pDJI == NULL))
    {
        // The only way we shouldn't have a DJI is from a dynamic method or from oom (which the LS doesn't handle).
        _ASSERTE(!"Debugger doesn't support OOM scenarios.");
        return;
    }


    DebuggerController::DispatchUnwind(g_pEEInterface->GetThread(),
                                       fd, pDJI, offset, handlerFP, STEP_EXCEPTION_HANDLER);
}

BOOL Debugger::ShouldAutoAttach()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!CORDebuggerAttached());

    // We're relying on the caller to determine the

    LOG((LF_CORDB, LL_INFO1000000, "D::SAD\n"));

    // Check if the user has specified a seting in the registry about what he
    // wants done when an unhandled exception occurs.
    DebuggerLaunchSetting dls = GetDbgJITDebugLaunchSetting();

    return (dls == DLS_ATTACH_DEBUGGER);

    // @TODO cache the debugger launch setting.

}

BOOL Debugger::FallbackJITAttachPrompt()
{
    _ASSERTE(!CORDebuggerAttached());
    return (ATTACH_YES == this->ShouldAttachDebuggerProxy(false));
}

void Debugger::MarkDebuggerAttachedInternal()
{
    LIMITED_METHOD_CONTRACT;

    // Attach is complete now.
    LOG((LF_CORDB, LL_INFO10000, "D::FEDA: Attach Complete!\n"));
    g_pEEInterface->MarkDebuggerAttached();

    _ASSERTE(HasLazyData());
}
void Debugger::MarkDebuggerUnattachedInternal()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(HasLazyData());

    g_pEEInterface->MarkDebuggerUnattached();
}

//-----------------------------------------------------------------------------
// Favor to do lazy initialization on helper thread.
// This is needed to allow lazy intialization in Stack Overflow scenarios.
// We may or may not already be initialized.
//-----------------------------------------------------------------------------
void LazyInitFavor(void *)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;
    Debugger::DebuggerLockHolder dbgLockHolder(g_pDebugger);
    HRESULT hr;
    hr = g_pDebugger->LazyInitWrapper();
    (void)hr; //prevent "unused variable" error from GCC

    // On checked builds, warn that we're hitting a scenario that debugging doesn't support.
    _ASSERTE(SUCCEEDED(hr) || !"Couldn't initialize lazy data for LastChanceManagedException");
}

/******************************************************************************
 *
 ******************************************************************************/
LONG Debugger::LastChanceManagedException(EXCEPTION_POINTERS * pExceptionInfo,
                                          Thread *pThread,
                                          BOOL jitAttachRequested)
{
    CONTRACTL
    {
        NOTHROW;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
        MODE_ANY;
    }
    CONTRACTL_END;

    // @@@
    // Implements DebugInterface.
    // Can be run only on managed thread.

    LOG((LF_CORDB, LL_INFO10000, "D::LastChanceManagedException\n"));

    // Don't stop for native debugging anywhere inside our inproc-Filters.
    CantStopHolder hHolder;

    EXCEPTION_RECORD * pExceptionRecord = pExceptionInfo->ExceptionRecord;
    CONTEXT * pContext = pExceptionInfo->ContextRecord;

    // You're allowed to call this function with a NULL exception record and context. If you do, then its assumed
    // that we want to head right down to asking the user if they want to attach a debugger. No need to try to
    // dispatch the exception to the debugger controllers. You have to pass NULL for both the exception record and
    // the context, though. They're a pair. Both have to be NULL, or both have to be valid.
    _ASSERTE(((pExceptionRecord != NULL) && (pContext != NULL)) ||
             ((pExceptionRecord == NULL) && (pContext == NULL)));

    if (CORDBUnrecoverableError(this))
    {
        return ExceptionContinueSearch;
    }

    // We don't do anything on the second pass
    if ((pExceptionRecord != NULL) && ((pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING) != 0))
    {
        return ExceptionContinueSearch;
    }

    // Let the controllers have a chance at it - this may be the only handler which can catch the exception if this
    // is a native patch.

    if ((pThread != NULL) &&
        (pContext != NULL) &&
        CORDebuggerAttached() &&
        DebuggerController::DispatchNativeException(pExceptionRecord,
                                                    pContext,
                                                    pExceptionRecord->ExceptionCode,
                                                    pThread))
    {
        return ExceptionContinueExecution;
    }

    // Otherwise, run our last chance exception logic
    ATTACH_ACTION action;
    action = ATTACH_NO;

    if (CORDebuggerAttached() || jitAttachRequested)
    {
        LOG((LF_CORDB, LL_INFO10000, "D::BEH ... debugger attached.\n"));

        Thread *thread = g_pEEInterface->GetThread();
        _ASSERTE((thread != NULL) && (thread == pThread));

        // ExceptionFlags is 0 for continuable, EXCEPTION_NONCONTINUABLE otherwise. Note that if we don't have an
        // exception record, then we assume this is a non-continuable exception.
        bool continuable = (pExceptionRecord != NULL) && (pExceptionRecord->ExceptionFlags == 0);

        LOG((LF_CORDB, LL_INFO10000, "D::BEH ... sending exception.\n"));

        HRESULT hr = E_FAIL;

        // In the jit-attach case, lazy-init. We may be in a stack-overflow, so do it via a favor to avoid
        // using this thread's stack space.
        if (jitAttachRequested)
        {
            m_pRCThread->DoFavor((FAVORCALLBACK) LazyInitFavor, NULL);
        }

        // The only way we don't have lazy data at this point is in an OOM scenario, which
        // the debugger doesn't support.
        if (!HasLazyData())
        {
            return ExceptionContinueSearch;
        }


        // In Whidbey, we used to set the filter CONTEXT when we hit an unhandled exception while doing
        // mixed-mode debugging.  This helps the debugger walk the stack since it can skip the leaf
        // portion of the stack (including stack frames in the runtime) and start the stackwalk at the
        // faulting stack frame.  The code to set the filter CONTEXT is in a hijack function which is only
        // used during mixed-mode debugging.
        if (m_pRCThread->GetDCB()->m_rightSideIsWin32Debugger)
        {
            GCX_COOP();

            _ASSERTE(thread->GetFilterContext() == NULL);
            thread->SetFilterContext(pExceptionInfo->ContextRecord);
        }
        EX_TRY
        {
            // We pass the attaching status to SendException so that it knows
            // whether to attach a debugger or not. We should really do the
            // attach stuff out here and not bother with the flag.
            hr = SendException(thread,
                          FALSE,
                          ((pContext != NULL) ? (SIZE_T)GetIP(pContext) : NULL),
                          ((pContext != NULL) ? (SIZE_T)GetSP(pContext) : NULL),
                          continuable,
                          !!jitAttachRequested,  // If we are JIT attaching on an unhandled exceptioin, we force
                          !!jitAttachRequested,  // the exception to be uninterceptable.
                          pExceptionInfo);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);
        if (m_pRCThread->GetDCB()->m_rightSideIsWin32Debugger)
        {
            GCX_COOP();

            thread->SetFilterContext(NULL);
        }
    }
    else
    {
        // Note: we don't do anything on NO or TERMINATE. We just return to the exception logic, which will abort the
        // app or not depending on what the CLR impl decides is appropiate.
        _ASSERTE(action == ATTACH_TERMINATE || action == ATTACH_NO);
    }

    return ExceptionContinueSearch;
}

//
// NotifyUserOfFault notifies the user of a fault (unhandled exception
// or user breakpoint) in the process, giving them the option to
// attach a debugger or terminate the application.
//
int Debugger::NotifyUserOfFault(bool userBreakpoint, DebuggerLaunchSetting dls)
{
    LOG((LF_CORDB, LL_INFO1000000, "D::NotifyUserOfFault\n"));

    CONTRACTL
    {
        NOTHROW;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    int result = IDCANCEL;

    if (!CORDebuggerAttached())
    {
        DWORD pid;
        DWORD tid;

        pid = GetCurrentProcessId();
        tid = GetCurrentThreadId();

        DWORD flags = 0;
        UINT resIDMessage = 0;

        if (userBreakpoint)
        {
            resIDMessage = IDS_DEBUG_USER_BREAKPOINT_MSG;
            flags |= MB_ABORTRETRYIGNORE | MB_ICONEXCLAMATION;
        }
        else
        {
            resIDMessage = IDS_DEBUG_UNHANDLED_EXCEPTION_MSG;
            flags |= MB_OKCANCEL | MB_ICONEXCLAMATION;
        }

        {
            // Another potential hang. This may get run on the helper if we have a stack overflow.
            // Hopefully the odds of 1 thread hitting a stack overflow while another is stuck holding the heap
            // lock is very small.
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

            result = MessageBox(resIDMessage, IDS_DEBUG_SERVICE_CAPTION,
                flags, TRUE, TRUE, pid, pid, tid, tid);
        }
    }

    LOG((LF_CORDB, LL_INFO1000000, "D::NotifyUserOfFault left\n"));
    return result;
}


// Proxy for ShouldAttachDebugger
struct ShouldAttachDebuggerParams {
    Debugger*                   m_pThis;
    bool                        m_fIsUserBreakpoint;
    Debugger::ATTACH_ACTION     m_retval;
};

// This is called by the helper thread
void ShouldAttachDebuggerStub(ShouldAttachDebuggerParams * p)
{
    WRAPPER_NO_CONTRACT;

    p->m_retval = p->m_pThis->ShouldAttachDebugger(p->m_fIsUserBreakpoint);
}

// This gets called just like the normal version, but it sends the call over to the helper thread
Debugger::ATTACH_ACTION Debugger::ShouldAttachDebuggerProxy(bool fIsUserBreakpoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    if (!HasLazyData())
    {
        DebuggerLockHolder lockHolder(this);
        HRESULT hr = LazyInitWrapper();
        if (FAILED(hr))
        {
            // We already stress logged this case.
            return ATTACH_NO;
        }
    }


    if (!IsGuardPageGone())
        return ShouldAttachDebugger(fIsUserBreakpoint);

    ShouldAttachDebuggerParams p;
    p.m_pThis = this;
    p.m_fIsUserBreakpoint = fIsUserBreakpoint;

    LOG((LF_CORDB, LL_INFO1000000, "D::SADProxy\n"));
    m_pRCThread->DoFavor((FAVORCALLBACK) ShouldAttachDebuggerStub, &p);
    LOG((LF_CORDB, LL_INFO1000000, "D::SADProxy return %d\n", p.m_retval));

    return p.m_retval;
}

//---------------------------------------------------------------------------------------
// Do policy to determine if we should attach a debugger.
//
// Arguments:
//    fIsUserBreakpoint - true iff this is in response to a user-breakpoint, else false.
//
// Returns:
//    Action to perform based off policy.
//    ATTACH_NO if a debugger is already attached.
Debugger::ATTACH_ACTION Debugger::ShouldAttachDebugger(bool fIsUserBreakpoint)
{
    CONTRACTL
    {
        NOTHROW;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
        MODE_ANY;
    }
    CONTRACTL_END;


    LOG((LF_CORDB, LL_INFO1000000, "D::SAD\n"));

    // If the debugger is already attached, not necessary to re-attach
    if (CORDebuggerAttached())
    {
        return ATTACH_NO;
    }

    // Check if the user has specified a seting in the registry about what he wants done when an unhandled exception
    // occurs.
    DebuggerLaunchSetting dls = GetDbgJITDebugLaunchSetting();


    if (dls == DLS_ATTACH_DEBUGGER)
    {
        return ATTACH_YES;
    }
    else
    {
        // Only ask the user once if they wish to attach a debugger.  This is because LastChanceManagedException can be called
        // twice, which causes ShouldAttachDebugger to be called twice, which causes the user to have to answer twice.
        static BOOL s_fHasAlreadyAsked = FALSE;
        static ATTACH_ACTION s_action;


        // This lock is also part of the above workaround.
        // Must go to preemptive to take this lock since we'll trigger down the road.
        GCX_PREEMP();
        DebuggerLockHolder lockHolder(this);

        // We always want to ask about user breakpoints!
        if (!s_fHasAlreadyAsked || fIsUserBreakpoint)
        {
            if (!fIsUserBreakpoint)
                s_fHasAlreadyAsked = TRUE;

            // While we could theoretically run into a deadlock if another thread
            // which acquires the debugger lock in cooperative GC mode is blocked
            // on this thread while it is running arbitrary user code out of the
            // MessageBox message pump, given that this codepath will only be used
            // on Win9x and that the chances of this happenning are quite slim,
            // for Whidbey a GCViolation is acceptable.
            CONTRACT_VIOLATION(GCViolation);

            // Ask the user if they want to attach
            int iRes = NotifyUserOfFault(fIsUserBreakpoint, dls);

            // If it's a user-defined breakpoint, they must hit Retry to launch
            // the debugger.  If it's an unhandled exception, user must press
            // Cancel to attach the debugger.
            if ((iRes == IDCANCEL) || (iRes == IDRETRY))
                s_action = ATTACH_YES;

            else if ((iRes == IDABORT) || (iRes == IDOK))
                s_action = ATTACH_TERMINATE;

            else
                s_action = ATTACH_NO;
        }

        // dbgLockHolder goes out of scope - implicit Release
        return s_action;
    }
}


//---------------------------------------------------------------------------------------
// SendUserBreakpoint is called by Runtime threads to send that they've hit
// a user breakpoint to the Right Side.
//
// Parameters:
//    thread - managed thread that the breakpoint is on
//
// Notes:
//    A user breakpoint is generally triggered by a call to System.Diagnostics.Debugger.Break.
//    This can be very common. VB's 'stop' statement compiles to a Debugger.Break call.
//    Some other CLR facilities (MDAs) may call this directly too.
//
//    This may trigger a Jit attach.
//    If the debugger is already attached, this will issue a step-out so that the UserBreakpoint
//    appears to come from the callsite.
void Debugger::SendUserBreakpoint(Thread * thread)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;

        PRECONDITION(thread != NULL);
        PRECONDITION(thread == ::GetThreadNULLOk());
    }
    CONTRACTL_END;


#ifdef _DEBUG
    // For testing Watson, we want a consistent way to be able to generate a
    // Fatal Execution Error
    // So we have a debug-only knob in this particular managed call that can be used
    // to artificially inject the error.
    // This is only for testing.
    static int fDbgInjectFEE = -1;

    if (fDbgInjectFEE == -1)
        fDbgInjectFEE = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgInjectFEE);

    if (fDbgInjectFEE)
    {
        STRESS_LOG0(LF_CORDB, LL_INFO10000, "Debugger posting bogus FEE b/c knob DbgInjectFEE is set.\n");
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        // These never return.
    }
#endif

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    // UserBreakpoint behaves differently if we're under a debugger vs. a jit-attach.
    // If we're under the debugger, it does an additional step-out to get us back to the call site.

    // If already attached, then do a step-out and send the userbreak event.
    if (CORDebuggerAttached())
    {
        // A debugger is already attached, so setup a DebuggerUserBreakpoint controller to get us out of the helper
        // that got us here. The DebuggerUserBreakpoint will call AttachDebuggerForBreakpoint for us when we're out
        // of the helper. The controller will delete itself when its done its work.
        DebuggerUserBreakpoint::HandleDebugBreak(thread);
        return;
     }

    ATTACH_ACTION dbgAction = ShouldAttachDebugger(true);

    // No debugger is attached. Consider a JIT attach.
    // This will do ShouldAttachDebugger() and wait for the results.
    // - It may terminate if the user requested that.
    // - It may do a full jit-attach.
    if (dbgAction == ATTACH_YES)
    {
        JitAttach(thread, NULL, TRUE, FALSE);
    }
    else if (dbgAction == ATTACH_TERMINATE)
    {
        // ATTACH_TERMINATE indicates the the user wants to terminate the app.
        LOG((LF_CORDB, LL_INFO10000, "D::SUB: terminating this process due to user request\n"));

        // Should this go through the host?
        TerminateProcess(GetCurrentProcess(), 0);
        _ASSERTE(!"Should never reach this point.");
    }
    else
    {
        _ASSERTE(dbgAction == ATTACH_NO);
    }

    if (CORDebuggerAttached())
    {
        // On jit-attach, we just send the UserBreak event. Don't do an extra step-out.
        SendUserBreakpointAndSynchronize(thread);
    }
    else if (IsDebuggerPresent())
    {
        DebugBreak();
    }
}


// void Debugger::ThreadCreated():  ThreadCreated is called when
// a new Runtime thread has been created, but before its ever seen
// managed code.  This is a callback invoked by the EE into the Debugger.
// This will create a DebuggerThreadStarter patch, which will set
// a patch at the first instruction in the managed code.  When we hit
// that patch, the DebuggerThreadStarter will invoke ThreadStarted, below.
//
// Thread* pRuntimeThread:  The EE Thread object representing the
//      runtime thread that has just been created.
void Debugger::ThreadCreated(Thread* pRuntimeThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // @@@
    // This function implements the DebugInterface. But it is also called from Attach
    // logic internally.
    //

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO100, "D::TC: thread created for 0x%x. ******\n",
         GetThreadIdHelper(pRuntimeThread)));

    // Sanity check the thread.
    _ASSERTE(pRuntimeThread != NULL);
    _ASSERTE(pRuntimeThread->GetThreadId() != 0);


    // Create a thread starter and enable its WillEnterManaged code
    // callback. This will cause the starter to trigger once the
    // thread has hit managed code, which will cause
    // Debugger::ThreadStarted() to be called.  NOTE: the starter will
    // be deleted automatically when its done its work.
    DebuggerThreadStarter *starter = new (interopsafe, nothrow) DebuggerThreadStarter(pRuntimeThread);

    if (starter == NULL)
    {
        CORDBDebuggerSetUnrecoverableWin32Error(this, 0, false);
        return;
    }

    starter->EnableTraceCall(LEAF_MOST_FRAME);
}


// void Debugger::ThreadStarted():  ThreadStarted is called when
// a new Runtime thread has reached its first managed code. This is
// called by the DebuggerThreadStarter patch's SendEvent method.
//
// Thread* pRuntimeThread:  The EE Thread object representing the
//      runtime thread that has just hit managed code.
void Debugger::ThreadStarted(Thread* pRuntimeThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // @@@
    // This method implemented DebugInterface but it is also called from Controller

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO100, "D::TS: thread attach : ID=%#x AD:%#x\n",
         GetThreadIdHelper(pRuntimeThread), pRuntimeThread->GetDomain()));

    // We just need to send a VMPTR_Thread. The RS will get everything else it needs from DAC.
    //

        _ASSERTE((g_pEEInterface->GetThread() &&
                 !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) ||
                 g_fInControlC);
        _ASSERTE(ThreadHoldsLock());

    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,
                 DB_IPCE_THREAD_ATTACH,
                 pRuntimeThread,
                 pRuntimeThread->GetDomain());


    m_pRCThread->SendIPCEvent();

        //
        // Well, if this thread got created _after_ we started sync'ing
        // then its Runtime thread flags don't have the fact that there
        // is a debug suspend pending. We need to call over to the
        // Runtime and set the flag in the thread now...
        //
        if (m_trappingRuntimeThreads)
    {
            g_pEEInterface->MarkThreadForDebugSuspend(pRuntimeThread);
    }
}


//---------------------------------------------------------------------------------------
//
// DetachThread is called by Runtime threads when they are completing
// their execution and about to be destroyed.
//
// Arguments:
//    pRuntimeThread - Pointer to the runtime's thread object to detach.
//
// Return Value:
//    None
//
//---------------------------------------------------------------------------------------
void Debugger::DetachThread(Thread *pRuntimeThread)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    if (m_ignoreThreadDetach)
    {
        return;
    }

    _ASSERTE (pRuntimeThread != NULL);


    LOG((LF_CORDB, LL_INFO100, "D::DT: thread detach : ID=%#x AD:%#x.\n",
         GetThreadIdHelper(pRuntimeThread), pRuntimeThread->GetDomain()));


    // We may be killing a thread before the Thread-starter fired.
    // So check (and cancel) any outstanding thread-starters.
    // If we don't, this old thread starter may conflict w/ a new thread-starter
    // if AppDomains or EE Thread's get recycled.
    DebuggerController::CancelOutstandingThreadStarter(pRuntimeThread);

    // Controller lock is bigger than debugger lock.
    // Don't take debugger lock before the CancelOutStandingThreadStarter function.
    SENDIPCEVENT_BEGIN(this, pRuntimeThread);

    if (CORDebuggerAttached())
    {
        // Send a detach thread event to the Right Side.
        DebuggerIPCEvent * pEvent = m_pRCThread->GetIPCEventSendBuffer();

        InitIPCEvent(pEvent,
                     DB_IPCE_THREAD_DETACH,
                     pRuntimeThread,
                     pRuntimeThread->GetDomain());

        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        TrapAllRuntimeThreads();

        // This prevents a race condition where we blocked on the Lock()
        // above while another thread was sending an event and while we
        // were blocked the debugger suspended us and so we wouldn't be
        // resumed after the suspension about to happen below.
        pRuntimeThread->ResetThreadStateNC(Thread::TSNC_DebuggerUserSuspend);
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::DT: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;
}


//
// SuspendComplete is called when the last Runtime thread reaches a safe point in response to having its trap flags set.
// This may be called on either the real helper thread or someone doing helper thread duty.
//
// It could also be called for sending garbage collection events (see DebuggerRCThread::SendIPCEvent for more about the
// thread mode associated with the events)
//
BOOL Debugger::SuspendComplete(bool isEESuspendedForGC)
{
    CONTRACTL
    {
        NOTHROW;
        if (isEESuspendedForGC) { GC_NOTRIGGER; } else { GC_TRIGGERS; }
        // This will is conceptually mode-cooperative.
        // But we haven't marked the runtime as stopped yet (m_stopped), so the contract
        // subsystem doesn't realize it yet.
        DISABLED(MODE_COOPERATIVE);
    }
    CONTRACTL_END;

    // @@@
    // Call from RCThread::MainLoop and TemporaryHelperThreadMainLoop.
    // when all threads suspended. Can happen on managed thread or helper thread.
    // If happen on managed thread, it must be doing the helper thread duty.
    //

    _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach);

    // We should be holding debugger lock m_mutex.
    _ASSERTE(ThreadHoldsLock());

    // We can't throw here (we're in the middle of the runtime suspension logic).
    // But things below us throw. So we catch the exception, but then what state are we in?

    if (!isEESuspendedForGC) {_ASSERTE((!g_pEEInterface->GetThread() || !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) || g_fInControlC); }
    if (!isEESuspendedForGC) { _ASSERTE(ThisIsHelperThreadWorker()); }

    STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::SC: suspension complete\n");

    // We have suspended runtime.

    // We're stopped now. Marking m_stopped allows us to use MODE_COOPERATIVE contracts.
    if (isEESuspendedForGC)
    {
        _ASSERTE(!m_stopped);
    }
    else
    {
        _ASSERTE(!m_stopped && m_trappingRuntimeThreads);
    }
    m_stopped = true;


    // Send the sync complete event to the Right Side.
    {
        // If we fail to send the SyncComplete, what do we do?
        CONTRACT_VIOLATION(ThrowsViolation);

        SendSyncCompleteIPCEvent(isEESuspendedForGC); // sets m_stopped = true...
    }

    // Everything in the next scope is meant to mimic what we do UnlockForEventSending minus EnableEventHandling.
    // We do the EEH part when we get the Continue event.
    {
#ifdef _DEBUG
        //_ASSERTE(m_tidLockedForEventSending == GetCurrentThreadId());
        m_tidLockedForEventSending = 0;
#endif

        //
        // Event handling is re-enabled by the RCThread in response to a
        // continue message from the Right Side.

    }

    // @todo - what should we do if this function failed?
    return TRUE;
}




//---------------------------------------------------------------------------------------
//
// Debugger::SendCreateAppDomainEvent - notify the RS of an AppDomain
//
// Arguments:
//    pRuntimeAppdomain - pointer to the AppDomain
//
// Return Value:
//    None
//
// Notes:
//    This is used to notify the debugger of either a newly created
//    AppDomain (when fAttaching is FALSE) or of existing AppDomains
//    at attach time (fAttaching is TRUE).  In both cases, this should
//    be called before any LoadModule/LoadAssembly events are sent for
//    this domain.  Otherwise the RS will get an event for an AppDomain
//    it doesn't recognize and ASSERT.
//
//    For the non-attach case this means there is no need to enumerate
//    the assemblies/modules in an AppDomain after sending this event
//    because we know there won't be any.
//

void Debugger::SendCreateAppDomainEvent(AppDomain * pRuntimeAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    STRESS_LOG1(LF_CORDB, LL_INFO10000, "D::SCADE: AppDomain creation:%#08x\n",
            pRuntimeAppDomain);



    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);



    // We may have detached while waiting in LockForEventSending,
    // in which case we can't send the event.
    if (CORDebuggerAttached())
    {
        // Send a create appdomain event to the Right Side.
        DebuggerIPCEvent * pEvent = m_pRCThread->GetIPCEventSendBuffer();

        InitIPCEvent(pEvent,
                     DB_IPCE_CREATE_APP_DOMAIN,
                     pThread,
                     pRuntimeAppDomain);

        // Only send a pointer to the AppDomain, the RS will get everything else via DAC.
        pEvent->AppDomainData.vmAppDomain.SetRawPtr(pRuntimeAppDomain);
        m_pRCThread->SendIPCEvent();

        TrapAllRuntimeThreads();
    }

    // Let other Runtime threads handle their events.
    SENDIPCEVENT_END;

}


//
// LoadAssembly is called when a new Assembly gets loaded.
//
void Debugger::LoadAssembly(DomainAssembly * pDomainAssembly)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO100, "D::LA: Load Assembly Asy:0x%p AD:0x%p which:%ls\n",
        pDomainAssembly, pDomainAssembly->GetAppDomain(), pDomainAssembly->GetAssembly()->GetDebugName() ));

    if (!CORDebuggerAttached())
    {
        return;
    }

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread)


    if (CORDebuggerAttached())
    {
        // Send a load assembly event to the Right Side.
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce,
                     DB_IPCE_LOAD_ASSEMBLY,
                     pThread,
                     pDomainAssembly->GetAppDomain());

        ipce->AssemblyData.vmDomainAssembly.SetRawPtr(pDomainAssembly);

        m_pRCThread->SendIPCEvent();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::LA: Skipping SendIPCEvent because RS detached."));
    }

    // Stop all Runtime threads
    if (CORDebuggerAttached())
    {
        TrapAllRuntimeThreads();
    }

    SENDIPCEVENT_END;
}



//
// UnloadAssembly is called when a Runtime thread unloads an assembly.
//
void Debugger::UnloadAssembly(DomainAssembly * pDomainAssembly)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO100, "D::UA: Unload Assembly Asy:0x%p AD:0x%p which:%ls\n",
         pDomainAssembly, pDomainAssembly->GetAppDomain(), pDomainAssembly->GetAssembly()->GetDebugName() ));

    Thread *thread = g_pEEInterface->GetThread();
    // Note that the debugger lock is reentrant, so we may or may not hold it already.
    SENDIPCEVENT_BEGIN(this, thread);

    // Send the unload assembly event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

    InitIPCEvent(ipce,
                 DB_IPCE_UNLOAD_ASSEMBLY,
                 thread,
                 pDomainAssembly->GetAppDomain());
    ipce->AssemblyData.vmDomainAssembly.SetRawPtr(pDomainAssembly);

    SendSimpleIPCEventAndBlock();

    // This will block on the continue
    SENDIPCEVENT_END;

}




//
// LoadModule is called when a Runtime thread loads a new module and a debugger
// is attached.  This also includes when a domain-neutral module is "loaded" into
// a new domain.
//
// TODO: remove pszModuleName and perhaps other args.
void Debugger::LoadModule(Module* pRuntimeModule,
                          LPCWSTR pszModuleName, // module file name.
                          DWORD dwModuleName, // length of pszModuleName in chars, not including null.
                          Assembly *pAssembly,
                          AppDomain *pAppDomain,
                          DomainFile *  pDomainFile,
                          BOOL fAttaching)
{

    CONTRACTL
    {
        NOTHROW; // not protected for Throws.
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // @@@@
    // Implement DebugInterface but can be called internally as well.
    // This can be called by EE loading module or when we are attaching called by IteratingAppDomainForAttaching
    //
    _ASSERTE(!fAttaching);

    if (CORDBUnrecoverableError(this))
        return;

    // If this is a dynamic module, then it's part of a multi-module assembly. The manifest
    // module within the assembly contains metadata for all the module names in the assembly.
    // When a new dynamic module is created, the manifest module's metadata is updated to
    // include the new module (see code:Assembly.CreateDynamicModule).
    // So we need to update the RS's copy of the metadata. One place the manifest module's
    // metadata gets used is in code:DacDbiInterfaceImpl.GetModuleSimpleName
    //
    // See code:ReflectionModule.CaptureModuleMetaDataToMemory for why we send the metadata-refresh here.
    if (pRuntimeModule->IsReflection() && !pRuntimeModule->IsManifest() && !fAttaching)
    {
        HRESULT hr = S_OK;
        EX_TRY
        {
            // The loader lookups may throw or togggle GC mode, so do them inside a TRY/Catch and
            // outside any debugger locks.
            Module * pManifestModule = pRuntimeModule->GetAssembly()->GetManifestModule();

            _ASSERTE(pManifestModule != pRuntimeModule);
            _ASSERTE(pManifestModule->IsManifest());
            _ASSERTE(pManifestModule->GetAssembly() == pRuntimeModule->GetAssembly());

            DomainFile * pManifestDomainFile = pManifestModule->GetDomainFile();

            DebuggerLockHolder dbgLockHolder(this);

            // Raise the debug event.
            // This still tells the debugger that the manifest module metadata is invalid and needs to
            // be refreshed.
            DebuggerIPCEvent eventMetadataUpdate;
            InitIPCEvent(&eventMetadataUpdate, DB_IPCE_METADATA_UPDATE, NULL, pAppDomain);

            eventMetadataUpdate.MetadataUpdateData.vmDomainFile.SetRawPtr(pManifestDomainFile);

            SendRawEvent(&eventMetadataUpdate);
        }
        EX_CATCH_HRESULT(hr);
        SIMPLIFYING_ASSUMPTION_SUCCEEDED(hr);
    }


    DebuggerModule * module = NULL;

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);



    DebuggerIPCEvent* ipce = NULL;

    // Don't create new record if already loaded. We do still want to send the ModuleLoad event, however.
    // The RS has logic to ignore duplicate ModuleLoad events. We have to send what could possibly be a dup, though,
    // due to some really nasty issues with getting proper assembly and module load events from the loader when dealing
    // with shared assemblies.
    module = LookupOrCreateModule(pDomainFile);
    _ASSERTE(module != NULL);


    // During a real LoadModule event, debugger can change jit flags.
    // Can't do this during a fake event sent on attach.
    // This is cleared after we send the LoadModule event.
    module->SetCanChangeJitFlags(true);


    // @dbgtodo  inspection - Check whether the DomainFile we get is consistent with the Module and AppDomain we get.
    // We should simply things when we actually get rid of DebuggerModule, possibly by just passing the
    // DomainFile around.
    _ASSERTE(module->GetDomainFile()    == pDomainFile);
    _ASSERTE(module->GetAppDomain()     == pDomainFile->GetAppDomain());
    _ASSERTE(module->GetRuntimeModule() == pDomainFile->GetModule());

    // Send a load module event to the Right Side.
    ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce,DB_IPCE_LOAD_MODULE, pThread, pAppDomain);

    ipce->LoadModuleData.vmDomainFile.SetRawPtr(pDomainFile);

    m_pRCThread->SendIPCEvent();

    {
        // Stop all Runtime threads
        HRESULT hr = S_OK;
        EX_TRY
        {
            TrapAllRuntimeThreads();
        }
        EX_CATCH_HRESULT(hr); // @dbgtodo  synchronization - catch exception and go on to restore state.
        // Synchronization feature crew needs to figure out what happens to TrapAllRuntimeThreads().
    }

    SENDIPCEVENT_END;

    // need to update pdb stream for SQL passed in pdb stream
    // regardless attach or not.
    //
    if (pRuntimeModule->IsIStream())
    {
        // Just ignore failures. Caller was just sending a debug event and we don't
        // want that to interop non-debugging functionality.
        HRESULT hr = S_OK;
        EX_TRY
        {
            SendUpdateModuleSymsEventAndBlock(pRuntimeModule, pAppDomain);
        }
        EX_CATCH_HRESULT(hr);
    }

    // Now that we're done with the load module event, can no longer change Jit flags.
    module->SetCanChangeJitFlags(false);
}


//---------------------------------------------------------------------------------------
//
// Special LS-only notification that a module has reached the FILE_LOADED level. For now
// this is only useful to bind breakpoints in generic instantiations from NGENd modules
// that we couldn't bind earlier (at LoadModule notification time) because the method
// iterator refuses to consider modules earlier than the FILE_LOADED level. Normally
// generic instantiations would have their breakpoints bound when they get JITted, but in
// the case of NGEN that may never happen, so we need to bind them here.
//
// Arguments:
//      * pRuntimeModule - Module that just loaded
//      * pAppDomain - AD into which the Module was loaded
//
// Assumptions:
//     This is called during the loading process, and blocks that process from
//     completing. The module has reached the FILE_LOADED stage, but typically not yet
//     the IsReadyForTypeLoad stage.
//

void Debugger::LoadModuleFinished(Module * pRuntimeModule, AppDomain * pAppDomain)
{
    CONTRACTL
    {
        SUPPORTS_DAC;
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    _ASSERTE(pRuntimeModule != NULL);
    _ASSERTE(pAppDomain != NULL);

    if (CORDBUnrecoverableError(this))
        return;

    // Just as an optimization, skip binding breakpoints if there's no debugger attached.
    // If a debugger attaches at some point after here, it will be able to bind patches
    // by making the request at that time. If a debugger detaches at some point after
    // here, there's no harm in having extra patches bound.
    if (!CORDebuggerAttached())
        return;

    // For now, this notification only does interesting work if the module that loaded is
    // an NGENd module, because all we care about in this notification is ensuring NGENd
    // methods get breakpoints bound on them
    if (!pRuntimeModule->HasNativeImage())
        return;

    // This notification is called just before MODULE_READY_FOR_TYPELOAD gets set. But
    // for shared modules (loaded into multiple domains), MODULE_READY_FOR_TYPELOAD has
    // already been set if this module was already loaded into an earlier domain. For
    // such cases, there's no need to bind breakpoints now because the module has already
    // been fully loaded into at least one domain, and breakpoint binding has already
    // been done for us
    if (pRuntimeModule->IsReadyForTypeLoad())
        return;

#ifdef _DEBUG
    {
        // This notification is called once the module is loaded
        DomainFile * pDomainFile = pRuntimeModule->GetDomainFile();
        _ASSERTE((pDomainFile != NULL) && (pDomainFile->GetLoadLevel() >= FILE_LOADED));
    }
#endif // _DEBUG

    // Find all IL Master patches for this module, and bind & activate their
    // corresponding slave patches.
    {
        DebuggerController::ControllerLockHolder ch;

        HASHFIND info;
        DebuggerPatchTable * pTable = DebuggerController::GetPatchTable();

        for (DebuggerControllerPatch * pMasterPatchCur = pTable->GetFirstPatch(&info);
            pMasterPatchCur != NULL;
            pMasterPatchCur = pTable->GetNextPatch(&info))
        {
            if (!pMasterPatchCur->IsILMasterPatch())
                continue;

            DebuggerMethodInfo *dmi = GetOrCreateMethodInfo(pMasterPatchCur->key.module, pMasterPatchCur->key.md);

            // Found a relevant IL master patch. Now bind all corresponding slave patches
            // that belong to this Module
            DebuggerMethodInfo::DJIIterator it;
            dmi->IterateAllDJIs(pAppDomain, pRuntimeModule, pMasterPatchCur->pMethodDescFilter, &it);
            for (; !it.IsAtEnd(); it.Next())
            {
                DebuggerJitInfo *dji = it.Current();
                _ASSERTE(dji->m_jitComplete);

                if (dji->m_encVersion != pMasterPatchCur->GetEnCVersion())
                    continue;

                // Do we already have a slave for this DJI & Controller?  If so, no need
                // to add another one
                BOOL fSlaveExists = FALSE;
                HASHFIND f;
                for (DebuggerControllerPatch * pSlavePatchCur = pTable->GetFirstPatch(&f);
                    pSlavePatchCur != NULL;
                    pSlavePatchCur = pTable->GetNextPatch(&f))
                {
                    if (pSlavePatchCur->IsILSlavePatch() &&
                        (pSlavePatchCur->GetDJI() == dji) &&
                        (pSlavePatchCur->controller == pMasterPatchCur->controller))
                    {
                        fSlaveExists = TRUE;
                        break;
                    }
                }

                if (fSlaveExists)
                    continue;

                pMasterPatchCur->controller->AddBindAndActivateILSlavePatch(pMasterPatchCur, dji);
            }
        }
    }
}


// Send the raw event for Updating symbols. Debugger must query for contents from out-of-process
//
// Arguments:
//   pRuntimeModule - required, module to send symbols for. May be domain neutral.
//   pAppDomain - required, appdomain that module is in.
//
// Notes:
//   This is just a ping event. Debugger must query for actual symbol contents.
//   This keeps the launch + attach cases identical.
//   This just sends the raw event and does not synchronize the runtime.
//   Use code:Debugger.SendUpdateModuleSymsEventAndBlock for that.
void Debugger::SendRawUpdateModuleSymsEvent(Module *pRuntimeModule, AppDomain *pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;

        PRECONDITION(ThreadHoldsLock());

        // Debugger must have been attached to get us to this point.
        // We hold the Debugger-lock, so debugger could not have detached from
        // underneath us either.
        PRECONDITION(CORDebuggerAttached());
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this))
        return;

    // This event is used to trigger the ICorDebugManagedCallback::UpdateModuleSymbols
    // callback.  That callback is defined to pass a PDB stream, and so we still use this
    // only for legacy compatibility reasons when we've actually got PDB symbols.
    // New clients know they must request a new symbol reader after ClassLoad events.
    if (pRuntimeModule->GetInMemorySymbolStream() == NULL)
        return; // Non-PDB symbols

    DebuggerModule* module = LookupOrCreateModule(pRuntimeModule, pAppDomain);
    PREFIX_ASSUME(module != NULL);

    DebuggerIPCEvent* ipce = NULL;
    ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce, DB_IPCE_UPDATE_MODULE_SYMS,
                 g_pEEInterface->GetThread(),
                 pAppDomain);

    ipce->UpdateModuleSymsData.vmDomainFile.SetRawPtr((module ? module->GetDomainFile() : NULL));

    m_pRCThread->SendIPCEvent();
}

//
// UpdateModuleSyms is called when the symbols for a module need to be
// sent to the Right Side because they've changed.
//
// Arguments:
//   pRuntimeModule - required, module to send symbols for. May be domain neutral.
//   pAppDomain - required, appdomain that module is in.
//
//
// Notes:
//    This will send the event (via code:Debugger.SendRawUpdateModuleSymsEvent) and then synchronize
//    the runtime waiting for a continue.
//
//    This should only be called in cases where we reasonably expect to send symbols.
//    However, this may not send symbols if the symbols aren't available.
void Debugger::SendUpdateModuleSymsEventAndBlock(Module* pRuntimeModule, AppDomain *pAppDomain)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (CORDBUnrecoverableError(this) || !CORDebuggerAttached())
    {
        return;
    }

    CGrowableStream * pStream = pRuntimeModule->GetInMemorySymbolStream();
    LOG((LF_CORDB, LL_INFO10000, "D::UMS: update module syms RuntimeModule:0x%08x CGrowableStream:0x%08x\n", pRuntimeModule, pStream));
    if (pStream == NULL)
    {
        // No in-memory Pdb available.
        STRESS_LOG1(LF_CORDB, LL_INFO10000, "No syms available %p", pRuntimeModule);
        return;
    }

    SENDIPCEVENT_BEGIN(this, g_pEEInterface->GetThread()); // toggles to preemptive

    // Actually send the event
    if (CORDebuggerAttached())
    {
        SendRawUpdateModuleSymsEvent(pRuntimeModule, pAppDomain);
        TrapAllRuntimeThreads();
    }

    SENDIPCEVENT_END;
}


//
// UnloadModule is called by the Runtime for each module (including shared ones)
// in an AppDomain that is being unloaded, when a debugger is attached.
// In the EE, a module may be domain-neutral and therefore shared across all AppDomains.
// We abstract this detail away in the Debugger and consider each such EE module to correspond
// to multiple "Debugger Module" instances (one per AppDomain).
// Therefore, this doesn't necessarily mean the runtime is unloading the module, just
// that the Debugger should consider it's (per-AppDomain) DebuggerModule to be unloaded.
//
void Debugger::UnloadModule(Module* pRuntimeModule,
                            AppDomain *pAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // @@@@
    // implements DebugInterface.
    // can only called by EE on Module::NotifyDebuggerUnload
    //

    if (CORDBUnrecoverableError(this))
        return;



    LOG((LF_CORDB, LL_INFO100, "D::UM: unload module Mod:%#08x AD:%#08x runtimeMod:%#08x modName:%ls\n",
         LookupOrCreateModule(pRuntimeModule, pAppDomain), pAppDomain, pRuntimeModule, pRuntimeModule->GetDebugName()));


    Thread *thread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, thread);

    if (CORDebuggerAttached())
    {

        DebuggerModule* module = LookupOrCreateModule(pRuntimeModule, pAppDomain);
        if (module == NULL)
        {
            LOG((LF_CORDB, LL_INFO100, "D::UM: module already unloaded AD:%#08x runtimeMod:%#08x modName:%ls\n",
                 pAppDomain, pRuntimeModule, pRuntimeModule->GetDebugName()));
            goto LExit;
        }
        _ASSERTE(module != NULL);

        STRESS_LOG6(LF_CORDB, LL_INFO10000,
            "D::UM: Unloading RTMod:%#08x (DomFile: %#08x, IsISStream:%#08x); DMod:%#08x(RTMod:%#08x DomFile: %#08x)\n",
            pRuntimeModule, pRuntimeModule->GetDomainFile(), pRuntimeModule->IsIStream(),
            module, module->GetRuntimeModule(), module->GetDomainFile());

        // Note: the appdomain the module was loaded in must match the appdomain we're unloading it from. If it doesn't,
        // then we've either found the wrong DebuggerModule in LookupModule or we were passed bad data.
        _ASSERTE(module->GetAppDomain() == pAppDomain);

        // Send the unload module event to the Right Side.
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce, DB_IPCE_UNLOAD_MODULE, thread, pAppDomain);
        ipce->UnloadModuleData.vmDomainFile.SetRawPtr((module ? module->GetDomainFile() : NULL));
        ipce->UnloadModuleData.debuggerAssemblyToken.Set(pRuntimeModule->GetClassLoader()->GetAssembly());
        m_pRCThread->SendIPCEvent();

        //
        // Cleanup the module (only for resources consumed when a debugger is attached)
        //

        // Remove all patches that apply to this module/AppDomain combination
        AppDomain* domainToRemovePatchesIn = NULL;  // all domains by default

        // Note that we'll explicitly NOT delete DebuggerControllers, so that
        // the Right Side can delete them later.
        DebuggerController::RemovePatchesFromModule(pRuntimeModule, domainToRemovePatchesIn);

        // Deactive all JMC functions in this module.  We don't do this for shared assemblies
        // because JMC status is not maintained on a per-AppDomain basis and we don't
        // want to change the JMC behavior of the module in other domains.
        LOG((LF_CORDB, LL_EVERYTHING, "Setting all JMC methods to false:\n"));
        DebuggerDataLockHolder debuggerDataLockHolder(this);
        DebuggerMethodInfoTable * pTable = GetMethodInfoTable();
        if (pTable != NULL)
        {
            HASHFIND info;

            for (DebuggerMethodInfo *dmi = pTable->GetFirstMethodInfo(&info);
                dmi != NULL;
                dmi = pTable->GetNextMethodInfo(&info))
            {
                if (dmi->m_module == pRuntimeModule)
                {
                    dmi->SetJMCStatus(false);
                }
            }
        }
        LOG((LF_CORDB, LL_EVERYTHING, "Done clearing JMC methods!\n"));

        // Delete the Left Side representation of the module.
        if (m_pModules != NULL)
        {
            DebuggerDataLockHolder chInfo(this);
            m_pModules->RemoveModule(pRuntimeModule, pAppDomain);
        }

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::UM: Skipping SendIPCEvent because RS detached."));
    }

LExit:
    SENDIPCEVENT_END;
}

// Called when this module is completely gone from ALL AppDomains, regardless of
// whether a debugger is attached.
// This is normally not called only domain-neutral assemblies because they can't be unloaded.
// However, it may be called if the loader fails to completely load a domain-neutral assembly.
void Debugger::DestructModule(Module *pModule)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO100, "D::DM: destruct module runtimeMod:%#08x modName:%ls\n",
         pModule, pModule->GetDebugName()));

    // @@@
    // Implements DebugInterface.
    // It is called for Module::Destruct. We do not need to send any IPC event.

    DebuggerLockHolder dbgLockHolder(this);

    // We should have removed all patches at AD unload time (or detach time if the
    // debugger detached).
    _ASSERTE( !DebuggerController::ModuleHasPatches(pModule) );

    // Do module clean-up that applies even when no debugger is attached.
    // Ideally, we might like to do this cleanup more eagerly and detministically,
    // but we don't currently get any early AD unload callback from the loader
    // when no debugger is attached.  Perhaps we should make the loader
    // call this callback earlier.
    RemoveModuleReferences(pModule);
}


// Internal helper to remove all the DJIs / DMIs and other references for a given Module.
// If we don't remove the DJIs / DMIs, then we're subject to recycling bugs because the underlying
// MethodDescs will get removed. Thus we'll look up a new MD and it will pull up an old DMI that matched
// the old MD. Now the DMI and MD are out of sync and it's downhill from there.
// Note that DMIs may be used (and need cleanup) even when no debugger is attached.
void Debugger::RemoveModuleReferences( Module* pModule )
{
    _ASSERTE( ThreadHoldsLock() );

    // We want to remove all references to the module from the various
    // tables.  It's not just possible, but probable, that the module
    // will be re-loaded at the exact same address, and in that case,
    // we'll have piles of entries in our DJI table that mistakenly
    // match this new module.
    // Note that this doesn't apply to domain neutral assemblies, that only
    // get unloaded when the process dies.  We won't be reclaiming their
    // DJIs/patches b/c the process is going to die, so we'll reclaim
    // the memory when the various hashtables are unloaded.

    if (m_pMethodInfos != NULL)
    {
        HRESULT hr = S_OK;
        if (!HasLazyData())
        {
            hr = LazyInitWrapper();
        }

        if (SUCCEEDED(hr))
        {
            DebuggerDataLockHolder debuggerDataLockHolder(this);

            m_pMethodInfos->ClearMethodsOfModule(pModule);

            // DebuggerDataLockHolder out of scope - release implied
        }
    }
}

//---------------------------------------------------------------------------------------
//
// SendClassLoadUnloadEvent - notify the RS of a class either loading or unloading.
//
// Arguments:
//
//    fAttaching - true if a debugger is in the process of attaching
//
// Return Value:
//    None
//
//---------------------------------------------------------------------------------------
void Debugger::SendClassLoadUnloadEvent (mdTypeDef classMetadataToken,
                                         DebuggerModule * pClassDebuggerModule,
                                         Assembly *pAssembly,
                                         AppDomain *pAppDomain,
                                         BOOL fIsLoadEvent)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;


    LOG((LF_CORDB,LL_INFO10000, "D::SCLUE: Tok:0x%x isLoad:0x%x Mod:%#08x AD:%#08x\n",
        classMetadataToken, fIsLoadEvent, pClassDebuggerModule, pAppDomain));

    DebuggerIPCEvent * pEvent = m_pRCThread->GetIPCEventSendBuffer();

    BOOL fIsReflection = pClassDebuggerModule->GetRuntimeModule()->IsReflection();

    if (fIsLoadEvent == TRUE)
    {
        // We need to update Metadata before Symbols (since symbols depend on metadata)
        // It's debatable which needs to come first: Class Load or Sym update.
        // V1.1 sent Sym Update first so that binding at the class load has the latest symbols.
        // However, The Class Load may need to be in sync with updating new metadata,
        // and that has to come before the Sym update.
        InitIPCEvent(pEvent, DB_IPCE_LOAD_CLASS, g_pEEInterface->GetThread(), pAppDomain);

        pEvent->LoadClass.classMetadataToken = classMetadataToken;
        pEvent->LoadClass.vmDomainFile.SetRawPtr((pClassDebuggerModule ? pClassDebuggerModule->GetDomainFile() : NULL));
        pEvent->LoadClass.classDebuggerAssemblyToken.Set(pAssembly);


        // For class loads in dynamic modules, RS knows that the metadata has now grown and is invalid.
        // RS will re-fetch new metadata from out-of-process.
    }
    else
    {
        InitIPCEvent(pEvent, DB_IPCE_UNLOAD_CLASS, g_pEEInterface->GetThread(), pAppDomain);

        pEvent->UnloadClass.classMetadataToken = classMetadataToken;
        pEvent->UnloadClass.vmDomainFile.SetRawPtr((pClassDebuggerModule ? pClassDebuggerModule->GetDomainFile() : NULL));
        pEvent->UnloadClass.classDebuggerAssemblyToken.Set(pAssembly);
    }

    m_pRCThread->SendIPCEvent();

    if (fIsLoadEvent && fIsReflection)
    {
        // Send the raw event, but don't actually sync and block the runtime.
        SendRawUpdateModuleSymsEvent(pClassDebuggerModule->GetRuntimeModule(), pAppDomain);
    }

}



/******************************************************************************
 *
 ******************************************************************************/
BOOL Debugger::SendSystemClassLoadUnloadEvent(mdTypeDef classMetadataToken,
                                              Module *classModule,
                                              BOOL fIsLoadEvent)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    if (!m_dClassLoadCallbackCount)
    {
        return FALSE;
    }

    BOOL fRetVal = FALSE;

    Assembly *pAssembly = classModule->GetAssembly();

    if (!m_pAppDomainCB->Lock())
        return (FALSE);

    AppDomainInfo *pADInfo = m_pAppDomainCB->FindFirst();

    while (pADInfo != NULL)
    {
        AppDomain *pAppDomain = pADInfo->m_pAppDomain;
        _ASSERTE(pAppDomain != NULL);

        // Only notify for app domains where the module has been fully loaded already
        // We used to make a different check here domain->ContainsAssembly() but that
        // triggers too early in the loading process. FindDomainFile will not become
        // non-NULL until the module is fully loaded into the domain which is what we
        // want.
        if (classModule->GetDomainFile() != NULL )
        {
            // Find the Left Side module that this class belongs in.
            DebuggerModule* pModule = LookupOrCreateModule(classModule, pAppDomain);
            _ASSERTE(pModule != NULL);

            // Only send a class load event if they're enabled for this module.
            if (pModule && pModule->ClassLoadCallbacksEnabled())
            {
                SendClassLoadUnloadEvent(classMetadataToken,
                                         pModule,
                                         pAssembly,
                                         pAppDomain,
                                         fIsLoadEvent);
                fRetVal = TRUE;
            }
        }

        pADInfo = m_pAppDomainCB->FindNext(pADInfo);
    }

    m_pAppDomainCB->Unlock();

    return fRetVal;
}


//
// LoadClass is called when a Runtime thread loads a new Class.
// Returns TRUE if an event is sent, FALSE otherwise
BOOL  Debugger::LoadClass(TypeHandle th,
                          mdTypeDef  classMetadataToken,
                          Module    *classModule,
                          AppDomain *pAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // @@@
    // Implements DebugInterface
    // This can be called by EE/Loader when class is loaded.
    //

    BOOL fRetVal = FALSE;

    if (CORDBUnrecoverableError(this))
        return FALSE;

    // Note that pAppDomain may be null.  The AppDomain isn't used here, and doesn't make a lot of sense since
    // we may be delivering the notification for a class in an assembly which is loaded into multiple AppDomains.  We
    // handle this in SendSystemClassLoadUnloadEvent below by looping through all AppDomains and dispatching
    // events for each that contain this assembly.

    LOG((LF_CORDB, LL_INFO10000, "D::LC: load class Tok:%#08x Mod:%#08x AD:%#08x classMod:%#08x modName:%ls\n",
         classMetadataToken, (pAppDomain == NULL) ? NULL : LookupOrCreateModule(classModule, pAppDomain),
         pAppDomain, classModule, classModule->GetDebugName()));

    //
    // If we're attaching, then we only need to send the event. We
    // don't need to disable event handling or lock the debugger
    // object.
    //
    SENDIPCEVENT_BEGIN(this, g_pEEInterface->GetThread());

    if (CORDebuggerAttached())
    {
        fRetVal = SendSystemClassLoadUnloadEvent(classMetadataToken, classModule, TRUE);

        if (fRetVal == TRUE)
        {
            // Stop all Runtime threads
            TrapAllRuntimeThreads();
        }
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::LC: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;

    return fRetVal;
}


//
// UnloadClass is called when a Runtime thread unloads a Class.
//
void Debugger::UnloadClass(mdTypeDef classMetadataToken,
                           Module *classModule,
                           AppDomain *pAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // @@@
    // Implements DebugInterface
    // Can only be called from EE

    if (CORDBUnrecoverableError(this))
    {
        return;
    }

    LOG((LF_CORDB, LL_INFO10000, "D::UC: unload class Tok:0x%08x Mod:%#08x AD:%#08x runtimeMod:%#08x modName:%ls\n",
         classMetadataToken, LookupOrCreateModule(classModule, pAppDomain), pAppDomain, classModule, classModule->GetDebugName()));

    Assembly *pAssembly = classModule->GetClassLoader()->GetAssembly();
    DebuggerModule *pModule = LookupOrCreateModule(classModule, pAppDomain);

    if ((pModule == NULL) || !pModule->ClassLoadCallbacksEnabled())
    {
        return;
    }

    SENDIPCEVENT_BEGIN(this, g_pEEInterface->GetThread());

    if (CORDebuggerAttached())
    {
        _ASSERTE((pAppDomain != NULL) && (pAssembly != NULL) && (pModule != NULL));

        SendClassLoadUnloadEvent(classMetadataToken, pModule, pAssembly, pAppDomain, FALSE);

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::UC: Skipping SendIPCEvent because RS detached."));
    }

    // Let other Runtime threads handle their events.
    SENDIPCEVENT_END;

}

/******************************************************************************
 *
 ******************************************************************************/
void Debugger::FuncEvalComplete(Thread* pThread, DebuggerEval *pDE)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    if (CORDBUnrecoverableError(this))
        return;

    LOG((LF_CORDB, LL_INFO1000, "D::FEC: func eval complete pDE:%p evalType:%d %s %s\n",
        pDE, pDE->m_evalType, pDE->m_successful ? "Success" : "Fail", pDE->m_aborted ? "Abort" : "Completed"));


    _ASSERTE(pDE->m_completed);
    _ASSERTE((g_pEEInterface->GetThread() && !g_pEEInterface->GetThread()->m_fPreemptiveGCDisabled) || g_fInControlC);
    _ASSERTE(ThreadHoldsLock());

    //
    // Get the domain that the result is valid in. The RS will cache this in the ICorDebugValue
    // Note: it's possible that the AppDomain has (or is about to be) unloaded, which could lead to a
    // crash when we use the DebuggerModule.  Ideally we'd only be using AppDomain IDs here.
    // We can't easily convert our ADID to an AppDomain* (SystemDomain::GetAppDomainFromId)
    // because we can't proove that that the AppDomain* would be valid (not unloaded).
    //
    AppDomain *pDomain = pThread->GetDomain();
    AppDomain *pResultDomain = ((pDE->m_debuggerModule == NULL) ? pDomain : pDE->m_debuggerModule->GetAppDomain());

    // Send a func eval complete event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce, DB_IPCE_FUNC_EVAL_COMPLETE, pThread, pDomain);

    ipce->FuncEvalComplete.funcEvalKey = pDE->m_funcEvalKey;
    ipce->FuncEvalComplete.successful = pDE->m_successful;
    ipce->FuncEvalComplete.aborted = pDE->m_aborted;
    ipce->FuncEvalComplete.resultAddr = pDE->m_result;
    ipce->FuncEvalComplete.vmAppDomain.SetRawPtr(pResultDomain);
    ipce->FuncEvalComplete.vmObjectHandle = pDE->m_vmObjectHandle;

    LOG((LF_CORDB, LL_INFO1000, "D::FEC: TypeHandle is %p\n", pDE->m_resultType.AsPtr()));

    Debugger::TypeHandleToExpandedTypeInfo(pDE->m_retValueBoxing, // whether return values get boxed or not depends on the particular FuncEval we're doing...
                                           pResultDomain,
                                           pDE->m_resultType,
                                           &ipce->FuncEvalComplete.resultType);

    _ASSERTE(ipce->FuncEvalComplete.resultType.elementType != ELEMENT_TYPE_VALUETYPE);

    // We must adjust the result address to point to the right place
    ipce->FuncEvalComplete.resultAddr = ArgSlotEndianessFixup((ARG_SLOT*)ipce->FuncEvalComplete.resultAddr,
        GetSizeForCorElementType(ipce->FuncEvalComplete.resultType.elementType));

    LOG((LF_CORDB, LL_INFO1000, "D::FEC: returned el %04x resultAddr %p\n",
        ipce->FuncEvalComplete.resultType.elementType, ipce->FuncEvalComplete.resultAddr));

    m_pRCThread->SendIPCEvent();

#endif
}

/******************************************************************************
 *
 ******************************************************************************/
bool Debugger::ResumeThreads(AppDomain* pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThisIsHelperThreadWorker());
    }
    CONTRACTL_END;

    // Okay, mark that we're not stopped anymore and let the
    // Runtime threads go...
    ReleaseAllRuntimeThreads(pAppDomain);

    // Return that we've continued the process.
    return true;
}


class CodeBuffer
{
public:

    BYTE *getCodeBuffer(DebuggerJitInfo *dji)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        CodeRegionInfo codeRegionInfo = CodeRegionInfo::GetCodeRegionInfo(dji);

        if (codeRegionInfo.getAddrOfColdCode())
        {
            _ASSERTE(codeRegionInfo.getSizeOfHotCode() != 0);
            _ASSERTE(codeRegionInfo.getSizeOfColdCode() != 0);
            S_SIZE_T totalSize = S_SIZE_T( codeRegionInfo.getSizeOfHotCode() ) +
                                                S_SIZE_T( codeRegionInfo.getSizeOfColdCode() );
            if ( totalSize.IsOverflow() )
            {
                _ASSERTE(0 && "Buffer overflow error in getCodeBuffer");
                return NULL;
            }

            BYTE *code = (BYTE *) buffer.AllocNoThrow( totalSize.Value() );
            if (code)
            {
                memcpy(code,
                       (void *) codeRegionInfo.getAddrOfHotCode(),
                       codeRegionInfo.getSizeOfHotCode());

                memcpy(code + codeRegionInfo.getSizeOfHotCode(),
                       (void *) codeRegionInfo.getAddrOfColdCode(),
                       codeRegionInfo.getSizeOfColdCode());

                // Now patch the control transfer instructions
            }

            return code;
        }
        else
        {
            return dac_cast<PTR_BYTE>(codeRegionInfo.getAddrOfHotCode());
        }
    }
private:

    CQuickBytes buffer;
};


//---------------------------------------------------------------------------------------
//
// Called on the helper thread to serialize metadata so it can be read out-of-process.
//
// Arguments:
//    pModule - module that needs metadata serialization
//    countBytes - out value, holds the number of bytes which were allocated in the
//                 serialized buffer
//
// Return Value:
//    A pointer to a serialized buffer of metadata. The caller should free this bufer using
//    DeleteInteropSafe
//
// Assumptions:
//    This is called on the helper-thread, or a thread pretending to be the helper-thread.
//    For any synchronous message, the debuggee should be synchronized. The only async
//    messages are Attach and Async-Break.
//
//
//---------------------------------------------------------------------------------------
BYTE* Debugger::SerializeModuleMetaData(Module * pModule, DWORD * countBytes)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "Debugger::SMMD called\n"));

    // Do not release the emitter. This is a weak reference.
    IMetaDataEmit *pEmitter = pModule->GetEmitter();
    _ASSERTE(pEmitter != NULL);

    HRESULT hr;
    BYTE* metadataBuffer = NULL;
    ReleaseHolder<IMDInternalEmit> pInternalEmitter;
    ULONG originalUpdateMode;
    hr = pEmitter->QueryInterface(IID_IMDInternalEmit, (void **)&pInternalEmitter);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO10, "Debugger::SMMD pEmitter doesn't support IID_IMDInternalEmit hr=0x%x\n", hr));
        ThrowHR(hr);
    }
    _ASSERTE(pInternalEmitter != NULL);

    hr = pInternalEmitter->SetMDUpdateMode(MDUpdateExtension, &originalUpdateMode);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO10, "Debugger::SMMD SetMDUpdateMode failed hr=0x%x\n", hr));
        ThrowHR(hr);
    }
    _ASSERTE(originalUpdateMode == MDUpdateFull);

    hr = pEmitter->GetSaveSize(cssQuick, countBytes);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO10, "Debugger::SMMD GetSaveSize failed hr=0x%x\n", hr));
        pInternalEmitter->SetMDUpdateMode(originalUpdateMode, NULL);
        ThrowHR(hr);
    }

    EX_TRY
    {
        metadataBuffer = new (interopsafe) BYTE[*countBytes];
    }
    EX_CATCH
    {
        LOG((LF_CORDB, LL_INFO10, "Debugger::SMMD Allocation failed\n"));
        pInternalEmitter->SetMDUpdateMode(originalUpdateMode, NULL);
        EX_RETHROW;
    }
    EX_END_CATCH(SwallowAllExceptions);
    _ASSERTE(metadataBuffer != NULL); // allocation would throw first

    // Caller ensures serialization that guarantees that the metadata doesn't grow underneath us.
    hr = pEmitter->SaveToMemory(metadataBuffer, *countBytes);
    if(FAILED(hr))
    {
        LOG((LF_CORDB, LL_INFO10, "Debugger::SMMD SaveToMemory failed hr=0x%x\n", hr));
        DeleteInteropSafe(metadataBuffer);
        pInternalEmitter->SetMDUpdateMode(originalUpdateMode, NULL);
        ThrowHR(hr);
    }

    pInternalEmitter->SetMDUpdateMode(originalUpdateMode, NULL);
    LOG((LF_CORDB, LL_INFO10000, "Debugger::SMMD exiting\n"));
    return metadataBuffer;
}

//---------------------------------------------------------------------------------------
//
// Handle an IPC event from the Debugger.
//
// Arguments:
//    event - IPC event to handle.
//
// Return Value:
//    True if the event was a continue. Else false.
//
// Assumptions:
//    This is called on the helper-thread, or a thread pretending to be the helper-thread.
//    For any synchronous message, the debuggee should be synchronized. The only async
//    messages are Attach and Async-Break.
//
// Notes:
// HandleIPCEvent is called by the RC thread in response to an event
// from the Debugger Interface. No other IPC events, nor any Runtime
// events will come in until this method returns. Returns true if this
// was a Continue event.
//
// If this function is called on native debugger helper thread, we will
// handle everything. However if this is called on managed thread doing
// helper thread duty, we will fail on operation since we are mainly
// waiting for CONTINUE message from the RS.
//
//
//---------------------------------------------------------------------------------------

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
bool Debugger::HandleIPCEvent(DebuggerIPCEvent * pEvent)
{
    CONTRACTL
    {
        THROWS;
        if (g_pEEInterface->GetThread() != NULL) { GC_TRIGGERS; } else { GC_NOTRIGGER; }

        PRECONDITION(ThisIsHelperThreadWorker());

        if (m_stopped)
        {
            MODE_COOPERATIVE;
        }
        else
        {
            MODE_ANY;
        }
    }
    CONTRACTL_END;

    // If we're the temporary helper thread, then we may reject certain operations.
    bool temporaryHelp = ThisIsTempHelperThread();


#ifdef _DEBUG
    // This reg key allows us to test our unhandled event filter installed in HandleIPCEventWrapper
    // to make sure it works properly.
    static int s_fDbgFaultInHandleIPCEvent = -1;
    if (s_fDbgFaultInHandleIPCEvent == -1)
    {
        s_fDbgFaultInHandleIPCEvent = UnsafeGetConfigDWORD(CLRConfig::INTERNAL_DbgFaultInHandleIPCEvent);
    }

    // If we need to fault, let's generate an access violation.
    if (s_fDbgFaultInHandleIPCEvent)
    {
        *((volatile BYTE *)0) = 0;
    }
#endif

    BOOL fSuccess;
    bool fContinue = false;
    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_INFO10000, "D::HIPCE: got %s\n", IPCENames::GetName(pEvent->type)));
    DbgLog((DebuggerIPCEventType)(pEvent->type & DB_IPCE_TYPE_MASK));

    // As for runtime is considered stopped, it means that managed threads will not
    // execute anymore managed code. However, these threads may be still running for
    // unmanaged code. So it is not true that we do not need to hold the lock while processing
    // synchrnoized event.
    //
    // The worst of all, it is the special case where user break point and exception can
    // be sent as part of attach if debugger was launched by managed app.
    //
    DebuggerLockHolder dbgLockHolder(this, FALSE);
    bool lockedThreadStore = false;

    if ((pEvent->type & DB_IPCE_TYPE_MASK) == DB_IPCE_ASYNC_BREAK ||
        (pEvent->type & DB_IPCE_TYPE_MASK) == DB_IPCE_ATTACHING ||
        this->m_willBlockOnGarbageCollectionEvent)
    {
        if (!this->m_willBlockOnGarbageCollectionEvent && !this->m_stopped)
        {
            lockedThreadStore = true;
            ThreadSuspend::LockThreadStore(ThreadSuspend::SUSPEND_FOR_DEBUGGER);
        }
        dbgLockHolder.Acquire();
    }
    else
    {
        _ASSERTE(m_stopped);
        _ASSERTE(ThreadHoldsLock());
    }


    switch (pEvent->type & DB_IPCE_TYPE_MASK)
    {

    case DB_IPCE_ATTACHING:
        // In V3, Attach is atomic, meaning that there isn't a complex handshake back and forth between LS + RS.
        // the RS sends a single-attaching event and attaches at the first response from the Left-side.
        StartCanaryThread();

        // In V3 after attaching event was handled we iterate throughout all ADs and made shadow copies of PDBs in the BIN directories.
        // After all AppDomain, DomainAssembly and modules iteration was available in out-of-proccess model in V4 the code that enables
        // PDBs to be copied was not called at attach time.
        // Eliminating PDBs copying side effect is an issue: Dev10 #927143
        EX_TRY
        {
            IterateAppDomainsForPdbs();
        }
        EX_CATCH_HRESULT(hr); // ignore failures

        if (m_jitAttachInProgress)
        {
            // For jit-attach, mark that we're attached now.
            // This lets callers to code:Debugger.JitAttach check the flag and
            // send the jit-attach event just like a normal event.
            MarkDebuggerAttachedInternal();

            // set the managed attach event so that waiting threads can continue
            VERIFY(SetEvent(GetAttachEvent()));
            break;
        }

        VERIFY(SetEvent(GetAttachEvent()));

        //
        // For regular (non-jit) attach, fall through to do an async break.
        //
        FALLTHROUGH;

    case DB_IPCE_ASYNC_BREAK:
        {
            if (temporaryHelp)
            {
                // Don't support async break on temporary helper thread.
                // Well, this function does not return HR. So this means that
                // ASYNC_BREAK event will be catching silently while we are
                // doing helper thread duty!
                //
                hr = CORDBG_E_NOTREADY;
            }
            else
            {
                // not synchornized. We get debugger lock upon the function entry
                _ASSERTE(ThreadHoldsLock());

                // Simply trap all Runtime threads if we're not already trying to.
                if (!m_willBlockOnGarbageCollectionEvent && !m_trappingRuntimeThreads)
                {
                    // If the RS sent an Async-break, then that's an explicit request.
                    m_RSRequestedSync = TRUE;
                    TrapAllRuntimeThreads(); // Non-blocking...
                }
            }
            break;
        }

    case DB_IPCE_CONTINUE:
        {
            if (this->m_isBlockedOnGarbageCollectionEvent)
            {
                this->m_stopped = false;
                SetEvent(this->GetGarbageCollectionBlockerEvent());
            }
            else
            {
                fContinue = ResumeThreads(pEvent->vmAppDomain.GetRawPtr());

                //
                // Go ahead and release the TSL now that we're continuing. This ensures that we've held
                // the thread store lock the entire time the Runtime was just stopped.
                //
                ThreadSuspend::UnlockThreadStore(FALSE, ThreadSuspend::SUSPEND_FOR_DEBUGGER);
            }
            GetCanary()->ClearCache();
            break;
        }

    case DB_IPCE_BREAKPOINT_ADD:
        {

            //
            // Currently, we can't create a breakpoint before a
            // function desc is available.
            // Also, we can't know if a breakpoint is ok
            // prior to the method being JITted.
            //

            _ASSERTE(hr == S_OK);
            DebuggerBreakpoint * pDebuggerBP = NULL;

            DebuggerModule * pDebuggerModule = LookupOrCreateModule(pEvent->BreakpointData.vmDomainFile);
            Module * pModule = pDebuggerModule->GetRuntimeModule();
            DebuggerMethodInfo * pDMI = GetOrCreateMethodInfo(pModule, pEvent->BreakpointData.funcMetadataToken);
            MethodDesc * pMethodDesc = pEvent->BreakpointData.nativeCodeMethodDescToken.UnWrap();

            DebuggerJitInfo * pDJI =  NULL;
            if ((pMethodDesc != NULL) && (pDMI != NULL))
            {
                pDJI = pDMI->FindOrCreateInitAndAddJitInfo(pMethodDesc, NULL /* startAddr */);
            }

            {
                // If we haven't been either JITted or EnC'd yet, then
                // we'll put a patch in by offset, implicitly relative
                // to the first version of the code.

                pDebuggerBP = new (interopsafe, nothrow) DebuggerBreakpoint(pModule,
                                                                            pEvent->BreakpointData.funcMetadataToken,
                                                                            pEvent->vmAppDomain.GetRawPtr(),
                                                                            pEvent->BreakpointData.offset,
                                                                            !pEvent->BreakpointData.isIL,
                                                                            pEvent->BreakpointData.encVersion,
                                                                            pMethodDesc,
                                                                            pDJI,
                                                                            pEvent->BreakpointData.nativeCodeMethodDescToken == NULL,
                                                                            &fSuccess);

                TRACE_ALLOC(pDebuggerBP);

                if ((pDebuggerBP != NULL) && !fSuccess)
                {
                    DeleteInteropSafe(pDebuggerBP);
                    pDebuggerBP = NULL;
                    hr = CORDBG_E_UNABLE_TO_SET_BREAKPOINT;
                }
            }

            if ((pDebuggerBP == NULL) && !FAILED(hr))
            {
                hr = E_OUTOFMEMORY;
            }

            LOG((LF_CORDB,LL_INFO10000,"\tBP Add: BPTOK:"
                "0x%x, tok=0x%08x, offset=0x%x, isIL=%d dm=0x%x m=0x%x\n",
                 pDebuggerBP,
                 pEvent->BreakpointData.funcMetadataToken,
                 pEvent->BreakpointData.offset,
                 pEvent->BreakpointData.isIL,
                 pDebuggerModule,
                 pModule));

            //
            // We're using a two-way event here, so we place the
            // result event into the _receive_ buffer, not the send
            // buffer.
            //

            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCEvent(pIPCResult,
                         DB_IPCE_BREAKPOINT_ADD_RESULT,
                         g_pEEInterface->GetThread(),
                         pEvent->vmAppDomain);

            pIPCResult->BreakpointData.breakpointToken.Set(pDebuggerBP);
            pIPCResult->hr = hr;

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_STEP:
        {
            LOG((LF_CORDB,LL_INFO10000, "D::HIPCE: stepIn:0x%x frmTok:0x%x"
                "StepIn:0x%x RangeIL:0x%x RangeCount:0x%x MapStop:0x%x "
                "InterceptStop:0x%x AppD:0x%x\n",
                pEvent->StepData.stepIn,
                pEvent->StepData.frameToken.GetSPValue(),
                pEvent->StepData.stepIn,
                pEvent->StepData.rangeIL,
                pEvent->StepData.rangeCount,
                pEvent->StepData.rgfMappingStop,
                pEvent->StepData.rgfInterceptStop,
                pEvent->vmAppDomain.GetRawPtr()));

            // <TODO>@todo memory allocation - bad if we're synced</TODO>
            Thread * pThread = pEvent->StepData.vmThreadToken.GetRawPtr();
            AppDomain * pAppDomain = pEvent->vmAppDomain.GetRawPtr();

            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCEvent(pIPCResult,
                         DB_IPCE_STEP_RESULT,
                         pThread,
                         pEvent->vmAppDomain);

            if (temporaryHelp)
            {
                // Can't step on the temporary helper thread.
                pIPCResult->hr = CORDBG_E_NOTREADY;
            }
            else
            {
                DebuggerStepper * pStepper;

                if (pEvent->StepData.IsJMCStop)
                {
                    pStepper = new (interopsafe, nothrow) DebuggerJMCStepper(pThread,
                                                                             pEvent->StepData.rgfMappingStop,
                                                                             pEvent->StepData.rgfInterceptStop,
                                                                             pAppDomain);
                }
                else
                {
                    pStepper = new (interopsafe, nothrow) DebuggerStepper(pThread,
                                                                          pEvent->StepData.rgfMappingStop,
                                                                          pEvent->StepData.rgfInterceptStop,
                                                                           pAppDomain);
                }

                if (pStepper == NULL)
                {
                    pIPCResult->hr = E_OUTOFMEMORY;

                    m_pRCThread->SendIPCReply();

                    break;
                }
                TRACE_ALLOC(pStepper);

                unsigned int cRanges = pEvent->StepData.totalRangeCount;

                _ASSERTE(cRanges == 0 || ((cRanges > 0) && (cRanges == pEvent->StepData.rangeCount)));

                if (!pStepper->Step(pEvent->StepData.frameToken,
                                    pEvent->StepData.stepIn,
                                    &(pEvent->StepData.range),
                                    cRanges,
                                    ((cRanges > 0) ? pEvent->StepData.rangeIL : false)))
                {
                    pIPCResult->hr = E_OUTOFMEMORY;

                    m_pRCThread->SendIPCReply();

                    DeleteInteropSafe(pStepper);
                    break;
                }

                pIPCResult->StepData.stepperToken.Set(pStepper);


            } // end normal step case.


            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_STEP_OUT:
        {
            // <TODO>@todo memory allocation - bad if we're synced</TODO>
            Thread * pThread = pEvent->StepData.vmThreadToken.GetRawPtr();
            AppDomain * pAppDomain = pEvent->vmAppDomain.GetRawPtr();

            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCEvent(pIPCResult,
                         DB_IPCE_STEP_RESULT,
                         pThread,
                         pAppDomain);

            if (temporaryHelp)
            {
                // Can't step on the temporary helper thread.
                pIPCResult->hr = CORDBG_E_NOTREADY;
            }
            else
            {
                DebuggerStepper * pStepper;

                if (pEvent->StepData.IsJMCStop)
                {
                    pStepper = new (interopsafe, nothrow) DebuggerJMCStepper(pThread,
                                                                             pEvent->StepData.rgfMappingStop,
                                                                             pEvent->StepData.rgfInterceptStop,
                                                                             pAppDomain);
                }
                else
                {
                    pStepper = new (interopsafe, nothrow) DebuggerStepper(pThread,
                                                                          pEvent->StepData.rgfMappingStop,
                                                                          pEvent->StepData.rgfInterceptStop,
                                                                          pAppDomain);
                }


                if (pStepper == NULL)
                {
                    pIPCResult->hr = E_OUTOFMEMORY;
                    m_pRCThread->SendIPCReply();

                    break;
                }

                TRACE_ALLOC(pStepper);

                // Safe to stack trace b/c we're stopped.
                StackTraceTicket ticket(pThread);

                pStepper->StepOut(pEvent->StepData.frameToken, ticket);

                pIPCResult->StepData.stepperToken.Set(pStepper);
            }

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_BREAKPOINT_REMOVE:
        {
            // <TODO>@todo memory allocation - bad if we're synced</TODO>

            DebuggerBreakpoint * pDebuggerBP = pEvent->BreakpointData.breakpointToken.UnWrap();

            pDebuggerBP->Delete();
        }
        break;

    case DB_IPCE_STEP_CANCEL:
        {
            // <TODO>@todo memory allocation - bad if we're synced</TODO>
            LOG((LF_CORDB,LL_INFO10000, "D:HIPCE:Got STEP_CANCEL for stepper 0x%p\n",
                 pEvent->StepData.stepperToken.UnWrap()));

            DebuggerStepper * pStepper = pEvent->StepData.stepperToken.UnWrap();

            pStepper->Delete();
        }
        break;

    case DB_IPCE_SET_ALL_DEBUG_STATE:
        {
            Thread * pThread = pEvent->SetAllDebugState.vmThreadToken.GetRawPtr();
            CorDebugThreadState debugState = pEvent->SetAllDebugState.debugState;

            LOG((LF_CORDB,LL_INFO10000,"HandleIPCE: SetAllDebugState: except thread 0x%08x (ID:0x%x) to state 0x%x\n",
                 pThread,
                 (pThread != NULL) ? GetThreadIdHelper(pThread) : 0,
                 debugState));

            if (!g_fProcessDetach)
            {
                g_pEEInterface->SetAllDebugState(pThread, debugState);
            }

            STRESS_LOG1(LF_CORDB,LL_INFO10000,"HandleIPC: Got 0x%x back from SetAllDebugState\n", hr);

            // Just send back an HR.
            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            PREFIX_ASSUME(pIPCResult != NULL);

            InitIPCEvent(pIPCResult, DB_IPCE_SET_DEBUG_STATE_RESULT, NULL, NULL);

            pIPCResult->hr = S_OK;

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_GET_GCHANDLE_INFO:
        // Given an unvalidated GC-handle, find out all the info about it to view the object
        // at the other end
        {
            OBJECTHANDLE objectHandle = pEvent->GetGCHandleInfo.GCHandle.GetRawPtr();

            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            PREFIX_ASSUME(pIPCResult != NULL);

            InitIPCEvent(pIPCResult, DB_IPCE_GET_GCHANDLE_INFO_RESULT, NULL, NULL);

            bool fValid = SUCCEEDED(ValidateGCHandle(objectHandle));

            AppDomain * pAppDomain = NULL;

            if(fValid)
            {
                // Get the appdomain
                pAppDomain = AppDomain::GetCurrentDomain();

                _ASSERTE(pAppDomain != NULL);
            }

            pIPCResult->hr = S_OK;
            pIPCResult->GetGCHandleInfoResult.vmAppDomain.SetRawPtr(pAppDomain);
            pIPCResult->GetGCHandleInfoResult.fValid = fValid;

            m_pRCThread->SendIPCReply();

        }
        break;

    case DB_IPCE_GET_BUFFER:
        {
            GetAndSendBuffer(m_pRCThread, pEvent->GetBuffer.bufSize);
        }
        break;

    case DB_IPCE_RELEASE_BUFFER:
        {
            SendReleaseBuffer(m_pRCThread, pEvent->ReleaseBuffer.pBuffer);
        }
        break;
#ifdef EnC_SUPPORTED
    case DB_IPCE_APPLY_CHANGES:
        {
            LOG((LF_ENC, LL_INFO100, "D::HIPCE: DB_IPCE_APPLY_CHANGES 1\n"));

            DebuggerModule * pDebuggerModule = LookupOrCreateModule(pEvent->ApplyChanges.vmDomainFile);
            //
            // @todo handle error.
            //
            hr = ApplyChangesAndSendResult(pDebuggerModule,
                                           pEvent->ApplyChanges.cbDeltaMetadata,
                                           (BYTE*) CORDB_ADDRESS_TO_PTR(pEvent->ApplyChanges.pDeltaMetadata),
                                           pEvent->ApplyChanges.cbDeltaIL,
                                           (BYTE*) CORDB_ADDRESS_TO_PTR(pEvent->ApplyChanges.pDeltaIL));

            LOG((LF_ENC, LL_INFO100, "D::HIPCE: DB_IPCE_APPLY_CHANGES 2\n"));
        }
        break;
#endif // EnC_SUPPORTED

    case DB_IPCE_SET_CLASS_LOAD_FLAG:
        {
            DebuggerModule *pDebuggerModule = LookupOrCreateModule(pEvent->SetClassLoad.vmDomainFile);

            _ASSERTE(pDebuggerModule != NULL);

            LOG((LF_CORDB, LL_INFO10000,
                 "D::HIPCE: class load flag is %d for module 0x%p\n",
                 pEvent->SetClassLoad.flag,
                 pDebuggerModule));

            pDebuggerModule->EnableClassLoadCallbacks((BOOL)pEvent->SetClassLoad.flag);
        }
        break;

    case DB_IPCE_IS_TRANSITION_STUB:
        GetAndSendTransitionStubInfo((CORDB_ADDRESS_TYPE*)pEvent->IsTransitionStub.address);
        break;

    case DB_IPCE_MODIFY_LOGSWITCH:
        g_pEEInterface->DebuggerModifyingLogSwitch (pEvent->LogSwitchSettingMessage.iLevel,
                                                    pEvent->LogSwitchSettingMessage.szSwitchName.GetString());

        break;

    case DB_IPCE_ENABLE_LOG_MESSAGES:
        {
            bool fOnOff = pEvent->LogSwitchSettingMessage.iLevel ? true : false;
            EnableLogMessages (fOnOff);
        }
        break;

    case DB_IPCE_SET_IP:

        {
            // This is a synchronous event (reply required)
            DebuggerIPCEvent * pIPCResult = m_pRCThread->GetIPCEventReceiveBuffer();

            // Don't have an explicit reply msg
            InitIPCReply(pIPCResult, DB_IPCE_SET_IP);

            if (temporaryHelp)
            {
                pIPCResult->hr = CORDBG_E_NOTREADY;
            }
            else if (!g_fProcessDetach)
            {
                //
                // Since this pointer is coming from the RS, it may be NULL or something
                // unexpected in an OOM situation.  Quickly just sanity check them.
                //
                Thread * pThread = pEvent->SetIP.vmThreadToken.GetRawPtr();
                Module * pModule = pEvent->SetIP.vmDomainFile.GetRawPtr()->GetModule();

                // Get the DJI for this function
                DebuggerMethodInfo * pDMI = GetOrCreateMethodInfo(pModule, pEvent->SetIP.mdMethod);
                DebuggerJitInfo * pDJI = NULL;
                if (pDMI != NULL)
                {
                    // In the EnC case, if we look for an older version, we need to find the DJI by starting
                    // address, rather than just by MethodDesc. In the case of generics, we may need to create a DJI, so we
                    pDJI = pDMI->FindOrCreateInitAndAddJitInfo(pEvent->SetIP.vmMethodDesc.GetRawPtr(),
                                                               PINSTRToPCODE((TADDR)pEvent->SetIP.startAddress));
                }

                if ((pDJI != NULL) && (pThread != NULL) && (pModule != NULL))
                {
                    CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(&(pIPCResult->hr), GetCanary());

                    if (SUCCEEDED(pIPCResult->hr))
                    {
                        pIPCResult->hr = SetIP(pEvent->SetIP.fCanSetIPOnly,
                                          pThread,
                                          pModule,
                                               pEvent->SetIP.mdMethod,
                                               pDJI,
                                               pEvent->SetIP.offset,
                                               pEvent->SetIP.fIsIL
                                               );
                    }
                }
                else
                {
                    pIPCResult->hr = E_INVALIDARG;
                }
            }
            else
            {
                pIPCResult->hr = S_OK;
            }

            // Send the result
            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_DETACH_FROM_PROCESS:
        LOG((LF_CORDB, LL_INFO10000, "Detaching from process!\n"));

        // Delete all controllers (remove patches etc.)
        DebuggerController::DeleteAllControllers();
        // Note that we'd like to be able to do this assert here
        //      _ASSERTE(DebuggerController::GetNumberOfPatches() == 0);
        // However controllers may get queued for deletion if there is outstanding
        // work and so we can't gaurentee the deletion will complete now.
        // @dbgtodo  inspection: This shouldn't be an issue in the complete V3 architecture

        MarkDebuggerUnattachedInternal();

        m_pRCThread->RightSideDetach();


        // Clear JMC status
        {
            LOG((LF_CORDB, LL_EVERYTHING, "Setting all JMC methods to false:\n"));
            // On detach, set all DMI's JMC status to false.
            // We have to do this b/c we clear the DebuggerModules and allocated
            // new ones on re-attach; and the DMI & DM need to be in sync
            // (in this case, agreeing that JMC-status = false).
            // This also syncs the EE modules and disables all JMC probes.
            DebuggerMethodInfoTable * pMethodInfoTable = g_pDebugger->GetMethodInfoTable();

            if (pMethodInfoTable != NULL)
            {
                HASHFIND hashFind;
                DebuggerDataLockHolder debuggerDataLockHolder(this);

                for (DebuggerMethodInfo * pMethodInfo = pMethodInfoTable->GetFirstMethodInfo(&hashFind);
                    pMethodInfo != NULL;
                    pMethodInfo = pMethodInfoTable->GetNextMethodInfo(&hashFind))
                {
                    pMethodInfo->SetJMCStatus(false);
                }
            }
            LOG((LF_CORDB, LL_EVERYTHING, "Done clearing JMC methods!\n"));
        }

        // Clean up the hash of DebuggerModules
        // This method is overridden to also free all DebuggerModule objects
        if (m_pModules != NULL)
        {

            // Removes all DebuggerModules
            DebuggerDataLockHolder ch(this);
            m_pModules->Clear();

        }

        // Reply to the detach message before we release any Runtime threads. This ensures that the debugger will get
        // the detach reply before the process exits if the main thread is near exiting.
        m_pRCThread->SendIPCReply();

        if (this->m_isBlockedOnGarbageCollectionEvent)
        {
            this->m_stopped = FALSE;
            SetEvent(this->GetGarbageCollectionBlockerEvent());
        }
        else
        {
            // Let the process run free now... there is no debugger to bother it anymore.
            fContinue = ResumeThreads(pEvent->vmAppDomain.GetRawPtr());

            //
            // Go ahead and release the TSL now that we're continuing. This ensures that we've held
            // the thread store lock the entire time the Runtime was just stopped.
            //
            ThreadSuspend::UnlockThreadStore(FALSE, ThreadSuspend::SUSPEND_FOR_DEBUGGER);
        }

        break;

#ifndef DACCESS_COMPILE

    case DB_IPCE_FUNC_EVAL:
        {
            // This is a synchronous event (reply required)
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            Thread * pThread = pEvent->FuncEval.vmThreadToken.GetRawPtr();

            InitIPCEvent(pEvent, DB_IPCE_FUNC_EVAL_SETUP_RESULT, pThread, pThread->GetDomain());

            BYTE * pbArgDataArea = NULL;
            DebuggerEval * pDebuggerEvalKey = NULL;

            pEvent->hr = FuncEvalSetup(&(pEvent->FuncEval), &pbArgDataArea, &pDebuggerEvalKey);

            // Send the result of how the func eval setup went.
            pEvent->FuncEvalSetupComplete.argDataArea = PTR_TO_CORDB_ADDRESS(pbArgDataArea);
            pEvent->FuncEvalSetupComplete.debuggerEvalKey.Set(pDebuggerEvalKey);

            m_pRCThread->SendIPCReply();
        }

        break;

#endif

    case DB_IPCE_SET_REFERENCE:
        {
            // This is a synchronous event (reply required)
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCReply(pEvent, DB_IPCE_SET_REFERENCE_RESULT);

            pEvent->hr = SetReference(pEvent->SetReference.objectRefAddress,
                                      pEvent->SetReference.vmObjectHandle,
                                      pEvent->SetReference.newReference);

            // Send the result of how the set reference went.
            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_SET_VALUE_CLASS:
        {
            // This is a synchronous event (reply required)
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCReply(pEvent, DB_IPCE_SET_VALUE_CLASS_RESULT);

            pEvent->hr = SetValueClass(pEvent->SetValueClass.oldData,
                                       pEvent->SetValueClass.newData,
                                       &pEvent->SetValueClass.type);

            // Send the result of how the set reference went.
            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_GET_THREAD_FOR_TASKID:
        {
             Thread *pThreadRet = NULL;

             // This is a synchronous event (reply required)
             pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

             InitIPCReply(pEvent, DB_IPCE_GET_THREAD_FOR_TASKID_RESULT);

             pEvent->GetThreadForTaskIdResult.vmThreadToken.SetRawPtr(pThreadRet);
             pEvent->hr = S_OK;

             m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_CREATE_HANDLE:
        {
             Object * pObject = (Object*)pEvent->CreateHandle.objectToken;
             OBJECTREF objref = ObjectToOBJECTREF(pObject);
             AppDomain * pAppDomain = pEvent->vmAppDomain.GetRawPtr();
             CorDebugHandleType handleType = pEvent->CreateHandle.handleType;
             OBJECTHANDLE objectHandle;

             // This is a synchronous event (reply required)
             pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

             InitIPCReply(pEvent, DB_IPCE_CREATE_HANDLE_RESULT);

             {
                 // Handle creation may need to allocate memory.
                 // The API specifically limits the number of handls Cordbg can create,
                 // so we could preallocate and fail allocating anything beyond that.
                 CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(&(pEvent->hr), GetCanary());

                 if (SUCCEEDED(pEvent->hr))
                 {
                    switch (handleType)
                    {
                    case HANDLE_STRONG:
                        // create strong handle
                        objectHandle = pAppDomain->CreateStrongHandle(objref);
                        break;
                    case HANDLE_WEAK_TRACK_RESURRECTION:
                         // create the weak long handle
                         objectHandle = pAppDomain->CreateLongWeakHandle(objref);
                        break;
                    case HANDLE_PINNED:
                        // create pinning handle
                        objectHandle = pAppDomain->CreatePinningHandle(objref);
                        break;
                    default:
                        pEvent->hr = E_INVALIDARG;
                    }
                 }
                 if (SUCCEEDED(pEvent->hr))
                 {
                    pEvent->CreateHandleResult.vmObjectHandle.SetRawPtr(objectHandle);
                 }
             }

             m_pRCThread->SendIPCReply();
             break;
        }

    case DB_IPCE_DISPOSE_HANDLE:
        {
            // DISPOSE an object handle
            OBJECTHANDLE objectHandle = pEvent->DisposeHandle.vmObjectHandle.GetRawPtr();
            CorDebugHandleType handleType = pEvent->DisposeHandle.handleType;

            switch (handleType)
            {
            case HANDLE_STRONG:
                DestroyStrongHandle(objectHandle);
                break;
            case HANDLE_WEAK_TRACK_RESURRECTION:
                DestroyLongWeakHandle(objectHandle);
                break;
            case HANDLE_PINNED:
                DestroyPinningHandle(objectHandle);
                break;
            default:
                pEvent->hr = E_INVALIDARG;
            }
            break;
        }

#ifndef DACCESS_COMPILE

    case DB_IPCE_FUNC_EVAL_ABORT:
        {
            LOG((LF_CORDB, LL_INFO1000, "D::HIPCE: Got FuncEvalAbort for pDE:%08x\n",
                pEvent->FuncEvalAbort.debuggerEvalKey.UnWrap()));

            // This is a synchronous event (reply required)

            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();
            InitIPCReply(pEvent,DB_IPCE_FUNC_EVAL_ABORT_RESULT);

            pEvent->hr = FuncEvalAbort(pEvent->FuncEvalAbort.debuggerEvalKey.UnWrap());

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_FUNC_EVAL_RUDE_ABORT:
        {
            LOG((LF_CORDB, LL_INFO1000, "D::HIPCE: Got FuncEvalRudeAbort for pDE:%08x\n",
                pEvent->FuncEvalRudeAbort.debuggerEvalKey.UnWrap()));

            // This is a synchronous event (reply required)

            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCReply(pEvent, DB_IPCE_FUNC_EVAL_RUDE_ABORT_RESULT);

            pEvent->hr = FuncEvalRudeAbort(pEvent->FuncEvalRudeAbort.debuggerEvalKey.UnWrap());

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_FUNC_EVAL_CLEANUP:

        // This is a synchronous event (reply required)

        pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

        InitIPCReply(pEvent,DB_IPCE_FUNC_EVAL_CLEANUP_RESULT);

        pEvent->hr = FuncEvalCleanup(pEvent->FuncEvalCleanup.debuggerEvalKey.UnWrap());

        m_pRCThread->SendIPCReply();

        break;

#endif

    case DB_IPCE_CONTROL_C_EVENT_RESULT:
        {
            // store the result of whether the event has been handled by the debugger and
            // wake up the thread waiting for the result
            SetDebuggerHandlingCtrlC(pEvent->hr == S_OK);
            VERIFY(SetEvent(GetCtrlCMutex()));
        }
        break;

    // Set the JMC status on invididual methods
    case DB_IPCE_SET_METHOD_JMC_STATUS:
        {
            // Get the info out of the event
            DebuggerModule * pDebuggerModule = LookupOrCreateModule(pEvent->SetJMCFunctionStatus.vmDomainFile);
            Module * pModule = pDebuggerModule->GetRuntimeModule();

            bool fStatus = (pEvent->SetJMCFunctionStatus.dwStatus != 0);

            mdMethodDef token = pEvent->SetJMCFunctionStatus.funcMetadataToken;

            // Prepare reply
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCEvent(pEvent, DB_IPCE_SET_METHOD_JMC_STATUS_RESULT, NULL, NULL);

            pEvent->hr = S_OK;

            if (pDebuggerModule->HasAnyOptimizedCode() && fStatus)
            {
                // If there's optimized code, then we can't be set JMC status to true.
                // That's because JMC probes are not injected in optimized code, and we
                // need a JMC probe to have a JMC function.
                pEvent->hr = CORDBG_E_CANT_SET_TO_JMC;
            }
            else
            {
                DebuggerDataLockHolder debuggerDataLockHolder(this);
                // This may be called on an unjitted method, so we may
                // have to create the MethodInfo.
                DebuggerMethodInfo * pMethodInfo = GetOrCreateMethodInfo(pModule, token);

                if (pMethodInfo == NULL)
                {
                    pEvent->hr = E_OUTOFMEMORY;
                }
                else
                {
                    // Update the storage on the LS
                    pMethodInfo->SetJMCStatus(fStatus);
                }
            }

            // Send reply
            m_pRCThread->SendIPCReply();
        }
        break;

    // Get the JMC status on a given function
    case DB_IPCE_GET_METHOD_JMC_STATUS:
        {
            // Get the method
            DebuggerModule * pDebuggerModule = LookupOrCreateModule(pEvent->SetJMCFunctionStatus.vmDomainFile);

            Module * pModule = pDebuggerModule->GetRuntimeModule();

            mdMethodDef token = pEvent->SetJMCFunctionStatus.funcMetadataToken;

            // Init reply
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();
            InitIPCEvent(pEvent, DB_IPCE_GET_METHOD_JMC_STATUS_RESULT, NULL, NULL);

            //
            // This may be called on an unjitted method, so we may
            // have to create the MethodInfo.
            //
            DebuggerMethodInfo * pMethodInfo = GetOrCreateMethodInfo(pModule, token);

            if (pMethodInfo == NULL)
            {
                pEvent->hr = E_OUTOFMEMORY;
            }
            else
            {
                bool fStatus = pMethodInfo->IsJMCFunction();
                pEvent->SetJMCFunctionStatus.dwStatus = fStatus;
                pEvent->hr = S_OK;
            }

            m_pRCThread->SendIPCReply();
        }
        break;

    case DB_IPCE_SET_MODULE_JMC_STATUS:
        {
            // Get data out of event
            DebuggerModule * pDebuggerModule = LookupOrCreateModule(pEvent->SetJMCFunctionStatus.vmDomainFile);

            bool fStatus = (pEvent->SetJMCFunctionStatus.dwStatus != 0);

            // Prepare reply
            pEvent = m_pRCThread->GetIPCEventReceiveBuffer();

            InitIPCReply(pEvent, DB_IPCE_SET_MODULE_JMC_STATUS_RESULT);

            pEvent->hr = S_OK;

            if (pDebuggerModule->HasAnyOptimizedCode() && fStatus)
            {
                // If there's optimized code, then we can't be set JMC status to true.
                // That's because JMC probes are not injected in optimized code, and we
                // need a JMC probe to have a JMC function.
                pEvent->hr = CORDBG_E_CANT_SET_TO_JMC;
            }
            else
            {
                g_pDebugger->SetModuleDefaultJMCStatus(pDebuggerModule->GetRuntimeModule(), fStatus);
            }



            // Send reply
            m_pRCThread->SendIPCReply();
        }
        break;


    case DB_IPCE_INTERCEPT_EXCEPTION:
        GetAndSendInterceptCommand(pEvent);
        break;

    case DB_IPCE_RESOLVE_UPDATE_METADATA_1:
        {

            LOG((LF_CORDB, LL_INFO10000, "D::HIPCE Handling DB_IPCE_RESOLVE_UPDATE_METADATA_1\n"));
            // This isn't ideal - Making SerializeModuleMetaData not call new is hard,
            // but the odds of trying to load a module after a thread is stopped w/
            // the heap lock should be pretty low.
            // All of the metadata calls can violate this and call new.
            SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

            Module * pModule = pEvent->MetadataUpdateRequest.vmModule.GetRawPtr();
            LOG((LF_CORDB, LL_INFO100000, "D::HIPCE Got module 0x%x\n", pModule));

            DWORD countBytes = 0;

            // This will allocate memory. Debugger will then copy from here and send a
            // DB_IPCE_RESOLVE_UPDATE_METADATA_2 to free this memory.
            BYTE* pData = NULL;
            EX_TRY
            {
                LOG((LF_CORDB, LL_INFO100000, "D::HIPCE Calling SerializeModuleMetaData\n"));
                pData = SerializeModuleMetaData(pModule, &countBytes);

            }
            EX_CATCH_HRESULT(hr);

            LOG((LF_CORDB, LL_INFO100000, "D::HIPCE hr is 0x%x\n", hr));

            DebuggerIPCEvent * pResult = m_pRCThread->GetIPCEventReceiveBuffer();
            InitIPCEvent(pResult, DB_IPCE_RESOLVE_UPDATE_METADATA_1_RESULT, NULL, NULL);

            pResult->MetadataUpdateRequest.pMetadataStart = pData;
            pResult->MetadataUpdateRequest.nMetadataSize = countBytes;
            pResult->hr = hr;
            LOG((LF_CORDB, LL_INFO1000000, "D::HIPCE metadataStart=0x%x, nMetadataSize=0x%x\n", pData, countBytes));

            m_pRCThread->SendIPCReply();
            LOG((LF_CORDB, LL_INFO1000000, "D::HIPCE reply sent\n"));
        }
        break;

    case DB_IPCE_RESOLVE_UPDATE_METADATA_2:
        {
            // Delete memory allocated with DB_IPCE_RESOLVE_UPDATE_METADATA_1.
            BYTE * pData = (BYTE *) pEvent->MetadataUpdateRequest.pMetadataStart;
            DeleteInteropSafe(pData);

            DebuggerIPCEvent * pResult = m_pRCThread->GetIPCEventReceiveBuffer();
            InitIPCEvent(pResult, DB_IPCE_RESOLVE_UPDATE_METADATA_2_RESULT, NULL, NULL);
            pResult->hr = S_OK;
            m_pRCThread->SendIPCReply();
        }

        break;

    default:
        // We should never get an event that we don't know about.
        CONSISTENCY_CHECK_MSGF(false, ("Unknown Debug-Event on LS:id=0x%08x.", pEvent->type));
        LOG((LF_CORDB, LL_INFO10000, "Unknown event type: 0x%08x\n",
             pEvent->type));
    }

    STRESS_LOG0(LF_CORDB, LL_INFO10000, "D::HIPCE: finished handling event\n");

    if (lockedThreadStore)
    {
        ThreadSuspend::UnlockThreadStore(FALSE, ThreadSuspend::SUSPEND_FOR_DEBUGGER);
    }
    // dbgLockHolder goes out of scope - implicit Release
    return fContinue;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

/*
 * GetAndSendInterceptCommand
 *
 * This function processes an INTERCEPT_EXCEPTION IPC event, sending the appropriate response.
 *
 * Parameters:
 *   event - the event to process.
 *
 * Returns:
 *   hr - HRESULT.
 *
 */
HRESULT Debugger::GetAndSendInterceptCommand(DebuggerIPCEvent *event)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    _ASSERTE((event->type & DB_IPCE_TYPE_MASK) == DB_IPCE_INTERCEPT_EXCEPTION);

    //
    // Simple state validation first.
    //
    Thread *pThread = event->InterceptException.vmThreadToken.GetRawPtr();

    if ((pThread != NULL) &&
        !m_forceNonInterceptable &&
        IsInterceptableException(pThread))
    {
        ThreadExceptionState* pExState = pThread->GetExceptionState();

        // We can only have one interception going on at any given time.
        if (!pExState->GetFlags()->DebuggerInterceptInfo())
        {
            //
            // Now start processing the parameters from the event.
            //
            FramePointer targetFramePointer = event->InterceptException.frameToken;

            ControllerStackInfo csi;

            // Safe because we're stopped.
            StackTraceTicket ticket(pThread);
            csi.GetStackInfo(ticket, pThread, targetFramePointer, NULL);

            if (csi.m_targetFrameFound)
            {
                //
                // If the target frame is below the point where the current exception was
                // thrown from, then we should reject this interception command.  This
                // can happen in a func-eval during an exception callback, or during a
                // breakpoint in a filter function.  Or it can just be a user error.
                //
                CONTEXT* pContext = pExState->GetContextRecord();

                // This is an approximation on IA64, where we should use the caller SP instead of
                // the current SP.  However, if the targetFramePointer is valid, the comparison should
                // still work.  targetFramePointer should be valid because it ultimately comes from a
                // full stackwalk.
                FramePointer excepFramePointer = FramePointer::MakeFramePointer(GetSP(pContext));

                if (IsCloserToRoot(excepFramePointer, targetFramePointer))
                {
                    hr = CORDBG_E_CURRENT_EXCEPTION_IS_OUTSIDE_CURRENT_EXECUTION_SCOPE;
                    goto LSendResponse;
                }


                //
                // If the instruction that faulted is not in this managed code, at the leaf
                // frame, then the IP is actually the return address from the managed or
                // unmanaged function that really did fault.  Thus, we actually want the
                // IP of the call instruction.  I fake this by simply subtracting 1 from
                // the IP, which is close enough approximation for the search below.
                //
                if (pExState->GetContextRecord() != NULL)
                {
                    // If the faulting instruction is not in managed code, then the interception frame
                    // must be non-leaf.
                    if (!g_pEEInterface->IsManagedNativeCode((BYTE *)(GetIP(pExState->GetContextRecord()))))
                    {
                        csi.m_activeFrame.relOffset--;
                    }
                    else
                    {
                        MethodDesc *pMethodDesc = g_pEEInterface->GetNativeCodeMethodDesc(dac_cast<PCODE>(GetIP(pExState->GetContextRecord())));

                        // check if the interception frame is the leaf frame
                        if ((pMethodDesc == NULL) ||
                            (pMethodDesc != csi.m_activeFrame.md) ||
                            (GetSP(pExState->GetContextRecord()) != GetRegdisplaySP(&(csi.m_activeFrame.registers))))
                        {
                            csi.m_activeFrame.relOffset--;
                        }
                    }
                }

                //
                // Now adjust the IP to be the previous zero-stack depth sequence point.
                //
                SIZE_T foundOffset = 0;
                DebuggerJitInfo *pJitInfo = csi.m_activeFrame.GetJitInfoFromFrame();

                if (pJitInfo != NULL)
                {
                    ICorDebugInfo::SourceTypes src;

                    ULONG relOffset = csi.m_activeFrame.relOffset;

#if defined(FEATURE_EH_FUNCLETS)
                    int funcletIndex = PARENT_METHOD_INDEX;

                    // For funclets, we need to make sure that the stack empty sequence point we use is
                    // in the same funclet as the current offset.
                    if (csi.m_activeFrame.IsFuncletFrame())
                    {
                        funcletIndex = pJitInfo->GetFuncletIndex(relOffset, DebuggerJitInfo::GFIM_BYOFFSET);
                    }

                    // Refer to the loop using pMap below.
                    DebuggerILToNativeMap* pMap = NULL;
#endif // FEATURE_EH_FUNCLETS

                    for (unsigned int i = 0; i < pJitInfo->GetSequenceMapCount(); i++)
                    {
                        SIZE_T startOffset = pJitInfo->GetSequenceMap()[i].nativeStartOffset;

                        if (DbgIsSpecialILOffset(pJitInfo->GetSequenceMap()[i].ilOffset))
                        {
                            LOG((LF_CORDB, LL_INFO10000,
                                    "D::HIPCE: not placing breakpoint at special offset 0x%x\n", startOffset));
                            continue;
                        }

                        if ((i >= 1) && (startOffset == pJitInfo->GetSequenceMap()[i-1].nativeStartOffset))
                        {
                            LOG((LF_CORDB, LL_INFO10000,
                                 "D::HIPCE: not placing redundant breakpoint at duplicate offset 0x%x\n", startOffset));
                            continue;
                        }

                        if (startOffset > relOffset)
                        {
                            LOG((LF_CORDB, LL_INFO10000,
                                 "D::HIPCE: Stopping scan for breakpoint at offset 0x%x\n", startOffset));
                            continue;
                        }

                        src = pJitInfo->GetSequenceMap()[i].source;

                        if (!(src & ICorDebugInfo::STACK_EMPTY))
                        {
                            LOG((LF_CORDB, LL_INFO10000, "D::HIPCE: not placing E&C breakpoint at offset "
                                    "0x%x b/c not STACK_EMPTY:it's 0x%x\n", startOffset, src));
                            continue;
                        }

                        if ((foundOffset < startOffset) && (startOffset <= relOffset)
#if defined(FEATURE_EH_FUNCLETS)
                            // Check if we are still in the same funclet.
                            && (funcletIndex == pJitInfo->GetFuncletIndex(startOffset, DebuggerJitInfo::GFIM_BYOFFSET))
#endif // FEATURE_EH_FUNCLETS
                           )
                        {
                            LOG((LF_CORDB, LL_INFO10000, "D::HIPCE: updating breakpoint at native offset 0x%x\n",
                                 startOffset));
                            foundOffset = startOffset;
#if defined(FEATURE_EH_FUNCLETS)
                            // Save the map entry for modification later.
                            pMap = &(pJitInfo->GetSequenceMap()[i]);
#endif // FEATURE_EH_FUNCLETS
                        }
                    }

#if defined(FEATURE_EH_FUNCLETS)
                    // This is nasty.  Starting recently we could have multiple sequence points with the same IL offset
                    // in the SAME funclet/parent method (previously different sequence points with the same IL offset
                    // imply that they are in different funclet/parent method).  Fortunately, we only run into this
                    // if we have a loop which throws a range check failed exception.  The code for throwing the
                    // exception executes out of line (this is JIT-specific, of course).  The following loop makes sure
                    // that when we interecept the exception, we intercept it at the smallest native offset instead
                    // of intercepting it right before we throw the exception.
                    for (/* no initialization */; pMap > pJitInfo->GetSequenceMap() ; pMap--)
                    {
                        if (pMap->ilOffset == (pMap-1)->ilOffset)
                        {
                            foundOffset = (pMap-1)->nativeStartOffset;
                        }
                        else
                        {
                            break;
                        }
                    }
                    _ASSERTE(foundOffset < relOffset);
#endif // FEATURE_EH_FUNCLETS

                    //
                    // Set up a breakpoint on the intercept IP
                    //
                    DebuggerContinuableExceptionBreakpoint *pBreakpoint;

                    pBreakpoint = new (interopsafe, nothrow) DebuggerContinuableExceptionBreakpoint(pThread,
                                                                                                    foundOffset,
                                                                                                    pJitInfo,
                                                                                                    csi.m_activeFrame.currentAppDomain
                                                                                                   );

                    if (pBreakpoint != NULL)
                    {
                        //
                        // Set up the VM side of intercepting.
                        //
                        if (pExState->GetDebuggerState()->SetDebuggerInterceptInfo(csi.m_activeFrame.pIJM,
                                                              pThread,
                                                              csi.m_activeFrame.MethodToken,
                                                              csi.m_activeFrame.md,
                                                              foundOffset,
#if defined (TARGET_ARM )|| defined (TARGET_ARM64 )
                                                              // ARM requires the caller stack pointer, not the current stack pointer
                                                              CallerStackFrame::FromRegDisplay(&(csi.m_activeFrame.registers)),
#else
                                                              StackFrame::FromRegDisplay(&(csi.m_activeFrame.registers)),
#endif
                                                              pExState->GetFlags()
                                                             ))
                        {
                            //
                            // Make sure no more exception callbacks come thru.
                            //
                            pExState->GetFlags()->SetSentDebugFirstChance();
                            pExState->GetFlags()->SetSentDebugUserFirstChance();
                            pExState->GetFlags()->SetSentDebugUnwindBegin();

                            //
                            // Save off this breakpoint, so that if the exception gets unwound before we hit
                            // the breakpoint - the exception info can call back to remove it.
                            //
                            pExState->GetDebuggerState()->SetDebuggerInterceptContext((void *)pBreakpoint);

                            hr = S_OK;
                        }
                        else // VM could not set up for intercept
                        {
                            DeleteInteropSafe(pBreakpoint);
                            hr = E_INVALIDARG;
                        }

                    }
                    else // could not allocate for breakpoint
                    {
                        hr = E_OUTOFMEMORY;
                    }

                }
                else // could not get JitInfo
                {
                    hr = E_FAIL;
                }

            }
            else // target frame not found.
            {
                hr = E_INVALIDARG;
            }

        }
        else // already set up for an intercept.
        {
            hr = CORDBG_E_INTERCEPT_FRAME_ALREADY_SET;
        }

    }
    else if (pThread == NULL)
    {
        hr = E_INVALIDARG; // pThread is NULL.
    }
    else
    {
        hr = CORDBG_E_NONINTERCEPTABLE_EXCEPTION;
    }

LSendResponse:

    //
    // Prepare reply
    //
    event = m_pRCThread->GetIPCEventReceiveBuffer();
    InitIPCReply(event, DB_IPCE_INTERCEPT_EXCEPTION_RESULT);
    event->hr = hr;

    //
    // Send reply
    //
    m_pRCThread->SendIPCReply();

    return hr;
}

// Poll & wait for the real helper thread to come up.
// It's possible that the helper thread  is blocked by DllMain, and so we can't
// Wait infinite. If this poll does timeout, then it just means we're likely
// go do helper duty instead of have the real helper do it.
void Debugger::PollWaitingForHelper()
{

    LOG((LF_CORDB, LL_INFO10000, "PollWaitingForHelper() start\n"));

    DebuggerIPCControlBlock * pDCB = g_pRCThread->GetDCB();

    PREFIX_ASSUME(pDCB != NULL);

    int nTotalMSToWait = 8 * 1000;

    // Spin waiting for either the real helper thread or a temp. to be ready.
    // This should never timeout unless the helper is blocked on the loader lock.
    while (!pDCB->m_helperThreadId && !pDCB->m_temporaryHelperThreadId)
    {
        STRESS_LOG1(LF_CORDB,LL_INFO1000, "PollWaitForHelper. %d\n", nTotalMSToWait);

        // If we hold the lock, we'll block the helper thread and this poll is not useful
        _ASSERTE(!ThreadHoldsLock());

        const DWORD dwTime = 50;
        ClrSleepEx(dwTime, FALSE);
        nTotalMSToWait -= dwTime;

        if (nTotalMSToWait <= 0)
        {
            LOG((LF_CORDB, LL_INFO10000, "PollWaitingForHelper() timeout\n"));
            return;
        }
    }

    LOG((LF_CORDB, LL_INFO10000, "PollWaitingForHelper() succeed\n"));
    return;
}




void Debugger::TypeHandleToBasicTypeInfo(AppDomain *pAppDomain, TypeHandle th, DebuggerIPCE_BasicTypeData *res)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::THTBTI: converting left-side type handle to basic right-side type info, ELEMENT_TYPE: %d.\n", th.GetSignatureCorElementType()));
    // GetSignatureCorElementType returns E_T_CLASS for E_T_STRING... :-(
    if (th.IsNull())
    {
        res->elementType = ELEMENT_TYPE_VOID;
    }
    else if (th.GetMethodTable() == g_pObjectClass)
    {
        res->elementType = ELEMENT_TYPE_OBJECT;
    }
    else if (th.GetMethodTable() == g_pStringClass)
    {
        res->elementType = ELEMENT_TYPE_STRING;
    }
    else
    {
        res->elementType = th.GetSignatureCorElementType();
    }

    switch (res->elementType)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_FNPTR:
    case ELEMENT_TYPE_BYREF:
        res->vmTypeHandle = WrapTypeHandle(th);
        res->metadataToken = mdTokenNil;
        res->vmDomainFile.SetRawPtr(NULL);
        break;

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:
        {
            res->vmTypeHandle = th.HasInstantiation() ? WrapTypeHandle(th) : VMPTR_TypeHandle::NullPtr();
                                                                             // only set if instantiated
            res->metadataToken = th.GetCl();
            DebuggerModule * pDModule = LookupOrCreateModule(th.GetModule(), pAppDomain);
            res->vmDomainFile.SetRawPtr((pDModule ? pDModule->GetDomainFile() : NULL));
            break;
        }

    default:
        res->vmTypeHandle = VMPTR_TypeHandle::NullPtr();
        res->metadataToken = mdTokenNil;
        res->vmDomainFile.SetRawPtr(NULL);
        break;
    }
    return;
}

void Debugger::TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed,
                                            AppDomain *pAppDomain,
                                            TypeHandle th,
                                            DebuggerIPCE_ExpandedTypeData *res)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (th.IsNull())
    {
        res->elementType = ELEMENT_TYPE_VOID;
    }
    else if (th.GetMethodTable() == g_pObjectClass)
    {
        res->elementType = ELEMENT_TYPE_OBJECT;
    }
    else if (th.GetMethodTable() == g_pStringClass)
    {
        res->elementType = ELEMENT_TYPE_STRING;
    }
    else
    {
    LOG((LF_CORDB, LL_INFO10000, "D::THTETI: converting left-side type handle to expanded right-side type info, ELEMENT_TYPE: %d.\n", th.GetSignatureCorElementType()));
    // GetSignatureCorElementType returns E_T_CLASS for E_T_STRING... :-(
        res->elementType = th.GetSignatureCorElementType();
    }

    switch (res->elementType)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
        _ASSERTE(th.IsArray());
        res->ArrayTypeData.arrayRank = th.GetRank();
        TypeHandleToBasicTypeInfo(pAppDomain,
                                  th.GetArrayElementTypeHandle(),
                                  &(res->ArrayTypeData.arrayTypeArg));
        break;

    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
        if (boxed == AllBoxed)
        {
            res->elementType = ELEMENT_TYPE_CLASS;
            goto treatAllValuesAsBoxed;
        }
        _ASSERTE(th.IsTypeDesc());
        TypeHandleToBasicTypeInfo(pAppDomain,
                                  th.AsTypeDesc()->GetTypeParam(),
                                  &(res->UnaryTypeData.unaryTypeArg));
        break;

    case ELEMENT_TYPE_VALUETYPE:
        if (boxed == OnlyPrimitivesUnboxed || boxed == AllBoxed)
            res->elementType = ELEMENT_TYPE_CLASS;
        FALLTHROUGH;

    case ELEMENT_TYPE_CLASS:
        {
treatAllValuesAsBoxed:
            res->ClassTypeData.typeHandle = th.HasInstantiation() ? WrapTypeHandle(th) : VMPTR_TypeHandle::NullPtr(); // only set if instantiated
            res->ClassTypeData.metadataToken = th.GetCl();
            DebuggerModule * pModule = LookupOrCreateModule(th.GetModule(), pAppDomain);
            res->ClassTypeData.vmDomainFile.SetRawPtr((pModule ? pModule->GetDomainFile() : NULL));
            _ASSERTE(!res->ClassTypeData.vmDomainFile.IsNull());
            break;
        }

    case ELEMENT_TYPE_FNPTR:
        {
            if (boxed == AllBoxed)
            {
                res->elementType = ELEMENT_TYPE_CLASS;
                goto treatAllValuesAsBoxed;
            }
            res->NaryTypeData.typeHandle = WrapTypeHandle(th);
            break;
        }
    default:
        // The element type is sufficient, unless the type is effectively a "boxed"
        // primitive value type...
        if (boxed == AllBoxed)
        {
            res->elementType = ELEMENT_TYPE_CLASS;
            goto treatAllValuesAsBoxed;
        }
        break;
    }
    LOG((LF_CORDB, LL_INFO10000, "D::THTETI: converted left-side type handle to expanded right-side type info, res->ClassTypeData.typeHandle = 0x%08x.\n", res->ClassTypeData.typeHandle.GetRawPtr()));
    return;
}


HRESULT Debugger::BasicTypeInfoToTypeHandle(DebuggerIPCE_BasicTypeData *data, TypeHandle *pRes)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::BTITTH: expanding basic right-side type to left-side type, ELEMENT_TYPE: %d.\n", data->elementType));
    *pRes = TypeHandle();
    TypeHandle th;
    switch (data->elementType)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
        _ASSERTE(!data->vmTypeHandle.IsNull());
        th = GetTypeHandle(data->vmTypeHandle);
        break;

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:
        {
            if (!data->vmTypeHandle.IsNull())
            {
                th = GetTypeHandle(data->vmTypeHandle);
            }
            else
            {
                DebuggerModule *pDebuggerModule = g_pDebugger->LookupOrCreateModule(data->vmDomainFile);

                th = g_pEEInterface->FindLoadedClass(pDebuggerModule->GetRuntimeModule(), data->metadataToken);
            if (th.IsNull())
            {
                LOG((LF_CORDB, LL_INFO10000, "D::ETITTH: class isn't loaded.\n"));
                    return CORDBG_E_CLASS_NOT_LOADED;
            }

            _ASSERTE(th.GetNumGenericArgs() == 0);
            }
            break;
        }

    case ELEMENT_TYPE_FNPTR:
        {
            _ASSERTE(!data->vmTypeHandle.IsNull());
            th = GetTypeHandle(data->vmTypeHandle);
            break;
        }

    default:
        th = g_pEEInterface->FindLoadedElementType(data->elementType);
        break;
    }
    if (th.IsNull())
        return CORDBG_E_CLASS_NOT_LOADED;
    *pRes = th;
    return S_OK;
}

// Iterate through the type argument data, creating type handles as we go.
void Debugger::TypeDataWalk::ReadTypeHandles(unsigned int nTypeArgs, TypeHandle *ppResults)
{
    WRAPPER_NO_CONTRACT;

    for (unsigned int i = 0; i < nTypeArgs; i++)
        ppResults[i] = ReadTypeHandle();
    }

TypeHandle Debugger::TypeDataWalk::ReadInstantiation(Module *pModule, mdTypeDef tok, unsigned int nTypeArgs)
{
    WRAPPER_NO_CONTRACT;

    DWORD dwAllocSize;
    if (!ClrSafeInt<DWORD>::multiply(nTypeArgs, sizeof(TypeHandle), dwAllocSize))
    {
        ThrowHR(COR_E_OVERFLOW);
    }
    TypeHandle * inst = (TypeHandle *) _alloca(dwAllocSize);
    ReadTypeHandles(nTypeArgs, inst) ;
    TypeHandle th = g_pEEInterface->LoadInstantiation(pModule, tok, nTypeArgs, inst);
    if (th.IsNull())
      COMPlusThrow(kArgumentException, W("Argument_InvalidGenericArg"));
    return th;
}

TypeHandle Debugger::TypeDataWalk::ReadTypeHandle()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DebuggerIPCE_TypeArgData * data = ReadOne();
    if (!data)
      COMPlusThrow(kArgumentException, W("Argument_InvalidGenericArg"));

    LOG((LF_CORDB, LL_INFO10000, "D::ETITTH: expanding right-side type to left-side type, ELEMENT_TYPE: %d.\n", data->data.elementType));

    TypeHandle th;
    CorElementType et = data->data.elementType;
    switch (et)
    {
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
        if(data->numTypeArgs == 1)
        {
            TypeHandle typar = ReadTypeHandle();
            switch (et)
            {
            case ELEMENT_TYPE_ARRAY:
            case ELEMENT_TYPE_SZARRAY:
                th = g_pEEInterface->LoadArrayType(data->data.elementType, typar, data->data.ArrayTypeData.arrayRank);
          break;
    case ELEMENT_TYPE_PTR:
    case ELEMENT_TYPE_BYREF:
                th = g_pEEInterface->LoadPointerOrByrefType(data->data.elementType, typar);
          break;
            default:
                _ASSERTE(0);
        }
        }
        break;

    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_VALUETYPE:
        {
            DebuggerModule *pDebuggerModule = g_pDebugger->LookupOrCreateModule(data->data.ClassTypeData.vmDomainFile);
            th = ReadInstantiation(pDebuggerModule->GetRuntimeModule(), data->data.ClassTypeData.metadataToken, data->numTypeArgs);
            break;
        }

    case ELEMENT_TYPE_FNPTR:
        {
            SIZE_T cbAllocSize;
            if ((!ClrSafeInt<SIZE_T>::multiply(data->numTypeArgs, sizeof(TypeHandle), cbAllocSize)) ||
                (cbAllocSize != (size_t)(cbAllocSize)))
            {
                _ASSERTE(COR_E_OVERFLOW);
                cbAllocSize = UINT_MAX;
            }
            TypeHandle * inst = (TypeHandle *) _alloca(cbAllocSize);
            ReadTypeHandles(data->numTypeArgs, inst) ;
            th = g_pEEInterface->LoadFnptrType(inst, data->numTypeArgs);
            break;
        }

    default:
        th = g_pEEInterface->LoadElementType(data->data.elementType);
        break;
    }
    if (th.IsNull())
      COMPlusThrow(kArgumentNullException, W("ArgumentNull_Type"));
    return th;

}

//
// GetAndSendTransitionStubInfo figures out if an address is a stub
// address and sends the result back to the right side.
//
void Debugger::GetAndSendTransitionStubInfo(CORDB_ADDRESS_TYPE *stubAddress)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::GASTSI: IsTransitionStub. Addr=0x%08x\n", stubAddress));

    bool result = false;

    result = g_pEEInterface->IsStub((const BYTE *)stubAddress);


    // If its not a stub, then maybe its an address in mscoree?
    if (result == false)
    {
        result = (IsIPInModule(GetClrModuleBase(), (PCODE)stubAddress) == TRUE);
    }

    // This is a synchronous event (reply required)
    DebuggerIPCEvent *event = m_pRCThread->GetIPCEventReceiveBuffer();
    InitIPCEvent(event, DB_IPCE_IS_TRANSITION_STUB_RESULT, NULL, NULL);
    event->IsTransitionStubResult.isStub = result;

    // Send the result
    m_pRCThread->SendIPCReply();
}

/*
 * A generic request for a buffer in the left-side for use by the right-side
 *
 * This is a synchronous event (reply required).
 */
HRESULT Debugger::GetAndSendBuffer(DebuggerRCThread* rcThread, ULONG bufSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // This is a synchronous event (reply required)
    DebuggerIPCEvent* event = rcThread->GetIPCEventReceiveBuffer();
    PREFIX_ASSUME(event != NULL);
    InitIPCEvent(event, DB_IPCE_GET_BUFFER_RESULT, NULL, NULL);

    // Allocate the buffer
    event->GetBufferResult.hr = AllocateRemoteBuffer( bufSize, &event->GetBufferResult.pBuffer );

    // Send the result
    return rcThread->SendIPCReply();
}

/*
 * Allocate a buffer in the left-side for use by the right-side
 */
HRESULT Debugger::AllocateRemoteBuffer( ULONG bufSize, void **ppBuffer )
    {
    CONTRACTL
        {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // The call to Append below will call CUnorderedArray, which will call unsafe New.
    HRESULT hr;
    CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(&hr, GetCanary());
    if( FAILED(hr) )
            {
        return hr;
            }

    // Actually allocate the buffer
    BYTE* pBuffer = new (interopsafe, nothrow) BYTE[bufSize];

    LOG((LF_CORDB, LL_EVERYTHING, "D::ARB: new'd 0x%x\n", *ppBuffer));

    // Check for out of memory error
    if (pBuffer == NULL)
            {
        return E_OUTOFMEMORY;
        }

    // Track the allocation so we can free it later
    void **ppNextBlob = GetMemBlobs()->Append();
    if( ppNextBlob == NULL )
    {
        DeleteInteropSafe( pBuffer );
        return E_OUTOFMEMORY;
    }
   *ppNextBlob = pBuffer;

   // Return the allocated memory
   *ppBuffer = pBuffer;
   return S_OK;
}

/*
 * Used to release a previously-requested buffer
 *
 * This is a synchronous event (reply required).
 */
HRESULT Debugger::SendReleaseBuffer(DebuggerRCThread* rcThread, void *pBuffer)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000, "D::SRB for buffer 0x%x\n", pBuffer));

    // This is a synchronous event (reply required)
    DebuggerIPCEvent* event = rcThread->GetIPCEventReceiveBuffer();
    PREFIX_ASSUME(event != NULL);
    InitIPCEvent(event, DB_IPCE_RELEASE_BUFFER_RESULT, NULL, NULL);

    _ASSERTE(pBuffer != NULL);

    // Free the memory
    ReleaseRemoteBuffer(pBuffer, true);

    // Indicate success in reply
    event->ReleaseBufferResult.hr = S_OK;

    // Send the result
    return rcThread->SendIPCReply();
}


//
// Used to delete the buffer previously-requested  by the right side.
// We've factored the code since both the ~Debugger and SendReleaseBuffer
// methods do this.
//
HRESULT Debugger::ReleaseRemoteBuffer(void *pBuffer, bool removeFromBlobList)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_EVERYTHING, "D::RRB: Releasing RS-alloc'd buffer 0x%x\n", pBuffer));

    // Remove the buffer from the blob list if necessary.
    if (removeFromBlobList)
    {
        USHORT cBlobs = GetMemBlobs()->Count();
        void **rgpBlobs = GetMemBlobs()->Table();

        USHORT i;
        for (i = 0; i < cBlobs; i++)
        {
            if (rgpBlobs[i] == pBuffer)
            {
                GetMemBlobs()->DeleteByIndex(i);
                break;
            }
        }

        // We should have found a match.  All buffers passed to ReleaseRemoteBuffer
        // should have been allocated with AllocateRemoteBuffer and not yet freed.
        _ASSERTE( i < cBlobs );
    }

    // Delete the buffer. (Need cast for GCC template support)
    DeleteInteropSafe( (BYTE*)pBuffer );

    return S_OK;
}

//
// UnrecoverableError causes the Left Side to enter a state where no more
// debugging can occur and we leave around enough information for the
// Right Side to tell what happened.
//
void Debugger::UnrecoverableError(HRESULT errorHR,
                                  unsigned int errorCode,
                                  const char *errorFile,
                                  unsigned int errorLine,
                                  bool exitThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10,
         "Unrecoverable error: hr=0x%08x, code=%d, file=%s, line=%d\n",
         errorHR, errorCode, errorFile, errorLine));

    //
    // Setting this will ensure that not much else happens...
    //
    m_unrecoverableError = TRUE;

    //
    // Fill out the control block with the error.
    // in-proc will find out when the function fails
    //
    DebuggerIPCControlBlock *pDCB = m_pRCThread->GetDCB();

    PREFIX_ASSUME(pDCB != NULL);

    pDCB->m_errorHR = errorHR;
    pDCB->m_errorCode = errorCode;

    //
    // If we're told to, exit the thread.
    //
    if (exitThread)
    {
        LOG((LF_CORDB, LL_INFO10,
             "Thread exiting due to unrecoverable error.\n"));
        ExitThread(errorHR);
    }
}

//
// Callback for IsThreadAtSafePlace's stack walk.
//
StackWalkAction Debugger::AtSafePlaceStackWalkCallback(CrawlFrame *pCF,
                                                       VOID* data)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(pCF));
        PRECONDITION(CheckPointer(data));
    }
    CONTRACTL_END;

    bool *atSafePlace = (bool*)data;
    LOG((LF_CORDB, LL_INFO100000, "D:AtSafePlaceStackWalkCallback\n"));

    if (pCF->IsFrameless() && pCF->IsActiveFunc())
    {
        LOG((LF_CORDB, LL_INFO1000000, "D:AtSafePlaceStackWalkCallback, IsFrameLess() and IsActiveFunc()\n"));
        if (g_pEEInterface->CrawlFrameIsGcSafe(pCF))
        {
            LOG((LF_CORDB, LL_INFO1000000, "D:AtSafePlaceStackWalkCallback - TRUE: CrawlFrameIsGcSafe()\n"));
            *atSafePlace = true;
        }
    }
    return SWA_ABORT;
}

//
// Determine, via a quick one frame stack walk, if a given thread is
// in a gc safe place.
//
bool Debugger::IsThreadAtSafePlaceWorker(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;

    bool atSafePlace = false;

    // Setup our register display.
    REGDISPLAY rd;
    CONTEXT *context = g_pEEInterface->GetThreadFilterContext(thread);

    _ASSERTE(!(g_pEEInterface->GetThreadFilterContext(thread) && ISREDIRECTEDTHREAD(thread)));
    if (context != NULL)
    {
        g_pEEInterface->InitRegDisplay(thread, &rd, context, TRUE);
    }
    else
    {
        CONTEXT ctx;
        ZeroMemory(&rd, sizeof(rd));
        ZeroMemory(&ctx, sizeof(ctx));
#if defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
        rd.ControlPC = ctx.Eip;
        rd.PCTAddr = (TADDR)&(ctx.Eip);
#else
        FillRegDisplay(&rd, &ctx);
#endif

        if (ISREDIRECTEDTHREAD(thread))
        {
            thread->GetFrame()->UpdateRegDisplay(&rd);
        }
    }

    // Do the walk. If it fails, we don't care, because we default
    // atSafePlace to false.
    g_pEEInterface->StackWalkFramesEx(
                                 thread,
                                 &rd,
                                 Debugger::AtSafePlaceStackWalkCallback,
                                 (VOID*)(&atSafePlace),
                                 QUICKUNWIND | HANDLESKIPPEDFRAMES |
                                 DISABLE_MISSING_FRAME_DETECTION | SKIP_GSCOOKIE_CHECK);

#ifdef LOGGING
    if (!atSafePlace)
        LOG((LF_CORDB | LF_GC, LL_INFO1000,
             "Thread 0x%x is not at a safe place.\n",
             GetThreadIdHelper(thread)));
#endif

    return atSafePlace;
}

bool Debugger::IsThreadAtSafePlace(Thread *thread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(thread));
    }
    CONTRACTL_END;


    if (m_fShutdownMode)
    {
        return true;
    }

    // <TODO>
    //
    // Make sure this fix is evaluated when doing real work for debugging SO handling.
    //
    // On the Stack Overflow code path calling IsThreadAtSafePlaceWorker as it is
    // currently implemented is way too stack intensive. For now we cheat and just
    // say that if a thread is in the middle of handling a SO it is NOT at a safe
    // place. This is a reasonably safe assumption to make and hopefully shouldn't
    // result in deadlocking the debugger.
    if ( (thread->IsExceptionInProgress()) &&
         (g_pEEInterface->GetThreadException(thread) == CLRException::GetPreallocatedStackOverflowExceptionHandle()) )
    {
        return false;
    }
    // </TODO>
    else
    {
        return IsThreadAtSafePlaceWorker(thread);
    }
}

//-----------------------------------------------------------------------------
// Get the complete user state flags.
// This will collect flags both from the EE and from the LS.
// This is the real implementation of the RS's ICorDebugThread::GetUserState().
//
// Parameters:
//    pThread - non-null thread to get state for.
//
// Returns: a CorDebugUserState flags enum describing state.
//-----------------------------------------------------------------------------
CorDebugUserState Debugger::GetFullUserState(Thread *pThread)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pThread));
    }
    CONTRACTL_END;

    CorDebugUserState state = g_pEEInterface->GetPartialUserState(pThread);

    bool fSafe = IsThreadAtSafePlace(pThread);
    if (!fSafe)
    {
        state = (CorDebugUserState) (state | USER_UNSAFE_POINT);
    }

    return state;
}

/******************************************************************************
 *
 * Helper for debugger to get an unique thread id
 *
 ******************************************************************************/
DWORD Debugger::GetThreadIdHelper(Thread *pThread)
{
    WRAPPER_NO_CONTRACT;

    return pThread->GetOSThreadId();
}

//-----------------------------------------------------------------------------
// Called by EnC during remapping to get information about the local vars.
// EnC will then use this to set values in the new version to their corresponding
// values from the old version.
//
// Returns a pointer to the debugger's copies of the maps. Caller
// does not own the memory provided via vars outparameter.
//-----------------------------------------------------------------------------
void Debugger::GetVarInfo(MethodDesc *       fd,   // [IN] method of interest
                    void *DebuggerVersionToken,    // [IN] which edit version
                    SIZE_T *           cVars,      // [OUT] size of 'vars'
                    const ICorDebugInfo::NativeVarInfo **vars     // [OUT] map telling where local vars are stored
                    )
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

    DebuggerJitInfo * ji = (DebuggerJitInfo *)DebuggerVersionToken;

    // If we didn't supply a DJI, then we're asking for the most recent version.
    if (ji == NULL)
    {
        ji = GetLatestJitInfoFromMethodDesc(fd);
    }
    _ASSERTE(fd == ji->m_nativeCodeVersion.GetMethodDesc());

    PREFIX_ASSUME(ji != NULL);

    *vars = ji->GetVarNativeInfo();
    *cVars = ji->GetVarNativeInfoCount();
}

#include "openum.h"

#ifdef EnC_SUPPORTED

//---------------------------------------------------------------------------------------
//
// Apply an EnC edit to the CLR datastructures and send the result event to the
// debugger right-side.
//
// Arguments:
//    pDebuggerModule  - the module in which the edit should occur
//    cbMetadata       - the number of bytes in pMetadata
//    pMetadata        - pointer to the delta metadata
//    cbIL             - the number of bytes in pIL
//    pIL              - pointer to the delta IL
//
// Return Value:
//
// Assumptions:
//
// Notes:
//
// This is just the first half of processing an EnC request (hot swapping).  This updates
// the metadata and other CLR data structures to reflect the edit, but does not directly
// affect code which is currently running.  In order to achieve on-stack replacement
// (remap of running code), we mine all old methods with "EnC remap breakpoints"
// (instances of DebuggerEnCBreakpoint) at many sequence points.  When one of those
// breakpoints is hit, we give the debugger a RemapOpportunity event and give it a
// chance to remap the execution to the new version of the method.
//

HRESULT Debugger::ApplyChangesAndSendResult(DebuggerModule * pDebuggerModule,
                                            DWORD cbMetadata,
                                            BYTE *pMetadata,
                                            DWORD cbIL,
                                            BYTE *pIL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // @todo - if EnC never works w/ interop, caller New on the helper thread may be ok.
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;

    HRESULT hr = S_OK;

    LOG((LF_ENC, LL_INFO100, "Debugger::ApplyChangesAndSendResult\n"));

    Module *pModule = pDebuggerModule->GetRuntimeModule();
    if (! pModule->IsEditAndContinueEnabled())
    {
        hr =  CORDBG_E_ENC_MODULE_NOT_ENC_ENABLED;
    }
    else
    {
        // Violation with the following call stack:
        //                CONTRACT in MethodTableBuilder::InitMethodDesc
        //                CONTRACT in EEClass::AddMethod
        //                CONTRACT in EditAndContinueModule::AddMethod
        //                CONTRACT in EditAndContinueModule::ApplyEditAndContinue
        //                CONTRACT in EEDbgInterfaceImpl::EnCApplyChanges
        //   VIOLATED-->  CONTRACT in Debugger::ApplyChangesAndSendResult
        CONTRACT_VIOLATION(GCViolation);

        // Tell the VM to apply the edit
        hr = g_pEEInterface->EnCApplyChanges(
            (EditAndContinueModule*)pModule, cbMetadata, pMetadata, cbIL, pIL);
    }

    LOG((LF_ENC, LL_INFO100, "Debugger::ApplyChangesAndSendResult 2\n"));

    DebuggerIPCEvent* event = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(event,
                 DB_IPCE_APPLY_CHANGES_RESULT,
                 NULL,
                 NULL);

    event->ApplyChangesResult.hr = hr;

    // Send the result
    return m_pRCThread->SendIPCEvent();
}

//
// This structure is used to hold a list of the sequence points in a function and
// determine which should have remap breakpoints applied to them for EnC
//
class EnCSequencePointHelper
{
public:
    // Calculates remap info given the supplied JitInfo
    EnCSequencePointHelper(DebuggerJitInfo *pJitInfo);
    ~EnCSequencePointHelper();

    // Returns true if the specified sequence point (given by it's index in the
    // sequence point table in the JitInfo) should get an EnC remap breakpoint.
    BOOL ShouldSetRemapBreakpoint(unsigned int offsetIndex);

private:
    DebuggerJitInfo *m_pJitInfo;

    DebugOffsetToHandlerInfo *m_pOffsetToHandlerInfo;
};

//
// Goes through the list of sequence points for a function and determines whether or not each
// is a valid Remap Breakpoint location (not in a special offset, must be empty stack, and not in a handler.
//
EnCSequencePointHelper::EnCSequencePointHelper(DebuggerJitInfo *pJitInfo)
    : m_pJitInfo(pJitInfo),
    m_pOffsetToHandlerInfo(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_pJitInfo->GetSequenceMapCount() == 0)
    {
        return;
    }

    // Construct a list of native offsets we may want to place EnC breakpoints at
    m_pOffsetToHandlerInfo = new DebugOffsetToHandlerInfo[m_pJitInfo->GetSequenceMapCount()];
    for (unsigned int i = 0; i < m_pJitInfo->GetSequenceMapCount(); i++)
    {
        // By default this slot is unused.  We want the indexes in m_pOffsetToHandlerInfo
        // to correspond to the indexes of m_pJitInfo->GetSequenceMapCount, so we rely
        // on a -1 offset to indicate that a DebuggerOffsetToHandlerInfo is unused.
        // However, it would be cleaner and permit a simpler API to the EE if we just
        // had an array mapping the offsets instead.
        m_pOffsetToHandlerInfo[i].offset = (SIZE_T) -1;
        m_pOffsetToHandlerInfo[i].isInFilterOrHandler = FALSE;

        SIZE_T offset = m_pJitInfo->GetSequenceMap()[i].nativeStartOffset;

        // Check if this is a "special" IL offset, such as representing the prolog or eppilog,
        // or other region not directly mapped to native code.
        if (DbgIsSpecialILOffset(pJitInfo->GetSequenceMap()[i].ilOffset))
        {
            LOG((LF_ENC, LL_INFO10000,
                 "D::UF: not placing E&C breakpoint at special offset 0x%x (IL: 0x%x)\n",
                 offset, m_pJitInfo->GetSequenceMap()[i].ilOffset));
            continue;
        }

        // Skip duplicate sequence points
        if (i >=1 && offset == pJitInfo->GetSequenceMap()[i-1].nativeStartOffset)
        {
            LOG((LF_ENC, LL_INFO10000,
                 "D::UF: not placing redundant E&C "
                 "breakpoint at duplicate offset 0x%x (IL: 0x%x)\n",
                 offset, m_pJitInfo->GetSequenceMap()[i].ilOffset));
            continue;
        }

        // Skip sequence points that aren't due to the evaluation stack being empty
        // We can only remap at stack-empty points (since we don't have a mapping for
        // contents of the evaluation stack).
        if (!(pJitInfo->GetSequenceMap()[i].source & ICorDebugInfo::STACK_EMPTY))
        {
            LOG((LF_ENC, LL_INFO10000,
                 "D::UF: not placing E&C breakpoint at offset "
                 "0x%x (IL: 0x%x) b/c not STACK_EMPTY:it's 0x%x\n", offset,
                 m_pJitInfo->GetSequenceMap()[i].ilOffset, pJitInfo->GetSequenceMap()[i].source));
            continue;
        }

        // So far this sequence point looks good, so store it's native offset so we can get
        // EH information about it from the EE.
        LOG((LF_ENC, LL_INFO10000,
             "D::UF: possibly placing E&C breakpoint at offset "
             "0x%x (IL: 0x%x)\n", offset, m_pJitInfo->GetSequenceMap()[i].ilOffset));
        m_pOffsetToHandlerInfo[i].offset = m_pJitInfo->GetSequenceMap()[i].nativeStartOffset;

    }

    // Ask the EE to fill in the isInFilterOrHandler bit for the native offsets we're interested in
    g_pEEInterface->DetermineIfOffsetsInFilterOrHandler(
        (BYTE *)pJitInfo->m_addrOfCode, m_pOffsetToHandlerInfo, m_pJitInfo->GetSequenceMapCount());
}

EnCSequencePointHelper::~EnCSequencePointHelper()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_pOffsetToHandlerInfo)
    {
        delete m_pOffsetToHandlerInfo;
    }
}

//
// Returns if we should set a remap breakpoint at a given offset.  We only set them at 0-depth stack
// and not when inside a handler, either finally, filter, or catch
//
BOOL EnCSequencePointHelper::ShouldSetRemapBreakpoint(unsigned int offsetIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    {
        // GetSequenceMapCount calls LazyInitBounds() which can eventually
        // call ExecutionManager::IncrementReader
        CONTRACT_VIOLATION(TakesLockViolation);
        _ASSERTE(offsetIndex <= m_pJitInfo->GetSequenceMapCount());
    }

    // If this slot is unused (offset -1), we excluded it early
    if (m_pOffsetToHandlerInfo[offsetIndex].offset == (SIZE_T) -1)
    {
        return FALSE;
    }

    // Otherwise, check the isInFilterOrHandler bit
    if (m_pOffsetToHandlerInfo[offsetIndex].isInFilterOrHandler)
    {
        LOG((LF_ENC, LL_INFO10000,
             "D::UF: not placing E&C breakpoint in filter/handler at offset 0x%x\n",
             m_pOffsetToHandlerInfo[offsetIndex].offset));
        return FALSE;
    }

    return TRUE;
}


//-----------------------------------------------------------------------------
// For each function that's EnC-ed, the EE will call either UpdateFunction
// (if the function already is loaded + jitted) or AddFunction
//
// This is called before the EE updates the MethodDesc, so pMD does not yet
// point to the version we'll be remapping to.
//-----------------------------------------------------------------------------
HRESULT Debugger::UpdateFunction(MethodDesc* pMD, SIZE_T encVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
        PRECONDITION(ThisIsHelperThread()); // guarantees we're serialized.
        PRECONDITION(IsStopped());
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::UF: updating "
         "%s::%s to version %d\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, encVersion));

    // tell the RS that this function has been updated so that it can create new CorDBFunction
    Module *pModule = g_pEEInterface->MethodDescGetModule(pMD);
    _ASSERTE(pModule != NULL);
    mdToken methodDef = pMD->GetMemberDef();
    SendEnCUpdateEvent(DB_IPCE_ENC_UPDATE_FUNCTION,
                       pModule,
                       methodDef,
                       pMD->GetMethodTable()->GetCl(),
                       encVersion);

    DebuggerMethodInfo *dmi = GetOrCreateMethodInfo(pModule, methodDef);
    if (dmi == NULL)
    {
        return E_OUTOFMEMORY;
    }

    // The DMI always holds the most current EnC version number. We always JIT the most
    // current version of the function, so when we do see a JitBegin we will create a new
    // dji for it and stash the current version there. We don't want to change the current
    // jit info because it has to maintain the version for the code it corresponds to.
    dmi->SetCurrentEnCVersion(encVersion);

    // This is called before the MethodDesc is updated to point to the new function.
    // So this call will get the most recent old function.
    DebuggerJitInfo *pJitInfo = GetLatestJitInfoFromMethodDesc(pMD);

    if (pJitInfo == NULL )
    {
        LOG((LF_CORDB,LL_INFO10000,"Unable to get DJI by recently "
            "D::UF: JITted version number (it hasn't been jitted yet),"
            "which is fine\n"));
        return S_OK;
    }

    //
    // Mine the old version of the method with patches so that we can provide
    // remap opportunities whenever the old version of the method is executed.
    //

    if (pJitInfo->m_encBreakpointsApplied)
    {
        LOG((LF_CORDB,LL_INFO10000,"D::UF: Breakpoints already applied\n"));
        return S_OK;
    }

    LOG((LF_CORDB,LL_INFO10000,"D::UF: Applying breakpoints\n"));

    // We only place the patches if we have jit info for this
    // function, i.e., its already been jitted. Otherwise, the EE will
    // pickup the new method on the next JIT anyway.

    EnCSequencePointHelper sequencePointHelper(pJitInfo);

    // For each offset in the IL->Native map, set a new EnC breakpoint on the
    // ones that we know could be remap points.
    for (unsigned int i = 0; i < pJitInfo->GetSequenceMapCount(); i++)
    {
        // Skip if this isn't a valid remap point (eg. is in an exception handler)
        if (! sequencePointHelper.ShouldSetRemapBreakpoint(i))
        {
            continue;
        }

        SIZE_T offset = pJitInfo->GetSequenceMap()[i].nativeStartOffset;

        LOG((LF_CORDB, LL_INFO10000,
             "D::UF: placing E&C breakpoint at native offset 0x%x\n",
             offset));

        DebuggerEnCBreakpoint *bp;

        // Create and activate a new EnC remap breakpoint here in the old version of the method
        bp = new (interopsafe) DebuggerEnCBreakpoint( offset,
                                                      pJitInfo,
                                                      DebuggerEnCBreakpoint::REMAP_PENDING,
                                                     (AppDomain *)pModule->GetDomain());

        _ASSERTE(bp != NULL);
    }

    pJitInfo->m_encBreakpointsApplied = true;

    return S_OK;
}

// Called to update a function that hasn't yet been loaded (and so we don't have a MethodDesc).
// This may be updating an existing function  on a type that hasn't been loaded
// or adding a new function to a type that hasn't been loaded.
// We need to notify the debugger so that it can properly track version info.
HRESULT Debugger::UpdateNotYetLoadedFunction(mdMethodDef token, Module * pModule, SIZE_T encVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(ThisIsHelperThread());
        PRECONDITION(ThreadHoldsLock()); // must have lock since we're on helper and stopped.
    }
    CONTRACTL_END;

    DebuggerMethodInfo *dmi = GetOrCreateMethodInfo(pModule, token);
    if (! dmi)
    {
        return E_OUTOFMEMORY;
    }
    dmi->SetCurrentEnCVersion(encVersion);


    // Must tell the RS that this function has been added so that it can create new CorDBFunction.
    mdTypeDef classToken = 0;

    HRESULT hr = pModule->GetMDImport()->GetParentToken(token, &classToken);
    if (FAILED(hr))
    {
        // We never expect this to actually fail, but just in case it does for some other strange reason,
        // we'll return before we AV.
        CONSISTENCY_CHECK_MSGF(false, ("Class lookup failed:mdToken:0x%08x, pModule=%p. hr=0x%08x\n", token, pModule, hr));
        return hr;
    }

    SendEnCUpdateEvent(DB_IPCE_ENC_ADD_FUNCTION, pModule, token, classToken, encVersion);


    return S_OK;
}

// Called to add a new function when the type has been loaded already.
// This is effectively the same as above, except that we're given a
// MethodDesc instead of a module and token.
// This should probably be merged into a single method since the caller
// should always have a module and token available in both cases.
HRESULT Debugger::AddFunction(MethodDesc* pMD, SIZE_T encVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(ThisIsHelperThread());
        PRECONDITION(ThreadHoldsLock()); // must have lock since we're on helper and stopped.
    }
    CONTRACTL_END;

    DebuggerDataLockHolder debuggerDataLockHolder(this);

    LOG((LF_CORDB, LL_INFO10000, "D::AF: adding "
         "%s::%s to version %d\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName, encVersion));

    _ASSERTE(pMD != NULL);
    Module *pModule = g_pEEInterface->MethodDescGetModule(pMD);
    _ASSERTE(pModule != NULL);
    mdToken methodDef = pMD->GetMemberDef();

    // tell the RS that this function has been added so that it can create new CorDBFunction
    SendEnCUpdateEvent( DB_IPCE_ENC_ADD_FUNCTION,
                        pModule,
                        methodDef,
                        pMD->GetMethodTable()->GetCl(),
                        encVersion);

    DebuggerMethodInfo *dmi = CreateMethodInfo(pModule, methodDef);
    if (! dmi)
    {
        return E_OUTOFMEMORY;
    }
    dmi->SetCurrentEnCVersion(encVersion);

    return S_OK;
}

// Invoke when a field is added to a class using EnC
HRESULT Debugger::AddField(FieldDesc* pFD, SIZE_T encVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::AFld: adding "
         "%8.8d::%8.8d to version %d\n", pFD->GetApproxEnclosingMethodTable()->GetCl(), pFD->GetMemberDef(), encVersion));

    // tell the RS that this field has been added so that it can update it's structures
    SendEnCUpdateEvent( DB_IPCE_ENC_ADD_FIELD,
                        pFD->GetModule(),
                        pFD->GetMemberDef(),
                        pFD->GetApproxEnclosingMethodTable()->GetCl(),
                        encVersion);

    return S_OK;
}

//
// RemapComplete is called when we are just about to resume into
// the function so that we can setup our breakpoint to trigger
// a call to the RemapComplete callback once the function is actually
// on the stack. We need to wait until the function is jitted before
// we can add the trigger, which doesn't happen until we call
// ResumeInUpdatedFunction in the VM
//
// addr is address within the given function, which we use to determine
// exact EnC version.
//
HRESULT Debugger::RemapComplete(MethodDesc* pMD, TADDR addr, SIZE_T nativeOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

    _ASSERTE(pMD != NULL);
    _ASSERTE(addr != NULL);

    LOG((LF_CORDB, LL_INFO10000, "D::RC: installed remap complete patch for "
         "%s::%s to version %d\n", pMD->m_pszDebugClassName, pMD->m_pszDebugMethodName));

    DebuggerMethodInfo *dmi = GetOrCreateMethodInfo(pMD->GetModule(), pMD->GetMemberDef());

    if (dmi == NULL)
    {
        return E_OUTOFMEMORY;
    }

    DebuggerJitInfo *pJitInfo = GetJitInfo(pMD, (const BYTE *) addr);

    if (pJitInfo == NULL)
    {
        _ASSERTE(!"Debugger doesn't handle OOM");
        return E_OUTOFMEMORY;
    }
    _ASSERTE(pJitInfo->m_addrOfCode + nativeOffset == addr);

    DebuggerEnCBreakpoint *bp;

    // Create and activate a new REMAP_COMPLETE EnC breakpoint to let us know when
    // the EE has completed the remap process.
    // This will be deleted when the patch is hit.
    bp = new (interopsafe, nothrow) DebuggerEnCBreakpoint( nativeOffset,
                                                           pJitInfo,
                                                           DebuggerEnCBreakpoint::REMAP_COMPLETE,
                                       (AppDomain *)pMD->GetModule()->GetDomain());
    if (bp == NULL)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

//-----------------------------------------------------------------------------
// Called by EnC stuff to map an IL offset to a native offset for the given
// method described by (pMD, nativeFnxStart).
//
// pMD - methoddesc for method being remapped
// ilOffset - incoming offset in old method to remap.
// nativeFnxStart - address of new function. This can be used to find the DJI
//   for the new method.
// nativeOffset - outparameter for native linear offset relative to start address.
//-----------------------------------------------------------------------------

HRESULT Debugger::MapILInfoToCurrentNative(MethodDesc *pMD,
                                           SIZE_T ilOffset,
                                           TADDR nativeFnxStart,
                                           SIZE_T *nativeOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
        PRECONDITION(nativeOffset != NULL);
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(nativeFnxStart != NULL);
    }
    CONTRACTL_END;

    _ASSERTE(HasLazyData()); // only used for EnC, should have already inited.


    LOG((LF_CORDB, LL_INFO1000000, "D::MILITCN: %s::%s ilOff:0x%x, "
        ", natFnx:0x%x dji:0x%x\n", pMD->m_pszDebugClassName,
        pMD->m_pszDebugMethodName, ilOffset, nativeFnxStart));

    *nativeOffset = 0;
    DebuggerJitInfo *djiTo = GetJitInfo( pMD, (const BYTE *)nativeFnxStart);
    if (djiTo == NULL)
    {
        _ASSERTE(!"No DJI in EnC case: should only happen on oom. Debugger doesn't support OOM.");
        return E_FAIL;
    }

    DebuggerJitInfo::ILToNativeOffsetIterator it;
    djiTo->InitILToNativeOffsetIterator(it, ilOffset);
    *nativeOffset = it.CurrentAssertOnlyOne(NULL);
    return S_OK;
}

#endif // EnC_SUPPORTED

//---------------------------------------------------------------------------------------
// Hijack worker stub called from asm stub. This can then delegate to other hijacks.
//
// Arguments:
//     pContext - context from which we were hijacked. Always non-null.
//     pRecord - exception record if hijacked from an exception event.
//              Else null (if hijacked from a managed IP).
//     reason - hijack reason. Use this to delegate to the proper hijack stub.
//     pData   - arbitrary data for the hijack to use. (eg, such as a DebuggerEval object)
//
// Returns:
//     This does not return. Instead it restores this threads context to pContext.
//
// Assumptions:
//     If hijacked at an exception event, the debugger must have cleared the exception.
//
// Notes:
//     The debugger hijacked the thread to get us here via the DacDbi Hijack primitive.
//     This is called from a hand coded asm stub.
//
void STDCALL ExceptionHijackWorker(
    CONTEXT * pContext,
    EXCEPTION_RECORD * pRecord,
    EHijackReason::EHijackReason reason,
    void * pData)
{
    STRESS_LOG0(LF_CORDB,LL_INFO100, "D::EHW: Enter ExceptionHijackWorker\n");

    // We could have many different reasons for hijacking. Switch and invoke the proper hijacker.
    switch(reason)
    {
        case EHijackReason::kUnhandledException:
            STRESS_LOG0(LF_CORDB,LL_INFO10, "D::EHW: Calling g_pDebugger->UnhandledHijackWorker()\n");
            _ASSERTE(pData == NULL);
            g_pDebugger->UnhandledHijackWorker(pContext, pRecord);
            break;
#ifdef FEATURE_INTEROP_DEBUGGING
    case EHijackReason::kM2UHandoff:
            _ASSERTE(pData == NULL);
            g_pDebugger->M2UHandoffHijackWorker(pContext, pRecord);
            break;
    case EHijackReason::kFirstChanceSuspend:
            _ASSERTE(pData == NULL);
            g_pDebugger->FirstChanceSuspendHijackWorker(pContext, pRecord);
            break;
    case EHijackReason::kGenericHijack:
            _ASSERTE(pData == NULL);
            g_pDebugger->GenericHijackFunc();
            break;
#endif
    default:
            CONSISTENCY_CHECK_MSGF(false, ("Unrecognized Hijack code: %d", reason));
    }

    // Currently, no Hijack actually returns yet.
    UNREACHABLE();

    // If we return to this point, then we'll restore ourselves.
    // We've got the context that we were hijacked from, so we should be able to just
    // call SetThreadContext on ourself to fix us.
}

#if defined(FEATURE_EH_FUNCLETS) && !defined(TARGET_UNIX)

#if defined(TARGET_AMD64)
// ----------------------------------------------------------------------------
// EmptyPersonalityRoutine
//
// Description:
//    This personality routine is used to work around a limitation of the OS unwinder when we return
//    ExceptionCollidedUnwind.
//    See code:ExceptionHijackPersonalityRoutine for more information.
//
// Arguments:
//    * pExceptionRecord   - not used
//    * MemoryStackFp      - not used
//    * BackingStoreFp     - not used
//    * pContextRecord     - not used
//    * pDispatcherContext - not used
//    * GlobalPointer      - not used
//
// Return Value:
//    Always return ExceptionContinueSearch.
//

EXCEPTION_DISPOSITION EmptyPersonalityRoutine(IN     PEXCEPTION_RECORD   pExceptionRecord,
                                              IN     ULONG64             MemoryStackFp,
                                              IN OUT PCONTEXT            pContextRecord,
                                              IN OUT PDISPATCHER_CONTEXT pDispatcherContext)
{
    LIMITED_METHOD_CONTRACT;
    return ExceptionContinueSearch;
}
#endif // TARGET_AMD64

//---------------------------------------------------------------------------------------
// Personality routine for unwinder the assembly hijack stub on 64-bit.
//
// Arguments:
//    standard Personality routine signature.
//
// Assumptions:
//    This is caleld by the OS exception logic during exception handling.
//
// Notes:
//    We just need 1 personality routine for the tiny assembly hijack stub.
//    All the C++ code invoked by the stub is ok.
//
//    This needs to fetch the original context that this thread was hijacked from
//    (which the hijack pushed onto the stack) and pass that back to the OS. This lets
//    ths OS unwind out of the hijack.
//
//    This function should only be executed if an unhandled exception is intercepted by a managed debugger.
//    Otherwise there should never be a 2nd pass exception dispatch crossing the hijack stub.
//
//    The basic idea here is straightforward.  The OS does an exception dispatch and hit our hijack stub.
//    Since the hijack stub is not unwindable, we need a personality routine to restore the CONTEXT and
//    tell the OS to continue the dispatch with that CONTEXT by returning ExceptionCollidedUnwind.
//
//    However, empricially, the OS expects that when we return ExceptionCollidedUnwind, the function
//    represented by the CONTEXT has a personality routine.  The OS will actually AV if we return a NULL
//    personality routine.
//
//    On AMD64, we work around this by using an empty personality routine.

EXTERN_C EXCEPTION_DISPOSITION
ExceptionHijackPersonalityRoutine(IN     PEXCEPTION_RECORD   pExceptionRecord
                        BIT64_ARG(IN     ULONG64             MemoryStackFp)
                    NOT_BIT64_ARG(IN     ULONG32             MemoryStackFp),
                                  IN OUT PCONTEXT            pContextRecord,
                                  IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                 )
{
#if defined(TARGET_AMD64)
    CONTEXT * pHijackContext = NULL;

    // Get the 1st parameter (the Context) from hijack worker.
    // EstablisherFrame points to the stack slot 8 bytes above the
    // return address to the ExceptionHijack. This would contain the
    // parameters passed to ExceptionHijackWorker, which is marked
    // STDCALL, but the x64 calling convention lets the
    // ExceptionHijackWorker use that stack space, resulting in the
    // context being overwritten. Instead, we get the context from the
    // previous stack frame, which contains the arguments to
    // ExceptionHijack, placed there by the debugger in
    // DacDbiInterfaceImpl::Hijack. This works because ExceptionHijack
    // allocates exactly 4 stack slots.
    pHijackContext = *reinterpret_cast<CONTEXT **>(pDispatcherContext->EstablisherFrame + 0x20);

    // This copies pHijackContext into pDispatcherContext, which the OS can then
    // use to walk the stack.
    FixupDispatcherContext(pDispatcherContext, pHijackContext, pContextRecord, (PEXCEPTION_ROUTINE)EmptyPersonalityRoutine);
#else
    _ASSERTE(!"NYI - ExceptionHijackPersonalityRoutine()");
#endif

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record and
    // dispatcher context and restart the exception dispatching on this call frame, which is
    // exactly the behavior we want.
    return ExceptionCollidedUnwind;
}
#endif // FEATURE_EH_FUNCLETS && !TARGET_UNIX


// UEF Prototype from excep.cpp
LONG InternalUnhandledExceptionFilter_Worker(EXCEPTION_POINTERS *pExceptionInfo);

//---------------------------------------------------------------------------------------
// Hijack for a 2nd-chance exception. Will invoke the CLR's UEF.
//
// Arguments:
//     pContext - context that this thread was hijacked from.
//     pRecord - exception record of the exception that this was hijacked at.
//     pData - random data.
// Notes:
// When under a native-debugger, the OS does not invoking the Unhandled Exception Filter (UEF).
// It dispatches a 2nd-chance Exception event instead.
// However, the CLR's UEF does lots of useful work (like dispatching the 2nd-chance managed exception,
// allowing func-eval on 2nd-chance, and allowing intercepting unhandled exceptions).
// So we'll emulate the OS behavior here by invoking the CLR's UEF directly.
//
void Debugger::UnhandledHijackWorker(CONTEXT * pContext, EXCEPTION_RECORD * pRecord)
{
    CONTRACTL
    {
        // The ultimate protection shield is that this hijack can be executed under the same circumstances
        // as a top-level UEF that pinvokes into managed code
        // - That means we're GC-triggers safe
        // - that means that we can crawl the stack. (1st-pass EH logic ensures this).
        // We need to be GC-triggers because this may invoke a func-eval.
        GC_TRIGGERS;

        // Don't throw out of a hijack! There's nobody left to catch this.
        NOTHROW;

        // We expect to always be in preemptive here by the time we get this unhandled notification.
        // We know this is true because a native UEF is preemptive.
        // More detail:
        //   1) If we got here from a software exception (eg, Throw from C#), then the jit helper
        //       toggled us to preemptive before calling RaiseException().
        //   2) If we got here from a hardware exception in managed code, then the 1st-pass already did
        //       some magic to get us into preemptive. On x86, this is magic. On 64-bit, it did some magic
        //       to push a Faulting-Exception-Frame and rethrow the exception as a software exception.
        MODE_PREEMPTIVE;


        PRECONDITION(CheckPointer(pContext));
        PRECONDITION(CheckPointer(pRecord));
    }
    CONTRACTL_END;

    EXCEPTION_POINTERS exceptionInfo;
    exceptionInfo.ContextRecord = pContext;
    exceptionInfo.ExceptionRecord = pRecord;

    // Snag the Runtime thread. Since we're hijacking a managed exception, we should always have one.
    Thread * pThread = g_pEEInterface->GetThread();
    (void)pThread; //prevent "unused variable" error from GCC
    _ASSERTE(pThread != NULL);

    BOOL fSOException = FALSE;

    if ((pRecord != NULL) &&
        (pRecord->ExceptionCode == STATUS_STACK_OVERFLOW))
    {
        fSOException = TRUE;
    }

    // because we hijack here during jit attach invoked by the OS we need to make sure that the debugger is completely
    // attached before continuing. If we ever hijacked here when an attach was not in progress this function returns
    // immediately so no problems there.
    WaitForDebuggerAttach();
    PostJitAttach();

    // On Win7 WatsonLastChance returns CONTINUE_SEARCH for unhandled exceptions execpt stack overflow, and
    // lets OS launch debuggers for us.  Before the unhandled exception reaches the OS, CLR UEF has already
    // processed this unhandled exception.  Thus, we should not call into CLR UEF again if it is the case.
    if (pThread &&
        (pThread->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException) ||
         fSOException))
    {

        FrameWithCookie<FaultingExceptionFrame> fef;
#if defined(FEATURE_EH_FUNCLETS)
        *((&fef)->GetGSCookiePtr()) = GetProcessGSCookie();
#endif // FEATURE_EH_FUNCLETS
        if ((pContext != NULL) && fSOException)
        {
            GCX_COOP();     // Must be cooperative to modify frame chain.

            // EEPolicy::HandleFatalStackOverflow pushes a FaultingExceptionFrame on the stack after SO
            // exception.  Our hijack code runs in the exception context, and overwrites the stack space
            // after SO excpetion, so this frame was popped out before invoking RaiseFailFast.  We need to
            // put it back here for running func-eval code.
            // This cumbersome code should be removed once SO synchronization is moved to be completely
            // out-of-process.
            fef.InitAndLink(pContext);
        }

        STRESS_LOG0(LF_CORDB, LL_INFO10, "D::EHW: Calling NotifyDebuggerLastChance\n");
        NotifyDebuggerLastChance(pThread, &exceptionInfo, TRUE);

        // Continuing from a second chance managed exception causes the process to exit.
        TerminateProcess(GetCurrentProcess(), 0);
    }

    // Since this is a unhandled managed exception:
    // - we always have a Thread* object.
    // - we always have a throwable
    // - we executed through the 1st-pass of the EH logic. This means the 1st-pass could do work
    //   to enforce certain invariants (like the ones listed here, or ensuring the thread can be crawled)

    // Need to call the CLR's UEF. This will do all the key work including:
    // - send the managed 2nd-chance exception event.
    // - deal with synchronization.
    // - allow func-evals.
    // - deal with interception.

    // If intercepted, then this never returns. It will manually invoke the unwinders and fix the context.

    // InternalUnhandledExceptionFilter_Worker has a throws contract, but should not be throwing in any
    // conditions we care about. This hijack should never throw, so catch everything.
    HRESULT hrIgnore;
    EX_TRY
    {
        InternalUnhandledExceptionFilter_Worker(&exceptionInfo);
    }
    EX_CATCH_HRESULT(hrIgnore);

    // Continuing from a second chance managed exception causes the process to exit.
    TerminateProcess(GetCurrentProcess(), 0);
}

#ifdef FEATURE_INTEROP_DEBUGGING
//
// This is the handler function that is put in place of a thread's top-most SEH handler function when it is hijacked by
// the Right Side during an unmanaged first chance exception.
//
typedef EXCEPTION_DISPOSITION (__cdecl *SEHHandler)(EXCEPTION_RECORD *pExceptionRecord,
                             EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
                             CONTEXT *pContext,
                             void *DispatcherContext);
#define DOSPEW 0

#if DOSPEW
#define SPEW(s) s
#else
#define SPEW(s)
#endif




//-----------------------------------------------------------------------------
// Hijack when we have a M2U handoff.
// This happens when we do a step-out from Managed-->Unmanaged, and so we hit a managed patch in Native code.
// This also happens when a managed stepper does a step-in to unmanaged code.
// Since we're in native code, there's no CPFH, and so we have to hijack.
// @todo-  could this be removed? Step-out to native is illegal in v2.0, and do existing
// CLR filters catch the step-in patch?
// @dbgtodo  controller/stepping - this will be completely unneeded in V3 when all stepping is oop
//-----------------------------------------------------------------------------
VOID Debugger::M2UHandoffHijackWorker(CONTEXT *pContext,
                                      EXCEPTION_RECORD *pExceptionRecord)
{
    // We must use a static contract here because the function does not return normally
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS; // from sending managed event
    STATIC_CONTRACT_MODE_PREEMPTIVE; // we're in umanaged code.

    LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: Context=0x%p exception record=0x%p\n",
        pContext, pExceptionRecord));

    // We should only be here for a BP
    _ASSERTE(pExceptionRecord->ExceptionCode == STATUS_BREAKPOINT);

    // Get the current runtime thread. This is only an optimized TLS access.
    // Since we're coming off a managed-step, we should always have a thread.
    Thread *pEEThread = g_pEEInterface->GetThread();
    _ASSERTE(pEEThread != NULL);

    _ASSERTE(!pEEThread->GetInteropDebuggingHijacked());
    pEEThread->SetInteropDebuggingHijacked(TRUE);

    //win32 has a weird property where EIP points after the BP in the debug event
    //so we are adjusting it to point at the BP
    CORDbgAdjustPCForBreakInstruction((DT_CONTEXT*)pContext);
    LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: Context ip set to 0x%p\n", GetIP(pContext)));

    _ASSERTE(!ISREDIRECTEDTHREAD(pEEThread));

    // Don't bother setting FilterContext here because we already pass it to FirstChanceNativeException.
    // Shortcut right to our dispatch native exception logic, there may be no COMPlusFrameHandler in place!
    EX_TRY
    {
        LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: Calling FirstChanceNativeException\n"));
        bool okay;
        okay = g_pDebugger->FirstChanceNativeException(pExceptionRecord,
            pContext,
            pExceptionRecord->ExceptionCode,
            pEEThread);
        _ASSERTE(okay == true);
        LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: FirstChanceNativeException returned\n"));
    }
    EX_CATCH
    {
        // It would be really bad if somebody threw here. We're actually outside of managed code,
        // so there's not a lot we can do besides just swallow the exception and hope for the best.
        LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: ERROR! FirstChanceNativeException threw an exception\n"));
    }
    EX_END_CATCH(SwallowAllExceptions);

    _ASSERTE(!ISREDIRECTEDTHREAD(pEEThread));
    _ASSERTE(pEEThread->GetInteropDebuggingHijacked());
    pEEThread->SetInteropDebuggingHijacked(FALSE);

    // This signal will be received by the RS and it will use SetThreadContext
    // to clear away the entire hijack frame. This function does not return.
    LOG((LF_CORDB, LL_INFO1000, "D::M2UHHW: Flaring hijack complete\n"));
    SignalHijackComplete();

    _ASSERTE(!"UNREACHABLE");
}

//-----------------------------------------------------------------------------
// This hijack is run after receiving an IB event that we don't know how the
// debugger will want to continue. Under the covers we clear the event and divert
// execution here where we block until the debugger decides whether or not to clear
// the event. At that point we exit this hijack and the LS diverts execution back
// to the offending instruction.
// We don't know:
// - whether we have an EE-thread?
// - how we're going to continue this (handled / not-handled).
//
// But we do know that:
// - this exception does not belong to the CLR.
// - this thread is not in cooperative mode.
//-----------------------------------------------------------------------------
LONG Debugger::FirstChanceSuspendHijackWorker(CONTEXT *pContext,
                                              EXCEPTION_RECORD *pExceptionRecord)
{
    // if we aren't set up to do interop debugging this function should just bail out
    if(m_pRCThread == NULL)
        return EXCEPTION_CONTINUE_SEARCH;

    DebuggerIPCControlBlock *pDCB = m_pRCThread->GetDCB();
    if(pDCB == NULL)
        return EXCEPTION_CONTINUE_SEARCH;

    if (!pDCB->m_rightSideIsWin32Debugger)
        return EXCEPTION_CONTINUE_SEARCH;

    // at this point we know that there is an interop debugger attached. This makes it safe to send
    // flares
#if DOSPEW
    DWORD tid = GetCurrentThreadId();
#endif

    SPEW(fprintf(stderr, "0x%x D::FCHF: in first chance hijack filter.\n", tid));
    SPEW(fprintf(stderr, "0x%x D::FCHF: pExceptionRecord=0x%p (%d), pContext=0x%p (%d)\n", tid, pExceptionRecord, sizeof(EXCEPTION_RECORD),
        pContext, sizeof(CONTEXT)));
#if defined(TARGET_AMD64)
    SPEW(fprintf(stderr, "0x%x D::FCHF: code=0x%08x, addr=0x%p, Rip=0x%p, Rsp=0x%p, EFlags=0x%08x\n",
        tid, pExceptionRecord->ExceptionCode, pExceptionRecord->ExceptionAddress, pContext->Rip, pContext->Rsp,
        pContext->EFlags));
#elif defined(TARGET_X86)
    SPEW(fprintf(stderr, "0x%x D::FCHF: code=0x%08x, addr=0x%08x, Eip=0x%08x, Esp=0x%08x, EFlags=0x%08x\n",
        tid, pExceptionRecord->ExceptionCode, pExceptionRecord->ExceptionAddress, pContext->Eip, pContext->Esp,
        pContext->EFlags));
#elif defined(TARGET_ARM64)
    SPEW(fprintf(stderr, "0x%x D::FCHF: code=0x%08x, addr=0x%08x, Pc=0x%p, Sp=0x%p, EFlags=0x%08x\n",
        tid, pExceptionRecord->ExceptionCode, pExceptionRecord->ExceptionAddress, pContext->Pc, pContext->Sp,
        pContext->EFlags));
#endif

    // This memory is used as IPC during the hijack. We will place a pointer to this in
    // the EE debugger word (a TLS slot that works even on the debugger break-in thread)
    // and then the RS can write info into the memory.
    DebuggerIPCFirstChanceData fcd;

    // Accessing through the volatile pointer to fend off some potential compiler optimizations.
    // If the debugger changes that data from OOP we need to see those updates
    volatile DebuggerIPCFirstChanceData* pFcd = &fcd;

    // The Windows native break in thread does not have TLS storage allocated.
    bool debuggerBreakInThread = (NtCurrentTeb()->ThreadLocalStoragePointer == NULL);
    {
        // Hijack filters are always in the can't stop range.
        // The RS knows this b/c it knows which threads it hijacked.
        // Bump up the CS counter so that any further calls in the LS can see this too.
        // (This makes places where we assert that we're in a CS region happy).
        CantStopHolder hCantStop(!debuggerBreakInThread);

        // Get the current runtime thread. This is only an optimized TLS access.
        Thread *pEEThread = debuggerBreakInThread ? NULL : g_pEEInterface->GetThread();

        // Hook up the memory so RS can get to it
        fcd.pLeftSideContext.Set((DT_CONTEXT*)pContext);
        fcd.action = HIJACK_ACTION_EXIT_UNHANDLED;
        fcd.debugCounter = 0;

        SPEW(fprintf(stderr, "0x%x D::FCHF: Set debugger word to 0x%p.\n", tid, pFcd));
        g_pEEInterface->SetThreadDebuggerWord((VOID*)pFcd);

        // Signal the RS to tell us what to do
        SPEW(fprintf(stderr, "0x%x D::FCHF: Signaling hijack started.\n", tid));
        SignalHijackStarted();
        SPEW(fprintf(stderr, "0x%x D::FCHF: Signaling hijack started complete. DebugCounter=0x%x\n", tid, pFcd->debugCounter));

        if (pFcd->action == HIJACK_ACTION_WAIT)
        {
            // This exception does NOT belong to the CLR.
            // If we belong to the CLR, then we either:
            // - were a  M2U transition, in which case we should be in a different Hijack
            // - were a CLR exception in CLR code, in which case we should have continued and let the inproc handlers get it.
            SPEW(fprintf(stderr, "0x%x D::FCHF: exception does not belong to the Runtime, pEEThread=0x%p, pContext=0x%p\n",
                         tid, pEEThread, pContext));

            if (pEEThread != NULL)
            {
                _ASSERTE(!pEEThread->GetInteropDebuggingHijacked()); // hijack is not re-entrant.
                pEEThread->SetInteropDebuggingHijacked(TRUE);

                // Setting the FilterContext must be done in cooperative mode (since it's like pushing a Frame onto the Frame chain).
                // Thus we have a violation. We don't really need the filter context specifically here, we're just using
                // it for legacy purposes as a way to stash the context of the original exception (that this thread was hijacked from).
                // @todo - use another way to store the context indepedent of the Filter context.
                CONTRACT_VIOLATION(ModeViolation);
                _ASSERTE(g_pEEInterface->GetThreadFilterContext(pEEThread) == NULL);
                g_pEEInterface->SetThreadFilterContext(pEEThread, pContext);
            }

            // Wait for the continue. We may / may not have an EE Thread for this, (and we're definitely
            // not doing fiber-mode debugging), so just use a raw win32 API, and not some fancy fiber-safe call.
            SPEW(fprintf(stderr, "0x%x D::FCHF: waiting for continue.\n", tid));
            DWORD ret = WaitForSingleObject(g_pDebugger->m_pRCThread->GetDCB()->m_leftSideUnmanagedWaitEvent, INFINITE);
            SPEW(fprintf(stderr, "0x%x D::FCHF: waiting for continue complete.\n", tid));

            if (ret != WAIT_OBJECT_0)
            {
                SPEW(fprintf(stderr, "0x%x D::FCHF: wait failed!\n", tid));
            }

            if (pEEThread != NULL)
            {
                _ASSERTE(pEEThread->GetInteropDebuggingHijacked());
                pEEThread->SetInteropDebuggingHijacked(FALSE);
                _ASSERTE(!ISREDIRECTEDTHREAD(pEEThread));

                // See violation above.
                CONTRACT_VIOLATION(ModeViolation);
                g_pEEInterface->SetThreadFilterContext(pEEThread, NULL);
                _ASSERTE(g_pEEInterface->GetThreadFilterContext(pEEThread) == NULL);
            }
        }

        SPEW(fprintf(stderr, "0x%x D::FCHF: signaling HijackComplete.\n", tid));
        SignalHijackComplete();
        SPEW(fprintf(stderr, "0x%x D::FCHF: done signaling HijackComplete. DebugCounter=0x%x\n", tid, pFcd->debugCounter));

        // we should know what we are about to do now
        _ASSERTE(pFcd->action != HIJACK_ACTION_WAIT);

        // cleanup from above
        SPEW(fprintf(stderr, "0x%x D::FCHF: set debugger word = NULL.\n", tid));
        g_pEEInterface->SetThreadDebuggerWord(NULL);

    } // end can't stop region

    if (pFcd->action == HIJACK_ACTION_EXIT_HANDLED)
    {
        SPEW(fprintf(stderr, "0x%x D::FCHF: exiting with CONTINUE_EXECUTION\n", tid));
        return EXCEPTION_CONTINUE_EXECUTION;
    }
    else
    {
        SPEW(fprintf(stderr, "0x%x D::FCHF: exiting with CONTINUE_SEARCH\n", tid));
        _ASSERTE(pFcd->action == HIJACK_ACTION_EXIT_UNHANDLED);
        return EXCEPTION_CONTINUE_SEARCH;
    }
}

#if defined(TARGET_X86) || defined(TARGET_AMD64) || defined(TARGET_ARM64)
void GenericHijackFuncHelper()
{
#if DOSPEW
    DWORD tid = GetCurrentThreadId();
#endif

    // The Windows native break in thread does not have TLS storage allocated.
    bool debuggerBreakInThread = (NtCurrentTeb()->ThreadLocalStoragePointer == NULL);

    // Hijack filters are always in the can't stop range.
    // The RS knows this b/c it knows which threads it hijacked.
    // Bump up the CS counter so that any further calls in the LS can see this too.
    // (This makes places where we assert that we're in a CS region happy).
    CantStopHolder hCantStop(!debuggerBreakInThread);

    SPEW(fprintf(stderr, "0x%x D::GHF: in generic hijack.\n", tid));

    // There is no need to setup any context pointer or interact with the Right Side in anyway. We simply wait for
    // the continue event to be set.
    SPEW(fprintf(stderr, "0x%x D::GHF: waiting for continue.\n", tid));

    // If this thread has an EE thread and that EE thread has preemptive gc disabled, then mark that there is a
    // thread at an unsafe place and enable pgc. This will allow us to sync even with this thread hijacked.
    bool disabled = false;

    Thread *pEEThread = debuggerBreakInThread ? NULL : g_pEEInterface->GetThread();

    if (pEEThread != NULL)
    {
        disabled = g_pEEInterface->IsPreemptiveGCDisabled();
        _ASSERTE(!disabled);

        _ASSERTE(!pEEThread->GetInteropDebuggingHijacked());
        pEEThread->SetInteropDebuggingHijacked(TRUE);
    }

    DWORD ret = WaitForSingleObject(g_pRCThread->GetDCB()->m_leftSideUnmanagedWaitEvent,
                                    INFINITE);

    if (ret != WAIT_OBJECT_0)
    {
        SPEW(fprintf(stderr, "0x%x D::GHF: wait failed!\n", tid));
    }

    // Get the continue type. Non-zero means that the exception was not cleared by the Right Side and therefore has
    // not been handled. Zero means that the exception has been cleared. (Presumably, the debugger altered the
    // thread's context before clearing the exception, so continuing will give a different result.)
    DWORD continueType = 0;

    void* threadDebuggerWord = g_pEEInterface->GetThreadDebuggerWord();

    if (pEEThread != NULL)
    {
        // We've got a Thread ptr, so get the continue type out of the thread's debugger word.
        continueType = (DWORD)(size_t) threadDebuggerWord;

        _ASSERTE(pEEThread->GetInteropDebuggingHijacked());
        pEEThread->SetInteropDebuggingHijacked(FALSE);
    }
    else if (threadDebuggerWord != NULL)
    {
        continueType = 1;
        g_pEEInterface->SetThreadDebuggerWord(NULL);
    }

    SPEW(fprintf(stderr, "0x%x D::GHF: continued with %d.\n", tid, continueType));

    if (continueType)
    {
        SPEW(fprintf(stderr, "0x%x D::GHF: calling ExitProcess\n", tid));

        // Continuing from a second chance exception without clearing the exception causes the process to
        // exit. Note: the continue type will only be non-zero if this hijack was setup for a second chance
        // exception. If the hijack was setup for another type of debug event, then we'll never get here.
        //
        // We explicitly terminate the process directly instead of going through any escalation policy because:
        // 1) that's what a native-only debugger would do. Interop and Native-only should be the same.
        // 2) there's no CLR escalation policy anyways for *native* unhandled exceptions.
        // 3) The escalation policy may do lots of extra confusing work (like fire MDAs) that can only cause
        // us grief.
        TerminateProcess(GetCurrentProcess(), 0);
    }

    SPEW(fprintf(stderr, "0x%x D::GHF: signaling continue...\n", tid));
}
#endif


//
// This is the function that a thread is hijacked to by the Right Side during a variety of debug events. This function
// must be naked.
//
#if defined(TARGET_X86)
__declspec(naked)
#endif // defined (_x86_)
void Debugger::GenericHijackFunc(void)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)

#if defined(TARGET_X86)
    _asm
    {
        push ebp
        mov  ebp,esp
        sub  esp,__LOCAL_SIZE
    }
#endif
    // We can't have C++ classes w/ dtors in a declspec naked, so just have call into a helper.
    GenericHijackFuncHelper();

#if defined(TARGET_X86)
    _asm
    {
        mov esp,ebp
        pop ebp
    }
#endif

    // This signals the Right Side that this thread is ready to have its context restored.
    ExceptionNotForRuntime();

#else
    _ASSERTE(!"@todo - port GenericHijackFunc");
#endif // defined (_x86_)

    _ASSERTE(!"Should never get here (Debugger::GenericHijackFunc)");
}




//#ifdef TARGET_X86
//
// This is the function that is called when we determine that a first chance exception hijack has
// begun and memory is prepared for the RS to tell the LS what to do
//
void Debugger::SignalHijackStarted(void)
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_INTEROP_DEBUGGING)
    SignalHijackStartedFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif
}

//
// This is the function that is called when we determine that a first chance exception really belongs to the Runtime,
// and that that exception is due to a managed->unmanaged transition. This notifies the Right Side of this and the Right
// Side fixes up the thread's execution state from there, making sure to remember that it needs to continue to hide the
// hijack state of the thread.
//
void Debugger::ExceptionForRuntimeHandoffStart(void)
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_INTEROP_DEBUGGING)
    ExceptionForRuntimeHandoffStartFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif

}

//
// This is the function that is called when the original handler returns after we've determined that an exception was
// due to a managed->unmanaged transition. This notifies the Right Side of this and the Right Side fixes up the thread's
// execution state from there, making sure to turn off its flag indicating that the thread's hijack state should still
// be hidden.
//
void Debugger::ExceptionForRuntimeHandoffComplete(void)
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_INTEROP_DEBUGGING)
    ExceptionForRuntimeHandoffCompleteFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif

}

//
// This signals the RS that a hijack function is ready to return. This will cause the RS to restore
// the thread context
//
void Debugger::SignalHijackComplete(void)
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_INTEROP_DEBUGGING)
    SignalHijackCompleteFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif

}

//
// This is the function that is called when we determine that a first chance exception does not belong to the
// Runtime. This notifies the Right Side of this and the Right Side fixes up the thread's execution state from there.
//
void Debugger::ExceptionNotForRuntime(void)
{
    WRAPPER_NO_CONTRACT;

#if defined(FEATURE_INTEROP_DEBUGGING)
    ExceptionNotForRuntimeFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif
}

//
// This is the function that is called when we want to send a sync complete event to the Right Side when it is the Win32
// debugger of this process. This notifies the Right Side of this and the Right Side fixes up the thread's execution
// state from there.
//
void Debugger::NotifyRightSideOfSyncComplete(void)
{
    WRAPPER_NO_CONTRACT;
    STRESS_LOG0(LF_CORDB, LL_INFO100000, "D::NRSOSC: Sending flare...\n");
#if defined(FEATURE_INTEROP_DEBUGGING)
    NotifyRightSideOfSyncCompleteFlare();
#else
    _ASSERTE(!"@todo - port the flares to the platform your running on.");
#endif
    STRESS_LOG0(LF_CORDB, LL_INFO100000, "D::NRSOSC: Flare sent\n");
}

#endif // FEATURE_INTEROP_DEBUGGING

/******************************************************************************
 *
 ******************************************************************************/
bool Debugger::GetILOffsetFromNative (MethodDesc *pFunc, const BYTE *pbAddr,
                                      DWORD nativeOffset, DWORD *ilOffset)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

    _ASSERTE(pFunc != NULL);
    _ASSERTE(pbAddr != NULL);

    if (!HasLazyData())
    {
        DebuggerLockHolder dbgLockHolder(this);
        // This is an entry path into the debugger, so make sure we're inited.
        LazyInit();
    }

    // Sometimes we'll get called w/ an instantiating stub MD.
    if (pFunc->IsWrapperStub())
    {
        pFunc = pFunc->GetWrappedMethodDesc();
    }

    if (pFunc->IsDynamicMethod())
    {
        return false;
    }

    DebuggerMethodInfo *methodInfo = GetOrCreateMethodInfo(pFunc->GetModule(), pFunc->GetMemberDef());
    if (methodInfo == NULL)
    {
        return false;
    }

    PCODE methodStartAddress = g_pEEInterface->GetNativeCodeStartAddress((PCODE)pbAddr);
    if (methodStartAddress == NULL)
    {
        return false;
    }

    DebuggerJitInfo *jitInfo = methodInfo->FindOrCreateInitAndAddJitInfo(pFunc, methodStartAddress);
    if (jitInfo == NULL)
    {
        return false;
    }

    CorDebugMappingResult map;
    DWORD whichIDontCare;
    *ilOffset = jitInfo->MapNativeOffsetToIL(
                                    nativeOffset,
                                    &map,
                                    &whichIDontCare);
    return true;
}

/******************************************************************************
 *
 ******************************************************************************/
DWORD Debugger::GetHelperThreadID(void )
{
    LIMITED_METHOD_CONTRACT;

    return m_pRCThread ? m_pRCThread->GetDCB()->m_temporaryHelperThreadId : 0;
}


// HRESULT Debugger::InsertToMethodInfoList():  Make sure
//  that there's only one head of the the list of DebuggerMethodInfos
//  for the (implicitly) given MethodDef/Module pair.
HRESULT
Debugger::InsertToMethodInfoList( DebuggerMethodInfo *dmi )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO10000,"D:IAHOL DMI: dmi:0x%08x\n", dmi));

    HRESULT hr = S_OK;

    _ASSERTE(dmi != NULL);

    _ASSERTE(HasDebuggerDataLock());

    //    CHECK_DJI_TABLE_DEBUGGER;

    hr = CheckInitMethodInfoTable();

    if (FAILED(hr)) {
        return (hr);
    }

    DebuggerMethodInfo *dmiPrev = m_pMethodInfos->GetMethodInfo(dmi->m_module, dmi->m_token);

    _ASSERTE((dmiPrev == NULL) || ((dmi->m_token == dmiPrev->m_token) && (dmi->m_module == dmiPrev->m_module)));

    LOG((LF_CORDB,LL_INFO10000,"D:IAHOL: current head of dmi list:0x%08x\n",dmiPrev));

    if (dmiPrev != NULL)
    {
        dmi->m_prevMethodInfo = dmiPrev;
        dmiPrev->m_nextMethodInfo = dmi;

        _ASSERTE(dmi->m_module != NULL);
        hr = m_pMethodInfos->OverwriteMethodInfo(dmi->m_module,
                                         dmi->m_token,
                                         dmi,
                                         FALSE);

        LOG((LF_CORDB,LL_INFO10000,"D:IAHOL: DMI version 0x%04x for token 0x%08x\n",
            dmi->GetCurrentEnCVersion(),dmi->m_token));
    }
    else
    {
        LOG((LF_CORDB, LL_EVERYTHING, "AddMethodInfo being called in D:IAHOL\n"));
        hr = m_pMethodInfos->AddMethodInfo(dmi->m_module,
                                         dmi->m_token,
                                         dmi);
    }
#ifdef _DEBUG
    dmiPrev = m_pMethodInfos->GetMethodInfo(dmi->m_module, dmi->m_token);
    LOG((LF_CORDB,LL_INFO10000,"D:IAHOL: new head of dmi list:0x%08x\n",
        dmiPrev));
#endif //_DEBUG

    // DebuggerDataLockHolder out of scope - release implied
    return hr;
}

//-----------------------------------------------------------------------------
// Helper to get an SString through the IPC buffer.
// We do this by putting the SString data into a LS_RS_buffer object,
// and then the RS reads it out as soon as it's queued.
// It's very very important that the SString's buffer is around while we send the event.
// So we pass the SString by reference in case there's an implicit conversion (because
// we don't want to do the conversion on a temporary object and then lose that object).
//-----------------------------------------------------------------------------
void SetLSBufferFromSString(Ls_Rs_StringBuffer * pBuffer, SString & str)
{
    // Copy string contents (+1 for null terminator) into a LS_RS_Buffer.
    // Then the RS can pull it out as a null-terminated string.
    pBuffer->SetLsData(
        (BYTE*) str.GetUnicode(),
        (str.GetCount() +1)* sizeof(WCHAR)
    );
}

//*************************************************************
// structure that we to marshal MDA Notification event data.
//*************************************************************
struct SendMDANotificationParams
{
    Thread * m_pThread; // may be NULL. Lets us send on behalf of other threads.

    // Pass SStrings by ptr in case to guarantee that they're shared (in case we internally modify their storage).
    SString * m_szName;
    SString * m_szDescription;
    SString * m_szXML;
    CorDebugMDAFlags m_flags;

    SendMDANotificationParams(
        Thread * pThread, // may be NULL. Lets us send on behalf of other threads.
        SString * szName,
        SString * szDescription,
        SString * szXML,
        CorDebugMDAFlags flags
    ) :
        m_pThread(pThread),
        m_szName(szName),
        m_szDescription(szDescription),
        m_szXML(szXML),
        m_flags(flags)
    {
        LIMITED_METHOD_CONTRACT;
    }

};

//-----------------------------------------------------------------------------
// Actually send the MDA event. (Could be on any thread)
// Parameters:
//    params - data to initialize the IPC event.
//-----------------------------------------------------------------------------
void Debugger::SendRawMDANotification(
    SendMDANotificationParams * params
)
{
    // Send the unload assembly event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();

    Thread * pThread = params->m_pThread;
    AppDomain *pAppDomain = (pThread != NULL) ? pThread->GetDomain() : NULL;

    InitIPCEvent(ipce,
                 DB_IPCE_MDA_NOTIFICATION,
                 pThread,
                 pAppDomain);

    SetLSBufferFromSString(&ipce->MDANotification.szName, *(params->m_szName));
    SetLSBufferFromSString(&ipce->MDANotification.szDescription, *(params->m_szDescription));
    SetLSBufferFromSString(&ipce->MDANotification.szXml, *(params->m_szXML));
    ipce->MDANotification.dwOSThreadId = GetCurrentThreadId();
    ipce->MDANotification.flags = params->m_flags;

    m_pRCThread->SendIPCEvent();
}

//-----------------------------------------------------------------------------
// Send an MDA notification. This ultimately translates to an ICorDebugMDA object on the Right-Side.
// Called by EE to send a MDA debug event. This will block on the debug event
// until the RS continues us.
// Debugger may or may not be attached. If bAttached, then this
// will trigger a jitattach as well.
// See MDA documentation for what szName, szDescription + szXML should look like.
// The debugger just passes them through.
//
// Parameters:
//   pThread - thread for debug event.  May be null.
//   szName - short name of MDA.
//   szDescription - full description of MDA.
//   szXML - xml string for MDA.
//   bAttach - do a JIT-attach
//-----------------------------------------------------------------------------
void Debugger::SendMDANotification(
    Thread * pThread, // may be NULL. Lets us send on behalf of other threads.
    SString * szName,
    SString * szDescription,
    SString * szXML,
    CorDebugMDAFlags flags,
    BOOL bAttach
)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    PREFIX_ASSUME(szName != NULL);
    PREFIX_ASSUME(szDescription != NULL);
    PREFIX_ASSUME(szXML != NULL);

    // Note: we normally don't send events like this when there is an unrecoverable error. However,
    // if a host attempts to setup fiber mode on a thread, then we'll set an unrecoverable error
    // and use an MDA to 1) tell the user and 2) get the Right Side to notice the unrecoverable error.
    // Therefore, we'll go ahead and send a MDA event if the unrecoverable error is
    // CORDBG_E_CANNOT_DEBUG_FIBER_PROCESS.
    DebuggerIPCControlBlock *pDCB = m_pRCThread->GetDCB();


    // If the MDA is ocuring very early in startup before the DCB is setup, then bail.
    if (pDCB == NULL)
    {
        return;
    }

    if (CORDBUnrecoverableError(this) && (pDCB->m_errorHR != CORDBG_E_CANNOT_DEBUG_FIBER_PROCESS))
    {
        return;
    }

    // Validate flags. Make sure that folks don't start passing flags that we don't handle.
    // If pThread != current thread, caller should either pass in MDA_FLAG_SLIP or guarantee
    // that pThread is not slipping.
    _ASSERTE((flags & ~(MDA_FLAG_SLIP)) == 0);

    // Helper thread should not be triggering MDAs. The helper thread is executing code in a very constrained
    // and controlled region and shouldn't be able to do anything dangerous.
    // If we revise this in the future, we should probably just post the event to the RS w/ use the MDA_FLAG_SLIP flag,
    // and then not bother suspending the runtime. The RS will get it on its next event.
    // The jit-attach logic below assumes we're not on the helper. (If we are on the helper, then a debugger should already
    // be attached)
    if (ThisIsHelperThreadWorker())
    {
        CONSISTENCY_CHECK_MSGF(false, ("MDA '%s' fired on *helper* thread.\r\nDesc:%s",
            szName->GetUnicode(), szDescription->GetUnicode()
        ));

        // If for some reason we're wrong about the assert above, we'll just ignore the MDA (rather than potentially deadlock)
        return;
    }

    // Public entry point into the debugger. May cause a jit-attach, so we may need to be lazily-init.
    if (!HasLazyData())
    {
        DebuggerLockHolder dbgLockHolder(this);
        // This is an entry path into the debugger, so make sure we're inited.
        LazyInit();
    }


    // Cases:
    // 1) Debugger already attached, send event normally (ignore severity)
    // 2) No debugger attached, Non-severe probe - ignore.
    // 3) No debugger attached, Severe-probe - do a jit-attach.
    bool fTryJitAttach = bAttach == TRUE;

    // Check case #2 - no debugger, and no jit-attach. Early opt out.
    if (!CORDebuggerAttached() && !fTryJitAttach)
    {
        return;
    }

    if (pThread == NULL)
    {
        // If there's no thread object, then we're not blocking after the event,
        // and thus this probe may slip.
        flags = (CorDebugMDAFlags) (flags | MDA_FLAG_SLIP);
    }

    {
        GCX_PREEMP_EEINTERFACE_TOGGLE_IFTHREAD();

        // For "Severe" probes, we'll do a jit attach dialog
        if (fTryJitAttach)
        {
            // May return:
            // - S_OK if we do a jit-attach,
            // - S_FALSE if a debugger is already attached.
            // - Error in other cases..

            JitAttach(pThread, NULL, TRUE, FALSE);
        }

        // Debugger may be attached now...
        if (CORDebuggerAttached())
        {
            SendMDANotificationParams params(pThread, szName, szDescription, szXML, flags);

            // Non-attach case. Send like normal event.
            // This includes if someone launch the debugger during the meantime.
            // just send the event
            SENDIPCEVENT_BEGIN(this, pThread);

            // Send Log message event to the Right Side
            SendRawMDANotification(&params);

            // Stop all Runtime threads
            // Even if we don't have a managed thead object, this will catch us at the next good spot.
            TrapAllRuntimeThreads();

            // Let other Runtime threads handle their events.
            SENDIPCEVENT_END;
        }
    } // end of GCX_PREEMP_EEINTERFACE_TOGGLE()
}

//*************************************************************
// This method sends a log message over to the right side for the debugger to log it.
//
// The CLR doesn't assign any semantics to the level or cateogory values.
// The BCL has a level convention (LoggingLevels enum), but this isn't exposed publicly,
// so we shouldn't base our behavior on it in any way.
//*************************************************************
void Debugger::SendLogMessage(int iLevel,
                              SString * pSwitchName,
                              SString * pMessage)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::SLM: Sending log message.\n"));

    // Send the message only if the debugger is attached to this appdomain.
    // Note the the debugger may detach at any time, so we'll have to check
    // this again after we get the lock.
    AppDomain *pAppDomain = g_pEEInterface->GetThread()->GetDomain();

    if (!CORDebuggerAttached())
    {
        return;
    }

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);

    // Send Log message event to the Right Side
    SendRawLogMessage(
        pThread,
        pAppDomain,
        iLevel,
        pSwitchName,
        pMessage);

    // Stop all Runtime threads
    TrapAllRuntimeThreads();

    // Let other Runtime threads handle their events.
    SENDIPCEVENT_END;
}


//*************************************************************
//
// Helper function to just send LogMessage event. Can be called on either
// helper thread or managed thread.
//
//*************************************************************
void Debugger::SendRawLogMessage(
    Thread                                    *pThread,
    AppDomain                                 *pAppDomain,
    int                                        iLevel,
    SString *   pCategory,
    SString *   pMessage
)
{
    DebuggerIPCEvent* ipce;


    // We should have hold debugger lock
    // This can happen on either native helper thread or managed thread
    _ASSERTE(ThreadHoldsLock());

    // It's possible that the debugger dettached while we were waiting
    // for our lock. Check again and abort the event if it did.
    if (!CORDebuggerAttached())
    {
        return;
    }

    ipce = m_pRCThread->GetIPCEventSendBuffer();

    // Send a LogMessage event to the Right Side
    InitIPCEvent(ipce,
                 DB_IPCE_FIRST_LOG_MESSAGE,
                 pThread,
                 pAppDomain);

    ipce->FirstLogMessage.iLevel = iLevel;
    ipce->FirstLogMessage.szCategory.SetString(pCategory->GetUnicode());
    SetLSBufferFromSString(&ipce->FirstLogMessage.szContent, *pMessage);

    m_pRCThread->SendIPCEvent();
}


// This function sends a message to the right side informing it about
// the creation/modification of a LogSwitch
void Debugger::SendLogSwitchSetting(int iLevel,
                                    int iReason,
                                    __in_z LPCWSTR pLogSwitchName,
                                    __in_z LPCWSTR pParentSwitchName)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "D::SLSS: Sending log switch message switch=%S parent=%S.\n",
        pLogSwitchName, pParentSwitchName));

    // Send the message only if the debugger is attached to this appdomain.
    if (!CORDebuggerAttached())
    {
        return;
    }

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);

    if (CORDebuggerAttached())
    {
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce,
                     DB_IPCE_LOGSWITCH_SET_MESSAGE,
                     pThread,
                     pThread->GetDomain());

        ipce->LogSwitchSettingMessage.iLevel = iLevel;
        ipce->LogSwitchSettingMessage.iReason = iReason;


        ipce->LogSwitchSettingMessage.szSwitchName.SetString(pLogSwitchName);

        if (pParentSwitchName == NULL)
        {
            pParentSwitchName = W("");
        }

        ipce->LogSwitchSettingMessage.szParentSwitchName.SetString(pParentSwitchName);

        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::SLSS: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;
}

// send a custom debugger notification to the RS
// Arguments:
//     input: pThread    - thread on which the notification occurred
//            pDomain    - domain file for the domain in which the notification occurred
//            classToken - metadata token for the type of the notification object
void Debugger::SendCustomDebuggerNotification(Thread * pThread,
                                              DomainFile * pDomain,
                                              mdTypeDef classToken)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10000, "D::SLM: Sending log message.\n"));

    // Send the message only if the debugger is attached to this appdomain.
    // Note the the debugger may detach at any time, so we'll have to check
    // this again after we get the lock.
    if (!CORDebuggerAttached())
    {
        return;
    }

    Thread *curThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, curThread);

    if (CORDebuggerAttached())
    {
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce,
                     DB_IPCE_CUSTOM_NOTIFICATION,
                     curThread,
                     curThread->GetDomain());

        VMPTR_DomainFile vmDomainFile = VMPTR_DomainFile::MakePtr(pDomain);

        ipce->CustomNotification.classToken = classToken;
        ipce->CustomNotification.vmDomainFile = vmDomainFile;


        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::SCDN: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;
}


//-----------------------------------------------------------------------------
//
// Add the AppDomain to the list stored in the IPC block.  It adds the id and
// the name.
//
// Arguments:
//     pAppDomain - The runtime app domain object to add.
//
// Return Value:
//     S_OK on success, else detailed error code.
//
HRESULT Debugger::AddAppDomainToIPC(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    LPCWSTR szName = NULL;

    LOG((LF_CORDB, LL_INFO100, "D::AADTIPC: Executing AADTIPC for AppDomain 0x%08x.\n",
        pAppDomain));

    STRESS_LOG1(LF_CORDB, LL_INFO10000, "D::AADTIPC: AddAppDomainToIPC:%#08x\n",
            pAppDomain);



    _ASSERTE(m_pAppDomainCB->m_iTotalSlots > 0);
    _ASSERTE(m_pAppDomainCB->m_rgListOfAppDomains != NULL);

    {
        //
        // We need to synchronize this routine with the attach logic.  The "normal"
        // attach case uses the HelperThread and TrapAllRuntimeThreads to synchronize
        // the runtime before sending any of the events (including AppDomainCreates)
        // to the right-side.  Thus, we can synchronize with this case by forcing us
        // to go co-operative.  If we were already co-op, then the helper thread will
        // wait to start the attach until all co-op threads are paused.  If we were
        // pre-emptive, then going co-op will suspend us until the HelperThread finishes.
        //
        // The second case is under the IPC event for ATTACHING, which is where there are
        // zero app domains, so it is considered an 'early attach' case.  To synchronize
        // with this we have to grab and hold the AppDomainDB lock.
        //

        GCX_COOP();

        // Lock the list
        if (!m_pAppDomainCB->Lock())
        {
            return E_FAIL;
        }

        // Get a free entry from the list
        AppDomainInfo *pAppDomainInfo = m_pAppDomainCB->GetFreeEntry();

        // Function returns NULL if the list is full and a realloc failed.
        if (!pAppDomainInfo)
        {
            hr = E_OUTOFMEMORY;
            goto LErrExit;
        }

        // Now set the AppDomainName.

        /*
         * TODO :
         *
         * Make sure that returning NULL here does not result in a catastrophic
         * failure.
         *
         * GetFriendlyNameNoThrow may call SetFriendlyName, which may call
         * UpdateAppDomainEntryInIPC. There is no recursive death, however, because
         * the AppDomainInfo object does not contain a pointer to the app domain
         * yet.
         */
        szName = pAppDomain->GetFriendlyNameForDebugger();
        pAppDomainInfo->SetName(szName);

        // Save on to the appdomain pointer
        pAppDomainInfo->m_pAppDomain = pAppDomain;

        // bump the used slot count
        m_pAppDomainCB->m_iNumOfUsedSlots++;

LErrExit:
        // UnLock the list
        m_pAppDomainCB->Unlock();

        // Send event to debugger if one is attached.
        if (CORDebuggerAttached())
        {
            SendCreateAppDomainEvent(pAppDomain);
        }
    }

    return hr;
}


/******************************************************************************
 * Remove the AppDomain from the list stored in the IPC block and send an ExitAppDomain
 * event to the debugger if attached.
 ******************************************************************************/
HRESULT Debugger::RemoveAppDomainFromIPC (AppDomain *pAppDomain)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    HRESULT hr = E_FAIL;

    LOG((LF_CORDB, LL_INFO100, "D::RADFIPC: Executing RADFIPC for AppDomain 0x%08x.\n",
        pAppDomain));

    // if none of the slots are occupied, then simply return.
    if (m_pAppDomainCB->m_iNumOfUsedSlots == 0)
        return hr;

    // Lock the list
    if (!m_pAppDomainCB->Lock())
        return (E_FAIL);


    // Look for the entry
    AppDomainInfo *pADInfo = m_pAppDomainCB->FindEntry(pAppDomain);

    // Shouldn't be trying to remove an appdomain that was never added
    if (!pADInfo)
    {
        // We'd like to assert this, but there is a small window where we may have
        // called AppDomain::Init (and so it's fair game to call Stop, and hence come here),
        // but not yet published the app domain.
        // _ASSERTE(!"D::RADFIPC: trying to remove an AppDomain that was never added");
        hr = (E_FAIL);
        goto ErrExit;
    }

    // Release the entry
    m_pAppDomainCB->FreeEntry(pADInfo);

ErrExit:
    // UnLock the list
    m_pAppDomainCB->Unlock();

    //
    // The Debugger expects to never get an unload event for the default AppDomain.
    //

    return hr;
}

/******************************************************************************
 * Update the AppDomain in the list stored in the IPC block.
 ******************************************************************************/
HRESULT Debugger::UpdateAppDomainEntryInIPC(AppDomain *pAppDomain)
{
    CONTRACTL
    {
        NOTHROW;
        if (GetThreadNULLOk()) { GC_TRIGGERS;} else {DISABLED(GC_NOTRIGGER);}
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    LPCWSTR szName = NULL;

    LOG((LF_CORDB, LL_INFO100,
         "D::UADEIIPC: Executing UpdateAppDomainEntryInIPC ad:0x%x.\n",
         pAppDomain));

    // if none of the slots are occupied, then simply return.
    if (m_pAppDomainCB->m_iNumOfUsedSlots == 0)
        return (E_FAIL);

    // Lock the list
    if (!m_pAppDomainCB->Lock())
        return (E_FAIL);

    // Look up the info entry
    AppDomainInfo *pADInfo = m_pAppDomainCB->FindEntry(pAppDomain);

    if (!pADInfo)
    {
        hr = E_FAIL;
        goto ErrExit;
    }

    // Update the name only if new name is non-null
    szName = pADInfo->m_pAppDomain->GetFriendlyNameForDebugger();
    pADInfo->SetName(szName);

    LOG((LF_CORDB, LL_INFO100,
         "D::UADEIIPC: New name:%ls (AD:0x%x)\n", pADInfo->m_szAppDomainName,
         pAppDomain));

ErrExit:
    // UnLock the list
    m_pAppDomainCB->Unlock();

    return hr;
}

HRESULT Debugger::CopyModulePdb(Module* pRuntimeModule)
{
    CONTRACTL
    {
        THROWS;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        PRECONDITION(ThisIsHelperThread());
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pRuntimeModule->IsVisibleToDebugger())
    {
        return S_OK;
    }

    HRESULT hr = S_OK;

    return hr;
}

/******************************************************************************
 * When attaching to a process, this is called to enumerate all of the
 * AppDomains currently in the process and allow modules pdbs to be copied over to the shadow dir maintaining out V2 in-proc behaviour.
 ******************************************************************************/
HRESULT Debugger::IterateAppDomainsForPdbs()
{
    CONTRACTL
    {
        THROWS;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;

        PRECONDITION(ThisIsHelperThread());
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG0(LF_CORDB, LL_INFO100, "Entered function IterateAppDomainsForPdbs()\n");
    HRESULT hr = S_OK;

    // Lock the list
    if (!m_pAppDomainCB->Lock())
        return (E_FAIL);

    // Iterate through the app domains
    AppDomainInfo *pADInfo = m_pAppDomainCB->FindFirst();

    while (pADInfo)
    {
        STRESS_LOG2(LF_CORDB, LL_INFO100, "Iterating over domain AD:%#08x %ls\n", pADInfo->m_pAppDomain, pADInfo->m_szAppDomainName);

        AppDomain::AssemblyIterator i;
        i = pADInfo->m_pAppDomain->IterateAssembliesEx((AssemblyIterationFlags)(kIncludeLoaded | kIncludeLoading | kIncludeExecution));
        CollectibleAssemblyHolder<DomainAssembly *> pDomainAssembly;
        while (i.Next(pDomainAssembly.This()))
        {
            if (!pDomainAssembly->IsVisibleToDebugger())
                continue;

            DomainAssembly::ModuleIterator j = pDomainAssembly->IterateModules(kModIterIncludeLoading);
            while (j.Next())
            {
                DomainFile * pDomainFile = j.GetDomainFile();
                if (!pDomainFile->ShouldNotifyDebugger())
                    continue;

                Module* pRuntimeModule = pDomainFile->GetModule();
                CopyModulePdb(pRuntimeModule);
            }
            if (pDomainAssembly->ShouldNotifyDebugger())
            {
                CopyModulePdb(pDomainAssembly->GetModule());
            }
        }

        // Get the next appdomain in the list
        pADInfo = m_pAppDomainCB->FindNext(pADInfo);
    }

    // Unlock the list
    m_pAppDomainCB->Unlock();

    STRESS_LOG0(LF_CORDB, LL_INFO100, "Exiting function IterateAppDomainsForPdbs\n");

    return hr;
}


/******************************************************************************
 *
 ******************************************************************************/
HRESULT Debugger::InitAppDomainIPC(void)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;

        PRECONDITION(CheckPointer(m_pAppDomainCB));
    }
    CONTRACTL_END;

    // Ensure that if we throw here, the Terminate will get called and cleanup all resources.
    // This will make Init an atomic operation - it either fully inits or fully fails.
    class EnsureCleanup
    {
        Debugger * m_pThis;

    public:
        EnsureCleanup(Debugger * pThis)
        {
            m_pThis = pThis;
        }

        void SupressCleanup()
        {
            m_pThis = NULL;
        }

        ~EnsureCleanup()
        {
            if (m_pThis != NULL)
            {
                m_pThis->TerminateAppDomainIPC();
            }
        }
    } hEnsureCleanup(this);

    DWORD dwStrLen = 0;
    SString szExeName;
    int i;

    // all fields in the object can be zero initialized.
    // If we throw, before fully initializing this, then cleanup won't try to free
    // uninited values.
    ZeroMemory(m_pAppDomainCB, sizeof(*m_pAppDomainCB));

    // Create a mutex to allow the Left and Right Sides to properly
    // synchronize. The Right Side will spin until m_hMutex is valid,
    // then it will acquire it before accessing the data.
    HandleHolder hMutex(WszCreateMutex(NULL, TRUE/*hold*/, NULL));
    if (hMutex == NULL)
    {
        ThrowLastError();
    }
    if (!m_pAppDomainCB->m_hMutex.SetLocal(hMutex))
    {
        ThrowLastError();
    }
    hMutex.SuppressRelease();

    m_pAppDomainCB->m_iSizeInBytes = INITIAL_APP_DOMAIN_INFO_LIST_SIZE *
                                                sizeof (AppDomainInfo);

    // Number of slots in AppDomainListElement array
    m_pAppDomainCB->m_rgListOfAppDomains = new AppDomainInfo[INITIAL_APP_DOMAIN_INFO_LIST_SIZE];
    _ASSERTE(m_pAppDomainCB->m_rgListOfAppDomains != NULL); // throws on oom


    m_pAppDomainCB->m_iTotalSlots = INITIAL_APP_DOMAIN_INFO_LIST_SIZE;

    // Initialize each AppDomainListElement
    for (i = 0; i < INITIAL_APP_DOMAIN_INFO_LIST_SIZE; i++)
    {
        m_pAppDomainCB->m_rgListOfAppDomains[i].FreeEntry();
    }

    // also initialize the process name
    dwStrLen = WszGetModuleFileName(NULL,
                                    szExeName);


    // If we couldn't get the name, then use a nice default.
    if (dwStrLen == 0)
    {
        szExeName.Set(W("<NoProcessName>"));
        dwStrLen = szExeName.GetCount();
    }

    // If we got the name, copy it into a buffer. dwStrLen is the
    // count of characters in the name, not including the null
    // terminator.
    m_pAppDomainCB->m_szProcessName = new WCHAR[dwStrLen + 1];
    _ASSERTE(m_pAppDomainCB->m_szProcessName != NULL); // throws on oom

    wcscpy_s(m_pAppDomainCB->m_szProcessName, dwStrLen + 1, szExeName);

    // Add 1 to the string length so the Right Side will copy out the
    // null terminator, too.
    m_pAppDomainCB->m_iProcessNameLengthInBytes = (dwStrLen + 1) * sizeof(WCHAR);

    if (m_pAppDomainCB->m_hMutex != NULL)
    {
        m_pAppDomainCB->Unlock();
    }

    hEnsureCleanup.SupressCleanup();
    return S_OK;
}

/******************************************************************************
 * Unitialize the AppDomain IPC block
 * Returns:
 * S_OK -if fully unitialized
 * E_FAIL - if we can't get ownership of the block, and thus no unitialization
 *          work is done.
 ******************************************************************************/
HRESULT Debugger::TerminateAppDomainIPC(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // If we have no AppDomain block, then we can consider it's already terminated.
    if (m_pAppDomainCB == NULL)
        return S_OK;

    HRESULT hr = S_OK;

    // Lock the list
    // If there's no mutex, then we're in a partially created state.
    // This means InitAppDomainIPC failed halfway through. But we're still thread safe
    // since other threads can't access us if we don't have the mutex.
    if ((m_pAppDomainCB->m_hMutex != NULL) && !m_pAppDomainCB->Lock())
    {
        // The callers don't check our return value, we may want to know when we can't gracefully clean up
       LOG((LF_CORDB, LL_INFO10, "Debugger::TerminateAppDomainIPC: Failed to get AppDomain IPC lock, not cleaning up.\n"));

        // If the lock is valid, but we can't get it, then we can't really
        // uninitialize since someone else is using the block.
        return (E_FAIL);
    }

    // The shared IPC segment could still be around after the debugger
    // object has been destroyed during process shutdown. So, reset
    // the UsedSlots count to 0 so that any out of process clients
    // enumeratingthe app domains in this process see 0 AppDomains.
    m_pAppDomainCB->m_iNumOfUsedSlots = 0;
    m_pAppDomainCB->m_iTotalSlots = 0;

    // Now delete the memory allocated for AppDomainInfo  array
    delete [] m_pAppDomainCB->m_rgListOfAppDomains;
    m_pAppDomainCB->m_rgListOfAppDomains = NULL;

    delete [] m_pAppDomainCB->m_szProcessName;
    m_pAppDomainCB->m_szProcessName = NULL;
    m_pAppDomainCB->m_iProcessNameLengthInBytes = 0;

    // Set the mutex handle to NULL.
    // If the Right Side acquires the mutex, it will verify
    // that the handle is still not NULL. If it is, then it knows it
    // really lost.
    RemoteHANDLE m = m_pAppDomainCB->m_hMutex;
    m_pAppDomainCB->m_hMutex.m_hLocal = NULL;

    // And bring us back to a fully unintialized state.
    ZeroMemory(m_pAppDomainCB, sizeof(*m_pAppDomainCB));

    // We're done. release and close the mutex.  Note that this must be done
    // after we clear it out above to ensure there is no race condition.
    if( m != NULL )
    {
        VERIFY(ReleaseMutex(m));
        m.Close();
    }

    return hr;
}


#ifndef DACCESS_COMPILE

//
// FuncEvalSetup sets up a function evaluation for the given method on the given thread.
//
HRESULT Debugger::FuncEvalSetup(DebuggerIPCE_FuncEvalInfo *pEvalInfo,
                                BYTE **argDataArea,
                                DebuggerEval **debuggerEvalKey)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Thread *pThread = pEvalInfo->vmThreadToken.GetRawPtr();


    //
    // If TS_AbortRequested (which may have been set by a pending FuncEvalAbort),
    // we will not be able to do a new func-eval
    //
    // <TODO>@TODO: Remember the current value of m_State, reset m_State as appropriate,
    // do the new func-eval, and then set m_State to the original value</TODO>
    if (pThread->m_State & Thread::TS_AbortRequested)
        return CORDBG_E_FUNC_EVAL_BAD_START_POINT;

    if (g_fProcessDetach)
        return CORDBG_E_FUNC_EVAL_BAD_START_POINT;

    // If there is no guard page on this thread, then we've taken a stack overflow exception and can't run managed
    // code on this thread. Therefore, we can't do a func eval on this thread.
    if (!pThread->DetermineIfGuardPagePresent())
    {
        return CORDBG_E_ILLEGAL_IN_STACK_OVERFLOW;
    }

    bool fInException = pEvalInfo->evalDuringException;

    // The thread has to be at a GC safe place for now, just in case the func eval causes a collection. Processing an
    // exception also counts as a "safe place." Eventually, we'd like to have to avoid this check and eval anyway, but
    // that's a way's off...
    if (!fInException && !g_pDebugger->IsThreadAtSafePlace(pThread))
        return CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT;

    // For now, we assume that the target thread must be stopped in managed code due to a single step or a
    // breakpoint. Being stopped while sending a first or second chance exception is also valid, and there may or may
    // not be a filter context when we do a func eval from such places. This will loosen over time, eventually allowing
    // threads that are stopped anywhere in managed code to perform func evals.
    CONTEXT *filterContext = GetManagedStoppedCtx(pThread);

    if (filterContext == NULL && !fInException)
    {
        return CORDBG_E_ILLEGAL_AT_GC_UNSAFE_POINT;
    }

    if (filterContext != NULL && ::GetSP(filterContext) != ALIGN_DOWN(::GetSP(filterContext), STACK_ALIGN_SIZE))
    {
        // SP is not aligned, we cannot do a FuncEval here
        LOG((LF_CORDB, LL_INFO1000, "D::FES SP is unaligned"));
        return CORDBG_E_FUNC_EVAL_BAD_START_POINT;
    }

    // Allocate the breakpoint instruction info for the debugger info in in executable memory.
    DebuggerHeap *pHeap = g_pDebugger->GetInteropSafeExecutableHeap_NoThrow();
    if (pHeap == NULL)
    {
        return E_OUTOFMEMORY;
    }

    DebuggerEvalBreakpointInfoSegment *bpInfoSegmentRX = (DebuggerEvalBreakpointInfoSegment*)pHeap->Alloc(sizeof(DebuggerEvalBreakpointInfoSegment));
    if (bpInfoSegmentRX == NULL)
    {
        return E_OUTOFMEMORY;
    }

    // Create a DebuggerEval to hold info about this eval while its in progress. Constructor copies the thread's
    // CONTEXT.
    DebuggerEval *pDE = new (interopsafe, nothrow) DebuggerEval(filterContext, pEvalInfo, fInException, bpInfoSegmentRX);

    if (pDE == NULL)
    {
        return E_OUTOFMEMORY;
    }
    else if (!pDE->Init())
    {
        // We fail to change the m_breakpointInstruction field to PAGE_EXECUTE_READWRITE permission.
        return E_FAIL;
    }

    SIZE_T argDataAreaSize = 0;

    argDataAreaSize += pEvalInfo->genericArgsNodeCount * sizeof(DebuggerIPCE_TypeArgData);

    if ((pEvalInfo->funcEvalType == DB_IPCE_FET_NORMAL) ||
        (pEvalInfo->funcEvalType == DB_IPCE_FET_NEW_OBJECT) ||
        (pEvalInfo->funcEvalType == DB_IPCE_FET_NEW_OBJECT_NC))
        argDataAreaSize += pEvalInfo->argCount * sizeof(DebuggerIPCE_FuncEvalArgData);
    else if (pEvalInfo->funcEvalType == DB_IPCE_FET_NEW_STRING)
        argDataAreaSize += pEvalInfo->stringSize;
    else if (pEvalInfo->funcEvalType == DB_IPCE_FET_NEW_ARRAY)
        argDataAreaSize += pEvalInfo->arrayRank * sizeof(SIZE_T);

    if (argDataAreaSize > 0)
    {
        pDE->m_argData = new (interopsafe, nothrow) BYTE[argDataAreaSize];

        if (pDE->m_argData == NULL)
        {
            DeleteInteropSafeExecutable(pDE);
            return E_OUTOFMEMORY;
        }

        // Pass back the address of the argument data area so the right side can write to it for us.
        *argDataArea = pDE->m_argData;
    }

    // Set the thread's IP (in the filter context) to our hijack function if we're stopped due to a breakpoint or single
    // step.
    if (!fInException)
    {
        _ASSERTE(filterContext != NULL);

        ::SetIP(filterContext, (UINT_PTR)GetEEFuncEntryPoint(::FuncEvalHijack));

        // Don't be fooled into thinking you can push things onto the thread's stack now. If the thread is stopped at a
        // breakpoint or from a single step, then its really suspended in the SEH filter. ESP in the thread's CONTEXT,
        // therefore, points into the middle of the thread's current stack. So we pass things we need in the hijack in
        // the thread's registers.

        // Set the first argument to point to the DebuggerEval.
#if defined(TARGET_X86)
        filterContext->Eax = (DWORD)pDE;
#elif defined(TARGET_AMD64)
#ifdef UNIX_AMD64_ABI
        filterContext->Rdi = (SIZE_T)pDE;
#else // UNIX_AMD64_ABI
        filterContext->Rcx = (SIZE_T)pDE;
#endif // !UNIX_AMD64_ABI
#elif defined(TARGET_ARM)
        filterContext->R0 = (DWORD)pDE;
#elif defined(TARGET_ARM64)
        filterContext->X0 = (SIZE_T)pDE;
#else
        PORTABILITY_ASSERT("Debugger::FuncEvalSetup is not implemented on this platform.");
#endif

        //
        // To prevent GCs until the func-eval gets a chance to run, we increment the counter here.
        // We only need to do this if we have changed the filter CONTEXT, since the stack will be unwalkable
        // in this case.
        //
        g_pDebugger->IncThreadsAtUnsafePlaces();
    }
    else
    {
        HRESULT hr = CheckInitPendingFuncEvalTable();

        if (FAILED(hr))
        {
            DeleteInteropSafeExecutable(pDE);  // Note this runs the destructor for DebuggerEval, which releases its internal buffers
            return (hr);
        }
        // If we're in an exception, then add a pending eval for this thread. This will cause us to perform the func
        // eval when the user continues the process after the current exception event.
        GetPendingEvals()->AddPendingEval(pDE->m_thread, pDE);
    }


    // Return that all went well. Tracing the stack at this point should not show that the func eval is setup, but it
    // will show a wrong IP, so it shouldn't be done.
    *debuggerEvalKey = pDE;

    LOG((LF_CORDB, LL_INFO100000, "D:FES for pDE:%08x evalType:%d on thread %#x, id=0x%x\n",
        pDE, pDE->m_evalType, pThread, GetThreadIdHelper(pThread)));

    return S_OK;
}

//
// FuncEvalAbort: Does a gentle abort of a func-eval already in progress.
//    Because this type of abort waits for the thread to get to a good state,
//    it may never return, or may time out.
//

//
// Wait at most 0.5 seconds.
//
#define FUNC_EVAL_DEFAULT_TIMEOUT_VALUE 500

HRESULT
Debugger::FuncEvalAbort(
    DebuggerEval *debuggerEvalKey
    )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DebuggerEval *pDE = (DebuggerEval*) debuggerEvalKey;
    HRESULT hr = S_OK;
    CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(&hr, GetCanary());
    if (FAILED(hr))
    {
        return hr;
    }


    if (pDE->m_aborting == DebuggerEval::FE_ABORT_NONE)
    {
        // Remember that we're aborting this func eval.
        pDE->m_aborting = DebuggerEval::FE_ABORT_NORMAL;

        LOG((LF_CORDB, LL_INFO1000,
             "D::FEA: performing UserAbort on thread %#x, id=0x%x\n",
             pDE->m_thread, GetThreadIdHelper(pDE->m_thread)));

        if (!g_fProcessDetach && !pDE->m_completed)
        {
            //
            // Perform a stop on the thread that the eval is running on.
            // This will cause a ThreadAbortException to be thrown on the thread.
            //
            EX_TRY
            {
                hr = pDE->m_thread->UserAbort(EEPolicy::TA_Safe, (DWORD)FUNC_EVAL_DEFAULT_TIMEOUT_VALUE);
                if (hr == HRESULT_FROM_WIN32(ERROR_TIMEOUT))
                {
                    hr = S_OK;
                }
            }
            EX_CATCH
            {
                _ASSERTE(!"Unknown exception from UserAbort(), not expected");
            }
            EX_END_CATCH(EX_RETHROW);

        }

        LOG((LF_CORDB, LL_INFO1000, "D::FEA: UserAbort complete.\n"));
    }

    return hr;
}

//
// FuncEvalRudeAbort: Does a rude abort of a func-eval in progress.  This
//     leaves the thread in an undetermined state.
//
HRESULT
Debugger::FuncEvalRudeAbort(
    DebuggerEval *debuggerEvalKey
    )
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(&hr, GetCanary());
    if (FAILED(hr))
    {
        return hr;
    }


    DebuggerEval *pDE = debuggerEvalKey;


    if (!(pDE->m_aborting & DebuggerEval::FE_ABORT_RUDE))
    {
        //
        // Remember that we're aborting this func eval.
        //
        pDE->m_aborting = (DebuggerEval::FUNC_EVAL_ABORT_TYPE)(pDE->m_aborting | DebuggerEval::FE_ABORT_RUDE);

        LOG((LF_CORDB, LL_INFO1000,
             "D::FEA: performing RudeAbort on thread %#x, id=0x%x\n",
             pDE->m_thread, Debugger::GetThreadIdHelper(pDE->m_thread)));

        if (!g_fProcessDetach && !pDE->m_completed)
        {
            //
            // Perform a stop on the thread that the eval is running on.
            // This will cause a ThreadAbortException to be thrown on the thread.
            //
            EX_TRY
            {
                hr = pDE->m_thread->UserAbort(EEPolicy::TA_Rude, (DWORD)FUNC_EVAL_DEFAULT_TIMEOUT_VALUE);
                if (hr == HRESULT_FROM_WIN32(ERROR_TIMEOUT))
                {
                    hr = S_OK;
                }
            }
            EX_CATCH
            {
                    _ASSERTE(!"Unknown exception from UserAbort(), not expected");
                    EX_RETHROW;
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }

        LOG((LF_CORDB, LL_INFO1000, "D::FEA: RudeAbort complete.\n"));
    }

    return hr;
}

//
// FuncEvalCleanup cleans up after a function evaluation is released.
//
HRESULT Debugger::FuncEvalCleanup(DebuggerEval *debuggerEvalKey)
{
    LIMITED_METHOD_CONTRACT;

    DebuggerEval *pDE = debuggerEvalKey;

    _ASSERTE(pDE->m_completed);

    LOG((LF_CORDB, LL_INFO1000, "D::FEC: pDE:%08x 0x%08x, id=0x%x\n",
         pDE, pDE->m_thread, GetThreadIdHelper(pDE->m_thread)));

    DeleteInteropSafeExecutable(pDE->m_bpInfoSegment);
    DeleteInteropSafe(pDE);

    return S_OK;
}

#endif // ifndef DACCESS_COMPILE

//
// SetReference sets an object reference for the Right Side,
// respecting the write barrier for references that are in the heap.
//
HRESULT Debugger::SetReference(void *objectRefAddress,
                               VMPTR_OBJECTHANDLE vmObjectHandle,
                               void *newReference)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT     hr = S_OK;

    hr = ValidateObject((Object *)newReference);
    if (FAILED(hr))
    {
        return hr;
    }


    // If the object ref isn't in a handle, then go ahead and use
    // SetObjectReference.
    if (vmObjectHandle.IsNull())
    {
        OBJECTREF *dst = (OBJECTREF*)objectRefAddress;
        OBJECTREF  src = *((OBJECTREF*)&newReference);

        SetObjectReference(dst, src);
    }
    else
    {

            // If the object reference to set is inside of a handle, then
            // fixup the handle.
            OBJECTHANDLE h = vmObjectHandle.GetRawPtr();
            OBJECTREF  src = *((OBJECTREF*)&newReference);

            IGCHandleManager* mgr = GCHandleUtilities::GetGCHandleManager();
            mgr->StoreObjectInHandle(h, OBJECTREFToObject(src));
        }

    return S_OK;
}

//
// SetValueClass sets a value class for the Right Side, respecting the write barrier for references that are embedded
// within in the value class.
//
HRESULT Debugger::SetValueClass(void *oldData, void *newData, DebuggerIPCE_BasicTypeData * type)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    TypeHandle th;
    hr = BasicTypeInfoToTypeHandle(type, &th);

    if (FAILED(hr))
        return CORDBG_E_CLASS_NOT_LOADED;

    // Update the value class.
    CopyValueClass(oldData, newData, th.GetMethodTable());

    // Free the buffer that is holding the new data. This is a buffer that was created in response to a GET_BUFFER
    // message, so we release it with ReleaseRemoteBuffer.
    ReleaseRemoteBuffer((BYTE*)newData, true);

    return hr;
}

/******************************************************************************
 *
 ******************************************************************************/
HRESULT Debugger::SetILInstrumentedCodeMap(MethodDesc *fd,
                                           BOOL fStartJit,
                                           ULONG32 cILMapEntries,
                                           COR_IL_MAP rgILMapEntries[])
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS_FROM_GETJITINFO;
    }
    CONTRACTL_END;

    if (!HasLazyData())
    {
        DebuggerLockHolder dbgLockHolder(this);
        // This is an entry path into the debugger, so make sure we're inited.
        LazyInit();
    }

    DebuggerMethodInfo * dmi = GetOrCreateMethodInfo(fd->GetModule(), fd->GetMemberDef());
    if (dmi == NULL)
    {
        return E_OUTOFMEMORY;
    }

    dmi->SetInstrumentedILMap(rgILMapEntries, cILMapEntries);

    return S_OK;
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
void Debugger::EarlyHelperThreadDeath(void)
{
    WRAPPER_NO_CONTRACT;

    if (m_pRCThread)
        m_pRCThread->EarlyHelperThreadDeath();
}

//
// This tells the debugger that shutdown of the in-proc debugging services has begun. We need to know this during
// managed/unmanaged debugging so we can stop doing certian things to the process (like hijacking threads.)
//
void Debugger::ShutdownBegun(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;


    // Shouldn't be Debugger-stopped if we're shutting down.
    // However, shutdown can occur in preemptive mode. Thus if the RS does an AsyncBreak late
    // enough, then the LS will appear to be stopped but may still shutdown.
    // Since the debuggee can exit asynchronously at any time (eg, suppose somebody forcefully
    // kills it with taskman), this doesn't introduce a new case.
    // That aside, it would be great to be able to assert this:
    //_ASSERTE(!IsStopped());

    if (m_pRCThread != NULL)
    {
        DebuggerIPCControlBlock *dcb = m_pRCThread->GetDCB();

        if ((dcb != NULL) && (dcb->m_rightSideIsWin32Debugger))
            dcb->m_shutdownBegun = true;
    }
}

/*
 * LockDebuggerForShutdown
 *
 * This routine is used during shutdown to tell the in-process portion of the
 * debugger to synchronize with any threads that are currently using the
 * debugging facilities such that no more threads will run debugging services.
 *
 * This is accomplished by transitioning the debugger lock in to a state where
 * it will block all threads, except for the finalizer, shutdown, and helper thread.
 */
void Debugger::LockDebuggerForShutdown(void)
{
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    DebuggerLockHolder dbgLockHolder(this);

    // Shouldn't be Debugger-stopped if we're shutting down.
    // However, shutdown can occur in preemptive mode. Thus if the RS does an AsyncBreak late
    // enough, then the LS will appear to be stopped but may still shutdown.
    // Since the debuggee can exit asynchronously at any time (eg, suppose somebody forcefully
    // kills it with taskman), this doesn't introduce a new case.
    // That aside, it would be great to be able to assert this:
    //_ASSERTE(!IsStopped());

    // After setting this flag, nonspecial threads will not be able to
    // take the debugger lock.
    m_fShutdownMode = true;

    m_ignoreThreadDetach = TRUE;
#else
    DacNotImpl();
#endif
}


/*
 * DisableDebugger
 *
 * This routine is used by the EE to inform the debugger that it should block all
 * threads from executing as soon as it can.  Any thread entering the debugger can
 * block infinitely, as well.
 *
 * This is accomplished by transitioning the debugger lock into a mode where it will
 * block all threads infinitely rather than taking the lock.
 *
 */
void Debugger::DisableDebugger(void)
{
#ifndef DACCESS_COMPILE

    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    m_fDisabled = true;

    CORDBDebuggerSetUnrecoverableError(this, CORDBG_E_DEBUGGING_DISABLED, false);

#else
    DacNotImpl();
#endif
}


/****************************************************************************
 * This will perform the duties of the helper thread if none already exists.
 * This is called in the case that the loader lock is held and so no new
 * threads can be spun up to be the helper thread, so the existing thread
 * must be the helper thread until a new one can spin up.
 * This is also called in the shutdown case (g_fProcessDetach==true) and our
 * helper may have already been blown away.
 ***************************************************************************/
void Debugger::DoHelperThreadDuty()
{
    CONTRACTL
    {
        THROWS;
        WRAPPER(GC_TRIGGERS);
    }
    CONTRACTL_END;

    // This should not be a real helper thread.
    _ASSERTE(!IsDbgHelperSpecialThread());
    _ASSERTE(ThreadHoldsLock());

    // We may be here in the shutdown case (only if the shutdown started after we got here).
    // We'll get killed randomly anyways, so not much we can do.

    // These assumptions are based off us being called from TART.
    _ASSERTE(ThreadStore::HoldingThreadStore() || g_fProcessDetach); // got this from TART
    _ASSERTE(m_trappingRuntimeThreads); // We're only called from TART.
    _ASSERTE(!m_stopped); // we haven't sent the sync-complete yet.

    // Can't have 2 threads doing helper duty.
    _ASSERTE(m_pRCThread->GetDCB()->m_temporaryHelperThreadId == 0);

    LOG((LF_CORDB, LL_INFO1000,
         "D::SSCIPCE: helper thread is not ready, doing helper "
         "thread duty...\n"));

    // We're the temporary helper thread now.
    DWORD dwMyTID = GetCurrentThreadId();
    m_pRCThread->GetDCB()->m_temporaryHelperThreadId = dwMyTID;

    // Make sure the helper thread has something to wait on while
    // we're trying to be the helper thread.
    VERIFY(ResetEvent(m_pRCThread->GetHelperThreadCanGoEvent()));

    // We have not sent the sync-complete flare yet.

    // Now that we've synchronized, we'll eventually send the sync-complete. But we're currently within the
    // scope of sombody already sending an event. So unlock from that event so that we can send the sync-complete.
    // Don't release the debugger lock
    //
    UnlockFromEventSending(NULL);

    // We are the temporary helper thread. We will not deal with everything! But just pump for
    // continue.
    //
    m_pRCThread->TemporaryHelperThreadMainLoop();

    // We do not need to relock it since we never release it.
    LockForEventSending(NULL);
    _ASSERTE(ThreadHoldsLock());


    STRESS_LOG1(LF_CORDB, LL_INFO1000,
         "D::SSCIPCE: done doing helper thread duty. "
         "Current helper thread id=0x%x\n",
         m_pRCThread->GetDCB()->m_helperThreadId);

    // We're not the temporary helper thread anymore.
    _ASSERTE(m_pRCThread->GetDCB()->m_temporaryHelperThreadId == dwMyTID);
    m_pRCThread->GetDCB()->m_temporaryHelperThreadId = 0;

    // Let the helper thread go if its waiting on us.
    VERIFY(SetEvent(m_pRCThread->GetHelperThreadCanGoEvent()));
}



// This function is called from the EE to notify the right side
// whenever the name of a thread or AppDomain changes
//
// Notes:
//   This just sends a ping event to notify that the name has been changed.
//   It does not send the actual updated name. Instead, the debugger can query for the name.
//
//   For an AppDomain name change:
//   - pAppDoamin != NULL
//   - name retrieved via ICorDebugAppDomain::GetName
//
//   For a Thread name change:
//   - pAppDomain == NULL, pThread != NULL
//   - name retrieved via a func-eval of Thread::get_Name
HRESULT Debugger::NameChangeEvent(AppDomain *pAppDomain, Thread *pThread)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    // Don't try to send one of these if the thread really isn't setup
    // yet. This can happen when initially setting up an app domain,
    // before the appdomain create event has been sent. Since the app
    // domain create event hasn't been sent yet in this case, its okay
    // to do this...
    if (g_pEEInterface->GetThread() == NULL)
        return S_OK;

    // Skip if thread doesn't yet have native ID.
    // This can easily happen if an app sets Thread.Name before it calls Thread.Start.
    // Since this is just a ping-event, it's ignorable. The debugger can query the thread name at Thread.Start in this case.
    // This emulates whidbey semantics.
    if (pThread != NULL)
    {
        if (pThread->GetOSThreadId() == 0)
        {
            return S_OK;
        }
    }

    LOG((LF_CORDB, LL_INFO1000, "D::NCE: Sending NameChangeEvent 0x%x 0x%x\n",
        pAppDomain, pThread));

    Thread *curThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, curThread);

    if (CORDebuggerAttached())
    {

            DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce,
                     DB_IPCE_NAME_CHANGE,
                     curThread,
                     curThread->GetDomain());


        if (pAppDomain)
        {
            ipce->NameChange.eventType = APP_DOMAIN_NAME_CHANGE;
                ipce->NameChange.vmAppDomain.SetRawPtr(pAppDomain);
        }
        else
        {
            // Thread Name
            ipce->NameChange.eventType = THREAD_NAME_CHANGE;
            _ASSERTE (pThread);
            ipce->NameChange.vmThread.SetRawPtr(pThread);
        }

        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::NCE: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;

    return S_OK;

}

//---------------------------------------------------------------------------------------
//
// Send an event to the RS indicating that there's a Ctrl-C or Ctrl-Break.
//
// Arguments:
//    dwCtrlType - represents the type of the event (Ctrl-C or Ctrl-Break)
//
// Return Value:
//    Return TRUE if the event has been handled by the debugger.
//

BOOL Debugger::SendCtrlCToDebugger(DWORD dwCtrlType)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO1000, "D::SCCTD: Sending CtrlC Event 0x%x\n", dwCtrlType));

    // Prevent other Runtime threads from handling events.
    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);

    if (CORDebuggerAttached())
    {
        DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce,
                     DB_IPCE_CONTROL_C_EVENT,
                     pThread,
                     NULL);

        // The RS doesn't do anything with dwCtrlType
        m_pRCThread->SendIPCEvent();

        // Stop all Runtime threads
        TrapAllRuntimeThreads();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::SCCTD: Skipping SendIPCEvent because RS detached."));
    }

    SENDIPCEVENT_END;

    // now wait for notification from the right side about whether or not
    // the out-of-proc debugger is handling ControlC events.
    ::WaitForSingleObject(GetCtrlCMutex(), INFINITE);

    return GetDebuggerHandlingCtrlC();
}

// Allows the debugger to keep an up to date list of special threads
HRESULT Debugger::UpdateSpecialThreadList(DWORD cThreadArrayLength,
                                        DWORD *rgdwThreadIDArray)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(g_pRCThread != NULL);

    DebuggerIPCControlBlock *pIPC = g_pRCThread->GetDCB();
    _ASSERTE(pIPC);

    if (!pIPC)
        return (E_FAIL);

    // Save the thread list information, and mark the dirty bit so
    // the right side knows.
    pIPC->m_specialThreadList = rgdwThreadIDArray;
    pIPC->m_specialThreadListLength = cThreadArrayLength;
    pIPC->m_specialThreadListDirty = true;

    return (S_OK);
}

//
// If a thread is Win32 suspended right after hitting a breakpoint instruction, but before the OS has transitioned the
// thread over to the user-level exception dispatching logic, then we may see the IP pointing after the breakpoint
// instruction. There are times when the Runtime will use the IP to try to determine what code as run in the prolog or
// epilog, most notably when unwinding a frame. If the thread is suspended in such a case, then the unwind will believe
// that the instruction that the breakpoint replaced has really been executed, which is not true. This confuses the
// unwinding logic. This function is called from Thread::HandledJITCase() to help us recgonize when this may have
// happened and allow us to skip the unwind and abort the HandledJITCase.
//
// The criteria is this:
//
// 1) If a debugger is attached.
//
// 2) If the instruction before the IP is a breakpoint instruction.
//
// 3) If the IP is in the prolog or epilog of a managed function.
//
BOOL Debugger::IsThreadContextInvalid(Thread *pThread, CONTEXT *pCtx)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    BOOL invalid = FALSE;

    // Get the thread context.
    BOOL success = pCtx != NULL;
    CONTEXT ctx;
    if (!success)
    {
        ctx.ContextFlags = CONTEXT_CONTROL;
        success = pThread->GetThreadContext(&ctx);
        if (success)
        {
            pCtx = &ctx;
        }
    }

    if (success)
    {
        // Check single-step flag
        if (IsSSFlagEnabled(reinterpret_cast<DT_CONTEXT *>(pCtx) ARM_ARG(pThread) ARM64_ARG(pThread)))
        {
            // Can't hijack a thread whose SS-flag is set. This could lead to races
            // with the thread taking the SS-exception.
            // The debugger's controller filters will poll for GC to avoid starvation.
            STRESS_LOG0(LF_CORDB, LL_EVERYTHING, "HJC - Hardware trace flag applied\n");
            return TRUE;
        }
    }

    if (success)
    {
#ifdef TARGET_X86
        // Grab Eip - 1
        LPVOID address = (((BYTE*)GetIP(pCtx)) - 1);

        EX_TRY
        {
            // Use AVInRuntimeImplOkHolder.
            AVInRuntimeImplOkayHolder AVOkay;

            // Is it a breakpoint?
            if (AddressIsBreakpoint((CORDB_ADDRESS_TYPE*)address))
            {
                size_t prologSize; // Unused...
                if (g_pEEInterface->IsInPrologOrEpilog((BYTE*)GetIP(pCtx), &prologSize))
                {
                    LOG((LF_CORDB, LL_INFO1000, "D::ITCI: thread is after a BP and in prolog or epilog.\n"));
                    invalid = TRUE;
                }
            }
        }
        EX_CATCH
        {
            // If we fault trying to read the byte before EIP, then we know that its not a breakpoint.
            // Do nothing.  The default return value is FALSE.
        }
        EX_END_CATCH(SwallowAllExceptions);
#else // TARGET_X86
        // Non-x86 can detect whether the thread is suspended after an exception is hit but before
        // the kernel has dispatched the exception to user mode by trap frame reporting.
        // See Thread::IsContextSafeToRedirect().
#endif // TARGET_X86
    }
    else
    {
        // If we can't get the context, then its definetly invalid... ;)
        LOG((LF_CORDB, LL_INFO1000, "D::ITCI: couldn't get thread's context!\n"));
        invalid = TRUE;
    }

    return invalid;
}


// notification when a SQL connection begins
void Debugger::CreateConnection(CONNID dwConnectionId, __in_z WCHAR *wzName)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "D::CreateConnection %d\n.", dwConnectionId));

    if (CORDBUnrecoverableError(this))
        return;

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);

    if (CORDebuggerAttached())
    {
        DebuggerIPCEvent* ipce;

        // Send a update module syns event to the Right Side.
        ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce, DB_IPCE_CREATE_CONNECTION,
                     pThread,
                     NULL);
        ipce->CreateConnection.connectionId = dwConnectionId;
        _ASSERTE(wzName != NULL);
        ipce->CreateConnection.wzConnectionName.SetString(wzName);

        m_pRCThread->SendIPCEvent();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::CreateConnection: Skipping SendIPCEvent because RS detached."));
    }

    // Stop all Runtime threads if we actually sent an event
    if (CORDebuggerAttached())
    {
        TrapAllRuntimeThreads();
    }

    SENDIPCEVENT_END;
}

// notification when a SQL connection ends
void Debugger::DestroyConnection(CONNID dwConnectionId)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "D::DestroyConnection %d\n.", dwConnectionId));

    if (CORDBUnrecoverableError(this))
        return;

    Thread *thread = g_pEEInterface->GetThread();
    // Note that the debugger lock is reentrant, so we may or may not hold it already.
    SENDIPCEVENT_BEGIN(this, thread);

    // Send a update module syns event to the Right Side.
    DebuggerIPCEvent* ipce = m_pRCThread->GetIPCEventSendBuffer();
    InitIPCEvent(ipce, DB_IPCE_DESTROY_CONNECTION,
                 thread,
                 NULL);
    ipce->ConnectionChange.connectionId = dwConnectionId;

    // IPC event is now initialized, so we can send it over.
    SendSimpleIPCEventAndBlock();

    // This will block on the continue
    SENDIPCEVENT_END;

}

// notification for SQL connection changes
void Debugger::ChangeConnection(CONNID dwConnectionId)
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT;
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
    }
    CONTRACTL_END;

    LOG((LF_CORDB,LL_INFO1000, "D::ChangeConnection %d\n.", dwConnectionId));

    if (CORDBUnrecoverableError(this))
        return;

    Thread *pThread = g_pEEInterface->GetThread();
    SENDIPCEVENT_BEGIN(this, pThread);

    if (CORDebuggerAttached())
    {
        DebuggerIPCEvent* ipce;

        // Send a update module syns event to the Right Side.
        ipce = m_pRCThread->GetIPCEventSendBuffer();
        InitIPCEvent(ipce, DB_IPCE_CHANGE_CONNECTION,
                     pThread,
                     NULL);
        ipce->ConnectionChange.connectionId = dwConnectionId;
        m_pRCThread->SendIPCEvent();
    }
    else
    {
        LOG((LF_CORDB,LL_INFO1000, "D::ChangeConnection: Skipping SendIPCEvent because RS detached."));
    }

    // Stop all Runtime threads if we actually sent an event
    if (CORDebuggerAttached())
    {
        TrapAllRuntimeThreads();
    }

    SENDIPCEVENT_END;
}


//
// Are we the helper thread?
// Some important things about running on the helper thread:
// - there's only 1, so guaranteed to be thread-safe.
// - we'll never run managed code.
// - therefore, Never GC.
// - It listens for events from the RS.
// - It's the only thread to send a sync complete.
//
bool ThisIsHelperThreadWorker(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // This can
    Thread * pThread;
    pThread = GetThreadNULLOk();

    // First check for a real helper thread. This will do a FLS access.
    bool fIsHelperThread = !!IsDbgHelperSpecialThread();
    if (fIsHelperThread)
    {
        // If we're on the real helper thread, we never run managed code
        // and so we'd better not have an EE thread object.
        _ASSERTE((pThread == NULL) || !"The helper thread should not being running managed code.\n"
            "Are you running managed code inside the dllmain? If so, your scenario is invalid and this"
            "assert is only the tip of the iceberg.\n");
        return true;
    }

    // Even if we're not on the real helper thread, we may still be on a thread
    // pretending to be the helper. (Helper Duty, etc).
    DWORD id = GetCurrentThreadId();

    // Check for temporary helper thread.
    if (ThisIsTempHelperThread(id))
    {
        return true;
    }

    return false;
}

//
// Make call to the static method.
// This is exposed to the contracts susbsystem so that the helper thread can call
// things on MODE_COOPERATIVE.
//
bool Debugger::ThisIsHelperThread(void)
{
    WRAPPER_NO_CONTRACT;

    return ThisIsHelperThreadWorker();
}

// Check if we're the temporary helper thread. Have 2 forms of this, 1 that assumes the current
// thread (but has the overhead of an extra call to GetCurrentThreadId() if we laready know the tid.
bool ThisIsTempHelperThread()
{
    WRAPPER_NO_CONTRACT;

    DWORD id = GetCurrentThreadId();
    return ThisIsTempHelperThread(id);
}

bool ThisIsTempHelperThread(DWORD tid)
{
    WRAPPER_NO_CONTRACT;

    // If helper thread class isn't created, then there's no helper thread.
    // No one is doing helper thread duty either.
    // It's also possible we're in a shutdown case and have already deleted the
    // data for the helper thread.
    if (g_pRCThread != NULL)
    {
        // May be the temporary helper thread...
        DebuggerIPCControlBlock * pBlock = g_pRCThread->GetDCB();
        if (pBlock != NULL)
        {
            DWORD idTemp = pBlock->m_temporaryHelperThreadId;

            if (tid == idTemp)
            {
                return true;
            }
        }
    }
    return false;

}


// This function is called when host call ICLRSecurityAttributeManager::setDacl.
// It will redacl our SSE, RSEA, RSER events.
HRESULT Debugger::ReDaclEvents(PSECURITY_DESCRIPTOR securityDescriptor)
{
    WRAPPER_NO_CONTRACT;

    return m_pRCThread->ReDaclEvents(securityDescriptor);
}

/* static */
void Debugger::AcquireDebuggerDataLock(Debugger *pDebugger)
{
    WRAPPER_NO_CONTRACT;

    if (!g_fProcessDetach)
    {
        pDebugger->GetDebuggerDataLock()->Enter();
    }
}

/* static */
void Debugger::ReleaseDebuggerDataLock(Debugger *pDebugger)
{
    WRAPPER_NO_CONTRACT;

    if (!g_fProcessDetach)
    {
        pDebugger->GetDebuggerDataLock()->Leave();
    }
}


#else // DACCESS_COMPILE

// determine whether the LS holds the data lock. If it does, we will assume the locked data is in an
// inconsistent state and will throw an exception. The DAC will execute this if we are executing code
// that takes the lock.
// Arguments: input: pDebugger - the LS debugger data structure
/* static */
void Debugger::AcquireDebuggerDataLock(Debugger *pDebugger)
{
    SUPPORTS_DAC;

    if (pDebugger->GetDebuggerDataLock()->GetEnterCount() != 0)
    {
        ThrowHR(CORDBG_E_PROCESS_NOT_SYNCHRONIZED);
    }
}

void Debugger::ReleaseDebuggerDataLock(Debugger *pDebugger)
{
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
/* ------------------------------------------------------------------------ *
 * Functions for DebuggerHeap executable memory allocations
 * ------------------------------------------------------------------------ */

DebuggerHeapExecutableMemoryAllocator::~DebuggerHeapExecutableMemoryAllocator()
{
    while (m_pages != NULL)
    {
        DebuggerHeapExecutableMemoryPage *temp = m_pages->GetNextPage();

        // Free this page
        INDEBUG(BOOL ret =) VirtualFree(m_pages, 0, MEM_RELEASE);
        ASSERT(ret == TRUE);

        m_pages = temp;
    }

    ASSERT(m_pages == NULL);
}

void* DebuggerHeapExecutableMemoryAllocator::Allocate(DWORD numberOfBytes)
{
    if (numberOfBytes > DBG_MAX_EXECUTABLE_ALLOC_SIZE)
    {
        ASSERT(!"Allocating more than DBG_MAX_EXECUTABLE_ALLOC_SIZE at once is unsupported and breaks our assumptions.");
        return NULL;
    }

    if (numberOfBytes == 0)
    {
        // Should we allocate anything in this case?
        ASSERT(!"Allocate called with 0 for numberOfBytes!");
        return NULL;
    }

    CrstHolder execMemAllocCrstHolder(&m_execMemAllocMutex);

    int chunkToUse = -1;
    DebuggerHeapExecutableMemoryPage *pageToAllocateOn = NULL;
    for (DebuggerHeapExecutableMemoryPage *currPage = m_pages; currPage != NULL; currPage = currPage->GetNextPage())
    {
        if (CheckPageForAvailability(currPage, &chunkToUse))
        {
            pageToAllocateOn = currPage;
            break;
        }
    }

    if (pageToAllocateOn == NULL)
    {
        // No existing page had availability, so create a new page and use that.
        pageToAllocateOn = AddNewPage();
        if (pageToAllocateOn == NULL)
        {
            ASSERT(!"Call to AddNewPage failed!");
            return NULL;
        }

        if (!CheckPageForAvailability(pageToAllocateOn, &chunkToUse))
        {
            ASSERT(!"No availability on new page?");
            return NULL;
        }
    }

    ASSERT(chunkToUse >= 1 && (uint)chunkToUse < CHUNKS_PER_DEBUGGERHEAP);
    return GetPointerToChunkWithUsageUpdate(pageToAllocateOn, chunkToUse, ChangePageUsageAction::ALLOCATE);
}

void DebuggerHeapExecutableMemoryAllocator::Free(void* addr)
{
    ASSERT(addr != NULL);

    CrstHolder execMemAllocCrstHolder(&m_execMemAllocMutex);

    DebuggerHeapExecutableMemoryPage *pageToFreeIn = static_cast<DebuggerHeapExecutableMemoryChunk*>(addr)->data.startOfPage;
    _ASSERTE(pageToFreeIn != NULL);

    int chunkNum = static_cast<DebuggerHeapExecutableMemoryChunk*>(addr)->data.chunkNumber;

    // Sanity check: assert that the address really represents the start of a chunk.
    ASSERT(((uint64_t)addr - (uint64_t)pageToFreeIn) % EXPECTED_CHUNKSIZE == 0);

    GetPointerToChunkWithUsageUpdate(pageToFreeIn, chunkNum, ChangePageUsageAction::FREE);
}

DebuggerHeapExecutableMemoryPage* DebuggerHeapExecutableMemoryAllocator::AddNewPage()
{
    void* newPageAddr = VirtualAlloc(NULL, sizeof(DebuggerHeapExecutableMemoryPage), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

    DebuggerHeapExecutableMemoryPage *newPage = new (newPageAddr) DebuggerHeapExecutableMemoryPage;
    CrstHolder execMemAllocCrstHolder(&m_execMemAllocMutex);
    newPage->SetNextPage(m_pages);

    // Add the new page to the linked list of pages
    m_pages = newPage;
    return newPage;
}

bool DebuggerHeapExecutableMemoryAllocator::CheckPageForAvailability(DebuggerHeapExecutableMemoryPage* page, /* _Out_ */ int* chunkToUse)
{
    CrstHolder execMemAllocCrstHolder(&m_execMemAllocMutex);
    uint64_t occupancy = page->GetPageOccupancy();
    bool available = occupancy != MAX_CHUNK_MASK;

    if (!available)
    {
        if (chunkToUse)
        {
            *chunkToUse = -1;
        }

        return false;
    }

    if (chunkToUse)
    {
        // skip the first bit, as that's used by the booking chunk.
        for (int i = CHUNKS_PER_DEBUGGERHEAP - 2; i >= 0; i--)
        {
            uint64_t mask = (1ull << i);
            if ((mask & occupancy) == 0)
            {
                *chunkToUse = CHUNKS_PER_DEBUGGERHEAP - i - 1;
                break;
            }
        }
    }

    return true;
}

void* DebuggerHeapExecutableMemoryAllocator::GetPointerToChunkWithUsageUpdate(DebuggerHeapExecutableMemoryPage* page, int chunkNumber, ChangePageUsageAction action)
{
    ASSERT(action == ChangePageUsageAction::ALLOCATE || action == ChangePageUsageAction::FREE);
    uint64_t mask = 1ull << (CHUNKS_PER_DEBUGGERHEAP - chunkNumber - 1);

    CrstHolder execMemAllocCrstHolder(&m_execMemAllocMutex);
    uint64_t prevOccupancy = page->GetPageOccupancy();
    uint64_t newOccupancy = (action == ChangePageUsageAction::ALLOCATE) ? (prevOccupancy | mask) : (prevOccupancy ^ mask);
    page->SetPageOccupancy(newOccupancy);

    return page->GetPointerToChunk(chunkNumber);
}
#endif // DACCESS_COMPILE

/* ------------------------------------------------------------------------ *
 * DebuggerHeap impl
 * ------------------------------------------------------------------------ */

DebuggerHeap::DebuggerHeap()
{
#ifdef USE_INTEROPSAFE_HEAP
    m_hHeap = NULL;
#endif
    m_fExecutable = FALSE;
}

DebuggerHeap::~DebuggerHeap()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    Destroy();
}

void DebuggerHeap::Destroy()
{
#ifdef USE_INTEROPSAFE_HEAP
    if (IsInit())
    {
        ::HeapDestroy(m_hHeap);
        m_hHeap = NULL;
    }
#endif
#if !defined(HOST_WINDOWS) && !defined(DACCESS_COMPILE)
    if (m_execMemAllocator != NULL)
    {
        delete m_execMemAllocator;
    }
#endif
}

bool DebuggerHeap::IsInit()
{
    LIMITED_METHOD_CONTRACT;
#ifdef USE_INTEROPSAFE_HEAP
    return m_hHeap != NULL;
#else
    return true;
#endif
}

HRESULT DebuggerHeap::Init(BOOL fExecutable)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

    // Have knob catch if we don't want to lazy init the debugger.
    _ASSERTE(!g_DbgShouldntUseDebugger);
    m_fExecutable = fExecutable;

#ifdef USE_INTEROPSAFE_HEAP
    // If already inited, then we're done.
    // We normally don't double-init. However, we may oom between when we allocate the heap and when we do other initialization.
    // We don't worry about backout code to free the heap. Rather, we'll just leave it alive and nop if we try to allocate it again.
    if (IsInit())
    {
        return S_OK;
    }

    // Create a standard, grow-able, thread-safe heap.
    DWORD dwFlags = ((fExecutable == TRUE)? HEAP_CREATE_ENABLE_EXECUTE : 0);
    m_hHeap = ::HeapCreate(dwFlags, 0, 0);
    if (m_hHeap == NULL)
    {
        return HRESULT_FROM_GetLastError();
    }
#endif

#ifndef HOST_WINDOWS
    m_execMemAllocator = NULL;
    if (m_fExecutable)
    {
        m_execMemAllocator = new (nothrow) DebuggerHeapExecutableMemoryAllocator();
        ASSERT(m_execMemAllocator != NULL);
        if (m_execMemAllocator == NULL)
        {
            return E_OUTOFMEMORY;
        }
    }
#endif    

#endif // !DACCESS_COMPILE

    return S_OK;
}

// Only use canaries on x86 b/c they throw of alignment on Ia64.
#if defined(_DEBUG) && defined(TARGET_X86)
#define USE_INTEROPSAFE_CANARY
#endif

#ifdef USE_INTEROPSAFE_CANARY
// Small header to to prefix interop-heap blocks.
// This lets us enforce that we don't delete interopheap data from a non-interop heap.
struct InteropHeapCanary
{
    ULONGLONG m_canary;

    // Raw address - this is what the heap alloc + free routines use.
    // User address - this is what the user sees after we adjust the raw address for the canary

    // Given a raw address to an allocated block, get the canary + mark it.
    static InteropHeapCanary * GetFromRawAddr(void * pStart)
    {
        _ASSERTE(pStart != NULL);
        InteropHeapCanary * p = (InteropHeapCanary*) pStart;
        p->Mark();
        return p;
    }

    // Get the raw address from this canary.
    void * GetRawAddr()
    {
        return (void*) this;
    }

    // Get a canary from a start address.
    static InteropHeapCanary * GetFromUserAddr(void * pStart)
    {
        _ASSERTE(pStart != NULL);
        InteropHeapCanary * p = ((InteropHeapCanary*) pStart)-1;
        p->Check();
        return p;
    }
    void * GetUserAddr()
    {
        this->Check();
        return (void*) (this + 1);
    }

protected:
    void Check()
    {
        CONSISTENCY_CHECK_MSGF((m_canary == kInteropHeapCookie),
            ("Using InteropSafe delete on non-interopsafe allocated memory.\n"));
    }
    void Mark()
    {
        m_canary = kInteropHeapCookie;
    }
    static const ULONGLONG kInteropHeapCookie = 0x12345678;
};
#endif // USE_INTEROPSAFE_CANARY

void *DebuggerHeap::Alloc(DWORD size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef USE_INTEROPSAFE_CANARY
    // Make sure we allocate enough space for the canary at the start.
    size += sizeof(InteropHeapCanary);
#endif

    void *ret = NULL;

#ifndef DACCESS_COMPILE

#ifdef USE_INTEROPSAFE_HEAP
    _ASSERTE(m_hHeap != NULL);
    ret = ::HeapAlloc(m_hHeap, HEAP_ZERO_MEMORY, size);
#else // USE_INTEROPSAFE_HEAP

#ifdef HOST_WINDOWS
    HANDLE hExecutableHeap  = ClrGetProcessExecutableHeap();

    if (hExecutableHeap == NULL)
    {
        return NULL;
    }

    ret = ::HeapAlloc(hExecutableHeap, 0, size);
#else // HOST_WINDOWS
    if (m_fExecutable)
    {
        ret = m_execMemAllocator->Allocate(size);
    }
    else
    {
        ret = malloc(size);
    }
#endif // HOST_WINDOWS

#endif // USE_INTEROPSAFE_HEAP

#ifdef USE_INTEROPSAFE_CANARY
    if (ret == NULL)
    {
        return NULL;
    }
    InteropHeapCanary * pCanary = InteropHeapCanary::GetFromRawAddr(ret);
    ret = pCanary->GetUserAddr();
#endif
#endif // !DACCESS_COMPILE
    return ret;
}

// Realloc memory.
// If this fails, the original memory is still valid.
void *DebuggerHeap::Realloc(void *pMem, DWORD newSize, DWORD oldSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(pMem != NULL);
    _ASSERTE(newSize != 0);
    _ASSERTE(oldSize != 0);

#if defined(USE_INTEROPSAFE_HEAP) && !defined(USE_INTEROPSAFE_CANARY) && defined(HOST_WINDOWS)
    // No canaries in this case.
    // Call into realloc.
    void *ret;

    _ASSERTE(m_hHeap != NULL);
    ret = ::HeapReAlloc(m_hHeap, HEAP_ZERO_MEMORY, pMem, newSize);
#else
    // impl Realloc on top of alloc & free.
    void *ret;

    ret = this->Alloc(newSize);
    if (ret == NULL)
    {
        // Not supposed to free original memory in failure condition.
        return NULL;
    }

    memcpy(ret, pMem, oldSize);
    this->Free(pMem);
#endif

    return ret;
}

void DebuggerHeap::Free(void *pMem)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE

#ifdef USE_INTEROPSAFE_CANARY
    // Check for canary

    if (pMem != NULL)
    {
        InteropHeapCanary * pCanary = InteropHeapCanary::GetFromUserAddr(pMem);
        pMem = pCanary->GetRawAddr();
    }
#endif

#ifdef USE_INTEROPSAFE_HEAP
    if (pMem != NULL)
    {
        _ASSERTE(m_hHeap != NULL);
        ::HeapFree(m_hHeap, 0, pMem);
    }
#else
    if (pMem != NULL)
    {
#ifdef HOST_WINDOWS
        HANDLE hProcessExecutableHeap  = ClrGetProcessExecutableHeap();
        _ASSERTE(hProcessExecutableHeap != NULL);
        ::HeapFree(hProcessExecutableHeap, NULL, pMem);
#else // HOST_WINDOWS
        if (m_fExecutable)
        {
            m_execMemAllocator->Free(pMem);
        }
        else
        {
            free(pMem);
        }
#endif // HOST_WINDOWS
    }
#endif
#endif // !DACCESS_COMPILE
}

#ifndef DACCESS_COMPILE


// Undef this so we can call them from the EE versions.
#undef UtilMessageBoxVA

// Message box API for the left side of the debugger. This API handles calls from the
// debugger helper thread as well as from normal EE threads. It is the only one that
// should be used from inside the debugger left side.
int Debugger::MessageBox(
                  UINT uText,       // Resource Identifier for Text message
                  UINT uCaption,    // Resource Identifier for Caption
                  UINT uType,       // Style of MessageBox
                  BOOL displayForNonInteractive,    // Display even if the process is running non interactive
                  BOOL showFileNameInTitle,         // Flag to show FileName in Caption
                  ...)              // Additional Arguments
{
    CONTRACTL
    {
        MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT;
        MODE_PREEMPTIVE;
        NOTHROW;

        PRECONDITION(ThisMaybeHelperThread());
    }
    CONTRACTL_END;

    va_list marker;
    va_start(marker, showFileNameInTitle);

    // Add the MB_TASKMODAL style to indicate that the dialog should be displayed on top of the windows
    // owned by the current thread and should prevent interaction with them until dismissed.
    uType |= MB_TASKMODAL;

    int result = UtilMessageBoxVA(NULL, uText, uCaption, uType, displayForNonInteractive, showFileNameInTitle, marker);
    va_end( marker );

    return result;
}

// Redefine this to an error just in case code is added after this point in the file.
#define UtilMessageBoxVA __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")

#else // DACCESS_COMPILE
void
Debugger::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    DAC_ENUM_VTHIS();
    SUPPORTS_DAC;
    _ASSERTE(m_rgHijackFunction != NULL);

    if ( flags != CLRDATA_ENUM_MEM_TRIAGE)
    {
        if (m_pMethodInfos.IsValid())
        {
            m_pMethodInfos->EnumMemoryRegions(flags);
        }

        DacEnumMemoryRegion(dac_cast<TADDR>(m_pLazyData),
                                sizeof(DebuggerLazyInit));
    }

    // Needed for stack walking from an initial native context.  If the debugger can find the
    // on-disk image of clr.dll, then this is not necessary.
    DacEnumMemoryRegion(dac_cast<TADDR>(m_rgHijackFunction), sizeof(MemoryRange)*kMaxHijackFunctions);
}


// This code doesn't hang out in Frame/TransitionFrame/FuncEvalFrame::EnumMemoryRegions() like it would
// for other normal VM objects because we don't want to have code in VM directly referencing LS types.
// Frames.h's FuncEvalFrame simply does a forward decl of DebuggerEval and gets away with it because it
// never does anything but a cast of a TADDR.
void
Debugger::EnumMemoryRegionsIfFuncEvalFrame(CLRDataEnumMemoryFlags flags, Frame * pFrame)
{
    SUPPORTS_DAC;

    if ((pFrame != NULL) && (pFrame->GetFrameType() == Frame::TYPE_FUNC_EVAL))
    {
        FuncEvalFrame * pFEF = dac_cast<PTR_FuncEvalFrame>(pFrame);
        DebuggerEval * pDE = pFEF->GetDebuggerEval();

        if (pDE != NULL)
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(pDE), sizeof(DebuggerEval), true);

            if (pDE->m_debuggerModule != NULL)
                DacEnumMemoryRegion(dac_cast<TADDR>(pDE->m_debuggerModule), sizeof(DebuggerModule), true);
        }
    }
}

#endif // #ifdef DACCESS_COMPILE

#ifndef DACCESS_COMPILE
void Debugger::StartCanaryThread()
{
     // we need to already have the rcthread running and the pointer stored
    _ASSERTE(m_pRCThread != NULL && g_pRCThread == m_pRCThread);
    _ASSERTE(m_pRCThread->GetDCB() != NULL);
    _ASSERTE(GetCanary() != NULL);

    GetCanary()->Init();
}
#endif // DACCESS_COMPILE

#endif //DEBUGGING_SUPPORTED
