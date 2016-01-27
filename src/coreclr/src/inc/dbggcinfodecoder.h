// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*****************************************************************
 *
 * GC Information Decoding API
 *
 * This is an older well-tested implementation 
 *      now used to verify the real encoding
 * Define VERIFY_GCINFO to enable the verification
 *
 *****************************************************************/

#ifdef VERIFY_GCINFO

#ifndef _DBG_GC_INFO_DECODER_
#define _DBG_GC_INFO_DECODER_

#include "daccess.h"

#ifndef GCINFODECODER_NO_EE

#include "eetwain.h"

#else // GCINFODECODER_NO_EE

#if !defined(_NTAMD64_)
#include "clrnt.h"
#endif

// Misc. VM types:

class Object;
typedef Object *OBJECTREF;
typedef SIZE_T TADDR;

// Stuff from gc.h:

#ifndef __GC_H

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2

#endif // !__GC_H


// Stuff from check.h:

#ifndef UNREACHABLE
#define UNREACHABLE() __assume(0)
#endif

// Stuff from eetwain.h:

#ifndef _EETWAIN_H

typedef void (*GCEnumCallback)(
    LPVOID          hCallback,      // callback data
    OBJECTREF*      pObject,        // address of obect-reference we are reporting
    uint32_t        flags           // is this a pinned and/or interior pointer
);


#if !defined(_TARGET_X86_)
#define USE_GC_INFO_DECODER
#endif

#include "regdisp.h"

#endif // !_EETWAIN_H

#endif // GCINFODECODER_NO_EE

#include "gcinfotypes.h"


namespace DbgGcInfo {

struct GcSlotDesc
{
    union
    {
        UINT32 RegisterNumber;
        GcStackSlot Stack;
    } Slot;
    GcSlotFlags Flags;
};

class BitStreamReader
{
public:
    BitStreamReader( const BYTE* pBuffer )
        {
            _ASSERTE( pBuffer != NULL );
            m_pBuffer = (PTR_BYTE)(TADDR)pBuffer;
            m_BitsRead = 0;
        }

    //
    // bit 0 is the least significative bit
    // count can be negative so that bits are written in most-significative to least-significative order
    //
    size_t Read( int numBits )
    {
        size_t result = 0;
        int curBitsRead = 0;

        while( curBitsRead < numBits )
        {
            int currByte = m_BitsRead /8;
            int currBitInCurrentByte = m_BitsRead % 8;
            int bitsLeftInCurrentByte = 8 - currBitInCurrentByte;
            _ASSERTE( bitsLeftInCurrentByte > 0 );

            int bitsToReadInCurrentByte = min( numBits - curBitsRead, bitsLeftInCurrentByte );

            size_t data = m_pBuffer[ currByte ];
            data >>= currBitInCurrentByte;
            data &= (1<<bitsToReadInCurrentByte) -1;

            data <<= curBitsRead;
            result |= data;

            curBitsRead +=  bitsToReadInCurrentByte;
            m_BitsRead += bitsToReadInCurrentByte;
        }

        return result;
    }

    // Returns the number of bits read so far
    size_t GetCurrentPos()
    {
        return m_BitsRead;
    }

    void SetCurrentPos( size_t pos )
    {
        m_BitsRead = pos;
    }

    // Can use negative values
    void Skip( SSIZE_T numBitsToSkip )
    {
        m_BitsRead += numBitsToSkip;
        _ASSERTE( m_BitsRead >= 0 );
    }

    //--------------------------------------------------------------------------
    // Decode variable length numbers
    // See the corresponding methods on BitStreamWriter for more information on the format
    //--------------------------------------------------------------------------

    inline size_t DecodeVarLengthUnsigned( int base )
    {
        _ASSERTE((base > 0) && (base < (int)sizeof(size_t)*8));
        size_t numEncodings = 1 << base;
        size_t result = 0;
        for(int shift=0; ; shift+=base)
        {
            _ASSERTE(shift+base <= (int)sizeof(size_t)*8);
            
            size_t currentChunk = Read(base+1);
            result |= (currentChunk & (numEncodings-1)) << shift;
            if(!(currentChunk & numEncodings))
            {
                // Extension bit is not set, we're done.
                return result;
            }
        }
    }

    inline SSIZE_T DecodeVarLengthSigned( int base )
    {
        _ASSERTE((base > 0) && (base < (int)sizeof(SSIZE_T)*8));
        size_t numEncodings = 1 << base;
        SSIZE_T result = 0;
        for(int shift=0; ; shift+=base)
        {
            _ASSERTE(shift+base <= (int)sizeof(SSIZE_T)*8);
            
            size_t currentChunk = Read(base+1);
            result |= (currentChunk & (numEncodings-1)) << shift;
            if(!(currentChunk & numEncodings))
            {
                // Extension bit is not set, sign-extend and we're done.
                int sbits = sizeof(SSIZE_T)*8 - (shift+base);
                result <<= sbits;
                result >>= sbits;   // This provides the sign extension
                return result;
            }
        }
    }

private:
        PTR_BYTE m_pBuffer;
        size_t m_BitsRead;
};


class GcInfoDecoder
{
public:

    // If you are not insterested in interruptibility or gc lifetime information, pass 0 as instructionOffset
    GcInfoDecoder(
            const BYTE* gcInfoAddr,
            GcInfoDecoderFlags flags,
            UINT32 instructionOffset = 0
            );


    //------------------------------------------------------------------------
    // Interruptibility
    //------------------------------------------------------------------------

    bool IsInterruptible();

    // Returns true to stop enumerating.
    typedef bool EnumerateInterruptibleRangesCallback (UINT32 startOffset, UINT32 stopOffset, LPVOID hCallback);

    void EnumerateInterruptibleRanges (
                EnumerateInterruptibleRangesCallback *pCallback,
                LPVOID                                hCallback);

    //------------------------------------------------------------------------
    // GC lifetime information
    //------------------------------------------------------------------------

    bool EnumerateLiveSlots(
                PREGDISPLAY         pRD,
                bool                reportScratchSlots,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                LPVOID              hCallBack
                );

    void VerifyLiveRegister(
                            UINT32 regNum,
                            GcSlotFlags flags
                            );

                            
    void VerifyLiveStackSlot(
                            int spOffset,
                            GcStackSlotBase spBase,
                            GcSlotFlags flags
                            );
                            
    void DoFinalVerification();

    //------------------------------------------------------------------------
    // Miscellaneous method information
    //------------------------------------------------------------------------

    INT32   GetSecurityObjectStackSlot();
    INT32   GetPSPSymStackSlot();
    INT32   GetGenericsInstContextStackSlot();
    bool    GetIsVarArg();
    UINT32  GetCodeLength();
    UINT32  GetStackBaseRegister();
    UINT32  GetSizeOfEditAndContinuePreservedArea();

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32  GetSizeOfStackParameterArea();
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

private:
    BitStreamReader m_Reader;
    UINT32  m_InstructionOffset;

    // Pre-decoded information
    bool    m_IsInterruptible;
    bool    m_IsVarArg;
    INT32   m_SecurityObjectStackSlot;
    INT32   m_PSPSymStackSlot;
    INT32   m_GenericsInstContextStackSlot;
    UINT32  m_CodeLength;
    UINT32  m_StackBaseRegister;
    UINT32  m_SizeOfEditAndContinuePreservedArea;
    UINT32  m_NumInterruptibleRanges;

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32 m_SizeOfStackOutgoingAndScratchArea;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

#ifdef _DEBUG
    GcInfoDecoderFlags m_Flags;
#endif

    GcSlotDesc* m_pLiveRegisters;
    GcSlotDesc* m_pLiveStackSlots;
    int m_NumLiveRegisters;
    int m_NumLiveStackSlots;

    CQuickBytes qbSlots1;
    CQuickBytes qbSlots2;
    
    static bool SetIsInterruptibleCB (UINT32 startOffset, UINT32 stopOffset, LPVOID hCallback);

    OBJECTREF* GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        );

    OBJECTREF* GetStackSlot(
                        INT32           spOffset,
                        GcStackSlotBase spBase,
                        PREGDISPLAY     pRD
                        );

    bool IsScratchRegister(int regNum,  PREGDISPLAY pRD);
    bool IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY pRD);

    void ReportRegisterToGC(
                                int             regNum,
                                BOOL            isInterior,
                                BOOL            isPinned,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack
                                );

    void ReportStackSlotToGC(
                                INT32           spOffset,
                                GcStackSlotBase spBase,
                                BOOL            isInterior,
                                BOOL            isPinned,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack
                                );


};

}

#endif // _DBG_GC_INFO_DECODER_
#endif // VERIFY_GCINFO

