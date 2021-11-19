// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "corpriv.h"
#include "tailcallhelp.h"
#include "dllimport.h"
#include "formattype.h"
#include "sigformat.h"
#include "gcrefmap.h"
#include "threads.h"


FCIMPL2(void*, TailCallHelp::AllocTailCallArgBuffer, INT32 size, void* gcDesc)
{
    CONTRACTL
    {
        FCALL_CHECK;
        INJECT_FAULT(FCThrow(kOutOfMemoryException););
    }
    CONTRACTL_END

    _ASSERTE(size >= 0);

    void* result = GetThread()->GetTailCallTls()->AllocArgBuffer(size, gcDesc);

    if (result == NULL)
        FCThrow(kOutOfMemoryException);

    return result;
}
FCIMPLEND

FCIMPL2(void*, TailCallHelp::GetTailCallInfo, void** retAddrSlot, void** retAddr)
{
    FCALL_CONTRACT;

    Thread* thread = GetThread();

    *retAddr = thread->GetReturnAddress(retAddrSlot);
    return thread->GetTailCallTls();
}
FCIMPLEND


struct ArgBufferValue
{
    TypeHandle TyHnd;
    unsigned int Offset;

    ArgBufferValue(TypeHandle tyHnd = TypeHandle(), unsigned int offset = 0)
        : TyHnd(tyHnd), Offset(offset)
    {
    }
};

struct ArgBufferLayout
{
    bool HasTargetAddress;
    bool HasInstArg;
    unsigned int TargetAddressOffset;
    InlineSArray<ArgBufferValue, 8> Values;
    unsigned int Size;

    ArgBufferLayout()
        : HasTargetAddress(false)
        , HasInstArg(false)
        , TargetAddressOffset(0)
        , Size(0)
    {
    }
};

struct TailCallInfo
{
    MethodDesc* Caller;
    MethodDesc* Callee;
    PTR_LoaderAllocator LoaderAllocator;
    MetaSig* CallSiteSig;
    bool CallSiteIsVirtual;
    TypeHandle RetTyHnd;
    ArgBufferLayout ArgBufLayout;
    bool HasGCDescriptor;
    GCRefMapBuilder GCRefMapBuilder;

    TailCallInfo(
        MethodDesc* pCallerMD, MethodDesc* pCalleeMD,
        PTR_LoaderAllocator pLoaderAllocator,
        MetaSig* callSiteSig, bool callSiteIsVirtual,
        TypeHandle retTyHnd)
        : Caller(pCallerMD)
        , Callee(pCalleeMD)
        , LoaderAllocator(pLoaderAllocator)
        , CallSiteSig(callSiteSig)
        , CallSiteIsVirtual(callSiteIsVirtual)
        , RetTyHnd(retTyHnd)
        , HasGCDescriptor(false)
    {
    }
};

static MethodDesc* s_tailCallDispatcherMD;
MethodDesc* TailCallHelp::GetOrLoadTailCallDispatcherMD()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(ThrowOutOfMemory());
    }
    CONTRACTL_END;

    if (s_tailCallDispatcherMD == NULL)
        s_tailCallDispatcherMD = CoreLibBinder::GetMethod(METHOD__RUNTIME_HELPERS__DISPATCH_TAILCALLS);

    return s_tailCallDispatcherMD;
}

MethodDesc* TailCallHelp::GetTailCallDispatcherMD()
{
    LIMITED_METHOD_CONTRACT;
    return s_tailCallDispatcherMD;
}


void TailCallHelp::CreateTailCallHelperStubs(
    MethodDesc* pCallerMD, MethodDesc* pCalleeMD,
    MetaSig& callSiteSig, bool virt, bool thisArgByRef, bool hasInstArg,
    MethodDesc** storeArgsStub, bool* storeArgsNeedsTarget,
    MethodDesc** callTargetStub)
{
    STANDARD_VM_CONTRACT;

#ifdef _DEBUG
    SigFormat incSig(callSiteSig, NULL);
    LOG((LF_STUBS, LL_INFO1000, "TAILCALLHELP: Incoming sig %s\n", incSig.GetCString()));
#endif

    *storeArgsNeedsTarget = pCalleeMD == NULL || pCalleeMD->IsSharedByGenericInstantiations();

    // The tailcall helper stubs are always allocated together with the caller.
    // If we ever wish to share these stubs they should be allocated with the
    // callee in most cases.
    LoaderAllocator* pLoaderAllocator = pCallerMD->GetLoaderAllocator();

    TypeHandle retTyHnd = NormalizeSigType(callSiteSig.GetRetTypeHandleThrowing());
    TailCallInfo info(pCallerMD, pCalleeMD, pLoaderAllocator, &callSiteSig, virt, retTyHnd);

    LayOutArgBuffer(callSiteSig, pCalleeMD, *storeArgsNeedsTarget, thisArgByRef, hasInstArg, &info.ArgBufLayout);
    info.HasGCDescriptor = GenerateGCDescriptor(pCalleeMD, info.ArgBufLayout, &info.GCRefMapBuilder);

    *storeArgsStub = CreateStoreArgsStub(info);
    *callTargetStub = CreateCallTargetStub(info);
}

void TailCallHelp::LayOutArgBuffer(
    MetaSig& callSiteSig, MethodDesc* calleeMD,
    bool storeTarget, bool thisArgByRef, bool hasInstArg, ArgBufferLayout* layout)
{
    unsigned int offs = offsetof(TailCallArgBuffer, Args);

    auto addValue = [&](TypeHandle th)
    {
        unsigned int alignment = CEEInfo::getClassAlignmentRequirementStatic(th);
        offs = AlignUp(offs, alignment);
        layout->Values.Append(ArgBufferValue(th, offs));
        offs += th.GetSize();
    };

    // User args
    if (callSiteSig.HasThis() && !callSiteSig.HasExplicitThis())
    {
        TypeHandle thisHnd;

        bool thisParamByRef = (calleeMD != NULL) ? calleeMD->GetMethodTable()->IsValueType() : thisArgByRef;
        if (thisParamByRef)
        {
            thisHnd = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_U1))
                      .MakeByRef();
        }
        else
        {
            thisHnd = TypeHandle(g_pObjectClass);
        }

        addValue(thisHnd);
    }

    layout->HasInstArg = hasInstArg;

#ifndef TARGET_X86
    if (hasInstArg)
    {
        addValue(TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_I)));
    }
#endif

    callSiteSig.Reset();
    CorElementType ty;
    while ((ty = callSiteSig.NextArg()) != ELEMENT_TYPE_END)
    {
        TypeHandle tyHnd = callSiteSig.GetLastTypeHandleThrowing();
        tyHnd = NormalizeSigType(tyHnd);
        addValue(tyHnd);
    }

#ifdef TARGET_X86
    if (hasInstArg)
    {
        addValue(TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_I)));
    }
#endif

    if (storeTarget)
    {
        offs = AlignUp(offs, TARGET_POINTER_SIZE);
        layout->TargetAddressOffset = offs;
        layout->HasTargetAddress = true;
        offs += TARGET_POINTER_SIZE;
    }

    layout->Size = offs;
}

// The types we get from a signature can be generic type arguments, but the
// stubs we create are not generic, so this function normalizes types in the
// signature to a compatible more general type.
TypeHandle TailCallHelp::NormalizeSigType(TypeHandle tyHnd)
{
    CorElementType ety = tyHnd.GetSignatureCorElementType();
    if (CorTypeInfo::IsPrimitiveType(ety))
    {
        return tyHnd;
    }
    if (CorTypeInfo::IsObjRef(ety))
    {
        return TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_OBJECT));
    }
    if (tyHnd.IsPointer() || tyHnd.IsFnPtrType())
    {
        return TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_I));
    }

    if (tyHnd.IsByRef())
    {
        return TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_U1))
               .MakeByRef();
    }

    _ASSERTE(ety == ELEMENT_TYPE_VALUETYPE && tyHnd.IsValueType());
    // Value type -- retain it to preserve its size
    return tyHnd;
}

bool TailCallHelp::GenerateGCDescriptor(
    MethodDesc* pTargetMD, const ArgBufferLayout& layout, GCRefMapBuilder* builder)
{
    auto writeGCType = [&](unsigned int argPos, CorInfoGCType type)
    {
        switch (type)
        {
            case TYPE_GC_REF: builder->WriteToken(argPos, GCREFMAP_REF); break;
            case TYPE_GC_BYREF: builder->WriteToken(argPos, GCREFMAP_INTERIOR); break;
            case TYPE_GC_NONE: break;
            default: UNREACHABLE_MSG("Invalid type"); break;
        }
    };

    bool reportInstArg = layout.HasInstArg;

    CQuickBytes gcPtrs;
    for (COUNT_T i = 0; i < layout.Values.GetCount(); i++)
    {
        const ArgBufferValue& val = layout.Values[i];

        unsigned int argPos = (val.Offset - offsetof(TailCallArgBuffer, Args)) / TARGET_POINTER_SIZE;

        TypeHandle tyHnd = val.TyHnd;
        if (tyHnd.IsValueType())
        {
            if (!tyHnd.GetMethodTable()->ContainsPointers())
            {
#ifndef TARGET_X86
                // The generic instantiation arg is right after this pointer
                if (reportInstArg)
                {
                    _ASSERTE(i == 0 || i == 1);
                    builder->WriteToken(argPos, pTargetMD->RequiresInstMethodDescArg() ? GCREFMAP_METHOD_PARAM : GCREFMAP_TYPE_PARAM);
                    reportInstArg = false;
                }
#endif
                continue;
            }

            unsigned int numSlots = (tyHnd.GetSize() + TARGET_POINTER_SIZE - 1) / TARGET_POINTER_SIZE;
            BYTE* ptr = static_cast<BYTE*>(gcPtrs.AllocThrows(numSlots));
            CEEInfo::getClassGClayoutStatic(tyHnd, ptr);
            for (unsigned int i = 0; i < numSlots; i++)
            {
                writeGCType(argPos + i , (CorInfoGCType)ptr[i]);
            }

            continue;
        }

        CorElementType ety = tyHnd.GetSignatureCorElementType();
        CorInfoGCType gc = CorTypeInfo::GetGCType(ety);

        writeGCType(argPos, gc);
    }

#ifdef TARGET_X86
    // The generic instantiation arg is last
    if (reportInstArg)
    {
        const ArgBufferValue& val = layout.Values[layout.Values.GetCount() - 1];
        unsigned int argPos = (val.Offset - offsetof(TailCallArgBuffer, Args)) / TARGET_POINTER_SIZE;
        builder->WriteToken(argPos, pTargetMD->RequiresInstMethodDescArg() ? GCREFMAP_METHOD_PARAM : GCREFMAP_TYPE_PARAM);
    }
#endif

    builder->Flush();

    return builder->GetBlobLength() > 0;
}

MethodDesc* TailCallHelp::CreateStoreArgsStub(TailCallInfo& info)
{
    SigBuilder sigBuilder;
    CreateStoreArgsStubSig(info, &sigBuilder);

    DWORD cbSig;
    PCCOR_SIGNATURE pSig = AllocateSignature(
        info.LoaderAllocator, sigBuilder, &cbSig);

    SigTypeContext emptyCtx;

    ILStubLinker sl(info.Caller->GetModule(),
                    Signature(pSig, cbSig),
                    &emptyCtx,
                    NULL,
                    ILSTUB_LINKER_FLAG_NONE);

    ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    DWORD bufferLcl = pCode->NewLocal(ELEMENT_TYPE_I);

    void* pGcDesc = NULL;
    if (info.HasGCDescriptor)
    {
        DWORD gcDescLen;
        PVOID gcDesc = info.GCRefMapBuilder.GetBlob(&gcDescLen);
        pGcDesc = AllocateBlob(info.LoaderAllocator, gcDesc, gcDescLen);
    }

    pCode->EmitLDC(info.ArgBufLayout.Size);
    pCode->EmitLDC(DWORD_PTR(pGcDesc));
    pCode->EmitCONV_I();
    pCode->EmitCALL(METHOD__RUNTIME_HELPERS__ALLOC_TAILCALL_ARG_BUFFER, 2, 1);
    pCode->EmitSTLOC(bufferLcl);

    auto emitOffs = [&](UINT offs)
    {
        pCode->EmitLDLOC(bufferLcl);
        pCode->EmitLDC(offs);
        pCode->EmitADD();
    };

    for (COUNT_T i = 0; i < info.ArgBufLayout.Values.GetCount(); i++)
    {
        const ArgBufferValue& arg = info.ArgBufLayout.Values[i];

        emitOffs(arg.Offset);
        pCode->EmitLDARG(i);
        EmitStoreTyHnd(pCode, arg.TyHnd);
    }

    if (info.ArgBufLayout.HasTargetAddress)
    {
        emitOffs(info.ArgBufLayout.TargetAddressOffset);
        pCode->EmitLDARG(info.ArgBufLayout.Values.GetCount());
        pCode->EmitSTIND_I();
    }

    pCode->EmitRET();

    Module* pLoaderModule = info.Caller->GetLoaderModule();
    MethodDesc* pStoreArgsMD =
        ILStubCache::CreateAndLinkNewILStubMethodDesc(
            info.LoaderAllocator,
            pLoaderModule->GetILStubCache()->GetOrCreateStubMethodTable(pLoaderModule),
            ILSTUB_TAILCALL_STOREARGS,
            info.Caller->GetModule(),
            pSig, cbSig,
            &emptyCtx,
            &sl);

#ifdef _DEBUG
    LOG((LF_STUBS, LL_INFO1000, "TAILCALLHELP: StoreArgs IL created\n"));
    sl.LogILStub(CORJIT_FLAGS());
#endif

    return pStoreArgsMD;
}

void TailCallHelp::CreateStoreArgsStubSig(
    const TailCallInfo& info, SigBuilder* sig)
{
    // The store-args stub will be different depending on the tailcall site.
    // Specifically the following things might be conditionally inserted:
    // * Call target address (for calli or generic calls resolved at tailcall site)
    // * This pointer (for instance calls)

    sig->AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT);

    ULONG paramCount = 0;
    paramCount += info.ArgBufLayout.Values.GetCount();
    if (info.ArgBufLayout.HasTargetAddress)
    {
        paramCount++;
    }

    sig->AppendData(paramCount);

    sig->AppendElementType(ELEMENT_TYPE_VOID);

    for (COUNT_T i = 0; i < info.ArgBufLayout.Values.GetCount(); i++)
    {
        const ArgBufferValue& val = info.ArgBufLayout.Values[i];
        AppendTypeHandle(*sig, val.TyHnd);
    }

    if (info.ArgBufLayout.HasTargetAddress)
    {
        sig->AppendElementType(ELEMENT_TYPE_I);
    }

#ifdef _DEBUG
    DWORD cbSig;
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sig->GetSignature(&cbSig);
    SigTypeContext emptyContext;
    MetaSig outMsig(pSig, cbSig, info.CallSiteSig->GetModule(), &emptyContext);
    SigFormat outSig(outMsig, NULL);
    LOG((LF_STUBS, LL_INFO1000, "TAILCALLHELP: StoreArgs sig: %s\n", outSig.GetCString()));
#endif // _DEBUG
}

MethodDesc* TailCallHelp::CreateCallTargetStub(const TailCallInfo& info)
{
    SigBuilder sigBuilder;
    CreateCallTargetStubSig(info, &sigBuilder);

    DWORD cbSig;
    PCCOR_SIGNATURE pSig = AllocateSignature(info.LoaderAllocator, sigBuilder, &cbSig);

    SigTypeContext emptyCtx;

    ILStubLinker sl(info.Caller->GetModule(),
                    Signature(pSig, cbSig),
                    &emptyCtx,
                    NULL,
                    ILSTUB_LINKER_FLAG_NONE);

    ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // void CallTarget(void* argBuffer, void* retVal, PortableTailCallFrame* pFrame)
    const int ARG_ARG_BUFFER = 0;
    const int ARG_RET_VAL = 1;
    const int ARG_PTR_FRAME = 2;

    auto emitOffs = [&](UINT offs)
    {
        pCode->EmitLDARG(ARG_ARG_BUFFER);
        pCode->EmitLDC(offs);
        pCode->EmitADD();
    };

    // pFrame->NextCall = 0;
    pCode->EmitLDARG(ARG_PTR_FRAME);
    pCode->EmitLDC(0);
    pCode->EmitCONV_U();
    pCode->EmitSTFLD(FIELD__PORTABLE_TAIL_CALL_FRAME__NEXT_CALL);

    // pFrame->TailCallAwareReturnAddress = NextCallReturnAddress();
    pCode->EmitLDARG(ARG_PTR_FRAME);
    pCode->EmitCALL(METHOD__STUBHELPERS__NEXT_CALL_RETURN_ADDRESS, 0, 1);
    pCode->EmitSTFLD(FIELD__PORTABLE_TAIL_CALL_FRAME__TAILCALL_AWARE_RETURN_ADDRESS);

    for (COUNT_T i = 0; i < info.ArgBufLayout.Values.GetCount(); i++)
    {
        const ArgBufferValue& arg = info.ArgBufLayout.Values[i];

        // arg = args->Arg_i
        emitOffs(arg.Offset);
        EmitLoadTyHnd(pCode, arg.TyHnd);
    }

    // All arguments are loaded on the stack, it is safe to disable the GC reporting of ArgBuffer now.
    // This is optimization to avoid extending argument lifetime unnecessarily.
    // We still need to report the inst argument of shared generic code to prevent it from being unloaded. The inst
    // argument is just a regular IntPtr on the stack. It is safe to stop reporting it only after the target method
    // takes over.
    pCode->EmitLDARG(ARG_ARG_BUFFER);
    pCode->EmitLDC(info.ArgBufLayout.HasInstArg ? TAILCALLARGBUFFER_INSTARG_ONLY : TAILCALLARGBUFFER_ABANDONED);
    pCode->EmitSTIND_I();

    int numRetVals = info.CallSiteSig->IsReturnTypeVoid() ? 0 : 1;
    // Normally there will not be any target and we just emit a normal
    // call/callvirt.
    if (!info.ArgBufLayout.HasTargetAddress)
    {
        _ASSERTE(info.Callee != NULL);
        // TODO: enable for varargs. We need to fix the TokenLookupMap to build
        // the proper MethodRef.
        _ASSERTE(!info.CallSiteSig->IsVarArg());

        if (info.CallSiteIsVirtual)
        {
            pCode->EmitCALLVIRT(
                pCode->GetToken(info.Callee),
                static_cast<int>(info.ArgBufLayout.Values.GetCount()),
                numRetVals);
        }
        else
        {
            pCode->EmitCALL(
                pCode->GetToken(info.Callee),
                static_cast<int>(info.ArgBufLayout.Values.GetCount()),
                numRetVals);
        }
    }
    else
    {
        // Build the signature for the calli.
        SigBuilder calliSig;

        if (info.CallSiteSig->HasThis())
        {
            _ASSERTE(info.ArgBufLayout.Values.GetCount() > 0);

            calliSig.AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS);
            calliSig.AppendData(info.ArgBufLayout.Values.GetCount() - 1);
        }
        else
        {
            calliSig.AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT);
            calliSig.AppendData(info.ArgBufLayout.Values.GetCount());
        }

        // Return type
        AppendTypeHandle(calliSig, info.RetTyHnd);

        COUNT_T firstSigArg = info.CallSiteSig->HasThis() ? 1 : 0;

        for (COUNT_T i = firstSigArg; i < info.ArgBufLayout.Values.GetCount(); i++)
        {
            const ArgBufferValue& val = info.ArgBufLayout.Values[i];
            AppendTypeHandle(calliSig, val.TyHnd);
        }

        DWORD cbCalliSig;
        PCCOR_SIGNATURE pCalliSig = (PCCOR_SIGNATURE)calliSig.GetSignature(&cbCalliSig);

        emitOffs(info.ArgBufLayout.TargetAddressOffset);
        pCode->EmitLDIND_I();

        pCode->EmitCALLI(
            pCode->GetSigToken(pCalliSig, cbCalliSig),
            static_cast<int>(info.ArgBufLayout.Values.GetCount()),
            numRetVals);
    }

    if (!info.CallSiteSig->IsReturnTypeVoid())
    {
        DWORD resultLcl = pCode->NewLocal(LocalDesc(info.RetTyHnd));
        pCode->EmitSTLOC(resultLcl);

        pCode->EmitLDARG(ARG_RET_VAL);
        pCode->EmitLDLOC(resultLcl);
        EmitStoreTyHnd(pCode, info.RetTyHnd);
    }

    pCode->EmitRET();

    Module* pLoaderModule = info.Caller->GetLoaderModule();
    MethodDesc* pCallTargetMD =
        ILStubCache::CreateAndLinkNewILStubMethodDesc(
            info.LoaderAllocator,
            pLoaderModule->GetILStubCache()->GetOrCreateStubMethodTable(pLoaderModule),
            ILSTUB_TAILCALL_CALLTARGET,
            info.Caller->GetModule(),
            pSig, cbSig,
            &emptyCtx,
            &sl);

#ifdef _DEBUG
    LOG((LF_STUBS, LL_INFO1000, "TAILCALLHELP: CallTarget IL created\n"));
    sl.LogILStub(CORJIT_FLAGS());
#endif

    return pCallTargetMD;
}

void TailCallHelp::CreateCallTargetStubSig(const TailCallInfo& info, SigBuilder* sig)
{
    sig->AppendByte(IMAGE_CEE_CS_CALLCONV_DEFAULT);

    // Arg buffer, return value pointer, and pointer to "tail call aware return address" field.
    sig->AppendData(3);

    // Returns void
    sig->AppendElementType(ELEMENT_TYPE_VOID);

    // Arg buffer
    sig->AppendElementType(ELEMENT_TYPE_I);

    // Return value
    sig->AppendElementType(ELEMENT_TYPE_I);

    // Pointer to tail call frame
    sig->AppendElementType(ELEMENT_TYPE_I);

#ifdef _DEBUG
    DWORD cbSig;
    PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sig->GetSignature(&cbSig);
    SigTypeContext emptyContext;
    MetaSig outMsig(pSig, cbSig, info.CallSiteSig->GetModule(), &emptyContext);
    SigFormat outSig(outMsig, NULL);
    LOG((LF_STUBS, LL_INFO1000, "TAILCALLHELP: CallTarget sig: %s\n", outSig.GetCString()));
#endif // _DEBUG
}

// Get TypeHandle for ByReference<System.Byte>
static TypeHandle GetByReferenceOfByteType()
{
    TypeHandle byteTH(CoreLibBinder::GetElementType(ELEMENT_TYPE_U1));
    Instantiation byteInst(&byteTH, 1);
    TypeHandle th = TypeHandle(CoreLibBinder::GetClass(CLASS__BYREFERENCE)).Instantiate(byteInst);
    return th;
}

// Get MethodDesc* for ByReference<System.Byte>::get_Value
static MethodDesc* GetByReferenceOfByteValueGetter()
{
    MethodDesc* getter = CoreLibBinder::GetMethod(METHOD__BYREFERENCE__GET_VALUE);
    getter =
        MethodDesc::FindOrCreateAssociatedMethodDesc(
                getter,
                GetByReferenceOfByteType().GetMethodTable(),
                false,
                Instantiation(),
                TRUE);

    return getter;
}

// Get MethodDesc* for ByReference<System.Byte>::.ctor
static MethodDesc* GetByReferenceOfByteCtor()
{
    MethodDesc* ctor = CoreLibBinder::GetMethod(METHOD__BYREFERENCE__CTOR);
    ctor =
        MethodDesc::FindOrCreateAssociatedMethodDesc(
                ctor,
                GetByReferenceOfByteType().GetMethodTable(),
                false,
                Instantiation(),
                TRUE);

    return ctor;
}

void TailCallHelp::EmitLoadTyHnd(ILCodeStream* stream, TypeHandle tyHnd)
{
    if (tyHnd.IsByRef())
        stream->EmitCALL(stream->GetToken(GetByReferenceOfByteValueGetter()), 1, 1);
    else
        stream->EmitLDOBJ(stream->GetToken(tyHnd));
}

void TailCallHelp::EmitStoreTyHnd(ILCodeStream* stream, TypeHandle tyHnd)
{
    if (tyHnd.IsByRef())
    {
        stream->EmitNEWOBJ(stream->GetToken(GetByReferenceOfByteCtor()), 1);
        stream->EmitSTOBJ(stream->GetToken(GetByReferenceOfByteType()));
    }
    else
    {
        stream->EmitSTOBJ(stream->GetToken(tyHnd));
    }
}

void TailCallHelp::AppendTypeHandle(SigBuilder& builder, TypeHandle th)
{
    if (th.IsByRef())
    {
        builder.AppendElementType(ELEMENT_TYPE_BYREF);
        th = th.AsTypeDesc()->GetRootTypeParam();
    }

    CorElementType ty = th.GetSignatureCorElementType();
    if (CorTypeInfo::IsPrimitiveType(ty) ||
        ty == ELEMENT_TYPE_OBJECT || ty == ELEMENT_TYPE_STRING)
    {
        builder.AppendElementType(ty);
        return;
    }

    _ASSERTE(ty == ELEMENT_TYPE_VALUETYPE || ty == ELEMENT_TYPE_CLASS);
    builder.AppendElementType(ELEMENT_TYPE_INTERNAL);
    builder.AppendPointer(th.AsPtr());
}

PCCOR_SIGNATURE TailCallHelp::AllocateSignature(LoaderAllocator* pLoaderAlloc, SigBuilder& sig, DWORD* sigLen)
{
    PCCOR_SIGNATURE pBuilderSig = (PCCOR_SIGNATURE)sig.GetSignature(sigLen);
    return (PCCOR_SIGNATURE)AllocateBlob(pLoaderAlloc, pBuilderSig, *sigLen);
}

void* TailCallHelp::AllocateBlob(LoaderAllocator* pLoaderAllocator, const void* blob, size_t blobLen)
{
    AllocMemTracker pamTracker;
    PVOID newBlob = pamTracker.Track(pLoaderAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(blobLen)));
    memcpy(newBlob, blob, blobLen);

    pamTracker.SuppressRelease();
    return newBlob;
}
