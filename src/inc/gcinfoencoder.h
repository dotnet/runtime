// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************
 *
 * GC Information Encoding API
 *
 *****************************************************************/

/*****************************************************************

 ENCODING LAYOUT

 1. Header
    - Flag:     isVarArg, 
                hasSecurityObject, 
                hasGSCookie,
                hasPSPSymStackSlot,
                hasGenericsInstContextStackSlot, 
                hasStackBaseregister,
                wantsReportOnlyLeaf,
                hasSizeOfEditAndContinuePreservedArea
    - CodeLength
    - Prolog (if hasSecurityObject || hasGenericsInstContextStackSlot || hasGSCookie)
    - Epilog (if hasGSCookie)
    - SecurityObjectStackSlot (if any)
    - GSCookieStackSlot (if any)
    - PSPSymStackSlot (if any)
    - GenericsInstContextStackSlot (if any)
    - StackBaseRegister (if any)
    - SizeOfEditAndContinuePreservedArea (if any)
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
#include "utilcode.h"
#include "corjit.h"
#include "slist.h"     // for SList
#include "arraylist.h"
#include "iallocator.h"
#include "stdmacros.h"
#include "eexcp.h"
#endif

#include "gcinfotypes.h"

#ifdef VERIFY_GCINFO
#include "dbggcinfoencoder.h"
#endif //VERIFY_GCINFO

#ifdef MEASURE_GCINFO
#define GCINFO_WRITE(writer, val, numBits, counter) \
    {                                               \
        writer.Write(val, numBits);                 \
        m_CurrentMethodSize.counter += numBits;     \
        m_CurrentMethodSize.TotalSize += numBits;   \
    }
#define GCINFO_WRITE_VARL_U(writer, val, base, counter) \
    {                                               \
        size_t __temp =                             \
         writer.EncodeVarLengthUnsigned(val, base); \
        m_CurrentMethodSize.counter += __temp;      \
        m_CurrentMethodSize.TotalSize += __temp;    \
    }
#define GCINFO_WRITE_VARL_S(writer, val, base, counter) \
    {                                               \
        size_t __temp =                             \
         writer.EncodeVarLengthSigned(val, base);   \
        m_CurrentMethodSize.counter += __temp;      \
        m_CurrentMethodSize.TotalSize += __temp;    \
    }
#define GCINFO_WRITE_VECTOR(writer, vector, counter)   \
    {                                               \
        WriteSlotStateVector(writer, vector);       \
        for(UINT32 i = 0; i < m_NumSlots; i++)      \
        {                                           \
            if(!m_SlotTable[i].IsDeleted() &&       \
               !m_SlotTable[i].IsUntracked())       \
            {                                       \
                m_CurrentMethodSize.counter++;      \
                m_CurrentMethodSize.TotalSize++;    \
            }                                       \
        }                                           \
    }
#define GCINFO_WRITE_VAR_VECTOR(writer, vector, baseSkip, baseRun, counter)   \
    {                                                                         \
        size_t __temp =                                                       \
           WriteSlotStateVarLengthVector(writer, vector, baseSkip, baseRun);  \
        m_CurrentMethodSize.counter += __temp;                                \
        m_CurrentMethodSize.TotalSize += __temp;                              \
    }
#else
#define GCINFO_WRITE(writer, val, numBits, counter) \
        writer.Write(val, numBits);

#define GCINFO_WRITE_VARL_U(writer, val, base, counter) \
        writer.EncodeVarLengthUnsigned(val, base);

#define GCINFO_WRITE_VARL_S(writer, val, base, counter) \
        writer.EncodeVarLengthSigned(val, base);

#define GCINFO_WRITE_VECTOR(writer, vector, counter)   \
        WriteSlotStateVector(writer, vector);

#define GCINFO_WRITE_VAR_VECTOR(writer, vector, baseSkip, baseRun, counter)   \
        WriteSlotStateVarLengthVector(writer, vector, baseSkip, baseRun);
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
    size_t NumEh;
    size_t NumTransitions;
    size_t SizeOfCode;

    size_t FlagsSize;
    size_t CodeLengthSize;
    size_t ProEpilogSize;
    size_t SecObjSize;
    size_t GsCookieSize;
    size_t GenericsCtxSize;
    size_t PspSymSize;
    size_t StackBaseSize;
    size_t FrameMarkerSize;
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
    size_t NumEhSize;
    size_t EhPosSize;
    size_t EhStateSize;
    size_t ChunkPtrSize;
    size_t ChunkMaskSize;
    size_t ChunkFinalStateSize;
    size_t ChunkTransitionSize;

    GcInfoSize()
    {
        memset(this, 0, sizeof(GcInfoSize));
    }

    GcInfoSize& operator+=(GcInfoSize& other)
    {
        TotalSize += other.TotalSize;

        NumMethods += other.NumMethods;
        NumCallSites += other.NumCallSites;
        NumRanges += other.NumRanges;
        NumRegs += other.NumRegs;
        NumStack += other.NumStack;
        NumEh += other.NumEh;
        NumTransitions += other.NumTransitions;
        SizeOfCode += other.SizeOfCode;
        
        FlagsSize += other.FlagsSize;
        CodeLengthSize += other.CodeLengthSize;
        ProEpilogSize += other.ProEpilogSize;
        SecObjSize += other.SecObjSize;
        GsCookieSize += other.GsCookieSize;
        GenericsCtxSize += other.GenericsCtxSize;
        PspSymSize += other.PspSymSize;
        StackBaseSize += other.StackBaseSize;
        FrameMarkerSize += other.FrameMarkerSize;
        FixedAreaSize += other.FixedAreaSize;
        NumCallSitesSize += other.NumCallSitesSize;
        NumRangesSize += other.NumRangesSize;
        CallSitePosSize += other.CallSitePosSize;
        RangeSize += other.RangeSize;
        NumRegsSize += other.NumRegsSize;
        NumStackSize += other.NumStackSize;
        RegSlotSize += other.RegSlotSize;
        StackSlotSize += other.StackSlotSize;
        CallSiteStateSize += other.CallSiteStateSize;
        NumEhSize += other.NumEhSize;
        EhPosSize += other.EhPosSize;
        EhStateSize += other.EhStateSize;
        ChunkPtrSize += other.ChunkPtrSize;
        ChunkMaskSize += other.ChunkMaskSize;
        ChunkFinalStateSize += other.ChunkFinalStateSize;
        ChunkTransitionSize += other.ChunkTransitionSize;

        return *this;
    }

    void Log(DWORD level, const char * header);
};
#endif

//-----------------------------------------------------------------------------
// The following macro controls whether the encoder has to call the IAllocator::Free method
// Don't call IAllocator::Free for mscorjit64.dll
//-----------------------------------------------------------------------------
//#define MUST_CALL_IALLOCATOR_FREE



struct GcSlotDesc
{
    union
    {
        UINT32 RegisterNumber;
        GcStackSlot Stack;
    } Slot;
    GcSlotFlags Flags;

    BOOL IsRegister()
    {
        return (Flags & GC_SLOT_IS_REGISTER);
    }
    BOOL IsInterior()
    {
        return (Flags & GC_SLOT_INTERIOR);
    }
    BOOL IsPinned()
    {
        return (Flags & GC_SLOT_PINNED);
    }    
    BOOL IsUntracked()
    {
        return (Flags & GC_SLOT_UNTRACKED);
    }    
    BOOL IsDeleted()
    {
        return (Flags & GC_SLOT_IS_DELETED);
    }
    void MarkDeleted()
    {
        Flags = (GcSlotFlags) (Flags | GC_SLOT_IS_DELETED);
    }
};

#define LOG_GCSLOTDESC_FMT "%s%c%d%s%s%s"
#define LOG_GCSLOTDESC_ARGS(pDesc) (pDesc)->IsRegister() ? "register"                                          \
                                        : GcStackSlotBaseNames[(pDesc)->Slot.Stack.Base],                      \
                                   (pDesc)->IsRegister() ? ' ' : (pDesc)->Slot.Stack.SpOffset < 0 ? '-' : '+', \
                                   (pDesc)->IsRegister() ? (pDesc)->Slot.RegisterNumber                        \
                                        : ((pDesc)->Slot.Stack.SpOffset),                                      \
                                   (pDesc)->IsPinned() ? " pinned" : "",                                       \
                                   (pDesc)->IsInterior() ? " interior" : "",                                   \
                                   (pDesc)->IsUntracked() ? " untracked" : ""

#define LOG_REGTRANSITION_FMT "register %u%s%s"
#define LOG_REGTRANSITION_ARGS(RegisterNumber, Flags)           \
                RegisterNumber,                                 \
                (Flags & GC_SLOT_PINNED)   ? " pinned"   : "",  \
                (Flags & GC_SLOT_INTERIOR) ? " interior" : ""

#define LOG_STACKTRANSITION_FMT "%s%c%d%s%s%s"
#define LOG_STACKTRANSITION_ARGS(BaseRegister, StackOffset, Flags)  \
        GcStackSlotBaseNames[BaseRegister],                         \
        ((StackOffset) < 0) ? '-' : '+',                            \
        ((StackOffset) >= 0) ? (StackOffset)                        \
                             : -(StackOffset),                      \
                (Flags & GC_SLOT_PINNED)   ? " pinned"   : "",      \
                (Flags & GC_SLOT_INTERIOR) ? " interior" : "",      \
                (Flags & GC_SLOT_UNTRACKED) ? " untracked" : ""


class BitArray
{
    friend class BitArrayIterator;
public:
    BitArray(IAllocator* pJitAllocator, size_t size_tCount)
    {
        m_pData = (size_t*)pJitAllocator->Alloc(size_tCount*sizeof(size_t));
        m_pEndData = m_pData + size_tCount;
#ifdef MUST_CALL_IALLOCATOR_FREE
        m_pJitAllocator = pJitAllocator;
#endif
    }

    inline size_t* DataPtr()
    {
        return m_pData;
    }
    
    inline void SetBit( size_t pos )
    {
        size_t element = pos / BITS_PER_SIZE_T;
        int bpos = (int)(pos % BITS_PER_SIZE_T);
        m_pData[element] |= ((size_t)1 << bpos);
    }

    inline void ClearBit( size_t pos )
    {
        size_t element = pos / BITS_PER_SIZE_T;
        int bpos = (int)(pos % BITS_PER_SIZE_T);
        m_pData[element] &= ~((size_t)1 << bpos);
    }

    inline void SetAll()
    {
        size_t* ptr = m_pData;
        while(ptr < m_pEndData)
            *(ptr++) = (size_t)(SSIZE_T)(-1);
    }
    
    inline void ClearAll()
    {
        size_t* ptr = m_pData;
        while(ptr < m_pEndData)
            *(ptr++) = (size_t) 0;
    }
    
    inline void WriteBit( size_t pos, BOOL val)
    {
        if(val)
            SetBit(pos);
        else
            ClearBit(pos);
    }
    
    inline size_t ReadBit( size_t pos ) const
    {
        size_t element = pos / BITS_PER_SIZE_T;
        int bpos = (int)(pos % BITS_PER_SIZE_T);
        return (m_pData[element] & ((size_t)1 << bpos));
    }

    inline bool operator==(const BitArray &other) const
    {
        _ASSERTE(other.m_pEndData - other.m_pData == m_pEndData - m_pData);
        size_t* dest = m_pData;
        size_t* src = other.m_pData;
        return 0 == memcmp(dest, src, (m_pEndData - m_pData) * sizeof(*m_pData));
    }

    inline int GetHashCode() const
    {
        const int* src = (const int*)m_pData;
        int result = *src++;
        while(src < (const int*)m_pEndData)
            result = _rotr(result, 5) ^ *src++;
            
        return result;
    }

    inline BitArray& operator=(const BitArray &other)
    {
        _ASSERTE(other.m_pEndData - other.m_pData == m_pEndData - m_pData);
        size_t* dest = m_pData;
        size_t* src = other.m_pData;
        while(dest < m_pEndData)
            *(dest++) = *(src++);
            
        return *this;
    }
    
    inline BitArray& operator|=(const BitArray &other)
    {
        _ASSERTE(other.m_pEndData - other.m_pData == m_pEndData - m_pData);
        size_t* dest = m_pData;
        size_t* src = other.m_pData;
        while(dest < m_pEndData)
            *(dest++) |= *(src++);
            
        return *this;
    }
    
#ifdef MUST_CALL_IALLOCATOR_FREE
    ~BitArray()
    {
        m_pAllocator->Free( m_pData );
    }    
#endif

private:
    size_t * m_pData;
    size_t * m_pEndData;
#ifdef MUST_CALL_IALLOCATOR_FREE
    IAllocator* m_pJitAllocator;
#endif
};


class BitArrayIterator
{
public:
    BitArrayIterator(BitArray* bitArray)
    {
        m_pCurData = (unsigned *)bitArray->m_pData;
        m_pEndData = (unsigned *)bitArray->m_pEndData;
        m_curBits = *m_pCurData;
        m_curBit = 0;
        m_curBase = 0;
        GetNext();
    }
    void operator++(int dummy) //int dummy is c++ for "this is postfix ++"
    {
        GetNext();
    }

    void operator++() // prefix ++
    {
        GetNext();
    }
    void GetNext()
    {
        m_curBits -= m_curBit;
        while (m_curBits == 0)
        {
            m_pCurData++;
            m_curBase += 32;
            if (m_pCurData == m_pEndData)
                break;
            m_curBits = *m_pCurData;
        }
        m_curBit = (unsigned)((int)m_curBits & -(int)m_curBits);
    }
    unsigned operator*()
    {
        assert(!end() && (m_curBit != 0));
        unsigned bitPosition = BitPosition(m_curBit);
        return bitPosition + m_curBase;
    }
    bool end()
    {
        return (m_pCurData == m_pEndData);
    }
private:
    unsigned*   m_pCurData;
    unsigned*   m_pEndData;
    unsigned    m_curBits;
    unsigned    m_curBit;
    unsigned    m_curBase;
};

class BitStreamWriter
{
public:
    BitStreamWriter( IAllocator* pAllocator );

    // bit 0 is the least significative bit
    void Write( size_t data, UINT32 count );
    void Write( BitArray& a, UINT32 count );

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
    static 
    int SizeofVarLengthUnsigned( size_t n, UINT32 base )
    {
        // If a value gets so big we are probably doing something wrong
        _ASSERTE(((INT32)(UINT32)n) >= 0);
        _ASSERTE((base > 0) && (base < BITS_PER_SIZE_T));
        size_t numEncodings = 1 << base;
        int bitsUsed;
        for(bitsUsed = base+1; ; bitsUsed += base+1)
        {
            if( n < numEncodings )
            {
                return bitsUsed;
            }
            else
            {
                n >>= base;
            }
        }
        return bitsUsed;
    }

    //--------------------------------------------------------
    // Encode variable length numbers
    // Uses base+1 bits at minimum
    // Bits 0..(base-1) represent the encoded quantity
    // If it doesn't fit, set bit #base to 1 and use base+1 more bits
    //--------------------------------------------------------
    int EncodeVarLengthUnsigned( size_t n, UINT32 base )
    {
        // If a value gets so big we are probably doing something wrong
        _ASSERTE(((INT32)(UINT32)n) >= 0);
        _ASSERTE((base > 0) && (base < BITS_PER_SIZE_T));
        size_t numEncodings = 1 << base;
        int bitsUsed;
        for(bitsUsed = base+1; ; bitsUsed += base+1)
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
    int EncodeVarLengthSigned( SSIZE_T n, UINT32 base )
    {
        _ASSERTE((base > 0) && (base < BITS_PER_SIZE_T));
        size_t numEncodings = 1 << base;
        for(int bitsUsed = base+1; ; bitsUsed += base+1)
        {
            size_t currentChunk = ((size_t) n) & (numEncodings-1);
            size_t topmostBit = currentChunk & (numEncodings >> 1);
            n >>= base; // signed arithmetic shift
            if((topmostBit && (n == (SSIZE_T)-1)) || (!topmostBit && (n == 0)))
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

    IAllocator* m_pAllocator;
    size_t m_BitCount;
    UINT32 m_FreeBitsInCurrentSlot;
    SList<MemoryBlockDesc> m_MemoryBlocks;
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

class GcInfoEncoder
{
public:
    GcInfoEncoder(
            ICorJitInfo*                pCorJitInfo,
            CORINFO_METHOD_INFO*        pMethodInfo,
            IAllocator*                 pJitAllocator
            );

    struct LifetimeTransition
    {
        UINT32 CodeOffset;
        GcSlotId SlotId;
        BYTE BecomesLive;
        BYTE IsDeleted;
    };


#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    void DefineCallSites(UINT32* pCallSites, BYTE* pCallSiteSizes, UINT32 numCallSites)
    {
        m_pCallSites = pCallSites;
        m_pCallSiteSizes = pCallSiteSizes;
        m_NumCallSites = numCallSites;
#ifdef _DEBUG
        for(UINT32 i=0; i<numCallSites; i++)
        {
            _ASSERTE(pCallSiteSizes[i] > 0);
            _ASSERTE(DENORMALIZE_CODE_OFFSET(NORMALIZE_CODE_OFFSET(pCallSites[i])) == pCallSites[i]);
            if(i > 0)
            {
                UINT32 prevEnd = pCallSites[i-1] + pCallSiteSizes[i-1];
                UINT32 curStart = pCallSites[i];
                _ASSERTE(curStart >= prevEnd);
            }
        }
#endif
    }
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
    // Miscellaneous method information
    //------------------------------------------------------------------------

    void SetSecurityObjectStackSlot( INT32 spOffset );
    void SetPrologSize( UINT32 prologSize );
    void SetGSCookieStackSlot( INT32 spOffsetGSCookie, UINT32 validRangeStart, UINT32 validRangeEnd );
    void SetPSPSymStackSlot( INT32 spOffsetPSPSym );
    void SetGenericsInstContextStackSlot( INT32 spOffsetGenericsContext, GENERIC_CONTEXTPARAM_TYPE type);
    void SetIsVarArg();
    void SetCodeLength( UINT32 length );

    // Optional in the general case. Required if the method uses GC_FRAMEREG_REL stack slots
    void SetStackBaseRegister( UINT32 registerNumber );

    // Number of slots preserved during EnC remap
    void SetSizeOfEditAndContinuePreservedArea( UINT32 size );

    // Used to only report a frame once for the leaf function/funclet
    // instead of once for each live function/funclet on the stack.
    // Called only by RyuJIT (not JIT64)
    void SetWantsReportOnlyLeaf();

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

    friend int __cdecl CompareLifetimeTransitionsByOffsetThenSlot(const void*, const void*);
    friend int CompareLifetimeTransitionsByChunk(const void*, const void*);


    struct InterruptibleRange
    {
        UINT32 NormStartOffset;
        UINT32 NormStopOffset;
    };

    class InterruptibleRangeAllocator
    {
    public:

        static void *Alloc (void *context, SIZE_T cb)
        {
            GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_InterruptibleRanges);
            return pGcInfoEncoder->m_pAllocator->Alloc(cb);
        }

        static void Free (void *context, void *pv)
        {
        #ifdef MUST_CALL_IALLOCATOR_FREE
            GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_InterruptibleRanges);
            pGcInfoEncoder->m_pAllocator->Free(pv);
        #endif
        }
    };

    class LifetimeTransitionAllocator
    {
    public:

        static void *Alloc (void *context, SIZE_T cb)
        {
            GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_LifetimeTransitions);
            return pGcInfoEncoder->m_pAllocator->Alloc(cb);
        }

        static void Free (void *context, void *pv)
        {
        #ifdef MUST_CALL_IALLOCATOR_FREE
            GcInfoEncoder *pGcInfoEncoder = CONTAINING_RECORD(context, GcInfoEncoder, m_LifetimeTransitions);
            pGcInfoEncoder->m_pAllocator->Free(pv);
        #endif
        }
    };

    ICorJitInfo*                m_pCorJitInfo;
    CORINFO_METHOD_INFO*        m_pMethodInfo;
    IAllocator*                 m_pAllocator;

#ifdef _DEBUG
    const char *m_MethodName, *m_ModuleName;
#endif

    BitStreamWriter     m_Info1;    // Used for everything except for chunk encodings
    BitStreamWriter     m_Info2;    // Used for chunk encodings

    StructArrayList<InterruptibleRange, 8, 2, InterruptibleRangeAllocator> m_InterruptibleRanges;
    StructArrayList<LifetimeTransition, 64, 2, LifetimeTransitionAllocator> m_LifetimeTransitions;

    bool   m_IsVarArg;
    bool   m_WantsReportOnlyLeaf;
    INT32  m_SecurityObjectStackSlot;
    INT32  m_GSCookieStackSlot;
    UINT32 m_GSCookieValidRangeStart;
    UINT32 m_GSCookieValidRangeEnd;
    INT32  m_PSPSymStackSlot;
    INT32  m_GenericsInstContextStackSlot;
    GENERIC_CONTEXTPARAM_TYPE m_contextParamType;
    UINT32 m_CodeLength;
    UINT32 m_StackBaseRegister;
    UINT32 m_SizeOfEditAndContinuePreservedArea;
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

    void WriteBitArray(BitArray& a, UINT32 count);

    inline void WriteSlotStateVector(BitStreamWriter &writer, const BitArray& vector)
    {
        for(UINT32 i = 0; i < m_NumSlots && !m_SlotTable[i].IsUntracked(); i++)
        {
            if(!m_SlotTable[i].IsDeleted())
                writer.Write(vector.ReadBit(i) ? 1 : 0, 1);
            else
                _ASSERTE(vector.ReadBit(i) == 0);
        }
    }

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

#ifdef VERIFY_GCINFO
    DbgGcInfo::GcInfoEncoder m_DbgEncoder;
#endif    

#ifdef MEASURE_GCINFO
    GcInfoSize m_CurrentMethodSize;
#endif
};

class LiveStateFuncs
{
public:
    static int GetHashCode(const BitArray * key)
    {
        return key->GetHashCode();
    }

    static bool Equals(const BitArray * k1, const BitArray * k2)
    {
        return *k1 == *k2;
    }
};


#endif // !__GCINFOENCODER_H__

