// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ILStubCache.cpp
//

//


#include "common.h"
#include "ilstubcache.h"
#include "dllimport.h"
#include <formattype.h>
#include "jitinterface.h"
#include "sigbuilder.h"

#include "eventtrace.h"

const char* FormatSig(MethodDesc* pMD, LoaderHeap *pHeap, AllocMemTracker *pamTracker);

ILStubCache::ILStubCache(LoaderAllocator *pAllocator)
    : m_crst(CrstStubCache, CRST_UNSAFE_ANYMODE)
    , m_pAllocator(pAllocator)
    , m_pStubMT(NULL)
{
    WRAPPER_NO_CONTRACT;
}

void ILStubCache::Init(LoaderAllocator* pAllocator)
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK(NULL == m_pAllocator);
    m_pAllocator = pAllocator;
}

#ifndef DACCESS_COMPILE

void CreateModuleIndependentSignature(LoaderHeap* pCreationHeap,
                                      AllocMemTracker* pamTracker,
                                      Module* pSigModule,
                                      PCCOR_SIGNATURE pSig, DWORD cbSig,
                                      SigTypeContext *pTypeContext,
                                      PCCOR_SIGNATURE* ppNewSig, DWORD* pcbNewSig)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pSigModule, NULL_NOT_OK));
        PRECONDITION(CheckPointer(ppNewSig, NULL_NOT_OK));
        PRECONDITION(CheckPointer(pcbNewSig, NULL_NOT_OK));
    }
    CONTRACTL_END;

    SigPointer  sigPtr(pSig, cbSig);

    SigBuilder sigBuilder;
    sigPtr.ConvertToInternalSignature(pSigModule, pTypeContext, &sigBuilder);

    DWORD cbNewSig;
    PVOID pConvertedSig = sigBuilder.GetSignature(&cbNewSig);

    PVOID pNewSig = pamTracker->Track(pCreationHeap->AllocMem(S_SIZE_T(cbNewSig)));
    memcpy(pNewSig, pConvertedSig, cbNewSig);

    *ppNewSig = (PCCOR_SIGNATURE)pNewSig;
    *pcbNewSig = cbNewSig;
}

// static
MethodDesc* ILStubCache::CreateAndLinkNewILStubMethodDesc(LoaderAllocator* pAllocator, MethodTable* pMT, DWORD dwStubFlags,
                                             Module* pSigModule, PCCOR_SIGNATURE pSig, DWORD cbSig, SigTypeContext *pTypeContext,
                                             ILStubLinker* pStubLinker)
{
    CONTRACT (MethodDesc*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT, NULL_NOT_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;
    AllocMemTracker amTracker;

    MethodDesc *pStubMD = ILStubCache::CreateNewMethodDesc(pAllocator->GetHighFrequencyHeap(),
                                                           pMT,
                                                           dwStubFlags,
                                                           pSigModule,
                                                           pSig, cbSig,
                                                           pTypeContext,
                                                           &amTracker);

    amTracker.SuppressRelease();

    pStubLinker->SetStubMethodDesc(pStubMD);

    ILStubResolver *pResolver = pStubMD->AsDynamicMethodDesc()->GetILStubResolver();

    pResolver->SetStubMethodDesc(pStubMD);


    {
        UINT   maxStack;
        size_t cbCode = pStubLinker->Link(&maxStack);
        DWORD cbSig = pStubLinker->GetLocalSigSize();

        COR_ILMETHOD_DECODER * pILHeader = pResolver->AllocGeneratedIL(cbCode, cbSig, maxStack);
        BYTE * pbBuffer   = (BYTE *)pILHeader->Code;
        BYTE * pbLocalSig = (BYTE *)pILHeader->LocalVarSig;
        _ASSERTE(cbSig == pILHeader->cbLocalVarSig);

        size_t numEH = pStubLinker->GetNumEHClauses();
        if (numEH > 0)
        {
            pStubLinker->WriteEHClauses(pResolver->AllocEHSect(numEH));
        }

        pStubLinker->GenerateCode(pbBuffer, cbCode);
        pStubLinker->GetLocalSig(pbLocalSig, cbSig);

        pResolver->SetJitFlags(CORJIT_FLAGS(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB));
    }

    pResolver->SetTokenLookupMap(pStubLinker->GetTokenLookupMap());

    RETURN pStubMD;

}


namespace
{
    LPCUTF8 GetStubMethodName(DynamicMethodDesc::ILStubType type)
    {
        CONTRACTL
        {
            MODE_ANY;
            GC_NOTRIGGER;
            NOTHROW;
        }
        CONTRACTL_END;

        switch (type)
        {
            case DynamicMethodDesc::StubCLRToNativeInterop: return "IL_STUB_PInvoke";
            case DynamicMethodDesc::StubCLRToCOMInterop:    return "IL_STUB_CLRtoCOM";
            case DynamicMethodDesc::StubNativeToCLRInterop: return "IL_STUB_ReversePInvoke";
            case DynamicMethodDesc::StubCOMToCLRInterop:    return "IL_STUB_COMtoCLR";
            case DynamicMethodDesc::StubStructMarshalInterop: return "IL_STUB_StructMarshal";
#ifdef FEATURE_ARRAYSTUB_AS_IL
            case DynamicMethodDesc::StubArrayOp:            return "IL_STUB_Array";
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
            case DynamicMethodDesc::StubMulticastDelegate:  return "IL_STUB_MulticastDelegate_Invoke";
#endif
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
            case DynamicMethodDesc::StubUnboxingIL:         return "IL_STUB_UnboxingStub";
            case DynamicMethodDesc::StubInstantiating:      return "IL_STUB_InstantiatingStub";
#endif
            case DynamicMethodDesc::StubWrapperDelegate:    return "IL_STUB_WrapperDelegate_Invoke";
            case DynamicMethodDesc::StubTailCallStoreArgs:  return "IL_STUB_StoreTailCallArgs";
            case DynamicMethodDesc::StubTailCallCallTarget: return "IL_STUB_CallTailCallTarget";
            case DynamicMethodDesc::StubVirtualStaticMethodDispatch: return "IL_STUB_bVirtualStaticMethodDispatch";
            default:
                UNREACHABLE_MSG("Unknown stub type");
        }
    }
}

// static
MethodDesc* ILStubCache::CreateNewMethodDesc(LoaderHeap* pCreationHeap, MethodTable* pMT, DWORD dwStubFlags,
                                             Module* pSigModule, PCCOR_SIGNATURE pSig, DWORD cbSig, SigTypeContext *pTypeContext,
                                             AllocMemTracker* pamTracker)
{
    CONTRACT (MethodDesc*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pMT, NULL_NOT_OK));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // @TODO: reuse the same chunk for multiple methods
    MethodDescChunk* pChunk = MethodDescChunk::CreateChunk(pCreationHeap,
                                                           1,
                                                           mcDynamic,
                                                           TRUE /* fNonVtableSlot */,
                                                           TRUE /* fNativeCodeSlot */,
                                                           pMT,
                                                           pamTracker);

    // Note: The method desc memory is zero initialized

    DynamicMethodDesc* pMD = (DynamicMethodDesc*)pChunk->GetFirstMethodDesc();

    pMD->SetMemberDef(0);
    pMD->SetSlot(MethodTable::NO_SLOT);       // we can't ever use the slot for dynamic methods
    // the no metadata part of the method desc
    pMD->m_pszMethodName = (PTR_CUTF8)"IL_STUB";
    pMD->InitializeFlags(DynamicMethodDesc::FlagPublic | DynamicMethodDesc::FlagIsILStub);
    pMD->SetTemporaryEntryPoint(pMT->GetLoaderAllocator(), pamTracker);

    //
    // convert signature to a compatible signature if needed
    //
    PCCOR_SIGNATURE pNewSig;
    DWORD           cbNewSig;

    // If we are in the same module and don't have any generics, we can use the incoming signature.
    // Note that pTypeContext may be non-empty and the signature can still have no E_T_(M)VAR in it.
    // We could do a more precise check if we cared.
    if (pMT->GetModule() == pSigModule && (pTypeContext == NULL || pTypeContext->IsEmpty()))
    {
        pNewSig = pSig;
        cbNewSig = cbSig;
    }
    else
    {
        CreateModuleIndependentSignature(pCreationHeap, pamTracker, pSigModule, pSig, cbSig, pTypeContext, &pNewSig, &cbNewSig);
    }
    pMD->SetStoredMethodSig(pNewSig, cbNewSig);

    SigPointer  sigPtr(pNewSig, cbNewSig);
    uint32_t    callConvInfo;
    IfFailThrow(sigPtr.GetCallingConvInfo(&callConvInfo));

    if (!(callConvInfo & CORINFO_CALLCONV_HASTHIS))
    {
        pMD->SetFlags(DynamicMethodDesc::FlagStatic);
        pMD->SetStatic();
    }

    pMD->m_pResolver = (ILStubResolver*)pamTracker->Track(pCreationHeap->AllocMem(S_SIZE_T(sizeof(ILStubResolver))));
#ifdef _DEBUG
    // Poison the ILStubResolver storage
    memset((void*)pMD->m_pResolver, 0xCC, sizeof(ILStubResolver));
#endif // _DEBUG
    pMD->m_pResolver = new (pMD->m_pResolver) ILStubResolver();

#ifdef FEATURE_ARRAYSTUB_AS_IL
    if (SF_IsArrayOpStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubArrayOp);
    }
    else
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    if (SF_IsMulticastDelegateStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubMulticastDelegate);
    }
    else
#endif
    if (SF_IsWrapperDelegateStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubWrapperDelegate);
    }
    else
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
    if (SF_IsUnboxingILStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubUnboxingIL);
    }
    else
    if (SF_IsInstantiatingStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubInstantiating);
    }
    else
#endif
    if (SF_IsTailCallStoreArgsStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubTailCallStoreArgs);
    }
    else
    if (SF_IsTailCallCallTargetStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubTailCallCallTarget);
    }
    else
#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
        // mark certain types of stub MDs with random flags so ILStubManager recognizes them
        if (SF_IsReverseStub(dwStubFlags))
        {
            pMD->SetILStubType(DynamicMethodDesc::StubCOMToCLRInterop);
        }
        else
        {
            pMD->SetILStubType(DynamicMethodDesc::StubCLRToCOMInterop);
        }
    }
    else
#endif
    if (SF_IsStructMarshalStub(dwStubFlags))
    {
        // Struct marshal stub MethodDescs might be directly called from `call` IL instructions
        // so we want to keep their compile time data alive as long as the LoaderAllocator in case they're used again.
        pMD->GetILStubResolver()->SetLoaderHeap(pCreationHeap);
        pMD->SetILStubType(DynamicMethodDesc::StubStructMarshalInterop);
    }
    else
    {
        // mark certain types of stub MDs with random flags so ILStubManager recognizes them
        if (SF_IsReverseStub(dwStubFlags))
        {
            pMD->SetILStubType(DynamicMethodDesc::StubNativeToCLRInterop);
        }
        else
        {
            if (SF_IsDelegateStub(dwStubFlags))
            {
                pMD->SetFlags(DynamicMethodDesc::FlagIsDelegate);
            }
            else if (SF_IsCALLIStub(dwStubFlags))
            {
                pMD->SetFlags(DynamicMethodDesc::FlagIsCALLI);
            }
            pMD->SetILStubType(DynamicMethodDesc::StubCLRToNativeInterop);
        }
    }

    if (SF_IsVirtualStaticMethodDispatchStub(dwStubFlags))
    {
        pMD->SetILStubType(DynamicMethodDesc::StubVirtualStaticMethodDispatch);
    }

// if we made it this far, we can set a more descriptive stub name
#ifdef FEATURE_ARRAYSTUB_AS_IL
    if (SF_IsArrayOpStub(dwStubFlags))
    {
        switch(dwStubFlags)
        {
            case ILSTUB_ARRAYOP_GET: pMD->m_pszMethodName = (PTR_CUTF8)"IL_STUB_Array_Get";
                                             break;
            case ILSTUB_ARRAYOP_SET: pMD->m_pszMethodName = (PTR_CUTF8)"IL_STUB_Array_Set";
                                             break;
            case ILSTUB_ARRAYOP_ADDRESS: pMD->m_pszMethodName = (PTR_CUTF8)"IL_STUB_Array_Address";
                                             break;
            default: _ASSERTE(!"Unknown array il stub");
        }
    }
    else
#endif
    {
        pMD->m_pszMethodName = GetStubMethodName(pMD->GetILStubType());
    }


#ifdef _DEBUG
    pMD->m_pszDebugMethodName = pMD->m_pszMethodName;
    pMD->m_pszDebugClassName  = ILStubResolver::GetStubClassName(pMD);  // must be called after type is set
    pMD->m_pszDebugMethodSignature = FormatSig(pMD, pCreationHeap, pamTracker);
    pMD->m_pDebugMethodTable = pMT;
#endif // _DEBUG

    RETURN pMD;
}

//
// This will get or create a MethodTable in the Module/AppDomain on which
// we can place a new IL stub MethodDesc.
//
MethodTable* ILStubCache::GetOrCreateStubMethodTable(Module* pModule)
{
    CONTRACT (MethodTable*)
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

#ifdef _DEBUG
    if (pModule->IsSystem())
    {
        // in the shared domain and compilation AD we are associated with the module
        CONSISTENCY_CHECK(pModule->GetILStubCache() == this);
    }
    else
    {
        // otherwise we are associated with the LoaderAllocator
        LoaderAllocator* pStubLoaderAllocator = LoaderAllocator::GetLoaderAllocator(this);
        CONSISTENCY_CHECK(pStubLoaderAllocator == pModule->GetLoaderAllocator());
    }
#endif // _DEBUG

    if (NULL == m_pStubMT)
    {
        CrstHolder ch(&m_crst);

        if (NULL == m_pStubMT)
        {
            AllocMemTracker amt;
            MethodTable* pNewMT = CreateMinimalMethodTable(pModule, m_pAllocator, &amt);
            amt.SuppressRelease();
            VolatileStore<MethodTable*>(&m_pStubMT, pNewMT);
        }
    }

    RETURN m_pStubMT;
}


MethodDesc* ILStubCache::LookupStubMethodDesc(ILStubHashBlob* pHashBlob)
{
    CrstHolder ch(&m_crst);

    // Try to find the stub
    const ILStubCacheEntry* phe = m_hashMap.LookupPtr(pHashBlob);
    if (phe)
    {
        return phe->m_pMethodDesc;
    }

    return NULL;
}

MethodDesc* ILStubCache::InsertStubMethodDesc(MethodDesc *pMD, ILStubHashBlob* pHashBlob)
{
    size_t cbSizeOfBlob = pHashBlob->m_cbSizeOfBlob;

    CrstHolder ch(&m_crst);

    const ILStubCacheEntry* phe = m_hashMap.LookupPtr(pHashBlob);
    if (phe == NULL)
    {
        AllocMemHolder<ILStubHashBlob> pBlobHolder( m_pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbSizeOfBlob)) );
        ILStubHashBlob* pBlob = pBlobHolder;
        _ASSERTE(pHashBlob->m_cbSizeOfBlob == cbSizeOfBlob);
        memcpy(pBlob, pHashBlob, cbSizeOfBlob);

        m_hashMap.Add(ILStubCacheEntry{ pMD, pBlob });
        pBlobHolder.SuppressRelease();

        return pMD;
    }
    else
    {
        return phe->m_pMethodDesc;
    }
}
#endif // DACCESS_COMPILE

//
// NGEN'ed IL stubs
//
//    - We will never NGEN a CALLI pinvoke or vararg pinvoke
//
//    - We will always place the IL stub MethodDesc on the same MethodTable that the
//      PInvoke or COM Interop call declaration lives on.
//
//    - We will not pre-populate our runtime ILStubCache with compile-time
//      information (i.e. NGENed stubs are only reachable from the same NGEN image.)
//
// JIT'ed IL stubs
//
//    - The ILStubCache is per-BaseDomain
//
//    - Each BaseDomain's ILStubCache will lazily create a "minimal MethodTable" to
//      serve as the home for IL stub MethodDescs
//
//    - The created MethodTables will use the Module belonging to one of the
//      following, based on what type of interop stub we need to create first.
//
//        - If that stub is for a static-sig-based pinvoke, we will use the
//          Module belonging to that pinvoke's MethodDesc.
//
//        - If that stub is for a CALLI or vararg pinvoke, we will use the
//          Module belonging to the VASigCookie that the caller supplied to us.
//
// It's important to point out that the Module we latch onto here has no knowledge
// of the MethodTable that we've just "added" to it.  There only exists a "back
// pointer" to the Module from the MethodTable itself.  So we're really only using
// that module to answer the question of what BaseDomain the MethodTable lives in.
// So as long as the BaseDomain for that module is the same as the BaseDomain the
// ILStubCache lives in, I think we have a fairly consistent story here.
//
// We're relying on the fact that a VASigCookie may only mention types within the
// corresponding module used to qualify the signature and the fact that interop
// stubs may only reference CoreLib code or code related to a type mentioned in
// the signature.  Both of these are true unless the sig is allowed to contain
// ELEMENT_TYPE_INTERNAL, which may refer to any type.
//
// We can only access E_T_INTERNAL through LCG, which does not permit referring
// to types in other BaseDomains.
//
//
// Places for improvement:
//
//    - allow NGEN'ing of CALLI pinvoke and vararg pinvoke
//
//    - pre-populate the per-BaseDomain cache with IL stubs from  NGEN'ed image
//

MethodDesc* ILStubCache::GetStubMethodDesc(
    MethodDesc* pTargetMD,
    ILStubHashBlob* pHashBlob,
    DWORD dwStubFlags,
    Module* pSigModule,
    Module* pSigLoaderModule,
    SigTypeContext* pTypeContext,
    PCCOR_SIGNATURE pSig,
    DWORD cbSig,
    AllocMemTracker* pamTracker,
    bool& bILStubCreator,
    MethodDesc *pLastMD)
{
    CONTRACT (MethodDesc*)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    MethodDesc*     pMD         = NULL;

#ifndef DACCESS_COMPILE
    ILStubHashBlob* pBlob       = NULL;

    INDEBUG(LPCSTR  pszResult   = "[hit cache]");

    if (SF_IsSharedStub(dwStubFlags))
    {
        CrstHolder ch(&m_crst);

        // Try to find the stub
        const ILStubCacheEntry* phe = m_hashMap.LookupPtr(pHashBlob);
        if (phe)
        {
            pMD = phe->m_pMethodDesc;
        }
    }

    if (!pMD)
    {
        //
        // Couldn't find it, let's make a new one.
        //

        Module *pContainingModule = pSigModule;
        if (pTargetMD != NULL)
        {
            // loader module may be different from signature module for generic targets
            pContainingModule = pTargetMD->GetLoaderModule();
        }

        if (pTypeContext != NULL)
        {
            // for generic calli targets loader module is given directly
            pContainingModule = pSigLoaderModule;
        }

        MethodTable *pStubMT = GetOrCreateStubMethodTable(pContainingModule);

        SigTypeContext typeContext;
        if (pTargetMD != NULL)
        {
            SigTypeContext::InitTypeContext(pTargetMD, &typeContext);
        }

        if (pTypeContext != NULL)
        {
            // for generic calli targets typeContext is given directly
            typeContext = *pTypeContext;
        }

        pMD = ILStubCache::CreateNewMethodDesc(m_pAllocator->GetHighFrequencyHeap(), pStubMT, dwStubFlags, pSigModule, pSig, cbSig, &typeContext, pamTracker);

        if (SF_IsSharedStub(dwStubFlags))
        {
            size_t cbSizeOfBlob = pHashBlob->m_cbSizeOfBlob;

            CrstHolder ch(&m_crst);

            const ILStubCacheEntry* phe = m_hashMap.LookupPtr(pHashBlob);
            if (phe == NULL)
            {
                AllocMemHolder<ILStubHashBlob> pBlobHolder( m_pAllocator->GetHighFrequencyHeap()->AllocMem(S_SIZE_T(cbSizeOfBlob)) );
                pBlob = pBlobHolder;
                _ASSERTE(pHashBlob->m_cbSizeOfBlob == cbSizeOfBlob);
                memcpy(pBlob, pHashBlob, cbSizeOfBlob);

                m_hashMap.Add(ILStubCacheEntry{ pMD, pBlob });
                pBlobHolder.SuppressRelease();

                INDEBUG(pszResult   = "[missed cache]");
                bILStubCreator = true;
            }
            else
            {
                INDEBUG(pszResult   = "[hit cache][wasted new MethodDesc due to race]");
                pMD = phe->m_pMethodDesc;
            }
        }
        else
        {
            INDEBUG(pszResult   = "[cache disabled for COM->CLR field access stubs]");
        }
    }

#ifdef _DEBUG
    CQuickBytes qbManaged;
    PrettyPrintSig(pSig,  cbSig, "*",  &qbManaged, pSigModule->GetMDImport(), NULL);
    LOG((LF_STUBS, LL_INFO1000, "ILSTUBCACHE: ILStubCache::GetStubMethodDesc %s StubMD: %p module: %p blob: %p sig: %s\n", pszResult, pMD, pSigModule, pBlob, qbManaged.Ptr()));
#endif // _DEBUG
#endif // DACCESS_COMPILE

    RETURN pMD;
}

void ILStubCache::DeleteEntry(ILStubHashBlob* pHashBlob)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CrstHolder ch(&m_crst);

    const ILStubCacheEntry *phe = m_hashMap.LookupPtr(pHashBlob);
    if (phe != NULL)
    {
#ifdef _DEBUG
        LOG((LF_STUBS, LL_INFO1000, "ILSTUBCACHE: ILStubCache::DeleteEntry StubMD: %p\n", phe->m_pMethodDesc));
#endif
        m_hashMap.Remove(pHashBlob);
    }
}

void ILStubCache::AddMethodDescChunkWithLockTaken(MethodDesc *pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pMD));
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    CrstHolder ch(&m_crst);

    pMD->GetMethodTable()->GetClass()->AddChunkIfItHasNotBeenAdded(pMD->GetMethodDescChunk());
#endif // DACCESS_COMPILE
}

ILStubCache::ILStubCacheTraits::count_t ILStubCache::ILStubCacheTraits::Hash(_In_ key_t key)
{
    LIMITED_METHOD_CONTRACT;

    size_t cb = key->m_cbSizeOfBlob - sizeof(ILStubHashBlobBase);
    int   hash = 0;

    for (size_t i = 0; i < cb; i++)
    {
        hash = _rotl(hash, 1) + key->m_rgbBlobData[i];
    }

    return hash;
}

bool ILStubCache::ILStubCacheTraits::Equals(_In_ key_t lhs, _In_ key_t rhs)
{
    LIMITED_METHOD_CONTRACT;

    if (lhs->m_cbSizeOfBlob != rhs->m_cbSizeOfBlob)
        return false;

    size_t blobDataSize = lhs->m_cbSizeOfBlob - sizeof(ILStubHashBlobBase);
    return memcmp(lhs->m_rgbBlobData, rhs->m_rgbBlobData, blobDataSize) == 0;
}
