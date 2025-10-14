// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// precode.h
//

//
// Stub that runs before the actual native code

#ifndef __PRECODE_H__
#define __PRECODE_H__

#ifdef FEATURE_PORTABLE_ENTRYPOINTS

#include "precode_portable.hpp"

#else // !FEATURE_PORTABLE_ENTRYPOINTS

#define PRECODE_ALIGNMENT sizeof(void*)

#if defined(TARGET_AMD64)

#define SIZEOF_PRECODE_BASE               16

#elif defined(TARGET_X86)

#define SIZEOF_PRECODE_BASE                8

#elif defined(TARGET_ARM64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN

#elif defined(TARGET_ARM)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN * 2

#elif defined(TARGET_LOONGARCH64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN

#elif defined(TARGET_RISCV64)

#define SIZEOF_PRECODE_BASE         CODE_SIZE_ALIGN

#endif // TARGET_AMD64

#ifndef DACCESS_COMPILE
// Given an address in a slot, figure out if the prestub will be called
BOOL DoesSlotCallPrestub(PCODE pCode);
#endif

#include <pshpack1.h>

// Invalid precode type
struct InvalidPrecode
{
    static const int Type = 0xff;
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

struct ThisPtrRetBufPrecode;

typedef DPTR(class UMEntryThunk) PTR_UMEntryThunk;
#define PRECODE_UMENTRY_THUNK_VALUE 0x7 // Define the value here and not in UMEntryThunk to avoid circular dependency with the dllimportcallback.h header

#ifdef FEATURE_INTERPRETER
struct InterpreterPrecode;

typedef DPTR(InterpreterPrecode) PTR_InterpreterPrecode;
#endif // FEATURE_INTERPRETER

// Regular precode
struct StubPrecode
{
    static const BYTE Type = 0x3;
#if defined(TARGET_AMD64)
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_X86)
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_ARM64)
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_ARM)
    static const SIZE_T CodeSize = 12;
#elif defined(TARGET_LOONGARCH64)
    static const SIZE_T CodeSize = 24;
#elif defined(TARGET_RISCV64)
    static const SIZE_T CodeSize = 24;
#endif // TARGET_AMD64

    BYTE m_code[CodeSize];

    void Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator = NULL, TADDR type = StubPrecode::Type, TADDR target = 0);

    static void StaticInitialize();

    PTR_StubPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_StubPrecodeData>(dac_cast<TADDR>(this) + GetStubCodePageSize());
    }

    TADDR GetMethodDesc();

#ifdef HAS_THISPTR_RETBUF_PRECODE
    ThisPtrRetBufPrecode* AsThisPtrRetBufPrecode();
#endif // HAS_THISPTR_RETBUF_PRECODE

#ifdef FEATURE_INTERPRETER
    PTR_InterpreterPrecode AsInterpreterPrecode();
#endif // FEATURE_INTERPRETER

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

    static BOOL IsStubPrecodeByASM(PCODE addr);
    static BOOL IsStubPrecodeByASM_DAC(PCODE addr);
#ifndef DACCESS_COMPILE

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

    static void GenerateCodePage(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size);

#endif // !DACCESS_COMPILE
};

typedef DPTR(StubPrecode) PTR_StubPrecode;


#ifdef HAS_PINVOKE_IMPORT_PRECODE

// PInvoke import precode
// (This is fake precode. VTable slot does not point to it.)
struct PInvokeImportPrecode : StubPrecode
{
    static const int Type = 0x05;

    void Init(PInvokeImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    LPVOID GetEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)PINSTRToPCODE(dac_cast<TADDR>(this));
    }
};
typedef DPTR(PInvokeImportPrecode) PTR_PInvokeImportPrecode;

#endif // HAS_PINVOKE_IMPORT_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE

struct ThisPtrRetBufPrecodeData
{
    PCODE Target;
    class MethodDesc *MethodDesc;
};

typedef DPTR(ThisPtrRetBufPrecodeData) PTR_ThisPtrRetBufPrecodeData;

// ThisPtrRetBufPrecode, built on the infra for the StubPrecode
// (This is fake precode. VTable slot does not point to it.)
struct ThisPtrRetBufPrecode : StubPrecode
{
    static const int Type = 0x08;

    void Init(ThisPtrRetBufPrecodeData* pPrecodeData, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);
    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    PTR_ThisPtrRetBufPrecodeData GetData() const
    {
        LIMITED_METHOD_CONTRACT;
        return dac_cast<PTR_ThisPtrRetBufPrecodeData>(StubPrecode::GetData()->SecretParam);
    }

    LPVOID GetEntryPoint()
    {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)PINSTRToPCODE(dac_cast<TADDR>(this));
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return GetData()->Target;
    }

    void ResetTargetInterlocked()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        ThisPtrRetBufPrecodeData *pData = GetData();
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

        ThisPtrRetBufPrecodeData *pData = GetData();
        return InterlockedCompareExchangeT<PCODE>(&pData->Target, (PCODE)target, (PCODE)expected) == expected;
    }

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return dac_cast<TADDR>(GetData()->MethodDesc);
    }
};
typedef DPTR(ThisPtrRetBufPrecode) PTR_ThisPtrRetBufPrecode;

inline ThisPtrRetBufPrecode* StubPrecode::AsThisPtrRetBufPrecode()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    return dac_cast<PTR_ThisPtrRetBufPrecode>(this);
}

#endif // HAS_THISPTR_RETBUF_PRECODE

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

    static InterpreterPrecode* FromEntryPoint(PCODE entryPoint)
    {
        LIMITED_METHOD_CONTRACT;
        return (InterpreterPrecode*)PCODEToPINSTR(entryPoint);
    }

    TADDR GetMethodDesc();
};

inline PTR_InterpreterPrecode StubPrecode::AsInterpreterPrecode()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return dac_cast<PTR_InterpreterPrecode>(this);
}

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
    static const int Type = 0x2;
#if defined(TARGET_AMD64)
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(TARGET_X86)
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 6;
#elif defined(TARGET_ARM64)
    static const SIZE_T CodeSize = 24;
    static const int FixupCodeOffset = 8;
#elif defined(TARGET_ARM)
    static const SIZE_T CodeSize = 16;
    static const int FixupCodeOffset = 4 + THUMB_CODE;
#elif defined(TARGET_LOONGARCH64)
    static const SIZE_T CodeSize = 32;
    static const int FixupCodeOffset = 12;
#elif defined(TARGET_RISCV64)
    static const SIZE_T CodeSize = 32;
    static const int FixupCodeOffset = 10;
#endif // TARGET_AMD64

    BYTE m_code[CodeSize];

    void Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    static void StaticInitialize();

    static void GenerateCodePage(uint8_t* pageBase, uint8_t* pageBaseRX, size_t size);
    static void GenerateDataPage(uint8_t* pageBase, size_t size);

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

    static BOOL IsFixupPrecodeByASM(PCODE addr);
    static BOOL IsFixupPrecodeByASM_DAC(PCODE addr);
#ifndef DACCESS_COMPILE

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
    PRECODE_INVALID         = InvalidPrecode::Type, // 0xFF
    PRECODE_STUB            = StubPrecode::Type, // 0x3
#ifdef FEATURE_INTERPRETER
    PRECODE_INTERPRETER     = InterpreterPrecode::Type, // 0x6
#endif // FEATURE_INTERPRETER
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    PRECODE_PINVOKE_IMPORT  = PInvokeImportPrecode::Type, // 0x5
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    PRECODE_FIXUP           = FixupPrecode::Type,
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    PRECODE_THISPTR_RETBUF  = ThisPtrRetBufPrecode::Type, // 0x8
#endif // HAS_THISPTR_RETBUF_PRECODE
    PRECODE_UMENTRY_THUNK   = PRECODE_UMENTRY_THUNK_VALUE, // 0x7 - Set the value here and not in UMEntryThunk to avoid circular dependency
#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS
    PRECODE_DYNAMIC_HELPERS = 0xa,
#endif // FEATURE_STUBPRECODE_DYNAMIC_HELPERS
};

inline TADDR StubPrecode::GetMethodDesc()
{
    LIMITED_METHOD_DAC_CONTRACT;

    switch (GetType())
    {
        case PRECODE_STUB:
        case PRECODE_PINVOKE_IMPORT:
            return GetSecretParam();

#ifdef FEATURE_INTERPRETER
        case PRECODE_INTERPRETER:
            return AsInterpreterPrecode()->GetMethodDesc();
#endif // FEATURE_INTERPRETER
        case PRECODE_UMENTRY_THUNK:
#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS
        case PRECODE_DYNAMIC_HELPERS:
#endif // FEATURE_STUBPRECODE_DYNAMIC_HELPERS
            return 0;

        case PRECODE_THISPTR_RETBUF:
            return AsThisPtrRetBufPrecode()->GetMethodDesc();
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
        case PRECODE_PINVOKE_IMPORT:
        case PRECODE_THISPTR_RETBUF:
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

#ifdef HAS_PINVOKE_IMPORT_PRECODE
    PInvokeImportPrecode* AsPInvokeImportPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_PInvokeImportPrecode>(this);
    }
#endif // HAS_PINVOKE_IMPORT_PRECODE

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

#ifdef FEATURE_INTERPRETER
    InterpreterPrecode* AsInterpreterPrecode()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return dac_cast<PTR_InterpreterPrecode>(this);
    }
#endif // FEATURE_INTERPRETER

private:
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
        CONSISTENCY_CHECK_MSGF(false, ("%s: Unexpected precode type: 0x%02x.", originator, precodeType));
#endif
    }

public:
    PrecodeType GetType()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        PrecodeType basicPrecodeType = PRECODE_INVALID;
        if (StubPrecode::IsStubPrecodeByASM(PINSTRToPCODE(dac_cast<TADDR>(this))))
        {
            basicPrecodeType = PRECODE_STUB;
        }

#ifdef HAS_FIXUP_PRECODE
        if (FixupPrecode::IsFixupPrecodeByASM(PINSTRToPCODE(dac_cast<TADDR>(this))))
        {
            basicPrecodeType = PRECODE_FIXUP;
        }
#endif

        if (basicPrecodeType == PRECODE_STUB)
        {
            // StubPrecode code is used for both StubPrecode, PInvokeImportPrecode, InterpreterPrecode, and ThisPtrRetBufPrecode,
            // so we need to get the real type
            return (PrecodeType)AsStubPrecode()->GetType();
        }
        else
        {
            return basicPrecodeType;
        }
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
            if (!IS_ALIGNED(pInstr, PRECODE_ALIGNMENT))
            {
                // This not a fixup precode or stub precode
                return NULL;
            }
            if (!StubPrecode::IsStubPrecodeByASM(addr))
            {
#ifdef HAS_FIXUP_PRECODE
                if (!FixupPrecode::IsFixupPrecodeByASM(addr))
#endif
                {
                    // This not a fixup precode or stub precode
                    return NULL;
                }
            }
        }

        PTR_Precode pPrecode = PTR_Precode(pInstr);
        return pPrecode;
    }

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

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size);

// Verify that the type for each precode is different
static_assert(StubPrecode::Type != PInvokeImportPrecode::Type);
static_assert(StubPrecode::Type != FixupPrecode::Type);
static_assert(StubPrecode::Type != ThisPtrRetBufPrecode::Type);
static_assert(FixupPrecode::Type != PInvokeImportPrecode::Type);
static_assert(FixupPrecode::Type != ThisPtrRetBufPrecode::Type);
static_assert(PInvokeImportPrecode::Type != ThisPtrRetBufPrecode::Type);

// Verify that the base type for each precode fits into each specific precode type
static_assert(sizeof(Precode) <= sizeof(PInvokeImportPrecode));
static_assert(sizeof(Precode) <= sizeof(FixupPrecode));
static_assert(sizeof(Precode) <= sizeof(ThisPtrRetBufPrecode));

#ifdef FEATURE_INTERPRETER
// we are allocating InterpreterPrecode in the interleaved StubPrecodeHeap
// (in Precode::AllocateInterpreterPrecode)
// and so we need it to fit the data into the StubPrecode::CodeSize
static_assert(sizeof(InterpreterPrecodeData) <= StubPrecode::CodeSize);
#endif // FEATURE_INTERPRETER

// A summary of the precode layout for diagnostic purposes
struct PrecodeMachineDescriptor
{
    uint32_t StubCodePageSize;
    uint8_t InvalidPrecodeType;
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    uint8_t PInvokeImportPrecodeType;
#endif

#ifdef HAS_FIXUP_PRECODE
    uint8_t FixupPrecodeType;
    uint8_t FixupCodeOffset;
    uint8_t FixupStubPrecodeSize;
    BYTE FixupBytes[FixupPrecode::CodeSize];
    BYTE FixupIgnoredBytes[FixupPrecode::CodeSize];
#endif // HAS_FIXUP_PRECODE

    uint8_t StubPrecodeSize;
    uint8_t StubPrecodeType;

    BYTE StubBytes[StubPrecode::CodeSize];
    BYTE StubIgnoredBytes[StubPrecode::CodeSize];

#ifdef HAS_THISPTR_RETBUF_PRECODE
    uint8_t ThisPointerRetBufPrecodeType;
#endif

#ifdef FEATURE_INTERPRETER
    uint8_t InterpreterPrecodeType;
#endif
#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS
    uint8_t DynamicHelperPrecodeType;
#endif
    uint8_t UMEntryPrecodeType;

public:
    PrecodeMachineDescriptor() = default;
    PrecodeMachineDescriptor(const PrecodeMachineDescriptor&) = delete;
    PrecodeMachineDescriptor& operator=(const PrecodeMachineDescriptor&) = delete;
    static void Init(PrecodeMachineDescriptor* dest);
};

extern InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;
#ifdef HAS_FIXUP_PRECODE
extern InterleavedLoaderHeapConfig s_fixupStubPrecodeHeapConfig;
#endif

#endif // FEATURE_PORTABLE_ENTRYPOINTS

#endif // __PRECODE_H__
