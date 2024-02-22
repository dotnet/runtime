// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef FEATURE_NATIVEAOT
#include "RhConfig.h"
#endif

#include "gcinfodecoder.h"

#ifdef USE_GC_INFO_DECODER

#ifndef CHECK_APP_DOMAIN
#define CHECK_APP_DOMAIN    0
#endif

#ifndef GCINFODECODER_CONTRACT
#define GCINFODECODER_CONTRACT LIMITED_METHOD_CONTRACT
#endif // !GCINFODECODER_CONTRACT


#ifndef GET_CALLER_SP
#define GET_CALLER_SP(pREGDISPLAY) EECodeManager::GetCallerSp(pREGDISPLAY)
#endif // !GET_CALLER_SP

#ifndef VALIDATE_OBJECTREF
#if defined(DACCESS_COMPILE)
#define VALIDATE_OBJECTREF(objref, fDeep)
#else // DACCESS_COMPILE
#define VALIDATE_OBJECTREF(objref, fDeep)                          \
    do {                                                           \
        Object* objPtr = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref); \
        if (objPtr)                                                \
        {                                                          \
            objPtr->Validate(fDeep);                               \
        }                                                          \
    } while(0)
#endif // DACCESS_COMPILE
#endif // !VALIDATE_OBJECTREF

#ifndef VALIDATE_ROOT
#include "gcenv.h"
#define VALIDATE_ROOT(isInterior, hCallBack, pObjRef)                                           \
    do {                                                                                        \
        /* Only call Object::Validate() with bDeep == TRUE if we are in the promote phase.  */  \
        /* We should call Validate() with bDeep == FALSE if we are in the relocation phase. */  \
        /* Actually with the introduction of the POPO feature, we cannot validate during    */  \
        /* relocate because POPO might have written over the object. It will require non    */  \
        /* trivial amount of work to make this work.*/                                          \
                                                                                                \
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);                                            \
                                                                                                \
        if (!(isInterior) && !(m_Flags & DECODE_NO_VALIDATION) && (pGCCtx->sc->promotion)) {    \
            VALIDATE_OBJECTREF(*(pObjRef), pGCCtx->sc->promotion == TRUE);                      \
        }                                                                                       \
    } while (0)
#endif // !VALIDATE_ROOT

#ifndef LOG_PIPTR
#define LOG_PIPTR(pObjRef, gcFlags, hCallBack)                                                                                                  \
    {                                                                                                                                           \
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);                                                                                            \
        if (pGCCtx->sc->promotion)                                                                                                              \
        {                                                                                                                                       \
            LOG((LF_GCROOTS, LL_INFO1000, /* Part Three */                                                                                      \
                LOG_PIPTR_OBJECT_CLASS(OBJECTREF_TO_UNCHECKED_OBJECTREF(*pObjRef), (gcFlags & GC_CALL_PINNED), (gcFlags & GC_CALL_INTERIOR)))); \
        }                                                                                                                                       \
        else                                                                                                                                    \
        {                                                                                                                                       \
            LOG((LF_GCROOTS, LL_INFO1000, /* Part Three */                                                                                      \
                LOG_PIPTR_OBJECT(OBJECTREF_TO_UNCHECKED_OBJECTREF(*pObjRef), (gcFlags & GC_CALL_PINNED), (gcFlags & GC_CALL_INTERIOR))));       \
        }                                                                                                                                       \
    }
#endif // !LOG_PIPTR

bool GcInfoDecoder::SetIsInterruptibleCB (UINT32 startOffset, UINT32 stopOffset, void * hCallback)
{
    GcInfoDecoder *pThis = (GcInfoDecoder*)hCallback;


    bool fStop = pThis->m_InstructionOffset >= startOffset && pThis->m_InstructionOffset < stopOffset;

    if (fStop)
        pThis->m_IsInterruptible = true;

    return fStop;
}

// returns true if we decoded all that was asked;
bool GcInfoDecoder::PredecodeFatHeader(int remainingFlags)
{
    int numFlagBits = (m_Version == 1) ? GC_INFO_FLAGS_BIT_SIZE_VERSION_1 : GC_INFO_FLAGS_BIT_SIZE;
    m_headerFlags = (GcInfoHeaderFlags)m_Reader.Read(numFlagBits);

    m_ReturnKind = (ReturnKind)((UINT32)m_Reader.Read(SIZE_OF_RETURN_KIND_IN_FAT_HEADER));

    remainingFlags &= ~(DECODE_RETURN_KIND | DECODE_VARARG);
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    remainingFlags &= ~DECODE_HAS_TAILCALLS;
#endif
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    m_CodeLength = (UINT32)DENORMALIZE_CODE_LENGTH((UINT32)m_Reader.DecodeVarLengthUnsigned(CODE_LENGTH_ENCBASE));
    remainingFlags &= ~DECODE_CODE_LENGTH;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    if (m_headerFlags & GC_INFO_HAS_GS_COOKIE)
    {
        // Note that normalization as a code offset can be different than
        //  normalization as code length
        UINT32 normCodeLength = NORMALIZE_CODE_OFFSET(m_CodeLength);

        // Decode prolog/epilog information
        UINT32 normPrologSize = (UINT32)m_Reader.DecodeVarLengthUnsigned(NORM_PROLOG_SIZE_ENCBASE) + 1;
        UINT32 normEpilogSize = (UINT32)m_Reader.DecodeVarLengthUnsigned(NORM_EPILOG_SIZE_ENCBASE);

        m_ValidRangeStart = (UINT32)DENORMALIZE_CODE_OFFSET(normPrologSize);
        m_ValidRangeEnd = (UINT32)DENORMALIZE_CODE_OFFSET(normCodeLength - normEpilogSize);
        _ASSERTE(m_ValidRangeStart < m_ValidRangeEnd);
    }
    else if ((m_headerFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
    {
        // Decode prolog information
        UINT32 normPrologSize = (UINT32)m_Reader.DecodeVarLengthUnsigned(NORM_PROLOG_SIZE_ENCBASE) + 1;
        m_ValidRangeStart = (UINT32)DENORMALIZE_CODE_OFFSET(normPrologSize);
        // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
        m_ValidRangeEnd = m_ValidRangeStart + 1;
    }
    else
    {
        m_ValidRangeStart = m_ValidRangeEnd = 0;
    }

    remainingFlags &= ~DECODE_PROLOG_LENGTH;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    // Decode the offset to the GS cookie.
    if (m_headerFlags & GC_INFO_HAS_GS_COOKIE)
    {
        m_GSCookieStackSlot = (INT32)DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(GS_COOKIE_STACK_SLOT_ENCBASE));
    }
    else
    {
        m_GSCookieStackSlot = NO_GS_COOKIE;
    }

    remainingFlags &= ~DECODE_GS_COOKIE;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    // Decode the offset to the PSPSym.
    // The PSPSym is relative to the caller SP on IA64 and the initial stack pointer before any stack allocation on X64 (InitialSP).
    if (m_headerFlags & GC_INFO_HAS_PSP_SYM)
    {
        m_PSPSymStackSlot = (INT32)DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(PSP_SYM_STACK_SLOT_ENCBASE));
    }
    else
    {
        m_PSPSymStackSlot = NO_PSP_SYM;
    }

    remainingFlags &= ~DECODE_PSP_SYM;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    // Decode the offset to the generics type context.
    if ((m_headerFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
    {
        m_GenericsInstContextStackSlot = (INT32)DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE));
    }
    else
    {
        m_GenericsInstContextStackSlot = NO_GENERICS_INST_CONTEXT;
    }

    remainingFlags &= ~DECODE_GENERICS_INST_CONTEXT;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    if (m_headerFlags & GC_INFO_HAS_STACK_BASE_REGISTER)
    {
        m_StackBaseRegister = (UINT32)DENORMALIZE_STACK_BASE_REGISTER(m_Reader.DecodeVarLengthUnsigned(STACK_BASE_REGISTER_ENCBASE));
    }
    else
    {
        m_StackBaseRegister = NO_STACK_BASE_REGISTER;
    }

    if (m_headerFlags & GC_INFO_HAS_EDIT_AND_CONTINUE_INFO)
    {
        m_SizeOfEditAndContinuePreservedArea = (UINT32)m_Reader.DecodeVarLengthUnsigned(SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE);
#ifdef TARGET_ARM64
        m_SizeOfEditAndContinueFixedStackFrame = (UINT32)m_Reader.DecodeVarLengthUnsigned(SIZE_OF_EDIT_AND_CONTINUE_FIXED_STACK_FRAME_ENCBASE);
#endif
    }
    else
    {
        m_SizeOfEditAndContinuePreservedArea = NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
#ifdef TARGET_ARM64
        m_SizeOfEditAndContinueFixedStackFrame = 0;
#endif
    }

    remainingFlags &= ~DECODE_EDIT_AND_CONTINUE;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

    if (m_headerFlags & GC_INFO_REVERSE_PINVOKE_FRAME)
    {
        m_ReversePInvokeFrameStackSlot = (INT32)DENORMALIZE_STACK_SLOT(m_Reader.DecodeVarLengthSigned(REVERSE_PINVOKE_FRAME_ENCBASE));
    }
    else
    {
        m_ReversePInvokeFrameStackSlot = NO_REVERSE_PINVOKE_FRAME;
    }

    remainingFlags &= ~DECODE_REVERSE_PINVOKE_VAR;
    if (remainingFlags == 0)
    {
        // Bail, if we've decoded enough,
        return true;
    }

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    m_SizeOfStackOutgoingAndScratchArea = (UINT32)DENORMALIZE_SIZE_OF_STACK_AREA(m_Reader.DecodeVarLengthUnsigned(SIZE_OF_STACK_AREA_ENCBASE));
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

    return false;
}

GcInfoDecoder::GcInfoDecoder(
            GCInfoToken gcInfoToken,
            GcInfoDecoderFlags flags,
            UINT32 breakOffset
            )
            : m_Reader(dac_cast<PTR_CBYTE>(gcInfoToken.Info))
            , m_InstructionOffset(breakOffset)
            , m_IsInterruptible(false)
            , m_ReturnKind(RT_Illegal)
#ifdef _DEBUG
            , m_Flags( flags )
            , m_GcInfoAddress(dac_cast<PTR_CBYTE>(gcInfoToken.Info))
#endif
           , m_Version(gcInfoToken.Version)
{
    _ASSERTE( (flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES)) || (0 == breakOffset) );

    // The current implementation doesn't support the two flags together
    _ASSERTE(
        ((flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES)) != (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES))
            );

    //--------------------------------------------
    // Pre-decode information
    //--------------------------------------------

    bool slimHeader = (m_Reader.ReadOneFast() == 0);
    // Use flag mask to bail out early if we already decoded all the pieces that caller requested
    int remainingFlags = flags == DECODE_EVERYTHING ? ~0 : flags;

    if (!slimHeader)
    {
        if (PredecodeFatHeader(remainingFlags))
            return;
    }
    else
    {
        if (m_Reader.ReadOneFast())
        {
            m_headerFlags = GC_INFO_HAS_STACK_BASE_REGISTER;
            m_StackBaseRegister = (UINT32)DENORMALIZE_STACK_BASE_REGISTER(0);
        }
        else
        {
            m_headerFlags = (GcInfoHeaderFlags)0;
            m_StackBaseRegister = NO_STACK_BASE_REGISTER;
        }

        m_ReturnKind = (ReturnKind)((UINT32)m_Reader.Read(SIZE_OF_RETURN_KIND_IN_SLIM_HEADER));

        remainingFlags &= ~(DECODE_RETURN_KIND | DECODE_VARARG);
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        remainingFlags &= ~DECODE_HAS_TAILCALLS;
#endif

        if (remainingFlags == 0)
        {
            // Bail, if we've decoded enough,
            return;
        }

        m_CodeLength = (UINT32)DENORMALIZE_CODE_LENGTH((UINT32)m_Reader.DecodeVarLengthUnsigned(CODE_LENGTH_ENCBASE));

        //
        // predecoding the rest of slim header does not require any reading.
        //

        m_ValidRangeStart = m_ValidRangeEnd = 0;
        m_GSCookieStackSlot = NO_GS_COOKIE;
        m_PSPSymStackSlot = NO_PSP_SYM;
        m_GenericsInstContextStackSlot = NO_GENERICS_INST_CONTEXT;
        m_SizeOfEditAndContinuePreservedArea = NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;

#ifdef TARGET_ARM64
        m_SizeOfEditAndContinueFixedStackFrame = 0;
#endif

        m_ReversePInvokeFrameStackSlot = NO_REVERSE_PINVOKE_FRAME;

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
        m_SizeOfStackOutgoingAndScratchArea = 0;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

        remainingFlags &= ~(DECODE_CODE_LENGTH
                            | DECODE_PROLOG_LENGTH
                            | DECODE_GS_COOKIE
                            | DECODE_PSP_SYM
                            | DECODE_GENERICS_INST_CONTEXT
                            | DECODE_EDIT_AND_CONTINUE
                            | DECODE_REVERSE_PINVOKE_VAR
                            );

        if (remainingFlags == 0)
        {
            // Bail, if we've decoded enough,
            return;
        }
    }

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    m_NumSafePoints = (UINT32) DENORMALIZE_NUM_SAFE_POINTS(m_Reader.DecodeVarLengthUnsigned(NUM_SAFE_POINTS_ENCBASE));
    m_SafePointIndex = m_NumSafePoints;
#endif

    if (slimHeader)
    {
        m_NumInterruptibleRanges = 0;
    }
    else
    {
        m_NumInterruptibleRanges = (UINT32) DENORMALIZE_NUM_INTERRUPTIBLE_RANGES(m_Reader.DecodeVarLengthUnsigned(NUM_INTERRUPTIBLE_RANGES_ENCBASE));
    }

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    if(flags & (DECODE_GC_LIFETIMES | DECODE_INTERRUPTIBILITY))
    {
        if(m_NumSafePoints)
        {
            // Safepoints are encoded with a -1 adjustment
            // DECODE_GC_LIFETIMES adjusts the offset accordingly, but DECODE_INTERRUPTIBILITY does not
            // adjust here
            UINT32 offset = flags & DECODE_INTERRUPTIBILITY ? m_InstructionOffset - 1 : m_InstructionOffset;
            m_SafePointIndex = FindSafePoint(offset);
        }
    }

    if(flags & (DECODE_FOR_RANGES_CALLBACK | DECODE_INTERRUPTIBILITY))
    {
        // Note that normalization as a code offset can be different than
        //  normalization as code length
        UINT32 normCodeLength = NORMALIZE_CODE_OFFSET(m_CodeLength);

        UINT32 numBitsPerOffset = CeilOfLog2(normCodeLength);
        m_Reader.Skip(m_NumSafePoints * numBitsPerOffset);
    }
#endif

    _ASSERTE(!m_IsInterruptible);
    if(flags & DECODE_INTERRUPTIBILITY)
    {
        EnumerateInterruptibleRanges(&SetIsInterruptibleCB, this);
    }
}

bool GcInfoDecoder::IsInterruptible()
{
    _ASSERTE( m_Flags & DECODE_INTERRUPTIBILITY );
    return m_IsInterruptible;
}

bool GcInfoDecoder::HasInterruptibleRanges()
{
    _ASSERTE(m_Flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES));
    return m_NumInterruptibleRanges > 0;
}

bool GcInfoDecoder::IsSafePoint()
{
    _ASSERTE(m_Flags & (DECODE_INTERRUPTIBILITY | DECODE_GC_LIFETIMES));
    return m_SafePointIndex != m_NumSafePoints;
}

bool GcInfoDecoder::AreSafePointsInterruptible()
{
    if (m_Version < 3)
        return false;

    // zero initialized
    static bool fInterruptibleCallSitesInited;
    static uint64_t fInterruptibleCallSites;

    if (!fInterruptibleCallSitesInited)
    {
#ifdef FEATURE_NATIVEAOT
        fInterruptibleCallSites = 1;
        g_pRhConfig->ReadConfigValue("InterruptibleCallSites", &fInterruptibleCallSites);
#else
        fInterruptibleCallSites = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_InterruptibleCallSites);
#endif
        fInterruptibleCallSitesInited = true;
    }

    return fInterruptibleCallSites != 0;
}

bool GcInfoDecoder::IsInterruptibleSafePoint()
{
    return IsSafePoint() && AreSafePointsInterruptible();
}

bool GcInfoDecoder::HasMethodDescGenericsInstContext()
{
    _ASSERTE( m_Flags & DECODE_GENERICS_INST_CONTEXT );
    return (m_headerFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) == GC_INFO_HAS_GENERICS_INST_CONTEXT_MD;
}

bool GcInfoDecoder::HasMethodTableGenericsInstContext()
{
    _ASSERTE( m_Flags & DECODE_GENERICS_INST_CONTEXT );
    return (m_headerFlags & GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) == GC_INFO_HAS_GENERICS_INST_CONTEXT_MT;
}

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

// This is used for gccoverage: is the given offset
//  a call-return offset with partially-interruptible GC info?
bool GcInfoDecoder::IsSafePoint(UINT32 codeOffset)
{
    _ASSERTE(m_Flags == DECODE_EVERYTHING && m_InstructionOffset == 0);
    if(m_NumSafePoints == 0)
        return false;

#if defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // Safepoints are encoded with a -1 adjustment
    codeOffset--;
#endif
    size_t savedPos = m_Reader.GetCurrentPos();
    UINT32 safePointIndex = FindSafePoint(codeOffset);
    m_Reader.SetCurrentPos(savedPos);
    return (bool) (safePointIndex != m_NumSafePoints);

}

// Repositioning within a bit stream is an involved operation, compared to sequential read,
// so we prefer linear search unless the number of safepoints is too high.
// The limit is not very significant as most methods will have just a few safe points.
// At 32, even if a single point is 16bit encoded (64K method length),
// the whole run will be under 64 bytes, so likely we will stay in the same cache line.
#define MAX_LINEAR_SEARCH 32

NOINLINE
UINT32 GcInfoDecoder::NarrowSafePointSearch(size_t savedPos, UINT32 breakOffset, UINT32* searchEnd)
{
    INT32 low = 0;
    INT32 high = (INT32)m_NumSafePoints;

    const UINT32 numBitsPerOffset = CeilOfLog2(NORMALIZE_CODE_OFFSET(m_CodeLength));
    while (high - low > MAX_LINEAR_SEARCH)
    {
        const INT32 mid = (low + high) / 2;
        _ASSERTE(mid >= 0 && mid < (INT32)m_NumSafePoints);
        m_Reader.SetCurrentPos(savedPos + (UINT32)mid * numBitsPerOffset);
        UINT32 midSpOffset = (UINT32)m_Reader.Read(numBitsPerOffset);

        if (breakOffset < midSpOffset)
            high = mid;
        else
            low = mid;
    }

    m_Reader.SetCurrentPos(savedPos +(UINT32)low * numBitsPerOffset);
    *searchEnd = high;
    return low;
}

UINT32 GcInfoDecoder::FindSafePoint(UINT32 breakOffset)
{
    _ASSERTE(m_NumSafePoints > 0);
    UINT32 result = m_NumSafePoints;
    const size_t savedPos = m_Reader.GetCurrentPos();
    const UINT32 numBitsPerOffset = CeilOfLog2(NORMALIZE_CODE_OFFSET(m_CodeLength));

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // Safepoints are encoded with a -1 adjustment
    if ((breakOffset & 1) != 0)
#endif
    {
        const UINT32 normBreakOffset = NORMALIZE_CODE_OFFSET(breakOffset);
        UINT32 linearSearchStart = 0;
        UINT32 linearSearchEnd = m_NumSafePoints;
        if (linearSearchEnd - linearSearchStart > MAX_LINEAR_SEARCH)
        {
            linearSearchStart = NarrowSafePointSearch(savedPos, normBreakOffset, &linearSearchEnd);
        }

        for (UINT32 i = linearSearchStart; i < linearSearchEnd; i++)
        {
            UINT32 spOffset = (UINT32)m_Reader.Read(numBitsPerOffset);
            if (spOffset == normBreakOffset)
            {
                result = i;
                break;
            }

            if (spOffset > normBreakOffset)
            {
                break;
            }
        }
    }

    m_Reader.SetCurrentPos(savedPos + m_NumSafePoints * numBitsPerOffset);
    return result;
}

void GcInfoDecoder::EnumerateSafePoints(EnumerateSafePointsCallback *pCallback, void * hCallback)
{
    if(m_NumSafePoints == 0)
        return;

    const UINT32 numBitsPerOffset = CeilOfLog2(NORMALIZE_CODE_OFFSET(m_CodeLength));

    for(UINT32 i = 0; i < m_NumSafePoints; i++)
    {
        UINT32 normOffset = (UINT32)m_Reader.Read(numBitsPerOffset);
        UINT32 offset = DENORMALIZE_CODE_OFFSET(normOffset) + 2;

#if defined(TARGET_AMD64) || defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Safepoints are encoded with a -1 adjustment
        offset--;
#endif

        pCallback(offset, hCallback);
    }
}
#endif

void GcInfoDecoder::EnumerateInterruptibleRanges (
            EnumerateInterruptibleRangesCallback *pCallback,
            void *                                hCallback)
{
    // If no info is found for the call site, we default to fully-interruptible
    LOG((LF_GCROOTS, LL_INFO1000000, "No GC info found for call site at offset %x. Defaulting to fully-interruptible information.\n", (int) m_InstructionOffset));

    UINT32 lastInterruptibleRangeStopOffsetNormalized = 0;

    for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
    {
        UINT32 normStartDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA1_ENCBASE );
        UINT32 normStopDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA2_ENCBASE ) + 1;

        UINT32 rangeStartOffsetNormalized = lastInterruptibleRangeStopOffsetNormalized + normStartDelta;
        UINT32 rangeStopOffsetNormalized = rangeStartOffsetNormalized + normStopDelta;

        UINT32 rangeStartOffset = DENORMALIZE_CODE_OFFSET(rangeStartOffsetNormalized);
        UINT32 rangeStopOffset = DENORMALIZE_CODE_OFFSET(rangeStopOffsetNormalized);

        bool fStop = pCallback(rangeStartOffset, rangeStopOffset, hCallback);
        if (fStop)
            return;

        lastInterruptibleRangeStopOffsetNormalized = rangeStopOffsetNormalized;
    }
}

INT32 GcInfoDecoder::GetGSCookieStackSlot()
{
    _ASSERTE( m_Flags & DECODE_GS_COOKIE );
    return m_GSCookieStackSlot;
}

INT32 GcInfoDecoder::GetReversePInvokeFrameStackSlot()
{
    _ASSERTE(m_Flags & DECODE_REVERSE_PINVOKE_VAR);
    return m_ReversePInvokeFrameStackSlot;
}

UINT32 GcInfoDecoder::GetGSCookieValidRangeStart()
{
    _ASSERTE( m_Flags & DECODE_GS_COOKIE );
    return m_ValidRangeStart;
}
UINT32 GcInfoDecoder::GetGSCookieValidRangeEnd()
{
    _ASSERTE( m_Flags & DECODE_GS_COOKIE );
    return m_ValidRangeEnd;
}

UINT32 GcInfoDecoder::GetPrologSize()
{
    _ASSERTE( m_Flags & DECODE_PROLOG_LENGTH );

    return m_ValidRangeStart;
}

INT32 GcInfoDecoder::GetGenericsInstContextStackSlot()
{
    _ASSERTE( m_Flags & DECODE_GENERICS_INST_CONTEXT );
    return m_GenericsInstContextStackSlot;
}

INT32 GcInfoDecoder::GetPSPSymStackSlot()
{
    _ASSERTE( m_Flags & DECODE_PSP_SYM );
    return m_PSPSymStackSlot;
}

bool GcInfoDecoder::GetIsVarArg()
{
    _ASSERTE( m_Flags & DECODE_VARARG );
    return m_headerFlags & GC_INFO_IS_VARARG;
}

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
bool GcInfoDecoder::HasTailCalls()
{
    _ASSERTE( m_Flags & DECODE_HAS_TAILCALLS );
    return ((m_headerFlags & GC_INFO_HAS_TAILCALLS) != 0);
}
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

bool GcInfoDecoder::WantsReportOnlyLeaf()
{
    // Only AMD64 with JIT64 can return false here.
#ifdef TARGET_AMD64
    return ((m_headerFlags & GC_INFO_WANTS_REPORT_ONLY_LEAF) != 0);
#else
    return true;
#endif
}

UINT32 GcInfoDecoder::GetCodeLength()
{
//    SUPPORTS_DAC;
    _ASSERTE( m_Flags & DECODE_CODE_LENGTH );
    return m_CodeLength;
}

ReturnKind GcInfoDecoder::GetReturnKind()
{
    //    SUPPORTS_DAC;
    _ASSERTE( m_Flags & DECODE_RETURN_KIND );
    return m_ReturnKind;
}

UINT32  GcInfoDecoder::GetStackBaseRegister()
{
    return m_StackBaseRegister;
}

UINT32 GcInfoDecoder::GetSizeOfEditAndContinuePreservedArea()
{
    _ASSERTE( m_Flags & DECODE_EDIT_AND_CONTINUE );
    return m_SizeOfEditAndContinuePreservedArea;
}

#ifdef TARGET_ARM64
UINT32 GcInfoDecoder::GetSizeOfEditAndContinueFixedStackFrame()
{
    _ASSERTE( m_Flags & DECODE_EDIT_AND_CONTINUE );
    return m_SizeOfEditAndContinueFixedStackFrame;
}
#endif

size_t  GcInfoDecoder::GetNumBytesRead()
{
    return (m_Reader.GetCurrentPos() + 7) / 8;
}


#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA

UINT32  GcInfoDecoder::GetSizeOfStackParameterArea()
{
    return m_SizeOfStackOutgoingAndScratchArea;
}

#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


bool GcInfoDecoder::EnumerateLiveSlots(
                PREGDISPLAY         pRD,
                bool                reportScratchSlots,
                unsigned            inputFlags,
                GCEnumCallback      pCallBack,
                void *              hCallBack
                )
{

    unsigned executionAborted = (inputFlags & ExecutionAborted);

    // In order to make ARM more x86-like we only ever report the leaf frame
    // of any given function. We accomplish this by having the stackwalker
    // pass a flag whenever walking the frame of a method where it has
    // previously visited a child funclet
    if (WantsReportOnlyLeaf() && (inputFlags & ParentOfFuncletStackFrame))
    {
        LOG((LF_GCROOTS, LL_INFO100000, "Not reporting this frame because it was already reported via another funclet.\n"));
        return true;
    }

    //
    // If this is a non-leaf frame and we are executing a call, the unwinder has given us the PC
    //  of the call instruction. We should adjust it to the PC of the instruction after the call in order to
    //  obtain transition information for scratch slots. However, we always assume scratch slots to be
    //  dead for non-leaf frames (except for ResumableFrames), so we don't need to adjust the PC.
    // If this is a non-leaf frame and we are not executing a call (i.e.: a fault occurred in the function),
    //  then it would be incorrect to adjust the PC
    //

    _ASSERTE(GC_SLOT_INTERIOR == GC_CALL_INTERIOR);
    _ASSERTE(GC_SLOT_PINNED == GC_CALL_PINNED);

    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    GcSlotDecoder slotDecoder;

    UINT32 normBreakOffset = NORMALIZE_CODE_OFFSET(m_InstructionOffset);

    // Normalized break offset
    // Relative to interruptible ranges #if PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    UINT32 pseudoBreakOffset = 0;
    UINT32 numInterruptibleLength = 0;
#else
    UINT32 pseudoBreakOffset = normBreakOffset;
    UINT32 numInterruptibleLength = NORMALIZE_CODE_OFFSET(m_CodeLength);
#endif


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    bool noTrackedRefs = false;

    if(m_SafePointIndex < m_NumSafePoints && !executionAborted)
    {
        // Skip interruptibility information
        for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
        {
            m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA1_ENCBASE );
            m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA2_ENCBASE );
        }
    }
    else
    {
        //
        // We didn't find the break offset in the list of call sites
        //    or we are in an executionAborted frame
        // So either we have fully-interruptible information,
        //    or execution will not resume at the current method
        //    and nothing should be reported
        //
        if(!executionAborted)
        {
            if(m_NumInterruptibleRanges == 0)
            {
                // No ranges and no explicit safepoint - must be MinOpts with untracked refs.
                noTrackedRefs = true;
            }
        }

        if(m_NumInterruptibleRanges != 0)
        {
            int countIntersections = 0;
            UINT32 lastNormStop = 0;
            for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
            {
                UINT32 normStartDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA1_ENCBASE );
                UINT32 normStopDelta = (UINT32) m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA2_ENCBASE ) + 1;

                UINT32 normStart = lastNormStop + normStartDelta;
                UINT32 normStop = normStart + normStopDelta;
                if(normBreakOffset >= normStart && normBreakOffset < normStop)
                {
                    _ASSERTE(pseudoBreakOffset == 0);
                    countIntersections++;
                    pseudoBreakOffset = numInterruptibleLength + normBreakOffset - normStart;
                }
                numInterruptibleLength += normStopDelta;
                lastNormStop = normStop;
            }
            _ASSERTE(countIntersections <= 1);
            if(countIntersections == 0)
            {
                _ASSERTE(executionAborted);
                LOG((LF_GCROOTS, LL_INFO100000, "Not reporting this frame because it is aborted and not fully interruptible.\n"));
                goto ExitSuccess;
            }
        }
    }
#else   // !PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    // Skip interruptibility information
    for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
    {
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA1_ENCBASE );
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA2_ENCBASE );
    }
#endif


    //------------------------------------------------------------------------------
    // Read the slot table
    //------------------------------------------------------------------------------


    slotDecoder.DecodeSlotTable(m_Reader);

    {
        UINT32 numSlots = slotDecoder.GetNumTracked();

        if(!numSlots)
            goto ReportUntracked;

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

        UINT32 numBitsPerOffset = 0;
        // Duplicate the encoder's heuristic to determine if we have indirect live
        // slot table (similar to the chunk pointers)
        if ((m_NumSafePoints > 0) && m_Reader.ReadOneFast())
        {
            numBitsPerOffset = (UINT32) m_Reader.DecodeVarLengthUnsigned(POINTER_SIZE_ENCBASE) + 1;
            _ASSERTE(numBitsPerOffset != 0);
        }

        //------------------------------------------------------------------------------
        // Try partially interruptible first
        //------------------------------------------------------------------------------

        if( !executionAborted && m_SafePointIndex != m_NumSafePoints )
        {
            if (numBitsPerOffset)
            {
                const size_t offsetTablePos = m_Reader.GetCurrentPos();
                m_Reader.Skip(m_SafePointIndex * numBitsPerOffset);
                const size_t liveStatesOffset = m_Reader.Read(numBitsPerOffset);
                const size_t liveStatesStart = ((offsetTablePos + m_NumSafePoints * numBitsPerOffset + 7) & (~7));
                m_Reader.SetCurrentPos(liveStatesStart + liveStatesOffset);
                if (m_Reader.ReadOneFast()) {
                    // RLE encoded
                    bool fSkip = (m_Reader.ReadOneFast() == 0);
                    bool fReport = true;
                    UINT32 readSlots = (UINT32)m_Reader.DecodeVarLengthUnsigned( fSkip ? LIVESTATE_RLE_SKIP_ENCBASE : LIVESTATE_RLE_RUN_ENCBASE );
                    fSkip = !fSkip;
                    while (readSlots < numSlots)
                    {
                        UINT32 cnt = (UINT32)m_Reader.DecodeVarLengthUnsigned( fSkip ? LIVESTATE_RLE_SKIP_ENCBASE : LIVESTATE_RLE_RUN_ENCBASE ) + 1;
                        if (fReport)
                        {
                            for(UINT32 slotIndex = readSlots; slotIndex < readSlots + cnt; slotIndex++)
                            {
                                ReportSlotToGC(slotDecoder,
                                               slotIndex,
                                               pRD,
                                               reportScratchSlots,
                                               inputFlags,
                                               pCallBack,
                                               hCallBack
                                               );
                            }
                        }
                        readSlots += cnt;
                        fSkip = !fSkip;
                        fReport = !fReport;
                    }
                    _ASSERTE(readSlots == numSlots);
                    goto ReportUntracked;
                }
                // Just a normal live state (1 bit per slot), so use the normal decoding loop
            }
            else
            {
                m_Reader.Skip(m_SafePointIndex * numSlots);
            }

            for(UINT32 slotIndex = 0; slotIndex < numSlots; slotIndex++)
            {
                if(m_Reader.ReadOneFast())
                {
                    ReportSlotToGC(
                            slotDecoder,
                            slotIndex,
                            pRD,
                            reportScratchSlots,
                            inputFlags,
                            pCallBack,
                            hCallBack
                            );
                }
            }
            goto ReportUntracked;
        }
        else
        {
            m_Reader.Skip(m_NumSafePoints * numSlots);
            if(m_NumInterruptibleRanges == 0)
                goto ReportUntracked;
        }
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

        _ASSERTE(m_NumInterruptibleRanges);
        _ASSERTE(numInterruptibleLength);

        // If no info is found for the call site, we default to fully-interruptible
        LOG((LF_GCROOTS, LL_INFO1000000, "No GC info found for call site at offset %x. Defaulting to fully-interruptible information.\n", (int) m_InstructionOffset));

        UINT32 numChunks = (numInterruptibleLength + NUM_NORM_CODE_OFFSETS_PER_CHUNK - 1) / NUM_NORM_CODE_OFFSETS_PER_CHUNK;
        UINT32 breakChunk = pseudoBreakOffset / NUM_NORM_CODE_OFFSETS_PER_CHUNK;
        _ASSERTE(breakChunk < numChunks);

        UINT32 numBitsPerPointer = (UINT32) m_Reader.DecodeVarLengthUnsigned(POINTER_SIZE_ENCBASE);

        if(!numBitsPerPointer)
            goto ReportUntracked;

        size_t pointerTablePos = m_Reader.GetCurrentPos();

        size_t chunkPointer;
        UINT32 chunk = breakChunk;
        for(;;)
        {
            m_Reader.SetCurrentPos(pointerTablePos + chunk * numBitsPerPointer);
            chunkPointer = m_Reader.Read(numBitsPerPointer);
            if(chunkPointer)
                break;

            if(chunk-- == 0)
                goto ReportUntracked;
        }

        size_t chunksStartPos = ((pointerTablePos + numChunks * numBitsPerPointer + 7) & (~7));
        size_t chunkPos = chunksStartPos + chunkPointer - 1;
        m_Reader.SetCurrentPos(chunkPos);

        {
            BitStreamReader couldBeLiveReader(m_Reader);

            UINT32 numCouldBeLiveSlots = 0;
            // A potentially compressed bit vector of which slots have any lifetimes
            if (m_Reader.ReadOneFast())
            {
                // RLE encoded
                bool fSkip = (m_Reader.ReadOneFast() == 0);
                bool fReport = true;
                UINT32 readSlots = (UINT32)m_Reader.DecodeVarLengthUnsigned( fSkip ? LIVESTATE_RLE_SKIP_ENCBASE : LIVESTATE_RLE_RUN_ENCBASE );
                fSkip = !fSkip;
                while (readSlots < numSlots)
                {
                    UINT32 cnt = (UINT32)m_Reader.DecodeVarLengthUnsigned( fSkip ? LIVESTATE_RLE_SKIP_ENCBASE : LIVESTATE_RLE_RUN_ENCBASE ) + 1;
                    if (fReport)
                    {
                        numCouldBeLiveSlots += cnt;
                    }
                    readSlots += cnt;
                    fSkip = !fSkip;
                    fReport = !fReport;
                }
                _ASSERTE(readSlots == numSlots);

            }
            else
            {
                for(UINT32 i = 0; i < numSlots; i++)
                {
                    if(m_Reader.ReadOneFast())
                        numCouldBeLiveSlots++;
                }
            }
            _ASSERTE(numCouldBeLiveSlots > 0);

            BitStreamReader finalStateReader(m_Reader);

            m_Reader.Skip(numCouldBeLiveSlots);

            int lifetimeTransitionsCount = 0;

            UINT32 slotIndex = 0;
            bool fSimple = (couldBeLiveReader.ReadOneFast() == 0);
            bool fSkipFirst = false; // silence the warning
            UINT32 cnt = 0;
            if (!fSimple)
            {
                fSkipFirst = (couldBeLiveReader.ReadOneFast() == 0);
                slotIndex = -1;
            }
            for(UINT32 i = 0; i < numCouldBeLiveSlots; i++)
            {
                if (fSimple)
                {
                    while(!couldBeLiveReader.ReadOneFast())
                        slotIndex++;
                }
                else if (cnt > 0)
                {
                    // We have more from the last run to report
                    cnt--;
                }
                // We need to find a new run
                else if (fSkipFirst)
                {
                    UINT32 tmp = (UINT32)couldBeLiveReader.DecodeVarLengthUnsigned( LIVESTATE_RLE_SKIP_ENCBASE ) + 1;
                    slotIndex += tmp;
                    cnt = (UINT32)couldBeLiveReader.DecodeVarLengthUnsigned( LIVESTATE_RLE_RUN_ENCBASE );
                }
                else
                {
                    UINT32 tmp = (UINT32)couldBeLiveReader.DecodeVarLengthUnsigned( LIVESTATE_RLE_RUN_ENCBASE ) + 1;
                    slotIndex += tmp;
                    cnt = (UINT32)couldBeLiveReader.DecodeVarLengthUnsigned( LIVESTATE_RLE_SKIP_ENCBASE );
                }

                UINT32 isLive = (UINT32) finalStateReader.Read(1);

                if(chunk == breakChunk)
                {
                    // Read transitions
                    UINT32 normBreakOffsetDelta = pseudoBreakOffset % NUM_NORM_CODE_OFFSETS_PER_CHUNK;
                    for(;;)
                    {
                        if(!m_Reader.ReadOneFast())
                            break;

                        UINT32 transitionOffset = (UINT32) m_Reader.Read(NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2);

                        lifetimeTransitionsCount++;
                        _ASSERTE(transitionOffset && transitionOffset < NUM_NORM_CODE_OFFSETS_PER_CHUNK);
                        if(transitionOffset > normBreakOffsetDelta)
                        {
                            isLive ^= 1;
                        }
                    }
                }

                if(isLive)
                {
                    ReportSlotToGC(
                            slotDecoder,
                            slotIndex,
                            pRD,
                            reportScratchSlots,
                            inputFlags,
                            pCallBack,
                            hCallBack
                            );
                }

                slotIndex++;
            }

            LOG((LF_GCROOTS, LL_INFO1000000, "Decoded %d lifetime transitions.\n", (int) lifetimeTransitionsCount ));
        }
    }

ReportUntracked:

    //------------------------------------------------------------------------------
    // Last report anything untracked
    // But only for the leaf funclet/frame
    // Turned on in the VM for regular GC reporting and the DAC for !CLRStack -gc
    // But turned off in the #includes for nidump and sos's !u -gcinfo and !gcinfo
    //------------------------------------------------------------------------------

    if (slotDecoder.GetNumUntracked() && !(inputFlags & (ParentOfFuncletStackFrame | NoReportUntracked)))
    {
        ReportUntrackedSlots(slotDecoder, pRD, inputFlags, pCallBack, hCallBack);
    }

ExitSuccess:

    return true;
}

void GcInfoDecoder::EnumerateUntrackedSlots(
                PREGDISPLAY         pRD,
                unsigned            inputFlags,
                GCEnumCallback      pCallBack,
                void *              hCallBack
                )
{
    _ASSERTE(GC_SLOT_INTERIOR == GC_CALL_INTERIOR);
    _ASSERTE(GC_SLOT_PINNED == GC_CALL_PINNED);

    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    GcSlotDecoder slotDecoder;

    // Skip interruptibility information
    for(UINT32 i=0; i<m_NumInterruptibleRanges; i++)
    {
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA1_ENCBASE );
        m_Reader.DecodeVarLengthUnsigned( INTERRUPTIBLE_RANGE_DELTA2_ENCBASE );
    }

    //------------------------------------------------------------------------------
    // Read the slot table
    //------------------------------------------------------------------------------

    slotDecoder.DecodeSlotTable(m_Reader);

    if (slotDecoder.GetNumUntracked())
    {
        ReportUntrackedSlots(slotDecoder, pRD, inputFlags, pCallBack, hCallBack);
    }
}

void GcInfoDecoder::ReportUntrackedSlots(
                GcSlotDecoder&      slotDecoder,
                PREGDISPLAY         pRD,
                unsigned            inputFlags,
                GCEnumCallback      pCallBack,
                void *              hCallBack
                )
{
    for(UINT32 slotIndex = slotDecoder.GetNumTracked(); slotIndex < slotDecoder.GetNumSlots(); slotIndex++)
    {
        ReportSlotToGC(slotDecoder,
                       slotIndex,
                       pRD,
                       true, // Report everything (although there should *never* be any scratch slots that are untracked)
                       inputFlags,
                       pCallBack,
                       hCallBack
                       );
    }
}

void GcSlotDecoder::DecodeSlotTable(BitStreamReader& reader)
{
    if (reader.ReadOneFast())
    {
        m_NumRegisters = (UINT32) reader.DecodeVarLengthUnsigned(NUM_REGISTERS_ENCBASE);
    }
    else
    {
        m_NumRegisters = 0;
    }
    UINT32 numStackSlots;
    if (reader.ReadOneFast())
    {
        numStackSlots = (UINT32) reader.DecodeVarLengthUnsigned(NUM_STACK_SLOTS_ENCBASE);
        m_NumUntracked = (UINT32) reader.DecodeVarLengthUnsigned(NUM_UNTRACKED_SLOTS_ENCBASE);
    }
    else
    {
        numStackSlots = 0;
        m_NumUntracked = 0;
    }
    m_NumSlots = m_NumRegisters + numStackSlots + m_NumUntracked;

    UINT32 i = 0;

    if(m_NumRegisters > 0)
    {
        // We certainly predecode the first register

        _ASSERTE(i <  MAX_PREDECODED_SLOTS);

        UINT32 normRegNum = (UINT32) reader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
        UINT32 regNum = DENORMALIZE_REGISTER(normRegNum);
        GcSlotFlags flags = (GcSlotFlags) reader.Read(2);

        m_SlotArray[0].Slot.RegisterNumber = regNum;
        m_SlotArray[0].Flags = flags;

        UINT32 loopEnd = _min(m_NumRegisters, MAX_PREDECODED_SLOTS);
        for(i++; i < loopEnd; i++)
        {
            if(flags)
            {
                normRegNum = (UINT32) reader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
                regNum = DENORMALIZE_REGISTER(normRegNum);
                flags = (GcSlotFlags) reader.Read(2);
            }
            else
            {
                UINT32 normRegDelta = (UINT32) reader.DecodeVarLengthUnsigned(REGISTER_DELTA_ENCBASE) + 1;
                normRegNum += normRegDelta;
                regNum = DENORMALIZE_REGISTER(normRegNum);
            }

            m_SlotArray[i].Slot.RegisterNumber = regNum;
            m_SlotArray[i].Flags = flags;
        }
    }

    if((numStackSlots > 0) && (i < MAX_PREDECODED_SLOTS))
    {
        // We have stack slots left and more room to predecode

        GcStackSlotBase spBase = (GcStackSlotBase) reader.Read(2);
        UINT32 normSpOffset = (INT32) reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
        INT32 spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
        GcSlotFlags flags = (GcSlotFlags) reader.Read(2);

        m_SlotArray[i].Slot.Stack.SpOffset = spOffset;
        m_SlotArray[i].Slot.Stack.Base = spBase;
        m_SlotArray[i].Flags = flags;

        UINT32 loopEnd = _min(m_NumRegisters + numStackSlots, MAX_PREDECODED_SLOTS);
        for(i++; i < loopEnd; i++)
        {
            spBase = (GcStackSlotBase) reader.Read(2);

            if(flags)
            {
                normSpOffset = (INT32) reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
                flags = (GcSlotFlags) reader.Read(2);
            }
            else
            {
                INT32 normSpOffsetDelta = (INT32) reader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
                normSpOffset += normSpOffsetDelta;
                spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
            }

            m_SlotArray[i].Slot.Stack.SpOffset = spOffset;
            m_SlotArray[i].Slot.Stack.Base = spBase;
            m_SlotArray[i].Flags = flags;
        }
    }

    if((m_NumUntracked > 0) && (i < MAX_PREDECODED_SLOTS))
    {
        // We have untracked stack slots left and more room to predecode

        GcStackSlotBase spBase = (GcStackSlotBase) reader.Read(2);
        UINT32 normSpOffset = (INT32) reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
        INT32 spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
        GcSlotFlags flags = (GcSlotFlags) reader.Read(2);

        m_SlotArray[i].Slot.Stack.SpOffset = spOffset;
        m_SlotArray[i].Slot.Stack.Base = spBase;
        m_SlotArray[i].Flags = flags;

        UINT32 loopEnd = _min(m_NumSlots, MAX_PREDECODED_SLOTS);
        for(i++; i < loopEnd; i++)
        {
            spBase = (GcStackSlotBase) reader.Read(2);

            if(flags)
            {
                normSpOffset = (INT32) reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
                flags = (GcSlotFlags) reader.Read(2);
            }
            else
            {
                INT32 normSpOffsetDelta = (INT32) reader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
                normSpOffset += normSpOffsetDelta;
                spOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
            }

            m_SlotArray[i].Slot.Stack.SpOffset = spOffset;
            m_SlotArray[i].Slot.Stack.Base = spBase;
            m_SlotArray[i].Flags = flags;
        }
    }

    // Done pre-decoding

    if(i < m_NumSlots)
    {
        // Prepare for lazy decoding

        _ASSERTE(i == MAX_PREDECODED_SLOTS);
        m_NumDecodedSlots = i;
        m_pLastSlot = &m_SlotArray[MAX_PREDECODED_SLOTS - 1];

        m_SlotReader = reader;

        // Move the argument reader past the end of the table

        GcSlotFlags flags = m_pLastSlot->Flags;

        // Skip any remaining registers

        for(; i < m_NumRegisters; i++)
        {
            if(flags)
            {
                reader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
                flags = (GcSlotFlags) reader.Read(2);
            }
            else
            {
                reader.DecodeVarLengthUnsigned(REGISTER_DELTA_ENCBASE);
            }
        }

        if(numStackSlots > 0)
        {
            if(i == m_NumRegisters)
            {
                // Skip the first stack slot

                reader.Read(2);
                reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                flags = (GcSlotFlags) reader.Read(2);
                i++;
            }

            // Skip any remaining stack slots

            const UINT32 loopEnd = m_NumRegisters + numStackSlots;
            for(; i < loopEnd; i++)
            {
                reader.Read(2);

                if(flags)
                {
                    reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                    flags = (GcSlotFlags) reader.Read(2);
                }
                else
                {
                    reader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
                }
            }
        }

        if(m_NumUntracked > 0)
        {
            if(i == m_NumRegisters + numStackSlots)
            {
                // Skip the first untracked slot

                reader.Read(2);
                reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                flags = (GcSlotFlags) reader.Read(2);
                i++;
            }

            // Skip any remaining untracked slots

            for(; i < m_NumSlots; i++)
            {
                reader.Read(2);

                if(flags)
                {
                    reader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                    flags = (GcSlotFlags) reader.Read(2);
                }
                else
                {
                    reader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
                }
            }
        }
    }
}

const GcSlotDesc* GcSlotDecoder::GetSlotDesc(UINT32 slotIndex)
{
    _ASSERTE(slotIndex < m_NumSlots);

    if(slotIndex < MAX_PREDECODED_SLOTS)
    {
        return &m_SlotArray[slotIndex];
    }

    _ASSERTE(m_NumDecodedSlots >= MAX_PREDECODED_SLOTS && m_NumDecodedSlots < m_NumSlots);
    _ASSERTE(m_NumDecodedSlots <= slotIndex);

    while(m_NumDecodedSlots <= slotIndex)
    {
        if(m_NumDecodedSlots < m_NumRegisters)
        {
            //
            // Decode a register
            //

            if(m_NumDecodedSlots == 0)
            {
                // Decode the first register
                UINT32 normRegNum = (UINT32) m_SlotReader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
                m_pLastSlot->Slot.RegisterNumber = DENORMALIZE_REGISTER(normRegNum);
                m_pLastSlot->Flags = (GcSlotFlags) m_SlotReader.Read(2);
            }
            else
            {
                if(m_pLastSlot->Flags)
                {
                    UINT32 normRegNum = (UINT32) m_SlotReader.DecodeVarLengthUnsigned(REGISTER_ENCBASE);
                    m_pLastSlot->Slot.RegisterNumber = DENORMALIZE_REGISTER(normRegNum);
                    m_pLastSlot->Flags = (GcSlotFlags) m_SlotReader.Read(2);
                }
                else
                {
                    UINT32 normRegDelta = (UINT32) m_SlotReader.DecodeVarLengthUnsigned(REGISTER_DELTA_ENCBASE) + 1;
                    UINT32 normRegNum = normRegDelta + NORMALIZE_REGISTER(m_pLastSlot->Slot.RegisterNumber);
                    m_pLastSlot->Slot.RegisterNumber = DENORMALIZE_REGISTER(normRegNum);
                }
            }
        }
        else
        {
            //
            // Decode a stack slot
            //

            if((m_NumDecodedSlots == m_NumRegisters) || (m_NumDecodedSlots == GetNumTracked()))
            {
                // Decode the first stack slot or first untracked slot
                m_pLastSlot->Slot.Stack.Base = (GcStackSlotBase) m_SlotReader.Read(2);
                UINT32 normSpOffset = (INT32) m_SlotReader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                m_pLastSlot->Slot.Stack.SpOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
                m_pLastSlot->Flags = (GcSlotFlags) m_SlotReader.Read(2);
            }
            else
            {
                m_pLastSlot->Slot.Stack.Base = (GcStackSlotBase) m_SlotReader.Read(2);

                if(m_pLastSlot->Flags)
                {
                    INT32 normSpOffset = (INT32) m_SlotReader.DecodeVarLengthSigned(STACK_SLOT_ENCBASE);
                    m_pLastSlot->Slot.Stack.SpOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
                    m_pLastSlot->Flags = (GcSlotFlags) m_SlotReader.Read(2);
                }
                else
                {
                    INT32 normSpOffsetDelta = (INT32) m_SlotReader.DecodeVarLengthUnsigned(STACK_SLOT_DELTA_ENCBASE);
                    INT32 normSpOffset = normSpOffsetDelta + NORMALIZE_STACK_SLOT(m_pLastSlot->Slot.Stack.SpOffset);
                    m_pLastSlot->Slot.Stack.SpOffset = DENORMALIZE_STACK_SLOT(normSpOffset);
                }
            }
        }

        m_NumDecodedSlots++;
    }

    return m_pLastSlot;
}


//-----------------------------------------------------------------------------
// Platform-specific methods
//-----------------------------------------------------------------------------

#if defined(TARGET_AMD64)


OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

#ifdef FEATURE_NATIVEAOT
    PTR_uintptr_t* ppRax = &pRD->pRax;
    if (regNum > 4) regNum--; // rsp is skipped in NativeAOT RegDisplay
#else
    // The fields of KNONVOLATILE_CONTEXT_POINTERS are in the same order as
    // the processor encoding numbers.

    ULONGLONG **ppRax = &pRD->pCurrentContextPointers->Rax;
#endif

    return (OBJECTREF*)*(ppRax + regNum);
}

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
OBJECTREF* GcInfoDecoder::GetCapturedRegister(
    int             regNum,
    PREGDISPLAY     pRD
    )
{
    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    // The fields of CONTEXT are in the same order as
    // the processor encoding numbers.

    ULONGLONG *pRax = &pRD->pCurrentContext->Rax;

    return (OBJECTREF*)(pRax + regNum);
}
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    UINT16 PreservedRegMask =
          (1 << 3)  // rbx
        | (1 << 5)  // rbp
#ifndef UNIX_AMD64_ABI
        | (1 << 6)  // rsi
        | (1 << 7)  // rdi
#endif // UNIX_AMD64_ABI
        | (1 << 12)  // r12
        | (1 << 13)  // r13
        | (1 << 14)  // r14
        | (1 << 15); // r15

    return !(PreservedRegMask & (1 << regNum));
}


bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    TADDR pSlot = (TADDR) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif
}


void GcInfoDecoder::ReportRegisterToGC(  // AMD64
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    _ASSERTE(regNum >= 0 && regNum <= 16);
    _ASSERTE(regNum != 4);  // rsp

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );
#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT) && !defined(SOS_TARGET_AMD64)
    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, the context
    // pointers for some nonvolatile registers are NULL.
    // In such case, we let the pObjRef point to the captured register
    // value in the context and pin the object itself.
    if (pObjRef == NULL)
    {
        // Report a pinned object to GC only in the promotion phase when the
        // GC is scanning roots.
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);
        if (!pGCCtx->sc->promotion)
        {
            return;
        }

        pObjRef = GetCapturedRegister(regNum, pRD);

        gcFlags |= GC_CALL_PINNED;
    }
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT && !SOS_TARGET_AMD64

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif //_DEBUG

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(regNum, 0, false)));
}

#elif defined(TARGET_ARM)

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE(regNum >= 0 && regNum <= 14);
    _ASSERTE(regNum != 13);  // sp

#ifdef FEATURE_NATIVEAOT
    if(regNum < 14)
    {
        PTR_uintptr_t* ppReg = &pRD->pR0;
        return (OBJECTREF*)*(ppReg + regNum);
    }
    else
    {
        return (OBJECTREF*) pRD->pLR;
    }
#else
    DWORD **ppReg;

    if(regNum <= 3)
    {
        ppReg = &pRD->volatileCurrContextPointers.R0;
        return (OBJECTREF*)*(ppReg + regNum);
    }
    else if(regNum == 12)
    {
        return (OBJECTREF*) pRD->volatileCurrContextPointers.R12;
    }
    else if(regNum == 14)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Lr;
    }

    ppReg = &pRD->pCurrentContextPointers->R4;

    return (OBJECTREF*)*(ppReg + regNum-4);
#endif
}

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
OBJECTREF* GcInfoDecoder::GetCapturedRegister(
    int             regNum,
    PREGDISPLAY     pRD
    )
{
    _ASSERTE(regNum >= 0 && regNum <= 14);
    _ASSERTE(regNum != 13);  // sp

    // The fields of CONTEXT are in the same order as
    // the processor encoding numbers.

    ULONG *pR0 = &pRD->pCurrentContext->R0;

    return (OBJECTREF*)(pR0 + regNum);
}
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT


bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 14);
    _ASSERTE(regNum != 13);  // sp

    return regNum <= 3 || regNum >= 12; // R12 and R14/LR are both scratch registers
}


bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    TADDR pSlot = (TADDR) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif
}


void GcInfoDecoder::ReportRegisterToGC(  // ARM
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    _ASSERTE(regNum >= 0 && regNum <= 14);
    _ASSERTE(regNum != 13);  // sp

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif //_DEBUG

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(regNum, 0, false)));
}

#elif defined(TARGET_ARM64)

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE(regNum >= 0 && regNum <= 30);
    _ASSERTE(regNum != 18); // TEB

#ifdef FEATURE_NATIVEAOT
    PTR_uintptr_t* ppReg = &pRD->pX0;

    return (OBJECTREF*)*(ppReg + regNum);
#else
    DWORD64 **ppReg;

    if(regNum <= 17)
    {
        ppReg = &pRD->volatileCurrContextPointers.X0;
        return (OBJECTREF*)*(ppReg + regNum);
    }
    else if(regNum == 29)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Fp;
    }
    else if(regNum == 30)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Lr;
    }

    ppReg = &pRD->pCurrentContextPointers->X19;

    return (OBJECTREF*)*(ppReg + regNum-19);
#endif
}

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 30);
    _ASSERTE(regNum != 18);

    return regNum <= 17 || regNum >= 29; // R12 and R14/LR are both scratch registers
}

bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    TADDR pSlot = (TADDR) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif

}

void GcInfoDecoder::ReportRegisterToGC( // ARM64
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    _ASSERTE(regNum >= 0 && regNum <= 30);
    _ASSERTE(regNum != 18);

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );
#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT) && !defined(SOS_TARGET_AMD64)
    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, the context
    // pointers for some nonvolatile registers are NULL.
    // In such case, we let the pObjRef point to the captured register
    // value in the context and pin the object itself.
    if (pObjRef == NULL)
    {
        // Report a pinned object to GC only in the promotion phase when the
        // GC is scanning roots.
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);
        if (!pGCCtx->sc->promotion)
        {
            return;
        }

        pObjRef = GetCapturedRegister(regNum, pRD);

        gcFlags |= GC_CALL_PINNED;
    }
#endif // TARGET_UNIX && !SOS_TARGET_ARM64

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif //_DEBUG

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(regNum, 0, false)));
}

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
OBJECTREF* GcInfoDecoder::GetCapturedRegister(
    int             regNum,
    PREGDISPLAY     pRD
    )
{
    _ASSERTE(regNum >= 0 && regNum <= 30);
    _ASSERTE(regNum != 18);

    if (regNum == 29)
    {
        return (OBJECTREF*) &pRD->pCurrentContext->Fp;
    }
    else if (regNum == 30)
    {
        return (OBJECTREF*) &pRD->pCurrentContext->Lr;
    }

    // The fields of CONTEXT are in the same order as
    // the processor encoding numbers.

    DWORD64 *pX0 = &pRD->pCurrentContext->X0;

    return (OBJECTREF*)(pX0 + regNum);
}
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT

#elif defined(TARGET_LOONGARCH64)

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
OBJECTREF* GcInfoDecoder::GetCapturedRegister(
    int             regNum,
    PREGDISPLAY     pRD
    )
{
    _ASSERTE(regNum >= 1 && regNum <= 31);

    // The fields of CONTEXT are in the same order as
    // the processor encoding numbers.

    DWORD64 *pR0 = &pRD->pCurrentContext->R0;

    return (OBJECTREF*)(pR0 + regNum);
}
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE((regNum == 1) || (regNum >= 4 && regNum <= 31));

#ifdef FEATURE_NATIVEAOT
    PTR_uintptr_t* ppReg = &pRD->pR0;

    return (OBJECTREF*)*(ppReg + regNum);
#else
    if(regNum == 1)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Ra;
    }
    else if(regNum == 22)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Fp;
    }
    else if (regNum < 22)
    {
        return (OBJECTREF*)*(DWORD64**)(&pRD->volatileCurrContextPointers.A0 + (regNum - 4));//A0=4.
    }

    return (OBJECTREF*)*(DWORD64**)(&pRD->pCurrentContextPointers->S0 + (regNum-23));
#endif
}

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 31);

    return (regNum <= 21 && ((regNum >= 4) || (regNum == 1)));
}

bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    TADDR pSlot = (TADDR) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif
}

void GcInfoDecoder::ReportRegisterToGC(
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    _ASSERTE(regNum > 0 && regNum <= 31);

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );
#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT) && !defined(SOS_TARGET_AMD64)

    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, the context
    // pointers for some nonvolatile registers are NULL.
    // In such case, we let the pObjRef point to the captured register
    // value in the context and pin the object itself.
    if (pObjRef == NULL)
    {
        // Report a pinned object to GC only in the promotion phase when the
        // GC is scanning roots.
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);
        if (!pGCCtx->sc->promotion)
        {
            return;
        }

        pObjRef = GetCapturedRegister(regNum, pRD);

        gcFlags |= GC_CALL_PINNED;
    }
#endif // TARGET_UNIX && !SOS_TARGET_ARM64

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif //_DEBUG

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(regNum, 0, false)));
}

#elif defined(TARGET_RISCV64)

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
OBJECTREF* GcInfoDecoder::GetCapturedRegister(
    int             regNum,
    PREGDISPLAY     pRD
    )
{
    _ASSERTE(regNum >= 1 && regNum <= 31);

    // The fields of CONTEXT are in the same order as
    // the processor encoding numbers.

    DWORD64 *pR0 = &pRD->pCurrentContext->R0;

    return (OBJECTREF*)(pR0 + regNum);
}
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    _ASSERTE((regNum == 1) || (regNum >= 5 && regNum <= 31));

#ifdef FEATURE_NATIVEAOT
    PTR_uintptr_t* ppReg = &pRD->pR0;

    return (OBJECTREF*)*(ppReg + regNum);
#else
    if(regNum == 1)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Ra;
    }
    else if (regNum < 8)
    {
        return (OBJECTREF*)*(DWORD64**)(&pRD->volatileCurrContextPointers.T0 + (regNum - 5));
    }
    else if(regNum == 8)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->Fp;
    }
    else if (regNum == 9)
    {
        return (OBJECTREF*) pRD->pCurrentContextPointers->S1;
    }
    else if (regNum < 18)
    {
        return (OBJECTREF*)*(DWORD64**)(&pRD->volatileCurrContextPointers.A0 + (regNum - 10));
    }
    else if (regNum < 28)
    {
        return (OBJECTREF*)*(DWORD64**)(&pRD->pCurrentContextPointers->S2 + (regNum-18));
    }
    return (OBJECTREF*)*(DWORD64**)(&pRD->volatileCurrContextPointers.T3 + (regNum-28));
#endif
}

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    _ASSERTE(regNum >= 0 && regNum <= 31);

    return (regNum >= 5 && regNum <= 7) || (regNum >= 10 and regNum <= 17) || regNum >= 28 || regNum == 1;
}

bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_Flags & DECODE_GC_LIFETIMES );

    TADDR pSlot = (TADDR) GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(pSlot >= pRD->SP);

    return (pSlot < pRD->SP + m_SizeOfStackOutgoingAndScratchArea);
#else
    return FALSE;
#endif
}

void GcInfoDecoder::ReportRegisterToGC(
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    _ASSERTE(regNum > 0 && regNum <= 31);

    LOG((LF_GCROOTS, LL_INFO1000, "Reporting " FMT_REG, regNum ));

    OBJECTREF* pObjRef = GetRegisterSlot( regNum, pRD );
#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT) && !defined(SOS_TARGET_AMD64)

    // On PAL, we don't always have the context pointers available due to
    // a limitation of an unwinding library. In such case, the context
    // pointers for some nonvolatile registers are NULL.
    // In such case, we let the pObjRef point to the captured register
    // value in the context and pin the object itself.
    if (pObjRef == NULL)
    {
        // Report a pinned object to GC only in the promotion phase when the
        // GC is scanning roots.
        GCCONTEXT* pGCCtx = (GCCONTEXT*)(hCallBack);
        if (!pGCCtx->sc->promotion)
        {
            return;
        }

        pObjRef = GetCapturedRegister(regNum, pRD);

        gcFlags |= GC_CALL_PINNED;
    }
#endif // TARGET_UNIX && !SOS_TARGET_ARM64

#ifdef _DEBUG
    if(IsScratchRegister(regNum, pRD))
    {
        // Scratch registers cannot be reported for non-leaf frames
        _ASSERTE(flags & ActiveStackFrame);
    }

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif //_DEBUG

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(regNum, 0, false)));
}

#else // Unknown platform

OBJECTREF* GcInfoDecoder::GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        )
{
    PORTABILITY_ASSERT("GcInfoDecoder::GetRegisterSlot");
    return NULL;
}

bool GcInfoDecoder::IsScratchRegister(int regNum,  PREGDISPLAY pRD)
{
    PORTABILITY_ASSERT("GcInfoDecoder::IsScratchRegister");
    return false;
}

bool GcInfoDecoder::IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY     pRD)
{
    _ASSERTE( !"NYI" );
    return false;
}

void GcInfoDecoder::ReportRegisterToGC(
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    _ASSERTE( !"NYI" );
}

#endif // Unknown platform


OBJECTREF* GcInfoDecoder::GetStackSlot(
                        INT32           spOffset,
                        GcStackSlotBase spBase,
                        PREGDISPLAY     pRD
                        )
{
    OBJECTREF* pObjRef;

    if( GC_SP_REL == spBase )
    {
        pObjRef = (OBJECTREF*) ((SIZE_T)pRD->SP + spOffset);
    }
    else if( GC_CALLER_SP_REL == spBase )
    {
        pObjRef = (OBJECTREF*) (GET_CALLER_SP(pRD) + spOffset);
    }
    else
    {
        _ASSERTE( GC_FRAMEREG_REL == spBase );
        _ASSERTE( NO_STACK_BASE_REGISTER != m_StackBaseRegister );

        SIZE_T * pFrameReg = (SIZE_T*) GetRegisterSlot(m_StackBaseRegister, pRD);

#if defined(TARGET_UNIX) && !defined(FEATURE_NATIVEAOT)
        // On PAL, we don't always have the context pointers available due to
        // a limitation of an unwinding library. In such case, the context
        // pointers for some nonvolatile registers are NULL.
        if (pFrameReg == NULL)
        {
            pFrameReg = (SIZE_T*) GetCapturedRegister(m_StackBaseRegister, pRD);
        }
#endif // TARGET_UNIX && !FEATURE_NATIVEAOT

        pObjRef = (OBJECTREF*)(*pFrameReg + spOffset);
    }

    return pObjRef;
}

#ifdef DACCESS_COMPILE
int GcInfoDecoder::GetStackReg(int spBase)
{
#if defined(TARGET_AMD64)
    int esp = 4;
#elif defined(TARGET_ARM)
    int esp = 13;
#elif defined(TARGET_ARM64)
    int esp = 31;
#elif defined(TARGET_LOONGARCH64)
    int esp = 3;
#elif defined(TARGET_RISCV64)
    int esp = 2;
#endif

    if( GC_SP_REL == spBase )
        return esp;
    else if ( GC_CALLER_SP_REL == spBase )
        return -(esp+1);
    else
        return m_StackBaseRegister;
}
#endif // DACCESS_COMPILE

void GcInfoDecoder::ReportStackSlotToGC(
                                INT32           spOffset,
                                GcStackSlotBase spBase,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack)
{
    GCINFODECODER_CONTRACT;

    OBJECTREF* pObjRef = GetStackSlot(spOffset, spBase, pRD);
    _ASSERTE(IS_ALIGNED(pObjRef, sizeof(OBJECTREF*)));

#ifdef _DEBUG
    LOG((LF_GCROOTS, LL_INFO1000, /* Part One */
             "Reporting %s" FMT_STK,
             ( (GC_SP_REL        == spBase) ? "" :
              ((GC_CALLER_SP_REL == spBase) ? "caller's " :
              ((GC_FRAMEREG_REL  == spBase) ? "frame " : "<unrecognized GcStackSlotBase> "))),
             DBG_STK(spOffset) ));

    LOG((LF_GCROOTS, LL_INFO1000, /* Part Two */
         "at" FMT_ADDR "as ", DBG_ADDR(pObjRef) ));

    VALIDATE_ROOT((gcFlags & GC_CALL_INTERIOR), hCallBack, pObjRef);

    LOG_PIPTR(pObjRef, gcFlags, hCallBack);
#endif

    gcFlags |= CHECK_APP_DOMAIN;

    pCallBack(hCallBack, pObjRef, gcFlags DAC_ARG(DacSlotLocation(GetStackReg(spBase), spOffset, true)));
}


#endif // USE_GC_INFO_DECODER

