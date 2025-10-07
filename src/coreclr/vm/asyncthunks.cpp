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

    MethodDesc *pAsyncOtherVariant = this->GetAsyncOtherVariant();
    _ASSERTE(!IsWrapperStub() && !pAsyncOtherVariant->IsWrapperStub());

    MetaSig msig(this);

    SigTypeContext sigContext(pAsyncOtherVariant);
    ILStubLinker sl(
        GetModule(),
        GetSignature(),
        &sigContext,
        pAsyncOtherVariant,
        (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

    if (IsAsyncMethod())
    {
        EmitAsyncMethodThunk(pAsyncOtherVariant, msig, &sl);
    }
    else
    {
        EmitTaskReturningThunk(pAsyncOtherVariant, msig, &sl);
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

void MethodDesc::EmitTaskReturningThunk(MethodDesc* pAsyncOtherVariant, MetaSig& thunkMsig, ILStubLinker* pSL)
{
    _ASSERTE(!pAsyncOtherVariant->IsAsyncThunkMethod());

    ILCodeStream* pCode = pSL->NewCodeStream(ILStubLinker::kDispatch);

    unsigned continuationLocal = pCode->NewLocal(LocalDesc(CoreLibBinder::GetClass(CLASS__CONTINUATION)));

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
    LocalDesc executionAndSyncBlockStoreLocalDesc(CoreLibBinder::GetClass(CLASS__EXECUTIONANDSYNCBLOCKSTORE));
    DWORD executionAndSyncBlockStoreLocal = pCode->NewLocal(executionAndSyncBlockStoreLocalDesc);

    ILCodeLabel* returnTaskLabel = pCode->NewCodeLabel();
    ILCodeLabel* suspendedLabel = pCode->NewCodeLabel();
    ILCodeLabel* finishedLabel = pCode->NewCodeLabel();

    pCode->EmitLDLOCA(executionAndSyncBlockStoreLocal);
    pCode->EmitCALL(pCode->GetToken(CoreLibBinder::GetMethod(METHOD__EXECUTIONANDSYNCBLOCKSTORE__PUSH)), 1, 0);

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

            int token;
            _ASSERTE(!pAsyncOtherVariant->IsWrapperStub());
            if (pAsyncOtherVariant->HasClassOrMethodInstantiation())
            {
                // For generic code emit generic signatures.
                int typeSigToken = mdTokenNil;
                if (pAsyncOtherVariant->HasClassInstantiation())
                {
                    SigBuilder typeSigBuilder;
                    typeSigBuilder.AppendElementType(ELEMENT_TYPE_GENERICINST);
                    typeSigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
                    // TODO: (async) Encoding potentially shared method tables in
                    // signatures of tokens seems odd, but this hits assert
                    // with the typical method table.
                    typeSigBuilder.AppendPointer(pAsyncOtherVariant->GetMethodTable());
                    DWORD numClassTypeArgs = pAsyncOtherVariant->GetNumGenericClassArgs();
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

                if (pAsyncOtherVariant->HasMethodInstantiation())
                {
                    SigBuilder methodSigBuilder;
                    DWORD numMethodTypeArgs = pAsyncOtherVariant->GetNumGenericMethodArgs();
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
                    token = pCode->GetToken(pAsyncOtherVariant, typeSigToken, methodSigToken);
                }
                else
                {
                    token = pCode->GetToken(pAsyncOtherVariant, typeSigToken);
                }
            }
            else
            {
                token = pCode->GetToken(pAsyncOtherVariant);
            }

            pCode->EmitCALL(token, localArg, logicalResultLocal != UINT_MAX ? 1 : 0);

            if (logicalResultLocal != UINT_MAX)
                pCode->EmitSTLOC(logicalResultLocal);
            pCode->EmitCALL(METHOD__STUBHELPERS__ASYNC_CALL_CONTINUATION, 0, 1);
            pCode->EmitSTLOC(continuationLocal);
            pCode->EmitLDLOC(continuationLocal);
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
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__VALUETASK__FROM_EXCEPTION_1);
                else
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__TASK__FROM_EXCEPTION_1);

                fromExceptionMD = FindOrCreateAssociatedMethodDesc(fromExceptionMD, fromExceptionMD->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);

                fromExceptionToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, fromExceptionMD);
            }
            else
            {
                MethodDesc* fromExceptionMD;
                if (isValueTask)
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__VALUETASK__FROM_EXCEPTION);
                else
                    fromExceptionMD = CoreLibBinder::GetMethod(METHOD__TASK__FROM_EXCEPTION);

                fromExceptionToken = pCode->GetToken(fromExceptionMD);
            }
            pCode->EmitCALL(fromExceptionToken, 1, 1);
            pCode->EmitSTLOC(returnTaskLocal);
            pCode->EmitLEAVE(returnTaskLabel);
            pCode->EndCatchBlock();
        }

        pCode->EmitLabel(suspendedLabel);

        int finalizeTaskReturningThunkToken;
        if (logicalResultLocal != UINT_MAX)
        {
            MethodDesc* md;
            if (isValueTask)
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__FINALIZE_VALUETASK_RETURNING_THUNK_1);
            else
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__FINALIZE_TASK_RETURNING_THUNK_1);

            md = FindOrCreateAssociatedMethodDesc(md, md->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);
            finalizeTaskReturningThunkToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, md);
        }
        else
        {
            MethodDesc* md;
            if (isValueTask)
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__FINALIZE_VALUETASK_RETURNING_THUNK);
            else
                md = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__FINALIZE_TASK_RETURNING_THUNK);
            finalizeTaskReturningThunkToken = pCode->GetToken(md);
        }
        pCode->EmitLDLOC(continuationLocal);
        pCode->EmitCALL(finalizeTaskReturningThunkToken, 1, 1);
        pCode->EmitSTLOC(returnTaskLocal);
        pCode->EmitLEAVE(returnTaskLabel);

        pCode->EndTryBlock();
    }
    //
    {
        pCode->BeginFinallyBlock();
        pCode->EmitLDLOCA(executionAndSyncBlockStoreLocal);
        pCode->EmitCALL(pCode->GetToken(CoreLibBinder::GetMethod(METHOD__EXECUTIONANDSYNCBLOCKSTORE__POP)), 1, 0);
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

bool MethodDesc::IsValueTaskAsyncThunk()
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

    // ReturnType comes now. Skip the modifiers.
    IfFailThrow(pSig.SkipCustomModifiers());

    // here we should have something Task, ValueTask, Task<retType> or ValueTask<retType>
    BYTE bElementType;
    IfFailThrow(pSig.GetByte(&bElementType));

    // skip ELEMENT_TYPE_GENERICINST
    if (bElementType == ELEMENT_TYPE_GENERICINST)
        IfFailThrow(pSig.GetByte(&bElementType));

    _ASSERTE(bElementType == ELEMENT_TYPE_VALUETYPE || bElementType == ELEMENT_TYPE_CLASS);
    return bElementType == ELEMENT_TYPE_VALUETYPE;
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

// Given a method Bar<T>.Foo, return a MethodSpec token for Bar<T>.Foo
// instantiated with the result type from the current async method's return
// type. For example, if "this" represents Task<List<T>> Foo<T>(), and
// "md" is TaskAwaiter<T>.GetResult(), this returns a MethodSpec representing
// TaskAwaiter<List<T>>.GetResult().
int MethodDesc::GetTokenForGenericTypeMethodCallWithAsyncReturnType(ILCodeStream* pCode, MethodDesc* md)
{
    if (!md->HasClassOrMethodInstantiation())
    {
        return pCode->GetToken(md);
    }

    // We never get here with a method instantiation currently.
    _ASSERTE(!md->HasMethodInstantiation());

    SigBuilder typeSigBuilder;
    typeSigBuilder.AppendData(ELEMENT_TYPE_GENERICINST);
    typeSigBuilder.AppendData(ELEMENT_TYPE_INTERNAL);
    // TODO: (async) Encoding potentially shared method tables in
    // signatures of tokens seems odd, but this hits assert
    // with the typical method table.
    typeSigBuilder.AppendPointer(md->GetMethodTable());
    typeSigBuilder.AppendData(1);

    SigPointer retTypeSig = GetAsyncThunkResultTypeSig();
    PCCOR_SIGNATURE retTypeSigRaw;
    uint32_t retTypeSigLen;
    retTypeSig.GetSignature(&retTypeSigRaw, &retTypeSigLen);

    typeSigBuilder.AppendBlob((const PVOID)retTypeSigRaw, retTypeSigLen);

    DWORD typeSigLen;
    PCCOR_SIGNATURE typeSig = (PCCOR_SIGNATURE)typeSigBuilder.GetSignature(&typeSigLen);
    int typeSigToken = pCode->GetSigToken(typeSig, typeSigLen);

    return pCode->GetToken(md, typeSigToken);
}

void MethodDesc::EmitAsyncMethodThunk(MethodDesc* pAsyncOtherVariant, MetaSig& msig, ILStubLinker* pSL)
{
    _ASSERTE(!pAsyncOtherVariant->IsAsyncThunkMethod());
    _ASSERTE(!pAsyncOtherVariant->IsVoid());

    // Implement IL that is effectively the following
    /*
    {
        Task task = other(arg);
        if (!task.IsCompleted)
        {
            // Magic function which will suspend the current run of async methods
            AsyncHelpers.TransparentAwaitTask(task);
        }
        return AsyncHelpers.CompletedTaskResult(task);
    }

    For ValueTask:
    {
        ValueTask vt = other(arg);
        if (vt.IsCompleted)
            return vt.Result/vt.ThrowIfCompletedUnsuccessfully();

        Task task = vt.AsTask();
        <same code as above>
    }
    */
    ILCodeStream* pCode = pSL->NewCodeStream(ILStubLinker::kDispatch);

    int userFuncToken;
    _ASSERTE(!pAsyncOtherVariant->IsWrapperStub());
    if (pAsyncOtherVariant->HasClassOrMethodInstantiation())
    {
        // For generic code emit generic signatures.
        int typeSigToken = mdTokenNil;
        if (pAsyncOtherVariant->HasClassInstantiation())
        {
            SigBuilder typeSigBuilder;
            typeSigBuilder.AppendElementType(ELEMENT_TYPE_GENERICINST);
            typeSigBuilder.AppendElementType(ELEMENT_TYPE_INTERNAL);
            // TODO: (async) Encoding potentially shared method tables in
            // signatures of tokens seems odd, but this hits assert
            // with the typical method table.
            typeSigBuilder.AppendPointer(pAsyncOtherVariant->GetMethodTable());
            DWORD numClassTypeArgs = pAsyncOtherVariant->GetNumGenericClassArgs();
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

        if (pAsyncOtherVariant->HasMethodInstantiation())
        {
            SigBuilder methodSigBuilder;
            DWORD numMethodTypeArgs = pAsyncOtherVariant->GetNumGenericMethodArgs();
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
            userFuncToken = pCode->GetToken(pAsyncOtherVariant, typeSigToken, methodSigToken);
        }
        else
        {
            userFuncToken = pCode->GetToken(pAsyncOtherVariant, typeSigToken);
        }
    }
    else
    {
        userFuncToken = pCode->GetToken(pAsyncOtherVariant);
    }

    DWORD localArg = 0;
    if (msig.HasThis())
    {
        pCode->EmitLDARG(localArg++);
    }
    for (UINT iArg = 0; iArg < msig.NumFixedArgs(); iArg++)
    {
        pCode->EmitLDARG(localArg++);
    }

    pCode->EmitCALL(userFuncToken, localArg, 1);

    TypeHandle thLogicalRetType = msig.GetRetTypeHandleThrowing();
    if (IsValueTaskAsyncThunk())
    {
        // Emit
        // if (vtask.IsCompleted)
        //   return vtask.Result/vtask.ThrowIfCompletedUnsuccessfully()
        // task = vtask.AsTask()
        //

        MethodTable* pMTValueTask;
        int isCompletedToken;
        int completionResultToken;
        int asTaskToken;
        if (msig.IsReturnTypeVoid())
        {
            pMTValueTask = CoreLibBinder::GetClass(CLASS__VALUETASK);

            MethodDesc* pMDValueTaskIsCompleted = CoreLibBinder::GetMethod(METHOD__VALUETASK__GET_ISCOMPLETED);
            MethodDesc* pMDCompletionResult = CoreLibBinder::GetMethod(METHOD__VALUETASK__THROW_IF_COMPLETED_UNSUCCESSFULLY);
            MethodDesc* pMDAsTask = CoreLibBinder::GetMethod(METHOD__VALUETASK__AS_TASK);

            isCompletedToken = pCode->GetToken(pMDValueTaskIsCompleted);
            completionResultToken = pCode->GetToken(pMDCompletionResult);
            asTaskToken = pCode->GetToken(pMDAsTask);
        }
        else
        {
            MethodTable* pMTValueTaskOpen = CoreLibBinder::GetClass(CLASS__VALUETASK_1);
            pMTValueTask = ClassLoader::LoadGenericInstantiationThrowing(pMTValueTaskOpen->GetModule(), pMTValueTaskOpen->GetCl(), Instantiation(&thLogicalRetType, 1)).GetMethodTable();

            MethodDesc* pMDValueTaskIsCompleted = CoreLibBinder::GetMethod(METHOD__VALUETASK_1__GET_ISCOMPLETED);
            MethodDesc* pMDCompletionResult = CoreLibBinder::GetMethod(METHOD__VALUETASK_1__GET_RESULT);
            MethodDesc* pMDAsTask = CoreLibBinder::GetMethod(METHOD__VALUETASK_1__AS_TASK);

            pMDValueTaskIsCompleted = FindOrCreateAssociatedMethodDesc(pMDValueTaskIsCompleted, pMTValueTask, FALSE, Instantiation(), FALSE);
            pMDCompletionResult = FindOrCreateAssociatedMethodDesc(pMDCompletionResult, pMTValueTask, FALSE, Instantiation(), FALSE);
            pMDAsTask = FindOrCreateAssociatedMethodDesc(pMDAsTask, pMTValueTask, FALSE, Instantiation(), FALSE);

            isCompletedToken = GetTokenForGenericTypeMethodCallWithAsyncReturnType(pCode, pMDValueTaskIsCompleted);
            completionResultToken = GetTokenForGenericTypeMethodCallWithAsyncReturnType(pCode, pMDCompletionResult);
            asTaskToken = GetTokenForGenericTypeMethodCallWithAsyncReturnType(pCode, pMDAsTask);
        }

        LocalDesc valueTaskLocalDesc(pMTValueTask);
        DWORD valueTaskLocal = pCode->NewLocal(valueTaskLocalDesc);
        ILCodeLabel* valueTaskNotCompletedLabel = pCode->NewCodeLabel();

        // Store value task returned by call to actual user func
        pCode->EmitSTLOC(valueTaskLocal);

        pCode->EmitLDLOCA(valueTaskLocal);
        pCode->EmitCALL(isCompletedToken, 1, 1);
        pCode->EmitBRFALSE(valueTaskNotCompletedLabel);

        pCode->EmitLDLOCA(valueTaskLocal);
        pCode->EmitCALL(completionResultToken, 1, msig.IsReturnTypeVoid() ? 0 : 1);
        pCode->EmitRET();

        pCode->EmitLabel(valueTaskNotCompletedLabel);
        pCode->EmitLDLOCA(valueTaskLocal);
        pCode->EmitCALL(asTaskToken, 1, 1);
    }

    MethodTable* pMTTask;

    int completedTaskResultToken;
    if (msig.IsReturnTypeVoid())
    {
        pMTTask = CoreLibBinder::GetClass(CLASS__TASK);

        MethodDesc* pMDCompletedTask = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__COMPLETED_TASK);
        completedTaskResultToken = pCode->GetToken(pMDCompletedTask);
    }
    else
    {
        MethodTable* pMTTaskOpen = CoreLibBinder::GetClass(CLASS__TASK_1);
        pMTTask = ClassLoader::LoadGenericInstantiationThrowing(pMTTaskOpen->GetModule(), pMTTaskOpen->GetCl(), Instantiation(&thLogicalRetType, 1)).GetMethodTable();

        MethodDesc* pMDCompletedTaskResult = CoreLibBinder::GetMethod(METHOD__ASYNC_HELPERS__COMPLETED_TASK_RESULT);
        pMDCompletedTaskResult = FindOrCreateAssociatedMethodDesc(pMDCompletedTaskResult, pMDCompletedTaskResult->GetMethodTable(), FALSE, Instantiation(&thLogicalRetType, 1), FALSE);
        completedTaskResultToken = GetTokenForGenericMethodCallWithAsyncReturnType(pCode, pMDCompletedTaskResult);
    }

    LocalDesc taskLocalDesc(pMTTask);
    DWORD taskLocal = pCode->NewLocal(taskLocalDesc);
    ILCodeLabel* pGetResultLabel = pCode->NewCodeLabel();

    // Store task returned by actual user func or by ValueTask.AsTask
    pCode->EmitSTLOC(taskLocal);

    pCode->EmitLDLOC(taskLocal);
    pCode->EmitCALL(METHOD__TASK__GET_ISCOMPLETED, 1, 1);
    pCode->EmitBRTRUE(pGetResultLabel);

    pCode->EmitLDLOC(taskLocal);
    pCode->EmitCALL(METHOD__ASYNC_HELPERS__TRANSPARENT_AWAIT_TASK, 1, 0);

    pCode->EmitLabel(pGetResultLabel);
    pCode->EmitLDLOC(taskLocal);
    pCode->EmitCALL(completedTaskResultToken, 1, msig.IsReturnTypeVoid() ? 0 : 1);
    pCode->EmitRET();
}
