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

/* internal function declarations *********************************************/

static void sigill_handler(int code, siginfo_t *siginfo, void *context);
static void sigfpe_handler(int code, siginfo_t *siginfo, void *context);
static void sigsegv_handler(int code, siginfo_t *siginfo, void *context);
static void sigtrap_handler(int code, siginfo_t *siginfo, void *context);
static void sigbus_handler(int code, siginfo_t *siginfo, void *context);
static void sigint_handler(int code, siginfo_t *siginfo, void *context);
static void sigquit_handler(int code, siginfo_t *siginfo, void *context);
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
void CorUnix::suspend_handler(int code, siginfo_t *siginfo, void *context);
void CorUnix::resume_handler(int code, siginfo_t *siginfo, void *context);
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

static void common_signal_handler(PEXCEPTION_POINTERS pointers, int code, 
                                  native_context_t *ucontext);

static void handle_signal(int signal_id, SIGFUNC sigfunc, struct sigaction *previousAction);
static void restore_signal(int signal_id, struct sigaction *previousAction);

/* internal data declarations *********************************************/

struct sigaction g_previous_sigill;
struct sigaction g_previous_sigtrap;
struct sigaction g_previous_sigfpe;
struct sigaction g_previous_sigbus;
struct sigaction g_previous_sigsegv;
struct sigaction g_previous_sigint;
struct sigaction g_previous_sigquit;

#if USE_SIGNALS_FOR_THREAD_SUSPENSION
struct sigaction g_previous_sigusr1;
struct sigaction g_previous_sigusr2;
#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

// Pipe used for sending SIGINT / SIGQUIT signals notifications to a helper thread
// that invokes the actual handler.
int g_signalPipe[2];

/* public function definitions ************************************************/

/*++
Function :
    SEHInitializeSignals

    Set-up signal handlers to catch signals and translate them to exceptions

Parameters :
    None

Return :
    TRUE in case of a success, FALSE otherwise
--*/
BOOL SEHInitializeSignals()
{
    TRACE("Initializing signal handlers\n");

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
    handle_signal(SIGILL, sigill_handler, &g_previous_sigill);
    handle_signal(SIGTRAP, sigtrap_handler, &g_previous_sigtrap);
    handle_signal(SIGFPE, sigfpe_handler, &g_previous_sigfpe);
    handle_signal(SIGBUS, sigbus_handler, &g_previous_sigbus);
    handle_signal(SIGSEGV, sigsegv_handler, &g_previous_sigsegv);
    handle_signal(SIGINT, sigint_handler, &g_previous_sigint);
    handle_signal(SIGQUIT, sigquit_handler, &g_previous_sigquit);
#if USE_SIGNALS_FOR_THREAD_SUSPENSION
    handle_signal(SIGUSR1, suspend_handler, &g_previous_sigusr1);
    handle_signal(SIGUSR2, resume_handler, &g_previous_sigusr2);
#endif

    /* The default action for SIGPIPE is process termination.
       Since SIGPIPE can be signaled when trying to write on a socket for which
       the connection has been dropped, we need to tell the system we want
       to ignore this signal.

       Instead of terminating the process, the system call which would had
       issued a SIGPIPE will, instead, report an error and set errno to EPIPE.
    */
    signal(SIGPIPE, SIG_IGN);

    int status = pipe(g_signalPipe);
    
    return (status == 0);
}

/*++
Function :
    SEHCleanupSignals

    Restore default signal handlers

Parameters :
    None

    (no return value)
    
note :
reason for this function is that during PAL_Terminate, we reach a point where 
SEH isn't possible anymore (handle manager is off, etc). Past that point, 
we can't avoid crashing on a signal     
--*/
void SEHCleanupSignals()
{
    TRACE("Restoring default signal handlers\n");

    /* Do not remove handlers for SIGUSR1 and SIGUSR2. They must remain so threads can be suspended
        during cleanup after this function has been called. */
    restore_signal(SIGILL, &g_previous_sigill);
    restore_signal(SIGTRAP, &g_previous_sigtrap);
    restore_signal(SIGFPE, &g_previous_sigfpe);
    restore_signal(SIGBUS, &g_previous_sigbus);
    restore_signal(SIGSEGV, &g_previous_sigsegv);
    restore_signal(SIGINT, &g_previous_sigint);
    restore_signal(SIGQUIT, &g_previous_sigquit);
}

/* internal function definitions **********************************************/

#if USE_SIGNALS_FOR_THREAD_SUSPENSION

void CorUnix::suspend_handler(int code, siginfo_t *siginfo, void *context)
{
    if (PALIsInitialized())
    {
        CPalThread *pThread = InternalGetCurrentThread();
        if (pThread->suspensionInfo.HandleSuspendSignal(pThread)) 
        {
            return;
        }
    }
    
    TRACE("SIGUSR1 signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigusr1.sa_sigaction != NULL)
    {
        g_previous_sigusr1.sa_sigaction(code, siginfo, context);
    }
}

void CorUnix::resume_handler(int code, siginfo_t *siginfo, void *context)
{
    if (PALIsInitialized())
    {
        CPalThread *pThread = InternalGetCurrentThread();
        if (pThread->suspensionInfo.HandleResumeSignal()) 
        {
            return;
        }
    }

    TRACE("SIGUSR2 signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigusr2.sa_sigaction != NULL)
    {
        g_previous_sigusr2.sa_sigaction(code, siginfo, context);
    }
}

#endif // USE_SIGNALS_FOR_THREAD_SUSPENSION

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
    if (PALIsInitialized())
    {
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
    }

    TRACE("SIGILL signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigill.sa_sigaction != NULL)
    {
        g_previous_sigill.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigill);
    }
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
    if (PALIsInitialized())
    {
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
    }

    TRACE("SIGFPE signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigfpe.sa_sigaction != NULL)
    {
        g_previous_sigfpe.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigfpe);
    }
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
    if (PALIsInitialized())
    {
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
    }

    TRACE("SIGSEGV signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigsegv.sa_sigaction != NULL)
    {
        g_previous_sigsegv.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigsegv);
    }
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
    if (PALIsInitialized())
    {
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
    }

    TRACE("SIGTRAP signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigtrap.sa_sigaction != NULL)
    {
        g_previous_sigtrap.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigtrap);
    }
}

/*++
Function :
    HandleExternalSignal

    Handle the SIGINT and SIGQUIT signals.


Parameters :
    signalCode - code of the external signal

    (no return value)
--*/
static void HandleExternalSignal(int signalCode)
{
    BYTE signalCodeByte = (BYTE)signalCode;
    ssize_t writtenBytes;
    do
    {
        writtenBytes = write(g_signalPipe[1], &signalCodeByte, 1);
    }
    while ((writtenBytes == -1) && (errno == EINTR));

    if (writtenBytes == -1)
    {
        // Fatal error
        abort();
    }
}

/*++
Function :
    sigint_handler

    handle SIGINT signal

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigint_handler(int code, siginfo_t *siginfo, void *context)
{
    if (PALIsInitialized())
    {
        HandleExternalSignal(code);
    }
    else 
    {
        TRACE("SIGINT signal was unhandled; chaining to previous sigaction\n");

        if (g_previous_sigint.sa_sigaction != NULL)
        {
            g_previous_sigint.sa_sigaction(code, siginfo, context);
        }
    }
}

/*++
Function :
    sigquit_handler

    handle SIGQUIT signal

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigquit_handler(int code, siginfo_t *siginfo, void *context)
{
    if (PALIsInitialized())
    {
        HandleExternalSignal(code);
    }
    else 
    {
        TRACE("SIGQUIT signal was unhandled; chaining to previous sigaction\n");

        if (g_previous_sigquit.sa_sigaction != NULL)
        {
            g_previous_sigquit.sa_sigaction(code, siginfo, context);
        }
    }
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
    if (PALIsInitialized())
    {
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
    }

    TRACE("SIGBUS signal was unhandled; chaining to previous sigaction\n");

    if (g_previous_sigbus.sa_sigaction != NULL)
    {
        g_previous_sigbus.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigbus);
    }
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
    sigset_t signal_set;
    CONTEXT context;

    // Pre-populate context with data from current frame, because ucontext doesn't have some data (e.g. SS register)
    // which is required for restoring context
    RtlCaptureContext(&context);

    // Fill context record with required information. from pal.h :
    // On non-Win32 platforms, the CONTEXT pointer in the
    // PEXCEPTION_POINTERS will contain at least the CONTEXT_CONTROL registers.
    CONTEXTFromNativeContext(ucontext, &context, CONTEXT_CONTROL | CONTEXT_INTEGER);

    pointers->ContextRecord = &context;

    /* Unmask signal so we can receive it again */
    sigemptyset(&signal_set);
    sigaddset(&signal_set, code);
    if(-1 == sigprocmask(SIG_UNBLOCK, &signal_set, NULL))
    {
        ASSERT("sigprocmask failed; error is %d (%s)\n", errno, strerror(errno));
    } 

    SEHProcessException(pointers);
}

/*++
Function :
    handle_signal

    register handler for specified signal

Parameters :
    int signal_id : signal to handle
    SIGFUNC sigfunc : signal handler
    previousAction : previous sigaction struct

    (no return value)
    
note : if sigfunc is NULL, the default signal handler is restored    
--*/
void handle_signal(int signal_id, SIGFUNC sigfunc, struct sigaction *previousAction)
{
    struct sigaction newAction;

    newAction.sa_flags = SA_RESTART;
#if HAVE_SIGINFO_T
    newAction.sa_handler = NULL;
    newAction.sa_sigaction = sigfunc;
    newAction.sa_flags |= SA_SIGINFO;
#else   /* HAVE_SIGINFO_T */
    newAction.sa_handler = SIG_DFL;
#endif  /* HAVE_SIGINFO_T */
    sigemptyset(&newAction.sa_mask);

    if (-1 == sigaction(signal_id, &newAction, previousAction))
    {
        ASSERT("handle_signal: sigaction() call failed with error code %d (%s)\n",
            errno, strerror(errno));
    }
}

/*++
Function :
    restore_signal

    restore handler for specified signal

Parameters :
    int signal_id : signal to handle
    previousAction : previous sigaction struct to restore

    (no return value)
--*/
void restore_signal(int signal_id, struct sigaction *previousAction)
{
    if (-1 == sigaction(signal_id, previousAction, NULL))
    {
        ASSERT("restore_signal: sigaction() call failed with error code %d (%s)\n",
            errno, strerror(errno));
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

PAL_ERROR
StartExternalSignalHandlerThread(
    CPalThread *pthr)
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

static
DWORD
PALAPI
ExternalSignalHandlerThreadRoutine(
    PVOID
    )
{
    DWORD dwThreadId;
    bool fContinue = TRUE;
    HANDLE hThread;
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pthr = InternalGetCurrentThread();

    //
    // Wait for a signal to occur
    //

    while (fContinue)
    {
        BYTE signalCode;
        ssize_t bytesRead;

        do
        {
            bytesRead = read(g_signalPipe[0], &signalCode, 1);
        }
        while ((bytesRead == -1) && (errno == EINTR));

        if (bytesRead == -1)
        {
            // Fatal error 
            abort();
        }

        switch (signalCode)
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
                    SIGINT == signalCode ? CTRL_C_EVENT : CTRL_BREAK_EVENT
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
                    if (!PALIsShuttingDown())
                    {
                        // If PAL is not shutting down, failure to create a thread is 
                        // a fatal error.
                        abort();
                    }
                    fContinue = FALSE;
                    break;
                }

                InternalCloseHandle(pthr, hThread);                
                break;
            }

            default:
                ASSERT("Unexpected signal %d in signal thread\n", signalCode);
                abort();
                break;
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

#endif // !HAVE_MACH_EXCEPTIONS
