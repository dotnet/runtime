//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*****************************************************************
 *
 * GC Information Decoding API
 *
 *****************************************************************/

#ifndef _GC_INFO_DECODER_
#define _GC_INFO_DECODER_

#include "daccess.h"

#ifndef GCINFODECODER_NO_EE

#include "eetwain.h"

#else // GCINFODECODER_NO_EE

// Stuff from cgencpu.h:

#ifndef __cgencpu_h__

inline void SetIP(T_CONTEXT* context, PCODE rip)
{
    _ASSERTE(!"don't call this");
}

inline TADDR GetSP(T_CONTEXT* context)
{
#ifdef _TARGET_AMD64_
    return (TADDR)context->Rsp;
#elif defined(_TARGET_ARM_)
    return (TADDR)context->Sp;
#elif defined(_TARGET_ARM64_)
    return (TADDR)context->Sp;
#else
    _ASSERTE(!"nyi for platform");
#endif
}

inline PCODE GetIP(T_CONTEXT* context) 
{
#ifdef _TARGET_AMD64_
    return (PCODE) context->Rip;
#elif defined(_TARGET_ARM_)
    return (PCODE)context->Pc;
#elif defined(_TARGET_ARM64_)
    return (PCODE)context->Pc;
#else
    _ASSERTE(!"nyi for platform");
#endif
}

#endif // !__cgencpu_h__

// Misc. VM types:

#ifndef DEFINE_OBJECTREF
#define DEFINE_OBJECTREF
class Object;
typedef Object *OBJECTREF;
#endif
typedef SIZE_T TADDR;

// Stuff from gc.h:

#ifndef __GC_H

#define GC_CALL_INTERIOR            0x1
#define GC_CALL_PINNED              0x2

#endif // !__GC_H

// Stuff from stdmacros.h (can't include because it includes contract.h, which uses #pragma once)

#ifndef _stdmacros_h_

inline BOOL IS_ALIGNED( size_t val, size_t alignment )
{
    // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
    _ASSERTE( 0 == (alignment & (alignment - 1)) ); 
    return 0 == (val & (alignment - 1));
}
inline BOOL IS_ALIGNED( void* val, size_t alignment )
{
    return IS_ALIGNED( (size_t) val, alignment );
}

#define FMT_REG     "r%d "
#define FMT_STK     "sp%s0x%02x "

#define DBG_STK(off)                   \
        (off >= 0) ? "+" : "-",        \
        (off >= 0) ? off : -off

#endif

// Stuff from check.h:

#ifndef UNREACHABLE
#define UNREACHABLE() __assume(0)
#endif

// Stuff from eetwain.h:

#ifndef _EETWAIN_H

typedef void (*GCEnumCallback)(
    LPVOID          hCallback,      // callback data
    OBJECTREF*      pObject,        // address of obect-reference we are reporting
    DWORD           flags           // is this a pinned and/or interior pointer
);

#ifndef _strike_h

enum ICodeManagerFlags
{
    ActiveStackFrame  =  0x0001, // this is the currently active function
    ExecutionAborted  =  0x0002, // execution of this function has been aborted
                                 // (i.e. it will not continue execution at the
                                 // current location)
    ParentOfFuncletStackFrame
                      =  0x0040, // A funclet for this frame was previously reported

    NoReportUntracked
                    =   0x0080, // EnumGCRefs/EnumerateLiveSlots should *not* include
                                // any untracked slots
};

#endif // !_strike_h

#if defined(_WIN64) || defined(_TARGET_ARM_)
#define USE_GC_INFO_DECODER
#endif

#include "regdisp.h"

#endif // !_EETWAIN_H

#endif // GCINFODECODER_NO_EE

#include "gcinfotypes.h"

#ifdef _DEBUG
    #define MAX_PREDECODED_SLOTS  4
#else
    #define MAX_PREDECODED_SLOTS 64
#endif



enum GcInfoDecoderFlags
{
    DECODE_SECURITY_OBJECT       = 0x01,    // stack location of security object
    DECODE_CODE_LENGTH           = 0x02,
    DECODE_VARARG                = 0x04,
    DECODE_INTERRUPTIBILITY      = 0x08,
    DECODE_GC_LIFETIMES          = 0x10,
    DECODE_NO_VALIDATION         = 0x20,
    DECODE_PSP_SYM               = 0x40,
    DECODE_GENERICS_INST_CONTEXT = 0x80,    // stack location of instantiation context for generics 
                                            // (this may be either the 'this' ptr or the instantiation secret param)
    DECODE_GS_COOKIE             = 0x100,   // stack location of the GS cookie
    DECODE_FOR_RANGES_CALLBACK   = 0x200,
    DECODE_PROLOG_LENGTH         = 0x400,   // length of the prolog (used to avoid reporting generics context)
    DECODE_EDIT_AND_CONTINUE     = 0x800,
};

#ifdef VERIFY_GCINFO
#include "dbggcinfodecoder.h"
#endif

enum GcInfoHeaderFlags
{
    GC_INFO_IS_VARARG                   = 0x1,
    GC_INFO_HAS_SECURITY_OBJECT         = 0x2,
    GC_INFO_HAS_GS_COOKIE               = 0x4,
    GC_INFO_HAS_PSP_SYM                 = 0x8,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK   = 0x30,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE   = 0x00,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MT     = 0x10,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MD     = 0x20,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_THIS   = 0x30,
    GC_INFO_HAS_STACK_BASE_REGISTER     = 0x40,
    GC_INFO_WANTS_REPORT_ONLY_LEAF      = 0x80,
    GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS = 0x100,

    GC_INFO_FLAGS_BIT_SIZE              = 9,
};


class BitStreamReader
{
public:
    BitStreamReader()
    {
        SUPPORTS_DAC;
    }

    BitStreamReader( PTR_CBYTE pBuffer )
    {
        SUPPORTS_DAC;

        _ASSERTE( pBuffer != NULL );

        m_pCurrent = m_pBuffer = dac_cast<PTR_size_t>((size_t)dac_cast<TADDR>(pBuffer) & ~((size_t)sizeof(size_t)-1));
        m_RelPos = m_InitialRelPos = (int)((size_t)dac_cast<TADDR>(pBuffer) % sizeof(size_t)) * 8;
    }

    BitStreamReader(const BitStreamReader& other)
    {
        SUPPORTS_DAC;

        m_pBuffer = other.m_pBuffer;
        m_InitialRelPos = other.m_InitialRelPos;
        m_pCurrent = other.m_pCurrent;
        m_RelPos = other.m_RelPos;
    }

    const BitStreamReader& operator=(const BitStreamReader& other)
    {
        SUPPORTS_DAC;

        m_pBuffer = other.m_pBuffer;
        m_InitialRelPos = other.m_InitialRelPos;
        m_pCurrent = other.m_pCurrent;
        m_RelPos = other.m_RelPos;
        return *this;
    }

    // NOTE: This routine is perf-critical
    __forceinline size_t Read( int numBits )
    {
        SUPPORTS_DAC;

        _ASSERTE(numBits > 0 && numBits <= BITS_PER_SIZE_T);

        size_t result = (*m_pCurrent) >> m_RelPos;
        int newRelPos = m_RelPos + numBits;
        if(newRelPos >= BITS_PER_SIZE_T)
        {
            m_pCurrent++;
            newRelPos -= BITS_PER_SIZE_T;
            if(newRelPos > 0)
            {
                size_t extraBits = (*m_pCurrent) << (numBits - newRelPos);
                result ^= extraBits;
            }
        }
        m_RelPos = newRelPos;
        result &= SAFE_SHIFT_LEFT(1, numBits) - 1;
        return result;
    }
                
    // This version reads one bit, returning zero/non-zero (not 0/1)
    // NOTE: This routine is perf-critical
    __forceinline size_t ReadOneFast()
    {
        SUPPORTS_DAC;

        size_t result = (*m_pCurrent) & (((size_t)1) << m_RelPos);
        if(++m_RelPos == BITS_PER_SIZE_T)
        {
            m_pCurrent++;
            m_RelPos = 0;
        }
        return result;
    }
                
        
    __forceinline size_t GetCurrentPos()
    {
        SUPPORTS_DAC;
        return (size_t) ((m_pCurrent - m_pBuffer) * BITS_PER_SIZE_T + m_RelPos - m_InitialRelPos);
    }
    
    __forceinline void SetCurrentPos( size_t pos )
    {
        STATIC_CONTRACT_SUPPORTS_DAC_HOST_ONLY; // note: this will set only the host instance, not the target instance

        size_t adjPos = pos + m_InitialRelPos;
        m_pCurrent = m_pBuffer + adjPos / BITS_PER_SIZE_T;
        m_RelPos = (int)(adjPos % BITS_PER_SIZE_T);
        _ASSERTE(GetCurrentPos() == pos);
    }

    __forceinline void Skip( SSIZE_T numBitsToSkip )
    {
        SUPPORTS_DAC;

        SetCurrentPos(GetCurrentPos() + numBitsToSkip);
    }

    __forceinline void AlignUpToByte()
    {
        if(m_RelPos <= BITS_PER_SIZE_T - 8)
        {
            m_RelPos = (m_RelPos + 7) & ~7;
        }
        else
        {
            m_RelPos = 0;
            m_pCurrent++;
        }
    }

    __forceinline size_t ReadBitAtPos( size_t pos )
    {
        size_t adjPos = pos + m_InitialRelPos;
        size_t* ptr = m_pBuffer + adjPos / BITS_PER_SIZE_T;
        int relPos = (int)(adjPos % BITS_PER_SIZE_T);
        return (*ptr) & (((size_t)1) << relPos);
    }

    
    //--------------------------------------------------------------------------
    // Decode variable length numbers
    // See the corresponding methods on BitStreamWriter for more information on the format
    //--------------------------------------------------------------------------

    inline size_t DecodeVarLengthUnsigned( int base )
    {
        _ASSERTE((base > 0) && (base < (int)BITS_PER_SIZE_T));
        size_t numEncodings = 1 << base;
        size_t result = 0;
        for(int shift=0; ; shift+=base)
        {
            _ASSERTE(shift+base <= (int)BITS_PER_SIZE_T);
            
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
        _ASSERTE((base > 0) && (base < (int)BITS_PER_SIZE_T));
        size_t numEncodings = 1 << base;
        SSIZE_T result = 0;
        for(int shift=0; ; shift+=base)
        {
            _ASSERTE(shift+base <= (int)BITS_PER_SIZE_T);
            
            size_t currentChunk = Read(base+1);
            result |= (currentChunk & (numEncodings-1)) << shift;
            if(!(currentChunk & numEncodings))
            {
                // Extension bit is not set, sign-extend and we're done.
                int sbits = BITS_PER_SIZE_T - (shift+base);
                result <<= sbits;
                result >>= sbits;   // This provides the sign extension
                return result;
            }
        }
    }

private:
    PTR_size_t m_pBuffer;
    int m_InitialRelPos;
    PTR_size_t m_pCurrent;
    int m_RelPos;
};

struct GcSlotDesc
{
    union
    {
        UINT32 RegisterNumber;
        GcStackSlot Stack;
    } Slot;
    GcSlotFlags Flags;
};

class GcSlotDecoder
{
public:
    GcSlotDecoder() 
    {}

    void DecodeSlotTable(BitStreamReader& reader);

    UINT32 GetNumSlots()
    {
        return m_NumSlots;
    }

    UINT32 GetNumUntracked()
    {
        return m_NumUntracked;
    }

    UINT32 GetNumTracked()
    {
        return m_NumSlots - m_NumUntracked;
    }

    UINT32 GetNumRegisters()
    {
        return m_NumRegisters;
    }

    const GcSlotDesc* GetSlotDesc(UINT32 slotIndex);
    
private:    
    GcSlotDesc m_SlotArray[MAX_PREDECODED_SLOTS];
    BitStreamReader m_SlotReader;
    UINT32 m_NumSlots;
    UINT32 m_NumRegisters;
    UINT32 m_NumUntracked;
    
    UINT32 m_NumDecodedSlots;
    GcSlotDesc* m_pLastSlot;
};

class GcInfoDecoder
{
public:

    // If you are not insterested in interruptibility or gc lifetime information, pass 0 as instructionOffset
    GcInfoDecoder(
            PTR_CBYTE gcInfoAddr,
            GcInfoDecoderFlags flags,
            UINT32 instructionOffset = 0
            );


    //------------------------------------------------------------------------
    // Interruptibility
    //------------------------------------------------------------------------

    bool IsInterruptible();

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    // This is used for gccoverage
    bool IsSafePoint(UINT32 codeOffset);

    typedef void EnumerateSafePointsCallback (UINT32 offset, LPVOID hCallback);
    void EnumerateSafePoints(EnumerateSafePointsCallback *pCallback, LPVOID hCallback);

#endif
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

    // Public for the gc info dumper
    void EnumerateUntrackedSlots(
                PREGDISPLAY         pRD,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                LPVOID              hCallBack
                );

    //------------------------------------------------------------------------
    // Miscellaneous method information
    //------------------------------------------------------------------------

    INT32   GetSecurityObjectStackSlot();
    INT32   GetGSCookieStackSlot();
    UINT32  GetGSCookieValidRangeStart();
    UINT32  GetGSCookieValidRangeEnd();
    UINT32  GetPrologSize();
    INT32   GetPSPSymStackSlot();
    INT32   GetGenericsInstContextStackSlot();
    bool    HasMethodDescGenericsInstContext();
    bool    HasMethodTableGenericsInstContext();
    bool    GetIsVarArg();
    bool    WantsReportOnlyLeaf();
    UINT32  GetCodeLength();
    UINT32  GetStackBaseRegister();
    UINT32  GetSizeOfEditAndContinuePreservedArea();
    size_t  GetNumBytesRead();

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32  GetSizeOfStackParameterArea();
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA


private:
    BitStreamReader m_Reader;
    UINT32  m_InstructionOffset;

    // Pre-decoded information
    bool    m_IsInterruptible;
    bool    m_IsVarArg;
    bool    m_GenericSecretParamIsMD;
    bool    m_GenericSecretParamIsMT;
    bool    m_WantsReportOnlyLeaf;
    INT32   m_SecurityObjectStackSlot;
    INT32   m_GSCookieStackSlot;
    UINT32  m_ValidRangeStart;
    UINT32  m_ValidRangeEnd;
    INT32   m_PSPSymStackSlot;
    INT32   m_GenericsInstContextStackSlot;
    UINT32  m_CodeLength;
    UINT32  m_StackBaseRegister;
    UINT32  m_SizeOfEditAndContinuePreservedArea;
#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    UINT32  m_NumSafePoints;
    UINT32  m_SafePointIndex;
    UINT32 FindSafePoint(UINT32 codeOffset);
#endif
    UINT32  m_NumInterruptibleRanges;

#ifdef FIXED_STACK_PARAMETER_SCRATCH_AREA
    UINT32 m_SizeOfStackOutgoingAndScratchArea;
#endif // FIXED_STACK_PARAMETER_SCRATCH_AREA

#ifdef _DEBUG
    GcInfoDecoderFlags m_Flags;
    PTR_CBYTE m_GcInfoAddress;
#endif

#ifdef VERIFY_GCINFO
    DbgGcInfo::GcInfoDecoder m_DbgDecoder;
#endif    

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

#ifdef DACCESS_COMPILE
    int GetStackReg(int spBase);
#endif // DACCESS_COMPILE

    bool IsScratchRegister(int regNum,  PREGDISPLAY pRD);
    bool IsScratchStackSlot(INT32 spOffset, GcStackSlotBase spBase, PREGDISPLAY pRD);

    void ReportUntrackedSlots(
                GcSlotDecoder&      slotDecoder,
                PREGDISPLAY         pRD,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                LPVOID              hCallBack
                );

    void ReportRegisterToGC(
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack
                                );

    void ReportStackSlotToGC(
                                INT32           spOffset,
                                GcStackSlotBase spBase,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                LPVOID          hCallBack
                                );


    inline void ReportSlotToGC(
                    GcSlotDecoder&      slotDecoder,
                    UINT32              slotIndex,
                    PREGDISPLAY         pRD,
                    bool                reportScratchSlots,
                    unsigned            inputFlags,
                    GCEnumCallback      pCallBack,
                    LPVOID              hCallBack
                    )
    {
        _ASSERTE(slotIndex < slotDecoder.GetNumSlots());
        const GcSlotDesc* pSlot = slotDecoder.GetSlotDesc(slotIndex);

        if(slotIndex < slotDecoder.GetNumRegisters())
        {
            UINT32 regNum = pSlot->Slot.RegisterNumber;
            if( reportScratchSlots || !IsScratchRegister( regNum, pRD ) )
            {
#ifdef VERIFY_GCINFO
                m_DbgDecoder.VerifyLiveRegister(
                            regNum,
                            pSlot->Flags
                            );
#endif

                ReportRegisterToGC(
                            regNum,
                            pSlot->Flags,
                            pRD,
                            inputFlags,
                            pCallBack,
                            hCallBack
                            );
            }
            else
            {
                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch register " FMT_REG " not reported\n", regNum));
            }
        }
        else
        {
            INT32 spOffset = pSlot->Slot.Stack.SpOffset;
            GcStackSlotBase spBase = pSlot->Slot.Stack.Base;
            if( reportScratchSlots || !IsScratchStackSlot(spOffset, spBase, pRD) )
            {
#ifdef VERIFY_GCINFO
                m_DbgDecoder.VerifyLiveStackSlot(
                            spOffset,
                            spBase,
                            pSlot->Flags
                            );
#endif

                ReportStackSlotToGC(
                            spOffset,
                            spBase,
                            pSlot->Flags,
                            pRD,
                            inputFlags,
                            pCallBack,
                            hCallBack
                            );
            }
            else
            {
                LOG((LF_GCROOTS, LL_INFO1000, "\"Live\" scratch stack slot " FMT_STK  " not reported\n", DBG_STK(spOffset)));
            }
        }
    }
};


#endif // _GC_INFO_DECODER_

