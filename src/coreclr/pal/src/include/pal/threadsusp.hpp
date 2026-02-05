// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

#include "pal/threadinfo.hpp"
#include "pal/thread.hpp"
#include "pal/init.h"
#if !HAVE_MACH_EXCEPTIONS
#include <signal.h>
#endif // !HAVE_MACH_EXCEPTIONS

#include <stdarg.h>

namespace CorUnix
{
    PAL_ERROR
    InternalResumeThread(
        CPalThread *pthrResumer,
        HANDLE hTarget,
        DWORD *pdwSuspendCount
    );

    class CThreadSuspensionInfo : public CThreadInfoInitializer
    {
        private:
            BOOL m_fPending; // TRUE if a suspension is pending on a thread (because the thread is in an unsafe region)
            BOOL m_fSelfsusp; // TRUE if thread is self suspending and while thread is self suspended
            int m_nBlockingPipe; // blocking pipe used for a process that was created suspended
#ifdef _DEBUG
            Volatile<LONG> m_lNumThreadsSuspendedByThisThread; // number of threads that this thread has suspended; used for suspension diagnostics
#endif
            pthread_mutex_t m_ptmSuspmutex; // thread's suspension mutex, which is used to synchronize suspension and resumption attempts
            BOOL m_fSuspmutexInitialized;

            /* Most of the variables above are either accessed by a thread
            holding the appropriate suspension mutex(es) or are only
            accessed by their own threads (and thus don't require
            synchronization).

            m_fPending, m_fSuspendSignalSent, and m_fResumeSignalSent
            may be set by a different thread than the owner and thus
            require synchronization.

            m_fSelfsusp is set to TRUE only by its own thread but may be later
            accessed by other threads.

            m_lNumThreadsSuspendedByThisThread is accessed by its owning
            thread and therefore does not require synchronization. */

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

            pthread_mutex_t*
            GetSuspensionMutex(
                void
                )
            {
                return &m_ptmSuspmutex;
            }

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

        public:
            virtual PAL_ERROR InitializePreCreate();

            CThreadSuspensionInfo()
                : m_fPending(FALSE)
                , m_fSelfsusp(FALSE)
                , m_nBlockingPipe(-1)
#ifdef _DEBUG
                , m_lNumThreadsSuspendedByThisThread(0)
#endif // _DEBUG
                , m_fSuspmutexInitialized(FALSE)
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
            InternalResumeThreadFromData(
                CPalThread *pthrResumer,
                CPalThread *pthrTarget,
                DWORD *pdwSuspendCount
            );

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

#endif // _PAL_THREADSUSP_HPP

