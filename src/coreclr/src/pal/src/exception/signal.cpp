//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    exception/signal.cpp

Abstract:

    Signal handler implementation (map signals to exceptions)



--*/

#include "pal/corunix.hpp"
#include "pal/handleapi.hpp"
#include "pal/thread.hpp"
#include "pal/threadinfo.hpp"
#include "pal/threadsusp.hpp"
#include "pal/seh.hpp"

#include "pal/palinternal.h"
#if !HAVE_MACH_EXCEPTIONS
#include "pal/dbgmsg.h"
#include "pal/init.h"
#include "pal/process.h"
#include "pal/debug.h"

#include <signal.h>
#include <errno.h>
#include <string.h>
#include <sys/ucontext.h>
#include <sys/utsname.h>
#include <unistd.h>

#include "pal/context.h"

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

/* local type definitions *****************************************************/

#if !HAVE_SIGINFO_T
/* This allows us to compile on platforms that don't have siginfo_t.
 * Exceptions will work poorly on those platforms. */
#warning Exceptions will work poorly on this platform
typedef void *siginfo_t;
#endif  /* !HAVE_SIGINFO_T */
typedef void (*SIGFUNC)(int, siginfo_t *, void *);

/* Static variables ***********************************************************/
static LONG fatal_signal_received;

/* internal function declarations *********************************************/

static void sigint_handler(int code, siginfo_t *siginfo, void *context);
static void sigquit_handler(int code, siginfo_t *siginfo, void *context);
static void sigill_handler(int code, siginfo_t *siginfo, void *context);
static void sigfpe_handler(int code, siginfo_t *siginfo, void *context);
static void sigsegv_handler(int code, siginfo_t *siginfo, void *context);
static void sigtrap_handler(int code, siginfo_t *siginfo, void *context);
static void sigbus_handler(int code, siginfo_t *siginfo, void *context);
static void fatal_signal_handler(int code, siginfo_t *siginfo, void *context);
static void common_signal_handler(PEXCEPTION_POINTERS pointers, int code, 
                                  native_context_t *ucontext);

void handle_signal(int signal_id, SIGFUNC sigfunc);
inline void check_pal_initialize(int signal_id);

#if HAVE__THREAD_SYS_SIGRETURN
int _thread_sys_sigreturn(native_context_t *);
#endif

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
void CorUnix::suspend_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    CPalThread *pThread = InternalGetCurrentThread();
    pThread->suspensionInfo.HandleSuspendSignal(pThread);
}

void CorUnix::resume_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    CPalThread *pThread = InternalGetCurrentThread();
    pThread->suspensionInfo.HandleResumeSignal();
}
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

/* public function definitions ************************************************/

/*++
Function :
    SEHInitializeSignals

    Set-up signal handlers to catch signals and translate them to exceptions

    (no parameters, no return value)
--*/
void SEHInitializeSignals(void)
{
    TRACE("Initializing signal handlers\n");

    fatal_signal_received = 0;
    
    /* we call handle signal for every possible signal, even
       if we don't provide a signal handler. 

       handle_signal will set SA_RESTART flag for specified signal.
       Therefore, all signals will have SA_RESTART flag set, preventing
       slow Unix system calls from being interrupted. On systems without
       siginfo_t, SIGKILL and SIGSTOP can't be restarted, so we don't
       handle those signals. Both the Darwin and FreeBSD man pages say
       that SIGKILL and SIGSTOP can't be handled, but FreeBSD allows us
       to register a handler for them anyway. We don't do that.

       see sigaction man page for more details
    */
    handle_signal(SIGHUP,    fatal_signal_handler);
    handle_signal(SIGINT,    sigint_handler);
    handle_signal(SIGQUIT,   sigquit_handler);
    handle_signal(SIGILL,    sigill_handler);
    handle_signal(SIGTRAP,   sigtrap_handler);
    handle_signal(SIGABRT,   fatal_signal_handler); 
#ifdef SIGEMT
    handle_signal(SIGEMT,    fatal_signal_handler);
#endif // SIGEMT
    handle_signal(SIGFPE,    sigfpe_handler);
    handle_signal(SIGBUS,    sigbus_handler);
    handle_signal(SIGSEGV,   sigsegv_handler);
    handle_signal(SIGSYS,    fatal_signal_handler); 
    handle_signal(SIGALRM,   fatal_signal_handler); 
    handle_signal(SIGTERM,   fatal_signal_handler); 
    handle_signal(SIGURG,    NULL);
    handle_signal(SIGTSTP,   NULL);
    handle_signal(SIGCONT,   NULL);
    handle_signal(SIGCHLD,   NULL);
    handle_signal(SIGTTIN,   NULL);
    handle_signal(SIGTTOU,   NULL);
    handle_signal(SIGIO,     NULL);
    handle_signal(SIGXCPU,   fatal_signal_handler);
    handle_signal(SIGXFSZ,   fatal_signal_handler);
    handle_signal(SIGVTALRM, fatal_signal_handler);
    handle_signal(SIGPROF,   fatal_signal_handler);
    handle_signal(SIGWINCH,  NULL);
#ifdef SIGINFO
    handle_signal(SIGINFO,   NULL);
#endif  // SIGINFO
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
    handle_signal(SIGUSR1,   suspend_handler);
    handle_signal(SIGUSR2,   resume_handler);
#endif
    
    /* The default action for SIGPIPE is process termination.
       Since SIGPIPE can be signaled when trying to write on a socket for which
       the connection has been dropped, we need to tell the system we want
       to ignore this signal. 
       
       Instead of terminating the process, the system call which would had
       issued a SIGPIPE will, instead, report an error and set errno to EPIPE.
    */
    signal(SIGPIPE, SIG_IGN);
}

/*++
Function :
    SEHCleanupSignals

    Restore default signal handlers

    (no parameters, no return value)
    
note :
reason for this function is that during PAL_Terminate, we reach a point where 
SEH isn't possible anymore (handle manager is off, etc). Past that point, 
we can't avoid crashing on a signal     
--*/
void SEHCleanupSignals (void)
{
    TRACE("Restoring default signal handlers\n");

    handle_signal(SIGHUP,    NULL);
    handle_signal(SIGINT,    NULL);
    handle_signal(SIGQUIT,   NULL);
    handle_signal(SIGILL,    NULL);
    handle_signal(SIGTRAP,   NULL);
    handle_signal(SIGABRT,   NULL); 
#ifdef SIGEMT
    handle_signal(SIGEMT,    NULL);
#endif // SIGEMT
    handle_signal(SIGFPE,    NULL);
    handle_signal(SIGBUS,    NULL);
    handle_signal(SIGPIPE,   NULL);
    handle_signal(SIGSEGV,   NULL);
    handle_signal(SIGSYS,    NULL); 
    handle_signal(SIGALRM,   NULL); 
    handle_signal(SIGTERM,   NULL); 
    handle_signal(SIGXCPU,   NULL);
    handle_signal(SIGXFSZ,   NULL);
    handle_signal(SIGVTALRM, NULL);
    handle_signal(SIGPROF,   NULL);
    /* Do not remove handlers for SIGUSR1 and SIGUSR2. They must remain so threads can be suspended
    during cleanup after this function has been called. */
}


/* internal function definitions **********************************************/

/*++
Function :
    sigint_handler

    This signal is now handled by the PAL signal handling thread : see seh.cpp
    The SIGINT signal (CONTROL_C_EXIT exception) is intercepted by the signal handling thread, 
    which creates a new thread, that calls SEHHandleControlEvent, to handle the SIGINT.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigint_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    ASSERT("Should not reach sigint_handler\n");
}

/*++
Function :
    sigquit_handler
    
    This signal is now handled by the PAL signal handling thread : see seh.cpp
    The SIGQUIT signal is intercepted by the signal handling thread,
    which create a new thread, that calls SEHHandleControlEvent, to handle the SIGQUIT.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigquit_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    ASSERT("Should not reach sigquit_handler\n");

}

/*++
Function :
    sigill_handler

    handle SIGILL signal (EXCEPTION_ILLEGAL_INSTRUCTION, others?)

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigill_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    EXCEPTION_RECORD record;
    EXCEPTION_POINTERS pointers;
    native_context_t *ucontext;

    ucontext = (native_context_t *)context;

    record.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    record.ExceptionFlags = EXCEPTION_IS_SIGNAL; 
    record.ExceptionRecord = NULL;
    record.ExceptionAddress = CONTEXTGetPC(ucontext);
    record.NumberParameters = 0;

    pointers.ExceptionRecord = &record;

    common_signal_handler(&pointers, code, ucontext);

    TRACE("SIGILL Signal was handled; continuing execution.\n");
}

/*++
Function :
    sigfpe_handler

    handle SIGFPE signal (division by zero, floating point exception)

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigfpe_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    EXCEPTION_RECORD record;
    EXCEPTION_POINTERS pointers;
    native_context_t *ucontext;

    ucontext = (native_context_t *)context;

    record.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    record.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    record.ExceptionRecord = NULL;
    record.ExceptionAddress = CONTEXTGetPC(ucontext);
    record.NumberParameters = 0;

    pointers.ExceptionRecord = &record;

    common_signal_handler(&pointers, code, ucontext);

    TRACE("SIGFPE Signal was handled; continuing execution.\n");
}

/*++
Function :
    sigsegv_handler

    handle SIGSEGV signal (EXCEPTION_ACCESS_VIOLATION, others)

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigsegv_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    EXCEPTION_RECORD record;
    EXCEPTION_POINTERS pointers;
    native_context_t *ucontext;
	
    ucontext = (native_context_t *)context;

    record.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    record.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    record.ExceptionRecord = NULL;
    record.ExceptionAddress = CONTEXTGetPC(ucontext);
    record.NumberParameters = 2;

    // TODO: First parameter says whether a read (0) or write (non-0) caused the
    // fault. We must disassemble the instruction at record.ExceptionAddress
    // to correctly fill in this value.
    record.ExceptionInformation[0] = 0;

    // Second parameter is the address that caused the fault.
    record.ExceptionInformation[1] = (size_t)siginfo->si_addr;

    pointers.ExceptionRecord = &record;

    common_signal_handler(&pointers, code, ucontext);

    TRACE("SIGSEGV Signal was handled; continuing execution.\n");
}

/*++
Function :
    sigtrap_handler

    handle SIGTRAP signal (EXCEPTION_SINGLE_STEP, EXCEPTION_BREAKPOINT)

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigtrap_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    EXCEPTION_RECORD record;
    EXCEPTION_POINTERS pointers;
    native_context_t *ucontext;

    ucontext = (native_context_t *)context;

    record.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    record.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    record.ExceptionRecord = NULL;
    record.ExceptionAddress = CONTEXTGetPC(ucontext);
    record.NumberParameters = 0;

    pointers.ExceptionRecord = &record;

    common_signal_handler(&pointers, code, ucontext);

    TRACE("SIGTRAP Signal was handled; continuing execution.\n");
}

/*++
Function :
    sigbus_handler

    handle SIGBUS signal (EXCEPTION_ACCESS_VIOLATION?)

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigbus_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    EXCEPTION_RECORD record;
    EXCEPTION_POINTERS pointers;
    native_context_t *ucontext;

    ucontext = (native_context_t *)context;

    record.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    record.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    record.ExceptionRecord = NULL;
    record.ExceptionAddress = CONTEXTGetPC(ucontext);
    record.NumberParameters = 2;

    // TODO: First parameter says whether a read (0) or write (non-0) caused the
    // fault. We must disassemble the instruction at record.ExceptionAddress
    // to correctly fill in this value.
    record.ExceptionInformation[0] = 0;

    // Second parameter is the address that caused the fault.
    record.ExceptionInformation[1] = (size_t)siginfo->si_addr;

    pointers.ExceptionRecord = &record;

    common_signal_handler(&pointers, code, ucontext);

    TRACE("SIGBUS Signal was handled; continuing execution.\n");
}

/*++
Function :
    SEHSetSafeState

    specify whether the current thread is in a state where exception handling 
    of signals can be done safely

Parameters:
    BOOL state : TRUE if the thread is safe, FALSE otherwise

(no return value)
--*/
void SEHSetSafeState(CPalThread *pthrCurrent, BOOL state)
{
    if (NULL == pthrCurrent)
    {
        ASSERT( "Unable to get the thread object.\n" );
        return;
    }
    pthrCurrent->sehInfo.safe_state = state;
}

/*++
Function :
    SEHGetSafeState

    determine whether the current thread is in a state where exception handling 
    of signals can be done safely

    (no parameters)

Return value :
    TRUE if the thread is in a safe state, FALSE otherwise
--*/
BOOL SEHGetSafeState(CPalThread *pthrCurrent)
{
    if (NULL == pthrCurrent)
    {
        ASSERT( "Unable to get the thread object.\n" );
        return FALSE;
    }
    return pthrCurrent->sehInfo.safe_state;
}

/*++
Function :
    fatal_signal_handler

    This signal handler has been replaced by the PAL signal handling thread : see seh.cpp
    Any signals assigned to this handler are intercepted by the signal handling thread, which
    initiates process termination and cleanup.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
void fatal_signal_handler(int code, siginfo_t *siginfo, void *context)
{
    check_pal_initialize(code);
    ASSERT("Should not reach fatal_signal_handler\n");
}

/*++
Function :
    common_signal_handler

    common code for all signal handlers

Parameters :
    PEXCEPTION_POINTERS pointers : exception information
    native_context_t *ucontext : context structure given to signal handler
    int code : signal received

    (no return value)
Note:
    the "pointers" parameter should contain a valid exception record pointer, 
    but the contextrecord pointer will be overwritten.    
--*/
static void common_signal_handler(PEXCEPTION_POINTERS pointers, int code, 
                                  native_context_t *ucontext)
{
    check_pal_initialize(code);
    sigset_t signal_set;
    CONTEXT context;
    CPalThread *pthrCurrent = InternalGetCurrentThread();

    // Fill context record with required information. from pal.h :
    // On non-Win32 platforms, the CONTEXT pointer in the
    // PEXCEPTION_POINTERS will contain at least the CONTEXT_CONTROL registers.
    CONTEXTFromNativeContext(ucontext, &context,
                             CONTEXT_CONTROL | CONTEXT_INTEGER);

    pointers->ContextRecord = &context;

    /* Unmask signal so we can receive it again */
    sigemptyset(&signal_set);
    sigaddset(&signal_set,code);
    if(-1 == sigprocmask(SIG_UNBLOCK,&signal_set,NULL))
    {
        ASSERT("sigprocmask failed; error is %d (%s)\n",errno, strerror(errno));
    } 

    /* see if we can safely try to handle the exception. we can't if the signal
       occurred while looking for an exception handler, because this would most 
       likely result in infinite recursion */
    if(SEHGetSafeState(pthrCurrent))
    {
        // Indicate the thread is no longer safe to handle signals.
        SEHSetSafeState(pthrCurrent, FALSE);
        SEHRaiseException(pthrCurrent, pointers, code);

        // SEHRaiseException may have returned before resetting the safe 
        // state; do it here.
        SEHSetSafeState(pthrCurrent, TRUE);

        // Use sigreturn, in case the exception handler has changed some
        // registers. sigreturn allows us to set the context and terminate
        // the signal handling.
        CONTEXTToNativeContext(&context, ucontext);

#if HAVE_SETCONTEXT
        setcontext(ucontext);
#elif HAVE__THREAD_SYS_SIGRETURN
        _thread_sys_sigreturn(ucontext);
#elif HAVE_SIGRETURN
        sigreturn(ucontext);
#else
#error Missing a sigreturn equivalent on this platform!
#endif
        ASSERT("sigreturn has returned, it should not.\n");
    }
    else
    {
        /* signal was received while in an unsafe mode. we were already 
           handling a signal when we got this one, and trying to handle it 
           again would most likely result in the signal being triggered again 
           (infinite recursion). abort. */
        ERROR("got a signal during unsafe portion of exception handling. "
              "aborting\n")
        ExitProcess(pointers->ExceptionRecord->ExceptionCode);
    }
}

/*++
Function :
    handle_signal

    register handler for specified signal

Parameters :
    int signal_id : signal to handle
    SIGFUNC sigfunc : signal handler

    (no return value)
    
note : if sigfunc is NULL, the default signal handler is restored    
--*/
void handle_signal(int signal_id, SIGFUNC sigfunc)
{
    struct sigaction act;

    act.sa_flags = SA_RESTART;

    if( NULL == sigfunc )
    {
        act.sa_handler=SIG_DFL;
#if HAVE_SIGINFO_T
        act.sa_sigaction=NULL;
#endif  /* HAVE_SIGINFO_T */
    }
    else
    {
#if HAVE_SIGINFO_T
        act.sa_handler=NULL;
        act.sa_sigaction=sigfunc;
        act.sa_flags |= SA_SIGINFO;
#else   /* HAVE_SIGINFO_T */
        act.sa_handler = SIG_DFL;
#endif  /* HAVE_SIGINFO_T */
    }
    sigemptyset(&act.sa_mask);

    if(-1==sigaction(signal_id,&act,NULL))
    {
        ASSERT("sigaction() call failed with error code %d (%s)\n",
              errno, strerror(errno));
    }
}

/*++
Function :
    check_pal_initialize

    Check if PAL is initialized. If it isn't, deregister signal handler
    and reraise the signal.

Parameters :
    int signal_id : signal to handle

    (no return value)
--*/    
inline void check_pal_initialize(int signal_id) 
{ 
    if (!PALIsInitialized()) 
    { 
        handle_signal(signal_id, NULL); 
        kill(gPID, signal_id); 
    } 
}

DWORD g_dwExternalSignalHandlerThreadId;

static
DWORD
PALAPI
ExternalSignalHandlerThreadRoutine(
    PVOID
    );

static
DWORD
PALAPI
ControlHandlerThreadRoutine(
    PVOID pvSignal
    );

static
DWORD
PALAPI
ShutdownThreadRoutine(
    PVOID
    );

PAL_ERROR
StartExternalSignalHandlerThread(
    CPalThread *pthr
    )
{
    PAL_ERROR palError = NO_ERROR;
    
#ifndef DO_NOT_USE_SIGNAL_HANDLING_THREAD
    HANDLE hThread;

    palError = InternalCreateThread(
        pthr,
        NULL,
        0,
        ExternalSignalHandlerThreadRoutine,
        NULL,
        0,
        SignalHandlerThread, // Want no_suspend variant
        &g_dwExternalSignalHandlerThreadId,
        &hThread
        );

    if (NO_ERROR != palError)
    {
        ERROR("Failure creating external signal handler thread (%d)\n", palError);
        goto done;
    }

    InternalCloseHandle(pthr, hThread);
#endif // DO_NOT_USE_SIGNAL_HANDLING_THREAD

done:

    return palError;        
}

static const int c_iShutdownWaitTime = 5;

static
DWORD
PALAPI
ExternalSignalHandlerThreadRoutine(
    PVOID
    )
{
    DWORD dwThreadId;
    bool fContinue = TRUE;
    bool fShutdownThreadLaunched = FALSE;
    HANDLE hThread;
    int iError;
    int iSignal;
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = InternalGetCurrentThread();
    sigset_t sigsetAll;
    sigset_t sigsetWait;

    //
    // Setup our signal masks
    //

    //
    // SIGPROF is not masked by this thread, and thus not waited for
    // in sigwait since SIGPROF is used by the BSD thread scheduler.
    // Masking SIGPROF in this thread leads to a significant 
    // reduction in performance.
    //

    (void)sigfillset(&sigsetAll); 
    (void)sigdelset(&sigsetAll, SIGPROF);
    (void)sigfillset(&sigsetWait);

#if SIGWAIT_FAILS_WHEN_PASSED_FULL_SIGSET
    (void)sigdelset(&sigsetWait, SIGKILL);
    (void)sigdelset(&sigsetWait, SIGSTOP);
    (void)sigdelset(&sigsetWait, SIGWAITING);
    (void)sigdelset(&sigsetWait, SIGALRM1);
#endif

    //
    // We don't want this thread to wait for signals that
    // we want to leave to the default handler (primarily
    // those involved with terminal or job control).
    //
    //

    (void)sigdelset(&sigsetWait, SIGURG);  
    (void)sigdelset(&sigsetWait, SIGTSTP);
    (void)sigdelset(&sigsetWait, SIGCONT);
    (void)sigdelset(&sigsetWait, SIGCHLD);
    (void)sigdelset(&sigsetWait, SIGTTIN);
    (void)sigdelset(&sigsetWait, SIGTTOU);
    (void)sigdelset(&sigsetWait, SIGIO);
    (void)sigdelset(&sigsetWait, SIGWINCH);
#ifdef SIGINFO
    (void)sigdelset(&sigsetWait, SIGINFO);
#endif  // SIGINFO
    (void)sigdelset(&sigsetWait, SIGPROF);

    //
    // Ideally, we'd like externally generated translated signals
    // (i.e., the signals that we convert to exceptions) to be directed
    // to this thread as well. Unfortunately on some platforms the sigwait
    // will take precedence over the synchronous signal on a thread within
    // this process -- the signal will get directed to this thread, instead
    // of the thread that executed the instruction that raised the signal.
    // This, needless to say, is not good for our EH mechanism.
    //
    // Furthermore, since these signals are not masked on other threads
    // on other platforms the externally generated signal will be directed
    // to one of those threads, instead of this one.
    //

    (void)sigdelset(&sigsetWait, SIGILL);
    (void)sigdelset(&sigsetWait, SIGTRAP);
    (void)sigdelset(&sigsetWait, SIGFPE);
    (void)sigdelset(&sigsetWait, SIGBUS);
    (void)sigdelset(&sigsetWait, SIGSEGV);

    //
    // Mask off all signals for this thread
    //
    
    iError = pthread_sigmask(SIG_SETMASK, &sigsetAll, NULL);
    if (0 != iError)
    {
        ASSERT("pthread sigmask(sigsetAll) failed\n");
        fContinue = FALSE;
    }

    //
    // Wait for a signal to occur
    //

    while (fContinue)
    {
        iError = sigwait(&sigsetWait, &iSignal);
        if (0 != iError)
        {
            ASSERT("sigwait(sigsetWait, iSignal) failed\n");
            fContinue = FALSE;
            break;
        }

        //
        // If the PAL is shutting down we want to exit after waiting
        // a few seconds (in the hopes that the normal shutdown
        // finishes...)
        //

        if (PALIsShuttingDown())
        {
            sleep(c_iShutdownWaitTime);
            fContinue = FALSE;
            break;
        }

        switch (iSignal)
        {
            case SIGINT:
            case SIGQUIT:
            {
                //
                // Spin up a new thread to run the console handlers. We want
                // to do this even if no handlers are installed, as in that
                // case we want to do a normal shutdown from the new thread
                // while still having this thread available to handle any
                // other incoming signals.
                //
                // The new thread is always spawned, even if there are already
                // currently console handlers running; this follows the
                // Windows behavior. Yes, this means that poorly written
                // console handler routines can make it impossible to kill
                // a process using Ctrl-C or Ctrl-Break. "kill -9" is
                // your friend.
                //
                // This thread must not be marked as a PalWorkerThread --
                // since it may run user code it needs to make
                // DLL_THREAD_ATTACH notifications.
                //

                PVOID pvCtrlCode = UintToPtr(
                    SIGINT == iSignal ? CTRL_C_EVENT : CTRL_BREAK_EVENT
                    );

                palError = InternalCreateThread(
                    pthr,
                    NULL,
                    0,
                    ControlHandlerThreadRoutine,
                    pvCtrlCode,
                    0,
                    UserCreatedThread,
                    &dwThreadId,
                    &hThread
                    );

                if (NO_ERROR != palError)
                {
                    fContinue = FALSE;
                    break;
                }

                InternalCloseHandle(pthr, hThread);                

                break;
            }

            default:
            {
                //
                // Any other signal received externally is fatal. If we
                // haven't yet spun up a shutdown thread, do so now; if we
                // have, we want to wait for a bit (in the hope that the
                // shutdown thread is able to complete) and then exit.
                //

                if (fShutdownThreadLaunched)
                {
                    sleep(c_iShutdownWaitTime);
                    fContinue = FALSE;
                    break;
                }

                //
                // Spin up a new thread to perform a graceful shutdown. As
                // with the console control handlers we want this thread
                // to continue to handle external signals.
                //
                // We're going to call TerminateProcess so it's OK for
                // this thread to be a worker thread -- DllMain routines
                // will not be called.
                //

                fShutdownThreadLaunched = TRUE;

                palError = InternalCreateThread(
                    pthr,
                    NULL,
                    0,
                    ShutdownThreadRoutine,
                    NULL,
                    0,
                    PalWorkerThread,
                    &dwThreadId,
                    &hThread
                    );

                if (NO_ERROR != palError)
                {
                    fContinue = FALSE;
                    break;
                }

                InternalCloseHandle(pthr, hThread);
                break;
            }
        }
    }

    //
    // Perform an immediate (non-graceful) shutdown
    //

    _exit(EXIT_FAILURE);    

    return 0;
}

static
DWORD
PALAPI
ControlHandlerThreadRoutine(
    PVOID pvSignal
    )
{
    // Uint and DWORD are implicitly the same.
    SEHHandleControlEvent(PtrToUint(pvSignal), NULL);
    return 0;
}

static
DWORD
PALAPI
ShutdownThreadRoutine(
    PVOID
    )
{
    TerminateProcess(GetCurrentProcess(), CONTROL_C_EXIT);
    return 0;
}

#endif // !HAVE_MACH_EXCEPTIONS
