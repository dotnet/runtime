// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/*****************************************************************
 *
 * GC Information Encoding API
 *
 *****************************************************************/

/*****************************************************************

 ENCODING LAYOUT

 1. Header

 Slim Header for simple and common cases:
    - EncodingType[Slim]
    - ReturnKind (Fat: 2 bits)
    - CodeLength
    - NumCallSites (#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)

 Fat Header for other cases:
    - EncodingType[Fat]
    - Flag:     isVarArg,
                hasSecurityObject,
                hasGSCookie,
                hasPSPSymStackSlot,
                hasGenericsInstContextStackSlot,
                hasStackBaseregister,
                wantsReportOnlyLeaf (AMD64 use only),
                hasTailCalls (ARM/ARM64 only)
                hasSizeOfEditAndContinuePreservedArea
                hasReversePInvokeFrame,
    - ReturnKind (Fat: 4 bits)
    - CodeLength
    - Prolog (if hasSecurityObject || hasGenericsInstContextStackSlot || hasGSCookie)
    - Epilog (if hasGSCookie)
    - SecurityObjectStackSlot (if any)
    - GSCookieStackSlot (if any)
    - PSPSymStackSlot (if any)
    - GenericsInstContextStackSlot (if any)
    - StackBaseRegister (if any)
    - SizeOfEditAndContinuePreservedArea (if any)
    - ReversePInvokeFrameSlot (if any)
    - SizeOfStackOutgoingAndScratchArea (#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA)
    - NumCallSites (#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
    - NumInterruptibleRanges

 2. Call sites offsets (#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
 3. Fully-interruptible ranges
 4. Slot table
 5. GC state at call sites (#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
 6. GC state at try clauses (#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED)
 7. Chunk pointers
 8. Chunk encodings


 STANDALONE_BUILD

 The STANDALONE_BUILD switch can be used to build the GcInfoEncoder library
 independently by clients outside the CoreClr tree.

 The GcInfo library uses some custom data-structures (ex: ArrayList, SimplerHashTable)
 and includes some utility libraries (ex: UtilCode) which pull in several other
 headers with considerable unrelated content. Rather than porting all the
 utility code to suite other clients, the  STANDALONE_BUILD switch can be used
 to include only the minimal set of headers specific to GcInfo encodings.

 Clients of STANDALONE_BUILD will likely use standard library
 implementations of data-structures like ArrayList, HashMap etc., in place
 of the custom implementation currently used by GcInfoEncoder.

 Rather than spew the GcInfoEnoder code with
 #ifdef STANDALONE_BUILD ... #else .. #endif blocks, we include a special
 header GcInfoUtil.h in STANDALONE_BUILD mode.  GcInfoUtil.h is expected to
 supply the interface/implementation for the data-structures and utilities
 used by GcInfoEncoder. This header should be provided by the clients doing
 the standalone build in their source tree.

*****************************************************************/


#ifndef __GCINFOENCODER_H__
#define __GCINFOENCODER_H__

#ifdef STANDALONE_BUILD
#include <wchar.h>
#include <stdio.h>
#include "GcInfoUtil.h"
#include "corjit.h"
#else
#include <windows.h>
#include <wchar.h>
#include <stdio.h>
#include "corjit.h"
#include "iallocator.h"
#include "gcinfoarraylist.h"
#include "stdmacros.h"
#include "eexcp.h"
#endif

#include "gcinfotypes.h"

// As stated in issue #6008, GcInfoSize should be incorporated into debug builds.
#ifdef _DEBUG
#define MEASURE_GCINFO
#endif

#ifdef MEASURE_GCINFO
struct GcInfoSize
{
    size_t TotalSize;

    size_t NumMethods;
    size_t NumCallSites;
    size_t NumRanges;
    size_t NumRegs;
    size_t NumStack;
    size_t NumUntracked;
    size_t NumTransitions;
    size_t SizeOfCode;
    size_t EncInfoSize;

    size_t UntrackedSlotSize;
    size_t NumUntrackedSize;
    size_t FlagsSize;
    size_t RetKindSize;
    size_t CodeLengthSize;
    size_t ProEpilogSize;
    size_t SecObjSize;
    size_t GsCookieSize;
    size_t PspSymSize;
    size_t GenericsCtxSize;
    size_t StackBaseSize;
    size_t ReversePInvokeFrameSize;
    size_t FixedAreaSize;
    size_t NumCallSitesSize;
    size_t NumRangesSize;
    size_t CallSitePosSize;
    size_t RangeSize;
    size_t NumRegsSize;
    size_t NumStackSize;
    size_t RegSlotSize;
    size_t StackSlotSize;
    size_t CallSiteStateSize;
    size_t EhPosSize;
    size_t EhStateSize;
    size_t ChunkPtrSize;
    size_t ChunkMaskSize;
    size_t ChunkFinalStateSize;
    size_t ChunkTransitionSize;

    GcInfoSize();
    GcInfoSize& operator+=(const GcInfoSize& other);
    void Log(DWORD level, const char * header);
};
#endif

struct GcSlotDesc
{
    union
    {
        UINT32 RegisterNumber;
        GcStackSlot Stack;
    } Slot;
    GcSlotFlags Flags;

    BOOL IsRegister() const
    {
        return (Flags & GC_SLOT_IS_REGISTER);
    }
    BOOL IsInterior() const
    {
        return (Flags & GC_SLOT_INTERIOR);
    }
    BOOL IsPinned() const
    {
        return (Flags & GC_SLOT_PINNED);
    }
    BOOL IsUntracked() const
    {
        return (Flags & GC_SLOT_UNTRACKED);
    }
    BOOL IsDeleted() const
    {
        return (Flags & GC_SLOT_IS_DELETED);
    }
    void MarkDeleted()
    {
        Flags = (GcSlotFlags) (Flags | GC_SLOT_IS_DELETED);
    }
};

class BitArray;
class BitStreamWriter
{
public:
    BitStreamWriter( IAllocator* pAllocator );

    // bit 0 is the least significative bit
    void Write( size_t data, UINT32 count );

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
    // Compute the number of bits used to encode variable length numbers
    // Uses base+1 bits at minimum
    // Bits 0..(base-1) represent the encoded quantity
    // If it doesn't fit, set bit #base to 1 and use base+1 more bits
    //--------------------------------------------------------
    static int SizeofVarLengthUnsigned( size_t n, UINT32 base );

    //--------------------------------------------------------
    // Encode variable length numbers
    // Uses base+1 bits at minimum
    // Bits 0..(base-1) represent the encoded quantity
    // If it doesn't fit, set bit #base to 1 and use base+1 more bits
    //--------------------------------------------------------
    int EncodeVarLengthUnsigned( size_t n, UINT32 base );

    //--------------------------------------------------------
    // Signed quantities are encoded the same as unsigned
    // The most relevant difference is that a number is considered
    // to fit in base bits if the topmost bit of a base-long chunk
    // matches the sign of the whole number
    //--------------------------------------------------------
    int EncodeVarLengthSigned( SSIZE_T n, UINT32 base );

private:
    class MemoryBlockList;
    class MemoryBlock
    {
        friend class MemoryBlockList;
        MemoryBlock* m_next;

    public:
        size_t Contents[];

        inline MemoryBlock* Next()
        {
            return m_next;
        }
    };

    class MemoryBlockList
    {
        MemoryBlock* m_head;
        MemoryBlock* m_tail;

    public:
        MemoryBlockList();

        inline MemoryBlock* Head()
        {
            return m_head;
        }

        MemoryBlock* AppendNew(IAllocator* allocator, size_t bytes);
        void Dispose(IAllocator* allocator);
    };

    IAllocator* m_pAllocator;
    size_t m_BitCount;
    UINT32 m_FreeBitsInCurrentSlot;
    MemoryBlockList m_MemoryBlocks;
    const static int m_MemoryBlockSize = 128;    // must be a multiple of the pointer size
    size_t* m_pCurrentSlot;            // bits are written through this pointer
    size_t* m_OutOfBlockSlot;        // sentinel value to determine when the block is full
#ifdef _DEBUG
    int m_MemoryBlocksCount;
#endif

private:
    // Writes bits knowing that they will all fit in the current memory slot
    inline void WriteInCurrentSlot( size_t data, UINT32 count )
    {
        data &= SAFE_SHIFT_LEFT(1, count) - 1;
        data <<= (BITS_PER_SIZE_T - m_FreeBitsInCurrentSlot);
        *m_pCurrentSlot |= data;
    }

    inline void AllocMemoryBlock()
    {
        _ASSERTE( IS_ALIGNED( m_MemoryBlockSize, sizeof( size_t ) ) );
        MemoryBlock* pMemBlock = m_MemoryBlocks.AppendNew(m_pAllocator, m_MemoryBlockSize);

        m_pCurrentSlot = pMemBlock->Contents;
        m_OutOfBlockSlot = m_pCurrentSlot + m_MemoryBlockSize / sizeof( size_t );

#ifdef _DEBUG
           m_MemoryBlocksCount++;
#endif

    }

    inline void InitCurrentSlot()
    {
        m_FreeBitsInCurrentSlot = BITS_PER_SIZE_T;
        *m_pCurrentSlot = 0;
    }
};


typedef UINT32 GcSlotId;


inline UINT32 GetNormCodeOffsetChunk(UINT32 normCodeOffset)
{
    return normCodeOffset / NUM_NORM_CODE_OFFSETS_PER_CHUNK;
}

inline UINT32 GetCodeOffsetChunk(UINT32 codeOffset)
{
    return (NORMALIZE_CODE_OFFSET(codeOffset)) / NUM_NORM_CODE_OFFSETS_PER_CHUNK;
}

enum GENERIC_CONTEXTPARAM_TYPE
{
    GENERIC_CONTEXTPARAM_NONE = 0,
    GENERIC_CONTEXTPARAM_MT = 1,
    GENERIC_CONTEXTPARAM_MD = 2,
    GENERIC_CONTEXTPARAM_THIS = 3,
};

extern void DECLSPEC_NORETURN ThrowOutOfMemory();

class GcInfoEncoder
{
public:
    typedef void (*NoMemoryFunction)(void);

    GcInfoEncoder(
            ICorJitInfo*                pCorJitInfo,
            CORINFO_METHOD_INFO*        pMethodInfo,
            IAllocator*                 pJitAllocator,
            NoMemoryFunction            pNoMem = ::ThrowOutOfMemory
            );

    struct LifetimeTransition
    {
        UINT32 CodeOffset;
        GcSlotId SlotId;
        BYTE BecomesLive;
        BYTE IsDeleted;
    };


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    void DefineCallSites(UINT32* pCallSites, BYTE* pCallSiteSizes, UINT32 numCallSites);
#endif

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
    // If spOffset is relative to the current SP, spOffset must be non-negative.
    // If spOffset is relative to the SP of the caller (same as SP at the method entry and exit)
    //   Negative offsets describe GC refs in the local and outgoing areas.
    //   Positive offsets describe GC refs in the scratch area
    // Note that if the dynamic allocation area is resized, the outgoing area will not be valid anymore
    //  Old slots must be declared dead and new ones can be defined.
    //  It's up to the JIT to do the right thing. We don't enforce this.

    GcSlotId GetRegisterSlotId( UINT32 regNum, GcSlotFlags flags );
    GcSlotId GetStackSlotId( INT32 spOffset, GcSlotFlags flags, GcStackSlotBase spBase = GC_CALLER_SP_REL );

    //
    // After a FinalizeSlotIds is called, no more slot definitions can be made.
    // FinalizeSlotIds must be called once and only once before calling Build()
    //
    void FinalizeSlotIds();


    //------------------------------------------------------------------------
    // Fully-interruptible information
    //------------------------------------------------------------------------

    //
    // For inputs, pass zero as offset
    //

    // Indicates that the GC state of slot "slotId" becomes (and remains, until another transition)
    // "slotState" after the instruction preceding "instructionOffset" (so it is first in this state when
    // the IP of a suspended thread is at this instruction offset).

    void SetSlotState(              UINT32      instructionOffset,
                                    GcSlotId    slotId,
                                    GcSlotState slotState
                                    );


    //------------------------------------------------------------------------
    // ReturnKind
    //------------------------------------------------------------------------

    void SetReturnKind(ReturnKind returnKind);

    //------------------------------------------------------------------------
    // Miscellaneous method information
    //------------------------------------------------------------------------

    void SetSecurityObjectStackSlot( INT32 spOffset );
    void SetPrologSize( UINT32 prologSize );
    void SetGSCookieStackSlot( INT32 spOffsetGSCookie, UINT32 validRangeStart, UINT32 validRangeEnd );
    void SetPSPSymStackSlot( INT32 spOffsetPSPSym );
    void SetGenericsInstContextStackSlot( INT32 spOffsetGenericsContext, GENERIC_CONTEXTPARAM_TYPE type);
    void SetReversePInvokeFrameSlot(INT32 spOffset);
    void SetIsVarArg();
    void SetCodeLength( UINT32 length );

    // Optional in the general case. Required if the method uses GC_FRAMEREG_REL stack slots
    void SetStackBaseRegister( UINT32 registerNumber );

    // Number of slots preserved during EnC remap
    void SetSizeOfEditAndContinuePreservedArea( UINT32 size );
#ifdef TARGET_ARM64
    void SetSizeOfEditAndContinueFixedStackFrame( UINT32 size );
#endif

#ifdef TARGET_AMD64
    // Used to only report a frame once for the leaf function/funclet
    // instead of once for each live function/funclet on the stack.
    // Called only by RyuJIT (not JIT64)
    void SetWantsReportOnlyLeaf();
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
    void SetHasTailCalls();
#endif // TARGET_AMD64

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
    BYTE* Emit();

private:

    friend struct CompareLifetimeTransitionsByOffsetThenSlot;
    friend struct CompareLifetimeTransitionsByChunk;


    struct InterruptibleRange
    {
        UINT32 NormStartOffset;
        UINT32 NormStopOffset;
    };

    ICorJitInfo*                m_pCorJitInfo;
    CORINFO_METHOD_INFO*        m_pMethodInfo;
    IAllocator*                 m_pAllocator;
    NoMemoryFunction            m_pNoMem;

#ifdef _DEBUG
    const char *m_MethodName, *m_ModuleName;
#endif

    BitStreamWriter     m_Info1;    // Used for everything except for chunk encodings
    BitStreamWriter     m_Info2;    // Used for chunk encodings

    GcInfoArrayList<InterruptibleRange, 8> m_InterruptibleRanges;
    GcInfoArrayList<LifetimeTransition, 64> m_LifetimeTransitions;

    bool   m_IsVarArg;
#if defined(TARGET_AMD64)
    bool   m_WantsReportOnlyLeaf;
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
    bool   m_HasTailCalls;
#endif // TARGET_AMD64
    INT32  m_SecurityObjectStackSlot;
    INT32  m_GSCookieStackSlot;
    UINT32 m_GSCookieValidRangeStart;
    UINT32 m_GSCookieValidRangeEnd;
    INT32  m_PSPSymStackSlot;
    INT32  m_GenericsInstContextStackSlot;
    GENERIC_CONTEXTPARAM_TYPE m_contextParamType;
    ReturnKind m_ReturnKind;
    UINT32 m_CodeLength;
    UINT32 m_StackBaseRegister;
    UINT32 m_SizeOfEditAndContinuePreservedArea;
#ifdef TARGET_ARM64
    UINT32 m_SizeOfEditAndContinueFixedStackFrame;
#endif
    INT32  m_ReversePInvokeFrameSlot;
    InterruptibleRange* m_pLastInterruptibleRange;

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32 m_SizeOfStackOutgoingAndScratchArea;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

    void * eeAllocGCInfo (size_t        blockSize);

private:

    friend class EncoderCheckState;

    static const UINT32 m_SlotTableInitialSize = 32;
    UINT32 m_SlotTableSize;
    UINT32 m_NumSlots;
    GcSlotDesc *m_SlotTable;

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    UINT32* m_pCallSites;
    BYTE* m_pCallSiteSizes;
    UINT32 m_NumCallSites;
#endif // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED

    void GrowSlotTable();

    void WriteSlotStateVector(BitStreamWriter &writer, const BitArray& vector);

    UINT32 SizeofSlotStateVarLengthVector(const BitArray& vector, UINT32 baseSkip, UINT32 baseRun);
    void SizeofSlotStateVarLengthVector(const BitArray& vector, UINT32 baseSkip, UINT32 baseRun, UINT32 * pSizeofSimple, UINT32 * pSizeofRLE, UINT32 * pSizeofRLENeg);
    UINT32 WriteSlotStateVarLengthVector(BitStreamWriter &writer, const BitArray& vector, UINT32 baseSkip, UINT32 baseRun);

    bool IsAlwaysScratch(GcSlotDesc &slot);

    // Assumes that "*ppTransitions" is has size "numTransitions", is sorted by CodeOffset then by SlotId,
    // and that "*ppEndTransitions" points one beyond the end of the array.  If "*ppTransitions" contains
    // any dead/live transitions pairs for the same CodeOffset and SlotID, removes those, by allocating a
    // new array, and copying the non-removed elements into it.  If it does this, sets "*ppTransitions" to
    // point to the new array, "*pNumTransitions" to its shorted length, and "*ppEndTransitions" to
    // point one beyond the used portion of this array.
    void EliminateRedundantLiveDeadPairs(LifetimeTransition** ppTransitions,
                                         size_t* pNumTransitions,
                                         LifetimeTransition** ppEndTransitions);

#ifdef _DEBUG
    bool m_IsSlotTableFrozen;
#endif

#ifdef MEASURE_GCINFO
    GcInfoSize m_CurrentMethodSize;
#endif
};

#endif // !__GCINFOENCODER_H__
