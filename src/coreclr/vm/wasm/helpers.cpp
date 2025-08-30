// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

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
    PORTABILITY_ASSERT("InlinedCallFrame::UpdateRegDisplay_Impl is not implemented on wasm");
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

extern "C" FCDECL2(VOID, JIT_WriteBarrier, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("JIT_WriteBarrier is not implemented on wasm");
}

extern "C" FCDECL2(VOID, JIT_CheckedWriteBarrier, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("JIT_CheckedWriteBarrier is not implemented on wasm");
}

extern "C" void STDCALL JIT_ByRefWriteBarrier()
{
    PORTABILITY_ASSERT("JIT_ByRefWriteBarrier is not implemented on wasm");
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

extern "C" FCDECL2(VOID, RhpAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpAssignRef is not implemented on wasm");
}

extern "C" FCDECL2(VOID, RhpCheckedAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpCheckedAssignRef is not implemented on wasm");
}

extern "C" FCDECL2(VOID, RhpByRefAssignRef, Object **dst, Object *ref)
{
    PORTABILITY_ASSERT("RhpByRefAssignRef is not implemented on wasm");
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

EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewVariableSizeObject is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewMaybeFrozen is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayFast is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewPtrArrayFast is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayFastAlign8 is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastAlign8, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFastAlign8 is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFastMisalign, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFastMisalign is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNewFast, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNewFast is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL1(Object*, RhpNew, CORINFO_CLASS_HANDLE typeHnd_)
{
    PORTABILITY_ASSERT("RhpNew is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhpNewArrayMaybeFrozen, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR size)
{
    PORTABILITY_ASSERT("RhpNewArrayMaybeFrozen is not implemented on wasm");
    return nullptr;
}

EXTERN_C FCDECL2(Object*, RhNewString, CORINFO_CLASS_HANDLE typeHnd_, INT_PTR stringLength)
{
    PORTABILITY_ASSERT("RhNewString is not implemented on wasm");
    return nullptr;
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

extern "C" int32_t mono_wasm_browser_entropy(uint8_t* buffer, int32_t bufferLength)
{
    PORTABILITY_ASSERT("mono_wasm_browser_entropy is not implemented");
    return -1;
}

void InvokeManagedMethod(MethodDesc *pMD, int8_t *pArgs, int8_t *pRet, PCODE target)
{
    PORTABILITY_ASSERT("Attempted to execute non-interpreter code from interpreter on wasm, this is not yet implemented");
}

void InvokeUnmanagedMethod(MethodDesc *targetMethod, int8_t *stack, InterpMethodContextFrame *pFrame, int32_t callArgsOffset, int32_t returnOffset, PCODE callTarget)
{
    PORTABILITY_ASSERT("Attempted to execute unmanaged code from interpreter on wasm, this is not yet implemented");
}

void InvokeCalliStub(PCODE ftn, void* cookie, int8_t *pArgs, int8_t *pRet)
{
    _ASSERTE(ftn != (PCODE)NULL);
    _ASSERTE(cookie != NULL);

    ((void(*)(PCODE, int8_t*, int8_t*))cookie)(ftn, pArgs, pRet);
}

void InvokeDelegateInvokeMethod(MethodDesc *pMDDelegateInvoke, int8_t *pArgs, int8_t *pRet, PCODE target)
{
    PORTABILITY_ASSERT("Attempted to execute non-interpreter code from interpreter on wasm, this is not yet implemented");
}

namespace
{
    void CallFuncVoidI32(PCODE ftn, int8_t *pArgs, int8_t *pRet)
    {
        void (*fn)(int32_t) = (void (*)(int32_t))ftn;
        (*fn)(((int32_t*)pArgs)[0]);
    }

    void CallFuncVoidI32I32(PCODE ftn, int8_t *pArgs, int8_t *pRet)
    {
        void (*fn)(int32_t, int32_t) = (void (*)(int32_t, int32_t))ftn;
        (*fn)(((int32_t*)pArgs)[0], ((int32_t*)pArgs)[2]);
    }

    void CallFuncVoidI32I32I32(PCODE ftn, int8_t *pArgs, int8_t *pRet)
    {
        void (*fn)(int32_t, int32_t, int32_t) = (void (*)(int32_t, int32_t, int32_t))ftn;
        (*fn)(((int32_t*)pArgs)[0], ((int32_t*)pArgs)[2], ((int32_t*)pArgs)[4]);
    }
}

LPVOID GetCookieForCalliSig(MetaSig* pMetaSig)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(CheckPointer(pMetaSig));
    _ASSERTE(pMetaSig->IsReturnTypeVoid());

    int32_t numArgs = pMetaSig->NumFixedArgs();
    switch (numArgs)
    {
        case 1: return (LPVOID)&CallFuncVoidI32;
        case 2: return (LPVOID)&CallFuncVoidI32I32;
        case 3: return (LPVOID)&CallFuncVoidI32I32I32;
        default:
            PORTABILITY_ASSERT("GetCookieForCalliSig: more than 3 arguments needs to be implemented");
            break;
    }

    return NULL;
}
