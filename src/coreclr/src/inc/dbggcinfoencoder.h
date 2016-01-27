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

#ifndef __DBGGCINFOENCODER_H__
#define __DBGGCINFOENCODER_H__

#include <windows.h>

#include <wchar.h>
#include <stdio.h>

#include "utilcode.h"
#include "corjit.h"
#include "list.h"     // for SList
#include "arraylist.h"

#include "stdmacros.h"
#include "gcinfotypes.h"


class IJitAllocator;



namespace DbgGcInfo {

//-----------------------------------------------------------------------------
// The following macro controls whether the encoder has to call the IJitAllocator::Free method
// Don't call IJitAllocator::Free for mscorjit64.dll
//-----------------------------------------------------------------------------
//#define MUST_CALL_JITALLOCATOR_FREE


class BitStreamWriter
{
public:
    BitStreamWriter( IJitAllocator* pAllocator );
    void Write( size_t data, int count );

    inline size_t GetBitCount()
    {
        return m_BitCount;
    }

    inline size_t GetByteCount()
    {
        return ( m_BitCount + 7 )  / 8;
    }


    void CopyTo( BYTE* buffer );
    void Dispose();

    //--------------------------------------------------------
    // Encode variable length numbers
    // Uses base+1 bits at minimum
    // Bits 0..(base-1) represent the encoded quantity
    // If it doesn't fit, set bit #base to 1 and use base+1 more bits
    //--------------------------------------------------------
    int EncodeVarLengthUnsigned( size_t n, int base )
    {
        _ASSERTE((base > 0) && (base < sizeof(size_t)*8));
        size_t numEncodings = 1 << base;
		int bitsUsed = base+1;
        for( ; ; bitsUsed += base+1)
        {
            if( n < numEncodings )
            {
                Write( n, base+1 ); // This sets the extension bit to zero
                return bitsUsed;
            }
            else
            {
                size_t currentChunk = n & (numEncodings-1);
                Write( currentChunk | numEncodings, base+1 );
                n >>= base;
            }
        }
        return bitsUsed;
    }

    //--------------------------------------------------------
    // Signed quantities are encoded the same as unsigned
    // The most relevant difference is that a number is considered
    // to fit in base bits if the topmost bit of a base-long chunk
    // matches the sign of the whole number
    //--------------------------------------------------------
    int EncodeVarLengthSigned( SSIZE_T n, int base )
    {
        _ASSERTE((base > 0) && (base < sizeof(SSIZE_T)*8));
        size_t numEncodings = 1 << base;
        for(int bitsUsed = base+1; ; bitsUsed += base+1)
        {
            size_t currentChunk = ((size_t) n) & (numEncodings-1);
            size_t topmostBit = currentChunk & (numEncodings >> 1);
            n >>= base; // signed arithmetic shift
            if( topmostBit && (n == (SSIZE_T)-1) || !topmostBit && (n == 0))
            {
                // The topmost bit correctly represents the sign
                Write( currentChunk, base+1 ); // This sets the extension bit to zero
                return bitsUsed;
            }
            else
            {
                Write( currentChunk | numEncodings, base+1 );
            }
        }
    }

private:

    class MemoryBlockDesc
    {
    public:
        size_t* StartAddress;
        SLink m_Link;

        inline void Init()
        {
            m_Link.m_pNext = NULL;
        }
    };

    IJitAllocator* m_pAllocator;
    size_t m_BitCount;
    int m_FreeBitsInCurrentSlot;
    SList<MemoryBlockDesc> m_MemoryBlocks;
    const static int m_MemoryBlockSize = 512;    // must be a multiple of the pointer size
    size_t* m_pCurrentSlot;            // bits are written through this pointer
    size_t* m_OutOfBlockSlot;        // sentinel value to determine when the block is full
#ifdef _DEBUG
    int m_MemoryBlocksCount;
#endif

private:
    // Writes bits knowing that they will all fit in the current memory slot
    inline void WriteInCurrentSlot( size_t data, int count )
    {
        data &= SAFE_SHIFT_LEFT(1, count) - 1;

        data <<= (sizeof( size_t )*8-m_FreeBitsInCurrentSlot);

        *m_pCurrentSlot |= data;
    }

    void AllocMemoryBlock();

    inline void InitCurrentSlot()
    {
        m_FreeBitsInCurrentSlot = sizeof( size_t )*8;
        *m_pCurrentSlot = 0;
    }

};

struct GcSlotDesc
{
    union
    {
        UINT32 RegisterNumber;
        GcStackSlot Stack;
    } Slot;
    unsigned IsRegister : 1;
    unsigned IsInterior : 1;
    unsigned IsPinned : 1;
};



typedef UINT32 GcSlotId;


class GcSlotSet
{
    friend class GcInfoEncoder;
public:
    GcSlotSet( GcInfoEncoder * pEncoder );

    // Copy constructor
    GcSlotSet( GcSlotSet & other );

    inline void Add( GcSlotId slotId );

    inline void Remove( GcSlotId slotId );

// Not used
#if 0
    inline void RemoveAll();

    void Add( GcSlotSet & other );
    void Subtract( GcSlotSet & other );
    void Intersect( GcSlotSet & other );
#endif

    // Must be called when done with the object
    inline void Dispose();

private:
    // A bit vector representing the set
    BYTE * m_Data;

    int m_NumBytes;

    GcInfoEncoder* m_pEncoder;
};


class GcInfoEncoder
{
public:
    GcInfoEncoder(
            ICorJitInfo*                pCorJitInfo,
            CORINFO_METHOD_INFO*        pMethodInfo,
            IJitAllocator*              pJitAllocator
            );


    //------------------------------------------------------------------------
    // Interruptibility
    //------------------------------------------------------------------------

    // An instruction at offset x will be interruptible
    //  if-and-only-if startInstructionOffset <= x < startInstructionOffset+length
    void DefineInterruptibleRange( UINT32 startInstructionOffset, UINT32 length );


    //------------------------------------------------------------------------
    // Slot information
    //------------------------------------------------------------------------

    //
    // spOffset are always relative to the SP of the caller (same as SP at the method entry and exit)
    // Negative offsets describe GC refs in the local and outgoing areas.
    // Positive offsets describe GC refs in the scratch area
    // Note that if the dynamic allocation area is resized, the outgoing area will not be valid anymore
    //  Old slots must be declared dead and new ones can be defined.
    //  It's up to the JIT to do the right thing. We don't enforce this.
    //

    GcSlotId GetRegisterSlotId( UINT32 regNum, GcSlotFlags flags );
    GcSlotId GetStackSlotId( INT32 spOffset, GcSlotFlags flags, GcStackSlotBase spBase = GC_CALLER_SP_REL );

    //
    // After a FinalizeSlotIds is called, no more slot definitions can be made.
    // FinalizeSlotIds must be called once and only once before calling DefineGcStateAtCallSite
    // If no call sites are described, calling FinalizeSlotIds can and should (for performance reasons) be avoided
    //
    void FinalizeSlotIds();


#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    //------------------------------------------------------------------------
    // Partially-interruptible information
    //------------------------------------------------------------------------


    void DefineGcStateAtSafePoint(
                UINT32          instructionOffset,
                GcSlotSet       &liveSlots
                );

#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
#endif

    //------------------------------------------------------------------------
    // Fully-interruptible information
    //------------------------------------------------------------------------

    //
    // For inputs, pass zero as offset
    //

    // This method defines what the GC state of a slot is when a thread's suspension IP
    //  is equal to instructionOffset

    void SetSlotState(              UINT32      instructionOffset,
                                    GcSlotId    slotId,
                                    GcSlotState slotState
                                    );



    //------------------------------------------------------------------------
    // Miscellaneous method information
    //------------------------------------------------------------------------

    void SetSecurityObjectStackSlot( INT32 spOffset );
    void SetPSPSymStackSlot( INT32 spOffsetPSPSym );
    void SetGenericsInstContextStackSlot( INT32 spOffsetGenericsContext );
    void SetIsVarArg();
    void SetCodeLength( UINT32 length );

    // Optional in the general case. Required if the method uses GC_FRAMEREG_REL stack slots
    void SetStackBaseRegister( UINT32 registerNumber );
    void SetSizeOfEditAndContinuePreservedArea( UINT32 size );

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    void SetSizeOfStackOutgoingAndScratchArea( UINT32 size );
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


    //------------------------------------------------------------------------
    // Encoding
    //------------------------------------------------------------------------

    //
    // Build() encodes GC information into temporary buffers.
    // The method description cannot change after Build is called
    //
    void Build();

    //
    // Write encoded information to its final destination and frees temporary buffers.
    // The encoder shouldn't be used anymore after calling this method.
    // It returns a pointer to the destination buffer, which address is byte-aligned
    //
    size_t GetByteCount();
    BYTE* Emit(BYTE* dest);

private:

    friend class LifetimeTransitionsQuickSort;
    friend class LifetimeTransitionsQuickSortByOffset;

    struct LifetimeTransition
    {
        UINT32 CodeOffset;
        GcSlotDesc SlotDesc;
        bool BecomesLive;
    };

    class LifetimeTransitionAllocator
    {
    public:

        static void *Alloc (void *context, SIZE_T cb);
        static void Free (void *context, void *pv);
    };

    ICorJitInfo*                m_pCorJitInfo;
    CORINFO_METHOD_INFO*        m_pMethodInfo;
    IJitAllocator*              m_pAllocator;

#ifdef _DEBUG
    char *m_MethodName, *m_ModuleName;
#endif

    BitStreamWriter     m_HeaderInfoWriter;
#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    BitStreamWriter     m_PartiallyInterruptibleInfoWriter;
#endif
#endif
    BitStreamWriter     m_FullyInterruptibleInfoWriter;

    StructArrayList<LifetimeTransition, 64, 2, LifetimeTransitionAllocator> m_LifetimeTransitions;
    LifetimeTransition *m_rgSortedTransitions;

    bool   m_IsVarArg;
    INT32  m_SecurityObjectStackSlot;
    INT32  m_PSPSymStackSlot;
    INT32  m_GenericsInstContextStackSlot;
    UINT32 m_CodeLength;
    UINT32 m_StackBaseRegister;
    UINT32 m_SizeOfEditAndContinuePreservedArea;
    UINT32 m_LastInterruptibleRangeStopOffset;
    UINT32 m_NumInterruptibleRanges;
    
#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32 m_SizeOfStackOutgoingAndScratchArea;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

    void * eeAllocGCInfo (size_t        blockSize);

    inline int EncodeFullyInterruptibleSlotFlags(GcSlotDesc slotDesc)
    {
        int flagEnc = 1;
        if( slotDesc.IsInterior )
            flagEnc |= 0x2;
        if( slotDesc.IsPinned )
            flagEnc |= 0x4;
        if(flagEnc == 1)
        {
            m_FullyInterruptibleInfoWriter.Write(0, 1);
            return 1;
        }
        else
        {
            m_FullyInterruptibleInfoWriter.Write(flagEnc, 3);
            return 3;
        }
    }      


private:

    friend class GcSlotSet;
    friend class EncoderCheckState;

    static const UINT32 m_MappingTableInitialSize = 32;
    UINT32 m_MappingTableSize;
    UINT32 m_NumSlotMappings;
    GcSlotDesc *m_SlotMappings;

#if 0
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    UINT32 m_NumSafePointsWithGcState;
#endif
#endif

    void GrowMappingTable();

#ifdef _DEBUG
    bool m_IsMappingTableFrozen;
#endif
};



// Not used
#if 0

void GcSlotSet::RemoveAll()
{
    ZeroMemory( m_Data, m_NumBytes );
}

#endif


void GcSlotSet::Dispose()
{
#ifdef MUST_CALL_JITALLOCATOR_FREE
    m_pEncoder->m_pAllocator->Free( m_Data );
#endif
}


}

#endif // !__DBGGCINFOENCODER_H__

#endif // VERIFY_GCINFO

