// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// precode.h
//

//
// Stub that runs before the actual native code

#ifndef __PRECODE_H__
#define __PRECODE_H__

typedef DPTR(class Precode) PTR_Precode;

#ifndef PRECODE_ALIGNMENT
#define PRECODE_ALIGNMENT sizeof(void*)
#endif

enum PrecodeType {
    PRECODE_INVALID         = InvalidPrecode::Type,
    PRECODE_STUB            = StubPrecode::Type,
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    PRECODE_NDIRECT_IMPORT  = NDirectImportPrecode::Type,
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_REMOTING_PRECODE
    PRECODE_REMOTING        = RemotingPrecode::Type,
#endif // HAS_REMOTING_PRECODE
#ifdef HAS_FIXUP_PRECODE
    PRECODE_FIXUP           = FixupPrecode::Type,
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    PRECODE_THISPTR_RETBUF  = ThisPtrRetBufPrecode::Type,
#endif // HAS_THISPTR_RETBUF_PRECODE
};

// For more details see. file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
class Precode {
#ifdef DACCESS_COMPILE
    friend class NativeImageDumper;
#endif

    BYTE m_data[SIZEOF_PRECODE_BASE];

    StubPrecode* AsStubPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_StubPrecode>(this);
    }

#ifdef HAS_NDIRECT_IMPORT_PRECODE
public:
    // Fake precodes has to be exposed
    NDirectImportPrecode* AsNDirectImportPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_NDirectImportPrecode>(this);
    }

private:
#endif // HAS_NDIRECT_IMPORT_PRECODE

#ifdef HAS_REMOTING_PRECODE
    RemotingPrecode* AsRemotingPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_RemotingPrecode>(this);
    }
#endif // HAS_REMOTING_PRECODE

#ifdef HAS_FIXUP_PRECODE
    FixupPrecode* AsFixupPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_FixupPrecode>(this);
    }
#endif // HAS_FIXUP_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE
    ThisPtrRetBufPrecode* AsThisPtrRetBufPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<PTR_ThisPtrRetBufPrecode>(this);
    }
#endif // HAS_THISPTR_RETBUF_PRECODE

    TADDR GetStart()
    {
        SUPPORTS_DAC;
        LIMITED_METHOD_CONTRACT;
        return dac_cast<TADDR>(this);
    }

    static void UnexpectedPrecodeType(const char * originator, PrecodeType precodeType)

    {
        SUPPORTS_DAC;
#ifdef DACCESS_COMPILE
        DacError(E_UNEXPECTED);
#else
#ifdef _PREFIX_
        // We only use __UNREACHABLE here since otherwise it would be a hint 
        // for the compiler to fold this case with the other cases in a switch
        // statement. However, we would rather have this case be a separate
        // code path so that we will get a clean crash sooner.
        __UNREACHABLE("Unexpected precode type");
#endif
        CONSISTENCY_CHECK_MSGF(false, ("%s: Unexpected precode type: 0x%02x.", originator, precodeType));
#endif
    }

public:
    PrecodeType GetType()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

#ifdef OFFSETOF_PRECODE_TYPE

        BYTE type = m_data[OFFSETOF_PRECODE_TYPE];
#ifdef _TARGET_X86_
        if (type == X86_INSTR_MOV_RM_R)
            type = m_data[OFFSETOF_PRECODE_TYPE_MOV_RM_R];
#endif //  _TARGET_X86_

#ifdef _TARGET_AMD64_
        if (type == (X86_INSTR_MOV_R10_IMM64 & 0xFF))
            type = m_data[OFFSETOF_PRECODE_TYPE_MOV_R10];
        else if ((type == (X86_INSTR_CALL_REL32 & 0xFF)) || (type == (X86_INSTR_JMP_REL32  & 0xFF)))
            type = m_data[OFFSETOF_PRECODE_TYPE_CALL_OR_JMP];
#endif // _AMD64

#if defined(HAS_FIXUP_PRECODE) && (defined(_TARGET_X86_) || defined(_TARGET_AMD64_))
        if (type == FixupPrecode::TypePrestub)
            type = FixupPrecode::Type;
#endif

#ifdef _TARGET_ARM_
        static_assert_no_msg(offsetof(StubPrecode, m_pTarget) == offsetof(NDirectImportPrecode, m_pMethodDesc));
        // If the precode does not have thumb bit on target, it must be NDirectImportPrecode.
        if (type == StubPrecode::Type && ((AsStubPrecode()->m_pTarget & THUMB_CODE) == 0))
            type = NDirectImportPrecode::Type;
#endif

        return (PrecodeType)type;

#else // OFFSETOF_PRECODE_TYPE
        return PRECODE_STUB;
#endif // OFFSETOF_PRECODE_TYPE
    }

    static BOOL IsValidType(PrecodeType t);

    static int AlignOf(PrecodeType t)
    {
        SUPPORTS_DAC;
        int align = PRECODE_ALIGNMENT;

#if defined(_TARGET_X86_) && defined(HAS_FIXUP_PRECODE)
        // Fixup precodes has to be aligned to allow atomic patching
        if (t == PRECODE_FIXUP)
            align = 8;
#endif // _TARGET_X86_ && HAS_FIXUP_PRECODE

        return align;
    }

    static SIZE_T SizeOf(PrecodeType t);

    SIZE_T SizeOf()
    {
        WRAPPER_NO_CONTRACT;
        return SizeOf(GetType());
    }

    // Note: This is immediate target of the precode. It does not follow jump stub if there is one.
    PCODE GetTarget();

    BOOL IsPointingTo(PCODE target, PCODE addr)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

#ifdef CROSSGEN_COMPILE
        // Crossgen does not create jump stubs on AMD64, so just return always false here to 
        // avoid non-deterministic behavior.
        return FALSE;
#else // CROSSGEN_COMPILE
        if (target == addr)
            return TRUE;

#ifdef _TARGET_AMD64_
        // Handle jump stubs
        if (isJumpRel64(target)) {
            target = decodeJump64(target);
            if (target == addr)
                return TRUE;
        }
#endif // _TARGET_AMD64_

        return FALSE;
#endif // CROSSGEN_COMPILE
    }

    BOOL IsPointingToNativeCode(PCODE pNativeCode)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

#ifdef HAS_REMOTING_PRECODE
        // Remoting precode is special case
        if (GetType() == PRECODE_REMOTING)
            return FALSE;
#endif

        return IsPointingTo(GetTarget(), pNativeCode);
    }

    BOOL IsPointingToPrestub(PCODE target);

    BOOL IsPointingToPrestub()
    {
        WRAPPER_NO_CONTRACT;
        return IsPointingToPrestub(GetTarget());
    }

    PCODE GetEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<TADDR>(this) + GetEntryPointOffset();
    }

    static SIZE_T GetEntryPointOffset()
    {
        LIMITED_METHOD_CONTRACT;
#ifdef _TARGET_ARM_
        return THUMB_CODE;
#else
        return 0;
#endif
    }

    MethodDesc *  GetMethodDesc(BOOL fSpeculative = FALSE);
    BOOL          IsCorrectMethodDesc(MethodDesc *  pMD);

    static Precode* Allocate(PrecodeType t, MethodDesc* pMD,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);
    void Init(PrecodeType t, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

#ifndef DACCESS_COMPILE
    BOOL SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub = TRUE);

    // Reset precode to point to prestub
    void Reset();
#endif // DACCESS_COMPILE

    static Precode* GetPrecodeFromEntryPoint(PCODE addr, BOOL fSpeculative = FALSE)
    {
        LIMITED_METHOD_DAC_CONTRACT;

#ifdef DACCESS_COMPILE
        // Always use speculative checks with DAC
        fSpeculative = TRUE;
#endif

        TADDR pInstr = PCODEToPINSTR(addr);

        // Always do consistency check in debug
        if (fSpeculative INDEBUG(|| TRUE))
        {
            if (!IS_ALIGNED(pInstr, PRECODE_ALIGNMENT) || !IsValidType(PTR_Precode(pInstr)->GetType()))
            {
                if (fSpeculative) return NULL;
                _ASSERTE(!"Precode::GetPrecodeFromEntryPoint: Unexpected code in precode");
            }
        }

        Precode* pPrecode = PTR_Precode(pInstr);

        if (!fSpeculative)
        {
            g_IBCLogger.LogMethodPrecodeAccess(pPrecode->GetMethodDesc());
        }

        return pPrecode;
    }

    // If addr is patched fixup precode, returns address that it points to. Otherwise returns NULL.
    static PCODE TryToSkipFixupPrecode(PCODE addr);

    //
    // Precode as temporary entrypoint
    // 

    static SIZE_T SizeOfTemporaryEntryPoint(PrecodeType t)
    {
        LIMITED_METHOD_DAC_CONTRACT;
#ifdef HAS_FIXUP_PRECODE_CHUNKS
        _ASSERTE(t != PRECODE_FIXUP);
#endif
        return ALIGN_UP(SizeOf(t), AlignOf(t));
    }

    static Precode * GetPrecodeForTemporaryEntryPoint(TADDR temporaryEntryPoints, int index);

    static SIZE_T SizeOfTemporaryEntryPoints(PrecodeType t, bool preallocateJumpStubs, int count);
    static SIZE_T SizeOfTemporaryEntryPoints(TADDR temporaryEntryPoints, int count);

    static TADDR AllocateTemporaryEntryPoints(MethodDescChunk* pChunk,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

#ifdef FEATURE_PREJIT
    //
    // NGEN stuff
    // 

    void Save(DataImage *image);
    void Fixup(DataImage *image, MethodDesc * pMD);

    BOOL IsPrebound(DataImage *image);

    // Helper class for saving precodes in chunks
    class SaveChunk
    {
#ifdef HAS_FIXUP_PRECODE_CHUNKS
        // Array of methods to be saved in the method desc chunk
        InlineSArray<MethodDesc *, 20> m_rgPendingChunk;
#endif // HAS_FIXUP_PRECODE_CHUNKS

    public:
        void Save(DataImage * image, MethodDesc * pMD);
        void Flush(DataImage * image);
    };
#endif // FEATURE_PREJIT

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

#ifdef HAS_FIXUP_PRECODE_CHUNKS
    static DWORD GetOffsetOfBase(PrecodeType t, DWORD count)
    {
        assert(t == PRECODE_FIXUP);
        return (DWORD)(count * sizeof(FixupPrecode));
    }

    static DWORD GetOffset(PrecodeType t, DWORD index, DWORD count)
    {
        assert(t == PRECODE_FIXUP);
        assert(index < count); 
        return (DWORD)((count - index - 1)* sizeof(FixupPrecode));
    }
#endif
};

#endif // __PRECODE_H__
