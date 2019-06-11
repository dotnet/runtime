// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

LPCUTF8 ILStubResolver::GetStubMethodName()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    switch (m_type)
    {
        case CLRToNativeInteropStub: return "IL_STUB_PInvoke";
        case CLRToCOMInteropStub:    return "IL_STUB_CLRtoCOM";
        case CLRToWinRTInteropStub:  return "IL_STUB_CLRtoWinRT";
        case NativeToCLRInteropStub: return "IL_STUB_ReversePInvoke";
        case COMToCLRInteropStub:    return "IL_STUB_COMtoCLR";
        case WinRTToCLRInteropStub:  return "IL_STUB_WinRTtoCLR";
#ifdef FEATURE_ARRAYSTUB_AS_IL
        case ArrayOpStub:            return "IL_STUB_Array";
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
        case MulticastDelegateStub:  return "IL_STUB_MulticastDelegate_Invoke";
#endif
#ifdef FEATURE_STUBS_AS_IL
        case UnboxingILStub:         return "IL_STUB_UnboxingStub";
        case InstantiatingStub:      return "IL_STUB_InstantiatingStub";
        case SecureDelegateStub:     return "IL_STUB_SecureDelegate_Invoke";
#endif
        default:
            UNREACHABLE_MSG("Unknown stub type");
    }
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

void ILStubResolver::ResolveToken(mdToken token, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD)
{        
    STANDARD_VM_CONTRACT;

    *pTH = NULL;
    *ppMD = NULL;
    *ppFD = NULL;

    switch (TypeFromToken(token))
    {
    case mdtMethodDef:
        {
            MethodDesc* pMD = m_pCompileTimeState->m_tokenLookupMap.LookupMethodDef(token);
            _ASSERTE(pMD);
            *ppMD = pMD;
            *pTH = TypeHandle(pMD->GetMethodTable());
        }
        break;

    case mdtTypeDef:
        {
            TypeHandle typeHnd = m_pCompileTimeState->m_tokenLookupMap.LookupTypeDef(token);
            _ASSERTE(!typeHnd.IsNull());
            *pTH = typeHnd;
        }
        break;

    case mdtFieldDef:
        {
            FieldDesc* pFD = m_pCompileTimeState->m_tokenLookupMap.LookupFieldDef(token);
            _ASSERTE(pFD);
            *ppFD = pFD;
            *pTH = TypeHandle(pFD->GetEnclosingMethodTable());
        }
        break;

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
    CONSISTENCY_CHECK_MSG(token == TOKEN_ILSTUB_TARGET_SIG, "IL stubs do not support any other signature tokens!");

    return m_pCompileTimeState->m_StubTargetMethodSig;
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

bool ILStubResolver::IsNativeToCLRInteropStub()
{
    return (m_type == NativeToCLRInteropStub);
}

bool ILStubResolver::IsCLRToNativeInteropStub()
{
    return (m_type == CLRToNativeInteropStub);
}

void ILStubResolver::SetStubType(ILStubType stubType)
{
    LIMITED_METHOD_CONTRACT;
    m_type = stubType;
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
    m_type(Unassigned),
    m_jitFlags()
{
    LIMITED_METHOD_CONTRACT;
    
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

    NewArrayHolder<BYTE>             pNewILCodeBuffer        = NULL;
    NewArrayHolder<BYTE>             pNewLocalSig            = NULL;
    NewArrayHolder<CompileTimeState> pNewCompileTimeState    = NULL;

    pNewCompileTimeState = (CompileTimeState *)new BYTE[sizeof(CompileTimeState)];
    memset(pNewCompileTimeState, 0, sizeof(CompileTimeState));

    pNewILCodeBuffer = new BYTE[cbCode];

    if (0 != cbLocalSig)
    {
        pNewLocalSig = new BYTE[cbLocalSig];
    }

    COR_ILMETHOD_DECODER* pILHeader = &pNewCompileTimeState->m_ILHeader;    

    pILHeader->Flags         = 0;
    pILHeader->CodeSize      = (DWORD)cbCode;
    pILHeader->MaxStack      = maxStack;
    pILHeader->EH            = 0;
    pILHeader->Sect          = 0;
    pILHeader->Code          = pNewILCodeBuffer;
    pILHeader->LocalVarSig   = pNewLocalSig;
    pILHeader->cbLocalVarSig = cbLocalSig;

#ifdef _DEBUG
    LPVOID pPrevCompileTimeState =
#endif // _DEBUG
    FastInterlockExchangePointer(&m_pCompileTimeState, pNewCompileTimeState.GetValue());
    CONSISTENCY_CHECK(ILNotYetGenerated == (UINT_PTR)pPrevCompileTimeState);

    pNewLocalSig.SuppressRelease();
    pNewILCodeBuffer.SuppressRelease();
    pNewCompileTimeState.SuppressRelease();

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

    if (nClauses >= 1)
    {
        size_t cbSize = sizeof(COR_ILMETHOD_SECT_EH)
                        - sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT)
                        + (nClauses * sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        m_pCompileTimeState->m_pEHSect = (COR_ILMETHOD_SECT_EH*) new BYTE[cbSize];
        CONSISTENCY_CHECK(NULL == m_pCompileTimeState->m_ILHeader.EH);
        m_pCompileTimeState->m_ILHeader.EH = m_pCompileTimeState->m_pEHSect;
        return m_pCompileTimeState->m_pEHSect;
    }
    else
    {
        return NULL;
    }
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

    ClearCompileTimeState(ILGeneratedAndFreed);
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
    }
    CONTRACTL_END;

    //
    // See allocations in AllocGeneratedIL and SetStubTargetMethodSig
    //
    
    COR_ILMETHOD_DECODER * pILHeader = &m_pCompileTimeState->m_ILHeader;    

    CONSISTENCY_CHECK(NULL != pILHeader->Code);
    delete[] pILHeader->Code;

    if (NULL != pILHeader->LocalVarSig)
    {
        delete[] pILHeader->LocalVarSig;
    }

    if (!m_pCompileTimeState->m_StubTargetMethodSig.IsNull())
    {
        delete[] m_pCompileTimeState->m_StubTargetMethodSig.GetPtr();
    }

    if (NULL != m_pCompileTimeState->m_pEHSect)
    {
        delete[] m_pCompileTimeState->m_pEHSect;
    }

    delete m_pCompileTimeState;

    FastInterlockExchangePointer(&m_pCompileTimeState, dac_cast<PTR_CompileTimeState>((TADDR)newState));
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

CORJIT_FLAGS ILStubResolver::GetJitFlags()
{
    LIMITED_METHOD_CONTRACT;
    return m_jitFlags;
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
