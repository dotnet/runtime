// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ILStubResolver.cpp
//

//


#include "common.h"

#include "field.h"

// returns pointer to IL code
BYTE* ILStubResolver::GetCodeInfo(unsigned* pCodeSize, unsigned* pStackSize, CorInfoOptions* pOptions, unsigned* pEHSize)
{
    CONTRACT(BYTE*)
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCodeSize));
        PRECONDITION(CheckPointer(pStackSize));
        PRECONDITION(CheckPointer(pOptions));
        PRECONDITION(CheckPointer(pEHSize));
        PRECONDITION(CheckPointer(m_pCompileTimeState));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

#ifndef DACCESS_COMPILE
    CORINFO_METHOD_INFO methodInfo;
    getMethodInfoILMethodHeaderHelper(&m_pCompileTimeState->m_ILHeader, &methodInfo);

    *pCodeSize = methodInfo.ILCodeSize;
    *pStackSize = methodInfo.maxStack;
    *pOptions = methodInfo.options;
    *pEHSize = methodInfo.EHcount;

    RETURN methodInfo.ILCode;
#else // DACCESS_COMPILE
    DacNotImpl();
    RETURN NULL;
#endif // DACCESS_COMPILE
}

// static
LPCUTF8 ILStubResolver::GetStubClassName(MethodDesc* pMD)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        SUPPORTS_DAC;
        PRECONDITION(pMD->IsILStub());
    }
    CONTRACTL_END;

    return "ILStubClass";
}

void ILStubResolver::GetJitContext(SecurityControlFlags* pSecurityControlFlags,
                                   TypeHandle* pTypeOwner)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pSecurityControlFlags));
        PRECONDITION(CheckPointer(pTypeOwner));
    }
    CONTRACTL_END;

    *pSecurityControlFlags = DynamicResolver::SkipVisibilityChecks;
    *pTypeOwner = TypeHandle();
}

ChunkAllocator* ILStubResolver::GetJitMetaHeap()
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(FALSE);
    return NULL;
}

bool ILStubResolver::RequiresAccessCheck()
{
    LIMITED_METHOD_CONTRACT;

#ifdef _DEBUG
    SecurityControlFlags securityControlFlags;
    TypeHandle typeOwner;
    GetJitContext(&securityControlFlags, &typeOwner);

    // Verify we can return false below because we skip visibility checks
    _ASSERTE((securityControlFlags & DynamicResolver::SkipVisibilityChecks) == DynamicResolver::SkipVisibilityChecks);
#endif // _DEBUG

    return false;
}

CORJIT_FLAGS ILStubResolver::GetJitFlags()
{
    LIMITED_METHOD_CONTRACT;
    return m_jitFlags;
}

SigPointer
ILStubResolver::GetLocalSig()
{
    STANDARD_VM_CONTRACT;

    return SigPointer(
        m_pCompileTimeState->m_ILHeader.LocalVarSig,
        m_pCompileTimeState->m_ILHeader.cbLocalVarSig);
}

OBJECTHANDLE ILStubResolver::ConstructStringLiteral(mdToken token)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(FALSE);
    return NULL;
}

BOOL ILStubResolver::IsValidStringRef(mdToken metaTok)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(FALSE);
    return FALSE;
}

STRINGREF ILStubResolver::GetStringLiteral(mdToken metaTok)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(FALSE);
    return NULL;
}

void ILStubResolver::ResolveToken(mdToken token, ResolvedToken* resolvedToken)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(resolvedToken != NULL);

    switch (TypeFromToken(token))
    {
    case mdtMethodDef:
        {
            MethodDesc* pMD = m_pCompileTimeState->m_tokenLookupMap.LookupMethodDef(token);
            _ASSERTE(pMD);
            resolvedToken->Method = pMD;
            resolvedToken->TypeHandle = TypeHandle(pMD->GetMethodTable());
        }
        break;

    case mdtTypeDef:
        {
            TypeHandle typeHnd = m_pCompileTimeState->m_tokenLookupMap.LookupTypeDef(token);
            _ASSERTE(!typeHnd.IsNull());
            resolvedToken->TypeHandle = typeHnd;
        }
        break;

    case mdtFieldDef:
        {
            FieldDesc* pFD = m_pCompileTimeState->m_tokenLookupMap.LookupFieldDef(token);
            _ASSERTE(pFD);
            resolvedToken->Field = pFD;
            resolvedToken->TypeHandle = TypeHandle(pFD->GetEnclosingMethodTable());
        }
        break;

#if !defined(DACCESS_COMPILE)
    case mdtMemberRef:
        {
            TokenLookupMap::MemberRefEntry entry = m_pCompileTimeState->m_tokenLookupMap.LookupMemberRef(token);
            if (entry.Type == mdtFieldDef)
            {
                _ASSERTE(entry.Entry.Field != NULL);

                if (entry.ClassSignatureToken != mdTokenNil)
                    resolvedToken->TypeSignature = m_pCompileTimeState->m_tokenLookupMap.LookupSig(entry.ClassSignatureToken);

                resolvedToken->Field = entry.Entry.Field;
                resolvedToken->TypeHandle = TypeHandle(entry.Entry.Field->GetApproxEnclosingMethodTable());
            }
            else
            {
                _ASSERTE(entry.Type == mdtMethodDef);
                _ASSERTE(entry.Entry.Method != NULL);

                if (entry.ClassSignatureToken != mdTokenNil)
                    resolvedToken->TypeSignature = m_pCompileTimeState->m_tokenLookupMap.LookupSig(entry.ClassSignatureToken);

                resolvedToken->Method = entry.Entry.Method;
                MethodTable* pMT = entry.Entry.Method->GetMethodTable();
                _ASSERTE(!pMT->ContainsGenericVariables());
                resolvedToken->TypeHandle = TypeHandle(pMT);
            }
        }
        break;

    case mdtMethodSpec:
        {
            TokenLookupMap::MethodSpecEntry entry = m_pCompileTimeState->m_tokenLookupMap.LookupMethodSpec(token);
            _ASSERTE(entry.Method != NULL);

            if (entry.ClassSignatureToken != mdTokenNil)
                resolvedToken->TypeSignature = m_pCompileTimeState->m_tokenLookupMap.LookupSig(entry.ClassSignatureToken);

            if (entry.MethodSignatureToken != mdTokenNil)
                resolvedToken->MethodSignature = m_pCompileTimeState->m_tokenLookupMap.LookupSig(entry.MethodSignatureToken);

            resolvedToken->Method = entry.Method;
            MethodTable* pMT = entry.Method->GetMethodTable();
            _ASSERTE(!pMT->ContainsGenericVariables());
            resolvedToken->TypeHandle = TypeHandle(pMT);
        }
        break;
#endif // !defined(DACCESS_COMPILE)

    default:
        UNREACHABLE_MSG("unexpected metadata token type");
    }
}

//---------------------------------------------------------------------------------------
//
SigPointer
ILStubResolver::ResolveSignature(
    mdToken token)
{
    STANDARD_VM_CONTRACT;

    if (token == TOKEN_ILSTUB_TARGET_SIG)
        return m_pCompileTimeState->m_StubTargetMethodSig;

    return m_pCompileTimeState->m_tokenLookupMap.LookupSig(token);
}

//---------------------------------------------------------------------------------------
//
SigPointer
ILStubResolver::ResolveSignatureForVarArg(
    mdToken token)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(FALSE);
    return SigPointer();
}

//---------------------------------------------------------------------------------------
//
void ILStubResolver::GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(m_pCompileTimeState));
        PRECONDITION(CheckPointer(m_pCompileTimeState->m_ILHeader.EH));
        PRECONDITION(EHnumber < m_pCompileTimeState->m_ILHeader.EH->EHCount());
    }
    CONTRACTL_END;

    COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehClause;
    const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;
    ehInfo = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)m_pCompileTimeState->m_ILHeader.EH->EHClause(EHnumber, &ehClause);
    clause->Flags = (CORINFO_EH_CLAUSE_FLAGS)ehInfo->GetFlags();
    clause->TryOffset = ehInfo->GetTryOffset();
    clause->TryLength = ehInfo->GetTryLength();
    clause->HandlerOffset = ehInfo->GetHandlerOffset();
    clause->HandlerLength = ehInfo->GetHandlerLength();
    clause->ClassToken = ehInfo->GetClassToken();
    clause->FilterOffset = ehInfo->GetFilterOffset();
}

void ILStubResolver::SetStubMethodDesc(MethodDesc* pStubMD)
{
    LIMITED_METHOD_CONTRACT;
    m_pStubMD = PTR_MethodDesc(pStubMD);
}

void ILStubResolver::SetStubTargetMethodDesc(MethodDesc* pStubTargetMD)
{
    LIMITED_METHOD_CONTRACT;
    m_pStubTargetMD = PTR_MethodDesc(pStubTargetMD);
}


//---------------------------------------------------------------------------------------
//
void
ILStubResolver::SetStubTargetMethodSig(
    PCCOR_SIGNATURE pStubTargetMethodSig,
    DWORD           cbStubTargetSigLength)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(m_pCompileTimeState));
    }
    CONTRACTL_END;

    NewArrayHolder<BYTE> pNewSig = new BYTE[cbStubTargetSigLength];

    memcpyNoGCRefs((void *)pNewSig, pStubTargetMethodSig, cbStubTargetSigLength);

    m_pCompileTimeState->m_StubTargetMethodSig = SigPointer(pNewSig, cbStubTargetSigLength);
    pNewSig.SuppressRelease();
}

//---------------------------------------------------------------------------------------
//
MethodDesc *
ILStubResolver::GetStubTargetMethodDesc()
{
    LIMITED_METHOD_CONTRACT;
    return m_pStubTargetMD;
}

MethodDesc* ILStubResolver::GetStubMethodDesc()
{
    LIMITED_METHOD_CONTRACT;
    return m_pStubMD;
}

ILStubResolver::ILStubResolver() :
    m_pCompileTimeState(dac_cast<PTR_CompileTimeState>(ILNotYetGenerated)),
    m_pStubMD(dac_cast<PTR_MethodDesc>(nullptr)),
    m_pStubTargetMD(dac_cast<PTR_MethodDesc>(nullptr)),
    m_jitFlags(),
    m_loaderHeap(dac_cast<PTR_LoaderHeap>(nullptr))
{
    LIMITED_METHOD_CONTRACT;

}

void ILStubResolver::SetLoaderHeap(PTR_LoaderHeap pLoaderHeap)
{
    m_loaderHeap = pLoaderHeap;
}

static COR_ILMETHOD_DECODER CreateILHeader(size_t cbCode, UINT maxStack, BYTE* pNewILCodeBuffer, BYTE* pNewLocalSig, DWORD cbLocalSig)
{
    COR_ILMETHOD_DECODER ilHeader{};
    ilHeader.CodeSize = (DWORD)cbCode;
    ilHeader.MaxStack = maxStack;
    ilHeader.Code = pNewILCodeBuffer;
    ilHeader.LocalVarSig = pNewLocalSig;
    ilHeader.cbLocalVarSig = cbLocalSig;
    return ilHeader;
}

//---------------------------------------------------------------------------------------
//
COR_ILMETHOD_DECODER *
ILStubResolver::AllocGeneratedIL(
    size_t cbCode,
    DWORD  cbLocalSig,
    UINT   maxStack)
{
    STANDARD_VM_CONTRACT;

#if !defined(DACCESS_COMPILE)
    _ASSERTE(0 != cbCode);

    // Perform a single allocation for all needed memory
    AllocMemHolder<BYTE> allocMemory;
    NewArrayHolder<BYTE> newMemory;
    BYTE* memory;

    S_SIZE_T toAlloc = (S_SIZE_T(sizeof(CompileTimeState)) + S_SIZE_T(cbCode) + S_SIZE_T(cbLocalSig));
    _ASSERTE(!toAlloc.IsOverflow());

    if (UseLoaderHeap())
    {
        allocMemory = m_loaderHeap->AllocMem(toAlloc);
        memory = allocMemory;
    }
    else
    {
        newMemory = new BYTE[toAlloc.Value()];
        memory = newMemory;
    }

    // Using placement new
    CompileTimeState* pNewCompileTimeState = new (memory) CompileTimeState{};

    BYTE* pNewILCodeBuffer = ((BYTE*)pNewCompileTimeState) + sizeof(*pNewCompileTimeState);
    BYTE* pNewLocalSig = (0 == cbLocalSig)
        ? NULL
        : (pNewILCodeBuffer + cbCode);

    COR_ILMETHOD_DECODER* pILHeader = &pNewCompileTimeState->m_ILHeader;
    *pILHeader = CreateILHeader(cbCode, maxStack, pNewILCodeBuffer, pNewLocalSig, cbLocalSig);

    LPVOID pPrevCompileTimeState = InterlockedExchangeT(&m_pCompileTimeState, pNewCompileTimeState);
    CONSISTENCY_CHECK(ILNotYetGenerated == (UINT_PTR)pPrevCompileTimeState);
    (void*)pPrevCompileTimeState;

    allocMemory.SuppressRelease();
    newMemory.SuppressRelease();
    return pILHeader;

#else  // DACCESS_COMPILE
    DacNotImpl();
    return NULL;

#endif // DACCESS_COMPILE
} // ILStubResolver::AllocGeneratedIL

//---------------------------------------------------------------------------------------
//
COR_ILMETHOD_DECODER* ILStubResolver::GetILHeader()
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(m_pCompileTimeState));
    }
    CONTRACTL_END;

    return &m_pCompileTimeState->m_ILHeader;
}

COR_ILMETHOD_SECT_EH* ILStubResolver::AllocEHSect(size_t nClauses)
{
    STANDARD_VM_CONTRACT;

    if (nClauses == 0)
        return NULL;

    size_t cbSize = sizeof(COR_ILMETHOD_SECT_EH)
                    - sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT)
                    + (nClauses * sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
    m_pCompileTimeState->m_pEHSect = (COR_ILMETHOD_SECT_EH*) new BYTE[cbSize];
    CONSISTENCY_CHECK(NULL == m_pCompileTimeState->m_ILHeader.EH);
    m_pCompileTimeState->m_ILHeader.EH = m_pCompileTimeState->m_pEHSect;
    return m_pCompileTimeState->m_pEHSect;
}

bool ILStubResolver::UseLoaderHeap()
{
    return m_loaderHeap != dac_cast<PTR_LoaderHeap>(nullptr);
}

void ILStubResolver::FreeCompileTimeState()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if ((ILNotYetGenerated == dac_cast<TADDR>(m_pCompileTimeState)) ||
        (ILGeneratedAndFreed == dac_cast<TADDR>(m_pCompileTimeState)))
    {
        return;
    }

    if (!UseLoaderHeap())
    {
        ClearCompileTimeState(ILGeneratedAndFreed);
    }

}

//---------------------------------------------------------------------------------------
//
void
ILStubResolver::ClearCompileTimeState(CompileTimeStatePtrSpecialValues newState)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!UseLoaderHeap());
    }
    CONTRACTL_END;

    //
    // See allocations in AllocGeneratedIL, SetStubTargetMethodSig and AllocEHSect
    //

    delete[](BYTE*)m_pCompileTimeState->m_StubTargetMethodSig.GetPtr();
    delete[](BYTE*)m_pCompileTimeState->m_pEHSect;

    // The allocation being deleted here was allocated using placement new
    // from a bulk allocation so manually call the destructor.
    m_pCompileTimeState->~CompileTimeState();
    delete[](BYTE*)(void*)m_pCompileTimeState;

    InterlockedExchangeT(&m_pCompileTimeState, dac_cast<PTR_CompileTimeState>((TADDR)newState));
} // ILStubResolver::ClearCompileTimeState

//---------------------------------------------------------------------------------------
//
void
ILStubResolver::SetTokenLookupMap(
    TokenLookupMap * pMap)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(m_pCompileTimeState));
    }
    CONTRACTL_END;

    // run copy ctor
    new (&m_pCompileTimeState->m_tokenLookupMap) TokenLookupMap(pMap);
}

bool ILStubResolver::IsCompiled()
{
    LIMITED_METHOD_CONTRACT;
    return (dac_cast<TADDR>(m_pCompileTimeState) == ILGeneratedAndFreed);
}

bool ILStubResolver::IsILGenerated()
{
    return (dac_cast<TADDR>(m_pCompileTimeState) != ILNotYetGenerated);
}

void ILStubResolver::SetJitFlags(CORJIT_FLAGS jitFlags)
{
    LIMITED_METHOD_CONTRACT;
    m_jitFlags = jitFlags;
}

// static
void ILStubResolver::StubGenFailed(ILStubResolver* pResolver)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if ((ILNotYetGenerated == dac_cast<TADDR>(pResolver->m_pCompileTimeState)) ||
        (ILGeneratedAndFreed == dac_cast<TADDR>(pResolver->m_pCompileTimeState)))
    {
        return;
    }

    pResolver->ClearCompileTimeState(ILNotYetGenerated);
}
