//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*****************************************************************************
 *
 * GC Information Encoding API
 *
 */

#include "gcinfoencoder.h"

#ifdef VERIFY_GCINFO
#include "dbggcinfoencoder.h"
#endif

#ifdef _DEBUG
    #ifndef LOGGING
        #define LOGGING
    #endif
#endif

#ifndef STANDALONE_BUILD
#include "log.h"
#include "simplerhash.h"
#endif

#ifdef MDIL
#define MUST_CALL_JITALLOCATOR_FREE 1
#endif 

typedef SimplerHashTable< const BitArray *, LiveStateFuncs, UINT32, DefaultSimplerHashBehavior > LiveStateHashTable;

#ifdef MEASURE_GCINFO
// Fi = fully-interruptible; we count any method that has one or more interruptible ranges
// Pi = partially-interruptible; methods with zero fully-interruptible ranges
GcInfoSize g_FiGcInfoSize;
GcInfoSize g_PiGcInfoSize;


void GcInfoSize::Log(DWORD level, const char * header)
{
    if(LoggingOn(LF_GCINFO, level))
    {
        LogSpew(LF_GCINFO, level, header);            

        LogSpew(LF_GCINFO, level, "---COUNTS---\n");
        LogSpew(LF_GCINFO, level, "NumMethods: %Iu\n", NumMethods);
        LogSpew(LF_GCINFO, level, "NumCallSites: %Iu\n", NumCallSites);
        LogSpew(LF_GCINFO, level, "NumRanges: %Iu\n", NumRanges);
        LogSpew(LF_GCINFO, level, "NumRegs: %Iu\n", NumRegs);
        LogSpew(LF_GCINFO, level, "NumStack: %Iu\n", NumStack);
        LogSpew(LF_GCINFO, level, "NumEh: %Iu\n", NumEh);
        LogSpew(LF_GCINFO, level, "NumTransitions: %Iu\n", NumTransitions);
        LogSpew(LF_GCINFO, level, "SizeOfCode: %Iu\n", SizeOfCode);

        LogSpew(LF_GCINFO, level, "---SIZES(bits)---\n");
        LogSpew(LF_GCINFO, level, "Total: %Iu\n", TotalSize);
        LogSpew(LF_GCINFO, level, "Flags: %Iu\n", FlagsSize);
        LogSpew(LF_GCINFO, level, "CodeLength: %Iu\n", CodeLengthSize);
        LogSpew(LF_GCINFO, level, "Prolog/Epilog: %Iu\n", ProEpilogSize);
        LogSpew(LF_GCINFO, level, "SecObj: %Iu\n", SecObjSize);
        LogSpew(LF_GCINFO, level, "GsCookie: %Iu\n", GsCookieSize);
        LogSpew(LF_GCINFO, level, "PspSym: %Iu\n", PspSymSize);
        LogSpew(LF_GCINFO, level, "GenericsCtx: %Iu\n", GenericsCtxSize);
        LogSpew(LF_GCINFO, level, "FrameMarker: %Iu\n", FrameMarkerSize);
        LogSpew(LF_GCINFO, level, "FixedArea: %Iu\n", FixedAreaSize);
        LogSpew(LF_GCINFO, level, "NumCallSites: %Iu\n", NumCallSitesSize);
        LogSpew(LF_GCINFO, level, "NumRanges: %Iu\n", NumRangesSize);
        LogSpew(LF_GCINFO, level, "CallSiteOffsets: %Iu\n", CallSitePosSize);
        LogSpew(LF_GCINFO, level, "Ranges: %Iu\n", RangeSize);
        LogSpew(LF_GCINFO, level, "NumRegs: %Iu\n", NumRegsSize);
        LogSpew(LF_GCINFO, level, "NumStack: %Iu\n", NumStackSize);
        LogSpew(LF_GCINFO, level, "RegSlots: %Iu\n", RegSlotSize);
        LogSpew(LF_GCINFO, level, "StackSlots: %Iu\n", StackSlotSize);
        LogSpew(LF_GCINFO, level, "CallSiteStates: %Iu\n", CallSiteStateSize);
        LogSpew(LF_GCINFO, level, "NumEh: %Iu\n", NumEhSize);
        LogSpew(LF_GCINFO, level, "EhOffsets: %Iu\n", EhPosSize);
        LogSpew(LF_GCINFO, level, "EhStates: %Iu\n", EhStateSize);
        LogSpew(LF_GCINFO, level, "ChunkPointers: %Iu\n", ChunkPtrSize);
        LogSpew(LF_GCINFO, level, "ChunkMasks: %Iu\n", ChunkMaskSize);
        LogSpew(LF_GCINFO, level, "ChunkFinalStates: %Iu\n", ChunkFinalStateSize);
        LogSpew(LF_GCINFO, level, "Transitions: %Iu\n", ChunkTransitionSize);
    }
}

#endif

#ifndef DISABLE_EH_VECTORS
inline BOOL IsEssential(EE_ILEXCEPTION_CLAUSE *pClause)
{
    _ASSERTE(pClause->TryEndPC >= pClause->TryStartPC);
     if(pClause->TryEndPC == pClause->TryStartPC)
        return FALSE;

     return TRUE;
}
#endif

GcInfoEncoder::GcInfoEncoder(
            ICorJitInfo*                pCorJitInfo,
            CORINFO_METHOD_INFO*        pMethodInfo,
            IAllocator*                 pJitAllocator
            )
    :   m_Info1( pJitAllocator ),
        m_Info2( pJitAllocator ),
        m_InterruptibleRanges(),
        m_LifetimeTransitions()
#ifdef VERIFY_GCINFO
        , m_DbgEncoder(pCorJitInfo, pMethodInfo, pJitAllocator)
#endif    
{
#ifdef MEASURE_GCINFO
    // This causes multiple complus.log files in JIT64.  TODO: consider using ICorJitInfo::logMsg instead.
    InitializeLogging();
#endif

    _ASSERTE( pCorJitInfo != NULL );
    _ASSERTE( pMethodInfo != NULL );
    _ASSERTE( pJitAllocator != NULL );

    m_pCorJitInfo = pCorJitInfo;
    m_pMethodInfo = pMethodInfo;
    m_pAllocator = pJitAllocator;

#ifdef _DEBUG
    CORINFO_METHOD_HANDLE methodHandle = pMethodInfo->ftn;

    // Get the name of the current method along with the enclosing class
    // or module name.
    m_MethodName =
        pCorJitInfo->getMethodName(methodHandle, (const char **)&m_ModuleName);
#endif


    m_SlotTableSize = m_SlotTableInitialSize;
    m_SlotTable = (GcSlotDesc*) m_pAllocator->Alloc( m_SlotTableSize*sizeof(GcSlotDesc) );
    m_NumSlots = 0;
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    m_pCallSites = NULL;
    m_pCallSiteSizes = NULL;
    m_NumCallSites = 0;
#endif

    m_SecurityObjectStackSlot = NO_SECURITY_OBJECT;
    m_GSCookieStackSlot = NO_GS_COOKIE;
    m_GSCookieValidRangeStart = 0;
    _ASSERTE(sizeof(m_GSCookieValidRangeEnd) == sizeof(UINT32));
    m_GSCookieValidRangeEnd = (UINT32) (-1); // == UINT32.MaxValue
    m_PSPSymStackSlot = NO_PSP_SYM;
    m_GenericsInstContextStackSlot = NO_GENERICS_INST_CONTEXT;
    m_contextParamType = GENERIC_CONTEXTPARAM_NONE;

    m_StackBaseRegister = NO_STACK_BASE_REGISTER;
    m_SizeOfEditAndContinuePreservedArea = NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
    m_WantsReportOnlyLeaf = false;
    m_IsVarArg = false;
    m_pLastInterruptibleRange = NULL;
    
#ifdef _DEBUG
    m_IsSlotTableFrozen = FALSE;
    m_CodeLength = 0;
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    m_SizeOfStackOutgoingAndScratchArea = -1;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA
#endif //_DEBUG
}

GcSlotId GcInfoEncoder::GetRegisterSlotId( UINT32 regNum, GcSlotFlags flags )
{
    // We could lookup an existing identical slot in the slot table (via some hashtable mechanism).
    // We just create duplicates for now.

#ifdef _DEBUG
    _ASSERTE( !m_IsSlotTableFrozen );
#endif

    if( m_NumSlots == m_SlotTableSize )
    {
        GrowSlotTable();
    }
    _ASSERTE( m_NumSlots < m_SlotTableSize );

    _ASSERTE( (flags & (GC_SLOT_IS_REGISTER | GC_SLOT_IS_DELETED | GC_SLOT_UNTRACKED)) == 0 );
    m_SlotTable[ m_NumSlots ].Slot.RegisterNumber = regNum;
    m_SlotTable[ m_NumSlots ].Flags = (GcSlotFlags) (flags | GC_SLOT_IS_REGISTER);

    GcSlotId newSlotId;
    newSlotId = m_NumSlots++;

#ifdef VERIFY_GCINFO
     GcSlotId dbgSlotId = m_DbgEncoder.GetRegisterSlotId(regNum, flags);
     _ASSERTE(dbgSlotId == newSlotId);
#endif   

    return newSlotId;
}

GcSlotId GcInfoEncoder::GetStackSlotId( INT32 spOffset, GcSlotFlags flags, GcStackSlotBase spBase )
{
    // We could lookup an existing identical slot in the slot table (via some hashtable mechanism).
    // We just create duplicates for now.

#ifdef _DEBUG
    _ASSERTE( !m_IsSlotTableFrozen );
#endif

    if( m_NumSlots == m_SlotTableSize )
    {
        GrowSlotTable();
    }
    _ASSERTE( m_NumSlots < m_SlotTableSize );

    // Not valid to reference anything below the current stack pointer
    _ASSERTE(GC_SP_REL != spBase || spOffset >= 0);

    _ASSERTE( (flags & (GC_SLOT_IS_REGISTER | GC_SLOT_IS_DELETED)) == 0 );

    // the spOffset for the stack slot is required to be pointer size aligned
    _ASSERTE((spOffset % TARGET_POINTER_SIZE) == 0);

    m_SlotTable[ m_NumSlots ].Slot.Stack.SpOffset = spOffset;
    m_SlotTable[ m_NumSlots ].Slot.Stack.Base = spBase;
    m_SlotTable[ m_NumSlots ].Flags = flags;

    GcSlotId newSlotId;
    newSlotId = m_NumSlots++;

#ifdef VERIFY_GCINFO
     GcSlotId dbgSlotId = m_DbgEncoder.GetStackSlotId(spOffset, flags, spBase);
     _ASSERTE(dbgSlotId == newSlotId);
#endif    

    return newSlotId;
}

void GcInfoEncoder::GrowSlotTable()
{
    m_SlotTableSize *= 2;
    GcSlotDesc* newSlotTable = (GcSlotDesc*) m_pAllocator->Alloc( m_SlotTableSize * sizeof(GcSlotDesc) );
    memcpy( newSlotTable, m_SlotTable, m_NumSlots * sizeof(GcSlotDesc) );

#ifdef MUST_CALL_JITALLOCATOR_FREE
    m_pAllocator->Free( m_SlotTable );
#endif

    m_SlotTable = newSlotTable;
}

void GcInfoEncoder::DefineInterruptibleRange( UINT32 startInstructionOffset, UINT32 length )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.DefineInterruptibleRange(startInstructionOffset, length);
#endif    

    UINT32 stopInstructionOffset = startInstructionOffset + length;

    UINT32 normStartOffset = NORMALIZE_CODE_OFFSET(startInstructionOffset);
    UINT32 normStopOffset = NORMALIZE_CODE_OFFSET(stopInstructionOffset);

    // Ranges must not overlap and must be passed sorted by increasing offset
    _ASSERTE(   
        m_pLastInterruptibleRange == NULL ||
        normStartOffset >= m_pLastInterruptibleRange->NormStopOffset
        );

    // Ignore empty ranges
    if(normStopOffset > normStartOffset)
    {
        if(m_pLastInterruptibleRange 
            && normStartOffset == m_pLastInterruptibleRange->NormStopOffset)
        {
            // Merge adjacent ranges
            m_pLastInterruptibleRange->NormStopOffset = normStopOffset;
        }
        else
        {
            InterruptibleRange range;
            range.NormStartOffset = normStartOffset;
            range.NormStopOffset = normStopOffset;
            m_pLastInterruptibleRange = m_InterruptibleRanges.AppendThrowing();
            *m_pLastInterruptibleRange = range;
        }
    }

    LOG((LF_GCINFO, LL_INFO1000000, "interruptible at %x length %x\n", startInstructionOffset, length));
}



//
// For inputs, pass zero as offset
//
void GcInfoEncoder::SetSlotState(
                            UINT32      instructionOffset,
                            GcSlotId    slotId,
                            GcSlotState slotState
                            )
{
    _ASSERTE( (m_SlotTable[ slotId ].Flags & GC_SLOT_UNTRACKED) == 0 );

#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetSlotState(instructionOffset, slotId, slotState);
#endif    

    LifetimeTransition transition;

    transition.SlotId = slotId;
    transition.CodeOffset = instructionOffset;
    transition.BecomesLive = ( slotState == GC_SLOT_LIVE );
    transition.IsDeleted = FALSE;

    *( m_LifetimeTransitions.AppendThrowing() ) = transition;

    LOG((LF_GCINFO, LL_INFO1000000, LOG_GCSLOTDESC_FMT " %s at %x\n", LOG_GCSLOTDESC_ARGS(&m_SlotTable[slotId]), slotState == GC_SLOT_LIVE ? "live" : "dead", instructionOffset));
}


void GcInfoEncoder::SetIsVarArg()
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetIsVarArg();
#endif    

    m_IsVarArg = true;
}

void GcInfoEncoder::SetCodeLength( UINT32 length )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetCodeLength(length);
#endif    

    _ASSERTE( length > 0 );
    _ASSERTE( m_CodeLength == 0 || m_CodeLength == length );
    m_CodeLength = length;
}


void GcInfoEncoder::SetSecurityObjectStackSlot( INT32 spOffset )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetSecurityObjectStackSlot(spOffset);
#endif    

    _ASSERTE( spOffset != NO_SECURITY_OBJECT );
#if defined(_TARGET_AMD64_)
    _ASSERTE( spOffset < 0x10 && "The security object cannot reside in an input variable!" );
#endif
    _ASSERTE( m_SecurityObjectStackSlot == NO_SECURITY_OBJECT || m_SecurityObjectStackSlot == spOffset );
    
    m_SecurityObjectStackSlot  = spOffset;
}

void GcInfoEncoder::SetPrologSize( UINT32 prologSize )
{
    _ASSERTE(prologSize != 0);
    _ASSERTE(m_GSCookieValidRangeStart == 0 || m_GSCookieValidRangeStart == prologSize);
    _ASSERTE(m_GSCookieValidRangeEnd == (UINT32)(-1) || m_GSCookieValidRangeEnd == prologSize+1);
 
    m_GSCookieValidRangeStart = prologSize;
    // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
    m_GSCookieValidRangeEnd   = prologSize+1;
}

void GcInfoEncoder::SetGSCookieStackSlot( INT32 spOffsetGSCookie, UINT32 validRangeStart, UINT32 validRangeEnd )
{
    _ASSERTE( spOffsetGSCookie != NO_GS_COOKIE );
    _ASSERTE( m_GSCookieStackSlot == NO_GS_COOKIE || m_GSCookieStackSlot == spOffsetGSCookie );
    _ASSERTE( validRangeStart < validRangeEnd );

    m_GSCookieStackSlot       = spOffsetGSCookie;
    m_GSCookieValidRangeStart = validRangeStart;
    m_GSCookieValidRangeEnd   = validRangeEnd;
}

void GcInfoEncoder::SetPSPSymStackSlot( INT32 spOffsetPSPSym )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetPSPSymStackSlot(spOffsetPSPSym);
#endif    

    _ASSERTE( spOffsetPSPSym != NO_PSP_SYM );
    _ASSERTE( m_PSPSymStackSlot == NO_PSP_SYM || m_PSPSymStackSlot == spOffsetPSPSym );

    m_PSPSymStackSlot              = spOffsetPSPSym;
}

void GcInfoEncoder::SetGenericsInstContextStackSlot( INT32 spOffsetGenericsContext, GENERIC_CONTEXTPARAM_TYPE type)
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetGenericsInstContextStackSlot(spOffsetGenericsContext);
#endif    

    _ASSERTE( spOffsetGenericsContext != NO_GENERICS_INST_CONTEXT);
    _ASSERTE( m_GenericsInstContextStackSlot == NO_GENERICS_INST_CONTEXT || m_GenericsInstContextStackSlot == spOffsetGenericsContext );

    m_GenericsInstContextStackSlot = spOffsetGenericsContext;
    m_contextParamType = type;
}

void GcInfoEncoder::SetStackBaseRegister( UINT32 regNum )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetStackBaseRegister(regNum);
#endif    

    _ASSERTE( regNum != NO_STACK_BASE_REGISTER );
    _ASSERTE(DENORMALIZE_STACK_BASE_REGISTER(NORMALIZE_STACK_BASE_REGISTER(regNum)) == regNum);
    _ASSERTE( m_StackBaseRegister == NO_STACK_BASE_REGISTER || m_StackBaseRegister == regNum );
    m_StackBaseRegister = regNum;
}

void GcInfoEncoder::SetSizeOfEditAndContinuePreservedArea( UINT32 slots )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetSizeOfEditAndContinuePreservedArea(slots);
#endif    

    _ASSERTE( slots != NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA );
    _ASSERTE( m_SizeOfEditAndContinuePreservedArea == NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA );
    m_SizeOfEditAndContinuePreservedArea = slots;
}

void GcInfoEncoder::SetWantsReportOnlyLeaf()
{
    m_WantsReportOnlyLeaf = true;
}

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
void GcInfoEncoder::SetSizeOfStackOutgoingAndScratchArea( UINT32 size )
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.SetSizeOfStackOutgoingAndScratchArea(size);
#endif    

    _ASSERTE( size != (UINT32)-1 );
    _ASSERTE( m_SizeOfStackOutgoingAndScratchArea == (UINT32)-1 || m_SizeOfStackOutgoingAndScratchArea == size );
    m_SizeOfStackOutgoingAndScratchArea = size;
}
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

class SlotTableIndexesQuickSort : public CQuickSort<UINT32>
{
    GcSlotDesc* m_SlotTable;
    
public:
    SlotTableIndexesQuickSort(
        GcSlotDesc*   slotTable,
        UINT32*   pBase,
        size_t               count
        )
        : CQuickSort<UINT32>( pBase, count ), m_SlotTable(slotTable)
    {}

    int Compare( UINT32* a, UINT32* b )
    {
        GcSlotDesc* pFirst = &(m_SlotTable[*a]);
        GcSlotDesc* pSecond = &(m_SlotTable[*b]);

        int firstFlags = pFirst->Flags ^ GC_SLOT_UNTRACKED;
        int secondFlags = pSecond->Flags ^ GC_SLOT_UNTRACKED;

        // All registers come before all stack slots
        // All untracked come last
        // Then sort them by flags, ensuring that the least-frequent interior/pinned flag combinations are first
        // This is accomplished in the comparison of flags, since we encode IsRegister in the highest flag bit
        // And we XOR the UNTRACKED flag to place them last in the second highest flag bit
        if( firstFlags > secondFlags ) return -1;
        if( firstFlags < secondFlags ) return 1;
        
        // Then sort them by slot
        if( pFirst->IsRegister() )
        {
            _ASSERTE( pSecond->IsRegister() );
            if( pFirst->Slot.RegisterNumber < pSecond->Slot.RegisterNumber ) return -1;
            if( pFirst->Slot.RegisterNumber > pSecond->Slot.RegisterNumber ) return 1;
        }
        else
        {
            _ASSERTE( !pSecond->IsRegister() );
            if( pFirst->Slot.Stack.SpOffset < pSecond->Slot.Stack.SpOffset ) return -1;
            if( pFirst->Slot.Stack.SpOffset > pSecond->Slot.Stack.SpOffset ) return 1;

            // This is arbitrary, but we want to make sure they are considered separate slots
            if( pFirst->Slot.Stack.Base < pSecond->Slot.Stack.Base ) return -1;
            if( pFirst->Slot.Stack.Base > pSecond->Slot.Stack.Base ) return 1;
        }

        // If we get here, the slots are identical
        _ASSERTE(!"Duplicate slots definitions found in GC information!");
        return 0;
    }
};


int __cdecl CompareLifetimeTransitionsByOffsetThenSlot(const void* p1, const void* p2)
{
    const GcInfoEncoder::LifetimeTransition* pFirst = (const GcInfoEncoder::LifetimeTransition*) p1;
    const GcInfoEncoder::LifetimeTransition* pSecond = (const GcInfoEncoder::LifetimeTransition*) p2;
        
    UINT32 firstOffset  = pFirst->CodeOffset;
    UINT32 secondOffset = pSecond->CodeOffset;

    if (firstOffset == secondOffset)
    {
        return pFirst->SlotId - pSecond->SlotId;
    }
    else
    {
        return firstOffset - secondOffset;
    }
}


int __cdecl CompareLifetimeTransitionsBySlot(const void* p1, const void* p2)
{
    const GcInfoEncoder::LifetimeTransition* pFirst = (const GcInfoEncoder::LifetimeTransition*) p1;
    const GcInfoEncoder::LifetimeTransition* pSecond = (const GcInfoEncoder::LifetimeTransition*) p2;
        
    UINT32 firstOffset  = pFirst->CodeOffset;
    UINT32 secondOffset = pSecond->CodeOffset;
    
    _ASSERTE(GetNormCodeOffsetChunk(firstOffset) == GetNormCodeOffsetChunk(secondOffset));

    // Sort them by slot
    if( pFirst->SlotId < pSecond->SlotId ) return -1;
    if( pFirst->SlotId > pSecond->SlotId ) return 1;

    // Then sort them by code offset
    if( firstOffset < secondOffset ) 
        return -1;
    else
    {
        _ASSERTE(( firstOffset > secondOffset ) && "Redundant transitions found in GC info!");
        return 1;
    }
}

void BitStreamWriter::Write(BitArray& a, UINT32 count)
{
    size_t* dataPtr = a.DataPtr();
    for(;;)
    {
        if(count <= BITS_PER_SIZE_T)
        {
            Write(*dataPtr, count);
            break;
        }
        Write(*(dataPtr++), BITS_PER_SIZE_T);
        count -= BITS_PER_SIZE_T;
    }   
}

void GcInfoEncoder::FinalizeSlotIds()
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.FinalizeSlotIds();
#endif    

#ifdef _DEBUG
    m_IsSlotTableFrozen = TRUE;
#endif
}

bool GcInfoEncoder::IsAlwaysScratch(GcSlotDesc &slotDesc)
{
#if defined(_TARGET_ARM_)

    _ASSERTE( m_SizeOfStackOutgoingAndScratchArea != (UINT32)-1 );
    if(slotDesc.IsRegister())
    {
        int regNum = (int) slotDesc.Slot.RegisterNumber;
        _ASSERTE(regNum >= 0 && regNum <= 14);
        _ASSERTE(regNum != 13);  // sp

        return ((regNum <= 3) || (regNum >= 12)); // R12 and R14/LR are both scratch registers
    }
    else if (!slotDesc.IsUntracked() && (slotDesc.Slot.Stack.Base == GC_SP_REL) &&
        ((UINT32)slotDesc.Slot.Stack.SpOffset < m_SizeOfStackOutgoingAndScratchArea))
    {
        return TRUE;
    }
    else
        return FALSE;

#elif defined(_TARGET_AMD64_)

    _ASSERTE( m_SizeOfStackOutgoingAndScratchArea != (UINT32)-1 );
    if(slotDesc.IsRegister())
    {
        int regNum = (int) slotDesc.Slot.RegisterNumber;
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
    else if (!slotDesc.IsUntracked() && (slotDesc.Slot.Stack.Base == GC_SP_REL) &&
        ((UINT32)slotDesc.Slot.Stack.SpOffset < m_SizeOfStackOutgoingAndScratchArea))
    {
        return TRUE;
    }
    else
        return FALSE;

#else
    return FALSE;
#endif
}

void GcInfoEncoder::Build()
{
#ifdef VERIFY_GCINFO
     m_DbgEncoder.Build();
#endif    

#ifdef _DEBUG
    _ASSERTE(m_IsSlotTableFrozen || m_NumSlots == 0);
#endif

    _ASSERTE((1 << NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2) == NUM_NORM_CODE_OFFSETS_PER_CHUNK);

    LOG((LF_GCINFO, LL_INFO100,
         "Entering GcInfoEncoder::Build() for method %s[%s]\n",
         m_MethodName, m_ModuleName
         ));


    ///////////////////////////////////////////////////////////////////////
    // Method header
    ///////////////////////////////////////////////////////////////////////

    UINT32 hasSecurityObject = (m_SecurityObjectStackSlot != NO_SECURITY_OBJECT);
    UINT32 hasGSCookie = (m_GSCookieStackSlot != NO_GS_COOKIE);
    UINT32 hasContextParamType = (m_GenericsInstContextStackSlot != NO_GENERICS_INST_CONTEXT);

    BOOL slimHeader = (!m_IsVarArg && !hasSecurityObject && !hasGSCookie && (m_PSPSymStackSlot == NO_PSP_SYM) &&
        !hasContextParamType && !m_WantsReportOnlyLeaf && (m_InterruptibleRanges.Count() == 0) &&
        ((m_StackBaseRegister == NO_STACK_BASE_REGISTER) || (NORMALIZE_STACK_BASE_REGISTER(m_StackBaseRegister) == 0))) &&
        (m_SizeOfEditAndContinuePreservedArea == NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA);

    if (slimHeader)
    {
        // Slim encoding means nothing special, partially interruptible, maybe a default frame register
        GCINFO_WRITE(m_Info1, 0, 1, FlagsSize); // Slim encoding
        GCINFO_WRITE(m_Info1, (m_StackBaseRegister == NO_STACK_BASE_REGISTER) ? 0 : 1, 1, FlagsSize);
    }
    else
    {
        GCINFO_WRITE(m_Info1, 1, 1, FlagsSize); // Fat encoding
        GCINFO_WRITE(m_Info1, (m_IsVarArg ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, (hasSecurityObject ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, (hasGSCookie ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, ((m_PSPSymStackSlot != NO_PSP_SYM) ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, m_contextParamType, 2, FlagsSize);
        GCINFO_WRITE(m_Info1, ((m_StackBaseRegister != NO_STACK_BASE_REGISTER) ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, (m_WantsReportOnlyLeaf ? 1 : 0), 1, FlagsSize);
        GCINFO_WRITE(m_Info1, ((m_SizeOfEditAndContinuePreservedArea != NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA) ? 1 : 0), 1, FlagsSize);
    }

    _ASSERTE( m_CodeLength > 0 );
    GCINFO_WRITE_VARL_U(m_Info1, NORMALIZE_CODE_LENGTH(m_CodeLength), CODE_LENGTH_ENCBASE, CodeLengthSize);

    if(hasGSCookie)
    {
        _ASSERTE(!slimHeader);
        // Save the valid code range, to be used for determining when GS cookie validation 
        // should be performed
        // Encode an intersection of valid offsets
        UINT32 intersectionStart = m_GSCookieValidRangeStart;
        UINT32 intersectionEnd = m_GSCookieValidRangeEnd;

        _ASSERTE(intersectionStart > 0 && intersectionStart < m_CodeLength);
        _ASSERTE(intersectionEnd > 0 && intersectionEnd <= m_CodeLength);
        _ASSERTE(intersectionStart <= intersectionEnd);
        UINT32 normPrologSize = NORMALIZE_CODE_OFFSET(intersectionStart);
        UINT32 normEpilogSize = NORMALIZE_CODE_OFFSET(m_CodeLength) - NORMALIZE_CODE_OFFSET(intersectionEnd);
        _ASSERTE(normPrologSize > 0 && normPrologSize < m_CodeLength);
        _ASSERTE(normEpilogSize >= 0 && normEpilogSize < m_CodeLength);
        
        GCINFO_WRITE_VARL_U(m_Info1, normPrologSize-1, NORM_PROLOG_SIZE_ENCBASE, ProEpilogSize);
        GCINFO_WRITE_VARL_U(m_Info1, normEpilogSize, NORM_EPILOG_SIZE_ENCBASE, ProEpilogSize);
    }
    else if (hasSecurityObject || hasContextParamType)
    {
        _ASSERTE(!slimHeader);
        // Save the prolog size, to be used for determining when it is not safe
        // to report generics param context and the security object 
        _ASSERTE(m_GSCookieValidRangeStart > 0 && m_GSCookieValidRangeStart < m_CodeLength);
        UINT32 normPrologSize = NORMALIZE_CODE_OFFSET(m_GSCookieValidRangeStart);
        _ASSERTE(normPrologSize > 0 && normPrologSize < m_CodeLength);

        GCINFO_WRITE_VARL_U(m_Info1, normPrologSize-1, NORM_PROLOG_SIZE_ENCBASE, ProEpilogSize);
    }

    // Encode the offset to the security object.
    if(hasSecurityObject)
    {
        _ASSERTE(!slimHeader);
#ifdef _DEBUG
        LOG((LF_GCINFO, LL_INFO1000, "Security object at " FMT_STK "\n",
             DBG_STK(m_SecurityObjectStackSlot)
             ));
#endif

        GCINFO_WRITE_VARL_S(m_Info1, NORMALIZE_STACK_SLOT(m_SecurityObjectStackSlot), SECURITY_OBJECT_STACK_SLOT_ENCBASE, SecObjSize);
    }
    
    // Encode the offset to the GS cookie.
    if(hasGSCookie)
    {
        _ASSERTE(!slimHeader);
#ifdef _DEBUG
        LOG((LF_GCINFO, LL_INFO1000, "GS cookie at " FMT_STK "\n",
             DBG_STK(m_GSCookieStackSlot)
             ));
#endif

        GCINFO_WRITE_VARL_S(m_Info1, NORMALIZE_STACK_SLOT(m_GSCookieStackSlot), GS_COOKIE_STACK_SLOT_ENCBASE, GsCookieSize);
    
    }
    
    // Encode the offset to the PSPSym.
    // The PSPSym is relative to the caller SP on IA64 and the initial stack pointer before stack allocations on X64.
    if(m_PSPSymStackSlot != NO_PSP_SYM)
    {
        _ASSERTE(!slimHeader);
#ifdef _DEBUG
        LOG((LF_GCINFO, LL_INFO1000, "Parent PSP at " FMT_STK "\n", DBG_STK(m_PSPSymStackSlot)));
#endif
        GCINFO_WRITE_VARL_S(m_Info1, NORMALIZE_STACK_SLOT(m_PSPSymStackSlot), PSP_SYM_STACK_SLOT_ENCBASE, PspSymSize);
    }

    // Encode the offset to the generics type context.
    if(m_GenericsInstContextStackSlot != NO_GENERICS_INST_CONTEXT)
    {
        _ASSERTE(!slimHeader);
#ifdef _DEBUG
        LOG((LF_GCINFO, LL_INFO1000, "Generics instantiation context at " FMT_STK "\n",
             DBG_STK(m_GenericsInstContextStackSlot)
             ));
#endif
        GCINFO_WRITE_VARL_S(m_Info1, NORMALIZE_STACK_SLOT(m_GenericsInstContextStackSlot), GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE, GenericsCtxSize);
    }

    if(!slimHeader && (m_StackBaseRegister != NO_STACK_BASE_REGISTER))
    {
        GCINFO_WRITE_VARL_U(m_Info1, NORMALIZE_STACK_BASE_REGISTER(m_StackBaseRegister), STACK_BASE_REGISTER_ENCBASE, StackBaseSize);
    }

    if (m_SizeOfEditAndContinuePreservedArea != NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA)
    {
        GCINFO_WRITE_VARL_U(m_Info1, m_SizeOfEditAndContinuePreservedArea, SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, EncPreservedSlots);
    }

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    if (!slimHeader)
    {
        _ASSERTE( m_SizeOfStackOutgoingAndScratchArea != (UINT32)-1 );
        GCINFO_WRITE_VARL_U(m_Info1, NORMALIZE_SIZE_OF_STACK_AREA(m_SizeOfStackOutgoingAndScratchArea), SIZE_OF_STACK_AREA_ENCBASE, FixedAreaSize);
    }
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

    UINT32 numInterruptibleRanges = (UINT32) m_InterruptibleRanges.Count();

    InterruptibleRange *pRanges = NULL;
    if(numInterruptibleRanges)
    {
        pRanges = (InterruptibleRange*) m_pAllocator->Alloc(numInterruptibleRanges * sizeof(InterruptibleRange));
        m_InterruptibleRanges.CopyTo(pRanges);
    }

    int size_tCount = (m_NumSlots + BITS_PER_SIZE_T - 1) / BITS_PER_SIZE_T;
    BitArray liveState(m_pAllocator, size_tCount);
    BitArray couldBeLive(m_pAllocator, size_tCount);


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    _ASSERTE(m_NumCallSites == 0 || m_pCallSites != NULL);

    ///////////////////////////////////////////////////////////////////////
    // Normalize call sites
    // Eliminate call sites that fall inside interruptible ranges
    ///////////////////////////////////////////////////////////////////////

    UINT32 numCallSites = 0;
    for(UINT32 callSiteIndex = 0; callSiteIndex < m_NumCallSites; callSiteIndex++)
    {
        UINT32 callSite = m_pCallSites[callSiteIndex];
        // There's a contract with the EE that says for non-leaf stack frames, where the
        // method is stopped at a call site, the EE will not query with the return PC, but
        // rather the return PC *minus 1*.
        // The reason is that variable/register liveness may change at the instruction immediately after the
        // call, so we want such frames to appear as if they are "within" the call.
        // Since we use "callSite" as the "key" when we search for the matching descriptor, also subtract 1 here
        // (after, of course, adding the size of the call instruction to get the return PC).
        callSite += m_pCallSiteSizes[callSiteIndex] - 1;

        UINT32 normOffset = NORMALIZE_CODE_OFFSET(callSite);

        BOOL keepIt = TRUE;

        for(UINT32 intRangeIndex = 0; intRangeIndex < numInterruptibleRanges; intRangeIndex++)
        {
            InterruptibleRange *pRange = &pRanges[intRangeIndex];
            if(pRange->NormStopOffset > normOffset)
            {
                if(pRange->NormStartOffset <= normOffset)
                {
                    keepIt = FALSE;
                }
                break;
            }
        }

        if(keepIt)
            m_pCallSites[numCallSites++] = normOffset;
    }

    GCINFO_WRITE_VARL_U(m_Info1, NORMALIZE_NUM_SAFE_POINTS(numCallSites), NUM_SAFE_POINTS_ENCBASE, NumCallSitesSize);
    m_NumCallSites = numCallSites;
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    if (slimHeader)
    {
        _ASSERTE(numInterruptibleRanges == 0);
    }
    else
    {
        GCINFO_WRITE_VARL_U(m_Info1, NORMALIZE_NUM_INTERRUPTIBLE_RANGES(numInterruptibleRanges), NUM_INTERRUPTIBLE_RANGES_ENCBASE, NumRangesSize);
    }



#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    ///////////////////////////////////////////////////////////////////////
    // Encode call site offsets
    ///////////////////////////////////////////////////////////////////////

    UINT32 numBitsPerOffset = CeilOfLog2(NORMALIZE_CODE_OFFSET(m_CodeLength));

    for(UINT32 callSiteIndex = 0; callSiteIndex < m_NumCallSites; callSiteIndex++)
    {
        UINT32 normOffset = m_pCallSites[callSiteIndex];

        _ASSERTE(normOffset < (UINT32)1 << (numBitsPerOffset+1));
        GCINFO_WRITE(m_Info1, normOffset, numBitsPerOffset, CallSitePosSize);
    }
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED


    ///////////////////////////////////////////////////////////////////////
    // Encode fully-interruptible ranges
    ///////////////////////////////////////////////////////////////////////

    if(numInterruptibleRanges)
    {
        UINT32 lastStopOffset = 0;
        
        for(UINT32 i = 0; i < numInterruptibleRanges; i++)
        {
            UINT32 normStartOffset = pRanges[i].NormStartOffset;
            UINT32 normStopOffset = pRanges[i].NormStopOffset;

            size_t normStartDelta = normStartOffset - lastStopOffset;
            size_t normStopDelta = normStopOffset - normStartOffset;
            _ASSERTE(normStopDelta > 0);
            
            lastStopOffset = normStopOffset;
            
            GCINFO_WRITE_VARL_U(m_Info1, normStartDelta, INTERRUPTIBLE_RANGE_DELTA1_ENCBASE, RangeSize);

            GCINFO_WRITE_VARL_U(m_Info1, normStopDelta-1, INTERRUPTIBLE_RANGE_DELTA2_ENCBASE, RangeSize);
        }
    }


    ///////////////////////////////////////////////////////////////////////
    // Pre-process transitions
    ///////////////////////////////////////////////////////////////////////


    size_t numTransitions = m_LifetimeTransitions.Count();
    LifetimeTransition *pTransitions = (LifetimeTransition*)m_pAllocator->Alloc(numTransitions * sizeof(LifetimeTransition));
    m_LifetimeTransitions.CopyTo(pTransitions);

    LifetimeTransition* pEndTransitions = pTransitions + numTransitions;
    LifetimeTransition* pCurrent;

    //-----------------------------------------------------------------
    // Sort the lifetime transitions by offset (then by slot id).
    //-----------------------------------------------------------------
    
    // Don't use the CQuickSort algorithm, it's prone to stack overflows
    qsort(
        pTransitions,
        numTransitions,
        sizeof(LifetimeTransition),
        CompareLifetimeTransitionsByOffsetThenSlot
        );

    // Eliminate transitions outside the method
    while(pEndTransitions > pTransitions)
    {
        LifetimeTransition *pPrev = pEndTransitions - 1;
        if(pPrev->CodeOffset < m_CodeLength)
            break;
        
        _ASSERTE(pPrev->CodeOffset == m_CodeLength && !pPrev->BecomesLive);
        pEndTransitions = pPrev;
    }

    // Now eliminate any pairs of dead/live transitions for the same slot at the same offset.
    EliminateRedundantLiveDeadPairs(&pTransitions, &numTransitions, &pEndTransitions);

#ifdef _DEBUG
    numTransitions = -1;
#endif
    ///////////////////////////////////////////////////////////////////////
    // Sort the slot table
    ///////////////////////////////////////////////////////////////////////

    {
        UINT32* sortedSlotIndexes = (UINT32*) m_pAllocator->Alloc(m_NumSlots * sizeof(UINT32));
        UINT32* sortOrder = (UINT32*) m_pAllocator->Alloc(m_NumSlots * sizeof(UINT32));

        for(UINT32 i = 0; i < m_NumSlots; i++)
        {
            sortedSlotIndexes[i] = i;
        }
        
        SlotTableIndexesQuickSort slotTableIndexesQuickSort(
            m_SlotTable,
            sortedSlotIndexes,
            m_NumSlots
            );
        slotTableIndexesQuickSort.Sort();

        for(UINT32 i = 0; i < m_NumSlots; i++)
        {
            sortOrder[sortedSlotIndexes[i]] = i;
        }

        // Re-order the slot table
        GcSlotDesc* pNewSlotTable = (GcSlotDesc*) m_pAllocator->Alloc(sizeof(GcSlotDesc) * m_NumSlots);
        for(UINT32 i = 0; i < m_NumSlots; i++)
        {
            pNewSlotTable[i] = m_SlotTable[sortedSlotIndexes[i]];
        }

        // Update transitions to assign new slot ids
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
        {
            UINT32 newSlotId = sortOrder[pCurrent->SlotId];
            pCurrent->SlotId = newSlotId;
        }

#ifdef MUST_CALL_JITALLOCATOR_FREE
        m_pAllocator->Free( m_SlotTable );
        m_pAllocator->Free( sortedSlotIndexes );
        m_pAllocator->Free( sortOrder );
#endif

        m_SlotTable = pNewSlotTable;
    }


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    ///////////////////////////////////////////////////////////////////////
    // Gather EH information
    ///////////////////////////////////////////////////////////////////////
   
    couldBeLive.ClearAll();

#ifndef DISABLE_EH_VECTORS
    UINT32 numEHClauses;
    EE_ILEXCEPTION *pEHInfo = (EE_ILEXCEPTION*) m_pCorJitInfo->getEHInfo();
    if (!pEHInfo)
        numEHClauses = 0;
    else
        numEHClauses = pEHInfo->EHCount();

    UINT32 numUsedEHClauses = numEHClauses;
    for (UINT32 clauseIndex = 0; clauseIndex < numEHClauses; clauseIndex++)
    {
        EE_ILEXCEPTION_CLAUSE * pClause;
        pClause = pEHInfo->EHClause(clauseIndex);

        if(!IsEssential(pClause))
            numUsedEHClauses--;
    }
    
    UINT32 ehTableBitCount = m_NumSlots * numUsedEHClauses;
    BitArray ehLiveSlots(m_pAllocator, (ehTableBitCount + BITS_PER_SIZE_T - 1) / BITS_PER_SIZE_T);
    ehLiveSlots.ClearAll();

    UINT32 basePos = 0;
    for (UINT32 clauseIndex = 0; clauseIndex < numEHClauses; clauseIndex++)
    {
        EE_ILEXCEPTION_CLAUSE * pClause;
        pClause = pEHInfo->EHClause(clauseIndex);

        _ASSERTE(pClause->TryEndPC <= m_CodeLength);
        if(!IsEssential(pClause))
            continue;
        
        liveState.ClearAll();
        
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
        {
            if(pCurrent->CodeOffset > pClause->TryStartPC)
                break;
            
            UINT32 slotIndex = pCurrent->SlotId;
            BYTE becomesLive = pCurrent->BecomesLive;
            _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                    || !liveState.ReadBit(slotIndex) && becomesLive);
            liveState.WriteBit(slotIndex, becomesLive);
        }

        for( ; pCurrent < pEndTransitions; pCurrent++)
        {
            if(pCurrent->CodeOffset >= pClause->TryEndPC)
                break;
            
            UINT32 slotIndex = pCurrent->SlotId;
            liveState.ClearBit(slotIndex);
        }

        // Copy to the EH live state table
        for(UINT32 i = 0; i < m_NumSlots; i++)
        {
            if(liveState.ReadBit(i))
                ehLiveSlots.SetBit(basePos + i);
        }
        basePos += m_NumSlots;

        // Keep track of which slots are used
        couldBeLive |= liveState;
    }
#endif  // DISABLE_EH_VECTORS
#endif  // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    
#if CODE_OFFSETS_NEED_NORMALIZATION
    // Do a pass to normalize transition offsets
    for(pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
    {
        _ASSERTE(pCurrent->CodeOffset <= m_CodeLength);
        pCurrent->CodeOffset = NORMALIZE_CODE_OFFSET(pCurrent->CodeOffset);
    }
#endif

    ///////////////////////////////////////////////////////////////////
    // Find out which slots are really used
    ///////////////////////////////////////////////////////////////////

    
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    if(m_NumCallSites)
    {
        _ASSERTE(m_pCallSites != NULL);
        liveState.ClearAll();
        
        UINT32 callSiteIndex = 0;
        UINT32 callSite = m_pCallSites[0];

        for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
        {
            if(pCurrent->CodeOffset > callSite)
            {
                couldBeLive |= liveState;

                if(++callSiteIndex == m_NumCallSites)
                    break;
                
                callSite = m_pCallSites[callSiteIndex];
            }
            else
            {
                UINT32 slotIndex = pCurrent->SlotId;
                if(!IsAlwaysScratch(m_SlotTable[slotIndex]))
                {
                    BYTE becomesLive = pCurrent->BecomesLive;
                    _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                            || !liveState.ReadBit(slotIndex) && becomesLive);
                      
                    liveState.WriteBit(slotIndex, becomesLive);
                }
                pCurrent++;
            }
        }
        // There could be call sites after the last transition
        if(callSiteIndex < m_NumCallSites)
        {
            couldBeLive |= liveState;
        }
    }

    if(numInterruptibleRanges)
    {
        liveState.ClearAll();

        InterruptibleRange *pCurrentRange = pRanges;
        InterruptibleRange *pEndRanges = pRanges + numInterruptibleRanges;        
       
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
        {
            // Find the first transition at offset > of the start of the current range
            LifetimeTransition *pFirstAfterStart = pCurrent;
            while(pFirstAfterStart->CodeOffset <= pCurrentRange->NormStartOffset)
            {
                UINT32 slotIndex = (UINT32) (pFirstAfterStart->SlotId);
                BYTE becomesLive = pFirstAfterStart->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);

                if(++pFirstAfterStart == pEndTransitions)
                    break;
            }

            couldBeLive |= liveState;
            
            // Now iterate through all the remaining transitions in the range, 
            //   making the offset range-relative, and tracking live state
            UINT32 rangeStop = pCurrentRange->NormStopOffset;
            
            for(pCurrent = pFirstAfterStart; pCurrent < pEndTransitions && pCurrent->CodeOffset < rangeStop; pCurrent++)
            {
                UINT32 slotIndex = (UINT32) (pCurrent->SlotId);
                BYTE becomesLive = pCurrent->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);
                couldBeLive.SetBit(slotIndex);
            }

            // Move to the next range
            if(pCurrentRange < pEndRanges - 1)
            {
                pCurrentRange++;
            }
            else
            {
                break;
            }
        }
    }

    //-----------------------------------------------------------------
    // Mark unneeded slots as deleted
    //-----------------------------------------------------------------

    UINT32 numUsedSlots = 0;    
    for(UINT32 i = 0; i < m_NumSlots; i++)
    {
        if(!(m_SlotTable[i].IsUntracked()) && (couldBeLive.ReadBit(i) == 0))
        {
            m_SlotTable[i].MarkDeleted();
        }
        else
            numUsedSlots++;
    }

    if(numUsedSlots < m_NumSlots)
    {
        // Delete transitions on unused slots
        LifetimeTransition *pNextFree = pTransitions;
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
        {
            UINT32 slotId = pCurrent->SlotId;
            if(!m_SlotTable[slotId].IsDeleted())
            {
                if(pCurrent > pNextFree)
                {
                    *pNextFree = *pCurrent;
                }
                pNextFree++;
            }
        }
        pEndTransitions = pNextFree;
    }

#else  // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    UINT32 numUsedSlots = m_NumSlots;

#endif  // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED


    ///////////////////////////////////////////////////////////////////////
    // Encode slot table
    ///////////////////////////////////////////////////////////////////////

    //------------------------------------------------------------------
    // Count registers and stack slots
    //------------------------------------------------------------------

    UINT32 numRegisters;
    UINT32 numUntrackedSlots;
    UINT32 numStackSlots;

    {
        UINT32 numDeleted = 0;
        UINT32 i;
        for(i = 0; i < m_NumSlots && m_SlotTable[i].IsRegister(); i++)
        {
            if(m_SlotTable[i].IsDeleted())
            {
                numDeleted++;
            }
        }
        numRegisters = i - numDeleted;

        for(; i < m_NumSlots && !m_SlotTable[i].IsUntracked(); i++)
        {
            if(m_SlotTable[i].IsDeleted())
            {
                numDeleted++;
            }
        }
        numStackSlots = i - (numRegisters + numDeleted);
    }
    numUntrackedSlots = numUsedSlots - (numRegisters + numStackSlots);

    // Common case: nothing, or a few registers
    if (numRegisters)
    {
        GCINFO_WRITE(m_Info1, 1, 1, FlagsSize);
        GCINFO_WRITE_VARL_U(m_Info1, numRegisters, NUM_REGISTERS_ENCBASE, NumRegsSize);
    }
    else
    {
        GCINFO_WRITE(m_Info1, 0, 1, FlagsSize);
    }
    if (numStackSlots || numUntrackedSlots)
    {
        GCINFO_WRITE(m_Info1, 1, 1, FlagsSize);
        GCINFO_WRITE_VARL_U(m_Info1, numStackSlots, NUM_STACK_SLOTS_ENCBASE, NumStackSize);
        GCINFO_WRITE_VARL_U(m_Info1, numUntrackedSlots, NUM_UNTRACKED_SLOTS_ENCBASE, NumUntrackedSize);
    }
    else
    {
        GCINFO_WRITE(m_Info1, 0, 1, FlagsSize);
    }

    UINT32 currentSlot = 0;
    
    if(numUsedSlots == 0)
        goto lExitSuccess;

    if(numRegisters > 0)
    {
        GcSlotDesc *pSlotDesc;
        do
        {
            _ASSERTE(currentSlot < m_NumSlots);
            pSlotDesc = &m_SlotTable[currentSlot++];
        }
        while(pSlotDesc->IsDeleted());
        _ASSERTE(pSlotDesc->IsRegister());

        // Encode slot identification
        UINT32 currentNormRegNum = NORMALIZE_REGISTER(pSlotDesc->Slot.RegisterNumber);
        GCINFO_WRITE_VARL_U(m_Info1, currentNormRegNum, REGISTER_ENCBASE, RegSlotSize);
        GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, RegSlotSize);
        
        for(UINT32 j = 1; j < numRegisters; j++)
        {
            UINT32 lastNormRegNum = currentNormRegNum;
            GcSlotFlags lastFlags = pSlotDesc->Flags;

            do
            {
                _ASSERTE(currentSlot < m_NumSlots);
                pSlotDesc = &m_SlotTable[currentSlot++];
            }
            while(pSlotDesc->IsDeleted());
            _ASSERTE(pSlotDesc->IsRegister());

            currentNormRegNum = NORMALIZE_REGISTER(pSlotDesc->Slot.RegisterNumber);

            if(lastFlags != GC_SLOT_IS_REGISTER)
            {
                GCINFO_WRITE_VARL_U(m_Info1, currentNormRegNum, REGISTER_ENCBASE, RegSlotSize);
                GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, RegSlotSize);
            }
            else
            {
                _ASSERTE(pSlotDesc->Flags == GC_SLOT_IS_REGISTER);
                GCINFO_WRITE_VARL_U(m_Info1, currentNormRegNum - lastNormRegNum - 1, REGISTER_DELTA_ENCBASE, RegSlotSize);
            }
        }
    }
    
    if(numStackSlots > 0)
    {
        GcSlotDesc *pSlotDesc;
        do
        {
            _ASSERTE(currentSlot < m_NumSlots);
            pSlotDesc = &m_SlotTable[currentSlot++];
        }
        while(pSlotDesc->IsDeleted());
        _ASSERTE(!pSlotDesc->IsRegister());
        _ASSERTE(!pSlotDesc->IsUntracked());

        // Encode slot identification
        _ASSERTE((pSlotDesc->Slot.Stack.Base & ~3) == 0);
        GCINFO_WRITE(m_Info1, pSlotDesc->Slot.Stack.Base, 2, StackSlotSize);
        INT32 currentNormStackSlot = NORMALIZE_STACK_SLOT(pSlotDesc->Slot.Stack.SpOffset);
        GCINFO_WRITE_VARL_S(m_Info1, currentNormStackSlot, STACK_SLOT_ENCBASE, StackSlotSize);

        GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, StackSlotSize);

        for(UINT32 j = 1; j < numStackSlots; j++)
        {
            INT32 lastNormStackSlot = currentNormStackSlot;
            GcSlotFlags lastFlags = pSlotDesc->Flags;

            do
            {
                _ASSERTE(currentSlot < m_NumSlots);
                pSlotDesc = &m_SlotTable[currentSlot++];
            }
            while(pSlotDesc->IsDeleted());
            _ASSERTE(!pSlotDesc->IsRegister());
            _ASSERTE(!pSlotDesc->IsUntracked());

            currentNormStackSlot = NORMALIZE_STACK_SLOT(pSlotDesc->Slot.Stack.SpOffset);

            _ASSERTE((pSlotDesc->Slot.Stack.Base & ~3) == 0);
            GCINFO_WRITE(m_Info1, pSlotDesc->Slot.Stack.Base, 2, StackSlotSize);
            
            if(lastFlags != GC_SLOT_BASE)
            {
                GCINFO_WRITE_VARL_S(m_Info1, currentNormStackSlot, STACK_SLOT_ENCBASE, StackSlotSize);
                GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, StackSlotSize);
            }
            else
            {
                _ASSERTE(pSlotDesc->Flags == GC_SLOT_BASE);
                GCINFO_WRITE_VARL_U(m_Info1, currentNormStackSlot - lastNormStackSlot, STACK_SLOT_DELTA_ENCBASE, StackSlotSize);
            }
        }
    }
    
    if(numUntrackedSlots > 0)
    {
        GcSlotDesc *pSlotDesc;
        do
        {
            _ASSERTE(currentSlot < m_NumSlots);
            pSlotDesc = &m_SlotTable[currentSlot++];
        }
        while(pSlotDesc->IsDeleted());
        _ASSERTE(!pSlotDesc->IsRegister());
        _ASSERTE(pSlotDesc->IsUntracked());

        // Encode slot identification
        _ASSERTE((pSlotDesc->Slot.Stack.Base & ~3) == 0);
        GCINFO_WRITE(m_Info1, pSlotDesc->Slot.Stack.Base, 2, UntrackedSlotSize);
        INT32 currentNormStackSlot = NORMALIZE_STACK_SLOT(pSlotDesc->Slot.Stack.SpOffset);
        GCINFO_WRITE_VARL_S(m_Info1, currentNormStackSlot, STACK_SLOT_ENCBASE, UntrackedSlotSize);

        GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, UntrackedSlotSize);

        for(UINT32 j = 1; j < numUntrackedSlots; j++)
        {
            INT32 lastNormStackSlot = currentNormStackSlot;
            GcSlotFlags lastFlags = pSlotDesc->Flags;

            do
            {
                _ASSERTE(currentSlot < m_NumSlots);
                pSlotDesc = &m_SlotTable[currentSlot++];
            }
            while(pSlotDesc->IsDeleted());
            _ASSERTE(!pSlotDesc->IsRegister());
            _ASSERTE(pSlotDesc->IsUntracked());

            currentNormStackSlot = NORMALIZE_STACK_SLOT(pSlotDesc->Slot.Stack.SpOffset);

            _ASSERTE((pSlotDesc->Slot.Stack.Base & ~3) == 0);
            GCINFO_WRITE(m_Info1, pSlotDesc->Slot.Stack.Base, 2, UntrackedSlotSize);
            
            if(lastFlags != GC_SLOT_UNTRACKED)
            {
                GCINFO_WRITE_VARL_S(m_Info1, currentNormStackSlot, STACK_SLOT_ENCBASE, UntrackedSlotSize);
                GCINFO_WRITE(m_Info1, pSlotDesc->Flags, 2, UntrackedSlotSize);
            }
            else
            {
                _ASSERTE(pSlotDesc->Flags == GC_SLOT_UNTRACKED);
                GCINFO_WRITE_VARL_U(m_Info1, currentNormStackSlot - lastNormStackSlot, STACK_SLOT_DELTA_ENCBASE, UntrackedSlotSize);
            }
        }
    }


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    //-----------------------------------------------------------------
    // Encode GC info at call sites
    //-----------------------------------------------------------------

    if(m_NumCallSites)
    {

        _ASSERTE(m_pCallSites != NULL);

        liveState.ClearAll();
        
        UINT32 callSiteIndex = 0;
        UINT32 callSite = m_pCallSites[0];

        // Create a hash table for storing the locations of the live sets
        LiveStateHashTable hashMap(m_pAllocator);

        for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
        {
            if(pCurrent->CodeOffset > callSite)
            {
                // Time to record the call site

                // Add it to the table if it doesn't exist
                UINT32 liveStateOffset = 0;
                if (!hashMap.Lookup(&liveState, &liveStateOffset))
                {
                    BitArray * newLiveState = new (m_pAllocator->Alloc(sizeof(BitArray))) BitArray(m_pAllocator, size_tCount);
                    *newLiveState = liveState;
                    hashMap.Set(newLiveState, (UINT32)(-1));
                }


                if(++callSiteIndex == m_NumCallSites)
                    break;
            
                callSite = m_pCallSites[callSiteIndex];
            }
            else
            {
                UINT32 slotIndex = pCurrent->SlotId;
                BYTE becomesLive = pCurrent->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);
                pCurrent++;
            }
        }

        // Check for call sites at offsets past the last transition
        if (callSiteIndex < m_NumCallSites)
        {
            UINT32 liveStateOffset = 0;
            if (!hashMap.Lookup(&liveState, &liveStateOffset))
            {
                BitArray * newLiveState = new (m_pAllocator->Alloc(sizeof(BitArray))) BitArray(m_pAllocator, size_tCount);
                *newLiveState = liveState;
                hashMap.Set(newLiveState, (UINT32)(-1));
            }
        }

        // Figure out the largest offset, and total size of the sets
        // Be sure to figure out the largest offset in the order that we will be emitting
        // them in and not the order of their appearances in the safe point array.
        // TODO: we should sort this to improve locality (the more frequent ones at the beginning)
        // and to improve the indirection size (if the largest one is last, we *might* be able
        // so use 1 less bit for each indirection for the offset encoding).
        UINT32 largestSetOffset = 0;
        UINT32 sizeofSets = 0;
        for (LiveStateHashTable::KeyIterator iter = hashMap.Begin(), end = hashMap.End(); !iter.Equal(end); iter.Next())
        {
            largestSetOffset = sizeofSets;
            sizeofSets += SizeofSlotStateVarLengthVector(*iter.Get(), LIVESTATE_RLE_SKIP_ENCBASE, LIVESTATE_RLE_RUN_ENCBASE);
        }

        // Now that we know the largest offset, we can figure out how much the indirection
        // will cost us and commit
        UINT32 numBitsPerPointer = ((largestSetOffset < 2) ? 1 : CeilOfLog2(largestSetOffset + 1));
        const size_t sizeofEncodedNumBitsPerPointer = BitStreamWriter::SizeofVarLengthUnsigned(numBitsPerPointer, POINTER_SIZE_ENCBASE);
        const size_t sizeofNoIndirection = m_NumCallSites * (numRegisters + numStackSlots);
        const size_t sizeofIndirection = sizeofEncodedNumBitsPerPointer  // Encode the pointer sizes
                                         + (m_NumCallSites * numBitsPerPointer) // Encoe the pointers
                                         + 7 // Up to 7 bits of alignment padding
                                         + sizeofSets; // Encode the actual live sets

        liveState.ClearAll();
        
        callSiteIndex = 0;
        callSite = m_pCallSites[0];

        if (sizeofIndirection < sizeofNoIndirection)
        {
            // we are using an indirection
            GCINFO_WRITE(m_Info1, 1, 1, FlagsSize);
            GCINFO_WRITE_VARL_U(m_Info1, numBitsPerPointer - 1, POINTER_SIZE_ENCBASE, CallSiteStateSize);

            // Now encode the live sets and record the real offset
            for (LiveStateHashTable::KeyIterator iter = hashMap.Begin(), end = hashMap.End(); !iter.Equal(end); iter.Next())
            {
                _ASSERTE(FitsIn<UINT32>(m_Info2.GetBitCount()));
                iter.SetValue((UINT32)m_Info2.GetBitCount());
                GCINFO_WRITE_VAR_VECTOR(m_Info2, *iter.Get(), LIVESTATE_RLE_SKIP_ENCBASE, LIVESTATE_RLE_RUN_ENCBASE, CallSiteStateSize);
            }

            _ASSERTE(sizeofSets == m_Info2.GetBitCount());

            for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
            {
                if(pCurrent->CodeOffset > callSite)
                {
                    // Time to encode the call site

                    // Find the match and emit it
                    UINT32 liveStateOffset;
                    bool found = hashMap.Lookup(&liveState, &liveStateOffset);
                    _ASSERTE(found);
                    (void)found;
                    GCINFO_WRITE(m_Info1, liveStateOffset, numBitsPerPointer, CallSiteStateSize);


                    if(++callSiteIndex == m_NumCallSites)
                        break;
                
                    callSite = m_pCallSites[callSiteIndex];
                }
                else
                {
                    UINT32 slotIndex = pCurrent->SlotId;
                    BYTE becomesLive = pCurrent->BecomesLive;
                    _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                            || !liveState.ReadBit(slotIndex) && becomesLive);
                    liveState.WriteBit(slotIndex, becomesLive);
                    pCurrent++;
                }
            }

            // Encode call sites at offsets past the last transition
            {
                UINT32 liveStateOffset;
                bool found = hashMap.Lookup(&liveState, &liveStateOffset);
                _ASSERTE(found);
                (void)found;
                for( ; callSiteIndex < m_NumCallSites; callSiteIndex++)
                {
                    GCINFO_WRITE(m_Info1, liveStateOffset, numBitsPerPointer, CallSiteStateSize);
                }
            }
        }
        else
        {
            // we are not using an indirection
            GCINFO_WRITE(m_Info1, 0, 1, FlagsSize);

            for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
            {
                if(pCurrent->CodeOffset > callSite)
                {
                    // Time to encode the call site
                    GCINFO_WRITE_VECTOR(m_Info1, liveState, CallSiteStateSize);

                    if(++callSiteIndex == m_NumCallSites)
                        break;
                
                    callSite = m_pCallSites[callSiteIndex];
                }
                else
                {
                    UINT32 slotIndex = pCurrent->SlotId;
                    BYTE becomesLive = pCurrent->BecomesLive;
                    _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                            || !liveState.ReadBit(slotIndex) && becomesLive);
                    liveState.WriteBit(slotIndex, becomesLive);
                    pCurrent++;
                }
            }

            // Encode call sites at offsets past the last transition
            for( ; callSiteIndex < m_NumCallSites; callSiteIndex++)
            {
                GCINFO_WRITE_VECTOR(m_Info1, liveState, CallSiteStateSize);
            }
        }

#ifdef MUST_CALL_JITALLOCATOR_FREE
        // Cleanup
        for (LiveStateHashTable::KeyIterator iter = hashMap.Begin(), end = hashMap.End(); !iter.Equal(end); iter.Next())
        {
            m_pAllocator->Free((LPVOID)iter.Get());
        }
#endif // MUST_CALL_JITALLOCATOR_FREE

    }

    //-----------------------------------------------------------------
    // Encode EH clauses and bit vectors
    //-----------------------------------------------------------------

#ifndef DISABLE_EH_VECTORS
    GCINFO_WRITE_VARL_U(m_Info1, numUsedEHClauses, NUM_EH_CLAUSES_ENCBASE, NumEhSize);

    basePos = 0;
    for(UINT32 clauseIndex = 0; clauseIndex < numEHClauses; clauseIndex++)
    {
        EE_ILEXCEPTION_CLAUSE * pClause;
        pClause = pEHInfo->EHClause(clauseIndex);

        if(!IsEssential(pClause))
            continue;
        
        UINT32 normStartOffset = NORMALIZE_CODE_OFFSET(pClause->TryStartPC);
        UINT32 normStopOffset = NORMALIZE_CODE_OFFSET(pClause->TryEndPC);
        _ASSERTE(normStopOffset > normStartOffset);        

        GCINFO_WRITE(m_Info1, normStartOffset, numBitsPerOffset, EhPosSize);
        GCINFO_WRITE(m_Info1, normStopOffset - 1, numBitsPerOffset, EhPosSize);
                
        for(UINT slotIndex = 0; slotIndex < m_NumSlots; slotIndex++)
        {
            if(!m_SlotTable[slotIndex].IsDeleted())
            {
                GCINFO_WRITE(m_Info1, ehLiveSlots.ReadBit(basePos + slotIndex) ? 1 : 0, 1, EhStateSize);
            }
        }
        basePos += m_NumSlots;
    }
#endif  // DISABLE_EH_VECTORS    
#endif  // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    
    ///////////////////////////////////////////////////////////////////////
    // Fully-interruptible: Encode lifetime transitions
    ///////////////////////////////////////////////////////////////////////

    if(numInterruptibleRanges)
    {
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED 
        //-----------------------------------------------------
        // Under partially-interruptible, make the transition
        //  offsets relative to the interruptible regions
        //-----------------------------------------------------
        
        // Compute total length of interruptible ranges
        UINT32 totalInterruptibleLength = 0;
        for(UINT32 i = 0; i < numInterruptibleRanges; i++)
        {
            InterruptibleRange *pRange = &pRanges[i];
            totalInterruptibleLength += pRange->NormStopOffset - pRange->NormStartOffset;
        }
        _ASSERTE(totalInterruptibleLength <= NORMALIZE_CODE_OFFSET(m_CodeLength));

        liveState.ClearAll();
        // Re-use couldBeLive
        BitArray& liveStateAtPrevRange = couldBeLive;
        liveStateAtPrevRange.ClearAll();

        InterruptibleRange *pCurrentRange = pRanges;
        InterruptibleRange *pEndRanges = pRanges + numInterruptibleRanges;        
        UINT32 cumInterruptibleLength = 0;
       
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
        {
            _ASSERTE(!m_SlotTable[pCurrent->SlotId].IsDeleted());
            
            // Find the first transition at offset > of the start of the current range
            LifetimeTransition *pFirstAfterStart = pCurrent;
            while(pFirstAfterStart->CodeOffset <= pCurrentRange->NormStartOffset)
            {
                UINT32 slotIndex = (UINT32) (pFirstAfterStart->SlotId);
                BYTE becomesLive = pFirstAfterStart->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);

                if(++pFirstAfterStart == pEndTransitions)
                    break;
            }

            // Now compare the liveState with liveStateAtPrevRange
            LifetimeTransition *pFirstPreserved = pFirstAfterStart;
            for(UINT32 slotIndex = 0; slotIndex < m_NumSlots; slotIndex++)
            {
                size_t isLive = liveState.ReadBit(slotIndex);
                if(isLive != liveStateAtPrevRange.ReadBit(slotIndex))
                {
                    pFirstPreserved--;
                    _ASSERTE(pFirstPreserved >= pCurrent);
                    pFirstPreserved->CodeOffset = cumInterruptibleLength;
                    pFirstPreserved->SlotId = slotIndex;
                    pFirstPreserved->BecomesLive = (isLive) ? 1 : 0;
                    _ASSERTE(!pFirstPreserved->IsDeleted);
                }
            }

            // Mark all the other transitions since last range as deleted
            _ASSERTE(pCurrent <= pFirstPreserved);
            while(pCurrent < pFirstPreserved)
            {
                (pCurrent++)->IsDeleted = TRUE;
            }
            
            // Now iterate through all the remaining transitions in the range, 
            //   making the offset range-relative, and tracking live state
            UINT32 rangeStop = pCurrentRange->NormStopOffset;
            
            for(pCurrent = pFirstAfterStart; pCurrent < pEndTransitions && pCurrent->CodeOffset < rangeStop; pCurrent++)
            {
                pCurrent->CodeOffset = 
                                pCurrent->CodeOffset - 
                                pCurrentRange->NormStartOffset + 
                                cumInterruptibleLength;
                
                UINT32 slotIndex = (UINT32) (pCurrent->SlotId);
                BYTE becomesLive = pCurrent->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);
            }

            // Move to the next range
            if(pCurrentRange < pEndRanges - 1)
            {
                cumInterruptibleLength += pCurrentRange->NormStopOffset - pCurrentRange->NormStartOffset;
                pCurrentRange++;

                liveStateAtPrevRange = liveState;
            }
            else
            {
                pEndTransitions = pCurrent;
                break;
            }
        }

        // Make another pass, deleting everything that's marked as deleted
        LifetimeTransition *pNextFree = pTransitions;
        for(pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
        {
            if(!pCurrent->IsDeleted)
            {
                if(pCurrent > pNextFree)
                {
                    *pNextFree = *pCurrent;
                }
                pNextFree++;
            }
        }
        pEndTransitions = pNextFree;

#else
        UINT32 totalInterruptibleLength = NORMALIZE_CODE_OFFSET(m_CodeLength);

#endif //PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

        //
        // Initialize chunk pointers
        //
        UINT32 numChunks = (totalInterruptibleLength + NUM_NORM_CODE_OFFSETS_PER_CHUNK - 1) / NUM_NORM_CODE_OFFSETS_PER_CHUNK;
        _ASSERTE(numChunks > 0);
        
        size_t* pChunkPointers = (size_t*) m_pAllocator->Alloc(numChunks*sizeof(size_t));
        ZeroMemory(pChunkPointers, numChunks*sizeof(size_t));

        //------------------------------------------------------------------
        // Encode transitions
        //------------------------------------------------------------------

        LOG((LF_GCINFO, LL_INFO1000, "Encoding %i lifetime transitions.\n", pEndTransitions - pTransitions));


        liveState.ClearAll();
        couldBeLive.ClearAll();

        for(pCurrent = pTransitions; pCurrent < pEndTransitions; )
        {
            _ASSERTE(pCurrent->CodeOffset < m_CodeLength);
        
            UINT32 currentChunk = GetNormCodeOffsetChunk(pCurrent->CodeOffset);
            _ASSERTE(currentChunk < numChunks);
            UINT32 numTransitionsInCurrentChunk = 1;

            for(;;)
            {
                UINT32 slotIndex = (UINT32) (pCurrent->SlotId);
                BYTE becomesLive = pCurrent->BecomesLive;
                _ASSERTE(liveState.ReadBit(slotIndex) && !becomesLive
                        || !liveState.ReadBit(slotIndex) && becomesLive);
                liveState.WriteBit(slotIndex, becomesLive);
                couldBeLive.SetBit(slotIndex);

                pCurrent++;
                if(pCurrent == pEndTransitions || GetNormCodeOffsetChunk(pCurrent->CodeOffset) != currentChunk)
                    break;

                numTransitionsInCurrentChunk++;
            }

            //-----------------------------------------------------
            // Time to encode the chunk
            //-----------------------------------------------------

            _ASSERTE(numTransitionsInCurrentChunk > 0);

            // Sort the transitions in this chunk by slot
            qsort(
                pCurrent - numTransitionsInCurrentChunk,
                numTransitionsInCurrentChunk,
                sizeof(LifetimeTransition),
                CompareLifetimeTransitionsBySlot
                );

            // Save chunk pointer
            pChunkPointers[currentChunk] = m_Info2.GetBitCount() + 1;

            // Write couldBeLive slot map
            GCINFO_WRITE_VAR_VECTOR(m_Info2, couldBeLive, LIVESTATE_RLE_SKIP_ENCBASE, LIVESTATE_RLE_RUN_ENCBASE, ChunkMaskSize);

            LOG((LF_GCINFO, LL_INFO100000,
                         "Chunk %d couldBeLive (%04x-%04x):\n", currentChunk,
                         currentChunk * NUM_NORM_CODE_OFFSETS_PER_CHUNK,
                         ((currentChunk + 1) * NUM_NORM_CODE_OFFSETS_PER_CHUNK) - 1
                         ));

            // Write final state
            // For all the bits set in couldBeLive.
            UINT32 i;
            for (BitArrayIterator iter(&couldBeLive); !iter.end(); iter++)
            {
                i = *iter;
                {
                    _ASSERTE(!m_SlotTable[i].IsDeleted());
                    _ASSERTE(!m_SlotTable[i].IsUntracked());
                    GCINFO_WRITE(   m_Info2, 
                                    liveState.ReadBit(i) ? 1 : 0,
                                    1,
                                    ChunkFinalStateSize
                                    );

                    LOG((LF_GCINFO, LL_INFO100000,
                         "\t" LOG_GCSLOTDESC_FMT " %s at end of chunk.\n",
                         LOG_GCSLOTDESC_ARGS(&m_SlotTable[i]),
                         liveState.ReadBit(i) ? "live" : "dead"));
                }
            }

            // Write transitions offsets
            UINT32 normChunkBaseCodeOffset = currentChunk * NUM_NORM_CODE_OFFSETS_PER_CHUNK;

            LifetimeTransition* pT = pCurrent - numTransitionsInCurrentChunk;
            
            for (BitArrayIterator iter(&couldBeLive); !iter.end(); iter++)
            {
                i = *iter;

                while(pT < pCurrent)
                {
                    GcSlotId slotId = pT->SlotId;
                    if(slotId != i)
                        break;

                    _ASSERTE(couldBeLive.ReadBit(slotId));

                    LOG((LF_GCINFO, LL_INFO100000,
                         "\tTransition " LOG_GCSLOTDESC_FMT " going %s at offset %04x.\n",
                         LOG_GCSLOTDESC_ARGS(&m_SlotTable[pT->SlotId]),
                         pT->BecomesLive ? "live" : "dead",
                         (int) pT->CodeOffset ));

                    // Write code offset delta
                    UINT32 normCodeOffset = pT->CodeOffset;
                    UINT32 normCodeOffsetDelta = normCodeOffset - normChunkBaseCodeOffset;

                    // Don't encode transitions at offset 0 as they are useless
                    if(normCodeOffsetDelta)
                    {
                        _ASSERTE(normCodeOffsetDelta < NUM_NORM_CODE_OFFSETS_PER_CHUNK);

                        GCINFO_WRITE(m_Info2, 1, 1, ChunkTransitionSize);
                        GCINFO_WRITE(m_Info2, normCodeOffsetDelta, NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2, ChunkTransitionSize);

#ifdef MEASURE_GCINFO                    
                        m_CurrentMethodSize.NumTransitions++;
#endif
                    }
                    
                    pT++;
                }

                // Write terminator
                GCINFO_WRITE(m_Info2, 0, 1, ChunkTransitionSize);

            }
            _ASSERTE(pT == pCurrent);
            
            couldBeLive = liveState;
        }

        //---------------------------------------------------------------------
        // The size of chunk encodings is now known. Write the chunk pointers.
        //---------------------------------------------------------------------


        // Find the largest pointer
        size_t largestPointer = 0;
        for(int i = numChunks - 1; i >=0; i--)
        {
            largestPointer = pChunkPointers[i];
            if(largestPointer > 0)
                break;
        }

        UINT32 numBitsPerPointer = CeilOfLog2(largestPointer + 1);
        GCINFO_WRITE_VARL_U(m_Info1, numBitsPerPointer, POINTER_SIZE_ENCBASE, ChunkPtrSize);

        if(numBitsPerPointer)
        {
            for(UINT32 i = 0; i < numChunks; i++)
            {
                GCINFO_WRITE(m_Info1, pChunkPointers[i], numBitsPerPointer, ChunkPtrSize);
            }
        }

        //-------------------------------------------------------------------
        // Cleanup
        //-------------------------------------------------------------------
        
#ifdef MUST_CALL_JITALLOCATOR_FREE
        m_pAllocator->Free(pRanges);
        m_pAllocator->Free(pChunkPointers);        
#endif
    }


#ifdef MUST_CALL_JITALLOCATOR_FREE
    m_pAllocator->Free(pTransitions);
#endif


lExitSuccess:;
    
    //-------------------------------------------------------------------
    // Update global stats
    //-------------------------------------------------------------------

#ifdef MEASURE_GCINFO
    m_CurrentMethodSize.NumMethods = 1;
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    m_CurrentMethodSize.NumCallSites = m_NumCallSites;
#ifdef DISABLE_EH_VECTORS
    m_CurrentMethodSize.NumEh = 0;
#else
    m_CurrentMethodSize.NumEh = numUsedEHClauses;
#endif
#endif
    m_CurrentMethodSize.NumRanges = numInterruptibleRanges;
    m_CurrentMethodSize.NumRegs = numRegisters;
    m_CurrentMethodSize.NumStack = numStackSlots;
    m_CurrentMethodSize.NumUntracked = numUntrackedSlots;
    m_CurrentMethodSize.SizeOfCode = m_CodeLength;
    if(numInterruptibleRanges)
    {
        g_FiGcInfoSize += m_CurrentMethodSize;
        m_CurrentMethodSize.Log(LL_INFO100, "=== FullyInterruptible method breakdown ===\r\n");
        g_FiGcInfoSize.Log(LL_INFO10, "=== FullyInterruptible global breakdown ===\r\n");
    }
    else
    {
        g_PiGcInfoSize += m_CurrentMethodSize;
        m_CurrentMethodSize.Log(LL_INFO100, "=== PartiallyInterruptible method breakdown ===\r\n");
        g_PiGcInfoSize.Log(LL_INFO10, "=== PartiallyInterruptible global breakdown ===\r\n");
    }
#endif
}

void GcInfoEncoder::SizeofSlotStateVarLengthVector(const BitArray &vector,
                                                   UINT32          baseSkip,
                                                   UINT32          baseRun,
                                                   UINT32         *pSizeofSimple,
                                                   UINT32         *pSizeofRLE,
                                                   UINT32         *pSizeofRLENeg)
{
    // Try 3 different encodings
    UINT32 sizeofSimple = 1;
    UINT32 sizeofRLE;
    UINT32 sizeofRLENeg;
    for(UINT32 i = 0; i < m_NumSlots && !m_SlotTable[i].IsUntracked(); i++)
    {
        if(!m_SlotTable[i].IsDeleted())
            sizeofSimple++;
    }

    if (sizeofSimple <= 2 + baseSkip + 1 + baseRun + 1)
    {
        // simple encoding is smaller than the smallest of the others
        // without even trying
        sizeofRLE = sizeofSimple + 1;
        sizeofRLENeg = sizeofSimple + 1;
    }
    else
    {
        sizeofRLE = 2; // For the header
        sizeofRLENeg = 2;

        UINT32 rleStart = 0;
        bool fPrev = false;
        UINT32 i;
        for(i = 0; i < m_NumSlots && !m_SlotTable[i].IsUntracked(); i++)
        {
            if(!m_SlotTable[i].IsDeleted())
            {
                if (vector.ReadBit(i))
                {
                    if (!fPrev)
                    {
                        // Skipping is done
                        sizeofRLE += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, baseSkip);
                        sizeofRLENeg += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, baseRun);
                        rleStart = i + 1;
                        fPrev = true;
                    }
                }
                else
                {
                    if (fPrev)
                    {
                        // Run is done
                        sizeofRLE += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, baseRun);
                        sizeofRLENeg += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, baseSkip);
                        rleStart = i + 1;
                        fPrev = false;
                    }
                }
            }
            else
            {
               rleStart++;
            }
        }

        _ASSERTE(i >= rleStart);
        sizeofRLE += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, fPrev ? baseRun : baseSkip);
        sizeofRLENeg += BitStreamWriter::SizeofVarLengthUnsigned(i - rleStart, fPrev ? baseSkip : baseRun);
    }

    *pSizeofSimple = sizeofSimple;
    *pSizeofRLE = sizeofRLE;
    *pSizeofRLENeg = sizeofRLENeg;
}

UINT32 GcInfoEncoder::SizeofSlotStateVarLengthVector(const BitArray &vector,
                                                    UINT32          baseSkip,
                                                    UINT32          baseRun)
{
    // Try 3 different encodings
    UINT32 sizeofSimple;
    UINT32 sizeofRLE;
    UINT32 sizeofRLENeg;
    SizeofSlotStateVarLengthVector(vector, baseSkip, baseRun, &sizeofSimple, &sizeofRLE, &sizeofRLENeg);

    if (sizeofSimple <= sizeofRLE && sizeofSimple <= sizeofRLENeg)
        return sizeofSimple;
    if (sizeofRLE <= sizeofRLENeg)
        return sizeofRLE;
    return sizeofRLENeg;
}

UINT32 GcInfoEncoder::WriteSlotStateVarLengthVector(BitStreamWriter &writer,
                                                    const BitArray  &vector,
                                                    UINT32           baseSkip,
                                                    UINT32           baseRun)
{
    // Try 3 different encodings
    UINT32 sizeofSimple;
    UINT32 sizeofRLE;
    UINT32 sizeofRLENeg;
    SizeofSlotStateVarLengthVector(vector, baseSkip, baseRun, &sizeofSimple, &sizeofRLE, &sizeofRLENeg);
    UINT32 result;

#ifdef _DEBUG
    size_t initial = writer.GetBitCount();
#endif // _DEBUG

    if (sizeofSimple <= sizeofRLE && sizeofSimple <= sizeofRLENeg)
    {
        // Simple encoding is smallest
        writer.Write(0, 1);
        WriteSlotStateVector(writer, vector);
        result = sizeofSimple;
    }
    else
    {
        // One of the RLE encodings is the smallest
        writer.Write(1, 1);

        if (sizeofRLENeg < sizeofRLE)
        {
            writer.Write(1, 1);
            UINT32 swap = baseSkip;
            baseSkip = baseRun;
            baseRun = swap;
            result = sizeofRLENeg;
        }
        else
        {
            writer.Write(0, 1);
            result = sizeofRLE;
        }
        

        UINT32 rleStart = 0;
        UINT32 i;
        bool fPrev = false;
        for(i = 0; i < m_NumSlots && !m_SlotTable[i].IsUntracked(); i++)
        {
            if(!m_SlotTable[i].IsDeleted())
            {
                
                if (vector.ReadBit(i))
                {
                    if (!fPrev)
                    {
                        // Skipping is done
                        writer.EncodeVarLengthUnsigned(i - rleStart, baseSkip);
                        rleStart = i + 1;
                        fPrev = true;
                    }
                }
                else
                {
                    if (fPrev)
                    {
                        // Run is done
                        writer.EncodeVarLengthUnsigned(i - rleStart, baseRun);
                        rleStart = i + 1;
                        fPrev = false;
                    }
                }
            }
            else
            {
                rleStart++;
            }
        }

        _ASSERTE(i >= rleStart);
        writer.EncodeVarLengthUnsigned(i - rleStart, fPrev ? baseRun : baseSkip);
    }

#ifdef _DEBUG
    _ASSERTE(result + initial == writer.GetBitCount());
#endif // _DEBUG

    return result;
}


void GcInfoEncoder::EliminateRedundantLiveDeadPairs(LifetimeTransition** ppTransitions,
                                                    size_t* pNumTransitions, 
                                                    LifetimeTransition** ppEndTransitions)
{
    LifetimeTransition* pTransitions = *ppTransitions;
    LifetimeTransition* pEndTransitions = *ppEndTransitions;

    LifetimeTransition* pNewTransitions = NULL;
    LifetimeTransition* pNewTransitionsCopyPtr = NULL;
    for (LifetimeTransition* pCurrent = pTransitions; pCurrent < pEndTransitions; pCurrent++)
    {
        // Is pCurrent the first of a dead/live pair?
        LifetimeTransition* pNext = pCurrent + 1;
        if (pNext < pEndTransitions &&
            pCurrent->CodeOffset == pNext->CodeOffset &&
            pCurrent->SlotId == pNext->SlotId &&
            pCurrent->IsDeleted == pNext->IsDeleted &&
            pCurrent->BecomesLive != pNext->BecomesLive)
        {
            // They are a pair we want to delete.  If this is the first pair we've encountered, allocate
            // the new array:
            if (pNewTransitions == NULL)
            {
                pNewTransitions = (LifetimeTransition*)m_pAllocator->Alloc((*pNumTransitions) * sizeof(LifetimeTransition));
                pNewTransitionsCopyPtr = pNewTransitions;
                // Copy from the start up to (but not including) pCurrent...
                for (LifetimeTransition* pCopyPtr = pTransitions; pCopyPtr < pCurrent; pCopyPtr++, pNewTransitionsCopyPtr++)
                    *pNewTransitionsCopyPtr = *pCopyPtr;
            }
            pCurrent++;
        }
        else
        {
            // pCurrent is not part of a pair.  If we're copying, copy.
            if (pNewTransitionsCopyPtr != NULL)
            {
                *pNewTransitionsCopyPtr++ = *pCurrent;
            }
        }
    }
    // If we deleted any pairs, substitute the new array for the old.
    if (pNewTransitions != NULL)
    {
        m_pAllocator->Free(pTransitions);
        *ppTransitions = pNewTransitions;
        assert(pNewTransitionsCopyPtr != NULL);
        *ppEndTransitions = pNewTransitionsCopyPtr;
        *pNumTransitions = (*ppEndTransitions) - (*ppTransitions);
    }
}

//
// Write encoded information to its final destination and frees temporary buffers.
// The encoder shouldn't be used anymore after calling this method.
//
BYTE* GcInfoEncoder::Emit()
{
    size_t cbGcInfoSize = m_Info1.GetByteCount() +
                          m_Info2.GetByteCount();

#ifdef VERIFY_GCINFO
     cbGcInfoSize += (sizeof(size_t)) + m_DbgEncoder.GetByteCount();
#endif    

    LOG((LF_GCINFO, LL_INFO100, "GcInfoEncoder::Emit(): Size of GC info is %u bytes, code size %u bytes.\n", (unsigned)cbGcInfoSize, m_CodeLength ));

    BYTE* destBuffer = (BYTE *)eeAllocGCInfo(cbGcInfoSize);
    // Allocator will throw an exception on failure.
    // NOTE: the returned pointer may not be aligned during ngen.
    _ASSERTE( destBuffer );

    BYTE* ptr = destBuffer;

#ifdef VERIFY_GCINFO
    _ASSERTE(sizeof(size_t) >= sizeof(UINT32));
    size_t __displacement = cbGcInfoSize - m_DbgEncoder.GetByteCount();
    ptr[0] = (BYTE)__displacement;
    ptr[1] = (BYTE) (__displacement >> 8);
    ptr[2] = (BYTE) (__displacement >> 16);
    ptr[3] = (BYTE) (__displacement >> 24);
    ptr += sizeof(size_t);
#endif    

    m_Info1.CopyTo( ptr );
    ptr += m_Info1.GetByteCount();
    m_Info1.Dispose();

    m_Info2.CopyTo( ptr );
    ptr += m_Info2.GetByteCount();
    m_Info2.Dispose();

#ifdef MUST_CALL_JITALLOCATOR_FREE
    m_pAllocator->Free( m_SlotTable );
#endif

#ifdef VERIFY_GCINFO
    _ASSERTE(ptr - destBuffer == __displacement);
    m_DbgEncoder.Emit(ptr);
#endif    

    return destBuffer;
}

void * GcInfoEncoder::eeAllocGCInfo (size_t        blockSize)
{
    return m_pCorJitInfo->allocGCInfo(blockSize);
}


BitStreamWriter::BitStreamWriter( IAllocator* pAllocator )
{
    m_pAllocator = pAllocator;
    m_BitCount = 0;
#ifdef _DEBUG
    m_MemoryBlocksCount = 0;
#endif

    // Allocate memory blocks lazily
    m_OutOfBlockSlot = m_pCurrentSlot = (size_t*) NULL;
    m_FreeBitsInCurrentSlot = 0;
}

//
// bit 0 is the least significative bit
// The stream encodes the first come bit in the least significant bit of each byte
//
void BitStreamWriter::Write( size_t data, UINT32 count )
{
    _ASSERT(count <= BITS_PER_SIZE_T);

    if(count)
    {
        // Increment it now as we change count later on
        m_BitCount += count;

        if( count > m_FreeBitsInCurrentSlot )
        {
            if( m_FreeBitsInCurrentSlot > 0 )
            {
                _ASSERTE(m_FreeBitsInCurrentSlot < BITS_PER_SIZE_T);
                WriteInCurrentSlot( data, m_FreeBitsInCurrentSlot );
                count -= m_FreeBitsInCurrentSlot;
                data >>= m_FreeBitsInCurrentSlot;
            }

            _ASSERTE( count > 0 );

            // Initialize the next slot
            if( ++m_pCurrentSlot >= m_OutOfBlockSlot )
            {
                // Get a new memory block
                AllocMemoryBlock();
            }

            InitCurrentSlot();

            // Write the remainder
            WriteInCurrentSlot( data, count );
            m_FreeBitsInCurrentSlot -= count;
        }
        else
        {
            WriteInCurrentSlot( data, count );
            m_FreeBitsInCurrentSlot -= count;
            // if m_FreeBitsInCurrentSlot becomes 0 a nwe slot will initialized on the next request
        }
    }
}


void BitStreamWriter::CopyTo( BYTE* buffer )
{
    int i,c;
    BYTE* source = NULL;

    MemoryBlockDesc* pMemBlockDesc = m_MemoryBlocks.GetHead();
    if( pMemBlockDesc == NULL )
        return;
        
    while( m_MemoryBlocks.GetNext( pMemBlockDesc ) != NULL )
    {
        source = (BYTE*) pMemBlockDesc->StartAddress;
        // @TODO: use memcpy instead
        for( i = 0; i < m_MemoryBlockSize; i++ )
        {
            *( buffer++ ) = *( source++ );
        }

        pMemBlockDesc = m_MemoryBlocks.GetNext( pMemBlockDesc );
        _ASSERTE( pMemBlockDesc != NULL );
    }

    source = (BYTE*) pMemBlockDesc->StartAddress;
    // The number of bytes to copy in the last block
    c = (int) ((BYTE*) ( m_pCurrentSlot + 1 ) - source - m_FreeBitsInCurrentSlot/8);
    _ASSERTE( c >= 0 );
    // @TODO: use memcpy instead
    for( i = 0; i < c; i++ )
    {
        *( buffer++ ) = *( source++ );
    }

}

void BitStreamWriter::Dispose()
{
#ifdef MUST_CALL_JITALLOCATOR_FREE
    MemoryBlockDesc* pMemBlockDesc;
    while( NULL != ( pMemBlockDesc = m_MemoryBlocks.RemoveHead() ) )
    {
        m_pAllocator->Free( pMemBlockDesc->StartAddress );
        m_pAllocator->Free( pMemBlockDesc );
    }
#endif
}

