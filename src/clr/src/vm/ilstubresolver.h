// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: ILStubResolver.h
// 

// 


#ifndef __ILSTUBRESOLVER_H__
#define __ILSTUBRESOLVER_H__

#include "stubgen.h"
class ILStubResolver : DynamicResolver
{
    friend class ILStubCache;
    friend class ILStubLinker;

public:

    // -----------------------------------
    // DynamicResolver interface methods
    // -----------------------------------

    void FreeCompileTimeState();
    void GetJitContext(SecurityControlFlags* pSecurityControlFlags,
                       TypeHandle* pTypeOwner);
    ChunkAllocator* GetJitMetaHeap();

    BYTE* GetCodeInfo(unsigned* pCodeSize, unsigned* pStackSize, CorInfoOptions* pOptions, unsigned* pEHSize);
    SigPointer GetLocalSig();
    
    OBJECTHANDLE ConstructStringLiteral(mdToken metaTok);
    BOOL IsValidStringRef(mdToken metaTok);
    void ResolveToken(mdToken token, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD);
    SigPointer ResolveSignature(mdToken token);
    SigPointer ResolveSignatureForVarArg(mdToken token);
    void GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause);

    static LPCUTF8 GetStubClassName(MethodDesc* pMD);
    LPCUTF8 GetStubMethodName();

    MethodDesc* GetDynamicMethod() { LIMITED_METHOD_CONTRACT; return m_pStubMD; }
    
    // -----------------------------------
    // ILStubResolver-specific methods
    // -----------------------------------
    bool IsNativeToCLRInteropStub();
    bool IsCLRToNativeInteropStub();
    MethodDesc* GetStubMethodDesc();
    MethodDesc* GetStubTargetMethodDesc();
    void SetStubTargetMethodDesc(MethodDesc* pStubTargetMD);
    void SetStubTargetMethodSig(PCCOR_SIGNATURE pStubTargetMethodSig, DWORD cbStubTargetSigLength);
    void SetStubMethodDesc(MethodDesc* pStubMD);

    COR_ILMETHOD_DECODER * AllocGeneratedIL(size_t cbCode, DWORD cbLocalSig, UINT maxStack);
    COR_ILMETHOD_DECODER * GetILHeader();
    COR_ILMETHOD_SECT_EH* AllocEHSect(size_t nClauses);

    bool IsCompiled();
    bool IsILGenerated();

    ILStubResolver();

    void SetTokenLookupMap(TokenLookupMap* pMap);

    void SetJitFlags(CORJIT_FLAGS jitFlags);
    CORJIT_FLAGS GetJitFlags();

    static void StubGenFailed(ILStubResolver* pResolver);

protected:    
    enum ILStubType
    {
        Unassigned = 0,
        CLRToNativeInteropStub,
        CLRToCOMInteropStub,
        CLRToWinRTInteropStub,
        NativeToCLRInteropStub,
        COMToCLRInteropStub,
        WinRTToCLRInteropStub,
#ifdef FEATURE_ARRAYSTUB_AS_IL 
        ArrayOpStub,
#endif
#ifdef FEATURE_MULTICASTSTUB_AS_IL
        MulticastDelegateStub,
#endif
#ifdef FEATURE_STUBS_AS_IL
        SecureDelegateStub,
        UnboxingILStub,
        InstantiatingStub,
#endif
    };

    enum CompileTimeStatePtrSpecialValues
    {
        ILNotYetGenerated   = NULL,
        ILGeneratedAndFreed = 1,
    };

    void ClearCompileTimeState(CompileTimeStatePtrSpecialValues newState);
    void SetStubType(ILStubType stubType);

    //
    // This stuff is only needed during JIT
    //
    struct CompileTimeState
    {
        COR_ILMETHOD_DECODER   m_ILHeader;
        COR_ILMETHOD_SECT_EH * m_pEHSect;
        SigPointer             m_StubTargetMethodSig;
        TokenLookupMap         m_tokenLookupMap;
    };
    typedef DPTR(struct CompileTimeState) PTR_CompileTimeState;

    PTR_CompileTimeState    m_pCompileTimeState;

    PTR_MethodDesc          m_pStubMD;
    PTR_MethodDesc          m_pStubTargetMD;
    ILStubType              m_type;
    CORJIT_FLAGS            m_jitFlags;
};

typedef Holder<ILStubResolver*, DoNothing<ILStubResolver*>, ILStubResolver::StubGenFailed, NULL> ILStubGenHolder;


#endif // __ILSTUBRESOLVER_H__
