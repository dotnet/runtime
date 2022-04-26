// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// precode.h
//

//
// Stub that runs before the actual native code

#ifndef __PRECODE_H__
#define __PRECODE_H__

#define PRECODE_ALIGNMENT sizeof(void*)

#if defined(HOST_AMD64)

#define OFFSETOF_PRECODE_TYPE              0
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_R10     10

#define SIZEOF_PRECODE_BASE               16

#elif defined(HOST_X86)

EXTERN_C VOID STDCALL PrecodeRemotingThunk();

#define OFFSETOF_PRECODE_TYPE              0
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_RM_R     6

#define SIZEOF_PRECODE_BASE                8

#elif defined(HOST_ARM64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0

#elif defined(HOST_ARM)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       3

#elif defined(HOST_LOONGARCH64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0

#endif // HOST_AMD64

#ifndef DACCESS_COMPILE
// Given an address in a slot, figure out if the prestub will be called
BOOL DoesSlotCallPrestub(PCODE pCode);
#endif

#include <pshpack1.h>

// Invalid precode type
struct InvalidPrecode
{
#if defined(HOST_AMD64) || defined(HOST_X86)
    // int3
    static const int Type = 0xCC;
#elif defined(HOST_ARM64) || defined(HOST_ARM)
    static const int Type = 0;
#elif defined(HOST_LOONGARCH64)
    static const int Type = 0xff;
#endif
};

struct StubPrecodeData
{
    PTR_MethodDesc MethodDesc;
    PCODE Target;
    BYTE Type;
};

typedef DPTR(StubPrecodeData) PTR_StubPrecodeData;

#if !(defined(TARGET_ARM64) && defined(TARGET_UNIX))
extern "C" void StubPrecodeCode();
extern "C" void StubPrecodeCode_End();
#endif

// Regular precode
struct StubPrecode
{
#if defined(HOST_AMD64)
    static const BYTE Type = 0x4C;
    static const int CodeSize = 24;
#elif defined(HOST_X86)
    static const BYTE Type = 0xA1;
    static const int CodeSize = 24;
#elif defined(HOST_ARM64)
    static const int Type = 0x4A;
    static const int CodeSize = 24;
#elif defined(HOST_ARM)
    static const int Type = 0xCF;
    static const int CodeSize = 12;
#elif defined(HOST_LOONGARCH64)
    static const int Type = 0x4;
    static const int CodeSize = 24;
#endif // HOST_AMD64

    BYTE m_code[CodeSize];

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    static void (*StubPrecodeCode)();
    static void (*StubPrecodeCode_End)();
#endif

    void Init(StubPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator = NULL, BYTE type = StubPrecode::Type, TADDR target = NULL);

    static void StaticInitialize();

    PTR_StubPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_StubPrecodeData>(dac_cast<TADDR>(this) + GetOsPageSize());
    }

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return dac_cast<TADDR>(GetData()->MethodDesc);
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetData()->Target;
    }

    BYTE GetType()
    {
        return GetData()->Type;
    }

#ifndef DACCESS_COMPILE
    static BOOL IsStubPrecodeByASM(PCODE addr);

    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        StubPrecodeData *pData = GetData();
        InterlockedExchangeT<PCODE>(&pData->Target, GetPreStubEntryPoint());
    }

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        StubPrecodeData *pData = GetData();
        return InterlockedCompareExchangeT<PCODE>(&pData->Target, (PCODE)target, (PCODE)expected) == expected;
  }

    static void GenerateCodePage(BYTE* pageBase, BYTE* pageBaseRX);

#endif // !DACCESS_COMPILE
};

typedef DPTR(StubPrecode) PTR_StubPrecode;


#ifdef HAS_NDIRECT_IMPORT_PRECODE

// NDirect import precode
// (This is fake precode. VTable slot does not point to it.)
struct NDirectImportPrecode : StubPrecode
{
    static const int Type = 0x01;

    void Init(NDirectImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    LPVOID GetEntrypoint()
    {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)PINSTRToPCODE(dac_cast<TADDR>(this));
    }
};
typedef DPTR(NDirectImportPrecode) PTR_NDirectImportPrecode;

#endif // HAS_NDIRECT_IMPORT_PRECODE


#ifdef HAS_FIXUP_PRECODE

struct FixupPrecodeData
{
    PCODE Target;
    MethodDesc *MethodDesc;
    PCODE PrecodeFixupThunk;
};

typedef DPTR(FixupPrecodeData) PTR_FixupPrecodeData;

#if !(defined(TARGET_ARM64) && defined(TARGET_UNIX))
extern "C" void FixupPrecodeCode();
extern "C" void FixupPrecodeCode_End();
#endif

// Fixup precode is used in ngen images when the prestub does just one time fixup.
// The fixup precode is simple jump once patched. It does not have the two instruction overhead of regular precode.
struct FixupPrecode
{
#if defined(HOST_AMD64)
    static const int Type = 0xFF;
    static const int CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(HOST_X86)
    static const int Type = 0xFF;
    static const int CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(HOST_ARM64)
    static const int Type = 0x0B;
    static const int CodeSize = 24;
    static const int FixupCodeOffset = 8;
#elif defined(HOST_ARM)
    static const int Type = 0xFF;
    static const int CodeSize = 12;
    static const int FixupCodeOffset = 4 + THUMB_CODE;
#elif defined(HOST_LOONGARCH64)
    static const int Type = 0x3;
    static const int CodeSize = 32;
    static const int FixupCodeOffset = 12;
#endif // HOST_AMD64

    BYTE m_code[CodeSize];

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    static void (*FixupPrecodeCode)();
    static void (*FixupPrecodeCode_End)();
#endif

    void Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    static void StaticInitialize();

    static void GenerateCodePage(BYTE* pageBase, BYTE* pageBaseRX);

    PTR_FixupPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_FixupPrecodeData>(dac_cast<TADDR>(this) + GetOsPageSize());
    }

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_CONTRACT;
        return (TADDR)GetData()->MethodDesc;
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return GetData()->Target;
    }

    PCODE *GetTargetSlot()
    {
        LIMITED_METHOD_CONTRACT;
        return &GetData()->Target;
    }

#ifndef DACCESS_COMPILE
    static BOOL IsFixupPrecodeByASM(PCODE addr);

    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        PCODE target = (PCODE)this + FixupCodeOffset;

        _ASSERTE(IS_ALIGNED(&GetData()->Target, sizeof(SIZE_T))); 
        InterlockedExchangeT<PCODE>(&GetData()->Target, target);
    }

    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        MethodDesc * pMD = (MethodDesc*)GetMethodDesc();
        g_IBCLogger.LogMethodPrecodeWriteAccess(pMD);

        PCODE oldTarget = (PCODE)GetData()->Target;
        if (oldTarget != ((PCODE)this + FixupCodeOffset))
        {
#ifdef FEATURE_CODE_VERSIONING
            // No change needed, jmp is already in place
#else
            // Setting the target more than once is unexpected
            return FALSE;
#endif
        }

        _ASSERTE(IS_ALIGNED(&GetData()->Target, sizeof(SIZE_T)));
        return InterlockedCompareExchangeT<PCODE>(&GetData()->Target, (PCODE)target, (PCODE)oldTarget) == (PCODE)oldTarget;
    }
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

typedef DPTR(FixupPrecode) PTR_FixupPrecode;

#endif // HAS_FIXUP_PRECODE

#include <poppack.h>

typedef DPTR(class Precode) PTR_Precode;

enum PrecodeType {
    PRECODE_INVALID         = InvalidPrecode::Type,
    PRECODE_STUB            = StubPrecode::Type,
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    PRECODE_NDIRECT_IMPORT  = NDirectImportPrecode::Type,
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    PRECODE_FIXUP           = FixupPrecode::Type,
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    PRECODE_THISPTR_RETBUF  = ThisPtrRetBufPrecode::Type,
#endif // HAS_THISPTR_RETBUF_PRECODE
};

// For more details see. file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
class Precode {

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

#ifdef HAS_FIXUP_PRECODE
    PTR_FixupPrecode AsFixupPrecode()
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

#ifdef TARGET_LOONGARCH64
        assert(0 == OFFSETOF_PRECODE_TYPE);
        short type = *((short*)m_data);
        type >>= 5;
#else
        BYTE type = m_data[OFFSETOF_PRECODE_TYPE];
#endif

        if (type == StubPrecode::Type)
        {
            // StubPrecode code is used for both StubPrecode and NDirectImportPrecode,
            // so we need to get the real type
            type = AsStubPrecode()->GetType();
        }

        return (PrecodeType)type;

#else // OFFSETOF_PRECODE_TYPE
        return PRECODE_STUB;
#endif // OFFSETOF_PRECODE_TYPE
    }

    static BOOL IsValidType(PrecodeType t);

    static int AlignOf(PrecodeType t)
    {
        SUPPORTS_DAC;
        unsigned int align = PRECODE_ALIGNMENT;

#if defined(TARGET_ARM) && defined(HAS_COMPACT_ENTRYPOINTS)
        // Precodes have to be aligned to allow fast compact entry points check
        _ASSERTE (align >= sizeof(void*));
#endif // TARGET_ARM && HAS_COMPACT_ENTRYPOINTS

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

        if (target == addr)
            return TRUE;

#ifdef TARGET_AMD64
        // Handle jump stubs
        if (isJumpRel64(target)) {
            target = decodeJump64(target);
            if (target == addr)
                return TRUE;
        }
#endif // TARGET_AMD64

        return FALSE;
    }

    BOOL IsPointingToNativeCode(PCODE pNativeCode)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;

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
        return PINSTRToPCODE(dac_cast<TADDR>(this));
    }

    PTR_PCODE GetTargetSlot();
    
    MethodDesc *  GetMethodDesc(BOOL fSpeculative = FALSE);
    BOOL          IsCorrectMethodDesc(MethodDesc *  pMD);

    static Precode* Allocate(PrecodeType t, MethodDesc* pMD,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);
    void Init(Precode* pPrecodeRX, PrecodeType t, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

#ifndef DACCESS_COMPILE
    void ResetTargetInterlocked();
    BOOL SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub = TRUE);

    // Reset precode to point to prestub
    void Reset();
#endif // DACCESS_COMPILE

    static PTR_Precode GetPrecodeFromEntryPoint(PCODE addr, BOOL fSpeculative = FALSE)
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

        PTR_Precode pPrecode = PTR_Precode(pInstr);

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

        return ALIGN_UP(SizeOf(t), AlignOf(t));
    }

    static Precode * GetPrecodeForTemporaryEntryPoint(TADDR temporaryEntryPoints, int index);

    static SIZE_T SizeOfTemporaryEntryPoints(PrecodeType t, int count);
    static SIZE_T SizeOfTemporaryEntryPoints(TADDR temporaryEntryPoints, int count);

    static TADDR AllocateTemporaryEntryPoints(MethodDescChunk* pChunk,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);

    static SIZE_T GetMaxTemporaryEntryPointsCount()
    {
        SIZE_T maxPrecodeCodeSize = Max(FixupPrecode::CodeSize, StubPrecode::CodeSize);
        return GetOsPageSize() / maxPrecodeCodeSize;
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

// Verify that the type for each precode is different
static_assert_no_msg(StubPrecode::Type != NDirectImportPrecode::Type);
static_assert_no_msg(StubPrecode::Type != FixupPrecode::Type);
static_assert_no_msg(StubPrecode::Type != ThisPtrRetBufPrecode::Type);
static_assert_no_msg(FixupPrecode::Type != NDirectImportPrecode::Type);
static_assert_no_msg(FixupPrecode::Type != ThisPtrRetBufPrecode::Type);
static_assert_no_msg(NDirectImportPrecode::Type != ThisPtrRetBufPrecode::Type);

#endif // __PRECODE_H__
