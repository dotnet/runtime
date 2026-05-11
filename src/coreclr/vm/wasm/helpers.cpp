// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpretershared.h>
#include <interpexec.h>
#include "callhelpers.hpp"
#include "stringthunkhash.h"
#include "pregeneratedstringthunks.h"
#include "callingconvention.h"
#include "cgensys.h"
#include "readytorun.h"

void ExecuteInterpretedMethodWithArgs_PortableEntryPoint(PCODE portableEntrypoint, TransitionBlock* block, size_t argsSize, int8_t* retBuff);

// -------------------------------------------------
// Logic that will eventually mostly be pregenerated for R2R to interpreter code
// -------------------------------------------------
namespace
{
    FCDECL0(void, CallInterpreter_RetVoid);
    WASM_CALLABLE_FUNC_1(void, CallInterpreter_RetVoid, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        void * result = NULL;
        
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, 0, (int8_t*)&result);
        return;
    }
    FCDECL1(void, CallInterpreter_I32_RetVoid, int32_t);
    WASM_CALLABLE_FUNC_2(void, CallInterpreter_I32_RetVoid, int32_t arg0, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[1];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return;
    }
    FCDECL2(void, CallInterpreter_I32_I32_RetVoid, int32_t, int32_t);
    WASM_CALLABLE_FUNC_3(void, CallInterpreter_I32_I32_RetVoid, int32_t arg0, int32_t arg1, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[2];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return;
    }
    FCDECL3(void, CallInterpreter_I32_I32_I32_RetVoid, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_4(void, CallInterpreter_I32_I32_I32_RetVoid, int32_t arg0, int32_t arg1, int32_t arg2, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[3];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");


        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return;
    }
    FCDECL4(void, CallInterpreter_I32_I32_I32_I32_RetVoid, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_5(void, CallInterpreter_I32_I32_I32_I32_RetVoid, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[4];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return;
    }
    FCDECL0(int32_t, CallInterpreter_RetI32);
    WASM_CALLABLE_FUNC_1(int32_t, CallInterpreter_RetI32, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, 0, (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL1(int32_t, CallInterpreter_I32_RetI32, int32_t);
    WASM_CALLABLE_FUNC_2(int32_t, CallInterpreter_I32_RetI32, int32_t arg0, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[1];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL2(int32_t, CallInterpreter_I32_I32_RetI32, int32_t, int32_t);
    WASM_CALLABLE_FUNC_3(int32_t, CallInterpreter_I32_I32_RetI32, int32_t arg0, int32_t arg1, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[2];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL3(int32_t, CallInterpreter_I32_I32_I32_RetI32, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_4(int32_t, CallInterpreter_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[3];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL4(int32_t, CallInterpreter_I32_I32_I32_I32_RetI32, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_5(int32_t, CallInterpreter_I32_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[4];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL5(int32_t, CallInterpreter_I32_I32_I32_I32_I32_RetI32, int32_t, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_6(int32_t, CallInterpreter_I32_I32_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, int32_t arg4, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[5];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        transitionBlock.args[4] = (int64_t)arg4;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL6(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_RetI32, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_7(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, int32_t arg4, int32_t arg5, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[6];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        transitionBlock.args[4] = (int64_t)arg4;
        transitionBlock.args[5] = (int64_t)arg5;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL7(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_I32_RetI32, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_8(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, int32_t arg4, int32_t arg5, int32_t arg6, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[7];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        transitionBlock.args[4] = (int64_t)arg4;
        transitionBlock.args[5] = (int64_t)arg5;
        transitionBlock.args[6] = (int64_t)arg6;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
    FCDECL8(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_I32_I32_RetI32, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t);
    WASM_CALLABLE_FUNC_9(int32_t, CallInterpreter_I32_I32_I32_I32_I32_I32_I32_I32_RetI32, int32_t arg0, int32_t arg1, int32_t arg2, int32_t arg3, int32_t arg4, int32_t arg5, int32_t arg6, int32_t arg7, PCODE portableEntrypoint)
    {
        struct
        {
            TransitionBlock block;
            int64_t args[8];
        } transitionBlock;
        transitionBlock.block.m_ReturnAddress = 0;
        transitionBlock.block.m_StackPointer = callersStackPointer;
        transitionBlock.args[0] = (int64_t)arg0;
        transitionBlock.args[1] = (int64_t)arg1;
        transitionBlock.args[2] = (int64_t)arg2;
        transitionBlock.args[3] = (int64_t)arg3;
        transitionBlock.args[4] = (int64_t)arg4;
        transitionBlock.args[5] = (int64_t)arg5;
        transitionBlock.args[6] = (int64_t)arg6;
        transitionBlock.args[7] = (int64_t)arg7;
        static_assert(offsetof(decltype(transitionBlock), args) == sizeof(TransitionBlock), "Args array must be at a TransitionBlock offset from the start of the block");

        void * result = NULL;
        ExecuteInterpretedMethodWithArgs_PortableEntryPoint(portableEntrypoint, &transitionBlock.block, sizeof(transitionBlock.args), (int8_t*)&result);
        return (int32_t)result;
    }
}

const StringToWasmSigThunk g_wasmPortableEntryPointThunks[] = {
    { "Ivp", (void*)&CallInterpreter_RetVoid },
    { "Ivip", (void*)&CallInterpreter_I32_RetVoid },
    { "Iviip", (void*)&CallInterpreter_I32_I32_RetVoid },
    { "Iviiip", (void*)&CallInterpreter_I32_I32_I32_RetVoid },
    { "Iviiiip", (void*)&CallInterpreter_I32_I32_I32_I32_RetVoid },
    { "Iip", (void*)&CallInterpreter_RetI32 },
    { "Iiip", (void*)&CallInterpreter_I32_RetI32 },
    { "Iiiip", (void*)&CallInterpreter_I32_I32_RetI32 },
    { "Iiiiip", (void*)&CallInterpreter_I32_I32_I32_RetI32 },
    { "Iiiiiip", (void*)&CallInterpreter_I32_I32_I32_I32_RetI32 },
    { "Iiiiiiip", (void*)&CallInterpreter_I32_I32_I32_I32_I32_RetI32 },
    { "Iiiiiiiip", (void*)&CallInterpreter_I32_I32_I32_I32_I32_I32_RetI32 },
    { "Iiiiiiiiip", (void*)&CallInterpreter_I32_I32_I32_I32_I32_I32_I32_RetI32 },
    { "Iiiiiiiiiip", (void*)&CallInterpreter_I32_I32_I32_I32_I32_I32_I32_I32_RetI32 },
};

const size_t g_wasmPortableEntryPointThunksCount = sizeof(g_wasmPortableEntryPointThunks) / sizeof(g_wasmPortableEntryPointThunks[0]);
// -------------------------------------------------
// END Logic that will eventually mostly be pregenerated for R2R to interpreter code END
// -------------------------------------------------

extern "C" void STDCALL CallCountingStubCode()
{
    PORTABILITY_ASSERT("CallCountingStubCode is not implemented on wasm");
}

extern "C" void CallCountingStubCode_End()
{
    PORTABILITY_ASSERT("CallCountingStubCode_End is not implemented on wasm");
}

extern "C" void STDCALL OnCallCountThresholdReachedStub()
{
    PORTABILITY_ASSERT("OnCallCountThresholdReachedStub is not implemented on wasm");
}

extern "C" void STDCALL ThePreStub()
{
    PORTABILITY_ASSERT("ThePreStub is not implemented on wasm");
}

extern "C" void InterpreterStub()
{
    PORTABILITY_ASSERT("InterpreterStub is not implemented on wasm");
}

extern "C" UINT_PTR STDCALL GetCurrentIP(void)
{
    PORTABILITY_ASSERT("GetCurrentIP is not implemented on wasm");
    return 0;
}

extern "C" void STDMETHODCALLTYPE JIT_ProfilerEnterLeaveTailcallStub(UINT_PTR ProfilerHandle)
{
    PORTABILITY_ASSERT("JIT_ProfilerEnterLeaveTailcallStub is not implemented on wasm");
}

extern "C" PCODE STDCALL DelayLoad_MethodCallImpl(TransitionBlock* pTransitionBlock, READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT* pImportThunkEntry, uint8_t *moduleBase, int32_t rvaOfModuleFixup)
{
    Module** ppModule = (Module**)(moduleBase + rvaOfModuleFixup);
    return ExternalMethodFixupWorker(pTransitionBlock, (TADDR)(moduleBase + pImportThunkEntry->RelocOffset), -1, *ppModule);
}

extern "C" __attribute__((naked)) PCODE STDCALL DelayLoad_MethodCall(TransitionBlock* pTransitionBlock, READYTORUN_IMPORT_THUNK_PORTABLE_ENTRYPOINT* pImportThunkEntry, uint8_t *moduleBase, int32_t rvaOfModuleFixup)
{
    asm ("local.get 0\n" /* Capture pTransitionBlock onto the stack for calling DelayLoad_MethodCallImpl function. This also happens to be the callersFramePointer */
         "local.get 0\n" /* Capture callersFramePointer onto the stack for setting the __stack_pointer */
         "global.get __stack_pointer\n" /* Get current value of stack global */
         "local.set 0\n"  /* Overwrite local 0 with the previous __stack_pointer value so it can be restored after the call */
         "global.set __stack_pointer\n" /* Set stack global to the initial value of callersFramePointer, which is the current stack pointer for the interpreter call */
         "local.get 1\n" /* Load pImportThunkEntry argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "local.get 2\n" /* Load moduleBase argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "local.get 3\n" /* Load rvaOfModuleFixup argument onto the stack for calling DelayLoad_MethodCallImpl function*/
         "call %0\n" /* Call the actual implementation function */
         "local.get 0\n" /* Reload the saved previous __stack_pointer value for restoration into the stack global */
         "global.set __stack_pointer\n"
         "return" :: "i" (DelayLoad_MethodCallImpl));
}

extern "C" void STDCALL DelayLoad_Helper()
{
    PORTABILITY_ASSERT("DelayLoad_Helper is not implemented on wasm");
}

extern "C" void STDCALL DelayLoad_Helper_Obj()
{
    PORTABILITY_ASSERT("DelayLoad_Helper_Obj is not implemented on wasm");
}

extern "C" void STDCALL DelayLoad_Helper_ObjObj()
{
    PORTABILITY_ASSERT("DelayLoad_Helper_ObjObj is not implemented on wasm");
}

extern "C" void STDCALL PInvokeImportThunk()
{
    PORTABILITY_ASSERT("PInvokeImportThunk is not implemented on wasm");
}

extern "C" void STDCALL StubPrecodeCode()
{
    PORTABILITY_ASSERT("StubPrecodeCode is not implemented on wasm");
}

extern "C" void STDCALL StubPrecodeCode_End()
{
    PORTABILITY_ASSERT("StubPrecodeCode_End is not implemented on wasm");
}

extern "C" void STDCALL FixupPrecodeCode()
{
    PORTABILITY_ASSERT("FixupPrecodeCode is not implemented on wasm");
}

extern "C" void STDCALL FixupPrecodeCode_End()
{
    PORTABILITY_ASSERT("FixupPrecodeCode_End is not implemented on wasm");
}

extern "C" void RhpInitialInterfaceDispatch()
{
    PORTABILITY_ASSERT("RhpInitialInterfaceDispatch is not implemented on wasm");
}

unsigned FuncEvalFrame::GetFrameAttribs_Impl(void)
{
    PORTABILITY_ASSERT("FuncEvalFrame::GetFrameAttribs_Impl is not implemented on wasm");
    return 0;
}

TADDR FuncEvalFrame::GetReturnAddressPtr_Impl()
{
    PORTABILITY_ASSERT("FuncEvalFrame::GetReturnAddressPtr_Impl is not implemented on wasm");
    return 0;
}

void FuncEvalFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    PORTABILITY_ASSERT("FuncEvalFrame::UpdateRegDisplay_Impl is not implemented on wasm");
}

void InlinedCallFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
#ifdef PROFILING_SUPPORTED
        PRECONDITION(CORProfilerStackSnapshotEnabled() || InlinedCallFrame::FrameHasActiveCall(this));
#endif
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    if (!InlinedCallFrame::FrameHasActiveCall(this))
    {
        LOG((LF_CORDB, LL_ERROR, "WARNING: InlinedCallFrame::UpdateRegDisplay called on inactive frame %p\n", this));
        return;
    }

    pRD->pCurrentContext->InterpreterIP = *(DWORD *)&m_pCallerReturnAddress;

    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    pRD->pCurrentContext->InterpreterSP = *(DWORD *)&m_pCallSiteSP;
    pRD->pCurrentContext->InterpreterFP = *(DWORD *)&m_pCalleeSavedFP;

#define CALLEE_SAVED_REGISTER(regname) pRD->pCurrentContextPointers->regname = NULL;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER

    SyncRegDisplayToCurrentContext(pRD);

#ifdef FEATURE_INTERPRETER
    if ((m_Next != FRAME_TOP) && (m_Next->GetFrameIdentifier() == FrameIdentifier::InterpreterFrame))
    {
        // If the next frame is an interpreter frame, we also need to set the first argument register to point to the interpreter frame.
        SetFirstArgReg(pRD->pCurrentContext, dac_cast<TADDR>(m_Next));
    }
#endif // FEATURE_INTERPRETER

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    InlinedCallFrame::UpdateRegDisplay_Impl(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

void FaultingExceptionFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    PORTABILITY_ASSERT("FaultingExceptionFrame::UpdateRegDisplay_Impl is not implemented on wasm");
}

void TransitionFrame::UpdateRegDisplay_Impl(const PREGDISPLAY pRD, bool updateFloats)
{
    pRD->pCurrentContext->InterpreterIP = GetReturnAddress();
    pRD->pCurrentContext->InterpreterSP = GetSP();

    SyncRegDisplayToCurrentContext(pRD);

    LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    TransitionFrame::UpdateRegDisplay_Impl(rip:%p, rsp:%p)\n", pRD->ControlPC, pRD->SP));
}

size_t CallDescrWorkerInternalReturnAddressOffset = 0;

VOID PALAPI RtlRestoreContext(IN PCONTEXT ContextRecord, IN PEXCEPTION_RECORD ExceptionRecord)
{
    PORTABILITY_ASSERT("RtlRestoreContext is not implemented on wasm");
}

extern "C" void TheUMEntryPrestub(void)
{
    PORTABILITY_ASSERT("TheUMEntryPrestub is not implemented on wasm");
}

extern "C" void STDCALL VarargPInvokeStub(void)
{
    PORTABILITY_ASSERT("VarargPInvokeStub is not implemented on wasm");
}

extern "C" void STDCALL VarargPInvokeStub_RetBuffArg(void)
{
    PORTABILITY_ASSERT("VarargPInvokeStub_RetBuffArg is not implemented on wasm");
}

extern "C" PCODE CID_VirtualOpenDelegateDispatch(TransitionBlock * pTransitionBlock)
{
    PORTABILITY_ASSERT("CID_VirtualOpenDelegateDispatch is not implemented on wasm");
    return 0;
}

EXTERN_C void JIT_WriteBarrier_End()
{
    PORTABILITY_ASSERT("JIT_WriteBarrier_End is not implemented on wasm");
}

EXTERN_C void JIT_CheckedWriteBarrier_End()
{
    PORTABILITY_ASSERT("JIT_CheckedWriteBarrier_End is not implemented on wasm");
}

EXTERN_C void JIT_ByRefWriteBarrier_End()
{
    PORTABILITY_ASSERT("JIT_ByRefWriteBarrier_End is not implemented on wasm");
}

EXTERN_C void JIT_StackProbe_End()
{
    PORTABILITY_ASSERT("JIT_StackProbe_End is not implemented on wasm");
}

EXTERN_C VOID STDCALL ResetCurrentContext()
{
    PORTABILITY_ASSERT("ResetCurrentContext is not implemented on wasm");
}

extern "C" void STDCALL GenericPInvokeCalliHelper(void)
{
    PORTABILITY_ASSERT("GenericPInvokeCalliHelper is not implemented on wasm");
}

EXTERN_C void JIT_PInvokeBegin(InlinedCallFrame* pFrame)
{
    PORTABILITY_ASSERT("JIT_PInvokeBegin is not implemented on wasm");
}

EXTERN_C void JIT_PInvokeEnd(InlinedCallFrame* pFrame)
{
    PORTABILITY_ASSERT("JIT_PInvokeEnd is not implemented on wasm");
}

extern "C" void STDCALL JIT_StackProbe()
{
    PORTABILITY_ASSERT("JIT_StackProbe is not implemented on wasm");
}

EXTERN_C FCDECL0(void, JIT_PollGC);
FCIMPL0(void, JIT_PollGC)
{
    PORTABILITY_ASSERT("JIT_PollGC is not implemented on wasm");
}
FCIMPLEND

void InitJITHelpers1()
{
    /* no-op WASM-TODO do we need to do anything for the interpreter? */
}

extern "C" HRESULT __cdecl CorDBGetInterface(DebugInterface** rcInterface)
{
    PORTABILITY_ASSERT("CorDBGetInterface is not implemented on wasm");
    return 0;
}

extern "C" void RhpInterfaceDispatch1()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch1 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch2()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch2 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch4()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch4 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch8()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch8 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch16()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch16 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch32()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch32 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatch64()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatch64 is not implemented on wasm");
}

extern "C" void RhpVTableOffsetDispatch()
{
    PORTABILITY_ASSERT("RhpVTableOffsetDispatch is not implemented on wasm");
}

typedef uint8_t CODE_LOCATION;
CODE_LOCATION RhpAssignRefAVLocation;
CODE_LOCATION RhpCheckedAssignRefAVLocation;
CODE_LOCATION RhpByRefAssignRefAVLocation1;
CODE_LOCATION RhpByRefAssignRefAVLocation2;

extern "C" void ThisPtrRetBufPrecodeWorker()
{
    PORTABILITY_ASSERT("ThisPtrRetBufPrecodeWorker is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation1()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation1 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation2()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation2 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation4()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation4 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation8()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation8 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation16()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation16 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation32()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation32 is not implemented on wasm");
}

extern "C" void RhpInterfaceDispatchAVLocation64()
{
    PORTABILITY_ASSERT("RhpInterfaceDispatchAVLocation64 is not implemented on wasm");
}

extern "C" void RhpVTableOffsetDispatchAVLocation()
{
    PORTABILITY_ASSERT("RhpVTableOffsetDispatchAVLocation is not implemented on wasm");
}

extern "C" void STDCALL ThePreStubPatchLabel(void)
{
    PORTABILITY_ASSERT("ThePreStubPatchLabel is not implemented on wasm");
}

LONG CLRNoCatchHandler(EXCEPTION_POINTERS* pExceptionInfo, PVOID pv)
{
    PORTABILITY_ASSERT("CLRNoCatchHandler is not implemented on wasm");
    return EXCEPTION_CONTINUE_SEARCH;
}

EXTERN_C void STDMETHODCALLTYPE ProfileEnterNaked(FunctionIDOrClientID functionIDOrClientID)
{
    PORTABILITY_ASSERT("ProfileEnterNaked is not implemented on wasm");
}

EXTERN_C void STDMETHODCALLTYPE ProfileLeaveNaked(UINT_PTR clientData)
{
    PORTABILITY_ASSERT("ProfileLeaveNaked is not implemented on wasm");
}

EXTERN_C void STDMETHODCALLTYPE ProfileTailcallNaked(UINT_PTR clientData)
{
    PORTABILITY_ASSERT("ProfileTailcallNaked is not implemented on wasm");
}

void InitJITWriteBarrierHelpers()
{
    // Nothing to do - wasm has static write barriers
}

int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    // WASM-TODO: implement me
    return 0;
}

int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    // Nothing to do - wasm has static write barriers
    return SWB_PASS;
}

void FlushWriteBarrierInstructionCache()
{
    // Nothing to do - wasm has static write barriers
}

EXTERN_C Thread * JIT_InitPInvokeFrame(InlinedCallFrame *pFrame)
{
    PORTABILITY_ASSERT("JIT_InitPInvokeFrame is not implemented on wasm");
    return nullptr;
}

void _DacGlobals::Initialize()
{
    /* no-op on wasm */
}

// Incorrectly typed temporary symbol to satisfy the linker.
int g_pDebugger;

void InvokeCalliStub(CalliStubParam* pParam)
{
    _ASSERTE(pParam->ftn != (PCODE)NULL);
    _ASSERTE(pParam->cookie != NULL);

    (pParam->cookie)(pParam->ftn, pParam->pArgs, pParam->pRet);
}

void InvokeUnmanagedCalli(PCODE ftn, InterpreterCalliCookie cookie, int8_t *pArgs, int8_t *pRet)
{
    _ASSERTE(ftn != (PCODE)NULL);
    _ASSERTE(cookie != NULL);
    (cookie)(ftn, pArgs, pRet);
}

void InvokeDelegateInvokeMethod(DelegateInvokeMethodParam* pParam)
{
    PORTABILITY_ASSERT("Attempted to execute non-interpreter code from interpreter on wasm, this is not yet implemented");
}

namespace
{
    enum class ConvertType
    {
        NotConvertible,
        ToI32,
        ToI64,
        ToF32,
        ToF64,
        ToStruct,   // S<N> — multi-field struct passed by pointer, structSize holds the size
        ToEmpty,    // e — empty struct, takes no wasm argument
    };

    struct ConvertResult
    {
        ConvertType type;
        uint32_t structSize; // only meaningful when type == ToStruct
    };

    // Lowers a TypeHandle to a ConvertResult, unwrapping single-field structs
    // per the BasicCABI spec.
    ConvertResult LowerTypeHandle(TypeHandle th)
    {
        uint32_t size = th.GetSize();
        CorElementType elemType = th.GetSignatureCorElementType();

        if ((elemType != ELEMENT_TYPE_VALUETYPE) && (elemType != ELEMENT_TYPE_TYPEDBYREF))
        {
            switch (elemType)
            {
                case ELEMENT_TYPE_I4: case ELEMENT_TYPE_U4:
                case ELEMENT_TYPE_I2: case ELEMENT_TYPE_U2:
                case ELEMENT_TYPE_I1: case ELEMENT_TYPE_U1:
                case ELEMENT_TYPE_BOOLEAN: case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I: case ELEMENT_TYPE_U:
                case ELEMENT_TYPE_PTR: case ELEMENT_TYPE_BYREF:
                case ELEMENT_TYPE_FNPTR:
                case ELEMENT_TYPE_CLASS: case ELEMENT_TYPE_STRING:
                case ELEMENT_TYPE_ARRAY: case ELEMENT_TYPE_SZARRAY:
                    return { ConvertType::ToI32, 0 };
                case ELEMENT_TYPE_I8: case ELEMENT_TYPE_U8:
                    return { ConvertType::ToI64, 0 };
                case ELEMENT_TYPE_R4:
                    return { ConvertType::ToF32, 0 };
                case ELEMENT_TYPE_R8:
                    return { ConvertType::ToF64, 0 };
                default:
                    return { ConvertType::NotConvertible, 0 };
            }
        }

        MethodTable* pMT = th.AsMethodTable();
        uint32_t numInstanceFields = pMT->GetNumInstanceFields();

        // WASM-TODO: Empty structs should return ToEmpty once .NET
        // stops padding them to size 1. See runtime issue #127361.

        if (numInstanceFields == 1)
        {
            FieldDesc* pField = pMT->GetApproxFieldDescListRaw();
            TypeHandle fieldType = pField->GetApproxFieldTypeHandleThrowing();
            if (fieldType.GetSize() == size)
            {
                // Single field, no padding — unwrap recursively
                return LowerTypeHandle(fieldType);
            }
            // One field with padding — treat as multi-field struct
        }

        return { ConvertType::ToStruct, size };
    }

    ConvertResult ConvertibleTo(CorElementType argType, MetaSig& sig, bool isReturn)
    {
        // See https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md
        switch (argType)
        {
            case ELEMENT_TYPE_BOOLEAN:
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I1:
            case ELEMENT_TYPE_U1:
            case ELEMENT_TYPE_I2:
            case ELEMENT_TYPE_U2:
            case ELEMENT_TYPE_I4:
            case ELEMENT_TYPE_U4:
            case ELEMENT_TYPE_STRING:
            case ELEMENT_TYPE_PTR:
            case ELEMENT_TYPE_BYREF:
            case ELEMENT_TYPE_CLASS:
            case ELEMENT_TYPE_ARRAY:
            case ELEMENT_TYPE_I:
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_FNPTR:
            case ELEMENT_TYPE_SZARRAY:
                return { ConvertType::ToI32, 0 };
            case ELEMENT_TYPE_I8:
            case ELEMENT_TYPE_U8:
                return { ConvertType::ToI64, 0 };
            case ELEMENT_TYPE_R4:
                return { ConvertType::ToF32, 0 };
            case ELEMENT_TYPE_R8:
                return { ConvertType::ToF64, 0 };
            case ELEMENT_TYPE_TYPEDBYREF:
            case ELEMENT_TYPE_VALUETYPE:
            {
                TypeHandle vt = isReturn
                    ? sig.GetRetTypeHandleThrowing()
                    : sig.GetLastTypeHandleThrowing();
                return LowerTypeHandle(vt);
            }
            default:
                return { ConvertType::NotConvertible, 0 };
        }
    }

    // Appends the encoding for a ConvertResult to keyBuffer.
    // Returns the number of characters that would be written (even if pos >= maxSize).
    // Only writes characters while pos < maxSize.
    uint32_t AppendTypeCode(ConvertResult cr, char* keyBuffer, uint32_t pos, uint32_t maxSize)
    {
        char c;
        switch (cr.type)
        {
            case ConvertType::ToI32:       c = 'i'; break;
            case ConvertType::ToI64:       c = 'l'; break;
            case ConvertType::ToF32:       c = 'f'; break;
            case ConvertType::ToF64:       c = 'd'; break;
            case ConvertType::ToEmpty:     c = 'e'; break;
            case ConvertType::ToStruct:
            {
                // Encode as S<N> where N is the struct size in decimal
                char sizeBuf[16];
                int len = sprintf_s(sizeBuf, sizeof(sizeBuf), "S%u", cr.structSize);
                for (int j = 0; j < len; j++)
                {
                    if (pos + (uint32_t)j < maxSize)
                        keyBuffer[pos + (uint32_t)j] = sizeBuf[j];
                }
                return (uint32_t)len;
            }
            default:
                PORTABILITY_ASSERT("Unknown type");
                c = '?';
                break;
        }

        if (pos < maxSize)
            keyBuffer[pos] = c;

        return 1;
    }

    // Computes the signature key string for a MetaSig.
    // The format is documented in docs/design/coreclr/botr/readytorun-format.md
    // (section "Wasm Signature String Encoding").
    // Returns the total number of characters needed (excluding null terminator).
    // Only writes characters while pos < maxSize, so the buffer is never overflowed.
    // Callers should check if the return value >= maxSize and retry with a larger buffer.
    uint32_t GetSignatureKey(MetaSig& sig, char prefix, char* keyBuffer, uint32_t maxSize)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        uint32_t pos = 0;

        if (pos < maxSize)
            keyBuffer[pos] = prefix;
        pos++;

        if (sig.IsReturnTypeVoid())
        {
            if (pos < maxSize)
                keyBuffer[pos] = 'v';
            pos++;
        }
        else
        {
            ConvertResult cr = ConvertibleTo(sig.GetReturnType(), sig, true /* isReturn */);
            if (cr.type == ConvertType::NotConvertible)
                return UINT32_MAX;
            pos += AppendTypeCode(cr, keyBuffer, pos, maxSize);
        }

        if (sig.HasThis())
        {
            if (pos < maxSize)
                keyBuffer[pos] = 'T';
            pos++;
        }

        for (CorElementType argType = sig.NextArg();
            argType != ELEMENT_TYPE_END;
            argType = sig.NextArg())
        {
            ConvertResult cr = ConvertibleTo(argType, sig, false /* isReturn */);
            if (cr.type == ConvertType::NotConvertible)
                return UINT32_MAX;
            pos += AppendTypeCode(cr, keyBuffer, pos, maxSize);
        }

        // Add the portable entrypoint parameter
        if (sig.GetCallingConvention() == IMAGE_CEE_CS_CALLCONV_DEFAULT)
        {
            if (pos < maxSize)
                keyBuffer[pos] = 'p';
            pos++;
        }

        if (pos < maxSize)
            keyBuffer[pos] = 0;

        return pos;
    }

    typedef StringToThunkHash StringToWasmSigThunkHash;
    static StringToWasmSigThunkHash* thunkCache = nullptr;
    static StringToWasmSigThunkHash* portableEntrypointThunkCache = nullptr;

    InterpreterCalliCookie LookupThunk(const char* key)
    {
        StringToWasmSigThunkHash* table = thunkCache;
        _ASSERTE(table != nullptr && "Wasm thunk cache not initialized. Call InitializeWasmThunkCaches() at EEStartup.");
        void* thunk;
        if (table->Lookup(key, &thunk))
            return (InterpreterCalliCookie)thunk;

        PCODE r2rThunk = LookupPregeneratedThunkByString(key);
        if (r2rThunk != NULL)
            return (InterpreterCalliCookie)(size_t)r2rThunk;

        return nullptr;
    }

    void* LookupPortableEntryPointThunk(const char* key)
    {
        StringToWasmSigThunkHash* table = portableEntrypointThunkCache;
        _ASSERTE(table != nullptr && "Wasm portable entrypoint thunk cache not initialized. Call InitializeWasmThunkCaches() at EEStartup.");
        void* thunk;
        if (table->Lookup(key, &thunk))
            return thunk;

        PCODE r2rThunk = LookupPregeneratedThunkByString(key);
        if (r2rThunk != NULL)
            return (void*)(size_t)r2rThunk;

        return nullptr;
    }

    // This is a simple signature computation routine for signatures currently supported in the wasm environment.
    InterpreterCalliCookie ComputeCalliSigThunk(MetaSig& sig)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(sizeof(int32_t) == sizeof(void*));

        BYTE callConv = sig.GetCallingConvention();
        switch (callConv)
        {
            case IMAGE_CEE_CS_CALLCONV_DEFAULT:
            case IMAGE_CEE_CS_CALLCONV_C:
            case IMAGE_CEE_CS_CALLCONV_STDCALL:
            case IMAGE_CEE_CS_CALLCONV_FASTCALL:
            case IMAGE_CEE_CS_CALLCONV_UNMANAGED:
                break;
            default:
                return NULL;
        }

        char fixedBuffer[64];
        char* keyBuffer = fixedBuffer;
        uint32_t keyBufferLen = sizeof(fixedBuffer);
        uint32_t needed = GetSignatureKey(sig, 'M', keyBuffer, keyBufferLen);
        if (needed == UINT32_MAX)
            return NULL;
        if (needed >= keyBufferLen)
        {
            keyBufferLen = needed + 1;
            keyBuffer = (char*)alloca(keyBufferLen);
            sig.Reset();
            needed = GetSignatureKey(sig, 'M', keyBuffer, keyBufferLen);
            if (needed == UINT32_MAX || needed >= keyBufferLen)
                return NULL;
        }

        InterpreterCalliCookie thunk = LookupThunk(keyBuffer);
#ifdef _DEBUG
        if (thunk == NULL)
            printf("WASM calli missing for key: %s\n", keyBuffer);
#endif
        return thunk;
    }

    void* ComputePortableEntryPointToInterpreterThunk(MetaSig& sig)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(sizeof(int32_t) == sizeof(void*));

        BYTE callConv = sig.GetCallingConvention();
        switch (callConv)
        {
            // Only allowed for default calling convention since that's the only one currently supported for portable entry points, but we may want to support more in the future.
            case IMAGE_CEE_CS_CALLCONV_DEFAULT:
                break;
            default:
                return NULL;
        }

        char fixedBuffer[64];
        char* keyBuffer = fixedBuffer;
        uint32_t keyBufferLen = sizeof(fixedBuffer);
        uint32_t needed = GetSignatureKey(sig, 'I', keyBuffer, keyBufferLen);
        if (needed == UINT32_MAX)
            return NULL;
        if (needed >= keyBufferLen)
        {
            keyBufferLen = needed + 1;
            keyBuffer = (char*)alloca(keyBufferLen);
            sig.Reset();
            needed = GetSignatureKey(sig, 'I', keyBuffer, keyBufferLen);
            if (needed == UINT32_MAX || needed >= keyBufferLen)
                return NULL;
        }

        void* thunk = LookupPortableEntryPointThunk(keyBuffer);
#ifdef _DEBUG
        if (thunk == NULL)
        {
            LOG((LF_STUBS, LL_INFO100000, "WASM R2R to interpreter call missing for key: %s\n", keyBuffer));
        }
#endif
        return thunk;
    }

    ULONG GetHashCode(MethodDesc* pMD, SString &strSource)
    {
        _ASSERTE(pMD != nullptr);

        // the key is in the form $"{MethodName}#{Method.GetParameters().Length}:{AssemblyName}:{Namespace}:{TypeName}";
        const char* pszNamespace = nullptr;
        const char* pszName = pMD->GetMethodTable()->GetFullyQualifiedNameInfo(&pszNamespace);
        MetaSig sig(pMD);
        strSource.Printf("%s#%d:%s:%s:%s",
            pMD->GetName(),
            sig.NumFixedArgs(),
            pMD->GetAssembly()->GetSimpleName(),
            pszNamespace != nullptr ? pszNamespace : "",
            pszName);

        return strSource.Hash();
    }

    struct ReverseThunkMapKey
    {
        ULONG HashCode;
        const char* Source;
    };

    class ReverseThunkHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<const ReverseThunkMapEntry*>>
    {
    public:
        typedef ReverseThunkMapKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return { e->hashCode, e->Source };
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            return (k1.HashCode == k2.HashCode) && strcmp(k1.Source, k2.Source) == 0;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            return k.HashCode;
        }
    };

    typedef SHash<ReverseThunkHashTraits> ReverseThunkHash;
    ReverseThunkHash* reverseThunkCache = nullptr;

    ReverseThunkHash* CreateReverseThunkHashTable()
    {
        ReverseThunkHash* newTable = new ReverseThunkHash();
        newTable->Reallocate(g_ReverseThunksCount * ReverseThunkHash::s_density_factor_denominator / ReverseThunkHash::s_density_factor_numerator + 1);
        for (size_t i = 0; i < g_ReverseThunksCount; i++)
        {
            newTable->Add(&g_ReverseThunks[i]);
        }

        ReverseThunkHash **ppCache = &reverseThunkCache;
        if (InterlockedCompareExchangeT(ppCache, newTable, nullptr) != nullptr)
        {
            // Another thread won the race, discard ours
            delete newTable;
        }
        return *ppCache;
    }

    const ReverseThunkMapValue* LookupThunk(MethodDesc* pMD)
    {
#ifdef LOGGING
        {
            const char* pszLookupNamespace = nullptr;
            const char* pszLookupName = pMD->GetMethodTable()->GetFullyQualifiedNameInfo(&pszLookupNamespace);
            LOG((LF_STUBS, LL_INFO100000, "WASM lookupThunk pMD: %s.%s::%s\n", pszLookupNamespace ? pszLookupNamespace : "", pszLookupName, pMD->GetName()));
        }
#endif // LOGGING

        ReverseThunkHash* table = VolatileLoad(&reverseThunkCache);

        if (table == nullptr)
        {
            LOG((LF_STUBS, LL_INFO100000, "WASM creating reverse thunk hash table for the first time\n"));
            table = CreateReverseThunkHashTable();
        }

        SString source;
        ULONG hashCode = GetHashCode(pMD, source);
        ReverseThunkMapKey key = { hashCode, source.GetUTF8() };
        const ReverseThunkMapEntry* entry = table->Lookup(key);
        const ReverseThunkMapValue* thunk = entry != nullptr ? &entry->value : nullptr;
        LOG((LF_STUBS, LL_INFO100000, "WASM reverse thunk %s for key: %u source: %s\n", thunk != nullptr ? "found" : "missing", hashCode, source.GetUTF8()));

        return thunk;
    }
}

// Called at EEStartup to initialize thunk tables
void InitializeWasmThunkCaches()
{
    {
        StringToWasmSigThunkHash* newTable = new StringToWasmSigThunkHash();
        newTable->Reallocate(g_wasmThunksCount * StringToWasmSigThunkHash::s_density_factor_denominator / StringToWasmSigThunkHash::s_density_factor_numerator + 1);
        for (size_t i = 0; i < g_wasmThunksCount; i++)
        {
            newTable->Add(g_wasmThunks[i].key, g_wasmThunks[i].value);
        }
        thunkCache = newTable;
    }

    {
        StringToWasmSigThunkHash* newTable = new StringToWasmSigThunkHash();
        newTable->Reallocate(g_wasmPortableEntryPointThunksCount * StringToWasmSigThunkHash::s_density_factor_denominator / StringToWasmSigThunkHash::s_density_factor_numerator + 1);
        for (size_t i = 0; i < g_wasmPortableEntryPointThunksCount; i++)
        {
            newTable->Add(g_wasmPortableEntryPointThunks[i].key, g_wasmPortableEntryPointThunks[i].value);
        }
        portableEntrypointThunkCache = newTable;
    }
}

InterpreterCalliCookie GetCookieForCalliSig(MetaSig metaSig, MethodDesc *pContextMD)
{
    STANDARD_VM_CONTRACT;

    InterpreterCalliCookie thunk = ComputeCalliSigThunk(metaSig);
    if (thunk == NULL)
    {
        PORTABILITY_ASSERT("GetCookieForCalliSig: unknown thunk signature");
    }

    return thunk;
}

void* GetPortableEntryPointToInterpreterThunk(MethodDesc *pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pMD->ContainsGenericVariables())
    {
        return NULL;
    }

    if (pMD->HasStoredSig())
    {
        PTR_StoredSigMethodDesc pSMD = dac_cast<PTR_StoredSigMethodDesc>(pMD);
        if (pSMD->HasStoredMethodSig() || pSMD->GetClassification()==mcDynamic)
        {
            DWORD sig;
            if (pSMD->GetStoredMethodSig(&sig) == NULL)
            {
                return NULL;
            }
        }
    }

    MetaSig sig(pMD);
    void* thunk = ComputePortableEntryPointToInterpreterThunk(sig);

    return thunk;
}

void* GetUnmanagedCallersOnlyThunk(MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pMD != NULL);
    _ASSERTE(pMD->HasUnmanagedCallersOnlyAttribute());

    const ReverseThunkMapValue* value = LookupThunk(pMD);
    if (value == NULL)
    {
        PORTABILITY_ASSERT("GetUnmanagedCallersOnlyThunk: unknown thunk for unmanaged callers only method");
        return NULL;
    }

    // Update the target method if not already set.
    _ASSERTE(value->Target != NULL);
    if (NULL == (*value->Target))
        *value->Target = pMD;

    _ASSERTE((*value->Target) == pMD);
    _ASSERTE(value->EntryPoint != NULL);
    return value->EntryPoint;
}

void InvokeManagedMethod(ManagedMethodParam *pParam)
{
    InterpreterCalliCookie cookie = pParam->pMD->GetCalliCookie();
    if (cookie == NULL)
    {
        MetaSig sig(pParam->pMD);
        cookie = GetCookieForCalliSig(sig, pParam->pMD);
        _ASSERTE(cookie != NULL);
        pParam->pMD->SetCalliCookie(cookie);
        cookie = pParam->pMD->GetCalliCookie();
    }

    CalliStubParam param = { pParam->target == NULL ? pParam->pMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY) : pParam->target, cookie, pParam->pArgs, pParam->pRet, pParam->pContinuationRet };
    InvokeCalliStub(&param);
}

void InvokeUnmanagedMethod(MethodDesc *targetMethod, int8_t *pArgs, int8_t *pRet, PCODE callTarget)
{
    PORTABILITY_ASSERT("Attempted to execute unmanaged code from interpreter on wasm, this is not yet implemented");
}
