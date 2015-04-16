//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

#if !DISABLE_EXCEPTIONS
// The port we use to handle exceptions and to set the thread context
mach_port_t s_ExceptionPort;

// Since we now forward some exception notifications to previous handlers we need to be able to wait on more
// than one port at a time (to intercept and forward replies from these forwarded notifications). Therefore we
// listen on a port set rather than a single port (the port above is one member, the initial one, of this port
// set).
static mach_port_t s_ExceptionPortSet;

// We also allow hosts to reset the exception handler on a thread so that can raise the chance their
// exceptions aren't handled or swallowed by another component (perhaps one that doesn't chain back to
// previous handlers like we do). In order to avoid stomping any previous handler we recorded when first
// setting up the thread we actually keep two sets of chain-back information: one for the bottom of the thread
// (chaining back to code that set up a handler before we did) and one for the top (chaining back to code that
// set up a handler after we did). We also allocate a second exception port for this latter case (which we put
// in the port set above so we can wait on both simultaneously). This allows us to distinguish where the
// exception was raised from since it's possible, for example, for an exception forwarded by us near the top
// of the stack to re-enter us for bottom registration and we want to chain back to the bottom handler in
// those situations rather than falsely claiming a handler cycle. This way we should be able to support a
// number of different handler scenarios with close to perfect semantics.
mach_port_t s_TopExceptionPort;

static BOOL s_DebugInitialized = FALSE;

// To better support side-by-side use of the process we forward any exceptions we don't understand to the
// previous handler registered for the exception (if there is one). We need to keep track of all the
// notifications we've forwarded so we can forward the eventual reply back to the original sender (the reply
// doesn't contain enough context for us to do this without this additional context). The structure below
// holds all the context necessary for a single forwarded notification and is held on a singly linked list by
// the exception thread.
struct ForwardedNotification
{
    ForwardedNotification      *m_pNext;                // Pointer to the next forward record or NULL
    mach_port_t                 m_hListenReplyPort;     // We listen to this port for a reply
    mach_port_t                 m_hForwardReplyPort;    // Forward the reply to this port
    thread_act_t                m_hTargetThread;        // Thread which caused the exception
    MachMessage::MessageType    m_eNotificationType;    // Message type of the original notification
    thread_state_flavor_t       m_eNotificationFlavor;  // Thread state flavor used in the original notification
    bool                        m_fTopException;        // Was this notification delivered to the "top" handler?
};

#endif // !DISABLE_EXCEPTIONS

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
    MachException_Default           = 2,
};

static exception_mask_t GetExceptionMask()
{
    static MachExceptionMode exMode = MachException_Uninitialized;

    if (exMode == MachException_Uninitialized)
    {
        const char * exceptionSettings = getenv(PAL_MACH_EXCEPTION_MODE);
        exMode = exceptionSettings
            ? (MachExceptionMode)atoi(exceptionSettings) : MachException_Default;
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
            PAL_DisplayDialogFormatted("NON-PAL RETAIL ASSERT", __VA_ARGS__); \
        }                                                               \
        DBG_DebugBreak();                                               \
        abort();                                                        \
    }

#define CHECK_MACH(function, MachRet)                                   \
    if (MachRet != KERN_SUCCESS)                                        \
    {                                                                   \
        NONPAL_RETAIL_ASSERT(function " failed: %08X: %s\n", MachRet, mach_error_string(MachRet)); \
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
#if !DISABLE_EXCEPTIONS
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

        // We could be called in two situations:
        //   1) The CoreCLR has seen this thread for the first time.
        //   2) The host has called ICLRRuntimeHost2::RegisterMacEHPort().
        // We store a set of previous handlers and register an exception port that is unique to both (to help
        // us get the correct chain-back semantics in as many scenarios as possible). The following call tells
        // us which we should do.
        mach_port_t hExceptionPort;
        CThreadMachExceptionHandlerNode *pSavedHandlers =
            m_sMachExceptionHandlers.GetNodeForInitialization(&hExceptionPort);
        
        NONPAL_TRACE("Enabling %s handlers for thread %08X\n",
                     hExceptionPort == s_TopExceptionPort ? "top" : "bottom",
                     pthread_mach_thread_np(GetPThreadSelf()));

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
                                              hExceptionPort,
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
            if (rgHandlers[i] == s_ExceptionPort || rgHandlers[i] == s_TopExceptionPort)
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
                        if (pSavedHandlers->m_handlers[k] == s_ExceptionPort ||
                            pSavedHandlers->m_handlers[k] == s_TopExceptionPort)
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
#endif // !DISABLE_EXCEPTIONS
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
#if !DISABLE_EXCEPTIONS
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
#endif // !DISABLE_EXCEPTIONS
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
#if !DISABLE_EXCEPTIONS
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
#endif // !DISABLE_EXCEPTIONS
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
#if !DISABLE_EXCEPTIONS
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
#endif // !DISABLE_EXCEPTIONS
}

#endif // FEATURE_PAL_SXS

#if !DISABLE_EXCEPTIONS
#if !defined(_AMD64_)
void PAL_DispatchException(PCONTEXT pContext, PEXCEPTION_RECORD pExRecord)
#else // defined(_AMD64_)

// Since catch_exception_raise pushed the context and exception record on the stack, we need to adjust the signature
// of PAL_DispatchException such that the corresponding arguments are considered to be on the stack per GCC64 calling
// convention rules. Hence, the first 6 dummy arguments (corresponding to RDI, RSI, RDX,RCX, R8, R9).
void PAL_DispatchException(DWORD64 dwRDI, DWORD64 dwRSI, DWORD64 dwRDX, DWORD64 dwRCX, DWORD64 dwR8, DWORD64 dwR9, PCONTEXT pContext, PEXCEPTION_RECORD pExRecord)
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

    if (g_hardwareExceptionHandler != NULL)
    {
        PAL_SEHException exception(pExRecord, pContext);

        g_hardwareExceptionHandler(&exception);

        ASSERT("HandleHardwareException has returned, it should not.\n");
    }
    else
    {
        ASSERT("Unhandled hardware exception\n");
    }

    ExitProcess(pExRecord->ExceptionCode);
}

#if defined(_X86_) || defined(_AMD64_)
extern "C" void PAL_DispatchExceptionWrapper();
#endif // _X86_ || _AMD64_

/*++
Function :
    exception_from_trap_code

    map a Trap code to a SEH exception code

Parameters :
    exception_type_t exception          : Trap code to map
    exception_data_t code               : Sub code
    mach_msg_type_number_t code_count   : Size of sub code
*/
static DWORD exception_from_trap_code(
   exception_type_t exception,          // [in] The type of the exception
   MACH_EH_TYPE(exception_data_t) code, // [in] A machine dependent array indicating a particular instance of exception
   mach_msg_type_number_t code_count,   // [in] The size of the buffer (in natural-sized units)
   EXCEPTION_RECORD *pExceptionRecord   // [out] Used to return exception parameters
#if defined(_AMD64_)
   , mach_port_t thread
#endif // defined(_AMD64_)
   )
{
    pExceptionRecord->NumberParameters = 0;

    switch(exception)
    {
    // Could not access memory. subcode contains the bad memory address. 
    case EXC_BAD_ACCESS:
        if (code_count != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected sub code");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        }
        pExceptionRecord->NumberParameters = 2;
        pExceptionRecord->ExceptionInformation[0] = 0;
        pExceptionRecord->ExceptionInformation[1] = code[1];

#if defined(_AMD64_)
        {
            // For X64, get exception state from the thread that contains the address (and not IP) using which
            // the fault happened.
            x86_exception_state64_t ThreadExceptionState;
            const thread_state_flavor_t ThreadExceptionStateFlavor = x86_EXCEPTION_STATE64;
            unsigned int ehStateCount = sizeof(ThreadExceptionState)/sizeof(unsigned);
            kern_return_t MachRet = thread_get_state(thread,
                                   ThreadExceptionStateFlavor,
                                   (thread_state_t)&ThreadExceptionState,
                                   &ehStateCount);
            CHECK_MACH("thread_get_state", MachRet);
            ULONG64 faultAddr = ThreadExceptionState.__faultvaddr;
            pExceptionRecord->ExceptionInformation[1] = faultAddr;
        }
#endif // _AMD64_

        return EXCEPTION_ACCESS_VIOLATION; 

    // Instruction failed. Illegal or undefined instruction or operand. 
    case EXC_BAD_INSTRUCTION :
        return EXCEPTION_ILLEGAL_INSTRUCTION; 

    // Arithmetic exception; exact nature of exception is in subcode field. 
    case EXC_ARITHMETIC:
        if (code_count != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected sub code");
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        }
        switch (*(unsigned *)code)
        {
#if defined(_X86_) || defined(_AMD64_)
        case EXC_I386_DIV:
            return EXCEPTION_INT_DIVIDE_BY_ZERO;
        case EXC_I386_INTO:
            return EXCEPTION_INT_OVERFLOW;
        case EXC_I386_EXTOVR:
            return EXCEPTION_FLT_OVERFLOW;
        case EXC_I386_BOUND:
            return EXCEPTION_ARRAY_BOUNDS_EXCEEDED;
#else
#error Trap code to exception mapping not defined for this architecture
#endif
        default:
            return EXCEPTION_ILLEGAL_INSTRUCTION; 
        }
        break;

    case EXC_SOFTWARE:
#if defined(_X86_) || defined(_AMD64_)
        return EXCEPTION_ILLEGAL_INSTRUCTION;
#else
#error Trap code to exception mapping not defined for this architecture
#endif

    // Trace, breakpoint, etc. Details in subcode field. 
    case EXC_BREAKPOINT:
#if defined(_X86_) || defined(_AMD64_)
        if (*(unsigned *)code == EXC_I386_SGL)
        {
            return EXCEPTION_SINGLE_STEP;
        }
        else if (*(unsigned *)code == EXC_I386_BPT)
        {
            return EXCEPTION_BREAKPOINT;
        }
#else
#error Trap code to exception mapping not defined for this architecture
#endif
        else
        {
            WARN("unexpected code %d for EXC_BREAKPOINT");
            return EXCEPTION_BREAKPOINT;
        }
        break;


    // System call requested. Details in subcode field. 
    case EXC_SYSCALL:
        return EXCEPTION_ILLEGAL_INSTRUCTION; 
        break;

    // System call with a number in the Mach call range requested. Details in subcode field. 
    case EXC_MACH_SYSCALL:
        return EXCEPTION_ILLEGAL_INSTRUCTION; 
        break;

    default:
        ASSERT("Got unknown trap code %d\n", exception);
        break;
    }
    return EXCEPTION_ILLEGAL_INSTRUCTION;
}

/*++
Function :
    catch_exception_raise

    called from SEHExceptionThread and does the exception processing

Return value :
   KERN_SUCCESS if the error is handled
   MIG_DESTROY_REQUEST if the error was not handled
--*/

static
kern_return_t
catch_exception_raise(
   mach_port_t exception_port,          // [in] Mach Port that is listening to the exception
   mach_port_t thread,                  // [in] thread the exception happened on
   mach_port_t task,                    // [in] task the exception happened on
   exception_type_t exception,          // [in] The type of the exception
   MACH_EH_TYPE(exception_data_t) code, // [in] A machine dependent array indicating a particular instance of exception
   mach_msg_type_number_t code_count)   // [in] The size of the buffer (in natural-sized units). 
{
    kern_return_t MachRet;
#if defined(_X86_)
    x86_thread_state32_t ThreadState;
    const thread_state_flavor_t ThreadStateFlavor = x86_THREAD_STATE32;
#elif defined(_AMD64_)
    x86_thread_state64_t ThreadState;
    const thread_state_flavor_t ThreadStateFlavor = x86_THREAD_STATE64;
#else
#error Thread state not defined for this architecture
#endif
    unsigned int count = sizeof(ThreadState)/sizeof(unsigned);
    CONTEXT ThreadContext;
    EXCEPTION_RECORD ExceptionRecord;
    memset(&ExceptionRecord, 0, sizeof(ExceptionRecord));

    ExceptionRecord.ExceptionCode = exception_from_trap_code(exception, code, code_count, &ExceptionRecord
#if defined(_AMD64_)
        , thread
#endif // _AMD64_
        );
    
    ThreadContext.ContextFlags = CONTEXT_ALL;

    MachRet = CONTEXT_GetThreadContextFromPort(thread, &ThreadContext);
    CHECK_MACH("CONTEXT_GetThreadContextFromPort", MachRet);

    // We need to hijack the thread to point to PAL_DispatchException
    MachRet = thread_get_state(thread,
                               ThreadStateFlavor,
                               (thread_state_t)&ThreadState,
                               &count);
    CHECK_MACH("thread_get_state", MachRet);

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
    // they both lie in the same page or the SP is at a higher address than the AV but still reasonably close
    // (we'll define close below) then we'll consider the AV to be an SO. Note that we can't assume that SP
    // will be in the same page as the AV on an SO, even though we force GCC to generate stack probes on stack
    // extension (-fstack-check). That's because GCC currently generates the probe *before* altering SP.
    // Since a given stack extension can involve multiple pages and GCC generates all the required probes
    // before updating SP in a single operation, the faulting probe can be at an address that is far removed
    // from the thread's current value of SP.

    // To work around this we'll first bound the definition of "close" to 512KB. This is the current size of
    // pthread stacks by default. While it's true that this value can be altered for a given thread (and the
    // main thread for that matter usually starts with an 8MB stack) I think it is reasonable to assume that a
    // single stack frame, alloca etc. should never be extending the stack this much in one go.

    // If we pass this check then we'll confirm it (in the case where the AV and SP aren't in the same or
    // adjacent pages) by checking that the first page following the faulting address belongs in the same VM
    // region as the current value of SP. Since all pages in a VM region have the same attributes this check
    // eliminates the possibility that there's another guard page in the range between the fault and the SP,
    // effectively establishing that the AV occurred in the guard page associated with the stack associated
    // with the SP.

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
    if (ExceptionRecord.ExceptionCode == EXCEPTION_ACCESS_VIOLATION)
    {
        // Assume this AV isn't an SO to begin with.
        bool fIsStackOverflow = false;

        // Calculate the page base addresses for the fault and the faulting thread's SP.
        int cbPage = getpagesize();
        char *pFaultPage = (char*)(ExceptionRecord.ExceptionInformation[1] & ~(cbPage - 1));
#ifdef _X86_
        char *pStackTopPage = (char*)(ThreadContext.Esp & ~(cbPage - 1));
#elif defined(_AMD64_)
        char *pStackTopPage = (char*)(ThreadContext.Rsp & ~(cbPage - 1));
#endif

        if (pFaultPage == pStackTopPage || pFaultPage == (pStackTopPage - cbPage))
        {
            // The easy case is when the AV occurred in the same or adjacent page as the stack pointer.
            fIsStackOverflow = true;
        }
        else if (pFaultPage < pStackTopPage && (pStackTopPage - pFaultPage) < (512 * 1024))
        {
            // If the two addresses look fairly close together (the size of the average pthread stack) we'll
            // dig deeper. Calculate the address of the page immediately following the fault and check that it
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
            MachRet = vm_region_64(
#else
            MachRet = vm_region(
#endif
                                mach_task_self(),
                                &vm_address,
                                &vm_size,
                                vm_flavor,
                                (vm_region_info_t)&info,
                                &infoCnt,
                                &object_name);
#ifdef _X86_
            CHECK_MACH("vm_region", MachRet);
#elif defined(_AMD64_)
            CHECK_MACH("vm_region_64", MachRet);
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
            void **targetSP = (void **)ThreadState.__rsp;
            vm_address_t targetAddr = (mach_vm_address_t)(targetSP);
            targetAddr -= sizeof(void *);
            vm_size_t vm_size = sizeof(void *);
            char arr[8];
            vm_size_t data_count = 8;
            MachRet = vm_read_overwrite(mach_task_self(), targetAddr, vm_size, (pointer_t)arr, &data_count);
            if (MachRet == KERN_INVALID_ADDRESS)
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

            abort();
        }
    }
#else // (_X86_ || _AMD64_) && __APPLE__
#error Platform not supported for correct stack overflow handling
#endif // (_X86_ || _AMD64_) && __APPLE__
#endif // CORECLR && _X86_

#if defined(_X86_)
    // If we're in single step mode, disable it since we're going to call PAL_DispatchException
    if (ExceptionRecord.ExceptionCode == EXCEPTION_SINGLE_STEP)
    {
        ThreadState.eflags &= ~EFL_TF;
    }

    ExceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL; 
    ExceptionRecord.ExceptionRecord = NULL;
    ExceptionRecord.ExceptionAddress = (void *)ThreadContext.Eip;

    void **FramePointer = (void **)ThreadState.esp;

    // ThreadState.eip points to the instruction that caused the fault.
    // In the frame we're constructing, we'll pretend PAL_DispatchExceptionWrapper
    // was called by eip+1 instead of eip to work around a quirk in the
    // stack unwinding logic of the gcc runtime.
    //
    // The quirk is the following.  The stack unwinder of the gcc runtime
    // has been designed for unwinding stack frames created by ordinary
    // function calls, so it expects that every return address on the
    // stack points to an instruction following a call instruction.  If
    // the target of a given call is a function declared to never return,
    // gcc can generate code where this call is the very last instruction
    // in a function, in which case the return address points to the first
    // instruction of the function that happens to be laid out immediately
    // after the calling function in the binary.  To ensure it gets the
    // exception handling table for the right function, the stack unwinder
    // therefore always subtracts one from any return address.  (Note that
    // gdb isn't as smart in this case and actually shows the wrong symbol
    // in a backtrace!)  Similarly, gcc's personality routine uses eip-1
    // when looking up which try block we're in, since the call could have
    // been the last instruction in the try block.
    //
    // This logic breaks down for the stack frame we are constructing here
    // if our faulting instruction is the first instruction in a try block,
    // since eip-1 would point outside the range of instructions that make
    // up said block.  We fix this by storing eip+1 as the return address.
    //
    // Do we expect this to cause any problems to other code?  No - since
    // we never actually return from PAL_DispatchException, this value
    // should only ever be examined by the exception unwinder and gcc's
    // personality routine.  At worst, this can cause some confusion to
    // developers looking at stack traces produced by tools such as gdb
    // and CrashReporter, who may see an eip that points in the middle of
    // an instruction.
    *--FramePointer = (void *)((ULONG_PTR)ThreadState.eip + 1);

    // Construct a stack frame for a pretend activation of the function
    // PAL_DispatchExceptionWrapper that serves only to make the stack
    // correctly unwindable by the system exception unwinder.
    // PAL_DispatchExceptionWrapper has an ebp frame, its local variables
    // are the context and exception record, and it has just "called"
    // PAL_DispatchException.
    *--FramePointer = (void *)ThreadState.ebp;
    ThreadState.ebp = (unsigned)FramePointer;

    // Put the context on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(CONTEXT));
    // Make sure it's aligned - CONTEXT has 8-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 8));
    CONTEXT *pContext = (CONTEXT *)FramePointer;
    *pContext = ThreadContext;

    // Put the exception record on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(EXCEPTION_RECORD));
    EXCEPTION_RECORD *pExceptionRecord = (EXCEPTION_RECORD *)FramePointer;
    *pExceptionRecord = ExceptionRecord;

    // Push arguments to PAL_DispatchException
    FramePointer = (void **)((ULONG_PTR)FramePointer - 2 * sizeof(void *));
    // Make sure it's aligned - ABI requires 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    FramePointer[0] = pContext;
    FramePointer[1] = pExceptionRecord;
    // Place the return address to right after the fake call in PAL_DispatchExceptionWrapper
    FramePointer[-1] = (void *)((ULONG_PTR)PAL_DispatchExceptionWrapper + 6);

    // Make the instruction register point to DispatchException
    ThreadState.eip = (unsigned)PAL_DispatchException;
    ThreadState.esp = (unsigned)&FramePointer[-1]; // skip return address
#elif defined(_AMD64_)
    // If we're in single step mode, disable it since we're going to call PAL_DispatchException
    if (ExceptionRecord.ExceptionCode == EXCEPTION_SINGLE_STEP)
    {
        ThreadState.__rflags &= ~EFL_TF;
    }

    ExceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL; 
    ExceptionRecord.ExceptionRecord = NULL;
    ExceptionRecord.ExceptionAddress = (void *)ThreadContext.Rip;

    void **FramePointer = (void **)ThreadState.__rsp;

    // ThreadState.eip points to the instruction that caused the fault.
    // In the frame we're constructing, we'll pretend PAL_DispatchExceptionWrapper
    // was called by eip+1 instead of eip to work around a quirk in the
    // stack unwinding logic of the gcc runtime.
    //
    // The quirk is the following.  The stack unwinder of the gcc runtime
    // has been designed for unwinding stack frames created by ordinary
    // function calls, so it expects that every return address on the
    // stack points to an instruction following a call instruction.  If
    // the target of a given call is a function declared to never return,
    // gcc can generate code where this call is the very last instruction
    // in a function, in which case the return address points to the first
    // instruction of the function that happens to be laid out immediately
    // after the calling function in the binary.  To ensure it gets the
    // exception handling table for the right function, the stack unwinder
    // therefore always subtracts one from any return address.  (Note that
    // gdb isn't as smart in this case and actually shows the wrong symbol
    // in a backtrace!)  Similarly, gcc's personality routine uses eip-1
    // when looking up which try block we're in, since the call could have
    // been the last instruction in the try block.
    //
    // This logic breaks down for the stack frame we are constructing here
    // if our faulting instruction is the first instruction in a try block,
    // since eip-1 would point outside the range of instructions that make
    // up said block.  We fix this by storing eip+1 as the return address.
    //
    // Do we expect this to cause any problems to other code?  No - since
    // we never actually return from PAL_DispatchException, this value
    // should only ever be examined by the exception unwinder and gcc's
    // personality routine.  At worst, this can cause some confusion to
    // developers looking at stack traces produced by tools such as gdb
    // and CrashReporter, who may see an eip that points in the middle of
    // an instruction.
    *--FramePointer = (void *)((ULONG_PTR)ThreadState.__rip + 1);

    // Construct a stack frame for a pretend activation of the function
    // PAL_DispatchExceptionWrapper that serves only to make the stack
    // correctly unwindable by the system exception unwinder.
    // PAL_DispatchExceptionWrapper has an ebp frame, its local variables
    // are the context and exception record, and it has just "called"
    // PAL_DispatchException.
    *--FramePointer = (void *)ThreadState.__rbp;
    ThreadState.__rbp = (SIZE_T)FramePointer;

    // Put the context on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(CONTEXT));
    // Make sure it's aligned - CONTEXT has 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    CONTEXT *pContext = (CONTEXT *)FramePointer;
    *pContext = ThreadContext;

    // Put the exception record on the stack
    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(EXCEPTION_RECORD));
    EXCEPTION_RECORD *pExceptionRecord = (EXCEPTION_RECORD *)FramePointer;
    *pExceptionRecord = ExceptionRecord;

    // Push arguments to PAL_DispatchException
    FramePointer = (void **)((ULONG_PTR)FramePointer - 2 * sizeof(void *));
    // Make sure it's aligned - ABI requires 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    FramePointer[0] = pContext;
    FramePointer[1] = pExceptionRecord;
    // Place the return address to right after the fake call in PAL_DispatchExceptionWrapper
    FramePointer[-1] = (void *)((ULONG_PTR)PAL_DispatchExceptionWrapper + 6);

    // Make the instruction register point to DispatchException
    ThreadState.__rip = (SIZE_T)PAL_DispatchException;
    ThreadState.__rsp = (SIZE_T)&FramePointer[-1]; // skip return address
#else
#error catch_exception_raise not defined for this architecture
#endif

    // Now set the thread state for the faulting thread so that PAL_DispatchException executes next
    MachRet = thread_set_state(thread,
                               ThreadStateFlavor,
                               (thread_state_t)&ThreadState,
                               count);
    CHECK_MACH("thread_set_state", MachRet);

    // We're done!
    return KERN_SUCCESS;
}

//----------------------------------------------------------------------

// Returns true if the given address resides within the memory range into which the CoreCLR binary is mapped.
bool IsWithinCoreCLR(void *pAddr)
{
    static void *s_pLowerBound = NULL;
    static void *s_pUpperBound = NULL;

    // Have we initialized our cached range values yet?
    if (s_pLowerBound == NULL || s_pUpperBound == NULL)
    {
        // No, initialize them now. We could be racing here (though currently only one thread calls this
        // function) but it doesn't matter since the data computed is invariant.

        // Get the mach_header for this instance of the CoreCLR by looking up out own function's address with
        // dladdr.
        Dl_info sInfo;
        int result = dladdr((void*)IsWithinCoreCLR, &sInfo);
        if (result == 0)
            NONPAL_RETAIL_ASSERT("dladdr() failed to locate IsWithinCoreCLR().");

        // Walk the segment commands in the Mach-O command array, looking for the segment with the highest
        // virtual address mapping. The end of this segment will correspond to the end of memory region that
        // the CoreCLR has been mapped to.
#if !defined(_AMD64_)
        mach_header *pHeader = (mach_header*)sInfo.dli_fbase;
        unsigned int addrMaxAddress = 0;
        unsigned int cbMaxSegment = 0;
        unsigned int iSegmentType = LC_SEGMENT;
#else // defined(_AMD64_)
        mach_header_64 *pHeader = (mach_header_64*)sInfo.dli_fbase;
        DWORD64 addrMaxAddress = 0;
        DWORD64 cbMaxSegment = 0;
        unsigned int iSegmentType = LC_SEGMENT_64;
#endif // !_AMD64_

        load_command *pCommand = (load_command*)(pHeader + 1);
        for (unsigned int i = 0; i < pHeader->ncmds; i++)
        {
            if (pCommand->cmd == iSegmentType)
            {
#if !defined(_AMD64_)
                segment_command *pSegment = (segment_command*)pCommand;
#else // defined(_AMD64_)
                segment_command_64 *pSegment = (segment_command_64*)pCommand;
#endif // !defined(_AMD64_)

                if (pSegment->vmaddr > addrMaxAddress)
                {
                    addrMaxAddress = pSegment->vmaddr;
                    cbMaxSegment = pSegment->vmsize;
                }
            }

            pCommand = (load_command*)((unsigned char*)pCommand + pCommand->cmdsize);
        }

        // The lower bound of the range is defined by the Mach-O image header.
        s_pLowerBound = pHeader;

        // The upper bound is the end address of the highest mapped segment in the image (the virtual address
        // we calculated in the previous step is relative to the base address of the image, i.e. the Mach-O
        // header).
        s_pUpperBound = (unsigned char*)pHeader + addrMaxAddress + cbMaxSegment;
    }

    // Perform the range check.
    return (pAddr >= s_pLowerBound) && (pAddr < s_pUpperBound);
}

#if !defined(_MSC_VER) && defined(_WIN64)
BOOL PALAPI PAL_IsIPInCoreCLR(IN PVOID address)
{
    BOOL fIsAddressWithinCoreCLR = FALSE;
    
    PERF_ENTRY(PAL_IsIPInCoreCLR);
    ENTRY("PAL_IsIPInCoreCLR (address=%p)\n", address);
    
    if (address != NULL)
    {
        if (IsWithinCoreCLR(address))
        {
            fIsAddressWithinCoreCLR = TRUE;
        }
    }
    
    LOGEXIT("PAL_IsIPInCoreCLR returns %d\n", fIsAddressWithinCoreCLR);
    PERF_EXIT(PAL_IsIPInCoreCLR);
    
    return fIsAddressWithinCoreCLR;
}

#endif //!defined(_MSC_VER) && defined(_WIN64)

extern malloc_zone_t *s_pExecutableHeap; // from heap.cpp in #ifdef CACHE_HEAP_ZONE

#pragma mark - 
#pragma mark malloc_zone utilities

// Ultimately, this may be more appropriate for heap.cpp, and rewritten in terms of
// a PAL heap and the win32 heap enumeration mechanism.

struct malloc_zone_owns_addr_context_t
{
    void *addr;
    bool fInUse;
};

static void malloc_zone_owns_addr_helper(task_t task, void *context, unsigned type_mask,
    vm_range_t *ranges, unsigned range_count)
{
    malloc_zone_owns_addr_context_t *our_context = (malloc_zone_owns_addr_context_t *)context;
    
    if (our_context->fInUse)
        return;
    
    vm_range_t *pRange = ranges;
    vm_range_t *pRangeMac = ranges + range_count;
    void *addr = our_context->addr;
    for (; pRange < pRangeMac; pRange++)
    {
        vm_range_t range = *pRange;
        
        if ((range.address <= (vm_address_t)addr) && ((range.address + range.size) > (vm_address_t)addr))
        {
            our_context->fInUse = true;
            return;
        }
    }
}

static bool malloc_zone_owns_addr(malloc_zone_t *zone, void * const addr)
{
    malloc_introspection_t *introspect = zone->introspect;
    malloc_zone_owns_addr_context_t context = { addr, false };
    
    (*introspect->enumerator)(mach_task_self(), &context, MALLOC_PTR_IN_USE_RANGE_TYPE, 
        (vm_address_t)zone, NULL /* memory_reader_t */, &malloc_zone_owns_addr_helper);
    
    return context.fInUse;
}

#pragma mark -

// Given an exception notification message determine whether the exception originated in code belonging to or
// generated by this instance of the CoreCLR. If true is returned the CoreCLR "owns" the faulting code and we
// should handle the exception. Otherwise the exception should be forwarded to another handler (if there is
// one) or the process terminated.
bool IsHandledException(MachMessage *pNotification)
{
    // Retrieve the state of faulting thread from the message (or directly, if the message doesn't contain
    // this information).
    thread_state_flavor_t sThreadStateFlavor;
#ifdef _X86_
    x86_thread_state32_t sThreadState;
    sThreadStateFlavor = x86_THREAD_STATE32;
#elif defined(_AMD64_)
    x86_thread_state64_t sThreadState;
    sThreadStateFlavor = x86_THREAD_STATE64;
#else
#error Unexpected architecture.
#endif

    pNotification->GetThreadState(sThreadStateFlavor, (thread_state_t)&sThreadState);

#ifdef _X86_
    void *ip = (void*)sThreadState.eip;
#elif defined(_AMD64_)
    void *ip = (void*)sThreadState.__rip;
#else
#error Unexpected architecture.
#endif

    // Was the exception raised from within the CoreCLR binary itself?
    if (IsWithinCoreCLR(ip))
    {
        NONPAL_TRACE("    IP (%p) is in CoreCLR.\n", ip);
        return true;
    }

    // Check inside our executable heap.
    bool fExecutableHeap = s_pExecutableHeap != NULL &&
        malloc_zone_owns_addr(s_pExecutableHeap, ip);
    if (fExecutableHeap)
    {
        NONPAL_TRACE("    IP (%p) is in the executable heap (%p).\n", ip, s_pExecutableHeap);
        return true;
    }
    
    // Check inside our virtual alloc reserves.
    if (VIRTUALOwnedRegion((UINT_PTR)ip))
    {
        NONPAL_TRACE("    IP (%p) is in a VirtualAlloc region.\n", ip);
        return true;
    }

    // Check for mapped regions, including mapped NGEN images.
    if (MAPGetRegionInfo(ip, NULL))
    {
        NONPAL_TRACE("    IP (%p) is in a memory mapped region.\n", ip);
        return true;
    }

#if defined(_AMD64_)
    // On 64bit, we can have JIT helpers call into supporting functions
    // where the AV can be triggered. In such a case, we should check if the
    // caller of faulting function is in CoreCLR. If so, we should handle the exception.
    //
    // To get the caller IP, we assume that the faulting library function sets up a RBP frame.
    // Currently, this validation is limited to JIT_MemCpy/JIT_MemSet, so is easy to ensure.
    // If this ever becomes more complex, then we should get full context and perform
    // native unwind to get the caller context.
    //
    // Fetch the caller's IP
    PULONG64 rbpFaultingFunc = (PULONG64)sThreadState.__rbp;
    void *pCallerIP = (void *)*((PULONG64)(rbpFaultingFunc+1));
    if (IsWithinCoreCLR(pCallerIP))
    {
        NONPAL_TRACE("    CallerIP (%p) is in CoreCLR.\n", pCallerIP);
        return true;
    }
#endif // defined(_AMD64_)

    // This doesn't look like an exception caused by our code, don't try to handle it.
    NONPAL_TRACE("    IP (%p) not apparently owned by this CoreCLR.\n", ip);
    return false;
}

static MachMessage sMessage;
static MachMessage sReplyOrForward;

//----------------------------------------------------------------------

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
    ForwardedNotification *pOutstandingForwards = NULL;

    kern_return_t MachRet;
    thread_act_t hThread;

    // Loop processing incoming messages forever.
    while (true)
    {
        // Receive the next message.
        sMessage.Receive(s_ExceptionPortSet);

        NONPAL_TRACE("Received message %s from %08X to %08X\n",
                     sMessage.GetMessageTypeName(),
                     sMessage.GetRemotePort(),
                     sMessage.GetLocalPort());

        if (sMessage.IsSetThreadRequest())
        {
            // Handle a request to set the thread context for the specified target thread.
            CONTEXT sContext;
            hThread = sMessage.GetThreadContext(&sContext);

            MachRet = thread_suspend(hThread);
            CHECK_MACH("thread_suspend", MachRet);

            MachRet = CONTEXT_SetThreadContextOnPort(hThread, &sContext);
            CHECK_MACH("CONTEXT_SetThreadContextOnPort", MachRet);

            MachRet = thread_resume(hThread);
            CHECK_MACH("thread_resume", MachRet);
        }
        else if (sMessage.IsExceptionNotification())
        {
            // This is a notification of an exception occurring on another thread.
            hThread = sMessage.GetThread();

            // Determine whether this notification was delivered to our "top" exception handler (one set up
            // by a host call to ICLRRuntimeHost2::RegisterMacEHPort()) or the "bottom" handler (set up the
            // first time the CLR saw the thread). This determines which handlers we chain back to if we
            // decide not to handle the exception ourselves.
            bool fTopException = sMessage.GetLocalPort() == s_TopExceptionPort;

            NONPAL_TRACE("    Notification is for exception %u on thread %08X (sent to the %s exception handler)\n",
                         sMessage.GetException(), hThread, fTopException ? "top" : "bottom");

            // Determine if we should handle this exception ourselves or forward it to the previous handler
            // registered for this exception type (if there is one).
            if (IsHandledException(&sMessage))
            {
                // This is one of ours, pass the relevant exception data to our handler.
                MACH_EH_TYPE(exception_data_type_t) rgCodes[2];
                
                int cCodes = sMessage.GetExceptionCodeCount();
                if (cCodes < 0 || cCodes > 2)
                    NONPAL_RETAIL_ASSERT("Bad exception code count: %d", cCodes);
                for (int i = 0; i < cCodes; i++)
                    rgCodes[i] = sMessage.GetExceptionCode(i);

                kern_return_t ret = catch_exception_raise(s_ExceptionPort,
                                                          hThread,
                                                          mach_task_self(),
                                                          sMessage.GetException(),
                                                          rgCodes,
                                                          cCodes);

                // Send the result of handling the exception back in a reply.
                sReplyOrForward.ReplyToNotification(&sMessage, ret);

                NONPAL_TRACE("    Handled request\n");
            }
            else
            {
                // We didn't recognize the exception as one of ours. Attempt to forward the notification to
                // any handler previously registered for this kind of exception.

                // Locate the record of previously installed handlers that the target thread keeps.
                // Note that the call to PROCThreadFromMachPort() requires taking the PAL process critical
                // section, which is dangerous. The assumption we make is that code holding this critical
                // section (of which there is little) never generates an exception while the lock is still
                // held.
                CorUnix::CPalThread *pTargetThread = PROCThreadFromMachPort(hThread);
                if (pTargetThread == NULL)
                    NONPAL_RETAIL_ASSERT("Failed to translate mach thread to a CPalThread.");
                CorUnix::CThreadMachExceptionHandlers *pHandlers =
                    pTargetThread->GetSavedMachHandlers();

                // Check whether there's even a handler for the particular exception we've been handed.
                CorUnix::MachExceptionHandler sHandler;
                if (!pHandlers->GetHandler(sMessage.GetException(), fTopException, &sHandler))
                {
                    // There's no previous handler to forward this notification to. Reply with a failure
                    // status to let the kernel know we can't handle the exception after all (maybe there's a
                    // handler at the task level).
                    sReplyOrForward.ReplyToNotification(&sMessage, KERN_FAILURE);

                    NONPAL_TRACE("    Unhandled request and no chain-back. Replying with failure.\n");
                }
                else
                {
                    // Check that we don't currently have an outstanding forward for the same thread. If we do
                    // it indicates a cycle (we've forwarded to someone that has ultimately forwarded the
                    // notification back to us). There's not much we can do in this case: forwarding again
                    // will almost certainly lead to a busy loop, dropping the notification is dangerous and
                    // we've already established we don't know enough about the exception to safely handle it.
                    // So we reply to the notification with a failure. The kernel will either find a handler
                    // at the task level or will kill the process.
                    // There is one situation in which it is legal to see the same notification twice: if we
                    // received and forwarded a notification from our top exception port (one established via
                    // ICLRRuntimeHost2::RegisterMacEHPort()) we might see it again on our bottom port and use
                    // the opportunity to forward it to a handler established before we ever saw the thread.
                    bool fReplied = false;
                    ForwardedNotification *pReq = pOutstandingForwards;
                    while (pReq)
                    {
                        // We have a cycle if we have a previous request with the same thread and either this
                        // is a new request to the top exception handler or the old forward we found was for
                        // the bottom handler. That is, if we were notified on the top handler we don't ever
                        // expect to find a notification for the same thread. If not (i.e. the notification
                        // came to the bottom handler) we don't expect to find a notification forwarded from
                        // the bottom handler. The only legal case for a thread match is if we're the bottom
                        // exception handler seeing the forward from the top handler.
                        if (pReq->m_hTargetThread == hThread &&
                            (fTopException || !pReq->m_fTopException))
                        {
                            NONPAL_TRACE("    Detected a cycle in exception handlers.\n");
                            sReplyOrForward.ReplyToNotification(&sMessage, KERN_FAILURE);
                            fReplied = true;
                            break;
                        }

                        pReq = pReq->m_pNext;
                    }
                    
                    if (!fReplied)
                    {
                        // Store enough detail about the notification that we can create a reply once the
                        // forwarded notification is replied to us.
                        ForwardedNotification *pNewNotification =
                            (ForwardedNotification*)malloc(sizeof(ForwardedNotification));
                        if (pNewNotification == NULL)
                            NONPAL_RETAIL_ASSERT("Exception thread ran out of memory to track forwarded exception notifications/");
                        pNewNotification->m_hForwardReplyPort = sMessage.GetRemotePort();
                        pNewNotification->m_hTargetThread = hThread;
                        pNewNotification->m_eNotificationType = sMessage.GetMessageType();
                        pNewNotification->m_eNotificationFlavor = sMessage.GetThreadStateFlavor();
                        pNewNotification->m_fTopException = fTopException;
    
                        // Forward the notification. A port is returned upon which the reply will be received at
                        // some point.
                        pNewNotification->m_hListenReplyPort = sReplyOrForward.ForwardNotification(&sHandler,
                                                                                                   &sMessage);
    
                        // Move the reply port into the listen set so we'll receive the reply as soon as it's sent.
                        MachRet = mach_port_move_member(mach_task_self(),
                                                        pNewNotification->m_hListenReplyPort,
                                                        s_ExceptionPortSet);
                        CHECK_MACH("mach_port_move_member", MachRet);
    
                        // Link the forward records into our list of outstanding forwards.
                        pNewNotification->m_pNext = pOutstandingForwards;
                        pOutstandingForwards = pNewNotification;
    
                        NONPAL_TRACE("    Forwarded request to %08X (reply expected on %08X).\n",
                                     sHandler.m_handler,
                                     pNewNotification->m_hListenReplyPort);
                     }
                }
            }
        }
        else if (sMessage.IsExceptionReply())
        {
            // We've received a reply to an exception notification we forwarded.

            // Locate the forwarding context we stored away to track this notification.
            bool fFoundNotification = false;
            ForwardedNotification *pReq = pOutstandingForwards;
            ForwardedNotification *pPrevReq = NULL;
            while (pReq)
            {
                // We match the reply to the original request based on the port the reply was sent to (we
                // allocate a new port for every notification we forward).
                if (pReq->m_hListenReplyPort == sMessage.GetLocalPort())
                {
                    fFoundNotification = true;

                    // Use the reply plus the original context we saved away to construct a reply to the
                    // original notification we received (we can't just pass the reply along because the
                    // format may differ, e.g. a different behavior or thread state flavor was requested).
                    sReplyOrForward.ForwardReply(pReq->m_hForwardReplyPort,
                                                 pReq->m_eNotificationType,
                                                 pReq->m_hTargetThread,
                                                 pReq->m_eNotificationFlavor,
                                                 &sMessage);

                    // Unlink the context record we saved and deallocate it now the request has been handled.
                    if (pPrevReq)
                        pPrevReq->m_pNext = pReq->m_pNext;
                    else
                        pOutstandingForwards = pReq->m_pNext;

                    free(pReq);

                    // Destroy the port we allocated just to receive the reply (this implicitly removes the
                    // port from the port set).
                    MachRet = mach_port_destroy(mach_task_self(), sMessage.GetLocalPort());
                    CHECK_MACH("mach_port_destroy", MachRet);

                    NONPAL_TRACE("    Sent reply back to original sender.\n");
                    break;
                }

                pPrevReq = pReq;
                pReq = pReq->m_pNext;
            }

            if (!fFoundNotification)
                NONPAL_RETAIL_ASSERT("Failed to find original request.");
        }
        else if (sMessage.IsSendOnceDestroyedNotify())
        {
            // It's possible that when we forward an exception notification that the receiver destroys the
            // message (by destroying the reply port) rather than replying. In this case we receive this
            // notification message.

            // Locate the forwarding context we stored away to track this notification.
            bool fFoundNotification = false;
            ForwardedNotification *pReq = pOutstandingForwards;
            ForwardedNotification *pPrevReq = NULL;
            while (pReq)
            {
                // We match the reply to the original request based on the port the notification was sent to
                // (we allocate a new port for every notification we forward).
                if (pReq->m_hListenReplyPort == sMessage.GetLocalPort())
                {
                    fFoundNotification = true;

                    // Destroy the port we allocated just to receive the reply (this implicitly removes the
                    // port from the port set).
                    MachRet = mach_port_destroy(mach_task_self(), sMessage.GetLocalPort());
                    CHECK_MACH("mach_port_destroy", MachRet);

                    // Destroy the port for the reply to the original notification. This effectively forwards
                    // the reply we just received (i.e. the exception has been handled and no further reply is
                    // going to be sent).
                    MachRet = mach_port_destroy(mach_task_self(), pReq->m_hForwardReplyPort);
                    CHECK_MACH("mach_port_destroy", MachRet);

                    // Unlink the context record we saved and deallocate it now the request has been handled.
                    if (pPrevReq)
                        pPrevReq->m_pNext = pReq->m_pNext;
                    else
                        pOutstandingForwards = pReq->m_pNext;

                    free(pReq);

                    NONPAL_TRACE("    Destroyed original sender's port.\n");
                    break;
                }

                pPrevReq = pReq;
                pReq = pReq->m_pNext;
            }

            if (!fFoundNotification)
                NONPAL_RETAIL_ASSERT("Failed to find original request.");
        }
        else
        {
            NONPAL_RETAIL_ASSERT("Unknown message type: %u", sMessage.GetMessageType());
        }
    }
}
#endif // !DISABLE_EXCEPTIONS

PAL_NORETURN void MachSetThreadContext(CONTEXT *lpContext)
{
#ifndef DISABLE_EXCEPTIONS
    // We need to send a message to the worker thread so that it can set our thread context
    // It is responsible for deallocating the thread port.
    MachMessage sRequest;
    sRequest.SendSetThread(s_ExceptionPort, mach_thread_self(), lpContext);
#else // !DISABLE_EXCEPTIONS
    ASSERT("MachSetThreadContext not allowed for DISABLE_EXCEPTIONS\n");
    TerminateProcess(GetCurrentProcess(), (UINT)(-1));
#endif // !DISABLE_EXCEPTIONS

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
#if !DISABLE_EXCEPTIONS
    // Prime the location of CoreCLR so that we don't deadlock under
    // dladdr with a pthread_mutex_lock.
    IsWithinCoreCLR(NULL);

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

    // Allocate a second port used to receive exceptions after a host has called
    // ICLRRuntimeHost2::RegisterMacEHPort() on a thread. Hosts do this in case another component has
    // registered a handler on the thread after the CoreCLR but is not chaining back exceptions to us. In
    // order to avoid losing the chain-back information we stashed ourselves when we first registered (which
    // would make us just as badly behaved) we keep two sets of chain back information and register two
    // exception ports. Which port we receive an exception notification on will tell us which handler we
    // should chain back to.

    MachRet = mach_port_allocate(mach_task_self(),
                                 MACH_PORT_RIGHT_RECEIVE,
                                 &s_TopExceptionPort);

    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_allocate failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    // Insert the send right into the task
    MachRet = mach_port_insert_right(mach_task_self(),
                                     s_TopExceptionPort,
                                     s_TopExceptionPort,
                                     MACH_MSG_TYPE_MAKE_SEND);

    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_insert_right failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    // Since we now have two exception ports plus additional reply ports used to capture replies to forwarded
    // notifications we need to be able to wait on more than one port at a time. Therefore we listen on a port
    // set rather than a single port (the ports allocated above are the only members initially).
    MachRet = mach_port_allocate(mach_task_self(),
                                 MACH_PORT_RIGHT_PORT_SET,
                                 &s_ExceptionPortSet);
    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_allocate failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    // Move the ports we allocated above into the port set.
    MachRet = mach_port_move_member(mach_task_self(),
                                    s_ExceptionPort,
                                    s_ExceptionPortSet);
    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_move_member failed: %d\n", MachRet);
        UTIL_SetLastErrorFromMach(MachRet);
        return FALSE;
    }

    MachRet = mach_port_move_member(mach_task_self(),
                                    s_TopExceptionPort,
                                    s_ExceptionPortSet);
    if (MachRet != KERN_SUCCESS)
    {
        ASSERT("mach_port_move_member failed: %d\n", MachRet);
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
#endif // !DISABLE_EXCEPTIONS

    // Tell the system to ignore SIGPIPE signals rather than use the default
    // behavior of terminating the process. Ignoring SIGPIPE will cause
    // calls that would otherwise raise that signal to return EPIPE instead.
    // The PAL expects EPIPE from those functions and won't handle a
    // SIGPIPE signal.
// TODO: move to palrt startup code
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
#if !DISABLE_EXCEPTIONS
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
#endif // !DISABLE_EXCEPTIONS
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

#endif // HAVE_MACH_EXCEPTIONS
