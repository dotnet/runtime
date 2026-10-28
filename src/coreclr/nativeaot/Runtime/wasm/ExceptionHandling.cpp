// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "slist.h"
#include "RuntimeInstance.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "CommonMacros.inl"
#include "WasmCodeManager.h"
#include "ExceptionHandling.h"

// File-local WebAssembly exception tag used only by RhpCallCatchFunclet. Carries
// no payload; the resume IP is communicated via the JIT-managed resumeIP local.
asm(".globl __nativeaot_wasm_restore_context_tag\n"
    ".tagtype __nativeaot_wasm_restore_context_tag\n"
    "__nativeaot_wasm_restore_context_tag:\n");

EXTERN_C void RhThrowEx(uintptr_t sp, Object* exception, ExInfo* pExInfo);
EXTERN_C void RhRethrow(uintptr_t sp, ExInfo* pActiveExInfo, ExInfo* pExInfo);

__attribute__((naked)) void ThrowRestoreContextTag()
{
    asm("throw __nativeaot_wasm_restore_context_tag\n"
        "unreachable\n" ::);
}

static uintptr_t GetFuncletAddress(void* virtualIP)
{
    WasmCodeManager* pCodeManager = static_cast<WasmCodeManager*>(
        GetRuntimeInstance()->GetCodeManagerForAddress(virtualIP));
    ASSERT(pCodeManager != nullptr);
    return pCodeManager->GetFuncletAddress(reinterpret_cast<uintptr_t>(virtualIP));
}

// Native-stack storage for the ExInfo and throw-site context. Both remain live
// while the managed dispatcher and funclets run, until catch resumption unwinds them.
struct alignas(16) WasmThrowFrame
{
    ExInfo m_exInfo;
    PAL_LIMITED_CONTEXT m_context;
};

// RhThrowEx/RhRethrow need a bridge at their incoming managed SP so stack walks
// can reconnect to callerSP. Allocate it below the actual native SP to keep
// their managed frames below all live native locals.
static __attribute__((naked, noreturn)) void CallThrowHelper(
    Object* exception, ExInfo* pActiveExInfo, ExInfo* pExInfo, uintptr_t callerSP, bool rethrow)
{
    asm("global.get      __stack_pointer\n" // Allocate space for the throw-site bridge
        "i32.const       %[frameSize]\n"
        "i32.sub\n"
        "global.set      __stack_pointer\n"

        "global.get      __stack_pointer\n"
        "i32.const       %[identity]\n"
        "i32.store       %[identityOffset]\n" // Mark this frame as a runtime EH bridge
        "global.get      __stack_pointer\n"
        "i32.const       %[throwIP]\n"
        "i32.store       %[returnIPOffset]\n" // Identify the throw-site thunk to stack walks
        "global.get      __stack_pointer\n"
        "local.get       3\n"
        "i32.store       %[callerSPOffset]\n" // Save the managed caller SP for unwinding through the bridge
        "global.get      __stack_pointer\n"
        "i32.const       0\n"
        "i32.store       %[paddingOffset]\n" // padding

        "local.get       4\n" // Select throw or rethrow dispatch
        "if\n"
        "global.get      __stack_pointer\n" // SP points to the throw-site bridge
        "local.get       1\n"               // currently active ExInfo
        "local.get       2\n"               // address of the new ExInfo
        "call            %[rethrowHelper]\n"
        "else\n"
        "global.get      __stack_pointer\n" // SP points to the throw-site bridge
        "local.get       0\n"               // exception object
        "local.get       2\n"               // address of the ExInfo
        "call            %[throwHelper]\n"
        "end_if\n"
        "unreachable\n" // no return
        :: [frameSize] "i"(sizeof(WasmEHFrame)),
           [identity] "i"(WasmEHFrame::Identity),
           [throwIP] "i"(StackFrameIterator::ThrowSiteThunkIP),
           [identityOffset] "i"(offsetof(WasmEHFrame, m_identity)),
           [returnIPOffset] "i"(offsetof(WasmEHFrame, m_returnIP)),
           [callerSPOffset] "i"(offsetof(WasmEHFrame, m_callerSP)),
           [paddingOffset] "i"(offsetof(WasmEHFrame, m_padding)),
           [rethrowHelper] "i"(RhRethrow),
           [throwHelper] "i"(RhThrowEx)
        : "memory");
}

static void ThrowException(uintptr_t callerSP, Object* exception, bool rethrow, bool exact)
{
    // Allocate space for the ExInfo and context; init the exception object to null.
    WasmThrowFrame frame = {};
    frame.m_context.SP = callerSP; // save the SP of the throw site
    frame.m_context.FP = WasmCodeManager::GetFramePointer(callerSP);
    frame.m_context.IP = WasmCodeManager::GetVirtualIP(callerSP);
    // set the exception context field on the ExInfo
    frame.m_exInfo.m_pExContext = &frame.m_context;
    frame.m_exInfo.m_kind =
        rethrow ? EK_None : // init to a deterministic value (ExKind.None)
        exact ? EK_RethrowFlag :
        EK_Throw;
    frame.m_exInfo.m_passNumber = 1; // init to the first pass
    frame.m_exInfo.m_idxCurClause = UINT32_MAX; // no clause is being invoked
    Thread* pThread = ThreadStore::GetCurrentThread();
    ExInfo* pActiveExInfo = pThread->GetCurExInfo(); // currently active ExInfo
    ASSERT(!rethrow || pActiveExInfo != nullptr);
    // link the ExInfo into the thread's ExInfo chain
    pThread->PushExInfo(&frame.m_exInfo);
    CallThrowHelper(exception, pActiveExInfo, &frame.m_exInfo, callerSP, rethrow);
}

// These CallFunclet functions are used to call funclets from the VM.
// They set up a new stack frame for calling a function which provides a NativeAOT EH bridge so that unwinds from the funclet reach the exception dispatcher.
// then invoke the new funclet with SP pointing to the top of the new frame.
// There are two versions of the function, one for funclets that need to pass a Throwable (catch/filter), and one for those that do not (finally/fault)
// Unlike CoreCLR's terminator-only frame, the bridge records the invocation kind and managed caller SP so stack walks can continue through the dispatcher.
__attribute__((naked)) uintptr_t CallFuncletWithThrowable(
    uintptr_t pFuncletToInvoke, uintptr_t fp, Object* pThrowable, uintptr_t callerSP, uintptr_t returnIP)
{
    asm ("global.get      __stack_pointer\n" // Capture original value of stack pointer
         "global.get      __stack_pointer\n" // Allocate the EH bridge below the native stack pointer.
         "i32.const       %[frameSize]\n"
         "i32.sub \n"
         "global.set      __stack_pointer\n" // Update the global stack pointer
         "global.get      __stack_pointer\n"
         "i32.const       %[identity]\n"
         "i32.store       %[identityOffset]\n" // Mark this frame as a runtime EH bridge
         "global.get      __stack_pointer\n"
         "local.get       4\n"
         "i32.store       %[returnIPOffset]\n" // Distinguish catch invocation from filter invocation
         "global.get      __stack_pointer\n"
         "local.get       3\n"
         "i32.store       %[callerSPOffset]\n" // Save the managed caller SP for unwinding through the bridge
         "global.get      __stack_pointer\n"
         "i32.const       0\n"
         "i32.store       %[paddingOffset]\n" // padding

         "global.get      __stack_pointer\n" // Get the stack pointer
         "local.get       1\n"               // Get the frame pointer argument for the original function
         "local.get       2\n"               // Get the exception object for the funclet
         "local.get       0\n"               // Get the funclet address to call
         "call_indirect    __indirect_function_table, (i32, i32, i32) -> (i32)\n"
         "local.set       0\n"               // Capture the return into a local so that we can return it after restoring the original stack pointer
         "global.set      __stack_pointer\n" // Restore the original stack pointer before returning to the caller of the funclet
         "local.get       0\n"               // Return the value from the funclet call
         "return"
          :: [frameSize] "i"(sizeof(WasmEHFrame)),
             [identity] "i"(WasmEHFrame::Identity),
             [identityOffset] "i"(offsetof(WasmEHFrame, m_identity)),
             [returnIPOffset] "i"(offsetof(WasmEHFrame, m_returnIP)),
             [callerSPOffset] "i"(offsetof(WasmEHFrame, m_callerSP)),
             [paddingOffset] "i"(offsetof(WasmEHFrame, m_padding)));
}

// Finally/fault version of the CallFunclet function which does not need to pass a Throwable
__attribute__((naked)) uintptr_t CallFuncletWithoutThrowable(
    uintptr_t pFuncletToInvoke, uintptr_t fp, uintptr_t callerSP)
{
    asm ("global.get      __stack_pointer\n" // Capture original value of stack pointer
         "global.get      __stack_pointer\n" // Allocate the EH bridge below the native stack pointer.
         "i32.const       %[frameSize]\n"
         "i32.sub \n"
         "global.set      __stack_pointer\n" // Update the global stack pointer
         "global.get      __stack_pointer\n"
         "i32.const       %[identity]\n"
         "i32.store       %[identityOffset]\n" // Mark this frame as a runtime EH bridge
         "global.get      __stack_pointer\n"
         "i32.const       %[funcletIP]\n"
         "i32.store       %[returnIPOffset]\n" // Identify exceptional finally/fault invocation
         "global.get      __stack_pointer\n"
         "local.get       2\n"
         "i32.store       %[callerSPOffset]\n" // Save the managed caller SP for unwinding through the bridge
         "global.get      __stack_pointer\n"
         "i32.const       0\n"
         "i32.store       %[paddingOffset]\n" // padding

         "global.get      __stack_pointer\n" // Get the stack pointer
         "local.get       1\n"               // Get the frame pointer argument for the original function
         "local.get       0\n"               // Get the funclet address to call
         "call_indirect    __indirect_function_table, (i32, i32) -> ()\n"
         "global.set      __stack_pointer\n" // Restore the original stack pointer before returning to the caller of the funclet
         "i32.const       0\n"
         "return\n"
          :: [frameSize] "i"(sizeof(WasmEHFrame)),
             [identity] "i"(WasmEHFrame::Identity),
             [funcletIP] "i"(StackFrameIterator::FuncletInvokeThunkIP),
             [identityOffset] "i"(offsetof(WasmEHFrame, m_identity)),
             [returnIPOffset] "i"(offsetof(WasmEHFrame, m_returnIP)),
             [callerSPOffset] "i"(offsetof(WasmEHFrame, m_callerSP)),
             [paddingOffset] "i"(offsetof(WasmEHFrame, m_padding)));
}

FCIMPL1(void, RhpThrowEx, Object* exception)
{
    ThrowException(callersStackPointer, exception, false, false);
}
FCIMPLEND

// Similar to RhpThrowEx, except that it sets the rethrow flag
FCIMPL1(void, RhpThrowExact, Object* exception)
{
    ThrowException(callersStackPointer, exception, false, true);
}
FCIMPLEND

// Similar to RhpThrowEx, except that it passes along the currently active ExInfo
FCIMPL0(void, RhpRethrow)
{
    ThrowException(callersStackPointer, nullptr, true, false);
}
FCIMPLEND

FCIMPL2(void, RhpCallFinallyFunclet, void* handlerIP, REGDISPLAY* pRegisterSet)
{
    Thread* pThread = ThreadStore::GetCurrentThread();

    // We want to suppress GC between invocations of subsequent finallys. We do this because we
    // cannot tolerate a GC after one finally has run (and possibly side-effected the GC state of the
    // method) and then been popped off the stack, leaving behind no trace of its effect.
    //
    // So we clear the state before and set it after invocation of the handler.
    pThread->ClearDoNotTriggerGc();
    uintptr_t handler = GetFuncletAddress(handlerIP);
    // The parent FP gives the handler access to the parent method's locals.
    CallFuncletWithoutThrowable(handler, pRegisterSet->GetFP(), callersStackPointer);
    pThread->SetDoNotTriggerGc();
}
FCIMPLEND

FCIMPL3(FC_BOOL_RET, RhpCallFilterFunclet, Object* exception, void* filterIP, REGDISPLAY* pRegisterSet)
{
    uintptr_t filter = GetFuncletAddress(filterIP);
    // Pass the parent FP and exception object; return the result of the filter execution.
    FC_RETURN_BOOL(CallFuncletWithThrowable(
        filter, pRegisterSet->GetFP(), exception, callersStackPointer, StackFrameIterator::FilterFuncletInvokeThunkIP) != 0);
}
FCIMPLEND

FCIMPL4(void*, RhpCallCatchFunclet, Object* exception, void* handlerIP, REGDISPLAY* pRegisterSet, ExInfo* pExInfo)
{
    Thread* pThread = ThreadStore::GetCurrentThread();
    // Clear the DoNotTriggerGc state before calling out to our managed catch funclet.
    pThread->ClearDoNotTriggerGc();
    uintptr_t handler = GetFuncletAddress(handlerIP);
    // Ignore the funclet's return value; the JIT-managed resumeIP local carries the continuation.
    CallFuncletWithThrowable(handler, pRegisterSet->GetFP(), exception, callersStackPointer, StackFrameIterator::FuncletInvokeThunkIP);
    // Pop the current ExInfo and any superseded records below the resume SP.
    pThread->PopExInfo(pExInfo, pRegisterSet->GetSP());
    // Resuming managed code at a catch continuation is done by throwing a native exception tag.
    ThrowRestoreContextTag();
    __builtin_unreachable();
}
FCIMPLEND
