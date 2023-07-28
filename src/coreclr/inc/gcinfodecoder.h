// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************
 *
 * GC Information Decoding API
 *
 *****************************************************************/

// ******************************************************************************
// WARNING!!!: These values are used by SOS in the diagnostics repo. Values should
// added or removed in a backwards and forwards compatible way.
// See: https://github.com/dotnet/diagnostics/blob/main/src/shared/inc/gcinfodecoder.h
// ******************************************************************************

#ifndef _GC_INFO_DECODER_
#define _GC_INFO_DECODER_

#define _max(a, b) (((a) > (b)) ? (a) : (b))
#define _min(a, b) (((a) < (b)) ? (a) : (b))

#if !defined(TARGET_X86)
#define USE_GC_INFO_DECODER
#endif

#if !defined(GCINFODECODER_NO_EE)

#include "eetwain.h"

#else

#ifdef FEATURE_NATIVEAOT

typedef ArrayDPTR(const uint8_t) PTR_CBYTE;

#define LIMITED_METHOD_CONTRACT
#define SUPPORTS_DAC

#define LOG(x)
#define LOG_PIPTR(pObjRef, gcFlags, hCallBack)
#define DAC_ARG(x)

#define VALIDATE_ROOT(isInterior, hCallBack, pObjRef)

#define UINT32 uint32_t
#define INT32 int32_t
#define UINT16 uint16_t
#define UINT uint32_t
#define SIZE_T uintptr_t
#define SSIZE_T intptr_t
#define LPVOID void*

typedef void * OBJECTREF;

#define GET_CALLER_SP(pREGDISPLAY) ((TADDR)0)

struct GCInfoToken
{
    PTR_VOID Info;
    UINT32 Version;

    GCInfoToken(PTR_VOID info)
    {
        Info = info;
        Version = 2;
    }
};

#else // FEATURE_NATIVEAOT

// Stuff from cgencpu.h:

#ifndef __cgencpu_h__

inline void SetIP(T_CONTEXT* context, PCODE rip)
{
    _ASSERTE(!"don't call this");
}

inline TADDR GetSP(T_CONTEXT* context)
{
#ifdef TARGET_AMD64
    return (TADDR)context->Rsp;
#elif defined(TARGET_ARM)
    return (TADDR)context->Sp;
#elif defined(TARGET_ARM64)
    return (TADDR)context->Sp;
#elif defined(TARGET_LOONGARCH64)
    return (TADDR)context->Sp;
#else
    _ASSERTE(!"nyi for platform");
#endif
}

inline PCODE GetIP(T_CONTEXT* context)
{
#ifdef TARGET_AMD64
    return (PCODE) context->Rip;
#elif defined(TARGET_ARM)
    return (PCODE)context->Pc;
#elif defined(TARGET_ARM64)
    return (PCODE)context->Pc;
#elif defined(TARGET_LOONGARCH64)
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

// Stuff from eetwain.h:

#ifndef _EETWAIN_H

typedef void (*GCEnumCallback)(
    void *          hCallback,      // callback data
    OBJECTREF*      pObject,        // address of object-reference we are reporting
    uint32_t        flags           // is this a pinned and/or interior pointer
);

#endif // !_EETWAIN_H

#include "regdisp.h"

#endif // FEATURE_NATIVEAOT

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

#endif // GCINFODECODER_NO_EE


#include "gcinfotypes.h"

#ifdef _DEBUG
    #define MAX_PREDECODED_SLOTS  4
#else
    #define MAX_PREDECODED_SLOTS 64
#endif



enum GcInfoDecoderFlags
{
    DECODE_EVERYTHING            = 0x0,
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
    DECODE_REVERSE_PINVOKE_VAR   = 0x1000,
    DECODE_RETURN_KIND           = 0x2000,
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    DECODE_HAS_TAILCALLS         = 0x4000,
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64
};

enum GcInfoHeaderFlags
{
    GC_INFO_IS_VARARG                   = 0x1,
    // unused                           = 0x2, // was GC_INFO_HAS_SECURITY_OBJECT
    GC_INFO_HAS_GS_COOKIE               = 0x4,
    GC_INFO_HAS_PSP_SYM                 = 0x8,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK   = 0x30,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE   = 0x00,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MT     = 0x10,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_MD     = 0x20,
    GC_INFO_HAS_GENERICS_INST_CONTEXT_THIS   = 0x30,
    GC_INFO_HAS_STACK_BASE_REGISTER     = 0x40,
#ifdef TARGET_AMD64
    GC_INFO_WANTS_REPORT_ONLY_LEAF      = 0x80,
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    GC_INFO_HAS_TAILCALLS               = 0x80,
#endif // TARGET_AMD64
    GC_INFO_HAS_EDIT_AND_CONTINUE_INFO = 0x100,
    GC_INFO_REVERSE_PINVOKE_FRAME = 0x200,

    GC_INFO_FLAGS_BIT_SIZE_VERSION_1    = 9,
    GC_INFO_FLAGS_BIT_SIZE              = 10,
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
        m_RelPos = m_InitialRelPos = (int)((size_t)dac_cast<TADDR>(pBuffer) % sizeof(size_t)) * 8/*BITS_PER_BYTE*/;
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
        size_t numEncodings = size_t{ 1 } << base;
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
        size_t numEncodings = size_t{ 1 } << base;
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

#ifdef USE_GC_INFO_DECODER
class GcInfoDecoder
{
public:

    // If you are not interested in interruptibility or gc lifetime information, pass 0 as instructionOffset
    GcInfoDecoder(
            GCInfoToken gcInfoToken,
            GcInfoDecoderFlags flags = DECODE_EVERYTHING,
            UINT32 instructionOffset = 0
            );

    //------------------------------------------------------------------------
    // Interruptibility
    //------------------------------------------------------------------------

    bool IsInterruptible();

#ifdef PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
    // This is used for gccoverage
    bool IsSafePoint(UINT32 codeOffset);

    typedef void EnumerateSafePointsCallback (UINT32 offset, void * hCallback);
    void EnumerateSafePoints(EnumerateSafePointsCallback * pCallback, void * hCallback);

#endif
    // Returns true to stop enumerating.
    typedef bool EnumerateInterruptibleRangesCallback (UINT32 startOffset, UINT32 stopOffset, void * hCallback);

    void EnumerateInterruptibleRanges (
                EnumerateInterruptibleRangesCallback *pCallback,
                void *                                hCallback);

    //------------------------------------------------------------------------
    // GC lifetime information
    //------------------------------------------------------------------------

    bool EnumerateLiveSlots(
                PREGDISPLAY         pRD,
                bool                reportScratchSlots,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                void *              hCallBack
                );

    // Public for the gc info dumper
    void EnumerateUntrackedSlots(
                PREGDISPLAY         pRD,
                unsigned            flags,
                GCEnumCallback      pCallBack,
                void *              hCallBack
                );

    //------------------------------------------------------------------------
    // Miscellaneous method information
    //------------------------------------------------------------------------

    INT32   GetGSCookieStackSlot();
    UINT32  GetGSCookieValidRangeStart();
    UINT32  GetGSCookieValidRangeEnd();
    UINT32  GetPrologSize();
    INT32   GetPSPSymStackSlot();
    INT32   GetGenericsInstContextStackSlot();
    INT32   GetReversePInvokeFrameStackSlot();
    bool    HasMethodDescGenericsInstContext();
    bool    HasMethodTableGenericsInstContext();
    bool    GetIsVarArg();
    bool    WantsReportOnlyLeaf();
#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool    HasTailCalls();
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || defined(TARGET_RISCV64)
    ReturnKind GetReturnKind();
    UINT32  GetCodeLength();
    UINT32  GetStackBaseRegister();
    UINT32  GetSizeOfEditAndContinuePreservedArea();
#ifdef TARGET_ARM64
    UINT32  GetSizeOfEditAndContinueFixedStackFrame();
#endif
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
#ifdef TARGET_AMD64
    bool    m_WantsReportOnlyLeaf;
#elif defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool    m_HasTailCalls;
#endif // TARGET_AMD64
    INT32   m_GSCookieStackSlot;
    INT32   m_ReversePInvokeFrameStackSlot;
    UINT32  m_ValidRangeStart;
    UINT32  m_ValidRangeEnd;
    INT32   m_PSPSymStackSlot;
    INT32   m_GenericsInstContextStackSlot;
    UINT32  m_CodeLength;
    UINT32  m_StackBaseRegister;
    UINT32  m_SizeOfEditAndContinuePreservedArea;
#ifdef TARGET_ARM64
    UINT32  m_SizeOfEditAndContinueFixedStackFrame;
#endif
    ReturnKind m_ReturnKind;
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
    UINT32 m_Version;

    static bool SetIsInterruptibleCB (UINT32 startOffset, UINT32 stopOffset, void * hCallback);

    OBJECTREF* GetRegisterSlot(
                        int             regNum,
                        PREGDISPLAY     pRD
                        );

#ifdef TARGET_UNIX
    OBJECTREF* GetCapturedRegister(
                        int             regNum,
                        PREGDISPLAY     pRD
                        );
#endif // TARGET_UNIX

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
                void *              hCallBack
                );

    void ReportRegisterToGC(
                                int             regNum,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack
                                );

    void ReportStackSlotToGC(
                                INT32           spOffset,
                                GcStackSlotBase spBase,
                                unsigned        gcFlags,
                                PREGDISPLAY     pRD,
                                unsigned        flags,
                                GCEnumCallback  pCallBack,
                                void *          hCallBack
                                );


    inline void ReportSlotToGC(
                    GcSlotDecoder&      slotDecoder,
                    UINT32              slotIndex,
                    PREGDISPLAY         pRD,
                    bool                reportScratchSlots,
                    unsigned            inputFlags,
                    GCEnumCallback      pCallBack,
                    void *              hCallBack
                    )
    {
        _ASSERTE(slotIndex < slotDecoder.GetNumSlots());
        const GcSlotDesc* pSlot = slotDecoder.GetSlotDesc(slotIndex);

        if(slotIndex < slotDecoder.GetNumRegisters())
        {
            UINT32 regNum = pSlot->Slot.RegisterNumber;
            if( reportScratchSlots || !IsScratchRegister( regNum, pRD ) )
            {
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
#endif // USE_GC_INFO_DECODER


#endif // _GC_INFO_DECODER_

