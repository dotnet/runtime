// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    exception/signal.cpp

Abstract:

    Signal handler implementation (map signals to exceptions)



--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

#include "pal/corunix.hpp"
#include "pal/handleapi.hpp"
#include "pal/thread.hpp"
#include "pal/threadinfo.hpp"
#include "pal/threadsusp.hpp"
#include "pal/seh.hpp"

#include "pal/palinternal.h"
#if !HAVE_MACH_EXCEPTIONS
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

#ifdef SIGRTMIN
#define INJECT_ACTIVATION_SIGNAL SIGRTMIN
#endif

#if !defined(INJECT_ACTIVATION_SIGNAL) && defined(FEATURE_HIJACK)
#error FEATURE_HIJACK requires INJECT_ACTIVATION_SIGNAL to be defined
#endif

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
static void sigterm_handler(int code, siginfo_t *siginfo, void *context);

static bool common_signal_handler(int code, siginfo_t *siginfo, void *sigcontext, int numParams, ...);

#ifdef INJECT_ACTIVATION_SIGNAL
static void inject_activation_handler(int code, siginfo_t *siginfo, void *context);
#endif

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
struct sigaction g_previous_sigterm;

static bool registered_sigterm_handler = false;

#ifdef INJECT_ACTIVATION_SIGNAL
struct sigaction g_previous_activation;
#endif

/* public function definitions ************************************************/

/*++
Function :
    SEHInitializeSignals

    Set up signal handlers to catch signals and translate them to exceptions

Parameters :
    None

Return :
    TRUE in case of a success, FALSE otherwise
--*/
BOOL SEHInitializeSignals(DWORD flags)
{
    TRACE("Initializing signal handlers\n");

    /* we call handle_signal for every possible signal, even
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

    if (flags & PAL_INITIALIZE_REGISTER_SIGTERM_HANDLER)
    {
        handle_signal(SIGTERM, sigterm_handler, &g_previous_sigterm);
        registered_sigterm_handler = true;
    }

#ifdef INJECT_ACTIVATION_SIGNAL
    handle_signal(INJECT_ACTIVATION_SIGNAL, inject_activation_handler, &g_previous_activation);
#endif

    /* The default action for SIGPIPE is process termination.
       Since SIGPIPE can be signaled when trying to write on a socket for which
       the connection has been dropped, we need to tell the system we want
       to ignore this signal.

       Instead of terminating the process, the system call which would had
       issued a SIGPIPE will, instead, report an error and set errno to EPIPE.
    */
    signal(SIGPIPE, SIG_IGN);

    return TRUE;
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
we can't avoid crashing on a signal.
--*/
void SEHCleanupSignals()
{
    TRACE("Restoring default signal handlers\n");

    restore_signal(SIGILL, &g_previous_sigill);
    restore_signal(SIGTRAP, &g_previous_sigtrap);
    restore_signal(SIGFPE, &g_previous_sigfpe);
    restore_signal(SIGBUS, &g_previous_sigbus);
    restore_signal(SIGSEGV, &g_previous_sigsegv);
    restore_signal(SIGINT, &g_previous_sigint);
    restore_signal(SIGQUIT, &g_previous_sigquit);

    if (registered_sigterm_handler)
    {
        restore_signal(SIGTERM, &g_previous_sigterm);
    }

#ifdef INJECT_ACTIVATION_SIGNAL
    restore_signal(INJECT_ACTIVATION_SIGNAL, &g_previous_activation);
#endif
}

/* internal function definitions **********************************************/

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
        if (common_signal_handler(code, siginfo, context, 0))
        {
            return;
        }
    }

    if (g_previous_sigill.sa_sigaction != NULL)
    {
        g_previous_sigill.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigill);
    }

    PROCNotifyProcessShutdown();
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
        if (common_signal_handler(code, siginfo, context, 0))
        {
            return;
        }
    }

    if (g_previous_sigfpe.sa_sigaction != NULL)
    {
        g_previous_sigfpe.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigfpe);
    }

    PROCNotifyProcessShutdown();
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
        // TODO: First variable parameter says whether a read (0) or write (non-0) caused the
        // fault. We must disassemble the instruction at record.ExceptionAddress
        // to correctly fill in this value.
        if (common_signal_handler(code, siginfo, context, 2, (size_t)0, (size_t)siginfo->si_addr))
        {
            return;
        }
    }

    if (g_previous_sigsegv.sa_sigaction != NULL)
    {
        g_previous_sigsegv.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigsegv);
    }

    PROCNotifyProcessShutdown();
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
        if (common_signal_handler(code, siginfo, context, 0))
        {
            return;
        }
    }

    if (g_previous_sigtrap.sa_sigaction != NULL)
    {
        g_previous_sigtrap.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // We abort instead of restore the original or default handler and returning
        // because returning from a SIGTRAP handler continues execution past the trap.
        PROCAbort();
    }

    PROCNotifyProcessShutdown();
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
        // TODO: First variable parameter says whether a read (0) or write (non-0) caused the
        // fault. We must disassemble the instruction at record.ExceptionAddress
        // to correctly fill in this value.
        if (common_signal_handler(code, siginfo, context, 2, (size_t)0, (size_t)siginfo->si_addr))
        {
            return;
        }
    }

    if (g_previous_sigbus.sa_sigaction != NULL)
    {
        g_previous_sigbus.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        restore_signal(code, &g_previous_sigbus);
    }

    PROCNotifyProcessShutdown();
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
    PROCNotifyProcessShutdown();

    // Restore the original or default handler and resend signal
    restore_signal(code, &g_previous_sigint);
    kill(gPID, code);
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
    PROCNotifyProcessShutdown();

    // Restore the original or default handler and resend signal
    restore_signal(code, &g_previous_sigquit);
    kill(gPID, code);
}

/*++
Function :
    sigterm_handler

    handle SIGTERM signal

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigterm_handler(int code, siginfo_t *siginfo, void *context)
{
    if (PALIsInitialized())
    {
        // g_pSynchronizationManager shouldn't be null if PAL is initialized.
        _ASSERTE(g_pSynchronizationManager != nullptr);

        g_pSynchronizationManager->SendTerminationRequestToWorkerThread();
    }
    else
    {
        if (g_previous_sigterm.sa_sigaction != NULL)
        {
            g_previous_sigterm.sa_sigaction(code, siginfo, context);
        }
    }
}

#ifdef INJECT_ACTIVATION_SIGNAL
/*++
Function :
    inject_activation_handler

    Handle the INJECT_ACTIVATION_SIGNAL signal. This signal interrupts a running thread
    so it can call the activation function that was specified when sending the signal.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

(no return value)
--*/
static void inject_activation_handler(int code, siginfo_t *siginfo, void *context)
{
    // Only accept activations from the current process
    if (g_activationFunction != NULL && siginfo->si_pid == getpid())
    {
        _ASSERTE(g_safeActivationCheckFunction != NULL);

        native_context_t *ucontext = (native_context_t *)context;

        CONTEXT winContext;
        CONTEXTFromNativeContext(
            ucontext, 
            &winContext, 
            CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT);

        if (g_safeActivationCheckFunction(CONTEXTGetPC(&winContext), /* checkingCurrentThread */ TRUE))
        {
            g_activationFunction(&winContext);
            // Activation function may have modified the context, so update it.
            CONTEXTToNativeContext(&winContext, ucontext);
        }
    }
    else if (g_previous_activation.sa_sigaction != NULL)
    {
        g_previous_activation.sa_sigaction(code, siginfo, context);
    }
}
#endif

/*++
Function :
    InjectActivationInternal

    Interrupt the specified thread and have it call the activationFunction passed in

Parameters :
    pThread            - target PAL thread
    activationFunction - function to call 

(no return value)
--*/
PAL_ERROR InjectActivationInternal(CorUnix::CPalThread* pThread)
{
#ifdef INJECT_ACTIVATION_SIGNAL
    int status = pthread_kill(pThread->GetPThreadSelf(), INJECT_ACTIVATION_SIGNAL);
    if (status != 0)
    {
        // Failure to send the signal is fatal. There are only two cases when sending
        // the signal can fail. First, if the signal ID is invalid and second, 
        // if the thread doesn't exist anymore.
        PROCAbort();
    }

    return NO_ERROR;
#else
    return ERROR_CANCELLED;
#endif
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
    int code : signal received
    siginfo_t *siginfo : siginfo passed to the signal handler
    void *context : context structure passed to the signal handler
    int numParams : number of variable parameters of the exception
    ... : variable parameters of the exception (each of size_t type)

    Returns true if the execution should continue or false if the exception was unhandled
Note:
    the "pointers" parameter should contain a valid exception record pointer,
    but the ContextRecord pointer will be overwritten.
--*/
static bool common_signal_handler(int code, siginfo_t *siginfo, void *sigcontext, int numParams, ...)
{
    sigset_t signal_set;
    CONTEXT *contextRecord;
    EXCEPTION_RECORD *exceptionRecord;
    native_context_t *ucontext;

    ucontext = (native_context_t *)sigcontext;

    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    exceptionRecord->ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    exceptionRecord->ExceptionFlags = EXCEPTION_IS_SIGNAL;
    exceptionRecord->ExceptionRecord = NULL;
    exceptionRecord->ExceptionAddress = GetNativeContextPC(ucontext);
    exceptionRecord->NumberParameters = numParams;

    va_list params;
    va_start(params, numParams);

    for (int i = 0; i < numParams; i++)
    {
        exceptionRecord->ExceptionInformation[i] = va_arg(params, size_t);
    }

    // Pre-populate context with data from current frame, because ucontext doesn't have some data (e.g. SS register)
    // which is required for restoring context
    RtlCaptureContext(contextRecord);

    ULONG contextFlags = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT;

#if defined(_AMD64_)
    contextFlags |= CONTEXT_XSTATE;
#endif

    // Fill context record with required information. from pal.h:
    // On non-Win32 platforms, the CONTEXT pointer in the
    // PEXCEPTION_POINTERS will contain at least the CONTEXT_CONTROL registers.
    CONTEXTFromNativeContext(ucontext, contextRecord, contextFlags);

    /* Unmask signal so we can receive it again */
    sigemptyset(&signal_set);
    sigaddset(&signal_set, code);
    int sigmaskRet = pthread_sigmask(SIG_UNBLOCK, &signal_set, NULL);
    if (sigmaskRet != 0)
    {
        ASSERT("pthread_sigmask failed; error number is %d\n", sigmaskRet);
    }

    contextRecord->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
    // The exception object takes ownership of the exceptionRecord and contextRecord
    PAL_SEHException exception(exceptionRecord, contextRecord);

    if (SEHProcessException(&exception))
    {
        // Exception handling may have modified the context, so update it.
        CONTEXTToNativeContext(contextRecord, ucontext);
        return true;
    }

    return false;
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

#endif // !HAVE_MACH_EXCEPTIONS
