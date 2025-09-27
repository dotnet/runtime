// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <interpretershared.h>

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

extern "C" void STDCALL DelayLoad_MethodCall()
{
    PORTABILITY_ASSERT("DelayLoad_MethodCall is not implemented on wasm");
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

extern "C" void STDCALL JIT_PatchedCodeLast()
{
    PORTABILITY_ASSERT("JIT_PatchedCodeLast is not implemented on wasm");
}

extern "C" void STDCALL JIT_PatchedCodeStart()
{
    PORTABILITY_ASSERT("JIT_PatchedCodeStart is not implemented on wasm");
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
    PORTABILITY_ASSERT("TransitionFrame::UpdateRegDisplay_Impl is not implemented on wasm");
}

size_t CallDescrWorkerInternalReturnAddressOffset;

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

extern "C" FCDECL2(VOID, JIT_WriteBarrier_Callable, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("JIT_WriteBarrier_Callable is not implemented on wasm");
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

EXTERN_C FCDECL0(void, JIT_PollGC)
{
    PORTABILITY_ASSERT("JIT_PollGC is not implemented on wasm");
}

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

void InvokeCalliStub(PCODE ftn, void* cookie, int8_t *pArgs, int8_t *pRet)
{
    _ASSERTE(ftn != (PCODE)NULL);
    _ASSERTE(cookie != NULL);

    PCODE actualFtn = (PCODE)PortableEntryPoint::GetActualCode(ftn);
    ((void(*)(PCODE, int8_t*, int8_t*))cookie)(actualFtn, pArgs, pRet);
}

void InvokeUnmanagedCalli(PCODE ftn, void *cookie, int8_t *pArgs, int8_t *pRet)
{
    _ASSERTE(ftn != (PCODE)NULL);
    _ASSERTE(cookie != NULL);

    // WASM-TODO: Reconcile calling conventions.
    ((void(*)(PCODE, int8_t*, int8_t*))cookie)(ftn, pArgs, pRet);
}

void InvokeDelegateInvokeMethod(MethodDesc *pMDDelegateInvoke, int8_t *pArgs, int8_t *pRet, PCODE target)
{
    PORTABILITY_ASSERT("Attempted to execute non-interpreter code from interpreter on wasm, this is not yet implemented");
}

namespace
{
    // Arguments are passed on the stack with each argument aligned to INTERP_STACK_SLOT_SIZE.
#define ARG_IND(i) ((int32_t)((int32_t*)(pArgs + (i * INTERP_STACK_SLOT_SIZE))))
#define ARG(i) (*(int32_t*)ARG_IND(i))

    void CallFunc_Void_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(void) = (void (*)(void))pcode;
        (*fptr)();
    }

    void CallFunc_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t) = (void (*)(int32_t))pcode;
        (*fptr)(ARG(0));
    }

    void CallFunc_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t) = (void (*)(int32_t, int32_t))pcode;
        (*fptr)(ARG(0), ARG(1));
    }

    void CallFunc_I32_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG(0), ARG(1), ARG(2));
    }

    void CallFunc_I32_I32_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG(0), ARG(1), ARG(2), ARG(3));
    }

    void CallFunc_I32_I32_I32_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG(0), ARG(1), ARG(2), ARG(3), ARG(4));
    }

    void CallFunc_I32_I32_I32_I32_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG(0), ARG(1), ARG(2), ARG(3), ARG(4), ARG(5));
    }

    void CallFunc_Void_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(void) = (int32_t (*)(void))pcode;
        *(int32_t*)pRet = (*fptr)();
    }

    void CallFunc_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t) = (int32_t (*)(int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG(0));
    }

    void CallFunc_I32_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t) = (int32_t (*)(int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG(0), ARG(1));
    }

    void CallFunc_I32_I32_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t, int32_t) = (int32_t (*)(int32_t, int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG(0), ARG(1), ARG(2));
    }

    void CallFunc_I32_I32_I32_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t, int32_t, int32_t) = (int32_t (*)(int32_t, int32_t, int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG(0), ARG(1), ARG(2), ARG(3));
    }

    // Special thunks for signatures with indirect arguments.

    void CallFunc_I32IND_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t) = (void (*)(int32_t))pcode;
        (*fptr)(ARG_IND(0));
    }


    void CallFunc_I32IND_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t) = (void (*)(int32_t, int32_t))pcode;
        (*fptr)(ARG_IND(0), ARG(1));
    }

    void CallFunc_I32IND_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG_IND(0), ARG(1), ARG(2));
    }

    void CallFunc_I32IND_I32_I32_I32_I32_I32_I32_RetVoid(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        void (*fptr)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t, int32_t))pcode;
        (*fptr)(ARG_IND(0), ARG(1), ARG(2), ARG(3), ARG(4), ARG(5), ARG(6));
    }

    void CallFunc_I32IND_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t) = (int32_t (*)(int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG_IND(0), ARG(1));
    }

    void CallFunc_I32_I32IND_I32_I32IND_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t, int32_t, int32_t, int32_t) = (int32_t (*)(int32_t, int32_t, int32_t, int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG(0), ARG_IND(1), ARG(2), ARG_IND(3), ARG(4));
    }

    void CallFunc_I32IND_I32_I32_I32_I32_I32_RetI32(PCODE pcode, int8_t *pArgs, int8_t *pRet)
    {
        int32_t (*fptr)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t) = (int32_t (*)(int32_t, int32_t, int32_t, int32_t, int32_t, int32_t))pcode;
        *(int32_t*)pRet = (*fptr)(ARG_IND(0), ARG(1), ARG(2), ARG(3), ARG(4), ARG(5));
    }

#undef ARG

    void* const RetVoidThunks[] =
    {
        (void*)&CallFunc_Void_RetVoid,
        (void*)&CallFunc_I32_RetVoid,
        (void*)&CallFunc_I32_I32_RetVoid,
        (void*)&CallFunc_I32_I32_I32_RetVoid,
        (void*)&CallFunc_I32_I32_I32_I32_RetVoid,
        (void*)&CallFunc_I32_I32_I32_I32_I32_RetVoid,
        (void*)&CallFunc_I32_I32_I32_I32_I32_I32_RetVoid,
    };

    void* const RetI32Thunks[] =
    {
        (void*)&CallFunc_Void_RetI32,
        (void*)&CallFunc_I32_RetI32,
        (void*)&CallFunc_I32_I32_RetI32,
        (void*)&CallFunc_I32_I32_I32_RetI32,
        (void*)&CallFunc_I32_I32_I32_I32_RetI32,
    };

    enum class ConvertType
    {
        NotConvertible,
        ToI32,
        ToI32Indirect
    };

    ConvertType ConvertibleTo(CorElementType argType, MetaSig& sig, bool isReturn)
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
                return ConvertType::ToI32;
            case ELEMENT_TYPE_TYPEDBYREF:
                // Typed references are passed indirectly in WASM since they are larger than pointer size.
                return ConvertType::ToI32Indirect;
            case ELEMENT_TYPE_VALUETYPE:
            {
                // In WASM, values types that are larger than pointer size or have multiple fields are passed indirectly.
                // WASM-TODO: Single fields may not always be passed as i32. Floats and doubles are passed as f32 and f64 respectively.
                TypeHandle vt = isReturn
                    ? sig.GetRetTypeHandleThrowing()
                    : sig.GetLastTypeHandleThrowing();

                if (!vt.IsTypeDesc()
                    && vt.AsMethodTable()->GetNumInstanceFields() >= 2)
                {
                    return ConvertType::ToI32Indirect;
                }

                return vt.GetSize() <= sizeof(uint32_t)
                    ? ConvertType::ToI32
                    : ConvertType::ToI32Indirect;
            }
            default:
                return ConvertType::NotConvertible;
        }
    }

    void* ComputeCalliSigThunkSpecial(bool isVoidReturn, uint32_t numArgs, ConvertType* args)
    {
        STANDARD_VM_CONTRACT;

        if (isVoidReturn)
        {
            switch(numArgs)
            {
                case 1:
                    if (args[0] == ConvertType::ToI32Indirect)
                    {
                        return (void*)&CallFunc_I32IND_RetVoid;
                    }
                    break;
                case 2:
                    if (args[0] == ConvertType::ToI32Indirect &&
                        args[1] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32IND_I32_RetVoid;
                    }
                    break;
                case 3:
                    if (args[0] == ConvertType::ToI32Indirect &&
                        args[1] == ConvertType::ToI32 &&
                        args[2] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32IND_I32_I32_RetVoid;
                    }
                    break;
                case 7:
                    if (args[0] == ConvertType::ToI32Indirect &&
                        args[1] == ConvertType::ToI32 &&
                        args[2] == ConvertType::ToI32 &&
                        args[3] == ConvertType::ToI32 &&
                        args[4] == ConvertType::ToI32 &&
                        args[5] == ConvertType::ToI32 &&
                        args[6] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32IND_I32_I32_I32_I32_I32_I32_RetVoid;
                    }
                    break;
            }
        }
        else
        {
            switch (numArgs) {
                case 2:
                    if (args[0] == ConvertType::ToI32Indirect &&
                        args[1] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32IND_I32_RetI32;
                    }
                    break;
                case 5:
                    if (args[0] == ConvertType::ToI32 &&
                        args[1] == ConvertType::ToI32Indirect &&
                        args[2] == ConvertType::ToI32 &&
                        args[3] == ConvertType::ToI32Indirect &&
                        args[4] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32_I32IND_I32_I32IND_I32_RetI32;
                    }
                    break;
                case 6:
                    if (args[0] == ConvertType::ToI32Indirect &&
                        args[1] == ConvertType::ToI32 &&
                        args[2] == ConvertType::ToI32 &&
                        args[3] == ConvertType::ToI32 &&
                        args[4] == ConvertType::ToI32 &&
                        args[5] == ConvertType::ToI32)
                    {
                        return (void*)&CallFunc_I32IND_I32_I32_I32_I32_I32_RetI32;
                    }
                    break;
            }
        }

        return NULL;
    }

    // This is a simple signature computation routine for signatures currently supported in the wasm environment.
    // Note: Currently only validates void return type and i32 wasm convertible arguments.
    void* ComputeCalliSigThunk(MetaSig& sig)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(sizeof(int32_t) == sizeof(void*));

        // Ensure an unmanaged calling convention.
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

        // Check return value. We only support void or i32 return types for now.
        bool returnsVoid = sig.IsReturnTypeVoid();
        if (!returnsVoid && ConvertibleTo(sig.GetReturnType(), sig, true /* isReturn */) != ConvertType::ToI32)
            return NULL;

        uint32_t numArgs = sig.NumFixedArgs() + (sig.HasThis() ? 1 : 0);
        ConvertType args[16];
        _ASSERTE(numArgs < ARRAY_SIZE(args));

        uint32_t i = 0;

        if (sig.HasThis())
        {
            args[i++] = ConvertType::ToI32;
        }

        // Ensure all arguments are wasm i32 compatible types.
        for (CorElementType argType = sig.NextArg();
            argType != ELEMENT_TYPE_END;
            argType = sig.NextArg())
        {
            // If we have no conversion, immediately return.
            ConvertType type = ConvertibleTo(argType, sig, false /* isReturn */);
            if (type == ConvertType::NotConvertible)
                return NULL;

            args[i++] = type;
        }

        // Check for homogeneous i32 argument types.
        for (uint32_t j = 0; j < numArgs; j++)
        {
            if (args[j] != ConvertType::ToI32)
                return ComputeCalliSigThunkSpecial(returnsVoid, numArgs, args);
        }

        void* const * thunks;
        if (returnsVoid)
        {
            thunks = RetVoidThunks;
            if (numArgs >= ARRAY_SIZE(RetVoidThunks))
                return NULL;
        }
        else
        {
            thunks = RetI32Thunks;
            if (numArgs >= ARRAY_SIZE(RetI32Thunks))
                return NULL;
        }

        return thunks[numArgs];
    }
}

LPVOID GetCookieForCalliSig(MetaSig metaSig)
{
    STANDARD_VM_CONTRACT;

    void* thunk = ComputeCalliSigThunk(metaSig);
    if (thunk == NULL)
    {
        PORTABILITY_ASSERT("GetCookieForCalliSig: unknown thunk signature");
    }

    return thunk;
}

void InvokeManagedMethod(MethodDesc *pMD, int8_t *pArgs, int8_t *pRet, PCODE target)
{
    MetaSig sig(pMD);
    void* cookie = GetCookieForCalliSig(sig);

    _ASSERTE(cookie != NULL);

    InvokeCalliStub(target == NULL ? pMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY) : target, cookie, pArgs, pRet);
}

void InvokeUnmanagedMethod(MethodDesc *targetMethod, int8_t *pArgs, int8_t *pRet, PCODE callTarget)
{
    PORTABILITY_ASSERT("Attempted to execute unmanaged code from interpreter on wasm, this is not yet implemented");
}
