// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************************
 *
 * GC Information Encoding API
 *
 * This is an older well-tested implementation 
 *      now used to verify the real encoding
 * Define VERIFY_GCINFO to enable the verification
 *
 */

#ifdef VERIFY_GCINFO

#include "dbggcinfoencoder.h"
#include "gcinfoencoder.h"


namespace DbgGcInfo {


#ifdef _DEBUG
    #ifndef LOGGING
        #define LOGGING
    #endif
#endif
#include "log.h"


void *GcInfoEncoder::LifetimeTransitionAllocator::Alloc (void *context, SIZE_T cb)
{
    GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_LifetimeTransitions);
    return pGcInfoEncoder->m_pAllocator->Alloc(cb);
}

void GcInfoEncoder::LifetimeTransitionAllocator::Free (void *context, void *pv)
{
#ifdef MUST_CALL_JITALLOCATOR_FREE
    GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_LifetimeTransitions);
    pGcInfoEncoder->m_pAllocator->Free(pv);
#endif
}


void BitStreamWriter::AllocMemoryBlock()
{
    _ASSERTE( IS_ALIGNED( m_MemoryBlockSize, sizeof( size_t ) ) );
    m_pCurrentSlot = (size_t*) m_pAllocator->Alloc( m_MemoryBlockSize );
    m_OutOfBlockSlot = m_pCurrentSlot + m_MemoryBlockSize / sizeof( size_t );

    MemoryBlockDesc* pMemBlockDesc = (MemoryBlockDesc*) m_pAllocator->Alloc( sizeof( MemoryBlockDesc ) );
    _ASSERTE( IS_ALIGNED( pMemBlockDesc, sizeof( void* ) ) );

    pMemBlockDesc->Init();
    pMemBlockDesc->StartAddress = m_pCurrentSlot;
    m_MemoryBlocks.InsertTail( pMemBlockDesc );

#ifdef _DEBUG
       m_MemoryBlocksCount++;
#endif

}

GcInfoEncoder::GcInfoEncoder(
            ICorJitInfo*                pCorJitInfo,
            CORINFO_METHOD_INFO*        pMethodInfo,
            IJitAllocator*              pJitAllocator
            )
    :   m_HeaderInfoWriter( pJitAllocator ),
#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
        m_PartiallyInterruptibleInfoWriter( pJitAllocator ),
#endif
#endif
        m_FullyInterruptibleInfoWriter( pJitAllocator ),
        m_LifetimeTransitions()
{
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
    m_MethodName = (char *)
        pCorJitInfo->getMethodName(methodHandle, (const char **)&m_ModuleName);
#endif


    m_MappingTableSize = m_MappingTableInitialSize;
    m_SlotMappings = (GcSlotDesc*) m_pAllocator->Alloc( m_MappingTableSize*sizeof(GcSlotDesc) );
    m_NumSlotMappings = 0;
#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    m_NumSafePointsWithGcState = 0;
#endif
#endif

    m_SecurityObjectStackSlot = NO_SECURITY_OBJECT;
    m_PSPSymStackSlot = NO_PSP_SYM;
    m_GenericsInstContextStackSlot = NO_GENERICS_INST_CONTEXT;
    m_StackBaseRegister = NO_STACK_BASE_REGISTER;
    m_SizeOfEditAndContinuePreservedArea = NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA;
    m_IsVarArg = false;
    m_LastInterruptibleRangeStopOffset = 0;
    m_NumInterruptibleRanges = 0;
    
#ifdef _DEBUG
    m_IsMappingTableFrozen = FALSE;
    m_CodeLength = 0;
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    m_SizeOfStackOutgoingAndScratchArea = -1;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA
#endif //_DEBUG
}

GcSlotId GcInfoEncoder::GetRegisterSlotId( UINT32 regNum, GcSlotFlags flags )
{
    // We could lookup an existing identical slot in the mapping table (via some hashtable mechanism).
    // We just create duplicates for now.

#ifdef _DEBUG
    _ASSERTE( !m_IsMappingTableFrozen );
#endif

    if( m_NumSlotMappings == m_MappingTableSize )
    {
        GrowMappingTable();
    }
    _ASSERTE( m_NumSlotMappings < m_MappingTableSize );

    m_SlotMappings[ m_NumSlotMappings ].IsRegister = 1;
    m_SlotMappings[ m_NumSlotMappings ].Slot.RegisterNumber = regNum;
    m_SlotMappings[ m_NumSlotMappings ].IsInterior = ( flags & GC_SLOT_INTERIOR ) ? 1 : 0;
    m_SlotMappings[ m_NumSlotMappings ].IsPinned = ( flags & GC_SLOT_PINNED ) ? 1 : 0;

    GcSlotId newSlotId;
    newSlotId = m_NumSlotMappings++;
    return newSlotId;
}

GcSlotId GcInfoEncoder::GetStackSlotId( INT32 spOffset, GcSlotFlags flags, GcStackSlotBase spBase )
{
    // We could lookup an existing identical slot in the mapping table (via some hashtable mechanism).
    // We just create duplicates for now.

#ifdef _DEBUG
    _ASSERTE( !m_IsMappingTableFrozen );
#endif

    if( m_NumSlotMappings == m_MappingTableSize )
    {
        GrowMappingTable();
    }
    _ASSERTE( m_NumSlotMappings < m_MappingTableSize );

    // Not valid to reference anything below the current stack pointer
    _ASSERTE(GC_SP_REL != spBase || spOffset >= 0);

    m_SlotMappings[ m_NumSlotMappings ].IsRegister = 0;
    m_SlotMappings[ m_NumSlotMappings ].Slot.Stack.SpOffset = spOffset;
    m_SlotMappings[ m_NumSlotMappings ].Slot.Stack.Base = spBase;
    m_SlotMappings[ m_NumSlotMappings ].IsInterior = ( flags & GC_SLOT_INTERIOR ) ? 1 : 0;
    m_SlotMappings[ m_NumSlotMappings ].IsPinned = ( flags & GC_SLOT_PINNED ) ? 1 : 0;

    GcSlotId newSlotId;
    newSlotId = m_NumSlotMappings++;
    return newSlotId;
}

void GcInfoEncoder::GrowMappingTable()
{
    m_MappingTableSize *= 2;
    GcSlotDesc* newMappingTable = (GcSlotDesc*) m_pAllocator->Alloc( m_MappingTableSize * sizeof(GcSlotDesc) );
    memcpy( newMappingTable, m_SlotMappings, m_NumSlotMappings * sizeof(GcSlotDesc) );

#ifdef MUST_CALL_JITALLOCATOR_FREE
    m_pAllocator->Free( m_SlotMappings );
#endif

    m_SlotMappings = newMappingTable;
}

GcSlotSet::GcSlotSet( GcInfoEncoder* pEncoder )
{
#ifdef _DEBUG
    _ASSERTE( pEncoder->m_IsMappingTableFrozen );
#endif

    m_pEncoder = pEncoder;
    m_NumBytes = ( pEncoder->m_NumSlotMappings + 7 ) / 8;
    m_Data = (BYTE*) pEncoder->m_pAllocator->Alloc( m_NumBytes );
}

// Copy constructor
GcSlotSet::GcSlotSet( GcSlotSet & other )
{
    m_pEncoder = other.m_pEncoder;
    m_NumBytes = other.m_NumBytes;
    m_Data = (BYTE*) other.m_pEncoder->m_pAllocator->Alloc( m_NumBytes );
    memcpy( m_Data, other.m_Data, m_NumBytes);
}

void GcSlotSet::Add( GcSlotId slotId )
{
    _ASSERTE( slotId < m_pEncoder->m_NumSlotMappings );
    m_Data[ slotId / 8 ] |= 1 << ( slotId % 8 );
}

void GcSlotSet::Remove( GcSlotId slotId )
{
    _ASSERTE( slotId < m_pEncoder->m_NumSlotMappings );
    m_Data[ slotId / 8 ] &= ~( 1 << ( slotId % 8 ) );
}

// Not used
#if 0

void GcSlotSet::Add( GcSlotSet & other )
{
    _ASSERTE( m_pEncoder == other.m_pEncoder );

    for( int i=0; i<m_NumBytes; i++ )
    {
        m_Data[ i ] |= other.m_Data[ i ];
    }
}

void GcSlotSet::Subtract( GcSlotSet & other )
{
    _ASSERTE( m_pEncoder == other.m_pEncoder );

    for( int i=0; i<m_NumBytes; i++ )
    {
        m_Data[ i ] &= ~( other.m_Data[ i ] );
    }
}

void GcSlotSet::Intersect( GcSlotSet & other )
{
    _ASSERTE( m_pEncoder == other.m_pEncoder );

    for( int i=0; i<m_NumBytes; i++ )
    {
        m_Data[ i ] &= other.m_Data[ i ];
    }
}

#endif // unused


void GcInfoEncoder::FinalizeSlotIds()
{
#ifdef _DEBUG
    m_IsMappingTableFrozen = TRUE;
#endif
}


#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

void GcInfoEncoder::DefineGcStateAtSafePoint(
                UINT32          instructionOffset,
                GcSlotSet       &liveSlots
                )
{
#ifdef _DEBUG
    _ASSERTE( m_IsMappingTableFrozen );
#endif

#ifdef _DEBUG
    // Verify that any slot is not reported multiple times. This is O(n^2) but it executes only under _DEBUG
    for( INT32 i1=0; i1<((INT32)m_NumSlotMappings)-1; i1++ )
    {
        BYTE isLive1 = liveSlots.m_Data[ i1 / 8 ] & ( 1 << ( i1 % 8 ) );
        if( isLive1 )
            for( UINT32 i2=i1+1; i2<m_NumSlotMappings; i2++ )
        {
            BYTE isLive2 = liveSlots.m_Data[ i2 / 8 ] & ( 1 << ( i2 % 8 ) );
            if( isLive2 )
            {
                if( m_SlotMappings[ i1 ].IsRegister && m_SlotMappings[ i2 ].IsRegister )
                {
                    _ASSERTE( m_SlotMappings[ i1 ].Slot.RegisterNumber != m_SlotMappings[ i2 ].Slot.RegisterNumber );
                }
                else if( !m_SlotMappings[ i1 ].IsRegister && !m_SlotMappings[ i2 ].IsRegister )
                {
                    _ASSERTE( m_SlotMappings[ i1 ].Slot.SpOffset != m_SlotMappings[ i2 ].Slot.SpOffset );
                }
            }
        }
    }
#endif

    m_PartiallyInterruptibleInfoWriter.Write( instructionOffset, 32 );

    UINT32 i;
    for( i=0; i<m_NumSlotMappings/8; i++ )
        m_PartiallyInterruptibleInfoWriter.Write( liveSlots.m_Data[ i ], 8 );

    if( m_NumSlotMappings % 8 > 0 )
        m_PartiallyInterruptibleInfoWriter.Write( liveSlots.m_Data[ i ], m_NumSlotMappings % 8 );

    m_NumSafePointsWithGcState++;
}

#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif

void GcInfoEncoder::DefineInterruptibleRange( UINT32 startInstructionOffset, UINT32 length )
{
    UINT32 stopInstructionOffset = startInstructionOffset + length;

    size_t normStartDelta = NORMALIZE_CODE_OFFSET(startInstructionOffset) - NORMALIZE_CODE_OFFSET(m_LastInterruptibleRangeStopOffset);
    size_t normStopDelta = NORMALIZE_CODE_OFFSET(stopInstructionOffset) - NORMALIZE_CODE_OFFSET(startInstructionOffset);
    _ASSERTE(normStopDelta > 0);
    
    m_LastInterruptibleRangeStopOffset = startInstructionOffset + length;

    m_NumInterruptibleRanges++;
    
    m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normStartDelta, INTERRUPTIBLE_RANGE_DELTA_ENCBASE);

    m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normStopDelta-1, INTERRUPTIBLE_RANGE_DELTA_ENCBASE );
}


///////////////////////////////////////////////////////////////////////////
// Tracking information
///////////////////////////////////////////////////////////////////////////


//
// For inputs, pass zero as offset
//

void GcInfoEncoder::SetSlotState(
                            UINT32      instructionOffset,
                            GcSlotId    slotId,
                            GcSlotState slotState
                            )
{
    LifetimeTransition transition;

    transition.SlotDesc = m_SlotMappings[ slotId ];
    transition.CodeOffset = instructionOffset;
    transition.BecomesLive = ( slotState == GC_SLOT_LIVE );

    *( m_LifetimeTransitions.AppendThrowing() ) = transition;
}


void GcInfoEncoder::SetIsVarArg()
{
    m_IsVarArg = true;
}

void GcInfoEncoder::SetCodeLength( UINT32 length )
{
    _ASSERTE( length > 0 );
    _ASSERTE( m_CodeLength == 0 || m_CodeLength == length );
    m_CodeLength = length;
}


void GcInfoEncoder::SetSecurityObjectStackSlot( INT32 spOffset )
{
    _ASSERTE( spOffset != NO_SECURITY_OBJECT );
    _ASSERTE( m_SecurityObjectStackSlot == NO_SECURITY_OBJECT || m_SecurityObjectStackSlot == spOffset );
    m_SecurityObjectStackSlot = spOffset;
}

void GcInfoEncoder::SetPSPSymStackSlot( INT32 spOffsetPSPSym )
{
    _ASSERTE( spOffsetPSPSym != NO_PSP_SYM );
    _ASSERTE( m_PSPSymStackSlot == NO_PSP_SYM || m_PSPSymStackSlot == spOffsetPSPSym );

    m_PSPSymStackSlot              = spOffsetPSPSym;
}

void GcInfoEncoder::SetGenericsInstContextStackSlot( INT32 spOffsetGenericsContext )
{
    _ASSERTE( spOffsetGenericsContext != NO_GENERICS_INST_CONTEXT);
    _ASSERTE( m_GenericsInstContextStackSlot == NO_GENERICS_INST_CONTEXT || m_GenericsInstContextStackSlot == spOffsetGenericsContext );

    m_GenericsInstContextStackSlot = spOffsetGenericsContext;
}

void GcInfoEncoder::SetStackBaseRegister( UINT32 regNum )
{
    _ASSERTE( regNum != NO_STACK_BASE_REGISTER );
    _ASSERTE( m_StackBaseRegister == NO_STACK_BASE_REGISTER || m_StackBaseRegister == regNum );
    m_StackBaseRegister = regNum;
}

void GCInfoEncoder::SetSizeOfEditAndContinuePreservedArea( UINT32 slots )
{
    _ASSERTE( regNum != NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA );
    _ASSERTE( m_SizeOfEditAndContinuePreservedArea == NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA );
    m_SizeOfEditAndContinuePreservedArea = slots;
}



#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
void GcInfoEncoder::SetSizeOfStackOutgoingAndScratchArea( UINT32 size )
{
    _ASSERTE( size != -1 );
    _ASSERTE( m_SizeOfStackOutgoingAndScratchArea == -1 || m_SizeOfStackOutgoingAndScratchArea == size );
    m_SizeOfStackOutgoingAndScratchArea = size;
}
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


class LifetimeTransitionsQuickSort : public CQuickSort<GcInfoEncoder::LifetimeTransition>
{
public:
    LifetimeTransitionsQuickSort(
        GcInfoEncoder::LifetimeTransition*   pBase,
        size_t               count
        )
        : CQuickSort<GcInfoEncoder::LifetimeTransition>( pBase, count )
    {}

    int Compare( GcInfoEncoder::LifetimeTransition* pFirst, GcInfoEncoder::LifetimeTransition* pSecond )
    {
        // All registers come before all stack slots
        if( pFirst->SlotDesc.IsRegister && !pSecond->SlotDesc.IsRegister ) return -1;
        if( !pFirst->SlotDesc.IsRegister && pSecond->SlotDesc.IsRegister ) return 1;

        // Then sort them by slot
        if( pFirst->SlotDesc.IsRegister )
        {
            _ASSERTE( pSecond->SlotDesc.IsRegister );
            if( pFirst->SlotDesc.Slot.RegisterNumber < pSecond->SlotDesc.Slot.RegisterNumber ) return -1;
            if( pFirst->SlotDesc.Slot.RegisterNumber > pSecond->SlotDesc.Slot.RegisterNumber ) return 1;
        }
        else
        {
            _ASSERTE( !pSecond->SlotDesc.IsRegister );
            if( pFirst->SlotDesc.Slot.Stack.SpOffset < pSecond->SlotDesc.Slot.Stack.SpOffset ) return -1;
            if( pFirst->SlotDesc.Slot.Stack.SpOffset > pSecond->SlotDesc.Slot.Stack.SpOffset ) return 1;

            // This is arbitrary, but we want to make sure they are considered separate slots
            if( pFirst->SlotDesc.Slot.Stack.Base < pSecond->SlotDesc.Slot.Stack.Base ) return -1;
            if( pFirst->SlotDesc.Slot.Stack.Base > pSecond->SlotDesc.Slot.Stack.Base ) return 1;
        }

        // Then sort them by code offset
        size_t firstOffset  = pFirst->CodeOffset;
        size_t secondOffset = pSecond->CodeOffset;
        if( firstOffset < secondOffset ) return -1;
        if( firstOffset > secondOffset ) return 1;

        //
        // Same slot and offset. We put all the going-live transition first
        //  so that the encoder will skip the remaining transitions and 
        //  the going-live transitions take precedence
        //
        _ASSERTE( ( pFirst->BecomesLive == 0 ) || ( pFirst->BecomesLive == 1 ) );
        _ASSERTE( ( pSecond->BecomesLive == 0 ) || ( pSecond->BecomesLive == 1 ) );
        return ( pSecond->BecomesLive - pFirst->BecomesLive );
    }
};


void GcInfoEncoder::Build()
{
    SIZE_T i;

    ///////////////////////////////////////////////////////////////////////
    // Method header
    ///////////////////////////////////////////////////////////////////////

    m_HeaderInfoWriter.Write( ( m_IsVarArg ? 1 : 0 ), 1 );

    if(m_SecurityObjectStackSlot != NO_SECURITY_OBJECT)
    {
        m_HeaderInfoWriter.Write( 1, 1 );
        m_HeaderInfoWriter.EncodeVarLengthSigned(NORMALIZE_STACK_SLOT(m_SecurityObjectStackSlot), SECURITY_OBJECT_STACK_SLOT_ENCBASE);
    }
    else
    {
        m_HeaderInfoWriter.Write( 0, 1 );
    }
    
    if (m_PSPSymStackSlot != NO_PSP_SYM)
    {
        m_HeaderInfoWriter.Write( 1, 1 );
        m_HeaderInfoWriter.EncodeVarLengthSigned(NORMALIZE_STACK_SLOT(m_PSPSymStackSlot), PSP_SYM_STACK_SLOT_ENCBASE);
    }
    else
    {
        m_HeaderInfoWriter.Write( 0, 1 );
    }

    if (m_GenericsInstContextStackSlot != NO_GENERICS_INST_CONTEXT)
    {
        m_HeaderInfoWriter.Write( 1, 1 );
        m_HeaderInfoWriter.EncodeVarLengthSigned(NORMALIZE_STACK_SLOT(m_GenericsInstContextStackSlot), GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE);
    }
    else
    {
        m_HeaderInfoWriter.Write( 0, 1 );
    }

    _ASSERTE( m_CodeLength > 0 );
    m_HeaderInfoWriter.EncodeVarLengthUnsigned(NORMALIZE_CODE_LENGTH(m_CodeLength), CODE_LENGTH_ENCBASE);

    if(m_StackBaseRegister != NO_STACK_BASE_REGISTER)
    {
        m_HeaderInfoWriter.Write( 1, 1 );
        m_HeaderInfoWriter.EncodeVarLengthUnsigned(NORMALIZE_STACK_BASE_REGISTER(m_StackBaseRegister), STACK_BASE_REGISTER_ENCBASE);
    }
    else
    {
        m_HeaderInfoWriter.Write( 0, 1 );
    }

    if(m_SizeOfEditAndContinuePreservedArea != NO_SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA)
    {
        m_HeaderInfoWriter.Write( 1, 1 );
        m_HeaderInfoWriter.EncodeVarLengthUnsigned(m_SizeOfEditAndContinuePreservedArea, SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE);
    }
    else
    {
        m_HeaderInfoWriter.Write( 0, 1 );
    }

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    _ASSERTE( m_SizeOfStackOutgoingAndScratchArea != -1 );
    m_HeaderInfoWriter.EncodeVarLengthUnsigned(NORMALIZE_SIZE_OF_STACK_AREA(m_SizeOfStackOutgoingAndScratchArea), SIZE_OF_STACK_AREA_ENCBASE);
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


    ///////////////////////////////////////////////////////////////////////
    // Fully-interruptible: encode number of interruptible ranges
    ///////////////////////////////////////////////////////////////////////

    m_HeaderInfoWriter.EncodeVarLengthUnsigned(NORMALIZE_NUM_INTERRUPTIBLE_RANGES(m_NumInterruptibleRanges), NUM_INTERRUPTIBLE_RANGES_ENCBASE);

#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    ///////////////////////////////////////////////////////////////////////
    // Partially-interruptible: Encode call sites
    ///////////////////////////////////////////////////////////////////////

    m_HeaderInfoWriter.Write( m_NumSafePointsWithGcState, sizeof( m_NumSafePointsWithGcState ) * 8 );

    if( m_NumSafePointsWithGcState > 0 )
    {
        m_HeaderInfoWriter.Write( m_NumSlotMappings, sizeof( m_NumSlotMappings ) * 8 );

        ///////////////////////////////////////////////////////////////////////
        // Partially-interruptible: Encode slot mappings
        ///////////////////////////////////////////////////////////////////////

        // Assert that we can write a GcSlotDesc with a single call to BitStreamWriter.Write()
        _ASSERTE( sizeof( GcSlotDesc ) <= sizeof( size_t ) );
        for( UINT32 i=0; i<m_NumSlotMappings; i++ )
        {
            size_t data = 0;
            *( (GcSlotDesc*) &data ) = m_SlotMappings[ i ];
            m_PartiallyInterruptibleInfoWriter.Write( data, sizeof( GcSlotDesc ) * 8 );
        }
    }

#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif

    ///////////////////////////////////////////////////////////////////////
    // Fully-interruptible: Encode lifetime transitions
    ///////////////////////////////////////////////////////////////////////

    m_rgSortedTransitions = (LifetimeTransition*)m_pAllocator->Alloc(m_LifetimeTransitions.Count() * sizeof(LifetimeTransition));
    m_LifetimeTransitions.CopyTo(m_rgSortedTransitions);

    // Sort them first
    LifetimeTransitionsQuickSort lifetimeTransitionsQSort(
        m_rgSortedTransitions,
        m_LifetimeTransitions.Count()
        );
    lifetimeTransitionsQSort.Sort();

    size_t numTransitions = m_LifetimeTransitions.Count();

    //------------------------------------------------------------------
    // Count registers and stack slots
    //------------------------------------------------------------------

    int numRegisters = 0;
    int numStackSlots = 0;

    if(numTransitions > 0)
    {
        i = 1;
        if(m_rgSortedTransitions[ 0 ].SlotDesc.IsRegister)
        {
            numRegisters++;

            for( ; i < numTransitions; i++ )
            {
                if(!(m_rgSortedTransitions[ i ].SlotDesc.IsRegister))
                {
                    numStackSlots++;
                    i++;
                    break;
                }
                _ASSERTE(m_rgSortedTransitions[ i-1 ].SlotDesc.IsRegister);
                if((m_rgSortedTransitions[ i ].SlotDesc.Slot.RegisterNumber) != (m_rgSortedTransitions[ i-1 ].SlotDesc.Slot.RegisterNumber))
                    numRegisters++;
            }
        }
        else
        {
            numStackSlots++;
        }

        for( ; i < numTransitions; i++ )
        {
            _ASSERTE(!(m_rgSortedTransitions[ i-1 ].SlotDesc.IsRegister));
            if((m_rgSortedTransitions[ i ].SlotDesc.Slot.Stack) != (m_rgSortedTransitions[ i-1 ].SlotDesc.Slot.Stack))
                numStackSlots++;
        }
    }
        

    size_t __registerSize = 0;
    size_t __stackSlotSize = 0;
    size_t __transitionSize = 0;
    size_t __numTransitions = 0;


    //------------------------------------------------------------------
    // Encode registers
    //------------------------------------------------------------------

    i = 0;

    m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(numRegisters, NUM_REGISTERS_ENCBASE);

    UINT32 lastNormRegNum = 0;

    for( int j=0; j < numRegisters; j++ )
    {
        _ASSERTE(m_rgSortedTransitions[ i ].SlotDesc.IsRegister);

        UINT32 currentRegNum = m_rgSortedTransitions[ i ].SlotDesc.Slot.RegisterNumber;

        // Encode slot identification
        UINT32 currentNormRegNum = NORMALIZE_REGISTER(currentRegNum);
        if( j == 0 )
            __registerSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(currentNormRegNum, REGISTER_ENCBASE);
        else
            __registerSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(currentNormRegNum - lastNormRegNum - 1, REGISTER_DELTA_ENCBASE);
        lastNormRegNum = currentNormRegNum;

        LifetimeTransition* pLastEncodedTransition = NULL;

        for( ; i < numTransitions; i++)
        {
            LifetimeTransition* pTransition = &(m_rgSortedTransitions[ i ]);

            if( !(pTransition->SlotDesc.IsRegister) || (pTransition->SlotDesc.Slot.RegisterNumber != currentRegNum))
                break;
            
            if( (pLastEncodedTransition == NULL) )
            {
                // Skip initial going-dead transitions (if any)
                if(!pTransition->BecomesLive)
                    continue;

                // Encode first going-live transition
                size_t normCodeOffset = NORMALIZE_CODE_OFFSET(pTransition->CodeOffset)+1; // Leave 0 available as terminator
                __transitionSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normCodeOffset, NORM_CODE_OFFSET_DELTA_ENCBASE);

                __transitionSize += EncodeFullyInterruptibleSlotFlags(pTransition->SlotDesc);

                __numTransitions++;
            }
            else
            {
                _ASSERTE(pLastEncodedTransition->SlotDesc.IsRegister && pLastEncodedTransition->SlotDesc.Slot.RegisterNumber == currentRegNum);

                // Skip transitions on identical offsets
                // If there are multiple transitions on the same code offset, we'll encode the first one only
                _ASSERTE(i > 0);
                LifetimeTransition* pPrevTransition = &(m_rgSortedTransitions[ i-1 ]);
                if( (pPrevTransition->CodeOffset == pTransition->CodeOffset) )
                {
                    _ASSERTE((!pPrevTransition->BecomesLive || !pTransition->BecomesLive) ||
                                    (pPrevTransition->SlotDesc.IsInterior == pTransition->SlotDesc.IsInterior) && 
                                    (pPrevTransition->SlotDesc.IsPinned == pTransition->SlotDesc.IsPinned));
                    continue;
                }

                // Also skip redundant transitions
                if( (pLastEncodedTransition->BecomesLive == pTransition->BecomesLive) && 
                        (pLastEncodedTransition->SlotDesc.IsInterior ==  pTransition->SlotDesc.IsInterior) &&
                        (pLastEncodedTransition->SlotDesc.IsPinned ==  pTransition->SlotDesc.IsPinned) )
                    continue;

                // Encode transition
                size_t normCodeOffsetDelta = NORMALIZE_CODE_OFFSET(pTransition->CodeOffset) - NORMALIZE_CODE_OFFSET(pLastEncodedTransition->CodeOffset);
                _ASSERTE(normCodeOffsetDelta != 0); // Leave 0 available as terminator
                __transitionSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normCodeOffsetDelta, NORM_CODE_OFFSET_DELTA_ENCBASE);

                if(pTransition->BecomesLive)
                {
                    m_FullyInterruptibleInfoWriter.Write(1, 1);
                    __transitionSize += EncodeFullyInterruptibleSlotFlags(pTransition->SlotDesc) + 1;
                }
                else
                {
                    m_FullyInterruptibleInfoWriter.Write(0, 1);
                    __transitionSize++;
                }

                __numTransitions++;
            }

            pLastEncodedTransition = pTransition;
        }

        // Encode termination for this slot
        m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(0, NORM_CODE_OFFSET_DELTA_ENCBASE);
    }

    
    //------------------------------------------------------------------
    // Encode stack slots
    //------------------------------------------------------------------

    m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(numStackSlots, NUM_STACK_SLOTS_ENCBASE);

    INT32 lastNormStackSlot = 0;

    for( int j=0; j < numStackSlots; j++ )
    {
        _ASSERTE(!m_rgSortedTransitions[ i ].SlotDesc.IsRegister);

        GcStackSlot currentStackSlot = m_rgSortedTransitions[ i ].SlotDesc.Slot.Stack;
        
        // Encode slot identification
        INT32 currentNormStackSlot = NORMALIZE_STACK_SLOT(currentStackSlot.SpOffset);
        if( j == 0 )
            __stackSlotSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthSigned(currentNormStackSlot, STACK_SLOT_ENCBASE);
        else
            __stackSlotSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(currentNormStackSlot - lastNormStackSlot, STACK_SLOT_DELTA_ENCBASE);
        lastNormStackSlot = currentNormStackSlot;
        _ASSERTE((currentStackSlot.Base & ~3) == 0);
        m_FullyInterruptibleInfoWriter.Write(currentStackSlot.Base, 2);
        __stackSlotSize += 2;

        LifetimeTransition* pLastEncodedTransition = NULL;

        for( ; i < numTransitions; i++)
        {
            LifetimeTransition* pTransition = &(m_rgSortedTransitions[ i ]);

            _ASSERTE(!pTransition->SlotDesc.IsRegister);

            if(pTransition->SlotDesc.Slot.Stack != currentStackSlot)
                break;
            
            if( (pLastEncodedTransition == NULL) )
            {
                // Skip initial going-dead transitions (if any)
                if(!pTransition->BecomesLive)
                    continue;

                // Encode first going-live transition
                size_t normCodeOffset = NORMALIZE_CODE_OFFSET(pTransition->CodeOffset)+1; // Leave 0 available as terminator
                __transitionSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normCodeOffset, NORM_CODE_OFFSET_DELTA_ENCBASE);

                __transitionSize += EncodeFullyInterruptibleSlotFlags(pTransition->SlotDesc);

                __numTransitions++;
            }
            else
            {
                _ASSERTE(!(pLastEncodedTransition->SlotDesc.IsRegister) && pLastEncodedTransition->SlotDesc.Slot.Stack == currentStackSlot);

                // Skip transitions on identical offsets
                // If there are multiple transitions on the same code offset, we'll encode the first one only
                _ASSERTE(i > 0);
                LifetimeTransition* pPrevTransition = &(m_rgSortedTransitions[ i-1 ]);
                if( (pPrevTransition->CodeOffset == pTransition->CodeOffset) )
                {
                    _ASSERTE((!pPrevTransition->BecomesLive || !pTransition->BecomesLive) ||
                                    (pPrevTransition->SlotDesc.IsInterior == pTransition->SlotDesc.IsInterior) && 
                                    (pPrevTransition->SlotDesc.IsPinned == pTransition->SlotDesc.IsPinned));
                    continue;
                }

                // Also skip redundant transitions
                if( (pLastEncodedTransition->BecomesLive == pTransition->BecomesLive) && 
                        (pLastEncodedTransition->SlotDesc.IsInterior ==  pTransition->SlotDesc.IsInterior) &&
                        (pLastEncodedTransition->SlotDesc.IsPinned ==  pTransition->SlotDesc.IsPinned) )
                    continue;

                // Encode transition
                size_t normCodeOffsetDelta = NORMALIZE_CODE_OFFSET(pTransition->CodeOffset) - NORMALIZE_CODE_OFFSET(pLastEncodedTransition->CodeOffset);
                _ASSERTE(normCodeOffsetDelta != 0); // Leave 0 available as terminator
                __transitionSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(normCodeOffsetDelta, NORM_CODE_OFFSET_DELTA_ENCBASE);

                if(pTransition->BecomesLive)
                {
                    m_FullyInterruptibleInfoWriter.Write(1, 1);
                    __transitionSize += EncodeFullyInterruptibleSlotFlags(pTransition->SlotDesc) + 1;
                }
                else
                {
                    m_FullyInterruptibleInfoWriter.Write(0, 1);
                    __transitionSize++;
                }

                __numTransitions++;
            }

            pLastEncodedTransition = pTransition;
        }

        // Encode termination for this slot
        __transitionSize += m_FullyInterruptibleInfoWriter.EncodeVarLengthUnsigned(0, NORM_CODE_OFFSET_DELTA_ENCBASE);
    }

}

size_t GcInfoEncoder::GetByteCount()
{
    return   m_HeaderInfoWriter.GetByteCount() +
#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
                        m_PartiallyInterruptibleInfoWriter.GetByteCount() +
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif
                        m_FullyInterruptibleInfoWriter.GetByteCount();
}

//
// Write encoded information to its final destination and frees temporary buffers.
// The encoder shouldn't be used anymore after calling this method.
//
BYTE* GcInfoEncoder::Emit(BYTE* destBuffer)
{
    size_t cbGcInfoSize = GetByteCount();

    _ASSERTE( destBuffer );

    m_HeaderInfoWriter.CopyTo( destBuffer );
    destBuffer += m_HeaderInfoWriter.GetByteCount();
    m_HeaderInfoWriter.Dispose();

#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    m_PartiallyInterruptibleInfoWriter.CopyTo( destBuffer );
    destBuffer += m_PartiallyInterruptibleInfoWriter.GetByteCount();
    m_PartiallyInterruptibleInfoWriter.Dispose();
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif

    m_FullyInterruptibleInfoWriter.CopyTo( destBuffer );
    m_FullyInterruptibleInfoWriter.Dispose();

    return destBuffer;
}

void * GcInfoEncoder::eeAllocGCInfo (size_t        blockSize)
{
    return m_pCorJitInfo->allocGCInfo((ULONG)blockSize);
}


BitStreamWriter::BitStreamWriter( IJitAllocator* pAllocator )
{
    m_pAllocator = pAllocator;
    m_BitCount = 0;
#ifdef _DEBUG
    m_MemoryBlocksCount = 0;
#endif

    // We are going to need at least one memory block, so we pre-allocate it
    AllocMemoryBlock();
    InitCurrentSlot();
}

//
// bit 0 is the least significative bit
// The stream encodes the first come bit in the least significant bit of each byte
//
void BitStreamWriter::Write( size_t data, int count )
{
    _ASSERTE( count > 0 );
    _ASSERT( count <= sizeof( size_t )*8 );

    // Increment it now as we change count later on
    m_BitCount += count;

    if( count > m_FreeBitsInCurrentSlot )
    {
        if( m_FreeBitsInCurrentSlot > 0 )
        {
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


void BitStreamWriter::CopyTo( BYTE* buffer )
{
    int i,c;
    BYTE* source = NULL;

    MemoryBlockDesc* pMemBlockDesc = m_MemoryBlocks.GetHead();
    _ASSERTE( pMemBlockDesc != NULL );
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

    m_pAllocator->Free( m_SlotMappings );
#endif
}

}

#endif // VERIFY_GCINFO

