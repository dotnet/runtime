// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "Pal.h"
#include "PalLimitedContext.h"
#include "CommonMacros.h"
#include "config.h"
#include "daccess.h"
#include "regdisplay.h"
#include "NativeContext.h"
#include "HardwareExceptions.h"
#include "UnixSignals.h"
#include "PalCreateDump.h"

#if defined(HOST_APPLE)
#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/exception.h>
#include <mach/task.h>
#endif

#if !HAVE_SIGINFO_T
#error Cannot handle hardware exceptions on this platform
#endif

#include <unix/ExceptionCodeForSignal.h>
#ifdef HOST_AMD64
#include <amd64/DivInstructionDecoder.h>
#endif

struct sigaction g_previousSIGSEGV;
struct sigaction g_previousSIGFPE;

// Exception handler for hardware exceptions
static PHARDWARE_EXCEPTION_HANDLER g_hardwareExceptionHandler = NULL;

#ifdef HOST_AMD64
// Check if a division by zero exception is in fact a division overflow.
bool IsDivByZeroAnIntegerOverflow(void* context)
{
    return DivInstructionHasNonZeroDivisor(
        (uint8_t*)GetPC(context),
        [context](uint32_t index) { return GetRegisterValueByIndex(context, index); });
}
#endif //HOST_AMD64

// Common handler for hardware exception signals
bool HardwareExceptionHandler(int code, siginfo_t *siginfo, void *context, void* faultAddress)
{
    if (g_hardwareExceptionHandler != NULL)
    {
        uintptr_t faultCode = GetExceptionCodeForSignal(siginfo, context);

#ifdef SI_KERNEL
        if (faultCode == EXCEPTION_SIGSEGV_SI_KERNEL)
        {
            faultCode = EXCEPTION_ACCESS_VIOLATION;
        }
#endif // SI_KERNEL

#ifdef HOST_AMD64
        // It is possible that an overflow was mapped to a divide-by-zero exception.
        // This happens when we try to divide the maximum negative value of a
        // signed integer with -1.
        //
        // Thus, we will attempt to decode the instruction @ RIP to determine if that
        // is the case using the faulting context.
        if ((faultCode == EXCEPTION_INT_DIVIDE_BY_ZERO) && IsDivByZeroAnIntegerOverflow(context))
        {
            // The exception was an integer overflow, so augment the fault code.
            faultCode = EXCEPTION_INT_OVERFLOW;
        }
#endif //HOST_AMD64

        PAL_LIMITED_CONTEXT palContext;
        NativeContextToPalContext(context, &palContext);

        uintptr_t arg0Reg;
        uintptr_t arg1Reg;
        int32_t disposition = g_hardwareExceptionHandler(faultCode, (uintptr_t)faultAddress, &palContext, &arg0Reg, &arg1Reg);
        if (disposition == EXCEPTION_CONTINUE_EXECUTION)
        {
            // TODO: better name
            RedirectNativeContext(context, &palContext, arg0Reg, arg1Reg);
            return true;
        }
    }

    return false;
}

// Handler for the SIGSEGV signal
void SIGSEGVHandler(int code, siginfo_t *siginfo, void *context)
{
    bool isHandled = HardwareExceptionHandler(code, siginfo, context, siginfo->si_addr);
    if (isHandled)
    {
        return;
    }

    if (g_previousSIGSEGV.sa_sigaction != NULL)
    {
        g_previousSIGSEGV.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        RestoreSignalHandler(code, &g_previousSIGSEGV);
    }

    PalCreateCrashDumpIfEnabled(code, siginfo, context);
}

// Handler for the SIGFPE signal
void SIGFPEHandler(int code, siginfo_t *siginfo, void *context)
{
    bool isHandled = HardwareExceptionHandler(code, siginfo, context, NULL);
    if (isHandled)
    {
        return;
    }

    if (g_previousSIGFPE.sa_sigaction != NULL)
    {
        g_previousSIGFPE.sa_sigaction(code, siginfo, context);
    }
    else
    {
        // Restore the original or default handler and restart h/w exception
        RestoreSignalHandler(code, &g_previousSIGFPE);
    }

    PalCreateCrashDumpIfEnabled(code, siginfo, context);
}

// Initialize hardware exception handling
bool InitializeHardwareExceptionHandling()
{
    if (!AddSignalHandler(SIGSEGV, SIGSEGVHandler, &g_previousSIGSEGV))
    {
        return false;
    }

    if (!AddSignalHandler(SIGFPE, SIGFPEHandler, &g_previousSIGFPE))
    {
        return false;
    }

#if defined(HOST_APPLE)
#ifndef HOST_TVOS // task_set_exception_ports is not supported on tvOS
	// LLDB installs task-wide Mach exception handlers. XNU dispatches Mach
	// exceptions first to any registered "activation" handler and then to
	// any registered task handler before dispatching the exception to a
	// host-wide Mach exception handler that does translation to POSIX
	// signals. This makes it impossible to use LLDB with implicit null
    // checks in NativeAOT; continuing execution after LLDB traps an
    // EXC_BAD_ACCESS will result in LLDB's EXC_BAD_ACCESS handler being
    // invoked again. This also interferes with the translation of SIGFPEs
    // to .NET-level ArithmeticExceptions. Work around this here by
	// installing a no-op task-wide Mach exception handler for
	// EXC_BAD_ACCESS and EXC_ARITHMETIC.
	kern_return_t kr = task_set_exception_ports(
		mach_task_self(),
		EXC_MASK_BAD_ACCESS | EXC_MASK_ARITHMETIC, /* SIGSEGV, SIGFPE */
		MACH_PORT_NULL,
		EXCEPTION_STATE_IDENTITY,
		MACHINE_THREAD_STATE);
    ASSERT(kr == KERN_SUCCESS);
#endif
#endif

    return true;
}

// Set hardware exception handler
void PalSetHardwareExceptionHandler(PHARDWARE_EXCEPTION_HANDLER handler)
{
    ASSERT_MSG(g_hardwareExceptionHandler == NULL, "Hardware exception handler already set")
    g_hardwareExceptionHandler = handler;
}
