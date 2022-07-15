// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


#ifndef TARGET_ARM
#error Should only include "cGenCpu.h" for ARM builds
#endif

#ifndef __cgencpu_h__
#define __cgencpu_h__

#include "utilcode.h"

// preferred alignment for data
#define DATA_ALIGNMENT 4

#define DISPATCH_STUB_FIRST_WORD 0xf8d0
#define DISPATCH_STUB_THIRD_WORD 0xb420
#define RESOLVE_STUB_FIRST_WORD 0xf8d0
#define RESOLVE_STUB_THIRD_WORD 0xb460
#define LOOKUP_STUB_FIRST_WORD 0xf8df

class MethodDesc;
class FramedMethodFrame;
class Module;
struct DeclActionInfo;
class ComCallMethodDesc;
class BaseDomain;
class ZapNode;
struct ArgLocDesc;

extern PCODE GetPreStubEntryPoint();

#ifndef TARGET_UNIX
#define USE_REDIRECT_FOR_GCSTRESS
#endif // TARGET_UNIX

// CPU-dependent functions
Stub * GenerateInitPInvokeFrameHelper();

EXTERN_C void checkStack(void);

#define THUMB_CODE      1

#define GetEEFuncEntryPoint(pfn) (GFN_TADDR(pfn) | THUMB_CODE)

//**********************************************************************

#define COMMETHOD_PREPAD                        12   // # extra bytes to allocate in addition to sizeof(ComCallMethodDesc)
#ifdef FEATURE_COMINTEROP
#define COMMETHOD_CALL_PRESTUB_SIZE             12
#define COMMETHOD_CALL_PRESTUB_ADDRESS_OFFSET   8   // the offset of the call target address inside the prestub
#endif // FEATURE_COMINTEROP

#define STACK_ALIGN_SIZE                        4

#define JUMP_ALLOCATE_SIZE                      8   // # bytes to allocate for a jump instruction
#define BACK_TO_BACK_JUMP_ALLOCATE_SIZE         8   // # bytes to allocate for a back to back jump instruction

#define HAS_COMPACT_ENTRYPOINTS                 1

#define HAS_NDIRECT_IMPORT_PRECODE              1

#define USE_INDIRECT_CODEHEADER


EXTERN_C void getFPReturn(int fpSize, INT64 *pRetVal);
EXTERN_C void setFPReturn(int fpSize, INT64 retVal);

#define HAS_FIXUP_PRECODE                       1

// ThisPtrRetBufPrecode one is necessary for closed delegates over static methods with return buffer
#define HAS_THISPTR_RETBUF_PRECODE              1

#define CODE_SIZE_ALIGN                         4
#define CACHE_LINE_SIZE                         32  // As per Intel Optimization Manual the cache line size is 32 bytes
#define LOG2SLOT                                LOG2_PTRSIZE

#define ENREGISTERED_RETURNTYPE_MAXSIZE         32  // bytes (maximum HFA size is 4 doubles)
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE 4   // bytes

#define CALLDESCR_ARGREGS                       1   // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                     1   // CallDescrWorker has FloatArgumentRegisters parameter

// Given a return address retrieved during stackwalk,
// this is the offset by which it should be decremented to arrive at the callsite.
#define STACKWALK_CONTROLPC_ADJUST_OFFSET 2

// Max offset for unconditional thumb branch
#define MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB 2048

// Offset of pc register
#define PC_REG_RELATIVE_OFFSET 4

#define FLOAT_REGISTER_SIZE 4 // each register in FloatArgumentRegisters is 4 bytes.

//**********************************************************************
// Parameter size
//**********************************************************************

inline unsigned StackElemSize(unsigned parmSize, bool isValueType = false /* unused */, bool isFloatHfa = false /* unused */)
{
    const unsigned stackSlotSize = 4;
    return ALIGN_UP(parmSize, stackSlotSize);
}

//**********************************************************************
// Frames
//**********************************************************************

//--------------------------------------------------------------------
// This represents the callee saved (non-volatile) registers saved as
// of a FramedMethodFrame.
//--------------------------------------------------------------------
typedef DPTR(struct CalleeSavedRegisters) PTR_CalleeSavedRegisters;
struct CalleeSavedRegisters {
    INT32 r4, r5, r6, r7, r8, r9, r10;
    INT32 r11; // frame pointer
    INT32 r14; // link register
};

//--------------------------------------------------------------------
// This represents the arguments that are stored in volatile registers.
// This should not overlap the CalleeSavedRegisters since those are already
// saved separately and it would be wasteful to save the same register twice.
// If we do use a non-volatile register as an argument, then the ArgIterator
// will probably have to communicate this back to the PromoteCallerStack
// routine to avoid a double promotion.
//--------------------------------------------------------------------
typedef DPTR(struct ArgumentRegisters) PTR_ArgumentRegisters;
struct ArgumentRegisters {
    INT32 r[4]; // r0, r1, r2, r3
};
#define NUM_ARGUMENT_REGISTERS 4

//--------------------------------------------------------------------
// This represents the floating point argument registers which are saved
// as part of the NegInfo for a FramedMethodFrame. Note that these
// might not be saved by all stubs: typically only those that call into
// C++ helpers will need to preserve the values in these volatile
// registers.
//--------------------------------------------------------------------
typedef DPTR(struct FloatArgumentRegisters) PTR_FloatArgumentRegisters;
struct FloatArgumentRegisters {
    union
    {
        float   s[16];  // s0-s15
        double  d[8];   // d0-d7
    };
};
#define NUM_FLOAT_ARGUMENT_REGISTERS 16 // Count the single registers, as they are addressable more finely

// forward decl
struct REGDISPLAY;
typedef REGDISPLAY *PREGDISPLAY;

// Sufficient context for Try/Catch restoration.
struct EHContext {
    INT32 r[16]; // note: includes r15(pc)
    void Setup(PCODE resumePC, PREGDISPLAY regs);

    inline TADDR GetSP() {
        LIMITED_METHOD_CONTRACT;
        return (TADDR)r[13];
    }
    inline void SetSP(LPVOID esp) {
        LIMITED_METHOD_CONTRACT;
        r[13] = (INT32)(size_t)esp;
    }

    inline LPVOID GetFP() {
        LIMITED_METHOD_CONTRACT;
        return (LPVOID)(UINT_PTR)r[11];
    }

    inline void SetArg(LPVOID arg) {
        LIMITED_METHOD_CONTRACT;
        r[0] = (INT32)(size_t)arg;
    }
};

#define ARGUMENTREGISTERS_SIZE sizeof(ArgumentRegisters)

//**********************************************************************
// Exception handling
//**********************************************************************

inline PCODE GetIP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return PCODE(context->Pc);
}

inline void SetIP(T_CONTEXT *context, PCODE eip) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Pc = DWORD(eip);
}

inline TADDR GetSP(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return TADDR(context->Sp);
}

inline PCODE GetLR(const T_CONTEXT * context) {
    LIMITED_METHOD_DAC_CONTRACT;
    return PCODE(context->Lr);
}

extern "C" LPVOID __stdcall GetCurrentSP();

inline void SetSP(T_CONTEXT *context, TADDR esp) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Sp = DWORD(esp);
}

inline void SetFP(T_CONTEXT *context, TADDR ebp) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->R11 = DWORD(ebp);
}

inline TADDR GetFP(const T_CONTEXT * context)
{
    LIMITED_METHOD_DAC_CONTRACT;
    return (TADDR)(context->R11);
}

inline void ClearITState(T_CONTEXT *context) {
    LIMITED_METHOD_DAC_CONTRACT;
    context->Cpsr = context->Cpsr & 0xf9ff03ff;
}

#ifdef FEATURE_COMINTEROP
void emitCOMStubCall (ComCallMethodDesc *pCOMMethodRX, ComCallMethodDesc *pCOMMethodRW, PCODE target);
#endif // FEATURE_COMINTEROP

//------------------------------------------------------------------------
inline void emitUnconditionalBranchThumb(LPBYTE pBuffer, int16_t offset)
{
    LIMITED_METHOD_CONTRACT;

    uint16_t *pInstr = (uint16_t *) pBuffer;

    // offset from -2KB to +2KB
    _ASSERTE (offset >= - MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB && offset < MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB);

    if (offset >= 0)
    {
        offset = offset >> 1;
    }
    else
    {
        offset = ((MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB + offset) >> 1) | 0x400;
    }

    *pInstr = 0xE000 | offset;
}

//------------------------------------------------------------------------
inline int16_t decodeUnconditionalBranchThumb(LPBYTE pBuffer)
{
    LIMITED_METHOD_CONTRACT;

    uint16_t *pInstr = (uint16_t *) pBuffer;

    int16_t offset = (~0xE000) & (*pInstr);

    if ((offset & 0x400) == 0)
    {
        offset = offset << 1;
    }
    else
    {
        offset = (~0x400) & offset;
        offset = (offset << 1) - MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB;
    }

    // offset from -2KB to +2KB
    _ASSERTE (offset >= - MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB && offset < MAX_OFFSET_UNCONDITIONAL_BRANCH_THUMB);

    return offset;
}

//------------------------------------------------------------------------
inline void emitJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    LIMITED_METHOD_CONTRACT;

    // The PC-relative load we emit below requires 4-byte alignment for the offset to be calculated correctly.
    _ASSERTE(((UINT_PTR)pBufferRX & 3) == 0);

    DWORD * pCode = (DWORD *)pBufferRW;

    // ldr pc, [pc, #0]
    pCode[0] = 0xf000f8df;
    pCode[1] = (DWORD)(size_t)target;
}

//------------------------------------------------------------------------
//  Given the same pBuffer that was used by emitJump this method
//  decodes the instructions and returns the jump target
inline PCODE decodeJump(PCODE pCode)
{
    LIMITED_METHOD_CONTRACT;

    TADDR pInstr = PCODEToPINSTR(pCode);

    return *dac_cast<PTR_PCODE>(pInstr + sizeof(DWORD));
}

//
// On IA64 back to back jumps should be separated by a nop bundle to get
// the best performance from the hardware's branch prediction logic.
// For all other platforms back to back jumps don't require anything special
// That is why we have these two wrapper functions that call emitJump and decodeJump
//

//------------------------------------------------------------------------
inline BOOL isJump(PCODE pCode)
{
    LIMITED_METHOD_DAC_CONTRACT;

    TADDR pInstr = PCODEToPINSTR(pCode);

    return *dac_cast<PTR_DWORD>(pInstr) == 0xf000f8df;
}

//------------------------------------------------------------------------
inline BOOL isBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    return isJump(pBuffer);
}

//------------------------------------------------------------------------
inline void emitBackToBackJump(LPBYTE pBufferRX, LPBYTE pBufferRW, LPVOID target)
{
    WRAPPER_NO_CONTRACT;
    emitJump(pBufferRX, pBufferRW, target);
}

//------------------------------------------------------------------------
inline PCODE decodeBackToBackJump(PCODE pBuffer)
{
    WRAPPER_NO_CONTRACT;
    return decodeJump(pBuffer);
}

//----------------------------------------------------------------------
#include "stublink.h"
struct ArrayOpScript;

inline BOOL IsThumbCode(PCODE pCode)
{
    return (pCode & THUMB_CODE) != 0;
}

struct ThumbReg
{
    int reg;
    ThumbReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 16);
    }

    operator int ()
    {
        return reg;
    }

    int operator == (ThumbReg other)
    {
        return reg == other.reg;
    }

    int operator != (ThumbReg other)
    {
        return reg != other.reg;
    }

    WORD Mask() const
    {
        return 1 << reg;
    }

};

struct ThumbCond
{
    int cond;
    ThumbCond(int cond):cond(cond)
    {
        _ASSERTE(0 <= cond && cond < 16);
    }
};

struct ThumbVFPSingleReg
{
    int reg;
    ThumbVFPSingleReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 31);
    }

    operator int ()
    {
        return reg;
    }

    int operator == (ThumbVFPSingleReg other)
    {
        return reg == other.reg;
    }

    int operator != (ThumbVFPSingleReg other)
    {
        return reg != other.reg;
    }

    WORD Mask() const
    {
        return 1 << reg;
    }

};

struct ThumbVFPDoubleReg
{
    int reg;
    ThumbVFPDoubleReg(int reg):reg(reg)
    {
        _ASSERTE(0 <= reg && reg < 31);
    }

    operator int ()
    {
        return reg;
    }

    int operator == (ThumbVFPDoubleReg other)
    {
        return reg == other.reg;
    }

    int operator != (ThumbVFPDoubleReg other)
    {
        return reg != other.reg;
    }

    WORD Mask() const
    {
        return 1 << reg;
    }
};

const ThumbReg thumbRegFp = ThumbReg(11);
const ThumbReg thumbRegSp = ThumbReg(13);
const ThumbReg thumbRegLr = ThumbReg(14);
const ThumbReg thumbRegPc = ThumbReg(15);

const ThumbCond thumbCondEq = ThumbCond(0);
const ThumbCond thumbCondNe = ThumbCond(1);
const ThumbCond thumbCondCs = ThumbCond(2);
const ThumbCond thumbCondCc = ThumbCond(3);
const ThumbCond thumbCondMi = ThumbCond(4);
const ThumbCond thumbCondPl = ThumbCond(5);
const ThumbCond thumbCondVs = ThumbCond(6);
const ThumbCond thumbCondVc = ThumbCond(7);
const ThumbCond thumbCondHi = ThumbCond(8);
const ThumbCond thumbCondLs = ThumbCond(9);
const ThumbCond thumbCondGe = ThumbCond(10);
const ThumbCond thumbCondLt = ThumbCond(11);
const ThumbCond thumbCondGt = ThumbCond(12);
const ThumbCond thumbCondLe = ThumbCond(13);
const ThumbCond thumbCondAl = ThumbCond(14);

class StubLinkerCPU : public StubLinker
{
public:
    static void Init();

    void ThumbEmitProlog(UINT cCalleeSavedRegs, UINT cbStackFrame, BOOL fPushArgRegs)
    {
        _ASSERTE(!m_fProlog);

        // Record the parameters of this prolog so that we can generate a matching epilog and unwind info.
        DescribeProlog(cCalleeSavedRegs, cbStackFrame, fPushArgRegs);

        // Trivial prologs (which is all that we support initially) consist of between one and three
        // instructions.

        // 1) Push argument registers. This is all or nothing (if we push, we push R0-R3).
        if (fPushArgRegs)
        {
            // push {r0-r3}
            ThumbEmitPush(ThumbReg(0).Mask() | ThumbReg(1).Mask() | ThumbReg(2).Mask() | ThumbReg(3).Mask());
        }

        // 2) Push callee saved registers. We always start pushing at R4, and only saved consecutive registers
        //    from there (max is R11). Additionally we always assume LR is saved for these types of prolog.
        // push {r4-rX,lr}
        WORD wRegisters = thumbRegLr.Mask();
        for (unsigned int i = 4; i < (4 + cCalleeSavedRegs); i++)
            wRegisters |= ThumbReg(i).Mask();
        ThumbEmitPush(wRegisters);

        // 3) Reserve space on the stack for the rest of the frame.
        if (cbStackFrame)
        {
            // sub sp, #cbStackFrame
            ThumbEmitSubSp(cbStackFrame);
        }
    }

    void ThumbEmitEpilog()
    {
        // Generate an epilog matching a prolog generated by ThumbEmitProlog.
        _ASSERTE(m_fProlog);

        // If additional stack space for a frame was allocated remove it now.
        if (m_cbStackFrame)
        {
            // add sp, #m_cbStackFrame
            ThumbEmitAddSp(m_cbStackFrame);
        }

        // Pop callee saved registers (we always have at least LR). If no argument registers were saved then
        // we can restore LR back into PC and we're done. Otherwise LR needs to be restored into LR.
        // pop {r4-rX,lr|pc}
        WORD wRegisters = m_fPushArgRegs ? thumbRegLr.Mask() : thumbRegPc.Mask();
        for (unsigned int i = 4; i < (4 + m_cCalleeSavedRegs); i++)
            wRegisters |= ThumbReg(i).Mask();
        ThumbEmitPop(wRegisters);

        if (!m_fPushArgRegs)
            return;

        // We pushed the argument registers. These aren't restored, but we need to reclaim the stack space.
        // add sp, #16
        ThumbEmitAddSp(16);

        // Return. The return address has been restored into LR at this point.
        // bx lr
        ThumbEmitJumpRegister(thumbRegLr);
    }

    void ThumbEmitGetThread(ThumbReg dest);

    void ThumbEmitNop()
    {
        // nop
        Emit16(0xbf00);
    }

    void ThumbEmitBreakpoint()
    {
        // Permanently undefined instruction #0xfe (see ARMv7-A A6.2.6). The debugger seems to accept this as
        // a reasonable breakpoint substitute (it's what DebugBreak uses). Bkpt #0, on the other hand, always
        // seems to flow directly to the kernel debugger (even if we ignore it there it doesn't seem to be
        // picked up by the user mode debugger).
#ifdef __linux__
        Emit16(0xde01);
#else
        Emit16(0xdefe);
#endif
    }

    void ThumbEmitMovConstant(ThumbReg dest, int constant)
    {
        _ASSERT(dest != thumbRegPc);

        //Emit 2 Byte instructions when dest reg < 8 & constant <256
        if(dest <= 7 && constant < 256 && constant >= 0)
        {
            Emit16((WORD)(0x2000 | dest<<8 | (WORD)constant));
        }
        else // emit 4 byte instructions
        {
            WORD wConstantLow = (WORD)(constant & 0xffff);
            WORD wConstantHigh = (WORD)(constant >> 16);

            // movw regDest, #wConstantLow
            Emit16((WORD)(0xf240 | (wConstantLow >> 12) | ((wConstantLow & 0x0800) ? 0x0400 : 0x0000)));
            Emit16((WORD)((dest << 8) | (((wConstantLow >> 8) & 0x0007) << 12) | (wConstantLow & 0x00ff)));

            if (wConstantHigh)
            {
                // movt regDest, #wConstantHighw
                Emit16((WORD)(0xf2c0 | (wConstantHigh >> 12) | ((wConstantHigh & 0x0800) ? 0x0400 : 0x0000)));
                Emit16((WORD)((dest << 8) | (((wConstantHigh >> 8) & 0x0007) << 12) | (wConstantHigh & 0x00ff)));
            }
        }
    }

    void ThumbEmitLoadRegIndirect(ThumbReg dest, ThumbReg source, int offset)
    {
        _ASSERTE((offset >= 0) && (offset <= 4095));

        // ldr regDest, [regSource + #offset]
        if ((dest < 8) && (source < 8) && ((offset & 0x3) == 0) && (offset < 125))
        {
            // Encoding T1
            Emit16((WORD)(0x6800 | ((offset >> 2) << 6) | (source << 3) | dest));
        }
        else
        {
            // Encoding T3
            Emit16((WORD)(0xf8d0 | source));
            Emit16((WORD)((dest << 12) | offset));
        }
    }

    void ThumbEmitLoadIndirectPostIncrement(ThumbReg dest, ThumbReg source, int offset)
    {
        _ASSERTE((offset >= 0) && (offset <= 255));

        // ldr regDest, [regSource], #offset
        Emit16((WORD)(0xf850 | source));
        Emit16((WORD)(0x0b00 | (dest << 12) | offset));
    }

    void ThumbEmitStoreRegIndirect(ThumbReg source, ThumbReg dest, int offset)
    {
        _ASSERTE((offset >= -255) && (offset <= 4095));

        // str regSource, [regDest + #offset]
        if (offset < 0)
        {
            Emit16((WORD)(0xf840 | dest));
            Emit16((WORD)(0x0C00 | (source << 12) | (UINT8)(-offset)));
        }
        else
        if ((dest < 8) && (source < 8) && ((offset & 0x3) == 0) && (offset < 125))
        {
            // Encoding T1
            Emit16((WORD)(0x6000 | ((offset >> 2) << 6) | (dest << 3) | source));
        }
        else
        {
            // Encoding T3
            Emit16((WORD)(0xf8c0 | dest));
            Emit16((WORD)((source << 12) | offset));
        }
    }

    void ThumbEmitStoreIndirectPostIncrement(ThumbReg source, ThumbReg dest, int offset)
    {
        _ASSERTE((offset >= 0) && (offset <= 255));

        // str regSource, [regDest], #offset
        Emit16((WORD)(0xf840 | dest));
        Emit16((WORD)(0x0b00 | (source << 12) | offset));
    }

    void ThumbEmitLoadOffsetScaledReg(ThumbReg dest, ThumbReg base, ThumbReg offset, int shift)
    {
        _ASSERTE(shift >=0 && shift <=3);

        Emit16((WORD)(0xf850 | base));
        Emit16((WORD)((dest << 12) | (shift << 4) | offset));
    }

    void ThumbEmitCallRegister(ThumbReg target)
    {
        // blx regTarget
        Emit16((WORD)(0x4780 | (target << 3)));
    }

    void ThumbEmitJumpRegister(ThumbReg target)
    {
        // bx regTarget
        Emit16((WORD)(0x4700 | (target << 3)));
    }

    void ThumbEmitMovRegReg(ThumbReg dest, ThumbReg source)
    {
        // mov regDest, regSource
        Emit16((WORD)(0x4600 | ((dest > 7) ? 0x0080 : 0x0000) | (source << 3) | (dest & 0x0007)));
    }

    //Assuming SP is only subtracted in prolog
    void ThumbEmitSubSp(int value)
    {
        _ASSERTE(value >= 0);
        _ASSERTE((value & 0x3) == 0);

        if(value < 512)
        {
            // encoding T1
            // sub sp, sp, #(value >> 2)
            Emit16((WORD)(0xb080 | (value >> 2)));
        }
        else if(value < 4096)
        {
            // Using 32-bit encoding
            Emit16((WORD)(0xf2ad| ((value & 0x0800) >> 1)));
            Emit16((WORD)(0x0d00| ((value & 0x0700) << 4) | (value & 0x00ff)));
        }
        else
        {
            // For values >= 4K (pageSize) must check for guard page

            // mov r4, value
            ThumbEmitMovConstant(ThumbReg(4), value);
            // mov r12, checkStack
            ThumbEmitMovConstant(ThumbReg(12), (int)checkStack);
            // bl r12
            ThumbEmitCallRegister(ThumbReg(12));

            // sub sp,sp,r4
            Emit16((WORD)0xebad);
            Emit16((WORD)0x0d04);
        }
    }

    void ThumbEmitAddSp(int value)
    {
        _ASSERTE(value >= 0);
        _ASSERTE((value & 0x3) == 0);

        if(value < 512)
        {
            // encoding T2
            // add sp, sp, #(value >> 2)
            Emit16((WORD)(0xb000 | (value >> 2)));
        }
        else if(value < 4096)
        {
            // Using 32-bit encoding T4
            Emit16((WORD)(0xf20d| ((value & 0x0800) >> 1)));
            Emit16((WORD)(0x0d00| ((value & 0x0700) << 4) | (value & 0x00ff)));
        }
        else
        {
            //Must use temp register for values >=4096
            ThumbEmitMovConstant(ThumbReg(12), value);
            // add sp,sp,r12
            Emit16((WORD)0x44e5);
        }
    }

    void ThumbEmitAddReg(ThumbReg dest, ThumbReg source)
    {

        _ASSERTE(dest != source);
        Emit16((WORD)(0x4400 | ((dest & 0x8)<<4) | (source<<3) | (dest & 0x7)));
    }

    void ThumbEmitAdd(ThumbReg dest, ThumbReg source, unsigned int value)
    {

        if(value<4096)
        {
            // addw dest, source, #value
            unsigned int i = (value & 0x800) >> 11;
            unsigned int imm3 = (value & 0x700) >> 8;
            unsigned int imm8 = value & 0xff;
            Emit16((WORD)(0xf200 | (i << 10) | source));
            Emit16((WORD)((imm3 << 12) | (dest << 8) | imm8));
        }
        else
        {
            // if immediate is more than 4096 only ADD (register) will work
            // move immediate to dest reg and call ADD(reg)
            // this will not work if dest is same as source.
            _ASSERTE(dest != source);
            ThumbEmitMovConstant(dest, value);
            ThumbEmitAddReg(dest, source);
        }
    }

    void ThumbEmitSub(ThumbReg dest, ThumbReg source, unsigned int value)
    {
        _ASSERTE(value < 4096);

        // subw dest, source, #value
        unsigned int i = (value & 0x800) >> 11;
        unsigned int imm3 = (value & 0x700) >> 8;
        unsigned int imm8 = value & 0xff;
        Emit16((WORD)(0xf2a0 | (i << 10) | source));
        Emit16((WORD)((imm3 << 12) | (dest << 8) | imm8));
    }

    void ThumbEmitIncrement(ThumbReg dest, unsigned int value)
    {
        while (value)
        {
            if (value >= 4095)
            {
                // addw <dest>, <dest>, #4095
                ThumbEmitAdd(dest, dest, 4095);
                value -= 4095;
            }
            else if (value <= 255)
            {
                // add <dest>, #value
                Emit16((WORD)(0x3000 | (dest << 8) | value));
                break;
            }
            else
            {
                // addw <dest>, <dest>, #value
                ThumbEmitAdd(dest, dest, value);
                break;
            }
        }
    }

    void ThumbEmitPush(WORD registers)
    {
        _ASSERTE(registers != 0);
        _ASSERTE((registers & 0xa000) == 0); // Pushing SP or PC undefined

        // push {registers}
        if (CountBits(registers) == 1)
        {
            // Encoding T3 (exactly one register, high or low)
            WORD reg = 15;
            while ((registers & (WORD)(1 << reg)) == 0)
            {
                reg--;
            }
            Emit16(0xf84d);
            Emit16(0x0d04 | (reg << 12));
        }
        else if ((registers & 0xbf00) == 0)
        {
            // Encoding T1 (low registers plus maybe LR)
            Emit16(0xb400 | (registers & thumbRegLr.Mask() ? 0x0100: 0x0000) | (registers & 0x00ff));
        }
        else
        {
            // Encoding T2 (two or more registers, high or low)
            Emit16(0xe92d);
            Emit16(registers);
        }
    }

    void ThumbEmitLoadStoreMultiple(ThumbReg base, bool load, WORD registers)
    {
        _ASSERTE(CountBits(registers) > 1);
        _ASSERTE((registers & 0xFF00) == 0); // This only supports the small encoding
        _ASSERTE(base < 8); // This only supports the small encoding
        _ASSERTE((base.Mask() & registers) == 0); // This only supports the small encoding

        // (LDM|STM) base, {registers}
        WORD flag = load ? 0x0800 : 0;
        Emit16(0xc000 | flag | ((base & 7) << 8) | (registers & 0xFF));
    }

    void ThumbEmitPop(WORD registers)
    {
        _ASSERTE(registers != 0);
        _ASSERTE((registers & 0xc000) != 0xc000); // Popping PC and LR together undefined

        // pop {registers}
        if (CountBits(registers) == 1)
        {
            // Encoding T3 (exactly one register, high or low)
            WORD reg = 15;
            while ((registers & (WORD)(1 << reg)) == 0)
            {
                reg--;
            }
            Emit16(0xf85d);
            Emit16(0x0b04 | (reg << 12));
        }
        else if ((registers & 0x7f00) == 0)
        {
            // Encoding T1 (low registers plus maybe PC)
            Emit16(0xbc00 | (registers & thumbRegPc.Mask() ? 0x0100: 0x0000) | (registers & 0x00ff));
        }
        else
        {
            // Encoding T2 (two or more registers, high or low)
            Emit16(0xe8bd);
            Emit16(registers);
        }
    }

    void ThumbEmitLoadVFPSingleRegIndirect(ThumbVFPSingleReg dest, ThumbReg source, int offset)
    {
        _ASSERTE((offset >= -1020) && (offset <= 1020));
        _ASSERTE(offset%4==0);

        Emit16((WORD) (0xed10 | ((offset > 0 ? 0x1: 0x0) << 7) | ((dest & 0x1) << 6) | source));
        Emit16((WORD) (0x0a00 | ((dest & 0x1e) << 11) | (abs(offset)>>2)));
    }

    void ThumbEmitLoadVFPDoubleRegIndirect(ThumbVFPDoubleReg dest, ThumbReg source, int offset)
    {
        _ASSERTE((offset >= -1020) && (offset <= 1020));
        _ASSERTE(offset%4==0);

        Emit16((WORD) (0xed10 | ((offset > 0 ? 0x1: 0x0) << 7) | ((dest & 0x10) << 6) | source));
        Emit16((WORD) (0x0b00 | ((dest & 0xf) << 12) | (abs(offset)>>2)));
    }

#ifdef FEATURE_INTERPRETER
    void ThumbEmitStoreMultipleVFPDoubleReg(ThumbVFPDoubleReg source, ThumbReg dest, unsigned numRegs)
    {
        _ASSERTE((numRegs + source) <= 16);

        // The third nibble is 0x8; the 0x4 bit (D) is zero because the source reg number must be less
        // than 16 for double registers.
        Emit16((WORD) (0xec80 | 0x80 | dest));
        Emit16((WORD) (((source & 0xf) << 12) | 0xb00 | numRegs));
    }

    void ThumbEmitLoadMultipleVFPDoubleReg(ThumbVFPDoubleReg dest, ThumbReg source, unsigned numRegs)
    {
        _ASSERTE((numRegs + dest) <= 16);

        // The third nibble is 0x8; the 0x4 bit (D) is zero because the source reg number must be less
        // than 16 for double registers.
        Emit16((WORD) (0xec90 | 0x80 | source));
        Emit16((WORD) (((dest & 0xf) << 12) | 0xb00 | numRegs));
    }
#endif // FEATURE_INTERPRETER

    // Scratches r12.
    void ThumbEmitTailCallManagedMethod(MethodDesc *pMD);

    void EmitShuffleThunk(struct ShuffleEntry *pShuffleEntryArray);
    VOID EmitComputedInstantiatingMethodStub(MethodDesc* pSharedMD, struct ShuffleEntry *pShuffleEntryArray, void* extraArg);
};

extern "C" void SinglecastDelegateInvokeStub();

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable:4359) // Prevent "warning C4359: 'UMEntryThunkCode': Alignment specifier is less than actual alignment (8), and will be ignored." in crossbitness scenario
#endif // _MSC_VER

struct DECLSPEC_ALIGN(4) UMEntryThunkCode
{
    WORD        m_code[4];

    TADDR       m_pTargetCode;
    TADDR       m_pvSecretParam;

    void Encode(UMEntryThunkCode *pEntryThunkCodeRX, BYTE* pTargetCode, void* pvSecretParam);
    void Poison();

    LPCBYTE GetEntryPoint() const
    {
        LIMITED_METHOD_CONTRACT;

        return (LPCBYTE)((TADDR)this | THUMB_CODE);
    }

    static int GetEntryPointOffset()
    {
        LIMITED_METHOD_CONTRACT;

        return 0;
    }
};

#ifdef _MSC_VER
#pragma warning(pop)
#endif // _MSC_VER

struct HijackArgs
{
    union
    {
        DWORD R0;
        size_t ReturnValue[1]; // this may not be the return value when return is >32bits
                               // or return value is in VFP reg but it works for us as
                               // this is only used by functions OnHijackWorker()
    };

    //
    // Non-volatile Integer registers
    //
    DWORD R4;
    DWORD R5;
    DWORD R6;
    DWORD R7;
    DWORD R8;
    DWORD R9;
    DWORD R10;
    DWORD R11;

    union
    {
        DWORD Lr;
        size_t ReturnAddress;
    };
};

// ClrFlushInstructionCache is used when we want to call FlushInstructionCache
// for a specific architecture in the common code, but not for other architectures.
// On IA64 ClrFlushInstructionCache calls the Kernel FlushInstructionCache function
// to flush the instruction cache.
// We call ClrFlushInstructionCache whenever we create or modify code in the heap.
// Currently ClrFlushInstructionCache has no effect on X86
//

inline BOOL ClrFlushInstructionCache(LPCVOID pCodeAddr, size_t sizeOfCode)
{
    return FlushInstructionCache(GetCurrentProcess(), pCodeAddr, sizeOfCode);
}

//
// JIT HELPER ALIASING FOR PORTABILITY.
//
// Create alias for optimized implementations of helpers provided on this platform
//
#define JIT_GetSharedGCStaticBase           JIT_GetSharedGCStaticBase_SingleAppDomain
#define JIT_GetSharedNonGCStaticBase        JIT_GetSharedNonGCStaticBase_SingleAppDomain
#define JIT_GetSharedGCStaticBaseNoCtor     JIT_GetSharedGCStaticBaseNoCtor_SingleAppDomain
#define JIT_GetSharedNonGCStaticBaseNoCtor  JIT_GetSharedNonGCStaticBaseNoCtor_SingleAppDomain


//------------------------------------------------------------------------
//
// Precode definitions
//
//------------------------------------------------------------------------
//
// Note: If you introduce new precode implementation below, then please
//       update PrecodeStubManager::CheckIsStub_Internal to account for it.

// Precode to shuffle this and retbuf for closed delegates over static methods with return buffer
struct ThisPtrRetBufPrecode {

    static const int Type = 0x46;

    // mov r12, r0
    // mov r0, r1
    // mov r1, r12
    // ldr pc, [pc, #0]     ; =m_pTarget
    // dcd pTarget
    // dcd pMethodDesc
    WORD    m_rgCode[6];
    TADDR   m_pTarget;
    TADDR   m_pMethodDesc;

    void Init(MethodDesc* pMD, LoaderAllocator *pLoaderAllocator);

    TADDR GetMethodDesc()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        return m_pMethodDesc;
    }

    PCODE GetTarget()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pTarget;
    }

#ifndef DACCESS_COMPILE
    BOOL SetTargetInterlocked(TADDR target, TADDR expected)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
        }
        CONTRACTL_END;

        ExecutableWriterHolder<ThisPtrRetBufPrecode> precodeWriterHolder(this, sizeof(ThisPtrRetBufPrecode));
        return InterlockedCompareExchange((LONG*)&precodeWriterHolder.GetRW()->m_pTarget, (LONG)target, (LONG)expected) == (LONG)expected;
    }
#endif // !DACCESS_COMPILE
};
typedef DPTR(ThisPtrRetBufPrecode) PTR_ThisPtrRetBufPrecode;


//**********************************************************************
// Miscellaneous
//**********************************************************************

// Given the first halfword value of an ARM (Thumb) instruction (which is either an entire
// 16-bit instruction, or the high-order halfword of a 32-bit instruction), determine how many bytes
// the instruction is (2 or 4) and return that.
inline size_t GetARMInstructionLength(WORD instr)
{
    // From the ARM Architecture Reference Manual, A6.1 "Thumb instruction set encoding":
    // If bits [15:11] of the halfword being decoded take any of the following values, the halfword is the first
    // halfword of a 32-bit instruction:
    //   0b11101
    //   0b11110
    //   0b11111
    // Otherwise, the halfword is a 16-bit instruction.
    if ((instr & 0xf800) > 0xe000)
    {
        return 4;
    }
    else
    {
        return 2;
    }
}

// Given a pointer to an ARM (Thumb) instruction address, determine how many bytes
// the instruction is (2 or 4) and return that.
inline size_t GetARMInstructionLength(PBYTE pInstr)
{
    return GetARMInstructionLength(*(WORD*)pInstr);
}

#endif // __cgencpu_h__
