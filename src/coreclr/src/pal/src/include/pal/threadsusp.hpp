//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    include/pal/threadsusp.hpp

Abstract:
    Declarations for thread suspension



--*/

#ifndef _PAL_THREADSUSP_HPP
#define _PAL_THREADSUSP_HPP

// Need this ifdef since this header is included by .c files so they can use the diagnostic function.
#ifdef __cplusplus 

// Note: do not include malloc.hpp from this header. The template InternalDelete
// needs to know the layout of class CPalThread, which includes a member of type 
// CThreadSuspensionInfo, which is defined later in this header, and it is not  
// yet known at this point.
// If any future change should bring this issue back, the circular dependency can 
// be further broken by making the InternalDelete's CPalThread argument a 
// templatized argument, so that type checking on it takes place only at 
// instantiation time.
#include "pal/threadinfo.hpp"
#include "pal/thread.hpp"
#include "pal/printfcpp.hpp"
#include "pal/mutex.hpp"
#include "pal/init.h"

#if !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION
#include <signal.h>
#endif // !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION
#include <semaphore.h>
#include <sched.h>

// We have a variety of options for synchronizing thread suspensions and resumptions between the requestor and
// target threads. Analyze the various capabilities given to us by configure and define one of three macros
// here for simplicity:
//  USE_POSIX_SEMAPHORES
//  USE_SYSV_SEMAPHORES
//  USE_PTHREAD_CONDVARS
#if HAS_POSIX_SEMAPHORES

// Favor posix semaphores.
#define USE_POSIX_SEMAPHORES 1

#if HAVE_SYS_SEMAPHORE_H
#include <sys/semaphore.h>
#endif // HAVE_SYS_SEMAPHORE_H

#elif HAS_PTHREAD_MUTEXES && HAVE_MACH_EXCEPTIONS

// Can only use the pthread solution if we're not using signals since pthread mutexes are not signal safe.
#define USE_PTHREAD_CONDVARS 1

#include <pthread.h>

#elif HAS_SYSV_SEMAPHORES

// SYSV semaphores are our last choice since they're shared across processes so it's possible to leak them
// on abnormal process termination.
#define USE_SYSV_SEMAPHORES 1

#include <sys/sem.h>
#include <sys/types.h>

#else
#error "Don't know how to synchronize thread suspends and resumes on this platform"
#endif // HAS_POSIX_SEMAPHORES

#include <stdarg.h>
	
namespace CorUnix
{
#ifdef _DEBUG
#define MAX_TRACKED_CRITSECS 8
#endif

    PAL_ERROR
    InternalSuspendThread(
        CPalThread *pthrSuspender,
        HANDLE hTarget,
        DWORD *pdwSuspendCount
    );

    PAL_ERROR
    InternalResumeThread(
        CPalThread *pthrResumer,
        HANDLE hTarget,
        DWORD *pdwSuspendCount
    );

    class CThreadSuspensionInfo : public CThreadInfoInitializer
    {    
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
        /* suspend_handler and resume_handler are friends of CThreadSuspensionInfo,
        which allows them to call private functions: HandleSuspendSignal and 
        HandleResumeSignal. */
        friend void suspend_handler(int code, siginfo_t *siginfo, void *context);
        friend void resume_handler(int code, siginfo_t *siginfo, void *context);
#endif    

        public:
            BOOL
            IsSuspensionStateSafe()
            {
                return (m_dwUnsafeRegionCount.Load() == 0);
            };

        private:
            Volatile<DWORD> m_dwSuspCount; // number of times that SuspendThread has been called on it.
            Volatile<DWORD> m_dwUnsafeRegionCount; // number of unsafe regions a thread is in
            BOOL m_fPending; // TRUE if a suspension is pending on a thread (because the thread is in an unsafe region)
            BOOL m_fSelfsusp; // TRUE if thread is self suspending and while thread is self suspended
            BOOL m_fSuspendedForShutdown; // TRUE once the thread is suspended during PAL cleanup
            int m_nBlockingPipe; // blocking pipe used for a process that was created suspended
#if USE_SIGNALS_FOR_THREAD_SUSPENSION        
            BOOL m_fSuspendSignalSent; // TRUE when a thread should expect to receive a SIGUSR1 for suspension
            BOOL m_fResumeSignalSent; // TRUE when a thread should expect to receive a SIGUSR2 for resumption
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
#ifdef _DEBUG
            Volatile<LONG> m_lNumThreadsSuspendedByThisThread; // number of threads that this thread has suspended; used for suspension diagnostics
#endif
            BOOL m_fPerformingSuspension; // TRUE when performing suspension operations; FALSE otherwise
#if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
            int m_nSpinlock; // thread's suspension spinlock, which is used to synchronize suspension and resumption attempts
#else // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
            pthread_mutex_t m_ptmSuspmutex; // thread's suspension mutex, which is used to synchronize suspension and resumption attempts
            BOOL m_fSuspmutexInitialized;
#endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
            sigset_t m_smOrigmask; // stores a thread's signal mask immediately before a suspension retry; it is restored after the suspension retry is completed
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
#if USE_POSIX_SEMAPHORES
            sem_t m_semSusp; // suspension semaphore
            sem_t m_semResume; // resumption semaphore
            BOOL m_fSemaphoresInitialized;
#elif USE_SYSV_SEMAPHORES
            // necessary id's and sembuf structures for SysV semaphores
            int m_nSemsuspid; // id for the suspend semaphore
            int m_nSemrespid; // id for the resume semaphore
            struct sembuf m_sbSemwait; // struct representing a wait operation
            struct sembuf m_sbSempost; // struct representing a post operation
#elif USE_PTHREAD_CONDVARS
            pthread_cond_t m_condSusp; // suspension condition variable
            pthread_mutex_t m_mutexSusp; // mutex associated with the condition above
            BOOL m_fSuspended; // set to true once the suspend has been acknowledged

            pthread_cond_t m_condResume; // resumption condition variable
            pthread_mutex_t m_mutexResume; // mutex associated with the condition above
            BOOL m_fResumed; // set to true once the resume has been acknowledged

            BOOL m_fSemaphoresInitialized;
#endif // USE_POSIX_SEMAPHORES

            /* Most of the variables above are either accessed by a thread 
            holding the appropriate suspension mutex(es) or are only
            accessed by their own threads (and thus don't require
            synchronization). 
            
            m_dwSuspCount, m_fPending, m_fSuspendedForShutdown,
            m_fSuspendSignalSent, and m_fResumeSignalSent 
            may be set by a different thread than the owner and thus
            require synchronization.

            m_dwUnsafeRegionCount can change even when a suspending
            thread has acquired suspension mutexes. However, it is only
            changed by its owner (no other thread will change a thread's
            unsafe region count).

            m_fSelfsusp is set to TRUE only by its own thread but may be later 
            accessed by other threads. 

            m_lNumThreadsSuspendedByThisThread and m_fPerformingSuspension are
            only accessed by their owning thread and therefore do not
            require synchronization. */

            DWORD
            GetUnsafeRegionCount(
                void
                )
            {
                return m_dwUnsafeRegionCount;
            };
           
            VOID
            IncrUnsafeRegionCount(
                void
                )
            {
                m_dwUnsafeRegionCount++;
            };  

            VOID
            DecrUnsafeRegionCount(
                void
                )
            {
                m_dwUnsafeRegionCount--;
            };

            VOID
            IncrSuspCount(
                void
                )
            {
                ++m_dwSuspCount;
            };  

            VOID
            DecrSuspCount(
                void
                )
            {
                --m_dwSuspCount;
            };  
            
#ifdef _DEBUG
            VOID
            IncrNumThreadsSuspendedByThisThread(
                )
            {
                InterlockedIncrement(&m_lNumThreadsSuspendedByThisThread);
            };

            VOID
            DecrNumThreadsSuspendedByThisThread(
                )
            {
                InterlockedDecrement(&m_lNumThreadsSuspendedByThisThread);
            };
#endif

            VOID
            AcquireSuspensionLocks(
                CPalThread *pthrSuspender,
                CPalThread *pthrTarget
            );

            VOID
            ReleaseSuspensionLocks(
                CPalThread *pthrSuspender,
                CPalThread *pthrTarget
            );  

#if USE_POSIX_SEMAPHORES
            sem_t* 
            GetSuspendSemaphore(
                void
                )
            {
                return &m_semSusp;
            };

            sem_t* 
            GetResumeSemaphore(
                void
                )
            {
                return &m_semResume;
            };
#elif USE_SYSV_SEMAPHORES
            int
            GetSuspendSemaphoreId(
                void
                )
            {
                return m_nSemsuspid;
            };

            sembuf*
            GetSemaphorePostBuffer(
                void
                )
            {
                return &m_sbSempost;
            };
#endif // USE_POSIX_SEMAPHORES

#if DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
            LONG* 
            GetSuspensionSpinlock(
                void
                )
            {
                return &m_nSpinlock;
            }
#else // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
            pthread_mutex_t*
            GetSuspensionMutex(
                void
                )
            {
                return &m_ptmSuspmutex;
            }
#endif // DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
            
#if USE_SIGNALS_FOR_THREAD_SUSPENSION         	    
            void
            SetSuspendSignalSent(
                BOOL fSuspendSignalSent
                )
            {
                m_fSuspendSignalSent = fSuspendSignalSent;
            };   

            void
            SetResumeSignalSent(
                BOOL fResumeSignalSent
                )
            {
                m_fResumeSignalSent = fResumeSignalSent;
            };   
            
            BOOL
            GetSuspendSignalSent(
                void
                )
            {
                return m_fSuspendSignalSent;
            };  

            BOOL
            GetResumeSignalSent(
                void
                )
            {
                return m_fResumeSignalSent;
            };
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

            void
            SetSuspPending(
                BOOL fPending
                )
            {
                m_fPending = fPending;
            };	

            BOOL
            GetSuspPending(
                void
                )
            {
                return m_fPending;
            };     

            void
            SetSelfSusp(
                BOOL fSelfsusp
                )
            {
                m_fSelfsusp = fSelfsusp;
            }; 
            
            BOOL
            GetSelfSusp(
                void
                )
            {
                return m_fSelfsusp;
            }; 
                  
            DWORD
            GetSuspCount(
                void
                )
            {
                return m_dwSuspCount;
            };

            void
            PostOnSuspendSemaphore();

            void
            WaitOnSuspendSemaphore();     

            void
            PostOnResumeSemaphore();

            void
            WaitOnResumeSemaphore();             

            static 
            BOOL 
            TryAcquireSuspensionLock(
                CPalThread* pthrTarget
            );    

            int GetBlockingPipe(
                void
                )
            {
                return m_nBlockingPipe;
            };

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
            bool
            HandleSuspendSignal(
                CPalThread *pthrTarget
            );

            bool 
            HandleResumeSignal(
            );
#else // USE_SIGNALS_FOR_THREAD_SUSPENSION
            static
            BOOL 
            THREADHandleSuspendNative(
                CPalThread *pthrTarget
            );

            static
            BOOL 
            THREADHandleResumeNative(
                CPalThread *pthrTarget
            );
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

        public:
            virtual PAL_ERROR InitializePreCreate();

            CThreadSuspensionInfo()
                :
                m_dwSuspCount(0),
                m_dwUnsafeRegionCount(0),
                m_fPending(FALSE),
                m_fSelfsusp(FALSE),
                m_fSuspendedForShutdown(FALSE),
                m_nBlockingPipe(-1),
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
                m_fSuspendSignalSent(FALSE),
                m_fResumeSignalSent(FALSE),
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION
#ifdef _DEBUG
                m_lNumThreadsSuspendedByThisThread(0),
#endif // _DEBUG
                m_fPerformingSuspension(FALSE)
#if !DEADLOCK_WHEN_THREAD_IS_SUSPENDED_WHILE_BLOCKED_ON_MUTEX
                ,m_fSuspmutexInitialized(FALSE)
#endif
#if USE_POSIX_SEMAPHORES || USE_PTHREAD_CONDVARS
                ,m_fSemaphoresInitialized(FALSE)
#endif                
            {
                InitializeSuspensionLock();
            }; 

            virtual ~CThreadSuspensionInfo();

#ifdef _DEBUG
            LONG
            GetNumThreadsSuspendedByThisThread(
                void
                )
            {
                return m_lNumThreadsSuspendedByThisThread;
            };
#endif // _DEBUG

#if USE_SYSV_SEMAPHORES
            void
            DestroySemaphoreIds(
                void
            );
#endif

            void
            SetPerformingSuspension(
                BOOL fPerformingSuspension
                )
            {
                m_fPerformingSuspension = fPerformingSuspension;
            };

            BOOL
            IsPerformingSuspension()
            {
                return m_fPerformingSuspension;
            };

            void
            SetSuspendedForShutdown(
                BOOL fSuspendedForShutdown
                )
            {
                m_fSuspendedForShutdown = fSuspendedForShutdown;
            };
            
            BOOL
            GetSuspendedForShutdown(
                void
                )
            {
                return m_fSuspendedForShutdown;
            };

            void
            AcquireSuspensionLock(
                CPalThread *pthrCurrent
            );

            void
            ReleaseSuspensionLock(
                CPalThread *pthrCurrent
            );

            PAL_ERROR
            InternalSuspendNewThreadFromData(
                CPalThread *pThread
            );  

            PAL_ERROR
            InternalSuspendThreadFromData(
                CPalThread *pthrSuspender,
                CPalThread *pthrTarget,
                DWORD *pdwSuspendCount
            );

            PAL_ERROR
            InternalResumeThreadFromData(
                CPalThread *pthrResumer,
                CPalThread *pthrTarget,
                DWORD *pdwSuspendCount
            );

#if !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION
            static 
            VOID InitializeSignalSets();
#endif // !HAVE_MACH_EXCEPTIONS || USE_SIGNALS_FOR_THREAD_SUSPENSION

            VOID InitializeSuspensionLock();

            void SetBlockingPipe(
                int nBlockingPipe
                )
            {
                m_nBlockingPipe = nBlockingPipe;
            };
    };
} //end CorUnix

extern const BYTE WAKEUPCODE; // use for pipe reads during self suspend.
#endif // __cplusplus

#ifdef USE_GLOBAL_LOCK_FOR_SUSPENSION
extern LONG g_ssSuspensionLock;
#endif

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

        #ifdef _DEBUG
            void THREADMarkDiagnostic(const char *funcName);
        #endif // _DEBUG
        
        BOOL PALCIsSuspensionStateSafe(void);
        
#ifdef __cplusplus
    }
#endif // __cpluplus

#ifndef _DEBUG //  for non debug and checked builds, resolve macros to nothing.
    #define THREADMarkDiagnostic(funcName)
#endif // _DEBUG

#endif // _PAL_THREADSUSP_HPP

