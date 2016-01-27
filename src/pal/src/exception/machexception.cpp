// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    machexception.cpp

Abstract:

    Implementation of MACH exception API functions.



--*/

#include "pal/thread.hpp"
#include "pal/seh.hpp"
#include "pal/palinternal.h"
#if HAVE_MACH_EXCEPTIONS
#include "machexception.h"
#include "pal/dbgmsg.h"
#include "pal/critsect.h"
#include "pal/debug.h"
#include "pal/init.h"
#include "pal/utils.h"
#include "pal/context.h"
#include "pal/malloc.hpp"
#include "pal/process.h"
#include "pal/virtual.h"
#include "pal/map.hpp"

#include "machmessage.h"

#include <errno.h>
#include <string.h>
#include <unistd.h>
#include <pthread.h>
#include <dlfcn.h>
#include <mach-o/loader.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(EXCEPT);

void ForwardMachException(CPalThread *pThread, MachMessage *pMessage);

// The port we use to handle exceptions and to set the thread context
mach_port_t s_ExceptionPort;

static BOOL s_DebugInitialized = FALSE;

static const char * PAL_MACH_EXCEPTION_MODE = "PAL_MachExceptionMode";

enum MachExceptionMode
{
    // special value to indicate we've not initialized yet
    MachException_Uninitialized     = -1,

    // These can be combined with bitwise OR to incrementally turn off
    // functionality for diagnostics purposes.
    //
    // In practice, the following values are probably useful:
    //   1: Don't turn illegal instructions into SEH exceptions.
    //      On Intel, stack misalignment usually shows up as an
    //      illegal instruction.  PAL client code shouldn't
    //      expect to see any of these, so this option should
    //      always be safe to set.
    //   2: Don't listen for breakpoint exceptions.  This makes an
    //      SEH-based debugger (i.e., managed debugger) unusable,
    //      but you may need this option if you find that native
    //      breakpoints you set in PAL-dependent code don't work
    //      (causing hangs or crashes in the native debugger).
    //   3: Combination of the above.
    //      This is the typical setting for development
    //      (unless you're working on the managed debugger).
    //   7: In addition to the above, don't turn bad accesses and
    //      arithmetic exceptions into SEH.
    //      This is the typical setting for stress.
    MachException_SuppressIllegal   = 1,
    MachException_SuppressDebugging = 2,
    MachException_SuppressManaged   = 4,

    // Default value to use if environment variable not set.
    MachException_Default           = 0,
};

static exception_mask_t GetExceptionMask()
{
    static MachExceptionMode exMode = MachException_Uninitialized;

    if (exMode == MachException_Uninitialized)
    {
        exMode = MachException_Default;

        const char * exceptionSettings = getenv(PAL_MACH_EXCEPTION_MODE);
        if (exceptionSettings)
        {
            exMode = (MachExceptionMode)atoi(exceptionSettings);
        }
        else
        {
            if (PAL_IsDebuggerPresent())
            {
                exMode = MachException_SuppressDebugging;
            }
        }
    }

    exception_mask_t machExceptionMask = 0;
    if (!(exMode & MachException_SuppressIllegal))
    {
        machExceptionMask |= PAL_EXC_ILLEGAL_MASK;
    }
    if (!(exMode & MachException_SuppressDebugging))
    {
#ifdef FEATURE_PAL_SXS
        // Always hook exception ports for breakpoint exceptions.
        // The reason is that we don't know when a managed debugger
        // will attach, so we have to be prepared.  We don't want
        // to later go through the thread list and hook exception
        // ports for exactly those threads that currently are in
        // this PAL.
        machExceptionMask |= PAL_EXC_DEBUGGING_MASK;
#else // FEATURE_PAL_SXS
        if (s_DebugInitialized)
        {
            machExceptionMask |= PAL_EXC_DEBUGGING_MASK;
        }
#endif // FEATURE_PAL_SXS
    }
    if (!(exMode & MachException_SuppressManaged))
    {
        machExceptionMask |= PAL_EXC_MANAGED_MASK;
    }

    return machExceptionMask;
}

#define NONPAL_RETAIL_ASSERT(...)                                       \
    {                                                                   \
        {                                                               \
            PAL_EnterHolder enterHolder;                                \
            PAL_printf(__VA_ARGS__);                                    \
            PAL_DisplayDialogFormatted("NON-PAL ASSERT", __VA_ARGS__);  \
        }                                                               \
        DBG_DebugBreak();                                               \
        abort();                                                        \
    }

#define CHECK_MACH(function, machret)                                   \
    if (machret != KERN_SUCCESS)                                        \
    {                                                                   \
        NONPAL_RETAIL_ASSERT(function " failed: %08X: %s\n", machret, mach_error_string(machret)); \
    }

#ifdef FEATURE_PAL_SXS

/*++
Function :
    CPalThread::EnableMachExceptions 

    Hook Mach exceptions, i.e., call thread_swap_exception_ports
    to replace the thread's current exception ports with our own.
    The previously active exception ports are saved.  Called when
    this thread enters a region of code that depends on this PAL.

Return value :
    ERROR_SUCCESS, if enabling succeeded
    an error code, otherwise
--*/
PAL_ERROR CorUnix::CPalThread::EnableMachExceptions()
{
    TRACE("%08X: Enter()\n", (unsigned int)(size_t)this);

    exception_mask_t machExceptionMask = GetExceptionMask();
    if (machExceptionMask != 0)
    {
#ifdef _DEBUG
        // verify that the arrays we've allocated to hold saved exception ports
        // are the right size.
        exception_mask_t countBits = PAL_EXC_ALL_MASK;
        countBits = ((countBits & 0xAAAAAAAA) >>  1) + (countBits & 0x55555555);
        countBits = ((countBits & 0xCCCCCCCC) >>  2) + (countBits & 0x33333333);
        countBits = ((countBits & 0xF0F0F0F0) >>  4) + (countBits & 0x0F0F0F0F);
        countBits = ((countBits & 0xFF00FF00) >>  8) + (countBits & 0x00FF00FF);
        countBits = ((countBits & 0xFFFF0000) >> 16) + (countBits & 0x0000FFFF);
        if (countBits != static_cast<exception_mask_t>(
            CThreadMachExceptionHandlerNode::s_nPortsMax))
        {
            ASSERT("s_nPortsMax is %u, but needs to be %u\n",
                   CThreadMachExceptionHandlerNode::s_nPortsMax, countBits);
        }
#endif // _DEBUG

        // We store a set of previous handlers and register an exception port that is unique to both (to help
        // us get the correct chain-back semantics in as many scenarios as possible). The following call tells
        // us which we should do.
        CThreadMachExceptionHandlerNode *pSavedHandlers = m_sMachExceptionHandlers.GetNodeForInitialization();
        NONPAL_TRACE("Enabling handlers for thread %08X exception port %08X\n", GetMachPortSelf(), s_ExceptionPort);

        // Swap current handlers into temporary storage first. That's because it's possible (even likely) that
        // some or all of the handlers might still be ours. In those cases we don't want to overwrite the
        // chain-back entries with these useless self-references.
        kern_return_t MachRet;
        mach_msg_type_number_t oldCount = CThreadMachExceptionHandlerNode::s_nPortsMax;
        exception_mask_t rgMasks[CThreadMachExceptionHandlerNode::s_nPortsMax];
        exception_handler_t rgHandlers[CThreadMachExceptionHandlerNode::s_nPortsMax];
        exception_behavior_t rgBehaviors[CThreadMachExceptionHandlerNode::s_nPortsMax];
        thread_state_flavor_t rgFlavors[CThreadMachExceptionHandlerNode::s_nPortsMax];
        thread_port_t thread = mach_thread_self();
        exception_behavior_t excepBehavior = EXCEPTION_STATE_IDENTITY;

        MachRet = thread_swap_exception_ports(thread,
                                              machExceptionMask,
                                              s_ExceptionPort,
                                              excepBehavior,
                                              MACHINE_THREAD_STATE,
                                              rgMasks,
                                              &oldCount,
                                              rgHandlers,
                                              rgBehaviors,
                                              rgFlavors);

        kern_return_t MachRetDeallocate = mach_port_deallocate(mach_task_self(), thread);
        CHECK_MACH("mach_port_deallocate", MachRetDeallocate);

        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("thread_swap_exception_ports failed: %d\n", MachRet);
            return UTIL_MachErrorToPalError(MachRet);
        }

        // Scan through the returned handlers looking for those that are ours.
        for (mach_msg_type_number_t i = 0; i < oldCount; i++)
        {
            if (rgHandlers[i] == s_ExceptionPort)
            {
                // We were already registered for the exceptions indicated by rgMasks[i]. Look through each
                // exception (set bit in the mask) separately, checking whether we previously had a (non-CLR)
                // registration for that handle.
                for (size_t j = 0; j < (sizeof(exception_mask_t) * 8); j++)
                {
                    // Skip unset bits (exceptions not covered by this entry).
                    exception_mask_t bmException = rgMasks[i] & (1 << j);
                    if (bmException == 0)
                        continue;

                    // Find record in the previous data that covers this exception.
                    bool fFoundPreviousHandler = false;
                    for (int k = 0; k < pSavedHandlers->m_nPorts; k++)
                    {
                        // Skip records for different exceptions.
                        if (!(pSavedHandlers->m_masks[k] & bmException))
                            continue;

                        // Found one. By definition it shouldn't be one of our handlers.
                        if (pSavedHandlers->m_handlers[k] == s_ExceptionPort)
                            ASSERT("Stored our own handlers in Mach exception chain-back info.\n");

                        // We need to replicate the handling details back into our temporary data in place of
                        // the CLR record. There are several things that can happen:
                        // 1) One of the other entries has the same handler, behavior and flavor (for a
                        //    different set of exceptions). We could merge the data for this exception into
                        //    that record (set another bit in the masks array entry).
                        // 2) This was the only exception in the current entry (only one bit was set in the
                        //    mask) and we can simply re-use this entry (overwrite the handler, behavior and
                        //    flavor entries).
                        // 3) Multiple exceptions were covered by this entry. In this case we should add a new
                        //    entry covering just the current exception. We're guaranteed to have space to do
                        //    this since we allocated enough entries to cover one exception per-entry and we
                        //    have at least one entry with two or more exceptions (this one).
                        // It turns out we can ignore case 1 (which involves complicating our logic still
                        // further) since we have no requirement to tightly pack all the entries for the same
                        // handler/behavior/flavor (like thread_swap_exception_ports does). We're perfectly
                        // happy having six entries for six exceptions handled by identical handlers rather
                        // than a single entry with six bits set in the exception mask.
                        if (rgMasks[i] == bmException)
                        {
                            // Entry was only for this exception. Simply overwrite handler/behavior and flavor
                            // with the stored values.
                            rgHandlers[i] = pSavedHandlers->m_handlers[k];
                            rgBehaviors[i] = pSavedHandlers->m_behaviors[k];
                            rgFlavors[i] = pSavedHandlers->m_flavors[k];
                        }
                        else
                        {
                            // More than one exception handled by this record. Store the old data in a new
                            // cell of the temporary data and remove the exception from the old cell.
                            if ((int)oldCount == CThreadMachExceptionHandlerNode::s_nPortsMax)
                                ASSERT("Ran out of space to expand exception handlers. This shouldn't happen.\n");

                            rgMasks[oldCount] = bmException;
                            rgHandlers[oldCount] = pSavedHandlers->m_handlers[k];
                            rgBehaviors[oldCount] = pSavedHandlers->m_behaviors[k];
                            rgFlavors[oldCount] = pSavedHandlers->m_flavors[k];

                            // The old cell no longer describes this exception.
                            rgMasks[i] &= ~bmException;

                            oldCount++;
                        }

                        // We found a match.
                        fFoundPreviousHandler = true;
                        break;
                    }

                    // If we didn't find a match then we still don't want to record our own handler. Just
                    // reset the bit in the masks value (implicitly recording that we have no-chain back entry
                    // for this exception).
                    if (!fFoundPreviousHandler)
                        rgMasks[i] &= ~bmException;
                }
            }
        }

        // We've cleaned any mention of our own handlers from the data. It's safe to persist it.
        pSavedHandlers->m_nPorts = oldCount;
        memcpy(pSavedHandlers->m_masks, rgMasks, sizeof(rgMasks));
        memcpy(pSavedHandlers->m_handlers, rgHandlers, sizeof(rgHandlers));
        memcpy(pSavedHandlers->m_behaviors, rgBehaviors, sizeof(rgBehaviors));
        memcpy(pSavedHandlers->m_flavors, rgFlavors, sizeof(rgFlavors));
    }
    return ERROR_SUCCESS;
}

/*++
Function :
    CPalThread::DisableMachExceptions

    Unhook Mach exceptions, i.e., call thread_set_exception_ports
    to restore the thread's exception ports with those we saved
    in EnableMachExceptions.  Called when this thread leaves a
    region of code that depends on this PAL.

Return value :
    ERROR_SUCCESS, if disabling succeeded
    an error code, otherwise
--*/
PAL_ERROR CorUnix::CPalThread::DisableMachExceptions()
{
    TRACE("%08X: Leave()\n", (unsigned int)(size_t)this);

    PAL_ERROR palError = NO_ERROR;
    
    // We only store exceptions when we're installing exceptions.
    if (0 == GetExceptionMask())
        return palError;
    
    // Get the handlers to restore. It isn't really as simple as this. We keep two sets of handlers (which
    // improves our ability to chain correctly in more scenarios) but this means we can encounter dilemmas
    // where we've recorded two different handlers for the same port and can only re-register one of them
    // (with a very high chance that it does not chain to the other). I don't believe it matters much today:
    // in the absence of CoreCLR shutdown we don't throw away our thread context until a thread dies (in fact
    // usually a bit later than this). Hopefully by the time this changes we'll have a better design for
    // hardware exception handling overall.
    CThreadMachExceptionHandlerNode *savedPorts = m_sMachExceptionHandlers.GetNodeForCleanup();

    kern_return_t MachRet = KERN_SUCCESS;
    for (int i = 0; i < savedPorts->m_nPorts; i++)
    {
        // If no handler was ever set, thread_swap_exception_ports returns
        // MACH_PORT_NULL for the handler and zero values for behavior
        // and flavor.  Unfortunately, the latter are invalid even for
        // MACH_PORT_NULL when you use thread_set_exception_ports.
        exception_behavior_t behavior =
            savedPorts->m_behaviors[i] ? savedPorts->m_behaviors[i] : EXCEPTION_DEFAULT;
        thread_state_flavor_t flavor =
            savedPorts->m_flavors[i] ? savedPorts->m_flavors[i] : MACHINE_THREAD_STATE;
        thread_port_t thread = mach_thread_self();
        MachRet = thread_set_exception_ports(thread,
                                             savedPorts->m_masks[i],
                                             savedPorts->m_handlers[i],
                                             behavior,
                                             flavor);
        kern_return_t MachRetDeallocate = mach_port_deallocate(mach_task_self(), thread);
        CHECK_MACH("mach_port_deallocate", MachRetDeallocate);
                                             
        if (MachRet != KERN_SUCCESS)
            break;
    }
    
    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("thread_set_exception_ports failed: %d\n", MachRet);
        palError = UTIL_MachErrorToPalError(MachRet);
    }

    return palError;
}

#else // FEATURE_PAL_SXS

/*++
Function :
    SEHEnableMachExceptions 

    Enable SEH-related stuff related to mach exceptions

    (no parameters)

Return value :
    TRUE  if enabling succeeded
    FALSE otherwise
--*/
BOOL SEHEnableMachExceptions()
{
    exception_mask_t machExceptionMask = GetExceptionMask();
    if (machExceptionMask != 0)
    {
        kern_return_t MachRet;
        MachRet = task_set_exception_ports(mach_task_self(),
                                           machExceptionMask,
                                           s_ExceptionPort,
                                           EXCEPTION_DEFAULT,
                                           MACHINE_THREAD_STATE);

        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("task_set_exception_ports failed: %d\n", MachRet);
            UTIL_SetLastErrorFromMach(MachRet);
            return FALSE;
        }
    }
    return TRUE;
}

/*++
Function :
    SEHDisableMachExceptions

    Disable SEH-related stuff related to mach exceptions

    (no parameters)

Return value :
    TRUE  if enabling succeeded
    FALSE otherwise
--*/
BOOL SEHDisableMachExceptions()
{
    exception_mask_t machExceptionMask = GetExceptionMask();
    if (machExceptionMask != 0)
    {
        kern_return_t MachRet;
        MachRet = task_set_exception_ports(mach_task_self(),
                                           machExceptionMask,
                                           MACH_PORT_NULL,
                                           EXCEPTION_DEFAULT,
                                           MACHINE_THREAD_STATE);

        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("task_set_exception_ports failed: %d\n", MachRet);
            UTIL_SetLastErrorFromMach(MachRet);
            return FALSE;
        }
    }
    return TRUE;
}

#endif // FEATURE_PAL_SXS

#if !defined(_AMD64_)
void PAL_DispatchException(PCONTEXT pContext, PEXCEPTION_RECORD pExRecord, MachMessage *pMessage)
#else // defined(_AMD64_)

// Since HijackFaultingThread pushed the context, exception record and mach exception message on the stack,
// we need to adjust the signature of PAL_DispatchException such that the corresponding arguments are considered
// to be on the stack per GCC64 calling convention rules. Hence, the first 6 dummy arguments (corresponding to RDI,
// RSI, RDX,RCX, R8, R9).
void PAL_DispatchException(DWORD64 dwRDI, DWORD64 dwRSI, DWORD64 dwRDX, DWORD64 dwRCX, DWORD64 dwR8, DWORD64 dwR9, PCONTEXT pContext, PEXCEPTION_RECORD pExRecord, MachMessage *pMessage)
#endif // !defined(_AMD64_)
{
    CPalThread *pThread = InternalGetCurrentThread();

#if FEATURE_PAL_SXS
    if (!pThread->IsInPal())
    {
        // It's now possible to observe system exceptions in code running outside the PAL (as the result of a
        // p/invoke since we no longer revert our Mach exception ports in this case). In that scenario we need
        // to re-enter the PAL now as the exception signals the end of the p/invoke.
        PAL_Reenter(PAL_BoundaryBottom);
    }
#endif // FEATURE_PAL_SXS

    EXCEPTION_POINTERS pointers;
    pointers.ExceptionRecord = pExRecord;
    pointers.ContextRecord = pContext;

    TRACE("PAL_DispatchException(EC %08x EA %p)\n", pExRecord->ExceptionCode, pExRecord->ExceptionAddress);
    SEHProcessException(&pointers);

    // Chain the exception to the next PAL
    ForwardMachException(pThread, pMessage);
}

#if defined(_X86_) || defined(_AMD64_)
extern "C" void PAL_DispatchExceptionWrapper();
extern "C" int PAL_DispatchExceptionReturnOffset;
#endif // _X86_ || _AMD64_

/*++
Function :
    ExceptionRecordFromMessage

    Setups up an ExceptionRecord from an exception message

Parameters :
    message - exception message to build the exception record
    pExceptionRecord - exception record to setup
*/
static void 
ExceptionRecordFromMessage(
    MachMessage &message,               // [in] exception message
    EXCEPTION_RECORD *pExceptionRecord) // [out] Used to return exception parameters
{
    exception_type_t exception = message.GetException();
    MACH_EH_TYPE(exception_data_type_t) subcodes[2];
    mach_msg_type_number_t subcode_count;
    
    subcode_count = message.GetExceptionCodeCount();
    if (subcode_count < 0 || subcode_count > 2)
        NONPAL_RETAIL_ASSERT("Bad exception subcode count: %d", subcode_count);

    for (int i = 0; i < subcode_count; i++)
        subcodes[i] = message.GetExceptionCode(i);

    memset(pExceptionRecord, 0, sizeof(EXCEPTION_RECORD));

    DWORD exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;

    switch(exception)
    {
    // Could not access memory. subcode contains the bad memory address. 
    case EXC_BAD_ACCESS:
        if (subcode_count != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected subcode");
            exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION; 
        }
        else
        {
            exceptionCode = EXCEPTION_ACCESS_VIOLATION;

            pExceptionRecord->NumberParameters = 2;
            pExceptionRecord->ExceptionInformation[0] = 0;
            pExceptionRecord->ExceptionInformation[1] = subcodes[1];
            NONPAL_TRACE("subcodes[1] = %llx\n", subcodes[1]);
        }
        break;

    // Instruction failed. Illegal or undefined instruction or operand. 
    case EXC_BAD_INSTRUCTION :
        // TODO: Identify privileged instruction. Need to get the thread state and read the machine code. May
        // be better to do this in the place that calls SEHProcessException, similar to how it's done on Linux.
        exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION; 
        break;

    // Arithmetic exception; exact nature of exception is in subcode field. 
    case EXC_ARITHMETIC:
        if (subcode_count != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected subcode");
            exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION; 
        }
        else
        {
            switch (subcodes[0])
            {
#if defined(_X86_) || defined(_AMD64_)
                case EXC_I386_DIV:
                    exceptionCode = EXCEPTION_INT_DIVIDE_BY_ZERO;
                    break;
                case EXC_I386_INTO:
                    exceptionCode = EXCEPTION_INT_OVERFLOW;
                    break;
                case EXC_I386_EXTOVR:
                    exceptionCode = EXCEPTION_FLT_OVERFLOW;
                    break;
                case EXC_I386_BOUND:
                    exceptionCode = EXCEPTION_ARRAY_BOUNDS_EXCEEDED;
                    break;
#else
#error Trap code to exception mapping not defined for this architecture
#endif
                default:
                    exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
                    break;
            }
        }
        break;

    case EXC_SOFTWARE:
#if defined(_X86_) || defined(_AMD64_)
        exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
        break;
#else
#error Trap code to exception mapping not defined for this architecture
#endif

    // Trace, breakpoint, etc. Details in subcode field. 
    case EXC_BREAKPOINT:
#if defined(_X86_) || defined(_AMD64_)
        if (subcodes[0] == EXC_I386_SGL)
        {
            exceptionCode = EXCEPTION_SINGLE_STEP;
        }
        else if (subcodes[0] == EXC_I386_BPT)
        {
            exceptionCode = EXCEPTION_BREAKPOINT;
        }
#else
#error Trap code to exception mapping not defined for this architecture
#endif
        else
        {
            WARN("unexpected subcode %d for EXC_BREAKPOINT", subcodes[0]);
            exceptionCode = EXCEPTION_BREAKPOINT;
        }
        break;


    // System call requested. Details in subcode field. 
    case EXC_SYSCALL:
        exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION; 
        break;

    // System call with a number in the Mach call range requested. Details in subcode field. 
    case EXC_MACH_SYSCALL:
        exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION; 
        break;

    default:
        ASSERT("Got unknown trap code %d\n", exception);
        break;
    }

    pExceptionRecord->ExceptionCode = exceptionCode;
}

#ifdef _DEBUG
const char *
GetExceptionString(
   exception_type_t exception
)
{
    switch(exception)
    {
    case EXC_BAD_ACCESS:
        return "EXC_BAD_ACCESS";

    case EXC_BAD_INSTRUCTION:
        return "EXC_BAD_INSTRUCTION";

    case EXC_ARITHMETIC:
        return "EXC_ARITHMETIC";

    case EXC_SOFTWARE:
        return "EXC_SOFTWARE";

    case EXC_BREAKPOINT:
        return "EXC_BREAKPOINT";

    case EXC_SYSCALL:
        return "EXC_SYSCALL";

    case EXC_MACH_SYSCALL:
        return "EXC_MACH_SYSCALL";

    default:
        ASSERT("Got unknown trap code %d\n", exception);
        break;
    }
    return "INVALID CODE";
}
#endif // _DEBUG

/*++
Function :
    HijackFaultingThread

    Sets the faulting thread up to return to PAL_DispatchException with an
    ExceptionRecord, thread CONTEXT and the exception MachMessage.

Parameters:
    thread - thread the exception happened
    task - task the exception happened
    message - exception message

Return value :
    None
--*/

static
void
HijackFaultingThread(
    mach_port_t thread,             // [in] thread the exception happened on
    mach_port_t task,               // [in] task the exception happened on
    MachMessage &message)           // [in] exception message
{
    thread_state_flavor_t threadStateFlavor;
    x86_thread_state_t threadState;
    EXCEPTION_RECORD exceptionRecord;
    CONTEXT threadContext;
    kern_return_t machret;
    unsigned int count;

    // Fill in the exception record from the exception message
    ExceptionRecordFromMessage(message, &exceptionRecord);
    
    // Get the thread state from the exception message and convert the count of bytes into
    // the count of ints
    threadStateFlavor = message.GetThreadStateFlavor();
    count = message.GetThreadState(threadStateFlavor, (thread_state_t)&threadState, thread) / sizeof(natural_t);

#ifdef _X86_
    threadContext.ContextFlags = CONTEXT_FLOATING_POINT | CONTEXT_EXTENDED_REGISTERS
#else
    threadContext.ContextFlags = CONTEXT_FLOATING_POINT;
#endif
    // Get just the floating point registers directly from the thread because the message context is only
    // the general registers.
    machret = CONTEXT_GetThreadContextFromPort(thread, &threadContext);
    CHECK_MACH("CONTEXT_GetThreadContextFromPort", machret);

    // Now get the rest of the registers from the exception message. Don't save/restore the debug registers 
    // because loading them on OSx causes a privileged instruction fault. The "DE" in CR4 is set.
    threadContext.ContextFlags |= CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS;
    CONTEXT_GetThreadContextFromThreadState(threadStateFlavor, (thread_state_t)&threadState, &threadContext);

#if defined(CORECLR) && (defined(_X86_) || defined(_AMD64_))
    // For CoreCLR we look more deeply at access violations to determine whether they're the result of a stack
    // overflow. If so we'll terminate the process immediately (the current default policy of the CoreCLR EE).
    // Otherwise we'll either A/V ourselves trying to set up the SEH exception record and context on the
    // target thread's stack (unlike Windows there's no extra stack reservation to guarantee this can be done)
    // or, and this the case we're trying to avoid, it's possible we'll succeed and the runtime will go ahead
    // and process the SO like it was a simple AV. Since the runtime doesn't currently implement stack probing
    // on non-Windows platforms, this could lead to data corruption (we have SO intolerant code in the runtime
    // which manipulates global state under the assumption that an SO cannot occur due to a prior stack
    // probe).

    // Determining whether an AV is really an SO is not quite straightforward. We can get stack bounds
    // information from pthreads but (a) we only have the target Mach thread port and no way to map to a
    // pthread easily and (b) the pthread functions lie about the bounds on the main thread.

    // Instead we inspect the target thread SP we just retrieved above and compare it with the AV address. If
    // they both lie in the same page or the SP is at a higher address than the AV but in the same VM region,
    // then we'll consider the AV to be an SO. Note that we can't assume that SP will be in the same page as
    // the AV on an SO, even though we force GCC to generate stack probes on stack extension (-fstack-check).
    // That's because GCC currently generates the probe *before* altering SP. Since a given stack extension can
    // involve multiple pages and GCC generates all the required probes before updating SP in a single
    // operation, the faulting probe can be at an address that is far removed from the thread's current value
    // of SP.

    // In the case where the AV and SP aren't in the same or adjacent pages we check if the first page
    // following the faulting address belongs in the same VM region as the current value of SP. Since all pages
    // in a VM region have the same attributes this check eliminates the possibility that there's another guard
    // page in the range between the fault and the SP, effectively establishing that the AV occurred in the
    // guard page associated with the stack associated with the SP.

    // We are assuming here that thread stacks are always allocated in a single VM region. I've seen no
    // evidence thus far that this is not the case (and the mere fact we rely on Mach apis already puts us on
    // brittle ground anyway).

    //  (a)     SP always marks the current limit of the stack (in that all valid stack accesses will be of
    //          the form [SP + delta]). The Mac x86 ABI appears to guarantee this (or rather it does not
    //          guarantee that stack slots below SP will not be invalidated by asynchronous events such as
    //          interrupts, which mostly amounts to the same thing for user mode code). Note that the Mac PPC
    //          ABI does allow some (constrained) access below SP, but we're not currently supporting this
    //          platform.
    //  (b)     All code will extend the stack "carefully" (by which we mean that stack extensions of more
    //          than one page in size will touch at least one byte in each intervening page (in decreasing
    //          address order), to guarantee that the guard page is hit before memory beyond the guard page is
    //          corrupted). Our managed jits always generate code which does this as does MSVC. GCC, however,
    //          does not do this by default. We have to explicitly provide the -fstack-check compiler option
    //          to enable the behavior.
#if (defined(_X86_) || defined(_AMD64_)) && defined(__APPLE__)
    if (exceptionRecord.ExceptionCode == EXCEPTION_ACCESS_VIOLATION)
    {
        // Assume this AV isn't an SO to begin with.
        bool fIsStackOverflow = false;

        // Calculate the page base addresses for the fault and the faulting thread's SP.
        int cbPage = getpagesize();
        char *pFaultPage = (char*)(exceptionRecord.ExceptionInformation[1] & ~(cbPage - 1));
#ifdef _X86_
        char *pStackTopPage = (char*)(threadContext.Esp & ~(cbPage - 1));
#elif defined(_AMD64_)
        char *pStackTopPage = (char*)(threadContext.Rsp & ~(cbPage - 1));
#endif

        if (pFaultPage == pStackTopPage || pFaultPage == (pStackTopPage - cbPage))
        {
            // The easy case is when the AV occurred in the same or adjacent page as the stack pointer.
            fIsStackOverflow = true;
        }
        else if (pFaultPage < pStackTopPage)
        {
            // Calculate the address of the page immediately following the fault and check that it
            // lies in the same VM region as the stack pointer.
            vm_address_t vm_address;
            vm_size_t vm_size;
            vm_region_flavor_t vm_flavor;
            mach_msg_type_number_t infoCnt;
#ifdef BIT64
            vm_region_basic_info_data_64_t info;
            infoCnt = VM_REGION_BASIC_INFO_COUNT_64;
            vm_flavor = VM_REGION_BASIC_INFO_64;
#else
            vm_region_basic_info_data_t info;
            infoCnt = VM_REGION_BASIC_INFO_COUNT;
            vm_flavor = VM_REGION_BASIC_INFO;
#endif
            mach_port_t object_name;

            vm_address = (vm_address_t)(pFaultPage + cbPage);

#ifdef BIT64
            machret = vm_region_64(
#else
            machret = vm_region(
#endif
                                mach_task_self(),
                                &vm_address,
                                &vm_size,
                                vm_flavor,
                                (vm_region_info_t)&info,
                                &infoCnt,
                                &object_name);
#ifdef _X86_
            CHECK_MACH("vm_region", machret);
#elif defined(_AMD64_)
            CHECK_MACH("vm_region_64", machret);
#endif

            // If vm_region updated the address we gave it then that address was not part of a region at all
            // (and so this cannot be an SO). Otherwise check that the ESP lies in the region returned.
            char *pRegionStart = (char*)vm_address;
            char *pRegionEnd = (char*)vm_address + vm_size;
            if (pRegionStart == (pFaultPage + cbPage) && pStackTopPage < pRegionEnd)
                fIsStackOverflow = true;
        }

#if defined(_AMD64_)
        if (!fIsStackOverflow)
        {
            // Check if we can read pointer sizeD bytes below the target thread's stack pointer.
            // If we are unable to, then it implies we have run into SO.
            void **targetSP = (void **)threadState.uts.ts64.__rsp;
            vm_address_t targetAddr = (mach_vm_address_t)(targetSP);
            targetAddr -= sizeof(void *);
            vm_size_t vm_size = sizeof(void *);
            char arr[8];
            vm_size_t data_count = 8;
            machret = vm_read_overwrite(mach_task_self(), targetAddr, vm_size, (pointer_t)arr, &data_count);
            if (machret == KERN_INVALID_ADDRESS)
            {
                fIsStackOverflow = true;
            }
        }
#endif // _AMD64_

        if (fIsStackOverflow)
        {
            // We have a stack overflow. Abort the process immediately. It would be nice to let the VM do this
            // but the Windows mechanism (where a stack overflow SEH exception is delivered on the faulting
            // thread) will not work most of the time since non-Windows OSs don't keep a reserve stack
            // extension allocated for this purpose.

            // TODO: Once our event reporting story is further along we probably want to report something
            // here. If our runtime policy for SO ever changes (the most likely candidate being "unload
            // appdomain on SO) then we'll have to do something more complex here, probably involving a
            // handshake with the runtime in order to report the SO without attempting to extend the faulting
            // thread's stack any further. Note that we cannot call most PAL functions from the context of
            // this thread since we're not a PAL thread.

            write(STDERR_FILENO, StackOverflowMessage, sizeof(StackOverflowMessage) - 1);
            abort();
        }
    }
#else // (_X86_ || _AMD64_) && __APPLE__
#error Platform not supported for correct stack overflow handling
#endif // (_X86_ || _AMD64_) && __APPLE__
#endif // CORECLR && _X86_

#if defined(_X86_)
    _ASSERTE((threadStateFlavor == x86_THREAD_STATE32) || ((threadStateFlavor == x86_THREAD_STATE) && (threadState.tsh.flavor == x86_THREAD_STATE32)));

    // If we're in single step mode, disable it since we're going to call PAL_DispatchException
    if (exceptionRecord.ExceptionCode == EXCEPTION_SINGLE_STEP)
    {
        threadState.uts.ts32.eflags &= ~EFL_TF;
    }

    exceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL; 
    exceptionRecord.ExceptionRecord = NULL;
    exceptionRecord.ExceptionAddress = (void *)threadContext.Eip;

    void **FramePointer = (void **)threadState.uts.ts32.esp;

    *--FramePointer = (void *)((ULONG_PTR)threadState.uts.ts32.eip);

    // Construct a stack frame for a pretend activation of the function
    // PAL_DispatchExceptionWrapper that serves only to make the stack
    // correctly unwindable by the system exception unwinder.
    // PAL_DispatchExceptionWrapper has an ebp frame, its local variables
    // are the context and exception record, and it has just "called"
    // PAL_DispatchException.
    *--FramePointer = (void *)threadState.uts.ts32.ebp;
    threadState.uts.ts32.ebp = (unsigned)FramePointer;

    // Put the context on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(CONTEXT));
    // Make sure it's aligned - CONTEXT has 8-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 8));
    CONTEXT *pContext = (CONTEXT *)FramePointer;
    *pContext = threadContext;

    // Put the exception record on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(EXCEPTION_RECORD));
    EXCEPTION_RECORD *pExceptionRecord = (EXCEPTION_RECORD *)FramePointer;
    *pExceptionRecord = exceptionRecord;

    // Put the exception message on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(MachMessage));
    MachMessage *pMessage = (MachMessage *)FramePointer;
    pMessage->InitializeFrom(message);

    // Push arguments to PAL_DispatchException
    FramePointer = (void **)((ULONG_PTR)FramePointer - 3 * sizeof(void *));

    // Make sure it's aligned - ABI requires 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    FramePointer[0] = pContext;
    FramePointer[1] = pExceptionRecord;
    FramePointer[2] = pMessage;

    // Place the return address to right after the fake call in PAL_DispatchExceptionWrapper
    FramePointer[-1] = (void *)((ULONG_PTR)PAL_DispatchExceptionWrapper + PAL_DispatchExceptionReturnOffset);

    // Make the instruction register point to DispatchException
    threadState.uts.ts32.eip = (unsigned)PAL_DispatchException;
    threadState.uts.ts32.esp = (unsigned)&FramePointer[-1]; // skip return address
#elif defined(_AMD64_)
    _ASSERTE((threadStateFlavor == x86_THREAD_STATE64) || ((threadStateFlavor == x86_THREAD_STATE) && (threadState.tsh.flavor == x86_THREAD_STATE64)));

    // If we're in single step mode, disable it since we're going to call PAL_DispatchException
    if (exceptionRecord.ExceptionCode == EXCEPTION_SINGLE_STEP)
    {
        threadState.uts.ts64.__rflags &= ~EFL_TF;
    }

    exceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL; 
    exceptionRecord.ExceptionRecord = NULL;
    exceptionRecord.ExceptionAddress = (void *)threadContext.Rip;

    void **FramePointer = (void **)threadState.uts.ts64.__rsp;

    *--FramePointer = (void *)((ULONG_PTR)threadState.uts.ts64.__rip);

    // Construct a stack frame for a pretend activation of the function
    // PAL_DispatchExceptionWrapper that serves only to make the stack
    // correctly unwindable by the system exception unwinder.
    // PAL_DispatchExceptionWrapper has an ebp frame, its local variables
    // are the context and exception record, and it has just "called"
    // PAL_DispatchException.
    *--FramePointer = (void *)threadState.uts.ts64.__rbp;
    threadState.uts.ts64.__rbp = (SIZE_T)FramePointer;

    // Put the context on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(CONTEXT));
    // Make sure it's aligned - CONTEXT has 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    CONTEXT *pContext = (CONTEXT *)FramePointer;
    *pContext = threadContext;

    // Put the exception record on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(EXCEPTION_RECORD));
    EXCEPTION_RECORD *pExceptionRecord = (EXCEPTION_RECORD *)FramePointer;
    *pExceptionRecord = exceptionRecord;

    // Put the exception message on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(MachMessage));
    MachMessage *pMessage = (MachMessage *)FramePointer;
    pMessage->InitializeFrom(message);

    // Push arguments to PAL_DispatchException
    FramePointer = (void **)((ULONG_PTR)FramePointer - 3 * sizeof(void *));

    // Make sure it's aligned - ABI requires 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    FramePointer[0] = pContext;
    FramePointer[1] = pExceptionRecord;
    FramePointer[2] = pMessage;

    // Place the return address to right after the fake call in PAL_DispatchExceptionWrapper
    FramePointer[-1] = (void *)((ULONG_PTR)PAL_DispatchExceptionWrapper + PAL_DispatchExceptionReturnOffset);

    // Make the instruction register point to DispatchException
    threadState.uts.ts64.__rip = (SIZE_T)PAL_DispatchException;
    threadState.uts.ts64.__rsp = (SIZE_T)&FramePointer[-1]; // skip return address
#else
#error HijackFaultingThread not defined for this architecture
#endif

    // Now set the thread state for the faulting thread so that PAL_DispatchException executes next
    machret = thread_set_state(thread, threadStateFlavor, (thread_state_t)&threadState, count);
    CHECK_MACH("thread_set_state", machret);
}

/*++
Function :
    SEHExceptionThread

    Entry point for the thread that will listen for exception in any other thread.

#ifdef FEATURE_PAL_SXS
    NOTE: This thread is not a PAL thread, and it must not be one.  If it was,
    exceptions on this thread would be delivered to the port this thread itself
    is listening on.

    In particular, if another thread overflows its stack, the exception handling
    thread receives a message.  It will try to create a PAL_DispatchException
    frame on the faulting thread, which will likely fault.  If the exception
    processing thread is not a PAL thread, the process gets terminated with a
    bus error; if the exception processing thread was a PAL thread, we would see
    a hang (since no thread is listening for the exception message that gets sent).
    Of the two ugly behaviors, the bus error is definitely favorable.

    This means: no printf, no TRACE, no PAL allocation, no ExitProcess,
    no LastError in this function and its helpers.  To report fatal failure,
    use NONPAL_RETAIL_ASSERT.
#endif // FEATURE_PAL_SXS

Parameters :
    void *args - not used

Return value :
   Never returns
--*/
void *SEHExceptionThread(void *args)
{
    MachMessage sMessage;
    MachMessage sReplyOrForward;

    kern_return_t machret;
    thread_act_t hThread;

    // Loop processing incoming messages forever.
    while (true)
    {
        // Receive the next message.
        sMessage.Receive(s_ExceptionPort);

        NONPAL_TRACE("Received message %s from %08x to %08x\n",
            sMessage.GetMessageTypeName(), 
            sMessage.GetRemotePort(), 
            sMessage.GetLocalPort());

        if (sMessage.IsSetThreadRequest())
        {
            // Handle a request to set the thread context for the specified target thread.
            CONTEXT sContext;
            hThread = sMessage.GetThreadContext(&sContext);

            while (true)
            {
                machret = thread_suspend(hThread);
                CHECK_MACH("thread_suspend", machret);

                // Ensure that if the thread was running in the kernel, the kernel operation
                // is safely aborted so that it can be restarted later.
                machret = thread_abort_safely(hThread);
                if (machret == KERN_SUCCESS)
                {
                    break;
                }

                // The thread was running in the kernel executing a non-atomic operation
                // that cannot be restarted, so we need to resume the thread and retry
                machret = thread_resume(hThread);
                CHECK_MACH("thread_resume", machret);
            }
            
            machret = CONTEXT_SetThreadContextOnPort(hThread, &sContext);
            CHECK_MACH("CONTEXT_SetThreadContextOnPort", machret);

            machret = thread_resume(hThread);
            CHECK_MACH("thread_resume", machret);
        }
        else if (sMessage.IsExceptionNotification())
        {
            // This is a notification of an exception occurring on another thread.
            hThread = sMessage.GetThread();

            NONPAL_TRACE("Notification is for exception %u (%s) on thread port %08x flavor %d\n",
                sMessage.GetException(),
                GetExceptionString(sMessage.GetException()),
                hThread,
                sMessage.GetThreadStateFlavor());

            HijackFaultingThread(hThread, mach_task_self(), sMessage);

            // Send the result of handling the exception back in a reply.
            sReplyOrForward.ReplyToNotification(&sMessage, KERN_SUCCESS);
        }
        else if (sMessage.IsExceptionReply())
        {
            NONPAL_TRACE("Exception reply - ignored\n");
        }
        else
        {
            NONPAL_RETAIL_ASSERT("Unknown message type: %u", sMessage.GetMessageType());
        }
    }
}

void ForwardMachException(CPalThread *pThread, MachMessage *pMessage)
{
    thread_act_t hThread = pThread->GetMachPortSelf();
    MachMessage sReplyOrForward;

    // Locate the record of previously installed handlers that the target thread keeps.
    CorUnix::CThreadMachExceptionHandlers *pHandlers = pThread->GetSavedMachHandlers();

    // Check whether there's even a handler for the particular exception we've been handed.
    CorUnix::MachExceptionHandler sHandler;
    if (pHandlers->GetHandler(pMessage->GetException(), &sHandler))
    {
        NONPAL_TRACE("Forward request to port %08x\n", sHandler.m_handler);

        // Forward the notification
        sReplyOrForward.ForwardNotification(&sHandler, pMessage);

        // Spin wait until this thread is hijacked or the process is aborted.
        while (TRUE)
        {
            sched_yield();
        }
    }
    else
    {
        // There's no previous handler to forward this notification to.
        NONPAL_TRACE("Unhandled exception and no chain-back - aborting process\n");
        PROCAbort();
    }
}

PAL_NORETURN 
void MachSetThreadContext(CONTEXT *lpContext)
{
    // We need to send a message to the worker thread so that it can set our thread context
    // It is responsible for deallocating the thread port.
    MachMessage sRequest;
    sRequest.SendSetThread(s_ExceptionPort, mach_thread_self(), lpContext);

    // Make sure we don't do anything
    while (TRUE)
    {
        sched_yield();
    }
}

/*++
Function :
    SEHInitializeMachExceptions 

    Initialize all SEH-related stuff related to mach exceptions

    (no parameters)

Return value :
    TRUE  if SEH support initialization succeeded
    FALSE otherwise
--*/
BOOL SEHInitializeMachExceptions (void)
{
    kern_return_t MachRet;
    int CreateRet;
    pthread_t exception_thread;

    // Allocate a mach port that will listen in on exceptions
    MachRet = mach_port_allocate(mach_task_self(),
                                 MACH_PORT_RIGHT_RECEIVE,
                                 &s_ExceptionPort);

    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_allocate failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    // Insert the send right into the task
    MachRet = mach_port_insert_right(mach_task_self(),
                                     s_ExceptionPort,
                                     s_ExceptionPort,
                                     MACH_MSG_TYPE_MAKE_SEND);

    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_insert_right failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    // Create the thread that will listen to the exception for all threads
    CreateRet = pthread_create(&exception_thread, NULL, SEHExceptionThread, NULL);

    if ( CreateRet != 0 )
    {
        ERROR("pthread_create failed, error is %d (%s)\n", CreateRet, strerror(CreateRet));
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return FALSE;
    }

#ifndef FEATURE_PAL_SXS
    if (!SEHEnableMachExceptions())
    {
        return FALSE;
    }
#endif // !FEATURE_PAL_SXS

    // Tell the system to ignore SIGPIPE signals rather than use the default
    // behavior of terminating the process. Ignoring SIGPIPE will cause
    // calls that would otherwise raise that signal to return EPIPE instead.
    // The PAL expects EPIPE from those functions and won't handle a
    // SIGPIPE signal.
    signal(SIGPIPE, SIG_IGN);

    // We're done
    return TRUE;
}

/*++
Function :
    MachExceptionInitializeDebug 

    Initialize the mach exception handlers necessary for a managed debugger
    to work

Return value :
    None
--*/
void MachExceptionInitializeDebug(void)
{
    if (s_DebugInitialized == FALSE)
    {
#ifndef FEATURE_PAL_SXS
        kern_return_t MachRet;
        MachRet = task_set_exception_ports(mach_task_self(),
                                           PAL_EXC_DEBUGGING_MASK,
                                           s_ExceptionPort,
                                           EXCEPTION_DEFAULT,
                                           MACHINE_THREAD_STATE);
        if (MachRet != KERN_SUCCESS)
        {
            ASSERT("task_set_exception_ports failed: %d\n", MachRet);
            TerminateProcess(GetCurrentProcess(), (UINT)(-1));
        }
#endif // !FEATURE_PAL_SXS
        s_DebugInitialized = TRUE;
    }
}

/*++
Function :
    SEHCleanupExceptionPort

    Restore default exception port handler

    (no parameters, no return value)
    
Note :
During PAL_Terminate, we reach a point where SEH isn't possible any more
(handle manager is off, etc). Past that point, we can't avoid crashing on
an exception.
--*/
void SEHCleanupExceptionPort(void)
{
    TRACE("Restoring default exception ports\n");
#ifndef FEATURE_PAL_SXS
    SEHDisableMachExceptions();
#endif // !FEATURE_PAL_SXS
    s_DebugInitialized = FALSE;
}

extern "C" void ActivationHandler(CONTEXT* context)
{
    if (g_activationFunction != NULL)
    {
        g_activationFunction(context);
    }

    RtlRestoreContext(context, NULL);
    DebugBreak();
}

extern "C" void ActivationHandlerWrapper();
extern "C" int ActivationHandlerReturnOffset;

PAL_ERROR InjectActivationInternal(CPalThread* pThread)
{
    PAL_ERROR palError;

    mach_port_t threadPort = pThread->GetMachPortSelf();
    kern_return_t MachRet = thread_suspend(threadPort);
    palError = (MachRet == KERN_SUCCESS) ? NO_ERROR : ERROR_GEN_FAILURE;

    if (palError == NO_ERROR)
    {
        mach_msg_type_number_t count;

        x86_exception_state64_t ExceptionState;
        count = x86_EXCEPTION_STATE64_COUNT;
        MachRet = thread_get_state(threadPort,
                                   x86_EXCEPTION_STATE64,
                                   (thread_state_t)&ExceptionState,
                                   &count);
        _ASSERT_MSG(MachRet == KERN_SUCCESS, "thread_get_state for x86_EXCEPTION_STATE64\n");

        // Inject the activation only if the thread doesn't have a pending hardware exception
        static const int MaxHardwareExceptionVector = 31;
        if (ExceptionState.__trapno > MaxHardwareExceptionVector)
        {
            x86_thread_state64_t ThreadState;
            count = x86_THREAD_STATE64_COUNT;
            MachRet = thread_get_state(threadPort,
                                       x86_THREAD_STATE64,
                                       (thread_state_t)&ThreadState,
                                       &count);
            _ASSERT_MSG(MachRet == KERN_SUCCESS, "thread_get_state for x86_THREAD_STATE64\n");

            if ((g_safeActivationCheckFunction != NULL) && g_safeActivationCheckFunction(ThreadState.__rip))
            {
                // TODO: it would be nice to preserve the red zone in case a jitter would want to use it
                // Do we really care about unwinding through the wrapper?
                size_t* sp = (size_t*)ThreadState.__rsp;
                *(--sp) = ThreadState.__rip;
                *(--sp) = ThreadState.__rbp;
                size_t rbpAddress = (size_t)sp;
                size_t contextAddress = (((size_t)sp) - sizeof(CONTEXT)) & ~15;
                size_t returnAddressAddress = contextAddress - sizeof(size_t);
                *(size_t*)(returnAddressAddress) =  ActivationHandlerReturnOffset + (size_t)ActivationHandlerWrapper;

                // Fill in the context in the helper frame with the full context of the suspended thread.
                // The ActivationHandler will use the context to resume the execution of the thread
                // after the activation function returns.
                CONTEXT *pContext = (CONTEXT *)contextAddress;
                pContext->ContextFlags = CONTEXT_FULL | CONTEXT_SEGMENTS;
                MachRet = CONTEXT_GetThreadContextFromPort(threadPort, pContext);
                _ASSERT_MSG(MachRet == KERN_SUCCESS, "CONTEXT_GetThreadContextFromPort\n");

                // Make the instruction register point to ActivationHandler
                ThreadState.__rip = (size_t)ActivationHandler;
                ThreadState.__rsp = returnAddressAddress;
                ThreadState.__rbp = rbpAddress;
                ThreadState.__rdi = contextAddress;

                MachRet = thread_set_state(threadPort,
                                           x86_THREAD_STATE64,
                                           (thread_state_t)&ThreadState,
                                           count);
                _ASSERT_MSG(MachRet == KERN_SUCCESS, "thread_set_state\n");
            }
        }

        MachRet = thread_resume(threadPort);
        palError = (MachRet == ERROR_SUCCESS) ? NO_ERROR : ERROR_GEN_FAILURE;
    }
    else
    {
        printf("Suspension failed with error 0x%x\n", palError);
    }

    return palError;
}

#endif // HAVE_MACH_EXCEPTIONS
