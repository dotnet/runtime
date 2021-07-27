// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "pal/process.h"
#include "pal/thread.hpp"
#include "pal/threadinfo.hpp"
#include "pal/threadsusp.hpp"
#include "pal/seh.hpp"
#include "pal/signal.hpp"

#include "pal/palinternal.h"

#include <errno.h>
#include <signal.h>

#if !HAVE_MACH_EXCEPTIONS
#include "pal/init.h"
#include "pal/debug.h"
#include "pal/virtual.h"
#include "pal/utils.h"

#include <string.h>
#include <sys/ucontext.h>
#include <sys/utsname.h>
#include <unistd.h>
#include <sys/mman.h>


#endif // !HAVE_MACH_EXCEPTIONS
#include "pal/context.h"

#ifdef SIGRTMIN
#define INJECT_ACTIVATION_SIGNAL SIGRTMIN
#else
#define INJECT_ACTIVATION_SIGNAL SIGUSR1
#endif

#if !defined(INJECT_ACTIVATION_SIGNAL) && defined(FEATURE_HIJACK)
#error FEATURE_HIJACK requires INJECT_ACTIVATION_SIGNAL to be defined
#endif

using namespace CorUnix;

/* local type definitions *****************************************************/

typedef void (*SIGFUNC)(int, siginfo_t *, void *);

/* internal function declarations *********************************************/

static void sigterm_handler(int code, siginfo_t *siginfo, void *context);
#ifdef INJECT_ACTIVATION_SIGNAL
static void inject_activation_handler(int code, siginfo_t *siginfo, void *context);
#endif

static void sigill_handler(int code, siginfo_t *siginfo, void *context);
static void sigfpe_handler(int code, siginfo_t *siginfo, void *context);
static void sigsegv_handler(int code, siginfo_t *siginfo, void *context);
static void sigtrap_handler(int code, siginfo_t *siginfo, void *context);
static void sigbus_handler(int code, siginfo_t *siginfo, void *context);
static void sigint_handler(int code, siginfo_t *siginfo, void *context);
static void sigquit_handler(int code, siginfo_t *siginfo, void *context);
static void sigabrt_handler(int code, siginfo_t *siginfo, void *context);

static bool common_signal_handler(int code, siginfo_t *siginfo, void *sigcontext, int numParams, ...);

static void handle_signal(int signal_id, SIGFUNC sigfunc, struct sigaction *previousAction, int additionalFlags = 0, bool skipIgnored = false);
static void restore_signal(int signal_id, struct sigaction *previousAction);
static void restore_signal_and_resend(int code, struct sigaction* action);

/* internal data declarations *********************************************/

bool g_registered_signal_handlers = false;
#if !HAVE_MACH_EXCEPTIONS
bool g_enable_alternate_stack_check = false;
#endif // !HAVE_MACH_EXCEPTIONS

static bool g_registered_sigterm_handler = false;
static bool g_registered_activation_handler = false;

struct sigaction g_previous_sigterm;
#ifdef INJECT_ACTIVATION_SIGNAL
struct sigaction g_previous_activation;
#endif

struct sigaction g_previous_sigill;
struct sigaction g_previous_sigtrap;
struct sigaction g_previous_sigfpe;
struct sigaction g_previous_sigbus;
struct sigaction g_previous_sigsegv;
struct sigaction g_previous_sigint;
struct sigaction g_previous_sigquit;
struct sigaction g_previous_sigabrt;

#if !HAVE_MACH_EXCEPTIONS

// Offset of the local variable containing pointer to windows style context in the common_signal_handler function.
// This offset is relative to the frame pointer.
int g_common_signal_handler_context_locvar_offset = 0;

// TOP of special stack for handling stack overflow
volatile void* g_stackOverflowHandlerStack = NULL;

// Flag that is or-ed with SIGSEGV to indicate that the SIGSEGV was a stack overflow
const int StackOverflowFlag = 0x40000000;

#endif // !HAVE_MACH_EXCEPTIONS

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
BOOL SEHInitializeSignals(CorUnix::CPalThread *pthrCurrent, DWORD flags)
{
    TRACE("Initializing signal handlers %04x\n", flags);

#if !HAVE_MACH_EXCEPTIONS
    char* enableAlternateStackCheck = getenv("COMPlus_EnableAlternateStackCheck");

    g_enable_alternate_stack_check = enableAlternateStackCheck && (strtoul(enableAlternateStackCheck, NULL, 10) != 0);
#endif

    if (flags & PAL_INITIALIZE_REGISTER_SIGNALS)
    {
        g_registered_signal_handlers = true;

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
        handle_signal(SIGFPE, sigfpe_handler, &g_previous_sigfpe);
        handle_signal(SIGBUS, sigbus_handler, &g_previous_sigbus);
        handle_signal(SIGABRT, sigabrt_handler, &g_previous_sigabrt);
        // We don't setup a handler for SIGINT/SIGQUIT when those signals are ignored.
        // Otherwise our child processes would reset to the default on exec causing them
        // to terminate on these signals.
        handle_signal(SIGINT, sigint_handler, &g_previous_sigint, 0 /* additionalFlags */, true /* skipIgnored */);
        handle_signal(SIGQUIT, sigquit_handler, &g_previous_sigquit, 0 /* additionalFlags */, true /* skipIgnored */);

#if HAVE_MACH_EXCEPTIONS
        handle_signal(SIGSEGV, sigsegv_handler, &g_previous_sigsegv);
#else
        handle_signal(SIGTRAP, sigtrap_handler, &g_previous_sigtrap);
        // SIGSEGV handler runs on a separate stack so that we can handle stack overflow
        handle_signal(SIGSEGV, sigsegv_handler, &g_previous_sigsegv, SA_ONSTACK);

        if (!pthrCurrent->EnsureSignalAlternateStack())
        {
            return FALSE;
        }

        // Allocate the minimal stack necessary for handling stack overflow
        int stackOverflowStackSize = ALIGN_UP(sizeof(SignalHandlerWorkerReturnPoint), 16) + 7 * 4096;
        // Align the size to virtual page size and add one virtual page as a stack guard
        stackOverflowStackSize = ALIGN_UP(stackOverflowStackSize, GetVirtualPageSize()) + GetVirtualPageSize();
        int flags = MAP_ANONYMOUS | MAP_PRIVATE;
#ifdef MAP_STACK
        flags |= MAP_STACK;
#endif
        g_stackOverflowHandlerStack = mmap(NULL, stackOverflowStackSize, PROT_READ | PROT_WRITE, flags, -1, 0);
        if (g_stackOverflowHandlerStack == MAP_FAILED)
        {
            return FALSE;
        }

        // create a guard page for the alternate stack
        int st = mprotect((void*)g_stackOverflowHandlerStack, GetVirtualPageSize(), PROT_NONE);
        if (st != 0)
        {
            munmap((void*)g_stackOverflowHandlerStack, stackOverflowStackSize);
            return FALSE;
        }

        g_stackOverflowHandlerStack = (void*)((size_t)g_stackOverflowHandlerStack + stackOverflowStackSize);
#endif // HAVE_MACH_EXCEPTIONS
    }

    /* The default action for SIGPIPE is process termination.
       Since SIGPIPE can be signaled when trying to write on a socket for which
       the connection has been dropped, we need to tell the system we want
       to ignore this signal.

       Instead of terminating the process, the system call which would had
       issued a SIGPIPE will, instead, report an error and set errno to EPIPE.
    */
    signal(SIGPIPE, SIG_IGN);

    if (flags & PAL_INITIALIZE_REGISTER_SIGTERM_HANDLER)
    {
        g_registered_sigterm_handler = true;
        handle_signal(SIGTERM, sigterm_handler, &g_previous_sigterm);
    }

#ifdef INJECT_ACTIVATION_SIGNAL
    handle_signal(INJECT_ACTIVATION_SIGNAL, inject_activation_handler, &g_previous_activation);
    g_registered_activation_handler = true;
#endif

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

    if (g_registered_signal_handlers)
    {
        restore_signal(SIGILL, &g_previous_sigill);
#if !HAVE_MACH_EXCEPTIONS
        restore_signal(SIGTRAP, &g_previous_sigtrap);
#endif
        restore_signal(SIGFPE, &g_previous_sigfpe);
        restore_signal(SIGBUS, &g_previous_sigbus);
        restore_signal(SIGABRT, &g_previous_sigabrt);
        restore_signal(SIGSEGV, &g_previous_sigsegv);
        restore_signal(SIGINT, &g_previous_sigint);
        restore_signal(SIGQUIT, &g_previous_sigquit);
    }

#ifdef INJECT_ACTIVATION_SIGNAL
    if (g_registered_activation_handler)
    {
        restore_signal(INJECT_ACTIVATION_SIGNAL, &g_previous_activation);
    }
#endif

    if (g_registered_sigterm_handler)
    {
        restore_signal(SIGTERM, &g_previous_sigterm);
    }
}

/*++
Function :
    SEHCleanupAbort()

    Restore default SIGABORT signal handlers

    (no parameters, no return value)
--*/
void SEHCleanupAbort()
{
    if (g_registered_signal_handlers)
    {
        restore_signal(SIGABRT, &g_previous_sigabrt);
    }
}

/* internal function definitions **********************************************/

/*++
Function :
    IsRunningOnAlternateStack

    Detects if the current signal handlers is running on an alternate stack

Parameters :
    The context of the signal

Return :
    true if we are running on an alternate stack

--*/
bool IsRunningOnAlternateStack(void *context)
{
#if HAVE_MACH_EXCEPTIONS
    return false;
#else
    bool isRunningOnAlternateStack;
    if (g_enable_alternate_stack_check)
    {
        // Note: WSL doesn't return the alternate signal ranges in the uc_stack (the whole structure is zeroed no
        // matter whether the code is running on an alternate stack or not). So the check would always fail on WSL.
        stack_t *signalStack = &((native_context_t *)context)->uc_stack;
        // Check if the signalStack local variable address is within the alternate stack range. If it is not,
        // then either the alternate stack was not installed at all or the current method is not running on it.
        void* alternateStackEnd = (char *)signalStack->ss_sp + signalStack->ss_size;
        isRunningOnAlternateStack = ((signalStack->ss_flags & SS_DISABLE) == 0) && (signalStack->ss_sp <= &signalStack) && (&signalStack < alternateStackEnd);
    }
    else
    {
        // If alternate stack check is disabled, consider always that we are running on an alternate
        // signal handler stack.
        isRunningOnAlternateStack = true;
    }

    return isRunningOnAlternateStack;
#endif // HAVE_MACH_EXCEPTIONS
}

static bool IsSaSigInfo(struct sigaction* action)
{
    return (action->sa_flags & SA_SIGINFO) != 0;
}

static bool IsSigDfl(struct sigaction* action)
{
    // macOS can return sigaction with SIG_DFL and SA_SIGINFO.
    // SA_SIGINFO means we should use sa_sigaction, but here we want to check sa_handler.
    // So we ignore SA_SIGINFO when sa_sigaction and sa_handler are at the same address.
    return (&action->sa_handler == (void*)&action->sa_sigaction || !IsSaSigInfo(action)) &&
            action->sa_handler == SIG_DFL;
}

static bool IsSigIgn(struct sigaction* action)
{
    return (&action->sa_handler == (void*)&action->sa_sigaction || !IsSaSigInfo(action)) &&
            action->sa_handler == SIG_IGN;
}

/*++
Function :
    invoke_previous_action

    synchronously invokes the previous action or aborts when that is not possible

Parameters :
    action  : previous sigaction struct
    code    : signal code
    siginfo : signal siginfo
    context : signal context
    signalRestarts: BOOL state : TRUE if the process will be signalled again

    (no return value)
--*/
static void invoke_previous_action(struct sigaction* action, int code, siginfo_t *siginfo, void *context, bool signalRestarts = true)
{
    _ASSERTE(action != NULL);

    if (IsSigIgn(action))
    {
        if (signalRestarts)
        {
            // This signal mustn't be ignored because it will be restarted.
            PROCAbort(code);
        }
        return;
    }
    else if (IsSigDfl(action))
    {
        if (signalRestarts)
        {
            // Restore the original and restart h/w exception.
            restore_signal(code, action);
        }
        else
        {
            // We can't invoke the original handler because returning from the
            // handler doesn't restart the exception.
            PROCAbort(code);
        }
    }
    else if (IsSaSigInfo(action))
    {
        // Directly call the previous handler.
        _ASSERTE(action->sa_sigaction != NULL);
        action->sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Directly call the previous handler.
        _ASSERTE(action->sa_handler != NULL);
        action->sa_handler(code);
    }

    PROCNotifyProcessShutdown(IsRunningOnAlternateStack(context));

    PROCCreateCrashDumpIfEnabled(code);
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
    if (PALIsInitialized())
    {
        if (common_signal_handler(code, siginfo, context, 0))
        {
            return;
        }
    }

    invoke_previous_action(&g_previous_sigill, code, siginfo, context);
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

    invoke_previous_action(&g_previous_sigfpe, code, siginfo, context);
}

#if !HAVE_MACH_EXCEPTIONS

/*++
Function :
    signal_handler_worker

    Handles signal on the original stack where the signal occured.
    Invoked via setcontext.

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)
    returnPoint - context to which the function returns if the common_signal_handler returns

    (no return value)
--*/
extern "C" void signal_handler_worker(int code, siginfo_t *siginfo, void *context, SignalHandlerWorkerReturnPoint* returnPoint)
{
    // TODO: First variable parameter says whether a read (0) or write (non-0) caused the
    // fault. We must disassemble the instruction at record.ExceptionAddress
    // to correctly fill in this value.

    // Unmask the activation signal now that we are running on the original stack of the thread
    sigset_t signal_set;
    sigemptyset(&signal_set);
    sigaddset(&signal_set, INJECT_ACTIVATION_SIGNAL);

    int sigmaskRet = pthread_sigmask(SIG_UNBLOCK, &signal_set, NULL);
    if (sigmaskRet != 0)
    {
        ASSERT("pthread_sigmask failed; error number is %d\n", sigmaskRet);
    }

    returnPoint->returnFromHandler = common_signal_handler(code, siginfo, context, 2, (size_t)0, (size_t)siginfo->si_addr);

    // We are going to return to the alternate stack, so block the activation signal again
    sigmaskRet = pthread_sigmask(SIG_BLOCK, &signal_set, NULL);
    if (sigmaskRet != 0)
    {
        ASSERT("pthread_sigmask failed; error number is %d\n", sigmaskRet);
    }

    RtlRestoreContext(&returnPoint->context, NULL);
}

/*++
Function :
    SwitchStackAndExecuteHandler

    Switch to the stack specified by the sp argument

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)
    sp - stack pointer of the stack to execute the handler on.
         If sp == 0, execute it on the original stack where the signal has occured.
Return :
    The return value from the signal handler
--*/
static bool SwitchStackAndExecuteHandler(int code, siginfo_t *siginfo, void *context, size_t sp)
{
    // Establish a return point in case the common_signal_handler returns

    volatile bool contextInitialization = true;

    void *ptr = alloca(sizeof(SignalHandlerWorkerReturnPoint) + alignof(SignalHandlerWorkerReturnPoint) - 1);
    SignalHandlerWorkerReturnPoint *pReturnPoint = (SignalHandlerWorkerReturnPoint *)ALIGN_UP(ptr, alignof(SignalHandlerWorkerReturnPoint));
    RtlCaptureContext(&pReturnPoint->context);

    // When the signal handler worker completes, it uses setcontext to return to this point

    if (contextInitialization)
    {
        contextInitialization = false;
        ExecuteHandlerOnCustomStack(code, siginfo, context, sp, pReturnPoint);
        _ASSERTE(FALSE); // The ExecuteHandlerOnCustomStack should never return
    }

    return pReturnPoint->returnFromHandler;
}

#endif // !HAVE_MACH_EXCEPTIONS

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
#if !HAVE_MACH_EXCEPTIONS
    if (PALIsInitialized())
    {
        // First check if we have a stack overflow
        size_t sp = (size_t)GetNativeContextSP((native_context_t *)context);
        size_t failureAddress = (size_t)siginfo->si_addr;

        // If the failure address is at most one page above or below the stack pointer,
        // we have a stack overflow.
        if ((failureAddress - (sp - GetVirtualPageSize())) < 2 * GetVirtualPageSize())
        {
            if (GetCurrentPalThread())
            {
                size_t handlerStackTop = __sync_val_compare_and_swap((size_t*)&g_stackOverflowHandlerStack, (size_t)g_stackOverflowHandlerStack, 0);
                if (handlerStackTop == 0)
                {
                    // We have only one stack for handling stack overflow preallocated. We let only the first thread that hits stack overflow to
                    // run the exception handling code on that stack (which ends up just dumping the stack trace and aborting the process).
                    // Other threads are held spinning and sleeping here until the process exits.
                    while (true)
                    {
                        sleep(1);
                    }
                }

                if (SwitchStackAndExecuteHandler(code | StackOverflowFlag, siginfo, context, (size_t)handlerStackTop))
                {
                    PROCAbort(SIGSEGV);
                }
            }
            else
            {
                (void)!write(STDERR_FILENO, StackOverflowMessage, sizeof(StackOverflowMessage) - 1);
                PROCAbort(SIGSEGV);
            }
        }

        // Now that we know the SIGSEGV didn't happen due to a stack overflow, execute the common
        // hardware signal handler on the original stack.

        if (GetCurrentPalThread() && IsRunningOnAlternateStack(context))
        {
            if (SwitchStackAndExecuteHandler(code, siginfo, context, 0 /* sp */)) // sp == 0 indicates execution on the original stack
            {
                return;
            }
        }
        else
        {
            // The code flow gets here when the signal handler is not running on an alternate stack or when it wasn't created
            // by coreclr. In both cases, we execute the common_signal_handler directly.
            // If thread isn't created by coreclr and has alternate signal stack GetCurrentPalThread() will return NULL too.
            // But since in this case we don't handle hardware exceptions (IsSafeToHandleHardwareException returns false)
            // we can call common_signal_handler on the alternate stack.
            if (common_signal_handler(code, siginfo, context, 2, (size_t)0, (size_t)siginfo->si_addr))
            {
                return;
            }
        }
    }
#endif // !HAVE_MACH_EXCEPTIONS

    invoke_previous_action(&g_previous_sigsegv, code, siginfo, context);
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

    // The signal doesn't restart, returning from a SIGTRAP handler continues execution past the trap.
    invoke_previous_action(&g_previous_sigtrap, code, siginfo, context, /* signalRestarts */ false);
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

    invoke_previous_action(&g_previous_sigbus, code, siginfo, context);
}

/*++
Function :
    sigabrt_handler

    handle SIGABRT signal - abort() API

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

    (no return value)
--*/
static void sigabrt_handler(int code, siginfo_t *siginfo, void *context)
{
    invoke_previous_action(&g_previous_sigabrt, code, siginfo, context);
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

    restore_signal_and_resend(code, &g_previous_sigint);
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

    restore_signal_and_resend(code, &g_previous_sigquit);
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
        restore_signal_and_resend(SIGTERM, &g_previous_sigterm);
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
    if (g_activationFunction != NULL && (siginfo->si_pid == getpid()
#ifdef HOST_OSX
    // On OSX si_pid is sometimes 0. It was confirmed by Apple to be expected, as the si_pid is tracked at the process level. So when multiple
    // signals are in flight in the same process at the same time, it may be overwritten / zeroed.
    || siginfo->si_pid == 0
#endif
    ))
    {
        _ASSERTE(g_safeActivationCheckFunction != NULL);

        native_context_t *ucontext = (native_context_t *)context;

        CONTEXT winContext;
        CONTEXTFromNativeContext(
            ucontext,
            &winContext,
            CONTEXT_CONTROL | CONTEXT_INTEGER);

        if (g_safeActivationCheckFunction(CONTEXTGetPC(&winContext), /* checkingCurrentThread */ TRUE))
        {
            int savedErrNo = errno; // Make sure that errno is not modified
            g_activationFunction(&winContext);
            errno = savedErrNo;

            // Activation function may have modified the context, so update it.
            CONTEXTToNativeContext(&winContext, ucontext);
        }
    }
    else
    {
        // Call the original handler when it is not ignored or default (terminate).
        if (g_previous_activation.sa_flags & SA_SIGINFO)
        {
            _ASSERTE(g_previous_activation.sa_sigaction != NULL);
            g_previous_activation.sa_sigaction(code, siginfo, context);
        }
        else
        {
            if (g_previous_activation.sa_handler != SIG_IGN &&
                g_previous_activation.sa_handler != SIG_DFL)
            {
                _ASSERTE(g_previous_activation.sa_handler != NULL);
                g_previous_activation.sa_handler(code);
            }
        }
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
    // We can get EAGAIN when printing stack overflow stack trace and when other threads hit
    // stack overflow too. Those are held in the sigsegv_handler with blocked signals until
    // the process exits.
    if ((status != 0) && (status != EAGAIN))
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


#if !HAVE_MACH_EXCEPTIONS
/*++
Function :
    signal_ignore_handler

    Simple signal handler which does nothing

Parameters :
    POSIX signal handler parameter list ("man sigaction" for details)

(no return value)
--*/
static void signal_ignore_handler(int code, siginfo_t *siginfo, void *context)
{
}
#endif // !HAVE_MACH_EXCEPTIONS


void PAL_IgnoreProfileSignal(int signalNum)
{
#if !HAVE_MACH_EXCEPTIONS
    // Add a signal handler which will ignore signals
    // This will allow signal to be used as a marker in perf recording.
    // This will be used as an aid to synchronize recorded profile with
    // test cases
    //
    // signal(signalNum, SGN_IGN) can not be used here.  It will ignore
    // the signal in kernel space and therefore generate no recordable
    // event for profiling. Preventing it being used for profile
    // synchronization
    //
    // Since this is only used in rare circumstances no attempt to
    // restore the old handler will be made
    handle_signal(signalNum, signal_ignore_handler, 0);
#endif
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
__attribute__((noinline))
static bool common_signal_handler(int code, siginfo_t *siginfo, void *sigcontext, int numParams, ...)
{
#if !HAVE_MACH_EXCEPTIONS
    sigset_t signal_set;
    CONTEXT signalContextRecord;
    EXCEPTION_RECORD exceptionRecord;
    native_context_t *ucontext;

    ucontext = (native_context_t *)sigcontext;
    g_common_signal_handler_context_locvar_offset = (int)((char*)&signalContextRecord - (char*)__builtin_frame_address(0));

    if (code == (SIGSEGV | StackOverflowFlag))
    {
        exceptionRecord.ExceptionCode = EXCEPTION_STACK_OVERFLOW;
        code &= ~StackOverflowFlag;
    }
    else
    {
        exceptionRecord.ExceptionCode = CONTEXTGetExceptionCodeForSignal(siginfo, ucontext);
    }
    exceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    exceptionRecord.ExceptionRecord = NULL;
    exceptionRecord.ExceptionAddress = GetNativeContextPC(ucontext);
    exceptionRecord.NumberParameters = numParams;

    va_list params;
    va_start(params, numParams);

    for (int i = 0; i < numParams; i++)
    {
        exceptionRecord.ExceptionInformation[i] = va_arg(params, size_t);
    }

    // Pre-populate context with data from current frame, because ucontext doesn't have some data (e.g. SS register)
    // which is required for restoring context
    RtlCaptureContext(&signalContextRecord);

    ULONG contextFlags = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_FLOATING_POINT;

#if defined(HOST_AMD64)
    contextFlags |= CONTEXT_XSTATE;
#endif

    // Fill context record with required information. from pal.h:
    // On non-Win32 platforms, the CONTEXT pointer in the
    // PEXCEPTION_POINTERS will contain at least the CONTEXT_CONTROL registers.
    CONTEXTFromNativeContext(ucontext, &signalContextRecord, contextFlags);

    /* Unmask signal so we can receive it again */
    sigemptyset(&signal_set);
    sigaddset(&signal_set, code);
    int sigmaskRet = pthread_sigmask(SIG_UNBLOCK, &signal_set, NULL);
    if (sigmaskRet != 0)
    {
        ASSERT("pthread_sigmask failed; error number is %d\n", sigmaskRet);
    }

    signalContextRecord.ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;

    // The exception object takes ownership of the exceptionRecord and contextRecord
    PAL_SEHException exception(&exceptionRecord, &signalContextRecord, true);

    if (SEHProcessException(&exception))
    {
        // Exception handling may have modified the context, so update it.
        CONTEXTToNativeContext(exception.ExceptionPointers.ContextRecord, ucontext);
        return true;
    }
#endif // !HAVE_MACH_EXCEPTIONS
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
void handle_signal(int signal_id, SIGFUNC sigfunc, struct sigaction *previousAction, int additionalFlags, bool skipIgnored)
{
    struct sigaction newAction;

    newAction.sa_flags = SA_RESTART | additionalFlags;
    newAction.sa_handler = NULL;
    newAction.sa_sigaction = sigfunc;
    newAction.sa_flags |= SA_SIGINFO;
    sigemptyset(&newAction.sa_mask);

#ifdef INJECT_ACTIVATION_SIGNAL
    if ((additionalFlags & SA_ONSTACK) != 0)
    {
        // A handler that runs on a separate stack should not be interrupted by the activation signal
        // until it switches back to the regular stack, since that signal's handler would run on the
        // limited separate stack and likely run into a stack overflow.
        sigaddset(&newAction.sa_mask, INJECT_ACTIVATION_SIGNAL);
    }
#endif

    if (skipIgnored)
    {
        if (-1 == sigaction(signal_id, NULL, previousAction))
        {
            ASSERT("handle_signal: sigaction() call failed with error code %d (%s)\n",
                errno, strerror(errno));
        }
        else if (previousAction->sa_handler == SIG_IGN)
        {
            return;
        }
    }

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

/*++
Function :
    restore_signal_and_resend

    restore handler for specified signal and signal the process

Parameters :
    int signal_id : signal to handle
    previousAction : previous sigaction struct to restore

    (no return value)
--*/
void restore_signal_and_resend(int signal_id, struct sigaction* previousAction)
{
    restore_signal(signal_id, previousAction);
    kill(gPID, signal_id);
}
