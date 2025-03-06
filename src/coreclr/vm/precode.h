// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// precode.h
//

//
// Stub that runs before the actual native code

#ifndef __PRECODE_H__
#define __PRECODE_H__

#define PRECODE_ALIGNMENT sizeof(void*)

#if defined(TARGET_AMD64)

#define OFFSETOF_PRECODE_TYPE              0
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_R10     10

#define SIZEOF_PRECODE_BASE               16

#elif defined(TARGET_X86)

EXTERN_C VOID STDCALL PrecodeRemotingThunk();

#define OFFSETOF_PRECODE_TYPE              0
#define OFFSETOF_PRECODE_TYPE_CALL_OR_JMP  5
#define OFFSETOF_PRECODE_TYPE_MOV_RM_R     6

#define SIZEOF_PRECODE_BASE                8

#elif defined(TARGET_ARM64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0

#elif defined(TARGET_ARM)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN * 2
#define OFFSETOF_PRECODE_TYPE       7

#elif defined(TARGET_LOONGARCH64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0
#define SHIFTOF_PRECODE_TYPE        5

#elif defined(TARGET_RISCV64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN
#define OFFSETOF_PRECODE_TYPE       0

#endif // TARGET_AMD64

#ifndef DACCESS_COMPILE
// Given an address in a slot, figure out if the prestub will be called
BOOL DoesSlotCallPrestub(PCODE pCode);
#endif

#include <pshpack1.h>

// Invalid precode type
struct InvalidPrecode
{
#if defined(TARGET_AMD64) || defined(TARGET_X86)
    // int3
    static const int Type = 0xCC;
#elif defined(TARGET_ARM64) || defined(TARGET_ARM)
    static const int Type = 0;
#elif defined(TARGET_LOONGARCH64)
    static const int Type = 0xff;
#elif defined(TARGET_RISCV64)
    static const int Type = 0xff;
#endif
};

struct StubPrecodeData
{
    TADDR SecretParam;
    PCODE Target;
    TADDR Type; // Use a TADDR here instead of just a byte, so that different offsets into the StubPrecode can't mistakenly
                // match the Type field.  This is a defense-in-depth measure (and only matters for access from the debugger)
};

typedef DPTR(StubPrecodeData) PTR_StubPrecodeData;

#if !(defined(TARGET_ARM64) && defined(TARGET_UNIX))
extern "C" void StubPrecodeCode();
extern "C" void StubPrecodeCode_End();
#endif

#ifdef FEATURE_INTERPRETER
extern "C" void InterpreterStub();
#endif

class UMEntryThunk;
typedef DPTR(class UMEntryThunk) PTR_UMEntryThunk;
#define PRECODE_UMENTRY_THUNK_VALUE 0x7 // Define the value here and not in UMEntryThunk to avoid circular dependency with the dllimportcallback.h header

// Regular precode
struct StubPrecode
{
#if defined(TARGET_AMD64)
    static const BYTE Type = 0x4C;
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_X86)
    static const BYTE Type = 0xA1;
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_ARM64)
    static const int Type = 0x4A;
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_ARM)
    static const int Type = 0xFF;
    static const SIZE_T CodeSize = 12;
#elif defined(TARGET_LOONGARCH64)
    static const int Type = 0x4;
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_RISCV64)
    static const int Type = 0x17;
    static const SIZE_T CodeSize = 24;
#endif // TARGET_AMD64

    BYTE m_code[CodeSize];

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    static void (*StubPrecodeCode)();
    static void (*StubPrecodeCode_End)();
#endif

    void Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator = NULL, TADDR type = StubPrecode::Type, TADDR target = 0);

    static void StaticInitialize();

    PTR_StubPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_StubPrecodeData>(dac_cast<TADDR>(this) + GetStubCodePageSize());
    }

    TADDR GetMethodDesc();

#ifndef DACCESS_COMPILE
    void SetSecretParam(TADDR secretParam)
    {
        LIMITED_METHOD_CONTRACT;

        GetData()->SecretParam = secretParam;
    }
#endif // DACCESS_COMPILE

    TADDR GetSecretParam() const
    {
        LIMITED_METHOD_CONTRACT;

        return GetData()->SecretParam;
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetData()->Target;
    }

    BYTE GetType();

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

    void SetTargetUnconditional(TADDR target)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        StubPrecodeData *pData = GetData();
        pData->Target = (PCODE)target;
    }

    static void GenerateCodePage(BYTE* pageBase, BYTE* pageBaseRX, SIZE_T size);

#endif // !DACCESS_COMPILE
};

typedef DPTR(StubPrecode) PTR_StubPrecode;


#ifdef HAS_NDIRECT_IMPORT_PRECODE

// NDirect import precode
// (This is fake precode. VTable slot does not point to it.)
struct NDirectImportPrecode : StubPrecode
{
    static const int Type = 0x05;

    void Init(NDirectImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    LPVOID GetEntrypoint()
    {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)PINSTRToPCODE(dac_cast<TADDR>(this));
    }
};
typedef DPTR(NDirectImportPrecode) PTR_NDirectImportPrecode;

#endif // HAS_NDIRECT_IMPORT_PRECODE

#ifdef FEATURE_INTERPRETER
struct InterpreterPrecodeData
{
    TADDR ByteCodeAddr;
    PCODE Target;
    BYTE Type;
};

typedef DPTR(InterpreterPrecodeData) PTR_InterpreterPrecodeData;

struct InterpreterPrecode
{
    static const int Type = 0x06;

    BYTE m_code[StubPrecode::CodeSize];

    void Init(InterpreterPrecode* pPrecodeRX, TADDR byteCodeAddr);

    PTR_InterpreterPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_InterpreterPrecodeData>(dac_cast<TADDR>(this) + GetStubCodePageSize());
    }

    PCODE GetEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return PINSTRToPCODE(dac_cast<TADDR>(this));
    }
};
#endif // FEATURE_INTERPRETER

#ifdef HAS_FIXUP_PRECODE

struct FixupPrecodeData
{
    PCODE Target;
    class MethodDesc *MethodDesc;
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
#if defined(TARGET_AMD64)
    static const int Type = 0xFF;
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(TARGET_X86)
    static const int Type = 0xFF;
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(TARGET_ARM64)
    static const int Type = 0x0B;
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 8;
#elif defined(TARGET_ARM)
    static const int Type = 0xCF;
    static const SIZE_T CodeSize = 12;
    static const int FixupCodeOffset = 4 + THUMB_CODE;
#elif defined(TARGET_LOONGARCH64)
    static const int Type = 0x3;
    static const SIZE_T CodeSize = 32;
    static const int FixupCodeOffset = 12;
#elif defined(TARGET_RISCV64)
    static const int Type = 0x97;
    static const SIZE_T CodeSize = 32;
    static const int FixupCodeOffset = 10;
#endif // TARGET_AMD64

    BYTE m_code[CodeSize];

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    static void (*FixupPrecodeCode)();
    static void (*FixupPrecodeCode_End)();
#endif

    void Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    static void StaticInitialize();

    static void GenerateCodePage(BYTE* pageBase, BYTE* pageBaseRX, SIZE_T size);

    PTR_FixupPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_FixupPrecodeData>(dac_cast<TADDR>(this) + GetStubCodePageSize());
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
#ifdef FEATURE_INTERPRETER
    PRECODE_INTERPRETER     = InterpreterPrecode::Type,
#endif // FEATURE_INTERPRETER
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    PRECODE_NDIRECT_IMPORT  = NDirectImportPrecode::Type,
#endif // HAS_NDIRECT_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    PRECODE_FIXUP           = FixupPrecode::Type,
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    PRECODE_THISPTR_RETBUF  = ThisPtrRetBufPrecode::Type,
#endif // HAS_THISPTR_RETBUF_PRECODE
    PRECODE_UMENTRY_THUNK   = PRECODE_UMENTRY_THUNK_VALUE, // Set the value here and not in UMEntryThunk to avoid circular dependency
};

inline TADDR StubPrecode::GetMethodDesc()
{
    LIMITED_METHOD_DAC_CONTRACT;

    switch (GetType())
    {
        case PRECODE_STUB:
        case PRECODE_NDIRECT_IMPORT:
            return GetSecretParam();

        case PRECODE_UMENTRY_THUNK:
#ifdef FEATURE_INTERPRETER
        case PRECODE_INTERPRETER:
#endif // FEATURE_INTERPRETER
            return 0;
    }

    _ASSERTE(!"Unknown precode type");
    return 0;
}

inline BYTE StubPrecode::GetType()
{
    LIMITED_METHOD_DAC_CONTRACT;
    TADDR type = GetData()->Type;

    // There are a limited number of valid bit patterns here. Restrict to those, so that the
    // speculative variant of GetPrecodeFromEntryPoint is more robust. Type is stored as a TADDR
    // so that a single byte matching is not enough to cause a false match.
    switch (type)
    {
        case PRECODE_UMENTRY_THUNK:
        case PRECODE_STUB:
        case PRECODE_NDIRECT_IMPORT:
#ifdef FEATURE_INTERPRETER
        case PRECODE_INTERPRETER:
#endif // FEATURE_INTERPRETER
            return (BYTE)type;
    }

    return 0;
}


// For more details see. file:../../doc/BookOfTheRuntime/ClassLoader/MethodDescDesign.doc
class Precode {

    BYTE m_data[SIZEOF_PRECODE_BASE];

public:
    UMEntryThunk* AsUMEntryThunk();
    StubPrecode* AsStubPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_StubPrecode>(this);
    }
private:

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

#if defined(TARGET_LOONGARCH64)
        assert(0 == OFFSETOF_PRECODE_TYPE);
        static_assert(5 == SHIFTOF_PRECODE_TYPE, "expected shift of 5");
        short type = *((short*)m_data);
        type >>= SHIFTOF_PRECODE_TYPE;
#elif defined(TARGET_RISCV64)
        assert(0 == OFFSETOF_PRECODE_TYPE);
        BYTE type = *((BYTE*)m_data + OFFSETOF_PRECODE_TYPE);
#else
#if defined(SHIFTOF_PRECODE_TYPE)
#error "did not expect SHIFTOF_PRECODE_TYPE to be defined"
#endif
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

    static Precode* Allocate(PrecodeType t, MethodDesc* pMD,
        LoaderAllocator *pLoaderAllocator, AllocMemTracker *pamTracker);
#ifdef FEATURE_INTERPRETER
    static InterpreterPrecode* AllocateInterpreterPrecode(PCODE byteCode,
        LoaderAllocator *  pLoaderAllocator, AllocMemTracker *  pamTracker);
#endif // FEATURE_INTERPRETER
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

// Verify that the base type for each precode fits into each specific precode type
static_assert_no_msg(sizeof(Precode) <= sizeof(NDirectImportPrecode));
static_assert_no_msg(sizeof(Precode) <= sizeof(FixupPrecode));
static_assert_no_msg(sizeof(Precode) <= sizeof(ThisPtrRetBufPrecode));

#ifndef DACCESS_COMPILE
// A summary of the precode layout for diagnostic purposes
struct PrecodeMachineDescriptor
{
    uint32_t StubCodePageSize;

    uint8_t OffsetOfPrecodeType;
    // cDAC will do (where N = 8*ReadWidthOfPrecodeType):
    //   uintN_t PrecodeType = *(uintN_t*)(pPrecode + OffsetOfPrecodeType);
    //   PrecodeType >>= ShiftOfPrecodeType;
    //   return (byte)PrecodeType;
    uint8_t ReadWidthOfPrecodeType;
    uint8_t ShiftOfPrecodeType;

    uint8_t InvalidPrecodeType;
    uint8_t StubPrecodeType;
#ifdef HAS_NDIRECT_IMPORT_PRECODE
    uint8_t PInvokeImportPrecodeType;
#endif

#ifdef HAS_FIXUP_PRECODE
    uint8_t FixupPrecodeType;
#endif

#ifdef HAS_THISPTR_RETBUF_PRECODE
    uint8_t ThisPointerRetBufPrecodeType;
#endif

public:
    PrecodeMachineDescriptor() = default;
    PrecodeMachineDescriptor(const PrecodeMachineDescriptor&) = delete;
    PrecodeMachineDescriptor& operator=(const PrecodeMachineDescriptor&) = delete;
    static void Init(PrecodeMachineDescriptor* dest);
};
#endif //DACCESS_COMPILE

#endif // __PRECODE_H__
