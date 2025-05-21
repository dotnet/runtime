// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: ILStubResolver.h
//

//


#ifndef __ILSTUBRESOLVER_H__
#define __ILSTUBRESOLVER_H__

#include "stubgen.h"
class ILStubResolver : public DynamicResolver
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
    bool RequiresAccessCheck();
    CORJIT_FLAGS GetJitFlags();

    BYTE* GetCodeInfo(unsigned* pCodeSize, unsigned* pStackSize, CorInfoOptions* pOptions, unsigned* pEHSize);
    SigPointer GetLocalSig();

    OBJECTHANDLE ConstructStringLiteral(mdToken metaTok);
    BOOL IsValidStringRef(mdToken metaTok);
    STRINGREF GetStringLiteral(mdToken metaTok);
    void ResolveToken(mdToken token, ResolvedToken* resolvedToken);
    SigPointer ResolveSignature(mdToken token);
    SigPointer ResolveSignatureForVarArg(mdToken token);
    void GetEHInfo(unsigned EHnumber, CORINFO_EH_CLAUSE* clause);

    static LPCUTF8 GetStubClassName(MethodDesc* pMD);

    MethodDesc* GetDynamicMethod() { LIMITED_METHOD_CONTRACT; return m_pStubMD; }

    // -----------------------------------
    // ILStubResolver-specific methods
    // -----------------------------------
    ILStubResolver();

    bool IsCompiled();
    bool IsILGenerated();

    MethodDesc* GetStubMethodDesc();
    MethodDesc* GetStubTargetMethodDesc();
    COR_ILMETHOD_DECODER* GetILHeader();

#ifndef DACCESS_COMPILE
    // This is only set for StructMarshal interop stubs.
    // See callsites for more details.
    void SetLoaderHeap(PTR_LoaderHeap pLoaderHeap);
    void SetTokenLookupMap(TokenLookupMap* pMap);
    void SetJitFlags(CORJIT_FLAGS jitFlags);
    void SetStubMethodDesc(MethodDesc* pStubMD);
    void SetStubTargetMethodDesc(MethodDesc* pStubTargetMD);
    void SetStubTargetMethodSig(PCCOR_SIGNATURE pStubTargetMethodSig, DWORD cbStubTargetSigLength);

    COR_ILMETHOD_DECODER* AllocGeneratedIL(size_t cbCode, DWORD cbLocalSig, UINT maxStack);
    COR_ILMETHOD_SECT_EH* AllocEHSect(size_t nClauses);

    COR_ILMETHOD_DECODER* FinalizeILStub(ILStubLinker* sl);
#endif // !DACCESS_COMPILE

    static void StubGenFailed(ILStubResolver* pResolver);

protected:

    enum CompileTimeStatePtrSpecialValues
    {
        ILNotYetGenerated   = 0,
        ILGeneratedAndFreed = 1,
    };

    void ClearCompileTimeState(CompileTimeStatePtrSpecialValues newState);
    bool UseLoaderHeap();

    //
    // This stuff is only needed during JIT
    //
    struct CompileTimeState
    {
        CompileTimeState() = default;
        ~CompileTimeState() = default;

        COR_ILMETHOD_DECODER   m_ILHeader;
        COR_ILMETHOD_SECT_EH * m_pEHSect;
        SigPointer             m_StubTargetMethodSig;
        TokenLookupMap         m_tokenLookupMap;
    };
    typedef DPTR(struct CompileTimeState) PTR_CompileTimeState;

    PTR_CompileTimeState    m_pCompileTimeState;

    PTR_MethodDesc          m_pStubMD;
    PTR_MethodDesc          m_pStubTargetMD;
    CORJIT_FLAGS            m_jitFlags;
    PTR_LoaderHeap          m_loaderHeap;
};

typedef Holder<ILStubResolver*, DoNothing<ILStubResolver*>, ILStubResolver::StubGenFailed, 0> ILStubGenHolder;

#endif // __ILSTUBRESOLVER_H__
