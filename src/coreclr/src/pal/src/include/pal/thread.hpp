// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/thread.hpp

Abstract:
    Header file for thread structures



--*/

#ifndef _PAL_THREAD_HPP_
#define _PAL_THREAD_HPP_

#include "corunix.hpp"
#include "shm.hpp"
#include "cs.hpp"

#include <pthread.h>    
#include <sys/syscall.h>
#if HAVE_MACH_EXCEPTIONS
#include <mach/mach.h>
#endif // HAVE_MACH_EXCEPTIONS

#include "threadsusp.hpp"
#include "tls.hpp"
#include "synchobjects.hpp"
#include <errno.h>

namespace CorUnix
{
    enum PalThreadType
    {
        UserCreatedThread,
        PalWorkerThread,
        SignalHandlerThread
    };
    
    PAL_ERROR
    InternalCreateThread(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpThreadAttributes,
        DWORD dwStackSize,
        LPTHREAD_START_ROUTINE lpStartAddress,
        LPVOID lpParameter,
        DWORD dwCreationFlags,
        PalThreadType eThreadType,
        LPDWORD lpThreadId,
        HANDLE *phThread
        );

    PAL_ERROR
    InternalGetThreadPriority(
        CPalThread *pThread,
        HANDLE hTargetThread,
        int *piNewPriority
        );

    PAL_ERROR
    InternalSetThreadPriority(
        CPalThread *pThread,
        HANDLE hTargetThread,
        int iNewPriority
        );

    PAL_ERROR
    InternalGetThreadDataFromHandle(
        CPalThread *pThread,
        HANDLE hThread,
        DWORD dwRightsRequired,
        CPalThread **ppTargetThread,
        IPalObject **ppobjThread
        );

    VOID
    InternalEndCurrentThread(
        CPalThread *pThread
        );

    PAL_ERROR
    InternalCreateDummyThread(
        CPalThread *pThread,
        LPSECURITY_ATTRIBUTES lpThreadAttributes,
        CPalThread **ppDummyThread,
        HANDLE *phThread
        );

    PAL_ERROR
    InitializeGlobalThreadData(
        void
        );

    PAL_ERROR
    CreateThreadData(
        CPalThread **ppThread
        );

    PAL_ERROR
    CreateThreadObject(
        CPalThread *pThread,
        CPalThread *pNewThread,
        HANDLE *phThread
        );

    PAL_ERROR
    InitializeEndingThreadsData(
        void
        );

    BOOL
    GetThreadTimesInternal(
        IN HANDLE hThread,
        OUT LPFILETIME lpKernelTime,
        OUT LPFILETIME lpUserTime);
        
#ifdef FEATURE_PAL_SXS
#if HAVE_MACH_EXCEPTIONS
    // Structure used to record all Mach exception handlers registered on a given thread at a specific point
    // in time.
    struct CThreadMachExceptionHandlerNode
    {
        // Maximum number of exception ports we hook.  Must be the count
        // of all bits set in the exception masks defined in machexception.h.
        static const int s_nPortsMax = 6;

        // Saved exception ports, exactly as returned by
        // thread_swap_exception_ports.
        int m_nPorts;
        exception_mask_t m_masks[s_nPortsMax];
        exception_handler_t m_handlers[s_nPortsMax];
        exception_behavior_t m_behaviors[s_nPortsMax];
        thread_state_flavor_t m_flavors[s_nPortsMax];
        
        CThreadMachExceptionHandlerNode() : m_nPorts(-1) {}
    };

    // Structure used to return data about a single handler to a caller.
    struct MachExceptionHandler
    {
        exception_mask_t m_mask;
        exception_handler_t m_handler;
        exception_behavior_t m_behavior;
        thread_state_flavor_t m_flavor;
    };

    // Class abstracting previousy registered Mach exception handlers for a thread.
    class CThreadMachExceptionHandlers
    {
    public:
        // Returns a pointer to the handler node that should be initialized next. The first time this is
        // called for a thread the bottom node will be returned. Thereafter the top node will be returned.
        // Also returns the Mach exception port that should be registered.
        CThreadMachExceptionHandlerNode *GetNodeForInitialization();

        // Returns a pointer to the handler node for cleanup. This will always be the bottom node. This isn't
        // really the right algorithm (because there isn't one). There are lots of reasonable scenarios where
        // we will break chaining by removing our handler from a thread (by registering two handler ports we
        // can support a lot more chaining scenarios but we can't pull the same sort of trick when
        // unregistering, in particular we have two sets of chain back handlers and no way to reach into other
        // components and alter what their chain-back information is).
        CThreadMachExceptionHandlerNode *GetNodeForCleanup() { return &m_node; }

        // Get handler details for a given type of exception. If successful the structure pointed at by
        // pHandler is filled in and true is returned. Otherwise false is returned.
        bool GetHandler(exception_type_t eException, MachExceptionHandler *pHandler);

    private:
        // Look for a handler for the given exception within the given handler node. Return its index if
        // successful or -1 otherwise.
        int GetIndexOfHandler(exception_mask_t bmExceptionMask, CThreadMachExceptionHandlerNode *pNode);

        CThreadMachExceptionHandlerNode m_node;
    };
#endif // HAVE_MACH_EXCEPTIONS
#endif // FEATURE_PAL_SXS
    
    class CThreadSEHInfo : public CThreadInfoInitializer
    {
    public:
#if !HAVE_MACH_EXCEPTIONS
        BOOL safe_state;
        int signal_code;
#endif // !HAVE_MACH_EXCEPTIONSG

        CThreadSEHInfo()
        {
        };
    };

    /* In the windows CRT there is a constant defined for the max width
    of a _ecvt conversion. That constant is 348. 348 for the value, plus
    the exponent value, decimal, and sign if required. */
#define ECVT_MAX_COUNT_SIZE 348
#define ECVT_MAX_BUFFER_SIZE 357

    /*STR_TIME_SIZE is defined as 26 the size of the
      return val by ctime_r*/
#define STR_TIME_SIZE 26

    class CThreadCRTInfo : public CThreadInfoInitializer
    {
    public:
        CHAR *       strtokContext; // Context for strtok function
        WCHAR *      wcstokContext; // Context for wcstok function
        struct PAL_tm localtimeBuffer; // Buffer for localtime function
        CHAR         ctimeBuffer[ STR_TIME_SIZE ]; // Buffer for ctime function
        CHAR         ECVTBuffer[ ECVT_MAX_BUFFER_SIZE ]; // Buffer for _ecvt function.

        CThreadCRTInfo() :
            strtokContext(NULL),
            wcstokContext(NULL)
        {
            ZeroMemory(&localtimeBuffer, sizeof(localtimeBuffer));
            ZeroMemory(ctimeBuffer, sizeof(ctimeBuffer));
            ZeroMemory(ECVTBuffer, sizeof(ECVTBuffer));
        };
    };

    class CPalThread
    {
        friend
            PAL_ERROR
            CorUnix::InternalCreateThread(
                CPalThread *,
                LPSECURITY_ATTRIBUTES,
                DWORD,
                LPTHREAD_START_ROUTINE,
                LPVOID,
                DWORD,
                PalThreadType,
                LPDWORD,
                HANDLE*
                );

        friend
            PAL_ERROR
            InternalCreateDummyThread(
                CPalThread *pThread,
                LPSECURITY_ATTRIBUTES lpThreadAttributes,
                CPalThread **ppDummyThread,
                HANDLE *phThread
                );

        friend
            PAL_ERROR
            InternalSetThreadPriority(
                CPalThread *,
                HANDLE,
                int
                );

        friend
            PAL_ERROR
            InitializeGlobalThreadData(
                void
                );

        friend
            PAL_ERROR
            CreateThreadData(
                CPalThread **ppThread
                );

        friend
            PAL_ERROR
            CreateThreadObject(
                CPalThread *pThread,
                CPalThread *pNewThread,
                HANDLE *phThread
                );

        friend CatchHardwareExceptionHolder;
        
    private:

        CPalThread *m_pNext;
        DWORD m_dwExitCode;
        BOOL m_fExitCodeSet;
        CRITICAL_SECTION m_csLock;
        bool m_fLockInitialized;
        bool m_fIsDummy;

        //
        // Minimal reference count, used primarily for cleanup purposes. A
        // new thread object has an initial refcount of 1. This initial
        // reference is removed by CorUnix::InternalEndCurrentThread.
        //
        // The only other spot the refcount is touched is from within
        // CPalObjectBase::ReleaseReference -- incremented before the
        // destructors for an ojbect are called, and decremented afterwords.
        // This permits the freeing of the thread structure to happen after
        // the freeing of the enclosing thread object has completed.
        //

        LONG m_lRefCount;

        //
        // The IPalObject for this thread. The thread will release its reference
        // to this object when it exits.
        //

        IPalObject *m_pThreadObject;

        //
        // Thread ID info
        //

        SIZE_T m_threadId;
        DWORD m_dwLwpId;
        pthread_t m_pthreadSelf;        

#if HAVE_MACH_THREADS
        mach_port_t m_machPortSelf;
#endif 

        // > 0 when there is an exception holder which causes h/w
        // exceptions to be sent down the C++ exception chain.
        int m_hardwareExceptionHolderCount;

        //
        // Start info
        //

        LPTHREAD_START_ROUTINE m_lpStartAddress;
        LPVOID m_lpStartParameter;
        BOOL m_bCreateSuspended;

        int m_iThreadPriority;
        PalThreadType m_eThreadType;

        //
        // pthread mutex / condition variable for gating thread startup.
        // InternalCreateThread waits on the condition variable to determine
        // when the new thread has reached passed all failure points in
        // the entry routine
        //

        pthread_mutex_t m_startMutex;
        pthread_cond_t m_startCond;
        bool m_fStartItemsInitialized;
        bool m_fStartStatus;
        bool m_fStartStatusSet;

        // Base address of the stack of this thread
        void* m_stackBase;
        // Limit address of the stack of this thread
        void* m_stackLimit;

        // The default stack size of a newly created thread (currently 256KB)
        // when the dwStackSize paramter of PAL_CreateThread()
        // is zero. This value can be set by setting the
        // environment variable PAL_THREAD_DEFAULT_STACK_SIZE
        // (the value should be in bytes and in hex).
        static DWORD s_dwDefaultThreadStackSize; 

        //
        // The thread entry routine (called from InternalCreateThread)
        //

        static void* ThreadEntry(void * pvParam);
        
#ifdef FEATURE_PAL_SXS
        //
        // Data for PAL side-by-side support
        //

    private:
        // This is set whenever this thread is currently executing within
        // a region of code that depends on this instance of the PAL
        // in the process.
        bool m_fInPal;

#if HAVE_MACH_EXCEPTIONS
        // Record of Mach exception handlers that were already registered when we register our own CoreCLR
        // specific handlers.
        CThreadMachExceptionHandlers m_sMachExceptionHandlers;
#endif // HAVE_MACH_EXCEPTIONS
#endif // FEATURE_PAL_SXS

    public:

        //
        // Embedded information for areas owned by other subsystems
        //

        CThreadSynchronizationInfo synchronizationInfo;
        CThreadSuspensionInfo suspensionInfo;
        CThreadSEHInfo sehInfo;
        CThreadTLSInfo tlsInfo;
        CThreadApcInfo apcInfo;
        CThreadCRTInfo crtInfo;

        CPalThread()
            :
            m_pNext(NULL),
            m_dwExitCode(STILL_ACTIVE),
            m_fExitCodeSet(FALSE),
            m_fLockInitialized(FALSE),
            m_fIsDummy(FALSE),
            m_lRefCount(1),
            m_pThreadObject(NULL),
            m_threadId(0),
            m_dwLwpId(0),
            m_pthreadSelf(0),
#if HAVE_MACH_THREADS
            m_machPortSelf(0),
#endif            
            m_hardwareExceptionHolderCount(0),
            m_lpStartAddress(NULL),
            m_lpStartParameter(NULL),
            m_bCreateSuspended(FALSE),
            m_iThreadPriority(THREAD_PRIORITY_NORMAL),
            m_eThreadType(UserCreatedThread),
            m_fStartItemsInitialized(FALSE),
            m_fStartStatus(FALSE),
            m_fStartStatusSet(FALSE),
            m_stackBase(NULL),
            m_stackLimit(NULL)
#ifdef FEATURE_PAL_SXS
          , m_fInPal(TRUE)
#endif // FEATURE_PAL_SXS
        {
        };

        virtual ~CPalThread();

        PAL_ERROR
        RunPreCreateInitializers(
            void
            );

        //
        // m_threadId and m_dwLwpId must be set before calling
        // RunPostCreateInitializers
        //

        PAL_ERROR
        RunPostCreateInitializers(
            void
            );

        //
        // SetStartStatus is called by THREADEntry or InternalSuspendNewThread
        // to inform InternalCreateThread of the results of the thread's
        // initialization. InternalCreateThread calls WaitForStartStatus to
        // obtain this information (and will not return to its caller until
        // the info is available).
        //

        void
        SetStartStatus(
            bool fStartSucceeded
            );

        bool
        WaitForStartStatus(
            void
            );

        void
        Lock(
            CPalThread *pThread
            )
        {
            InternalEnterCriticalSection(pThread, &m_csLock);
        };

        void
        Unlock(
            CPalThread *pThread
            )
        {
            InternalLeaveCriticalSection(pThread, &m_csLock);
        };

        //
        // The following three methods provide access to the 
        // native lock used to protect thread native wait data.
        //

        void
        AcquireNativeWaitLock(
            void
            )
        {
            synchronizationInfo.AcquireNativeWaitLock();
        }

        void
        ReleaseNativeWaitLock(
            void
            )
        {
            synchronizationInfo.ReleaseNativeWaitLock();
        }

        bool
        TryAcquireNativeWaitLock(
            void
            )
        {
            return synchronizationInfo.TryAcquireNativeWaitLock();
        }

        static void
        SetLastError(
            DWORD dwLastError
            )
        {
            // Reuse errno to store last error
            errno = dwLastError;
        };

        static DWORD
        GetLastError(
            void
            )
        {
            // Reuse errno to store last error
            return errno;
        };

        void
        SetExitCode(
            DWORD dwExitCode
            )
        {
            m_dwExitCode = dwExitCode;
            m_fExitCodeSet = TRUE;
        };

        BOOL
        GetExitCode(
            DWORD *pdwExitCode
            )
        {
            *pdwExitCode = m_dwExitCode;
            return m_fExitCodeSet;
        };

        SIZE_T
        GetThreadId(
            void
            )
        {
            return m_threadId;
        };
        
        DWORD
        GetLwpId(
            void
            )
        {
            return m_dwLwpId;
        };

        pthread_t
        GetPThreadSelf(
            void
            )
        {
            return m_pthreadSelf;
        };

#if HAVE_MACH_THREADS
        mach_port_t
        GetMachPortSelf(
            void
            )
        {
            return m_machPortSelf;
        };
#endif

        bool 
        IsHardwareExceptionsEnabled()
        {
            return m_hardwareExceptionHolderCount > 0;
        }

        LPTHREAD_START_ROUTINE
        GetStartAddress(
            void
            )
        {
            return m_lpStartAddress;
        };

        LPVOID
        GetStartParameter(
            void
            )
        {
            return m_lpStartParameter;
        };

        BOOL
        GetCreateSuspended(
            void
            )
        {
            return m_bCreateSuspended;
        };

        PalThreadType
        GetThreadType(
            void
            )
        {
            return m_eThreadType;
        };

        int
        GetThreadPriority(
            void
            )
        {
            return m_iThreadPriority;
        };

        IPalObject *
        GetThreadObject(
            void
            )
        {
            return m_pThreadObject;
        }

        BOOL
        IsDummy(
            void
            )
        {
            return m_fIsDummy;
        };

        CPalThread*
        GetNext(
            void
            )
        {
            return m_pNext;
        };

        void
        SetNext(
            CPalThread *pNext
            )
        {
            m_pNext = pNext;
        };

        void
        AddThreadReference(
            void
            );

        void
        ReleaseThreadReference(
            void
            );

        // Get base address of the current thread's stack
        static
        void *
        GetStackBase(
            void
            );

        // Get cached base address of this thread's stack
        // Can be called only for the current thread.
        void *
        GetCachedStackBase(
            void
            );

        // Get limit address of the current thread's stack
        static
        void *
        GetStackLimit(
            void
            );

        // Get cached limit address of this thread's stack
        // Can be called only for the current thread.
        void *
        GetCachedStackLimit(
            void
            );
        
#ifdef FEATURE_PAL_SXS
        //
        // Functions for PAL side-by-side support
        //

        // This function needs to be called on a thread when it enters
        // a region of code that depends on this instance of the PAL
        // in the process.
        PAL_ERROR Enter(PAL_Boundary boundary);

        // This function needs to be called on a thread when it leaves
        // a region of code that depends on this instance of the PAL
        // in the process.
        PAL_ERROR Leave(PAL_Boundary boundary);

        // Returns TRUE whenever this thread is executing in a region
        // of code that depends on this instance of the PAL in the process.
        BOOL IsInPal()
        {
            return m_fInPal;
        };

#if HAVE_MACH_EXCEPTIONS
        // Hook Mach exceptions, i.e., call thread_swap_exception_ports
        // to replace the thread's current exception ports with our own.
        // The previously active exception ports are saved.  Called when
        // this thread enters a region of code that depends on this PAL.
        // Should only fail on internal errors.
        PAL_ERROR EnableMachExceptions();

        // Unhook Mach exceptions, i.e., call thread_set_exception_ports
        // to restore the thread's exception ports with those we saved
        // in EnableMachExceptions.  Called when this thread leaves a
        // region of code that depends on this PAL.  Should only fail
        // on internal errors.
        PAL_ERROR DisableMachExceptions();

        // The exception handling thread needs to be able to get at the list of handlers that installing our
        // own handler on a thread has displaced (in case we need to forward an exception that we don't want
        // to handle).
        CThreadMachExceptionHandlers *GetSavedMachHandlers()
        {
            return &m_sMachExceptionHandlers;
        }
#endif // HAVE_MACH_EXCEPTIONS
#endif // FEATURE_PAL_SXS
    };

#if defined(FEATURE_PAL_SXS)
    extern "C" CPalThread *CreateCurrentThreadData();
#endif // FEATURE_PAL_SXS

    inline CPalThread *GetCurrentPalThread()
    {
        return reinterpret_cast<CPalThread*>(pthread_getspecific(thObjKey));
    }

    inline CPalThread *InternalGetCurrentThread()
    {
        CPalThread *pThread = GetCurrentPalThread();
#if defined(FEATURE_PAL_SXS)
        if (pThread == nullptr)
            pThread = CreateCurrentThreadData();
#endif // FEATURE_PAL_SXS
        return pThread;
    }

/***

    $$TODO: These are needed only to support cross-process thread duplication
    
    class CThreadImmutableData
    {
    public:
        DWORD dwProcessId;
    };

    class CThreadSharedData
    {
    public:
        DWORD dwThreadId;
        DWORD dwExitCode;
    };
***/

    //
    // The process local information for a thread is just a pointer
    // to the underlying CPalThread object.
    //

    class CThreadProcessLocalData
    {
    public:
        CPalThread *pThread;
    };

    extern CObjectType otThread;
}

BOOL
TLSInitialize(
    void
    );

VOID
TLSCleanup(
    void
    );

VOID 
WaitForEndingThreads(
    void
    );

extern int free_threads_spinlock;

extern PAL_ActivationFunction g_activationFunction;
extern PAL_SafeActivationCheckFunction g_safeActivationCheckFunction;

/*++
Macro:
  THREADSilentGetCurrentThreadId

Abstract:
  Same as GetCurrentThreadId, but it doesn't output any traces.
  It is useful for tracing functions to display the thread ID
  without generating any new traces.

  TODO: how does the perf of pthread_self compare to
  InternalGetCurrentThread when we find the thread in the
  cache?

  If the perf of pthread_self is comparable to that of the stack
  bounds based lookaside system, why aren't we using it in the
  cache?

  In order to match the thread ids that debuggers use at least for
  linux we need to use gettid(). 

--*/
#if defined(__LINUX__)
#define THREADSilentGetCurrentThreadId() (SIZE_T)syscall(SYS_gettid)
#elif defined(__APPLE__)
inline SIZE_T THREADSilentGetCurrentThreadId() {
    uint64_t tid;
    pthread_threadid_np(pthread_self(), &tid);
    return (SIZE_T)tid;
}
#else
#define THREADSilentGetCurrentThreadId() (SIZE_T)pthread_self()
#endif

#endif // _PAL_THREAD_HPP_
