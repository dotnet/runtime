// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    machexception.cpp

Abstract:

    Implementation of MACH exception API functions.

--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(EXCEPT); // some headers have code with asserts, so do this first

#include "pal/thread.hpp"
#include "pal/seh.hpp"
#include "pal/palinternal.h"
#if HAVE_MACH_EXCEPTIONS
#include "machexception.h"
#include "pal/critsect.h"
#include "pal/debug.h"
#include "pal/init.h"
#include "pal/utils.h"
#include "pal/context.h"
#include "pal/malloc.hpp"
#include "pal/process.h"
#include "pal/virtual.h"
#include "pal/map.hpp"
#include "pal/environ.h"

#include "machmessage.h"

#include <errno.h>
#include <string.h>
#include <unistd.h>
#include <pthread.h>
#include <dlfcn.h>
#include <mach-o/loader.h>
#include <sys/mman.h>

using namespace CorUnix;

// The port we use to handle exceptions and to set the thread context
mach_port_t s_ExceptionPort;

static DWORD s_PalInitializeFlags = 0;

static const char * PAL_MACH_EXCEPTION_MODE = "PAL_MachExceptionMode";

// This struct is used to track the threads that need to have an exception forwarded
// to the next thread level port in the chain (if exists). An entry is added by the
// faulting sending a special message to the exception thread which saves it on an
// list that is searched when the restarted exception notification is received again.
struct ForwardedException
{
    ForwardedException *m_next;
    thread_act_t Thread;
    exception_type_t ExceptionType;
    CPalThread *PalThread;
};

// The singly linked list and enumerator for the ForwardException struct
struct ForwardedExceptionList
{
private:
    ForwardedException *m_head;
    ForwardedException *m_previous;

public:
    ForwardedException *Current;

    ForwardedExceptionList()
    {
        m_head = NULL;
        MoveFirst();
    }

    void MoveFirst()
    {
        Current = m_head;
        m_previous = NULL;
    }

    bool IsEOL()
    {
        return Current == NULL;
    }

    void MoveNext()
    {
        m_previous = Current;
        Current = Current->m_next;
    }

    void Add(ForwardedException *item)
    {
        item->m_next = m_head;
        m_head = item;
    }

    void Delete()
    {
        if (m_previous == NULL)
        {
            m_head = Current->m_next;
        }
        else
        {
            m_previous->m_next = Current->m_next;
        }
        free(Current);

        Current = m_head;
        m_previous = NULL;
    }
};

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

/*++
Function :
    GetExceptionMask()

    Returns the mach exception mask for the exceptions to hook for a thread.

Return value :
    mach exception mask
--*/
static
exception_mask_t
GetExceptionMask()
{
    static MachExceptionMode exMode = MachException_Uninitialized;

    if (exMode == MachException_Uninitialized)
    {
        exMode = MachException_Default;

        char* exceptionSettings = EnvironGetenv(PAL_MACH_EXCEPTION_MODE);
        if (exceptionSettings)
        {
            exMode = (MachExceptionMode)atoi(exceptionSettings);
            free(exceptionSettings);
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

    if (s_PalInitializeFlags & PAL_INITIALIZE_REGISTER_SIGNALS)
    {
        if (!(exMode & MachException_SuppressIllegal))
        {
            machExceptionMask |= PAL_EXC_ILLEGAL_MASK;
        }
        if (!(exMode & MachException_SuppressDebugging) && (s_PalInitializeFlags & PAL_INITIALIZE_DEBUGGER_EXCEPTIONS))
        {
            // Always hook exception ports for breakpoint exceptions.
            // The reason is that we don't know when a managed debugger
            // will attach, so we have to be prepared.  We don't want
            // to later go through the thread list and hook exception
            // ports for exactly those threads that currently are in
            // this PAL.
            machExceptionMask |= PAL_EXC_DEBUGGING_MASK;
        }
        if (!(exMode & MachException_SuppressManaged))
        {
            machExceptionMask |= PAL_EXC_MANAGED_MASK;
        }
    }

    return machExceptionMask;
}

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
        if (countBits != static_cast<exception_mask_t>(CThreadMachExceptionHandlers::s_nPortsMax))
        {
            ASSERT("s_nPortsMax is %u, but needs to be %u\n",
                   CThreadMachExceptionHandlers::s_nPortsMax, countBits);
        }
#endif // _DEBUG

        NONPAL_TRACE("Enabling handlers for thread %08x exception mask %08x exception port %08x\n",
            GetMachPortSelf(), machExceptionMask, s_ExceptionPort);

        CThreadMachExceptionHandlers *pSavedHandlers = GetSavedMachHandlers();

        // Swap current handlers into temporary storage first. That's because it's possible (even likely) that
        // some or all of the handlers might still be ours. In those cases we don't want to overwrite the
        // chain-back entries with these useless self-references.
        kern_return_t machret;
        kern_return_t machretDeallocate;
        thread_port_t thread = mach_thread_self();

        machret = thread_swap_exception_ports(
            thread,
            machExceptionMask,
            s_ExceptionPort,
            EXCEPTION_DEFAULT | MACH_EXCEPTION_CODES,
            THREAD_STATE_NONE,
            pSavedHandlers->m_masks,
            &pSavedHandlers->m_nPorts,
            pSavedHandlers->m_handlers,
            pSavedHandlers->m_behaviors,
            pSavedHandlers->m_flavors);

        machretDeallocate = mach_port_deallocate(mach_task_self(), thread);
        CHECK_MACH("mach_port_deallocate", machretDeallocate);

        if (machret != KERN_SUCCESS)
        {
            ASSERT("thread_swap_exception_ports failed: %d %s\n", machret, mach_error_string(machret));
            return UTIL_MachErrorToPalError(machret);
        }

#ifdef _DEBUG
        NONPAL_TRACE("EnableMachExceptions: THREAD PORT count %d\n", pSavedHandlers->m_nPorts);
        for (mach_msg_type_number_t i = 0; i < pSavedHandlers->m_nPorts; i++)
        {
            _ASSERTE(pSavedHandlers->m_handlers[i] != s_ExceptionPort);
            NONPAL_TRACE("EnableMachExceptions: THREAD PORT mask %08x handler: %08x behavior %08x flavor %u\n",
                pSavedHandlers->m_masks[i],
                pSavedHandlers->m_handlers[i],
                pSavedHandlers->m_behaviors[i],
                pSavedHandlers->m_flavors[i]);
        }
#endif // _DEBUG
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

    // Get the handlers to restore.
    CThreadMachExceptionHandlers *savedPorts = GetSavedMachHandlers();

    kern_return_t MachRet = KERN_SUCCESS;
    for (int i = 0; i < savedPorts->m_nPorts; i++)
    {
        // If no handler was ever set, thread_swap_exception_ports returns
        // MACH_PORT_NULL for the handler and zero values for behavior
        // and flavor.  Unfortunately, the latter are invalid even for
        // MACH_PORT_NULL when you use thread_set_exception_ports.
        exception_behavior_t behavior = savedPorts->m_behaviors[i] ? savedPorts->m_behaviors[i] : EXCEPTION_DEFAULT;
        thread_state_flavor_t flavor = savedPorts->m_flavors[i] ? savedPorts->m_flavors[i] : MACHINE_THREAD_STATE;
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

#if defined(HOST_AMD64)
// Since HijackFaultingThread pushed the context, exception record and info on the stack, we need to adjust the
// signature of PAL_DispatchException such that the corresponding arguments are considered to be on the stack
// per GCC64 calling convention rules. Hence, the first 6 dummy arguments (corresponding to RDI, RSI, RDX,RCX, R8, R9).
extern "C"
void PAL_DispatchException(DWORD64 dwRDI, DWORD64 dwRSI, DWORD64 dwRDX, DWORD64 dwRCX, DWORD64 dwR8, DWORD64 dwR9, PCONTEXT pContext, PEXCEPTION_RECORD pExRecord, MachExceptionInfo *pMachExceptionInfo)
#elif defined(HOST_ARM64)
extern "C"
void PAL_DispatchException(PCONTEXT pContext, PEXCEPTION_RECORD pExRecord, MachExceptionInfo *pMachExceptionInfo)
#endif
{
    CPalThread *pThread = InternalGetCurrentThread();

    CONTEXT *contextRecord;
    EXCEPTION_RECORD *exceptionRecord;
    AllocateExceptionRecords(&exceptionRecord, &contextRecord);

    *contextRecord = *pContext;
    *exceptionRecord = *pExRecord;

    contextRecord->ContextFlags |= CONTEXT_EXCEPTION_ACTIVE;
    bool continueExecution;

    {
        // The exception object takes ownership of the exceptionRecord and contextRecord
        PAL_SEHException exception(exceptionRecord, contextRecord);

        TRACE("PAL_DispatchException(EC %08x EA %p)\n", pExRecord->ExceptionCode, pExRecord->ExceptionAddress);

        continueExecution = SEHProcessException(&exception);
        if (continueExecution)
        {
            // Make a copy of the exception records so that we can free them before restoring the context
            *pContext = *contextRecord;
            *pExRecord = *exceptionRecord;
        }

        // The exception records are destroyed by the PAL_SEHException destructor now.
    }

    if (continueExecution)
    {
#if defined(HOST_ARM64)
        // RtlRestoreContext assembly corrupts X16 & X17, so it cannot be
        // used for GCStress=C restore
        MachSetThreadContext(pContext);
#else
        RtlRestoreContext(pContext, pExRecord);
#endif
    }

    // Send the forward request to the exception thread to process
    MachMessage sSendMessage;
    sSendMessage.SendForwardException(s_ExceptionPort, pMachExceptionInfo, pThread);

    // Spin wait until this thread is hijacked by the exception thread
    while (TRUE)
    {
        sched_yield();
    }
}

extern "C" void PAL_DispatchExceptionWrapper();
extern "C" int PAL_DispatchExceptionReturnOffset;

/*++
Function :
    BuildExceptionRecord

    Sets up up an ExceptionRecord from an exception message

Parameters :
    exceptionInfo - exception info to build the exception record
    pExceptionRecord - exception record to setup
*/
static
void
BuildExceptionRecord(
    MachExceptionInfo& exceptionInfo,               // [in] exception info
    EXCEPTION_RECORD *pExceptionRecord)             // [out] Used to return exception parameters
{
    memset(pExceptionRecord, 0, sizeof(EXCEPTION_RECORD));

    DWORD exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;

    switch(exceptionInfo.ExceptionType)
    {
    // Could not access memory. subcode contains the bad memory address.
    case EXC_BAD_ACCESS:
        if (exceptionInfo.SubcodeCount != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected subcode");
            exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
        }
        else
        {
#if defined(HOST_AMD64)
            exceptionCode = EXCEPTION_ACCESS_VIOLATION;
#elif defined(HOST_ARM64)
            switch (exceptionInfo.Subcodes[0])
            {
                case EXC_ARM_DA_ALIGN:
                    exceptionCode = EXCEPTION_DATATYPE_MISALIGNMENT;
                    break;
                case EXC_ARM_DA_DEBUG:
                    exceptionCode = EXCEPTION_BREAKPOINT;
                    break;
                case EXC_ARM_SP_ALIGN:
                    exceptionCode = EXCEPTION_DATATYPE_MISALIGNMENT;
                    break;
                case EXC_ARM_SWP:
                    exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
                    break;
                case EXC_ARM_PAC_FAIL:
                    // PAC Authentication failure fall through
                default:
                    exceptionCode = EXCEPTION_ACCESS_VIOLATION;
            }
#else
#error Unexpected architecture
#endif

            pExceptionRecord->NumberParameters = 2;
            pExceptionRecord->ExceptionInformation[0] = 0;
            pExceptionRecord->ExceptionInformation[1] = exceptionInfo.Subcodes[1];
            NONPAL_TRACE("subcodes[1] = %llx\n", (uint64_t) exceptionInfo.Subcodes[1]);
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
        if (exceptionInfo.SubcodeCount != 2)
        {
            NONPAL_RETAIL_ASSERT("Got an unexpected subcode");
            exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
        }
        else
        {
            switch (exceptionInfo.Subcodes[0])
            {
#if defined(HOST_AMD64)
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
#elif defined(HOST_ARM64)
                case EXC_ARM_FP_IO:
                    exceptionCode = EXCEPTION_FLT_INVALID_OPERATION;
                    break;
                case EXC_ARM_FP_DZ:
                    exceptionCode = EXCEPTION_FLT_DIVIDE_BY_ZERO;
                    break;
                case EXC_ARM_FP_OF:
                    exceptionCode = EXCEPTION_FLT_OVERFLOW;
                    break;
                case EXC_ARM_FP_UF:
                    exceptionCode = EXCEPTION_FLT_UNDERFLOW;
                    break;
                case EXC_ARM_FP_IX:
                    exceptionCode = EXCEPTION_FLT_INEXACT_RESULT;
                    break;
                case EXC_ARM_FP_ID:
                    exceptionCode = EXCEPTION_FLT_DENORMAL_OPERAND;
                    break;
#else
#error Unexpected architecture
#endif
                default:
                    exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
                    break;
            }
        }
        break;

    case EXC_SOFTWARE:
        exceptionCode = EXCEPTION_ILLEGAL_INSTRUCTION;
        break;

    // Trace, breakpoint, etc. Details in subcode field.
    case EXC_BREAKPOINT:
#if defined(HOST_AMD64)
        if (exceptionInfo.Subcodes[0] == EXC_I386_SGL)
        {
            exceptionCode = EXCEPTION_SINGLE_STEP;
        }
        else if (exceptionInfo.Subcodes[0] == EXC_I386_BPT)
        {
            exceptionCode = EXCEPTION_BREAKPOINT;
        }
#elif defined(HOST_ARM64)
        if (exceptionInfo.Subcodes[0] == EXC_ARM_BREAKPOINT)
        {
            exceptionCode = EXCEPTION_BREAKPOINT;
        }
#else
#error Unexpected architecture
#endif
        else
        {
            WARN("unexpected subcode %d for EXC_BREAKPOINT", exceptionInfo.Subcodes[0]);
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
        NONPAL_ASSERT("Got unknown trap code %d\n", exceptionInfo.ExceptionType);
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
        NONPAL_ASSERT("Got unknown trap code %d\n", exception);
        break;
    }
    return "INVALID CODE";
}
#endif // _DEBUG

/*++
Function :
    HijackFaultingThread

    Sets the faulting thread up to return to PAL_DispatchException with an
    ExceptionRecord and thread CONTEXT.

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
    MachMessage& message)           // [in] exception message
{
    MachExceptionInfo exceptionInfo(thread, message);
    EXCEPTION_RECORD exceptionRecord;
    CONTEXT threadContext;
    kern_return_t machret;

    // Fill in the exception record from the exception info
    BuildExceptionRecord(exceptionInfo, &exceptionRecord);

#if defined(HOST_AMD64)
    threadContext.ContextFlags = CONTEXT_FLOATING_POINT;
    CONTEXT_GetThreadContextFromThreadState(x86_FLOAT_STATE, (thread_state_t)&exceptionInfo.FloatState, &threadContext);

    threadContext.ContextFlags |= CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS;
    CONTEXT_GetThreadContextFromThreadState(x86_THREAD_STATE, (thread_state_t)&exceptionInfo.ThreadState, &threadContext);

    void **targetSP = (void **)threadContext.Rsp;
#elif defined(HOST_ARM64)
    threadContext.ContextFlags = CONTEXT_FLOATING_POINT;
    CONTEXT_GetThreadContextFromThreadState(ARM_NEON_STATE64, (thread_state_t)&exceptionInfo.FloatState, &threadContext);

    threadContext.ContextFlags |= CONTEXT_CONTROL | CONTEXT_INTEGER;
    CONTEXT_GetThreadContextFromThreadState(ARM_THREAD_STATE64, (thread_state_t)&exceptionInfo.ThreadState, &threadContext);

    void **targetSP = (void **)threadContext.Sp;
#else
#error Unexpected architecture
#endif

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
    // Assume that AV isn't an SO to begin with.
    bool fIsStackOverflow = false;

    if (exceptionRecord.ExceptionCode == EXCEPTION_ACCESS_VIOLATION)
    {
        // Calculate the page base addresses for the fault and the faulting thread's SP.
        int cbPage = getpagesize();
        char *pFaultPage = (char*)(exceptionRecord.ExceptionInformation[1] & ~(cbPage - 1));
        char *pStackTopPage = (char*)((size_t)targetSP & ~(cbPage - 1));

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
            vm_region_basic_info_data_64_t info;
            infoCnt = VM_REGION_BASIC_INFO_COUNT_64;
            vm_flavor = VM_REGION_BASIC_INFO_64;
            mach_port_t object_name;

            vm_address = (vm_address_t)(pFaultPage + cbPage);

            machret = vm_region_64(
                mach_task_self(),
                &vm_address,
                &vm_size,
                vm_flavor,
                (vm_region_info_t)&info,
                &infoCnt,
                &object_name);
            CHECK_MACH("vm_region_64", machret);

            // If vm_region updated the address we gave it then that address was not part of a region at all
            // (and so this cannot be an SO). Otherwise check that the ESP lies in the region returned.
            char *pRegionStart = (char*)vm_address;
            char *pRegionEnd = (char*)vm_address + vm_size;
            if (pRegionStart == (pFaultPage + cbPage) && pStackTopPage < pRegionEnd)
                fIsStackOverflow = true;
        }

        if (!fIsStackOverflow)
        {
            // Check if we can read pointer sizeD bytes below the target thread's stack pointer.
            // If we are unable to, then it implies we have run into SO.
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
    }

    if (fIsStackOverflow)
    {
        exceptionRecord.ExceptionCode = EXCEPTION_STACK_OVERFLOW;
    }

    exceptionRecord.ExceptionFlags = EXCEPTION_IS_SIGNAL;
    exceptionRecord.ExceptionRecord = NULL;

#if defined(HOST_AMD64)
    NONPAL_ASSERTE(exceptionInfo.ThreadState.tsh.flavor == x86_THREAD_STATE64);

    // Make a copy of the thread state because the one in exceptionInfo needs to be preserved to restore
    // the state if the exception is forwarded.
    x86_thread_state64_t ts64 = exceptionInfo.ThreadState.uts.ts64;

    // If we're in single step mode, disable it since we're going to call PAL_DispatchException
    if (exceptionRecord.ExceptionCode == EXCEPTION_SINGLE_STEP)
    {
        ts64.__rflags &= ~EFL_TF;
    }

    exceptionRecord.ExceptionAddress = (void *)ts64.__rip;

    void **FramePointer = (void **)ts64.__rsp;
#elif defined(HOST_ARM64)
    // Make a copy of the thread state because the one in exceptionInfo needs to be preserved to restore
    // the state if the exception is forwarded.
    arm_thread_state64_t ts64 = exceptionInfo.ThreadState;

    exceptionRecord.ExceptionAddress = (void *)arm_thread_state64_get_pc_fptr(ts64);

    void **FramePointer = (void **)arm_thread_state64_get_sp(ts64);
#else
#error Unexpected architecture
#endif

    if (fIsStackOverflow)
    {
        // Allocate the minimal stack necessary for handling stack overflow
        int stackOverflowStackSize = 7 * 4096;
        // Align the size to virtual page size and add one virtual page as a stack guard
        stackOverflowStackSize = ALIGN_UP(stackOverflowStackSize, GetVirtualPageSize()) + GetVirtualPageSize();
        void* stackOverflowHandlerStack = mmap(NULL, stackOverflowStackSize, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);

        if ((stackOverflowHandlerStack == MAP_FAILED) || mprotect((void*)stackOverflowHandlerStack, GetVirtualPageSize(), PROT_NONE) != 0)
        {
            // We are out of memory or we've failed to protect the guard page, so resort to just printing a stack overflow message and abort
            write(STDERR_FILENO, StackOverflowMessage, sizeof(StackOverflowMessage) - 1);
            abort();
        }

        FramePointer = (void**)((size_t)stackOverflowHandlerStack + stackOverflowStackSize);
    }

    // Construct a stack frame for a pretend activation of the function
    // PAL_DispatchExceptionWrapper that serves only to make the stack
    // correctly unwindable by the system exception unwinder.
    // PAL_DispatchExceptionWrapper has an ebp frame, its local variables
    // are the context and exception record, and it has just "called"
    // PAL_DispatchException.
#if defined(HOST_AMD64)
    *--FramePointer = (void *)ts64.__rip;
    *--FramePointer = (void *)ts64.__rbp;

    ts64.__rbp = (SIZE_T)FramePointer;
#elif defined(HOST_ARM64)
    *--FramePointer = (void *)arm_thread_state64_get_pc_fptr(ts64);
    *--FramePointer = (void *)arm_thread_state64_get_fp(ts64);

    arm_thread_state64_set_fp(ts64, FramePointer);
#else
#error Unexpected architecture
#endif

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

    FramePointer = (void **)((ULONG_PTR)FramePointer - sizeof(MachExceptionInfo));
    MachExceptionInfo *pMachExceptionInfo = (MachExceptionInfo *)FramePointer;
    *pMachExceptionInfo = exceptionInfo;

#if defined(HOST_AMD64)
    // Push arguments to PAL_DispatchException
    FramePointer = (void **)((ULONG_PTR)FramePointer - 3 * sizeof(void *));

    // Make sure it's aligned - ABI requires 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    FramePointer[0] = pContext;
    FramePointer[1] = pExceptionRecord;
    FramePointer[2] = pMachExceptionInfo;

    // Place the return address to right after the fake call in PAL_DispatchExceptionWrapper
    FramePointer[-1] = (void *)((ULONG_PTR)PAL_DispatchExceptionWrapper + PAL_DispatchExceptionReturnOffset);

    // Make the instruction register point to DispatchException
    ts64.__rip = (SIZE_T)PAL_DispatchException;
    ts64.__rsp = (SIZE_T)&FramePointer[-1]; // skip return address

    // Now set the thread state for the faulting thread so that PAL_DispatchException executes next
    machret = thread_set_state(thread, x86_THREAD_STATE64, (thread_state_t)&ts64, x86_THREAD_STATE64_COUNT);
    CHECK_MACH("thread_set_state(thread)", machret);
#elif defined(HOST_ARM64)
    // Setup arguments to PAL_DispatchException
    ts64.__x[0] = (uint64_t)pContext;
    ts64.__x[1] = (uint64_t)pExceptionRecord;
    ts64.__x[2] = (uint64_t)pMachExceptionInfo;

    // Make sure it's aligned - SP has 16-byte alignment
    FramePointer = (void **)((ULONG_PTR)FramePointer - ((ULONG_PTR)FramePointer % 16));
    arm_thread_state64_set_sp(ts64, FramePointer);

    // Make the call to DispatchException
    arm_thread_state64_set_lr_fptr(ts64, (uint64_t)PAL_DispatchExceptionWrapper + PAL_DispatchExceptionReturnOffset);
    arm_thread_state64_set_pc_fptr(ts64, PAL_DispatchException);

    // Now set the thread state for the faulting thread so that PAL_DispatchException executes next
    machret = thread_set_state(thread, ARM_THREAD_STATE64, (thread_state_t)&ts64, ARM_THREAD_STATE64_COUNT);
    CHECK_MACH("thread_set_state(thread)", machret);
#else
#error Unexpected architecture
#endif
}

/*++
Function :
    SuspendMachThread

    Suspend the specified thread.

Parameters:
    thread - mach thread port

Return value :
    KERN_SUCCESS if the suspend succeeded, other code in case of failure
--*/
static
kern_return_t
SuspendMachThread(thread_act_t thread)
{
    kern_return_t machret;

    while (true)
    {
        machret = thread_suspend(thread);
        if (machret != KERN_SUCCESS)
        {
            break;
        }

        // Ensure that if the thread was running in the kernel, the kernel operation
        // is safely aborted so that it can be restarted later.
        machret = thread_abort_safely(thread);
        if (machret == KERN_SUCCESS)
        {
            break;
        }

        // The thread was running in the kernel executing a non-atomic operation
        // that cannot be restarted, so we need to resume the thread and retry
        machret = thread_resume(thread);
        if (machret != KERN_SUCCESS)
        {
            break;
        }
    }

    return machret;
}

/*++
Function :
    SEHExceptionThread

    Entry point for the thread that will listen for exception in any other thread.

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

Parameters :
    void *args - not used

Return value :
   Never returns
--*/
void *
SEHExceptionThread(void *args)
{
    ForwardedExceptionList feList;
    MachMessage sReplyOrForward;
    MachMessage sMessage;
    kern_return_t machret;
    thread_act_t thread;

    // Loop processing incoming messages forever.
    while (true)
    {
        // Receive the next message.
        sMessage.Receive(s_ExceptionPort);

        NONPAL_TRACE("Received message %s (%08x) from (remote) %08x to (local) %08x\n",
            sMessage.GetMessageTypeName(),
            sMessage.GetMessageType(),
            sMessage.GetRemotePort(),
            sMessage.GetLocalPort());

        if (sMessage.IsSetThreadRequest())
        {
            // Handle a request to set the thread context for the specified target thread.
            CONTEXT sContext;
            thread = sMessage.GetThreadContext(&sContext);

            // Suspend the target thread
            machret = SuspendMachThread(thread);
            CHECK_MACH("SuspendMachThread", machret);

            machret = CONTEXT_SetThreadContextOnPort(thread, &sContext);
            CHECK_MACH("CONTEXT_SetThreadContextOnPort", machret);

            machret = thread_resume(thread);
            CHECK_MACH("thread_resume", machret);
        }
        else if (sMessage.IsExceptionNotification())
        {
            // This is a notification of an exception occurring on another thread.
            exception_type_t exceptionType = sMessage.GetException();
            thread = sMessage.GetThread();

#ifdef _DEBUG
            if (NONPAL_TRACE_ENABLED)
            {
                NONPAL_TRACE("ExceptionNotification %s (%u) thread %08x flavor %u\n",
                    GetExceptionString(exceptionType),
                    exceptionType,
                    thread,
                    sMessage.GetThreadStateFlavor());

                int subcode_count = sMessage.GetExceptionCodeCount();
                for (int i = 0; i < subcode_count; i++)
                    NONPAL_TRACE("ExceptionNotification subcode[%d] = %llx\n", i, (uint64_t) sMessage.GetExceptionCode(i));

#if defined(HOST_AMD64)
                x86_thread_state64_t threadStateActual;
                unsigned int count = sizeof(threadStateActual) / sizeof(unsigned);
                machret = thread_get_state(thread, x86_THREAD_STATE64, (thread_state_t)&threadStateActual, &count);
                CHECK_MACH("thread_get_state", machret);

                NONPAL_TRACE("ExceptionNotification actual  rip %016llx rsp %016llx rbp %016llx rax %016llx r15 %016llx eflags %08llx\n",
                    threadStateActual.__rip,
                    threadStateActual.__rsp,
                    threadStateActual.__rbp,
                    threadStateActual.__rax,
                    threadStateActual.__r15,
                    threadStateActual.__rflags);

                x86_exception_state64_t threadExceptionState;
                unsigned int ehStateCount = sizeof(threadExceptionState) / sizeof(unsigned);
                machret = thread_get_state(thread, x86_EXCEPTION_STATE64, (thread_state_t)&threadExceptionState, &ehStateCount);
                CHECK_MACH("thread_get_state", machret);

                NONPAL_TRACE("ExceptionNotification trapno %04x cpu %04x err %08x faultAddr %016llx\n",
                    threadExceptionState.__trapno,
                    threadExceptionState.__cpu,
                    threadExceptionState.__err,
                    threadExceptionState.__faultvaddr);
#elif defined(HOST_ARM64)
                arm_thread_state64_t threadStateActual;
                unsigned int count = sizeof(threadStateActual) / sizeof(unsigned);
                machret = thread_get_state(thread, ARM_THREAD_STATE64, (thread_state_t)&threadStateActual, &count);
                CHECK_MACH("thread_get_state", machret);

                NONPAL_TRACE("ExceptionNotification actual  lr %p sp %016llx fp %016llx pc %p cpsr %08x\n",
                    arm_thread_state64_get_lr_fptr(threadStateActual),
                    arm_thread_state64_get_sp(threadStateActual),
                    arm_thread_state64_get_fp(threadStateActual),
                    arm_thread_state64_get_pc_fptr(threadStateActual),
                    threadStateActual.__cpsr);

                arm_exception_state64_t threadExceptionState;
                unsigned int ehStateCount = sizeof(threadExceptionState) / sizeof(unsigned);
                machret = thread_get_state(thread, ARM_EXCEPTION_STATE64, (thread_state_t)&threadExceptionState, &ehStateCount);
                CHECK_MACH("thread_get_state", machret);

                NONPAL_TRACE("ExceptionNotification far %016llx esr %08x exception %08x\n",
                    threadExceptionState.__far,
                    threadExceptionState.__esr,
                    threadExceptionState.__exception);
#else
#error Unexpected architecture
#endif
            }
#endif // _DEBUG

            bool feFound = false;
            feList.MoveFirst();

            while (!feList.IsEOL())
            {
                mach_port_type_t ePortType;
                if (mach_port_type(mach_task_self(), feList.Current->Thread, &ePortType) != KERN_SUCCESS || (ePortType & MACH_PORT_TYPE_DEAD_NAME))
                {
                    NONPAL_TRACE("Forwarded exception: invalid thread port %08x\n", feList.Current->Thread);

                    // Unlink and delete the forwarded exception instance
                    feList.Delete();
                }
                else
                {
                    if (feList.Current->Thread == thread)
                    {
                        bool isSameException = feList.Current->ExceptionType == exceptionType;
                        feFound = true;

                        // Locate the record of previously installed handlers that the target thread keeps.
                        CThreadMachExceptionHandlers *pHandlers = feList.Current->PalThread->GetSavedMachHandlers();

                        // Unlink and delete the forwarded exception instance
                        feList.Delete();

                        // Check if the current exception type matches the forwarded one and whether
                        // there's a handler for the particular exception we've been handed.
                        MachExceptionHandler sHandler;
                        if (isSameException && pHandlers->GetHandler(exceptionType, &sHandler))
                        {
                            NONPAL_TRACE("ForwardNotification thread %08x to handler %08x\n", thread, sHandler.m_handler);
                            sReplyOrForward.ForwardNotification(&sHandler, sMessage);
                        }
                        else
                        {
                            NONPAL_TRACE("ReplyToNotification KERN_FAILURE thread %08x port %08x sameException %d\n",
                                thread, sMessage.GetRemotePort(), isSameException);
                            sReplyOrForward.ReplyToNotification(sMessage, KERN_FAILURE);
                        }
                        break;
                    }

                    feList.MoveNext();
                }
            }

            if (!feFound)
            {
                NONPAL_TRACE("HijackFaultingThread thread %08x\n", thread);
                HijackFaultingThread(thread, mach_task_self(), sMessage);

                // Send the result of handling the exception back in a reply.
                NONPAL_TRACE("ReplyToNotification KERN_SUCCESS thread %08x port %08x\n", thread, sMessage.GetRemotePort());
                sReplyOrForward.ReplyToNotification(sMessage, KERN_SUCCESS);
            }
        }
        else if (sMessage.IsForwardExceptionRequest())
        {
            thread = sMessage.GetThread();

            NONPAL_TRACE("ForwardExceptionRequest for thread %08x\n", thread);

            // Suspend the faulting thread.
            machret = SuspendMachThread(thread);
            CHECK_MACH("SuspendMachThread", machret);

            // Set the context back to the original faulting state.
            MachExceptionInfo *pExceptionInfo = sMessage.GetExceptionInfo();
            pExceptionInfo->RestoreState(thread);

            // Allocate an forwarded exception entry
            ForwardedException *pfe = (ForwardedException *)malloc(sizeof(ForwardedException));
            if (pfe == NULL)
            {
                NONPAL_RETAIL_ASSERT("Exception thread ran out of memory to track forwarded exception notifications");
            }

            // Save the forwarded exception entry away for the restarted exception message
            pfe->Thread = thread;
            pfe->ExceptionType = pExceptionInfo->ExceptionType;
            pfe->PalThread = sMessage.GetPalThread();
            feList.Add(pfe);

            // Now let the thread run at the original exception context to restart the exception
            NONPAL_TRACE("ForwardExceptionRequest resuming thread %08x exception type %08x\n", thread, pfe->ExceptionType);
            machret = thread_resume(thread);
            CHECK_MACH("thread_resume", machret);
        }
        else
        {
            NONPAL_RETAIL_ASSERT("Unknown message type: %u", sMessage.GetMessageType());
        }
    }
}

/*++
Function :
    MachExceptionInfo constructor

    Saves the exception info from the exception notification message and
    the current thread state.

Parameters:
    thread - thread port to restore
    message - exception message

Return value :
    none
--*/
MachExceptionInfo::MachExceptionInfo(mach_port_t thread, MachMessage& message)
{
    kern_return_t machret;

    ExceptionType = message.GetException();
    SubcodeCount = message.GetExceptionCodeCount();
    NONPAL_RETAIL_ASSERTE(SubcodeCount >= 0 && SubcodeCount <= 2);

    for (int i = 0; i < SubcodeCount; i++)
        Subcodes[i] = message.GetExceptionCode(i);

#if defined(HOST_AMD64)
    mach_msg_type_number_t count = x86_THREAD_STATE_COUNT;
    machret = thread_get_state(thread, x86_THREAD_STATE, (thread_state_t)&ThreadState, &count);
    CHECK_MACH("thread_get_state", machret);

    count = x86_FLOAT_STATE_COUNT;
    machret = thread_get_state(thread, x86_FLOAT_STATE, (thread_state_t)&FloatState, &count);
    CHECK_MACH("thread_get_state(float)", machret);

    count = x86_DEBUG_STATE_COUNT;
    machret = thread_get_state(thread, x86_DEBUG_STATE, (thread_state_t)&DebugState, &count);
    CHECK_MACH("thread_get_state(debug)", machret);
#elif defined(HOST_ARM64)
    mach_msg_type_number_t count = ARM_THREAD_STATE64_COUNT;
    machret = thread_get_state(thread, ARM_THREAD_STATE64, (thread_state_t)&ThreadState, &count);
    CHECK_MACH("thread_get_state", machret);

    count = ARM_NEON_STATE64_COUNT;
    machret = thread_get_state(thread, ARM_NEON_STATE64, (thread_state_t)&FloatState, &count);
    CHECK_MACH("thread_get_state(float)", machret);

    count = ARM_DEBUG_STATE64_COUNT;
    machret = thread_get_state(thread, ARM_DEBUG_STATE64, (thread_state_t)&DebugState, &count);
    CHECK_MACH("thread_get_state(debug)", machret);
#else
#error Unexpected architecture
#endif
}

/*++
Function :
    MachExceptionInfo::RestoreState

    Restore the thread to the saved exception info state.

Parameters:
    thread - thread port to restore

Return value :
    none
--*/
void MachExceptionInfo::RestoreState(mach_port_t thread)
{
#if defined(HOST_AMD64)
    // If we are restarting a breakpoint, we need to bump the IP back one to
    // point at the actual int 3 instructions.
    if (ExceptionType == EXC_BREAKPOINT)
    {
        if (Subcodes[0] == EXC_I386_BPT)
        {
            ThreadState.uts.ts64.__rip--;
        }
    }
    kern_return_t machret = thread_set_state(thread, x86_THREAD_STATE, (thread_state_t)&ThreadState, x86_THREAD_STATE_COUNT);
    CHECK_MACH("thread_set_state(thread)", machret);

    machret = thread_set_state(thread, x86_FLOAT_STATE, (thread_state_t)&FloatState, x86_FLOAT_STATE_COUNT);
    CHECK_MACH("thread_set_state(float)", machret);

    machret = thread_set_state(thread, x86_DEBUG_STATE, (thread_state_t)&DebugState, x86_DEBUG_STATE_COUNT);
    CHECK_MACH("thread_set_state(debug)", machret);
#elif defined(HOST_ARM64)
    kern_return_t machret = thread_set_state(thread, ARM_THREAD_STATE64, (thread_state_t)&ThreadState, ARM_THREAD_STATE64_COUNT);
    CHECK_MACH("thread_set_state(thread)", machret);

    machret = thread_set_state(thread, ARM_NEON_STATE64, (thread_state_t)&FloatState, ARM_NEON_STATE64_COUNT);
    CHECK_MACH("thread_set_state(float)", machret);

    machret = thread_set_state(thread, ARM_DEBUG_STATE64, (thread_state_t)&DebugState, ARM_DEBUG_STATE64_COUNT);
    CHECK_MACH("thread_set_state(debug)", machret);
#else
#error Unexpected architecture
#endif
}

/*++
Function :
    MachSetThreadContext

    Sets the context of the current thread by sending a notification
    to the exception thread.

Parameters:
    lpContext - the CONTEXT to set the current thread

Return value :
    Doesn't return
--*/
PAL_NORETURN
void
MachSetThreadContext(CONTEXT *lpContext)
{
    // We need to send a message to the worker thread so that it can set our thread context.
    MachMessage sRequest;
    sRequest.SendSetThread(s_ExceptionPort, lpContext);

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

    flags - PAL_INITIALIZE flags

Return value :
    TRUE  if SEH support initialization succeeded
    FALSE otherwise
--*/
BOOL
SEHInitializeMachExceptions(DWORD flags)
{
    pthread_t exception_thread;
    kern_return_t machret;

    s_PalInitializeFlags = flags;

    if (flags & PAL_INITIALIZE_REGISTER_SIGNALS)
    {
        // Allocate a mach port that will listen in on exceptions
        machret = mach_port_allocate(mach_task_self(), MACH_PORT_RIGHT_RECEIVE, &s_ExceptionPort);
        if (machret != KERN_SUCCESS)
        {
            ASSERT("mach_port_allocate failed: %d\n", machret);
            UTIL_SetLastErrorFromMach(machret);
            return FALSE;
        }

        // Insert the send right into the task
        machret = mach_port_insert_right(mach_task_self(), s_ExceptionPort, s_ExceptionPort, MACH_MSG_TYPE_MAKE_SEND);
        if (machret != KERN_SUCCESS)
        {
            ASSERT("mach_port_insert_right failed: %d\n", machret);
            UTIL_SetLastErrorFromMach(machret);
            return FALSE;
        }

        // Create the thread that will listen to the exception for all threads
        int createret = pthread_create(&exception_thread, NULL, SEHExceptionThread, NULL);
        if (createret != 0)
        {
            ERROR("pthread_create failed, error is %d (%s)\n", createret, strerror(createret));
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            return FALSE;
        }

#ifdef _DEBUG
        if (NONPAL_TRACE_ENABLED)
        {
            CThreadMachExceptionHandlers taskHandlers;
            machret = task_get_exception_ports(mach_task_self(),
                PAL_EXC_ALL_MASK,
                taskHandlers.m_masks,
                &taskHandlers.m_nPorts,
                taskHandlers.m_handlers,
                taskHandlers.m_behaviors,
                taskHandlers.m_flavors);

            if (machret == KERN_SUCCESS)
            {
                NONPAL_TRACE("SEHInitializeMachExceptions: TASK PORT count %d\n", taskHandlers.m_nPorts);
                for (mach_msg_type_number_t i = 0; i < taskHandlers.m_nPorts; i++)
                {
                    NONPAL_TRACE("SEHInitializeMachExceptions: TASK PORT mask %08x handler: %08x behavior %08x flavor %u\n",
                        taskHandlers.m_masks[i],
                        taskHandlers.m_handlers[i],
                        taskHandlers.m_behaviors[i],
                        taskHandlers.m_flavors[i]);
                }
            }
            else
            {
                NONPAL_TRACE("SEHInitializeMachExceptions: task_get_exception_ports FAILED %d %s\n", machret, mach_error_string(machret));
            }
        }
#endif // _DEBUG
    }

    // We're done
    return TRUE;
}

#endif // HAVE_MACH_EXCEPTIONS
