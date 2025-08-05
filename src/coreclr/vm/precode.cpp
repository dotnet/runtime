// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// precode.cpp
//

//
// Stub that runs before the actual native code
//


#include "common.h"
#include "dllimportcallback.h"
#include "../interpreter/interpretershared.h"

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

InterleavedLoaderHeapConfig s_stubPrecodeHeapConfig;
#ifdef HAS_FIXUP_PRECODE
InterleavedLoaderHeapConfig s_fixupStubPrecodeHeapConfig;
#endif

//==========================================================================================
// class Precode
//==========================================================================================
BOOL Precode::IsValidType(PrecodeType t)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    switch (t) {
    case PRECODE_STUB:
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    case PRECODE_PINVOKE_IMPORT:
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
#endif // HAS_THISPTR_RETBUF_PRECODE
#ifdef FEATURE_INTERPRETER
    case PRECODE_INTERPRETER:
#endif // FEATURE_INTERPRETER
    case PRECODE_UMENTRY_THUNK:
        return TRUE;
    default:
        return FALSE;
    }
}

UMEntryThunk* Precode::AsUMEntryThunk()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return dac_cast<PTR_UMEntryThunk>(this);
}

SIZE_T Precode::SizeOf(PrecodeType t)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    switch (t)
    {
    case PRECODE_STUB:
        return sizeof(StubPrecode);
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    case PRECODE_PINVOKE_IMPORT:
        return sizeof(PInvokeImportPrecode);
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        return sizeof(FixupPrecode);
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        return sizeof(ThisPtrRetBufPrecode);
#endif // HAS_THISPTR_RETBUF_PRECODE
#ifdef FEATURE_INTERPRETER
    case PRECODE_INTERPRETER:
        return sizeof(InterpreterPrecode);
#endif // FEATURE_INTERPRETER

    default:
        UnexpectedPrecodeType("Precode::SizeOf", t);
        break;
    }
    return 0;
}

// Note: This is immediate target of the precode. It does not follow jump stub if there is one.
PCODE Precode::GetTarget()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    PCODE target = 0;

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
    case PRECODE_STUB:
        target = AsStubPrecode()->GetTarget();
        break;
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        target = AsFixupPrecode()->GetTarget();
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        target = AsThisPtrRetBufPrecode()->GetTarget();
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        UnexpectedPrecodeType("Precode::GetTarget", precodeType);
        break;
    }
    return target;
}

MethodDesc* Precode::GetMethodDesc(BOOL fSpeculative /*= FALSE*/)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    TADDR pMD = (TADDR)NULL;

    PrecodeType precodeType = GetType();
#ifdef TARGET_WASM
    pMD = *(TADDR*)(m_data + OFFSETOF_PRECODE_MD);
#else
    switch (precodeType)
    {
    case PRECODE_STUB:
        pMD = AsStubPrecode()->GetMethodDesc();
        break;
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    case PRECODE_PINVOKE_IMPORT:
        pMD = AsPInvokeImportPrecode()->GetMethodDesc();
        break;
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        pMD = AsFixupPrecode()->GetMethodDesc();
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        pMD = AsThisPtrRetBufPrecode()->GetMethodDesc();
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_UMENTRY_THUNK:
        return NULL;
        break;
#ifdef FEATURE_INTERPRETER
    case PRECODE_INTERPRETER:
        pMD = AsInterpreterPrecode()->GetMethodDesc();
        break;
#endif // FEATURE_INTERPRETER

    default:
        break;
    }
#endif // TARGET_WASM

    if (pMD == (TADDR)NULL)
    {
        if (fSpeculative)
            return NULL;
        else
            UnexpectedPrecodeType("Precode::GetMethodDesc", precodeType);
    }

    // GetMethodDesc() on platform specific precode types returns TADDR. It should return
    // PTR_MethodDesc instead. It is a workaround to resolve cyclic dependency between headers.
    // Once we headers factoring of headers cleaned up, we should be able to get rid of it.
    return (PTR_MethodDesc)pMD;
}

#ifdef FEATURE_INTERPRETER
TADDR InterpreterPrecode::GetMethodDesc()
{
    LIMITED_METHOD_DAC_CONTRACT;

    InterpByteCodeStart* pInterpreterCode = dac_cast<PTR_InterpByteCodeStart>(GetData()->ByteCodeAddr);
    return (TADDR)pInterpreterCode->Method->methodHnd;
}
#endif // FEATURE_INTERPRETER

BOOL Precode::IsPointingToPrestub(PCODE target)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsPointingTo(target, GetPreStubEntryPoint()))
        return TRUE;

#ifdef HAS_FIXUP_PRECODE
    if (IsPointingTo(target, ((PCODE)this + FixupPrecode::FixupCodeOffset)))
        return TRUE;
#endif

    return FALSE;
}

#ifndef DACCESS_COMPILE

void FlushCacheForDynamicMappedStub(void* code, SIZE_T size)
{
#ifdef FEATURE_CORECLR_FLUSH_INSTRUCTION_CACHE_TO_PROTECT_STUB_READS
    // While the allocation of a stub will use a memory mapping technique, and not actually write to the set of instructions,
    // the set of instructions in the stub has non-barrier protected reads from the StubPrecodeData structure. In order to protect those
    // reads we would either need barrier instructions in the stub, or we need to ensure that the precode is flushed in the instruction cache
    // which will have the side effect of ensuring that the reads within the stub will happen *after* the writes to the StubPrecodeData structure which
    // happened in the Init routine above.
    ClrFlushInstructionCache(code, size);
#endif // FEATURE_CORECLR_FLUSH_INSTRUCTION_CACHE_TO_PROTECT_STUB_READS
}

#ifdef FEATURE_INTERPRETER
InterpreterPrecode* Precode::AllocateInterpreterPrecode(PCODE byteCode,
                                                        LoaderAllocator *  pLoaderAllocator,
                                                        AllocMemTracker *  pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    InterpreterPrecode* pPrecode = (InterpreterPrecode*)pamTracker->Track(pLoaderAllocator->GetNewStubPrecodeHeap()->AllocStub());
    pPrecode->Init(pPrecode, byteCode);

    FlushCacheForDynamicMappedStub(pPrecode, sizeof(InterpreterPrecode));

#ifdef FEATURE_PERFMAP
    PerfMap::LogStubs(__FUNCTION__, "UMEntryThunk", (PCODE)pPrecode, sizeof(InterpreterPrecode), PerfMapStubType::IndividualWithinBlock);
#endif
    return pPrecode;
}
#endif // FEATURE_INTERPRETER

Precode* Precode::Allocate(PrecodeType t, MethodDesc* pMD,
                           LoaderAllocator *  pLoaderAllocator,
                           AllocMemTracker *  pamTracker)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Precode* pPrecode = NULL;

    if (t == PRECODE_FIXUP)
    {
        pPrecode = (Precode*)pamTracker->Track(pLoaderAllocator->GetFixupPrecodeHeap()->AllocStub());
        pPrecode->Init(pPrecode, t, pMD, pLoaderAllocator);

        // PRECODE_FIXUP is the most common precode type, and we are able to optimize creation of it in a special
        // way that allows us to avoid the need for FlushInstructionCache when the precode is created. Notably,
        // Before we map the memory for the executable section of the FixupPrecodeThunks into the process, we fill
        // the Target with pointers so that Target points at the second portion of the FixupPrecodeThunk. So when
        // we map the executable memory in, we get a natural FlushInstructionCache for that scenario. So there is
        // never a case where we need to worry about the Target field being set to NULL. Then we either are able
        // to see the actual final Target (which doesn't require any further synchronization), or we'll hit the memory
        // barrier in the second portion of the FixupPrecodeThunk and find that the MethodDesc/PrecodeFixupThunk are
        // properly set. See FixupPrecode::GenerateDataPage for the code to fill in the target.
#ifdef FEATURE_PERFMAP
        PerfMap::LogStubs(__FUNCTION__, "FixupPrecode", (PCODE)pPrecode, sizeof(FixupPrecode), PerfMapStubType::IndividualWithinBlock);
#endif
    }
#ifdef HAS_THISPTR_RETBUF_PRECODE
    else if (t == PRECODE_THISPTR_RETBUF)
    {
        ThisPtrRetBufPrecode* pThisPtrRetBufPrecode = (ThisPtrRetBufPrecode*)pamTracker->Track(pLoaderAllocator->GetNewStubPrecodeHeap()->AllocStub());
        ThisPtrRetBufPrecodeData *pData = (ThisPtrRetBufPrecodeData*)pamTracker->Track(pLoaderAllocator->GetLowFrequencyHeap()->AllocMem(S_SIZE_T(sizeof(ThisPtrRetBufPrecodeData))));
        pThisPtrRetBufPrecode->Init(pData, pMD, pLoaderAllocator);
        pPrecode = (Precode*)pThisPtrRetBufPrecode;

        FlushCacheForDynamicMappedStub(pPrecode, sizeof(ThisPtrRetBufPrecode));

#ifdef FEATURE_PERFMAP
        PerfMap::LogStubs(__FUNCTION__, "ThisPtrRetBuf", (PCODE)pPrecode, sizeof(ThisPtrRetBufPrecodeData), PerfMapStubType::IndividualWithinBlock);
#endif
        }
#endif // HAS_THISPTR_RETBUF_PRECODE
    else
    {
        _ASSERTE(t == PRECODE_STUB || t == PRECODE_PINVOKE_IMPORT);
        pPrecode = (Precode*)pamTracker->Track(pLoaderAllocator->GetNewStubPrecodeHeap()->AllocStub());
        pPrecode->Init(pPrecode, t, pMD, pLoaderAllocator);

        FlushCacheForDynamicMappedStub(pPrecode, sizeof(StubPrecode));

#ifdef FEATURE_PERFMAP
        PerfMap::LogStubs(__FUNCTION__, t == PRECODE_STUB ? "StubPrecode" : "PInvokeImportPrecode", (PCODE)pPrecode, sizeof(StubPrecode), PerfMapStubType::IndividualWithinBlock);
#endif
    }

    return pPrecode;
}

void Precode::Init(Precode* pPrecodeRX, PrecodeType t, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_WASM
    m_data[OFFSETOF_PRECODE_TYPE] = t;
    *(TADDR*)(m_data + OFFSETOF_PRECODE_MD) = (TADDR)pMD;
#else
    switch (t) {
    case PRECODE_STUB:
        ((StubPrecode*)this)->Init((StubPrecode*)pPrecodeRX, (TADDR)pMD, pLoaderAllocator);
        break;
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    case PRECODE_PINVOKE_IMPORT:
        ((PInvokeImportPrecode*)this)->Init((PInvokeImportPrecode*)pPrecodeRX, pMD, pLoaderAllocator);
        break;
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        ((FixupPrecode*)this)->Init((FixupPrecode*)pPrecodeRX, pMD, pLoaderAllocator);
        break;
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        ((ThisPtrRetBufPrecode*)this)->Init(pMD, pLoaderAllocator);
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE
    default:
        UnexpectedPrecodeType("Precode::Init", t);
        break;
    }
#endif

    _ASSERTE(IsValidType(GetType()));
}

void Precode::ResetTargetInterlocked()
{
    WRAPPER_NO_CONTRACT;

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
        case PRECODE_STUB:
            AsStubPrecode()->ResetTargetInterlocked();
            break;

#ifdef HAS_FIXUP_PRECODE
        case PRECODE_FIXUP:
            AsFixupPrecode()->ResetTargetInterlocked();
            break;
#endif // HAS_FIXUP_PRECODE

        default:
            UnexpectedPrecodeType("Precode::ResetTargetInterlocked", precodeType);
            break;
    }

    // Although executable code is modified on x86/x64, a FlushInstructionCache() is not necessary on those platforms due to the
    // interlocked operation above (see ClrFlushInstructionCache())
}

BOOL Precode::SetTargetInterlocked(PCODE target, BOOL fOnlyRedirectFromPrestub)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!IsPointingToPrestub(target));

    PCODE expected = GetTarget();
    BOOL ret = FALSE;

    if (fOnlyRedirectFromPrestub && !IsPointingToPrestub(expected))
        return FALSE;

    PrecodeType precodeType = GetType();
    switch (precodeType)
    {
    case PRECODE_STUB:
        ret = AsStubPrecode()->SetTargetInterlocked(target, expected);
        break;

#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
        ret = AsFixupPrecode()->SetTargetInterlocked(target, expected);
        break;
#endif // HAS_FIXUP_PRECODE

#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
        ret = AsThisPtrRetBufPrecode()->SetTargetInterlocked(target, expected);
        break;
#endif // HAS_THISPTR_RETBUF_PRECODE

    default:
        UnexpectedPrecodeType("Precode::SetTargetInterlocked", precodeType);
        break;
    }

    // Although executable code is modified on x86/x64, a FlushInstructionCache() is not necessary on those platforms due to the
    // interlocked operation above (see ClrFlushInstructionCache())

    return ret;
}

void Precode::Reset()
{
    WRAPPER_NO_CONTRACT;

    MethodDesc* pMD = GetMethodDesc();

    PrecodeType t = GetType();
    SIZE_T size = Precode::SizeOf(t);

    switch (t)
    {
    case PRECODE_STUB:
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    case PRECODE_PINVOKE_IMPORT:
#endif // HAS_PINVOKE_IMPORT_PRECODE
#ifdef HAS_FIXUP_PRECODE
    case PRECODE_FIXUP:
#endif // HAS_FIXUP_PRECODE
#ifdef HAS_THISPTR_RETBUF_PRECODE
    case PRECODE_THISPTR_RETBUF:
#endif // HAS_THISPTR_RETBUF_PRECODE
        Init(this, t, pMD, pMD->GetLoaderAllocator());
        break;

    default:
        _ASSERTE(!"Unexpected precode type");
        JIT_FailFast();
        break;
    }
}

#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
void Precode::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    PrecodeType t = GetType();

    DacEnumMemoryRegion(GetStart(), SizeOf(t));
}
#endif

#ifdef HAS_FIXUP_PRECODE

#ifdef DACCESS_COMPILE
void FixupPrecode::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DacEnumMemoryRegion(dac_cast<TADDR>(this), sizeof(FixupPrecode));
    DacEnumMemoryRegion(dac_cast<TADDR>(GetData()), sizeof(FixupPrecodeData));
}
#endif // DACCESS_COMPILE

#endif // HAS_FIXUP_PRECODE

#ifndef DACCESS_COMPILE

#ifdef HAS_THISPTR_RETBUF_PRECODE
extern "C" void ThisPtrRetBufPrecodeWorker();
void ThisPtrRetBufPrecode::Init(ThisPtrRetBufPrecodeData* pPrecodeData, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    StubPrecode::Init(this, dac_cast<TADDR>(pPrecodeData), pLoaderAllocator, ThisPtrRetBufPrecode::Type, (TADDR)ThisPtrRetBufPrecodeWorker);
    pPrecodeData->MethodDesc = pMD;
    pPrecodeData->Target = GetPreStubEntryPoint();
}

void ThisPtrRetBufPrecode::Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    ThisPtrRetBufPrecodeData* pPrecodeData = GetData();
    pPrecodeData->MethodDesc = pMD;
    pPrecodeData->Target = GetPreStubEntryPoint();
}
#endif // HAS_THISPTR_RETBUF_PRECODE

void StubPrecode::Init(StubPrecode* pPrecodeRX, TADDR secretParam, LoaderAllocator *pLoaderAllocator /* = NULL */,
    TADDR type /* = StubPrecode::Type */, TADDR target /* = NULL */)
{
    WRAPPER_NO_CONTRACT;

    StubPrecodeData *pStubData = GetData();

    if (pLoaderAllocator != NULL)
    {
        // Use pMD == NULL in all precode initialization methods to allocate the initial jump stub in non-dynamic heap
        // that has the same lifetime like as the precode itself
        if (target == (TADDR)NULL)
            target = GetPreStubEntryPoint();
        pStubData->Target = target;
    }

    pStubData->SecretParam = secretParam;
    pStubData->Type = type;
}

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    #define ENUM_PAGE_SIZE(size) \
        extern "C" void StubPrecodeCode##size(); \
        extern "C" void StubPrecodeCode##size##_End();
    ENUM_PAGE_SIZES
    #undef ENUM_PAGE_SIZE
#else
extern "C" void StubPrecodeCode();
extern "C" void StubPrecodeCode_End();
#endif

#ifdef TARGET_X86
extern "C" size_t StubPrecodeCode_MethodDesc_Offset;
extern "C" size_t StubPrecodeCode_Target_Offset;

#define SYMBOL_VALUE(name) ((size_t)&name)

#endif

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
static void (*StubPrecodeCode)();
static void (*StubPrecodeCode_End)();
#endif

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE
extern "C" void StubPrecodeCodeTemplate();
#else
#define StubPrecodeCodeTemplate NULL
#endif

void StubPrecode::StaticInitialize()
{
#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    #define ENUM_PAGE_SIZE(size) \
        case size: \
            StubPrecodeCode = StubPrecodeCode##size; \
            StubPrecodeCode_End = StubPrecodeCode##size##_End; \
            _ASSERTE((SIZE_T)((BYTE*)StubPrecodeCode##size##_End - (BYTE*)StubPrecodeCode##size) <= StubPrecode::CodeSize); \
            break;

    int pageSize = GetStubCodePageSize();
    switch (pageSize)
    {
        ENUM_PAGE_SIZES
        default:
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Unsupported OS page size"));
    }

    if (StubPrecodeCodeTemplate != NULL && pageSize != 0x4000)
    {
        // This should fail if the template is used on a platform which doesn't support the supported page size for templates
        ThrowHR(COR_E_EXECUTIONENGINE);
    }

    #undef ENUM_PAGE_SIZE
#elif defined(TARGET_WASM)
    // StubPrecode is not implemented on WASM
#else
    _ASSERTE((SIZE_T)((BYTE*)StubPrecodeCode_End - (BYTE*)StubPrecodeCode) <= StubPrecode::CodeSize);
#endif
    _ASSERTE(IsStubPrecodeByASM_DAC((PCODE)StubPrecodeCode));
    if (StubPrecodeCodeTemplate != NULL)
    {
        _ASSERTE(IsStubPrecodeByASM_DAC((PCODE)StubPrecodeCodeTemplate));
    }

    InitializeLoaderHeapConfig(&s_stubPrecodeHeapConfig, StubPrecode::CodeSize, (void*)StubPrecodeCodeTemplate, StubPrecode::GenerateCodePage, NULL);
}

void StubPrecode::GenerateCodePage(uint8_t* pageBase, uint8_t* pageBaseRX, size_t pageSize)
{
#ifndef TARGET_WASM
    int totalCodeSize = (int)(pageSize / StubPrecode::CodeSize) * StubPrecode::CodeSize;
#ifdef TARGET_X86
    for (int i = 0; i < totalCodeSize; i += StubPrecode::CodeSize)
    {
        memcpy(pageBase + i, (const void*)StubPrecodeCode, (uint8_t*)StubPrecodeCode_End - (uint8_t*)StubPrecodeCode);

        uint8_t* pTargetSlot = pageBaseRX + i + pageSize + offsetof(StubPrecodeData, Target);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(StubPrecodeCode_Target_Offset)) = pTargetSlot;

        BYTE* pMethodDescSlot = pageBaseRX + i + pageSize + offsetof(StubPrecodeData, SecretParam);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(StubPrecodeCode_MethodDesc_Offset)) = pMethodDescSlot;
    }
#else // TARGET_X86
    FillStubCodePage(pageBase, (const void*)PCODEToPINSTR((PCODE)StubPrecodeCode), StubPrecode::CodeSize, pageSize);
#endif // TARGET_X86
#ifdef _DEBUG
    for (int i = 0; i < totalCodeSize; i += StubPrecode::CodeSize)
    {
        _ASSERTE(StubPrecode::IsStubPrecodeByASM((PCODE)(pageBaseRX + i)));
        _ASSERTE(StubPrecode::IsStubPrecodeByASM_DAC((PCODE)(pageBaseRX + i)));
    }
#endif // _DEBUG
#endif // TARGET_WASM
}

BOOL StubPrecode::IsStubPrecodeByASM(PCODE addr)
{
    BYTE *pInstr = (BYTE*)PCODEToPINSTR(addr);
#ifdef TARGET_X86
    return *pInstr == *(BYTE*)(StubPrecodeCode) &&
            *(DWORD*)(pInstr + SYMBOL_VALUE(StubPrecodeCode_MethodDesc_Offset)) == (DWORD)(pInstr + GetStubCodePageSize() + offsetof(StubPrecodeData, SecretParam)) &&
            *(WORD*)(pInstr + 5) == *(WORD*)((BYTE*)StubPrecodeCode + 5) &&
            *(DWORD*)(pInstr + SYMBOL_VALUE(StubPrecodeCode_Target_Offset)) == (DWORD)(pInstr + GetStubCodePageSize() + offsetof(StubPrecodeData, Target));
#else // TARGET_X86
    BYTE *pTemplateInstr = (BYTE*)PCODEToPINSTR((PCODE)StubPrecodeCode);
    BYTE *pTemplateInstrEnd = (BYTE*)PCODEToPINSTR((PCODE)StubPrecodeCode_End);
    while ((pTemplateInstr < pTemplateInstrEnd) && (*pInstr == *pTemplateInstr))
    {
        pInstr++;
        pTemplateInstr++;
    }

    return pTemplateInstr == pTemplateInstrEnd;
#endif // TARGET_X86
}

#ifdef FEATURE_INTERPRETER
void InterpreterPrecode::Init(InterpreterPrecode* pPrecodeRX, TADDR byteCodeAddr)
{
    WRAPPER_NO_CONTRACT;
    InterpreterPrecodeData *pStubData = GetData();

    pStubData->Target = (PCODE)InterpreterStub;
    pStubData->ByteCodeAddr = byteCodeAddr;
    pStubData->Type = InterpreterPrecode::Type;
}
#endif // FEATURE_INTERPRETER

#ifdef HAS_PINVOKE_IMPORT_PRECODE

void PInvokeImportPrecode::Init(PInvokeImportPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;
    StubPrecode::Init(pPrecodeRX, (TADDR)pMD, pLoaderAllocator, PInvokeImportPrecode::Type, GetEEFuncEntryPoint(PInvokeImportThunk));
}

#endif // HAS_PINVOKE_IMPORT_PRECODE

#ifdef HAS_FIXUP_PRECODE
void FixupPrecode::Init(FixupPrecode* pPrecodeRX, MethodDesc* pMD, LoaderAllocator *pLoaderAllocator)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(pPrecodeRX == this);

    FixupPrecodeData *pData = GetData();
    pData->MethodDesc = pMD;

    _ASSERTE(GetMethodDesc() == (TADDR)pMD);

    pData->PrecodeFixupThunk = GetPreStubEntryPoint();
    VolatileStore(&pData->Target, (PCODE)pPrecodeRX + FixupPrecode::FixupCodeOffset);
}

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    #define ENUM_PAGE_SIZE(size) \
        extern "C" void FixupPrecodeCode##size(); \
        extern "C" void FixupPrecodeCode##size##_End();
    ENUM_PAGE_SIZES
    #undef ENUM_PAGE_SIZE
#else
extern "C" void FixupPrecodeCode();
extern "C" void FixupPrecodeCode_End();
#endif

#ifdef TARGET_X86
extern "C" size_t FixupPrecodeCode_MethodDesc_Offset;
extern "C" size_t FixupPrecodeCode_Target_Offset;
extern "C" size_t FixupPrecodeCode_PrecodeFixupThunk_Offset;
#endif

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
static void (*FixupPrecodeCode)();
static void (*FixupPrecodeCode_End)();
#endif

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE
extern "C" void FixupPrecodeCodeTemplate();
#else
#define FixupPrecodeCodeTemplate NULL
#endif

void FixupPrecode::StaticInitialize()
{
#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    #define ENUM_PAGE_SIZE(size) \
        case size: \
            FixupPrecodeCode = FixupPrecodeCode##size; \
            FixupPrecodeCode_End = FixupPrecodeCode##size##_End; \
            _ASSERTE((SIZE_T)((BYTE*)FixupPrecodeCode##size##_End - (BYTE*)FixupPrecodeCode##size) <= FixupPrecode::CodeSize); \
            break;

    int pageSize = GetStubCodePageSize();

    switch (pageSize)
    {
        ENUM_PAGE_SIZES
        default:
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Unsupported OS page size"));
    }
    #undef ENUM_PAGE_SIZE

    if (FixupPrecodeCodeTemplate != NULL && pageSize != 0x4000)
    {
        // This should fail if the template is used on a platform which doesn't support the supported page size for templates
        ThrowHR(COR_E_EXECUTIONENGINE);
    }
#elif defined(TARGET_WASM)
    // FixupPrecode is not implemented on WASM
#else
    _ASSERTE((SIZE_T)((BYTE*)FixupPrecodeCode_End - (BYTE*)FixupPrecodeCode) <= FixupPrecode::CodeSize);
#endif
    _ASSERTE(IsFixupPrecodeByASM_DAC((PCODE)FixupPrecodeCode));
    if (FixupPrecodeCodeTemplate != NULL)
    {
        _ASSERTE(IsFixupPrecodeByASM_DAC((PCODE)FixupPrecodeCodeTemplate));
    }
    InitializeLoaderHeapConfig(&s_fixupStubPrecodeHeapConfig, FixupPrecode::CodeSize, (void*)FixupPrecodeCodeTemplate, FixupPrecode::GenerateCodePage, FixupPrecode::GenerateDataPage);
}

void FixupPrecode::GenerateDataPage(uint8_t* pageBase, size_t pageSize)
{
#ifndef TARGET_WASM
    // Fill in the data page such that the target of the fixup precode starts as initialized to point
    // to the start of the precode itself, so that before the memory for the precode is initialized,
    // the precode is in a state where it will loop forever.
    // When initializing the precode to have the MethodDesc/Prestub target, the write to update the Target
    // to go through the code which passes the extra MethodDesc argument, will be done with a VolatileStore
    // such that both the MethodDesc and the PrecodeFixupThunk are updated before the Target is updated to
    // make it possible to hit that code. Finally, the FixupPrecode assembly logic will have a load memory
    // barrier, which will match up with that store.
    //
    // Finally, to make this all work, we ensure that this data page generation function is called
    // BEFORE the code page is ever mapped into memory, so that it cannot be speculatively executed
    // before this logic completes.
    int totalCodeSize = (int)((pageSize / FixupPrecode::CodeSize) * FixupPrecode::CodeSize);
    for (int i = 0; i < totalCodeSize; i += FixupPrecode::CodeSize)
    {
        PCODE* ppTargetSlot = (PCODE*)(pageBase + i + offsetof(FixupPrecodeData, Target));
        *ppTargetSlot = ((Precode*)(pageBase - pageSize + i))->GetEntryPoint();
    }
#endif // !TARGET_WASM
}

void FixupPrecode::GenerateCodePage(uint8_t* pageBase, uint8_t* pageBaseRX, size_t pageSize)
{
#ifndef TARGET_WASM
    int totalCodeSize = (int)((pageSize / FixupPrecode::CodeSize) * FixupPrecode::CodeSize);
#ifdef TARGET_X86

    for (int i = 0; i < totalCodeSize; i += FixupPrecode::CodeSize)
    {
        memcpy(pageBase + i, (const void*)FixupPrecodeCode, FixupPrecode::CodeSize);
        uint8_t* pTargetSlot = pageBaseRX + i + pageSize + offsetof(FixupPrecodeData, Target);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(FixupPrecodeCode_Target_Offset)) = pTargetSlot;

        BYTE* pMethodDescSlot = pageBaseRX + i + pageSize + offsetof(FixupPrecodeData, MethodDesc);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(FixupPrecodeCode_MethodDesc_Offset)) = pMethodDescSlot;

        BYTE* pPrecodeFixupThunkSlot = pageBaseRX + i + pageSize + offsetof(FixupPrecodeData, PrecodeFixupThunk);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(FixupPrecodeCode_PrecodeFixupThunk_Offset)) = pPrecodeFixupThunkSlot;
    }
#else // TARGET_X86
    FillStubCodePage(pageBase, (const void*)PCODEToPINSTR((PCODE)FixupPrecodeCode), FixupPrecode::CodeSize, pageSize);
#endif // TARGET_X86
#ifdef _DEBUG
    for (int i = 0; i < totalCodeSize; i += FixupPrecode::CodeSize)
    {
        _ASSERTE(FixupPrecode::IsFixupPrecodeByASM((PCODE)(pageBaseRX + i)));
        _ASSERTE(FixupPrecode::IsFixupPrecodeByASM_DAC((PCODE)(pageBaseRX + i)));
    }
#endif // _DEBUG
#endif // !TARGET_WASM
}

BOOL FixupPrecode::IsFixupPrecodeByASM(PCODE addr)
{
    BYTE *pInstr = (BYTE*)PCODEToPINSTR(addr);
#ifdef TARGET_X86
    return
        *(WORD*)(pInstr) == *(WORD*)(FixupPrecodeCode) &&
        *(DWORD*)(pInstr + SYMBOL_VALUE(FixupPrecodeCode_Target_Offset)) == (DWORD)(pInstr + GetStubCodePageSize() + offsetof(FixupPrecodeData, Target)) &&
        *(pInstr + 6) == *((BYTE*)FixupPrecodeCode + 6) &&
        *(DWORD*)(pInstr + SYMBOL_VALUE(FixupPrecodeCode_MethodDesc_Offset)) == (DWORD)(pInstr + GetStubCodePageSize() + offsetof(FixupPrecodeData, MethodDesc)) &&
        *(WORD*)(pInstr + 11) == *(WORD*)((BYTE*)FixupPrecodeCode + 11) &&
        *(DWORD*)(pInstr + SYMBOL_VALUE(FixupPrecodeCode_PrecodeFixupThunk_Offset)) == (DWORD)(pInstr + GetStubCodePageSize() + offsetof(FixupPrecodeData, PrecodeFixupThunk));
#else // TARGET_X86
    BYTE *pTemplateInstr = (BYTE*)PCODEToPINSTR((PCODE)FixupPrecodeCode);
    BYTE *pTemplateInstrEnd = (BYTE*)PCODEToPINSTR((PCODE)FixupPrecodeCode_End);
    while ((pTemplateInstr < pTemplateInstrEnd) && (*pInstr == *pTemplateInstr))
    {
        pInstr++;
        pTemplateInstr++;
    }

    return pTemplateInstr == pTemplateInstrEnd;
#endif // TARGET_X86
}

#endif // HAS_FIXUP_PRECODE

BOOL DoesSlotCallPrestub(PCODE pCode)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(pCode != GetPreStubEntryPoint());
    } CONTRACTL_END;

    TADDR pInstr = dac_cast<TADDR>(PCODEToPINSTR(pCode));

    if (!IS_ALIGNED(pInstr, PRECODE_ALIGNMENT))
    {
        return FALSE;
    }

    //FixupPrecode
#if defined(HAS_FIXUP_PRECODE)
    if (FixupPrecode::IsFixupPrecodeByASM(pCode))
    {
        PCODE pTarget = dac_cast<PTR_FixupPrecode>(pInstr)->GetTarget();

        return pTarget == PCODEToPINSTR(pCode) + FixupPrecode::FixupCodeOffset;
    }
#endif

    // StubPrecode
    if (StubPrecode::IsStubPrecodeByASM(pCode))
    {
        pCode = dac_cast<PTR_StubPrecode>(pInstr)->GetTarget();
        return pCode == GetPreStubEntryPoint();
    }

    return FALSE;
}

void PrecodeMachineDescriptor::Init(PrecodeMachineDescriptor *dest)
{
    dest->InvalidPrecodeType = PRECODE_INVALID;
#ifdef HAS_PINVOKE_IMPORT_PRECODE
    dest->PInvokeImportPrecodeType = PRECODE_PINVOKE_IMPORT;
#endif

#ifdef HAS_FIXUP_PRECODE
    dest->FixupPrecodeType = PRECODE_FIXUP;
    dest->FixupCodeOffset = FixupPrecode::FixupCodeOffset;
    dest->FixupStubPrecodeSize = FixupPrecode::CodeSize;

    memset(dest->FixupBytes, 0, FixupPrecode::CodeSize);
    memset(dest->FixupIgnoredBytes, 0, FixupPrecode::CodeSize);

    BYTE *pTemplateInstr;
    BYTE *pTemplateInstrEnd;
    uint8_t bytesMeaningful;
#ifdef TARGET_X86
    memset(dest->FixupIgnoredBytes + SYMBOL_VALUE(FixupPrecodeCode_Target_Offset), 1, 4);
    memset(dest->FixupIgnoredBytes + SYMBOL_VALUE(FixupPrecodeCode_MethodDesc_Offset), 1, 4);
    memset(dest->FixupIgnoredBytes + SYMBOL_VALUE(FixupPrecodeCode_PrecodeFixupThunk_Offset), 1, 4);
#endif // TARGET_X86
    pTemplateInstr = (BYTE*)PCODEToPINSTR((PCODE)FixupPrecodeCode);
    pTemplateInstrEnd = (BYTE*)PCODEToPINSTR((PCODE)FixupPrecodeCode_End);
    bytesMeaningful = (uint8_t)(pTemplateInstrEnd - pTemplateInstr);
    memcpy(dest->FixupBytes, pTemplateInstr, bytesMeaningful);
    memset(dest->FixupIgnoredBytes + bytesMeaningful, 1, FixupPrecode::CodeSize - bytesMeaningful);
#endif // HAS_FIXUP_PRECODE

    dest->StubPrecodeSize = StubPrecode::CodeSize;
    dest->StubPrecodeType = PRECODE_STUB;

    memset(dest->StubBytes, 0, StubPrecode::CodeSize);
    memset(dest->StubIgnoredBytes, 0, StubPrecode::CodeSize);
#ifdef TARGET_X86
    memset(dest->StubIgnoredBytes + SYMBOL_VALUE(StubPrecodeCode_Target_Offset), 1, 4);
    memset(dest->StubIgnoredBytes + SYMBOL_VALUE(StubPrecodeCode_MethodDesc_Offset), 1, 4);
#endif // TARGET_X86
    pTemplateInstr = (BYTE*)PCODEToPINSTR((PCODE)StubPrecodeCode);
    pTemplateInstrEnd = (BYTE*)PCODEToPINSTR((PCODE)StubPrecodeCode_End);
    bytesMeaningful = (uint8_t)(pTemplateInstrEnd - pTemplateInstr);
    memcpy(dest->StubBytes, pTemplateInstr, bytesMeaningful);
    memset(dest->StubIgnoredBytes + bytesMeaningful, 1, StubPrecode::CodeSize - bytesMeaningful);

#ifdef HAS_THISPTR_RETBUF_PRECODE
    dest->ThisPointerRetBufPrecodeType = PRECODE_THISPTR_RETBUF;
#endif

#ifdef FEATURE_INTERPRETER
    dest->InterpreterPrecodeType = PRECODE_INTERPRETER;
#endif
#ifdef FEATURE_STUBPRECODE_DYNAMIC_HELPERS
    dest->DynamicHelperPrecodeType = PRECODE_DYNAMIC_HELPERS;
#endif
    dest->UMEntryPrecodeType = PRECODE_UMENTRY_THUNK;

    dest->StubCodePageSize = GetStubCodePageSize();
}

#endif // !DACCESS_COMPILE

#include <cdacplatformmetadata.hpp>

#ifdef HAS_FIXUP_PRECODE
#ifndef DACCESS_COMPILE
BOOL FixupPrecode::IsFixupPrecodeByASM_DAC(PCODE addr)
#else
BOOL FixupPrecode::IsFixupPrecodeByASM(PCODE addr)
#endif
{
    LIMITED_METHOD_DAC_CONTRACT;
    PrecodeMachineDescriptor *precodeDescriptor = &(&g_cdacPlatformMetadata)->precode;
    PTR_BYTE pInstr = dac_cast<PTR_BYTE>(PCODEToPINSTR(addr));
    for (int i = 0; i < precodeDescriptor->FixupStubPrecodeSize; i++)
    {
        if (precodeDescriptor->FixupIgnoredBytes[i] == 0)
        {
            if (precodeDescriptor->FixupBytes[i] != *pInstr)
            {
                return FALSE;
            }
        }
        pInstr++;
    }

    return TRUE;
}
#endif // HAS_FIXUP_PRECODE

#ifndef DACCESS_COMPILE
BOOL StubPrecode::IsStubPrecodeByASM_DAC(PCODE addr)
#else
BOOL StubPrecode::IsStubPrecodeByASM(PCODE addr)
#endif
{
    LIMITED_METHOD_DAC_CONTRACT;
    PrecodeMachineDescriptor *precodeDescriptor = &(&g_cdacPlatformMetadata)->precode;
    PTR_BYTE pInstr = dac_cast<PTR_BYTE>(PCODEToPINSTR(addr));
    for (int i = 0; i < precodeDescriptor->StubPrecodeSize; i++)
    {
        if (precodeDescriptor->StubIgnoredBytes[i] == 0)
        {
            if (precodeDescriptor->StubBytes[i] != *pInstr)
            {
                return FALSE;
            }
        }
        pInstr++;
    }

    return TRUE;
}
