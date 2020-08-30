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
#include "ngenhash.inl"
#include "compile.h"

#include "eventtrace.h"

const char* FormatSig(MethodDesc* pMD, LoaderHeap *pHeap, AllocMemTracker *pamTracker);

ILStubCache::ILStubCache(LoaderHeap *pHeap) :
    CClosedHashBase(
#ifdef _DEBUG
                      3,
#else
                      17,    // CClosedHashTable will grow as necessary
#endif

                      sizeof(ILCHASHENTRY),
                      FALSE
                   ),
    m_crst(CrstStubCache, CRST_UNSAFE_ANYMODE),
    m_heap(pHeap),
    m_pStubMT(NULL)
{
    WRAPPER_NO_CONTRACT;
}

void ILStubCache::Init(LoaderHeap* pHeap)
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK(NULL == m_heap);
    m_heap = pHeap;
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
                                                           FALSE /* fComPlusCallInfo */,
                                                           pMT,
                                                           pamTracker);

    // Note: The method desc memory is zero initialized

    DynamicMethodDesc* pMD = (DynamicMethodDesc*)pChunk->GetFirstMethodDesc();

    pMD->SetMemberDef(0);
    pMD->SetSlot(MethodTable::NO_SLOT);       // we can't ever use the slot for dynamic methods
    // the no metadata part of the method desc
    pMD->m_pszMethodName.SetValue((PTR_CUTF8)"IL_STUB");
    pMD->m_dwExtendedFlags = mdPublic | DynamicMethodDesc::nomdILStub;

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
    ULONG       callConvInfo;
    IfFailThrow(sigPtr.GetCallingConvInfo(&callConvInfo));

    if (!(callConvInfo & CORINFO_CALLCONV_HASTHIS))
    {
        pMD->m_dwExtendedFlags |= mdStatic;
        pMD->SetStatic();
    }

    pMD->m_pResolver = (ILStubResolver*)pamTracker->Track(pCreationHeap->AllocMem(S_SIZE_T(sizeof(ILStubResolver))));
#ifdef _DEBUG
    // Poison the ILStubResolver storage
    memset((void*)pMD->m_pResolver, 0xCC, sizeof(ILStubResolver));
#endif // _DEBUG
    pMD->m_pResolver = new (pMD->m_pResolver) ILStubResolver();
    pMD->GetILStubResolver()->SetLoaderHeap(pCreationHeap);

#ifdef FEATURE_ARRAYSTUB_AS_IL
    if (SF_IsArrayOpStub(dwStubFlags))
    {
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::ArrayOpStub);
    }
    else
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    if (SF_IsMulticastDelegateStub(dwStubFlags))
    {
        pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdMulticastStub;
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::MulticastDelegateStub);
    }
    else
#endif
    if (SF_IsWrapperDelegateStub(dwStubFlags))
    {
        pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdWrapperDelegateStub;
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::WrapperDelegateStub);
    }
    else
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
    if (SF_IsUnboxingILStub(dwStubFlags))
    {
        pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdUnboxingILStub;
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::UnboxingILStub);
    }
    else
    if (SF_IsInstantiatingStub(dwStubFlags))
    {
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::InstantiatingStub);
    }
    else
#endif
    if (SF_IsTailCallStoreArgsStub(dwStubFlags))
    {
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::TailCallStoreArgsStub);
    }
    else
    if (SF_IsTailCallCallTargetStub(dwStubFlags))
    {
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::TailCallCallTargetStub);
    }
    else
#ifdef FEATURE_COMINTEROP
    if (SF_IsCOMStub(dwStubFlags))
    {
        // mark certain types of stub MDs with random flags so ILStubManager recognizes them
        if (SF_IsReverseStub(dwStubFlags))
        {
            pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdReverseStub;

            ILStubResolver::ILStubType type = ILStubResolver::COMToCLRInteropStub;
            pMD->GetILStubResolver()->SetStubType(type);
        }
        else
        {
            ILStubResolver::ILStubType type =  ILStubResolver::CLRToCOMInteropStub;
            pMD->GetILStubResolver()->SetStubType(type);
        }
    }
    else
#endif
    if (SF_IsStructMarshalStub(dwStubFlags))
    {
        pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdStructMarshalStub;
        pMD->GetILStubResolver()->SetStubType(ILStubResolver::StructMarshalInteropStub);
    }
    else
    {
        // mark certain types of stub MDs with random flags so ILStubManager recognizes them
        if (SF_IsReverseStub(dwStubFlags))
        {
            pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdReverseStub;
#if !defined(TARGET_X86)
            pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdUnmanagedCallersOnlyStub;
#endif
            pMD->GetILStubResolver()->SetStubType(ILStubResolver::NativeToCLRInteropStub);
        }
        else
        {
            if (SF_IsDelegateStub(dwStubFlags))
            {
                pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdDelegateStub;
            }
            else if (SF_IsCALLIStub(dwStubFlags))
            {
                pMD->m_dwExtendedFlags |= DynamicMethodDesc::nomdCALLIStub;
            }
            pMD->GetILStubResolver()->SetStubType(ILStubResolver::CLRToNativeInteropStub);
        }
    }

// if we made it this far, we can set a more descriptive stub name
#ifdef FEATURE_ARRAYSTUB_AS_IL
    if (SF_IsArrayOpStub(dwStubFlags))
    {
        switch(dwStubFlags)
        {
            case ILSTUB_ARRAYOP_GET: pMD->m_pszMethodName.SetValue((PTR_CUTF8)"IL_STUB_Array_Get");
                                             break;
            case ILSTUB_ARRAYOP_SET: pMD->m_pszMethodName.SetValue((PTR_CUTF8)"IL_STUB_Array_Set");
                                             break;
            case ILSTUB_ARRAYOP_ADDRESS: pMD->m_pszMethodName.SetValue((PTR_CUTF8)"IL_STUB_Array_Address");
                                             break;
            default: _ASSERTE(!"Unknown array il stub");
        }
    }
    else
#endif
    {
        pMD->m_pszMethodName.SetValue(pMD->GetILStubResolver()->GetStubMethodName());
    }


#ifdef _DEBUG
    pMD->m_pszDebugMethodName = RelativePointer<PTR_CUTF8>::GetValueAtPtr(PTR_HOST_MEMBER_TADDR(DynamicMethodDesc, pMD, m_pszMethodName));
    pMD->m_pszDebugClassName  = ILStubResolver::GetStubClassName(pMD);  // must be called after type is set
    pMD->m_pszDebugMethodSignature = FormatSig(pMD, pCreationHeap, pamTracker);
    pMD->m_pDebugMethodTable.SetValue(pMT);
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
    if (pModule->IsSystem() || pModule->GetDomain()->AsAppDomain()->IsCompilationDomain())
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
            MethodTable* pNewMT = CreateMinimalMethodTable(pModule, m_heap, &amt);
            amt.SuppressRelease();
            VolatileStore<MethodTable*>(&m_pStubMT, pNewMT);
        }
    }

    RETURN m_pStubMT;
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
    MethodDesc *pTargetMD,
    ILStubHashBlob* pParams,
    DWORD dwStubFlags,
    Module* pSigModule,
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
    bool bFireETWCacheHitEvent = true;

#ifndef DACCESS_COMPILE
    ILStubHashBlob* pBlob       = NULL;

    INDEBUG(LPCSTR  pszResult   = "[hit cache]");


    if (SF_IsSharedStub(dwStubFlags))
    {
        CrstHolder ch(&m_crst);

        // Try to find the stub
        ILCHASHENTRY*   phe         = NULL;

        phe = (ILCHASHENTRY*)Find((LPVOID)pParams);
        if (phe)
        {
            pMD = phe->m_pMethodDesc;
            if (pMD == pLastMD)
                bFireETWCacheHitEvent = false;
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

        MethodTable *pStubMT = GetOrCreateStubMethodTable(pContainingModule);

        SigTypeContext typeContext;
        if (pTargetMD != NULL)
        {
            SigTypeContext::InitTypeContext(pTargetMD, &typeContext);
        }

        pMD = ILStubCache::CreateNewMethodDesc(m_heap, pStubMT, dwStubFlags, pSigModule, pSig, cbSig, &typeContext, pamTracker);

        if (SF_IsSharedStub(dwStubFlags))
        {
            size_t cbSizeOfBlob = pParams->m_cbSizeOfBlob;
            AllocMemHolder<ILStubHashBlob> pBlobHolder( m_heap->AllocMem(S_SIZE_T(cbSizeOfBlob)) );

            CrstHolder ch(&m_crst);

            ILCHASHENTRY*   phe         = NULL;

            bool bNew;
            phe = (ILCHASHENTRY*)FindOrAdd((LPVOID)pParams, bNew);
            bILStubCreator |= bNew;

            if (NULL != phe)
            {
                if (bNew)
                {
                    pBlobHolder.SuppressRelease();

                    phe->m_pMethodDesc   = pMD;
                    pBlob = pBlobHolder;
                    phe->m_pBlob         = pBlob;

                    _ASSERTE(pParams->m_cbSizeOfBlob == cbSizeOfBlob);
                    memcpy(pBlob, pParams, cbSizeOfBlob);

                    INDEBUG(pszResult   = "[missed cache]");
                    bFireETWCacheHitEvent = false;
                }
                else
                {
                    INDEBUG(pszResult   = "[hit cache][wasted new MethodDesc due to race]");
                }
                pMD = phe->m_pMethodDesc;
            }
            else
            {
                pMD = NULL;
            }
        }
        else
        {
            INDEBUG(pszResult   = "[cache disabled for COM->CLR field access stubs]");
        }
    }


    if (!pMD)
    {
        // Couldn't grow hash table due to lack of memory.
        COMPlusThrowOM();
    }

#ifdef _DEBUG
    CQuickBytes qbManaged;
    PrettyPrintSig(pSig,  cbSig, "*",  &qbManaged, pSigModule->GetMDImport(), NULL);
    LOG((LF_STUBS, LL_INFO1000, "ILSTUBCACHE: ILStubCache::GetStubMethodDesc %s StubMD: %p module: %p blob: %p sig: %s\n", pszResult, pMD, pSigModule, pBlob, qbManaged.Ptr()));
#endif // _DEBUG
#endif // DACCESS_COMPILE

    RETURN pMD;
}

void ILStubCache::DeleteEntry(void* pParams)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    CrstHolder ch(&m_crst);

    ILCHASHENTRY*   phe         = NULL;

    phe = (ILCHASHENTRY*)Find((LPVOID)pParams);
    if (phe)
    {
#ifdef _DEBUG
        LOG((LF_STUBS, LL_INFO1000, "ILSTUBCACHE: ILStubCache::DeleteEntry StubMD: %p\n", phe->m_pMethodDesc));
#endif

        Delete(pParams);
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

//---------------------------------------------------------
// Destructor
//---------------------------------------------------------
ILStubCache::~ILStubCache()
{
}


//*****************************************************************************
// Hash is called with a pointer to an element in the table.  You must override
// this method and provide a hash algorithm for your element type.
//*****************************************************************************
unsigned int ILStubCache::Hash(       // The key value.
    void const*  pData)                // Raw data to hash.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const ILStubHashBlob* pBlob = (const ILStubHashBlob *)pData;

    size_t cb   = pBlob->m_cbSizeOfBlob - sizeof(ILStubHashBlobBase);
    int   hash = 0;

    for (size_t i = 0; i < cb; i++)
    {
        hash = _rotl(hash,1) + pBlob->m_rgbBlobData[i];
    }

    return hash;
}

//*****************************************************************************
// Compare is used in the typical memcmp way, 0 is eqaulity, -1/1 indicate
// direction of miscompare.  In this system everything is always equal or not.
//*****************************************************************************
unsigned int ILStubCache::Compare(    // 0, -1, or 1.
    void const*  pData,                // Raw key data on lookup.
    BYTE*        pElement)             // The element to compare data against.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    const ILStubHashBlob* pBlob1    = (const ILStubHashBlob*)pData;
    const ILStubHashBlob* pBlob2    = (const ILStubHashBlob*)GetKey(pElement);
    size_t cb1 = pBlob1->m_cbSizeOfBlob - sizeof(ILStubHashBlobBase);
    size_t cb2 = pBlob2->m_cbSizeOfBlob - sizeof(ILStubHashBlobBase);

    if (cb1 != cb2)
    {
        return 1; // not equal
    }
    else
    {
        // @TODO: use memcmp
        for (size_t i = 0; i < cb1; i++)
        {
            if (pBlob1->m_rgbBlobData[i] != pBlob2->m_rgbBlobData[i])
            {
                return 1; // not equal
            }
        }
        return 0;   // equal
    }
}

//*****************************************************************************
// Return true if the element is free to be used.
//*****************************************************************************
CClosedHashBase::ELEMENTSTATUS ILStubCache::Status(     // The status of the entry.
    BYTE*        pElement)             // The element to check.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc* pMD = ((ILCHASHENTRY*)pElement)->m_pMethodDesc;

    if (pMD == NULL)
    {
        return FREE;
    }
    else if (pMD == (MethodDesc*)(-((INT_PTR)1)))
    {
        return DELETED;
    }
    else
    {
        return USED;
    }
}

//*****************************************************************************
// Sets the status of the given element.
//*****************************************************************************
void ILStubCache::SetStatus(
    BYTE*         pElement,            // The element to set status for.
    CClosedHashBase::ELEMENTSTATUS eStatus)             // New status.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCHASHENTRY* phe = (ILCHASHENTRY*)pElement;

    switch (eStatus)
    {
        case FREE:    phe->m_pMethodDesc = NULL;   break;
        case DELETED: phe->m_pMethodDesc = (MethodDesc*)(-((INT_PTR)1)); break;
        default:
            _ASSERTE(!"MLCacheEntry::SetStatus(): Bad argument.");
    }
}

//*****************************************************************************
// Returns the internal key value for an element.
//*****************************************************************************
void* ILStubCache::GetKey(             // The data to hash on.
    BYTE*        pElement)             // The element to return data ptr for.
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ILCHASHENTRY* phe = (ILCHASHENTRY*)pElement;
    return (void *)(phe->m_pBlob);
}

#ifdef FEATURE_PREJIT

// ============================================================================
// Stub method hash entry methods
// ============================================================================
PTR_MethodDesc StubMethodHashEntry::GetMethod()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return pMD;
}

PTR_MethodDesc StubMethodHashEntry::GetStubMethod()
{
    LIMITED_METHOD_DAC_CONTRACT;
    return pStubMD;
}

#ifndef DACCESS_COMPILE

void StubMethodHashEntry::SetMethodAndStub(MethodDesc *pMD, MethodDesc *pStubMD)
{
    LIMITED_METHOD_CONTRACT;
    this->pMD = pMD;
    this->pStubMD = pStubMD;
}

// ============================================================================
// Stub method hash table methods
// ============================================================================
/* static */ StubMethodHashTable *StubMethodHashTable::Create(LoaderAllocator *pAllocator, Module *pModule, DWORD dwNumBuckets, AllocMemTracker *pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END

    LoaderHeap *pHeap = pAllocator->GetLowFrequencyHeap();
    StubMethodHashTable *pThis = (StubMethodHashTable *)pamTracker->Track(pHeap->AllocMem((S_SIZE_T)sizeof(StubMethodHashTable)));

    new (pThis) StubMethodHashTable(pModule, pHeap, dwNumBuckets);

    return pThis;
}

// Calculate a hash value for a key
static DWORD Hash(MethodDesc *pMD)
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwHash = 0x87654321;
#define INST_HASH_ADD(_value) dwHash = ((dwHash << 5) + dwHash) ^ (_value)

    INST_HASH_ADD(pMD->GetMemberDef());

    Instantiation inst = pMD->GetClassInstantiation();
    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        TypeHandle thArg = inst[i];

        if (thArg.GetMethodTable())
        {
            INST_HASH_ADD(thArg.GetCl());

            Instantiation sArgInst = thArg.GetInstantiation();
            for (DWORD j = 0; j < sArgInst.GetNumArgs(); j++)
            {
                TypeHandle thSubArg = sArgInst[j];
                if (thSubArg.GetMethodTable())
                    INST_HASH_ADD(thSubArg.GetCl());
                else
                    INST_HASH_ADD(thSubArg.GetSignatureCorElementType());
            }
        }
        else
            INST_HASH_ADD(thArg.GetSignatureCorElementType());
    }

    return dwHash;
}

MethodDesc *StubMethodHashTable::FindMethodDesc(MethodDesc *pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    MethodDesc *pMDResult = NULL;

    DWORD dwHash = Hash(pMD);
    StubMethodHashEntry_t* pSearch;
    LookupContext sContext;

    for (pSearch = BaseFindFirstEntryByHash(dwHash, &sContext);
         pSearch != NULL;
         pSearch = BaseFindNextEntryByHash(&sContext))
    {
        if (pSearch->GetMethod() == pMD)
        {
            pMDResult = pSearch->GetStubMethod();
            break;
        }
    }

    return pMDResult;
}

// Add method desc to the hash table; must not be present already
void StubMethodHashTable::InsertMethodDesc(MethodDesc *pMD, MethodDesc *pStubMD)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        INJECT_FAULT(COMPlusThrowOM(););
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(CheckPointer(pStubMD));
    }
    CONTRACTL_END

    StubMethodHashEntry_t *pNewEntry = (StubMethodHashEntry_t *)BaseAllocateEntry(NULL);
    pNewEntry->SetMethodAndStub(pMD, pStubMD);

    DWORD dwHash = Hash(pMD);
    BaseInsertEntry(dwHash, pNewEntry);
}

#ifdef FEATURE_NATIVE_IMAGE_GENERATION
// Save the hash table and any method descriptors referenced by it
void StubMethodHashTable::Save(DataImage *image, CorProfileData *pProfileData)
{
    WRAPPER_NO_CONTRACT;
    BaseSave(image, pProfileData);
}

void StubMethodHashTable::Fixup(DataImage *image)
{
    WRAPPER_NO_CONTRACT;
    BaseFixup(image);
}

void StubMethodHashTable::FixupEntry(DataImage *pImage, StubMethodHashEntry_t *pEntry, void *pFixupBase, DWORD cbFixupOffset)
{
    WRAPPER_NO_CONTRACT;
    pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(StubMethodHashEntry_t, pMD), pEntry->GetMethod());
    pImage->FixupField(pFixupBase, cbFixupOffset + offsetof(StubMethodHashEntry_t, pStubMD), pEntry->GetStubMethod());
}

bool StubMethodHashTable::ShouldSave(DataImage *pImage, StubMethodHashEntry_t *pEntry)
{
    STANDARD_VM_CONTRACT;

    MethodDesc *pMD = pEntry->GetMethod();
    if (pMD->GetClassification() == mcInstantiated)
    {
        // save entries only for "accepted" methods
        if (!pImage->GetPreloader()->IsMethodInTransitiveClosureOfInstantiations(CORINFO_METHOD_HANDLE(pMD)))
            return false;
    }

    // Save the entry only if the native code was successfully generated for the stub
    if (pImage->GetCodeAddress(pEntry->GetStubMethod()) == NULL)
        return false;

    return true;
}
#endif // FEATURE_NATIVE_IMAGE_GENERATION

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE

void StubMethodHashTable::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    BaseEnumMemoryRegions(flags);
}

void StubMethodHashTable::EnumMemoryRegionsForEntry(StubMethodHashEntry_t *pEntry, CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    if (pEntry->GetMethod().IsValid())
        pEntry->GetMethod()->EnumMemoryRegions(flags);
}

#endif // DACCESS_COMPILE

#endif // FEATURE_PREJIT
