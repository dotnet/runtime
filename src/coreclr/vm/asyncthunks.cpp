// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: asyncthunks.cpp
//

// ===========================================================================
// This file contains the implementation for creating and using prestubs
// ===========================================================================
//

#include "common.h"

bool MethodDesc::TryGenerateAsyncThunk(DynamicResolver** resolver, COR_ILMETHOD_DECODER** methodILDecoder)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(resolver != NULL);
    _ASSERTE(methodILDecoder != NULL);
    _ASSERTE(*resolver == NULL && *methodILDecoder == NULL);
    _ASSERTE(IsIL());
    _ASSERTE(!HasILHeader());

    if (!IsAsyncThunkMethod())
    {
        return false;
    }

    MethodDesc* pAsyncOtherVariant = nullptr;
    if (!IsAsyncMethod())
    {
        // a non-async thunk is implemented in terms of the async variant which has user code
        pAsyncOtherVariant = this->GetAsyncVariant();
    }
    else
    {
        _ASSERTE(IsReturnDroppingThunk());
        // this is a special void-returning async variant that calls
        // the normal async variant and drops the result
        pAsyncOtherVariant = this->GetAsyncVariant();
    }

    _ASSERTE(!IsWrapperStub() && !pAsyncOtherVariant->IsWrapperStub());

    MetaSig msig(this);

    SigTypeContext sigContext(pAsyncOtherVariant);
    ILStubLinker sl(
        GetModule(),
        GetSignature(),
        &sigContext,
        pAsyncOtherVariant,
        (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

    if (!IsAsyncMethod())
    {
        EmitTaskReturningThunk(pAsyncOtherVariant, msig, &sl);
    }
    else
    {
        _ASSERTE(IsReturnDroppingThunk());
        EmitReturnDroppingThunk(pAsyncOtherVariant, msig, &sl);
    }

    NewHolder<ILStubResolver> ilResolver = new ILStubResolver();
    // Initialize the resolver target details.
    ilResolver->SetStubMethodDesc(this);
    ilResolver->SetStubTargetMethodDesc(pAsyncOtherVariant);

    // Generate all IL associated data for JIT
    *methodILDecoder = ilResolver->FinalizeILStub(&sl);
    *resolver = ilResolver.Extract();
    return true;
}

// Provided an async method, emits a Task-returning wrapper.
// The emitted code matches method EmitTaskReturningThunk in the Managed Type System.
void MethodDesc::EmitTaskReturningThunk(MethodDesc* pAsyncCallVariant, MetaSig& thunkMsig, ILStubLinker* pSL)
{
    _ASSERTE(!pAsyncCallVariant->IsAsyncThunkMethod());

    // Emits roughly the following code:
    //
    // RuntimeAsyncStackState stackState;
    // ref RuntimeAsyncAwaitState awaitState = ref AsyncHelpers.t_runtimeAsyncAwaitState;
    // awaitState.Push(&stackState);
    //
    // try
    // {
    //   try
    //   {
    //     T result = Inner(args);
    //     // call an intrisic to see if the call above produced a continuation
    //     if (AsyncHelpers.AsyncCallContinuation() == null)
    //       return Task.FromResult(result);
    //
    //     return CreateRuntimeAsyncTask(ref awaitState);
    //   }
    //   catch (Exception ex)
    //   {
    //     return TaskFromException(ex);
    //   }
    // }
    // finally
    // {
    //   awaitState.Pop();
    // }

    ILCodeStream* pCode = pSL->NewCodeStream(ILStubLinker::kDispatch);

    TypeHandle thTaskRet = thunkMsig.GetRetTypeHandleThrowing();

    bool isValueTask = thTaskRet.GetMethodTable()->IsValueType();

    TypeHandle thLogicalRetType;
    DWORD logicalResultLocal = UINT_MAX;
    if (thTaskRet.GetNumGenericArgs() > 0)
    {
        thLogicalRetType = thTaskRet.GetMethodTable()->GetInstantiation()[0];
        logicalResultLocal = pCode->NewLocal(LocalDesc(thLogicalRetType));
    }

    LocalDesc returnLocalDesc(thTaskRet);
    DWORD returnTaskLocal = pCode->NewLocal(returnLocalDesc);

    LocalDesc stackStateLocalDesc(TypeHandle(CoreLibBinder::GetClass(CLASS__RUNTIME_ASYNC_STACK_STATE)));
    DWORD stackStateLocal = pCode->NewLocal(stackStateLocalDesc);

    LocalDesc refAwaitStateLocalDesc(TypeHandle(CoreLibBinder::GetClass(CLASS__RUNTIME_ASYNC_AWAIT_STATE)));
    refAwaitStateLocalDesc.MakeByRef();
    DWORD refAwaitStateLocal = pCode->NewLocal(refAwaitStateLocalDesc);

    ILCodeLabel* returnTaskLabel = pCode->NewCodeLabel();
    ILCodeLabel* suspendedLabel = pCode->NewCodeLabel();
    ILCodeLabel* finishedLabel = pCode->NewCodeLabel();

    pCode->EmitLDSFLDA(pCode->GetToken(CoreLibBinder::GetField(FIELD__ASYNC_HELPERS__TLS_RUNTIME_ASYNC_AWAIT_STATE)));
    pCode->EmitSTLOC(refAwaitStateLocal);

    pCode->EmitLDLOC(refAwaitStateLocal);
    pCode->EmitLDLOCA(stackStateLocal);
    pCode->EmitCALL(METHOD__RUNTIME_ASYNC_AWAIT_STATE__PUSH, 2, 0);

    {
        pCode->BeginTryBlock();
        pCode->EmitNOP("Separate try blocks");
        {
            pCode->BeginTryBlock();

            DWORD localArg = 0;
            if (thunkMsig.HasThis())
            {
                pCode->EmitLDARG(localArg++);
            }

            for (UINT iArg = 0; iArg < thunkMsig.NumFixedArgs(); iArg++)
            {
                pCode->EmitLDARG(localArg++);
            }

            int token = GetTokenForThunkTarget(pCode, pAsyncCallVariant);

            pCode->EmitCALL(token, localArg, logicalResultLocal != UINT_MAX ? 1 : 0);

            if (logicalResultLocal != UINT_MAX)
                pCode->EmitSTLOC(logicalResultLocal);
            pCode->EmitCALL(METHOD__ASYNC_HELPERS__ASYNC_CALL_CONTINUATION, 0, 1);
            pCode->EmitBRFALSE(finishedLabel);

            pCode->EmitLEAVE(suspendedLabel);

            pCode->EmitLabel(finishedLabel);
            if (logicalResultLocal != UINT_MAX)
            {
                pCode->EmitLDLOC(logicalResultLocal);
                MethodDesc* md;
                if (isValueTask)
                    md = CoreLibBinder::GetMethod(METHOD__VALUETASK__FROM_RESULT_T);
                else
                    md = CoreLibBinder::GetMethod(METHOD__TASK__FROM_RESULT_T);
                md = FindOrCreateAssociatedMethodDesc(md, md->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);

                int fromResultToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, md);
                pCode->EmitCALL(fromResultToken, 1, 1);
            }
            else
            {
                if (isValueTask)
                    pCode->EmitCALL(METHOD__VALUETASK__GET_COMPLETED_TASK, 0, 1);
                else
                    pCode->EmitCALL(METHOD__TASK__GET_COMPLETED_TASK, 0, 1);
            }
            pCode->EmitSTLOC(returnTaskLocal);
            pCode->EmitLEAVE(returnTaskLabel);

            pCode->EndTryBlock();
        }
        // Catch
        {
            pCode->BeginCatchBlock(pCode->GetToken(CoreLibBinder::GetClass(CLASS__EXCEPTION)));

            int fromExceptionToken;
            if (logicalResultLocal != UINT_MAX)
            {
                MethodDesc* fromExceptionMD;
                if (isValueTask)
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__VALUETASK_FROM_EXCEPTION_1);
                else
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__TASK_FROM_EXCEPTION_1);

                fromExceptionMD = FindOrCreateAssociatedMethodDesc(fromExceptionMD, fromExceptionMD->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);
                fromExceptionToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, fromExceptionMD);
            }
            else
            {
                MethodDesc* md;
                if (isValueTask)
                    md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__VALUETASK_FROM_EXCEPTION);
                else
                    md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__TASK_FROM_EXCEPTION);

                fromExceptionToken = pCode->GetToken(md);
            }

            pCode->EmitCALL(fromExceptionToken, 1, 1);
            pCode->EmitSTLOC(returnTaskLocal);
            pCode->EmitLEAVE(returnTaskLabel);
            pCode->EndCatchBlock();
        }

        pCode->EmitLabel(suspendedLabel);

        int createRuntimeAsyncTaskToken;
        if (logicalResultLocal != UINT_MAX)
        {
            MethodDesc* md;
            if (isValueTask)
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__CREATE_RUNTIME_ASYNC_VALUE_TASK_1);
            else
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__CREATE_RUNTIME_ASYNC_TASK_1);

            md = FindOrCreateAssociatedMethodDesc(md, md->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);
            createRuntimeAsyncTaskToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, md);
        }
        else
        {
            MethodDesc* md;
            if (isValueTask)
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__CREATE_RUNTIME_ASYNC_VALUE_TASK);
            else
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__CREATE_RUNTIME_ASYNC_TASK);
            createRuntimeAsyncTaskToken = pCode->GetToken(md);
        }
        pCode->EmitLDLOC(refAwaitStateLocal);
        pCode->EmitCALL(createRuntimeAsyncTaskToken, 1, 1);
        pCode->EmitSTLOC(returnTaskLocal);
        pCode->EmitLEAVE(returnTaskLabel);

        pCode->EndTryBlock();
    }
    //
    {
        pCode->BeginFinallyBlock();
        pCode->EmitLDLOC(refAwaitStateLocal);
        pCode->EmitCALL(METHOD__RUNTIME_ASYNC_AWAIT_STATE__POP, 1, 0);
        pCode->EmitENDFINALLY();
        pCode->EndFinallyBlock();
    }

    pCode->EmitLabel(returnTaskLabel);
    pCode->EmitLDLOC(returnTaskLocal);
    pCode->EmitRET();
}

// Given an async thunk method, return a SigPointer to the unwrapped result type. For
// example, for Task<T> Foo<T>() this returns the signature representing
// (MVAR 0). For Task<int>, it returns the signature representing (int).
SigPointer MethodDesc::GetAsyncThunkResultTypeSig()
{
    _ASSERTE(IsAsyncThunkMethod());
    PCCOR_SIGNATURE pSigRaw;
    DWORD cSig;
    if (FAILED(GetMDImport()->GetSigOfMethodDef(GetMemberDef(), &cSig, &pSigRaw)))
    {
        _ASSERTE(!"Loaded MethodDesc should not fail to get signature");
        pSigRaw = NULL;
        cSig = 0;
    }

    SigPointer pSig(pSigRaw, cSig);
    uint32_t callConvInfo;
    IfFailThrow(pSig.GetCallingConvInfo(&callConvInfo));

    if ((callConvInfo & IMAGE_CEE_CS_CALLCONV_GENERIC) != 0)
    {
        // GenParamCount
        IfFailThrow(pSig.GetData(NULL));
    }

    // ParamCount
    IfFailThrow(pSig.GetData(NULL));

    // ReturnType comes now. Skip the modifiers (like modreqs in async signatures).
    IfFailThrow(pSig.SkipCustomModifiers());

    CorElementType etype;
    IfFailThrow(pSig.PeekElemType(&etype));

    // here we should have something Task<retType> or ValueTask<retType>
    _ASSERTE(etype == ELEMENT_TYPE_GENERICINST);

    // GENERICINST <generic type> <argCnt> <arg1>

    // ELEMENT_TYPE_GENERICINST
    IfFailThrow(pSig.GetElemType(NULL));

    // Task`1/ValueTask`1
    IfFailThrow(pSig.SkipExactlyOne());

    // argCnt
    IfFailThrow(pSig.GetData(NULL));

    // Get the start of the return type
    PCCOR_SIGNATURE returnTypeSig;
    uint32_t tailLength;
    pSig.GetSignature(&returnTypeSig, &tailLength);

    // Skip to the end of the return type so we can get the length.
    IfFailThrow(pSig.SkipExactlyOne());

    PCCOR_SIGNATURE returnTypeSigEnd;
    pSig.GetSignature(&returnTypeSigEnd, &tailLength);

    return SigPointer(returnTypeSig, (DWORD)(returnTypeSigEnd - returnTypeSig));
}

// Given a method Foo<T>, return a MethodSpec token for Foo<T> instantiated
// with the result type from the current async method's return type. For
// example, if "this" represents Task<List<T>> Foo<T>(), and "md" is
// Task.FromResult<T>, this returns a MethodSpec representing
// Task.FromResult<List<T>>.
int MethodDesc::GetTokenForGenericMethodCallWithAsyncReturnType(ILCodeStream* pCode, MethodDesc* md)
{
    if (!md->HasClassOrMethodInstantiation())
    {
        return pCode->GetToken(md);
    }

    // We never get here with a class instantiation currently.
    _ASSERTE(!md->HasClassInstantiation());

    SigBuilder methodSigBuilder;
    methodSigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_GENERICINST);
    methodSigBuilder.AppendData(1);
    SigPointer retTypeSig = GetAsyncThunkResultTypeSig();
    PCCOR_SIGNATURE retTypeSigRaw;
    uint32_t retTypeSigLen;
    retTypeSig.GetSignature(&retTypeSigRaw, &retTypeSigLen);
    methodSigBuilder.AppendBlob((const PVOID)retTypeSigRaw, retTypeSigLen);

    DWORD methodSigLen;
    PCCOR_SIGNATURE methodSig = (PCCOR_SIGNATURE)methodSigBuilder.GetSignature(&methodSigLen);
    int methodSigToken = pCode->GetSigToken(methodSig, methodSigLen);

    return pCode->GetToken(md, mdTokenNil, methodSigToken);
}

int MethodDesc::GetTokenForThunkTarget(ILCodeStream* pCode, MethodDesc* md)
{
    _ASSERTE(!md->IsWrapperStub());

    if (!md->HasClassOrMethodInstantiation())
    {
        return pCode->GetToken(md);
    }

    // Emit trivial forwarding class/method instantiations. For tokens
    // ILStubResolver::ResolveToken returns the MethodDesc* and its
    // MethodTable* directly, without doing any instantiation. Here we match
    // the instantiation that MemberLoader::GetMethodDescFromMethodSpec would
    // apply when handling a normal MethodSpec from metadata specifying this
    // trivial forwarding instantiation.
    md = FindOrCreateAssociatedMethodDesc(
        md,
        md->GetMethodTable(),
        /* forceBoxedEntryPoint */ FALSE,
        md->GetMethodInstantiation(),
        /* allowInstParam */ FALSE);

    int typeSigToken = mdTokenNil;
    if (md->HasClassInstantiation())
    {
        SigBuilder typeSigBuilder;
        typeSigBuilder.AppendElementType(ELEMENT_TYPE_GENERICINST);
        typeSigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);

        // Like above we want the preinstantiated method table here to get the
        // instantiation that does not happen for ILStubResolver's resolution.
        typeSigBuilder.AppendPointer(md->GetMethodTable());
        DWORD numClassTypeArgs = md->GetNumGenericClassArgs();
        typeSigBuilder.AppendData(numClassTypeArgs);
        for (DWORD i = 0; i < numClassTypeArgs; ++i)
        {
            typeSigBuilder.AppendElementType(ELEMENT_TYPE_VAR);
            typeSigBuilder.AppendData(i);
        }

        DWORD typeSigLen;
        PCCOR_SIGNATURE typeSig = (PCCOR_SIGNATURE)typeSigBuilder.GetSignature(&typeSigLen);
        typeSigToken = pCode->GetSigToken(typeSig, typeSigLen);
    }

    if (!md->HasMethodInstantiation())
    {
        return pCode->GetToken(md, typeSigToken);
    }

    SigBuilder methodSigBuilder;
    DWORD numMethodTypeArgs = md->GetNumGenericMethodArgs();
    methodSigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_GENERICINST);
    methodSigBuilder.AppendData(numMethodTypeArgs);
    for (DWORD i = 0; i < numMethodTypeArgs; ++i)
    {
        methodSigBuilder.AppendElementType(ELEMENT_TYPE_MVAR);
        methodSigBuilder.AppendData(i);
    }

    DWORD sigLen;
    PCCOR_SIGNATURE sig = (PCCOR_SIGNATURE)methodSigBuilder.GetSignature(&sigLen);
    int methodSigToken = pCode->GetSigToken(sig, sigLen);
    return pCode->GetToken(md, typeSigToken, methodSigToken);
}

// Provided an async variant, emits an async wrapper that drops the returned value.
// Used in the covariant return scenario.
// The emitted code matches EmitReturnDroppingThunk in the Managed Type System.
void MethodDesc::EmitReturnDroppingThunk(MethodDesc* pAsyncOtherVariant, MetaSig& msig, ILStubLinker* pSL)
{
    _ASSERTE(pAsyncOtherVariant->IsAsyncVariantMethod());

    _ASSERTE(!pAsyncOtherVariant->IsVoid());
    _ASSERTE(pAsyncOtherVariant->IsVirtual());
    _ASSERTE(this->IsVoid());
    _ASSERTE(this->IsVirtual());

    // Implement IL that is effectively the following:
    // {
    //    this.other(arg); // CALLVIRT
    //    return;
    // }
    ILCodeStream* pCode = pSL->NewCodeStream(ILStubLinker::kDispatch);
    int token = GetTokenForThunkTarget(pCode, pAsyncOtherVariant);

    DWORD localArg = 0;
    pCode->EmitLDARG(localArg++);
    for (UINT iArg = 0; iArg < msig.NumFixedArgs(); iArg++)
    {
        pCode->EmitLDARG(localArg++);
    }

    // other(arg)
    pCode->EmitCALLVIRT(token, localArg, 1);
    // return;
    pCode->EmitPOP();
    pCode->EmitRET();
}
