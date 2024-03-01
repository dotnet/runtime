// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: StubGen.h
//

//


#ifndef __STUBGEN_H__
#define __STUBGEN_H__

#include "stublink.h"

struct ILStubEHClause;
class ILStubLinker;

#ifndef DACCESS_COMPILE
struct StructMarshalStubs
{
    static const DWORD MANAGED_STRUCT_ARGIDX = 0;
    static const DWORD NATIVE_STRUCT_ARGIDX = 1;
    static const DWORD OPERATION_ARGIDX = 2;
    static const DWORD CLEANUP_WORK_LIST_ARGIDX = 3;

    enum MarshalOperation
    {
        Marshal,
        Unmarshal,
        Cleanup
    };
};

struct LocalDesc
{
    const static size_t MAX_LOCALDESC_ELEMENTS = 8;

    BYTE    ElementType[MAX_LOCALDESC_ELEMENTS];
    size_t  cbType;
    TypeHandle InternalToken;  // only valid with ELEMENT_TYPE_INTERNAL

    // used only for E_T_FNPTR and E_T_ARRAY
    PCCOR_SIGNATURE pSig;
    union
    {
        Module*         pSigModule;
        size_t          cbArrayBoundsInfo;
        BOOL            bIsCopyConstructed; // used for E_T_PTR
    };

    LocalDesc()
    {
    }

    inline LocalDesc(CorElementType elemType)
    {
        ElementType[0]     = static_cast<BYTE>(elemType);
        cbType             = 1;
        bIsCopyConstructed = FALSE;
    }

    inline LocalDesc(TypeHandle thType)
    {
        ElementType[0]     = ELEMENT_TYPE_INTERNAL;
        cbType             = 1;
        InternalToken      = thType;
        bIsCopyConstructed = FALSE;
    }

    inline LocalDesc(MethodTable *pMT)
    {
        WRAPPER_NO_CONTRACT;
        ElementType[0]     = ELEMENT_TYPE_INTERNAL;
        cbType             = 1;
        InternalToken      = TypeHandle(pMT);
        bIsCopyConstructed = FALSE;
    }

    void MakeByRef()
    {
        LIMITED_METHOD_CONTRACT;
        ChangeType(ELEMENT_TYPE_BYREF);
    }

    void MakePinned()
    {
        LIMITED_METHOD_CONTRACT;
        ChangeType(ELEMENT_TYPE_PINNED);
    }

    void MakeArray()
    {
        LIMITED_METHOD_CONTRACT;
        ChangeType(ELEMENT_TYPE_SZARRAY);
    }

    // makes the LocalDesc semantically equivalent to ET_TYPE_CMOD_REQD<IsCopyConstructed>/ET_TYPE_CMOD_REQD<NeedsCopyConstructorModifier>
    void MakeCopyConstructedPointer()
    {
        LIMITED_METHOD_CONTRACT;
        MakePointer();
        bIsCopyConstructed = TRUE;
    }

    void MakePointer()
    {
        LIMITED_METHOD_CONTRACT;
        ChangeType(ELEMENT_TYPE_PTR);
    }

    void ChangeType(CorElementType elemType)
    {
        LIMITED_METHOD_CONTRACT;
        PREFIX_ASSUME((MAX_LOCALDESC_ELEMENTS-1) >= cbType);

        for (size_t i = cbType; i >= 1; i--)
        {
            ElementType[i]  = ElementType[i-1];
        }

        ElementType[0]  = static_cast<BYTE>(elemType);
        cbType          += 1;
    }

    bool IsValueClass()
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        bool lastElementTypeIsValueType = false;

        if (ElementType[cbType - 1] == ELEMENT_TYPE_VALUETYPE)
        {
            lastElementTypeIsValueType = true;
        }
        else if ((ElementType[cbType - 1] == ELEMENT_TYPE_INTERNAL) &&
                    (InternalToken.IsNativeValueType() ||
                     InternalToken.GetMethodTable()->IsValueType()))
        {
            lastElementTypeIsValueType = true;
        }

        if (!lastElementTypeIsValueType)
        {
             return false;
        }

        // verify that the prefix element types don't make the type a non-value type
        // this only works on LocalDescs with the prefixes exposed in the Add* methods above.
        for (size_t i = 0; i < cbType - 1; i++)
        {
            if (ElementType[i] == ELEMENT_TYPE_BYREF
                || ElementType[i] == ELEMENT_TYPE_SZARRAY
                || ElementType[i] == ELEMENT_TYPE_PTR)
            {
                return false;
            }
        }

        return true;
    }
};

class StubSigBuilder
{
public:
    StubSigBuilder();

    DWORD   Append(LocalDesc* pLoc);

protected:
    CQuickBytes     m_qbSigBuffer;
    uint32_t        m_nItems;
    BYTE*           m_pbSigCursor;
    size_t          m_cbSig;

    enum Constants { INITIAL_BUFFER_SIZE  = 256 };

    void EnsureEnoughQuickBytes(size_t cbToAppend);
};

//---------------------------------------------------------------------------------------
//
class LocalSigBuilder : protected StubSigBuilder
{
public:
    DWORD NewLocal(LocalDesc * pLoc)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_ANY;
            PRECONDITION(CheckPointer(pLoc));
        }
        CONTRACTL_END;

        return Append(pLoc);
    }

    DWORD GetSigSize();
    DWORD GetSig(BYTE * pbSig, DWORD cbBuffer);

};  // class LocalSigBuilder

//---------------------------------------------------------------------------------------
//
class FunctionSigBuilder : protected StubSigBuilder
{
public:
    FunctionSigBuilder();

    DWORD NewArg(LocalDesc * pArg)
    {
        WRAPPER_NO_CONTRACT;

        return Append(pArg);
    }

    DWORD GetNumArgs()
    {
        LIMITED_METHOD_CONTRACT;
        return m_nItems;
    }

    void SetCallingConv(CorCallingConvention callingConv)
    {
        LIMITED_METHOD_CONTRACT;
        m_callingConv = callingConv;
    }

    void AddCallConvModOpt(mdToken token);

    CorCallingConvention GetCallingConv()
    {
        LIMITED_METHOD_CONTRACT;
        return m_callingConv;
    }

    void SetSig(PCCOR_SIGNATURE pSig, DWORD cSig);

    DWORD GetSigSize();
    DWORD GetSig(BYTE * pbSig, DWORD cbBuffer);

    void SetReturnType(LocalDesc* pLoc);

    CorElementType GetReturnElementType()
    {
        LIMITED_METHOD_CONTRACT;

        CONSISTENCY_CHECK(m_qbReturnSig.Size() > 0);
        return *(CorElementType *)m_qbReturnSig.Ptr();
    }

    PCCOR_SIGNATURE GetReturnSig()
    {
        LIMITED_METHOD_CONTRACT;

        CONSISTENCY_CHECK(m_qbReturnSig.Size() > 0);
        return (PCCOR_SIGNATURE)m_qbReturnSig.Ptr();
    }

protected:
    CorCallingConvention m_callingConv;
    CQuickBytes          m_qbReturnSig;
    CQuickBytes          m_qbCallConvModOpts;
};  // class FunctionSigBuilder

#endif // DACCESS_COMPILE

#ifdef _DEBUG
// exercise the resize code
#define TOKEN_LOOKUP_MAP_SIZE  (8*sizeof(void*))
#else // _DEBUG
#define TOKEN_LOOKUP_MAP_SIZE  (64*sizeof(void*))
#endif // _DEBUG

//---------------------------------------------------------------------------------------
//
class TokenLookupMap
{
public:
    TokenLookupMap()
    {
        STANDARD_VM_CONTRACT;

        m_qbEntries.AllocThrows(TOKEN_LOOKUP_MAP_SIZE);
        m_nextAvailableRid = 0;
    }

    // copy ctor
    TokenLookupMap(TokenLookupMap* pSrc)
    {
        STANDARD_VM_CONTRACT;

        m_nextAvailableRid = pSrc->m_nextAvailableRid;
        size_t size = pSrc->m_qbEntries.Size();
        m_qbEntries.AllocThrows(size);
        memcpy(m_qbEntries.Ptr(), pSrc->m_qbEntries.Ptr(), size);

        m_signatures.Preallocate(pSrc->m_signatures.GetCount());
        for (COUNT_T i = 0; i < pSrc->m_signatures.GetCount(); i++)
        {
            const CQuickBytesSpecifySize<16>& src = pSrc->m_signatures[i];
            auto dst = m_signatures.Append();
            dst->AllocThrows(src.Size());
            memcpy(dst->Ptr(), src.Ptr(), src.Size());
        }

        m_memberRefs.Set(pSrc->m_memberRefs);
        m_methodSpecs.Set(pSrc->m_methodSpecs);
    }

    TypeHandle LookupTypeDef(mdToken token)
    {
        WRAPPER_NO_CONTRACT;
        return LookupTokenWorker<mdtTypeDef, MethodTable*>(token);
    }
    MethodDesc* LookupMethodDef(mdToken token)
    {
        WRAPPER_NO_CONTRACT;
        return LookupTokenWorker<mdtMethodDef, MethodDesc*>(token);
    }
    FieldDesc* LookupFieldDef(mdToken token)
    {
        WRAPPER_NO_CONTRACT;
        return LookupTokenWorker<mdtFieldDef, FieldDesc*>(token);
    }

    struct MemberRefEntry final
    {
        CorTokenType Type;
        mdToken ClassSignatureToken;
        union
        {
            FieldDesc* Field;
            MethodDesc* Method;
        } Entry;
    };
    MemberRefEntry LookupMemberRef(mdToken token)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(RidFromToken(token) - 1 < m_memberRefs.GetCount());
            PRECONDITION(RidFromToken(token) != 0);
            PRECONDITION(TypeFromToken(token) == mdtMemberRef);
        }
        CONTRACTL_END;

        return m_memberRefs[static_cast<COUNT_T>(RidFromToken(token) - 1)];
    }

    struct MethodSpecEntry final
    {
        mdToken ClassSignatureToken;
        mdToken MethodSignatureToken;
        MethodDesc* Method;
    };
    MethodSpecEntry LookupMethodSpec(mdToken token)
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(RidFromToken(token) - 1 < m_methodSpecs.GetCount());
            PRECONDITION(RidFromToken(token) != 0);
            PRECONDITION(TypeFromToken(token) == mdtMethodSpec);
        }
        CONTRACTL_END;

        return m_methodSpecs[static_cast<COUNT_T>(RidFromToken(token) - 1)];
    }

    SigPointer LookupSig(mdToken token)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(RidFromToken(token)-1 < m_signatures.GetCount());
            PRECONDITION(RidFromToken(token) != 0);
            PRECONDITION(TypeFromToken(token) == mdtSignature);
        }
        CONTRACTL_END;

        CQuickBytesSpecifySize<16>& sigData = m_signatures[static_cast<COUNT_T>(RidFromToken(token)-1)];
        PCCOR_SIGNATURE pSig = (PCCOR_SIGNATURE)sigData.Ptr();
        DWORD cbSig = static_cast<DWORD>(sigData.Size());
        return SigPointer(pSig, cbSig);
    }

    mdToken GetToken(TypeHandle pMT)
    {
        WRAPPER_NO_CONTRACT;
        return GetTokenWorker<mdtTypeDef, TypeHandle>(pMT);
    }
    mdToken GetToken(MethodDesc* pMD)
    {
        WRAPPER_NO_CONTRACT;
        return GetTokenWorker<mdtMethodDef, MethodDesc*>(pMD);
    }
    mdToken GetToken(MethodDesc* pMD, mdToken typeSignature)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(pMD != NULL);
        }
        CONTRACTL_END;

        MemberRefEntry* entry;
        mdToken token = GetMemberRefWorker(&entry);
        entry->Type = mdtMethodDef;
        entry->ClassSignatureToken = typeSignature;
        entry->Entry.Method = pMD;
        return token;
    }
    mdToken GetToken(MethodDesc* pMD, mdToken typeSignature, mdToken methodSignature)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(pMD != NULL);
            PRECONDITION(typeSignature != mdTokenNil);
            PRECONDITION(methodSignature != mdTokenNil);
        }
        CONTRACTL_END;

        MethodSpecEntry* entry;
        mdToken token = GetMethodSpecWorker(&entry);
        entry->ClassSignatureToken = typeSignature;
        entry->MethodSignatureToken = methodSignature;
        entry->Method = pMD;
        return token;
    }
    mdToken GetToken(FieldDesc* pFieldDesc)
    {
        WRAPPER_NO_CONTRACT;
        return GetTokenWorker<mdtFieldDef, FieldDesc*>(pFieldDesc);
    }
    mdToken GetToken(FieldDesc* pFieldDesc, mdToken typeSignature)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(pFieldDesc != NULL);
        }
        CONTRACTL_END;

        MemberRefEntry* entry;
        mdToken token = GetMemberRefWorker(&entry);
        entry->Type = mdtFieldDef;
        entry->ClassSignatureToken = typeSignature;
        entry->Entry.Field = pFieldDesc;
        return token;
    }

    mdToken GetSigToken(PCCOR_SIGNATURE pSig, DWORD cbSig)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(pSig != NULL);
        }
        CONTRACTL_END;

        mdToken token = TokenFromRid(m_signatures.GetCount(), mdtSignature)+1;
        CQuickBytesSpecifySize<16>& sigData = *m_signatures.Append();
        sigData.AllocThrows(cbSig);
        memcpy(sigData.Ptr(), pSig, cbSig);
        return token;
    }

protected:
    mdToken GetMemberRefWorker(MemberRefEntry** entry)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(entry != NULL);
        }
        CONTRACTL_END;

        mdToken token = TokenFromRid(m_memberRefs.GetCount(), mdtMemberRef) + 1;
        *entry = &*m_memberRefs.Append(); // Dereference the iterator and then take the address
        return token;
    }

    mdToken GetMethodSpecWorker(MethodSpecEntry** entry)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(entry != NULL);
        }
        CONTRACTL_END;

        mdToken token = TokenFromRid(m_methodSpecs.GetCount(), mdtMethodSpec) + 1;
        *entry = &*m_methodSpecs.Append(); // Dereference the iterator and then take the address
        return token;
    }

    template<mdToken TokenType, typename HandleType>
    HandleType LookupTokenWorker(mdToken token)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(RidFromToken(token)-1 < m_nextAvailableRid);
            PRECONDITION(RidFromToken(token) != 0);
            PRECONDITION(TypeFromToken(token) == TokenType);
        }
        CONTRACTL_END;

        return ((HandleType*)m_qbEntries.Ptr())[RidFromToken(token)-1];
    }

    template<mdToken TokenType, typename HandleType>
    mdToken GetTokenWorker(HandleType handle)
    {
        CONTRACTL
        {
            THROWS;
            MODE_ANY;
            GC_NOTRIGGER;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        if (m_qbEntries.Size() <= (sizeof(handle) * m_nextAvailableRid))
        {
            m_qbEntries.ReSizeThrows(2 * m_qbEntries.Size());
        }

        mdToken token = TokenFromRid(m_nextAvailableRid++, TokenType)+1;

        ((HandleType*)m_qbEntries.Ptr())[RidFromToken(token)-1] = handle;

        return token;
    }

    uint32_t                                       m_nextAvailableRid;
    CQuickBytesSpecifySize<TOKEN_LOOKUP_MAP_SIZE>  m_qbEntries;
    SArray<CQuickBytesSpecifySize<16>, FALSE>      m_signatures;
    SArray<MemberRefEntry, FALSE>                  m_memberRefs;
    SArray<MethodSpecEntry, FALSE>                 m_methodSpecs;
};

class ILCodeLabel;
class ILCodeStream;

#ifndef DACCESS_COMPILE
struct ILStubEHClause
{
    enum Kind { kNone, kTypedCatch, kFinally };

    DWORD kind;
    DWORD dwTryBeginOffset;
    DWORD cbTryLength;
    DWORD dwHandlerBeginOffset;
    DWORD cbHandlerLength;
    DWORD dwTypeToken;
};

struct ILStubEHClauseBuilder
{
    DWORD kind;
    ILCodeLabel* tryBeginLabel;
    ILCodeLabel* tryEndLabel;
    ILCodeLabel* handlerBeginLabel;
    ILCodeLabel* handlerEndLabel;
    DWORD typeToken;
};


enum ILStubLinkerFlags
{
    ILSTUB_LINKER_FLAG_NONE                 = 0x00,
    ILSTUB_LINKER_FLAG_TARGET_HAS_THIS      = 0x01,
    ILSTUB_LINKER_FLAG_STUB_HAS_THIS        = 0x02,
    ILSTUB_LINKER_FLAG_NDIRECT              = 0x04,
    ILSTUB_LINKER_FLAG_REVERSE              = 0x08,
    ILSTUB_LINKER_FLAG_SUPPRESSGCTRANSITION = 0x10,
};

//---------------------------------------------------------------------------------------
//
class ILStubLinker
{
    friend class ILCodeLabel;
    friend class ILCodeStream;

public:

    ILStubLinker(Module* pModule, const Signature &signature, SigTypeContext *pTypeContext, MethodDesc *pMD, ILStubLinkerFlags flags);
    ~ILStubLinker();

    void GenerateCode(BYTE* pbBuffer, size_t cbBufferSize);
    void ClearCode();

    void SetStubMethodDesc(MethodDesc *pMD);
protected:

    void DeleteCodeLabels();
    void DeleteCodeStreams();

    struct ILInstruction
    {
        UINT16      uInstruction;
        INT16       iStackDelta;
        UINT_PTR    uArg;
    };

    static void PatchInstructionArgument(ILCodeLabel* pLabel, UINT_PTR uNewArg
        DEBUG_ARG(UINT16 uExpectedInstruction));

#ifdef _DEBUG
    bool IsInCodeStreamList(ILCodeStream* pcs);
#endif // _DEBUG

public:

    void    SetHasThis (bool fHasThis);
    bool    HasThis () { LIMITED_METHOD_CONTRACT; return m_fHasThis; }

    DWORD GetLocalSigSize();
    DWORD GetLocalSig(BYTE * pbLocalSig, DWORD cbBuffer);

    DWORD GetStubTargetMethodSigSize();
    DWORD GetStubTargetMethodSig(BYTE * pbLocalSig, DWORD cbBuffer);

    void SetStubTargetMethodSig(PCCOR_SIGNATURE pSig, DWORD cSig);

    void GetStubTargetReturnType(LocalDesc * pLoc);
    void GetStubTargetReturnType(LocalDesc * pLoc, Module * pModule);

    void GetStubArgType(LocalDesc * pLoc);
    void GetStubArgType(LocalDesc * pLoc, Module * pModule);
    void GetStubReturnType(LocalDesc * pLoc);
    void GetStubReturnType(LocalDesc * pLoc, Module * pModule);
    CorCallingConvention GetStubTargetCallingConv();

    CorElementType GetStubTargetReturnElementType() { WRAPPER_NO_CONTRACT; return m_nativeFnSigBuilder.GetReturnElementType(); }

    static void GetManagedTypeHelper(LocalDesc* pLoc, Module* pModule, PCCOR_SIGNATURE pSig, SigTypeContext *pTypeContext);

    BOOL StubHasVoidReturnType();

    Stub *Link(LoaderHeap *pHeap, UINT *pcbSize /* = NULL*/, BOOL fMC);

    size_t  Link(UINT* puMaxStack);

    size_t GetNumEHClauses();
    // Write out EH clauses. Number of items written out will be GetNumEHCLauses().
    void WriteEHClauses(COR_ILMETHOD_SECT_EH* sect);

    TokenLookupMap* GetTokenLookupMap() { LIMITED_METHOD_CONTRACT; return &m_tokenMap; }

    enum CodeStreamType
    {
        kSetup,
        kMarshal,
        kDispatch,
        kReturnUnmarshal,
        kUnmarshal,
        kExceptionCleanup,
        kCleanup,
        kExceptionHandler,
    };

    ILCodeStream* NewCodeStream(CodeStreamType codeStreamType);

    MethodDesc *GetTargetMD() { LIMITED_METHOD_CONTRACT; return m_pMD; }
    Signature GetStubSignature() { LIMITED_METHOD_CONTRACT; return m_stubSig; }

    void ClearCodeStreams();

    void LogILStub(CORJIT_FLAGS jitFlags, SString *pDumpILStubCode = NULL);
protected:
    void DumpIL_FormatToken(mdToken token, SString &strTokenFormatting);
    void LogILStubWorker(ILInstruction* pInstrBuffer, UINT numInstr, size_t* pcbCode, INT* piCurStack, SString *pDumpILStubCode = NULL);
    void LogILInstruction(size_t curOffset, bool isLabeled, INT iCurStack, ILInstruction* pInstruction, SString *pDumpILStubCode = NULL);

private:
    ILCodeStream*       m_pCodeStreamList;

    TokenLookupMap      m_tokenMap;
    LocalSigBuilder     m_localSigBuilder;
    FunctionSigBuilder  m_nativeFnSigBuilder;
    BYTE                m_rgbBuffer[sizeof(COR_ILMETHOD_DECODER)];

    Signature       m_stubSig;      // managed sig of stub
    SigTypeContext* m_pTypeContext; // type context for m_stubSig

    SigPointer      m_managedSigPtr;
    void*           m_pCode;
    Module*         m_pStubSigModule;
    ILCodeLabel*    m_pLabelList;

    bool    FirstPassLink(ILInstruction* pInstrBuffer, UINT numInstr, size_t* pcbCode, INT* piCurStack, UINT* puMaxStack);
    void    SecondPassLink(ILInstruction* pInstrBuffer, UINT numInstr, size_t* pCurCodeOffset);

    BYTE*   GenerateCodeWorker(BYTE* pbBuffer, ILInstruction* pInstrBuffer, UINT numInstr, size_t* pcbCode);

    static ILCodeStream* FindLastCodeStream(ILCodeStream* pList);

protected:
    //
    // the public entrypoints for these methods are in ILCodeStream
    //
    ILCodeLabel* NewCodeLabel();
    int GetToken(MethodDesc* pMD);
    int GetToken(MethodDesc* pMD, mdToken typeSignature);
    int GetToken(MethodDesc* pMD, mdToken typeSignature, mdToken methodSignature);
    int GetToken(MethodTable* pMT);
    int GetToken(TypeHandle th);
    int GetToken(FieldDesc* pFD);
    int GetToken(FieldDesc* pFD, mdToken typeSignature);
    int GetSigToken(PCCOR_SIGNATURE pSig, DWORD cbSig);
    DWORD NewLocal(CorElementType typ = ELEMENT_TYPE_I);
    DWORD NewLocal(LocalDesc loc);

    DWORD SetStubTargetArgType(CorElementType typ, bool fConsumeStubArg = true);
    DWORD SetStubTargetArgType(LocalDesc* pLoc = NULL, bool fConsumeStubArg = true);       // passing pLoc = NULL means "use stub arg type"
    void SetStubTargetReturnType(CorElementType typ);
    void SetStubTargetReturnType(LocalDesc* pLoc);
    void SetStubTargetCallingConv(CorCallingConvention uNativeCallingConv);
    void SetStubTargetCallingConv(CorInfoCallConvExtension callConv);

    bool ReturnOpcodePopsStack()
    {
        if ((!m_fIsReverseStub && m_StubHasVoidReturnType) || (m_fIsReverseStub && m_StubTargetHasVoidReturnType))
        {
            return false;
        }
        return true;
    }

    void TransformArgForJIT(LocalDesc *pLoc);

    Module * GetStubSigModule();
    SigTypeContext *GetStubSigTypeContext();

    BOOL    m_StubHasVoidReturnType;
    BOOL    m_StubTargetHasVoidReturnType;
    BOOL    m_fIsReverseStub;
    INT     m_iTargetStackDelta;
    DWORD   m_cbCurrentCompressedSigLen;
    DWORD   m_nLocals;

    bool    m_fHasThis;

    // We need this MethodDesc so we can reconstruct the generics
    // SigTypeContext info, if needed.
    MethodDesc * m_pMD;
};  // class ILStubLinker


//---------------------------------------------------------------------------------------
//
class ILCodeLabel
{
    friend class ILStubLinker;
    friend class ILCodeStream;

public:
    ILCodeLabel();
    ~ILCodeLabel();

    size_t GetCodeOffset();

private:
    void SetCodeOffset(size_t codeOffset);

    ILCodeLabel*  m_pNext;
    ILStubLinker* m_pOwningStubLinker;
    ILCodeStream* m_pCodeStreamOfLabel;         // this is the ILCodeStream that the index is relative to
    size_t        m_codeOffset;                 // this is the absolute resolved IL offset after linking
    UINT          m_idxLabeledInstruction;      // this is the index within the instruction buffer of the owning ILCodeStream
};

class ILCodeStream
{
    friend class ILStubLinker;

public:
    enum ILInstrEnum
    {
#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
        name,

#include "opcode.def"
#undef OPDEF
    };

private:
    static ILInstrEnum LowerOpcode(ILInstrEnum instr, ILStubLinker::ILInstruction* pInstr);

#ifdef _DEBUG
    static bool IsSupportedInstruction(ILInstrEnum instr);
#endif // _DEBUG

    static bool IsBranchInstruction(ILInstrEnum instr)
    {
        LIMITED_METHOD_CONTRACT;
        return ((instr >= CEE_BR) && (instr <= CEE_BLT_UN)) || (instr == CEE_LEAVE);
    }


    void BeginHandler   (DWORD kind, DWORD typeToken);
    void EndHandler     (DWORD kind);
public:
    void BeginTryBlock  ();
    void EndTryBlock    ();
    void BeginCatchBlock(int token);
    void EndCatchBlock  ();
    void BeginFinallyBlock();
    void EndFinallyBlock();

    void EmitADD        ();
    void EmitADD_OVF    ();
    void EmitAND        ();
    void EmitARGLIST    ();
    void EmitBEQ        (ILCodeLabel* pCodeLabel);
    void EmitBGE        (ILCodeLabel* pCodeLabel);
    void EmitBGE_UN     (ILCodeLabel* pCodeLabel);
    void EmitBGT        (ILCodeLabel* pCodeLabel);
    void EmitBLE        (ILCodeLabel* pCodeLabel);
    void EmitBLE_UN     (ILCodeLabel* pCodeLabel);
    void EmitBLT        (ILCodeLabel* pCodeLabel);
    void EmitBNE_UN     (ILCodeLabel* pCodeLabel);
    void EmitBR         (ILCodeLabel* pCodeLabel);
    void EmitBREAK      ();
    void EmitBRFALSE    (ILCodeLabel* pCodeLabel);
    void EmitBRTRUE     (ILCodeLabel* pCodeLabel);
    void EmitCALL       (int token, int numInArgs, int numRetArgs);
    void EmitCALLI      (int token, int numInArgs, int numRetArgs);
    void EmitCALLVIRT   (int token, int numInArgs, int numRetArgs);
    void EmitCEQ        ();
    void EmitCGT        ();
    void EmitCGT_UN     ();
    void EmitCLT        ();
    void EmitCLT_UN     ();
    void EmitCONSTRAINED(int token);
    void EmitCONV_I     ();
    void EmitCONV_I1    ();
    void EmitCONV_I2    ();
    void EmitCONV_I4    ();
    void EmitCONV_I8    ();
    void EmitCONV_U     ();
    void EmitCONV_U1    ();
    void EmitCONV_U2    ();
    void EmitCONV_U4    ();
    void EmitCONV_U8    ();
    void EmitCONV_R4    ();
    void EmitCONV_R8    ();
    void EmitCONV_OVF_I4();
    void EmitCONV_T     (CorElementType type);
    void EmitCPBLK      ();
    void EmitCPOBJ      (int token);
    void EmitDUP        ();
    void EmitENDFINALLY ();
    void EmitINITBLK    ();
    void EmitINITOBJ    (int token);
    void EmitJMP        (int token);
    void EmitLDARG      (unsigned uArgIdx);
    void EmitLDARGA     (unsigned uArgIdx);
    void EmitLDC        (DWORD_PTR uConst);
    void EmitLDC_R4     (UINT32 uConst);
    void EmitLDC_R8     (UINT64 uConst);
    void EmitLDELEMA    (int token);
    void EmitLDELEM_REF ();
    void EmitLDFLD      (int token);
    void EmitLDFLDA     (int token);
    void EmitLDFTN      (int token);
    void EmitLDIND_I    ();
    void EmitLDIND_I1   ();
    void EmitLDIND_I2   ();
    void EmitLDIND_I4   ();
    void EmitLDIND_I8   ();
    void EmitLDIND_R4   ();
    void EmitLDIND_R8   ();
    void EmitLDIND_REF  ();
    void EmitLDIND_T    (LocalDesc* pType);
    void EmitLDIND_U1   ();
    void EmitLDIND_U2   ();
    void EmitLDIND_U4   ();
    void EmitLDLEN      ();
    void EmitLDLOC      (DWORD dwLocalNum);
    void EmitLDLOCA     (DWORD dwLocalNum);
    void EmitLDNULL     ();
    void EmitLDOBJ      (int token);
    void EmitLDSFLD     (int token);
    void EmitLDSFLDA    (int token);
    void EmitLDTOKEN    (int token);
    void EmitLEAVE      (ILCodeLabel* pCodeLabel);
    void EmitLOCALLOC   ();
    void EmitMUL        ();
    void EmitMUL_OVF    ();
    void EmitNEWOBJ     (int token, int numInArgs);
    void EmitNOP        (LPCSTR pszNopComment);
    void EmitPOP        ();
    void EmitRET        ();
    void EmitSHR_UN     ();
    void EmitSTARG      (unsigned uArgIdx);
    void EmitSTELEM_REF ();
    void EmitSTIND_I    ();
    void EmitSTIND_I1   ();
    void EmitSTIND_I2   ();
    void EmitSTIND_I4   ();
    void EmitSTIND_I8   ();
    void EmitSTIND_R4   ();
    void EmitSTIND_R8   ();
    void EmitSTIND_REF  ();
    void EmitSTIND_T    (LocalDesc* pType);
    void EmitSTFLD      (int token);
    void EmitSTLOC      (DWORD dwLocalNum);
    void EmitSTOBJ      (int token);
    void EmitSTSFLD     (int token);
    void EmitSUB        ();
    void EmitTHROW      ();
    void EmitUNALIGNED  (BYTE alignment);

    // Overloads to simplify common usage patterns
    void EmitNEWOBJ     (BinderMethodID id, int numInArgs);
    void EmitCALL       (BinderMethodID id, int numInArgs, int numRetArgs);
    void EmitLDFLD      (BinderFieldID id);
    void EmitSTFLD      (BinderFieldID id);
    void EmitLDFLDA     (BinderFieldID id);

    void EmitLabel(ILCodeLabel* pLabel);
    void EmitLoadThis ();
    void EmitLoadNullPtr();
    void EmitArgIteratorCreateAndLoad();

    ILCodeLabel* NewCodeLabel();

    void ClearCode();

    //
    // these functions just forward to the owning ILStubLinker
    //

    int GetToken(MethodDesc* pMD);
    int GetToken(MethodDesc* pMD, mdToken typeSignature);
    int GetToken(MethodDesc* pMD, mdToken typeSignature, mdToken methodSignature);
    int GetToken(MethodTable* pMT);
    int GetToken(TypeHandle th);
    int GetToken(FieldDesc* pFD);
    int GetToken(FieldDesc* pFD, mdToken typeSignature);
    int GetSigToken(PCCOR_SIGNATURE pSig, DWORD cbSig);

    DWORD NewLocal(CorElementType typ = ELEMENT_TYPE_I);
    DWORD NewLocal(LocalDesc loc);
    DWORD SetStubTargetArgType(CorElementType typ, bool fConsumeStubArg = true);
    DWORD SetStubTargetArgType(LocalDesc* pLoc = NULL, bool fConsumeStubArg = true);       // passing pLoc = NULL means "use stub arg type"
    void SetStubTargetReturnType(CorElementType typ);
    void SetStubTargetReturnType(LocalDesc* pLoc);


    //
    // ctors/dtor
    //

    ILCodeStream(ILStubLinker* pOwner, ILStubLinker::CodeStreamType codeStreamType) :
        m_pNextStream(NULL),
        m_pOwner(pOwner),
        m_pqbILInstructions(NULL),
        m_uCurInstrIdx(0),
        m_codeStreamType(codeStreamType)
    {
    }

    ~ILCodeStream()
    {
        CONTRACTL
        {
            MODE_ANY;
            NOTHROW;
            GC_TRIGGERS;
        }
        CONTRACTL_END;

        if (NULL != m_pqbILInstructions)
        {
            delete m_pqbILInstructions;
            m_pqbILInstructions = NULL;
        }
    }

    ILStubLinker::CodeStreamType GetStreamType() { return m_codeStreamType; }

    LPCSTR GetStreamDescription(ILStubLinker::CodeStreamType streamType);

protected:

    void Emit(ILInstrEnum instr, INT16 iStackDelta, UINT_PTR uArg);

    enum Constants
    {
        INITIAL_NUM_IL_INSTRUCTIONS = 64,
        INITIAL_IL_INSTRUCTION_BUFFER_SIZE = INITIAL_NUM_IL_INSTRUCTIONS * sizeof(ILStubLinker::ILInstruction),
    };

    typedef CQuickBytesSpecifySize<INITIAL_IL_INSTRUCTION_BUFFER_SIZE> ILCodeStreamBuffer;

    ILCodeStream*                 m_pNextStream;
    ILStubLinker*                 m_pOwner;
    ILCodeStreamBuffer*           m_pqbILInstructions;
    UINT                          m_uCurInstrIdx;
    ILStubLinker::CodeStreamType  m_codeStreamType;       // Type of the ILCodeStream
    SArray<ILStubEHClauseBuilder> m_buildingEHClauses;
    SArray<ILStubEHClauseBuilder> m_finishedEHClauses;

#ifndef TARGET_64BIT
    const static UINT32 SPECIAL_VALUE_NAN_64_ON_32 = 0xFFFFFFFF;
#endif // TARGET_64BIT
};
#endif // DACCESS_COMPILE

#define TOKEN_ILSTUB_TARGET_SIG (TokenFromRid(0xFFFFFF, mdtSignature))

#endif  // __STUBGEN_H__
